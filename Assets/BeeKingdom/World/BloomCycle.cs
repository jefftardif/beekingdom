namespace BeeKingdom.World
{
    public sealed class BloomCycle
    {
        public double GrowSeconds { get; }
        public double BloomSeconds { get; }
        public double FadedSeconds { get; }
        public double DormantSeconds { get; }

        public BloomCycle(double growSeconds, double bloomSeconds, double fadedSeconds, double dormantSeconds)
        {
            GrowSeconds = growSeconds <= 0d ? 1d : growSeconds;
            BloomSeconds = bloomSeconds <= 0d ? 1d : bloomSeconds;
            FadedSeconds = fadedSeconds <= 0d ? 1d : fadedSeconds;
            DormantSeconds = dormantSeconds <= 0d ? 1d : dormantSeconds;
        }

        public FlowerGrowthStage Resolve(double ageSeconds)
        {
            double cycle = GrowSeconds + BloomSeconds + FadedSeconds + DormantSeconds;
            double value = ageSeconds % cycle;
            if (value < GrowSeconds * 0.25d) return FlowerGrowthStage.Seedling;
            if (value < GrowSeconds) return FlowerGrowthStage.Growing;
            if (value < GrowSeconds + BloomSeconds) return FlowerGrowthStage.Blooming;
            if (value < GrowSeconds + BloomSeconds + FadedSeconds) return FlowerGrowthStage.Faded;
            return FlowerGrowthStage.Dormant;
        }

        public static BloomCycle CreateDefault()
        {
            return new BloomCycle(3600d, 7200d, 3600d, 3600d);
        }
    }
}
