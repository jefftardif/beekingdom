using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class RewardLedgerServiceTests
{
    [Fact]
    public async Task GrantCreatesClaimableRewardAndLedgerEntryAndEvent()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var repository = new MemoryRepository(CreateState());
        var service = new RewardLedgerService(repository, clock, Options());

        RewardLedgerCommandResult result = await service.GrantAsync(new(Guid.NewGuid(), Guid.NewGuid(), "event_001", "event", "honey", 500, 0, "grant-1", "notification.event_001"));

        Assert.True(result.Succeeded);
        Assert.Equal("reward_granted", result.Code);
        Assert.Equal(1, repository.State.Revision);
        Assert.True(repository.State.Rewards!.ContainsKey("event_001"));
        RewardLedgerState ledger = repository.State.RewardLedger!;
        Assert.Equal(1, ledger.Revision);
        RewardLedgerEntry entry = ledger.Entries["event_001"];
        Assert.Equal("event", entry.Source);
        Assert.Equal("honey", entry.ResourceKey);
        Assert.Equal(500, entry.Amount);
        Assert.Equal(0, entry.CreditedAmount);
        Assert.False(entry.Claimed);
        Assert.Equal("notification.event_001", entry.NotificationKey);
        Assert.Contains(ledger.Events, e => e.EventKey == RewardLedgerService.EventRewardGranted && e.TargetKey == "event_001");
        Assert.Single(result.Snapshot.Rewards);
        Assert.Single(result.Snapshot.Events);
    }

    [Fact]
    public async Task ReplayingSameIdempotencyKeyDoesNotDuplicate()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var repository = new MemoryRepository(CreateState());
        var service = new RewardLedgerService(repository, clock, Options());
        var command = new GrantRewardCommand(repository.State.PlayerId, repository.State.HiveId, "event_001", "event", "honey", 500, 0, "grant-1");

        Assert.True((await service.GrantAsync(command)).Succeeded);
        RewardLedgerCommandResult replay = await service.GrantAsync(command);

        Assert.True(replay.Succeeded);
        Assert.Equal(1, repository.State.Revision);
        Assert.Single(repository.State.RewardLedger!.Entries);
        Assert.Single(repository.State.RewardLedger!.Events);
    }

    [Fact]
    public async Task StaleRevisionIsRejectedWithoutStateChange()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var repository = new MemoryRepository(CreateState());
        var service = new RewardLedgerService(repository, clock, Options());

        RewardLedgerCommandResult result = await service.GrantAsync(new(repository.State.PlayerId, repository.State.HiveId, "event_001", "event", "honey", 500, 5, "grant-1"));

        Assert.False(result.Succeeded);
        Assert.Equal("revision_conflict", result.Code);
        Assert.Equal(0, repository.State.Revision);
        Assert.Empty(repository.State.RewardLedger!.Entries);
    }

    [Fact]
    public async Task ClaimThroughHiveOperationServiceSyncsLedgerEntryAndAppendsEvent()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var repository = new MemoryRepository(CreateState());
        var ledger = new RewardLedgerService(repository, clock, Options());
        Assert.True((await ledger.GrantAsync(new(repository.State.PlayerId, repository.State.HiveId, "event_001", "event", "honey", 500, 0, "grant-1"))).Succeeded);

        var operations = new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>());
        HiveCommandResult claim = await operations.ClaimRewardAsync(new(repository.State.PlayerId, repository.State.HiveId, repository.State.Revision, "event_001", "claim-1"));

        Assert.True(claim.Succeeded);
        Assert.Equal(500, repository.State.Resources["honey"].Amount);
        RewardLedgerEntry entry = repository.State.RewardLedger!.Entries["event_001"];
        Assert.True(entry.Claimed);
        Assert.Equal(500, entry.CreditedAmount);
        Assert.NotNull(entry.ClaimedAtUtc);
        Assert.Contains(repository.State.RewardLedger!.Events, e => e.EventKey == RewardLedgerService.EventRewardClaimed && e.TargetKey == "event_001");
        Assert.Contains(repository.State.RewardLedger!.Events, e => e.EventKey == RewardLedgerService.EventRewardGranted && e.TargetKey == "event_001");
    }

    [Fact]
    public async Task QueueCompletionIsSettledExactlyOnceAcrossReads()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        HiveOperation operation = new(Guid.NewGuid(), "honey_storage", 1, 2, clock.UtcNow.AddMinutes(-5), clock.UtcNow.AddMinutes(-1), HiveOperationStatus.AwaitingCollection, "honey", 100, null);
        var repository = new MemoryRepository(CreateState(operation));
        var service = new RewardLedgerService(repository, clock, Options());

        RewardLedgerReadSnapshot? first = await service.ReadAsync(repository.State.PlayerId, repository.State.HiveId);
        RewardLedgerReadSnapshot? second = await service.ReadAsync(repository.State.PlayerId, repository.State.HiveId);

        Assert.NotNull(first);
        Assert.Single(first.Events);
        Assert.Equal(RewardLedgerService.EventQueueCompleted, first.Events[0].EventKey);
        Assert.Equal("honey_storage", first.Events[0].TargetKey);
        Assert.Single(second.Events);
        Assert.Equal(1, repository.State.RewardLedger!.Revision);
    }

    [Fact]
    public async Task SpeedUpSnapshotExposesPendingRewardsAndLedgerEvents()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        HiveOperation operation = new(Guid.NewGuid(), "honey_storage", 1, 2, clock.UtcNow.AddMinutes(-5), clock.UtcNow.AddMinutes(-1), HiveOperationStatus.AwaitingCollection, "honey", 100, null);
        var repository = new MemoryRepository(CreateState(operation));
        var ledger = new RewardLedgerService(repository, clock, Options());
        await ledger.ReadAsync(repository.State.PlayerId, repository.State.HiveId);
        Assert.True((await ledger.GrantAsync(new(repository.State.PlayerId, repository.State.HiveId, "event_001", "event", "honey", 500, repository.State.Revision, "grant-1"))).Succeeded);
        var speedUps = new SpeedUpInventoryService(repository, clock, SpeedUpOptions());

        SpeedUpReadSnapshot? snapshot = await speedUps.ReadAsync(repository.State.PlayerId, repository.State.HiveId);

        Assert.NotNull(snapshot);
        Assert.Contains("event_001", snapshot.Rewards);
        Assert.Contains(RewardLedgerService.EventQueueCompleted + ":honey_storage", snapshot.Events);
        Assert.Contains(RewardLedgerService.EventRewardGranted + ":event_001", snapshot.Events);
    }

    [Fact]
    public async Task InvalidGrantIsRejected()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow);
        var repository = new MemoryRepository(CreateState());
        var service = new RewardLedgerService(repository, clock, Options());

        RewardLedgerCommandResult negative = await service.GrantAsync(new(repository.State.PlayerId, repository.State.HiveId, "event_001", "event", "honey", -5, 0, "grant-1"));
        RewardLedgerCommandResult blankKey = await service.GrantAsync(new(repository.State.PlayerId, repository.State.HiveId, "", "event", "honey", 5, 0, "grant-2"));

        Assert.False(negative.Succeeded);
        Assert.Equal("invalid_request", negative.Code);
        Assert.False(blankKey.Succeeded);
        Assert.Equal("invalid_request", blankKey.Code);
        Assert.Empty(repository.State.RewardLedger!.Entries);
    }

    private static RewardLedgerOptions Options() => new() { Enabled = true };

    private static SpeedUpOptions SpeedUpOptions() => new()
    {
        Enabled = true,
        Catalog = [new SpeedUpDefinition("universal_60s", SpeedUpCategories.Universal, 60)]
    };

    private static PlayerHiveState CreateState(HiveOperation? operation = null) => new(
        Guid.NewGuid(), Guid.NewGuid(), HiveStateMigrator.CurrentModelVersion, 0,
        new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 1000), ["pollen"] = new(0, 1000), ["wax"] = new(0, 1000) },
        new Dictionary<string, int> { ["honey_storage"] = 1 },
        operation is null ? [] : [operation],
        new Dictionary<string, IdempotencyReceipt>(),
        SpeedUps: new Dictionary<string, int>(StringComparer.Ordinal),
        RewardLedger: new(0, new Dictionary<string, RewardLedgerEntry>(StringComparer.Ordinal), new List<RewardLedgerEvent>(), new HashSet<string>(StringComparer.Ordinal), new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal)));

    private sealed class TestClock(DateTimeOffset value) : IServerClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }

    private sealed class MemoryRepository(PlayerHiveState initial) : IHiveStateRepository
    {
        private readonly object gate = new();
        public PlayerHiveState State { get; private set; } = initial;

        public Task<PlayerHiveState> ExecuteAtomicallyAsync(Guid playerId, Guid hiveId, Func<PlayerHiveState, PlayerHiveState> mutation, CancellationToken cancellationToken = default)
        {
            lock (gate) State = HiveStateMigrator.ToCurrent(mutation(State));
            return Task.FromResult(State);
        }

        public Task<PlayerHiveState?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken cancellationToken = default) => Task.FromResult<PlayerHiveState?>(State);
        public Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(new[] { State.HiveId });
        public Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(int limit, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlayerHiveState>>(new[] { State });
    }
}
