using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class WorldState
    {
        private readonly Dictionary<string, WorldRegion> regions = new Dictionary<string, WorldRegion>();
        private readonly Dictionary<WorldChunkCoordinate, WorldChunk> chunks = new Dictionary<WorldChunkCoordinate, WorldChunk>();

        public string WorldId { get; }
        public WorldSeed Seed { get; }
        public WorldGenerationProfileType ProfileType { get; }
        public IReadOnlyDictionary<string, WorldRegion> Regions => regions;
        public IReadOnlyDictionary<WorldChunkCoordinate, WorldChunk> Chunks => chunks;

        public WorldState(string worldId, WorldSeed seed, WorldGenerationProfileType profileType)
        {
            WorldId = string.IsNullOrWhiteSpace(worldId) ? "world" : worldId;
            Seed = seed;
            ProfileType = profileType;
        }

        public void AddRegion(WorldRegion region, WorldChunk chunk)
        {
            regions[region.RegionId] = region;
            chunks[chunk.Coordinate] = chunk;
            chunk.AddRegion(region.RegionId);
        }

        public bool TryGetRegion(string regionId, out WorldRegion region)
        {
            return regions.TryGetValue(regionId, out region);
        }
    }
}
