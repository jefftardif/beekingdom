using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WorldGenerationFrameworkTests
    {
        [Test]
        public void SameSeedProducesSameWorld()
        {
            WorldGenerator generator = new WorldGenerator();
            WorldGenerationProfile profile = WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Standard);

            WorldState first = generator.CreateWorld(new WorldSeed("alpha"), profile);
            WorldState second = generator.CreateWorld(new WorldSeed("alpha"), profile);

            WorldRegion firstRegion = first.Regions["region-0-0"];
            WorldRegion secondRegion = second.Regions["region-0-0"];
            Assert.That(firstRegion.BiomeType, Is.EqualTo(secondRegion.BiomeType));
            Assert.That(firstRegion.Richness, Is.EqualTo(secondRegion.Richness));
            Assert.That(firstRegion.Resources["nectar"], Is.EqualTo(secondRegion.Resources["nectar"]));
        }

        [Test]
        public void CreateWorldGeneratesChunksAndRegions()
        {
            WorldManager manager = new WorldManager();

            WorldState world = manager.CreateWorld(new WorldSeed("starter"), WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Tutorial));

            Assert.That(world.Regions.Count, Is.EqualTo(9));
            Assert.That(world.Chunks.Count, Is.EqualTo(9));
            Assert.That(manager.Diagnostics.RegionsGenerated, Is.EqualTo(9));
        }

        [Test]
        public void ValidateWorldAcceptsGeneratedWorld()
        {
            WorldManager manager = new WorldManager();
            manager.CreateWorld(new WorldSeed("valid"), WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Rich));

            WorldValidationResult result = manager.ValidateWorld();

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void LoadWorldMakesRegionsAvailable()
        {
            WorldGenerator generator = new WorldGenerator();
            WorldGenerationProfile profile = WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Standard);
            WorldState world = generator.CreateWorld(new WorldSeed("load"), profile);
            WorldManager manager = new WorldManager();

            manager.LoadWorld(world, profile);

            Assert.That(manager.GetRegion("region-0-0"), Is.Not.Null);
            Assert.That(manager.Diagnostics.RegionsLoaded, Is.EqualTo(world.Regions.Count));
        }

        [Test]
        public void GenerateRegionStreamsAdditionalChunk()
        {
            WorldManager manager = new WorldManager();
            manager.CreateWorld(new WorldSeed("stream"), WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Tutorial));

            WorldRegion region = manager.GenerateRegion(new WorldChunkCoordinate(5, 5));

            Assert.That(region.RegionId, Is.EqualTo("region-5-5"));
            Assert.That(manager.GetRegion("region-5-5"), Is.SameAs(region));
        }

        [Test]
        public void MultipleSeedsRemainStable()
        {
            for (int i = 0; i < 64; i++)
            {
                WorldManager manager = new WorldManager();
                manager.CreateWorld(new WorldSeed("seed-" + i), WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Standard));

                Assert.That(manager.ValidateWorld().IsValid, Is.True);
                Assert.That(manager.GetStatistics().RegionCount, Is.EqualTo(49));
            }
        }
    }
}
