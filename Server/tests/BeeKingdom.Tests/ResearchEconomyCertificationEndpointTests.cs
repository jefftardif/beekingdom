using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.HiveOperations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuthenticationManager = BeeKingdom.Authentication.AuthenticationManager;

namespace BeeKingdom.Tests;

public sealed class ResearchEconomyCertificationEndpointTests
{
    [Test]
    public async Task AuthenticatedReconnectPreservesResearchAndSpeedUpReceiptThroughCompletion()
    {
        var clock = new TestClock();
        var settings = new Dictionary<string, string?>
        {
            ["LivingHiveResearch:Enabled"] = "true",
            ["LivingHiveResearch:CatalogVersion"] = "m096-v1",
            ["SpeedUps:Enabled"] = "true"
        };
        int index = 0;
        foreach (string id in HiveOperationService.ResearchCatalog.Keys)
            settings[$"LivingHiveResearch:Catalog:{index++}"] = id;
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
            builder.ConfigureServices(services => services.AddSingleton<IServerClock>(clock));
        });
        string email = $"m096-{Guid.NewGuid():N}@bee.test";
        factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");
        using var client = factory.CreateClient();
        Guid player = await Login(client, factory, email);
        Guid hive = Guid.NewGuid();
        var repository = factory.Services.GetRequiredService<IHiveStateRepository>();
        var seed = await repository.ExecuteAtomicallyAsync(player, hive, state => state with
        {
            Resources = new() { ["honey"] = new(100000, 100000), ["wax"] = new(100000, 100000), ["pollen"] = new(100000, 100000) },
            Research = new(new(), null), SpeedUps = new() { ["research_300s"] = 2 }
        });
        string path = $"/game/v1/hives/{hive}";
        var catalog = await client.GetFromJsonAsync<ResearchReadSnapshot>(path + "/research");
        Assert.That(catalog!.Offers.Count, Is.EqualTo(10));
        var started = await Post<ResearchResponse>(client, path + "/research/foraging_routes_i/start", new ResearchMutationRequest(seed.Revision, "start"));
        Assert.That(started.Snapshot.ActiveOperation!.CompletesAtUtc, Is.EqualTo(clock.UtcNow.AddSeconds(120)));

        using var reconnected = factory.CreateClient();
        Assert.That(await Login(reconnected, factory, email), Is.EqualTo(player));
        var resumed = await reconnected.GetFromJsonAsync<ResearchReadSnapshot>(path + "/research");
        Assert.That(resumed!.ActiveOperation, Is.EqualTo(started.Snapshot.ActiveOperation));
        Assert.That(resumed.Balances["honey"].Amount, Is.EqualTo(99760));
        var speedRequest = new ApplySpeedUpRequest("research_300s", "research", "foraging_routes_i", 300, resumed.Revision, "speed");
        var accelerated = await Post<SpeedUpApplyResponse>(reconnected, path + "/speedups/research/apply", speedRequest);
        var replay = await Post<SpeedUpApplyResponse>(reconnected, path + "/speedups/research/apply", speedRequest);
        Assert.That(replay.Receipt, Is.EqualTo(accelerated.Receipt));
        Assert.That(replay.Snapshot.Inventory.Items["research_300s"], Is.EqualTo(1));
        var ready = await reconnected.GetFromJsonAsync<ResearchReadSnapshot>(path + "/research");
        Assert.That(ready!.ActiveOperation!.Status, Is.EqualTo("awaiting_completion"));
        using var tooLate = await reconnected.PostAsJsonAsync(path + "/speedups/research/apply",
            speedRequest with { ExpectedRevision = ready.Revision, IdempotencyKey = "too-late" });
        Assert.That(tooLate.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var completed = await Post<ResearchResponse>(reconnected, path + $"/research/{started.Receipt.OperationId}/complete",
            new ResearchMutationRequest(ready.Revision, "complete"));
        Assert.That(completed.Snapshot.ActiveOperation, Is.Null);
        Assert.That(completed.Snapshot.Completed.Single().Effects.HoneyProductionBonusBps, Is.EqualTo(200));
        var finalReplay = await Post<SpeedUpApplyResponse>(reconnected, path + "/speedups/research/apply", speedRequest);
        Assert.That(finalReplay.Receipt, Is.EqualTo(accelerated.Receipt));
        Assert.That(finalReplay.Snapshot.Revision, Is.EqualTo(completed.Snapshot.Revision));
        Assert.That(finalReplay.Snapshot.Inventory.Items["research_300s"], Is.EqualTo(1));
    }

    private static async Task<Guid> Login(HttpClient client, WebApplicationFactory<Program> factory, string email)
    {
        using var response = await client.PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1.0" });
        response.EnsureSuccessStatusCode();
        using var auth = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string token = auth.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return factory.Services.GetRequiredService<AuthenticationManager>().ValidateToken(token).PlayerId!.Value;
    }

    private static async Task<T> Post<T>(HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(path, body);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<T>();
        Assert.That(result, Is.Not.Null);
        return result!;
    }

    private sealed class TestClock : IServerClock
    {
        public DateTimeOffset UtcNow { get; } = DateTimeOffset.UtcNow;
    }
}
