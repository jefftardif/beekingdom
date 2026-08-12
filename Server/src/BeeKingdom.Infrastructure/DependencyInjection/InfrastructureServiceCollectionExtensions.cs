using BeeKingdom.Infrastructure.Background;
using BeeKingdom.Infrastructure.Configuration;
using BeeKingdom.Infrastructure.Events;
using BeeKingdom.Infrastructure.Hosting;
using BeeKingdom.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddBeeKingdomInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BeeKingdomServerOptions>()
            .Bind(configuration.GetSection(BeeKingdomServerOptions.SectionName))
            .Validate(options => options.EventHistoryLimit > 0, "EventHistoryLimit must be greater than zero.");
        services.AddOptions<BeeKingdomServerHostProfile>()
            .Bind(configuration.GetSection(BeeKingdomServerHostProfile.SectionName));

        services.AddSingleton<IServerClock, SystemServerClock>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddHostedService<ServerHeartbeatWorker>();

        return services;
    }
}
