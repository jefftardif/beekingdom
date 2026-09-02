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

    // M042-CL: alliance/leaders channel access is now server-authoritative - the client-declared
    // "requesterRole" string on the wire is no longer trusted at all. These tests now drive
    // access purely through a fake IAllianceMembershipResolver, matching what the real
    // BeeKingdom.Alliance-backed resolver will report from actual server-side membership rows.

    [Test]
    public void NonMemberIsDeniedAllianceChannel()
    {
        LocalChatAudienceResolver resolver = CreateResolver(new FakeAllianceMembershipResolver());
        ChatAudienceDecision decision = resolver.ResolveConversationAccess(Requester, AllianceRequest());

        Assert.That(decision.Allowed, Is.False);
        Assert.That(decision.ReasonCode, Is.EqualTo("alliance_membership_required"));
    }

    [Test]
    public void RealMemberIsAllowedAllianceChannelWithServerRole()
    {
        var fake = new FakeAllianceMembershipResolver();
        fake.SetRole(AllianceId, Requester.Value, ChatPermissionRole.Member);
        LocalChatAudienceResolver resolver = CreateResolver(fake);

        ChatAudienceDecision decision = resolver.ResolveConversationAccess(Requester, AllianceRequest());

        Assert.That(decision.Allowed, Is.True);
        Assert.That(decision.RequesterRole, Is.EqualTo(ChatPermissionRole.Member));
    }

    [Test]
    public void ClientDeclaredRoleIsIgnoredCompletely()
    {
        // No membership registered in the fake resolver at all - even though the request claims
        // "leader" on the wire, access must still be denied. This is the exact regression this
        // change exists to prevent.
        LocalChatAudienceResolver resolver = CreateResolver(new FakeAllianceMembershipResolver());
        ChatAudienceDecision decision = resolver.ResolveConversationAccess(Requester, AllianceRequest("leader"));

        Assert.That(decision.Allowed, Is.False);
        Assert.That(decision.ReasonCode, Is.EqualTo("alliance_membership_required"));
    }

    [Test]
    public void KickedMemberLosesChatAccess()
    {
        var fake = new FakeAllianceMembershipResolver();
        fake.SetRole(AllianceId, Requester.Value, ChatPermissionRole.Member);
        LocalChatAudienceResolver resolver = CreateResolver(fake);
        Assert.That(resolver.ResolveConversationAccess(Requester, AllianceRequest()).Allowed, Is.True, "sanity check before kick");

        fake.RemoveRole(AllianceId, Requester.Value); // simulates AllianceService.Kick clearing the membership
        ChatAudienceDecision afterKick = resolver.ResolveConversationAccess(Requester, AllianceRequest());

        Assert.That(afterKick.Allowed, Is.False);
        Assert.That(afterKick.ReasonCode, Is.EqualTo("alliance_membership_required"));
    }

    [Test]
    public void LeaderRoleComesFromServerMembershipForLeadersChannel()
    {
        var fake = new FakeAllianceMembershipResolver();
        fake.SetRole(AllianceId, Requester.Value, ChatPermissionRole.Member);
        LocalChatAudienceResolver resolver = CreateResolver(fake);

        ChatAudienceDecision memberInLeaders = resolver.ResolveConversationAccess(Requester, LeadersRequest());
        Assert.That(memberInLeaders.Allowed, Is.False, "a plain Member must not reach the Leaders channel");
        Assert.That(memberInLeaders.ReasonCode, Is.EqualTo("alliance_leader_role_required"));

        fake.SetRole(AllianceId, Requester.Value, ChatPermissionRole.Officer);
        ChatAudienceDecision officerInLeaders = resolver.ResolveConversationAccess(Requester, LeadersRequest());
        Assert.That(officerInLeaders.Allowed, Is.True);
        Assert.That(officerInLeaders.RequesterRole, Is.EqualTo(ChatPermissionRole.Officer));
    }

    [Test]
    public void AllianceAnnouncementsRequireOfficerOrLeaderFromServerMembership()
    {
        var fake = new FakeAllianceMembershipResolver();
        Guid member = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        LocalChatAudienceResolver resolver = CreateResolver(fake);

        // No membership at all - claiming "leader" on the wire must not matter.
        ChatAudienceDecision noMembership = resolver.ResolveAnnouncementAccess(Requester, AllianceId, AnnouncementRequest("leader", member));
        Assert.That(noMembership.Allowed, Is.False);
        Assert.That(noMembership.ReasonCode, Is.EqualTo("alliance_leader_role_required"));

        fake.SetRole(AllianceId, Requester.Value, ChatPermissionRole.Member);
        ChatAudienceDecision memberDecision = resolver.ResolveAnnouncementAccess(Requester, AllianceId, AnnouncementRequest(null, member));
        Assert.That(memberDecision.Allowed, Is.False);
        Assert.That(memberDecision.ReasonCode, Is.EqualTo("alliance_leader_role_required"));

        fake.SetRole(AllianceId, Requester.Value, ChatPermissionRole.Leader);
        ChatAudienceDecision leaderDecision = resolver.ResolveAnnouncementAccess(Requester, AllianceId, AnnouncementRequest(null, member));
        Assert.That(leaderDecision.Allowed, Is.True);
        Assert.That(leaderDecision.Participants.Select(player => player.Value), Is.EquivalentTo(new[] { Requester.Value, member }));
    }

    private static LocalChatAudienceResolver CreateResolver(IAllianceMembershipResolver? allianceMembership = null)
    {
        return new LocalChatAudienceResolver(Options.Create(new ChatOptions { MaxPrivateRecipients = 20 }), allianceMembership);
    }

    private static CreateChatConversationRequest AllianceRequest(string? requesterRole = null)
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

    private static CreateChatConversationRequest LeadersRequest(string? requesterRole = null)
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

    private static CreateAllianceAnnouncementRequest AnnouncementRequest(string? requesterRole, Guid member)
    {
        return new CreateAllianceAnnouncementRequest(
            GameServerId,
            WorldId,
            "Message alliance",
            [member],
            "announcement_001",
            requesterRole);
    }

    // Stands in for BeeKingdom.Alliance's real AllianceMembershipResolver (wrapping
    // IAllianceRepository.GetActiveMembership) without this test project depending on the
    // Alliance module - exactly mirrors the interface's real contract.
    private sealed class FakeAllianceMembershipResolver : IAllianceMembershipResolver
    {
        private readonly Dictionary<(Guid, Guid), ChatPermissionRole> roles = new();
        public void SetRole(Guid allianceId, Guid playerId, ChatPermissionRole role) => roles[(allianceId, playerId)] = role;
        public void RemoveRole(Guid allianceId, Guid playerId) => roles.Remove((allianceId, playerId));
        public ChatPermissionRole? GetMemberRole(Guid allianceId, Guid playerId) => roles.TryGetValue((allianceId, playerId), out var role) ? role : null;
    }
}
