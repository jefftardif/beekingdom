using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Simulation.Models;

public enum SimulationState
{
    Stopped = 0,
    Running = 1,
    Paused = 2
}

public enum SimulationTickMode
{
    Fixed = 0,
    VariableAdministration = 1,
    FastForward = 2
}

public enum SimulationStage
{
    GameplayEvents = 1,
    GameplayEffects = 2,
    GameplayAttributes = 3,
    Construction = 4,
    Population = 5,
    BeeLifecycle = 6,
    BeeNeeds = 7,
    BeeHealth = 8,
    Fatigue = 9,
    Experience = 10,
    AI = 11,
    Economy = 12,
    World = 13,
    SaveCheck = 14,
    Diagnostics = 15
}

public sealed record SimulationContext(
    long TickId,
    Guid WorldId,
    ColonyId ColonyId,
    DateTimeOffset Timestamp,
    string Season,
    string Weather,
    IReadOnlyList<string> ActiveEvents,
    SimulationTickMode Mode);

public sealed record SimulationTickResult(
    long TickId,
    ColonyId ColonyId,
    DateTimeOffset Timestamp,
    IReadOnlyList<SimulationStage> ExecutedStages,
    bool SnapshotProduced,
    TimeSpan Elapsed);

public sealed record LoadedSimulationColony(
    ColonyId ColonyId,
    Guid WorldId,
    string Season,
    string Weather,
    DateTimeOffset LoadedAtUtc,
    DateTimeOffset LastActivityUtc,
    long LastTickId);
