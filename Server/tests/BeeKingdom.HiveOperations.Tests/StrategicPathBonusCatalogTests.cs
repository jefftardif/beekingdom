using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class StrategicPathBonusCatalogTests
{
    [Theory]
    [InlineData("royal_guard")]
    [InlineData("striker")]
    public void Combat_classes_grant_the_combat_power_bonus_to_every_family(string path)
    {
        var bonus = StrategicPathBonusCatalog.CombatPowerBonusBpByFamily(path);

        Assert.Equal(StrategicPathBonusCatalog.CombatPowerBonusBp, bonus["guardians"]);
        Assert.Equal(StrategicPathBonusCatalog.CombatPowerBonusBp, bonus["wingrunners"]);
        Assert.Equal(StrategicPathBonusCatalog.CombatPowerBonusBp, bonus["darters"]);
    }

    [Theory]
    [InlineData("nurturer")]
    [InlineData("scout")]
    [InlineData("alchemist")]
    [InlineData(null)]
    public void Non_combat_classes_grant_no_combat_power_bonus(string? path)
    {
        var bonus = StrategicPathBonusCatalog.CombatPowerBonusBpByFamily(path);

        Assert.All(bonus.Values, bp => Assert.Equal(0, bp));
    }

    [Theory]
    [InlineData("nurturer")]
    [InlineData("alchemist")]
    public void Support_classes_grant_the_production_rate_bonus(string path)
    {
        Assert.Equal(StrategicPathBonusCatalog.ProductionRateBonusBp, StrategicPathBonusCatalog.ProductionRateBonusBpFor(path));
    }

    [Theory]
    [InlineData("royal_guard")]
    [InlineData("striker")]
    [InlineData("scout")]
    [InlineData(null)]
    public void Non_support_classes_grant_no_production_rate_bonus(string? path)
    {
        Assert.Equal(0, StrategicPathBonusCatalog.ProductionRateBonusBpFor(path));
    }

    [Fact]
    public void Scout_grants_the_capacity_bonus_and_no_other_class_does()
    {
        Assert.Equal(StrategicPathBonusCatalog.CapacityBonusBp, StrategicPathBonusCatalog.CapacityBonusBpFor("scout"));
        Assert.Equal(0, StrategicPathBonusCatalog.CapacityBonusBpFor("royal_guard"));
        Assert.Equal(0, StrategicPathBonusCatalog.CapacityBonusBpFor("nurturer"));
        Assert.Equal(0, StrategicPathBonusCatalog.CapacityBonusBpFor(null));
    }
}
