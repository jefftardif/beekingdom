using System.Linq;
using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class QuestChainServiceTests
{
    private static QuestChainOptions Options() => new() { Enabled = true };

    private static (Guid, Guid, DurableJsonHiveStateRepository) NewRepo(
        Dictionary<string, int> buildingLevels = null,
        DoctrineRosterState roster = null,
        HiveResearchState research = null,
        WorldResourceCollectionState worldResources = null,
        QuestChainState quest = null)
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        string root = Path.Combine(Path.GetTempPath(), "quest-chain-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, 11, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 1_000_000), ["pollen"] = new(0, 1_000_000), ["wax"] = new(0, 1_000_000) },
            buildingLevels ?? new Dictionary<string, int>(), [], new(),
            DoctrineRoster: roster, Research: research, WorldResourceCollection: worldResources, QuestChain: quest));
        return (p, h, repo);
    }

    [Fact]
    public async Task ReadCreatesDefaultStateWithNoObjectivesDone()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new QuestChainService(repo, new Clock(), Options());
        QuestChainSnapshot snapshot = await service.ReadAsync(p, h);
        Assert.Equal(5, snapshot.Objectives.Count);
        Assert.All(snapshot.Objectives, o => Assert.False(o.Done));
        Assert.All(snapshot.Objectives, o => Assert.False(o.Claimed));
        Assert.All(snapshot.Objectives, o => Assert.False(o.CanClaim));
    }

    [Fact]
    public async Task ObjectiveBecomesDoneWhenUnderlyingStateAlreadyReflectsIt()
    {
        (Guid p, Guid h, var repo) = NewRepo(
            buildingLevels: new Dictionary<string, int> { ["honey_storage"] = 2 },
            roster: new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = 3 }, null, new()),
            research: new HiveResearchState(new Dictionary<string, ResearchCompletion> { ["r1"] = new("r1", DateTimeOffset.UtcNow, new(0, 0, 0, 0, 0, 0)) }, null),
            worldResources: new WorldResourceCollectionState(0, new Dictionary<string, DateTimeOffset> { ["node1"] = DateTimeOffset.UtcNow }, null, new()));
        var service = new QuestChainService(repo, new Clock(), Options());
        QuestChainSnapshot snapshot = await service.ReadAsync(p, h);
        Assert.True(snapshot.Objectives.Single(o => o.ObjectiveKey == "q1_building_upgrade").Done);
        Assert.True(snapshot.Objectives.Single(o => o.ObjectiveKey == "q2_troop_recruit").Done);
        Assert.True(snapshot.Objectives.Single(o => o.ObjectiveKey == "q3_research_complete").Done);
        Assert.False(snapshot.Objectives.Single(o => o.ObjectiveKey == "q4_world_map_visit").Done);
        Assert.True(snapshot.Objectives.Single(o => o.ObjectiveKey == "q5_world_resource_collect").Done);
    }

    [Fact]
    public async Task ReportWorldMapVisitedIsIdempotentAndCompletesThatObjectiveOnly()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new QuestChainService(repo, new Clock(), Options());
        QuestChainSnapshot before = await service.ReadAsync(p, h);
        Assert.False(before.Objectives.Single(o => o.ObjectiveKey == "q4_world_map_visit").Done);

        QuestChainSnapshot first = await service.ReportWorldMapVisitedAsync(p, h);
        Assert.True(first.Objectives.Single(o => o.ObjectiveKey == "q4_world_map_visit").Done);
        Assert.Equal(1, first.Revision);

        QuestChainSnapshot second = await service.ReportWorldMapVisitedAsync(p, h);
        Assert.Equal(1, second.Revision);
    }

    [Fact]
    public async Task ClaimFailsWhenObjectiveNotYetDone()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new QuestChainService(repo, new Clock(), Options());
        QuestChainSnapshot snapshot = await service.ReadAsync(p, h);
        QuestChainClaimResult result = await service.ClaimAsync(p, h, "q1_building_upgrade", new(snapshot.Revision, "k1"));
        Assert.False(result.Succeeded);
        Assert.Equal("game.quest_incomplete", result.Code);
    }

    [Fact]
    public async Task ClaimSucceedsCreditsRewardOnceAndPreventsDoubleClaim()
    {
        (Guid p, Guid h, var repo) = NewRepo(buildingLevels: new Dictionary<string, int> { ["honey_storage"] = 2 });
        var service = new QuestChainService(repo, new Clock(), Options());
        QuestChainSnapshot snapshot = await service.ReadAsync(p, h);
        Assert.True(snapshot.Objectives.Single(o => o.ObjectiveKey == "q1_building_upgrade").CanClaim);

        QuestChainClaimResult claim = await service.ClaimAsync(p, h, "q1_building_upgrade", new(snapshot.Revision, "k1"));
        Assert.True(claim.Succeeded);
        Assert.Equal("game.quest_claimed", claim.Code);
        Assert.True(claim.Snapshot.Objectives.Single(o => o.ObjectiveKey == "q1_building_upgrade").Claimed);
        Assert.False(claim.Snapshot.Objectives.Single(o => o.ObjectiveKey == "q1_building_upgrade").CanClaim);

        QuestChainClaimResult second = await service.ClaimAsync(p, h, "q1_building_upgrade", new(claim.Snapshot.Revision, "k2"));
        Assert.False(second.Succeeded);
        Assert.Equal("game.quest_already_claimed", second.Code);
    }

    [Fact]
    public async Task ReplayingSameIdempotencyKeyReturnsSameResultWithoutDoubleCrediting()
    {
        (Guid p, Guid h, var repo) = NewRepo(buildingLevels: new Dictionary<string, int> { ["honey_storage"] = 2 });
        var service = new QuestChainService(repo, new Clock(), Options());
        QuestChainSnapshot snapshot = await service.ReadAsync(p, h);

        QuestChainClaimResult first = await service.ClaimAsync(p, h, "q1_building_upgrade", new(snapshot.Revision, "same-key"));
        Assert.True(first.Succeeded);
        QuestChainClaimResult replay = await service.ClaimAsync(p, h, "q1_building_upgrade", new(snapshot.Revision, "same-key"));
        Assert.True(replay.Succeeded);
        Assert.Equal(first.Snapshot.Revision, replay.Snapshot.Revision);
    }

    [Fact]
    public async Task ClaimingOneObjectiveDoesNotAffectOthers()
    {
        (Guid p, Guid h, var repo) = NewRepo(
            buildingLevels: new Dictionary<string, int> { ["honey_storage"] = 2 },
            roster: new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = 3 }, null, new()));
        var service = new QuestChainService(repo, new Clock(), Options());
        QuestChainSnapshot snapshot = await service.ReadAsync(p, h);

        QuestChainClaimResult claim = await service.ClaimAsync(p, h, "q1_building_upgrade", new(snapshot.Revision, "k1"));
        Assert.True(claim.Succeeded);
        QuestChainObjectiveReadModel q2 = claim.Snapshot.Objectives.Single(o => o.ObjectiveKey == "q2_troop_recruit");
        Assert.True(q2.Done);
        Assert.False(q2.Claimed);
        Assert.True(q2.CanClaim);
    }

    private sealed class Clock : IServerClock
    {
        private DateTimeOffset current = DateTimeOffset.Parse("2026-09-09T12:00:00Z");
        public DateTimeOffset UtcNow => current;
    }
}
