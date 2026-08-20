using System.Collections.Generic;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class WorldBiomeCatalogTests
    {
        [Test]
        public void BiomeForChunkCoversTheFullPaintedGridWithoutThrowing()
        {
            HashSet<WorldBiome> seen = new HashSet<WorldBiome>();
            for (int y = 0; y < WorldMapWave6StreamingTileProvider.Rows; y++)
            {
                for (int x = 0; x < WorldMapWave6StreamingTileProvider.Columns; x++)
                {
                    int chunkX = WorldMapWave6StreamingTileProvider.OriginChunkX + x;
                    int chunkY = WorldMapWave6StreamingTileProvider.OriginChunkY + y;
                    seen.Add(WorldBiomeCatalog.BiomeForChunk(chunkX, chunkY));
                }
            }

            Assert.That(seen.Count, Is.EqualTo(6), "All 6 bible biomes should appear somewhere on the painted map.");
        }

        [Test]
        public void BiomeForChunkClampsOutOfRangeCoordinatesInsteadOfThrowing()
        {
            Assert.DoesNotThrow(() => WorldBiomeCatalog.BiomeForChunk(-100, -100));
            Assert.DoesNotThrow(() => WorldBiomeCatalog.BiomeForChunk(10000, 10000));
        }

        [Test]
        public void ProfileForReturnsMatchingProfilePerBiome()
        {
            foreach (WorldBiomeProfile profile in WorldBiomeCatalog.Profiles)
            {
                Assert.That(WorldBiomeCatalog.ProfileFor(profile.Biome).Biome, Is.EqualTo(profile.Biome));
            }
        }

        [Test]
        public void RegionsReferenceValidBiomes()
        {
            Assert.That(WorldBiomeCatalog.Regions.Length, Is.EqualTo(5));
            foreach (WorldRegionProfile region in WorldBiomeCatalog.Regions)
            {
                Assert.That(string.IsNullOrWhiteSpace(region.RegionId), Is.False);
                Assert.That(string.IsNullOrWhiteSpace(region.Label), Is.False);
            }
        }
    }
}
