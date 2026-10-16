using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

/// <summary>
/// Feature flag deliberately defaults OFF until Unity client, migration and
/// balance have passed user acceptance and authenticated end-to-end testing.
/// </summary>
public sealed class NurseryCohortOptions
{
    public const string SectionName = "NurseryCohorts";
    public bool Enabled { get; set; }
}

public sealed record NurseryActiveCohort(
    Guid CohortId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset ReadyAtUtc,
    int ExpectedRecruits,
    int NutritionAtStart,
    int StabilityAtStart);

public sealed record NurseryCohortState(
    long Revision,
    long RecruitsInReserve,
    NurseryActiveCohort? ActiveCohort,
    Dictionary<string, IdempotencyReceipt> Receipts);

public sealed record NurseryCohortSnapshot(
    Guid PlayerId,
    Guid HiveId,
    string ContractVersion,
    long HiveRevision,
    long CohortRevision,
    DateTimeOffset ServerTimeUtc,
    long RecruitsInReserve,
    long ReserveCapacity,
    NurseryActiveCohort? Active,
    NurseryCohortForecast? NextForecast,
    bool CanStart,
    string Status);

public sealed record NurseryCohortStartRequest(
    long ExpectedRevision,
    string IdempotencyKey);
public sealed record NurseryCohortStartCommand(
    Guid PlayerId,
    Guid HiveId,
    long ExpectedRevision,
    string IdempotencyKey);

public sealed record NurseryCohortCommandReceipt(
    string IdempotencyKey,
    Guid CohortId,
    long RevisionBefore,
    long RevisionAfter,
    DateTimeOffset AcceptedAtUtc,
    string Code);

public sealed record NurseryCohortCommandResult(
    bool Succeeded,
    string Code,
    NurseryCohortSnapshot Snapshot,
    NurseryCohortCommandReceipt? Receipt = null);

/// <summary>
/// Durable, atomic prototype of Nursery Cohort V1. Fully disconnected from
/// military training, and disabled by default. No paid incubation, and NO
/// changes to existing Feed/Stabilize operation contracts.
/// </summary>
public sealed class NurseryCohortService(
    IHiveStateRepository repository, IServerClock clock, NurseryCohortOptions options)
{
    public const string ContractVersion = "bk-nursery-cohorts-v1-prototype";
    public const string NurseryBuildingKey = "nursery_cluster";
    private const int MaxReceipts = 128;

    private readonly NurseryCohortOptions o =
        options ?? throw new ArgumentNullException(nameof(options));

    public async Task<NurseryCohortSnapshot> ReadAsync(
        Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        EnsureEnabled();
        if (playerId == Guid.Empty || hiveId == Guid.Empty)
            throw new ArgumentException("game.invalid_request");
        DateTimeOffset now = ServerNow();
        if (await repository.ReadAsync(playerId, hiveId, ct) is null)
            throw new KeyNotFoundException("game.not_found");
        PlayerHiveState hive = await repository.ExecuteAtomicallyAsync(playerId, hiveId,
            state =>
            {
                EnsureScope(state, playerId, hiveId);
                return SettleReadyCohort(state, now);
            }, ct);
        return Snapshot(hive, now);
    }

    public async Task<NurseryCohortCommandResult> StartAsync(
        NurseryCohortStartCommand command, CancellationToken ct = default)
    {
        EnsureEnabled();
        if (command is null || command.PlayerId == Guid.Empty
            || command.HiveId == Guid.Empty || command.ExpectedRevision < 0
            || command.ExpectedRevision == long.MaxValue
            || !ValidIdempotencyKey(command.IdempotencyKey))
            throw new ArgumentException("game.invalid_request");
        // Both supported hive repositories can AUTO-CREATE missing hives inside
        // ExecuteAtomicallyAsync. The nursery must only mutate a pre-existing
        // player-owned hive, never create a phantom hive from an arbitrary URL.
        if (await repository.ReadAsync(command.PlayerId, command.HiveId, ct) is null)
            throw new KeyNotFoundException("game.not_found");
        DateTimeOffset now = ServerNow();
        NurseryCohortCommandResult? result = null;
        await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId,
            original =>
            {
                EnsureScope(original, command.PlayerId, command.HiveId);
                PlayerHiveState state = SettleReadyCohort(original, now);
                NurseryCohortState current = state.NurseryCohorts ?? Empty();
                string payload = Hash("start|" + command.ExpectedRevision.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                    + "|" + NurseryGrowthRules.ContractVersion);
                if (current.Receipts.TryGetValue(command.IdempotencyKey,
                    out IdempotencyReceipt? stored))
                {
                    if (stored.PayloadHash != payload)
                    {
                        result = Fail(state, now, "game.idempotency_conflict");
                        return state;
                    }

                    NurseryCohortCommandReceipt receipt = new(
                        command.IdempotencyKey, stored.OperationId ?? Guid.Empty,
                        stored.RevisionBefore ?? 0,
                        stored.RevisionAfter ?? 0,
                        stored.AcceptedAtUtc ?? stored.CreatedAtUtc,
                        stored.Code);
                    result = new(true, stored.Code, Snapshot(state, now), receipt);
                    return state;
                }
                if (state.Revision != command.ExpectedRevision)
                {
                    result = Fail(state, now, "game.revision_conflict");
                    return state;
                }
                if (state.BuildingLevels.GetValueOrDefault(NurseryBuildingKey) < 1)
                {
                    result = Fail(state, now, "game.nursery_not_built");
                    return state;
                }
                if (state.BroodVitality is not { } vitality)
                {
                    result = Fail(state, now, "game.vitality_not_initialized");
                    return state;
                }
                if (current.ActiveCohort is not null)
                {
                    result = Fail(state, now, "game.nursery_cohort_busy");
                    return state;
                }
                if (current.Revision == long.MaxValue)
                    throw new InvalidDataException("nursery revision overflow");
                if (state.Revision == long.MaxValue)
                    throw new InvalidDataException("hive revision overflow");

                NurseryCohortForecast plan = Forecast(vitality, current, now);
                if (!plan.CanStartWithCurrentReserve)
                {
                    result = Fail(state, now, "game.nursery_reserve_full");
                    return state;
                }

                Guid id = Guid.NewGuid();
                NurseryActiveCohort active = new(
                    id, now, plan.EndsAtUtc, plan.ExpectedRecruits,
                    plan.NutritionAtStart, plan.StabilityAtStart);
                long revBefore = state.Revision;
                long revAfter = checked(revBefore + 1);
                NurseryCohortState nextState = current with
                {
                    Revision = checked(current.Revision + 1),
                    ActiveCohort = active
                };
                var receipts = new Dictionary<string, IdempotencyReceipt>(
                    current.Receipts, StringComparer.Ordinal)
                {
                    [command.IdempotencyKey] = new(
                        payload, true, "game.nursery_cohort_started",
                        id, now, revBefore, revAfter,
                        AcceptedAtUtc: now)
                };
                while (receipts.Count > MaxReceipts)
                {
                    string victim = receipts
                        .Where(entry => entry.Key != command.IdempotencyKey)
                        .OrderBy(entry => entry.Value.CreatedAtUtc)
                        .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                        .First().Key;
                    receipts.Remove(victim);
                }

                PlayerHiveState updated = state with
                {
                    Revision = revAfter,
                    NurseryCohorts = nextState with { Receipts = receipts }
                };
                result = new(true, "game.nursery_cohort_started",
                    Snapshot(updated, now), new(
                        command.IdempotencyKey, id, revBefore, revAfter, now,
                        "game.nursery_cohort_started"));
                return updated;
            }, ct);
        return result ?? throw new InvalidOperationException(
            "Nursery mutation did not return a result");
    }

    private static NurseryCohortState Empty() => new(
        0, 0, null, new Dictionary<string, IdempotencyReceipt>(
            StringComparer.Ordinal));

    private static NurseryCohortForecast Forecast(
        BroodVitalityState vitality,
        NurseryCohortState state,
        DateTimeOffset now) =>
        NurseryGrowthRules.Preview(
            vitality.Nutrition, vitality.Stability,
            NurseryGrowthRules.InitialBaseRecruits,
            NurseryGrowthRules.InitialBaseDurationSeconds,
            state.RecruitsInReserve,
            NurseryGrowthRules.InitialReserveCapacity, now);

    private static PlayerHiveState SettleReadyCohort(
        PlayerHiveState hive, DateTimeOffset now)
    {
        NurseryCohortState? state = hive.NurseryCohorts;
        if (state?.ActiveCohort is not { } active
            || now < active.ReadyAtUtc)
            return hive;
        if (active.ExpectedRecruits < 1
            || active.ExpectedRecruits > NurseryGrowthRules.InitialReserveCapacity
            || state.RecruitsInReserve < 0
            || state.RecruitsInReserve >
                NurseryGrowthRules.InitialReserveCapacity - active.ExpectedRecruits)
            throw new InvalidDataException("Nursery reserve capacity invariant");
        if (hive.Revision == long.MaxValue || state.Revision == long.MaxValue)
            throw new InvalidDataException("Nursery settling revision overflow");
        return hive with
        {
            Revision = checked(hive.Revision + 1),
            NurseryCohorts = state with
            {
                Revision = checked(state.Revision + 1),
                RecruitsInReserve = checked(
                    state.RecruitsInReserve + active.ExpectedRecruits),
                ActiveCohort = null
            }
        };
    }

    private NurseryCohortSnapshot Snapshot(
        PlayerHiveState hive, DateTimeOffset now)
    {
        NurseryCohortState s = hive.NurseryCohorts ?? Empty();
        NurseryCohortForecast? forecast = hive.BroodVitality is { } vitality
            ? Forecast(vitality, s, now) : null;
        bool buildingReady = hive.BuildingLevels.GetValueOrDefault(
            NurseryBuildingKey) >= 1;
        bool canStart = buildingReady && forecast?.CanStartWithCurrentReserve == true
            && s.ActiveCohort is null;
        string status = !buildingReady ? "nursery_not_built"
            : forecast is null ? "vitality_not_initialized"
            : s.ActiveCohort is not null ? "incubating"
            : canStart ? "ready_to_start"
            : "reserve_full";
        return new(hive.PlayerId, hive.HiveId, ContractVersion,
            hive.Revision, s.Revision, now,
            s.RecruitsInReserve,
            NurseryGrowthRules.InitialReserveCapacity,
            s.ActiveCohort, forecast, canStart, status);
    }

    private NurseryCohortCommandResult Fail(
        PlayerHiveState hive, DateTimeOffset now, string code) =>
        new(false, code, Snapshot(hive, now));

    private void EnsureEnabled()
    {
        if (!o.Enabled)
            throw new InvalidOperationException(
                "Nursery cohorts are disabled until certified.");
    }

    private DateTimeOffset ServerNow()
    {
        DateTimeOffset now = clock.UtcNow;
        if (now.Offset != TimeSpan.Zero)
            throw new InvalidDataException("Nursery clock must be UTC.");
        return now;
    }

    private static void EnsureScope(
        PlayerHiveState state, Guid player, Guid hive)
    {
        if (state.PlayerId != player || state.HiveId != hive)
            throw new InvalidDataException("Nursery owner/hive scope mismatch.");
    }

    private static bool ValidIdempotencyKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) && key.Trim() == key
        && key.Length <= 256
        && key.All(ch => char.IsAsciiLetterOrDigit(ch)
            || ch is '-' or '_' or '.');

    private static string Hash(string payload) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
}
