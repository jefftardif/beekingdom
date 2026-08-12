using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Authentication;
using BeeKingdom.Authentication.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class OfficialAuthenticationEndpointTests
{
    [Test]
    public async Task Readiness_is_public_minimal_and_contains_no_credentials()
    {
        await using var factory = Factory("Development"); using var client = factory.CreateClient();
        using var response = await client.GetAsync("/runtime/account-session-readiness"); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(json.RootElement.GetProperty("sessionCreationAllowed").GetBoolean(), Is.False);
        Assert.That(json.RootElement.GetProperty("tokenIssuanceAllowed").GetBoolean(), Is.False);
        Assert.That(json.RootElement.GetProperty("secretsAllowedInResponse").GetBoolean(), Is.False);
        Assert.That(json.RootElement.ToString(), Does.Not.Contain("accessToken"));
        Assert.That(json.RootElement.ToString(), Does.Not.Contain("refreshToken"));
    }

    [Test]
    public async Task Login_and_refresh_return_attested_identity_and_rotate_once()
    {
        await using var factory = Factory("Development"); using var client = factory.CreateClient(); var email = $"auth-{Guid.NewGuid():N}@bee.test"; factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");
        using var login = await client.PostAsJsonAsync("/auth/login", Login(email)); using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync()); var access = loginJson.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!; var refresh = loginJson.RootElement.GetProperty("tokens").GetProperty("refreshToken").GetString()!;
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK)); Assert.That(loginJson.RootElement.GetProperty("playerId").ValueKind, Is.EqualTo(JsonValueKind.Object)); Assert.That(loginJson.RootElement.GetProperty("session").GetProperty("sessionId").GetString(), Is.Not.Empty); Assert.That(loginJson.RootElement.GetProperty("tokens").GetProperty("accessTokenExpiresUtc").GetDateTimeOffset().Offset, Is.EqualTo(TimeSpan.Zero));
        using var rotated = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = refresh }); using var rotatedJson = JsonDocument.Parse(await rotated.Content.ReadAsStringAsync()); Assert.That(rotated.StatusCode, Is.EqualTo(HttpStatusCode.OK)); Assert.That(rotatedJson.RootElement.GetProperty("accessToken").GetString(), Is.Not.EqualTo(access)); Assert.That(rotatedJson.RootElement.GetProperty("refreshToken").GetString(), Is.Not.EqualTo(refresh)); Assert.That(rotatedJson.RootElement.GetProperty("playerId").ValueKind, Is.EqualTo(JsonValueKind.Object)); Assert.That(rotatedJson.RootElement.GetProperty("sessionId").GetString(), Is.EqualTo(loginJson.RootElement.GetProperty("session").GetProperty("sessionId").GetString())); Assert.That(rotatedJson.RootElement.GetProperty("refreshTokenExpiresUtc").GetDateTimeOffset().Offset, Is.EqualTo(TimeSpan.Zero));
        using var replay = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = refresh }); Assert.That(replay.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized)); Assert.That((await replay.Content.ReadAsStringAsync()), Does.Contain("auth.session_required"));
    }

    [Test]
    public async Task Login_ignores_json_ip_and_stores_connection_ip_only()
    {
        await using var factory = Factory("Development");
        using var client = factory.CreateClient();
        var email = $"ip-{Guid.NewGuid():N}@bee.test";
        factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");

        using var response = await client.PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1.0.0", ipAddress = "203.0.113.77", deviceIdentifier = "official-auth-tests", region = "local" });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(json.RootElement.ToString(), Does.Not.Contain("203.0.113.77"));
        string sessionId = json.RootElement.GetProperty("session").GetProperty("sessionId").GetString()!;
        AuthenticationSession? session = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().QuerySession(sessionId);
        Assert.That(session, Is.Not.Null);
        Assert.That(session!.IpAddress, Is.Not.EqualTo("203.0.113.77"));
        Assert.That(session.IpAddress, Is.Not.Empty);
    }

    [Test]
    public async Task Logout_uses_bearer_session_and_ignores_declared_session_id()
    {
        await using var factory = Factory("Development"); using var client = factory.CreateClient(); var email = $"logout-{Guid.NewGuid():N}@bee.test"; factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret"); using var login = await client.PostAsJsonAsync("/auth/login", Login(email)); using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync()); var access = loginJson.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access); using var logout = await client.PostAsJsonAsync("/auth/logout", new { sessionId = "attacker-session" }); Assert.That(logout.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var validate = await client.PostAsJsonAsync("/auth/validate", new { accessToken = access }); Assert.That(validate.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        using var second = await client.PostAsJsonAsync("/auth/logout", new { sessionId = "attacker-session" }); Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Invalid_requests_and_credentials_use_stable_errors()
    {
        await using var factory = Factory("Development"); using var client = factory.CreateClient();
        using var malformedLogin = await client.PostAsJsonAsync("/auth/login", new { email = "", password = "", clientVersion = "1" }); using var malformedRefresh = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = "" }); using var badCredentials = await client.PostAsJsonAsync("/auth/login", Login($"missing-{Guid.NewGuid():N}@bee.test"));
        Assert.That(malformedLogin.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest)); Assert.That(await malformedLogin.Content.ReadAsStringAsync(), Does.Contain("auth.invalid_request")); Assert.That(malformedRefresh.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest)); Assert.That(badCredentials.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized)); Assert.That(await badCredentials.Content.ReadAsStringAsync(), Does.Contain("auth.invalid_credentials"));
    }

    [Test]
    public async Task Two_players_have_independent_rotating_refresh_tokens()
    {
        await using var factory = Factory("Development"); using var client = factory.CreateClient(); var a = $"a-{Guid.NewGuid():N}@bee.test"; var b = $"b-{Guid.NewGuid():N}@bee.test"; var store = factory.Services.GetRequiredService<IAccountCredentialStore>(); store.CreateEmailAccount(a, "secret"); store.CreateEmailAccount(b, "secret"); using var loginA = await client.PostAsJsonAsync("/auth/login", Login(a)); using var loginB = await client.PostAsJsonAsync("/auth/login", Login(b)); using var jsonA = JsonDocument.Parse(await loginA.Content.ReadAsStringAsync()); using var jsonB = JsonDocument.Parse(await loginB.Content.ReadAsStringAsync()); var refreshA = jsonA.RootElement.GetProperty("tokens").GetProperty("refreshToken").GetString()!; var refreshB = jsonB.RootElement.GetProperty("tokens").GetProperty("refreshToken").GetString()!;
        using var rotatedA = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = refreshA }); using var rotatedB = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = refreshB }); Assert.That(rotatedA.StatusCode, Is.EqualTo(HttpStatusCode.OK)); Assert.That(rotatedB.StatusCode, Is.EqualTo(HttpStatusCode.OK)); using var replayA = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = refreshA }); Assert.That(replayA.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Session_limit_and_lockout_use_409_and_429()
    {
        await using var limited = Factory("Development", new Dictionary<string, string?> { ["Authentication:MaxSessionsPerAccount"] = "1" }); using var limitedClient = limited.CreateClient(); var limitedEmail = $"limit-{Guid.NewGuid():N}@bee.test"; limited.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(limitedEmail, "secret"); using var first = await limitedClient.PostAsJsonAsync("/auth/login", Login(limitedEmail)); using var second = await limitedClient.PostAsJsonAsync("/auth/login", Login(limitedEmail)); Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK)); Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.Conflict)); Assert.That(await second.Content.ReadAsStringAsync(), Does.Contain("auth.session_limit"));
        await using var locked = Factory("Development", new Dictionary<string, string?> { ["Authentication:MaxFailedAttempts"] = "1" }); using var lockedClient = locked.CreateClient(); var lockedEmail = $"locked-{Guid.NewGuid():N}@bee.test"; locked.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(lockedEmail, "secret"); using var failure = await lockedClient.PostAsJsonAsync("/auth/login", Login(lockedEmail, "wrong")); using var limitedFailure = await lockedClient.PostAsJsonAsync("/auth/login", Login(lockedEmail)); Assert.That(failure.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized)); Assert.That(limitedFailure.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests)); Assert.That(await limitedFailure.Content.ReadAsStringAsync(), Does.Contain("auth.rate_limited"));
    }

    private static WebApplicationFactory<Program> Factory(string environment, IReadOnlyDictionary<string, string?>? settings = null) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => { builder.UseSetting("environment", environment); if (settings is not null) builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings)); });
    private static object Login(string email, string password = "secret") => new { email, password, clientVersion = "1.0.0", ipAddress = "127.0.0.1", deviceIdentifier = "official-auth-tests", region = "local" };
}
