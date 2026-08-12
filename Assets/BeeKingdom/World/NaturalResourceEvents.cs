using BeeKingdom.Core.Events;
using BeeKingdom.Economy;

namespace BeeKingdom.World
{
    public readonly struct NaturalResourceRegenerated : IGameplayEvent
    {
        public string NodeId { get; }
        public NaturalResourceRegenerated(string nodeId) { NodeId = nodeId; }
    }

    public readonly struct NaturalResourceDepleted : IGameplayEvent
    {
        public string NodeId { get; }
        public ResourceType ResourceType { get; }
        public NaturalResourceDepleted(string nodeId, ResourceType resourceType) { NodeId = nodeId; ResourceType = resourceType; }
    }
}
