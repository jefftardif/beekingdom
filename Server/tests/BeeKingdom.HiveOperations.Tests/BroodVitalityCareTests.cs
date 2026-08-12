using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class BroodVitalityCareTests
{
    [Fact] public async Task Invalid_keys_types_and_scope_are_rejected_before_mutation()
    { var p=Guid.NewGuid();var h=Guid.NewGuid();var now=DateTimeOffset.UtcNow;var root=Path.Combine(Path.GetTempPath(),"bc-invalid-"+Guid.NewGuid());try{var repo=new DurableJsonHiveStateRepository(root,(x,y)=>new PlayerHiveState(x,y,HiveStateMigrator.CurrentModelVersion,0,new Dictionary<string,ResourceBalance>{{"honey",new(500,500)},{"wax",new(50,50)},{"pollen",new(0,10)}},new(),[],new(),BroodVitality:new(20,20,0,now,null)));var svc=new BroodVitalityCareService(repo,new MutableClock(now));await Assert.ThrowsAsync<ArgumentException>(()=>svc.StartAsync(Guid.Empty,h,"feeding",new(0,"x")));await Assert.ThrowsAsync<ArgumentException>(()=>svc.StartAsync(p,h,"unknown",new(0,"x")));await Assert.ThrowsAsync<ArgumentException>(()=>svc.StartAsync(p,h,"feeding",new(0,"bad key")));}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
    [Fact] public async Task Absent_vitality_busy_and_early_completion_are_deterministic()
    { var p=Guid.NewGuid();var h=Guid.NewGuid();var now=DateTimeOffset.UtcNow;var root=Path.Combine(Path.GetTempPath(),"bc-busy-"+Guid.NewGuid());try{var repo=new DurableJsonHiveStateRepository(root,(x,y)=>new PlayerHiveState(x,y,HiveStateMigrator.CurrentModelVersion,0,new Dictionary<string,ResourceBalance>{{"honey",new(0,500)},{"wax",new(50,50)},{"pollen",new(0,10)}},new(),[],new()));var svc=new BroodVitalityCareService(repo,new MutableClock(now));var absent=await svc.StartAsync(p,h,"feeding",new(0,"a"));Assert.Equal("game.vitality_not_initialized",absent.Code);await repo.ExecuteAtomicallyAsync(p,h,s=>s with{BroodVitality=new(20,20,0,now,new(Guid.NewGuid(),"stabilization",now,now.AddSeconds(13)))});var busy=await svc.StartAsync(p,h,"feeding",new(0,"b"));Assert.Equal("game.vitality_busy",busy.Code);}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
    [Fact] public async Task Stabilization_caps_at_one_hundred_and_uses_thirteen_seconds()
    { var p=Guid.NewGuid();var h=Guid.NewGuid();var now=DateTimeOffset.UtcNow;var root=Path.Combine(Path.GetTempPath(),"bc-stab-"+Guid.NewGuid());try{var clock=new MutableClock(now);var repo=new DurableJsonHiveStateRepository(root,(x,y)=>new PlayerHiveState(x,y,HiveStateMigrator.CurrentModelVersion,0,new Dictionary<string,ResourceBalance>{{"honey",new(0,500)},{"wax",new(50,100)},{"pollen",new(0,10)}},new(),[],new(),BroodVitality:new(20,99,0,now,null)));var svc=new BroodVitalityCareService(repo,clock);var s=await svc.StartAsync(p,h,"stabilization",new(0,"s"));Assert.True(s.Succeeded);Assert.Equal(5,s.State.Resources["wax"].Amount);clock.Now=clock.Now.AddSeconds(13);var c=await svc.CompleteAsync(p,h,s.Receipt!.OperationId,new(s.State.Revision,"c"));Assert.True(c.Succeeded);Assert.Equal(100,c.State.BroodVitality!.Stability);}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
    [Fact] public async Task Max_vitality_revision_and_overflow_are_rejected()
    { var p=Guid.NewGuid();var h=Guid.NewGuid();var now=DateTimeOffset.UtcNow;var root=Path.Combine(Path.GetTempPath(),"bc-overflow-"+Guid.NewGuid());try{var repo=new DurableJsonHiveStateRepository(root,(x,y)=>new PlayerHiveState(x,y,HiveStateMigrator.CurrentModelVersion,long.MaxValue-1,new Dictionary<string,ResourceBalance>{{"honey",new(500,500)},{"wax",new(50,50)},{"pollen",new(0,10)}},new(),[],new(),BroodVitality:new(20,20,long.MaxValue,now,null)));var svc=new BroodVitalityCareService(repo,new MutableClock(now));await Assert.ThrowsAsync<InvalidOperationException>(()=>svc.StartAsync(p,h,"feeding",new(long.MaxValue-1,"x")));}finally{if(Directory.Exists(root))Directory.Delete(root,true);}}
    [Fact]
    public async Task Start_and_complete_replay_exactly_from_dedicated_receipts()
    {
        var p = Guid.NewGuid(); var h = Guid.NewGuid(); var now = new DateTimeOffset(2026, 7, 23, 12, 0, 0, TimeSpan.Zero);
        var clock = new MutableClock(now); var root = Path.Combine(Path.GetTempPath(), "brood-care-" + Guid.NewGuid());
        try
        {
            var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0,
                new Dictionary<string, ResourceBalance> { ["honey"] = new(500, 1000), ["wax"] = new(100, 1000), ["pollen"] = new(0, 1000) }, new(), [], new(), BroodVitality: new BroodVitalityState(50, 50, 0, now, null)));
            var svc = new BroodVitalityCareService(repo, clock);
            var started = await svc.StartAsync(p, h, BroodVitalityOperationTypes.Feeding, new(0, "start-1"));
            Assert.True(started.Succeeded); Assert.NotNull(started.Receipt); Assert.Single(started.State.BroodCareReceipts!);
            var replayStart = await svc.StartAsync(p, h, BroodVitalityOperationTypes.Feeding, new(0, "start-1"));
            Assert.Equal(started.Receipt, replayStart.Receipt); Assert.Equal(started.State.Revision, replayStart.State.Revision); Assert.Equal(200, replayStart.State.Resources["honey"].Amount);
            clock.Now = clock.Now.AddSeconds(12);
            var completed = await svc.CompleteAsync(p, h, started.Receipt!.OperationId, new(started.State.Revision, "complete-1"));
            Assert.True(completed.Succeeded); Assert.Null(completed.State.BroodVitality!.ActiveOperation); Assert.Equal(72, completed.State.BroodVitality.Nutrition);
            var replayComplete = await svc.CompleteAsync(p, h, started.Receipt.OperationId, new(started.State.Revision, "complete-1"));
            Assert.Equal(completed.Receipt, replayComplete.Receipt); Assert.Equal(completed.State.Revision, replayComplete.State.Revision); Assert.Equal(72, replayComplete.State.BroodVitality!.Nutrition);
            var conflict = await svc.CompleteAsync(p, h, started.Receipt.OperationId, new(started.State.Revision + 1, "complete-1"));
            Assert.Equal("game.idempotency_conflict", conflict.Code);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Invalid_or_insufficient_care_does_not_create_receipts()
    {
        var p = Guid.NewGuid(); var h = Guid.NewGuid(); var now = DateTimeOffset.UtcNow; var root = Path.Combine(Path.GetTempPath(), "brood-care-invalid-" + Guid.NewGuid());
        try
        {
            var repo = new DurableJsonHiveStateRepository(root, (x, y) => new PlayerHiveState(x, y, HiveStateMigrator.CurrentModelVersion, 0, new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 1), ["wax"] = new(0, 1), ["pollen"] = new(0, 1) }, new(), [], new(), BroodVitality: new BroodVitalityState(10, 10, 0, now, null)));
            var svc = new BroodVitalityCareService(repo, new MutableClock(now));
            var result = await svc.StartAsync(p, h, BroodVitalityOperationTypes.Feeding, new(0, "x"));
            Assert.False(result.Succeeded); Assert.Equal("game.insufficient_resources", result.Code); Assert.True(result.State.BroodCareReceipts is null or { Count: 0 });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Early_completion_revision_conflict_and_exact_completion_are_distinct()
    {
        Guid playerId = Guid.NewGuid();
        Guid hiveId = Guid.NewGuid();
        DateTimeOffset now =
            new(2026, 7, 23, 15, 0, 0, TimeSpan.Zero);
        var clock = new MutableClock(now);
        string root = Path.Combine(
            Path.GetTempPath(),
            "brood-care-timing-" + Guid.NewGuid());
        try
        {
            var repository = new DurableJsonHiveStateRepository(
                root,
                (player, hive) => NewState(player, hive, now));
            var service = new BroodVitalityCareService(repository, clock);
            BroodVitalityCareResult started = await service.StartAsync(
                playerId,
                hiveId,
                BroodVitalityOperationTypes.Feeding,
                new(0, "timing-start"));

            BroodVitalityCareResult early = await service.CompleteAsync(
                playerId,
                hiveId,
                started.Receipt!.OperationId,
                new(1, "timing-complete"));
            Assert.False(early.Succeeded);
            Assert.Equal("game.vitality_not_ready", early.Code);
            Assert.Equal(1, early.State.Revision);
            Assert.NotNull(early.State.BroodVitality!.ActiveOperation);

            clock.Now = now.AddSeconds(12);
            BroodVitalityCareResult stale = await service.CompleteAsync(
                playerId,
                hiveId,
                started.Receipt.OperationId,
                new(0, "timing-stale"));
            Assert.False(stale.Succeeded);
            Assert.Equal("game.revision_conflict", stale.Code);

            BroodVitalityCareResult completed = await service.CompleteAsync(
                playerId,
                hiveId,
                started.Receipt.OperationId,
                new(1, "timing-complete"));
            Assert.True(completed.Succeeded);
            Assert.Equal(2, completed.State.Revision);
            Assert.Equal(72, completed.State.BroodVitality!.Nutrition);
            Assert.Null(completed.State.BroodVitality.ActiveOperation);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Fresh_start_marks_daily_round_once_when_enabled()
    {
        Guid playerId = Guid.NewGuid();
        Guid hiveId = Guid.NewGuid();
        DateTimeOffset now =
            new(2026, 7, 23, 16, 0, 0, TimeSpan.Zero);
        string root = Path.Combine(
            Path.GetTempPath(),
            "brood-care-round-" + Guid.NewGuid());
        try
        {
            var repository = new DurableJsonHiveStateRepository(
                root,
                (player, hive) => NewState(player, hive, now));
            var service = new BroodVitalityCareService(
                repository,
                new MutableClock(now),
                dailyRoundEnabled: true);

            BroodVitalityCareResult started = await service.StartAsync(
                playerId,
                hiveId,
                BroodVitalityOperationTypes.Feeding,
                new(0, "round-start"));
            Assert.True(started.Succeeded);
            Assert.True(started.State.DailyRound!.OperationLaunched);
            Assert.Equal(1, started.State.Revision);

            BroodVitalityCareResult replay = await service.StartAsync(
                playerId,
                hiveId,
                BroodVitalityOperationTypes.Feeding,
                new(0, "round-start"));
            Assert.Equal(started.Receipt, replay.Receipt);
            Assert.Equal(1, replay.State.Revision);
            Assert.True(replay.State.DailyRound!.OperationLaunched);
            Assert.Equal(200, replay.State.Resources["honey"].Amount);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Receipt_bound_and_migrator_guards_are_enforced()
    {
        Guid playerId = Guid.NewGuid();
        Guid hiveId = Guid.NewGuid();
        DateTimeOffset now =
            new(2026, 7, 23, 17, 0, 0, TimeSpan.Zero);
        var receipts = Enumerable.Range(0, 128).ToDictionary(
            index => "receipt-" + index,
            index => new BroodCareStoredReceipt(
                new string('a', 64),
                true,
                BroodVitalityOperationTypes.Feeding,
                Guid.NewGuid(),
                0,
                1,
                now,
                "game.vitality_care_started"),
            StringComparer.Ordinal);
        PlayerHiveState valid = NewState(playerId, hiveId, now) with
        {
            Revision = 128,
            BroodCareReceipts = receipts
        };

        Assert.Equal(128, HiveStateMigrator.ToCurrent(valid)
            .BroodCareReceipts!.Count);

        var tooMany =
            new Dictionary<string, BroodCareStoredReceipt>(
                receipts,
                StringComparer.Ordinal)
            {
                ["receipt-overflow"] = receipts["receipt-0"]
            };
        Assert.Throws<InvalidDataException>(() =>
            HiveStateMigrator.ToCurrent(valid with
            {
                BroodCareReceipts = tooMany
            }));
        Assert.Throws<InvalidDataException>(() =>
            HiveStateMigrator.ToCurrent(valid with
            {
                BroodCareReceipts =
                    new Dictionary<string, BroodCareStoredReceipt>
                    {
                        ["failed-receipt"] =
                            receipts["receipt-0"] with
                            {
                                Succeeded = false
                            }
                    }
            }));
        Assert.Throws<InvalidOperationException>(() =>
            HiveStateMigrator.ToCurrent(valid with
            {
                BroodVitality =
                    valid.BroodVitality! with
                    {
                        Revision = 129
                    }
            }));
    }

    private static PlayerHiveState NewState(
        Guid playerId,
        Guid hiveId,
        DateTimeOffset now)
    {
        return new PlayerHiveState(
            playerId,
            hiveId,
            HiveStateMigrator.CurrentModelVersion,
            0,
            new Dictionary<string, ResourceBalance>
            {
                ["honey"] = new(500, 1000),
                ["wax"] = new(100, 1000),
                ["pollen"] = new(0, 1000)
            },
            new(),
            [],
            new(),
            BroodVitality:
                new BroodVitalityState(50, 50, 0, now, null));
    }

    private sealed class MutableClock(DateTimeOffset value) : IServerClock { public DateTimeOffset Now { get; set; } = value; public DateTimeOffset UtcNow => Now; }
}
