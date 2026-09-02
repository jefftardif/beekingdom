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
    // M043I-CL: SQL Server persistence added (SqlAlliance*Repository, migration
    // 090_alliance_platform.sql) after production was confirmed to actually run
    // Persistence:Provider=SqlServer (via IIS app-pool env var, not the checked-in
    // appsettings.Production.json default) - the M042 DurableJson-only design threw at startup
    // in that case, which crashed the whole server (see the M043H incident report). Alliance now
    // follows the same provider-selection shape as every other module (Accounts/Chat/Colony):
    // SqlAlliance*Repository when Persistence:Provider=SqlServer, DurableJsonAlliance*Repository
    // (JSON files on disk, atomic writes) otherwise.
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
            services.AddSingleton<IAllianceRepository, SqlAllianceRepository>();
            services.AddSingleton<IAllianceActivityRepository, SqlAllianceActivityRepository>();
            services.AddSingleton<IAllianceDiplomacyRepository, SqlAllianceDiplomacyRepository>();
            services.AddSingleton<IAllianceWarRepository, SqlAllianceWarRepository>();
        }
        else
        {
            string root = Path.Combine(AppContext.BaseDirectory, "data", "alliances");
            services.AddSingleton<IAllianceRepository>(_ => new DurableJsonAllianceRepository(Path.Combine(root, "core")));
            services.AddSingleton<IAllianceActivityRepository>(_ => new DurableJsonAllianceActivityRepository(Path.Combine(root, "activity")));
            services.AddSingleton<IAllianceDiplomacyRepository>(_ => new DurableJsonAllianceDiplomacyRepository(Path.Combine(root, "diplomacy")));
            services.AddSingleton<IAllianceWarRepository>(_ => new DurableJsonAllianceWarRepository(Path.Combine(root, "wars")));
        }

        services.AddSingleton<IAllianceActivityPublisher, AllianceActivityPublisher>();
        // Registered AFTER AddBeeKingdomChat's own NullAllianceMembershipResolver default (see
        // Program.cs call order) so this real, server-authoritative implementation wins.
        services.AddSingleton<IAllianceMembershipResolver, AllianceMembershipResolver>();
        services.AddSingleton<AllianceService>();

        return services;
    }
}
