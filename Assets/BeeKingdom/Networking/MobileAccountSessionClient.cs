using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public enum MobileAccountSessionState
    {
        NotConfigured = 0,
        SignedOut = 1,
        Authenticating = 2,
        Authenticated = 3,
        Refreshing = 4,
        Expired = 5,
        Faulted = 6,
        Offline = 7
    }

    public enum MobileAccountSessionError
    {
        NotConfigured = 0,
        InvalidRequest = 1,
        InvalidResponse = 2,
        AuthenticationRejected = 3,
        ProtectedStorageUnavailable = 4,
        ProtectedStorageFailure = 5,
        SessionExpired = 6,
        TransportFailure = 7,
        RemoteLogoutFailure = 8
    }

    public sealed class MobileAccountSessionException : Exception
    {
        public MobileAccountSessionException(MobileAccountSessionError error, string safeCode)
            : base(safeCode ?? string.Empty)
        {
            Error = error;
            SafeCode = safeCode ?? string.Empty;
        }

        public MobileAccountSessionError Error { get; }
        public string SafeCode { get; }
    }

    public sealed class RemoteAccountSessionReadinessClaims
    {
        public bool LiveAccounts { get; set; }
        public bool LiveSessions { get; set; }
        public bool OfficialProgression { get; set; }
        public bool OfficialPersistence { get; set; }
        public bool RealTimeSynchronization { get; set; }
        public bool GameplayAuthorityGranted { get; set; }
    }

    public sealed class RemoteAccountSessionReadiness
    {
        public DateTimeOffset ServerTimeUtc { get; set; }
        public bool AccountCreationAllowed { get; set; }
        public bool SessionCreationAllowed { get; set; }
        public bool TokenIssuanceAllowed { get; set; }
        public bool SecretsAllowedInResponse { get; set; }
        public RemoteAccountSessionReadinessClaims Claims { get; set; }
        public List<string> Blockers { get; set; }
    }

    public sealed class MobileAccountLoginRequest
    {
        public MobileAccountLoginRequest(
            string email,
            string password,
            string clientVersion,
            string deviceIdentifier,
            string region)
        {
            Email = email ?? string.Empty;
            Password = password ?? string.Empty;
            ClientVersion = clientVersion ?? string.Empty;
            DeviceIdentifier = deviceIdentifier ?? string.Empty;
            Region = region ?? string.Empty;
        }

        public string Email { get; }
        public string Password { get; }
        public string ClientVersion { get; }
        public string DeviceIdentifier { get; }
        public string Region { get; }
    }

    public sealed class RemoteMobileAuthenticationSession
    {
        public string SessionId { get; set; }
        public Guid PlayerId { get; set; }
        public Guid AccountId { get; set; }
        public DateTimeOffset LoginUtc { get; set; }
        public DateTimeOffset ExpirationUtc { get; set; }
        public bool IsRevoked { get; set; }
    }

    public sealed class RemoteMobileTokenPair
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTimeOffset AccessTokenExpiresUtc { get; set; }
        public DateTimeOffset RefreshTokenExpiresUtc { get; set; }
        public Guid PlayerId { get; set; }
        public string SessionId { get; set; }
    }

    public sealed class RemoteMobileLoginResult
    {
        public bool Succeeded { get; set; }
        public Guid PlayerId { get; set; }
        public Guid AccountId { get; set; }
        public RemoteMobileAuthenticationSession Session { get; set; }
        public RemoteMobileTokenPair Tokens { get; set; }
        public string ErrorCode { get; set; }
        public bool IsNewAccount { get; set; }
        public string DisplayName { get; set; }
        public bool IsOnboarded { get; set; }
    }

    public sealed class GoogleLoginRequest
    {
        public GoogleLoginRequest(
            string authorizationCode,
            string codeVerifier,
            string redirectUri,
            string clientVersion,
            string deviceIdentifier,
            string region)
        {
            AuthorizationCode = authorizationCode ?? string.Empty;
            CodeVerifier = codeVerifier ?? string.Empty;
            RedirectUri = redirectUri ?? string.Empty;
            ClientVersion = clientVersion ?? string.Empty;
            DeviceIdentifier = deviceIdentifier ?? string.Empty;
            Region = region ?? string.Empty;
        }

        public string AuthorizationCode { get; }
        public string CodeVerifier { get; }
        public string RedirectUri { get; }
        public string ClientVersion { get; }
        public string DeviceIdentifier { get; }
        public string Region { get; }
    }

    public interface IMobileAccountSessionRestTransport
    {
        Task<RemoteAccountSessionReadiness> ReadReadinessAsync(CancellationToken cancellationToken);
        Task<RemoteMobileLoginResult> LoginAsync(MobileAccountLoginRequest request, CancellationToken cancellationToken);
        Task<RemoteMobileLoginResult> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken);
        Task<RemoteMobileTokenPair> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
        Task LogoutAsync(string bearerAccessToken, CancellationToken cancellationToken);
    }

    public sealed class ProtectedRefreshTokenRecord
    {
        public ProtectedRefreshTokenRecord(
            Guid playerId,
            Guid accountId,
            string sessionId,
            string refreshToken,
            DateTimeOffset refreshTokenExpiresUtc)
        {
            PlayerId = playerId;
            AccountId = accountId;
            SessionId = sessionId ?? string.Empty;
            RefreshToken = refreshToken ?? string.Empty;
            RefreshTokenExpiresUtc = refreshTokenExpiresUtc;
        }

        public Guid PlayerId { get; }
        public Guid AccountId { get; }
        public string SessionId { get; }
        public string RefreshToken { get; }
        public DateTimeOffset RefreshTokenExpiresUtc { get; }
    }

    public interface IProtectedRefreshTokenStore
    {
        bool IsProtectionAvailable { get; }
        Task SaveAsync(ProtectedRefreshTokenRecord record, CancellationToken cancellationToken);
        Task<ProtectedRefreshTokenRecord> LoadAsync(CancellationToken cancellationToken);
        Task DeleteAsync(CancellationToken cancellationToken);
    }

    public sealed class UnavailableProtectedRefreshTokenStore : IProtectedRefreshTokenStore
    {
        public bool IsProtectionAvailable => false;

        public Task SaveAsync(ProtectedRefreshTokenRecord record, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new MobileAccountSessionException(
                MobileAccountSessionError.ProtectedStorageUnavailable,
                "auth.protected_storage_unavailable");
        }

        public Task<ProtectedRefreshTokenRecord> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<ProtectedRefreshTokenRecord>(null);
        }

        public Task DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    public interface IMobileAccountSessionClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public sealed class SystemMobileAccountSessionClock : IMobileAccountSessionClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    public sealed class MobileAccountSessionClient : IRefreshableGameAccountSessionSource
    {
        private const int MaxEmailCharacters = 254;
        private const int MaxPasswordCharacters = 1024;
        private const int MaxMetadataCharacters = 256;
        private const int MaxSessionIdCharacters = 512;
        private const int MaxTokenCharacters = 8192;

        private readonly MobileAccountSessionGate gate;
        private readonly IMobileAccountSessionRestTransport transport;
        private readonly IProtectedRefreshTokenStore refreshTokenStore;
        private readonly IMobileAccountSessionClock clock;
        private readonly SemaphoreSlim lifecycle = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim gameRefresh = new SemaphoreSlim(1, 1);

        private GameAccountSession activeSession;
        private Guid activeAccountId;
        private string activeSessionId = string.Empty;
        private DateTimeOffset accessTokenExpiresUtc;
        private ProtectedRefreshTokenRecord refreshRecord;

        public MobileAccountSessionClient(
            MobileAccountSessionGate gate,
            IMobileAccountSessionRestTransport transport,
            IProtectedRefreshTokenStore refreshTokenStore,
            IMobileAccountSessionClock clock = null)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.refreshTokenStore = refreshTokenStore ?? throw new ArgumentNullException(nameof(refreshTokenStore));
            this.clock = clock ?? new SystemMobileAccountSessionClock();
        }

        public MobileAccountSessionGate Gate => gate;
        public MobileAccountSessionState State { get; private set; } = MobileAccountSessionState.NotConfigured;
        public string LastSafeErrorCode { get; private set; } = string.Empty;
        public bool HasProtectedRefreshToken => refreshRecord != null;
        public bool ServerGameplayAuthorityGranted { get; private set; }
        public Guid ActivePlayerId => activeSession == null ? Guid.Empty : activeSession.PlayerId;
        public Guid ActiveAccountId => activeAccountId;
        public string ActiveSessionId => activeSessionId;

        public async Task<AccountSessionReadinessSnapshot> InitializeAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ClearAccessTokenMemory();
                refreshRecord = null;
                ServerGameplayAuthorityGranted = false;
                LastSafeErrorCode = string.Empty;
                gate.ResetForLogoutOrPlayerChange();
                if (!refreshTokenStore.IsProtectionAvailable)
                {
                    State = MobileAccountSessionState.NotConfigured;
                    LastSafeErrorCode = "auth.protected_storage_unavailable";
                    return gate.Snapshot;
                }

                try
                {
                    ProtectedRefreshTokenRecord stored = await refreshTokenStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                    if (stored != null)
                    {
                        ValidateStoredRecord(stored);
                        if (stored.RefreshTokenExpiresUtc <= clock.UtcNow)
                            await DeleteProtectedRecordBestEffortAsync(cancellationToken).ConfigureAwait(false);
                        else
                            refreshRecord = stored;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    await DeleteProtectedRecordBestEffortAsync(CancellationToken.None).ConfigureAwait(false);
                    State = MobileAccountSessionState.Faulted;
                    LastSafeErrorCode = "auth.protected_storage_read_failed";
                    gate.Apply(AccountSessionReadinessSnapshot.Unavailable(LastSafeErrorCode));
                    return gate.Snapshot;
                }

                gate.ConfigureTransport(true);
                gate.Apply(AccountSessionReadinessSnapshot.Checking());
                RemoteAccountSessionReadiness remote;
                try
                {
                    remote = await transport.ReadReadinessAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    State = MobileAccountSessionState.Faulted;
                    LastSafeErrorCode = "auth.readiness_unavailable";
                    gate.Apply(AccountSessionReadinessSnapshot.Unavailable(LastSafeErrorCode));
                    return gate.Snapshot;
                }

                try
                {
                    ValidateReadiness(remote);
                }
                catch (MobileAccountSessionException exception)
                {
                    State = MobileAccountSessionState.Faulted;
                    LastSafeErrorCode = exception.SafeCode;
                    gate.Apply(AccountSessionReadinessSnapshot.Unavailable(exception.SafeCode));
                    return gate.Snapshot;
                }
                AccountSessionReadinessSnapshot snapshot = AccountSessionReadinessSnapshot.FromServer(
                    remote.AccountCreationAllowed,
                    remote.SessionCreationAllowed,
                    remote.TokenIssuanceAllowed,
                    remote.Claims.LiveAccounts,
                    remote.Claims.LiveSessions);
                gate.Apply(snapshot);
                ServerGameplayAuthorityGranted = remote.Claims.GameplayAuthorityGranted;
                State = MobileAccountSessionState.SignedOut;
                return snapshot;
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public Task<GameAccountSession> LoginAsync(
            MobileAccountLoginRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateLoginRequest(request);
            return CompleteLoginAsync(ct => transport.LoginAsync(request, ct), cancellationToken);
        }

        public Task<GameAccountSession> LoginWithGoogleAsync(
            GoogleLoginRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AuthorizationCode) || string.IsNullOrWhiteSpace(request.CodeVerifier) ||
                string.IsNullOrWhiteSpace(request.RedirectUri) || string.IsNullOrWhiteSpace(request.ClientVersion) ||
                string.IsNullOrWhiteSpace(request.DeviceIdentifier) || string.IsNullOrWhiteSpace(request.Region))
                throw new MobileAccountSessionException(MobileAccountSessionError.InvalidRequest, "auth.invalid_request");
            return CompleteLoginAsync(ct => transport.LoginWithGoogleAsync(request, ct), cancellationToken);
        }

        private async Task<GameAccountSession> CompleteLoginAsync(
            Func<CancellationToken, Task<RemoteMobileLoginResult>> transportCall,
            CancellationToken cancellationToken)
        {
            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                RequireLoginReady();
                if (activeSession != null || refreshRecord != null)
                    await LogoutInsideLockAsync(cancellationToken, failWhenRemoteLogoutFails: true).ConfigureAwait(false);

                State = MobileAccountSessionState.Authenticating;
                LastSafeErrorCode = string.Empty;
                RemoteMobileLoginResult result;
                try
                {
                    result = await transportCall(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    State = MobileAccountSessionState.SignedOut;
                    throw;
                }
                catch
                {
                    throw Fail(MobileAccountSessionError.TransportFailure, "auth.transport_failure");
                }

                if (result == null || !result.Succeeded)
                    throw Fail(MobileAccountSessionError.AuthenticationRejected, SafeAuthenticationCode(result == null ? null : result.ErrorCode));
                ValidateLoginResult(result);

                ProtectedRefreshTokenRecord pending = new ProtectedRefreshTokenRecord(
                    result.PlayerId,
                    result.AccountId,
                    result.Session.SessionId,
                    result.Tokens.RefreshToken,
                    result.Tokens.RefreshTokenExpiresUtc);
                try
                {
                    await SaveAndVerifyAsync(pending, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    await RevokeIssuedSessionBestEffortAsync(result.Tokens.AccessToken).ConfigureAwait(false);
                    throw;
                }
                PublishSession(result.PlayerId, result.AccountId, result.Session.SessionId, result.Tokens, result.IsNewAccount, result.DisplayName, result.IsOnboarded);
                return activeSession;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MobileAccountSessionException exception)
            {
                ClearAccessTokenMemory();
                LastSafeErrorCode = exception.SafeCode;
                if (State != MobileAccountSessionState.SignedOut) State = MobileAccountSessionState.Faulted;
                throw;
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public async Task<GameAccountSession> RestoreOrRefreshAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                RequireLoginReady();
                ProtectedRefreshTokenRecord stored = refreshRecord;
                if (stored == null)
                {
                    try
                    {
                        stored = await refreshTokenStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        throw Fail(MobileAccountSessionError.ProtectedStorageFailure, "auth.protected_storage_read_failed");
                    }
                }

                if (stored == null)
                {
                    ClearAccessTokenMemory();
                    State = MobileAccountSessionState.SignedOut;
                    LastSafeErrorCode = string.Empty;
                    return null;
                }

                ValidateStoredRecord(stored);
                refreshRecord = stored;
                if (stored.RefreshTokenExpiresUtc <= clock.UtcNow)
                {
                    await DeleteProtectedRecordBestEffortAsync(cancellationToken).ConfigureAwait(false);
                    ClearAccessTokenMemory();
                    State = MobileAccountSessionState.Expired;
                    throw Fail(MobileAccountSessionError.SessionExpired, "auth.session_expired", MobileAccountSessionState.Expired);
                }

                State = MobileAccountSessionState.Refreshing;
                RemoteMobileTokenPair rotated;
                try
                {
                    rotated = await transport.RefreshAsync(stored.RefreshToken, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (MobileAccountSessionException exception)
                {
                    throw Fail(
                        exception.Error,
                        exception.SafeCode,
                        exception.Error == MobileAccountSessionError.SessionExpired
                            ? MobileAccountSessionState.Expired
                            : MobileAccountSessionState.Faulted);
                }
                catch
                {
                    throw Fail(MobileAccountSessionError.TransportFailure, "auth.refresh_failed");
                }

                ValidateTokenPair(rotated);
                if (rotated.PlayerId != stored.PlayerId || !string.Equals(rotated.SessionId, stored.SessionId, StringComparison.Ordinal))
                    throw Fail(MobileAccountSessionError.InvalidResponse, "auth.refresh_identity_mismatch");
                if (string.Equals(rotated.RefreshToken, stored.RefreshToken, StringComparison.Ordinal))
                    throw Fail(MobileAccountSessionError.InvalidResponse, "auth.refresh_not_rotated");

                ProtectedRefreshTokenRecord replacement = new ProtectedRefreshTokenRecord(
                    stored.PlayerId,
                    stored.AccountId,
                    stored.SessionId,
                    rotated.RefreshToken,
                    rotated.RefreshTokenExpiresUtc);
                try
                {
                    await SaveAndVerifyAsync(replacement, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    await RevokeIssuedSessionBestEffortAsync(rotated.AccessToken).ConfigureAwait(false);
                    throw;
                }
                PublishSession(stored.PlayerId, stored.AccountId, stored.SessionId, rotated);
                return activeSession;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MobileAccountSessionException exception)
            {
                ClearAccessTokenMemory();
                LastSafeErrorCode = exception.SafeCode;
                if (exception.Error == MobileAccountSessionError.InvalidResponse ||
                    exception.Error == MobileAccountSessionError.SessionExpired)
                    await DeleteProtectedRecordBestEffortAsync(cancellationToken).ConfigureAwait(false);
                if (exception.Error == MobileAccountSessionError.TransportFailure)
                    State = MobileAccountSessionState.Offline;
                else if (State != MobileAccountSessionState.Expired)
                    State = MobileAccountSessionState.Faulted;
                throw;
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await LogoutInsideLockAsync(cancellationToken, failWhenRemoteLogoutFails: true).ConfigureAwait(false);
            }
            finally
            {
                lifecycle.Release();
            }
        }

        public void MarkNetworkUnavailable(string safeCode = "auth.network_unavailable")
        {
            if (activeSession == null || State == MobileAccountSessionState.SignedOut ||
                State == MobileAccountSessionState.Expired || State == MobileAccountSessionState.NotConfigured)
                return;

            State = MobileAccountSessionState.Offline;
            LastSafeErrorCode = string.IsNullOrWhiteSpace(safeCode) ? "auth.network_unavailable" : safeCode;
        }

        public bool TryGetSession(out GameAccountSession session)
        {
            GameAccountSession current = activeSession;
            if (State != MobileAccountSessionState.Authenticated || current == null ||
                current.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(current.AccessToken) ||
                current.AccessToken.Length > MaxTokenCharacters || accessTokenExpiresUtc <= clock.UtcNow)
            {
                session = null;
                return false;
            }

            session = current;
            return true;
        }

        public bool TryGetKnownPlayerId(out Guid playerId)
        {
            GameAccountSession current = activeSession;
            if (current != null && current.PlayerId != Guid.Empty)
            {
                playerId = current.PlayerId;
                return true;
            }
            ProtectedRefreshTokenRecord stored = refreshRecord;
            if (stored != null && stored.PlayerId != Guid.Empty)
            {
                playerId = stored.PlayerId;
                return true;
            }
            playerId = Guid.Empty;
            return false;
        }

        public async Task<GameAccountSession> GetFreshSessionAsync(CancellationToken cancellationToken)
        {
            GameAccountSession current;
            if (TryGetSession(out current)) return current;
            await gameRefresh.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (TryGetSession(out current)) return current;
                return await RestoreOrRefreshAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                gameRefresh.Release();
            }
        }

        public async Task<GameAccountSession> RefreshAfterUnauthorizedAsync(
            string rejectedAccessToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(rejectedAccessToken) || rejectedAccessToken.Length > MaxTokenCharacters)
                throw new MobileAccountSessionException(MobileAccountSessionError.SessionExpired, "auth.session_expired");
            await gameRefresh.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                GameAccountSession current;
                if (TryGetSession(out current) &&
                    !string.Equals(current.AccessToken, rejectedAccessToken, StringComparison.Ordinal))
                    return current;
                return await RestoreOrRefreshAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                gameRefresh.Release();
            }
        }

        public async Task InvalidateUnauthorizedSessionAsync(
            string rejectedAccessToken,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await gameRefresh.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    GameAccountSession current = activeSession;
                    if (current != null && !string.Equals(current.AccessToken, rejectedAccessToken, StringComparison.Ordinal))
                        return;
                    ClearAccessTokenMemory();
                    await DeleteProtectedRecordBestEffortAsync(CancellationToken.None).ConfigureAwait(false);
                    State = MobileAccountSessionState.Expired;
                    LastSafeErrorCode = "auth.session_expired";
                }
                finally
                {
                    lifecycle.Release();
                }
            }
            finally
            {
                gameRefresh.Release();
            }
        }

        public IReadOnlyList<string> ProofRows()
        {
            return new[]
            {
                "mobile_account_state:" + State,
                "mobile_account_player_present:" + (ActivePlayerId != Guid.Empty).ToString().ToLowerInvariant(),
                "mobile_account_session_present:" + (!string.IsNullOrEmpty(activeSessionId)).ToString().ToLowerInvariant(),
                "server_gameplay_authority_granted:" + ServerGameplayAuthorityGranted.ToString().ToLowerInvariant(),
                "access_token_storage:memory_only",
                "refresh_token_storage:protected_store_only",
                "refresh_token_rotation_required:true",
                "game_access_refresh_deduplicated:true",
                "game_access_unauthorized_retry_budget:one",
                "game_second_unauthorized_purges_session:true",
                "password_retained_by_client:false",
                "logout_clears_local_even_on_remote_failure:true",
                "player_switch_closes_previous_session:true",
                "plaintext_player_prefs_for_tokens:false",
                "last_safe_error_code:" + (string.IsNullOrEmpty(LastSafeErrorCode) ? "none" : LastSafeErrorCode)
            };
        }

        private async Task LogoutInsideLockAsync(CancellationToken cancellationToken, bool failWhenRemoteLogoutFails)
        {
            string accessToken = activeSession == null ? string.Empty : activeSession.AccessToken;
            bool remoteFailed = false;
            bool canceled = false;
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                try
                {
                    await transport.LogoutAsync(accessToken, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    remoteFailed = true;
                    canceled = true;
                }
                catch
                {
                    remoteFailed = true;
                }
            }

            try
            {
                await refreshTokenStore.DeleteAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                ClearAccessTokenMemory();
                refreshRecord = null;
                State = MobileAccountSessionState.Faulted;
                LastSafeErrorCode = "auth.protected_storage_delete_failed";
                throw new MobileAccountSessionException(
                    MobileAccountSessionError.ProtectedStorageFailure,
                    LastSafeErrorCode);
            }

            ClearAccessTokenMemory();
            refreshRecord = null;
            State = MobileAccountSessionState.SignedOut;
            LastSafeErrorCode = remoteFailed ? "auth.remote_logout_failed" : string.Empty;
            if (canceled) throw new OperationCanceledException(cancellationToken);
            if (remoteFailed && failWhenRemoteLogoutFails)
                throw new MobileAccountSessionException(MobileAccountSessionError.RemoteLogoutFailure, LastSafeErrorCode);
        }

        private async Task RevokeIssuedSessionBestEffortAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken)) return;
            try
            {
                await transport.LogoutAsync(accessToken, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private async Task SaveAndVerifyAsync(ProtectedRefreshTokenRecord pending, CancellationToken cancellationToken)
        {
            if (!refreshTokenStore.IsProtectionAvailable)
                throw Fail(MobileAccountSessionError.ProtectedStorageUnavailable, "auth.protected_storage_unavailable");
            try
            {
                await refreshTokenStore.SaveAsync(pending, cancellationToken).ConfigureAwait(false);
                ProtectedRefreshTokenRecord verified = await refreshTokenStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                if (!SameProtectedRecord(pending, verified))
                    throw new InvalidOperationException("Protected refresh record verification failed.");
                refreshRecord = verified;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MobileAccountSessionException)
            {
                throw;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning("Bee Kingdom protected refresh record save/verify failed (" + exception.GetType().FullName + "): " + exception.Message);
                await DeleteProtectedRecordBestEffortAsync(cancellationToken).ConfigureAwait(false);
                throw Fail(MobileAccountSessionError.ProtectedStorageFailure, "auth.protected_storage_write_failed");
            }
        }

        private async Task DeleteProtectedRecordBestEffortAsync(CancellationToken cancellationToken)
        {
            try
            {
                await refreshTokenStore.DeleteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }
            refreshRecord = null;
        }

        private void PublishSession(
            Guid playerId,
            Guid accountId,
            string sessionId,
            RemoteMobileTokenPair tokens,
            bool isNewAccount = false,
            string displayName = null,
            bool isOnboarded = false)
        {
            activeSession = new GameAccountSession(playerId, tokens.AccessToken, isNewAccount, displayName, isOnboarded);
            activeAccountId = accountId;
            activeSessionId = sessionId;
            accessTokenExpiresUtc = tokens.AccessTokenExpiresUtc;
            State = MobileAccountSessionState.Authenticated;
            LastSafeErrorCode = string.Empty;
        }

        private void ClearAccessTokenMemory()
        {
            activeSession = null;
            activeAccountId = Guid.Empty;
            activeSessionId = string.Empty;
            accessTokenExpiresUtc = default(DateTimeOffset);
        }

        private void RequireLoginReady()
        {
            if (!gate.CanSubmitLogin || !refreshTokenStore.IsProtectionAvailable)
                throw Fail(MobileAccountSessionError.NotConfigured, "auth.not_configured", MobileAccountSessionState.NotConfigured);
        }

        private MobileAccountSessionException Fail(
            MobileAccountSessionError error,
            string code,
            MobileAccountSessionState state = MobileAccountSessionState.Faulted)
        {
            State = state;
            LastSafeErrorCode = code ?? string.Empty;
            return new MobileAccountSessionException(error, LastSafeErrorCode);
        }

        private static void ValidateReadiness(RemoteAccountSessionReadiness readiness)
        {
            if (readiness == null || !IsUtc(readiness.ServerTimeUtc) || readiness.Claims == null ||
                readiness.SecretsAllowedInResponse || readiness.Blockers == null)
                throw new MobileAccountSessionException(MobileAccountSessionError.InvalidResponse, "auth.readiness_invalid");
        }

        private static void ValidateLoginRequest(MobileAccountLoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > MaxEmailCharacters ||
                request.Email.IndexOf('@') <= 0 || string.IsNullOrEmpty(request.Password) || request.Password.Length > MaxPasswordCharacters ||
                string.IsNullOrWhiteSpace(request.ClientVersion) || request.ClientVersion.Length > MaxMetadataCharacters ||
                string.IsNullOrWhiteSpace(request.DeviceIdentifier) || request.DeviceIdentifier.Length > MaxMetadataCharacters ||
                string.IsNullOrWhiteSpace(request.Region) || request.Region.Length > MaxMetadataCharacters)
                throw new MobileAccountSessionException(MobileAccountSessionError.InvalidRequest, "auth.invalid_request");
        }

        private void ValidateLoginResult(RemoteMobileLoginResult result)
        {
            if (result.PlayerId == Guid.Empty || result.AccountId == Guid.Empty || result.Session == null ||
                result.Session.PlayerId != result.PlayerId || result.Session.AccountId != result.AccountId ||
                result.Session.IsRevoked || string.IsNullOrWhiteSpace(result.Session.SessionId) ||
                result.Session.SessionId.Length > MaxSessionIdCharacters || !IsUtc(result.Session.LoginUtc) ||
                !IsUtc(result.Session.ExpirationUtc) || result.Session.ExpirationUtc <= result.Session.LoginUtc)
                throw new MobileAccountSessionException(MobileAccountSessionError.InvalidResponse, "auth.login_response_invalid");
            ValidateTokenPair(result.Tokens);
            if (result.Tokens.PlayerId != result.PlayerId ||
                !string.Equals(result.Tokens.SessionId, result.Session.SessionId, StringComparison.Ordinal))
                throw new MobileAccountSessionException(MobileAccountSessionError.InvalidResponse, "auth.token_identity_mismatch");
            if (result.Tokens.RefreshTokenExpiresUtc > result.Session.ExpirationUtc)
                throw new MobileAccountSessionException(MobileAccountSessionError.InvalidResponse, "auth.session_expiration_invalid");
        }

        private void ValidateTokenPair(RemoteMobileTokenPair tokens)
        {
            if (tokens == null || string.IsNullOrWhiteSpace(tokens.AccessToken) ||
                tokens.AccessToken.Length > MaxTokenCharacters || string.IsNullOrWhiteSpace(tokens.RefreshToken) ||
                tokens.RefreshToken.Length > MaxTokenCharacters || string.Equals(tokens.AccessToken, tokens.RefreshToken, StringComparison.Ordinal) ||
                !IsUtc(tokens.AccessTokenExpiresUtc) || !IsUtc(tokens.RefreshTokenExpiresUtc) ||
                tokens.AccessTokenExpiresUtc <= clock.UtcNow || tokens.AccessTokenExpiresUtc >= tokens.RefreshTokenExpiresUtc ||
                tokens.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(tokens.SessionId) ||
                tokens.SessionId.Length > MaxSessionIdCharacters)
                throw new MobileAccountSessionException(MobileAccountSessionError.InvalidResponse, "auth.token_response_invalid");
        }

        private static void ValidateStoredRecord(ProtectedRefreshTokenRecord record)
        {
            if (record == null || record.PlayerId == Guid.Empty || record.AccountId == Guid.Empty ||
                string.IsNullOrWhiteSpace(record.SessionId) || record.SessionId.Length > MaxSessionIdCharacters ||
                string.IsNullOrWhiteSpace(record.RefreshToken) || record.RefreshToken.Length > MaxTokenCharacters ||
                !IsUtc(record.RefreshTokenExpiresUtc))
                throw new MobileAccountSessionException(MobileAccountSessionError.SessionExpired, "auth.session_missing");
        }

        private static bool SameProtectedRecord(ProtectedRefreshTokenRecord expected, ProtectedRefreshTokenRecord actual)
        {
            return expected != null && actual != null && expected.PlayerId == actual.PlayerId &&
                expected.AccountId == actual.AccountId && string.Equals(expected.SessionId, actual.SessionId, StringComparison.Ordinal) &&
                string.Equals(expected.RefreshToken, actual.RefreshToken, StringComparison.Ordinal) &&
                expected.RefreshTokenExpiresUtc == actual.RefreshTokenExpiresUtc;
        }

        private static bool IsUtc(DateTimeOffset value)
        {
            return value != default(DateTimeOffset) && value.Offset == TimeSpan.Zero;
        }

        private static string SafeAuthenticationCode(string remoteCode)
        {
            if (string.Equals(remoteCode, "auth.invalid_credentials", StringComparison.Ordinal) ||
                string.Equals(remoteCode, "auth.rate_limited", StringComparison.Ordinal) ||
                string.Equals(remoteCode, "auth.session_limit", StringComparison.Ordinal) ||
                string.Equals(remoteCode, "auth.unavailable", StringComparison.Ordinal) ||
                string.Equals(remoteCode, "auth.invalid_request", StringComparison.Ordinal))
                return remoteCode;
            if (string.Equals(remoteCode, "invalid_credentials", StringComparison.Ordinal) ||
                string.Equals(remoteCode, "account_locked", StringComparison.Ordinal) ||
                string.Equals(remoteCode, "account_disabled", StringComparison.Ordinal) ||
                string.Equals(remoteCode, "client_version_unsupported", StringComparison.Ordinal))
                return "auth." + remoteCode;
            return "auth.rejected";
        }
    }
}
