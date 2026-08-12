namespace BeeKingdom.Infrastructure.Time;

public sealed class SystemServerClock : IServerClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
