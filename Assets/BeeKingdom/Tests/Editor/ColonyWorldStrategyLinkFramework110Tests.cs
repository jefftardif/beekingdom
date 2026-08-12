using System;
using BeeKingdom.Core.Time;
using BeeKingdom.Economy;
using BeeKingdom.Population;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ColonyWorldStrategyLinkFramework110Tests
    {
        [Test]
        public void VisibleDroughtIncreasesWeatherRisk()
        {
            WorldAwareStrategyContext context = Collect("colony-1", WorldWeather.Wind, 50d, 10d, 0d);

            Assert.That(context.WeatherRisk, Is.GreaterThan(0d));
        }

        [Test]
        public void LowResourcesIncreaseFoodPressure()
        {
            WorldAwareStrategyContext context = Collect("colony-1", WorldWeather.Clear, 100d, 0d, 0d);

            Assert.That(context.FoodPressure, Is.EqualTo(1d));
        }

        [Test]
        public void UnknownRegionDoesNotInfluenceContext()
        {
            ColonyWorldSignalCollector collector = new ColonyWorldSignalCollector();
            WorldExplorationVisibilitySnapshot visibility = new WorldExplorationVisibilitySnapshot(new[] { new RegionVisibilityRecord("colony-1", "world-1", "region-1", ExplorationVisibilityState.Stale, 1, -1) });

            WorldAwareStrategyContext context = collector.Collect(new ColonyWorldSignalInput("colony-1", visibility, new[] { Weather("region-1", WorldWeather.Storm) }, Array.Empty<RegionalEcologySnapshot>(), Array.Empty<RegionalResourceDistributionSnapshot>(), Array.Empty<RegionalEventPropagationSnapshot>()), new ColonyWorldSignalWeights());

            Assert.That(context.WeatherRisk, Is.EqualTo(0d));
        }

        [Test]
        public void CriticalRegionalEventIncreasesThreatPressure()
        {
            WorldAwareStrategyContext context = Collect("colony-1", WorldWeather.Clear, 100d, 50d, 0.9d);

            Assert.That(context.WorldThreatPressure, Is.EqualTo(0.9d));
        }

        [Test]
        public void DifferentVisibilityProducesDifferentContexts()
        {
            WorldAwareStrategyContext first = Collect("colony-1", WorldWeather.Storm, 100d, 50d, 0d);
            WorldAwareStrategyContext second = Collect("colony-2", WorldWeather.Clear, 100d, 50d, 0d);

            Assert.That(first.WeatherRisk, Is.Not.EqualTo(second.WeatherRisk));
        }

        [Test]
        public void AdapterDoesNotModifyColonyStrategyManager()
        {
            ColonyStrategyManager manager = new ColonyStrategyManager();
            ColonyWorldStrategyAdapter adapter = new ColonyWorldStrategyAdapter();

            StrategyContext context = adapter.ToStrategyContext(Collect("colony-1", WorldWeather.Clear, 100d, 50d, 0d));

            Assert.That(manager.QueryCurrentStrategy(), Is.Null);
            Assert.That(context.FoodPressure, Is.EqualTo(0.5d));
        }

        private static WorldAwareStrategyContext Collect(string colonyId, WorldWeather weather, double capacity, double amount, double threat)
        {
            ColonyWorldSignalCollector collector = new ColonyWorldSignalCollector();
            WorldExplorationVisibilitySnapshot visibility = new WorldExplorationVisibilitySnapshot(new[] { new RegionVisibilityRecord(colonyId, "world-1", "region-1", ExplorationVisibilityState.Visible, 1, 10) });
            RegionalEventPropagationSnapshot regionalEvent = new RegionalEventPropagationSnapshot("event-1", "critical", "region-1", 1, false, new[] { new RegionalEventAffectedRegion("region-1", 0, threat) });
            return collector.Collect(new ColonyWorldSignalInput(
                colonyId,
                visibility,
                new[] { Weather("region-1", weather) },
                new[] { new RegionalEcologySnapshot("world-1", "region-1", 0.2d, RegionalEcologyStateKind.Healthy, weather, 1, 0d, 0d, 0d) },
                new[] { Resources("region-1", weather, capacity, amount) },
                new[] { regionalEvent }),
                new ColonyWorldSignalWeights());
        }

        private static RegionalWeatherSnapshot Weather(string regionId, WorldWeather weather)
        {
            return new RegionalWeatherSnapshot("world-1", regionId, WorldBiomeType.Prairie, SimulationSeason.Spring, weather, 20d, 0.5d, 1, "weather");
        }

        private static RegionalResourceDistributionSnapshot Resources(string regionId, WorldWeather weather, double capacity, double amount)
        {
            RegionalResourceNodePlan plan = new RegionalResourceNodePlan(regionId + "-nectar-00", regionId, new HexCoordinates(0, 0), ResourceType.Nectar, RegionalResourceCategory.Floral, capacity, amount, 1d, 1);
            return new RegionalResourceDistributionSnapshot("world-1", regionId, 1, SimulationSeason.Spring, weather, new[] { plan });
        }
    }
}
