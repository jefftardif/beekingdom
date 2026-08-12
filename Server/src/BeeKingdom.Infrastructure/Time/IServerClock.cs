namespace BeeKingdom.Infrastructure.Time;

public interface IServerClock
{
    DateTimeOffset UtcNow { get; }
}
