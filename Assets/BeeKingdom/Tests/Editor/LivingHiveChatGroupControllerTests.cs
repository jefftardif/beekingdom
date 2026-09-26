using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Gameplay.Communication;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    // RAP-OPTIONNEL-COMMUNICATIONS_01 - group state as the Chat Royal screen consumes it.
    public sealed class LivingHiveChatGroupControllerTests
    {
        // The screen must never invent a world scope: creating a group in the wrong world would be
        // invisible to the player and unfixable from the client. Refusing loudly is the contract.
        [Test]
        public async Task CreatingAGroupWithoutAWorldScopeIsRefusedAndNothingIsSent()
        {
            var rest = new RecordingRest();
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new StubSession()));

            string conversationId = await controller.CreateGroupAsync("Escadron", new[] { "p2" }, CancellationToken.None);

            Assert.That(conversationId, Is.Null);
            Assert.That(controller.HasGroupScope, Is.False);
            Assert.That(controller.Snapshot().ErrorCode, Is.EqualTo("chat_group_scope_not_configured"));
            Assert.That(rest.Requests.Any(path => path.Contains("/chat/v1/groups")), Is.False);
        }

        [Test]
        public void ConfiguringTheScopeMakesGroupCreationAvailable()
        {
            var controller = new LivingHiveChatController(new ServerChatProvider(new RecordingRest(), new StubSession()));

            controller.ConfigureGroupScope("gs-1", "world-1");

            Assert.That(controller.HasGroupScope, Is.True);
            Assert.That(controller.Snapshot().GroupScopeConfigured, Is.True);
        }

        [Test]
        public void ABlankScopeIsTreatedAsNoScopeRatherThanAnEmptyOne()
        {
            var controller = new LivingHiveChatController(new ServerChatProvider(new RecordingRest(), new StubSession()));

            controller.ConfigureGroupScope("   ", "world-1");

            Assert.That(controller.HasGroupScope, Is.False);
        }

        [Test]
        public async Task PendingInvitationsAndAnsweredOnesReachTheSnapshotSeparately()
        {
            var rest = new RecordingRest();
            rest.Received.Add(new RemoteChatGroupInvite { InviteId = "i1", ConversationId = "c1", GroupTitle = "Escadron", InviterDisplayName = "Reine", Status = RemoteChatGroupInviteStatus.Pending });
            rest.Sent.Add(new RemoteChatGroupInvite { InviteId = "i2", ConversationId = "c2", GroupTitle = "Patrouille", InviteeDisplayName = "Alex", Status = RemoteChatGroupInviteStatus.Declined });
            var provider = new ServerChatProvider(rest, new StubSession());
            var controller = new LivingHiveChatController(provider);
            await provider.ConnectAsync(CancellationToken.None);

            await controller.RefreshInvitationsAsync(CancellationToken.None);
            LivingHiveChatSnapshot snapshot = controller.Snapshot();

            Assert.That(snapshot.PendingInvitations.Single().InviterDisplayName, Is.EqualTo("Reine"));
            Assert.That(snapshot.SentInvitationResponses.Single().Status, Is.EqualTo(RemoteChatGroupInviteStatus.Declined));
        }

        // A refusal must be announced once. The controller drops it locally as soon as it is
        // acknowledged, otherwise the toast would fire again at every poll tick.
        [Test]
        public async Task AcknowledgingARefusalRemovesItFromTheSnapshot()
        {
            var rest = new RecordingRest();
            rest.Sent.Add(new RemoteChatGroupInvite { InviteId = "i2", ConversationId = "c2", GroupTitle = "Patrouille", InviteeDisplayName = "Alex", Status = RemoteChatGroupInviteStatus.Declined });
            var provider = new ServerChatProvider(rest, new StubSession());
            var controller = new LivingHiveChatController(provider);
            await provider.ConnectAsync(CancellationToken.None);
            await controller.RefreshInvitationsAsync(CancellationToken.None);

            await controller.AcknowledgeSentInvitationResponsesAsync(new[] { "i2" }, CancellationToken.None);

            Assert.That(controller.Snapshot().SentInvitationResponses, Is.Empty);
        }

        [Test]
        public async Task TheServerSideAutoInviteRuleIsReflectedInTheSnapshot()
        {
            var rest = new RecordingRest { Preference = RemoteChatAutoInviteResponse.Accept };
            var provider = new ServerChatProvider(rest, new StubSession());
            var controller = new LivingHiveChatController(provider);
            await provider.ConnectAsync(CancellationToken.None);

            await controller.RefreshPreferencesAsync(CancellationToken.None);

            Assert.That(controller.Snapshot().AutoInviteResponse, Is.EqualTo(RemoteChatAutoInviteResponse.Accept));
        }

        [Test]
        public void AnUnknownAutoInviteRuleIsRejectedByTheController()
        {
            var controller = new LivingHiveChatController(new ServerChatProvider(new RecordingRest(), new StubSession()));
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await controller.UpdateAutoInviteResponseAsync("Peut-etre", CancellationToken.None));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task LeavingOnlyRemovesTheGroupAfterServerConfirmation(bool confirmed)
        {
            var rest = new RecordingRest { Left = confirmed };
            var provider = new ServerChatProvider(rest, new StubSession());
            var cache = new MemoryRecentCache();
            var controller = new LivingHiveChatController(provider, recentCache: cache);
            await provider.ConnectAsync(CancellationToken.None);
            await controller.SelectKnownConversationAsync("g1", "Escadron", "Group", CancellationToken.None);
            await controller.RefreshGroupDetailAsync("g1", CancellationToken.None);

            await controller.LeaveGroupAsync("g1", CancellationToken.None);

            Assert.That(controller.Snapshot().Conversations.Any(item => item.ConversationId == "g1"), Is.EqualTo(!confirmed));
            Assert.That(controller.Snapshot().SelectedGroup == null, Is.EqualTo(confirmed));
            Assert.That(cache.Load().Conversations.Any(item => item.ConversationId == "g1"), Is.EqualTo(!confirmed));
        }

        [Test]
        public async Task PollingAnExclusionClearsSelectionDetailsAndPersistedCache()
        {
            var rest = new RecordingRest();
            var delay = new PollStepDelay();
            var cache = new MemoryRecentCache();
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new StubSession()), delay: delay, recentCache: cache);
            await controller.OpenAsync(CancellationToken.None);
            try
            {
                await controller.SelectKnownConversationAsync("g1", "Escadron", "Group", CancellationToken.None);
                await controller.RefreshGroupDetailAsync("g1", CancellationToken.None);
                delay.Tick.SetResult(true);
                Assert.That(await Task.WhenAny(delay.NextPoll.Task, Task.Delay(5000)), Is.SameAs(delay.NextPoll.Task));

                Assert.That(controller.Snapshot().SelectedConversationId, Is.Null);
                Assert.That(controller.Snapshot().SelectedGroup, Is.Null);
                Assert.That(controller.Snapshot().Messages, Is.Empty);
                Assert.That(cache.Load().Conversations, Is.Empty);
                Assert.That(cache.Load().SelectedConversationId, Is.Null);
            }
            finally { await controller.CloseAsync(CancellationToken.None); }
        }

        [TestCase(RemoteChatError.Forbidden)]
        [TestCase(RemoteChatError.Transport)]
        public async Task GroupDetailRefreshClearsRevokedMembershipButKeepsItDuringNetworkFailure(RemoteChatError error)
        {
            var rest = new RecordingRest();
            var provider = new ServerChatProvider(rest, new StubSession());
            var controller = new LivingHiveChatController(provider);
            await provider.ConnectAsync(CancellationToken.None);
            await controller.SelectKnownConversationAsync("g1", "Escadron", "Group", CancellationToken.None);
            await controller.RefreshGroupDetailAsync("g1", CancellationToken.None);
            rest.GroupError = error;

            await controller.RefreshGroupDetailAsync("g1", CancellationToken.None);

            bool revoked = error == RemoteChatError.Forbidden;
            Assert.That(controller.Snapshot().SelectedGroup == null, Is.EqualTo(revoked));
            Assert.That(controller.Snapshot().Conversations.Any(item => item.ConversationId == "g1"), Is.EqualTo(!revoked));
            if (revoked) Assert.That(controller.Snapshot().SelectedConversationId, Is.Null);
        }

        [Test]
        public async Task CreationSelectsTheReturnedGroupAndMutationsUpdateTheDisplayedLeaderAndMembers()
        {
            var rest = new RecordingRest();
            var provider = new ServerChatProvider(rest, new StubSession());
            var controller = new LivingHiveChatController(provider);
            await provider.ConnectAsync(CancellationToken.None);
            controller.ConfigureGroupScope("gs1", "world1");
            await controller.SelectKnownConversationAsync("old", "Ancien", "Group", CancellationToken.None);

            string created = await controller.CreateGroupAsync("Escadron", new[] { "p2" }, CancellationToken.None);
            Assert.That(created, Is.EqualTo("g1"));
            Assert.That(controller.Snapshot().SelectedConversationId, Is.EqualTo("g1"));
            rest.Detail.OwnerPlayerId = "p2";
            rest.Detail.ViewerIsLeader = false;
            rest.Detail.Members.Add(new RemoteChatGroupMember { PlayerId = "p2", DisplayName = "Reine", IsLeader = true });
            await controller.TransferLeadershipAsync("g1", "p2", CancellationToken.None);
            Assert.That(controller.Snapshot().SelectedGroup.OwnerPlayerId, Is.EqualTo("p2"));
            Assert.That(controller.Snapshot().SelectedGroup.ViewerIsLeader, Is.False);
            Assert.That(controller.Snapshot().SelectedGroup.Members.Single().DisplayName, Is.EqualTo("Reine"));

            rest.Detail.Members.Clear();
            await controller.RemoveGroupMemberAsync("g1", "p2", CancellationToken.None);
            Assert.That(controller.Snapshot().SelectedGroup.Members, Is.Empty);
        }

        [Test]
        public async Task ReopeningAfterExclusionDiscardsCachedGroupDetails()
        {
            var rest = new RecordingRest();
            var controller = new LivingHiveChatController(new ServerChatProvider(rest, new StubSession()));
            await controller.OpenAsync(CancellationToken.None);
            try
            {
                await controller.SelectKnownConversationAsync("g1", "Escadron", "Group", CancellationToken.None);
                await controller.RefreshGroupDetailAsync("g1", CancellationToken.None);
                await controller.OpenAsync(CancellationToken.None);
                Assert.That(controller.Snapshot().SelectedGroup, Is.Null);
                Assert.That(controller.Snapshot().Conversations, Is.Empty);
                // A later invitation/selection must not resurrect the old leader permissions.
                await controller.SelectKnownConversationAsync("g1", "Escadron", "Group", CancellationToken.None);
                Assert.That(controller.Snapshot().SelectedGroup, Is.Null);
            }
            finally { await controller.CloseAsync(CancellationToken.None); }
        }

        // ==================== harness ====================

        private sealed class MemoryRecentCache : IChatRecentCache
        {
            private ChatRecentCacheSnapshot snapshot = new ChatRecentCacheSnapshot();
            public ChatRecentCacheSnapshot Load() => snapshot;
            public void Save(ChatRecentCacheSnapshot value) => snapshot = value;
            public void Clear() => snapshot = new ChatRecentCacheSnapshot();
        }

        private sealed class PollStepDelay : IChatDelay
        {
            public readonly TaskCompletionSource<bool> Tick = new TaskCompletionSource<bool>();
            public readonly TaskCompletionSource<bool> NextPoll = new TaskCompletionSource<bool>();
            private int calls;
            public Task WaitAsync(TimeSpan duration, CancellationToken ct)
            {
                if (++calls == 1) return Tick.Task;
                NextPoll.TrySetResult(true);
                return Task.Delay(Timeout.Infinite, ct);
            }
        }

        private sealed class StubSession : IChatSessionSource
        {
            public Task<ChatSession> GetSessionAsync(CancellationToken ct) => Task.FromResult(new ChatSession("p1", "test-token"));
        }

        private sealed class RecordingRest : IChatRestTransport
        {
            public readonly List<string> Requests = new List<string>();
            public readonly List<RemoteChatGroupInvite> Received = new List<RemoteChatGroupInvite>();
            public readonly List<RemoteChatGroupInvite> Sent = new List<RemoteChatGroupInvite>();
            public string Preference = RemoteChatAutoInviteResponse.Ask;
            public bool Left = true;
            public RemoteChatError GroupError;
            public readonly RemoteChatGroupDetail Detail = new RemoteChatGroupDetail { ConversationId = "g1", Title = "Escadron", OwnerPlayerId = "p1", ViewerIsLeader = true };

            public Task<ChatTransportResponse<T>> SendAsync<T>(ChatTransportRequest request, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                Requests.Add(request.Path);

                object body = null;
                if (typeof(T) == typeof(RemoteCapabilities)) body = Capabilities();
                else if (typeof(T) == typeof(RemoteConversationPage)) body = new RemoteConversationPage();
                else if (typeof(T) == typeof(RemoteMessagePage)) body = new RemoteMessagePage();
                else if (typeof(T) == typeof(RemoteChatGroupLeaveResult)) body = new RemoteChatGroupLeaveResult { Left = Left };
                else if (typeof(T) == typeof(RemoteChatGroupDetail))
                {
                    if (GroupError != RemoteChatError.None) throw new RemoteChatTransportException(GroupError, "group failure");
                    body = Detail;
                }
                else if (typeof(T) == typeof(RemoteChatPreferences)) body = new RemoteChatPreferences { AutoInviteResponse = Preference };
                else if (typeof(T) == typeof(RemoteChatInviteAcknowledgeResult)) body = new RemoteChatInviteAcknowledgeResult { Acknowledged = 1 };
                else if (typeof(T) == typeof(RemoteChatGroupInviteList))
                {
                    var list = new RemoteChatGroupInviteList();
                    list.Items.AddRange(request.Path.Contains("/sent") ? Sent : Received);
                    body = list;
                }

                return Task.FromResult(new ChatTransportResponse<T>
                {
                    StatusCode = 200,
                    Body = (T)body,
                    CacheControl = request.BypassCache ? "no-store, no-cache, max-age=0, must-revalidate" : null,
                    AgeSeconds = request.BypassCache ? 0 : (int?)null
                });
            }

            private static RemoteCapabilities Capabilities() => new RemoteCapabilities
            {
                Provider = "server",
                Server = true,
                Realtime = false,
                ProtocolVersion = "chat-v1",
                IdempotencyReceiptRetentionDays = 30,
                ReadCursors = true,
                ModerationReports = true,
                OfflineDelivery = true,
                Channels = new List<string> { "Alliance", "Server", "Private", "Leaders", "Group" },
                Limits = new RemoteChatLimits
                {
                    BodyMaxCharacters = 500,
                    MaxPrivateRecipients = 20,
                    MessagesPerMinutePerPlayer = 30,
                    MessagesPerTenSecondsPerConversation = 8,
                    PrivateConversationCreatesPerHour = 20
                }
            };
        }
    }
}
