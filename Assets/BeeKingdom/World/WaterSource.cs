using BeeKingdom.Core.Time;

namespace BeeKingdom.World
{
    public sealed class WaterSource
    {
        public string SourceId { get; }
        public string RegionId { get; }
        public HexCoordinates Coordinates { get; }
        public WaterSourceType SourceType { get; }
        public WaterQuality Quality { get; private set; }
        public double Capacity { get; }
        public double AvailableAmount { get; private set; }
        public double SeasonalRechargePerSecond { get; }

        public WaterSource(string sourceId, string regionId, HexCoordinates coordinates, WaterSourceType sourceType, WaterQuality quality, double capacity, double initialAmount, double seasonalRechargePerSecond)
        {
            SourceId = string.IsNullOrWhiteSpace(sourceId) ? System.Guid.NewGuid().ToString("N") : sourceId;
            RegionId = regionId;
            Coordinates = coordinates;
            SourceType = sourceType;
            Quality = quality;
            Capacity = capacity < 0d ? 0d : capacity;
            AvailableAmount = System.Math.Min(Capacity, System.Math.Max(0d, initialAmount));
            SeasonalRechargePerSecond = seasonalRechargePerSecond < 0d ? 0d : seasonalRechargePerSecond;
        }

        public double Collect(double amount)
        {
            double collected = System.Math.Min(AvailableAmount, System.Math.Max(0d, amount));
            AvailableAmount -= collected;
            return collected;
        }

        public void Recharge(double deltaSeconds, SimulationSeason season, WorldWeather weather)
        {
            double modifier = season == SimulationSeason.Winter ? 0.35d : season == SimulationSeason.Spring ? 1.25d : 1d;
            if (weather == WorldWeather.Rain) modifier *= 1.5d;
            if (weather == WorldWeather.Storm) modifier *= 0.75d;
            AvailableAmount = System.Math.Min(Capacity, AvailableAmount + SeasonalRechargePerSecond * modifier * System.Math.Max(0d, deltaSeconds));
        }

        public void SetQuality(WaterQuality quality)
        {
            Quality = quality;
        }
    }
}
