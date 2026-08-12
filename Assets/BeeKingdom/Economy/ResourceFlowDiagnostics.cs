namespace BeeKingdom.Economy
{
    public sealed class ResourceFlowDiagnostics
    {
        public int TransactionCount { get; private set; }
        public int ShortageCount { get; private set; }
        public int StorageFullCount { get; private set; }

        public void RecordTransaction() { TransactionCount++; }
        public void RecordShortage() { ShortageCount++; }
        public void RecordStorageFull() { StorageFullCount++; }
    }
}
