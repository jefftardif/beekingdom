using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

/// <summary>
/// Alpha pilot configuration. All balance values are provisional, server-owned,
/// and the feature is OFF by default until acceptance tests and balancing.
/// </summary>
public sealed class CivilPopulationOptions
{
    public const string SectionName = "CivilPopulation";
    public bool Enabled { get; set; }
    public long WorkersPerCohort { get; set; } = 20;
    public long HoneyCost { get; set; } = 50;
    public long PollenCost { get; set; } = 10;
    public int IncubationMinutes { get; set; } = 60;
    public long CapacityPerNurseryLevel { get; set; } = 200;

    public void Validate()
    {
        if (WorkersPerCohort is < 1 or > 10_000
            || HoneyCost is < 0 or > 1_000_000
            || PollenCost is < 0 or > 1_000_000
            || IncubationMinutes is < 1 or > 10_080
            || CapacityPerNurseryLevel is < 1 or > 1_000_000)
            throw new InvalidDataException("Invalid civil population pilot configuration.");
    }
}

public sealed record CivilNurseryCohort(
    Guid CohortId, DateTimeOffset StartedAtUtc, DateTimeOffset ReadyAtUtc,
    long ExpectedWorkers, long HoneySpent, long PollenSpent);

/// <summary>
/// Civil workers are NEVER part of DoctrineRoster, CommittedTroops or
/// NurseryCohortState.RecruitsInReserve. Assigned/Recovering are reserved for
/// future server-authoritative expedition/recovery transitions.
/// </summary>
public sealed record CivilPopulationState(
    long Revision,
    long AvailableWorkers,
    long AssignedWorkers,
    long RecoveringWorkers,
    CivilNurseryCohort? Active,
    Dictionary<string, IdempotencyReceipt> Receipts);

public sealed record CivilPopulationSnapshot(
    Guid PlayerId, Guid HiveId, string ContractVersion, long HiveRevision,
    long CivilRevision, DateTimeOffset ServerTimeUtc,
    long Capacity, long AvailableWorkers, long AssignedWorkers,
    long RecoveringWorkers, CivilNurseryCohort? Active,
    bool CanStart, string Status);

public sealed record StartCivilCohortRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record CivilPopulationCommandReceipt(
    string IdempotencyKey, Guid CohortId,
    long RevisionBefore, long RevisionAfter,
    DateTimeOffset AcceptedAtUtc, string Code);
public sealed record CivilPopulationCommandResult(
    bool Succeeded, string Code, CivilPopulationSnapshot Snapshot,
    CivilPopulationCommandReceipt? Receipt = null);

/// <summary>
/// Persisted, paid civilian brood cycle using the existing *hive* transaction.
/// This component does not implement shared floral sites or expeditions; those
/// require an additional authoritative, cross-hive transaction coordinator.
/// </summary>
public sealed class CivilPopulationService(
    IHiveStateRepository repository, IServerClock clock, CivilPopulationOptions options)
{
    public const string ContractVersion = "bk-civil-population-v1-alpha";
    private const string NurseryKey = "nursery_cluster";
    private const int MaximumReceipts = 20_000;
    private readonly CivilPopulationOptions settings = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<CivilPopulationSnapshot> ReadAsync(
        Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        EnsureEnabled();
        EnsureIdentity(playerId, hiveId);
        if (await repository.ReadAsync(playerId, hiveId, ct) is null)
            throw new KeyNotFoundException("game.not_found");
        PlayerHiveState updated = await repository.ExecuteAtomicallyAsync(
            playerId, hiveId,
            state => SettleMatureCohort(RequireScope(state, playerId, hiveId), Now()),
            ct);
        return Snapshot(updated, Now());
    }

    public async Task<CivilPopulationCommandResult> StartAsync(
        Guid playerId, Guid hiveId, StartCivilCohortRequest request,
        CancellationToken ct = default)
    {
        EnsureEnabled();
        EnsureIdentity(playerId, hiveId);
        if (request is null || request.ExpectedRevision < 0
            || request.ExpectedRevision == long.MaxValue
            || !ValidKey(request.IdempotencyKey))
            throw new ArgumentException("game.invalid_request");
        // Prevent phantom hives: both repositories otherwise create rows
        // during ExecuteAtomicallyAsync if the supplied hive doesn't exist.
        if (await repository.ReadAsync(playerId, hiveId, ct) is null)
            throw new KeyNotFoundException("game.not_found");

        CivilPopulationCommandResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, original =>
        {
            DateTimeOffset now = Now();
            PlayerHiveState state = SettleMatureCohort(
                RequireScope(original, playerId, hiveId), now);
            CivilPopulationState civil = state.CivilPopulation ?? Empty();
            string payloadHash = Hash("civil-start-v1|" +
                request.ExpectedRevision.ToString(System.Globalization.CultureInfo.InvariantCulture));

            // Check the receipt BEFORE the revision to make network retries safe.
            if (civil.Receipts.TryGetValue(request.IdempotencyKey,
                    out IdempotencyReceipt? previous))
            {
                if (!string.Equals(previous.PayloadHash, payloadHash, StringComparison.Ordinal))
                {
                    result = Failure(state, now, "game.idempotency_conflict");
                    return state;
                }
                result = new CivilPopulationCommandResult(
                    previous.Succeeded, previous.Code, Snapshot(state, now),
                    new CivilPopulationCommandReceipt(
                        request.IdempotencyKey, previous.OperationId ?? Guid.Empty,
                        previous.RevisionBefore ?? 0, previous.RevisionAfter ?? 0,
                        previous.AcceptedAtUtc ?? previous.CreatedAtUtc, previous.Code));
                return state;
            }

            if (state.Revision != request.ExpectedRevision)
            {
                result = Failure(state, now, "game.revision_conflict");
                return state;
            }
            if (state.BuildingLevels.GetValueOrDefault(NurseryKey) < 1)
            {
                result = Failure(state, now, "game.nursery_not_built");
                return state;
            }
            if (civil.Active is not null)
            {
                result = Failure(state, now, "game.civil_cohort_busy");
                return state;
            }

            long cap = Capacity(state);
            long occupied = checked(civil.AvailableWorkers
                + civil.AssignedWorkers + civil.RecoveringWorkers);
            if (occupied > cap || settings.WorkersPerCohort > cap - occupied)
            {
                result = Failure(state, now, "game.civil_population_capacity");
                return state;
            }
            if (!CanPay(state))
            {
                result = Failure(state, now, "game.insufficient_resources");
                return state;
            }
            if (civil.Receipts.Count >= MaximumReceipts)
            {
                // Never evict idempotency keys while they could be replayed.
                result = Failure(state, now, "game.civil_receipt_limit");
                return state;
            }

            Dictionary<string, ResourceBalance> resources =
                new(state.Resources, StringComparer.Ordinal);
            resources["honey"] = resources["honey"] with
            {
                Amount = resources["honey"].Amount - settings.HoneyCost
            };
            resources["pollen"] = resources["pollen"] with
            {
                Amount = resources["pollen"].Amount - settings.PollenCost
            };

            Guid cohortId = Guid.NewGuid();
            CivilNurseryCohort active = new(
                cohortId, now, now.AddMinutes(settings.IncubationMinutes),
                settings.WorkersPerCohort, settings.HoneyCost, settings.PollenCost);
            long before = state.Revision, after = checked(before + 1);
            Dictionary<string, IdempotencyReceipt> receipts =
                new(civil.Receipts, StringComparer.Ordinal)
                {
                    [request.IdempotencyKey] = new(
                        payloadHash, true, "game.civil_cohort_started", cohortId,
                        now, before, after, AcceptedAtUtc: now)
                };
            PlayerHiveState updated = state with
            {
                Revision = after,
                Resources = resources,
                CivilPopulation = civil with
                {
                    Revision = checked(civil.Revision + 1),
                    Active = active,
                    Receipts = receipts
                }
            };
            result = new CivilPopulationCommandResult(
                true, "game.civil_cohort_started", Snapshot(updated, now),
                new CivilPopulationCommandReceipt(
                    request.IdempotencyKey, cohortId, before, after,
                    now, "game.civil_cohort_started"));
            return updated;
        }, ct);
        return result ?? throw new InvalidOperationException(
            "Civil cohort mutation produced no result.");
    }

    /// <summary>
    /// Called as part of a durable hive transaction; the ready cohort is
    /// delivered once on the next authenticated read or command, even if Unity
    /// was closed during maturation. No manual claim or client timer is used.
    /// </summary>
    internal static PlayerHiveState SettleMatureCohort(
        PlayerHiveState state, DateTimeOffset now)
    {
        CivilPopulationState? civil = state.CivilPopulation;
        CivilNurseryCohort? active = civil?.Active;
        if (civil is null || active is null || now < active.ReadyAtUtc)
            return state;
        return state with
        {
            Revision = checked(state.Revision + 1),
            CivilPopulation = civil with
            {
                Revision = checked(civil.Revision + 1),
                AvailableWorkers = checked(civil.AvailableWorkers + active.ExpectedWorkers),
                Active = null
            }
        };
    }

    private CivilPopulationSnapshot Snapshot(PlayerHiveState state, DateTimeOffset now)
    {
        CivilPopulationState civil = state.CivilPopulation ?? Empty();
        long capacity = Capacity(state);
        long occupied = checked(civil.AvailableWorkers + civil.AssignedWorkers
            + civil.RecoveringWorkers);
        bool canStart = state.BuildingLevels.GetValueOrDefault(NurseryKey) >= 1
            && civil.Active is null
            && occupied <= capacity
            && settings.WorkersPerCohort <= capacity - occupied
            && civil.Receipts.Count < MaximumReceipts
            && CanPay(state);
        string status = civil.Active is not null ? "incubating"
            : state.BuildingLevels.GetValueOrDefault(NurseryKey) < 1 ? "nursery_missing"
            : occupied > capacity || settings.WorkersPerCohort > capacity - occupied
                ? "capacity_full"
            : CanPay(state) ? "available" : "insufficient_resources";
        return new CivilPopulationSnapshot(
            state.PlayerId, state.HiveId, ContractVersion, state.Revision,
            civil.Revision, now, capacity, civil.AvailableWorkers,
            civil.AssignedWorkers, civil.RecoveringWorkers, civil.Active,
            canStart, status);
    }

    private static CivilPopulationState Empty() => new(
        0, 0, 0, 0, null, new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal));

    public static void ValidateStored(CivilPopulationState civil)
    {
        if (civil.Revision < 0 || civil.AvailableWorkers < 0
            || civil.AssignedWorkers < 0 || civil.RecoveringWorkers < 0
            || civil.Receipts is null || civil.Receipts.Count > MaximumReceipts
            || civil.Receipts.Any(r => !ValidKey(r.Key) || r.Value is null))
            throw new InvalidDataException("Invalid persisted civil population.");
        _ = checked(civil.AvailableWorkers + civil.AssignedWorkers + civil.RecoveringWorkers);
        if (civil.Active is not null
            && (civil.Active.CohortId == Guid.Empty
                || civil.Active.ExpectedWorkers < 1
                || civil.Active.HoneySpent < 0 || civil.Active.PollenSpent < 0
                || civil.Active.ReadyAtUtc <= civil.Active.StartedAtUtc))
            throw new InvalidDataException("Invalid persisted civil cohort.");
    }

    private static PlayerHiveState RequireScope(
        PlayerHiveState state, Guid playerId, Guid hiveId)
    {
        if (state.PlayerId != playerId || state.HiveId != hiveId)
            throw new UnauthorizedAccessException("game.hive_forbidden");
        return state;
    }

    private static void EnsureIdentity(Guid playerId, Guid hiveId)
    {
        if (playerId == Guid.Empty || hiveId == Guid.Empty)
            throw new ArgumentException("game.invalid_request");
    }

    private void EnsureEnabled()
    {
        settings.Validate();
        if (!settings.Enabled)
            throw new InvalidOperationException("game.civil_population_disabled");
    }

    private long Capacity(PlayerHiveState state) =>
        checked(Math.Max(0, state.BuildingLevels.GetValueOrDefault(NurseryKey))
            * settings.CapacityPerNurseryLevel);

    private bool CanPay(PlayerHiveState state) =>
        state.Resources.TryGetValue("honey", out ResourceBalance? honey)
        && honey.Amount >= settings.HoneyCost
        && state.Resources.TryGetValue("pollen", out ResourceBalance? pollen)
        && pollen.Amount >= settings.PollenCost;

    private static bool ValidKey(string? key) =>
        !string.IsNullOrWhiteSpace(key)
        && key.Length is >= 8 and <= 128
        && key.All(c => c is >= 'a' and <= 'z'
            or >= 'A' and <= 'Z'
            or >= '0' and <= '9' or '-' or '_' or '.' or ':');

    private static string Hash(string payload) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

    private CivilPopulationCommandResult Failure(
        PlayerHiveState state, DateTimeOffset now, string code) =>
        new(false, code, Snapshot(state, now));

    private DateTimeOffset Now() => clock.UtcNow.ToUniversalTime();
}
