using BeeKingdom.Core.Time;
using BeeKingdom.Economy;
using BeeKingdom.Gameplay;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WaterHydrationSystemTests
    {
        [Test]
        public void SourceRechargesWithSeasonAndWeather()
        {
            WaterManager manager = new WaterManager();
            WaterSource source = new WaterSource("dew", "region", new HexCoordinates(0, 0), WaterSourceType.Dew, WaterQuality.Clean, 100d, 0d, 1d);
            manager.RegisterSource(source);
            manager.SetEnvironment(SimulationSeason.Spring, WorldWeather.Rain);

            manager.Execute(SimulationContextFactory.Create(10d));

            Assert.That(source.AvailableAmount, Is.GreaterThan(10d));
        }

        [Test]
        public void CollectWaterStoresThroughResourceFlow()
        {
            ResourceFlowManager flow = new ResourceFlowManager();
            WaterManager manager = new WaterManager(flow);
            manager.RegisterSource(new WaterSource("river", "region", new HexCoordinates(0, 0), WaterSourceType.River, WaterQuality.Clean, 100d, 50d, 1d));

            double collected = manager.CollectWater("river", "hive-water", 20d, 0d);

            Assert.That(collected, Is.EqualTo(20d));
            Assert.That(flow.QueryFlow("hive-water", ResourceType.Water), Is.EqualTo(20d));
            Assert.That(manager.Diagnostics.TransportedWater, Is.EqualTo(20d));
        }

        [Test]
        public void HydrationDemandScalesWithPopulationAndTime()
        {
            WaterManager manager = new WaterManager();
            manager.SetDemand(new HydrationDemand("hive", 100, 2d));

            Assert.That(manager.GetDemandForSeconds("hive", 43200d), Is.EqualTo(100d));
        }

        [Test]
        public void DepletedSourceRecordsDiagnostic()
        {
            WaterManager manager = new WaterManager();
            manager.RegisterSource(new WaterSource("pond", "region", new HexCoordinates(0, 0), WaterSourceType.Pond, WaterQuality.Stagnant, 10d, 5d, 0d));

            manager.CollectWater("pond", "hive-water", 10d, 0d);

            Assert.That(manager.GetSource("pond").AvailableAmount, Is.EqualTo(0d));
            Assert.That(manager.Diagnostics.DepletedSources, Is.EqualTo(1));
        }

        [Test]
        public void SeedFromRegionCreatesWaterSource()
        {
            WorldManager worldManager = new WorldManager();
            WorldState world = worldManager.CreateWorld(new WorldSeed("water"), WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Tutorial));
            HexGrid grid = HexGrid.FromWorld(world);
            WaterManager water = new WaterManager();

            water.SeedFromRegion(world.Regions["region-0-0"], grid);

            Assert.That(water.Diagnostics.SourceCount, Is.EqualTo(1));
            Assert.That(water.Diagnostics.AvailableWater, Is.GreaterThan(0d));
        }
    }
}
