using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // Monde vivant (demande de Jeff, 2026-08-01) : presence ambiante uniquement - lecture seule,
    // aucune mutation, aucune interaction. Reutilise le meme patron d'authentification que les
    // autres clients de jeu, simplifie puisqu'il n'y a ici ni idempotence ni ecriture.
    public enum WorldPresenceClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class WorldPresenceClientException : Exception
    {
        public WorldPresenceClientException(WorldPresenceClientError error, string message) : base(message) { Error = error; }
        public WorldPresenceClientError Error { get; }
    }

    public sealed class RemoteWorldPresenceSighting
    {
        public Guid HiveId { get; set; }
        public string ColonyLabel { get; set; }
        public string NodeId { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndsAtUtc { get; set; }
    }

    public sealed class RemoteWorldPresenceSnapshot
    {
        public DateTimeOffset ServerTimeUtc { get; set; }
        public List<RemoteWorldPresenceSighting> Sightings { get; set; }
    }

    public interface IWorldPresenceClient
    {
        Task<RemoteWorldPresenceSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
    }

    public sealed class WorldPresenceClient : IWorldPresenceClient
    {
        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public WorldPresenceClient(MobileAccountSessionGate sessionGate, IGameAccountSessionSource sessionSource, IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<RemoteWorldPresenceSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            if (hiveId == Guid.Empty) throw new WorldPresenceClientException(WorldPresenceClientError.InvalidRequest, "A hive identifier is required.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new WorldPresenceClientException(WorldPresenceClientError.NotConfigured, "Official account session transport is not ready.");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new WorldPresenceClientException(WorldPresenceClientError.AuthenticationRequired, "An official account session is required.");

            RemoteWorldPresenceSnapshot response;
            try
            {
                response = await transport.SendAsync<RemoteWorldPresenceSnapshot>(
                    new AuthenticatedGameRestRequest("GET", BoardPath(hiveId)), session.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                throw MapTransportFailure(exception);
            }
            if (response == null) throw new WorldPresenceClientException(WorldPresenceClientError.InvalidResponse, "The world presence response is empty.");
            response.Sightings ??= new List<RemoteWorldPresenceSighting>();
            return response;
        }

        private static WorldPresenceClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure) return new WorldPresenceClientException(WorldPresenceClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized) return new WorldPresenceClientException(WorldPresenceClientError.AuthenticationRequired, exception.SafeCode);
            return new WorldPresenceClientException(WorldPresenceClientError.InvalidResponse, exception.SafeCode);
        }

        public static string BoardPath(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/world-presence";
    }
}
