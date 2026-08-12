namespace BeeKingdom.Protocol.Responses;

public sealed record ServerFirstReadinessResponse(
    string Service,
    DateTimeOffset ServerTimeUtc,
    string Environment,
    string GameServerId,
    string DefaultWorldId,
    string ShardName,
    string ProductionTarget,
    string HandshakePath,
    bool OfficialServerRequired,
    bool ProductionRouteProven,
    string ProductionRouteStatus,
    string OfflineMode,
    string AccountStatus,
    string SessionStatus,
    string ColonyReadModelStatus,
    bool GameplayAuthorityGranted,
    bool MutationAllowed,
    bool BackupRequiredBeforeDeployment,
    bool RollbackRequiresApproval,
    bool SecretsAllowedInReports,
    ServerFirstForbiddenClaims ForbiddenClaims);

public sealed record ServerFirstForbiddenClaims(
    bool OfflineOfficialPlay,
    bool AccountLive,
    bool SessionLive,
    bool OfficialPersistence,
    bool OfficialProgression,
    bool RealTimeSynchronization,
    bool Economy,
    bool Social,
    bool Ranking,
    bool Matchmaking);
