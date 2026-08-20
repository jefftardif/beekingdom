using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Authentication;
using AuthenticationManager = BeeKingdom.Authentication.AuthenticationManager;
using BeeKingdom.HiveOperations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Tests;

public sealed class BuildingUpgradeEndpointTests
{
    [Test] public async Task ClosedFlagReturns503BeforeAuth(){await using var f=Create(false);using var c=f.CreateClient();var r=await c.GetAsync($"/game/v1/hives/{Guid.NewGuid()}/building-upgrades");Assert.That(r.StatusCode,Is.EqualTo(HttpStatusCode.ServiceUnavailable));}
    [Test] public async Task EnabledRequiresBearerAndExposesSnapshot(){await using var f=Create(true);using var c=f.CreateClient();var hive=Guid.NewGuid();var unauth=await c.GetAsync($"/game/v1/hives/{hive}/building-upgrades");Assert.That(unauth.StatusCode,Is.EqualTo(HttpStatusCode.Unauthorized));var email=$"upgrade-{Guid.NewGuid():N}@bee.test";f.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email,"secret");var login=await c.PostAsJsonAsync("/auth/login",new{email,password="secret",clientVersion="1.0"});using var doc=JsonDocument.Parse(await login.Content.ReadAsStringAsync());var token=doc.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);var p=f.Services.GetRequiredService<AuthenticationManager>().ValidateToken(token).PlayerId!.Value;await f.Services.GetRequiredService<IHiveStateRepository>().ExecuteAtomicallyAsync(p,hive,s=>s with{BuildingLevels=new Dictionary<string,int>{{"wax_workshop",1}},Resources=new Dictionary<string,ResourceBalance>{{"honey",new(100,200)},{"pollen",new(100,200)},{"wax",new(0,100)}}});var r=await c.GetAsync($"/game/v1/hives/{hive}/building-upgrades");Assert.That(r.StatusCode,Is.EqualTo(HttpStatusCode.OK));using var body=JsonDocument.Parse(await r.Content.ReadAsStringAsync());Assert.That(body.RootElement.GetProperty("contractVersion").GetString(),Is.EqualTo("living-hive-building-upgrade-v1"));Assert.That(body.RootElement.GetProperty("offers").GetArrayLength(),Is.EqualTo(1));}
    [Test] public async Task StartWithDailyRoundMarksOnceAndReplaysWithoutSecondDebit(){await using var f=Create(true,true);using var c=f.CreateClient();var hive=Guid.NewGuid();var email=$"upgrade-daily-{Guid.NewGuid():N}@bee.test";f.Services.GetRequiredService<IAccountCredentialStore>().CreateEmailAccount(email,"secret");var login=await c.PostAsJsonAsync("/auth/login",new{email,password="secret",clientVersion="1.0"});using var doc=JsonDocument.Parse(await login.Content.ReadAsStringAsync());var token=doc.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString()!;c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);var p=f.Services.GetRequiredService<AuthenticationManager>().ValidateToken(token).PlayerId!.Value;var repo=f.Services.GetRequiredService<IHiveStateRepository>();await repo.ExecuteAtomicallyAsync(p,hive,s=>s with{BuildingLevels=new Dictionary<string,int>{{"wax_workshop",1}},Resources=new Dictionary<string,ResourceBalance>{{"honey",new(100,200)},{"pollen",new(100,200)},{"wax",new(0,100)}}});var seed=await repo.ReadAsync(p,hive);var request=new{expectedRevision=seed!.Revision,idempotencyKey="upgrade-daily"};var first=await c.PostAsJsonAsync($"/game/v1/hives/{hive}/building-upgrades/wax_workshop/start",request);Assert.That(first.StatusCode,Is.EqualTo(HttpStatusCode.OK));var state=await repo.ReadAsync(p,hive);Assert.That(state!.DailyRound!.OperationLaunched,Is.True);Assert.That(state.Revision,Is.EqualTo(seed.Revision+1));Assert.That(state.Resources["honey"].Amount,Is.EqualTo(80));var replay=await c.PostAsJsonAsync($"/game/v1/hives/{hive}/building-upgrades/wax_workshop/start",request);Assert.That(replay.StatusCode,Is.EqualTo(HttpStatusCode.OK));var after=await repo.ReadAsync(p,hive);Assert.That(after!.Revision,Is.EqualTo(state.Revision));Assert.That(after.Resources["honey"].Amount,Is.EqualTo(80));Assert.That(after.DailyRound,Is.EqualTo(state.DailyRound));}
    private static WebApplicationFactory<Program> Create(bool enabled,bool daily=false){var d=new Dictionary<string,string?>{{"BuildingUpgrades:Enabled",enabled.ToString().ToLowerInvariant()},{"HiveDailyRound:Enabled",daily.ToString().ToLowerInvariant()}};if(enabled){d["BuildingUpgrades:CatalogVersion"]="phase-test-v1";d["BuildingUpgrades:Catalog:0:BuildingKey"]="wax_workshop";d["BuildingUpgrades:Catalog:0:FromLevel"]="1";d["BuildingUpgrades:Catalog:0:ToLevel"]="2";d["BuildingUpgrades:Catalog:0:Duration"]="01:00:00";d["BuildingUpgrades:Catalog:0:Costs:honey"]="20";d["BuildingUpgrades:Catalog:0:Costs:pollen"]="60";}return new WebApplicationFactory<Program>().WithWebHostBuilder(b=>{b.UseSetting("environment","Development");b.ConfigureAppConfiguration((_,c)=>{
        // appsettings.json now ships a real, populated BuildingUpgrades:Catalog (Hive
        // gameplay sprint) - Microsoft.Extensions.Configuration merges array sections by
        // index across providers rather than replacing them, so overriding just index 0
        // here would otherwise leave the shipped file's other 69 entries mixed in
        // underneath (extra offers, duplicate (buildingKey, fromLevel) validation
        // failures). Strip that one section out of what's already been bound from the
        // app's own config files before layering this test's own small catalog on top, so
        // the test stays isolated exactly as it was before the real catalog existed.
        IConfigurationRoot built=c.Build();
        var withoutBuildingUpgrades=built.AsEnumerable().Where(kv=>kv.Value!=null&&!kv.Key.StartsWith("BuildingUpgrades:",StringComparison.Ordinal)).ToDictionary(kv=>kv.Key,kv=>kv.Value);
        c.Sources.Clear();
        c.AddInMemoryCollection(withoutBuildingUpgrades);
        c.AddInMemoryCollection(d);
    });});}
}
