using System.Text.Json.Serialization;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Models;

// Requests/results for AllianceService. Kept in one file deliberately - these are thin data
// carriers, not behavior; splitting them across many files would only add navigation overhead.

public sealed record CreateAllianceRequest(string Name, string Tag, string Description, string Language, string EmblemKey, AllianceJoinMode JoinMode, string ClientRequestId);
public sealed record CreateAllianceResult(AllianceEntity Alliance, bool Deduplicated);

// M043-CL: the ONLY way a client can discover "what alliance am I in, and what is my own role in
// it" without already knowing an AllianceId - every other read endpoint requires one.
public sealed record MyAllianceOverview(AllianceEntity Alliance, AllianceMembership Membership);

// Always 200 OK with HasAlliance=false rather than a bare JSON null body - NO_ALLIANCE is a normal,
// expected state, not an error, and a literal null response body is awkward for typed JSON clients
// (the Unity codec's Deserialize<T> explicitly rejects a null payload as malformed).
public sealed record MyAllianceOverviewResponse(bool HasAlliance, AllianceEntity? Alliance, AllianceMembership? Membership)
{
    public static readonly MyAllianceOverviewResponse None = new(false, null, null);
    public static MyAllianceOverviewResponse From(MyAllianceOverview? overview) =>
        overview == null ? None : new MyAllianceOverviewResponse(true, overview.Alliance, overview.Membership);
}

public sealed record AllianceSearchQuery(string? NameOrTag, string? Language, AllianceJoinMode? JoinMode, int Offset, int Limit);
public sealed record AlliancePublicSummary(AllianceId AllianceId, string Name, string Tag, string EmblemKey, string Language, AllianceJoinMode JoinMode, int MemberCount, int MaxMembers, string PublicSlug);
public sealed record AllianceSearchPage(IReadOnlyList<AlliancePublicSummary> Items, int TotalCount);

public sealed record JoinOpenAllianceResult(AllianceEntity Alliance, AllianceMembership Membership);

public sealed record SubmitApplicationRequest(string Message, string ClientRequestId);
public sealed record ApplicationDecisionResult(AllianceApplication Application, AllianceMembership? Membership);

// M043B-CL: real DisplayName for the Leader/Officer application review UI - batch-resolved
// server-side (see AllianceService.ListPendingApplicationsForMyAlliance), same pattern as
// AllianceMemberSummary.
public sealed record AllianceApplicationView(Guid ApplicationId, AllianceId AllianceId, PlayerId PlayerId, string DisplayName, AllianceApplicationStatus Status, DateTimeOffset SubmittedAtUtc, string Message);

// M043S-CL: PlayerId has no [JsonConverter] of its own (deliberately - see Identifiers.cs), so
// System.Text.Json's default handling of this record struct expects an object shape
// ({"invitedPlayerId":{"value":"<guid>"}}), but AllianceClient's CreateInvitationWireRequest (like
// every other client-side request DTO in this codebase) sends InvitedPlayerId as a bare GUID
// string. Deserializing that bare string into a PlayerId with no converter threw inside ASP.NET's
// own request-body binding - before ExecuteAlliance's try/catch ever ran - so every real
// invitation attempt failed with .NET's generic error shape (which the client can't parse as an
// AllianceErrorEnvelope, hence the "game.rejected" fallback) instead of ever reaching
// AllianceService.CreateInvitation. Proven via a production AllianceInvitations table that was
// completely empty despite the CEO's real invite attempts.
public sealed record CreateInvitationRequest([property: JsonConverter(typeof(PlayerIdJsonConverter))] PlayerId InvitedPlayerId, string ClientRequestId);
public sealed record InvitationDecisionResult(AllianceInvitation Invitation, AllianceMembership? Membership);

public sealed record UpdateAllianceProfileRequest(string? Description, string? Language, string? EmblemKey, AllianceJoinMode? JoinMode, long ExpectedRevision);

public sealed record LeadershipTransferResult(AllianceEntity Alliance, AllianceMembership PreviousLeader, AllianceMembership NewLeader);

// --- Diplomacy ---
public sealed record ProposeDiplomacyRequest(AllianceRelationType RelationType, string ClientRequestId);
public sealed record DiplomacyDecisionResult(AllianceDiplomaticRelation Relation);

// --- War ---
public sealed record DeclareWarRequest(AllianceId DefenderAllianceId, string ClientRequestId);
public sealed record DeclareWarResult(AllianceWar War);

// --- Public Web contract (AlliancePublicProfile) ---
// Deliberately excludes anything private: no pending invitations, no applications, no
// officer-only action metadata. This is the shape the future beekingdomgame.com/alliance/{slug}
// page (and any authenticated Web manager built on top of it later) is designed against.
public sealed record AllianceLeaderSummary(PlayerId PlayerId, string DisplayName);

public sealed record AllianceDiplomacySummary(int AllyCount, int NonAggressionPactCount, int HostileCount, int ActiveWarCount);

// Member-visible only (not part of AlliancePublicProfile) - the roster itself is not public Web
// data per the mission brief's public/private split, even though aggregate counts are.
// M043B-CL: DisplayName added - resolved server-side (batch, via IPlayerDirectoryService) so Unity
// never has to do N+1 lookups per member. Falls back to string.Empty if no account record resolves
// (should not happen in practice - every active membership implies a real account) so the client can
// decide its own fallback (e.g. a shortened PlayerId) rather than the server fabricating a name.
public sealed record AllianceMemberSummary(PlayerId PlayerId, string DisplayName, AllianceRole Role, DateTimeOffset JoinedAtUtc);

public sealed record AlliancePublicProfile
{
    public required AllianceId AllianceId { get; init; }
    public required string Name { get; init; }
    public required string Tag { get; init; }
    public required string Description { get; init; }
    public required string Language { get; init; }
    public required string EmblemKey { get; init; }
    public required int MemberCount { get; init; }
    public required int MaxMembers { get; init; }
    public required AllianceLeaderSummary Leader { get; init; }
    public required AllianceStatus Status { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required AllianceJoinMode JoinMode { get; init; }
    public required string PublicSlug { get; init; }
    public AllianceDiplomacySummary? Diplomacy { get; init; }
}
