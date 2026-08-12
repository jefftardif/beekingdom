using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class HexPathIndex
    {
        private readonly Dictionary<HexCoordinates, int> movementCosts = new Dictionary<HexCoordinates, int>();

        public void SetCost(HexCoordinates coordinates, int movementCost)
        {
            movementCosts[coordinates] = movementCost <= 0 ? 1 : movementCost;
        }

        public int GetCost(HexCoordinates coordinates)
        {
            return movementCosts.TryGetValue(coordinates, out int cost) ? cost : 1;
        }
    }
}
