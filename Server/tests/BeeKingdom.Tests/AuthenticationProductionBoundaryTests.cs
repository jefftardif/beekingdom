using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BeeKingdom.Tests;

public sealed class AuthenticationProductionBoundaryTests
{
    [Test]
    public async Task Production_keeps_account_creation_and_token_issuance_closed()
    {
        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting("environment", "Production"));
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage register = await client.PostAsJsonAsync("/accounts", new { displayName = "closed", email = "closed@example.test" });
        HttpResponseMessage login = await client.PostAsJsonAsync("/auth/login", new { email = "closed@example.test", password = "secret", clientVersion = "1", ipAddress = "127.0.0.1", deviceIdentifier = "test", region = "local" });
        HttpResponseMessage refresh = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = "not-issued" });
        Assert.Multiple(() => { Assert.That(register.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable)); Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable)); Assert.That(refresh.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable)); });
        string payload = await login.Content.ReadAsStringAsync();
        Assert.That(payload, Does.Contain("auth.unavailable"));
        Assert.That(payload, Does.Not.Contain("accessToken"));
        Assert.That(payload, Does.Not.Contain("refreshToken"));
    }
}
