using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed record RoyalReserveReadSnapshot(
    Guid PlayerId,
    Guid HiveId,
    string ContractVersion,
    long Revision,
    DateTimeOffset ServerTimeUtc,
    int BankLevel,
    long Capacity,
    long ReservedTotal,
    IReadOnlyDictionary<string, long> Reserved,
    IReadOnlyDictionary<string, ResourceBalance> LiquidBalances);

public sealed record RoyalReserveTransferRequest(
    string ResourceKey,
    long Amount,
    long ExpectedRevision,
    string IdempotencyKey);

public sealed record RoyalReserveCommandResult(
    bool Succeeded,
    string Code,
    RoyalReserveReadSnapshot Snapshot);

public sealed class RoyalReserveService(
    IHiveStateRepository repository,
    IServerClock clock,
    HiveOfflineProductionOptions storageOptions)
{
    public const string ContractVersion = "living-hive-bank-v1";
    public const string BankBuildingKey = "hive_bank";
    private const int MaxReceipts = 4096;
    private const long BaseCapacity = 5_000;
    private static readonly string[] ResourceKeys = ["honey", "pollen", "wax"];

    public async Task<RoyalReserveReadSnapshot?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        ValidateIds(playerId, hiveId);
        PlayerHiveState? state = await repository.ReadAsync(playerId, hiveId, ct);
        return state is null ? null : Snapshot(state, Utc());
    }

    public Task<RoyalReserveCommandResult> DepositAsync(
        Guid playerId,
        Guid hiveId,
        RoyalReserveTransferRequest request,
        CancellationToken ct = default) =>
        TransferAsync(playerId, hiveId, request, withdraw: false, ct);

    public Task<RoyalReserveCommandResult> WithdrawAsync(
        Guid playerId,
        Guid hiveId,
        RoyalReserveTransferRequest request,
        CancellationToken ct = default) =>
        TransferAsync(playerId, hiveId, request, withdraw: true, ct);

    public static long CapacityForBankLevel(int level)
    {
        if (level < 1 || level > 100) throw new InvalidDataException("Invalid bank level.");
        return checked(BaseCapacity * level * level);
    }

    private async Task<RoyalReserveCommandResult> TransferAsync(
        Guid playerId,
        Guid hiveId,
        RoyalReserveTransferRequest request,
        bool withdraw,
        CancellationToken ct)
    {
        ValidateIds(playerId, hiveId);
        if (request is null ||
            !ResourceKeys.Contains(request.ResourceKey, StringComparer.Ordinal) ||
            request.Amount <= 0 ||
            request.Amount > 1_000_000_000_000L ||
            request.ExpectedRevision < 0 ||
            request.ExpectedRevision == long.MaxValue ||
            !ValidIdempotencyKey(request.IdempotencyKey))
            return EmptyFailure(playerId, hiveId, "game.invalid_request");

        string action = withdraw ? "withdraw" : "deposit";
        string payloadHash = Hash(
            $"{action}|{request.ResourceKey}|{request.Amount}|{request.ExpectedRevision}");

        RoyalReserveCommandResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            DateTimeOffset now = Utc();
            RoyalReserveState reserve = EnsureReserve(state.RoyalReserve);

            if (reserve.Receipts.TryGetValue(request.IdempotencyKey, out IdempotencyReceipt? stored))
            {
                result = stored.PayloadHash == payloadHash
                    ? new RoyalReserveCommandResult(
                        stored.Succeeded,
                        stored.Code,
                        Snapshot(state, now))
                    : new RoyalReserveCommandResult(
                        false,
                        "game.idempotency_conflict",
                        Snapshot(state, now));
                return state;
            }

            // La Réserve Royale possède sa propre révision métier. Les autres mutations de la
            // ruche (production, combat, lecture avec accrual, etc.) ne doivent jamais invalider
            // une transaction bancaire préparée à partir d'un snapshot encore courant de la Banque.
            if (reserve.Revision != request.ExpectedRevision)
            {
                result = new(false, "game.revision_conflict", Snapshot(state, now));
                return state;
            }

            if (!state.Resources.TryGetValue(request.ResourceKey, out ResourceBalance? liquid) ||
                liquid.Amount < 0 ||
                liquid.Capacity < liquid.Amount)
            {
                result = new(false, "game.invalid_resource_state", Snapshot(state, now));
                return state;
            }

            int bankLevel = BankLevel(state);
            long reserveCapacity = CapacityForBankLevel(bankLevel);
            long currentReserved = reserve.Amounts.GetValueOrDefault(request.ResourceKey);
            long totalReserved = SafeSum(reserve.Amounts.Values);

            if (!withdraw)
            {
                if (liquid.Amount < request.Amount)
                {
                    result = new(false, "game.insufficient_resources", Snapshot(state, now));
                    return state;
                }
                if (checked(totalReserved + request.Amount) > reserveCapacity)
                {
                    result = new(false, "game.bank_reserve_capacity_insufficient", Snapshot(state, now));
                    return state;
                }
            }
            else
            {
                if (currentReserved < request.Amount)
                {
                    result = new(false, "game.bank_reserve_insufficient", Snapshot(state, now));
                    return state;
                }

                long effectiveStorageCapacity = EffectiveLiquidCapacity(state, request.ResourceKey, liquid);
                if (checked(liquid.Amount + request.Amount) > effectiveStorageCapacity)
                {
                    result = new(false, "game.storage_capacity_insufficient", Snapshot(state, now));
                    return state;
                }
            }

            long reserveRevisionBefore = reserve.Revision;
            long reserveRevisionAfter = checked(reserveRevisionBefore + 1);
            long hiveRevisionAfter = checked(state.Revision + 1);
            Dictionary<string, ResourceBalance> resources = new(state.Resources, StringComparer.Ordinal);
            Dictionary<string, long> amounts = new(reserve.Amounts, StringComparer.Ordinal);

            if (withdraw)
            {
                resources[request.ResourceKey] = liquid with { Amount = checked(liquid.Amount + request.Amount) };
                amounts[request.ResourceKey] = checked(currentReserved - request.Amount);
            }
            else
            {
                resources[request.ResourceKey] = liquid with { Amount = checked(liquid.Amount - request.Amount) };
                amounts[request.ResourceKey] = checked(currentReserved + request.Amount);
            }

            string successCode = withdraw
                ? "game.bank_reserve_withdrawn"
                : "game.bank_reserve_deposited";

            Dictionary<string, IdempotencyReceipt> receipts =
                new(reserve.Receipts, StringComparer.Ordinal)
                {
                    [request.IdempotencyKey] = new(
                        payloadHash,
                        true,
                        successCode,
                        null,
                        now,
                        reserveRevisionBefore,
                        reserveRevisionAfter,
                        AcceptedAtUtc: now)
                };
            TrimReceipts(receipts);

            RoyalReserveState updatedReserve = new(reserveRevisionAfter, amounts, receipts);
            PlayerHiveState updated = state with
            {
                Revision = hiveRevisionAfter,
                Resources = resources,
                RoyalReserve = updatedReserve
            };
            result = new(true, successCode, Snapshot(updated, now));
            return updated;
        }, ct);

        return result!;
    }

    private RoyalReserveReadSnapshot Snapshot(PlayerHiveState state, DateTimeOffset now)
    {
        RoyalReserveState reserve = EnsureReserve(state.RoyalReserve);
        Dictionary<string, long> amounts = ResourceKeys.ToDictionary(
            key => key,
            key => reserve.Amounts.GetValueOrDefault(key),
            StringComparer.Ordinal);
        Dictionary<string, ResourceBalance> liquid = ResourceKeys.ToDictionary(
            key => key,
            key => state.Resources.GetValueOrDefault(key, new ResourceBalance(0, 0)),
            StringComparer.Ordinal);

        int bankLevel = BankLevel(state);
        return new(
            state.PlayerId,
            state.HiveId,
            ContractVersion,
            reserve.Revision,
            now,
            bankLevel,
            CapacityForBankLevel(bankLevel),
            SafeSum(amounts.Values),
            amounts,
            liquid);
    }

    private long EffectiveLiquidCapacity(
        PlayerHiveState state,
        string resourceKey,
        ResourceBalance currentBalance)
    {
        OfflineProductionCatalogEntry? item = storageOptions.Catalog?
            .SingleOrDefault(x => string.Equals(x.ResourceKey, resourceKey, StringComparison.Ordinal));
        if (item is null) return currentBalance.Capacity;
        return Math.Min(
            currentBalance.Capacity,
            HiveOfflineProductionService.EffectiveCapacity(state, item));
    }

    private static RoyalReserveState EnsureReserve(RoyalReserveState? reserve) =>
        reserve is null
            ? new(
                0,
                new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    ["honey"] = 0,
                    ["pollen"] = 0,
                    ["wax"] = 0
                },
                new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal))
            : reserve;

    private static int BankLevel(PlayerHiveState state)
    {
        int level = state.BuildingLevels.GetValueOrDefault(BankBuildingKey, 1);
        if (level < 1) throw new InvalidDataException("Invalid bank level.");
        return level;
    }

    private DateTimeOffset Utc()
    {
        DateTimeOffset now = clock.UtcNow;
        if (now.Offset != TimeSpan.Zero) throw new InvalidDataException("Server clock must be UTC.");
        return now;
    }

    private static void ValidateIds(Guid playerId, Guid hiveId)
    {
        if (playerId == Guid.Empty || hiveId == Guid.Empty)
            throw new ArgumentException("game.invalid_request");
    }

    private static bool ValidIdempotencyKey(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value == value.Trim() &&
        value.Length <= 256;

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static long SafeSum(IEnumerable<long> values)
    {
        try { return checked(values.Sum()); }
        catch (OverflowException) { throw new InvalidDataException("Invalid royal reserve amount."); }
    }

    private static void TrimReceipts(Dictionary<string, IdempotencyReceipt> receipts)
    {
        while (receipts.Count > MaxReceipts)
        {
            string oldest = receipts
                .OrderBy(x => x.Value.CreatedAtUtc)
                .ThenBy(x => x.Key, StringComparer.Ordinal)
                .First().Key;
            receipts.Remove(oldest);
        }
    }

    private RoyalReserveCommandResult EmptyFailure(Guid playerId, Guid hiveId, string code) =>
        new(
            false,
            code,
            new RoyalReserveReadSnapshot(
                playerId,
                hiveId,
                ContractVersion,
                0,
                DateTimeOffset.UnixEpoch,
                1,
                CapacityForBankLevel(1),
                0,
                new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    ["honey"] = 0,
                    ["pollen"] = 0,
                    ["wax"] = 0
                },
                new Dictionary<string, ResourceBalance>(StringComparer.Ordinal)));
}
