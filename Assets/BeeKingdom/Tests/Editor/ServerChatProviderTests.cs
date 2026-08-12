using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using BeeKingdom.Gameplay.Communication;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ServerChatProviderTests
    {
        [Test]
        public async Task SendRetryAndRealtimeRestDuplicateAreIdempotent()
        {
            var transport = new FakeRest();
            var realtime = new FakeRealtime();
            var provider = NewProvider(transport, realtime);
            await provider.ConnectAsync(CancellationToken.None);
            RemoteSendResult first = await provider.SendAsync("c1", "hello", "request-1", CancellationToken.None);
            RemoteSendResult retry = await provider.SendAsync("c1", "hello", "request-1", CancellationToken.None);
            await realtime.Emit(new RemoteChatEvent { ConversationId = "c1", Message = first.Message });
            IReadOnlyList<RemoteChatMessage> snapshot = await provider.ReconcileAsync("c1", 0, CancellationToken.None);
            Assert.That(retry.Deduplicated, Is.True);
            Assert.That(transport.PostCount, Is.EqualTo(1));
            Assert.That(snapshot, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task ReconciliationOrdersEventsAndFillsSequenceGap()
        {
            var transport = new FakeRest();
            transport.Page.Items.Add(Message(3, "r3"));
            transport.Page.Items.Add(Message(1, "r1"));
            transport.Page.Items.Add(Message(2, "r2"));
            IReadOnlyList<RemoteChatMessage> result = await NewProvider(transport).ReconcileAsync("c1", 0, CancellationToken.None);
            Assert.That(result[0].Sequence, Is.EqualTo(1));
            Assert.That(result[2].Sequence, Is.EqualTo(3));
        }

        [Test]
        public async Task MissingRealtimeFallsBackToPolling()
        {
            var provider = NewProvider(new FakeRest());
            await provider.ConnectAsync(CancellationToken.None);
            Assert.That(provider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.Polling));
        }

        [Test]
        public async Task EnsureRealtimeSubscriptionsJoinsEachConversationOnceAndSkipsRepeats()
        {
            var realtime = new FakeRealtime();
            var provider = NewProvider(new FakeRest(), realtime);
            await provider.ConnectAsync(CancellationToken.None);
            Assert.That(provider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.Realtime));

            await provider.EnsureRealtimeSubscriptionsAsync(new[] { "c1", "c2" }, CancellationToken.None);
            await provider.EnsureRealtimeSubscriptionsAsync(new[] { "c2", "c3" }, CancellationToken.None);

            Assert.That(realtime.JoinedConversationIds, Is.EqualTo(new[] { "c1", "c2", "c3" }));
        }

        [Test]
        public async Task EnsureRealtimeSubscriptionsDoesNothingWhenPolling()
        {
            var realtime = new FakeRealtime();
            var provider = NewProvider(new FakeRest { Capabilities = ValidCapabilities(realtime: false) }, realtime);
            await provider.ConnectAsync(CancellationToken.None);
            Assert.That(provider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.Polling));

            await provider.EnsureRealtimeSubscriptionsAsync(new[] { "c1" }, CancellationToken.None);

            Assert.That(realtime.JoinedConversationIds, Is.Empty);
        }

        [Test]
        public async Task DisconnectResetsJoinedConversationsSoReconnectRejoinsThem()
        {
            var realtime = new FakeRealtime();
            var provider = NewProvider(new FakeRest(), realtime);
            await provider.ConnectAsync(CancellationToken.None);
            await provider.EnsureRealtimeSubscriptionsAsync(new[] { "c1" }, CancellationToken.None);
            Assert.That(realtime.JoinedConversationIds, Is.EqualTo(new[] { "c1" }));

            await provider.DisconnectAsync(CancellationToken.None);
            await provider.ConnectAsync(CancellationToken.None);
            await provider.EnsureRealtimeSubscriptionsAsync(new[] { "c1" }, CancellationToken.None);

            Assert.That(realtime.JoinedConversationIds, Is.EqualTo(new[] { "c1", "c1" }));
        }

        [Test]
        public void ExpiredSessionIsExplicit()
        {
            var provider = new ServerChatProvider(new FakeRest { StatusCode = 401 }, new FakeSession());
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.GetCapabilitiesAsync(CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Unauthorized));
            Assert.That(provider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.AuthenticationRequired));
        }

        [Test]
        public async Task TranslationCacheUsesMessageLocaleAndModelAndOriginalRemainsAvailable()
        {
            var transport = new FakeRest();
            var provider = NewProvider(transport);
            MessageTranslation first = await provider.TranslateAsync("m1", "fr-CA", "v1", CancellationToken.None);
            MessageTranslation cached = await provider.TranslateAsync("m1", "fr-CA", "v1", CancellationToken.None);
            await provider.TranslateAsync("m1", "en-US", "v1", CancellationToken.None);
            Assert.That(cached, Is.SameAs(first));
            Assert.That(transport.TranslationPostCount, Is.EqualTo(2));
            Assert.That(provider.OriginalText(Message(1, "x")), Is.EqualTo("body"));
        }

        [Test]
        public void CancellationPropagatesWithoutNetworkCall()
        {
            var transport = new FakeRest();
            var source = new CancellationAwareSession();
            var provider = new ServerChatProvider(transport, source);
            var cts = new CancellationTokenSource(); cts.Cancel();
            Assert.ThrowsAsync<OperationCanceledException>(async () => await provider.ListConversationsAsync(20, cts.Token));
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public async Task PollingRetriesNetworkLossThenRecoversWithinBound()
        {
            var transport = new FakeRest { FailuresRemaining = 2 };
            transport.Page.Items.Add(Message(1, "r1"));
            var delay = new FakeDelay();
            var provider = new ServerChatProvider(transport, new FakeSession(), null, new ChatRetryPolicy(3, TimeSpan.Zero), delay);
            IReadOnlyList<RemoteChatMessage> result = await provider.PollWithRetryAsync("c1", 0, CancellationToken.None);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(transport.CallCount, Is.EqualTo(3));
            Assert.That(delay.Count, Is.EqualTo(2));
        }

        [Test]
        public void PollingStopsAtRetryLimit()
        {
            var transport = new FakeRest { FailuresRemaining = 10 };
            var provider = new ServerChatProvider(transport, new FakeSession(), null, new ChatRetryPolicy(2, TimeSpan.Zero), new FakeDelay());
            Assert.ThrowsAsync<InvalidOperationException>(async () => await provider.PollWithRetryAsync("c1", 0, CancellationToken.None));
            Assert.That(transport.CallCount, Is.EqualTo(2));
            Assert.That(provider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.Offline));
        }

        [Test]
        public void PollingFailsFastOnInvalidResponseWithoutRetry()
        {
            var transport = new FakeRest();
            transport.Page.Items.Add(new RemoteChatMessage { MessageId = "m1", ConversationId = "other", Sequence = 1, ClientRequestId = "r1", SenderId = "p1", OriginalBody = "body" });
            var delay = new FakeDelay();
            var provider = new ServerChatProvider(transport, new FakeSession(), retryPolicy: new ChatRetryPolicy(3), delay: delay);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.PollWithRetryAsync("c1", 0, CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.InvalidResponse));
            Assert.That(transport.CallCount, Is.EqualTo(1));
            Assert.That(delay.Count, Is.Zero);
        }

        [Test]
        public void PollingRateLimitHonorsBoundedRetryAfterAndReturnsTypedFailure()
        {
            var transport = new FakeRest { StatusCode = 429, RetryAfterSeconds = 600 };
            var delay = new FakeDelay();
            var provider = new ServerChatProvider(transport, new FakeSession(), retryPolicy: new ChatRetryPolicy(2, TimeSpan.FromSeconds(2)), delay: delay);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.PollWithRetryAsync("c1", 0, CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.RateLimited));
            Assert.That(transport.CallCount, Is.EqualTo(2));
            Assert.That(delay.Durations.Single(), Is.EqualTo(TimeSpan.FromSeconds(300)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatRetryPolicy(3, TimeSpan.FromSeconds(31)));
        }

        [Test]
        public async Task ConversationCreationRequiresAndForwardsStableRequestId()
        {
            var transport = new FakeRest();
            var provider = NewProvider(transport);
            var request = new RemoteCreateConversationRequest { ChannelType = "Private", ClientRequestId = "conversation-1", ParticipantIds = new List<string> { "p2" } };
            RemoteCreateConversationResult result = await provider.CreateConversationAsync(request, CancellationToken.None);
            Assert.That(result.Conversation.ConversationId, Is.EqualTo("c-created"));
            Assert.That(transport.LastCreateRequest.ClientRequestId, Is.EqualTo("conversation-1"));
        }

        [Test]
        public void UnityCodecUsesServerCamelCaseAndMapsOriginalMessage()
        {
            var codec = new UnityChatJsonCodec(new SystemTextJsonBackend());
            string payload = codec.Serialize(new RemoteSendMessageRequest { ClientRequestId = "r1", Body = "bonjour", ClientCreatedAt = "2026-07-21T12:00:00Z" });
            RemoteSendResult result = codec.Deserialize<RemoteSendResult>("{\"message\":{\"messageId\":\"m1\",\"conversationId\":\"c1\",\"sequence\":7,\"clientRequestId\":\"r1\",\"senderPlayerId\":\"p1\",\"body\":\"original\",\"acceptedAtUtc\":\"2026-07-21T12:00:01Z\"},\"deduplicated\":false,\"serverSequence\":7}");
            Assert.That(payload, Does.Contain("\"clientRequestId\":\"r1\""));
            Assert.That(payload, Does.Not.Contain("ClientRequestId"));
            Assert.That(result.Message.OriginalBody, Is.EqualTo("original"));
            Assert.That(result.Message.Sequence, Is.EqualTo(7));
        }

        [Test]
        public void UnityCodecRejectsUnknownWireTypes()
        {
            var codec = new UnityChatJsonCodec(new SystemTextJsonBackend());
            Assert.Throws<NotSupportedException>(() => codec.Serialize(new object()));
            Assert.Throws<NotSupportedException>(() => codec.Deserialize<int>("{}"));
        }

        [Test]
        public void CreationCodecMapsServerInboxObject()
        {
            var codec = new UnityChatJsonCodec(new SystemTextJsonBackend());
            RemoteCreateConversationResult result = codec.Deserialize<RemoteCreateConversationResult>("{\"conversation\":{\"conversationId\":\"c1\",\"title\":\"Private\",\"lastSequence\":4},\"inbox\":{\"conversationId\":\"c1\",\"readCursorSequence\":2,\"unreadCount\":2,\"mentionCount\":1,\"isMuted\":false,\"isArchived\":false}}");
            Assert.That(result.Inbox.ConversationId, Is.EqualTo("c1"));
            Assert.That(result.Inbox.UnreadCount, Is.EqualTo(2));
            Assert.That(result.Conversation.LastSequence, Is.EqualTo(4));
        }

        [Test]
        public void SynchronizerCancelsAndDisconnectsWhenPanelCloses()
        {
            var transport = new FakeRest();
            transport.Page.Items.Add(Message(1, "r1"));
            var realtime = new FakeRealtime();
            var provider = NewProvider(transport, realtime);
            var delay = new CancellingDelay();
            var synchronizer = new ChatConversationSynchronizer(provider, delay, new ChatSynchronizationPolicy(TimeSpan.Zero, 1));
            var cts = new CancellationTokenSource(); delay.Source = cts;
            int snapshots = 0;
            Assert.ThrowsAsync<OperationCanceledException>(async () => await synchronizer.RunAsync("c1", items => { snapshots++; return Task.CompletedTask; }, cts.Token));
            Assert.That(snapshots, Is.EqualTo(1));
            Assert.That(realtime.DisconnectCount, Is.EqualTo(1));
            Assert.That(provider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.Offline));
        }

        [Test]
        public async Task PendingSendSurvivesProviderRestartAndKeepsOriginalIdentity()
        {
            var store = new FakePendingStore();
            var offline = new FakeRest { FailuresRemaining = 1 };
            var firstProvider = new ServerChatProvider(offline, new FakeSession(), null, null, null, store);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await firstProvider.SendAsync("c1", "durable", "stable-1", CancellationToken.None));
            Assert.That((await store.LoadAsync(CancellationToken.None)), Has.Count.EqualTo(1));
            string createdAt = store.Items[0].ClientCreatedAt;

            var online = new FakeRest();
            var restartedProvider = new ServerChatProvider(online, new FakeSession(), null, null, null, store);
            IReadOnlyList<RemoteSendResult> results = await restartedProvider.RetryPendingAsync(CancellationToken.None);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(online.LastSendRequest.ClientRequestId, Is.EqualTo("stable-1"));
            Assert.That(online.LastSendRequest.ClientCreatedAt, Is.EqualTo(createdAt));
            Assert.That((await store.LoadAsync(CancellationToken.None)), Is.Empty);
        }

        [Test]
        public async Task PendingRequestIdCollisionIsRejectedBeforeNetwork()
        {
            var store = new FakePendingStore();
            await store.SaveAsync(new PendingChatSend { ConversationId = "c1", Body = "original", ClientRequestId = "same", ClientCreatedAt = "2026-07-21T12:00:00Z" }, CancellationToken.None);
            var transport = new FakeRest();
            var provider = new ServerChatProvider(transport, new FakeSession(), null, null, null, store);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await provider.SendAsync("c1", "different", "same", CancellationToken.None));
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public async Task ExpiredSessionKeepsPendingSendForLaterAuthentication()
        {
            var store = new FakePendingStore();
            var provider = new ServerChatProvider(new FakeRest { StatusCode = 401 }, new FakeSession(), null, null, null, store);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "later", "auth-1", CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Unauthorized));
            Assert.That((await store.LoadAsync(CancellationToken.None)), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task StatusZeroIsNetworkFailureAndKeepsPendingSend()
        {
            var store = new FakePendingStore();
            var provider = new ServerChatProvider(new FakeRest { StatusCode = 0 }, new FakeSession(), null, null, null, store);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "offline", "network-0", CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Transport));
            Assert.That((await store.LoadAsync(CancellationToken.None)), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task PublicCapabilitiesDoNotRequireOrSendPlayerSession()
        {
            var transport = new FakeRest();
            var sessions = new CountingSession();
            var provider = new ServerChatProvider(transport, sessions);
            await provider.GetCapabilitiesAsync(CancellationToken.None);
            Assert.That(sessions.GetCount, Is.Zero);
            Assert.That(transport.LastBearerToken, Is.Null);
        }

        [Test]
        public async Task UnauthorizedRequestRefreshesExactlyOnceThenSucceeds()
        {
            var transport = new FakeRest(); transport.ResponseStatuses.Enqueue(401); transport.ResponseStatuses.Enqueue(200);
            var sessions = new RefreshableSession();
            var provider = new ServerChatProvider(transport, sessions);
            await provider.ListConversationsAsync(20, CancellationToken.None);
            Assert.That(sessions.RefreshCount, Is.EqualTo(1));
            Assert.That(transport.CallCount, Is.EqualTo(2));
            Assert.That(transport.LastBearerToken, Is.EqualTo("refreshed-token"));
        }

        [Test]
        public void SecondUnauthorizedResponseStopsWithoutRefreshLoop()
        {
            var transport = new FakeRest(); transport.ResponseStatuses.Enqueue(401); transport.ResponseStatuses.Enqueue(401); transport.ResponseStatuses.Enqueue(200);
            var sessions = new RefreshableSession();
            var provider = new ServerChatProvider(transport, sessions);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ListConversationsAsync(20, CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Unauthorized));
            Assert.That(sessions.RefreshCount, Is.EqualTo(1));
            Assert.That(transport.CallCount, Is.EqualTo(2));
            Assert.That(provider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.AuthenticationRequired));
        }

        [Test]
        public void CancellationDuringRefreshPreventsSecondNetworkRequest()
        {
            var transport = new FakeRest(); transport.ResponseStatuses.Enqueue(401); transport.ResponseStatuses.Enqueue(200);
            var cts = new CancellationTokenSource();
            var sessions = new CancellingRefreshSession { Source = cts };
            var provider = new ServerChatProvider(transport, sessions);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await provider.ListConversationsAsync(20, cts.Token));
            Assert.That(sessions.RefreshCount, Is.EqualTo(1));
            Assert.That(transport.CallCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ConflictCarriesStableServerCodeAndRemovesDefinitiveOutboxEntry()
        {
            var store = new FakePendingStore();
            var transport = new FakeRest { StatusCode = 409, RawErrorBody = "{\"code\":\"chat.idempotency_conflict\",\"message\":\"ignored\"}" };
            var codec = new UnityChatJsonCodec(new SystemTextJsonBackend());
            var provider = new ServerChatProvider(transport, new FakeSession(), null, null, null, store, codec);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "payload", "conflict-1", CancellationToken.None));
            Assert.That(error.StatusCode, Is.EqualTo(409));
            Assert.That(error.ServerCode, Is.EqualTo("chat.idempotency_conflict"));
            Assert.That((await store.LoadAsync(CancellationToken.None)), Is.Empty);
        }

        [Test]
        public async Task RateLimitCarriesRetryAfterAndPreservesOutbox()
        {
            var store = new FakePendingStore();
            var transport = new FakeRest { StatusCode = 429, RawErrorBody = "{\"code\":\"chat.rate_limited\",\"retryAfterSeconds\":12}", RetryAfterSeconds = 9 };
            var codec = new UnityChatJsonCodec(new SystemTextJsonBackend());
            var provider = new ServerChatProvider(transport, new FakeSession(), null, null, null, store, codec);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "later", "rate-1", CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.RateLimited));
            Assert.That(error.ServerCode, Is.EqualTo("chat.rate_limited"));
            Assert.That(error.RetryAfterSeconds, Is.EqualTo(9));
            Assert.That((await store.LoadAsync(CancellationToken.None)), Has.Count.EqualTo(1));
        }

        [Test]
        public void MalformedErrorBodyFallsBackWithoutExposingServerText()
        {
            var transport = new FakeRest { StatusCode = 503, RawErrorBody = "not-json" };
            var codec = new UnityChatJsonCodec(new SystemTextJsonBackend());
            var provider = new ServerChatProvider(transport, new FakeSession(), null, null, null, null, codec);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ListConversationsAsync(20, CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Transport));
            Assert.That(error.ServerCode, Is.Null);
            Assert.That(error.Message, Does.Not.Contain("not-json"));
        }

        [Test]
        public async Task PendingConversationSurvivesRestartAndNormalizesParticipants()
        {
            var store = new FakePendingConversationStore();
            var request = new RemoteCreateConversationRequest { ChannelType = "Private", ClientRequestId = "create-1", ParticipantIds = new List<string> { "p3", "p2", "p2" }, Title = "  Team  " };
            var first = new ServerChatProvider(new FakeRest { StatusCode = 503 }, new FakeSession(), null, null, null, null, null, store);
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await first.CreateConversationAsync(request, CancellationToken.None));
            Assert.That(await store.LoadAsync(CancellationToken.None), Has.Count.EqualTo(1));

            var transport = new FakeRest();
            var restarted = new ServerChatProvider(transport, new FakeSession(), null, null, null, null, null, store);
            IReadOnlyList<RemoteCreateConversationResult> results = await restarted.RetryPendingConversationsAsync(CancellationToken.None);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(transport.LastCreateRequest.ParticipantIds, Is.EqualTo(new[] { "p2", "p3" }));
            Assert.That(transport.LastCreateRequest.Title, Is.EqualTo("Team"));
            Assert.That(await store.LoadAsync(CancellationToken.None), Is.Empty);
        }

        [Test]
        public async Task PendingConversationCollisionIsRejectedBeforeNetwork()
        {
            var store = new FakePendingConversationStore();
            await store.SaveAsync(new PendingChatConversationCreation { Request = new RemoteCreateConversationRequest { ChannelType = "Private", ClientRequestId = "same-create", Title = "First", ParticipantIds = new List<string> { "p2" } } }, CancellationToken.None);
            var transport = new FakeRest();
            var provider = new ServerChatProvider(transport, new FakeSession(), null, null, null, null, null, store);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await provider.CreateConversationAsync(new RemoteCreateConversationRequest { ChannelType = "Private", ClientRequestId = "same-create", Title = "Other", ParticipantIds = new List<string> { "p2" } }, CancellationToken.None));
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public async Task ConversationConflictIsDefinitiveButRateLimitIsRetained()
        {
            var codec = new UnityChatJsonCodec(new SystemTextJsonBackend());
            var conflictStore = new FakePendingConversationStore();
            var conflict = new ServerChatProvider(new FakeRest { StatusCode = 409 }, new FakeSession(), null, null, null, null, codec, conflictStore);
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await conflict.CreateConversationAsync(new RemoteCreateConversationRequest { ChannelType = "Private", ClientRequestId = "c409", ParticipantIds = new List<string> { "p2" } }, CancellationToken.None));
            Assert.That(await conflictStore.LoadAsync(CancellationToken.None), Is.Empty);

            var rateStore = new FakePendingConversationStore();
            var limited = new ServerChatProvider(new FakeRest { StatusCode = 429 }, new FakeSession(), null, null, null, null, codec, rateStore);
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await limited.CreateConversationAsync(new RemoteCreateConversationRequest { ChannelType = "Private", ClientRequestId = "c429", ParticipantIds = new List<string> { "p2" } }, CancellationToken.None));
            Assert.That(await rateStore.LoadAsync(CancellationToken.None), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task VersionedConversationJournalRoundTrips()
        {
            var strings = new MemoryStringStore();
            var journal = new VersionedChatPendingConversationStore(strings, new SystemTextJsonBackend(), "conversations");
            await journal.SaveAsync(new PendingChatConversationCreation { AttemptCount = 2, Request = new RemoteCreateConversationRequest { ChannelType = "Private", ClientRequestId = "journal-create", ParticipantIds = new List<string> { "p2" } } }, CancellationToken.None);
            var restarted = new VersionedChatPendingConversationStore(strings, new SystemTextJsonBackend(), "conversations");
            IReadOnlyList<PendingChatConversationCreation> loaded = await restarted.LoadAsync(CancellationToken.None);
            Assert.That(loaded, Has.Count.EqualTo(1));
            Assert.That(loaded[0].AttemptCount, Is.EqualTo(2));
            Assert.That(loaded[0].Request.ClientRequestId, Is.EqualTo("journal-create"));
        }

        [Test]
        public async Task ModerationReportSurvivesRestartAndKeepsStableRequestId()
        {
            var store = new FakePendingReportStore();
            var first = new ServerChatProvider(new FakeRest { StatusCode = 503 }, new FakeSession(), pendingReports: store);
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await first.ReportAsync("m1", "spam", "report-1", CancellationToken.None));
            Assert.That(await store.LoadAsync(CancellationToken.None), Has.Count.EqualTo(1));
            var transport = new FakeRest();
            var restarted = new ServerChatProvider(transport, new FakeSession(), pendingReports: store);
            IReadOnlyList<RemoteModerationReport> results = await restarted.RetryPendingReportsAsync(CancellationToken.None);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(transport.LastReportRequest.ClientRequestId, Is.EqualTo("report-1"));
            Assert.That(await store.LoadAsync(CancellationToken.None), Is.Empty);
        }

        [Test]
        public async Task ModerationReportCollisionIsRejectedBeforeNetwork()
        {
            var store = new FakePendingReportStore();
            await store.SaveAsync(new PendingModerationReportRequest { MessageId = "m1", Category = "spam", ClientRequestId = "same-report" }, CancellationToken.None);
            var transport = new FakeRest();
            var provider = new ServerChatProvider(transport, new FakeSession(), pendingReports: store);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await provider.ReportAsync("m1", "harassment", "same-report", CancellationToken.None));
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public async Task ModerationForbiddenIsDefinitiveButRateLimitIsRetained()
        {
            var forbiddenStore = new FakePendingReportStore();
            var forbidden = new ServerChatProvider(new FakeRest { StatusCode = 403 }, new FakeSession(), pendingReports: forbiddenStore);
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await forbidden.ReportAsync("m1", "spam", "report-403", CancellationToken.None));
            Assert.That(await forbiddenStore.LoadAsync(CancellationToken.None), Is.Empty);
            var rateStore = new FakePendingReportStore();
            var rate = new ServerChatProvider(new FakeRest { StatusCode = 429 }, new FakeSession(), pendingReports: rateStore);
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await rate.ReportAsync("m1", "spam", "report-429", CancellationToken.None));
            Assert.That(await rateStore.LoadAsync(CancellationToken.None), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task VersionedModerationJournalRoundTrips()
        {
            var strings = new MemoryStringStore();
            var journal = new VersionedChatPendingModerationReportStore(strings, new SystemTextJsonBackend(), "reports");
            await journal.SaveAsync(new PendingModerationReportRequest { MessageId = "m1", Category = "spam", ClientRequestId = "journal-report", AttemptCount = 2 }, CancellationToken.None);
            var restarted = new VersionedChatPendingModerationReportStore(strings, new SystemTextJsonBackend(), "reports");
            IReadOnlyList<PendingModerationReportRequest> loaded = await restarted.LoadAsync(CancellationToken.None);
            Assert.That(loaded, Has.Count.EqualTo(1));
            Assert.That(loaded[0].AttemptCount, Is.EqualTo(2));
        }

        [Test]
        public async Task ReadCursorSurvivesRestartAndRetriesMaximumSequence()
        {
            var store = new FakePendingReadStore();
            var first = new ServerChatProvider(new FakeRest { StatusCode = 503 }, new FakeSession(), pendingReads: store);
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await first.MarkReadAsync("c1", 7, CancellationToken.None));
            await store.SaveMaximumAsync(new PendingReadCursor { ConversationId = "c1", Sequence = 9 }, CancellationToken.None);
            var transport = new FakeRest();
            var restarted = new ServerChatProvider(transport, new FakeSession(), pendingReads: store);
            await restarted.RetryPendingReadsAsync(CancellationToken.None);
            Assert.That(transport.LastReadRequest.Sequence, Is.EqualTo(9));
            Assert.That(await store.LoadAsync(CancellationToken.None), Is.Empty);
        }

        [Test]
        public async Task OlderReadNeverRegressesStoredMaximum()
        {
            var store = new FakePendingReadStore();
            await store.SaveMaximumAsync(new PendingReadCursor { ConversationId = "c1", Sequence = 10 }, CancellationToken.None);
            await store.SaveMaximumAsync(new PendingReadCursor { ConversationId = "c1", Sequence = 4 }, CancellationToken.None);
            IReadOnlyList<PendingReadCursor> loaded = await store.LoadAsync(CancellationToken.None);
            Assert.That(loaded.Single().Sequence, Is.EqualTo(10));
            await store.RemoveThroughAsync("c1", 9, CancellationToken.None);
            Assert.That(await store.LoadAsync(CancellationToken.None), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task AckInFlightDoesNotEraseNewerRead()
        {
            var store = new FakePendingReadStore();
            var transport = new BlockingReadTransport();
            var provider = new ServerChatProvider(transport, new FakeSession(), pendingReads: store);
            Task<object> operation = provider.MarkReadAsync("c1", 5, CancellationToken.None);
            await transport.Started.Task;
            await store.SaveMaximumAsync(new PendingReadCursor { ConversationId = "c1", Sequence = 8 }, CancellationToken.None);
            transport.Release.TrySetResult(true);
            await operation;
            IReadOnlyList<PendingReadCursor> loaded = await store.LoadAsync(CancellationToken.None);
            Assert.That(loaded.Single().Sequence, Is.EqualTo(8));
        }

        [Test]
        public async Task VersionedReadJournalCoalescesAndRoundTrips()
        {
            var strings = new MemoryStringStore();
            var journal = new VersionedChatPendingReadStore(strings, new SystemTextJsonBackend(), "reads");
            await journal.SaveMaximumAsync(new PendingReadCursor { ConversationId = "c1", Sequence = 3 }, CancellationToken.None);
            await journal.SaveMaximumAsync(new PendingReadCursor { ConversationId = "c1", Sequence = 6 }, CancellationToken.None);
            var restarted = new VersionedChatPendingReadStore(strings, new SystemTextJsonBackend(), "reads");
            Assert.That((await restarted.LoadAsync(CancellationToken.None)).Single().Sequence, Is.EqualTo(6));
        }

        [Test]
        public async Task ConversationPaginationDeduplicatesAcrossPages()
        {
            var transport = new FakeRest();
            transport.ConversationPages.Enqueue(new RemoteConversationPage { Items = new List<RemoteConversation> { Conversation("c1"), Conversation("c2") }, NextCursor = "page-2" });
            transport.ConversationPages.Enqueue(new RemoteConversationPage { Items = new List<RemoteConversation> { Conversation("c2"), Conversation("c3") } });
            RemoteConversationLoadResult result = await NewProvider(transport).LoadAllConversationsAsync(new ChatPaginationPolicy(2, 5), CancellationToken.None);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.PagesLoaded, Is.EqualTo(2));
            Assert.That(result.Items.Select(item => item.ConversationId), Is.EquivalentTo(new[] { "c1", "c2", "c3" }));
        }

        [Test]
        public void ConversationCursorCycleIsRejected()
        {
            var transport = new FakeRest();
            transport.ConversationPages.Enqueue(new RemoteConversationPage { NextCursor = "same" });
            transport.ConversationPages.Enqueue(new RemoteConversationPage { NextCursor = "same" });
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await NewProvider(transport).LoadAllConversationsAsync(new ChatPaginationPolicy(10, 5), CancellationToken.None));
            Assert.That(transport.CallCount, Is.EqualTo(2));
        }

        [Test]
        public async Task MessagePaginationLoadsEveryPageInSequence()
        {
            var transport = new FakeRest();
            transport.MessagePages.Enqueue(new RemoteMessagePage { Items = new List<RemoteChatMessage> { Message(1, "r1"), Message(2, "r2") }, NextAfterSequence = 2 });
            transport.MessagePages.Enqueue(new RemoteMessagePage { Items = new List<RemoteChatMessage> { Message(3, "r3") } });
            RemoteReconciliationResult result = await NewProvider(transport).ReconcileFullyAsync("c1", 0, new ChatPaginationPolicy(2, 5), CancellationToken.None);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Items, Has.Count.EqualTo(3));
            Assert.That(result.ConfirmedSequence, Is.EqualTo(3));
        }

        [Test]
        public async Task PaginationLimitReturnsExplicitIncompleteResult()
        {
            var transport = new FakeRest();
            transport.MessagePages.Enqueue(new RemoteMessagePage { Items = new List<RemoteChatMessage> { Message(1, "r1") }, NextAfterSequence = 1 });
            RemoteReconciliationResult result = await NewProvider(transport).ReconcileFullyAsync("c1", 0, new ChatPaginationPolicy(1, 1), CancellationToken.None);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.NextAfterSequence, Is.EqualTo(1));
            Assert.That(transport.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void CapabilitiesCodecMapsFeatureFlagsAndLimits()
        {
            var codec = new UnityChatJsonCodec(new SystemTextJsonBackend());
            RemoteCapabilities value = codec.Deserialize<RemoteCapabilities>("{\"provider\":\"server\",\"server\":true,\"officialGain\":false,\"protocolVersion\":\"chat-v1\",\"idempotencyReceiptRetentionDays\":30,\"channels\":[\"Alliance\",\"Private\"],\"emojis\":true,\"mentions\":true,\"offlineDelivery\":true,\"readCursors\":true,\"moderationReports\":true,\"realtime\":false,\"limits\":{\"bodyMaxCharacters\":500,\"messagesPerMinutePerPlayer\":30,\"messagesPerTenSecondsPerConversation\":8,\"privateConversationCreatesPerHour\":20,\"maxPrivateRecipients\":20}}");
            Assert.That(value.ProtocolVersion, Is.EqualTo("chat-v1"));
            Assert.That(value.Channels, Does.Contain("Private"));
            Assert.That(value.Limits.BodyMaxCharacters, Is.EqualTo(500));
            Assert.That(value.ReadCursors, Is.True);
            Assert.That(value.IdempotencyReceiptRetentionDays, Is.EqualTo(30));
        }

        [Test]
        public void EnabledServerWithoutReceiptRetentionIsRejectedBeforeSession()
        {
            RemoteCapabilities capabilities = ValidCapabilities(true, false);
            capabilities.IdempotencyReceiptRetentionDays = 0;
            var sessions = new CountingSession();
            var provider = new ServerChatProvider(new FakeRest { Capabilities = capabilities }, sessions);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ConnectAsync(CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Incompatible));
            Assert.That(error.ServerCode, Is.EqualTo("receipt_retention_invalid"));
            Assert.That(sessions.GetCount, Is.Zero);
        }

        [Test]
        public async Task NegotiatedReceiptRetentionReducesEffectiveReplayWindow()
        {
            var clock = new FakeClock(new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero));
            var store = new FakePendingStore();
            store.Items.Add(new PendingChatSend { ConversationId = "c1", Body = "private", ClientRequestId = "old", ClientCreatedAt = "2026-07-13T12:00:00Z" });
            var transport = new FakeRest();
            transport.Capabilities.IdempotencyReceiptRetentionDays = 8;
            var provider = new ServerChatProvider(transport, new FakeSession(), pendingSends: store, replayPolicy: new ChatPendingReplayPolicy(TimeSpan.FromDays(29)), clock: clock);
            RemoteCapabilityDecision decision = await provider.NegotiateCapabilitiesAsync("chat-v1", CancellationToken.None);
            int calls = transport.CallCount;
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.RetryPendingAsync(CancellationToken.None));
            Assert.That(decision.EffectiveReplayMaxAgeDays, Is.EqualTo(7));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.LocalOperationExpired));
            Assert.That(transport.CallCount, Is.EqualTo(calls));
            Assert.That(store.Items, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task StrictProviderBlocksPersistentMutationsUntilCapabilitiesAreNegotiated()
        {
            var rest = new FakeRest();
            var sends = new FakePendingStore();
            var conversations = new FakePendingConversationStore();
            var reports = new FakePendingReportStore();
            var reads = new FakePendingReadStore();
            var sink = new CollectingDiagnostics();
            var sessions = new CountingSession();
            var provider = new ServerChatProvider(rest, sessions, pendingSends: sends, pendingConversations: conversations, pendingReports: reports, pendingReads: reads, diagnostics: sink, requireCapabilityNegotiation: true);

            RemoteChatTransportException send = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "private", "s1", CancellationToken.None));
            RemoteChatTransportException create = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.CreateConversationAsync(new RemoteCreateConversationRequest { ClientRequestId = "c1" }, CancellationToken.None));
            RemoteChatTransportException read = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.MarkReadAsync("c1", 1, CancellationToken.None));
            RemoteChatTransportException report = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ReportAsync("m1", "spam", "r1", CancellationToken.None));
            RemoteChatTransportException drain = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.DrainPendingAsync(CancellationToken.None));
            RemoteChatTransportException list = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ListConversationsAsync(10, CancellationToken.None));
            RemoteChatTransportException reconcile = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ReconcileAsync("c1", 0, CancellationToken.None));
            RemoteChatTransportException translation = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.TranslateAsync("m1", "fr-CA", "v1", CancellationToken.None));

            foreach (RemoteChatTransportException error in new[] { send, create, read, report, drain, list, reconcile, translation })
            {
                Assert.That(error.Error, Is.EqualTo(RemoteChatError.Incompatible));
                Assert.That(error.ServerCode, Is.EqualTo("capability_negotiation_required"));
            }
            Assert.That(rest.CallCount, Is.EqualTo(0));
            Assert.That(sessions.GetCount, Is.EqualTo(0));
            Assert.That(await sends.LoadAsync(CancellationToken.None), Is.Empty);
            Assert.That(await conversations.LoadAsync(CancellationToken.None), Is.Empty);
            Assert.That(await reads.LoadAsync(CancellationToken.None), Is.Empty);
            Assert.That(await reports.LoadAsync(CancellationToken.None), Is.Empty);
            Assert.That(sink.Events.Count(item => item.Code == "capability_negotiation_required"), Is.EqualTo(8));

            await provider.NegotiateCapabilitiesAsync("chat-v1", CancellationToken.None);
            Assert.That(sessions.GetCount, Is.EqualTo(0));
            RemoteSendResult result = await provider.SendAsync("c1", "allowed", "s2", CancellationToken.None);
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(sessions.GetCount, Is.EqualTo(1));
        }

        [Test]
        public async Task CapabilityLeaseExpiresAndDisconnectInvalidatesIt()
        {
            var clock = new FakeClock(new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero));
            var rest = new FakeRest();
            var sessions = new CountingSession();
            var provider = new ServerChatProvider(rest, sessions, clock: clock, requireCapabilityNegotiation: true, capabilityLeasePolicy: new ChatCapabilityLeasePolicy(TimeSpan.FromMinutes(5)));
            RemoteCapabilityDecision first = await provider.NegotiateCapabilitiesAsync("chat-v1", CancellationToken.None);
            Assert.That(first.IsAvailable, Is.True);
            clock.UtcNow = clock.UtcNow.AddMinutes(5).AddTicks(1);
            int calls = rest.CallCount;
            RemoteChatTransportException expired = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ListConversationsAsync(10, CancellationToken.None));
            Assert.That(expired.ServerCode, Is.EqualTo("capability_lease_expired"));
            Assert.That(rest.CallCount, Is.EqualTo(calls));
            Assert.That(sessions.GetCount, Is.EqualTo(0));

            await provider.NegotiateCapabilitiesAsync("chat-v1", CancellationToken.None);
            await provider.ListConversationsAsync(10, CancellationToken.None);
            Assert.That(sessions.GetCount, Is.EqualTo(1));
            await provider.DisconnectAsync(CancellationToken.None);
            RemoteChatTransportException disconnected = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ListConversationsAsync(10, CancellationToken.None));
            Assert.That(disconnected.ServerCode, Is.EqualTo("capability_negotiation_required"));
        }

        [Test]
        public void CapabilityLeasePolicyRejectsStaleOrExcessiveDurations()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatCapabilityLeasePolicy(TimeSpan.FromSeconds(29)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatCapabilityLeasePolicy(TimeSpan.FromHours(1).Add(TimeSpan.FromTicks(1))));
        }

        [Test]
        public async Task CapabilitiesBypassCachesWhileBusinessRequestsDoNotChangeSemantics()
        {
            var rest = new FakeRest();
            var provider = new ServerChatProvider(rest, new FakeSession(), requireCapabilityNegotiation: true);
            await provider.NegotiateCapabilitiesAsync("chat-v1", CancellationToken.None);
            await provider.ListConversationsAsync(10, CancellationToken.None);
            Assert.That(rest.Requests, Has.Count.EqualTo(2));
            Assert.That(rest.Requests[0].Path, Is.EqualTo("/chat/v1/capabilities"));
            Assert.That(rest.Requests[0].BypassCache, Is.True);
            Assert.That(rest.Requests[0].BearerToken, Is.Null);
            Assert.That(rest.Requests[1].BypassCache, Is.False);
            Assert.That(rest.Requests[1].BearerToken, Is.EqualTo("test-token"));
        }

        [Test]
        public void CacheableOrAgedCapabilitiesAreRejectedBeforeSession()
        {
            var sessions = new CountingSession();
            var cacheable = new FakeRest { CapabilityCacheControl = "public, max-age=300" };
            var provider = new ServerChatProvider(cacheable, sessions, requireCapabilityNegotiation: true);
            RemoteChatTransportException cacheError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ConnectAsync(CancellationToken.None));
            Assert.That(cacheError.ServerCode, Is.EqualTo("capability_cache_policy_invalid"));
            Assert.That(sessions.GetCount, Is.EqualTo(0));

            var aged = new FakeRest { CapabilityAgeSeconds = 1 };
            provider = new ServerChatProvider(aged, sessions, requireCapabilityNegotiation: true);
            RemoteChatTransportException ageError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ConnectAsync(CancellationToken.None));
            Assert.That(ageError.ServerCode, Is.EqualTo("capability_cache_policy_invalid"));
            Assert.That(sessions.GetCount, Is.EqualTo(0));
        }

        [Test]
        public async Task ActiveCapabilitiesRejectUnknownProviderUnsafeBoundsAndChannels()
        {
            RemoteCapabilities providerInvalid = ValidCapabilities(); providerInvalid.Provider = "mirror";
            RemoteCapabilities bodyInvalid = ValidCapabilities(); bodyInvalid.Limits.BodyMaxCharacters = 4001;
            RemoteCapabilities retentionInvalid = ValidCapabilities(); retentionInvalid.IdempotencyReceiptRetentionDays = int.MaxValue;
            RemoteCapabilities channelInvalid = ValidCapabilities(); channelInvalid.Channels = new List<string> { "Alliance", "GlobalAdmin" };
            RemoteCapabilities duplicateInvalid = ValidCapabilities(); duplicateInvalid.Channels = new List<string> { "Private", "private" };
            RemoteCapabilities[] values = { providerInvalid, bodyInvalid, retentionInvalid, channelInvalid, duplicateInvalid };
            string[] reasons = { "provider_invalid", "limits_invalid", "receipt_retention_invalid", "channels_invalid", "channels_invalid" };
            for (int index = 0; index < values.Length; index++)
            {
                var sessions = new CountingSession();
                var provider = new ServerChatProvider(new FakeRest { Capabilities = values[index] }, sessions, requireCapabilityNegotiation: true);
                RemoteCapabilityDecision decision = await provider.NegotiateCapabilitiesAsync("chat-v1", CancellationToken.None);
                Assert.That(decision.IsAvailable, Is.False);
                Assert.That(decision.ReasonCode, Is.EqualTo(reasons[index]));
                Assert.That(sessions.GetCount, Is.Zero);
            }
        }

        [Test]
        public async Task OpaqueIdentifiersAreEncodedAsSingleBoundedPathSegments()
        {
            var rest = new FakeRest();
            var provider = new ServerChatProvider(rest, new FakeSession());
            await provider.ReconcileAsync("c/a?#", 0, CancellationToken.None);
            await provider.SendAsync("c/a?#", "body", "send-path", CancellationToken.None);
            await provider.ReportAsync("m/a?#", "spam", "report-path", CancellationToken.None);
            await provider.MarkReadAsync("c/a?#", 2, CancellationToken.None);
            await provider.TranslateAsync("m/a?#", "fr-CA", "v1", CancellationToken.None);
            string[] paths = rest.Requests.Select(item => item.Path).ToArray();
            Assert.That(paths, Does.Contain("/chat/v1/conversations/c%2Fa%3F%23/messages?afterSequence=0"));
            Assert.That(paths, Does.Contain("/chat/v1/conversations/c%2Fa%3F%23/messages"));
            Assert.That(paths, Does.Contain("/chat/v1/messages/m%2Fa%3F%23/report"));
            Assert.That(paths, Does.Contain("/chat/v1/conversations/c%2Fa%3F%23/read"));
            Assert.That(paths, Does.Contain("/chat/v1/messages/m%2Fa%3F%23/translations"));
        }

        [Test]
        public void InvalidOpaqueIdentifiersStopBeforeJournalAndNetwork()
        {
            var rest = new FakeRest();
            var store = new FakePendingStore();
            var provider = new ServerChatProvider(rest, new FakeSession(), pendingSends: store);
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.SendAsync(new string('x', 257), "body", "too-long", CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.SendAsync(" padded ", "body", "padded", CancellationToken.None));
            Assert.That(rest.CallCount, Is.Zero);
            Assert.That(store.Items, Is.Empty);
        }

        [Test]
        public void DisabledServerStopsBeforeSessionAndRealtime()
        {
            var transport = new FakeRest { Capabilities = ValidCapabilities(false, true) };
            var sessions = new CountingSession();
            var realtime = new FakeRealtime();
            var provider = new ServerChatProvider(transport, sessions, realtime);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ConnectAsync(CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Disabled));
            Assert.That(sessions.GetCount, Is.Zero);
            Assert.That(realtime.ConnectCount, Is.Zero);
        }

        [Test]
        public async Task RealtimeNotAdvertisedUsesPollingWithoutRealtimeAttempt()
        {
            var transport = new FakeRest { Capabilities = ValidCapabilities(true, false) };
            var realtime = new FakeRealtime();
            var provider = new ServerChatProvider(transport, new FakeSession(), realtime);
            await provider.ConnectAsync(CancellationToken.None);
            Assert.That(provider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.Polling));
            Assert.That(realtime.ConnectCount, Is.Zero);
        }

        [Test]
        public async Task RealtimeTransportFailureFallsBackButContractFailureRemainsVisible()
        {
            var transientRealtime = new FakeRealtime();
            transientRealtime.ConnectFailures.Enqueue(new RemoteChatTransportException(RemoteChatError.Transport, "network"));
            var transientProvider = new ServerChatProvider(new FakeRest(), new FakeSession(), transientRealtime);
            await transientProvider.ConnectAsync(CancellationToken.None);
            Assert.That(transientProvider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.Polling));

            var invalidRealtime = new FakeRealtime();
            invalidRealtime.ConnectFailures.Enqueue(new RemoteChatTransportException(RemoteChatError.InvalidResponse, "bad handshake", serverCode: "realtime_contract_invalid"));
            var invalidProvider = new ServerChatProvider(new FakeRest(), new FakeSession(), invalidRealtime);
            RemoteChatTransportException invalid = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await invalidProvider.ConnectAsync(CancellationToken.None));
            Assert.That(invalid.ServerCode, Is.EqualTo("realtime_contract_invalid"));
            Assert.That(invalidProvider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.Offline));
        }

        [Test]
        public void RefreshedRealtimeSessionOnlyFallsBackForTransientFailure()
        {
            var realtime = new FakeRealtime();
            realtime.ConnectFailures.Enqueue(new RemoteChatTransportException(RemoteChatError.Unauthorized, "expired"));
            realtime.ConnectFailures.Enqueue(new RemoteChatTransportException(RemoteChatError.Forbidden, "forbidden"));
            var provider = new ServerChatProvider(new FakeRest(), new RefreshableSession(), realtime);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ConnectAsync(CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Forbidden));
            Assert.That(provider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.Offline));
            Assert.That(realtime.ConnectCount, Is.EqualTo(2));
        }

        [Test]
        public void IncompatibleProtocolStopsBeforeSession()
        {
            RemoteCapabilities capabilities = ValidCapabilities(true, false); capabilities.ProtocolVersion = "chat-v2";
            var sessions = new CountingSession();
            var provider = new ServerChatProvider(new FakeRest { Capabilities = capabilities }, sessions);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ConnectAsync(CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Incompatible));
            Assert.That(error.ServerCode, Is.EqualTo("protocol_incompatible"));
            Assert.That(sessions.GetCount, Is.Zero);
        }

        [Test]
        public async Task NegotiatedBodyLimitRejectsBeforeOutboxAndNetwork()
        {
            var transport = new FakeRest(); transport.Capabilities.Limits.BodyMaxCharacters = 4;
            var store = new FakePendingStore();
            var provider = new ServerChatProvider(transport, new FakeSession(), pendingSends: store);
            await provider.ConnectAsync(CancellationToken.None);
            int calls = transport.CallCount;
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "12345", "too-long", CancellationToken.None));
            Assert.That(error.ServerCode, Is.EqualTo("body_too_long"));
            Assert.That(transport.CallCount, Is.EqualTo(calls));
            Assert.That(await store.LoadAsync(CancellationToken.None), Is.Empty);
        }

        [Test]
        public async Task NegotiatedRecipientLimitRejectsBeforeConversationJournal()
        {
            var transport = new FakeRest(); transport.Capabilities.Limits.MaxPrivateRecipients = 1;
            var store = new FakePendingConversationStore();
            var provider = new ServerChatProvider(transport, new FakeSession(), pendingConversations: store);
            await provider.ConnectAsync(CancellationToken.None);
            int calls = transport.CallCount;
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.CreateConversationAsync(new RemoteCreateConversationRequest { ChannelType = "Private", ClientRequestId = "too-many", ParticipantIds = new List<string> { "p2", "p3" } }, CancellationToken.None));
            Assert.That(error.ServerCode, Is.EqualTo("too_many_private_recipients"));
            Assert.That(transport.CallCount, Is.EqualTo(calls));
            Assert.That(await store.LoadAsync(CancellationToken.None), Is.Empty);
        }

        [Test]
        public async Task UnannouncedReadAndModerationFeaturesLeaveNoDurableWork()
        {
            var transport = new FakeRest(); transport.Capabilities.ReadCursors = false; transport.Capabilities.ModerationReports = false;
            var readStore = new FakePendingReadStore(); var reportStore = new FakePendingReportStore();
            var provider = new ServerChatProvider(transport, new FakeSession(), pendingReports: reportStore, pendingReads: readStore);
            await provider.ConnectAsync(CancellationToken.None);
            int calls = transport.CallCount;
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.MarkReadAsync("c1", 2, CancellationToken.None));
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ReportAsync("m1", "spam", "unsupported-report", CancellationToken.None));
            Assert.That(transport.CallCount, Is.EqualTo(calls));
            Assert.That(await readStore.LoadAsync(CancellationToken.None), Is.Empty);
            Assert.That(await reportStore.LoadAsync(CancellationToken.None), Is.Empty);
        }

        [Test]
        public async Task DiagnosticsContainOnlySafeStructuredFields()
        {
            var transport = new FakeRest();
            var sink = new CollectingDiagnostics();
            var codec = new UnityChatJsonCodec(new SystemTextJsonBackend());
            var provider = new ServerChatProvider(transport, new FakeSession(), errorDecoder: codec, diagnostics: sink);
            await provider.ConnectAsync(CancellationToken.None);
            transport.StatusCode = 429; transport.RawErrorBody = "{\"code\":\"chat.rate_limited\",\"message\":\"server detail\"}";
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("secret-conversation", "secret-body", "secret-request", CancellationToken.None));
            string serialized = JsonSerializer.Serialize(sink.Events);
            Assert.That(serialized, Does.Contain("chat.rate_limited"));
            Assert.That(serialized, Does.Not.Contain("secret-body"));
            Assert.That(serialized, Does.Not.Contain("secret-request"));
            Assert.That(serialized, Does.Not.Contain("secret-conversation"));
            Assert.That(serialized, Does.Not.Contain("test-token"));
            Assert.That(serialized, Does.Not.Contain("server detail"));
        }

        [Test]
        public async Task DiagnosticSinkFailureNeverBreaksChatAndGapUsesCountOnly()
        {
            var throwing = new ThrowingDiagnostics();
            var provider = new ServerChatProvider(new FakeRest(), new FakeSession(), diagnostics: throwing);
            await provider.ConnectAsync(CancellationToken.None);
            var sink = new CollectingDiagnostics();
            provider = new ServerChatProvider(new FakeRest(), new FakeSession(), diagnostics: sink);
            RemoteChatMessage privateMessage = Message(3, "private-request"); privateMessage.ConversationId = "private-id";
            await provider.ApplyRealtimeEventAsync(new RemoteChatEvent { ConversationId = "private-id", Message = privateMessage }, CancellationToken.None);
            ChatDiagnosticEvent gap = sink.Events.Single(item => item.Code == "realtime_sequence_gap");
            Assert.That(gap.Count, Is.EqualTo(2));
            Assert.That(JsonSerializer.Serialize(gap), Does.Not.Contain("private-id"));
        }

        [Test]
        public void RemoteFactoryBuildsCompleteClientForHttps()
        {
            RemoteChatClientComponents components = RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "https://chat.example.test", StoragePrefix = "Test.Chat", StoragePartitionId = "player-one" }, new FakeSession(), new MemoryStringStore(), new TestDataProtector("key"));
            Assert.That(components.Provider, Is.Not.Null);
            Assert.That(components.Synchronizer, Is.Not.Null);
            Assert.That(components.PendingRecovery, Is.Not.Null);
        }

        [Test]
        public void ChatEndpointCompositionAcceptsOriginOrCanonicalApiRootWithoutDuplication()
        {
            const string path = "/chat/v1/conversations/c1/messages?afterSequence=0";
            const string expected = "https://chat.example.test/chat/v1/conversations/c1/messages?afterSequence=0";
            Assert.That(ChatEndpointUrl.Compose("https://chat.example.test", path), Is.EqualTo(expected));
            Assert.That(ChatEndpointUrl.Compose("https://chat.example.test/", path), Is.EqualTo(expected));
            Assert.That(ChatEndpointUrl.Compose("https://chat.example.test/chat/v1", path), Is.EqualTo(expected));
            Assert.That(ChatEndpointUrl.Compose("https://chat.example.test/chat/v1/", path), Is.EqualTo(expected));
        }

        [Test]
        public void ChatEndpointCompositionRejectsAmbiguousOrUnsafeUrlsAndPaths()
        {
            Assert.Throws<ArgumentException>(() => ChatEndpointUrl.NormalizeBaseUrl("https://user:pass@chat.example.test/chat/v1"));
            Assert.Throws<ArgumentException>(() => ChatEndpointUrl.NormalizeBaseUrl("https://chat.example.test/chat/v1?tenant=x"));
            Assert.Throws<ArgumentException>(() => ChatEndpointUrl.NormalizeBaseUrl("https://chat.example.test/chat/v1#fragment"));
            Assert.Throws<ArgumentException>(() => ChatEndpointUrl.NormalizeBaseUrl("https://chat.example.test/other"));
            Assert.Throws<ArgumentException>(() => ChatEndpointUrl.Compose("https://chat.example.test/chat/v1", "/chat/v10/messages"));
            Assert.Throws<ArgumentException>(() => ChatEndpointUrl.Compose("https://chat.example.test/chat/v1", "https://other.example/chat/v1"));
        }

        [Test]
        public void UnityTransportPolicyForbidsAutomaticHttpRedirects()
        {
            Assert.That(ChatHttpSecurityPolicy.RedirectLimit, Is.Zero,
                "A chat redirect could forward an authenticated request outside the validated endpoint.");
        }

        [Test]
        public void RestTimeoutPolicyIsFiniteBoundedAndRoundsFractionsUp()
        {
            Assert.That(new ChatHttpTimeoutPolicy().TimeoutSeconds, Is.EqualTo(30));
            Assert.That(new ChatHttpTimeoutPolicy(TimeSpan.FromMilliseconds(1)).TimeoutSeconds, Is.EqualTo(1));
            Assert.That(new ChatHttpTimeoutPolicy(TimeSpan.FromMilliseconds(1500)).TimeoutSeconds, Is.EqualTo(2));
            Assert.That(new ChatHttpTimeoutPolicy(TimeSpan.FromSeconds(120)).TimeoutSeconds, Is.EqualTo(120));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatHttpTimeoutPolicy(TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatHttpTimeoutPolicy(TimeSpan.FromSeconds(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatHttpTimeoutPolicy(TimeSpan.FromSeconds(121)));
        }

        [Test]
        public void RemoteFactoryRejectsInvalidRestTimeoutConfiguration()
        {
            var protector = new TestDataProtector("key");
            Assert.Throws<ArgumentOutOfRangeException>(() => RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "https://chat.example.test/chat/v1", StoragePartitionId = "p1", RequestTimeout = TimeSpan.Zero }, new FakeSession(), new MemoryStringStore(), protector));
            Assert.Throws<ArgumentOutOfRangeException>(() => RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "https://chat.example.test/chat/v1", StoragePartitionId = "p1", RequestTimeout = TimeSpan.FromMinutes(3) }, new FakeSession(), new MemoryStringStore(), protector));
        }

        [Test]
        public void BoundedResponseBufferAcceptsExactLimitAndRejectsNextByteWithoutGrowth()
        {
            var buffer = new BoundedChatResponseBuffer(1024);
            Assert.That(buffer.TryAppend(new byte[1000], 1000), Is.True);
            Assert.That(buffer.TryAppend(new byte[24], 24), Is.True);
            Assert.That(buffer.Length, Is.EqualTo(1024));
            Assert.That(buffer.TryAppend(new byte[1], 1), Is.False);
            Assert.That(buffer.LimitExceeded, Is.True);
            Assert.That(buffer.Length, Is.EqualTo(1024));
        }

        [Test]
        public void ResponseSizePolicyAndFactoryRejectUnsafeBounds()
        {
            Assert.That(new ChatHttpResponsePolicy().MaxBytes, Is.EqualTo(1048576));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatHttpResponsePolicy(1023));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatHttpResponsePolicy(4194305));
            var protector = new TestDataProtector("key");
            Assert.Throws<ArgumentOutOfRangeException>(() => RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "https://chat.example.test/chat/v1", StoragePartitionId = "p1", MaxResponseBytes = 1023 }, new FakeSession(), new MemoryStringStore(), protector));
        }

        [Test]
        public void IncompleteSuccessfulHttpResponseIsRejectedAsTransportFailure()
        {
            var transport = new FakeRest { StatusCode = 200, TransportError = "response limit exceeded" };
            var provider = new ServerChatProvider(transport, new FakeSession());
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ListConversationsAsync(20, CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Transport));
        }

        [Test]
        public void RequestPolicyMeasuresUtf8BytesAndRejectsBeforePayloadAllocation()
        {
            var policy = new ChatHttpRequestPolicy(1024);
            Assert.That(policy.EncodeJson(new string('é', 512)), Has.Length.EqualTo(1024));
            RemoteChatTransportException error = Assert.Throws<RemoteChatTransportException>(() => policy.EncodeJson(new string('é', 513)));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.LocalRequestTooLarge));
            Assert.That(error.ServerCode, Is.EqualTo("local_request_too_large"));
        }

        [Test]
        public void RequestSizePolicyAndFactoryRejectUnsafeBounds()
        {
            Assert.That(new ChatHttpRequestPolicy().MaxBytes, Is.EqualTo(65536));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatHttpRequestPolicy(1023));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatHttpRequestPolicy(1048577));
            var protector = new TestDataProtector("key");
            Assert.Throws<ArgumentOutOfRangeException>(() => RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "https://chat.example.test/chat/v1", StoragePartitionId = "p1", MaxRequestBytes = 1048577 }, new FakeSession(), new MemoryStringStore(), protector));
        }

        [Test]
        public void RequestTargetPolicyUsesUtf8BytesAndRejectsControlsAndUnsafeBounds()
        {
            var policy = new ChatHttpRequestTargetPolicy(1024);
            policy.Validate("/chat/v1?cursor=" + new string('é', 500));
            RemoteChatTransportException oversized = Assert.Throws<RemoteChatTransportException>(() => policy.Validate("/chat/v1?cursor=" + new string('é', 505)));
            Assert.That(oversized.ServerCode, Is.EqualTo("local_request_target_too_large"));
            Assert.Throws<ArgumentException>(() => policy.Validate("/chat/v1?cursor=x\r\ny"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatHttpRequestTargetPolicy(1023));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatHttpRequestTargetPolicy(16385));
        }

        [Test]
        public void MismatchedMessageReceiptIsRejectedWithoutAcknowledgingPendingSend()
        {
            var store = new FakePendingStore();
            var transport = new FakeRest { SendResultOverride = new RemoteSendResult { Message = new RemoteChatMessage { MessageId = "m-wrong", ConversationId = "other", ClientRequestId = "request-1", OriginalBody = "body", SenderId = "p1", Sequence = 1 }, ServerSequence = 1 } };
            var provider = new ServerChatProvider(transport, new FakeSession(), pendingSends: store);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "body", "request-1", CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.InvalidResponse));
            Assert.That(error.ServerCode, Is.EqualTo("message_receipt_mismatch"));
            Assert.That(store.Items, Has.Count.EqualTo(1));
        }

        [Test]
        public void ReceiptFromDifferentSenderCannotAcknowledgeAuthenticatedPlayersSend()
        {
            var store = new FakePendingStore();
            var transport = new FakeRest { SendResultOverride = new RemoteSendResult { Message = new RemoteChatMessage { MessageId = "m1", ConversationId = "c1", ClientRequestId = "request-1", OriginalBody = "body", SenderId = "p2", Sequence = 1 }, ServerSequence = 1 } };
            var provider = new ServerChatProvider(transport, new FixedSession(new ChatSession("p1", "valid-token")), pendingSends: store);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "body", "request-1", CancellationToken.None));
            Assert.That(error.ServerCode, Is.EqualTo("message_receipt_mismatch"));
            Assert.That(store.Items, Has.Count.EqualTo(1));
        }

        [Test]
        public void DifferentAccountCannotReadOrWritePartitionJournals()
        {
            var rest = new FakeRest();
            var sends = new FakePendingStore();
            var conversations = new FakePendingConversationStore();
            var reports = new FakePendingReportStore();
            var reads = new FakePendingReadStore();
            var provider = new ServerChatProvider(rest, new FixedSession(new ChatSession("p2", "valid-token")), pendingSends: sends, pendingConversations: conversations, pendingReports: reports, pendingReads: reads, expectedPlayerId: "p1");

            foreach (AsyncTestDelegate operation in new AsyncTestDelegate[]
            {
                async () => await provider.SendAsync("c1", "body", "send-1", CancellationToken.None),
                async () => await provider.CreateConversationAsync(new RemoteCreateConversationRequest { ChannelType = "Private", ParticipantIds = new List<string> { "p2" }, ClientRequestId = "create-1" }, CancellationToken.None),
                async () => await provider.ReportAsync("m1", "spam", "report-1", CancellationToken.None),
                async () => await provider.MarkReadAsync("c1", 1, CancellationToken.None),
                async () => await provider.GetPendingQueueStatusAsync(CancellationToken.None),
                async () => await provider.RetryPendingAsync(CancellationToken.None)
            })
            {
                RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(operation);
                Assert.That(error.Error, Is.EqualTo(RemoteChatError.LocalAccountMismatch));
                Assert.That(error.ServerCode, Is.EqualTo("local_account_mismatch"));
            }

            Assert.That(rest.CallCount, Is.Zero);
            Assert.That(sends.Items, Is.Empty);
            Assert.That(conversations.Count, Is.Zero);
            Assert.That(reports.Count, Is.Zero);
            Assert.That(reads.Count, Is.Zero);
        }

        [Test]
        public void AccountChangeDuringRefreshCannotReplayPendingOperation()
        {
            var rest = new FakeRest();
            rest.ResponseStatuses.Enqueue(401);
            var sends = new FakePendingStore();
            var provider = new ServerChatProvider(rest, new AccountChangingRefreshSession(), pendingSends: sends, expectedPlayerId: "p1");

            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "body", "send-1", CancellationToken.None));

            Assert.That(error.Error, Is.EqualTo(RemoteChatError.LocalAccountMismatch));
            Assert.That(rest.CallCount, Is.EqualTo(1));
            Assert.That(sends.Items, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task CachedTranslationCannotCrossAccountBoundary()
        {
            var rest = new FakeRest();
            var sessions = new MutableSession(new ChatSession("p1", "valid-token"));
            var provider = new ServerChatProvider(rest, sessions, expectedPlayerId: "p1");
            await provider.ApplyRealtimeEventAsync(new RemoteChatEvent { ConversationId = "c1", Sequence = 1, Message = Message(1, "r1") }, CancellationToken.None);
            await provider.TranslateAsync("m1", "fr-CA", "v1", CancellationToken.None);

            sessions.Current = new ChatSession("p2", "other-token");
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.TranslateAsync("m1", "fr-CA", "v1", CancellationToken.None));

            Assert.That(error.Error, Is.EqualTo(RemoteChatError.LocalAccountMismatch));
            Assert.That(rest.TranslationPostCount, Is.EqualTo(1));
            Assert.That(provider.GetConfirmedSequence("c1"), Is.Zero);
        }

        [Test]
        public async Task DisconnectClearsVolatileMessagesSequencesAndTranslations()
        {
            var rest = new FakeRest();
            var provider = new ServerChatProvider(rest, new FakeSession());
            await provider.ApplyRealtimeEventAsync(new RemoteChatEvent { ConversationId = "c1", Sequence = 1, Message = Message(1, "r1") }, CancellationToken.None);
            await provider.TranslateAsync("m1", "fr-CA", "v1", CancellationToken.None);
            Assert.That(provider.GetConfirmedSequence("c1"), Is.EqualTo(1));

            await provider.DisconnectAsync(CancellationToken.None);
            Assert.That(provider.GetConfirmedSequence("c1"), Is.Zero);
            await provider.TranslateAsync("m1", "fr-CA", "v1", CancellationToken.None);

            Assert.That(rest.TranslationPostCount, Is.EqualTo(2));
        }

        [Test]
        public async Task ResponseArrivingAfterDisconnectCannotAcknowledgePendingJournal()
        {
            var rest = new BlockingReadTransport();
            var reads = new FakePendingReadStore();
            var provider = new ServerChatProvider(rest, new FakeSession(), pendingReads: reads);
            Task<object> operation = provider.MarkReadAsync("c1", 7, CancellationToken.None);
            await rest.Started.Task;

            await provider.DisconnectAsync(CancellationToken.None);
            rest.Release.TrySetResult(true);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await operation);

            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Cancelled));
            Assert.That(error.ServerCode, Is.EqualTo("local_session_changed"));
            Assert.That(reads.Count, Is.EqualTo(1));
            Assert.That(provider.GetConfirmedSequence("c1"), Is.Zero);
        }

        [Test]
        public async Task LogoutDuringJournalWritePreventsHttpButPreservesPendingOperation()
        {
            var rest = new FakeRest();
            var sends = new BlockingPendingSendStore();
            var provider = new ServerChatProvider(rest, new FakeSession(), pendingSends: sends);
            Task<RemoteSendResult> operation = provider.SendAsync("c1", "body", "send-1", CancellationToken.None);
            await sends.SaveStarted.Task;

            await provider.DisconnectAsync(CancellationToken.None);
            sends.ReleaseSave.TrySetResult(true);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await operation);

            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Cancelled));
            Assert.That(error.ServerCode, Is.EqualTo("local_session_changed"));
            Assert.That(rest.CallCount, Is.Zero);
            Assert.That(sends.Items, Has.Count.EqualTo(1));
        }

        [Test]
        public void MismatchedConversationAndModerationReceiptsRemainPending()
        {
            var conversations = new FakePendingConversationStore();
            var conversationRest = new FakeRest { CreateResultOverride = new RemoteCreateConversationResult { Conversation = new RemoteConversation { ConversationId = "c-created" }, Inbox = new RemoteInboxEntry { ConversationId = "c-created" }, ClientRequestId = "wrong" } };
            var conversationProvider = new ServerChatProvider(conversationRest, new FakeSession(), pendingConversations: conversations);
            RemoteChatTransportException createError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await conversationProvider.CreateConversationAsync(new RemoteCreateConversationRequest { ChannelType = "Private", ParticipantIds = new List<string> { "p2" }, ClientRequestId = "create-1" }, CancellationToken.None));
            Assert.That(createError.ServerCode, Is.EqualTo("conversation_receipt_mismatch"));
            Assert.That(conversations.Count, Is.EqualTo(1));

            var reports = new FakePendingReportStore();
            var reportRest = new FakeRest { ReportResultOverride = new RemoteModerationReport { ReportId = "report-1", MessageId = "other", ClientRequestId = "report-1", Status = "open" } };
            var reportProvider = new ServerChatProvider(reportRest, new FakeSession(), pendingReports: reports);
            RemoteChatTransportException reportError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await reportProvider.ReportAsync("m1", "spam", "report-1", CancellationToken.None));
            Assert.That(reportError.ServerCode, Is.EqualTo("moderation_receipt_mismatch"));
            Assert.That(reports.Count, Is.EqualTo(1));
        }

        [Test]
        public void MismatchedReadReceiptDoesNotDiscardMonotoneCursor()
        {
            var reads = new FakePendingReadStore();
            var transport = new FakeRest { ReadResultOverride = new RemoteInboxEntry { ConversationId = "other", ReadCursorSequence = 9 } };
            var provider = new ServerChatProvider(transport, new FakeSession(), pendingReads: reads);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.MarkReadAsync("c1", 9, CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.InvalidResponse));
            Assert.That(error.ServerCode, Is.EqualTo("read_receipt_mismatch"));
            Assert.That(reads.Count, Is.EqualTo(1));
        }

        [Test]
        public void CrossConversationPageAndRealtimeEventAreRejectedBeforeMerge()
        {
            var transport = new FakeRest();
            transport.Page.Items.Add(new RemoteChatMessage { MessageId = "m1", ConversationId = "other", Sequence = 1, ClientRequestId = "r1", SenderId = "p1", OriginalBody = "body" });
            var provider = new ServerChatProvider(transport, new FakeSession());
            RemoteChatTransportException pageError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ReconcileAsync("c1", 0, CancellationToken.None));
            Assert.That(pageError.ServerCode, Is.EqualTo("message_page_invalid"));
            Assert.That(provider.GetConfirmedSequence("c1"), Is.Zero);

            var realtime = new RemoteChatEvent { ConversationId = "c1", Sequence = 2, Message = new RemoteChatMessage { MessageId = "m2", ConversationId = "other", Sequence = 2, ClientRequestId = "r2", SenderId = "p1", OriginalBody = "body" } };
            RemoteChatTransportException realtimeError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ApplyRealtimeEventAsync(realtime, CancellationToken.None));
            Assert.That(realtimeError.ServerCode, Is.EqualTo("realtime_event_mismatch"));
            Assert.That(provider.GetConfirmedSequence("c1"), Is.Zero);
        }

        [Test]
        public void ConversationAndMessagePagesRejectDuplicatesRegressionAndExcessItems()
        {
            var conversations = new FakeRest();
            conversations.ConversationPages.Enqueue(new RemoteConversationPage { Items = new List<RemoteConversation> { new RemoteConversation { ConversationId = "c1" }, new RemoteConversation { ConversationId = "c1" } } });
            var conversationProvider = new ServerChatProvider(conversations, new FakeSession());
            RemoteChatTransportException duplicate = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await conversationProvider.ListConversationsAsync(20, CancellationToken.None));
            Assert.That(duplicate.ServerCode, Is.EqualTo("conversation_page_invalid"));

            var messages = new FakeRest();
            messages.Page.Items.Add(Message(2, "r2"));
            messages.Page.Items.Add(Message(2, "r2-duplicate"));
            var messageProvider = new ServerChatProvider(messages, new FakeSession());
            RemoteChatTransportException regression = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await messageProvider.ReconcileAsync("c1", 0, CancellationToken.None));
            Assert.That(regression.ServerCode, Is.EqualTo("message_page_invalid"));
        }

        [Test]
        public async Task TranslationResponseMustMatchRequestBeforeItCanBeCached()
        {
            var transport = new FakeRest { TranslationResultOverride = new MessageTranslation { MessageId = "other", SourceLocale = "es", TargetLocale = "fr-CA", ModelVersion = "v1", TranslatedText = "wrong", Status = "completed" } };
            var provider = new ServerChatProvider(transport, new FakeSession());
            RemoteChatTransportException mismatch = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.TranslateAsync("m1", "fr-CA", "v1", CancellationToken.None));
            Assert.That(mismatch.ServerCode, Is.EqualTo("translation_response_mismatch"));
            transport.TranslationResultOverride = null;
            MessageTranslation valid = await provider.TranslateAsync("m1", "fr-CA", "v1", CancellationToken.None);
            MessageTranslation cached = await provider.TranslateAsync("m1", "fr-CA", "v1", CancellationToken.None);
            Assert.That(valid.TranslatedText, Is.EqualTo("translated"));
            Assert.That(cached, Is.SameAs(valid));
            Assert.That(transport.TranslationPostCount, Is.EqualTo(2));
        }

        [Test]
        public void TranslationParametersAreBoundedBeforeNetwork()
        {
            var transport = new FakeRest();
            var provider = new ServerChatProvider(transport, new FakeSession());
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.TranslateAsync("m1", "fr--CA", "v1", CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.TranslateAsync("m1", "fr-CA", "unsafe version", CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.TranslateAsync("m1", new string('a', 36), "v1", CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.TranslateAsync("m1", "fr", new string('v', 129), CancellationToken.None));
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public void InvalidMutationFieldsNeverEnterPersistentJournalsOrNetwork()
        {
            var transport = new FakeRest();
            var sends = new FakePendingStore();
            var conversations = new FakePendingConversationStore();
            var reports = new FakePendingReportStore();
            var provider = new ServerChatProvider(transport, new FakeSession(), pendingSends: sends, pendingConversations: conversations, pendingReports: reports);

            Assert.ThrowsAsync<ArgumentException>(async () => await provider.SendAsync("c1", " padded ", "send-1", CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.SendAsync("c1", "body", new string('r', 257), CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.CreateConversationAsync(new RemoteCreateConversationRequest { ChannelType = "Private", ClientRequestId = "create-1", ParticipantIds = Enumerable.Range(0, 101).Select(index => "p" + index).ToList() }, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.CreateConversationAsync(new RemoteCreateConversationRequest { ChannelType = "Unknown", ClientRequestId = "create-2" }, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.ReportAsync("m1", new string('c', 65), "report-1", CancellationToken.None));

            Assert.That(sends.Items, Is.Empty);
            Assert.That(conversations.Count, Is.Zero);
            Assert.That(reports.Count, Is.Zero);
            Assert.That(transport.CallCount, Is.Zero);

            sends.Items.Add(new PendingChatSend { ConversationId = "c1", Body = " restored-invalid ", ClientRequestId = "restored-1", ClientCreatedAt = "2026-07-21T12:00:00Z" });
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.RetryPendingAsync(CancellationToken.None));
            Assert.That(sends.Items, Has.Count.EqualTo(1));
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public void LowestTransportLayerEnforcesPublicCapabilitiesAndAuthenticatedBusinessRoutes()
        {
            Assert.DoesNotThrow(() => ChatHttpRequestInvariant.Validate(new ChatTransportRequest { Method = "GET", Path = "/chat/v1/capabilities", BypassCache = true }));
            Assert.DoesNotThrow(() => ChatHttpRequestInvariant.Validate(new ChatTransportRequest { Method = "GET", Path = "/chat/v1/conversations?limit=20", BearerToken = "valid-token" }));
            Assert.DoesNotThrow(() => ChatHttpRequestInvariant.Validate(new ChatTransportRequest { Method = "POST", Path = "/chat/v1/conversations", BearerToken = "valid-token", Body = new object() }));

            Assert.Throws<ArgumentException>(() => ChatHttpRequestInvariant.Validate(new ChatTransportRequest { Method = "GET", Path = "/chat/v1/capabilities", BypassCache = true, BearerToken = "must-not-leak" }));
            Assert.Throws<ArgumentException>(() => ChatHttpRequestInvariant.Validate(new ChatTransportRequest { Method = "GET", Path = "/chat/v1/capabilities" }));
            Assert.Throws<ArgumentException>(() => ChatHttpRequestInvariant.Validate(new ChatTransportRequest { Method = "GET", Path = "/chat/v1/conversations?limit=20" }));
            Assert.Throws<ArgumentException>(() => ChatHttpRequestInvariant.Validate(new ChatTransportRequest { Method = "GET", Path = "/chat/v1/conversations?limit=20", BearerToken = "valid-token", BypassCache = true }));
            Assert.Throws<ArgumentException>(() => ChatHttpRequestInvariant.Validate(new ChatTransportRequest { Method = "DELETE", Path = "/chat/v1/conversations/c1", BearerToken = "valid-token" }));
            Assert.Throws<ArgumentException>(() => ChatHttpRequestInvariant.Validate(new ChatTransportRequest { Method = "GET", Path = "/chat/v1/conversations", BearerToken = "valid-token", Body = new object() }));
            Assert.Throws<ArgumentException>(() => ChatHttpRequestInvariant.Validate(new ChatTransportRequest { Method = "POST", Path = "/chat/v1/conversations", BearerToken = "valid-token" }));
        }

        [Test]
        public async Task ConversationCursorIsBoundedEscapedAndInvalidServerCursorIsRejected()
        {
            var transport = new FakeRest();
            var provider = new ServerChatProvider(transport, new FakeSession());
            await provider.ListConversationsAsync(100, "opaque?&value", CancellationToken.None);
            Assert.That(transport.Requests.Single().Path, Does.EndWith("&cursor=opaque%3F%26value"));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await provider.ListConversationsAsync(101, CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () => await provider.ListConversationsAsync(20, new string('x', 1025), CancellationToken.None));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await provider.ReconcileAsync("c1", -1, CancellationToken.None));
            transport.ConversationPages.Enqueue(new RemoteConversationPage { NextCursor = new string('x', 1025) });
            RemoteChatTransportException invalid = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.LoadAllConversationsAsync(new ChatPaginationPolicy(), CancellationToken.None));
            Assert.That(invalid.Error, Is.EqualTo(RemoteChatError.InvalidResponse));
            Assert.That(invalid.ServerCode, Is.EqualTo("invalid_conversation_cursor"));
        }

        [TestCase("token with space")]
        [TestCase("token\r\nInjected: value")]
        [TestCase("token=middle.part")]
        public void MalformedBearerSessionIsRejectedBeforeNetwork(string token)
        {
            var transport = new FakeRest();
            var provider = new ServerChatProvider(transport, new FixedSession(new ChatSession("p1", token)));
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ListConversationsAsync(20, CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Unauthorized));
            Assert.That(provider.ConnectionState, Is.EqualTo(RemoteChatConnectionState.AuthenticationRequired));
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public void OversizedBearerAndMalformedPlayerIdentityAreRejectedBeforeNetwork()
        {
            var transport = new FakeRest();
            var oversized = new ServerChatProvider(transport, new FixedSession(new ChatSession("p1", new string('a', ChatSessionSecurity.MaxBearerTokenCharacters + 1))));
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await oversized.ListConversationsAsync(20, CancellationToken.None));
            var malformedPlayer = new ServerChatProvider(transport, new FixedSession(new ChatSession(" player ", "valid-token")));
            Assert.ThrowsAsync<RemoteChatTransportException>(async () => await malformedPlayer.ListConversationsAsync(20, CancellationToken.None));
            Assert.That(transport.CallCount, Is.Zero);
        }

        [Test]
        public void RemoteFactoryRejectsPublicHttpAndInvalidConfiguration()
        {
            var protector = new TestDataProtector("key");
            Assert.Throws<ArgumentException>(() => RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "http://chat.example.test", StoragePartitionId = "p1" }, new FakeSession(), new MemoryStringStore(), protector));
            Assert.Throws<ArgumentException>(() => RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "not-a-url", StoragePartitionId = "p1" }, new FakeSession(), new MemoryStringStore(), protector));
            Assert.Throws<ArgumentException>(() => RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "https://chat.example.test", StoragePrefix = " ", StoragePartitionId = "p1" }, new FakeSession(), new MemoryStringStore(), protector));
            Assert.Throws<ArgumentException>(() => RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "https://chat.example.test" }, new FakeSession(), new MemoryStringStore(), protector));
        }

        [Test]
        public void RemoteFactoryAllowsHttpOnlyForExplicitLoopbackDevelopment()
        {
            var protector = new TestDataProtector("key");
            Assert.Throws<ArgumentException>(() => RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "http://localhost:5000", StoragePartitionId = "p1" }, new FakeSession(), new MemoryStringStore(), protector));
            RemoteChatClientComponents components = RemoteChatClientFactory.Create(new RemoteChatClientOptions { BaseUrl = "http://127.0.0.1:5000", AllowInsecureLoopback = true, StoragePartitionId = "p1" }, new FakeSession(), new MemoryStringStore(), protector);
            Assert.That(components.Provider, Is.Not.Null);
        }

        [Test]
        public void FactoryPartitionsProtectedJournalsPerPlayerWithoutLeakingPlayerId()
        {
            string first = ChatStoragePartition.KeyPrefix("BeeKingdom.Chat", "private-player-one");
            string second = ChatStoragePartition.KeyPrefix("BeeKingdom.Chat", "private-player-two");
            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(first, Does.Not.Contain("private-player-one"));
            Assert.That(second, Does.Not.Contain("private-player-two"));
            Assert.That(first, Is.EqualTo(ChatStoragePartition.KeyPrefix("BeeKingdom.Chat", "private-player-one")));
        }

        [Test]
        public void RecentCacheIsProtectedPartitionedBoundedAndRestorable()
        {
            var raw = new MultiKeyStringStore();
            string prefix = ChatStoragePartition.KeyPrefix("BeeKingdom.Chat", "player-one");
            string key = prefix + ".Recent.v1";
            var cache = new VersionedChatRecentCache(raw, new ProtectedChatStringStore(raw, new TestDataProtector("device-key"), prefix + ".Protection.v1"), new SystemTextJsonBackend(), key, 20);
            var messages = new List<LivingHiveChatMessage>();
            for (int sequence = 1; sequence <= 25; sequence++) messages.Add(new LivingHiveChatMessage { MessageId = "m" + sequence, ConversationId = "c1", ClientRequestId = "r" + sequence, OriginalBody = "private body " + sequence, VisibleBody = "private body " + sequence, Sequence = sequence, CreatedAt = DateTimeOffset.UtcNow, Delivery = LivingHiveChatDelivery.Confirmed });

            cache.Save(new ChatRecentCacheSnapshot { SelectedConversationId = "c1", Conversations = new[] { new LivingHiveChatConversation { ConversationId = "c1", Title = "Alliance", LastSequence = 25 } }, Messages = messages });
            ChatRecentCacheSnapshot restored = cache.Load();

            Assert.That(restored.Messages, Has.Count.EqualTo(20));
            Assert.That(restored.Messages[0].Sequence, Is.EqualTo(6));
            Assert.That(raw.Keys.Single(), Does.Not.Contain("player-one"));
            Assert.That(raw.Values.Single(), Does.Not.Contain("private body"));
        }

        [Test]
        public void RecentCacheTamperIsQuarantinedBeforeSourceReset()
        {
            var raw = new MultiKeyStringStore();
            const string key = "BeeKingdom.Chat.Player.hash.Recent.v1";
            var cache = new VersionedChatRecentCache(raw, new ProtectedChatStringStore(raw, new TestDataProtector("device-key")), new SystemTextJsonBackend(), key);
            cache.Save(new ChatRecentCacheSnapshot { SelectedConversationId = "c1", Conversations = new[] { new LivingHiveChatConversation { ConversationId = "c1" } } });
            string encrypted = raw.Read(key);
            raw.Write(key, encrypted + "tampered");

            ChatRecentCacheException error = Assert.Throws<ChatRecentCacheException>(() => cache.Load());

            Assert.That(error.Quarantined, Is.True);
            Assert.That(raw.Read(key), Is.Null);
            Assert.That(raw.Read(key + ".Quarantine.v1"), Is.EqualTo(encrypted + "tampered"));
        }

        [Test]
        public void RecentCacheQuarantineRotatesAcrossTwoSuccessiveCorruptions()
        {
            var raw = new MultiKeyStringStore();
            const string key = "BeeKingdom.Chat.Player.hash.Recent.v1";
            var cache = new VersionedChatRecentCache(raw, new ProtectedChatStringStore(raw, new TestDataProtector("device-key")), new SystemTextJsonBackend(), key);
            var snapshot = new ChatRecentCacheSnapshot { SelectedConversationId = "c1", Conversations = new[] { new LivingHiveChatConversation { ConversationId = "c1" } } };
            cache.Save(snapshot);
            string firstCorruption = raw.Read(key) + "first";
            raw.Write(key, firstCorruption);
            Assert.Throws<ChatRecentCacheException>(() => cache.Load());

            cache.Save(snapshot);
            string secondCorruption = raw.Read(key) + "second";
            raw.Write(key, secondCorruption);
            ChatRecentCacheException second = Assert.Throws<ChatRecentCacheException>(() => cache.Load());

            Assert.That(second.Quarantined, Is.True);
            Assert.That(raw.Read(key), Is.Null);
            Assert.That(raw.Read(key + ".Quarantine.v1"), Is.EqualTo(secondCorruption));
            Assert.That(raw.Read(key + ".Quarantine.Previous.v1"), Is.EqualTo(firstCorruption));
            Assert.That(raw.Read(key + ".Quarantine.Staging.v1"), Is.Null);
        }

        [Test]
        public void RecentCachePreservesCurrentBlobWhenQuarantineCannotBeVerified()
        {
            var raw = new FailingRecentQuarantineStore();
            const string key = "BeeKingdom.Chat.Player.hash.Recent.v1";
            var cache = new VersionedChatRecentCache(raw, new ProtectedChatStringStore(raw, new TestDataProtector("device-key")), new SystemTextJsonBackend(), key);
            cache.Save(new ChatRecentCacheSnapshot { SelectedConversationId = "c1", Conversations = new[] { new LivingHiveChatConversation { ConversationId = "c1" } } });
            string corrupted = raw.Read(key) + "corrupted";
            raw.Write(key, corrupted);
            raw.FailCurrentQuarantine = true;

            ChatRecentCacheException error = Assert.Throws<ChatRecentCacheException>(() => cache.Load());

            Assert.That(error.Quarantined, Is.False);
            Assert.That(raw.Read(key), Is.EqualTo(corrupted));
        }

        [Test]
        public void RecentCacheAlwaysRetainsSelectedConversationBeyondCapacityBoundary()
        {
            var raw = new MultiKeyStringStore();
            const string key = "BeeKingdom.Chat.Player.hash.Recent.v1";
            var cache = new VersionedChatRecentCache(raw, new ProtectedChatStringStore(raw, new TestDataProtector("device-key")), new SystemTextJsonBackend(), key);
            var conversations = new List<LivingHiveChatConversation>();
            for (int index = 0; index < 105; index++) conversations.Add(new LivingHiveChatConversation { ConversationId = "c" + index, Title = "Canal " + index });
            cache.Save(new ChatRecentCacheSnapshot
            {
                SelectedConversationId = "c104",
                Conversations = conversations,
                Messages = new[]
                {
                    new LivingHiveChatMessage { MessageId = "selected", ConversationId = "c104", OriginalBody = "retenu", Sequence = 4, CreatedAt = DateTimeOffset.UtcNow, Delivery = LivingHiveChatDelivery.Confirmed },
                    new LivingHiveChatMessage { MessageId = "other", ConversationId = "c1", OriginalBody = "écarté", Sequence = 5, CreatedAt = DateTimeOffset.UtcNow, Delivery = LivingHiveChatDelivery.Confirmed }
                }
            });

            ChatRecentCacheSnapshot restored = cache.Load();

            Assert.That(restored.Conversations, Has.Count.EqualTo(100));
            Assert.That(restored.Conversations.Any(item => item.ConversationId == "c104"), Is.True);
            Assert.That(restored.Messages.Single().MessageId, Is.EqualTo("selected"));
        }

        [Test]
        public async Task PlayerPartitionsKeepPendingMessagesIsolated()
        {
            var raw = new MultiKeyStringStore();
            var protectedStore = new ProtectedChatStringStore(raw, new TestDataProtector("device-key"));
            string firstKey = ChatStoragePartition.KeyPrefix("BeeKingdom.Chat", "player-one") + ".PendingSends.v1";
            string secondKey = ChatStoragePartition.KeyPrefix("BeeKingdom.Chat", "player-two") + ".PendingSends.v1";
            var first = new VersionedChatPendingSendStore(protectedStore, new SystemTextJsonBackend(), firstKey);
            var second = new VersionedChatPendingSendStore(protectedStore, new SystemTextJsonBackend(), secondKey);
            await first.SaveAsync(new PendingChatSend { ConversationId = "c1", Body = "private body", ClientRequestId = "r1", ClientCreatedAt = "2026-07-21T12:00:00Z" }, CancellationToken.None);
            Assert.That(await first.LoadAsync(CancellationToken.None), Has.Count.EqualTo(1));
            Assert.That(await second.LoadAsync(CancellationToken.None), Is.Empty);
            Assert.That(raw.Keys, Has.Count.EqualTo(1));
            Assert.That(raw.Keys.Single(), Does.Not.Contain("player-one"));
            Assert.That(raw.Values.Single(), Does.Not.Contain("private body"));
        }

        [Test]
        public async Task PendingJournalLimitsPreserveExistingEntriesAndAllowIdempotentUpdates()
        {
            var policy = new ChatPendingJournalPolicy(1);
            var json = new SystemTextJsonBackend();

            var sendsRaw = new MemoryStringStore();
            var sends = new VersionedChatPendingSendStore(sendsRaw, json, "sends", policy);
            await sends.SaveAsync(new PendingChatSend { ConversationId = "c1", ClientRequestId = "s1", Body = "one", ClientCreatedAt = "2026-07-21T12:00:00Z" }, CancellationToken.None);
            string sendsPreserved = sendsRaw.Value;
            Assert.ThrowsAsync<ChatPendingJournalFullException>(async () => await sends.SaveAsync(new PendingChatSend { ConversationId = "c1", ClientRequestId = "s2", Body = "two", ClientCreatedAt = "2026-07-21T12:00:00Z" }, CancellationToken.None));
            Assert.That(sendsRaw.Value, Is.EqualTo(sendsPreserved));
            await sends.SaveAsync(new PendingChatSend { ConversationId = "c1", ClientRequestId = "s1", Body = "updated", ClientCreatedAt = "2026-07-21T12:00:00Z", AttemptCount = 2 }, CancellationToken.None);
            Assert.That((await sends.LoadAsync(CancellationToken.None)).Single().AttemptCount, Is.EqualTo(2));

            var conversationsRaw = new MemoryStringStore();
            var conversations = new VersionedChatPendingConversationStore(conversationsRaw, json, "conversations", policy);
            await conversations.SaveAsync(new PendingChatConversationCreation { Request = new RemoteCreateConversationRequest { ClientRequestId = "c1" } }, CancellationToken.None);
            string conversationsPreserved = conversationsRaw.Value;
            Assert.ThrowsAsync<ChatPendingJournalFullException>(async () => await conversations.SaveAsync(new PendingChatConversationCreation { Request = new RemoteCreateConversationRequest { ClientRequestId = "c2" } }, CancellationToken.None));
            Assert.That(conversationsRaw.Value, Is.EqualTo(conversationsPreserved));

            var reportsRaw = new MemoryStringStore();
            var reports = new VersionedChatPendingModerationReportStore(reportsRaw, json, "reports", policy);
            await reports.SaveAsync(new PendingModerationReportRequest { ClientRequestId = "r1", MessageId = "m1", Category = "spam" }, CancellationToken.None);
            string reportsPreserved = reportsRaw.Value;
            Assert.ThrowsAsync<ChatPendingJournalFullException>(async () => await reports.SaveAsync(new PendingModerationReportRequest { ClientRequestId = "r2", MessageId = "m2", Category = "spam" }, CancellationToken.None));
            Assert.That(reportsRaw.Value, Is.EqualTo(reportsPreserved));

            var readsRaw = new MemoryStringStore();
            var reads = new VersionedChatPendingReadStore(readsRaw, json, "reads", policy);
            await reads.SaveMaximumAsync(new PendingReadCursor { ConversationId = "one", Sequence = 1 }, CancellationToken.None);
            string readsPreserved = readsRaw.Value;
            Assert.ThrowsAsync<ChatPendingJournalFullException>(async () => await reads.SaveMaximumAsync(new PendingReadCursor { ConversationId = "two", Sequence = 1 }, CancellationToken.None));
            Assert.That(readsRaw.Value, Is.EqualTo(readsPreserved));
            await reads.SaveMaximumAsync(new PendingReadCursor { ConversationId = "one", Sequence = 2 }, CancellationToken.None);
            Assert.That((await reads.LoadAsync(CancellationToken.None)).Single().Sequence, Is.EqualTo(2));
        }

        [Test]
        public void PendingJournalPolicyRejectsUnsafeCapacities()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatPendingJournalPolicy(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatPendingJournalPolicy(4097));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatPendingJournalPolicy(1, 1023));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatPendingJournalPolicy(1, 8388609));
        }

        [Test]
        public void InvalidPendingEntriesAreRejectedBeforeAnyWrite()
        {
            var json = new SystemTextJsonBackend();
            var sendsRaw = new MemoryStringStore();
            Assert.ThrowsAsync<ArgumentException>(async () => await new VersionedChatPendingSendStore(sendsRaw, json).SaveAsync(new PendingChatSend { ClientRequestId = "s1" }, CancellationToken.None));
            Assert.That(sendsRaw.Value, Is.Null);
            var conversationsRaw = new MemoryStringStore();
            Assert.ThrowsAsync<ArgumentException>(async () => await new VersionedChatPendingConversationStore(conversationsRaw, json).SaveAsync(new PendingChatConversationCreation { Request = new RemoteCreateConversationRequest { ClientRequestId = "c1", ParticipantIds = new List<string> { " " } } }, CancellationToken.None));
            Assert.That(conversationsRaw.Value, Is.Null);
            var reportsRaw = new MemoryStringStore();
            Assert.ThrowsAsync<ArgumentException>(async () => await new VersionedChatPendingModerationReportStore(reportsRaw, json).SaveAsync(new PendingModerationReportRequest { ClientRequestId = "r1", MessageId = "m1" }, CancellationToken.None));
            Assert.That(reportsRaw.Value, Is.Null);
            var readsRaw = new MemoryStringStore();
            Assert.ThrowsAsync<ArgumentException>(async () => await new VersionedChatPendingReadStore(readsRaw, json).SaveMaximumAsync(new PendingReadCursor { ConversationId = "c1", Sequence = 1, AttemptCount = -1 }, CancellationToken.None));
            Assert.That(readsRaw.Value, Is.Null);
        }

        [Test]
        public void SerializedSizeLimitPreservesExistingDataAndPreventsNetwork()
        {
            var json = new SystemTextJsonBackend();
            var raw = new MemoryStringStore();
            var sends = new VersionedChatPendingSendStore(raw, json, "s", new ChatPendingJournalPolicy(10, 1024));
            var rest = new FakeRest();
            var provider = new ServerChatProvider(rest, new FakeSession(), pendingSends: sends);
            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", new string('x', 2000), "large-send", CancellationToken.None));
            Assert.That(error.Error, Is.EqualTo(RemoteChatError.LocalQueueFull));
            Assert.That(raw.Value, Is.Null);
            Assert.That(rest.CallCount, Is.EqualTo(0));

            raw.Value = new string('z', 1025);
            string preserved = raw.Value;
            Assert.ThrowsAsync<ChatPendingStoreException>(async () => await sends.LoadAsync(CancellationToken.None));
            Assert.That(raw.Value, Is.EqualTo(preserved));
        }

        [Test]
        public async Task ProviderMapsFullLocalQueuesToSafeActionableErrors()
        {
            var policy = new ChatPendingJournalPolicy(1);
            var json = new SystemTextJsonBackend();
            var sink = new CollectingDiagnostics();

            var sends = new VersionedChatPendingSendStore(new MemoryStringStore(), json, "s", policy);
            await sends.SaveAsync(new PendingChatSend { ConversationId = "c1", ClientRequestId = "existing-send", Body = "existing secret", ClientCreatedAt = "2026-07-21T12:00:00Z" }, CancellationToken.None);
            var provider = new ServerChatProvider(new FakeRest(), new FakeSession(), pendingSends: sends, diagnostics: sink);
            RemoteChatTransportException sendError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("private-conversation", "new secret", "new-send", CancellationToken.None));

            var conversations = new VersionedChatPendingConversationStore(new MemoryStringStore(), json, "c", policy);
            await conversations.SaveAsync(new PendingChatConversationCreation { Request = new RemoteCreateConversationRequest { ClientRequestId = "existing-conversation" } }, CancellationToken.None);
            provider = new ServerChatProvider(new FakeRest(), new FakeSession(), pendingConversations: conversations, diagnostics: sink);
            RemoteChatTransportException conversationError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.CreateConversationAsync(new RemoteCreateConversationRequest { ChannelType = "Alliance", ClientRequestId = "new-conversation", Title = "private title" }, CancellationToken.None));

            var reports = new VersionedChatPendingModerationReportStore(new MemoryStringStore(), json, "r", policy);
            await reports.SaveAsync(new PendingModerationReportRequest { ClientRequestId = "existing-report", MessageId = "existing-message", Category = "spam" }, CancellationToken.None);
            provider = new ServerChatProvider(new FakeRest(), new FakeSession(), pendingReports: reports, diagnostics: sink);
            RemoteChatTransportException reportError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.ReportAsync("private-message", "private-category", "new-report", CancellationToken.None));

            var reads = new VersionedChatPendingReadStore(new MemoryStringStore(), json, "q", policy);
            await reads.SaveMaximumAsync(new PendingReadCursor { ConversationId = "existing-read", Sequence = 1 }, CancellationToken.None);
            provider = new ServerChatProvider(new FakeRest(), new FakeSession(), pendingReads: reads, diagnostics: sink);
            RemoteChatTransportException readError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.MarkReadAsync("private-read", 2, CancellationToken.None));

            foreach (RemoteChatTransportException error in new[] { sendError, conversationError, reportError, readError })
            {
                Assert.That(error.Error, Is.EqualTo(RemoteChatError.LocalQueueFull));
                Assert.That(error.ServerCode, Is.EqualTo("local_queue_full"));
                Assert.That(error.StatusCode, Is.EqualTo(0));
            }
            Assert.That(sink.Events.Select(item => item.Operation), Is.EquivalentTo(new[] { "send_message", "create_conversation", "report_message", "mark_read" }));
            Assert.That(sink.Events.All(item => item.Code == "local_queue_full" && item.Count == 1 && item.StatusCode == 0), Is.True);
            string diagnostics = JsonSerializer.Serialize(sink.Events);
            Assert.That(diagnostics, Does.Not.Contain("new secret"));
            Assert.That(diagnostics, Does.Not.Contain("private-conversation"));
            Assert.That(diagnostics, Does.Not.Contain("private title"));
            Assert.That(diagnostics, Does.Not.Contain("private-message"));
            Assert.That(diagnostics, Does.Not.Contain("private-category"));
            Assert.That(diagnostics, Does.Not.Contain("private-read"));
        }

        [Test]
        public async Task CoordinatedDrainReportsBeforeCompletedAndRemainingCounts()
        {
            var json = new SystemTextJsonBackend();
            var sends = new VersionedChatPendingSendStore(new MemoryStringStore(), json);
            var conversations = new VersionedChatPendingConversationStore(new MemoryStringStore(), json);
            var reports = new VersionedChatPendingModerationReportStore(new MemoryStringStore(), json);
            var reads = new VersionedChatPendingReadStore(new MemoryStringStore(), json);
            await conversations.SaveAsync(new PendingChatConversationCreation { Request = new RemoteCreateConversationRequest { ChannelType = "Alliance", ClientRequestId = "conversation-1" } }, CancellationToken.None);
            await sends.SaveAsync(new PendingChatSend { ConversationId = "c1", Body = "secret", ClientRequestId = "send-1", ClientCreatedAt = "2026-07-21T12:00:00Z" }, CancellationToken.None);
            await reads.SaveMaximumAsync(new PendingReadCursor { ConversationId = "c1", Sequence = 1 }, CancellationToken.None);
            await reports.SaveAsync(new PendingModerationReportRequest { MessageId = "m1", Category = "spam", ClientRequestId = "report-1" }, CancellationToken.None);
            var sink = new CollectingDiagnostics();
            var provider = new ServerChatProvider(new FakeRest(), new FakeSession(), pendingSends: sends, pendingConversations: conversations, pendingReports: reports, pendingReads: reads, diagnostics: sink);

            ChatPendingQueueStatus status = await provider.GetPendingQueueStatusAsync(CancellationToken.None);
            ChatPendingDrainResult result = await provider.DrainPendingAsync(CancellationToken.None);

            Assert.That(status.Total, Is.EqualTo(4));
            Assert.That(result.Before.Total, Is.EqualTo(4));
            Assert.That(result.Completed, Is.EqualTo(4));
            Assert.That(result.Remaining.Total, Is.EqualTo(0));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(sink.Events.Single(item => item.Code == "pending_drain_started").Count, Is.EqualTo(4));
            Assert.That(sink.Events.Single(item => item.Code == "pending_drain_completed").Count, Is.EqualTo(4));
        }

        [Test]
        public async Task CoordinatedDrainPreservesPartialResultOnTransportFailure()
        {
            var json = new SystemTextJsonBackend();
            var conversations = new VersionedChatPendingConversationStore(new MemoryStringStore(), json);
            var sends = new VersionedChatPendingSendStore(new MemoryStringStore(), json);
            await conversations.SaveAsync(new PendingChatConversationCreation { Request = new RemoteCreateConversationRequest { ChannelType = "Alliance", ClientRequestId = "private-conversation" } }, CancellationToken.None);
            await sends.SaveAsync(new PendingChatSend { ConversationId = "private-id", Body = "private-body", ClientRequestId = "private-send", ClientCreatedAt = "2026-07-21T12:00:00Z" }, CancellationToken.None);
            var rest = new FakeRest();
            rest.ResponseStatuses.Enqueue(200);
            rest.ResponseStatuses.Enqueue(503);
            var sink = new CollectingDiagnostics();
            var provider = new ServerChatProvider(rest, new FakeSession(), pendingSends: sends, pendingConversations: conversations, diagnostics: sink);

            ChatPendingDrainException error = Assert.ThrowsAsync<ChatPendingDrainException>(async () => await provider.DrainPendingAsync(CancellationToken.None));

            Assert.That(error.Result.Before.Total, Is.EqualTo(2));
            Assert.That(error.Result.Completed, Is.EqualTo(1));
            Assert.That(error.Result.Remaining.Sends, Is.EqualTo(1));
            Assert.That(error.Result.Remaining.Conversations, Is.EqualTo(0));
            ChatDiagnosticEvent diagnostic = sink.Events.Single(item => item.Code == "pending_drain_incomplete");
            Assert.That(diagnostic.Count, Is.EqualTo(1));
            Assert.That(diagnostic.Error, Is.EqualTo(RemoteChatError.Transport));
            string serialized = JsonSerializer.Serialize(sink.Events);
            Assert.That(serialized, Does.Not.Contain("private-body"));
            Assert.That(serialized, Does.Not.Contain("private-id"));
            Assert.That(serialized, Does.Not.Contain("private-send"));
            Assert.That(serialized, Does.Not.Contain("private-conversation"));
        }

        [Test]
        public void CorruptedPendingDataBecomesSafeLocalStorageErrorWithoutNetworkOrDeletion()
        {
            var raw = new MemoryStringStore { Value = "private-corrupted-value" };
            var sends = new VersionedChatPendingSendStore(raw, new SystemTextJsonBackend());
            var rest = new FakeRest();
            var sink = new CollectingDiagnostics();
            var provider = new ServerChatProvider(rest, new FakeSession(), pendingSends: sends, diagnostics: sink);

            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("private-conversation", "private-body", "private-request", CancellationToken.None));

            Assert.That(error.Error, Is.EqualTo(RemoteChatError.LocalStorageUnavailable));
            Assert.That(error.ServerCode, Is.EqualTo("local_storage_unavailable"));
            Assert.That(error.StatusCode, Is.EqualTo(0));
            Assert.That(raw.Value, Is.EqualTo("private-corrupted-value"));
            Assert.That(rest.CallCount, Is.EqualTo(0));
            string diagnostics = JsonSerializer.Serialize(sink.Events);
            Assert.That(diagnostics, Does.Contain("local_storage_unavailable"));
            Assert.That(diagnostics, Does.Not.Contain("private-corrupted-value"));
            Assert.That(diagnostics, Does.Not.Contain("private-conversation"));
            Assert.That(diagnostics, Does.Not.Contain("private-body"));
            Assert.That(diagnostics, Does.Not.Contain("private-request"));
        }

        [Test]
        public async Task WrongProtectionKeyAndWriteFailureBecomeLocalStorageErrors()
        {
            var raw = new MemoryStringStore();
            var json = new SystemTextJsonBackend();
            var original = new VersionedChatPendingSendStore(new ProtectedChatStringStore(raw, new TestDataProtector("key-one")), json);
            await original.SaveAsync(new PendingChatSend { ConversationId = "c1", ClientRequestId = "private-existing", Body = "private-existing-body", ClientCreatedAt = "2026-07-21T12:00:00Z" }, CancellationToken.None);
            string preserved = raw.Value;
            var rest = new FakeRest();
            var wrongKey = new VersionedChatPendingSendStore(new ProtectedChatStringStore(raw, new TestDataProtector("key-two")), json);
            var provider = new ServerChatProvider(rest, new FakeSession(), pendingSends: wrongKey);
            RemoteChatTransportException readError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "new", "new-request", CancellationToken.None));
            Assert.That(readError.Error, Is.EqualTo(RemoteChatError.LocalStorageUnavailable));
            Assert.That(raw.Value, Is.EqualTo(preserved));
            Assert.That(rest.CallCount, Is.EqualTo(0));

            var failing = new VersionedChatPendingSendStore(new ProtectedChatStringStore(new ThrowingStringStore(), new TestDataProtector("key")), json);
            provider = new ServerChatProvider(rest, new FakeSession(), pendingSends: failing);
            RemoteChatTransportException writeError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.SendAsync("c1", "new", "write-request", CancellationToken.None));
            Assert.That(writeError.Error, Is.EqualTo(RemoteChatError.LocalStorageUnavailable));
            Assert.That(rest.CallCount, Is.EqualTo(0));
        }

        [Test]
        public void PendingRecoveryQuarantinesVerifiesAndRestoresEncryptedValues()
        {
            var raw = new MultiKeyStringStore();
            const string prefix = "BeeKingdom.Chat.Player.partition";
            string sendKey = prefix + ".PendingSends.v1";
            string readKey = prefix + ".PendingReads.v1";
            raw.Write(sendKey, "encrypted-send-envelope");
            raw.Write(readKey, "encrypted-read-envelope");
            var recovery = new ChatPendingPartitionRecovery(raw, prefix);
            const string recoveryId = "11111111111111111111111111111111";

            ChatPendingRecoveryReceipt quarantined = recovery.QuarantineAndReset(recoveryId);
            Assert.That(quarantined.EntryFiles, Is.EqualTo(2));
            Assert.That(quarantined.SourceCleared, Is.True);
            Assert.That(quarantined.BackupRetained, Is.True);
            Assert.That(raw.Read(sendKey), Is.Null);
            Assert.That(raw.Read(readKey), Is.Null);
            Assert.That(raw.Values, Does.Contain("encrypted-send-envelope"));
            Assert.That(raw.Values, Does.Contain("encrypted-read-envelope"));

            ChatPendingRecoveryReceipt restored = recovery.Restore(recoveryId);
            Assert.That(restored.EntryFiles, Is.EqualTo(2));
            Assert.That(restored.BackupRetained, Is.False);
            Assert.That(raw.Read(sendKey), Is.EqualTo("encrypted-send-envelope"));
            Assert.That(raw.Read(readKey), Is.EqualTo("encrypted-read-envelope"));
            Assert.That(raw.Keys.Any(key => key.Contains(".Recovery.v1.", StringComparison.Ordinal)), Is.False);
        }

        [Test]
        public void PendingRecoveryNeverOverwritesNewDataAndCopyFailurePreservesSource()
        {
            var raw = new MultiKeyStringStore();
            const string prefix = "BeeKingdom.Chat.Player.partition";
            string sendKey = prefix + ".PendingSends.v1";
            const string recoveryId = "22222222222222222222222222222222";
            raw.Write(sendKey, "old-envelope");
            var recovery = new ChatPendingPartitionRecovery(raw, prefix);
            recovery.QuarantineAndReset(recoveryId);
            raw.Write(sendKey, "new-envelope");
            ChatPendingRecoveryException conflict = Assert.Throws<ChatPendingRecoveryException>(() => recovery.Restore(recoveryId));
            Assert.That(conflict.SourcePreserved, Is.True);
            Assert.That(conflict.BackupRetained, Is.True);
            Assert.That(raw.Read(sendKey), Is.EqualTo("new-envelope"));
            Assert.That(raw.Values, Does.Contain("old-envelope"));

            var failing = new FailingBackupStringStore();
            failing.Write(sendKey, "source-envelope");
            ChatPendingRecoveryException copyFailure = Assert.Throws<ChatPendingRecoveryException>(() => new ChatPendingPartitionRecovery(failing, prefix).QuarantineAndReset("33333333333333333333333333333333"));
            Assert.That(copyFailure.SourcePreserved, Is.True);
            Assert.That(failing.Read(sendKey), Is.EqualTo("source-envelope"));
        }

        [Test]
        public void RestoredJournalsRejectOversizeInvalidAndDuplicateEntriesWithoutRewrite()
        {
            var json = new SystemTextJsonBackend();
            var sendsRaw = new MemoryStringStore { Value = "{\"schemaVersion\":1,\"items\":[{\"schemaVersion\":1,\"conversationId\":\"c1\",\"body\":\"a\",\"clientRequestId\":\"s1\",\"clientCreatedAt\":\"2026-07-21T12:00:00Z\",\"attemptCount\":0},{\"schemaVersion\":1,\"conversationId\":\"c1\",\"body\":\"b\",\"clientRequestId\":\"s2\",\"clientCreatedAt\":\"2026-07-21T12:00:00Z\",\"attemptCount\":0}]}" };
            string sendsPreserved = sendsRaw.Value;
            Assert.ThrowsAsync<ChatPendingStoreException>(async () => await new VersionedChatPendingSendStore(sendsRaw, json, "s", new ChatPendingJournalPolicy(1)).LoadAsync(CancellationToken.None));
            Assert.That(sendsRaw.Value, Is.EqualTo(sendsPreserved));

            var conversationsRaw = new MemoryStringStore { Value = "{\"schemaVersion\":1,\"items\":[{\"schemaVersion\":1,\"attemptCount\":0,\"clientRequestId\":\"\",\"participantIds\":[]}]}" };
            string conversationsPreserved = conversationsRaw.Value;
            Assert.ThrowsAsync<ChatPendingStoreException>(async () => await new VersionedChatPendingConversationStore(conversationsRaw, json).LoadAsync(CancellationToken.None));
            Assert.That(conversationsRaw.Value, Is.EqualTo(conversationsPreserved));

            var reportsRaw = new MemoryStringStore { Value = "{\"schemaVersion\":1,\"items\":[{\"schemaVersion\":1,\"messageId\":\"m1\",\"category\":\"spam\",\"clientRequestId\":\"same\",\"attemptCount\":0},{\"schemaVersion\":1,\"messageId\":\"m2\",\"category\":\"spam\",\"clientRequestId\":\"same\",\"attemptCount\":0}]}" };
            string reportsPreserved = reportsRaw.Value;
            Assert.ThrowsAsync<ChatPendingStoreException>(async () => await new VersionedChatPendingModerationReportStore(reportsRaw, json).LoadAsync(CancellationToken.None));
            Assert.That(reportsRaw.Value, Is.EqualTo(reportsPreserved));

            var readsRaw = new MemoryStringStore { Value = "{\"schemaVersion\":1,\"items\":[{\"schemaVersion\":1,\"conversationId\":\"c1\",\"sequence\":-1,\"attemptCount\":0}]}" };
            string readsPreserved = readsRaw.Value;
            Assert.ThrowsAsync<ChatPendingStoreException>(async () => await new VersionedChatPendingReadStore(readsRaw, json).LoadAsync(CancellationToken.None));
            Assert.That(readsRaw.Value, Is.EqualTo(readsPreserved));
        }

        [Test]
        public async Task QuarantineIsExclusiveAgainstConcurrentJournalWrites()
        {
            const string prefix = "BeeKingdom.Chat.Player.partition";
            var raw = new BlockingBackupStringStore();
            var partitionGate = new ChatPersistenceGate();
            var journal = new VersionedChatPendingSendStore(raw, new SystemTextJsonBackend(), prefix + ".PendingSends.v1", persistenceGate: partitionGate);
            await journal.SaveAsync(new PendingChatSend { ConversationId = "c1", Body = "old", ClientRequestId = "old", ClientCreatedAt = "2026-07-21T12:00:00Z" }, CancellationToken.None);
            var recovery = new ChatPendingPartitionRecovery(raw, prefix, partitionGate);

            Task<ChatPendingRecoveryReceipt> quarantine = Task.Run(() => recovery.QuarantineAndReset("44444444444444444444444444444444"));
            Assert.That(raw.BackupStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);
            Task save = journal.SaveAsync(new PendingChatSend { ConversationId = "c1", Body = "new", ClientRequestId = "new", ClientCreatedAt = "2026-07-21T12:01:00Z" }, CancellationToken.None);
            Task first = await Task.WhenAny(save, Task.Delay(100));
            Assert.That(first, Is.Not.SameAs(save), "The write must wait while quarantine owns the partition gate.");

            raw.ReleaseBackup.Set();
            ChatPendingRecoveryReceipt receipt = await quarantine;
            await save;
            IReadOnlyList<PendingChatSend> active = await journal.LoadAsync(CancellationToken.None);
            Assert.That(receipt.EntryFiles, Is.EqualTo(1));
            Assert.That(active.Select(item => item.ClientRequestId), Is.EqualTo(new[] { "new" }));
            Assert.That(raw.Values.Any(value => value.Contains("\"clientRequestId\":\"old\"", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public async Task ExpiredPendingOperationsArePreservedAndNeverReachNetwork()
        {
            var json = new SystemTextJsonBackend();
            var rest = new FakeRest();
            var sink = new CollectingDiagnostics();
            var clock = new FakeClock(new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero));
            var policy = new ChatPendingReplayPolicy(TimeSpan.FromDays(7));
            const string old = "2026-07-01T12:00:00Z";

            var conversations = new VersionedChatPendingConversationStore(new MemoryStringStore(), json);
            await conversations.SaveAsync(new PendingChatConversationCreation { EnqueuedAtUtc = old, Request = new RemoteCreateConversationRequest { ClientRequestId = "private-conversation" } }, CancellationToken.None);
            var provider = new ServerChatProvider(rest, new FakeSession(), pendingConversations: conversations, diagnostics: sink, replayPolicy: policy, clock: clock);
            RemoteChatTransportException conversationError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.RetryPendingConversationsAsync(CancellationToken.None));

            var sends = new VersionedChatPendingSendStore(new MemoryStringStore(), json);
            await sends.SaveAsync(new PendingChatSend { ConversationId = "private-id", Body = "private-body", ClientRequestId = "private-send", ClientCreatedAt = old }, CancellationToken.None);
            provider = new ServerChatProvider(rest, new FakeSession(), pendingSends: sends, diagnostics: sink, replayPolicy: policy, clock: clock);
            RemoteChatTransportException sendError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.RetryPendingAsync(CancellationToken.None));

            var reads = new VersionedChatPendingReadStore(new MemoryStringStore(), json);
            await reads.SaveMaximumAsync(new PendingReadCursor { ConversationId = "private-read", Sequence = 2, EnqueuedAtUtc = old }, CancellationToken.None);
            provider = new ServerChatProvider(rest, new FakeSession(), pendingReads: reads, diagnostics: sink, replayPolicy: policy, clock: clock);
            RemoteChatTransportException readError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.RetryPendingReadsAsync(CancellationToken.None));

            var reports = new VersionedChatPendingModerationReportStore(new MemoryStringStore(), json);
            await reports.SaveAsync(new PendingModerationReportRequest { MessageId = "private-message", Category = "private-category", ClientRequestId = "private-report", EnqueuedAtUtc = old }, CancellationToken.None);
            provider = new ServerChatProvider(rest, new FakeSession(), pendingReports: reports, diagnostics: sink, replayPolicy: policy, clock: clock);
            RemoteChatTransportException reportError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await provider.RetryPendingReportsAsync(CancellationToken.None));

            foreach (RemoteChatTransportException error in new[] { conversationError, sendError, readError, reportError })
            {
                Assert.That(error.Error, Is.EqualTo(RemoteChatError.LocalOperationExpired));
                Assert.That(error.ServerCode, Is.EqualTo("local_operation_expired"));
                Assert.That(error.StatusCode, Is.EqualTo(0));
            }
            Assert.That(rest.CallCount, Is.EqualTo(0));
            Assert.That((await conversations.LoadAsync(CancellationToken.None)), Has.Count.EqualTo(1));
            Assert.That((await sends.LoadAsync(CancellationToken.None)), Has.Count.EqualTo(1));
            Assert.That((await reads.LoadAsync(CancellationToken.None)), Has.Count.EqualTo(1));
            Assert.That((await reports.LoadAsync(CancellationToken.None)), Has.Count.EqualTo(1));
            string diagnostics = JsonSerializer.Serialize(sink.Events);
            Assert.That(sink.Events.Count(item => item.Code == "local_operation_expired"), Is.EqualTo(4));
            Assert.That(diagnostics, Does.Not.Contain("private-body"));
            Assert.That(diagnostics, Does.Not.Contain("private-id"));
            Assert.That(diagnostics, Does.Not.Contain("private-message"));
            Assert.That(diagnostics, Does.Not.Contain("private-category"));
        }

        [Test]
        public void PendingReplayPolicyStaysBelowServerReceiptRetention()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatPendingReplayPolicy(TimeSpan.FromMinutes(59)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatPendingReplayPolicy(TimeSpan.FromDays(30)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChatPendingReplayPolicy(TimeSpan.FromDays(7), TimeSpan.FromHours(2)));
        }

        [Test]
        public void ProtectedStoreNeverWritesPlaintextAndRoundTrips()
        {
            var raw = new MemoryStringStore();
            var store = new ProtectedChatStringStore(raw, new TestDataProtector("device-key"));
            store.Write("outbox", "secret-body");
            Assert.That(raw.Value, Does.Not.Contain("secret-body"));
            Assert.That(store.Read("outbox"), Is.EqualTo("secret-body"));
        }

        [Test]
        public void ProtectedStoreRejectsTamperAndWrongKeyWithoutDeleting()
        {
            var raw = new MemoryStringStore();
            new ProtectedChatStringStore(raw, new TestDataProtector("key-one")).Write("outbox", "secret-body");
            string preserved = raw.Value;
            Assert.Throws<ChatProtectedStoreException>(() => new ProtectedChatStringStore(raw, new TestDataProtector("key-two")).Read("outbox"));
            Assert.That(raw.Value, Is.EqualTo(preserved));
            raw.Value = preserved.Substring(0, preserved.Length - 1) + (preserved.EndsWith("A", StringComparison.Ordinal) ? "B" : "A");
            string tampered = raw.Value;
            Assert.Throws<ChatProtectedStoreException>(() => new ProtectedChatStringStore(raw, new TestDataProtector("key-one")).Read("outbox"));
            Assert.That(raw.Value, Is.EqualTo(tampered));
        }

        [Test]
        public void ProtectedStoreRejectsNoOpProtectorBeforeWrite()
        {
            var raw = new MemoryStringStore();
            Assert.Throws<ChatProtectedStoreException>(() => new ProtectedChatStringStore(raw, new NoOpDataProtector()).Write("outbox", "secret-body"));
            Assert.That(raw.Value, Is.Null);
        }

        [Test]
        public async Task VersionedJournalRoundTripsAndDeletesLastAcknowledgedSend()
        {
            var strings = new MemoryStringStore();
            var journal = new VersionedChatPendingSendStore(strings, new SystemTextJsonBackend(), "outbox");
            await journal.SaveAsync(new PendingChatSend { ConversationId = "c1", Body = "persist", ClientRequestId = "r1", ClientCreatedAt = "2026-07-21T12:00:00Z", AttemptCount = 2 }, CancellationToken.None);
            var restarted = new VersionedChatPendingSendStore(strings, new SystemTextJsonBackend(), "outbox");
            IReadOnlyList<PendingChatSend> loaded = await restarted.LoadAsync(CancellationToken.None);
            Assert.That(loaded, Has.Count.EqualTo(1));
            Assert.That(loaded[0].AttemptCount, Is.EqualTo(2));
            await restarted.RemoveAsync("r1", CancellationToken.None);
            Assert.That(await restarted.LoadAsync(CancellationToken.None), Is.Empty);
            Assert.That(strings.Value, Is.Null);
        }

        [Test]
        public void CorruptedJournalIsPreservedAndReported()
        {
            var strings = new MemoryStringStore { Value = "{broken" };
            var journal = new VersionedChatPendingSendStore(strings, new SystemTextJsonBackend(), "outbox");
            Assert.ThrowsAsync<ChatPendingStoreException>(async () => await journal.LoadAsync(CancellationToken.None));
            Assert.That(strings.Value, Is.EqualTo("{broken"));
        }

        [Test]
        public async Task TranslationErrorKeepsOriginalAndOriginalToggleIsPermanent()
        {
            var transport = new FakeRest { TranslationStatusCode = 503 };
            var controller = new ChatTranslationController(NewProvider(transport));
            RemoteChatMessage message = Message(1, "r1");
            TranslationDisplayState failed = await controller.TranslateAsync(message, "fr-CA", "v1", CancellationToken.None);
            Assert.That(failed.Mode, Is.EqualTo(TranslationDisplayMode.Error));
            Assert.That(failed.VisibleText, Is.EqualTo("body"));
            Assert.That(failed.Error, Is.EqualTo(RemoteChatError.Transport));

            transport.TranslationStatusCode = 200;
            TranslationDisplayState translated = await controller.TranslateAsync(message, "fr-CA", "v1", CancellationToken.None);
            Assert.That(translated.Mode, Is.EqualTo(TranslationDisplayMode.Translated));
            TranslationDisplayState original = controller.ShowOriginal(message);
            Assert.That(original.Mode, Is.EqualTo(TranslationDisplayMode.Original));
            Assert.That(original.VisibleText, Is.EqualTo("body"));
        }

        [Test]
        public void TranslationCancellationRestoresOriginal()
        {
            var transport = new FakeRest();
            var controller = new ChatTranslationController(NewProvider(transport));
            RemoteChatMessage message = Message(1, "r1");
            var cts = new CancellationTokenSource(); cts.Cancel();
            Assert.ThrowsAsync<OperationCanceledException>(async () => await controller.TranslateAsync(message, "fr-CA", "v1", cts.Token));
            Assert.That(controller.Get(message).Mode, Is.EqualTo(TranslationDisplayMode.Original));
            Assert.That(controller.Get(message).VisibleText, Is.EqualTo("body"));
        }

        [Test]
        public async Task RealtimeGapTriggersRestBeforeSequenceIsConfirmed()
        {
            var transport = new FakeRest();
            transport.Page.Items.Add(Message(1, "r1"));
            transport.Page.Items.Add(Message(2, "r2"));
            var provider = NewProvider(transport);
            IReadOnlyList<RemoteChatMessage> snapshot = await provider.ApplyRealtimeEventAsync(new RemoteChatEvent { ConversationId = "c1", Sequence = 3, Message = Message(3, "r3") }, CancellationToken.None);
            Assert.That(snapshot, Has.Count.EqualTo(3));
            Assert.That(provider.GetConfirmedSequence("c1"), Is.EqualTo(3));
            Assert.That(transport.CallCount, Is.EqualTo(1));
        }

        [Test]
        public async Task OutOfOrderRealtimeEventsRemainUnconfirmedUntilGapArrives()
        {
            var transport = new FakeRest();
            var provider = NewProvider(transport);
            await provider.ApplyRealtimeEventAsync(new RemoteChatEvent { ConversationId = "c1", Sequence = 2, Message = Message(2, "r2") }, CancellationToken.None);
            Assert.That(provider.GetConfirmedSequence("c1"), Is.EqualTo(0));
            IReadOnlyList<RemoteChatMessage> snapshot = await provider.ApplyRealtimeEventAsync(new RemoteChatEvent { ConversationId = "c1", Sequence = 1, Message = Message(1, "r1") }, CancellationToken.None);
            Assert.That(provider.GetConfirmedSequence("c1"), Is.EqualTo(2));
            Assert.That(snapshot, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task RealtimeEventsQueuedBeforeLogoutCannotMergeAfterDisconnect()
        {
            var rest = new BlockingMessageTransport();
            var sessions = new SignallingSession(3);
            var provider = new ServerChatProvider(rest, sessions);
            Task<IReadOnlyList<RemoteChatMessage>> gap = provider.ApplyRealtimeEventAsync(new RemoteChatEvent { ConversationId = "c1", Sequence = 2, Message = Message(2, "r2") }, CancellationToken.None);
            await rest.Started.Task;
            Task<IReadOnlyList<RemoteChatMessage>> queued = provider.ApplyRealtimeEventAsync(new RemoteChatEvent { ConversationId = "c1", Sequence = 1, Message = Message(1, "r1") }, CancellationToken.None);
            await sessions.ThresholdReached.Task;

            await provider.DisconnectAsync(CancellationToken.None);
            rest.Release.TrySetResult(true);

            RemoteChatTransportException gapError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await gap);
            RemoteChatTransportException queuedError = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await queued);
            Assert.That(gapError.ServerCode, Is.EqualTo("local_session_changed"));
            Assert.That(queuedError.ServerCode, Is.EqualTo("local_session_changed"));
            Assert.That(provider.GetConfirmedSequence("c1"), Is.Zero);
        }

        [Test]
        public async Task LivingHiveControllerShowsOnlyServerConversationsAndBoundedHistory()
        {
            var rest = new FakeRest();
            rest.ConversationPages.Enqueue(new RemoteConversationPage { Items = new List<RemoteConversation> { new RemoteConversation { ConversationId = "c1", Title = "Alliance des saules", ChannelType = "Alliance", LastSequence = 30, UnreadCount = 7 } } });
            var page = new RemoteMessagePage();
            for (int sequence = 1; sequence <= 30; sequence++) page.Items.Add(new RemoteChatMessage { MessageId = "m" + sequence, ConversationId = "c1", Sequence = sequence, ClientRequestId = "r" + sequence, SenderId = "p2", SenderDisplayName = "Abeille " + sequence, OriginalBody = "message " + sequence });
            rest.MessagePages.Enqueue(page);
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new FakeSession()), 20);

            await controller.OpenAsync(CancellationToken.None);
            LivingHiveChatSnapshot snapshot = controller.Snapshot();

            Assert.That(snapshot.Status, Is.EqualTo(LivingHiveChatStatus.Polling));
            Assert.That(snapshot.Conversations, Has.Count.EqualTo(1));
            Assert.That(snapshot.Conversations[0].ChannelType, Is.EqualTo("Alliance"));
            Assert.That(snapshot.Conversations[0].UnreadCount, Is.Zero);
            Assert.That(snapshot.Messages, Has.Count.EqualTo(20));
            Assert.That(snapshot.Messages[0].Sequence, Is.EqualTo(11));
            Assert.That(snapshot.LastMessage.SenderDisplayName, Is.EqualTo("Abeille 30"));
            await controller.CloseAsync(CancellationToken.None);
        }

        [Test]
        public async Task LivingHiveControllerRestoresProtectedRecentCacheWhenServerIsOffline()
        {
            var raw = new MultiKeyStringStore();
            const string key = "BeeKingdom.Chat.Player.hash.Recent.v1";
            var cache = new VersionedChatRecentCache(raw, new ProtectedChatStringStore(raw, new TestDataProtector("device-key")), new SystemTextJsonBackend(), key);
            cache.Save(new ChatRecentCacheSnapshot
            {
                SelectedConversationId = "c1",
                Conversations = new[] { new LivingHiveChatConversation { ConversationId = "c1", Title = "Alliance", LastSequence = 4 } },
                Messages = new[] { new LivingHiveChatMessage { MessageId = "m4", ConversationId = "c1", ClientRequestId = "r4", OriginalBody = "dernier confirmé", VisibleBody = "dernier confirmé", Sequence = 4, CreatedAt = DateTimeOffset.UtcNow, Delivery = LivingHiveChatDelivery.Confirmed } }
            });
            var rest = new FakeRest { StatusCode = 503 };
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new FakeSession()), recentCache: cache);

            await controller.OpenAsync(CancellationToken.None);
            LivingHiveChatSnapshot snapshot = controller.Snapshot();

            Assert.That(snapshot.Status, Is.EqualTo(LivingHiveChatStatus.Offline));
            Assert.That(snapshot.Conversations.Single().ConversationId, Is.EqualTo("c1"));
            Assert.That(snapshot.LastMessage.OriginalBody, Is.EqualTo("dernier confirmé"));
            await controller.CloseAsync(CancellationToken.None);
        }

        [Test]
        public async Task LivingHiveControllerReconcilesRestoredCacheWithServerAuthority()
        {
            var raw = new MultiKeyStringStore();
            const string key = "BeeKingdom.Chat.Player.hash.Recent.v1";
            var cache = new VersionedChatRecentCache(raw, new ProtectedChatStringStore(raw, new TestDataProtector("device-key")), new SystemTextJsonBackend(), key);
            cache.Save(new ChatRecentCacheSnapshot { SelectedConversationId = "c1", Conversations = new[] { new LivingHiveChatConversation { ConversationId = "c1", Title = "Ancien", LastSequence = 1 } }, Messages = new[] { new LivingHiveChatMessage { MessageId = "m1", ConversationId = "c1", OriginalBody = "ancien", VisibleBody = "ancien", Sequence = 1, CreatedAt = DateTimeOffset.UtcNow, Delivery = LivingHiveChatDelivery.Confirmed } } });
            var rest = new FakeRest();
            rest.ConversationPages.Enqueue(new RemoteConversationPage { Items = new List<RemoteConversation> { new RemoteConversation { ConversationId = "c1", Title = "Serveur", LastSequence = 2 } } });
            rest.MessagePages.Enqueue(new RemoteMessagePage { Items = new List<RemoteChatMessage> { new RemoteChatMessage { MessageId = "m2", ConversationId = "c1", ClientRequestId = "r2", SenderId = "p2", OriginalBody = "autorité serveur", Sequence = 2, CreatedAt = DateTimeOffset.UtcNow } } });
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new FakeSession()), recentCache: cache);

            await controller.OpenAsync(CancellationToken.None);
            LivingHiveChatSnapshot snapshot = controller.Snapshot();

            Assert.That(snapshot.Conversations.Single().Title, Is.EqualTo("Serveur"));
            Assert.That(snapshot.Messages.Single().OriginalBody, Is.EqualTo("autorité serveur"));
            Assert.That(cache.Load().Messages.Single().Sequence, Is.EqualTo(2));
            await controller.CloseAsync(CancellationToken.None);
        }

        [Test]
        public async Task LivingHiveControllerKeepsOptimisticMessageQueuedWhenServerIsOffline()
        {
            var rest = new FakeRest();
            rest.ConversationPages.Enqueue(new RemoteConversationPage { Items = new List<RemoteConversation> { new RemoteConversation { ConversationId = "c1", Title = "Alliance", ChannelType = "Alliance" } } });
            rest.MessagePages.Enqueue(new RemoteMessagePage());
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new FakeSession(), pendingSends: new FakePendingStore()));
            await controller.OpenAsync(CancellationToken.None);
            rest.StatusCode = 503;

            await controller.SendAsync("Besoin de renfort", CancellationToken.None);
            LivingHiveChatSnapshot snapshot = controller.Snapshot();

            Assert.That(snapshot.Status, Is.EqualTo(LivingHiveChatStatus.Offline));
            Assert.That(snapshot.PendingCount, Is.EqualTo(1));
            Assert.That(snapshot.Messages, Has.Count.EqualTo(1));
            Assert.That(snapshot.Messages[0].OriginalBody, Is.EqualTo("Besoin de renfort"));
            Assert.That(snapshot.Messages[0].Delivery, Is.EqualTo(LivingHiveChatDelivery.Queued));
            await controller.CloseAsync(CancellationToken.None);
        }

        [Test]
        public async Task LivingHiveControllerReceivesAppliedRealtimeMessageWithoutUserRefresh()
        {
            var rest = new FakeRest();
            rest.ConversationPages.Enqueue(new RemoteConversationPage { Items = new List<RemoteConversation> { new RemoteConversation { ConversationId = "c1", Title = "Alliance", ChannelType = "Alliance" } } });
            rest.MessagePages.Enqueue(new RemoteMessagePage());
            var provider = new ServerChatProvider(rest, new FakeSession());
            var controller = new LivingHiveChatController(provider);
            await controller.OpenAsync(CancellationToken.None);

            await provider.ApplyRealtimeEventAsync(new RemoteChatEvent { ConversationId = "c1", Sequence = 1, Message = new RemoteChatMessage { MessageId = "m1", ConversationId = "c1", Sequence = 1, ClientRequestId = "r1", SenderId = "p2", SenderDisplayName = "Butineuse", OriginalBody = "Ruche sécurisée" } }, CancellationToken.None);
            await controller.AwaitRealtimeReceiptsAsync();
            LivingHiveChatSnapshot snapshot = controller.Snapshot();

            Assert.That(snapshot.Messages, Has.Count.EqualTo(1));
            Assert.That(snapshot.LastMessage.VisibleBody, Is.EqualTo("Ruche sécurisée"));
            Assert.That(snapshot.LastMessage.SenderDisplayName, Is.EqualTo("Butineuse"));
            Assert.That(rest.LastReadRequest.Sequence, Is.EqualTo(1));
            await controller.CloseAsync(CancellationToken.None);
        }

        [Test]
        public async Task LivingHiveControllerReopensWithoutSecondRealtimeConnectionOrPollingLoop()
        {
            var rest = new FakeRest();
            rest.ConversationPages.Enqueue(new RemoteConversationPage());
            rest.ConversationPages.Enqueue(new RemoteConversationPage());
            var realtime = new FakeRealtime();
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new FakeSession(), realtime));

            await controller.OpenAsync(CancellationToken.None);
            await controller.OpenAsync(CancellationToken.None);

            Assert.That(realtime.ConnectCount, Is.EqualTo(1));
            Assert.That(controller.Snapshot().Status, Is.EqualTo(LivingHiveChatStatus.Online));
            await controller.CloseAsync(CancellationToken.None);
        }

        [Test]
        public async Task LivingHiveControllerReopensWithoutStartingSecondPollingLoop()
        {
            var rest = new FakeRest { Capabilities = ValidCapabilities(realtime: false) };
            rest.ConversationPages.Enqueue(new RemoteConversationPage());
            rest.ConversationPages.Enqueue(new RemoteConversationPage());
            var delay = new BlockingCountingDelay();
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new FakeSession()), delay: delay);

            await controller.OpenAsync(CancellationToken.None);
            await controller.OpenAsync(CancellationToken.None);
            await Task.Yield();

            Assert.That(delay.Count, Is.EqualTo(1));
            Assert.That(controller.Snapshot().Status, Is.EqualTo(LivingHiveChatStatus.Polling));
            await controller.CloseAsync(CancellationToken.None);
        }

        [Test]
        public async Task LivingHiveBootstrapComposesExactPlayerPartitionAndPurgesOnLogout()
        {
            var bootstrap = new LivingHiveChatBootstrap();
            var options = new RemoteChatClientOptions { BaseUrl = "https://chat.example.test/chat/v1", StoragePartitionId = "p1" };
            await bootstrap.ActivateAsync(options, new FixedSession(new ChatSession("p1", "valid-token")), new MemoryStringStore(), new NoOpDataProtector());
            Assert.That(LivingHiveChatRuntime.IsConfigured, Is.True);

            await bootstrap.LogoutAsync();
            Assert.That(LivingHiveChatRuntime.IsConfigured, Is.False);

            RemoteChatTransportException mismatch = Assert.ThrowsAsync<RemoteChatTransportException>(async () => await bootstrap.ActivateAsync(options, new FixedSession(new ChatSession("p2", "other-token")), new MemoryStringStore(), new NoOpDataProtector()));
            Assert.That(mismatch.Error, Is.EqualTo(RemoteChatError.LocalAccountMismatch));
            Assert.That(LivingHiveChatRuntime.IsConfigured, Is.False);
        }

        [Test]
        public void SessionCoordinatorRejectsPreparationBeforeBootstrapActivation()
        {
            var bootstrap = new RecordingChatBootstrap();
            using var coordinator = new LivingHiveChatSessionCoordinator(bootstrap);
            var binding = ChatBinding("p1", new MutableSession(new ChatSession("p1", "valid-token")));

            RemoteChatTransportException error = Assert.ThrowsAsync<RemoteChatTransportException>(async () =>
                await coordinator.SessionAvailableAsync(new FixedAccountReadiness(false), binding));

            Assert.That(error.Error, Is.EqualTo(RemoteChatError.Disabled));
            Assert.That(error.ServerCode, Is.EqualTo("account_session_not_ready"));
            Assert.That(bootstrap.ActivateCount, Is.Zero);
            Assert.That(bootstrap.LogoutCount, Is.EqualTo(1));
        }

        [Test]
        public void SessionReadinessAdapterReflectsLiveShellGateWithoutAssemblyCoupling()
        {
            bool ready = false;
            var adapter = new DelegateChatAccountSessionReadiness(() => ready);
            Assert.That(adapter.CanSubmitLogin, Is.False);
            ready = true;
            Assert.That(adapter.CanSubmitLogin, Is.True);
        }

        [Test]
        public async Task SessionCoordinatorKeepsOneActivationForLiveTokenSourceOfSamePlayer()
        {
            var bootstrap = new RecordingChatBootstrap();
            using var coordinator = new LivingHiveChatSessionCoordinator(bootstrap);
            var sessions = new MutableSession(new ChatSession("p1", "first-token"));
            var binding = ChatBinding("p1", sessions);
            var ready = new FixedAccountReadiness(true);

            await coordinator.SessionAvailableAsync(ready, binding);
            sessions.Current = new ChatSession("p1", "refreshed-token");
            await coordinator.SessionAvailableAsync(ready, binding);

            Assert.That(bootstrap.ActivateCount, Is.EqualTo(1));
            Assert.That(bootstrap.LastSessions, Is.SameAs(sessions));
            Assert.That(bootstrap.LogoutCount, Is.Zero);
            await coordinator.SessionEndedAsync();
            Assert.That(bootstrap.LogoutCount, Is.EqualTo(1));
        }

        [Test]
        public async Task SessionCoordinatorReplacesChangedBindingForSamePlayerInsteadOfKeepingStaleTokenSource()
        {
            var bootstrap = new RecordingChatBootstrap();
            using var coordinator = new LivingHiveChatSessionCoordinator(bootstrap);
            var ready = new FixedAccountReadiness(true);
            var firstSessions = new MutableSession(new ChatSession("p1", "first-token"));
            var replacementSessions = new MutableSession(new ChatSession("p1", "replacement-token"));

            await coordinator.SessionAvailableAsync(ready, ChatBinding("p1", firstSessions));
            await coordinator.SessionAvailableAsync(ready, ChatBinding("p1", replacementSessions));

            Assert.That(bootstrap.Operations, Is.EqualTo(new[] { "activate:p1", "logout", "activate:p1" }));
            Assert.That(bootstrap.LastSessions, Is.SameAs(replacementSessions));
            Assert.That(bootstrap.PublishedActivationCount, Is.EqualTo(2));
            await coordinator.SessionEndedAsync();
        }

        [Test]
        public async Task SessionCoordinatorClosesPlayerABeforeActivatingPlayerB()
        {
            var bootstrap = new RecordingChatBootstrap();
            using var coordinator = new LivingHiveChatSessionCoordinator(bootstrap);
            var ready = new FixedAccountReadiness(true);

            await coordinator.SessionAvailableAsync(ready, ChatBinding("player-a", new MutableSession(new ChatSession("player-a", "token-a"))));
            await coordinator.SessionAvailableAsync(ready, ChatBinding("player-b", new MutableSession(new ChatSession("player-b", "token-b"))));

            Assert.That(bootstrap.Operations, Is.EqualTo(new[] { "activate:player-a", "logout", "activate:player-b" }));
            await coordinator.SessionEndedAsync();
        }

        [Test]
        public async Task SessionCoordinatorLogoutCancelsDelayedActivationBeforeItCanPublish()
        {
            var bootstrap = new RecordingChatBootstrap { BlockActivation = true };
            using var coordinator = new LivingHiveChatSessionCoordinator(bootstrap);
            Task activation = coordinator.SessionAvailableAsync(new FixedAccountReadiness(true), ChatBinding("p1", new MutableSession(new ChatSession("p1", "valid-token"))));
            await bootstrap.ActivationStarted.Task;

            Task logout = coordinator.SessionEndedAsync();
            Assert.CatchAsync<OperationCanceledException>(async () => await activation);
            await logout;

            Assert.That(bootstrap.ActivateCount, Is.EqualTo(1));
            Assert.That(bootstrap.PublishedActivationCount, Is.Zero);
            Assert.That(bootstrap.LogoutCount, Is.EqualTo(1));
        }

        [Test]
        public void LivingHiveBootstrapCancellationAfterSessionLookupNeverConfiguresRuntime()
        {
            var bootstrap = new LivingHiveChatBootstrap();
            using var cancellation = new CancellationTokenSource();
            var sessions = new CancellingSession(cancellation, new ChatSession("p1", "valid-token"));
            var options = new RemoteChatClientOptions { BaseUrl = "https://chat.example.test/chat/v1", StoragePartitionId = "p1" };

            Assert.CatchAsync<OperationCanceledException>(async () => await bootstrap.ActivateAsync(options, sessions, new MemoryStringStore(), new NoOpDataProtector(), ct: cancellation.Token));

            Assert.That(LivingHiveChatRuntime.IsConfigured, Is.False);
        }

        [Test]
        public async Task SessionCoordinatorCanRetryCleanlyAfterActivationFailure()
        {
            var bootstrap = new RecordingChatBootstrap { FailuresRemaining = 1 };
            using var coordinator = new LivingHiveChatSessionCoordinator(bootstrap);
            var binding = ChatBinding("p1", new MutableSession(new ChatSession("p1", "valid-token")));
            var ready = new FixedAccountReadiness(true);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await coordinator.SessionAvailableAsync(ready, binding));
            await coordinator.SessionAvailableAsync(ready, binding);

            Assert.That(bootstrap.ActivateCount, Is.EqualTo(2));
            Assert.That(bootstrap.PublishedActivationCount, Is.EqualTo(1));
            Assert.That(bootstrap.LogoutCount, Is.EqualTo(1));
            await coordinator.SessionEndedAsync();
        }

        private static LivingHiveChatSessionBinding ChatBinding(string playerId, IChatSessionSource sessions) => new LivingHiveChatSessionBinding(
            new RemoteChatClientOptions { BaseUrl = "https://chat.example.test/chat/v1", StoragePartitionId = playerId }, sessions, new MemoryStringStore(), new NoOpDataProtector());

        private static ServerChatProvider NewProvider(FakeRest rest, FakeRealtime realtime = null) => new ServerChatProvider(rest, new FakeSession(), realtime);
        private static RemoteChatMessage Message(long sequence, string request) => new RemoteChatMessage { ConversationId = "c1", MessageId = "m" + sequence, Sequence = sequence, ClientRequestId = request, SenderId = "p1", OriginalBody = "body" };
        private static RemoteConversation Conversation(string id) => new RemoteConversation { ConversationId = id, Title = id };
        private static RemoteCapabilities ValidCapabilities(bool server = true, bool realtime = true) => new RemoteCapabilities { Provider = "server", Server = server, Realtime = realtime, ProtocolVersion = "chat-v1", IdempotencyReceiptRetentionDays = 30, ReadCursors = true, ModerationReports = true, OfflineDelivery = true, Channels = new List<string> { "Alliance", "Server", "Private", "Leaders" }, Limits = new RemoteChatLimits { BodyMaxCharacters = 500, MaxPrivateRecipients = 20, MessagesPerMinutePerPlayer = 30, MessagesPerTenSecondsPerConversation = 8, PrivateConversationCreatesPerHour = 20 } };

        private sealed class FakeSession : IChatSessionSource { public Task<ChatSession> GetSessionAsync(CancellationToken ct) => Task.FromResult(new ChatSession("p1", "test-token")); }
        private sealed class FixedSession : IChatSessionSource { private readonly ChatSession session; public FixedSession(ChatSession session) { this.session = session; } public Task<ChatSession> GetSessionAsync(CancellationToken ct) => Task.FromResult(session); }
        private sealed class MutableSession : IChatSessionSource { public ChatSession Current; public MutableSession(ChatSession current) { Current = current; } public Task<ChatSession> GetSessionAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(Current); } }
        private sealed class CancellingSession : IChatSessionSource
        {
            private readonly CancellationTokenSource cancellation;
            private readonly ChatSession session;
            public CancellingSession(CancellationTokenSource cancellation, ChatSession session) { this.cancellation = cancellation; this.session = session; }
            public Task<ChatSession> GetSessionAsync(CancellationToken ct) { cancellation.Cancel(); return Task.FromResult(session); }
        }
        private sealed class FixedAccountReadiness : IChatAccountSessionReadiness { public FixedAccountReadiness(bool canSubmitLogin) { CanSubmitLogin = canSubmitLogin; } public bool CanSubmitLogin { get; } }
        private sealed class RecordingChatBootstrap : ILivingHiveChatBootstrap
        {
            public readonly List<string> Operations = new List<string>();
            public readonly TaskCompletionSource<bool> ActivationStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public bool BlockActivation;
            public int FailuresRemaining;
            public int ActivateCount;
            public int PublishedActivationCount;
            public int LogoutCount;
            public IChatSessionSource LastSessions;

            public async Task ActivateAsync(RemoteChatClientOptions options, IChatSessionSource sessions, IChatStringStore storage, IChatDataProtector protector, IChatRealtimeTransport realtime = null, IChatDiagnosticsSink diagnostics = null, CancellationToken ct = default)
            {
                ActivateCount++;
                LastSessions = sessions;
                Operations.Add("activate:" + options.StoragePartitionId);
                ActivationStarted.TrySetResult(true);
                if (BlockActivation) await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                if (FailuresRemaining > 0) { FailuresRemaining--; throw new InvalidOperationException("activation failed"); }
                ct.ThrowIfCancellationRequested();
                PublishedActivationCount++;
            }

            public Task LogoutAsync(CancellationToken ct = default)
            {
                LogoutCount++;
                Operations.Add("logout");
                return Task.CompletedTask;
            }
        }
        private sealed class SignallingSession : IChatSessionSource
        {
            private readonly int threshold;
            private int count;
            public readonly TaskCompletionSource<bool> ThresholdReached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public SignallingSession(int threshold) { this.threshold = threshold; }
            public Task<ChatSession> GetSessionAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); if (Interlocked.Increment(ref count) >= threshold) ThresholdReached.TrySetResult(true); return Task.FromResult(new ChatSession("p1", "valid-token")); }
        }
        private sealed class CountingSession : IChatSessionSource { public int GetCount; public Task<ChatSession> GetSessionAsync(CancellationToken ct) { GetCount++; return Task.FromResult(new ChatSession("p1", "should-not-be-used")); } }
        private sealed class RefreshableSession : IRefreshableChatSessionSource
        {
            public int RefreshCount;
            public Task<ChatSession> GetSessionAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(new ChatSession("p1", "expired-token")); }
            public Task<ChatSession> RefreshSessionAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); RefreshCount++; return Task.FromResult(new ChatSession("p1", "refreshed-token")); }
        }
        private sealed class AccountChangingRefreshSession : IRefreshableChatSessionSource
        {
            public Task<ChatSession> GetSessionAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(new ChatSession("p1", "expired-token")); }
            public Task<ChatSession> RefreshSessionAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(new ChatSession("p2", "refreshed-token")); }
        }
        private sealed class CancellingRefreshSession : IRefreshableChatSessionSource
        {
            public CancellationTokenSource Source;
            public int RefreshCount;
            public Task<ChatSession> GetSessionAsync(CancellationToken ct) => Task.FromResult(new ChatSession("p1", "expired-token"));
            public Task<ChatSession> RefreshSessionAsync(CancellationToken ct) { RefreshCount++; Source.Cancel(); ct.ThrowIfCancellationRequested(); return Task.FromResult<ChatSession>(null); }
        }
        private sealed class FakePendingStore : IChatPendingSendStore
        {
            public readonly List<PendingChatSend> Items = new List<PendingChatSend>();
            public Task<IReadOnlyList<PendingChatSend>> LoadAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult((IReadOnlyList<PendingChatSend>)new List<PendingChatSend>(Items)); }
            public Task SaveAsync(PendingChatSend pending, CancellationToken ct) { ct.ThrowIfCancellationRequested(); Items.RemoveAll(item => item.ClientRequestId == pending.ClientRequestId); Items.Add(pending); return Task.CompletedTask; }
            public Task RemoveAsync(string id, CancellationToken ct) { ct.ThrowIfCancellationRequested(); Items.RemoveAll(item => item.ClientRequestId == id); return Task.CompletedTask; }
        }
        private sealed class BlockingPendingSendStore : IChatPendingSendStore
        {
            public readonly List<PendingChatSend> Items = new List<PendingChatSend>();
            public readonly TaskCompletionSource<bool> SaveStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> ReleaseSave = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public Task<IReadOnlyList<PendingChatSend>> LoadAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult((IReadOnlyList<PendingChatSend>)new List<PendingChatSend>(Items)); }
            public async Task SaveAsync(PendingChatSend pending, CancellationToken ct) { SaveStarted.TrySetResult(true); await ReleaseSave.Task; ct.ThrowIfCancellationRequested(); Items.RemoveAll(item => item.ClientRequestId == pending.ClientRequestId); Items.Add(pending); }
            public Task RemoveAsync(string id, CancellationToken ct) { ct.ThrowIfCancellationRequested(); Items.RemoveAll(item => item.ClientRequestId == id); return Task.CompletedTask; }
        }
        private sealed class FakePendingConversationStore : IChatPendingConversationStore
        {
            private readonly List<PendingChatConversationCreation> items = new List<PendingChatConversationCreation>();
            public int Count => items.Count;
            public Task<IReadOnlyList<PendingChatConversationCreation>> LoadAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult((IReadOnlyList<PendingChatConversationCreation>)new List<PendingChatConversationCreation>(items)); }
            public Task SaveAsync(PendingChatConversationCreation pending, CancellationToken ct) { ct.ThrowIfCancellationRequested(); items.RemoveAll(item => item.Request.ClientRequestId == pending.Request.ClientRequestId); items.Add(pending); return Task.CompletedTask; }
            public Task RemoveAsync(string id, CancellationToken ct) { ct.ThrowIfCancellationRequested(); items.RemoveAll(item => item.Request.ClientRequestId == id); return Task.CompletedTask; }
        }
        private sealed class FakePendingReportStore : IChatPendingModerationReportStore
        {
            private readonly List<PendingModerationReportRequest> items = new List<PendingModerationReportRequest>();
            public int Count => items.Count;
            public Task<IReadOnlyList<PendingModerationReportRequest>> LoadAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult((IReadOnlyList<PendingModerationReportRequest>)new List<PendingModerationReportRequest>(items)); }
            public Task SaveAsync(PendingModerationReportRequest pending, CancellationToken ct) { ct.ThrowIfCancellationRequested(); items.RemoveAll(item => item.ClientRequestId == pending.ClientRequestId); items.Add(pending); return Task.CompletedTask; }
            public Task RemoveAsync(string id, CancellationToken ct) { ct.ThrowIfCancellationRequested(); items.RemoveAll(item => item.ClientRequestId == id); return Task.CompletedTask; }
        }
        private sealed class FakePendingReadStore : IChatPendingReadStore
        {
            private readonly Dictionary<string, PendingReadCursor> items = new Dictionary<string, PendingReadCursor>();
            public int Count => items.Count;
            public Task<IReadOnlyList<PendingReadCursor>> LoadAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult((IReadOnlyList<PendingReadCursor>)items.Values.ToList()); }
            public Task SaveMaximumAsync(PendingReadCursor pending, CancellationToken ct) { ct.ThrowIfCancellationRequested(); if (!items.TryGetValue(pending.ConversationId, out PendingReadCursor current) || pending.Sequence > current.Sequence) items[pending.ConversationId] = pending; else if (pending.Sequence == current.Sequence) current.AttemptCount = Math.Max(current.AttemptCount, pending.AttemptCount); return Task.CompletedTask; }
            public Task RemoveThroughAsync(string id, long sequence, CancellationToken ct) { ct.ThrowIfCancellationRequested(); if (items.TryGetValue(id, out PendingReadCursor current) && current.Sequence <= sequence) items.Remove(id); return Task.CompletedTask; }
        }
        private sealed class CollectingDiagnostics : IChatDiagnosticsSink { public readonly List<ChatDiagnosticEvent> Events = new List<ChatDiagnosticEvent>(); public void Record(ChatDiagnosticEvent diagnosticEvent) => Events.Add(diagnosticEvent); }
        private sealed class ThrowingDiagnostics : IChatDiagnosticsSink { public void Record(ChatDiagnosticEvent diagnosticEvent) => throw new InvalidOperationException("diagnostics unavailable"); }
        private sealed class BlockingReadTransport : IChatRestTransport
        {
            public readonly TaskCompletionSource<bool> Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> Release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public async Task<ChatTransportResponse<T>> SendAsync<T>(ChatTransportRequest request, CancellationToken ct) { Started.TrySetResult(true); await Release.Task; ct.ThrowIfCancellationRequested(); var read = (RemoteMarkReadRequest)request.Body; object receipt = new RemoteInboxEntry { ConversationId = "c1", ReadCursorSequence = read.Sequence }; return new ChatTransportResponse<T> { StatusCode = 200, Body = (T)receipt }; }
        }
        private sealed class BlockingMessageTransport : IChatRestTransport
        {
            public readonly TaskCompletionSource<bool> Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> Release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public async Task<ChatTransportResponse<T>> SendAsync<T>(ChatTransportRequest request, CancellationToken ct) { Started.TrySetResult(true); await Release.Task; ct.ThrowIfCancellationRequested(); object page = new RemoteMessagePage(); return new ChatTransportResponse<T> { StatusCode = 200, Body = (T)page }; }
        }
        private sealed class MemoryStringStore : IChatStringStore
        {
            public string Value;
            public string Read(string key) => Value;
            public void Write(string key, string value) => Value = value;
            public void Delete(string key) => Value = null;
        }

        private sealed class MultiKeyStringStore : IChatStringStore
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public IEnumerable<string> Keys => values.Keys;
            public IEnumerable<string> Values => values.Values;
            public string Read(string key) => values.TryGetValue(key, out string value) ? value : null;
            public void Write(string key, string value) => values[key] = value;
            public void Delete(string key) => values.Remove(key);
        }

        private sealed class ThrowingStringStore : IChatStringStore
        {
            public string Read(string key) => null;
            public void Write(string key, string value) => throw new InvalidOperationException("storage failed");
            public void Delete(string key) => throw new InvalidOperationException("storage failed");
        }

        private sealed class FailingBackupStringStore : IChatStringStore
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public string Read(string key) => values.TryGetValue(key, out string value) ? value : null;
            public void Write(string key, string value) { if (key.Contains(".Recovery.v1.", StringComparison.Ordinal)) throw new InvalidOperationException("backup failed"); values[key] = value; }
            public void Delete(string key) => values.Remove(key);
        }

        private sealed class FailingRecentQuarantineStore : IChatStringStore
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public bool FailCurrentQuarantine;
            public string Read(string key) => values.TryGetValue(key, out string value) ? value : null;
            public void Write(string key, string value)
            {
                if (FailCurrentQuarantine && key.EndsWith(".Quarantine.v1", StringComparison.Ordinal) && !key.EndsWith(".Previous.v1", StringComparison.Ordinal) && !key.EndsWith(".Staging.v1", StringComparison.Ordinal)) return;
                values[key] = value;
            }
            public void Delete(string key) => values.Remove(key);
        }

        private sealed class BlockingBackupStringStore : IChatStringStore
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public readonly ManualResetEventSlim BackupStarted = new ManualResetEventSlim(false);
            public readonly ManualResetEventSlim ReleaseBackup = new ManualResetEventSlim(false);
            public IEnumerable<string> Values => values.Values;
            public string Read(string key) { lock (values) return values.TryGetValue(key, out string value) ? value : null; }
            public void Write(string key, string value)
            {
                if (key.Contains(".Recovery.v1.", StringComparison.Ordinal)) { BackupStarted.Set(); ReleaseBackup.Wait(); }
                lock (values) values[key] = value;
            }
            public void Delete(string key) { lock (values) values.Remove(key); }
        }

        private sealed class TestDataProtector : IChatDataProtector
        {
            private readonly string secret;
            public TestDataProtector(string secret) { this.secret = secret; }
            public string Protect(string purpose, string plaintext)
            {
                string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(purpose + "\n" + plaintext));
                return payload + "." + Signature(payload);
            }
            public string Unprotect(string purpose, string protectedValue)
            {
                string[] parts = protectedValue.Split('.');
                if (parts.Length != 2 || !string.Equals(parts[1], Signature(parts[0]), StringComparison.Ordinal)) throw new InvalidOperationException("Invalid envelope.");
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                string prefix = purpose + "\n";
                if (!decoded.StartsWith(prefix, StringComparison.Ordinal)) throw new InvalidOperationException("Invalid purpose.");
                return decoded.Substring(prefix.Length);
            }
            private string Signature(string payload)
            {
                using (SHA256 hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(secret + "\n" + payload)));
            }
        }

        private sealed class NoOpDataProtector : IChatDataProtector
        {
            public string Protect(string purpose, string plaintext) => plaintext;
            public string Unprotect(string purpose, string protectedValue) => protectedValue;
        }
        private sealed class SystemTextJsonBackend : IChatJsonBackend
        {
            private readonly JsonSerializerOptions options = new JsonSerializerOptions { IncludeFields = true };
            public string ToJson(object value) => JsonSerializer.Serialize(value, value.GetType(), options);
            public T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, options);
        }
        private sealed class CancellationAwareSession : IChatSessionSource { public Task<ChatSession> GetSessionAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(new ChatSession("p1", "x")); } }
        private sealed class FakeDelay : IChatDelay { public int Count; public readonly List<TimeSpan> Durations = new List<TimeSpan>(); public Task WaitAsync(TimeSpan duration, CancellationToken ct) { ct.ThrowIfCancellationRequested(); Count++; Durations.Add(duration); return Task.CompletedTask; } }
        private sealed class BlockingCountingDelay : IChatDelay { public int Count; public async Task WaitAsync(TimeSpan duration, CancellationToken ct) { Interlocked.Increment(ref Count); await Task.Delay(Timeout.InfiniteTimeSpan, ct); } }
        private sealed class FakeClock : IChatClock { public DateTime UtcNow { get; set; } public FakeClock(DateTimeOffset utcNow) { UtcNow = utcNow.UtcDateTime; } }
        private sealed class CancellingDelay : IChatDelay { public CancellationTokenSource Source; public Task WaitAsync(TimeSpan duration, CancellationToken ct) { Source.Cancel(); ct.ThrowIfCancellationRequested(); return Task.CompletedTask; } }
        private sealed class FakeRealtime : IChatRealtimeTransport
        {
            private Func<RemoteChatEvent, Task> callback;
            public int DisconnectCount;
            public int ConnectCount;
            public readonly Queue<Exception> ConnectFailures = new Queue<Exception>();
            public readonly List<string> JoinedConversationIds = new List<string>();
            public readonly List<string> LeftConversationIds = new List<string>();
            public bool IsAvailable => true;
            public Task ConnectAsync(ChatSession session, Func<RemoteChatEvent, Task> onEvent, CancellationToken ct) { ConnectCount++; callback = onEvent; return ConnectFailures.Count == 0 ? Task.CompletedTask : Task.FromException(ConnectFailures.Dequeue()); }
            public Task JoinConversationAsync(string conversationId, CancellationToken ct) { JoinedConversationIds.Add(conversationId); return Task.CompletedTask; }
            public Task LeaveConversationAsync(string conversationId, CancellationToken ct) { LeftConversationIds.Add(conversationId); return Task.CompletedTask; }
            public Task DisconnectAsync(CancellationToken ct) { DisconnectCount++; return Task.CompletedTask; }
            public Task Emit(RemoteChatEvent evt) => callback(evt);
        }
        private sealed class FakeRest : IChatRestTransport
        {
            public int StatusCode = 200;
            public int CallCount;
            public int PostCount;
            public int TranslationPostCount;
            public int TranslationStatusCode = 200;
            public readonly Queue<int> ResponseStatuses = new Queue<int>();
            public string LastBearerToken;
            public string RawErrorBody;
            public string TransportError;
            public RemoteSendResult SendResultOverride;
            public RemoteCreateConversationResult CreateResultOverride;
            public RemoteModerationReport ReportResultOverride;
            public RemoteInboxEntry ReadResultOverride;
            public MessageTranslation TranslationResultOverride;
            public int? RetryAfterSeconds;
            public RemoteReportMessageRequest LastReportRequest;
            public RemoteMarkReadRequest LastReadRequest;
            public RemoteCapabilities Capabilities = ValidCapabilities();
            public int FailuresRemaining;
            public RemoteCreateConversationRequest LastCreateRequest;
            public RemoteSendMessageRequest LastSendRequest;
            public readonly RemoteMessagePage Page = new RemoteMessagePage();
            public readonly Queue<RemoteMessagePage> MessagePages = new Queue<RemoteMessagePage>();
            public readonly Queue<RemoteConversationPage> ConversationPages = new Queue<RemoteConversationPage>();
            public readonly List<ChatTransportRequest> Requests = new List<ChatTransportRequest>();
            public string CapabilityCacheControl = "no-store, no-cache, max-age=0, must-revalidate";
            public int? CapabilityAgeSeconds = 0;
            public Task<ChatTransportResponse<T>> SendAsync<T>(ChatTransportRequest request, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested(); CallCount++; LastBearerToken = request.BearerToken; Requests.Add(request);
                if (FailuresRemaining-- > 0) throw new InvalidOperationException("offline");
                int responseStatus = ResponseStatuses.Count > 0 ? ResponseStatuses.Dequeue() : StatusCode;
                object body = null;
                if (request.Path.Contains("/messages?") && request.Method == "GET") body = MessagePages.Count > 0 ? MessagePages.Dequeue() : Page;
                else if (request.Path.StartsWith("/chat/v1/conversations?", StringComparison.Ordinal) && request.Method == "GET") body = ConversationPages.Count > 0 ? ConversationPages.Dequeue() : new RemoteConversationPage();
                else if (request.Path.EndsWith("/messages") && request.Method == "POST") { PostCount++; LastSendRequest = (RemoteSendMessageRequest)request.Body; string conversationId = Uri.UnescapeDataString(request.Path.Split('/')[4]); body = SendResultOverride ?? new RemoteSendResult { Message = new RemoteChatMessage { ConversationId = conversationId, MessageId = "m1", Sequence = 1, ClientRequestId = LastSendRequest.ClientRequestId, OriginalBody = LastSendRequest.Body, SenderId = "p1" }, ServerSequence = 1 }; Page.Items.Clear(); Page.Items.Add(((RemoteSendResult)body).Message); }
                else if (request.Path.EndsWith("/translations")) { TranslationPostCount++; if (TranslationStatusCode != 200) return Task.FromResult(new ChatTransportResponse<T> { StatusCode = TranslationStatusCode }); var input = (TranslationRequest)request.Body; body = TranslationResultOverride ?? new MessageTranslation { MessageId = input.MessageId, SourceLocale = "es", TargetLocale = input.TargetLocale, ModelVersion = input.ModelVersion, TranslatedText = "translated", Status = "completed" }; }
                else if (request.Path == "/chat/v1/conversations" && request.Method == "POST") { LastCreateRequest = (RemoteCreateConversationRequest)request.Body; body = CreateResultOverride ?? new RemoteCreateConversationResult { Conversation = new RemoteConversation { ConversationId = "c-created" }, Inbox = new RemoteInboxEntry { ConversationId = "c-created" }, ClientRequestId = LastCreateRequest.ClientRequestId }; }
                else if (request.Path.EndsWith("/report") && request.Method == "POST") { LastReportRequest = (RemoteReportMessageRequest)request.Body; string messageId = request.Path.Split('/')[4]; body = ReportResultOverride ?? new RemoteModerationReport { ReportId = "moderation-1", MessageId = Uri.UnescapeDataString(messageId), ClientRequestId = LastReportRequest.ClientRequestId, Status = "open" }; }
                else if (request.Path.EndsWith("/read") && request.Method == "POST") { LastReadRequest = (RemoteMarkReadRequest)request.Body; string conversationId = Uri.UnescapeDataString(request.Path.Split('/')[4]); body = ReadResultOverride ?? new RemoteInboxEntry { ConversationId = conversationId, ReadCursorSequence = LastReadRequest.Sequence }; }
                else if (typeof(T) == typeof(RemoteCapabilities)) body = Capabilities;
                return Task.FromResult(new ChatTransportResponse<T> { StatusCode = responseStatus, Body = (T)body, RawBody = RawErrorBody, TransportError = TransportError, RetryAfterSeconds = RetryAfterSeconds, CacheControl = request.BypassCache ? CapabilityCacheControl : null, AgeSeconds = request.BypassCache ? CapabilityAgeSeconds : null });
            }
        }
    }
}
