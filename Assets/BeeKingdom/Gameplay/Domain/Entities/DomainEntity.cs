using System;
using BeeKingdom.Gameplay.Domain.Interfaces;

namespace BeeKingdom.Gameplay.Domain.Entities
{
    [Serializable]
    public abstract class DomainEntity<TId> : IEntity, IIdentifiable<TId>, IUpdatable, ISoftDeletable
    {
        public TId Id { get; }
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        protected DomainEntity(TId id, DateTime createdAt)
        {
            Id = id;
            CreatedAt = createdAt;
            UpdatedAt = createdAt;
        }

        public void Touch(DateTime updatedAt)
        {
            UpdatedAt = updatedAt;
        }

        public void MarkDeleted(DateTime deletedAt)
        {
            IsDeleted = true;
            DeletedAt = deletedAt;
            UpdatedAt = deletedAt;
        }
    }
}
