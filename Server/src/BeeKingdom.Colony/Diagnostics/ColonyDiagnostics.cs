namespace BeeKingdom.Colony.Diagnostics;

public sealed class ColonyDiagnostics
{
    public long ActiveColonies { get; private set; }
    public long LoadedColonies { get; private set; }
    public long SaveTicks { get; private set; }
    public long LoadTicks { get; private set; }
    public long SnapshotBytes { get; private set; }
    public long SnapshotCount { get; private set; }
    public long PersistenceErrors { get; private set; }

    public double AverageSnapshotBytes => SnapshotCount == 0 ? 0 : SnapshotBytes / (double)SnapshotCount;

    public void RecordLoaded(long ticks)
    {
        LoadedColonies++;
        LoadTicks += ticks;
    }

    public void RecordSaved(long ticks, int bytes)
    {
        SaveTicks += ticks;
        SnapshotBytes += bytes;
        SnapshotCount++;
    }

    public void RecordPersistenceError() => PersistenceErrors++;
    public void SetActiveColonies(long count) => ActiveColonies = count;
}
