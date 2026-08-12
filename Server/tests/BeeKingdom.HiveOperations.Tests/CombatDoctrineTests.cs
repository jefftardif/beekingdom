using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class CombatDoctrineTests
{
    [Fact]
    public void Catalog_has_three_stable_unique_families_and_version() {
        CombatDoctrineService service = new();
        CombatDoctrineSnapshot snapshot = service.GetSnapshot();
        Assert.Equal("phase4-combat-v1", snapshot.CatalogVersion);
        Assert.Equal(3, snapshot.Families.Count);
        Assert.Equal(3, snapshot.Families.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(new[] { "guardians", "wingrunners", "darters" }, snapshot.Families);
    }

    [Fact]
    public void Cycle_is_complete_and_same_family_is_neutral() {
        CombatDoctrineService service = new();
        Assert.True(service.HasAdvantage("guardians", "darters"));
        Assert.True(service.HasAdvantage("darters", "wingrunners"));
        Assert.True(service.HasAdvantage("wingrunners", "guardians"));
        Assert.False(service.HasAdvantage("guardians", "guardians"));
        Assert.False(service.HasAdvantage("wingrunners", "wingrunners"));
        Assert.False(service.HasAdvantage("darters", "darters"));
        Assert.Equal(0, service.GetSnapshot().Families.Count(a => service.HasAdvantage(a, a)));
    }
}
