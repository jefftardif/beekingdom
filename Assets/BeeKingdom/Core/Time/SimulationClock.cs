namespace BeeKingdom.Core.Time
{
    public sealed class SimulationClock
    {
        private long tickIndex;
        private double totalSeconds;

        public SimulationTimestamp Timestamp => new SimulationTimestamp(tickIndex, totalSeconds);

        public SimulationTimestamp Advance(double deltaSeconds)
        {
            if (deltaSeconds <= 0d)
            {
                return Timestamp;
            }

            tickIndex++;
            totalSeconds += deltaSeconds;
            return Timestamp;
        }
    }
}
