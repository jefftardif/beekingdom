using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    // M045B-CL: wire-level coverage for the Alliance Help client methods added on top of the
    // existing AllianceClient. Same architecture constraint as AllianceClientTests.cs (see its
    // top-of-file comment): AllianceCenterPanelController's new RequestHelp/RefreshHelpOperationState
    // state machine lives in the default Assembly-CSharp assembly, unreachable from this
    // BeeKingdom.Tests.asmdef-scoped project - so this proves the client sends exactly the request
    // M045's real server contract expects (single call, correct path, correct body), which is the
    // layer this project can actually unit test.
    public sealed class AllianceHelpClientTests
    {
        private static readonly Guid PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid HelpRequestId = Guid.Parse("99999999-8888-7777-6666-555555555555");
        private const string Token = "alliance-help-test-token";

        [Test]
        public async Task CreateHelpRequestAsync_SendsExactlyOneRequestWithTheRealOperationIdentifiers()
        {
            var response = new RemoteAllianceHelpCommandResult { Succeeded = true, Code = "request_created", Request = new RemoteAllianceHelpRequest { HelpRequestId = HelpRequestId, HelpCount = 0, MaxHelpCount = 10 } };
            var transport = new TypeCapturingTransport(response);
            var client = NewClient(transport);

            RemoteAllianceHelpCommandResult result = await client.CreateHelpRequestAsync(HiveId, RemoteAllianceHelpCategories.Construction, "honey_storage", "help-request-key-1");

            Assert.That(transport.Requests, Has.Count.EqualTo(1), "must call the real M045 create path exactly once, never a second/local path");
            Assert.That(transport.Requests[0].Path, Is.EqualTo("/alliance/v1/help/requests"));
            Assert.That(transport.Requests[0].Method, Is.EqualTo("POST"));
            var body = (CreateAllianceHelpRequestWireRequest)transport.Requests[0].Body;
            Assert.That(body.HiveId, Is.EqualTo(HiveId));
            Assert.That(body.OperationCategory, Is.EqualTo(RemoteAllianceHelpCategories.Construction));
            Assert.That(body.OperationTargetId, Is.EqualTo("honey_storage"));
            Assert.That(body.ClientRequestId, Is.EqualTo("help-request-key-1"));
            Assert.That(result.Request.HelpRequestId, Is.EqualTo(HelpRequestId));
        }

        [Test]
        public async Task ContributeHelpAsync_PostsToTheSpecificRequestIdWithClientRequestIdBody()
        {
            var response = new RemoteContributeAllianceHelpResult { Succeeded = true, Code = "help_applied", Request = new RemoteAllianceHelpRequest { HelpRequestId = HelpRequestId, HelpCount = 1, MaxHelpCount = 10 }, DurationReductionSeconds = 60 };
            var transport = new TypeCapturingTransport(response);
            var client = NewClient(transport);

            RemoteContributeAllianceHelpResult result = await client.ContributeHelpAsync(HelpRequestId, "help-contribute-key-1");

            Assert.That(transport.Requests, Has.Count.EqualTo(1));
            Assert.That(transport.Requests[0].Path, Is.EqualTo("/alliance/v1/help/requests/" + HelpRequestId.ToString("D") + "/contribute"));
            Assert.That(transport.Requests[0].Method, Is.EqualTo("POST"));
            var body = (AllianceHelpContributeWireRequest)transport.Requests[0].Body;
            Assert.That(body.ClientRequestId, Is.EqualTo("help-contribute-key-1"));
            Assert.That(result.Request.HelpCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ContributeHelpAllAsync_PostsToTheContributeAllPathOnce()
        {
            var response = new RemoteContributeAllianceHelpAllResult { Results = new List<RemoteContributeAllianceHelpResult>() };
            var transport = new TypeCapturingTransport(response);
            var client = NewClient(transport);

            await client.ContributeHelpAllAsync("help-contribute-all-key-1");

            Assert.That(transport.Requests, Has.Count.EqualTo(1));
            Assert.That(transport.Requests[0].Path, Is.EqualTo("/alliance/v1/help/contribute-all"));
            var body = (AllianceHelpContributeWireRequest)transport.Requests[0].Body;
            Assert.That(body.ClientRequestId, Is.EqualTo("help-contribute-all-key-1"));
        }

        [Test]
        public async Task GetMyOpenHelpRequestAsync_BuildsTheCategoryAndTargetIdQueryString()
        {
            var transport = new TypeCapturingTransport((RemoteAllianceHelpRequest)null);
            var client = NewClient(transport);

            RemoteAllianceHelpRequest result = await client.GetMyOpenHelpRequestAsync(RemoteAllianceHelpCategories.Research, "foraging routes i", CancellationToken.None);

            Assert.That(transport.Requests[0].Method, Is.EqualTo("GET"));
            Assert.That(transport.Requests[0].Path, Is.EqualTo("/alliance/v1/help/requests/mine?category=research&targetId=foraging%20routes%20i"));
            Assert.That(result, Is.Null, "no open request must round-trip as null, not throw or fabricate one");
        }

        [Test]
        public async Task ListHelpRequestsAsync_RequestsTheRealViewListType()
        {
            var views = new List<RemoteAllianceHelpRequestView> { new RemoteAllianceHelpRequestView { HelpRequestId = HelpRequestId, RequestingDisplayName = "Jeff", RemainingSeconds = 120, HelpCount = 0, MaxHelpCount = 10 } };
            var transport = new TypeCapturingTransport(views);
            var client = NewClient(transport);

            List<RemoteAllianceHelpRequestView> result = await client.ListHelpRequestsAsync();

            Assert.That(transport.Requests[0].Path, Is.EqualTo("/alliance/v1/help/requests"));
            Assert.That(transport.RequestedTypes[0], Is.EqualTo(typeof(List<RemoteAllianceHelpRequestView>)));
            Assert.That(result[0].RequestingDisplayName, Is.EqualTo("Jeff"), "must be a real resolved DisplayName, never left for the client to fabricate");
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

        private sealed class TypeCapturingTransport : IAuthenticatedGameRestTransport
        {
            private readonly object response;
            public TypeCapturingTransport(object response) { this.response = response; }
            public List<Type> RequestedTypes { get; } = new List<Type>();
            public List<AuthenticatedGameRestRequest> Requests { get; } = new List<AuthenticatedGameRestRequest>();
            public Task<T> SendAsync<T>(AuthenticatedGameRestRequest request, string bearerAccessToken, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                RequestedTypes.Add(typeof(T));
                return Task.FromResult((T)response);
            }
        }
    }
}
