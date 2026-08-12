using BeeKingdom.Colony.Configuration;
using BeeKingdom.Colony.Events;
using BeeKingdom.Colony.Registry;
using BeeKingdom.Colony.Repositories;
using BeeKingdom.Persistence.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Colony.DependencyInjection;

public static class ColonyServiceCollectionExtensions
{
    public static IServiceCollection AddBeeKingdomColony(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ColonyOptions>()
            .Bind(configuration.GetSection(ColonyOptions.SectionName))
            .Validate(options => options.MaxSnapshotBytes > 0, "MaxSnapshotBytes must be positive.")
            .Validate(options => options.AutoSaveInterval > TimeSpan.Zero, "AutoSaveInterval must be positive.")
            .Validate(options => options.RetentionDays > 0, "RetentionDays must be positive.");

        if (PersistenceOptions.UsesSqlServer(configuration))
        {
            services.AddSingleton<IColonyRepository, SqlColonyRepository>();
        }
        else
        {
            services.AddSingleton<IColonyRepository, InMemoryColonyRepository>();
        }

        services.AddSingleton<ColonyRegistry>();
        services.AddSingleton<IColonyEventSink, InMemoryColonyEventSink>();
        services.AddSingleton<IColonyService, ColonyService>();
        services.AddSingleton<ColonyManager>();

        return services;
    }
}
