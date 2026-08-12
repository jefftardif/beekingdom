using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class ChampionBeeCombatContributionTests
{
    [Fact]
    public void Assigned_role_matching_champion_adds_role_bonus_to_its_family_and_global_bonus_to_all()
    {
        var progress = new ChampionBeeProgressState(
            new Dictionary<string, int> { ["striga"] = 3 },
            new List<string> { "striga" });

        ChampionCombatContribution contribution = ChampionBeeCatalog.CombatContribution(progress);

        // striga: Guardians role, RoleStatBonusPercentPerLevel=3f, GlobalStatBonusPercentPerLevel=0.5f, level 3
        // role bonus = 3% * 3 = 9% = 900bp on guardians; global bonus = 0.5% * 3 = 1.5% = 150bp on all three families.
        Assert.Equal(1050, contribution.PowerBonusBpByFamily["guardians"]);
        Assert.Equal(150, contribution.PowerBonusBpByFamily["wingrunners"]);
        Assert.Equal(150, contribution.PowerBonusBpByFamily["darters"]);
        Assert.Contains("striga", contribution.ContributingBeeIds);
    }

    [Fact]
    public void Civilian_champion_with_zero_combat_bonuses_does_not_contribute()
    {
        var progress = new ChampionBeeProgressState(
            new Dictionary<string, int> { ["nectaria"] = 5 },
            new List<string> { "nectaria" });

        ChampionCombatContribution contribution = ChampionBeeCatalog.CombatContribution(progress);

        Assert.All(contribution.PowerBonusBpByFamily.Values, bp => Assert.Equal(0, bp));
        Assert.DoesNotContain("nectaria", contribution.ContributingBeeIds);
    }

    [Fact]
    public void Multiple_assigned_champions_stack_additively()
    {
        var progress = new ChampionBeeProgressState(
            new Dictionary<string, int> { ["striga"] = 1, ["zephyra"] = 1 },
            new List<string> { "striga", "zephyra" });

        ChampionCombatContribution contribution = ChampionBeeCatalog.CombatContribution(progress);

        // Each level-1: role bonus 300bp on its own family, global bonus 50bp each (100bp total) on all three.
        Assert.Equal(300 + 100, contribution.PowerBonusBpByFamily["guardians"]);
        Assert.Equal(300 + 100, contribution.PowerBonusBpByFamily["wingrunners"]);
        Assert.Equal(100, contribution.PowerBonusBpByFamily["darters"]);
        Assert.Equal(2, contribution.ContributingBeeIds.Count);
    }

    [Fact]
    public void No_assigned_champions_yields_zero_bonus_everywhere()
    {
        ChampionCombatContribution contribution = ChampionBeeCatalog.CombatContribution(null);

        Assert.All(contribution.PowerBonusBpByFamily.Values, bp => Assert.Equal(0, bp));
        Assert.Empty(contribution.ContributingBeeIds);
    }
}
