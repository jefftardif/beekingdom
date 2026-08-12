using BeeKingdom.Shared.Persistence;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Tests.Fakes;

public sealed class HiveLoopInMemoryReadinessRepositoryFake : IHiveLoopReadinessRepository
{
    private readonly List<HivePlayerResourceReadinessRecord> resources = [];
    private readonly List<HiveBuildingReadinessRecord> buildings = [];
    private readonly List<HiveConstructionQueueReadinessRecord> constructionQueue = [];
    private readonly List<HiveTroopCountReadinessRecord> troopCounts = [];
    private readonly List<HiveTrainingQueueReadinessRecord> trainingQueue = [];
    private readonly List<HiveIdempotencyReadinessRecord> idempotencyRecords = [];

    public HiveLoopRepositoryReadinessDescriptor Descriptor { get; } = HiveLoopRepositoryReadinessContracts.CreateDescriptor();

    public int FakeWriteCount { get; private set; }

    public void SeedResource(HivePlayerResourceReadinessRecord record) => resources.Add(EnsureReadiness(record));

    public void SeedBuilding(HiveBuildingReadinessRecord record) => buildings.Add(EnsureReadiness(record));

    public void SeedTroopCount(HiveTroopCountReadinessRecord record) => troopCounts.Add(EnsureReadiness(record));

    public Task<IReadOnlyList<HivePlayerResourceReadinessRecord>> ReadPlayerResourcesAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<HivePlayerResourceReadinessRecord>>(
            resources.Where(record => IsSameScope(record.PlayerId, record.WorldId, record.GameServerId, playerId, worldId, gameServerId)).ToArray());
    }

    public Task<IReadOnlyList<HiveBuildingReadinessRecord>> ReadHiveBuildingsAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<HiveBuildingReadinessRecord>>(
            buildings.Where(record => IsSameScope(record.PlayerId, record.WorldId, record.GameServerId, playerId, worldId, gameServerId)).ToArray());
    }

    public Task<IReadOnlyList<HiveConstructionQueueReadinessRecord>> ReadConstructionQueueAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        HiveLoopQueueItemReadinessStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<HiveConstructionQueueReadinessRecord>>(
            constructionQueue
                .Where(record => IsSameScope(record.PlayerId, record.WorldId, record.GameServerId, playerId, worldId, gameServerId))
                .Where(record => status is null || record.Status == status)
                .ToArray());
    }

    public Task<IReadOnlyList<HiveTroopCountReadinessRecord>> ReadTroopCountsAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<HiveTroopCountReadinessRecord>>(
            troopCounts.Where(record => IsSameScope(record.PlayerId, record.WorldId, record.GameServerId, playerId, worldId, gameServerId)).ToArray());
    }

    public Task<IReadOnlyList<HiveTrainingQueueReadinessRecord>> ReadTrainingQueueAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        HiveLoopQueueItemReadinessStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<HiveTrainingQueueReadinessRecord>>(
            trainingQueue
                .Where(record => IsSameScope(record.PlayerId, record.WorldId, record.GameServerId, playerId, worldId, gameServerId))
                .Where(record => status is null || record.Status == status)
                .ToArray());
    }

    public Task<HiveIdempotencyReadinessRecord?> ReadIdempotencyRecordAsync(
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId,
        string idempotencyKeyHash,
        CancellationToken cancellationToken = default)
    {
        HiveIdempotencyReadinessRecord? record = idempotencyRecords.SingleOrDefault(item =>
            IsSameScope(item.PlayerId, item.WorldId, item.GameServerId, playerId, worldId, gameServerId) &&
            string.Equals(item.IdempotencyKeyHash, idempotencyKeyHash, StringComparison.Ordinal));

        return Task.FromResult(record);
    }

    public Task<HiveLoopRepositoryReservationResult> TryReserveUpgradeAsync(
        HiveLoopUpgradeReservationIntent intent,
        CancellationToken cancellationToken = default)
    {
        string[] readinessErrors = ValidateReadiness(intent.NonLive, intent.ReadinessOnly);
        if (readinessErrors.Length > 0)
        {
            return Task.FromResult(RejectedReservation(readinessErrors));
        }

        IdempotencyCheck idempotency = CheckIdempotency(intent.PlayerId, intent.WorldId, intent.GameServerId, intent.IdempotencyKeyHash, intent.RequestPayloadHash);
        if (idempotency == IdempotencyCheck.DifferentPayload)
        {
            return Task.FromResult(RejectedReservation(["IdempotencyDifferentPayload"]));
        }

        if (idempotency == IdempotencyCheck.SamePayload)
        {
            return Task.FromResult(new HiveLoopRepositoryReservationResult(
                Accepted: false,
                NonLive: true,
                ReadinessOnly: true,
                OfficialProgressionApplied: false,
                LiveSqlWriteApplied: false,
                QueueItemId: null,
                ResultCode: "ReadinessIdempotencyReplay",
                ValidationErrors: []));
        }

        Guid queueItemId = Guid.NewGuid();
        constructionQueue.Add(new HiveConstructionQueueReadinessRecord(
            queueItemId,
            intent.PlayerId,
            intent.WorldId,
            intent.GameServerId,
            intent.BuildingId,
            intent.BuildingKey,
            intent.FromLevel,
            intent.ToLevel,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(1),
            intent.ExpectedResourceRevision,
            intent.ExpectedBuildingRevision,
            HiveLoopQueueItemReadinessStatus.Pending,
            intent.IdempotencyKeyHash,
            intent.CatalogVersion,
            ReadOnly: true,
            NonLive: true));
        FakeWriteCount++;

        return Task.FromResult(new HiveLoopRepositoryReservationResult(
            Accepted: true,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveSqlWriteApplied: false,
            queueItemId,
            "ReadinessReservedUpgrade",
            ValidationErrors: []));
    }

    public Task<HiveLoopRepositoryReservationResult> TryReserveTrainingAsync(
        HiveLoopTrainingReservationIntent intent,
        CancellationToken cancellationToken = default)
    {
        string[] readinessErrors = ValidateReadiness(intent.NonLive, intent.ReadinessOnly);
        if (readinessErrors.Length > 0)
        {
            return Task.FromResult(RejectedReservation(readinessErrors));
        }

        IdempotencyCheck idempotency = CheckIdempotency(intent.PlayerId, intent.WorldId, intent.GameServerId, intent.IdempotencyKeyHash, intent.RequestPayloadHash);
        if (idempotency == IdempotencyCheck.DifferentPayload)
        {
            return Task.FromResult(RejectedReservation(["IdempotencyDifferentPayload"]));
        }

        if (idempotency == IdempotencyCheck.SamePayload)
        {
            return Task.FromResult(new HiveLoopRepositoryReservationResult(
                Accepted: false,
                NonLive: true,
                ReadinessOnly: true,
                OfficialProgressionApplied: false,
                LiveSqlWriteApplied: false,
                QueueItemId: null,
                ResultCode: "ReadinessIdempotencyReplay",
                ValidationErrors: []));
        }

        Guid queueItemId = Guid.NewGuid();
        trainingQueue.Add(new HiveTrainingQueueReadinessRecord(
            queueItemId,
            intent.PlayerId,
            intent.WorldId,
            intent.GameServerId,
            intent.TroopKey,
            intent.Quantity,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(1),
            intent.ExpectedResourceRevision,
            intent.ExpectedArmyRevision,
            HiveLoopQueueItemReadinessStatus.Pending,
            intent.IdempotencyKeyHash,
            intent.CatalogVersion,
            ReadOnly: true,
            NonLive: true));
        FakeWriteCount++;

        return Task.FromResult(new HiveLoopRepositoryReservationResult(
            Accepted: true,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveSqlWriteApplied: false,
            queueItemId,
            "ReadinessReservedTraining",
            ValidationErrors: []));
    }

    public Task<HiveLoopDueQueueCompletionResult> CompleteDueQueuesAsync(
        HiveLoopDueQueueCompletionIntent intent,
        CancellationToken cancellationToken = default)
    {
        List<Guid> completedIds = [];

        if (!intent.NonLive || !intent.ReadinessOnly || intent.MaxItems <= 0)
        {
            return Task.FromResult(new HiveLoopDueQueueCompletionResult(
                NonLive: true,
                ReadinessOnly: true,
                OfficialProgressionApplied: false,
                LiveSqlWriteApplied: false,
                ConstructionItemsConsidered: 0,
                TrainingItemsConsidered: 0,
                QueueItemIds: completedIds));
        }

        int remaining = intent.MaxItems;
        int completedConstruction = 0;
        int completedTraining = 0;

        for (int index = 0; index < constructionQueue.Count && remaining > 0; index++)
        {
            HiveConstructionQueueReadinessRecord item = constructionQueue[index];
            if (item.WorldId == intent.WorldId &&
                item.GameServerId == intent.GameServerId &&
                item.Status == HiveLoopQueueItemReadinessStatus.Pending &&
                item.CompleteAtUtc <= intent.CompleteBeforeOrAtUtc)
            {
                constructionQueue[index] = item with { Status = HiveLoopQueueItemReadinessStatus.Ready };
                completedIds.Add(item.QueueItemId);
                completedConstruction++;
                remaining--;
            }
        }

        for (int index = 0; index < trainingQueue.Count && remaining > 0; index++)
        {
            HiveTrainingQueueReadinessRecord item = trainingQueue[index];
            if (item.WorldId == intent.WorldId &&
                item.GameServerId == intent.GameServerId &&
                item.Status == HiveLoopQueueItemReadinessStatus.Pending &&
                item.CompleteAtUtc <= intent.CompleteBeforeOrAtUtc)
            {
                trainingQueue[index] = item with { Status = HiveLoopQueueItemReadinessStatus.Ready };
                completedIds.Add(item.QueueItemId);
                completedTraining++;
                remaining--;
            }
        }

        FakeWriteCount += completedIds.Count;

        return Task.FromResult(new HiveLoopDueQueueCompletionResult(
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveSqlWriteApplied: false,
            completedConstruction,
            completedTraining,
            completedIds));
    }

    public Task<HiveLoopIdempotencyRecordResult> RecordIdempotencyResultAsync(
        HiveLoopIdempotencyRecordIntent intent,
        CancellationToken cancellationToken = default)
    {
        string[] readinessErrors = ValidateReadiness(intent.NonLive, intent.ReadinessOnly);
        if (readinessErrors.Length > 0)
        {
            return Task.FromResult(RejectedIdempotency(readinessErrors));
        }

        IdempotencyCheck idempotency = CheckIdempotency(intent.PlayerId, intent.WorldId, intent.GameServerId, intent.IdempotencyKeyHash, intent.RequestPayloadHash);
        if (idempotency == IdempotencyCheck.DifferentPayload)
        {
            return Task.FromResult(RejectedIdempotency(["IdempotencyDifferentPayload"]));
        }

        if (idempotency == IdempotencyCheck.SamePayload)
        {
            return Task.FromResult(new HiveLoopIdempotencyRecordResult(
                Recorded: false,
                NonLive: true,
                ReadinessOnly: true,
                OfficialProgressionApplied: false,
                LiveSqlWriteApplied: false,
                ResultCode: "ReadinessIdempotencyReplay",
                ValidationErrors: []));
        }

        idempotencyRecords.Add(new HiveIdempotencyReadinessRecord(
            intent.PlayerId,
            intent.WorldId,
            intent.GameServerId,
            intent.IdempotencyKeyHash,
            intent.RequestPayloadHash,
            intent.CommandKind,
            intent.ResultPayloadHash,
            intent.CreatedAtUtc,
            intent.ExpiresAtUtc,
            ReadOnly: true,
            NonLive: true));
        FakeWriteCount++;

        return Task.FromResult(new HiveLoopIdempotencyRecordResult(
            Recorded: true,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveSqlWriteApplied: false,
            ResultCode: "ReadinessRecorded",
            ValidationErrors: []));
    }

    private IdempotencyCheck CheckIdempotency(PlayerId playerId, WorldId worldId, GameServerId gameServerId, string idempotencyKeyHash, string requestPayloadHash)
    {
        HiveIdempotencyReadinessRecord? existing = idempotencyRecords.SingleOrDefault(item =>
            IsSameScope(item.PlayerId, item.WorldId, item.GameServerId, playerId, worldId, gameServerId) &&
            string.Equals(item.IdempotencyKeyHash, idempotencyKeyHash, StringComparison.Ordinal));

        if (existing is null)
        {
            return IdempotencyCheck.None;
        }

        return string.Equals(existing.RequestPayloadHash, requestPayloadHash, StringComparison.Ordinal)
            ? IdempotencyCheck.SamePayload
            : IdempotencyCheck.DifferentPayload;
    }

    private static HiveLoopRepositoryReservationResult RejectedReservation(IReadOnlyList<string> errors)
    {
        return new HiveLoopRepositoryReservationResult(
            Accepted: false,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveSqlWriteApplied: false,
            QueueItemId: null,
            ResultCode: "ReadinessRejected",
            ValidationErrors: errors);
    }

    private static HiveLoopIdempotencyRecordResult RejectedIdempotency(IReadOnlyList<string> errors)
    {
        return new HiveLoopIdempotencyRecordResult(
            Recorded: false,
            NonLive: true,
            ReadinessOnly: true,
            OfficialProgressionApplied: false,
            LiveSqlWriteApplied: false,
            ResultCode: "ReadinessRejected",
            ValidationErrors: errors);
    }

    private static string[] ValidateReadiness(bool nonLive, bool readinessOnly)
    {
        List<string> errors = [];

        if (!nonLive)
        {
            errors.Add("NonLiveRequired");
        }

        if (!readinessOnly)
        {
            errors.Add("ReadinessOnlyRequired");
        }

        return errors.ToArray();
    }

    private static HivePlayerResourceReadinessRecord EnsureReadiness(HivePlayerResourceReadinessRecord record)
    {
        return record with { ReadOnly = true, NonLive = true };
    }

    private static HiveBuildingReadinessRecord EnsureReadiness(HiveBuildingReadinessRecord record)
    {
        return record with { ReadOnly = true, NonLive = true };
    }

    private static HiveTroopCountReadinessRecord EnsureReadiness(HiveTroopCountReadinessRecord record)
    {
        return record with { ReadOnly = true, NonLive = true };
    }

    private static bool IsSameScope(
        PlayerId currentPlayerId,
        WorldId currentWorldId,
        GameServerId currentGameServerId,
        PlayerId playerId,
        WorldId worldId,
        GameServerId gameServerId)
    {
        return currentPlayerId == playerId && currentWorldId == worldId && currentGameServerId == gameServerId;
    }

    private enum IdempotencyCheck
    {
        None,
        SamePayload,
        DifferentPayload
    }
}
