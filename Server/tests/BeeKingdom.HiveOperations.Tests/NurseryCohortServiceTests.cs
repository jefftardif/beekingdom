using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class NurseryCohortServiceTests
{
    private static readonly DateTimeOffset Origin =
        new(2026, 10, 14, 18, 0, 0, TimeSpan.Zero);

    private sealed class MutableClock(DateTimeOffset initial) : IServerClock
    {
        public DateTimeOffset UtcNow { get; set; } = initial;
    }

    private static PlayerHiveState Seed(Guid player, Guid hive, int nurseryLevel = 1,
        bool hasVitality = true, NurseryCohortState? nursery = null)
    {
        return new PlayerHiveState(player, hive,
            HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance> {
                ["honey"] = new(10_000, 20_000),
                ["wax"] = new(1000, 5000),
                ["pollen"] = new(2000, 5000)
            },
            nurseryLevel == 0 ? new() : new() { ["nursery_cluster"] = nurseryLevel },
            [], new(),
            BroodVitality: hasVitality ? new(65,65,0,Origin,null) : null,
            NurseryCohorts: nursery);
    }

    private static NurseryCohortService Service(
        string root, MutableClock clock, bool enabled = true,
        Func<Guid,Guid,PlayerHiveState>? seed = null)
    {
        var repo = new DurableJsonHiveStateRepository(
            root, seed ?? ((p,h) => Seed(p,h)));
        return new NurseryCohortService(repo, clock, new NurseryCohortOptions {
            Enabled = enabled
        });
    }

    private static string DirectoryForTest() => Path.Combine(
        Path.GetTempPath(),"bk-nursery-cohort-"+Guid.NewGuid().ToString("N"));

    private static async Task ProvisionAsync(
        string root, Guid p, Guid h,
        Func<Guid, Guid, PlayerHiveState>? seed = null)
    {
        var repo = new DurableJsonHiveStateRepository(
            root, seed ?? ((x,y) => Seed(x,y)));
        await repo.ExecuteAtomicallyAsync(p,h,s => s);
    }

    [Fact]
    public async Task Feature_flag_is_off_by_default_and_cannot_mint_recruits()
    {
        string path = DirectoryForTest();
        try
        {
            var service = Service(path,new MutableClock(Origin),enabled: false);
            Guid p=Guid.NewGuid(),h=Guid.NewGuid();
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.StartAsync(new(p,h,0,"start-disabled")));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReadAsync(p,h));
            Assert.False(new NurseryCohortOptions().Enabled);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path,true); }
    }

    [Fact]
    public async Task Old_hive_without_cohorts_keeps_its_data_and_has_no_free_recruits()
    {
        string path = DirectoryForTest();
        try
        {
            var clock = new MutableClock(Origin);
            var service = Service(path,clock);
            Guid p=Guid.NewGuid(),h=Guid.NewGuid();
            await ProvisionAsync(path,p,h);
            NurseryCohortCommandResult started = await service.StartAsync(
                new(p,h,0,"first"));
            Assert.True(started.Succeeded);
            Assert.Equal(0,started.Snapshot.RecruitsInReserve);
            Assert.NotNull(started.Snapshot.Active);
            Assert.Equal("incubating",started.Snapshot.Status);
            Assert.Equal(1,started.Snapshot.HiveRevision);
            Assert.Equal(0,started.Snapshot.NextForecast!.RecruitsAlreadyInReserve);
            Assert.Equal(30,started.Snapshot.Active!.ExpectedRecruits);
            Assert.Equal(Origin.AddHours(1),started.Snapshot.Active.ReadyAtUtc);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path,true); }
    }

    [Fact]
    public async Task Exact_replay_does_not_create_a_second_cohort_or_revision()
    {
        string path = DirectoryForTest();
        try
        {
            var service = Service(path,new MutableClock(Origin));
            Guid p=Guid.NewGuid(),h=Guid.NewGuid();
            await ProvisionAsync(path,p,h);
            var req = new NurseryCohortStartCommand(p,h,0,"same-command");
            var first = await service.StartAsync(req);
            var second = await service.StartAsync(req);
            Assert.True(first.Succeeded);
            Assert.True(second.Succeeded);
            Assert.Equal(first.Receipt,second.Receipt);
            Assert.Equal(first.Snapshot.Active!.CohortId,
                second.Snapshot.Active!.CohortId);
            Assert.Equal(1,second.Snapshot.HiveRevision);
            Assert.Equal(0,second.Snapshot.RecruitsInReserve);
            Assert.Equal("game.idempotency_conflict",
                (await service.StartAsync(new(p,h,1,"same-command"))).Code);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path,true); }
    }

    [Fact]
    public async Task Different_second_command_cannot_parallel_start_a_second_cohort()
    {
        string path = DirectoryForTest();
        try
        {
            var service = Service(path,new MutableClock(Origin));
            Guid p=Guid.NewGuid(),h=Guid.NewGuid();
            await ProvisionAsync(path,p,h);
            Assert.True((await service.StartAsync(new(p,h,0,"first"))).Succeeded);
            var busy = await service.StartAsync(new(p,h,1,"second"));
            Assert.False(busy.Succeeded);
            Assert.Equal("game.nursery_cohort_busy",busy.Code);
            Assert.Single((await service.ReadAsync(p,h)).Active is null
                ? Array.Empty<NurseryActiveCohort>()
                : new[]{(await service.ReadAsync(p,h)).Active!});
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path,true); }
    }

    [Fact]
    public async Task Automatic_maturation_credits_reserve_once_even_after_repeated_reads()
    {
        string path = DirectoryForTest();
        try
        {
            var clock = new MutableClock(Origin);
            Guid p=Guid.NewGuid(),h=Guid.NewGuid();
            await ProvisionAsync(path,p,h);
            var service = Service(path,clock);
            Assert.True((await service.StartAsync(new(p,h,0,"batch1"))).Succeeded);
            clock.UtcNow=Origin.AddMinutes(59);
            Assert.Equal(0,(await service.ReadAsync(p,h)).RecruitsInReserve);
            clock.UtcNow=Origin.AddMinutes(60);
            NurseryCohortSnapshot mature = await service.ReadAsync(p,h);
            Assert.Null(mature.Active);
            Assert.Equal(30,mature.RecruitsInReserve);
            Assert.Equal(2,mature.HiveRevision);
            Assert.Equal(2,mature.CohortRevision);
            NurseryCohortSnapshot repeated = await service.ReadAsync(p,h);
            Assert.Equal(mature,repeated);
            Assert.Equal(30,repeated.RecruitsInReserve);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path,true); }
    }

    [Fact]
    public async Task Four_batches_fill_reserve_and_fifth_is_refused_without_loss()
    {
        string path = DirectoryForTest();
        try
        {
            var clock = new MutableClock(Origin);
            Guid p=Guid.NewGuid(),h=Guid.NewGuid();
            await ProvisionAsync(path,p,h);
            var service = Service(path,clock);
            for(int i=0;i<4;i++)
            {
                var read = i==0 ? null : await service.ReadAsync(p,h);
                long expected = read?.HiveRevision ?? 0;
                var started = await service.StartAsync(
                    new(p,h,expected,"cycle-"+i));
                Assert.True(started.Succeeded);
                clock.UtcNow=clock.UtcNow.AddHours(1);
                var done = await service.ReadAsync(p,h);
                Assert.Equal((i+1)*30,done.RecruitsInReserve);
            }
            NurseryCohortSnapshot final = await service.ReadAsync(p,h);
            Assert.False(final.CanStart);
            Assert.Equal("reserve_full",final.Status);
            var blocked = await service.StartAsync(
                new(p,h,final.HiveRevision,"overflow"));
            Assert.False(blocked.Succeeded);
            Assert.Equal("game.nursery_reserve_full",blocked.Code);
            Assert.Equal(120,(await service.ReadAsync(p,h)).RecruitsInReserve);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path,true); }
    }

    [Fact]
    public async Task Persisted_state_survives_repository_reopen_and_keeps_combat_army_intact()
    {
        string path = DirectoryForTest();
        try
        {
            var clock=new MutableClock(Origin);
            Guid p=Guid.NewGuid(),h=Guid.NewGuid();
            await ProvisionAsync(path,p,h);
            var service=Service(path,clock);
            var started=await service.StartAsync(new(p,h,0,"persistent-1"));
            Assert.True(started.Succeeded);
            var restored=Service(path,clock);
            Assert.Equal(started.Snapshot.Active!.CohortId,
                (await restored.ReadAsync(p,h)).Active!.CohortId);
            clock.UtcNow=Origin.AddHours(2);
            Assert.Equal(30,(await restored.ReadAsync(p,h)).RecruitsInReserve);
            var repo = new DurableJsonHiveStateRepository(
                path,(x,y)=>Seed(x,y));
            PlayerHiveState state=(await repo.ReadAsync(p,h))!;
            Assert.Equal(30,state.NurseryCohorts!.RecruitsInReserve);
            Assert.Null(state.DoctrineRoster);
            Assert.Equal(10_000,state.Resources["honey"].Amount);
            Assert.Equal(1000,state.Resources["wax"].Amount);
            Assert.Equal(65,state.BroodVitality!.Nutrition);
            Assert.Equal(65,state.BroodVitality.Stability);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path,true); }
    }

    [Fact]
    public async Task Invalid_ids_keys_levels_and_missing_brood_never_start_a_cohort()
    {
        string path = DirectoryForTest();
        try
        {
            Guid p=Guid.NewGuid(),h=Guid.NewGuid();
            var service=Service(path,new MutableClock(Origin));
            await Assert.ThrowsAsync<ArgumentException>(()=>
                service.StartAsync(new(Guid.Empty,h,0,"a")));
            await Assert.ThrowsAsync<ArgumentException>(()=>
                service.StartAsync(new(p,h,0,"bad key")));
            await ProvisionAsync(path,p,h,(x,y)=>Seed(x,y,nurseryLevel:0));
            var withoutLevel=Service(path,new MutableClock(Origin),seed:
                (x,y)=>Seed(x,y,nurseryLevel:0));
            Assert.Equal("game.nursery_not_built",
                (await withoutLevel.StartAsync(new(p,h,0,"no-building"))).Code);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path,true); }
        string other=DirectoryForTest();
        try
        {
            Guid missingPlayer=Guid.NewGuid(),missingHive=Guid.NewGuid();
            await ProvisionAsync(other,missingPlayer,missingHive,
                (x,y)=>Seed(x,y,hasVitality:false));
            var service=Service(other,new MutableClock(Origin),seed:
                (x,y)=>Seed(x,y,hasVitality:false));
            Assert.Equal("game.vitality_not_initialized",
                (await service.StartAsync(
                    new(missingPlayer,missingHive,0,"missing-vitality"))).Code);
        }
        finally { if (Directory.Exists(other)) Directory.Delete(other,true); }
    }

    [Fact]
    public async Task Concurrent_start_with_different_keys_can_succeed_only_once()
    {
        string path = DirectoryForTest();
        try
        {
            var service=Service(path,new MutableClock(Origin));
            Guid p=Guid.NewGuid(),h=Guid.NewGuid();
            await ProvisionAsync(path,p,h);
            var a=service.StartAsync(new(p,h,0,"race-a"));
            var b=service.StartAsync(new(p,h,0,"race-b"));
            var results=await Task.WhenAll(a,b);
            Assert.Single(results.Where(r=>r.Succeeded));
            Assert.Single(results.Where(r=>!r.Succeeded));
            Assert.Equal(1,(await service.ReadAsync(p,h)).HiveRevision);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path,true); }
    }

    [Fact]
    public async Task Missing_hive_is_rejected_without_implicitly_creating_a_new_player_hive()
    {
        string path = DirectoryForTest();
        try
        {
            Guid p=Guid.NewGuid(),h=Guid.NewGuid();
            var service=Service(path,new MutableClock(Origin));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.StartAsync(new(p,h,0,"never-create")));
            var repo = new DurableJsonHiveStateRepository(
                path,(x,y)=>Seed(x,y));
            Assert.Null(await repo.ReadAsync(p,h));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path,true); }
    }

    [Fact]
    public void Migration_validation_rejects_corrupted_or_overcapacity_cohorts()
    {
        Guid p=Guid.NewGuid(),h=Guid.NewGuid();
        NurseryCohortState bad = new(0,120,new(
            Guid.NewGuid(),Origin,Origin.AddMinutes(30),30,65,65),
            new Dictionary<string,IdempotencyReceipt>());
        PlayerHiveState state=Seed(p,h,nursery:bad);
        Assert.Throws<InvalidDataException>(()=>
            HiveStateMigrator.ToCurrent(state));
        Assert.Null(HiveStateMigrator.ToCurrent(
            Seed(p,h)).NurseryCohorts);
    }
}
