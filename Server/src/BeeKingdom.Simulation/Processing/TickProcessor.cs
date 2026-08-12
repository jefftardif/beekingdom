using System.Diagnostics;
using BeeKingdom.Colony;
using BeeKingdom.Colony.Models;
using BeeKingdom.Simulation.Configuration;
using BeeKingdom.Simulation.Diagnostics;
using BeeKingdom.Simulation.Models;
using BeeKingdom.Simulation.Scheduling;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Simulation.Processing;

public sealed class TickProcessor
{
    private static readonly SimulationStage[] StageOrder =
    [
        SimulationStage.GameplayEvents,
        SimulationStage.GameplayEffects,
        SimulationStage.GameplayAttributes,
        SimulationStage.Construction,
        SimulationStage.Population,
        SimulationStage.BeeLifecycle,
        SimulationStage.BeeNeeds,
        SimulationStage.BeeHealth,
        SimulationStage.Fatigue,
        SimulationStage.Experience,
        SimulationStage.AI,
        SimulationStage.Economy,
        SimulationStage.World,
        SimulationStage.SaveCheck,
        SimulationStage.Diagnostics
    ];

    private readonly SimulationScheduler scheduler;
    private readonly ColonyManager colonies;
    private readonly SimulationDiagnostics diagnostics;
    private readonly SimulationOptions options;

    public TickProcessor(SimulationScheduler scheduler, ColonyManager colonies, SimulationDiagnostics diagnostics, IOptions<SimulationOptions> options)
    {
        this.scheduler = scheduler;
        this.colonies = colonies;
        this.diagnostics = diagnostics;
        this.options = options.Value;
    }

    public SimulationTickResult Execute(SimulationContext context)
    {
        long start = Stopwatch.GetTimestamp();
        List<SimulationStage> executedStages = new(StageOrder.Length);
        bool snapshotProduced = false;

        foreach (SimulationStage stage in StageOrder)
        {
            foreach (var system in scheduler.GetSystemsForStage(stage))
            {
                system.Execute(context);
            }

            if (stage == SimulationStage.SaveCheck && options.AutoSaveEveryTicks > 0 && context.TickId % options.AutoSaveEveryTicks == 0)
            {
                long saveStart = Stopwatch.GetTimestamp();
                colonies.SaveColony(context.ColonyId, ColonySnapshotKind.Incremental);
                diagnostics.RecordSave(Stopwatch.GetTimestamp() - saveStart);
                snapshotProduced = true;
            }

            executedStages.Add(stage);
        }

        return new SimulationTickResult(
            context.TickId,
            context.ColonyId,
            context.Timestamp,
            executedStages,
            snapshotProduced,
            Stopwatch.GetElapsedTime(start));
    }
}
