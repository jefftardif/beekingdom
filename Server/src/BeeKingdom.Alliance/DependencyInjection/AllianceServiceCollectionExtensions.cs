using BeeKingdom.Alliance.Activity;
using BeeKingdom.Alliance.Configuration;
using BeeKingdom.Alliance.Integration;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.Chat.Audience;
using BeeKingdom.Persistence.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Alliance.DependencyInjection;

public static class AllianceServiceCollectionExtensions
{
    // M042-CL: durable persistence added - one DurableJsonAlliance*Repository per subdomain,
    // same tier/pattern as DurableJsonHiveStateRepository (JSON files on disk, atomic writes, no
    // SQL). Mirrors the Hive repository's own provider-selection shape (Program.cs ~line 72) but
    // is stricter for the SqlServer case: the Hive repository silently falls back to DurableJson
    // for anything that isn't explicitly SqlServer, but Alliance has NO SqlServer implementation
    // at all - if config asks for SqlServer, this throws a clear, actionable message at startup
    // instead of silently persisting to disk under a config that claims SQL. Per the mission
    // brief: "STOP avant toute migration SQL et documenter exactement ce qui serait requis" - see
    // Docs/Alliance/ALLIANCE_PLATFORM_ARCHITECTURE.md section 17 for exactly what a real SQL
    // migration would need (schema, repositories, migration scripts - none of it built here).
    public static IServiceCollection AddBeeKingdomAlliance(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AllianceOptions>()
            .Bind(configuration.GetSection(AllianceOptions.SectionName))
            .Validate(o => o.MaxMembers is > 0 and <= 1000, "MaxMembers must be between 1 and 1000.")
            .Validate(o => o.NameMinLength > 0 && o.NameMaxLength >= o.NameMinLength, "Invalid name length bounds.")
            .Validate(o => o.TagMinLength > 0 && o.TagMaxLength >= o.TagMinLength, "Invalid tag length bounds.")
            .ValidateOnStart();

        if (PersistenceOptions.UsesSqlServer(configuration))
        {
            throw new InvalidOperationException(
                "Alliance persistence has no SQL Server implementation yet (Persistence:Provider=SqlServer is set). " +
                "This is an explicit stop, not an accident - see Docs/Alliance/ALLIANCE_PLATFORM_ARCHITECTURE.md " +
                "section 17 (SQL_PRODUCTION_MIGRATION_PENDING) for what a real migration would require. " +
                "Alliance currently persists to JSON files (DurableJsonAlliance*Repository) regardless of the " +
                "SQL setting used for Hive state - do not silently fall back to that for Alliance without a " +
                "deliberate decision, since Alliance and Hive data would then live in two different places.");
        }

        string root = Path.Combine(AppContext.BaseDirectory, "data", "alliances");
        services.AddSingleton<IAllianceRepository>(_ => new DurableJsonAllianceRepository(Path.Combine(root, "core")));
        services.AddSingleton<IAllianceActivityRepository>(_ => new DurableJsonAllianceActivityRepository(Path.Combine(root, "activity")));
        services.AddSingleton<IAllianceDiplomacyRepository>(_ => new DurableJsonAllianceDiplomacyRepository(Path.Combine(root, "diplomacy")));
        services.AddSingleton<IAllianceWarRepository>(_ => new DurableJsonAllianceWarRepository(Path.Combine(root, "wars")));
        services.AddSingleton<IAllianceActivityPublisher, AllianceActivityPublisher>();
        // Registered AFTER AddBeeKingdomChat's own NullAllianceMembershipResolver default (see
        // Program.cs call order) so this real, server-authoritative implementation wins.
        services.AddSingleton<IAllianceMembershipResolver, AllianceMembershipResolver>();
        services.AddSingleton<AllianceService>();

        return services;
    }
}
