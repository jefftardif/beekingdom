using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public enum HivePerimeterClientError
    {
        NotConfigured = 0,
        AuthenticationRequired = 1,
        InvalidRequest = 2,
        InvalidResponse = 3,
        TransportFailure = 4
    }

    public sealed class HivePerimeterClientException : Exception
    {
        public HivePerimeterClientException(HivePerimeterClientError error, string message)
            : base(message)
        {
            Error = error;
        }

        public HivePerimeterClientError Error { get; }
    }

    public sealed class GameAccountSession
    {
        public GameAccountSession(Guid playerId, string accessToken, bool isNewAccount = false, string displayName = null, bool isOnboarded = false)
        {
            PlayerId = playerId;
            AccessToken = accessToken ?? string.Empty;
            IsNewAccount = isNewAccount;
            DisplayName = displayName;
            IsOnboarded = isOnboarded;
        }

        public Guid PlayerId { get; }
        public string AccessToken { get; }
        public bool IsNewAccount { get; }
        public string DisplayName { get; }
        public bool IsOnboarded { get; }
    }

    public interface IGameAccountSessionSource
    {
        bool TryGetSession(out GameAccountSession session);
    }

    public interface IRefreshableGameAccountSessionSource : IGameAccountSessionSource
    {
        bool TryGetKnownPlayerId(out Guid playerId);
        Task<GameAccountSession> GetFreshSessionAsync(CancellationToken cancellationToken);
        Task<GameAccountSession> RefreshAfterUnauthorizedAsync(
            string rejectedAccessToken,
            CancellationToken cancellationToken);
        Task InvalidateUnauthorizedSessionAsync(
            string rejectedAccessToken,
            CancellationToken cancellationToken);
    }

    public enum GameReadSource
    {
        None = 0,
        Server = 1,
        ProtectedCache = 2
    }

    public sealed class AuthenticatedGameRestRequest
    {
        public AuthenticatedGameRestRequest(string method, string path, object body = null)
        {
            Method = method ?? string.Empty;
            Path = path ?? string.Empty;
            Body = body;
        }

        public string Method { get; }
        public string Path { get; }
        public object Body { get; }

        public override string ToString()
        {
            return Method + " " + Path;
        }
    }

    public interface IAuthenticatedGameRestTransport
    {
        Task<T> SendAsync<T>(
            AuthenticatedGameRestRequest request,
            string bearerAccessToken,
            CancellationToken cancellationToken);
    }

    public sealed class SquadReservationMutationRequest
    {
        public long ExpectedRevision { get; set; }
        public Dictionary<string, long> Quantities { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class SquadReservationReleaseRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class HivePerimeterLaunchRequest
    {
        public string SignalKey { get; set; }
        public string SignalInstanceId { get; set; }
        public string ReservationId { get; set; }
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class HivePerimeterMutationRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteSquadReservationSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public string CatalogVersion { get; set; }
        public long RosterRevision { get; set; }
        public long ReservationRevision { get; set; }
        public int Capacity { get; set; }
        public Dictionary<string, long> Roster { get; set; }
        public Dictionary<string, long> Available { get; set; }
        public Dictionary<string, long> Reserved { get; set; }
        public string ReservationId { get; set; }
    }

    public sealed class RemoteSquadReservationReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string IdempotencyKey { get; set; }
        public string Action { get; set; }
        public string ReservationId { get; set; }
        public Dictionary<string, long> Quantities { get; set; }
        public long ReservationRevisionBefore { get; set; }
        public long ReservationRevisionAfter { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public string Code { get; set; }
    }

    public sealed class RemoteSquadReservationResponse
    {
        public RemoteSquadReservationReceipt Receipt { get; set; }
        public RemoteSquadReservationSnapshot Snapshot { get; set; }
    }

    public interface IHiveSquadReservationClient
    {
        GameReadSource LastReadSource { get; }
        DateTimeOffset LastReadCachedAtUtc { get; }
        Task<RemoteSquadReservationSnapshot> ReadReservationAsync(
            Guid hiveId,
            CancellationToken cancellationToken =
                default(CancellationToken));
        Task<RemoteSquadReservationResponse>
            CommitReservationWithReceiptAsync(
                Guid hiveId,
                long expectedRevision,
                IReadOnlyDictionary<string, long> quantities,
                string idempotencyKey,
                CancellationToken cancellationToken =
                    default(CancellationToken));
        Task<RemoteSquadReservationResponse>
            ReleaseReservationWithReceiptAsync(
                Guid hiveId,
                long expectedRevision,
                string idempotencyKey,
                CancellationToken cancellationToken =
                    default(CancellationToken));
    }

    public sealed class RemoteHivePerimeterSignal
    {
        public string SignalKey { get; set; }
        public string SignalInstanceId { get; set; }
        public string HazardDoctrine { get; set; }
        public TimeSpan Duration { get; set; }
        public int MinimumSquad { get; set; }
        public long HoneyReward { get; set; }
        public long PollenReward { get; set; }
        public bool Completed { get; set; }
        public bool CanLaunch { get; set; }
    }

    public sealed class RemoteHivePerimeterActiveSortie
    {
        public Guid SortieId { get; set; }
        public string SignalKey { get; set; }
        public string SignalInstanceId { get; set; }
        public string ReservationId { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndsAtUtc { get; set; }
        public long Revision { get; set; }
    }

    public sealed class RemoteHiveResourceBalance
    {
        public long Amount { get; set; }
        public long Capacity { get; set; }
    }

    public sealed class RemoteHivePerimeterClaimReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public Guid SortieId { get; set; }
        public string SignalKey { get; set; }
        public string SignalInstanceId { get; set; }
        public DateTimeOffset CycleStartedAtUtc { get; set; }
        public DateTimeOffset CycleEndsAtUtc { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public Dictionary<string, long> CreditedByResource { get; set; }
        public Dictionary<string, RemoteHiveResourceBalance> ResultingBalances { get; set; }
    }

    public sealed class RemoteHivePerimeterSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public DateTimeOffset CycleStartedAtUtc { get; set; }
        public DateTimeOffset CycleEndsAtUtc { get; set; }
        public RemoteHivePerimeterActiveSortie Active { get; set; }
        public RemoteSquadReservationSnapshot Reservation { get; set; }
        public List<RemoteHivePerimeterSignal> Signals { get; set; }
        public RemoteHivePerimeterClaimReceipt ClaimReceipt { get; set; }
    }

    public sealed class RemoteHivePerimeterMutationReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string IdempotencyKey { get; set; }
        public string Action { get; set; }
        public Guid SortieId { get; set; }
        public string SignalKey { get; set; }
        public string SignalInstanceId { get; set; }
        public string ReservationId { get; set; }
        public DateTimeOffset CycleStartedAtUtc { get; set; }
        public DateTimeOffset CycleEndsAtUtc { get; set; }
        public long RevisionBefore { get; set; }
        public long RevisionAfter { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public string Code { get; set; }
        public Dictionary<string, long> CreditedByResource { get; set; }
        public Dictionary<string, RemoteHiveResourceBalance> ResultingBalances { get; set; }
    }

    public sealed class RemoteHivePerimeterMutationResponse
    {
        public RemoteHivePerimeterMutationReceipt Receipt { get; set; }
        public RemoteHivePerimeterSnapshot Snapshot { get; set; }
    }

    public interface IHivePerimeterSortieClient
    {
        GameReadSource LastReadSource { get; }
        DateTimeOffset LastReadCachedAtUtc { get; }
        Task<RemoteHivePerimeterSnapshot> ReadSortieBoardAsync(
            Guid hiveId,
            CancellationToken cancellationToken =
                default(CancellationToken));
        Task<RemoteHivePerimeterMutationResponse> LaunchWithReceiptAsync(
            Guid hiveId,
            string signalKey,
            string signalInstanceId,
            string reservationId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken =
                default(CancellationToken));
        Task<RemoteHivePerimeterMutationResponse> ClaimWithReceiptAsync(
            Guid hiveId,
            Guid sortieId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken =
                default(CancellationToken));
        Task<RemoteHivePerimeterMutationResponse> RecallWithReceiptAsync(
            Guid hiveId,
            Guid sortieId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken =
                default(CancellationToken));
    }

    public sealed class HivePerimeterSortieClient :
        IHiveSquadReservationClient,
        IHivePerimeterSortieClient
    {
        public const string ReservationContractVersion = "phase4-combat-squad-reservation-v1";
        public const string RecruitmentCatalogVersion = "phase4-combat-v1";
        public const string SortieContractVersion = "phase5-hive-perimeter-sortie-v1";
        public const int InitialCapacity = 12;

        private const int MaxCapacity = 1000;
        private const long MaxQuantity = 1000000;
        private static readonly string[] Families = { "guardians", "wingrunners", "darters" };
        private static readonly IReadOnlyDictionary<string, SignalContract> Signals =
            new Dictionary<string, SignalContract>(StringComparer.Ordinal)
            {
                ["foraging_scout"] = new SignalContract("wingrunners", TimeSpan.FromSeconds(16), 1, 40, 20),
                ["brood_watch"] = new SignalContract("guardians", TimeSpan.FromSeconds(20), 2, 25, 35)
            };

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;
        private readonly ProtectedGameReadCache readCache;

        public HivePerimeterSortieClient(
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
                "mobile_sortie_contract:" + SortieContractVersion,
                "mobile_reservation_contract:" + ReservationContractVersion,
                "mobile_reservation_receipt_validated:true",
                "mobile_sortie_mutation_receipt_validated:true",
                "server_time_authoritative:true",
                "server_rewards_authoritative:true",
                "claim_receipt_validated:true",
                "claim_receipt_device_lifetime:panel_session_only",
                "device_snapshot_memory_only:true",
                "access_token_persisted:false",
                "refresh_token_persisted:false",
                "game_auth_retry_after_401:once",
                "game_network_mutation_retry:false",
                "protected_read_cache_configured:" + (readCache != null).ToString().ToLowerInvariant(),
                "protected_read_cache_source:" + LastReadSource.ToString().ToLowerInvariant(),
                "offline_reward_mutation:false"
            };
        }

        public Task<RemoteSquadReservationSnapshot> ReadReservationAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            return SendReservationAsync(
                hiveId,
                new AuthenticatedGameRestRequest("GET", ReservationPath(hiveId)),
                cancellationToken);
        }

        public async Task<RemoteSquadReservationSnapshot>
            CommitReservationAsync(
            Guid hiveId,
            long expectedRevision,
            IReadOnlyDictionary<string, long> quantities,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RemoteSquadReservationResponse response =
                await CommitReservationWithReceiptAsync(
                    hiveId,
                    expectedRevision,
                    quantities,
                    idempotencyKey,
                    cancellationToken).ConfigureAwait(false);
            return response.Snapshot;
        }

        public Task<RemoteSquadReservationResponse>
            CommitReservationWithReceiptAsync(
            Guid hiveId,
            long expectedRevision,
            IReadOnlyDictionary<string, long> quantities,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            Dictionary<string, long> copy = CopyAndValidateQuantities(quantities);
            return SendReservationMutationAsync(
                hiveId,
                new AuthenticatedGameRestRequest(
                    "POST",
                    ReservationCommitPath(hiveId),
                    new SquadReservationMutationRequest
                    {
                        ExpectedRevision = expectedRevision,
                        Quantities = copy,
                        IdempotencyKey = idempotencyKey
                    }),
                "commit",
                expectedRevision,
                idempotencyKey,
                copy,
                cancellationToken);
        }

        public async Task<RemoteSquadReservationSnapshot>
            ReleaseReservationAsync(
            Guid hiveId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RemoteSquadReservationResponse response =
                await ReleaseReservationWithReceiptAsync(
                    hiveId,
                    expectedRevision,
                    idempotencyKey,
                    cancellationToken).ConfigureAwait(false);
            return response.Snapshot;
        }

        public Task<RemoteSquadReservationResponse>
            ReleaseReservationWithReceiptAsync(
            Guid hiveId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            return SendReservationMutationAsync(
                hiveId,
                new AuthenticatedGameRestRequest(
                    "POST",
                    ReservationReleasePath(hiveId),
                    new SquadReservationReleaseRequest
                    {
                        ExpectedRevision = expectedRevision,
                        IdempotencyKey = idempotencyKey
                    }),
                "release",
                expectedRevision,
                idempotencyKey,
                null,
                cancellationToken);
        }

        public Task<RemoteHivePerimeterSnapshot> ReadSortieBoardAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            return SendBoardAsync(
                hiveId,
                new AuthenticatedGameRestRequest("GET", SortieBoardPath(hiveId)),
                cancellationToken);
        }

        public Task<RemoteHivePerimeterSnapshot> LaunchAsync(
            Guid hiveId,
            string signalKey,
            string signalInstanceId,
            string reservationId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return UnwrapSnapshotAsync(
                LaunchWithReceiptAsync(
                    hiveId,
                    signalKey,
                    signalInstanceId,
                    reservationId,
                    expectedRevision,
                    idempotencyKey,
                    cancellationToken));
        }

        public Task<RemoteHivePerimeterMutationResponse>
            LaunchWithReceiptAsync(
            Guid hiveId,
            string signalKey,
            string signalInstanceId,
            string reservationId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            if (string.IsNullOrWhiteSpace(signalKey) || signalKey.Length > 64 || !Signals.ContainsKey(signalKey))
                throw InvalidRequest("Unknown perimeter signal.");
            RequireWireId(signalInstanceId, nameof(signalInstanceId));
            RequireWireId(reservationId, nameof(reservationId));
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            return SendSortieMutationAsync(
                hiveId,
                new AuthenticatedGameRestRequest(
                    "POST",
                    SortieLaunchPath(hiveId),
                    new HivePerimeterLaunchRequest
                    {
                        SignalKey = signalKey,
                        SignalInstanceId = signalInstanceId,
                        ReservationId = reservationId,
                        ExpectedRevision = expectedRevision,
                        IdempotencyKey = idempotencyKey
                    }),
                "launch",
                expectedRevision,
                idempotencyKey,
                Guid.Empty,
                signalKey,
                signalInstanceId,
                reservationId,
                cancellationToken);
        }

        public Task<RemoteHivePerimeterSnapshot> ClaimAsync(
            Guid hiveId,
            Guid sortieId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return UnwrapSnapshotAsync(
                ClaimWithReceiptAsync(
                    hiveId,
                    sortieId,
                    expectedRevision,
                    idempotencyKey,
                    cancellationToken));
        }

        public Task<RemoteHivePerimeterMutationResponse>
            ClaimWithReceiptAsync(
            Guid hiveId,
            Guid sortieId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return FinishWithReceiptAsync(
                hiveId,
                sortieId,
                expectedRevision,
                idempotencyKey,
                "claim",
                cancellationToken);
        }

        public Task<RemoteHivePerimeterSnapshot> RecallAsync(
            Guid hiveId,
            Guid sortieId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return UnwrapSnapshotAsync(
                RecallWithReceiptAsync(
                    hiveId,
                    sortieId,
                    expectedRevision,
                    idempotencyKey,
                    cancellationToken));
        }

        public Task<RemoteHivePerimeterMutationResponse>
            RecallWithReceiptAsync(
            Guid hiveId,
            Guid sortieId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return FinishWithReceiptAsync(
                hiveId,
                sortieId,
                expectedRevision,
                idempotencyKey,
                "recall",
                cancellationToken);
        }

        private Task<RemoteHivePerimeterMutationResponse>
            FinishWithReceiptAsync(
            Guid hiveId,
            Guid sortieId,
            long expectedRevision,
            string idempotencyKey,
            string action,
            CancellationToken cancellationToken)
        {
            RequireHive(hiveId);
            if (sortieId == Guid.Empty) throw InvalidRequest("A sortie identifier is required.");
            RequireRevision(expectedRevision);
            RequireKey(idempotencyKey, nameof(idempotencyKey));
            string path = string.Equals(action, "claim", StringComparison.Ordinal)
                ? SortieClaimPath(hiveId, sortieId)
                : SortieRecallPath(hiveId, sortieId);
            return SendSortieMutationAsync(
                hiveId,
                new AuthenticatedGameRestRequest(
                    "POST",
                    path,
                    new HivePerimeterMutationRequest
                    {
                        ExpectedRevision = expectedRevision,
                        IdempotencyKey = idempotencyKey
                    }),
                action,
                expectedRevision,
                idempotencyKey,
                sortieId,
                string.Empty,
                string.Empty,
                string.Empty,
                cancellationToken);
        }

        private static async Task<RemoteHivePerimeterSnapshot>
            UnwrapSnapshotAsync(
                Task<RemoteHivePerimeterMutationResponse> operation)
        {
            RemoteHivePerimeterMutationResponse response =
                await operation.ConfigureAwait(false);
            return response.Snapshot;
        }

        private async Task<RemoteSquadReservationSnapshot> SendReservationAsync(
            Guid hiveId,
            AuthenticatedGameRestRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
                RemoteSquadReservationSnapshot response = await SendWithSingleAuthenticationRefreshAsync<RemoteSquadReservationSnapshot>(
                    request,
                    context,
                    cancellationToken).ConfigureAwait(false);
                ValidateReservation(response, context.PlayerId, hiveId);
                SetServerReadSource();
                if (IsRead(request))
                    await SaveValidatedReadBestEffortAsync(
                        context.PlayerId,
                        hiveId,
                        ReservationContractVersion,
                        request.Path,
                        response,
                        cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch (Exception exception) when (IsRead(request) && IsOfflineEligible(exception))
            {
                RemoteSquadReservationSnapshot cached = await TryLoadReservationCacheAsync(hiveId, request.Path, cancellationToken)
                    .ConfigureAwait(false);
                if (cached != null) return cached;
                throw;
            }
        }

        private async Task<RemoteSquadReservationResponse>
            SendReservationMutationAsync(
                Guid hiveId,
                AuthenticatedGameRestRequest request,
                string action,
                long expectedRevision,
                string idempotencyKey,
                IReadOnlyDictionary<string, long> expectedQuantities,
                CancellationToken cancellationToken)
        {
            SessionContext context =
                await RequireSessionAsync(cancellationToken)
                    .ConfigureAwait(false);
            RemoteSquadReservationResponse response =
                await SendWithSingleAuthenticationRefreshAsync
                    <RemoteSquadReservationResponse>(
                        request,
                        context,
                        cancellationToken)
                    .ConfigureAwait(false);
            ValidateReservationResponse(
                response,
                context.PlayerId,
                hiveId,
                action,
                expectedRevision,
                idempotencyKey,
                expectedQuantities);
            SetServerReadSource();
            return response;
        }

        private async Task<RemoteHivePerimeterMutationResponse>
            SendSortieMutationAsync(
                Guid hiveId,
                AuthenticatedGameRestRequest request,
                string action,
                long expectedRevision,
                string idempotencyKey,
                Guid expectedSortieId,
                string expectedSignalKey,
                string expectedSignalInstanceId,
                string expectedReservationId,
                CancellationToken cancellationToken)
        {
            SessionContext context =
                await RequireSessionAsync(cancellationToken)
                    .ConfigureAwait(false);
            RemoteHivePerimeterMutationResponse response =
                await SendWithSingleAuthenticationRefreshAsync
                    <RemoteHivePerimeterMutationResponse>(
                        request,
                        context,
                        cancellationToken)
                    .ConfigureAwait(false);
            ValidateSortieMutationResponse(
                response,
                context.PlayerId,
                hiveId,
                action,
                expectedRevision,
                idempotencyKey,
                expectedSortieId,
                expectedSignalKey,
                expectedSignalInstanceId,
                expectedReservationId);
            SetServerReadSource();
            return response;
        }

        private async Task<RemoteHivePerimeterSnapshot> SendBoardAsync(
            Guid hiveId,
            AuthenticatedGameRestRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
                RemoteHivePerimeterSnapshot response = await SendWithSingleAuthenticationRefreshAsync<RemoteHivePerimeterSnapshot>(
                    request,
                    context,
                    cancellationToken).ConfigureAwait(false);
                ValidateBoard(response, context.PlayerId, hiveId);
                SetServerReadSource();
                if (IsRead(request))
                    await SaveValidatedReadBestEffortAsync(
                        context.PlayerId,
                        hiveId,
                        SortieContractVersion,
                        request.Path,
                        response,
                        cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch (Exception exception) when (IsRead(request) && IsOfflineEligible(exception))
            {
                RemoteHivePerimeterSnapshot cached = await TryLoadBoardCacheAsync(hiveId, request.Path, cancellationToken)
                    .ConfigureAwait(false);
                if (cached != null) return cached;
                throw;
            }
        }

        private async Task<SessionContext> RequireSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new HivePerimeterClientException(HivePerimeterClientError.NotConfigured, "Official account session transport is not ready.");
            IRefreshableGameAccountSessionSource refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable != null)
            {
                try
                {
                    GameAccountSession fresh = await refreshable.GetFreshSessionAsync(cancellationToken).ConfigureAwait(false);
                    if (fresh == null || fresh.PlayerId == Guid.Empty || string.IsNullOrWhiteSpace(fresh.AccessToken) ||
                        fresh.AccessToken.Length > 8192)
                        throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "An official account session is required.");
                    return new SessionContext(fresh.PlayerId, fresh.AccessToken);
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
            if (!sessionSource.TryGetSession(out session) || session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
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
                throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "The game session was rejected.");

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

        private async Task SaveValidatedReadBestEffortAsync<T>(
            Guid playerId,
            Guid hiveId,
            string contract,
            string path,
            T value,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return;
            try
            {
                await readCache.SaveValidatedReadAsync(playerId, hiveId, contract, path, value, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }
        }

        private async Task<RemoteSquadReservationSnapshot> TryLoadReservationCacheAsync(
            Guid hiveId,
            string path,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return null;
            Guid playerId;
            if (!TryGetKnownPlayerId(out playerId)) return null;
            ProtectedGameReadCacheHit<RemoteSquadReservationSnapshot> hit = await readCache.TryLoadAsync<RemoteSquadReservationSnapshot>(
                playerId,
                hiveId,
                ReservationContractVersion,
                path,
                cancellationToken).ConfigureAwait(false);
            if (hit == null) return null;
            ValidateReservation(hit.Value, playerId, hiveId);
            SetProtectedCacheReadSource(hit.CachedAtUtc);
            return hit.Value;
        }

        private async Task<RemoteHivePerimeterSnapshot> TryLoadBoardCacheAsync(
            Guid hiveId,
            string path,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return null;
            Guid playerId;
            if (!TryGetKnownPlayerId(out playerId)) return null;
            ProtectedGameReadCacheHit<RemoteHivePerimeterSnapshot> hit = await readCache.TryLoadAsync<RemoteHivePerimeterSnapshot>(
                playerId,
                hiveId,
                SortieContractVersion,
                path,
                cancellationToken).ConfigureAwait(false);
            if (hit == null) return null;
            ValidateBoard(hit.Value, playerId, hiveId);
            SetProtectedCacheReadSource(hit.CachedAtUtc);
            return hit.Value;
        }

        private bool TryGetKnownPlayerId(out Guid playerId)
        {
            IRefreshableGameAccountSessionSource refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable != null && refreshable.TryGetKnownPlayerId(out playerId) && playerId != Guid.Empty) return true;
            GameAccountSession session;
            if (sessionSource.TryGetSession(out session) && session != null && session.PlayerId != Guid.Empty)
            {
                playerId = session.PlayerId;
                return true;
            }
            playerId = Guid.Empty;
            return false;
        }

        private void SetServerReadSource()
        {
            LastReadSource = GameReadSource.Server;
            LastReadCachedAtUtc = default(DateTimeOffset);
        }

        private void SetProtectedCacheReadSource(DateTimeOffset cachedAtUtc)
        {
            LastReadSource = GameReadSource.ProtectedCache;
            LastReadCachedAtUtc = cachedAtUtc;
        }

        private static bool IsRead(AuthenticatedGameRestRequest request)
        {
            return request != null && string.Equals(request.Method, "GET", StringComparison.Ordinal);
        }

        private static bool IsOfflineEligible(Exception exception)
        {
            HivePerimeterClientException client = exception as HivePerimeterClientException;
            if (client != null)
                return client.Error == HivePerimeterClientError.TransportFailure || client.Error == HivePerimeterClientError.NotConfigured;
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
            return new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, exception.SafeCode);
        }

        private static void ValidateBoard(RemoteHivePerimeterSnapshot board, Guid playerId, Guid hiveId)
        {
            if (board == null) throw InvalidResponse("The perimeter response is empty.");
            if (board.PlayerId != playerId || board.HiveId != hiveId)
                throw InvalidResponse("The perimeter response belongs to another session or hive.");
            if (!string.Equals(board.ContractVersion, SortieContractVersion, StringComparison.Ordinal))
                throw InvalidResponse("The perimeter contract version is unsupported.");
            if (board.Revision < 0) throw InvalidResponse("The perimeter revision is invalid.");
            if (!IsUtc(board.ServerTimeUtc) || board.ServerTimeUtc < board.CycleStartedAtUtc)
                throw InvalidResponse("The perimeter server time is invalid.");
            ValidateCycle(board.CycleStartedAtUtc, board.CycleEndsAtUtc);
            if (board.Active == null && board.ServerTimeUtc >= board.CycleEndsAtUtc)
                throw InvalidResponse("An inactive perimeter snapshot cannot remain in an expired cycle.");
            ValidateReservation(board.Reservation, playerId, hiveId);
            if (board.Signals == null || board.Signals.Count != Signals.Count)
                throw InvalidResponse("The perimeter signal catalog is incomplete.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (RemoteHivePerimeterSignal signal in board.Signals)
            {
                if (signal == null || string.IsNullOrEmpty(signal.SignalKey) || !seen.Add(signal.SignalKey))
                    throw InvalidResponse("The perimeter signal catalog contains an invalid key.");
                SignalContract contract;
                if (!Signals.TryGetValue(signal.SignalKey, out contract))
                    throw InvalidResponse("The perimeter signal catalog contains an unknown signal.");
                if (!string.Equals(signal.HazardDoctrine, contract.HazardDoctrine, StringComparison.Ordinal) ||
                    signal.Duration != contract.Duration || signal.MinimumSquad != contract.MinimumSquad ||
                    signal.HoneyReward != contract.HoneyReward || signal.PollenReward != contract.PollenReward)
                    throw InvalidResponse("A perimeter signal definition differs from the supported contract.");
                string expectedInstance = CreateSignalInstanceId(playerId, hiveId, board.CycleStartedAtUtc, signal.SignalKey);
                if (!string.Equals(signal.SignalInstanceId, expectedInstance, StringComparison.Ordinal))
                    throw InvalidResponse("A perimeter signal instance is not bound to this player, hive and cycle.");
                bool expectedCanLaunch = board.Active == null && !signal.Completed;
                if (signal.CanLaunch != expectedCanLaunch)
                    throw InvalidResponse("A perimeter signal launch state is inconsistent.");
            }

            if (board.Active != null) ValidateActive(board.Active, board, seen);
            if (board.ClaimReceipt != null) ValidateClaimReceipt(board.ClaimReceipt, board);
        }

        private static void ValidateClaimReceipt(
            RemoteHivePerimeterClaimReceipt receipt,
            RemoteHivePerimeterSnapshot board)
        {
            if (receipt.PlayerId != board.PlayerId || receipt.HiveId != board.HiveId || receipt.SortieId == Guid.Empty)
                throw InvalidResponse("The perimeter claim receipt belongs to another session, hive or sortie.");
            if (!IsUtc(receipt.CycleStartedAtUtc) || !IsUtc(receipt.CycleEndsAtUtc) ||
                receipt.CycleStartedAtUtc != board.CycleStartedAtUtc || receipt.CycleEndsAtUtc != board.CycleEndsAtUtc)
                throw InvalidResponse("The perimeter claim receipt is detached from this cycle.");
            if (!IsUtc(receipt.ServerTimeUtc) || receipt.ServerTimeUtc < receipt.CycleStartedAtUtc ||
                receipt.ServerTimeUtc > board.ServerTimeUtc || receipt.Revision <= 0 || receipt.Revision > board.Revision)
                throw InvalidResponse("The perimeter claim receipt timing or revision is invalid.");

            RemoteHivePerimeterSignal signal = board.Signals.SingleOrDefault(item =>
                string.Equals(item.SignalKey, receipt.SignalKey, StringComparison.Ordinal));
            if (board.Active != null || board.Reservation.Reserved.Values.Any(value => value != 0) ||
                !string.IsNullOrEmpty(board.Reservation.ReservationId) || signal == null || !signal.Completed ||
                !string.Equals(signal.SignalInstanceId, receipt.SignalInstanceId, StringComparison.Ordinal))
                throw InvalidResponse("The perimeter claim receipt is detached from its completed signal.");
            if (!HasExactResourceKeys(receipt.CreditedByResource) || !HasExactResourceKeys(receipt.ResultingBalances))
                throw InvalidResponse("The perimeter claim receipt resource set is invalid.");

            ValidateClaimResource(receipt, "honey", signal.HoneyReward);
            ValidateClaimResource(receipt, "pollen", signal.PollenReward);
        }

        private static void ValidateClaimResource(
            RemoteHivePerimeterClaimReceipt receipt,
            string resourceKey,
            long advertisedReward)
        {
            long credited = receipt.CreditedByResource[resourceKey];
            RemoteHiveResourceBalance balance = receipt.ResultingBalances[resourceKey];
            if (credited < 0 || credited > advertisedReward || balance == null ||
                balance.Amount < 0 || balance.Capacity < balance.Amount || balance.Amount < credited)
                throw InvalidResponse("A perimeter claim receipt resource balance is invalid.");
            if (credited < advertisedReward && balance.Amount != balance.Capacity)
                throw InvalidResponse("A reduced perimeter credit is not explained by storage capacity.");
        }

        private static bool HasExactResourceKeys<T>(Dictionary<string, T> map)
        {
            return map != null && map.Count == 2 && map.ContainsKey("honey") && map.ContainsKey("pollen") &&
                map.Keys.All(key => string.Equals(key, "honey", StringComparison.Ordinal) ||
                    string.Equals(key, "pollen", StringComparison.Ordinal));
        }

        private static void ValidateActive(
            RemoteHivePerimeterActiveSortie active,
            RemoteHivePerimeterSnapshot board,
            HashSet<string> signalKeys)
        {
            if (active.SortieId == Guid.Empty || !signalKeys.Contains(active.SignalKey) ||
                !IsLowerHex(active.SignalInstanceId, 32) || !IsLowerHex(active.ReservationId, 32))
                throw InvalidResponse("The active perimeter sortie contains invalid identifiers.");
            RemoteHivePerimeterSignal signal = board.Signals.Single(item => item.SignalKey == active.SignalKey);
            if (!string.Equals(active.SignalInstanceId, signal.SignalInstanceId, StringComparison.Ordinal) ||
                board.Reservation == null ||
                !string.Equals(active.ReservationId, board.Reservation.ReservationId, StringComparison.Ordinal))
                throw InvalidResponse("The active perimeter sortie is detached from its signal or reservation.");
            if (!IsUtc(active.StartedAtUtc) || !IsUtc(active.EndsAtUtc) ||
                active.EndsAtUtc <= active.StartedAtUtc || active.EndsAtUtc - active.StartedAtUtc != signal.Duration ||
                active.StartedAtUtc < board.CycleStartedAtUtc || board.ServerTimeUtc < active.StartedAtUtc ||
                active.Revision != board.Revision || active.Revision <= 0)
                throw InvalidResponse("The active perimeter sortie timing or revision is invalid.");
        }

        private static void ValidateCycle(DateTimeOffset start, DateTimeOffset end)
        {
            if (!IsUtc(start) || !IsUtc(end) || end - start != TimeSpan.FromHours(8) ||
                start.Minute != 0 || start.Second != 0 || start.Millisecond != 0 || start.Ticks % TimeSpan.TicksPerSecond != 0 ||
                (start.Hour != 0 && start.Hour != 8 && start.Hour != 16))
                throw InvalidResponse("The perimeter cycle is not a supported eight-hour UTC cycle.");
        }

        private static void ValidateReservation(RemoteSquadReservationSnapshot reservation, Guid playerId, Guid hiveId)
        {
            if (reservation == null) throw InvalidResponse("The squad reservation response is empty.");
            if (reservation.PlayerId != playerId || reservation.HiveId != hiveId)
                throw InvalidResponse("The squad reservation belongs to another session or hive.");
            if (!string.Equals(reservation.ContractVersion, ReservationContractVersion, StringComparison.Ordinal) ||
                !string.Equals(reservation.CatalogVersion, RecruitmentCatalogVersion, StringComparison.Ordinal))
                throw InvalidResponse("The squad reservation contract is unsupported.");
            if (reservation.RosterRevision < 0 || reservation.ReservationRevision < 0 ||
                reservation.Capacity < 1 || reservation.Capacity > MaxCapacity)
                throw InvalidResponse("The squad reservation revision or capacity is invalid.");
            ValidateFamilyMap(reservation.Roster, "roster");
            ValidateFamilyMap(reservation.Available, "available");
            ValidateFamilyMap(reservation.Reserved, "reserved");

            long total = 0;
            try
            {
                foreach (string family in Families)
                {
                    long roster = reservation.Roster[family];
                    long reserved = reservation.Reserved[family];
                    if (reserved > roster || reservation.Available[family] != roster - reserved)
                        throw InvalidResponse("The squad reservation arithmetic is inconsistent.");
                    total = checked(total + reserved);
                }
            }
            catch (OverflowException)
            {
                throw InvalidResponse("The squad reservation total overflowed.");
            }

            if (total > reservation.Capacity)
                throw InvalidResponse("The squad reservation exceeds capacity.");
            if (total == 0)
            {
                if (!string.IsNullOrEmpty(reservation.ReservationId))
                    throw InvalidResponse("An empty squad cannot carry a reservation identifier.");
            }
            else if (!IsLowerHex(reservation.ReservationId, 32))
            {
                throw InvalidResponse("The squad reservation identifier is invalid.");
            }
        }

        private static void ValidateReservationResponse(
            RemoteSquadReservationResponse response,
            Guid playerId,
            Guid hiveId,
            string expectedAction,
            long expectedRevision,
            string expectedIdempotencyKey,
            IReadOnlyDictionary<string, long> expectedQuantities)
        {
            if (response == null || response.Receipt == null ||
                response.Snapshot == null)
                throw InvalidResponse(
                    "The squad reservation mutation response is incomplete.");
            ValidateReservation(response.Snapshot, playerId, hiveId);

            RemoteSquadReservationReceipt receipt = response.Receipt;
            if (receipt.PlayerId != playerId || receipt.HiveId != hiveId)
                throw InvalidResponse(
                    "The squad reservation receipt belongs to another partition.");
            if (!string.Equals(
                    receipt.IdempotencyKey,
                    expectedIdempotencyKey,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    receipt.Action,
                    expectedAction,
                    StringComparison.Ordinal))
                throw InvalidResponse(
                    "The squad reservation receipt does not match the command.");
            if (receipt.ReservationRevisionBefore != expectedRevision ||
                receipt.ReservationRevisionAfter !=
                    expectedRevision + 1 ||
                response.Snapshot.ReservationRevision <
                    receipt.ReservationRevisionAfter)
                throw InvalidResponse(
                    "The squad reservation receipt revision is inconsistent.");
            if (!IsUtc(receipt.AcceptedAtUtc) ||
                receipt.AcceptedAtUtc == default(DateTimeOffset))
                throw InvalidResponse(
                    "The squad reservation receipt time is invalid.");
            ValidateFamilyMap(receipt.Quantities, "receipt");
            if (string.Equals(expectedAction, "commit", StringComparison.Ordinal))
            {
                if (!IsLowerHex(receipt.ReservationId, 32) ||
                    !string.Equals(
                        receipt.Code,
                        "game.squad_reserved",
                        StringComparison.Ordinal) ||
                    expectedQuantities == null ||
                    Families.Any(family =>
                        receipt.Quantities[family] !=
                        expectedQuantities[family]))
                    throw InvalidResponse(
                        "The squad reservation commit receipt is inconsistent.");
                if (response.Snapshot.ReservationRevision ==
                        receipt.ReservationRevisionAfter &&
                    (!string.Equals(
                         response.Snapshot.ReservationId,
                         receipt.ReservationId,
                         StringComparison.Ordinal) ||
                     Families.Any(family =>
                         response.Snapshot.Reserved[family] !=
                         receipt.Quantities[family])))
                    throw InvalidResponse(
                        "The fresh squad reservation snapshot differs from its receipt.");
            }
            else if (string.Equals(
                         expectedAction,
                         "release",
                         StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(receipt.ReservationId) ||
                    receipt.Quantities.Values.Any(value => value != 0) ||
                    !string.Equals(
                        receipt.Code,
                        "game.squad_released",
                        StringComparison.Ordinal))
                    throw InvalidResponse(
                        "The squad reservation release receipt is inconsistent.");
                if (response.Snapshot.ReservationRevision ==
                        receipt.ReservationRevisionAfter &&
                    (!string.IsNullOrEmpty(
                         response.Snapshot.ReservationId) ||
                     response.Snapshot.Reserved.Values.Any(
                         value => value != 0)))
                    throw InvalidResponse(
                        "The fresh release snapshot still reserves a squad.");
            }
            else
            {
                throw InvalidResponse(
                    "The squad reservation receipt action is unsupported.");
            }
        }

        private static void ValidateSortieMutationResponse(
            RemoteHivePerimeterMutationResponse response,
            Guid playerId,
            Guid hiveId,
            string expectedAction,
            long expectedRevision,
            string expectedIdempotencyKey,
            Guid expectedSortieId,
            string expectedSignalKey,
            string expectedSignalInstanceId,
            string expectedReservationId)
        {
            if (response == null || response.Receipt == null ||
                response.Snapshot == null)
                throw InvalidResponse(
                    "The perimeter mutation response is incomplete.");
            ValidateBoard(response.Snapshot, playerId, hiveId);

            RemoteHivePerimeterMutationReceipt receipt =
                response.Receipt;
            if (receipt.PlayerId != playerId ||
                receipt.HiveId != hiveId)
                throw InvalidResponse(
                    "The perimeter mutation receipt belongs to another partition.");
            if (!string.Equals(
                    receipt.IdempotencyKey,
                    expectedIdempotencyKey,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    receipt.Action,
                    expectedAction,
                    StringComparison.Ordinal))
                throw InvalidResponse(
                    "The perimeter mutation receipt does not match the command.");
            if (receipt.RevisionBefore != expectedRevision ||
                receipt.RevisionAfter != expectedRevision + 1)
                throw InvalidResponse(
                    "The perimeter mutation receipt revision is inconsistent.");
            if (!IsUtc(receipt.AcceptedAtUtc) ||
                receipt.AcceptedAtUtc == default(DateTimeOffset))
                throw InvalidResponse(
                    "The perimeter mutation receipt time is invalid.");
            ValidateCycle(
                receipt.CycleStartedAtUtc,
                receipt.CycleEndsAtUtc);
            if (receipt.AcceptedAtUtc < receipt.CycleStartedAtUtc)
                throw InvalidResponse(
                    "The perimeter mutation receipt predates its cycle.");
            if (receipt.SortieId == Guid.Empty ||
                (expectedSortieId != Guid.Empty &&
                 receipt.SortieId != expectedSortieId) ||
                !Signals.ContainsKey(receipt.SignalKey) ||
                !string.Equals(
                    receipt.SignalInstanceId,
                    CreateSignalInstanceId(
                        playerId,
                        hiveId,
                        receipt.CycleStartedAtUtc,
                        receipt.SignalKey),
                    StringComparison.Ordinal) ||
                !IsLowerHex(receipt.ReservationId, 32))
                throw InvalidResponse(
                    "The perimeter mutation receipt identifiers are invalid.");

            string expectedCode;
            if (string.Equals(
                    expectedAction,
                    "launch",
                    StringComparison.Ordinal))
            {
                expectedCode = "game.perimeter_launched";
                if (!string.Equals(
                        receipt.SignalKey,
                        expectedSignalKey,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        receipt.SignalInstanceId,
                        expectedSignalInstanceId,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        receipt.ReservationId,
                        expectedReservationId,
                        StringComparison.Ordinal))
                    throw InvalidResponse(
                        "The perimeter launch receipt differs from the command.");
                ValidateNoRewardReceipt(receipt);
            }
            else if (string.Equals(
                         expectedAction,
                         "claim",
                         StringComparison.Ordinal))
            {
                expectedCode = "game.perimeter_claimed";
                ValidateMutationClaimResources(receipt);
            }
            else if (string.Equals(
                         expectedAction,
                         "recall",
                         StringComparison.Ordinal))
            {
                expectedCode = "game.perimeter_recalled";
                ValidateNoRewardReceipt(receipt);
            }
            else
            {
                throw InvalidResponse(
                    "The perimeter mutation receipt action is unsupported.");
            }
            if (!string.Equals(
                    receipt.Code,
                    expectedCode,
                    StringComparison.Ordinal))
                throw InvalidResponse(
                    "The perimeter mutation receipt code is invalid.");

            RemoteHivePerimeterSnapshot snapshot =
                response.Snapshot;
            bool sameCycle =
                snapshot.CycleStartedAtUtc ==
                receipt.CycleStartedAtUtc;
            if (sameCycle &&
                snapshot.Revision < receipt.RevisionAfter)
                throw InvalidResponse(
                    "The perimeter snapshot predates its mutation receipt.");
            if (!sameCycle &&
                snapshot.CycleStartedAtUtc <
                receipt.CycleStartedAtUtc)
                throw InvalidResponse(
                    "The perimeter snapshot cycle predates its receipt.");
            if (sameCycle &&
                snapshot.Revision == receipt.RevisionAfter)
                ValidateFreshMutationSnapshot(
                    snapshot,
                    receipt,
                    expectedAction);
        }

        private static void ValidateFreshMutationSnapshot(
            RemoteHivePerimeterSnapshot snapshot,
            RemoteHivePerimeterMutationReceipt receipt,
            string action)
        {
            if (string.Equals(action, "launch", StringComparison.Ordinal))
            {
                if (snapshot.Active == null ||
                    snapshot.Active.SortieId != receipt.SortieId ||
                    !string.Equals(
                        snapshot.Active.SignalKey,
                        receipt.SignalKey,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        snapshot.Active.SignalInstanceId,
                        receipt.SignalInstanceId,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        snapshot.Active.ReservationId,
                        receipt.ReservationId,
                        StringComparison.Ordinal))
                    throw InvalidResponse(
                        "The fresh perimeter snapshot differs from its launch receipt.");
                return;
            }

            RemoteHivePerimeterSignal signal =
                snapshot.Signals.Single(item =>
                    string.Equals(
                        item.SignalKey,
                        receipt.SignalKey,
                        StringComparison.Ordinal));
            bool shouldBeCompleted =
                string.Equals(action, "claim", StringComparison.Ordinal);
            if (snapshot.Active != null ||
                !string.IsNullOrEmpty(
                    snapshot.Reservation.ReservationId) ||
                snapshot.Reservation.Reserved.Values.Any(
                    value => value != 0) ||
                signal.Completed != shouldBeCompleted)
                throw InvalidResponse(
                    "The fresh perimeter completion snapshot differs from its receipt.");
        }

        private static void ValidateNoRewardReceipt(
            RemoteHivePerimeterMutationReceipt receipt)
        {
            if (receipt.CreditedByResource == null ||
                receipt.ResultingBalances == null ||
                receipt.CreditedByResource.Count != 0 ||
                receipt.ResultingBalances.Count != 0)
                throw InvalidResponse(
                    "A non-claim perimeter receipt cannot contain rewards.");
        }

        private static void ValidateMutationClaimResources(
            RemoteHivePerimeterMutationReceipt receipt)
        {
            if (!HasExactResourceKeys(
                    receipt.CreditedByResource) ||
                !HasExactResourceKeys(
                    receipt.ResultingBalances))
                throw InvalidResponse(
                    "The perimeter claim mutation receipt resource set is invalid.");
            SignalContract signal = Signals[receipt.SignalKey];
            ValidateMutationClaimResource(
                receipt,
                "honey",
                signal.HoneyReward);
            ValidateMutationClaimResource(
                receipt,
                "pollen",
                signal.PollenReward);
        }

        private static void ValidateMutationClaimResource(
            RemoteHivePerimeterMutationReceipt receipt,
            string resourceKey,
            long advertisedReward)
        {
            long credited =
                receipt.CreditedByResource[resourceKey];
            RemoteHiveResourceBalance balance =
                receipt.ResultingBalances[resourceKey];
            if (credited < 0 ||
                credited > advertisedReward ||
                balance == null ||
                balance.Amount < 0 ||
                balance.Capacity < balance.Amount ||
                balance.Amount < credited)
                throw InvalidResponse(
                    "A perimeter mutation receipt resource balance is invalid.");
            if (credited < advertisedReward &&
                balance.Amount != balance.Capacity)
                throw InvalidResponse(
                    "A reduced perimeter mutation credit is not capacity-bound.");
        }

        private static void ValidateFamilyMap(Dictionary<string, long> map, string name)
        {
            if (map == null || map.Count != Families.Length || Families.Any(family => !map.ContainsKey(family)) ||
                map.Any(item => !Families.Contains(item.Key) || item.Value < 0 || item.Value > MaxQuantity))
                throw InvalidResponse("The squad " + name + " map is invalid.");
        }

        private static Dictionary<string, long> CopyAndValidateQuantities(
            IReadOnlyDictionary<string, long> quantities)
        {
            if (quantities == null || quantities.Count != Families.Length ||
                Families.Any(family => !quantities.ContainsKey(family)) ||
                quantities.Any(item => !Families.Contains(item.Key) || item.Value < 0 || item.Value > MaxQuantity))
                throw InvalidRequest("Exactly the three canonical squad families are required.");
            var copy = new Dictionary<string, long>(StringComparer.Ordinal);
            long total = 0;
            try
            {
                foreach (string family in Families)
                {
                    copy[family] = quantities[family];
                    total = checked(total + quantities[family]);
                }
            }
            catch (OverflowException)
            {
                throw InvalidRequest("The squad quantity total overflowed.");
            }
            // Capacity is server-authoritative (RemoteSquadReservationSnapshot.Capacity), which can
            // grow beyond the client's local InitialCapacity fallback constant - only reject a
            // structurally empty squad here, let the server enforce the real ceiling.
            if (total <= 0) throw InvalidRequest("The squad must contain at least one bee.");
            return copy;
        }

        private static string CreateSignalInstanceId(Guid playerId, Guid hiveId, DateTimeOffset cycleStart, string signalKey)
        {
            string payload = "instance|" + playerId.ToString("N") + "|" + hiveId.ToString("N") + "|" +
                cycleStart.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) + "|" + signalKey;
            byte[] hash;
            using (SHA256 sha = SHA256.Create()) hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var builder = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString(0, 32);
        }

        public static string ReservationPath(Guid hiveId)
        {
            RequireHive(hiveId);
            return "/game/v1/hives/" + hiveId.ToString("D") + "/combat/squad-reservation";
        }

        public static string ReservationCommitPath(Guid hiveId)
        {
            return ReservationPath(hiveId) + "/commit";
        }

        public static string ReservationReleasePath(Guid hiveId)
        {
            return ReservationPath(hiveId) + "/release";
        }

        public static string SortieBoardPath(Guid hiveId)
        {
            RequireHive(hiveId);
            return "/game/v1/hives/" + hiveId.ToString("D") + "/perimeter-sortie";
        }

        public static string SortieLaunchPath(Guid hiveId)
        {
            return SortieBoardPath(hiveId) + "/launch";
        }

        public static string SortieClaimPath(
            Guid hiveId,
            Guid sortieId)
        {
            RequireHive(hiveId);
            if (sortieId == Guid.Empty)
                throw InvalidRequest(
                    "A sortie identifier is required.");
            return SortieBoardPath(hiveId) + "/" +
                sortieId.ToString("D") + "/claim";
        }

        public static string SortieRecallPath(
            Guid hiveId,
            Guid sortieId)
        {
            RequireHive(hiveId);
            if (sortieId == Guid.Empty)
                throw InvalidRequest(
                    "A sortie identifier is required.");
            return SortieBoardPath(hiveId) + "/" +
                sortieId.ToString("D") + "/recall";
        }

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required.");
        }

        private static void RequireRevision(long revision)
        {
            if (revision < 0 || revision == long.MaxValue)
                throw InvalidRequest(
                    "The expected revision is outside the supported range.");
        }

        private static void RequireKey(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 256)
                throw InvalidRequest(name + " must contain between one and 256 characters.");
        }

        private static void RequireWireId(string value, string name)
        {
            if (!IsLowerHex(value, 32))
                throw InvalidRequest(name + " must be a canonical 32-character identifier.");
        }

        private static bool IsUtc(DateTimeOffset value)
        {
            return value.Offset == TimeSpan.Zero;
        }

        private static bool IsLowerHex(string value, int length)
        {
            if (string.IsNullOrEmpty(value) || value.Length != length) return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'))) return false;
            }
            return true;
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

        private sealed class SignalContract
        {
            public SignalContract(string hazardDoctrine, TimeSpan duration, int minimumSquad, long honeyReward, long pollenReward)
            {
                HazardDoctrine = hazardDoctrine;
                Duration = duration;
                MinimumSquad = minimumSquad;
                HoneyReward = honeyReward;
                PollenReward = pollenReward;
            }

            public string HazardDoctrine { get; }
            public TimeSpan Duration { get; }
            public int MinimumSquad { get; }
            public long HoneyReward { get; }
            public long PollenReward { get; }
        }
    }
}
