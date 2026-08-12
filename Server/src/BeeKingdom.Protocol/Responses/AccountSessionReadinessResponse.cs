namespace BeeKingdom.Protocol.Responses;

public sealed record AccountSessionReadinessResponse(
    string Service,
    DateTimeOffset ServerTimeUtc,
    string Environment,
    string GameServerId,
    string DefaultWorldId,
    string ShardName,
    string ProductionTarget,
    string PersistenceProvider,
    bool UsesSqlServer,
    bool RuntimeConnectionConfigured,
    bool MigrationConnectionConfigured,
    bool AccountRepositoryConfigured,
    bool CredentialStoreConfigured,
    bool SessionStoreConfigured,
    string AccountStatus,
    string SessionStatus,
    string CredentialStatus,
    string ColonyReadModelStatus,
    bool AccountCreationAllowed,
    bool SessionCreationAllowed,
    bool TokenIssuanceAllowed,
    bool OfficialPersistenceClaimAllowed,
    bool RequiresProductionRouteProof,
    bool RequiresBackupEvidence,
    bool RequiresRollbackApproval,
    bool SecretsAllowedInResponse,
    AccountSessionReadinessClaims Claims,
    IReadOnlyList<string> Blockers);

public sealed record AccountSessionReadinessClaims(
    bool LiveAccounts,
    bool LiveSessions,
    bool OfficialProgression,
    bool OfficialPersistence,
    bool RealTimeSynchronization,
    bool GameplayAuthorityGranted);
