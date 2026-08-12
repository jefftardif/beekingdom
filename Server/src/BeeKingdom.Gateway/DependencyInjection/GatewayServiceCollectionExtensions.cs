using BeeKingdom.Gateway.Configuration;
using BeeKingdom.Gateway.Connections;
using BeeKingdom.Gateway.Events;
using BeeKingdom.Gateway.RateLimiting;
using BeeKingdom.Gateway.Routing;
using BeeKingdom.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Gateway.DependencyInjection;

public static class GatewayServiceCollectionExtensions
{
    public static IServiceCollection AddBeeKingdomGateway(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GatewayOptions>()
            .Bind(configuration.GetSection(GatewayOptions.SectionName))
            .Validate(options => options.MaxConnections > 0, "MaxConnections must be positive.")
            .Validate(options => options.MaxMessageBytes > 0, "MaxMessageBytes must be positive.")
            .Validate(options => options.PlayerMessagesPerMinute > 0, "PlayerMessagesPerMinute must be positive.");

        services.AddSingleton<ProtocolManager>();
        services.AddSingleton<GatewayHost>();
        services.AddSingleton<ConnectionManager>();
        services.AddSingleton<SessionRouter>();
        services.AddSingleton<RequestRouter>();
        services.AddSingleton<GatewayRateLimiter>();
        services.AddSingleton<IGatewayEventSink, InMemoryGatewayEventSink>();
        services.AddSingleton<GatewayManager>();

        return services;
    }
}
