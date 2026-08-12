using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.HiveOperations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;

namespace BeeKingdom.Tests;

public sealed class HivePerimeterSortieEndpointTests
{
    [Test]
    public async Task Default_closed_flag_returns_game_unavailable_for_all_routes()
    {
        await using var factory = CreateFactory(false); using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }); var hive = Guid.NewGuid();
        foreach (var request in new[] {
            () => client.GetAsync($"/game/v1/hives/{hive:D}/perimeter-sortie"),
            () => client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/launch", new { signalKey = "foraging_scout", signalInstanceId = "x", reservationId = "r", expectedRevision = 0, idempotencyKey = "k" }),
            () => client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/{Guid.NewGuid():D}/claim", new { expectedRevision = 0, idempotencyKey = "k" }),
            () => client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/{Guid.NewGuid():D}/recall", new { expectedRevision = 0, idempotencyKey = "k" }) })
        { using var response = await request(); Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable)); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo("game.unavailable")); }
    }

    [Test]
    public async Task Enabled_contract_rejects_missing_auth_and_invalid_requests()
    {
        await using var factory = CreateFactory(true); using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }); var hive = Guid.NewGuid();
        var unauthorized = await client.GetAsync($"/game/v1/hives/{hive:D}/perimeter-sortie"); Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        var token = await LoginTestAccount(factory, client, $"perimeter-{Guid.NewGuid():N}@bee.test"); client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var invalid = await client.PostAsJsonAsync("/game/v1/hives/not-guid/perimeter-sortie/launch", new { signalKey = "", signalInstanceId = "", reservationId = "", expectedRevision = -1, idempotencyKey = "" }); Assert.That(invalid.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public void Both_settings_keep_feature_closed()
    {
        using var factory = CreateFactory(false); var root = factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath;
        foreach (var file in new[] { Path.Combine(root, "appsettings.json"), Path.Combine(root, "appsettings.Production.json") })
        { using var doc = JsonDocument.Parse(File.ReadAllText(file)); Assert.That(doc.RootElement.GetProperty("HivePerimeterSortie").GetProperty("Enabled").GetBoolean(), Is.False); }
    }

    [Test]
    public async Task Enabled_launch_claim_replay_and_release_guard()
    {
        var root = Path.Combine(Path.GetTempPath(), "perimeter-http-" + Guid.NewGuid().ToString("N")); var clock = new MutableClock(new(2026,7,21,7,30,0,TimeSpan.Zero)); await using var factory = CreateFactory(true, root, clock); using var client = factory.CreateClient(); try { var hive=Guid.NewGuid(); var token = await LoginTestAccount(factory, client, $"launch-{Guid.NewGuid():N}@bee.test"); client.DefaultRequestHeaders.Authorization = new("Bearer", token); var player=factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value; await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player,hive,s=>s); var get=await client.GetFromJsonAsync<HivePerimeterSnapshot>($"/game/v1/hives/{hive:D}/perimeter-sortie"); var sig=get!.Signals[0]; var payload=new{signalKey=sig.SignalKey,signalInstanceId=sig.SignalInstanceId,reservationId=get.Reservation.ReservationId,expectedRevision=0,idempotencyKey="l"}; var launch=await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/launch",payload); Assert.That(launch.StatusCode,Is.EqualTo(HttpStatusCode.OK)); var first=await launch.Content.ReadFromJsonAsync<HivePerimeterResponse>()!; Assert.That(get.Revision,Is.EqualTo(0)); Assert.That(first!.Snapshot.Revision,Is.EqualTo(1)); Assert.That(first.Snapshot.ServerTimeUtc.Offset,Is.EqualTo(TimeSpan.Zero)); Assert.That(first.Snapshot.ServerTimeUtc,Is.EqualTo(clock.UtcNow)); var replay=await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/launch",payload); Assert.That(replay.StatusCode,Is.EqualTo(HttpStatusCode.OK)); var release=await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/squad-reservation/release",new{expectedRevision=0,idempotencyKey="release-during-sortie"}); Assert.That(release.StatusCode,Is.EqualTo(HttpStatusCode.Conflict)); Assert.That(JsonDocument.Parse(await release.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString(),Is.EqualTo("game.squad_in_use")); var early=await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/{first.Snapshot.Active!.SortieId:D}/claim",new{expectedRevision=1,idempotencyKey="claim-1"}); Assert.That(early.StatusCode,Is.EqualTo(HttpStatusCode.Conflict)); Assert.That(JsonDocument.Parse(await early.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString(),Is.EqualTo("game.perimeter_not_complete")); clock.Advance(TimeSpan.FromSeconds(17)); var claim=await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/{first.Snapshot.Active.SortieId:D}/claim",new{expectedRevision=1,idempotencyKey="claim-1"}); Assert.That(claim.StatusCode,Is.EqualTo(HttpStatusCode.OK)); var claimSnapshot=await claim.Content.ReadFromJsonAsync<HivePerimeterResponse>()!; Assert.That(claimSnapshot!.Snapshot.Revision,Is.EqualTo(2)); Assert.That(claimSnapshot.Snapshot.ServerTimeUtc,Is.EqualTo(clock.UtcNow)); Assert.That(claimSnapshot.Snapshot.Active,Is.Null); var state=await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(player,hive); Assert.That(state!.SquadReservation!.ReservationId,Is.Null); Assert.That(state.DoctrineRoster!.Counts["guardians"],Is.EqualTo(4)); Assert.That(state.DoctrineRoster.Counts["wingrunners"],Is.EqualTo(6)); Assert.That(state.DoctrineRoster.Counts["darters"],Is.EqualTo(4)); Assert.That(state.Resources["honey"].Amount,Is.EqualTo(140)); Assert.That(state.Resources["pollen"].Amount,Is.EqualTo(120)); Assert.That(state.HivePerimeterSortie!.Active,Is.Null); Assert.That(state.SquadReservation!.Reserved.Values.Sum(),Is.EqualTo(0)); Assert.That(state.HivePerimeterSortie.CompletedSignalKeys,Does.Contain("foraging_scout")); var retry=await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/{first.Snapshot.Active.SortieId:D}/claim",new{expectedRevision=1,idempotencyKey="claim-1"}); Assert.That(retry.StatusCode,Is.EqualTo(HttpStatusCode.OK)); var after=await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(player,hive); Assert.That(after!.Resources["honey"].Amount,Is.EqualTo(140)); Assert.That(after.Resources["pollen"].Amount,Is.EqualTo(120)); var reserve=await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/squad-reservation/commit",new{expectedRevision=1,quantities=new{guardians=1,wingrunners=1,darters=0},idempotencyKey="reserve-2"}); Assert.That(reserve.StatusCode,Is.EqualTo(HttpStatusCode.OK)); var board2=await client.GetFromJsonAsync<HivePerimeterSnapshot>($"/game/v1/hives/{hive:D}/perimeter-sortie"); var second=board2!.Signals.Single(x=>x.SignalKey=="brood_watch"); var launch2=await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/launch",new{signalKey=second.SignalKey,signalInstanceId=second.SignalInstanceId,reservationId=board2.Reservation.ReservationId,expectedRevision=2,idempotencyKey="launch-2"}); Assert.That(launch2.StatusCode,Is.EqualTo(HttpStatusCode.OK)); var secondSnapshot=await launch2.Content.ReadFromJsonAsync<HivePerimeterResponse>(); Assert.That(secondSnapshot!.Snapshot.Revision,Is.EqualTo(3)); Assert.That(secondSnapshot.Snapshot.Active!.SignalKey,Is.EqualTo("brood_watch")); Assert.That(board2.Revision,Is.EqualTo(2)); Assert.That(secondSnapshot.Snapshot.Active.SignalInstanceId,Is.EqualTo(second.SignalInstanceId)); Assert.That(secondSnapshot.Snapshot.Active.ReservationId,Is.EqualTo(board2.Reservation.ReservationId)); } finally { if(Directory.Exists(root)) Directory.Delete(root,true); }
    }

    [Test]
    public async Task Enabled_recall_capacity_and_conflicts()
    {
        var root=Path.Combine(Path.GetTempPath(),"perimeter-http-"+Guid.NewGuid().ToString("N")); var clock=new MutableClock(new(2026,7,21,7,30,0,TimeSpan.Zero)); await using var factory = CreateFactory(true,root,clock); using var client = factory.CreateClient(); try { var hive=Guid.NewGuid(); var token = await LoginTestAccount(factory, client, $"recall-{Guid.NewGuid():N}@bee.test"); client.DefaultRequestHeaders.Authorization = new("Bearer", token); var player=factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value; await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player,hive,s=>s); var get=await client.GetFromJsonAsync<HivePerimeterSnapshot>($"/game/v1/hives/{hive:D}/perimeter-sortie"); var sig=get!.Signals.Single(x=>x.SignalKey=="brood_watch"); var launch=await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/launch",new{signalKey=sig.SignalKey,signalInstanceId=sig.SignalInstanceId,reservationId=get.Reservation.ReservationId,expectedRevision=0,idempotencyKey="l"}); var first=await launch.Content.ReadFromJsonAsync<HivePerimeterResponse>()!; var recall=await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/{first.Snapshot.Active!.SortieId:D}/recall",new{expectedRevision=1,idempotencyKey="r"}); Assert.That(recall.StatusCode,Is.EqualTo(HttpStatusCode.OK)); var state=await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(player,hive); Assert.That(state!.HivePerimeterSortie!.Active,Is.Null); Assert.That(state.SquadReservation!.ReservationId,Is.Null); Assert.That(state.DoctrineRoster!.Counts["guardians"],Is.EqualTo(4)); Assert.That(state.DoctrineRoster.Counts["wingrunners"],Is.EqualTo(6)); Assert.That(state.DoctrineRoster.Counts["darters"],Is.EqualTo(4)); Assert.That(state.Resources["honey"].Amount,Is.EqualTo(100)); Assert.That(state.Resources["pollen"].Amount,Is.EqualTo(100)); Assert.That(state.HivePerimeterSortie!.CompletedSignalKeys,Is.Empty); Assert.That(state.SquadReservation!.Reserved.Values.Sum(),Is.EqualTo(0)); } finally { if(Directory.Exists(root)) Directory.Delete(root,true); }
    }

    [Test]
    public async Task Enabled_claim_http_exposes_authoritative_receipt_and_exact_replay()
    {
        var root = Path.Combine(Path.GetTempPath(), "perimeter-http-receipt-" + Guid.NewGuid().ToString("N")); var clock = new MutableClock(new(2026, 7, 21, 9, 0, 0, TimeSpan.Zero)); await using var factory = CreateFactory(true, root, clock); using var client = factory.CreateClient();
        try
        {
            var hive = Guid.NewGuid(); var token = await LoginTestAccount(factory, client, $"receipt-{Guid.NewGuid():N}@bee.test"); client.DefaultRequestHeaders.Authorization = new("Bearer", token); var player = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value; await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player, hive, s => s);
            var board = await client.GetFromJsonAsync<HivePerimeterSnapshot>($"/game/v1/hives/{hive:D}/perimeter-sortie"); var signal = board!.Signals.Single(x => x.SignalKey == "foraging_scout"); var launch = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/launch", new { signalKey = signal.SignalKey, signalInstanceId = signal.SignalInstanceId, reservationId = board.Reservation.ReservationId, expectedRevision = board.Revision, idempotencyKey = "receipt-launch" }); var launched = await launch.Content.ReadFromJsonAsync<HivePerimeterResponse>(); clock.Advance(TimeSpan.FromSeconds(17)); var claim = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/{launched!.Snapshot.Active!.SortieId:D}/claim", new { expectedRevision = launched.Snapshot.Revision, idempotencyKey = "receipt-claim" }); Assert.That(claim.StatusCode, Is.EqualTo(HttpStatusCode.OK)); var receiptSnapshot = await claim.Content.ReadFromJsonAsync<HivePerimeterResponse>(); Assert.That(receiptSnapshot!.Snapshot.ClaimReceipt, Is.Not.Null); Assert.That(receiptSnapshot.Snapshot.ClaimReceipt!.PlayerId, Is.EqualTo(player)); Assert.That(receiptSnapshot.Snapshot.ClaimReceipt.HiveId, Is.EqualTo(hive)); Assert.That(receiptSnapshot.Snapshot.ClaimReceipt.CreditedByResource["honey"], Is.EqualTo(40)); Assert.That(receiptSnapshot.Snapshot.ClaimReceipt.CreditedByResource["pollen"], Is.EqualTo(20));
            var replay = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/perimeter-sortie/{launched.Snapshot.Active.SortieId:D}/claim", new { expectedRevision = launched.Snapshot.Revision, idempotencyKey = "receipt-claim" }); Assert.That(replay.StatusCode, Is.EqualTo(HttpStatusCode.OK)); var replaySnapshot = await replay.Content.ReadFromJsonAsync<HivePerimeterResponse>(); Assert.That(replaySnapshot!.Snapshot.ClaimReceipt!.SortieId, Is.EqualTo(receiptSnapshot.Snapshot.ClaimReceipt.SortieId)); Assert.That(replaySnapshot.Snapshot.ClaimReceipt.ServerTimeUtc, Is.EqualTo(receiptSnapshot.Snapshot.ClaimReceipt.ServerTimeUtc)); Assert.That(replaySnapshot.Snapshot.ClaimReceipt.CreditedByResource["honey"], Is.EqualTo(40));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static async Task<string> LoginTestAccount(WebApplicationFactory<Program> factory, HttpClient client, string email)
    {
        factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1.0.0", ipAddress = "127.0.0.1", deviceIdentifier = "perimeter-tests", region = "local" });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return json.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;
    }

    private static WebApplicationFactory<Program> CreateFactory(bool enabled, string? root = null, MutableClock? clock = null) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("environment", "Development"); builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["HivePerimeterSortie:Enabled"] = enabled.ToString(), ["CombatSquadReservation:Enabled"] = "true" })); builder.ConfigureTestServices(services => { if (clock is not null) { services.RemoveAll<BeeKingdom.HiveOperations.IServerClock>(); services.AddSingleton<BeeKingdom.HiveOperations.IServerClock>(clock); } if (root is not null) { services.RemoveAll<IHiveStateRepository>(); services.AddSingleton<IHiveStateRepository>(new DurableJsonHiveStateRepository(root,(p,h)=>new PlayerHiveState(p,h,HiveStateMigrator.CurrentModelVersion,0,new Dictionary<string,ResourceBalance>{{"honey",new(100,1000)},{"pollen",new(100,1000)}},new(),[],new(),DoctrineRoster:new DoctrineRosterState(0,new(){{"guardians",4},{"wingrunners",6},{"darters",4}},null,new()),SquadReservation:new SquadReservationState(0,12,new(){{"guardians",3},{"wingrunners",6},{"darters",3}},"reservation",new())))); } });
    });
    private sealed class MutableClock(DateTimeOffset now) : BeeKingdom.HiveOperations.IServerClock { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan value)=>UtcNow+=value; }
}







