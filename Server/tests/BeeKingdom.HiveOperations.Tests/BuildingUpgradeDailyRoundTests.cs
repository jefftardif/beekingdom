using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class BuildingUpgradeDailyRoundTests
{
    [Fact]
    public async Task StartFlagControlsFreshFactAndReplayIsStable()
    {
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-22T12:00:00Z"));
        var options = new BuildingUpgradeOptions { Enabled = true, CatalogVersion = "test-v1", Catalog = [new("wax_workshop", 1, 2, TimeSpan.FromSeconds(10), new Dictionary<string, long> { ["honey"] = 10, ["pollen"] = 5 })] };
        string root = Path.Combine(Path.GetTempPath(), "building-daily-" + Guid.NewGuid());
        try
        {
            var factory = (Guid p, Guid h) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0,
                new Dictionary<string, ResourceBalance> { ["honey"] = new(100, 200), ["pollen"] = new(100, 200) },
                new Dictionary<string, int> { ["wax_workshop"] = 1 }, [], new());
            var repo = new DurableJsonHiveStateRepository(root, factory);
            var cmd = new StartBuildingUpgradeRequest(0, "b1");
            var enabled = new BuildingUpgradeService(repo, clock, options, true);
            var first = await enabled.StartAsync(player, hive, "wax_workshop", cmd);
            Assert.True(first.Succeeded);
            var firstState = (await repo.ReadAsync(player, hive))!;
            Assert.True(firstState.DailyRound?.OperationLaunched);
            Assert.Equal(1, firstState.Revision);
            var replay = await enabled.StartAsync(player, hive, "wax_workshop", cmd);
            Assert.True(replay.Succeeded);
            var replayState = (await repo.ReadAsync(player, hive))!;
            Assert.Equal(firstState.Revision, replayState.Revision);
            Assert.Equal(firstState.DailyRound, replayState.DailyRound);

            var offRepo = new DurableJsonHiveStateRepository(root + "-off", factory);
            var off = await new BuildingUpgradeService(offRepo, clock, options, false).StartAsync(player, hive, "wax_workshop", cmd);
            Assert.True(off.Succeeded);
            Assert.Null((await offRepo.ReadAsync(player, hive))!.DailyRound);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); if (Directory.Exists(root + "-off")) Directory.Delete(root + "-off", true); }
    }

    private sealed class FixedClock(DateTimeOffset value) : IServerClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }
}
