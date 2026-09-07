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

// RAP-OPTIONNEL-COMMUNICATIONS_01: a pending/answered invitation to join a Group conversation.
// Status is one of ChatGroupInviteStatus's constants. Kept as a separate row (not a participant
// with CanRead=false) so a declined invitation stays auditable and the inviter can be told about
// the refusal at his next poll.
public sealed record ChatGroupInvite(
    Guid InviteId,
    Guid ConversationId,
    PlayerId InviterPlayerId,
    PlayerId InviteePlayerId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? RespondedAtUtc,
    bool InviterAcknowledged = false);

public static class ChatGroupInviteStatus
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string Declined = "Declined";
    public const string Cancelled = "Cancelled";

    public static bool IsKnown(string value)
        => value is Pending or Accepted or Declined or Cancelled;
}

// Server-side, cross-device chat preference. Deliberately server-side (not PlayerPrefs) because the
// auto-response rule must apply even when the invited player is offline or playing from the future
// web client; purely cosmetic preferences (accent colour) stay client-local.
public sealed record ChatPreferences(
    PlayerId PlayerId,
    string AutoInviteResponse,
    DateTimeOffset UpdatedAtUtc);

public static class ChatAutoInviteResponse
{
    public const string Ask = "Ask";
    public const string Accept = "Accept";
    public const string Decline = "Decline";

    public static bool IsKnown(string value) => value is Ask or Accept or Decline;
}
