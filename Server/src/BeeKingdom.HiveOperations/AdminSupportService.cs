using System.Globalization;

namespace BeeKingdom.HiveOperations;

// Internal support/troubleshooting tool for Jeff: view a player's hive state and manually
// correct it (refund a resource, restore lost troops, grant a compensation slot) when a bug
// requires it. Deliberately simple concurrency model — every mutation requires the caller's
// last-known revision (optimistic concurrency), so an accidental double-submit fails cleanly
// with a conflict instead of double-applying; no separate idempotency-key ledger, since this is
// a human clicking a button in a browser, not an unreliable mobile client with an offline outbox.
public sealed record AdjustResourceCommand(Guid PlayerId, Guid HiveId, string Resource, long Delta, string Reason, long ExpectedRevision);
public sealed record AdjustRosterCommand(Guid PlayerId, Guid HiveId, string Family, long Delta, string Reason, long ExpectedRevision);
public sealed record GrantCombatPatrolSlotCommand(Guid PlayerId, Guid HiveId, bool Premium, string Reason, long ExpectedRevision);
// Octroi manuel de jetons de rappel (demande de Jeff, 2026-08-26) : seul moyen d'en obtenir pour
// l'instant en l'absence de boutique/systeme de quetes branche - voir CombatPatrolService.RecallItemId.
public sealed record AdjustRecallTokensCommand(Guid PlayerId, Guid HiveId, long Delta, string Reason, long ExpectedRevision);
public sealed record AdminMutationResult(bool Succeeded, string Code, PlayerHiveState State);
public sealed record AdminDiagnostics(
    Guid PlayerId, Guid HiveId, long Revision,
    IReadOnlyDictionary<string, ResourceBalance> Resources,
    IReadOnlyDictionary<string, long> Roster,
    IReadOnlyDictionary<string, int> BuildingLevels,
    int CombatPatrolActiveCount, int CombatPatrolTotalSlots,
    int CombatPatrolResourcePurchasedSlots, int CombatPatrolPremiumPurchasedSlots,
    IReadOnlyList<AdminAuditEntry> AdminAudit);

public sealed class AdminSupportService
{
    private const int MaxAuditEntries = 4096;
    private static readonly string[] Families = ["guardians", "wingrunners", "darters"];
    private readonly IHiveStateRepository repository;
    private readonly IServerClock clock;

    public AdminSupportService(IHiveStateRepository repository, IServerClock clock)
    {
        this.repository = repository;
        this.clock = clock;
    }

    public async Task<AdminDiagnostics> ReadDiagnosticsAsync(Guid playerId, Guid hiveId, CancellationToken ct)
    {
        PlayerHiveState state = await repository.ReadAsync(playerId, hiveId, ct) ?? throw new KeyNotFoundException();
        DoctrineRosterState roster = state.DoctrineRoster ?? new DoctrineRosterState(0, new(), null, new());
        CombatPatrolState patrol = state.CombatPatrol ?? new CombatPatrolState(0, new List<CombatPatrolActiveEncounter>(), new(), new());
        int totalSlots = Math.Min(CombatPatrolCatalog.MaxConcurrentSlots, 1 + patrol.ResourcePurchasedSlots + patrol.PremiumPurchasedSlots);
        return new(
            state.PlayerId, state.HiveId, state.Revision,
            state.Resources, roster.Counts, state.BuildingLevels,
            patrol.ActiveEncounters.Count, totalSlots, patrol.ResourcePurchasedSlots, patrol.PremiumPurchasedSlots,
            state.AdminAudit ?? new List<AdminAuditEntry>());
    }

    public async Task<AdminMutationResult> AdjustResourceAsync(AdjustResourceCommand command, CancellationToken ct)
    {
        RequireRevision(command.ExpectedRevision);
        RequireReason(command.Reason);
        AdminMutationResult? result = null;
        await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
        {
            if (state.Revision != command.ExpectedRevision)
            { result = new(false, "game.revision_conflict", state); return state; }
            if (!state.Resources.TryGetValue(command.Resource, out ResourceBalance? balance))
            { result = new(false, "game.invalid_request", state); return state; }
            long nextAmount = Math.Max(0, Math.Min(balance.Capacity, balance.Amount + command.Delta));
            var resources = new Dictionary<string, ResourceBalance>(state.Resources, StringComparer.Ordinal) { [command.Resource] = balance with { Amount = nextAmount } };
            string details = command.Resource + ": " + balance.Amount.ToString(CultureInfo.InvariantCulture) + " -> " + nextAmount.ToString(CultureInfo.InvariantCulture) + " (delta " + command.Delta.ToString(CultureInfo.InvariantCulture) + ")";
            PlayerHiveState next = AppendAudit(state with { Revision = checked(state.Revision + 1), Resources = resources }, "resource_adjust", details, command.Reason);
            result = new(true, "game.admin_resource_adjusted", next);
            return next;
        }, ct);
        return result!;
    }

    public async Task<AdminMutationResult> AdjustRosterAsync(AdjustRosterCommand command, CancellationToken ct)
    {
        RequireRevision(command.ExpectedRevision);
        RequireReason(command.Reason);
        if (!Families.Contains(command.Family)) throw new ArgumentException("game.invalid_request");
        AdminMutationResult? result = null;
        await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
        {
            if (state.Revision != command.ExpectedRevision)
            { result = new(false, "game.revision_conflict", state); return state; }
            DoctrineRosterState roster = state.DoctrineRoster ?? new DoctrineRosterState(0, new(), null, new());
            var counts = new Dictionary<string, long>(roster.Counts, StringComparer.Ordinal);
            long before = counts.GetValueOrDefault(command.Family);
            long after = Math.Max(0, before + command.Delta);
            counts[command.Family] = after;
            string details = command.Family + ": " + before.ToString(CultureInfo.InvariantCulture) + " -> " + after.ToString(CultureInfo.InvariantCulture) + " (delta " + command.Delta.ToString(CultureInfo.InvariantCulture) + ")";
            PlayerHiveState next = AppendAudit(state with { Revision = checked(state.Revision + 1), DoctrineRoster = roster with { Counts = counts } }, "roster_adjust", details, command.Reason);
            result = new(true, "game.admin_roster_adjusted", next);
            return next;
        }, ct);
        return result!;
    }

    public async Task<AdminMutationResult> GrantCombatPatrolSlotAsync(GrantCombatPatrolSlotCommand command, CancellationToken ct)
    {
        RequireRevision(command.ExpectedRevision);
        RequireReason(command.Reason);
        AdminMutationResult? result = null;
        await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
        {
            if (state.Revision != command.ExpectedRevision)
            { result = new(false, "game.revision_conflict", state); return state; }
            CombatPatrolState patrol = state.CombatPatrol ?? new CombatPatrolState(0, new List<CombatPatrolActiveEncounter>(), new(), new());
            int current = command.Premium ? patrol.PremiumPurchasedSlots : patrol.ResourcePurchasedSlots;
            int max = command.Premium ? CombatPatrolCatalog.MaxPremiumPurchasedSlots : CombatPatrolCatalog.MaxResourcePurchasedSlots;
            if (current >= max)
            { result = new(false, "game.patrol_slot_limit_reached", state); return state; }
            CombatPatrolState nextPatrol = command.Premium
                ? patrol with { PremiumPurchasedSlots = patrol.PremiumPurchasedSlots + 1 }
                : patrol with { ResourcePurchasedSlots = patrol.ResourcePurchasedSlots + 1 };
            string details = (command.Premium ? "premium" : "resource") + " combat patrol slot granted (" + (current + 1) + ")";
            PlayerHiveState next = AppendAudit(state with { Revision = checked(state.Revision + 1), CombatPatrol = nextPatrol }, "combat_patrol_slot_grant", details, command.Reason);
            result = new(true, "game.admin_slot_granted", next);
            return next;
        }, ct);
        return result!;
    }

    public async Task<AdminMutationResult> AdjustRecallTokensAsync(AdjustRecallTokensCommand command, CancellationToken ct)
    {
        RequireRevision(command.ExpectedRevision);
        RequireReason(command.Reason);
        AdminMutationResult? result = null;
        await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
        {
            if (state.Revision != command.ExpectedRevision)
            { result = new(false, "game.revision_conflict", state); return state; }
            var items = new Dictionary<string, int>(state.SpeedUps ?? new Dictionary<string, int>(StringComparer.Ordinal), StringComparer.Ordinal);
            long before = items.GetValueOrDefault(CombatPatrolService.RecallItemId);
            long after = Math.Max(0, before + command.Delta);
            items[CombatPatrolService.RecallItemId] = (int)Math.Min(after, int.MaxValue);
            string details = "recall tokens: " + before.ToString(CultureInfo.InvariantCulture) + " -> " + after.ToString(CultureInfo.InvariantCulture) + " (delta " + command.Delta.ToString(CultureInfo.InvariantCulture) + ")";
            PlayerHiveState next = AppendAudit(state with { Revision = checked(state.Revision + 1), SpeedUps = items }, "combat_recall_tokens_adjust", details, command.Reason);
            result = new(true, "game.admin_recall_tokens_adjusted", next);
            return next;
        }, ct);
        return result!;
    }

    private PlayerHiveState AppendAudit(PlayerHiveState state, string action, string details, string reason)
    {
        var entry = new AdminAuditEntry(Guid.NewGuid(), RequireUtc(clock.UtcNow), action, details, reason);
        var audit = new List<AdminAuditEntry>(state.AdminAudit ?? new List<AdminAuditEntry>()) { entry };
        while (audit.Count > MaxAuditEntries) audit.RemoveAt(0);
        return state with { AdminAudit = audit };
    }

    private static void RequireRevision(long revision) { if (revision < 0 || revision == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(revision)); }
    private static void RequireReason(string reason) { if (string.IsNullOrWhiteSpace(reason) || reason.Length > 512) throw new ArgumentException("game.invalid_request"); }
    private static DateTimeOffset RequireUtc(DateTimeOffset value) => value.Offset == TimeSpan.Zero ? value : value.ToUniversalTime();
}
