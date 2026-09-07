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
    string TranslationModelVersion)
{
    // RAP-OPTIONNEL-COMMUNICATIONS_01: the world scope a client must quote to CREATE a conversation
    // or a group. Deliberately init-only properties rather than positional parameters so every
    // existing construction site and test keeps compiling unchanged; the transport layer fills them
    // in at the /chat/v1/capabilities endpoint, where the server identity is available.
    public string? GameServerId { get; init; }
    public string? DefaultWorldId { get; init; }
}

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

// ==================== Player-created groups (RAP-OPTIONNEL-COMMUNICATIONS_01) ====================
// Shaped to be consumed as-is by a future web client: every response carries display names already
// resolved server-side, so no client needs a second round trip to the player directory to render.

public sealed record CreateChatGroupRequest(
    Guid GameServerId,
    Guid WorldId,
    string Title,
    IReadOnlyList<Guid>? InviteePlayerIds,
    string ClientRequestId);

public sealed record InviteToChatGroupRequest(Guid InviteePlayerId);
public sealed record RespondToChatGroupInviteRequest(bool Accept);
public sealed record TransferChatGroupLeadershipRequest(Guid NewLeaderPlayerId);
public sealed record UpdateChatPreferencesRequest(string AutoInviteResponse);
public sealed record AcknowledgeChatInvitationsRequest(IReadOnlyList<Guid>? InviteIds);

public sealed record ChatGroupMemberDto(Guid PlayerId, string DisplayName, ChatPermissionRole Role, bool IsLeader, DateTimeOffset JoinedAtUtc);

public sealed record ChatGroupDetail(
    Guid ConversationId,
    string? Title,
    Guid? OwnerPlayerId,
    bool ViewerIsLeader,
    IReadOnlyList<ChatGroupMemberDto> Members,
    IReadOnlyList<ChatGroupInviteDto> PendingInvites);

public sealed record ChatGroupInviteDto(
    Guid InviteId,
    Guid ConversationId,
    string GroupTitle,
    Guid InviterPlayerId,
    string InviterDisplayName,
    Guid InviteePlayerId,
    string InviteeDisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? RespondedAtUtc);

public sealed record CreateChatGroupResult(ChatConversation Conversation, ChatGroupDetail Detail);

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
