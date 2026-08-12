namespace BeeKingdom.Server;

public sealed class ServerIdentityOptions
{
    public const string SectionName = "ServerIdentity";

    public string GameServerId { get; set; } = "00000000-0000-0000-0000-000000000001";
    public string DefaultWorldId { get; set; } = "00000000-0000-0000-0000-000000000101";
    public string ShardName { get; set; } = "production-preparation";
}
