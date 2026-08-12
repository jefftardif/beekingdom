using BeeKingdom.Core.Time;
using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WorldRegionFramework101To102Tests
    {
        [Test]
        public void WorldEngineCreatesAndSavesWorldSnapshot()
        {
            WorldDefinition definition = new WorldDefinition("world-1", "Test World", new WorldSeed("seed"));
            definition.RegisterBiome(new BiomeDefinition("prairie", WorldBiomeType.Prairie, WorldClimate.Temperate));
            WorldEngine engine = new WorldEngine();

            WorldInstance instance = engine.CreateWorld(definition);
            WorldSnapshot snapshot = engine.SaveWorld(instance.Definition.WorldId);

            Assert.That(snapshot.WorldId, Is.EqualTo("world-1"));
            Assert.That(definition.Biomes.ContainsKey("prairie"), Is.True);
        }

        [Test]
        public void RegionManagerLoadsSuspendsAndQueriesNeighbors()
        {
            RegionManager manager = new RegionManager();
            RegionDefinition center = new RegionDefinition("center", "world-1", new WorldSeed("seed"), WorldBiomeType.Forest, WorldWeather.Clear, SimulationSeason.Spring, 22d, 0.5d, 16, 8, 4, new[] { "east" });
            RegionDefinition east = new RegionDefinition("east", "world-1", new WorldSeed("seed-east"), WorldBiomeType.Meadow, WorldWeather.Cloudy, SimulationSeason.Spring, 20d, 0.6d, 16, 8, 4);
            manager.RegisterRegion(center);
            manager.RegisterRegion(east);

            RegionInstance loaded = manager.LoadRegion("center");
            manager.LoadRegion("east");
            manager.SetState("center", RegionSimulationState.Suspended);

            Assert.That(loaded.Snapshot.State, Is.EqualTo(RegionSimulationState.Suspended));
            Assert.That(manager.QueryNeighborRegions("center").Count, Is.EqualTo(1));
        }
    }
}
