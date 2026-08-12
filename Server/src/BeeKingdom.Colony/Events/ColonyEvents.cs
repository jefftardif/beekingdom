using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Colony.Events;

public interface IColonyEvent
{
    DateTimeOffset OccurredAtUtc { get; }
    ColonyId ColonyId { get; }
}

public sealed record ColonyCreated(DateTimeOffset OccurredAtUtc, ColonyId ColonyId, PlayerId PlayerId) : IColonyEvent;
public sealed record ColonyLoaded(DateTimeOffset OccurredAtUtc, ColonyId ColonyId, PlayerId PlayerId) : IColonyEvent;
public sealed record ColonySaved(DateTimeOffset OccurredAtUtc, ColonyId ColonyId, long Revision) : IColonyEvent;
public sealed record ColonyDeleted(DateTimeOffset OccurredAtUtc, ColonyId ColonyId) : IColonyEvent;
public sealed record ColonyRenamed(DateTimeOffset OccurredAtUtc, ColonyId ColonyId, string HiveName) : IColonyEvent;
public sealed record ColonyStatisticsUpdated(DateTimeOffset OccurredAtUtc, ColonyId ColonyId, long Revision) : IColonyEvent;

public interface IColonyEventSink
{
    void Publish(IColonyEvent colonyEvent);
}

public sealed class InMemoryColonyEventSink : IColonyEventSink
{
    private readonly List<IColonyEvent> events = new();
    public IReadOnlyList<IColonyEvent> Events => events;
    public void Publish(IColonyEvent colonyEvent) => events.Add(colonyEvent);
}
