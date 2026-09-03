using BeeKingdom.Alliance;
using BeeKingdom.Alliance.Configuration;
using BeeKingdom.Alliance.Integration;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.Chat;
using BeeKingdom.Chat.Audience;
using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Realtime;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeeKingdom.Tests;

// M042-CL: Part 3 (Alliance <-> Communication) exercised end-to-end - a real ChatService/
// ChatManager wired to the SAME IAllianceRepository AllianceService uses, exactly mirroring
// Program.cs's real DI graph (just built by hand instead of through the container).
public sealed class AllianceChatIntegrationTests
{
    private sealed class SystemClock : IServerClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

    private static (AllianceService Alliances, IChatRepository ChatRepo) BuildStack()
    {
        var allianceRepository = new InMemoryAllianceRepository();
        var chatRepository = new InMemoryChatRepository();
        var resolver = new LocalChatAudienceResolver(
            Options.Create(new ChatOptions { MaxPrivateRecipients = 20 }),
            new AllianceMembershipResolver(allianceRepository));
        var chatService = new ChatService(chatRepository, resolver, new NoopChatRealtimeDispatcher(), new SystemClock(), Options.Create(new ChatOptions { Enabled = true }));
        var chatManager = new ChatManager(chatService);

        var allianceOptions = Options.Create(new AllianceOptions { Enabled = true, MaxMembers = 100 });
        var alliances = new AllianceService(
            allianceRepository,
            new InMemoryAllianceActivityRepository(),
            new InMemoryAllianceDiplomacyRepository(),
            new InMemoryAllianceWarRepository(),
            allianceOptions,
            chatManager,
            chatRepository);
        return (alliances, chatRepository);
    }

    [Test]
    public void CreateAlliance_CreatesRealChatConversationAndAddsLeader()
    {
        (AllianceService alliances, IChatRepository chatRepo) = BuildStack();
        PlayerId leader = PlayerId.New();

        AllianceEntity alliance = alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "", "fr-CA", "", AllianceJoinMode.Open, "create-1")).Alliance;

        Assert.That(alliance.ChatConversationId, Is.Not.Null.And.Not.EqualTo(Guid.Empty));
        ChatConversationParticipant? leaderParticipant = chatRepo.GetParticipant(alliance.ChatConversationId!.Value, leader);
        Assert.That(leaderParticipant, Is.Not.Null);
        Assert.That(leaderParticipant!.Role, Is.EqualTo(ChatPermissionRole.Leader));
        Assert.That(leaderParticipant.RemovedAtUtc, Is.Null);
    }

    [Test]
    public void CreateAlliance_RetryDoesNotCreateASecondChatConversation()
    {
        (AllianceService alliances, IChatRepository _) = BuildStack();
        PlayerId leader = PlayerId.New();
        var request = new CreateAllianceRequest("Golden Hive", "GLD", "", "fr-CA", "", AllianceJoinMode.Open, "stable-key");

        AllianceEntity first = alliances.CreateAlliance(leader, request).Alliance;
        AllianceEntity retried = alliances.CreateAlliance(leader, request).Alliance;

        Assert.That(retried.ChatConversationId, Is.EqualTo(first.ChatConversationId));
    }

    [Test]
    public void JoinOpen_AddsRealChatParticipant()
    {
        (AllianceService alliances, IChatRepository chatRepo) = BuildStack();
        PlayerId leader = PlayerId.New();
        AllianceEntity alliance = alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "", "fr-CA", "", AllianceJoinMode.Open, "create-1")).Alliance;
        PlayerId member = PlayerId.New();

        alliances.JoinOpen(member, alliance.AllianceId);

        ChatConversationParticipant? participant = chatRepo.GetParticipant(alliance.ChatConversationId!.Value, member);
        Assert.That(participant, Is.Not.Null);
        Assert.That(participant!.RemovedAtUtc, Is.Null);
        Assert.That(participant.Role, Is.EqualTo(ChatPermissionRole.Member));
    }

    [Test]
    public void AcceptInvitation_AddsRealChatParticipant()
    {
        // M043T-CL: JoinOpen_AddsRealChatParticipant above covers the open-join path; this is the
        // same proof for the invite-then-accept path (the exact Stara scenario) - AcceptInvitation
        // calls the same SyncChatParticipantAdded as JoinOpen/AcceptApplication, but that was never
        // actually exercised against a real chat repository until now.
        (AllianceService alliances, IChatRepository chatRepo) = BuildStack();
        PlayerId leader = PlayerId.New();
        AllianceEntity alliance = alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "", "fr-CA", "", AllianceJoinMode.InviteOnly, "create-1")).Alliance;
        PlayerId invitee = PlayerId.New();
        AllianceInvitation invitation = alliances.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(invitee, "invite-1"));

        alliances.AcceptInvitation(invitee, invitation.InvitationId);

        ChatConversationParticipant? participant = chatRepo.GetParticipant(alliance.ChatConversationId!.Value, invitee);
        Assert.That(participant, Is.Not.Null);
        Assert.That(participant!.RemovedAtUtc, Is.Null);
        Assert.That(participant.Role, Is.EqualTo(ChatPermissionRole.Member));
        Assert.That(participant.CanRead, Is.True);
        Assert.That(participant.CanWrite, Is.True);
    }

    [Test]
    public void Kick_RemovesRealChatParticipant()
    {
        (AllianceService alliances, IChatRepository chatRepo) = BuildStack();
        PlayerId leader = PlayerId.New();
        AllianceEntity alliance = alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "", "fr-CA", "", AllianceJoinMode.Open, "create-1")).Alliance;
        PlayerId member = PlayerId.New();
        alliances.JoinOpen(member, alliance.AllianceId);

        alliances.Kick(leader, member);

        ChatConversationParticipant? participant = chatRepo.GetParticipant(alliance.ChatConversationId!.Value, member);
        Assert.That(participant, Is.Not.Null);
        Assert.That(participant!.RemovedAtUtc, Is.Not.Null);
    }

    [Test]
    public void Leave_RemovesRealChatParticipant()
    {
        (AllianceService alliances, IChatRepository chatRepo) = BuildStack();
        PlayerId leader = PlayerId.New();
        AllianceEntity alliance = alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "", "fr-CA", "", AllianceJoinMode.Open, "create-1")).Alliance;
        PlayerId member = PlayerId.New();
        alliances.JoinOpen(member, alliance.AllianceId);

        alliances.Leave(member);

        ChatConversationParticipant? participant = chatRepo.GetParticipant(alliance.ChatConversationId!.Value, member);
        Assert.That(participant!.RemovedAtUtc, Is.Not.Null);
    }

    [Test]
    public void EndToEnd_MemberCanSendMessageAfterJoin_KickedMemberIsRejectedByRealAudienceResolver()
    {
        var allianceRepository = new InMemoryAllianceRepository();
        var chatRepository = new InMemoryChatRepository();
        var resolver = new LocalChatAudienceResolver(
            Options.Create(new ChatOptions { MaxPrivateRecipients = 20 }),
            new AllianceMembershipResolver(allianceRepository));
        var chatService = new ChatService(chatRepository, resolver, new NoopChatRealtimeDispatcher(), new SystemClock(), Options.Create(new ChatOptions { Enabled = true }));
        var chatManager = new ChatManager(chatService);
        var alliances = new AllianceService(
            allianceRepository, new InMemoryAllianceActivityRepository(), new InMemoryAllianceDiplomacyRepository(), new InMemoryAllianceWarRepository(),
            Options.Create(new AllianceOptions { Enabled = true, MaxMembers = 100 }), chatManager, chatRepository);

        PlayerId leader = PlayerId.New();
        AllianceEntity alliance = alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "", "fr-CA", "", AllianceJoinMode.Open, "create-1")).Alliance;
        PlayerId member = PlayerId.New();
        alliances.JoinOpen(member, alliance.AllianceId);
        Guid conversationId = alliance.ChatConversationId!.Value;

        // A real, full round-trip through the audience resolver: the member's real server-side
        // role (not a client-declared one) is what authorizes reading this alliance conversation.
        chatManager.EnsureCanRead(member, conversationId); // must not throw

        alliances.Kick(leader, member);

        Assert.Throws<UnauthorizedAccessException>(() => chatManager.EnsureCanRead(member, conversationId));
    }

    // ---------------- M043Q-CL: real sender DisplayName snapshot ----------------

    private sealed class StubChatSenderDisplayNameResolver : IChatSenderDisplayNameResolver
    {
        private readonly Dictionary<Guid, string> names;
        public StubChatSenderDisplayNameResolver(Dictionary<Guid, string> names) { this.names = names; }
        public string? ResolveDisplayName(Guid playerId) => names.TryGetValue(playerId, out string? name) ? name : null;
    }

    [Test]
    public async Task SendMessage_UsesResolvedDisplayNameWhenResolverIsWired()
    {
        var allianceRepository = new InMemoryAllianceRepository();
        var chatRepository = new InMemoryChatRepository();
        var resolver = new LocalChatAudienceResolver(
            Options.Create(new ChatOptions { MaxPrivateRecipients = 20 }),
            new AllianceMembershipResolver(allianceRepository));
        PlayerId leader = PlayerId.New();
        var senderResolver = new StubChatSenderDisplayNameResolver(new Dictionary<Guid, string> { [leader.Value] = "QueenBee" });
        var chatService = new ChatService(chatRepository, resolver, new NoopChatRealtimeDispatcher(), new SystemClock(), Options.Create(new ChatOptions { Enabled = true }), senderResolver);
        var chatManager = new ChatManager(chatService);
        var alliances = new AllianceService(
            allianceRepository, new InMemoryAllianceActivityRepository(), new InMemoryAllianceDiplomacyRepository(), new InMemoryAllianceWarRepository(),
            Options.Create(new AllianceOptions { Enabled = true, MaxMembers = 100 }), chatManager, chatRepository);
        AllianceEntity alliance = alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "", "fr-CA", "", AllianceJoinMode.Open, "create-1")).Alliance;

        SendChatMessageResult result = await chatManager.SendMessageAsync(leader, alliance.ChatConversationId!.Value, new SendChatMessageRequest("send-1", "hello", null, null, null, null, DateTimeOffset.UtcNow));

        Assert.That(result.Message.SenderDisplayNameSnapshot, Is.EqualTo("QueenBee"));
    }

    [Test]
    public async Task SendMessage_FallsBackToPlayerIdWhenResolverHasNoName()
    {
        var allianceRepository = new InMemoryAllianceRepository();
        var chatRepository = new InMemoryChatRepository();
        var resolver = new LocalChatAudienceResolver(
            Options.Create(new ChatOptions { MaxPrivateRecipients = 20 }),
            new AllianceMembershipResolver(allianceRepository));
        PlayerId leader = PlayerId.New();
        var senderResolver = new StubChatSenderDisplayNameResolver(new Dictionary<Guid, string>());
        var chatService = new ChatService(chatRepository, resolver, new NoopChatRealtimeDispatcher(), new SystemClock(), Options.Create(new ChatOptions { Enabled = true }), senderResolver);
        var chatManager = new ChatManager(chatService);
        var alliances = new AllianceService(
            allianceRepository, new InMemoryAllianceActivityRepository(), new InMemoryAllianceDiplomacyRepository(), new InMemoryAllianceWarRepository(),
            Options.Create(new AllianceOptions { Enabled = true, MaxMembers = 100 }), chatManager, chatRepository);
        AllianceEntity alliance = alliances.CreateAlliance(leader, new CreateAllianceRequest("Golden Hive", "GLD", "", "fr-CA", "", AllianceJoinMode.Open, "create-1")).Alliance;

        SendChatMessageResult result = await chatManager.SendMessageAsync(leader, alliance.ChatConversationId!.Value, new SendChatMessageRequest("send-1", "hello", null, null, null, null, DateTimeOffset.UtcNow));

        Assert.That(result.Message.SenderDisplayNameSnapshot, Is.EqualTo($"player:{leader.Value:N}"));
    }
}
