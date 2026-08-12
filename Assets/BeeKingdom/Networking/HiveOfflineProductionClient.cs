using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteOfflineProductionBalance
    {
        public long Amount { get; set; }
        public long Capacity { get; set; }
    }

    public sealed class RemoteOfflineProductionLine
    {
        public string BuildingKey { get; set; }
        public string ResourceKey { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal HourlyRate { get; set; }
        public long Capacity { get; set; }
        public long CollectableWholeUnits { get; set; }
    }

    public sealed class RemoteOfflineProductionSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public string CatalogVersion { get; set; }
        public long ProductionRevision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public DateTimeOffset ProductionAsOfUtc { get; set; }
        public TimeSpan MaxRecognizedDuration { get; set; }
        public List<RemoteOfflineProductionLine> Lines { get; set; }
        public Dictionary<string, RemoteOfflineProductionBalance> Balances { get; set; }
    }

    public sealed class OfflineProductionCollectRequest
    {
        public long ExpectedProductionRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteOfflineProductionReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string IdempotencyKey { get; set; }
        public string BuildingKey { get; set; }
        public string ResourceKey { get; set; }
        public long CreditedAmount { get; set; }
        public decimal RemainingPending { get; set; }
        public long ProductionRevision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public RemoteOfflineProductionBalance ResultingBalance { get; set; }
    }

    public sealed class RemoteOfflineProductionCollectResponse
    {
        public RemoteOfflineProductionReceipt Receipt { get; set; }
        public RemoteOfflineProductionSnapshot Snapshot { get; set; }
    }

    public interface IHiveOfflineProductionClient
    {
        GameReadSource LastReadSource { get; }
        DateTimeOffset LastReadCachedAtUtc { get; }
        Task<RemoteOfflineProductionSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteOfflineProductionCollectResponse> CollectAsync(
            Guid hiveId,
            string buildingKey,
            long expectedProductionRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public sealed class HiveOfflineProductionClient : IHiveOfflineProductionClient
    {
        public const string ContractVersion = "living-hive-offline-production-v1";
        public static readonly TimeSpan MaximumRecognizedDuration = TimeSpan.FromDays(7);

        private const decimal MaximumHourlyRate = 1000000m;
        private const long MaximumLineCapacity = 1000000000L;
        private static readonly IReadOnlyDictionary<string, string> BuildingResources =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["honey_storage"] = "honey",
                ["wax_workshop"] = "wax",
                ["warehouse_cells"] = "pollen"
            };

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;
        private readonly ProtectedGameReadCache readCache;

        public HiveOfflineProductionClient(
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
                "mobile_offline_production_contract:" + ContractVersion,
                "production_time_authority:server",
                "production_balance_authority:server",
                "production_collection_authority:server",
                "production_get_protected_cache:" + (readCache != null && readCache.IsProtectionAvailable).ToString().ToLowerInvariant(),
                "production_cache_read_only:true",
                "production_mutation_offline_retry:false",
                "production_local_credit:false",
                "production_read_source:" + LastReadSource.ToString().ToLowerInvariant()
            };
        }

        public async Task<RemoteOfflineProductionSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            string path = Path(hiveId);
            try
            {
                SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
                var request = new AuthenticatedGameRestRequest("GET", path);
                RemoteOfflineProductionSnapshot snapshot =
                    await SendWithSingleAuthenticationRefreshAsync<RemoteOfflineProductionSnapshot>(request, context, cancellationToken)
                        .ConfigureAwait(false);
                ValidateSnapshot(snapshot, context.PlayerId, hiveId);
                LastReadSource = GameReadSource.Server;
                LastReadCachedAtUtc = default(DateTimeOffset);
                await SaveValidatedReadBestEffortAsync(context.PlayerId, hiveId, path, snapshot, cancellationToken)
                    .ConfigureAwait(false);
                return snapshot;
            }
            catch (Exception exception) when (IsOfflineEligible(exception))
            {
                RemoteOfflineProductionSnapshot cached = await TryLoadCacheAsync(hiveId, path, cancellationToken)
                    .ConfigureAwait(false);
                if (cached != null) return cached;
                throw;
            }
        }

        public async Task<RemoteOfflineProductionCollectResponse> CollectAsync(
            Guid hiveId,
            string buildingKey,
            long expectedProductionRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            string resourceKey;
            if (string.IsNullOrWhiteSpace(buildingKey) || !BuildingResources.TryGetValue(buildingKey, out resourceKey))
                throw InvalidRequest("The production building is unsupported.");
            if (expectedProductionRevision < 0 || expectedProductionRevision == long.MaxValue)
                throw InvalidRequest("The expected production revision is outside the supported range.");
            RequireIdempotencyKey(idempotencyKey);

            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                Path(hiveId) + "/" + buildingKey + "/collect",
                new OfflineProductionCollectRequest
                {
                    ExpectedProductionRevision = expectedProductionRevision,
                    IdempotencyKey = idempotencyKey
                });
            RemoteOfflineProductionCollectResponse response =
                await SendWithSingleAuthenticationRefreshAsync<RemoteOfflineProductionCollectResponse>(request, context, cancellationToken)
                    .ConfigureAwait(false);
            ValidateCollectResponse(
                response,
                context.PlayerId,
                hiveId,
                buildingKey,
                resourceKey,
                expectedProductionRevision,
                idempotencyKey);
            LastReadSource = GameReadSource.Server;
            LastReadCachedAtUtc = default(DateTimeOffset);
            return response;
        }

        private async Task<SessionContext> RequireSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.NotConfigured,
                    "Official account session transport is not ready.");

            IRefreshableGameAccountSessionSource refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable != null)
            {
                try
                {
                    return RequireUsableSession(await refreshable.GetFreshSessionAsync(cancellationToken).ConfigureAwait(false));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (MobileAccountSessionException exception)
                {
                    throw MapSessionFailure(exception);
                }
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
                return await transport.SendAsync<T>(request, context.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error != AuthenticatedGameRestError.Unauthorized)
                    throw MapTransportFailure(exception);
            }

            IRefreshableGameAccountSessionSource refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable == null)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.AuthenticationRequired,
                    "The game session was rejected.");

            GameAccountSession replacement;
            try
            {
                replacement = await refreshable.RefreshAfterUnauthorizedAsync(context.AccessToken, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MobileAccountSessionException exception)
            {
                throw MapSessionFailure(exception);
            }

            if (replacement == null || replacement.PlayerId != context.PlayerId ||
                string.IsNullOrWhiteSpace(replacement.AccessToken) || replacement.AccessToken.Length > 8192)
            {
                await refreshable.InvalidateUnauthorizedSessionAsync(context.AccessToken, cancellationToken)
                    .ConfigureAwait(false);
                throw InvalidResponse("The refreshed game session changed identity.");
            }

            try
            {
                return await transport.SendAsync<T>(request, replacement.AccessToken, cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                {
                    await refreshable.InvalidateUnauthorizedSessionAsync(replacement.AccessToken, cancellationToken)
                        .ConfigureAwait(false);
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
            RemoteOfflineProductionSnapshot snapshot,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return;
            try
            {
                await readCache.SaveValidatedReadAsync(
                    playerId,
                    hiveId,
                    ContractVersion,
                    path,
                    snapshot,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }
        }

        private async Task<RemoteOfflineProductionSnapshot> TryLoadCacheAsync(
            Guid hiveId,
            string path,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return null;
            Guid playerId;
            if (!TryGetKnownPlayerId(out playerId)) return null;
            ProtectedGameReadCacheHit<RemoteOfflineProductionSnapshot> hit =
                await readCache.TryLoadAsync<RemoteOfflineProductionSnapshot>(
                    playerId,
                    hiveId,
                    ContractVersion,
                    path,
                    cancellationToken).ConfigureAwait(false);
            if (hit == null) return null;
            ValidateSnapshot(hit.Value, playerId, hiveId);
            LastReadSource = GameReadSource.ProtectedCache;
            LastReadCachedAtUtc = hit.CachedAtUtc;
            return hit.Value;
        }

        private bool TryGetKnownPlayerId(out Guid playerId)
        {
            IRefreshableGameAccountSessionSource refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable != null && refreshable.TryGetKnownPlayerId(out playerId) && playerId != Guid.Empty)
                return true;
            GameAccountSession session;
            if (sessionSource.TryGetSession(out session) && session != null && session.PlayerId != Guid.Empty)
            {
                playerId = session.PlayerId;
                return true;
            }
            playerId = Guid.Empty;
            return false;
        }

        private static void ValidateCollectResponse(
            RemoteOfflineProductionCollectResponse response,
            Guid playerId,
            Guid hiveId,
            string buildingKey,
            string resourceKey,
            long expectedProductionRevision,
            string idempotencyKey)
        {
            if (response == null || response.Receipt == null || response.Snapshot == null)
                throw InvalidResponse("The production collection response is incomplete.");
            ValidateSnapshot(response.Snapshot, playerId, hiveId);
            RemoteOfflineProductionReceipt receipt = response.Receipt;
            RemoteOfflineProductionLine line = response.Snapshot.Lines.Single(item => item.BuildingKey == buildingKey);
            RemoteOfflineProductionBalance balance = response.Snapshot.Balances[resourceKey];

            if (receipt.PlayerId != playerId || receipt.HiveId != hiveId ||
                !string.Equals(receipt.BuildingKey, buildingKey, StringComparison.Ordinal) ||
                !string.Equals(receipt.ResourceKey, resourceKey, StringComparison.Ordinal) ||
                !string.Equals(receipt.IdempotencyKey, idempotencyKey, StringComparison.Ordinal) ||
                receipt.CreditedAmount <= 0 || receipt.CreditedAmount > MaximumLineCapacity ||
                receipt.RemainingPending != line.PendingAmount ||
                receipt.ProductionRevision != checked(expectedProductionRevision + 1) ||
                receipt.ProductionRevision != response.Snapshot.ProductionRevision ||
                receipt.ServerTimeUtc != response.Snapshot.ServerTimeUtc ||
                receipt.ResultingBalance == null ||
                receipt.ResultingBalance.Amount != balance.Amount ||
                receipt.ResultingBalance.Capacity != balance.Capacity ||
                balance.Amount < receipt.CreditedAmount)
                throw InvalidResponse("The production collection receipt is inconsistent.");
        }

        private static void ValidateSnapshot(
            RemoteOfflineProductionSnapshot snapshot,
            Guid playerId,
            Guid hiveId)
        {
            if (snapshot == null || snapshot.PlayerId != playerId || snapshot.HiveId != hiveId)
                throw InvalidResponse("The production snapshot belongs to another account or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal) ||
                !IsSafeToken(snapshot.CatalogVersion))
                throw InvalidResponse("The production contract or catalog is unsupported.");
            if (snapshot.ProductionRevision < 0 || !IsUtc(snapshot.ServerTimeUtc) ||
                !IsUtc(snapshot.ProductionAsOfUtc) || snapshot.ProductionAsOfUtc > snapshot.ServerTimeUtc ||
                snapshot.MaxRecognizedDuration <= TimeSpan.Zero ||
                snapshot.MaxRecognizedDuration > MaximumRecognizedDuration)
                throw InvalidResponse("The production revision or server time is invalid.");
            if (snapshot.Lines == null || snapshot.Lines.Count != BuildingResources.Count ||
                snapshot.Balances == null || snapshot.Balances.Count != BuildingResources.Count)
                throw InvalidResponse("Exactly the three production lines and resource balances are required.");
            if (snapshot.Lines.Any(line => line == null || !BuildingResources.ContainsKey(line.BuildingKey)) ||
                snapshot.Lines.Select(line => line.BuildingKey).Distinct(StringComparer.Ordinal).Count() != BuildingResources.Count ||
                snapshot.Balances.Any(balance => !BuildingResources.Values.Contains(balance.Key)))
                throw InvalidResponse("The production snapshot contains an unknown or duplicate key.");

            foreach (KeyValuePair<string, string> mapping in BuildingResources)
            {
                RemoteOfflineProductionLine line = snapshot.Lines.Single(
                    item => string.Equals(item.BuildingKey, mapping.Key, StringComparison.Ordinal));
                RemoteOfflineProductionBalance balance;
                if (line == null || !string.Equals(line.ResourceKey, mapping.Value, StringComparison.Ordinal) ||
                    !snapshot.Balances.TryGetValue(mapping.Value, out balance) || balance == null)
                    throw InvalidResponse("The production building and resource mapping is invalid.");
                if (line.HourlyRate <= 0m || line.HourlyRate > MaximumHourlyRate ||
                    line.Capacity <= 0 || line.Capacity > MaximumLineCapacity ||
                    line.PendingAmount < 0m || line.PendingAmount > line.Capacity ||
                    balance.Amount < 0 || balance.Capacity < 0 || balance.Amount > balance.Capacity)
                    throw InvalidResponse("A production quantity, rate, or capacity is invalid.");

                long whole = decimal.ToInt64(decimal.Floor(line.PendingAmount));
                long headroom = balance.Capacity - balance.Amount;
                long expectedCollectable = Math.Min(whole, headroom);
                if (line.CollectableWholeUnits != expectedCollectable)
                    throw InvalidResponse("The collectable production amount is inconsistent.");
            }

        }

        private static SessionContext RequireUsableSession(GameAccountSession session)
        {
            if (session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.AuthenticationRequired,
                    "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static bool IsOfflineEligible(Exception exception)
        {
            HivePerimeterClientException client = exception as HivePerimeterClientException;
            if (client != null)
                return client.Error == HivePerimeterClientError.TransportFailure ||
                    client.Error == HivePerimeterClientError.NotConfigured;
            AuthenticatedGameRestException transportFailure = exception as AuthenticatedGameRestException;
            return transportFailure != null && transportFailure.Error == AuthenticatedGameRestError.NetworkFailure;
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
            return InvalidResponse(exception.SafeCode);
        }

        private static bool IsSafeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64) return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= 'a' && character <= 'z') ||
                    (character >= '0' && character <= '9') || character == '.' || character == '_' || character == '-'))
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
            return "/game/v1/hives/" + hiveId.ToString("D") + "/offline-production";
        }

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required.");
        }

        private static void RequireIdempotencyKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256)
                throw InvalidRequest("The idempotency key must contain between one and 256 characters.");
        }

        private static HivePerimeterClientException InvalidRequest(string message)
        {
            return new HivePerimeterClientException(HivePerimeterClientError.InvalidRequest, message);
        }

        private static HivePerimeterClientException InvalidResponse(string message)
        {
            return new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, message);
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
