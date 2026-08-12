using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class MobileAccountSessionUiTests
    {
        [TestCase(true, 390f, 844f)]
        [TestCase(false, 1600f, 900f)]
        public void OfficialLoginTargetsAreMobileSafeAndStayInsidePanel(bool portrait, float width, float height)
        {
            Rect panel = HiveViewProductUiPresenter.SplashAuthPanelRectForProof(portrait, width, height);
            Rect[] targets = HiveViewProductUiPresenter.MobileAccountLoginRectsForProof(portrait, width, height);
            Assert.That(targets.Length, Is.EqualTo(3));
            for (int index = 0; index < targets.Length; index++)
            {
                Assert.That(targets[index].width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(targets[index].height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(targets[index].xMin, Is.GreaterThanOrEqualTo(panel.xMin));
                Assert.That(targets[index].xMax, Is.LessThanOrEqualTo(panel.xMax));
                Assert.That(targets[index].yMin, Is.GreaterThanOrEqualTo(panel.yMin));
                Assert.That(targets[index].yMax, Is.LessThanOrEqualTo(panel.yMax - 72f));
                if (index > 0) Assert.That(targets[index - 1].Overlaps(targets[index]), Is.False);
            }
        }

        [Test]
        public void PresenterOnlyExposesCredentialFormWhenClientAndServerGatesAreReady()
        {
            HiveViewProductUiPresenter.ResetMobileAccountSessionForProof();
            HiveViewProductUiPresenter.SetSplashAuthGateForProof("login");
            HiveViewProductUiPresenter.SetAccountSessionReadinessForProof("ready");
            try
            {
                string[] before = HiveViewProductUiPresenter.SplashAuthDemoForProof();
                Assert.That(before, Does.Contain("login_credential_form_visible:false"));

                var client = new MobileAccountSessionClient(
                    HiveViewProductUiPresenter.AccountSessionGateForRuntime(),
                    new NoOpTransport(),
                    new AvailableEmptyStore());
                HiveViewProductUiPresenter.ConfigureMobileAccountSessionForRuntime(
                    client,
                    (email, password) => new MobileAccountLoginRequest(email, password, "1", "opaque", "test"));

                string[] after = HiveViewProductUiPresenter.SplashAuthDemoForProof();
                Assert.That(after, Does.Contain("login_credential_form_visible:true"));
                Assert.That(after, Does.Contain("password_collection_while_closed:false"));
                Assert.That(after, Does.Contain("access_token_storage:memory_only"));
                Assert.That(after, Does.Contain("refresh_token_storage:protected_store_only"));
            }
            finally
            {
                HiveViewProductUiPresenter.ResetMobileAccountSessionForProof();
            }
        }

        [Test]
        public void SignedOutClientDoesNotHideReadyGoogleLogin()
        {
            HiveViewProductUiPresenter.ResetMobileAccountSessionForProof();
            HiveViewProductUiPresenter.SetAccountSessionReadinessForProof("ready");
            var client = new MobileAccountSessionClient(
                HiveViewProductUiPresenter.AccountSessionGateForRuntime(),
                new NoOpTransport(),
                new AvailableEmptyStore());
            HiveViewProductUiPresenter.ConfigureMobileAccountSessionForRuntime(
                client,
                (email, password) => new MobileAccountLoginRequest(email, password, "1", "opaque", "test"),
                (authorizationCode, codeVerifier, redirectUri) => new GoogleLoginRequest(authorizationCode, codeVerifier, redirectUri, "1", "opaque", "test"),
                "google-client-id");
            try
            {
                Assert.That(ValueFor(HiveViewProductUiPresenter.ConnectionTruthForProof(), "connection_truth_state"), Is.EqualTo("Ready"));
            }
            finally
            {
                HiveViewProductUiPresenter.ResetMobileAccountSessionForProof();
            }
        }

        [Test]
        public void OfficialTransportRequiresTlsAndKeepsRoutesExplicit()
        {
            Assert.Throws<ArgumentException>(() => new UnityMobileAccountSessionRestTransport("http://example.test"));
            Assert.DoesNotThrow(() => new UnityMobileAccountSessionRestTransport("https://api.example.test"));
            Assert.DoesNotThrow(() => new UnityMobileAccountSessionRestTransport("http://127.0.0.1:5133", 20, true));
            string[] rows = UnityMobileAccountSessionRestTransport.ProofRows();
            Assert.That(rows, Does.Contain("auth_refresh_route:POST /auth/refresh"));
            Assert.That(rows, Does.Contain("auth_logout_route:POST /auth/logout bearer-only"));
            Assert.That(rows, Does.Contain("auth_transport_tls_required:true"));
            Assert.That(rows, Does.Contain("auth_transport_custom_certificate_handler:false"));
        }

        [Test]
        public void EditorNeverPretendsToProvideAndroidProtectedTokenStorage()
        {
            var store = new AndroidKeystoreRefreshTokenStore();
            Assert.That(store.IsProtectionAvailable, Is.False);
            Assert.That(AndroidKeystoreRefreshTokenStore.ProofRows(), Does.Contain("android_refresh_key_provider:AndroidKeyStore"));
            Assert.That(AndroidKeystoreRefreshTokenStore.ProofRows(), Does.Contain("editor_refresh_persistence:false"));
        }

        [Test]
        public void OfficialGameTransportRequiresTlsAndOwnsNoAutomaticRetry()
        {
            var codec = new SystemTextGameJsonCodec();
            Assert.Throws<ArgumentException>(() => new UnityAuthenticatedGameRestTransport("http://example.test", codec));
            Assert.DoesNotThrow(() => new UnityAuthenticatedGameRestTransport("https://api.example.test", codec));
            Assert.DoesNotThrow(() => new UnityAuthenticatedGameRestTransport("http://127.0.0.1:5133", codec, 20, true));
            string[] rows = UnityAuthenticatedGameRestTransport.ProofRows();
            Assert.That(rows, Does.Contain("game_transport_tls_required:true"));
            Assert.That(rows, Does.Contain("game_transport_automatic_retry:false"));
            Assert.That(rows, Does.Contain("game_transport_get_requires_private_no_store:true"));
        }

        [Test]
        public void EditorNeverPretendsToProvideAndroidProtectedGameCache()
        {
            var store = new AndroidKeystoreGameReadCacheStore();
            Assert.That(store.IsProtectionAvailable, Is.False);
            Assert.That(AndroidKeystoreGameReadCacheStore.ProofRows(), Does.Contain("android_game_cache_key_provider:AndroidKeyStore"));
            Assert.That(AndroidKeystoreGameReadCacheStore.ProofRows(), Does.Contain("editor_game_cache_persistence:false"));
        }

        [TestCase("checking")]
        [TestCase("preparation")]
        [TestCase("server_ready")]
        [TestCase("ready")]
        [TestCase("unavailable")]
        public void ConnectionSurfacesStayAlignedAcrossReadinessGateStates(string readinessState)
        {
            HiveViewProductUiPresenter.ResetMobileAccountSessionForProof();
            HiveViewProductUiPresenter.SetAccountSessionReadinessForProof(readinessState);
            try
            {
                string[] truth = HiveViewProductUiPresenter.ConnectionTruthForProof();
                string[] surfaces = HiveViewProductUiPresenter.ConnectionSurfacesForProof();

                string state = ValueFor(truth, "connection_truth_state");
                string badge = ValueFor(truth, "connection_truth_badge");
                string title = ValueFor(truth, "connection_truth_title");
                string body = ValueFor(truth, "connection_truth_body");

                Assert.That(ValueFor(surfaces, "connection_surfaces_state"), Is.EqualTo(state));
                Assert.That(ValueFor(surfaces, "connection_surfaces_title"), Is.EqualTo(title));
                Assert.That(ValueFor(surfaces, "connection_surfaces_body"), Is.EqualTo(body));
                Assert.That(ValueFor(surfaces, "connection_surfaces_badge"), Is.EqualTo(badge));
                Assert.That(ValueFor(surfaces, "connection_surfaces_hud_badge"), Is.EqualTo(badge));
                Assert.That(ValueFor(surfaces, "connection_surfaces_splash_uses_badge_key"), Is.EqualTo("true"));
                Assert.That(ValueFor(surfaces, "connection_surfaces_single_source"), Is.EqualTo("current_connection_truth"));
            }
            finally
            {
                HiveViewProductUiPresenter.ResetMobileAccountSessionForProof();
            }
        }

        [Test]
        public async Task NetworkLossKeepsSplashCardAndHudOnTheSameOfflineTruthUntilRestore()
        {
            HiveViewProductUiPresenter.ResetMobileAccountSessionForProof();
            HiveViewProductUiPresenter.SetAccountSessionReadinessForProof("ready");
            var transport = new LiveSessionTransport(new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero));
            var client = new MobileAccountSessionClient(
                HiveViewProductUiPresenter.AccountSessionGateForRuntime(),
                transport,
                new RetainingStore(),
                new FixedClock(new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero)));

            HiveViewProductUiPresenter.ConfigureMobileAccountSessionForRuntime(
                client,
                (email, password) => new MobileAccountLoginRequest(email, password, "1", "opaque-device", "ca-east"));

            try
            {
                await client.InitializeAsync();
                HiveViewProductUiPresenter.SetAccountSessionReadinessForProof("ready");
                GameAccountSession session = await client.LoginAsync(Request("auth@bee.test", "secret"));
                Assert.That(session, Is.Not.Null);

                string[] liveTruth = HiveViewProductUiPresenter.ConnectionTruthForProof();
                string[] liveSurfaces = HiveViewProductUiPresenter.ConnectionSurfacesForProof();
                Assert.That(ValueFor(liveSurfaces, "connection_surfaces_state"), Is.EqualTo("AuthenticatedLive"));
                Assert.That(ValueFor(liveSurfaces, "connection_surfaces_hud_badge"), Is.EqualTo(ValueFor(liveTruth, "connection_truth_badge")));

                client.MarkNetworkUnavailable("auth.network_unavailable");

                string[] offlineTruth = HiveViewProductUiPresenter.ConnectionTruthForProof();
                string[] offlineSurfaces = HiveViewProductUiPresenter.ConnectionSurfacesForProof();
                Assert.That(ValueFor(offlineTruth, "connection_truth_state"), Is.EqualTo("Offline"));
                Assert.That(ValueFor(offlineSurfaces, "connection_surfaces_state"), Is.EqualTo("Offline"));
                Assert.That(ValueFor(offlineSurfaces, "connection_surfaces_title"), Is.EqualTo(ValueFor(offlineTruth, "connection_truth_title")));
                Assert.That(ValueFor(offlineSurfaces, "connection_surfaces_badge"), Is.EqualTo(ValueFor(offlineTruth, "connection_truth_badge")));
                Assert.That(ValueFor(offlineSurfaces, "connection_surfaces_hud_badge"), Is.EqualTo(ValueFor(offlineTruth, "connection_truth_badge")));

                GameAccountSession restored = await client.RestoreOrRefreshAsync();
                Assert.That(restored, Is.Not.Null);

                string[] restoredSurfaces = HiveViewProductUiPresenter.ConnectionSurfacesForProof();
                Assert.That(ValueFor(restoredSurfaces, "connection_surfaces_state"), Is.EqualTo("AuthenticatedLive"));
                Assert.That(ValueFor(restoredSurfaces, "connection_surfaces_hud_badge"), Is.EqualTo(ValueFor(HiveViewProductUiPresenter.ConnectionTruthForProof(), "connection_truth_badge")));
            }
            finally
            {
                HiveViewProductUiPresenter.ResetMobileAccountSessionForProof();
            }
        }

        private static string ValueFor(string[] rows, string prefix)
        {
            foreach (string row in rows)
            {
                if (row.StartsWith(prefix + ":", StringComparison.Ordinal))
                    return row.Substring(prefix.Length + 1);
            }

            return string.Empty;
        }

        private static MobileAccountLoginRequest Request(string email, string password)
        {
            return new MobileAccountLoginRequest(email, password, "1", "opaque-device", "ca-east");
        }

        private sealed class AvailableEmptyStore : IProtectedRefreshTokenStore
        {
            public bool IsProtectionAvailable => true;
            public Task SaveAsync(ProtectedRefreshTokenRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task<ProtectedRefreshTokenRecord> LoadAsync(CancellationToken cancellationToken) => Task.FromResult<ProtectedRefreshTokenRecord>(null);
            public Task DeleteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class FixedClock : IMobileAccountSessionClock
        {
            public FixedClock(DateTimeOffset now)
            {
                UtcNow = now;
            }

            public DateTimeOffset UtcNow { get; }
        }

        private sealed class RetainingStore : IProtectedRefreshTokenStore
        {
            private ProtectedRefreshTokenRecord record;

            public bool IsProtectionAvailable => true;
            public Task SaveAsync(ProtectedRefreshTokenRecord value, CancellationToken cancellationToken)
            {
                record = value;
                return Task.CompletedTask;
            }

            public Task<ProtectedRefreshTokenRecord> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(record);
            public Task DeleteAsync(CancellationToken cancellationToken)
            {
                record = null;
                return Task.CompletedTask;
            }
        }

        private sealed class NoOpTransport : IMobileAccountSessionRestTransport
        {
            public Task<RemoteAccountSessionReadiness> ReadReadinessAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
            public Task<RemoteMobileLoginResult> LoginAsync(MobileAccountLoginRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
            public Task<RemoteMobileLoginResult> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
            public Task<RemoteMobileTokenPair> RefreshAsync(string refreshToken, CancellationToken cancellationToken) => throw new NotSupportedException();
            public Task LogoutAsync(string bearerAccessToken, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class LiveSessionTransport : IMobileAccountSessionRestTransport
        {
            private readonly DateTimeOffset now;
            private int tokenVersion;

            public LiveSessionTransport(DateTimeOffset now)
            {
                this.now = now;
            }

            public Guid PlayerId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
            public Guid AccountId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            public Task<RemoteAccountSessionReadiness> ReadReadinessAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(new RemoteAccountSessionReadiness
                {
                    ServerTimeUtc = now,
                    AccountCreationAllowed = true,
                    SessionCreationAllowed = true,
                    TokenIssuanceAllowed = true,
                    SecretsAllowedInResponse = false,
                    Claims = new RemoteAccountSessionReadinessClaims
                    {
                        LiveAccounts = true,
                        LiveSessions = true,
                        GameplayAuthorityGranted = true
                    },
                    Blockers = new List<string>()
                });
            }

            public Task<RemoteMobileLoginResult> LoginAsync(MobileAccountLoginRequest request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Result(++tokenVersion));
            }

            public Task<RemoteMobileLoginResult> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Result(++tokenVersion));
            }

            public Task<RemoteMobileTokenPair> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RemoteMobileTokenPair tokens = Tokens(++tokenVersion);
                return Task.FromResult(tokens);
            }

            public Task LogoutAsync(string bearerAccessToken, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }

            private RemoteMobileLoginResult Result(int version)
            {
                return new RemoteMobileLoginResult
                {
                    Succeeded = true,
                    PlayerId = PlayerId,
                    AccountId = AccountId,
                    Session = new RemoteMobileAuthenticationSession
                    {
                        SessionId = "session-a",
                        PlayerId = PlayerId,
                        AccountId = AccountId,
                        LoginUtc = now,
                        ExpirationUtc = now.AddDays(14),
                        IsRevoked = false
                    },
                    Tokens = Tokens(version)
                };
            }

            private RemoteMobileTokenPair Tokens(int version)
            {
                return new RemoteMobileTokenPair
                {
                    AccessToken = "access-" + version,
                    RefreshToken = "refresh-" + version,
                    AccessTokenExpiresUtc = now.AddMinutes(15 + version),
                    RefreshTokenExpiresUtc = now.AddDays(14),
                    PlayerId = PlayerId,
                    SessionId = "session-a"
                };
            }
        }
    }
}
