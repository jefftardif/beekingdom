using System.Collections.Generic;

namespace BeeKingdom.Core.Entities
{
    public sealed class SimulationEntity
    {
        private readonly HashSet<string> tags = new HashSet<string>();

        public EntityId Id { get; }
        public string EntityType { get; }
        public EntityLifecycleState State { get; private set; }
        public IReadOnlyCollection<string> Tags => tags;

        public SimulationEntity(EntityId id, string entityType, IEnumerable<string> tags = null)
        {
            Id = id;
            EntityType = string.IsNullOrWhiteSpace(entityType) ? "Entity" : entityType;
            State = EntityLifecycleState.Created;
            if (tags != null)
            {
                foreach (string tag in tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag)) this.tags.Add(tag);
                }
            }
        }

        public bool Activate() => Change(EntityLifecycleState.Created, EntityLifecycleState.Active) || Change(EntityLifecycleState.Suspended, EntityLifecycleState.Active);
        public bool Suspend() => Change(EntityLifecycleState.Active, EntityLifecycleState.Suspended);
        public bool Destroy()
        {
            if (State == EntityLifecycleState.Destroyed) return false;
            State = EntityLifecycleState.Destroyed;
            return true;
        }

        private bool Change(EntityLifecycleState from, EntityLifecycleState to)
        {
            if (State != from) return false;
            State = to;
            return true;
        }
    }
}
