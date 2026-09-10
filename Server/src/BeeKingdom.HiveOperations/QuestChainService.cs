using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed class QuestChainOptions
{
    public const string SectionName = "QuestChain";
    public bool Enabled { get; set; }
}

// M077-CL: chaine d'objectifs Alpha, persistante, non expirante, a reclamation INDIVIDUELLE par
// objectif (contrairement a HiveMilestoneEventService, dont la reclamation est unique et globale
// pour tout le lot et qui expire apres une fenetre de temps). Meme mecanique de fond que ce
// service (etat lu directement depuis PlayerHiveState d'autres systemes deja persistes, aucune
// nouvelle instrumentation dans ces systemes, cle d'idempotence + revision optimiste par mutation)
// mais objectifs et recompenses propres, et jamais de reinitialisation/expiration. Un seul
// objectif (world_map_visit) n'a aucun signal serveur naturel ailleurs (ouvrir la World Map est un
// pur changement de scene cote client) : il est donc auto-declare par le client via
// ReportWorldMapVisitedAsync, idempotent par nature (mettre un booleen a vrai est deja sans effet
// de bord a rejouer), le reste de la chaine reste verifie server-side.
public sealed class QuestChainService(IHiveStateRepository repository, IServerClock clock, QuestChainOptions options)
{
    public const string ContractVersion = "living-hive-quest-chain-v1";

    private sealed record QuestObjectiveDefinition(string Key, string RewardResourceKey, long RewardAmount);

    // Ordre narratif Q1->Q5 (mission M077-CL). Toutes les recompenses sont des ressources de base
    // (miel/cire/pollen) - pas de Gelee Royale, pas de Speed Up pour cette premiere chaine Alpha,
    // conformement a la demande explicite du CEO.
    private static readonly QuestObjectiveDefinition[] Objectives =
    [
        new("q1_building_upgrade", "honey", 200),
        new("q2_troop_recruit", "wax", 150),
        new("q3_research_complete", "pollen", 150),
        new("q4_world_map_visit", "honey", 100),
        new("q5_world_resource_collect", "wax", 100)
    ];

    private readonly QuestChainOptions o = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<QuestChainSnapshot> ReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        Ensure();
        DateTimeOffset now = Utc();
        PlayerHiveState state = await repository.ExecuteAtomicallyAsync(playerId, hiveId, s => s with { QuestChain = s.QuestChain ?? NewState() }, ct);
        return Snapshot(state, now);
    }

    // Auto-declaration client (aucun signal serveur naturel pour "le joueur a ouvert la World
    // Map"). Idempotent : ne fait rien si deja vrai, jamais de retour arriere possible.
    public async Task<QuestChainSnapshot> ReportWorldMapVisitedAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        Ensure();
        DateTimeOffset now = Utc();
        PlayerHiveState state = await repository.ExecuteAtomicallyAsync(playerId, hiveId, s =>
        {
            QuestChainState quest = s.QuestChain ?? NewState();
            if (quest.WorldMapVisited) return s with { QuestChain = quest };
            return s with { QuestChain = quest with { Revision = quest.Revision + 1, WorldMapVisited = true } };
        }, ct);
        return Snapshot(state, now);
    }

    public async Task<QuestChainClaimResult> ClaimAsync(Guid playerId, Guid hiveId, string objectiveKey, ClaimQuestChainObjectiveRequest request, CancellationToken ct = default)
    {
        Ensure();
        QuestObjectiveDefinition? definition = Objectives.FirstOrDefault(d => string.Equals(d.Key, objectiveKey, StringComparison.Ordinal));
        if (definition is null || request is null || request.ExpectedRevision < 0 || !ValidKey(request.IdempotencyKey))
            return Fail(playerId, hiveId, "game.invalid_request");

        QuestChainClaimResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            DateTimeOffset now = Utc();
            QuestChainState quest = state.QuestChain ?? NewState();
            string hash = Hash($"claim|{objectiveKey}|{request.ExpectedRevision}");
            if (quest.Receipts.TryGetValue(request.IdempotencyKey, out IdempotencyReceipt? stored))
            {
                result = stored.PayloadHash == hash ? Replay(state, quest, stored, now) : Fail(state, quest, "game.idempotency_conflict", now);
                return state with { QuestChain = quest };
            }
            if (quest.Revision != request.ExpectedRevision)
            { result = Fail(state, quest, "game.revision_conflict", now); return state with { QuestChain = quest }; }
            if (quest.ClaimedObjectiveKeys.Contains(objectiveKey))
            { result = Fail(state, quest, "game.quest_already_claimed", now); return state with { QuestChain = quest }; }
            if (!ObjectiveDone(state, quest, objectiveKey))
            { result = Fail(state, quest, "game.quest_incomplete", now); return state with { QuestChain = quest }; }

            Dictionary<string, ResourceBalance> resources = new(state.Resources, StringComparer.Ordinal);
            ApplyReward(resources, definition.RewardResourceKey, definition.RewardAmount);
            HashSet<string> claimed = new(quest.ClaimedObjectiveKeys, StringComparer.Ordinal) { objectiveKey };
            QuestChainState updatedQuest = quest with { Revision = quest.Revision + 1, ClaimedObjectiveKeys = claimed };
            Dictionary<string, IdempotencyReceipt> receipts = new(updatedQuest.Receipts, StringComparer.Ordinal)
            {
                [request.IdempotencyKey] = new IdempotencyReceipt(hash, true, "game.quest_claimed", null, now, quest.Revision, updatedQuest.Revision, AcceptedAtUtc: now)
            };
            updatedQuest = updatedQuest with { Receipts = receipts };
            PlayerHiveState updated = state with { Resources = resources, QuestChain = updatedQuest };
            result = new(true, "game.quest_claimed", Snapshot(updated, now));
            return updated;
        }, ct);
        return result!;
    }

    private static bool ObjectiveDone(PlayerHiveState state, QuestChainState quest, string objectiveKey) => objectiveKey switch
    {
        "q1_building_upgrade" => state.BuildingLevels.Values.DefaultIfEmpty(0).Max() >= 2,
        "q2_troop_recruit" => (state.DoctrineRoster?.Counts.Values.Sum() ?? 0) > 0,
        "q3_research_complete" => (state.Research?.Completed.Count ?? 0) > 0,
        "q4_world_map_visit" => quest.WorldMapVisited,
        "q5_world_resource_collect" => (state.WorldResourceCollection?.NodeReadyAtUtc.Count ?? 0) > 0,
        _ => false
    };

    private void Ensure()
    {
        if (!o.Enabled) throw new InvalidOperationException("Quest chain is disabled");
    }

    private DateTimeOffset Utc()
    {
        DateTimeOffset now = clock.UtcNow;
        if (now.Offset != TimeSpan.Zero) throw new InvalidDataException("Server clock must be UTC");
        return now;
    }

    private static QuestChainState NewState() => new(0, new HashSet<string>(StringComparer.Ordinal), false, new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal));
    private static bool ValidKey(string? key) => !string.IsNullOrWhiteSpace(key) && key.Trim() == key && key.Length <= 256;
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static void ApplyReward(Dictionary<string, ResourceBalance> resources, string key, long amount)
    {
        if (!resources.TryGetValue(key, out ResourceBalance? balance) || amount <= 0 || balance.Amount < 0 || balance.Capacity < balance.Amount) return;
        long credited = Math.Min(amount, balance.Capacity - balance.Amount);
        resources[key] = balance with { Amount = balance.Amount + credited };
    }

    private QuestChainSnapshot Snapshot(PlayerHiveState state, DateTimeOffset now)
    {
        QuestChainState quest = state.QuestChain ?? NewState();
        List<QuestChainObjectiveReadModel> objectives = Objectives.Select(d =>
        {
            bool done = ObjectiveDone(state, quest, d.Key);
            bool claimed = quest.ClaimedObjectiveKeys.Contains(d.Key);
            return new QuestChainObjectiveReadModel(d.Key, done, claimed, !claimed && done, d.RewardResourceKey, d.RewardAmount);
        }).ToList();
        return new(state.PlayerId, state.HiveId, ContractVersion, quest.Revision, now, objectives);
    }

    private QuestChainClaimResult Fail(Guid playerId, Guid hiveId, string code) =>
        new(false, code, new QuestChainSnapshot(playerId, hiveId, ContractVersion, 0, DateTimeOffset.UnixEpoch, Array.Empty<QuestChainObjectiveReadModel>()));

    private QuestChainClaimResult Fail(PlayerHiveState state, QuestChainState quest, string code, DateTimeOffset now) =>
        new(false, code, Snapshot(state with { QuestChain = quest }, now));

    private QuestChainClaimResult Replay(PlayerHiveState state, QuestChainState quest, IdempotencyReceipt receipt, DateTimeOffset now) =>
        new(receipt.Succeeded, receipt.Code, Snapshot(state with { QuestChain = quest }, now));
}

public sealed record QuestChainState(long Revision, HashSet<string> ClaimedObjectiveKeys, bool WorldMapVisited, Dictionary<string, IdempotencyReceipt> Receipts);
public sealed record QuestChainObjectiveReadModel(string ObjectiveKey, bool Done, bool Claimed, bool CanClaim, string RewardResourceKey, long RewardAmount);
public sealed record QuestChainSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, long Revision, DateTimeOffset ServerTimeUtc, IReadOnlyList<QuestChainObjectiveReadModel> Objectives);
public sealed record ClaimQuestChainObjectiveRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record QuestChainClaimResult(bool Succeeded, string Code, QuestChainSnapshot Snapshot);
