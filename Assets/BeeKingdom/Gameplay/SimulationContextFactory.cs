using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;

namespace BeeKingdom.Gameplay
{
    public static class SimulationContextFactory
    {
        public static SimulationExecutionContext Create(double deltaSeconds)
        {
            return new SimulationExecutionContext(
                new SimulationTimestamp(0, deltaSeconds),
                new SimulationCalendar(1, 12, 0, SimulationSeason.Summer),
                SimulationTickFrequency.EveryFrame,
                deltaSeconds,
                null);
        }
    }
}
