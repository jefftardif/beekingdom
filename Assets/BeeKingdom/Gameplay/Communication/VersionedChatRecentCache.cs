using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeeKingdom.Gameplay.Communication
{
    public sealed class ChatRecentCacheException : IOException
    {
        public bool Quarantined { get; }
        public ChatRecentCacheException(string message, bool quarantined, Exception inner = null) : base(message, inner) { Quarantined = quarantined; }
    }

    public sealed class ChatRecentCacheSnapshot
    {
        public string SelectedConversationId { get; set; }
        public IReadOnlyList<LivingHiveChatConversation> Conversations { get; set; } = Array.Empty<LivingHiveChatConversation>();
        public IReadOnlyList<LivingHiveChatMessage> Messages { get; set; } = Array.Empty<LivingHiveChatMessage>();
    }

    public interface IChatRecentCache
    {
        ChatRecentCacheSnapshot Load();
        void Save(ChatRecentCacheSnapshot snapshot);
        void Clear();
    }

    public sealed class VersionedChatRecentCache : IChatRecentCache
    {
        public const int CurrentSchemaVersion = 1;
        private readonly IChatStringStore rawStore;
        private readonly IChatStringStore protectedStore;
        private readonly IChatJsonBackend json;
        private readonly string key;
        private readonly string quarantineKey;
        private readonly string previousQuarantineKey;
        private readonly string stagingQuarantineKey;
        private readonly int maxMessages;
        private readonly int maxSerializedCharacters;
        private readonly object gate = new object();

        public VersionedChatRecentCache(IChatStringStore rawStore, IChatStringStore protectedStore, IChatJsonBackend json, string key, int maxMessages = 100, int maxSerializedCharacters = 524288)
        {
            this.rawStore = rawStore ?? throw new ArgumentNullException(nameof(rawStore));
            this.protectedStore = protectedStore ?? throw new ArgumentNullException(nameof(protectedStore));
            this.json = json ?? throw new ArgumentNullException(nameof(json));
            this.key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("Recent-cache key is required.", nameof(key)) : key;
            quarantineKey = key + ".Quarantine.v1";
            previousQuarantineKey = key + ".Quarantine.Previous.v1";
            stagingQuarantineKey = key + ".Quarantine.Staging.v1";
            if (maxMessages < 20 || maxMessages > 500) throw new ArgumentOutOfRangeException(nameof(maxMessages));
            if (maxSerializedCharacters < 4096 || maxSerializedCharacters > 2097152) throw new ArgumentOutOfRangeException(nameof(maxSerializedCharacters));
            this.maxMessages = maxMessages;
            this.maxSerializedCharacters = maxSerializedCharacters;
        }

        public ChatRecentCacheSnapshot Load()
        {
            lock (gate)
            {
                try
                {
                    string value = protectedStore.Read(key);
                    if (string.IsNullOrWhiteSpace(value)) return new ChatRecentCacheSnapshot();
                    EnsureSize(value);
                    WireCache wire = json.FromJson<WireCache>(value);
                    Validate(wire);
                    return Map(wire);
                }
                catch (Exception exception) when (!(exception is ChatRecentCacheException))
                {
                    bool quarantined = QuarantineEncryptedSource();
                    throw new ChatRecentCacheException("Recent chat cache was invalid; its encrypted source was preserved in quarantine.", quarantined, exception);
                }
            }
        }

        public void Save(ChatRecentCacheSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            lock (gate)
            {
                WireCache wire = Map(snapshot);
                Validate(wire);
                string value = json.ToJson(wire);
                EnsureSize(value);
                protectedStore.Write(key, value);
            }
        }

        public void Clear() { lock (gate) protectedStore.Delete(key); }

        private bool QuarantineEncryptedSource()
        {
            string encrypted = rawStore.Read(key);
            if (string.IsNullOrEmpty(encrypted)) return false;
            rawStore.Write(stagingQuarantineKey, encrypted);
            if (!string.Equals(rawStore.Read(stagingQuarantineKey), encrypted, StringComparison.Ordinal)) throw new ChatRecentCacheException("Recent-cache quarantine staging verification failed; source was preserved.", false);
            string current = rawStore.Read(quarantineKey);
            if (!string.IsNullOrEmpty(current))
            {
                rawStore.Write(previousQuarantineKey, current);
                if (!string.Equals(rawStore.Read(previousQuarantineKey), current, StringComparison.Ordinal)) throw new ChatRecentCacheException("Recent-cache previous quarantine verification failed; source was preserved.", false);
            }
            rawStore.Write(quarantineKey, encrypted);
            if (!string.Equals(rawStore.Read(quarantineKey), encrypted, StringComparison.Ordinal)) throw new ChatRecentCacheException("Recent-cache quarantine verification failed; source was preserved.", false);
            rawStore.Delete(key);
            rawStore.Delete(stagingQuarantineKey);
            return true;
        }

        private void EnsureSize(string value) { if (value == null || value.Length > maxSerializedCharacters) throw new InvalidDataException("Recent chat cache exceeds its configured serialized size."); }

        private void Validate(WireCache wire)
        {
            if (wire == null || wire.schemaVersion != CurrentSchemaVersion || wire.conversations == null || wire.messages == null) throw new InvalidDataException("Recent chat cache has an unsupported or incomplete schema.");
            if (wire.messages.Length > maxMessages || wire.conversations.Length > 100) throw new InvalidDataException("Recent chat cache exceeds its configured capacity.");
            var conversationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (WireConversation item in wire.conversations)
                if (item == null || string.IsNullOrWhiteSpace(item.conversationId) || item.lastSequence < 0 || item.unreadCount < 0 || item.mentionCount < 0 || !conversationIds.Add(item.conversationId)) throw new InvalidDataException("Recent chat cache contains an invalid conversation.");
            if (!string.IsNullOrWhiteSpace(wire.selectedConversationId) && !conversationIds.Contains(wire.selectedConversationId)) throw new InvalidDataException("Recent chat cache selects an inaccessible conversation.");
            var messageIds = new HashSet<string>(StringComparer.Ordinal);
            long previous = -1;
            foreach (WireMessage item in wire.messages)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.messageId) || string.IsNullOrWhiteSpace(item.conversationId) || !conversationIds.Contains(item.conversationId) || item.sequence < 0 || item.sequence < previous || item.body == null || item.body.Length > 16000 || !DateTimeOffset.TryParse(item.createdAt, out _) || !messageIds.Add(item.messageId)) throw new InvalidDataException("Recent chat cache contains an invalid message.");
                if (!string.Equals(item.conversationId, wire.selectedConversationId, StringComparison.Ordinal)) throw new InvalidDataException("Recent chat cache contains a message outside its selected conversation.");
                previous = item.sequence;
            }
        }

        private ChatRecentCacheSnapshot Map(WireCache wire) => new ChatRecentCacheSnapshot
        {
            SelectedConversationId = wire.selectedConversationId,
            Conversations = wire.conversations.Select(item => new LivingHiveChatConversation { ConversationId = item.conversationId, Title = item.title, ChannelType = item.channelType, LastSequence = item.lastSequence, UnreadCount = item.unreadCount, MentionCount = item.mentionCount }).ToArray(),
            Messages = wire.messages.Select(item => new LivingHiveChatMessage { MessageId = item.messageId, ConversationId = item.conversationId, ClientRequestId = item.clientRequestId, SenderPlayerId = item.senderPlayerId, SenderDisplayName = item.senderDisplayName, OriginalBody = item.body, VisibleBody = item.body, Sequence = item.sequence, CreatedAt = DateTimeOffset.Parse(item.createdAt), Delivery = LivingHiveChatDelivery.Confirmed }).ToArray()
        };

        private WireCache Map(ChatRecentCacheSnapshot snapshot)
        {
            LivingHiveChatConversation[] allConversations = (snapshot.Conversations ?? Array.Empty<LivingHiveChatConversation>()).Where(item => item != null).ToArray();
            var boundedConversations = allConversations.Take(100).ToList();
            LivingHiveChatConversation selected = allConversations.FirstOrDefault(item => string.Equals(item.ConversationId, snapshot.SelectedConversationId, StringComparison.Ordinal));
            if (selected != null && !boundedConversations.Any(item => string.Equals(item.ConversationId, selected.ConversationId, StringComparison.Ordinal)))
            {
                if (boundedConversations.Count == 100) boundedConversations[boundedConversations.Count - 1] = selected;
                else boundedConversations.Add(selected);
            }
            LivingHiveChatConversation[] sourceConversations = boundedConversations.ToArray();
            LivingHiveChatMessage[] confirmed = (snapshot.Messages ?? Array.Empty<LivingHiveChatMessage>()).Where(item => item != null && item.Delivery == LivingHiveChatDelivery.Confirmed && !string.IsNullOrWhiteSpace(item.MessageId) && string.Equals(item.ConversationId, snapshot.SelectedConversationId, StringComparison.Ordinal)).OrderBy(item => item.Sequence).TakeLast(maxMessages).ToArray();
            return new WireCache
            {
                schemaVersion = CurrentSchemaVersion,
                selectedConversationId = snapshot.SelectedConversationId,
                conversations = sourceConversations.Select(item => new WireConversation { conversationId = item.ConversationId, title = item.Title, channelType = item.ChannelType, lastSequence = item.LastSequence, unreadCount = item.UnreadCount, mentionCount = item.MentionCount }).ToArray(),
                messages = confirmed.Select(item => new WireMessage { messageId = item.MessageId, conversationId = item.ConversationId, clientRequestId = item.ClientRequestId, senderPlayerId = item.SenderPlayerId, senderDisplayName = item.SenderDisplayName, body = item.OriginalBody, sequence = item.Sequence, createdAt = item.CreatedAt.ToUniversalTime().ToString("O") }).ToArray()
            };
        }

        [Serializable] private sealed class WireCache { public int schemaVersion; public string selectedConversationId; public WireConversation[] conversations; public WireMessage[] messages; }
        [Serializable] private sealed class WireConversation { public string conversationId; public string title; public string channelType; public long lastSequence; public int unreadCount; public int mentionCount; }
        [Serializable] private sealed class WireMessage { public string messageId; public string conversationId; public string clientRequestId; public string senderPlayerId; public string senderDisplayName; public string body; public long sequence; public string createdAt; }
    }
}
