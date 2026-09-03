using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    // M043C-CL: wire-contract coverage for PlayerDirectoryClient (M043B-CL) - the generic,
    // reusable player-search client. Written this session because the Unity Editor MCP connection
    // was unresponsive when M043B was authored, so this coverage was deferred rather than shipped
    // untested. Mirrors AllianceClientTests.cs's TypeCapturingTransport pattern (records the actual
    // generic type argument/path requested, not just "did it accept whatever I handed it").
    public sealed class PlayerDirectoryClientTests
    {
        private static readonly Guid PlayerIdA = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid PlayerIdB = Guid.Parse("22222222-3333-4444-5555-666666666666");
        private const string Token = "directory-test-token";

        [Test]
        public async Task SearchAsync_RequestsTheCorrectAuthenticatedPathAndDto()
        {
            var results = new List<RemotePlayerPublicIdentity>
            {
                new RemotePlayerPublicIdentity { PlayerId = PlayerIdA, DisplayName = "Queen Jeff" },
                new RemotePlayerPublicIdentity { PlayerId = PlayerIdB, DisplayName = "Scout Marie" }
            };
            var transport = new RecordingTransport(results);
            var client = NewClient(transport);

            List<RemotePlayerPublicIdentity> result = await client.SearchAsync("queen", 0, 20);

            Assert.That(transport.RequestedTypes[0], Is.EqualTo(typeof(List<RemotePlayerPublicIdentity>)),
                "must request the real wire DTO list, not something else");
            Assert.That(transport.Requests[0].Method, Is.EqualTo("GET"));
            Assert.That(transport.Requests[0].Path, Is.EqualTo("/game/v1/players/search?q=queen&offset=0&limit=20"));
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task SearchAsync_ParsesRealDisplayNameAndPlayerId()
        {
            var transport = new RecordingTransport(new List<RemotePlayerPublicIdentity>
            {
                new RemotePlayerPublicIdentity { PlayerId = PlayerIdA, DisplayName = "Queen Jeff" }
            });
            var client = NewClient(transport);

            List<RemotePlayerPublicIdentity> result = await client.SearchAsync("queen jeff", 0, 20);

            Assert.That(result[0].PlayerId, Is.EqualTo(PlayerIdA));
            Assert.That(result[0].DisplayName, Is.EqualTo("Queen Jeff"));
        }

        [Test]
        public void SearchAsync_QueryTooShort_RejectedBeforeAnyTransportCall()
        {
            var transport = new RecordingTransport(new List<RemotePlayerPublicIdentity>());
            var client = NewClient(transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () => await client.SearchAsync("a", 0, 20));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidRequest));
            Assert.That(transport.Requests, Is.Empty, "a too-short query must never reach the transport/server at all");
        }

        [Test]
        public void SearchAsync_BlankQuery_RejectedBeforeAnyTransportCall()
        {
            var transport = new RecordingTransport(new List<RemotePlayerPublicIdentity>());
            var client = NewClient(transport);

            Assert.ThrowsAsync<HivePerimeterClientException>(async () => await client.SearchAsync("   ", 0, 20));
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public void SearchAsync_MalformedServerResponse_SurfacesAsInvalidResponse()
        {
            var transport = new RecordingTransport(new AuthenticatedGameRestException(AuthenticatedGameRestError.InvalidResponse, "game.response_invalid"));
            var client = NewClient(transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () => await client.SearchAsync("queen", 0, 20));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.InvalidResponse));
        }

        [Test]
        public void SearchAsync_ClosedOfficialGate_StopsBeforeAnyTransportCall()
        {
            var gate = new MobileAccountSessionGate(); // never configured - session transport not ready
            var source = new FakeSessionSource(new GameAccountSession(PlayerIdA, Token));
            var transport = new RecordingTransport(new List<RemotePlayerPublicIdentity>());
            var client = new PlayerDirectoryClient(gate, source, transport);

            HivePerimeterClientException error = Assert.ThrowsAsync<HivePerimeterClientException>(async () => await client.SearchAsync("queen", 0, 20));

            Assert.That(error.Error, Is.EqualTo(HivePerimeterClientError.NotConfigured));
            Assert.That(transport.Requests, Is.Empty);
        }

        [Test]
        public async Task SearchAsync_UnauthorizedOnce_RefreshesSessionAndRetriesWithNewToken()
        {
            var results = new List<RemotePlayerPublicIdentity> { new RemotePlayerPublicIdentity { PlayerId = PlayerIdA, DisplayName = "Queen Jeff" } };
            var source = new RefreshableSessionSource(PlayerIdA, Token, "refreshed-token");
            var transport = new UnauthorizedOnceTransport(results);
            var gate = new MobileAccountSessionGate();
            gate.ConfigureTransport(true);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(true, true, true, true, true));
            var client = new PlayerDirectoryClient(gate, source, transport);

            List<RemotePlayerPublicIdentity> result = await client.SearchAsync("queen", 0, 20);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(source.RefreshCalls, Is.EqualTo(1));
            Assert.That(transport.Tokens, Is.EqualTo(new[] { Token, "refreshed-token" }));
        }

        private static PlayerDirectoryClient NewClient(RecordingTransport transport)
        {
            var gate = new MobileAccountSessionGate();
            gate.ConfigureTransport(true);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(true, true, true, true, true));
            return new PlayerDirectoryClient(gate, new FakeSessionSource(new GameAccountSession(PlayerIdA, Token)), transport);
        }

        private sealed class FakeSessionSource : IGameAccountSessionSource
        {
            private readonly GameAccountSession session;
            public FakeSessionSource(GameAccountSession session) { this.session = session; }
            public bool TryGetSession(out GameAccountSession value) { value = session; return value != null; }
        }

        private sealed class RefreshableSessionSource : IRefreshableGameAccountSessionSource
        {
            private readonly Guid playerId;
            private readonly string replacementToken;
            private GameAccountSession session;
            public RefreshableSessionSource(Guid playerId, string token, string replacementToken)
            {
                this.playerId = playerId;
                this.replacementToken = replacementToken;
                session = new GameAccountSession(playerId, token);
            }
            public int RefreshCalls { get; private set; }
            public bool TryGetSession(out GameAccountSession value) { value = session; return value != null; }
            public bool TryGetKnownPlayerId(out Guid value) { value = playerId; return value != Guid.Empty; }
            public Task<GameAccountSession> GetFreshSessionAsync(CancellationToken cancellationToken)
            { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(session); }
            public Task<GameAccountSession> RefreshAfterUnauthorizedAsync(string rejectedAccessToken, CancellationToken cancellationToken)
            { cancellationToken.ThrowIfCancellationRequested(); RefreshCalls++; session = new GameAccountSession(playerId, replacementToken); return Task.FromResult(session); }
            public Task InvalidateUnauthorizedSessionAsync(string rejectedAccessToken, CancellationToken cancellationToken)
            { cancellationToken.ThrowIfCancellationRequested(); session = null; return Task.CompletedTask; }
        }

        // Records the actual generic type argument and the exact request (method/path) per call.
        private sealed class RecordingTransport : IAuthenticatedGameRestTransport
        {
            private readonly object response;
            private readonly Exception failure;
            public RecordingTransport(object response) { this.response = response; }
            public RecordingTransport(Exception failure) { this.failure = failure; }
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

        private sealed class UnauthorizedOnceTransport : IAuthenticatedGameRestTransport
        {
            private readonly object response;
            private bool first = true;
            public UnauthorizedOnceTransport(object response) { this.response = response; }
            public List<string> Tokens { get; } = new List<string>();
            public Task<T> SendAsync<T>(AuthenticatedGameRestRequest request, string bearerAccessToken, CancellationToken cancellationToken)
            {
                Tokens.Add(bearerAccessToken);
                if (first) { first = false; throw new AuthenticatedGameRestException(AuthenticatedGameRestError.Unauthorized, "game.session_required"); }
                return Task.FromResult((T)response);
            }
        }
    }
}
