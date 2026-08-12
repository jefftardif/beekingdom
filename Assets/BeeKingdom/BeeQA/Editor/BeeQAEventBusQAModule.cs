using System.Collections.Generic;
using BeeKingdom.Gameplay.Events;

namespace BeeKingdom.BeeQA
{
    public sealed class BeeQAEventBusQAModule : BeeQAModuleBase
    {
        public override string Id => "beeqa.event_bus";
        public override string DisplayName => "EventBus QA";
        public override string Description => "Vérifie publication, réception, ordre et désabonnement du Game Event Bus.";
        public override string Version => "1.0.0";
        public override BeeQACategory Category => BeeQACategory.Automation;

        protected override bool ExecuteCore(out string message)
        {
            var bus = new GameEventBus();
            var order = new List<int>(2);
            bool received = false;
            GameEventSubscription first = bus.Subscribe<BuildingCompleted>((eventData, context) =>
            {
                received = eventData.BuildingId == "beeqa_building" && context.Sequence == 1L;
                order.Add(1);
            });
            GameEventSubscription second = bus.Subscribe<BuildingCompleted>((eventData, context) => order.Add(2));
            try
            {
                bus.Publish(new BuildingCompleted("beeqa_building", System.Guid.Empty), "beeqa");
                bool ordered = order.Count == 2 && order[0] == 1 && order[1] == 2;
                bool active = bus.HasSubscribers<BuildingCompleted>();
                first.Dispose();
                second.Dispose();
                bool disposed = !bus.HasSubscribers<BuildingCompleted>();
                bool passed = received && ordered && active && disposed;
                message = passed ? "Publication, réception, ordre et désabonnement valides." : "Le contrat Event Bus a échoué.";
                return passed;
            }
            finally
            {
                first.Dispose();
                second.Dispose();
            }
        }
    }
}
