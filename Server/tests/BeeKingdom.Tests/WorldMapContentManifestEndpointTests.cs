using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BeeKingdom.Tests;

public sealed class WorldMapContentManifestEndpointTests
{
    [Test]
    public async Task Default_closed_returns_content_unavailable()
    {
        await using var factory = Factory(new Dictionary<string, string?>());
        using var response = await factory.CreateClient().GetAsync("/runtime/world-map-content-manifest");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("content.unavailable"));
    }

    [Test]
    public async Task Enabled_manifest_is_bounded_https_and_etagged()
    {
        await using var factory = Factory(new Dictionary<string, string?>
        {
            ["WorldMapContentManifest:Enabled"] = "true",
            ["WorldMapContentManifest:Channel"] = "stable",
            ["WorldMapContentManifest:Version"] = "2026.07.22",
            ["WorldMapContentManifest:Platform"] = "android",
            ["WorldMapContentManifest:MinimumAppVersion"] = "1.0.0",
            ["WorldMapContentManifest:Bundles:0:BundleId"] = "worldmap-core",
            ["WorldMapContentManifest:Bundles:0:SizeBytes"] = "1234",
            ["WorldMapContentManifest:Bundles:0:Sha256"] = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            ["WorldMapContentManifest:Bundles:0:Uri"] = "https://cdn.example.invalid/worldmap-core-v1.bundle"
        });
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/runtime/world-map-content-manifest");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.ETag, Is.Not.Null);
        Assert.That(response.Headers.CacheControl?.Public, Is.True);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("contractVersion").GetString(), Is.EqualTo("world-map-content-v1"));
        Assert.That(json.RootElement.GetProperty("bundles").GetArrayLength(), Is.EqualTo(1));

        using HttpRequestMessage conditional = new(HttpMethod.Get, "/runtime/world-map-content-manifest");
        conditional.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(response.Headers.ETag!.Tag));
        using HttpResponseMessage notModified = await client.SendAsync(conditional);
        Assert.That(notModified.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
    }

    [TestCase("http://cdn.example.invalid/a.bundle")]
    [TestCase("https://user:pass@cdn.example.invalid/a.bundle")]
    [TestCase("https://cdn.example.invalid/a.bundle?token=x")]
    [TestCase("https://cdn.example.invalid/a.bundle#fragment")]
    public async Task Invalid_uri_is_closed_and_not_cacheable(string uri)
    {
        await using var factory = Factory(EnabledSettings(uri, "worldmap-core", "1234"));
        using var response = await factory.CreateClient().GetAsync("/runtime/world-map-content-manifest");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That(response.Headers.CacheControl?.NoStore, Is.True);
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain(uri));
    }

    [Test]
    public async Task Duplicate_and_oversized_bundles_are_closed()
    {
        var duplicate = EnabledSettings("https://cdn.example.invalid/a.bundle", "worldmap-core", "1234");
        duplicate["WorldMapContentManifest:Bundles:1:BundleId"] = "WORLDMAP-CORE";
        duplicate["WorldMapContentManifest:Bundles:1:SizeBytes"] = "1234";
        duplicate["WorldMapContentManifest:Bundles:1:Sha256"] = new string('b', 64);
        duplicate["WorldMapContentManifest:Bundles:1:Uri"] = "https://cdn.example.invalid/b.bundle";
        await using var factory = Factory(duplicate);
        using var response = await factory.CreateClient().GetAsync("/runtime/world-map-content-manifest");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That(response.Headers.CacheControl?.NoStore, Is.True);

        await using var oversizedFactory = Factory(EnabledSettings("https://cdn.example.invalid/a.bundle", "worldmap-core", "536870913"));
        using var oversized = await oversizedFactory.CreateClient().GetAsync("/runtime/world-map-content-manifest");
        Assert.That(oversized.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));

        var total = EnabledSettings("https://cdn.example.invalid/a.bundle", "worldmap-0", "500000000");
        for (int i = 1; i < 5; i++)
        {
            total[$"WorldMapContentManifest:Bundles:{i}:BundleId"] = $"worldmap-{i}";
            total[$"WorldMapContentManifest:Bundles:{i}:SizeBytes"] = "500000000";
            total[$"WorldMapContentManifest:Bundles:{i}:Sha256"] = new string((char)('a' + i), 64);
            total[$"WorldMapContentManifest:Bundles:{i}:Uri"] = $"https://cdn.example.invalid/{i}.bundle";
        }
        await using var totalFactory = Factory(total);
        using var totalResponse = await totalFactory.CreateClient().GetAsync("/runtime/world-map-content-manifest");
        Assert.That(totalResponse.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    private static Dictionary<string, string?> EnabledSettings(string uri, string id, string size) => new()
    {
        ["WorldMapContentManifest:Enabled"] = "true",
        ["WorldMapContentManifest:Channel"] = "stable",
        ["WorldMapContentManifest:Version"] = "2026.07.22",
        ["WorldMapContentManifest:Platform"] = "android",
        ["WorldMapContentManifest:MinimumAppVersion"] = "1.0.0",
        ["WorldMapContentManifest:Bundles:0:BundleId"] = id,
        ["WorldMapContentManifest:Bundles:0:SizeBytes"] = size,
        ["WorldMapContentManifest:Bundles:0:Sha256"] = new string('a', 64),
        ["WorldMapContentManifest:Bundles:0:Uri"] = uri
    };

    private static WebApplicationFactory<Program> Factory(IReadOnlyDictionary<string, string?> settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
        });
}
