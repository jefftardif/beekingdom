using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Authentication;
using BeeKingdom.HiveOperations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BeeKingdom.Tests;

// Full HTTP/DI/authentication/persistence proof on TestServer.
// All accounts and hives are generated for tests; no production database.
public sealed class AcademyTrainingEndpointTests
{
    private static readonly DateTimeOffset Origin =
        new(2026,10,19,14,0,0,TimeSpan.Zero);

    private sealed class Clock(DateTimeOffset initial) : IServerClock
    {
        public DateTimeOffset Now { get; set; } = initial;
        public DateTimeOffset UtcNow => Now;
    }

    private static WebApplicationFactory<Program> Factory(
        bool enabled, Clock? clock = null)
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment","Development");
            builder.ConfigureAppConfiguration((_,configuration)=>
                configuration.AddInMemoryCollection(new Dictionary<string,string?>
                {
                    ["AcademyTraining:Enabled"]=enabled.ToString().ToLowerInvariant(),
                    ["HiveDailyRound:Enabled"]="false"
                }));
            if (clock is not null)
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IServerClock>();
                    services.AddSingleton<IServerClock>(clock);
                });
        });

    private static async Task<string> Login(
        WebApplicationFactory<Program> factory,HttpClient client)
    {
        string email="academy-http-"+Guid.NewGuid().ToString("N")+"@bee.test";
        factory.Services.GetRequiredService<IAccountCredentialStore>()
            .CreateEmailAccount(email,"secret");
        using var response=await client.PostAsJsonAsync("/auth/login",
            new {email,password="secret",clientVersion="1.0",
                ipAddress="127.0.0.1",deviceIdentifier="academy",region="local"});
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("tokens")
            .GetProperty("accessToken").GetString()!;
    }

    private static async Task<(Guid player,Guid hive)> Provision(
        WebApplicationFactory<Program> factory,string bearer,
        int academyLevel=2)
    {
        Guid player=factory.Services.GetRequiredService<
            BeeKingdom.Authentication.AuthenticationManager>()
            .ValidateToken(bearer).PlayerId!.Value;
        Guid hive=Guid.NewGuid();
        await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ExecuteAtomicallyAsync(player,hive,state=>state with
            {
                BuildingLevels=new Dictionary<string,int>
                {
                    ["academy_canopy"]=academyLevel
                },
                Resources=new Dictionary<string,ResourceBalance>
                {
                    ["honey"]=new(10000,20000),
                    ["wax"]=new(5000,10000),
                    ["pollen"]=new(5000,10000)
                }
            });
        return (player,hive);
    }

    [Test]
    public async Task Disabled_flags_return_503_without_auth_or_creating_a_hive()
    {
        await using var factory=Factory(false);
        using var client=factory.CreateClient();
        Guid hive=Guid.NewGuid();
        string endpoint=$"/game/v1/hives/{hive:D}/academy";
        Assert.That((await client.GetAsync(endpoint)).StatusCode,
            Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await client.PostAsJsonAsync(endpoint+"/tactics/start",
            new { expectedRevision=0,idempotencyKey="acad-disabled-start" }))
            .StatusCode,Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        Assert.That((await client.PostAsJsonAsync(endpoint+"/"+
            Guid.NewGuid().ToString("D")+"/complete",
            new { expectedRevision=0,idempotencyKey="acad-disabled-end" }))
            .StatusCode,Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task Enabled_flags_require_a_valid_bearer_on_all_academy_routes()
    {
        await using var factory=Factory(true);
        using var client=factory.CreateClient();
        Guid hive=Guid.NewGuid();
        string path=$"/game/v1/hives/{hive:D}/academy";
        Assert.That((await client.GetAsync(path)).StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await client.PostAsJsonAsync(path+"/tactics/start",
            new { expectedRevision=0,idempotencyKey="acad-no-auth-start" }))
            .StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await client.PostAsJsonAsync(path+"/"+
            Guid.NewGuid().ToString("D")+"/complete",
            new { expectedRevision=0,idempotencyKey="acad-no-auth-end" }))
            .StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Full_authenticated_course_start_replay_timer_and_certification()
    {
        var clock=new Clock(Origin);
        await using var factory=Factory(true,clock);
        using var client=factory.CreateClient();
        string bearer=await Login(factory,client);
        client.DefaultRequestHeaders.Authorization=new("Bearer",bearer);
        (Guid player,Guid hive)=await Provision(factory,bearer);
        string path=$"/game/v1/hives/{hive:D}/academy";

        var initial=await client.GetAsync(path);
        Assert.That(initial.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        using (var json=JsonDocument.Parse(await initial.Content.ReadAsStringAsync()))
        {
            JsonElement snapshot=json.RootElement;
            Assert.That(snapshot.GetProperty("playerId").GetGuid(),Is.EqualTo(player));
            Assert.That(snapshot.GetProperty("hiveId").GetGuid(),Is.EqualTo(hive));
            Assert.That(snapshot.GetProperty("completed").GetArrayLength(),Is.Zero);
            Assert.That(snapshot.GetProperty("academyBuildingLevel").GetInt32(),
                Is.EqualTo(2));
            Assert.That(snapshot.GetProperty("courses").GetArrayLength(),Is.EqualTo(4));
            Assert.That(snapshot.GetProperty("courses")[0].GetProperty("costs")
                .GetProperty("honey").GetInt64(),Is.EqualTo(240L));
        }

        var body=new{expectedRevision=0,idempotencyKey="acad-web-start-001"};
        var first=await client.PostAsJsonAsync(path+"/tactics/start",body);
        Assert.That(first.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        using var firstJson=JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        Assert.That(firstJson.RootElement.GetProperty("succeeded").GetBoolean(),Is.True);
        JsonElement started=firstJson.RootElement.GetProperty("snapshot");
        Guid operation=started.GetProperty("activeOperation")
            .GetProperty("operationId").GetGuid();
        Assert.That(started.GetProperty("balances").GetProperty("honey")
            .GetProperty("amount").GetInt64(),Is.EqualTo(9760L));
        Assert.That(started.GetProperty("balances").GetProperty("pollen")
            .GetProperty("amount").GetInt64(),Is.EqualTo(4925L));

        var replay=await client.PostAsJsonAsync(path+"/tactics/start",body);
        Assert.That(replay.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        using(var json=JsonDocument.Parse(await replay.Content.ReadAsStringAsync()))
            Assert.That(json.RootElement.GetProperty("receipt").GetRawText(),
                Is.EqualTo(firstJson.RootElement.GetProperty("receipt").GetRawText()));

        var before=await client.PostAsJsonAsync(path+"/"+operation.ToString("D")+
            "/complete",new{expectedRevision=1,idempotencyKey="acad-web-end-001"});
        Assert.That(before.StatusCode,Is.EqualTo(HttpStatusCode.Conflict));
        clock.Now=Origin.AddMinutes(3);
        using(var queue=JsonDocument.Parse(await (await client.GetAsync(path))
            .Content.ReadAsStringAsync()))
            Assert.That(queue.RootElement.GetProperty("activeOperation")
                .GetProperty("status").GetString(),
                Is.EqualTo("awaiting_completion"));

        var finished=await client.PostAsJsonAsync(
            path+"/"+operation.ToString("D")+"/complete",
            new{expectedRevision=1,idempotencyKey="acad-web-end-001"});
        Assert.That(finished.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        using var doc=JsonDocument.Parse(await finished.Content.ReadAsStringAsync());
        JsonElement final=doc.RootElement.GetProperty("snapshot");
        Assert.That(final.GetProperty("revision").GetInt64(),Is.EqualTo(2));
        Assert.That(final.GetProperty("earnedCredits").GetInt32(),Is.EqualTo(1));
        Assert.That(final.GetProperty("completed")[0].GetProperty("courseId")
            .GetString(),Is.EqualTo("tactics"));
        Assert.That(final.GetProperty("activeOperation").ValueKind,
            Is.EqualTo(JsonValueKind.Null));

        using var second=JsonDocument.Parse(await (await client.PostAsJsonAsync(
            path+"/"+operation.ToString("D")+"/complete",
            new{expectedRevision=1,idempotencyKey="acad-web-end-001"}))
                .Content.ReadAsStringAsync());
        Assert.That(second.RootElement.GetProperty("snapshot")
            .GetProperty("earnedCredits").GetInt32(),Is.EqualTo(1));
        var saved=(await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ReadAsync(player,hive))!;
        Assert.That(saved.Resources["honey"].Amount,Is.EqualTo(9760L));
        Assert.That(saved.Academy!.Completed.Count,Is.EqualTo(1));
    }

    [Test]
    public async Task Foreign_user_and_missing_hive_cannot_leak_or_create_an_academy()
    {
        await using var factory=Factory(true);
        using var owner=factory.CreateClient();
        string ownerToken=await Login(factory,owner);
        owner.DefaultRequestHeaders.Authorization=new("Bearer",ownerToken);
        (Guid player,Guid hive)=await Provision(factory,ownerToken);
        using var stranger=factory.CreateClient();
        stranger.DefaultRequestHeaders.Authorization=new("Bearer",
            await Login(factory,stranger));
        string path=$"/game/v1/hives/{hive:D}/academy";
        Assert.That((await stranger.GetAsync(path)).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await stranger.PostAsJsonAsync(path+"/tactics/start",
            new{expectedRevision=0,idempotencyKey="acad-foreign-start"}))
            .StatusCode,Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await stranger.PostAsJsonAsync(path+"/"+
            Guid.NewGuid().ToString("D")+"/complete",
            new{expectedRevision=0,idempotencyKey="acad-foreign-end"}))
            .StatusCode,Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await owner.GetAsync("/game/v1/hives/not-a-guid/academy"))
            .StatusCode,Is.EqualTo(HttpStatusCode.BadRequest));
        string absent=$"/game/v1/hives/{Guid.NewGuid():D}/academy";
        Assert.That((await owner.GetAsync(absent)).StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
        var saved=(await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ReadAsync(player,hive))!;
        Assert.That(saved.Revision,Is.Zero);
        Assert.That(saved.Academy,Is.Null);
    }

    [Test]
    public async Task Two_racing_http_starts_cannot_charge_twice()
    {
        await using var factory=Factory(true,new Clock(Origin));
        using var client=factory.CreateClient();
        string bearer=await Login(factory,client);
        client.DefaultRequestHeaders.Authorization=new("Bearer",bearer);
        (Guid player,Guid hive)=await Provision(factory,bearer);
        string path=$"/game/v1/hives/{hive:D}/academy/tactics/start";
        var one=client.PostAsJsonAsync(path,
            new{expectedRevision=0,idempotencyKey="acad-race-first"});
        var two=client.PostAsJsonAsync(path,
            new{expectedRevision=0,idempotencyKey="acad-race-other"});
        HttpResponseMessage[] responses=await Task.WhenAll(one,two);
        Assert.That(responses.Count(r=>r.StatusCode==HttpStatusCode.OK),Is.EqualTo(1));
        Assert.That(responses.Count(r=>r.StatusCode==HttpStatusCode.Conflict),Is.EqualTo(1));
        var saved=(await factory.Services.GetRequiredService<IHiveStateRepository>()
            .ReadAsync(player,hive))!;
        Assert.That(saved.Revision,Is.EqualTo(1));
        Assert.That(saved.Resources["honey"].Amount,Is.EqualTo(9760L));
        Assert.That(saved.Resources["pollen"].Amount,Is.EqualTo(4925L));
        Assert.That(saved.Academy!.ActiveOperation,Is.Not.Null);
        foreach(var response in responses)response.Dispose();
    }
}
