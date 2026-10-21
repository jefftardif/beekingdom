using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class FloralSiteEndpointTests
{
    private static string NewSiteId() => "clover-" + Guid.NewGuid().ToString("N");

    [Test]
    public async Task Disabled_world_sites_are_unavailable()
    {
        await using var factory = Factory(false, NewSiteId());
        using var client = factory.CreateClient();
        Assert.That((await client.GetAsync("/game/v1/world/floral-sites")).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await client.GetAsync("/game/v1/world/floral-sites/any-site")).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Public_metadata_is_only_visible_to_authenticated_players()
    {
        await using var factory = Factory(true, NewSiteId());
        using var client = factory.CreateClient();
        Assert.That((await client.GetAsync("/game/v1/world/floral-sites")).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await client.GetAsync("/game/v1/world/floral-sites/any-site")).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Two_authenticated_players_observe_same_server_authoritative_site()
    {
        string siteId = NewSiteId();
        await using var factory = Factory(true, siteId);
        using var playerA = factory.CreateClient();
        using var playerB = factory.CreateClient();
        playerA.DefaultRequestHeaders.Authorization = new("Bearer",
            await Login(factory, playerA));
        playerB.DefaultRequestHeaders.Authorization = new("Bearer",
            await Login(factory, playerB));

        string route = "/game/v1/world/floral-sites/" + siteId;
        using var responseA = await playerA.GetAsync(route);
        using var responseB = await playerB.GetAsync(route);
        Assert.That(responseA.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(responseB.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var siteA = JsonDocument.Parse(await responseA.Content.ReadAsStringAsync());
        using var siteB = JsonDocument.Parse(await responseB.Content.ReadAsStringAsync());
        foreach (string property in new[] { "siteId", "worldId", "regionId", "revision",
                                             "pollenRemaining", "fertilityBp", "biodiversityBp" })
            Assert.That(siteA.RootElement.GetProperty(property).GetRawText(),
                Is.EqualTo(siteB.RootElement.GetProperty(property).GetRawText()), property);
        Assert.That(siteA.RootElement.GetProperty("siteId").GetString(),
            Is.EqualTo(siteId));
        Assert.That(siteA.RootElement.GetProperty("nameFr").GetString(),
            Is.EqualTo("Trèfle blanc"));
        Assert.That(siteA.RootElement.GetProperty("nameEn").GetString(),
            Is.EqualTo("White clover"));
        Assert.That(siteA.RootElement.GetProperty("pollenRemaining").GetInt64(),
            Is.EqualTo(100));

        using var list = await playerB.GetAsync("/game/v1/world/floral-sites");
        Assert.That(list.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var catalog = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.That(catalog.RootElement.GetArrayLength(), Is.EqualTo(2));
        Assert.That(catalog.RootElement[0].GetProperty("siteId").GetString(),
            Is.EqualTo(siteId));
    }

    [Test]
    public async Task Unknown_or_malformed_site_cannot_be_created_or_harvested()
    {
        await using var factory = Factory(true, NewSiteId());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer",
            await Login(factory, client));
        Assert.That((await client.GetAsync("/game/v1/world/floral-sites/unknown-site"))
            .StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await client.GetAsync("/game/v1/world/floral-sites/INVALID_KEY!"))
            .StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        // No mutation endpoint exists, so users cannot mint virtual pollen.
        Assert.That((await client.PostAsJsonAsync(
            "/game/v1/world/floral-sites/unknown-site/harvest",
            new { requestedPollen = 999999 })).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static WebApplicationFactory<Program> Factory(bool enabled, string siteId)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FloralSites:Enabled"] = enabled.ToString(),
                    ["FloralSites:CatalogVersion"] = "alpha.qa.v1",
                    ["FloralSites:Sites:0:WorldId"] = "world-qa",
                    ["FloralSites:Sites:0:RegionId"] = "region-qa",
                    ["FloralSites:Sites:0:SiteId"] = siteId,
                    ["FloralSites:Sites:0:FamilyId"] = "clover",
                    ["FloralSites:Sites:0:NameFr"] = "Trèfle blanc",
                    ["FloralSites:Sites:0:NameEn"] = "White clover",
                    ["FloralSites:Sites:0:MaximumPollenUnits"] = "100",
                    ["FloralSites:Sites:0:InitialFertilityBp"] = "6700",
                    ["FloralSites:Sites:0:InitialBiodiversityBp"] = "5000",
                    ["FloralSites:Sites:1:WorldId"] = "world-qa",
                    ["FloralSites:Sites:1:RegionId"] = "region-qa",
                    ["FloralSites:Sites:1:SiteId"] = "clover-partner-" + siteId,
                    ["FloralSites:Sites:1:FamilyId"] = "clover",
                    ["FloralSites:Sites:1:NameFr"] = "Pré de trèfles",
                    ["FloralSites:Sites:1:NameEn"] = "Clover meadow",
                    ["FloralSites:Sites:1:MaximumPollenUnits"] = "250",
                    ["FloralSites:Sites:1:InitialFertilityBp"] = "7500",
                    ["FloralSites:Sites:1:InitialBiodiversityBp"] = "5200"
                }));
        });
    }

    private static async Task<string> Login(
        WebApplicationFactory<Program> factory, HttpClient client)
    {
        string email = "floral-" + Guid.NewGuid().ToString("N") + "@bee.test";
        factory.Services.GetRequiredService<IAccountCredentialStore>()
            .CreateEmailAccount(email, "secret");
        using var response = await client.PostAsJsonAsync("/auth/login",
            new { email, password = "secret", clientVersion = "1",
                ipAddress = "127.0.0.1", deviceIdentifier = "flora",
                region = "local" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("tokens")
            .GetProperty("accessToken").GetString()!;
    }
}
