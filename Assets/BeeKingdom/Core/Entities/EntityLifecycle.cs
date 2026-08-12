namespace BeeKingdom.Core.Entities
{
    public sealed class EntityLifecycle
    {
        public bool Activate(SimulationEntity entity) => entity != null && entity.Activate();
        public bool Suspend(SimulationEntity entity) => entity != null && entity.Suspend();
        public bool Destroy(SimulationEntity entity) => entity != null && entity.Destroy();
    }
}
