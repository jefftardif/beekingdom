using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class CombatFormationReadinessTests
{
    [Fact]
    public void Missing_roster_is_not_recorded_and_never_synthesizes_zero_counts()
    {
        Guid player = Guid.NewGuid();
        Guid hive = Guid.NewGuid();
        PlayerHiveState state = new(player, hive, 6, 4, new(), new(), new(), new());
        CombatFormationReadinessSnapshot snapshot = new CombatFormationReadinessService().FromAuthoritativeState(state);
        Assert.Equal("not_recorded", snapshot.AvailabilityStatus);
        Assert.Empty(snapshot.Families);
        Assert.Contains("Gardiennes", snapshot.UnclassifiedLegacyRoles);
        Assert.Equal(4, snapshot.Revision);
    }

    [Fact]
    public void Invalid_scope_is_rejected()
    {
        PlayerHiveState state = new(Guid.Empty, Guid.NewGuid(), 6, 0, new(), new(), new(), new());
        Assert.Throws<ArgumentException>(() => new CombatFormationReadinessService().FromAuthoritativeState(state));
    }
}
