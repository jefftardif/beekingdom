using BeeKingdom.Persistence.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Alliance.Help;

public static class AllianceHelpServiceCollectionExtensions
{
    // Called after AddBeeKingdomAlliance (needs IAllianceRepository) and after the inline
    // IHiveStateRepository/IServerClock registrations in Program.cs (needs both) - but like every
    // other module in this codebase, .NET's DI container resolves by registered type at first use,
    // not by registration order, so the exact call order here is for readability only.
    public static IServiceCollection AddBeeKingdomAllianceHelp(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AllianceHelpOptions>()
            .Bind(configuration.GetSection(AllianceHelpOptions.SectionName))
            .Validate(o => { o.Validate(); return true; })
            .ValidateOnStart();

        if (PersistenceOptions.UsesSqlServer(configuration))
        {
            services.AddSingleton<IAllianceHelpRepository, SqlAllianceHelpRepository>();
        }
        else
        {
            // Non-SQL local/dev environments only - production runs Persistence:Provider=SqlServer
            // (see SqlAllianceHelpRepository's class comment). Not durable across a process restart.
            services.AddSingleton<IAllianceHelpRepository, InMemoryAllianceHelpRepository>();
        }

        services.AddSingleton<AllianceHelpService>();

        return services;
    }
}
