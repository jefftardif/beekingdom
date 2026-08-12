using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed record HivePerimeterSignalDefinition(string SignalKey, string HazardDoctrine, TimeSpan Duration, int MinimumSquad, long HoneyReward, long PollenReward);
public sealed record HivePerimeterSignalReadModel(string SignalKey, string SignalInstanceId, string HazardDoctrine, TimeSpan Duration, int MinimumSquad, long HoneyReward, long PollenReward, bool Completed, bool CanLaunch);
public sealed record HivePerimeterSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, long Revision, DateTimeOffset ServerTimeUtc, DateTimeOffset CycleStartedAtUtc, DateTimeOffset CycleEndsAtUtc, HivePerimeterActiveSortie? Active, SquadReservationSnapshot Reservation, IReadOnlyList<HivePerimeterSignalReadModel> Signals, HivePerimeterClaimReceipt? ClaimReceipt = null);
public sealed record LaunchHivePerimeterSortieCommand(Guid PlayerId, Guid HiveId, string SignalKey, string SignalInstanceId, string ReservationId, long ExpectedRevision, string IdempotencyKey);
public sealed record ClaimHivePerimeterSortieCommand(Guid PlayerId, Guid HiveId, Guid SortieId, long ExpectedRevision, string IdempotencyKey);
public sealed record RecallHivePerimeterSortieCommand(Guid PlayerId, Guid HiveId, Guid SortieId, long ExpectedRevision, string IdempotencyKey);
public sealed record HivePerimeterResult(bool Succeeded, string Code, HivePerimeterSnapshot Snapshot, HivePerimeterClaimReceipt? ClaimReceipt = null, HivePerimeterPublicReceipt? Receipt = null);
public sealed record HivePerimeterPublicReceipt(Guid PlayerId, Guid HiveId, string IdempotencyKey, string Action, Guid SortieId, string SignalKey, string SignalInstanceId, string? ReservationId, DateTimeOffset CycleStartedAtUtc, DateTimeOffset CycleEndsAtUtc, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, IReadOnlyDictionary<string,long> CreditedByResource, IReadOnlyDictionary<string,ResourceBalance> ResultingBalances, string Code);
public sealed record HivePerimeterMutationResponse(HivePerimeterPublicReceipt Receipt, HivePerimeterSnapshot Snapshot);
public sealed record HivePerimeterResponse(HivePerimeterPublicReceipt Receipt, HivePerimeterSnapshot Snapshot);

public sealed class HivePerimeterSortieService
{
    public const string ContractVersion = "phase5-hive-perimeter-sortie-v1";
    private const int MaxReceipts = 128;
    private static readonly string[] Families = ["guardians", "wingrunners", "darters"];
    public static readonly IReadOnlyDictionary<string, HivePerimeterSignalDefinition> Catalog = new Dictionary<string, HivePerimeterSignalDefinition>(StringComparer.Ordinal)
    {
        ["foraging_scout"] = new("foraging_scout", "wingrunners", TimeSpan.FromSeconds(16), 1, 40, 20),
        ["brood_watch"] = new("brood_watch", "guardians", TimeSpan.FromSeconds(20), 2, 25, 35)
    };
    private readonly IHiveStateRepository repository;
    private readonly IServerClock clock;
    public HivePerimeterSortieService(IHiveStateRepository repository, IServerClock clock) { this.repository = repository; this.clock = clock; }

    public async Task<HivePerimeterSnapshot> ReadAsync(Guid player, Guid hive, CancellationToken ct)
    {
        var state = await repository.ReadAsync(player, hive, ct) ?? throw new KeyNotFoundException();
        return Snapshot(state with { HivePerimeterSortie = CurrentCycle(state.HivePerimeterSortie, RequireUtc(clock.UtcNow)) });
    }

    public async Task<HivePerimeterResult> LaunchAsync(LaunchHivePerimeterSortieCommand command, CancellationToken ct)
    {
        if (command.ExpectedRevision < 0 || command.ExpectedRevision == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(command.ExpectedRevision));
        HivePerimeterResult? result = null;
        await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
        {
            var now = RequireUtc(clock.UtcNow);
            var cycle = CurrentCycle(state.HivePerimeterSortie, now);
            var key = command.IdempotencyKey ?? string.Empty;
            var hash = Hash($"launch|{command.SignalKey}|{command.SignalInstanceId}|{command.ReservationId}|{command.ExpectedRevision}");
            if (cycle.Receipts.TryGetValue(key, out var old))
            {
                if (old.PayloadHash != hash) { result = new(false, "game.idempotency_conflict", Snapshot(state)); return state; }
                var replay = new HivePerimeterPublicReceipt(command.PlayerId, command.HiveId, key, "launch", old.OperationId ?? Guid.Empty,
                    old.PreviousStep ?? string.Empty, old.ResultingStep ?? string.Empty, old.Answer, cycle.CycleStartedAtUtc, cycle.CycleEndsAtUtc,
                    old.RevisionBefore ?? cycle.Revision, old.RevisionAfter ?? cycle.Revision,
                    old.AcceptedAtUtc ?? old.CreatedAtUtc, new Dictionary<string,long>(), new Dictionary<string,ResourceBalance>(), old.Code);
                result = new(old.Succeeded, old.Code, Snapshot(state), null, replay); return state;
            }
            if (!ValidKey(command.IdempotencyKey) || !Catalog.TryGetValue(command.SignalKey, out var signal) || !ValidKey(command.ReservationId) || !ValidKey(command.SignalInstanceId) || command.SignalInstanceId != InstanceId(command.PlayerId, command.HiveId, cycle.CycleStartedAtUtc, command.SignalKey))
            { result = new(false, "game.invalid_request", Snapshot(state)); return state; }
            if (cycle.Active is not null || cycle.Revision != command.ExpectedRevision || cycle.CycleEndsAtUtc <= now)
            { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            var reservation = state.SquadReservation;
            if (reservation is null || !string.Equals(reservation.ReservationId, command.ReservationId, StringComparison.Ordinal) || ReservedTotal(reservation) < signal.MinimumSquad)
            { result = new(false, "game.perimeter_precondition_failed", Snapshot(state)); return state; }
            if (cycle.Revision == long.MaxValue) throw new InvalidDataException("perimeter revision overflow");
            if ((cycle.CompletedSignalKeys ?? []).Contains(signal.SignalKey)) { result = new(false, "game.perimeter_signal_completed", Snapshot(state)); return state; }
            var op = new HivePerimeterActiveSortie(Guid.NewGuid(), signal.SignalKey, command.SignalInstanceId, command.ReservationId, now, now.Add(signal.Duration), cycle.Revision + 1, command.IdempotencyKey!, hash);
            var receipts = new Dictionary<string, IdempotencyReceipt>(cycle.Receipts, StringComparer.Ordinal) { [key] = new(hash, true, "game.perimeter_launched", op.SortieId, now, cycle.Revision, cycle.Revision + 1, op.SignalKey, op.SignalInstanceId, op.ReservationId, now) };
            var next = state with { Revision = checked(state.Revision + 1), HivePerimeterSortie = cycle with { Revision = cycle.Revision + 1, Active = op, Receipts = receipts } };
            result = new(true, "game.perimeter_launched", Snapshot(next), null, new(command.PlayerId,command.HiveId,command.IdempotencyKey!,"launch",op.SortieId,op.SignalKey,op.SignalInstanceId,op.ReservationId,cycle.CycleStartedAtUtc,cycle.CycleEndsAtUtc,cycle.Revision,cycle.Revision+1,now,new Dictionary<string,long>(),new Dictionary<string,ResourceBalance>(),"game.perimeter_launched")); return next;
        }, ct);
        return result!;
    }

    public Task<HivePerimeterResult> ClaimAsync(ClaimHivePerimeterSortieCommand command, CancellationToken ct) => FinishAsync(command.PlayerId, command.HiveId, command.SortieId, command.ExpectedRevision, command.IdempotencyKey, true, ct);
    public Task<HivePerimeterResult> RecallAsync(RecallHivePerimeterSortieCommand command, CancellationToken ct) => FinishAsync(command.PlayerId, command.HiveId, command.SortieId, command.ExpectedRevision, command.IdempotencyKey, false, ct);

    private async Task<HivePerimeterResult> FinishAsync(Guid player, Guid hive, Guid sortieId, long expectedRevision, string? idempotencyKey, bool reward, CancellationToken ct)
    {
        if (expectedRevision < 0 || expectedRevision == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(expectedRevision));
        HivePerimeterResult? result = null;
        await repository.ExecuteAtomicallyAsync(player, hive, state =>
        {
            var now = RequireUtc(clock.UtcNow);
            var cycle = CurrentCycle(state.HivePerimeterSortie, now);
            var key = idempotencyKey ?? string.Empty;
            var hash = Hash($"{(reward ? "claim" : "recall")}|{sortieId:N}|{expectedRevision}");
            if (cycle.Receipts.TryGetValue(key, out var old))
            {
                var storedClaim = cycle.ClaimReceipts?.GetValueOrDefault(key);
                if (old.PayloadHash != hash) { result = new(false, "game.idempotency_conflict", Snapshot(state, storedClaim), storedClaim); return state; }
                var replay = new HivePerimeterPublicReceipt(player, hive, key, reward ? "claim" : "recall", old.OperationId ?? sortieId,
                    old.PreviousStep ?? storedClaim?.SignalKey ?? string.Empty, old.ResultingStep ?? storedClaim?.SignalInstanceId ?? string.Empty,
                    old.Answer ?? storedClaim?.SortieId.ToString(), storedClaim?.CycleStartedAtUtc ?? cycle.CycleStartedAtUtc, storedClaim?.CycleEndsAtUtc ?? cycle.CycleEndsAtUtc, old.RevisionBefore ?? cycle.Revision, old.RevisionAfter ?? cycle.Revision,
                    old.AcceptedAtUtc ?? old.CreatedAtUtc, storedClaim?.CreditedByResource ?? new Dictionary<string,long>(),
                    storedClaim?.ResultingBalances ?? new Dictionary<string,ResourceBalance>(), old.Code);
                result = new(old.Succeeded, old.Code, Snapshot(state, storedClaim), storedClaim, replay);
                return state;
            }
            if (!ValidKey(idempotencyKey) || sortieId == Guid.Empty)
            { result = new(false, "game.invalid_request", Snapshot(state)); return state; }
            var active = cycle.Active;
            if (active is null || active.SortieId != sortieId || cycle.Revision != expectedRevision)
            { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            if (reward && now < active.EndsAtUtc)
            { result = new(false, "game.perimeter_not_complete", Snapshot(state)); return state; }
            if (cycle.Revision == long.MaxValue) throw new InvalidDataException("perimeter revision overflow");
            var nextResources = new Dictionary<string, ResourceBalance>(state.Resources, StringComparer.Ordinal);
            var credited = new Dictionary<string, long>(StringComparer.Ordinal);
            if (reward)
            {
                var signal = Catalog[active.SignalKey];
                credited["honey"] = ApplyReward(nextResources, "honey", signal.HoneyReward);
                credited["pollen"] = ApplyReward(nextResources, "pollen", signal.PollenReward);
            }
            var empty = Families.ToDictionary(f => f, _ => 0L, StringComparer.Ordinal);
            var reservation = state.SquadReservation;
            var released = reservation is null ? null : reservation with { Revision = reservation.Revision + 1, Reserved = empty, ReservationId = null };
            if (state.SquadReservation is null || state.SquadReservation.ReservationId != active.ReservationId) { result = new(false, "game.perimeter_conflict", Snapshot(state)); return state; }
            var receipts = new Dictionary<string, IdempotencyReceipt>(cycle.Receipts, StringComparer.Ordinal) { [key] = new(hash, true, reward ? "game.perimeter_claimed" : "game.perimeter_recalled", sortieId, now, cycle.Revision, cycle.Revision + 1, active.SignalKey, active.SignalInstanceId, active.ReservationId, now) };
            var completed = new HashSet<string>(cycle.CompletedSignalKeys ?? [], StringComparer.Ordinal); if (reward) completed.Add(active.SignalKey);
            HivePerimeterClaimReceipt? claimReceipt = reward
                ? new(player, hive, sortieId, active.SignalKey, active.SignalInstanceId, cycle.CycleStartedAtUtc, cycle.CycleEndsAtUtc, cycle.Revision + 1, now, credited, nextResources.Where(x => x.Key is "honey" or "pollen").ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal))
                : null;
            var claimReceipts = new Dictionary<string, HivePerimeterClaimReceipt>(cycle.ClaimReceipts ?? new(StringComparer.Ordinal), StringComparer.Ordinal);
            if (claimReceipt is not null) claimReceipts[key] = claimReceipt;
            var next = state with { Revision = checked(state.Revision + 1), Resources = nextResources, SquadReservation = released, HivePerimeterSortie = cycle with { Revision = cycle.Revision + 1, Active = null, Receipts = receipts, CompletedSignalKeys = completed, ClaimReceipts = claimReceipts } };
            result = new(true, reward ? "game.perimeter_claimed" : "game.perimeter_recalled", Snapshot(next, claimReceipt), claimReceipt, new(player,hive,key,reward?"claim":"recall",sortieId,active.SignalKey,active.SignalInstanceId,active.ReservationId,cycle.CycleStartedAtUtc,cycle.CycleEndsAtUtc,cycle.Revision,cycle.Revision+1,now,credited,nextResources.Where(x=>x.Key is "honey" or "pollen").ToDictionary(x=>x.Key,x=>x.Value),reward?"game.perimeter_claimed":"game.perimeter_recalled")); return next;
        }, ct);
        return result!;
    }

    private HivePerimeterSnapshot Snapshot(PlayerHiveState state, HivePerimeterClaimReceipt? claimReceipt = null)
    {
        var now = RequireUtc(clock.UtcNow);
        var cycle = state.HivePerimeterSortie ?? NewState(now);
        var roster = state.DoctrineRoster?.Counts ?? new Dictionary<string, long>();
        var reservationState = state.SquadReservation;
        var reserved = Families.ToDictionary(f => f, f => reservationState?.Reserved.GetValueOrDefault(f) ?? 0L, StringComparer.Ordinal);
        var rosterView = Families.ToDictionary(f => f, f => roster.GetValueOrDefault(f), StringComparer.Ordinal);
        var available = Families.ToDictionary(f => f, f => Math.Max(0, rosterView[f] - reserved[f]), StringComparer.Ordinal);
        var reservation = new SquadReservationSnapshot(state.PlayerId, state.HiveId, CombatSquadReservationService.ContractVersion, CombatRecruitmentService.CatalogVersion, state.DoctrineRoster?.Revision ?? 0, reservationState?.Revision ?? 0, reservationState?.Capacity ?? CombatSquadReservationService.InitialCapacity, rosterView, available, reserved, reservationState?.ReservationId);
        var completed = cycle.CompletedSignalKeys ?? new HashSet<string>(StringComparer.Ordinal);
        var signals = Catalog.Values.Select(s => new HivePerimeterSignalReadModel(s.SignalKey, InstanceId(state.PlayerId, state.HiveId, cycle.CycleStartedAtUtc, s.SignalKey), s.HazardDoctrine, s.Duration, s.MinimumSquad, s.HoneyReward, s.PollenReward, completed.Contains(s.SignalKey), cycle.Active is null && !completed.Contains(s.SignalKey))).ToArray();
        return new(state.PlayerId, state.HiveId, ContractVersion, cycle.Revision, now, cycle.CycleStartedAtUtc, cycle.CycleEndsAtUtc, cycle.Active, reservation, signals, claimReceipt);
    }

    private static HivePerimeterSortieState CurrentCycle(HivePerimeterSortieState? state, DateTimeOffset now)
    {
        var cycle = state ?? NewState(now);
        return cycle.CycleEndsAtUtc <= now && cycle.Active is null ? NewState(now) with
        {
            Receipts = new Dictionary<string, IdempotencyReceipt>(cycle.Receipts, StringComparer.Ordinal),
            ClaimReceipts = new Dictionary<string, HivePerimeterClaimReceipt>(cycle.ClaimReceipts ?? new(StringComparer.Ordinal), StringComparer.Ordinal)
        } : cycle;
    }
    private static HivePerimeterSortieState NewState(DateTimeOffset now)
    {
        var utc = RequireUtc(now); var start = new DateTimeOffset(utc.UtcDateTime.Date.AddHours((utc.Hour / 8) * 8), TimeSpan.Zero);
        return new(0, start, start.AddHours(8), null, new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));
    }
    private static DateTimeOffset RequireUtc(DateTimeOffset value) => value.Offset == TimeSpan.Zero ? value : value.ToUniversalTime();
    private static bool ValidKey(string? key) => !string.IsNullOrWhiteSpace(key) && key.Length <= 256;
    private static long ReservedTotal(SquadReservationState reservation) => checked(reservation.Reserved.Values.Aggregate(0L, (sum, value) => checked(sum + value)));
    private static long ApplyReward(Dictionary<string, ResourceBalance> resources, string key, long amount)
    {
        if (!resources.TryGetValue(key, out var balance) || amount < 0 || balance.Amount < 0 || balance.Capacity < balance.Amount) return 0;
        var credited = Math.Min(amount, balance.Capacity - balance.Amount);
        resources[key] = balance with { Amount = balance.Amount + credited }; return credited;
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static string InstanceId(Guid player, Guid hive, DateTimeOffset cycleStart, string signal) => Hash($"instance|{player:N}|{hive:N}|{cycleStart.UtcDateTime:O}|{signal}")[..32].ToLowerInvariant();
}
