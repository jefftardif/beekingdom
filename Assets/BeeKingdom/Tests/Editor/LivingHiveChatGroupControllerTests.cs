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

        // ==================== harness ====================

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

            public Task<ChatTransportResponse<T>> SendAsync<T>(ChatTransportRequest request, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                Requests.Add(request.Path);

                object body = null;
                if (typeof(T) == typeof(RemoteCapabilities)) body = Capabilities();
                else if (typeof(T) == typeof(RemoteConversationPage)) body = new RemoteConversationPage();
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
