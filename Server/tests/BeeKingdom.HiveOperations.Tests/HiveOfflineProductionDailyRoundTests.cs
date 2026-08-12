using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class HiveOfflineProductionDailyRoundTests
{
    [Fact]
    public async Task CollectFlagControlsFreshFactAndReplayIsStable()
    {
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-22T12:00:00Z"));
        var options = new HiveOfflineProductionOptions { Enabled = true, CatalogVersion = "test-v1", MaxRecognizedDuration = TimeSpan.FromHours(2), Catalog = [
            new("honey_storage", "honey", 10m, 100), new("wax_workshop", "wax", 5m, 100), new("warehouse_cells", "pollen", 8m, 100)] };
        string root = Path.Combine(Path.GetTempPath(), "offline-daily-" + Guid.NewGuid());
        try
        {
            var factory = (Guid p, Guid h) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0,
                new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 100), ["wax"] = new(0, 100), ["pollen"] = new(0, 100) }, new(), [], new(),
                OfflineProduction: new HiveOfflineProductionState(clock.UtcNow.AddHours(1), new Dictionary<string, decimal> { ["honey_storage"] = 1.5m, ["wax_workshop"] = 0m, ["warehouse_cells"] = 0m }, 0, new()));
            var repo = new DurableJsonHiveStateRepository(root, factory);
            var enabled = new HiveOfflineProductionService(repo, clock, options, true);
            var request = new CollectOfflineProductionRequest(0, "offline-1");
            var first = await enabled.CollectAsync(player, hive, "honey_storage", request);
            Assert.True(first.Succeeded);
            var state = (await repo.ReadAsync(player, hive))!;
            Assert.True(state.DailyRound?.CollectionReceived);
            long revision = state.Revision;
            var replay = await enabled.CollectAsync(player, hive, "honey_storage", request);
            Assert.True(replay.Succeeded);
            var replayState = (await repo.ReadAsync(player, hive))!;
            Assert.Equal(revision, replayState.Revision);
            Assert.Equal(state.DailyRound, replayState.DailyRound);
            var offRepo = new DurableJsonHiveStateRepository(root + "-off", factory);
            var off = await new HiveOfflineProductionService(offRepo, clock, options, false).CollectAsync(player, hive, "honey_storage", request);
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
