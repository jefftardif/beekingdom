using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public sealed class RemoteBuildingUpgradeBalance
    {
        public long Amount { get; set; }
        public long Capacity { get; set; }
    }

    public sealed class RemoteBuildingUpgradeOffer
    {
        public string BuildingKey { get; set; }
        public int FromLevel { get; set; }
        public int ToLevel { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, long> Costs { get; set; }
    }

    public sealed class RemoteBuildingUpgradeOperation
    {
        public Guid OperationId { get; set; }
        public string BuildingKey { get; set; }
        public int FromLevel { get; set; }
        public int ToLevel { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset CompletesAtUtc { get; set; }
        public string Status { get; set; }
    }

    // M055-CL : progression du Palais Royal (administration_core). Champs ADDITIFS -
    // un serveur sans section RoyalPalaceProgression renvoie null et tout le reste du
    // contrat living-hive-building-upgrade-v1 continue de fonctionner a l'identique.
    public sealed class RemoteRoyalPalaceRequirement
    {
        public string BuildingKey { get; set; }
        public int MinimumLevel { get; set; }
        public int CurrentLevel { get; set; }
        public bool IsSatisfied { get; set; }
    }

    public sealed class RemoteRoyalPalaceUnlock
    {
        public string Key { get; set; }
        public string Description { get; set; }
        public bool Enforced { get; set; }
    }

    public sealed class RemoteRoyalPalaceProgression
    {
        public string BuildingKey { get; set; }
        public string DefinitionVersion { get; set; }
        public bool IsAlphaBalance { get; set; }
        public int CurrentLevel { get; set; }
        public int? NextLevel { get; set; }
        public int MaxConfiguredLevel { get; set; }
        public bool IsMaxConfiguredLevel { get; set; }
        public bool RequirementsSatisfied { get; set; }
        public string BlockedReasonCode { get; set; }
        public string BlockingBuildingKey { get; set; }
        public int BlockingBuildingMinimumLevel { get; set; }
        public string NextLevelDescription { get; set; }
        public List<RemoteRoyalPalaceRequirement> NextLevelRequirements { get; set; }
        public List<RemoteRoyalPalaceUnlock> NextLevelUnlocks { get; set; }
        public List<RemoteRoyalPalaceUnlock> UnlockedSoFar { get; set; }
    }

    public sealed class RemoteBuildingUpgradeSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public string CatalogVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public Dictionary<string, RemoteBuildingUpgradeBalance> Balances { get; set; }
        public Dictionary<string, int> BuildingLevels { get; set; }
        public List<RemoteBuildingUpgradeOffer> Offers { get; set; }
        public RemoteBuildingUpgradeOperation ActiveOperation { get; set; }
        public RemoteRoyalPalaceProgression RoyalPalace { get; set; }
    }

    public sealed class BuildingUpgradeMutationRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteBuildingUpgradeReceipt
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string IdempotencyKey { get; set; }
        public Guid OperationId { get; set; }
        public string BuildingKey { get; set; }
        public int FromLevel { get; set; }
        public int ToLevel { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public string Code { get; set; }
    }

    public sealed class RemoteBuildingUpgradeMutationResponse
    {
        public RemoteBuildingUpgradeReceipt Receipt { get; set; }
        public RemoteBuildingUpgradeSnapshot Snapshot { get; set; }
    }

    public interface IHiveBuildingUpgradeClient
    {
        GameReadSource LastReadSource { get; }
        DateTimeOffset LastReadCachedAtUtc { get; }
        Task<RemoteBuildingUpgradeSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteBuildingUpgradeMutationResponse> StartAsync(
            Guid hiveId,
            string buildingKey,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteBuildingUpgradeMutationResponse> CompleteAsync(
            Guid hiveId,
            Guid operationId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public sealed class HiveBuildingUpgradeClient : IHiveBuildingUpgradeClient
    {
        public const string ContractVersion = "living-hive-building-upgrade-v1";
        public const string RunningStatus = "running";
        public const string AwaitingCompletionStatus = "awaiting_completion";
        // M055-CL : identifiant interne reel du Palais Royal (Coeur royal). Cote Unity il est
        // expose sous BuildingTypes.RoyalPalace ("ROYAL_PALACE") et traduit vers cette cle par
        // BuildingMappingTable - c'est CETTE cle qui existe cote serveur et en persistance.
        public const string RoyalPalaceBuildingKey = "administration_core";
        public const string PrerequisitesNotMetCode = "game.royal_palace_prerequisites";
        public const string StartedCode = "game.building_upgrade_started";
        public const string CompletedCode = "game.building_upgrade_completed";
        public static readonly TimeSpan MaximumDuration = TimeSpan.FromDays(7);

        // M039-CL: doit rester le miroir exact du catalogue serveur (BuildingUpgradeOptions.Catalog,
        // voir appsettings.*.json) - un batiment present cote serveur mais absent ici fait echouer
        // ValidateSnapshot pour TOUT compte dont ce batiment est encore dans la plage du catalogue
        // (ex.: guard_post niveau 1 pour un compte neuf), meme si le batiment cible n'est pas celui
        // que le joueur tente d'ameliorer - le snapshot entier est rejete des qu'une offre est invalide.
        private static readonly HashSet<string> SupportedBuildings =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "honey_storage", "wax_workshop", "warehouse_cells", "nursery_cluster",
                "guard_post", "defense_growth", "genetics_garden", "research_node",
                "infirmary_grove", "academy_canopy", "hive_bank", "administration_core",
                "alliance_future_hall", "archives_honeyfall"
            };
        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;
        private readonly ProtectedGameReadCache readCache;

        public HiveBuildingUpgradeClient(
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
                "mobile_building_upgrade_contract:" + ContractVersion,
                "building_upgrade_catalog_authority:server",
                "building_upgrade_cost_authority:server",
                "building_upgrade_time_authority:server",
                "building_upgrade_level_authority:server",
                "building_upgrade_get_protected_cache:" + (readCache != null && readCache.IsProtectionAvailable).ToString().ToLowerInvariant(),
                "building_upgrade_cache_read_only:true",
                "building_upgrade_validated_mutation_refreshes_cache:true",
                "building_upgrade_mutation_offline_retry:false",
                "building_upgrade_local_debit:false",
                "building_upgrade_local_completion:false",
                "building_upgrade_read_source:" + LastReadSource.ToString().ToLowerInvariant()
            };
        }

        public async Task<RemoteBuildingUpgradeSnapshot> ReadAsync(
            Guid hiveId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            string path = Path(hiveId);
            try
            {
                SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
                var request = new AuthenticatedGameRestRequest("GET", path);
                RemoteBuildingUpgradeSnapshot snapshot =
                    await SendWithSingleAuthenticationRefreshAsync<RemoteBuildingUpgradeSnapshot>(request, context, cancellationToken)
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
                RemoteBuildingUpgradeSnapshot cached = await TryLoadCacheAsync(hiveId, path, cancellationToken)
                    .ConfigureAwait(false);
                if (cached != null) return cached;
                throw;
            }
        }

        public async Task<RemoteBuildingUpgradeMutationResponse> StartAsync(
            Guid hiveId,
            string buildingKey,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            RequireBuilding(buildingKey);
            RequireRevision(expectedRevision);
            RequireIdempotencyKey(idempotencyKey);
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                Path(hiveId) + "/" + buildingKey + "/start",
                new BuildingUpgradeMutationRequest
                {
                    ExpectedRevision = expectedRevision,
                    IdempotencyKey = idempotencyKey
                });
            RemoteBuildingUpgradeMutationResponse response =
                await SendWithSingleAuthenticationRefreshAsync<RemoteBuildingUpgradeMutationResponse>(request, context, cancellationToken)
                    .ConfigureAwait(false);
            ValidateMutationResponse(response, context.PlayerId, hiveId, buildingKey, Guid.Empty,
                expectedRevision, idempotencyKey, StartedCode, true);
            LastReadSource = GameReadSource.Server;
            LastReadCachedAtUtc = default(DateTimeOffset);
            await SaveValidatedReadBestEffortAsync(
                context.PlayerId, hiveId, Path(hiveId), response.Snapshot, CancellationToken.None).ConfigureAwait(false);
            return response;
        }

        public async Task<RemoteBuildingUpgradeMutationResponse> CompleteAsync(
            Guid hiveId,
            Guid operationId,
            long expectedRevision,
            string idempotencyKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireHive(hiveId);
            if (operationId == Guid.Empty) throw InvalidRequest("A building operation identifier is required.");
            RequireRevision(expectedRevision);
            RequireIdempotencyKey(idempotencyKey);
            SessionContext context = await RequireSessionAsync(cancellationToken).ConfigureAwait(false);
            var request = new AuthenticatedGameRestRequest(
                "POST",
                Path(hiveId) + "/" + operationId.ToString("D") + "/complete",
                new BuildingUpgradeMutationRequest
                {
                    ExpectedRevision = expectedRevision,
                    IdempotencyKey = idempotencyKey
                });
            RemoteBuildingUpgradeMutationResponse response =
                await SendWithSingleAuthenticationRefreshAsync<RemoteBuildingUpgradeMutationResponse>(request, context, cancellationToken)
                    .ConfigureAwait(false);
            ValidateMutationResponse(response, context.PlayerId, hiveId, string.Empty, operationId,
                expectedRevision, idempotencyKey, CompletedCode, false);
            LastReadSource = GameReadSource.Server;
            LastReadCachedAtUtc = default(DateTimeOffset);
            await SaveValidatedReadBestEffortAsync(
                context.PlayerId, hiveId, Path(hiveId), response.Snapshot, CancellationToken.None).ConfigureAwait(false);
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
            RemoteBuildingUpgradeSnapshot snapshot,
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

        private async Task<RemoteBuildingUpgradeSnapshot> TryLoadCacheAsync(
            Guid hiveId,
            string path,
            CancellationToken cancellationToken)
        {
            if (readCache == null || !readCache.IsProtectionAvailable) return null;
            Guid playerId;
            if (!TryGetKnownPlayerId(out playerId)) return null;
            ProtectedGameReadCacheHit<RemoteBuildingUpgradeSnapshot> hit =
                await readCache.TryLoadAsync<RemoteBuildingUpgradeSnapshot>(
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
            RemoteBuildingUpgradeMutationResponse response,
            Guid playerId,
            Guid hiveId,
            string expectedBuildingKey,
            Guid expectedOperationId,
            long expectedRevision,
            string idempotencyKey,
            string expectedCode,
            bool starting)
        {
            if (response == null || response.Receipt == null || response.Snapshot == null)
                throw InvalidResponse("The building upgrade response is incomplete.");
            ValidateSnapshot(response.Snapshot, playerId, hiveId);
            RemoteBuildingUpgradeReceipt receipt = response.Receipt;
            long nextRevision;
            try { nextRevision = checked(expectedRevision + 1L); }
            catch (OverflowException) { throw InvalidResponse("The building upgrade revision overflowed."); }

            if (receipt.PlayerId != playerId || receipt.HiveId != hiveId || receipt.OperationId == Guid.Empty ||
                !SupportedBuildings.Contains(receipt.BuildingKey) ||
                (!string.IsNullOrEmpty(expectedBuildingKey) && receipt.BuildingKey != expectedBuildingKey) ||
                (expectedOperationId != Guid.Empty && receipt.OperationId != expectedOperationId) ||
                receipt.FromLevel < 1 || receipt.ToLevel != receipt.FromLevel + 1 ||
                receipt.Revision != nextRevision || response.Snapshot.Revision < receipt.Revision ||
                !IsUtc(receipt.AcceptedAtUtc) || receipt.AcceptedAtUtc > response.Snapshot.ServerTimeUtc ||
                !string.Equals(receipt.IdempotencyKey, idempotencyKey, StringComparison.Ordinal) ||
                !string.Equals(receipt.Code, expectedCode, StringComparison.Ordinal))
                throw InvalidResponse("The building upgrade receipt is inconsistent.");

            int currentLevel;
            if (!response.Snapshot.BuildingLevels.TryGetValue(receipt.BuildingKey, out currentLevel))
                throw InvalidResponse("The upgraded building is absent from the snapshot.");

            if (starting)
            {
                RemoteBuildingUpgradeOperation active = response.Snapshot.ActiveOperation;
                bool activeMatches = active != null && active.OperationId == receipt.OperationId &&
                    active.BuildingKey == receipt.BuildingKey && active.FromLevel == receipt.FromLevel &&
                    active.ToLevel == receipt.ToLevel && currentLevel == receipt.FromLevel;
                bool completedBeforeReplay = currentLevel >= receipt.ToLevel &&
                    (active == null || active.OperationId != receipt.OperationId);
                if (!activeMatches && !completedBeforeReplay)
                    throw InvalidResponse("The started operation is not bound to the snapshot.");
            }
            else if (currentLevel < receipt.ToLevel ||
                (response.Snapshot.ActiveOperation != null &&
                 response.Snapshot.ActiveOperation.OperationId == receipt.OperationId))
            {
                throw InvalidResponse("The completed level is not authoritative in the snapshot.");
            }
        }

        private static void ValidateSnapshot(RemoteBuildingUpgradeSnapshot snapshot, Guid playerId, Guid hiveId)
        {
            if (snapshot == null || snapshot.PlayerId != playerId || snapshot.HiveId != hiveId)
                throw InvalidResponse("The building upgrade snapshot belongs to another account or hive.");
            if (!string.Equals(snapshot.ContractVersion, ContractVersion, StringComparison.Ordinal) ||
                !IsSafeToken(snapshot.CatalogVersion) || snapshot.Revision < 0 || !IsUtc(snapshot.ServerTimeUtc))
                throw InvalidResponse("The building upgrade contract, revision, catalog, or server time is invalid.");
            if (snapshot.Balances == null || snapshot.BuildingLevels == null || snapshot.Offers == null ||
                snapshot.Balances.Count > 16 || snapshot.BuildingLevels.Count > 64 || snapshot.Offers.Count > 16)
                throw InvalidResponse("The building upgrade snapshot is incomplete or unbounded.");

            foreach (KeyValuePair<string, RemoteBuildingUpgradeBalance> entry in snapshot.Balances)
            {
                RemoteBuildingUpgradeBalance balance = entry.Value;
                if (!IsSafeToken(entry.Key) || balance == null || balance.Amount < 0 ||
                    balance.Capacity < 0 || balance.Amount > balance.Capacity)
                    throw InvalidResponse("A building upgrade resource balance is invalid.");
            }
            foreach (KeyValuePair<string, int> entry in snapshot.BuildingLevels)
            {
                if (!IsSafeToken(entry.Key) || entry.Value < 1 || entry.Value > 1000)
                    throw InvalidResponse("A building level is invalid.");
            }
            if (snapshot.Offers.Select(offer => offer == null ? string.Empty : offer.BuildingKey)
                .Distinct(StringComparer.Ordinal).Count() != snapshot.Offers.Count)
                throw InvalidResponse("A building upgrade offer is duplicated.");
            foreach (RemoteBuildingUpgradeOffer offer in snapshot.Offers)
            {
                if (offer == null || !SupportedBuildings.Contains(offer.BuildingKey) ||
                    offer.FromLevel < 1 || offer.ToLevel != offer.FromLevel + 1 ||
                    offer.Duration <= TimeSpan.Zero || offer.Duration > MaximumDuration ||
                    offer.Costs == null || offer.Costs.Count == 0 || offer.Costs.Count > 8 ||
                    !snapshot.BuildingLevels.TryGetValue(offer.BuildingKey, out int level) || level != offer.FromLevel)
                    throw InvalidResponse("A building upgrade offer is invalid.");
                foreach (KeyValuePair<string, long> cost in offer.Costs)
                {
                    if (!IsSafeToken(cost.Key) || cost.Value <= 0 || cost.Value > 1000000000L ||
                        !snapshot.Balances.ContainsKey(cost.Key))
                        throw InvalidResponse("A building upgrade cost is invalid.");
                }
            }

            ValidateRoyalPalace(snapshot);

            RemoteBuildingUpgradeOperation operation = snapshot.ActiveOperation;
            if (operation == null) return;
            int current;
            if (operation.OperationId == Guid.Empty || !SupportedBuildings.Contains(operation.BuildingKey) ||
                operation.FromLevel < 1 || operation.ToLevel != operation.FromLevel + 1 ||
                !IsUtc(operation.StartedAtUtc) || !IsUtc(operation.CompletesAtUtc) ||
                operation.CompletesAtUtc <= operation.StartedAtUtc ||
                operation.CompletesAtUtc - operation.StartedAtUtc > MaximumDuration ||
                !snapshot.BuildingLevels.TryGetValue(operation.BuildingKey, out current) || current != operation.FromLevel ||
                (operation.Status != RunningStatus && operation.Status != AwaitingCompletionStatus) ||
                (operation.Status == RunningStatus && operation.CompletesAtUtc <= snapshot.ServerTimeUtc) ||
                (operation.Status == AwaitingCompletionStatus && operation.CompletesAtUtc > snapshot.ServerTimeUtc))
                throw InvalidResponse("The active building upgrade operation is inconsistent.");
        }

        // M055-CL. Deux principes :
        //  1. ABSENCE TOLEREE - `RoyalPalace` null est un etat valide (serveur sans section
        //     de progression configuree, ou snapshot mis en cache avant M055). On ne casse
        //     jamais une lecture par ailleurs valide juste parce que le bloc manque.
        //  2. PRESENCE BORNEE - s'il est la, il doit etre coherent et borne, comme le reste
        //     du snapshot : rien d'illimite, rien de contradictoire avec BuildingLevels.
        private static void ValidateRoyalPalace(RemoteBuildingUpgradeSnapshot snapshot)
        {
            RemoteRoyalPalaceProgression royal = snapshot.RoyalPalace;
            if (royal == null) return;

            if (!string.Equals(royal.BuildingKey, RoyalPalaceBuildingKey, StringComparison.Ordinal) ||
                !IsSafeToken(royal.DefinitionVersion) ||
                royal.CurrentLevel < 1 || royal.CurrentLevel > 1000 ||
                royal.MaxConfiguredLevel < 0 || royal.MaxConfiguredLevel > 1000 ||
                (royal.NextLevel.HasValue && royal.NextLevel.Value != royal.CurrentLevel + 1) ||
                (royal.IsMaxConfiguredLevel && royal.NextLevel.HasValue) ||
                royal.BlockingBuildingMinimumLevel < 0 || royal.BlockingBuildingMinimumLevel > 1000)
                throw InvalidResponse("The royal palace progression is inconsistent.");

            // Le niveau annonce doit etre le MEME que celui du batiment dans le snapshot :
            // c'est la garantie qu'il n'existe pas un second compteur cote serveur.
            int level;
            if (snapshot.BuildingLevels.TryGetValue(RoyalPalaceBuildingKey, out level) && level != royal.CurrentLevel)
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.InvalidResponse,
                    "The royal palace level disagrees with the building level.");

            ValidateRoyalPalaceRequirements(royal.NextLevelRequirements);
            ValidateRoyalPalaceUnlocks(royal.NextLevelUnlocks);
            ValidateRoyalPalaceUnlocks(royal.UnlockedSoFar);
        }

        private static void ValidateRoyalPalaceRequirements(List<RemoteRoyalPalaceRequirement> requirements)
        {
            if (requirements == null) return;
            if (requirements.Count > 16) throw InvalidResponse("The royal palace requirements are unbounded.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (RemoteRoyalPalaceRequirement requirement in requirements)
            {
                if (requirement == null || !IsSafeToken(requirement.BuildingKey) ||
                    requirement.MinimumLevel < 1 || requirement.MinimumLevel > 1000 ||
                    requirement.CurrentLevel < 0 || requirement.CurrentLevel > 1000 ||
                    requirement.IsSatisfied != requirement.CurrentLevel >= requirement.MinimumLevel ||
                    !seen.Add(requirement.BuildingKey))
                    throw InvalidResponse("A royal palace requirement is invalid.");
            }
        }

        private static void ValidateRoyalPalaceUnlocks(List<RemoteRoyalPalaceUnlock> unlocks)
        {
            if (unlocks == null) return;
            if (unlocks.Count > 64) throw InvalidResponse("The royal palace unlocks are unbounded.");
            foreach (RemoteRoyalPalaceUnlock unlock in unlocks)
            {
                if (unlock == null || !IsSafeToken(unlock.Key) ||
                    string.IsNullOrWhiteSpace(unlock.Description) || unlock.Description.Length > 200)
                    throw InvalidResponse("A royal palace unlock is invalid.");
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
            return "/game/v1/hives/" + hiveId.ToString("D") + "/building-upgrades";
        }

        private static void RequireHive(Guid hiveId)
        {
            if (hiveId == Guid.Empty) throw InvalidRequest("A hive identifier is required.");
        }

        private static void RequireBuilding(string buildingKey)
        {
            if (string.IsNullOrWhiteSpace(buildingKey) || buildingKey.Trim() != buildingKey ||
                !SupportedBuildings.Contains(buildingKey))
                throw InvalidRequest("The building is unsupported.");
        }

        private static void RequireRevision(long value)
        {
            if (value < 0 || value == long.MaxValue)
                throw InvalidRequest("The expected building upgrade revision is outside the supported range.");
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
