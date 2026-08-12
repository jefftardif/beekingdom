using BeeKingdom.Core.Time;

namespace BeeKingdom.World
{
    public sealed class FlowerPatch
    {
        public string PatchId { get; }
        public string RegionId { get; }
        public HexCoordinates Coordinates { get; }
        public FlowerSpecies Species { get; }
        public FlowerGrowthStage Stage { get; private set; }
        public double AgeSeconds { get; private set; }
        public double Nectar { get; private set; }
        public double Pollen { get; private set; }

        public FlowerPatch(string patchId, string regionId, HexCoordinates coordinates, FlowerSpecies species)
        {
            PatchId = string.IsNullOrWhiteSpace(patchId) ? System.Guid.NewGuid().ToString("N") : patchId;
            RegionId = regionId;
            Coordinates = coordinates;
            Species = species;
            Stage = FlowerGrowthStage.Seedling;
        }

        public bool Advance(double deltaSeconds, SimulationSeason season, WorldWeather weather)
        {
            FlowerGrowthStage previous = Stage;
            AgeSeconds += deltaSeconds < 0d ? 0d : deltaSeconds;
            Stage = Species.BloomCycle.Resolve(AgeSeconds);
            if (Stage == FlowerGrowthStage.Blooming)
            {
                double regen = Species.PollinationRules.BaseRegenerationPerSecond *
                    Species.PollinationRules.GetRegenerationMultiplier(season, weather) *
                    deltaSeconds;
                Nectar = System.Math.Min(Species.NectarCapacity, Nectar + regen);
                Pollen = System.Math.Min(Species.PollenCapacity, Pollen + regen * 0.75d);
            }
            else if (Stage == FlowerGrowthStage.Dormant)
            {
                Nectar = 0d;
                Pollen = 0d;
            }

            return previous != FlowerGrowthStage.Blooming && Stage == FlowerGrowthStage.Blooming;
        }

        public FlowerHarvestResult Harvest(double nectarAmount, double pollenAmount)
        {
            double nectar = System.Math.Min(Nectar, System.Math.Max(0d, nectarAmount));
            double pollen = System.Math.Min(Pollen, System.Math.Max(0d, pollenAmount));
            Nectar -= nectar;
            Pollen -= pollen;
            return new FlowerHarvestResult(nectar, pollen, Nectar <= 0d && Pollen <= 0d);
        }
    }

    public readonly struct FlowerHarvestResult
    {
        public double Nectar { get; }
        public double Pollen { get; }
        public bool IsDepleted { get; }

        public FlowerHarvestResult(double nectar, double pollen, bool isDepleted)
        {
            Nectar = nectar;
            Pollen = pollen;
            IsDepleted = isDepleted;
        }
    }
}
