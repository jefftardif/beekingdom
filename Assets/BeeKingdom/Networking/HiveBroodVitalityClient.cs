using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteBroodVitalityOperation
    {
        public Guid OperationId { get; set; }
        public string Type { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndsAtUtc { get; set; }
    }

    public sealed class RemoteBroodVitalityState
    {
        public int Nutrition { get; set; }
        public int Stability { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public RemoteBroodVitalityOperation ActiveOperation { get; set; }
    }

    public sealed class RemoteBroodVitalitySnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public long GlobalRevision { get; set; }
        public RemoteBroodVitalityState Vitality { get; set; }
    }

    public sealed class BroodVitalityCareMutationRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteBroodVitalityCareReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string IdempotencyKey { get; set; }
        public Guid OperationId { get; set; }
        public string Type { get; set; }
        public long RevisionBefore { get; set; }
        public long RevisionAfter { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public string Code { get; set; }
    }

    public sealed class RemoteBroodVitalityCareResponse
    {
        public RemoteBroodVitalityCareReceipt Receipt { get; set; }
        public RemoteBroodVitalitySnapshot Snapshot { get; set; }
    }

    public interface IHiveBroodVitalityClient
    {
        GameReadSource LastReadSource { get; }
        DateTimeOffset LastReadCachedAtUtc { get; }
        Task<RemoteBroodVitalitySnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteBroodVitalityCareResponse> StartCareAsync(
            Guid hiveId,
            string type,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteBroodVitalityCareResponse> CompleteCareAsync(
            Guid hiveId,
            Guid operationId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public sealed class HiveBroodVitalityClient : IHiveBroodVitalityClient
    {
        public const string ContractVersion = "living-hive-brood-vitality-v1";
        public const string FeedingType = "feeding";
        public const string StabilizationType = "stabilization";
        public const string StartedCode = "game.vitality_care_started";
        public const string CompletedCode = "game.vitality_care_completed";
        public const long FeedingHoneyCost = 300;
        public const long StabilizationWaxCost = 45;
        public const int FeedingNutritionGain = 22;
        public const int StabilizationStabilityGain = 7;
        public const int FeedingDurationSeconds = 12;
        public const int StabilizationDurationSeconds = 13;

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;
        private readonly ProtectedGameReadCache readCache;

        public HiveBroodVitalityClient(
            MobileAccountSessionGate sessionGate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport,
            ProtectedGameReadCache readCache = null)
        {
            this.sessionGate =
                sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource =
                sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport =
                transport ?? throw new ArgumentNullException(nameof(transport));
            this.readCache = readCache;
        }

        public GameReadSource LastReadSource { get; private set; }
        public DateTimeOffset LastReadCachedAtUtc { get; private set; }

        public IReadOnlyList<string> ProofRows()
        {
            return new[]
            {
                "mobile_brood_vitality_contract:" + ContractVersion,
                "brood_vitality_authority:server",
                "brood_vitality_cache:protected_read_only",
                "brood_vitality_uninitialized:null",
                "brood_care_cost_authority:server",
                "brood_care_timer_authority:server_utc",
                "brood_care_revision_required:true",
                "brood_care_idempotency_required:true",
                "brood_care_offline_submission:false",
                "brood_care_local_resource_debit:false",
                "brood_care_local_vitality_credit:false",
                "brood_vitality_read_source:" +
                    LastReadSource.ToString().ToLowerInvariant()
            };
        }

        public async Task<RemoteBroodVitalitySnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            string path = Path(hiveId);
            try
            {
                SessionContext context =
                    await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
                RemoteBroodVitalitySnapshot snapshot =
                    await SendWithSingleAuthenticationRefreshAsync<RemoteBroodVitalitySnapshot>(
                        new AuthenticatedGameRestRequest("GET", path),
                        context,
                        cancellationToken).ConfigureAwait(false);
                ValidateSnapshot(snapshot, context.PlayerId, hiveId);
                LastReadSource = GameReadSource.Server;
                LastReadCachedAtUtc = default(DateTimeOffset);
                await SaveValidatedReadBestEffortAsync(
                    context.PlayerId,
                    hiveId,
                    path,
                    snapshot,
                    cancellationToken).ConfigureAwait(false);
                return snapshot;
            }
            catch (Exception exception) when (IsOfflineEligible(exception))
            {
                RemoteBroodVitalitySnapshot cached =
                    await TryLoadCacheAsync(
                        hiveId,
                        path,
                        cancellationToken).ConfigureAwait(false);
                if (cached != null) return cached;
                throw;
            }
        }

        public Task<RemoteBroodVitalityCareResponse> StartCareAsync(
            Guid hiveId,
            string type,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireCareType(type);
            return MutateAsync(
                hiveId,
                StartPath(hiveId, type),
                type,
                Guid.Empty,
                expectedRevision,
                idempotencyKey,
                StartedCode,
                cancellationToken);
        }

        public Task<RemoteBroodVitalityCareResponse> CompleteCareAsync(
            Guid hiveId,
            Guid operationId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (operationId == Guid.Empty)
                throw InvalidRequest("A care operation identifier is required.");
            return MutateAsync(
                hiveId,
                CompletePath(hiveId, operationId),
                string.Empty,
                operationId,
                expectedRevision,
                idempotencyKey,
                CompletedCode,
                cancellationToken);
        }

        internal static void ValidateSnapshot(
            RemoteBroodVitalitySnapshot snapshot,
            Guid playerId,
            Guid hiveId)
        {
            if (snapshot == null ||
                snapshot.PlayerId != playerId ||
                snapshot.HiveId != hiveId)
                throw InvalidResponse(
                    "The brood vitality snapshot belongs to another account or hive.");
            if (!string.Equals(
                    snapshot.ContractVersion,
                    ContractVersion,
                    StringComparison.Ordinal) ||
                snapshot.GlobalRevision < 0 ||
                !IsUtc(snapshot.ServerTimeUtc))
                throw InvalidResponse(
                    "The brood vitality contract, revision, or server time is invalid.");

            RemoteBroodVitalityState vitality = snapshot.Vitality;
            if (vitality == null) return;
            if (vitality.Nutrition < 0 ||
                vitality.Nutrition > 100 ||
                vitality.Stability < 0 ||
                vitality.Stability > 100 ||
                vitality.Revision < 0 ||
                vitality.Revision > snapshot.GlobalRevision ||
                !IsUtc(vitality.UpdatedAtUtc) ||
                vitality.UpdatedAtUtc > snapshot.ServerTimeUtc)
                throw InvalidResponse("The brood vitality values are invalid.");

            RemoteBroodVitalityOperation operation = vitality.ActiveOperation;
            if (operation == null) return;
            RequireValidRemoteOperation(operation, snapshot.ServerTimeUtc);
        }

        internal static void ValidateMutationResponse(
            RemoteBroodVitalityCareResponse response,
            Guid playerId,
            Guid hiveId,
            string expectedType,
            Guid expectedOperationId,
            long expectedRevision,
            string idempotencyKey,
            string expectedCode)
        {
            if (response == null ||
                response.Receipt == null ||
                response.Snapshot == null)
                throw InvalidResponse(
                    "The brood vitality mutation response is incomplete.");
            ValidateSnapshot(response.Snapshot, playerId, hiveId);
            RemoteBroodVitalityCareReceipt receipt = response.Receipt;
            if (receipt.PlayerId != playerId ||
                receipt.HiveId != hiveId ||
                receipt.OperationId == Guid.Empty ||
                !IsCareType(receipt.Type) ||
                !string.Equals(
                    receipt.IdempotencyKey,
                    idempotencyKey,
                    StringComparison.Ordinal) ||
                receipt.RevisionBefore != expectedRevision ||
                receipt.RevisionAfter != expectedRevision + 1 ||
                response.Snapshot.GlobalRevision < receipt.RevisionAfter ||
                !IsUtc(receipt.AcceptedAtUtc) ||
                receipt.AcceptedAtUtc > response.Snapshot.ServerTimeUtc ||
                !string.Equals(receipt.Code, expectedCode, StringComparison.Ordinal))
                throw InvalidResponse("The brood vitality receipt is invalid.");

            if (string.Equals(expectedCode, StartedCode, StringComparison.Ordinal))
            {
                if (!string.Equals(
                        receipt.Type,
                        expectedType,
                        StringComparison.Ordinal) ||
                    expectedOperationId != Guid.Empty)
                    throw InvalidResponse(
                        "The started care operation does not match its receipt.");
                if (response.Snapshot.GlobalRevision == receipt.RevisionAfter &&
                    (response.Snapshot.Vitality == null ||
                     response.Snapshot.Vitality.ActiveOperation == null ||
                     response.Snapshot.Vitality.ActiveOperation.OperationId !=
                        receipt.OperationId ||
                     !string.Equals(
                         response.Snapshot.Vitality.ActiveOperation.Type,
                         receipt.Type,
                         StringComparison.Ordinal)))
                    throw InvalidResponse(
                        "The fresh started care snapshot is inconsistent.");
            }
            else if (receipt.OperationId != expectedOperationId)
            {
                throw InvalidResponse(
                    "The completed care operation does not match its receipt.");
            }
            else if (response.Snapshot.GlobalRevision == receipt.RevisionAfter &&
                (response.Snapshot.Vitality == null ||
                 response.Snapshot.Vitality.ActiveOperation != null))
                throw InvalidResponse(
                    "The fresh completed care snapshot is inconsistent.");
        }

        private async Task<RemoteBroodVitalityCareResponse> MutateAsync(
            Guid hiveId,
            string path,
            string expectedType,
            Guid expectedOperationId,
            long expectedRevision,
            string idempotencyKey,
            string expectedCode,
            CancellationToken cancellationToken)
        {
            RequireHive(hiveId);
            RequireRevision(expectedRevision);
            RequireIdempotencyKey(idempotencyKey);
            SessionContext context =
                await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                path,
                new BroodVitalityCareMutationRequest
                {
                    ExpectedRevision = expectedRevision,
                    IdempotencyKey = idempotencyKey
                });
            RemoteBroodVitalityCareResponse response =
                await SendWithSingleAuthenticationRefreshAsync
                    <RemoteBroodVitalityCareResponse>(
                        request,
                        context,
                        cancellationToken).ConfigureAwait(false);
            ValidateMutationResponse(
                response,
                context.PlayerId,
                hiveId,
                expectedType,
                expectedOperationId,
                expectedRevision,
                idempotencyKey,
                expectedCode);
            LastReadSource = GameReadSource.Server;
            LastReadCachedAtUtc = default(DateTimeOffset);
            await SaveValidatedReadBestEffortAsync(
                context.PlayerId,
                hiveId,
                Path(hiveId),
                response.Snapshot,
                CancellationToken.None).ConfigureAwait(false);
            return response;
        }

        private async Task<SessionContext> RequireSessionAsync(
            CancellationToken cancellationToken)
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
                        await refreshable.GetFreshSessionAsync(cancellationToken)
                            .ConfigureAwait(false));
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
                return await transport.SendAsync<T>(
                    request,
                    context.AccessToken,
                    cancellationToken).ConfigureAwait(false);
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
                    context.AccessToken,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MobileAccountSessionException exception)
            {
                throw MapSessionFailure(exception);
            }

            if (replacement == null ||
                replacement.PlayerId != context.PlayerId ||
                string.IsNullOrWhiteSpace(replacement.AccessToken) ||
                replacement.AccessToken.Length > 8192)
            {
                await refreshable.InvalidateUnauthorizedSessionAsync(
                    context.AccessToken,
                    cancellationToken).ConfigureAwait(false);
                throw InvalidResponse("The refreshed game session changed identity.");
            }

            try
            {
                return await transport.SendAsync<T>(
                    request,
                    replacement.AccessToken,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                {
                    await refreshable.InvalidateUnauthorizedSessionAsync(
                        replacement.AccessToken,
                        cancellationToken).ConfigureAwait(false);
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
            RemoteBroodVitalitySnapshot snapshot,
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

        private async Task<RemoteBroodVitalitySnapshot> TryLoadCacheAsync(
            Guid hiveId,
            string path,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return null;
            Guid playerId;
            if (!TryGetKnownPlayerId(out playerId)) return null;
            ProtectedGameReadCacheHit<RemoteBroodVitalitySnapshot> hit =
                await readCache.TryLoadAsync<RemoteBroodVitalitySnapshot>(
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

        private static void RequireValidRemoteOperation(
            RemoteBroodVitalityOperation operation,
            DateTimeOffset serverTimeUtc)
        {
            if (operation.OperationId == Guid.Empty ||
                !IsCareType(operation.Type) ||
                !IsUtc(operation.StartedAtUtc) ||
                !IsUtc(operation.EndsAtUtc) ||
                operation.StartedAtUtc > serverTimeUtc ||
                operation.EndsAtUtc <= operation.StartedAtUtc)
                throw InvalidResponse("The brood vitality operation is invalid.");
            int duration = string.Equals(
                operation.Type,
                FeedingType,
                StringComparison.Ordinal)
                ? FeedingDurationSeconds
                : StabilizationDurationSeconds;
            if (operation.EndsAtUtc != operation.StartedAtUtc.AddSeconds(duration))
                throw InvalidResponse(
                    "The brood vitality operation duration is invalid.");
        }

        private static bool IsOfflineEligible(Exception exception)
        {
            HivePerimeterClientException client =
                exception as HivePerimeterClientException;
            return client != null &&
                (client.Error == HivePerimeterClientError.TransportFailure ||
                 client.Error == HivePerimeterClientError.NotConfigured);
        }

        private static HivePerimeterClientException MapSessionFailure(
            MobileAccountSessionException exception)
        {
            if (exception.Error == MobileAccountSessionError.TransportFailure)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.TransportFailure,
                    exception.SafeCode);
            if (exception.Error == MobileAccountSessionError.NotConfigured)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.NotConfigured,
                    exception.SafeCode);
            return new HivePerimeterClientException(
                HivePerimeterClientError.AuthenticationRequired,
                exception.SafeCode);
        }

        private static HivePerimeterClientException MapTransportFailure(
            AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.TransportFailure,
                    exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.AuthenticationRequired,
                    exception.SafeCode);
            return InvalidResponse(exception.SafeCode);
        }

        public static string Path(Guid hiveId)
        {
            RequireHive(hiveId);
            return "/game/v1/hives/" + hiveId.ToString("D") +
                "/brood/vitality";
        }

        public static string StartPath(Guid hiveId, string type)
        {
            RequireHive(hiveId);
            RequireCareType(type);
            return Path(hiveId) + "/care/start?type=" + type;
        }

        public static string CompletePath(Guid hiveId, Guid operationId)
        {
            RequireHive(hiveId);
            if (operationId == Guid.Empty)
                throw InvalidRequest("A care operation identifier is required.");
            return Path(hiveId) + "/care/" + operationId.ToString("D") +
                "/complete";
        }

        private static bool IsCareType(string type)
        {
            return string.Equals(type, FeedingType, StringComparison.Ordinal) ||
                string.Equals(
                    type,
                    StabilizationType,
                    StringComparison.Ordinal);
        }

        private static void RequireCareType(string type)
        {
            if (!IsCareType(type))
                throw InvalidRequest("A known brood care type is required.");
        }

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty)
                throw InvalidRequest("A hive identifier is required.");
        }

        private static void RequireRevision(long revision)
        {
            if (revision < 0 || revision == long.MaxValue)
                throw InvalidRequest("A bounded brood vitality revision is required.");
        }

        private static void RequireIdempotencyKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Trim() != value ||
                value.Length > 256)
                throw InvalidRequest("A bounded idempotency key is required.");
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (character < 0x21 ||
                    character > 0x7e ||
                    character == '"' ||
                    character == '\\')
                    throw InvalidRequest("The idempotency key contains unsafe characters.");
            }
        }

        private static bool IsUtc(DateTimeOffset value)
        {
            return value != default(DateTimeOffset) &&
                value.Offset == TimeSpan.Zero;
        }

        private static HivePerimeterClientException InvalidRequest(string message)
        {
            return new HivePerimeterClientException(
                HivePerimeterClientError.InvalidRequest,
                message);
        }

        private static HivePerimeterClientException InvalidResponse(string message)
        {
            return new HivePerimeterClientException(
                HivePerimeterClientError.InvalidResponse,
                message);
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
