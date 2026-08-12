using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.HiveOperations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;

namespace BeeKingdom.Tests;

public sealed class AdminSupportEndpointTests
{
    private const string SupportKey = "test-support-key";

    [Test]
    public async Task Disabled_flag_returns_game_unavailable()
    {
        await using var factory = CreateFactory(enabled: false);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/admin/v1/players/lookup?email=x@bee.test");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo("game.unavailable"));
    }

    [Test]
    public async Task Missing_or_wrong_key_is_unauthorized()
    {
        await using var factory = CreateFactory(enabled: true);
        using var client = factory.CreateClient();

        var noKey = await client.GetAsync("/admin/v1/players/lookup?email=x@bee.test");
        Assert.That(noKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        client.DefaultRequestHeaders.Add("X-BeeKingdom-Support-Key", "wrong-key");
        var wrongKey = await client.GetAsync("/admin/v1/players/lookup?email=x@bee.test");
        Assert.That(wrongKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Full_flow_lookup_diagnostics_adjust_and_audit()
    {
        var root = Path.Combine(Path.GetTempPath(), "admin-http-" + Guid.NewGuid().ToString("N"));
        await using var factory = CreateFactory(enabled: true, root: root);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Support-Key", SupportKey);
        try
        {
            string email = $"support-{Guid.NewGuid():N}@bee.test";
            factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");
            var lookup = await client.GetFromJsonAsync<JsonElement>("/admin/v1/players/lookup?email=" + Uri.EscapeDataString(email));
            Guid playerId = lookup.GetProperty("playerId").GetGuid();

            Guid hiveId = Guid.NewGuid();
            await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(playerId, hiveId, s => s);

            var hives = await client.GetFromJsonAsync<JsonElement>($"/admin/v1/players/{playerId:D}/hives");
            Assert.That(hives.GetProperty("hiveIds").EnumerateArray().Any(x => x.GetGuid() == hiveId), Is.True);

            var diagnostics = await client.GetFromJsonAsync<JsonElement>($"/admin/v1/players/{playerId:D}/hives/{hiveId:D}/diagnostics");
            long revision = diagnostics.GetProperty("revision").GetInt64();

            var adjust = await client.PostAsJsonAsync($"/admin/v1/players/{playerId:D}/hives/{hiveId:D}/resources/adjust", new { resource = "honey", delta = 500, reason = "Bug #7 refund", expectedRevision = revision });
            Assert.That(adjust.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var state = await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(playerId, hiveId);
            Assert.That(state!.Resources["honey"].Amount, Is.EqualTo(500));
            Assert.That(state.AdminAudit, Has.Count.EqualTo(1));
            Assert.That(state.AdminAudit![0].Reason, Is.EqualTo("Bug #7 refund"));

            var refreshed = await client.GetFromJsonAsync<JsonElement>($"/admin/v1/players/{playerId:D}/hives/{hiveId:D}/diagnostics");
            Assert.That(refreshed.GetProperty("adminAudit").GetArrayLength(), Is.EqualTo(1));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Adjust_without_reason_is_rejected()
    {
        var root = Path.Combine(Path.GetTempPath(), "admin-http-" + Guid.NewGuid().ToString("N"));
        await using var factory = CreateFactory(enabled: true, root: root);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Support-Key", SupportKey);
        try
        {
            Guid playerId = Guid.NewGuid(); Guid hiveId = Guid.NewGuid();
            await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(playerId, hiveId, s => s);
            var response = await client.PostAsJsonAsync($"/admin/v1/players/{playerId:D}/hives/{hiveId:D}/resources/adjust", new { resource = "honey", delta = 10, reason = "", expectedRevision = 0 });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static WebApplicationFactory<Program> CreateFactory(bool enabled, string? root = null) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("environment", "Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AdminSupport:Enabled"] = enabled.ToString(),
            ["AdminSupport:Key"] = SupportKey
        }));
        if (root is not null)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHiveStateRepository>();
                services.AddSingleton<IHiveStateRepository>(new DurableJsonHiveStateRepository(root, (p, h) => new PlayerHiveState(
                    p, h, HiveStateMigrator.CurrentModelVersion, 0,
                    new Dictionary<string, ResourceBalance> { ["honey"] = new(0, 100_000), ["pollen"] = new(0, 100_000) },
                    new(), [], new())));
            });
        }
    });
}
