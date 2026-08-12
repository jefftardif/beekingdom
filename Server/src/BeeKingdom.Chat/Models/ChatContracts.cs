using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Chat.Models;

public sealed record ChatCapabilities(
    string Provider,
    bool Server,
    bool OfficialGain,
    string ProtocolVersion,
    IReadOnlyList<ChatChannelType> Channels,
    bool Emojis,
    bool Mentions,
    bool OfflineDelivery,
    bool ReadCursors,
    bool ModerationReports,
    bool Realtime,
    ChatLimits Limits,
    int IdempotencyReceiptRetentionDays,
    bool TranslationAvailable,
    string TranslationModelVersion);

public sealed record ChatLimits(
    int BodyMaxCharacters,
    int MessagesPerMinutePerPlayer,
    int MessagesPerTenSecondsPerConversation,
    int PrivateConversationCreatesPerHour,
    int MaxPrivateRecipients);

public sealed record ChatReadiness(
    string Status,
    bool Enabled,
    bool RealtimeEnabled,
    bool PersistentSqlSchemaPrepared,
    bool LiveDeploymentAllowed,
    IReadOnlyList<string> Blockers);

public sealed record CreateChatConversationRequest(
    ChatChannelType ChannelType,
    Guid GameServerId,
    Guid WorldId,
    string? AudienceKey,
    string? Title,
    IReadOnlyList<Guid>? ParticipantIds,
    string ClientRequestId,
    string? RequesterAllianceRole = null);

public sealed record SendChatMessageRequest(
    string ClientRequestId,
    string Body,
    IReadOnlyList<ChatContentPart>? ContentParts,
    IReadOnlyList<ChatMentionInput>? Mentions,
    IReadOnlyList<ChatEmoji>? Emoji,
    Guid? ReplyToMessageId,
    DateTimeOffset ClientCreatedAt);

public sealed record ChatMentionInput(Guid PlayerId, string Label);
public sealed record MarkChatConversationReadRequest(long Sequence);
public sealed record ReportChatMessageRequest(string ClientRequestId, string Category);

public sealed record CreateAllianceAnnouncementRequest(
    Guid GameServerId,
    Guid WorldId,
    string Body,
    IReadOnlyList<Guid> MemberPlayerIds,
    string ClientRequestId,
    string? RequesterAllianceRole = null);

public sealed record ChatConversationPage(IReadOnlyList<ChatConversation> Items, string? NextCursor);
public sealed record ChatMessagePage(IReadOnlyList<ChatMessage> Items, long? NextAfterSequence);
public sealed record SendChatMessageResult(ChatMessage Message, bool Deduplicated, long ServerSequence);
public sealed record CreateChatConversationResult(ChatConversation Conversation, ChatInboxEntry Inbox);
public sealed record CreateAllianceAnnouncementResult(ChatConversation Conversation, SendChatMessageResult SendResult);

public sealed record ChatEventEnvelope(
    string EventId,
    string EventType,
    DateTimeOffset OccurredAt,
    Guid ConversationId,
    long? Sequence,
    PlayerId? ActorId,
    object Payload,
    string Provider,
    int SchemaVersion);
