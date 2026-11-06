using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class AllianceHeadquartersClient
    {
        private readonly MobileAccountSessionGate gate;
        private readonly IGameAccountSessionSource sessions;
        private readonly IAuthenticatedGameRestTransport transport;

        public AllianceHeadquartersClient(MobileAccountSessionGate gate, IGameAccountSessionSource sessions, IAuthenticatedGameRestTransport transport)
        { this.gate = gate; this.sessions = sessions; this.transport = transport; }

        public Guid CurrentPlayerId
        {
            get { GameAccountSession session; return sessions.TryGetSession(out session) && session != null ? session.PlayerId : Guid.Empty; }
        }

        public async Task<RemoteHeadquartersResponse> ReadAsync(CancellationToken ct)
        {
            if (!gate.CanSubmitLogin) throw new InvalidOperationException("territory_session_unavailable");
            GameAccountSession session = null;
            var refreshable = sessions as IRefreshableGameAccountSessionSource;
            if (refreshable != null) session = await refreshable.GetFreshSessionAsync(ct);
            else sessions.TryGetSession(out session);
            if (session == null || session.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(session.AccessToken))
                throw new InvalidOperationException("territory_session_required");
            var response = await transport.SendAsync<RemoteHeadquartersResponse>(
                new AuthenticatedGameRestRequest("GET", "/alliance/v1/territory/headquarters/mine"), session.AccessToken, ct);
            ct.ThrowIfCancellationRequested();
            GameAccountSession current;
            if (!sessions.TryGetSession(out current) || current == null || current.PlayerId != session.PlayerId
                || current.AccessToken != session.AccessToken || response == null || response.PlayerId != session.PlayerId)
                throw new InvalidOperationException("territory_session_changed");
            return response;
        }
    }
}
