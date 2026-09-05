using BeeKingdom.Accounts;
using BeeKingdom.Accounts.DependencyInjection;
using BeeKingdom.Accounts.Models;
using BeeKingdom.Alliance;
using BeeKingdom.Alliance.Configuration;
using BeeKingdom.Alliance.DependencyInjection;
using BeeKingdom.Alliance.Help;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Research;
using BeeKingdom.Authentication;
using BeeKingdom.Authentication.DependencyInjection;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Authentication.Security;
using BeeKingdom.Chat;
using BeeKingdom.Chat.DependencyInjection;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Realtime;
using BeeKingdom.Chat.Translations;
using BeeKingdom.Colony;
using BeeKingdom.Colony.DependencyInjection;
using BeeKingdom.Colony.Models;
using BeeKingdom.Database;
using BeeKingdom.Gateway;
using BeeKingdom.Gateway.DependencyInjection;
using BeeKingdom.Gateway.Models;
using BeeKingdom.HiveOperations;
using BeeKingdom.News;
using IServerClock = BeeKingdom.Infrastructure.Time.IServerClock;
using BeeKingdom.Infrastructure.Configuration;
using BeeKingdom.Infrastructure.DependencyInjection;
using BeeKingdom.Infrastructure.Hosting;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Persistence.DependencyInjection;
using BeeKingdom.Persistence.Configuration;
using BeeKingdom.Persistence.Migrations;
using BeeKingdom.Protocol.Requests;
using BeeKingdom.Protocol.Responses;
using BeeKingdom.Protocol.Versioning;
using BeeKingdom.Server;
using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Simulation;
using BeeKingdom.Simulation.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_048_576);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services
    .AddBeeKingdomInfrastructure(builder.Configuration)
    .AddBeeKingdomPersistence(builder.Configuration)
    .AddBeeKingdomAuthentication(builder.Configuration)
    .AddBeeKingdomChat(builder.Configuration)
    .AddBeeKingdomAlliance(builder.Configuration)
    .AddBeeKingdomAccounts(builder.Configuration)
    .AddBeeKingdomGateway(builder.Configuration)
    .AddBeeKingdomColony(builder.Configuration)
    .AddBeeKingdomSimulation(builder.Configuration);

builder.Services.AddSingleton<IHiveStateRepository>(serviceProvider =>
{
    PersistenceOptions persistence = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
    SqlServerOptions sql = serviceProvider.GetRequiredService<IOptions<SqlServerOptions>>().Value;
    if (string.Equals(persistence.Provider, PersistenceOptions.SqlServerProvider, StringComparison.OrdinalIgnoreCase))
    {
        string connectionString = sql.RuntimeConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(sql.RuntimeConnectionStringName) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = sql.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(sql.ConnectionStringName) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("SQL hive persistence requires an external runtime connection string.");
        return new SqlHiveStateRepository(connectionString, CreateInitialHiveState, sql.CommandTimeoutSeconds);
    }

    return new DurableJsonHiveStateRepository(Path.Combine(AppContext.BaseDirectory, "data", "hives"), CreateInitialHiveState);
});
builder.Services.AddSingleton<HiveOperationService>(serviceProvider => new HiveOperationService(
    serviceProvider.GetRequiredService<IHiveStateRepository>(),
    new BeeKingdom.HiveOperations.SystemServerClock(),
    Array.Empty<BuildingOperationDefinition>()));
builder.Services.AddSingleton<StrategicPathService>(serviceProvider => new StrategicPathService(
    serviceProvider.GetRequiredService<IHiveStateRepository>(), new BeeKingdom.HiveOperations.SystemServerClock()));
builder.Services.AddSingleton<CombatDoctrineService>();
builder.Services.AddSingleton<CombatFormationReadinessService>();
builder.Services.AddSingleton<CombatRecruitmentService>(sp => new CombatRecruitmentService(sp.GetRequiredService<IHiveStateRepository>(), new BeeKingdom.HiveOperations.SystemServerClock()));
builder.Services.AddSingleton<CombatSquadReservationService>(sp => new CombatSquadReservationService(sp.GetRequiredService<IHiveStateRepository>(), sp.GetRequiredService<BeeKingdom.HiveOperations.IServerClock>()));
builder.Services.AddSingleton<BeeKingdom.HiveOperations.IServerClock, BeeKingdom.HiveOperations.SystemServerClock>();
builder.Services.AddSingleton<HivePerimeterSortieService>(sp => new HivePerimeterSortieService(sp.GetRequiredService<IHiveStateRepository>(), sp.GetRequiredService<BeeKingdom.HiveOperations.IServerClock>()));
builder.Services.AddSingleton<CombatPatrolService>(sp => new CombatPatrolService(sp.GetRequiredService<IHiveStateRepository>(), sp.GetRequiredService<BeeKingdom.HiveOperations.IServerClock>(), sp.GetRequiredService<IAllianceGameplayBonusResolver>()));
builder.Services.AddSingleton<AdminSupportService>(sp => new AdminSupportService(sp.GetRequiredService<IHiveStateRepository>(), sp.GetRequiredService<BeeKingdom.HiveOperations.IServerClock>()));
builder.Services.AddSingleton<RewardLedgerService>(sp => new RewardLedgerService(sp.GetRequiredService<IHiveStateRepository>(), sp.GetRequiredService<BeeKingdom.HiveOperations.IServerClock>(), sp.GetRequiredService<IOptions<RewardLedgerOptions>>().Value));
builder.Services.AddBeeKingdomAllianceHelp(builder.Configuration);
builder.Services.AddBeeKingdomAllianceResearch(builder.Configuration);
builder.Services.AddBeeKingdomNews(builder.Configuration);
builder.Services.AddOptions<AdminSupportOptions>()
    .Bind(builder.Configuration.GetSection(AdminSupportOptions.SectionName));
builder.Services.AddOptions<DevToolsOptions>()
    .Bind(builder.Configuration.GetSection(DevToolsOptions.SectionName));

builder.Services.AddOptions<OpsSecurityOptions>()
    .Bind(builder.Configuration.GetSection(OpsSecurityOptions.SectionName));
builder.Services.AddOptions<SqlProductionDryRunOptions>()
    .Bind(builder.Configuration.GetSection(SqlProductionDryRunOptions.SectionName));
builder.Services.AddOptions<RuntimeHandshakeOptions>()
    .Bind(builder.Configuration.GetSection(RuntimeHandshakeOptions.SectionName));
builder.Services.AddOptions<ServerFirstReadinessOptions>()
    .Bind(builder.Configuration.GetSection(ServerFirstReadinessOptions.SectionName));
builder.Services.AddOptions<ServerIdentityOptions>()
    .Bind(builder.Configuration.GetSection(ServerIdentityOptions.SectionName));
builder.Services.AddOptions<AccountSessionReadinessOptions>()
    .Bind(builder.Configuration.GetSection(AccountSessionReadinessOptions.SectionName));
builder.Services.AddOptions<WorldMapReadinessOptions>()
    .Bind(builder.Configuration.GetSection(WorldMapReadinessOptions.SectionName));
builder.Services.AddOptions<WorldRegistryReadinessOptions>()
    .Bind(builder.Configuration.GetSection(WorldRegistryReadinessOptions.SectionName));
builder.Services.AddOptions<FoundationDotationOptions>()
    .Bind(builder.Configuration.GetSection(FoundationDotationOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<BroodVitalityOptions>()
    .Bind(builder.Configuration.GetSection(BroodVitalityOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<WorkshopBatchQualificationOptions>()
    .Bind(builder.Configuration.GetSection(WorkshopBatchQualificationOptions.SectionName));
builder.Services.AddOptions<LivingHiveResearchOptions>()
    .Bind(builder.Configuration.GetSection(LivingHiveResearchOptions.SectionName));
builder.Services.AddOptions<ChampionBeeProgressionOptions>()
    .Bind(builder.Configuration.GetSection(ChampionBeeProgressionOptions.SectionName));
builder.Services.AddOptions<VipProgressionOptions>()
    .Bind(builder.Configuration.GetSection(VipProgressionOptions.SectionName));
builder.Services.AddOptions<HiveStockSnapshotOptions>()
    .Bind(builder.Configuration.GetSection(HiveStockSnapshotOptions.SectionName))
    .Validate(options => { options.Validate(); return true; }, "Invalid hive stock options")
    .ValidateOnStart();
builder.Services.AddOptions<HiveDailyRoundOptions>()
    .Bind(builder.Configuration.GetSection(HiveDailyRoundOptions.SectionName));
builder.Services.AddOptions<HiveOperationResumeOptions>()
    .Bind(builder.Configuration.GetSection(HiveOperationResumeOptions.SectionName));
builder.Services.AddOptions<HiveProgressionSnapshotOptions>()
    .Bind(builder.Configuration.GetSection(HiveProgressionSnapshotOptions.SectionName));
builder.Services.AddOptions<HiveOfflineProductionOptions>()
    .Bind(builder.Configuration.GetSection(HiveOfflineProductionOptions.SectionName))
    .Validate(options => { options.Validate(); return true; }, "Invalid offline production options")
    .ValidateOnStart();
builder.Services.AddOptions<BuildingUpgradeOptions>()
    .Bind(builder.Configuration.GetSection(BuildingUpgradeOptions.SectionName))
    .Validate(options => { options.Validate(); return true; }, "Invalid building upgrade options")
    .ValidateOnStart();
builder.Services.AddOptions<SpeedUpOptions>()
    .Bind(builder.Configuration.GetSection(SpeedUpOptions.SectionName))
    .Validate(options => { options.Validate(); return true; }, "Invalid SpeedUp options")
    .ValidateOnStart();
builder.Services.AddOptions<RewardLedgerOptions>()
    .Bind(builder.Configuration.GetSection(RewardLedgerOptions.SectionName))
    .Validate(options => { options.Validate(); return true; }, "Invalid reward ledger options")
    .ValidateOnStart();
builder.Services.AddOptions<StrategicPathOptions>().Bind(builder.Configuration.GetSection(StrategicPathOptions.SectionName));
builder.Services.AddOptions<CombatDoctrineOptions>().Bind(builder.Configuration.GetSection(CombatDoctrineOptions.SectionName));
builder.Services.AddOptions<CombatFormationReadinessOptions>().Bind(builder.Configuration.GetSection(CombatFormationReadinessOptions.SectionName));
builder.Services.AddOptions<CombatRecruitmentOptions>().Bind(builder.Configuration.GetSection(CombatRecruitmentOptions.SectionName));
builder.Services.AddOptions<CombatSquadReservationOptions>().Bind(builder.Configuration.GetSection(CombatSquadReservationOptions.SectionName));
builder.Services.AddOptions<HivePerimeterSortieOptions>().Bind(builder.Configuration.GetSection("HivePerimeterSortie"));
builder.Services.AddOptions<WorldResourceCollectionOptions>()
    .Bind(builder.Configuration.GetSection(WorldResourceCollectionOptions.SectionName))
    .Validate(options => { options.Validate(); return true; }, "Invalid world resource collection options")
    .ValidateOnStart();
builder.Services.AddOptions<HiveMilestoneEventOptions>().Bind(builder.Configuration.GetSection(HiveMilestoneEventOptions.SectionName));
builder.Services.AddOptions<CombatPatrolOptions>().Bind(builder.Configuration.GetSection("CombatPatrol"));
builder.Services.AddOptions<WorldMapContentManifestOptions>().Bind(builder.Configuration.GetSection(WorldMapContentManifestOptions.SectionName));

WebApplication app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception)
    {
        string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "unhandled-exceptions.log");
        string entry = $"{DateTimeOffset.UtcNow:O} {context.Request.Method} {context.Request.Path}{context.Request.QueryString}{Environment.NewLine}{exception}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
        try { await File.AppendAllTextAsync(logPath, entry); } catch { }
        app.Logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        if (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { code = "server.unhandled_exception", message = "server.unhandled_exception" });
        }
    }
});

app.Use(async (context, next) =>
{
    // M043J-CL: the Unity client's shared transport enforces this cache boundary on every GET
    // response regardless of domain, not just Hive gameplay - Alliance ("/alliance/v1") was added
    // without extending this middleware, so every AllianceClient GET was rejected client-side with
    // "game.read_cache_boundary_missing" even though the server itself was healthy.
    if ((context.Request.Path.StartsWithSegments("/game/v1") || context.Request.Path.StartsWithSegments("/alliance/v1"))
        && HttpMethods.IsGet(context.Request.Method))
    {
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.Pragma = "no-cache";
    }

    await next();
});

app.MapGet("/runtime/world-map-content-manifest", (HttpContext context, IOptions<WorldMapContentManifestOptions> options) =>
{
    WorldMapContentManifestOptions value = options.Value;
    static IResult Unavailable(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        return GameError(StatusCodes.Status503ServiceUnavailable, "content.unavailable", "content.error.unavailable");
    }
    const long MaxBundleBytes = 512L * 1024 * 1024;
    const long MaxManifestBytes = 2L * 1024 * 1024 * 1024;
    if (!value.Enabled)
        return Unavailable(context);

    if (string.IsNullOrWhiteSpace(value.Channel) || value.Channel.Length > 32
        || string.IsNullOrWhiteSpace(value.Version) || value.Version.Length > 128
        || string.IsNullOrWhiteSpace(value.Platform) || value.Platform.Length > 32
        || string.IsNullOrWhiteSpace(value.MinimumAppVersion) || value.MinimumAppVersion.Length > 64
        || !System.Text.RegularExpressions.Regex.IsMatch(value.Channel, "^[a-z0-9][a-z0-9._-]*$")
        || !System.Text.RegularExpressions.Regex.IsMatch(value.Platform, "^[a-z0-9][a-z0-9._-]*$")
        || value.Bundles.Count == 0 || value.Bundles.Count > 64)
        return Unavailable(context);

    List<WorldMapBundleManifest> bundles = [];
    HashSet<string> bundleIds = new(StringComparer.OrdinalIgnoreCase);
    long totalBytes = 0;
    foreach (WorldMapBundleOptions bundle in value.Bundles)
    {
        if (string.IsNullOrWhiteSpace(bundle.BundleId) || bundle.BundleId.Length > 128
            || !System.Text.RegularExpressions.Regex.IsMatch(bundle.BundleId, "^[a-z0-9][a-z0-9._-]*$")
            || !bundleIds.Add(bundle.BundleId) || bundle.SizeBytes <= 0 || bundle.SizeBytes > MaxBundleBytes
            || !System.Text.RegularExpressions.Regex.IsMatch(bundle.Sha256 ?? "", "^[0-9a-fA-F]{64}$")
            || !Uri.TryCreate(bundle.Uri, UriKind.Absolute, out Uri? uri)
            || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || bundle.Uri.Length > 2048)
            return Unavailable(context);
        try { totalBytes = checked(totalBytes + bundle.SizeBytes); }
        catch (OverflowException) { return Unavailable(context); }
        if (totalBytes > MaxManifestBytes) return Unavailable(context);
        bundles.Add(new WorldMapBundleManifest(bundle.BundleId, bundle.SizeBytes, bundle.Sha256.ToLowerInvariant(), bundle.Uri));
    }

    WorldMapContentManifestResponse response = new("world-map-content-v1", value.Channel, value.Version, value.Platform, value.MinimumAppVersion, bundles);
    string payload = System.Text.Json.JsonSerializer.Serialize(response, BeeKingdom.Shared.Serialization.BeeJson.CreateDefaultOptions());
    string etag = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    context.Response.Headers.ETag = $"\"{etag}\"";
    context.Response.Headers.CacheControl = "public, max-age=60, must-revalidate";
    if (string.Equals(context.Request.Headers.IfNoneMatch.ToString(), $"\"{etag}\"", StringComparison.Ordinal))
        return Results.StatusCode(StatusCodes.Status304NotModified);
    return Results.Ok(response);
});

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/chat/v1")
        && context.RequestServices.GetRequiredService<IOptions<ChatOptions>>().Value is ChatOptions chatOptions)
    {
        int requestTargetBytes = Encoding.UTF8.GetByteCount(context.Request.PathBase + context.Request.Path + context.Request.QueryString);
        if (requestTargetBytes > chatOptions.MaxRequestTargetBytes)
        {
            await ChatError(StatusCodes.Status414UriTooLong, "chat.invalid_request", "chat.error.invalid_request").ExecuteAsync(context);
            return;
        }

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            await next();
            return;
        }

        IHttpMaxRequestBodySizeFeature? bodySize = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySize is not null && bodySize.IsReadOnly == false)
        {
            bodySize.MaxRequestBodySize = chatOptions.MaxRequestBytes;
        }

        if (context.Request.ContentLength > chatOptions.MaxRequestBytes)
        {
            await ChatError(StatusCodes.Status413PayloadTooLarge, "chat.invalid_request", "chat.error.invalid_request").ExecuteAsync(context);
            return;
        }
    }

    await next();
});

app.MapGet("/", () => Results.Redirect("/health"));
app.MapHub<ChatRealtimeHub>("/chat/v1/realtime");

app.MapGet("/health", (IOptions<BeeKingdomServerOptions> options, IOptions<BeeKingdomServerHostProfile> serverProfile, IOptions<ServerIdentityOptions> serverIdentity, IServerClock clock) =>
{
    return Results.Ok(new
    {
        service = options.Value.ServerName,
        status = "Healthy",
        serverTimeUtc = clock.UtcNow,
        gameServerId = NormalizeGuidString(serverIdentity.Value.GameServerId),
        defaultWorldId = NormalizeGuidString(serverIdentity.Value.DefaultWorldId),
        shardName = serverIdentity.Value.ShardName,
        protocolVersion = ProtocolVersion.Current.ToString(),
        hosting = serverProfile.Value.HostingModel,
        sqlServerRole = serverProfile.Value.SqlServerRole
    });
});

app.MapPost("/protocol/ping", (PingRequest request, IOptions<BeeKingdomServerOptions> options, IHostEnvironment environment, IServerClock clock) =>
{
    return Results.Ok(new PingResponse(options.Value.ServerName, ProtocolVersion.Current, clock.UtcNow, environment.EnvironmentName));
});

app.MapPost("/runtime/handshake", (RuntimeHandshakeRequest request, IOptions<BeeKingdomServerOptions> options, IOptions<RuntimeHandshakeOptions> handshake, IOptions<ServerIdentityOptions> serverIdentity, IHostEnvironment environment, IServerClock clock) =>
{
    ProtocolVersion clientVersion = new(request.SupportedProtocolMajor, request.SupportedProtocolMinor);
    bool compatible = clientVersion == ProtocolVersion.Current;
    RuntimeHandshakeOptions handshakeOptions = handshake.Value;
    ServerIdentityOptions identity = serverIdentity.Value;

    return Results.Ok(new RuntimeHandshakeResponse(
        options.Value.ServerName,
        clock.UtcNow,
        environment.EnvironmentName,
        NormalizeGuidString(identity.GameServerId),
        NormalizeGuidString(identity.DefaultWorldId),
        identity.ShardName,
        ProtocolVersion.Current,
        compatible,
        handshakeOptions.Availability,
        handshakeOptions.MaintenanceMessage,
        handshakeOptions.FallbackMode,
        NonGameplay: true,
        GameplayAuthorityGranted: false,
        MutationAllowed: false,
        RequiresAccount: false,
        new RuntimeHandshakeLiveClaims(
            Accounts: false,
            Sessions: false,
            Persistence: false,
            RealTimeSynchronization: false,
            Economy: false,
            Social: false,
            Ranking: false,
            Matchmaking: false)));
});

app.MapGet("/runtime/server-first-readiness", (IOptions<BeeKingdomServerOptions> options, IOptions<ServerFirstReadinessOptions> readiness, IOptions<ServerIdentityOptions> serverIdentity, IHostEnvironment environment, IServerClock clock) =>
{
    ServerFirstReadinessOptions state = readiness.Value;
    ServerIdentityOptions identity = serverIdentity.Value;

    return Results.Ok(new ServerFirstReadinessResponse(
        options.Value.ServerName,
        clock.UtcNow,
        environment.EnvironmentName,
        NormalizeGuidString(identity.GameServerId),
        NormalizeGuidString(identity.DefaultWorldId),
        identity.ShardName,
        state.ProductionTarget,
        state.HandshakePath,
        OfficialServerRequired: true,
        state.ProductionRouteProven,
        state.ProductionRouteStatus,
        state.OfflineMode,
        state.AccountStatus,
        state.SessionStatus,
        state.ColonyReadModelStatus,
        GameplayAuthorityGranted: false,
        MutationAllowed: false,
        BackupRequiredBeforeDeployment: true,
        RollbackRequiresApproval: true,
        SecretsAllowedInReports: false,
        new ServerFirstForbiddenClaims(
            OfflineOfficialPlay: true,
            AccountLive: true,
            SessionLive: true,
            OfficialPersistence: true,
            OfficialProgression: true,
            RealTimeSynchronization: true,
            Economy: true,
            Social: true,
            Ranking: true,
            Matchmaking: true)));
});

app.MapGet("/runtime/account-session-readiness", (IOptions<BeeKingdomServerOptions> options, IOptions<AccountSessionReadinessOptions> readiness, IOptions<ServerIdentityOptions> serverIdentity, IHostEnvironment environment, IServerClock clock, IConfiguration configuration, IOptions<SqlServerOptions> sqlServer) =>
{
    AccountSessionReadinessOptions state = readiness.Value;
    ServerIdentityOptions identity = serverIdentity.Value;
    string provider = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()?.Provider ?? PersistenceOptions.InMemoryProvider;
    bool usesSqlServer = string.Equals(provider, PersistenceOptions.SqlServerProvider, StringComparison.OrdinalIgnoreCase);
    SqlServerOptions sql = sqlServer.Value;
    bool runtimeConnectionConfigured = HasRuntimeSqlConnectionString(configuration, sql);
    bool migrationConnectionConfigured = HasMigrationSqlConnectionString(configuration, sql);
    string[] blockers = BuildAccountSessionReadinessBlockers(state, usesSqlServer, runtimeConnectionConfigured, migrationConnectionConfigured);

    return Results.Ok(new AccountSessionReadinessResponse(
        options.Value.ServerName,
        clock.UtcNow,
        environment.EnvironmentName,
        NormalizeGuidString(identity.GameServerId),
        NormalizeGuidString(identity.DefaultWorldId),
        identity.ShardName,
        state.ProductionTarget,
        provider,
        usesSqlServer,
        runtimeConnectionConfigured,
        migrationConnectionConfigured,
        AccountRepositoryConfigured: true,
        CredentialStoreConfigured: true,
        SessionStoreConfigured: true,
        state.AccountStatus,
        state.SessionStatus,
        state.CredentialStatus,
        state.ColonyReadModelStatus,
        state.AccountCreationAllowed,
        state.SessionCreationAllowed,
        state.TokenIssuanceAllowed,
        state.OfficialPersistenceClaimAllowed,
        state.RequiresProductionRouteProof,
        state.RequiresBackupEvidence,
        state.RequiresRollbackApproval,
        SecretsAllowedInResponse: false,
        new AccountSessionReadinessClaims(
            LiveAccounts: string.Equals(state.AccountStatus, "Live", StringComparison.OrdinalIgnoreCase),
            LiveSessions: string.Equals(state.SessionStatus, "Live", StringComparison.OrdinalIgnoreCase),
            OfficialProgression: false,
            OfficialPersistence: state.OfficialPersistenceClaimAllowed,
            RealTimeSynchronization: false,
            GameplayAuthorityGranted: string.Equals(state.AccountStatus, "Live", StringComparison.OrdinalIgnoreCase)
                && string.Equals(state.SessionStatus, "Live", StringComparison.OrdinalIgnoreCase)),
        blockers));
});

app.MapGet("/runtime/world-map-readiness", (IOptions<BeeKingdomServerOptions> options, IOptions<WorldMapReadinessOptions> readiness, IOptions<ServerIdentityOptions> serverIdentity, IOptions<ServerFirstReadinessOptions> serverFirst, IHostEnvironment environment, IServerClock clock) =>
{
    WorldMapReadinessOptions state = readiness.Value;
    ServerIdentityOptions identity = serverIdentity.Value;
    string[] blockers = BuildWorldMapReadinessBlockers(state, serverFirst.Value.ProductionRouteProven);

    return Results.Ok(new WorldMapReadinessResponse(
        options.Value.ServerName,
        clock.UtcNow,
        environment.EnvironmentName,
        NormalizeGuidString(identity.GameServerId),
        NormalizeGuidString(identity.DefaultWorldId),
        identity.ShardName,
        state.ProductionTarget,
        state.WorldMapStatus,
        state.WorldMapBoundary,
        ReadOnly: true,
        NonLive: true,
        state.ProductionRouteProven,
        state.MapGameplayEnabled,
        state.LiveTerritoryEnabled,
        state.LiveAllianceEnabled,
        state.LiveScoutingEnabled,
        state.LiveWarEnabled,
        state.LiveEconomyEnabled,
        state.RealTimeSynchronizationEnabled,
        state.OfficialProgressionEnabled,
        BuildWorldMapNodeModels(),
        new WorldMapForbiddenClaims(
            LiveWorldMap: true,
            OfficialTerritory: true,
            ActiveAlliance: true,
            LiveScouting: true,
            LiveFlightPath: true,
            LiveWar: true,
            LivePvp: true,
            LiveEconomy: true,
            Ranking: true,
            Matchmaking: true,
            RealTimeSynchronization: true),
        blockers));
});

app.MapGet("/runtime/world-registry-readiness", (IOptions<BeeKingdomServerOptions> options, IOptions<WorldRegistryReadinessOptions> readiness, IOptions<ServerIdentityOptions> serverIdentity, IOptions<ServerFirstReadinessOptions> serverFirst, IHostEnvironment environment, IServerClock clock) =>
{
    WorldRegistryReadinessOptions state = readiness.Value;
    ServerIdentityOptions identity = serverIdentity.Value;
    string gameServerId = NormalizeGuidString(identity.GameServerId);
    string defaultWorldId = NormalizeGuidString(identity.DefaultWorldId);
    string[] blockers = BuildWorldRegistryReadinessBlockers(state, serverFirst.Value.ProductionRouteProven);

    return Results.Ok(new WorldRegistryReadinessResponse(
        options.Value.ServerName,
        clock.UtcNow,
        environment.EnvironmentName,
        gameServerId,
        defaultWorldId,
        identity.ShardName,
        state.ProductionTarget,
        state.RegistryStatus,
        ReadOnly: true,
        NonLive: true,
        state.ProductionRouteProven,
        state.WorldSelectionEnabled,
        state.WorldCreationEnabled,
        state.WorldTransferEnabled,
        state.WorldMergeEnabled,
        state.LivePopulationEnabled,
        BuildWorldCapacityPolicy(state),
        BuildWorldRegistryEntries(state, gameServerId, defaultWorldId),
        new WorldRegistryForbiddenClaims(
            LiveWorldSelection: true,
            LivePopulation: true,
            AutoWorldCreation: true,
            WorldTransfer: true,
            WorldMerge: true,
            CrossServerGameplay: true,
            Ranking: true,
            Matchmaking: true,
            OfficialProgression: true),
        blockers));
});

app.MapGet("/runtime/world-identity-readiness", (IOptions<BeeKingdomServerOptions> options, IOptions<ServerIdentityOptions> serverIdentity, IHostEnvironment environment, IServerClock clock) =>
{
    ServerIdentityOptions identity = serverIdentity.Value;
    bool gameServerIdValid = Guid.TryParse(identity.GameServerId, out Guid gameServerId);
    bool defaultWorldIdValid = Guid.TryParse(identity.DefaultWorldId, out Guid defaultWorldId);
    bool identifiersDistinct = gameServerIdValid && defaultWorldIdValid && gameServerId != defaultWorldId;
    string[] blockers = BuildWorldIdentityReadinessBlockers(gameServerIdValid, defaultWorldIdValid, identifiersDistinct);

    return Results.Ok(new WorldIdentityReadinessResponse(
        options.Value.ServerName,
        clock.UtcNow,
        environment.EnvironmentName,
        NormalizeGuidString(identity.GameServerId),
        NormalizeGuidString(identity.DefaultWorldId),
        identity.ShardName,
        gameServerIdValid,
        defaultWorldIdValid,
        identifiersDistinct,
        RequiresWorldScopeForAccounts: true,
        RequiresWorldScopeForColonies: true,
        RequiresWorldScopeForWorldMap: true,
        SingleWorldAssumptionAllowed: false,
        LiveWorldSelectionAllowed: false,
        OfficialProgressionAllowed: false,
        BuildWorldIdentityScopes(),
        blockers));
});

app.MapGet("/runtime/chat-readiness", (ChatManager chat) => Results.Ok(chat.GetReadiness()));

app.MapGet("/game/v1/hives/{hiveId}/hive-stock", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<HiveStockSnapshotOptions> configured, IOptions<HiveDailyRoundOptions> daily, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    try { configured.Value.Validate(); } catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    var state = await repository.ReadAsync(auth.PlayerId!.Value, parsed, ct); if (state is null) return GameError(404, "game.hive_not_found", "game.error.not_found");
    try { if (daily.Value.Enabled) { var mark = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>()).RecordSnapshotReadAsync(auth.PlayerId.Value, parsed, ct); if (!mark.Succeeded) return GameError(503, "game.unavailable", "game.error.unavailable"); state = mark.State; } var snapshot = HiveStockSnapshotFactory.FromAuthoritativeState(state, configured.Value.CatalogVersion, clock.UtcNow); return Results.Ok(snapshot); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});

app.MapGet("/game/v1/hives/{hiveId}/daily-round", async (HttpContext context,string hiveId,AuthenticationManager authentication,IHiveStateRepository repository,BeeKingdom.HiveOperations.IServerClock clock,IOptions<HiveDailyRoundOptions> configured,CancellationToken ct)=>
{
 if(!configured.Value.Enabled)return GameError(503,"game.unavailable","game.error.unavailable");var auth=AuthenticateGameRequest(context,authentication);if(!auth.IsValid)return GameError(401,"game.session_required","game.error.session_required");if(!TryParseGameResourceId(hiveId,out Guid hive))return GameError(400,"game.invalid_request","game.error.invalid_request");var state=await repository.ReadAsync(auth.PlayerId!.Value,hive,ct);if(state is null)return GameError(404,"game.hive_not_found","game.error.not_found");var now=clock.UtcNow;var day=new DateTimeOffset(now.UtcDateTime.Date,TimeSpan.Zero);var round=state.DailyRound is { } r&&r.DayUtc==day?r:new(day,false,false,false,null);var facts=new Dictionary<string,bool>{{"collection_received",round.CollectionReceived},{"operation_launched",round.OperationLaunched},{"snapshot_read",round.SnapshotRead}};return Results.Ok(new HiveDailyRoundSnapshot(state.PlayerId,state.HiveId,"living-hive-daily-round-v1",day,day.AddDays(1),now,state.Revision,facts,facts.Values.Count(x=>x),120,60, facts.Values.All(x=>x)&&round.ClaimedAtUtc is null,round.ClaimedAtUtc));
});
app.MapPost("/game/v1/hives/{hiveId}/daily-round/claim", async (HttpContext context,string hiveId,AuthenticationManager authentication,IHiveStateRepository repository,BeeKingdom.HiveOperations.IServerClock clock,IOptions<HiveDailyRoundOptions> configured,HiveDailyRoundClaimRequest request,CancellationToken ct)=>
{
 if(!configured.Value.Enabled)return GameError(503,"game.unavailable","game.error.unavailable");var auth=AuthenticateGameRequest(context,authentication);if(!auth.IsValid)return GameError(401,"game.session_required","game.error.session_required");if(!TryParseGameResourceId(hiveId,out Guid hive)||request is null||request.ExpectedRevision<0||request.ExpectedRevision==long.MaxValue||string.IsNullOrWhiteSpace(request.IdempotencyKey)||request.IdempotencyKey.Trim()!=request.IdempotencyKey||request.IdempotencyKey.Length>256||!DateOnly.TryParseExact(request.ExpectedDayUtc,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out _))return GameError(400,"game.invalid_request","game.error.invalid_request");var result=await new HiveOperationService(repository,clock,Array.Empty<BuildingOperationDefinition>()).ClaimDailyRoundAsync(new(auth.PlayerId!.Value,hive,request.ExpectedRevision,request.IdempotencyKey,request.ExpectedDayUtc),ct);if(!result.Succeeded)return GameError(409,"game."+result.Code,"game.error.conflict");var s=result.State;var now=clock.UtcNow;var round=s.DailyRound!;var facts=new Dictionary<string,bool>{{"collection_received",round.CollectionReceived},{"operation_launched",round.OperationLaunched},{"snapshot_read",round.SnapshotRead}};if(s.DailyRoundReceipts is null||!s.DailyRoundReceipts.TryGetValue(request.IdempotencyKey,out var stored))return GameError(503,"game.unavailable","game.error.unavailable");var snapshot=new HiveDailyRoundSnapshot(s.PlayerId,s.HiveId,"living-hive-daily-round-v1",stored.DayUtc,stored.DayUtc.AddDays(1),now,s.Revision,facts,facts.Values.Count(x=>x),120,60, false,round.ClaimedAtUtc);var receipt=new HiveDailyRoundClaimReceipt(s.PlayerId,s.HiveId,request.IdempotencyKey,stored.DayUtc,stored.RevisionBefore,stored.RevisionAfter,stored.AcceptedAtUtc,stored.CreditedHoney,stored.CreditedPollen,stored.Code.StartsWith("game.",StringComparison.Ordinal)?stored.Code:"game."+stored.Code);return Results.Ok(new HiveDailyRoundClaimResponse(receipt,snapshot));
});

app.MapPost("/game/v1/hives/{hiveId}/ensure", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, CancellationToken ct) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    PlayerHiveState state = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>()).EnsureAsync(auth.PlayerId!.Value, hive, ct);
    return Results.Ok(new { revision = state.Revision });
});

app.MapGet("/game/v1/hives/{hiveId}/offline-production", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<HiveOfflineProductionOptions> configured, IOptions<HiveDailyRoundOptions> daily, IAllianceGameplayBonusResolver allianceBonusResolver, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsedHive)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { configured.Value.Validate(); var service = new HiveOfflineProductionService(repository, clock, configured.Value, daily.Value.Enabled, allianceBonusResolver); return Results.Ok(await service.ReadSnapshotAsync(auth.PlayerId.Value, parsedHive, ct)); }
    catch (ArgumentException) { return GameError(400, "game.invalid_request", "game.error.invalid_request"); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
    catch (InvalidOperationException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});

app.MapPost("/game/v1/hives/{hiveId}/offline-production/{buildingKey}/collect", async (HttpContext context, string hiveId, string buildingKey, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<HiveOfflineProductionOptions> configured, IOptions<HiveDailyRoundOptions> daily, IAllianceGameplayBonusResolver allianceBonusResolver, CollectOfflineProductionRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsedHive) || string.IsNullOrWhiteSpace(buildingKey) || buildingKey.Trim() != buildingKey || buildingKey.Length > 256 || request is null || request.ExpectedProductionRevision < 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim() != request.IdempotencyKey || request.IdempotencyKey.Length > 256)
        return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try
    {
        configured.Value.Validate(); var service = new HiveOfflineProductionService(repository, clock, configured.Value, daily.Value.Enabled, allianceBonusResolver); var result = await service.CollectAsync(auth.PlayerId.Value, parsedHive, buildingKey, request, ct);
        if (result.Succeeded) return Results.Ok(result.Response);
        return result.Code switch { "game.invalid_request" => GameError(400, result.Code, "game.error.invalid_request"), _ => GameError(409, result.Code, "game.error.conflict") };
    }
    catch (ArgumentException) { return GameError(400, "game.invalid_request", "game.error.invalid_request"); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
    catch (InvalidOperationException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});

app.MapGet("/game/v1/hives/{hiveId}/research", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<LivingHiveResearchOptions> configured, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503,"game.unavailable","game.error.unavailable");
    TokenValidationResult auth=AuthenticateGameRequest(context,authentication); if(!auth.IsValid)return GameError(401,"game.session_required","game.error.session_required");
    if(!TryParseGameResourceId(hiveId,out Guid hive))return GameError(400,"game.invalid_request","game.error.invalid_request");
    var state=await repository.ReadAsync(auth.PlayerId!.Value,hive,ct); if(state is null)return GameError(404,"game.research_not_found","game.error.not_found");
    return Results.Ok(BuildResearchSnapshot(state,clock.UtcNow,configured.Value.CatalogVersion,configured.Value.Catalog));
});
app.MapPost("/game/v1/hives/{hiveId}/research/{researchId}/start", async (HttpContext context,string hiveId,string researchId,AuthenticationManager authentication,IHiveStateRepository repository,BeeKingdom.HiveOperations.IServerClock clock,IOptions<LivingHiveResearchOptions> configured,IOptions<HiveDailyRoundOptions> daily,ResearchMutationRequest request,CancellationToken ct)=>
{
    if(!configured.Value.Enabled)return GameError(503,"game.unavailable","game.error.unavailable"); TokenValidationResult auth=AuthenticateGameRequest(context,authentication);if(!auth.IsValid)return GameError(401,"game.session_required","game.error.session_required");if(!TryParseGameResourceId(hiveId,out Guid hive)||request is null)return GameError(400,"game.invalid_request","game.error.invalid_request");
    if(request is null||request.ExpectedRevision<0||string.IsNullOrWhiteSpace(request.IdempotencyKey)||request.IdempotencyKey.Trim()!=request.IdempotencyKey||request.IdempotencyKey.Length>256||!configured.Value.Catalog.Contains(researchId,StringComparer.Ordinal))return GameError(400,"game.invalid_request","game.error.invalid_request");var result=await new HiveOperationService(repository,clock,Array.Empty<BuildingOperationDefinition>(),null,null,daily.Value.Enabled).StartResearchAsync(new(auth.PlayerId!.Value,hive,researchId,request.ExpectedRevision,request.IdempotencyKey),ct); if(!result.Succeeded)return GameError(result.Code=="invalid_request"?400:409,"game."+result.Code,"game.error.conflict");return Results.Ok(new ResearchResponse(new(auth.PlayerId.Value,hive,request.IdempotencyKey,result.OperationId!.Value,result.ResearchId,result.RevisionAfter,result.AcceptedAtUtc,"game.research_started"),BuildResearchSnapshot(result.State,clock.UtcNow,configured.Value.CatalogVersion,configured.Value.Catalog)));
});
app.MapPost("/game/v1/hives/{hiveId}/research/{operationId}/complete", async (HttpContext context,string hiveId,string operationId,AuthenticationManager authentication,IHiveStateRepository repository,BeeKingdom.HiveOperations.IServerClock clock,IOptions<LivingHiveResearchOptions> configured,IOptions<HiveDailyRoundOptions> daily,ResearchMutationRequest request,CancellationToken ct)=>
{
    if(!configured.Value.Enabled)return GameError(503,"game.unavailable","game.error.unavailable");TokenValidationResult auth=AuthenticateGameRequest(context,authentication);if(!auth.IsValid)return GameError(401,"game.session_required","game.error.session_required");if(!TryParseGameResourceId(hiveId,out Guid hive)||!Guid.TryParse(operationId,out Guid op)||request is null)return GameError(400,"game.invalid_request","game.error.invalid_request");if(request.ExpectedRevision<0||string.IsNullOrWhiteSpace(request.IdempotencyKey)||request.IdempotencyKey.Trim()!=request.IdempotencyKey||request.IdempotencyKey.Length>256)return GameError(400,"game.invalid_request","game.error.invalid_request");var result=await new HiveOperationService(repository,clock,Array.Empty<BuildingOperationDefinition>()).CompleteResearchAsync(new(auth.PlayerId!.Value,hive,op,request.ExpectedRevision,request.IdempotencyKey),ct);if(!result.Succeeded)return GameError(409,"game."+result.Code,"game.error.conflict");var state=result.State;return Results.Ok(new ResearchResponse(new(auth.PlayerId.Value,hive,request.IdempotencyKey,op,result.ResearchId,result.RevisionAfter,result.AcceptedAtUtc,"game.research_completed"),BuildResearchSnapshot(state,clock.UtcNow,configured.Value.CatalogVersion,configured.Value.Catalog)));
});

// M037 — FTUE Tutorial persistence (chapter/step, idempotent, no SQL migration)
app.MapGet("/game/v1/hives/{hiveId}/tutorial", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, CancellationToken ct) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    PlayerHiveState state = await repository.ReadAsync(auth.PlayerId!.Value, hive, ct);
    if (state is null) return GameError(404, "game.hive_not_found", "game.error.not_found");
    var t = state.Tutorial;
    return Results.Ok(new TutorialProgressResponse(t?.ChapterKey ?? string.Empty, t?.SafeResumeStepKey ?? string.Empty, t?.LastObservedStepKey ?? string.Empty, t?.UpdatedAtUtc ?? DateTimeOffset.MinValue, state.Revision));
});
app.MapPost("/game/v1/hives/{hiveId}/tutorial/progress", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, SaveTutorialProgressHttpRequest request, CancellationToken ct) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive) || request is null) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    if (request.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim() != request.IdempotencyKey || request.IdempotencyKey.Length > 256) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    string chapter = request.ChapterKey?.Trim() ?? string.Empty;
    string safe = request.SafeResumeStepKey?.Trim() ?? string.Empty;
    string last = request.LastObservedStepKey?.Trim() ?? string.Empty;
    if (chapter.Length > 128 || safe.Length > 128 || last.Length > 128) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    var result = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>()).SaveTutorialProgressAsync(new(auth.PlayerId!.Value, hive, request.ExpectedRevision, chapter, safe, last, request.IdempotencyKey), ct);
    if (!result.Succeeded) return GameError(result.Code == "invalid_request" ? 400 : 409, "game." + result.Code, "game.error.conflict");
    var nt = result.State.Tutorial;
    return Results.Ok(new TutorialProgressResponse(nt?.ChapterKey ?? string.Empty, nt?.SafeResumeStepKey ?? string.Empty, nt?.LastObservedStepKey ?? string.Empty, nt?.UpdatedAtUtc ?? clock.UtcNow, result.State.Revision));
});

app.MapGet("/game/v1/hives/{hiveId}/champion-bees", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<ChampionBeeProgressionOptions> configured, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    PlayerHiveState? state = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>()).ReadAsync(auth.PlayerId!.Value, hive, ct);
    if (state is null) return GameError(404, "game.hive_not_found", "game.error.not_found");
    int coeurRoyalLevel = state.BuildingLevels.GetValueOrDefault("administration_core", 1);
    return Results.Ok(new ChampionBeeSnapshotResponse(
        state.ChampionBees?.Levels ?? new Dictionary<string, int>(StringComparer.Ordinal),
        state.ChampionBees?.AssignedBeeIds ?? new List<string>(),
        BeeKingdom.HiveOperations.ChampionBeeCatalog.MaxAssignedForCoeurRoyalLevel(coeurRoyalLevel),
        state.Revision));
});
app.MapPost("/game/v1/hives/{hiveId}/champion-bees/{beeId}/grant", async (HttpContext context, string hiveId, string beeId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<ChampionBeeProgressionOptions> configured, ChampionBeeMutationHttpRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive) || request is null || request.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256)
        return GameError(400, "game.invalid_request", "game.error.invalid_request");
    ChampionBeeCommandResult result = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>())
        .GrantChampionBeeAsync(new(auth.PlayerId!.Value, hive, beeId, request.ExpectedRevision, request.IdempotencyKey), ct);
    if (!result.Succeeded) return GameError(result.Code == "invalid_request" ? 400 : 409, "game." + result.Code, "game.error.conflict");
    return Results.Ok(new ChampionBeeMutationResponse(true, result.Code, result.BeeId, result.Level, result.AssignedBeeIds.ToList(), result.RevisionAfter));
});
app.MapPost("/game/v1/hives/{hiveId}/champion-bees/{beeId}/level-up", async (HttpContext context, string hiveId, string beeId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<ChampionBeeProgressionOptions> configured, ChampionBeeMutationHttpRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive) || request is null || request.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256)
        return GameError(400, "game.invalid_request", "game.error.invalid_request");
    ChampionBeeCommandResult result = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>())
        .LevelUpChampionBeeAsync(new(auth.PlayerId!.Value, hive, beeId, request.ExpectedRevision, request.IdempotencyKey), ct);
    if (!result.Succeeded) return GameError(result.Code == "invalid_request" ? 400 : 409, "game." + result.Code, "game.error.conflict");
    return Results.Ok(new ChampionBeeMutationResponse(true, result.Code, result.BeeId, result.Level, result.AssignedBeeIds.ToList(), result.RevisionAfter));
});
app.MapPost("/game/v1/hives/{hiveId}/champion-bees/assignment", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<ChampionBeeProgressionOptions> configured, SetChampionBeeAssignmentHttpRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive) || request is null || request.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256)
        return GameError(400, "game.invalid_request", "game.error.invalid_request");
    ChampionBeeCommandResult result = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>())
        .SetChampionBeeAssignmentAsync(new(auth.PlayerId!.Value, hive, request.BeeIds ?? new List<string>(), request.ExpectedRevision, request.IdempotencyKey), ct);
    if (!result.Succeeded) return GameError(result.Code == "invalid_request" ? 400 : 409, "game." + result.Code, "game.error.conflict");
    return Results.Ok(new ChampionBeeMutationResponse(true, result.Code, result.BeeId, result.Level, result.AssignedBeeIds.ToList(), result.RevisionAfter));
});

app.MapGet("/game/v1/hives/{hiveId}/troop-tiers", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<ChampionBeeProgressionOptions> configured, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    PlayerHiveState? state = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>()).ReadAsync(auth.PlayerId!.Value, hive, ct);
    if (state is null) return GameError(404, "game.hive_not_found", "game.error.not_found");
    return Results.Ok(new TroopTierSnapshotResponse(state.TroopTierProgress?.Tiers ?? new Dictionary<string, int>(StringComparer.Ordinal), state.Revision));
});
app.MapPost("/game/v1/hives/{hiveId}/troop-tiers/{populationId}/promote", async (HttpContext context, string hiveId, string populationId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<ChampionBeeProgressionOptions> configured, PromoteTroopTierHttpRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive) || request is null || request.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256)
        return GameError(400, "game.invalid_request", "game.error.invalid_request");
    TroopTierCommandResult result = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>())
        .PromoteTroopTierAsync(new(auth.PlayerId!.Value, hive, populationId, request.ExpectedRevision, request.IdempotencyKey), ct);
    if (!result.Succeeded) return GameError(result.Code == "invalid_request" ? 400 : 409, "game." + result.Code, "game.error.conflict");
    return Results.Ok(new TroopTierMutationResponse(true, result.Code, result.PopulationId, result.Tier, result.RevisionAfter));
});

app.MapGet("/game/v1/hives/{hiveId}/vip", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<VipProgressionOptions> configured, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    PlayerHiveState? state = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>()).ReadAsync(auth.PlayerId!.Value, hive, ct);
    if (state is null) return GameError(404, "game.hive_not_found", "game.error.not_found");
    long lifetimePoints = state.Vip?.LifetimePoints ?? 0;
    int level = BeeKingdom.HiveOperations.VipCatalog.LevelForPoints(lifetimePoints);
    return Results.Ok(new VipSnapshotResponse(lifetimePoints, level, BeeKingdom.HiveOperations.VipCatalog.NextThreshold(level), BeeKingdom.HiveOperations.VipCatalog.CapacityBonusBps(level), state.Revision));
});
app.MapPost("/dev/hives/{hiveId}/grant-vip-points", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IHostEnvironment environment, IOptions<DevToolsOptions> devTools, GrantVipPointsHttpRequest request, CancellationToken ct) =>
{
    if (environment.IsProduction() || !devTools.Value.AllowDevAccountSeeding) return Results.NotFound();
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive) || request is null || request.Points <= 0 || request.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256)
        return GameError(400, "game.invalid_request", "game.error.invalid_request");
    VipCommandResult result = await new HiveOperationService(repository, clock, Array.Empty<BuildingOperationDefinition>())
        .GrantVipPointsAsync(new(auth.PlayerId!.Value, hive, request.Points, request.ExpectedRevision, request.IdempotencyKey, "dev_grant"), ct);
    if (!result.Succeeded) return GameError(result.Code == "invalid_request" ? 400 : 409, "game." + result.Code, "game.error.conflict");
    return Results.Ok(new VipSnapshotResponse(result.LifetimePoints, result.Level, BeeKingdom.HiveOperations.VipCatalog.NextThreshold(result.Level), BeeKingdom.HiveOperations.VipCatalog.CapacityBonusBps(result.Level), result.RevisionAfter));
});

// Local-dev-only helper (meme garde que grant-vip-points) : fixe directement le niveau
// d'un batiment, pour tester des systemes dont le declencheur (ex. Voie strategique,
// palier de batiment >= 10) n'est pas encore atteignable via le contenu reel du
// catalogue d'amelioration. Jamais accessible en production.
app.MapPost("/dev/hives/{hiveId}/set-building-level", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, IHostEnvironment environment, IOptions<DevToolsOptions> devTools, SetBuildingLevelHttpRequest request, CancellationToken ct) =>
{
    if (environment.IsProduction() || !devTools.Value.AllowDevAccountSeeding) return Results.NotFound();
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive) || request is null || string.IsNullOrWhiteSpace(request.BuildingKey) || request.Level < 1)
        return GameError(400, "game.invalid_request", "game.error.invalid_request");
    PlayerHiveState updated = await repository.ExecuteAtomicallyAsync(auth.PlayerId!.Value, hive, state =>
    {
        Dictionary<string, int> levels = new(state.BuildingLevels) { [request.BuildingKey] = request.Level };
        return state with { BuildingLevels = levels };
    }, ct);
    return Results.Ok(new { buildingKey = request.BuildingKey, level = updated.BuildingLevels.GetValueOrDefault(request.BuildingKey, 1), revision = updated.Revision });
});

// Local-dev-only helper (meme garde que set-building-level/grant-vip-points) : credite directement
// une ressource, pour tester des systemes dont les couts reels (recrutement, ameliorations) ne
// sont pas atteignables depuis un compte de test fraichement cree. Jamais accessible en production.
app.MapPost("/dev/hives/{hiveId}/grant-resource", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, IHostEnvironment environment, IOptions<DevToolsOptions> devTools, GrantResourceHttpRequest request, CancellationToken ct) =>
{
    if (environment.IsProduction() || !devTools.Value.AllowDevAccountSeeding) return Results.NotFound();
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid hive) || request is null || string.IsNullOrWhiteSpace(request.ResourceKey) || request.Amount < 0)
        return GameError(400, "game.invalid_request", "game.error.invalid_request");
    PlayerHiveState updated = await repository.ExecuteAtomicallyAsync(auth.PlayerId!.Value, hive, state =>
    {
        Dictionary<string, ResourceBalance> resources = new(state.Resources);
        ResourceBalance current = resources.GetValueOrDefault(request.ResourceKey, new ResourceBalance(0, 1_000_000_000));
        resources[request.ResourceKey] = current with { Amount = current.Amount + request.Amount };
        return state with { Resources = resources };
    }, ct);
    return Results.Ok(new { resourceKey = request.ResourceKey, amount = updated.Resources.GetValueOrDefault(request.ResourceKey, new ResourceBalance(0, 0)).Amount, revision = updated.Revision });
});

app.MapGet("/game/v1/hives/{hiveId}/building-upgrades", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<BuildingUpgradeOptions> configured, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { return Results.Ok(await new BuildingUpgradeService(repository, clock, configured.Value).ReadAsync(auth.PlayerId!.Value, parsed, ct)); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
    catch (InvalidOperationException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});
app.MapPost("/game/v1/hives/{hiveId}/building-upgrades/{buildingKey}/start", async (HttpContext context, string hiveId, string buildingKey, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<BuildingUpgradeOptions> configured, IOptions<HiveDailyRoundOptions> daily, StartBuildingUpgradeRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed) || string.IsNullOrWhiteSpace(buildingKey) || request is null) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { var result=await new BuildingUpgradeService(repository, clock, configured.Value, daily.Value.Enabled).StartAsync(auth.PlayerId!.Value, parsed, buildingKey, request, ct); return result.Succeeded ? Results.Ok(result.Response) : GameError(result.Code=="game.invalid_request"?400:409,result.Code,"game.error.conflict"); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});
app.MapPost("/game/v1/hives/{hiveId}/building-upgrades/{operationId}/complete", async (HttpContext context, string hiveId, string operationId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<BuildingUpgradeOptions> configured, CompleteBuildingUpgradeRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed) || !Guid.TryParse(operationId, out Guid op) || request is null) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { var result=await new BuildingUpgradeService(repository, clock, configured.Value).CompleteAsync(auth.PlayerId!.Value, parsed, op, request, ct); return result.Succeeded ? Results.Ok(result.Response) : GameError(result.Code=="game.invalid_request"?400:409,result.Code,"game.error.conflict"); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});

app.MapGet("/game/v1/hives/{hiveId}/speedups", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<SpeedUpOptions> configured, CancellationToken ct) =>
    await ReadSpeedUps(context, hiveId, authentication, repository, clock, configured, ct));
app.MapPost("/game/v1/hives/{hiveId}/speedups/apply", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<SpeedUpOptions> configured, ApplySpeedUpRequest request, CancellationToken ct) =>
    await ApplySpeedUp(context, hiveId, string.Empty, authentication, repository, clock, configured, request, ct));
app.MapPost("/game/v1/hives/{hiveId}/speedups/{category}/apply", async (HttpContext context, string hiveId, string category, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<SpeedUpOptions> configured, ApplySpeedUpRequest request, CancellationToken ct) =>
    await ApplySpeedUp(context, hiveId, category, authentication, repository, clock, configured, request, ct));

app.MapPost("/game/v1/hives/{hiveId}/chapter-1/foundation", async (
    HttpContext context,
    AuthenticationManager authentication,
    HiveOperationService hiveOperations,
    IOptions<FoundationDotationOptions> foundationOptions,
    string hiveId,
    ClaimFoundationDotationHttpRequest request,
    CancellationToken cancellationToken) =>
{
    if (!foundationOptions.Value.Enabled)
        return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid)
        return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsedHiveId))
        return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    if (string.IsNullOrWhiteSpace(request.Choice) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
        return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");

    HiveCommandResult result = await hiveOperations.ClaimFoundationDotationAsync(new(
        auth.PlayerId.Value,
        parsedHiveId,
        request.ExpectedRevision,
        request.Choice,
        request.IdempotencyKey), cancellationToken);
    return result.Code switch
    {
        "foundation_claimed" => Results.Ok(ToFoundationResponse(result)),
        "idempotency_conflict" => GameError(StatusCodes.Status409Conflict, "game.idempotency_conflict", "game.error.idempotency_conflict"),
        "invalid_foundation_choice" => GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request"),
        "revision_conflict" or "installation_incomplete" or "foundation_already_claimed" or "storage_capacity_insufficient" =>
            GameError(StatusCodes.Status409Conflict, "game.foundation_conflict", "game.error.foundation_conflict"),
        _ => GameError(StatusCodes.Status409Conflict, "game.foundation_conflict", "game.error.foundation_conflict")
    };
});

app.MapGet("/game/v1/hives/{hiveId}/brood/vitality", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<BroodVitalityOptions> options, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsedHiveId)) return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    PlayerHiveState? state = await repository.ReadAsync(auth.PlayerId.Value, parsedHiveId, ct);
    if (state is null) return GameError(StatusCodes.Status404NotFound, "game.not_found", "game.error.not_found");
    BroodVitalityState? vitality = state.BroodVitality;
    var snapshot = new BroodVitalityCareSnapshot(state.PlayerId, state.HiveId, BroodVitalityCareService.ContractVersion, clock.UtcNow, state.Revision, vitality);
    return Results.Ok(snapshot);
});

app.MapPost("/game/v1/hives/{hiveId}/brood/vitality/care/start", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<BroodVitalityOptions> options, IOptions<HiveDailyRoundOptions> daily, BroodVitalityCareRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable"); var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required"); if (!TryParseGameResourceId(hiveId, out var hive)) return GameError(400, "game.invalid_request", "game.error.invalid_request"); if (request is null) return GameError(400, "game.invalid_request", "game.error.invalid_request"); var type = context.Request.Query["type"].ToString(); try { var r = await new BroodVitalityCareService(repository, clock, daily.Value.Enabled).StartAsync(auth.PlayerId!.Value, hive, type, request, ct); return r.Succeeded && r.Receipt is not null ? Results.Ok(new BroodVitalityCareResponse(r.Receipt, new(auth.PlayerId!.Value, hive, BroodVitalityCareService.ContractVersion, clock.UtcNow, r.State.Revision, r.State.BroodVitality))) : GameError(409, r.Code, "game.error.conflict"); } catch (ArgumentException) { return GameError(400, "game.invalid_request", "game.error.invalid_request"); }
});

app.MapPost("/game/v1/hives/{hiveId}/brood/vitality/care/{operationId}/complete", async (HttpContext context, string hiveId, string operationId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<BroodVitalityOptions> options, BroodVitalityCareRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable"); var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required"); if (!TryParseGameResourceId(hiveId, out var hive) || !Guid.TryParse(operationId, out var op)) return GameError(400, "game.invalid_request", "game.error.invalid_request"); try { var r = await new BroodVitalityCareService(repository, clock).CompleteAsync(auth.PlayerId!.Value, hive, op, request, ct); return r.Succeeded && r.Receipt is not null ? Results.Ok(new BroodVitalityCareResponse(r.Receipt, new(auth.PlayerId!.Value, hive, BroodVitalityCareService.ContractVersion, clock.UtcNow, r.State.Revision, r.State.BroodVitality))) : GameError(409, r.Code, "game.error.conflict"); } catch (ArgumentException) { return GameError(400, "game.invalid_request", "game.error.invalid_request"); }
});

app.MapGet("/game/v1/hives/{hiveId}/strategic-path", async (HttpContext context, string hiveId, AuthenticationManager authentication, StrategicPathService strategic, IOptions<StrategicPathOptions> options, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsedHiveId)) return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    try { return Results.Ok(await strategic.ReadSnapshotAsync(auth.PlayerId!.Value, parsedHiveId, ct)); }
    catch (KeyNotFoundException) { return GameError(StatusCodes.Status404NotFound, "game.not_found", "game.error.not_found"); }
});

app.MapGet("/game/v1/combat/doctrine", (HttpContext context, AuthenticationManager authentication, CombatDoctrineService doctrine, IOptions<CombatDoctrineOptions> options) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    return Results.Ok(doctrine.GetSnapshot());
});

app.MapGet("/game/v1/hives/{hiveId}/combat/formation-readiness", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, CombatFormationReadinessService readiness, IOptions<CombatFormationReadinessOptions> options, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsedHiveId)) return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    PlayerHiveState? state = await repository.ReadAsync(auth.PlayerId!.Value, parsedHiveId, ct);
    if (state is null) return GameError(StatusCodes.Status404NotFound, "game.not_found", "game.error.not_found");
    return Results.Ok(readiness.FromAuthoritativeState(state));
});

app.MapGet("/game/v1/hives/{hiveId}/combat/recruitment", async (HttpContext context, string hiveId, AuthenticationManager authentication, CombatRecruitmentService recruitment, IOptions<CombatRecruitmentOptions> options, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid id)) return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    try { return Results.Ok(await recruitment.ReadAsync(auth.PlayerId!.Value, id, ct)); } catch (KeyNotFoundException) { return GameError(StatusCodes.Status404NotFound, "game.not_found", "game.error.not_found"); }
});

app.MapPost("/game/v1/hives/{hiveId}/combat/recruitment/start", async (HttpContext context, string hiveId, AuthenticationManager authentication, CombatRecruitmentService recruitment, IOptions<CombatRecruitmentOptions> options, DoctrineRecruitmentStartRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out Guid id) || string.IsNullOrWhiteSpace(request.Family) || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256 || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue) return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    try { DoctrineRecruitmentResult result = await recruitment.StartAsync(new(auth.PlayerId!.Value, id, request.Family, request.ExpectedRevision, request.IdempotencyKey), ct); return result.Succeeded && result.Receipt is not null ? Results.Ok(new DoctrineRecruitmentResponse(result.Receipt, result.Snapshot)) : GameError(StatusCodes.Status409Conflict, result.Code, "game.error.recruitment_conflict"); } catch (ArgumentException) { return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request"); }
});

app.MapPost("/game/v1/hives/{hiveId}/combat/recruitment/{operationId:guid}/claim", async (HttpContext context, string hiveId, Guid operationId, AuthenticationManager authentication, CombatRecruitmentService recruitment, IOptions<CombatRecruitmentOptions> options, DoctrineRecruitmentClaimRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out Guid id) || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256 || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue) return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    try { DoctrineRecruitmentResult result = await recruitment.ClaimAsync(new(auth.PlayerId!.Value, id, operationId, request.ExpectedRevision, request.IdempotencyKey), ct); return result.Succeeded && result.Receipt is not null ? Results.Ok(new DoctrineRecruitmentResponse(result.Receipt, result.Snapshot)) : GameError(StatusCodes.Status409Conflict, result.Code, "game.error.recruitment_conflict"); } catch (ArgumentException) { return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request"); }
});

app.MapGet("/game/v1/hives/{hiveId}/combat/squad-reservation", async (HttpContext context, string hiveId, AuthenticationManager authentication, CombatSquadReservationService service, IOptions<CombatSquadReservationOptions> options, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid id)) return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    try { return Results.Ok(await service.ReadAsync(auth.PlayerId!.Value, id, ct)); } catch (KeyNotFoundException) { return GameError(StatusCodes.Status404NotFound, "game.not_found", "game.error.not_found"); }
});

app.MapPost("/game/v1/hives/{hiveId}/combat/squad-reservation/commit", async (HttpContext context, string hiveId, AuthenticationManager authentication, CombatSquadReservationService service, IOptions<CombatSquadReservationOptions> options, SquadReservationHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out Guid id) || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    SquadReservationResult result = await service.CommitAsync(new(auth.PlayerId!.Value, id, request.ExpectedRevision, request.Quantities ?? new(), request.IdempotencyKey), ct);
    return result.Succeeded && result.Receipt is not null ? Results.Ok(new SquadReservationResponse(result.Receipt, result.Snapshot)) : GameError(StatusCodes.Status409Conflict, result.Code, "game.error.squad_reservation_conflict");
});

app.MapPost("/game/v1/hives/{hiveId}/combat/squad-reservation/release", async (HttpContext context, string hiveId, AuthenticationManager authentication, CombatSquadReservationService service, IOptions<CombatSquadReservationOptions> options, SquadReservationReleaseHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out Guid id) || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    SquadReservationResult result = await service.ReleaseAsync(new(auth.PlayerId!.Value, id, request.ExpectedRevision, request.IdempotencyKey), ct);
    return result.Succeeded && result.Receipt is not null ? Results.Ok(new SquadReservationResponse(result.Receipt, result.Snapshot)) : GameError(StatusCodes.Status409Conflict, result.Code, "game.error.squad_reservation_conflict");
});

app.MapGet("/game/v1/hives/{hiveId}/perimeter-sortie", async (HttpContext context, string hiveId, AuthenticationManager authentication, HivePerimeterSortieService service, IOptions<HivePerimeterSortieOptions> options, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid id)) return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    try { return Results.Ok(await service.ReadAsync(auth.PlayerId!.Value, id, ct)); } catch (KeyNotFoundException) { return GameError(StatusCodes.Status404NotFound, "game.not_found", "game.error.not_found"); }
});

app.MapPost("/game/v1/hives/{hiveId}/perimeter-sortie/launch", async (HttpContext context, string hiveId, AuthenticationManager authentication, HivePerimeterSortieService service, IOptions<HivePerimeterSortieOptions> options, HivePerimeterLaunchHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out var id) || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || string.IsNullOrWhiteSpace(request.SignalKey) || request.SignalKey.Length > 64 || string.IsNullOrWhiteSpace(request.SignalInstanceId) || request.SignalInstanceId.Length > 256 || string.IsNullOrWhiteSpace(request.ReservationId) || request.ReservationId.Length > 256 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    var result = await service.LaunchAsync(new(auth.PlayerId!.Value, id, request.SignalKey, request.SignalInstanceId, request.ReservationId, request.ExpectedRevision, request.IdempotencyKey), ct); return result.Succeeded && result.Receipt is not null ? Results.Ok(new HivePerimeterResponse(result.Receipt, result.Snapshot)) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.perimeter_conflict");
});
app.MapPost("/game/v1/hives/{hiveId}/perimeter-sortie/{sortieId:guid}/claim", async (HttpContext context, string hiveId, Guid sortieId, AuthenticationManager authentication, HivePerimeterSortieService service, IOptions<HivePerimeterSortieOptions> options, HivePerimeterMutationHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable"); var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out var id) || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) return GameError(400, "game.invalid_request", "game.error.invalid_request"); var result = await service.ClaimAsync(new(auth.PlayerId!.Value, id, sortieId, request.ExpectedRevision, request.IdempotencyKey), ct); return result.Succeeded && result.Receipt is not null ? Results.Ok(new HivePerimeterResponse(result.Receipt, result.Snapshot)) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.perimeter_conflict");
});
app.MapPost("/game/v1/hives/{hiveId}/perimeter-sortie/{sortieId:guid}/recall", async (HttpContext context, string hiveId, Guid sortieId, AuthenticationManager authentication, HivePerimeterSortieService service, IOptions<HivePerimeterSortieOptions> options, HivePerimeterMutationHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable"); var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out var id) || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) return GameError(400, "game.invalid_request", "game.error.invalid_request"); var result = await service.RecallAsync(new(auth.PlayerId!.Value, id, sortieId, request.ExpectedRevision, request.IdempotencyKey), ct); return result.Succeeded && result.Receipt is not null ? Results.Ok(new HivePerimeterResponse(result.Receipt, result.Snapshot)) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.perimeter_conflict");
});

app.MapGet("/game/v1/hives/{hiveId}/combat/patrol", async (HttpContext context, string hiveId, AuthenticationManager authentication, CombatPatrolService service, IOptions<CombatPatrolOptions> options, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out var id)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { return Results.Ok(await service.ReadAsync(auth.PlayerId!.Value, id, ct)); } catch (KeyNotFoundException) { return GameError(404, "game.not_found", "game.error.not_found"); }
});
app.MapPost("/game/v1/hives/{hiveId}/combat/patrol/{tier:int}/preview", async (HttpContext context, string hiveId, int tier, AuthenticationManager authentication, CombatPatrolService service, IOptions<CombatPatrolOptions> options, CombatPatrolPreviewHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out var id) || request.Guardians < 0 || request.Wingrunners < 0 || request.Darters < 0) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { return Results.Ok(await service.PreviewAsync(new(auth.PlayerId!.Value, id, tier, request.Guardians, request.Wingrunners, request.Darters), ct)); }
    catch (ArgumentException) { return GameError(400, "game.invalid_tier", "game.error.invalid_request"); }
    catch (KeyNotFoundException) { return GameError(404, "game.not_found", "game.error.not_found"); }
});
app.MapPost("/game/v1/hives/{hiveId}/combat/patrol/launch", async (HttpContext context, string hiveId, AuthenticationManager authentication, CombatPatrolService service, IOptions<CombatPatrolOptions> options, CombatPatrolLaunchHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out var id) || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || request.Guardians < 0 || request.Wingrunners < 0 || request.Darters < 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try
    {
        var result = await service.LaunchAsync(new(auth.PlayerId!.Value, id, request.Tier, request.Guardians, request.Wingrunners, request.Darters, request.ExpectedRevision, request.IdempotencyKey), ct);
        return result.Succeeded ? Results.Ok(new CombatPatrolMutationResponse(result.Snapshot, result.ClaimReceipt)) : GameError(result.Code is "game.invalid_request" or "game.patrol_underpowered" or "game.patrol_invalid_composition" or "game.patrol_insufficient_troops" ? 400 : 409, result.Code, "game.error.patrol_conflict");
    }
    catch (ArgumentException) { return GameError(400, "game.invalid_tier", "game.error.invalid_request"); }
});
app.MapPost("/game/v1/hives/{hiveId}/combat/patrol/{encounterId:guid}/claim", async (HttpContext context, string hiveId, Guid encounterId, AuthenticationManager authentication, CombatPatrolService service, IOptions<CombatPatrolOptions> options, CombatPatrolMutationHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out var id) || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    var result = await service.ClaimAsync(new(auth.PlayerId!.Value, id, encounterId, request.ExpectedRevision, request.IdempotencyKey), ct);
    return result.Succeeded ? Results.Ok(new CombatPatrolMutationResponse(result.Snapshot, result.ClaimReceipt)) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.patrol_conflict");
});
app.MapPost("/game/v1/hives/{hiveId}/combat/patrol/{encounterId:guid}/recall", async (HttpContext context, string hiveId, Guid encounterId, AuthenticationManager authentication, CombatPatrolService service, IOptions<CombatPatrolOptions> options, CombatPatrolMutationHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out var id) || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    var result = await service.RecallAsync(new(auth.PlayerId!.Value, id, encounterId, request.ExpectedRevision, request.IdempotencyKey), ct);
    return result.Succeeded ? Results.Ok(new CombatPatrolMutationResponse(result.Snapshot, result.ClaimReceipt)) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.patrol_conflict");
});
app.MapPost("/game/v1/hives/{hiveId}/combat/patrol/slots/purchase-resource", async (HttpContext context, string hiveId, AuthenticationManager authentication, CombatPatrolService service, IOptions<CombatPatrolOptions> options, CombatPatrolMutationHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out var id) || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    var result = await service.PurchaseResourceSlotAsync(new(auth.PlayerId!.Value, id, request.ExpectedRevision, request.IdempotencyKey), ct);
    return result.Succeeded ? Results.Ok(new CombatPatrolMutationResponse(result.Snapshot, result.ClaimReceipt)) : GameError(result.Code is "game.invalid_request" or "game.insufficient_resources" or "game.patrol_slot_limit_reached" ? 400 : 409, result.Code, "game.error.patrol_conflict");
});
// NOTE: this route only grants the entitlement — see CombatPatrolService.GrantPremiumSlotAsync.
// It must not ship reachable from raw client input without a real store-receipt check placed
// in front of it first.
app.MapPost("/game/v1/hives/{hiveId}/combat/patrol/slots/grant-premium", async (HttpContext context, string hiveId, AuthenticationManager authentication, CombatPatrolService service, IOptions<CombatPatrolOptions> options, CombatPatrolMutationHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    var auth = AuthenticateGameRequest(context, authentication); if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (request is null || !TryParseGameResourceId(hiveId, out var id) || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    var result = await service.GrantPremiumSlotAsync(new(auth.PlayerId!.Value, id, request.ExpectedRevision, request.IdempotencyKey), ct);
    return result.Succeeded ? Results.Ok(new CombatPatrolMutationResponse(result.Snapshot, result.ClaimReceipt)) : GameError(result.Code is "game.invalid_request" or "game.patrol_slot_limit_reached" ? 400 : 409, result.Code, "game.error.patrol_conflict");
});

// Internal player-support admin surface — see AdminSupportOptions/AuthorizeAdminSupport.
// Shared-secret gated (X-BeeKingdom-Support-Key), off by default everywhere. The page itself
// is not gated server-side (it's a static shell with no data), the API calls it makes are.
app.MapGet("/admin/ui", () => Results.Content(AdminUiPage.Html, "text/html"));

app.MapGet("/admin/v1/players/lookup", (HttpContext context, string email, IOptions<AdminSupportOptions> adminOptions, IAccountCredentialStore accounts) =>
{
    IResult? authorization = AuthorizeAdminSupport(context, adminOptions.Value);
    if (authorization is not null) return authorization;
    if (string.IsNullOrWhiteSpace(email)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    return accounts.TryGetByEmail(email, out AuthenticationAccount account)
        ? Results.Ok(new AdminPlayerLookupResponse(account.PlayerId.Value, account.AccountId, account.Email, account.State.ToString()))
        : GameError(404, "game.not_found", "game.error.not_found");
});

app.MapGet("/admin/v1/players/{playerId:guid}/hives", async (HttpContext context, Guid playerId, IOptions<AdminSupportOptions> adminOptions, IHiveStateRepository repository, CancellationToken ct) =>
{
    IResult? authorization = AuthorizeAdminSupport(context, adminOptions.Value);
    if (authorization is not null) return authorization;
    IReadOnlyList<Guid> hiveIds = await repository.ListHiveIdsAsync(playerId, ct);
    return Results.Ok(new AdminPlayerHivesResponse(hiveIds));
});

app.MapGet("/admin/v1/players/{playerId:guid}/hives/{hiveId:guid}/diagnostics", async (HttpContext context, Guid playerId, Guid hiveId, IOptions<AdminSupportOptions> adminOptions, AdminSupportService admin, CancellationToken ct) =>
{
    IResult? authorization = AuthorizeAdminSupport(context, adminOptions.Value);
    if (authorization is not null) return authorization;
    try { return Results.Ok(await admin.ReadDiagnosticsAsync(playerId, hiveId, ct)); }
    catch (KeyNotFoundException) { return GameError(404, "game.not_found", "game.error.not_found"); }
});

app.MapPost("/admin/v1/players/{playerId:guid}/hives/{hiveId:guid}/resources/adjust", async (HttpContext context, Guid playerId, Guid hiveId, IOptions<AdminSupportOptions> adminOptions, AdminSupportService admin, AdminResourceAdjustHttpRequest request, CancellationToken ct) =>
{
    IResult? authorization = AuthorizeAdminSupport(context, adminOptions.Value);
    if (authorization is not null) return authorization;
    if (request is null || string.IsNullOrWhiteSpace(request.Resource) || string.IsNullOrWhiteSpace(request.Reason) || request.ExpectedRevision < 0) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try
    {
        AdminMutationResult result = await admin.AdjustResourceAsync(new(playerId, hiveId, request.Resource, request.Delta, request.Reason, request.ExpectedRevision), ct);
        return result.Succeeded ? Results.Ok(result.State) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.admin_conflict");
    }
    catch (ArgumentException) { return GameError(400, "game.invalid_request", "game.error.invalid_request"); }
});

app.MapPost("/admin/v1/players/{playerId:guid}/hives/{hiveId:guid}/roster/adjust", async (HttpContext context, Guid playerId, Guid hiveId, IOptions<AdminSupportOptions> adminOptions, AdminSupportService admin, AdminRosterAdjustHttpRequest request, CancellationToken ct) =>
{
    IResult? authorization = AuthorizeAdminSupport(context, adminOptions.Value);
    if (authorization is not null) return authorization;
    if (request is null || string.IsNullOrWhiteSpace(request.Family) || string.IsNullOrWhiteSpace(request.Reason) || request.ExpectedRevision < 0) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try
    {
        AdminMutationResult result = await admin.AdjustRosterAsync(new(playerId, hiveId, request.Family, request.Delta, request.Reason, request.ExpectedRevision), ct);
        return result.Succeeded ? Results.Ok(result.State) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.admin_conflict");
    }
    catch (ArgumentException) { return GameError(400, "game.invalid_request", "game.error.invalid_request"); }
});

app.MapPost("/admin/v1/players/{playerId:guid}/hives/{hiveId:guid}/combat-patrol/slots/grant", async (HttpContext context, Guid playerId, Guid hiveId, IOptions<AdminSupportOptions> adminOptions, AdminSupportService admin, AdminGrantSlotHttpRequest request, CancellationToken ct) =>
{
    IResult? authorization = AuthorizeAdminSupport(context, adminOptions.Value);
    if (authorization is not null) return authorization;
    if (request is null || string.IsNullOrWhiteSpace(request.Reason) || request.ExpectedRevision < 0) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try
    {
        AdminMutationResult result = await admin.GrantCombatPatrolSlotAsync(new(playerId, hiveId, request.Premium, request.Reason, request.ExpectedRevision), ct);
        return result.Succeeded ? Results.Ok(result.State) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.admin_conflict");
    }
    catch (ArgumentException) { return GameError(400, "game.invalid_request", "game.error.invalid_request"); }
});

app.MapPost("/admin/v1/players/{playerId:guid}/hives/{hiveId:guid}/combat-patrol/recall-tokens/adjust", async (HttpContext context, Guid playerId, Guid hiveId, IOptions<AdminSupportOptions> adminOptions, AdminSupportService admin, AdminAdjustRecallTokensHttpRequest request, CancellationToken ct) =>
{
    IResult? authorization = AuthorizeAdminSupport(context, adminOptions.Value);
    if (authorization is not null) return authorization;
    if (request is null || string.IsNullOrWhiteSpace(request.Reason) || request.ExpectedRevision < 0) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try
    {
        AdminMutationResult result = await admin.AdjustRecallTokensAsync(new(playerId, hiveId, request.Delta, request.Reason, request.ExpectedRevision), ct);
        return result.Succeeded ? Results.Ok(result.State) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.admin_conflict");
    }
    catch (ArgumentException) { return GameError(400, "game.invalid_request", "game.error.invalid_request"); }
});

app.MapPost("/admin/v1/players/{playerId:guid}/hives/{hiveId:guid}/buildings/level", async (HttpContext context, Guid playerId, Guid hiveId, IOptions<AdminSupportOptions> adminOptions, AdminSupportService admin, AdminSetBuildingLevelHttpRequest request, CancellationToken ct) =>
{
    IResult? authorization = AuthorizeAdminSupport(context, adminOptions.Value);
    if (authorization is not null) return authorization;
    if (request is null || string.IsNullOrWhiteSpace(request.BuildingKey) || string.IsNullOrWhiteSpace(request.Reason) || request.Level < 0 || request.ExpectedRevision < 0) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try
    {
        AdminMutationResult result = await admin.SetBuildingLevelAsync(new(playerId, hiveId, request.BuildingKey, request.Level, request.Reason, request.ExpectedRevision), ct);
        return result.Succeeded ? Results.Ok(result.State) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.admin_conflict");
    }
    catch (ArgumentException) { return GameError(400, "game.invalid_request", "game.error.invalid_request"); }
});

// Bootstrap-only: grants/revokes the Admin (and, if ever needed directly, Moderator) role
// on an account. Shared-secret gated like the rest of this admin surface - deliberately NOT
// reachable from inside the game. Once an account holds Admin, it can grant/revoke
// Moderator to other players itself via the session-authenticated /accounts/v1/role/...
// endpoints below, so this endpoint should only be called rarely (e.g. to make the very
// first Admin accounts).
app.MapPost("/admin/v1/accounts/{accountId:guid}/role", (HttpContext context, Guid accountId, IOptions<AdminSupportOptions> adminOptions, IAccountCredentialStore credentials, AdminSetRoleHttpRequest request) =>
{
    IResult? authorization = AuthorizeAdminSupport(context, adminOptions.Value);
    if (authorization is not null) return authorization;
    if (request is null || string.IsNullOrWhiteSpace(request.Reason) || !Enum.IsDefined(request.Role)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    if (!credentials.TryGetByAccountId(accountId, out AuthenticationAccount account)) return GameError(404, "game.not_found", "game.error.not_found");
    credentials.Save(account with { Role = request.Role });
    return Results.Ok(new AdminPlayerLookupResponse(account.PlayerId.Value, account.AccountId, account.Email, account.State.ToString()));
});

app.MapPost("/game/v1/hives/{hiveId}/strategic-path", async (HttpContext context, string hiveId, AuthenticationManager authentication, StrategicPathService strategic, IOptions<StrategicPathOptions> options, StrategicPathHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsedHiveId) || request.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(request.PathId) || request.PathId.Length > 64 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256)
        return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    StrategicPathCommandResult result = await strategic.ChooseAsync(new(auth.PlayerId!.Value, parsedHiveId, request.PathId, request.ExpectedRevision, request.IdempotencyKey), ct);
    return result.Code switch
    {
        "game.strategic_path_selected" => Results.Ok(result.Snapshot),
        "game.idempotency_conflict" => GameError(StatusCodes.Status409Conflict, result.Code, "game.error.idempotency_conflict"),
        "game.invalid_request" => GameError(StatusCodes.Status400BadRequest, result.Code, "game.error.invalid_request"),
        "game.revision_conflict" or "game.strategic_path_ineligible" or "game.strategic_path_locked" => GameError(StatusCodes.Status409Conflict, result.Code, "game.error.strategic_path_conflict"),
        _ => GameError(StatusCodes.Status409Conflict, result.Code, "game.error.strategic_path_conflict")
    };
});

app.MapGet("/game/v1/hives/{hiveId}/world-resources", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<WorldResourceCollectionOptions> configured, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { return Results.Ok(await new WorldResourceCollectionService(repository, clock, configured.Value).ReadAsync(auth.PlayerId!.Value, parsed, ct)); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
    catch (InvalidOperationException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});
app.MapPost("/game/v1/hives/{hiveId}/world-resources/{nodeId}/launch", async (HttpContext context, string hiveId, string nodeId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<WorldResourceCollectionOptions> configured, LaunchWorldResourceCollectionRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed) || string.IsNullOrWhiteSpace(nodeId) || request is null) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { var result = await new WorldResourceCollectionService(repository, clock, configured.Value).LaunchAsync(auth.PlayerId!.Value, parsed, nodeId, request, ct); return result.Succeeded ? Results.Ok(result.Snapshot) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.conflict"); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});
app.MapPost("/game/v1/hives/{hiveId}/world-resources/{flightId:guid}/claim", async (HttpContext context, string hiveId, Guid flightId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<WorldResourceCollectionOptions> configured, ClaimWorldResourceCollectionRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed) || request is null) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { var result = await new WorldResourceCollectionService(repository, clock, configured.Value).ClaimAsync(auth.PlayerId!.Value, parsed, flightId, request, ct); return result.Succeeded ? Results.Ok(result.Snapshot) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.conflict"); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});
app.MapPost("/game/v1/hives/{hiveId}/world-resources/{flightId:guid}/recall", async (HttpContext context, string hiveId, Guid flightId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<WorldResourceCollectionOptions> configured, RecallWorldResourceCollectionRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed) || request is null) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { var result = await new WorldResourceCollectionService(repository, clock, configured.Value).RecallAsync(auth.PlayerId!.Value, parsed, flightId, request, ct); return result.Succeeded ? Results.Ok(result.Snapshot) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.conflict"); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});
// Monde vivant (demande de Jeff, 2026-08-01) : lecture seule, presence ambiante uniquement -
// jamais de mutation, jamais d'interaction avec l'etat d'un autre joueur.
app.MapGet("/game/v1/hives/{hiveId}/world-presence", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, CancellationToken ct) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    return Results.Ok(await new WorldPresenceService(repository, clock).ReadAsync(parsed, ct));
});

// Carnet du Bestiaire (demande de Jeff, 2026-08-01) : lecture seule, sous-produit du flux Combat
// Patrol existant - jamais de mutation depuis cette route.
app.MapGet("/game/v1/hives/{hiveId}/bestiary-codex", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, CancellationToken ct) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { return Results.Ok(await new BestiaryCodexService(repository, clock).ReadAsync(auth.PlayerId!.Value, parsed, ct)); }
    catch (KeyNotFoundException) { return GameError(404, "game.not_found", "game.error.not_found"); }
});

app.MapGet("/game/v1/hives/{hiveId}/milestone-event", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<HiveMilestoneEventOptions> configured, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { return Results.Ok(await new HiveMilestoneEventService(repository, clock, configured.Value).ReadAsync(auth.PlayerId!.Value, parsed, ct)); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
    catch (InvalidOperationException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});
app.MapPost("/game/v1/hives/{hiveId}/milestone-event/claim", async (HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<HiveMilestoneEventOptions> configured, ClaimHiveMilestoneEventRequest request, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed) || request is null) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try { var result = await new HiveMilestoneEventService(repository, clock, configured.Value).ClaimAsync(auth.PlayerId!.Value, parsed, request, ct); return result.Succeeded ? Results.Ok(result.Snapshot) : GameError(result.Code == "game.invalid_request" ? 400 : 409, result.Code, "game.error.conflict"); }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});

app.MapGet("/game/v1/hives/{hiveId}/rewards", async (HttpContext context, string hiveId, AuthenticationManager authentication, RewardLedgerService ledger, IOptions<RewardLedgerOptions> configured, CancellationToken ct) =>
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try
    {
        RewardLedgerReadSnapshot? snapshot = await ledger.ReadAsync(auth.PlayerId!.Value, parsed, ct);
        return snapshot is null ? GameError(404, "game.hive_not_found", "game.error.not_found") : Results.Ok(snapshot);
    }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
    catch (InvalidOperationException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});
app.MapPost("/admin/v1/players/{playerId:guid}/hives/{hiveId:guid}/rewards/grant", async (HttpContext context, Guid playerId, Guid hiveId, IOptions<AdminSupportOptions> adminOptions, RewardLedgerService ledger, AdminGrantRewardHttpRequest request, CancellationToken ct) =>
{
    IResult? authorization = AuthorizeAdminSupport(context, adminOptions.Value);
    if (authorization is not null) return authorization;
    if (request is null || string.IsNullOrWhiteSpace(request.RewardKey) || string.IsNullOrWhiteSpace(request.Source) || string.IsNullOrWhiteSpace(request.ResourceKey) || request.Amount <= 0 || request.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || string.IsNullOrWhiteSpace(request.Reason))
        return GameError(400, "game.invalid_request", "game.error.invalid_request");
    try
    {
        RewardLedgerCommandResult result = await ledger.GrantAsync(new(playerId, hiveId, request.RewardKey, request.Source, request.ResourceKey, request.Amount, request.ExpectedRevision, request.IdempotencyKey, request.NotificationKey), ct);
        return result.Succeeded ? Results.Ok(result.Snapshot) : GameError(result.Code is "invalid_request" ? 400 : 409, result.Code, "game.error.admin_conflict");
    }
    catch (InvalidDataException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
    catch (InvalidOperationException) { return GameError(503, "game.unavailable", "game.error.unavailable"); }
});

app.MapPost("/game/v1/hives/{hiveId}/workshop/batch-qualification", async (
    HttpContext context, string hiveId, AuthenticationManager authentication, HiveOperationService hiveOperations,
    IOptions<WorkshopBatchQualificationOptions> options, WorkshopBatchQualificationHttpRequest request, CancellationToken ct) =>
{
    if (!options.Value.Enabled) return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsedHiveId) || request.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(request.Answer) || request.Answer.Length > 32 || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256)
        return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    WorkshopBatchQualificationResult result = await hiveOperations.QualifyWorkshopBatchAsync(new(auth.PlayerId!.Value, parsedHiveId, request.ExpectedRevision, request.Answer, request.IdempotencyKey), ct);
    return result.Code switch
    {
        "tutorial_answer_incorrect" or "tutorial_advanced" => Results.Ok(new WorkshopBatchQualificationHttpResponse(result.PreviousStep, result.ResultingStep, result.Answer, result.RevisionBefore, result.RevisionAfter, result.AcceptedAtUtc, result.Code)),
        "invalid_request" => GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request"),
        "idempotency_conflict" => GameError(StatusCodes.Status409Conflict, "game.idempotency_conflict", "game.error.idempotency_conflict"),
        "revision_conflict" => GameError(StatusCodes.Status409Conflict, "game.revision_conflict", "game.error.revision_conflict"),
        "tutorial_precondition_failed" => GameError(StatusCodes.Status409Conflict, "game.tutorial_precondition_failed", "game.error.tutorial_precondition_failed"),
        _ => GameError(StatusCodes.Status409Conflict, "game.tutorial_precondition_failed", "game.error.tutorial_precondition_failed")
    };
});

app.MapGet("/chat/v1/capabilities", (HttpContext context, ChatManager chat) =>
{
    context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate";
    context.Response.Headers.Pragma = "no-cache";
    context.Response.Headers.Expires = "0";
    context.Response.Headers.Vary = "Accept-Encoding";
    return Results.Ok(chat.GetCapabilities());
});

app.MapGet("/chat/v1/conversations", (HttpContext context, AuthenticationManager authentication, ChatManager chat, int? limit, string? cursor) =>
{
    TokenValidationResult auth = AuthenticateChatRequest(context, authentication);
    if (!auth.IsValid)
    {
        return ChatError(StatusCodes.Status401Unauthorized, "chat.session_required", "chat.error.session_required");
    }

    return ExecuteChat(context, () => { ChatConversationPage page=chat.ListConversations(auth.PlayerId,limit??50,cursor); return Results.Ok(new ChatTransportConversationPage(page.Items.Select(x=>ChatTransportMapper.Conversation(x,chat.GetLastSequence(x.ConversationId),chat.GetInbox(auth.PlayerId,x.ConversationId))).ToArray(),page.NextCursor)); });
});

app.MapPost("/chat/v1/conversations", (HttpContext context, AuthenticationManager authentication, ChatManager chat, CreateChatConversationRequest request) =>
{
    TokenValidationResult auth = AuthenticateChatRequest(context, authentication);
    if (!auth.IsValid)
    {
        return ChatError(StatusCodes.Status401Unauthorized, "chat.session_required", "chat.error.session_required");
    }

    return ExecuteChat(context, () => { CreateChatConversationResult value=chat.CreateConversation(auth.PlayerId,request); return Results.Ok(new ChatTransportCreateConversationResult(ChatTransportMapper.Conversation(value.Conversation,chat.GetLastSequence(value.Conversation.ConversationId),value.Inbox),value.Inbox,request.ClientRequestId)); });
});

app.MapGet("/chat/v1/conversations/{conversationId}/messages", (HttpContext context, AuthenticationManager authentication, ChatManager chat, string conversationId, long? afterSequence, int? limit) =>
{
    TokenValidationResult auth = AuthenticateChatRequest(context, authentication);
    if (!auth.IsValid)
    {
        return ChatError(StatusCodes.Status401Unauthorized, "chat.session_required", "chat.error.session_required");
    }

    if (!TryParseChatResourceId(conversationId, out Guid parsedConversationId)) return ChatError(400,"chat.invalid_request","chat.error.invalid_request");
    return ExecuteChat(context, () => { ChatMessagePage page=chat.GetMessages(auth.PlayerId,parsedConversationId,afterSequence??0,limit??50); return Results.Ok(new ChatWireMessagePage(page.Items.Select(ChatTransportMapper.Message).ToArray(),page.NextAfterSequence)); });
});

app.MapPost("/chat/v1/conversations/{conversationId}/messages", async (HttpContext context, AuthenticationManager authentication, ChatManager chat, string conversationId, SendChatMessageRequest request, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateChatRequest(context, authentication);
    if (!auth.IsValid)
    {
        return ChatError(StatusCodes.Status401Unauthorized, "chat.session_required", "chat.error.session_required");
    }

    if (!TryParseChatResourceId(conversationId, out Guid parsedConversationId)) return ChatError(400,"chat.invalid_request","chat.error.invalid_request");
    return await ExecuteChatAsync(context, async () => { SendChatMessageResult value=await chat.SendMessageAsync(auth.PlayerId,parsedConversationId,request,cancellationToken); return Results.Ok(new ChatWireSendResult(ChatTransportMapper.Message(value.Message),value.Deduplicated,value.ServerSequence)); });
});

app.MapPost("/chat/v1/conversations/{conversationId}/read", (HttpContext context, AuthenticationManager authentication, ChatManager chat, string conversationId, MarkChatConversationReadRequest request) =>
{
    TokenValidationResult auth = AuthenticateChatRequest(context, authentication);
    if (!auth.IsValid)
    {
        return ChatError(StatusCodes.Status401Unauthorized, "chat.session_required", "chat.error.session_required");
    }

    if (!TryParseChatResourceId(conversationId, out Guid parsedConversationId)) return ChatError(400,"chat.invalid_request","chat.error.invalid_request");
    return ExecuteChat(context, () => Results.Ok(chat.MarkRead(auth.PlayerId, parsedConversationId, request.Sequence)));
});

app.MapPost("/chat/v1/messages/{messageId}/report", (HttpContext context, AuthenticationManager authentication, ChatManager chat, string messageId, ReportChatMessageRequest request) =>
{
    TokenValidationResult auth = AuthenticateChatRequest(context, authentication);
    if (!auth.IsValid)
    {
        return ChatError(StatusCodes.Status401Unauthorized, "chat.session_required", "chat.error.session_required");
    }

    if (!TryParseChatResourceId(messageId, out Guid parsedMessageId)) return ChatError(400,"chat.invalid_request","chat.error.invalid_request");
    return ExecuteChat(context, () => { ChatModerationReport report=chat.ReportMessage(auth.PlayerId, parsedMessageId, request); return Results.Ok(new ChatTransportModerationReportResult(report.ReportId,report.MessageId,request.ClientRequestId,report.Status)); });
});

app.MapPost("/chat/v1/messages/{messageId}/translations", async (HttpContext context, AuthenticationManager authentication, ChatTranslationService translations, string messageId, ChatTranslationRequest request, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateChatRequest(context, authentication);
    if (!auth.IsValid) return ChatError(StatusCodes.Status401Unauthorized, "chat.session_required", "chat.error.session_required");
    if (!TryParseChatResourceId(messageId, out Guid parsedMessageId)) return ChatError(400,"chat.invalid_request","chat.error.invalid_request");
    try { return Results.Ok(await translations.TranslateAsync(auth.PlayerId, parsedMessageId, request, cancellationToken)); }
    catch (InvalidOperationException exception) when (exception.Message == "translation_rate_limited") { context.Response.Headers.RetryAfter="60"; return ChatError(429,"chat.rate_limited","chat.error.rate_limited",60); }
    catch (InvalidOperationException exception) when (exception.Message is "translation_provider_unavailable" or "translation_invalid_response") { context.Response.Headers.RetryAfter="30"; return ChatError(503,"chat.translation_unavailable","chat.error.translation_unavailable",30); }
    catch (UnauthorizedAccessException) { return ChatError(403,"chat.forbidden","chat.error.forbidden"); }
    catch (KeyNotFoundException) { return ChatError(404,"chat.not_found","chat.error.not_found"); }
    catch (ArgumentException) { return ChatError(400,"chat.invalid_request","chat.error.invalid_request"); }
});

app.MapPost("/chat/v1/alliances/{allianceId:guid}/announcements", async (HttpContext context, AuthenticationManager authentication, ChatManager chat, Guid allianceId, CreateAllianceAnnouncementRequest request, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateChatRequest(context, authentication);
    if (!auth.IsValid)
    {
        return ChatError(StatusCodes.Status401Unauthorized, "chat.session_required", "chat.error.session_required");
    }

    return await ExecuteChatAsync(context, async () => Results.Ok(await chat.SendAllianceAnnouncementAsync(auth.PlayerId, allianceId, request, cancellationToken)));
});

// ==================== Player Directory (M043B-CL) ====================
// Generic, reusable player search - NOT Alliance-specific (see BeeKingdom.Accounts.
// PlayerDirectoryService). Auth-required; never exposes email/status/auth-provider data. A blank
// or too-short query is rejected (400), not silently treated as "list everyone".

app.MapGet("/game/v1/players/search", (HttpContext context, AuthenticationManager authentication, BeeKingdom.Accounts.IPlayerDirectoryService directory, string? q, int? offset, int? limit) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "game.session_required", "game.error.session_required");
    try
    {
        return Results.Ok(directory.Search(q ?? string.Empty, offset ?? 0, limit ?? 20));
    }
    catch (ArgumentException)
    {
        return GameError(StatusCodes.Status400BadRequest, "game.invalid_request", "game.error.invalid_request");
    }
});

// ==================== Alliance (M041-CL) ====================
// Auth/path-parsing/error-mapping mirrors the existing /game/v1/* family (AuthenticateGameRequest,
// TryParseGameResourceId, GameError) - Alliance is a gameplay domain, not chat, even though it
// links to Chat for the alliance conversation. See AllianceService.cs for the exception vocabulary
// this ExecuteAlliance wrapper maps to HTTP status codes.

app.MapPost("/alliance/v1/alliances", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, CreateAllianceRequest request) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.CreateAlliance(auth.PlayerId, request)));
});

app.MapGet("/alliance/v1/membership/mine", (HttpContext context, AuthenticationManager authentication, AllianceService alliances) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() =>
        Results.Ok(BeeKingdom.Alliance.Models.MyAllianceOverviewResponse.From(alliances.GetMyAlliance(auth.PlayerId))));
});

app.MapGet("/alliance/v1/alliances/search", (HttpContext context, AllianceService alliances, string? nameOrTag, string? language, string? joinMode, int? offset, int? limit) =>
{
    AllianceJoinMode? parsedJoinMode = Enum.TryParse<AllianceJoinMode>(joinMode, true, out var jm) ? jm : null;
    return ExecuteAlliance(() => Results.Ok(alliances.Search(new AllianceSearchQuery(nameOrTag, language, parsedJoinMode, offset ?? 0, limit ?? 20))));
});

app.MapGet("/alliance/v1/alliances/{allianceId:guid}", (AllianceService alliances, Guid allianceId) =>
    ExecuteAlliance(() => Results.Ok(alliances.GetPublicProfile(new AllianceId(allianceId)))));

app.MapGet("/alliance/v1/alliances/by-slug/{slug}", (AllianceService alliances, string slug) =>
    ExecuteAlliance(() =>
    {
        BeeKingdom.Alliance.Models.AllianceEntity? entity = alliances.GetBySlug(slug);
        return entity == null ? AllianceError(404, "alliance.not_found") : Results.Ok(alliances.GetPublicProfile(entity.AllianceId));
    }));

app.MapGet("/alliance/v1/alliances/{allianceId:guid}/activity/public", (AllianceService alliances, Guid allianceId, long? beforeSequence, int? limit) =>
    ExecuteAlliance(() => Results.Ok(alliances.ListPublicActivity(new AllianceId(allianceId), beforeSequence, limit ?? 30))));

app.MapGet("/alliance/v1/alliances/{allianceId:guid}/activity", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid allianceId, long? beforeSequence, int? limit) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.ListActivity(auth.PlayerId, new AllianceId(allianceId), beforeSequence, limit ?? 30)));
});

app.MapGet("/alliance/v1/alliances/{allianceId:guid}/members", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid allianceId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.ListMembers(auth.PlayerId, new AllianceId(allianceId))));
});

app.MapPost("/alliance/v1/alliances/{allianceId:guid}/join", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid allianceId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.JoinOpen(auth.PlayerId, new AllianceId(allianceId))));
});

app.MapPost("/alliance/v1/alliances/{allianceId:guid}/applications", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid allianceId, SubmitApplicationRequest request) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.SubmitApplication(auth.PlayerId, new AllianceId(allianceId), request)));
});

app.MapGet("/alliance/v1/applications/pending", (HttpContext context, AuthenticationManager authentication, AllianceService alliances) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.ListPendingApplicationsForMyAlliance(auth.PlayerId)));
});

app.MapPost("/alliance/v1/applications/{applicationId:guid}/cancel", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid applicationId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.CancelApplication(auth.PlayerId, applicationId)));
});

app.MapPost("/alliance/v1/applications/{applicationId:guid}/accept", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid applicationId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.AcceptApplication(auth.PlayerId, applicationId)));
});

app.MapPost("/alliance/v1/applications/{applicationId:guid}/reject", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid applicationId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.RejectApplication(auth.PlayerId, applicationId)));
});

app.MapPost("/alliance/v1/alliances/{allianceId:guid}/invitations", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid allianceId, CreateInvitationRequest request) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.CreateInvitation(auth.PlayerId, new AllianceId(allianceId), request)));
});

app.MapGet("/alliance/v1/invitations/mine", (HttpContext context, AuthenticationManager authentication, AllianceService alliances) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.ListMyInvitations(auth.PlayerId)));
});

app.MapPost("/alliance/v1/invitations/{invitationId:guid}/accept", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid invitationId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.AcceptInvitation(auth.PlayerId, invitationId)));
});

app.MapPost("/alliance/v1/invitations/{invitationId:guid}/decline", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid invitationId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.DeclineInvitation(auth.PlayerId, invitationId)));
});

app.MapPost("/alliance/v1/invitations/{invitationId:guid}/revoke", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid invitationId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.RevokeInvitation(auth.PlayerId, invitationId)));
});

app.MapPost("/alliance/v1/membership/leave", (HttpContext context, AuthenticationManager authentication, AllianceService alliances) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    // M043-CL: a bare Results.Ok() writes an empty response body, which the Unity codec's
    // Deserialize<T> explicitly rejects as malformed (even for T=object) - always return a small
    // real JSON body from a mutation endpoint a typed client will actually parse.
    return ExecuteAlliance(() => { alliances.Leave(auth.PlayerId); return Results.Ok(new { success = true }); });
});

app.MapPost("/alliance/v1/membership/{targetPlayerId:guid}/kick", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid targetPlayerId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => { alliances.Kick(auth.PlayerId, new PlayerId(targetPlayerId)); return Results.Ok(new { success = true }); });
});

app.MapPost("/alliance/v1/membership/{targetPlayerId:guid}/promote", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid targetPlayerId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.Promote(auth.PlayerId, new PlayerId(targetPlayerId))));
});

app.MapPost("/alliance/v1/membership/{targetPlayerId:guid}/demote", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid targetPlayerId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.Demote(auth.PlayerId, new PlayerId(targetPlayerId))));
});

app.MapPost("/alliance/v1/membership/{targetPlayerId:guid}/transfer-leadership", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid targetPlayerId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.TransferLeadership(auth.PlayerId, new PlayerId(targetPlayerId))));
});

app.MapPost("/alliance/v1/alliances/dissolve", (HttpContext context, AuthenticationManager authentication, AllianceService alliances) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.Dissolve(auth.PlayerId)));
});

app.MapPost("/alliance/v1/alliances/profile", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, UpdateAllianceProfileRequest request) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.UpdateProfile(auth.PlayerId, request)));
});

app.MapPost("/alliance/v1/diplomacy/{targetAllianceId:guid}/propose", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid targetAllianceId, ProposeDiplomacyRequest request) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.ProposeRelation(auth.PlayerId, new AllianceId(targetAllianceId), request)));
});

app.MapPost("/alliance/v1/diplomacy/{proposerAllianceId:guid}/accept", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid proposerAllianceId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.RespondToRelation(auth.PlayerId, new AllianceId(proposerAllianceId), true)));
});

app.MapPost("/alliance/v1/diplomacy/{proposerAllianceId:guid}/reject", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid proposerAllianceId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.RespondToRelation(auth.PlayerId, new AllianceId(proposerAllianceId), false)));
});

app.MapPost("/alliance/v1/diplomacy/{otherAllianceId:guid}/cancel", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, Guid otherAllianceId) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.CancelRelation(auth.PlayerId, new AllianceId(otherAllianceId))));
});

app.MapPost("/alliance/v1/war/declare", (HttpContext context, AuthenticationManager authentication, AllianceService alliances, DeclareWarRequest request) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return ExecuteAlliance(() => Results.Ok(alliances.DeclareWar(auth.PlayerId, request)));
});

// ---------------- M045-CL: Alliance Help ----------------
// Cooperative help against the REAL Construction/Research/Training/Healing timers - see
// AllianceHelpService's class comment. Never a parallel timer: OperationTimerReduction (shared with
// SpeedUpInventoryService) is the only thing that ever mutates a real operation's remaining time.

app.MapGet("/alliance/v1/help/requests", async (HttpContext context, AuthenticationManager authentication, AllianceHelpService helpService, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return await ExecuteAllianceHelpAsync(async () => Results.Ok(await helpService.ListHelpableViewsForCurrentAllianceAsync(auth.PlayerId, cancellationToken)));
});

app.MapGet("/alliance/v1/help/requests/mine", async (HttpContext context, AuthenticationManager authentication, AllianceHelpService helpService, string category, string targetId, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return await ExecuteAllianceHelpAsync(async () =>
    {
        AllianceHelpRequest? mine = await helpService.GetMyOpenRequestAsync(auth.PlayerId, category, targetId, cancellationToken);
        return Results.Ok(mine);
    });
});

app.MapPost("/alliance/v1/help/requests", async (HttpContext context, AuthenticationManager authentication, AllianceHelpService helpService, CreateAllianceHelpRequestCommand request, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return await ExecuteAllianceHelpAsync(async () =>
    {
        AllianceHelpCommandResult result = await helpService.CreateRequestAsync(auth.PlayerId, request, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : AllianceHelpError(result.Code);
    });
});

app.MapPost("/alliance/v1/help/requests/{helpRequestId:guid}/contribute", async (HttpContext context, AuthenticationManager authentication, AllianceHelpService helpService, Guid helpRequestId, AllianceHelpContributeWireRequest request, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return await ExecuteAllianceHelpAsync(async () =>
    {
        ContributeAllianceHelpResult result = await helpService.ContributeAsync(auth.PlayerId, helpRequestId, request.ClientRequestId, cancellationToken);
        return result.Succeeded ? Results.Ok(result) : AllianceHelpError(result.Code);
    });
});

app.MapPost("/alliance/v1/help/contribute-all", async (HttpContext context, AuthenticationManager authentication, AllianceHelpService helpService, AllianceHelpContributeWireRequest request, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return await ExecuteAllianceHelpAsync(async () => Results.Ok(await helpService.ContributeAllAsync(auth.PlayerId, request.ClientRequestId, cancellationToken)));
});

// M052-CL: Alliance Research - Bible-aligned lifecycle (BIBLE_ALLIANCE_RESEARCH.md V1.0):
// Chef-only funding-target selection -> member donations (clamped to real remaining need) ->
// fully-funded READY -> Chef/Officer launch -> server-authoritative timer -> COMPLETED -> bonus.
// See AllianceResearchService's own class comment for the two-aggregate atomicity strategy.
app.MapGet("/alliance/v1/research", async (HttpContext context, AuthenticationManager authentication, AllianceResearchService researchService, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return await ExecuteAllianceResearchAsync(async () => Results.Ok(await researchService.GetSnapshotAsync(auth.PlayerId, cancellationToken)));
});

app.MapPost("/alliance/v1/research/funding-target", async (HttpContext context, AuthenticationManager authentication, AllianceResearchService researchService, AllianceResearchFundingTargetWireRequest request, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return await ExecuteAllianceResearchAsync(async () =>
    {
        AllianceResearchCommandResult result = await researchService.SelectFundingTargetAsync(auth.PlayerId, new SelectAllianceResearchFundingTargetCommand(request.TechnologyId, request.ClientRequestId), cancellationToken);
        return result.Succeeded ? Results.Ok(result) : AllianceResearchError(result.Code);
    });
});

app.MapPost("/alliance/v1/research/{technologyId}/donate", async (HttpContext context, AuthenticationManager authentication, AllianceResearchService researchService, string technologyId, AllianceResearchDonateWireRequest request, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return await ExecuteAllianceResearchAsync(async () =>
    {
        AllianceResearchCommandResult result = await researchService.DonateAsync(auth.PlayerId,
            new DonateToAllianceResearchCommand(request.HiveId, technologyId, request.ResourceKey, request.Amount, request.ClientRequestId), cancellationToken);
        return result.Succeeded ? Results.Ok(result) : AllianceResearchError(result.Code);
    });
});

app.MapPost("/alliance/v1/research/{technologyId}/launch", async (HttpContext context, AuthenticationManager authentication, AllianceResearchService researchService, string technologyId, AllianceResearchLaunchWireRequest request, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return await ExecuteAllianceResearchAsync(async () =>
    {
        AllianceResearchCommandResult result = await researchService.LaunchAsync(auth.PlayerId, new LaunchAllianceResearchCommand(technologyId, request.ClientRequestId), cancellationToken);
        return result.Succeeded ? Results.Ok(result) : AllianceResearchError(result.Code);
    });
});

app.MapPost("/alliance/v1/research/{technologyId}/speedup", async (HttpContext context, AuthenticationManager authentication, AllianceResearchService researchService, string technologyId, AllianceResearchSpeedUpWireRequest request, CancellationToken cancellationToken) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(StatusCodes.Status401Unauthorized, "alliance.session_required", "alliance.error.session_required");
    return await ExecuteAllianceResearchAsync(async () =>
    {
        AllianceResearchCommandResult result = await researchService.ApplySpeedUpAsync(auth.PlayerId,
            new ApplyAllianceResearchSpeedUpCommand(request.HiveId, technologyId, request.ItemId, request.ClientRequestId), cancellationToken);
        return result.Succeeded ? Results.Ok(result) : AllianceResearchError(result.Code);
    });
});

// M0??-CL: bilingual (EN/FR) News/Actualites CMS backing the companion website
// (beekingdom-web). Admin authoring gates on the EXISTING AuthenticationAccount.Role ==
// AccountRole.Admin - the exact pattern used by /accounts/v1/role/lookup above, no separate
// password system. Public read endpoints (list + detail) require no auth at all.
app.MapGet("/news/v1/articles", async (NewsService news, int? limit, string? cursor, CancellationToken cancellationToken) =>
{
    return await ExecuteNewsAsync(async () =>
    {
        int offset = ParseNewsCursor(cursor);
        IReadOnlyList<NewsArticle> page = await news.ListPublishedAsync(offset, limit, cancellationToken);
        var summaries = page.Select(NewsArticleSummary.FromArticle).ToArray();
        string? nextCursor = summaries.Length > 0 ? (offset + summaries.Length).ToString() : null;
        return Results.Ok(new NewsArticlePage<NewsArticleSummary>(summaries, nextCursor));
    });
});

app.MapGet("/news/v1/articles/{slug}", async (NewsService news, string slug, CancellationToken cancellationToken) =>
{
    return await ExecuteNewsAsync(async () =>
    {
        NewsArticle? article = await news.GetPublishedBySlugAsync(slug, cancellationToken);
        return article is null ? NewsError(404, "not_found") : Results.Ok(NewsArticleDetail.FromArticle(article));
    });
});

app.MapGet("/news/v1/admin/articles", async (HttpContext context, AuthenticationManager authentication, IAccountCredentialStore credentials, NewsService news, int? limit, string? cursor, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeNewsAdmin(context, authentication, credentials, out _);
    if (authorization != null) return authorization;
    return await ExecuteNewsAsync(async () =>
    {
        int offset = ParseNewsCursor(cursor);
        IReadOnlyList<NewsArticle> page = await news.ListAllAsync(offset, limit, cancellationToken);
        var summaries = page.Select(NewsArticleSummary.FromArticle).ToArray();
        string? nextCursor = summaries.Length > 0 ? (offset + summaries.Length).ToString() : null;
        return Results.Ok(new NewsArticlePage<NewsArticleSummary>(summaries, nextCursor));
    });
});

app.MapGet("/news/v1/admin/articles/{slug}", async (HttpContext context, AuthenticationManager authentication, IAccountCredentialStore credentials, NewsService news, string slug, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeNewsAdmin(context, authentication, credentials, out _);
    if (authorization != null) return authorization;
    return await ExecuteNewsAsync(async () =>
    {
        NewsArticle? article = await news.GetAnyBySlugAsync(slug, cancellationToken);
        return article is null ? NewsError(404, "not_found") : Results.Ok(article);
    });
});

app.MapPost("/news/v1/admin/articles", async (HttpContext context, AuthenticationManager authentication, IAccountCredentialStore credentials, NewsService news, NewsArticleCreateRequest request, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeNewsAdmin(context, authentication, credentials, out AuthenticationAccount caller);
    if (authorization != null) return authorization;
    return await ExecuteNewsAsync(async () =>
    {
        NewsArticleCommandResult result = await news.CreateAsync(caller.AccountId, request, cancellationToken);
        return result.Succeeded ? Results.Ok(result.Article) : NewsCommandError(result.Code);
    });
});

app.MapPut("/news/v1/admin/articles/{slug}", async (HttpContext context, AuthenticationManager authentication, IAccountCredentialStore credentials, NewsService news, string slug, NewsArticleUpdateRequest request, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeNewsAdmin(context, authentication, credentials, out _);
    if (authorization != null) return authorization;
    return await ExecuteNewsAsync(async () =>
    {
        NewsArticleCommandResult result = await news.UpdateAsync(slug, request, cancellationToken);
        return result.Succeeded ? Results.Ok(result.Article) : NewsCommandError(result.Code);
    });
});

app.MapPost("/news/v1/admin/articles/{slug}/publish", async (HttpContext context, AuthenticationManager authentication, IAccountCredentialStore credentials, NewsService news, string slug, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeNewsAdmin(context, authentication, credentials, out _);
    if (authorization != null) return authorization;
    return await ExecuteNewsAsync(async () =>
    {
        NewsArticleCommandResult result = await news.PublishAsync(slug, cancellationToken);
        return result.Succeeded ? Results.Ok(result.Article) : NewsCommandError(result.Code);
    });
});

app.MapPost("/news/v1/admin/articles/{slug}/unpublish", async (HttpContext context, AuthenticationManager authentication, IAccountCredentialStore credentials, NewsService news, string slug, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeNewsAdmin(context, authentication, credentials, out _);
    if (authorization != null) return authorization;
    return await ExecuteNewsAsync(async () =>
    {
        NewsArticleCommandResult result = await news.UnpublishAsync(slug, cancellationToken);
        return result.Succeeded ? Results.Ok(result.Article) : NewsCommandError(result.Code);
    });
});

app.MapDelete("/news/v1/admin/articles/{slug}", async (HttpContext context, AuthenticationManager authentication, IAccountCredentialStore credentials, NewsService news, string slug, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeNewsAdmin(context, authentication, credentials, out _);
    if (authorization != null) return authorization;
    return await ExecuteNewsAsync(async () =>
    {
        NewsArticleCommandResult result = await news.DeleteAsync(slug, cancellationToken);
        return result.Succeeded ? Results.Ok() : NewsCommandError(result.Code);
    });
});

app.MapGet("/ops/migrations/pending", async (HttpContext context, IOptions<OpsSecurityOptions> ops, IMigrationRunner migrations, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeOps(context, ops.Value);
    if (authorization != null)
    {
        return authorization;
    }

    return Results.Ok(await migrations.GetPendingMigrationsAsync(cancellationToken));
});

app.MapPost("/ops/migrations/apply", async (HttpContext context, IOptions<OpsSecurityOptions> ops, IMigrationRunner migrations, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeOps(context, ops.Value);
    if (authorization != null)
    {
        return authorization;
    }

    IResult? migrationAuthorization = AuthorizeMigrationApply(context, ops.Value);
    if (migrationAuthorization != null)
    {
        return migrationAuthorization;
    }

    await migrations.ApplyPendingMigrationsAsync(cancellationToken);
    return Results.Ok(new { status = "Applied" });
});

// M054B-CL: one-time Royal Seals legacy-balance backfill (see RoyalSealsMigrationService's own
// class comment) - mirrors the /ops/migrations/pending + /ops/migrations/apply Admin Key / Migration
// Key convention exactly. The GET is a pure read-only preview (dryRun: true internally) so the CEO
// can see the real inventory (players affected, total amount) before ever authorizing the POST,
// which is the only call that actually writes. Both report identical counts for the same database
// state - re-running the POST after it has already fully completed is always a safe no-op (idempotent
// via the SAME PlayerHiveState.Receipts mechanism every other paid action in this codebase uses).
app.MapGet("/ops/royal-seals-migration/preview", async (HttpContext context, IOptions<OpsSecurityOptions> ops, BeeKingdom.Alliance.Research.RoyalSealsMigrationService migration, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeOps(context, ops.Value);
    if (authorization != null)
    {
        return authorization;
    }

    return Results.Ok(await migration.MigrateAsync(dryRun: true, cancellationToken));
});

app.MapPost("/ops/royal-seals-migration/apply", async (HttpContext context, IOptions<OpsSecurityOptions> ops, BeeKingdom.Alliance.Research.RoyalSealsMigrationService migration, CancellationToken cancellationToken) =>
{
    IResult? authorization = AuthorizeOps(context, ops.Value);
    if (authorization != null)
    {
        return authorization;
    }

    IResult? migrationAuthorization = AuthorizeMigrationApply(context, ops.Value);
    if (migrationAuthorization != null)
    {
        return migrationAuthorization;
    }

    return Results.Ok(await migration.MigrateAsync(dryRun: false, cancellationToken));
});

// M043Q-CL: narrow, read-only support lookup (email -> real onboarded DisplayName only, nothing
// else) - gated by the same Ops:AdminKey already live in production for /ops/migrations/*, not by
// AdminSupportOptions (disabled in prod by design; that surface also exposes mutation endpoints,
// disproportionate for a single read). Never returns PlayerId/AccountId/status.
app.MapGet("/ops/players/lookup-display-name", (HttpContext context, string? email, IOptions<OpsSecurityOptions> ops, IAccountCredentialStore accounts) =>
{
    IResult? authorization = AuthorizeOps(context, ops.Value);
    if (authorization != null)
    {
        return authorization;
    }

    if (string.IsNullOrWhiteSpace(email))
    {
        return Results.BadRequest(new { code = "game.invalid_request", message = "game.error.invalid_request" });
    }

    return accounts.TryGetByEmail(email, out AuthenticationAccount account)
        ? Results.Ok(new { email = account.Email, displayName = account.DisplayName, isOnboarded = account.IsOnboarded })
        : Results.NotFound(new { code = "game.not_found", message = "game.error.not_found" });
});

app.MapGet("/ops/migrations/rollback-plan", (HttpContext context, IOptions<OpsSecurityOptions> ops) =>
{
    IResult? authorization = AuthorizeOps(context, ops.Value);
    if (authorization != null)
    {
        return authorization;
    }

    return Results.Ok(new
    {
        destructive = true,
        executableByEndpoint = false,
        requiresBackup = true,
        requiresMaintenanceWindow = true,
        scripts = DatabaseRollbackCatalog.Rollbacks.Select((script, index) => new
        {
            order = index + 1,
            script.Name,
            script.Sql
        }).ToArray()
    });
});

app.MapGet("/ops/monitoring", (HttpContext context, IOptions<OpsSecurityOptions> ops, IHostEnvironment environment, IServerClock clock, IConfiguration configuration, MigrationDiagnostics migrations) =>
{
    IResult? authorization = AuthorizeOps(context, ops.Value);
    if (authorization != null)
    {
        return authorization;
    }

    return Results.Ok(new
    {
        service = "BeeKingdom.Server",
        environment = environment.EnvironmentName,
        serverTimeUtc = clock.UtcNow,
        persistenceProvider = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()?.Provider ?? PersistenceOptions.InMemoryProvider,
        processUptimeSeconds = Environment.TickCount64 / 1000,
        managedMemoryBytes = GC.GetTotalMemory(forceFullCollection: false),
        migrations = new
        {
            pendingChecks = migrations.PendingChecks,
            applyAttempts = migrations.ApplyAttempts,
            appliedScripts = migrations.AppliedScripts,
            failures = migrations.Failures,
            lastScript = migrations.LastScript,
            lastFailure = migrations.LastFailure,
            lastFailureUtc = migrations.LastFailureUtc,
            lastSuccessUtc = migrations.LastSuccessUtc
        }
    });
});

app.MapGet("/ops/readiness", (HttpContext context, IOptions<OpsSecurityOptions> ops, IHostEnvironment environment, IServerClock clock, IConfiguration configuration, IOptions<BeeKingdomServerHostProfile> serverProfile, IOptions<SqlServerOptions> sqlServer) =>
{
    IResult? authorization = AuthorizeOps(context, ops.Value);
    if (authorization != null)
    {
        return authorization;
    }

    string provider = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()?.Provider ?? PersistenceOptions.InMemoryProvider;
    bool usesSqlServer = string.Equals(provider, PersistenceOptions.SqlServerProvider, StringComparison.OrdinalIgnoreCase);
    SqlServerOptions sql = sqlServer.Value;
    string runtimeConnectionName = string.IsNullOrWhiteSpace(sql.RuntimeConnectionStringName)
        ? sql.ConnectionStringName
        : sql.RuntimeConnectionStringName;
    string migrationConnectionName = string.IsNullOrWhiteSpace(sql.MigrationConnectionStringName)
        ? sql.ConnectionStringName
        : sql.MigrationConnectionStringName;
    bool runtimeConnectionConfigured = HasRuntimeSqlConnectionString(configuration, sql);
    bool migrationConnectionConfigured = HasMigrationSqlConnectionString(configuration, sql);
    bool adminKeyConfigured = IsAdminKeyConfigured(ops.Value);
    bool migrationKeyConfigured = IsMigrationApplyKeyConfigured(ops.Value);
    bool migrationKeyDistinct = !ops.Value.RequireAdminKey
        || !migrationKeyConfigured
        || AreOperationKeysDistinct(ops.Value);

    string[] blockers = BuildReadinessBlockers(
        usesSqlServer,
        runtimeConnectionConfigured,
        migrationConnectionConfigured,
        ops.Value.RequireAdminKey,
        adminKeyConfigured,
        ops.Value.RequireMigrationApplyKey,
        migrationKeyConfigured,
        migrationKeyDistinct);

    return Results.Ok(new
    {
        service = "BeeKingdom.Server",
        environment = environment.EnvironmentName,
        serverTimeUtc = clock.UtcNow,
        ready = blockers.Length == 0,
        blockers,
        hosting = new
        {
            model = serverProfile.Value.HostingModel,
            targetOperatingSystem = serverProfile.Value.TargetOperatingSystem,
            sqlServerRole = serverProfile.Value.SqlServerRole,
            iisExpected = string.Equals(serverProfile.Value.HostingModel, "IIS", StringComparison.OrdinalIgnoreCase),
            httpsRequiredAtEdge = environment.IsProduction()
        },
        persistence = new
        {
            provider,
            sqlServerEnabled = usesSqlServer
        },
        sqlServer = new
        {
            databaseName = sql.DatabaseName,
            runtimeConnectionStringName = runtimeConnectionName,
            runtimeConnectionStringConfigured = runtimeConnectionConfigured,
            migrationConnectionStringName = migrationConnectionName,
            migrationConnectionStringConfigured = migrationConnectionConfigured,
            runtimeAndMigrationIdentitiesSeparated = !string.Equals(runtimeConnectionName, migrationConnectionName, StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(sql.MigrationConnectionString)
                || !string.IsNullOrWhiteSpace(sql.RuntimeConnectionString)
        },
        operations = new
        {
            requireAdminKey = ops.Value.RequireAdminKey,
            adminKeyConfigured,
            requireMigrationApplyKey = ops.Value.RequireMigrationApplyKey,
            migrationApplyKeyConfigured = migrationKeyConfigured,
            migrationApplyKeyDistinctFromAdminKey = migrationKeyDistinct,
            monitoringSecured = ops.Value.RequireAdminKey && adminKeyConfigured,
            rollbackPlanSecured = ops.Value.RequireAdminKey && adminKeyConfigured,
            migrationApplySecured = ops.Value.RequireAdminKey
                && adminKeyConfigured
                && ops.Value.RequireMigrationApplyKey
                && migrationKeyConfigured
                && migrationKeyDistinct
        }
    });
});

app.MapGet("/ops/sql-production-dry-run", (HttpContext context, IOptions<OpsSecurityOptions> ops, IHostEnvironment environment, IServerClock clock, IConfiguration configuration, IOptions<BeeKingdomServerHostProfile> serverProfile, IOptions<SqlServerOptions> sqlServer, IOptions<SqlProductionDryRunOptions> dryRun) =>
{
    IResult? authorization = AuthorizeOps(context, ops.Value);
    if (authorization != null)
    {
        return authorization;
    }

    string provider = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()?.Provider ?? PersistenceOptions.InMemoryProvider;
    bool usesSqlServer = string.Equals(provider, PersistenceOptions.SqlServerProvider, StringComparison.OrdinalIgnoreCase);
    SqlServerOptions sql = sqlServer.Value;
    SqlProductionDryRunOptions dryRunOptions = dryRun.Value;
    string runtimeConnectionName = string.IsNullOrWhiteSpace(sql.RuntimeConnectionStringName)
        ? sql.ConnectionStringName
        : sql.RuntimeConnectionStringName;
    string migrationConnectionName = string.IsNullOrWhiteSpace(sql.MigrationConnectionStringName)
        ? sql.ConnectionStringName
        : sql.MigrationConnectionStringName;
    bool runtimeConnectionConfigured = HasRuntimeSqlConnectionString(configuration, sql);
    bool migrationConnectionConfigured = HasMigrationSqlConnectionString(configuration, sql);
    bool identitiesSeparated = !string.Equals(runtimeConnectionName, migrationConnectionName, StringComparison.OrdinalIgnoreCase)
        || !string.IsNullOrWhiteSpace(sql.MigrationConnectionString)
        || !string.IsNullOrWhiteSpace(sql.RuntimeConnectionString);
    bool backupEvidenceConfigured = !dryRunOptions.RequireBackupEvidence || !string.IsNullOrWhiteSpace(dryRunOptions.BackupEvidenceReference);
    bool maintenanceWindowConfigured = !dryRunOptions.RequireMaintenanceWindow || !string.IsNullOrWhiteSpace(dryRunOptions.MaintenanceWindowReference);
    bool adminKeyConfigured = IsAdminKeyConfigured(ops.Value);
    bool migrationKeyConfigured = IsMigrationApplyKeyConfigured(ops.Value);
    bool migrationKeyDistinct = !ops.Value.RequireAdminKey
        || !migrationKeyConfigured
        || AreOperationKeysDistinct(ops.Value);

    string[] blockers = BuildSqlProductionDryRunBlockers(
        usesSqlServer,
        runtimeConnectionConfigured,
        migrationConnectionConfigured,
        identitiesSeparated,
        ops.Value.RequireAdminKey,
        adminKeyConfigured,
        ops.Value.RequireMigrationApplyKey,
        migrationKeyConfigured,
        migrationKeyDistinct,
        backupEvidenceConfigured,
        maintenanceWindowConfigured,
        dryRunOptions.RollbackPlanAcknowledged);

    return Results.Ok(new
    {
        service = "BeeKingdom.Server",
        environment = environment.EnvironmentName,
        serverTimeUtc = clock.UtcNow,
        targetHost = dryRunOptions.TargetHost,
        readyForDryRun = blockers.Length == 0,
        destructive = false,
        appliesMigrations = false,
        executesRollback = false,
        publishesDeployment = false,
        blockers,
        hosting = new
        {
            model = serverProfile.Value.HostingModel,
            targetOperatingSystem = serverProfile.Value.TargetOperatingSystem,
            productionTargetMatchesConfiguredHost = string.Equals(dryRunOptions.TargetHost, "104.129.128.136", StringComparison.Ordinal)
        },
        persistence = new
        {
            provider,
            sqlServerEnabled = usesSqlServer
        },
        sqlServer = new
        {
            databaseName = sql.DatabaseName,
            runtimeConnectionStringName = runtimeConnectionName,
            runtimeConnectionStringConfigured = runtimeConnectionConfigured,
            migrationConnectionStringName = migrationConnectionName,
            migrationConnectionStringConfigured = migrationConnectionConfigured,
            runtimeAndMigrationIdentitiesSeparated = identitiesSeparated
        },
        operations = new
        {
            requireAdminKey = ops.Value.RequireAdminKey,
            adminKeyConfigured,
            requireMigrationApplyKey = ops.Value.RequireMigrationApplyKey,
            migrationApplyKeyConfigured = migrationKeyConfigured,
            migrationApplyKeyDistinctFromAdminKey = migrationKeyDistinct
        },
        backup = new
        {
            required = dryRunOptions.RequireBackupEvidence,
            evidenceReferenceConfigured = backupEvidenceConfigured
        },
        rollback = new
        {
            endpointExecutable = false,
            planAcknowledged = dryRunOptions.RollbackPlanAcknowledged,
            scriptCount = DatabaseRollbackCatalog.Rollbacks.Count
        },
        maintenance = new
        {
            required = dryRunOptions.RequireMaintenanceWindow,
            windowReferenceConfigured = maintenanceWindowConfigured
        },
        accountSessionReadModelPreparation = new
        {
            accountsTablePlanned = DatabaseCatalog.Migrations.Any(script => script.Sql.Contains("dbo.Accounts", StringComparison.OrdinalIgnoreCase)),
            credentialsTablePlanned = DatabaseCatalog.Migrations.Any(script => script.Sql.Contains("dbo.AuthenticationAccounts", StringComparison.OrdinalIgnoreCase)),
            sessionsTablePlanned = DatabaseCatalog.Migrations.Any(script => script.Sql.Contains("dbo.AuthenticationSessions", StringComparison.OrdinalIgnoreCase)),
            coloniesTablePlanned = DatabaseCatalog.Migrations.Any(script => script.Sql.Contains("dbo.Colonies", StringComparison.OrdinalIgnoreCase)),
            snapshotsTablePlanned = DatabaseCatalog.Migrations.Any(script => script.Sql.Contains("dbo.ColonySnapshots", StringComparison.OrdinalIgnoreCase))
        }
    });
});

app.MapPost("/auth/login", async (HttpContext context, AuthenticationManager authentication, AuthenticationRequest request, IOptions<AccountSessionReadinessOptions> readiness, IHostEnvironment environment, CancellationToken cancellationToken) =>
{
    if (environment.IsProduction() && (!readiness.Value.SessionCreationAllowed || !readiness.Value.TokenIssuanceAllowed))
        return AuthUnavailable();
    if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || request.Email.Length > 320 || request.Password.Length > 512 || string.IsNullOrWhiteSpace(request.ClientVersion) || request.ClientVersion.Length > 64)
        return AuthError(StatusCodes.Status400BadRequest, "auth.invalid_request");
    string connectionIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    AuthenticationResult result = await authentication.Authenticate(request with { IpAddress = connectionIp }, cancellationToken);
    if (result.Succeeded) return Results.Ok(result);
    return result.ErrorCode switch
    {
        "max_sessions_reached" => AuthError(StatusCodes.Status409Conflict, "auth.session_limit"),
        "account_locked" => AuthError(StatusCodes.Status429TooManyRequests, "auth.rate_limited"),
        _ => AuthError(StatusCodes.Status401Unauthorized, "auth.invalid_credentials")
    };
});

app.MapPost("/auth/login/google", async (HttpContext context, AuthenticationManager authentication, GoogleLoginHttpRequest request, IOptions<AccountSessionReadinessOptions> readiness, IHostEnvironment environment, CancellationToken cancellationToken) =>
{
    if (environment.IsProduction() && (!readiness.Value.SessionCreationAllowed || !readiness.Value.TokenIssuanceAllowed))
        return AuthUnavailable();
    if (request is null || string.IsNullOrWhiteSpace(request.AuthorizationCode) || string.IsNullOrWhiteSpace(request.CodeVerifier) ||
        string.IsNullOrWhiteSpace(request.RedirectUri) || string.IsNullOrWhiteSpace(request.ClientVersion) || request.ClientVersion.Length > 64)
        return AuthError(StatusCodes.Status400BadRequest, "auth.invalid_request");
    string connectionIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    GoogleAuthenticationRequest googleRequest = new(
        request.AuthorizationCode,
        request.CodeVerifier,
        request.RedirectUri,
        request.ClientVersion,
        connectionIp,
        request.DeviceIdentifier,
        request.Region);
    AuthenticationResult result = await authentication.AuthenticateWithGoogle(googleRequest, cancellationToken);
    if (result.Succeeded) return Results.Ok(result);
    return result.ErrorCode switch
    {
        "max_sessions_reached" => AuthError(StatusCodes.Status409Conflict, "auth.session_limit"),
        "account_disabled" => AuthError(StatusCodes.Status403Forbidden, "auth.account_disabled"),
        _ => AuthError(StatusCodes.Status401Unauthorized, "auth.google_sign_in_failed")
    };
});

app.MapPost("/auth/display-name", (HttpContext context, AuthenticationManager authentication, IAccountCredentialStore credentials, SetDisplayNameHttpRequest request, IOptions<ServerIdentityOptions> serverIdentity) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return AuthError(StatusCodes.Status401Unauthorized, "auth.session_required");

    string displayName = (request?.DisplayName ?? string.Empty).Trim();
    if (displayName.Length is < 3 or > 20)
        return AuthError(StatusCodes.Status400BadRequest, "auth.display_name_invalid");

    Guid worldId = Guid.TryParse(serverIdentity.Value.DefaultWorldId, out Guid parsedWorldId) ? parsedWorldId : Guid.Empty;
    if (credentials.IsDisplayNameTaken(worldId, displayName, auth.AccountId))
        return AuthError(StatusCodes.Status409Conflict, "auth.display_name_taken");

    if (!credentials.TryGetByAccountId(auth.AccountId, out AuthenticationAccount existing))
        return AuthError(StatusCodes.Status404NotFound, "auth.account_not_found");

    credentials.Save(existing with { DisplayName = displayName, IsOnboarded = true });
    return Results.Ok(new { displayName, isOnboarded = true });
});

app.MapPost("/auth/refresh", async (AuthenticationManager authentication, RefreshTokenRequest request, IOptions<AccountSessionReadinessOptions> readiness, IHostEnvironment environment, CancellationToken cancellationToken) =>
{
    if (environment.IsProduction() && !readiness.Value.TokenIssuanceAllowed)
        return AuthUnavailable();
    if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken) || request.RefreshToken.Length > 8192)
        return AuthError(StatusCodes.Status400BadRequest, "auth.invalid_request");
    AuthenticationTokenPair? tokens = await authentication.RefreshToken(request.RefreshToken, cancellationToken);
    return tokens == null ? AuthError(StatusCodes.Status401Unauthorized, "auth.session_required") : Results.Ok(tokens);
});

app.MapPost("/auth/validate", (AuthenticationManager authentication, AccessTokenRequest request) =>
{
    TokenValidationResult result = authentication.ValidateToken(request.AccessToken);
    return result.IsValid ? Results.Ok(result) : Results.Unauthorized();
});

app.MapPost("/auth/logout", (HttpContext context, AuthenticationManager authentication) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return AuthError(StatusCodes.Status401Unauthorized, "auth.session_required");
    return authentication.Logout(auth.SessionId)
        ? Results.Ok(new { revoked = true })
        : AuthError(StatusCodes.Status401Unauthorized, "auth.session_required");
});

app.MapPost("/accounts", (AccountManager accounts, CreateAccountRequest request, IOptions<AccountSessionReadinessOptions> readiness, IHostEnvironment environment) =>
{
    if (environment.IsProduction() && !readiness.Value.AccountCreationAllowed)
        return AuthUnavailable();
    return Results.Ok(accounts.CreateAccount(request));
});

// Local-dev-only helper: no HTTP path otherwise exists to create login credentials
// (IAccountCredentialStore.CreateEmailAccount is only ever called from server-side test
// fixtures). Never reachable outside Development with DevTools:AllowDevAccountSeeding=true.
app.MapPost("/dev/seed-account", (IHostEnvironment environment, IOptions<DevToolsOptions> devTools, IAccountCredentialStore credentials, DevSeedAccountRequest request) =>
{
    if (environment.IsProduction() || !devTools.Value.AllowDevAccountSeeding) return Results.NotFound();
    if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { error = "invalid_request" });
    if (credentials.TryGetByEmail(request.Email, out _)) return Results.Conflict(new { error = "already_exists" });
    AuthenticationAccount account = credentials.CreateEmailAccount(request.Email, request.Password);
    return Results.Ok(new { playerId = account.PlayerId.Value, email = account.Email });
});

app.MapGet("/accounts/{accountId:guid}", (AccountManager accounts, Guid accountId) =>
{
    AccountRecord? account = accounts.GetAccount(accountId);
    return account == null ? Results.NotFound() : Results.Ok(account);
});

app.MapPost("/accounts/{accountId:guid}/profile", (AccountManager accounts, Guid accountId, UpdateAccountProfileRequest request) =>
{
    return Results.Ok(accounts.UpdateProfile(accountId, request.DisplayName, request.Language, request.TimeZone, request.Country));
});

app.MapPost("/accounts/{accountId:guid}/preferences", (AccountManager accounts, Guid accountId, AccountPreferences preferences) =>
{
    return Results.Ok(accounts.UpdatePreferences(accountId, preferences));
});

// In-game moderator management: reachable from the client (Jeff's requirement is that
// naming moderators happens "à même le jeu", not through the external /admin/ui tool).
// Both endpoints resolve the CALLER's own role from their authenticated AccountId
// (TokenValidationResult.AccountId, the same identity AuthenticateGameRequest uses
// everywhere else) and require Role == Admin server-side - never from client-supplied
// input. This is the opposite of ChatContracts.RequesterAllianceRole/
// LocalChatAudienceResolver, which trusts a client-supplied role string for chat audience
// resolution; that field must never be reused as an authorization signal for anything
// real, which is exactly why role authorization here is re-derived from the account
// record on every call instead. Responses only ever expose AccountRoleLookupResult, never
// the raw AuthenticationAccount, so PasswordHash can never leak through this surface.
app.MapGet("/accounts/v1/role/lookup", (HttpContext context, string query, AuthenticationManager authentication, IAccountCredentialStore credentials) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!credentials.TryGetByAccountId(auth.AccountId, out AuthenticationAccount caller) || caller.Role != AccountRole.Admin) return GameError(403, "game.forbidden", "game.error.forbidden");
    if (string.IsNullOrWhiteSpace(query) || query.Length > 128) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    var results = credentials.SearchByDisplayName(query).Select(a => new AccountRoleLookupResult(a.AccountId, a.DisplayName, a.Email, a.Role)).ToArray();
    return Results.Ok(results);
});

app.MapPost("/accounts/v1/role/assign", (HttpContext context, AuthenticationManager authentication, IAccountCredentialStore credentials, AccountRoleAssignHttpRequest request) =>
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!credentials.TryGetByAccountId(auth.AccountId, out AuthenticationAccount caller) || caller.Role != AccountRole.Admin) return GameError(403, "game.forbidden", "game.error.forbidden");
    // Admin can only be granted through the bootstrap admin endpoint, never from here -
    // otherwise any Admin could mint more Admins from inside the game.
    if (request is null || request.Role is not (AccountRole.Player or AccountRole.Moderator)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    if (!credentials.TryGetByAccountId(request.TargetAccountId, out AuthenticationAccount target)) return GameError(404, "game.not_found", "game.error.not_found");
    credentials.Save(target with { Role = request.Role });
    return Results.Ok(new AccountRoleLookupResult(target.AccountId, target.DisplayName, target.Email, request.Role));
});

app.MapPost("/gateway/connections", (GatewayManager gateway, GatewayConnectionRequest request) =>
{
    return Results.Ok(gateway.AcceptConnection(request));
});

app.MapPost("/gateway/connections/{connectionId:guid}/authenticate", (GatewayManager gateway, Guid connectionId, AccessTokenRequest request) =>
{
    return Results.Ok(gateway.AuthenticateSession(connectionId, request.AccessToken));
});

app.MapPost("/gateway/connections/{connectionId:guid}/disconnect", (GatewayManager gateway, Guid connectionId) =>
{
    return gateway.Disconnect(connectionId) ? Results.Ok() : Results.NotFound();
});

app.MapGet("/gateway/statistics", (GatewayManager gateway) =>
{
    return Results.Ok(gateway.GetGatewayStatistics());
});

app.MapPost("/colonies", (ColonyManager colonies, CreateColonyHttpRequest request) =>
{
    return Results.Ok(colonies.CreateColony(new CreateColonyRequest(new PlayerId(request.PlayerId), request.WorldId, request.HiveName, new BeeId(request.QueenId))));
});

app.MapGet("/colonies/{colonyId:guid}", (ColonyManager colonies, Guid colonyId) =>
{
    IReadOnlyList<ColonyRecord> matches = colonies.QueryColony(new ColonyQuery());
    ColonyRecord? colony = matches.FirstOrDefault(record => record.Profile.ColonyId.Value == colonyId);
    return colony == null ? Results.NotFound() : Results.Ok(colony);
});

app.MapPost("/colonies/{colonyId:guid}/load", (ColonyManager colonies, Guid colonyId) =>
{
    return Results.Ok(colonies.LoadColony(new ColonyId(colonyId)));
});

app.MapPost("/colonies/{colonyId:guid}/save", (ColonyManager colonies, Guid colonyId, SaveColonyHttpRequest request) =>
{
    return Results.Ok(colonies.SaveColony(new ColonyId(colonyId), request.Kind));
});

app.MapPost("/colonies/{colonyId:guid}/rename", (ColonyManager colonies, Guid colonyId, RenameColonyHttpRequest request) =>
{
    return Results.Ok(colonies.RenameColony(new ColonyId(colonyId), request.HiveName));
});

app.MapPost("/colonies/{colonyId:guid}/status", (ColonyManager colonies, Guid colonyId, ChangeColonyStatusHttpRequest request) =>
{
    return Results.Ok(colonies.SetColonyStatus(new ColonyId(colonyId), request.Status));
});

app.MapDelete("/colonies/{colonyId:guid}", (ColonyManager colonies, Guid colonyId) =>
{
    return Results.Ok(colonies.DeleteColony(new ColonyId(colonyId)));
});

app.MapGet("/colonies/{colonyId:guid}/statistics", (ColonyManager colonies, Guid colonyId) =>
{
    return Results.Ok(colonies.GetColonyStatistics(new ColonyId(colonyId)));
});

app.MapPost("/simulation/start", (SimulationManager simulation) =>
{
    simulation.StartSimulation();
    return Results.Ok(new { state = simulation.State.ToString() });
});

app.MapPost("/simulation/stop", (SimulationManager simulation) =>
{
    simulation.StopSimulation();
    return Results.Ok(new { state = simulation.State.ToString() });
});

app.MapPost("/simulation/pause", (SimulationManager simulation) =>
{
    simulation.PauseSimulation();
    return Results.Ok(new { state = simulation.State.ToString() });
});

app.MapPost("/simulation/resume", (SimulationManager simulation) =>
{
    simulation.ResumeSimulation();
    return Results.Ok(new { state = simulation.State.ToString() });
});

app.MapPost("/simulation/tick", (SimulationManager simulation) =>
{
    return Results.Ok(simulation.ExecuteTick());
});

app.MapPost("/simulation/fast-forward", (SimulationManager simulation, FastForwardHttpRequest request) =>
{
    return Results.Ok(simulation.FastForward(request.Ticks));
});

app.MapPost("/simulation/colonies/{colonyId:guid}/load", (SimulationManager simulation, Guid colonyId) =>
{
    return Results.Ok(simulation.LoadColony(new ColonyId(colonyId)));
});

app.MapPost("/simulation/colonies/{colonyId:guid}/unload", (SimulationManager simulation, Guid colonyId) =>
{
    return simulation.UnloadColony(new ColonyId(colonyId)) ? Results.Ok() : Results.NotFound();
});

app.MapGet("/simulation/diagnostics", (SimulationManager simulation) =>
{
    return Results.Ok(simulation.Diagnostics);
});

static IResult? AuthorizeOps(HttpContext context, OpsSecurityOptions options)
{
    if (!options.RequireAdminKey)
    {
        return null;
    }

    if (!IsAdminKeyConfigured(options))
    {
        return Results.Problem("Operations endpoints require Ops:AdminKey or Ops:AdminKeySha256 when Ops:RequireAdminKey is enabled.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!context.Request.Headers.TryGetValue("X-BeeKingdom-Admin-Key", out Microsoft.Extensions.Primitives.StringValues provided))
    {
        return Results.Unauthorized();
    }

    return VerifyProvidedSecret(provided.ToString(), options.AdminKey, options.AdminKeySha256) ? null : Results.Unauthorized();
}

static IResult? AuthorizeAdminSupport(HttpContext context, AdminSupportOptions options)
{
    if (!options.Enabled)
    {
        return GameError(StatusCodes.Status503ServiceUnavailable, "game.unavailable", "Admin support is not enabled.");
    }

    if (string.IsNullOrWhiteSpace(options.Key) && string.IsNullOrWhiteSpace(options.KeySha256))
    {
        return Results.Problem("Admin support endpoints require AdminSupport:Key or AdminSupport:KeySha256 when AdminSupport:Enabled is true.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!context.Request.Headers.TryGetValue("X-BeeKingdom-Support-Key", out Microsoft.Extensions.Primitives.StringValues provided))
    {
        return Results.Unauthorized();
    }

    return VerifyProvidedSecret(provided.ToString(), options.Key, options.KeySha256) ? null : Results.Unauthorized();
}

static TokenValidationResult AuthenticateChatRequest(HttpContext context, AuthenticationManager authentication)
{
    string? authorization = context.Request.Headers.Authorization.FirstOrDefault();
    if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return TokenValidationResult.Invalid("missing_bearer_token");
    }

    string token = authorization["Bearer ".Length..];
    return BearerTokenSyntax.IsValid(token)
        ? authentication.ValidateToken(token)
        : TokenValidationResult.Invalid("token_invalid");
}

static TokenValidationResult AuthenticateGameRequest(HttpContext context, AuthenticationManager authentication)
{
    string? authorization = context.Request.Headers.Authorization.FirstOrDefault();
    if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return TokenValidationResult.Invalid("missing_bearer_token");
    string token = authorization["Bearer ".Length..];
    return BearerTokenSyntax.IsValid(token) ? authentication.ValidateToken(token) : TokenValidationResult.Invalid("token_invalid");
}

static bool TryParseChatResourceId(string value, out Guid id)
{
    id = Guid.Empty;
    return value.Length is >= 1 and <= 256
        && value.AsSpan().Trim().Length == value.Length
        && Guid.TryParse(value, out id);
}

static bool TryParseGameResourceId(string value, out Guid id)
{
    id = Guid.Empty;
    return value.Length is >= 1 and <= 256
        && value.AsSpan().Trim().Length == value.Length
        && Guid.TryParse(value, out id);
}

static IResult ExecuteChat(HttpContext context, Func<IResult> action)
{
    try
    {
        return action();
    }
    catch (UnauthorizedAccessException)
    {
        return ChatError(403,"chat.forbidden","chat.error.forbidden");
    }
    catch (KeyNotFoundException)
    {
        return ChatError(404,"chat.not_found","chat.error.not_found");
    }
    catch (ArgumentException)
    {
        return ChatError(400,"chat.invalid_request","chat.error.invalid_request");
    }
    catch (InvalidOperationException exception) when (string.Equals(exception.Message, "chat_disabled", StringComparison.Ordinal))
    {
        context.Response.Headers.RetryAfter="30"; return ChatError(503,"chat.unavailable","chat.error.unavailable",30);
    }
    catch (InvalidOperationException exception) when (string.Equals(exception.Message, "idempotency_conflict", StringComparison.Ordinal))
    {
        return ChatError(409,"chat.idempotency_conflict","chat.error.idempotency_conflict");
    }
    catch (InvalidOperationException exception) when (string.Equals(exception.Message, "chat_rate_limited", StringComparison.Ordinal))
    {
        context.Response.Headers.RetryAfter="60"; return ChatError(429,"chat.rate_limited","chat.error.rate_limited",60);
    }
}

static async Task<IResult> ExecuteChatAsync(HttpContext context, Func<Task<IResult>> action)
{
    try
    {
        return await action();
    }
    catch (UnauthorizedAccessException)
    {
        return ChatError(403,"chat.forbidden","chat.error.forbidden");
    }
    catch (KeyNotFoundException)
    {
        return ChatError(404,"chat.not_found","chat.error.not_found");
    }
    catch (ArgumentException)
    {
        return ChatError(400,"chat.invalid_request","chat.error.invalid_request");
    }
    catch (InvalidOperationException exception) when (string.Equals(exception.Message, "chat_disabled", StringComparison.Ordinal))
    {
        context.Response.Headers.RetryAfter="30"; return ChatError(503,"chat.unavailable","chat.error.unavailable",30);
    }
    catch (InvalidOperationException exception) when (string.Equals(exception.Message, "idempotency_conflict", StringComparison.Ordinal))
    {
        return ChatError(409,"chat.idempotency_conflict","chat.error.idempotency_conflict");
    }
    catch (InvalidOperationException exception) when (string.Equals(exception.Message, "chat_rate_limited", StringComparison.Ordinal))
    {
        context.Response.Headers.RetryAfter="60"; return ChatError(429,"chat.rate_limited","chat.error.rate_limited",60);
    }
}

static IResult ChatError(int statusCode,string code,string message,int? retryAfterSeconds=null)
    => Results.Json(new ChatTranslationError(code,message,retryAfterSeconds),statusCode:statusCode);

// M041-CL: AllianceService's exception vocabulary (see its top-of-file comment) mapped to HTTP.
// InvalidOperationException.Message is a stable machine code forwarded as-is to the client for
// codes not explicitly enumerated here, so a new domain error added to the service later doesn't
// require touching this wrapper too - it just surfaces as a generic 409 with the real code string.
static IResult ExecuteAlliance(Func<IResult> action)
{
    try
    {
        return action();
    }
    catch (UnauthorizedAccessException)
    {
        return AllianceError(403, "alliance.forbidden");
    }
    catch (KeyNotFoundException)
    {
        return AllianceError(404, "alliance.not_found");
    }
    catch (ArgumentException)
    {
        return AllianceError(400, "alliance.invalid_request");
    }
    catch (InvalidOperationException exception) when (exception.Message == "alliance_disabled")
    {
        return AllianceError(503, "alliance.unavailable");
    }
    catch (InvalidOperationException exception)
    {
        return AllianceError(409, "alliance." + exception.Message);
    }
}

static IResult AllianceError(int statusCode, string code)
    => Results.Json(new AllianceErrorEnvelope(code), statusCode: statusCode);

// M0??-CL: News admin gate - exact same pattern as /accounts/v1/role/lookup (AuthenticateGameRequest
// then AccountRole.Admin check via IAccountCredentialStore). `caller` is only meaningful when this
// returns null (authorized); callers that don't need the account can discard it.
static IResult? AuthorizeNewsAdmin(HttpContext context, AuthenticationManager authentication, IAccountCredentialStore credentials, out AuthenticationAccount caller)
{
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid)
    {
        caller = default;
        return GameError(401, "news.session_required", "news.error.session_required");
    }

    if (!credentials.TryGetByAccountId(auth.AccountId, out caller) || caller.Role != AccountRole.Admin)
    {
        return NewsError(403, "forbidden");
    }

    return null;
}

static int ParseNewsCursor(string? cursor) => int.TryParse(cursor, out int offset) && offset >= 0 ? offset : 0;

static IResult NewsError(int statusCode, string code)
    => Results.Json(new AllianceErrorEnvelope("news." + code), statusCode: statusCode);

static IResult NewsCommandError(string code) => NewsError(code switch
{
    "not_found" => 404,
    "slug_taken" => 409,
    "locale_incomplete" => 409,
    "invalid_request" => 400,
    _ => 400
}, code);

// News, like AllianceResearch, returns result records (Succeeded/Code) rather than throwing for
// expected domain rejections (slug taken, locale incomplete, not found) - this wrapper only needs
// to catch the one exception NewsService DOES throw (feature disabled).
static async Task<IResult> ExecuteNewsAsync(Func<Task<IResult>> action)
{
    try
    {
        return await action();
    }
    catch (InvalidOperationException exception) when (exception.Message == "news_disabled")
    {
        return NewsError(503, "unavailable");
    }
}

// M045-CL: AllianceHelpService returns result records (Succeeded/Code) rather than throwing for
// expected domain rejections (already helped, request full, wrong alliance, etc.) - this wrapper
// only needs to catch the one exception it DOES throw (feature disabled) plus the generic
// unauthorized/not-found/invalid-request vocabulary shared with the rest of Alliance.
static async Task<IResult> ExecuteAllianceHelpAsync(Func<Task<IResult>> action)
{
    try
    {
        return await action();
    }
    catch (UnauthorizedAccessException)
    {
        return AllianceError(403, "alliance.forbidden");
    }
    catch (KeyNotFoundException)
    {
        return AllianceError(404, "alliance.not_found");
    }
    catch (ArgumentException)
    {
        return AllianceError(400, "alliance.invalid_request");
    }
    catch (InvalidOperationException exception) when (exception.Message == "alliance_help_disabled")
    {
        return AllianceError(503, "alliance.unavailable");
    }
}

static IResult AllianceHelpError(string code) => AllianceError(CodeToStatus(code), "alliance.help." + code);

static int CodeToStatus(string code) => code switch
{
    "not_a_member" or "different_alliance" or "cannot_help_own_request" or "hive_not_owned" => 403,
    "not_found" or "operation_not_found" or "hive_not_found" => 404,
    "invalid_request" or "invalid_category" => 400,
    _ => 409
};

// M051-CL: AllianceResearchService returns result records (Succeeded/Code) rather than throwing
// for expected domain rejections (locked technology, already completed, insufficient resources) -
// this wrapper only needs to catch the one exception it DOES throw (feature disabled) plus the
// generic unauthorized/not-found/invalid-request vocabulary shared with the rest of Alliance.
static async Task<IResult> ExecuteAllianceResearchAsync(Func<Task<IResult>> action)
{
    try
    {
        return await action();
    }
    catch (UnauthorizedAccessException)
    {
        return AllianceError(403, "alliance.forbidden");
    }
    catch (KeyNotFoundException)
    {
        return AllianceError(404, "alliance.not_found");
    }
    catch (ArgumentException)
    {
        return AllianceError(400, "alliance.invalid_request");
    }
    catch (InvalidOperationException exception) when (exception.Message == "not_a_member")
    {
        return AllianceError(403, "alliance.research.not_a_member");
    }
    catch (InvalidOperationException exception) when (exception.Message == "alliance_research_disabled")
    {
        return AllianceError(503, "alliance.unavailable");
    }
}

static IResult AllianceResearchError(string code) => AllianceError(AllianceResearchCodeToStatus(code), "alliance.research." + code);

static int AllianceResearchCodeToStatus(string code) => code switch
{
    "not_a_member" or "hive_not_owned" or "not_authorized" => 403,
    "technology_not_found" or "hive_not_found" or "item_not_found" => 404,
    "invalid_request" or "invalid_resource" => 400,
    "technology_locked" or "technology_completed" or "insufficient_resources" or "not_the_funding_target"
        or "technology_ready" or "technology_researching" or "technology_completed_funding_for_resource"
        or "funding_incomplete" or "slot_occupied" or "already_researching" or "technology_already_researching"
        or "no_speedup_available" or "technology_not_researching" => 409,
    _ => 409
};

static ResearchReadSnapshot BuildResearchSnapshot(PlayerHiveState state, DateTimeOffset now, string catalogVersion, IReadOnlyList<string> configuredCatalog)
{
    var offers = HiveOperationService.ResearchCatalog.Where(x=>configuredCatalog.Contains(x.Key,StringComparer.Ordinal)).Select(x => new ResearchOffer(x.Key, x.Value.Duration, x.Value.Costs, x.Value.Effects, x.Value.Prerequisites)).Where(x => !(state.Research?.Completed.ContainsKey(x.ResearchId) ?? false)).ToArray();
    var active = state.Research?.ActiveOperation;
    var op = active is null ? null : new ResearchActiveOperation(active.OperationId, active.ResearchId, active.StartedAtUtc, active.EndsAtUtc, active.EndsAtUtc <= now ? "awaiting_completion" : "running");
    var completed = state.Research?.Completed.Values.Select(x => new ResearchCompletedEntry(x.ResearchId,x.CompletedAtUtc,x.Effects)).ToArray() ?? Array.Empty<ResearchCompletedEntry>();
    return new(state.PlayerId,state.HiveId,"living-hive-research-v1",catalogVersion,state.Revision,now,new Dictionary<string,ResourceBalance>(state.Resources),completed,offers,op);
}

static IResult GameError(int statusCode, string code, string message, int? retryAfterSeconds = null)
    => Results.Json(new GameErrorEnvelope(code, message, retryAfterSeconds), statusCode: statusCode);

static IResult? AuthorizeMigrationApply(HttpContext context, OpsSecurityOptions options)
{
    if (!options.RequireMigrationApplyKey)
    {
        return null;
    }

    if (!IsMigrationApplyKeyConfigured(options))
    {
        return Results.Problem("Migration apply endpoint requires Ops:MigrationApplyKey or Ops:MigrationApplyKeySha256 when Ops:RequireMigrationApplyKey is enabled.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (options.RequireAdminKey && !AreOperationKeysDistinct(options))
    {
        return Results.Problem("Migration apply key must be distinct from Ops:AdminKey.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!context.Request.Headers.TryGetValue("X-BeeKingdom-Migration-Key", out Microsoft.Extensions.Primitives.StringValues provided))
    {
        return Results.Unauthorized();
    }

    return VerifyProvidedSecret(provided.ToString(), options.MigrationApplyKey, options.MigrationApplyKeySha256) ? null : Results.Unauthorized();
}

static bool IsAdminKeyConfigured(OpsSecurityOptions options)
{
    return !string.IsNullOrWhiteSpace(options.AdminKey)
        || !string.IsNullOrWhiteSpace(options.AdminKeySha256);
}

static bool IsMigrationApplyKeyConfigured(OpsSecurityOptions options)
{
    return !string.IsNullOrWhiteSpace(options.MigrationApplyKey)
        || !string.IsNullOrWhiteSpace(options.MigrationApplyKeySha256);
}

static bool AreOperationKeysDistinct(OpsSecurityOptions options)
{
    string adminHash = GetConfiguredSecretHash(options.AdminKey, options.AdminKeySha256);
    string migrationHash = GetConfiguredSecretHash(options.MigrationApplyKey, options.MigrationApplyKeySha256);
    return string.IsNullOrWhiteSpace(adminHash)
        || string.IsNullOrWhiteSpace(migrationHash)
        || !FixedTimeEquals(adminHash, migrationHash);
}

static bool VerifyProvidedSecret(string provided, string configuredPlainText, string configuredSha256)
{
    if (!string.IsNullOrWhiteSpace(configuredSha256)
        && FixedTimeEquals(ComputeSha256Hex(provided), NormalizeHash(configuredSha256)))
    {
        return true;
    }

    return !string.IsNullOrWhiteSpace(configuredPlainText) && FixedTimeEquals(provided, configuredPlainText);
}

static string GetConfiguredSecretHash(string configuredPlainText, string configuredSha256)
{
    if (!string.IsNullOrWhiteSpace(configuredSha256))
    {
        return NormalizeHash(configuredSha256);
    }

    return string.IsNullOrWhiteSpace(configuredPlainText) ? string.Empty : ComputeSha256Hex(configuredPlainText);
}

static string ComputeSha256Hex(string value)
{
    byte[] bytes = Encoding.UTF8.GetBytes(value);
    byte[] hash = SHA256.HashData(bytes);
    return Convert.ToHexString(hash).ToLowerInvariant();
}

static string NormalizeHash(string value)
{
    return value.Trim().Replace(":", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
}

static bool HasRuntimeSqlConnectionString(IConfiguration configuration, SqlServerOptions options)
{
    if (!string.IsNullOrWhiteSpace(options.RuntimeConnectionString))
    {
        return true;
    }

    if (!string.IsNullOrWhiteSpace(options.RuntimeConnectionStringName)
        && !string.IsNullOrWhiteSpace(configuration.GetConnectionString(options.RuntimeConnectionStringName)))
    {
        return true;
    }

    return !string.IsNullOrWhiteSpace(configuration.GetConnectionString(options.ConnectionStringName))
        || !string.IsNullOrWhiteSpace(options.ConnectionString);
}

static bool HasMigrationSqlConnectionString(IConfiguration configuration, SqlServerOptions options)
{
    if (!string.IsNullOrWhiteSpace(options.MigrationConnectionString))
    {
        return true;
    }

    if (!string.IsNullOrWhiteSpace(options.MigrationConnectionStringName)
        && !string.IsNullOrWhiteSpace(configuration.GetConnectionString(options.MigrationConnectionStringName)))
    {
        return true;
    }

    return HasRuntimeSqlConnectionString(configuration, options);
}

static string NormalizeGuidString(string value)
{
    return Guid.TryParse(value, out Guid parsed) ? parsed.ToString("D") : value;
}

static string[] BuildAccountSessionReadinessBlockers(AccountSessionReadinessOptions state, bool usesSqlServer, bool runtimeConnectionConfigured, bool migrationConnectionConfigured)
{
    List<string> blockers = [];

    if (state.RequiresProductionRouteProof)
    {
        blockers.Add("Production route proof is required before live account/session claims.");
    }

    if (!usesSqlServer)
    {
        blockers.Add("SQL Server persistence is not selected.");
    }

    if (usesSqlServer && !runtimeConnectionConfigured)
    {
        blockers.Add("Runtime SQL connection is not configured.");
    }

    if (usesSqlServer && !migrationConnectionConfigured)
    {
        blockers.Add("Migration SQL connection is not configured.");
    }

    if (state.RequiresBackupEvidence)
    {
        blockers.Add("Backup evidence is required before production account/session activation.");
    }

    if (state.RequiresRollbackApproval)
    {
        blockers.Add("Rollback approval is required before production account/session activation.");
    }

    if (!state.AccountCreationAllowed)
    {
        blockers.Add("Account creation is not allowed by readiness configuration.");
    }

    if (!state.SessionCreationAllowed)
    {
        blockers.Add("Session creation is not allowed by readiness configuration.");
    }

    if (!state.TokenIssuanceAllowed)
    {
        blockers.Add("Token issuance is not allowed by readiness configuration.");
    }

    return blockers.ToArray();
}

static WorldMapNodeModel[] BuildWorldMapNodeModels()
{
    return
    [
        new WorldMapNodeModel("HiveMapNode", "Draft", WorldScoped: true, GameServerScoped: true, ReadOnly: true, LiveClaimAllowed: false, "Hive placement placeholder for future world-map design."),
        new WorldMapNodeModel("AllianceTerritory", "Draft", WorldScoped: true, GameServerScoped: true, ReadOnly: true, LiveClaimAllowed: false, "Non-official territory boundary placeholder."),
        new WorldMapNodeModel("FlightPath", "Draft", WorldScoped: true, GameServerScoped: true, ReadOnly: true, LiveClaimAllowed: false, "Non-live route preview placeholder."),
        new WorldMapNodeModel("ResourceField", "Draft", WorldScoped: true, GameServerScoped: true, ReadOnly: true, LiveClaimAllowed: false, "Non-economic resource field placeholder."),
        new WorldMapNodeModel("Wonder", "Draft", WorldScoped: true, GameServerScoped: true, ReadOnly: true, LiveClaimAllowed: false, "Non-live landmark placeholder."),
        new WorldMapNodeModel("HostileNest", "Draft", WorldScoped: true, GameServerScoped: true, ReadOnly: true, LiveClaimAllowed: false, "Non-combat threat placeholder.")
    ];
}

static string[] BuildWorldMapReadinessBlockers(WorldMapReadinessOptions state, bool serverFirstProductionRouteProven)
{
    List<string> blockers = [];

    if (!serverFirstProductionRouteProven || !state.ProductionRouteProven)
    {
        blockers.Add("Production route proof is required before world map live claims.");
    }

    if (!state.MapGameplayEnabled)
    {
        blockers.Add("World map gameplay is not enabled.");
    }

    if (!state.LiveTerritoryEnabled)
    {
        blockers.Add("Live territory is not enabled.");
    }

    if (!state.LiveAllianceEnabled)
    {
        blockers.Add("Live alliance ownership is not enabled.");
    }

    if (!state.LiveScoutingEnabled)
    {
        blockers.Add("Live scouting is not enabled.");
    }

    if (!state.LiveWarEnabled)
    {
        blockers.Add("Live war and PvP are not enabled.");
    }

    if (!state.LiveEconomyEnabled)
    {
        blockers.Add("Live world economy is not enabled.");
    }

    if (!state.RealTimeSynchronizationEnabled)
    {
        blockers.Add("Real-time synchronization is not enabled.");
    }

    if (!state.OfficialProgressionEnabled)
    {
        blockers.Add("Official progression is not enabled.");
    }

    return blockers.ToArray();
}

static WorldRegistryEntry[] BuildWorldRegistryEntries(WorldRegistryReadinessOptions state, string gameServerId, string defaultWorldId)
{
    if (state.Worlds.Count == 0)
    {
        return [BuildWorldRegistryEntry(state, defaultWorldId, gameServerId, state.DefaultWorldDisplayName, state.DefaultWorldStatus, state.DefaultWorldRegion, state.DefaultWorldLocale, state.CreatedAccounts, state.ActivePlayersEstimate, state.VeryActiveDailyPlayersEstimate, state.AllianceCount, state.ServerRecommended, state.ServerFull)];
    }

    return state.Worlds
        .Select(world => BuildWorldRegistryEntry(
            state,
            NormalizeGuidString(world.WorldId ?? defaultWorldId),
            NormalizeGuidString(world.GameServerId ?? gameServerId),
            world.DisplayName ?? state.DefaultWorldDisplayName,
            world.Status ?? state.DefaultWorldStatus,
            world.Region ?? state.DefaultWorldRegion,
            world.Locale ?? state.DefaultWorldLocale,
            world.CreatedAccounts,
            world.ActivePlayersEstimate,
            world.VeryActiveDailyPlayersEstimate,
            world.AllianceCount,
            world.ServerRecommended,
            world.ServerFull))
        .ToArray();
}

static WorldRegistryEntry BuildWorldRegistryEntry(
    WorldRegistryReadinessOptions state,
    string worldId,
    string gameServerId,
    string displayName,
    string status,
    string region,
    string locale,
    int? createdAccounts,
    int? activePlayersEstimate,
    int? veryActiveDailyPlayersEstimate,
    int? allianceCount,
    bool serverRecommended,
    bool serverFull)
{
    return new WorldRegistryEntry(
        worldId,
        gameServerId,
        displayName,
        status,
        region,
        locale,
        Recommended: serverRecommended,
        Joinable: false,
        Live: false,
        Capacity: null,
        Population: null,
        state.MinAccountsPerWorld,
        state.MaxAccountsPerWorld,
        state.MinActivePlayersPerWorld,
        state.MaxActivePlayersPerWorld,
        state.MinVeryActiveDailyPlayers,
        state.MaxVeryActiveDailyPlayers,
        state.MaxPlayersPerAlliance,
        createdAccounts,
        activePlayersEstimate,
        veryActiveDailyPlayersEstimate,
        allianceCount,
        serverRecommended,
        serverFull,
        MockReadiness: true);
}

static WorldCapacityPolicy BuildWorldCapacityPolicy(WorldRegistryReadinessOptions state)
{
    return new WorldCapacityPolicy(
        state.MinAccountsPerWorld,
        state.MaxAccountsPerWorld,
        state.MinActivePlayersPerWorld,
        state.MaxActivePlayersPerWorld,
        state.MinVeryActiveDailyPlayers,
        state.MaxVeryActiveDailyPlayers,
        state.MaxPlayersPerAlliance,
        BuildSupportedWorldStatuses());
}

static string[] BuildWorldRegistryReadinessBlockers(WorldRegistryReadinessOptions state, bool serverFirstProductionRouteProven)
{
    List<string> blockers = [];

    if (!serverFirstProductionRouteProven || !state.ProductionRouteProven)
    {
        blockers.Add("Production route proof is required before live world registry claims.");
    }

    if (!state.WorldSelectionEnabled)
    {
        blockers.Add("Live world selection is not enabled.");
    }

    if (!state.WorldCreationEnabled)
    {
        blockers.Add("World creation is not enabled.");
    }

    if (!state.WorldTransferEnabled)
    {
        blockers.Add("World transfer is not enabled.");
    }

    if (!state.WorldMergeEnabled)
    {
        blockers.Add("World merge is not enabled.");
    }

    if (!state.LivePopulationEnabled)
    {
        blockers.Add("Live population metrics are not enabled.");
    }

    if (state.MinAccountsPerWorld != 800 || state.MaxAccountsPerWorld != 1500)
    {
        blockers.Add("World account capacity policy must remain 800 to 1500 accounts until a new architecture decision changes it.");
    }

    if (state.MinActivePlayersPerWorld != 300 || state.MaxActivePlayersPerWorld != 600)
    {
        blockers.Add("World active-player capacity policy must remain 300 to 600 players until a new architecture decision changes it.");
    }

    if (state.MinVeryActiveDailyPlayers != 100 || state.MaxVeryActiveDailyPlayers != 300)
    {
        blockers.Add("Very-active daily player policy must remain 100 to 300 players until a new architecture decision changes it.");
    }

    if (state.MaxPlayersPerAlliance != 100)
    {
        blockers.Add("Alliance capacity policy must remain 100 players per alliance until a new architecture decision changes it.");
    }

    if (state.ServerRecommended && state.ServerFull)
    {
        blockers.Add("A full world cannot be advertised as the recommended server.");
    }

    if (!IsSupportedWorldStatus(state.DefaultWorldStatus))
    {
        blockers.Add($"Default world status '{state.DefaultWorldStatus}' is not supported by the readiness registry.");
    }

    HashSet<string> worldIds = new(StringComparer.OrdinalIgnoreCase);
    HashSet<string> recommendedWorldIds = new(StringComparer.OrdinalIgnoreCase);

    foreach (WorldRegistryWorldOptions world in state.Worlds)
    {
        string worldId = world.WorldId ?? string.Empty;
        string status = world.Status ?? state.DefaultWorldStatus;

        if (!IsSupportedWorldStatus(status))
        {
            blockers.Add($"World '{worldId}' status '{status}' is not supported by the readiness registry.");
        }

        if (!string.IsNullOrWhiteSpace(worldId) && !worldIds.Add(NormalizeGuidString(worldId)))
        {
            blockers.Add($"World '{worldId}' is configured more than once in the readiness registry.");
        }

        if (world.ServerRecommended)
        {
            recommendedWorldIds.Add(string.IsNullOrWhiteSpace(worldId) ? "<missing-world-id>" : NormalizeGuidString(worldId));
        }

        if (world.ServerRecommended && world.ServerFull)
        {
            blockers.Add($"World '{worldId}' cannot be both full and recommended.");
        }
    }

    if (recommendedWorldIds.Count > 1)
    {
        blockers.Add("Only one world can be advertised as recommended in the readiness registry.");
    }

    return blockers.ToArray();
}

static string[] BuildSupportedWorldStatuses()
{
    return ["Open", "Full", "Locked", "Maintenance", "Preparing"];
}

static bool IsSupportedWorldStatus(string? status)
{
    return BuildSupportedWorldStatuses().Contains(status, StringComparer.OrdinalIgnoreCase);
}

static WorldIdentityScope[] BuildWorldIdentityScopes()
{
    return
    [
        new WorldIdentityScope("Accounts", RequiresGameServerId: true, RequiresWorldId: true, "FutureRequired"),
        new WorldIdentityScope("Colonies", RequiresGameServerId: true, RequiresWorldId: true, "FutureRequired"),
        new WorldIdentityScope("WorldMap", RequiresGameServerId: true, RequiresWorldId: true, "PreparationOnly"),
        new WorldIdentityScope("Alliances", RequiresGameServerId: true, RequiresWorldId: true, "FutureRequired"),
        new WorldIdentityScope("Chat", RequiresGameServerId: true, RequiresWorldId: true, "FutureRequired"),
        new WorldIdentityScope("Rankings", RequiresGameServerId: true, RequiresWorldId: true, "FutureRequired")
    ];
}

static string[] BuildWorldIdentityReadinessBlockers(bool gameServerIdValid, bool defaultWorldIdValid, bool identifiersDistinct)
{
    List<string> blockers = [];

    if (!gameServerIdValid)
    {
        blockers.Add("GameServerId must be a valid GUID before world-scoped live features.");
    }

    if (!defaultWorldIdValid)
    {
        blockers.Add("DefaultWorldId must be a valid GUID before world-scoped live features.");
    }

    if (!identifiersDistinct)
    {
        blockers.Add("GameServerId and DefaultWorldId must remain distinct concepts.");
    }

    blockers.Add("Live world selection requires a dedicated SERVER.");
    blockers.Add("Official progression requires a dedicated SERVER.");

    return blockers.ToArray();
}

static string[] BuildReadinessBlockers(
    bool usesSqlServer,
    bool runtimeConnectionConfigured,
    bool migrationConnectionConfigured,
    bool requireAdminKey,
    bool adminKeyConfigured,
    bool requireMigrationApplyKey,
    bool migrationKeyConfigured,
    bool migrationKeyDistinct)
{
    List<string> blockers = [];
    if (usesSqlServer && !runtimeConnectionConfigured)
    {
        blockers.Add("SqlServer runtime connection string is not configured.");
    }

    if (usesSqlServer && !migrationConnectionConfigured)
    {
        blockers.Add("SqlServer migration connection string is not configured.");
    }

    if (!requireAdminKey || !adminKeyConfigured)
    {
        blockers.Add("Ops admin key protection is not fully configured.");
    }

    if (!requireMigrationApplyKey || !migrationKeyConfigured)
    {
        blockers.Add("Ops migration apply key protection is not fully configured.");
    }

    if (!migrationKeyDistinct)
    {
        blockers.Add("Ops migration apply key must be distinct from the admin key.");
    }

    return blockers.ToArray();
}

static string[] BuildSqlProductionDryRunBlockers(
    bool usesSqlServer,
    bool runtimeConnectionConfigured,
    bool migrationConnectionConfigured,
    bool identitiesSeparated,
    bool requireAdminKey,
    bool adminKeyConfigured,
    bool requireMigrationApplyKey,
    bool migrationKeyConfigured,
    bool migrationKeyDistinct,
    bool backupEvidenceConfigured,
    bool maintenanceWindowConfigured,
    bool rollbackPlanAcknowledged)
{
    List<string> blockers = [];
    if (!usesSqlServer)
    {
        blockers.Add("Persistence provider must be SqlServer for production SQL dry run.");
    }

    if (!runtimeConnectionConfigured)
    {
        blockers.Add("SqlServer runtime connection string is not configured.");
    }

    if (!migrationConnectionConfigured)
    {
        blockers.Add("SqlServer migration connection string is not configured.");
    }

    if (!identitiesSeparated)
    {
        blockers.Add("Runtime and migration SQL identities must be separated.");
    }

    if (!requireAdminKey || !adminKeyConfigured)
    {
        blockers.Add("Ops admin key protection is not fully configured.");
    }

    if (!requireMigrationApplyKey || !migrationKeyConfigured)
    {
        blockers.Add("Ops migration apply key protection is not fully configured.");
    }

    if (!migrationKeyDistinct)
    {
        blockers.Add("Ops migration apply key must be distinct from the admin key.");
    }

    if (!backupEvidenceConfigured)
    {
        blockers.Add("Verified SQL backup evidence reference is required before production SQL dry run.");
    }

    if (!maintenanceWindowConfigured)
    {
        blockers.Add("Maintenance window reference is required before production SQL dry run.");
    }

    if (!rollbackPlanAcknowledged)
    {
        blockers.Add("Rollback plan must be acknowledged before production SQL dry run.");
    }

    return blockers.ToArray();
}

static bool FixedTimeEquals(string provided, string expected)
{
    byte[] providedBytes = Encoding.UTF8.GetBytes(provided);
    byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
    return providedBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
}

static PlayerHiveState CreateInitialHiveState(Guid playerId, Guid hiveId) => new(
    playerId,
    hiveId,
    HiveStateMigrator.CurrentModelVersion,
    0,
    // La vraie limite de stockage vient desormais du niveau de batiment (voir
    // HiveOfflineProductionService.EffectiveCapacity) - cette capacite fixe n'est
    // plus qu'un plafond de securite tres large, plus jamais le facteur limitant.
    // Bootstrap Alpha : ressources initiales permettant la premiere boucle (upgrade guard_post 1→2 : 972 honey / 251 wax).
    new Dictionary<string, ResourceBalance>
    {
        ["honey"] = new(1500, 1_000_000_000),
        ["pollen"] = new(500, 1_000_000_000),
        ["wax"] = new(500, 1_000_000_000)
    },
     new Dictionary<string, int> { ["honey_reserve"] = 1, ["guard_post"] = 1 },
     [],
     new Dictionary<string, IdempotencyReceipt>(),
     InstallationComplete: false,
     SpeedUps: new Dictionary<string, int>(StringComparer.Ordinal));

static IResult AuthUnavailable() => Results.Json(new AuthenticationUnavailableEnvelope("auth.unavailable", "auth.unavailable"), statusCode: StatusCodes.Status503ServiceUnavailable);
static IResult AuthError(int statusCode, string code) => Results.Json(new AuthenticationUnavailableEnvelope(code, code), statusCode: statusCode);

static FoundationDotationHttpResponse ToFoundationResponse(HiveCommandResult result)
{
    FoundationDotationState foundation = result.State.FoundationDotation
        ?? throw new InvalidOperationException("Foundation result is missing its persisted proof.");
    return new FoundationDotationHttpResponse(
        foundation.Choice,
        foundation.HoneyAwarded,
        foundation.PollenAwarded,
        foundation.Proof,
        result.State.Revision,
        result.State.Resources.GetValueOrDefault("honey", new(0, 0)).Amount,
        result.State.Resources.GetValueOrDefault("pollen", new(0, 0)).Amount);
}

static async Task<IResult> ReadSpeedUps(HttpContext context, string hiveId, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<SpeedUpOptions> configured, CancellationToken ct)
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    SpeedUpReadSnapshot? snapshot = await new SpeedUpInventoryService(repository, clock, configured.Value).ReadAsync(auth.PlayerId!.Value, parsed, ct);
    return snapshot is null ? GameError(404, "game.hive_not_found", "game.error.not_found") : Results.Ok(snapshot);
}

static async Task<IResult> ApplySpeedUp(HttpContext context, string hiveId, string routeCategory, AuthenticationManager authentication, IHiveStateRepository repository, BeeKingdom.HiveOperations.IServerClock clock, IOptions<SpeedUpOptions> configured, ApplySpeedUpRequest? request, CancellationToken ct)
{
    if (!configured.Value.Enabled) return GameError(503, "game.unavailable", "game.error.unavailable");
    TokenValidationResult auth = AuthenticateGameRequest(context, authentication);
    if (!auth.IsValid) return GameError(401, "game.session_required", "game.error.session_required");
    if (!TryParseGameResourceId(hiveId, out Guid parsed) || request is null) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    if (!string.IsNullOrWhiteSpace(routeCategory) && !string.Equals(routeCategory, request.Category, StringComparison.Ordinal)) return GameError(400, "game.invalid_request", "game.error.invalid_request");
    SpeedUpCommandResult result = await new SpeedUpInventoryService(repository, clock, configured.Value).ApplyAsync(auth.PlayerId!.Value, parsed, request, ct);
    if (result.Succeeded) return Results.Ok(result.Response);
    int status = result.Code is "invalid_request" or "invalid_speedup" ? 400 : result.Code == "timer_not_found" ? 404 : 409;
    return GameError(status, "game." + result.Code, "game.error.conflict");
}

app.Run();

public partial class Program;

public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record AccessTokenRequest(string AccessToken);
public sealed record LogoutRequest(string SessionId);
public sealed record UpdateAccountProfileRequest(string DisplayName, string? Language, string? TimeZone, string? Country);
public sealed record CreateColonyHttpRequest(Guid PlayerId, Guid WorldId, string HiveName, Guid QueenId);
public sealed record SaveColonyHttpRequest(ColonySnapshotKind Kind);
public sealed record RenameColonyHttpRequest(string HiveName);
public sealed record ChangeColonyStatusHttpRequest(ColonyStatus Status);
public sealed record FastForwardHttpRequest(int Ticks);
public sealed record ClaimFoundationDotationHttpRequest(long ExpectedRevision, string Choice, string IdempotencyKey);
public sealed record FoundationDotationHttpResponse(string Choice, long HoneyAwarded, long PollenAwarded, string Proof, long Revision, long HoneyBalance, long PollenBalance);
public sealed record BroodVitalityHttpResponse(Guid HiveId, bool Initialized, int? Nutrition, int? Stability, long? Revision, DateTimeOffset? UpdatedAtUtc, BroodVitalityOperation? ActiveOperation);
public sealed record WorkshopBatchQualificationHttpRequest(long ExpectedRevision, string Answer, string IdempotencyKey);
public sealed record WorkshopBatchQualificationHttpResponse(string PreviousStep, string ResultingStep, string Answer, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, string Code);
public sealed record StrategicPathHttpRequest(string PathId, long ExpectedRevision, string IdempotencyKey);
public sealed record DoctrineRecruitmentStartRequest(string Family, long ExpectedRevision, string IdempotencyKey);
public sealed record DoctrineRecruitmentClaimRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record SquadReservationHttpRequest(long ExpectedRevision, Dictionary<string,long>? Quantities, string IdempotencyKey);
public sealed record SquadReservationReleaseHttpRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record HivePerimeterLaunchHttpRequest(string SignalKey, string SignalInstanceId, string ReservationId, long ExpectedRevision, string IdempotencyKey);
public sealed record HivePerimeterMutationHttpRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record AdminPlayerLookupResponse(Guid PlayerId, Guid AccountId, string Email, string Status);
public sealed record AdminPlayerHivesResponse(IReadOnlyList<Guid> HiveIds);
public sealed record AdminResourceAdjustHttpRequest(string Resource, long Delta, string Reason, long ExpectedRevision);
public sealed record AdminRosterAdjustHttpRequest(string Family, long Delta, string Reason, long ExpectedRevision);
public sealed record AdminGrantSlotHttpRequest(bool Premium, string Reason, long ExpectedRevision);
public sealed record AdminAdjustRecallTokensHttpRequest(long Delta, string Reason, long ExpectedRevision);
public sealed record AdminSetBuildingLevelHttpRequest(string BuildingKey, int Level, string Reason, long ExpectedRevision);
public sealed record AdminSetRoleHttpRequest(AccountRole Role, string Reason);
public sealed record AccountRoleAssignHttpRequest(Guid TargetAccountId, AccountRole Role);
public sealed record AccountRoleLookupResult(Guid AccountId, string? DisplayName, string Email, AccountRole Role);
public sealed record AdminGrantRewardHttpRequest(string RewardKey, string Source, string ResourceKey, long Amount, string Reason, long ExpectedRevision, string IdempotencyKey, string? NotificationKey = null);
public sealed record CombatPatrolPreviewHttpRequest(long Guardians, long Wingrunners, long Darters);
public sealed record CombatPatrolLaunchHttpRequest(int Tier, long Guardians, long Wingrunners, long Darters, long ExpectedRevision, string IdempotencyKey);
public sealed record CombatPatrolMutationHttpRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record CombatPatrolMutationResponse(CombatPatrolSnapshot Snapshot, CombatPatrolClaimReceipt? ClaimReceipt);
public sealed record DevSeedAccountRequest(string Email, string Password);
public sealed record GoogleLoginHttpRequest(string AuthorizationCode, string CodeVerifier, string RedirectUri, string ClientVersion, string DeviceIdentifier, string Region);
public sealed record ChampionBeeMutationHttpRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record SaveTutorialProgressHttpRequest(string ChapterKey, string SafeResumeStepKey, string LastObservedStepKey, long ExpectedRevision, string IdempotencyKey);
public sealed record TutorialProgressResponse(string ChapterKey, string SafeResumeStepKey, string LastObservedStepKey, DateTimeOffset UpdatedAtUtc, long Revision);
public sealed record SetChampionBeeAssignmentHttpRequest(List<string> BeeIds, long ExpectedRevision, string IdempotencyKey);
public sealed record ChampionBeeSnapshotResponse(Dictionary<string, int> Levels, List<string> AssignedBeeIds, int MaxAssigned, long Revision);
public sealed record ChampionBeeMutationResponse(bool Succeeded, string Code, string BeeId, int Level, List<string> AssignedBeeIds, long Revision);
public sealed record PromoteTroopTierHttpRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record TroopTierSnapshotResponse(Dictionary<string, int> Tiers, long Revision);
public sealed record TroopTierMutationResponse(bool Succeeded, string Code, string PopulationId, int Tier, long Revision);
public sealed record VipSnapshotResponse(long LifetimePoints, int Level, long? NextThreshold, int CapacityBonusBps, long Revision);
public sealed record GrantVipPointsHttpRequest(long Points, long ExpectedRevision, string IdempotencyKey);
public sealed record SetBuildingLevelHttpRequest(string BuildingKey, int Level);
public sealed record GrantResourceHttpRequest(string ResourceKey, long Amount);
public sealed record SetDisplayNameHttpRequest(string DisplayName);
public sealed record GameErrorEnvelope(string Code, string Message, int? RetryAfterSeconds);
public sealed record AllianceErrorEnvelope(string Code);
public sealed record AllianceHelpContributeWireRequest(string ClientRequestId);
public sealed record AllianceResearchFundingTargetWireRequest(string TechnologyId, string ClientRequestId);
public sealed record AllianceResearchDonateWireRequest(Guid HiveId, string ResourceKey, long Amount, string ClientRequestId);
public sealed record AllianceResearchLaunchWireRequest(string ClientRequestId);
public sealed record AllianceResearchSpeedUpWireRequest(Guid HiveId, string ItemId, string ClientRequestId);
public sealed record AuthenticationUnavailableEnvelope(string Code, string Message);
