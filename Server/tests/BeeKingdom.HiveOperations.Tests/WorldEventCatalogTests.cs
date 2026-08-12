using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class WorldEventCatalogTests
{
    [Fact]
    public void Active_is_stable_within_the_same_four_hour_window()
    {
        DateTimeOffset early = new(2026, 7, 25, 8, 5, 0, TimeSpan.Zero);
        DateTimeOffset late = new(2026, 7, 25, 11, 55, 0, TimeSpan.Zero);
        Assert.Equal(WorldEventCatalog.Active(early).Key, WorldEventCatalog.Active(late).Key);
    }

    [Fact]
    public void Active_changes_across_a_window_boundary()
    {
        DateTimeOffset justBefore = new(2026, 7, 25, 11, 59, 0, TimeSpan.Zero);
        DateTimeOffset justAfter = new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);
        Assert.NotEqual(WorldEventCatalog.Active(justBefore).Key, WorldEventCatalog.Active(justAfter).Key);
    }

    [Fact]
    public void Active_covers_all_six_events_across_a_single_day()
    {
        DateTimeOffset dayStart = new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);
        var seen = new HashSet<string>();
        for (int hour = 0; hour < 24; hour += 4) seen.Add(WorldEventCatalog.Active(dayStart.AddHours(hour)).Key);
        Assert.Equal(6, seen.Count);
    }

    [Fact]
    public void Active_shifts_its_daily_offset_so_the_sequence_is_not_identical_every_day()
    {
        DateTimeOffset day0 = new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);
        Assert.NotEqual(WorldEventCatalog.Active(day0).Key, WorldEventCatalog.Active(day0.AddDays(1)).Key);
    }

    [Fact]
    public void NextChangeAtUtc_points_to_the_start_of_the_following_window()
    {
        DateTimeOffset mid = new(2026, 7, 25, 5, 30, 0, TimeSpan.Zero);
        DateTimeOffset expected = new(2026, 7, 25, 8, 0, 0, TimeSpan.Zero);
        Assert.Equal(expected, WorldEventCatalog.NextChangeAtUtc(mid));
    }

    [Fact]
    public void FeaturedRegionTier_picks_exactly_one_region_among_several_eligible_ones()
    {
        List<int> eligible = [2, 4]; // deux paliers "guardians" (Fourmi coupeuse, Mante predatrice)
        DateTimeOffset t = new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero);
        var seen = new HashSet<int>();
        for (int i = 0; i < 8; i++)
        {
            int? featured = WorldEventCatalog.FeaturedRegionTier(t.AddHours(4 * i), eligible);
            Assert.NotNull(featured);
            Assert.Contains(featured!.Value, eligible);
            seen.Add(featured.Value);
        }
        // Sur plusieurs cycles, ce n'est jamais TOUJOURS le meme des deux - preuve que
        // l'evenement ne boost plus les deux paliers de la famille en meme temps.
        Assert.Equal(2, seen.Count);
    }

    [Fact]
    public void FeaturedRegionTier_returns_null_for_an_empty_region_list()
    {
        Assert.Null(WorldEventCatalog.FeaturedRegionTier(DateTimeOffset.UtcNow, []));
    }

    [Fact]
    public void FeaturedRegionNodeId_picks_exactly_one_node_among_several_eligible_ones()
    {
        List<string> eligible = ["res_a", "res_b", "res_c"];
        DateTimeOffset t = new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero);
        var seen = new HashSet<string>();
        for (int i = 0; i < 12; i++)
        {
            string? featured = WorldEventCatalog.FeaturedRegionNodeId(t.AddHours(4 * i), eligible);
            Assert.NotNull(featured);
            Assert.Contains(featured, eligible);
            seen.Add(featured!);
        }
        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void ApplyBonusBp_supports_both_boosts_and_reductions()
    {
        Assert.Equal(125, WorldEventCatalog.ApplyBonusBp(100, 2500));
        Assert.Equal(80, WorldEventCatalog.ApplyBonusBp(100, -2000));
        Assert.Equal(0, WorldEventCatalog.ApplyBonusBp(0, 2500));
    }
}
