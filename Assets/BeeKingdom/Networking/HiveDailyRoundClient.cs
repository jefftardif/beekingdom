using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteHiveDailyRoundSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public DateTimeOffset DayUtc { get; set; }
        public DateTimeOffset NextResetUtc { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public long Revision { get; set; }
        public Dictionary<string, bool> Facts { get; set; }
        public int CompletedCount { get; set; }
        public long HoneyReward { get; set; }
        public long PollenReward { get; set; }
        public bool ClaimAvailable { get; set; }
        public DateTimeOffset? ClaimedAtUtc { get; set; }
    }

    public sealed class HiveDailyRoundClaimRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
        public string ExpectedDayUtc { get; set; }
    }

    public sealed class RemoteHiveDailyRoundReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string IdempotencyKey { get; set; }
        public DateTimeOffset DayUtc { get; set; }
        public long RevisionBefore { get; set; }
        public long RevisionAfter { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public long CreditedHoney { get; set; }
        public long CreditedPollen { get; set; }
        public string Code { get; set; }
    }

    public sealed class RemoteHiveDailyRoundClaimResponse
    {
        public RemoteHiveDailyRoundReceipt Receipt { get; set; }
        public RemoteHiveDailyRoundSnapshot Snapshot { get; set; }
    }

    public interface IHiveDailyRoundClient
    {
        GameReadSource LastReadSource { get; }
        DateTimeOffset LastReadCachedAtUtc { get; }
        Task<RemoteHiveDailyRoundSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteHiveDailyRoundClaimResponse> ClaimAsync(
            Guid hiveId,
            DateTimeOffset expectedDayUtc,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public sealed class HiveDailyRoundClient : IHiveDailyRoundClient
    {
        public const string ContractVersion = "living-hive-daily-round-v1";
        public const string CollectionFact = "collection_received";
        public const string OperationFact = "operation_launched";
        public const string SnapshotFact = "snapshot_read";
        public const string ClaimedCode = "game.daily_round_claimed";
        public const long HoneyReward = 120;
        public const long PollenReward = 60;

        private static readonly string[] ExpectedFacts =
        {
            CollectionFact,
            OperationFact,
            SnapshotFact
        };

        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;
        private readonly ProtectedGameReadCache readCache;

        public HiveDailyRoundClient(
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
                "mobile_daily_round_contract:" + ContractVersion,
                "daily_round_day_authority:server_utc",
                "daily_round_fact_authority:server_transactions",
                "daily_round_reward_authority:server",
                "daily_round_get_protected_cache:" +
                    (readCache != null && readCache.IsProtectionAvailable)
                    .ToString()
                    .ToLowerInvariant(),
                "daily_round_cache_read_only:true",
                "daily_round_claim_expected_day_required:true",
                "daily_round_claim_idempotency_required:true",
                "daily_round_claim_offline:false",
                "daily_round_local_fact_submission:false",
                "daily_round_local_reward_credit:false",
                "daily_round_read_source:" + LastReadSource.ToString().ToLowerInvariant()
            };
        }

        public async Task<RemoteHiveDailyRoundSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            string path = Path(hiveId);
            try
            {
                SessionContext context =
                    await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
                var request = new AuthenticatedGameRestRequest("GET", path);
                RemoteHiveDailyRoundSnapshot snapshot =
                    await SendWithSingleAuthenticationRefreshAsync<RemoteHiveDailyRoundSnapshot>(
                        request,
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
                RemoteHiveDailyRoundSnapshot cached =
                    await TryLoadCacheAsync(hiveId, path, cancellationToken).ConfigureAwait(false);
                if (cached != null) return cached;
                throw;
            }
        }

        public async Task<RemoteHiveDailyRoundClaimResponse> ClaimAsync(
            Guid hiveId,
            DateTimeOffset expectedDayUtc,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            RequireDay(expectedDayUtc);
            RequireRevision(expectedRevision);
            RequireIdempotencyKey(idempotencyKey);
            SessionContext context =
                await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                ClaimPath(hiveId),
                new HiveDailyRoundClaimRequest
                {
                    ExpectedRevision = expectedRevision,
                    IdempotencyKey = idempotencyKey,
                    ExpectedDayUtc = expectedDayUtc.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture)
                });
            RemoteHiveDailyRoundClaimResponse response =
                await SendWithSingleAuthenticationRefreshAsync<RemoteHiveDailyRoundClaimResponse>(
                    request,
                    context,
                    cancellationToken).ConfigureAwait(false);
            ValidateClaimResponse(
                response,
                context.PlayerId,
                hiveId,
                expectedDayUtc,
                expectedRevision,
                idempotencyKey);
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

        internal static void ValidateSnapshot(
            RemoteHiveDailyRoundSnapshot snapshot,
            Guid playerId,
            Guid hiveId)
        {
            if (snapshot == null ||
                snapshot.PlayerId != playerId ||
                snapshot.HiveId != hiveId)
                throw InvalidResponse(
                    "The daily round snapshot belongs to another account or hive.");
            if (!string.Equals(
                    snapshot.ContractVersion,
                    ContractVersion,
                    StringComparison.Ordinal) ||
                snapshot.Revision < 0 ||
                !IsUtc(snapshot.DayUtc) ||
                snapshot.DayUtc.TimeOfDay != TimeSpan.Zero ||
                !IsUtc(snapshot.NextResetUtc) ||
                snapshot.NextResetUtc != snapshot.DayUtc.AddDays(1) ||
                !IsUtc(snapshot.ServerTimeUtc) ||
                snapshot.ServerTimeUtc < snapshot.DayUtc ||
                snapshot.ServerTimeUtc >= snapshot.NextResetUtc)
                throw InvalidResponse(
                    "The daily round contract, revision, or server day is invalid.");

            if (snapshot.Facts == null ||
                snapshot.Facts.Count != ExpectedFacts.Length ||
                ExpectedFacts.Any(key => !snapshot.Facts.ContainsKey(key)) ||
                snapshot.Facts.Keys.Any(key =>
                    !ExpectedFacts.Contains(key, StringComparer.Ordinal)))
                throw InvalidResponse("The daily round facts are incomplete or unknown.");

            int completed = ExpectedFacts.Count(key => snapshot.Facts[key]);
            bool allCompleted = completed == ExpectedFacts.Length;
            if (snapshot.CompletedCount != completed ||
                snapshot.HoneyReward != HoneyReward ||
                snapshot.PollenReward != PollenReward ||
                snapshot.ClaimAvailable !=
                    (allCompleted && !snapshot.ClaimedAtUtc.HasValue))
                throw InvalidResponse(
                    "The daily round completion or reward projection is invalid.");

            if (snapshot.ClaimedAtUtc.HasValue &&
                (!allCompleted ||
                 !IsUtc(snapshot.ClaimedAtUtc.Value) ||
                 snapshot.ClaimedAtUtc.Value < snapshot.DayUtc ||
                 snapshot.ClaimedAtUtc.Value > snapshot.ServerTimeUtc))
                throw InvalidResponse("The daily round claim timestamp is invalid.");
        }

        internal static void ValidateClaimResponse(
            RemoteHiveDailyRoundClaimResponse response,
            Guid playerId,
            Guid hiveId,
            DateTimeOffset expectedDayUtc,
            long expectedRevision,
            string idempotencyKey)
        {
            if (response == null || response.Receipt == null || response.Snapshot == null)
                throw InvalidResponse("The daily round claim response is incomplete.");
            ValidateSnapshot(response.Snapshot, playerId, hiveId);
            RemoteHiveDailyRoundReceipt receipt = response.Receipt;
            if (receipt.PlayerId != playerId ||
                receipt.HiveId != hiveId ||
                !string.Equals(
                    receipt.IdempotencyKey,
                    idempotencyKey,
                    StringComparison.Ordinal) ||
                !string.Equals(receipt.Code, ClaimedCode, StringComparison.Ordinal) ||
                receipt.DayUtc != expectedDayUtc ||
                receipt.DayUtc != response.Snapshot.DayUtc ||
                receipt.RevisionBefore != expectedRevision ||
                receipt.RevisionAfter != expectedRevision + 1 ||
                response.Snapshot.Revision < receipt.RevisionAfter ||
                receipt.CreditedHoney != HoneyReward ||
                receipt.CreditedPollen != PollenReward ||
                !IsUtc(receipt.AcceptedAtUtc) ||
                receipt.AcceptedAtUtc < response.Snapshot.DayUtc ||
                receipt.AcceptedAtUtc > response.Snapshot.ServerTimeUtc ||
                response.Snapshot.DayUtc != expectedDayUtc ||
                !response.Snapshot.ClaimedAtUtc.HasValue ||
                response.Snapshot.ClaimedAtUtc.Value != receipt.AcceptedAtUtc ||
                response.Snapshot.ClaimAvailable)
                throw InvalidResponse("The daily round claim receipt is invalid.");
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
            RemoteHiveDailyRoundSnapshot snapshot,
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

        private async Task<RemoteHiveDailyRoundSnapshot> TryLoadCacheAsync(
            Guid hiveId,
            string path,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return null;
            Guid playerId;
            if (!TryGetKnownPlayerId(out playerId)) return null;
            ProtectedGameReadCacheHit<RemoteHiveDailyRoundSnapshot> hit =
                await readCache.TryLoadAsync<RemoteHiveDailyRoundSnapshot>(
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

        private static bool IsUtc(DateTimeOffset value)
        {
            return value != default(DateTimeOffset) &&
                value.Offset == TimeSpan.Zero;
        }

        private static string Path(Guid hiveId)
        {
            return "/game/v1/hives/" + hiveId.ToString("D") + "/daily-round";
        }

        public static string ClaimPath(Guid hiveId)
        {
            RequireHive(hiveId);
            return Path(hiveId) + "/claim";
        }

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty)
                throw InvalidRequest("A hive identifier is required.");
        }

        private static void RequireDay(DateTimeOffset dayUtc)
        {
            if (!IsUtc(dayUtc) || dayUtc.TimeOfDay != TimeSpan.Zero)
                throw InvalidRequest("A canonical UTC daily round day is required.");
        }

        private static void RequireRevision(long revision)
        {
            if (revision < 0 || revision == long.MaxValue)
                throw InvalidRequest("A bounded daily round revision is required.");
        }

        private static void RequireIdempotencyKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Trim() != value ||
                value.Length > 256 ||
                value.Any(character =>
                    character < 0x21 ||
                    character > 0x7e ||
                    character == '\"' ||
                    character == '\\'))
                throw InvalidRequest("A bounded daily round idempotency key is required.");
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
