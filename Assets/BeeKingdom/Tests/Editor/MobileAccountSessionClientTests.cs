using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class MobileAccountSessionClientTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 7, 22, 18, 0, 0, TimeSpan.Zero);

        [Test]
        public async Task MissingProtectedStoreKeepsTransportAndCredentialCollectionClosed()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var client = new MobileAccountSessionClient(
                gate,
                transport,
                new UnavailableProtectedRefreshTokenStore(),
                new FakeClock(Now));

            AccountSessionReadinessSnapshot snapshot = await client.InitializeAsync();

            Assert.That(snapshot.State, Is.EqualTo(AccountSessionReadinessState.NotConfigured));
            Assert.That(gate.TransportConfigured, Is.False);
            Assert.That(gate.CanCollectCredentials, Is.False);
            Assert.That(transport.ReadinessCalls, Is.Zero);
            Assert.That(client.LastSafeErrorCode, Is.EqualTo("auth.protected_storage_unavailable"));
        }

        [Test]
        public async Task PreparationOnlyServerNeverReceivesCredentials()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now) { Live = false };
            var client = Client(gate, transport, new MemoryProtectedStore(), Now);
            await client.InitializeAsync();

            MobileAccountSessionException error = Assert.ThrowsAsync<MobileAccountSessionException>(async () =>
                await client.LoginAsync(Request("queen@bee.test", "not-retained")));

            Assert.That(gate.Snapshot.State, Is.EqualTo(AccountSessionReadinessState.PreparationOnly));
            Assert.That(error.Error, Is.EqualTo(MobileAccountSessionError.NotConfigured));
            Assert.That(transport.LoginCalls, Is.Zero);
        }

        [Test]
        public async Task LoginPublishesAccessTokenOnlyAfterProtectedRefreshRoundTrip()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var client = Client(gate, transport, store, Now);
            await client.InitializeAsync();

            GameAccountSession session = await client.LoginAsync(Request("queen@bee.test", "one-frame-password"));

            Assert.That(session.PlayerId, Is.EqualTo(transport.PlayerA));
            Assert.That(session.AccessToken, Is.EqualTo("access-1"));
            Assert.That(store.SaveCalls, Is.EqualTo(1));
            Assert.That(store.LoadCalls, Is.EqualTo(2));
            Assert.That(store.Record.RefreshToken, Is.EqualTo("refresh-1"));
            Assert.That(client.State, Is.EqualTo(MobileAccountSessionState.Authenticated));
            Assert.That(client.ProofRows(), Does.Contain("access_token_storage:memory_only"));
            Assert.That(client.ProofRows(), Does.Contain("password_retained_by_client:false"));
            Assert.That(string.Join("|", client.ProofRows()), Does.Not.Contain("one-frame-password"));
        }

        [Test]
        public async Task NetworkLossUsesOneOfflineStateAndRestoreReturnsToAuthenticated()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var client = Client(gate, transport, store, Now);
            await client.InitializeAsync();
            await client.LoginAsync(Request("queen@bee.test", "secret"));

            client.MarkNetworkUnavailable();

            GameAccountSession session;
            Assert.That(client.State, Is.EqualTo(MobileAccountSessionState.Offline));
            Assert.That(client.TryGetSession(out session), Is.False);

            GameAccountSession restored = await client.RestoreOrRefreshAsync();

            Assert.That(restored, Is.Not.Null);
            Assert.That(client.State, Is.EqualTo(MobileAccountSessionState.Authenticated));
            Assert.That(client.TryGetSession(out session), Is.True);
        }

        [Test]
        public async Task GoogleLoginPublishesTheSameAuthenticatedStateAsCredentialLogin()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var client = Client(gate, transport, new MemoryProtectedStore(), Now);
            await client.InitializeAsync();

            GameAccountSession session = await client.LoginWithGoogleAsync(
                new GoogleLoginRequest(
                    "authorization-code",
                    "code-verifier",
                    "http://127.0.0.1/callback",
                    "google-client-id",
                    "1",
                    "device",
                    "CA"));

            Assert.That(session, Is.Not.Null);
            Assert.That(client.State, Is.EqualTo(MobileAccountSessionState.Authenticated));
            Assert.That(client.LastSafeErrorCode, Is.Empty);
        }

        [Test]
        public async Task RestoreRotatesOneTimeRefreshAndPreservesBoundIdentity()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var first = Client(gate, transport, store, Now);
            await first.InitializeAsync();
            await first.LoginAsync(Request("queen@bee.test", "secret"));

            var restoredGate = new MobileAccountSessionGate();
            var restored = Client(restoredGate, transport, store, Now.AddMinutes(1));
            await restored.InitializeAsync();
            GameAccountSession session = await restored.RestoreOrRefreshAsync();

            Assert.That(transport.RefreshTokens, Is.EqualTo(new[] { "refresh-1" }));
            Assert.That(store.Record.RefreshToken, Is.EqualTo("refresh-2"));
            Assert.That(session.PlayerId, Is.EqualTo(transport.PlayerA));
            Assert.That(session.AccessToken, Is.EqualTo("access-2"));
        }

        [Test]
        public async Task RefreshIdentityMismatchIsRejectedAndProtectedRecordDeleted()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var client = Client(gate, transport, store, Now);
            await client.InitializeAsync();
            await client.LoginAsync(Request("queen@bee.test", "secret"));
            transport.ReturnForeignRefreshIdentity = true;

            MobileAccountSessionException error = Assert.ThrowsAsync<MobileAccountSessionException>(async () =>
                await client.RestoreOrRefreshAsync());

            GameAccountSession session;
            Assert.That(error.SafeCode, Is.EqualTo("auth.refresh_identity_mismatch"));
            Assert.That(store.Record, Is.Null);
            Assert.That(client.TryGetSession(out session), Is.False);
        }

        [Test]
        public async Task ExpiredRefreshIsDeletedWithoutNetworkMutation()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore
            {
                Record = new ProtectedRefreshTokenRecord(
                    transport.PlayerA,
                    transport.AccountA,
                    "session-a",
                    "expired-refresh",
                    Now.AddSeconds(-1))
            };
            var client = Client(gate, transport, store, Now);
            await client.InitializeAsync();

            GameAccountSession restored = await client.RestoreOrRefreshAsync();

            Assert.That(restored, Is.Null);
            Assert.That(transport.RefreshTokens, Is.Empty);
            Assert.That(store.Record, Is.Null);
            Assert.That(client.State, Is.EqualTo(MobileAccountSessionState.SignedOut));
        }

        [Test]
        public async Task RemoteLogoutFailureStillPurgesEveryLocalCredential()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var client = Client(gate, transport, store, Now);
            await client.InitializeAsync();
            await client.LoginAsync(Request("queen@bee.test", "secret"));
            transport.FailLogout = true;

            MobileAccountSessionException error = Assert.ThrowsAsync<MobileAccountSessionException>(async () =>
                await client.LogoutAsync());

            GameAccountSession session;
            Assert.That(error.Error, Is.EqualTo(MobileAccountSessionError.RemoteLogoutFailure));
            Assert.That(store.Record, Is.Null);
            Assert.That(client.TryGetSession(out session), Is.False);
            Assert.That(client.State, Is.EqualTo(MobileAccountSessionState.SignedOut));
            Assert.That(client.LastSafeErrorCode, Is.EqualTo("auth.remote_logout_failed"));
        }

        [Test]
        public async Task SecondLoginClosesFirstPlayerBeforePublishingSecond()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var client = Client(gate, transport, store, Now);
            await client.InitializeAsync();
            await client.LoginAsync(Request("queen@bee.test", "secret"));
            transport.NextLoginUsesPlayerB = true;

            GameAccountSession second = await client.LoginAsync(Request("worker@bee.test", "secret-two"));

            Assert.That(transport.Events, Is.EqualTo(new[] { "login:a", "logout:access-1", "login:b" }));
            Assert.That(second.PlayerId, Is.EqualTo(transport.PlayerB));
            Assert.That(store.Record.PlayerId, Is.EqualTo(transport.PlayerB));
        }

        [Test]
        public async Task FailedProtectedWriteNeverPublishesAccessToken()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore { CorruptAfterSave = true };
            var client = Client(gate, transport, store, Now);
            await client.InitializeAsync();

            MobileAccountSessionException error = Assert.ThrowsAsync<MobileAccountSessionException>(async () =>
                await client.LoginAsync(Request("queen@bee.test", "secret")));

            GameAccountSession session;
            Assert.That(error.Error, Is.EqualTo(MobileAccountSessionError.ProtectedStorageFailure));
            Assert.That(client.TryGetSession(out session), Is.False);
            Assert.That(store.Record, Is.Null);
            Assert.That(transport.Events, Is.EqualTo(new[] { "login:a", "logout:access-1" }));
        }

        [Test]
        public async Task AccessExpiryStopsAuthenticatedGameClientsWithoutPersistingAccessToken()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var clock = new FakeClock(Now);
            var client = new MobileAccountSessionClient(gate, transport, store, clock);
            await client.InitializeAsync();
            await client.LoginAsync(Request("queen@bee.test", "secret"));
            clock.UtcNow = Now.AddMinutes(16);

            GameAccountSession session;
            Assert.That(client.TryGetSession(out session), Is.False);
            Assert.That(store.Record.RefreshToken, Is.EqualTo("refresh-1"));
        }

        [Test]
        public async Task FailedRotatedRefreshWriteRevokesNewServerSession()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var client = Client(gate, transport, store, Now);
            await client.InitializeAsync();
            await client.LoginAsync(Request("queen@bee.test", "secret"));
            store.CorruptAfterSave = true;

            Assert.ThrowsAsync<MobileAccountSessionException>(async () => await client.RestoreOrRefreshAsync());

            Assert.That(transport.Events, Does.Contain("logout:access-2"));
            Assert.That(store.Record, Is.Null);
        }

        [Test]
        public async Task CancellationDuringRemoteLogoutStillPurgesLocalCredentials()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var client = Client(gate, transport, store, Now);
            await client.InitializeAsync();
            await client.LoginAsync(Request("queen@bee.test", "secret"));
            transport.CancelLogout = true;

            bool canceled = false;
            try
            {
                await client.LogoutAsync();
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }

            GameAccountSession session;
            Assert.That(canceled, Is.True);
            Assert.That(store.Record, Is.Null);
            Assert.That(client.TryGetSession(out session), Is.False);
            Assert.That(client.State, Is.EqualTo(MobileAccountSessionState.SignedOut));
        }

        [Test]
        public async Task ServerRejectedRefreshPurgesRevokedProtectedToken()
        {
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var client = Client(gate, transport, store, Now);
            await client.InitializeAsync();
            await client.LoginAsync(Request("queen@bee.test", "secret"));
            transport.RejectRefresh = true;

            MobileAccountSessionException error = Assert.ThrowsAsync<MobileAccountSessionException>(async () =>
                await client.RestoreOrRefreshAsync());

            Assert.That(error.Error, Is.EqualTo(MobileAccountSessionError.SessionExpired));
            Assert.That(store.Record, Is.Null);
            Assert.That(client.State, Is.EqualTo(MobileAccountSessionState.Expired));
        }

        [Test]
        public async Task ConcurrentExpiredGameReadsShareOneRefreshRotation()
        {
            var clock = new FakeClock(Now);
            var gate = new MobileAccountSessionGate();
            var transport = new FakeTransport(Now);
            var store = new MemoryProtectedStore();
            var client = new MobileAccountSessionClient(gate, transport, store, clock);
            await client.InitializeAsync();
            await client.LoginAsync(Request("queen@bee.test", "secret"));
            clock.UtcNow = Now.AddMinutes(16).AddSeconds(30);

            GameAccountSession[] sessions = await Task.WhenAll(
                client.GetFreshSessionAsync(CancellationToken.None),
                client.GetFreshSessionAsync(CancellationToken.None));

            Assert.That(transport.RefreshTokens.Count, Is.EqualTo(1));
            Assert.That(sessions[0].AccessToken, Is.EqualTo(sessions[1].AccessToken));
            Assert.That(sessions[0].AccessToken, Is.EqualTo("access-2"));
        }

        private static MobileAccountSessionClient Client(
            MobileAccountSessionGate gate,
            FakeTransport transport,
            MemoryProtectedStore store,
            DateTimeOffset now)
        {
            return new MobileAccountSessionClient(gate, transport, store, new FakeClock(now));
        }

        private static MobileAccountLoginRequest Request(string email, string password)
        {
            return new MobileAccountLoginRequest(email, password, "1.0.0", "opaque-device-installation", "ca-east");
        }

        private sealed class FakeClock : IMobileAccountSessionClock
        {
            public FakeClock(DateTimeOffset now) { UtcNow = now; }
            public DateTimeOffset UtcNow { get; set; }
        }

        private sealed class MemoryProtectedStore : IProtectedRefreshTokenStore
        {
            public bool IsProtectionAvailable { get; set; } = true;
            public ProtectedRefreshTokenRecord Record { get; set; }
            public int SaveCalls { get; private set; }
            public int LoadCalls { get; private set; }
            public bool CorruptAfterSave { get; set; }

            public Task SaveAsync(ProtectedRefreshTokenRecord record, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SaveCalls++;
                Record = CorruptAfterSave
                    ? new ProtectedRefreshTokenRecord(record.PlayerId, record.AccountId, record.SessionId, "corrupted", record.RefreshTokenExpiresUtc)
                    : record;
                return Task.CompletedTask;
            }

            public Task<ProtectedRefreshTokenRecord> LoadAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCalls++;
                return Task.FromResult(Record);
            }

            public Task DeleteAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Record = null;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeTransport : IMobileAccountSessionRestTransport
        {
            private readonly DateTimeOffset now;
            private int tokenVersion;

            public FakeTransport(DateTimeOffset now)
            {
                this.now = now;
            }

            public Guid PlayerA { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
            public Guid AccountA { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            public Guid PlayerB { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
            public Guid AccountB { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            public bool Live { get; set; } = true;
            public bool FailLogout { get; set; }
            public bool CancelLogout { get; set; }
            public bool NextLoginUsesPlayerB { get; set; }
            public bool ReturnForeignRefreshIdentity { get; set; }
            public bool RejectRefresh { get; set; }
            public int ReadinessCalls { get; private set; }
            public int LoginCalls { get; private set; }
            public List<string> RefreshTokens { get; } = new List<string>();
            public List<string> Events { get; } = new List<string>();

            public Task<RemoteAccountSessionReadiness> ReadReadinessAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReadinessCalls++;
                return Task.FromResult(new RemoteAccountSessionReadiness
                {
                    ServerTimeUtc = now,
                    AccountCreationAllowed = Live,
                    SessionCreationAllowed = Live,
                    TokenIssuanceAllowed = Live,
                    SecretsAllowedInResponse = false,
                    Claims = new RemoteAccountSessionReadinessClaims { LiveAccounts = Live, LiveSessions = Live },
                    Blockers = new List<string>()
                });
            }

            public Task<RemoteMobileLoginResult> LoginAsync(MobileAccountLoginRequest request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoginCalls++;
                bool useB = NextLoginUsesPlayerB;
                NextLoginUsesPlayerB = false;
                Guid player = useB ? PlayerB : PlayerA;
                Guid account = useB ? AccountB : AccountA;
                string session = useB ? "session-b" : "session-a";
                Events.Add("login:" + (useB ? "b" : "a"));
                return Task.FromResult(new RemoteMobileLoginResult
                {
                    Succeeded = true,
                    PlayerId = player,
                    AccountId = account,
                    Session = new RemoteMobileAuthenticationSession
                    {
                        SessionId = session,
                        PlayerId = player,
                        AccountId = account,
                        LoginUtc = now,
                        ExpirationUtc = now.AddDays(14),
                        IsRevoked = false
                    },
                    Tokens = Tokens(++tokenVersion, player, session)
                });
            }

            public Task<RemoteMobileLoginResult> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return LoginAsync(
                    new MobileAccountLoginRequest(
                        "google@bee.test",
                        "oauth-exchange",
                        request.ClientVersion,
                        request.DeviceIdentifier,
                        request.Region),
                    cancellationToken);
            }

            public Task<RemoteMobileTokenPair> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RefreshTokens.Add(refreshToken);
                if (RejectRefresh)
                    throw new MobileAccountSessionException(MobileAccountSessionError.SessionExpired, "auth.session_expired");
                Guid player = ReturnForeignRefreshIdentity ? PlayerB : PlayerA;
                string session = ReturnForeignRefreshIdentity ? "session-b" : "session-a";
                return Task.FromResult(Tokens(++tokenVersion, player, session));
            }

            public Task LogoutAsync(string bearerAccessToken, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Events.Add("logout:" + bearerAccessToken);
                if (CancelLogout) throw new OperationCanceledException();
                if (FailLogout) throw new InvalidOperationException("simulated");
                return Task.CompletedTask;
            }

            private RemoteMobileTokenPair Tokens(int version, Guid player, string session)
            {
                return new RemoteMobileTokenPair
                {
                    AccessToken = "access-" + version,
                    RefreshToken = "refresh-" + version,
                    AccessTokenExpiresUtc = now.AddMinutes(15 + version),
                    RefreshTokenExpiresUtc = now.AddDays(14),
                    PlayerId = player,
                    SessionId = session
                };
            }
        }
    }
}
