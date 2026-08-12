using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    public sealed class ServerChatProvider
    {
        private const int MaxConversationCursorCharacters = 1024;
        private readonly IChatRestTransport rest;
        private readonly IChatRealtimeTransport realtime;
        private readonly IChatSessionSource sessions;
        private readonly IChatDelay delay;
        private readonly ChatRetryPolicy retryPolicy;
        private readonly IChatPendingSendStore pendingSends;
        private readonly IChatErrorDecoder errorDecoder;
        private readonly IChatPendingConversationStore pendingConversations;
        private readonly IChatPendingModerationReportStore pendingReports;
        private readonly IChatPendingReadStore pendingReads;
        private readonly IChatDiagnosticsSink diagnostics;
        private readonly ChatPendingReplayPolicy replayPolicy;
        private readonly IChatClock clock;
        private TimeSpan effectiveReplayMaxAge;
        private readonly bool requireCapabilityNegotiation;
        private readonly ChatCapabilityLeasePolicy capabilityLeasePolicy;
        private readonly string expectedPlayerId;
        private DateTimeOffset? capabilitiesNegotiatedAtUtc;
        private readonly Dictionary<string, SortedDictionary<long, RemoteChatMessage>> messages = new Dictionary<string, SortedDictionary<long, RemoteChatMessage>>(StringComparer.Ordinal);
        private readonly Dictionary<string, RemoteChatMessage> requests = new Dictionary<string, RemoteChatMessage>(StringComparer.Ordinal);
        private readonly Dictionary<string, MessageTranslation> translations = new Dictionary<string, MessageTranslation>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> confirmedSequences = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly HashSet<string> joinedRealtimeConversationIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly SemaphoreSlim realtimeGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim pendingDrainGate = new SemaphoreSlim(1, 1);
        private int sessionEpoch;

        public RemoteChatConnectionState ConnectionState { get; private set; } = RemoteChatConnectionState.Offline;
        public RemoteCapabilities NegotiatedCapabilities { get; private set; }
        public event Action<RemoteChatEvent> RealtimeEventApplied;
        public ServerChatProvider(IChatRestTransport rest, IChatSessionSource sessions, IChatRealtimeTransport realtime = null, ChatRetryPolicy retryPolicy = null, IChatDelay delay = null, IChatPendingSendStore pendingSends = null, IChatErrorDecoder errorDecoder = null, IChatPendingConversationStore pendingConversations = null, IChatPendingModerationReportStore pendingReports = null, IChatPendingReadStore pendingReads = null, IChatDiagnosticsSink diagnostics = null, ChatPendingReplayPolicy replayPolicy = null, IChatClock clock = null, bool requireCapabilityNegotiation = false, ChatCapabilityLeasePolicy capabilityLeasePolicy = null, string expectedPlayerId = null)
        { this.rest = rest ?? throw new ArgumentNullException(nameof(rest)); this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions)); this.realtime = realtime; this.retryPolicy = retryPolicy ?? new ChatRetryPolicy(); this.delay = delay ?? new TaskChatDelay(); this.pendingSends = pendingSends ?? new MemoryPendingSendStore(); this.errorDecoder = errorDecoder; this.pendingConversations = pendingConversations ?? new MemoryPendingConversationStore(); this.pendingReports = pendingReports ?? new MemoryPendingReportStore(); this.pendingReads = pendingReads ?? new MemoryPendingReadStore(); this.diagnostics = diagnostics; this.replayPolicy = replayPolicy ?? new ChatPendingReplayPolicy(); this.clock = clock ?? new SystemChatClock(); this.requireCapabilityNegotiation = requireCapabilityNegotiation; this.capabilityLeasePolicy = capabilityLeasePolicy ?? new ChatCapabilityLeasePolicy(); if (expectedPlayerId != null && !ChatSessionSecurity.IsValidPlayerId(expectedPlayerId)) throw new ArgumentException("Expected player identity is invalid.", nameof(expectedPlayerId)); this.expectedPlayerId = expectedPlayerId; effectiveReplayMaxAge = this.replayPolicy.MaxAge; }

        public Task<RemoteCapabilities> GetCapabilitiesAsync(CancellationToken ct) => Send<RemoteCapabilities>("GET", "/chat/v1/capabilities", null, ct, false, false, true);
        public async Task<RemoteCapabilityDecision> NegotiateCapabilitiesAsync(string requiredProtocol, CancellationToken ct)
        {
            InvalidateCapabilities();
            RemoteCapabilities capabilities = await GetCapabilitiesAsync(ct);
            if (capabilities == null) throw new RemoteChatTransportException(RemoteChatError.InvalidResponse, "Chat capabilities response is missing.");
            NegotiatedCapabilities = capabilities;
            if (!string.Equals(capabilities.ProtocolVersion, requiredProtocol, StringComparison.Ordinal)) { Emit("capability_rejected", "negotiate", serverCode: "protocol_incompatible"); return new RemoteCapabilityDecision { IsAvailable = false, ReasonCode = "protocol_incompatible", Capabilities = capabilities }; }
            if (!string.Equals(capabilities.Provider, "server", StringComparison.OrdinalIgnoreCase)) return RejectCapabilities(capabilities, "provider_invalid");
            if (!capabilities.Server) { Emit("capability_rejected", "negotiate", serverCode: "server_disabled"); return new RemoteCapabilityDecision { IsAvailable = false, ReasonCode = "server_disabled", Capabilities = capabilities }; }
            if (!ValidLimits(capabilities.Limits)) return RejectCapabilities(capabilities, "limits_invalid");
            if (!ValidChannels(capabilities.Channels)) return RejectCapabilities(capabilities, "channels_invalid");
            if (capabilities.IdempotencyReceiptRetentionDays < 2 || capabilities.IdempotencyReceiptRetentionDays > 3650) return RejectCapabilities(capabilities, "receipt_retention_invalid");
            effectiveReplayMaxAge = TimeSpan.FromDays(Math.Min(replayPolicy.MaxAge.TotalDays, capabilities.IdempotencyReceiptRetentionDays - 1));
            capabilitiesNegotiatedAtUtc = clock.UtcNow;
            bool useRealtime = capabilities.Realtime && realtime != null && realtime.IsAvailable;
            Emit("capability_ready", "negotiate", count: useRealtime ? 1 : 0);
            return new RemoteCapabilityDecision { IsAvailable = true, UseRealtime = useRealtime, ReasonCode = capabilities.Realtime ? "ready" : "polling_only", Capabilities = capabilities, EffectiveReplayMaxAgeDays = (int)effectiveReplayMaxAge.TotalDays };
        }
        private RemoteCapabilityDecision RejectCapabilities(RemoteCapabilities capabilities, string reason)
        { Emit("capability_rejected", "negotiate", serverCode: reason); return new RemoteCapabilityDecision { IsAvailable = false, ReasonCode = reason, Capabilities = capabilities }; }
        private static bool ValidLimits(RemoteChatLimits limits) => limits != null
            && limits.BodyMaxCharacters >= 1 && limits.BodyMaxCharacters <= 4000
            && limits.MessagesPerMinutePerPlayer >= 1 && limits.MessagesPerMinutePerPlayer <= 600
            && limits.MessagesPerTenSecondsPerConversation >= 1 && limits.MessagesPerTenSecondsPerConversation <= 100
            && limits.PrivateConversationCreatesPerHour >= 1 && limits.PrivateConversationCreatesPerHour <= 1000
            && limits.MaxPrivateRecipients >= 1 && limits.MaxPrivateRecipients <= 100;
        private static bool ValidChannels(IReadOnlyList<string> channels)
        {
            if (channels == null || channels.Count == 0) return false;
            var known = new HashSet<string>(new[] { "Alliance", "Server", "Private", "Leaders" }, StringComparer.OrdinalIgnoreCase);
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return channels.All(channel => !string.IsNullOrWhiteSpace(channel) && known.Contains(channel) && unique.Add(channel));
        }
        public Task<RemoteConversationPage> ListConversationsAsync(int limit, CancellationToken ct)
        { return ListConversationsAsync(limit, null, ct); }
        public async Task<RemoteConversationPage> ListConversationsAsync(int limit, string cursor, CancellationToken ct)
        {
            EnsureRemoteOperationReady("list_conversations");
            if (limit < 1 || limit > 100) throw new ArgumentOutOfRangeException(nameof(limit));
            string path = "/chat/v1/conversations?limit=" + limit;
            if (cursor != null) path += "&cursor=" + Uri.EscapeDataString(ValidateCursor(cursor, nameof(cursor)));
            RemoteConversationPage page = await GetAsync<RemoteConversationPage>(path, ct);
            ValidateConversationPage(page, limit);
            return page;
        }
        public async Task<RemoteConversationLoadResult> LoadAllConversationsAsync(ChatPaginationPolicy policy, CancellationToken ct)
        {
            policy = policy ?? new ChatPaginationPolicy();
            var items = new Dictionary<string, RemoteConversation>(StringComparer.Ordinal);
            var cursors = new HashSet<string>(StringComparer.Ordinal);
            string cursor = null;
            for (int pageIndex = 1; pageIndex <= policy.MaxPages; pageIndex++)
            {
                RemoteConversationPage page = await ListConversationsAsync(policy.PageSize, cursor, ct) ?? throw new RemoteChatTransportException(RemoteChatError.InvalidResponse, "Chat server returned no conversation page.");
                foreach (RemoteConversation item in page.Items ?? new List<RemoteConversation>()) if (item != null && !string.IsNullOrWhiteSpace(item.ConversationId)) items[item.ConversationId] = item;
                if (string.IsNullOrWhiteSpace(page.NextCursor)) return new RemoteConversationLoadResult { Items = items.Values.ToList(), IsComplete = true, PagesLoaded = pageIndex };
                try { ValidateCursor(page.NextCursor, nameof(page.NextCursor)); }
                catch (ArgumentException exception) { throw new RemoteChatTransportException(RemoteChatError.InvalidResponse, "Chat server returned an invalid conversation cursor.", 0, "invalid_conversation_cursor", innerException: exception); }
                if (!cursors.Add(page.NextCursor) || string.Equals(page.NextCursor, cursor, StringComparison.Ordinal)) throw new RemoteChatTransportException(RemoteChatError.InvalidResponse, "Chat conversation cursor did not progress.");
                cursor = page.NextCursor;
                if (pageIndex == policy.MaxPages) return new RemoteConversationLoadResult { Items = items.Values.ToList(), IsComplete = false, PagesLoaded = pageIndex, NextCursor = cursor };
            }
            throw new InvalidOperationException("Unreachable pagination state.");
        }
        public async Task<RemoteCreateConversationResult> CreateConversationAsync(RemoteCreateConversationRequest request, CancellationToken ct)
        {
            EnsureRemoteOperationReady("create_conversation");
            int operationEpoch = Volatile.Read(ref sessionEpoch);
            ChatSession operationSession = await RequireSession(ct);
            EnsureSessionEpoch(operationEpoch);
            if (request == null || string.IsNullOrWhiteSpace(request.ClientRequestId)) throw new ArgumentException("A stable client request id is required.", nameof(request));
            RemoteCreateConversationRequest normalized = Normalize(request);
            ValidateConversationEnvelope(normalized);
            ValidateConversationCapabilities(normalized);
            PendingChatConversationCreation pending = (await AccessPendingAsync(() => pendingConversations.LoadAsync(ct), "create_conversation")).FirstOrDefault(item => string.Equals(item.Request?.ClientRequestId, normalized.ClientRequestId, StringComparison.Ordinal));
            if (pending != null && !Same(pending.Request, normalized)) throw new InvalidOperationException("ClientRequestId collision with a different pending conversation payload.");
            if (pending == null) pending = new PendingChatConversationCreation { Request = normalized, EnqueuedAtUtc = clock.UtcNow.ToString("O") };
            EnsureReplayFresh(pending.EnqueuedAtUtc, "create_conversation");
            await PersistPendingAsync(() => pendingConversations.SaveAsync(pending, ct), "create_conversation");
            return await CreatePendingConversationAsync(pending, ct, operationSession, operationEpoch);
        }

        public async Task<IReadOnlyList<RemoteCreateConversationResult>> RetryPendingConversationsAsync(CancellationToken ct)
        {
            EnsureRemoteOperationReady("retry_conversations");
            int operationEpoch = Volatile.Read(ref sessionEpoch);
            ChatSession operationSession = await RequireSession(ct);
            EnsureSessionEpoch(operationEpoch);
            var results = new List<RemoteCreateConversationResult>();
            foreach (PendingChatConversationCreation pending in await AccessPendingAsync(() => pendingConversations.LoadAsync(ct), "retry_conversations"))
            { ct.ThrowIfCancellationRequested(); EnsureReplayFresh(pending.EnqueuedAtUtc, "create_conversation"); results.Add(await CreatePendingConversationAsync(pending, ct, operationSession, operationEpoch)); }
            return results;
        }

        public async Task<IReadOnlyList<RemoteChatMessage>> ReconcileAsync(string conversationId, long afterSequence, CancellationToken ct)
        {
            EnsureRemoteOperationReady("reconcile_messages");
            if (afterSequence < 0) throw new ArgumentOutOfRangeException(nameof(afterSequence));
            string conversationSegment = OpaquePathSegment(conversationId, nameof(conversationId));
            RemoteMessagePage page = await GetAsync<RemoteMessagePage>("/chat/v1/conversations/" + conversationSegment + "/messages?afterSequence=" + afterSequence, ct);
            ValidateMessagePage(page, conversationId, afterSequence);
            confirmedSequences[conversationId] = Math.Max(GetConfirmedSequence(conversationId), afterSequence);
            Merge(conversationId, page.Items);
            return Snapshot(conversationId);
        }
        public async Task<RemoteReconciliationResult> ReconcileFullyAsync(string conversationId, long afterSequence, ChatPaginationPolicy policy, CancellationToken ct)
        {
            EnsureRemoteOperationReady("reconcile_messages");
            if (afterSequence < 0) throw new ArgumentOutOfRangeException(nameof(afterSequence));
            string conversationSegment = OpaquePathSegment(conversationId, nameof(conversationId));
            policy = policy ?? new ChatPaginationPolicy();
            long cursor = afterSequence;
            var seen = new HashSet<long>();
            for (int pageIndex = 1; pageIndex <= policy.MaxPages; pageIndex++)
            {
                RemoteMessagePage page = await GetAsync<RemoteMessagePage>("/chat/v1/conversations/" + conversationSegment + "/messages?afterSequence=" + cursor + "&limit=" + policy.PageSize, ct) ?? throw new RemoteChatTransportException(RemoteChatError.InvalidResponse, "Chat server returned no message page.");
                ValidateMessagePage(page, conversationId, cursor, policy.PageSize);
                confirmedSequences[conversationId] = Math.Max(GetConfirmedSequence(conversationId), cursor);
                Merge(conversationId, page.Items);
                if (!page.NextAfterSequence.HasValue) return new RemoteReconciliationResult { Items = Snapshot(conversationId), IsComplete = true, PagesLoaded = pageIndex, ConfirmedSequence = GetConfirmedSequence(conversationId) };
                long next = page.NextAfterSequence.Value;
                if (next <= cursor || !seen.Add(next)) throw new RemoteChatTransportException(RemoteChatError.InvalidResponse, "Chat message cursor did not progress.");
                cursor = next;
                if (pageIndex == policy.MaxPages) return new RemoteReconciliationResult { Items = Snapshot(conversationId), IsComplete = false, PagesLoaded = pageIndex, ConfirmedSequence = GetConfirmedSequence(conversationId), NextAfterSequence = cursor };
            }
            throw new InvalidOperationException("Unreachable pagination state.");
        }

        public long GetConfirmedSequence(string conversationId) => confirmedSequences.TryGetValue(conversationId, out long value) ? value : 0;

        public async Task<IReadOnlyList<RemoteChatMessage>> ApplyRealtimeEventAsync(RemoteChatEvent evt, CancellationToken ct)
        {
            if (evt == null || evt.Message == null || string.IsNullOrWhiteSpace(evt.ConversationId)) return new List<RemoteChatMessage>();
            int operationEpoch = Volatile.Read(ref sessionEpoch);
            await RequireSession(ct);
            if (!IsValidMessage(evt.Message, evt.ConversationId) || evt.Sequence.HasValue && evt.Sequence.Value != evt.Message.Sequence)
                throw InvalidReceipt("realtime_event_mismatch");
            await realtimeGate.WaitAsync(ct);
            try
            {
                EnsureSessionEpoch(operationEpoch);
                long confirmed = GetConfirmedSequence(evt.ConversationId);
                if (evt.Message.Sequence > confirmed + 1) { Emit("realtime_sequence_gap", "reconcile", count: checked((int)Math.Min(int.MaxValue, evt.Message.Sequence - confirmed - 1))); await ReconcileAsync(evt.ConversationId, confirmed, ct); }
                EnsureSessionEpoch(operationEpoch);
                Merge(evt.ConversationId, new[] { evt.Message });
                try { RealtimeEventApplied?.Invoke(evt); } catch { }
                return Snapshot(evt.ConversationId);
            }
            finally { realtimeGate.Release(); }
        }

        public async Task<IReadOnlyList<RemoteChatMessage>> PollWithRetryAsync(string conversationId, long afterSequence, CancellationToken ct)
        {
            EnsureRemoteOperationReady("poll_messages");
            Exception last = null;
            for (int attempt = 1; attempt <= retryPolicy.MaxAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try { ConnectionState = RemoteChatConnectionState.Polling; return (await ReconcileFullyAsync(conversationId, afterSequence, null, ct)).Items; }
                catch (RemoteChatTransportException exception) when (!IsTransientPollFailure(exception)) { throw; }
                catch (Exception exception) when (!(exception is OperationCanceledException))
                {
                    last = exception;
                    if (attempt < retryPolicy.MaxAttempts)
                    {
                        RemoteChatError error = exception is RemoteChatTransportException remote ? remote.Error : RemoteChatError.Transport;
                        Emit("poll_retry", "messages", error: error, attempt: attempt);
                        await delay.WaitAsync(PollRetryDelay(exception), ct);
                    }
                }
            }
            ConnectionState = RemoteChatConnectionState.Offline;
            if (last is RemoteChatTransportException typed) throw typed;
            throw new InvalidOperationException("Chat polling retry limit reached.", last);
        }

        private static bool IsTransientPollFailure(RemoteChatTransportException exception) => exception.Error == RemoteChatError.Transport || exception.Error == RemoteChatError.Offline || exception.Error == RemoteChatError.RateLimited;
        private TimeSpan PollRetryDelay(Exception exception)
        {
            int retryAfterSeconds = exception is RemoteChatTransportException remote && remote.RetryAfterSeconds.HasValue ? Math.Max(0, Math.Min(300, remote.RetryAfterSeconds.Value)) : 0;
            TimeSpan serverDelay = TimeSpan.FromSeconds(retryAfterSeconds);
            return serverDelay > retryPolicy.Delay ? serverDelay : retryPolicy.Delay;
        }

        public async Task<RemoteSendResult> SendAsync(string conversationId, string body, string clientRequestId, CancellationToken ct)
        {
            EnsureRemoteOperationReady("send_message");
            int operationEpoch = Volatile.Read(ref sessionEpoch);
            ChatSession operationSession = await RequireSession(ct);
            EnsureSessionEpoch(operationEpoch);
            OpaquePathSegment(conversationId, nameof(conversationId));
            ValidateClientRequestId(clientRequestId, nameof(clientRequestId));
            if (string.IsNullOrWhiteSpace(body) || body.Length > 4000 || !string.Equals(body, body.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Message body must contain 1 to 4000 non-padding characters.", nameof(body));
            ValidateSendCapabilities(body);
            if (requests.TryGetValue(clientRequestId, out RemoteChatMessage known)) return new RemoteSendResult { Message = known, Deduplicated = true, ServerSequence = known.Sequence };
            PendingChatSend pending = await FindPendingAsync(clientRequestId, ct);
            if (pending != null && (!string.Equals(pending.ConversationId, conversationId, StringComparison.Ordinal) || !string.Equals(pending.Body, body, StringComparison.Ordinal)))
                throw new InvalidOperationException("ClientRequestId collision with a different pending payload.");
            if (pending == null) pending = new PendingChatSend { ConversationId = conversationId, Body = body, ClientRequestId = clientRequestId, ClientCreatedAt = clock.UtcNow.ToString("O") };
            EnsureReplayFresh(pending.ClientCreatedAt, "send_message");
            await PersistPendingAsync(() => pendingSends.SaveAsync(pending, ct), "send_message");
            return await SendPendingAsync(pending, ct, operationSession, operationEpoch);
        }

        public async Task<IReadOnlyList<RemoteSendResult>> RetryPendingAsync(CancellationToken ct)
        {
            EnsureRemoteOperationReady("retry_messages");
            int operationEpoch = Volatile.Read(ref sessionEpoch);
            ChatSession operationSession = await RequireSession(ct);
            EnsureSessionEpoch(operationEpoch);
            var results = new List<RemoteSendResult>();
            foreach (PendingChatSend pending in await AccessPendingAsync(() => pendingSends.LoadAsync(ct), "retry_messages"))
            {
                ct.ThrowIfCancellationRequested();
                EnsureReplayFresh(pending.ClientCreatedAt, "send_message");
                results.Add(await SendPendingAsync(pending, ct, operationSession, operationEpoch));
            }
            return results;
        }

        public async Task<object> MarkReadAsync(string conversationId, long sequence, CancellationToken ct)
        {
            EnsureRemoteOperationReady("mark_read");
            int operationEpoch = Volatile.Read(ref sessionEpoch);
            ChatSession operationSession = await RequireSession(ct);
            EnsureSessionEpoch(operationEpoch);
            OpaquePathSegment(conversationId, nameof(conversationId));
            if (string.IsNullOrWhiteSpace(conversationId) || sequence < 0) throw new ArgumentException("Conversation and non-negative sequence are required.");
            ValidateFeature(NegotiatedCapabilities == null || NegotiatedCapabilities.ReadCursors, "read_cursors_not_supported");
            await PersistPendingAsync(() => pendingReads.SaveMaximumAsync(new PendingReadCursor { ConversationId = conversationId.Trim(), Sequence = sequence, EnqueuedAtUtc = clock.UtcNow.ToString("O") }, ct), "mark_read");
            PendingReadCursor pending = (await AccessPendingAsync(() => pendingReads.LoadAsync(ct), "mark_read")).First(item => string.Equals(item.ConversationId, conversationId.Trim(), StringComparison.Ordinal));
            EnsureReplayFresh(pending.EnqueuedAtUtc, "mark_read");
            return await SubmitPendingReadAsync(pending, ct, operationSession, operationEpoch);
        }
        public async Task RetryPendingReadsAsync(CancellationToken ct)
        {
            EnsureRemoteOperationReady("retry_reads");
            int operationEpoch = Volatile.Read(ref sessionEpoch);
            ChatSession operationSession = await RequireSession(ct);
            EnsureSessionEpoch(operationEpoch);
            foreach (PendingReadCursor pending in await AccessPendingAsync(() => pendingReads.LoadAsync(ct), "retry_reads")) { ct.ThrowIfCancellationRequested(); EnsureReplayFresh(pending.EnqueuedAtUtc, "mark_read"); await SubmitPendingReadAsync(pending, ct, operationSession, operationEpoch); }
        }
        public async Task<RemoteModerationReport> ReportAsync(string messageId, string category, string clientRequestId, CancellationToken ct)
        {
            EnsureRemoteOperationReady("report_message");
            int operationEpoch = Volatile.Read(ref sessionEpoch);
            ChatSession operationSession = await RequireSession(ct);
            EnsureSessionEpoch(operationEpoch);
            OpaquePathSegment(messageId, nameof(messageId));
            if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(clientRequestId)) throw new ArgumentException("Message, category and stable request id are required.");
            ValidateFeature(NegotiatedCapabilities == null || NegotiatedCapabilities.ModerationReports, "moderation_reports_not_supported");
            var normalized = new PendingModerationReportRequest { MessageId = messageId.Trim(), Category = category.Trim(), ClientRequestId = clientRequestId.Trim(), EnqueuedAtUtc = clock.UtcNow.ToString("O") };
            ValidateClientRequestId(normalized.ClientRequestId, nameof(clientRequestId));
            ValidateBoundedText(normalized.Category, 64, nameof(category));
            PendingModerationReportRequest pending = (await AccessPendingAsync(() => pendingReports.LoadAsync(ct), "report_message")).FirstOrDefault(item => string.Equals(item.ClientRequestId, normalized.ClientRequestId, StringComparison.Ordinal));
            if (pending != null && (!string.Equals(pending.MessageId, normalized.MessageId, StringComparison.Ordinal) || !string.Equals(pending.Category, normalized.Category, StringComparison.Ordinal))) throw new InvalidOperationException("ClientRequestId collision with a different moderation report.");
            if (pending == null) pending = normalized;
            EnsureReplayFresh(pending.EnqueuedAtUtc, "report_message");
            await PersistPendingAsync(() => pendingReports.SaveAsync(pending, ct), "report_message");
            return await SubmitPendingReportAsync(pending, ct, operationSession, operationEpoch);
        }
        public async Task<IReadOnlyList<RemoteModerationReport>> RetryPendingReportsAsync(CancellationToken ct)
        {
            EnsureRemoteOperationReady("retry_reports");
            int operationEpoch = Volatile.Read(ref sessionEpoch);
            ChatSession operationSession = await RequireSession(ct);
            EnsureSessionEpoch(operationEpoch);
            var results = new List<RemoteModerationReport>();
            foreach (PendingModerationReportRequest pending in await AccessPendingAsync(() => pendingReports.LoadAsync(ct), "retry_reports")) { ct.ThrowIfCancellationRequested(); EnsureReplayFresh(pending.EnqueuedAtUtc, "report_message"); results.Add(await SubmitPendingReportAsync(pending, ct, operationSession, operationEpoch)); }
            return results;
        }

        public async Task<ChatPendingQueueStatus> GetPendingQueueStatusAsync(CancellationToken ct)
        {
            await RequireSession(ct);
            IReadOnlyList<PendingChatConversationCreation> conversations = await AccessPendingAsync(() => pendingConversations.LoadAsync(ct), "queue_status");
            IReadOnlyList<PendingChatSend> sends = await AccessPendingAsync(() => pendingSends.LoadAsync(ct), "queue_status");
            IReadOnlyList<PendingReadCursor> reads = await AccessPendingAsync(() => pendingReads.LoadAsync(ct), "queue_status");
            IReadOnlyList<PendingModerationReportRequest> reports = await AccessPendingAsync(() => pendingReports.LoadAsync(ct), "queue_status");
            return new ChatPendingQueueStatus
            {
                Conversations = conversations?.Count ?? 0,
                Sends = sends?.Count ?? 0,
                Reads = reads?.Count ?? 0,
                Reports = reports?.Count ?? 0
            };
        }

        public async Task<ChatPendingDrainResult> DrainPendingAsync(CancellationToken ct)
        {
            EnsureRemoteOperationReady("drain_pending");
            await pendingDrainGate.WaitAsync(ct);
            ChatPendingQueueStatus before = null;
            try
            {
                before = await GetPendingQueueStatusAsync(ct);
                Emit("pending_drain_started", "drain_pending", count: before.Total);
                await RetryPendingConversationsAsync(ct);
                await RetryPendingAsync(ct);
                await RetryPendingReadsAsync(ct);
                await RetryPendingReportsAsync(ct);
                ChatPendingQueueStatus remaining = await GetPendingQueueStatusAsync(ct);
                var result = new ChatPendingDrainResult { Before = before, Remaining = remaining };
                Emit("pending_drain_completed", "drain_pending", count: result.Completed);
                return result;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                ChatPendingQueueStatus remaining = null;
                try { remaining = await GetPendingQueueStatusAsync(CancellationToken.None); } catch { }
                var result = new ChatPendingDrainResult { Before = before, Remaining = remaining };
                RemoteChatError error = exception is RemoteChatTransportException transport ? transport.Error : RemoteChatError.Transport;
                Emit("pending_drain_incomplete", "drain_pending", error: error, count: remaining?.Total ?? before?.Total ?? 0);
                throw new ChatPendingDrainException(result, exception);
            }
            finally { pendingDrainGate.Release(); }
        }

        public async Task<MessageTranslation> TranslateAsync(string messageId, string targetLocale, string modelVersion, CancellationToken ct)
        {
            EnsureRemoteOperationReady("translate_message");
            ChatSession operationSession = await RequireSession(ct);
            string messageSegment = OpaquePathSegment(messageId, nameof(messageId));
            ValidateLocale(targetLocale, nameof(targetLocale));
            ValidateModelVersion(modelVersion, nameof(modelVersion));
            string key = messageId.Length + ":" + messageId + targetLocale.Length + ":" + targetLocale + modelVersion.Length + ":" + modelVersion;
            if (translations.TryGetValue(key, out MessageTranslation cached)) return cached;
            MessageTranslation value = await PostAsync<MessageTranslation>("/chat/v1/messages/" + messageSegment + "/translations", new TranslationRequest { MessageId = messageId, TargetLocale = targetLocale, ModelVersion = modelVersion }, ct, initialSession: operationSession);
            if (!IsMatchingTranslation(value, messageId, targetLocale, modelVersion)) throw InvalidReceipt("translation_response_mismatch");
            if (value != null && string.Equals(value.Status, "completed", StringComparison.OrdinalIgnoreCase)) translations[key] = value;
            return value;
        }

        public string OriginalText(RemoteChatMessage message) => message?.OriginalBody;

        public async Task ConnectAsync(CancellationToken ct)
        {
            RemoteCapabilityDecision decision = await NegotiateCapabilitiesAsync("chat-v1", ct);
            if (!decision.IsAvailable) throw new RemoteChatTransportException(decision.ReasonCode == "server_disabled" ? RemoteChatError.Disabled : RemoteChatError.Incompatible, "Chat remote capability negotiation failed.", 0, decision.ReasonCode);
            ChatSession session = await RequireSession(ct);
            if (!decision.UseRealtime) { ConnectionState = RemoteChatConnectionState.Polling; Emit("connection_polling", "connect"); return; }
            ConnectionState = RemoteChatConnectionState.ConnectingRealtime;
            try { await ConnectRealtimeOnceAsync(session, ct); ConnectionState = RemoteChatConnectionState.Realtime; Emit("connection_realtime", "connect"); }
            catch (RemoteChatTransportException exception) when (exception.Error == RemoteChatError.Unauthorized && sessions is IRefreshableChatSessionSource refreshable)
            {
                ChatSession refreshed = await refreshable.RefreshSessionAsync(ct);
                EnsureValidSession(refreshed);
                try { await ConnectRealtimeOnceAsync(refreshed, ct); ConnectionState = RemoteChatConnectionState.Realtime; }
                catch (RemoteChatTransportException retry) when (retry.Error == RemoteChatError.Unauthorized) { ConnectionState = RemoteChatConnectionState.AuthenticationRequired; throw; }
                catch (RemoteChatTransportException retry) when (IsTransientRealtimeFailure(retry)) { SetRealtimeFallback(retry.Error); }
                catch (RemoteChatTransportException) { ConnectionState = RemoteChatConnectionState.Offline; throw; }
                catch when (!ct.IsCancellationRequested) { SetRealtimeFallback(RemoteChatError.Transport); }
            }
            catch (RemoteChatTransportException exception) when (exception.Error == RemoteChatError.Unauthorized) { ConnectionState = RemoteChatConnectionState.AuthenticationRequired; throw; }
            catch (RemoteChatTransportException exception) when (IsTransientRealtimeFailure(exception)) { SetRealtimeFallback(exception.Error); }
            catch (RemoteChatTransportException) { ConnectionState = RemoteChatConnectionState.Offline; throw; }
            catch when (!ct.IsCancellationRequested) { SetRealtimeFallback(RemoteChatError.Transport); }
        }

        private static bool IsTransientRealtimeFailure(RemoteChatTransportException exception) => exception.Error == RemoteChatError.Transport || exception.Error == RemoteChatError.Offline || exception.Error == RemoteChatError.RateLimited;
        private void SetRealtimeFallback(RemoteChatError error) { ConnectionState = RemoteChatConnectionState.Polling; Emit("realtime_fallback", "connect", error: error); }

        public async Task DisconnectAsync(CancellationToken ct)
        { try { if (realtime != null) await realtime.DisconnectAsync(ct); } finally { ConnectionState = RemoteChatConnectionState.Offline; ClearVolatileState(); InvalidateCapabilities(); } }

        // The realtime hub groups events by conversation: the client must explicitly join every
        // conversation it wants live "chat.event" updates for (unread counts included), not just the
        // one currently open. Safe to call repeatedly — already-joined conversations are skipped.
        public async Task EnsureRealtimeSubscriptionsAsync(IEnumerable<string> conversationIds, CancellationToken ct)
        {
            if (realtime == null || !realtime.IsAvailable || ConnectionState != RemoteChatConnectionState.Realtime) return;
            foreach (string conversationId in conversationIds ?? Enumerable.Empty<string>())
            {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(conversationId) || !joinedRealtimeConversationIds.Add(conversationId)) continue;
                try { await realtime.JoinConversationAsync(conversationId, ct); }
                catch (OperationCanceledException) { joinedRealtimeConversationIds.Remove(conversationId); throw; }
                catch (Exception)
                {
                    // A single conversation failing to join must not break the whole refresh: that
                    // conversation simply misses live updates until the next successful attempt.
                    joinedRealtimeConversationIds.Remove(conversationId);
                    Emit("realtime_join_failed", "join_conversation", error: RemoteChatError.Transport, count: 1);
                }
            }
        }

        private void ClearVolatileState()
        {
            Interlocked.Increment(ref sessionEpoch);
            messages.Clear();
            requests.Clear();
            translations.Clear();
            confirmedSequences.Clear();
            joinedRealtimeConversationIds.Clear();
        }

        public void InvalidateCapabilities()
        {
            NegotiatedCapabilities = null;
            capabilitiesNegotiatedAtUtc = null;
            effectiveReplayMaxAge = replayPolicy.MaxAge;
        }

        private async Task HandleRealtimeAsync(RemoteChatEvent evt)
        { await ApplyRealtimeEventAsync(evt, CancellationToken.None); }

        private void Merge(string conversationId, IEnumerable<RemoteChatMessage> incoming)
        {
            if (!messages.TryGetValue(conversationId, out SortedDictionary<long, RemoteChatMessage> stream)) messages[conversationId] = stream = new SortedDictionary<long, RemoteChatMessage>();
            foreach (RemoteChatMessage message in incoming ?? Enumerable.Empty<RemoteChatMessage>())
            { if (message != null && message.Sequence > 0) stream[message.Sequence] = message; if (!string.IsNullOrEmpty(message?.ClientRequestId)) requests[message.ClientRequestId] = message; }
            long confirmed = GetConfirmedSequence(conversationId);
            while (stream.ContainsKey(confirmed + 1)) confirmed++;
            confirmedSequences[conversationId] = confirmed;
        }

        private IReadOnlyList<RemoteChatMessage> Snapshot(string conversationId) => messages.TryGetValue(conversationId, out SortedDictionary<long, RemoteChatMessage> stream) ? stream.Values.ToList() : new List<RemoteChatMessage>();
        private async Task<T> GetAsync<T>(string path, CancellationToken ct) => await Send<T>("GET", path, null, ct);
        private async Task<T> PostAsync<T>(string path, object body, CancellationToken ct, Action<T, ChatSession> validate = null, ChatSession initialSession = null, int? initialEpoch = null) => await Send<T>("POST", path, body, ct, validate: validate, initialSession: initialSession, initialEpoch: initialEpoch);
        private async Task<T> Send<T>(string method, string path, object body, CancellationToken ct, bool requiresSession = true, bool allowRefresh = true, bool bypassCache = false, Action<T, ChatSession> validate = null, ChatSession initialSession = null, int? initialEpoch = null)
        {
            int operationEpoch = initialEpoch ?? Volatile.Read(ref sessionEpoch);
            ChatSession session = requiresSession ? initialSession ?? await RequireSession(ct) : null;
            if (requiresSession) EnsureValidSession(session);
            EnsureSessionEpoch(operationEpoch);
            ChatTransportResponse<T> response = await rest.SendAsync<T>(new ChatTransportRequest { Method = method, Path = path, Body = body, BearerToken = session?.AccessToken, BypassCache = bypassCache }, ct);
            if (response != null && response.StatusCode != 0 && string.IsNullOrEmpty(response.TransportError)) EnsureSessionEpoch(operationEpoch);
            if (response != null && response.StatusCode == 401 && requiresSession && allowRefresh && sessions is IRefreshableChatSessionSource refreshable)
            {
                ChatSession refreshed = await refreshable.RefreshSessionAsync(ct);
                EnsureValidSession(refreshed);
                return await SendWithSession<T>(method, path, body, refreshed, ct, validate, operationEpoch);
            }
            if (response == null || response.StatusCode == 0 || !string.IsNullOrEmpty(response.TransportError)) throw new RemoteChatTransportException(RemoteChatError.Transport, "Chat network transport did not receive a complete HTTP response.", 0);
            if (response.StatusCode == 401) ConnectionState = RemoteChatConnectionState.AuthenticationRequired;
            if (!response.IsSuccess) throw CreateHttpException(response, response.StatusCode == 401 ? RemoteChatError.Unauthorized : response.StatusCode == 403 ? RemoteChatError.Forbidden : response.StatusCode == 429 ? RemoteChatError.RateLimited : response.StatusCode >= 500 ? RemoteChatError.Transport : RemoteChatError.InvalidResponse);
            if (bypassCache) ValidateCapabilityCachePolicy(response.CacheControl, response.AgeSeconds);
            EnsureSessionEpoch(operationEpoch);
            validate?.Invoke(response.Body, session);
            return response.Body;
        }

        private async Task<T> SendWithSession<T>(string method, string path, object body, ChatSession session, CancellationToken ct, Action<T, ChatSession> validate = null, int? operationEpoch = null)
        {
            int expectedEpoch = operationEpoch ?? Volatile.Read(ref sessionEpoch);
            ChatTransportResponse<T> response = await rest.SendAsync<T>(new ChatTransportRequest { Method = method, Path = path, Body = body, BearerToken = session.AccessToken }, ct);
            if (response != null && response.StatusCode != 0 && string.IsNullOrEmpty(response.TransportError)) EnsureSessionEpoch(expectedEpoch);
            if (response == null || response.StatusCode == 0 || !string.IsNullOrEmpty(response.TransportError)) throw new RemoteChatTransportException(RemoteChatError.Transport, "Chat network transport did not receive a complete HTTP response.", 0);
            if (response.StatusCode == 401) ConnectionState = RemoteChatConnectionState.AuthenticationRequired;
            if (!response.IsSuccess) throw CreateHttpException(response, response.StatusCode == 401 ? RemoteChatError.Unauthorized : response.StatusCode == 403 ? RemoteChatError.Forbidden : response.StatusCode == 429 ? RemoteChatError.RateLimited : response.StatusCode >= 500 ? RemoteChatError.Transport : RemoteChatError.InvalidResponse);
            EnsureSessionEpoch(expectedEpoch);
            validate?.Invoke(response.Body, session);
            return response.Body;
        }

        private void EnsureSessionEpoch(int expectedEpoch)
        {
            if (Volatile.Read(ref sessionEpoch) == expectedEpoch) return;
            throw new RemoteChatTransportException(RemoteChatError.Cancelled, "Chat session changed before the operation completed.", 0, "local_session_changed");
        }
        private RemoteChatTransportException CreateHttpException<T>(ChatTransportResponse<T> response, RemoteChatError error)
        {
            RemoteChatProblem problem = errorDecoder?.Decode(response.RawBody);
            Emit("http_error", "request", response.StatusCode, error, problem?.Code);
            return new RemoteChatTransportException(error, "Chat request failed with HTTP status " + response.StatusCode + ".", response.StatusCode, problem?.Code, response.RetryAfterSeconds ?? problem?.RetryAfterSeconds);
        }
        private void ValidateCapabilityCachePolicy(string cacheControl, int? ageSeconds)
        {
            string[] directives = (cacheControl ?? string.Empty).Split(',').Select(value => value.Trim()).ToArray();
            bool noStore = directives.Any(value => string.Equals(value, "no-store", StringComparison.OrdinalIgnoreCase));
            bool noCache = directives.Any(value => string.Equals(value, "no-cache", StringComparison.OrdinalIgnoreCase));
            bool zeroAge = directives.Any(value => string.Equals(value.Replace(" ", string.Empty), "max-age=0", StringComparison.OrdinalIgnoreCase));
            if (noStore && noCache && zeroAge && (!ageSeconds.HasValue || ageSeconds.Value == 0)) return;
            Emit("capability_cache_policy_invalid", "negotiate", error: RemoteChatError.Incompatible, serverCode: "capability_cache_policy_invalid");
            throw new RemoteChatTransportException(RemoteChatError.Incompatible, "Chat capabilities response may be stale and was rejected.", 0, "capability_cache_policy_invalid");
        }
        private void Emit(string code, string operation, int statusCode = 0, RemoteChatError error = RemoteChatError.None, string serverCode = null, int attempt = 0, int count = 0)
        { try { diagnostics?.Record(new ChatDiagnosticEvent { Code = code, Operation = operation, StatusCode = statusCode, Error = error, ServerCode = serverCode, Attempt = attempt, Count = count }); } catch { } }

        private async Task PersistPendingAsync(Func<Task> persist, string operation)
        {
            try { await persist(); }
            catch (ChatPendingJournalFullException exception)
            {
                Emit("local_queue_full", operation, error: RemoteChatError.LocalQueueFull, serverCode: "local_queue_full", count: exception.Capacity);
                throw new RemoteChatTransportException(RemoteChatError.LocalQueueFull, "The offline chat queue is full. Reconnect before trying a new operation.", 0, "local_queue_full", innerException: exception);
            }
            catch (ChatPendingJournalSizeException exception)
            {
                Emit("local_queue_full", operation, error: RemoteChatError.LocalQueueFull, serverCode: "local_queue_full", count: exception.MaxCharacters);
                throw new RemoteChatTransportException(RemoteChatError.LocalQueueFull, "The offline chat queue is full. Reconnect before trying a new operation.", 0, "local_queue_full", innerException: exception);
            }
            catch (Exception exception) when (exception is ChatPendingStoreException || exception is ChatProtectedStoreException)
            {
                throw LocalStorageFailure(operation, exception);
            }
        }

        private async Task<T> AccessPendingAsync<T>(Func<Task<T>> access, string operation)
        {
            try { return await access(); }
            catch (Exception exception) when (exception is ChatPendingStoreException || exception is ChatProtectedStoreException) { throw LocalStorageFailure(operation, exception); }
        }

        private async Task AccessPendingAsync(Func<Task> access, string operation)
        {
            try { await access(); }
            catch (Exception exception) when (exception is ChatPendingStoreException || exception is ChatProtectedStoreException) { throw LocalStorageFailure(operation, exception); }
        }

        private RemoteChatTransportException LocalStorageFailure(string operation, Exception exception)
        {
            Emit("local_storage_unavailable", operation, error: RemoteChatError.LocalStorageUnavailable, serverCode: "local_storage_unavailable");
            return new RemoteChatTransportException(RemoteChatError.LocalStorageUnavailable, "Protected chat recovery data is unavailable and was preserved.", 0, "local_storage_unavailable", innerException: exception);
        }

        private void EnsureReplayFresh(string enqueuedAtUtc, string operation)
        {
            if (!DateTimeOffset.TryParse(enqueuedAtUtc, out DateTimeOffset enqueued)) throw LocalStorageFailure(operation, new InvalidOperationException("Pending operation timestamp is invalid."));
            TimeSpan age = clock.UtcNow - enqueued;
            if (age <= effectiveReplayMaxAge && age >= -replayPolicy.AllowedClockSkew) return;
            Emit("local_operation_expired", operation, error: RemoteChatError.LocalOperationExpired, serverCode: "local_operation_expired");
            throw new RemoteChatTransportException(RemoteChatError.LocalOperationExpired, "This pending chat operation is outside the safe replay window and was preserved.", 0, "local_operation_expired");
        }

        private void EnsureRemoteOperationReady(string operation)
        {
            if (!requireCapabilityNegotiation) return;
            if (NegotiatedCapabilities != null && NegotiatedCapabilities.Server && capabilitiesNegotiatedAtUtc.HasValue)
            {
                TimeSpan age = clock.UtcNow - capabilitiesNegotiatedAtUtc.Value;
                if (age >= TimeSpan.Zero && age <= capabilityLeasePolicy.Duration) return;
                InvalidateCapabilities();
                Emit("capability_lease_expired", operation, error: RemoteChatError.Incompatible, serverCode: "capability_lease_expired");
                throw new RemoteChatTransportException(RemoteChatError.Incompatible, "Chat capabilities expired and must be negotiated again.", 0, "capability_lease_expired");
            }
            Emit("capability_negotiation_required", operation, error: RemoteChatError.Incompatible, serverCode: "capability_negotiation_required");
            throw new RemoteChatTransportException(RemoteChatError.Incompatible, "Chat capabilities must be negotiated before remote operations.", 0, "capability_negotiation_required");
        }
        private static string OpaquePathSegment(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || !string.Equals(value, value.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Opaque chat identifier must contain 1 to 256 non-padding characters.", parameterName);
            return Uri.EscapeDataString(value);
        }
        private static string ValidateCursor(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaxConversationCursorCharacters || !string.Equals(value, value.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Conversation cursor must contain 1 to 1024 non-padding characters.", parameterName);
            for (int index = 0; index < value.Length; index++) if (char.IsControl(value[index])) throw new ArgumentException("Conversation cursor cannot contain control characters.", parameterName);
            return value;
        }
        private void ValidateConversationPage(RemoteConversationPage page, int requestedLimit)
        {
            if (page?.Items == null || page.Items.Count > requestedLimit) throw InvalidReceipt("conversation_page_invalid");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RemoteConversation conversation in page.Items)
                if (conversation == null || !IsOpaqueIdentifier(conversation.ConversationId) || conversation.LastSequence < 0 || !ids.Add(conversation.ConversationId)) throw InvalidReceipt("conversation_page_invalid");
            if (page.NextCursor != null) try { ValidateCursor(page.NextCursor, nameof(page.NextCursor)); } catch (ArgumentException exception) { throw new RemoteChatTransportException(RemoteChatError.InvalidResponse, "Chat server returned an invalid conversation cursor.", 0, "invalid_conversation_cursor", innerException: exception); }
        }
        private void ValidateMessagePage(RemoteMessagePage page, string conversationId, long afterSequence, int requestedLimit = 100)
        {
            if (page?.Items == null || page.Items.Count > requestedLimit) throw InvalidReceipt("message_page_invalid");
            long maximum = afterSequence;
            var sequences = new HashSet<long>();
            foreach (RemoteChatMessage message in page.Items)
            {
                if (!IsValidMessage(message, conversationId) || message.Sequence <= afterSequence || !sequences.Add(message.Sequence)) throw InvalidReceipt("message_page_invalid");
                maximum = Math.Max(maximum, message.Sequence);
            }
            if (page.NextAfterSequence.HasValue && (page.Items.Count == 0 || page.NextAfterSequence.Value < maximum)) throw InvalidReceipt("message_page_invalid");
        }
        private bool IsValidMessage(RemoteChatMessage message, string conversationId)
        {
            int maxBody = NegotiatedCapabilities?.Limits?.BodyMaxCharacters ?? 4000;
            return message != null && IsOpaqueIdentifier(message.MessageId) && string.Equals(message.ConversationId, conversationId, StringComparison.Ordinal) && IsOpaqueIdentifier(message.ClientRequestId) &&
                message.Sequence > 0 && ChatSessionSecurity.IsValidPlayerId(message.SenderId) && message.OriginalBody != null && message.OriginalBody.Length <= maxBody;
        }
        private static bool IsOpaqueIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || !string.Equals(value, value.Trim(), StringComparison.Ordinal)) return false;
            for (int index = 0; index < value.Length; index++) if (char.IsControl(value[index])) return false;
            return true;
        }
        private static void ValidateLocale(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 2 || value.Length > 35 || value[0] == '-' || value[value.Length - 1] == '-' || value.Contains("--")) throw new ArgumentException("Locale must be a bounded BCP 47 language tag.", parameterName);
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool asciiAlphaNumeric = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' || character >= '0' && character <= '9';
                if (!asciiAlphaNumeric && character != '-') throw new ArgumentException("Locale must be a bounded BCP 47 language tag.", parameterName);
            }
        }
        private static void ValidateModelVersion(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 128) throw new ArgumentException("Translation model version must contain 1 to 128 safe characters.", parameterName);
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool asciiAlphaNumeric = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' || character >= '0' && character <= '9';
                if (!asciiAlphaNumeric && character != '-' && character != '_' && character != '.') throw new ArgumentException("Translation model version must contain 1 to 128 safe characters.", parameterName);
            }
        }
        private static bool IsMatchingTranslation(MessageTranslation value, string messageId, string targetLocale, string modelVersion)
        {
            if (value == null || !string.Equals(value.MessageId, messageId, StringComparison.Ordinal) || !string.Equals(value.TargetLocale, targetLocale, StringComparison.OrdinalIgnoreCase) || !string.Equals(value.ModelVersion, modelVersion, StringComparison.Ordinal)) return false;
            bool completed = string.Equals(value.Status, "completed", StringComparison.OrdinalIgnoreCase);
            bool pending = string.Equals(value.Status, "pending", StringComparison.OrdinalIgnoreCase);
            if (!completed && !pending) return false;
            if (completed && (value.TranslatedText == null || value.TranslatedText.Length > 16000)) return false;
            if (!string.IsNullOrEmpty(value.SourceLocale)) try { ValidateLocale(value.SourceLocale, nameof(value.SourceLocale)); } catch (ArgumentException) { return false; }
            return true;
        }
        private async Task<ChatSession> RequireSession(CancellationToken ct)
        {
            ChatSession session = await sessions.GetSessionAsync(ct);
            EnsureValidSession(session);
            return session;
        }
        private void EnsureValidSession(ChatSession session)
        {
            if (session == null || !ChatSessionSecurity.IsValidPlayerId(session.PlayerId) || !ChatSessionSecurity.IsValidBearerToken(session.AccessToken))
            {
                ConnectionState = RemoteChatConnectionState.AuthenticationRequired;
                throw new RemoteChatTransportException(RemoteChatError.Unauthorized, "A valid chat session is required.");
            }
            if (expectedPlayerId != null && !string.Equals(session.PlayerId, expectedPlayerId, StringComparison.Ordinal))
            {
                ConnectionState = RemoteChatConnectionState.Offline;
                ClearVolatileState();
                Emit("local_account_mismatch", "session", error: RemoteChatError.LocalAccountMismatch, serverCode: "local_account_mismatch");
                throw new RemoteChatTransportException(RemoteChatError.LocalAccountMismatch, "The chat session does not match the active local storage partition.", 0, "local_account_mismatch");
            }
        }
        private Task ConnectRealtimeOnceAsync(ChatSession session, CancellationToken ct) => realtime.ConnectAsync(session, HandleRealtimeAsync, ct);

        private async Task<PendingChatSend> FindPendingAsync(string clientRequestId, CancellationToken ct)
        {
            return (await AccessPendingAsync(() => pendingSends.LoadAsync(ct), "send_message")).FirstOrDefault(item => string.Equals(item.ClientRequestId, clientRequestId, StringComparison.Ordinal));
        }

        private async Task<RemoteSendResult> SendPendingAsync(PendingChatSend pending, CancellationToken ct, ChatSession operationSession = null, int? operationEpoch = null)
        {
            OpaquePathSegment(pending?.ConversationId, nameof(pending.ConversationId));
            ValidateClientRequestId(pending.ClientRequestId, nameof(pending.ClientRequestId));
            if (string.IsNullOrWhiteSpace(pending.Body) || pending.Body.Length > 4000 || !string.Equals(pending.Body, pending.Body.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Pending message body is invalid.", nameof(pending));
            pending.AttemptCount++;
            await PersistPendingAsync(() => pendingSends.SaveAsync(pending, ct), "send_message");
            if (operationEpoch.HasValue) EnsureSessionEpoch(operationEpoch.Value);
            Emit("outbox_attempt", "send", attempt: pending.AttemptCount);
            try
            {
                var payload = new RemoteSendMessageRequest { ClientRequestId = pending.ClientRequestId, Body = pending.Body, ClientCreatedAt = pending.ClientCreatedAt };
                RemoteSendResult result = await PostAsync<RemoteSendResult>("/chat/v1/conversations/" + OpaquePathSegment(pending.ConversationId, nameof(pending.ConversationId)) + "/messages", payload, ct,
                    (receipt, requestSession) => { if (!IsMatchingSendReceipt(receipt, pending, requestSession.PlayerId)) throw InvalidReceipt("message_receipt_mismatch"); }, operationSession, operationEpoch);
                requests[pending.ClientRequestId] = result.Message;
                Merge(pending.ConversationId, new[] { result.Message });
                await AccessPendingAsync(() => pendingSends.RemoveAsync(pending.ClientRequestId, ct), "send_message");
                Emit("outbox_acknowledged", "send", attempt: pending.AttemptCount);
                return result;
            }
            catch (RemoteChatTransportException exception) when (IsTerminalClientRejection(exception))
            {
                await AccessPendingAsync(() => pendingSends.RemoveAsync(pending.ClientRequestId, ct), "send_message");
                throw;
            }
        }
        private async Task<RemoteCreateConversationResult> CreatePendingConversationAsync(PendingChatConversationCreation pending, CancellationToken ct, ChatSession operationSession = null, int? operationEpoch = null)
        {
            if (pending?.Request == null) throw new ArgumentException("Pending conversation request is required.", nameof(pending));
            ValidateConversationEnvelope(pending.Request);
            pending.AttemptCount++;
            await PersistPendingAsync(() => pendingConversations.SaveAsync(pending, ct), "create_conversation");
            if (operationEpoch.HasValue) EnsureSessionEpoch(operationEpoch.Value);
            try
            {
                RemoteCreateConversationResult result = await PostAsync<RemoteCreateConversationResult>("/chat/v1/conversations", pending.Request, ct, initialSession: operationSession, initialEpoch: operationEpoch);
                if (result?.Conversation == null || result.Inbox == null || !string.Equals(result.ClientRequestId, pending.Request.ClientRequestId, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(result.Conversation.ConversationId) || !string.Equals(result.Inbox.ConversationId, result.Conversation.ConversationId, StringComparison.Ordinal) || result.Conversation.LastSequence < 0)
                    throw InvalidReceipt("conversation_receipt_mismatch");
                await AccessPendingAsync(() => pendingConversations.RemoveAsync(pending.Request.ClientRequestId, ct), "create_conversation");
                return result;
            }
            catch (RemoteChatTransportException exception) when (IsTerminalClientRejection(exception))
            { await AccessPendingAsync(() => pendingConversations.RemoveAsync(pending.Request.ClientRequestId, ct), "create_conversation"); throw; }
        }
        private static RemoteCreateConversationRequest Normalize(RemoteCreateConversationRequest request) => new RemoteCreateConversationRequest
        {
            ChannelType = request.ChannelType?.Trim(), GameServerId = request.GameServerId?.Trim(), WorldId = request.WorldId?.Trim(), AudienceKey = request.AudienceKey?.Trim(), Title = request.Title?.Trim(), ClientRequestId = request.ClientRequestId.Trim(),
            ParticipantIds = (request.ParticipantIds ?? new List<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList()
        };
        private static bool Same(RemoteCreateConversationRequest left, RemoteCreateConversationRequest right) =>
            string.Equals(left?.ChannelType, right?.ChannelType, StringComparison.Ordinal) && string.Equals(left?.GameServerId, right?.GameServerId, StringComparison.Ordinal) && string.Equals(left?.WorldId, right?.WorldId, StringComparison.Ordinal) && string.Equals(left?.AudienceKey, right?.AudienceKey, StringComparison.Ordinal) && string.Equals(left?.Title, right?.Title, StringComparison.Ordinal) && (left?.ParticipantIds ?? new List<string>()).SequenceEqual(right?.ParticipantIds ?? new List<string>(), StringComparer.Ordinal);
        private void ValidateSendCapabilities(string body)
        {
            if (NegotiatedCapabilities?.Limits == null) return;
            ValidateFeature(!string.IsNullOrWhiteSpace(body), "message_empty");
            ValidateFeature(body.Length <= NegotiatedCapabilities.Limits.BodyMaxCharacters, "body_too_long");
        }
        private void ValidateConversationCapabilities(RemoteCreateConversationRequest request)
        {
            if (NegotiatedCapabilities == null) return;
            ValidateFeature(NegotiatedCapabilities.Channels.Any(channel => string.Equals(channel, request.ChannelType, StringComparison.OrdinalIgnoreCase)), "channel_not_supported");
            if (string.Equals(request.ChannelType, "Private", StringComparison.OrdinalIgnoreCase) && NegotiatedCapabilities.Limits != null)
                ValidateFeature(request.ParticipantIds.Count <= NegotiatedCapabilities.Limits.MaxPrivateRecipients, "too_many_private_recipients");
        }
        private static void ValidateConversationEnvelope(RemoteCreateConversationRequest request)
        {
            ValidateClientRequestId(request.ClientRequestId, nameof(request.ClientRequestId));
            ValidateBoundedText(request.ChannelType, 32, nameof(request.ChannelType));
            if (!new[] { "Alliance", "Server", "Private", "Leaders" }.Any(value => string.Equals(value, request.ChannelType, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Conversation channel is invalid.", nameof(request.ChannelType));
            ValidateOptionalBoundedText(request.GameServerId, 64, nameof(request.GameServerId));
            ValidateOptionalBoundedText(request.WorldId, 64, nameof(request.WorldId));
            ValidateOptionalBoundedText(request.AudienceKey, 256, nameof(request.AudienceKey));
            ValidateOptionalBoundedText(request.Title, 256, nameof(request.Title));
            if (request.ParticipantIds == null || request.ParticipantIds.Count > 100) throw new ArgumentException("Conversation participant count is invalid.", nameof(request.ParticipantIds));
            foreach (string participantId in request.ParticipantIds) if (!IsOpaqueIdentifier(participantId)) throw new ArgumentException("Conversation participant identifier is invalid.", nameof(request.ParticipantIds));
            if (string.Equals(request.ChannelType, "Private", StringComparison.OrdinalIgnoreCase) && request.ParticipantIds.Count == 0) throw new ArgumentException("Private conversation requires at least one participant.", nameof(request.ParticipantIds));
        }
        private static void ValidateClientRequestId(string value, string parameterName)
        { if (!IsOpaqueIdentifier(value)) throw new ArgumentException("Client request identifier must contain 1 to 256 non-padding characters.", parameterName); }
        private static void ValidateBoundedText(string value, int maxCharacters, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > maxCharacters || !string.Equals(value, value.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Chat text field is invalid or exceeds its limit.", parameterName);
            for (int index = 0; index < value.Length; index++) if (char.IsControl(value[index])) throw new ArgumentException("Chat text field contains a control character.", parameterName);
        }
        private static void ValidateOptionalBoundedText(string value, int maxCharacters, string parameterName)
        { if (value != null) ValidateBoundedText(value, maxCharacters, parameterName); }
        private static void ValidateFeature(bool condition, string code)
        { if (!condition) throw new RemoteChatTransportException(RemoteChatError.Incompatible, "Chat action is not supported by negotiated capabilities.", 0, code); }
        private async Task<RemoteModerationReport> SubmitPendingReportAsync(PendingModerationReportRequest pending, CancellationToken ct, ChatSession operationSession = null, int? operationEpoch = null)
        {
            OpaquePathSegment(pending?.MessageId, nameof(pending.MessageId));
            ValidateClientRequestId(pending.ClientRequestId, nameof(pending.ClientRequestId));
            ValidateBoundedText(pending.Category, 64, nameof(pending.Category));
            pending.AttemptCount++;
            await PersistPendingAsync(() => pendingReports.SaveAsync(pending, ct), "report_message");
            if (operationEpoch.HasValue) EnsureSessionEpoch(operationEpoch.Value);
            try
            {
                RemoteModerationReport result = await PostAsync<RemoteModerationReport>("/chat/v1/messages/" + OpaquePathSegment(pending.MessageId, nameof(pending.MessageId)) + "/report", new RemoteReportMessageRequest { ClientRequestId = pending.ClientRequestId, Category = pending.Category }, ct, initialSession: operationSession, initialEpoch: operationEpoch);
                if (result == null || string.IsNullOrWhiteSpace(result.ReportId) || string.IsNullOrWhiteSpace(result.Status) || !string.Equals(result.MessageId, pending.MessageId, StringComparison.Ordinal) || !string.Equals(result.ClientRequestId, pending.ClientRequestId, StringComparison.Ordinal))
                    throw InvalidReceipt("moderation_receipt_mismatch");
                await AccessPendingAsync(() => pendingReports.RemoveAsync(pending.ClientRequestId, ct), "report_message");
                return result;
            }
            catch (RemoteChatTransportException exception) when (IsTerminalClientRejection(exception))
            { await AccessPendingAsync(() => pendingReports.RemoveAsync(pending.ClientRequestId, ct), "report_message"); throw; }
        }
        private async Task<object> SubmitPendingReadAsync(PendingReadCursor pending, CancellationToken ct, ChatSession operationSession = null, int? operationEpoch = null)
        {
            OpaquePathSegment(pending?.ConversationId, nameof(pending.ConversationId));
            if (pending.Sequence < 0) throw new ArgumentException("Pending read sequence is invalid.", nameof(pending));
            pending.AttemptCount++;
            await PersistPendingAsync(() => pendingReads.SaveMaximumAsync(pending, ct), "mark_read");
            if (operationEpoch.HasValue) EnsureSessionEpoch(operationEpoch.Value);
            try
            {
                RemoteInboxEntry result = await PostAsync<RemoteInboxEntry>("/chat/v1/conversations/" + OpaquePathSegment(pending.ConversationId, nameof(pending.ConversationId)) + "/read", new RemoteMarkReadRequest { Sequence = pending.Sequence }, ct, initialSession: operationSession, initialEpoch: operationEpoch);
                if (result == null || !string.Equals(result.ConversationId, pending.ConversationId, StringComparison.Ordinal) || result.ReadCursorSequence < pending.Sequence || result.UnreadCount < 0 || result.MentionCount < 0)
                    throw InvalidReceipt("read_receipt_mismatch");
                await AccessPendingAsync(() => pendingReads.RemoveThroughAsync(pending.ConversationId, pending.Sequence, ct), "mark_read");
                return result;
            }
            catch (RemoteChatTransportException exception) when (IsTerminalClientRejection(exception))
            { await AccessPendingAsync(() => pendingReads.RemoveThroughAsync(pending.ConversationId, long.MaxValue, ct), "mark_read"); throw; }
        }

        private static bool IsMatchingSendReceipt(RemoteSendResult result, PendingChatSend pending, string expectedSenderPlayerId)
        {
            RemoteChatMessage message = result?.Message;
            return message != null && !string.IsNullOrWhiteSpace(message.MessageId) && message.Sequence > 0 && result.ServerSequence == message.Sequence &&
                string.Equals(message.ConversationId, pending.ConversationId, StringComparison.Ordinal) && string.Equals(message.ClientRequestId, pending.ClientRequestId, StringComparison.Ordinal) &&
                string.Equals(message.OriginalBody, pending.Body, StringComparison.Ordinal) && string.Equals(message.SenderId, expectedSenderPlayerId, StringComparison.Ordinal);
        }
        private static RemoteChatTransportException InvalidReceipt(string code) => new RemoteChatTransportException(RemoteChatError.InvalidResponse, "Chat server returned a receipt that does not match the pending operation.", 0, code);
        private static bool IsTerminalClientRejection(RemoteChatTransportException exception) => exception.Error == RemoteChatError.Forbidden || exception.Error == RemoteChatError.InvalidResponse && exception.StatusCode >= 400 && exception.StatusCode < 500;

        private sealed class TaskChatDelay : IChatDelay
        {
            public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken) => Task.Delay(duration, cancellationToken);
        }
        private sealed class MemoryPendingSendStore : IChatPendingSendStore
        {
            private readonly Dictionary<string, PendingChatSend> items = new Dictionary<string, PendingChatSend>(StringComparer.Ordinal);
            public Task<IReadOnlyList<PendingChatSend>> LoadAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult((IReadOnlyList<PendingChatSend>)items.Values.ToList()); }
            public Task SaveAsync(PendingChatSend pending, CancellationToken ct) { ct.ThrowIfCancellationRequested(); items[pending.ClientRequestId] = pending; return Task.CompletedTask; }
            public Task RemoveAsync(string id, CancellationToken ct) { ct.ThrowIfCancellationRequested(); items.Remove(id); return Task.CompletedTask; }
        }
        private sealed class MemoryPendingConversationStore : IChatPendingConversationStore
        {
            private readonly Dictionary<string, PendingChatConversationCreation> items = new Dictionary<string, PendingChatConversationCreation>(StringComparer.Ordinal);
            public Task<IReadOnlyList<PendingChatConversationCreation>> LoadAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult((IReadOnlyList<PendingChatConversationCreation>)items.Values.ToList()); }
            public Task SaveAsync(PendingChatConversationCreation pending, CancellationToken ct) { ct.ThrowIfCancellationRequested(); items[pending.Request.ClientRequestId] = pending; return Task.CompletedTask; }
            public Task RemoveAsync(string id, CancellationToken ct) { ct.ThrowIfCancellationRequested(); items.Remove(id); return Task.CompletedTask; }
        }
        private sealed class MemoryPendingReportStore : IChatPendingModerationReportStore
        {
            private readonly Dictionary<string, PendingModerationReportRequest> items = new Dictionary<string, PendingModerationReportRequest>(StringComparer.Ordinal);
            public Task<IReadOnlyList<PendingModerationReportRequest>> LoadAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult((IReadOnlyList<PendingModerationReportRequest>)items.Values.ToList()); }
            public Task SaveAsync(PendingModerationReportRequest pending, CancellationToken ct) { ct.ThrowIfCancellationRequested(); items[pending.ClientRequestId] = pending; return Task.CompletedTask; }
            public Task RemoveAsync(string id, CancellationToken ct) { ct.ThrowIfCancellationRequested(); items.Remove(id); return Task.CompletedTask; }
        }
        private sealed class MemoryPendingReadStore : IChatPendingReadStore
        {
            private readonly Dictionary<string, PendingReadCursor> items = new Dictionary<string, PendingReadCursor>(StringComparer.Ordinal);
            public Task<IReadOnlyList<PendingReadCursor>> LoadAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult((IReadOnlyList<PendingReadCursor>)items.Values.ToList()); }
            public Task SaveMaximumAsync(PendingReadCursor pending, CancellationToken ct) { ct.ThrowIfCancellationRequested(); if (!items.TryGetValue(pending.ConversationId, out PendingReadCursor current) || pending.Sequence > current.Sequence) items[pending.ConversationId] = pending; else if (pending.Sequence == current.Sequence) current.AttemptCount = Math.Max(current.AttemptCount, pending.AttemptCount); return Task.CompletedTask; }
            public Task RemoveThroughAsync(string id, long sequence, CancellationToken ct) { ct.ThrowIfCancellationRequested(); if (items.TryGetValue(id, out PendingReadCursor current) && current.Sequence <= sequence) items.Remove(id); return Task.CompletedTask; }
        }
    }
}
