using System.Linq;
using BeeKingdom.Core.Time;
using BeeKingdom.Economy;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class RegionalResourceDistributionFramework105Tests
    {
        [Test]
        public void PrairieGeneratesNectarAndPollenWithStableIds()
        {
            RegionalResourceDistributor distributor = new RegionalResourceDistributor(new WorldSeed("resources"));
            RegionDefinition region = Region(WorldBiomeType.Prairie);
            BiomeProfile biome = BiomeProfile.CreateStandard("prairie", WorldBiomeType.Prairie, WorldClimate.Temperate, 1d, 1d, 1d);
            RegionalWeatherSnapshot weather = Weather(region, WorldWeather.Clear);

            RegionalResourceDistributionSnapshot snapshot = distributor.BuildDistribution(region, biome, weather);

            Assert.That(snapshot.Plans.Any(plan => plan.ResourceType == ResourceType.Nectar), Is.True);
            Assert.That(snapshot.Plans.Any(plan => plan.ResourceType == ResourceType.Pollen), Is.True);
            Assert.That(snapshot.Plans.All(plan => plan.NodeId.StartsWith("region-1-")), Is.True);
        }

        [Test]
        public void WetlandGeneratesWaterPlans()
        {
            RegionalResourceDistributor distributor = new RegionalResourceDistributor(new WorldSeed("resources"));
            RegionDefinition region = Region(WorldBiomeType.Wetland);
            BiomeProfile biome = BiomeProfile.CreateStandard("wetland", WorldBiomeType.Wetland, WorldClimate.Humid, 1d, 1d, 1.5d);

            RegionalResourceDistributionSnapshot snapshot = distributor.BuildDistribution(region, biome, Weather(region, WorldWeather.Rain));

            Assert.That(snapshot.Plans.Any(plan => plan.ResourceType == ResourceType.Water), Is.True);
        }

        [Test]
        public void RainIncreasesWaterCapacityWithoutProducingStock()
        {
            RegionalResourceDistributor distributor = new RegionalResourceDistributor(new WorldSeed("resources"));
            RegionDefinition region = Region(WorldBiomeType.Wetland);
            BiomeProfile biome = BiomeProfile.CreateStandard("wetland", WorldBiomeType.Wetland, WorldClimate.Humid, 1d, 1d, 1d);

            double clearCapacity = distributor.BuildDistribution(region, biome, Weather(region, WorldWeather.Clear)).Plans.First(plan => plan.ResourceType == ResourceType.Water).Capacity;
            double rainCapacity = distributor.BuildDistribution(region, biome, Weather(region, WorldWeather.Rain)).Plans.First(plan => plan.ResourceType == ResourceType.Water).Capacity;
            ResourceFlowManager flow = new ResourceFlowManager();

            Assert.That(rainCapacity, Is.GreaterThan(clearCapacity));
            Assert.That(flow.QueryFlow("hive", ResourceType.Water), Is.EqualTo(0d));
        }

        [Test]
        public void SameSeedProducesSameSnapshot()
        {
            RegionalResourceDistributor first = new RegionalResourceDistributor(new WorldSeed("resources"));
            RegionalResourceDistributor second = new RegionalResourceDistributor(new WorldSeed("resources"));
            RegionDefinition region = Region(WorldBiomeType.Meadow);
            BiomeProfile biome = BiomeProfile.CreateStandard("meadow", WorldBiomeType.Meadow, WorldClimate.Temperate, 1.2d, 1d, 1d);
            RegionalWeatherSnapshot weather = Weather(region, WorldWeather.Cloudy);

            Assert.That(first.BuildDistribution(region, biome, weather).Equals(second.BuildDistribution(region, biome, weather)), Is.True);
        }

        [Test]
        public void InvalidResourceRuleIsRejected()
        {
            RegionalResourceDiagnostics diagnostics = new RegionalResourceDiagnostics();
            RegionalResourceDistributor distributor = new RegionalResourceDistributor(new WorldSeed("resources"), diagnostics);
            RegionDefinition region = Region(WorldBiomeType.Urban);
            BiomeProfile biome = new BiomeProfile(
                "urban",
                WorldBiomeType.Urban,
                WorldClimate.Variable,
                BiomeModifierSet.Identity,
                new[] { new BiomeResourceRule("unknown-resource", 1d, 1d) },
                new[] { BiomeClimateRule.FromClimate(WorldClimate.Variable) });

            RegionalResourceDistributionSnapshot snapshot = distributor.BuildDistribution(region, biome, Weather(region, WorldWeather.Clear));

            Assert.That(snapshot.Plans.Count, Is.EqualTo(0));
            Assert.That(diagnostics.RejectedRuleCount, Is.EqualTo(1));
        }

        [Test]
        public void PlansAreSortedByRegionThenNodeId()
        {
            RegionalResourceDistributor distributor = new RegionalResourceDistributor(new WorldSeed("resources"));
            RegionDefinition region = Region(WorldBiomeType.FlowerFields);
            BiomeProfile biome = BiomeProfile.CreateStandard("flower-fields", WorldBiomeType.FlowerFields, WorldClimate.Temperate, 1.6d, 1d, 1d);

            RegionalResourceDistributionSnapshot snapshot = distributor.BuildDistribution(region, biome, Weather(region, WorldWeather.Clear));

            string[] sorted = snapshot.Plans.Select(plan => plan.NodeId).OrderBy(id => id).ToArray();
            Assert.That(snapshot.Plans.Select(plan => plan.NodeId).ToArray(), Is.EqualTo(sorted));
        }

        private static RegionDefinition Region(WorldBiomeType biome)
        {
            return new RegionDefinition("region-1", "world-1", new WorldSeed("region"), biome, WorldWeather.Clear, SimulationSeason.Spring, 20d, 0.5d, 16, 8, 4);
        }

        private static RegionalWeatherSnapshot Weather(RegionDefinition region, WorldWeather weather)
        {
            return new RegionalWeatherSnapshot(region.WorldId, region.RegionId, region.Biome, SimulationSeason.Spring, weather, region.Temperature, region.Humidity, 1, "weather");
        }
    }
}
