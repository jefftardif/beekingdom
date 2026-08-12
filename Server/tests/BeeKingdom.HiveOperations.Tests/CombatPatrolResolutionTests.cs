using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class CombatPatrolResolutionTests
{
    [Fact]
    public void Neutral_composition_at_exact_required_power_yields_victory_band()
    {
        BestiaryTierDefinition tier = CombatPatrolCatalog.Tiers[2]; // guardians hazard, required=90
        var squad = new Dictionary<string, long> { ["guardians"] = 18, ["wingrunners"] = 0, ["darters"] = 0 };

        CombatPatrolResolutionResult result = CombatPatrolResolution.Resolve(squad, tier);

        Assert.Equal(90, result.AvailablePower);
        Assert.Equal(10000, result.ReadinessBp);
        Assert.Equal(CombatPatrolOutcomeBand.Victory, result.Band);
        Assert.Equal(tier.HoneyReward, result.HoneyCredited);
        Assert.Equal(tier.PollenReward, result.PollenCredited);
    }

    [Fact]
    public void Advantaged_family_beats_neutral_family_for_the_same_headcount()
    {
        BestiaryTierDefinition tier = CombatPatrolCatalog.Tiers[2]; // guardians hazard; wingrunners counter guardians
        var advantaged = new Dictionary<string, long> { ["wingrunners"] = 18 };
        var neutral = new Dictionary<string, long> { ["guardians"] = 18 };

        CombatPatrolResolutionResult advantagedResult = CombatPatrolResolution.Resolve(advantaged, tier);
        CombatPatrolResolutionResult neutralResult = CombatPatrolResolution.Resolve(neutral, tier);

        Assert.True(advantagedResult.AvailablePower > neutralResult.AvailablePower);
        Assert.Equal(CombatPatrolOutcomeBand.DecisiveVictory, advantagedResult.Band);
        Assert.Equal(CombatPatrolOutcomeBand.Victory, neutralResult.Band);
        Assert.Equal(0, advantagedResult.PermanentLosses["wingrunners"]);
        Assert.Equal(0, advantagedResult.WoundedLosses["wingrunners"]);
    }

    [Fact]
    public void Disadvantaged_family_fares_worse_than_neutral_family_for_the_same_headcount()
    {
        BestiaryTierDefinition tier = CombatPatrolCatalog.Tiers[2]; // guardians hazard; guardians counter darters
        var disadvantaged = new Dictionary<string, long> { ["darters"] = 18 };
        var neutral = new Dictionary<string, long> { ["guardians"] = 18 };

        CombatPatrolResolutionResult disadvantagedResult = CombatPatrolResolution.Resolve(disadvantaged, tier);
        CombatPatrolResolutionResult neutralResult = CombatPatrolResolution.Resolve(neutral, tier);

        Assert.True(disadvantagedResult.AvailablePower < neutralResult.AvailablePower);
        Assert.Equal(CombatPatrolOutcomeBand.HardWon, disadvantagedResult.Band);
        long totalLoss = disadvantagedResult.PermanentLosses["darters"] + disadvantagedResult.WoundedLosses["darters"];
        Assert.True(totalLoss > 0);
    }

    [Fact]
    public void Most_losses_are_wounded_and_recoverable_only_a_small_share_is_permanent()
    {
        BestiaryTierDefinition tier = CombatPatrolCatalog.Tiers[6]; // darters hazard, required=600
        var disadvantaged = new Dictionary<string, long> { ["wingrunners"] = 140 }; // disadvantaged vs darters-styled hazard, HardWon band

        CombatPatrolResolutionResult result = CombatPatrolResolution.Resolve(disadvantaged, tier);

        Assert.Equal(CombatPatrolOutcomeBand.HardWon, result.Band);
        long permanent = result.PermanentLosses["wingrunners"];
        long wounded = result.WoundedLosses["wingrunners"];
        Assert.True(wounded > 0);
        Assert.True(permanent > 0);
        // Permanent share is small (~15%): wounded losses must clearly dominate the total.
        Assert.True(permanent < wounded);
        Assert.True(permanent <= (permanent + wounded) * CombatPatrolResolution.PermanentLossShareBp / 10000 + 1);
    }

    [Fact]
    public void Recovery_duration_scales_with_patrol_duration_and_is_longer_for_harder_tiers()
    {
        TimeSpan t1Recovery = CombatPatrolResolution.ComputeRecoveryDuration(CombatPatrolCatalog.Tiers[1]);
        TimeSpan t7Recovery = CombatPatrolResolution.ComputeRecoveryDuration(CombatPatrolCatalog.Tiers[7]);

        Assert.True(t1Recovery > TimeSpan.Zero);
        Assert.True(t7Recovery > t1Recovery);
    }

    [Fact]
    public void Readiness_below_blocked_threshold_yields_zero_loss_zero_reward_blocked_band()
    {
        BestiaryTierDefinition tier = CombatPatrolCatalog.Tiers[3]; // required=160
        var tooWeak = new Dictionary<string, long> { ["guardians"] = 5 }; // power=25, readiness far below 7000bp

        CombatPatrolResolutionResult result = CombatPatrolResolution.Resolve(tooWeak, tier);

        Assert.Equal(CombatPatrolOutcomeBand.Blocked, result.Band);
        Assert.False(CombatPatrolResolution.CanLaunch(result.ReadinessBp));
        Assert.All(result.PermanentLosses.Values, loss => Assert.Equal(0, loss));
        Assert.All(result.WoundedLosses.Values, loss => Assert.Equal(0, loss));
        Assert.Equal(0, result.HoneyCredited);
        Assert.Equal(0, result.PollenCredited);
    }

    [Fact]
    public void Overwhelming_power_yields_decisive_victory_with_zero_losses_and_bonus_reward()
    {
        BestiaryTierDefinition tier = CombatPatrolCatalog.Tiers[1]; // required=40, wingrunners hazard
        var squad = new Dictionary<string, long> { ["guardians"] = 40 }; // disadvantaged vs wingrunners hazard, but headcount dwarfs the requirement regardless

        CombatPatrolResolutionResult result = CombatPatrolResolution.Resolve(squad, tier);

        Assert.Equal(CombatPatrolOutcomeBand.DecisiveVictory, result.Band);
        Assert.All(result.PermanentLosses.Values, loss => Assert.Equal(0, loss));
        Assert.All(result.WoundedLosses.Values, loss => Assert.Equal(0, loss));
        Assert.True(result.HoneyCredited > tier.HoneyReward);
        Assert.True(result.PollenCredited > tier.PollenReward);
    }

    [Fact]
    public void Losses_never_exceed_the_committed_squad_count()
    {
        BestiaryTierDefinition tier = CombatPatrolCatalog.Tiers[6]; // darters hazard, required=600
        var barelyReady = new Dictionary<string, long> { ["wingrunners"] = 168 }; // disadvantaged vs darters-styled hazard

        CombatPatrolResolutionResult result = CombatPatrolResolution.Resolve(barelyReady, tier);

        long totalLoss = result.PermanentLosses["wingrunners"] + result.WoundedLosses["wingrunners"];
        Assert.True(totalLoss <= barelyReady["wingrunners"]);
    }

    [Fact]
    public void Resolution_is_deterministic_for_identical_inputs()
    {
        BestiaryTierDefinition tier = CombatPatrolCatalog.Tiers[5];
        var squad = new Dictionary<string, long> { ["guardians"] = 30, ["wingrunners"] = 20, ["darters"] = 10 };

        CombatPatrolResolutionResult first = CombatPatrolResolution.Resolve(squad, tier);
        CombatPatrolResolutionResult second = CombatPatrolResolution.Resolve(squad, tier);

        // Compare scalars plus a structural (not reference) dictionary comparison for the loss
        // dictionaries, since record equality falls back to reference equality on plain Dictionary members.
        Assert.Equal(first.Band, second.Band);
        Assert.Equal(first.ReadinessBp, second.ReadinessBp);
        Assert.Equal(first.AvailablePower, second.AvailablePower);
        Assert.Equal(first.HoneyCredited, second.HoneyCredited);
        Assert.Equal(first.PollenCredited, second.PollenCredited);
        Assert.Equal(first.PermanentLosses, second.PermanentLosses);
        Assert.Equal(first.WoundedLosses, second.WoundedLosses);
    }

    [Fact]
    public void Champion_power_bonus_increases_available_power_and_can_flip_the_outcome_band()
    {
        BestiaryTierDefinition tier = CombatPatrolCatalog.Tiers[2]; // guardians hazard, required=90
        var squad = new Dictionary<string, long> { ["guardians"] = 18 }; // exactly at required power -> Victory (see above)
        var championBonus = new Dictionary<string, long> { ["guardians"] = 5000 }; // +50% power, e.g. a leveled assigned champion

        CombatPatrolResolutionResult withoutBonus = CombatPatrolResolution.Resolve(squad, tier);
        CombatPatrolResolutionResult withBonus = CombatPatrolResolution.Resolve(squad, tier, championBonus);

        Assert.Equal(CombatPatrolOutcomeBand.Victory, withoutBonus.Band);
        Assert.True(withBonus.AvailablePower > withoutBonus.AvailablePower);
        Assert.Equal(CombatPatrolOutcomeBand.DecisiveVictory, withBonus.Band);
    }

    [Fact]
    public void Champion_power_bonus_only_applies_to_the_family_it_targets()
    {
        BestiaryTierDefinition tier = CombatPatrolCatalog.Tiers[2];
        var squad = new Dictionary<string, long> { ["guardians"] = 10, ["wingrunners"] = 10 };
        var championBonus = new Dictionary<string, long> { ["guardians"] = 5000 };

        long powerWithBonus = CombatPatrolResolution.ComputeAvailablePower(squad, tier.HazardFamily, championBonus);
        long powerWithoutBonus = CombatPatrolResolution.ComputeAvailablePower(squad, tier.HazardFamily);

        Assert.Equal(powerWithoutBonus + 10 * CombatPatrolResolution.UnitPower * 5000 / 10000, powerWithBonus);
    }

    [Fact]
    public void Catalog_covers_seven_tiers_with_valid_hazard_families()
    {
        Assert.Equal(7, CombatPatrolCatalog.Tiers.Count);
        Assert.All(CombatPatrolCatalog.Tiers.Values, tier => Assert.Contains(tier.HazardFamily, CombatDoctrineService.Families));
    }
}
