using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // Premier "evenement" du jeu (demande de Jeff, 2026-07-31) : un defi ponctuel qui relie
    // plusieurs systemes deja construits (batiment, recrutement, collecte mondiale, voie
    // strategique, VIP). Meme patron simple (pas de file d'attente hors-ligne) que
    // WorldResourceCollectionClient - une consultation/reclamation peu frequente.
    public enum HiveMilestoneEventClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class HiveMilestoneEventClientException : Exception
    {
        public HiveMilestoneEventClientException(HiveMilestoneEventClientError error, string message) : base(message) { Error = error; }
        public HiveMilestoneEventClientError Error { get; }
    }

    public sealed class ClaimHiveMilestoneEventMutationRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteHiveMilestoneObjective
    {
        public string ObjectiveKey { get; set; }
        public bool Done { get; set; }
    }

    public sealed class RemoteHiveMilestoneEventSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public DateTimeOffset WindowEndsAtUtc { get; set; }
        public bool WindowExpired { get; set; }
        public List<RemoteHiveMilestoneObjective> Objectives { get; set; }
        public int RequiredObjectiveCount { get; set; }
        public bool Claimed { get; set; }
        public bool CanClaim { get; set; }
        public Dictionary<string, long> Reward { get; set; }
    }

    public interface IHiveMilestoneEventClient
    {
        Task<RemoteHiveMilestoneEventSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
        Task<RemoteHiveMilestoneEventSnapshot> ClaimAsync(Guid hiveId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    }

    public sealed class HiveMilestoneEventClient : IHiveMilestoneEventClient
    {
        public const string ContractVersion = "living-hive-milestone-event-v1";

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public HiveMilestoneEventClient(MobileAccountSessionGate sessionGate, IGameAccountSessionSource sessionSource, IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<RemoteHiveMilestoneEventSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("GET", BoardPath(hiveId)), cancellationToken);
        }

        public Task<RemoteHiveMilestoneEventSnapshot> ClaimAsync(Guid hiveId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            var request = new AuthenticatedGameRestRequest("POST", ClaimPath(hiveId), new ClaimHiveMilestoneEventMutationRequest { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            return SendAsync(hiveId, request, cancellationToken);
        }

        private async Task<RemoteHiveMilestoneEventSnapshot> SendAsync(Guid hiveId, AuthenticatedGameRestRequest request, CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            RemoteHiveMilestoneEventSnapshot response;
            try
            {
                response = await transport.SendAsync<RemoteHiveMilestoneEventSnapshot>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
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
                throw new HiveMilestoneEventClientException(HiveMilestoneEventClientError.NotConfigured, "Official account session transport is not ready.");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new HiveMilestoneEventClientException(HiveMilestoneEventClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static void ValidateSnapshot(RemoteHiveMilestoneEventSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null) throw InvalidResponse("The milestone event response is empty.");
            if (snapshot.PlayerId != playerId || snapshot.HiveId != hiveId) throw InvalidResponse("The milestone event response belongs to another session or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal)) throw InvalidResponse("The milestone event contract version is unsupported.");
            if (snapshot.Revision < 0) throw InvalidResponse("The milestone event revision is invalid.");
            if (snapshot.Objectives == null) throw InvalidResponse("The milestone event objectives are missing.");
        }

        private static HiveMilestoneEventClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure) return new HiveMilestoneEventClientException(HiveMilestoneEventClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized) return new HiveMilestoneEventClientException(HiveMilestoneEventClientError.AuthenticationRequired, exception.SafeCode);
            return new HiveMilestoneEventClientException(HiveMilestoneEventClientError.InvalidResponse, exception.SafeCode);
        }

        private static void RequireHive(Guid hiveId) { if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required."); }
        private static void RequireRevision(long revision) { if (revision < 0 || revision == long.MaxValue) throw InvalidRequest("The expected revision is outside the supported range."); }
        private static void RequireKey(string value, string name) { if (string.IsNullOrWhiteSpace(value) || value.Length > 256) throw InvalidRequest(name + " must contain between one and 256 characters."); }

        public static string BoardPath(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/milestone-event";
        public static string ClaimPath(Guid hiveId) => BoardPath(hiveId) + "/claim";

        private static HiveMilestoneEventClientException InvalidRequest(string message) => new HiveMilestoneEventClientException(HiveMilestoneEventClientError.InvalidRequest, message);
        private static HiveMilestoneEventClientException InvalidResponse(string message) => new HiveMilestoneEventClientException(HiveMilestoneEventClientError.InvalidResponse, message);

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
