namespace BeeKingdom.Server;

public sealed class NewsAutomationOptions
{
    public const string SectionName = "NewsAutomation";

    public bool Enabled { get; set; }
    public string KeyFile { get; set; } = string.Empty;
}
