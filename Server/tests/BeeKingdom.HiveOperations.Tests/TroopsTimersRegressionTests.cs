using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class TroopsTimersRegressionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "m097-" + Guid.NewGuid());
    private readonly Guid player = Guid.NewGuid();
    private readonly Guid hive = Guid.NewGuid();
    private readonly Clock clock = new();

    [Theory]
    [InlineData("guardians", 0, false)]
    [InlineData("wingrunners", 5, false)]
    [InlineData("darters", 60, false)]
    [InlineData("guardians", 60, true)]
    public async Task Training_survives_restart_speedup_and_claim_replay(string family, int seconds, bool universal)
    {
        var service = new CombatRecruitmentService(Repository(), clock);
        var command = new StartDoctrineTrainingCommand(player, hive, family, 0, "start");
        var started = await service.StartAsync(command, default);
        Assert.True(started.Succeeded);
        var definition = CombatRecruitmentService.Catalog[family];
        Assert.Equal(clock.UtcNow.AddSeconds(14), started.Snapshot.ActiveOperation!.EndsAtUtc);
        Assert.Equal(10000 - definition.HoneyCost, started.Snapshot.Balances["honey"].Amount);
        Assert.Equal("game.recruitment_not_complete", (await service.ClaimAsync(new(player, hive, started.Receipt!.OperationId, 1, "early"), default)).Code);
        Assert.False((await service.StartAsync(command with { ExpectedRevision = 1, IdempotencyKey = "busy" }, default)).Succeeded);
        service = new CombatRecruitmentService(Repository(), clock);
        Assert.Equal(started.Receipt, (await service.StartAsync(command, default)).Receipt);
        if (seconds > 0) await Accelerate("training", family, seconds, universal);
        var snapshot = await service.ReadAsync(player, hive, default);
        Assert.Equal(0, snapshot.Counts.GetValueOrDefault(family));
        Assert.Equal(clock.UtcNow.AddSeconds(Math.Max(0, 14 - seconds)), snapshot.ActiveOperation!.EndsAtUtc);
        clock.Now = snapshot.ActiveOperation.EndsAtUtc;
        service = new CombatRecruitmentService(Repository(), clock);
        Assert.Equal("awaiting_completion", (await service.ReadAsync(player, hive, default)).ActiveOperation!.Status);
        var claim = new ClaimDoctrineTrainingCommand(player, hive, started.Receipt.OperationId, snapshot.Revision, "claim");
        var completed = await service.ClaimAsync(claim, default);
        Assert.True(completed.Succeeded);
        Assert.Equal(definition.BatchSize, completed.Snapshot.Counts[family]);
        Assert.Null(completed.Snapshot.ActiveOperation);
        var replay = await new CombatRecruitmentService(Repository(), clock).ClaimAsync(claim, default);
        Assert.Equal(completed.Receipt, replay.Receipt);
        Assert.Equal(definition.BatchSize, replay.Snapshot.Counts[family]);
    }

    [Theory]
    [InlineData("feeding", 0, false)]
    [InlineData("stabilization", 0, false)]
    [InlineData("feeding", 5, false)]
    [InlineData("stabilization", 5, false)]
    [InlineData("feeding", 60, false)]
    [InlineData("stabilization", 60, true)]
    public async Task Healing_survives_restart_speedup_and_completion_replay(string type, int seconds, bool universal)
    {
        var service = new BroodVitalityCareService(Repository(), clock);
        var command = new BroodVitalityCareRequest(0, "start");
        var started = await service.StartAsync(player, hive, type, command);
        Assert.True(started.Succeeded);
        int duration = type == "feeding" ? 12 : 13;
        string resource = type == "feeding" ? "honey" : "wax";
        Assert.Equal(10000 - (type == "feeding" ? 300 : 45), started.State.Resources[resource].Amount);
        Assert.Equal(clock.UtcNow.AddSeconds(duration), started.State.BroodVitality!.ActiveOperation!.EndsAtUtc);
        Assert.Equal("game.vitality_not_ready", (await service.CompleteAsync(player, hive, started.Receipt!.OperationId, new(1, "early"))).Code);
        Assert.Equal("game.vitality_busy", (await service.StartAsync(player, hive, type, new(1, "busy"))).Code);
        Assert.Equal(started.Receipt, (await new BroodVitalityCareService(Repository(), clock).StartAsync(player, hive, type, command)).Receipt);
        if (seconds > 0) await Accelerate("healing", type, seconds, universal);
        var persisted = (await Repository().ReadAsync(player, hive))!;
        Assert.Equal(50, persisted.BroodVitality!.Nutrition);
        Assert.Equal(50, persisted.BroodVitality.Stability);
        Assert.Equal(clock.UtcNow.AddSeconds(Math.Max(0, duration - seconds)), persisted.BroodVitality.ActiveOperation!.EndsAtUtc);
        clock.Now = persisted.BroodVitality.ActiveOperation.EndsAtUtc;
        service = new BroodVitalityCareService(Repository(), clock);
        var complete = new BroodVitalityCareRequest(persisted.Revision, "complete");
        var result = await service.CompleteAsync(player, hive, started.Receipt.OperationId, complete);
        Assert.True(result.Succeeded);
        Assert.Null(result.State.BroodVitality!.ActiveOperation);
        Assert.Equal(type == "feeding" ? 72 : 50, result.State.BroodVitality.Nutrition);
        Assert.Equal(type == "stabilization" ? 57 : 50, result.State.BroodVitality.Stability);
        var replay = await new BroodVitalityCareService(Repository(), clock).CompleteAsync(player, hive, started.Receipt.OperationId, complete);
        Assert.Equal(result.Receipt, replay.Receipt);
        Assert.Equal(result.State.BroodVitality, replay.State.BroodVitality);
    }

    [Theory]
    [InlineData("training", "guardians", false)]
    [InlineData("training", "guardians", true)]
    [InlineData("healing", "feeding", false)]
    [InlineData("healing", "stabilization", true)]
    public async Task Expired_timer_does_not_consume_speedup(string category, string target, bool universal)
    {
        if (category == "training")
            await new CombatRecruitmentService(Repository(), clock).StartAsync(new(player, hive, target, 0, "start"), default);
        else
            await new BroodVitalityCareService(Repository(), clock).StartAsync(player, hive, target, new(0, "start"));
        clock.Now = clock.Now.AddSeconds(14);
        var item = universal ? "universal_60s" : category + "_60s";
        var result = await new SpeedUpInventoryService(Repository(), clock, Options()).ApplyAsync(player, hive, new(item, category, target, 60, 1, "expired"));
        Assert.False(result.Succeeded);
        var persisted = (await Repository().ReadAsync(player, hive))!;
        Assert.Equal(2, persisted.SpeedUps![item]);
        Assert.Equal(1, persisted.Revision);
        Assert.Equal("awaiting_completion", Assert.Single(result.Snapshot!.Timers).Status);
    }

    [Theory]
    [InlineData("guardians")]
    [InlineData("wingrunners")]
    [InlineData("darters")]
    public async Task Promotions_are_durable_idempotent_and_add_25_percent_per_tier(string family)
    {
        for (int tier = 1; tier < 3; tier++)
        {
            var service = new HiveOperationService(Repository(), clock, []);
            var command = new PromoteTroopTierCommand(player, hive, family, tier - 1, "promote-" + tier);
            var promoted = await service.PromoteTroopTierAsync(command);
            Assert.True(promoted.Succeeded);
            var replay = await new HiveOperationService(Repository(), clock, []).PromoteTroopTierAsync(command);
            Assert.Equal(promoted.RevisionAfter, replay.RevisionAfter);
            var persisted = (await Repository().ReadAsync(player, hive))!;
            Assert.Equal(10000 - 200 * tier * (tier + 1) / 2, persisted.Resources["honey"].Amount);
            Assert.Equal(10000 - 80 * tier * (tier + 1) / 2, persisted.Resources["pollen"].Amount);
            var contribution = TroopTierCatalog.CombatContribution(persisted.TroopTierProgress);
            Assert.Equal(tier * 2500, contribution.PowerBonusBpByFamily[family]);
            Assert.All(contribution.PowerBonusBpByFamily.Where(x => x.Key != family), x => Assert.Equal(0, x.Value));
        }
        var capped = await new HiveOperationService(Repository(), clock, []).PromoteTroopTierAsync(new(player, hive, family, 2, "max"));
        Assert.Equal("troop_tier_max", capped.Code);
        Assert.Equal(2, capped.State.Revision);
    }

    private async Task Accelerate(string category, string target, int seconds, bool universal)
    {
        string item = (universal ? "universal" : category) + "_" + seconds + "s";
        var request = new ApplySpeedUpRequest(item, category, target, seconds, 1, "speedup");
        var result = await new SpeedUpInventoryService(Repository(), clock, Options()).ApplyAsync(player, hive, request);
        Assert.True(result.Succeeded);
        Assert.Equal(seconds >= 14 ? "awaiting_completion" : "running", Assert.Single(result.Snapshot!.Timers).Status);
        var replay = await new SpeedUpInventoryService(Repository(), clock, Options()).ApplyAsync(player, hive, request);
        Assert.True(replay.Succeeded);
        Assert.NotNull(replay.Response);
        Assert.Equal(result.Response!.Receipt, replay.Response!.Receipt);
        Assert.Equal(1, (await Repository().ReadAsync(player, hive))!.SpeedUps![item]);
    }

    private DurableJsonHiveStateRepository Repository() => new(root, (p, h) => new PlayerHiveState(
        p, h, HiveStateMigrator.CurrentModelVersion, 0,
        new() { ["honey"] = new(10000, 20000), ["pollen"] = new(10000, 20000), ["wax"] = new(10000, 20000) },
        new() { ["guard_post"] = 1 }, [], new(),
        BroodVitality: new(50, 50, 0, clock.UtcNow, null),
        SpeedUps: Options().Catalog.ToDictionary(x => x.ItemId, _ => 2)));

    private static SpeedUpOptions Options() => new()
    {
        Enabled = true,
        Catalog = [new("training_5s", "training", 5), new("training_60s", "training", 60),
            new("healing_5s", "healing", 5), new("healing_60s", "healing", 60), new("universal_60s", "universal", 60)]
    };

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }

    private sealed class Clock : IServerClock
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
    }
}
