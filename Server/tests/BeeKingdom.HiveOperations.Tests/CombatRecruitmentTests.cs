using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class CombatRecruitmentTests
{
    [Fact]
    public void Migration_v6_without_roster_stays_not_recorded()
    {
        var state = new PlayerHiveState(Guid.NewGuid(), Guid.NewGuid(), 6, 0, new(), new(), [], new());
        Assert.Null(HiveStateMigrator.ToCurrent(state).DoctrineRoster);
        Assert.Equal(HiveStateMigrator.CurrentModelVersion, HiveStateMigrator.ToCurrent(state).ModelVersion);
    }

    [Fact]
    public void Migration_preserves_registered_roster()
    {
        var roster = new DoctrineRosterState(2, new Dictionary<string, long> { ["guardians"] = 4, ["wingrunners"] = 0, ["darters"] = 0 }, null, new());
        var state = new PlayerHiveState(Guid.NewGuid(), Guid.NewGuid(), 6, 2, new(), new(), [], new(), DoctrineRoster: roster);
        var migrated = HiveStateMigrator.ToCurrent(state);
        Assert.Equal(roster, migrated.DoctrineRoster);
    }

    [Fact]
    public void Migration_adds_the_initial_guard_post_for_first_training()
    {
        var state = new PlayerHiveState(Guid.NewGuid(), Guid.NewGuid(), 10, 0, new(), new(), [], new());

        var migrated = HiveStateMigrator.ToCurrent(state);

        Assert.Equal(1, migrated.BuildingLevels["guard_post"]);
    }

    [Fact]
    public void Migration_rejects_count_above_bound_and_falsified_batch()
    {
        var tooMany = new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = 1_000_000_001 }, null, new());
        var state = new PlayerHiveState(Guid.NewGuid(), Guid.NewGuid(), 7, 0, new(), new(), [], new(), DoctrineRoster: tooMany);
        Assert.Throws<InvalidOperationException>(() => HiveStateMigrator.ToCurrent(state));
        var operation = new DoctrineTrainingOperation(Guid.NewGuid(), "guardians", 99, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddSeconds(14), 0, "k", "hash", false);
        var invalid = state with { DoctrineRoster = new DoctrineRosterState(0, new(), operation, new()) };
        Assert.Throws<InvalidOperationException>(() => HiveStateMigrator.ToCurrent(invalid));
    }

    [Fact]
    public void Catalog_has_exact_bounded_preview_recipes()
    {
        Assert.Equal(new[] { "guardians", "wingrunners", "darters" }, CombatRecruitmentService.Catalog.Keys);
        Assert.Equal(4, CombatRecruitmentService.Catalog["guardians"].BatchSize);
        Assert.Equal(6, CombatRecruitmentService.Catalog["wingrunners"].BatchSize);
        Assert.Equal(8, CombatRecruitmentService.Catalog["darters"].BatchSize);
        Assert.All(CombatRecruitmentService.Catalog.Values, d => Assert.Equal(TimeSpan.FromSeconds(14), d.Duration));
    }

    [Fact]
    public void ComputePopulationCapacity_grows_with_nursery_level_and_caps_at_max()
    {
        Assert.Equal(CombatRecruitmentService.InitialPopulationCapacity, CombatRecruitmentService.ComputePopulationCapacity(new Dictionary<string, int>()));
        Assert.Equal(
            CombatRecruitmentService.InitialPopulationCapacity + 5 * CombatRecruitmentService.PopulationCapacityPerNurseryLevel,
            CombatRecruitmentService.ComputePopulationCapacity(new Dictionary<string, int> { ["nursery_cluster"] = 5 }));
        Assert.Equal(
            CombatRecruitmentService.MaxPopulationCapacity,
            CombatRecruitmentService.ComputePopulationCapacity(new Dictionary<string, int> { ["nursery_cluster"] = 1_000_000 }));
    }

    [Fact]
    public async Task StartAsync_blocks_when_population_capacity_would_be_exceeded()
    {
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-08-19T12:00:00Z"));
        string root = Path.Combine(Path.GetTempPath(), "recruitment-capacity-" + Guid.NewGuid());
        try
        {
            var factory = (Guid p, Guid h) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0,
                new Dictionary<string, ResourceBalance> { ["honey"] = new(10_000, 20_000), ["pollen"] = new(10_000, 20_000) },
                new Dictionary<string, int> { ["guard_post"] = 1, ["nursery_cluster"] = 0 }, [], new(),
                DoctrineRoster: new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = 18 }, null, new()));
            var repo = new DurableJsonHiveStateRepository(root, factory);
            var service = new CombatRecruitmentService(repo, clock);

            var result = await service.StartAsync(new StartDoctrineTrainingCommand(player, hive, "guardians", 0, "cap-1"), CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal("game.population_capacity_exceeded", result.Code);
            Assert.Null((await repo.ReadAsync(player, hive))!.DoctrineRoster!.ActiveOperation);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task StartAsync_allows_training_once_nursery_level_raises_capacity_enough()
    {
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-08-19T12:00:00Z"));
        string root = Path.Combine(Path.GetTempPath(), "recruitment-capacity-" + Guid.NewGuid());
        try
        {
            var factory = (Guid p, Guid h) => new PlayerHiveState(p, h, HiveStateMigrator.CurrentModelVersion, 0,
                new Dictionary<string, ResourceBalance> { ["honey"] = new(10_000, 20_000), ["pollen"] = new(10_000, 20_000) },
                new Dictionary<string, int> { ["guard_post"] = 1, ["nursery_cluster"] = 5 },
                [], new(),
                DoctrineRoster: new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = 18 }, null, new()));
            var repo = new DurableJsonHiveStateRepository(root, factory);
            var service = new CombatRecruitmentService(repo, clock);

            var result = await service.StartAsync(new StartDoctrineTrainingCommand(player, hive, "guardians", 0, "cap-2"), CancellationToken.None);

            Assert.True(result.Succeeded);
            Assert.NotNull((await repo.ReadAsync(player, hive))!.DoctrineRoster!.ActiveOperation);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private sealed class FixedClock(DateTimeOffset value) : IServerClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }
}
