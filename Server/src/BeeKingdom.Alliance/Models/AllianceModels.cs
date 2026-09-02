using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Models;

// M041-CL: Alliance status kept intentionally minimal for Alpha (ACTIVE/DISBANDED) but the enum
// exists specifically so future states (AT_WAR, RESTRUCTURING, SUSPENDED) can be added without
// changing every caller's shape - callers must already switch/pattern-match on this enum.
public enum AllianceStatus
{
    Active = 0,
    Disbanded = 1
}

public enum AllianceJoinMode
{
    Open = 0,
    Application = 1,
    InviteOnly = 2
}

// Alpha roles only (Leader/Officer/Member) - AlliancePermissionPolicy is the single place that
// maps roles to capabilities, so adding custom ranks later only touches that policy, not every
// call site that currently does `if (role == ...)`.
public enum AllianceRole
{
    Member = 0,
    Officer = 1,
    Leader = 2
}

// The Alliance Aggregate Root. Deliberately NOT "a PlayerId with a list of members" - membership
// rows live in their own table/repository (AllianceMembership), and this record only carries the
// Alliance's own identity/lifecycle/leadership summary plus forward-looking reference fields for
// subsystems that don't exist yet (diplomacy, chat, web) so those can be wired in later without
// re-shaping this aggregate. See Docs/Alliance/ALLIANCE_PLATFORM_ARCHITECTURE.md section 2.
public sealed record AllianceEntity
{
    public required AllianceId AllianceId { get; init; }
    public required string Name { get; init; }
    public required string Tag { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Language { get; init; } = "fr-CA";
    public string EmblemKey { get; init; } = string.Empty;
    public required AllianceJoinMode JoinMode { get; init; }
    public required AllianceStatus Status { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required PlayerId CreatedByPlayerId { get; init; }
    public required PlayerId LeaderPlayerId { get; init; }
    public required int MemberCount { get; init; }
    public required int MaxMembers { get; init; }

    // Web navigation slug (beekingdomgame.com/alliance/{slug}) - explicitly NOT the identity;
    // AllianceId is. See AllianceSlugRegistry / slug-lookup contract for renaming behavior.
    public string PublicSlug { get; init; } = string.Empty;

    // Forward reference only - the real chat conversation is owned by BeeKingdom.Chat via the
    // existing "alliance:{allianceId:N}" audience key (LocalChatAudienceResolver). Populated once
    // the conversation is actually created (CreateAllianceAsync), never a second chat system.
    public Guid? ChatConversationId { get; init; }

    public required long Revision { get; init; }
    public DateTimeOffset? DisbandedAtUtc { get; init; }
}

// AllianceMembership: one row per player-in-alliance. A player belongs to at most ONE active
// Alliance at a time - enforced server-side in AllianceService (IAllianceRepository lookups),
// never trusted from the client.
public sealed record AllianceMembership
{
    public required AllianceId AllianceId { get; init; }
    public required PlayerId PlayerId { get; init; }
    public required AllianceRole Role { get; init; }
    public required DateTimeOffset JoinedAtUtc { get; init; }
    public PlayerId? InvitedByPlayerId { get; init; }
    public Guid? ApplicationId { get; init; }
    public DateTimeOffset LastRoleChangedAtUtc { get; init; }
    public DateTimeOffset? RemovedAtUtc { get; init; }
    public required long Revision { get; init; }
}

public enum AllianceApplicationStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Cancelled = 3
}

public sealed record AllianceApplication
{
    public required Guid ApplicationId { get; init; }
    public required AllianceId AllianceId { get; init; }
    public required PlayerId PlayerId { get; init; }
    public required AllianceApplicationStatus Status { get; init; }
    public required DateTimeOffset SubmittedAtUtc { get; init; }
    public DateTimeOffset? RespondedAtUtc { get; init; }
    public PlayerId? RespondedByPlayerId { get; init; }
    public string Message { get; init; } = string.Empty;
}

public enum AllianceInvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Revoked = 3
}

public sealed record AllianceInvitation
{
    public required Guid InvitationId { get; init; }
    public required AllianceId AllianceId { get; init; }
    public required PlayerId InvitedPlayerId { get; init; }
    public required PlayerId InvitedByPlayerId { get; init; }
    public required AllianceInvitationStatus Status { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? RespondedAtUtc { get; init; }
}

// A single, centralized permission surface - AllianceService checks capabilities through this
// instead of scattering `if (role == AllianceRole.Leader || role == AllianceRole.Officer)` across
// every mutation method. Fields left in even for not-yet-active subsystems (diplomacy/war) so the
// policy has one settled shape when those subsystems turn on.
public static class AlliancePermissionPolicy
{
    public static bool CanInvite(AllianceRole role) => role is AllianceRole.Officer or AllianceRole.Leader;
    public static bool CanApproveApplication(AllianceRole role) => role is AllianceRole.Officer or AllianceRole.Leader;
    public static bool CanRejectApplication(AllianceRole role) => role is AllianceRole.Officer or AllianceRole.Leader;
    public static bool CanKickMember(AllianceRole role) => role is AllianceRole.Officer or AllianceRole.Leader;
    public static bool CanPromote(AllianceRole role) => role == AllianceRole.Leader;
    public static bool CanDemote(AllianceRole role) => role == AllianceRole.Leader;
    public static bool CanEditProfile(AllianceRole role) => role is AllianceRole.Officer or AllianceRole.Leader;
    public static bool CanManageDiplomacy(AllianceRole role) => role == AllianceRole.Leader;
    public static bool CanDeclareWar(AllianceRole role) => role == AllianceRole.Leader;
    public static bool CanAcceptPeace(AllianceRole role) => role == AllianceRole.Leader;
    public static bool CanTransferLeadership(AllianceRole role) => role == AllianceRole.Leader;
    public static bool CanDissolve(AllianceRole role) => role == AllianceRole.Leader;

    // A kicker can never outrank/equal-rank their target except the Leader kicking anyone.
    public static bool CanKickTarget(AllianceRole actorRole, AllianceRole targetRole)
        => actorRole == AllianceRole.Leader
            ? targetRole != AllianceRole.Leader
            : actorRole == AllianceRole.Officer && targetRole == AllianceRole.Member;
}
