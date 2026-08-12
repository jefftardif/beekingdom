namespace BeeKingdom.Chat.Models;

public sealed record ChatWireMessageDto(Guid MessageId,Guid ConversationId,Guid GameServerId,Guid WorldId,ChatChannelType ChannelType,Guid SenderPlayerId,string SenderDisplayName,string Body,DateTimeOffset ClientCreatedAtUtc,DateTimeOffset AcceptedAtUtc,long Sequence,string ClientRequestId,string? SenderDisplayNameSnapshot = null);
public sealed record ChatWireMessagePage(IReadOnlyList<ChatWireMessageDto> Items,long? NextAfterSequence);
public sealed record ChatWireSendResult(ChatWireMessageDto Message,bool Deduplicated,long ServerSequence);
public sealed record ChatTransportConversationDto(Guid ConversationId, ChatChannelType ChannelType, string? Title, long LastSequence, long ReadCursorSequence, int UnreadCount, int MentionCount);
public sealed record ChatTransportConversationPage(IReadOnlyList<ChatTransportConversationDto> Items, string? NextCursor);
public sealed record ChatTransportCreateConversationResult(ChatTransportConversationDto Conversation, ChatInboxEntry Inbox, string ClientRequestId);
public sealed record ChatTransportModerationReportResult(Guid ReportId, Guid MessageId, string ClientRequestId, string Status);

public static class ChatTransportMapper
{
    public static ChatWireMessageDto Message(ChatMessage value)=>new(value.MessageId,value.ConversationId,value.GameServerId,value.WorldId,value.ChannelType,value.SenderPlayerId.Value,value.SenderDisplayNameSnapshot,value.Body,value.ClientCreatedAtUtc,value.AcceptedAtUtc,value.Sequence,value.ClientRequestId,value.SenderDisplayNameSnapshot);
    public static ChatTransportConversationDto Conversation(ChatConversation value,long lastSequence, ChatInboxEntry? inbox = null) => new(value.ConversationId,value.ChannelType,value.Title,lastSequence,inbox?.ReadCursorSequence ?? 0,inbox?.UnreadCount ?? 0,inbox?.MentionCount ?? 0);
}
