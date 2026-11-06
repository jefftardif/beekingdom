using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class FloralAllianceProjectRulesTests
{
    [Fact]
    public void Collective_pillars_are_default_disabled_and_require_distinct_members()
    {
        var options = new FloralAllianceProjectOptions();
        Assert.False(options.Enabled);
        Assert.True(options.RequiredDistinctMembers >= 2);
        Assert.True(options.MinimumPointsPerMember >= 1);
        options.Validate();
    }

    [Fact]
    public void Collective_drain_mode_preserves_delivery_processing_without_project_creation()
    {
        var opts = new FloralAllianceProjectOptions();
        Assert.False(opts.Enabled);
        Assert.False(opts.DrainExistingDeliveries);
        Assert.False(opts.CanApplyDeliveries);
        opts.DrainExistingDeliveries = true;
        Assert.True(opts.CanApplyDeliveries);
        opts.Validate();
        opts.DrainExistingDeliveries = false;
        opts.Enabled = true;
        Assert.True(opts.CanApplyDeliveries);
    }

    [Fact]
    public void Project_read_only_pilot_never_allows_creation_or_new_delivery_credit()
    {
        var options = new FloralAllianceProjectOptions();
        Assert.False(options.CanRead);
        Assert.False(options.CanCreate);
        options.ReadOnlyPilot = true;
        Assert.True(options.CanRead);
        Assert.False(options.CanCreate);
        Assert.False(options.CanApplyDeliveries);
        options.Enabled = true;
        Assert.True(options.CanRead);
        Assert.False(options.CanCreate);
        Assert.False(options.CanApplyDeliveries);
        options.DrainExistingDeliveries = true;
        Assert.True(options.CanApplyDeliveries);
        options.ReadOnlyPilot = false;
        Assert.True(options.CanCreate);
        options.Validate();
    }

    [Fact]
    public void Collective_level_depends_only_on_persisted_experience_thresholds()
    {
        var options = new FloralAllianceProjectOptions
        {
            GoalPoints = 20, RequiredDistinctMembers = 2,
            MinimumPointsPerMember = 5,
            LevelTwoExperience = 20, LevelThreeExperience = 100
        };
        Assert.Equal(1, options.Level(0));
        Assert.Equal(1, options.Level(19));
        Assert.Equal(2, options.Level(20));
        Assert.Equal(2, options.Level(99));
        Assert.Equal(3, options.Level(100));
        Assert.Equal(1, options.ActiveSlotsForLevel(1));
        Assert.Equal(2, options.ActiveSlotsForLevel(2));
    }

    [Theory]
    [InlineData(1, 2, 5)]
    [InlineData(20, 1, 5)]
    [InlineData(20, 2, 0)]
    public void Invalid_single_player_or_zero_contribution_rules_fail_closed(
        long goal, int required, long minimum)
    {
        var options = new FloralAllianceProjectOptions
        {
            GoalPoints = goal,
            RequiredDistinctMembers = required,
            MinimumPointsPerMember = minimum
        };
        Assert.Throws<InvalidDataException>(() => options.Validate());
    }
}
