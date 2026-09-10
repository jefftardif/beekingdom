using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // M077-CL: petite chaine d'objectifs Alpha persistante (5 objectifs, reclamation individuelle
    // par objectif). Meme patron simple (pas de file d'attente hors-ligne) que
    // HiveMilestoneEventClient, dont ce client est un frere structurel - contrat different
    // (reclamation par objectif plutot qu'un lot agrege, jamais d'expiration).
    public enum QuestChainClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class QuestChainClientException : Exception
    {
        public QuestChainClientException(QuestChainClientError error, string message) : base(message) { Error = error; }
        public QuestChainClientError Error { get; }
    }

    public sealed class ClaimQuestChainObjectiveMutationRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteQuestChainObjective
    {
        public string ObjectiveKey { get; set; }
        public bool Done { get; set; }
        public bool Claimed { get; set; }
        public bool CanClaim { get; set; }
        public string RewardResourceKey { get; set; }
        public long RewardAmount { get; set; }
    }

    public sealed class RemoteQuestChainSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public List<RemoteQuestChainObjective> Objectives { get; set; }
    }

    public interface IQuestChainClient
    {
        Task<RemoteQuestChainSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
        Task<RemoteQuestChainSnapshot> ReportWorldMapVisitedAsync(Guid hiveId, CancellationToken cancellationToken = default);
        Task<RemoteQuestChainSnapshot> ClaimAsync(Guid hiveId, string objectiveKey, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    }

    public sealed class QuestChainClient : IQuestChainClient
    {
        public const string ContractVersion = "living-hive-quest-chain-v1";

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public QuestChainClient(MobileAccountSessionGate sessionGate, IGameAccountSessionSource sessionSource, IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<RemoteQuestChainSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("GET", BoardPath(hiveId)), cancellationToken);
        }

        public Task<RemoteQuestChainSnapshot> ReportWorldMapVisitedAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("POST", WorldMapVisitedPath(hiveId)), cancellationToken);
        }

        public Task<RemoteQuestChainSnapshot> ClaimAsync(Guid hiveId, string objectiveKey, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireKey(objectiveKey, nameof(objectiveKey));
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            var request = new AuthenticatedGameRestRequest("POST", ClaimPath(hiveId, objectiveKey), new ClaimQuestChainObjectiveMutationRequest { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            return SendAsync(hiveId, request, cancellationToken);
        }

        private async Task<RemoteQuestChainSnapshot> SendAsync(Guid hiveId, AuthenticatedGameRestRequest request, CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            RemoteQuestChainSnapshot response;
            try
            {
                response = await transport.SendAsync<RemoteQuestChainSnapshot>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
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
                throw new QuestChainClientException(QuestChainClientError.NotConfigured, "Official account session transport is not ready.");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new QuestChainClientException(QuestChainClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static void ValidateSnapshot(RemoteQuestChainSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null) throw InvalidResponse("The quest chain response is empty.");
            if (snapshot.PlayerId != playerId || snapshot.HiveId != hiveId) throw InvalidResponse("The quest chain response belongs to another session or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal)) throw InvalidResponse("The quest chain contract version is unsupported.");
            if (snapshot.Revision < 0) throw InvalidResponse("The quest chain revision is invalid.");
            if (snapshot.Objectives == null) throw InvalidResponse("The quest chain objectives are missing.");
        }

        private static QuestChainClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure) return new QuestChainClientException(QuestChainClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized) return new QuestChainClientException(QuestChainClientError.AuthenticationRequired, exception.SafeCode);
            return new QuestChainClientException(QuestChainClientError.InvalidResponse, exception.SafeCode);
        }

        private static void RequireHive(Guid hiveId) { if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required."); }
        private static void RequireRevision(long revision) { if (revision < 0 || revision == long.MaxValue) throw InvalidRequest("The expected revision is outside the supported range."); }
        private static void RequireKey(string value, string name) { if (string.IsNullOrWhiteSpace(value) || value.Length > 256) throw InvalidRequest(name + " must contain between one and 256 characters."); }

        public static string BoardPath(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/quest-chain";
        public static string WorldMapVisitedPath(Guid hiveId) => BoardPath(hiveId) + "/world-map-visited";
        public static string ClaimPath(Guid hiveId, string objectiveKey) => BoardPath(hiveId) + "/" + objectiveKey + "/claim";

        private static QuestChainClientException InvalidRequest(string message) => new QuestChainClientException(QuestChainClientError.InvalidRequest, message);
        private static QuestChainClientException InvalidResponse(string message) => new QuestChainClientException(QuestChainClientError.InvalidResponse, message);

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
