using BeeKingdom.HiveOperations;
using BeeKingdom.Persistence.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Alliance.Research;

public static class AllianceResearchServiceCollectionExtensions
{
    public static IServiceCollection AddBeeKingdomAllianceResearch(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AllianceResearchOptions>()
            .Bind(configuration.GetSection(AllianceResearchOptions.SectionName))
            .Validate(o => { o.Validate(); return true; })
            .ValidateOnStart();

        if (PersistenceOptions.UsesSqlServer(configuration))
        {
            services.AddSingleton<IAllianceResearchRepository, SqlAllianceResearchRepository>();
        }
        else
        {
            // Non-SQL local/dev environments only - production runs Persistence:Provider=SqlServer
            // (see SqlAllianceResearchRepository's class comment). Not durable across a process restart.
            services.AddSingleton<IAllianceResearchRepository, InMemoryAllianceResearchRepository>();
        }

        services.AddSingleton<AllianceResearchService>();
        services.AddSingleton<AllianceResearchBonusResolver>();
        services.AddSingleton<IAllianceGameplayBonusResolver, AllianceGameplayBonusResolverAdapter>();

        return services;
    }
}
