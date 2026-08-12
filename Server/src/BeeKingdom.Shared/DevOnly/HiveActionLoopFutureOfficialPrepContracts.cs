using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.DevOnly;

public static class HiveActionLoopFutureOfficialPrep
{
    public const string EvidenceId = "SERVER-044-BEE-887-BEE-891";
    public const string SnapshotVersion = "future-official-hive-action-loop-v1";

    public static HiveOfficialPersistenceRequirementsInventory CreatePersistenceInventory()
    {
        return new HiveOfficialPersistenceRequirementsInventory(
            EvidenceId,
            DevOnly: true,
            NonLive: true,
            OfficialSaveActive: false,
            OfficialEconomyActive: false,
            OfficialPersistentArmyActive: false,
            ProductionMigrationAllowed: false,
            OfficialEndpointAllowed: false,
            WorldMapScopeAllowed: false,
            CandidateData:
            [
                new HiveFuturePersistentDataCandidate(HiveActionLoopSnapshotKinds.Resources, "player_resources", "Future authoritative resources by PlayerId, WorldId and GameServerId.", RequiresExpectedRevision: true, RequiresIdempotency: true, AllowedNow: false),
                new HiveFuturePersistentDataCandidate(HiveActionLoopSnapshotKinds.BuildingState, "hive_buildings", "Future authoritative building levels and upgrade state.", RequiresExpectedRevision: true, RequiresIdempotency: true, AllowedNow: false),
                new HiveFuturePersistentDataCandidate(HiveActionLoopSnapshotKinds.TrainingQueue, "training_queue", "Future authoritative training queue entries.", RequiresExpectedRevision: true, RequiresIdempotency: true, AllowedNow: false),
                new HiveFuturePersistentDataCandidate(HiveActionLoopSnapshotKinds.LocalArmyCounts, "troop_counts", "Future authoritative troop counts derived from completed training.", RequiresExpectedRevision: true, RequiresIdempotency: false, AllowedNow: false),
                new HiveFuturePersistentDataCandidate("action_history", "hive_action_history", "Future audit of accepted, rejected and replayed commands.", RequiresExpectedRevision: false, RequiresIdempotency: true, AllowedNow: false),
                new HiveFuturePersistentDataCandidate("snapshot_revision", "hive_snapshot_revisions", "Future snapshot version and revision audit per player/world/server scope.", RequiresExpectedRevision: true, RequiresIdempotency: false, AllowedNow: false)
            ],
            ForbiddenLiveClaims:
            [
                "official live server",
                "official save",
                "official economy",
                "official persistent army",
                "production SQL migration",
                "world map"
            ],
            RequiredFutureGates:
            [
                "Architect official persistence authorization",
                "SERVER migration specification",
                "QA official save validation",
                "Production publish authorization"
            ],
            ContractVersion.Current);
    }

    public static HiveFutureAuthoritativeActionHandlerHandoff CreateHandlerHandoff()
    {
        return new HiveFutureAuthoritativeActionHandlerHandoff(
            EvidenceId,
            DevOnly: true,
            NonLive: true,
            HandlerLive: false,
            RepositoryLive: false,
            MigrationLive: false,
            OfficialEndpointLive: false,
            OfficialSaveActive: false,
            FutureHandlers:
            [
                "ResourceCommandHandler",
                "UpgradeCommandHandler",
                "TrainingCommandHandler",
                "SnapshotReadHandler",
                "ReconciliationDecisionHandler"
            ],
            FutureRepositories:
            [
                "PlayerResourcesRepository",
                "HiveBuildingsRepository",
                "ConstructionQueueRepository",
                "TrainingQueueRepository",
                "IdempotencyRecordsRepository",
                "SnapshotRevisionRepository"
            ],
            FutureEndpoints:
            [
                "POST /players/{playerId}/hive/resources/commands",
                "POST /players/{playerId}/hive/buildings/{buildingId}/upgrade",
                "POST /players/{playerId}/hive/training",
                "GET /players/{playerId}/hive/snapshot",
                "POST /players/{playerId}/hive/reconciliation"
            ],
            RequiredFutureGates:
            [
                "Architect endpoint authorization",
                "SERVER authoritative handler implementation",
                "SQL migration approval",
                "QA anti double-spend validation",
                "Production publish authorization"
            ],
            ContractVersion.Current);
    }
}

public sealed record HiveOfficialPersistenceRequirementsInventory(
    string EvidenceId,
    bool DevOnly,
    bool NonLive,
    bool OfficialSaveActive,
    bool OfficialEconomyActive,
    bool OfficialPersistentArmyActive,
    bool ProductionMigrationAllowed,
    bool OfficialEndpointAllowed,
    bool WorldMapScopeAllowed,
    IReadOnlyList<HiveFuturePersistentDataCandidate> CandidateData,
    IReadOnlyList<string> ForbiddenLiveClaims,
    IReadOnlyList<string> RequiredFutureGates,
    ContractVersion ContractVersion);

public sealed record HiveFuturePersistentDataCandidate(
    string SnapshotKind,
    string FutureStorageName,
    string Purpose,
    bool RequiresExpectedRevision,
    bool RequiresIdempotency,
    bool AllowedNow);

public sealed record HiveFutureIdempotencyReplaySafetyPolicy(
    bool DevOnly,
    bool NonLive,
    bool IdempotencyKeyRequired,
    bool PayloadHashRequired,
    bool SamePayloadReturnsSameResult,
    bool DifferentPayloadRejectedAsConflict,
    bool OfficialProgressionApplied,
    bool OfficialEconomyApplied,
    ContractVersion ContractVersion)
{
    public HiveFutureIdempotencyReplayDecision DecideReplay(string originalPayloadHash, string replayPayloadHash)
    {
        return string.Equals(originalPayloadHash, replayPayloadHash, StringComparison.Ordinal)
            ? HiveFutureIdempotencyReplayDecision.SamePayloadReplay
            : HiveFutureIdempotencyReplayDecision.DifferentPayloadConflict;
    }
}

public sealed record HiveSnapshotDeltaAuditDevOnlyContract(
    Guid AuditId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string SnapshotVersion,
    long BeforeRevision,
    long AfterRevision,
    IReadOnlyList<HiveSnapshotDeltaEntryDevOnly> Deltas,
    bool DevOnly,
    bool NonLive,
    bool OfficialSaveApplied,
    bool OfficialProgressionApplied,
    ContractVersion ContractVersion);

public sealed record HiveSnapshotDeltaEntryDevOnly(
    string SnapshotKind,
    string Field,
    string BeforeValue,
    string AfterValue,
    string Reason);

public sealed record HiveLocalServerReconciliationDrillDevOnly(
    Guid DrillId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string SnapshotVersion,
    long LocalSnapshotRevision,
    long FutureServerSnapshotRevision,
    HiveLocalServerReconciliationOutcome Outcome,
    HiveActionLoopDevOnlyErrorCode? ErrorCode,
    string PlayerFacingState,
    bool DevOnly,
    bool NonLive,
    bool OfficialRestoreApplied,
    bool OfficialSaveApplied,
    bool HandlerLive,
    ContractVersion ContractVersion);

public sealed record HiveFutureAuthoritativeActionHandlerHandoff(
    string EvidenceId,
    bool DevOnly,
    bool NonLive,
    bool HandlerLive,
    bool RepositoryLive,
    bool MigrationLive,
    bool OfficialEndpointLive,
    bool OfficialSaveActive,
    IReadOnlyList<string> FutureHandlers,
    IReadOnlyList<string> FutureRepositories,
    IReadOnlyList<string> FutureEndpoints,
    IReadOnlyList<string> RequiredFutureGates,
    ContractVersion ContractVersion);

public enum HiveFutureIdempotencyReplayDecision
{
    SamePayloadReplay = 0,
    DifferentPayloadConflict = 1
}
