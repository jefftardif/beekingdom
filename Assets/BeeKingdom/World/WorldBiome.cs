using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class WorldBiome
    {
        private readonly List<string> floralSpecies;
        private readonly Dictionary<string, double> resourceWeights;

        public WorldBiomeType BiomeType { get; }
        public WorldClimate Climate { get; }
        public double RichnessMultiplier { get; }
        public double DifficultyMultiplier { get; }
        public IReadOnlyList<string> FloralSpecies => floralSpecies;
        public IReadOnlyDictionary<string, double> ResourceWeights => resourceWeights;

        public WorldBiome(WorldBiomeType biomeType, WorldClimate climate, double richnessMultiplier, double difficultyMultiplier, IReadOnlyList<string> floralSpecies, IReadOnlyDictionary<string, double> resourceWeights)
        {
            BiomeType = biomeType;
            Climate = climate;
            RichnessMultiplier = richnessMultiplier < 0d ? 0d : richnessMultiplier;
            DifficultyMultiplier = difficultyMultiplier < 0d ? 0d : difficultyMultiplier;
            this.floralSpecies = new List<string>(floralSpecies ?? new string[0]);
            this.resourceWeights = new Dictionary<string, double>(resourceWeights ?? new Dictionary<string, double>());
        }
    }
}
