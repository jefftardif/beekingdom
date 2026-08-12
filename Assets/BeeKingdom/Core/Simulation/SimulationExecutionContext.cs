using BeeKingdom.Core.Services;
using BeeKingdom.Core.Time;

namespace BeeKingdom.Core.Simulation
{
    public readonly struct SimulationExecutionContext
    {
        public SimulationTimestamp Timestamp { get; }
        public SimulationCalendar Calendar { get; }
        public SimulationTickFrequency Frequency { get; }
        public double DeltaSeconds { get; }
        public IServiceRegistry Services { get; }

        public SimulationExecutionContext(
            SimulationTimestamp timestamp,
            SimulationCalendar calendar,
            SimulationTickFrequency frequency,
            double deltaSeconds,
            IServiceRegistry services)
        {
            Timestamp = timestamp;
            Calendar = calendar;
            Frequency = frequency;
            DeltaSeconds = deltaSeconds;
            Services = services;
        }
    }
}
