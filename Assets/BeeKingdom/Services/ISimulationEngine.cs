using BeeKingdom.Core.Services;

namespace BeeKingdom.Services
{
    public interface ISimulationEngine : IGameService
    {
        bool IsRunning { get; }
        bool IsPaused { get; }
        SimulationContext Context { get; }
        SimulationEngineDiagnostics Diagnostics { get; }
        void Stop();
        void Reset();
        SimulationStatistics GetStatistics();
    }
}
