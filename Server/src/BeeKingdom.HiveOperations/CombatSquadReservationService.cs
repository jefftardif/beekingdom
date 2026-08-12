using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed record SquadReservationSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, string CatalogVersion,
    long RosterRevision, long ReservationRevision, int Capacity, IReadOnlyDictionary<string,long> Roster,
    IReadOnlyDictionary<string,long> Available, IReadOnlyDictionary<string,long> Reserved, string? ReservationId);
public sealed record CommitSquadReservationCommand(Guid PlayerId, Guid HiveId, long ExpectedRevision, Dictionary<string,long> Quantities, string IdempotencyKey);
public sealed record ReleaseSquadReservationCommand(Guid PlayerId, Guid HiveId, long ExpectedRevision, string IdempotencyKey);
public sealed record SquadReservationResult(bool Succeeded, string Code, SquadReservationSnapshot Snapshot, SquadReservationReceipt? Receipt = null);
public sealed record SquadReservationReceipt(Guid PlayerId, Guid HiveId, string IdempotencyKey, string Action, string? ReservationId, IReadOnlyDictionary<string,long> Quantities, long ReservationRevisionBefore, long ReservationRevisionAfter, DateTimeOffset AcceptedAtUtc, string Code);
public sealed record SquadReservationResponse(SquadReservationReceipt Receipt, SquadReservationSnapshot Snapshot);

public sealed class CombatSquadReservationService
{
    public const string ContractVersion = "phase4-combat-squad-reservation-v1";
    public const int InitialCapacity = 12;
    public const int MaxCapacity = 1000;
    // Squad capacity grows with the player's military progression. No official player-level or
    // skill-tree bonus system is implemented server-side yet (Docs/Architecture/PlayerSkillTree_Progression_Spec.md
    // is still OFFICIAL_PROGRESSION_IMPLEMENTATION=NOT_RUN) — guard_post is the closest real,
    // already-upgradeable building gating this feature (see CombatRecruitmentService.StartAsync),
    // so it stands in for "level" for now. Swap/extend this formula once official leveling ships.
    public const int CapacityPerGuardPostLevel = 4;

    public static int ComputeCapacity(IReadOnlyDictionary<string, int> buildingLevels)
    {
        int guardPostLevel = Math.Max(0, buildingLevels?.GetValueOrDefault("guard_post") ?? 0);
        long capacity = InitialCapacity + (long)guardPostLevel * CapacityPerGuardPostLevel;
        return (int)Math.Min(MaxCapacity, capacity);
    }
    private static readonly string[] Families = ["guardians", "wingrunners", "darters"];
    private readonly IHiveStateRepository repository; private readonly IServerClock clock;
    public CombatSquadReservationService(IHiveStateRepository repository, IServerClock? clock = null) { this.repository = repository; this.clock = clock ?? new SystemServerClock(); }

    public async Task<SquadReservationSnapshot> ReadAsync(Guid player, Guid hive, CancellationToken ct)
        => Snapshot(await repository.ReadAsync(player, hive, ct) ?? throw new KeyNotFoundException());

    public async Task<SquadReservationResult> CommitAsync(CommitSquadReservationCommand c, CancellationToken ct)
    {
        if (c.ExpectedRevision < 0 || c.ExpectedRevision == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(c.ExpectedRevision));
        SquadReservationResult? result = null;
        await repository.ExecuteAtomicallyAsync(c.PlayerId, c.HiveId, state =>
        {
            var key = c.IdempotencyKey ?? string.Empty;
            var roster = state.DoctrineRoster ?? new DoctrineRosterState(0, new(), null, new());
            var reservation = state.SquadReservation ?? new SquadReservationState(0, InitialCapacity, new(), null, new());
            int capacity = ComputeCapacity(state.BuildingLevels);
            if (state.ImplicitBuildingDefaultsApplied && state.BuildingLevels.GetValueOrDefault("guard_post") == 1)
                capacity = InitialCapacity;
            var hash = Hash("commit|" + (c.Quantities is null ? "<null>" : Canonical(c.Quantities)) + "|" + c.ExpectedRevision);
            if (reservation.Receipts.TryGetValue(key, out var old)) { var qty = ParseQuantities(old.Answer); var receipt = new SquadReservationReceipt(c.PlayerId,c.HiveId,key,"commit",old.ResultingStep,qty,old.RevisionBefore ?? reservation.Revision,old.RevisionAfter ?? reservation.Revision,old.AcceptedAtUtc ?? old.CreatedAtUtc,old.Code); result = old.PayloadHash == hash && old.Succeeded ? new(true,old.Code,Snapshot(state),receipt) : new(false,"game.idempotency_conflict",Snapshot(state)); return state; }
            if (!ValidKey(c.IdempotencyKey) || c.Quantities is null || !ValidQuantities(c.Quantities, capacity))
            { result = new(false, "game.invalid_request", Snapshot(state)); return state; }
            if (reservation.ReservationId is not null || reservation.Revision != c.ExpectedRevision)
            { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            if (c.Quantities.Any(x => x.Value > roster.Counts.GetValueOrDefault(x.Key)))
            { result = new(false, "game.squad_over_reserved", Snapshot(state)); return state; }
            if (reservation.Revision == long.MaxValue) throw new InvalidDataException("reservation revision overflow");
            var id = Guid.NewGuid().ToString("N");
            var accepted = clock.UtcNow; var receipts = new Dictionary<string, IdempotencyReceipt>(reservation.Receipts) { [key] = new(hash, true, "game.squad_reserved", null, accepted, reservation.Revision, reservation.Revision + 1, "commit", id, Canonical(c.Quantities)) }; while(receipts.Count>128){var victim=receipts.OrderBy(x=>x.Value.CreatedAtUtc).ThenBy(x=>x.Key,StringComparer.Ordinal).First(x=>x.Key!=key).Key;receipts.Remove(victim);}
            var next = state with { Revision = state.Revision + 1, SquadReservation = reservation with { Revision = reservation.Revision + 1, Capacity = capacity, Reserved = new(c.Quantities), ReservationId = id, Receipts = receipts } };
            result = new(true, "game.squad_reserved", Snapshot(next), new(c.PlayerId,c.HiveId,key,"commit",id,new Dictionary<string,long>(c.Quantities),reservation.Revision,reservation.Revision+1,accepted,"game.squad_reserved")); return next;
        }, ct);
        return result!;
    }

    public async Task<SquadReservationResult> ReleaseAsync(ReleaseSquadReservationCommand c, CancellationToken ct)
    {
        if (c.ExpectedRevision < 0 || c.ExpectedRevision == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(c.ExpectedRevision));
        SquadReservationResult? result = null;
        await repository.ExecuteAtomicallyAsync(c.PlayerId, c.HiveId, state =>
        {
            if (state.HivePerimeterSortie?.Active is not null || state.CombatPatrol?.ActiveEncounters is { Count: > 0 })
            { result = new(false, "game.squad_in_use", Snapshot(state)); return state; }
            var key = c.IdempotencyKey ?? string.Empty;
            var reservation = state.SquadReservation ?? new SquadReservationState(0, InitialCapacity, new(), null, new());
            var hash = Hash("release|" + c.ExpectedRevision);
            if (reservation.Receipts.TryGetValue(key, out var old)) { var receipt = new SquadReservationReceipt(c.PlayerId,c.HiveId,key,"release",null,Families.ToDictionary(f=>f,_=>0L),old.RevisionBefore ?? reservation.Revision,old.RevisionAfter ?? reservation.Revision,old.AcceptedAtUtc ?? old.CreatedAtUtc,old.Code); result = old.PayloadHash == hash && old.Succeeded ? new(true,old.Code,Snapshot(state),receipt) : new(false,"game.idempotency_conflict",Snapshot(state)); return state; }
            if (!ValidKey(c.IdempotencyKey)) { result = new(false, "game.invalid_request", Snapshot(state)); return state; }
            if (reservation.Revision != c.ExpectedRevision || reservation.ReservationId is null) { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            if (reservation.Revision == long.MaxValue) throw new InvalidDataException("reservation revision overflow");
            var accepted = clock.UtcNow; var receipts = new Dictionary<string, IdempotencyReceipt>(reservation.Receipts) { [key] = new(hash, true, "game.squad_released", null, accepted, reservation.Revision, reservation.Revision + 1, "release", reservation.ReservationId ?? "") }; while(receipts.Count>128){var victim=receipts.OrderBy(x=>x.Value.CreatedAtUtc).ThenBy(x=>x.Key,StringComparer.Ordinal).First(x=>x.Key!=key).Key;receipts.Remove(victim);}
            var empty = Families.ToDictionary(f => f, _ => 0L, StringComparer.Ordinal);
            var next = state with { Revision = state.Revision + 1, SquadReservation = reservation with { Revision = reservation.Revision + 1, Reserved = empty, ReservationId = null, Receipts = receipts } };
            result = new(true, "game.squad_released", Snapshot(next), new(c.PlayerId,c.HiveId,key,"release",null,Families.ToDictionary(f=>f,_=>0L),reservation.Revision,reservation.Revision+1,accepted,"game.squad_released")); return next;
        }, ct);
        return result!;
    }

    private SquadReservationSnapshot Snapshot(PlayerHiveState s)
    {
        var r = s.DoctrineRoster ?? new DoctrineRosterState(0, new(), null, new());
        var q = s.SquadReservation ?? new SquadReservationState(0, InitialCapacity, new(), null, new());
        var roster = Families.ToDictionary(x => x, x => r.Counts.GetValueOrDefault(x));
        var reserved = Families.ToDictionary(x => x, x => q.Reserved.GetValueOrDefault(x));
        var available = Families.ToDictionary(x => x, x => Math.Max(0, roster[x] - reserved[x]));
        return new(s.PlayerId, s.HiveId, ContractVersion, CombatRecruitmentService.CatalogVersion, r.Revision, q.Revision, ComputeCapacity(s.BuildingLevels), roster, available, reserved, q.ReservationId);
    }
    private static bool ValidKey(string? key) => !string.IsNullOrWhiteSpace(key) && key.Length <= 256;
    private const int MaxReceipts = 128;
    private const long MaxQuantity = 1_000_000;
    private static bool ValidQuantities(Dictionary<string,long>? q, int capacity)
    {
        if (q is null || q.Count != 3 || q.Keys.Any(k => !Families.Contains(k)) || q.Values.Any(x => x < 0 || x > MaxQuantity)) return false;
        try { var total = checked(q.Values.Sum()); return total > 0 && total <= capacity; } catch (OverflowException) { return false; }
    }
    private static string Canonical(Dictionary<string,long> q) => string.Join(";", Families.Select(f => f + "=" + q.GetValueOrDefault(f)));
    private static Dictionary<string,long> ParseQuantities(string? value) { var q=Families.ToDictionary(f=>f,_=>0L,StringComparer.Ordinal); if(string.IsNullOrWhiteSpace(value)) return q; foreach(var part in value.Split(';')){var bits=part.Split('=');if(bits.Length==2&&long.TryParse(bits[1],out var n)&&q.ContainsKey(bits[0]))q[bits[0]]=n;} return q; }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
