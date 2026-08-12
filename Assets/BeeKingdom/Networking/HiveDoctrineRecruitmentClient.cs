using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteDoctrineRecruitmentOffer
    {
        public string Family { get; set; }
        public int BatchSize { get; set; }
        public long HoneyCost { get; set; }
        public long PollenCost { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public sealed class RemoteDoctrineRecruitmentBalance
    {
        public long Amount { get; set; }
        public long Capacity { get; set; }
    }

    public sealed class RemoteDoctrineRecruitmentOperation
    {
        public Guid OperationId { get; set; }
        public string Family { get; set; }
        public int BatchSize { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndsAtUtc { get; set; }
        public string Status { get; set; }
    }

    public sealed class RemoteDoctrineRecruitmentSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public string CatalogVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public List<RemoteDoctrineRecruitmentOffer> Offers { get; set; }
        public Dictionary<string, RemoteDoctrineRecruitmentBalance> Balances { get; set; }
        public Dictionary<string, long> Counts { get; set; }
        public List<string> LegacyRoles { get; set; }
        public RemoteDoctrineRecruitmentOperation ActiveOperation { get; set; }
    }

    public sealed class DoctrineRecruitmentStartRequest
    {
        public string Family { get; set; }
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class DoctrineRecruitmentClaimRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteDoctrineRecruitmentReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string IdempotencyKey { get; set; }
        public Guid OperationId { get; set; }
        public string Family { get; set; }
        public int BatchSize { get; set; }
        public long RevisionBefore { get; set; }
        public long RevisionAfter { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public string Code { get; set; }
    }

    public sealed class RemoteDoctrineRecruitmentResponse
    {
        public RemoteDoctrineRecruitmentReceipt Receipt { get; set; }
        public RemoteDoctrineRecruitmentSnapshot Snapshot { get; set; }
    }

    public interface IHiveDoctrineRecruitmentClient
    {
        GameReadSource LastReadSource { get; }
        DateTimeOffset LastReadCachedAtUtc { get; }
        Task<RemoteDoctrineRecruitmentSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteDoctrineRecruitmentResponse> StartAsync(
            Guid hiveId,
            string family,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteDoctrineRecruitmentResponse> ClaimAsync(
            Guid hiveId,
            Guid operationId,
            string expectedFamily,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public sealed class HiveDoctrineRecruitmentClient :
        IHiveDoctrineRecruitmentClient
    {
        public const string ContractVersion =
            "phase4-combat-recruitment-v1";
        public const string CatalogVersion = "phase4-combat-v1";
        public const string RunningStatus = "running";
        public const string AwaitingCompletionStatus = "awaiting_completion";
        public const string StartedCode = "game.recruitment_started";
        public const string ClaimedCode = "game.recruitment_claimed";
        public static readonly TimeSpan MaximumDuration =
            TimeSpan.FromDays(1);

        private static readonly IReadOnlyDictionary
            <string, CanonicalOffer> CanonicalOffers =
            new Dictionary<string, CanonicalOffer>(StringComparer.Ordinal)
            {
                ["guardians"] = new CanonicalOffer(
                    4,
                    680,
                    180,
                    TimeSpan.FromSeconds(14)),
                ["wingrunners"] = new CanonicalOffer(
                    6,
                    420,
                    260,
                    TimeSpan.FromSeconds(14)),
                ["darters"] = new CanonicalOffer(
                    8,
                    500,
                    120,
                    TimeSpan.FromSeconds(14))
            };

        private static readonly HashSet<string> CanonicalLegacyRoles =
            new HashSet<string>(
                new[] { "Soldats", "Gardiennes", "Eclaireuses" },
                StringComparer.Ordinal);

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;
        private readonly ProtectedGameReadCache readCache;

        public HiveDoctrineRecruitmentClient(
            MobileAccountSessionGate sessionGate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport,
            ProtectedGameReadCache readCache = null)
        {
            this.sessionGate =
                sessionGate ?? throw new ArgumentNullException(
                    nameof(sessionGate));
            this.sessionSource =
                sessionSource ?? throw new ArgumentNullException(
                    nameof(sessionSource));
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
                "mobile_doctrine_recruitment_contract:" + ContractVersion,
                "doctrine_recruitment_catalog_authority:server",
                "doctrine_recruitment_balance_authority:server",
                "doctrine_recruitment_roster_authority:server",
                "doctrine_recruitment_time_authority:server",
                "doctrine_recruitment_get_protected_cache:" +
                    (readCache != null &&
                     readCache.IsProtectionAvailable).ToString()
                        .ToLowerInvariant(),
                "doctrine_recruitment_cache_read_only:true",
                "doctrine_recruitment_mutation_outbox_required:true",
                "doctrine_recruitment_mutation_offline_retry:false",
                "doctrine_recruitment_local_debit:false",
                "doctrine_recruitment_local_claim:false",
                "doctrine_recruitment_read_source:" +
                    LastReadSource.ToString().ToLowerInvariant()
            };
        }

        public async Task<RemoteDoctrineRecruitmentSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken =
                default(CancellationToken))
        {
            RequireHive(hiveId);
            string path = Path(hiveId);
            try
            {
                SessionContext context =
                    await RequireSessionAsync(cancellationToken)
                        .ConfigureAwait(false);
                var request = new AuthenticatedGameRestRequest("GET", path);
                RemoteDoctrineRecruitmentSnapshot snapshot =
                    await SendWithSingleAuthenticationRefreshAsync
                        <RemoteDoctrineRecruitmentSnapshot>(
                            request,
                            context,
                            cancellationToken)
                        .ConfigureAwait(false);
                ValidateSnapshot(snapshot, context.PlayerId, hiveId);
                LastReadSource = GameReadSource.Server;
                LastReadCachedAtUtc = default(DateTimeOffset);
                await SaveValidatedReadBestEffortAsync(
                        context.PlayerId,
                        hiveId,
                        path,
                        snapshot,
                        cancellationToken)
                    .ConfigureAwait(false);
                return snapshot;
            }
            catch (Exception exception) when (IsOfflineEligible(exception))
            {
                RemoteDoctrineRecruitmentSnapshot cached =
                    await TryLoadCacheAsync(
                            hiveId,
                            path,
                            cancellationToken)
                        .ConfigureAwait(false);
                if (cached != null) return cached;
                throw;
            }
        }

        public async Task<RemoteDoctrineRecruitmentResponse> StartAsync(
            Guid hiveId,
            string family,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken =
                default(CancellationToken))
        {
            RequireHive(hiveId);
            RequireFamily(family);
            RequireRevision(expectedRevision);
            RequireIdempotencyKey(idempotencyKey);
            SessionContext context =
                await RequireSessionAsync(cancellationToken)
                    .ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                StartPath(hiveId),
                new DoctrineRecruitmentStartRequest
                {
                    Family = family,
                    ExpectedRevision = expectedRevision,
                    IdempotencyKey = idempotencyKey
                });
            RemoteDoctrineRecruitmentResponse response =
                await SendWithSingleAuthenticationRefreshAsync
                    <RemoteDoctrineRecruitmentResponse>(
                        request,
                        context,
                        cancellationToken)
                    .ConfigureAwait(false);
            ValidateMutationResponse(
                response,
                context.PlayerId,
                hiveId,
                Guid.Empty,
                family,
                expectedRevision,
                idempotencyKey,
                StartedCode,
                true);
            await SaveMutationSnapshotBestEffortAsync(
                    context.PlayerId,
                    hiveId,
                    response.Snapshot)
                .ConfigureAwait(false);
            return response;
        }

        public async Task<RemoteDoctrineRecruitmentResponse> ClaimAsync(
            Guid hiveId,
            Guid operationId,
            string expectedFamily,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken =
                default(CancellationToken))
        {
            RequireHive(hiveId);
            if (operationId == Guid.Empty)
                throw InvalidRequest(
                    "A recruitment operation identifier is required.");
            RequireFamily(expectedFamily);
            RequireRevision(expectedRevision);
            RequireIdempotencyKey(idempotencyKey);
            SessionContext context =
                await RequireSessionAsync(cancellationToken)
                    .ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                ClaimPath(hiveId, operationId),
                new DoctrineRecruitmentClaimRequest
                {
                    ExpectedRevision = expectedRevision,
                    IdempotencyKey = idempotencyKey
                });
            RemoteDoctrineRecruitmentResponse response =
                await SendWithSingleAuthenticationRefreshAsync
                    <RemoteDoctrineRecruitmentResponse>(
                        request,
                        context,
                        cancellationToken)
                    .ConfigureAwait(false);
            ValidateMutationResponse(
                response,
                context.PlayerId,
                hiveId,
                operationId,
                expectedFamily,
                expectedRevision,
                idempotencyKey,
                ClaimedCode,
                false);
            await SaveMutationSnapshotBestEffortAsync(
                    context.PlayerId,
                    hiveId,
                    response.Snapshot)
                .ConfigureAwait(false);
            return response;
        }

        public static string Path(Guid hiveId)
        {
            RequireHive(hiveId);
            return "/game/v1/hives/" + hiveId.ToString("D") +
                "/combat/recruitment";
        }

        public static string StartPath(Guid hiveId)
        {
            return Path(hiveId) + "/start";
        }

        public static string ClaimPath(Guid hiveId, Guid operationId)
        {
            RequireHive(hiveId);
            if (operationId == Guid.Empty)
                throw InvalidRequest(
                    "A recruitment operation identifier is required.");
            return Path(hiveId) + "/" + operationId.ToString("D") +
                "/claim";
        }

        internal static void ValidateSnapshot(
            RemoteDoctrineRecruitmentSnapshot snapshot,
            Guid playerId,
            Guid hiveId)
        {
            if (snapshot == null ||
                snapshot.PlayerId != playerId ||
                snapshot.HiveId != hiveId ||
                !string.Equals(
                    snapshot.ContractVersion,
                    ContractVersion,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    snapshot.CatalogVersion,
                    CatalogVersion,
                    StringComparison.Ordinal) ||
                snapshot.Revision < 0 ||
                !IsUtc(snapshot.ServerTimeUtc) ||
                snapshot.Offers == null ||
                snapshot.Balances == null ||
                snapshot.Counts == null ||
                snapshot.LegacyRoles == null)
                throw InvalidResponse(
                    "The doctrine recruitment snapshot is incomplete.");

            ValidateOffers(snapshot.Offers);
            ValidateBalances(snapshot.Balances);
            ValidateCounts(snapshot.Counts);
            if (snapshot.LegacyRoles.Count !=
                    CanonicalLegacyRoles.Count ||
                snapshot.LegacyRoles.Any(string.IsNullOrWhiteSpace) ||
                snapshot.LegacyRoles.Distinct(StringComparer.Ordinal).Count() !=
                    snapshot.LegacyRoles.Count ||
                !CanonicalLegacyRoles.SetEquals(snapshot.LegacyRoles))
                throw InvalidResponse(
                    "The doctrine recruitment legacy roles are invalid.");

            if (snapshot.ActiveOperation != null)
                ValidateOperation(
                    snapshot.ActiveOperation,
                    snapshot.ServerTimeUtc);
        }

        internal static void ValidateMutationResponse(
            RemoteDoctrineRecruitmentResponse response,
            Guid playerId,
            Guid hiveId,
            Guid expectedOperationId,
            string expectedFamily,
            long expectedRevision,
            string idempotencyKey,
            string expectedCode,
            bool starting)
        {
            if (response == null ||
                response.Receipt == null ||
                response.Snapshot == null)
                throw InvalidResponse(
                    "The doctrine recruitment response is incomplete.");
            ValidateSnapshot(response.Snapshot, playerId, hiveId);
            RemoteDoctrineRecruitmentReceipt receipt = response.Receipt;
            CanonicalOffer offer = CanonicalOffers[expectedFamily];
            long nextRevision;
            try
            {
                nextRevision = checked(expectedRevision + 1L);
            }
            catch (OverflowException)
            {
                throw InvalidResponse(
                    "The doctrine recruitment revision overflowed.");
            }

            if (receipt.PlayerId != playerId ||
                receipt.HiveId != hiveId ||
                receipt.OperationId == Guid.Empty ||
                !string.Equals(
                    receipt.IdempotencyKey,
                    idempotencyKey,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    receipt.Family,
                    expectedFamily,
                    StringComparison.Ordinal) ||
                receipt.BatchSize != offer.BatchSize ||
                receipt.RevisionBefore != expectedRevision ||
                receipt.RevisionAfter != nextRevision ||
                response.Snapshot.Revision < receipt.RevisionAfter ||
                !IsUtc(receipt.AcceptedAtUtc) ||
                receipt.AcceptedAtUtc > response.Snapshot.ServerTimeUtc ||
                !string.Equals(
                    receipt.Code,
                    expectedCode,
                    StringComparison.Ordinal) ||
                (!starting &&
                 receipt.OperationId != expectedOperationId))
                throw InvalidResponse(
                    "The doctrine recruitment receipt is inconsistent.");

            if (response.Snapshot.Revision != receipt.RevisionAfter)
                return;

            RemoteDoctrineRecruitmentOperation active =
                response.Snapshot.ActiveOperation;
            if (starting)
            {
                if (active == null ||
                    active.OperationId != receipt.OperationId ||
                    !string.Equals(
                        active.Family,
                        expectedFamily,
                        StringComparison.Ordinal) ||
                    active.BatchSize != receipt.BatchSize)
                    throw InvalidResponse(
                        "The fresh recruitment start is inconsistent.");
            }
            else if (active != null)
            {
                throw InvalidResponse(
                    "The fresh recruitment claim is inconsistent.");
            }
        }

        private static void ValidateOffers(
            IReadOnlyList<RemoteDoctrineRecruitmentOffer> offers)
        {
            if (offers.Count != CanonicalOffers.Count)
                throw InvalidResponse(
                    "The doctrine recruitment offer count is invalid.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (RemoteDoctrineRecruitmentOffer offer in offers)
            {
                if (offer == null ||
                    !CanonicalOffers.TryGetValue(
                        offer.Family ?? string.Empty,
                        out CanonicalOffer expected) ||
                    !seen.Add(offer.Family) ||
                    offer.BatchSize != expected.BatchSize ||
                    offer.HoneyCost != expected.HoneyCost ||
                    offer.PollenCost != expected.PollenCost ||
                    offer.Duration != expected.Duration ||
                    offer.Duration <= TimeSpan.Zero ||
                    offer.Duration > MaximumDuration)
                    throw InvalidResponse(
                        "A doctrine recruitment offer is invalid.");
            }
        }

        private static void ValidateBalances(
            IReadOnlyDictionary
                <string, RemoteDoctrineRecruitmentBalance> balances)
        {
            if (balances.Count != 2 ||
                !balances.ContainsKey("honey") ||
                !balances.ContainsKey("pollen"))
                throw InvalidResponse(
                    "The doctrine recruitment balances are incomplete.");
            foreach (KeyValuePair
                         <string, RemoteDoctrineRecruitmentBalance> entry
                     in balances)
            {
                RemoteDoctrineRecruitmentBalance balance = entry.Value;
                if (balance == null ||
                    balance.Amount < 0 ||
                    balance.Capacity <= 0 ||
                    balance.Amount > balance.Capacity)
                    throw InvalidResponse(
                        "A doctrine recruitment balance is invalid.");
            }
        }

        private static void ValidateCounts(
            IReadOnlyDictionary<string, long> counts)
        {
            if (counts.Count > CanonicalOffers.Count)
                throw InvalidResponse(
                    "The doctrine recruitment roster is invalid.");
            foreach (KeyValuePair<string, long> entry in counts)
            {
                if (!CanonicalOffers.ContainsKey(entry.Key) ||
                    entry.Value < 0 ||
                    entry.Value > 1000000000L)
                    throw InvalidResponse(
                        "A doctrine recruitment count is invalid.");
            }
        }

        private static void ValidateOperation(
            RemoteDoctrineRecruitmentOperation operation,
            DateTimeOffset serverTimeUtc)
        {
            if (operation.OperationId == Guid.Empty ||
                !CanonicalOffers.TryGetValue(
                    operation.Family ?? string.Empty,
                    out CanonicalOffer offer) ||
                operation.BatchSize != offer.BatchSize ||
                !IsUtc(operation.StartedAtUtc) ||
                !IsUtc(operation.EndsAtUtc) ||
                operation.StartedAtUtc > serverTimeUtc ||
                operation.EndsAtUtc !=
                    operation.StartedAtUtc.Add(offer.Duration))
                throw InvalidResponse(
                    "The doctrine recruitment operation is invalid.");
            bool complete = operation.EndsAtUtc <= serverTimeUtc;
            string expectedStatus = complete
                ? AwaitingCompletionStatus
                : RunningStatus;
            if (!string.Equals(
                    operation.Status,
                    expectedStatus,
                    StringComparison.Ordinal))
                throw InvalidResponse(
                    "The doctrine recruitment operation status is invalid.");
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
                        await refreshable
                            .GetFreshSessionAsync(cancellationToken)
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
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error !=
                    AuthenticatedGameRestError.Unauthorized)
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
                replacement =
                    await refreshable.RefreshAfterUnauthorizedAsync(
                            context.AccessToken,
                            cancellationToken)
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

            if (replacement == null ||
                replacement.PlayerId != context.PlayerId ||
                string.IsNullOrWhiteSpace(replacement.AccessToken) ||
                replacement.AccessToken.Length > 8192)
            {
                await refreshable.InvalidateUnauthorizedSessionAsync(
                        context.AccessToken,
                        cancellationToken)
                    .ConfigureAwait(false);
                throw InvalidResponse(
                    "The refreshed game session changed identity.");
            }

            try
            {
                return await transport.SendAsync<T>(
                        request,
                        replacement.AccessToken,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error ==
                    AuthenticatedGameRestError.Unauthorized)
                {
                    await refreshable.InvalidateUnauthorizedSessionAsync(
                            replacement.AccessToken,
                            cancellationToken)
                        .ConfigureAwait(false);
                    throw new HivePerimeterClientException(
                        HivePerimeterClientError.AuthenticationRequired,
                        "The refreshed game session was rejected.");
                }
                throw MapTransportFailure(exception);
            }
        }

        private async Task SaveMutationSnapshotBestEffortAsync(
            Guid playerId,
            Guid hiveId,
            RemoteDoctrineRecruitmentSnapshot snapshot)
        {
            LastReadSource = GameReadSource.Server;
            LastReadCachedAtUtc = default(DateTimeOffset);
            await SaveValidatedReadBestEffortAsync(
                    playerId,
                    hiveId,
                    Path(hiveId),
                    snapshot,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        private async Task SaveValidatedReadBestEffortAsync(
            Guid playerId,
            Guid hiveId,
            string path,
            RemoteDoctrineRecruitmentSnapshot snapshot,
            CancellationToken cancellationToken)
        {
            if (readCache == null ||
                !readCache.IsProtectionAvailable)
                return;
            try
            {
                await readCache.SaveValidatedReadAsync(
                        playerId,
                        hiveId,
                        ContractVersion,
                        path,
                        snapshot,
                        cancellationToken)
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

        private async Task<RemoteDoctrineRecruitmentSnapshot>
            TryLoadCacheAsync(
                Guid hiveId,
                string path,
                CancellationToken cancellationToken)
        {
            if (readCache == null ||
                !readCache.IsProtectionAvailable)
                return null;
            if (!TryGetKnownPlayerId(out Guid playerId)) return null;
            ProtectedGameReadCacheHit
                <RemoteDoctrineRecruitmentSnapshot> hit =
                await readCache.TryLoadAsync
                        <RemoteDoctrineRecruitmentSnapshot>(
                            playerId,
                            hiveId,
                            ContractVersion,
                            path,
                            cancellationToken)
                    .ConfigureAwait(false);
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
            if (sessionSource.TryGetSession(
                    out GameAccountSession session) &&
                session != null &&
                session.PlayerId != Guid.Empty)
            {
                playerId = session.PlayerId;
                return true;
            }
            playerId = Guid.Empty;
            return false;
        }

        private static SessionContext RequireUsableSession(
            GameAccountSession session)
        {
            if (session == null ||
                session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) ||
                session.AccessToken.Length > 8192)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.AuthenticationRequired,
                    "An official account session is required.");
            return new SessionContext(
                session.PlayerId,
                session.AccessToken);
        }

        private static bool IsOfflineEligible(Exception exception)
        {
            HivePerimeterClientException client =
                exception as HivePerimeterClientException;
            return client != null &&
                (client.Error ==
                    HivePerimeterClientError.TransportFailure ||
                 client.Error == HivePerimeterClientError.NotConfigured);
        }

        private static HivePerimeterClientException MapSessionFailure(
            MobileAccountSessionException exception)
        {
            if (exception.Error ==
                MobileAccountSessionError.TransportFailure)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.TransportFailure,
                    exception.SafeCode);
            if (exception.Error ==
                MobileAccountSessionError.NotConfigured)
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
            if (exception.Error ==
                AuthenticatedGameRestError.NetworkFailure)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.TransportFailure,
                    exception.SafeCode);
            if (exception.Error ==
                AuthenticatedGameRestError.Unauthorized)
                return new HivePerimeterClientException(
                    HivePerimeterClientError.AuthenticationRequired,
                    exception.SafeCode);
            return InvalidResponse(exception.SafeCode);
        }

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty)
                throw InvalidRequest(
                    "A hive identifier is required.");
        }

        private static void RequireFamily(string family)
        {
            if (string.IsNullOrWhiteSpace(family) ||
                !CanonicalOffers.ContainsKey(family))
                throw InvalidRequest(
                    "A supported doctrine family is required.");
        }

        private static void RequireRevision(long revision)
        {
            if (revision < 0 || revision == long.MaxValue)
                throw InvalidRequest(
                    "A bounded doctrine roster revision is required.");
        }

        private static void RequireIdempotencyKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Trim() != value ||
                value.Length > 256 ||
                value.Any(character =>
                    !((character >= 'A' && character <= 'Z') ||
                      (character >= 'a' && character <= 'z') ||
                      (character >= '0' && character <= '9') ||
                      character == '-' ||
                      character == '_' ||
                      character == '.')))
                throw InvalidRequest(
                    "A safe idempotency key is required.");
        }

        private static bool IsUtc(DateTimeOffset value)
        {
            return value != default(DateTimeOffset) &&
                value.Offset == TimeSpan.Zero;
        }

        private static HivePerimeterClientException InvalidRequest(
            string message)
        {
            return new HivePerimeterClientException(
                HivePerimeterClientError.InvalidRequest,
                message);
        }

        private static HivePerimeterClientException InvalidResponse(
            string message)
        {
            return new HivePerimeterClientException(
                HivePerimeterClientError.InvalidResponse,
                message);
        }

        private sealed class CanonicalOffer
        {
            public CanonicalOffer(
                int batchSize,
                long honeyCost,
                long pollenCost,
                TimeSpan duration)
            {
                BatchSize = batchSize;
                HoneyCost = honeyCost;
                PollenCost = pollenCost;
                Duration = duration;
            }

            public int BatchSize { get; }
            public long HoneyCost { get; }
            public long PollenCost { get; }
            public TimeSpan Duration { get; }
        }

        private sealed class SessionContext
        {
            public SessionContext(
                Guid playerId,
                string accessToken)
            {
                PlayerId = playerId;
                AccessToken = accessToken;
            }

            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
