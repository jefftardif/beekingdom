using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteChampionBeeSnapshot
    {
        public Dictionary<string, int> Levels { get; set; }
        public List<string> AssignedBeeIds { get; set; }
        public int MaxAssigned { get; set; }
        public long Revision { get; set; }
    }

    public sealed class RemoteChampionBeeMutationResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public string BeeId { get; set; }
        public int Level { get; set; }
        public List<string> AssignedBeeIds { get; set; }
        public long Revision { get; set; }
    }

    public sealed class ChampionBeeMutationRequestWire
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class SetChampionBeeAssignmentRequestWire
    {
        public List<string> BeeIds { get; set; }
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public interface IHiveChampionBeeClient
    {
        Task<RemoteChampionBeeSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteChampionBeeMutationResult> GrantAsync(Guid hiveId, string beeId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteChampionBeeMutationResult> LevelUpAsync(Guid hiveId, string beeId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteChampionBeeMutationResult> SetAssignmentAsync(Guid hiveId, IReadOnlyList<string> beeIds, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken));
    }

    public sealed class HiveChampionBeeClient : IHiveChampionBeeClient
    {
        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public HiveChampionBeeClient(
            MobileAccountSessionGate sessionGate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<RemoteChampionBeeSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            HiveGameClientSessionHelper.SessionContext context = await HiveGameClientSessionHelper.RequireSessionAsync(sessionGate, sessionSource, cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest("GET", Path(hiveId));
            RemoteChampionBeeSnapshot snapshot = await HiveGameClientSessionHelper
                .SendWithSingleAuthenticationRefreshAsync<RemoteChampionBeeSnapshot>(transport, sessionSource, request, context, cancellationToken)
                .ConfigureAwait(false);
            if (snapshot == null || snapshot.Levels == null || snapshot.AssignedBeeIds == null || snapshot.Revision < 0)
                throw new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, "The champion bee snapshot is incomplete.");
            return snapshot;
        }

        public Task<RemoteChampionBeeMutationResult> GrantAsync(Guid hiveId, string beeId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken)) =>
            MutateAsync(hiveId, "/" + beeId + "/grant", expectedRevision, idempotencyKey, cancellationToken);

        public Task<RemoteChampionBeeMutationResult> LevelUpAsync(Guid hiveId, string beeId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken)) =>
            MutateAsync(hiveId, "/" + beeId + "/level-up", expectedRevision, idempotencyKey, cancellationToken);

        public async Task<RemoteChampionBeeMutationResult> SetAssignmentAsync(Guid hiveId, IReadOnlyList<string> beeIds, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            HiveGameClientSessionHelper.RequireIdempotencyKey(idempotencyKey);
            HiveGameClientSessionHelper.SessionContext context = await HiveGameClientSessionHelper.RequireSessionAsync(sessionGate, sessionSource, cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                Path(hiveId) + "/assignment",
                new SetChampionBeeAssignmentRequestWire { BeeIds = new List<string>(beeIds ?? Array.Empty<string>()), ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            RemoteChampionBeeMutationResult result = await HiveGameClientSessionHelper
                .SendWithSingleAuthenticationRefreshAsync<RemoteChampionBeeMutationResult>(transport, sessionSource, request, context, cancellationToken)
                .ConfigureAwait(false);
            RequireMutationResult(result);
            return result;
        }

        private async Task<RemoteChampionBeeMutationResult> MutateAsync(Guid hiveId, string suffix, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken)
        {
            RequireHive(hiveId);
            HiveGameClientSessionHelper.RequireIdempotencyKey(idempotencyKey);
            HiveGameClientSessionHelper.SessionContext context = await HiveGameClientSessionHelper.RequireSessionAsync(sessionGate, sessionSource, cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                Path(hiveId) + suffix,
                new ChampionBeeMutationRequestWire { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            RemoteChampionBeeMutationResult result = await HiveGameClientSessionHelper
                .SendWithSingleAuthenticationRefreshAsync<RemoteChampionBeeMutationResult>(transport, sessionSource, request, context, cancellationToken)
                .ConfigureAwait(false);
            RequireMutationResult(result);
            return result;
        }

        private static void RequireMutationResult(RemoteChampionBeeMutationResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.Code) || result.Revision < 0)
                throw new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, "The champion bee mutation response is incomplete.");
        }

        private static string Path(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/champion-bees";

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty) throw new HivePerimeterClientException(HivePerimeterClientError.InvalidRequest, "A hive identifier is required.");
        }
    }

    public sealed class RemoteTroopTierSnapshot
    {
        public Dictionary<string, int> Tiers { get; set; }
        public long Revision { get; set; }
    }

    public sealed class RemoteTroopTierMutationResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public string PopulationId { get; set; }
        public int Tier { get; set; }
        public long Revision { get; set; }
    }

    public sealed class PromoteTroopTierRequestWire
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public interface IHiveTroopTierClient
    {
        Task<RemoteTroopTierSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteTroopTierMutationResult> PromoteAsync(Guid hiveId, string populationId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken));
    }

    public sealed class HiveTroopTierClient : IHiveTroopTierClient
    {
        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public HiveTroopTierClient(
            MobileAccountSessionGate sessionGate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<RemoteTroopTierSnapshot> ReadAsync(Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            HiveGameClientSessionHelper.SessionContext context = await HiveGameClientSessionHelper.RequireSessionAsync(sessionGate, sessionSource, cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest("GET", Path(hiveId));
            RemoteTroopTierSnapshot snapshot = await HiveGameClientSessionHelper
                .SendWithSingleAuthenticationRefreshAsync<RemoteTroopTierSnapshot>(transport, sessionSource, request, context, cancellationToken)
                .ConfigureAwait(false);
            if (snapshot == null || snapshot.Tiers == null || snapshot.Revision < 0)
                throw new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, "The troop tier snapshot is incomplete.");
            return snapshot;
        }

        public async Task<RemoteTroopTierMutationResult> PromoteAsync(Guid hiveId, string populationId, long expectedRevision, string idempotencyKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            HiveGameClientSessionHelper.RequireIdempotencyKey(idempotencyKey);
            HiveGameClientSessionHelper.SessionContext context = await HiveGameClientSessionHelper.RequireSessionAsync(sessionGate, sessionSource, cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                Path(hiveId) + "/" + populationId + "/promote",
                new PromoteTroopTierRequestWire { ExpectedRevision = expectedRevision, IdempotencyKey = idempotencyKey });
            RemoteTroopTierMutationResult result = await HiveGameClientSessionHelper
                .SendWithSingleAuthenticationRefreshAsync<RemoteTroopTierMutationResult>(transport, sessionSource, request, context, cancellationToken)
                .ConfigureAwait(false);
            if (result == null || string.IsNullOrWhiteSpace(result.Code) || result.Revision < 0)
                throw new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, "The troop tier mutation response is incomplete.");
            return result;
        }

        private static string Path(Guid hiveId) => "/game/v1/hives/" + hiveId.ToString("D") + "/troop-tiers";

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty) throw new HivePerimeterClientException(HivePerimeterClientError.InvalidRequest, "A hive identifier is required.");
        }
    }

    // Plomberie de session/transport partagee par les clients de progression (abeilles
    // championnes, paliers de troupe) - mirroir simplifie de la logique dupliquee dans les
    // autres clients (HiveResearchClient, etc.) sans le cache de lecture protegee hors ligne
    // (ces deux systemes exigent une session serveur active, pas de repli hors ligne).
    internal static class HiveGameClientSessionHelper
    {
        internal sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken)
            {
                PlayerId = playerId;
                AccessToken = accessToken;
            }

            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }

        internal static async Task<SessionContext> RequireSessionAsync(
            MobileAccountSessionGate sessionGate,
            IGameAccountSessionSource sessionSource,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new HivePerimeterClientException(HivePerimeterClientError.NotConfigured, "Official account session transport is not ready.");

            IRefreshableGameAccountSessionSource refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable != null)
            {
                try
                {
                    return RequireUsableSession(await refreshable.GetFreshSessionAsync(cancellationToken).ConfigureAwait(false));
                }
                catch (OperationCanceledException) { throw; }
                catch (MobileAccountSessionException exception) { throw MapSessionFailure(exception); }
            }

            GameAccountSession session;
            if (!sessionSource.TryGetSession(out session)) session = null;
            return RequireUsableSession(session);
        }

        internal static async Task<T> SendWithSingleAuthenticationRefreshAsync<T>(
            IAuthenticatedGameRestTransport transport,
            IGameAccountSessionSource sessionSource,
            AuthenticatedGameRestRequest request,
            SessionContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                return await transport.SendAsync<T>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error != AuthenticatedGameRestError.Unauthorized)
                    throw MapTransportFailure(exception);
            }

            IRefreshableGameAccountSessionSource refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable == null)
                throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "The game session was rejected.");

            GameAccountSession replacement;
            try
            {
                replacement = await refreshable.RefreshAfterUnauthorizedAsync(context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (MobileAccountSessionException exception) { throw MapSessionFailure(exception); }

            if (replacement == null || replacement.PlayerId != context.PlayerId ||
                string.IsNullOrWhiteSpace(replacement.AccessToken) || replacement.AccessToken.Length > 8192)
            {
                await refreshable.InvalidateUnauthorizedSessionAsync(context.AccessToken, cancellationToken).ConfigureAwait(false);
                throw new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, "The refreshed game session changed identity.");
            }

            try
            {
                return await transport.SendAsync<T>(request, replacement.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                {
                    await refreshable.InvalidateUnauthorizedSessionAsync(replacement.AccessToken, cancellationToken).ConfigureAwait(false);
                    throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "The refreshed game session was rejected.");
                }
                throw MapTransportFailure(exception);
            }
        }

        internal static void RequireIdempotencyKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() != value || value.Length > 256)
                throw new HivePerimeterClientException(HivePerimeterClientError.InvalidRequest, "The idempotency key must contain between one and 256 trimmed characters.");
        }

        private static SessionContext RequireUsableSession(GameAccountSession session)
        {
            if (session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static HivePerimeterClientException MapSessionFailure(MobileAccountSessionException exception)
        {
            if (exception.Error == MobileAccountSessionError.TransportFailure)
                return new HivePerimeterClientException(HivePerimeterClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == MobileAccountSessionError.NotConfigured)
                return new HivePerimeterClientException(HivePerimeterClientError.NotConfigured, exception.SafeCode);
            return new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, exception.SafeCode);
        }

        private static HivePerimeterClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure)
                return new HivePerimeterClientException(HivePerimeterClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                return new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, exception.SafeCode);
            return new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, exception.SafeCode);
        }
    }
}
