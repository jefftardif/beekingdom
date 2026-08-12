namespace BeeKingdom.Hive
{
    public sealed class BeeLifecycleDiagnostics
    {
        public int BeeCount { get; private set; }
        public int AliveCount { get; private set; }
        public int DeadCount { get; private set; }
        public long LifecycleAdvances { get; private set; }

        public void Record(int beeCount, int aliveCount, int deadCount)
        {
            BeeCount = beeCount;
            AliveCount = aliveCount;
            DeadCount = deadCount;
            LifecycleAdvances++;
        }
    }
}
