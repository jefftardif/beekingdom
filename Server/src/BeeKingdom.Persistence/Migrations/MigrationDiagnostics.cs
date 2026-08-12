namespace BeeKingdom.Persistence.Migrations;

public sealed class MigrationDiagnostics
{
    private long pendingChecks;
    private long applyAttempts;
    private long appliedScripts;
    private long failures;
    private string? lastFailure;
    private string? lastScript;
    private DateTimeOffset? lastFailureUtc;
    private DateTimeOffset? lastSuccessUtc;
    private readonly object sync = new();

    public long PendingChecks => Interlocked.Read(ref pendingChecks);
    public long ApplyAttempts => Interlocked.Read(ref applyAttempts);
    public long AppliedScripts => Interlocked.Read(ref appliedScripts);
    public long Failures => Interlocked.Read(ref failures);

    public string? LastFailure
    {
        get { lock (sync) { return lastFailure; } }
    }

    public string? LastScript
    {
        get { lock (sync) { return lastScript; } }
    }

    public DateTimeOffset? LastFailureUtc
    {
        get { lock (sync) { return lastFailureUtc; } }
    }

    public DateTimeOffset? LastSuccessUtc
    {
        get { lock (sync) { return lastSuccessUtc; } }
    }

    public void RecordPendingCheck() => Interlocked.Increment(ref pendingChecks);
    public void RecordApplyAttempt() => Interlocked.Increment(ref applyAttempts);

    public void RecordScriptApplied(string scriptName)
    {
        Interlocked.Increment(ref appliedScripts);
        lock (sync)
        {
            lastScript = scriptName;
            lastSuccessUtc = DateTimeOffset.UtcNow;
        }
    }

    public void RecordFailure(Exception exception)
    {
        Interlocked.Increment(ref failures);
        lock (sync)
        {
            lastFailure = exception.GetType().Name + ": " + exception.Message;
            lastFailureUtc = DateTimeOffset.UtcNow;
        }
    }
}
