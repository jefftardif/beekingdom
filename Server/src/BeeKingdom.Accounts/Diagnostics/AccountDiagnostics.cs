namespace BeeKingdom.Accounts.Diagnostics;

public sealed class AccountDiagnostics
{
    public long TotalAccounts { get; private set; }
    public long ActiveAccounts { get; private set; }
    public long SuspendedAccounts { get; private set; }
    public long DailyCreations { get; private set; }
    public long Modifications { get; private set; }
    public long ProcessingTicks { get; private set; }

    public double AverageProcessingTicks => Modifications + DailyCreations == 0 ? 0 : ProcessingTicks / (double)(Modifications + DailyCreations);

    public void RecordCreated(long ticks)
    {
        TotalAccounts++;
        DailyCreations++;
        ProcessingTicks += ticks;
    }

    public void RecordUpdated(long ticks)
    {
        Modifications++;
        ProcessingTicks += ticks;
    }

    public void SetStatusCounts(long active, long suspended)
    {
        ActiveAccounts = active;
        SuspendedAccounts = suspended;
    }
}
