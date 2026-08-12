using BeeKingdom.Core.Time;

namespace BeeKingdom.World
{
    public sealed class ClimateRules
    {
        public double SpringProductionModifier { get; }
        public double SummerProductionModifier { get; }
        public double AutumnProductionModifier { get; }
        public double WinterProductionModifier { get; }
        public double RainMovementModifier { get; }
        public double StormMovementModifier { get; }
        public double WinterConsumptionModifier { get; }

        public ClimateRules(double springProductionModifier, double summerProductionModifier, double autumnProductionModifier, double winterProductionModifier, double rainMovementModifier, double stormMovementModifier, double winterConsumptionModifier)
        {
            SpringProductionModifier = springProductionModifier;
            SummerProductionModifier = summerProductionModifier;
            AutumnProductionModifier = autumnProductionModifier;
            WinterProductionModifier = winterProductionModifier;
            RainMovementModifier = rainMovementModifier;
            StormMovementModifier = stormMovementModifier;
            WinterConsumptionModifier = winterConsumptionModifier;
        }

        public double GetProductionModifier(SimulationSeason season)
        {
            switch (season)
            {
                case SimulationSeason.Spring: return SpringProductionModifier;
                case SimulationSeason.Summer: return SummerProductionModifier;
                case SimulationSeason.Autumn: return AutumnProductionModifier;
                case SimulationSeason.Winter: return WinterProductionModifier;
                default: return 1d;
            }
        }

        public double GetMovementModifier(WorldWeather weather)
        {
            if (weather == WorldWeather.Storm) return StormMovementModifier;
            if (weather == WorldWeather.Rain) return RainMovementModifier;
            return 1d;
        }

        public double GetConsumptionModifier(SimulationSeason season)
        {
            return season == SimulationSeason.Winter ? WinterConsumptionModifier : 1d;
        }

        public static ClimateRules CreateDefault()
        {
            return new ClimateRules(1.25d, 1.1d, 0.85d, 0.4d, 0.85d, 0.45d, 1.25d);
        }
    }
}
