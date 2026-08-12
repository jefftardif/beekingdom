using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // Client officiel du ledger Rewards (contrat "server-reward-ledger-v1") : lecture du
    // pipeline de settlement - recompenses claimables et evenements (queue_completed,
    // reward_granted, reward_claimed). Lecture seule ; les octrois restent reserves au
    // serveur et aux routes admin.
    public enum HiveRewardLedgerClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class HiveRewardLedgerClientException : Exception
    {
        public HiveRewardLedgerClientException(HiveRewardLedgerClientError error, string message) : base(message) { Error = error; }
        public HiveRewardLedgerClientError Error { get; }
    }

    public sealed class RemoteRewardLedgerEntry
    {
        public string RewardKey { get; set; }
        public string Source { get; set; }
        public string ResourceKey { get; set; }
        public long Amount { get; set; }
        public long CreditedAmount { get; set; }
        public bool Claimed { get; set; }
        public string NotificationKey { get; set; }
    }

    public sealed class RemoteRewardLedgerEvent
    {
        public string EventKey { get; set; }
        public string TargetKey { get; set; }
        public DateTimeOffset AtUtc { get; set; }
    }

    public sealed class RemoteRewardLedgerSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public List<RemoteRewardLedgerEntry> Rewards { get; set; }
        public List<RemoteRewardLedgerEvent> Events { get; set; }
    }

    public interface IHiveRewardLedgerClient
    {
        Task<RemoteRewardLedgerSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
    }

    public sealed class HiveRewardLedgerClient : IHiveRewardLedgerClient
    {
        public const string ContractVersion = "server-reward-ledger-v1";

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public HiveRewardLedgerClient(MobileAccountSessionGate sessionGate, IGameAccountSessionSource sessionSource, IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<RemoteRewardLedgerSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("GET", BoardPath(hiveId)), cancellationToken);
        }

        private async Task<RemoteRewardLedgerSnapshot> SendAsync(Guid hiveId, AuthenticatedGameRestRequest request, CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            RemoteRewardLedgerSnapshot response;
            try
            {
                response = await transport.SendAsync<RemoteRewardLedgerSnapshot>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
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
                throw new HiveRewardLedgerClientException(HiveRewardLedgerClientError.NotConfigured, "Official account session transport is not ready.");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new HiveRewardLedgerClientException(HiveRewardLedgerClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static void ValidateSnapshot(RemoteRewardLedgerSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null) throw InvalidResponse("The reward ledger response is empty.");
            if (snapshot.PlayerId != playerId || snapshot.HiveId != hiveId) throw InvalidResponse("The reward ledger response belongs to another session or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal)) throw InvalidResponse("The reward ledger contract version is unsupported.");
            if (snapshot.Revision < 0) throw InvalidResponse("The reward ledger revision is invalid.");
            if (snapshot.Rewards == null || snapshot.Events == null) throw InvalidResponse("The reward ledger collections are missing.");
        }

        private static HiveRewardLedgerClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure) return new HiveRewardLedgerClientException(HiveRewardLedgerClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized) return new HiveRewardLedgerClientException(HiveRewardLedgerClientError.AuthenticationRequired, exception.SafeCode);
            return new HiveRewardLedgerClientException(HiveRewardLedgerClientError.InvalidResponse, exception.SafeCode);
        }

        private static void RequireHive(Guid hiveId) { if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required."); }
        private static HiveRewardLedgerClientException InvalidRequest(string message) => new HiveRewardLedgerClientException(HiveRewardLedgerClientError.InvalidRequest, message);
        private static HiveRewardLedgerClientException InvalidResponse(string message) => new HiveRewardLedgerClientException(HiveRewardLedgerClientError.InvalidResponse, message);

        public static string BoardPath(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/rewards";

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
