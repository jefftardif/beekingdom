namespace BeeKingdom.Server;

public sealed class AccountSessionReadinessOptions
{
    public const string SectionName = "AccountSessionReadiness";

    public string ProductionTarget { get; set; } = "104.129.128.136";
    public string AccountStatus { get; set; } = "NotLive";
    public string SessionStatus { get; set; } = "NotLive";
    public string CredentialStatus { get; set; } = "PreparationOnly";
    public string ColonyReadModelStatus { get; set; } = "PreparationOnly";
    public bool AccountCreationAllowed { get; set; }
    public bool SessionCreationAllowed { get; set; }
    public bool TokenIssuanceAllowed { get; set; }
    public bool OfficialPersistenceClaimAllowed { get; set; }
    public bool RequiresProductionRouteProof { get; set; } = true;
    public bool RequiresBackupEvidence { get; set; } = true;
    public bool RequiresRollbackApproval { get; set; } = true;
}
