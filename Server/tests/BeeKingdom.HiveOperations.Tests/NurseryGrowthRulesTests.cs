using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class NurseryGrowthRulesTests
{
    private static readonly DateTimeOffset Start =
        new(2026, 10, 14, 20, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, NurseryVitalityTier.Fragile)]
    [InlineData(39, NurseryVitalityTier.Fragile)]
    [InlineData(40, NurseryVitalityTier.Watch)]
    [InlineData(64, NurseryVitalityTier.Watch)]
    [InlineData(65, NurseryVitalityTier.Stable)]
    [InlineData(89, NurseryVitalityTier.Stable)]
    [InlineData(90, NurseryVitalityTier.Thriving)]
    [InlineData(100, NurseryVitalityTier.Thriving)]
    public void Tiers_are_exactly_defined_on_all_boundaries(
        int vitality, NurseryVitalityTier expected)
        => Assert.Equal(expected, NurseryGrowthRules.Tier(vitality));

    [Theory]
    [InlineData(0)]
    [InlineData(39)]
    [InlineData(40)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(89)]
    [InlineData(90)]
    [InlineData(100)]
    public void Combined_quality_uses_minimum_and_not_mean(int lower)
    {
        NurseryCohortForecast result = NurseryGrowthRules.Preview(
            100, lower, 30, 3600, 0, 120, Start);
        Assert.Equal(lower, result.GlobalQuality);
        Assert.Equal(NurseryGrowthRules.Tier(lower), result.GlobalTier);
    }

    [Theory]
    [InlineData(0, 8000, 4500)]
    [InlineData(39, 8000, 4500)]
    [InlineData(40, 9000, 4000)]
    [InlineData(64, 9000, 4000)]
    [InlineData(65, 10000, 3600)]
    [InlineData(89, 10000, 3600)]
    [InlineData(90, 11000, 3273)]
    [InlineData(100, 11000, 3273)]
    public void Nutrition_changes_real_duration_but_not_yield(
        int nutrition, int speedBp, int duration)
    {
        NurseryCohortForecast result = NurseryGrowthRules.Preview(
            nutrition, 65, 30, 3600, 0, 120, Start);
        Assert.Equal(speedBp, result.NutritionSpeedBasisPoints);
        Assert.Equal(duration, result.DurationSeconds);
        Assert.Equal(30, result.ExpectedRecruits);
        Assert.Equal(Start.AddSeconds(duration), result.EndsAtUtc);
    }

    [Theory]
    [InlineData(0, 7500, 22)]
    [InlineData(39, 7500, 22)]
    [InlineData(40, 8900, 26)]
    [InlineData(64, 8900, 26)]
    [InlineData(65, 10000, 30)]
    [InlineData(89, 10000, 30)]
    [InlineData(90, 10450, 31)]
    [InlineData(100, 10450, 31)]
    public void Stability_changes_real_yield_without_double_applying_quality(
        int stability, int yieldBp, int recruits)
    {
        NurseryCohortForecast result = NurseryGrowthRules.Preview(
            65, stability, 30, 3600, 0, 120, Start);
        Assert.Equal(yieldBp, result.StabilityYieldBasisPoints);
        Assert.Equal(recruits, result.ExpectedRecruits);
        Assert.Equal(3600, result.DurationSeconds);
    }

    [Fact]
    public void Identical_inputs_produce_identical_outputs_without_any_state_mutation()
    {
        NurseryCohortForecast one = NurseryGrowthRules.Preview(
            75, 99, 30, 3600, 30, 120, Start);
        NurseryCohortForecast two = NurseryGrowthRules.Preview(
            75, 99, 30, 3600, 30, 120, Start);
        Assert.Equal(one, two);
        Assert.Equal(10450, one.IndicativeCombinedEfficiencyBasisPoints);
        Assert.True(one.CanStartWithCurrentReserve);
    }

    [Fact]
    public void Full_or_insufficient_reserve_refuses_to_start_not_discard_newborns()
    {
        NurseryCohortForecast full = NurseryGrowthRules.Preview(
            99, 99, 30, 3600, 120, 120, Start);
        NurseryCohortForecast insufficient = NurseryGrowthRules.Preview(
            99, 99, 30, 3600, 90, 120, Start);
        NurseryCohortForecast fits = NurseryGrowthRules.Preview(
            99, 99, 30, 3600, 89, 120, Start);
        Assert.False(full.CanStartWithCurrentReserve);
        Assert.False(insufficient.CanStartWithCurrentReserve); // 31 expected, 30 free
        Assert.True(fits.CanStartWithCurrentReserve); // 31 expected, 31 free
    }

    [Fact]
    public void Production_default_profile_is_explicitly_proposed_and_not_persisted()
    {
        NurseryCohortForecast result = NurseryGrowthRules.Preview(
            65, 65,
            NurseryGrowthRules.InitialBaseRecruits,
            NurseryGrowthRules.InitialBaseDurationSeconds,
            0, NurseryGrowthRules.InitialReserveCapacity, Start);
        Assert.Equal(30, result.ExpectedRecruits);
        Assert.Equal(120, result.ReserveCapacity);
        Assert.Equal("bk-nursery-growth-balance-v1-draft", result.ContractVersion);
        Assert.True(result.CanStartWithCurrentReserve);
        Assert.DoesNotContain("production", result.ContractVersion);
    }

    [Fact]
    public void Out_of_range_inputs_and_non_utc_clock_fail_closed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NurseryGrowthRules.Tier(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => NurseryGrowthRules.Tier(101));
        Assert.Throws<ArgumentOutOfRangeException>(() => NurseryGrowthRules.Preview(
            20, 20, 0, 3600, 0, 120, Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => NurseryGrowthRules.Preview(
            20, 20, 30, 0, 0, 120, Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => NurseryGrowthRules.Preview(
            20, 20, 30, 3600, 121, 120, Start));
        Assert.Throws<ArgumentException>(() => NurseryGrowthRules.Preview(
            20, 20, 30, 3600, 0, 120, Start.ToOffset(TimeSpan.FromHours(-4))));
        Assert.Throws<ArgumentException>(() => NurseryGrowthRules.Preview(
            20, 20, 30, 3600, 0, 120,
            new DateTimeOffset(9999,12,31,23,59,59,TimeSpan.Zero)));
    }

    [Fact]
    public void Inputs_cannot_overflow_or_create_negative_recruits()
    {
        NurseryCohortForecast atLimits = NurseryGrowthRules.Preview(
            100, 100, 1_000_000, 604_800, 0, 1_000_000_000, Start);
        Assert.InRange(atLimits.ExpectedRecruits, 1, 1_100_000);
        Assert.True(atLimits.DurationSeconds > 0);
        Assert.True(atLimits.CanStartWithCurrentReserve);
        Assert.Throws<ArgumentOutOfRangeException>(() => NurseryGrowthRules.Preview(
            70,70,int.MaxValue,3600,0,120,Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => NurseryGrowthRules.Preview(
            70,70,30,int.MaxValue,0,120,Start));
        Assert.Throws<ArgumentOutOfRangeException>(() => NurseryGrowthRules.Preview(
            70,70,30,3600,0,long.MaxValue,Start));
    }
}
