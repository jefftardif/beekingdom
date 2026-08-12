namespace BeeKingdom.Gateway.Configuration;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public int MaxConnections { get; set; } = 10_000;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(15);
    public int MaxMessageBytes { get; set; } = 64 * 1024;
    public int PlayerMessagesPerMinute { get; set; } = 120;
    public int SessionMessagesPerMinute { get; set; } = 120;
    public int IpMessagesPerMinute { get; set; } = 300;
    public int MessageTypePerMinute { get; set; } = 240;
}
