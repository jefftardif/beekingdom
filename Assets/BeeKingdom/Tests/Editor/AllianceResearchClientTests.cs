using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    // M051-CL: wire-level coverage for the Alliance Research client methods added on top of the
    // existing AllianceClient. Same architecture constraint as AllianceHelpClientTests.cs (see its
    // top-of-file comment): AllianceCenterPanelController's new RefreshResearch/DonateToResearch
    // state machine lives in the default Assembly-CSharp assembly, unreachable from this
    // BeeKingdom.Tests.asmdef-scoped project - so this proves the client sends exactly the request
    // M051's real server contract expects (single call, correct path, correct body), which is the
    // layer this project can actually unit test.
    public sealed class AllianceResearchClientTests
    {
        private static readonly Guid PlayerId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid HiveId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid AllianceId = Guid.Parse("99999999-8888-7777-6666-555555555555");
        private const string Token = "alliance-research-test-token";
        private const string TechnologyId = "prosperity_shared_reserves_i";

        [Test]
        public async Task GetAllianceResearchAsync_RequestsTheRealSharedSnapshot()
        {
            var snapshot = new RemoteAllianceResearchSnapshot
            {
                AllianceId = AllianceId,
                Technologies = new List<RemoteAllianceTechnology>
                {
                    new RemoteAllianceTechnology { TechnologyId = TechnologyId, RequiredProgress = 60, CurrentProgress = 10, Available = true }
                },
                MyContributionPoints = 10,
                MyDonationCount = 1
            };
            var transport = new TypeCapturingTransport(snapshot);
            var client = NewClient(transport);

            RemoteAllianceResearchSnapshot result = await client.GetAllianceResearchAsync();

            Assert.That(transport.Requests, Has.Count.EqualTo(1), "must call the real M051 read path exactly once, never a local/fabricated one");
            Assert.That(transport.Requests[0].Method, Is.EqualTo("GET"));
            Assert.That(transport.Requests[0].Path, Is.EqualTo("/alliance/v1/research"));
            Assert.That(result.Technologies[0].TechnologyId, Is.EqualTo(TechnologyId));
            Assert.That(result.MyContributionPoints, Is.EqualTo(10), "contribution total must be exactly what the server returned, never re-derived client-side");
        }

        // M051C-CL: Stage 1 certification failed because the UI had no real number to format an
        // effect from - proves the wire DTO now round-trips the real catalog bonus magnitude the
        // server exposes (ProductionBp/CapacityBp/CombatPowerBp), so "+1 %" on screen always comes
        // from server truth, never a client-hardcoded value.
        [Test]
        public async Task GetAllianceResearchAsync_RoundTripsRealBonusMagnitudes()
        {
            var snapshot = new RemoteAllianceResearchSnapshot
            {
                AllianceId = AllianceId,
                Technologies = new List<RemoteAllianceTechnology>
                {
                    new RemoteAllianceTechnology { TechnologyId = TechnologyId, RequiredProgress = 60, ProductionBp = 100, CapacityBp = 0, CombatPowerBp = 0 }
                }
            };
            var transport = new TypeCapturingTransport(snapshot);
            var client = NewClient(transport);

            RemoteAllianceResearchSnapshot result = await client.GetAllianceResearchAsync();

            Assert.That(result.Technologies[0].ProductionBp, Is.EqualTo(100), "the real catalog magnitude must reach the client unchanged");
            Assert.That(result.Technologies[0].CapacityBp, Is.EqualTo(0));
            Assert.That(result.Technologies[0].CombatPowerBp, Is.EqualTo(0));
        }

        [Test]
        public async Task DonateToAllianceResearchAsync_PostsToTheSpecificTechnologyWithHiveIdAndClientRequestId()
        {
            var response = new RemoteAllianceResearchDonateResult
            {
                Succeeded = true,
                Code = "donation_applied",
                Snapshot = new RemoteAllianceResearchSnapshot { AllianceId = AllianceId, Technologies = new List<RemoteAllianceTechnology>(), MyContributionPoints = 10, MyDonationCount = 1 }
            };
            var transport = new TypeCapturingTransport(response);
            var client = NewClient(transport);

            RemoteAllianceResearchDonateResult result = await client.DonateToAllianceResearchAsync(TechnologyId, HiveId, "donate-key-1");

            Assert.That(transport.Requests, Has.Count.EqualTo(1));
            Assert.That(transport.Requests[0].Path, Is.EqualTo("/alliance/v1/research/" + TechnologyId + "/donate"));
            Assert.That(transport.Requests[0].Method, Is.EqualTo("POST"));
            var body = (AllianceResearchDonateWireRequest)transport.Requests[0].Body;
            Assert.That(body.HiveId, Is.EqualTo(HiveId));
            Assert.That(body.ClientRequestId, Is.EqualTo("donate-key-1"));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Snapshot.MyDonationCount, Is.EqualTo(1));
        }

        [Test]
        public async Task DonateToAllianceResearchAsync_EscapesTheTechnologyIdInThePath()
        {
            var response = new RemoteAllianceResearchDonateResult { Succeeded = false, Code = "technology_not_found" };
            var transport = new TypeCapturingTransport(response);
            var client = NewClient(transport);

            await client.DonateToAllianceResearchAsync("weird id/segment", HiveId, "donate-key-2");

            Assert.That(transport.Requests[0].Path, Is.EqualTo("/alliance/v1/research/weird%20id%2Fsegment/donate"));
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
            public Task<T> SendAsync<T>(AuthenticatedGameRestRequest request, string bearerAccessToken, System.Threading.CancellationToken cancellationToken)
            {
                Requests.Add(request);
                RequestedTypes.Add(typeof(T));
                return Task.FromResult((T)response);
            }
        }
    }
}
