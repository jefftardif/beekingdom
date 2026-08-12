using System;
using System.Collections.Generic;

namespace BeeKingdom.Core.Simulation
{
    public interface ISimulationSystem
    {
        Type SystemType { get; }
        string Name { get; }
        SimulationPhase Phase { get; }
        int Priority { get; }
        IReadOnlyList<Type> RunsAfter { get; }
        IReadOnlyList<Type> RunsBefore { get; }
        void Execute(in SimulationExecutionContext context);
    }
}
