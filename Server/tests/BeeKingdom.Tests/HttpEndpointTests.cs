using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeeKingdom.Shared.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BeeKingdom.Tests;

public sealed class HttpEndpointTests
{
    [Test]
    public async Task AccountsEndpointCreatesAndReadsAccount()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/accounts",
            new { displayName = "Http Queen", email = "http-queen@bee.test" },
            BeeJson.CreateDefaultOptions());

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using JsonDocument created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        Guid accountId = created.RootElement
            .GetProperty("profile")
            .GetProperty("accountId")
            .GetGuid();

        HttpResponseMessage getResponse = await client.GetAsync($"/accounts/{accountId}");

        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using JsonDocument fetched = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(fetched.RootElement.GetProperty("profile").GetProperty("displayName").GetString(), Is.EqualTo("Http Queen"));
            Assert.That(fetched.RootElement.GetProperty("profile").GetProperty("email").GetString(), Is.EqualTo("http-queen@bee.test"));
        });
    }

    [Test]
    public async Task ColoniesEndpointCreatesReadsAndReportsStatistics()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        Guid playerId = Guid.NewGuid();
        Guid worldId = Guid.NewGuid();
        Guid queenId = Guid.NewGuid();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/colonies",
            new { playerId, worldId, hiveName = "HTTP Hive", queenId },
            BeeJson.CreateDefaultOptions());

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using JsonDocument created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        Guid colonyId = created.RootElement
            .GetProperty("profile")
            .GetProperty("colonyId")
            .GetProperty("value")
            .GetGuid();

        HttpResponseMessage getResponse = await client.GetAsync($"/colonies/{colonyId}");
        HttpResponseMessage statisticsResponse = await client.GetAsync($"/colonies/{colonyId}/statistics");

        Assert.Multiple(() =>
        {
            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(statisticsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });

        using JsonDocument fetched = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.That(fetched.RootElement.GetProperty("profile").GetProperty("hiveName").GetString(), Is.EqualTo("HTTP Hive"));
    }

    [Test]
    public async Task MigrationApplyEndpointRunsInMemoryRunner()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/ops/migrations/apply", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(payload.RootElement.GetProperty("status").GetString(), Is.EqualTo("Applied"));
    }

    [Test]
    public async Task RuntimeHandshakeReturnsNonGameplayBoundary()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/runtime/handshake",
            new
            {
                clientBuild = "editor-preview",
                clientEnvironment = "SandboxPlayground",
                supportedProtocolMajor = 1,
                supportedProtocolMinor = 0
            },
            BeeJson.CreateDefaultOptions());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string rawPayload = await response.Content.ReadAsStringAsync();
        using JsonDocument payload = JsonDocument.Parse(rawPayload);
        JsonElement root = payload.RootElement;
        JsonElement liveClaims = root.GetProperty("liveClaims");

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("clientProtocolCompatible").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("availability").GetString(), Is.EqualTo("ServerInPreparation"));
            Assert.That(root.GetProperty("gameServerId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000001"));
            Assert.That(root.GetProperty("defaultWorldId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000101"));
            Assert.That(root.GetProperty("shardName").GetString(), Is.EqualTo("production-preparation"));
            Assert.That(root.GetProperty("fallbackMode").GetString(), Is.EqualTo("LocalOnly"));
            Assert.That(root.GetProperty("nonGameplay").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("gameplayAuthorityGranted").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("mutationAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("requiresAccount").GetBoolean(), Is.False);
            Assert.That(liveClaims.GetProperty("accounts").GetBoolean(), Is.False);
            Assert.That(liveClaims.GetProperty("sessions").GetBoolean(), Is.False);
            Assert.That(liveClaims.GetProperty("persistence").GetBoolean(), Is.False);
            Assert.That(liveClaims.GetProperty("realTimeSynchronization").GetBoolean(), Is.False);
            Assert.That(liveClaims.GetProperty("economy").GetBoolean(), Is.False);
            Assert.That(liveClaims.GetProperty("social").GetBoolean(), Is.False);
            Assert.That(liveClaims.GetProperty("ranking").GetBoolean(), Is.False);
            Assert.That(liveClaims.GetProperty("matchmaking").GetBoolean(), Is.False);
            Assert.That(rawPayload, Does.Not.Contain("colonyId"));
            Assert.That(rawPayload, Does.Not.Contain("beeId"));
            Assert.That(rawPayload, Does.Not.Contain("resource"));
            Assert.That(rawPayload, Does.Not.Contain("inventory"));
            Assert.That(rawPayload, Does.Not.Contain("sessionId"));
            Assert.That(rawPayload, Does.Not.Contain("accountId"));
        });
    }

    [Test]
    public async Task RuntimeHandshakeReportsUnsupportedProtocolWithoutMutatingState()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/runtime/handshake",
            new
            {
                clientBuild = "future-preview",
                clientEnvironment = "SandboxPlayground",
                supportedProtocolMajor = 9,
                supportedProtocolMinor = 0
            },
            BeeJson.CreateDefaultOptions());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = payload.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("clientProtocolCompatible").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("nonGameplay").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("mutationAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("fallbackMode").GetString(), Is.EqualTo("LocalOnly"));
        });
    }

    [Test]
    public async Task ServerFirstReadinessReportsPublicNonLiveGate()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/runtime/server-first-readiness");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string rawPayload = await response.Content.ReadAsStringAsync();
        using JsonDocument payload = JsonDocument.Parse(rawPayload);
        JsonElement root = payload.RootElement;
        JsonElement forbidden = root.GetProperty("forbiddenClaims");

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("productionTarget").GetString(), Is.EqualTo("104.129.128.136"));
            Assert.That(root.GetProperty("handshakePath").GetString(), Is.EqualTo("/runtime/handshake"));
            Assert.That(root.GetProperty("gameServerId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000001"));
            Assert.That(root.GetProperty("defaultWorldId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000101"));
            Assert.That(root.GetProperty("shardName").GetString(), Is.EqualTo("production-preparation"));
            Assert.That(root.GetProperty("officialServerRequired").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("productionRouteProven").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("productionRouteStatus").GetString(), Is.EqualTo("NotRouted"));
            Assert.That(root.GetProperty("offlineMode").GetString(), Is.EqualTo("ConsultationOnly"));
            Assert.That(root.GetProperty("accountStatus").GetString(), Is.EqualTo("NotLive"));
            Assert.That(root.GetProperty("sessionStatus").GetString(), Is.EqualTo("NotLive"));
            Assert.That(root.GetProperty("colonyReadModelStatus").GetString(), Is.EqualTo("PreparationOnly"));
            Assert.That(root.GetProperty("gameplayAuthorityGranted").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("mutationAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("backupRequiredBeforeDeployment").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("rollbackRequiresApproval").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("secretsAllowedInReports").GetBoolean(), Is.False);
            Assert.That(forbidden.GetProperty("offlineOfficialPlay").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("accountLive").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("sessionLive").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("officialPersistence").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("realTimeSynchronization").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("economy").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("social").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("ranking").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("matchmaking").GetBoolean(), Is.True);
            Assert.That(rawPayload, Does.Not.Contain("AdminKey"));
            Assert.That(rawPayload, Does.Not.Contain("MigrationApplyKey"));
            Assert.That(rawPayload, Does.Not.Contain("Password"));
            Assert.That(rawPayload, Does.Not.Contain("connectionString"));
        });
    }

    [Test]
    public async Task AccountSessionReadinessReportsPreparedButNotLiveState()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/runtime/account-session-readiness");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string rawPayload = await response.Content.ReadAsStringAsync();
        using JsonDocument payload = JsonDocument.Parse(rawPayload);
        JsonElement root = payload.RootElement;
        JsonElement claims = root.GetProperty("claims");
        JsonElement blockers = root.GetProperty("blockers");

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("productionTarget").GetString(), Is.EqualTo("104.129.128.136"));
            Assert.That(root.GetProperty("gameServerId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000001"));
            Assert.That(root.GetProperty("defaultWorldId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000101"));
            Assert.That(root.GetProperty("shardName").GetString(), Is.EqualTo("production-preparation"));
            Assert.That(root.GetProperty("persistenceProvider").GetString(), Is.EqualTo("InMemory"));
            Assert.That(root.GetProperty("usesSqlServer").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("accountRepositoryConfigured").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("credentialStoreConfigured").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("sessionStoreConfigured").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("accountStatus").GetString(), Is.EqualTo("NotLive"));
            Assert.That(root.GetProperty("sessionStatus").GetString(), Is.EqualTo("NotLive"));
            Assert.That(root.GetProperty("credentialStatus").GetString(), Is.EqualTo("PreparationOnly"));
            Assert.That(root.GetProperty("colonyReadModelStatus").GetString(), Is.EqualTo("PreparationOnly"));
            Assert.That(root.GetProperty("accountCreationAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("sessionCreationAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("tokenIssuanceAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("officialPersistenceClaimAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("secretsAllowedInResponse").GetBoolean(), Is.False);
            Assert.That(claims.GetProperty("liveAccounts").GetBoolean(), Is.False);
            Assert.That(claims.GetProperty("liveSessions").GetBoolean(), Is.False);
            Assert.That(claims.GetProperty("officialProgression").GetBoolean(), Is.False);
            Assert.That(claims.GetProperty("officialPersistence").GetBoolean(), Is.False);
            Assert.That(claims.GetProperty("realTimeSynchronization").GetBoolean(), Is.False);
            Assert.That(claims.GetProperty("gameplayAuthorityGranted").GetBoolean(), Is.False);
            Assert.That(blockers.GetArrayLength(), Is.GreaterThan(0));
            Assert.That(rawPayload, Does.Not.Contain("accessToken"));
            Assert.That(rawPayload, Does.Not.Contain("refreshToken"));
            Assert.That(rawPayload, Does.Not.Contain("password"));
            Assert.That(rawPayload, Does.Not.Contain("connectionString"));
            Assert.That(rawPayload, Does.Not.Contain("sessionId"));
            Assert.That(rawPayload, Does.Not.Contain("accountId"));
        });
    }

    [Test]
    public async Task WorldMapReadinessReportsReadOnlyNonLiveFoundation()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/runtime/world-map-readiness");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string rawPayload = await response.Content.ReadAsStringAsync();
        using JsonDocument payload = JsonDocument.Parse(rawPayload);
        JsonElement root = payload.RootElement;
        JsonElement forbidden = root.GetProperty("forbiddenClaims");
        JsonElement nodeModels = root.GetProperty("nodeModels");
        JsonElement blockers = root.GetProperty("blockers");

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("productionTarget").GetString(), Is.EqualTo("104.129.128.136"));
            Assert.That(root.GetProperty("gameServerId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000001"));
            Assert.That(root.GetProperty("defaultWorldId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000101"));
            Assert.That(root.GetProperty("shardName").GetString(), Is.EqualTo("production-preparation"));
            Assert.That(root.GetProperty("worldMapStatus").GetString(), Is.EqualTo("PreparationOnly"));
            Assert.That(root.GetProperty("worldMapBoundary").GetString(), Is.EqualTo("ReadOnlyNonLiveFoundation"));
            Assert.That(root.GetProperty("readOnly").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("nonLive").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("productionRouteProven").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("mapGameplayEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("liveTerritoryEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("liveAllianceEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("liveScoutingEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("liveWarEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("liveEconomyEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("realTimeSynchronizationEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("officialProgressionEnabled").GetBoolean(), Is.False);
            Assert.That(forbidden.GetProperty("liveWorldMap").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("officialTerritory").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("activeAlliance").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("liveScouting").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("liveFlightPath").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("liveWar").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("livePvp").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("liveEconomy").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("ranking").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("matchmaking").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("realTimeSynchronization").GetBoolean(), Is.True);
            Assert.That(nodeModels.GetArrayLength(), Is.EqualTo(6));
            Assert.That(nodeModels.EnumerateArray().Select(node => node.GetProperty("nodeType").GetString()), Does.Contain("HiveMapNode"));
            Assert.That(nodeModels.EnumerateArray().All(node => node.GetProperty("readOnly").GetBoolean()), Is.True);
            Assert.That(nodeModels.EnumerateArray().All(node => node.GetProperty("liveClaimAllowed").GetBoolean() == false), Is.True);
            Assert.That(blockers.GetArrayLength(), Is.GreaterThan(0));
            Assert.That(rawPayload, Does.Not.Contain("territoryId"));
            Assert.That(rawPayload, Does.Not.Contain("allianceId"));
            Assert.That(rawPayload, Does.Not.Contain("accountId"));
            Assert.That(rawPayload, Does.Not.Contain("sessionId"));
            Assert.That(rawPayload, Does.Not.Contain("resourceAmount"));
            Assert.That(rawPayload, Does.Not.Contain("rankingPosition"));
        });
    }

    [Test]
    public async Task WorldRegistryReadinessReportsReadOnlyNonLiveDefaultWorld()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/runtime/world-registry-readiness");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string rawPayload = await response.Content.ReadAsStringAsync();
        using JsonDocument payload = JsonDocument.Parse(rawPayload);
        JsonElement root = payload.RootElement;
        JsonElement worlds = root.GetProperty("worlds");
        JsonElement world = worlds[0];
        JsonElement capacityPolicy = root.GetProperty("capacityPolicy");
        JsonElement statuses = capacityPolicy.GetProperty("supportedWorldStatuses");
        JsonElement forbidden = root.GetProperty("forbiddenClaims");
        JsonElement blockers = root.GetProperty("blockers");

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("productionTarget").GetString(), Is.EqualTo("104.129.128.136"));
            Assert.That(root.GetProperty("gameServerId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000001"));
            Assert.That(root.GetProperty("defaultWorldId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000101"));
            Assert.That(root.GetProperty("shardName").GetString(), Is.EqualTo("production-preparation"));
            Assert.That(root.GetProperty("registryStatus").GetString(), Is.EqualTo("PreparationOnly"));
            Assert.That(root.GetProperty("readOnly").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("nonLive").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("productionRouteProven").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("worldSelectionEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("worldCreationEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("worldTransferEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("worldMergeEnabled").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("livePopulationEnabled").GetBoolean(), Is.False);
            Assert.That(capacityPolicy.GetProperty("minAccountsPerWorld").GetInt32(), Is.EqualTo(800));
            Assert.That(capacityPolicy.GetProperty("maxAccountsPerWorld").GetInt32(), Is.EqualTo(1500));
            Assert.That(capacityPolicy.GetProperty("minActivePlayersPerWorld").GetInt32(), Is.EqualTo(300));
            Assert.That(capacityPolicy.GetProperty("maxActivePlayersPerWorld").GetInt32(), Is.EqualTo(600));
            Assert.That(capacityPolicy.GetProperty("minVeryActiveDailyPlayers").GetInt32(), Is.EqualTo(100));
            Assert.That(capacityPolicy.GetProperty("maxVeryActiveDailyPlayers").GetInt32(), Is.EqualTo(300));
            Assert.That(capacityPolicy.GetProperty("maxPlayersPerAlliance").GetInt32(), Is.EqualTo(100));
            Assert.That(statuses.EnumerateArray().Select(status => status.GetString()), Is.EquivalentTo(new[] { "Open", "Full", "Locked", "Maintenance", "Preparing" }));
            Assert.That(worlds.GetArrayLength(), Is.EqualTo(1));
            Assert.That(world.GetProperty("worldId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000101"));
            Assert.That(world.GetProperty("gameServerId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000001"));
            Assert.That(world.GetProperty("displayName").GetString(), Is.EqualTo("Bee Kingdom 1"));
            Assert.That(world.GetProperty("status").GetString(), Is.EqualTo("Preparing"));
            Assert.That(world.GetProperty("recommended").GetBoolean(), Is.False);
            Assert.That(world.GetProperty("joinable").GetBoolean(), Is.False);
            Assert.That(world.GetProperty("live").GetBoolean(), Is.False);
            Assert.That(world.GetProperty("capacity").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(world.GetProperty("population").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(world.GetProperty("minAccountsPerWorld").GetInt32(), Is.EqualTo(800));
            Assert.That(world.GetProperty("maxAccountsPerWorld").GetInt32(), Is.EqualTo(1500));
            Assert.That(world.GetProperty("minActivePlayersPerWorld").GetInt32(), Is.EqualTo(300));
            Assert.That(world.GetProperty("maxActivePlayersPerWorld").GetInt32(), Is.EqualTo(600));
            Assert.That(world.GetProperty("minVeryActiveDailyPlayers").GetInt32(), Is.EqualTo(100));
            Assert.That(world.GetProperty("maxVeryActiveDailyPlayers").GetInt32(), Is.EqualTo(300));
            Assert.That(world.GetProperty("maxPlayersPerAlliance").GetInt32(), Is.EqualTo(100));
            Assert.That(world.GetProperty("createdAccounts").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(world.GetProperty("activePlayersEstimate").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(world.GetProperty("veryActiveDailyPlayersEstimate").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(world.GetProperty("allianceCount").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(world.GetProperty("serverRecommended").GetBoolean(), Is.False);
            Assert.That(world.GetProperty("serverFull").GetBoolean(), Is.False);
            Assert.That(world.GetProperty("mockReadiness").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("liveWorldSelection").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("livePopulation").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("autoWorldCreation").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("worldTransfer").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("worldMerge").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("crossServerGameplay").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("ranking").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("matchmaking").GetBoolean(), Is.True);
            Assert.That(forbidden.GetProperty("officialProgression").GetBoolean(), Is.True);
            Assert.That(blockers.GetArrayLength(), Is.GreaterThan(0));
            Assert.That(rawPayload, Does.Not.Contain("accountId"));
            Assert.That(rawPayload, Does.Not.Contain("sessionId"));
            Assert.That(rawPayload, Does.Not.Contain("playerId"));
            Assert.That(rawPayload, Does.Not.Contain("allianceId"));
            Assert.That(rawPayload, Does.Not.Contain("rankingPosition"));
        });
    }

    [Test]
    public async Task WorldRegistryReadinessSupportsConfiguredMultiWorldReadinessWithoutLiveCounters()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WorldRegistryReadiness:Worlds:0:WorldId"] = "00000000-0000-0000-0000-000000000201",
                    ["WorldRegistryReadiness:Worlds:0:GameServerId"] = "00000000-0000-0000-0000-000000000021",
                    ["WorldRegistryReadiness:Worlds:0:DisplayName"] = "Bee Kingdom East",
                    ["WorldRegistryReadiness:Worlds:0:Status"] = "Open",
                    ["WorldRegistryReadiness:Worlds:0:Region"] = "NA-East",
                    ["WorldRegistryReadiness:Worlds:0:Locale"] = "fr-CA",
                    ["WorldRegistryReadiness:Worlds:0:ServerRecommended"] = "true",
                    ["WorldRegistryReadiness:Worlds:0:ServerFull"] = "false",
                    ["WorldRegistryReadiness:Worlds:1:WorldId"] = "00000000-0000-0000-0000-000000000202",
                    ["WorldRegistryReadiness:Worlds:1:GameServerId"] = "00000000-0000-0000-0000-000000000022",
                    ["WorldRegistryReadiness:Worlds:1:DisplayName"] = "Bee Kingdom West",
                    ["WorldRegistryReadiness:Worlds:1:Status"] = "Full",
                    ["WorldRegistryReadiness:Worlds:1:Region"] = "NA-West",
                    ["WorldRegistryReadiness:Worlds:1:Locale"] = "en-US",
                    ["WorldRegistryReadiness:Worlds:1:ServerRecommended"] = "false",
                    ["WorldRegistryReadiness:Worlds:1:ServerFull"] = "true"
                });
            });
        });

        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/runtime/world-registry-readiness");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string rawPayload = await response.Content.ReadAsStringAsync();
        using JsonDocument payload = JsonDocument.Parse(rawPayload);
        JsonElement root = payload.RootElement;
        JsonElement worlds = root.GetProperty("worlds");
        JsonElement recommendedWorld = worlds[0];
        JsonElement fullWorld = worlds[1];

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("readOnly").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("nonLive").GetBoolean(), Is.True);
            Assert.That(worlds.GetArrayLength(), Is.EqualTo(2));
            Assert.That(recommendedWorld.GetProperty("worldId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000201"));
            Assert.That(recommendedWorld.GetProperty("gameServerId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000021"));
            Assert.That(recommendedWorld.GetProperty("status").GetString(), Is.EqualTo("Open"));
            Assert.That(recommendedWorld.GetProperty("serverRecommended").GetBoolean(), Is.True);
            Assert.That(recommendedWorld.GetProperty("serverFull").GetBoolean(), Is.False);
            Assert.That(fullWorld.GetProperty("worldId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000202"));
            Assert.That(fullWorld.GetProperty("gameServerId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000022"));
            Assert.That(fullWorld.GetProperty("status").GetString(), Is.EqualTo("Full"));
            Assert.That(fullWorld.GetProperty("serverRecommended").GetBoolean(), Is.False);
            Assert.That(fullWorld.GetProperty("serverFull").GetBoolean(), Is.True);
            Assert.That(recommendedWorld.GetProperty("createdAccounts").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(recommendedWorld.GetProperty("activePlayersEstimate").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(recommendedWorld.GetProperty("veryActiveDailyPlayersEstimate").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(recommendedWorld.GetProperty("allianceCount").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(fullWorld.GetProperty("createdAccounts").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(fullWorld.GetProperty("activePlayersEstimate").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(fullWorld.GetProperty("veryActiveDailyPlayersEstimate").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(fullWorld.GetProperty("allianceCount").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(rawPayload, Does.Not.Contain("accountId"));
            Assert.That(rawPayload, Does.Not.Contain("sessionId"));
            Assert.That(rawPayload, Does.Not.Contain("playerId"));
            Assert.That(rawPayload, Does.Not.Contain("allianceId"));
        });
    }

    [Test]
    public async Task WorldIdentityReadinessReportsValidScopedNonLiveIdentifiers()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/runtime/world-identity-readiness");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string rawPayload = await response.Content.ReadAsStringAsync();
        using JsonDocument payload = JsonDocument.Parse(rawPayload);
        JsonElement root = payload.RootElement;
        JsonElement scopes = root.GetProperty("requiredScopes");
        JsonElement blockers = root.GetProperty("blockers");

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("gameServerId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000001"));
            Assert.That(root.GetProperty("defaultWorldId").GetString(), Is.EqualTo("00000000-0000-0000-0000-000000000101"));
            Assert.That(root.GetProperty("shardName").GetString(), Is.EqualTo("production-preparation"));
            Assert.That(root.GetProperty("gameServerIdValid").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("defaultWorldIdValid").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("gameServerIdAndWorldIdDistinct").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("requiresWorldScopeForAccounts").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("requiresWorldScopeForColonies").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("requiresWorldScopeForWorldMap").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("singleWorldAssumptionAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("liveWorldSelectionAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("officialProgressionAllowed").GetBoolean(), Is.False);
            Assert.That(scopes.GetArrayLength(), Is.EqualTo(6));
            Assert.That(scopes.EnumerateArray().Select(scope => scope.GetProperty("domain").GetString()), Does.Contain("Accounts"));
            Assert.That(scopes.EnumerateArray().Select(scope => scope.GetProperty("domain").GetString()), Does.Contain("WorldMap"));
            Assert.That(scopes.EnumerateArray().All(scope => scope.GetProperty("requiresGameServerId").GetBoolean()), Is.True);
            Assert.That(scopes.EnumerateArray().All(scope => scope.GetProperty("requiresWorldId").GetBoolean()), Is.True);
            Assert.That(blockers.GetArrayLength(), Is.GreaterThan(0));
            Assert.That(rawPayload, Does.Not.Contain("accountId"));
            Assert.That(rawPayload, Does.Not.Contain("sessionId"));
            Assert.That(rawPayload, Does.Not.Contain("playerId"));
            Assert.That(rawPayload, Does.Not.Contain("colonyId"));
            Assert.That(rawPayload, Does.Not.Contain("allianceId"));
        });
    }

    [Test]
    public async Task OpsEndpointsRequireAdminKeyWhenEnabled()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKey"] = "test-admin-key"
                });
            });
        });

        using HttpClient client = factory.CreateClient();

        HttpResponseMessage unauthorized = await client.GetAsync("/ops/monitoring");
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Admin-Key", "test-admin-key");
        HttpResponseMessage authorized = await client.GetAsync("/ops/monitoring");

        Assert.Multiple(() =>
        {
            Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(authorized.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task OpsEndpointsFailClosedWhenAdminKeyIsMissing()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKey"] = ""
                });
            });
        });

        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/ops/monitoring");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task OpsEndpointsAcceptSha256AdminKeyWithoutPlainTextConfiguration()
    {
        const string adminKey = "hashed-admin-key";

        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKey"] = "",
                    ["Ops:AdminKeySha256"] = Sha256Hex(adminKey)
                });
            });
        });

        using HttpClient client = factory.CreateClient();

        HttpResponseMessage unauthorized = await client.GetAsync("/ops/monitoring");
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Admin-Key", adminKey);
        HttpResponseMessage authorized = await client.GetAsync("/ops/monitoring");

        Assert.Multiple(() =>
        {
            Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(authorized.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task MigrationApplyRequiresDedicatedMigrationKeyWhenEnabled()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKey"] = "test-admin-key",
                    ["Ops:RequireMigrationApplyKey"] = "true",
                    ["Ops:MigrationApplyKey"] = "test-migration-key"
                });
            });
        });

        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Admin-Key", "test-admin-key");

        HttpResponseMessage missingMigrationKey = await client.PostAsync("/ops/migrations/apply", null);
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Migration-Key", "test-migration-key");
        HttpResponseMessage authorized = await client.PostAsync("/ops/migrations/apply", null);

        Assert.Multiple(() =>
        {
            Assert.That(missingMigrationKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(authorized.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task MigrationApplyFailsClosedWhenMigrationKeyMatchesAdminKey()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKey"] = "same-key",
                    ["Ops:RequireMigrationApplyKey"] = "true",
                    ["Ops:MigrationApplyKey"] = "same-key"
                });
            });
        });

        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Admin-Key", "same-key");
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Migration-Key", "same-key");

        HttpResponseMessage response = await client.PostAsync("/ops/migrations/apply", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task MigrationApplyAcceptsSha256MigrationKeyWithoutPlainTextConfiguration()
    {
        const string adminKey = "hashed-admin-key";
        const string migrationKey = "hashed-migration-key";

        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKey"] = "",
                    ["Ops:AdminKeySha256"] = Sha256Hex(adminKey),
                    ["Ops:RequireMigrationApplyKey"] = "true",
                    ["Ops:MigrationApplyKey"] = "",
                    ["Ops:MigrationApplyKeySha256"] = Sha256Hex(migrationKey)
                });
            });
        });

        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Admin-Key", adminKey);

        HttpResponseMessage missingMigrationKey = await client.PostAsync("/ops/migrations/apply", null);
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Migration-Key", migrationKey);
        HttpResponseMessage authorized = await client.PostAsync("/ops/migrations/apply", null);

        Assert.Multiple(() =>
        {
            Assert.That(missingMigrationKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(authorized.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task OpsMonitoringIncludesMigrationDiagnostics()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsync("/ops/migrations/apply", null);
        HttpResponseMessage response = await client.GetAsync("/ops/monitoring");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement migrations = payload.RootElement.GetProperty("migrations");
        Assert.Multiple(() =>
        {
            Assert.That(migrations.GetProperty("applyAttempts").GetInt64(), Is.GreaterThanOrEqualTo(1));
            Assert.That(migrations.GetProperty("appliedScripts").GetInt64(), Is.GreaterThanOrEqualTo(1));
            Assert.That(migrations.GetProperty("failures").GetInt64(), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task OpsReadinessRequiresAdminKeyWhenEnabled()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKey"] = "readiness-admin-key"
                });
            });
        });

        using HttpClient client = factory.CreateClient();

        HttpResponseMessage unauthorized = await client.GetAsync("/ops/readiness");
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Admin-Key", "readiness-admin-key");
        HttpResponseMessage authorized = await client.GetAsync("/ops/readiness");

        Assert.Multiple(() =>
        {
            Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(authorized.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task OpsReadinessReportsSqlAndOpsConfigurationWithoutLeakingSecrets()
    {
        const string adminKey = "readiness-admin-secret";
        const string migrationKey = "readiness-migration-secret";
        const string runtimeSecret = "runtime-password-secret";
        const string migrationSecret = "migration-password-secret";

        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Persistence:Provider"] = "SqlServer",
                    ["SqlServer:RuntimeConnectionStringName"] = "BeeKingdomRuntime",
                    ["SqlServer:MigrationConnectionStringName"] = "BeeKingdomMigrations",
                    ["ConnectionStrings:BeeKingdomRuntime"] = $"Server=runtime-host;Database=BeeKingdom;User Id=bee_runtime;Password={runtimeSecret};Encrypt=True;TrustServerCertificate=False;",
                    ["ConnectionStrings:BeeKingdomMigrations"] = $"Server=migration-host;Database=BeeKingdom;User Id=bee_migration;Password={migrationSecret};Encrypt=True;TrustServerCertificate=False;",
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKey"] = adminKey,
                    ["Ops:RequireMigrationApplyKey"] = "true",
                    ["Ops:MigrationApplyKey"] = migrationKey
                });
            });
        });

        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Admin-Key", adminKey);

        HttpResponseMessage response = await client.GetAsync("/ops/readiness");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string rawPayload = await response.Content.ReadAsStringAsync();
        using JsonDocument payload = JsonDocument.Parse(rawPayload);
        JsonElement root = payload.RootElement;
        JsonElement sql = root.GetProperty("sqlServer");
        JsonElement operations = root.GetProperty("operations");

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("ready").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("persistence").GetProperty("sqlServerEnabled").GetBoolean(), Is.True);
            Assert.That(sql.GetProperty("runtimeConnectionStringName").GetString(), Is.EqualTo("BeeKingdomRuntime"));
            Assert.That(sql.GetProperty("migrationConnectionStringName").GetString(), Is.EqualTo("BeeKingdomMigrations"));
            Assert.That(sql.GetProperty("runtimeConnectionStringConfigured").GetBoolean(), Is.True);
            Assert.That(sql.GetProperty("migrationConnectionStringConfigured").GetBoolean(), Is.True);
            Assert.That(sql.GetProperty("runtimeAndMigrationIdentitiesSeparated").GetBoolean(), Is.True);
            Assert.That(operations.GetProperty("monitoringSecured").GetBoolean(), Is.True);
            Assert.That(operations.GetProperty("rollbackPlanSecured").GetBoolean(), Is.True);
            Assert.That(operations.GetProperty("migrationApplySecured").GetBoolean(), Is.True);
            Assert.That(rawPayload, Does.Not.Contain(adminKey));
            Assert.That(rawPayload, Does.Not.Contain(migrationKey));
            Assert.That(rawPayload, Does.Not.Contain(runtimeSecret));
            Assert.That(rawPayload, Does.Not.Contain(migrationSecret));
        });
    }

    [Test]
    public async Task RollbackPlanEndpointReturnsNonExecutableDestructivePlan()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/ops/migrations/rollback-plan");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement scripts = payload.RootElement.GetProperty("scripts");
        Assert.Multiple(() =>
        {
            Assert.That(payload.RootElement.GetProperty("destructive").GetBoolean(), Is.True);
            Assert.That(payload.RootElement.GetProperty("executableByEndpoint").GetBoolean(), Is.False);
            Assert.That(payload.RootElement.GetProperty("requiresBackup").GetBoolean(), Is.True);
            Assert.That(scripts.GetArrayLength(), Is.EqualTo(10));
            Assert.That(scripts[0].GetProperty("name").GetString(), Is.EqualTo("070_rollback_hive_operations.sql"));
            Assert.That(scripts[1].GetProperty("name").GetString(), Is.EqualTo("064_rollback_chat_contract_bounds.sql"));
            Assert.That(scripts[5].GetProperty("name").GetString(), Is.EqualTo("060_rollback_chat_messaging.sql"));
        });
    }

    [Test]
    public async Task SqlProductionDryRunRequiresAdminKeyWhenEnabled()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKey"] = "dry-run-admin-key"
                });
            });
        });

        using HttpClient client = factory.CreateClient();

        HttpResponseMessage unauthorized = await client.GetAsync("/ops/sql-production-dry-run");
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Admin-Key", "dry-run-admin-key");
        HttpResponseMessage authorized = await client.GetAsync("/ops/sql-production-dry-run");

        Assert.Multiple(() =>
        {
            Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(authorized.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task SqlProductionDryRunReportsReadinessWithoutLeakingSecrets()
    {
        const string adminKey = "dry-run-admin-secret";
        const string migrationKey = "dry-run-migration-secret";
        const string runtimeSecret = "runtime-dry-run-password";
        const string migrationSecret = "migration-dry-run-password";

        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Persistence:Provider"] = "SqlServer",
                    ["SqlServer:RuntimeConnectionStringName"] = "BeeKingdomRuntime",
                    ["SqlServer:MigrationConnectionStringName"] = "BeeKingdomMigrations",
                    ["ConnectionStrings:BeeKingdomRuntime"] = $"Server=runtime-host;Database=BeeKingdom;User Id=bee_runtime;Password={runtimeSecret};Encrypt=True;TrustServerCertificate=False;",
                    ["ConnectionStrings:BeeKingdomMigrations"] = $"Server=migration-host;Database=BeeKingdom;User Id=bee_migration;Password={migrationSecret};Encrypt=True;TrustServerCertificate=False;",
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKeySha256"] = Sha256Hex(adminKey),
                    ["Ops:RequireMigrationApplyKey"] = "true",
                    ["Ops:MigrationApplyKeySha256"] = Sha256Hex(migrationKey),
                    ["SqlProductionDryRun:BackupEvidenceReference"] = "backup-evidence-recorded-outside-repo",
                    ["SqlProductionDryRun:MaintenanceWindowReference"] = "maintenance-window-approved-outside-repo",
                    ["SqlProductionDryRun:RollbackPlanAcknowledged"] = "true"
                });
            });
        });

        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Admin-Key", adminKey);

        HttpResponseMessage response = await client.GetAsync("/ops/sql-production-dry-run");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        string rawPayload = await response.Content.ReadAsStringAsync();
        using JsonDocument payload = JsonDocument.Parse(rawPayload);
        JsonElement root = payload.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("readyForDryRun").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("destructive").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("appliesMigrations").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("executesRollback").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("targetHost").GetString(), Is.EqualTo("104.129.128.136"));
            Assert.That(root.GetProperty("sqlServer").GetProperty("runtimeAndMigrationIdentitiesSeparated").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("backup").GetProperty("evidenceReferenceConfigured").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("maintenance").GetProperty("windowReferenceConfigured").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("rollback").GetProperty("endpointExecutable").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("accountSessionReadModelPreparation").GetProperty("accountsTablePlanned").GetBoolean(), Is.True);
            Assert.That(rawPayload, Does.Not.Contain(adminKey));
            Assert.That(rawPayload, Does.Not.Contain(migrationKey));
            Assert.That(rawPayload, Does.Not.Contain(runtimeSecret));
            Assert.That(rawPayload, Does.Not.Contain(migrationSecret));
        });
    }

    [Test]
    public async Task SqlProductionDryRunBlocksWithoutBackupMaintenanceAndRollbackAcknowledgement()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Persistence:Provider"] = "SqlServer",
                    ["SqlServer:RuntimeConnectionStringName"] = "BeeKingdomRuntime",
                    ["SqlServer:MigrationConnectionStringName"] = "BeeKingdomMigrations",
                    ["ConnectionStrings:BeeKingdomRuntime"] = "Server=runtime-host;Database=BeeKingdom;User Id=bee_runtime;Password=runtime-password;Encrypt=True;",
                    ["ConnectionStrings:BeeKingdomMigrations"] = "Server=migration-host;Database=BeeKingdom;User Id=bee_migration;Password=migration-password;Encrypt=True;",
                    ["Ops:RequireAdminKey"] = "true",
                    ["Ops:AdminKey"] = "dry-run-admin-key",
                    ["Ops:RequireMigrationApplyKey"] = "true",
                    ["Ops:MigrationApplyKey"] = "dry-run-migration-key"
                });
            });
        });

        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BeeKingdom-Admin-Key", "dry-run-admin-key");

        HttpResponseMessage response = await client.GetAsync("/ops/sql-production-dry-run");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement blockers = payload.RootElement.GetProperty("blockers");

        Assert.Multiple(() =>
        {
            Assert.That(payload.RootElement.GetProperty("readyForDryRun").GetBoolean(), Is.False);
            Assert.That(blockers.EnumerateArray().Select(item => item.GetString()), Does.Contain("Verified SQL backup evidence reference is required before production SQL dry run."));
            Assert.That(blockers.EnumerateArray().Select(item => item.GetString()), Does.Contain("Maintenance window reference is required before production SQL dry run."));
            Assert.That(blockers.EnumerateArray().Select(item => item.GetString()), Does.Contain("Rollback plan must be acknowledged before production SQL dry run."));
        });
    }

    [Test]
    public async Task HiveLoopCommandEndpointsAreNotExposed()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        Guid playerId = Guid.NewGuid();
        Guid buildingId = Guid.NewGuid();

        HttpResponseMessage upgrade = await client.PostAsJsonAsync(
            $"/players/{playerId}/hive/buildings/{buildingId}/upgrade",
            new { buildingKey = "honey_storage" },
            BeeJson.CreateDefaultOptions());
        HttpResponseMessage training = await client.PostAsJsonAsync(
            $"/players/{playerId}/army/training",
            new { troopKey = "worker_bee", quantity = 1 },
            BeeJson.CreateDefaultOptions());

        Assert.Multiple(() =>
        {
            Assert.That(upgrade.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(training.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task HiveLoopRepositoryEndpointsAreNotExposed()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        Guid playerId = Guid.NewGuid();

        HttpResponseMessage resources = await client.GetAsync($"/players/{playerId}/hive/resources");
        HttpResponseMessage buildings = await client.GetAsync($"/players/{playerId}/hive/buildings");
        HttpResponseMessage queues = await client.GetAsync($"/players/{playerId}/hive/queues");
        HttpResponseMessage idempotency = await client.PostAsJsonAsync(
            $"/players/{playerId}/hive/idempotency-records",
            new { idempotencyKeyHash = "readiness-only" },
            BeeJson.CreateDefaultOptions());

        Assert.Multiple(() =>
        {
            Assert.That(resources.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(buildings.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(queues.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(idempotency.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task HiveActionLoopDevOnlyBridgeEndpointsAreNotExposed()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        Guid playerId = Guid.NewGuid();

        HttpResponseMessage resourceTick = await client.PostAsJsonAsync($"/dev-only/players/{playerId}/hive/resource-tick", new { resourceKey = "honey" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage upgrade = await client.PostAsJsonAsync($"/dev-only/players/{playerId}/hive/upgrade", new { buildingKey = "honey_storage" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage training = await client.PostAsJsonAsync($"/dev-only/players/{playerId}/hive/training", new { troopKey = "worker_bee" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage save = await client.PostAsJsonAsync($"/dev-only/players/{playerId}/hive/save", new { snapshot = "future-only" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage resourceCommand = await client.PostAsJsonAsync($"/dev-only/players/{playerId}/hive/resource-command", new { resourceKey = "honey" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage snapshot = await client.GetAsync($"/dev-only/players/{playerId}/hive/snapshot");
        HttpResponseMessage reconcile = await client.PostAsJsonAsync($"/dev-only/players/{playerId}/hive/reconcile", new { snapshotRevision = 1 }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage officialResourceCommand = await client.PostAsJsonAsync($"/players/{playerId}/hive/resources/commands", new { resourceKey = "honey" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage officialUpgrade = await client.PostAsJsonAsync($"/players/{playerId}/hive/buildings/{Guid.NewGuid()}/upgrade", new { buildingKey = "nursery" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage officialTraining = await client.PostAsJsonAsync($"/players/{playerId}/hive/training", new { troopKey = "worker_bee" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage officialSnapshot = await client.GetAsync($"/players/{playerId}/hive/snapshot");
        HttpResponseMessage officialReconciliation = await client.PostAsJsonAsync($"/players/{playerId}/hive/reconciliation", new { snapshotRevision = 1 }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage evidencePrep = await client.PostAsJsonAsync($"/players/{playerId}/hive/evidence", new { evidenceId = "SERVER-045" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage idempotencyEvidence = await client.PostAsJsonAsync($"/players/{playerId}/hive/idempotency-evidence", new { idempotencyKey = "preview" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage snapshotEvidence = await client.PostAsJsonAsync($"/players/{playerId}/hive/snapshot-delta-evidence", new { snapshotRevision = 1 }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage officialSave = await client.PostAsJsonAsync($"/players/{playerId}/hive/official-save", new { snapshotRevision = 1 }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage carryForward = await client.PostAsJsonAsync($"/players/{playerId}/hive/non-claim-carry-forward", new { targetDemo = "DEMO-075" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage evidenceContinuity = await client.PostAsJsonAsync($"/players/{playerId}/hive/evidence-continuity", new { sourceEvidenceId = "SERVER-045" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage liveStateMatrix = await client.GetAsync($"/players/{playerId}/hive/live-state-matrix");
        HttpResponseMessage serverFutureSupport = await client.GetAsync($"/players/{playerId}/hive/server-future-support");
        HttpResponseMessage demo076ServerManifest = await client.PostAsJsonAsync($"/players/{playerId}/hive/demo076-server-manifest", new { official_server_live = false }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage qa076LiveClaimChecklist = await client.GetAsync($"/players/{playerId}/hive/qa076-live-claim-checklist");
        HttpResponseMessage officialServerBoundary = await client.GetAsync($"/players/{playerId}/hive/official-server-claim-boundary");
        HttpResponseMessage demo077BoundaryManifest = await client.PostAsJsonAsync($"/players/{playerId}/hive/demo077-server-boundary", new { official_server_live = false }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage qa077ClaimBoundary = await client.GetAsync($"/players/{playerId}/hive/qa077-official-server-claim-boundary");
        HttpResponseMessage serverLiveVisualGuard = await client.GetAsync($"/players/{playerId}/hive/server-live-claim-visual-guard");
        HttpResponseMessage demo078VisualManifest = await client.PostAsJsonAsync($"/players/{playerId}/hive/demo078-visual-server-guard", new { official_server_live = false }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage qa078VisualGuard = await client.GetAsync($"/players/{playerId}/hive/qa078-server-live-visual-guard");
        HttpResponseMessage officialAuthAccounts = await client.PostAsJsonAsync("/auth/accounts", new { email = "bee@example.test" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage obsoleteTokenRefreshPath = await client.PostAsJsonAsync("/auth/token/refresh", new { refreshToken = "future-only" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage officialProfile = await client.GetAsync("/me/profile");
        HttpResponseMessage gameServers = await client.GetAsync("/game-servers");
        HttpResponseMessage gameServerSelection = await client.PostAsJsonAsync("/me/game-server-selection", new { gameServerId = Guid.NewGuid() }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage googleLink = await client.PostAsJsonAsync("/auth/link/google", new { providerToken = "not-a-real-token" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage facebookLink = await client.PostAsJsonAsync("/auth/link/facebook", new { providerToken = "not-a-real-token" }, BeeJson.CreateDefaultOptions());
        HttpResponseMessage guestDemo = await client.PostAsJsonAsync("/auth/guest", new { guestDeviceId = "demo-only" }, BeeJson.CreateDefaultOptions());

        Assert.Multiple(() =>
        {
            Assert.That(resourceTick.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(upgrade.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(training.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(resourceCommand.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(snapshot.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(reconcile.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(officialResourceCommand.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(officialUpgrade.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(officialTraining.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(officialSnapshot.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(officialReconciliation.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(evidencePrep.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(idempotencyEvidence.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(snapshotEvidence.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(officialSave.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(carryForward.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(evidenceContinuity.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(liveStateMatrix.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(serverFutureSupport.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(demo076ServerManifest.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(qa076LiveClaimChecklist.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(officialServerBoundary.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(demo077BoundaryManifest.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(qa077ClaimBoundary.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(serverLiveVisualGuard.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(demo078VisualManifest.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(qa078VisualGuard.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(officialAuthAccounts.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(obsoleteTokenRefreshPath.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(officialProfile.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(gameServers.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(gameServerSelection.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(googleLink.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(facebookLink.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(guestDemo.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    private static WebApplicationFactory<Program> CreateFactory(Action<IWebHostBuilder>? configure = null)
    {
        WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("environment", "Development"));
        return configure == null ? factory : factory.WithWebHostBuilder(configure);
    }

    private static string Sha256Hex(string value)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
