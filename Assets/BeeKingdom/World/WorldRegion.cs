using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class WorldRegion
    {
        private readonly Dictionary<string, double> resources;
        private readonly List<string> floralSpecies;

        public string RegionId { get; }
        public WorldChunkCoordinate Coordinate { get; }
        public WorldBiomeType BiomeType { get; }
        public WorldClimate Climate { get; }
        public double Richness { get; }
        public double Difficulty { get; }
        public WorldWeather Weather { get; private set; }
        public IReadOnlyDictionary<string, double> Resources => resources;
        public IReadOnlyList<string> FloralSpecies => floralSpecies;

        public WorldRegion(string regionId, WorldChunkCoordinate coordinate, WorldBiomeType biomeType, WorldClimate climate, double richness, double difficulty, WorldWeather weather, IReadOnlyDictionary<string, double> resources, IReadOnlyList<string> floralSpecies)
        {
            RegionId = regionId;
            Coordinate = coordinate;
            BiomeType = biomeType;
            Climate = climate;
            Richness = richness < 0d ? 0d : richness;
            Difficulty = difficulty < 0d ? 0d : difficulty;
            Weather = weather;
            this.resources = new Dictionary<string, double>(resources ?? new Dictionary<string, double>());
            this.floralSpecies = new List<string>(floralSpecies ?? new string[0]);
        }

        public void ChangeWeather(WorldWeather weather)
        {
            Weather = weather;
        }
    }
}
