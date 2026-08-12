using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Core.Simulation
{
    public interface ISimulationScheduler : IGameService
    {
        SimulationDiagnostics Diagnostics { get; }
        void RegisterSystem(ISimulationSystem system);
        bool UnregisterSystem(Type systemType);
        bool EnableSystem(Type systemType);
        bool DisableSystem(Type systemType);
        void ExecuteTick(in SimulationExecutionContext context);
        IReadOnlyList<ISimulationSystem> GetRegisteredSystems();
    }
}
