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

public sealed class CombatPatrolEndpointTests
{
    private static readonly System.Text.Json.JsonSerializerOptions ReadOptions = new(System.Text.Json.JsonSerializerDefaults.Web) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    [Test]
    public async Task Default_closed_flag_returns_game_unavailable_for_all_routes()
    {
        await using var factory = CreateFactory(false); using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }); var hive = Guid.NewGuid();
        foreach (var request in new Func<Task<HttpResponseMessage>>[] {
            () => client.GetAsync($"/game/v1/hives/{hive:D}/combat/patrol"),
            () => client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/1/preview", new { guardians = 0, wingrunners = 0, darters = 0 }),
            () => client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/launch", new { tier = 1, guardians = 0, wingrunners = 0, darters = 0, expectedRevision = 0, idempotencyKey = "k" }),
            () => client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/{Guid.NewGuid():D}/claim", new { expectedRevision = 0, idempotencyKey = "k" }),
            () => client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/{Guid.NewGuid():D}/recall", new { expectedRevision = 0, idempotencyKey = "k" }),
            () => client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/slots/purchase-resource", new { expectedRevision = 0, idempotencyKey = "k" }),
            () => client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/slots/grant-premium", new { expectedRevision = 0, idempotencyKey = "k" }) })
        { using var response = await request(); Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable)); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo("game.unavailable")); }
    }

    [Test]
    public async Task Enabled_contract_rejects_missing_auth_and_invalid_requests()
    {
        await using var factory = CreateFactory(true); using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }); var hive = Guid.NewGuid();
        var unauthorized = await client.GetAsync($"/game/v1/hives/{hive:D}/combat/patrol"); Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        var token = await LoginTestAccount(factory, client, $"patrol-{Guid.NewGuid():N}@bee.test"); client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var invalid = await client.PostAsJsonAsync("/game/v1/hives/not-guid/combat/patrol/launch", new { tier = 1, guardians = -1, wingrunners = 0, darters = 0, expectedRevision = -1, idempotencyKey = "" }); Assert.That(invalid.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public void Both_settings_keep_feature_closed()
    {
        using var factory = CreateFactory(false); var root = factory.Services.GetRequiredService<IHostEnvironment>().ContentRootPath;
        foreach (var file in new[] { Path.Combine(root, "appsettings.json"), Path.Combine(root, "appsettings.Production.json") })
        { using var doc = JsonDocument.Parse(File.ReadAllText(file)); Assert.That(doc.RootElement.GetProperty("CombatPatrol").GetProperty("Enabled").GetBoolean(), Is.False); }
    }

    [Test]
    public async Task Enabled_launch_is_rejected_when_underpowered_with_zero_mutation()
    {
        var root = Path.Combine(Path.GetTempPath(), "patrol-http-" + Guid.NewGuid().ToString("N")); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        await using var factory = CreateFactory(true, root, clock, guardians: 1, wingrunners: 0, darters: 0);
        using var client = factory.CreateClient();
        try
        {
            var hive = Guid.NewGuid(); var token = await LoginTestAccount(factory, client, $"weak-{Guid.NewGuid():N}@bee.test"); client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            var player = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value;
            await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player, hive, s => s);

            var preview = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/3/preview", new { guardians = 1, wingrunners = 0, darters = 0 });
            var previewed = await preview.Content.ReadFromJsonAsync<CombatPatrolPreview>();
            Assert.That(previewed!.CanLaunch, Is.False);
            Assert.That(previewed.BlockReason, Is.EqualTo("game.patrol_underpowered"));

            var launch = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/launch", new { tier = 3, guardians = 1, wingrunners = 0, darters = 0, expectedRevision = 0, idempotencyKey = "launch" });
            Assert.That(launch.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            using var json = JsonDocument.Parse(await launch.Content.ReadAsStringAsync());
            Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo("game.patrol_underpowered"));

            var state = await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(player, hive);
            Assert.That(state!.CombatPatrol, Is.Null);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Enabled_launch_claim_applies_real_losses_credits_partial_reward_and_replays()
    {
        var root = Path.Combine(Path.GetTempPath(), "patrol-http-" + Guid.NewGuid().ToString("N")); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        // Tier 2 (guardians hazard, required=90): darters are doctrinally disadvantaged against it -> HardWon with real losses.
        await using var factory = CreateFactory(true, root, clock, guardians: 0, wingrunners: 0, darters: 18, guardPostLevel: 2);
        using var client = factory.CreateClient();
        try
        {
            var hive = Guid.NewGuid(); var token = await LoginTestAccount(factory, client, $"launch-{Guid.NewGuid():N}@bee.test"); client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            var player = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value;
            await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player, hive, s => s);

            var launch = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/launch", new { tier = 2, guardians = 0, wingrunners = 0, darters = 18, expectedRevision = 0, idempotencyKey = "launch" });
            Assert.That(launch.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var launched = await launch.Content.ReadFromJsonAsync<CombatPatrolMutationResponse>(ReadOptions);
            Assert.That(launched!.Snapshot.ActiveEncounters, Has.Count.EqualTo(1));
            var encounterId = launched.Snapshot.ActiveEncounters[0].EncounterId;

            clock.Advance(CombatPatrolCatalog.Tiers[2].Duration);
            var claim = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/{encounterId:D}/claim", new { expectedRevision = launched.Snapshot.Revision, idempotencyKey = "claim" });
            Assert.That(claim.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var claimed = await claim.Content.ReadFromJsonAsync<CombatPatrolMutationResponse>(ReadOptions);
            Assert.That(claimed!.ClaimReceipt, Is.Not.Null);
            long permanent = claimed.ClaimReceipt!.PermanentLosses["darters"];
            long wounded = claimed.ClaimReceipt.WoundedLosses["darters"];
            Assert.That(wounded, Is.GreaterThan(0));
            Assert.That(claimed.ClaimReceipt.CreditedByResource["honey"], Is.GreaterThan(0));
            Assert.That(claimed.Snapshot.ActiveEncounters, Is.Empty);
            Assert.That(claimed.Snapshot.Recovering, Has.Count.EqualTo(1));

            var state = await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(player, hive);
            Assert.That(state!.DoctrineRoster!.Counts["darters"], Is.EqualTo(18 - permanent - wounded));

            var replay = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/{encounterId:D}/claim", new { expectedRevision = launched.Snapshot.Revision, idempotencyKey = "claim" });
            Assert.That(replay.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var replayed = await replay.Content.ReadFromJsonAsync<CombatPatrolMutationResponse>(ReadOptions);
            Assert.That(replayed!.ClaimReceipt!.WoundedLosses["darters"], Is.EqualTo(wounded));

            // The recovery window elapsing brings the wounded bees back into the roster on next read.
            clock.Advance(CombatPatrolResolution.ComputeRecoveryDuration(CombatPatrolCatalog.Tiers[2]));
            var recoveredSnapshot = await client.GetFromJsonAsync<CombatPatrolSnapshot>($"/game/v1/hives/{hive:D}/combat/patrol", ReadOptions);
            Assert.That(recoveredSnapshot!.Recovering, Is.Empty);
            var healedState = await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(player, hive);
            Assert.That(healedState!.DoctrineRoster!.Counts["darters"], Is.EqualTo(18 - permanent));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Enabled_recall_returns_squad_without_loss_reward_and_squad_release_is_blocked_while_active()
    {
        var root = Path.Combine(Path.GetTempPath(), "patrol-http-" + Guid.NewGuid().ToString("N")); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        await using var factory = CreateFactory(true, root, clock, guardians: 18, wingrunners: 0, darters: 0, guardPostLevel: 2);
        using var client = factory.CreateClient();
        try
        {
            var hive = Guid.NewGuid(); var token = await LoginTestAccount(factory, client, $"recall-{Guid.NewGuid():N}@bee.test"); client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            var player = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value;
            await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player, hive, s => s);

            var launch = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/launch", new { tier = 2, guardians = 18, wingrunners = 0, darters = 0, expectedRevision = 0, idempotencyKey = "launch" });
            var launched = await launch.Content.ReadFromJsonAsync<CombatPatrolMutationResponse>(ReadOptions);
            var encounterId = launched!.Snapshot.ActiveEncounters[0].EncounterId;

            var release = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/squad-reservation/release", new { expectedRevision = 0, idempotencyKey = "release-during-patrol" });
            Assert.That(release.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            using (var json = JsonDocument.Parse(await release.Content.ReadAsStringAsync())) Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo("game.squad_in_use"));

            var recall = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/{encounterId:D}/recall", new { expectedRevision = launched.Snapshot.Revision, idempotencyKey = "recall" });
            Assert.That(recall.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var recalled = await recall.Content.ReadFromJsonAsync<CombatPatrolMutationResponse>(ReadOptions);
            Assert.That(recalled!.ClaimReceipt, Is.Null);
            Assert.That(recalled.Snapshot.TierCooldownEndsAtUtc, Is.Empty);

            var state = await factory.Services.GetRequiredService<IHiveStateRepository>().ReadAsync(player, hive);
            Assert.That(state!.DoctrineRoster!.Counts["guardians"], Is.EqualTo(18));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Enabled_second_concurrent_launch_needs_a_purchased_slot()
    {
        var root = Path.Combine(Path.GetTempPath(), "patrol-http-" + Guid.NewGuid().ToString("N")); var clock = new MutableClock(new(2026, 7, 25, 9, 0, 0, TimeSpan.Zero));
        await using var factory = CreateFactory(true, root, clock, guardians: 0, wingrunners: 40, darters: 0, guardPostLevel: 4, honey: 10_000, pollen: 10_000);
        using var client = factory.CreateClient();
        try
        {
            var hive = Guid.NewGuid(); var token = await LoginTestAccount(factory, client, $"slots-{Guid.NewGuid():N}@bee.test"); client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            var player = factory.Services.GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>().ValidateToken(token).PlayerId!.Value;
            await factory.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(player, hive, s => s);

            var first = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/launch", new { tier = 1, guardians = 0, wingrunners = 20, darters = 0, expectedRevision = 0, idempotencyKey = "launch-a" });
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var firstLaunched = await first.Content.ReadFromJsonAsync<CombatPatrolMutationResponse>(ReadOptions);

            var blocked = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/launch", new { tier = 1, guardians = 0, wingrunners = 10, darters = 0, expectedRevision = firstLaunched!.Snapshot.Revision, idempotencyKey = "launch-b" });
            Assert.That(blocked.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            using (var json = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync())) Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo("game.patrol_no_slot_available"));

            var purchase = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/slots/purchase-resource", new { expectedRevision = firstLaunched.Snapshot.Revision, idempotencyKey = "buy-slot" });
            Assert.That(purchase.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var purchased = await purchase.Content.ReadFromJsonAsync<CombatPatrolMutationResponse>(ReadOptions);
            Assert.That(purchased!.Snapshot.TotalSlots, Is.EqualTo(2));

            var second = await client.PostAsJsonAsync($"/game/v1/hives/{hive:D}/combat/patrol/launch", new { tier = 1, guardians = 0, wingrunners = 10, darters = 0, expectedRevision = purchased.Snapshot.Revision, idempotencyKey = "launch-b2" });
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var secondLaunched = await second.Content.ReadFromJsonAsync<CombatPatrolMutationResponse>(ReadOptions);
            Assert.That(secondLaunched!.Snapshot.ActiveEncounters, Has.Count.EqualTo(2));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static async Task<string> LoginTestAccount(WebApplicationFactory<Program> factory, HttpClient client, string email)
    {
        factory.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email, "secret");
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password = "secret", clientVersion = "1.0.0", ipAddress = "127.0.0.1", deviceIdentifier = "patrol-tests", region = "local" });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return json.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;
    }

    private static WebApplicationFactory<Program> CreateFactory(bool enabled, string? root = null, MutableClock? clock = null, long guardians = 0, long wingrunners = 0, long darters = 0, int guardPostLevel = 0, long honey = 100_000, long pollen = 100_000) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("environment", "Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["CombatPatrol:Enabled"] = enabled.ToString(), ["CombatSquadReservation:Enabled"] = "true" }));
        builder.ConfigureTestServices(services =>
        {
            if (clock is not null) { services.RemoveAll<BeeKingdom.HiveOperations.IServerClock>(); services.AddSingleton<BeeKingdom.HiveOperations.IServerClock>(clock); }
            if (root is not null)
            {
                services.RemoveAll<IHiveStateRepository>();
                services.AddSingleton<IHiveStateRepository>(new DurableJsonHiveStateRepository(root, (p, h) => new PlayerHiveState(
                    p, h, HiveStateMigrator.CurrentModelVersion, 0,
                    new Dictionary<string, ResourceBalance> { ["honey"] = new(honey, 1_000_000), ["pollen"] = new(pollen, 1_000_000) },
                    new Dictionary<string, int> { ["guard_post"] = guardPostLevel }, [], new(),
                    DoctrineRoster: new DoctrineRosterState(0, new() { ["guardians"] = guardians, ["wingrunners"] = wingrunners, ["darters"] = darters }, null, new()))));
            }
        });
    });

    private sealed class MutableClock(DateTimeOffset now) : BeeKingdom.HiveOperations.IServerClock { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan value) => UtcNow += value; }
}
