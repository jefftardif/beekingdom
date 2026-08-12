using System.Linq;
using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class HiveMilestoneEventServiceTests
{
    private static HiveMilestoneEventOptions Options() => new()
    {
        Enabled = true,
        RequiredObjectiveCount = 3,
        WindowDays = 30,
        RewardHoney = 800,
        RewardPollen = 500,
        VipPointsObjectiveThreshold = 50,
        TroopCountObjectiveThreshold = 10
    };

    private static (Guid, Guid, DurableJsonHiveStateRepository) NewRepo(
        Dictionary<string, int> buildingLevels = null,
        DoctrineRosterState roster = null,
        WorldResourceCollectionState worldResources = null,
        StrategicPathState strategicPath = null,
        VipProgressState vip = null)
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        string root = Path.Combine(Path.GetTempPath(), "milestone-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, 10, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 1_000_000), ["pollen"] = new(0, 1_000_000), ["wax"] = new(0, 1_000_000) },
            buildingLevels ?? new Dictionary<string, int>(), [], new(),
            DoctrineRoster: roster, WorldResourceCollection: worldResources, StrategicPath: strategicPath, Vip: vip));
        return (p, h, repo);
    }

    [Fact]
    public async Task ReadReportsIncompleteObjectivesAndCannotClaim()
    {
        (Guid p, Guid h, var repo) = NewRepo();
        var service = new HiveMilestoneEventService(repo, new Clock(0), Options());
        HiveMilestoneEventSnapshot snapshot = await service.ReadAsync(p, h);
        Assert.False(snapshot.CanClaim);
        Assert.False(snapshot.Claimed);
        Assert.All(snapshot.Objectives, o => Assert.False(o.Done));
    }

    [Fact]
    public async Task ClaimFailsWhenFewerThanRequiredObjectivesAreDone()
    {
        (Guid p, Guid h, var repo) = NewRepo(buildingLevels: new Dictionary<string, int> { ["honey_storage"] = 2 },
            strategicPath: new StrategicPathState("phase4-v1", "striker", 1, DateTimeOffset.Parse("2026-07-31T00:00:00Z"), new()));
        var service = new HiveMilestoneEventService(repo, new Clock(0), Options());
        HiveMilestoneEventSnapshot snapshot = await service.ReadAsync(p, h);
        Assert.Equal(2, snapshot.Objectives.Count(o => o.Done));
        HiveMilestoneEventResult result = await service.ClaimAsync(p, h, new(snapshot.Revision, "k1"));
        Assert.False(result.Succeeded);
        Assert.Equal("game.milestone_incomplete", result.Code);
    }

    [Fact]
    public async Task ClaimSucceedsWithThreeObjectivesAndCreditsRewardOnce()
    {
        (Guid p, Guid h, var repo) = NewRepo(
            buildingLevels: new Dictionary<string, int> { ["honey_storage"] = 2 },
            roster: new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = 12 }, null, new()),
            strategicPath: new StrategicPathState("phase4-v1", "striker", 1, DateTimeOffset.Parse("2026-07-31T00:00:00Z"), new()));
        var service = new HiveMilestoneEventService(repo, new Clock(0), Options());
        HiveMilestoneEventSnapshot snapshot = await service.ReadAsync(p, h);
        Assert.Equal(3, snapshot.Objectives.Count(o => o.Done));
        Assert.True(snapshot.CanClaim);

        HiveMilestoneEventResult claim = await service.ClaimAsync(p, h, new(snapshot.Revision, "k1"));
        Assert.True(claim.Succeeded);
        Assert.Equal("game.milestone_claimed", claim.Code);
        Assert.True(claim.Snapshot.Claimed);

        HiveMilestoneEventResult second = await service.ClaimAsync(p, h, new(claim.Snapshot.Revision, "k2"));
        Assert.False(second.Succeeded);
        Assert.Equal("game.milestone_already_claimed", second.Code);
    }

    [Fact]
    public async Task ClaimFailsAfterWindowExpires()
    {
        (Guid p, Guid h, var repo) = NewRepo(
            buildingLevels: new Dictionary<string, int> { ["honey_storage"] = 2 },
            roster: new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = 12 }, null, new()),
            strategicPath: new StrategicPathState("phase4-v1", "striker", 1, DateTimeOffset.Parse("2026-07-31T00:00:00Z"), new()));
        var clock = new Clock(0);
        var service = new HiveMilestoneEventService(repo, clock, Options());
        HiveMilestoneEventSnapshot snapshot = await service.ReadAsync(p, h);
        clock.AdvanceDays(31);
        HiveMilestoneEventSnapshot laterSnapshot = await service.ReadAsync(p, h);
        Assert.True(laterSnapshot.WindowExpired);
        Assert.False(laterSnapshot.CanClaim);
        HiveMilestoneEventResult result = await service.ClaimAsync(p, h, new(snapshot.Revision, "k1"));
        Assert.False(result.Succeeded);
        Assert.Equal("game.milestone_window_expired", result.Code);
    }

    private sealed class Clock(double startSeconds) : IServerClock
    {
        private DateTimeOffset current = DateTimeOffset.Parse("2026-07-31T12:00:00Z").AddSeconds(startSeconds);
        public DateTimeOffset UtcNow => current;
        public void AdvanceDays(double days) => current = current.AddDays(days);
    }
}
