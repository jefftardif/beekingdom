using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class HiveStockSnapshotTests
{
    [Fact]
    public void SnapshotUsesOnlyAuthoritativeStateAndDerivesActiveEngagements()
    {
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid(), operationId = Guid.NewGuid();
        DateTimeOffset start = DateTimeOffset.Parse("2026-07-22T12:00:00Z");
        PlayerHiveState state = new(player, hive, 6, 9,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(240, 1000), ["wax"] = new(8, 50), ["pollen"] = new(90, 400) }, new(),
            [new(operationId, "apiary", 0, 1, start, start.AddSeconds(16), HiveOperationStatus.Running, "honey", 0, null)], new(),
            Research: new(new Dictionary<string, ResearchCompletion> { ["foraging_routes_i"] = new("foraging_routes_i", start, new ResearchEffects(200, 0, 0, 0, 0, 0)) }, null));
        HiveStockSnapshot snapshot = HiveStockSnapshotFactory.FromAuthoritativeState(state, "test-v1", DateTimeOffset.UtcNow);
        Assert.Equal(player, snapshot.PlayerId); Assert.Equal(hive, snapshot.HiveId); Assert.Equal(9, snapshot.Revision);
        Assert.Equal(new ResourceStockSnapshot(240, 1000), snapshot.Honey); Assert.Equal(new ResourceStockSnapshot(8, 50), snapshot.Wax); Assert.Equal(new ResourceStockSnapshot(90, 400), snapshot.Pollen);
        Assert.Null(snapshot.Population); Assert.Contains("foraging_routes_i", snapshot.CompletedResearchIds); Assert.Single(snapshot.ActiveEngagements);
        Assert.Equal(operationId, snapshot.ActiveEngagements[0].OperationId);
    }

    [Fact]
    public void SnapshotRejectsCorruptAuthoritativeShapes()
    {
        Guid p=Guid.NewGuid(), h=Guid.NewGuid(); var now=DateTimeOffset.Parse("2026-07-22T12:00:00Z");
        PlayerHiveState Base() => new(p,h,1,1,new Dictionary<string,ResourceBalance>{{"honey",new(1,2)},{"wax",new(1,2)},{"pollen",new(1,2)}},new(),new(),new(),Research:new(new Dictionary<string,ResearchCompletion>(),null));
        Assert.Throws<InvalidDataException>(()=>HiveStockSnapshotFactory.FromAuthoritativeState(Base() with { Resources = new Dictionary<string,ResourceBalance>{{"honey",new(1,2)}} },"test-v1",now));
        Assert.Throws<InvalidDataException>(()=>HiveStockSnapshotFactory.FromAuthoritativeState(Base(),"Bad Version",now));
        Assert.Throws<InvalidDataException>(()=>HiveStockSnapshotFactory.FromAuthoritativeState(Base(),"test-v1",default));
    }
}
