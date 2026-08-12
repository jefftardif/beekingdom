using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.HiveOperations;
using BeeKingdom.Authentication.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class GameFoundationEndpointTests
{
    [Test]
    public async Task Default_closed_flag_returns_game_unavailable_without_mutation()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(enabled: false);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Guid hiveId = Guid.NewGuid();

        HttpResponseMessage response = await client.PostAsJsonAsync($"/game/v1/hives/{hiveId:D}/chapter-1/foundation", new { expectedRevision = 0, choice = "honey_reserve", idempotencyKey = "closed-foundation" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(body.RootElement.GetProperty("code").GetString(), Is.EqualTo("game.unavailable"));
        Assert.That(await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(Guid.NewGuid(), hiveId), Is.Null);
    }

    [Test]
    public async Task Enabled_test_route_uses_game_contract_and_idempotent_proof()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(enabled: true);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Guid hiveId = Guid.NewGuid();
        HttpResponseMessage unauthorized = await client.PostAsJsonAsync($"/game/v1/hives/{hiveId:D}/chapter-1/foundation", new { expectedRevision = 0, choice = "honey_reserve", idempotencyKey = "foundation-http-1" });
        Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        using JsonDocument unauthorizedJson = JsonDocument.Parse(await unauthorized.Content.ReadAsStringAsync());
        Assert.That(unauthorizedJson.RootElement.GetProperty("code").GetString(), Is.EqualTo("game.session_required"));

        using HttpClient authenticated = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        string token = await LoginTestAccount(factory, authenticated, $"foundation-{Guid.NewGuid():N}@bee.test");
        authenticated.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        Guid playerId = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId.Value;
        await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(playerId, hiveId, state => state with { InstallationComplete = true });

        HttpResponseMessage invalidId = await authenticated.PostAsJsonAsync("/game/v1/hives/not-a-guid/chapter-1/foundation", new { expectedRevision = 0, choice = "honey_reserve", idempotencyKey = "invalid-id" });
        Assert.That(invalidId.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(JsonDocument.Parse(await invalidId.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString(), Is.EqualTo("game.invalid_request"));
        HttpResponseMessage invalidChoice = await authenticated.PostAsJsonAsync($"/game/v1/hives/{hiveId:D}/chapter-1/foundation", new { expectedRevision = 0, choice = "other", idempotencyKey = "invalid-choice" });
        Assert.That(invalidChoice.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        HttpResponseMessage emptyKey = await authenticated.PostAsJsonAsync($"/game/v1/hives/{hiveId:D}/chapter-1/foundation", new { expectedRevision = 0, choice = "honey_reserve", idempotencyKey = "" });
        Assert.That(emptyKey.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        HttpResponseMessage first = await authenticated.PostAsJsonAsync($"/game/v1/hives/{hiveId:D}/chapter-1/foundation", new { expectedRevision = 0, choice = "mixed_foundation", idempotencyKey = "foundation-http-2" });
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using JsonDocument firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        string proof = firstJson.RootElement.GetProperty("proof").GetString()!;
        Assert.Multiple(() =>
        {
            Assert.That(firstJson.RootElement.GetProperty("choice").GetString(), Is.EqualTo("mixed_foundation"));
            Assert.That(firstJson.RootElement.GetProperty("honeyAwarded").GetInt64(), Is.EqualTo(170));
            Assert.That(firstJson.RootElement.GetProperty("pollenAwarded").GetInt64(), Is.EqualTo(80));
        });

        HttpResponseMessage retry = await authenticated.PostAsJsonAsync($"/game/v1/hives/{hiveId:D}/chapter-1/foundation", new { expectedRevision = 0, choice = "mixed_foundation", idempotencyKey = "foundation-http-2" });
        Assert.That(retry.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using JsonDocument retryJson = JsonDocument.Parse(await retry.Content.ReadAsStringAsync());
        Assert.That(retryJson.RootElement.GetProperty("proof").GetString(), Is.EqualTo(proof));

        HttpResponseMessage conflict = await authenticated.PostAsJsonAsync($"/game/v1/hives/{hiveId:D}/chapter-1/foundation", new { expectedRevision = 0, choice = "honey_reserve", idempotencyKey = "foundation-http-2" });
        Assert.That(conflict.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(JsonDocument.Parse(await conflict.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString(), Is.EqualTo("game.idempotency_conflict"));
        HttpResponseMessage secondChoice = await authenticated.PostAsJsonAsync($"/game/v1/hives/{hiveId:D}/chapter-1/foundation", new { expectedRevision = 1, choice = "honey_reserve", idempotencyKey = "foundation-http-3" });
        Assert.That(secondChoice.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(JsonDocument.Parse(await secondChoice.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString(), Is.EqualTo("game.foundation_conflict"));
    }

    private static WebApplicationFactory<Program> CreateFactory(bool enabled)
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FoundingFoundation:Enabled"] = enabled.ToString()
            }));
        });

    private static async Task<string> LoginTestAccount(WebApplicationFactory<Program> factory, HttpClient client, string email)
    {
        factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password = "secret",
            clientVersion = "1.0.0",
            ipAddress = "127.0.0.1",
            deviceIdentifier = "foundation-tests",
            region = "local"
        });
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using JsonDocument login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return login.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;
    }
}
