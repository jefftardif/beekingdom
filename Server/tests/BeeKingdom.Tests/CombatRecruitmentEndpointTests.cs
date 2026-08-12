using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.HiveOperations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class CombatRecruitmentEndpointTests
{
    [Test]
    public async Task Closed_flag_short_circuits_all_recruitment_routes()
    {
        await using var f = Factory(false); using var c = f.CreateClient(); var h = Guid.NewGuid();
        Assert.That((await c.GetAsync($"/game/v1/hives/{h:D}/combat/recruitment")).StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await c.PostAsJsonAsync($"/game/v1/hives/{h:D}/combat/recruitment/start", new { family = "guardians", expectedRevision = 0, idempotencyKey = "x" })).StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Enabled_routes_require_auth_and_reject_invalid_requests()
    {
        await using var f = Factory(true); using var c = f.CreateClient(); var h = Guid.NewGuid();
        Assert.That((await c.GetAsync($"/game/v1/hives/{h:D}/combat/recruitment")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        var token = await Login(f, c); c.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var bad = await c.PostAsJsonAsync($"/game/v1/hives/{h:D}/combat/recruitment/start", new { family = "guardians", expectedRevision = -1, idempotencyKey = "bad key" });
        Assert.That(bad.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Enabled_start_replay_claim_early_and_player_isolation()
    {
        await using var f = Factory(true); using var c = f.CreateClient(); var token = await Login(f, c); c.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var auth = f.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>(); var p = auth.ValidateToken(token).PlayerId!.Value; var h = Guid.NewGuid();
        await f.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(p, h, s => s with { Resources = new Dictionary<string, ResourceBalance> { ["honey"] = new(1000, 1000), ["pollen"] = new(1000, 1000) }, BuildingLevels = new Dictionary<string, int> { ["guard_post"] = 1 }, DoctrineRoster = new DoctrineRosterState(0, new Dictionary<string, long> { ["guardians"] = 0, ["wingrunners"] = 0, ["darters"] = 0 }, null, new()) });
        var start = await c.PostAsJsonAsync($"/game/v1/hives/{h:D}/combat/recruitment/start", new { family = "guardians", expectedRevision = 0, idempotencyKey = "recruit-1" }); Assert.That(start.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var doc = JsonDocument.Parse(await start.Content.ReadAsStringAsync()); var op = doc.RootElement.GetProperty("snapshot").GetProperty("activeOperation").GetProperty("operationId").GetGuid();
        var replay = await c.PostAsJsonAsync($"/game/v1/hives/{h:D}/combat/recruitment/start", new { family = "guardians", expectedRevision = 0, idempotencyKey = "recruit-1" }); Assert.That(replay.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var early = await c.PostAsJsonAsync($"/game/v1/hives/{h:D}/combat/recruitment/{op:D}/claim", new { expectedRevision = 1, idempotencyKey = "claim-1" }); Assert.That(early.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        using var foreign = f.CreateClient(); var foreignToken = await Login(f, foreign); foreign.DefaultRequestHeaders.Authorization = new("Bearer", foreignToken); Assert.That((await foreign.GetAsync($"/game/v1/hives/{h:D}/combat/recruitment")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static WebApplicationFactory<Program> Factory(bool enabled) => new WebApplicationFactory<Program>().WithWebHostBuilder(b => { b.UseSetting("environment", "Development"); b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["CombatRecruitment:Enabled"] = enabled.ToString(), ["CombatFormationReadiness:Enabled"] = "true" })); });
    private static async Task<string> Login(WebApplicationFactory<Program> f, HttpClient c) { var email = $"recruit-{Guid.NewGuid():N}@bee.test"; f.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret"); using var d = JsonDocument.Parse(await (await c.PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1", ipAddress = "127.0.0.1", deviceIdentifier = "recruit", region = "local" })).Content.ReadAsStringAsync()); return d.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!; }
}
