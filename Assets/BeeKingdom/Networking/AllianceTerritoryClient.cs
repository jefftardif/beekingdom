using System;
using System.Threading;
using System.Threading.Tasks;
namespace BeeKingdom.Networking
{
    /// <summary>Read only; no offline outlines, no demo identities, no mutation retry.</summary>
    public sealed class AllianceTerritoryClient
    {
        private readonly MobileAccountSessionGate gate;
        private readonly IGameAccountSessionSource sessions;
        private readonly IAuthenticatedGameRestTransport transport;
        public AllianceTerritoryClient(MobileAccountSessionGate gate, IGameAccountSessionSource sessions, IAuthenticatedGameRestTransport transport)
        { this.gate=gate; this.sessions=sessions; this.transport=transport; }
        public async Task<RemoteTerritoryResponse> ReadAsync(string world, string surface, string unit,
            Guid minimumAllianceId,long minimumRevision,CancellationToken ct)
        {
            if (!gate.CanSubmitLogin) throw new InvalidOperationException("territory_session_unavailable");
            GameAccountSession session=null;
            var refreshable=sessions as IRefreshableGameAccountSessionSource;
            if (refreshable!=null) session=await refreshable.GetFreshSessionAsync(ct);
            else sessions.TryGetSession(out session);
            if (session==null || session.PlayerId==Guid.Empty || string.IsNullOrWhiteSpace(session.AccessToken))
                throw new InvalidOperationException("territory_session_required");
            var result=await transport.SendAsync<RemoteTerritoryResponse>(new AuthenticatedGameRestRequest("GET",
                "/alliance/v1/territory/mine?worldId="+Uri.EscapeDataString(world)+"&surfaceId="+Uri.EscapeDataString(surface)
                +"&minimumAllianceId="+minimumAllianceId.ToString("D")+"&minimumRevision="+minimumRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)),session.AccessToken,ct);
            GameAccountSession current;
            if (!sessions.TryGetSession(out current) || current==null || current.PlayerId!=session.PlayerId)
                throw new InvalidOperationException("territory_session_changed");
            if (!TerritoryOutlineValidation.ValidResponse(result,session.PlayerId,world,surface,unit))
                throw new InvalidOperationException("territory_response_invalid");
            // Conservative display freshness policy; clock skew hides the outline
            // rather than silently reviving stale possession. No gameplay mutation.
            if(result.ServerTimeUtc<DateTimeOffset.UtcNow.AddSeconds(-30)
                || result.ServerTimeUtc>DateTimeOffset.UtcNow.AddSeconds(5))
                throw new InvalidOperationException("territory_response_stale");
            return result;
        }
    }
}
