namespace BeeKingdom.Gameplay
{
    public sealed class IntegrationDiagnostics
    {
        public int Population { get; private set; }
        public double TotalResources { get; private set; }
        public int ActiveTasks { get; private set; }
        public double SimulatedSeconds { get; private set; }
        public double AverageTickSeconds { get; private set; }
        public int EventsPerSecond { get; private set; }
        public int ErrorCount { get; private set; }

        public void RecordPopulation(int value)
        {
            Population = value;
        }

        public void RecordResources(double value)
        {
            TotalResources = value;
        }

        public void RecordTasks(int value)
        {
            ActiveTasks = value;
        }

        public void RecordTick(double simulatedSeconds, double averageTickSeconds)
        {
            SimulatedSeconds = simulatedSeconds;
            AverageTickSeconds = averageTickSeconds;
        }

        public void RecordEvents(int eventsPerSecond)
        {
            EventsPerSecond = eventsPerSecond;
        }

        public void RecordError()
        {
            ErrorCount++;
        }
    }
}
