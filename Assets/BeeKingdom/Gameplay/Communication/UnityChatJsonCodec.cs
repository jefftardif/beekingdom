using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BeeKingdom.Gameplay.Communication
{
#pragma warning disable 0649 // JsonUtility renseigne ces champs par reflexion.
    public interface IChatJsonBackend
    {
        string ToJson(object value);
        T FromJson<T>(string json);
    }

    public sealed class UnityJsonBackend : IChatJsonBackend
    {
        public string ToJson(object value) => JsonUtility.ToJson(value);
        public T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);
    }

    public sealed class UnityChatJsonCodec : IChatJsonCodec, IChatErrorDecoder
    {
        private readonly IChatJsonBackend backend;
        public UnityChatJsonCodec(IChatJsonBackend backend = null) { this.backend = backend ?? new UnityJsonBackend(); }

        public string Serialize(object value)
        {
            if (value is RemoteSendMessageRequest send) return backend.ToJson(new WireSend { clientRequestId = send.ClientRequestId, body = send.Body, clientCreatedAt = send.ClientCreatedAt });
            if (value is RemoteMarkReadRequest read) return backend.ToJson(new WireRead { sequence = read.Sequence });
            if (value is RemoteReportMessageRequest report) return backend.ToJson(new WireReport { clientRequestId = report.ClientRequestId, category = report.Category });
            if (value is TranslationRequest translation) return backend.ToJson(new WireTranslationRequest { messageId = translation.MessageId, targetLocale = translation.TargetLocale, modelVersion = translation.ModelVersion });
            if (value is RemoteCreateConversationRequest conversation) return backend.ToJson(new WireCreateConversation { channelType = conversation.ChannelType, gameServerId = conversation.GameServerId, worldId = conversation.WorldId, audienceKey = conversation.AudienceKey, title = conversation.Title, participantIds = conversation.ParticipantIds.ToArray(), clientRequestId = conversation.ClientRequestId });
            throw new NotSupportedException("Unsupported chat JSON request type: " + (value == null ? "null" : value.GetType().FullName));
        }

        public T Deserialize<T>(string json)
        {
            object value;
            Type type = typeof(T);
            if (type == typeof(object)) value = json;
            else if (type == typeof(RemoteCapabilities)) { WireCapabilities wire = backend.FromJson<WireCapabilities>(json); value = new RemoteCapabilities { Provider = wire.provider, Server = wire.server, OfficialGain = wire.officialGain, Realtime = wire.realtime, Emojis = wire.emojis, Mentions = wire.mentions, OfflineDelivery = wire.offlineDelivery, ReadCursors = wire.readCursors, ModerationReports = wire.moderationReports, ProtocolVersion = wire.protocolVersion, IdempotencyReceiptRetentionDays = wire.idempotencyReceiptRetentionDays, Channels = (wire.channels ?? Array.Empty<string>()).ToList(), Limits = wire.limits == null ? null : new RemoteChatLimits { BodyMaxCharacters = wire.limits.bodyMaxCharacters, MessagesPerMinutePerPlayer = wire.limits.messagesPerMinutePerPlayer, MessagesPerTenSecondsPerConversation = wire.limits.messagesPerTenSecondsPerConversation, PrivateConversationCreatesPerHour = wire.limits.privateConversationCreatesPerHour, MaxPrivateRecipients = wire.limits.maxPrivateRecipients }, TranslationAvailable = wire.translationAvailable, TranslationModelVersion = wire.translationModelVersion }; }
            else if (type == typeof(RemoteConversationPage)) value = Map(backend.FromJson<WireConversationPage>(json));
            else if (type == typeof(RemoteMessagePage)) value = Map(backend.FromJson<WireMessagePage>(json));
            else if (type == typeof(RemoteSendResult)) { WireSendResult wire = backend.FromJson<WireSendResult>(json); value = new RemoteSendResult { Message = Map(wire.message), Deduplicated = wire.deduplicated, ServerSequence = wire.serverSequence }; }
            else if (type == typeof(RemoteCreateConversationResult)) { WireCreateResult wire = backend.FromJson<WireCreateResult>(json); value = new RemoteCreateConversationResult { Conversation = Map(wire.conversation), Inbox = Map(wire.inbox), ClientRequestId = wire.clientRequestId }; }
            else if (type == typeof(RemoteModerationReport)) { WireModerationReport wire = backend.FromJson<WireModerationReport>(json); value = new RemoteModerationReport { ReportId = wire.reportId, MessageId = wire.messageId, ClientRequestId = wire.clientRequestId, Status = wire.status }; }
            else if (type == typeof(RemoteInboxEntry)) value = Map(backend.FromJson<WireInbox>(json));
            else if (type == typeof(MessageTranslation)) { WireTranslation wire = backend.FromJson<WireTranslation>(json); value = new MessageTranslation { MessageId = wire.messageId, SourceLocale = wire.sourceLocale, TargetLocale = wire.targetLocale, ModelVersion = wire.modelVersion, TranslatedText = wire.translatedText, Status = wire.status }; }
            else throw new NotSupportedException("Unsupported chat JSON response type: " + type.FullName);
            return (T)value;
        }

        public RemoteChatProblem Decode(string rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody)) return null;
            try
            {
                WireProblem wire = backend.FromJson<WireProblem>(rawBody);
                if (wire == null || string.IsNullOrWhiteSpace(wire.code)) return null;
                return new RemoteChatProblem { Code = wire.code, Message = wire.message, RetryAfterSeconds = wire.retryAfterSeconds > 0 ? wire.retryAfterSeconds : (int?)null };
            }
            catch { return null; }
        }

        private static RemoteConversationPage Map(WireConversationPage wire)
        {
            var result = new RemoteConversationPage { NextCursor = wire == null ? null : wire.nextCursor };
            if (wire?.items != null) foreach (WireConversation item in wire.items) result.Items.Add(Map(item));
            return result;
        }

        private static RemoteMessagePage Map(WireMessagePage wire)
        {
            var result = new RemoteMessagePage { NextAfterSequence = wire == null || wire.nextAfterSequence <= 0 ? (long?)null : wire.nextAfterSequence };
            if (wire?.items != null) foreach (WireMessage item in wire.items) result.Items.Add(Map(item));
            return result;
        }

        private static RemoteConversation Map(WireConversation wire) => wire == null ? null : new RemoteConversation { ConversationId = wire.conversationId, Title = wire.title, ChannelType = wire.channelType, LastSequence = wire.lastSequence, ReadCursorSequence = wire.readCursorSequence, UnreadCount = wire.unreadCount, MentionCount = wire.mentionCount };
        private static RemoteInboxEntry Map(WireInbox wire) => wire == null ? null : new RemoteInboxEntry { ConversationId = wire.conversationId, ReadCursorSequence = wire.readCursorSequence, UnreadCount = wire.unreadCount, MentionCount = wire.mentionCount, IsMuted = wire.isMuted, IsArchived = wire.isArchived };
        private static RemoteChatMessage Map(WireMessage wire)
        {
            DateTimeOffset created;
            DateTimeOffset.TryParse(wire?.acceptedAtUtc ?? wire?.clientCreatedAtUtc, out created);
            return wire == null ? null : new RemoteChatMessage { MessageId = wire.messageId, ConversationId = wire.conversationId, Sequence = wire.sequence, ClientRequestId = wire.clientRequestId, SenderId = wire.senderPlayerId, SenderDisplayName = wire.senderDisplayNameSnapshot, ChannelType = wire.channelType, OriginalBody = wire.body, CreatedAt = created };
        }

        [Serializable] private sealed class WireSend { public string clientRequestId; public string body; public string clientCreatedAt; }
        [Serializable] private sealed class WireRead { public long sequence; }
        [Serializable] private sealed class WireReport { public string clientRequestId; public string category; }
        [Serializable] private sealed class WireTranslationRequest { public string messageId; public string targetLocale; public string modelVersion; }
        [Serializable] private sealed class WireCreateConversation { public string channelType; public string gameServerId; public string worldId; public string audienceKey; public string title; public string[] participantIds; public string clientRequestId; }
        [Serializable] private sealed class WireCapabilities { public string provider; public bool server; public bool officialGain; public string protocolVersion; public string[] channels; public bool emojis; public bool mentions; public bool offlineDelivery; public bool readCursors; public bool moderationReports; public bool realtime; public int idempotencyReceiptRetentionDays; public WireLimits limits; public bool translationAvailable; public string translationModelVersion; }
        [Serializable] private sealed class WireLimits { public int bodyMaxCharacters; public int messagesPerMinutePerPlayer; public int messagesPerTenSecondsPerConversation; public int privateConversationCreatesPerHour; public int maxPrivateRecipients; }
        [Serializable] private sealed class WireConversationPage { public WireConversation[] items; public string nextCursor; }
        [Serializable] private sealed class WireMessagePage { public WireMessage[] items; public long nextAfterSequence; }
        [Serializable] private sealed class WireConversation { public string conversationId; public string title; public string channelType; public long lastSequence; public long readCursorSequence; public int unreadCount; public int mentionCount; }
        [Serializable] private sealed class WireMessage { public string messageId; public string conversationId; public long sequence; public string clientRequestId; public string senderPlayerId; public string senderDisplayNameSnapshot; public string channelType; public string body; public string clientCreatedAtUtc; public string acceptedAtUtc; }
        [Serializable] private sealed class WireSendResult { public WireMessage message; public bool deduplicated; public long serverSequence; }
        [Serializable] private sealed class WireCreateResult { public WireConversation conversation; public WireInbox inbox; public string clientRequestId; }
        [Serializable] private sealed class WireInbox { public string conversationId; public long readCursorSequence; public int unreadCount; public int mentionCount; public bool isMuted; public bool isArchived; }
        [Serializable] private sealed class WireModerationReport { public string reportId; public string messageId; public string clientRequestId; public string status; }
        [Serializable] private sealed class WireTranslation { public string messageId; public string sourceLocale; public string targetLocale; public string modelVersion; public string translatedText; public string status; }
        [Serializable] private sealed class WireProblem { public string code; public string message; public int retryAfterSeconds; }
    }
#pragma warning restore 0649
}
