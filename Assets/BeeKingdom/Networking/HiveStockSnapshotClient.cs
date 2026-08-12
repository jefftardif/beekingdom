using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteHiveStockResource
    {
        public long Amount { get; set; }
        public long Capacity { get; set; }
    }

    public sealed class RemoteHiveStockEngagement
    {
        public Guid OperationId { get; set; }
        public string Kind { get; set; }
        public string Key { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndsAtUtc { get; set; }
    }

    public sealed class RemoteHiveStockSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public string CatalogVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public RemoteHiveStockResource Honey { get; set; }
        public RemoteHiveStockResource Wax { get; set; }
        public RemoteHiveStockResource Pollen { get; set; }
        public long? Population { get; set; }
        public long? PopulationCapacity { get; set; }
        public List<string> CompletedResearchIds { get; set; }
        public List<RemoteHiveStockEngagement> ActiveEngagements { get; set; }
    }

    public interface IHiveStockSnapshotClient
    {
        GameReadSource LastReadSource { get; }
        DateTimeOffset LastReadCachedAtUtc { get; }
        Task<RemoteHiveStockSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public sealed class HiveStockSnapshotClient : IHiveStockSnapshotClient
    {
        public const string ContractVersion = "living-hive-stock-v1";
        public static readonly TimeSpan MaximumEngagementDuration = TimeSpan.FromDays(30);

        private static readonly HashSet<string> SupportedEngagementKinds =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "BuildingUpgrade",
                "Training",
                "Production",
                "Research"
            };

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;
        private readonly ProtectedGameReadCache readCache;

        public HiveStockSnapshotClient(
            MobileAccountSessionGate sessionGate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport,
            ProtectedGameReadCache readCache = null)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.readCache = readCache;
        }

        public GameReadSource LastReadSource { get; private set; }
        public DateTimeOffset LastReadCachedAtUtc { get; private set; }

        public IReadOnlyList<string> ProofRows()
        {
            return new[]
            {
                "mobile_hive_stock_contract:" + ContractVersion,
                "hive_stock_resource_authority:server",
                "hive_stock_population_authority:server_or_unavailable",
                "hive_stock_engagement_authority:server",
                "hive_stock_get_protected_cache:" +
                    (readCache != null && readCache.IsProtectionAvailable).ToString().ToLowerInvariant(),
                "hive_stock_cache_partition:player_and_hive",
                "hive_stock_cache_read_only:true",
                "hive_stock_local_resource_fallback:false",
                "hive_stock_local_population_fallback:false",
                "hive_stock_local_engagement_fallback:false",
                "hive_stock_mutation:false",
                "hive_stock_read_source:" + LastReadSource.ToString().ToLowerInvariant()
            };
        }

        public async Task<RemoteHiveStockSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            string path = Path(hiveId);
            try
            {
                SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
                var request = new AuthenticatedGameRestRequest("GET", path);
                RemoteHiveStockSnapshot snapshot =
                    await SendWithSingleAuthenticationRefreshAsync<RemoteHiveStockSnapshot>(
                        request, context, cancellationToken).ConfigureAwait(false);
                ValidateSnapshot(snapshot, context.PlayerId, hiveId);
                LastReadSource = GameReadSource.Server;
                LastReadCachedAtUtc = default(DateTimeOffset);
                await SaveValidatedReadBestEffortAsync(
                    context.PlayerId, hiveId, path, snapshot, cancellationToken).ConfigureAwait(false);
                return snapshot;
            }
            catch (Exception exception) when (IsOfflineEligible(exception))
            {
                RemoteHiveStockSnapshot cached =
                    await TryLoadCacheAsync(hiveId, path, cancellationToken).ConfigureAwait(false);
                if (cached != null) return cached;
                throw;
            }
        }

        private async Task<SessionContext> RequireSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.NotConfigured,
                    "Official account session transport is not ready.");

            IRefreshableGameAccountSessionSource refreshable =
                sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable != null)
            {
                try
                {
                    return RequireUsableSession(
                        await refreshable.GetFreshSessionAsync(cancellationToken).ConfigureAwait(false));
                }
                catch (OperationCanceledException) { throw; }
                catch (MobileAccountSessionException exception) { throw MapSessionFailure(exception); }
            }

            GameAccountSession session;
            if (!sessionSource.TryGetSession(out session)) session = null;
            return RequireUsableSession(session);
        }

        private async Task<T> SendWithSingleAuthenticationRefreshAsync<T>(
            AuthenticatedGameRestRequest request,
            SessionContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                return await transport.SendAsync<T>(
                    request, context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error != AuthenticatedGameRestError.Unauthorized)
                    throw MapTransportFailure(exception);
            }

            IRefreshableGameAccountSessionSource refreshable =
                sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable == null)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.AuthenticationRequired,
                    "The game session was rejected.");

            GameAccountSession replacement;
            try
            {
                replacement = await refreshable.RefreshAfterUnauthorizedAsync(
                    context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (MobileAccountSessionException exception) { throw MapSessionFailure(exception); }

            if (replacement == null || replacement.PlayerId != context.PlayerId ||
                string.IsNullOrWhiteSpace(replacement.AccessToken) ||
                replacement.AccessToken.Length > 8192)
            {
                await refreshable.InvalidateUnauthorizedSessionAsync(
                    context.AccessToken, cancellationToken).ConfigureAwait(false);
                throw InvalidResponse("The refreshed game session changed identity.");
            }

            try
            {
                return await transport.SendAsync<T>(
                    request, replacement.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                {
                    await refreshable.InvalidateUnauthorizedSessionAsync(
                        replacement.AccessToken, cancellationToken).ConfigureAwait(false);
                    throw new HivePerimeterClientException(
                        HivePerimeterClientError.AuthenticationRequired,
                        "The refreshed game session was rejected.");
                }
                throw MapTransportFailure(exception);
            }
        }

        private async Task SaveValidatedReadBestEffortAsync(
            Guid playerId,
            Guid hiveId,
            string path,
            RemoteHiveStockSnapshot snapshot,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return;
            try
            {
                await readCache.SaveValidatedReadAsync(
                    playerId, hiveId, ContractVersion, path, snapshot, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }

        private async Task<RemoteHiveStockSnapshot> TryLoadCacheAsync(
            Guid hiveId,
            string path,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return null;
            Guid playerId;
            if (!TryGetKnownPlayerId(out playerId)) return null;
            ProtectedGameReadCacheHit<RemoteHiveStockSnapshot> hit =
                await readCache.TryLoadAsync<RemoteHiveStockSnapshot>(
                    playerId, hiveId, ContractVersion, path, cancellationToken).ConfigureAwait(false);
            if (hit == null) return null;
            ValidateSnapshot(hit.Value, playerId, hiveId);
            LastReadSource = GameReadSource.ProtectedCache;
            LastReadCachedAtUtc = hit.CachedAtUtc;
            return hit.Value;
        }

        private bool TryGetKnownPlayerId(out Guid playerId)
        {
            IRefreshableGameAccountSessionSource refreshable =
                sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable != null &&
                refreshable.TryGetKnownPlayerId(out playerId) &&
                playerId != Guid.Empty)
                return true;
            GameAccountSession session;
            if (sessionSource.TryGetSession(out session) &&
                session != null &&
                session.PlayerId != Guid.Empty)
            {
                playerId = session.PlayerId;
                return true;
            }
            playerId = Guid.Empty;
            return false;
        }

        internal static void ValidateSnapshot(
            RemoteHiveStockSnapshot snapshot,
            Guid playerId,
            Guid hiveId)
        {
            if (snapshot == null || snapshot.PlayerId != playerId || snapshot.HiveId != hiveId)
                throw InvalidResponse("The hive stock snapshot belongs to another account or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal) ||
                !IsSafeToken(snapshot.CatalogVersion) ||
                snapshot.Revision < 0 ||
                !IsUtc(snapshot.ServerTimeUtc))
                throw InvalidResponse("The hive stock contract, catalog, revision, or time is invalid.");

            ValidateResource(snapshot.Honey, "honey");
            ValidateResource(snapshot.Wax, "wax");
            ValidateResource(snapshot.Pollen, "pollen");

            bool hasPopulation = snapshot.Population.HasValue;
            if (hasPopulation != snapshot.PopulationCapacity.HasValue ||
                hasPopulation && (snapshot.Population.Value < 0 ||
                    snapshot.PopulationCapacity.Value < snapshot.Population.Value))
                throw InvalidResponse("The hive population is incomplete or invalid.");

            if (snapshot.CompletedResearchIds == null ||
                snapshot.ActiveEngagements == null ||
                snapshot.CompletedResearchIds.Count > 64 ||
                snapshot.ActiveEngagements.Count > 64)
                throw InvalidResponse("The hive stock snapshot is incomplete or unbounded.");

            if (snapshot.CompletedResearchIds.Any(value => !IsSafeToken(value)) ||
                snapshot.CompletedResearchIds.Distinct(StringComparer.Ordinal).Count() !=
                    snapshot.CompletedResearchIds.Count)
                throw InvalidResponse("A completed research identifier is invalid or duplicated.");

            if (snapshot.ActiveEngagements.Select(item =>
                    item == null ? Guid.Empty : item.OperationId)
                .Distinct().Count() != snapshot.ActiveEngagements.Count)
                throw InvalidResponse("An active engagement is duplicated.");

            foreach (RemoteHiveStockEngagement engagement in snapshot.ActiveEngagements)
            {
                if (engagement == null ||
                    engagement.OperationId == Guid.Empty ||
                    !SupportedEngagementKinds.Contains(engagement.Kind) ||
                    !IsSafeToken(engagement.Key) ||
                    !IsUtc(engagement.StartedAtUtc) ||
                    !IsUtc(engagement.EndsAtUtc) ||
                    engagement.StartedAtUtc > snapshot.ServerTimeUtc ||
                    engagement.EndsAtUtc <= engagement.StartedAtUtc ||
                    engagement.EndsAtUtc - engagement.StartedAtUtc > MaximumEngagementDuration)
                    throw InvalidResponse("An active hive engagement is invalid.");
            }
        }

        private static void ValidateResource(RemoteHiveStockResource resource, string key)
        {
            if (resource == null ||
                resource.Amount < 0 ||
                resource.Capacity < resource.Amount)
                throw InvalidResponse("The " + key + " stock balance is invalid.");
        }

        private static SessionContext RequireUsableSession(GameAccountSession session)
        {
            if (session == null ||
                session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) ||
                session.AccessToken.Length > 8192)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.AuthenticationRequired,
                    "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static bool IsOfflineEligible(Exception exception)
        {
            HivePerimeterClientException client = exception as HivePerimeterClientException;
            return client != null &&
                (client.Error == HivePerimeterClientError.TransportFailure ||
                 client.Error == HivePerimeterClientError.NotConfigured);
        }

        private static HivePerimeterClientException MapSessionFailure(
            MobileAccountSessionException exception)
        {
            if (exception.Error == MobileAccountSessionError.TransportFailure)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == MobileAccountSessionError.NotConfigured)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.NotConfigured, exception.SafeCode);
            return new HivePerimeterClientException(
                HivePerimeterClientError.AuthenticationRequired, exception.SafeCode);
        }

        private static HivePerimeterClientException MapTransportFailure(
            AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.AuthenticationRequired, exception.SafeCode);
            return InvalidResponse(exception.SafeCode);
        }

        private static bool IsSafeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Trim() != value ||
                value.Length > 64)
                return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') ||
                    character == '.' ||
                    character == '_' ||
                    character == '-'))
                    return false;
            }
            return true;
        }

        private static bool IsUtc(DateTimeOffset value)
        {
            return value != default(DateTimeOffset) && value.Offset == TimeSpan.Zero;
        }

        private static string Path(Guid hiveId)
        {
            return "/game/v1/hives/" + hiveId.ToString("D") + "/hive-stock";
        }

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.InvalidRequest,
                    "A hive identifier is required.");
        }

        private static HivePerimeterClientException InvalidResponse(string message)
        {
            return new HivePerimeterClientException(
                HivePerimeterClientError.InvalidResponse, message);
        }

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
