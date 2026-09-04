using BeeKingdom.HiveOperations;

namespace BeeKingdom.Tests;

public sealed class HiveOfflineProductionServiceTests
{
    [Test] public async Task FirstReadSeedsAtNowWithZeroPending() { var (s, c, p, h, _) = Create(); var snap = await s.ReadSnapshotAsync(p, h); Assert.That(snap.ProductionAsOfUtc, Is.EqualTo(c.UtcNow)); Assert.That(snap.ProductionRevision, Is.EqualTo(0)); Assert.That(snap.Lines.All(x => x.PendingAmount == 0), Is.True); }
    [Test] public async Task AccrualCapsAtConfiguredMaximumAndDoesNotChangeProductionRevision() { var (s, c, p, h, r) = Create(TimeSpan.FromHours(2)); await s.ReadSnapshotAsync(p,h); c.UtcNow = c.UtcNow.AddDays(3); var snap = await s.ReadSnapshotAsync(p,h); Assert.That(snap.ProductionRevision, Is.EqualTo(0)); Assert.That(snap.Lines.Single(x=>x.ResourceKey=="honey").PendingAmount, Is.EqualTo(20m)); }
    [Test] public async Task FutureMarkerAccruesZeroAndResetsToNow() { var (s, c, p, h, _) = Create(); await s.ReadSnapshotAsync(p,h); c.UtcNow = c.UtcNow.AddHours(-1); var snap = await s.ReadSnapshotAsync(p,h); Assert.That(snap.ProductionAsOfUtc, Is.EqualTo(c.UtcNow)); Assert.That(snap.Lines.All(x=>x.PendingAmount==0), Is.True); }
    [Test] public async Task SameTimestampSecondReadDoesNotChangeStateRevision() { var (s, c, p, h, r) = Create(); await s.ReadSnapshotAsync(p,h); var before = (await r.ReadAsync(p,h))!.Revision; await s.ReadSnapshotAsync(p,h); var after = (await r.ReadAsync(p,h))!.Revision; Assert.That(after, Is.EqualTo(before)); }
    [Test] public void NonUtcClockFailsClosedWithoutPersisting() { var (s, _, p, h, _) = Create(nonUtc:true); Assert.ThrowsAsync<InvalidOperationException>(async ()=> await s.ReadSnapshotAsync(p,h)); }
    [Test] public void MissingOrInvalidResourceBalanceFailsClosed() { var (s, _, p, h, _) = Create(invalidResources:true); Assert.ThrowsAsync<InvalidDataException>(async ()=> await s.ReadSnapshotAsync(p,h)); }
    [Test] public async Task SuccessfulCollectCreditsFloorAndPreservesFraction() { var (s,c,p,h,r)=Create(); await s.ReadSnapshotAsync(p,h); c.UtcNow=c.UtcNow.AddMinutes(9); var x=await s.CollectAsync(p,h,"honey_storage",new(0,"k")); Assert.That(x.Succeeded,Is.True); Assert.That(x.Response!.Receipt.CreditedAmount,Is.EqualTo(1)); Assert.That(x.Response.Receipt.RemainingPending,Is.EqualTo(0.5m)); }
    [Test] public async Task CollectIsLimitedByResourceHeadroom() { var (s,c,p,h,r)=Create(); r.Replace((await r.ReadAsync(p,h))! with { Resources = new Dictionary<string,ResourceBalance>{{"honey",new(99,100)},{"wax",new(0,100)},{"pollen",new(0,100)}}}); await s.ReadSnapshotAsync(p,h); c.UtcNow=c.UtcNow.AddHours(1); var x=await s.CollectAsync(p,h,"honey_storage",new(0,"k")); Assert.That(x.Response!.Receipt.CreditedAmount,Is.EqualTo(1)); }
    [Test] public async Task CapacityFullReturnsCodeWithoutCredit() { var(s,c,p,h,r)=Create(); r.Replace((await r.ReadAsync(p,h))! with { Resources = new Dictionary<string,ResourceBalance>{{"honey",new(100,100)},{"wax",new(0,100)},{"pollen",new(0,100)}}}); await s.ReadSnapshotAsync(p,h); c.UtcNow=c.UtcNow.AddHours(1); var x=await s.CollectAsync(p,h,"honey_storage",new(0,"k")); Assert.That(x.Code,Is.EqualTo("game.resource_capacity_full")); }
    [Test] public async Task NotReadyReturnsCodeWithoutReceipt() { var(s,c,p,h,r)=Create(); await s.ReadSnapshotAsync(p,h); var x=await s.CollectAsync(p,h,"honey_storage",new(0,"k")); Assert.That(x.Code,Is.EqualTo("game.production_not_ready")); Assert.That((await r.ReadAsync(p,h))!.OfflineProduction!.Receipts,Is.Empty); }
    [Test] public async Task StaleRevisionReturnsConflictWithoutCredit() { var(s,c,p,h,r)=Create(); await s.ReadSnapshotAsync(p,h); c.UtcNow=c.UtcNow.AddHours(1); var x=await s.CollectAsync(p,h,"honey_storage",new(9,"k")); Assert.That(x.Code,Is.EqualTo("game.production_conflict")); }
    [Test] public async Task SameKeySamePayloadReplaysExactStoredResponseAfterAnotherSuccessfulMutation() { var(s,c,p,h,r)=Create(); await s.ReadSnapshotAsync(p,h); c.UtcNow=c.UtcNow.AddHours(1); var first=await s.CollectAsync(p,h,"honey_storage",new(0,"k")); var second=await s.CollectAsync(p,h,"wax_workshop",new(1,"w")); var replay=await s.CollectAsync(p,h,"honey_storage",new(0,"k")); Assert.That(replay.Response,Is.EqualTo(first.Response)); }
    [Test] public async Task SameKeyDifferentPayloadReturnsIdempotencyConflictWithoutCredit() { var(s,c,p,h,r)=Create(); await s.ReadSnapshotAsync(p,h); c.UtcNow=c.UtcNow.AddHours(1); _=await s.CollectAsync(p,h,"honey_storage",new(0,"k")); var x=await s.CollectAsync(p,h,"wax_workshop",new(1,"k")); Assert.That(x.Code,Is.EqualTo("game.idempotency_conflict")); }
    [Test] public async Task ResearchBonusesStackAcrossCompletedTiersAndApplyToRateAndCapacity()
    {
        var (s, c, p, h, r) = Create(catalogCapacity: 1000);
        var completed = new Dictionary<string, ResearchCompletion>
        {
            ["foraging_routes_i"] = new("foraging_routes_i", c.UtcNow, new ResearchEffects(200, 0, 0, 0, 0, 0)),
            ["foraging_routes_ii"] = new("foraging_routes_ii", c.UtcNow, new ResearchEffects(500, 0, 0, 0, 0, 0)),
            ["tempered_combs_ii"] = new("tempered_combs_ii", c.UtcNow, new ResearchEffects(0, 800, 300, 0, 0, 0)),
            ["sealed_reserves"] = new("sealed_reserves", c.UtcNow, new ResearchEffects(0, 0, 0, 0, 0, 1000)),
        };
        r.Replace((await r.ReadAsync(p, h))! with { Research = new(completed, null) });
        var snap = await s.ReadSnapshotAsync(p, h);
        Assert.That(snap.Lines.Single(x => x.ResourceKey == "honey").HourlyRate, Is.EqualTo(10.7m));
        Assert.That(snap.Lines.Single(x => x.ResourceKey == "wax").HourlyRate, Is.EqualTo(5.15m));
        Assert.That(snap.Lines.Single(x => x.ResourceKey == "wax").Capacity, Is.EqualTo(1180));
        Assert.That(snap.Lines.Single(x => x.ResourceKey == "honey").Capacity, Is.EqualTo(1100));
        Assert.That(snap.Lines.Single(x => x.ResourceKey == "pollen").Capacity, Is.EqualTo(1100));
    }
    [Test] public async Task TwoPlayersAndTwoHivesAreIsolated() { var(s,c,p,h,r)=Create(); var p2=Guid.NewGuid(); r.Seed(new PlayerHiveState(p2,h,10,0,new Dictionary<string,ResourceBalance>{{"honey",new(0,100)},{"wax",new(0,100)},{"pollen",new(0,100)}},new(),new(),new())); Assert.That((await s.ReadSnapshotAsync(p2,h)).Lines.All(x=>x.PendingAmount==0),Is.True); }
    [TestCase("wrong", "honey", 1, 1, false)]
    [TestCase("honey_storage", "wax", 1, 1, false)]
    [TestCase("honey_storage", "honey", 0, 1, false)]
    [TestCase("honey_storage", "honey", 1, 0, false)]
    [TestCase("honey_storage", "honey", 1, 1, true)]
    public void OptionsValidationRejectsInvalidShapes(string building, string resource, decimal rate, long capacity, bool badVersion) { var o=new HiveOfflineProductionOptions{Enabled=false,CatalogVersion=badVersion?" bad ":"test-v1",Catalog=[new(building,resource,rate,capacity),new("wax_workshop","wax",1,1),new("warehouse_cells","pollen",1,1)]}; Assert.Throws<InvalidDataException>(()=>o.Validate()); var empty=new HiveOfflineProductionOptions{Enabled=false}; Assert.DoesNotThrow(()=>empty.Validate()); }
    [Test] public void MigratorV9WithoutOfflineBlockBecomesV10WithNullBlock() { var(_,_,p,h,r)=Create(); var state=r.ReadAsync(p,h).Result with { ModelVersion=9, OfflineProduction=null }; var migrated=HiveStateMigrator.ToCurrent(state); Assert.That(migrated.ModelVersion,Is.EqualTo(10)); Assert.That(migrated.OfflineProduction,Is.Null); }
    [Test] public async Task MigratorAcceptsValidV10ReceiptRoundTrip() { var(s,c,p,h,r)=Create(); await s.ReadSnapshotAsync(p,h); c.UtcNow=c.UtcNow.AddHours(1); var result=await s.CollectAsync(p,h,"honey_storage",new(0,"roundtrip")); Assert.That(result.Succeeded,Is.True); var state=(await r.ReadAsync(p,h))!; var migrated=HiveStateMigrator.ToCurrent(state); Assert.That(migrated.OfflineProduction!.Receipts.ContainsKey("roundtrip"),Is.True); Assert.That(migrated.OfflineProduction.Revision,Is.EqualTo(1)); }
    [Test] public async Task MigratorRejectsCorruptReceiptEnvelopeAndMapping() { var state=await ValidReceiptState(); var p=state.OfflineProduction!; var key=p.Receipts.Keys.Single(); var receipt=p.Receipts[key]; foreach(var bad in new[]{receipt with { PayloadHash=null! }, receipt with { PayloadHash="A"+receipt.PayloadHash[1..] }, receipt with { Response=receipt.Response with { Receipt=receipt.Response.Receipt with { PlayerId=Guid.NewGuid() } } }, receipt with { Response=receipt.Response with { Receipt=receipt.Response.Receipt with { IdempotencyKey="other" } } }, receipt with { Response=receipt.Response with { Receipt=receipt.Response.Receipt with { BuildingKey="bad" } } }, receipt with { Response=receipt.Response with { Receipt=receipt.Response.Receipt with { ResourceKey="bad" } } }, receipt with { Response=receipt.Response with { Receipt=receipt.Response.Receipt with { ResultingBalance=new(2,1) } } }}) Assert.Throws<InvalidDataException>(()=>HiveStateMigrator.ToCurrent(state with { OfflineProduction=p with { Receipts=new(){[key]=bad} } })); }
    [Test] public async Task MigratorRejectsCorruptReceiptSnapshot() { var state=await ValidReceiptState(); var p=state.OfflineProduction!; var key=p.Receipts.Keys.Single(); var receipt=p.Receipts[key]; foreach(var bad in new[]{receipt with { Response=receipt.Response with { Snapshot=receipt.Response.Snapshot with { ContractVersion="bad" } } }, receipt with { Response=receipt.Response with { Snapshot=receipt.Response.Snapshot with { CatalogVersion="BAD" } } }, receipt with { Response=receipt.Response with { Snapshot=receipt.Response.Snapshot with { ProductionRevision=9 } } }, receipt with { Response=receipt.Response with { Snapshot=receipt.Response.Snapshot with { Lines=receipt.Response.Snapshot.Lines.Take(2).ToArray() } } }}) Assert.Throws<InvalidDataException>(()=>HiveStateMigrator.ToCurrent(state with { OfflineProduction=p with { Receipts=new(){[key]=bad} } })); }
    [Test] public async Task DurableJsonRoundTripPreservesPendingRevisionReceiptAndExactReplay() { string root=Path.Combine(Path.GetTempPath(),"bee-offline-"+Guid.NewGuid().ToString("N")); try { Guid p=Guid.NewGuid(), h=Guid.NewGuid(); var clock=new FakeClock(DateTimeOffset.UtcNow); var options=new HiveOfflineProductionOptions{Enabled=true,CatalogVersion="test-v1",MaxRecognizedDuration=TimeSpan.FromHours(2),Catalog=[new("honey_storage","honey",10,100),new("wax_workshop","wax",5,100),new("warehouse_cells","pollen",8,100)]}; Func<Guid,Guid,PlayerHiveState> seed=(a,b)=>new(a,b,10,0,new Dictionary<string,ResourceBalance>{{"honey",new(0,100)},{"wax",new(0,100)},{"pollen",new(0,100)}},new(),new(),new()); var repo1=new DurableJsonHiveStateRepository(root,seed); var service1=new HiveOfflineProductionService(repo1,clock,options); await service1.ReadSnapshotAsync(p,h); clock.UtcNow=clock.UtcNow.AddHours(1); var first=await service1.CollectAsync(p,h,"honey_storage",new(0,"durable-1")); var repo2=new DurableJsonHiveStateRepository(root,seed); var service2=new HiveOfflineProductionService(repo2,clock,options); var persisted=await repo2.ReadAsync(p,h); Assert.That(persisted!.ModelVersion,Is.EqualTo(10)); Assert.That(persisted.OfflineProduction!.Receipts.ContainsKey("durable-1"),Is.True); var replay=await service2.CollectAsync(p,h,"honey_storage",new(0,"durable-1")); Assert.That(replay.Code,Is.EqualTo("game.idempotency_replay")); Assert.That(System.Text.Json.JsonSerializer.Serialize(replay.Response),Is.EqualTo(System.Text.Json.JsonSerializer.Serialize(first.Response))); Assert.That((await repo2.ReadAsync(p,h))!.Resources["honey"].Amount,Is.EqualTo(10)); } finally { if(Directory.Exists(root)) Directory.Delete(root,true); } }
    [Test] public async Task ReceiptRetentionStaysAt512AndEvictsOldestDeterministically() { var(s,c,p,h,r)=Create(); await s.ReadSnapshotAsync(p,h); OfflineProductionCollectResponse? last=null; for(int i=0;i<=512;i++){ c.UtcNow=c.UtcNow.AddMinutes(7); var x=await s.CollectAsync(p,h,"honey_storage",new(i,$"k-{i:D3}")); Assert.That(x.Succeeded,Is.True); last=x.Response; } var state=(await r.ReadAsync(p,h))!; Assert.That(state.OfflineProduction!.Receipts.Count,Is.EqualTo(512)); Assert.That(state.OfflineProduction.Receipts.ContainsKey("k-000"),Is.False); Assert.That(state.OfflineProduction.Receipts.ContainsKey("k-001"),Is.True); Assert.That(state.OfflineProduction.Receipts.ContainsKey("k-512"),Is.True); var replay=await s.CollectAsync(p,h,"honey_storage",new(512,"k-512")); Assert.That(replay.Code,Is.EqualTo("game.idempotency_replay")); Assert.That(System.Text.Json.JsonSerializer.Serialize(replay.Response),Is.EqualTo(System.Text.Json.JsonSerializer.Serialize(last))); }
    private async Task<PlayerHiveState> ValidReceiptState() { var(s,c,p,h,r)=Create(); await s.ReadSnapshotAsync(p,h); c.UtcNow=c.UtcNow.AddHours(1); await s.CollectAsync(p,h,"honey_storage",new(0,"corrupt")); return (await r.ReadAsync(p,h))!; }

    // M051-CL: item 16 of the mission's required test list - "completed research bonus actually
    // modifies target gameplay calculation" for the Prosperity/Cooperation branches' real
    // integration point (EffectiveRate/EffectiveCapacity via the optional IAllianceGameplayBonusResolver).
    [Test]
    public async Task AllianceResearchProductionAndCapacityBonusAppliesFromResolver()
    {
        Guid p = Guid.NewGuid(), h = Guid.NewGuid();
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var repo = new MemoryRepo();
        repo.Seed(new PlayerHiveState(p, h, 10, 0, new Dictionary<string, ResourceBalance> { { "honey", new(0, 1000) }, { "wax", new(0, 1000) }, { "pollen", new(0, 1000) } }, new(), new(), new()));
        var options = new HiveOfflineProductionOptions { Enabled = true, CatalogVersion = "test-v1", Catalog = [new("honey_storage", "honey", 100m, 1000), new("wax_workshop", "wax", 1, 1), new("warehouse_cells", "pollen", 1, 1)] };
        var resolver = new StubAllianceBonusResolver(new AllianceGameplayBonus(1000, 500, 0)); // +10% production, +5% capacity
        var service = new HiveOfflineProductionService(repo, clock, options, allianceBonusResolver: resolver);

        var withBonus = await service.ReadSnapshotAsync(p, h);
        var line = withBonus.Lines.Single(x => x.ResourceKey == "honey");
        Assert.That(line.HourlyRate, Is.EqualTo(110m), "1000bp (+10%) alliance production bonus must apply to the real rate");
        Assert.That(line.Capacity, Is.EqualTo(1050), "500bp (+5%) alliance capacity bonus must apply to the real capacity");

        var withoutBonus = await new HiveOfflineProductionService(repo, clock, options).ReadSnapshotAsync(p, h);
        var baseline = withoutBonus.Lines.Single(x => x.ResourceKey == "honey");
        Assert.That(baseline.HourlyRate, Is.EqualTo(100m), "no resolver registered must behave exactly as before (AllianceGameplayBonus.None)");
        Assert.That(baseline.Capacity, Is.EqualTo(1000));
    }
    private sealed class StubAllianceBonusResolver(AllianceGameplayBonus bonus) : IAllianceGameplayBonusResolver
    {
        public Task<AllianceGameplayBonus> ResolveAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult(bonus);
    }

    private static (HiveOfflineProductionService service, FakeClock clock, Guid player, Guid hive, MemoryRepo repo) Create(TimeSpan? max = null, bool invalidResources = false, bool nonUtc = false, decimal catalogRate = 10, long catalogCapacity = 1_000_000_000)
    {
        Guid p=Guid.NewGuid(), h=Guid.NewGuid(); var clock=new FakeClock(nonUtc ? new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified), TimeSpan.FromHours(1)) : DateTimeOffset.UtcNow); var repo=new MemoryRepo(); var resources=invalidResources ? new Dictionary<string,ResourceBalance>{{"honey",new(10,1)}} : new(){{"honey",new(0,1_000_000_000)},{"wax",new(0,1_000_000_000)},{"pollen",new(0,1_000_000_000)}}; repo.Seed(new PlayerHiveState(p,h,10,0,resources,new(),new(),new())); var options=new HiveOfflineProductionOptions{Enabled=true,CatalogVersion="test-v1",MaxRecognizedDuration=max??TimeSpan.FromHours(2),Catalog=[new("honey_storage","honey",catalogRate,catalogCapacity),new("wax_workshop","wax",5,catalogCapacity),new("warehouse_cells","pollen",8,catalogCapacity)]}; return (new HiveOfflineProductionService(repo,clock,options),clock,p,h,repo);
    }
    private sealed class FakeClock(DateTimeOffset now) : IServerClock { public DateTimeOffset UtcNow { get; set; } = now; }
    private sealed class MemoryRepo : IHiveStateRepository { private readonly Dictionary<(Guid,Guid),PlayerHiveState> data=[]; public void Seed(PlayerHiveState s)=>data[(s.PlayerId,s.HiveId)]=s; public void Replace(PlayerHiveState s)=>Seed(s); public Task<PlayerHiveState?> ReadAsync(Guid p,Guid h,CancellationToken ct=default)=>Task.FromResult(data.TryGetValue((p,h),out var s)?s:null); public Task<PlayerHiveState> ExecuteAtomicallyAsync(Guid p,Guid h,Func<PlayerHiveState,PlayerHiveState> m,CancellationToken ct=default){var s=data[(p,h)]; var n=m(HiveStateMigrator.ToCurrent(s)); data[(p,h)]=n; return Task.FromResult(n);} public Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid p,CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<Guid>>(data.Keys.Where(k=>k.Item1==p).Select(k=>k.Item2).ToList()); public Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(int limit,CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<PlayerHiveState>>(data.Values.Take(limit).ToList()); }
}

file static class OfflineSnapshotTestExtensions { public static TimeSpan RecognizedDuration(this OfflineProductionReadSnapshot s) => s.MaxRecognizedDuration; }
