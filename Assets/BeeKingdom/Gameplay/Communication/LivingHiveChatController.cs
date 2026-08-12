using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    public enum LivingHiveChatStatus { NotConfigured, Connecting, Online, Polling, Offline, AuthenticationRequired, Unavailable, Error }
    public enum LivingHiveChatDelivery { Confirmed, Queued, Failed }

    public sealed class LivingHiveChatConversation
    {
        public string ConversationId { get; set; }
        public string Title { get; set; }
        public string ChannelType { get; set; }
        public long LastSequence { get; set; }
        public int UnreadCount { get; set; }
        public int MentionCount { get; set; }
    }

    public sealed class LivingHiveChatMessage
    {
        public string MessageId { get; set; }
        public string ConversationId { get; set; }
        public string ClientRequestId { get; set; }
        public string SenderPlayerId { get; set; }
        public string SenderDisplayName { get; set; }
        public string OriginalBody { get; set; }
        public string VisibleBody { get; set; }
        public string SourceLocale { get; set; }
        public string TargetLocale { get; set; }
        public long Sequence { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public LivingHiveChatDelivery Delivery { get; set; }
        public bool IsTranslated { get; set; }
    }

    public sealed class LivingHiveChatSnapshot
    {
        public LivingHiveChatStatus Status { get; set; }
        public string ErrorCode { get; set; }
        public string SelectedConversationId { get; set; }
        public IReadOnlyList<LivingHiveChatConversation> Conversations { get; set; } = Array.Empty<LivingHiveChatConversation>();
        public IReadOnlyList<LivingHiveChatMessage> Messages { get; set; } = Array.Empty<LivingHiveChatMessage>();
        public int PendingCount { get; set; }
        public bool TranslationAvailable { get; set; }
        public string TranslationModelVersion { get; set; }
        public int TotalUnread => Conversations.Sum(value => Math.Max(0, value.UnreadCount));
        public LivingHiveChatMessage LastMessage => Messages.LastOrDefault();
    }

    public sealed class LivingHiveChatController
    {
        public const int DefaultRecentMessageLimit = 100;
        private readonly ServerChatProvider provider;
        private readonly ChatTranslationController translations;
        private readonly int recentMessageLimit;
        private readonly IChatDelay delay;
        private readonly TimeSpan pollInterval;
        private readonly IChatRecentCache recentCache;
        private readonly object gate = new object();
        private readonly List<LivingHiveChatConversation> conversations = new List<LivingHiveChatConversation>();
        private readonly List<LivingHiveChatMessage> messages = new List<LivingHiveChatMessage>();
        private LivingHiveChatStatus status = LivingHiveChatStatus.Offline;
        private string errorCode;
        private string selectedConversationId;
        private int pendingCount;
        private CancellationTokenSource liveUpdates;
        private Task pollingTask = Task.CompletedTask;
        private Task realtimeReceiptTask = Task.CompletedTask;

        public LivingHiveChatController(ServerChatProvider provider, int recentMessageLimit = DefaultRecentMessageLimit, IChatDelay delay = null, TimeSpan? pollInterval = null, IChatRecentCache recentCache = null)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            if (recentMessageLimit < 20 || recentMessageLimit > 500) throw new ArgumentOutOfRangeException(nameof(recentMessageLimit));
            this.recentMessageLimit = recentMessageLimit;
            this.delay = delay ?? new SystemChatDelay();
            this.pollInterval = pollInterval ?? TimeSpan.FromSeconds(5);
            this.recentCache = recentCache;
            if (this.pollInterval < TimeSpan.FromSeconds(1) || this.pollInterval > TimeSpan.FromMinutes(1)) throw new ArgumentOutOfRangeException(nameof(pollInterval));
            translations = new ChatTranslationController(provider);
            provider.RealtimeEventApplied += OnRealtimeEventApplied;
        }

        public LivingHiveChatSnapshot Snapshot()
        {
            lock (gate)
            {
                RemoteCapabilities capabilities = provider.NegotiatedCapabilities;
                return new LivingHiveChatSnapshot
                {
                    Status = status,
                    ErrorCode = errorCode,
                    SelectedConversationId = selectedConversationId,
                    PendingCount = pendingCount,
                    Conversations = conversations.Select(Clone).ToArray(),
                    Messages = messages.Select(Clone).ToArray(),
                    TranslationAvailable = capabilities != null && capabilities.TranslationAvailable,
                    TranslationModelVersion = capabilities?.TranslationModelVersion
                };
            }
        }

        public async Task OpenAsync(CancellationToken ct)
        {
            RestoreRecentCache();
            SetStatus(LivingHiveChatStatus.Connecting, null);
            try
            {
                bool alreadyConnected = provider.ConnectionState == RemoteChatConnectionState.Realtime
                    || provider.ConnectionState == RemoteChatConnectionState.Polling;
                if (!alreadyConnected) await provider.ConnectAsync(ct);
                RemoteConversationLoadResult loaded = await provider.LoadAllConversationsAsync(new ChatPaginationPolicy(50, 10), ct);
                IReadOnlyList<RemoteConversation> accessible = loaded?.Items ?? Array.Empty<RemoteConversation>();
                lock (gate)
                {
                    conversations.Clear();
                    conversations.AddRange(accessible.Where(value => value != null && !string.IsNullOrWhiteSpace(value.ConversationId)).Select(MapConversation));
                    if (!conversations.Any(value => string.Equals(value.ConversationId, selectedConversationId, StringComparison.Ordinal)))
                        selectedConversationId = conversations.FirstOrDefault()?.ConversationId;
                }
                await provider.EnsureRealtimeSubscriptionsAsync(accessible.Select(value => value.ConversationId), ct);
                await RefreshSelectedAsync(ct);
                SetStatus(provider.ConnectionState == RemoteChatConnectionState.Realtime ? LivingHiveChatStatus.Online : LivingHiveChatStatus.Polling, null);
                EnsureLiveUpdates(ct, provider.ConnectionState == RemoteChatConnectionState.Polling);
            }
            catch (RemoteChatTransportException exception)
            {
                SetStatus(MapStatus(exception.Error), exception.ServerCode ?? exception.Error.ToString());
            }
        }

        public async Task SelectConversationAsync(string conversationId, CancellationToken ct)
        {
            lock (gate)
            {
                if (!conversations.Any(value => string.Equals(value.ConversationId, conversationId, StringComparison.Ordinal))) throw new ArgumentException("Conversation is not accessible in the current server snapshot.", nameof(conversationId));
                selectedConversationId = conversationId;
                messages.Clear();
            }
            await RefreshSelectedAsync(ct);
        }

        public async Task RefreshSelectedAsync(CancellationToken ct)
        {
            string conversationId;
            lock (gate) conversationId = selectedConversationId;
            if (string.IsNullOrWhiteSpace(conversationId)) { lock (gate) messages.Clear(); return; }
            try
            {
                long afterSequence = provider.GetConfirmedSequence(conversationId);
                RemoteReconciliationResult result = await provider.ReconcileFullyAsync(conversationId, afterSequence, new ChatPaginationPolicy(100, 20), ct);
                IReadOnlyList<RemoteChatMessage> source = result?.Items ?? Array.Empty<RemoteChatMessage>();
                long latest = source.Count == 0 ? 0 : source.Max(value => value.Sequence);
                lock (gate)
                {
                    messages.Clear();
                    messages.AddRange(source.OrderBy(value => value.Sequence).TakeLast(recentMessageLimit).Select(MapMessage));
                }
                if (latest > 0)
                {
                    await provider.MarkReadAsync(conversationId, latest, ct);
                    lock (gate)
                    {
                        LivingHiveChatConversation selected = conversations.FirstOrDefault(value => string.Equals(value.ConversationId, conversationId, StringComparison.Ordinal));
                        if (selected != null) { selected.UnreadCount = 0; selected.MentionCount = 0; selected.LastSequence = Math.Max(selected.LastSequence, latest); }
                    }
                }
                PersistRecentCache();
            }
            catch (RemoteChatTransportException exception)
            {
                SetStatus(MapStatus(exception.Error), exception.ServerCode ?? exception.Error.ToString());
            }
        }

        public async Task SendAsync(string body, CancellationToken ct)
        {
            string normalized = body?.Trim();
            if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("Message body is required.", nameof(body));
            string conversationId;
            lock (gate) conversationId = selectedConversationId;
            if (string.IsNullOrWhiteSpace(conversationId)) throw new InvalidOperationException("No accessible conversation is selected.");
            string requestId = Guid.NewGuid().ToString("N");
            var optimistic = new LivingHiveChatMessage { ConversationId = conversationId, ClientRequestId = requestId, OriginalBody = normalized, VisibleBody = normalized, CreatedAt = DateTimeOffset.UtcNow, Delivery = LivingHiveChatDelivery.Queued };
            lock (gate) { messages.Add(optimistic); TrimMessages(); pendingCount++; }
            try
            {
                RemoteSendResult result = await provider.SendAsync(conversationId, normalized, requestId, ct);
                lock (gate)
                {
                    int index = messages.FindIndex(value => string.Equals(value.ClientRequestId, requestId, StringComparison.Ordinal));
                    if (index >= 0) messages[index] = MapMessage(result.Message);
                    pendingCount = Math.Max(0, pendingCount - 1);
                    TrimMessages();
                }
                PersistRecentCache();
            }
            catch (RemoteChatTransportException exception)
            {
                lock (gate)
                {
                    LivingHiveChatMessage pending = messages.FirstOrDefault(value => string.Equals(value.ClientRequestId, requestId, StringComparison.Ordinal));
                    if (pending != null) pending.Delivery = IsRetryable(exception.Error) ? LivingHiveChatDelivery.Queued : LivingHiveChatDelivery.Failed;
                }
                SetStatus(MapStatus(exception.Error), exception.ServerCode ?? exception.Error.ToString());
            }
        }

        public async Task ResumeAsync(CancellationToken ct)
        {
            SetStatus(LivingHiveChatStatus.Connecting, null);
            try
            {
                ChatPendingDrainResult drained = await provider.DrainPendingAsync(ct);
                lock (gate) pendingCount = drained?.Remaining?.Total ?? 0;
                await OpenAsync(ct);
            }
            catch (ChatPendingDrainException exception)
            {
                lock (gate) pendingCount = exception.Result?.Remaining?.Total ?? pendingCount;
                SetStatus(LivingHiveChatStatus.Offline, "pending_drain_incomplete");
            }
            catch (RemoteChatTransportException exception) { SetStatus(MapStatus(exception.Error), exception.ServerCode ?? exception.Error.ToString()); }
        }

        public async Task TranslateAsync(string messageId, string targetLocale, string modelVersion, CancellationToken ct)
        {
            LivingHiveChatMessage message;
            lock (gate) message = messages.FirstOrDefault(value => string.Equals(value.MessageId, messageId, StringComparison.Ordinal));
            if (message == null || string.IsNullOrWhiteSpace(message.MessageId)) throw new ArgumentException("A confirmed visible message is required.", nameof(messageId));
            TranslationDisplayState state = await translations.TranslateAsync(new RemoteChatMessage { MessageId = message.MessageId, ConversationId = message.ConversationId, OriginalBody = message.OriginalBody }, targetLocale, modelVersion, ct);
            lock (gate)
            {
                LivingHiveChatMessage current = messages.FirstOrDefault(value => string.Equals(value.MessageId, messageId, StringComparison.Ordinal));
                if (current != null && state.Mode == TranslationDisplayMode.Translated) { current.VisibleBody = state.VisibleText; current.SourceLocale = state.SourceLocale; current.TargetLocale = state.TargetLocale; current.IsTranslated = true; }
            }
        }

        public void ShowOriginal(string messageId)
        {
            lock (gate)
            {
                LivingHiveChatMessage current = messages.FirstOrDefault(value => string.Equals(value.MessageId, messageId, StringComparison.Ordinal));
                if (current != null) { current.VisibleBody = current.OriginalBody; current.IsTranslated = false; }
            }
        }

        public async Task CloseAsync(CancellationToken ct)
        {
            liveUpdates?.Cancel();
            try { await Task.WhenAll(pollingTask, RealtimeReceiptTask()); } catch (Exception) { }
            liveUpdates?.Dispose();
            liveUpdates = null;
            pollingTask = Task.CompletedTask;
            await provider.DisconnectAsync(ct);
            lock (gate) { messages.Clear(); conversations.Clear(); selectedConversationId = null; pendingCount = 0; status = LivingHiveChatStatus.Offline; errorCode = null; }
        }

        private void SetStatus(LivingHiveChatStatus value, string code) { lock (gate) { status = value; errorCode = code; } }
        public Task AwaitRealtimeReceiptsAsync() => RealtimeReceiptTask();
        private Task RealtimeReceiptTask() { lock (gate) return realtimeReceiptTask; }
        private void EnsureLiveUpdates(CancellationToken lifetime, bool usePolling)
        {
            if (liveUpdates != null && !liveUpdates.IsCancellationRequested) return;
            liveUpdates?.Cancel();
            liveUpdates?.Dispose();
            liveUpdates = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
            pollingTask = usePolling ? PollLoopAsync(liveUpdates.Token) : Task.CompletedTask;
        }
        private async Task PollLoopAsync(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    await delay.WaitAsync(pollInterval, ct);
                    await RefreshConversationListAsync(ct);
                    await RefreshSelectedAsync(ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { SetStatus(LivingHiveChatStatus.Offline, "polling_interrupted"); }
        }
        private async Task RefreshConversationListAsync(CancellationToken ct)
        {
            RemoteConversationLoadResult loaded = await provider.LoadAllConversationsAsync(new ChatPaginationPolicy(50, 10), ct);
            IReadOnlyList<RemoteConversation> accessible = loaded?.Items ?? Array.Empty<RemoteConversation>();
            lock (gate)
            {
                conversations.Clear();
                conversations.AddRange(accessible.Where(value => value != null && !string.IsNullOrWhiteSpace(value.ConversationId)).Select(MapConversation));
            }
            await provider.EnsureRealtimeSubscriptionsAsync(accessible.Select(value => value.ConversationId), ct);
            PersistRecentCache();
        }
        private void OnRealtimeEventApplied(RemoteChatEvent evt)
        {
            if (evt?.Message == null) return;
            bool changed = false;
            lock (gate)
            {
                LivingHiveChatConversation conversation = conversations.FirstOrDefault(value => string.Equals(value.ConversationId, evt.ConversationId, StringComparison.Ordinal));
                if (conversation != null) conversation.LastSequence = Math.Max(conversation.LastSequence, evt.Message.Sequence);
                if (!string.Equals(selectedConversationId, evt.ConversationId, StringComparison.Ordinal))
                {
                    if (conversation != null) conversation.UnreadCount++;
                    changed = conversation != null;
                }
                else
                {
                    int index = messages.FindIndex(value => value.Sequence == evt.Message.Sequence || !string.IsNullOrEmpty(evt.Message.ClientRequestId) && string.Equals(value.ClientRequestId, evt.Message.ClientRequestId, StringComparison.Ordinal));
                    LivingHiveChatMessage mapped = MapMessage(evt.Message);
                    if (index >= 0) messages[index] = mapped; else messages.Add(mapped);
                    messages.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
                    TrimMessages();
                    changed = true;
                    CancellationToken token = liveUpdates?.Token ?? CancellationToken.None;
                    Task previous = realtimeReceiptTask;
                    realtimeReceiptTask = AcknowledgeRealtimeReadAsync(previous, evt.ConversationId, evt.Message.Sequence, token);
                }
            }
            if (changed) PersistRecentCache();
        }
        private async Task AcknowledgeRealtimeReadAsync(Task previous, string conversationId, long sequence, CancellationToken ct)
        {
            await Task.Yield();
            try { await previous; } catch (OperationCanceledException) { }
            ct.ThrowIfCancellationRequested();
            await provider.MarkReadAsync(conversationId, sequence, ct);
            lock (gate)
            {
                LivingHiveChatConversation conversation = conversations.FirstOrDefault(value => string.Equals(value.ConversationId, conversationId, StringComparison.Ordinal));
                if (conversation != null) { conversation.UnreadCount = 0; conversation.MentionCount = 0; }
            }
            PersistRecentCache();
        }
        private void TrimMessages() { if (messages.Count > recentMessageLimit) messages.RemoveRange(0, messages.Count - recentMessageLimit); }
        private void RestoreRecentCache()
        {
            if (recentCache == null) return;
            lock (gate) if (conversations.Count > 0 || messages.Count > 0) return;
            try
            {
                ChatRecentCacheSnapshot cached = recentCache.Load();
                lock (gate)
                {
                    conversations.Clear();
                    conversations.AddRange((cached.Conversations ?? Array.Empty<LivingHiveChatConversation>()).Select(Clone));
                    selectedConversationId = cached.SelectedConversationId;
                    messages.Clear();
                    messages.AddRange((cached.Messages ?? Array.Empty<LivingHiveChatMessage>()).Where(item => item != null && item.Delivery == LivingHiveChatDelivery.Confirmed).TakeLast(recentMessageLimit).Select(Clone));
                }
            }
            catch (ChatRecentCacheException) { SetStatus(LivingHiveChatStatus.Offline, "local_recent_cache_quarantined"); }
            catch (Exception) { SetStatus(LivingHiveChatStatus.Offline, "local_recent_cache_unavailable"); }
        }

        private void PersistRecentCache()
        {
            if (recentCache == null) return;
            try
            {
                ChatRecentCacheSnapshot snapshot;
                lock (gate) snapshot = new ChatRecentCacheSnapshot { SelectedConversationId = selectedConversationId, Conversations = conversations.Select(Clone).ToArray(), Messages = messages.Where(item => item.Delivery == LivingHiveChatDelivery.Confirmed).Select(Clone).ToArray() };
                recentCache.Save(snapshot);
            }
            catch (Exception) { SetStatus(LivingHiveChatStatus.Offline, "local_recent_cache_unavailable"); }
        }
        private static bool IsRetryable(RemoteChatError error) => error == RemoteChatError.Transport || error == RemoteChatError.Offline || error == RemoteChatError.RateLimited || error == RemoteChatError.Cancelled;
        private static LivingHiveChatStatus MapStatus(RemoteChatError error) => error == RemoteChatError.Unauthorized || error == RemoteChatError.LocalAccountMismatch ? LivingHiveChatStatus.AuthenticationRequired : error == RemoteChatError.Disabled || error == RemoteChatError.Incompatible ? LivingHiveChatStatus.Unavailable : error == RemoteChatError.Transport || error == RemoteChatError.Offline || error == RemoteChatError.RateLimited ? LivingHiveChatStatus.Offline : LivingHiveChatStatus.Error;
        private static LivingHiveChatConversation MapConversation(RemoteConversation value) => new LivingHiveChatConversation { ConversationId = value.ConversationId, Title = value.Title, ChannelType = value.ChannelType, LastSequence = value.LastSequence, UnreadCount = Math.Max(0, value.UnreadCount), MentionCount = Math.Max(0, value.MentionCount) };
        private static LivingHiveChatMessage MapMessage(RemoteChatMessage value) => new LivingHiveChatMessage { MessageId = value.MessageId, ConversationId = value.ConversationId, ClientRequestId = value.ClientRequestId, SenderPlayerId = value.SenderId, SenderDisplayName = value.SenderDisplayName, OriginalBody = value.OriginalBody, VisibleBody = value.OriginalBody, Sequence = value.Sequence, CreatedAt = value.CreatedAt, Delivery = LivingHiveChatDelivery.Confirmed };
        private static LivingHiveChatConversation Clone(LivingHiveChatConversation value) => new LivingHiveChatConversation { ConversationId = value.ConversationId, Title = value.Title, ChannelType = value.ChannelType, LastSequence = value.LastSequence, UnreadCount = value.UnreadCount, MentionCount = value.MentionCount };
        private static LivingHiveChatMessage Clone(LivingHiveChatMessage value) => new LivingHiveChatMessage { MessageId = value.MessageId, ConversationId = value.ConversationId, ClientRequestId = value.ClientRequestId, SenderPlayerId = value.SenderPlayerId, SenderDisplayName = value.SenderDisplayName, OriginalBody = value.OriginalBody, VisibleBody = value.VisibleBody, SourceLocale = value.SourceLocale, TargetLocale = value.TargetLocale, Sequence = value.Sequence, CreatedAt = value.CreatedAt, Delivery = value.Delivery, IsTranslated = value.IsTranslated };
    }

    public static class LivingHiveChatRuntime
    {
        private static readonly object Gate = new object();
        private static LivingHiveChatController controller;
        private static CancellationTokenSource lifetime;

        public static bool IsConfigured { get { lock (Gate) return controller != null; } }
        public static LivingHiveChatSnapshot Snapshot { get { lock (Gate) return controller?.Snapshot() ?? new LivingHiveChatSnapshot { Status = LivingHiveChatStatus.NotConfigured }; } }
        public static async Task ReconfigureAsync(LivingHiveChatController value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            await ResetAsync();
            lock (Gate) { controller = value; lifetime = new CancellationTokenSource(); }
        }
        public static Task OpenAsync() { lock (Gate) return controller == null ? Task.CompletedTask : controller.OpenAsync(lifetime.Token); }
        public static Task SelectAsync(string id) { lock (Gate) return controller == null ? Task.CompletedTask : controller.SelectConversationAsync(id, lifetime.Token); }
        public static Task SendAsync(string body) { lock (Gate) return controller == null ? Task.CompletedTask : controller.SendAsync(body, lifetime.Token); }
        public static Task ResumeAsync() { lock (Gate) return controller == null ? Task.CompletedTask : controller.ResumeAsync(lifetime.Token); }
        public static Task TranslateAsync(string messageId, string locale, string modelVersion) { lock (Gate) return controller == null ? Task.CompletedTask : controller.TranslateAsync(messageId, locale, modelVersion, lifetime.Token); }
        public static void ShowOriginal(string messageId) { lock (Gate) controller?.ShowOriginal(messageId); }
        public static async Task CloseAsync() { LivingHiveChatController value; CancellationToken token; lock (Gate) { value = controller; token = lifetime?.Token ?? CancellationToken.None; } if (value != null) await value.CloseAsync(token); }
        public static async Task ResetAsync()
        {
            LivingHiveChatController value;
            lock (Gate) { lifetime?.Cancel(); value = controller; }
            if (value != null) try { await value.CloseAsync(CancellationToken.None); } catch { }
            lock (Gate) { lifetime?.Dispose(); lifetime = null; controller = null; }
        }
    }
}
