using System.Collections.Generic;

namespace BeeKingdom.Core.Integration
{
    public sealed class IntegrationRegistry
    {
        private readonly Dictionary<string, GameplayBridge> bridges = new Dictionary<string, GameplayBridge>();

        public int Count => bridges.Count;

        public bool Register(GameplayBridge bridge)
        {
            if (bridge == null || bridges.ContainsKey(bridge.BridgeId)) return false;
            bridges.Add(bridge.BridgeId, bridge);
            return true;
        }

        public bool Unregister(string bridgeId)
        {
            return bridges.Remove(bridgeId);
        }

        public bool TryGet(string bridgeId, out GameplayBridge bridge)
        {
            return bridges.TryGetValue(bridgeId, out bridge);
        }

        public IReadOnlyList<GameplayBridge> QueryByCapability(string capability)
        {
            List<GameplayBridge> result = new List<GameplayBridge>();
            foreach (GameplayBridge bridge in bridges.Values)
            {
                if (bridge.Supports(capability)) result.Add(bridge);
            }
            return result;
        }
    }
}
