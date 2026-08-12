namespace BeeKingdom.Core.Attributes
{
    public sealed class GameplayAttributeDiagnostics
    {
        public int RegisteredAttributes { get; private set; }
        public int AttributeSets { get; private set; }
        public int Changes { get; private set; }
        public int Recalculations { get; private set; }
        public int Clamps { get; private set; }
        public int Snapshots { get; private set; }
        public int Restores { get; private set; }

        public void RecordRegistered(int count) { RegisteredAttributes = count; }
        public void RecordSets(int count) { AttributeSets = count; }
        public void RecordChange() { Changes++; }
        public void RecordRecalculation() { Recalculations++; }
        public void RecordClamp() { Clamps++; }
        public void RecordSnapshot() { Snapshots++; }
        public void RecordRestore() { Restores++; }
    }
}
