using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class WorldGenerator
    {
        public WorldState CreateWorld(WorldSeed seed, WorldGenerationProfile profile)
        {
            WorldState world = new WorldState("world-" + seed.Hash, seed, profile.ProfileType);
            for (int x = -profile.RegionRadius; x <= profile.RegionRadius; x++)
            {
                for (int y = -profile.RegionRadius; y <= profile.RegionRadius; y++)
                {
                    WorldRegion region = GenerateRegion(seed, profile, new WorldChunkCoordinate(x, y));
                    world.AddRegion(region, new WorldChunk(region.Coordinate, profile.ChunkSize));
                }
            }

            return world;
        }

        public WorldRegion GenerateRegion(WorldSeed seed, WorldGenerationProfile profile, WorldChunkCoordinate coordinate)
        {
            DeterministicRandom random = new DeterministicRandom(seed.Hash, coordinate.X, coordinate.Y);
            WorldBiomeType biomeType = SelectBiome(random);
            WorldBiome biome = profile.GetBiome(biomeType);
            double richness = (0.75d + random.NextDouble() * 0.5d) * biome.RichnessMultiplier;
            double difficulty = (0.75d + random.NextDouble() * 0.5d) * biome.DifficultyMultiplier;
            WorldWeather weather = SelectWeather(random, biome.Climate);
            Dictionary<string, double> resources = new Dictionary<string, double>();
            foreach (var pair in biome.ResourceWeights)
            {
                resources[pair.Key] = pair.Value * richness * (25d + random.NextDouble() * 75d);
            }

            string regionId = "region-" + coordinate.X + "-" + coordinate.Y;
            return new WorldRegion(regionId, coordinate, biomeType, biome.Climate, richness, difficulty, weather, resources, biome.FloralSpecies);
        }

        private static WorldBiomeType SelectBiome(DeterministicRandom random)
        {
            WorldBiomeType[] values =
            {
                WorldBiomeType.Prairie,
                WorldBiomeType.Forest,
                WorldBiomeType.Mountain,
                WorldBiomeType.River,
                WorldBiomeType.Marsh,
                WorldBiomeType.FlowerFields
            };
            return values[random.NextInt(values.Length)];
        }

        private static WorldWeather SelectWeather(DeterministicRandom random, WorldClimate climate)
        {
            double value = random.NextDouble();
            if (climate == WorldClimate.Humid && value < 0.35d) return WorldWeather.Rain;
            if (climate == WorldClimate.Cold && value < 0.25d) return WorldWeather.Wind;
            if (climate == WorldClimate.Variable && value < 0.25d) return WorldWeather.Storm;
            if (value < 0.15d) return WorldWeather.Cloudy;
            return WorldWeather.Clear;
        }

        private sealed class DeterministicRandom
        {
            private uint state;

            public DeterministicRandom(int seed, int x, int y)
            {
                unchecked
                {
                    state = (uint)(seed ^ (x * 73856093) ^ (y * 19349663));
                    if (state == 0u) state = 1u;
                }
            }

            public int NextInt(int maxExclusive)
            {
                return (int)(NextUInt() % (uint)maxExclusive);
            }

            public double NextDouble()
            {
                return NextUInt() / (double)uint.MaxValue;
            }

            private uint NextUInt()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }
        }
    }
}
