using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BeeKingdom.Gameplay.Communication
{
    public enum ChannelType
    {
        Alliance,
        Server,
        Private,
        Leadership
    }

    public enum ChatRole
    {
        Member,
        Officer,
        Leader,
        Moderator,
        System
    }

    public enum MessageState
    {
        Queued,
        Accepted,
        Delivered,
        Failed,
        Hidden,
        Deleted,
        Expired
    }

    public enum ModerationStatus
    {
        Clear,
        Pending,
        Blocked,
        Masked,
        Review
    }

    public enum ConnectionState
    {
        Offline,
        Online
    }

    public enum ChatEventType
    {
        ConversationCreated,
        MessageQueued,
        MessageCreated,
        MessageDelivered,
        MessageUpdated,
        MessageModerated,
        MessageDeleted,
        InboxUpdated,
        ProviderStatusChanged,
        SyncCompleted
    }

    public enum SendFailureCode
    {
        None,
        Offline,
        Forbidden,
        EmptyBody,
        BodyTooLong,
        InvalidRecipient,
        Blocked,
        Masked,
        DuplicateSuppressed,
        RateLimited,
        UnknownConversation,
        InvalidRequest
    }

    public readonly struct ConversationId : IEquatable<ConversationId>
    {
        public string Value { get; }

        public ConversationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Conversation id is required.", nameof(value));
            Value = value;
        }

        public static ConversationId ForAlliance(string allianceId) => new ConversationId("alliance:" + RequirePart(allianceId, nameof(allianceId)));
        public static ConversationId ForServer(string serverId) => new ConversationId("server:" + RequirePart(serverId, nameof(serverId)));
        public static ConversationId ForLeadership(string allianceId) => new ConversationId("leaders:" + RequirePart(allianceId, nameof(allianceId)));
        public static ConversationId ForPrivatePair(string firstPlayerId, string secondPlayerId)
        {
            string first = RequirePart(firstPlayerId, nameof(firstPlayerId));
            string second = RequirePart(secondPlayerId, nameof(secondPlayerId));
            if (string.Equals(first, second, StringComparison.Ordinal)) throw new ArgumentException("A private pair needs two players.");
            return new ConversationId("private:" + string.Join("|", new[] { first, second }.OrderBy(value => value, StringComparer.Ordinal)));
        }

        public bool Equals(ConversationId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ConversationId && Equals((ConversationId)obj);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(ConversationId left, ConversationId right) => left.Equals(right);
        public static bool operator !=(ConversationId left, ConversationId right) => !left.Equals(right);

        private static string RequirePart(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("An id part is required.", parameterName);
            return value.Trim();
        }
    }

    public readonly struct MessageId : IEquatable<MessageId>
    {
        public string Value { get; }

        public MessageId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Message id is required.", nameof(value));
            Value = value;
        }

        public static MessageId ForClientRequest(string providerSeed, ClientRequestId requestId)
        {
            return new MessageId("msg_" + Clean(providerSeed) + "_" + Clean(requestId.Value));
        }

        public bool Equals(MessageId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MessageId && Equals((MessageId)obj);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(MessageId left, MessageId right) => left.Equals(right);
        public static bool operator !=(MessageId left, MessageId right) => !left.Equals(right);

        private static string Clean(string value)
        {
            return (value ?? string.Empty).Trim().Replace(" ", "_").Replace(":", "_").Replace("|", "_");
        }
    }

    public readonly struct ClientRequestId : IEquatable<ClientRequestId>
    {
        public string Value { get; }

        public ClientRequestId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Client request id is required.", nameof(value));
            Value = value.Trim();
        }

        public bool Equals(ClientRequestId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ClientRequestId && Equals((ClientRequestId)obj);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(ClientRequestId left, ClientRequestId right) => left.Equals(right);
        public static bool operator !=(ClientRequestId left, ClientRequestId right) => !left.Equals(right);
    }

    public sealed class LocalChatUser
    {
        private readonly ReadOnlyCollection<ChatRole> roles;

        public string PlayerId { get; }
        public string DisplayName { get; }
        public string AllianceId { get; }
        public string ServerId { get; }
        public bool IsConnected { get; }
        public bool IsSuspended { get; }
        public IReadOnlyList<ChatRole> Roles => roles;

        public LocalChatUser(string playerId, string displayName, string allianceId, string serverId, IEnumerable<ChatRole> roles = null, bool isConnected = true, bool isSuspended = false)
        {
            if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("Player id is required.", nameof(playerId));
            PlayerId = playerId.Trim();
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? PlayerId : displayName.Trim();
            AllianceId = allianceId == null ? string.Empty : allianceId.Trim();
            ServerId = serverId == null ? string.Empty : serverId.Trim();
            this.roles = new ReadOnlyCollection<ChatRole>((roles ?? new[] { ChatRole.Member }).Distinct().ToList());
            IsConnected = isConnected;
            IsSuspended = isSuspended;
        }

        public bool HasRole(ChatRole role) => roles.Contains(role);
    }

    public sealed class ChatCapabilities
    {
        public string Provider { get; }
        public bool Server { get; }
        public bool OfficialGain { get; }
        public string NetworkTransport { get; }
        public string FixtureSeed { get; }
        public bool SupportsEmojis { get; }
        public bool SupportsMentions { get; }
        public IReadOnlyList<ChannelType> SupportedChannels { get; }
        public LocalChatLimits Limits { get; }

        public ChatCapabilities(string fixtureSeed, LocalChatLimits limits)
        {
            Provider = "local";
            Server = false;
            OfficialGain = false;
            NetworkTransport = "none";
            FixtureSeed = fixtureSeed;
            SupportsEmojis = true;
            SupportsMentions = true;
            SupportedChannels = new ReadOnlyCollection<ChannelType>(new[] { ChannelType.Alliance, ChannelType.Server, ChannelType.Private, ChannelType.Leadership });
            Limits = limits;
        }
    }

    public sealed class LocalChatLimits
    {
        public int MaxBodyCharacters { get; }
        public int MaxPrivateRecipients { get; }
        public TimeSpan DuplicateWindow { get; }
        public TimeSpan OutboxExpiration { get; }

        public LocalChatLimits(int maxBodyCharacters = 500, int maxPrivateRecipients = 20, TimeSpan? duplicateWindow = null, TimeSpan? outboxExpiration = null)
        {
            MaxBodyCharacters = maxBodyCharacters;
            MaxPrivateRecipients = maxPrivateRecipients;
            DuplicateWindow = duplicateWindow ?? TimeSpan.FromSeconds(30);
            OutboxExpiration = outboxExpiration ?? TimeSpan.FromHours(24);
        }
    }

    public sealed class ManualChatClock : IChatClock
    {
        public DateTime UtcNow { get; private set; }

        public ManualChatClock(DateTime utcNow)
        {
            UtcNow = utcNow.ToUniversalTime();
        }

        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
        public void Set(DateTime utcNow) => UtcNow = utcNow.ToUniversalTime();
    }

    public sealed class Conversation
    {
        private readonly ReadOnlyCollection<string> participantIds;
        private readonly Dictionary<string, bool> archivedFor = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> mutedFor = new Dictionary<string, bool>(StringComparer.Ordinal);

        public ConversationId Id { get; }
        public ConversationId ConversationId => Id;
        public ChannelType ChannelType { get; }
        public string Title { get; }
        public IReadOnlyList<string> ParticipantIds => participantIds;
        public string CreatedBy { get; }
        public DateTime CreatedAt { get; }
        public MessageId? LastMessageId { get; internal set; }
        public DateTime? LastActivityAt { get; internal set; }

        public Conversation(ConversationId id, ChannelType channelType, string title, IEnumerable<string> participantIds, string createdBy, DateTime createdAt)
        {
            Id = id;
            ChannelType = channelType;
            Title = string.IsNullOrWhiteSpace(title) ? id.Value : title;
            this.participantIds = new ReadOnlyCollection<string>((participantIds ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList());
            CreatedBy = createdBy;
            CreatedAt = createdAt;
        }

        public bool HasParticipant(string playerId) => participantIds.Contains(playerId, StringComparer.Ordinal);
        public bool IsArchivedFor(string playerId) => archivedFor.TryGetValue(playerId, out bool value) && value;
        public bool IsMutedFor(string playerId) => mutedFor.TryGetValue(playerId, out bool value) && value;
        internal void SetArchived(string playerId, bool value) => archivedFor[playerId] = value;
        internal void SetMuted(string playerId, bool value) => mutedFor[playerId] = value;
    }

    public sealed class ModerationInfo
    {
        public ModerationStatus Status { get; internal set; }
        public string ReasonCode { get; internal set; }
        public DateTime CheckedAt { get; internal set; }
        public string PolicyVersion { get; internal set; }
    }

    public sealed class MessageRecord
    {
        private readonly ReadOnlyCollection<string> recipientIds;
        private readonly ReadOnlyCollection<string> mentions;

        public MessageId MessageId { get; }
        public ConversationId ConversationId { get; }
        public ChannelType ChannelType { get; }
        public string SenderId { get; }
        public string SenderDisplayName { get; }
        public IReadOnlyList<string> RecipientIds => recipientIds;
        public string Body { get; internal set; }
        public IReadOnlyList<string> Mentions => mentions;
        public MessageId? ReplyToMessageId { get; }
        public DateTime ClientCreatedAt { get; }
        public DateTime? AcceptedAt { get; internal set; }
        public int? Sequence { get; internal set; }
        public ClientRequestId ClientRequestId { get; }
        public MessageState State { get; internal set; }
        public ModerationInfo Moderation { get; }
        public DateTime? EditedAt { get; internal set; }
        public DateTime? DeletedAt { get; internal set; }
        public int SchemaVersion { get; }

        public MessageRecord(MessageId messageId, ConversationId conversationId, ChannelType channelType, string senderId, string senderDisplayName, IEnumerable<string> recipientIds, string body, IEnumerable<string> mentions, MessageId? replyToMessageId, DateTime clientCreatedAt, ClientRequestId clientRequestId, MessageState state, ModerationInfo moderation)
        {
            MessageId = messageId;
            ConversationId = conversationId;
            ChannelType = channelType;
            SenderId = senderId;
            SenderDisplayName = senderDisplayName;
            this.recipientIds = new ReadOnlyCollection<string>((recipientIds ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList());
            Body = body ?? string.Empty;
            this.mentions = new ReadOnlyCollection<string>((mentions ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList());
            ReplyToMessageId = replyToMessageId;
            ClientCreatedAt = clientCreatedAt;
            ClientRequestId = clientRequestId;
            State = state;
            Moderation = moderation ?? new ModerationInfo { Status = ModerationStatus.Pending, PolicyVersion = "local-v1" };
            SchemaVersion = 1;
        }
    }

    public sealed class InboxEntry
    {
        public string UserId { get; }
        public ConversationId ConversationId { get; }
        public MessageId? LastMessageId { get; internal set; }
        public DateTime? LastActivityAt { get; internal set; }
        public int UnreadCount { get; internal set; }
        public int MentionCount { get; internal set; }
        public bool IsMuted { get; internal set; }
        public bool IsArchived { get; internal set; }
        public int ReadCursor { get; internal set; }

        public InboxEntry(string userId, ConversationId conversationId)
        {
            UserId = userId;
            ConversationId = conversationId;
        }
    }

    public sealed class OutboxEntry
    {
        public ClientRequestId ClientRequestId { get; }
        public ConversationId ConversationId { get; }
        public MessageId MessageId { get; }
        public string PayloadHash { get; }
        public int AttemptCount { get; internal set; }
        public DateTime CreatedAt { get; }
        public DateTime NextAttemptAt { get; internal set; }
        public string LastErrorCode { get; internal set; }

        public OutboxEntry(ClientRequestId clientRequestId, ConversationId conversationId, MessageId messageId, string payloadHash, DateTime createdAt)
        {
            ClientRequestId = clientRequestId;
            ConversationId = conversationId;
            MessageId = messageId;
            PayloadHash = payloadHash;
            CreatedAt = createdAt;
            NextAttemptAt = createdAt;
        }
    }

    public sealed class ConversationFilter
    {
        public ChannelType? ChannelType { get; set; }
        public bool IncludeArchived { get; set; }
        public string SearchText { get; set; }
    }

    public sealed class ConversationPage
    {
        public IReadOnlyList<Conversation> Items { get; }
        public bool HasMore { get; }

        public ConversationPage(IEnumerable<Conversation> items, bool hasMore = false)
        {
            Items = new ReadOnlyCollection<Conversation>((items ?? Enumerable.Empty<Conversation>()).ToList());
            HasMore = hasMore;
        }
    }

    public sealed class MessagePage
    {
        public IReadOnlyList<MessageRecord> Items { get; }
        public int? NextSequence { get; }
        public bool HasMore { get; }

        public MessagePage(IEnumerable<MessageRecord> items, int? nextSequence = null, bool hasMore = false)
        {
            Items = new ReadOnlyCollection<MessageRecord>((items ?? Enumerable.Empty<MessageRecord>()).ToList());
            NextSequence = nextSequence;
            HasMore = hasMore;
        }
    }

    public sealed class CreateConversationInput
    {
        public ChannelType ChannelType { get; }
        public string ContextId { get; }
        public string Title { get; }
        public IReadOnlyList<string> ParticipantIds { get; }

        public CreateConversationInput(ChannelType channelType, string contextId, string title, IEnumerable<string> participantIds)
        {
            ChannelType = channelType;
            ContextId = contextId;
            Title = title;
            ParticipantIds = new ReadOnlyCollection<string>((participantIds ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).ToList());
        }
    }

    public sealed class SendMessageInput
    {
        public ConversationId ConversationId { get; }
        public string SenderId { get; }
        public string Body { get; }
        public IReadOnlyList<string> RecipientIds { get; }
        public IReadOnlyList<string> Mentions { get; }
        public MessageId? ReplyToMessageId { get; }
        public ClientRequestId ClientRequestId { get; }
        public DateTime? ClientCreatedAt { get; }

        public SendMessageInput(ConversationId conversationId, string senderId, string body, ClientRequestId clientRequestId, IEnumerable<string> recipientIds = null, IEnumerable<string> mentions = null, MessageId? replyToMessageId = null, DateTime? clientCreatedAt = null)
        {
            ConversationId = conversationId;
            SenderId = senderId;
            Body = body ?? string.Empty;
            ClientRequestId = clientRequestId;
            RecipientIds = new ReadOnlyCollection<string>((recipientIds ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).ToList());
            Mentions = new ReadOnlyCollection<string>((mentions ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).ToList());
            ReplyToMessageId = replyToMessageId;
            ClientCreatedAt = clientCreatedAt;
        }
    }

    public sealed class SendResult
    {
        public MessageRecord Message { get; }
        public bool Accepted => Message != null && (Message.State == MessageState.Accepted || Message.State == MessageState.Delivered || Message.State == MessageState.Hidden);
        public bool Queued => Message != null && Message.State == MessageState.Queued;
        public bool Deduplicated { get; }
        public SendFailureCode FailureCode { get; }
        public string ErrorCode { get; }

        public SendResult(MessageRecord message, bool deduplicated = false, SendFailureCode failureCode = SendFailureCode.None, string errorCode = null)
        {
            Message = message;
            Deduplicated = deduplicated;
            FailureCode = failureCode;
            ErrorCode = errorCode ?? failureCode.ToString().ToLowerInvariant();
        }
    }

    public sealed class ReadCursor
    {
        public string UserId { get; }
        public ConversationId ConversationId { get; }
        public int Sequence { get; }

        public ReadCursor(string userId, ConversationId conversationId, int sequence)
        {
            UserId = userId;
            ConversationId = conversationId;
            Sequence = sequence;
        }
    }

    public sealed class ModerationReport
    {
        public string ReportId { get; }
        public MessageId MessageId { get; }
        public string ReporterId { get; }
        public string Category { get; }
        public DateTime CreatedAt { get; }

        public ModerationReport(string reportId, MessageId messageId, string reporterId, string category, DateTime createdAt)
        {
            ReportId = reportId;
            MessageId = messageId;
            ReporterId = reporterId;
            Category = category;
            CreatedAt = createdAt;
        }
    }

    public sealed class ChatEvent
    {
        public string EventId { get; }
        public ChatEventType EventType { get; }
        public DateTime OccurredAt { get; }
        public ConversationId ConversationId { get; }
        public int? Sequence { get; }
        public string ActorId { get; }
        public object Payload { get; }
        public string Provider { get; }
        public int SchemaVersion { get; }

        public ChatEvent(string eventId, ChatEventType eventType, DateTime occurredAt, ConversationId conversationId, int? sequence, string actorId, object payload)
        {
            EventId = eventId;
            EventType = eventType;
            OccurredAt = occurredAt;
            ConversationId = conversationId;
            Sequence = sequence;
            ActorId = actorId;
            Payload = payload;
            Provider = "local";
            SchemaVersion = 1;
        }
    }

    public interface IChatProvider
    {
        ChatCapabilities GetCapabilities();
        ConversationPage ListConversations(string userId, ConversationFilter filter = null);
        MessagePage GetMessages(ConversationId conversationId, int afterSequence = 0, int limit = 50);
        Conversation CreateConversation(CreateConversationInput input);
        SendResult SendMessage(SendMessageInput input);
        SendResult RetryMessage(ClientRequestId clientRequestId);
        ReadCursor MarkConversationRead(ConversationId conversationId, int sequence);
        InboxEntry SetMuted(ConversationId conversationId, bool muted);
        ModerationReport ReportMessage(MessageId messageId, string category);
        IDisposable Subscribe(Action<ChatEvent> listener);
        ConnectionState GetConnectionState();
    }
}
