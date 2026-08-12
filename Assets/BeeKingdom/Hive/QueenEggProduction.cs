namespace BeeKingdom.Hive
{
    public sealed class QueenEggProduction
    {
        private double accumulator;

        public float BaseEggsPerMinute { get; private set; }
        public int TotalProduced { get; private set; }

        public QueenEggProduction(float baseEggsPerMinute)
        {
            BaseEggsPerMinute = baseEggsPerMinute < 0f ? 0f : baseEggsPerMinute;
        }

        public int Produce(double deltaSeconds, QueenHealth health, int energy, float fertility, float seasonModifier, float bonusModifier, int level)
        {
            if (deltaSeconds <= 0d || health.IsDead || energy <= 0)
            {
                return 0;
            }

            float healthFactor = health.Ratio;
            float energyFactor = energy > 100 ? 1f : energy / 100f;
            float levelFactor = 1f + ((level - 1) * 0.05f);
            double eggs = (BaseEggsPerMinute / 60d) * deltaSeconds * healthFactor * energyFactor * fertility * seasonModifier * bonusModifier * levelFactor;
            accumulator += eggs;
            int produced = (int)accumulator;
            if (produced > 0)
            {
                accumulator -= produced;
                TotalProduced += produced;
            }

            return produced;
        }
    }
}
