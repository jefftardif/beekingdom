using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    public enum RemoteChatError { None, Offline, Disabled, Incompatible, Unauthorized, Forbidden, RateLimited, LocalQueueFull, LocalStorageUnavailable, LocalOperationExpired, LocalRequestTooLarge, LocalAccountMismatch, Cancelled, Transport, InvalidResponse }
    public enum RemoteChatConnectionState { Offline, Polling, ConnectingRealtime, Realtime, AuthenticationRequired }

    public sealed class RemoteChatTransportException : Exception
    {
        public RemoteChatError Error { get; }
        public int StatusCode { get; }
        public string ServerCode { get; }
        public int? RetryAfterSeconds { get; }
        public RemoteChatTransportException(RemoteChatError error, string message, int statusCode = 0, string serverCode = null, int? retryAfterSeconds = null, Exception innerException = null) : base(message, innerException)
        { Error = error; StatusCode = statusCode; ServerCode = serverCode; RetryAfterSeconds = retryAfterSeconds; }
    }

    public sealed class ChatSession
    {
        public string PlayerId { get; }
        public string AccessToken { get; }
        public ChatSession(string playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
    }

    public static class ChatSessionSecurity
    {
        public const int MaxPlayerIdCharacters = 256;
        public const int MaxBearerTokenCharacters = 8192;

        public static bool IsValidPlayerId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaxPlayerIdCharacters || !string.Equals(value, value.Trim(), StringComparison.Ordinal)) return false;
            for (int index = 0; index < value.Length; index++) if (char.IsControl(value[index])) return false;
            return true;
        }

        public static bool IsValidBearerToken(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxBearerTokenCharacters) return false;
            bool padding = false;
            bool content = false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (character == '=') { padding = true; continue; }
                bool asciiAlphaNumeric = (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9');
                if (padding || !(asciiAlphaNumeric || character == '-' || character == '.' || character == '_' || character == '~' || character == '+' || character == '/')) return false;
                content = true;
            }
            return content;
        }
    }

    public interface IChatSessionSource { Task<ChatSession> GetSessionAsync(CancellationToken cancellationToken); }
    public interface IRefreshableChatSessionSource : IChatSessionSource { Task<ChatSession> RefreshSessionAsync(CancellationToken cancellationToken); }

    public sealed class ChatTransportRequest
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string BearerToken { get; set; }
        public object Body { get; set; }
        public bool BypassCache { get; set; }
    }

    public sealed class ChatTransportResponse<T>
    {
        public int StatusCode { get; set; }
        public T Body { get; set; }
        public string TransportError { get; set; }
        public string RawBody { get; set; }
        public int? RetryAfterSeconds { get; set; }
        public string CacheControl { get; set; }
        public int? AgeSeconds { get; set; }
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
    }

    public interface IChatRestTransport
    {
        Task<ChatTransportResponse<T>> SendAsync<T>(ChatTransportRequest request, CancellationToken cancellationToken);
    }

    public sealed class RemoteChatProblem { public string Code { get; set; } public string Message { get; set; } public int? RetryAfterSeconds { get; set; } }
    public interface IChatErrorDecoder { RemoteChatProblem Decode(string rawBody); }

    public sealed class RemoteChatMessage
    {
        public string MessageId { get; set; }
        public string ConversationId { get; set; }
        public long Sequence { get; set; }
        public string ClientRequestId { get; set; }
        public string SenderId { get; set; }
        public string SenderDisplayName { get; set; }
        public string ChannelType { get; set; }
        public string OriginalBody { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class RemoteConversation
    {
        public string ConversationId { get; set; }
        public string Title { get; set; }
        public string ChannelType { get; set; }
        public long LastSequence { get; set; }
        public long ReadCursorSequence { get; set; }
        public int UnreadCount { get; set; }
        public int MentionCount { get; set; }
    }

    public sealed class RemoteChatLimits { public int BodyMaxCharacters { get; set; } public int MessagesPerMinutePerPlayer { get; set; } public int MessagesPerTenSecondsPerConversation { get; set; } public int PrivateConversationCreatesPerHour { get; set; } public int MaxPrivateRecipients { get; set; } }
    public sealed class RemoteCapabilities
    {
        public string Provider { get; set; }
        public bool Server { get; set; }
        public bool OfficialGain { get; set; }
        public bool Realtime { get; set; }
        public bool Emojis { get; set; }
        public bool Mentions { get; set; }
        public bool OfflineDelivery { get; set; }
        public bool ReadCursors { get; set; }
        public bool ModerationReports { get; set; }
        public string ProtocolVersion { get; set; }
        public List<string> Channels { get; set; } = new List<string>();
        public RemoteChatLimits Limits { get; set; }
        public int IdempotencyReceiptRetentionDays { get; set; }
        public bool TranslationAvailable { get; set; }
        public string TranslationModelVersion { get; set; }
    }
    public sealed class RemoteCapabilityDecision { public bool IsAvailable { get; set; } public bool UseRealtime { get; set; } public string ReasonCode { get; set; } public RemoteCapabilities Capabilities { get; set; } public int EffectiveReplayMaxAgeDays { get; set; } }
    public sealed class RemoteConversationPage { public List<RemoteConversation> Items { get; set; } = new List<RemoteConversation>(); public string NextCursor { get; set; } }
    public sealed class RemoteMessagePage { public List<RemoteChatMessage> Items { get; set; } = new List<RemoteChatMessage>(); public long? NextAfterSequence { get; set; } }
    public sealed class ChatPaginationPolicy
    {
        public int PageSize { get; }
        public int MaxPages { get; }
        public ChatPaginationPolicy(int pageSize = 50, int maxPages = 20)
        { if (pageSize < 1 || pageSize > 100) throw new ArgumentOutOfRangeException(nameof(pageSize)); if (maxPages < 1 || maxPages > 100) throw new ArgumentOutOfRangeException(nameof(maxPages)); PageSize = pageSize; MaxPages = maxPages; }
    }
    public sealed class RemoteConversationLoadResult { public IReadOnlyList<RemoteConversation> Items { get; set; } public bool IsComplete { get; set; } public int PagesLoaded { get; set; } public string NextCursor { get; set; } }
    public sealed class RemoteReconciliationResult { public IReadOnlyList<RemoteChatMessage> Items { get; set; } public bool IsComplete { get; set; } public int PagesLoaded { get; set; } public long ConfirmedSequence { get; set; } public long? NextAfterSequence { get; set; } }
    public sealed class RemoteSendResult { public RemoteChatMessage Message { get; set; } public bool Deduplicated { get; set; } public long ServerSequence { get; set; } }
    public sealed class RemoteSendMessageRequest
    {
        public string ClientRequestId { get; set; }
        public string Body { get; set; }
        public string ClientCreatedAt { get; set; }
    }
    public sealed class RemoteMarkReadRequest { public long Sequence { get; set; } }
    public sealed class RemoteReportMessageRequest { public string ClientRequestId { get; set; } public string Category { get; set; } }
    public sealed class RemoteCreateConversationRequest
    {
        public string ChannelType { get; set; }
        public string GameServerId { get; set; }
        public string WorldId { get; set; }
        public string AudienceKey { get; set; }
        public string Title { get; set; }
        public List<string> ParticipantIds { get; set; } = new List<string>();
        public string ClientRequestId { get; set; }
    }
    public sealed class RemoteInboxEntry
    {
        public string ConversationId { get; set; }
        public long ReadCursorSequence { get; set; }
        public int UnreadCount { get; set; }
        public int MentionCount { get; set; }
        public bool IsMuted { get; set; }
        public bool IsArchived { get; set; }
    }
    public sealed class RemoteCreateConversationResult { public RemoteConversation Conversation { get; set; } public RemoteInboxEntry Inbox { get; set; } public string ClientRequestId { get; set; } }
    public sealed class RemoteModerationReport { public string ReportId { get; set; } public string MessageId { get; set; } public string ClientRequestId { get; set; } public string Status { get; set; } }

    public sealed class ChatPendingQueueStatus
    {
        public int Sends { get; set; }
        public int Conversations { get; set; }
        public int Reports { get; set; }
        public int Reads { get; set; }
        public int Total => checked(Sends + Conversations + Reports + Reads);
    }

    public sealed class ChatPendingDrainResult
    {
        public ChatPendingQueueStatus Before { get; set; }
        public ChatPendingQueueStatus Remaining { get; set; }
        public int Completed => Before == null || Remaining == null ? 0 : Math.Max(0, Before.Total - Remaining.Total);
        public bool IsComplete => Remaining != null && Remaining.Total == 0;
    }

    public sealed class ChatPendingDrainException : Exception
    {
        public ChatPendingDrainResult Result { get; }
        public ChatPendingDrainException(ChatPendingDrainResult result, Exception innerException) : base("The pending chat queue was only partially drained; remaining entries were preserved.", innerException) { Result = result; }
    }

    public sealed class ChatRetryPolicy
    {
        public int MaxAttempts { get; }
        public TimeSpan Delay { get; }
        public ChatRetryPolicy(int maxAttempts = 3, TimeSpan? delay = null)
        {
            if (maxAttempts < 1 || maxAttempts > 8) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            MaxAttempts = maxAttempts;
            Delay = delay ?? TimeSpan.FromSeconds(2);
            if (Delay < TimeSpan.Zero || Delay > TimeSpan.FromSeconds(30)) throw new ArgumentOutOfRangeException(nameof(delay));
        }
    }

    public interface IChatDelay { Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken); }
    public sealed class SystemChatDelay : IChatDelay { public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken) => Task.Delay(duration, cancellationToken); }

    public sealed class PendingChatSend
    {
        public int SchemaVersion { get; set; } = 1;
        public string ConversationId { get; set; }
        public string Body { get; set; }
        public string ClientRequestId { get; set; }
        public string ClientCreatedAt { get; set; }
        public int AttemptCount { get; set; }
    }

    public interface IChatPendingSendStore
    {
        Task<IReadOnlyList<PendingChatSend>> LoadAsync(CancellationToken cancellationToken);
        Task SaveAsync(PendingChatSend pending, CancellationToken cancellationToken);
        Task RemoveAsync(string clientRequestId, CancellationToken cancellationToken);
    }

    public sealed class PendingChatConversationCreation
    {
        public int SchemaVersion { get; set; } = 1;
        public RemoteCreateConversationRequest Request { get; set; }
        public int AttemptCount { get; set; }
        public string EnqueuedAtUtc { get; set; } = DateTimeOffset.UtcNow.ToString("O");
    }

    public interface IChatPendingConversationStore
    {
        Task<IReadOnlyList<PendingChatConversationCreation>> LoadAsync(CancellationToken cancellationToken);
        Task SaveAsync(PendingChatConversationCreation pending, CancellationToken cancellationToken);
        Task RemoveAsync(string clientRequestId, CancellationToken cancellationToken);
    }

    public sealed class PendingModerationReportRequest
    {
        public int SchemaVersion { get; set; } = 1;
        public string MessageId { get; set; }
        public string Category { get; set; }
        public string ClientRequestId { get; set; }
        public int AttemptCount { get; set; }
        public string EnqueuedAtUtc { get; set; } = DateTimeOffset.UtcNow.ToString("O");
    }

    public interface IChatPendingModerationReportStore
    {
        Task<IReadOnlyList<PendingModerationReportRequest>> LoadAsync(CancellationToken cancellationToken);
        Task SaveAsync(PendingModerationReportRequest pending, CancellationToken cancellationToken);
        Task RemoveAsync(string clientRequestId, CancellationToken cancellationToken);
    }

    public sealed class PendingReadCursor
    {
        public int SchemaVersion { get; set; } = 1;
        public string ConversationId { get; set; }
        public long Sequence { get; set; }
        public int AttemptCount { get; set; }
        public string EnqueuedAtUtc { get; set; } = DateTimeOffset.UtcNow.ToString("O");
    }

    public interface IChatPendingReadStore
    {
        Task<IReadOnlyList<PendingReadCursor>> LoadAsync(CancellationToken cancellationToken);
        Task SaveMaximumAsync(PendingReadCursor pending, CancellationToken cancellationToken);
        Task RemoveThroughAsync(string conversationId, long acknowledgedSequence, CancellationToken cancellationToken);
    }
    public sealed class ChatDiagnosticEvent
    {
        public string Code { get; set; }
        public string Operation { get; set; }
        public int StatusCode { get; set; }
        public RemoteChatError Error { get; set; }
        public string ServerCode { get; set; }
        public int Attempt { get; set; }
        public int Count { get; set; }
    }
    public interface IChatDiagnosticsSink { void Record(ChatDiagnosticEvent diagnosticEvent); }

    public sealed class ChatPendingReplayPolicy
    {
        public TimeSpan MaxAge { get; }
        public TimeSpan AllowedClockSkew { get; }
        public ChatPendingReplayPolicy(TimeSpan? maxAge = null, TimeSpan? allowedClockSkew = null)
        {
            MaxAge = maxAge ?? TimeSpan.FromDays(7);
            AllowedClockSkew = allowedClockSkew ?? TimeSpan.FromMinutes(5);
            if (MaxAge < TimeSpan.FromHours(1) || MaxAge > TimeSpan.FromDays(29)) throw new ArgumentOutOfRangeException(nameof(maxAge));
            if (AllowedClockSkew < TimeSpan.Zero || AllowedClockSkew > TimeSpan.FromHours(1)) throw new ArgumentOutOfRangeException(nameof(allowedClockSkew));
        }
    }
    public sealed class ChatCapabilityLeasePolicy
    {
        public TimeSpan Duration { get; }
        public ChatCapabilityLeasePolicy(TimeSpan? duration = null)
        {
            Duration = duration ?? TimeSpan.FromMinutes(5);
            if (Duration < TimeSpan.FromSeconds(30) || Duration > TimeSpan.FromHours(1)) throw new ArgumentOutOfRangeException(nameof(duration));
        }
    }

    public interface IChatClock { DateTime UtcNow { get; } }
    public sealed class SystemChatClock : IChatClock { public DateTime UtcNow => DateTime.UtcNow; }

    public sealed class RemoteChatEvent
    {
        public string EventId { get; set; }
        public string ConversationId { get; set; }
        public long? Sequence { get; set; }
        public RemoteChatMessage Message { get; set; }
    }

    public interface IChatRealtimeTransport
    {
        bool IsAvailable { get; }
        Task ConnectAsync(ChatSession session, Func<RemoteChatEvent, Task> onEvent, CancellationToken cancellationToken);
        Task JoinConversationAsync(string conversationId, CancellationToken cancellationToken);
        Task LeaveConversationAsync(string conversationId, CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
    }

    public sealed class TranslationRequest
    {
        public string MessageId { get; set; }
        public string TargetLocale { get; set; }
        public string ModelVersion { get; set; }
    }

    public sealed class MessageTranslation
    {
        public string MessageId { get; set; }
        public string SourceLocale { get; set; }
        public string TargetLocale { get; set; }
        public string ModelVersion { get; set; }
        public string TranslatedText { get; set; }
        public string Status { get; set; }
        public string CacheKey => MessageId + "+" + TargetLocale + "+" + ModelVersion;
    }
}
