using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Research;

// M051-CL: Alliance Research belongs to the ALLIANCE, never to a member's own PlayerHiveState -
// one AllianceResearchState per Alliance, shared and visible identically by every current member,
// exactly the product requirement ("if Jeff donates, Stara must see the increased progress").
// Persisted as a single JSON-blob-per-aggregate row (mirrors PlayerHiveState/SqlHiveStateRepository
// - the same pattern already proven for the codebase's highest-contention state), guarded by a real
// exclusive SQL app-lock per AllianceId rather than optimistic-revision retries, so two concurrent
// donations to the same Alliance simply serialize - no lost update, no double-completion possible.

public sealed record AllianceTechnologyProgress(
    string TechnologyId,
    long CurrentProgress,
    bool Completed,
    DateTimeOffset? CompletedAtUtc);

// One row per player who has ever donated to this Alliance's research - kept even after the player
// leaves (historical contribution), per the mission's "do not permanently bake bonuses into personal
// stats" requirement being about BONUSES, not about erasing a player's own contribution history.
public sealed record AllianceResearchContribution(
    Guid PlayerId,
    long TotalPoints,
    long DonationCount);

public sealed record AllianceResearchState(
    Guid AllianceId,
    int ModelVersion,
    long Revision,
    Dictionary<string, AllianceTechnologyProgress> Technologies,
    Dictionary<Guid, AllianceResearchContribution> Contributions,
    // Idempotency for the Alliance-side progress increment step of a donation - keyed by
    // "{playerId:N}:{clientRequestId}". The player-side resource debit has its own, separate
    // idempotency guard (PlayerHiveState.Receipts, same mechanism every other paid action in this
    // codebase already uses) - this set exists because a donation crosses two aggregates and each
    // side must independently refuse to double-apply a retried request.
    HashSet<string> ProcessedDonationIds)
{
    public static AllianceResearchState Empty(Guid allianceId) => new(
        allianceId, ModelVersion: 1, Revision: 0,
        new Dictionary<string, AllianceTechnologyProgress>(StringComparer.Ordinal),
        new Dictionary<Guid, AllianceResearchContribution>(),
        new HashSet<string>(StringComparer.Ordinal));
}

public sealed record DonateToAllianceResearchCommand(Guid HiveId, string TechnologyId, string ClientRequestId);

public sealed record AllianceResearchDonateResult(bool Succeeded, string Code, AllianceResearchReadSnapshot? Snapshot);

public sealed record AllianceTechnologyReadModel(
    string TechnologyId,
    string Branch,
    int Tier,
    string DisplayNameKey,
    string DescriptionKey,
    string BonusSummaryKey,
    long RequiredProgress,
    long CurrentProgress,
    bool Completed,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<string> PrerequisiteIds,
    bool Locked,
    bool Available,
    IReadOnlyDictionary<string, long> DonationCost,
    long DonationProgressPerDonation);

public sealed record AllianceResearchReadSnapshot(
    Guid AllianceId,
    string ContractVersion,
    DateTimeOffset ServerTimeUtc,
    long Revision,
    IReadOnlyList<AllianceTechnologyReadModel> Technologies,
    long MyContributionPoints,
    long MyDonationCount);

// Aggregated, already-resolved bonus a player currently receives from their Alliance's completed
// research - resolved fresh every time (never cached/baked), so a player who leaves their Alliance
// or joins a new one sees the correct bonus on the very next resolve, per the mission's explicit
// membership semantics requirement.
public sealed record AllianceResearchBonus(long ProductionBp, long CapacityBp, long CombatPowerBp)
{
    public static readonly AllianceResearchBonus None = new(0, 0, 0);
}
