using BeeKingdom.Alliance;
using BeeKingdom.Alliance.Configuration;
using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

public sealed class AllianceServiceTests
{
    private static AllianceService CreateService(int maxMembers = 100, bool diplomacyEnabled = true, bool warEnabled = true)
    {
        var options = Options.Create(new AllianceOptions
        {
            Enabled = true,
            DiplomacyEnabled = diplomacyEnabled,
            WarEnabled = warEnabled,
            MaxMembers = maxMembers
        });
        return new AllianceService(
            new InMemoryAllianceRepository(),
            new InMemoryAllianceActivityRepository(),
            new InMemoryAllianceDiplomacyRepository(),
            new InMemoryAllianceWarRepository(),
            options);
    }

    private static PlayerId NewPlayer() => PlayerId.New();

    private static AllianceEntity CreateAlliance(AllianceService service, PlayerId leader, AllianceJoinMode joinMode = AllianceJoinMode.Open, string name = "Golden Hive", string tag = "GLD")
        => service.CreateAlliance(leader, new CreateAllianceRequest(name, tag, "desc", "fr-CA", "", joinMode, "create-" + leader.Value)).Alliance;

    // ---------------- Create ----------------

    [Test]
    public void CreateAlliance_AcceptsRealCeoPayloadUnderProductionDefaultOptions()
    {
        // M043L-CL: uses the AllianceOptions property-initializer defaults (no override), exactly
        // matching production (appsettings.json/appsettings.Production.json only set
        // Enabled/DiplomacyEnabled/WarEnabled/MaxMembers, never Name/Tag/Description bounds) - and
        // the exact CEO Create form values captured live from the Play Mode session during the
        // alliance.invalid_request investigation. This passing test is what proved the server's own
        // validation was never the problem - the real bug was the request body serializing to "{}"
        // client-side (see UnityAuthenticatedGameRestContracts.SystemTextGameJsonCodec, IncludeFields).
        var options = Options.Create(new AllianceOptions { Enabled = true, MaxMembers = 100 });
        var service = new AllianceService(
            new InMemoryAllianceRepository(),
            new InMemoryAllianceActivityRepository(),
            new InMemoryAllianceDiplomacyRepository(),
            new InMemoryAllianceWarRepository(),
            options);
        PlayerId leader = NewPlayer();
        var request = new CreateAllianceRequest(
            "BeeKingdom Alpha", "BKA", "Alliance officielle de test Alpha BeeKingdom",
            "fr-CA", "", AllianceJoinMode.InviteOnly, "mobile-alliance-create-" + Guid.NewGuid().ToString("N"));

        CreateAllianceResult result = service.CreateAlliance(leader, request);

        Assert.That(result.Alliance.Name, Is.EqualTo("BeeKingdom Alpha"));
    }

    [Test]
    public void CreateAlliance_MakesCreatorLeaderAndIsIdempotent()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        var request = new CreateAllianceRequest("Golden Hive", "GLD", "desc", "fr-CA", "emblem_01", AllianceJoinMode.Open, "req-1");

        CreateAllianceResult first = service.CreateAlliance(leader, request);
        CreateAllianceResult second = service.CreateAlliance(leader, request);

        Assert.That(first.Deduplicated, Is.False);
        Assert.That(second.Deduplicated, Is.True);
        Assert.That(second.Alliance.AllianceId, Is.EqualTo(first.Alliance.AllianceId));
        Assert.That(first.Alliance.LeaderPlayerId, Is.EqualTo(leader));
        Assert.That(first.Alliance.MemberCount, Is.EqualTo(1));
        Assert.That(first.Alliance.PublicSlug, Is.EqualTo("golden-hive"));
    }

    // ---------------- GetMyAlliance (M043-CL) ----------------

    [Test]
    public void GetMyAlliance_ReturnsNullWhenPlayerHasNoActiveMembership()
    {
        AllianceService service = CreateService();
        Assert.That(service.GetMyAlliance(NewPlayer()), Is.Null);
    }

    [Test]
    public void GetMyAlliance_ReturnsAllianceAndOwnMembershipWhenPresent()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);

        MyAllianceOverview? overview = service.GetMyAlliance(leader);

        Assert.That(overview, Is.Not.Null);
        Assert.That(overview!.Alliance.AllianceId, Is.EqualTo(alliance.AllianceId));
        Assert.That(overview.Membership.PlayerId, Is.EqualTo(leader));
        Assert.That(overview.Membership.Role, Is.EqualTo(AllianceRole.Leader));
    }

    [Test]
    public void GetMyAlliance_ReturnsNullAfterLeaving()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId member = NewPlayer();
        service.JoinOpen(member, alliance.AllianceId);

        service.Leave(member);

        Assert.That(service.GetMyAlliance(member), Is.Null);
    }

    [Test]
    public void CreateAlliance_PlayerAlreadyInAllianceIsRejected()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        CreateAlliance(service, leader);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.CreateAlliance(leader, new CreateAllianceRequest("Second", "SEC", "", "fr-CA", "", AllianceJoinMode.Open, "req-2")));
        Assert.That(ex!.Message, Is.EqualTo("already_in_alliance"));
    }

    [Test]
    public void CreateAlliance_InvalidNameOrTagRejected()
    {
        AllianceService service = CreateService();
        Assert.Throws<ArgumentException>(() =>
            service.CreateAlliance(NewPlayer(), new CreateAllianceRequest("a", "GLD", "", "fr-CA", "", AllianceJoinMode.Open, "r")));
        Assert.Throws<ArgumentException>(() =>
            service.CreateAlliance(NewPlayer(), new CreateAllianceRequest("Golden Hive", "G", "", "fr-CA", "", AllianceJoinMode.Open, "r")));
    }

    // ---------------- Search ----------------

    [Test]
    public void Search_FiltersByNameOrTag()
    {
        AllianceService service = CreateService();
        CreateAlliance(service, NewPlayer(), name: "Golden Hive", tag: "GLD");
        CreateAlliance(service, NewPlayer(), name: "Silver Wasp", tag: "SLV");

        AllianceSearchPage page = service.Search(new AllianceSearchQuery("Golden", null, null, 0, 20));
        Assert.That(page.TotalCount, Is.EqualTo(1));
        Assert.That(page.Items.Single().Name, Is.EqualTo("Golden Hive"));
    }

    // ---------------- Open join ----------------

    [Test]
    public void JoinOpen_AddsMemberAndEmitsActivity()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId joiner = NewPlayer();

        JoinOpenAllianceResult result = service.JoinOpen(joiner, alliance.AllianceId);

        Assert.That(result.Alliance.MemberCount, Is.EqualTo(2));
        Assert.That(result.Membership.Role, Is.EqualTo(AllianceRole.Member));

        AllianceActivityPage activity = service.ListPublicActivity(alliance.AllianceId, null, 10);
        Assert.That(activity.Items.Any(e => e.Type == AllianceActivityType.MemberJoined && e.ActorPlayerId == joiner), Is.True);
    }

    [Test]
    public void JoinOpen_RejectsWhenAtCapacity()
    {
        AllianceService service = CreateService(maxMembers: 1);
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);

        var ex = Assert.Throws<InvalidOperationException>(() => service.JoinOpen(NewPlayer(), alliance.AllianceId));
        Assert.That(ex!.Message, Is.EqualTo("capacity_full"));
    }

    [Test]
    public void JoinOpen_RejectsNonOpenAlliance()
    {
        AllianceService service = CreateService();
        AllianceEntity alliance = CreateAlliance(service, NewPlayer(), AllianceJoinMode.Application);
        Assert.Throws<InvalidOperationException>(() => service.JoinOpen(NewPlayer(), alliance.AllianceId));
    }

    // ---------------- Applications ----------------

    [Test]
    public void Application_SubmitAcceptFlow()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.Application);
        PlayerId applicant = NewPlayer();

        AllianceApplication application = service.SubmitApplication(applicant, alliance.AllianceId, new SubmitApplicationRequest("please", "app-1"));
        Assert.That(application.Status, Is.EqualTo(AllianceApplicationStatus.Pending));

        ApplicationDecisionResult accepted = service.AcceptApplication(leader, application.ApplicationId);
        Assert.That(accepted.Application.Status, Is.EqualTo(AllianceApplicationStatus.Accepted));
        Assert.That(accepted.Membership, Is.Not.Null);
        Assert.That(service.ListPublicActivity(alliance.AllianceId, null, 10).Items.Any(e => e.Type == AllianceActivityType.MemberJoined), Is.True);
    }

    [Test]
    public void Application_RejectDoesNotCreateMembership()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.Application);
        PlayerId applicant = NewPlayer();
        AllianceApplication application = service.SubmitApplication(applicant, alliance.AllianceId, new SubmitApplicationRequest("", "app-2"));

        ApplicationDecisionResult rejected = service.RejectApplication(leader, application.ApplicationId);
        Assert.That(rejected.Application.Status, Is.EqualTo(AllianceApplicationStatus.Rejected));
        Assert.That(rejected.Membership, Is.Null);
    }

    [Test]
    public void Application_NonOfficerCannotAccept()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.Application);
        PlayerId member = NewPlayer();
        service.JoinOpen(member, CreateAlliance(service, NewPlayer(), AllianceJoinMode.Open).AllianceId); // unrelated alliance, just to have a "member" role identity elsewhere
        PlayerId applicant = NewPlayer();
        AllianceApplication application = service.SubmitApplication(applicant, alliance.AllianceId, new SubmitApplicationRequest("", "app-3"));

        // A random outsider (not even a member of THIS alliance) must be denied.
        Assert.Throws<InvalidOperationException>(() => service.AcceptApplication(member, application.ApplicationId));
    }

    [Test]
    public void Application_CancelIsOwnerOnly()
    {
        AllianceService service = CreateService();
        AllianceEntity alliance = CreateAlliance(service, NewPlayer(), AllianceJoinMode.Application);
        PlayerId applicant = NewPlayer();
        AllianceApplication application = service.SubmitApplication(applicant, alliance.AllianceId, new SubmitApplicationRequest("", "app-4"));

        Assert.Throws<UnauthorizedAccessException>(() => service.CancelApplication(NewPlayer(), application.ApplicationId));
        AllianceApplication cancelled = service.CancelApplication(applicant, application.ApplicationId);
        Assert.That(cancelled.Status, Is.EqualTo(AllianceApplicationStatus.Cancelled));
    }

    // ---------------- Invitations ----------------

    [Test]
    public void Invitation_CreateAcceptFlow()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.InviteOnly);
        PlayerId invitee = NewPlayer();

        AllianceInvitation invitation = service.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(invitee, "inv-1"));
        Assert.That(service.ListMyInvitations(invitee).Any(i => i.InvitationId == invitation.InvitationId), Is.True);

        InvitationDecisionResult accepted = service.AcceptInvitation(invitee, invitation.InvitationId);
        Assert.That(accepted.Membership, Is.Not.Null);
        Assert.That(accepted.Membership!.Role, Is.EqualTo(AllianceRole.Member));
        Assert.That(accepted.Invitation.Status, Is.EqualTo(AllianceInvitationStatus.Accepted));

        Assert.That(service.ListPublicActivity(alliance.AllianceId, null, 10).Items
            .Any(e => e.Type == AllianceActivityType.MemberJoined && e.ActorPlayerId == invitee), Is.True,
            "AcceptInvitation must publish a MemberJoined activity for the invitee, same as JoinOpen/AcceptApplication.");
    }

    [Test]
    public void Invitation_OnlyInviteeCanAcceptOrDecline()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId invitee = NewPlayer();
        AllianceInvitation invitation = service.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(invitee, "inv-2"));

        Assert.Throws<UnauthorizedAccessException>(() => service.AcceptInvitation(NewPlayer(), invitation.InvitationId));
    }

    [Test]
    public void Invitation_AcceptRetried_DoesNotDuplicateMembershipOrMemberCount()
    {
        // M043T-CL: a double-tap on "Accept" (or a retry after a lost/ambiguous response) must not
        // create a second AllianceMembership row or double-increment MemberCount. AcceptInvitation's
        // own early branch for a non-Pending invitation already Accepted returns the existing
        // decision instead of re-running the membership-creation path - this proves that branch
        // actually holds under a real double call, not just by reading the code.
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.InviteOnly);
        PlayerId invitee = NewPlayer();
        AllianceInvitation invitation = service.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(invitee, "inv-retry-1"));

        InvitationDecisionResult first = service.AcceptInvitation(invitee, invitation.InvitationId);
        InvitationDecisionResult second = service.AcceptInvitation(invitee, invitation.InvitationId);

        Assert.That(second.Invitation.Status, Is.EqualTo(AllianceInvitationStatus.Accepted));
        Assert.That(second.Membership, Is.Not.Null);
        Assert.That(second.Membership!.PlayerId, Is.EqualTo(first.Membership!.PlayerId));

        IReadOnlyList<AllianceMemberSummary> members = service.ListMembers(leader, alliance.AllianceId);
        Assert.That(members.Count(m => m.PlayerId == invitee), Is.EqualTo(1));
        Assert.That(service.GetPublicProfile(alliance.AllianceId).MemberCount, Is.EqualTo(2), "leader + invitee, not 3");
    }

    [Test]
    public void Invitation_MemberCannotInvite()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId member = NewPlayer();
        service.JoinOpen(member, alliance.AllianceId);

        Assert.Throws<UnauthorizedAccessException>(() => service.CreateInvitation(member, alliance.AllianceId, new CreateInvitationRequest(NewPlayer(), "inv-3")));
    }

    // ---------------- M043S-CL: real Invite-flow idempotency (the exact Stara-shaped scenario) ----------------

    [Test]
    public void CreateInvitationRequest_DeserializesTheExactWireShapeAllianceClientSends()
    {
        // M043S-CL: the real, confirmed production bug - PlayerId has no [JsonConverter] of its
        // own (by design), so without a converter on this specific property, System.Text.Json
        // expects an object shape ({"invitedPlayerId":{"value":"<guid>"}}) but
        // AllianceClient.CreateInvitationWireRequest (like every client request DTO in this
        // codebase) sends InvitedPlayerId as a bare GUID string. That mismatch threw inside
        // ASP.NET's own request-body binding for every real CreateInvitation call ever made -
        // proven via a production AllianceInvitations table that was completely empty despite real
        // invite attempts. This reproduces the server's actual ConfigureHttpJsonOptions shape
        // (camelCase property names, string enums) against the exact wire JSON the client sends.
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        Guid invitedPlayerId = Guid.NewGuid();
        string wireJson = "{\"invitedPlayerId\":\"" + invitedPlayerId.ToString("D") + "\",\"clientRequestId\":\"mobile-alliance-invite-key\"}";

        CreateInvitationRequest? request = System.Text.Json.JsonSerializer.Deserialize<CreateInvitationRequest>(wireJson, options);

        Assert.That(request, Is.Not.Null);
        Assert.That(request!.InvitedPlayerId.Value, Is.EqualTo(invitedPlayerId));
        Assert.That(request.ClientRequestId, Is.EqualTo("mobile-alliance-invite-key"));
    }

    // ---------------- M043T-CL: real Accept-flow wire shape (server -> client this time) ----------------
    //
    // First pass here assumed PlayerId/AllianceId (no [JsonConverter] of their own, confirmed by
    // reading Identifiers.cs) would serialize as {"value":"<guid>"} and break every Unity DTO that
    // declares the same field as a bare System.Guid (RemoteAllianceInvitation.InvitedPlayerId,
    // RemoteAllianceMembership.PlayerId, RemoteAllianceMemberSummary.PlayerId, etc.) - confirmed true
    // by direct serialization below. But Unity's actual codec (SystemTextGameJsonCodec,
    // Assets/BeeKingdom/Networking/AuthenticatedGameRestContracts.cs) registers a custom
    // BeeGuidJsonConverter on System.Text.Json's `Guid` itself, which reads EITHER a bare string OR
    // an object with a "value"/"Value" string property - this was already added (M043-CL era, exact
    // commit not traced) specifically to tolerate this shape mismatch project-wide. So the
    // server->client contract for Accept/ListMyInvitations/ListMembers already works today; this is
    // a proof of that, not a bug fix. Left in as regression coverage - if BeeGuidJsonConverter's
    // object-shape branch is ever removed from the client without a matching server-side converter
    // being added, these tests must still pass or the next call site to add a PlayerId/AllianceId
    // response field silently breaks the exact same way M043S's request-side bug did.
    private static Guid ParseAsUnityBeeGuidConverterWould(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.String)
            return Guid.ParseExact(element.GetString()!, "D");
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object
            && (element.TryGetProperty("value", out var value) || element.TryGetProperty("Value", out value))
            && value.ValueKind == System.Text.Json.JsonValueKind.String)
            return Guid.ParseExact(value.GetString()!, "D");
        throw new System.Text.Json.JsonException("A game identifier is malformed.");
    }

    [Test]
    public void AcceptInvitation_ResponseIdsRoundTripThroughUnitysGuidConverter()
    {
        var serverOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId invitee = NewPlayer();
        AllianceInvitation invitation = service.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(invitee, "invite-key-accept-shape"));

        InvitationDecisionResult decision = service.AcceptInvitation(invitee, invitation.InvitationId);
        string wireJson = System.Text.Json.JsonSerializer.Serialize(decision, serverOptions);

        // Confirmed: the server DOES emit the object-wrapped shape (no converter on PlayerId/
        // AllianceId themselves, by design - see Identifiers.cs). Not asserting this away; asserting
        // the client-side tolerance that actually makes it work end-to-end.
        Assert.That(wireJson, Does.Contain("\"value\":\"" + invitee.Value.ToString("D") + "\""));

        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(wireJson);
        Guid roundTrippedInvitedPlayerId = ParseAsUnityBeeGuidConverterWould(document.RootElement.GetProperty("invitation").GetProperty("invitedPlayerId"));
        Guid roundTrippedMembershipPlayerId = ParseAsUnityBeeGuidConverterWould(document.RootElement.GetProperty("membership").GetProperty("playerId"));
        Assert.That(roundTrippedInvitedPlayerId, Is.EqualTo(invitee.Value));
        Assert.That(roundTrippedMembershipPlayerId, Is.EqualTo(invitee.Value));
    }

    [Test]
    public void ListMyInvitations_ResponseIdsRoundTripThroughUnitysGuidConverter()
    {
        // Stara's client calls GET /alliance/v1/invitations/mine BEFORE ever reaching Accept - this
        // proves that step of the flow independently, not just the Accept response itself.
        var serverOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId invitee = NewPlayer();
        service.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(invitee, "invite-key-list-shape"));

        IReadOnlyList<AllianceInvitation> mine = service.ListMyInvitations(invitee);
        string wireJson = System.Text.Json.JsonSerializer.Serialize(mine, serverOptions);

        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(wireJson);
        Guid roundTrippedAllianceId = ParseAsUnityBeeGuidConverterWould(document.RootElement[0].GetProperty("allianceId"));
        Guid roundTrippedInvitedPlayerId = ParseAsUnityBeeGuidConverterWould(document.RootElement[0].GetProperty("invitedPlayerId"));
        Assert.That(roundTrippedAllianceId, Is.EqualTo(alliance.AllianceId.Value));
        Assert.That(roundTrippedInvitedPlayerId, Is.EqualTo(invitee.Value));
    }

    [Test]
    public void ListMembers_ResponseIdsRoundTripThroughUnitysGuidConverter()
    {
        // Same question for the roster screen the acceptance flow's memberCount check depends on -
        // AllianceMemberSummary.PlayerId is also an un-converted PlayerId server-side.
        var serverOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);

        IReadOnlyList<AllianceMemberSummary> members = service.ListMembers(leader, alliance.AllianceId);
        string wireJson = System.Text.Json.JsonSerializer.Serialize(members, serverOptions);

        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(wireJson);
        Guid roundTrippedPlayerId = ParseAsUnityBeeGuidConverterWould(document.RootElement[0].GetProperty("playerId"));
        Assert.That(roundTrippedPlayerId, Is.EqualTo(leader.Value));
    }

    [Test]
    public void Invitation_SecondCallForSameStillPendingTarget_ThrowsAlreadyInvited_NoDuplicateRowCreated()
    {
        // A second real click (or a retry after the first click's response was lost/never showed
        // feedback client-side) must never create a second pending invitation for the same target -
        // this is the server invariant the Unity UI's AlreadyPending state relies on.
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.InviteOnly);
        PlayerId invitee = NewPlayer();
        AllianceInvitation first = service.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(invitee, "stara-invite-1"));

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(
            () => service.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(invitee, "stara-invite-2")));

        Assert.That(thrown!.Message, Is.EqualTo("already_invited"));
        Assert.That(service.ListMyInvitations(invitee).Count(i => i.InvitationId == first.InvitationId), Is.EqualTo(1));
    }

    [Test]
    public void Invitation_SameClientRequestIdReplayed_IsIdempotent_ReturnsSameInvitationNoDuplicate()
    {
        // A genuine network retry (same click, same generated ClientRequestId resent because the
        // first response never reached the client) must replay the exact same invitation, not
        // create a second one and not throw already_invited against itself.
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.InviteOnly);
        PlayerId invitee = NewPlayer();

        AllianceInvitation first = service.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(invitee, "stara-replay"));
        AllianceInvitation replayed = service.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(invitee, "stara-replay"));

        Assert.That(replayed.InvitationId, Is.EqualTo(first.InvitationId));
        Assert.That(service.ListMyInvitations(invitee).Count(i => i.InvitationId == first.InvitationId), Is.EqualTo(1));
    }

    // ---------------- Roles / promote / demote / kick / leave ----------------

    [Test]
    public void Promote_ThenDemote_RoundTrips()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId member = NewPlayer();
        service.JoinOpen(member, alliance.AllianceId);

        AllianceMembership promoted = service.Promote(leader, member);
        Assert.That(promoted.Role, Is.EqualTo(AllianceRole.Officer));
        Assert.That(service.ListPublicActivity(alliance.AllianceId, null, 10).Items.Any(e => e.Type == AllianceActivityType.MemberPromoted), Is.True);

        AllianceMembership demoted = service.Demote(leader, member);
        Assert.That(demoted.Role, Is.EqualTo(AllianceRole.Member));
    }

    [Test]
    public void Promote_OnlyLeaderCan()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId officer = NewPlayer();
        service.JoinOpen(officer, alliance.AllianceId);
        service.Promote(leader, officer);
        PlayerId member = NewPlayer();
        service.JoinOpen(member, alliance.AllianceId);

        Assert.Throws<UnauthorizedAccessException>(() => service.Promote(officer, member));
    }

    [Test]
    public void Kick_OfficerCanKickMemberButNotOfficer()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId officer = NewPlayer();
        service.JoinOpen(officer, alliance.AllianceId);
        service.Promote(leader, officer);
        PlayerId otherOfficer = NewPlayer();
        service.JoinOpen(otherOfficer, alliance.AllianceId);
        service.Promote(leader, otherOfficer);
        PlayerId member = NewPlayer();
        service.JoinOpen(member, alliance.AllianceId);

        Assert.Throws<UnauthorizedAccessException>(() => service.Kick(officer, otherOfficer));
        service.Kick(officer, member); // officer kicking a plain member is allowed
        Assert.That(service.ListPublicActivity(alliance.AllianceId, null, 10).Items.Any(e => e.Type == AllianceActivityType.MemberKicked), Is.True);
    }

    [Test]
    public void Leave_LeaderMustTransferOrDissolveFirst()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        CreateAlliance(service, leader);

        var ex = Assert.Throws<InvalidOperationException>(() => service.Leave(leader));
        Assert.That(ex!.Message, Is.EqualTo("leader_must_transfer_or_dissolve"));
    }

    [Test]
    public void Leave_MemberReducesCountAndFreesSlotForNewAlliance()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId member = NewPlayer();
        service.JoinOpen(member, alliance.AllianceId);

        service.Leave(member);
        Assert.That(service.GetPublicProfile(alliance.AllianceId).MemberCount, Is.EqualTo(1));

        // one-player-one-alliance invariant: after leaving, the player can join another.
        AllianceEntity other = CreateAlliance(service, NewPlayer());
        Assert.DoesNotThrow(() => service.JoinOpen(member, other.AllianceId));
    }

    // ---------------- Leadership transfer ----------------

    [Test]
    public void TransferLeadership_SwapsRoles()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId member = NewPlayer();
        service.JoinOpen(member, alliance.AllianceId);

        LeadershipTransferResult result = service.TransferLeadership(leader, member);
        Assert.That(result.Alliance.LeaderPlayerId, Is.EqualTo(member));
        Assert.That(result.PreviousLeader.Role, Is.EqualTo(AllianceRole.Officer));
        Assert.That(result.NewLeader.Role, Is.EqualTo(AllianceRole.Leader));

        // Old leader can no longer transfer (not leader anymore).
        Assert.Throws<UnauthorizedAccessException>(() => service.TransferLeadership(leader, member));
    }

    // ---------------- Dissolve ----------------

    [Test]
    public void Dissolve_ClosesMembershipsAndCancelsApplications()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.Application);
        PlayerId applicant = NewPlayer();
        AllianceApplication application = service.SubmitApplication(applicant, alliance.AllianceId, new SubmitApplicationRequest("", "app-5"));

        AllianceEntity disbanded = service.Dissolve(leader);
        Assert.That(disbanded.Status, Is.EqualTo(AllianceStatus.Disbanded));
        Assert.That(service.GetApplicationForProof(application.ApplicationId)!.Status, Is.EqualTo(AllianceApplicationStatus.Cancelled));

        // Leader can now create a new alliance since their membership was closed.
        Assert.DoesNotThrow(() => service.CreateAlliance(leader, new CreateAllianceRequest("New Hive", "NEW", "", "fr-CA", "", AllianceJoinMode.Open, "req-new")));
    }

    // ---------------- Profile ----------------

    [Test]
    public void UpdateProfile_RequiresRevisionMatchAndPermission()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId member = NewPlayer();
        service.JoinOpen(member, alliance.AllianceId);

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.UpdateProfile(member, new UpdateAllianceProfileRequest("new desc", null, null, null, alliance.Revision)));

        var wrongRevision = Assert.Throws<InvalidOperationException>(() =>
            service.UpdateProfile(leader, new UpdateAllianceProfileRequest("new desc", null, null, null, alliance.Revision + 99)));
        Assert.That(wrongRevision!.Message, Is.EqualTo("revision_conflict"));

        // The join above already bumped the alliance's real revision past the stale local
        // `alliance` snapshot (JoinOpen increments Revision by 1) - reuse that, not the original.
        AllianceEntity updated = service.UpdateProfile(leader, new UpdateAllianceProfileRequest("new desc", null, null, null, alliance.Revision + 1));
        Assert.That(updated.Description, Is.EqualTo("new desc"));
    }

    // ---------------- Activity ----------------

    [Test]
    public void Activity_PublicFeedExcludesMembersOnlyEvents()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        service.UpdateProfile(leader, new UpdateAllianceProfileRequest("desc2", null, null, null, alliance.Revision)); // ProfileUpdated is MembersOnly

        AllianceActivityPage publicFeed = service.ListPublicActivity(alliance.AllianceId, null, 20);
        Assert.That(publicFeed.Items.Any(e => e.Type == AllianceActivityType.ProfileUpdated), Is.False);

        AllianceActivityPage memberFeed = service.ListActivity(leader, alliance.AllianceId, null, 20);
        Assert.That(memberFeed.Items.Any(e => e.Type == AllianceActivityType.ProfileUpdated), Is.True);
    }

    [Test]
    public void Activity_PaginationIsStableBySequence()
    {
        AllianceService service = CreateService(maxMembers: 20);
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        for (int i = 0; i < 5; i++) service.JoinOpen(NewPlayer(), alliance.AllianceId);

        AllianceActivityPage firstPage = service.ListPublicActivity(alliance.AllianceId, null, 3);
        Assert.That(firstPage.Items, Has.Count.EqualTo(3));
        AllianceActivityPage secondPage = service.ListPublicActivity(alliance.AllianceId, firstPage.NextBeforeSequence, 3);
        Assert.That(secondPage.Items.Select(e => e.Sequence), Is.All.LessThan(firstPage.Items.Min(e => e.Sequence)));
    }

    // ---------------- Diplomacy ----------------

    [Test]
    public void Diplomacy_ProposeAcceptCreatesActiveRelation()
    {
        AllianceService service = CreateService();
        PlayerId leaderA = NewPlayer();
        AllianceEntity allianceA = CreateAlliance(service, leaderA, name: "Alliance A", tag: "AAA");
        PlayerId leaderB = NewPlayer();
        AllianceEntity allianceB = CreateAlliance(service, leaderB, name: "Alliance B", tag: "BBB");

        DiplomacyDecisionResult proposal = service.ProposeRelation(leaderA, allianceB.AllianceId, new ProposeDiplomacyRequest(AllianceRelationType.NonAggressionPact, "dip-1"));
        Assert.That(proposal.Relation.Status, Is.EqualTo(AllianceRelationStatus.Proposed));

        DiplomacyDecisionResult accepted = service.RespondToRelation(leaderB, allianceA.AllianceId, true);
        Assert.That(accepted.Relation.Status, Is.EqualTo(AllianceRelationStatus.Active));
        Assert.That(service.GetPublicProfile(allianceA.AllianceId).Diplomacy!.NonAggressionPactCount, Is.EqualTo(1));
    }

    [Test]
    public void Diplomacy_ProposerCannotAcceptOwnProposal()
    {
        AllianceService service = CreateService();
        PlayerId leaderA = NewPlayer();
        AllianceEntity allianceA = CreateAlliance(service, leaderA, name: "Alliance A", tag: "AAA");
        PlayerId leaderB = NewPlayer();
        AllianceEntity allianceB = CreateAlliance(service, leaderB, name: "Alliance B", tag: "BBB");
        service.ProposeRelation(leaderA, allianceB.AllianceId, new ProposeDiplomacyRequest(AllianceRelationType.Ally, "dip-2"));

        Assert.Throws<UnauthorizedAccessException>(() => service.RespondToRelation(leaderA, allianceB.AllianceId, true));
    }

    [Test]
    public void Diplomacy_NonLeaderCannotPropose()
    {
        AllianceService service = CreateService();
        PlayerId leaderA = NewPlayer();
        AllianceEntity allianceA = CreateAlliance(service, leaderA, name: "Alliance A", tag: "AAA");
        PlayerId memberA = NewPlayer();
        service.JoinOpen(memberA, allianceA.AllianceId);
        AllianceEntity allianceB = CreateAlliance(service, NewPlayer(), name: "Alliance B", tag: "BBB");

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.ProposeRelation(memberA, allianceB.AllianceId, new ProposeDiplomacyRequest(AllianceRelationType.NonAggressionPact, "dip-3")));
    }

    [Test]
    public void Diplomacy_DuplicateAcceptIsSafe()
    {
        AllianceService service = CreateService();
        PlayerId leaderA = NewPlayer();
        AllianceEntity allianceA = CreateAlliance(service, leaderA, name: "Alliance A", tag: "AAA");
        PlayerId leaderB = NewPlayer();
        AllianceEntity allianceB = CreateAlliance(service, leaderB, name: "Alliance B", tag: "BBB");
        service.ProposeRelation(leaderA, allianceB.AllianceId, new ProposeDiplomacyRequest(AllianceRelationType.Ally, "dip-4"));

        service.RespondToRelation(leaderB, allianceA.AllianceId, true);
        DiplomacyDecisionResult second = service.RespondToRelation(leaderB, allianceA.AllianceId, true);
        Assert.That(second.Relation.Status, Is.EqualTo(AllianceRelationStatus.Active));
    }

    // ---------------- War foundation ----------------

    [Test]
    public void War_DeclareSucceedsAndEmitsActivity()
    {
        AllianceService service = CreateService();
        PlayerId leaderA = NewPlayer();
        AllianceEntity allianceA = CreateAlliance(service, leaderA, name: "Alliance A", tag: "AAA");
        AllianceEntity allianceB = CreateAlliance(service, NewPlayer(), name: "Alliance B", tag: "BBB");

        DeclareWarResult result = service.DeclareWar(leaderA, new DeclareWarRequest(allianceB.AllianceId, "war-1"));
        Assert.That(result.War.Status, Is.EqualTo(AllianceWarStatus.Declared));
        Assert.That(result.War.AttackerAllianceId, Is.EqualTo(allianceA.AllianceId));
        Assert.That(service.ListPublicActivity(allianceA.AllianceId, null, 10).Items.Any(e => e.Type == AllianceActivityType.AllianceWarDeclared), Is.True);
        Assert.That(service.ListPublicActivity(allianceB.AllianceId, null, 10).Items.Any(e => e.Type == AllianceActivityType.AllianceWarDeclared), Is.True);
    }

    [Test]
    public void War_CannotDeclareOnSelf()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        Assert.Throws<InvalidOperationException>(() => service.DeclareWar(leader, new DeclareWarRequest(alliance.AllianceId, "war-2")));
    }

    [Test]
    public void War_OnlyLeaderCanDeclare()
    {
        AllianceService service = CreateService();
        PlayerId leaderA = NewPlayer();
        AllianceEntity allianceA = CreateAlliance(service, leaderA, name: "Alliance A", tag: "AAA");
        PlayerId memberA = NewPlayer();
        service.JoinOpen(memberA, allianceA.AllianceId);
        AllianceEntity allianceB = CreateAlliance(service, NewPlayer(), name: "Alliance B", tag: "BBB");

        Assert.Throws<UnauthorizedAccessException>(() => service.DeclareWar(memberA, new DeclareWarRequest(allianceB.AllianceId, "war-3")));
    }

    [Test]
    public void War_CannotDeclareDuplicateActiveWar()
    {
        AllianceService service = CreateService();
        PlayerId leaderA = NewPlayer();
        AllianceEntity allianceA = CreateAlliance(service, leaderA, name: "Alliance A", tag: "AAA");
        AllianceEntity allianceB = CreateAlliance(service, NewPlayer(), name: "Alliance B", tag: "BBB");
        service.DeclareWar(leaderA, new DeclareWarRequest(allianceB.AllianceId, "war-4"));

        var ex = Assert.Throws<InvalidOperationException>(() => service.DeclareWar(leaderA, new DeclareWarRequest(allianceB.AllianceId, "war-5")));
        Assert.That(ex!.Message, Is.EqualTo("duplicate_active_war"));
    }

    [Test]
    public void War_DisabledFlagBlocksDeclaration()
    {
        AllianceService service = CreateService(warEnabled: false);
        PlayerId leaderA = NewPlayer();
        AllianceEntity allianceA = CreateAlliance(service, leaderA, name: "Alliance A", tag: "AAA");
        AllianceEntity allianceB = CreateAlliance(service, NewPlayer(), name: "Alliance B", tag: "BBB");

        var ex = Assert.Throws<InvalidOperationException>(() => service.DeclareWar(leaderA, new DeclareWarRequest(allianceB.AllianceId, "war-6")));
        Assert.That(ex!.Message, Is.EqualTo("war_disabled"));
    }

    [Test]
    public void ListMembers_RequiresMembershipAndReturnsRoster()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader);
        PlayerId member = NewPlayer();
        service.JoinOpen(member, alliance.AllianceId);

        Assert.Throws<InvalidOperationException>(() => service.ListMembers(NewPlayer(), alliance.AllianceId));
        var roster = service.ListMembers(leader, alliance.AllianceId);
        Assert.That(roster.Select(m => m.PlayerId), Is.EquivalentTo(new[] { leader, member }));
    }

    // ---------------- Pending applications for my alliance (M043B-CL) ----------------

    [Test]
    public void ListPendingApplicationsForMyAlliance_LeaderAllowed()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.Application);
        service.SubmitApplication(NewPlayer(), alliance.AllianceId, new SubmitApplicationRequest("please", "app-1"));

        var pending = service.ListPendingApplicationsForMyAlliance(leader);

        Assert.That(pending.Count, Is.EqualTo(1));
        Assert.That(pending[0].AllianceId, Is.EqualTo(alliance.AllianceId));
    }

    [Test]
    public void ListPendingApplicationsForMyAlliance_MemberIsDenied()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader); // Open join mode
        PlayerId member = NewPlayer();
        service.JoinOpen(member, alliance.AllianceId);

        var ex = Assert.Throws<UnauthorizedAccessException>(() => service.ListPendingApplicationsForMyAlliance(member));
        Assert.That(ex!.Message, Is.EqualTo("insufficient_permission"));
    }

    [Test]
    public void ListPendingApplicationsForMyAlliance_NonMemberIsDenied()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        CreateAlliance(service, leader, AllianceJoinMode.Application);

        // No client-supplied AllianceId exists on this call at all - the alliance is always derived
        // from the actor's OWN real membership, so a non-member can never enumerate ANY alliance's
        // applications, not even by guessing an id (there is no id parameter to guess).
        Assert.Throws<InvalidOperationException>(() => service.ListPendingApplicationsForMyAlliance(NewPlayer()));
    }

    [Test]
    public void ListPendingApplicationsForMyAlliance_OfficerCanApprove()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.Application);
        PlayerId officer = NewPlayer();
        AllianceInvitation invitation = service.CreateInvitation(leader, alliance.AllianceId, new CreateInvitationRequest(officer, "invite-officer"));
        service.AcceptInvitation(officer, invitation.InvitationId);
        service.Promote(leader, officer);
        service.SubmitApplication(NewPlayer(), alliance.AllianceId, new SubmitApplicationRequest("please", "app-2"));

        var pending = service.ListPendingApplicationsForMyAlliance(officer);

        Assert.That(pending.Count, Is.EqualTo(1));
    }

    // ---------------- Member DisplayName resolution (M043B-CL) ----------------

    [Test]
    public void ListMembers_ResolvesRealDisplayNamesViaPlayerDirectory()
    {
        PlayerId leader = NewPlayer();
        PlayerId member = NewPlayer();
        var directory = new FakePlayerDirectory(new Dictionary<PlayerId, string> { [leader] = "Queen Jeff", [member] = "Scout Marie" });
        AllianceService service = CreateServiceWithDirectory(directory);
        AllianceEntity alliance = CreateAlliance(service, leader);
        service.JoinOpen(member, alliance.AllianceId);

        var roster = service.ListMembers(leader, alliance.AllianceId);

        Assert.That(roster.Single(m => m.PlayerId == leader).DisplayName, Is.EqualTo("Queen Jeff"));
        Assert.That(roster.Single(m => m.PlayerId == member).DisplayName, Is.EqualTo("Scout Marie"));
    }

    [Test]
    public void ListMembers_FallsBackToEmptyDisplayNameWhenDirectoryHasNoRecord_NeverFabricates()
    {
        PlayerId leader = NewPlayer();
        var directory = new FakePlayerDirectory(new Dictionary<PlayerId, string>());
        AllianceService service = CreateServiceWithDirectory(directory);
        AllianceEntity alliance = CreateAlliance(service, leader);

        var roster = service.ListMembers(leader, alliance.AllianceId);

        Assert.That(roster.Single().DisplayName, Is.EqualTo(string.Empty));
    }

    [Test]
    public void PublicProfile_ResolvesRealLeaderDisplayName()
    {
        PlayerId leader = NewPlayer();
        var directory = new FakePlayerDirectory(new Dictionary<PlayerId, string> { [leader] = "Queen Jeff" });
        AllianceService service = CreateServiceWithDirectory(directory);
        AllianceEntity alliance = CreateAlliance(service, leader);

        AlliancePublicProfile profile = service.GetPublicProfile(alliance.AllianceId);

        Assert.That(profile.Leader.DisplayName, Is.EqualTo("Queen Jeff"));
    }

    private static AllianceService CreateServiceWithDirectory(BeeKingdom.Accounts.IPlayerDirectoryService directory)
    {
        var options = Options.Create(new AllianceOptions { Enabled = true, DiplomacyEnabled = true, WarEnabled = true, MaxMembers = 100 });
        return new AllianceService(
            new InMemoryAllianceRepository(),
            new InMemoryAllianceActivityRepository(),
            new InMemoryAllianceDiplomacyRepository(),
            new InMemoryAllianceWarRepository(),
            options,
            playerDirectory: directory);
    }

    private sealed class FakePlayerDirectory : BeeKingdom.Accounts.IPlayerDirectoryService
    {
        private readonly Dictionary<PlayerId, string> names;
        public FakePlayerDirectory(Dictionary<PlayerId, string> names) { this.names = names; }
        public IReadOnlyList<BeeKingdom.Shared.ValueObjects.PlayerPublicIdentity> Search(string displayNameContains, int offset, int limit) => Array.Empty<BeeKingdom.Shared.ValueObjects.PlayerPublicIdentity>();
        public BeeKingdom.Shared.ValueObjects.PlayerPublicIdentity? GetByPlayerId(PlayerId playerId) =>
            names.TryGetValue(playerId, out string? name) ? new BeeKingdom.Shared.ValueObjects.PlayerPublicIdentity(playerId, name) : null;
        public IReadOnlyDictionary<PlayerId, BeeKingdom.Shared.ValueObjects.PlayerPublicIdentity> GetByPlayerIds(IReadOnlyCollection<PlayerId> playerIds) =>
            playerIds.Where(names.ContainsKey).ToDictionary(id => id, id => new BeeKingdom.Shared.ValueObjects.PlayerPublicIdentity(id, names[id]));
    }

    // ---------------- Web DTO safety ----------------

    [Test]
    public void PublicProfile_DoesNotLeakPrivateFields()
    {
        AllianceService service = CreateService();
        PlayerId leader = NewPlayer();
        AllianceEntity alliance = CreateAlliance(service, leader, AllianceJoinMode.Application);
        service.SubmitApplication(NewPlayer(), alliance.AllianceId, new SubmitApplicationRequest("secret reason", "app-priv"));

        AlliancePublicProfile profile = service.GetPublicProfile(alliance.AllianceId);
        // The public DTO type simply has no field capable of exposing applications/invitations -
        // this assertion documents that contract explicitly rather than relying on "it compiles".
        Assert.That(profile.GetType().GetProperties().Select(p => p.Name),
            Does.Not.Contain("PendingApplications").And.Not.Contain("PendingInvitations"));
    }
}
