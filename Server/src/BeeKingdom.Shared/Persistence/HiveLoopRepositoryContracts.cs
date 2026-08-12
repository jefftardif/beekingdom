using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Shared.Persistence;

public interface IHiveLoopReadinessRepository
{
    HiveLoopRepositoryReadinessDescriptor Descriptor { get; }

    Task<IReadOnlyList<HivePlayerResourceReadinessRecord>> ReadPlayerResourcesAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HiveBuildingReadinessRecord>> ReadHiveBuildingsAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HiveConstructionQueueReadinessRecord>> ReadConstructionQueueAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        HiveLoopQueueItemReadinessStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HiveTroopCountReadinessRecord>> ReadTroopCountsAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HiveTrainingQueueReadinessRecord>> ReadTrainingQueueAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        HiveLoopQueueItemReadinessStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<HiveIdempotencyReadinessRecord?> ReadIdempotencyRecordAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        string idempotencyKeyHash,
        CancellationToken cancellationToken = default);

    Task<HiveLoopRepositoryReservationResult> TryReserveUpgradeAsync(
        HiveLoopUpgradeReservationIntent intent,
        CancellationToken cancellationToken = default);

    Task<HiveLoopRepositoryReservationResult> TryReserveTrainingAsync(
        HiveLoopTrainingReservationIntent intent,
        CancellationToken cancellationToken = default);

    Task<HiveLoopDueQueueCompletionResult> CompleteDueQueuesAsync(
        HiveLoopDueQueueCompletionIntent intent,
        CancellationToken cancellationToken = default);

    Task<HiveLoopIdempotencyRecordResult> RecordIdempotencyResultAsync(
        HiveLoopIdempotencyRecordIntent intent,
        CancellationToken cancellationToken = default);
}

public sealed record HiveLoopRepositoryReadinessDescriptor(
    bool NonLive,
    bool ReadinessOnly,
    bool LocalOnly,
    bool ProductionSqlAllowed,
    bool EndpointExposed,
    bool OfficialProgressionAllowed,
    IReadOnlyList<string> SupportedFutureTables,
    IReadOnlyList<string> IntendedAtomicOperations)
{
    public HiveLoopRepositoryReadinessValidationResult Validate()
    {
        List<string> errors = [];

        if (!NonLive || !ReadinessOnly || !LocalOnly)
        {
            errors.Add("Hive loop repository contracts must remain non-live, readiness-only and local-only.");
        }

        if (ProductionSqlAllowed || EndpointExposed || OfficialProgressionAllowed)
        {
            errors.Add("Hive loop repository contracts cannot allow production SQL, endpoint exposure or official progression.");
        }

        string[] requiredOperations =
        [
            HiveLoopRepositoryReadinessOperations.TryReserveUpgrade,
            HiveLoopRepositoryReadinessOperations.TryReserveTraining,
            HiveLoopRepositoryReadinessOperations.CompleteDueQueues,
            HiveLoopRepositoryReadinessOperations.RecordIdempotencyResult
        ];

        foreach (string operation in requiredOperations)
        {
            if (!IntendedAtomicOperations.Contains(operation, StringComparer.Ordinal))
            {
                errors.Add($"Future atomic operation '{operation}' is missing.");
            }
        }

        foreach (string table in HiveLoopPersistenceReadinessCatalog.CreateReadinessDesign().FutureTables)
        {
            if (!SupportedFutureTables.Contains(table, StringComparer.Ordinal))
            {
                errors.Add($"Future table '{table}' is not supported by the repository readiness descriptor.");
            }
        }

        return new HiveLoopRepositoryReadinessValidationResult(errors.Count == 0, errors);
    }
}

public static class HiveLoopRepositoryReadinessOperations
{
    public const string TryReserveUpgrade = "TryReserveUpgrade";
    public const string TryReserveTraining = "TryReserveTraining";
    public const string CompleteDueQueues = "CompleteDueQueues";
    public const string RecordIdempotencyResult = "RecordIdempotencyResult";
}

public static class HiveLoopRepositoryReadinessContracts
{
    public static HiveLoopRepositoryReadinessDescriptor CreateDescriptor()
    {
        return new HiveLoopRepositoryReadinessDescriptor(
            NonLive: true,
            ReadinessOnly: true,
            LocalOnly: true,
            ProductionSqlAllowed: false,
            EndpointExposed: false,
            OfficialProgressionAllowed: false,
            HiveLoopPersistenceReadinessCatalog.CreateReadinessDesign().FutureTables,
            [
                HiveLoopRepositoryReadinessOperations.TryReserveUpgrade,
                HiveLoopRepositoryReadinessOperations.TryReserveTraining,
                HiveLoopRepositoryReadinessOperations.CompleteDueQueues,
                HiveLoopRepositoryReadinessOperations.RecordIdempotencyResult
            ]);
    }
}

public sealed record HiveLoopRepositoryReadinessValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public sealed record HiveLoopUpgradeReservationIntent(
    Guid CommandId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    BuildingId BuildingId,
    string BuildingKey,
    int FromLevel,
    int ToLevel,
    long ExpectedResourceRevision,
    long ExpectedBuildingRevision,
    string IdempotencyKeyHash,
    string RequestPayloadHash,
    string CatalogVersion,
    bool NonLive,
    bool ReadinessOnly);

public sealed record HiveLoopTrainingReservationIntent(
    Guid CommandId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string TroopKey,
    int Quantity,
    long ExpectedResourceRevision,
    long ExpectedArmyRevision,
    string IdempotencyKeyHash,
    string RequestPayloadHash,
    string CatalogVersion,
    bool NonLive,
    bool ReadinessOnly);

public sealed record HiveLoopDueQueueCompletionIntent(
    WorldId WorldId,
    GameServerId GameServerId,
    DateTimeOffset CompleteBeforeOrAtUtc,
    int MaxItems,
    bool NonLive,
    bool ReadinessOnly);

public sealed record HiveLoopIdempotencyRecordIntent(
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string IdempotencyKeyHash,
    string RequestPayloadHash,
    string CommandKind,
    string ResultPayloadHash,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool NonLive,
    bool ReadinessOnly);

public sealed record HiveLoopRepositoryReservationResult(
    bool Accepted,
    bool NonLive,
    bool ReadinessOnly,
    bool OfficialProgressionApplied,
    bool LiveSqlWriteApplied,
    Guid? QueueItemId,
    string ResultCode,
    IReadOnlyList<string> ValidationErrors);

public sealed record HiveLoopDueQueueCompletionResult(
    bool NonLive,
    bool ReadinessOnly,
    bool OfficialProgressionApplied,
    bool LiveSqlWriteApplied,
    int ConstructionItemsConsidered,
    int TrainingItemsConsidered,
    IReadOnlyList<Guid> QueueItemIds);

public sealed record HiveLoopIdempotencyRecordResult(
    bool Recorded,
    bool NonLive,
    bool ReadinessOnly,
    bool OfficialProgressionApplied,
    bool LiveSqlWriteApplied,
    string ResultCode,
    IReadOnlyList<string> ValidationErrors);
