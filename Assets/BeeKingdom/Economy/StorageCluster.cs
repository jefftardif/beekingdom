using System.Collections.Generic;

namespace BeeKingdom.Economy
{
    public sealed class StorageCluster
    {
        private readonly List<StorageCell> cells = new List<StorageCell>();
        public string ClusterId { get; }
        public ResourceType ResourceType { get; }
        public IReadOnlyList<StorageCell> Cells => cells;
        public bool IsFull
        {
            get
            {
                for (int i = 0; i < cells.Count; i++) if (cells[i].AvailableSpace > 0d) return false;
                return cells.Count > 0;
            }
        }

        public StorageCluster(string clusterId, ResourceType resourceType) { ClusterId = clusterId; ResourceType = resourceType; }
        public void AddCell(StorageCell cell) { if (cell.ResourceType == ResourceType) cells.Add(cell); }
    }
}
