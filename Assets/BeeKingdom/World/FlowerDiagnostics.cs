namespace BeeKingdom.World
{
    public sealed class FlowerDiagnostics
    {
        public int PatchCount { get; private set; }
        public int BloomingCount { get; private set; }
        public int DepletedCount { get; private set; }

        public void Record(int patches, int blooming)
        {
            PatchCount = patches;
            BloomingCount = blooming;
        }

        public void RecordDepleted()
        {
            DepletedCount++;
        }
    }
}
