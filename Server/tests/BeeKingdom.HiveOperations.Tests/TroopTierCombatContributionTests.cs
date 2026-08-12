using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class TroopTierCombatContributionTests
{
    [Fact]
    public void Tier_one_or_missing_progress_yields_zero_bonus()
    {
        TroopTierCombatContribution atDefault = TroopTierCatalog.CombatContribution(null);
        TroopTierCombatContribution atTierOne = TroopTierCatalog.CombatContribution(new TroopTierState(new Dictionary<string, int> { ["guardians"] = 1 }));

        Assert.All(atDefault.PowerBonusBpByFamily.Values, bp => Assert.Equal(0, bp));
        Assert.All(atTierOne.PowerBonusBpByFamily.Values, bp => Assert.Equal(0, bp));
        Assert.Equal(1, atDefault.TierByFamily["guardians"]);
    }

    [Fact]
    public void Each_tier_above_one_adds_twenty_five_percent_to_its_own_family_only()
    {
        var progress = new TroopTierState(new Dictionary<string, int> { ["guardians"] = 3, ["wingrunners"] = 2 });

        TroopTierCombatContribution contribution = TroopTierCatalog.CombatContribution(progress);

        Assert.Equal(5000, contribution.PowerBonusBpByFamily["guardians"]); // tier 3 -> +50%
        Assert.Equal(2500, contribution.PowerBonusBpByFamily["wingrunners"]); // tier 2 -> +25%
        Assert.Equal(0, contribution.PowerBonusBpByFamily["darters"]); // untouched -> tier 1
        Assert.Equal(3, contribution.TierByFamily["guardians"]);
        Assert.Equal(2, contribution.TierByFamily["wingrunners"]);
        Assert.Equal(1, contribution.TierByFamily["darters"]);
    }

    [Fact]
    public void Tier_is_clamped_to_the_catalog_maximum()
    {
        var progress = new TroopTierState(new Dictionary<string, int> { ["guardians"] = 99 });

        TroopTierCombatContribution contribution = TroopTierCatalog.CombatContribution(progress);

        Assert.Equal(TroopTierCatalog.MaxTier, contribution.TierByFamily["guardians"]);
        Assert.Equal((TroopTierCatalog.MaxTier - 1) * TroopTierCatalog.PowerBonusBpPerTierAboveOne, contribution.PowerBonusBpByFamily["guardians"]);
    }

    [Fact]
    public void Non_combat_populations_are_not_part_of_the_contribution()
    {
        var progress = new TroopTierState(new Dictionary<string, int> { ["soldiers"] = 3, ["scouts"] = 3 });

        TroopTierCombatContribution contribution = TroopTierCatalog.CombatContribution(progress);

        Assert.All(contribution.PowerBonusBpByFamily.Values, bp => Assert.Equal(0, bp));
        Assert.Equal(3, contribution.PowerBonusBpByFamily.Count);
    }
}
