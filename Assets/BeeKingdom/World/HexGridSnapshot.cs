using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class HexGridSnapshot
    {
        public int ChunkSize { get; }
        public IReadOnlyList<HexCell> Cells { get; }

        public HexGridSnapshot(int chunkSize, IReadOnlyList<HexCell> cells)
        {
            ChunkSize = chunkSize;
            Cells = cells;
        }
    }
}
