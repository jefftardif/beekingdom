namespace BeeKingdom.Server;

public sealed class SqlProductionDryRunOptions
{
    public const string SectionName = "SqlProductionDryRun";

    public string TargetHost { get; set; } = "104.129.128.136";
    public bool RequireBackupEvidence { get; set; } = true;
    public string BackupEvidenceReference { get; set; } = string.Empty;
    public bool RequireMaintenanceWindow { get; set; } = true;
    public string MaintenanceWindowReference { get; set; } = string.Empty;
    public bool RollbackPlanAcknowledged { get; set; }
}
