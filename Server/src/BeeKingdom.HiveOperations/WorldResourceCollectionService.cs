using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

// Le placement visuel du noeud sur la carte reste entiere responsabilite du client (deja gere
// par WorldMapMmoFullscreenFoundationBootstrap, place de facon procedurale/seedee) - le serveur
// n'a besoin de connaitre que l'identifiant pour faire correspondre l'economie reelle au bon
// noeud visuel, pas sa position.
public sealed record WorldResourceNodeDefinition(string NodeId, string ResourceKey, string Tier, long Yield, TimeSpan Duration, TimeSpan Cooldown, string Label);

public sealed class WorldResourceCollectionOptions
{
    public const string SectionName = "WorldResourceCollection";
    private static readonly HashSet<string> KnownResourceKeys = new(StringComparer.Ordinal) { "honey", "pollen", "wax" };
    private static readonly HashSet<string> KnownTiers = new(StringComparer.Ordinal) { "poor", "medium", "rich" };

    public bool Enabled { get; set; }
    public string CatalogVersion { get; set; } = "";
    public List<WorldResourceNodeDefinition> Catalog { get; set; } = [];

    public void Validate()
    {
        if (!Enabled && Catalog.Count == 0) return;
        if (Catalog.Count == 0
            || CatalogVersion.Length is < 1 or > 64
            || CatalogVersion.Trim() != CatalogVersion
            || !System.Text.RegularExpressions.Regex.IsMatch(CatalogVersion, "^[a-z0-9._-]+$"))
            throw new InvalidDataException("Invalid world resource collection options");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (WorldResourceNodeDefinition entry in Catalog)
        {
            if (string.IsNullOrWhiteSpace(entry.NodeId) || entry.NodeId.Length > 128
                || !KnownResourceKeys.Contains(entry.ResourceKey)
                || !KnownTiers.Contains(entry.Tier)
                || entry.Yield <= 0
                || entry.Duration <= TimeSpan.Zero || entry.Duration > TimeSpan.FromHours(6)
                || entry.Cooldown < TimeSpan.Zero || entry.Cooldown > TimeSpan.FromDays(1)
                || string.IsNullOrWhiteSpace(entry.Label)
                || !seen.Add(entry.NodeId))
                throw new InvalidDataException("Invalid world resource collection catalog");
        }
    }
}

public sealed record WorldResourceCollectionState(long Revision, Dictionary<string, DateTimeOffset> NodeReadyAtUtc, WorldResourceActiveFlight? Active, Dictionary<string, IdempotencyReceipt> Receipts, Dictionary<string, WorldResourceClaimReceipt>? ClaimReceipts = null);
// CommittedTroops (demande de Jeff, 2026-08-01) : premiere brique de l'architecture de deploiement
// reutilisable plus tard (PvP, raids, renforts, occupation de points d'interet) - l'escouade est
// reellement engagee hors de la ruche pour toute la duree du vol, comptabilisee par
// HiveTroopDeploymentAccounting exactement comme les encounters de Combat Patrol.
public sealed record WorldResourceActiveFlight(Guid FlightId, string NodeId, DateTimeOffset StartedAtUtc, DateTimeOffset EndsAtUtc, Dictionary<string, long> CommittedTroops, long Revision, string LaunchIdempotencyKey, string PayloadHash);
public sealed record WorldResourceClaimReceipt(Guid PlayerId, Guid HiveId, Guid FlightId, string NodeId, string ResourceKey, long CreditedAmount, long Revision, DateTimeOffset ServerTimeUtc, ResourceBalance ResultingBalance, bool DailyFocusApplied = false, bool WorldEventApplied = false, string WorldEventKey = "");
public sealed record WorldResourceNodeReadModel(string NodeId, string ResourceKey, string Tier, long Yield, TimeSpan Duration, TimeSpan Cooldown, string Label, bool Ready, DateTimeOffset? ReadyAtUtc, bool CanLaunch, bool IsDailyFocus = false, bool IsWorldEventBoosted = false);
public sealed record WorldResourceCollectionSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, string CatalogVersion, long Revision, DateTimeOffset ServerTimeUtc, IReadOnlyList<WorldResourceNodeReadModel> Nodes, WorldResourceActiveFlight? Active, WorldResourceClaimReceipt? ClaimReceipt = null, string? FeaturedNodeId = null, ActiveWorldEvent? WorldEvent = null, IReadOnlyDictionary<string, long>? AvailableRoster = null);
public sealed record LaunchWorldResourceCollectionRequest(long Guardians, long Wingrunners, long Darters, long ExpectedRevision, string IdempotencyKey);
public sealed record ClaimWorldResourceCollectionRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record RecallWorldResourceCollectionRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record WorldResourceCollectionResult(bool Succeeded, string Code, WorldResourceCollectionSnapshot Snapshot, WorldResourceClaimReceipt? ClaimReceipt = null);

// Premiere boucle de jeu qui fait vraiment sortir le joueur de sa ruche : la carte du monde
// (WorldMapMmoFullscreenFoundationBootstrap) affiche deja des noeuds de ressource visibles et un
// bouton "Collecter" reel, mais tout etait local/demo (aucun serveur, recompense affichee puis
// perdue - voir l'audit du 2026-07-31). Ce service rend reel exactement les 3 noeuds dont la
// ressource existe deja dans l'economie du serveur (miel/pollen/cire) ; les autres types visibles
// sur la carte (nectar/eau/propolis/gelee royale) restent volontairement demo pour l'instant (pas
// de nouvelle monnaie inventee sans besoin reel - voir Claude_Continuation.md).
public sealed class WorldResourceCollectionService(IHiveStateRepository repository, IServerClock clock, WorldResourceCollectionOptions options)
{
    public const string ContractVersion = "living-hive-world-resource-collection-v1";
    private readonly WorldResourceCollectionOptions o = options ?? throw new ArgumentNullException(nameof(options));
    private Dictionary<string, WorldResourceNodeDefinition> CatalogByKey => o.Catalog.ToDictionary(x => x.NodeId, StringComparer.Ordinal);

    public async Task<WorldResourceCollectionSnapshot> ReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        Ensure();
        DateTimeOffset now = Utc();
        return Snapshot(await repository.ExecuteAtomicallyAsync(playerId, hiveId, state => state, ct), now);
    }

    public async Task<WorldResourceCollectionResult> LaunchAsync(Guid playerId, Guid hiveId, string nodeId, LaunchWorldResourceCollectionRequest request, CancellationToken ct = default)
    {
        Ensure();
        if (string.IsNullOrWhiteSpace(nodeId) || request is null || request.ExpectedRevision < 0 || !ValidKey(request.IdempotencyKey))
            return Fail(playerId, hiveId, "game.invalid_request");

        var catalogByKey = CatalogByKey;
        WorldResourceCollectionResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            DateTimeOffset now = Utc();
            WorldResourceCollectionState collection = state.WorldResourceCollection ?? NewState();
            string hash = Hash($"launch|{nodeId}|{request.Guardians}|{request.Wingrunners}|{request.Darters}|{request.ExpectedRevision}");
            if (collection.Receipts.TryGetValue(request.IdempotencyKey, out IdempotencyReceipt? stored))
            {
                result = stored.PayloadHash == hash ? Replay(state, collection, stored, now, request.IdempotencyKey) : Fail(state, collection, "game.idempotency_conflict", now);
                return state;
            }
            if (!catalogByKey.TryGetValue(nodeId, out WorldResourceNodeDefinition? node))
            { result = Fail(state, collection, "game.invalid_request", now); return state; }
            if (collection.Revision != request.ExpectedRevision)
            { result = Fail(state, collection, "game.revision_conflict", now); return state; }
            if (collection.Active is not null)
            { result = Fail(state, collection, "game.world_resource_busy", now); return state; }
            if (collection.NodeReadyAtUtc.GetValueOrDefault(nodeId, DateTimeOffset.MinValue) > now)
            { result = Fail(state, collection, "game.world_resource_cooling_down", now); return state; }

            // Escouade reellement engagee (demande de Jeff, 2026-08-01) : meme comptabilite
            // partagee que Combat Patrol (HiveTroopDeploymentAccounting), pour qu'aucune abeille ne
            // soit jamais comptee a la fois en combat et en collecte.
            Dictionary<string, long> requestedTroops = new(StringComparer.Ordinal)
            {
                ["guardians"] = Math.Max(0, request.Guardians),
                ["wingrunners"] = Math.Max(0, request.Wingrunners),
                ["darters"] = Math.Max(0, request.Darters)
            };
            int capacity = CombatSquadReservationService.ComputeCapacity(state.BuildingLevels);
            if (!HiveTroopDeploymentAccounting.IsValidComposition(requestedTroops, capacity))
            { result = Fail(state, collection, "game.invalid_request", now); return state; }
            IReadOnlyDictionary<string, long> availableRoster = HiveTroopDeploymentAccounting.ComputeAvailableRoster(state);
            if (HiveTroopDeploymentAccounting.Families.Any(f => requestedTroops.GetValueOrDefault(f) > availableRoster.GetValueOrDefault(f)))
            { result = Fail(state, collection, "game.world_resource_insufficient_troops", now); return state; }

            WorldResourceActiveFlight flight = new(Guid.NewGuid(), nodeId, now, now + node.Duration, requestedTroops, collection.Revision + 1, request.IdempotencyKey, hash);
            WorldResourceCollectionState updatedCollection = collection with { Revision = collection.Revision + 1, Active = flight };
            PlayerHiveState updated = state with { WorldResourceCollection = updatedCollection };
            result = Success(updated, now, flight, request.IdempotencyKey, "game.world_resource_launched", null);
            return Receipt(updated, updatedCollection, request.IdempotencyKey, hash, result);
        }, ct);
        return result!;
    }

    public async Task<WorldResourceCollectionResult> ClaimAsync(Guid playerId, Guid hiveId, Guid flightId, ClaimWorldResourceCollectionRequest request, CancellationToken ct = default)
    {
        Ensure();
        if (flightId == Guid.Empty || request is null || request.ExpectedRevision < 0 || !ValidKey(request.IdempotencyKey))
            return Fail(playerId, hiveId, "game.invalid_request");

        var catalogByKey = CatalogByKey;
        WorldResourceCollectionResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            DateTimeOffset now = Utc();
            WorldResourceCollectionState collection = state.WorldResourceCollection ?? NewState();
            string hash = Hash($"claim|{flightId}|{request.ExpectedRevision}");
            if (collection.Receipts.TryGetValue(request.IdempotencyKey, out IdempotencyReceipt? stored))
            {
                var storedClaim = collection.ClaimReceipts?.GetValueOrDefault(request.IdempotencyKey);
                result = stored.PayloadHash == hash ? Replay(state, collection, stored, now, request.IdempotencyKey, storedClaim) : Fail(state, collection, "game.idempotency_conflict", now);
                return state;
            }
            WorldResourceActiveFlight? active = collection.Active;
            if (active is null || active.FlightId != flightId || collection.Revision != request.ExpectedRevision)
            { result = Fail(state, collection, "game.revision_conflict", now); return state; }
            if (now < active.EndsAtUtc)
            { result = Fail(state, collection, "game.world_resource_not_ready", now); return state; }
            if (!catalogByKey.TryGetValue(active.NodeId, out WorldResourceNodeDefinition? node))
            { result = Fail(state, collection, "game.invalid_request", now); return state; }

            // Cible du jour (demande de Jeff, 2026-07-31) : un noeud different chaque jour civil
            // recoit +50% de recompense a la validation - meme mecanique et meme raisonnement que
            // pour Combat Patrol, pure fonction de la date.
            bool dailyFocusApplied = string.Equals(node.NodeId, DailyFocusCatalog.FeaturedWorldResourceNodeId(now, o.Catalog.Select(x => x.NodeId).ToList()), StringComparison.Ordinal);
            long yield = dailyFocusApplied ? DailyFocusCatalog.ApplyRewardBonus(node.Yield) : node.Yield;
            // Evenement mondial dynamique, localise (demande de Jeff, 2026-08-01) : la meteo du
            // cycle ne boost/reduit plus tous les noeuds de la ressource visee a la fois - un seul
            // noeud precis, choisi parmi eux, est reellement affecte ce cycle. En plus (et
            // independamment) de la Cible du jour - change plusieurs fois par jour au lieu d'une fois.
            ActiveWorldEvent worldEvent = WorldEventCatalog.Active(now);
            bool worldEventApplied = string.Equals(node.NodeId, WorldEventFeaturedNodeId(worldEvent, now), StringComparison.Ordinal);
            if (worldEventApplied) yield = WorldEventCatalog.ApplyBonusBp(yield, worldEvent.BonusBp);
            Dictionary<string, ResourceBalance> resources = new(state.Resources, StringComparer.Ordinal);
            long credited = ApplyReward(resources, node.ResourceKey, yield);
            Dictionary<string, DateTimeOffset> readyAt = new(collection.NodeReadyAtUtc, StringComparer.Ordinal) { [node.NodeId] = now + node.Cooldown };
            WorldResourceClaimReceipt claim = new(playerId, hiveId, flightId, node.NodeId, node.ResourceKey, credited, collection.Revision + 1, now, resources[node.ResourceKey], dailyFocusApplied, worldEventApplied, worldEventApplied ? worldEvent.Key : "");
            Dictionary<string, WorldResourceClaimReceipt> claimReceipts = new(collection.ClaimReceipts ?? new(StringComparer.Ordinal), StringComparer.Ordinal) { [request.IdempotencyKey] = claim };
            WorldResourceCollectionState updatedCollection = collection with { Revision = collection.Revision + 1, Active = null, NodeReadyAtUtc = readyAt, ClaimReceipts = claimReceipts };
            PlayerHiveState updated = state with { Resources = resources, WorldResourceCollection = updatedCollection };
            result = Success(updated, now, null, request.IdempotencyKey, "game.world_resource_claimed", claim);
            return Receipt(updated, updatedCollection, request.IdempotencyKey, hash, result);
        }, ct);
        return result!;
    }

    // Rappel de l'escouade avant la fin (demande de Jeff, 2026-08-01) : rend les troupes engagees
    // immediatement (aucune recompense, aucun malus de repos) - meme raisonnement que
    // CombatPatrolService.RecallAsync (un "annuler" honnete, jamais une penalite deguisee). Le
    // noeud redevient disponible tout de suite (pas de mise en cooldown, contrairement a une
    // vraie recolte) puisque rien n'a ete recolte.
    public async Task<WorldResourceCollectionResult> RecallAsync(Guid playerId, Guid hiveId, Guid flightId, RecallWorldResourceCollectionRequest request, CancellationToken ct = default)
    {
        Ensure();
        if (flightId == Guid.Empty || request is null || request.ExpectedRevision < 0 || !ValidKey(request.IdempotencyKey))
            return Fail(playerId, hiveId, "game.invalid_request");

        WorldResourceCollectionResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            DateTimeOffset now = Utc();
            WorldResourceCollectionState collection = state.WorldResourceCollection ?? NewState();
            string hash = Hash($"recall|{flightId}|{request.ExpectedRevision}");
            if (collection.Receipts.TryGetValue(request.IdempotencyKey, out IdempotencyReceipt? stored))
            {
                result = stored.PayloadHash == hash ? Replay(state, collection, stored, now, request.IdempotencyKey) : Fail(state, collection, "game.idempotency_conflict", now);
                return state;
            }
            WorldResourceActiveFlight? active = collection.Active;
            if (active is null || active.FlightId != flightId || collection.Revision != request.ExpectedRevision)
            { result = Fail(state, collection, "game.revision_conflict", now); return state; }

            WorldResourceCollectionState updatedCollection = collection with { Revision = collection.Revision + 1, Active = null };
            PlayerHiveState updated = state with { WorldResourceCollection = updatedCollection };
            result = Success(updated, now, null, request.IdempotencyKey, "game.world_resource_recalled", null);
            return Receipt(updated, updatedCollection, request.IdempotencyKey, hash, result);
        }, ct);
        return result!;
    }

    private void Ensure()
    {
        o.Validate();
        if (!o.Enabled) throw new InvalidOperationException("World resource collection is disabled");
    }

    private DateTimeOffset Utc()
    {
        DateTimeOffset now = clock.UtcNow;
        if (now.Offset != TimeSpan.Zero) throw new InvalidDataException("Server clock must be UTC");
        return now;
    }

    private static WorldResourceCollectionState NewState() => new(0, new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal), null, new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal));
    private static bool ValidKey(string? key) => !string.IsNullOrWhiteSpace(key) && key.Trim() == key && key.Length <= 256;
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static long ApplyReward(Dictionary<string, ResourceBalance> resources, string key, long amount)
    {
        if (!resources.TryGetValue(key, out ResourceBalance? balance) || amount < 0 || balance.Amount < 0 || balance.Capacity < balance.Amount) return 0;
        long credited = Math.Min(amount, balance.Capacity - balance.Amount);
        resources[key] = balance with { Amount = balance.Amount + credited };
        return credited;
    }

    private WorldResourceCollectionSnapshot Snapshot(PlayerHiveState state, DateTimeOffset now, WorldResourceClaimReceipt? claimReceipt = null)
    {
        WorldResourceCollectionState collection = state.WorldResourceCollection ?? NewState();
        string? featuredNodeId = DailyFocusCatalog.FeaturedWorldResourceNodeId(now, o.Catalog.Select(x => x.NodeId).ToList());
        ActiveWorldEvent worldEvent = WorldEventCatalog.Active(now);
        string? worldEventFeaturedNodeId = WorldEventFeaturedNodeId(worldEvent, now);
        List<WorldResourceNodeReadModel> nodes = [];
        foreach (WorldResourceNodeDefinition node in o.Catalog)
        {
            DateTimeOffset? readyAt = collection.NodeReadyAtUtc.TryGetValue(node.NodeId, out DateTimeOffset value) ? value : null;
            bool ready = readyAt is null || readyAt <= now;
            bool isDailyFocus = string.Equals(node.NodeId, featuredNodeId, StringComparison.Ordinal);
            bool isWorldEventBoosted = string.Equals(node.NodeId, worldEventFeaturedNodeId, StringComparison.Ordinal);
            nodes.Add(new WorldResourceNodeReadModel(node.NodeId, node.ResourceKey, node.Tier, node.Yield, node.Duration, node.Cooldown, node.Label, ready, ready ? null : readyAt, ready && collection.Active is null, isDailyFocus, isWorldEventBoosted));
        }
        IReadOnlyDictionary<string, long> availableRoster = HiveTroopDeploymentAccounting.ComputeAvailableRoster(state);
        return new(state.PlayerId, state.HiveId, ContractVersion, o.CatalogVersion, collection.Revision, now, nodes, collection.Active, claimReceipt, featuredNodeId, worldEvent, availableRoster);
    }

    // Localisation de l'evenement mondial (demande de Jeff, 2026-08-01) : parmi les noeuds qui
    // partagent la ressource visee par une meteo active, lequel est la region precise ciblee ce
    // cycle. Retourne null si l'evenement actif n'est pas une meteo (donc sans effet ici).
    private string? WorldEventFeaturedNodeId(ActiveWorldEvent worldEvent, DateTimeOffset now)
    {
        if (worldEvent.Kind != WorldEventKind.Weather) return null;
        List<string> eligible = o.Catalog
            .Where(n => string.Equals(n.ResourceKey, worldEvent.TargetKey, StringComparison.Ordinal))
            .Select(n => n.NodeId).ToList();
        return WorldEventCatalog.FeaturedRegionNodeId(now, eligible);
    }

    private WorldResourceCollectionResult Fail(Guid playerId, Guid hiveId, string code) =>
        new(false, code, new WorldResourceCollectionSnapshot(playerId, hiveId, ContractVersion, o.CatalogVersion, 0, DateTimeOffset.UnixEpoch, Array.Empty<WorldResourceNodeReadModel>(), null));

    private WorldResourceCollectionResult Fail(PlayerHiveState state, WorldResourceCollectionState collection, string code, DateTimeOffset now) =>
        new(false, code, Snapshot(state with { WorldResourceCollection = collection }, now));

    private WorldResourceCollectionResult Success(PlayerHiveState state, DateTimeOffset now, WorldResourceActiveFlight? _, string idempotencyKey, string code, WorldResourceClaimReceipt? claimReceipt) =>
        new(true, code, Snapshot(state, now, claimReceipt), claimReceipt);

    private WorldResourceCollectionResult Replay(PlayerHiveState state, WorldResourceCollectionState collection, IdempotencyReceipt receipt, DateTimeOffset now, string idempotencyKey, WorldResourceClaimReceipt? claimReceipt = null) =>
        new(receipt.Succeeded, receipt.Code, Snapshot(state with { WorldResourceCollection = collection }, now, claimReceipt), claimReceipt);

    private PlayerHiveState Receipt(PlayerHiveState state, WorldResourceCollectionState collection, string idempotencyKey, string hash, WorldResourceCollectionResult result)
    {
        Dictionary<string, IdempotencyReceipt> receipts = new(collection.Receipts, StringComparer.Ordinal)
        {
            [idempotencyKey] = new IdempotencyReceipt(hash, result.Succeeded, result.Code, null, DateTimeOffset.UtcNow, collection.Revision - 1, collection.Revision, AcceptedAtUtc: DateTimeOffset.UtcNow)
        };
        WorldResourceCollectionState withReceipt = collection with { Receipts = receipts };
        return state with { WorldResourceCollection = withReceipt };
    }
}
