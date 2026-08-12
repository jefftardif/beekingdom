using BeeKingdom.Core.Events;

namespace BeeKingdom.Core.Integration
{
    public readonly struct GameplayBridgeRegistered : IGameplayEvent { public string BridgeId { get; } public GameplayBridgeRegistered(string bridgeId) { BridgeId = bridgeId; } }
    public readonly struct GameplayIntegrationRouted : IGameplayEvent { public string Capability { get; } public string BridgeId { get; } public GameplayIntegrationRouted(string capability, string bridgeId) { Capability = capability; BridgeId = bridgeId; } }
}
