using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class AcademyTrainingServiceTests
{
    private static readonly DateTimeOffset Origin =
        new(2026, 10, 19, 13, 0, 0, TimeSpan.Zero);
    private sealed class Clock(DateTimeOffset now) : IServerClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    private static PlayerHiveState Seed(Guid player,Guid hive,int level = 4) =>
        new(player,hive,HiveStateMigrator.CurrentModelVersion,0,
            new Dictionary<string,ResourceBalance>
            {
                ["honey"] = new(10000,20000),
                ["pollen"] = new(5000,10000),
                ["wax"] = new(3000,10000)
            },
            new Dictionary<string,int> { ["academy_canopy"] = level },
            [],new());

    private static string Temp() => Path.Combine(Path.GetTempPath(),
        "bk-academy-"+Guid.NewGuid().ToString("N"));
    private static DurableJsonHiveStateRepository Repo(string root,int level = 4)
        => new(root,(p,h)=>Seed(p,h,level));
    private static AcademyTrainingService Service(
        DurableJsonHiveStateRepository repo,Clock clock,bool enabled = true)
        => new(repo,clock,new AcademyTrainingOptions{Enabled=enabled});
    private static async Task<(Guid,Guid)> Provision(
        DurableJsonHiveStateRepository repo)
    {
        Guid p=Guid.NewGuid(),h=Guid.NewGuid();
        await repo.ExecuteAtomicallyAsync(p,h,x=>x);
        return (p,h);
    }

    [Fact]
    public async Task Feature_disabled_does_not_read_or_mutate()
    {
        string dir=Temp();
        try
        {
            var repo=Repo(dir);
            var (p,h)=await Provision(repo);
            var service=Service(repo,new Clock(Origin),false);
            Assert.False(new AcademyTrainingOptions().Enabled);
            await Assert.ThrowsAsync<InvalidOperationException>(()=>service.ReadAsync(p,h));
            await Assert.ThrowsAsync<InvalidOperationException>(()=>service.StartAsync(
                p,h,"tactics",new(0,"academic-key-off")));
            Assert.Null((await repo.ReadAsync(p,h))!.Academy);
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
    }

    [Fact]
    public async Task Old_hives_are_compatible_and_have_no_free_certificates()
    {
        string dir=Temp();
        try
        {
            var repo=Repo(dir);
            var (p,h)=await Provision(repo);
            var snap=await Service(repo,new Clock(Origin)).ReadAsync(p,h);
            Assert.Equal(0,snap.EarnedCredits);
            Assert.Empty(snap.Completed);
            Assert.Null(snap.ActiveOperation);
            Assert.Equal(4,snap.AcademyBuildingLevel);
            Assert.Equal(4,snap.Courses.Count);
            Assert.Equal("locked",snap.Courses.Single(x=>x.CourseId=="logistics").State);
            Assert.Equal("available",snap.Courses.Single(x=>x.CourseId=="tactics").State);
            Assert.Equal(0,snap.Revision);
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
    }

    [Fact]
    public async Task Start_is_atomic_deducts_resources_once_and_replays_identically()
    {
        string dir=Temp();
        try
        {
            var repo=Repo(dir);
            var (p,h)=await Provision(repo);
            var service=Service(repo,new Clock(Origin));
            var request=new AcademyTrainingMutationRequest(0,"start-tactics-001");
            var first=await service.StartAsync(p,h,"tactics",request);
            var second=await service.StartAsync(p,h,"tactics",request);
            Assert.True(first.Succeeded);
            Assert.True(second.Succeeded);
            Assert.Equal(first.Receipt,second.Receipt);
            Assert.Equal(first.Snapshot.ActiveOperation!.OperationId,
                second.Snapshot.ActiveOperation!.OperationId);
            Assert.Equal(1,second.Snapshot.Revision);
            Assert.Equal(9760,second.Snapshot.Balances["honey"].Amount);
            Assert.Equal(4925,second.Snapshot.Balances["pollen"].Amount);
            Assert.Null((await repo.ReadAsync(p,h))!.Academy!.Completed.FirstOrDefault());
            Assert.Equal("game.idempotency_conflict",
                (await service.StartAsync(p,h,"tactics",new(1,"start-tactics-001"))).Code);
            Assert.Equal("game.academy_busy",
                (await service.StartAsync(p,h,"tactics",new(1,"start-tactics-002"))).Code);
            Assert.Equal("game.revision_conflict",
                (await service.StartAsync(p,h,"tactics",new(0,"start-tactics-003"))).Code);
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
    }

    [Fact]
    public async Task Must_wait_for_server_time_then_claim_certificate_once()
    {
        string dir=Temp();
        try
        {
            var repo=Repo(dir);
            var (p,h)=await Provision(repo);
            var clock=new Clock(Origin);
            var service=Service(repo,clock);
            var started=await service.StartAsync(p,h,"tactics",new(0,"start-tactics-111"));
            Guid id=started.Snapshot.ActiveOperation!.OperationId;
            var tooSoon=await service.CompleteAsync(p,h,id,new(1,"end-tactics-111"));
            Assert.False(tooSoon.Succeeded);
            Assert.Equal("game.academy_not_ready",tooSoon.Code);
            Assert.Equal(1,tooSoon.Snapshot.Revision);
            clock.UtcNow=Origin.AddMinutes(3);
            Assert.Equal("awaiting_completion",
                (await service.ReadAsync(p,h)).ActiveOperation!.Status);
            var finished=await service.CompleteAsync(p,h,id,new(1,"end-tactics-111"));
            Assert.True(finished.Succeeded);
            Assert.Equal(2,finished.Snapshot.Revision);
            Assert.Equal(1,finished.Snapshot.EarnedCredits);
            Assert.Single(finished.Snapshot.Completed);
            Assert.Null(finished.Snapshot.ActiveOperation);
            Assert.Equal("available",finished.Snapshot.Courses.Single(
                c=>c.CourseId=="logistics").State);
            var replay=await service.CompleteAsync(p,h,id,new(1,"end-tactics-111"));
            Assert.True(replay.Succeeded);
            Assert.Equal(finished.Receipt,replay.Receipt);
            Assert.Equal(2,replay.Snapshot.Revision);
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
    }

    [Fact]
    public async Task Level_and_prerequisites_gate_real_course_enrollment()
    {
        string dir=Temp();
        try
        {
            var repo=Repo(dir,1);
            var (p,h)=await Provision(repo);
            var clock=new Clock(Origin);
            var service=Service(repo,clock);
            Assert.Equal("game.academy_prerequisite_missing",
                (await service.StartAsync(p,h,"mentorship",new(0,"start-mentor-001"))).Code);
            Assert.Equal("game.academy_prerequisite_missing",
                (await service.StartAsync(p,h,"logistics",new(0,"start-logist-001"))).Code);
            var t=await service.StartAsync(p,h,"tactics",new(0,"start-tactic-002"));
            Assert.True(t.Succeeded);
            clock.UtcNow=Origin.AddMinutes(3);
            var done=await service.CompleteAsync(p,h,
                t.Snapshot.ActiveOperation!.OperationId,new(1,"end-tactic-002"));
            Assert.True(done.Succeeded);
            Assert.Equal("game.academy_prerequisite_missing",
                (await service.StartAsync(p,h,"logistics",new(2,"start-logist-002"))).Code);
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
    }

    [Fact]
    public async Task Credentials_scope_and_restart_persistence_are_isolated()
    {
        string dir=Temp();
        try
        {
            var repo=Repo(dir);
            var (p,h)=await Provision(repo);
            var (other,otherHive)=await Provision(repo);
            var service=Service(repo,new Clock(Origin));
            Assert.True((await service.StartAsync(p,h,"tactics",
                new(0,"start-academy-333"))).Succeeded);
            Assert.Null((await repo.ReadAsync(other,otherHive))!.Academy);
            var restored=Service(Repo(dir),new Clock(Origin.AddMinutes(10)));
            Assert.Equal("awaiting_completion",
                (await restored.ReadAsync(p,h)).ActiveOperation!.Status);
            await Assert.ThrowsAsync<KeyNotFoundException>(()=>
                restored.ReadAsync(other,h));
            await Assert.ThrowsAsync<KeyNotFoundException>(()=>
                restored.StartAsync(other,h,"tactics",new(0,"cross-user-key")));
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
    }

    [Fact]
    public async Task Forged_completion_and_invalid_input_never_credit_a_diploma()
    {
        string dir=Temp();
        try
        {
            var repo=Repo(dir);
            var (p,h)=await Provision(repo);
            var service=Service(repo,new Clock(Origin));
            await Assert.ThrowsAsync<ArgumentException>(()=>
                service.StartAsync(p,h,"invented_course",new(0,"valid-academy-key")));
            await Assert.ThrowsAsync<ArgumentException>(()=>
                service.StartAsync(p,h,"tactics",new(-1,"bad")));
            var forged=await service.CompleteAsync(
                p,h,Guid.NewGuid(),new(0,"forged-completion"));
            Assert.False(forged.Succeeded);
            Assert.Empty((await service.ReadAsync(p,h)).Completed);
            Assert.Equal(0,(await service.ReadAsync(p,h)).Revision);
        }
        finally { if(Directory.Exists(dir)) Directory.Delete(dir,true); }
    }
}
