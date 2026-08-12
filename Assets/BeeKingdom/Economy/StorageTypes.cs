namespace BeeKingdom.Economy
{
    public enum StorageCellState { Empty, Filling, Full, Reserved, Locked, Damaged }
    public enum StoragePolicy { Nearest, Balanced, Priority, Specialized, FutureAI }

    public readonly struct StoragePosition
    {
        public int X { get; }
        public int Y { get; }
        public StoragePosition(int x, int y) { X = x; Y = y; }
        public int ManhattanDistance(StoragePosition other) => System.Math.Abs(X - other.X) + System.Math.Abs(Y - other.Y);
    }
}
