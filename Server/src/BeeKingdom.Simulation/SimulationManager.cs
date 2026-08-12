using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Simulation.Diagnostics;
using BeeKingdom.Simulation.Models;

namespace BeeKingdom.Simulation;

public sealed class SimulationManager
{
    private readonly SimulationEngine engine;

    public SimulationManager(SimulationEngine engine)
    {
        this.engine = engine;
    }

    public SimulationDiagnostics Diagnostics => engine.Diagnostics;
    public SimulationState State => engine.State;
    public void StartSimulation() => engine.StartSimulation();
    public void StopSimulation() => engine.StopSimulation();
    public void PauseSimulation() => engine.PauseSimulation();
    public void ResumeSimulation() => engine.ResumeSimulation();
    public IReadOnlyList<SimulationTickResult> ExecuteTick() => engine.ExecuteTick();
    public IReadOnlyList<SimulationTickResult> ExecuteVariableAdministrationTick() => engine.ExecuteTick(SimulationTickMode.VariableAdministration);
    public IReadOnlyList<SimulationTickResult> FastForward(int ticks) => engine.FastForward(ticks);
    public LoadedSimulationColony LoadColony(ColonyId colonyId) => engine.LoadColony(colonyId);
    public bool UnloadColony(ColonyId colonyId) => engine.UnloadColony(colonyId);
    public IReadOnlyList<LoadedSimulationColony> GetLoadedColonies() => engine.GetLoadedColonies();
}
