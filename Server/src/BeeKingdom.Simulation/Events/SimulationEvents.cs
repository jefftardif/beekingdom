using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Simulation.Events;

public interface ISimulationEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}

public sealed record SimulationStarted(DateTimeOffset OccurredAtUtc) : ISimulationEvent;
public sealed record SimulationStopped(DateTimeOffset OccurredAtUtc) : ISimulationEvent;
public sealed record TickExecuted(DateTimeOffset OccurredAtUtc, long TickId, int ColoniesSimulated) : ISimulationEvent;
public sealed record SimulationColonyLoaded(DateTimeOffset OccurredAtUtc, ColonyId ColonyId) : ISimulationEvent;
public sealed record SimulationColonyUnloaded(DateTimeOffset OccurredAtUtc, ColonyId ColonyId) : ISimulationEvent;
public sealed record SimulationPaused(DateTimeOffset OccurredAtUtc) : ISimulationEvent;
public sealed record SimulationResumed(DateTimeOffset OccurredAtUtc) : ISimulationEvent;

public interface ISimulationEventSink
{
    void Publish(ISimulationEvent simulationEvent);
}

public sealed class InMemorySimulationEventSink : ISimulationEventSink
{
    private readonly List<ISimulationEvent> events = new();
    private readonly object sync = new();

    public IReadOnlyList<ISimulationEvent> Events
    {
        get
        {
            lock (sync)
            {
                return events.ToArray();
            }
        }
    }

    public void Publish(ISimulationEvent simulationEvent)
    {
        lock (sync)
        {
            events.Add(simulationEvent);
        }
    }
}
