using BeeKingdom.Chat;
using BeeKingdom.Chat.Audience;
using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Realtime;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

// RAP-OPTIONNEL-COMMUNICATIONS_01: player-created chat groups, invitations, leadership and the
// server-side auto-response preference. Built the same way AllianceChatIntegrationTests builds its
// stack - the real ChatService over the in-memory repository, no container.
public sealed class ChatGroupServiceTests
{
    private sealed class SystemClock : IServerClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

    private static readonly Guid GameServerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorldId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static (ChatService Service, IChatRepository Repository) BuildStack()
    {
        InMemoryChatRepository repository = new();
        LocalChatAudienceResolver resolver = new(Options.Create(new ChatOptions { MaxPrivateRecipients = 20 }));
        ChatService service = new(repository, resolver, new NoopChatRealtimeDispatcher(), new SystemClock(), Options.Create(new ChatOptions { Enabled = true, MaxPrivateRecipients = 20 }));
        return (service, repository);
    }

    private static CreateChatGroupRequest GroupRequest(string clientRequestId, params Guid[] invitees)
        => new(GameServerId, WorldId, "Escadron Miel", invitees, clientRequestId);

    [Test]
    public void CreateGroup_MakesCreatorLeaderAndInviteesPendingNotMembers()
    {
        (ChatService service, IChatRepository repository) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId invitee = PlayerId.New();

        CreateChatGroupResult result = service.CreateGroup(creator, GroupRequest("g1", invitee.Value));

        Assert.Multiple(() =>
        {
            Assert.That(result.Conversation.ChannelType, Is.EqualTo(ChatChannelType.Group));
            Assert.That(result.Conversation.AudienceKey, Does.StartWith("group:"));
            Assert.That(result.Detail.ViewerIsLeader, Is.True);
            Assert.That(result.Detail.OwnerPlayerId, Is.EqualTo(creator.Value));
            Assert.That(result.Detail.Members, Has.Count.EqualTo(1));
            Assert.That(result.Detail.PendingInvites, Has.Count.EqualTo(1));
            // The invited player is NOT a participant until he accepts - otherwise he would read the
            // room's messages before ever answering.
            Assert.That(repository.GetParticipant(result.Conversation.ConversationId, invitee), Is.Null);
        });
    }

    [Test]
    public void CreateGroup_IsIdempotentOnClientRequestId()
    {
        (ChatService service, _) = BuildStack();
        PlayerId creator = PlayerId.New();

        Guid first = service.CreateGroup(creator, GroupRequest("same")).Conversation.ConversationId;
        Guid second = service.CreateGroup(creator, GroupRequest("same")).Conversation.ConversationId;

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void CreateGroup_TwoGroupsWithIdenticalMembersDoNotCollide()
    {
        (ChatService service, _) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId invitee = PlayerId.New();

        Guid first = service.CreateGroup(creator, GroupRequest("g1", invitee.Value)).Conversation.ConversationId;
        Guid second = service.CreateGroup(creator, GroupRequest("g2", invitee.Value)).Conversation.ConversationId;

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void CreateGroup_RejectsEmptyTitle()
    {
        (ChatService service, _) = BuildStack();
        Assert.Throws<ArgumentException>(() => service.CreateGroup(PlayerId.New(), new CreateChatGroupRequest(GameServerId, WorldId, "   ", [], "g1")));
    }

    [Test]
    public void AcceptInvitation_AddsMemberAndListsGroupForHim()
    {
        (ChatService service, IChatRepository repository) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId invitee = PlayerId.New();
        CreateChatGroupResult group = service.CreateGroup(creator, GroupRequest("g1", invitee.Value));

        IReadOnlyList<ChatGroupInviteDto> pending = service.ListPendingInvitations(invitee);
        Assert.That(pending, Has.Count.EqualTo(1));
        service.RespondToInvitation(invitee, pending[0].InviteId, accept: true);

        ChatConversationParticipant? participant = repository.GetParticipant(group.Conversation.ConversationId, invitee);
        Assert.Multiple(() =>
        {
            Assert.That(participant, Is.Not.Null);
            Assert.That(participant!.Role, Is.EqualTo(ChatPermissionRole.Member));
            Assert.That(service.ListConversations(invitee, 50).Items.Select(item => item.ConversationId), Contains.Item(group.Conversation.ConversationId));
            Assert.That(service.ListPendingInvitations(invitee), Is.Empty);
        });
    }

    [Test]
    public void DeclineInvitation_LeavesHimOutAndSurfacesTheRefusalToTheInviterOnce()
    {
        (ChatService service, IChatRepository repository) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId invitee = PlayerId.New();
        CreateChatGroupResult group = service.CreateGroup(creator, GroupRequest("g1", invitee.Value));

        Guid inviteId = service.ListPendingInvitations(invitee)[0].InviteId;
        service.RespondToInvitation(invitee, inviteId, accept: false);

        IReadOnlyList<ChatGroupInviteDto> responses = service.ListSentInvitationResponses(creator);
        Assert.Multiple(() =>
        {
            Assert.That(repository.GetParticipant(group.Conversation.ConversationId, invitee), Is.Null);
            Assert.That(responses, Has.Count.EqualTo(1));
            Assert.That(responses[0].Status, Is.EqualTo(ChatGroupInviteStatus.Declined));
        });

        service.AcknowledgeInvitationResponses(creator, [inviteId]);
        Assert.That(service.ListSentInvitationResponses(creator), Is.Empty, "an acknowledged refusal must not be re-notified at every poll");
    }

    [Test]
    public void RespondToInvitation_RejectsSomebodyElsesInvitationAndDoubleAnswers()
    {
        (ChatService service, _) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId invitee = PlayerId.New();
        service.CreateGroup(creator, GroupRequest("g1", invitee.Value));
        Guid inviteId = service.ListPendingInvitations(invitee)[0].InviteId;

        Assert.Throws<UnauthorizedAccessException>(() => service.RespondToInvitation(PlayerId.New(), inviteId, accept: true));
        service.RespondToInvitation(invitee, inviteId, accept: true);
        Assert.Throws<InvalidOperationException>(() => service.RespondToInvitation(invitee, inviteId, accept: true));
    }

    [Test]
    public void InviteToGroup_IsLeaderOnly()
    {
        (ChatService service, _) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId member = PlayerId.New();
        CreateChatGroupResult group = service.CreateGroup(creator, GroupRequest("g1", member.Value));
        service.RespondToInvitation(member, service.ListPendingInvitations(member)[0].InviteId, accept: true);

        Assert.Throws<UnauthorizedAccessException>(() => service.InviteToGroup(member, group.Conversation.ConversationId, PlayerId.New().Value));
        Assert.DoesNotThrow(() => service.InviteToGroup(creator, group.Conversation.ConversationId, PlayerId.New().Value));
    }

    [Test]
    public void InviteToGroup_RejectsAnExistingMember()
    {
        (ChatService service, _) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId member = PlayerId.New();
        CreateChatGroupResult group = service.CreateGroup(creator, GroupRequest("g1", member.Value));
        service.RespondToInvitation(member, service.ListPendingInvitations(member)[0].InviteId, accept: true);

        Assert.Throws<ArgumentException>(() => service.InviteToGroup(creator, group.Conversation.ConversationId, member.Value));
    }

    [Test]
    public void TransferLeadership_SwapsRolesBothWays()
    {
        (ChatService service, _) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId member = PlayerId.New();
        CreateChatGroupResult group = service.CreateGroup(creator, GroupRequest("g1", member.Value));
        service.RespondToInvitation(member, service.ListPendingInvitations(member)[0].InviteId, accept: true);

        service.TransferGroupLeadership(creator, group.Conversation.ConversationId, member.Value);

        ChatGroupDetail afterForMember = service.GetGroupDetail(member, group.Conversation.ConversationId);
        Assert.Multiple(() =>
        {
            Assert.That(afterForMember.ViewerIsLeader, Is.True);
            Assert.That(afterForMember.OwnerPlayerId, Is.EqualTo(member.Value));
            Assert.That(service.GetGroupDetail(creator, group.Conversation.ConversationId).ViewerIsLeader, Is.False);
        });

        // The former leader must really have lost his powers, not just his badge.
        Assert.Throws<UnauthorizedAccessException>(() => service.RemoveGroupMember(creator, group.Conversation.ConversationId, member.Value));
    }

    [Test]
    public void RemoveGroupMember_KicksAMemberAndCancelsAPendingInvitation()
    {
        (ChatService service, IChatRepository repository) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId member = PlayerId.New();
        PlayerId invited = PlayerId.New();
        CreateChatGroupResult group = service.CreateGroup(creator, GroupRequest("g1", member.Value, invited.Value));
        service.RespondToInvitation(member, service.ListPendingInvitations(member)[0].InviteId, accept: true);

        service.RemoveGroupMember(creator, group.Conversation.ConversationId, member.Value);
        service.RemoveGroupMember(creator, group.Conversation.ConversationId, invited.Value);

        Assert.Multiple(() =>
        {
            Assert.That(repository.GetParticipant(group.Conversation.ConversationId, member)!.RemovedAtUtc, Is.Not.Null);
            Assert.That(service.ListPendingInvitations(invited), Is.Empty);
            Assert.That(service.GetGroupDetail(creator, group.Conversation.ConversationId).Members, Has.Count.EqualTo(1));
        });

        Assert.Throws<InvalidOperationException>(() => service.RemoveGroupMember(creator, group.Conversation.ConversationId, creator.Value));
    }

    [Test]
    public void LeaveGroup_ForbidsALeaderWithMembersLeftButAllowsTheLastOneOut()
    {
        (ChatService service, _) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId member = PlayerId.New();
        CreateChatGroupResult group = service.CreateGroup(creator, GroupRequest("g1", member.Value));
        service.RespondToInvitation(member, service.ListPendingInvitations(member)[0].InviteId, accept: true);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => service.LeaveGroup(creator, group.Conversation.ConversationId))!;
        Assert.That(error.Message, Is.EqualTo("group_leader_must_transfer"));

        service.LeaveGroup(member, group.Conversation.ConversationId);
        Assert.DoesNotThrow(() => service.LeaveGroup(creator, group.Conversation.ConversationId));
    }

    [Test]
    public void AutoAcceptPreference_JoinsWithoutAskingEvenWhileTheInviteeIsOffline()
    {
        (ChatService service, IChatRepository repository) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId invitee = PlayerId.New();
        service.UpdatePreferences(invitee, new UpdateChatPreferencesRequest(ChatAutoInviteResponse.Accept));

        CreateChatGroupResult group = service.CreateGroup(creator, GroupRequest("g1", invitee.Value));

        Assert.Multiple(() =>
        {
            Assert.That(repository.GetParticipant(group.Conversation.ConversationId, invitee), Is.Not.Null);
            Assert.That(service.ListPendingInvitations(invitee), Is.Empty);
        });
    }

    [Test]
    public void AutoDeclinePreference_RefusesAndStillNotifiesTheInviter()
    {
        (ChatService service, IChatRepository repository) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId invitee = PlayerId.New();
        service.UpdatePreferences(invitee, new UpdateChatPreferencesRequest(ChatAutoInviteResponse.Decline));

        CreateChatGroupResult group = service.CreateGroup(creator, GroupRequest("g1", invitee.Value));

        Assert.Multiple(() =>
        {
            Assert.That(repository.GetParticipant(group.Conversation.ConversationId, invitee), Is.Null);
            Assert.That(service.ListPendingInvitations(invitee), Is.Empty);
            Assert.That(service.ListSentInvitationResponses(creator).Single().Status, Is.EqualTo(ChatGroupInviteStatus.Declined));
        });
    }

    [Test]
    public void Preferences_DefaultToAskAndRejectUnknownValues()
    {
        (ChatService service, _) = BuildStack();
        PlayerId player = PlayerId.New();

        Assert.That(service.GetPreferences(player).AutoInviteResponse, Is.EqualTo(ChatAutoInviteResponse.Ask));
        Assert.Throws<ArgumentException>(() => service.UpdatePreferences(player, new UpdateChatPreferencesRequest("whatever")));
    }

    [Test]
    public async Task GroupMembers_CanActuallyExchangeMessagesAndNonMembersCannotRead()
    {
        (ChatService service, _) = BuildStack();
        PlayerId creator = PlayerId.New();
        PlayerId member = PlayerId.New();
        PlayerId outsider = PlayerId.New();
        CreateChatGroupResult group = service.CreateGroup(creator, GroupRequest("g1", member.Value));
        service.RespondToInvitation(member, service.ListPendingInvitations(member)[0].InviteId, accept: true);

        await service.SendMessageAsync(member, group.Conversation.ConversationId, new SendChatMessageRequest("m1", "Bonjour la ruche", null, null, null, null, DateTimeOffset.UtcNow));

        Assert.Multiple(() =>
        {
            Assert.That(service.GetMessages(creator, group.Conversation.ConversationId, 0, 50).Items.Single().Body, Is.EqualTo("Bonjour la ruche"));
            Assert.That(() => service.GetMessages(outsider, group.Conversation.ConversationId, 0, 50), Throws.TypeOf<UnauthorizedAccessException>());
        });
    }

    [Test]
    public void GroupOperations_RejectANonGroupConversation()
    {
        (ChatService service, _) = BuildStack();
        PlayerId player = PlayerId.New();
        CreateChatConversationResult privateChat = service.CreateConversation(player, new CreateChatConversationRequest(
            ChatChannelType.Private, GameServerId, WorldId, null, "Prive", [PlayerId.New().Value], "p1"));

        Assert.Throws<ArgumentException>(() => service.GetGroupDetail(player, privateChat.Conversation.ConversationId));
    }
}
