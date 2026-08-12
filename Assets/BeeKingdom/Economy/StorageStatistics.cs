namespace BeeKingdom.Economy
{
    public readonly struct StorageStatistics
    {
        public int CellCount { get; }
        public double TotalAmount { get; }
        public double TotalCapacity { get; }
        public StorageStatistics(int cellCount, double totalAmount, double totalCapacity) { CellCount = cellCount; TotalAmount = totalAmount; TotalCapacity = totalCapacity; }
    }
}
