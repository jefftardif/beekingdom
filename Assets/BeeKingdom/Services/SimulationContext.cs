using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Data;

namespace BeeKingdom.Services
{
    public sealed class SimulationContext
    {
        public IServiceRegistry Services { get; }
        public ITimeService TimeService { get; }
        public ISimulationScheduler Scheduler { get; }
        public IEventBus EventBus { get; }
        public ISaveService SaveService { get; }
        public IDataRegistry DataRegistry { get; }
        public SimulationWorld World { get; }

        public SimulationContext(
            IServiceRegistry services,
            ITimeService timeService,
            ISimulationScheduler scheduler,
            IEventBus eventBus,
            ISaveService saveService,
            IDataRegistry dataRegistry,
            SimulationWorld world)
        {
            Services = services;
            TimeService = timeService;
            Scheduler = scheduler;
            EventBus = eventBus;
            SaveService = saveService;
            DataRegistry = dataRegistry;
            World = world;
        }
    }
}
