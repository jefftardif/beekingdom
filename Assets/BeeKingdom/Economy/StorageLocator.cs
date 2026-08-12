namespace BeeKingdom.Economy
{
    public sealed class StorageLocator
    {
        public bool TryFind(StorageGrid grid, ResourceType type, double amount, StoragePosition origin, StoragePolicy policy, out StorageCell cell)
        {
            cell = null;
            int bestDistance = int.MaxValue;
            double bestSpace = -1d;
            foreach (StorageCell candidate in grid.Cells.Values)
            {
                if (candidate.ResourceType != type || candidate.AvailableSpace < amount || candidate.State == StorageCellState.Full || candidate.State == StorageCellState.Locked || candidate.State == StorageCellState.Damaged)
                {
                    continue;
                }

                int distance = candidate.Position.ManhattanDistance(origin);
                double space = candidate.AvailableSpace;
                bool better = policy == StoragePolicy.Balanced ? space > bestSpace : distance < bestDistance;
                if (better)
                {
                    cell = candidate;
                    bestDistance = distance;
                    bestSpace = space;
                }
            }

            return cell != null;
        }
    }
}
