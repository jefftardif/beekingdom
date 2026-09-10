using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // M076I-CL: le Courrier (rapports de combat, invitations d'Alliance, recompenses) doit
    // survivre a un changement d'appareil - demande CEO explicite : "il faut que ce soit
    // sauvegarde sur le serveur, nous ne conservons rien localement" (un premier correctif
    // PlayerPrefs local a ete juge insuffisant). Meme patron simple (pas de file d'attente
    // hors-ligne) que QuestChainClient, dont ce client est un frere structurel - le client
    // reste seul a DERIVER le contenu de chaque message (combat/alliance restent la source de
    // verite pour leur propre contenu), ce transport ne fait que persister la boite deja
    // construite.
    public enum CourierMailboxClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class CourierMailboxClientException : Exception
    {
        public CourierMailboxClientException(CourierMailboxClientError error, string message) : base(message) { Error = error; }
        public CourierMailboxClientError Error { get; }
    }

    public sealed class RemoteCourierReward
    {
        public string ItemId { get; set; }
        public int Amount { get; set; }
        public bool Collected { get; set; }
    }

    public sealed class RemoteCourierMessage
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Preview { get; set; }
        public string Body { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public bool Read { get; set; }
        public bool Favorite { get; set; }
        public List<RemoteCourierReward> Rewards { get; set; }
    }

    public sealed class RemoteCourierMailboxSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public List<RemoteCourierMessage> Messages { get; set; }
    }

    public sealed class AppendCourierMessageMutationRequest
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Preview { get; set; }
        public string Body { get; set; }
        public List<RemoteCourierReward> Rewards { get; set; }
    }

    public sealed class SetCourierFlagMutationRequest
    {
        public bool Value { get; set; }
    }

    public interface ICourierMailboxClient
    {
        Task<RemoteCourierMailboxSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
        Task<RemoteCourierMailboxSnapshot> AppendAsync(Guid hiveId, AppendCourierMessageMutationRequest message, CancellationToken cancellationToken = default);
        Task<RemoteCourierMailboxSnapshot> SetReadAsync(Guid hiveId, string messageId, bool read, CancellationToken cancellationToken = default);
        Task<RemoteCourierMailboxSnapshot> SetFavoriteAsync(Guid hiveId, string messageId, bool favorite, CancellationToken cancellationToken = default);
        Task<RemoteCourierMailboxSnapshot> CollectRewardsAsync(Guid hiveId, string messageId, CancellationToken cancellationToken = default);
        Task<RemoteCourierMailboxSnapshot> MarkAllReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
        Task<RemoteCourierMailboxSnapshot> DeleteReadAsync(Guid hiveId, CancellationToken cancellationToken = default);
    }

    public sealed class CourierMailboxClient : ICourierMailboxClient
    {
        public const string ContractVersion = "living-hive-courier-mailbox-v1";

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public CourierMailboxClient(MobileAccountSessionGate sessionGate, IGameAccountSessionSource sessionSource, IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<RemoteCourierMailboxSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("GET", BoardPath(hiveId)), cancellationToken);
        }

        public Task<RemoteCourierMailboxSnapshot> AppendAsync(Guid hiveId, AppendCourierMessageMutationRequest message, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            if (message == null) throw InvalidRequest("A courier message is required.");
            RequireKey(message.Id, nameof(message.Id));
            RequireKey(message.Category, nameof(message.Category));
            RequireKey(message.Title, nameof(message.Title));
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("POST", AppendPath(hiveId), message), cancellationToken);
        }

        public Task<RemoteCourierMailboxSnapshot> SetReadAsync(Guid hiveId, string messageId, bool read, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireKey(messageId, nameof(messageId));
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("POST", ReadPath(hiveId, messageId), new SetCourierFlagMutationRequest { Value = read }), cancellationToken);
        }

        public Task<RemoteCourierMailboxSnapshot> SetFavoriteAsync(Guid hiveId, string messageId, bool favorite, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireKey(messageId, nameof(messageId));
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("POST", FavoritePath(hiveId, messageId), new SetCourierFlagMutationRequest { Value = favorite }), cancellationToken);
        }

        public Task<RemoteCourierMailboxSnapshot> CollectRewardsAsync(Guid hiveId, string messageId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            RequireKey(messageId, nameof(messageId));
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("POST", CollectPath(hiveId, messageId)), cancellationToken);
        }

        public Task<RemoteCourierMailboxSnapshot> MarkAllReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("POST", MarkAllReadPath(hiveId)), cancellationToken);
        }

        public Task<RemoteCourierMailboxSnapshot> DeleteReadAsync(Guid hiveId, CancellationToken cancellationToken = default)
        {
            RequireHive(hiveId);
            return SendAsync(hiveId, new AuthenticatedGameRestRequest("POST", DeleteReadPath(hiveId)), cancellationToken);
        }

        private async Task<RemoteCourierMailboxSnapshot> SendAsync(Guid hiveId, AuthenticatedGameRestRequest request, CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            RemoteCourierMailboxSnapshot response;
            try
            {
                response = await transport.SendAsync<RemoteCourierMailboxSnapshot>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
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
                throw new CourierMailboxClientException(CourierMailboxClientError.NotConfigured, "Official account session transport is not ready.");
            if (!sessionSource.TryGetSession(out GameAccountSession session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new CourierMailboxClientException(CourierMailboxClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static void ValidateSnapshot(RemoteCourierMailboxSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null) throw InvalidResponse("The courier mailbox response is empty.");
            if (snapshot.PlayerId != playerId || snapshot.HiveId != hiveId) throw InvalidResponse("The courier mailbox response belongs to another session or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal)) throw InvalidResponse("The courier mailbox contract version is unsupported.");
            if (snapshot.Messages == null) throw InvalidResponse("The courier mailbox messages are missing.");
        }

        private static CourierMailboxClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure) return new CourierMailboxClientException(CourierMailboxClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized) return new CourierMailboxClientException(CourierMailboxClientError.AuthenticationRequired, exception.SafeCode);
            return new CourierMailboxClientException(CourierMailboxClientError.InvalidResponse, exception.SafeCode);
        }

        private static void RequireHive(Guid hiveId) { if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required."); }
        private static void RequireKey(string value, string name) { if (string.IsNullOrWhiteSpace(value) || value.Length > 256) throw InvalidRequest(name + " must contain between one and 256 characters."); }

        public static string BoardPath(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/courier";
        public static string AppendPath(Guid hiveId) => BoardPath(hiveId) + "/append";
        public static string ReadPath(Guid hiveId, string messageId) => BoardPath(hiveId) + "/" + Uri.EscapeDataString(messageId) + "/read";
        public static string FavoritePath(Guid hiveId, string messageId) => BoardPath(hiveId) + "/" + Uri.EscapeDataString(messageId) + "/favorite";
        public static string CollectPath(Guid hiveId, string messageId) => BoardPath(hiveId) + "/" + Uri.EscapeDataString(messageId) + "/collect";
        public static string MarkAllReadPath(Guid hiveId) => BoardPath(hiveId) + "/mark-all-read";
        public static string DeleteReadPath(Guid hiveId) => BoardPath(hiveId) + "/delete-read";

        private static CourierMailboxClientException InvalidRequest(string message) => new CourierMailboxClientException(CourierMailboxClientError.InvalidRequest, message);
        private static CourierMailboxClientException InvalidResponse(string message) => new CourierMailboxClientException(CourierMailboxClientError.InvalidResponse, message);

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
