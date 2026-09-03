using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // M043B-CL: the generic, reusable player-search client the M043 report identified as entirely
    // missing - deliberately NOT Alliance-specific (mirrors the server-side split: this wraps
    // /game/v1/players/search, not any /alliance/v1/* route). Communication/Friends/mail-recipient
    // selection can construct their own PlayerDirectoryClient later using this exact same pattern -
    // AllianceCenterPanelController only ever calls it through IPlayerDirectoryClient, never
    // duplicates search logic inside AllianceClient.

    [Serializable]
    public sealed class RemotePlayerPublicIdentity
    {
        public Guid PlayerId { get; set; }
        public string DisplayName { get; set; }
    }

    public interface IPlayerDirectoryClient
    {
        Task<List<RemotePlayerPublicIdentity>> SearchAsync(string query, int offset, int limit, CancellationToken cancellationToken = default);
    }

    public sealed class PlayerDirectoryClient : IPlayerDirectoryClient
    {
        private const string BasePath = "/game/v1/players";
        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public PlayerDirectoryClient(
            MobileAccountSessionGate sessionGate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<List<RemotePlayerPublicIdentity>> SearchAsync(string query, int offset, int limit, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                throw new HivePerimeterClientException(HivePerimeterClientError.InvalidRequest, "The search query must contain at least 2 characters.");
            string path = BasePath + "/search?q=" + Uri.EscapeDataString(query.Trim()) + "&offset=" + offset + "&limit=" + limit;
            return SendAsync<List<RemotePlayerPublicIdentity>>("GET", path, cancellationToken);
        }

        // ---------------- plumbing (mirrors AllianceClient/HiveResearchClient) ----------------

        private async Task<T> SendAsync<T>(string method, string path, CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(method, path);
            return await SendWithSingleAuthenticationRefreshAsync<T>(request, context, cancellationToken).ConfigureAwait(false);
        }

        private async Task<SessionContext> RequireSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new HivePerimeterClientException(HivePerimeterClientError.NotConfigured, "Official account session transport is not ready.");

            var refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable != null)
            {
                try { return RequireUsableSession(await refreshable.GetFreshSessionAsync(cancellationToken).ConfigureAwait(false)); }
                catch (OperationCanceledException) { throw; }
                catch (MobileAccountSessionException exception) { throw MapSessionFailure(exception); }
            }

            GameAccountSession session;
            if (!sessionSource.TryGetSession(out session)) session = null;
            return RequireUsableSession(session);
        }

        private async Task<T> SendWithSingleAuthenticationRefreshAsync<T>(AuthenticatedGameRestRequest request, SessionContext context, CancellationToken cancellationToken)
        {
            try
            {
                return await transport.SendAsync<T>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error != AuthenticatedGameRestError.Unauthorized) throw MapTransportFailure(exception);
            }

            var refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable == null)
                throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "The game session was rejected.");

            GameAccountSession replacement;
            try { replacement = await refreshable.RefreshAfterUnauthorizedAsync(context.AccessToken, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { throw; }
            catch (MobileAccountSessionException exception) { throw MapSessionFailure(exception); }

            if (replacement == null || replacement.PlayerId != context.PlayerId ||
                string.IsNullOrWhiteSpace(replacement.AccessToken) || replacement.AccessToken.Length > 8192)
            {
                await refreshable.InvalidateUnauthorizedSessionAsync(context.AccessToken, cancellationToken).ConfigureAwait(false);
                throw new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, "The refreshed game session changed identity.");
            }

            try
            {
                return await transport.SendAsync<T>(request, replacement.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                {
                    await refreshable.InvalidateUnauthorizedSessionAsync(replacement.AccessToken, cancellationToken).ConfigureAwait(false);
                    throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "The refreshed game session was rejected.");
                }
                throw MapTransportFailure(exception);
            }
        }

        private static SessionContext RequireUsableSession(GameAccountSession session)
        {
            if (session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static HivePerimeterClientException MapSessionFailure(MobileAccountSessionException exception)
        {
            if (exception.Error == MobileAccountSessionError.TransportFailure)
                return new HivePerimeterClientException(HivePerimeterClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == MobileAccountSessionError.NotConfigured)
                return new HivePerimeterClientException(HivePerimeterClientError.NotConfigured, exception.SafeCode);
            return new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, exception.SafeCode);
        }

        private static HivePerimeterClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure)
                return new HivePerimeterClientException(HivePerimeterClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                return new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, exception.SafeCode);
            return new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, exception.SafeCode);
        }

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
