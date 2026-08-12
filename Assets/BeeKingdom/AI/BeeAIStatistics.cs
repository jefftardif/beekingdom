namespace BeeKingdom.AI
{
    public readonly struct BeeAIStatistics
    {
        public int BrainCount { get; }
        public int ActiveCount { get; }
        public int WaitingCount { get; }

        public BeeAIStatistics(int brainCount, int activeCount, int waitingCount)
        {
            BrainCount = brainCount;
            ActiveCount = activeCount;
            WaitingCount = waitingCount;
        }
    }
}
