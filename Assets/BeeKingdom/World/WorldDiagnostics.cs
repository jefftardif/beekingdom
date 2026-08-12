namespace BeeKingdom.World
{
    public sealed class WorldDiagnostics
    {
        public int WorldsCreated { get; private set; }
        public int RegionsGenerated { get; private set; }
        public int RegionsLoaded { get; private set; }
        public int ValidationFailures { get; private set; }
        public WorldStatistics LastStatistics { get; private set; }

        public void RecordWorldCreated(WorldStatistics statistics)
        {
            WorldsCreated++;
            LastStatistics = statistics;
        }

        public void RecordRegionGenerated()
        {
            RegionsGenerated++;
        }

        public void RecordRegionLoaded()
        {
            RegionsLoaded++;
        }

        public void RecordValidation(WorldValidationResult result)
        {
            if (result != null && !result.IsValid)
            {
                ValidationFailures++;
            }
        }
    }
}
