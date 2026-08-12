using System.Collections.Generic;

namespace BeeKingdom.Core.Integration
{
    public sealed class GameplayBridge
    {
        private readonly HashSet<string> capabilities = new HashSet<string>();

        public string BridgeId { get; }
        public string Domain { get; }
        public IReadOnlyCollection<string> Capabilities => capabilities;

        public GameplayBridge(string bridgeId, string domain, IEnumerable<string> capabilities)
        {
            BridgeId = string.IsNullOrWhiteSpace(bridgeId) ? throw new System.ArgumentException("Bridge id is required.", nameof(bridgeId)) : bridgeId;
            Domain = string.IsNullOrWhiteSpace(domain) ? "General" : domain;
            if (capabilities != null)
            {
                foreach (string capability in capabilities)
                {
                    if (!string.IsNullOrWhiteSpace(capability)) this.capabilities.Add(capability);
                }
            }
        }

        public bool Supports(string capability)
        {
            return capabilities.Contains(capability);
        }
    }
}
