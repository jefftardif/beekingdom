using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

// Covers the Alliance aggregate itself plus its tightly-coupled membership/application/invitation
// rows - mirrors IChatRepository's shape (one cohesive interface for a related cluster of
// entities) rather than one interface per table. Diplomacy/Activity/War get their own interfaces
// below since those are genuinely separate subdomains that can evolve independently.
public interface IAllianceRepository
{
    AllianceEntity Save(AllianceEntity alliance);
    AllianceEntity? Get(AllianceId allianceId);
    AllianceEntity? GetBySlug(string slug);
    IReadOnlyList<AllianceEntity> Search(AllianceSearchQuery query, out int totalCount);

    // Idempotency receipts for Create - keyed by creator PlayerId + ClientRequestId, same pattern
    // as ChatConversationCreationReceipt.
    AllianceId? GetCreateReceipt(PlayerId playerId, string clientRequestId);
    void SaveCreateReceipt(PlayerId playerId, string clientRequestId, AllianceId allianceId);

    // Membership
    AllianceMembership SaveMembership(AllianceMembership membership);
    AllianceMembership? GetActiveMembership(AllianceId allianceId, PlayerId playerId);
    AllianceMembership? GetActiveMembershipForPlayer(PlayerId playerId);
    IReadOnlyList<AllianceMembership> ListActiveMembers(AllianceId allianceId);

    // Applications
    AllianceApplication SaveApplication(AllianceApplication application);
    AllianceApplication? GetApplication(Guid applicationId);
    AllianceApplication? GetPendingApplication(AllianceId allianceId, PlayerId playerId);
    IReadOnlyList<AllianceApplication> ListPendingApplications(AllianceId allianceId);
    // M056-CL: by-PLAYER counterpart of ListPendingApplications, needed by the Admin-gated account
    // deletion cascade - without it a deleted account's pending application would keep sitting in
    // some other alliance's applications list forever, pointing at a player id that no longer
    // resolves. Read-only helper; the cascade cancels what it finds through SaveApplication.
    IReadOnlyList<AllianceApplication> ListPendingApplicationsForPlayer(PlayerId playerId);
    Guid? GetApplicationReceipt(PlayerId playerId, string clientRequestId);
    void SaveApplicationReceipt(PlayerId playerId, string clientRequestId, Guid applicationId);

    // Invitations
    AllianceInvitation SaveInvitation(AllianceInvitation invitation);
    AllianceInvitation? GetInvitation(Guid invitationId);
    AllianceInvitation? GetPendingInvitation(AllianceId allianceId, PlayerId invitedPlayerId);
    IReadOnlyList<AllianceInvitation> ListPendingInvitationsForPlayer(PlayerId playerId);
    Guid? GetInvitationReceipt(PlayerId playerId, string clientRequestId);
    void SaveInvitationReceipt(PlayerId playerId, string clientRequestId, Guid invitationId);
}
