namespace BeeKingdom.Hive
{
    public sealed class HiveDiagnostics
    {
        public int Population { get; private set; }
        public int Capacity { get; private set; }
        public int BuildingCount { get; private set; }
        public int InventoryCount { get; private set; }
        public bool LastValidationPassed { get; private set; }
        public int ValidationErrorCount { get; private set; }

        public void Record(HiveStatistics statistics, int validationErrorCount)
        {
            Population = statistics.Population;
            Capacity = statistics.Capacity.MaxPopulation;
            BuildingCount = statistics.BuildingCount;
            InventoryCount = statistics.InventoryCount;
            LastValidationPassed = statistics.IsValid;
            ValidationErrorCount = validationErrorCount;
        }
    }
}
