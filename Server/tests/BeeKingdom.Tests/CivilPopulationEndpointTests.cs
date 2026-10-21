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

public sealed class CivilPopulationEndpointTests
{
    private static readonly DateTimeOffset Origin =
        new(2026, 10, 21, 19, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Feature_disabled_endpoints_return_503()
    {
        await using var factory = Factory(false);
        using var client = factory.CreateClient();
        string route = $"/game/v1/hives/{Guid.NewGuid():D}/civil-population";
        Assert.That((await client.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await client.PostAsJsonAsync(route + "/cohorts/start",
            new { expectedRevision = 0, idempotencyKey = "test-disabled-001" })).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Feature_enabled_requires_authentication()
    {
        await using var factory = Factory(true);
        using var client = factory.CreateClient();
        string route = $"/game/v1/hives/{Guid.NewGuid():D}/civil-population";
        Assert.That((await client.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await client.PostAsJsonAsync(route + "/cohorts/start",
            new { expectedRevision = 0, idempotencyKey = "test-unauth-001" })).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Authenticated_nursery_birth_matures_offline_without_double_award()
    {
        var clock = new MutableClock(Origin);
        await using var factory = Factory(true, clock);
        using var client = factory.CreateClient();
        string token = await Login(factory, client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Guid player = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;
        Guid hive = Guid.NewGuid();
        await Seed(factory, player, hive);
        string route = $"/game/v1/hives/{hive:D}/civil-population";

        using var initial = JsonDocument.Parse(await (await client.GetAsync(route))
            .Content.ReadAsStringAsync());
        Assert.That(initial.RootElement.GetProperty("availableWorkers").GetInt64(), Is.Zero);

        using var response = await client.PostAsJsonAsync(route + "/cohorts/start",
            new { expectedRevision = 0, idempotencyKey = "http-civil-start-001" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var started = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(started.RootElement.GetProperty("succeeded").GetBoolean(), Is.True);
        Assert.That(started.RootElement.GetProperty("snapshot").GetProperty("active")
            .GetProperty("expectedWorkers").GetInt64(), Is.EqualTo(20));

        using var replay = await client.PostAsJsonAsync(route + "/cohorts/start",
            new { expectedRevision = 0, idempotencyKey = "http-civil-start-001" });
        Assert.That(replay.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var replayResult = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        Assert.That(replayResult.RootElement.GetProperty("receipt").GetRawText(),
            Is.EqualTo(started.RootElement.GetProperty("receipt").GetRawText()));

        clock.Now = Origin.AddHours(2);
        using var matured = JsonDocument.Parse(await (await client.GetAsync(route))
            .Content.ReadAsStringAsync());
        Assert.That(matured.RootElement.GetProperty("availableWorkers").GetInt64(),
            Is.EqualTo(20));
        using var reread = JsonDocument.Parse(await (await client.GetAsync(route))
            .Content.ReadAsStringAsync());
        Assert.That(reread.RootElement.GetProperty("availableWorkers").GetInt64(),
            Is.EqualTo(20));

        PlayerHiveState stored = (await factory.Services
            .GetRequiredService<IHiveStateRepository>().ReadAsync(player, hive))!;
        Assert.That(stored.Resources["honey"].Amount, Is.EqualTo(4950));
        Assert.That(stored.Resources["pollen"].Amount, Is.EqualTo(990));
        Assert.That(stored.NurseryCohorts!.RecruitsInReserve, Is.EqualTo(17));
        Assert.That(stored.CivilPopulation!.Active, Is.Null);
    }

    [Test]
    public async Task Foreign_player_cannot_use_another_hives_civil_population()
    {
        await using var factory = Factory(true);
        using var owner = factory.CreateClient();
        using var visitor = factory.CreateClient();
        string ownerToken = await Login(factory, owner);
        string visitorToken = await Login(factory, visitor);
        Guid ownerId = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(ownerToken).PlayerId!.Value;
        Guid hive = Guid.NewGuid();
        await Seed(factory, ownerId, hive);

        visitor.DefaultRequestHeaders.Authorization = new("Bearer", visitorToken);
        string route = $"/game/v1/hives/{hive:D}/civil-population";
        Assert.That((await visitor.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await visitor.PostAsJsonAsync(route + "/cohorts/start",
            new { expectedRevision = 0, idempotencyKey = "foreign-civil-001" })).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        PlayerHiveState stored = (await factory.Services
            .GetRequiredService<IHiveStateRepository>().ReadAsync(ownerId, hive))!;
        Assert.That(stored.CivilPopulation, Is.Null);
    }

    [Test]
    public async Task Two_http_commands_cannot_create_duplicate_cohort()
    {
        await using var factory = Factory(true, new MutableClock(Origin));
        using var client = factory.CreateClient();
        string token = await Login(factory, client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Guid player = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;
        Guid hive = Guid.NewGuid();
        await Seed(factory, player, hive);
        string route = $"/game/v1/hives/{hive:D}/civil-population/cohorts/start";

        HttpResponseMessage[] results = await Task.WhenAll(
            client.PostAsJsonAsync(route, new { expectedRevision = 0, idempotencyKey = "race-civil-aaaa" }),
            client.PostAsJsonAsync(route, new { expectedRevision = 0, idempotencyKey = "race-civil-bbbb" }));
        using var first = results[0];
        using var second = results[1];
        Assert.That(results.Count(r => r.StatusCode == HttpStatusCode.OK), Is.EqualTo(1));
        Assert.That(results.Count(r => r.StatusCode == HttpStatusCode.Conflict), Is.EqualTo(1));
        PlayerHiveState state = (await factory.Services
            .GetRequiredService<IHiveStateRepository>().ReadAsync(player, hive))!;
        Assert.That(state.Resources["honey"].Amount, Is.EqualTo(4950));
        Assert.That(state.CivilPopulation!.Active, Is.Not.Null);
    }

    private static async Task Seed(WebApplicationFactory<Program> factory, Guid player, Guid hive)
    {
        await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ExecuteAtomicallyAsync(player, hive, state => state with
            {
                BuildingLevels = new Dictionary<string, int> { ["nursery_cluster"] = 1 },
                Resources = new Dictionary<string, ResourceBalance>
                {
                    ["honey"] = new(5000, 10000),
                    ["pollen"] = new(1000, 10000),
                    ["wax"] = new(500, 1000)
                },
                NurseryCohorts = new NurseryCohortState(0, 17, null,
                    new Dictionary<string, IdempotencyReceipt>())
            });
    }

    private static WebApplicationFactory<Program> Factory(bool enabled,
        MutableClock? clock = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CivilPopulation:Enabled"] = enabled.ToString(),
                    ["HiveDailyRound:Enabled"] = "false"
                }));
            if (clock != null)
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IServerClock>();
                    services.AddSingleton<IServerClock>(clock);
                });
        });

    private static async Task<string> Login(
        WebApplicationFactory<Program> factory, HttpClient client)
    {
        string email = "civil-" + Guid.NewGuid().ToString("N") + "@bee.test";
        factory.Services.GetRequiredService<IAccountCredentialStore>()
            .CreateEmailAccount(email, "secret");
        using HttpResponseMessage response = await client.PostAsJsonAsync("/auth/login",
            new { email, password = "secret", clientVersion = "1",
                ipAddress = "127.0.0.1", deviceIdentifier = "civil", region = "local" });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("tokens").GetProperty("accessToken")
            .GetString()!;
    }

    private sealed class MutableClock(DateTimeOffset start) : IServerClock
    {
        public DateTimeOffset Now { get; set; } = start;
        public DateTimeOffset UtcNow => Now;
    }
}

