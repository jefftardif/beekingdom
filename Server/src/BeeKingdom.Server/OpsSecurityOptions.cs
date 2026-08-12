namespace BeeKingdom.Server;

public sealed class OpsSecurityOptions
{
    public const string SectionName = "Ops";

    public bool RequireAdminKey { get; set; } = true;
    public string AdminKey { get; set; } = string.Empty;
    public string AdminKeySha256 { get; set; } = string.Empty;
    public bool RequireMigrationApplyKey { get; set; } = true;
    public string MigrationApplyKey { get; set; } = string.Empty;
    public string MigrationApplyKeySha256 { get; set; } = string.Empty;
}
