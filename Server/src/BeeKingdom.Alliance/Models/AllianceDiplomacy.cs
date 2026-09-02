using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Models;

// M041-CL: NEUTRAL is deliberately NOT a stored row - it's the absence of any
// AllianceDiplomaticRelation between two alliances. Storing a row per pair for the default state
// would mean writing O(n^2) rows for every alliance that exists; instead the repository's lookup
// returns null and the service treats "no relation found" as Neutral. Every other relation type
// (NAP/Ally/Hostile/War) is an explicit row.
public enum AllianceRelationType
{
    Neutral = 0,
    NonAggressionPact = 1,
    Ally = 2,
    Hostile = 3,
    War = 4
}

public enum AllianceRelationStatus
{
    Proposed = 0,
    Active = 1,
    Ended = 2,
    Rejected = 3,
    Cancelled = 4
}

// Relation rows are stored with a canonical ordering (AllianceIdA < AllianceIdB by Guid comparison,
// see AllianceDiplomacyService.CanonicalPair) so there's exactly one row per pair regardless of who
// proposed it - avoids duplicate/conflicting rows for the same relationship.
public sealed record AllianceDiplomaticRelation
{
    public required Guid RelationId { get; init; }
    public required AllianceId AllianceIdA { get; init; }
    public required AllianceId AllianceIdB { get; init; }
    public required AllianceRelationType RelationType { get; init; }
    public required AllianceRelationStatus Status { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset UpdatedAtUtc { get; init; }
    public required AllianceId InitiatedByAllianceId { get; init; }
    public required long Revision { get; init; }
}

public enum AllianceWarStatus
{
    Declared = 0,
    Active = 1,
    Ended = 2,
    Cancelled = 3
}

// A war is a relationship between two Alliance aggregates, not "player A attacks player B" - see
// mission brief "WAR = RELATION BETWEEN ALLIANCE ENTITIES". Individual future combat reports
// reference WarId; this record itself carries no battle mechanics, scoring, or rewards.
public sealed record AllianceWar
{
    public required Guid WarId { get; init; }
    public required AllianceId AttackerAllianceId { get; init; }
    public required AllianceId DefenderAllianceId { get; init; }
    public required AllianceWarStatus Status { get; init; }
    public required DateTimeOffset DeclaredAtUtc { get; init; }
    public DateTimeOffset? StartedAtUtc { get; init; }
    public DateTimeOffset? EndedAtUtc { get; init; }
    public AllianceId? WinnerAllianceId { get; init; }
    public required long Revision { get; init; }
}
