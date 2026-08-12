using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class HexGrid
    {
        private readonly Dictionary<HexCoordinates, HexCell> cells = new Dictionary<HexCoordinates, HexCell>();
        private readonly HashSet<WorldChunkCoordinate> loadedChunks = new HashSet<WorldChunkCoordinate>();
        private readonly HexRegionIndex regionIndex = new HexRegionIndex();
        private readonly HexPathIndex pathIndex = new HexPathIndex();

        public int ChunkSize { get; }
        public IReadOnlyDictionary<HexCoordinates, HexCell> Cells => cells;
        public HexRegionIndex RegionIndex => regionIndex;
        public HexPathIndex PathIndex => pathIndex;

        public HexGrid(int chunkSize = 16)
        {
            ChunkSize = chunkSize <= 0 ? 16 : chunkSize;
        }

        public HexCell CreateCell(HexCoordinates coordinates, string regionId = null, int movementCost = 1)
        {
            HexCell cell = new HexCell(coordinates, coordinates.ToChunkCoordinate(ChunkSize), regionId, movementCost);
            cells[coordinates] = cell;
            pathIndex.SetCost(coordinates, movementCost);
            if (!string.IsNullOrWhiteSpace(regionId))
            {
                regionIndex.Add(regionId, coordinates);
            }

            return cell;
        }

        public HexCell GetCell(HexCoordinates coordinates)
        {
            if (cells.TryGetValue(coordinates, out HexCell cell))
            {
                return cell;
            }

            return CreateCell(coordinates);
        }

        public IReadOnlyList<HexCell> GetNeighbors(HexCoordinates coordinates)
        {
            HexCell[] neighbors = new HexCell[6];
            for (int i = 0; i < 6; i++)
            {
                neighbors[i] = GetCell(coordinates.Neighbor(i));
            }

            return neighbors;
        }

        public void MapWorldRegion(WorldRegion region)
        {
            int centerQ = region.Coordinate.X * ChunkSize;
            int centerR = region.Coordinate.Y * ChunkSize;
            HexCoordinates center = new HexCoordinates(centerQ, centerR);
            CreateCell(center, region.RegionId, MovementCostFor(region));
            for (int i = 0; i < 6; i++)
            {
                CreateCell(center.Neighbor(i), region.RegionId, MovementCostFor(region));
            }
        }

        public void LoadChunk(WorldChunkCoordinate coordinate)
        {
            loadedChunks.Add(coordinate);
            foreach (HexCell cell in cells.Values)
            {
                if (cell.ChunkCoordinate.Equals(coordinate))
                {
                    cell.SetLoaded(true);
                }
            }
        }

        public void UnloadChunk(WorldChunkCoordinate coordinate)
        {
            loadedChunks.Remove(coordinate);
            foreach (HexCell cell in cells.Values)
            {
                if (cell.ChunkCoordinate.Equals(coordinate))
                {
                    cell.SetLoaded(false);
                }
            }
        }

        public bool IsChunkLoaded(WorldChunkCoordinate coordinate)
        {
            return loadedChunks.Contains(coordinate);
        }

        public HexGridSnapshot CreateSnapshot()
        {
            return new HexGridSnapshot(ChunkSize, new List<HexCell>(cells.Values));
        }

        public static HexGrid FromWorld(WorldState world, int chunkSize = 16)
        {
            HexGrid grid = new HexGrid(chunkSize);
            foreach (WorldRegion region in world.Regions.Values)
            {
                grid.MapWorldRegion(region);
            }

            return grid;
        }

        private static int MovementCostFor(WorldRegion region)
        {
            return region.BiomeType == WorldBiomeType.Mountain ? 3 :
                region.BiomeType == WorldBiomeType.Marsh ? 2 :
                1;
        }
    }
}
