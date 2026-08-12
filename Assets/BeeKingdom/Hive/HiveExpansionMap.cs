using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public sealed class HiveExpansionMap
    {
        private readonly HashSet<string> unlockedCells = new HashSet<string>();

        public int UnlockedCellCount => unlockedCells.Count;

        public bool Unlock(string cellId)
        {
            if (string.IsNullOrWhiteSpace(cellId))
            {
                return false;
            }

            return unlockedCells.Add(cellId);
        }

        public bool IsUnlocked(string cellId)
        {
            return unlockedCells.Contains(cellId);
        }
    }
}
