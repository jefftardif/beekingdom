namespace BeeKingdom.Gateway;

public sealed class GatewayHost
{
    public string HostId { get; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;
}
