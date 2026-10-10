using System;
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
