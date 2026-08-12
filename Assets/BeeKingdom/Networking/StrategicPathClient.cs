using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // A single, permanent, one-time choice among 5 hive identities (royal_guard/striker/
    // nurturer/scout/alchemist) - no active state, no timers, no receipts, just a read and a
    // one-shot write that locks forever server-side. Deliberately as small as CombatPatrolClient,
    // not the larger HivePerimeterSortieClient pattern (no offline mutation outbox needed for a
    // single low-frequency choice).
    public enum StrategicPathClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class StrategicPathClientException : Exception
    {
        public StrategicPathClientException(StrategicPathClientError error, string message) : base(message) { Error = error; }
        public StrategicPathClientError Error { get; }
    }

    public sealed class StrategicPathChooseRequest
    {
        public string PathId { get; set; }
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteStrategicPathSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string CatalogVersion { get; set; }
        public List<string> CanonicalPaths { get; set; }
        public string SelectedPath { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }

    public interface IStrategicPathClient
    {
        Task<RemoteStrategicPathSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
        Task<RemoteStrategicPathSnapshot> ChooseAsync(Guid hiveId, string pathId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    }

    public sealed class StrategicPathClient : IStrategicPathClient
    {
        public const string CatalogVersion = "phase4-v1";
        private static readonly string[] CanonicalPaths = { "royal_guard", "striker", "nurturer", "scout", "alchemist" };

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public StrategicPathClient(MobileAccountSessionGate sessionGate, IGameAccountSessionSource sessionSource, IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<RemoteStrategicPathSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("GET", Path(hiveId)), cancellationToken);
        }

        public Task<RemoteStrategicPathSnapshot> ChooseAsync(Guid hiveId, string pathId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequirePath(pathId);
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey);
            var request = new AuthenticatedGameRestRequest("POST", Path(hiveId), new StrategicPathChooseRequest
            {
                PathId = pathId,
                ExpectedRevision = expectedRevision,
                IdempotencyKey = idempotencyKey
            });
            return SendAsync(hiveId, request, cancellationToken);
        }

        private async Task<RemoteStrategicPathSnapshot> SendAsync(Guid hiveId, AuthenticatedGameRestRequest request, CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            RemoteStrategicPathSnapshot response;
            try
            {
                response = await transport.SendAsync<RemoteStrategicPathSnapshot>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                throw MapTransportFailure(exception);
            }
            Validate(response, context.PlayerId, hiveId);
            return response;
        }

        private async Task<SessionContext> RequireSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new StrategicPathClientException(StrategicPathClientError.NotConfigured, "Official account session transport is not ready.");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new StrategicPathClientException(StrategicPathClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static void Validate(RemoteStrategicPathSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null) throw InvalidResponse("The strategic path response is empty.");
            if (snapshot.PlayerId != playerId || snapshot.HiveId != hiveId) throw InvalidResponse("The strategic path response belongs to another session or hive.");
            if (!string.Equals(snapshot.CatalogVersion, CatalogVersion, StringComparison.Ordinal)) throw InvalidResponse("The strategic path catalog version is unsupported.");
            if (snapshot.Revision < 0) throw InvalidResponse("The strategic path revision is invalid.");
            if (snapshot.CanonicalPaths == null || snapshot.CanonicalPaths.Count != CanonicalPaths.Length ||
                CanonicalPaths.Any(path => !snapshot.CanonicalPaths.Contains(path)))
                throw InvalidResponse("The strategic path catalog is inconsistent.");
            if (snapshot.SelectedPath != null && !CanonicalPaths.Contains(snapshot.SelectedPath))
                throw InvalidResponse("The selected strategic path is unknown.");
        }

        private static StrategicPathClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure) return new StrategicPathClientException(StrategicPathClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized) return new StrategicPathClientException(StrategicPathClientError.AuthenticationRequired, exception.SafeCode);
            return new StrategicPathClientException(StrategicPathClientError.InvalidResponse, exception.SafeCode);
        }

        private static void RequireHive(Guid hiveId) { if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required."); }
        private static void RequirePath(string pathId) { if (string.IsNullOrWhiteSpace(pathId) || !CanonicalPaths.Contains(pathId)) throw InvalidRequest("The strategic path is unsupported."); }
        private static void RequireRevision(long revision) { if (revision < 0 || revision == long.MaxValue) throw InvalidRequest("The expected revision is outside the supported range."); }
        private static void RequireKey(string value) { if (string.IsNullOrWhiteSpace(value) || value.Length > 256) throw InvalidRequest("The idempotency key must contain between one and 256 characters."); }

        public static string Path(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/strategic-path";

        private static StrategicPathClientException InvalidRequest(string message) => new StrategicPathClientException(StrategicPathClientError.InvalidRequest, message);
        private static StrategicPathClientException InvalidResponse(string message) => new StrategicPathClientException(StrategicPathClientError.InvalidResponse, message);

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
