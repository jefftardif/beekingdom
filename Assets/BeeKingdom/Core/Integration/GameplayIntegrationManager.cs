using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Core.Integration
{
    public sealed class GameplayIntegrationManager
    {
        private readonly IntegrationRegistry registry = new IntegrationRegistry();
        private readonly IEventBus eventBus;

        public IntegrationDiagnostics Diagnostics { get; } = new IntegrationDiagnostics();

        public GameplayIntegrationManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterBridge(GameplayBridge bridge)
        {
            bool registered = registry.Register(bridge);
            if (registered)
            {
                Diagnostics.RecordRegistered(registry.Count);
                eventBus?.Publish(new GameplayBridgeRegistered(bridge.BridgeId));
            }
            return registered;
        }

        public bool UnregisterBridge(string bridgeId)
        {
            bool removed = registry.Unregister(bridgeId);
            if (removed) Diagnostics.RecordRegistered(registry.Count);
            return removed;
        }

        public bool TryRoute(string capability, out GameplayBridge bridge)
        {
            IReadOnlyList<GameplayBridge> bridges = registry.QueryByCapability(capability);
            if (bridges.Count == 0)
            {
                bridge = null;
                Diagnostics.RecordMissingRoute();
                return false;
            }

            bridge = bridges[0];
            Diagnostics.RecordRouted();
            eventBus?.Publish(new GameplayIntegrationRouted(capability, bridge.BridgeId));
            return true;
        }

        public IReadOnlyList<GameplayBridge> QueryBridges(string capability)
        {
            return registry.QueryByCapability(capability);
        }
    }
}
