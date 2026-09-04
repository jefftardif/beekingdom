using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Models;

// M041-CL: this is the central domain for the future Alliance web page. Every alliance-relevant
// happening (membership changes today; combat/research/diplomacy/war later) becomes one of these,
// append-only, ordered by Sequence. The Web page and the Unity window both read the SAME feed -
// never two separate "activity" concepts.
public enum AllianceActivityType
{
    AllianceCreated = 0,
    MemberJoined = 1,
    MemberLeft = 2,
    MemberKicked = 3,
    MemberPromoted = 4,
    MemberDemoted = 5,
    LeadershipTransferred = 6,
    ProfileUpdated = 7,

    // Reserved for future player-gameplay ingestion (IAllianceActivityPublisher) - not emitted by
    // anything yet, infrastructure only. See ALLIANCE_PLATFORM_ARCHITECTURE.md section 10.
    PlayerBuildingUpgraded = 100,
    PlayerResearchCompleted = 101,
    PlayerAttackStarted = 102,
    PlayerAttackWon = 103,
    PlayerAttackLost = 104,
    CreatureDefeated = 105,
    GatheringCompleted = 106,

    // Reserved for the diplomacy/war foundations below.
    AllianceWarDeclared = 200,
    AllianceWarEnded = 201,
    AllianceDiplomacyChanged = 202,

    // Reserved for future subsystems out of scope tonight (territory/buildings/tech).
    AllianceTerritoryCaptured = 300,
    AllianceBuildingUpgraded = 301,
    // M052-CL: reused for funded/launched/completed Alliance Research milestones (Bible section 21)
    // - differentiated via AllianceActivityPayload.Result ("funded"/"launched"/"completed") rather
    // than one new enum value per milestone.
    AllianceTechnologyCompleted = 302,
    // M052-CL: the Chef's own funding-target selection - distinct enough from the technology's own
    // lifecycle milestones above to warrant its own type (Bible section 21's own example sentence:
    // "Stara a désigné « Maîtrise du Miel III » comme nouvel objectif de l'Alliance").
    AllianceResearchFundingTargetSelected = 310
}

// Who is allowed to see this activity entry. The public Web page must only ever be handed entries
// whose Visibility is Public - enforced in the repository's public-feed query, not by trusting the
// caller to filter client-side.
public enum AllianceActivityVisibility
{
    Public = 0,
    MembersOnly = 1,
    OfficersOnly = 2,
    SystemPrivate = 3
}

// Structured payload, never a pre-localized sentence - "Type + Actor + Target + Entity + Level +
// Result + Timestamp" per the mission brief, so Unity and the future Web client each localize it
// in their own language/format from the same data.
public sealed record AllianceActivityPayload
{
    public string? EntityKey { get; init; }
    public string? EntityName { get; init; }
    public int? Level { get; init; }
    public string? Result { get; init; }
    public IReadOnlyDictionary<string, string>? Extra { get; init; }
}

public sealed record AllianceActivityEvent
{
    public required Guid ActivityId { get; init; }
    public required AllianceId AllianceId { get; init; }
    public required AllianceActivityType Type { get; init; }
    public required DateTimeOffset OccurredAtUtc { get; init; }
    public PlayerId? ActorPlayerId { get; init; }
    public PlayerId? TargetPlayerId { get; init; }
    public AllianceId? RelatedAllianceId { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public required AllianceActivityVisibility Visibility { get; init; }
    public AllianceActivityPayload? Payload { get; init; }

    // Monotonic per-alliance ordering key for stable pagination - independent of OccurredAtUtc so
    // two events with the same timestamp still sort deterministically.
    public required long Sequence { get; init; }
}

public sealed record AllianceActivityPage(IReadOnlyList<AllianceActivityEvent> Items, long? NextBeforeSequence);
