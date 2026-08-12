using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class LivingHiveResearchTests
{
    [Fact]
    public async Task StartDebitsOnceAndReplayIsStable()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-22T12:00:00Z"));
        string root = Path.Combine(Path.GetTempPath(), "research-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(1000, 2000), ["pollen"] = new(500, 1000) }, new(), [], new()));
        var svc = new HiveOperationService(repo, clock, []);
        var cmd = new StartResearchCommand(p, h, "foraging_routes_i", 0, "r1");
        ResearchCommandResult first = await svc.StartResearchAsync(cmd);
        ResearchCommandResult replay = await svc.StartResearchAsync(cmd);
        Assert.True(first.Succeeded); Assert.Equal("research_started", first.Code);
        Assert.Equal(first.OperationId, replay.OperationId); Assert.Equal(first.RevisionAfter, replay.RevisionAfter);
        PlayerHiveState state = (await repo.ReadAsync(p, h))!;
        Assert.Equal(760, state.Resources["honey"].Amount); Assert.Equal(410, state.Resources["pollen"].Amount);
        Assert.NotNull(state.Research?.ActiveOperation);
    }

    [Fact]
    public async Task CompletionCannotBeEarlyAndThenAppliesEffectOnce()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-22T12:00:00Z"));
        string root = Path.Combine(Path.GetTempPath(), "research-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(1000, 2000), ["pollen"] = new(500, 1000) }, new(), [], new()));
        var svc = new HiveOperationService(repo, clock, []);
        ResearchCommandResult started = await svc.StartResearchAsync(new(p, h, "tempered_combs_i", 0, "s1"));
        ResearchCommandResult early = await svc.CompleteResearchAsync(new(p, h, started.OperationId!.Value, 1, "c1"));
        Assert.False(early.Succeeded); Assert.Equal("research_not_ready", early.Code);
        clock.Now = clock.Now.AddSeconds(120);
        ResearchCommandResult done = await svc.CompleteResearchAsync(new(p, h, started.OperationId!.Value, 1, "c2"));
        Assert.True(done.Succeeded); Assert.Equal("research_completed", done.Code);
        ResearchCommandResult replay = await svc.CompleteResearchAsync(new(p, h, started.OperationId!.Value, 1, "c2"));
        Assert.Equal(done.RevisionAfter, replay.RevisionAfter);
        Assert.Equal(500, done.State.Research!.Completed["tempered_combs_i"].Effects.WaxCapacityBonusBps);
    }

    [Fact]
    public async Task StartRejectsWhenPrerequisiteMissingThenSucceedsOnceItIsCompleted()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-30T12:00:00Z"));
        string root = Path.Combine(Path.GetTempPath(), "research-prereq-" + Guid.NewGuid());
        var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance> { ["honey"] = new(100000, 100000), ["pollen"] = new(100000, 100000) }, new(), [], new()));
        var svc = new HiveOperationService(repo, clock, []);
        ResearchCommandResult blocked = await svc.StartResearchAsync(new(p, h, "foraging_routes_ii", 0, "blocked"));
        Assert.False(blocked.Succeeded);
        Assert.Equal("research_prerequisite_missing", blocked.Code);

        ResearchCommandResult started = await svc.StartResearchAsync(new(p, h, "foraging_routes_i", 0, "s1"));
        Assert.True(started.Succeeded);
        clock.Now = clock.Now.AddSeconds(120);
        ResearchCommandResult completed = await svc.CompleteResearchAsync(new(p, h, started.OperationId!.Value, 1, "c1"));
        Assert.True(completed.Succeeded);

        ResearchCommandResult unlocked = await svc.StartResearchAsync(new(p, h, "foraging_routes_ii", 2, "s2"));
        Assert.True(unlocked.Succeeded);
        Assert.Equal("research_started", unlocked.Code);
    }

    [Fact]
    public async Task StartResearch_DailyRoundFlagMarksFreshOnceAndReplayDoesNotMutate()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        var clock = new FixedClock(DateTimeOffset.Parse("2026-07-22T12:00:00Z"));
        string root = Path.Combine(Path.GetTempPath(), "research-daily-" + Guid.NewGuid());
        try
        {
            var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0,
                new Dictionary<string, ResourceBalance> { ["honey"] = new(1000, 2000), ["pollen"] = new(500, 1000) }, new(), [], new()));
            var svc = new HiveOperationService(repo, clock, [], null, null, true);
            var cmd = new StartResearchCommand(p, h, "foraging_routes_i", 0, "daily-r");
            var first = await svc.StartResearchAsync(cmd);
            Assert.True(first.Succeeded);
            Assert.True(first.State.DailyRound?.OperationLaunched);
            long revision = first.State.Revision;
            var replay = await svc.StartResearchAsync(cmd);
            Assert.True(replay.Succeeded);
            Assert.Equal(revision, replay.State.Revision);
            Assert.Equal(first.State.DailyRound, replay.State.DailyRound);
            var disabledRepo = new DurableJsonHiveStateRepository(root + "-off", (x, y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0,
                new Dictionary<string, ResourceBalance> { ["honey"] = new(1000, 2000), ["pollen"] = new(500, 1000) }, new(), [], new()));
            var disabled = await new HiveOperationService(disabledRepo, clock, [], null, null, false).StartResearchAsync(cmd);
            Assert.True(disabled.Succeeded);
            Assert.Null(disabled.State.DailyRound);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); if (Directory.Exists(root + "-off")) Directory.Delete(root + "-off", true); }
    }

    private sealed class FixedClock(DateTimeOffset value) : IServerClock
    {
        public DateTimeOffset Now { get; set; } = value;
        public DateTimeOffset UtcNow => Now;
    }
}
