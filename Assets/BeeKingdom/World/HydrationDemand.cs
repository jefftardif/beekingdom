namespace BeeKingdom.World
{
    public readonly struct HydrationDemand
    {
        public string HiveId { get; }
        public int Population { get; }
        public double WaterPerBeePerDay { get; }
        public double DailyDemand => Population * WaterPerBeePerDay;

        public HydrationDemand(string hiveId, int population, double waterPerBeePerDay)
        {
            HiveId = string.IsNullOrWhiteSpace(hiveId) ? "hive" : hiveId;
            Population = population < 0 ? 0 : population;
            WaterPerBeePerDay = waterPerBeePerDay < 0d ? 0d : waterPerBeePerDay;
        }

        public double DemandForSeconds(double seconds)
        {
            return DailyDemand * (seconds <= 0d ? 0d : seconds / 86400d);
        }
    }
}
