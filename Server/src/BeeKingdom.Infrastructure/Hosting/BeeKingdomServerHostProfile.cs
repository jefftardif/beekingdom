namespace BeeKingdom.Infrastructure.Hosting;

public sealed class BeeKingdomServerHostProfile
{
    public const string SectionName = "ServerHost";

    public string HostingModel { get; set; } = "IIS";
    public string TargetOperatingSystem { get; set; } = "Windows Server 2025";
    public string SqlServerRole { get; set; } = "Dedicated Bee Kingdom SQL Server database";
}
