namespace BeeKingdom.World
{
    public readonly struct WorldStatistics
    {
        public int RegionCount { get; }
        public int ChunkCount { get; }
        public double AverageRichness { get; }
        public double AverageDifficulty { get; }

        public WorldStatistics(int regionCount, int chunkCount, double averageRichness, double averageDifficulty)
        {
            RegionCount = regionCount;
            ChunkCount = chunkCount;
            AverageRichness = averageRichness;
            AverageDifficulty = averageDifficulty;
        }
    }
}
