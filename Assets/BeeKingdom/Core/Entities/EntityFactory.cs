namespace BeeKingdom.Core.Entities
{
    public sealed class EntityFactory
    {
        private long nextId = 1;
        public SimulationEntity Create(string entityType, params string[] tags)
        {
            return new SimulationEntity(new EntityId(entityType + "-" + nextId++), entityType, tags);
        }
    }
}
