namespace BeeKingdom.World
{
    public sealed class HexCell
    {
        public HexCoordinates Coordinates { get; }
        public WorldChunkCoordinate ChunkCoordinate { get; }
        public string RegionId { get; private set; }
        public bool IsLoaded { get; private set; }
        public int MovementCost { get; private set; }

        public HexCell(HexCoordinates coordinates, WorldChunkCoordinate chunkCoordinate, string regionId, int movementCost = 1)
        {
            Coordinates = coordinates;
            ChunkCoordinate = chunkCoordinate;
            RegionId = regionId;
            MovementCost = movementCost <= 0 ? 1 : movementCost;
        }

        public void AssignRegion(string regionId)
        {
            RegionId = regionId;
        }

        public void SetLoaded(bool isLoaded)
        {
            IsLoaded = isLoaded;
        }
    }
}
