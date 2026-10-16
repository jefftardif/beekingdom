using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace BeeKingdom.Networking {

    public enum RoyalReserveClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }


    public sealed class RoyalReserveClientException : Exception
    {
        public RoyalReserveClientException(RoyalReserveClientError error, string safeCode) : base(safeCode)
        {
            Error = error;
            SafeCode = safeCode ?? string.Empty;
        }

        public RoyalReserveClientError Error { get; }
        public string SafeCode { get; }
    }


    public sealed class RoyalReserveMutationRequest
    {
        public string ResourceKey { get; set; }
        public long Amount { get; set; }
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }


    public sealed class RemoteRoyalReserveSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public int BankLevel { get; set; }
        public long Capacity { get; set; }
        public long ReservedTotal { get; set; }
        public Dictionary<string, long> Reserved { get; set; }
        public Dictionary<string, RemoteHiveResourceBalance> LiquidBalances { get; set; }
    }


    public interface IRoyalReserveClient
    {
        Task<RemoteRoyalReserveSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
        Task<RemoteRoyalReserveSnapshot> DepositAsync(Guid hiveId, string resourceKey, long amount, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<RemoteRoyalReserveSnapshot> WithdrawAsync(Guid hiveId, string resourceKey, long amount, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default);
    }


    public sealed class RoyalReserveClient : IRoyalReserveClient
    {
        public const string ContractVersion = "living-hive-bank-v1";

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public RoyalReserveClient(
            MobileAccountSessionGate sessionGate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<RemoteRoyalReserveSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("GET", BasePath(hiveId)), cancellationToken);
        }

        public Task<RemoteRoyalReserveSnapshot> DepositAsync(
            Guid hiveId,
            string resourceKey,
            long amount,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            TransferAsync(hiveId, "deposit", resourceKey, amount, expectedRevision, idempotencyKey, cancellationToken);

        public Task<RemoteRoyalReserveSnapshot> WithdrawAsync(
            Guid hiveId,
            string resourceKey,
            long amount,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            TransferAsync(hiveId, "withdraw", resourceKey, amount, expectedRevision, idempotencyKey, cancellationToken);

        private Task<RemoteRoyalReserveSnapshot> TransferAsync(
            Guid hiveId,
            string action,
            string resourceKey,
            long amount,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            RequireHive(hiveId);
            if (resourceKey != "honey" && resourceKey != "wax" && resourceKey != "pollen")
                throw InvalidRequest("game.invalid_request");
            if (amount <= 0 || expectedRevision < 0 || expectedRevision == long.MaxValue)
                throw InvalidRequest("game.invalid_request");
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 256)
                throw InvalidRequest("game.invalid_request");

            var body = new RoyalReserveMutationRequest
            {
                ResourceKey = resourceKey,
                Amount = amount,
                ExpectedRevision = expectedRevision,
                IdempotencyKey = idempotencyKey
            };
            return SendAsync(
                hiveId,
                new AuthenticatedGameRestRequest("POST", BasePath(hiveId) + "/" + action, body),
                cancellationToken);
        }

        private async Task<RemoteRoyalReserveSnapshot> SendAsync(
            Guid hiveId,
            AuthenticatedGameRestRequest request,
            CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            RemoteRoyalReserveSnapshot response;
            try
            {
                response = await transport.SendAsync<RemoteRoyalReserveSnapshot>(
                    request,
                    context.AccessToken,
                    cancellationToken).ConfigureAwait(false);
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
                throw new RoyalReserveClientException(RoyalReserveClientError.NotConfigured, "game.not_configured");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null ||
                session.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(session.AccessToken) ||
                session.AccessToken.Length > 8192)
                throw new RoyalReserveClientException(RoyalReserveClientError.AuthenticationRequired, "game.session_required");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static void Validate(RemoteRoyalReserveSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null || snapshot.PlayerId != playerId || snapshot.HiveId != hiveId ||
                snapshot.ContractVersion != ContractVersion || snapshot.Revision < 0 ||
                snapshot.BankLevel < 1 || snapshot.Capacity < 0 || snapshot.ReservedTotal < 0 ||
                snapshot.Reserved == null || snapshot.LiquidBalances == null)
                throw InvalidResponse("game.invalid_response");

            string[] keys = { "honey", "pollen", "wax" };
            if (keys.Any(key => !snapshot.Reserved.ContainsKey(key) || !snapshot.LiquidBalances.ContainsKey(key)) ||
                snapshot.Reserved.Any(x => x.Value < 0) ||
                snapshot.LiquidBalances.Any(x => x.Value == null || x.Value.Amount < 0 || x.Value.Capacity < x.Value.Amount) ||
                snapshot.Reserved.Values.Sum() != snapshot.ReservedTotal ||
                snapshot.ReservedTotal > snapshot.Capacity)
                throw InvalidResponse("game.invalid_response");
        }

        private static RoyalReserveClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure)
                return new RoyalReserveClientException(RoyalReserveClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                return new RoyalReserveClientException(RoyalReserveClientError.AuthenticationRequired, exception.SafeCode);
            return new RoyalReserveClientException(RoyalReserveClientError.InvalidResponse, exception.SafeCode);
        }

        public static string BasePath(Guid hiveId) =>
            "/game/v1/hives/" + hiveId.ToString("D") + "/bank/royal-reserve";

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty) throw InvalidRequest("game.invalid_request");
        }

        private static RoyalReserveClientException InvalidRequest(string safeCode) =>
            new RoyalReserveClientException(RoyalReserveClientError.InvalidRequest, safeCode);

        private static RoyalReserveClientException InvalidResponse(string safeCode) =>
            new RoyalReserveClientException(RoyalReserveClientError.InvalidResponse, safeCode);

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken)
            {
                PlayerId = playerId;
                AccessToken = accessToken;
            }

            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }

}
