namespace BeeKingdom.Data
{
    public sealed class RegistryDiagnostics
    {
        public int DefinitionCount { get; private set; }
        public long EstimatedMemoryBytes { get; private set; }
        public long LastLoadTicks { get; private set; }
        public int ValidationErrorCount { get; private set; }
        public int ValidationWarningCount { get; private set; }
        public int ReloadCount { get; private set; }

        public void RecordLoad(int definitionCount, long estimatedMemoryBytes, long elapsedTicks)
        {
            DefinitionCount = definitionCount;
            EstimatedMemoryBytes = estimatedMemoryBytes;
            LastLoadTicks = elapsedTicks;
            ReloadCount++;
        }

        public void RecordValidation(RegistryValidationResult result)
        {
            ValidationErrorCount = result.ErrorCount;
            ValidationWarningCount = result.WarningCount;
        }
    }
}
