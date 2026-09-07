using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Chat;

// RAP-OPTIONNEL-COMMUNICATIONS_01 - player-created group rooms.
//
// Why this is NOT routed through IChatAudienceResolver like the four fixed channels: that seam
// answers "is this player allowed inside THIS KIND of room" (alliance membership, leader rank...).
// A group's membership is not derivable from anything - the creator picks it, one player at a time,
// and it changes afterwards. So groups get an explicit, mutable invite list owned by a Leader,
// enforced here, and the audience resolver is left untouched.
//
// Everything a client needs to render is resolved server-side (display names included) so the future
// web client consumes the same payloads as Unity with no extra round trips.
public sealed partial class ChatService
{
    private const int GroupTitleMaxCharacters = 64;

    public CreateChatGroupResult CreateGroup(PlayerId playerId, CreateChatGroupRequest request)
    {
        EnsureEnabled();
        MaybePurgeReceipts();
        if (request.GameServerId == Guid.Empty || request.WorldId == Guid.Empty) throw new ArgumentException("scope_required");
        if (string.IsNullOrWhiteSpace(request.ClientRequestId) || request.ClientRequestId.Length > 256) throw new ArgumentException("client_request_id_required");
        string title = (request.Title ?? string.Empty).Trim();
        if (title.Length == 0) throw new ArgumentException("group_title_required");
        if (title.Length > GroupTitleMaxCharacters) throw new ArgumentException("group_title_too_large");

        Guid[] invitees = NormalizeInvitees(playerId, request.InviteePlayerIds);
        if (invitees.Length > options.MaxPrivateRecipients) throw new ArgumentException("group_invitees_invalid");

        string payloadHash = ComputeGroupPayloadHash(request.GameServerId, request.WorldId, title, invitees);
        ChatConversationCreationReceipt? receipt = repository.GetConversationCreationReceipt(playerId, request.ClientRequestId.Trim());
        if (receipt != null)
        {
            if (!string.Equals(receipt.PayloadHash, payloadHash, StringComparison.Ordinal)) throw new InvalidOperationException("idempotency_conflict");
            ChatConversation replay = repository.GetConversation(receipt.ConversationId) ?? throw new InvalidOperationException("idempotency_record_missing_conversation");
            return new CreateChatGroupResult(replay, BuildGroupDetail(replay, playerId));
        }

        DateTimeOffset now = clock.UtcNow;
        ChatConversation conversation = new(
            Guid.NewGuid(),
            request.GameServerId,
            request.WorldId,
            ChatChannelType.Group,
            // Random, not derived from the member set: two groups with the same members are two
            // different groups, so they must never collide on UX_ChatConversations_Audience the way
            // Private/Alliance rooms deliberately do.
            $"group:{Guid.NewGuid():N}",
            title,
            playerId,
            now,
            null,
            null,
            "group_standard",
            1);

        repository.SaveConversation(conversation, [new ChatConversationParticipant(conversation.ConversationId, playerId, ChatPermissionRole.Leader, now, null, true, true)]);
        repository.SaveInbox(CreateInbox(playerId, conversation.ConversationId, null, now));
        repository.SaveConversationCreationReceipt(new(playerId, request.ClientRequestId.Trim(), payloadHash, conversation.ConversationId, now));

        foreach (Guid invitee in invitees)
        {
            IssueInvite(conversation, playerId, new PlayerId(invitee), now);
        }

        return new CreateChatGroupResult(conversation, BuildGroupDetail(conversation, playerId));
    }

    public ChatGroupInviteDto InviteToGroup(PlayerId actingPlayerId, Guid conversationId, Guid inviteePlayerId)
    {
        EnsureEnabled();
        ChatConversation conversation = RequireGroup(conversationId);
        RequireGroupLeader(conversationId, actingPlayerId);
        if (inviteePlayerId == Guid.Empty || inviteePlayerId == actingPlayerId.Value) throw new ArgumentException("group_invitee_invalid");

        PlayerId invitee = new(inviteePlayerId);
        ChatConversationParticipant? existing = repository.GetParticipant(conversationId, invitee);
        if (existing is { RemovedAtUtc: null }) throw new ArgumentException("group_member_exists");

        ChatGroupInvite? pending = repository.GetPendingInvite(conversationId, invitee);
        if (pending != null) return ToInviteDto(pending, conversation);

        return ToInviteDto(IssueInvite(conversation, actingPlayerId, invitee, clock.UtcNow), conversation);
    }

    public IReadOnlyList<ChatGroupInviteDto> ListPendingInvitations(PlayerId playerId)
    {
        EnsureEnabled();
        return repository.ListPendingInvitesForPlayer(playerId)
            .Select(invite => ToInviteDto(invite, repository.GetConversation(invite.ConversationId)))
            .ToArray();
    }

    // The inviter's side of the loop. Realtime is disabled in production (Chat__RealtimeEnabled),
    // and the rest of the game already polls REST, so a refusal is delivered as an unacknowledged
    // row the inviter picks up on his next poll and then acknowledges - which is also exactly what a
    // web client would do.
    public IReadOnlyList<ChatGroupInviteDto> ListSentInvitationResponses(PlayerId playerId)
    {
        EnsureEnabled();
        return repository.ListInvitesSentByPlayer(playerId, onlyUnacknowledgedResponses: true)
            .Select(invite => ToInviteDto(invite, repository.GetConversation(invite.ConversationId)))
            .ToArray();
    }

    public int AcknowledgeInvitationResponses(PlayerId playerId, IReadOnlyList<Guid> inviteIds)
    {
        EnsureEnabled();
        return repository.AcknowledgeInviteResponses(playerId, inviteIds ?? []);
    }

    public ChatGroupInviteDto RespondToInvitation(PlayerId playerId, Guid inviteId, bool accept)
    {
        EnsureEnabled();
        ChatGroupInvite invite = repository.GetGroupInvite(inviteId) ?? throw new KeyNotFoundException("invite_not_found");
        if (invite.InviteePlayerId != playerId) throw new UnauthorizedAccessException("forbidden");
        if (invite.Status != ChatGroupInviteStatus.Pending) throw new InvalidOperationException("group_invite_not_pending");
        return ToInviteDto(ApplyInviteResponse(invite, accept), repository.GetConversation(invite.ConversationId));
    }

    public ChatGroupDetail GetGroupDetail(PlayerId playerId, Guid conversationId)
    {
        EnsureEnabled();
        ChatConversation conversation = RequireGroup(conversationId);
        RequireRead(conversationId, playerId);
        return BuildGroupDetail(conversation, playerId);
    }

    public ChatGroupDetail RemoveGroupMember(PlayerId actingPlayerId, Guid conversationId, Guid targetPlayerId)
    {
        EnsureEnabled();
        ChatConversation conversation = RequireGroup(conversationId);
        RequireGroupLeader(conversationId, actingPlayerId);
        if (targetPlayerId == actingPlayerId.Value) throw new InvalidOperationException("group_leader_cannot_remove_self");

        PlayerId target = new(targetPlayerId);
        ChatConversationParticipant? participant = repository.GetParticipant(conversationId, target);
        if (participant is null || participant.RemovedAtUtc != null)
        {
            // Not a member (yet): the leader is really cancelling an outstanding invitation.
            ChatGroupInvite? pending = repository.GetPendingInvite(conversationId, target)
                ?? throw new KeyNotFoundException("group_member_not_found");
            repository.UpdateGroupInviteStatus(pending.InviteId, ChatGroupInviteStatus.Cancelled, clock.UtcNow);
            return BuildGroupDetail(conversation, actingPlayerId);
        }

        repository.RemoveParticipant(conversationId, target, clock.UtcNow);
        return BuildGroupDetail(conversation, actingPlayerId);
    }

    public ChatGroupDetail TransferGroupLeadership(PlayerId actingPlayerId, Guid conversationId, Guid newLeaderPlayerId)
    {
        EnsureEnabled();
        ChatConversation conversation = RequireGroup(conversationId);
        RequireGroupLeader(conversationId, actingPlayerId);
        if (newLeaderPlayerId == actingPlayerId.Value) throw new InvalidOperationException("group_leader_already");

        PlayerId newLeader = new(newLeaderPlayerId);
        ChatConversationParticipant? target = repository.GetParticipant(conversationId, newLeader);
        if (target is null || target.RemovedAtUtc != null) throw new KeyNotFoundException("group_member_not_found");

        repository.UpdateParticipantRole(conversationId, newLeader, ChatPermissionRole.Leader);
        repository.UpdateParticipantRole(conversationId, actingPlayerId, ChatPermissionRole.Member);
        return BuildGroupDetail(conversation, actingPlayerId);
    }

    public void LeaveGroup(PlayerId playerId, Guid conversationId)
    {
        EnsureEnabled();
        RequireGroup(conversationId);
        ChatConversationParticipant participant = repository.GetParticipant(conversationId, playerId) is { RemovedAtUtc: null } active
            ? active
            : throw new UnauthorizedAccessException("forbidden");

        if (participant.Role == ChatPermissionRole.Leader && ActiveParticipants(conversationId).Any(item => item.PlayerId != playerId))
        {
            // A group must never be left leaderless while somebody is still inside it: nobody could
            // then invite, remove, or hand over. Leaving as the last member is fine - the room simply
            // goes dormant, and its history stays readable to nobody, which is the intended end state.
            throw new InvalidOperationException("group_leader_must_transfer");
        }

        repository.RemoveParticipant(conversationId, playerId, clock.UtcNow);
    }

    public ChatPreferences GetPreferences(PlayerId playerId)
    {
        EnsureEnabled();
        return repository.GetChatPreferences(playerId) ?? new ChatPreferences(playerId, ChatAutoInviteResponse.Ask, clock.UtcNow);
    }

    public ChatPreferences UpdatePreferences(PlayerId playerId, UpdateChatPreferencesRequest request)
    {
        EnsureEnabled();
        string value = (request.AutoInviteResponse ?? string.Empty).Trim();
        if (!ChatAutoInviteResponse.IsKnown(value)) throw new ArgumentException("chat_preference_invalid");
        return repository.SaveChatPreferences(new ChatPreferences(playerId, value, clock.UtcNow));
    }

    // ==================== internals ====================

    private ChatGroupInvite IssueInvite(ChatConversation conversation, PlayerId inviter, PlayerId invitee, DateTimeOffset now)
    {
        ChatGroupInvite invite = repository.SaveGroupInvite(new ChatGroupInvite(
            Guid.NewGuid(),
            conversation.ConversationId,
            inviter,
            invitee,
            ChatGroupInviteStatus.Pending,
            now,
            null));

        // The invitee's standing rule is applied server-side, immediately, so it also holds while he
        // is offline or connected from another client. "Ask" leaves the invitation pending.
        string rule = repository.GetChatPreferences(invitee)?.AutoInviteResponse ?? ChatAutoInviteResponse.Ask;
        return rule switch
        {
            ChatAutoInviteResponse.Accept => ApplyInviteResponse(invite, accept: true),
            ChatAutoInviteResponse.Decline => ApplyInviteResponse(invite, accept: false),
            _ => invite
        };
    }

    private ChatGroupInvite ApplyInviteResponse(ChatGroupInvite invite, bool accept)
    {
        DateTimeOffset now = clock.UtcNow;
        if (accept)
        {
            repository.UpsertParticipant(new ChatConversationParticipant(invite.ConversationId, invite.InviteePlayerId, ChatPermissionRole.Member, now, null, true, true));
            repository.SaveInbox(CreateInbox(invite.InviteePlayerId, invite.ConversationId, null, now));
        }

        return repository.UpdateGroupInviteStatus(invite.InviteId, accept ? ChatGroupInviteStatus.Accepted : ChatGroupInviteStatus.Declined, now)
            ?? invite with { Status = accept ? ChatGroupInviteStatus.Accepted : ChatGroupInviteStatus.Declined, RespondedAtUtc = now };
    }

    private ChatConversation RequireGroup(Guid conversationId)
    {
        ChatConversation conversation = repository.GetConversation(conversationId) ?? throw new KeyNotFoundException("conversation_not_found");
        if (conversation.ChannelType != ChatChannelType.Group) throw new ArgumentException("conversation_not_a_group");
        return conversation;
    }

    private ChatConversationParticipant RequireGroupLeader(Guid conversationId, PlayerId playerId)
    {
        ChatConversationParticipant participant = RequireRead(conversationId, playerId);
        if (participant.Role != ChatPermissionRole.Leader) throw new UnauthorizedAccessException("group_leader_role_required");
        return participant;
    }

    private IReadOnlyList<ChatConversationParticipant> ActiveParticipants(Guid conversationId)
        => repository.ListParticipants(conversationId).Where(item => item.RemovedAtUtc == null && item.CanRead).ToArray();

    private ChatGroupDetail BuildGroupDetail(ChatConversation conversation, PlayerId viewer)
    {
        IReadOnlyList<ChatConversationParticipant> members = ActiveParticipants(conversation.ConversationId);
        bool viewerIsLeader = members.Any(member => member.PlayerId == viewer && member.Role == ChatPermissionRole.Leader);
        Guid? owner = members.FirstOrDefault(member => member.Role == ChatPermissionRole.Leader)?.PlayerId.Value;

        return new ChatGroupDetail(
            conversation.ConversationId,
            conversation.Title,
            owner,
            viewerIsLeader,
            members
                .OrderByDescending(member => member.Role == ChatPermissionRole.Leader)
                .ThenBy(member => member.JoinedAtUtc)
                .Select(member => new ChatGroupMemberDto(member.PlayerId.Value, DisplayName(member.PlayerId), member.Role, member.Role == ChatPermissionRole.Leader, member.JoinedAtUtc))
                .ToArray(),
            repository.ListPendingInvitesForConversation(conversation.ConversationId)
                .Select(invite => ToInviteDto(invite, conversation))
                .ToArray());
    }

    private ChatGroupInviteDto ToInviteDto(ChatGroupInvite invite, ChatConversation? conversation) => new(
        invite.InviteId,
        invite.ConversationId,
        conversation?.Title ?? string.Empty,
        invite.InviterPlayerId.Value,
        DisplayName(invite.InviterPlayerId),
        invite.InviteePlayerId.Value,
        DisplayName(invite.InviteePlayerId),
        invite.Status,
        invite.CreatedAtUtc,
        invite.RespondedAtUtc);

    private string DisplayName(PlayerId playerId)
        => senderDisplayNameResolver.ResolveDisplayName(playerId.Value) is { Length: > 0 } resolved ? resolved : $"player:{playerId.Value:N}";

    private static Guid[] NormalizeInvitees(PlayerId creator, IReadOnlyList<Guid>? invitees)
        => (invitees ?? []).Where(id => id != Guid.Empty && id != creator.Value).Distinct().ToArray();

    private static string ComputeGroupPayloadHash(Guid gameServerId, Guid worldId, string title, IReadOnlyList<Guid> invitees)
    {
        string canonical = JsonSerializer.Serialize(new { gameServerId, worldId, title, invitees = invitees.OrderBy(id => id).ToArray() }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}

public partial interface IChatService
{
    CreateChatGroupResult CreateGroup(PlayerId playerId, CreateChatGroupRequest request);
    ChatGroupInviteDto InviteToGroup(PlayerId actingPlayerId, Guid conversationId, Guid inviteePlayerId);
    IReadOnlyList<ChatGroupInviteDto> ListPendingInvitations(PlayerId playerId);
    IReadOnlyList<ChatGroupInviteDto> ListSentInvitationResponses(PlayerId playerId);
    int AcknowledgeInvitationResponses(PlayerId playerId, IReadOnlyList<Guid> inviteIds);
    ChatGroupInviteDto RespondToInvitation(PlayerId playerId, Guid inviteId, bool accept);
    ChatGroupDetail GetGroupDetail(PlayerId playerId, Guid conversationId);
    ChatGroupDetail RemoveGroupMember(PlayerId actingPlayerId, Guid conversationId, Guid targetPlayerId);
    ChatGroupDetail TransferGroupLeadership(PlayerId actingPlayerId, Guid conversationId, Guid newLeaderPlayerId);
    void LeaveGroup(PlayerId playerId, Guid conversationId);
    ChatPreferences GetPreferences(PlayerId playerId);
    ChatPreferences UpdatePreferences(PlayerId playerId, UpdateChatPreferencesRequest request);
}
