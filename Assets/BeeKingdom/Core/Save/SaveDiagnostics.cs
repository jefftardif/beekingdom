namespace BeeKingdom.Core.Save
{
    public sealed class SaveDiagnostics
    {
        public int SaveCount { get; private set; }
        public int LoadCount { get; private set; }
        public int AutoSaveCount { get; private set; }
        public int DeleteCount { get; private set; }
        public int ValidationFailureCount { get; private set; }
        public int MigrationCount { get; private set; }
        public int IncrementalSkipCount { get; private set; }
        public string LastSlot { get; private set; } = string.Empty;

        public void RecordSave(string slot) { SaveCount++; LastSlot = slot ?? string.Empty; }
        public void RecordLoad(string slot) { LoadCount++; LastSlot = slot ?? string.Empty; }
        public void RecordAutoSave(string slot) { AutoSaveCount++; LastSlot = slot ?? string.Empty; }
        public void RecordDelete(string slot) { DeleteCount++; LastSlot = slot ?? string.Empty; }
        public void RecordValidationFailure() { ValidationFailureCount++; }
        public void RecordMigration() { MigrationCount++; }
        public void RecordIncrementalSkip() { IncrementalSkipCount++; }
    }
}
