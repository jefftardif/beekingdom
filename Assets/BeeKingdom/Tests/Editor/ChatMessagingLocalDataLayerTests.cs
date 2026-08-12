using System;
using System.Linq;
using BeeKingdom.Gameplay.Communication;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ChatMessagingLocalDataLayerTests
    {
        [Test]
        public void FourChannelsHaveStableIdsAndPersistAcceptedMessages()
        {
            LocalChatProvider provider = LocalChatProvider.CreateEmpty();
            Conversation alliance = provider.CreateConversation(new CreateConversationInput(ChannelType.Alliance, "alliance_demo", "Alliance", null));
            Conversation server = provider.CreateConversation(new CreateConversationInput(ChannelType.Server, "server_demo", "Server", null));
            Conversation leadership = provider.CreateConversation(new CreateConversationInput(ChannelType.Leadership, "alliance_demo", "Leaders", null));
            Conversation privateChat = provider.CreateConversation(new CreateConversationInput(ChannelType.Private, null, "Private", new[] { "player_scout" }));

            Assert.That(new[] { alliance.Id, server.Id, leadership.Id, privateChat.Id }.Distinct().Count(), Is.EqualTo(4));
            Assert.That(provider.GetCapabilities().Server, Is.False);
            Assert.That(provider.GetCapabilities().OfficialGain, Is.False);
            Assert.That(alliance.Id, Is.EqualTo(ConversationId.ForAlliance("alliance_demo")));
            Assert.That(server.Id, Is.EqualTo(ConversationId.ForServer("server_demo")));
            Assert.That(leadership.Id, Is.EqualTo(ConversationId.ForLeadership("alliance_demo")));
            Assert.That(privateChat.Id, Is.EqualTo(ConversationId.ForPrivatePair("player_queen", "player_scout")));

            SendResult allianceResult = provider.SendMessage(new SendMessageInput(alliance.Id, "player_queen", "Alliance hello", new ClientRequestId("test-alliance-1")));
            SendResult serverResult = provider.SendMessage(new SendMessageInput(server.Id, "player_queen", "Server hello", new ClientRequestId("test-server-1")));
            SendResult leadershipResult = provider.SendMessage(new SendMessageInput(leadership.Id, "player_queen", "Leadership hello", new ClientRequestId("test-leadership-1")));
            SendResult privateResult = provider.SendMessage(new SendMessageInput(privateChat.Id, "player_queen", "Private hello", new ClientRequestId("test-private-1"), new[] { "player_scout" }));

            Assert.That(allianceResult.Accepted && serverResult.Accepted && leadershipResult.Accepted && privateResult.Accepted, Is.True);
            Assert.That(provider.GetMessages(alliance.Id).Items.Single().Body, Is.EqualTo("Alliance hello"));
            Assert.That(provider.GetMessages(privateChat.Id).Items.Single().ChannelType, Is.EqualTo(ChannelType.Private));
            Assert.That(allianceResult.Message.MessageId, Is.EqualTo(MessageId.ForClientRequest(LocalChatProvider.DefaultFixtureSeed, new ClientRequestId("test-alliance-1"))));
        }

        [Test]
        public void LeadershipAnnouncementRequiresLeaderOrOfficer()
        {
            LocalChatProvider leaderProvider = LocalChatProvider.CreateEmpty("player_queen");
            Conversation leadership = leaderProvider.CreateConversation(new CreateConversationInput(ChannelType.Leadership, "alliance_demo", "Leadership", null));
            SendResult announcement = leaderProvider.SendMessage(new SendMessageInput(leadership.Id, "player_queen", "Annonce dirigeants", new ClientRequestId("leaders-1")));

            Assert.That(announcement.Accepted, Is.True);
            Assert.That(announcement.Message.ChannelType, Is.EqualTo(ChannelType.Leadership));

            LocalChatProvider memberProvider = LocalChatProvider.CreateEmpty("player_member");
            Assert.Throws<InvalidOperationException>(() => memberProvider.CreateConversation(new CreateConversationInput(ChannelType.Leadership, "alliance_demo", "Leadership", null)));
        }

        [Test]
        public void PrivateMessageQueuesOfflineAndReconnectDoesNotDuplicateIt()
        {
            ManualChatClock clock = new ManualChatClock(new DateTime(2026, 7, 15, 14, 0, 0, DateTimeKind.Utc));
            LocalChatProvider provider = LocalChatProvider.CreateEmpty("player_queen", clock, false);
            Conversation privateChat = provider.CreateConversation(new CreateConversationInput(ChannelType.Private, null, "Private", new[] { "player_scout" }));
            ClientRequestId requestId = new ClientRequestId("offline-private-1");

            SendResult queued = provider.SendMessage(new SendMessageInput(privateChat.Id, "player_queen", "Message en attente", requestId, new[] { "player_scout" }));
            Assert.That(queued.Queued, Is.True);
            Assert.That(provider.GetPendingOutbox().Count, Is.EqualTo(1));
            Assert.That(provider.GetMessages(privateChat.Id).Items, Is.Empty);

            provider.Reconnect();
            Assert.That(provider.GetPendingOutbox(), Is.Empty);
            Assert.That(provider.GetMessages(privateChat.Id).Items.Count, Is.EqualTo(1));
            SendResult retry = provider.SendMessage(new SendMessageInput(privateChat.Id, "player_queen", "Message en attente", requestId, new[] { "player_scout" }));
            Assert.That(retry.Deduplicated, Is.True);
            Assert.That(provider.GetMessages(privateChat.Id).Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void UnreadCountsUseCursorAndExcludeSenderMessages()
        {
            LocalChatProvider provider = LocalChatProvider.CreateEmpty("player_queen");
            Conversation alliance = provider.CreateConversation(new CreateConversationInput(ChannelType.Alliance, "alliance_demo", "Alliance", null));
            SendResult first = provider.SendMessage(new SendMessageInput(alliance.Id, "player_queen", "One", new ClientRequestId("unread-1")));
            SendResult second = provider.SendMessage(new SendMessageInput(alliance.Id, "player_queen", "Two", new ClientRequestId("unread-2"), mentions: new[] { "player_member" }));

            InboxEntry memberInbox = provider.GetInboxEntry("player_member", alliance.Id);
            InboxEntry senderInbox = provider.GetInboxEntry("player_queen", alliance.Id);
            Assert.That(memberInbox.UnreadCount, Is.EqualTo(2));
            Assert.That(memberInbox.MentionCount, Is.EqualTo(1));
            Assert.That(senderInbox.UnreadCount, Is.EqualTo(0));

            ReadCursor cursor = provider.MarkConversationReadFor("player_member", alliance.Id, first.Message.Sequence.Value);
            Assert.That(cursor.Sequence, Is.EqualTo(first.Message.Sequence.Value));
            Assert.That(provider.GetInboxEntry("player_member", alliance.Id).UnreadCount, Is.EqualTo(1));
            provider.MarkConversationReadFor("player_member", alliance.Id, 0);
            Assert.That(provider.GetInboxEntry("player_member", alliance.Id).ReadCursor, Is.EqualTo(first.Message.Sequence.Value));
        }
    }
}
