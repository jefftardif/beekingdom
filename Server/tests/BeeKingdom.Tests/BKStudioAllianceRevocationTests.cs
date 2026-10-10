using System;
using System.Linq;
using System.Threading.Tasks;
using BeeKingdom.Chat;
using BeeKingdom.Chat.Audience;
using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Chat.Realtime;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace BeeKingdom.Tests;

public sealed class BKStudioAllianceRevocationTests
{
    [Test]
    public async Task Revoked_alliance_membership_denies_stale_read_and_send_permissions()
    {
        var options = Options.Create(new ChatOptions { Enabled = true, RealtimeEnabled = false });
        var repository = new InMemoryChatRepository();
        PlayerId player = PlayerId.New();
        Guid allianceId = Guid.NewGuid();
        var membership = new MutableMembershipResolver(allianceId, player.Value);
        var service = new ChatService(repository,
            new LocalChatAudienceResolver(options, membership),
            new NoopChatRealtimeDispatcher(), new Clock(), options,
            allianceMembershipResolver: membership);
        var conversation = service.CreateConversation(player,
            new CreateChatConversationRequest(
                ChatChannelType.Alliance,
                Guid.NewGuid(), Guid.NewGuid(),
                "alliance:" + allianceId.ToString("N"),
                "Alliance Test", Array.Empty<Guid>(), "revocation-create"));

        Assert.That(repository.GetParticipant(conversation.Conversation.ConversationId, player), Is.Not.Null);
        membership.IsActive = false;

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.GetMessages(player, conversation.Conversation.ConversationId, 0, 10));

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SendMessageAsync(player, conversation.Conversation.ConversationId,
                new SendChatMessageRequest("revocation-send", "should be denied",
                    null, null, null, null, DateTimeOffset.UtcNow)));
    }

    [Test]
    public void Revoked_alliance_member_is_hidden_from_paged_conversation_list()
    {
        var options = Options.Create(new ChatOptions { Enabled = true, RealtimeEnabled = false });
        var repository = new InMemoryChatRepository();
        PlayerId player = PlayerId.New();
        Guid allianceId = Guid.NewGuid();
        var membership = new MutableMembershipResolver(allianceId, player.Value);
        var service = new ChatService(repository,
            new LocalChatAudienceResolver(options, membership),
            new NoopChatRealtimeDispatcher(), new Clock(), options,
            allianceMembershipResolver: membership);

        var alliance = service.CreateConversation(player,
            new CreateChatConversationRequest(ChatChannelType.Alliance,
                Guid.NewGuid(), Guid.NewGuid(), "alliance:" + allianceId.ToString("N"),
                "Alliance Secrete", Array.Empty<Guid>(), "list-revoked-create"));

        Guid[] validIds = new Guid[3];
        for (int i = 0; i < validIds.Length; i++)
        {
            var publicChat = service.CreateConversation(player,
                new CreateChatConversationRequest(ChatChannelType.Server,
                    Guid.NewGuid(), Guid.NewGuid(), null,
                    "Monde " + i, Array.Empty<Guid>(), "list-public-" + i));
            validIds[i] = publicChat.Conversation.ConversationId;
        }

        Assert.That(service.ListConversations(player, 10).Items
            .Any(c => c.ConversationId == alliance.Conversation.ConversationId), Is.True);
        membership.IsActive = false;

        var visible = service.ListConversations(player, 10);
        Assert.That(visible.Items.Any(c => c.ConversationId == alliance.Conversation.ConversationId),
            Is.False, "A revoked Alliance member must not see chat metadata in the inbox list.");
        Assert.That(visible.Items.Select(c => c.ConversationId).OrderBy(x => x),
            Is.EqualTo(validIds.OrderBy(x => x)), "Valid world conversations must remain visible.");

        string? cursor = null;
        var paged = new System.Collections.Generic.List<Guid>();
        for (int page = 0; page < 10; page++)
        {
            ChatConversationPage next = service.ListConversations(player, 1, cursor);
            paged.AddRange(next.Items.Select(c => c.ConversationId));
            cursor = next.NextCursor;
            if (cursor == null) break;
            Assert.That(page, Is.LessThan(9), "Pagination must terminate.");
        }
        Assert.That(paged.OrderBy(x => x), Is.EqualTo(validIds.OrderBy(x => x)));
        Assert.That(paged.Distinct().Count(), Is.EqualTo(3), "No duplicate or skipped page.");
    }

    private sealed class MutableMembershipResolver : IAllianceMembershipResolver
    {
        private readonly Guid allianceId;
        private readonly Guid playerId;
        public bool IsActive { get; set; } = true;
        public MutableMembershipResolver(Guid allianceId, Guid playerId)
        {
            this.allianceId = allianceId;
            this.playerId = playerId;
        }
        public ChatPermissionRole? GetMemberRole(Guid requestedAllianceId, Guid requestedPlayerId) =>
            IsActive && requestedAllianceId == allianceId && requestedPlayerId == playerId
                ? ChatPermissionRole.Member
                : null;
    }

    private sealed class Clock : IServerClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
