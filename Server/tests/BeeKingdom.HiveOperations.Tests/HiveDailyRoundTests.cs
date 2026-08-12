using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class HiveDailyRoundTests
{
    [Fact]
    public async Task SnapshotReadMarksOncePerUtcDayAndAdvancesExactlyOnce()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        var clock = new MutableClock(DateTimeOffset.Parse("2026-07-22T10:00:00Z"));
        string root = Path.Combine(Path.GetTempPath(), "snapshot-round-" + Guid.NewGuid());
        try
        {
            var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0,
                new Dictionary<string, ResourceBalance> { ["honey"] = new(10, 100), ["pollen"] = new(5, 100) }, new(), [], new()));
            var service = new HiveOperationService(repo, clock, [], null, null, true);
            var first = await service.RecordSnapshotReadAsync(p, h);
            Assert.True(first.Succeeded);
            Assert.True(first.State.DailyRound?.SnapshotRead);
            Assert.Equal(1, first.State.Revision);
            var repeat = await service.RecordSnapshotReadAsync(p, h);
            Assert.True(repeat.Succeeded);
            Assert.Equal(1, repeat.State.Revision);
            Assert.Equal(first.State.DailyRound, repeat.State.DailyRound);
            clock.Now = clock.Now.AddDays(1);
            var next = await service.RecordSnapshotReadAsync(p, h);
            Assert.True(next.Succeeded);
            Assert.True(next.State.DailyRound?.SnapshotRead);
            Assert.Equal(2, next.State.Revision);
            Assert.NotEqual(first.State.DailyRound!.DayUtc, next.State.DailyRound!.DayUtc);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task VerifiedMilestonesClaimOnceAndReplay()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid(), op = Guid.NewGuid(), launch = Guid.NewGuid(); var clock = new Clock(DateTimeOffset.Parse("2026-07-22T12:00:00Z"));
        string root = Path.Combine(Path.GetTempPath(), "round-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0, new Dictionary<string, ResourceBalance> { ["honey"] = new(100, 1000), ["pollen"] = new(50, 500) }, new(), [new(op, "x", 0, 1, clock.UtcNow.AddSeconds(-20), clock.UtcNow.AddSeconds(-1), HiveOperationStatus.Collected, "honey", 0, clock.UtcNow.AddSeconds(-1)), new(launch, "y", 0, 1, clock.UtcNow.AddSeconds(-1), clock.UtcNow.AddSeconds(15), HiveOperationStatus.Running, "honey", 0, null)], new()));
        var service = new HiveOperationService(repo, clock, []);
        Assert.True((await service.RecordCollectionReceiptAsync(p, h, op)).Succeeded);
        Assert.False((await service.RecordOperationLaunchAsync(p, h, op)).Succeeded);
        Assert.True((await service.RecordOperationLaunchAsync(p, h, launch)).Succeeded);
        Assert.True((await service.RecordSnapshotReadAsync(p, h)).Succeeded);
        DailyRoundCommandResult claim = await service.ClaimDailyRoundAsync(new(p, h, 3, "claim-1", "2026-07-22"));
        Assert.True(claim.Succeeded); Assert.Equal("daily_round_claimed", claim.Code); Assert.Equal(220, claim.State.Resources["honey"].Amount);
        DailyRoundCommandResult replay = await service.ClaimDailyRoundAsync(new(p, h, 3, "claim-1", "2026-07-22"));
        Assert.Equal(claim.RevisionAfter, replay.RevisionAfter); Assert.Equal(claim.Code, replay.Code);
    }

    private sealed class Clock(DateTimeOffset value) : IServerClock { public DateTimeOffset UtcNow => value; }
    private sealed class MutableClock(DateTimeOffset value) : IServerClock { public DateTimeOffset Now { get; set; } = value; public DateTimeOffset UtcNow => Now; }
}
