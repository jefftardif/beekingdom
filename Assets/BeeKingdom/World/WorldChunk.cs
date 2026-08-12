using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class WorldChunk
    {
        private readonly List<string> regionIds = new List<string>();

        public WorldChunkCoordinate Coordinate { get; }
        public int Size { get; }
        public bool IsLoaded { get; private set; }
        public IReadOnlyList<string> RegionIds => regionIds;

        public WorldChunk(WorldChunkCoordinate coordinate, int size)
        {
            Coordinate = coordinate;
            Size = size <= 0 ? 16 : size;
        }

        public void AddRegion(string regionId)
        {
            if (!regionIds.Contains(regionId))
            {
                regionIds.Add(regionId);
            }
        }

        public void Load()
        {
            IsLoaded = true;
        }
    }
}
