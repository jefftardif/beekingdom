using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Chat.Models;

public sealed record ChatConversation(
    Guid ConversationId,
    Guid GameServerId,
    Guid WorldId,
    ChatChannelType ChannelType,
    string AudienceKey,
    string? Title,
    PlayerId? CreatedByPlayerId,
    DateTimeOffset CreatedAtUtc,
    Guid? LastMessageId,
    DateTimeOffset? LastActivityAtUtc,
    string RetentionPolicy,
    int SchemaVersion);

public sealed record ChatConversationParticipant(
    Guid ConversationId,
    PlayerId PlayerId,
    ChatPermissionRole Role,
    DateTimeOffset JoinedAtUtc,
    DateTimeOffset? RemovedAtUtc,
    bool CanRead,
    bool CanWrite);

public sealed record ChatMessage(
    Guid MessageId,
    Guid ConversationId,
    Guid GameServerId,
    Guid WorldId,
    ChatChannelType ChannelType,
    PlayerId SenderPlayerId,
    string SenderDisplayNameSnapshot,
    string Body,
    IReadOnlyList<ChatContentPart> ContentParts,
    IReadOnlyList<ChatMention> Mentions,
    IReadOnlyList<ChatEmoji> Emoji,
    Guid? ReplyToMessageId,
    DateTimeOffset ClientCreatedAtUtc,
    DateTimeOffset AcceptedAtUtc,
    long Sequence,
    string ClientRequestId,
    ChatMessageState State,
    ChatModerationStatus ModerationStatus,
    string? ModerationReasonCode,
    DateTimeOffset? EditedAtUtc,
    DateTimeOffset? DeletedAtUtc,
    int SchemaVersion);

public sealed record ChatContentPart(string Kind, string? Text, string? Shortcode, string? Unicode, string? Alt, string? PlayerId, string? Label);
public sealed record ChatMention(PlayerId PlayerId, string Label);
public sealed record ChatEmoji(string Shortcode, string Unicode, string Alt);

public sealed record ChatInboxEntry(
    PlayerId PlayerId,
    Guid ConversationId,
    Guid? LastMessageId,
    DateTimeOffset? LastActivityAtUtc,
    long ReadCursorSequence,
    int UnreadCount,
    int MentionCount,
    bool IsMuted,
    bool IsArchived,
    DateTimeOffset UpdatedAtUtc);

public sealed record ChatOutboxReceipt(
    PlayerId PlayerId,
    Guid ConversationId,
    string ClientRequestId,
    string PayloadHash,
    Guid? MessageId,
    DateTimeOffset? AcceptedAtUtc,
    string? LastErrorCode);

public sealed record ChatConversationCreationReceipt(PlayerId PlayerId,string ClientRequestId,string PayloadHash,Guid ConversationId,DateTimeOffset CreatedAtUtc);

public sealed record ChatModerationReport(
    Guid ReportId,
    Guid MessageId,
    PlayerId ReporterPlayerId,
    string Category,
    DateTimeOffset CreatedAtUtc,
    string Status);

public sealed record ChatModerationReportReceipt(PlayerId ReporterPlayerId,string ClientRequestId,string PayloadHash,Guid ReportId,DateTimeOffset CreatedAtUtc);
