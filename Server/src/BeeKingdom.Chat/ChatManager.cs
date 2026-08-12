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
}
