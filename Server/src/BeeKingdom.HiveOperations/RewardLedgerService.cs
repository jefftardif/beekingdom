using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed class RewardLedgerOptions
{
    public const string SectionName = "RewardLedger";
    public bool Enabled { get; set; }
    public string ContractVersion { get; set; } = "server-reward-ledger-v1";
    public int MaxEntries { get; set; } = 512;
    public int MaxEvents { get; set; } = 64;
    public int MaxSettledOperationIds { get; set; } = 512;
    public int MaxReceipts { get; set; } = 256;

    public void Validate()
    {
        if (!Enabled) return;
        if (string.IsNullOrWhiteSpace(ContractVersion)) throw new InvalidDataException("Invalid reward ledger configuration");
        if (MaxEntries < 1 || MaxEntries > 4096 || MaxEvents < 1 || MaxEvents > 1024 || MaxSettledOperationIds < 1 || MaxSettledOperationIds > 4096 || MaxReceipts < 1 || MaxReceipts > 4096)
            throw new InvalidDataException("Invalid reward ledger limits");
    }
}

// Pipeline de settlement server-authoritative (voir RewardLedgerState) : octroi idempotent de
// recompenses claimables, recensement des completions de files en evenements, et lecture du
// ledger pour alimenter les collections `Rewards`/`Events` des snapshots (SpeedUp inclus).
// Chaque octroi ecrit la recompense claimable (state.Rewards) ET l'entree de ledger dans la
// meme mutation atomique; la reclamation existante (HiveOperationService.ClaimRewardAsync)
// synchronise l'entree et append l'evenement `reward_claimed`.
public sealed class RewardLedgerService(IHiveStateRepository repository, IServerClock clock, RewardLedgerOptions options)
{
    public const string EventQueueCompleted = "queue_completed";
    public const string EventRewardGranted = "reward_granted";
    public const string EventRewardClaimed = "reward_claimed";

    private readonly RewardLedgerOptions o = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<RewardLedgerReadSnapshot?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        Ensure();
        if (await repository.ReadAsync(playerId, hiveId, ct) is null) return null;
        RewardLedgerReadSnapshot? snapshot = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            state = Settle(state, clock.UtcNow, o.MaxEvents, o.MaxSettledOperationIds);
            snapshot = Snapshot(state, clock.UtcNow);
            return state;
        }, ct);
        return snapshot;
    }

    public async Task<RewardLedgerCommandResult> GrantAsync(GrantRewardCommand command, CancellationToken ct = default)
    {
        Ensure();
        if (command is null) return Fail(Guid.Empty, Guid.Empty, "invalid_request");
        if (string.IsNullOrWhiteSpace(command.RewardKey) || command.RewardKey.Trim() != command.RewardKey || command.RewardKey.Length > 128
            || string.IsNullOrWhiteSpace(command.Source) || command.Source.Trim() != command.Source || command.Source.Length > 64
            || string.IsNullOrWhiteSpace(command.ResourceKey) || command.Amount <= 0 || command.Amount > 1_000_000_000_000L
            || command.ExpectedRevision < 0 || string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim() != command.IdempotencyKey || command.IdempotencyKey.Length > 256
            || command.NotificationKey is { } note && (string.IsNullOrWhiteSpace(note) || note.Trim() != note || note.Length > 128))
            return Fail(command.PlayerId, command.HiveId, "invalid_request");

        string payloadHash = Hash(command);
        RewardLedgerCommandResult? result = null;
        await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
        {
            state = Settle(state, clock.UtcNow, o.MaxEvents, o.MaxSettledOperationIds);
            RewardLedgerState ledger = state.RewardLedger ?? NewLedgerState();
            if (ledger.Receipts.TryGetValue(command.IdempotencyKey, out IdempotencyReceipt? stored))
            {
                result = stored.PayloadHash == payloadHash
                    ? new(stored.Succeeded, stored.Code, Snapshot(state, clock.UtcNow))
                    : new(false, "idempotency_conflict", Snapshot(state, clock.UtcNow));
                return state;
            }
            if (state.Revision != command.ExpectedRevision)
                return RecordFailure(state, ledger, command, payloadHash, "revision_conflict", out result);
            if (state.Rewards?.ContainsKey(command.RewardKey) == true || ledger.Entries.ContainsKey(command.RewardKey))
                return RecordFailure(state, ledger, command, payloadHash, "reward_already_granted", out result);
            if (ledger.Entries.Count >= o.MaxEntries)
                return RecordFailure(state, ledger, command, payloadHash, "reward_ledger_full", out result);

            DateTimeOffset now = clock.UtcNow;
            RewardLedgerEntry entry = new(command.RewardKey, command.Source, command.ResourceKey, command.Amount, 0, false, now, null, command.NotificationKey);
            Dictionary<string, RewardLedgerEntry> entries = new(ledger.Entries, StringComparer.Ordinal) { [command.RewardKey] = entry };
            Dictionary<string, RewardState> rewards = new(state.Rewards ?? new Dictionary<string, RewardState>(StringComparer.Ordinal), StringComparer.Ordinal)
            {
                [command.RewardKey] = new(command.RewardKey, command.ResourceKey, command.Amount, false, null)
            };
            List<RewardLedgerEvent> events = [.. ledger.Events, new RewardLedgerEvent(EventRewardGranted, command.RewardKey, now)];
            if (events.Count > o.MaxEvents) events.RemoveAt(0);
            RewardLedgerState updatedLedger = ledger with { Revision = ledger.Revision + 1, Entries = entries, Events = events };
            PlayerHiveState updated = state with { Revision = state.Revision + 1, Rewards = rewards, RewardLedger = updatedLedger };
            return RecordSuccess(updated, updatedLedger, command, payloadHash, now, out result);
        }, ct);
        return result ?? Fail(command.PlayerId, command.HiveId, "mutation_failed");
    }

    // Recensement idempotent des completions de files : une operation devenue
    // AwaitingCollection (timer ecoule) produit exactement UN evenement `queue_completed`,
    // marquee par OperationId dans SettledOperationIds - jamais de doublon au re-jeu.
    public static PlayerHiveState Settle(PlayerHiveState state, DateTimeOffset now, int maxEvents = 64, int maxSettledOperationIds = 512)
    {
        if (state.Operations is null || state.Operations.Count == 0) return state;
        RewardLedgerState ledger = state.RewardLedger ?? NewLedgerState();
        List<RewardLedgerEvent> events = [.. ledger.Events];
        HashSet<string> settled = [.. ledger.SettledOperationIds];
        bool changed = false;
        foreach (HiveOperation operation in state.Operations)
        {
            if (operation.Status != HiveOperationStatus.AwaitingCollection) continue;
            string id = operation.OperationId.ToString("N");
            if (!settled.Add(id)) continue;
            events.Add(new RewardLedgerEvent(EventQueueCompleted, operation.BuildingKey, now));
            if (events.Count > maxEvents) events.RemoveAt(0);
            if (settled.Count > maxSettledOperationIds) settled.Remove(settled.First());
            changed = true;
        }
        if (!changed) return state;
        RewardLedgerState updatedLedger = ledger with { Revision = ledger.Revision + 1, Events = events, SettledOperationIds = settled };
        return state with { Revision = state.Revision + 1, RewardLedger = updatedLedger };
    }

    private PlayerHiveState RecordFailure(PlayerHiveState state, RewardLedgerState ledger, GrantRewardCommand command, string hash, string code, out RewardLedgerCommandResult? result)
    {
        result = new(false, code, Snapshot(state, clock.UtcNow));
        Dictionary<string, IdempotencyReceipt> receipts = new(ledger.Receipts, StringComparer.Ordinal)
        {
            [command.IdempotencyKey] = new IdempotencyReceipt(hash, false, code, null, clock.UtcNow, state.Revision, state.Revision, AcceptedAtUtc: clock.UtcNow)
        };
        return state with { RewardLedger = ledger with { Receipts = receipts } };
    }

    private PlayerHiveState RecordSuccess(PlayerHiveState state, RewardLedgerState ledger, GrantRewardCommand command, string hash, DateTimeOffset now, out RewardLedgerCommandResult? result)
    {
        Dictionary<string, IdempotencyReceipt> receipts = new(ledger.Receipts, StringComparer.Ordinal)
        {
            [command.IdempotencyKey] = new IdempotencyReceipt(hash, true, "reward_granted", null, now, state.Revision - 1, state.Revision, AcceptedAtUtc: now)
        };
        if (receipts.Count > o.MaxReceipts)
        {
            KeyValuePair<string, IdempotencyReceipt> victim = receipts.Where(x => x.Key != command.IdempotencyKey).OrderBy(x => x.Value.CreatedAtUtc).ThenBy(x => x.Key, StringComparer.Ordinal).FirstOrDefault();
            if (victim.Key is not null) receipts.Remove(victim.Key);
        }
        PlayerHiveState updated = state with { RewardLedger = ledger with { Receipts = receipts } };
        result = new(true, "reward_granted", Snapshot(updated, now));
        return updated;
    }

    private RewardLedgerCommandResult Fail(Guid playerId, Guid hiveId, string code) =>
        new(false, code, new RewardLedgerReadSnapshot(playerId, hiveId, o.ContractVersion, 0, DateTimeOffset.UnixEpoch, Array.Empty<RewardLedgerEntryReadModel>(), Array.Empty<RewardLedgerEventReadModel>()));

    private void Ensure()
    {
        o.Validate();
        if (!o.Enabled) throw new InvalidOperationException("Reward ledger is disabled");
    }

    private RewardLedgerReadSnapshot Snapshot(PlayerHiveState state, DateTimeOffset now)
    {
        RewardLedgerState ledger = state.RewardLedger ?? NewLedgerState();
        List<RewardLedgerEntryReadModel> rewards = ledger.Entries
            .OrderBy(x => x.Value.GrantedAtUtc)
            .Select(x => new RewardLedgerEntryReadModel(x.Value.RewardKey, x.Value.Source, x.Value.ResourceKey, x.Value.Amount, x.Value.CreditedAmount, x.Value.Claimed, x.Value.NotificationKey))
            .ToList();
        List<RewardLedgerEventReadModel> events = ledger.Events
            .Select(x => new RewardLedgerEventReadModel(x.EventKey, x.TargetKey, x.AtUtc))
            .ToList();
        return new(state.PlayerId, state.HiveId, o.ContractVersion, ledger.Revision, now, rewards, events);
    }

    private static RewardLedgerState NewLedgerState() => new(
        0,
        new Dictionary<string, RewardLedgerEntry>(StringComparer.Ordinal),
        new List<RewardLedgerEvent>(),
        new HashSet<string>(StringComparer.Ordinal),
        new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal));

    private static string Hash(GrantRewardCommand command) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.RewardKey + "|" + command.Source + "|" + command.ResourceKey + "|" + command.Amount + "|" + command.ExpectedRevision))).ToLowerInvariant();
}
