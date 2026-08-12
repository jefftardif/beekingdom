using BeeKingdom.Simulation.Models;
using BeeKingdom.Simulation.Systems;

namespace BeeKingdom.Simulation.Scheduling;

public sealed class SimulationScheduler
{
    private readonly List<ISimulationSystem> systems = new();
    private readonly object sync = new();

    public void Register(ISimulationSystem system)
    {
        lock (sync)
        {
            systems.Add(system);
        }
    }

    public IReadOnlyList<ISimulationSystem> GetSystemsForStage(SimulationStage stage)
    {
        lock (sync)
        {
            return systems
                .Where(system => system.Stage == stage)
                .OrderBy(system => system.Stage)
                .ThenBy(system => system.Order)
                .ThenBy(system => system.Name, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public IReadOnlyList<ISimulationSystem> GetExecutionPlan()
    {
        lock (sync)
        {
            return systems
                .OrderBy(system => system.Stage)
                .ThenBy(system => system.Order)
                .ThenBy(system => system.Name, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
