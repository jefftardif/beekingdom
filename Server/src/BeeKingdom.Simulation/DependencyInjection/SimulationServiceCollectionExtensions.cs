using BeeKingdom.Simulation.Configuration;
using BeeKingdom.Simulation.Diagnostics;
using BeeKingdom.Simulation.Events;
using BeeKingdom.Simulation.Processing;
using BeeKingdom.Simulation.Scheduling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Simulation.DependencyInjection;

public static class SimulationServiceCollectionExtensions
{
    public static IServiceCollection AddBeeKingdomSimulation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SimulationOptions>()
            .Bind(configuration.GetSection(SimulationOptions.SectionName))
            .Validate(options => options.FixedTickInterval > TimeSpan.Zero, "FixedTickInterval must be positive.")
            .Validate(options => options.AutoSaveEveryTicks > 0, "AutoSaveEveryTicks must be positive.")
            .Validate(options => options.InactiveUnloadAfter > TimeSpan.Zero, "InactiveUnloadAfter must be positive.")
            .Validate(options => options.MaxFastForwardTicks >= 0, "MaxFastForwardTicks must be zero or positive.")
            .Validate(options => options.MaxColoniesPerTickBatch > 0, "MaxColoniesPerTickBatch must be positive.");

        services.AddSingleton<SimulationDiagnostics>();
        services.AddSingleton<ISimulationEventSink, InMemorySimulationEventSink>();
        services.AddSingleton<SimulationScheduler>();
        services.AddSingleton<TickProcessor>();
        services.AddSingleton<SimulationEngine>();
        services.AddSingleton<SimulationManager>();

        return services;
    }
}
