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
}
