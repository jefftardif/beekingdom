using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public static class SpeedUpCategories
{
    public const string Construction = "construction";
    public const string Research = "research";
    public const string Training = "training";
    public const string Healing = "healing";
    public const string Manufacturing = "manufacturing";
    public const string Universal = "universal";
}

public sealed record SpeedUpDefinition(string ItemId, string Category, long DurationSeconds);

public sealed class SpeedUpOptions
{
    public const string SectionName = "SpeedUps";
    public bool Enabled { get; set; }
    public string ContractVersion { get; set; } = "server-speedup-v1";
    public List<SpeedUpDefinition> Catalog { get; set; } = DefaultCatalog();

    public void Validate()
    {
        if (!Enabled && Catalog.Count == 0) return;
        if (string.IsNullOrWhiteSpace(ContractVersion) || Catalog.Count == 0) throw new InvalidDataException("Invalid SpeedUp configuration");
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (SpeedUpDefinition item in Catalog)
        {
            if (!AllowedCategory(item.Category) || string.IsNullOrWhiteSpace(item.ItemId) || item.DurationSeconds <= 0 || !ids.Add(item.ItemId))
                throw new InvalidDataException("Invalid SpeedUp catalog");
        }
    }

    public SpeedUpDefinition? Find(string itemId) => Catalog.FirstOrDefault(item => string.Equals(item.ItemId, itemId, StringComparison.Ordinal));

    private static List<SpeedUpDefinition> DefaultCatalog()
    {
        long[] durations = [60, 300, 600, 900, 1800, 3600, 10800, 28800, 43200, 86400, 259200, 604800, 2592000];
        string[] categories = [SpeedUpCategories.Construction, SpeedUpCategories.Research, SpeedUpCategories.Training, SpeedUpCategories.Healing, SpeedUpCategories.Manufacturing, SpeedUpCategories.Universal];
        List<SpeedUpDefinition> result = [];
        foreach (string category in categories)
            foreach (long duration in durations)
                result.Add(new SpeedUpDefinition(category + "_" + duration + "s", category, duration));
        return result;
    }

    internal static bool AllowedCategory(string category) => category is SpeedUpCategories.Construction or SpeedUpCategories.Research or SpeedUpCategories.Training or SpeedUpCategories.Healing or SpeedUpCategories.Manufacturing or SpeedUpCategories.Universal;
}

public sealed record ApplySpeedUpRequest(string ItemId, string Category, string TargetId, long DurationSeconds, long ExpectedRevision, string IdempotencyKey);
public sealed record SpeedUpTimerSnapshot(string Category, string TargetId, Guid? OperationId, DateTimeOffset CompletesAtUtc, string Status);
public sealed record SpeedUpInventorySnapshot(IReadOnlyDictionary<string, int> Items);
public sealed record SpeedUpReadSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, long Revision, DateTimeOffset ServerTimeUtc, SpeedUpInventorySnapshot Inventory, IReadOnlyList<SpeedUpTimerSnapshot> Timers, IReadOnlyList<string> Rewards, IReadOnlyList<string> Events);
public sealed record SpeedUpReceipt(Guid PlayerId, Guid HiveId, string IdempotencyKey, string ItemId, string Category, string TargetId, long ConsumedQuantity, long Revision, DateTimeOffset AcceptedAtUtc, string Code);
public sealed record SpeedUpApplyResponse(SpeedUpReceipt Receipt, SpeedUpReadSnapshot Snapshot);
public sealed record SpeedUpCommandResult(bool Succeeded, string Code, SpeedUpReadSnapshot Snapshot, SpeedUpApplyResponse? Response = null);

public sealed class SpeedUpInventoryService(IHiveStateRepository repository, IServerClock clock, SpeedUpOptions options)
{
    private readonly SpeedUpOptions configuration = options ?? throw new ArgumentNullException(nameof(options));
    private readonly IReadOnlyDictionary<string, ISpeedUpTargetHandler> handlers = CreateHandlers();

    public async Task<SpeedUpReadSnapshot?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        if (await repository.ReadAsync(playerId, hiveId, cancellationToken) is null) return null;
        SpeedUpReadSnapshot? snapshot = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            state = RewardLedgerService.Settle(state, clock.UtcNow);
            snapshot = Snapshot(state, clock.UtcNow);
            return state;
        }, cancellationToken);
        return snapshot;
    }

    public async Task<SpeedUpCommandResult> ApplyAsync(Guid playerId, Guid hiveId, ApplySpeedUpRequest request, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        if (request is null || request.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(request.ItemId) || string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.TargetId) || !ValidIdempotencyKey(request.IdempotencyKey))
            return Failure(playerId, hiveId, "invalid_request");

        SpeedUpDefinition? definition = configuration.Find(request.ItemId);
        if (definition is null || !string.Equals(definition.Category, request.Category, StringComparison.Ordinal) && !string.Equals(definition.Category, SpeedUpCategories.Universal, StringComparison.Ordinal) || definition.DurationSeconds != request.DurationSeconds)
            return Failure(playerId, hiveId, "invalid_speedup");

        string payloadHash = Hash(request);
        SpeedUpCommandResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            DateTimeOffset now = clock.UtcNow;
            if (state.Receipts.TryGetValue(request.IdempotencyKey, out IdempotencyReceipt? stored))
            {
                result = stored.PayloadHash == payloadHash
                    ? new SpeedUpCommandResult(stored.Succeeded, stored.Code, Snapshot(state, now))
                    : new SpeedUpCommandResult(false, "idempotency_conflict", Snapshot(state, now));
                return state;
            }
            if (state.Revision != request.ExpectedRevision) return RecordFailure(state, request, payloadHash, "revision_conflict", now, out result);

            Dictionary<string, int> inventory = new(state.SpeedUps ?? new Dictionary<string, int>(StringComparer.Ordinal));
            if (!inventory.TryGetValue(request.ItemId, out int quantity) || quantity <= 0)
                return RecordFailure(state, request, payloadHash, "inventory_insufficient", now, out result);

            string handlerKey = string.Equals(definition.Category, SpeedUpCategories.Universal, StringComparison.Ordinal) ? SpeedUpCategories.Universal : request.Category;
            if (!handlers.TryGetValue(handlerKey, out ISpeedUpTargetHandler? handler))
                return RecordFailure(state, request, payloadHash, "category_unsupported", now, out result);
            if (!handler.TryApply(state, request.TargetId, now, TimeSpan.FromSeconds(definition.DurationSeconds), out TargetApplyResult target))
                return RecordFailure(state, request, payloadHash, target.Code, now, out result);

            inventory[request.ItemId] = quantity - 1;
            PlayerHiveState updated = target.State with { Revision = state.Revision + 1, SpeedUps = inventory };
            string code = target.Completed ? "speedup_applied_completed" : "speedup_applied";
            return RecordSuccess(updated, request, payloadHash, code, now, target.OperationId, out result);
        }, cancellationToken);
        return result ?? Failure(playerId, hiveId, "mutation_failed");
    }

    private PlayerHiveState RecordFailure(PlayerHiveState state, ApplySpeedUpRequest request, string hash, string code, DateTimeOffset now, out SpeedUpCommandResult? result)
    {
        result = new SpeedUpCommandResult(false, code, Snapshot(state, now));
        Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts)
        {
            [request.IdempotencyKey] = new IdempotencyReceipt(hash, false, code, null, now, state.Revision, state.Revision, AcceptedAtUtc: now)
        };
        return state with { Receipts = receipts };
    }

    private PlayerHiveState RecordSuccess(PlayerHiveState state, ApplySpeedUpRequest request, string hash, string code, DateTimeOffset now, Guid? operationId, out SpeedUpCommandResult? result)
    {
        Dictionary<string, IdempotencyReceipt> receipts = new(state.Receipts)
        {
            [request.IdempotencyKey] = new IdempotencyReceipt(hash, true, code, operationId, now, state.Revision - 1, state.Revision, AcceptedAtUtc: now)
        };
        PlayerHiveState updated = state with { Receipts = receipts };
        SpeedUpReceipt receipt = new(updated.PlayerId, updated.HiveId, request.IdempotencyKey, request.ItemId, request.Category, request.TargetId, 1, updated.Revision, now, code);
        SpeedUpReadSnapshot snapshot = Snapshot(updated, now);
        result = new SpeedUpCommandResult(true, code, snapshot, new SpeedUpApplyResponse(receipt, snapshot));
        return updated;
    }

    private SpeedUpCommandResult Failure(Guid playerId, Guid hiveId, string code) => new(false, code, new SpeedUpReadSnapshot(playerId, hiveId, configuration.ContractVersion, 0, DateTimeOffset.UnixEpoch, new SpeedUpInventorySnapshot(new Dictionary<string, int>()), Array.Empty<SpeedUpTimerSnapshot>(), Array.Empty<string>(), Array.Empty<string>()));
    private void EnsureEnabled() { configuration.Validate(); if (!configuration.Enabled) throw new InvalidOperationException("SpeedUps are disabled"); }
    private static bool ValidIdempotencyKey(string value) => !string.IsNullOrWhiteSpace(value) && value.Trim() == value && value.Length <= 256;
    private static string Hash(ApplySpeedUpRequest request) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.ItemId + "|" + request.Category + "|" + request.TargetId + "|" + request.DurationSeconds + "|" + request.ExpectedRevision))).ToLowerInvariant();

    private SpeedUpReadSnapshot Snapshot(PlayerHiveState state, DateTimeOffset now)
    {
        List<SpeedUpTimerSnapshot> timers = [];
        foreach (HiveOperation operation in state.Operations.Where(item => item.Status != HiveOperationStatus.Collected))
            timers.Add(new SpeedUpTimerSnapshot(operation.Kind.ToString().ToLowerInvariant(), operation.BuildingKey, operation.OperationId, operation.CompletesAtUtc, operation.Status.ToString().ToLowerInvariant()));
        if (state.Research?.ActiveOperation is ResearchOperation research)
            timers.Add(new SpeedUpTimerSnapshot(SpeedUpCategories.Research, research.ResearchId, research.OperationId, research.EndsAtUtc, "running"));
        if (state.DoctrineRoster?.ActiveOperation is DoctrineTrainingOperation training)
            timers.Add(new SpeedUpTimerSnapshot(SpeedUpCategories.Training, training.Family, training.OperationId, training.EndsAtUtc, "running"));
        if (state.BroodVitality?.ActiveOperation is BroodVitalityOperation healing)
            timers.Add(new SpeedUpTimerSnapshot(SpeedUpCategories.Healing, healing.Type, healing.OperationId, healing.EndsAtUtc, "running"));
        List<string> rewards = state.RewardLedger?.Entries
            .Where(entry => !entry.Value.Claimed)
            .OrderBy(entry => entry.Value.GrantedAtUtc)
            .Select(entry => entry.Key)
            .ToList() ?? [];
        List<string> events = state.RewardLedger?.Events
            .Select(entry => entry.EventKey + ":" + entry.TargetKey)
            .ToList() ?? [];
        return new SpeedUpReadSnapshot(state.PlayerId, state.HiveId, configuration.ContractVersion, state.Revision, now, new SpeedUpInventorySnapshot(new Dictionary<string, int>(state.SpeedUps ?? new Dictionary<string, int>())), timers, rewards, events);
    }

    private static Dictionary<string, ISpeedUpTargetHandler> CreateHandlers() => new(StringComparer.Ordinal)
    {
        [SpeedUpCategories.Construction] = new OperationSpeedUpHandler(HiveOperationKind.BuildingUpgrade),
        [SpeedUpCategories.Manufacturing] = new OperationSpeedUpHandler(HiveOperationKind.Production),
        [SpeedUpCategories.Research] = new ResearchSpeedUpHandler(),
        [SpeedUpCategories.Training] = new TrainingSpeedUpHandler(),
        [SpeedUpCategories.Healing] = new HealingSpeedUpHandler(),
        [SpeedUpCategories.Universal] = new CompositeSpeedUpHandler(new OperationSpeedUpHandler(HiveOperationKind.BuildingUpgrade), new ResearchSpeedUpHandler(), new TrainingSpeedUpHandler(), new HealingSpeedUpHandler(), new OperationSpeedUpHandler(HiveOperationKind.Production))
    };

    private interface ISpeedUpTargetHandler
    {
        bool TryApply(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out TargetApplyResult result);
    }

    private sealed record TargetApplyResult(bool Success, string Code, PlayerHiveState State, Guid? OperationId, bool Completed);

    private sealed class OperationSpeedUpHandler(HiveOperationKind kind) : ISpeedUpTargetHandler
    {
        public bool TryApply(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out TargetApplyResult result)
        {
            int index = state.Operations.FindIndex(operation => operation.Kind == kind && operation.BuildingKey == targetId && operation.Status != HiveOperationStatus.Collected);
            if (index < 0) { result = new(false, "timer_not_found", state, null, false); return false; }
            HiveOperation operation = state.Operations[index];
            DateTimeOffset end = operation.CompletesAtUtc - duration;
            bool completed = end <= now;
            List<HiveOperation> operations = [.. state.Operations];
            operations[index] = operation with { CompletesAtUtc = end <= now ? now : end, Status = completed ? HiveOperationStatus.AwaitingCollection : HiveOperationStatus.Running };
            result = new(true, "speedup_applied", state with { Operations = operations }, operation.OperationId, completed);
            return true;
        }
    }

    private sealed class ResearchSpeedUpHandler : ISpeedUpTargetHandler
    {
        public bool TryApply(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out TargetApplyResult result)
        {
            ResearchOperation? operation = state.Research?.ActiveOperation;
            if (operation is null || operation.ResearchId != targetId) { result = new(false, "timer_not_found", state, null, false); return false; }
            DateTimeOffset end = operation.EndsAtUtc - duration;
            bool completed = end <= now;
            HiveResearchState research = state.Research! with { ActiveOperation = operation with { EndsAtUtc = end <= now ? now : end } };
            result = new(true, "speedup_applied", state with { Research = research }, operation.OperationId, completed);
            return true;
        }
    }

    private sealed class TrainingSpeedUpHandler : ISpeedUpTargetHandler
    {
        public bool TryApply(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out TargetApplyResult result)
        {
            DoctrineTrainingOperation? operation = state.DoctrineRoster?.ActiveOperation;
            if (operation is null || operation.Family != targetId) { result = new(false, "timer_not_found", state, null, false); return false; }
            DateTimeOffset end = operation.EndsAtUtc - duration;
            bool completed = end <= now;
            DoctrineRosterState roster = state.DoctrineRoster! with { ActiveOperation = operation with { EndsAtUtc = end <= now ? now : end } };
            result = new(true, "speedup_applied", state with { DoctrineRoster = roster }, operation.OperationId, completed);
            return true;
        }
    }

    private sealed class HealingSpeedUpHandler : ISpeedUpTargetHandler
    {
        public bool TryApply(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out TargetApplyResult result)
        {
            BroodVitalityOperation? operation = state.BroodVitality?.ActiveOperation;
            if (operation is null || operation.Type != targetId) { result = new(false, "timer_not_found", state, null, false); return false; }
            DateTimeOffset end = operation.EndsAtUtc - duration;
            bool completed = end <= now;
            BroodVitalityState vitality = state.BroodVitality! with { ActiveOperation = operation with { EndsAtUtc = end <= now ? now : end } };
            result = new(true, "speedup_applied", state with { BroodVitality = vitality }, operation.OperationId, completed);
            return true;
        }
    }

    private sealed class CompositeSpeedUpHandler(params ISpeedUpTargetHandler[] handlers) : ISpeedUpTargetHandler
    {
        public bool TryApply(PlayerHiveState state, string targetId, DateTimeOffset now, TimeSpan duration, out TargetApplyResult result)
        {
            foreach (ISpeedUpTargetHandler handler in handlers)
                if (handler.TryApply(state, targetId, now, duration, out result)) return true;
            result = new(false, "timer_not_found", state, null, false);
            return false;
        }
    }
}
