using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Shared.Persistence;

public static class HiveLoopPersistenceReadinessTables
{
    public const string PlayerResources = "player_resources";
    public const string HiveBuildings = "hive_buildings";
    public const string ConstructionQueue = "construction_queue";
    public const string TroopCounts = "troop_counts";
    public const string TrainingQueue = "training_queue";
    public const string IdempotencyRecords = "idempotency_records";
}

public sealed record HiveLoopPersistenceReadinessDesign(
    bool ReadOnly,
    bool NonLive,
    bool ProductionMigrationAllowed,
    bool LiveSqlWritesAllowed,
    IReadOnlyList<string> FutureTables,
    HiveLoopPersistenceTransactionPolicy TransactionPolicy)
{
    public HiveLoopPersistenceReadinessValidationResult Validate()
    {
        List<string> errors = [];
        string[] requiredTables =
        [
            HiveLoopPersistenceReadinessTables.PlayerResources,
            HiveLoopPersistenceReadinessTables.HiveBuildings,
            HiveLoopPersistenceReadinessTables.ConstructionQueue,
            HiveLoopPersistenceReadinessTables.TroopCounts,
            HiveLoopPersistenceReadinessTables.TrainingQueue,
            HiveLoopPersistenceReadinessTables.IdempotencyRecords
        ];

        if (!ReadOnly)
        {
            errors.Add("Hive loop persistence readiness design must remain read-only.");
        }

        if (!NonLive)
        {
            errors.Add("Hive loop persistence readiness design must remain non-live.");
        }

        if (ProductionMigrationAllowed || LiveSqlWritesAllowed)
        {
            errors.Add("Hive loop persistence readiness design cannot allow production migrations or live SQL writes.");
        }

        foreach (string table in requiredTables)
        {
            if (!FutureTables.Contains(table, StringComparer.Ordinal))
            {
                errors.Add($"Future table '{table}' is missing from the readiness design.");
            }
        }

        if (!TransactionPolicy.RequiresAtomicResourceDebitAndQueueInsert)
        {
            errors.Add("Future resource debit and queue insert must be atomic.");
        }

        if (!TransactionPolicy.RequiresExpectedRevision)
        {
            errors.Add("Future commands must require expected revisions.");
        }

        if (!TransactionPolicy.RequiresIdempotencyPayloadHash)
        {
            errors.Add("Future commands must store an idempotency payload hash.");
        }

        if (!TransactionPolicy.RequiresWorldAndGameServerScope)
        {
            errors.Add("Future persistence rows must remain scoped by WorldId and GameServerId.");
        }

        return new HiveLoopPersistenceReadinessValidationResult(errors.Count == 0, errors);
    }
}

public sealed record HiveLoopPersistenceTransactionPolicy(
    bool RequiresAtomicResourceDebitAndQueueInsert,
    bool RequiresExpectedRevision,
    bool RequiresIdempotencyPayloadHash,
    bool RequiresWorldAndGameServerScope,
    bool RejectsCrossWorldReplay,
    bool RejectsDifferentPayloadForSameIdempotencyKey);

public sealed record HiveLoopPersistenceReadinessValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public static class HiveLoopPersistenceReadinessCatalog
{
    public static HiveLoopPersistenceReadinessDesign CreateReadinessDesign()
    {
        return new HiveLoopPersistenceReadinessDesign(
            ReadOnly: true,
            NonLive: true,
            ProductionMigrationAllowed: false,
            LiveSqlWritesAllowed: false,
            FutureTables:
            [
                HiveLoopPersistenceReadinessTables.PlayerResources,
                HiveLoopPersistenceReadinessTables.HiveBuildings,
                HiveLoopPersistenceReadinessTables.ConstructionQueue,
                HiveLoopPersistenceReadinessTables.TroopCounts,
                HiveLoopPersistenceReadinessTables.TrainingQueue,
                HiveLoopPersistenceReadinessTables.IdempotencyRecords
            ],
            new HiveLoopPersistenceTransactionPolicy(
                RequiresAtomicResourceDebitAndQueueInsert: true,
                RequiresExpectedRevision: true,
                RequiresIdempotencyPayloadHash: true,
                RequiresWorldAndGameServerScope: true,
                RejectsCrossWorldReplay: true,
                RejectsDifferentPayloadForSameIdempotencyKey: true));
    }
}

public sealed record HivePlayerResourceReadinessRecord(
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string ResourceKey,
    long Amount,
    long Capacity,
    long Revision,
    string CatalogVersion,
    bool ReadOnly,
    bool NonLive);

public sealed record HiveBuildingReadinessRecord(
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    BuildingId BuildingId,
    string BuildingKey,
    int Level,
    long Revision,
    string CatalogVersion,
    bool ReadOnly,
    bool NonLive);

public sealed record HiveConstructionQueueReadinessRecord(
    Guid QueueItemId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    BuildingId BuildingId,
    string BuildingKey,
    int FromLevel,
    int ToLevel,
    DateTimeOffset EnqueuedAtUtc,
    DateTimeOffset CompleteAtUtc,
    long ExpectedResourceRevision,
    long ExpectedBuildingRevision,
    HiveLoopQueueItemReadinessStatus Status,
    string IdempotencyKeyHash,
    string CatalogVersion,
    bool ReadOnly,
    bool NonLive);

public sealed record HiveTroopCountReadinessRecord(
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string TroopKey,
    long Quantity,
    long ArmyRevision,
    string CatalogVersion,
    bool ReadOnly,
    bool NonLive);

public sealed record HiveTrainingQueueReadinessRecord(
    Guid QueueItemId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string TroopKey,
    int Quantity,
    DateTimeOffset EnqueuedAtUtc,
    DateTimeOffset CompleteAtUtc,
    long ExpectedResourceRevision,
    long ExpectedArmyRevision,
    HiveLoopQueueItemReadinessStatus Status,
    string IdempotencyKeyHash,
    string CatalogVersion,
    bool ReadOnly,
    bool NonLive);

public sealed record HiveIdempotencyReadinessRecord(
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string IdempotencyKeyHash,
    string RequestPayloadHash,
    string CommandKind,
    string ResultPayloadHash,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool ReadOnly,
    bool NonLive);

public enum HiveLoopQueueItemReadinessStatus
{
    Pending = 0,
    Ready = 1,
    Cancelled = 2
}
