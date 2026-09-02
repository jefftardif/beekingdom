using BeeKingdom.Accounts;
using BeeKingdom.Alliance.Configuration;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.Chat;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Alliance;

// M041-CL: server-authoritative Alliance domain service. Exception vocabulary mirrors ChatService/
// HiveOperationService so Program.cs can reuse the same try/catch → HTTP status mapping style
// already established for the rest of the game API:
//   ArgumentException            -> 400 invalid_request
//   UnauthorizedAccessException  -> 403 forbidden
//   KeyNotFoundException         -> 404 not_found
//   InvalidOperationException.Message is a stable machine code (e.g. "revision_conflict",
//     "idempotency_conflict", "already_in_alliance", "alliance_disabled", "capacity_full",
//     "duplicate_active_war", "not_a_member", "insufficient_permission") -> mapped per-code.
public sealed class AllianceService
{
    private readonly IAllianceRepository repository;
    private readonly IAllianceActivityRepository activityRepository;
    private readonly IAllianceDiplomacyRepository diplomacyRepository;
    private readonly IAllianceWarRepository warRepository;
    private readonly IOptions<AllianceOptions> options;
    private readonly ChatManager? chatManager;
    private readonly IChatRepository? chatRepository;
    private readonly IPlayerDirectoryService? playerDirectory;

    public AllianceService(
        IAllianceRepository repository,
        IAllianceActivityRepository activityRepository,
        IAllianceDiplomacyRepository diplomacyRepository,
        IAllianceWarRepository warRepository,
        IOptions<AllianceOptions> options,
        ChatManager? chatManager = null,
        IChatRepository? chatRepository = null,
        IPlayerDirectoryService? playerDirectory = null)
    {
        this.repository = repository;
        this.activityRepository = activityRepository;
        this.diplomacyRepository = diplomacyRepository;
        this.warRepository = warRepository;
        this.options = options;
        // M042-CL: optional and nullable on purpose - every pre-existing AllianceServiceTests
        // constructor call omits these, and must keep working unchanged (chat linking/sync just
        // silently no-ops when either is absent, it never throws). DI (AddBeeKingdomAlliance)
        // always supplies both for the real running server.
        this.chatManager = chatManager;
        this.chatRepository = chatRepository;
        // M043B-CL: same optional/nullable pattern - display names fall back to empty string (never
        // fabricated) when absent, DI always supplies the real one.
        this.playerDirectory = playerDirectory;
    }

    private AllianceOptions O => options.Value;

    private void RequireEnabled()
    {
        if (!O.Enabled) throw new InvalidOperationException("alliance_disabled");
    }

    // ---------------- Create ----------------

    public CreateAllianceResult CreateAlliance(PlayerId actorPlayerId, CreateAllianceRequest request)
    {
        RequireEnabled();
        if (string.IsNullOrWhiteSpace(request.ClientRequestId) || request.ClientRequestId.Length > 128)
            throw new ArgumentException("invalid_request");

        AllianceId? existingId = repository.GetCreateReceipt(actorPlayerId, request.ClientRequestId);
        if (existingId.HasValue)
        {
            AllianceEntity? existing = repository.Get(existingId.Value) ?? throw new KeyNotFoundException("not_found");
            return new CreateAllianceResult(existing, Deduplicated: true);
        }

        if (repository.GetActiveMembershipForPlayer(actorPlayerId) != null)
            throw new InvalidOperationException("already_in_alliance");

        string name = (request.Name ?? string.Empty).Trim();
        string tag = (request.Tag ?? string.Empty).Trim();
        if (name.Length < O.NameMinLength || name.Length > O.NameMaxLength) throw new ArgumentException("invalid_request");
        if (tag.Length < O.TagMinLength || tag.Length > O.TagMaxLength) throw new ArgumentException("invalid_request");
        if ((request.Description ?? string.Empty).Length > O.DescriptionMaxLength) throw new ArgumentException("invalid_request");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var allianceId = AllianceId.New();
        AllianceEntity alliance = new()
        {
            AllianceId = allianceId,
            Name = name,
            Tag = tag,
            Description = request.Description ?? string.Empty,
            Language = string.IsNullOrWhiteSpace(request.Language) ? "fr-CA" : request.Language,
            EmblemKey = request.EmblemKey ?? string.Empty,
            JoinMode = request.JoinMode,
            Status = AllianceStatus.Active,
            CreatedAtUtc = now,
            CreatedByPlayerId = actorPlayerId,
            LeaderPlayerId = actorPlayerId,
            MemberCount = 1,
            MaxMembers = O.MaxMembers,
            PublicSlug = BuildUniqueSlug(name),
            Revision = 1
        };
        repository.Save(alliance);
        repository.SaveCreateReceipt(actorPlayerId, request.ClientRequestId, allianceId);

        repository.SaveMembership(new AllianceMembership
        {
            AllianceId = allianceId,
            PlayerId = actorPlayerId,
            Role = AllianceRole.Leader,
            JoinedAtUtc = now,
            LastRoleChangedAtUtc = now,
            Revision = 1
        });

        // Part 3A: create/resolve the real alliance chat conversation and record its real id.
        // This whole block is reached exactly once per real creation (a retried CreateAlliance
        // call returns early above, via the create receipt, before ever getting here) - Chat's
        // own idempotency on top is a second, redundant safety net, not the only one.
        Guid? chatConversationId = CreateOrLinkAllianceChat(actorPlayerId, allianceId, name, request.ClientRequestId);
        if (chatConversationId.HasValue)
        {
            alliance = repository.Save(alliance with { ChatConversationId = chatConversationId.Value });
        }

        Publish(allianceId, AllianceActivityType.AllianceCreated, now, actorPlayerId, null, AllianceActivityVisibility.Public, null);

        return new CreateAllianceResult(alliance, Deduplicated: false);
    }

    private Guid? CreateOrLinkAllianceChat(PlayerId creatorPlayerId, AllianceId allianceId, string allianceName, string allianceClientRequestId)
    {
        if (chatManager == null) return null;
        try
        {
            CreateChatConversationResult result = chatManager.CreateConversation(creatorPlayerId, new CreateChatConversationRequest(
                ChatChannelType.Alliance,
                ParseConfiguredGuid(O.GameServerId),
                ParseConfiguredGuid(O.WorldId),
                "alliance:" + allianceId.Value.ToString("N"),
                allianceName,
                Array.Empty<Guid>(),
                "alliance-chat-" + allianceClientRequestId));
            return result.Conversation.ConversationId;
        }
        catch (Exception)
        {
            // Chat linking must never block Alliance creation itself - the Alliance is real and
            // saved regardless; a missing ChatConversationId just means the chat tab has nothing
            // to show yet (same graceful-degradation posture as the rest of this service toward
            // optional dependencies).
            return null;
        }
    }

    private static Guid ParseConfiguredGuid(string value) => Guid.TryParse(value, out Guid parsed) ? parsed : Guid.Empty;

    // Part 3B: keep the alliance's real chat conversation participants in lockstep with real
    // membership changes. Every call site below is best-effort (swallows exceptions) for the
    // same reason as chat creation itself - a chat sync failure must never roll back or block a
    // membership change that has already genuinely happened.
    private void SyncChatParticipantAdded(Guid? chatConversationId, PlayerId playerId, AllianceRole role, DateTimeOffset joinedAtUtc)
    {
        if (chatRepository == null || chatConversationId is not { } conversationId || conversationId == Guid.Empty) return;
        try
        {
            chatRepository.UpsertParticipant(new ChatConversationParticipant(
                conversationId, playerId, ToChatRole(role), joinedAtUtc, RemovedAtUtc: null, CanRead: true, CanWrite: true));
        }
        catch (Exception) { /* best-effort - see class-level note */ }
    }

    private void SyncChatParticipantRemoved(Guid? chatConversationId, PlayerId playerId, DateTimeOffset removedAtUtc)
    {
        if (chatRepository == null || chatConversationId is not { } conversationId || conversationId == Guid.Empty) return;
        try { chatRepository.RemoveParticipant(conversationId, playerId, removedAtUtc); }
        catch (Exception) { /* best-effort - see class-level note */ }
    }

    private static ChatPermissionRole ToChatRole(AllianceRole role) => role switch
    {
        AllianceRole.Leader => ChatPermissionRole.Leader,
        AllianceRole.Officer => ChatPermissionRole.Officer,
        _ => ChatPermissionRole.Member
    };

    private string BuildUniqueSlug(string name)
    {
        string baseSlug = Slugify(name);
        if (repository.GetBySlug(baseSlug) == null) return baseSlug;
        for (int i = 2; i < 1000; i++)
        {
            string candidate = $"{baseSlug}-{i}";
            if (repository.GetBySlug(candidate) == null) return candidate;
        }
        return $"{baseSlug}-{Guid.NewGuid():N}"[..40];
    }

    private static string Slugify(string value)
    {
        char[] chars = value.Trim().ToLowerInvariant().ToCharArray();
        var builder = new System.Text.StringBuilder(chars.Length);
        bool lastWasHyphen = false;
        foreach (char c in chars)
        {
            if (char.IsLetterOrDigit(c)) { builder.Append(c); lastWasHyphen = false; }
            else if (!lastWasHyphen && builder.Length > 0) { builder.Append('-'); lastWasHyphen = true; }
        }
        string result = builder.ToString().Trim('-');
        return string.IsNullOrEmpty(result) ? Guid.NewGuid().ToString("N")[..8] : result;
    }

    // ---------------- Search / discovery ----------------

    public AllianceSearchPage Search(AllianceSearchQuery query)
    {
        RequireEnabled();
        int limit = Math.Clamp(query.Limit, 1, O.SearchPageMaxLimit);
        IReadOnlyList<AllianceEntity> page = repository.Search(query with { Limit = limit }, out int totalCount);
        return new AllianceSearchPage(
            page.Select(ToPublicSummary).ToArray(),
            totalCount);
    }

    private static AlliancePublicSummary ToPublicSummary(AllianceEntity a)
        => new(a.AllianceId, a.Name, a.Tag, a.EmblemKey, a.Language, a.JoinMode, a.MemberCount, a.MaxMembers, a.PublicSlug);

    // ---------------- Join (open) ----------------

    public JoinOpenAllianceResult JoinOpen(PlayerId actorPlayerId, AllianceId allianceId)
    {
        RequireEnabled();
        if (repository.GetActiveMembershipForPlayer(actorPlayerId) != null)
            throw new InvalidOperationException("already_in_alliance");

        AllianceEntity alliance = RequireActiveAlliance(allianceId);
        if (alliance.JoinMode != AllianceJoinMode.Open)
            throw new InvalidOperationException("invalid_request");

        // Capacity/race-safety: re-check membership + count under the same call, and rely on the
        // repository's own locking for the actual mutation - a lost race here just means the
        // count read is stale by the time Save() runs, so re-derive count from real membership
        // rows rather than trusting alliance.MemberCount blindly.
        int currentCount = repository.ListActiveMembers(allianceId).Count;
        if (currentCount >= alliance.MaxMembers) throw new InvalidOperationException("capacity_full");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AllianceMembership membership = repository.SaveMembership(new AllianceMembership
        {
            AllianceId = allianceId,
            PlayerId = actorPlayerId,
            Role = AllianceRole.Member,
            JoinedAtUtc = now,
            LastRoleChangedAtUtc = now,
            Revision = 1
        });

        AllianceEntity updated = repository.Save(alliance with { MemberCount = repository.ListActiveMembers(allianceId).Count, Revision = alliance.Revision + 1 });
        SyncChatParticipantAdded(updated.ChatConversationId, actorPlayerId, AllianceRole.Member, now);
        Publish(allianceId, AllianceActivityType.MemberJoined, now, actorPlayerId, null, AllianceActivityVisibility.Public, null);
        return new JoinOpenAllianceResult(updated, membership);
    }

    // ---------------- Applications ----------------

    public AllianceApplication SubmitApplication(PlayerId actorPlayerId, AllianceId allianceId, SubmitApplicationRequest request)
    {
        RequireEnabled();
        if (string.IsNullOrWhiteSpace(request.ClientRequestId)) throw new ArgumentException("invalid_request");
        Guid? existingId = repository.GetApplicationReceipt(actorPlayerId, request.ClientRequestId);
        if (existingId.HasValue) return repository.GetApplication(existingId.Value) ?? throw new KeyNotFoundException("not_found");

        if (repository.GetActiveMembershipForPlayer(actorPlayerId) != null) throw new InvalidOperationException("already_in_alliance");
        AllianceEntity alliance = RequireActiveAlliance(allianceId);
        if (alliance.JoinMode != AllianceJoinMode.Application) throw new InvalidOperationException("invalid_request");
        if (repository.GetPendingApplication(allianceId, actorPlayerId) != null) throw new InvalidOperationException("already_applied");

        var application = new AllianceApplication
        {
            ApplicationId = Guid.NewGuid(),
            AllianceId = allianceId,
            PlayerId = actorPlayerId,
            Status = AllianceApplicationStatus.Pending,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            Message = (request.Message ?? string.Empty).Length > 280 ? request.Message![..280] : request.Message ?? string.Empty
        };
        repository.SaveApplication(application);
        repository.SaveApplicationReceipt(actorPlayerId, request.ClientRequestId, application.ApplicationId);
        return application;
    }

    public AllianceApplication CancelApplication(PlayerId actorPlayerId, Guid applicationId)
    {
        AllianceApplication application = repository.GetApplication(applicationId) ?? throw new KeyNotFoundException("not_found");
        if (application.PlayerId != actorPlayerId) throw new UnauthorizedAccessException();
        if (application.Status != AllianceApplicationStatus.Pending) throw new InvalidOperationException("invalid_state");
        AllianceApplication cancelled = application with { Status = AllianceApplicationStatus.Cancelled, RespondedAtUtc = DateTimeOffset.UtcNow };
        repository.SaveApplication(cancelled);
        return cancelled;
    }

    public ApplicationDecisionResult AcceptApplication(PlayerId actorPlayerId, Guid applicationId)
    {
        AllianceApplication application = repository.GetApplication(applicationId) ?? throw new KeyNotFoundException("not_found");
        if (application.Status != AllianceApplicationStatus.Pending)
        {
            // Idempotent-friendly: if already accepted, return the existing membership rather than erroring.
            if (application.Status == AllianceApplicationStatus.Accepted)
                return new ApplicationDecisionResult(application, repository.GetActiveMembership(application.AllianceId, application.PlayerId));
            throw new InvalidOperationException("invalid_state");
        }

        AllianceMembership actor = RequireMembership(application.AllianceId, actorPlayerId);
        if (!AlliancePermissionPolicy.CanApproveApplication(actor.Role)) throw new UnauthorizedAccessException();
        if (repository.GetActiveMembershipForPlayer(application.PlayerId) != null) throw new InvalidOperationException("already_in_alliance");

        AllianceEntity alliance = RequireActiveAlliance(application.AllianceId);
        int currentCount = repository.ListActiveMembers(application.AllianceId).Count;
        if (currentCount >= alliance.MaxMembers) throw new InvalidOperationException("capacity_full");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AllianceApplication accepted = application with { Status = AllianceApplicationStatus.Accepted, RespondedAtUtc = now, RespondedByPlayerId = actorPlayerId };
        repository.SaveApplication(accepted);

        AllianceMembership membership = repository.SaveMembership(new AllianceMembership
        {
            AllianceId = application.AllianceId,
            PlayerId = application.PlayerId,
            Role = AllianceRole.Member,
            JoinedAtUtc = now,
            LastRoleChangedAtUtc = now,
            ApplicationId = applicationId,
            Revision = 1
        });
        AllianceEntity updatedAlliance = repository.Save(alliance with { MemberCount = repository.ListActiveMembers(application.AllianceId).Count, Revision = alliance.Revision + 1 });
        SyncChatParticipantAdded(updatedAlliance.ChatConversationId, application.PlayerId, AllianceRole.Member, now);
        Publish(application.AllianceId, AllianceActivityType.MemberJoined, now, application.PlayerId, null, AllianceActivityVisibility.Public, null);
        return new ApplicationDecisionResult(accepted, membership);
    }

    public ApplicationDecisionResult RejectApplication(PlayerId actorPlayerId, Guid applicationId)
    {
        AllianceApplication application = repository.GetApplication(applicationId) ?? throw new KeyNotFoundException("not_found");
        if (application.Status != AllianceApplicationStatus.Pending)
        {
            if (application.Status == AllianceApplicationStatus.Rejected) return new ApplicationDecisionResult(application, null);
            throw new InvalidOperationException("invalid_state");
        }
        AllianceMembership actor = RequireMembership(application.AllianceId, actorPlayerId);
        if (!AlliancePermissionPolicy.CanRejectApplication(actor.Role)) throw new UnauthorizedAccessException();

        AllianceApplication rejected = application with { Status = AllianceApplicationStatus.Rejected, RespondedAtUtc = DateTimeOffset.UtcNow, RespondedByPlayerId = actorPlayerId };
        repository.SaveApplication(rejected);
        return new ApplicationDecisionResult(rejected, null);
    }

    // ---------------- Invitations ----------------

    public AllianceInvitation CreateInvitation(PlayerId actorPlayerId, AllianceId allianceId, CreateInvitationRequest request)
    {
        RequireEnabled();
        if (string.IsNullOrWhiteSpace(request.ClientRequestId)) throw new ArgumentException("invalid_request");
        Guid? existingId = repository.GetInvitationReceipt(actorPlayerId, request.ClientRequestId);
        if (existingId.HasValue) return repository.GetInvitation(existingId.Value) ?? throw new KeyNotFoundException("not_found");

        AllianceMembership actor = RequireMembership(allianceId, actorPlayerId);
        if (!AlliancePermissionPolicy.CanInvite(actor.Role)) throw new UnauthorizedAccessException();
        if (repository.GetActiveMembershipForPlayer(request.InvitedPlayerId) != null) throw new InvalidOperationException("target_already_in_alliance");
        if (repository.GetPendingInvitation(allianceId, request.InvitedPlayerId) != null) throw new InvalidOperationException("already_invited");

        var invitation = new AllianceInvitation
        {
            InvitationId = Guid.NewGuid(),
            AllianceId = allianceId,
            InvitedPlayerId = request.InvitedPlayerId,
            InvitedByPlayerId = actorPlayerId,
            Status = AllianceInvitationStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        repository.SaveInvitation(invitation);
        repository.SaveInvitationReceipt(actorPlayerId, request.ClientRequestId, invitation.InvitationId);
        return invitation;
    }

    public InvitationDecisionResult AcceptInvitation(PlayerId actorPlayerId, Guid invitationId)
    {
        AllianceInvitation invitation = repository.GetInvitation(invitationId) ?? throw new KeyNotFoundException("not_found");
        if (invitation.InvitedPlayerId != actorPlayerId) throw new UnauthorizedAccessException();
        if (invitation.Status != AllianceInvitationStatus.Pending)
        {
            if (invitation.Status == AllianceInvitationStatus.Accepted)
                return new InvitationDecisionResult(invitation, repository.GetActiveMembership(invitation.AllianceId, actorPlayerId));
            throw new InvalidOperationException("invalid_state");
        }
        if (repository.GetActiveMembershipForPlayer(actorPlayerId) != null) throw new InvalidOperationException("already_in_alliance");

        AllianceEntity alliance = RequireActiveAlliance(invitation.AllianceId);
        int currentCount = repository.ListActiveMembers(invitation.AllianceId).Count;
        if (currentCount >= alliance.MaxMembers) throw new InvalidOperationException("capacity_full");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AllianceInvitation accepted = invitation with { Status = AllianceInvitationStatus.Accepted, RespondedAtUtc = now };
        repository.SaveInvitation(accepted);

        AllianceMembership membership = repository.SaveMembership(new AllianceMembership
        {
            AllianceId = invitation.AllianceId,
            PlayerId = actorPlayerId,
            Role = AllianceRole.Member,
            JoinedAtUtc = now,
            LastRoleChangedAtUtc = now,
            InvitedByPlayerId = invitation.InvitedByPlayerId,
            Revision = 1
        });
        AllianceEntity updatedAlliance = repository.Save(alliance with { MemberCount = repository.ListActiveMembers(invitation.AllianceId).Count, Revision = alliance.Revision + 1 });
        SyncChatParticipantAdded(updatedAlliance.ChatConversationId, actorPlayerId, AllianceRole.Member, now);
        Publish(invitation.AllianceId, AllianceActivityType.MemberJoined, now, actorPlayerId, null, AllianceActivityVisibility.Public, null);
        return new InvitationDecisionResult(accepted, membership);
    }

    public AllianceInvitation DeclineInvitation(PlayerId actorPlayerId, Guid invitationId)
    {
        AllianceInvitation invitation = repository.GetInvitation(invitationId) ?? throw new KeyNotFoundException("not_found");
        if (invitation.InvitedPlayerId != actorPlayerId) throw new UnauthorizedAccessException();
        if (invitation.Status != AllianceInvitationStatus.Pending)
        {
            if (invitation.Status == AllianceInvitationStatus.Declined) return invitation;
            throw new InvalidOperationException("invalid_state");
        }
        AllianceInvitation declined = invitation with { Status = AllianceInvitationStatus.Declined, RespondedAtUtc = DateTimeOffset.UtcNow };
        repository.SaveInvitation(declined);
        return declined;
    }

    public AllianceInvitation RevokeInvitation(PlayerId actorPlayerId, Guid invitationId)
    {
        AllianceInvitation invitation = repository.GetInvitation(invitationId) ?? throw new KeyNotFoundException("not_found");
        AllianceMembership actor = RequireMembership(invitation.AllianceId, actorPlayerId);
        if (!AlliancePermissionPolicy.CanInvite(actor.Role)) throw new UnauthorizedAccessException();
        if (invitation.Status != AllianceInvitationStatus.Pending)
        {
            if (invitation.Status == AllianceInvitationStatus.Revoked) return invitation;
            throw new InvalidOperationException("invalid_state");
        }
        AllianceInvitation revoked = invitation with { Status = AllianceInvitationStatus.Revoked, RespondedAtUtc = DateTimeOffset.UtcNow };
        repository.SaveInvitation(revoked);
        return revoked;
    }

    public IReadOnlyList<AllianceInvitation> ListMyInvitations(PlayerId actorPlayerId) => repository.ListPendingInvitationsForPlayer(actorPlayerId);

    // ---------------- Leave / kick ----------------

    public void Leave(PlayerId actorPlayerId)
    {
        AllianceMembership membership = repository.GetActiveMembershipForPlayer(actorPlayerId) ?? throw new InvalidOperationException("not_a_member");
        if (membership.Role == AllianceRole.Leader) throw new InvalidOperationException("leader_must_transfer_or_dissolve");
        RemoveMember(membership, AllianceActivityType.MemberLeft, actorPlayerId, actorPlayerId);
    }

    public void Kick(PlayerId actorPlayerId, PlayerId targetPlayerId)
    {
        AllianceMembership actor = RequireMembership(RequireAllianceIdForPlayer(actorPlayerId), actorPlayerId);
        AllianceMembership target = repository.GetActiveMembership(actor.AllianceId, targetPlayerId) ?? throw new KeyNotFoundException("not_found");
        if (!AlliancePermissionPolicy.CanKickMember(actor.Role) || !AlliancePermissionPolicy.CanKickTarget(actor.Role, target.Role))
            throw new UnauthorizedAccessException();
        RemoveMember(target, AllianceActivityType.MemberKicked, actorPlayerId, targetPlayerId);
    }

    private void RemoveMember(AllianceMembership membership, AllianceActivityType activityType, PlayerId actorPlayerId, PlayerId targetPlayerId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        repository.SaveMembership(membership with { RemovedAtUtc = now, Revision = membership.Revision + 1 });
        AllianceEntity? alliance = repository.Get(membership.AllianceId);
        if (alliance != null)
        {
            alliance = repository.Save(alliance with { MemberCount = repository.ListActiveMembers(membership.AllianceId).Count, Revision = alliance.Revision + 1 });
            SyncChatParticipantRemoved(alliance.ChatConversationId, targetPlayerId, now);
        }
        Publish(membership.AllianceId, activityType, now, actorPlayerId, targetPlayerId, AllianceActivityVisibility.Public, null);
    }

    // ---------------- Promote / demote ----------------

    public AllianceMembership Promote(PlayerId actorPlayerId, PlayerId targetPlayerId)
    {
        AllianceMembership actor = RequireMembership(RequireAllianceIdForPlayer(actorPlayerId), actorPlayerId);
        if (!AlliancePermissionPolicy.CanPromote(actor.Role)) throw new UnauthorizedAccessException();
        AllianceMembership target = repository.GetActiveMembership(actor.AllianceId, targetPlayerId) ?? throw new KeyNotFoundException("not_found");
        if (target.Role != AllianceRole.Member) throw new InvalidOperationException("invalid_state");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AllianceMembership updated = repository.SaveMembership(target with { Role = AllianceRole.Officer, LastRoleChangedAtUtc = now, Revision = target.Revision + 1 });
        SyncChatParticipantAdded(repository.Get(actor.AllianceId)?.ChatConversationId, targetPlayerId, AllianceRole.Officer, updated.JoinedAtUtc);
        Publish(actor.AllianceId, AllianceActivityType.MemberPromoted, now, actorPlayerId, targetPlayerId, AllianceActivityVisibility.Public, null);
        return updated;
    }

    public AllianceMembership Demote(PlayerId actorPlayerId, PlayerId targetPlayerId)
    {
        AllianceMembership actor = RequireMembership(RequireAllianceIdForPlayer(actorPlayerId), actorPlayerId);
        if (!AlliancePermissionPolicy.CanDemote(actor.Role)) throw new UnauthorizedAccessException();
        AllianceMembership target = repository.GetActiveMembership(actor.AllianceId, targetPlayerId) ?? throw new KeyNotFoundException("not_found");
        if (target.Role != AllianceRole.Officer) throw new InvalidOperationException("invalid_state");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AllianceMembership updated = repository.SaveMembership(target with { Role = AllianceRole.Member, LastRoleChangedAtUtc = now, Revision = target.Revision + 1 });
        SyncChatParticipantAdded(repository.Get(actor.AllianceId)?.ChatConversationId, targetPlayerId, AllianceRole.Member, updated.JoinedAtUtc);
        Publish(actor.AllianceId, AllianceActivityType.MemberDemoted, now, actorPlayerId, targetPlayerId, AllianceActivityVisibility.Public, null);
        return updated;
    }

    // ---------------- Leadership transfer ----------------

    public LeadershipTransferResult TransferLeadership(PlayerId actorPlayerId, PlayerId targetPlayerId)
    {
        AllianceMembership actor = RequireMembership(RequireAllianceIdForPlayer(actorPlayerId), actorPlayerId);
        if (!AlliancePermissionPolicy.CanTransferLeadership(actor.Role)) throw new UnauthorizedAccessException();
        if (targetPlayerId == actorPlayerId) throw new InvalidOperationException("invalid_request");
        AllianceMembership target = repository.GetActiveMembership(actor.AllianceId, targetPlayerId) ?? throw new KeyNotFoundException("not_found");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AllianceMembership previousLeader = repository.SaveMembership(actor with { Role = AllianceRole.Officer, LastRoleChangedAtUtc = now, Revision = actor.Revision + 1 });
        AllianceMembership newLeader = repository.SaveMembership(target with { Role = AllianceRole.Leader, LastRoleChangedAtUtc = now, Revision = target.Revision + 1 });

        AllianceEntity alliance = repository.Get(actor.AllianceId) ?? throw new KeyNotFoundException("not_found");
        AllianceEntity updated = repository.Save(alliance with { LeaderPlayerId = targetPlayerId, Revision = alliance.Revision + 1 });
        SyncChatParticipantAdded(updated.ChatConversationId, actorPlayerId, AllianceRole.Officer, previousLeader.JoinedAtUtc);
        SyncChatParticipantAdded(updated.ChatConversationId, targetPlayerId, AllianceRole.Leader, newLeader.JoinedAtUtc);
        Publish(actor.AllianceId, AllianceActivityType.LeadershipTransferred, now, actorPlayerId, targetPlayerId, AllianceActivityVisibility.Public, null);
        return new LeadershipTransferResult(updated, previousLeader, newLeader);
    }

    // ---------------- Dissolve ----------------

    public AllianceEntity Dissolve(PlayerId actorPlayerId)
    {
        AllianceMembership actor = RequireMembership(RequireAllianceIdForPlayer(actorPlayerId), actorPlayerId);
        if (!AlliancePermissionPolicy.CanDissolve(actor.Role)) throw new UnauthorizedAccessException();

        AllianceEntity alliance = repository.Get(actor.AllianceId) ?? throw new KeyNotFoundException("not_found");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (AllianceMembership member in repository.ListActiveMembers(actor.AllianceId))
        {
            repository.SaveMembership(member with { RemovedAtUtc = now, Revision = member.Revision + 1 });
            // Dissolve "archives" the alliance conversation by removing every participant -
            // BeeKingdom.Chat has no separate archive flag on ChatConversation today, so an
            // inaccessible-to-everyone conversation is the closest real equivalent (see
            // ALLIANCE_PLATFORM_ARCHITECTURE.md / M042-CL report for this documented limitation).
            SyncChatParticipantRemoved(alliance.ChatConversationId, member.PlayerId, now);
        }
        foreach (AllianceApplication application in repository.ListPendingApplications(actor.AllianceId))
        {
            repository.SaveApplication(application with { Status = AllianceApplicationStatus.Cancelled, RespondedAtUtc = now });
        }
        // Pending invitations for this alliance are revoked lazily (checked at accept-time via
        // RequireActiveAlliance failing on a disbanded alliance) rather than enumerated here -
        // there's no ListPendingInvitationsForAlliance index, only ...ForPlayer, and adding one
        // just for this rare path isn't worth the extra repository surface tonight.

        AllianceEntity disbanded = repository.Save(alliance with { Status = AllianceStatus.Disbanded, DisbandedAtUtc = now, Revision = alliance.Revision + 1 });
        Publish(actor.AllianceId, AllianceActivityType.AllianceCreated, now, actorPlayerId, null, AllianceActivityVisibility.Public, null);
        return disbanded;
    }

    // ---------------- Profile ----------------

    public AllianceEntity UpdateProfile(PlayerId actorPlayerId, UpdateAllianceProfileRequest request)
    {
        AllianceMembership actor = RequireMembership(RequireAllianceIdForPlayer(actorPlayerId), actorPlayerId);
        if (!AlliancePermissionPolicy.CanEditProfile(actor.Role)) throw new UnauthorizedAccessException();
        AllianceEntity alliance = repository.Get(actor.AllianceId) ?? throw new KeyNotFoundException("not_found");
        if (alliance.Revision != request.ExpectedRevision) throw new InvalidOperationException("revision_conflict");
        if (request.Description is { Length: > 0 } d && d.Length > O.DescriptionMaxLength) throw new ArgumentException("invalid_request");

        AllianceEntity updated = repository.Save(alliance with
        {
            Description = request.Description ?? alliance.Description,
            Language = request.Language ?? alliance.Language,
            EmblemKey = request.EmblemKey ?? alliance.EmblemKey,
            JoinMode = request.JoinMode ?? alliance.JoinMode,
            Revision = alliance.Revision + 1
        });
        Publish(actor.AllianceId, AllianceActivityType.ProfileUpdated, DateTimeOffset.UtcNow, actorPlayerId, null, AllianceActivityVisibility.MembersOnly, null);
        return updated;
    }

    // ---------------- Public profile / activity ----------------

    public AlliancePublicProfile GetPublicProfile(AllianceId allianceId)
    {
        AllianceEntity alliance = repository.Get(allianceId) ?? throw new KeyNotFoundException("not_found");
        IReadOnlyList<AllianceDiplomaticRelation> relations = diplomacyRepository.ListForAlliance(allianceId);
        var diplomacy = new AllianceDiplomacySummary(
            relations.Count(r => r.RelationType == AllianceRelationType.Ally && r.Status == AllianceRelationStatus.Active),
            relations.Count(r => r.RelationType == AllianceRelationType.NonAggressionPact && r.Status == AllianceRelationStatus.Active),
            relations.Count(r => r.RelationType == AllianceRelationType.Hostile && r.Status == AllianceRelationStatus.Active),
            warRepository.ListActiveForAlliance(allianceId).Count);

        return new AlliancePublicProfile
        {
            AllianceId = alliance.AllianceId,
            Name = alliance.Name,
            Tag = alliance.Tag,
            Description = alliance.Description,
            Language = alliance.Language,
            EmblemKey = alliance.EmblemKey,
            MemberCount = alliance.MemberCount,
            MaxMembers = alliance.MaxMembers,
            Leader = new AllianceLeaderSummary(alliance.LeaderPlayerId, ResolveDisplayName(alliance.LeaderPlayerId)),
            Status = alliance.Status,
            CreatedAtUtc = alliance.CreatedAtUtc,
            JoinMode = alliance.JoinMode,
            PublicSlug = alliance.PublicSlug,
            Diplomacy = diplomacy
        };
    }

    public AllianceEntity? GetBySlug(string slug) => repository.GetBySlug(slug);

    // M043-CL: NO_ALLIANCE vs IN_ALLIANCE detection for the Unity client - see MyAllianceOverview.
    public MyAllianceOverview? GetMyAlliance(PlayerId actorPlayerId)
    {
        AllianceMembership? membership = repository.GetActiveMembershipForPlayer(actorPlayerId);
        if (membership == null) return null;
        AllianceEntity alliance = repository.Get(membership.AllianceId) ?? throw new KeyNotFoundException("not_found");
        return new MyAllianceOverview(alliance, membership);
    }

    // Member-visible roster - not exposed on AlliancePublicProfile (see AllianceMemberSummary).
    public IReadOnlyList<AllianceMemberSummary> ListMembers(PlayerId actorPlayerId, AllianceId allianceId)
    {
        RequireMembership(allianceId, actorPlayerId);
        IReadOnlyList<AllianceMembership> members = repository.ListActiveMembers(allianceId);
        // M043B-CL: batch-resolved (one server-side pass), not one lookup per member - avoids N+1
        // both server-side and, more importantly, avoids Unity ever needing N+1 HTTP calls.
        IReadOnlyDictionary<PlayerId, string> names = ResolveDisplayNames(members.Select(m => m.PlayerId));
        return members
            .Select(m => new AllianceMemberSummary(m.PlayerId, names.TryGetValue(m.PlayerId, out string? name) ? name : string.Empty, m.Role, m.JoinedAtUtc))
            .ToArray();
    }

    public AllianceApplication? GetApplicationForProof(Guid applicationId) => repository.GetApplication(applicationId);

    // M043B-CL: closes the gap M043 documented - the repository already supported listing pending
    // applications by AllianceId, the service just never exposed it. AllianceId is ALWAYS derived
    // server-side from the actor's own real membership (RequireAllianceIdForPlayer) - never taken
    // as a client-supplied parameter, so a non-member/non-officer can never enumerate another
    // alliance's applications by guessing an id.
    public IReadOnlyList<AllianceApplicationView> ListPendingApplicationsForMyAlliance(PlayerId actorPlayerId)
    {
        AllianceId allianceId = RequireAllianceIdForPlayer(actorPlayerId);
        AllianceMembership actor = RequireMembership(allianceId, actorPlayerId);
        if (!AlliancePermissionPolicy.CanApproveApplication(actor.Role)) throw new UnauthorizedAccessException("insufficient_permission");
        IReadOnlyList<AllianceApplication> applications = repository.ListPendingApplications(allianceId);
        IReadOnlyDictionary<PlayerId, string> names = ResolveDisplayNames(applications.Select(a => a.PlayerId));
        return applications
            .Select(a => new AllianceApplicationView(a.ApplicationId, a.AllianceId, a.PlayerId,
                names.TryGetValue(a.PlayerId, out string? name) ? name : string.Empty, a.Status, a.SubmittedAtUtc, a.Message))
            .ToArray();
    }

    private string ResolveDisplayName(PlayerId playerId) => playerDirectory?.GetByPlayerId(playerId)?.DisplayName ?? string.Empty;

    private IReadOnlyDictionary<PlayerId, string> ResolveDisplayNames(IEnumerable<PlayerId> playerIds)
    {
        PlayerId[] ids = playerIds.ToArray();
        if (playerDirectory == null) return ids.ToDictionary(id => id, _ => string.Empty);
        var identities = playerDirectory.GetByPlayerIds(ids);
        return ids.ToDictionary(id => id, id => identities.TryGetValue(id, out var identity) ? identity.DisplayName : string.Empty);
    }

    public AllianceActivityPage ListActivity(PlayerId actorPlayerId, AllianceId allianceId, long? beforeSequence, int limit)
    {
        AllianceMembership? membership = repository.GetActiveMembership(allianceId, actorPlayerId);
        AllianceActivityVisibility maxVisibility = membership == null
            ? AllianceActivityVisibility.Public
            : AlliancePermissionPolicy.CanManageDiplomacy(membership.Role) ? AllianceActivityVisibility.OfficersOnly : AllianceActivityVisibility.MembersOnly;
        return activityRepository.ListForAlliance(allianceId, beforeSequence, Math.Clamp(limit, 1, O.ActivityPageMaxLimit), maxVisibility);
    }

    public AllianceActivityPage ListPublicActivity(AllianceId allianceId, long? beforeSequence, int limit)
        => activityRepository.ListPublicForAlliance(allianceId, beforeSequence, Math.Clamp(limit, 1, O.ActivityPageMaxLimit));

    // ---------------- Diplomacy ----------------

    public DiplomacyDecisionResult ProposeRelation(PlayerId actorPlayerId, AllianceId targetAllianceId, ProposeDiplomacyRequest request)
    {
        if (!O.DiplomacyEnabled) throw new InvalidOperationException("diplomacy_disabled");
        if (string.IsNullOrWhiteSpace(request.ClientRequestId)) throw new ArgumentException("invalid_request");
        if (request.RelationType is not (AllianceRelationType.NonAggressionPact or AllianceRelationType.Ally))
            throw new ArgumentException("invalid_request");

        Guid? existingReceipt = diplomacyRepository.GetProposalReceipt(actorPlayerId, request.ClientRequestId);
        AllianceMembership actor = RequireMembership(RequireAllianceIdForPlayer(actorPlayerId), actorPlayerId);
        if (!AlliancePermissionPolicy.CanManageDiplomacy(actor.Role)) throw new UnauthorizedAccessException();
        if (actor.AllianceId == targetAllianceId) throw new InvalidOperationException("invalid_request");
        RequireActiveAlliance(targetAllianceId);

        if (existingReceipt.HasValue)
        {
            AllianceDiplomaticRelation? existing = diplomacyRepository.GetRelation(actor.AllianceId, targetAllianceId);
            if (existing != null) return new DiplomacyDecisionResult(existing);
        }

        AllianceDiplomaticRelation? current = diplomacyRepository.GetRelation(actor.AllianceId, targetAllianceId);
        if (current is { Status: AllianceRelationStatus.Proposed or AllianceRelationStatus.Active })
            throw new InvalidOperationException("relation_already_pending_or_active");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var relation = new AllianceDiplomaticRelation
        {
            RelationId = Guid.NewGuid(),
            AllianceIdA = actor.AllianceId,
            AllianceIdB = targetAllianceId,
            RelationType = request.RelationType,
            Status = AllianceRelationStatus.Proposed,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            InitiatedByAllianceId = actor.AllianceId,
            Revision = 1
        };
        diplomacyRepository.Save(relation);
        diplomacyRepository.SaveProposalReceipt(actorPlayerId, request.ClientRequestId, relation.RelationId);
        PublishDiplomacy(actor.AllianceId, targetAllianceId, now, actorPlayerId, AllianceActivityType.AllianceDiplomacyChanged);
        return new DiplomacyDecisionResult(relation);
    }

    public DiplomacyDecisionResult RespondToRelation(PlayerId actorPlayerId, AllianceId proposerAllianceId, bool accept)
    {
        if (!O.DiplomacyEnabled) throw new InvalidOperationException("diplomacy_disabled");
        AllianceMembership actor = RequireMembership(RequireAllianceIdForPlayer(actorPlayerId), actorPlayerId);
        if (!AlliancePermissionPolicy.CanManageDiplomacy(actor.Role)) throw new UnauthorizedAccessException();

        AllianceDiplomaticRelation relation = diplomacyRepository.GetRelation(actor.AllianceId, proposerAllianceId) ?? throw new KeyNotFoundException("not_found");
        if (relation.InitiatedByAllianceId == actor.AllianceId) throw new UnauthorizedAccessException(); // can't accept your own proposal
        if (relation.Status != AllianceRelationStatus.Proposed)
        {
            if (relation.Status is AllianceRelationStatus.Active or AllianceRelationStatus.Rejected) return new DiplomacyDecisionResult(relation);
            throw new InvalidOperationException("invalid_state");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AllianceDiplomaticRelation updated = diplomacyRepository.Save(relation with
        {
            Status = accept ? AllianceRelationStatus.Active : AllianceRelationStatus.Rejected,
            UpdatedAtUtc = now,
            Revision = relation.Revision + 1
        });
        PublishDiplomacy(relation.AllianceIdA, relation.AllianceIdB, now, actorPlayerId, AllianceActivityType.AllianceDiplomacyChanged);
        return new DiplomacyDecisionResult(updated);
    }

    public DiplomacyDecisionResult CancelRelation(PlayerId actorPlayerId, AllianceId otherAllianceId)
    {
        if (!O.DiplomacyEnabled) throw new InvalidOperationException("diplomacy_disabled");
        AllianceMembership actor = RequireMembership(RequireAllianceIdForPlayer(actorPlayerId), actorPlayerId);
        if (!AlliancePermissionPolicy.CanManageDiplomacy(actor.Role)) throw new UnauthorizedAccessException();

        AllianceDiplomaticRelation relation = diplomacyRepository.GetRelation(actor.AllianceId, otherAllianceId) ?? throw new KeyNotFoundException("not_found");
        if (relation.Status is AllianceRelationStatus.Ended or AllianceRelationStatus.Cancelled or AllianceRelationStatus.Rejected)
            return new DiplomacyDecisionResult(relation);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AllianceRelationStatus resultStatus = relation.Status == AllianceRelationStatus.Proposed ? AllianceRelationStatus.Cancelled : AllianceRelationStatus.Ended;
        AllianceDiplomaticRelation updated = diplomacyRepository.Save(relation with { Status = resultStatus, UpdatedAtUtc = now, Revision = relation.Revision + 1 });
        PublishDiplomacy(relation.AllianceIdA, relation.AllianceIdB, now, actorPlayerId, AllianceActivityType.AllianceDiplomacyChanged);
        return new DiplomacyDecisionResult(updated);
    }

    private void PublishDiplomacy(AllianceId allianceA, AllianceId allianceB, DateTimeOffset now, PlayerId actorPlayerId, AllianceActivityType type)
    {
        Publish(allianceA, type, now, actorPlayerId, null, AllianceActivityVisibility.Public, allianceB);
        Publish(allianceB, type, now, actorPlayerId, null, AllianceActivityVisibility.Public, allianceA);
    }

    // ---------------- War (declaration foundation only) ----------------

    public DeclareWarResult DeclareWar(PlayerId actorPlayerId, DeclareWarRequest request)
    {
        if (!O.WarEnabled) throw new InvalidOperationException("war_disabled");
        if (string.IsNullOrWhiteSpace(request.ClientRequestId)) throw new ArgumentException("invalid_request");

        Guid? existingId = warRepository.GetDeclareReceipt(actorPlayerId, request.ClientRequestId);
        AllianceMembership actor = RequireMembership(RequireAllianceIdForPlayer(actorPlayerId), actorPlayerId);
        if (existingId.HasValue)
        {
            AllianceWar? existingWar = warRepository.Get(existingId.Value);
            if (existingWar != null) return new DeclareWarResult(existingWar);
        }

        if (!AlliancePermissionPolicy.CanDeclareWar(actor.Role)) throw new UnauthorizedAccessException();
        if (actor.AllianceId == request.DefenderAllianceId) throw new InvalidOperationException("invalid_request");
        RequireActiveAlliance(actor.AllianceId);
        RequireActiveAlliance(request.DefenderAllianceId);
        if (warRepository.HasActiveWarBetween(actor.AllianceId, request.DefenderAllianceId))
            throw new InvalidOperationException("duplicate_active_war");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var war = new AllianceWar
        {
            WarId = Guid.NewGuid(),
            AttackerAllianceId = actor.AllianceId,
            DefenderAllianceId = request.DefenderAllianceId,
            Status = AllianceWarStatus.Declared,
            DeclaredAtUtc = now,
            Revision = 1
        };
        warRepository.Save(war);
        warRepository.SaveDeclareReceipt(actorPlayerId, request.ClientRequestId, war.WarId);

        // Declaring war implies (at minimum) a hostile relation - update/create the relation row
        // so GetPublicProfile's diplomacy summary and future relation queries stay consistent.
        AllianceDiplomaticRelation? relation = diplomacyRepository.GetRelation(actor.AllianceId, request.DefenderAllianceId);
        diplomacyRepository.Save(new AllianceDiplomaticRelation
        {
            RelationId = relation?.RelationId ?? Guid.NewGuid(),
            AllianceIdA = actor.AllianceId,
            AllianceIdB = request.DefenderAllianceId,
            RelationType = AllianceRelationType.War,
            Status = AllianceRelationStatus.Active,
            CreatedAtUtc = relation?.CreatedAtUtc ?? now,
            UpdatedAtUtc = now,
            InitiatedByAllianceId = actor.AllianceId,
            Revision = (relation?.Revision ?? 0) + 1
        });

        Publish(war.AttackerAllianceId, AllianceActivityType.AllianceWarDeclared, now, actorPlayerId, null, AllianceActivityVisibility.Public, war.DefenderAllianceId);
        Publish(war.DefenderAllianceId, AllianceActivityType.AllianceWarDeclared, now, actorPlayerId, null, AllianceActivityVisibility.Public, war.AttackerAllianceId);
        return new DeclareWarResult(war);
    }

    // ---------------- Helpers ----------------

    private AllianceEntity RequireActiveAlliance(AllianceId allianceId)
    {
        AllianceEntity alliance = repository.Get(allianceId) ?? throw new KeyNotFoundException("not_found");
        if (alliance.Status != AllianceStatus.Active) throw new InvalidOperationException("alliance_disbanded");
        return alliance;
    }

    private AllianceMembership RequireMembership(AllianceId allianceId, PlayerId playerId)
        => repository.GetActiveMembership(allianceId, playerId) ?? throw new InvalidOperationException("not_a_member");

    private AllianceId RequireAllianceIdForPlayer(PlayerId playerId)
        => (repository.GetActiveMembershipForPlayer(playerId) ?? throw new InvalidOperationException("not_a_member")).AllianceId;

    private void Publish(AllianceId allianceId, AllianceActivityType type, DateTimeOffset now, PlayerId? actor, PlayerId? target, AllianceActivityVisibility visibility, AllianceId? relatedAlliance)
    {
        activityRepository.Append(new AllianceActivityEvent
        {
            ActivityId = Guid.NewGuid(),
            AllianceId = allianceId,
            Type = type,
            OccurredAtUtc = now,
            ActorPlayerId = actor,
            TargetPlayerId = target,
            RelatedAllianceId = relatedAlliance,
            Visibility = visibility,
            Sequence = 0
        });
    }
}
