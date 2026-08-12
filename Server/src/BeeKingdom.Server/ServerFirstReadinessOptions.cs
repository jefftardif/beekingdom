namespace BeeKingdom.Server;

public sealed class ServerFirstReadinessOptions
{
    public const string SectionName = "ServerFirstReadiness";

    public string ProductionTarget { get; set; } = "104.129.128.136";
    public string HandshakePath { get; set; } = "/runtime/handshake";
    public bool ProductionRouteProven { get; set; }
    public string ProductionRouteStatus { get; set; } = "NotRouted";
    public string OfflineMode { get; set; } = "ConsultationOnly";
    public string AccountStatus { get; set; } = "NotLive";
    public string SessionStatus { get; set; } = "NotLive";
    public string ColonyReadModelStatus { get; set; } = "PreparationOnly";
}
