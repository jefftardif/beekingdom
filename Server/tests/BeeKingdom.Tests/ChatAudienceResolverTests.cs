using BeeKingdom.Chat.Audience;
using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

public sealed class ChatAudienceResolverTests
{
    private static readonly Guid GameServerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorldId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid AllianceId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly PlayerId Requester = new(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

    [Test]
    public void ServerAndPrivateChannelsResolveDeterministicParticipants()
    {
        LocalChatAudienceResolver resolver = CreateResolver();
        Guid recipient = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        ChatAudienceDecision server = resolver.ResolveConversationAccess(Requester, new CreateChatConversationRequest(
            ChatChannelType.Server,
            GameServerId,
            WorldId,
            null,
            "Global",
            Array.Empty<Guid>(),
            "server_001"));
        ChatAudienceDecision privateChat = resolver.ResolveConversationAccess(Requester, new CreateChatConversationRequest(
            ChatChannelType.Private,
            GameServerId,
            WorldId,
            null,
            "Private",
            [recipient],
            "private_001"));

        Assert.Multiple(() =>
        {
            Assert.That(server.Allowed, Is.True);
            Assert.That(server.RequesterRole, Is.EqualTo(ChatPermissionRole.Member));
            Assert.That(server.Participants.Select(player => player.Value), Is.EquivalentTo(new[] { Requester.Value }));
            Assert.That(privateChat.Allowed, Is.True);
            Assert.That(privateChat.Participants.Select(player => player.Value), Is.EquivalentTo(new[] { Requester.Value, recipient }));
        });
    }

    [Test]
    public void AllianceAndLeadersChannelsUseResolverRoleGate()
    {
        LocalChatAudienceResolver resolver = CreateResolver();

        ChatAudienceDecision allianceWithoutRole = resolver.ResolveConversationAccess(Requester, AllianceRequest(null));
        ChatAudienceDecision allianceMember = resolver.ResolveConversationAccess(Requester, AllianceRequest("member"));
        ChatAudienceDecision leadersMember = resolver.ResolveConversationAccess(Requester, LeadersRequest("member"));
        ChatAudienceDecision leadersOfficer = resolver.ResolveConversationAccess(Requester, LeadersRequest("officer"));

        Assert.Multiple(() =>
        {
            Assert.That(allianceWithoutRole.Allowed, Is.False);
            Assert.That(allianceWithoutRole.ReasonCode, Is.EqualTo("alliance_membership_required"));
            Assert.That(allianceMember.Allowed, Is.True);
            Assert.That(allianceMember.RequesterRole, Is.EqualTo(ChatPermissionRole.Member));
            Assert.That(leadersMember.Allowed, Is.False);
            Assert.That(leadersMember.ReasonCode, Is.EqualTo("alliance_leader_role_required"));
            Assert.That(leadersOfficer.Allowed, Is.True);
            Assert.That(leadersOfficer.RequesterRole, Is.EqualTo(ChatPermissionRole.Officer));
        });
    }

    [Test]
    public void AllianceAnnouncementsRequireOfficerOrLeader()
    {
        LocalChatAudienceResolver resolver = CreateResolver();
        Guid member = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        ChatAudienceDecision memberDecision = resolver.ResolveAnnouncementAccess(Requester, AllianceId, AnnouncementRequest("member", member));
        ChatAudienceDecision leaderDecision = resolver.ResolveAnnouncementAccess(Requester, AllianceId, AnnouncementRequest("leader", member));

        Assert.Multiple(() =>
        {
            Assert.That(memberDecision.Allowed, Is.False);
            Assert.That(memberDecision.ReasonCode, Is.EqualTo("alliance_leader_role_required"));
            Assert.That(leaderDecision.Allowed, Is.True);
            Assert.That(leaderDecision.Participants.Select(player => player.Value), Is.EquivalentTo(new[] { Requester.Value, member }));
        });
    }

    private static LocalChatAudienceResolver CreateResolver()
    {
        return new LocalChatAudienceResolver(Options.Create(new ChatOptions { MaxPrivateRecipients = 20 }));
    }

    private static CreateChatConversationRequest AllianceRequest(string? requesterRole)
    {
        return new CreateChatConversationRequest(
            ChatChannelType.Alliance,
            GameServerId,
            WorldId,
            $"alliance:{AllianceId:N}",
            "Alliance",
            Array.Empty<Guid>(),
            "alliance_001",
            requesterRole);
    }

    private static CreateChatConversationRequest LeadersRequest(string requesterRole)
    {
        return new CreateChatConversationRequest(
            ChatChannelType.Leaders,
            GameServerId,
            WorldId,
            $"leaders:{AllianceId:N}",
            "Leaders",
            Array.Empty<Guid>(),
            "leaders_001",
            requesterRole);
    }

    private static CreateAllianceAnnouncementRequest AnnouncementRequest(string requesterRole, Guid member)
    {
        return new CreateAllianceAnnouncementRequest(
            GameServerId,
            WorldId,
            "Message alliance",
            [member],
            "announcement_001",
            requesterRole);
    }
}
