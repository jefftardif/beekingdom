namespace BeeKingdom.Protocol.Events;

public interface IServerEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAtUtc { get; }
}
