using BeeKingdom.Hive;

namespace BeeKingdom.AI
{
    public readonly struct BeeDecisionContext
    {
        public double DeltaSeconds { get; }
        public TaskInstance Task { get; }

        public BeeDecisionContext(double deltaSeconds, TaskInstance task)
        {
            DeltaSeconds = deltaSeconds;
            Task = task;
        }
    }
}
