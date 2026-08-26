using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BeeKingdom.Networking
{
    public sealed class UnityMobileAccountSessionRestTransport : IMobileAccountSessionRestTransport
    {
        private const int MaxResponseBytes = 1024 * 1024;
        private readonly string baseUrl;
        private readonly int timeoutSeconds;

        public UnityMobileAccountSessionRestTransport(
            string baseUrl,
            int timeoutSeconds = 20,
            bool allowInsecureLoopbackForDevelopment = false)
        {
            Uri uri;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttps &&
                 !(allowInsecureLoopbackForDevelopment && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
                throw new ArgumentException("Official account transport requires HTTPS or an explicitly allowed development loopback.", nameof(baseUrl));
            if (timeoutSeconds < 5 || timeoutSeconds > 120)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            this.baseUrl = uri.AbsoluteUri.TrimEnd('/');
            this.timeoutSeconds = timeoutSeconds;
        }

        public async Task<RemoteAccountSessionReadiness> ReadReadinessAsync(CancellationToken cancellationToken)
        {
            TransportResponse response = await SendAsync("GET", "/runtime/account-session-readiness", null, null, cancellationToken);
            RequireSuccess(response, "auth.readiness_unavailable");
            if (ContainsSecretField(response.Body))
                throw InvalidResponse("auth.readiness_contains_secret");
            ReadinessWire wire = Parse<ReadinessWire>(response.Body, "auth.readiness_invalid");
            return new RemoteAccountSessionReadiness
            {
                ServerTimeUtc = ParseUtc(wire.serverTimeUtc, "auth.readiness_invalid"),
                AccountCreationAllowed = wire.accountCreationAllowed,
                SessionCreationAllowed = wire.sessionCreationAllowed,
                TokenIssuanceAllowed = wire.tokenIssuanceAllowed,
                SecretsAllowedInResponse = wire.secretsAllowedInResponse,
                Claims = wire.claims == null
                    ? null
                    : new RemoteAccountSessionReadinessClaims
                    {
                        LiveAccounts = wire.claims.liveAccounts,
                        LiveSessions = wire.claims.liveSessions,
                        OfficialProgression = wire.claims.officialProgression,
                        OfficialPersistence = wire.claims.officialPersistence,
                        RealTimeSynchronization = wire.claims.realTimeSynchronization,
                        GameplayAuthorityGranted = wire.claims.gameplayAuthorityGranted
                    },
                Blockers = wire.blockers == null ? null : new List<string>(wire.blockers)
            };
        }

        public async Task<RemoteMobileLoginResult> LoginAsync(
            MobileAccountLoginRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var body = new LoginRequestWire
            {
                email = request.Email,
                password = request.Password,
                clientVersion = request.ClientVersion,
                ipAddress = string.Empty,
                deviceIdentifier = request.DeviceIdentifier,
                region = request.Region
            };
            TransportResponse response = await SendAsync("POST", "/auth/login", JsonUtility.ToJson(body), null, cancellationToken);
            if (!response.Success)
            {
                return new RemoteMobileLoginResult
                {
                    Succeeded = false,
                    ErrorCode = ParseErrorCode(response.Body)
                };
            }

            LoginResultWire wire = Parse<LoginResultWire>(response.Body, "auth.login_response_invalid");
            return MapLogin(wire);
        }

        public async Task<RemoteMobileLoginResult> LoginWithGoogleAsync(
            GoogleLoginRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var body = new GoogleLoginRequestWire
            {
                authorizationCode = request.AuthorizationCode,
                codeVerifier = request.CodeVerifier,
                redirectUri = request.RedirectUri,
                oauthClientId = request.OAuthClientId,
                clientVersion = request.ClientVersion,
                deviceIdentifier = request.DeviceIdentifier,
                region = request.Region
            };
            TransportResponse response = await SendAsync("POST", "/auth/login/google", JsonUtility.ToJson(body), null, cancellationToken);
            if (!response.Success)
            {
                return new RemoteMobileLoginResult
                {
                    Succeeded = false,
                    ErrorCode = ParseErrorCode(response.Body)
                };
            }

            LoginResultWire wire = Parse<LoginResultWire>(response.Body, "auth.login_response_invalid");
            return MapLogin(wire);
        }

        public async Task<RemoteMobileTokenPair> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new MobileAccountSessionException(MobileAccountSessionError.SessionExpired, "auth.session_required");
            var body = new RefreshRequestWire { refreshToken = refreshToken };
            TransportResponse response = await SendAsync("POST", "/auth/refresh", JsonUtility.ToJson(body), null, cancellationToken);
            if (!response.Success)
            {
                string code = ParseErrorCode(response.Body);
                if (response.StatusCode == 401 || string.Equals(code, "auth.session_required", StringComparison.Ordinal))
                    throw new MobileAccountSessionException(MobileAccountSessionError.SessionExpired, "auth.session_expired");
                throw new MobileAccountSessionException(MobileAccountSessionError.AuthenticationRejected, code);
            }

            return MapTokens(Parse<TokenPairWire>(response.Body, "auth.token_response_invalid"));
        }

        public async Task LogoutAsync(string bearerAccessToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bearerAccessToken)) return;
            TransportResponse response = await SendAsync("POST", "/auth/logout", "{}", bearerAccessToken, cancellationToken);
            if (response.Success || response.StatusCode == 401) return;
            throw new MobileAccountSessionException(MobileAccountSessionError.RemoteLogoutFailure, "auth.remote_logout_failed");
        }

        public static string[] ProofRows()
        {
            return new[]
            {
                "auth_readiness_route:GET /runtime/account-session-readiness",
                "auth_login_route:POST /auth/login",
                "auth_refresh_route:POST /auth/refresh",
                "auth_logout_route:POST /auth/logout bearer-only",
                "auth_transport_tls_required:true",
                "auth_transport_custom_certificate_handler:false",
                "auth_transport_automatic_retry:false",
                "auth_transport_response_bound_bytes:" + MaxResponseBytes,
                "auth_transport_error_body_exposes_tokens:false"
            };
        }

        private async Task<TransportResponse> SendAsync(
            string method,
            string path,
            string json,
            string bearer,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var request = new UnityWebRequest(baseUrl + path, method))
            {
                request.timeout = timeoutSeconds;
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                if (!string.IsNullOrWhiteSpace(bearer))
                    request.SetRequestHeader("Authorization", "Bearer " + bearer);

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                operation.completed += _ => completion.TrySetResult(true);
                SynchronizationContext unityContext = SynchronizationContext.Current;
                using (cancellationToken.Register(() =>
                {
                    if (unityContext != null)
                        unityContext.Post(_ => request.Abort(), null);
                    else
                        request.Abort();
                }))
                {
                    await completion.Task;
                }
                cancellationToken.ThrowIfCancellationRequested();

                byte[] data = request.downloadHandler == null ? null : request.downloadHandler.data;
                if (data != null && data.Length > MaxResponseBytes)
                    throw InvalidResponse("auth.response_too_large");
                string body = request.downloadHandler == null ? string.Empty : request.downloadHandler.text;
                bool protocolSuccess = request.responseCode >= 200 && request.responseCode <= 299;
                if (!protocolSuccess && request.result != UnityWebRequest.Result.ProtocolError)
                    throw new MobileAccountSessionException(MobileAccountSessionError.TransportFailure, "auth.transport_failure");
                return new TransportResponse((int)request.responseCode, body, protocolSuccess);
            }
        }

        private static RemoteMobileLoginResult MapLogin(LoginResultWire wire)
        {
            if (wire == null) throw InvalidResponse("auth.login_response_invalid");
            Guid playerId = ParsePlayerId(wire.playerId, "auth.login_response_invalid");
            Guid accountId = ParseGuid(wire.accountId, "auth.login_response_invalid");
            SessionWire session = wire.session;
            if (session == null) throw InvalidResponse("auth.login_response_invalid");
            return new RemoteMobileLoginResult
            {
                Succeeded = wire.succeeded,
                PlayerId = playerId,
                AccountId = accountId,
                ErrorCode = wire.errorCode,
                Session = new RemoteMobileAuthenticationSession
                {
                    SessionId = session.sessionId,
                    PlayerId = ParsePlayerId(session.playerId, "auth.login_response_invalid"),
                    AccountId = ParseGuid(session.accountId, "auth.login_response_invalid"),
                    LoginUtc = ParseUtc(session.loginUtc, "auth.login_response_invalid"),
                    ExpirationUtc = ParseUtc(session.expirationUtc, "auth.login_response_invalid"),
                    IsRevoked = session.isRevoked
                },
                Tokens = MapTokens(wire.tokens),
                IsNewAccount = wire.isNewAccount,
                DisplayName = wire.displayName,
                IsOnboarded = wire.isOnboarded
            };
        }

        private static RemoteMobileTokenPair MapTokens(TokenPairWire wire)
        {
            if (wire == null) throw InvalidResponse("auth.token_response_invalid");
            return new RemoteMobileTokenPair
            {
                AccessToken = wire.accessToken,
                RefreshToken = wire.refreshToken,
                AccessTokenExpiresUtc = ParseUtc(wire.accessTokenExpiresUtc, "auth.token_response_invalid"),
                RefreshTokenExpiresUtc = ParseUtc(wire.refreshTokenExpiresUtc, "auth.token_response_invalid"),
                PlayerId = ParsePlayerId(wire.playerId, "auth.token_response_invalid"),
                SessionId = wire.sessionId
            };
        }

        private static T Parse<T>(string json, string code) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) throw InvalidResponse(code);
            try
            {
                T value = JsonUtility.FromJson<T>(json);
                if (value == null) throw InvalidResponse(code);
                return value;
            }
            catch (MobileAccountSessionException)
            {
                throw;
            }
            catch
            {
                throw InvalidResponse(code);
            }
        }

        private static DateTimeOffset ParseUtc(string value, string code)
        {
            DateTimeOffset result;
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result) ||
                result.Offset != TimeSpan.Zero)
                throw InvalidResponse(code);
            return result;
        }

        private static Guid ParsePlayerId(PlayerIdWire value, string code)
        {
            return value == null ? throw InvalidResponse(code) : ParseGuid(value.value, code);
        }

        private static Guid ParseGuid(string value, string code)
        {
            Guid parsed;
            if (!Guid.TryParseExact(value, "D", out parsed) || parsed == Guid.Empty)
                throw InvalidResponse(code);
            return parsed;
        }

        private static void RequireSuccess(TransportResponse response, string code)
        {
            if (!response.Success)
                throw new MobileAccountSessionException(MobileAccountSessionError.TransportFailure, code);
        }

        private static string ParseErrorCode(string body)
        {
            try
            {
                ErrorWire error = JsonUtility.FromJson<ErrorWire>(body ?? string.Empty);
                if (error != null && IsSafeErrorCode(error.code)) return error.code;
            }
            catch
            {
            }
            return "auth.rejected";
        }

        private static bool IsSafeErrorCode(string code)
        {
            return string.Equals(code, "auth.invalid_request", StringComparison.Ordinal) ||
                string.Equals(code, "auth.invalid_credentials", StringComparison.Ordinal) ||
                string.Equals(code, "auth.session_required", StringComparison.Ordinal) ||
                string.Equals(code, "auth.session_limit", StringComparison.Ordinal) ||
                string.Equals(code, "auth.rate_limited", StringComparison.Ordinal) ||
                string.Equals(code, "auth.unavailable", StringComparison.Ordinal) ||
                string.Equals(code, "auth.account_disabled", StringComparison.Ordinal) ||
                string.Equals(code, "auth.google_sign_in_failed", StringComparison.Ordinal);
        }

        private static bool ContainsSecretField(string json)
        {
            return (json ?? string.Empty).IndexOf("accessToken", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (json ?? string.Empty).IndexOf("refreshToken", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (json ?? string.Empty).IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static MobileAccountSessionException InvalidResponse(string code)
        {
            return new MobileAccountSessionException(MobileAccountSessionError.InvalidResponse, code);
        }

        private sealed class TransportResponse
        {
            public TransportResponse(int statusCode, string body, bool success)
            {
                StatusCode = statusCode;
                Body = body ?? string.Empty;
                Success = success;
            }

            public int StatusCode { get; }
            public string Body { get; }
            public bool Success { get; }
        }

        [Serializable]
        private sealed class LoginRequestWire
        {
            public string email;
            public string password;
            public string clientVersion;
            public string ipAddress;
            public string deviceIdentifier;
            public string region;
        }

        [Serializable]
        private sealed class RefreshRequestWire
        {
            public string refreshToken;
        }

        [Serializable]
        private sealed class GoogleLoginRequestWire
        {
            public string authorizationCode;
            public string codeVerifier;
            public string redirectUri;
            public string oauthClientId;
            public string clientVersion;
            public string deviceIdentifier;
            public string region;
        }

        [Serializable]
        private sealed class PlayerIdWire
        {
            public string value;
        }

        [Serializable]
        private sealed class ClaimsWire
        {
            public bool liveAccounts;
            public bool liveSessions;
            public bool officialProgression;
            public bool officialPersistence;
            public bool realTimeSynchronization;
            public bool gameplayAuthorityGranted;
        }

        [Serializable]
        private sealed class ReadinessWire
        {
            public string serverTimeUtc;
            public bool accountCreationAllowed;
            public bool sessionCreationAllowed;
            public bool tokenIssuanceAllowed;
            public bool secretsAllowedInResponse;
            public ClaimsWire claims;
            public string[] blockers;
        }

        [Serializable]
        private sealed class SessionWire
        {
            public string sessionId;
            public PlayerIdWire playerId;
            public string accountId;
            public string loginUtc;
            public string expirationUtc;
            public bool isRevoked;
        }

        [Serializable]
        private sealed class TokenPairWire
        {
            public string accessToken;
            public string refreshToken;
            public string accessTokenExpiresUtc;
            public string refreshTokenExpiresUtc;
            public PlayerIdWire playerId;
            public string sessionId;
        }

        [Serializable]
        private sealed class LoginResultWire
        {
            public bool succeeded;
            public PlayerIdWire playerId;
            public string accountId;
            public SessionWire session;
            public TokenPairWire tokens;
            public string errorCode;
            public bool isNewAccount;
            public string displayName;
            public bool isOnboarded;
        }

        [Serializable]
        private sealed class ErrorWire
        {
            public string code;
        }
    }
}
