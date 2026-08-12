using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // Premiere boucle "sortir de la ruche" : le joueur envoie ses abeilles collecter un noeud de
    // ressource visible sur la carte du monde, attend le vol, puis valide le gain. Meme patron
    // simple (pas de file d'attente hors-ligne) que CombatPatrolClient - une action discretionnaire
    // et peu frequente, pas une mutation qui doit survivre a une coupure reseau prolongee.
    public enum WorldResourceCollectionClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class WorldResourceCollectionClientException : Exception
    {
        public WorldResourceCollectionClientException(WorldResourceCollectionClientError error, string message) : base(message) { Error = error; }
        public WorldResourceCollectionClientError Error { get; }
    }

    public sealed class WorldResourceCollectionMutationRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    // Escouade reellement engagee (demande de Jeff, 2026-08-01) : premiere brique de
    // l'architecture de deploiement reutilisable plus tard (PvP, raids, renforts, occupation de
    // points d'interet) - miroir du meme patron deja utilise par CombatPatrolLaunchRequest.
    public sealed class WorldResourceCollectionLaunchMutationRequest
    {
        public long Guardians { get; set; }
        public long Wingrunners { get; set; }
        public long Darters { get; set; }
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteWorldResourceNode
    {
        public string NodeId { get; set; }
        public string ResourceKey { get; set; }
        public string Tier { get; set; }
        public long Yield { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan Cooldown { get; set; }
        public string Label { get; set; }
        public bool Ready { get; set; }
        public DateTimeOffset? ReadyAtUtc { get; set; }
        public bool CanLaunch { get; set; }
        public bool IsDailyFocus { get; set; }
        public bool IsWorldEventBoosted { get; set; }
    }

    public sealed class RemoteWorldResourceActiveFlight
    {
        public Guid FlightId { get; set; }
        public string NodeId { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndsAtUtc { get; set; }
        public Dictionary<string, long> CommittedTroops { get; set; }
    }

    public sealed class RemoteWorldResourceClaimReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public Guid FlightId { get; set; }
        public string NodeId { get; set; }
        public string ResourceKey { get; set; }
        public long CreditedAmount { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public RemoteHiveResourceBalance ResultingBalance { get; set; }
        public bool DailyFocusApplied { get; set; }
        public bool WorldEventApplied { get; set; }
        public string WorldEventKey { get; set; }
    }

    public sealed class RemoteWorldResourceCollectionSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public List<RemoteWorldResourceNode> Nodes { get; set; }
        public RemoteWorldResourceActiveFlight Active { get; set; }
        public RemoteWorldResourceClaimReceipt ClaimReceipt { get; set; }
        public string FeaturedNodeId { get; set; }
        public RemoteActiveWorldEvent WorldEvent { get; set; }
        public Dictionary<string, long> AvailableRoster { get; set; }
    }

    public interface IWorldResourceCollectionClient
    {
        Task<RemoteWorldResourceCollectionSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
        Task<RemoteWorldResourceCollectionSnapshot> LaunchAsync(Guid hiveId, string nodeId, long guardians, long wingrunners, long darters, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<RemoteWorldResourceCollectionSnapshot> ClaimAsync(Guid hiveId, Guid flightId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<RemoteWorldResourceCollectionSnapshot> RecallAsync(Guid hiveId, Guid flightId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    }

    public sealed class WorldResourceCollectionClient : IWorldResourceCollectionClient
    {
        public const string ContractVersion = "living-hive-world-resource-collection-v1";

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public WorldResourceCollectionClient(MobileAccountSessionGate sessionGate, IGameAccountSessionSource sessionSource, IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<RemoteWorldResourceCollectionSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("GET", BoardPath(hiveId)), cancellationToken);
        }

        public Task<RemoteWorldResourceCollectionSnapshot> LaunchAsync(Guid hiveId, string nodeId, long guardians, long wingrunners, long darters, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireNodeId(nodeId);
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            var request = new AuthenticatedGameRestRequest("POST", LaunchPath(hiveId, nodeId), new WorldResourceCollectionLaunchMutationRequest
            {
                Guardians = Math.Max(0, guardians),
                Wingrunners = Math.Max(0, wingrunners),
                Darters = Math.Max(0, darters),
                ExpectedRevision = expectedRevision,
                IdempotencyKey = idempotencyKey
            });
            return SendAsync(hiveId, request, cancellationToken);
        }

        public Task<RemoteWorldResourceCollectionSnapshot> ClaimAsync(Guid hiveId, Guid flightId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            if (flightId == Guid.Empty) throw InvalidRequest("A flight identifier is required.");
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            var request = new AuthenticatedGameRestRequest("POST", ClaimPath(hiveId, flightId), new WorldResourceCollectionMutationRequest { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            return SendAsync(hiveId, request, cancellationToken);
        }

        public Task<RemoteWorldResourceCollectionSnapshot> RecallAsync(Guid hiveId, Guid flightId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            if (flightId == Guid.Empty) throw InvalidRequest("A flight identifier is required.");
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            var request = new AuthenticatedGameRestRequest("POST", RecallPath(hiveId, flightId), new WorldResourceCollectionMutationRequest { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            return SendAsync(hiveId, request, cancellationToken);
        }

        private async Task<RemoteWorldResourceCollectionSnapshot> SendAsync(Guid hiveId, AuthenticatedGameRestRequest request, CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            RemoteWorldResourceCollectionSnapshot response;
            try
            {
                response = await transport.SendAsync<RemoteWorldResourceCollectionSnapshot>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                throw MapTransportFailure(exception);
            }
            ValidateSnapshot(response, context.PlayerId, hiveId);
            return response;
        }

        private async Task<SessionContext> RequireSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new WorldResourceCollectionClientException(WorldResourceCollectionClientError.NotConfigured, "Official account session transport is not ready.");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new WorldResourceCollectionClientException(WorldResourceCollectionClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static void ValidateSnapshot(RemoteWorldResourceCollectionSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null) throw InvalidResponse("The world resource collection response is empty.");
            if (snapshot.PlayerId != playerId || snapshot.HiveId != hiveId) throw InvalidResponse("The world resource collection response belongs to another session or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal)) throw InvalidResponse("The world resource collection contract version is unsupported.");
            if (snapshot.Revision < 0) throw InvalidResponse("The world resource collection revision is invalid.");
            if (snapshot.Nodes != null)
            {
                var seen = new HashSet<string>();
                foreach (RemoteWorldResourceNode node in snapshot.Nodes)
                    if (node == null || string.IsNullOrWhiteSpace(node.NodeId) || string.IsNullOrWhiteSpace(node.ResourceKey) || node.Yield <= 0 || !seen.Add(node.NodeId))
                        throw InvalidResponse("A world resource node is inconsistent.");
            }
            if (snapshot.Active != null && (snapshot.Active.FlightId == Guid.Empty || string.IsNullOrWhiteSpace(snapshot.Active.NodeId) || snapshot.Active.EndsAtUtc <= snapshot.Active.StartedAtUtc))
                throw InvalidResponse("The active world resource flight is inconsistent.");
        }

        private static WorldResourceCollectionClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure) return new WorldResourceCollectionClientException(WorldResourceCollectionClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized) return new WorldResourceCollectionClientException(WorldResourceCollectionClientError.AuthenticationRequired, exception.SafeCode);
            return new WorldResourceCollectionClientException(WorldResourceCollectionClientError.InvalidResponse, exception.SafeCode);
        }

        private static void RequireHive(Guid hiveId) { if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required."); }
        private static void RequireNodeId(string nodeId) { if (string.IsNullOrWhiteSpace(nodeId) || nodeId.Length > 128) throw InvalidRequest("A node identifier is required."); }
        private static void RequireRevision(long revision) { if (revision < 0 || revision == long.MaxValue) throw InvalidRequest("The expected revision is outside the supported range."); }
        private static void RequireKey(string value, string name) { if (string.IsNullOrWhiteSpace(value) || value.Length > 256) throw InvalidRequest(name + " must contain between one and 256 characters."); }

        public static string BoardPath(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/world-resources";
        public static string LaunchPath(Guid hiveId, string nodeId) => BoardPath(hiveId) + "/" + Uri.EscapeDataString(nodeId) + "/launch";
        public static string ClaimPath(Guid hiveId, Guid flightId) => BoardPath(hiveId) + "/" + flightId.ToString("D") + "/claim";
        public static string RecallPath(Guid hiveId, Guid flightId) => BoardPath(hiveId) + "/" + flightId.ToString("D") + "/recall";

        private static WorldResourceCollectionClientException InvalidRequest(string message) => new WorldResourceCollectionClientException(WorldResourceCollectionClientError.InvalidRequest, message);
        private static WorldResourceCollectionClientException InvalidResponse(string message) => new WorldResourceCollectionClientException(WorldResourceCollectionClientError.InvalidResponse, message);

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
