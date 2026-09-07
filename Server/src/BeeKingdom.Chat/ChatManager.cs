using BeeKingdom.Chat.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Chat;

public sealed class ChatManager
{
    private readonly IChatService service;

    public ChatManager(IChatService service)
    {
        this.service = service;
    }

    public ChatCapabilities GetCapabilities() => service.GetCapabilities();
    public ChatReadiness GetReadiness() => service.GetReadiness();
    public CreateChatConversationResult CreateConversation(PlayerId playerId, CreateChatConversationRequest request) => service.CreateConversation(playerId, request);
    public ChatConversationPage ListConversations(PlayerId playerId, int limit, string? cursor = null) => service.ListConversations(playerId, limit, cursor);
    public ChatInboxEntry? GetInbox(PlayerId playerId, Guid conversationId) => service.GetInbox(playerId, conversationId);
    public ChatMessagePage GetMessages(PlayerId playerId, Guid conversationId, long afterSequence, int limit) => service.GetMessages(playerId, conversationId, afterSequence, limit);
    public Task<SendChatMessageResult> SendMessageAsync(PlayerId playerId, Guid conversationId, SendChatMessageRequest request, CancellationToken cancellationToken = default) => service.SendMessageAsync(playerId, conversationId, request, cancellationToken);
    public ChatInboxEntry MarkRead(PlayerId playerId, Guid conversationId, long sequence) => service.MarkRead(playerId, conversationId, sequence);
    public ChatModerationReport ReportMessage(PlayerId playerId, Guid messageId, ReportChatMessageRequest request) => service.ReportMessage(playerId, messageId, request);
    public Task<CreateAllianceAnnouncementResult> SendAllianceAnnouncementAsync(PlayerId playerId, Guid allianceId, CreateAllianceAnnouncementRequest request, CancellationToken cancellationToken = default) => service.SendAllianceAnnouncementAsync(playerId, allianceId, request, cancellationToken);
    public void EnsureCanRead(PlayerId playerId, Guid conversationId) => service.EnsureCanRead(playerId, conversationId);
    public long GetLastSequence(Guid conversationId) => service.GetLastSequence(conversationId);

    // RAP-OPTIONNEL-COMMUNICATIONS_01 - player-created groups, invitations and chat preferences.
    public CreateChatGroupResult CreateGroup(PlayerId playerId, CreateChatGroupRequest request) => service.CreateGroup(playerId, request);
    public ChatGroupInviteDto InviteToGroup(PlayerId actingPlayerId, Guid conversationId, Guid inviteePlayerId) => service.InviteToGroup(actingPlayerId, conversationId, inviteePlayerId);
    public IReadOnlyList<ChatGroupInviteDto> ListPendingInvitations(PlayerId playerId) => service.ListPendingInvitations(playerId);
    public IReadOnlyList<ChatGroupInviteDto> ListSentInvitationResponses(PlayerId playerId) => service.ListSentInvitationResponses(playerId);
    public int AcknowledgeInvitationResponses(PlayerId playerId, IReadOnlyList<Guid> inviteIds) => service.AcknowledgeInvitationResponses(playerId, inviteIds);
    public ChatGroupInviteDto RespondToInvitation(PlayerId playerId, Guid inviteId, bool accept) => service.RespondToInvitation(playerId, inviteId, accept);
    public ChatGroupDetail GetGroupDetail(PlayerId playerId, Guid conversationId) => service.GetGroupDetail(playerId, conversationId);
    public ChatGroupDetail RemoveGroupMember(PlayerId actingPlayerId, Guid conversationId, Guid targetPlayerId) => service.RemoveGroupMember(actingPlayerId, conversationId, targetPlayerId);
    public ChatGroupDetail TransferGroupLeadership(PlayerId actingPlayerId, Guid conversationId, Guid newLeaderPlayerId) => service.TransferGroupLeadership(actingPlayerId, conversationId, newLeaderPlayerId);
    public void LeaveGroup(PlayerId playerId, Guid conversationId) => service.LeaveGroup(playerId, conversationId);
    public ChatPreferences GetPreferences(PlayerId playerId) => service.GetPreferences(playerId);
    public ChatPreferences UpdatePreferences(PlayerId playerId, UpdateChatPreferencesRequest request) => service.UpdatePreferences(playerId, request);
}
