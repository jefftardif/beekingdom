namespace BeeKingdom.Authentication.Diagnostics;

public sealed class AuthenticationDiagnostics
{
    public long SuccessfulLogins { get; private set; }
    public long RefusedLogins { get; private set; }
    public long InvalidAttempts { get; private set; }
    public long ActiveSessions { get; private set; }
    public long ExpiredSessions { get; private set; }
    public long TotalAuthenticationTicks { get; private set; }

    public double AverageAuthenticationTicks => SuccessfulLogins + RefusedLogins == 0 ? 0 : TotalAuthenticationTicks / (double)(SuccessfulLogins + RefusedLogins);

    public void RecordSuccess(long ticks)
    {
        SuccessfulLogins++;
        ActiveSessions++;
        TotalAuthenticationTicks += ticks;
    }

    public void RecordFailure(long ticks)
    {
        RefusedLogins++;
        InvalidAttempts++;
        TotalAuthenticationTicks += ticks;
    }

    public void RecordSessionClosed()
    {
        if (ActiveSessions > 0)
        {
            ActiveSessions--;
        }
    }
}
