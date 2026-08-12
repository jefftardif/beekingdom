namespace BeeKingdom.Hive
{
    public sealed class HiveGrowthDiagnostics
    {
        public int PlannedChambers { get; private set; }
        public int CompletedChambers { get; private set; }
        public int TopologyRevisions { get; private set; }
        public int ValidationFailures { get; private set; }

        public void RecordPlan()
        {
            PlannedChambers++;
        }

        public void RecordCompletion()
        {
            CompletedChambers++;
        }

        public void RecordTopologyRevision(int revision)
        {
            TopologyRevisions = revision;
        }

        public void RecordValidation(HiveLayoutValidationResult result)
        {
            if (result != null && !result.IsValid)
            {
                ValidationFailures++;
            }
        }
    }
}
