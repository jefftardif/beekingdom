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

public sealed class HiveDailyRoundEndpointTests
{
    [Test]
    public async Task Invalid_requests_return_400()
    {
        await using var factory = CreateFactory(true); using var client = factory.CreateClient(); string token = await Login(factory, client); client.DefaultRequestHeaders.Authorization = new("Bearer", token); var hive = Guid.NewGuid(); var day = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        foreach (var (url, body) in new[] { ("/game/v1/hives/not-guid/daily-round/claim", new { expectedRevision = 0L, idempotencyKey = "x", expectedDayUtc = day }), ($"/game/v1/hives/{hive:D}/daily-round/claim", new { expectedRevision = -1L, idempotencyKey = "x", expectedDayUtc = day }), ($"/game/v1/hives/{hive:D}/daily-round/claim", new { expectedRevision = long.MaxValue, idempotencyKey = "x", expectedDayUtc = day }), ($"/game/v1/hives/{hive:D}/daily-round/claim", new { expectedRevision = 0L, idempotencyKey = " ", expectedDayUtc = day }), ($"/game/v1/hives/{hive:D}/daily-round/claim", new { expectedRevision = 0L, idempotencyKey = "x", expectedDayUtc = "bad" }) }) { var r = await client.PostAsJsonAsync(url, body); Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest)); }
    }

    [Test]
    public async Task Closed_flag_returns_503_before_auth_for_get_and_claim()
    {
        await using var factory = CreateFactory(false); using var client = factory.CreateClient(); var hive = Guid.NewGuid();
        var get = await client.GetAsync($"/game/v1/hives/{hive:D}/daily-round"); var post = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/daily-round/claim", new { expectedRevision = 0, idempotencyKey = "x", expectedDayUtc = "2026-07-23" });
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable)); Assert.That(post.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Enabled_requires_auth_and_returns_camel_case_snapshot()
    {
        await using var factory = CreateFactory(true); using var client = factory.CreateClient(); var hive = Guid.NewGuid();
        var unauthorized = await client.GetAsync($"/game/v1/hives/{hive:D}/daily-round"); Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        string token = await Login(factory, client); client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var player = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value;
        var day = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);
        await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player, hive, s => s with { DailyRound = new HiveDailyRoundState(day, true, true, true, null), Resources = new Dictionary<string,ResourceBalance> { ["honey"] = new(0,1000), ["pollen"] = new(0,1000) } });
        var response = await client.GetAsync($"/game/v1/hives/{hive:D}/daily-round"); Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK)); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("contractVersion").GetString(), Is.EqualTo("living-hive-daily-round-v1")); Assert.That(DateTimeOffset.Parse(json.RootElement.GetProperty("dayUtc").GetString()!).Date, Is.EqualTo(day.Date)); Assert.That(json.RootElement.GetProperty("honeyReward").GetInt64(), Is.EqualTo(120));
    }

    [Test]
    public async Task Claim_returns_typed_receipt_and_replay_conflict()
    {
        await using var factory = CreateFactory(true); using var client = factory.CreateClient(); string token = await Login(factory, client); client.DefaultRequestHeaders.Authorization = new("Bearer", token); var player = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value; var hive = Guid.NewGuid();
        var day = new DateTimeOffset(DateTimeOffset.UtcNow.UtcDateTime.Date, TimeSpan.Zero);
        await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player, hive, s => s with { DailyRound = new HiveDailyRoundState(day, true, true, true, null), Resources = new Dictionary<string,ResourceBalance> { ["honey"] = new(0,1000), ["pollen"] = new(0,1000) } });
        var dayText = day.ToString("yyyy-MM-dd"); var body = new { expectedRevision = 0, idempotencyKey = "claim-http", expectedDayUtc = dayText }; var first = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/daily-round/claim", body); Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK)); using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync()); var receipt1 = firstJson.RootElement.GetProperty("receipt").Clone(); Assert.That(receipt1.GetProperty("playerId").GetString(), Is.EqualTo(player.ToString())); Assert.That(receipt1.GetProperty("hiveId").GetString(), Is.EqualTo(hive.ToString())); Assert.That(receipt1.GetProperty("idempotencyKey").GetString(), Is.EqualTo("claim-http")); Assert.That(receipt1.GetProperty("dayUtc").GetDateTimeOffset(), Is.EqualTo(day)); Assert.That(receipt1.GetProperty("revisionBefore").GetInt64(), Is.EqualTo(0)); Assert.That(receipt1.GetProperty("revisionAfter").GetInt64(), Is.EqualTo(1)); Assert.That(receipt1.GetProperty("creditedHoney").GetInt64(), Is.EqualTo(120)); Assert.That(receipt1.GetProperty("creditedPollen").GetInt64(), Is.EqualTo(60)); Assert.That(receipt1.GetProperty("code").GetString(), Is.EqualTo("game.daily_round_claimed"));
        var stateAfter = (await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(player, hive))!; Assert.That(stateAfter.Resources["honey"].Amount, Is.EqualTo(120)); Assert.That(stateAfter.Resources["pollen"].Amount, Is.EqualTo(60)); await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player, hive, s => s with { Revision = s.Revision + 1 });
        var replay = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/daily-round/claim", body); Assert.That(replay.StatusCode, Is.EqualTo(HttpStatusCode.OK)); using var replayJson = JsonDocument.Parse(await replay.Content.ReadAsStringAsync()); Assert.That(replayJson.RootElement.GetProperty("receipt").GetRawText(), Is.EqualTo(receipt1.GetRawText())); Assert.That(replayJson.RootElement.GetProperty("snapshot").GetProperty("revision").GetInt64(), Is.EqualTo(2)); var stateReplay = (await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(player, hive))!; Assert.That(stateReplay.Resources["honey"].Amount, Is.EqualTo(120)); Assert.That(stateReplay.Resources["pollen"].Amount, Is.EqualTo(60));
        var conflict = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/daily-round/claim", new { expectedRevision = 1, idempotencyKey = "claim-http", expectedDayUtc = dayText }); Assert.That(conflict.StatusCode, Is.EqualTo(HttpStatusCode.Conflict)); using var conflictJson = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync()); Assert.That(conflictJson.RootElement.GetProperty("code").GetString(), Is.EqualTo("game.idempotency_conflict"));
    }

    private static WebApplicationFactory<Program> CreateFactory(bool enabled) => new WebApplicationFactory<Program>().WithWebHostBuilder(b => { b.UseSetting("environment", "Development"); b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string,string?> { ["HiveDailyRound:Enabled"] = enabled.ToString() })); });
    private static async Task<string> Login(WebApplicationFactory<Program> factory, HttpClient client) { string email = $"daily-{Guid.NewGuid():N}@bee.test"; factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret"); var r = await client.PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1.0.0", ipAddress = "127.0.0.1", deviceIdentifier = "daily-tests", region = "local" }); using var j = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return j.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!; }
}
