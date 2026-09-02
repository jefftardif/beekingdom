using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

// M043B-CL: full HTTP round-trip for GET /game/v1/players/search - auth gate, the "blank query
// extracts everyone" guard, and a real successful call end-to-end through the real DI container.
public sealed class PlayerDirectoryEndpointTests
{
    [Test]
    public async Task Search_RequiresAuthentication()
    {
        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("environment", "Development"));
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/game/v1/players/search?q=queen");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(body.RootElement.GetProperty("code").GetString(), Is.EqualTo("game.session_required"));
    }

    [Test]
    public async Task Search_RejectsBlankOrTooShortQuery()
    {
        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("environment", "Development"));
        using HttpClient client = factory.CreateClient();
        string token = await LoginTestAccount(factory, client, $"directory-blank-{Guid.NewGuid():N}@bee.test");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage blank = await client.GetAsync("/game/v1/players/search?q=");
        HttpResponseMessage tooShort = await client.GetAsync("/game/v1/players/search?q=a");

        Assert.That(blank.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(tooShort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Search_AuthenticatedRealQuery_ReturnsOkArray()
    {
        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("environment", "Development"));
        using HttpClient client = factory.CreateClient();
        string token = await LoginTestAccount(factory, client, $"directory-real-{Guid.NewGuid():N}@bee.test");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.GetAsync("/game/v1/players/search?q=queenbee");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(body.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
    }

    private static async Task<string> LoginTestAccount(WebApplicationFactory<Program> factory, HttpClient client, string email)
    {
        factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password = "secret",
            clientVersion = "1.0.0",
            ipAddress = "127.0.0.1",
            deviceIdentifier = "player-directory-tests",
            region = "local"
        });
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using JsonDocument login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return login.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;
    }
}
