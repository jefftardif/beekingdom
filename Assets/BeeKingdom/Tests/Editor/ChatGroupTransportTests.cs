using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Gameplay.Communication;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    // RAP-OPTIONNEL-COMMUNICATIONS_01 - client transport for player-created groups.
    public sealed class ChatGroupTransportTests
    {
        // The single highest-value regression here: the server now advertises "Group" among its
        // channels. The client validates that list and REJECTS the whole negotiation on an unknown
        // channel, so an un-updated ValidChannels would not have broken "groups" - it would have
        // taken the entire chat offline.
        [Test]
        public async Task NegotiationAcceptsTheGroupChannelAdvertisedByTheServer()
        {
            var rest = new FakeGroupRest();
            rest.Capabilities.Channels.Add("Group");
            var provider = NewProvider(rest);

            RemoteCapabilityDecision decision = await provider.NegotiateCapabilitiesAsync("chat-v1", CancellationToken.None);

            Assert.That(decision.IsAvailable, Is.True, "The Group channel must not be treated as an unknown channel.");
        }

        [Test]
        public async Task NegotiationPublishesTheWorldScopeGroupCreationNeeds()
        {
            var rest = new FakeGroupRest();
            rest.Capabilities.GameServerId = "gs-1";
            rest.Capabilities.DefaultWorldId = "world-1";
            var provider = NewProvider(rest);

            await provider.NegotiateCapabilitiesAsync("chat-v1", CancellationToken.None);

            Assert.That(provider.NegotiatedCapabilities.GameServerId, Is.EqualTo("gs-1"));
            Assert.That(provider.NegotiatedCapabilities.DefaultWorldId, Is.EqualTo("world-1"));
        }

        [Test]
        public async Task CreateGroupPostsNormalizedInviteesToTheGroupsEndpoint()
        {
            var rest = new FakeGroupRest();
            var provider = NewProvider(rest);
            await provider.ConnectAsync(CancellationToken.None);

            await provider.CreateGroupAsync(new RemoteCreateGroupRequest
            {
                GameServerId = " gs-1 ",
                WorldId = "world-1",
                Title = "  Escadron Miel  ",
                ClientRequestId = "req-1",
                // duplicates and blanks must not reach the wire
                InviteePlayerIds = new[] { "p2", "p2", "  ", "p3" }
            }, CancellationToken.None);

            ChatTransportRequest sent = rest.Requests.Last();
            var payload = (RemoteCreateGroupRequest)sent.Body;
            Assert.That(sent.Path, Is.EqualTo("/chat/v1/groups"));
            Assert.That(sent.Method, Is.EqualTo("POST"));
            Assert.That(payload.Title, Is.EqualTo("Escadron Miel"));
            Assert.That(payload.GameServerId, Is.EqualTo("gs-1"));
            Assert.That(payload.InviteePlayerIds, Is.EqualTo(new[] { "p2", "p3" }));
        }

        [Test]
        public async Task MemberAndInvitationCallsAddressTheRightResourcePaths()
        {
            var rest = new FakeGroupRest();
            var provider = NewProvider(rest);
            await provider.ConnectAsync(CancellationToken.None);

            await provider.RemoveGroupMemberAsync("conv-1", "player-9", CancellationToken.None);
            await provider.TransferGroupLeadershipAsync("conv-1", "player-9", CancellationToken.None);
            await provider.RespondToInvitationAsync("invite-7", true, CancellationToken.None);
            await provider.LeaveGroupAsync("conv-1", CancellationToken.None);

            List<string> paths = rest.Requests.Select(item => item.Path).ToList();
            Assert.That(paths, Does.Contain("/chat/v1/groups/conv-1/members/player-9/remove"));
            Assert.That(paths, Does.Contain("/chat/v1/groups/conv-1/leadership/transfer"));
            Assert.That(paths, Does.Contain("/chat/v1/invitations/invite-7/respond"));
            Assert.That(paths, Does.Contain("/chat/v1/groups/conv-1/leave"));
        }

        // Ids are interpolated straight into the URL path, so a hostile or malformed id must be
        // rejected before it is escaped rather than sent and left for the server to refuse.
        [Test]
        public async Task ResourceIdsCarryingPathSeparatorsAreRejected()
        {
            var provider = NewProvider(new FakeGroupRest());
            await provider.ConnectAsync(CancellationToken.None);

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await provider.RemoveGroupMemberAsync("conv-1/../../admin", "player-9", CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await provider.RespondToInvitationAsync(string.Empty, true, CancellationToken.None));
        }

        [Test]
        public async Task InvitationListsSurviveAnEmptyServerAnswer()
        {
            var rest = new FakeGroupRest();
            var provider = NewProvider(rest);
            await provider.ConnectAsync(CancellationToken.None);

            IReadOnlyList<RemoteChatGroupInvite> received = await provider.ListInvitationsAsync(CancellationToken.None);

            Assert.That(received, Is.Not.Null);
            Assert.That(received, Is.Empty);
        }

        [Test]
        public async Task AcknowledgingNothingDoesNotCallTheServer()
        {
            var rest = new FakeGroupRest();
            var provider = NewProvider(rest);
            await provider.ConnectAsync(CancellationToken.None);
            int before = rest.Requests.Count;

            int acknowledged = await provider.AcknowledgeInvitationResponsesAsync(Array.Empty<string>(), CancellationToken.None);

            Assert.That(acknowledged, Is.Zero);
            Assert.That(rest.Requests.Count, Is.EqualTo(before));
        }

        [Test]
        public void UnknownAutoInviteRulesAreRefusedBeforeReachingTheServer()
        {
            var provider = NewProvider(new FakeGroupRest());
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await provider.UpdateChatPreferencesAsync("Maybe", CancellationToken.None));
        }

        // ==================== codec ====================

        [Test]
        public void CodecSerializesGroupRequestsWithTheServerFieldNames()
        {
            var codec = new UnityChatJsonCodec();

            string json = codec.Serialize(new RemoteCreateGroupRequest
            {
                GameServerId = "gs-1",
                WorldId = "world-1",
                Title = "Escadron",
                ClientRequestId = "req-1",
                InviteePlayerIds = new[] { "p2" }
            });

            Assert.That(json, Does.Contain("\"gameServerId\""));
            Assert.That(json, Does.Contain("\"inviteePlayerIds\""));
            Assert.That(json, Does.Contain("\"clientRequestId\""));
        }

        [Test]
        public void CodecReadsAGroupDetailIncludingLeadershipAndPendingInvites()
        {
            var codec = new UnityChatJsonCodec();
            const string json = "{\"conversationId\":\"c1\",\"title\":\"Escadron\",\"ownerPlayerId\":\"p1\",\"viewerIsLeader\":true," +
                "\"members\":[{\"playerId\":\"p1\",\"displayName\":\"Reine\",\"role\":\"Leader\",\"isLeader\":true}]," +
                "\"pendingInvites\":[{\"inviteId\":\"i1\",\"conversationId\":\"c1\",\"groupTitle\":\"Escadron\",\"inviteeDisplayName\":\"Alex\",\"status\":\"Pending\"}]}";

            RemoteChatGroupDetail detail = codec.Deserialize<RemoteChatGroupDetail>(json);

            Assert.That(detail.ViewerIsLeader, Is.True);
            Assert.That(detail.Members.Single().IsLeader, Is.True);
            Assert.That(detail.PendingInvites.Single().InviteeDisplayName, Is.EqualTo("Alex"));
        }

        // The invitations endpoints answer a bare JSON array, which JsonUtility cannot parse at all;
        // the codec wraps it. Without this the whole invitation loop returns nothing, silently.
        [Test]
        public void CodecReadsTheBareInvitationArrayReturnedByTheServer()
        {
            var codec = new UnityChatJsonCodec();
            const string json = "[{\"inviteId\":\"i1\",\"conversationId\":\"c1\",\"groupTitle\":\"Escadron\",\"inviterDisplayName\":\"Reine\",\"status\":\"Pending\"}," +
                "{\"inviteId\":\"i2\",\"conversationId\":\"c2\",\"groupTitle\":\"Patrouille\",\"inviterDisplayName\":\"Alex\",\"status\":\"Declined\"}]";

            RemoteChatGroupInviteList list = codec.Deserialize<RemoteChatGroupInviteList>(json);

            Assert.That(list.Items, Has.Count.EqualTo(2));
            Assert.That(list.Items[1].Status, Is.EqualTo(RemoteChatGroupInviteStatus.Declined));
        }

        [Test]
        public void CodecReadsAnEmptyInvitationArray()
        {
            RemoteChatGroupInviteList list = new UnityChatJsonCodec().Deserialize<RemoteChatGroupInviteList>("[]");
            Assert.That(list.Items, Is.Empty);
        }

        [Test]
        public void CodecReadsTheServerSideAutoInviteRule()
        {
            RemoteChatPreferences preferences = new UnityChatJsonCodec()
                .Deserialize<RemoteChatPreferences>("{\"autoInviteResponse\":\"Decline\"}");
            Assert.That(preferences.AutoInviteResponse, Is.EqualTo(RemoteChatAutoInviteResponse.Decline));
        }

        // ==================== harness ====================

        private static ServerChatProvider NewProvider(FakeGroupRest rest) => new ServerChatProvider(rest, new FakeGroupSession());

        private sealed class FakeGroupSession : IChatSessionSource
        {
            public Task<ChatSession> GetSessionAsync(CancellationToken ct) => Task.FromResult(new ChatSession("p1", "test-token"));
        }

        private sealed class FakeGroupRest : IChatRestTransport
        {
            public readonly List<ChatTransportRequest> Requests = new List<ChatTransportRequest>();

            public RemoteCapabilities Capabilities = new RemoteCapabilities
            {
                Provider = "server",
                Server = true,
                Realtime = false,
                ProtocolVersion = "chat-v1",
                IdempotencyReceiptRetentionDays = 30,
                ReadCursors = true,
                ModerationReports = true,
                OfflineDelivery = true,
                Channels = new List<string> { "Alliance", "Server", "Private", "Leaders" },
                Limits = new RemoteChatLimits
                {
                    BodyMaxCharacters = 500,
                    MaxPrivateRecipients = 20,
                    MessagesPerMinutePerPlayer = 30,
                    MessagesPerTenSecondsPerConversation = 8,
                    PrivateConversationCreatesPerHour = 20
                }
            };

            public Task<ChatTransportResponse<T>> SendAsync<T>(ChatTransportRequest request, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                Requests.Add(request);

                object body = null;
                if (typeof(T) == typeof(RemoteCapabilities)) body = Capabilities;
                else if (typeof(T) == typeof(RemoteConversationPage)) body = new RemoteConversationPage();
                else if (typeof(T) == typeof(RemoteChatGroupInviteList)) body = new RemoteChatGroupInviteList();
                else if (typeof(T) == typeof(RemoteChatGroupDetail)) body = new RemoteChatGroupDetail { ConversationId = "conv-1", Title = "Escadron" };
                else if (typeof(T) == typeof(RemoteChatGroupInvite)) body = new RemoteChatGroupInvite { InviteId = "invite-7", ConversationId = "conv-1", Status = RemoteChatGroupInviteStatus.Accepted };
                else if (typeof(T) == typeof(RemoteChatGroupLeaveResult)) body = new RemoteChatGroupLeaveResult { Left = true };
                else if (typeof(T) == typeof(RemoteChatInviteAcknowledgeResult)) body = new RemoteChatInviteAcknowledgeResult { Acknowledged = 1 };
                else if (typeof(T) == typeof(RemoteChatPreferences)) body = new RemoteChatPreferences { AutoInviteResponse = RemoteChatAutoInviteResponse.Ask };

                // The provider rejects a capabilities answer that could have come from a cache, so the
                // fake must echo the same no-store/Age headers the real endpoint sends.
                return Task.FromResult(new ChatTransportResponse<T>
                {
                    StatusCode = 200,
                    Body = (T)body,
                    CacheControl = request.BypassCache ? "no-store, no-cache, max-age=0, must-revalidate" : null,
                    AgeSeconds = request.BypassCache ? 0 : (int?)null
                });
            }
        }
    }
}
