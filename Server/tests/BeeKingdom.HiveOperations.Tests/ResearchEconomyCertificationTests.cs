using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class ResearchEconomyCertificationTests
{
    // Independent acceptance values from BeeKingdom_ResearchTree_Design, economy branch.
    public static TheoryData<string, long, long, long, int, string, ResearchEffects> Economy => new()
    {
        { "foraging_routes_i", 240, 0, 90, 120, "", new(200, 0, 0, 0, 0, 0) },
        { "foraging_routes_ii", 900, 0, 500, 360, "foraging_routes_i", new(500, 0, 0, 0, 0, 0) },
        { "foraging_routes_iii", 2400, 0, 1400, 720, "foraging_routes_ii", new(800, 0, 0, 0, 0, 0) },
        { "tempered_combs_i", 180, 0, 120, 120, "", new(0, 500, 0, 0, 0, 0) },
        { "tempered_combs_ii", 900, 0, 500, 360, "tempered_combs_i", new(0, 800, 300, 0, 0, 0) },
        { "tempered_combs_iii", 2400, 0, 1400, 720, "tempered_combs_ii", new(0, 1000, 500, 0, 0, 0) },
        { "pollen_sorting_i", 200, 150, 0, 120, "", new(0, 0, 0, 500, 0, 0) },
        { "pollen_sorting_ii", 800, 600, 0, 360, "pollen_sorting_i", new(0, 0, 0, 800, 0, 0) },
        { "pollen_sorting_iii", 2200, 1600, 0, 720, "pollen_sorting_ii", new(0, 0, 0, 1000, 500, 0) },
        { "sealed_reserves", 6000, 4000, 4000, 1200, "foraging_routes_iii,tempered_combs_iii,pollen_sorting_iii", new(0, 0, 0, 0, 0, 1000) }
    };

    [Theory]
    [MemberData(nameof(Economy))]
    public async Task CatalogCostsDependenciesAndDurableLifecycle(string id, long honey, long wax, long pollen,
        int seconds, string dependencies, ResearchEffects effects)
    {
        Assert.Equal(10, HiveOperationService.ResearchCatalog.Count);
        ResearchCatalogEntry offer = HiveOperationService.ResearchCatalog[id];
        string[] prerequisites = dependencies.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var costs = new Dictionary<string, long> { ["honey"] = honey, ["wax"] = wax, ["pollen"] = pollen };
        Assert.Equal(TimeSpan.FromSeconds(seconds), offer.Duration);
        Assert.Equal(effects, offer.Effects);
        Assert.Equal(prerequisites, offer.Prerequisites);
        Assert.Equal(costs.Count(pair => pair.Value > 0), offer.Costs.Count);
        foreach (var cost in costs) Assert.Equal(cost.Value, offer.Costs.GetValueOrDefault(cost.Key));

        // Every dependency matters, including each of the three convergence prerequisites.
        foreach (string missing in prerequisites)
        {
            using var blocked = new Harness(prerequisites.Where(value => value != missing));
            var result = await blocked.Service.StartResearchAsync(new(blocked.Player, blocked.Hive, id, 0, "blocked"));
            Assert.False(result.Succeeded);
            Assert.Equal("research_prerequisite_missing", result.Code);
            Assert.Equal(0, result.State.Revision);
            Assert.Null(result.State.Research!.ActiveOperation);
            Assert.All(result.State.Resources.Values, balance => Assert.Equal(100000, balance.Amount));
        }

        foreach (var cost in offer.Costs)
        {
            using var poor = new Harness(prerequisites);
            await poor.Repository.ExecuteAtomicallyAsync(poor.Player, poor.Hive, state => state with
            {
                Resources = new(state.Resources) { [cost.Key] = new(cost.Value - 1, 100000) }
            });
            var result = await poor.Service.StartResearchAsync(new(poor.Player, poor.Hive, id, 0, "poor"));
            Assert.Equal("insufficient_resources", result.Code);
            Assert.Equal(0, result.State.Revision);
            Assert.Null(result.State.Research!.ActiveOperation);
            Assert.Equal(cost.Value - 1, result.State.Resources[cost.Key].Amount);
        }

        using var h = new Harness(prerequisites);
        var command = new StartResearchCommand(h.Player, h.Hive, id, 0, "start");
        var started = await h.Service.StartResearchAsync(command);
        Assert.True(started.Succeeded);
        Assert.Equal(1, started.State.Revision);
        Assert.Equal(h.Clock.UtcNow.AddSeconds(seconds), started.State.Research!.ActiveOperation!.EndsAtUtc);
        foreach (var cost in costs) Assert.Equal(100000 - cost.Value, started.State.Resources[cost.Key].Amount);
        h.Reconnect();
        var replay = await h.Service.StartResearchAsync(command);
        Assert.Equal(started.OperationId, replay.OperationId);
        Assert.Equal(1, replay.State.Revision);
        foreach (var cost in costs) Assert.Equal(100000 - cost.Value, replay.State.Resources[cost.Key].Amount);
        Assert.Equal("research_busy", (await h.Service.StartResearchAsync(new(h.Player, h.Hive, id, 1, "busy"))).Code);
        h.Clock.Now = h.Clock.Now.AddSeconds(seconds).AddTicks(-1);
        Assert.Equal("research_not_ready", (await h.Service.CompleteResearchAsync(new(h.Player, h.Hive, started.OperationId!.Value, 1, "early"))).Code);
        h.Clock.Now = h.Clock.Now.AddTicks(1);
        h.Reconnect();
        var completion = new CompleteResearchCommand(h.Player, h.Hive, started.OperationId!.Value, 1, "complete");
        var done = await h.Service.CompleteResearchAsync(completion);
        Assert.True(done.Succeeded);
        Assert.Equal(2, done.State.Revision);
        Assert.Null(done.State.Research!.ActiveOperation);
        Assert.Equal(effects, done.State.Research.Completed[id].Effects);
        h.Reconnect();
        var doneReplay = await h.Service.CompleteResearchAsync(completion);
        Assert.True(doneReplay.Succeeded);
        Assert.Equal(2, doneReplay.State.Revision);
        Assert.Equal(prerequisites.Length + 1, doneReplay.State.Research!.Completed.Count);
        Assert.Equal("research_already_completed", (await h.Service.StartResearchAsync(new(h.Player, h.Hive, id, 2, "again"))).Code);
    }

    [Fact]
    public async Task HoneyTwoPercentIsAppliedToRealProductionOnlyAfterCompletion()
    {
        using var h = new Harness();
        var started = await h.Service.StartResearchAsync(new(h.Player, h.Hive, "foraging_routes_i", 0, "start"));
        var before = await h.Production.ReadSnapshotAsync(h.Player, h.Hive);
        Assert.Equal(100m, before.Lines.Single(line => line.ResourceKey == "honey").HourlyRate);
        h.Clock.Now = h.Clock.Now.AddSeconds(120);
        var state = (await h.Repository.ReadAsync(h.Player, h.Hive))!;
        var done = await h.Service.CompleteResearchAsync(new(h.Player, h.Hive, started.OperationId!.Value, state.Revision, "complete"));
        Assert.True(done.Succeeded);
        var initial = await h.Production.ReadSnapshotAsync(h.Player, h.Hive);
        h.Clock.Now = h.Clock.Now.AddHours(1);
        h.Reconnect();
        var after = await h.Production.ReadSnapshotAsync(h.Player, h.Hive);
        Assert.Equal(102m, after.Lines.Single(line => line.ResourceKey == "honey").HourlyRate);
        Assert.Equal(102m, after.Lines.Single(line => line.ResourceKey == "honey").PendingAmount
            - initial.Lines.Single(line => line.ResourceKey == "honey").PendingAmount);
        Assert.Equal(100m, after.Lines.Single(line => line.ResourceKey == "wax").HourlyRate);
        Assert.Equal(100m, after.Lines.Single(line => line.ResourceKey == "pollen").HourlyRate);
        Assert.Equal(99760, after.Balances["honey"].Amount); // Production still requires collection.
    }

    [Fact]
    public async Task AllTenCanCompleteInSequenceAndBonusesAccumulateAfterReconnect()
    {
        using var h = new Harness();
        long revision = 0;
        foreach (object[] row in Economy)
        {
            string id = (string)row[0];
            var started = await h.Service.StartResearchAsync(new(h.Player, h.Hive, id, revision, "start-" + id));
            Assert.True(started.Succeeded, started.Code);
            h.Clock.Now = h.Clock.Now.AddSeconds((int)row[4]);
            var done = await h.Service.CompleteResearchAsync(new(h.Player, h.Hive, started.OperationId!.Value, started.RevisionAfter, "done-" + id));
            Assert.True(done.Succeeded, done.Code);
            revision = done.RevisionAfter;
            h.Reconnect();
        }
        var state = (await h.Repository.ReadAsync(h.Player, h.Hive))!;
        Assert.Equal(10, state.Research!.Completed.Count);
        var snapshot = await h.Production.ReadSnapshotAsync(h.Player, h.Hive);
        Assert.Equal(115m, snapshot.Lines.Single(line => line.ResourceKey == "honey").HourlyRate);
        Assert.Equal(108m, snapshot.Lines.Single(line => line.ResourceKey == "wax").HourlyRate);
        Assert.Equal(123m, snapshot.Lines.Single(line => line.ResourceKey == "pollen").HourlyRate);
        Assert.Equal(1100, snapshot.Lines.Single(line => line.ResourceKey == "honey").Capacity);
        Assert.Equal(1330, snapshot.Lines.Single(line => line.ResourceKey == "wax").Capacity);
        Assert.Equal(1150, snapshot.Lines.Single(line => line.ResourceKey == "pollen").Capacity);
    }

    [Theory]
    [InlineData("research")]
    [InlineData("universal")]
    public async Task ResearchSpeedUpPersistsClampsAndReplaysWithoutDoubleConsumption(string category)
    {
        using var h = new Harness();
        var started = await h.Service.StartResearchAsync(new(h.Player, h.Hive, "foraging_routes_i", 0, "start"));
        var request = new ApplySpeedUpRequest(category + "_60s", category, "foraging_routes_i", 60, 1, "speed");
        var first = await h.SpeedUps.ApplyAsync(h.Player, h.Hive, request);
        Assert.True(first.Succeeded);
        Assert.Equal(h.Clock.UtcNow.AddSeconds(60), first.Snapshot.Timers.Single().CompletesAtUtc);
        h.Reconnect();
        var replay = await h.SpeedUps.ApplyAsync(h.Player, h.Hive, request);
        Assert.True(replay.Succeeded);
        Assert.Equal(2, replay.Snapshot.Revision);
        Assert.NotNull(replay.Response);
        Assert.Equal(first.Response!.Receipt, replay.Response!.Receipt);
        Assert.Equal(2, replay.Snapshot.Inventory.Items[category + "_60s"]);
        var stale = await h.SpeedUps.ApplyAsync(h.Player, h.Hive, request with { IdempotencyKey = "stale" });
        Assert.Equal("revision_conflict", stale.Code);
        var wrong = await h.SpeedUps.ApplyAsync(h.Player, h.Hive, request with { ExpectedRevision = 2, TargetId = "tempered_combs_i", IdempotencyKey = "wrong" });
        Assert.Equal("timer_not_found", wrong.Code);
        h.Clock.Now = h.Clock.Now.AddSeconds(1);
        var final = await h.SpeedUps.ApplyAsync(h.Player, h.Hive, request with { ExpectedRevision = 2, IdempotencyKey = "finish" });
        Assert.Equal("speedup_applied_completed", final.Code);
        Assert.Equal(h.Clock.UtcNow, final.Snapshot.Timers.Single().CompletesAtUtc);
        Assert.Equal(1, final.Snapshot.Inventory.Items[category + "_60s"]);
        h.Reconnect();
        var done = await h.Service.CompleteResearchAsync(new(h.Player, h.Hive, started.OperationId!.Value, 3, "complete"));
        Assert.True(done.Succeeded);
        Assert.Equal(200, done.State.Research!.Completed["foraging_routes_i"].Effects.HoneyProductionBonusBps);
        var lateReplay = await h.SpeedUps.ApplyAsync(h.Player, h.Hive, request);
        Assert.Equal(first.Response.Receipt, lateReplay.Response!.Receipt);
        Assert.Equal(4, lateReplay.Response.Snapshot.Revision);
        Assert.Empty(lateReplay.Response.Snapshot.Timers);
    }

    [Theory]
    [InlineData("research")]
    [InlineData("universal")]
    public async Task ReadyResearchDoesNotConsumeSpeedUp(string category)
    {
        using var h = new Harness();
        await h.Service.StartResearchAsync(new(h.Player, h.Hive, "foraging_routes_i", 0, "start"));
        h.Clock.Now = h.Clock.Now.AddSeconds(120);
        var result = await h.SpeedUps.ApplyAsync(h.Player, h.Hive,
            new(category + "_60s", category, "foraging_routes_i", 60, 1, "too-late"));
        Assert.False(result.Succeeded);
        Assert.Equal("timer_not_found", result.Code);
        Assert.Equal(1, result.Snapshot.Revision);
        Assert.Equal(3, result.Snapshot.Inventory.Items[category + "_60s"]);
    }

    private sealed class Harness : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "m096-" + Guid.NewGuid());
        private readonly string[] prerequisites;
        public Guid Player { get; } = Guid.NewGuid();
        public Guid Hive { get; } = Guid.NewGuid();
        public TestClock Clock { get; } = new();
        public DurableJsonHiveStateRepository Repository { get; private set; } = null!;
        public HiveOperationService Service => new(Repository, Clock, []);
        public SpeedUpInventoryService SpeedUps => new(Repository, Clock, new() { Enabled = true });
        public HiveOfflineProductionService Production => new(Repository, Clock, new()
        {
            Enabled = true, CatalogVersion = "m096-v1", Catalog =
            [new("honey_storage", "honey", 100, 1000), new("wax_workshop", "wax", 100, 1000), new("warehouse_cells", "pollen", 100, 1000)]
        });

        public Harness(IEnumerable<string>? completed = null)
        {
            prerequisites = completed?.ToArray() ?? [];
            Reconnect();
        }

        public void Reconnect() => Repository = new(root, (player, hive) => new(player, hive,
            HiveStateMigrator.CurrentModelVersion, 0,
            new() { ["honey"] = new(100000, 100000), ["wax"] = new(100000, 100000), ["pollen"] = new(100000, 100000) },
            new(), [], new(), Research: new(prerequisites.ToDictionary(id => id,
                id => new ResearchCompletion(id, Clock.UtcNow, HiveOperationService.ResearchCatalog[id].Effects)), null),
            SpeedUps: new() { ["research_60s"] = 3, ["universal_60s"] = 3 }));

        public void Dispose()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private sealed class TestClock : IServerClock
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.Parse("2026-09-26T12:00:00Z");
        public DateTimeOffset UtcNow => Now;
    }
}
