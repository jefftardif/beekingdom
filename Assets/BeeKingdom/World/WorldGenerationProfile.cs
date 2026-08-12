using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class WorldGenerationProfile
    {
        private readonly Dictionary<WorldBiomeType, WorldBiome> biomes;

        public WorldGenerationProfileType ProfileType { get; }
        public int RegionRadius { get; }
        public int ChunkSize { get; }
        public IReadOnlyDictionary<WorldBiomeType, WorldBiome> Biomes => biomes;

        public WorldGenerationProfile(WorldGenerationProfileType profileType, int regionRadius, int chunkSize, IReadOnlyDictionary<WorldBiomeType, WorldBiome> biomes)
        {
            ProfileType = profileType;
            RegionRadius = regionRadius < 0 ? 0 : regionRadius;
            ChunkSize = chunkSize <= 0 ? 16 : chunkSize;
            this.biomes = new Dictionary<WorldBiomeType, WorldBiome>(biomes ?? new Dictionary<WorldBiomeType, WorldBiome>());
        }

        public WorldBiome GetBiome(WorldBiomeType type)
        {
            return biomes[type];
        }

        public static WorldGenerationProfile CreateDefault(WorldGenerationProfileType profileType)
        {
            double richness = profileType == WorldGenerationProfileType.Rich ? 1.4d : profileType == WorldGenerationProfileType.Harsh ? 0.65d : 1d;
            double difficulty = profileType == WorldGenerationProfileType.Harsh ? 1.4d : profileType == WorldGenerationProfileType.Tutorial ? 0.6d : 1d;
            return new WorldGenerationProfile(profileType, profileType == WorldGenerationProfileType.Tutorial ? 1 : 3, 16, new Dictionary<WorldBiomeType, WorldBiome>
            {
                { WorldBiomeType.Prairie, Biome(WorldBiomeType.Prairie, WorldClimate.Temperate, richness, difficulty, "clover", "daisy") },
                { WorldBiomeType.Forest, Biome(WorldBiomeType.Forest, WorldClimate.Humid, richness * 0.9d, difficulty * 1.1d, "wildflower", "bluebell") },
                { WorldBiomeType.Mountain, Biome(WorldBiomeType.Mountain, WorldClimate.Cold, richness * 0.6d, difficulty * 1.4d, "alpine-bloom") },
                { WorldBiomeType.River, Biome(WorldBiomeType.River, WorldClimate.Humid, richness * 1.1d, difficulty, "water-mint", "iris") },
                { WorldBiomeType.Marsh, Biome(WorldBiomeType.Marsh, WorldClimate.Variable, richness * 0.8d, difficulty * 1.2d, "marsh-marigold") },
                { WorldBiomeType.FlowerFields, Biome(WorldBiomeType.FlowerFields, WorldClimate.Temperate, richness * 1.6d, difficulty * 0.8d, "lavender", "sunflower", "clover") }
            });
        }

        private static WorldBiome Biome(WorldBiomeType type, WorldClimate climate, double richness, double difficulty, params string[] flowers)
        {
            return new WorldBiome(type, climate, richness, difficulty, flowers, new Dictionary<string, double>
            {
                { "nectar", richness },
                { "pollen", richness * 0.8d },
                { "water", climate == WorldClimate.Humid ? richness : richness * 0.4d }
            });
        }
    }
}
