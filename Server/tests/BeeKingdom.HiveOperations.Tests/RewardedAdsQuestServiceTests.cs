using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class RewardedAdsQuestServiceTests
{
    private static readonly DateTimeOffset Day = new(2026, 10, 24, 12, 34, 0, TimeSpan.Zero);
    private static readonly RewardedAdsQuestOptions Configuration = new()
    {
        Enabled = true,
        AppKey = RewardedAdsQuestOptions.ExpectedAppKey,
        PrivateKey = "S2STestOnlySecretDoNotDeployOrCommitToProduction"
    };

    [Fact]
    public void VerifiedSignatureRejectsTamperingAndOldOrFutureCallbacks()
    {
        Guid playerId = Guid.NewGuid();
        LevelPlayRewardCallback valid = Sign("S2SEVENT001", playerId);
        Assert.True(LevelPlayRewardSignature.TryVerify(valid, Configuration, Day,
            out Guid verified, out DateTimeOffset timestamp));
        Assert.Equal(playerId, verified);
        Assert.Equal(new DateTimeOffset(2026, 10, 24, 12, 34, 0, TimeSpan.Zero), timestamp);
        Assert.False(LevelPlayRewardSignature.TryVerify(valid with { Rewards = "1000" }, Configuration, Day, out _, out _));
        Assert.False(LevelPlayRewardSignature.TryVerify(valid with { Signature = new string('0', 32) }, Configuration, Day, out _, out _));
        Assert.False(LevelPlayRewardSignature.TryVerify(valid with { UserId = Guid.NewGuid().ToString("N") }, Configuration, Day, out _, out _));
        Assert.False(LevelPlayRewardSignature.TryVerify(valid with { AppKey = "another-app" }, Configuration, Day, out _, out _));
        Assert.False(LevelPlayRewardSignature.TryVerify(valid, Configuration, Day.AddDays(11), out _, out _));
        Assert.False(LevelPlayRewardSignature.TryVerify(valid, Configuration, Day.AddMinutes(-10), out _, out _));
        Assert.False(LevelPlayRewardSignature.TryVerify(Sign("unsafe%event", playerId), Configuration, Day, out _, out _));
    }

    [Fact]
    public async Task ExactlyThreeCreditedPerDayWithIdempotentEventReceipts()
    {
        var repo = new MemoryRepo(NewState());
        var clock = new TestClock(Day);
        var service = new RewardedAdsQuestService(repo, clock, Configuration);
        Guid player = repo.State.PlayerId;
        LevelPlayRewardProcessingResult first =
            await service.ProcessAsync(Sign("EVENT001", player));
        Assert.True(first.Ack);
        Assert.Equal("credited", first.Code);
        LevelPlayRewardProcessingResult duplicate =
            await service.ProcessAsync(Sign("EVENT001", player));
        Assert.True(duplicate.Ack);
        Assert.Equal("already_processed", duplicate.Code);
        Assert.Equal(1, repo.State.Revision);
        Assert.Equal(20, repo.State.Resources["honey"].Amount);
        Assert.Equal(10, repo.State.Resources["pollen"].Amount);
        Assert.Equal("credited", (await service.ProcessAsync(Sign("EVENT002", player))).Code);
        Assert.Equal("credited", (await service.ProcessAsync(Sign("EVENT003", player))).Code);
        Assert.Equal("daily_limit_reached", (await service.ProcessAsync(Sign("EVENT004", player))).Code);
        RewardedAdsQuestSnapshot snapshot = (await service.ReadAsync(player, repo.State.HiveId))!;
        Assert.Equal(3, snapshot.CompletedCount);
        Assert.Equal(0, snapshot.NextHoney);
        Assert.Equal(0, snapshot.NextPollen);
        Assert.Equal(120, repo.State.Resources["honey"].Amount);
        Assert.Equal(60, repo.State.Resources["pollen"].Amount);
        Assert.Equal(3, repo.State.RewardedAdsQuest!.CountByDay["2026-10-24"]);
        Assert.Equal(4, repo.State.RewardedAdsQuest.ProcessedEvents.Count);
        Assert.Equal(4, repo.State.Revision);
    }

    [Fact]
    public async Task ANewUtcDayResetsTheQuotaWithoutRecreditingAnOldEvent()
    {
        var repo = new MemoryRepo(NewState());
        var clock = new TestClock(Day);
        var service = new RewardedAdsQuestService(repo, clock, Configuration);
        Guid player = repo.State.PlayerId;
        await service.ProcessAsync(Sign("EVENT001", player, Day));
        clock.UtcNow = Day.AddDays(1);
        Assert.Equal("already_processed", (await service.ProcessAsync(Sign("EVENT001", player, Day))).Code);
        RewardedAdsQuestSnapshot snapshot = (await service.ReadAsync(player, repo.State.HiveId))!;
        Assert.Equal(0, snapshot.CompletedCount);
        Assert.Equal(3, snapshot.MaximumCount);
        Assert.Equal(20, snapshot.NextHoney);
        Assert.Equal("credited", (await service.ProcessAsync(Sign("NEXTDAY01", player, clock.UtcNow))).Code);
        Assert.Equal(40, repo.State.Resources["honey"].Amount);
        Assert.Equal(20, repo.State.Resources["pollen"].Amount);
        Assert.Equal(1, repo.State.RewardedAdsQuest!.CountByDay["2026-10-25"]);
    }

    [Fact]
    public async Task ParallelClaimsAreSerializedAndClampedByResourceCapacity()
    {
        var original = NewState() with
        {
            Resources = new Dictionary<string, ResourceBalance>
            {
                ["honey"] = new(970, 1000), ["pollen"] = new(999, 1000)
            }
        };
        var repo = new MemoryRepo(original);
        var clock = new TestClock(Day);
        var service = new RewardedAdsQuestService(repo, clock, Configuration);
        Guid player = repo.State.PlayerId;
        var tasks = Enumerable.Range(0, 25)
            .Select(x => service.ProcessAsync(Sign("RACE" + x.ToString("D4"), player)));
        var results = await Task.WhenAll(tasks);
        Assert.All(results, result => Assert.True(result.Ack));
        Assert.Equal(3, repo.State.RewardedAdsQuest!.CountByDay["2026-10-24"]);
        Assert.Equal(1000, repo.State.Resources["honey"].Amount);
        Assert.Equal(1000, repo.State.Resources["pollen"].Amount);
        Assert.Equal(30, repo.State.RewardedAdsQuest.LifetimeHoneyCredited);
        Assert.Equal(1, repo.State.RewardedAdsQuest.LifetimePollenCredited);
    }

    [Fact]
    public async Task DisabledOrUnsignedCallbacksFailClosedAndNoHiveIsCreated()
    {
        var repo = new MemoryRepo(NewState());
        var clock = new TestClock(Day);
        var disabled = new RewardedAdsQuestService(repo, clock, new RewardedAdsQuestOptions());
        Assert.Equal("not_configured", (await disabled.ProcessAsync(Sign("EVENT001", repo.State.PlayerId))).Code);
        Assert.Equal(0, repo.State.Revision);
        var enabled = new RewardedAdsQuestService(repo, clock, Configuration);
        Assert.False((await enabled.ProcessAsync(
            Sign("EVENT001", repo.State.PlayerId) with { Signature = "not-a-signature" })).Ack);
        Assert.Equal(0, repo.State.Revision);
        var foreign = Sign("EVENT001", Guid.NewGuid());
        Assert.False((await enabled.ProcessAsync(foreign)).Ack);
    }

    [Fact]
    public async Task PersistedHiveJsonRetainsTheServerAuthorityAfterReload()
    {
        var repo = new MemoryRepo(NewState());
        var clock = new TestClock(Day);
        var service = new RewardedAdsQuestService(repo, clock, Configuration);
        await service.ProcessAsync(Sign("EVENT001", repo.State.PlayerId));
        string json = JsonSerializer.Serialize(repo.State, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        PlayerHiveState loaded = HiveStateMigrator.ToCurrent(
            JsonSerializer.Deserialize<PlayerHiveState>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!);
        Assert.Equal(1, loaded.RewardedAdsQuest!.CountByDay["2026-10-24"]);
        var reloaded = new RewardedAdsQuestService(new MemoryRepo(loaded), clock, Configuration);
        Assert.Equal("already_processed",
            (await reloaded.ProcessAsync(Sign("EVENT001", loaded.PlayerId))).Code);
    }

    private static PlayerHiveState NewState() => new(
        Guid.NewGuid(), Guid.NewGuid(), HiveStateMigrator.CurrentModelVersion, 0,
        new Dictionary<string, ResourceBalance>
        {
            ["honey"] = new(0, 1000), ["pollen"] = new(0, 1000)
        },
        new Dictionary<string, int>(), new List<HiveOperation>(),
        new Dictionary<string, IdempotencyReceipt>());

    private static LevelPlayRewardCallback Sign(
        string eventId, Guid playerId, DateTimeOffset? eventTime = null)
    {
        string timestamp = (eventTime ?? Day).ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
        string id = playerId.ToString("N");
        string hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(
            timestamp + eventId + id + "1" + Configuration.PrivateKey))).ToLowerInvariant();
        return new(timestamp, eventId, id, "1", hash, Configuration.AppKey);
    }

    private sealed class TestClock(DateTimeOffset initial) : IServerClock
    {
        public DateTimeOffset UtcNow { get; set; } = initial;
    }

    private sealed class MemoryRepo(PlayerHiveState initial) : IHiveStateRepository
    {
        private readonly object gate = new();
        public PlayerHiveState State { get; private set; } = initial;
        public Task<PlayerHiveState> ExecuteAtomicallyAsync(
            Guid player, Guid hive, Func<PlayerHiveState, PlayerHiveState> action,
            CancellationToken cancellationToken = default)
        {
            lock (gate) { State = HiveStateMigrator.ToCurrent(action(State)); }
            return Task.FromResult(State);
        }
        public Task<PlayerHiveState?> ReadAsync(Guid player, Guid hive,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PlayerHiveState?>(player == State.PlayerId && hive == State.HiveId
                ? State : null);
        public Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid player,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(
                player == State.PlayerId ? new[] { State.HiveId } : Array.Empty<Guid>());
        public Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlayerHiveState>>(new[] { State });
        public Task<bool> DeleteAsync(Guid player, Guid hive,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
