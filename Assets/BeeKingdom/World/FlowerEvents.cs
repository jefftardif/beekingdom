using BeeKingdom.Core.Events;

namespace BeeKingdom.World
{
    public readonly struct FlowerBloomed : IGameplayEvent
    {
        public string PatchId { get; }
        public FlowerBloomed(string patchId) { PatchId = patchId; }
    }

    public readonly struct FlowerDepleted : IGameplayEvent
    {
        public string PatchId { get; }
        public FlowerDepleted(string patchId) { PatchId = patchId; }
    }
}
