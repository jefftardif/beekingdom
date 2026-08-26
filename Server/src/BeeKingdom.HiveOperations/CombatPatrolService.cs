using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed record PreviewCombatPatrolQuery(Guid PlayerId, Guid HiveId, int Tier, long Guardians, long Wingrunners, long Darters);
public sealed record CombatPatrolPreview(int Tier, string EnemyName, string HazardFamily, bool CanLaunch, string? BlockReason, long ReadinessBp, long AvailablePower, long RequiredPower, bool CooldownActive, DateTimeOffset? CooldownEndsAtUtc, bool IsDailyFocus = false, bool IsWorldEventBoosted = false);
public sealed record LaunchCombatPatrolCommand(Guid PlayerId, Guid HiveId, int Tier, long Guardians, long Wingrunners, long Darters, long ExpectedRevision, string IdempotencyKey);
public sealed record ClaimCombatPatrolCommand(Guid PlayerId, Guid HiveId, Guid EncounterId, long ExpectedRevision, string IdempotencyKey);
public sealed record RecallCombatPatrolCommand(Guid PlayerId, Guid HiveId, Guid EncounterId, long ExpectedRevision, string IdempotencyKey);
public sealed record PurchaseCombatPatrolResourceSlotCommand(Guid PlayerId, Guid HiveId, long ExpectedRevision, string IdempotencyKey);
public sealed record GrantCombatPatrolPremiumSlotCommand(Guid PlayerId, Guid HiveId, long ExpectedRevision, string IdempotencyKey);
public sealed record CombatPatrolSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, long Revision, DateTimeOffset ServerTimeUtc, IReadOnlyList<CombatPatrolActiveEncounter> ActiveEncounters, IReadOnlyDictionary<int, DateTimeOffset> TierCooldownEndsAtUtc, IReadOnlyList<CombatPatrolRecoveringBatch> Recovering, IReadOnlyDictionary<string, long> AvailableRoster, int Capacity, int TotalSlots, int ResourcePurchasedSlots, int PremiumPurchasedSlots, (long Honey, long Pollen)? NextResourceSlotCost, CombatPatrolClaimReceipt? ClaimReceipt = null, int FeaturedTier = 0, ActiveWorldEvent? WorldEvent = null, int? WorldEventFeaturedTier = null);
public sealed record CombatPatrolResult(bool Succeeded, string Code, CombatPatrolSnapshot Snapshot, CombatPatrolClaimReceipt? ClaimReceipt = null);

public sealed class CombatPatrolService
{
    public const string ContractVersion = "phase-combat-patrol-v2";
    private const int MaxReceipts = 128;
    private static readonly string[] Families = ["guardians", "wingrunners", "darters"];
    private readonly IHiveStateRepository repository;
    private readonly IServerClock clock;
    public CombatPatrolService(IHiveStateRepository repository, IServerClock clock) { this.repository = repository; this.clock = clock; }

    public async Task<CombatPatrolSnapshot> ReadAsync(Guid player, Guid hive, CancellationToken ct)
        => Snapshot(await ReadMaturedAsync(player, hive, ct));

    public async Task<CombatPatrolPreview> PreviewAsync(PreviewCombatPatrolQuery query, CancellationToken ct)
    {
        if (!CombatPatrolCatalog.TryGet(query.Tier, out BestiaryTierDefinition tier)) throw new ArgumentException("game.invalid_tier");
        PlayerHiveState state = await ReadMaturedAsync(query.PlayerId, query.HiveId, ct);
        CombatPatrolState patrol = state.CombatPatrol ?? EmptyPatrol();
        Dictionary<string, long> requested = Quantities(query.Guardians, query.Wingrunners, query.Darters);
        ChampionCombatContribution championContribution = ChampionBeeCatalog.CombatContribution(state.ChampionBees);
        TroopTierCombatContribution troopTierContribution = TroopTierCatalog.CombatContribution(state.TroopTierProgress);
        IReadOnlyDictionary<string, long> strategicPathBonus = StrategicPathBonusCatalog.CombatPowerBonusBpByFamily(state.StrategicPath?.SelectedPath);
        long availablePower = CombatPatrolResolution.ComputeAvailablePower(requested, tier.HazardFamily, MergedPowerBonus(championContribution.PowerBonusBpByFamily, troopTierContribution.PowerBonusBpByFamily, strategicPathBonus));
        long readinessBp = CombatPatrolResolution.ComputeReadinessBp(availablePower, tier.RequiredPower);
        bool meetsPower = CombatPatrolResolution.CanLaunch(readinessBp);
        DateTimeOffset now = RequireUtc(clock.UtcNow);
        bool hasCooldown = patrol.TierCooldownEndsAtUtc.TryGetValue(query.Tier, out DateTimeOffset cooldownEndsAtUtc);
        bool cooldownActive = hasCooldown && cooldownEndsAtUtc > now;
        int capacity = CombatSquadReservationService.ComputeCapacity(state.BuildingLevels);
        long totalRequested = requested.Values.Sum();
        IReadOnlyDictionary<string, long> availableRoster = ComputeAvailableRoster(state);
        bool hasSlot = patrol.ActiveEncounters.Count < TotalSlots(patrol);
        IReadOnlyDictionary<string, long> reservedForPreview = state.SquadReservation?.Reserved;
        bool isReservedSquadForPreview = reservedForPreview != null && Families.All(f => requested.GetValueOrDefault(f) <= reservedForPreview.GetValueOrDefault(f)) && requested.Values.Sum() > 0 && requested.Values.Sum() <= reservedForPreview.Values.Sum();
        string? blockReason = !hasSlot
            ? "game.patrol_no_slot_available"
            : cooldownActive
                ? "game.patrol_cooldown_active"
                : totalRequested <= 0 || totalRequested > capacity
                    ? "game.patrol_invalid_composition"
                    : !isReservedSquadForPreview && Families.Any(f => requested.GetValueOrDefault(f) > availableRoster.GetValueOrDefault(f))
                        ? "game.patrol_insufficient_troops"
                        : !meetsPower
                            ? "game.patrol_underpowered"
                            : null;
        bool isDailyFocus = tier.Tier == DailyFocusCatalog.FeaturedCombatTier(now);
        int? worldEventFeaturedTier = WorldEventFeaturedTier(WorldEventCatalog.Active(now), now);
        bool isWorldEventBoosted = worldEventFeaturedTier == tier.Tier;
        return new(tier.Tier, tier.EnemyName, tier.HazardFamily, blockReason is null, blockReason, readinessBp, availablePower, tier.RequiredPower, cooldownActive, cooldownActive ? cooldownEndsAtUtc : null, isDailyFocus, isWorldEventBoosted);
    }

    public async Task<CombatPatrolResult> LaunchAsync(LaunchCombatPatrolCommand command, CancellationToken ct)
    {
        if (command.ExpectedRevision < 0 || command.ExpectedRevision == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(command.ExpectedRevision));
        if (!CombatPatrolCatalog.TryGet(command.Tier, out BestiaryTierDefinition tier)) throw new ArgumentException("game.invalid_tier");
        CombatPatrolResult? result = null;
        await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
        {
            DateTimeOffset now = RequireUtc(clock.UtcNow);
            state = ApplyMaturedRecoveries(state, now);
            CombatPatrolState patrol = state.CombatPatrol ?? EmptyPatrol();
            string key = command.IdempotencyKey ?? string.Empty;
            Dictionary<string, long> requested = Quantities(command.Guardians, command.Wingrunners, command.Darters);
            string hash = Hash($"launch|{command.Tier}|{Canonical(requested)}|{command.ExpectedRevision}");
            if (patrol.Receipts.TryGetValue(key, out IdempotencyReceipt? old))
            {
                result = old.PayloadHash == hash ? new(old.Succeeded, old.Code, Snapshot(state)) : new(false, "game.idempotency_conflict", Snapshot(state));
                return state;
            }
            if (!ValidKey(command.IdempotencyKey))
            { result = new(false, "game.invalid_request", Snapshot(state)); return state; }
            if (patrol.Revision != command.ExpectedRevision)
            { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            if (patrol.ActiveEncounters.Count >= TotalSlots(patrol))
            { result = new(false, "game.patrol_no_slot_available", Snapshot(state)); return state; }
            if (patrol.TierCooldownEndsAtUtc.TryGetValue(command.Tier, out DateTimeOffset cooldownEndsAtUtc) && cooldownEndsAtUtc > now)
            { result = new(false, "game.patrol_cooldown_active", Snapshot(state)); return state; }
            int capacity = CombatSquadReservationService.ComputeCapacity(state.BuildingLevels);
            long totalRequested = requested.Values.Sum();
            if (totalRequested <= 0 || totalRequested > capacity)
            { result = new(false, "game.patrol_invalid_composition", Snapshot(state)); return state; }
            IReadOnlyDictionary<string, long> availableRoster = ComputeAvailableRoster(state);
            IReadOnlyDictionary<string, long> reserved = state.SquadReservation?.Reserved;
            bool isReservedSquad = reserved != null && Families.All(f => requested.GetValueOrDefault(f) <= reserved.GetValueOrDefault(f)) && requested.Values.Sum() > 0 && requested.Values.Sum() <= reserved.Values.Sum();
            if (!isReservedSquad && Families.Any(f => requested.GetValueOrDefault(f) > availableRoster.GetValueOrDefault(f)))
            { result = new(false, "game.patrol_insufficient_troops", Snapshot(state)); return state; }
            ChampionCombatContribution championContribution = ChampionBeeCatalog.CombatContribution(state.ChampionBees);
            TroopTierCombatContribution troopTierContribution = TroopTierCatalog.CombatContribution(state.TroopTierProgress);
            IReadOnlyDictionary<string, long> strategicPathBonus = StrategicPathBonusCatalog.CombatPowerBonusBpByFamily(state.StrategicPath?.SelectedPath);
            long availablePower = CombatPatrolResolution.ComputeAvailablePower(requested, tier.HazardFamily, MergedPowerBonus(championContribution.PowerBonusBpByFamily, troopTierContribution.PowerBonusBpByFamily, strategicPathBonus));
            long readinessBp = CombatPatrolResolution.ComputeReadinessBp(availablePower, tier.RequiredPower);
            if (!CombatPatrolResolution.CanLaunch(readinessBp))
            { result = new(false, "game.patrol_underpowered", Snapshot(state)); return state; }
            if (patrol.Revision == long.MaxValue) throw new InvalidDataException("combat patrol revision overflow");
            var encounter = new CombatPatrolActiveEncounter(Guid.NewGuid(), command.Tier, requested, now, now.Add(tier.Duration), command.IdempotencyKey!, hash);
            var activeEncounters = new List<CombatPatrolActiveEncounter>(patrol.ActiveEncounters) { encounter };
            var receipts = new Dictionary<string, IdempotencyReceipt>(patrol.Receipts, StringComparer.Ordinal) { [key] = new(hash, true, "game.patrol_launched", encounter.EncounterId, now, patrol.Revision, patrol.Revision + 1) };
            TrimReceipts(receipts, key);
            var nextPatrol = patrol with { Revision = patrol.Revision + 1, ActiveEncounters = activeEncounters, Receipts = receipts };
            var next = state with { Revision = checked(state.Revision + 1), CombatPatrol = nextPatrol };
            result = new(true, "game.patrol_launched", Snapshot(next));
            return next;
        }, ct);
        return result!;
    }

    public Task<CombatPatrolResult> ClaimAsync(ClaimCombatPatrolCommand command, CancellationToken ct)
        => FinishAsync(command.PlayerId, command.HiveId, command.EncounterId, command.ExpectedRevision, command.IdempotencyKey, resolve: true, ct);

    public Task<CombatPatrolResult> RecallAsync(RecallCombatPatrolCommand command, CancellationToken ct)
        => FinishAsync(command.PlayerId, command.HiveId, command.EncounterId, command.ExpectedRevision, command.IdempotencyKey, resolve: false, ct);

    public async Task<CombatPatrolResult> PurchaseResourceSlotAsync(PurchaseCombatPatrolResourceSlotCommand command, CancellationToken ct)
    {
        if (command.ExpectedRevision < 0 || command.ExpectedRevision == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(command.ExpectedRevision));
        CombatPatrolResult? result = null;
        await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
        {
            DateTimeOffset now = RequireUtc(clock.UtcNow);
            CombatPatrolState patrol = state.CombatPatrol ?? EmptyPatrol();
            string key = command.IdempotencyKey ?? string.Empty;
            string hash = Hash($"purchase-resource-slot|{command.ExpectedRevision}");
            if (patrol.Receipts.TryGetValue(key, out IdempotencyReceipt? old))
            {
                result = old.PayloadHash == hash ? new(old.Succeeded, old.Code, Snapshot(state)) : new(false, "game.idempotency_conflict", Snapshot(state));
                return state;
            }
            if (!ValidKey(command.IdempotencyKey))
            { result = new(false, "game.invalid_request", Snapshot(state)); return state; }
            if (patrol.Revision != command.ExpectedRevision)
            { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            if (patrol.ResourcePurchasedSlots >= CombatPatrolCatalog.MaxResourcePurchasedSlots)
            { result = new(false, "game.patrol_slot_limit_reached", Snapshot(state)); return state; }
            (long Honey, long Pollen) cost = CombatPatrolCatalog.ResourceSlotCosts[patrol.ResourcePurchasedSlots];
            if (!state.Resources.TryGetValue("honey", out ResourceBalance? honey) || !state.Resources.TryGetValue("pollen", out ResourceBalance? pollen) || honey.Amount < cost.Honey || pollen.Amount < cost.Pollen)
            { result = new(false, "game.insufficient_resources", Snapshot(state)); return state; }
            if (patrol.Revision == long.MaxValue) throw new InvalidDataException("combat patrol revision overflow");
            var resources = new Dictionary<string, ResourceBalance>(state.Resources, StringComparer.Ordinal) { ["honey"] = honey with { Amount = honey.Amount - cost.Honey }, ["pollen"] = pollen with { Amount = pollen.Amount - cost.Pollen } };
            var receipts = new Dictionary<string, IdempotencyReceipt>(patrol.Receipts, StringComparer.Ordinal) { [key] = new(hash, true, "game.patrol_slot_purchased", null, now, patrol.Revision, patrol.Revision + 1) };
            TrimReceipts(receipts, key);
            var nextPatrol = patrol with { Revision = patrol.Revision + 1, ResourcePurchasedSlots = patrol.ResourcePurchasedSlots + 1, Receipts = receipts };
            var next = state with { Revision = checked(state.Revision + 1), Resources = resources, CombatPatrol = nextPatrol };
            result = new(true, "game.patrol_slot_purchased", Snapshot(next));
            return next;
        }, ct);
        return result!;
    }

    // NOTE: this only grants the entitlement — no real-money payment is validated here. A real
    // store-receipt verification (App Store / Play Store server-to-server check) must happen
    // BEFORE this is ever called from a production endpoint. Do not wire this to raw client
    // input without that check in front of it; it exists so the data model and gating logic can
    // be built and tested ahead of the real store integration.
    public async Task<CombatPatrolResult> GrantPremiumSlotAsync(GrantCombatPatrolPremiumSlotCommand command, CancellationToken ct)
    {
        if (command.ExpectedRevision < 0 || command.ExpectedRevision == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(command.ExpectedRevision));
        CombatPatrolResult? result = null;
        await repository.ExecuteAtomicallyAsync(command.PlayerId, command.HiveId, state =>
        {
            DateTimeOffset now = RequireUtc(clock.UtcNow);
            CombatPatrolState patrol = state.CombatPatrol ?? EmptyPatrol();
            string key = command.IdempotencyKey ?? string.Empty;
            string hash = Hash($"grant-premium-slot|{command.ExpectedRevision}");
            if (patrol.Receipts.TryGetValue(key, out IdempotencyReceipt? old))
            {
                result = old.PayloadHash == hash ? new(old.Succeeded, old.Code, Snapshot(state)) : new(false, "game.idempotency_conflict", Snapshot(state));
                return state;
            }
            if (!ValidKey(command.IdempotencyKey))
            { result = new(false, "game.invalid_request", Snapshot(state)); return state; }
            if (patrol.Revision != command.ExpectedRevision)
            { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            if (patrol.PremiumPurchasedSlots >= CombatPatrolCatalog.MaxPremiumPurchasedSlots)
            { result = new(false, "game.patrol_slot_limit_reached", Snapshot(state)); return state; }
            if (patrol.Revision == long.MaxValue) throw new InvalidDataException("combat patrol revision overflow");
            var receipts = new Dictionary<string, IdempotencyReceipt>(patrol.Receipts, StringComparer.Ordinal) { [key] = new(hash, true, "game.patrol_slot_granted", null, now, patrol.Revision, patrol.Revision + 1) };
            TrimReceipts(receipts, key);
            var nextPatrol = patrol with { Revision = patrol.Revision + 1, PremiumPurchasedSlots = patrol.PremiumPurchasedSlots + 1, Receipts = receipts };
            var next = state with { Revision = checked(state.Revision + 1), CombatPatrol = nextPatrol };
            result = new(true, "game.patrol_slot_granted", Snapshot(next));
            return next;
        }, ct);
        return result!;
    }

    private async Task<CombatPatrolResult> FinishAsync(Guid player, Guid hive, Guid encounterId, long expectedRevision, string? idempotencyKey, bool resolve, CancellationToken ct)
    {
        if (expectedRevision < 0 || expectedRevision == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(expectedRevision));
        CombatPatrolResult? result = null;
        await repository.ExecuteAtomicallyAsync(player, hive, state =>
        {
            DateTimeOffset now = RequireUtc(clock.UtcNow);
            state = ApplyMaturedRecoveries(state, now);
            CombatPatrolState patrol = state.CombatPatrol ?? EmptyPatrol();
            string key = idempotencyKey ?? string.Empty;
            string hash = Hash($"{(resolve ? "claim" : "recall")}|{encounterId:N}|{expectedRevision}");
            if (patrol.Receipts.TryGetValue(key, out IdempotencyReceipt? old))
            {
                CombatPatrolClaimReceipt? storedClaim = patrol.ClaimReceipts?.GetValueOrDefault(key);
                result = old.PayloadHash == hash ? new(old.Succeeded, old.Code, Snapshot(state, storedClaim), storedClaim) : new(false, "game.idempotency_conflict", Snapshot(state, storedClaim));
                return state;
            }
            if (!ValidKey(idempotencyKey) || encounterId == Guid.Empty)
            { result = new(false, "game.invalid_request", Snapshot(state)); return state; }
            if (patrol.Revision != expectedRevision)
            { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            CombatPatrolActiveEncounter? active = patrol.ActiveEncounters.FirstOrDefault(e => e.EncounterId == encounterId);
            if (active is null)
            { result = new(false, "game.revision_conflict", Snapshot(state)); return state; }
            if (resolve && now < active.EndsAtUtc)
            { result = new(false, "game.patrol_not_complete", Snapshot(state)); return state; }
            if (!CombatPatrolCatalog.TryGet(active.Tier, out BestiaryTierDefinition tier))
                throw new InvalidDataException("Unknown combat patrol tier in active encounter.");
            if (patrol.Revision == long.MaxValue) throw new InvalidDataException("combat patrol revision overflow");

            var nextResources = new Dictionary<string, ResourceBalance>(state.Resources, StringComparer.Ordinal);
            var credited = new Dictionary<string, long>(StringComparer.Ordinal);
            DoctrineRosterState? nextRoster = state.DoctrineRoster;
            var recovering = new List<CombatPatrolRecoveringBatch>(patrol.Recovering ?? new List<CombatPatrolRecoveringBatch>());
            string code;
            CombatPatrolResolutionResult? resolution = null;
            ChampionCombatContribution championContribution = ChampionBeeCatalog.CombatContribution(state.ChampionBees);
            TroopTierCombatContribution troopTierContribution = TroopTierCatalog.CombatContribution(state.TroopTierProgress);
            string? strategicPathId = state.StrategicPath?.SelectedPath;
            IReadOnlyDictionary<string, long> strategicPathBonus = StrategicPathBonusCatalog.CombatPowerBonusBpByFamily(strategicPathId);
            var tierCooldowns = new Dictionary<int, DateTimeOffset>(patrol.TierCooldownEndsAtUtc);

            bool dailyFocusApplied = false;
            bool worldEventApplied = false;
            ActiveWorldEvent worldEvent = WorldEventCatalog.Active(now);
            if (resolve)
            {
                resolution = CombatPatrolResolution.Resolve(active.CommittedTroops, tier, MergedPowerBonus(championContribution.PowerBonusBpByFamily, troopTierContribution.PowerBonusBpByFamily, strategicPathBonus));
                DoctrineRosterState roster = state.DoctrineRoster ?? new DoctrineRosterState(0, new(), null, new());
                var counts = new Dictionary<string, long>(roster.Counts, StringComparer.Ordinal);
                TimeSpan recoveryDuration = CombatPatrolResolution.ComputeRecoveryDuration(tier);
                foreach (string family in Families)
                {
                    long permanentlyLost = resolution.PermanentLosses.GetValueOrDefault(family);
                    long wounded = resolution.WoundedLosses.GetValueOrDefault(family);
                    counts[family] = Math.Max(0, counts.GetValueOrDefault(family) - permanentlyLost - wounded);
                    if (wounded > 0) recovering.Add(new CombatPatrolRecoveringBatch(family, wounded, now.Add(recoveryDuration)));
                }
                nextRoster = roster with { Counts = counts };
                // Cible du jour (demande de Jeff, 2026-07-31) : un palier different chaque jour
                // civil recoit +50% de recompense a la validation - pure fonction de la date, ne
                // touche jamais la puissance de combat ni les seuils de resolution ci-dessus.
                dailyFocusApplied = active.Tier == DailyFocusCatalog.FeaturedCombatTier(now);
                long honeyReward = dailyFocusApplied ? DailyFocusCatalog.ApplyRewardBonus(resolution.HoneyCredited) : resolution.HoneyCredited;
                long pollenReward = dailyFocusApplied ? DailyFocusCatalog.ApplyRewardBonus(resolution.PollenCredited) : resolution.PollenCredited;
                // Evenement mondial dynamique, localise (demande de Jeff, 2026-08-01) : une "menace
                // en hausse" ne boost plus tous les paliers de sa famille de danger a la fois - un
                // seul palier precis, choisi parmi eux, recoit reellement le bonus ce cycle. En plus
                // (et independamment) de la Cible du jour - jamais la puissance de combat.
                worldEventApplied = WorldEventFeaturedTier(worldEvent, now) == active.Tier;
                if (worldEventApplied)
                {
                    honeyReward = WorldEventCatalog.ApplyBonusBp(honeyReward, worldEvent.BonusBp);
                    pollenReward = WorldEventCatalog.ApplyBonusBp(pollenReward, worldEvent.BonusBp);
                }
                credited["honey"] = ApplyReward(nextResources, "honey", honeyReward);
                credited["pollen"] = ApplyReward(nextResources, "pollen", pollenReward);
                tierCooldowns[active.Tier] = now.Add(tier.Cooldown);
                code = resolution.Band switch
                {
                    CombatPatrolOutcomeBand.DecisiveVictory => "game.patrol_decisive_victory",
                    CombatPatrolOutcomeBand.Victory => "game.patrol_victory",
                    _ => "game.patrol_hard_won"
                };
            }
            else
            {
                code = "game.patrol_recalled";
            }

            var remainingEncounters = patrol.ActiveEncounters.Where(e => e.EncounterId != encounterId).ToList();
            var receipts = new Dictionary<string, IdempotencyReceipt>(patrol.Receipts, StringComparer.Ordinal) { [key] = new(hash, true, code, encounterId, now, patrol.Revision, patrol.Revision + 1) };
            TrimReceipts(receipts, key);
            CombatPatrolClaimReceipt? claimReceipt = resolve
                ? new(player, hive, encounterId, active.Tier, resolution!.Band.ToString(), now, new Dictionary<string, long>(resolution.PermanentLosses, StringComparer.Ordinal), new Dictionary<string, long>(resolution.WoundedLosses, StringComparer.Ordinal), credited, nextResources.Where(x => x.Key is "honey" or "pollen").ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal), new List<string>(championContribution.ContributingBeeIds), new Dictionary<string, long>(championContribution.PowerBonusBpByFamily, StringComparer.Ordinal), new Dictionary<string, int>(troopTierContribution.TierByFamily, StringComparer.Ordinal), new Dictionary<string, long>(troopTierContribution.PowerBonusBpByFamily, StringComparer.Ordinal), resolution.AvailablePower, resolution.RequiredPower, resolution.ReadinessBp, strategicPathId, new Dictionary<string, long>(strategicPathBonus, StringComparer.Ordinal), dailyFocusApplied, worldEventApplied, worldEventApplied ? worldEvent.Key : "")
                : null;
            var claimReceipts = new Dictionary<string, CombatPatrolClaimReceipt>(patrol.ClaimReceipts ?? new Dictionary<string, CombatPatrolClaimReceipt>(StringComparer.Ordinal), StringComparer.Ordinal);
            if (claimReceipt is not null) claimReceipts[key] = claimReceipt;

            // Carnet du Bestiaire (demande de Jeff, 2026-08-01) : sous-produit automatique de la
            // reclamation deja existante ci-dessus - meme donnees, aucune nouvelle commande joueur.
            BestiaryCodexState? nextBestiaryCodex = state.BestiaryCodex;
            if (resolve)
            {
                nextBestiaryCodex = BestiaryCodexAccounting.RecordEncounter(
                    state.BestiaryCodex, active.Tier, resolution!.Band.ToString(), now,
                    credited.GetValueOrDefault("honey"), credited.GetValueOrDefault("pollen"),
                    championContribution.ContributingBeeIds, strategicPathId, worldEventApplied, dailyFocusApplied);
            }

            var nextPatrol = patrol with { Revision = patrol.Revision + 1, ActiveEncounters = remainingEncounters, Receipts = receipts, TierCooldownEndsAtUtc = tierCooldowns, ClaimReceipts = claimReceipts, Recovering = recovering };
            var next = state with { Revision = checked(state.Revision + 1), Resources = nextResources, DoctrineRoster = nextRoster, CombatPatrol = nextPatrol, BestiaryCodex = nextBestiaryCodex };
            result = new(true, code, Snapshot(next, claimReceipt), claimReceipt);
            return next;
        }, ct);
        return result!;
    }

    private async Task<PlayerHiveState> ReadMaturedAsync(Guid player, Guid hive, CancellationToken ct)
    {
        PlayerHiveState state = await repository.ReadAsync(player, hive, ct) ?? throw new KeyNotFoundException();
        DateTimeOffset now = RequireUtc(clock.UtcNow);
        IReadOnlyList<CombatPatrolRecoveringBatch> recovering = state.CombatPatrol?.Recovering ?? new List<CombatPatrolRecoveringBatch>();
        if (!recovering.Any(b => b.ReadyAtUtc <= now)) return state;
        return await repository.ExecuteAtomicallyAsync(player, hive, s =>
        {
            DateTimeOffset innerNow = RequireUtc(clock.UtcNow);
            return ApplyMaturedRecoveries(s, innerNow);
        }, ct);
    }

    private static PlayerHiveState ApplyMaturedRecoveries(PlayerHiveState state, DateTimeOffset now)
    {
        CombatPatrolState patrol = state.CombatPatrol ?? EmptyPatrol();
        List<CombatPatrolRecoveringBatch> pending = patrol.Recovering ?? new List<CombatPatrolRecoveringBatch>();
        if (pending.Count == 0 || !pending.Any(b => b.ReadyAtUtc <= now)) return state;
        DoctrineRosterState roster = state.DoctrineRoster ?? new DoctrineRosterState(0, new(), null, new());
        var counts = new Dictionary<string, long>(roster.Counts, StringComparer.Ordinal);
        var remaining = new List<CombatPatrolRecoveringBatch>();
        foreach (CombatPatrolRecoveringBatch batch in pending)
        {
            if (batch.ReadyAtUtc <= now) counts[batch.Family] = counts.GetValueOrDefault(batch.Family) + batch.Count;
            else remaining.Add(batch);
        }
        return state with
        {
            Revision = checked(state.Revision + 1),
            DoctrineRoster = roster with { Counts = counts },
            CombatPatrol = patrol with { Recovering = remaining }
        };
    }

    private CombatPatrolSnapshot Snapshot(PlayerHiveState state, CombatPatrolClaimReceipt? claimReceipt = null)
    {
        DateTimeOffset now = RequireUtc(clock.UtcNow);
        CombatPatrolState patrol = state.CombatPatrol ?? EmptyPatrol();
        int totalSlots = TotalSlots(patrol);
        IReadOnlyDictionary<string, long> availableRoster = ComputeAvailableRoster(state);
        int capacity = CombatSquadReservationService.ComputeCapacity(state.BuildingLevels);
        (long Honey, long Pollen)? nextResourceCost = patrol.ResourcePurchasedSlots < CombatPatrolCatalog.MaxResourcePurchasedSlots
            ? CombatPatrolCatalog.ResourceSlotCosts[patrol.ResourcePurchasedSlots]
            : null;
        ActiveWorldEvent worldEvent = WorldEventCatalog.Active(now);
        return new(state.PlayerId, state.HiveId, ContractVersion, patrol.Revision, now, patrol.ActiveEncounters, patrol.TierCooldownEndsAtUtc, patrol.Recovering ?? new List<CombatPatrolRecoveringBatch>(), availableRoster, capacity, totalSlots, patrol.ResourcePurchasedSlots, patrol.PremiumPurchasedSlots, nextResourceCost, claimReceipt, DailyFocusCatalog.FeaturedCombatTier(now), worldEvent, WorldEventFeaturedTier(worldEvent, now));
    }

    // Localisation de l'evenement mondial (demande de Jeff, 2026-08-01) : parmi les paliers qui
    // partagent la famille de danger visee par une "menace en hausse", lequel est la region precise
    // ciblee ce cycle. Retourne null si l'evenement actif n'est pas une menace de combat.
    private static int? WorldEventFeaturedTier(ActiveWorldEvent worldEvent, DateTimeOffset now)
    {
        if (worldEvent.Kind != WorldEventKind.ThreatSurge) return null;
        List<int> eligible = CombatPatrolCatalog.Tiers.Values
            .Where(t => string.Equals(t.HazardFamily, worldEvent.TargetKey, StringComparison.Ordinal))
            .Select(t => t.Tier).OrderBy(t => t).ToList();
        return WorldEventCatalog.FeaturedRegionTier(now, eligible);
    }

    private static int TotalSlots(CombatPatrolState patrol)
        => Math.Min(CombatPatrolCatalog.MaxConcurrentSlots, 1 + patrol.ResourcePurchasedSlots + patrol.PremiumPurchasedSlots);

    // Deleguee a HiveTroopDeploymentAccounting (demande de Jeff, 2026-08-01) : ce calcul doit
    // desormais aussi tenir compte des troupes engagees par la Collecte mondiale, pas seulement
    // par Combat Patrol - une seule source de verite partagee entre tous les systemes de terrain.
    private static IReadOnlyDictionary<string, long> ComputeAvailableRoster(PlayerHiveState state)
        => HiveTroopDeploymentAccounting.ComputeAvailableRoster(state);

    private static CombatPatrolState EmptyPatrol() => new(0, new List<CombatPatrolActiveEncounter>(), new(), new());

    // Combine plusieurs sources de bonus de puissance (championnes, palier de troupe, et de
    // futures sources - recherche, batiments, alliance) en un seul modificateur par famille pour
    // le calcul deterministe de CombatPatrolResolution. Chaque source reste suivie separement
    // sur le reçu (voir CombatPatrolClaimReceipt) pour que le debrief explique chaque contribution.
    private static Dictionary<string, long> MergedPowerBonus(params IReadOnlyDictionary<string, long>[] sources)
    {
        var merged = Families.ToDictionary(f => f, _ => 0L, StringComparer.Ordinal);
        foreach (IReadOnlyDictionary<string, long> source in sources)
            foreach (string family in Families)
                merged[family] += source.GetValueOrDefault(family);
        return merged;
    }

    private static Dictionary<string, long> Quantities(long guardians, long wingrunners, long darters) => new(StringComparer.Ordinal)
    {
        ["guardians"] = Math.Max(0, guardians),
        ["wingrunners"] = Math.Max(0, wingrunners),
        ["darters"] = Math.Max(0, darters)
    };

    private static string Canonical(Dictionary<string, long> q) => string.Join(";", Families.Select(f => f + "=" + q.GetValueOrDefault(f)));

    private static void TrimReceipts(Dictionary<string, IdempotencyReceipt> receipts, string protectedKey)
    {
        while (receipts.Count > MaxReceipts)
        {
            string victim = receipts.OrderBy(x => x.Value.CreatedAtUtc).ThenBy(x => x.Key, StringComparer.Ordinal).First(x => x.Key != protectedKey).Key;
            receipts.Remove(victim);
        }
    }

    private static DateTimeOffset RequireUtc(DateTimeOffset value) => value.Offset == TimeSpan.Zero ? value : value.ToUniversalTime();
    private static bool ValidKey(string? key) => !string.IsNullOrWhiteSpace(key) && key.Length <= 256;
    private static long ApplyReward(Dictionary<string, ResourceBalance> resources, string key, long amount)
    {
        if (!resources.TryGetValue(key, out ResourceBalance? balance) || amount < 0 || balance.Amount < 0 || balance.Capacity < balance.Amount) return 0;
        long creditedAmount = Math.Min(amount, balance.Capacity - balance.Amount);
        resources[key] = balance with { Amount = balance.Amount + creditedAmount };
        return creditedAmount;
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
