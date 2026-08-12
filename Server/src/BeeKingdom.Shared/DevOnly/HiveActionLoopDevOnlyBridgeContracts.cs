using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.DevOnly;

public sealed record HiveActionLoopDevOnlyBridgeDescriptor(
    string EvidenceId,
    bool DevOnly,
    bool NonLive,
    bool ServerOfficialClaimAllowed,
    bool OfficialSaveEnabled,
    bool OfficialEconomyEnabled,
    bool OfficialPersistentArmyEnabled,
    bool ProductionSqlAllowed,
    bool OfficialEndpointAllowed,
    bool PublishAllowed,
    bool WorldMapRuntimeAllowed,
    IReadOnlyList<string> SupportedContracts,
    HiveActionLoopIdempotencyPolicy IdempotencyPolicy,
    HiveActionLoopAntiDoubleSpendPolicy AntiDoubleSpendPolicy,
    HiveOfficialSaveFuturePreparation SaveFuturePreparation)
{
    public HiveActionLoopDevOnlyValidationResult Validate()
    {
        List<string> errors = [];

        if (!DevOnly || !NonLive)
        {
            errors.Add("Hive action loop bridge must remain dev-only and non-live.");
        }

        if (ServerOfficialClaimAllowed || OfficialSaveEnabled || OfficialEconomyEnabled || OfficialPersistentArmyEnabled)
        {
            errors.Add("Hive action loop bridge cannot claim official server progression, save, economy or persistent army.");
        }

        if (ProductionSqlAllowed || OfficialEndpointAllowed || PublishAllowed || WorldMapRuntimeAllowed)
        {
            errors.Add("Hive action loop bridge cannot allow production SQL, official endpoints, publish or world map runtime.");
        }

        string[] requiredContracts =
        [
            HiveActionLoopDevOnlyContracts.ResourceTick,
            HiveActionLoopDevOnlyContracts.ResourceCommand,
            HiveActionLoopDevOnlyContracts.UpgradeCommand,
            HiveActionLoopDevOnlyContracts.TrainingCommand,
            HiveActionLoopDevOnlyContracts.RejectionCatalog,
            HiveActionLoopDevOnlyContracts.SnapshotEnvelope,
            HiveActionLoopDevOnlyContracts.SnapshotRevision,
            HiveActionLoopDevOnlyContracts.Reconciliation,
            HiveActionLoopDevOnlyContracts.ArmySnapshot
        ];

        foreach (string contract in requiredContracts)
        {
            if (!SupportedContracts.Contains(contract, StringComparer.Ordinal))
            {
                errors.Add($"Required dev-only contract '{contract}' is missing.");
            }
        }

        if (!IdempotencyPolicy.RequiredForUpgrade || !IdempotencyPolicy.RequiredForTraining || !IdempotencyPolicy.RequiresPayloadHash)
        {
            errors.Add("Upgrade and training must require idempotency keys with payload hash.");
        }

        if (!AntiDoubleSpendPolicy.RequiresExpectedSnapshotRevision || !AntiDoubleSpendPolicy.RequiresAtomicCostAndQueueReservation)
        {
            errors.Add("Upgrade and training must require expected snapshot revision and atomic cost/queue reservation.");
        }

        if (SaveFuturePreparation.Activated || SaveFuturePreparation.OfficialSaveClaimAllowed)
        {
            errors.Add("Official save future preparation must not be activated or claimed.");
        }

        return new HiveActionLoopDevOnlyValidationResult(errors.Count == 0, errors);
    }
}

public static class HiveActionLoopDevOnlyContracts
{
    public const string ResourceTick = "ResourceTick";
    public const string ResourceCommand = "ResourceCommand";
    public const string UpgradeCommand = "UpgradeCommand";
    public const string TrainingCommand = "TrainingCommand";
    public const string RejectionCatalog = "RejectionCatalog";
    public const string SnapshotEnvelope = "SnapshotEnvelope";
    public const string SnapshotRevision = "SnapshotRevision";
    public const string Reconciliation = "Reconciliation";
    public const string ArmySnapshot = "ArmySnapshot";
}

public static class HiveActionLoopDevOnlyBridge
{
    public const string EvidenceId = "SERVER-042-BEE-858-BEE-859";

    public static HiveActionLoopDevOnlyBridgeDescriptor CreateDescriptor()
    {
        return new HiveActionLoopDevOnlyBridgeDescriptor(
            EvidenceId,
            DevOnly: true,
            NonLive: true,
            ServerOfficialClaimAllowed: false,
            OfficialSaveEnabled: false,
            OfficialEconomyEnabled: false,
            OfficialPersistentArmyEnabled: false,
            ProductionSqlAllowed: false,
            OfficialEndpointAllowed: false,
            PublishAllowed: false,
            WorldMapRuntimeAllowed: false,
            SupportedContracts:
            [
                HiveActionLoopDevOnlyContracts.ResourceTick,
                HiveActionLoopDevOnlyContracts.ResourceCommand,
                HiveActionLoopDevOnlyContracts.UpgradeCommand,
                HiveActionLoopDevOnlyContracts.TrainingCommand,
                HiveActionLoopDevOnlyContracts.RejectionCatalog,
                HiveActionLoopDevOnlyContracts.SnapshotEnvelope,
                HiveActionLoopDevOnlyContracts.SnapshotRevision,
                HiveActionLoopDevOnlyContracts.Reconciliation,
                HiveActionLoopDevOnlyContracts.ArmySnapshot
            ],
            new HiveActionLoopIdempotencyPolicy(
                RequiredForUpgrade: true,
                RequiredForTraining: true,
                RequiresPayloadHash: true,
                ReplaySamePayloadResult: "DevOnlyReplaySameResult",
                ReplayDifferentPayloadResult: "DevOnlyIdempotencyConflict"),
            new HiveActionLoopAntiDoubleSpendPolicy(
                RequiresExpectedSnapshotRevision: true,
                RequiresExpectedResourceRevision: true,
                RequiresExpectedQueueRevision: true,
                RequiresAtomicCostAndQueueReservation: true,
                RejectsClientCalculatedCost: true,
                RejectsClientCalculatedDuration: true),
            HiveOfficialSaveFuturePreparation.Create());
    }
}

public sealed record HiveActionLoopDevOnlyValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public sealed record HiveActionLoopIdempotencyPolicy(
    bool RequiredForUpgrade,
    bool RequiredForTraining,
    bool RequiresPayloadHash,
    string ReplaySamePayloadResult,
    string ReplayDifferentPayloadResult);

public sealed record HiveActionLoopAntiDoubleSpendPolicy(
    bool RequiresExpectedSnapshotRevision,
    bool RequiresExpectedResourceRevision,
    bool RequiresExpectedQueueRevision,
    bool RequiresAtomicCostAndQueueReservation,
    bool RejectsClientCalculatedCost,
    bool RejectsClientCalculatedDuration);

public sealed record HiveResourceCommandDevOnlyContract(
    Guid CommandId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string ResourceKey,
    string CommandKind,
    long ExpectedSnapshotRevision,
    long ExpectedResourceRevision,
    string IdempotencyKey,
    string PayloadHash,
    bool DevOnly,
    bool NonLive,
    bool OfficialEconomyRequested,
    ContractVersion ContractVersion);

public sealed record HiveResourceTickDevOnlyContract(
    Guid TickId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    DateTimeOffset ServerTimeUtc,
    long ExpectedSnapshotRevision,
    IReadOnlyList<HiveResourceDeltaDevOnly> Deltas,
    bool DevOnly,
    bool NonLive,
    bool OfficialEconomyApplied,
    ContractVersion ContractVersion);

public sealed record HiveUpgradeCommandDevOnlyContract(
    Guid CommandId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    BuildingId BuildingId,
    string BuildingKey,
    int FromLevel,
    int ToLevel,
    string IdempotencyKey,
    string PayloadHash,
    long ExpectedSnapshotRevision,
    long ExpectedResourceRevision,
    long ExpectedBuildingRevision,
    bool DevOnly,
    bool NonLive,
    bool OfficialProgressionRequested,
    ContractVersion ContractVersion);

public sealed record HiveTrainingCommandDevOnlyContract(
    Guid CommandId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string TroopKey,
    int Quantity,
    string IdempotencyKey,
    string PayloadHash,
    long ExpectedSnapshotRevision,
    long ExpectedResourceRevision,
    long ExpectedTrainingQueueRevision,
    bool DevOnly,
    bool NonLive,
    bool OfficialProgressionRequested,
    ContractVersion ContractVersion);

public sealed record HiveArmySnapshotDevOnlyContract(
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    long SnapshotRevision,
    IReadOnlyList<HiveArmyCountDevOnly> LocalArmyCounts,
    bool DevOnly,
    bool NonLive,
    bool OfficialPersistentArmyClaimed,
    ContractVersion ContractVersion);

public sealed record HiveActionLoopDevOnlyServerResponse(
    Guid CommandId,
    HiveActionLoopDevOnlyResponseStatus Status,
    HiveActionLoopDevOnlyErrorCode? ErrorCode,
    string Message,
    long ServerSnapshotRevision,
    string SnapshotVersion,
    bool DevOnly,
    bool NonLive,
    bool OfficialProgressionApplied,
    bool OfficialSaveApplied,
    bool OfficialEconomyApplied,
    bool OfficialPersistentArmyApplied,
    ContractVersion ContractVersion);

public sealed record HiveActionLoopFutureSnapshotSet(
    HiveResourcesSnapshotDevOnly Resources,
    HiveBuildingStateSnapshotDevOnly BuildingState,
    HiveTrainingQueueSnapshotDevOnly TrainingQueue,
    HiveArmySnapshotDevOnlyContract ArmySnapshot,
    bool DevOnly,
    bool NonLive,
    bool OfficialSaveApplied);

public sealed record HiveActionLoopSnapshotEnvelopeDevOnly(
    string SnapshotVersion,
    long SnapshotRevision,
    DateTimeOffset CreatedAtUtc,
    HiveResourcesSnapshotDevOnly Resources,
    HiveBuildingStateSnapshotDevOnly BuildingState,
    HiveTrainingQueueSnapshotDevOnly TrainingQueue,
    HiveArmySnapshotDevOnlyContract LocalArmySnapshot,
    bool DevOnly,
    bool NonLive,
    bool OfficialSaveApplied,
    ContractVersion ContractVersion);

public sealed record HiveSnapshotVersionRevisionDevOnly(
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string SnapshotVersion,
    long LocalSnapshotRevision,
    long? ServerSnapshotRevision,
    bool DevOnly,
    bool NonLive,
    bool OfficialSaveApplied,
    ContractVersion ContractVersion);

public sealed record HiveLocalServerReconciliationDevOnlyContract(
    Guid ReconciliationId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string SnapshotVersion,
    long LocalSnapshotRevision,
    long? ServerSnapshotRevision,
    HiveLocalServerReconciliationOutcome Outcome,
    HiveActionLoopDevOnlyErrorCode? ErrorCode,
    bool DevOnly,
    bool NonLive,
    bool OfficialSaveApplied,
    bool OfficialProgressionApplied,
    ContractVersion ContractVersion);

public sealed record HiveResourcesSnapshotDevOnly(
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    long SnapshotRevision,
    IReadOnlyList<HiveResourceAmountDevOnly> Resources);

public sealed record HiveBuildingStateSnapshotDevOnly(
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    long SnapshotRevision,
    IReadOnlyList<HiveBuildingStateDevOnly> Buildings);

public sealed record HiveTrainingQueueSnapshotDevOnly(
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    long SnapshotRevision,
    IReadOnlyList<HiveTrainingQueueItemDevOnly> Items);

public sealed record HiveResourceDeltaDevOnly(string ResourceKey, long Delta);

public sealed record HiveResourceAmountDevOnly(string ResourceKey, long Amount, long Capacity);

public sealed record HiveBuildingStateDevOnly(BuildingId BuildingId, string BuildingKey, int Level, bool UpgradeRunning);

public sealed record HiveTrainingQueueItemDevOnly(Guid QueueItemId, string TroopKey, int Quantity, DateTimeOffset CompleteAtUtc);

public sealed record HiveArmyCountDevOnly(string TroopKey, long LocalCount);

public sealed record HiveOfficialSaveFuturePreparation(
    bool Prepared,
    bool Activated,
    bool OfficialSaveClaimAllowed,
    bool ProductionSqlAllowed,
    bool RequiresExplicitFutureServer,
    IReadOnlyList<string> FutureSnapshotKinds,
    IReadOnlyList<string> RequiredFutureGates)
{
    public static HiveOfficialSaveFuturePreparation Create()
    {
        return new HiveOfficialSaveFuturePreparation(
            Prepared: true,
            Activated: false,
            OfficialSaveClaimAllowed: false,
            ProductionSqlAllowed: false,
            RequiresExplicitFutureServer: true,
            FutureSnapshotKinds:
            [
                HiveActionLoopSnapshotKinds.Resources,
                HiveActionLoopSnapshotKinds.BuildingState,
                HiveActionLoopSnapshotKinds.TrainingQueue,
                HiveActionLoopSnapshotKinds.LocalArmyCounts
            ],
            RequiredFutureGates:
            [
                "Architect validation",
                "SERVER migration authorization",
                "QA official save validation",
                "Production publish authorization"
            ]);
    }
}

public static class HiveActionLoopSnapshotKinds
{
    public const string Resources = "resources";
    public const string BuildingState = "building_state";
    public const string TrainingQueue = "training_queue";
    public const string LocalArmyCounts = "local_army_counts";
}

public static class HiveActionLoopDevOnlyRejectionCatalog
{
    public static IReadOnlyList<HiveActionLoopDevOnlyErrorCode> RequiredCodes { get; } =
    [
        HiveActionLoopDevOnlyErrorCode.InsufficientResources,
        HiveActionLoopDevOnlyErrorCode.AlreadyRunning,
        HiveActionLoopDevOnlyErrorCode.QueueBusy,
        HiveActionLoopDevOnlyErrorCode.CapReached,
        HiveActionLoopDevOnlyErrorCode.StaleSnapshot,
        HiveActionLoopDevOnlyErrorCode.IdempotencyConflict,
        HiveActionLoopDevOnlyErrorCode.UnknownCatalogEntry,
        HiveActionLoopDevOnlyErrorCode.Conflict
    ];

    public static IReadOnlyList<HiveActionLoopDevOnlyResponseStatus> RequiredStatuses { get; } =
    [
        HiveActionLoopDevOnlyResponseStatus.Accepted,
        HiveActionLoopDevOnlyResponseStatus.Rejected,
        HiveActionLoopDevOnlyResponseStatus.Pending,
        HiveActionLoopDevOnlyResponseStatus.StaleSnapshot,
        HiveActionLoopDevOnlyResponseStatus.Conflict,
        HiveActionLoopDevOnlyResponseStatus.CapReached,
        HiveActionLoopDevOnlyResponseStatus.InsufficientResources,
        HiveActionLoopDevOnlyResponseStatus.AlreadyRunning,
        HiveActionLoopDevOnlyResponseStatus.QueueBusy
    ];
}

public enum HiveActionLoopDevOnlyResponseStatus
{
    Accepted = 0,
    Rejected = 1,
    Pending = 2,
    StaleSnapshot = 3,
    Conflict = 4,
    CapReached = 5,
    InsufficientResources = 6,
    AlreadyRunning = 7,
    QueueBusy = 8
}

public enum HiveActionLoopDevOnlyErrorCode
{
    InsufficientResources = 0,
    AlreadyRunning = 1,
    QueueBusy = 2,
    CapReached = 3,
    StaleSnapshot = 4,
    IdempotencyConflict = 5,
    UnknownCatalogEntry = 6,
    Conflict = 7
}

public enum HiveLocalServerReconciliationOutcome
{
    LocalPreviewOnly = 0,
    ServerSnapshotPreferred = 1,
    RefreshRequired = 2,
    ConflictRequiresFutureOfficialAuthority = 3
}
