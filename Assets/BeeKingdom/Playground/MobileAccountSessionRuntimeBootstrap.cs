using System;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Gameplay.Communication;
using BeeKingdom.Networking;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public static class MobileAccountSessionRuntimeBootstrap
    {
        private const string ConfigurationResource = "BeeKingdom/MobileAccountSessionRuntime";
        private const string InstallationKey = "BeeKingdom.MobileAccount.InstallationId.v1";

        private static MobileAccountSessionClient client;
        private static MobileAccountSessionRuntimeConfiguration activeConfiguration;
        private static HivePerimeterSortiePanelController gameplayController;
        private static HiveOfflineProductionPanelController offlineProductionController;
        private static HiveBuildingUpgradePanelController buildingUpgradeController;
        private static HiveResearchPanelController researchController;
        private static HiveStockPanelController stockController;
        private static HiveDailyRoundPanelController dailyRoundController;
        private static HiveBroodVitalityPanelController broodVitalityController;
        private static HiveDoctrineRecruitmentPanelController
            doctrineRecruitmentController;
        private static HiveSquadReservationPanelController
            squadReservationController;
        private static CombatPatrolPanelController combatPatrolController;
        private static StrategicPathPanelController strategicPathController;
        private static WorldResourceCollectionPanelController worldResourceCollectionController;
        private static WorldPresencePanelController worldPresenceController;
        private static BestiaryCodexPanelController bestiaryCodexController;
        private static HiveMilestoneEventPanelController milestoneEventController;
        private static ProtectedGameMutationOutbox gameplayMutationOutbox;
        private static Guid gameplayPlayerId;
        private static Guid gameplayHiveId;
        private static IHiveChampionBeeClient championBeeClient;
        private static IHiveTroopTierClient troopTierClient;
        private static IHiveVipClient vipClient;
        private static IHiveSpeedUpClient speedUpClient;
        private static IHiveRewardLedgerClient rewardLedgerClient;
        private static HiveSpeedUpPanelController speedUpController;
        private static HiveRewardLedgerPanelController rewardLedgerController;
        private static bool championBeeAndTroopTierGameplayReady;

        public static IHiveChampionBeeClient ChampionBeeClient => championBeeAndTroopTierGameplayReady ? championBeeClient : null;
        public static IHiveTroopTierClient TroopTierClient => championBeeAndTroopTierGameplayReady ? troopTierClient : null;
        public static IHiveVipClient VipClient => championBeeAndTroopTierGameplayReady ? vipClient : null;
        public static IHiveSpeedUpClient SpeedUpClient => championBeeAndTroopTierGameplayReady ? speedUpClient : null;
        public static IHiveRewardLedgerClient RewardLedgerClient => championBeeAndTroopTierGameplayReady ? rewardLedgerClient : null;
        public static Guid GameplayHiveId => gameplayHiveId;
        private static readonly LivingHiveChatSessionCoordinator chatCoordinator = new LivingHiveChatSessionCoordinator();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static async void ConfigureBeforeSceneLoad()
        {
            MobileAccountSessionRuntimeConfiguration configuration =
                Resources.Load<MobileAccountSessionRuntimeConfiguration>(ConfigurationResource);
            if (configuration == null || !configuration.OfficialAccountsEnabled ||
                string.IsNullOrWhiteSpace(configuration.BaseUrl))
                return;
            activeConfiguration = configuration;

            try
            {
                var protectedStore = CreateRefreshTokenStore();
                var transport = new UnityMobileAccountSessionRestTransport(
                    configuration.BaseUrl,
                    configuration.TimeoutSeconds,
                    configuration.AllowInsecureLoopbackForDevelopment);
                client = new MobileAccountSessionClient(
                    HiveViewProductUiPresenter.AccountSessionGateForRuntime(),
                    transport,
                    protectedStore);
                HiveViewProductUiPresenter.ConfigureMobileAccountSessionForRuntime(
                    client,
                    (email, password) => new MobileAccountLoginRequest(
                        email,
                        password,
                        Application.version,
                        GetOrCreateOpaqueInstallationId(),
                        configuration.Region),
                    (authorizationCode, codeVerifier, redirectUri) => new GoogleLoginRequest(
                        authorizationCode,
                        codeVerifier,
                        redirectUri,
                        Application.version,
                        GetOrCreateOpaqueInstallationId(),
                        configuration.Region),
                    configuration.GoogleOAuthClientId);

                AccountSessionReadinessSnapshot readiness = await client.InitializeAsync();
                if (readiness.ServerAllowsLogin)
                {
                    try
                    {
                        await client.RestoreOrRefreshAsync();
                    }
                    catch (MobileAccountSessionException exception)
                    {
                        Debug.LogWarning("Bee Kingdom official session restore remains closed: " + exception.SafeCode);
                    }
                }
                TryConfigureGameplayForActiveSession();
                HiveViewProductUiPresenter.RefreshMobileAccountSessionMessageForRuntime();
            }
            catch (MobileAccountSessionException exception)
            {
                Debug.LogWarning("Bee Kingdom official account session remains closed: " + exception.SafeCode);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Bee Kingdom official account session remains closed: " + exception.GetType().Name);
            }
        }

        public static string[] ProofRows()
        {
            return new[]
            {
                "mobile_auth_bootstrap:runtime_resource_opt_in",
                "mobile_auth_default_without_resource:not_configured",
                "mobile_auth_installation_id:random_app_scoped",
                "mobile_auth_hardware_identifier_used:false",
                "mobile_auth_installation_id_authoritative:false",
                "mobile_auth_auto_restore:only_after_server_readiness",
                "mobile_gameplay_online_requires_server_authority:true",
                "mobile_gameplay_offline_requires_protected_known_player:true",
                "mobile_building_upgrade_controller:session_scoped",
                "mobile_building_upgrade_offline:read_only",
                "mobile_research_controller:session_scoped",
                "mobile_research_offline:read_only",
                "mobile_hive_stock_controller:session_scoped",
                "mobile_hive_stock_offline:read_only",
                "mobile_daily_round_controller:session_scoped",
                "mobile_daily_round_offline:read_only",
                "mobile_daily_round_claim_outbox:android_keystore_protected",
                "mobile_daily_round_claim_auto_submit:false",
                "mobile_brood_vitality_controller:session_scoped",
                "mobile_brood_vitality_offline:read_only",
                "mobile_brood_care_outbox:android_keystore_protected",
                "mobile_brood_care_auto_submit:false",
                "mobile_doctrine_recruitment_controller:session_scoped",
                "mobile_doctrine_recruitment_offline:read_only",
                "mobile_doctrine_recruitment_outbox:android_keystore_protected",
                "mobile_doctrine_recruitment_auto_submit:false",
                "mobile_squad_reservation_controller:session_scoped",
                "mobile_squad_reservation_offline:read_only",
                "mobile_squad_reservation_outbox:android_keystore_protected",
                "mobile_squad_reservation_auto_submit:false",
                "mobile_gameplay_outbox_explicit_logout_purge:true"
            };
        }

        public static bool TryConfigureGameplayForActiveSession()
        {
            CloseGameplayForSignedOutSession();
            MobileAccountSessionRuntimeConfiguration configuration = activeConfiguration;
            Guid hiveId;
            if (client == null || configuration == null || !configuration.OfficialGameplayEnabled ||
                !Guid.TryParseExact(configuration.OfficialHiveId, "D", out hiveId) || hiveId == Guid.Empty)
                return false;

            var protectedCacheStore = CreateGameReadCacheStore();
            Guid knownPlayerId;
            bool authenticatedAuthority = client.State == MobileAccountSessionState.Authenticated &&
                client.ServerGameplayAuthorityGranted;
            bool protectedOfflineIdentity = protectedCacheStore.IsProtectionAvailable &&
                client.TryGetKnownPlayerId(out knownPlayerId) && knownPlayerId != Guid.Empty;
            if (!authenticatedAuthority && !protectedOfflineIdentity) return false;
            if (!client.TryGetKnownPlayerId(out gameplayPlayerId) ||
                gameplayPlayerId == Guid.Empty)
                return false;

            if (authenticatedAuthority) ActivateChatForActiveSession(configuration);

            var codec = new SystemTextGameJsonCodec();
            var gameTransport = new UnityAuthenticatedGameRestTransport(
                configuration.BaseUrl,
                codec,
                configuration.TimeoutSeconds,
                configuration.AllowInsecureLoopbackForDevelopment,
                HandleGameplayNetworkSignal);
            var cache = new ProtectedGameReadCache(
                protectedCacheStore,
                codec);
            var sortieClient = new HivePerimeterSortieClient(
                client.Gate,
                client,
                gameTransport,
                cache);
            var combatPatrolClient = new CombatPatrolClient(
                client.Gate,
                client,
                gameTransport);
            combatPatrolController = new CombatPatrolPanelController(combatPatrolClient, hiveId);
            var strategicPathClient = new StrategicPathClient(
                client.Gate,
                client,
                gameTransport);
            strategicPathController = new StrategicPathPanelController(strategicPathClient, hiveId);
            var worldResourceCollectionClient = new WorldResourceCollectionClient(
                client.Gate,
                client,
                gameTransport);
            worldResourceCollectionController = new WorldResourceCollectionPanelController(worldResourceCollectionClient, hiveId);
            var worldPresenceClient = new WorldPresenceClient(
                client.Gate,
                client,
                gameTransport);
            worldPresenceController = new WorldPresencePanelController(worldPresenceClient, hiveId);
            var bestiaryCodexClient = new BestiaryCodexClient(
                client.Gate,
                client,
                gameTransport);
            bestiaryCodexController = new BestiaryCodexPanelController(bestiaryCodexClient, hiveId);
            var milestoneEventClient = new HiveMilestoneEventClient(
                client.Gate,
                client,
                gameTransport);
            milestoneEventController = new HiveMilestoneEventPanelController(milestoneEventClient, hiveId);
            var productionClient = new HiveOfflineProductionClient(
                client.Gate,
                client,
                gameTransport,
                cache);
            offlineProductionController = new HiveOfflineProductionPanelController(productionClient, hiveId);
            var buildingUpgradeClient = new HiveBuildingUpgradeClient(
                client.Gate,
                client,
                gameTransport,
                cache);
            buildingUpgradeController = new HiveBuildingUpgradePanelController(buildingUpgradeClient, hiveId);
            var researchClient = new HiveResearchClient(
                client.Gate,
                client,
                gameTransport,
                cache);
            researchController = new HiveResearchPanelController(researchClient, hiveId);
            gameplayHiveId = hiveId;
            championBeeClient = new HiveChampionBeeClient(client.Gate, client, gameTransport);
            troopTierClient = new HiveTroopTierClient(client.Gate, client, gameTransport);
            vipClient = new HiveVipClient(client.Gate, client, gameTransport);
            speedUpClient = new HiveSpeedUpClient(client.Gate, client, gameTransport);
            rewardLedgerClient = new HiveRewardLedgerClient(client.Gate, client, gameTransport);
            speedUpController = new HiveSpeedUpPanelController(speedUpClient, hiveId);
            rewardLedgerController = new HiveRewardLedgerPanelController(rewardLedgerClient, hiveId);
            championBeeAndTroopTierGameplayReady = authenticatedAuthority;
            var stockClient = new HiveStockSnapshotClient(
                client.Gate,
                client,
                gameTransport,
                cache);
            stockController = new HiveStockPanelController(stockClient, hiveId);
            gameplayMutationOutbox = new ProtectedGameMutationOutbox(
                CreateGameMutationOutboxStore(),
                codec);
            gameplayController =
                new HivePerimeterSortiePanelController(
                    sortieClient,
                    hiveId,
                    gameplayMutationOutbox);
            squadReservationController =
                new HiveSquadReservationPanelController(
                    sortieClient,
                    hiveId,
                    gameplayMutationOutbox);
            var dailyRoundClient = new HiveDailyRoundClient(
                client.Gate,
                client,
                gameTransport,
                cache);
            dailyRoundController = new HiveDailyRoundPanelController(
                dailyRoundClient,
                hiveId,
                gameplayMutationOutbox);
            var broodVitalityClient = new HiveBroodVitalityClient(
                client.Gate,
                client,
                gameTransport,
                cache);
            broodVitalityController = new HiveBroodVitalityPanelController(
                broodVitalityClient,
                hiveId,
                gameplayMutationOutbox);
            var doctrineRecruitmentClient =
                new HiveDoctrineRecruitmentClient(
                    client.Gate,
                    client,
                    gameTransport,
                    cache);
            doctrineRecruitmentController =
                new HiveDoctrineRecruitmentPanelController(
                    doctrineRecruitmentClient,
                    hiveId,
                    gameplayMutationOutbox);
            HiveViewProductUiPresenter.ConfigurePerimeterSortieControllerForRuntime(gameplayController);
            HiveViewProductUiPresenter.ConfigureCombatPatrolControllerForRuntime(combatPatrolController);
            HiveViewProductUiPresenter.ConfigureStrategicPathControllerForRuntime(strategicPathController);
            HiveViewProductUiPresenter.ConfigureWorldResourceCollectionControllerForRuntime(worldResourceCollectionController);
            HiveViewProductUiPresenter.ConfigureWorldPresenceControllerForRuntime(worldPresenceController);
            HiveViewProductUiPresenter.ConfigureBestiaryCodexControllerForRuntime(bestiaryCodexController);
            HiveViewProductUiPresenter.ConfigureMilestoneEventControllerForRuntime(milestoneEventController);
            HiveViewProductUiPresenter.ConfigureOfflineProductionControllerForRuntime(offlineProductionController);
            HiveViewProductUiPresenter.ConfigureBuildingUpgradeControllerForRuntime(buildingUpgradeController);
            HiveViewProductUiPresenter.ConfigureResearchControllerForRuntime(researchController);
            HiveViewProductUiPresenter.ConfigureHiveStockControllerForRuntime(stockController);
            HiveViewProductUiPresenter.ConfigureDailyRoundControllerForRuntime(dailyRoundController);
            HiveViewProductUiPresenter.ConfigureBroodVitalityControllerForRuntime(
                broodVitalityController);
            HiveViewProductUiPresenter
                .ConfigureDoctrineRecruitmentControllerForRuntime(
                    doctrineRecruitmentController);
            HiveViewProductUiPresenter
                .ConfigureSquadReservationControllerForRuntime(
                    squadReservationController);
            HiveViewProductUiPresenter.ConfigureSpeedUpControllerForRuntime(speedUpController);
            HiveViewProductUiPresenter.ConfigureRewardLedgerControllerForRuntime(rewardLedgerController);
            combatPatrolController.Refresh();
            strategicPathController.Refresh();
            worldResourceCollectionController.Refresh();
            worldPresenceController.Refresh();
            bestiaryCodexController.Refresh();
            milestoneEventController.Refresh();
            speedUpController.Refresh();
            rewardLedgerController.Refresh();
            gameplayController.Refresh();
            squadReservationController.Refresh();
            EnsureHiveThenRefreshGameplayState(vipClient, hiveId);
            return true;
        }

        private static void HandleGameplayNetworkSignal(bool reachable)
        {
            if (client == null || reachable) return;
            client.MarkNetworkUnavailable();
        }

        // Un joueur reel de premiere connexion n'a encore aucun etat de ruche cote serveur -
        // les endpoints de lecture (VIP, abeilles championnes, production hors ligne, etc.) ne
        // le creent jamais eux-memes (game.hive_not_found tant que rien n'existe). On materialise
        // l'etat une fois via /ensure avant de lancer les rafraichissements habituels.
        private static async void EnsureHiveThenRefreshGameplayState(IHiveVipClient vip, Guid hiveId)
        {
            try
            {
                await vip.EnsureHiveAsync(hiveId);
            }
            catch (Exception)
            {
            }

            offlineProductionController.Refresh();
            buildingUpgradeController.Refresh();
            researchController.Refresh();
            stockController.Refresh();
            dailyRoundController.Refresh();
            broodVitalityController.Refresh();
            doctrineRecruitmentController.Refresh();
        }

        public sealed class DisplayNameSubmitResult
        {
            public bool Succeeded;
            public string ErrorCode;

            public static DisplayNameSubmitResult Success() => new DisplayNameSubmitResult { Succeeded = true };
            public static DisplayNameSubmitResult Failure(string code) => new DisplayNameSubmitResult { Succeeded = false, ErrorCode = code };
        }

        public static async Task<DisplayNameSubmitResult> SubmitDisplayNameAsync(string displayName, CancellationToken cancellationToken = default)
        {
            if (client == null || activeConfiguration == null || !client.TryGetSession(out GameAccountSession session))
                return DisplayNameSubmitResult.Failure("auth.session_required");

            using (UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest(
                activeConfiguration.BaseUrl.TrimEnd('/') + "/auth/display-name", "POST"))
            {
                string json = JsonUtility.ToJson(new DisplayNameRequestWire { displayName = displayName });
                request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + session.AccessToken);
                request.timeout = activeConfiguration.TimeoutSeconds;

                UnityEngine.Networking.UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                operation.completed += _ => completion.TrySetResult(true);
                using (cancellationToken.Register(() =>
                {
                    try { request.Abort(); } catch (Exception) { }
                }))
                {
                    await completion.Task;
                }
                cancellationToken.ThrowIfCancellationRequested();

                bool success = request.responseCode >= 200 && request.responseCode <= 299;
                if (success) return DisplayNameSubmitResult.Success();

                string body = request.downloadHandler == null ? string.Empty : request.downloadHandler.text;
                string code = "auth.display_name_rejected";
                try
                {
                    ErrorWire error = JsonUtility.FromJson<ErrorWire>(body);
                    if (error != null && !string.IsNullOrWhiteSpace(error.code)) code = error.code;
                }
                catch (Exception)
                {
                }

                return DisplayNameSubmitResult.Failure(code);
            }
        }

        [Serializable]
        private sealed class DisplayNameRequestWire
        {
            public string displayName;
        }

        [Serializable]
        private sealed class ErrorWire
        {
            public string code;
        }

        public static async Task PurgeGameplayOutboxForExplicitLogoutAsync()
        {
            ProtectedGameMutationOutbox outbox = gameplayMutationOutbox;
            Guid playerId = gameplayPlayerId;
            CloseGameplayForSignedOutSession();
            if (outbox == null ||
                !outbox.IsProtectionAvailable ||
                playerId == Guid.Empty)
                return;
            try
            {
                await outbox.DeletePlayerAsync(
                    playerId,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Bee Kingdom protected gameplay logout purge failed: " +
                    exception.GetType().Name);
            }
        }

        private static void ActivateChatForActiveSession(MobileAccountSessionRuntimeConfiguration configuration)
        {
            if (client == null || configuration == null || string.IsNullOrWhiteSpace(configuration.BaseUrl)) return;
            var options = new RemoteChatClientOptions
            {
                BaseUrl = configuration.BaseUrl,
                AllowInsecureLoopback = configuration.AllowInsecureLoopbackForDevelopment,
                StoragePartitionId = gameplayPlayerId.ToString("D")
            };
            var binding = new LivingHiveChatSessionBinding(
                options,
                new MobileAccountChatSessionSource(client),
                new PlayerPrefsChatStringStore(),
                new LivingHiveChatDataProtector(),
                new SignalRChatRealtimeTransport(configuration.BaseUrl));
            var readiness = new DelegateChatAccountSessionReadiness(() =>
                client.State == MobileAccountSessionState.Authenticated && client.ServerGameplayAuthorityGranted);
            ForgetChatLifecycle(chatCoordinator.SessionAvailableAsync(readiness, binding));
        }

        private static async void ForgetChatLifecycle(Task task)
        {
            try { await task; }
            catch (Exception exception) { Debug.LogWarning("Bee Kingdom chat session activation failed: " + exception.GetType().Name); }
        }

        public static void CloseGameplayForSignedOutSession()
        {
            ForgetChatLifecycle(chatCoordinator.SessionEndedAsync());
            HivePerimeterSortiePanelController previous = gameplayController;
            HiveOfflineProductionPanelController previousProduction = offlineProductionController;
            HiveBuildingUpgradePanelController previousBuildingUpgrade = buildingUpgradeController;
            HiveResearchPanelController previousResearch = researchController;
            HiveStockPanelController previousStock = stockController;
            HiveDailyRoundPanelController previousDailyRound = dailyRoundController;
            HiveBroodVitalityPanelController previousBroodVitality =
                broodVitalityController;
            HiveDoctrineRecruitmentPanelController
                previousDoctrineRecruitment =
                    doctrineRecruitmentController;
            HiveSquadReservationPanelController
                previousSquadReservation =
                    squadReservationController;
            CombatPatrolPanelController previousCombatPatrol = combatPatrolController;
            StrategicPathPanelController previousStrategicPath = strategicPathController;
            WorldResourceCollectionPanelController previousWorldResourceCollection = worldResourceCollectionController;
            WorldPresencePanelController previousWorldPresence = worldPresenceController;
            BestiaryCodexPanelController previousBestiaryCodex = bestiaryCodexController;
            HiveMilestoneEventPanelController previousMilestoneEvent = milestoneEventController;
            combatPatrolController = null;
            strategicPathController = null;
            worldResourceCollectionController = null;
            worldPresenceController = null;
            bestiaryCodexController = null;
            milestoneEventController = null;
            gameplayController = null;
            offlineProductionController = null;
            buildingUpgradeController = null;
            researchController = null;
            stockController = null;
            dailyRoundController = null;
            broodVitalityController = null;
            doctrineRecruitmentController = null;
            squadReservationController = null;
            gameplayMutationOutbox = null;
            gameplayPlayerId = Guid.Empty;
            gameplayHiveId = Guid.Empty;
            championBeeClient = null;
            troopTierClient = null;
            vipClient = null;
            speedUpClient = null;
            rewardLedgerClient = null;
            HiveSpeedUpPanelController previousSpeedUp = speedUpController;
            HiveRewardLedgerPanelController previousRewardLedger = rewardLedgerController;
            speedUpController = null;
            rewardLedgerController = null;
            championBeeAndTroopTierGameplayReady = false;
            if (previous != null) previous.Dispose();
            if (previousProduction != null) previousProduction.Dispose();
            if (previousBuildingUpgrade != null) previousBuildingUpgrade.Dispose();
            if (previousResearch != null) previousResearch.Dispose();
            if (previousStock != null) previousStock.Dispose();
            if (previousDailyRound != null) previousDailyRound.Dispose();
            if (previousBroodVitality != null) previousBroodVitality.Dispose();
            if (previousDoctrineRecruitment != null)
                previousDoctrineRecruitment.Dispose();
            if (previousSquadReservation != null)
                previousSquadReservation.Dispose();
            if (previousCombatPatrol != null)
                previousCombatPatrol.Dispose();
            if (previousStrategicPath != null)
                previousStrategicPath.Dispose();
            if (previousWorldResourceCollection != null)
                previousWorldResourceCollection.Dispose();
            if (previousWorldPresence != null)
                previousWorldPresence.Dispose();
            if (previousBestiaryCodex != null)
                previousBestiaryCodex.Dispose();
            if (previousMilestoneEvent != null)
                previousMilestoneEvent.Dispose();
            if (previousSpeedUp != null)
                previousSpeedUp.Dispose();
            if (previousRewardLedger != null)
                previousRewardLedger.Dispose();
            HiveViewProductUiPresenter.ResetPerimeterSortieControllerForRuntime();
            HiveViewProductUiPresenter.ResetOfflineProductionControllerForRuntime();
            HiveViewProductUiPresenter.ResetBuildingUpgradeControllerForRuntime();
            HiveViewProductUiPresenter.ResetResearchControllerForRuntime();
            HiveViewProductUiPresenter.ResetHiveStockControllerForRuntime();
            HiveViewProductUiPresenter.ResetDailyRoundControllerForRuntime();
            HiveViewProductUiPresenter.ResetBroodVitalityControllerForRuntime();
            HiveViewProductUiPresenter
                .ResetDoctrineRecruitmentControllerForRuntime();
            HiveViewProductUiPresenter
                .ResetSquadReservationControllerForRuntime();
            HiveViewProductUiPresenter.ResetCombatPatrolControllerForRuntime();
            HiveViewProductUiPresenter.ResetStrategicPathControllerForRuntime();
            HiveViewProductUiPresenter.ResetWorldResourceCollectionControllerForRuntime();
            HiveViewProductUiPresenter.ResetWorldPresenceControllerForRuntime();
            HiveViewProductUiPresenter.ResetBestiaryCodexControllerForRuntime();
            HiveViewProductUiPresenter.ResetMilestoneEventControllerForRuntime();
            HiveViewProductUiPresenter.ResetSpeedUpControllerForRuntime();
            HiveViewProductUiPresenter.ResetRewardLedgerControllerForRuntime();
        }

        private static IProtectedRefreshTokenStore CreateRefreshTokenStore()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return new IosKeychainRefreshTokenStore();
#elif UNITY_EDITOR
            // Ni le Keychain iOS ni le Keystore Android ne sont disponibles dans l'Editeur -
            // stockage de secours non protege pour pouvoir tester le flux de connexion officiel
            // sans builder sur un vrai appareil. Jamais utilise hors de l'Editeur.
            return new EditorFallbackRefreshTokenStore();
#elif UNITY_STANDALONE_WIN
            return new WindowsDpapiRefreshTokenStore();
#else
            return new AndroidKeystoreRefreshTokenStore();
#endif
        }

        private static IProtectedGameReadCacheStore CreateGameReadCacheStore()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return new IosKeychainGameReadCacheStore();
#else
            return new AndroidKeystoreGameReadCacheStore();
#endif
        }

        private static IProtectedGameMutationOutboxStore CreateGameMutationOutboxStore()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return new PlatformGameMutationOutboxStore();
#elif UNITY_IOS && !UNITY_EDITOR
            return new IosKeychainGameMutationOutboxStore();
#else
            return new AndroidKeystoreGameMutationOutboxStore();
#endif
        }

        private static string GetOrCreateOpaqueInstallationId()
        {
            string value = PlayerPrefs.GetString(InstallationKey, string.Empty);
            Guid parsed;
            if (Guid.TryParseExact(value, "N", out parsed) && parsed != Guid.Empty) return value;
            value = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(InstallationKey, value);
            PlayerPrefs.Save();
            return value;
        }
    }
}
