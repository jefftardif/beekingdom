namespace BeeKingdom.HiveOperations;

public sealed record BroodVitalityCareRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record BroodVitalityCareReceipt(Guid PlayerId, Guid HiveId, string IdempotencyKey, Guid OperationId, string Type, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, string Code);
public sealed record BroodVitalityCareSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, DateTimeOffset ServerTimeUtc, long GlobalRevision, BroodVitalityState? Vitality);
public sealed record BroodVitalityCareResponse(BroodVitalityCareReceipt Receipt, BroodVitalityCareSnapshot Snapshot);
public sealed record BroodVitalityCareResult(bool Succeeded, string Code, BroodVitalityCareReceipt? Receipt, PlayerHiveState State);

public sealed class BroodVitalityCareService(IHiveStateRepository repository, IServerClock clock, bool dailyRoundEnabled = false)
{
    private const int MaxReceipts = 128;
    public const string ContractVersion = "living-hive-brood-vitality-v1";

    public async Task<BroodVitalityCareResult> StartAsync(Guid playerId, Guid hiveId, string type, BroodVitalityCareRequest request, CancellationToken ct = default)
    {
        Validate(playerId, hiveId, request);
        if (!BroodVitalityOperationTypes.Allowed.Contains(type)) throw new ArgumentException("game.invalid_request");
        var now = clock.UtcNow;
        if (now.Offset != TimeSpan.Zero) throw new InvalidDataException("non-UTC clock");
        BroodVitalityCareResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            EnsureScope(state, playerId, hiveId);
            var receipts = state.BroodCareReceipts ?? new Dictionary<string, BroodCareStoredReceipt>(StringComparer.Ordinal);
            var hash = Hash(type + "|" + request.ExpectedRevision);
            if (receipts.TryGetValue(request.IdempotencyKey, out var stored))
            {
                result = stored.PayloadHash == hash
                    ? new(true, stored.Code, ToReceipt(playerId, hiveId, request.IdempotencyKey, stored), state)
                    : new(false, "game.idempotency_conflict", null, state);
                return state;
            }
            if (state.BroodVitality is null) { result = new(false, "game.vitality_not_initialized", null, state); return state; }
            if (state.Revision != request.ExpectedRevision) { result = new(false, "game.revision_conflict", null, state); return state; }
            if (state.BroodVitality.ActiveOperation is not null) { result = new(false, "game.vitality_busy", null, state); return state; }
            if (state.BroodVitality.Revision == long.MaxValue) throw new InvalidDataException("vitality revision overflow");
            string resource = type == BroodVitalityOperationTypes.Feeding ? "honey" : "wax";
            long cost = type == BroodVitalityOperationTypes.Feeding ? 300 : 45;
            if (!state.Resources.TryGetValue(resource, out var balance) || balance.Amount < cost) { result = new(false, "game.insufficient_resources", null, state); return state; }
            if (state.Revision == long.MaxValue) throw new InvalidDataException("revision overflow");
            var operationId = Guid.NewGuid();
            DateTimeOffset ends; try { ends = now.AddSeconds(type == BroodVitalityOperationTypes.Feeding ? 12 : 13); } catch (ArgumentOutOfRangeException ex) { throw new InvalidDataException("operation time overflow", ex); }
            var op = new BroodVitalityOperation(operationId, type, now, ends);
            var before = state.Revision; var after = checked(before + 1);
            var storedNew = new BroodCareStoredReceipt(hash, true, type, operationId, before, after, now, "game.vitality_care_started");
            receipts = AddReceipt(receipts, request.IdempotencyKey, storedNew);
            var updated = state with
            {
                Revision = after,
                Resources = new Dictionary<string, ResourceBalance>(state.Resources) { [resource] = balance with { Amount = balance.Amount - cost } },
                BroodVitality = state.BroodVitality with { Revision = checked(state.BroodVitality.Revision + 1), ActiveOperation = op },
                BroodCareReceipts = receipts
            };
            if (dailyRoundEnabled) updated = HiveDailyRoundFacts.ApplyFreshFact(updated, now, HiveDailyRoundFact.OperationLaunched, false);
            result = new(true, storedNew.Code, ToReceipt(playerId, hiveId, request.IdempotencyKey, storedNew), updated);
            return updated;
        }, ct);
        return result!;
    }

    public async Task<BroodVitalityCareResult> CompleteAsync(Guid playerId, Guid hiveId, Guid operationId, BroodVitalityCareRequest request, CancellationToken ct = default)
    {
        Validate(playerId, hiveId, request);
        if (operationId == Guid.Empty) throw new ArgumentException("game.invalid_request");
        var now = clock.UtcNow;
        if (now.Offset != TimeSpan.Zero) throw new InvalidDataException("non-UTC clock");
        BroodVitalityCareResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            EnsureScope(state, playerId, hiveId);
            var receipts = state.BroodCareReceipts ?? new Dictionary<string, BroodCareStoredReceipt>(StringComparer.Ordinal);
            var hash = Hash(operationId.ToString("N") + "|" + request.ExpectedRevision);
            if (receipts.TryGetValue(request.IdempotencyKey, out var stored))
            {
                result = stored.PayloadHash == hash
                    ? new(true, stored.Code, ToReceipt(playerId, hiveId, request.IdempotencyKey, stored), state)
                    : new(false, "game.idempotency_conflict", null, state);
                return state;
            }
            if (state.BroodVitality?.ActiveOperation is not { } op || op.OperationId != operationId) { result = new(false, "game.vitality_not_found", null, state); return state; }
            if (state.Revision != request.ExpectedRevision) { result = new(false, "game.revision_conflict", null, state); return state; }
            if (op.EndsAtUtc > now) { result = new(false, "game.vitality_not_ready", null, state); return state; }
            if (state.Revision == long.MaxValue || state.BroodVitality.Revision == long.MaxValue) throw new InvalidDataException("revision overflow");
            int nutrition = state.BroodVitality.Nutrition, stability = state.BroodVitality.Stability;
            if (op.Type == BroodVitalityOperationTypes.Feeding) nutrition = Math.Min(100, nutrition + 22); else stability = Math.Min(100, stability + 7);
            var before = state.Revision; var after = checked(before + 1);
            var storedNew = new BroodCareStoredReceipt(hash, true, op.Type, operationId, before, after, now, "game.vitality_care_completed");
            receipts = AddReceipt(receipts, request.IdempotencyKey, storedNew);
            var updated = state with { Revision = after, BroodVitality = state.BroodVitality with { Nutrition = nutrition, Stability = stability, Revision = checked(state.BroodVitality.Revision + 1), UpdatedAtUtc = now, ActiveOperation = null }, BroodCareReceipts = receipts };
            result = new(true, storedNew.Code, ToReceipt(playerId, hiveId, request.IdempotencyKey, storedNew), updated);
            return updated;
        }, ct);
        return result!;
    }

    private static void Validate(Guid playerId, Guid hiveId, BroodVitalityCareRequest request)
    {
        if (playerId == Guid.Empty || hiveId == Guid.Empty || request is null || request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue || string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim() != request.IdempotencyKey || request.IdempotencyKey.Length > 256 || request.IdempotencyKey.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))) throw new ArgumentException("game.invalid_request");
    }
    private static void EnsureScope(PlayerHiveState state, Guid playerId, Guid hiveId) { if (state.PlayerId != playerId || state.HiveId != hiveId) throw new InvalidDataException("scope"); }
    private static BroodVitalityCareReceipt ToReceipt(Guid p, Guid h, string key, BroodCareStoredReceipt s) => new(p, h, key, s.OperationId, s.Type, s.RevisionBefore, s.RevisionAfter, s.AcceptedAtUtc, s.Code);
    private static Dictionary<string, BroodCareStoredReceipt> AddReceipt(Dictionary<string, BroodCareStoredReceipt> source, string key, BroodCareStoredReceipt receipt)
    {
        var copy = new Dictionary<string, BroodCareStoredReceipt>(source, StringComparer.Ordinal) { [key] = receipt };
        while (copy.Count > MaxReceipts)
        {
            var victim = copy.OrderBy(x => x.Value.AcceptedAtUtc).ThenBy(x => x.Key, StringComparer.Ordinal).First(x => x.Key != key);
            copy.Remove(victim.Key);
        }
        return copy;
    }
    private static string Hash(string value)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}
