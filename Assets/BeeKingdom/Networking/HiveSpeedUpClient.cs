using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // Client officiel SpeedUp (contrat "server-speedup-v1") : lecture de l'inventaire/timers
    // et application idempotente d'un item. Le serveur est la seule autorite sur les quantites,
    // les fins de timers et la revision ; le client ne transmet jamais de duree calculee hors
    // du catalogue (catalogue serveur valide itemId/categorie/duree ensemble).
    public enum HiveSpeedUpClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class HiveSpeedUpClientException : Exception
    {
        public HiveSpeedUpClientException(HiveSpeedUpClientError error, string message) : base(message) { Error = error; }
        public HiveSpeedUpClientError Error { get; }
    }

    public sealed class RemoteSpeedUpInventory
    {
        public Dictionary<string, int> Items { get; set; }
    }

    public sealed class RemoteSpeedUpTimer
    {
        public string Category { get; set; }
        public string TargetId { get; set; }
        public Guid? OperationId { get; set; }
        public DateTimeOffset CompletesAtUtc { get; set; }
        public string Status { get; set; }
    }

    public sealed class RemoteSpeedUpReadSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public RemoteSpeedUpInventory Inventory { get; set; }
        public List<RemoteSpeedUpTimer> Timers { get; set; }
        public List<string> Rewards { get; set; }
        public List<string> Events { get; set; }
    }

    public sealed class RemoteSpeedUpReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string IdempotencyKey { get; set; }
        public string ItemId { get; set; }
        public string Category { get; set; }
        public string TargetId { get; set; }
        public long ConsumedQuantity { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public string Code { get; set; }
    }

    public sealed class RemoteSpeedUpApplyResponse
    {
        public RemoteSpeedUpReceipt Receipt { get; set; }
        public RemoteSpeedUpReadSnapshot Snapshot { get; set; }
    }

    public sealed class ApplySpeedUpMutationRequest
    {
        public string ItemId { get; set; }
        public string Category { get; set; }
        public string TargetId { get; set; }
        public long DurationSeconds { get; set; }
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public interface IHiveSpeedUpClient
    {
        Task<RemoteSpeedUpReadSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
        Task<RemoteSpeedUpApplyResponse> ApplyAsync(Guid hiveId, ApplySpeedUpMutationRequest request, CancellationToken cancellationToken = default);
    }

    public sealed class HiveSpeedUpClient : IHiveSpeedUpClient
    {
        public const string ContractVersion = "server-speedup-v1";

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public HiveSpeedUpClient(MobileAccountSessionGate sessionGate, IGameAccountSessionSource sessionSource, IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<RemoteSpeedUpReadSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return ReadAsyncCore(hiveId, new AuthenticatedGameRestRequest("GET", BoardPath(hiveId)), cancellationToken);
        }

        public Task<RemoteSpeedUpApplyResponse> ApplyAsync(Guid hiveId, ApplySpeedUpMutationRequest request, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireApply(request);
            var body = new ApplySpeedUpMutationRequest
            {
                ItemId = request.ItemId,
                Category = request.Category,
                TargetId = request.TargetId,
                DurationSeconds = request.DurationSeconds,
                ExpectedRevision = request.ExpectedRevision,
                IdempotencyKey = request.IdempotencyKey
            };
            return ApplyAsyncCore(hiveId, new AuthenticatedGameRestRequest("POST", ApplyPath(hiveId), body), cancellationToken);
        }

        private async Task<RemoteSpeedUpReadSnapshot> ReadAsyncCore(Guid hiveId, AuthenticatedGameRestRequest request, CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            RemoteSpeedUpReadSnapshot response;
            try
            {
                response = await transport.SendAsync<RemoteSpeedUpReadSnapshot>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                throw MapTransportFailure(exception);
            }
            ValidateSnapshot(response, context.PlayerId, hiveId);
            return response;
        }

        private async Task<RemoteSpeedUpApplyResponse> ApplyAsyncCore(Guid hiveId, AuthenticatedGameRestRequest request, CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            RemoteSpeedUpApplyResponse response;
            try
            {
                response = await transport.SendAsync<RemoteSpeedUpApplyResponse>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                throw MapTransportFailure(exception);
            }
            if (response == null || response.Snapshot == null || response.Receipt == null)
                throw InvalidResponse("The speedup apply response is empty.");
            ValidateSnapshot(response.Snapshot, context.PlayerId, hiveId);
            if (response.Receipt.PlayerId != context.PlayerId || response.Receipt.HiveId != hiveId)
                throw InvalidResponse("The speedup receipt belongs to another session or hive.");
            return response;
        }

        private async Task<SessionContext> RequireSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new HiveSpeedUpClientException(HiveSpeedUpClientError.NotConfigured, "Official account session transport is not ready.");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new HiveSpeedUpClientException(HiveSpeedUpClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static void ValidateSnapshot(RemoteSpeedUpReadSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null) throw InvalidResponse("The speedup response is empty.");
            if (snapshot.PlayerId != playerId || snapshot.HiveId != hiveId) throw InvalidResponse("The speedup response belongs to another session or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal)) throw InvalidResponse("The speedup contract version is unsupported.");
            if (snapshot.Revision < 0) throw InvalidResponse("The speedup revision is invalid.");
            if (snapshot.Inventory == null || snapshot.Inventory.Items == null) throw InvalidResponse("The speedup inventory is missing.");
            if (snapshot.Timers == null || snapshot.Rewards == null || snapshot.Events == null) throw InvalidResponse("The speedup snapshot collections are missing.");
        }

        private static void RequireApply(ApplySpeedUpMutationRequest request)
        {
            if (request == null) throw InvalidRequest("A speedup apply request is required.");
            if (string.IsNullOrWhiteSpace(request.ItemId) || request.ItemId.Length > 128) throw InvalidRequest("The item identifier is invalid.");
            if (string.IsNullOrWhiteSpace(request.Category) || request.Category.Length > 64) throw InvalidRequest("The category is invalid.");
            if (string.IsNullOrWhiteSpace(request.TargetId) || request.TargetId.Length > 256) throw InvalidRequest("The target identifier is invalid.");
            if (request.DurationSeconds <= 0) throw InvalidRequest("The duration is outside the supported range.");
            if (request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue) throw InvalidRequest("The expected revision is outside the supported range.");
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 256) throw InvalidRequest("The idempotency key must contain between one and 256 characters.");
        }

        private static HiveSpeedUpClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure) return new HiveSpeedUpClientException(HiveSpeedUpClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized) return new HiveSpeedUpClientException(HiveSpeedUpClientError.AuthenticationRequired, exception.SafeCode);
            return new HiveSpeedUpClientException(HiveSpeedUpClientError.InvalidResponse, exception.SafeCode);
        }

        private static void RequireHive(Guid hiveId) { if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required."); }
        private static HiveSpeedUpClientException InvalidRequest(string message) => new HiveSpeedUpClientException(HiveSpeedUpClientError.InvalidRequest, message);
        private static HiveSpeedUpClientException InvalidResponse(string message) => new HiveSpeedUpClientException(HiveSpeedUpClientError.InvalidResponse, message);

        public static string BoardPath(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/speedups";
        public static string ApplyPath(Guid hiveId) => BoardPath(hiveId) + "/apply";

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
