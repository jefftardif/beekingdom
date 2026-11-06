using BeeKingdom.Authentication;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Persistence.Configuration;
using BeeKingdom.Persistence.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace BeeKingdom.TerritorySqlCandidate;

public sealed class CanonicalHivePositionOptions
{
    public const string SectionName = "CanonicalHivePositions";
    public bool Enabled { get; set; } = false;
    public string WorldId { get; set; } = "";
    public string SurfaceId { get; set; } = "";
}

public static class CanonicalHivePositionStartup
{
    public static IServiceCollection AddCanonicalHivePositions(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<CanonicalHivePositionOptions>().Bind(config.GetSection(CanonicalHivePositionOptions.SectionName))
            .Validate(o => !o.Enabled || Key(o.WorldId) && Key(o.SurfaceId), "Canonical hive map world and surface must be explicitly configured.")
            .Validate(o => !o.Enabled || PersistenceOptions.UsesSqlServer(config), "Canonical hive positions require the canonical SQL persistence provider.")
            .Validate(o => !o.Enabled || !string.IsNullOrWhiteSpace(RuntimeConnection(config)), "Canonical hive positions require the existing runtime SQL connection.")
            .Validate(o => !o.Enabled || ConsistentConnections(config), "Canonical hive and colony SQL connection settings conflict; reconcile configuration before activation.")
            .ValidateOnStart();
        services.AddSingleton(sp =>
        {
            var o = sp.GetRequiredService<IOptions<CanonicalHivePositionOptions>>().Value;
            if (!o.Enabled) throw new InvalidOperationException("hive_positions_disabled");
            return new SqlHivePositionStore(RuntimeConnection(config), o.WorldId, o.SurfaceId, true);
        });
        return services;
    }

    public static void MapCanonicalHivePositions(this WebApplication app,
        Func<HttpContext, AuthenticationManager, TokenValidationResult> authenticate)
    {
        var o = app.Services.GetRequiredService<IOptions<CanonicalHivePositionOptions>>().Value;
        if (!o.Enabled) return; // No route and no SQL connection when off.
        SqlHivePositionEndpoints.Map(app, app.Services.GetRequiredService<SqlHivePositionStore>(), o.WorldId, o.SurfaceId, authenticate);
    }

    private static bool Key(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 80;
    private static string RuntimeConnection(IConfiguration config)
    {
        var sql = config.GetSection(SqlServerOptions.SectionName).Get<SqlServerOptions>() ?? new SqlServerOptions();
        return new SqlConnectionFactory(config, Options.Create(sql)).GetRuntimeConnectionString();
    }
    private static bool ConsistentConnections(IConfiguration config)
    {
        var sql = config.GetSection(SqlServerOptions.SectionName).Get<SqlServerOptions>() ?? new SqlServerOptions();
        string hive = HiveRuntimeConnection(config, sql);
        try { return new SqlConnectionStringBuilder(RuntimeConnection(config)).EquivalentTo(new SqlConnectionStringBuilder(hive)); }
        catch (ArgumentException) { return false; }
    }
    // Existing Program hive factory prioritizes direct strings. Do not silently connect
    // positions/Colonies to one database while canonical HivePlayerStates uses another.
    private static string HiveRuntimeConnection(IConfiguration config, SqlServerOptions sql)
    {
        if (!string.IsNullOrWhiteSpace(sql.RuntimeConnectionString)) return sql.RuntimeConnectionString;
        if (!string.IsNullOrWhiteSpace(sql.RuntimeConnectionStringName))
        {
            string named = config.GetConnectionString(sql.RuntimeConnectionStringName) ?? "";
            if (!string.IsNullOrWhiteSpace(named)) return named;
        }
        if (!string.IsNullOrWhiteSpace(sql.ConnectionString)) return sql.ConnectionString;
        return string.IsNullOrWhiteSpace(sql.ConnectionStringName) ? "" : config.GetConnectionString(sql.ConnectionStringName) ?? "";
    }
}
