using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    public interface IChatStringStore
    {
        string Read(string key);
        void Write(string key, string value);
        void Delete(string key);
    }

    public sealed class ChatPendingStoreException : IOException
    {
        public ChatPendingStoreException(string message, Exception innerException = null) : base(message, innerException) { }
    }

    public sealed class ChatPendingJournalFullException : IOException
    {
        public int Capacity { get; }
        public ChatPendingJournalFullException(int capacity) : base("The pending chat journal is full; existing entries were preserved.") { Capacity = capacity; }
    }

    public sealed class ChatPendingJournalSizeException : IOException
    {
        public int MaxCharacters { get; }
        public ChatPendingJournalSizeException(int maxCharacters) : base("The pending chat journal exceeds its serialized size limit; existing data was preserved.") { MaxCharacters = maxCharacters; }
    }

    public sealed class ChatPendingJournalPolicy
    {
        public int MaxEntries { get; }
        public int MaxSerializedCharacters { get; }
        public ChatPendingJournalPolicy(int maxEntries = 256, int maxSerializedCharacters = 1048576)
        {
            if (maxEntries < 1 || maxEntries > 4096) throw new ArgumentOutOfRangeException(nameof(maxEntries));
            if (maxSerializedCharacters < 1024 || maxSerializedCharacters > 8388608) throw new ArgumentOutOfRangeException(nameof(maxSerializedCharacters));
            MaxEntries = maxEntries;
            MaxSerializedCharacters = maxSerializedCharacters;
        }
    }

    public sealed class VersionedChatPendingSendStore : IChatPendingSendStore
    {
        public const int CurrentSchemaVersion = 1;
        private readonly string key;
        private readonly IChatStringStore store;
        private readonly IChatJsonBackend json;
        private readonly ChatPendingJournalPolicy policy;
        private readonly ChatPersistenceGate persistenceGate;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);

        public VersionedChatPendingSendStore(IChatStringStore store, IChatJsonBackend json, string key = "BeeKingdom.Chat.PendingSends.v1", ChatPendingJournalPolicy policy = null, ChatPersistenceGate persistenceGate = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.json = json ?? throw new ArgumentNullException(nameof(json));
            this.key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("Journal key is required.", nameof(key)) : key;
            this.policy = policy ?? new ChatPendingJournalPolicy();
            this.persistenceGate = persistenceGate ?? new ChatPersistenceGate();
        }

        public async Task<IReadOnlyList<PendingChatSend>> LoadAsync(CancellationToken cancellationToken)
        {
            using (await persistenceGate.EnterAsync(cancellationToken))
            { await gate.WaitAsync(cancellationToken); try { return LoadUnsafe(); } finally { gate.Release(); } }
        }

        public async Task SaveAsync(PendingChatSend pending, CancellationToken cancellationToken)
        {
            if (!IsValid(pending)) throw new ArgumentException("Pending send is incomplete or invalid.", nameof(pending));
            using (await persistenceGate.EnterAsync(cancellationToken))
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    List<PendingChatSend> items = LoadUnsafe().ToList();
                    int index = items.FindIndex(item => string.Equals(item.ClientRequestId, pending.ClientRequestId, StringComparison.Ordinal));
                    if (index >= 0) items[index] = pending;
                    else { EnsureCapacity(items.Count); items.Add(pending); }
                    WriteUnsafe(items);
                }
                finally { gate.Release(); }
            }
        }

        public async Task RemoveAsync(string clientRequestId, CancellationToken cancellationToken)
        {
            using (await persistenceGate.EnterAsync(cancellationToken))
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    List<PendingChatSend> items = LoadUnsafe().Where(item => !string.Equals(item.ClientRequestId, clientRequestId, StringComparison.Ordinal)).ToList();
                    if (items.Count == 0) store.Delete(key); else WriteUnsafe(items);
                }
                finally { gate.Release(); }
            }
        }

        private IReadOnlyList<PendingChatSend> LoadUnsafe()
        {
            string value = store.Read(key);
            if (string.IsNullOrWhiteSpace(value)) return new List<PendingChatSend>();
            try
            {
                EnsureSerializedSize(value);
                WireJournal journal = json.FromJson<WireJournal>(value);
                if (journal == null || journal.schemaVersion != CurrentSchemaVersion || journal.items == null) throw new InvalidDataException("Unsupported or incomplete pending-send journal.");
                if (journal.items.Length > policy.MaxEntries) throw new InvalidDataException("Pending-send journal exceeds its configured capacity.");
                var identities = new HashSet<string>(StringComparer.Ordinal);
                foreach (WirePending item in journal.items)
                {
                    if (item == null || item.schemaVersion != CurrentSchemaVersion || string.IsNullOrWhiteSpace(item.conversationId) || item.body == null || string.IsNullOrWhiteSpace(item.clientRequestId) || item.attemptCount < 0 || !DateTimeOffset.TryParse(item.clientCreatedAt, out _)) throw new InvalidDataException("Pending-send journal contains an invalid entry.");
                    if (!identities.Add(item.clientRequestId)) throw new InvalidDataException("Pending-send journal contains duplicate identities.");
                }
                return journal.items.Select(Map).ToList();
            }
            catch (Exception exception) when (!(exception is ChatPendingStoreException))
            {
                throw new ChatPendingStoreException("The pending-send journal is corrupted or unsupported; it was preserved for recovery.", exception);
            }
        }

        private void WriteUnsafe(IEnumerable<PendingChatSend> items)
        {
            var journal = new WireJournal { schemaVersion = CurrentSchemaVersion, items = items.Select(Map).ToArray() };
            string value = json.ToJson(journal); EnsureSerializedSize(value); store.Write(key, value);
        }

        private void EnsureCapacity(int count) { if (count >= policy.MaxEntries) throw new ChatPendingJournalFullException(policy.MaxEntries); }
        private void EnsureSerializedSize(string value) { if (value != null && value.Length > policy.MaxSerializedCharacters) throw new ChatPendingJournalSizeException(policy.MaxSerializedCharacters); }
        private static bool IsValid(PendingChatSend item) => item != null && item.SchemaVersion == CurrentSchemaVersion && !string.IsNullOrWhiteSpace(item.ConversationId) && item.Body != null && !string.IsNullOrWhiteSpace(item.ClientRequestId) && item.AttemptCount >= 0 && DateTimeOffset.TryParse(item.ClientCreatedAt, out _);

        private static PendingChatSend Map(WirePending item) => new PendingChatSend { SchemaVersion = item.schemaVersion, ConversationId = item.conversationId, Body = item.body, ClientRequestId = item.clientRequestId, ClientCreatedAt = item.clientCreatedAt, AttemptCount = item.attemptCount };
        private static WirePending Map(PendingChatSend item) => new WirePending { schemaVersion = item.SchemaVersion, conversationId = item.ConversationId, body = item.Body, clientRequestId = item.ClientRequestId, clientCreatedAt = item.ClientCreatedAt, attemptCount = item.AttemptCount };

#pragma warning disable 0649
        [Serializable] private sealed class WireJournal { public int schemaVersion; public WirePending[] items; }
        [Serializable] private sealed class WirePending { public int schemaVersion; public string conversationId; public string body; public string clientRequestId; public string clientCreatedAt; public int attemptCount; }
#pragma warning restore 0649
    }

    public sealed class VersionedChatPendingConversationStore : IChatPendingConversationStore
    {
        public const int CurrentSchemaVersion = 1;
        private readonly string key;
        private readonly IChatStringStore store;
        private readonly IChatJsonBackend json;
        private readonly ChatPendingJournalPolicy policy;
        private readonly ChatPersistenceGate persistenceGate;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);

        public VersionedChatPendingConversationStore(IChatStringStore store, IChatJsonBackend json, string key = "BeeKingdom.Chat.PendingConversations.v1", ChatPendingJournalPolicy policy = null, ChatPersistenceGate persistenceGate = null)
        { this.store = store ?? throw new ArgumentNullException(nameof(store)); this.json = json ?? throw new ArgumentNullException(nameof(json)); this.key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("Journal key is required.", nameof(key)) : key; this.policy = policy ?? new ChatPendingJournalPolicy(); this.persistenceGate = persistenceGate ?? new ChatPersistenceGate(); }

        public async Task<IReadOnlyList<PendingChatConversationCreation>> LoadAsync(CancellationToken ct)
        { using (await persistenceGate.EnterAsync(ct)) { await gate.WaitAsync(ct); try { return LoadUnsafe(); } finally { gate.Release(); } } }

        public async Task SaveAsync(PendingChatConversationCreation pending, CancellationToken ct)
        {
            if (!IsValid(pending)) throw new ArgumentException("Pending conversation is incomplete or invalid.", nameof(pending));
            using (await persistenceGate.EnterAsync(ct))
            {
                await gate.WaitAsync(ct);
                try { List<PendingChatConversationCreation> items = LoadUnsafe().ToList(); int index = items.FindIndex(item => string.Equals(item.Request.ClientRequestId, pending.Request.ClientRequestId, StringComparison.Ordinal)); if (index >= 0) items[index] = pending; else { EnsureCapacity(items.Count); items.Add(pending); } WriteUnsafe(items); }
                finally { gate.Release(); }
            }
        }

        public async Task RemoveAsync(string id, CancellationToken ct)
        {
            using (await persistenceGate.EnterAsync(ct))
            {
                await gate.WaitAsync(ct);
                try { List<PendingChatConversationCreation> items = LoadUnsafe().Where(item => !string.Equals(item.Request.ClientRequestId, id, StringComparison.Ordinal)).ToList(); if (items.Count == 0) store.Delete(key); else WriteUnsafe(items); }
                finally { gate.Release(); }
            }
        }

        private IReadOnlyList<PendingChatConversationCreation> LoadUnsafe()
        {
            string value = store.Read(key);
            if (string.IsNullOrWhiteSpace(value)) return new List<PendingChatConversationCreation>();
            try
            {
                EnsureSerializedSize(value);
                WireConversationJournal journal = json.FromJson<WireConversationJournal>(value);
                if (journal == null || journal.schemaVersion != CurrentSchemaVersion || journal.items == null) throw new InvalidDataException("Unsupported pending-conversation journal.");
                if (journal.items.Length > policy.MaxEntries) throw new InvalidDataException("Pending-conversation journal exceeds its configured capacity.");
                var identities = new HashSet<string>(StringComparer.Ordinal);
                foreach (WireConversationPending item in journal.items)
                {
                    if (item == null || item.schemaVersion != CurrentSchemaVersion || string.IsNullOrWhiteSpace(item.clientRequestId) || item.attemptCount < 0 || !DateTimeOffset.TryParse(item.enqueuedAtUtc, out _)) throw new InvalidDataException("Pending-conversation journal contains an invalid entry.");
                    if (!identities.Add(item.clientRequestId)) throw new InvalidDataException("Pending-conversation journal contains duplicate identities.");
                    if ((item.participantIds ?? Array.Empty<string>()).Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Pending-conversation journal contains an invalid participant.");
                }
                return journal.items.Select(item => new PendingChatConversationCreation { SchemaVersion = item.schemaVersion, AttemptCount = item.attemptCount, EnqueuedAtUtc = item.enqueuedAtUtc, Request = new RemoteCreateConversationRequest { ChannelType = item.channelType, GameServerId = item.gameServerId, WorldId = item.worldId, AudienceKey = item.audienceKey, Title = item.title, ClientRequestId = item.clientRequestId, ParticipantIds = (item.participantIds ?? Array.Empty<string>()).ToList() } }).ToList();
            }
            catch (Exception exception) { throw new ChatPendingStoreException("The pending-conversation journal is corrupted or unsupported; it was preserved for recovery.", exception); }
        }

        private void WriteUnsafe(IEnumerable<PendingChatConversationCreation> items)
        {
            var journal = new WireConversationJournal { schemaVersion = CurrentSchemaVersion, items = items.Select(item => new WireConversationPending { schemaVersion = item.SchemaVersion, attemptCount = item.AttemptCount, enqueuedAtUtc = item.EnqueuedAtUtc, channelType = item.Request.ChannelType, gameServerId = item.Request.GameServerId, worldId = item.Request.WorldId, audienceKey = item.Request.AudienceKey, title = item.Request.Title, clientRequestId = item.Request.ClientRequestId, participantIds = item.Request.ParticipantIds.ToArray() }).ToArray() };
            string value = json.ToJson(journal); EnsureSerializedSize(value); store.Write(key, value);
        }
        private void EnsureCapacity(int count) { if (count >= policy.MaxEntries) throw new ChatPendingJournalFullException(policy.MaxEntries); }
        private void EnsureSerializedSize(string value) { if (value != null && value.Length > policy.MaxSerializedCharacters) throw new ChatPendingJournalSizeException(policy.MaxSerializedCharacters); }
        private static bool IsValid(PendingChatConversationCreation item) => item?.Request != null && item.SchemaVersion == CurrentSchemaVersion && !string.IsNullOrWhiteSpace(item.Request.ClientRequestId) && item.AttemptCount >= 0 && DateTimeOffset.TryParse(item.EnqueuedAtUtc, out _) && (item.Request.ParticipantIds ?? new List<string>()).All(value => !string.IsNullOrWhiteSpace(value));

#pragma warning disable 0649
        [Serializable] private sealed class WireConversationJournal { public int schemaVersion; public WireConversationPending[] items; }
        [Serializable] private sealed class WireConversationPending { public int schemaVersion; public int attemptCount; public string enqueuedAtUtc; public string channelType; public string gameServerId; public string worldId; public string audienceKey; public string title; public string clientRequestId; public string[] participantIds; }
#pragma warning restore 0649
    }

    public sealed class VersionedChatPendingModerationReportStore : IChatPendingModerationReportStore
    {
        public const int CurrentSchemaVersion = 1;
        private readonly string key;
        private readonly IChatStringStore store;
        private readonly IChatJsonBackend json;
        private readonly ChatPendingJournalPolicy policy;
        private readonly ChatPersistenceGate persistenceGate;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        public VersionedChatPendingModerationReportStore(IChatStringStore store, IChatJsonBackend json, string key = "BeeKingdom.Chat.PendingReports.v1", ChatPendingJournalPolicy policy = null, ChatPersistenceGate persistenceGate = null)
        { this.store = store ?? throw new ArgumentNullException(nameof(store)); this.json = json ?? throw new ArgumentNullException(nameof(json)); this.key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("Journal key is required.", nameof(key)) : key; this.policy = policy ?? new ChatPendingJournalPolicy(); this.persistenceGate = persistenceGate ?? new ChatPersistenceGate(); }
        public async Task<IReadOnlyList<PendingModerationReportRequest>> LoadAsync(CancellationToken ct) { using (await persistenceGate.EnterAsync(ct)) { await gate.WaitAsync(ct); try { return LoadUnsafe(); } finally { gate.Release(); } } }
        public async Task SaveAsync(PendingModerationReportRequest pending, CancellationToken ct)
        {
            if (!IsValid(pending)) throw new ArgumentException("Pending report is incomplete or invalid.", nameof(pending));
            using (await persistenceGate.EnterAsync(ct)) { await gate.WaitAsync(ct); try { List<PendingModerationReportRequest> items = LoadUnsafe().ToList(); int index = items.FindIndex(item => item.ClientRequestId == pending.ClientRequestId); if (index >= 0) items[index] = pending; else { EnsureCapacity(items.Count); items.Add(pending); } WriteUnsafe(items); } finally { gate.Release(); } }
        }
        public async Task RemoveAsync(string id, CancellationToken ct)
        {
            using (await persistenceGate.EnterAsync(ct)) { await gate.WaitAsync(ct); try { List<PendingModerationReportRequest> items = LoadUnsafe().Where(item => item.ClientRequestId != id).ToList(); if (items.Count == 0) store.Delete(key); else WriteUnsafe(items); } finally { gate.Release(); } }
        }
        private IReadOnlyList<PendingModerationReportRequest> LoadUnsafe()
        {
            string value = store.Read(key); if (string.IsNullOrWhiteSpace(value)) return new List<PendingModerationReportRequest>();
            try { EnsureSerializedSize(value); WireReportJournal journal = json.FromJson<WireReportJournal>(value); if (journal == null || journal.schemaVersion != CurrentSchemaVersion || journal.items == null) throw new InvalidDataException("Unsupported pending-report journal."); if (journal.items.Length > policy.MaxEntries) throw new InvalidDataException("Pending-report journal exceeds its configured capacity."); var identities = new HashSet<string>(StringComparer.Ordinal); foreach (WireReportPending item in journal.items) { if (item == null || item.schemaVersion != CurrentSchemaVersion || string.IsNullOrWhiteSpace(item.messageId) || string.IsNullOrWhiteSpace(item.category) || string.IsNullOrWhiteSpace(item.clientRequestId) || item.attemptCount < 0 || !DateTimeOffset.TryParse(item.enqueuedAtUtc, out _)) throw new InvalidDataException("Pending-report journal contains an invalid entry."); if (!identities.Add(item.clientRequestId)) throw new InvalidDataException("Pending-report journal contains duplicate identities."); } return journal.items.Select(item => new PendingModerationReportRequest { SchemaVersion = item.schemaVersion, MessageId = item.messageId, Category = item.category, ClientRequestId = item.clientRequestId, AttemptCount = item.attemptCount, EnqueuedAtUtc = item.enqueuedAtUtc }).ToList(); }
            catch (Exception exception) { throw new ChatPendingStoreException("The pending-report journal is corrupted or unsupported; it was preserved for recovery.", exception); }
        }
        private void WriteUnsafe(IEnumerable<PendingModerationReportRequest> items) { string value = json.ToJson(new WireReportJournal { schemaVersion = CurrentSchemaVersion, items = items.Select(item => new WireReportPending { schemaVersion = item.SchemaVersion, messageId = item.MessageId, category = item.Category, clientRequestId = item.ClientRequestId, attemptCount = item.AttemptCount, enqueuedAtUtc = item.EnqueuedAtUtc }).ToArray() }); EnsureSerializedSize(value); store.Write(key, value); }
        private void EnsureCapacity(int count) { if (count >= policy.MaxEntries) throw new ChatPendingJournalFullException(policy.MaxEntries); }
        private void EnsureSerializedSize(string value) { if (value != null && value.Length > policy.MaxSerializedCharacters) throw new ChatPendingJournalSizeException(policy.MaxSerializedCharacters); }
        private static bool IsValid(PendingModerationReportRequest item) => item != null && item.SchemaVersion == CurrentSchemaVersion && !string.IsNullOrWhiteSpace(item.MessageId) && !string.IsNullOrWhiteSpace(item.Category) && !string.IsNullOrWhiteSpace(item.ClientRequestId) && item.AttemptCount >= 0 && DateTimeOffset.TryParse(item.EnqueuedAtUtc, out _);
#pragma warning disable 0649
        [Serializable] private sealed class WireReportJournal { public int schemaVersion; public WireReportPending[] items; }
        [Serializable] private sealed class WireReportPending { public int schemaVersion; public string messageId; public string category; public string clientRequestId; public int attemptCount; public string enqueuedAtUtc; }
#pragma warning restore 0649
    }

    public sealed class VersionedChatPendingReadStore : IChatPendingReadStore
    {
        public const int CurrentSchemaVersion = 1;
        private readonly string key; private readonly IChatStringStore store; private readonly IChatJsonBackend json; private readonly ChatPendingJournalPolicy policy; private readonly ChatPersistenceGate persistenceGate; private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        public VersionedChatPendingReadStore(IChatStringStore store, IChatJsonBackend json, string key = "BeeKingdom.Chat.PendingReads.v1", ChatPendingJournalPolicy policy = null, ChatPersistenceGate persistenceGate = null) { this.store = store ?? throw new ArgumentNullException(nameof(store)); this.json = json ?? throw new ArgumentNullException(nameof(json)); this.key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("Journal key is required.", nameof(key)) : key; this.policy = policy ?? new ChatPendingJournalPolicy(); this.persistenceGate = persistenceGate ?? new ChatPersistenceGate(); }
        public async Task<IReadOnlyList<PendingReadCursor>> LoadAsync(CancellationToken ct) { using (await persistenceGate.EnterAsync(ct)) { await gate.WaitAsync(ct); try { return LoadUnsafe(); } finally { gate.Release(); } } }
        public async Task SaveMaximumAsync(PendingReadCursor pending, CancellationToken ct)
        {
            if (!IsValid(pending)) throw new ArgumentException("Pending read cursor is invalid.", nameof(pending));
            using (await persistenceGate.EnterAsync(ct)) { await gate.WaitAsync(ct); try { List<PendingReadCursor> items = LoadUnsafe().ToList(); int index = items.FindIndex(item => item.ConversationId == pending.ConversationId); if (index < 0) { EnsureCapacity(items.Count); items.Add(pending); } else if (pending.Sequence > items[index].Sequence) items[index] = pending; else if (pending.Sequence == items[index].Sequence) items[index].AttemptCount = Math.Max(items[index].AttemptCount, pending.AttemptCount); WriteUnsafe(items); } finally { gate.Release(); } }
        }
        public async Task RemoveThroughAsync(string id, long sequence, CancellationToken ct)
        {
            using (await persistenceGate.EnterAsync(ct)) { await gate.WaitAsync(ct); try { List<PendingReadCursor> items = LoadUnsafe().ToList(); items.RemoveAll(item => item.ConversationId == id && item.Sequence <= sequence); if (items.Count == 0) store.Delete(key); else WriteUnsafe(items); } finally { gate.Release(); } }
        }
        private IReadOnlyList<PendingReadCursor> LoadUnsafe()
        {
            string value = store.Read(key); if (string.IsNullOrWhiteSpace(value)) return new List<PendingReadCursor>();
            try { EnsureSerializedSize(value); WireReadJournal journal = json.FromJson<WireReadJournal>(value); if (journal == null || journal.schemaVersion != CurrentSchemaVersion || journal.items == null) throw new InvalidDataException("Unsupported pending-read journal."); if (journal.items.Length > policy.MaxEntries) throw new InvalidDataException("Pending-read journal exceeds its configured capacity."); var identities = new HashSet<string>(StringComparer.Ordinal); foreach (WireReadPending item in journal.items) { if (item == null || item.schemaVersion != CurrentSchemaVersion || string.IsNullOrWhiteSpace(item.conversationId) || item.sequence < 0 || item.attemptCount < 0 || !DateTimeOffset.TryParse(item.enqueuedAtUtc, out _)) throw new InvalidDataException("Pending-read journal contains an invalid entry."); if (!identities.Add(item.conversationId)) throw new InvalidDataException("Pending-read journal contains duplicate conversations."); } return journal.items.Select(item => new PendingReadCursor { SchemaVersion = item.schemaVersion, ConversationId = item.conversationId, Sequence = item.sequence, AttemptCount = item.attemptCount, EnqueuedAtUtc = item.enqueuedAtUtc }).ToList(); }
            catch (Exception exception) { throw new ChatPendingStoreException("The pending-read journal is corrupted or unsupported; it was preserved for recovery.", exception); }
        }
        private void WriteUnsafe(IEnumerable<PendingReadCursor> items) { string value = json.ToJson(new WireReadJournal { schemaVersion = CurrentSchemaVersion, items = items.Select(item => new WireReadPending { schemaVersion = item.SchemaVersion, conversationId = item.ConversationId, sequence = item.Sequence, attemptCount = item.AttemptCount, enqueuedAtUtc = item.EnqueuedAtUtc }).ToArray() }); EnsureSerializedSize(value); store.Write(key, value); }
        private void EnsureCapacity(int count) { if (count >= policy.MaxEntries) throw new ChatPendingJournalFullException(policy.MaxEntries); }
        private void EnsureSerializedSize(string value) { if (value != null && value.Length > policy.MaxSerializedCharacters) throw new ChatPendingJournalSizeException(policy.MaxSerializedCharacters); }
        private static bool IsValid(PendingReadCursor item) => item != null && item.SchemaVersion == CurrentSchemaVersion && !string.IsNullOrWhiteSpace(item.ConversationId) && item.Sequence >= 0 && item.AttemptCount >= 0 && DateTimeOffset.TryParse(item.EnqueuedAtUtc, out _);
#pragma warning disable 0649
        [Serializable] private sealed class WireReadJournal { public int schemaVersion; public WireReadPending[] items; }
        [Serializable] private sealed class WireReadPending { public int schemaVersion; public string conversationId; public long sequence; public int attemptCount; public string enqueuedAtUtc; }
#pragma warning restore 0649
    }
}
