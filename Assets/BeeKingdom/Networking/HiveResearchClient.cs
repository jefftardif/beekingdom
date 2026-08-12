using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteHiveResearchBalance
    {
        public long Amount { get; set; }
        public long Capacity { get; set; }
    }

    public sealed class RemoteHiveResearchEffects
    {
        public int HoneyProductionBonusBps { get; set; }
        public int WaxCapacityBonusBps { get; set; }
        public int WaxProductionBonusBps { get; set; }
        public int PollenProductionBonusBps { get; set; }
        public int PollenCapacityBonusBps { get; set; }
        public int GlobalCapacityBonusBps { get; set; }
    }

    public sealed class RemoteHiveResearchOffer
    {
        public string ResearchId { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, long> Costs { get; set; }
        public RemoteHiveResearchEffects Effects { get; set; }
        public List<string> Prerequisites { get; set; }
    }

    public sealed class RemoteHiveResearchCompletion
    {
        public string ResearchId { get; set; }
        public DateTimeOffset CompletedAtUtc { get; set; }
        public RemoteHiveResearchEffects Effects { get; set; }
    }

    public sealed class RemoteHiveResearchOperation
    {
        public Guid OperationId { get; set; }
        public string ResearchId { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset CompletesAtUtc { get; set; }
        public string Status { get; set; }
    }

    public sealed class RemoteHiveResearchSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public string CatalogVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public Dictionary<string, RemoteHiveResearchBalance> Balances { get; set; }
        public List<RemoteHiveResearchCompletion> Completed { get; set; }
        public List<RemoteHiveResearchOffer> Offers { get; set; }
        public RemoteHiveResearchOperation ActiveOperation { get; set; }
    }

    public sealed class HiveResearchMutationRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteHiveResearchReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string IdempotencyKey { get; set; }
        public Guid OperationId { get; set; }
        public string ResearchId { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public string Code { get; set; }
    }

    public sealed class RemoteHiveResearchMutationResponse
    {
        public RemoteHiveResearchReceipt Receipt { get; set; }
        public RemoteHiveResearchSnapshot Snapshot { get; set; }
    }

    public interface IHiveResearchClient
    {
        GameReadSource LastReadSource { get; }
        DateTimeOffset LastReadCachedAtUtc { get; }
        Task<RemoteHiveResearchSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteHiveResearchMutationResponse> StartAsync(
            Guid hiveId,
            string researchId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteHiveResearchMutationResponse> CompleteAsync(
            Guid hiveId,
            Guid operationId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public sealed class HiveResearchClient : IHiveResearchClient
    {
        public const string ContractVersion = "living-hive-research-v1";
        public const string ForagingRoutesId = "foraging_routes_i";
        public const string TemperedCombsId = "tempered_combs_i";
        public const string RunningStatus = "running";
        public const string AwaitingCompletionStatus = "awaiting_completion";
        public const string StartedCode = "game.research_started";
        public const string CompletedCode = "game.research_completed";
        public static readonly TimeSpan MaximumDuration = TimeSpan.FromDays(7);

        public const string PollenSortingIId = "pollen_sorting_i";
        public const string PollenSortingIiId = "pollen_sorting_ii";
        public const string PollenSortingIiiId = "pollen_sorting_iii";
        public const string SealedReservesId = "sealed_reserves";
        private const int MaximumEffectBps = 100000;
        private const int MaximumPrerequisites = 8;
        private static readonly HashSet<string> SupportedResearch =
            new HashSet<string>(StringComparer.Ordinal)
            {
                ForagingRoutesId, "foraging_routes_ii", "foraging_routes_iii",
                TemperedCombsId, "tempered_combs_ii", "tempered_combs_iii",
                PollenSortingIId, PollenSortingIiId, PollenSortingIiiId,
                SealedReservesId
            };
        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;
        private readonly ProtectedGameReadCache readCache;

        public HiveResearchClient(
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
                "mobile_research_contract:" + ContractVersion,
                "research_catalog_authority:server",
                "research_cost_authority:server",
                "research_time_authority:server",
                "research_effect_authority:server",
                "research_get_protected_cache:" + (readCache != null && readCache.IsProtectionAvailable).ToString().ToLowerInvariant(),
                "research_cache_read_only:true",
                "research_validated_mutation_refreshes_cache:true",
                "research_mutation_offline_retry:false",
                "research_local_debit:false",
                "research_local_completion:false",
                "research_local_effect_application:false",
                "research_read_source:" + LastReadSource.ToString().ToLowerInvariant()
            };
        }

        public async Task<RemoteHiveResearchSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            string path = Path(hiveId);
            try
            {
                SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
                var request = new AuthenticatedGameRestRequest("GET", path);
                RemoteHiveResearchSnapshot snapshot =
                    await SendWithSingleAuthenticationRefreshAsync<RemoteHiveResearchSnapshot>(request, context, cancellationToken)
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
                RemoteHiveResearchSnapshot cached = await TryLoadCacheAsync(hiveId, path, cancellationToken)
                    .ConfigureAwait(false);
                if (cached != null) return cached;
                throw;
            }
        }

        public async Task<RemoteHiveResearchMutationResponse> StartAsync(
            Guid hiveId,
            string researchId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            RequireResearch(researchId);
            RequireRevision(expectedRevision);
            RequireIdempotencyKey(idempotencyKey);
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                Path(hiveId) + "/" + researchId + "/start",
                new HiveResearchMutationRequest
                {
                    ExpectedRevision = expectedRevision,
                    IdempotencyKey = idempotencyKey
                });
            RemoteHiveResearchMutationResponse response =
                await SendWithSingleAuthenticationRefreshAsync<RemoteHiveResearchMutationResponse>(request, context, cancellationToken)
                    .ConfigureAwait(false);
            ValidateMutationResponse(response, context.PlayerId, hiveId, researchId, Guid.Empty,
                expectedRevision, idempotencyKey, StartedCode, true);
            await PublishValidatedMutationAsync(context.PlayerId, hiveId, response).ConfigureAwait(false);
            return response;
        }

        public async Task<RemoteHiveResearchMutationResponse> CompleteAsync(
            Guid hiveId,
            Guid operationId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            if (operationId == Guid.Empty) throw InvalidRequest("A research operation identifier is required.");
            RequireRevision(expectedRevision);
            RequireIdempotencyKey(idempotencyKey);
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                Path(hiveId) + "/" + operationId.ToString("D") + "/complete",
                new HiveResearchMutationRequest
                {
                    ExpectedRevision = expectedRevision,
                    IdempotencyKey = idempotencyKey
                });
            RemoteHiveResearchMutationResponse response =
                await SendWithSingleAuthenticationRefreshAsync<RemoteHiveResearchMutationResponse>(request, context, cancellationToken)
                    .ConfigureAwait(false);
            ValidateMutationResponse(response, context.PlayerId, hiveId, string.Empty, operationId,
                expectedRevision, idempotencyKey, CompletedCode, false);
            await PublishValidatedMutationAsync(context.PlayerId, hiveId, response).ConfigureAwait(false);
            return response;
        }

        private async Task PublishValidatedMutationAsync(
            Guid playerId,
            Guid hiveId,
            RemoteHiveResearchMutationResponse response)
        {
            LastReadSource = GameReadSource.Server;
            LastReadCachedAtUtc = default(DateTimeOffset);
            await SaveValidatedReadBestEffortAsync(
                playerId, hiveId, Path(hiveId), response.Snapshot, CancellationToken.None).ConfigureAwait(false);
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
            catch (OperationCanceledException) { throw; }
            catch (MobileAccountSessionException exception) { throw MapSessionFailure(exception); }

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
            RemoteHiveResearchSnapshot snapshot,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return;
            try
            {
                await readCache.SaveValidatedReadAsync(playerId, hiveId, ContractVersion, path, snapshot, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }

        private async Task<RemoteHiveResearchSnapshot> TryLoadCacheAsync(
            Guid hiveId,
            string path,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return null;
            Guid playerId;
            if (!TryGetKnownPlayerId(out playerId)) return null;
            ProtectedGameReadCacheHit<RemoteHiveResearchSnapshot> hit =
                await readCache.TryLoadAsync<RemoteHiveResearchSnapshot>(
                    playerId, hiveId, ContractVersion, path, cancellationToken).ConfigureAwait(false);
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

        private static void ValidateMutationResponse(
            RemoteHiveResearchMutationResponse response,
            Guid playerId,
            Guid hiveId,
            string expectedResearchId,
            Guid expectedOperationId,
            long expectedRevision,
            string idempotencyKey,
            string expectedCode,
            bool starting)
        {
            if (response == null || response.Receipt == null || response.Snapshot == null)
                throw InvalidResponse("The research response is incomplete.");
            ValidateSnapshot(response.Snapshot, playerId, hiveId);
            RemoteHiveResearchReceipt receipt = response.Receipt;
            long nextRevision;
            try { nextRevision = checked(expectedRevision + 1L); }
            catch (OverflowException) { throw InvalidResponse("The research revision overflowed."); }

            if (receipt.PlayerId != playerId || receipt.HiveId != hiveId || receipt.OperationId == Guid.Empty ||
                !SupportedResearch.Contains(receipt.ResearchId) ||
                (!string.IsNullOrEmpty(expectedResearchId) && receipt.ResearchId != expectedResearchId) ||
                (expectedOperationId != Guid.Empty && receipt.OperationId != expectedOperationId) ||
                receipt.Revision != nextRevision || response.Snapshot.Revision < receipt.Revision ||
                !IsUtc(receipt.AcceptedAtUtc) || receipt.AcceptedAtUtc > response.Snapshot.ServerTimeUtc ||
                !string.Equals(receipt.IdempotencyKey, idempotencyKey, StringComparison.Ordinal) ||
                !string.Equals(receipt.Code, expectedCode, StringComparison.Ordinal))
                throw InvalidResponse("The research receipt is inconsistent.");

            RemoteHiveResearchOperation active = response.Snapshot.ActiveOperation;
            bool completed = response.Snapshot.Completed.Any(item =>
                string.Equals(item.ResearchId, receipt.ResearchId, StringComparison.Ordinal));
            if (starting)
            {
                bool activeMatches = active != null && active.OperationId == receipt.OperationId &&
                    string.Equals(active.ResearchId, receipt.ResearchId, StringComparison.Ordinal);
                if (!activeMatches && !completed)
                    throw InvalidResponse("The started research is not bound to the snapshot.");
            }
            else if (!completed || active != null && active.OperationId == receipt.OperationId)
            {
                throw InvalidResponse("The completed research is not authoritative in the snapshot.");
            }
        }

        private static void ValidateSnapshot(RemoteHiveResearchSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null || snapshot.PlayerId != playerId || snapshot.HiveId != hiveId)
                throw InvalidResponse("The research snapshot belongs to another account or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal) ||
                !IsSafeToken(snapshot.CatalogVersion) || snapshot.Revision < 0 || !IsUtc(snapshot.ServerTimeUtc))
                throw InvalidResponse("The research contract, revision, catalog, or server time is invalid.");
            if (snapshot.Balances == null || snapshot.Completed == null || snapshot.Offers == null ||
                snapshot.Balances.Count > 16 || snapshot.Completed.Count > 64 || snapshot.Offers.Count > 16)
                throw InvalidResponse("The research snapshot is incomplete or unbounded.");

            foreach (KeyValuePair<string, RemoteHiveResearchBalance> entry in snapshot.Balances)
            {
                RemoteHiveResearchBalance balance = entry.Value;
                if (!IsSafeToken(entry.Key) || balance == null || balance.Amount < 0 ||
                    balance.Capacity < 0 || balance.Amount > balance.Capacity)
                    throw InvalidResponse("A research resource balance is invalid.");
            }

            if (snapshot.Completed.Select(item => item == null ? string.Empty : item.ResearchId)
                .Distinct(StringComparer.Ordinal).Count() != snapshot.Completed.Count)
                throw InvalidResponse("A completed research is duplicated.");
            foreach (RemoteHiveResearchCompletion completion in snapshot.Completed)
            {
                if (completion == null || !SupportedResearch.Contains(completion.ResearchId) ||
                    !IsUtc(completion.CompletedAtUtc) || completion.CompletedAtUtc > snapshot.ServerTimeUtc ||
                    !ValidEffects(completion.Effects))
                    throw InvalidResponse("A completed research is invalid.");
            }

            if (snapshot.Offers.Select(item => item == null ? string.Empty : item.ResearchId)
                .Distinct(StringComparer.Ordinal).Count() != snapshot.Offers.Count)
                throw InvalidResponse("A research offer is duplicated.");
            foreach (RemoteHiveResearchOffer offer in snapshot.Offers)
            {
                if (offer == null || !SupportedResearch.Contains(offer.ResearchId) ||
                    offer.Duration <= TimeSpan.Zero || offer.Duration > MaximumDuration ||
                    offer.Costs == null || offer.Costs.Count == 0 || offer.Costs.Count > 8 ||
                    !ValidEffects(offer.Effects) ||
                    snapshot.Completed.Any(item => item.ResearchId == offer.ResearchId))
                    throw InvalidResponse("A research offer is invalid.");
                foreach (KeyValuePair<string, long> cost in offer.Costs)
                {
                    if (!IsSafeToken(cost.Key) || cost.Value <= 0 || cost.Value > 1000000000L ||
                        !snapshot.Balances.ContainsKey(cost.Key))
                        throw InvalidResponse("A research cost is invalid.");
                }
                if (offer.Prerequisites != null)
                {
                    if (offer.Prerequisites.Count > MaximumPrerequisites)
                        throw InvalidResponse("A research offer has too many prerequisites.");
                    foreach (string prerequisite in offer.Prerequisites)
                        if (!SupportedResearch.Contains(prerequisite) || prerequisite == offer.ResearchId)
                            throw InvalidResponse("A research prerequisite is invalid.");
                }
            }

            RemoteHiveResearchOperation operation = snapshot.ActiveOperation;
            if (operation == null) return;
            if (operation.OperationId == Guid.Empty || !SupportedResearch.Contains(operation.ResearchId) ||
                !IsUtc(operation.StartedAtUtc) || !IsUtc(operation.CompletesAtUtc) ||
                operation.CompletesAtUtc <= operation.StartedAtUtc ||
                operation.CompletesAtUtc - operation.StartedAtUtc > MaximumDuration ||
                snapshot.Completed.Any(item => item.ResearchId == operation.ResearchId) ||
                (operation.Status != RunningStatus && operation.Status != AwaitingCompletionStatus) ||
                (operation.Status == RunningStatus && operation.CompletesAtUtc <= snapshot.ServerTimeUtc) ||
                (operation.Status == AwaitingCompletionStatus && operation.CompletesAtUtc > snapshot.ServerTimeUtc))
                throw InvalidResponse("The active research operation is inconsistent.");
        }

        // Chaque recherche du catalogue accorde un bonus numerique reel (parfois plusieurs a la
        // fois, ex. tempered_combs_ii accorde a la fois capacite ET production de cire) - la
        // validation reste generique (bornes + au moins un effet non nul) plutot qu'une carte
        // figee par identifiant, pour rester compatible avec un catalogue qui grandit cote
        // serveur sans devoir modifier ce client a chaque nouvelle recherche.
        private static bool ValidEffects(RemoteHiveResearchEffects effects)
        {
            if (effects == null) return false;
            int[] values =
            {
                effects.HoneyProductionBonusBps, effects.WaxCapacityBonusBps, effects.WaxProductionBonusBps,
                effects.PollenProductionBonusBps, effects.PollenCapacityBonusBps, effects.GlobalCapacityBonusBps
            };
            bool anyPositive = false;
            foreach (int value in values)
            {
                if (value < 0 || value > MaximumEffectBps) return false;
                if (value > 0) anyPositive = true;
            }
            return anyPositive;
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
            return client != null && (client.Error == HivePerimeterClientError.TransportFailure ||
                client.Error == HivePerimeterClientError.NotConfigured);
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
                if (!((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') ||
                    character == '.' || character == '_' || character == '-')) return false;
            }
            return true;
        }

        private static bool IsUtc(DateTimeOffset value)
        {
            return value != default(DateTimeOffset) && value.Offset == TimeSpan.Zero;
        }

        private static string Path(Guid hiveId)
        {
            return "/game/v1/hives/" + hiveId.ToString("D") + "/research";
        }

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required.");
        }

        private static void RequireResearch(string researchId)
        {
            if (string.IsNullOrWhiteSpace(researchId) || researchId.Trim() != researchId ||
                !SupportedResearch.Contains(researchId))
                throw InvalidRequest("The research is unsupported.");
        }

        private static void RequireRevision(long value)
        {
            if (value < 0 || value == long.MaxValue)
                throw InvalidRequest("The expected research revision is outside the supported range.");
        }

        private static void RequireIdempotencyKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() != value || value.Length > 256)
                throw InvalidRequest("The idempotency key must contain between one and 256 trimmed characters.");
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
