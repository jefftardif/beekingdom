namespace BeeKingdom.HiveOperations;

public sealed class HiveOperationDiagnostics
{
    private long accepted;
    private long rejected;
    private long idempotentReplays;
    private long revisionConflicts;

    public void RecordResult(string code, bool succeeded)
    {
        if (succeeded) Interlocked.Increment(ref accepted); else Interlocked.Increment(ref rejected);
        if (code == "revision_conflict") Interlocked.Increment(ref revisionConflicts);
    }

    public void RecordReplay(string code)
    {
        Interlocked.Increment(ref idempotentReplays);
        if (code == "revision_conflict") Interlocked.Increment(ref revisionConflicts);
    }

    public HiveOperationDiagnosticsSnapshot Snapshot() => new(
        Interlocked.Read(ref accepted),
        Interlocked.Read(ref rejected),
        Interlocked.Read(ref idempotentReplays),
        Interlocked.Read(ref revisionConflicts));
}

public sealed record HiveOperationDiagnosticsSnapshot(long Accepted, long Rejected, long IdempotentReplays, long RevisionConflicts);
