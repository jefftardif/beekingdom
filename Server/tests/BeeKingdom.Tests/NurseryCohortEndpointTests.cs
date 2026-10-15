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

public sealed class NurseryCohortEndpointTests
{
    [Test]
    public async Task Disabled_by_default_get_and_start_return_503_without_mutating_state()
    {
        await using var factory = Factory(false);
        using var client = factory.CreateClient();
        string route = $"/game/v1/hives/{Guid.NewGuid():D}/brood/cohorts";
        Assert.That((await client.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await client.PostAsJsonAsync(route + "/start",
            new { expectedRevision = 0, idempotencyKey = "disabled" })).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Enabled_endpoints_require_authentication()
    {
        await using var factory = Factory(true);
        using var client = factory.CreateClient();
        string route = $"/game/v1/hives/{Guid.NewGuid():D}/brood/cohorts";
        Assert.That((await client.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await client.PostAsJsonAsync(route + "/start",
            new { expectedRevision = 0, idempotencyKey = "unauth" })).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Authenticated_cohort_reads_real_maturation_and_exact_idempotent_replay()
    {
        DateTimeOffset now = new(2026, 10, 14, 19, 0, 0, TimeSpan.Zero);
        var clock = new MutableClock(now);
        await using var factory = Factory(true,clock);
        using var client = factory.CreateClient();

        string token = await Login(factory,client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Guid player = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;
        Guid hive = Guid.NewGuid();
        await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ExecuteAtomicallyAsync(player,hive,s=>s with
            {
                BuildingLevels = new Dictionary<string,int> { ["nursery_cluster"] = 1 },
                BroodVitality = new(65,65,0,now,null)
            });
        string route = $"/game/v1/hives/{hive:D}/brood/cohorts";
        using(var initial=JsonDocument.Parse(await (
            await client.GetAsync(route)).Content.ReadAsStringAsync()))
        {
            Assert.That(initial.RootElement.GetProperty("canStart").GetBoolean(),
                Is.True);
            Assert.That(initial.RootElement.GetProperty("recruitsInReserve").GetInt64(),
                Is.Zero);
        }

        var start = await client.PostAsJsonAsync(route+"/start",
            new { expectedRevision = 0,idempotencyKey = "first-cohort" });
        Assert.That(start.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        using var started=JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        Assert.That(started.RootElement.GetProperty("succeeded").GetBoolean(),Is.True);
        JsonElement firstReceipt = started.RootElement.GetProperty("receipt");
        Guid cohort=firstReceipt.GetProperty("cohortId").GetGuid();
        Assert.That(cohort,Is.Not.EqualTo(Guid.Empty));
        Assert.That(started.RootElement.GetProperty("snapshot").GetProperty("active")
            .GetProperty("expectedRecruits").GetInt32(),Is.EqualTo(30));

        var replay = await client.PostAsJsonAsync(route+"/start",
            new { expectedRevision=0,idempotencyKey="first-cohort" });
        Assert.That(replay.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var replayed=JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        Assert.That(replayed.RootElement.GetProperty("receipt").GetRawText(),
            Is.EqualTo(firstReceipt.GetRawText()));

        clock.Now=now.AddHours(1);
        var finished=await client.GetAsync(route);
        Assert.That(finished.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        using var matured=JsonDocument.Parse(await finished.Content.ReadAsStringAsync());
        Assert.That(matured.RootElement.GetProperty("active").ValueKind,
            Is.EqualTo(JsonValueKind.Null));
        Assert.That(matured.RootElement.GetProperty("recruitsInReserve").GetInt64(),
            Is.EqualTo(30));
        using var again=JsonDocument.Parse(await (
            await client.GetAsync(route)).Content.ReadAsStringAsync());
        Assert.That(again.RootElement.GetProperty("recruitsInReserve").GetInt64(),
            Is.EqualTo(30),"Repeated authenticated GET cannot mint extra recruits");
    }

    [Test]
    public async Task Authenticated_missing_hive_returns_404_and_never_creates_a_phantom_hive()
    {
        await using var factory = Factory(true);
        using var client = factory.CreateClient();
        string token = await Login(factory, client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Guid player = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;
        Guid hive = Guid.NewGuid();
        string url = $"/game/v1/hives/{hive:D}/brood/cohorts";
        Assert.That((await client.GetAsync(url)).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await client.PostAsJsonAsync(url + "/start",
            new { expectedRevision=0,idempotencyKey="dont-create" })).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ReadAsync(player,hive),Is.Null);
    }

    [Test]
    public async Task Foreign_player_cannot_read_or_start_cohorts_in_another_players_hive()
    {
        await using var factory = Factory(true);
        using var owner = factory.CreateClient();
        using var foreign = factory.CreateClient();
        string ownerToken = await Login(factory, owner);
        string foreignToken = await Login(factory, foreign);
        Guid ownerId = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(ownerToken).PlayerId!.Value;
        Guid foreignId = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(foreignToken).PlayerId!.Value;
        Assert.That(foreignId,Is.Not.EqualTo(ownerId));
        Guid hive = Guid.NewGuid();
        DateTimeOffset now = new(2026,10,14,20,0,0,TimeSpan.Zero);
        await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ExecuteAtomicallyAsync(ownerId,hive,s=>s with
            {
                BuildingLevels=new Dictionary<string,int>{{"nursery_cluster",1}},
                BroodVitality=new(75,80,0,now,null)
            });
        foreign.DefaultRequestHeaders.Authorization=new("Bearer",foreignToken);
        string url=$"/game/v1/hives/{hive:D}/brood/cohorts";
        Assert.That((await foreign.GetAsync(url)).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await foreign.PostAsJsonAsync(url+"/start",
            new{expectedRevision=0,idempotencyKey="foreign-command"})).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ReadAsync(ownerId,hive))!.NurseryCohorts,Is.Null);
        Assert.That(await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ReadAsync(foreignId,hive),Is.Null);
    }

    [Test]
    public async Task Malformed_hive_id_is_400_even_for_authenticated_player()
    {
        await using var factory=Factory(true);
        using var client=factory.CreateClient();
        client.DefaultRequestHeaders.Authorization=new("Bearer",
            await Login(factory,client));
        string url="/game/v1/hives/not-a-hive-id/brood/cohorts";
        Assert.That((await client.GetAsync(url)).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await client.PostAsJsonAsync(url+"/start",
            new {expectedRevision=0,idempotencyKey="invalid-hive"})).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Nonexistent_hive_does_not_get_created_from_cohort_url()
    {
        await using var factory = Factory(true);
        using var client = factory.CreateClient();
        string token = await Login(factory, client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Guid player = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;
        Guid unknownHive = Guid.NewGuid();
        string route = $"/game/v1/hives/{unknownHive:D}/brood/cohorts";
        Assert.That((await client.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await client.PostAsJsonAsync(route + "/start",
            new { expectedRevision = 0, idempotencyKey = "no-phantom-hive" })).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ReadAsync(player, unknownHive), Is.Null,
            "The nursery API must never create a player hive on failed start.");
    }

    [Test]
    public async Task Other_players_cannot_read_or_start_cohorts_in_an_owners_hive()
    {
        await using var factory = Factory(true);
        using var owner = factory.CreateClient();
        string ownerToken = await Login(factory, owner);
        owner.DefaultRequestHeaders.Authorization = new("Bearer", ownerToken);
        Guid ownerId = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(ownerToken).PlayerId!.Value;
        Guid hive = Guid.NewGuid();
        DateTimeOffset now = new(2026, 10, 14, 19, 0, 0, TimeSpan.Zero);
        await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ExecuteAtomicallyAsync(ownerId, hive, state => state with
            {
                BuildingLevels = new Dictionary<string, int> { ["nursery_cluster"] = 1 },
                BroodVitality = new(65, 65, 0, now, null)
            });

        using var visitor = factory.CreateClient();
        string foreignToken = await Login(factory, visitor);
        visitor.DefaultRequestHeaders.Authorization = new("Bearer", foreignToken);
        string route = $"/game/v1/hives/{hive:D}/brood/cohorts";
        Assert.That((await visitor.GetAsync(route)).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await visitor.PostAsJsonAsync(route + "/start",
            new { expectedRevision = 0, idempotencyKey = "foreign" })).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));

        PlayerHiveState stored = (await factory.Services
            .GetRequiredService<IHiveStateRepository>()
            .ReadAsync(ownerId, hive))!;
        Assert.That(stored.NurseryCohorts, Is.Null,
            "Foreign player requests must not change the owner's hive.");
        Assert.That(stored.Revision, Is.Zero);
        Assert.That((await owner.GetAsync(route)).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Bad_hive_identifiers_and_bad_command_keys_are_rejected()
    {
        await using var factory = Factory(true);
        using var client = factory.CreateClient();
        string token = await Login(factory, client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        string invalid = "/game/v1/hives/not-a-guid/brood/cohorts";
        Assert.That((await client.GetAsync(invalid)).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await client.PostAsJsonAsync(invalid + "/start",
            new { expectedRevision = 0, idempotencyKey = "valid" })).StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));

        Guid player = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;
        Guid hive = Guid.NewGuid();
        await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ExecuteAtomicallyAsync(player, hive, state => state with
            {
                BuildingLevels = new Dictionary<string, int> { ["nursery_cluster"] = 1 },
                BroodVitality = new(65, 65, 0,
                    new DateTimeOffset(2026, 10, 14, 19, 0, 0, TimeSpan.Zero), null)
            });
        string route = $"/game/v1/hives/{hive:D}/brood/cohorts/start";
        Assert.That((await client.PostAsJsonAsync(route,
            new { expectedRevision = 0, idempotencyKey = "spaces are invalid" }))
            .StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await client.PostAsJsonAsync(route,
            new { expectedRevision = -1, idempotencyKey = "negative" }))
            .StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var stored = await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ReadAsync(player, hive);
        Assert.That(stored!.NurseryCohorts, Is.Null);
        Assert.That(stored.Revision, Is.Zero);
    }

    [Test]
    public async Task Racing_http_starts_cannot_create_more_than_one_cohort()
    {
        DateTimeOffset now = new(2026, 10, 14, 19, 0, 0, TimeSpan.Zero);
        await using var factory = Factory(true, new MutableClock(now));
        using var client = factory.CreateClient();
        string token = await Login(factory, client);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Guid player = factory.Services
            .GetRequiredService<BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(token).PlayerId!.Value;
        Guid hive = Guid.NewGuid();
        await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ExecuteAtomicallyAsync(player, hive, state => state with
            {
                BuildingLevels = new Dictionary<string, int> { ["nursery_cluster"] = 1 },
                BroodVitality = new(65, 65, 0, now, null)
            });
        string route = $"/game/v1/hives/{hive:D}/brood/cohorts/start";
        var tasks = new[]
        {
            client.PostAsJsonAsync(route,
                new { expectedRevision = 0, idempotencyKey = "race-one" }),
            client.PostAsJsonAsync(route,
                new { expectedRevision = 0, idempotencyKey = "race-two" })
        };
        using var responseA = await tasks[0];
        using var responseB = await tasks[1];
        var statuses = new[] { responseA.StatusCode, responseB.StatusCode };
        Assert.That(statuses.Count(s => s == HttpStatusCode.OK), Is.EqualTo(1));
        Assert.That(statuses.Count(s => s == HttpStatusCode.Conflict), Is.EqualTo(1));
        PlayerHiveState hiveState = (await factory.Services
            .GetRequiredService<IHiveStateRepository>()
            .ReadAsync(player, hive))!;
        Assert.That(hiveState.NurseryCohorts!.ActiveCohort, Is.Not.Null);
        Assert.That(hiveState.NurseryCohorts.RecruitsInReserve, Is.Zero);
        Assert.That(hiveState.Revision, Is.EqualTo(1));
    }
    private static WebApplicationFactory<Program> Factory(
        bool enabled, MutableClock? clock = null)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment","Development");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string,string?>
                {
                    ["NurseryCohorts:Enabled"]=enabled.ToString(),
                    ["HiveDailyRound:Enabled"]="false"
                }));
            if(clock!=null)
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IServerClock>();
                    services.AddSingleton<IServerClock>(clock);
                });
        });
    }

    private static async Task<string> Login(
        WebApplicationFactory<Program> factory,HttpClient client)
    {
        string email="nursery-"+Guid.NewGuid().ToString("N")+"@bee.test";
        factory.Services.GetRequiredService<IAccountCredentialStore>()
            .CreateEmailAccount(email,"secret");
        var response=await client.PostAsJsonAsync("/auth/login",
            new {email,password="secret",clientVersion="1",
                ipAddress="127.0.0.1",deviceIdentifier="nursery",region="local"});
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("tokens")
            .GetProperty("accessToken").GetString()!;
    }

    private sealed class MutableClock(DateTimeOffset start):IServerClock
    {
        public DateTimeOffset Now { get; set; }=start;
        public DateTimeOffset UtcNow => Now;
    }
}
