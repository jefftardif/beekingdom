using System.Diagnostics;
using BeeKingdom.Colony;
using BeeKingdom.Colony.Models;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Simulation.Configuration;
using BeeKingdom.Simulation.Diagnostics;
using BeeKingdom.Simulation.Events;
using BeeKingdom.Simulation.Models;
using BeeKingdom.Simulation.Processing;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Simulation;

public sealed class SimulationEngine
{
    private readonly ColonyManager colonies;
    private readonly TickProcessor tickProcessor;
    private readonly IServerClock clock;
    private readonly ISimulationEventSink events;
    private readonly SimulationOptions options;
    private readonly Dictionary<ColonyId, LoadedSimulationColony> loadedColonies = new();
    private readonly object sync = new();
    private long nextTickId;

    public SimulationEngine(ColonyManager colonies, TickProcessor tickProcessor, IServerClock clock, ISimulationEventSink events, SimulationDiagnostics diagnostics, IOptions<SimulationOptions> options)
    {
        this.colonies = colonies;
        this.tickProcessor = tickProcessor;
        this.clock = clock;
        this.events = events;
        Diagnostics = diagnostics;
        this.options = options.Value;
    }

    public SimulationDiagnostics Diagnostics { get; }
    public SimulationState State { get; private set; } = SimulationState.Stopped;

    public void StartSimulation()
    {
        lock (sync)
        {
            if (State == SimulationState.Running)
            {
                return;
            }

            State = SimulationState.Running;
            events.Publish(new SimulationStarted(clock.UtcNow));
        }
    }

    public void StopSimulation()
    {
        lock (sync)
        {
            if (State == SimulationState.Stopped)
            {
                return;
            }

            State = SimulationState.Stopped;
            events.Publish(new SimulationStopped(clock.UtcNow));
        }
    }

    public void PauseSimulation()
    {
        lock (sync)
        {
            if (State != SimulationState.Running)
            {
                return;
            }

            State = SimulationState.Paused;
            events.Publish(new SimulationPaused(clock.UtcNow));
        }
    }

    public void ResumeSimulation()
    {
        lock (sync)
        {
            if (State != SimulationState.Paused)
            {
                return;
            }

            State = SimulationState.Running;
            events.Publish(new SimulationResumed(clock.UtcNow));
        }
    }

    public LoadedSimulationColony LoadColony(ColonyId colonyId)
    {
        lock (sync)
        {
            ColonyRecord colony = colonies.LoadColony(colonyId);
            LoadedSimulationColony loaded = new(colonyId, colony.Profile.WorldId, colony.Profile.CurrentSeason, "Clear", clock.UtcNow, clock.UtcNow, nextTickId);
            loadedColonies[colonyId] = loaded;
            Diagnostics.SetColoniesLoaded(loadedColonies.Count);
            events.Publish(new SimulationColonyLoaded(clock.UtcNow, colonyId));
            return loaded;
        }
    }

    public bool UnloadColony(ColonyId colonyId)
    {
        lock (sync)
        {
            bool removed = loadedColonies.Remove(colonyId);
            if (removed)
            {
                Diagnostics.SetColoniesLoaded(loadedColonies.Count);
                events.Publish(new SimulationColonyUnloaded(clock.UtcNow, colonyId));
            }

            return removed;
        }
    }

    public IReadOnlyList<SimulationTickResult> ExecuteTick(SimulationTickMode mode = SimulationTickMode.Fixed)
    {
        List<LoadedSimulationColony> batch;
        long tickId;

        lock (sync)
        {
            if (State != SimulationState.Running)
            {
                return Array.Empty<SimulationTickResult>();
            }

            UnloadInactiveColoniesCore();
            batch = loadedColonies.Values
                .OrderBy(colony => colony.WorldId)
                .ThenBy(colony => colony.ColonyId.Value)
                .Take(options.MaxColoniesPerTickBatch)
                .ToList();
            tickId = ++nextTickId;
        }

        long start = Stopwatch.GetTimestamp();
        List<SimulationTickResult> results = new(batch.Count);
        foreach (LoadedSimulationColony loaded in batch)
        {
            SimulationContext context = CreateContext(tickId, loaded, mode);
            SimulationTickResult result = tickProcessor.Execute(context);
            results.Add(result);

            lock (sync)
            {
                loadedColonies[loaded.ColonyId] = loaded with { LastActivityUtc = clock.UtcNow, LastTickId = tickId };
            }
        }

        Diagnostics.RecordTick(Stopwatch.GetTimestamp() - start, results.Count);
        events.Publish(new TickExecuted(clock.UtcNow, tickId, results.Count));
        return results;
    }

    public IReadOnlyList<SimulationTickResult> FastForward(int ticks)
    {
        if (ticks < 0 || ticks > options.MaxFastForwardTicks)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks), $"Fast forward tick count must be between 0 and {options.MaxFastForwardTicks}.");
        }

        List<SimulationTickResult> results = new();
        for (int i = 0; i < ticks; i++)
        {
            results.AddRange(ExecuteTick(SimulationTickMode.FastForward));
        }

        return results;
    }

    public IReadOnlyList<LoadedSimulationColony> GetLoadedColonies()
    {
        lock (sync)
        {
            return loadedColonies.Values.OrderBy(colony => colony.ColonyId.Value).ToArray();
        }
    }

    private SimulationContext CreateContext(long tickId, LoadedSimulationColony loaded, SimulationTickMode mode)
    {
        DateTimeOffset timestamp = mode == SimulationTickMode.VariableAdministration
            ? clock.UtcNow
            : options.SimulationEpochUtc.AddTicks(options.FixedTickInterval.Ticks * tickId);

        return new SimulationContext(tickId, loaded.WorldId, loaded.ColonyId, timestamp, loaded.Season, loaded.Weather, Array.Empty<string>(), mode);
    }

    private void UnloadInactiveColoniesCore()
    {
        DateTimeOffset now = clock.UtcNow;
        ColonyId[] inactive = loadedColonies.Values
            .Where(colony => now - colony.LastActivityUtc >= options.InactiveUnloadAfter)
            .Select(colony => colony.ColonyId)
            .ToArray();

        foreach (ColonyId colonyId in inactive)
        {
            loadedColonies.Remove(colonyId);
            events.Publish(new SimulationColonyUnloaded(clock.UtcNow, colonyId));
        }

        Diagnostics.SetColoniesLoaded(loadedColonies.Count);
    }
}
