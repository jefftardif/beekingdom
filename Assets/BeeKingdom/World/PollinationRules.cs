using BeeKingdom.Core.Time;

namespace BeeKingdom.World
{
    public sealed class PollinationRules
    {
        public double BaseRegenerationPerSecond { get; }
        public double RainModifier { get; }
        public double StormModifier { get; }
        public double SpringModifier { get; }
        public double WinterModifier { get; }

        public PollinationRules(double baseRegenerationPerSecond, double rainModifier, double stormModifier, double springModifier, double winterModifier)
        {
            BaseRegenerationPerSecond = baseRegenerationPerSecond < 0d ? 0d : baseRegenerationPerSecond;
            RainModifier = rainModifier < 0d ? 0d : rainModifier;
            StormModifier = stormModifier < 0d ? 0d : stormModifier;
            SpringModifier = springModifier < 0d ? 0d : springModifier;
            WinterModifier = winterModifier < 0d ? 0d : winterModifier;
        }

        public double GetRegenerationMultiplier(SimulationSeason season, WorldWeather weather)
        {
            double multiplier = 1d;
            if (season == SimulationSeason.Spring) multiplier *= SpringModifier;
            if (season == SimulationSeason.Winter) multiplier *= WinterModifier;
            if (weather == WorldWeather.Rain) multiplier *= RainModifier;
            if (weather == WorldWeather.Storm) multiplier *= StormModifier;
            return multiplier;
        }

        public static PollinationRules CreateDefault()
        {
            return new PollinationRules(0.02d, 1.25d, 0.25d, 1.4d, 0.35d);
        }
    }
}
