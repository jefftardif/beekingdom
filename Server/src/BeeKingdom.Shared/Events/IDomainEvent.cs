using BeeKingdom.Shared.Contracts;

namespace BeeKingdom.Shared.Events;

public interface IDomainEvent : IContract
{
    Guid EventId { get; }
    DateTimeOffset OccurredAtUtc { get; }
}
