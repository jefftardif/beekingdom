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

public sealed class FloralAllianceProjectEndpointTests
{
    [Test]
    public async Task Project_feature_disabled_by_default_returns_503()
    {
        await using var factory = Factory(enabled: false);
        using var client = factory.CreateClient();
        string route = $"/alliance/v1/alliances/{Guid.NewGuid():D}/floral-projects";
        Assert.That((await client.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await client.PostAsJsonAsync(route,
            new { siteId = "clover-qa", expectedProgressionRevision = 0,
                idempotencyKey = "disabled-project-01" })).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Json_storage_cannot_activate_collective_economy()
    {
        await using var factory = Factory(enabled: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            await Login(factory, client));
        string route = $"/alliance/v1/alliances/{Guid.NewGuid():D}/floral-projects";
        Assert.That((await client.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await client.PostAsJsonAsync(route,
            new { siteId = "clover-qa", expectedProgressionRevision = 0,
                idempotencyKey = "json-rejected-01" })).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Authenticated_alliance_route_validates_token_and_guid_first()
    {
        await using var factory = Factory(enabled: true, withBoundary: true);
        using var client = factory.CreateClient();
        string route = $"/alliance/v1/alliances/{Guid.NewGuid():D}/floral-projects";
        Assert.That((await client.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await client.PostAsJsonAsync(route,
            new { siteId = "clover-qa", expectedProgressionRevision = 0,
                idempotencyKey = "unauth-project-01" })).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            await Login(factory, client));
        Assert.That((await client.GetAsync(
            "/alliance/v1/alliances/invalid-guid/floral-projects")).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await client.GetAsync(route + "/bad-id")).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await client.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    private sealed class EmptyAuthorizedBoundary : IFloralAllianceProjectService
    {
        public Task<IReadOnlyList<FloralAllianceProjectSnapshot>> ListAsync(
            Guid playerId, Guid allianceId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FloralAllianceProjectSnapshot>>(
                Array.Empty<FloralAllianceProjectSnapshot>());

        public Task<FloralAllianceProjectSnapshot> ReadAsync(
            Guid playerId, Guid allianceId, Guid projectId,
            CancellationToken ct = default)
            => throw new KeyNotFoundException();

        public Task<FloralAllianceProjectCreateResult> CreateAsync(
            Guid playerId, Guid allianceId, CreateFloralAllianceProjectRequest request,
            CancellationToken ct = default)
            => throw new UnauthorizedAccessException();
    }

    private static WebApplicationFactory<Program> Factory(
        bool enabled, bool withBoundary = false) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FloralAllianceProjects:Enabled"] = enabled.ToString(),
                    ["FloralSites:Enabled"] = "true"
                }));
            if (withBoundary)
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IFloralAllianceProjectService>();
                    services.AddSingleton<IFloralAllianceProjectService>(
                        new EmptyAuthorizedBoundary());
                });
        });

    private static async Task<string> Login(
        WebApplicationFactory<Program> factory, HttpClient client)
    {
        string email = "collective-" + Guid.NewGuid().ToString("N") + "@bee.test";
        factory.Services.GetRequiredService<IAccountCredentialStore>()
            .CreateEmailAccount(email, "secret");
        using HttpResponseMessage response = await client.PostAsJsonAsync("/auth/login",
            new { email, password = "secret", clientVersion = "1",
                ipAddress = "127.0.0.1", deviceIdentifier = "collective",
                region = "local" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("tokens").GetProperty("accessToken")
            .GetString()!;
    }
}
