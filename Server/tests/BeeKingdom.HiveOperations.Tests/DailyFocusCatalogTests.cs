using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class DailyFocusCatalogTests
{
    [Fact]
    public void FeaturedCombatTier_is_stable_within_the_same_utc_day_and_in_range()
    {
        DateTimeOffset morning = new(2026, 7, 25, 1, 0, 0, TimeSpan.Zero);
        DateTimeOffset night = new(2026, 7, 25, 23, 59, 0, TimeSpan.Zero);
        int a = DailyFocusCatalog.FeaturedCombatTier(morning);
        int b = DailyFocusCatalog.FeaturedCombatTier(night);
        Assert.Equal(a, b);
        Assert.InRange(a, 1, 7);
    }

    [Fact]
    public void FeaturedCombatTier_cycles_with_a_7_day_period()
    {
        DateTimeOffset day0 = new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);
        int t0 = DailyFocusCatalog.FeaturedCombatTier(day0);
        int t7 = DailyFocusCatalog.FeaturedCombatTier(day0.AddDays(7));
        Assert.Equal(t0, t7);
    }

    [Fact]
    public void FeaturedCombatTier_changes_the_next_day_across_a_full_week()
    {
        DateTimeOffset day0 = new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);
        var seen = new HashSet<int>();
        for (int i = 0; i < 7; i++) seen.Add(DailyFocusCatalog.FeaturedCombatTier(day0.AddDays(i)));
        Assert.Equal(7, seen.Count); // all 7 tiers are featured exactly once across a full week
    }

    [Fact]
    public void FeaturedWorldResourceNodeId_rotates_through_the_provided_catalog_order()
    {
        List<string> nodes = ["res_pollen_core", "res_wax_core", "res_honey_core"];
        DateTimeOffset day0 = new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);
        var seen = new HashSet<string>();
        for (int i = 0; i < 3; i++)
        {
            string? id = DailyFocusCatalog.FeaturedWorldResourceNodeId(day0.AddDays(i), nodes);
            Assert.NotNull(id);
            Assert.Contains(id, nodes);
            seen.Add(id!);
        }
        Assert.Equal(3, seen.Count);
        Assert.Equal(DailyFocusCatalog.FeaturedWorldResourceNodeId(day0, nodes), DailyFocusCatalog.FeaturedWorldResourceNodeId(day0.AddDays(3), nodes));
    }

    [Fact]
    public void FeaturedWorldResourceNodeId_returns_null_for_an_empty_catalog()
    {
        Assert.Null(DailyFocusCatalog.FeaturedWorldResourceNodeId(DateTimeOffset.UtcNow, []));
    }

    [Fact]
    public void ApplyRewardBonus_adds_fifty_percent_rounded_down()
    {
        Assert.Equal(150, DailyFocusCatalog.ApplyRewardBonus(100));
        Assert.Equal(0, DailyFocusCatalog.ApplyRewardBonus(0));
        Assert.Equal(1, DailyFocusCatalog.ApplyRewardBonus(1)); // 1 + floor(1*0.5) = 1
        Assert.Equal(3, DailyFocusCatalog.ApplyRewardBonus(2)); // 2 + 1 = 3
    }
}
