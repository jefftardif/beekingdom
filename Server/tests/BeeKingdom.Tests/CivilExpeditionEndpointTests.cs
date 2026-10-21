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

public sealed class CivilExpeditionEndpointTests
{
    [Test]
    public async Task Default_disabled_endpoints_cannot_launch_or_mutate()
    {
        await using var factory = Factory(false);
        using var client = factory.CreateClient();
        string url = $"/game/v1/hives/{Guid.NewGuid():D}/civil-expeditions";
        Assert.That((await client.GetAsync(url)).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await client.PostAsJsonAsync(url + "/start", new
        {
            siteId = "clover-field-qa", workers = 100,
            expectedHiveRevision = 0, expectedSiteRevision = 0,
            idempotencyKey = "disabled-command-01"
        })).StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Json_storage_is_read_only_even_if_configuration_accidentally_enabled()
    {
        await using var factory = Factory(true);
        using var client = factory.CreateClient();
        string token = await Login(factory, client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Guid player = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;
        Guid hive = Guid.NewGuid();
        string url = $"/game/v1/hives/{hive:D}/civil-expeditions";
        Assert.That((await client.GetAsync(url)).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await client.PostAsJsonAsync(url + "/start", new
        {
            siteId = "clover-field-qa", workers = 100,
            expectedHiveRevision = 0, expectedSiteRevision = 0,
            idempotencyKey = "json-rejected-01"
        })).StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That(await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ReadAsync(player, hive), Is.Null);
    }

    [Test]
    public async Task Auth_and_identifiers_are_checked_before_any_uow()
    {
        await using var factory = Factory(true, fakeSql: true);
        using var client = factory.CreateClient();
        string url = $"/game/v1/hives/{Guid.NewGuid():D}/civil-expeditions";
        Assert.That((await client.GetAsync(url)).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await client.PostAsJsonAsync(url + "/start", new
        {
            siteId = "clover-field-qa", workers = 20,
            expectedHiveRevision = 0, expectedSiteRevision = 0,
            idempotencyKey = "unauth-request-01"
        })).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            await Login(factory, client));
        Assert.That((await client.GetAsync(
            "/game/v1/hives/bad-id/civil-expeditions")).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await client.GetAsync(url)).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await client.PostAsJsonAsync(url + "/start", new
        {
            siteId = "clover-field-qa", workers = 20,
            expectedHiveRevision = 0, expectedSiteRevision = 0,
            idempotencyKey = "missing-hive-01"
        })).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private sealed class TestSqlBoundary : ICivilExpeditionUnitOfWork
    {
        public Task<CivilExpeditionMutation> ExecuteAsync(
            Guid playerId, Guid hiveId, FloralSiteDefinition site,
            string catalogVersion,
            Func<PlayerHiveState, FloralSiteState, CivilExpeditionMutation> change,
            CancellationToken ct = default)
            => throw new InvalidOperationException(
                "Test boundary must reject unauthenticated or nonexistent hive BEFORE coordinator.");
    }

    private static WebApplicationFactory<Program> Factory(bool enabled, bool fakeSql = false) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CivilExpeditions:Enabled"] = enabled.ToString(),
                    ["FloralSites:Enabled"] = "true",
                    ["FloralSites:CatalogVersion"] = "alpha-qa-20261021",
                    ["FloralSites:Sites:0:WorldId"] = "world-qa",
                    ["FloralSites:Sites:0:RegionId"] = "region-qa",
                    ["FloralSites:Sites:0:SiteId"] = "clover-field-qa",
                    ["FloralSites:Sites:0:FamilyId"] = "clover",
                    ["FloralSites:Sites:0:NameFr"] = "Champ de trèfle",
                    ["FloralSites:Sites:0:NameEn"] = "Clover Field",
                    ["FloralSites:Sites:0:MaximumPollenUnits"] = "100",
                    ["FloralSites:Sites:0:InitialFertilityBp"] = "6000",
                    ["FloralSites:Sites:0:InitialBiodiversityBp"] = "5500"
                }));
            if (fakeSql)
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ICivilExpeditionUnitOfWork>();
                    services.AddSingleton<ICivilExpeditionUnitOfWork>(
                        new TestSqlBoundary());
                });
        });

    private static async Task<string> Login(
        WebApplicationFactory<Program> factory, HttpClient client)
    {
        string email = "civil-flight-" + Guid.NewGuid().ToString("N") + "@bee.test";
        factory.Services.GetRequiredService<IAccountCredentialStore>()
            .CreateEmailAccount(email, "secret");
        using var response = await client.PostAsJsonAsync("/auth/login",
            new { email, password = "secret", clientVersion = "1",
                ipAddress = "127.0.0.1", deviceIdentifier = "civil",
                region = "local" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("tokens")
            .GetProperty("accessToken").GetString()!;
    }
}
