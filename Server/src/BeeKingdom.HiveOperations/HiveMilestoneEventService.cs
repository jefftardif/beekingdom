using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.HiveOperations;

public sealed class HiveMilestoneEventOptions
{
    public const string SectionName = "HiveMilestoneEvent";
    public bool Enabled { get; set; }
    public int RequiredObjectiveCount { get; set; } = 3;
    public int WindowDays { get; set; } = 30;
    public long RewardHoney { get; set; } = 800;
    public long RewardPollen { get; set; } = 500;
    public long VipPointsObjectiveThreshold { get; set; } = 50;
    public long TroopCountObjectiveThreshold { get; set; } = 10;
}

public sealed record HiveMilestoneEventState(long Revision, DateTimeOffset WindowStartedAtUtc, DateTimeOffset WindowEndsAtUtc, bool Claimed, Dictionary<string, IdempotencyReceipt> Receipts);
public sealed record HiveMilestoneObjectiveReadModel(string ObjectiveKey, bool Done);
public sealed record HiveMilestoneEventSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, long Revision, DateTimeOffset ServerTimeUtc, DateTimeOffset WindowEndsAtUtc, bool WindowExpired, IReadOnlyList<HiveMilestoneObjectiveReadModel> Objectives, int RequiredObjectiveCount, bool Claimed, bool CanClaim, IReadOnlyDictionary<string, long> Reward);
public sealed record ClaimHiveMilestoneEventRequest(long ExpectedRevision, string IdempotencyKey);
public sealed record HiveMilestoneEventResult(bool Succeeded, string Code, HiveMilestoneEventSnapshot Snapshot);

// Premier "evenement" du jeu (demande de Jeff, 2026-07-31) : un defi ponctuel qui relie
// delibly plusieurs systemes deja construits cette phase plutot que d'en ajouter un isole
// (batiment, recrutement de troupes, collecte mondiale, voie strategique, VIP). Chaque objectif
// est lu directement depuis l'etat deja persiste par son propre systeme - aucune nouvelle
// instrumentation, aucun risque pour les services existants. Volontairement a usage unique (pas
// de reinitialisation hebdomadaire) pour eviter une recompense repetable sans nouvel effort ; la
// fenetre de temps limitee (WindowDays) donne quand meme le caractere "evenement" plutot
// qu'un succes permanent sans urgence.
public sealed class HiveMilestoneEventService(IHiveStateRepository repository, IServerClock clock, HiveMilestoneEventOptions options)
{
    public const string ContractVersion = "living-hive-milestone-event-v1";
    private static readonly string[] ObjectiveKeys = ["building_upgrade", "troop_recruit", "world_resource", "strategic_path", "vip_tier"];
    private readonly HiveMilestoneEventOptions o = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<HiveMilestoneEventSnapshot> ReadAsync(Guid playerId, Guid hiveId, CancellationToken ct = default)
    {
        Ensure();
        DateTimeOffset now = Utc();
        PlayerHiveState state = await repository.ExecuteAtomicallyAsync(playerId, hiveId, s => s with { MilestoneEvent = s.MilestoneEvent ?? NewState(now) }, ct);
        return Snapshot(state, now);
    }

    public async Task<HiveMilestoneEventResult> ClaimAsync(Guid playerId, Guid hiveId, ClaimHiveMilestoneEventRequest request, CancellationToken ct = default)
    {
        Ensure();
        if (request is null || request.ExpectedRevision < 0 || !ValidKey(request.IdempotencyKey))
            return Fail(playerId, hiveId, "game.invalid_request");

        HiveMilestoneEventResult? result = null;
        await repository.ExecuteAtomicallyAsync(playerId, hiveId, state =>
        {
            DateTimeOffset now = Utc();
            HiveMilestoneEventState milestone = state.MilestoneEvent ?? NewState(now);
            string hash = Hash($"claim|{request.ExpectedRevision}");
            if (milestone.Receipts.TryGetValue(request.IdempotencyKey, out IdempotencyReceipt? stored))
            {
                result = stored.PayloadHash == hash ? Replay(state, milestone, stored, now) : Fail(state, milestone, "game.idempotency_conflict", now);
                return state with { MilestoneEvent = milestone };
            }
            if (milestone.Revision != request.ExpectedRevision)
            { result = Fail(state, milestone, "game.revision_conflict", now); return state with { MilestoneEvent = milestone }; }
            if (milestone.Claimed)
            { result = Fail(state, milestone, "game.milestone_already_claimed", now); return state with { MilestoneEvent = milestone }; }
            if (now > milestone.WindowEndsAtUtc)
            { result = Fail(state, milestone, "game.milestone_window_expired", now); return state with { MilestoneEvent = milestone }; }
            if (CompletedObjectiveCount(state) < o.RequiredObjectiveCount)
            { result = Fail(state, milestone, "game.milestone_incomplete", now); return state with { MilestoneEvent = milestone }; }

            Dictionary<string, ResourceBalance> resources = new(state.Resources, StringComparer.Ordinal);
            ApplyReward(resources, "honey", o.RewardHoney);
            ApplyReward(resources, "pollen", o.RewardPollen);
            HiveMilestoneEventState updatedMilestone = milestone with { Revision = milestone.Revision + 1, Claimed = true };
            Dictionary<string, IdempotencyReceipt> receipts = new(updatedMilestone.Receipts, StringComparer.Ordinal)
            {
                [request.IdempotencyKey] = new IdempotencyReceipt(hash, true, "game.milestone_claimed", null, now, milestone.Revision, updatedMilestone.Revision, AcceptedAtUtc: now)
            };
            updatedMilestone = updatedMilestone with { Receipts = receipts };
            PlayerHiveState updated = state with { Resources = resources, MilestoneEvent = updatedMilestone };
            result = new(true, "game.milestone_claimed", Snapshot(updated, now));
            return updated;
        }, ct);
        return result!;
    }

    private int CompletedObjectiveCount(PlayerHiveState state) => ObjectiveKeys.Count(key => ObjectiveDone(state, key));

    private bool ObjectiveDone(PlayerHiveState state, string objectiveKey) => objectiveKey switch
    {
        "building_upgrade" => state.BuildingLevels.Values.DefaultIfEmpty(0).Max() >= 2,
        "troop_recruit" => (state.DoctrineRoster?.Counts.Values.Sum() ?? 0) >= o.TroopCountObjectiveThreshold,
        "world_resource" => (state.WorldResourceCollection?.NodeReadyAtUtc.Count ?? 0) > 0,
        "strategic_path" => state.StrategicPath?.SelectedPath != null,
        "vip_tier" => (state.Vip?.LifetimePoints ?? 0) >= o.VipPointsObjectiveThreshold,
        _ => false
    };

    private void Ensure()
    {
        if (!o.Enabled) throw new InvalidOperationException("Hive milestone event is disabled");
        if (o.RequiredObjectiveCount < 1 || o.RequiredObjectiveCount > ObjectiveKeys.Length) throw new InvalidDataException("Invalid milestone event options");
        if (o.WindowDays < 1 || o.RewardHoney < 0 || o.RewardPollen < 0) throw new InvalidDataException("Invalid milestone event options");
    }

    private DateTimeOffset Utc()
    {
        DateTimeOffset now = clock.UtcNow;
        if (now.Offset != TimeSpan.Zero) throw new InvalidDataException("Server clock must be UTC");
        return now;
    }

    private HiveMilestoneEventState NewState(DateTimeOffset now) => new(0, now, now + TimeSpan.FromDays(o.WindowDays), false, new Dictionary<string, IdempotencyReceipt>(StringComparer.Ordinal));
    private static bool ValidKey(string? key) => !string.IsNullOrWhiteSpace(key) && key.Trim() == key && key.Length <= 256;
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static void ApplyReward(Dictionary<string, ResourceBalance> resources, string key, long amount)
    {
        if (!resources.TryGetValue(key, out ResourceBalance? balance) || amount <= 0 || balance.Amount < 0 || balance.Capacity < balance.Amount) return;
        long credited = Math.Min(amount, balance.Capacity - balance.Amount);
        resources[key] = balance with { Amount = balance.Amount + credited };
    }

    private HiveMilestoneEventSnapshot Snapshot(PlayerHiveState state, DateTimeOffset now)
    {
        HiveMilestoneEventState milestone = state.MilestoneEvent ?? NewState(now);
        List<HiveMilestoneObjectiveReadModel> objectives = ObjectiveKeys.Select(key => new HiveMilestoneObjectiveReadModel(key, ObjectiveDone(state, key))).ToList();
        bool expired = now > milestone.WindowEndsAtUtc;
        bool canClaim = !milestone.Claimed && !expired && CompletedObjectiveCount(state) >= o.RequiredObjectiveCount;
        Dictionary<string, long> reward = new(StringComparer.Ordinal) { ["honey"] = o.RewardHoney, ["pollen"] = o.RewardPollen };
        return new(state.PlayerId, state.HiveId, ContractVersion, milestone.Revision, now, milestone.WindowEndsAtUtc, expired, objectives, o.RequiredObjectiveCount, milestone.Claimed, canClaim, reward);
    }

    private HiveMilestoneEventResult Fail(Guid playerId, Guid hiveId, string code) =>
        new(false, code, new HiveMilestoneEventSnapshot(playerId, hiveId, ContractVersion, 0, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, false, Array.Empty<HiveMilestoneObjectiveReadModel>(), o.RequiredObjectiveCount, false, false, new Dictionary<string, long>()));

    private HiveMilestoneEventResult Fail(PlayerHiveState state, HiveMilestoneEventState milestone, string code, DateTimeOffset now) =>
        new(false, code, Snapshot(state with { MilestoneEvent = milestone }, now));

    private HiveMilestoneEventResult Replay(PlayerHiveState state, HiveMilestoneEventState milestone, IdempotencyReceipt receipt, DateTimeOffset now) =>
        new(receipt.Succeeded, receipt.Code, Snapshot(state with { MilestoneEvent = milestone }, now));
}
