using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class HiveProgressionSnapshotTests
{
    [Fact]
    public void SnapshotKeepsScopeRevisionsCatalogAndAllLevelsAndTroops()
    {
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid(), world = Guid.NewGuid(), server = Guid.NewGuid();
        PlayerHiveState state = new(player, hive, 6, 7, new(), new Dictionary<string, int> { ["honey_storage"] = 3, ["nursery"] = 2 }, [], new());
        HiveProgressionSnapshot snapshot = HiveProgressionSnapshotFactory.FromAuthoritativeState(state, world, server, 4, new Dictionary<string, long> { ["worker_bee"] = 12, ["guard_bee"] = 5 }, "catalog-2");
        Assert.Equal((player, hive, world, server), (snapshot.PlayerId, snapshot.HiveId, snapshot.WorldId, snapshot.GameServerId));
        Assert.Equal(7, snapshot.BuildingRevision); Assert.Equal(4, snapshot.ArmyRevision); Assert.Equal(3, snapshot.BuildingLevels["honey_storage"]); Assert.Equal(12, snapshot.TroopCounts["worker_bee"]);
    }

    [Fact]
    public void NegativeValuesOrMissingScopeAreRejectedWithoutMerging()
    {
        Guid id = Guid.NewGuid(); PlayerHiveState state = new(id, id, 6, 0, new(), new Dictionary<string, int> { ["bad"] = -1 }, [], new());
        Assert.Throws<InvalidDataException>(() => HiveProgressionSnapshotFactory.FromAuthoritativeState(state, Guid.NewGuid(), Guid.NewGuid(), 0));
        Assert.Throws<ArgumentException>(() => HiveProgressionSnapshotFactory.FromAuthoritativeState(state with { BuildingLevels = new() }, Guid.Empty, Guid.NewGuid(), 0));
    }

    [Fact]
    public void InvalidStateIdentityRevisionKeysAndTroopsAreRejected()
    {
        Guid valid = Guid.NewGuid();
        PlayerHiveState Base() => new(valid, valid, 6, 0, new(), new Dictionary<string, int> { ["ok"] = 1 }, [], new());
        Assert.Throws<ArgumentException>(() => HiveProgressionSnapshotFactory.FromAuthoritativeState(null!, valid, valid, 0));
        Assert.Throws<ArgumentException>(() => HiveProgressionSnapshotFactory.FromAuthoritativeState(Base() with { PlayerId = Guid.Empty }, valid, valid, 0));
        Assert.Throws<ArgumentException>(() => HiveProgressionSnapshotFactory.FromAuthoritativeState(Base() with { HiveId = Guid.Empty }, valid, valid, 0));
        Assert.Throws<ArgumentException>(() => HiveProgressionSnapshotFactory.FromAuthoritativeState(Base() with { Revision = -1 }, valid, valid, 0));
        Assert.Throws<InvalidDataException>(() => HiveProgressionSnapshotFactory.FromAuthoritativeState(Base() with { BuildingLevels = new Dictionary<string, int> { [""] = 1 } }, valid, valid, 0));
        Assert.Throws<InvalidDataException>(() => HiveProgressionSnapshotFactory.FromAuthoritativeState(Base(), valid, valid, 0, new Dictionary<string, long> { [""] = 1 }));
        Assert.Throws<ArgumentException>(() => HiveProgressionSnapshotFactory.FromAuthoritativeState(Base(), valid, valid, 0, catalogVersion: " "));
    }
}
