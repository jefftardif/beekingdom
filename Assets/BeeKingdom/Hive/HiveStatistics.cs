namespace BeeKingdom.Hive
{
    public readonly struct HiveStatistics
    {
        public int Population { get; }
        public int BuildingCount { get; }
        public int InventoryCount { get; }
        public HiveCapacity Capacity { get; }
        public bool IsValid { get; }

        public HiveStatistics(int population, int buildingCount, int inventoryCount, HiveCapacity capacity, bool isValid)
        {
            Population = population;
            BuildingCount = buildingCount;
            InventoryCount = inventoryCount;
            Capacity = capacity;
            IsValid = isValid;
        }
    }
}
