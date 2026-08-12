namespace BeeKingdom.Infrastructure.Configuration;

public sealed class BeeKingdomServerOptions
{
    public const string SectionName = "BeeKingdom";

    public string ServerName { get; set; } = "BeeKingdom";
    public bool EnableDiagnostics { get; set; } = true;
    public bool EnableBackgroundWorkers { get; set; } = true;
    public int EventHistoryLimit { get; set; } = 1024;
}
