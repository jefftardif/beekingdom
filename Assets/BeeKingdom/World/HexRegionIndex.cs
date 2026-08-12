using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class HexRegionIndex
    {
        private readonly Dictionary<string, List<HexCoordinates>> cellsByRegion = new Dictionary<string, List<HexCoordinates>>();

        public void Add(string regionId, HexCoordinates coordinates)
        {
            if (!cellsByRegion.TryGetValue(regionId, out List<HexCoordinates> cells))
            {
                cells = new List<HexCoordinates>();
                cellsByRegion[regionId] = cells;
            }

            cells.Add(coordinates);
        }

        public IReadOnlyList<HexCoordinates> GetCells(string regionId)
        {
            return cellsByRegion.TryGetValue(regionId, out List<HexCoordinates> cells) ? cells : new HexCoordinates[0];
        }
    }
}
