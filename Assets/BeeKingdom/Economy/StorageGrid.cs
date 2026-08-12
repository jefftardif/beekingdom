using System.Collections.Generic;

namespace BeeKingdom.Economy
{
    public sealed class StorageGrid
    {
        private readonly Dictionary<string, StorageCell> cells = new Dictionary<string, StorageCell>();
        public IReadOnlyDictionary<string, StorageCell> Cells => cells;
        public void AddCell(StorageCell cell) { cells[cell.CellId] = cell; }
        public bool TryGetCell(string cellId, out StorageCell cell) => cells.TryGetValue(cellId, out cell);
    }
}
