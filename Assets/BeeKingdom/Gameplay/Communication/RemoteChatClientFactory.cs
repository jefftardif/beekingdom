using System;
using System.Security.Cryptography;
using System.Text;

namespace BeeKingdom.Gameplay.Communication
{
    public static class ChatEndpointUrl
    {
        private const string ApiRoot = "/chat/v1";

        public static string NormalizeBaseUrl(string value, bool allowInsecureLoopback = false)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri)) throw new ArgumentException("An absolute chat server URL is required.", nameof(value));
            bool secure = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            bool loopback = uri.IsLoopback && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
            if (!secure && !(allowInsecureLoopback && loopback)) throw new ArgumentException("Chat transport requires HTTPS; HTTP is allowed only for explicitly enabled loopback development.", nameof(value));
            if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new ArgumentException("Chat server URL cannot contain credentials, a query, or a fragment.", nameof(value));

            string path = uri.AbsolutePath.TrimEnd('/');
            if (path.Length != 0 && !string.Equals(path, ApiRoot, StringComparison.Ordinal))
                throw new ArgumentException("Chat server URL path must be empty or exactly /chat/v1.", nameof(value));
            return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + path;
        }

        public static string Compose(string baseUrl, string requestPath)
        {
            string normalized = NormalizeBaseUrl(baseUrl, allowInsecureLoopback: true);
            if (string.IsNullOrEmpty(requestPath) || !requestPath.StartsWith(ApiRoot, StringComparison.Ordinal) ||
                (requestPath.Length > ApiRoot.Length && requestPath[ApiRoot.Length] != '/' && requestPath[ApiRoot.Length] != '?'))
                throw new ArgumentException("Chat request path must begin with the canonical /chat/v1 route.", nameof(requestPath));

            bool baseIncludesApiRoot = new Uri(normalized).AbsolutePath.TrimEnd('/').Equals(ApiRoot, StringComparison.Ordinal);
            return normalized + (baseIncludesApiRoot ? requestPath.Substring(ApiRoot.Length) : requestPath);
        }
    }

    public sealed class RemoteChatClientOptions
    {
        public string BaseUrl { get; set; }
        public bool AllowInsecureLoopback { get; set; }
        public string StoragePrefix { get; set; } = "BeeKingdom.Chat";
        public string StoragePartitionId { get; set; }
        public int RetryMaxAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
        public int MaxRecoveryCycles { get; set; } = 3;
        public int MaxPendingEntriesPerJournal { get; set; } = 256;
        public int MaxPendingSerializedCharactersPerJournal { get; set; } = 1048576;
        public TimeSpan PendingReplayMaxAge { get; set; } = TimeSpan.FromDays(7);
        public TimeSpan CapabilityLeaseDuration { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxResponseBytes { get; set; } = ChatHttpResponsePolicy.DefaultMaxBytes;
        public int MaxRequestBytes { get; set; } = ChatHttpRequestPolicy.DefaultMaxBytes;
        public int MaxRequestTargetBytes { get; set; } = ChatHttpRequestTargetPolicy.DefaultMaxBytes;
        public int MaxRecentCachedMessages { get; set; } = 100;
        public int MaxRecentCacheSerializedCharacters { get; set; } = 524288;
    }

    public sealed class RemoteChatClientComponents
    {
        public ServerChatProvider Provider { get; }
        public ChatConversationSynchronizer Synchronizer { get; }
        public ChatPendingPartitionRecovery PendingRecovery { get; }
        public IChatRecentCache RecentCache { get; }
        internal RemoteChatClientComponents(ServerChatProvider provider, ChatConversationSynchronizer synchronizer, ChatPendingPartitionRecovery pendingRecovery, IChatRecentCache recentCache) { Provider = provider; Synchronizer = synchronizer; PendingRecovery = pendingRecovery; RecentCache = recentCache; }
    }

    public static class RemoteChatClientFactory
    {
        public static RemoteChatClientComponents Create(RemoteChatClientOptions options, IChatSessionSource sessions, IChatStringStore storage, IChatDataProtector protector, IChatRealtimeTransport realtime = null, IChatDiagnosticsSink diagnostics = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (sessions == null) throw new ArgumentNullException(nameof(sessions));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (protector == null) throw new ArgumentNullException(nameof(protector));
            string normalizedBaseUrl = ChatEndpointUrl.NormalizeBaseUrl(options.BaseUrl, options.AllowInsecureLoopback);
            if (string.IsNullOrWhiteSpace(options.StoragePrefix)) throw new ArgumentException("A storage prefix is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.StoragePartitionId)) throw new ArgumentException("A stable player storage partition is required.", nameof(options));

            var backend = new UnityJsonBackend();
            var codec = new UnityChatJsonCodec(backend);
            var delay = new SystemChatDelay();
            var journalPolicy = new ChatPendingJournalPolicy(options.MaxPendingEntriesPerJournal, options.MaxPendingSerializedCharactersPerJournal);
            var persistenceGate = new ChatPersistenceGate();
            string prefix = ChatStoragePartition.KeyPrefix(options.StoragePrefix, options.StoragePartitionId);
            var protectedStore = new ProtectedChatStringStore(storage, protector, prefix + ".Protection.v1");
            var provider = new ServerChatProvider(
                new UnityWebRequestChatRestTransport(normalizedBaseUrl, codec, options.RequestTimeout, options.MaxResponseBytes, options.MaxRequestBytes, options.MaxRequestTargetBytes), sessions, realtime,
                new ChatRetryPolicy(options.RetryMaxAttempts, options.RetryDelay), delay,
                new VersionedChatPendingSendStore(protectedStore, backend, prefix + ".PendingSends.v1", journalPolicy, persistenceGate), codec,
                new VersionedChatPendingConversationStore(protectedStore, backend, prefix + ".PendingConversations.v1", journalPolicy, persistenceGate),
                new VersionedChatPendingModerationReportStore(protectedStore, backend, prefix + ".PendingReports.v1", journalPolicy, persistenceGate),
                new VersionedChatPendingReadStore(protectedStore, backend, prefix + ".PendingReads.v1", journalPolicy, persistenceGate), diagnostics,
                replayPolicy: new ChatPendingReplayPolicy(options.PendingReplayMaxAge), requireCapabilityNegotiation: true,
                capabilityLeasePolicy: new ChatCapabilityLeasePolicy(options.CapabilityLeaseDuration), expectedPlayerId: options.StoragePartitionId.Trim());
            var synchronizer = new ChatConversationSynchronizer(provider, delay, new ChatSynchronizationPolicy(options.PollInterval, options.MaxRecoveryCycles));
            var recentCache = new VersionedChatRecentCache(storage, protectedStore, backend, prefix + ".Recent.v1", options.MaxRecentCachedMessages, options.MaxRecentCacheSerializedCharacters);
            return new RemoteChatClientComponents(provider, synchronizer, new ChatPendingPartitionRecovery(storage, prefix, persistenceGate), recentCache);
        }

    }

    public static class ChatStoragePartition
    {
        public static string KeyPrefix(string storagePrefix, string partitionId)
        {
            if (string.IsNullOrWhiteSpace(storagePrefix)) throw new ArgumentException("A storage prefix is required.", nameof(storagePrefix));
            if (string.IsNullOrWhiteSpace(partitionId)) throw new ArgumentException("A stable player storage partition is required.", nameof(partitionId));
            using (SHA256 sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes("BeeKingdom.Chat.StoragePartition.v1\n" + partitionId.Trim()));
                var result = new StringBuilder(32);
                for (int index = 0; index < 16; index++) result.Append(digest[index].ToString("x2"));
                return storagePrefix.Trim() + ".Player." + result;
            }
        }
    }
}
