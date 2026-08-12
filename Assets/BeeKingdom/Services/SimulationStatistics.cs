namespace BeeKingdom.Services
{
    public readonly struct SimulationStatistics
    {
        public long TicksExecuted { get; }
        public double AverageTickTicks { get; }
        public int ActiveSystems { get; }
        public long EstimatedMemoryBytes { get; }
        public int ErrorCount { get; }

        public SimulationStatistics(long ticksExecuted, double averageTickTicks, int activeSystems, long estimatedMemoryBytes, int errorCount)
        {
            TicksExecuted = ticksExecuted;
            AverageTickTicks = averageTickTicks;
            ActiveSystems = activeSystems;
            EstimatedMemoryBytes = estimatedMemoryBytes;
            ErrorCount = errorCount;
        }
    }
}
