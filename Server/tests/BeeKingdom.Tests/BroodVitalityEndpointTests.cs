using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.HiveOperations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BeeKingdom.Tests;

public sealed class BroodVitalityEndpointTests
{
    [Test]
    public async Task Closed_flag_short_circuits_get_and_mutations()
    {
        await using var f = Factory(false, true); using var c = f.CreateClient();
        var get = await c.GetAsync($"/game/v1/hives/{Guid.NewGuid():D}/brood/vitality");
        var post = await c.PostAsJsonAsync($"/game/v1/hives/{Guid.NewGuid():D}/brood/vitality/care/start?type=feeding", new { expectedRevision = 0, idempotencyKey = "safe-1" });
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable)); Assert.That(post.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Enabled_get_is_typed_and_auth_required()
    {
        await using var f = Factory(true); using var c = f.CreateClient();
        Assert.That((await c.GetAsync($"/game/v1/hives/{Guid.NewGuid():D}/brood/vitality")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        var token = await Login(f, c); c.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var p = f.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value; var h = Guid.NewGuid();
        await f.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(p, h, s => s);
        using var doc = JsonDocument.Parse(await (await c.GetAsync($"/game/v1/hives/{h:D}/brood/vitality")).Content.ReadAsStringAsync());
        Assert.That(doc.RootElement.GetProperty("contractVersion").GetString(), Is.EqualTo("living-hive-brood-vitality-v1"));
        Assert.That(doc.RootElement.GetProperty("hiveId").GetGuid(), Is.EqualTo(h)); Assert.That(doc.RootElement.GetProperty("vitality").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public async Task Enabled_start_complete_replay_and_conflict_are_exact()
    {
        var clock = new MutableClock(
            new DateTimeOffset(2026, 7, 23, 18, 0, 0, TimeSpan.Zero));
        await using var f = Factory(true, clock: clock);
        using var c = f.CreateClient();
        var token = await Login(f, c);
        c.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var p = f.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;
        var h = Guid.NewGuid();
        var now = clock.UtcNow;
        await f.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(p, h, s => s with { Resources = new Dictionary<string, ResourceBalance> { ["honey"] = new(500, 1000), ["wax"] = new(100, 1000), ["pollen"] = new(0, 1000) }, BroodVitality = new(50, 50, 0, now, null) });
        var start = await c.PostAsJsonAsync(
            $"/game/v1/hives/{h:D}/brood/vitality/care/start?type=feeding",
            new { expectedRevision = 0, idempotencyKey = "start-1" });
        Assert.That(start.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var sd =
            JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        JsonElement startReceipt = sd.RootElement.GetProperty("receipt");
        var op = startReceipt.GetProperty("operationId").GetGuid();
        Assert.Multiple(() =>
        {
            Assert.That(
                startReceipt.GetProperty("idempotencyKey").GetString(),
                Is.EqualTo("start-1"));
            Assert.That(
                startReceipt.GetProperty("revisionBefore").GetInt64(),
                Is.EqualTo(0));
            Assert.That(
                startReceipt.GetProperty("revisionAfter").GetInt64(),
                Is.EqualTo(1));
            Assert.That(
                sd.RootElement.GetProperty("snapshot")
                    .GetProperty("contractVersion").GetString(),
                Is.EqualTo(BroodVitalityCareService.ContractVersion));
            Assert.That(
                sd.RootElement.GetProperty("snapshot")
                    .GetProperty("vitality")
                    .GetProperty("activeOperation")
                    .GetProperty("endsAtUtc").GetDateTimeOffset(),
                Is.EqualTo(now.AddSeconds(12)));
        });
        string firstReceiptJson = startReceipt.GetRawText();

        var replayStart = await c.PostAsJsonAsync(
            $"/game/v1/hives/{h:D}/brood/vitality/care/start?type=feeding",
            new { expectedRevision = 0, idempotencyKey = "start-1" });
        Assert.That(replayStart.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var rsd = JsonDocument.Parse(
            await replayStart.Content.ReadAsStringAsync());
        Assert.That(
            rsd.RootElement.GetProperty("receipt").GetRawText(),
            Is.EqualTo(firstReceiptJson));

        var early = await c.PostAsJsonAsync(
            $"/game/v1/hives/{h:D}/brood/vitality/care/{op:D}/complete",
            new { expectedRevision = 1, idempotencyKey = "complete-1" });
        Assert.That(early.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        using (var earlyError = JsonDocument.Parse(
            await early.Content.ReadAsStringAsync()))
            Assert.That(
                earlyError.RootElement.GetProperty("code").GetString(),
                Is.EqualTo("game.vitality_not_ready"));

        clock.Now = now.AddSeconds(12);
        var complete = await c.PostAsJsonAsync(
            $"/game/v1/hives/{h:D}/brood/vitality/care/{op:D}/complete",
            new { expectedRevision = 1, idempotencyKey = "complete-1" });
        Assert.That(complete.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var completeDocument = JsonDocument.Parse(
            await complete.Content.ReadAsStringAsync());
        JsonElement completeReceipt =
            completeDocument.RootElement.GetProperty("receipt");
        JsonElement completeSnapshot =
            completeDocument.RootElement.GetProperty("snapshot");
        Assert.Multiple(() =>
        {
            Assert.That(
                completeReceipt.GetProperty("revisionBefore").GetInt64(),
                Is.EqualTo(1));
            Assert.That(
                completeReceipt.GetProperty("revisionAfter").GetInt64(),
                Is.EqualTo(2));
            Assert.That(
                completeReceipt.GetProperty("code").GetString(),
                Is.EqualTo("game.vitality_care_completed"));
            Assert.That(
                completeSnapshot.GetProperty("globalRevision").GetInt64(),
                Is.EqualTo(2));
            Assert.That(
                completeSnapshot.GetProperty("vitality")
                    .GetProperty("nutrition").GetInt32(),
                Is.EqualTo(72));
            Assert.That(
                completeSnapshot.GetProperty("vitality")
                    .GetProperty("activeOperation").ValueKind,
                Is.EqualTo(JsonValueKind.Null));
        });
        string completeReceiptJson = completeReceipt.GetRawText();

        var replayComplete = await c.PostAsJsonAsync(
            $"/game/v1/hives/{h:D}/brood/vitality/care/{op:D}/complete",
            new { expectedRevision = 1, idempotencyKey = "complete-1" });
        Assert.That(replayComplete.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var replayCompleteDocument = JsonDocument.Parse(
            await replayComplete.Content.ReadAsStringAsync());
        Assert.That(
            replayCompleteDocument.RootElement.GetProperty("receipt")
                .GetRawText(),
            Is.EqualTo(completeReceiptJson));

        var conflictingReplay = await c.PostAsJsonAsync(
            $"/game/v1/hives/{h:D}/brood/vitality/care/{op:D}/complete",
            new { expectedRevision = 2, idempotencyKey = "complete-1" });
        Assert.That(
            conflictingReplay.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));

        PlayerHiveState stored = (await f.Services
            .GetRequiredService<IHiveStateRepository>()
            .ReadAsync(p, h))!;
        Assert.Multiple(() =>
        {
            Assert.That(stored.Resources["honey"].Amount, Is.EqualTo(200));
            Assert.That(stored.BroodVitality!.Nutrition, Is.EqualTo(72));
            Assert.That(stored.Revision, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Invalid_commands_and_foreign_players_fail_closed()
    {
        var clock = new MutableClock(
            new DateTimeOffset(2026, 7, 23, 19, 0, 0, TimeSpan.Zero));
        await using var factory = Factory(true, clock: clock);
        using var client = factory.CreateClient();
        Guid hiveId = Guid.NewGuid();
        Assert.That(
            (await client.PostAsJsonAsync(
                $"/game/v1/hives/{hiveId:D}/brood/vitality/care/start?type=feeding",
                new { expectedRevision = 0, idempotencyKey = "start" }))
            .StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));

        string token = await Login(factory, client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Guid playerId = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;
        await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ExecuteAtomicallyAsync(
                playerId,
                hiveId,
                state => state with
                {
                    Resources = new Dictionary<string, ResourceBalance>
                    {
                        ["honey"] = new(100, 1000),
                        ["wax"] = new(10, 1000),
                        ["pollen"] = new(0, 1000)
                    },
                    BroodVitality =
                        new BroodVitalityState(
                            50,
                            50,
                            0,
                            clock.UtcNow,
                            null)
                });

        Assert.That(
            (await client.PostAsJsonAsync(
                $"/game/v1/hives/{hiveId:D}/brood/vitality/care/start?type=unknown",
                new { expectedRevision = 0, idempotencyKey = "unknown" }))
            .StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(
            (await client.PostAsJsonAsync(
                $"/game/v1/hives/{hiveId:D}/brood/vitality/care/start?type=feeding",
                new { expectedRevision = 0, idempotencyKey = "bad key" }))
            .StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        HttpResponseMessage insufficient = await client.PostAsJsonAsync(
            $"/game/v1/hives/{hiveId:D}/brood/vitality/care/start?type=feeding",
            new { expectedRevision = 0, idempotencyKey = "insufficient" });
        Assert.That(
            insufficient.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));
        using (var insufficientError = JsonDocument.Parse(
            await insufficient.Content.ReadAsStringAsync()))
            Assert.That(
                insufficientError.RootElement.GetProperty("code").GetString(),
                Is.EqualTo("game.insufficient_resources"));

        using var foreign = factory.CreateClient();
        string foreignToken = await Login(factory, foreign);
        foreign.DefaultRequestHeaders.Authorization =
            new("Bearer", foreignToken);
        Assert.That(
            (await foreign.GetAsync(
                $"/game/v1/hives/{hiveId:D}/brood/vitality")).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static WebApplicationFactory<Program> Factory(
        bool enabled,
        bool throwing = false,
        MutableClock? clock = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("environment", "Development");
            b.ConfigureAppConfiguration((_, c) =>
                c.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["BroodVitality:Enabled"] = enabled.ToString(),
                        ["HiveDailyRound:Enabled"] = "false"
                    }));
            if (throwing)
                b.ConfigureServices(s =>
                {
                    s.RemoveAll<IHiveStateRepository>();
                    s.AddSingleton<IHiveStateRepository, ThrowingRepository>();
                });
            if (clock != null)
                b.ConfigureServices(s =>
                {
                    s.RemoveAll<IServerClock>();
                    s.AddSingleton<IServerClock>(clock);
                });
        });
    private static async Task<string> Login(WebApplicationFactory<Program> f, HttpClient c) { var email = $"brood-{Guid.NewGuid():N}@bee.test"; f.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret"); using var d = JsonDocument.Parse(await (await c.PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1", ipAddress = "127.0.0.1", deviceIdentifier = "brood", region = "local" })).Content.ReadAsStringAsync()); return d.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!; }
    private sealed class MutableClock(DateTimeOffset value) : IServerClock
    {
        public DateTimeOffset Now { get; set; } = value;
        public DateTimeOffset UtcNow => Now;
    }
    private sealed class ThrowingRepository : IHiveStateRepository { public Task<PlayerHiveState> ExecuteAtomicallyAsync(Guid p, Guid h, Func<PlayerHiveState, PlayerHiveState> m, CancellationToken c = default) => throw new InvalidOperationException(); public Task<PlayerHiveState?> ReadAsync(Guid p, Guid h, CancellationToken c = default) => throw new InvalidOperationException(); public Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid p, CancellationToken c = default) => throw new InvalidOperationException(); public Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(int limit, CancellationToken c = default) => throw new InvalidOperationException(); }
}
