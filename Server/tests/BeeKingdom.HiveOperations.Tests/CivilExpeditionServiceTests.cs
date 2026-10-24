using BeeKingdom.HiveOperations;
using Xunit;

namespace BeeKingdom.HiveOperations.Tests;

public sealed class CivilExpeditionServiceTests
{
    private static readonly DateTimeOffset Origin =
        new(2026, 10, 21, 18, 0, 0, TimeSpan.Zero);

    private sealed class MutableClock(DateTimeOffset time) : IServerClock
    {
        public DateTimeOffset UtcNow { get; set; } = time;
    }

    private sealed class AtomicTestWorld :
        IHiveStateRepository, ICivilExpeditionUnitOfWork, IFloralSiteRepository, ICivilExpeditionDueIndex
    {
        private readonly SemaphoreSlim gate = new(1, 1);
        private readonly Dictionary<(Guid, Guid), PlayerHiveState> hives = new();
        private readonly Dictionary<string, FloralSiteState> sites = new();
        private readonly Dictionary<Guid, CivilExpeditionContribution> contributions = new();

        public bool FailBeforeCommit { get; set; }
        public bool FailAfterCommit { get; set; }

        public void AddHive(PlayerHiveState hive) =>
            hives[(hive.PlayerId, hive.HiveId)] = hive;

        public IReadOnlyCollection<CivilExpeditionContribution> Ledger => contributions.Values;
        public FloralSiteState? GlobalSite(string site) => sites.GetValueOrDefault(site);

        public async Task<CivilExpeditionMutation> ExecuteAsync(
            Guid playerId, Guid hiveId, FloralSiteDefinition definition,
            string version,
            Func<PlayerHiveState, FloralSiteState, CivilExpeditionMutation> change,
            CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                if (!hives.TryGetValue((playerId, hiveId), out var oldHive))
                    throw new KeyNotFoundException("game.not_found");
                bool existed = sites.TryGetValue(definition.SiteId, out var stored);
                FloralSiteState oldSite = stored ?? FloralSiteService.InitialState(
                    definition, version, Origin);
                var candidate = change(HiveStateMigrator.ToCurrent(oldHive), oldSite);
                if (FailBeforeCommit)
                {
                    FailBeforeCommit = false;
                    throw new IOException("Injected failure before cross-aggregate commit.");
                }
                if (candidate.Contribution is { } receipt
                    && contributions.ContainsKey(receipt.MissionId))
                    throw new InvalidDataException("Duplicate contribution mission ID.");
                hives[(playerId, hiveId)] = candidate.Hive;
                sites[definition.SiteId] = candidate.Site;
                if (candidate.Contribution is { } row)
                    contributions.Add(row.MissionId, row);
                if (FailAfterCommit)
                {
                    FailAfterCommit = false;
                    throw new IOException("Simulated network loss after commit.");
                }
                return candidate;
            }
            finally { gate.Release(); }
        }

        public async Task<FloralSiteState> ReadOrCreateAsync(
            FloralSiteDefinition definition, string version,
            DateTimeOffset nowUtc, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                if (!sites.TryGetValue(definition.SiteId, out var stored))
                {
                    stored = FloralSiteService.InitialState(definition, version, nowUtc);
                    sites[definition.SiteId] = stored;
                }
                return stored;
            }
            finally { gate.Release(); }
        }

        public async Task<PlayerHiveState?> ReadAsync(
            Guid playerId, Guid hiveId, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                return hives.TryGetValue((playerId, hiveId), out var state)
                    ? HiveStateMigrator.ToCurrent(state) : null;
            }
            finally { gate.Release(); }
        }

        public async Task<PlayerHiveState> ExecuteAtomicallyAsync(
            Guid playerId, Guid hiveId, Func<PlayerHiveState, PlayerHiveState> mutation,
            CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                if (!hives.TryGetValue((playerId, hiveId), out var state))
                    throw new KeyNotFoundException("game.not_found");
                PlayerHiveState result = mutation(HiveStateMigrator.ToCurrent(state));
                hives[(playerId, hiveId)] = result;
                return result;
            }
            finally { gate.Release(); }
        }

        public async Task<IReadOnlyList<CivilDueExpedition>> DueAsync(
            DateTimeOffset nowUtc, int limit, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                return hives.Values
                    .Where(x => x.CivilExpeditions?.Active is { } flight
                        && flight.ReturnsAtUtc <= nowUtc)
                    .Select(x => new CivilDueExpedition(
                        x.CivilExpeditions!.Active!.MissionId,
                        x.PlayerId, x.HiveId,
                        x.CivilExpeditions!.Active!.ReturnsAtUtc))
                    .OrderBy(x => x.ReturnsAtUtc).ThenBy(x => x.MissionId)
                    .Take(limit).ToArray();
            }
            finally { gate.Release(); }
        }
        public Task<IReadOnlyList<Guid>> ListHiveIdsAsync(
            Guid playerId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(hives.Keys
                .Where(x => x.Item1 == playerId).Select(x => x.Item2).ToList());

        public Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(
            int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PlayerHiveState>>(
                hives.Values.Take(limit).ToList());

        public Task<bool> DeleteAsync(
            Guid playerId, Guid hiveId, CancellationToken ct = default) =>
            Task.FromResult(hives.Remove((playerId, hiveId)));
    }

    private static FloralSiteOptions Flora(long reserve = 100) => new()
    {
        Enabled = true, CatalogVersion = "alpha-qa-20261021",
        Sites = [new("world-qa", "region-qa", "clover-field-qa",
            "clover", "Champ de trèfle", "Clover Field", reserve, 6000, 5500)]
    };
    private static CivilExpeditionOptions Rules(bool enabled = true) => new()
    {
        Enabled = enabled, DurationMinutes = 15, MaxWorkers = 100,
        PollenPerWorker = 1, FertilityGainBp = 5, BiodiversityGainBp = 2
    };

    private static PlayerHiveState Hive(Guid playerId, Guid hiveId,
        long workers = 100, long pollen = 0, long capacity = 200) =>
        new(playerId, hiveId, HiveStateMigrator.CurrentModelVersion, 0,
            new Dictionary<string, ResourceBalance>
            {
                ["pollen"] = new(pollen, capacity),
                ["honey"] = new(500, 1000),
                ["wax"] = new(200, 500)
            },
            new Dictionary<string, int> { ["nursery_cluster"] = 1 },
            [], new Dictionary<string, IdempotencyReceipt>(),
            CivilPopulation: new CivilPopulationState(
                0, workers, 0, 0, null, new()));

    private static CivilExpeditionService Service(
        AtomicTestWorld db, MutableClock clock,
        FloralSiteOptions flora, CivilExpeditionOptions? rules = null) =>
        new(db, db, clock, rules ?? Rules(), flora);

    private static StartCivilExpeditionRequest Start(
        string key, long workers = 20, long hiveRevision = 0,
        long siteRevision = 0) =>
        new("clover-field-qa", workers, hiveRevision, siteRevision, key);

    [Fact]
    public async Task Civilians_reach_field_offline_and_return_with_real_site_debit_once()
    {
        var db = new AtomicTestWorld();
        var clock = new MutableClock(Origin);
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
        db.AddHive(Hive(player, hive));
        var flora = Flora(50);
        var service = Service(db, clock, flora);
        var started = await service.StartAsync(
            player, hive, Start("mission-one-qa-01", 20));
        Assert.True(started.Succeeded);
        Assert.Equal("outbound", started.Snapshot.Status);
        Assert.Equal(80, started.Snapshot.AvailableWorkers);
        Assert.Equal(20, started.Snapshot.AssignedWorkers);
        Assert.Equal(50, db.GlobalSite("clover-field-qa")!.PollenRemaining);
        Assert.Empty(db.Ledger);

        clock.UtcNow = Origin.AddMinutes(6);
        Assert.Equal("pollinating",
            (await service.ReadAsync(player, hive)).Status);
        clock.UtcNow = Origin.AddMinutes(11);
        Assert.Equal("returning",
            (await service.ReadAsync(player, hive)).Status);

        // Simulated Unity exit and recreation of client/service, while the
        // same persistent store and server UTC clock survive.
        clock.UtcNow = Origin.AddMinutes(60);
        var reopened = Service(db, clock, flora);
        CivilExpeditionSnapshot completed = await reopened.ReadAsync(player, hive);
        Assert.Equal("idle", completed.Status);
        Assert.Null(completed.Active);
        Assert.Equal(100, completed.AvailableWorkers);
        Assert.Equal(0, completed.AssignedWorkers);
        Assert.Equal(20, completed.LastCompletion!.PollenCredited);
        Assert.Equal(30, db.GlobalSite("clover-field-qa")!.PollenRemaining);
        Assert.Equal(1, db.GlobalSite("clover-field-qa")!.Revision);
        Assert.Single(db.Ledger);
        Assert.Equal(20, (await db.ReadAsync(player, hive))!.Resources["pollen"].Amount);
        await reopened.ReadAsync(player, hive);
        Assert.Single(db.Ledger);
        Assert.Equal(20, (await db.ReadAsync(player, hive))!.Resources["pollen"].Amount);
        Assert.Null((await db.ReadAsync(player, hive))!.DoctrineRoster);
    }

    [Fact]
    public async Task Two_players_compete_for_one_finite_field_and_both_contribute()
    {
        var db = new AtomicTestWorld();
        var clock = new MutableClock(Origin);
        var flora = Flora(15);
        Guid a = Guid.NewGuid(), b = Guid.NewGuid();
        Guid ah = Guid.NewGuid(), bh = Guid.NewGuid();
        db.AddHive(Hive(a, ah)); db.AddHive(Hive(b, bh));
        var service = Service(db, clock, flora);
        CivilExpeditionResult[] starts = await Task.WhenAll(
            service.StartAsync(a, ah, Start("player-a-start-01", workers:12)),
            service.StartAsync(b, bh, Start("player-b-start-01", workers:12)));
        Assert.All(starts, result => Assert.True(result.Succeeded));
        clock.UtcNow = Origin.AddMinutes(16);
        CivilExpeditionSnapshot[] final = await Task.WhenAll(
            service.ReadAsync(a, ah), service.ReadAsync(b, bh));
        Assert.All(final, x => Assert.Equal(0, x.AssignedWorkers));

        var site = db.GlobalSite("clover-field-qa")!;
        Assert.Equal(0, site.PollenRemaining);
        Assert.Equal(2, site.ContributionPointsByPlayer!.Count);
        Assert.Equal(15, site.TotalPollinationPoints);
        Assert.Equal(15, db.Ledger.Sum(x => x.PollenCredited));
        Assert.Equal(2, db.Ledger.Count);
        Assert.Equal(15, (await db.ReadAsync(a, ah))!.Resources["pollen"].Amount
            + (await db.ReadAsync(b, bh))!.Resources["pollen"].Amount);
        FloralSiteSnapshot publicView = await new FloralSiteService(
            db, clock, flora).ReadAsync("clover-field-qa");
        Assert.Equal(15, publicView.TotalPollinationPoints);
        Assert.Equal(2, publicView.DistinctContributors);
        Assert.Equal(0, publicView.PollenRemaining);
        Assert.Equal(6010, publicView.FertilityBp);
        await Task.WhenAll(service.ReadAsync(a, ah), service.ReadAsync(b, bh));
        Assert.Equal(2, db.Ledger.Count);
    }

    [Fact]
    public async Task Retry_after_lost_http_response_never_double_reserves_civilians()
    {
        var db = new AtomicTestWorld { FailAfterCommit = true };
        var clock = new MutableClock(Origin);
        Guid player=Guid.NewGuid(), hive=Guid.NewGuid();
        db.AddHive(Hive(player,hive));
        var service = Service(db, clock, Flora());
        var req=Start("idempotent-retry-01", workers:25);
        await Assert.ThrowsAsync<IOException>(() =>
            service.StartAsync(player,hive,req));
        var replay=await service.StartAsync(player,hive,req);
        Assert.True(replay.Succeeded);
        Assert.Equal(25, replay.Snapshot.AssignedWorkers);
        Assert.Equal(75, replay.Snapshot.AvailableWorkers);
        Assert.Equal(1, replay.Snapshot.HiveRevision);
        Assert.Equal((await db.ReadAsync(player,hive))!.CivilExpeditions!.Active!.MissionId,
            replay.Receipt!.MissionId);
        var conflict=await service.StartAsync(player,hive,
            Start("idempotent-retry-01", workers:15));
        Assert.False(conflict.Succeeded);
        Assert.Equal("game.idempotency_conflict", conflict.Code);
    }

    [Fact]
    public async Task Before_commit_failure_does_not_partially_debit_or_release()
    {
        var db=new AtomicTestWorld();
        var clock=new MutableClock(Origin);
        Guid p=Guid.NewGuid(), h=Guid.NewGuid();
        db.AddHive(Hive(p,h));
        var service=Service(db,clock,Flora(45));
        Assert.True((await service.StartAsync(p,h,
            Start("single-crash-01",workers:30))).Succeeded);
        clock.UtcNow=Origin.AddMinutes(35);
        db.FailBeforeCommit=true;
        await Assert.ThrowsAsync<IOException>(() => service.ReadAsync(p,h));
        Assert.Equal(45,db.GlobalSite("clover-field-qa")!.PollenRemaining);
        Assert.Empty(db.Ledger);
        Assert.Equal(30,(await db.ReadAsync(p,h))!.CivilPopulation!.AssignedWorkers);
        Assert.Equal(0,(await db.ReadAsync(p,h))!.Resources["pollen"].Amount);
        var resumed=await service.ReadAsync(p,h);
        Assert.Equal(0,resumed.AssignedWorkers);
        Assert.Equal(30,resumed.LastCompletion!.PollenCredited);
        Assert.Single(db.Ledger);
        Assert.Equal(15,db.GlobalSite("clover-field-qa")!.PollenRemaining);
    }

    [Fact]
    public async Task Simultaneous_launches_stale_revisions_and_capacity_reject_safely()
    {
        var db=new AtomicTestWorld();
        var clock=new MutableClock(Origin);
        Guid p=Guid.NewGuid(),h=Guid.NewGuid();
        db.AddHive(Hive(p,h,workers:20));
        var svc=Service(db,clock,Flora());
        CivilExpeditionResult[] starts=await Task.WhenAll(
            svc.StartAsync(p,h,Start("race-start-first",workers:15)),
            svc.StartAsync(p,h,Start("race-start-other",workers:15)));
        Assert.Single(starts.Where(x=>x.Succeeded));
        Assert.Single(starts.Where(x=>!x.Succeeded));
        Assert.Equal(15,(await db.ReadAsync(p,h))!.CivilPopulation!.AssignedWorkers);
        var invalid=await svc.StartAsync(p,h,Start("bad-site-revision",5,1,99));
        Assert.Equal("game.revision_conflict",invalid.Code);
        Assert.Empty(db.Ledger);
    }

    [Fact]
    public async Task Warehouse_full_leaves_flower_stock_untouched_and_releases_workers()
    {
        var db=new AtomicTestWorld();
        var clock=new MutableClock(Origin);
        Guid p=Guid.NewGuid(),h=Guid.NewGuid();
        db.AddHive(Hive(p,h,workers:25,pollen:200,capacity:200));
        var svc=Service(db,clock,Flora(40));
        Assert.True((await svc.StartAsync(p,h,Start("full-warehouse-01",20))).Succeeded);
        clock.UtcNow=Origin.AddMinutes(20);
        CivilExpeditionSnapshot result=await svc.ReadAsync(p,h);
        Assert.Equal(0,result.LastCompletion!.PollenCredited);
        Assert.Equal(25,result.AvailableWorkers);
        Assert.Equal(40,db.GlobalSite("clover-field-qa")!.PollenRemaining);
        Assert.Empty(db.Ledger);
        Assert.Equal(0,db.GlobalSite("clover-field-qa")!.TotalPollinationPoints);
    }

    [Fact]
    public async Task Flags_off_and_foreign_hive_are_read_only()
    {
        var db=new AtomicTestWorld();
        var clock=new MutableClock(Origin);
        Guid p=Guid.NewGuid(),h=Guid.NewGuid();
        db.AddHive(Hive(p,h));
        var disabled=Service(db,clock,Flora(),Rules(false));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            disabled.StartAsync(p,h,Start("disabled-flight-01")));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            disabled.ReadAsync(p,h));
        var service=Service(db,clock,Flora());
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ReadAsync(Guid.NewGuid(),h));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.StartAsync(Guid.NewGuid(),h,Start("foreign-request-01")));
        Assert.Null((await db.ReadAsync(p,h))!.CivilExpeditions);
    }

    [Fact]
    public async Task Server_due_processor_settles_offline_flights_and_retries_failed_one()
    {
        var db = new AtomicTestWorld();
        var clock = new MutableClock(Origin);
        var flora = Flora(60);
        Guid a=Guid.NewGuid(), b=Guid.NewGuid();
        Guid ah=Guid.NewGuid(), bh=Guid.NewGuid();
        db.AddHive(Hive(a, ah));
        db.AddHive(Hive(b, bh));
        var service = Service(db, clock, flora);
        Assert.True((await service.StartAsync(a, ah,
            Start("due-player-alpha", workers:15))).Succeeded);
        Assert.True((await service.StartAsync(b, bh,
            Start("due-player-bravo", workers:15))).Succeeded);
        clock.UtcNow = Origin.AddHours(5);

        // No player calls GET: a server-only periodic due sweep completes both.
        var processor = new CivilExpeditionDueProcessor(db, service, clock);
        db.FailBeforeCommit = true;
        CivilExpeditionDueBatch first = await processor.ProcessAsync();
        Assert.Equal(2, first.Inspected);
        Assert.Equal(1, first.Settled);
        Assert.Single(first.RetryMissionIds);
        Assert.Single(db.Ledger);
        CivilExpeditionDueBatch second = await processor.ProcessAsync();
        Assert.Equal(1, second.Inspected);
        Assert.Equal(1, second.Settled);
        Assert.Empty(second.RetryMissionIds);
        Assert.Equal(2, db.Ledger.Count);
        Assert.Equal(30, db.GlobalSite("clover-field-qa")!.PollenRemaining);
        Assert.Equal(0, (await db.ReadAsync(a, ah))!.CivilPopulation!.AssignedWorkers);
        Assert.Equal(0, (await db.ReadAsync(b, bh))!.CivilPopulation!.AssignedWorkers);
        Assert.Equal(0, (await processor.ProcessAsync()).Inspected);
    }

    [Fact]
    public async Task Concurrent_server_workers_do_not_duplicate_pollination_credits()
    {
        var db = new AtomicTestWorld();
        var clock = new MutableClock(Origin);
        Guid p=Guid.NewGuid(), h=Guid.NewGuid();
        db.AddHive(Hive(p, h));
        var service = Service(db, clock, Flora(35));
        Assert.True((await service.StartAsync(p, h,
            Start("due-race-alpha", workers:20))).Succeeded);
        clock.UtcNow=Origin.AddHours(2);
        var processor = new CivilExpeditionDueProcessor(db, service, clock);
        await Task.WhenAll(processor.ProcessAsync(), processor.ProcessAsync());
        Assert.Single(db.Ledger);
        Assert.Equal(15, db.GlobalSite("clover-field-qa")!.PollenRemaining);
        Assert.Equal(20, (await db.ReadAsync(p, h))!.Resources["pollen"].Amount);
    }
    [Fact]
    public async Task Explicit_drain_mode_releases_reservations_when_both_features_are_off()
    {
        var db = new AtomicTestWorld();
        var clock = new MutableClock(Origin);
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
        db.AddHive(Hive(player, hive, workers: 45));
        FloralSiteOptions flora = Flora(30);
        CivilExpeditionOptions settings = Rules();
        var service = Service(db, clock, flora, settings);

        Assert.True((await service.StartAsync(player, hive,
            Start("drain-before-stop-01", workers:20))).Succeeded);
        Assert.Equal(20, (await db.ReadAsync(player, hive))!.CivilPopulation!.AssignedWorkers);

        // Simulates disabling both gameplay discovery and new departures.
        // Retain the stable catalogue so the server can recognize in-flight sites.
        settings.Enabled = false;
        settings.DrainExistingFlights = true;
        flora.Enabled = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(player, hive,
                Start("drain-new-flight-denied", workers:5, hiveRevision:1)));
        Assert.Equal(20, (await db.ReadAsync(player, hive))!.CivilPopulation!.AssignedWorkers);

        clock.UtcNow = Origin.AddMinutes(30);
        CivilExpeditionSnapshot settled = await service.ReadAsync(player, hive);
        Assert.Null(settled.Active);
        Assert.Equal(45, settled.AvailableWorkers);
        Assert.Equal(0, settled.AssignedWorkers);
        Assert.Equal(20, settled.LastCompletion!.PollenCredited);
        Assert.Equal(10, db.GlobalSite("clover-field-qa")!.PollenRemaining);
        Assert.Single(db.Ledger);

        await service.ReadAsync(player, hive);
        Assert.Single(db.Ledger);
        Assert.Equal(20, (await db.ReadAsync(player, hive))!.Resources["pollen"].Amount);
    }

    [Fact]
    public async Task Server_due_sweep_completes_existing_flights_in_drain_only_mode()
    {
        var db = new AtomicTestWorld();
        var clock = new MutableClock(Origin);
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
        db.AddHive(Hive(player, hive));
        FloralSiteOptions floral = Flora(12);
        CivilExpeditionOptions settings = Rules();
        var service = Service(db, clock, floral, settings);
        Assert.True((await service.StartAsync(player, hive,
            Start("drain-worker-flight-01", workers:20))).Succeeded);

        settings.DrainExistingFlights = true;
        settings.Enabled = false;
        floral.Enabled = false;
        clock.UtcNow = Origin.AddHours(2);

        CivilExpeditionDueBatch batch = await new CivilExpeditionDueProcessor(
            db, service, clock).ProcessAsync();
        Assert.Equal(1, batch.Inspected);
        Assert.Equal(1, batch.Settled);
        Assert.Empty(batch.RetryMissionIds);
        Assert.Equal(0, (await db.ReadAsync(player, hive))!.CivilPopulation!.AssignedWorkers);
        Assert.Equal(12, (await db.ReadAsync(player, hive))!.Resources["pollen"].Amount);
        Assert.Equal(0, db.GlobalSite("clover-field-qa")!.PollenRemaining);
        Assert.Empty((await new CivilExpeditionDueProcessor(
            db, service, clock).ProcessAsync()).RetryMissionIds);
    }

    [Fact]
    public async Task Drain_mode_with_missing_catalogue_fails_closed_without_losing_civilians()
    {
        var db = new AtomicTestWorld();
        var clock = new MutableClock(Origin);
        Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
        db.AddHive(Hive(player, hive));
        var started = Service(db, clock, Flora());
        Assert.True((await started.StartAsync(player, hive,
            Start("drain-catalog-flight-01", workers:20))).Succeeded);

        clock.UtcNow = Origin.AddHours(1);
        var missing = new FloralSiteOptions { Enabled = false };
        var settings = new CivilExpeditionOptions { DrainExistingFlights = true };
        var draining = Service(db, clock, missing, settings);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            draining.ReadAsync(player, hive));
        Assert.Equal(20, (await db.ReadAsync(player, hive))!.CivilPopulation!.AssignedWorkers);
        Assert.Equal(0, (await db.ReadAsync(player, hive))!.Resources["pollen"].Amount);
    }

    [Fact]
    public void Drain_mode_defaults_off_and_explicitly_prioritizes_no_new_departures()
    {
        var options = new CivilExpeditionOptions { Enabled = true };
        Assert.False(options.DrainExistingFlights);
        Assert.True(options.CanStart(true));
        Assert.True(options.CanSettle(true));
        options.DrainExistingFlights = true;
        Assert.False(options.CanStart(true));
        Assert.True(options.CanSettle(false));
        options.DrainExistingFlights = false;
        options.Enabled = false;
        Assert.False(options.CanSettle(true));
    }

    [Fact]
    public void Corrupt_expedition_or_contributions_fail_validation()
    {
        var p=Guid.NewGuid();
        var empty=CivilExpeditionRules.Empty();
        var mission=new CivilExpeditionMission(Guid.NewGuid(),"world-qa","region-qa",
            "clover-field-qa",50,"balanced",Origin,Origin.AddMinutes(5),
            Origin.AddMinutes(10),Origin.AddMinutes(15),1,5,2);
        Assert.Throws<InvalidDataException>(() =>
            CivilExpeditionRules.ValidateStored(empty with {Active=mission},
                new CivilPopulationState(0,50,0,0,null,new())));
        var corrupted=FloralSiteService.InitialState(
            Flora().Sites[0],Flora().CatalogVersion,Origin) with
            {TotalPollinationPoints=5};
        Assert.Throws<InvalidDataException>(() =>
            FloralSiteService.ValidateStored(corrupted));
    }
}
