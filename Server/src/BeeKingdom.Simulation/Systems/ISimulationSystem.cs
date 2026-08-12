using BeeKingdom.Simulation.Models;

namespace BeeKingdom.Simulation.Systems;

public interface ISimulationSystem
{
    SimulationStage Stage { get; }
    int Order { get; }
    string Name { get; }
    void Execute(SimulationContext context);
}
