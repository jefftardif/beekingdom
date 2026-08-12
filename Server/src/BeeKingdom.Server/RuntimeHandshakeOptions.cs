namespace BeeKingdom.Server;

public sealed class RuntimeHandshakeOptions
{
    public const string SectionName = "RuntimeHandshake";

    public string Availability { get; set; } = "ServerInPreparation";
    public string MaintenanceMessage { get; set; } = "Serveur Bee Kingdom en preparation.";
    public string FallbackMode { get; set; } = "LocalOnly";
}
