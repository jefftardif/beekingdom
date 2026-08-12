using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Data;

namespace BeeKingdom.Services
{
    public sealed class SimulationBootstrap
    {
        public SimulationContext CreateContext(IServiceRegistry services)
        {
            IDataRegistry dataRegistry = services.Get<IDataRegistry>();
            dataRegistry.Reload();

            return new SimulationContext(
                services,
                services.Get<ITimeService>(),
                services.Get<ISimulationScheduler>(),
                services.Get<IEventBus>(),
                services.Get<ISaveService>(),
                dataRegistry,
                new SimulationWorld());
        }
    }
}
