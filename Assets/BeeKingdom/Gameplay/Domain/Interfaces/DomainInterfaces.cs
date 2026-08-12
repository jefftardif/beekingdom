using System;

namespace BeeKingdom.Gameplay.Domain.Interfaces
{
    public interface IIdentifiable<out TId>
    {
        TId Id { get; }
    }

    public interface IEntity
    {
        DateTime CreatedAt { get; }
        DateTime UpdatedAt { get; }
    }

    /// <summary>
    /// Marker for entities that own a transactional consistency boundary.
    /// </summary>
    public interface IAggregateRoot
    {
    }

    public interface IUpdatable
    {
        void Touch(DateTime updatedAt);
    }

    public interface ISoftDeletable
    {
        bool IsDeleted { get; }
        DateTime? DeletedAt { get; }
        void MarkDeleted(DateTime deletedAt);
    }
}
