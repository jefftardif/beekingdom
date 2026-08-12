using System;
using BeeKingdom.Core.Time;

namespace BeeKingdom.Core.Services
{
    public interface ITimeService : IGameService
    {
        float DeltaTime { get; }
        float UnscaledDeltaTime { get; }
        float Time { get; }
        bool IsPaused { get; }
        SimulationTimeScale TimeScale { get; }
        SimulationTimestamp Timestamp { get; }
        SimulationCalendar Calendar { get; }
        TimeDiagnostics Diagnostics { get; }
        void SetTimeScale(float timeScale);
        void SetPaused(bool isPaused);
        void SetMaxOfflineSeconds(double maxOfflineSeconds);
        OfflineTimeResult CalculateOfflineTime(DateTime previousUtc, DateTime currentUtc);
    }
}
