namespace BeeKingdom.AI
{
    public sealed class BeeAIDiagnostics
    {
        public BeeAIStatistics LastStatistics { get; private set; }
        public long Updates { get; private set; }
        public int InterruptedCount { get; private set; }
        public int BehaviorsRegistered { get; private set; }

        public void Record(BeeAIStatistics statistics)
        {
            LastStatistics = statistics;
            Updates++;
        }

        public void RecordInterrupt()
        {
            InterruptedCount++;
        }

        public void RecordBehaviorRegistered()
        {
            BehaviorsRegistered++;
        }
    }
}
