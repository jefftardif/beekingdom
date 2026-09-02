using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    // M043-CL: regression coverage for the M041 AllianceClient bugs found and fixed this session -
    // several endpoints deserialized the server's response directly into the "inner" DTO (e.g.
    // RemoteAllianceEntity) when the server actually wraps it in a *Result record (e.g.
    // CreateAllianceResult{Alliance,Deduplicated}, see Server/src/BeeKingdom.Alliance/Models/
    // AllianceContracts.cs). System.Text.Json silently produced an all-default/empty object instead
    // of throwing, so this went undetected until traced against real server JSON. ScriptedTransport
    // here records the actual generic type argument each call requests, so these tests would have
    // caught the original bug (which requested the wrong, unwrapped type).
    //
    // Lives in Assets/BeeKingdom/Tests/ (BeeKingdom.Tests.asmdef, references BeeKingdom.Networking's
    // own asmdef) rather than Assets/BeeKingdom/Playground/ - a custom .asmdef assembly cannot
    // reference the implicit default Assembly-CSharp where AllianceCenterPanelController lives (see
    // Tests/Editor/Interaction/LivingHiveResearchBridgeTests.cs's
    // LivingHiveMenuAssemblyNeverReferencesTheDefaultPlaygroundAssembly for the same constraint
    // documented elsewhere in this project), so only the wire-level AllianceClient is testable here.
    public sealed class AllianceClientTests
    {
        private static readonly Guid PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid AllianceId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid TargetPlayerId = Guid.Parse("99999999-8888-7777-6666-555555555555");
        private const string Token = "alliance-test-token";

        [Test]
        public async Task CreateAllianceAsync_RequestsTheWrapperTypeAndUnwrapsAlliance()
        {
            var alliance = new RemoteAllianceEntity { AllianceId = AllianceId, Name = "Golden Hive", Tag = "GLD", Revision = 1 };
            var transport = new TypeCapturingTransport(new RemoteCreateAllianceResult { Alliance = alliance, Deduplicated = false });
            var client = NewClient(transport);

            RemoteAllianceEntity result = await client.CreateAllianceAsync("Golden Hive", "GLD", "desc", "fr-CA", "", RemoteAllianceJoinMode.Open, "key-1");

            Assert.That(transport.RequestedTypes[0], Is.EqualTo(typeof(RemoteCreateAllianceResult)),
                "must request the server's real CreateAllianceResult wrapper, not the bare entity");
            Assert.That(result.AllianceId, Is.EqualTo(AllianceId));
            Assert.That(result.Name, Is.EqualTo("Golden Hive"));
        }

        [Test]
        public async Task CreateAllianceAsync_ExactCeoPayload_ReachesTransportAndSerializesCorrectly()
        {
            // M043L-CL: reproduces, verbatim, the exact values captured live from the CEO's Create
            // Alliance form (via reflection on the running Play Mode session) at the moment his real
            // "alliance.invalid_request" attempt failed. If this test reaches the transport (i.e.
            // RequireKey and every other client-side pre-flight check pass), that proves the client
            // never blocked the call - the rejection has to be a real server-side decision.
            var alliance = new RemoteAllianceEntity { AllianceId = AllianceId, Name = "BeeKingdom Alpha", Tag = "BKA", Revision = 1 };
            var transport = new TypeCapturingTransport(new RemoteCreateAllianceResult { Alliance = alliance, Deduplicated = false });
            var client = NewClient(transport);
            string clientRequestId = "mobile-alliance-create-" + Guid.NewGuid().ToString("N");

            RemoteAllianceEntity result = await client.CreateAllianceAsync(
                "BeeKingdom Alpha", "BKA", "Alliance officielle de test Alpha BeeKingdom",
                "fr-CA", "", RemoteAllianceJoinMode.InviteOnly, clientRequestId);

            Assert.That(transport.Requests, Has.Count.EqualTo(1), "the client-side pre-flight checks (RequireKey) must not have blocked the call");
            var sentBody = (CreateAllianceWireRequest)transport.Requests[0].Body;
            Assert.That(sentBody.Name, Is.EqualTo("BeeKingdom Alpha"));
            Assert.That(sentBody.Tag, Is.EqualTo("BKA"));
            Assert.That(sentBody.Description, Is.EqualTo("Alliance officielle de test Alpha BeeKingdom"));
            Assert.That(sentBody.Language, Is.EqualTo("fr-CA"));
            Assert.That(sentBody.JoinMode, Is.EqualTo(RemoteAllianceJoinMode.InviteOnly));
            Assert.That(sentBody.ClientRequestId, Is.EqualTo(clientRequestId));

            string json = new SystemTextGameJsonCodec().Serialize(sentBody);
            TestContext.WriteLine("[M043L] Exact serialized request JSON: " + json);
            Assert.That(json, Does.Contain("\"name\":\"BeeKingdom Alpha\""));
            Assert.That(json, Does.Contain("\"tag\":\"BKA\""));
            Assert.That(json, Does.Contain("\"joinMode\":2"));
            Assert.That(result.Name, Is.EqualTo("BeeKingdom Alpha"));
        }

        [Test]
        public async Task JoinOpenAsync_RequestsTheWrapperTypeAndUnwrapsMembership()
        {
            var membership = new RemoteAllianceMembership { AllianceId = AllianceId, PlayerId = PlayerId, Role = RemoteAllianceRole.Member };
            var transport = new TypeCapturingTransport(new RemoteJoinOpenAllianceResult { Alliance = new RemoteAllianceEntity { AllianceId = AllianceId }, Membership = membership });
            var client = NewClient(transport);

            RemoteAllianceMembership result = await client.JoinOpenAsync(AllianceId);

            Assert.That(transport.RequestedTypes[0], Is.EqualTo(typeof(RemoteJoinOpenAllianceResult)));
            Assert.That(result.PlayerId, Is.EqualTo(PlayerId));
            Assert.That(result.Role, Is.EqualTo(RemoteAllianceRole.Member));
        }

        [Test]
        public async Task AcceptApplicationAsync_RequestsTheWrapperTypeAndUnwrapsApplication()
        {
            var application = new RemoteAllianceApplication { ApplicationId = Guid.NewGuid(), AllianceId = AllianceId, PlayerId = PlayerId, Status = RemoteAllianceApplicationStatus.Accepted };
            var transport = new TypeCapturingTransport(new RemoteApplicationDecisionResult { Application = application, Membership = new RemoteAllianceMembership() });
            var client = NewClient(transport);

            RemoteAllianceApplication result = await client.AcceptApplicationAsync(application.ApplicationId);

            Assert.That(transport.RequestedTypes[0], Is.EqualTo(typeof(RemoteApplicationDecisionResult)));
            Assert.That(result.Status, Is.EqualTo(RemoteAllianceApplicationStatus.Accepted));
        }

        [Test]
        public async Task AcceptInvitationAsync_RequestsTheWrapperTypeAndUnwrapsInvitation()
        {
            var invitation = new RemoteAllianceInvitation { InvitationId = Guid.NewGuid(), AllianceId = AllianceId, InvitedPlayerId = PlayerId, Status = RemoteAllianceInvitationStatus.Accepted };
            var transport = new TypeCapturingTransport(new RemoteInvitationDecisionResult { Invitation = invitation, Membership = new RemoteAllianceMembership() });
            var client = NewClient(transport);

            RemoteAllianceInvitation result = await client.AcceptInvitationAsync(invitation.InvitationId);

            Assert.That(transport.RequestedTypes[0], Is.EqualTo(typeof(RemoteInvitationDecisionResult)));
            Assert.That(result.Status, Is.EqualTo(RemoteAllianceInvitationStatus.Accepted));
        }

        [Test]
        public async Task TransferLeadershipAsync_RequestsTheWrapperTypeAndUnwrapsAlliance()
        {
            var alliance = new RemoteAllianceEntity { AllianceId = AllianceId, LeaderPlayerId = TargetPlayerId, Revision = 2 };
            var transport = new TypeCapturingTransport(new RemoteLeadershipTransferResult
            {
                Alliance = alliance,
                PreviousLeader = new RemoteAllianceMembership { PlayerId = PlayerId, Role = RemoteAllianceRole.Officer },
                NewLeader = new RemoteAllianceMembership { PlayerId = TargetPlayerId, Role = RemoteAllianceRole.Leader }
            });
            var client = NewClient(transport);

            RemoteAllianceEntity result = await client.TransferLeadershipAsync(TargetPlayerId);

            Assert.That(transport.RequestedTypes[0], Is.EqualTo(typeof(RemoteLeadershipTransferResult)));
            Assert.That(result.LeaderPlayerId, Is.EqualTo(TargetPlayerId));
        }

        [Test]
        public async Task GetMyAllianceAsync_ReturnsHasAllianceFalseWithoutThrowing()
        {
            var transport = new TypeCapturingTransport(new RemoteMyAllianceOverview { HasAlliance = false });
            var client = NewClient(transport);

            RemoteMyAllianceOverview result = await client.GetMyAllianceAsync();

            Assert.That(result.HasAlliance, Is.False);
            Assert.That(result.Alliance, Is.Null);
        }

        // M043E-CL: a CEO Play Mode certification failure showed the Alliance Center rendering a
        // Ready/IN_ALLIANCE shell with empty/default data for an account with NO active membership.
        // TypeCapturingTransport (used everywhere else in this file) never actually round-trips
        // through JSON - it hands back a pre-built C# object directly, so it cannot catch a real
        // wire-shape bug (exactly the M041 class of bug this file was created to guard against).
        // This test uses the REAL SystemTextGameJsonCodec against the EXACT JSON string the server
        // is proven (Server/tests/BeeKingdom.Tests/MyAllianceOverviewWireContractTests.cs) to
        // produce for a no-membership account: {"hasAlliance":false} - no "alliance"/"membership"
        // keys at all (BeeJson's WhenWritingNull DefaultIgnoreCondition omits them).
        [Test]
        public void RealJsonCodec_NoAllianceServerResponse_DeserializesToHasAllianceFalseWithNullNestedObjects()
        {
            var codec = new SystemTextGameJsonCodec();

            RemoteMyAllianceOverview result = codec.Deserialize<RemoteMyAllianceOverview>("{\"hasAlliance\":false}");

            Assert.That(result.HasAlliance, Is.False);
            Assert.That(result.Alliance, Is.Null, "a real no-membership response must never produce a non-null (even if empty/default) Alliance object");
            Assert.That(result.Membership, Is.Null, "a real no-membership response must never produce a non-null (even if empty/default) Membership object");
        }

        [Test]
        public void RealJsonCodec_HasAllianceResponse_DeserializesRealNestedAllianceAndMembership()
        {
            var codec = new SystemTextGameJsonCodec();
            string json = "{\"hasAlliance\":true,\"alliance\":{\"allianceId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\",\"name\":\"Golden Hive\",\"tag\":\"GLD\",\"joinMode\":0,\"status\":0,\"maxMembers\":100,\"memberCount\":1,\"revision\":1},\"membership\":{\"allianceId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\",\"playerId\":\"11111111-1111-1111-1111-111111111111\",\"role\":2,\"joinedAtUtc\":\"2026-01-01T00:00:00Z\"}}";

            RemoteMyAllianceOverview result = codec.Deserialize<RemoteMyAllianceOverview>(json);

            Assert.That(result.HasAlliance, Is.True);
            Assert.That(result.Alliance, Is.Not.Null);
            Assert.That(result.Alliance.AllianceId, Is.EqualTo(AllianceId));
            Assert.That(result.Alliance.Name, Is.EqualTo("Golden Hive"));
            Assert.That(result.Membership, Is.Not.Null);
            Assert.That(result.Membership.Role, Is.EqualTo(RemoteAllianceRole.Leader));
        }

        [Test]
        public async Task LeaveAsync_DoesNotThrowOnARealNonEmptyConfirmationBody()
        {
            // M043-CL: the server previously returned Results.Ok() with an EMPTY body for Leave/Kick,
            // which the codec always rejects as malformed even for T=object - fixed server-side to
            // return {"success":true}. This asserts the client-side contract (SendAsync<object>)
            // still works against a real (non-empty) body.
            var transport = new TypeCapturingTransport(new object());
            var client = NewClient(transport);

            await client.LeaveAsync();

            Assert.That(transport.RequestedTypes[0], Is.EqualTo(typeof(object)));
        }

        [Test]
        public async Task ListPendingApplicationsAsync_RequestsCorrectPathAndParsesDisplayName()
        {
            var applications = new List<RemoteAllianceApplicationView>
            {
                new RemoteAllianceApplicationView { ApplicationId = Guid.NewGuid(), AllianceId = AllianceId, PlayerId = PlayerId, DisplayName = "Scout Marie", Status = RemoteAllianceApplicationStatus.Pending }
            };
            var transport = new TypeCapturingTransport(applications);
            var client = NewClient(transport);

            List<RemoteAllianceApplicationView> result = await client.ListPendingApplicationsAsync();

            Assert.That(transport.RequestedTypes[0], Is.EqualTo(typeof(List<RemoteAllianceApplicationView>)));
            Assert.That(transport.Requests[0].Method, Is.EqualTo("GET"));
            Assert.That(transport.Requests[0].Path, Is.EqualTo("/alliance/v1/applications/pending"));
            Assert.That(result[0].DisplayName, Is.EqualTo("Scout Marie"));
            Assert.That(result[0].PlayerId, Is.EqualTo(PlayerId));
        }

        [Test]
        public async Task ListMembersAsync_ParsesRealDisplayName()
        {
            var members = new List<RemoteAllianceMemberSummary>
            {
                new RemoteAllianceMemberSummary { PlayerId = PlayerId, DisplayName = "Queen Jeff", Role = RemoteAllianceRole.Leader, JoinedAtUtc = DateTimeOffset.UtcNow }
            };
            var transport = new TypeCapturingTransport(members);
            var client = NewClient(transport);

            List<RemoteAllianceMemberSummary> result = await client.ListMembersAsync(AllianceId);

            Assert.That(transport.Requests[0].Path, Is.EqualTo("/alliance/v1/alliances/" + AllianceId.ToString("D") + "/members"));
            Assert.That(result[0].DisplayName, Is.EqualTo("Queen Jeff"));
            Assert.That(result[0].Role, Is.EqualTo(RemoteAllianceRole.Leader));
        }

        [Test]
        public async Task ListMembersAsync_MissingDisplayNameFallsBackToEmptyString_NeverFabricated()
        {
            var members = new List<RemoteAllianceMemberSummary>
            {
                new RemoteAllianceMemberSummary { PlayerId = PlayerId, DisplayName = null, Role = RemoteAllianceRole.Member, JoinedAtUtc = DateTimeOffset.UtcNow }
            };
            var transport = new TypeCapturingTransport(members);
            var client = NewClient(transport);

            List<RemoteAllianceMemberSummary> result = await client.ListMembersAsync(AllianceId);

            Assert.That(result[0].DisplayName, Is.Null.Or.Empty);
        }

        // M043G-CL: proves the precondition AllianceCenterPanelController.StableError's fix relies
        // on - a real, valid server rejection (e.g. 503 "alliance.unavailable" when Alliance.Enabled
        // is off, or 404 "alliance.not_found" for a corrupt membership pointing at a deleted
        // Alliance) preserves the server's real SafeCode string on the resulting
        // HivePerimeterClientException, rather than losing it. StableError itself lives in
        // AllianceCenterPanelController (default Assembly-CSharp, unreachable from this
        // BeeKingdom.Tests.asmdef-scoped project - see this file's own top-of-file comment), so this
        // is the closest layer this project can actually unit test.
        [Test]
        public void SearchAsync_ServerRejectionWithAllianceCode_PreservesTheRealSafeCode()
        {
            var transport = new TypeCapturingTransport(new AuthenticatedGameRestException(AuthenticatedGameRestError.RemoteRejected, "alliance.unavailable", 503));
            var client = NewClient(transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () => await client.SearchAsync(null, null, null, 0, 20));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
            Assert.That(error.Message, Is.EqualTo("alliance.unavailable"),
                "the real server SafeCode must survive to the controller layer - it is what StableError strips the 'alliance.' prefix from");
        }

        [Test]
        public void GetMyAllianceAsync_CorruptMembershipRejection_PreservesTheRealSafeCode()
        {
            var transport = new TypeCapturingTransport(new AuthenticatedGameRestException(AuthenticatedGameRestError.RemoteRejected, "alliance.not_found", 404));
            var client = NewClient(transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () => await client.GetMyAllianceAsync());

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
            Assert.That(error.Message, Is.EqualTo("alliance.not_found"));
        }

        private static AllianceClient NewClient(TypeCapturingTransport transport)
        {
            var gate = new MobileAccountSessionGate();
            gate.ConfigureTransport(true);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(true, true, true, true, true));
            return new AllianceClient(gate, new FakeSessionSource(new GameAccountSession(PlayerId, Token)), transport);
        }

        private sealed class FakeSessionSource : IGameAccountSessionSource
        {
            private readonly GameAccountSession session;
            public FakeSessionSource(GameAccountSession session) { this.session = session; }
            public bool TryGetSession(out GameAccountSession value) { value = session; return value != null; }
        }

        // Unlike the plain ScriptedTransport used elsewhere in this folder (which just casts a
        // pre-built object to T without recording T), this records the actual requested generic
        // type argument per call - the only way to assert "did the client ask for the CORRECT wire
        // shape" rather than merely "did it accept whatever shape I handed it".
        private sealed class TypeCapturingTransport : IAuthenticatedGameRestTransport
        {
            private readonly object response;
            private readonly Exception failure;
            public TypeCapturingTransport(object response) { this.response = response; }
            public TypeCapturingTransport(Exception failure) { this.failure = failure; }
            public List<Type> RequestedTypes { get; } = new List<Type>();
            public List<AuthenticatedGameRestRequest> Requests { get; } = new List<AuthenticatedGameRestRequest>();
            public Task<T> SendAsync<T>(AuthenticatedGameRestRequest request, string bearerAccessToken, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                RequestedTypes.Add(typeof(T));
                if (failure != null) throw failure;
                return Task.FromResult((T)response);
            }
        }
    }
}
