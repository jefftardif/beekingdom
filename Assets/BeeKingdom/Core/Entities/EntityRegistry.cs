using System.Collections.Generic;

namespace BeeKingdom.Core.Entities
{
    public sealed class EntityRegistry
    {
        private readonly Dictionary<EntityId, SimulationEntity> entities = new Dictionary<EntityId, SimulationEntity>();

        public int Count => entities.Count;

        public bool Register(SimulationEntity entity)
        {
            if (entity == null || !entity.Id.IsValid || entities.ContainsKey(entity.Id)) return false;
            entities.Add(entity.Id, entity);
            return true;
        }

        public bool Unregister(EntityId id) => entities.Remove(id);
        public bool TryGet(EntityId id, out SimulationEntity entity) => entities.TryGetValue(id, out entity);
        public IReadOnlyCollection<SimulationEntity> GetAll() => entities.Values;
    }
}
