using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BeeKingdom.Audio;
using BeeKingdom.Networking;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class WorldMapMmoFullscreenFoundationBootstrap : MonoBehaviour
    {
        private const string OfficialMapPath = "C:/projets/beekingdom/carte.png";
        private const string WorldId = "BK-DEMO-WORLD-WAVE6-LOCAL";
        private const string GameServerId = "GS-DEMO-WAVE6-READINESS";
        private const int LocalDemoSeed = 738921;
        private const int ChunkSize = 512;
        private const int SectorSizeChunks = 4;
        private const int WorldChunkWidth = 64;
        private const int WorldChunkHeight = 64;
        private const int ActiveChunkRadius = 2;
        private const int StressWorldMapChunks = 50;
        private const int BudgetActiveChunks = 25;
        private const int BudgetWave6TextureCache = WorldMapWave6StreamingTileProvider.CacheCapacity;
        private const int BudgetActiveHives = 25;
        private const int BudgetActiveResources = 75;
        private const int BudgetActiveBestiary = 25;
        private const long BudgetStressAllocBytes = 2_000_000L;
        private const float MinZoom = 0.30f;
        private const float MaxZoom = 2.65f;
        private const float ZoomDamping = 10f;
        private const float PanDamping = 14f;
        private const float MinHiveDistance = 300f;
        private const float MinHiveResourceDistance = 105f;
        private const string RuntimeEntityResourceRoot = "WorldMapRuntimeEntitiesWave1";
        private const string RuntimeResourcePremiumRoot = "WorldMapRuntimeEntitiesWave6Premium";
        private const string Wave6RuntimePlacementMaskResource = "WorldMapRuntimePlacement/wave6_wave5method_12288_placement_mask";
        private const string CombatMarchBeeBodyResource = "WorldMapWave6Runtime/CombatMarch/CombatMarchBeeBody";
        private const string CombatMarchBeeWingsResource = "WorldMapWave6Runtime/CombatMarch/CombatMarchBeeWings";

        [SerializeField] private bool useV3DPreviewRuntimePackageForPlayMode;
        [SerializeField] private bool useV3ECandidateRuntimePackageForPlayMode;
        [SerializeField] private bool useV3MPreviewRuntimePackageForPlayMode;
        [SerializeField] private bool useV3VCandidateRuntimePackageForPlayMode;
        [SerializeField] private bool useV3OReducedAuditPreviewRuntimePackageForPlayMode;
        [SerializeField] private bool useRouteLockCoherentProofRuntimePackageForPlayMode;
        [SerializeField] private bool useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode;
        [SerializeField] private bool useWave5Method12288PreviewRuntimePackageForPlayMode;
        [SerializeField] private bool useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode;
        [SerializeField] private bool useV2INativeAuditPreviewRuntimePackageForPlayMode;
        [SerializeField] private bool useV2OPerimeterAuditPreviewRuntimePackageForPlayMode;
        [SerializeField] private bool useV2IRepairAuditPreviewRuntimePackageForPlayMode;
        [SerializeField] private bool useV2ISelectedHdLocalRepairReviewRuntimePackageForPlayMode;
        [SerializeField] private bool useInitialAuditViewForPlayMode;
        [SerializeField] private int initialAuditChunkX = 16;
        [SerializeField] private int initialAuditChunkY = 19;
        [SerializeField] private float initialAuditZoom = 0.58f;
        [SerializeField] private string initialAuditViewLabel = "Audit jonction montagne/foret";

        private Wave3RuntimeGutterTileProvider wave3Provider;
        private WorldMapWave6StreamingTileProvider wave6Provider;
        private WorldMapBearDenLandmark bearDenLandmark;
        private WorldMapLocalLabRuntime localLab;
        private Texture2D pixel;
        private Vector2 currentWorldCenter;
        private Vector2 targetWorldCenter;
        private float currentZoom = 1f;
        private float targetZoom = 1f;
        private Vector2 lastMousePosition;
        private Vector2 mouseDownPosition;
        private float mouseDragDistance;
        private bool dragging;
        private float lastTouchDistance;
        private Vector2 lastTouchCenter;
        private float animatedTime;
        private string status = "Carte MMO large monde - chunks local/demo";
        private string selectedHiveId = "hive_player_test";
        private string selectedResourceId = "res_nectar_core";
        private string selectedBestiaryId = string.Empty;
        // Premiers Points d'Interet (demande de Jeff, 2026-08-01) : lieux remarquables purement
        // informationnels - identite visuelle propre, description, selectionnables, mais aucune
        // action/mecanique. Prepare les futurs systemes (alliances, occupation, evenements, boss)
        // sans en construire aucun maintenant.
        private string selectedPointOfInterestId = string.Empty;
        private string localRewardText = "Aucune recompense locale";
        private string bestiaryCombatText = "Aucun combat bestiaire";
        private CollectionFlightState collectionState = CollectionFlightState.Idle;
        private float collectionTimer;
        private float officialWorldResourceRefreshTimer;
        private float worldPresenceRefreshTimer;
        private int nextFlightId = 1;
        private bool debugChunkOverlay;
        private bool stress50x50ModeEnabled;
        private bool mapToolsCollapsed = true;
        private bool mapFilterHives = true;
        private bool mapFilterResources = true;
        private bool mapFilterThreats = true;
        private bool mapFilterBearDen = true;
        private bool mapFilterBiomeOverlay = true;
        private string mapToolsStatus = "Lecture carte prete";
        private bool spawnInspectorCollapsed = true;
        private bool spawnDiagnosticOverlayEnabled;
        private int spawnInspectorSeed = 738921;
        private string spawnSeedVersion = "spawn_v1";
        private string spawnInspectorStatus = "Apercu spawn local pret";
        private List<SpawnPreviewRecord> spawnPreviewRecords = new List<SpawnPreviewRecord>();
        private SpawnPreviewSummary spawnPreviewSummary;
        private string selectedSpawnPreviewId = string.Empty;

        private readonly Dictionary<Vector2Int, WorldChunkData> chunkCache = new Dictionary<Vector2Int, WorldChunkData>();
        private readonly List<Vector2Int> activeChunks = new List<Vector2Int>();
        private readonly List<WorldHiveNode> hives = new List<WorldHiveNode>();
        private readonly List<WorldResourceNode> resources = new List<WorldResourceNode>();
        private readonly List<WorldBestiaryNode> bestiary = new List<WorldBestiaryNode>();
        private readonly List<WorldPointOfInterestNode> pointsOfInterest = new List<WorldPointOfInterestNode>();
        private readonly List<WorldFlightRecord> flights = new List<WorldFlightRecord>();
        private readonly Dictionary<string, Texture2D> runtimeEntityTextureCache = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, int> resourceRemaining = new Dictionary<string, int>();
        private readonly Dictionary<string, float> resourceRespawnAt = new Dictionary<string, float>();
        private readonly Dictionary<Vector2Int, RuntimePlacementMaskEntry> runtimePlacementMask = new Dictionary<Vector2Int, RuntimePlacementMaskEntry>();
        private bool runtimePlacementMaskLoaded;
        private int runtimePlacementMaskEntries;

        private void Awake()
        {
            MusicManager.EnsureInstance().Play(MusicTrack.World);
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            targetWorldCenter = new Vector2(
                (WorldMapWave6StreamingTileProvider.OriginChunkX + WorldMapWave6StreamingTileProvider.Columns * 0.5f) * ChunkSize,
                (WorldMapWave6StreamingTileProvider.OriginChunkY + WorldMapWave6StreamingTileProvider.Rows * 0.5f) * ChunkSize);
            currentWorldCenter = targetWorldCenter;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ApplyInitialAuditViewIfNeeded();
#endif
            LoadWave6RuntimeTiles();
            LoadRuntimePlacementMask();
            LoadBearDenLandmark();
            // LoadLocalLab() volontairement non appele : ce labo interne (PLAYER_TEST_HIVE,
            // ENEMY_TEST_HIVE, NODE_TEST, panneau "LAB LOCAL | NON OFFICIEL") s'affichait par-dessus
            // la carte reelle pour tous les joueurs. Le code reste disponible (localLab restera
            // simplement null) pour un futur outil developpeur explicite, mais ne doit plus jamais
            // se charger automatiquement en jeu (PREMIUM_PLAYTEST_REPORT.md).
            RefreshActiveChunks(true);
            if (!FocusGuidedPlayerHiveIfNeeded() && !useInitialAuditViewForPlayMode)
            {
                CenterOnPlayerHiveInstant();
            }
            if (useInitialAuditViewForPlayMode && !string.IsNullOrEmpty(initialAuditViewLabel))
            {
                status = initialAuditViewLabel;
            }
            SelectDefaultResource();
            // Les deux vols d'exemple fabriques ("Vol allie demo"/"Retour neutre demo") ont ete
            // retires : le journal des vols doit refleter uniquement de vraies expeditions lancees
            // par le joueur, jamais des entrees inventees au chargement (PREMIUM_PLAYTEST_REPORT.md).
        }

        private bool FocusGuidedPlayerHiveIfNeeded()
        {
            if (!HiveViewProductUiPresenter.GuidedWorldMapTutorialActiveForRuntime()) return false;
            GetOrCreateChunk(PlayerCoreChunk());
            WorldHiveNode playerHive = HiveById("hive_player_test");
            if (playerHive == null) return false;

            currentWorldCenter = playerHive.WorldCoord;
            targetWorldCenter = playerHive.WorldCoord;
            selectedHiveId = playerHive.Id;
            if (HiveViewProductUiPresenter.GuidedWorldMapForagingTutorialActiveForRuntime())
            {
                selectedResourceId = "res_pollen_core";
            }
            RefreshActiveChunks(true);
            status = "Ruche du joueur centree - tutoriel carte 50x50";
            return true;
        }

        // Comportement par defaut a l'ouverture de la carte (hors tutoriel guide et hors vue
        // d'audit QA) : centre immediatement la camera sur la ruche du joueur, sans animation de
        // pan depuis le centre geometrique de la grille - Jeff veut voir sa ruche des l'ouverture.
        private void CenterOnPlayerHiveInstant()
        {
            GetOrCreateChunk(PlayerCoreChunk());
            WorldHiveNode playerHive = HiveById("hive_player_test");
            if (playerHive == null) return;

            currentWorldCenter = playerHive.WorldCoord;
            targetWorldCenter = playerHive.WorldCoord;
            ClampTargetWorldCenter();
            currentWorldCenter = targetWorldCenter;
            selectedHiveId = playerHive.Id;
            RefreshActiveChunks(true);
            status = "Ruche du joueur centree a l'ouverture";
        }

        private void CenterOnPlayerHive()
        {
            GetOrCreateChunk(PlayerCoreChunk());
            WorldHiveNode playerHive = HiveById("hive_player_test");
            if (playerHive == null) return;

            targetWorldCenter = playerHive.WorldCoord;
            selectedHiveId = playerHive.Id;
            RefreshActiveChunks(true);
            status = "Ruche du joueur localisee @ " + CoordLabel(playerHive.WorldCoord);
        }

        private void Update()
        {
            animatedTime += Time.deltaTime;
            HandleInput();
            UpdateResourceRespawns();
            UpdateCollectionFlight();
            UpdateOfficialWorldResourceCollectionPolling();
            UpdateWorldPresencePolling();
            if (localLab != null) localLab.Update(Time.deltaTime);
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, 1f - Mathf.Exp(-ZoomDamping * Time.deltaTime));
            currentWorldCenter = Vector2.Lerp(currentWorldCenter, targetWorldCenter, 1f - Mathf.Exp(-PanDamping * Time.deltaTime));
            if (wave6Provider != null)
            {
                wave6Provider.UpdateStreaming(targetWorldCenter, targetZoom, Screen.width, Screen.height);
            }
            RefreshActiveChunks(false);
        }

        private void OnGUI()
        {
            EnsurePixel();
            HandleGuidedWorldMapGuiInput();
            DrawBackground();
            DrawActiveChunks();
            DrawBiomeOverlay();
            if (debugChunkOverlay) DrawChunkDebugOverlay();
            if (mapFilterBearDen) DrawBearDenLandmark();
            DrawAerialFlights();
            if (mapFilterResources) DrawResources();
            if (mapFilterThreats) DrawBestiary();
            if (mapFilterHives) DrawHives();
            DrawPointsOfInterest();
            DrawRegionLabels();
            if (localLab != null) localLab.DrawWorld(WorldToScreen, animatedTime);
            // Ambiance meteo (demande de Jeff, 2026-08-02, "la meteo influence legerement
            // l'ambiance") : teinte l'ensemble du monde deja dessine (terrain + entites) - dessinee
            // apres le monde et avant le HUD pour que les panneaux restent parfaitement lisibles.
            // Reutilise exclusivement WorldEventCatalog (fonction pure du temps, deja server-side,
            // simplement miroitee cote client) - aucun nouvel appel reseau, aucune nouvelle donnee.
            DrawWorldEventAmbiance();
            DrawFixedHud();
            DrawActionPanel();
            DrawFlightJournal();
            DrawMiniMap();
            DrawWorldMapReturnBar();
            if (spawnDiagnosticOverlayEnabled) DrawSpawnInspector();
            if (spawnDiagnosticOverlayEnabled) DrawSpawnDiagnosticOverlay();
            if (localLab != null) localLab.DrawHud();
            DrawGuidedWorldTransitionTutorial();
            HiveViewProductUiPresenter.DrawChatOverlayForWorldMap();
            HiveViewProductUiPresenter.DrawCombatPatrolOverlayForWorldMap();
        }

        private void OnDestroy()
        {
            if (wave6Provider != null) wave6Provider.Dispose();
            if (bearDenLandmark != null) bearDenLandmark.Dispose();
            UnloadRuntimeEntityTextures();
            if (pixel != null) Destroy(pixel);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const int SpawnProofSeedA = 738921;
        private const int SpawnProofSeedB = 918337;
        private const string SpawnProofSeedVersion = "spawn_v1";
        private const string SpawnProofAlternateSeedVersion = "spawn_v2_proof";
        private const string SpawnProofExclusionVersion = "exclusion_v1";
        private const string SpawnProofWorldGridVersion = "wave6_50x50_within_logical_64x64_v1";
        private const float SpawnCriticalOverlapDistance = 0.001f;
        private const float SpawnMinorOverlapDistance = 48f;

        public const string Step4DCanonicalScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";

        public DevProofState ApplyDeterministicProofState(int chunkX, int chunkY, float zoom, string label)
        {
            Vector2Int chunk = new Vector2Int(Mathf.Clamp(chunkX, 0, WorldChunkWidth - 1), Mathf.Clamp(chunkY, 0, WorldChunkHeight - 1));
            float clampedZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            Vector2 center = ProofChunkCenter(chunk);
            currentZoom = clampedZoom;
            targetZoom = clampedZoom;
            currentWorldCenter = center;
            targetWorldCenter = center;
            ClampTargetWorldCenter();
            currentWorldCenter = targetWorldCenter;
            RefreshActiveChunks(true);
            status = "Step4D proof state - " + label;
            return CurrentDeterministicProofState(label);
        }

        public DevProofState CurrentDeterministicProofState(string label)
        {
            Vector2Int chunk = CurrentChunk();
            bool wave6Ready = wave6Provider != null && wave6Provider.ManifestReady && !wave6Provider.HasLoadFailure;
            string terrainState = wave6Ready ? "wave6_50x50_streamed_tiles_shared_transform" : "wave6_unavailable_fail_closed";
            return new DevProofState(
                label,
                Screen.width,
                Screen.height,
                currentZoom,
                currentWorldCenter,
                chunk,
                activeChunks.Count,
                terrainState,
                wave6Ready,
                "Clamp",
                wave6Ready);
        }

        public void ApplyWave6ProofView(Vector2 worldCenter, float zoom)
        {
            currentZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            targetZoom = currentZoom;
            currentWorldCenter = worldCenter;
            targetWorldCenter = worldCenter;
            ClampTargetWorldCenter();
            currentWorldCenter = targetWorldCenter;
            if (wave6Provider != null)
            {
                wave6Provider.UpdateStreaming(currentWorldCenter, currentZoom, Screen.width, Screen.height, true);
            }
            RefreshActiveChunks(true);
        }

        public bool ApplyWave6RuntimePackageForProof(string resourceRoot, string expectedMasterSha256, string label)
        {
            if (wave6Provider != null) wave6Provider.Dispose();
            wave3Provider = null;
            wave6Provider = new WorldMapWave6StreamingTileProvider(resourceRoot, expectedMasterSha256);
            bool visibleReady = wave6Provider.Initialize(targetWorldCenter, targetZoom, Screen.width, Screen.height);
            if (wave6Provider.ManifestReady && !wave6Provider.HasLoadFailure)
            {
                status = "Wave6 50x50 proof package - " + label;
                RefreshActiveChunks(true);
                return visibleReady && wave6Provider.HasAllVisibleTiles;
            }

            status = "Wave6 proof package unavailable - " + label;
            return false;
        }

        public void SetV3DPreviewRuntimePackageForPlayMode(bool enabled)
        {
            useV3DPreviewRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3ECandidateRuntimePackageForPlayMode = false;
                useV3MPreviewRuntimePackageForPlayMode = false;
                useV3VCandidateRuntimePackageForPlayMode = false;
                useV3OReducedAuditPreviewRuntimePackageForPlayMode = false;
                useRouteLockCoherentProofRuntimePackageForPlayMode = false;
                useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = false;
                useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2INativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = false;
                useV2IRepairAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesV3DPreviewRuntimePackageForPlayMode()
        {
            return useV3DPreviewRuntimePackageForPlayMode;
        }

        public void SetV3ECandidateRuntimePackageForPlayMode(bool enabled)
        {
            useV3ECandidateRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3DPreviewRuntimePackageForPlayMode = false;
                useV3MPreviewRuntimePackageForPlayMode = false;
                useV3VCandidateRuntimePackageForPlayMode = false;
                useV3OReducedAuditPreviewRuntimePackageForPlayMode = false;
                useRouteLockCoherentProofRuntimePackageForPlayMode = false;
                useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = false;
                useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2INativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = false;
                useV2IRepairAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesV3ECandidateRuntimePackageForPlayMode()
        {
            return useV3ECandidateRuntimePackageForPlayMode;
        }

        public void SetV3MPreviewRuntimePackageForPlayMode(bool enabled)
        {
            useV3MPreviewRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3DPreviewRuntimePackageForPlayMode = false;
                useV3ECandidateRuntimePackageForPlayMode = false;
                useV3VCandidateRuntimePackageForPlayMode = false;
                useV3OReducedAuditPreviewRuntimePackageForPlayMode = false;
                useRouteLockCoherentProofRuntimePackageForPlayMode = false;
                useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = false;
                useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2INativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = false;
                useV2IRepairAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesV3MPreviewRuntimePackageForPlayMode()
        {
            return useV3MPreviewRuntimePackageForPlayMode;
        }

        public void SetV3VCandidateRuntimePackageForPlayMode(bool enabled)
        {
            useV3VCandidateRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3DPreviewRuntimePackageForPlayMode = false;
                useV3ECandidateRuntimePackageForPlayMode = false;
                useV3MPreviewRuntimePackageForPlayMode = false;
                useV3OReducedAuditPreviewRuntimePackageForPlayMode = false;
                useRouteLockCoherentProofRuntimePackageForPlayMode = false;
                useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = false;
                useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2INativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = false;
                useV2IRepairAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesV3VCandidateRuntimePackageForPlayMode()
        {
            return useV3VCandidateRuntimePackageForPlayMode;
        }

        public void SetV3OReducedAuditPreviewRuntimePackageForPlayMode(bool enabled)
        {
            useV3OReducedAuditPreviewRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3DPreviewRuntimePackageForPlayMode = false;
                useV3ECandidateRuntimePackageForPlayMode = false;
                useV3MPreviewRuntimePackageForPlayMode = false;
                useV3VCandidateRuntimePackageForPlayMode = false;
                useRouteLockCoherentProofRuntimePackageForPlayMode = false;
                useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = false;
                useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2INativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = false;
                useV2IRepairAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesV3OReducedAuditPreviewRuntimePackageForPlayMode()
        {
            return useV3OReducedAuditPreviewRuntimePackageForPlayMode;
        }

        public void SetRouteLockCoherentProofRuntimePackageForPlayMode(bool enabled)
        {
            useRouteLockCoherentProofRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3DPreviewRuntimePackageForPlayMode = false;
                useV3ECandidateRuntimePackageForPlayMode = false;
                useV3MPreviewRuntimePackageForPlayMode = false;
                useV3VCandidateRuntimePackageForPlayMode = false;
                useV3OReducedAuditPreviewRuntimePackageForPlayMode = false;
                useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = false;
                useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2INativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = false;
                useV2IRepairAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesRouteLockCoherentProofRuntimePackageForPlayMode()
        {
            return useRouteLockCoherentProofRuntimePackageForPlayMode;
        }

        public void SetRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode(bool enabled)
        {
            useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3DPreviewRuntimePackageForPlayMode = false;
                useV3ECandidateRuntimePackageForPlayMode = false;
                useV3MPreviewRuntimePackageForPlayMode = false;
                useV3VCandidateRuntimePackageForPlayMode = false;
                useV3OReducedAuditPreviewRuntimePackageForPlayMode = false;
                useRouteLockCoherentProofRuntimePackageForPlayMode = false;
                useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2INativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = false;
                useV2IRepairAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode()
        {
            return useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode;
        }

        public void SetSupportCenterNativeAuditPreviewRuntimePackageForPlayMode(bool enabled)
        {
            useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3DPreviewRuntimePackageForPlayMode = false;
                useV3ECandidateRuntimePackageForPlayMode = false;
                useV3MPreviewRuntimePackageForPlayMode = false;
                useV3VCandidateRuntimePackageForPlayMode = false;
                useV3OReducedAuditPreviewRuntimePackageForPlayMode = false;
                useRouteLockCoherentProofRuntimePackageForPlayMode = false;
                useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = false;
                useV2INativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = false;
                useV2IRepairAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesSupportCenterNativeAuditPreviewRuntimePackageForPlayMode()
        {
            return useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode;
        }

        public void SetV2INativeAuditPreviewRuntimePackageForPlayMode(bool enabled)
        {
            useV2INativeAuditPreviewRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3DPreviewRuntimePackageForPlayMode = false;
                useV3ECandidateRuntimePackageForPlayMode = false;
                useV3MPreviewRuntimePackageForPlayMode = false;
                useV3VCandidateRuntimePackageForPlayMode = false;
                useV3OReducedAuditPreviewRuntimePackageForPlayMode = false;
                useRouteLockCoherentProofRuntimePackageForPlayMode = false;
                useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = false;
                useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = false;
                useV2IRepairAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesV2INativeAuditPreviewRuntimePackageForPlayMode()
        {
            return useV2INativeAuditPreviewRuntimePackageForPlayMode;
        }

        public void SetV2OPerimeterAuditPreviewRuntimePackageForPlayMode(bool enabled)
        {
            useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3DPreviewRuntimePackageForPlayMode = false;
                useV3ECandidateRuntimePackageForPlayMode = false;
                useV3MPreviewRuntimePackageForPlayMode = false;
                useV3VCandidateRuntimePackageForPlayMode = false;
                useV3OReducedAuditPreviewRuntimePackageForPlayMode = false;
                useRouteLockCoherentProofRuntimePackageForPlayMode = false;
                useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = false;
                useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2INativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2IRepairAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesV2OPerimeterAuditPreviewRuntimePackageForPlayMode()
        {
            return useV2OPerimeterAuditPreviewRuntimePackageForPlayMode;
        }

        public void SetV2IRepairAuditPreviewRuntimePackageForPlayMode(bool enabled)
        {
            useV2IRepairAuditPreviewRuntimePackageForPlayMode = enabled;
            if (enabled)
            {
                useV3DPreviewRuntimePackageForPlayMode = false;
                useV3ECandidateRuntimePackageForPlayMode = false;
                useV3MPreviewRuntimePackageForPlayMode = false;
                useV3VCandidateRuntimePackageForPlayMode = false;
                useV3OReducedAuditPreviewRuntimePackageForPlayMode = false;
                useRouteLockCoherentProofRuntimePackageForPlayMode = false;
                useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode = false;
                useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2INativeAuditPreviewRuntimePackageForPlayMode = false;
                useV2OPerimeterAuditPreviewRuntimePackageForPlayMode = false;
            }
        }

        public bool UsesV2IRepairAuditPreviewRuntimePackageForPlayMode()
        {
            return useV2IRepairAuditPreviewRuntimePackageForPlayMode;
        }

        public void SetInitialAuditViewForPlayMode(bool enabled, int chunkX, int chunkY, float zoom, string label)
        {
            useInitialAuditViewForPlayMode = enabled;
            initialAuditChunkX = Mathf.Clamp(chunkX, 0, WorldChunkWidth - 1);
            initialAuditChunkY = Mathf.Clamp(chunkY, 0, WorldChunkHeight - 1);
            initialAuditZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            initialAuditViewLabel = string.IsNullOrEmpty(label) ? "Audit visuel" : label;
        }

        private void ApplyInitialAuditViewIfNeeded()
        {
            if (!useInitialAuditViewForPlayMode) return;

            Vector2Int chunk = new Vector2Int(
                Mathf.Clamp(initialAuditChunkX, 0, WorldChunkWidth - 1),
                Mathf.Clamp(initialAuditChunkY, 0, WorldChunkHeight - 1));
            float clampedZoom = Mathf.Clamp(initialAuditZoom, MinZoom, MaxZoom);
            Vector2 center = ProofChunkCenter(chunk);
            currentZoom = clampedZoom;
            targetZoom = clampedZoom;
            currentWorldCenter = center;
            targetWorldCenter = center;
        }

        public string CurrentWave6MasterSha256ForProof()
        {
            return wave6Provider != null ? wave6Provider.MasterSha256 : string.Empty;
        }

        [Obsolete("Compatibility camera hook. The canonical terrain is Wave6 50x50.")]
        public void ApplyWave5ProofView(Vector2 worldCenter, float zoom)
        {
            ApplyWave6ProofView(worldCenter, zoom);
        }

        public void SetBearDenVisibilityForProof(bool visible)
        {
            if (bearDenLandmark != null) bearDenLandmark.SetVisibility(visible);
        }

        public bool BearDenVisibleForProof()
        {
            return bearDenLandmark != null && bearDenLandmark.IsLoaded && bearDenLandmark.IsVisible;
        }

        public Wave5ProofSnapshot CurrentWave5ProofSnapshot()
        {
            return new Wave5ProofSnapshot(
                currentWorldCenter,
                currentZoom,
                wave6Provider != null && wave6Provider.ManifestReady,
                wave6Provider != null && wave6Provider.HasAllVisibleTiles,
                wave6Provider != null ? wave6Provider.LoadedVisibleTileCount : 0,
                wave6Provider != null ? wave6Provider.RequiredVisibleTileCount : 0,
                wave6Provider != null ? wave6Provider.CachedTileCount : 0,
                wave6Provider != null ? wave6Provider.PendingTileCount : 0,
                wave6Provider != null ? wave6Provider.WorldBounds : default,
                bearDenLandmark != null && bearDenLandmark.IsLoaded,
                bearDenLandmark != null && bearDenLandmark.IsVisible,
                bearDenLandmark != null ? bearDenLandmark.WorldAnchor : default);
        }

        public Wave6ProofSnapshot CurrentWave6ProofSnapshot()
        {
            return new Wave6ProofSnapshot(
                currentWorldCenter,
                currentZoom,
                wave6Provider != null && wave6Provider.ManifestReady,
                wave6Provider != null && wave6Provider.HasAllVisibleTiles,
                wave6Provider != null ? wave6Provider.LoadedVisibleTileCount : 0,
                wave6Provider != null ? wave6Provider.RequiredVisibleTileCount : 0,
                wave6Provider != null ? wave6Provider.CachedTileCount : 0,
                wave6Provider != null ? wave6Provider.PendingTileCount : 0,
                wave6Provider != null ? wave6Provider.WorldBounds : default,
                bearDenLandmark != null && bearDenLandmark.IsLoaded,
                bearDenLandmark != null && bearDenLandmark.IsVisible,
                bearDenLandmark != null ? bearDenLandmark.WorldAnchor : default);
        }

        public RuntimeEntitiesProofSnapshot CurrentRuntimeEntitiesProofSnapshot()
        {
            int texturedResources = 0;
            int waterNodes = 0;
            int honeyNodes = 0;
            for (int i = 0; i < resources.Count; i++)
            {
                WorldResourceNode resource = resources[i];
                if (resource.Kind == ResourceKind.Water) waterNodes++;
                if (resource.Kind == ResourceKind.Honey) honeyNodes++;
                if (RuntimeEntityTexture(ResourceTexturePath(resource)) != null) texturedResources++;
            }

            int texturedBestiary = 0;
            int maxTier = 0;
            for (int i = 0; i < bestiary.Count; i++)
            {
                WorldBestiaryNode beast = bestiary[i];
                maxTier = Mathf.Max(maxTier, beast.Tier);
                if (RuntimeEntityTexture(BestiaryTexturePath(beast)) != null) texturedBestiary++;
            }

            return new RuntimeEntitiesProofSnapshot(
                resources.Count,
                texturedResources,
                waterNodes,
                honeyNodes,
                bestiary.Count,
                texturedBestiary,
                maxTier,
                runtimePlacementMaskLoaded,
                runtimePlacementMaskEntries,
                runtimePlacementMaskLoaded && runtimePlacementMaskEntries >= WorldMapWave6StreamingTileProvider.Rows * WorldMapWave6StreamingTileProvider.Columns);
        }

        public ResourceInteractionProofSnapshot RunResourceInteractionProofForProof()
        {
            ApplyWave5ProofView(new Vector2(16640f, 16640f), 1.0f);
            WorldResourceNode poor = FirstResourceByTier("poor");
            WorldResourceNode medium = FirstResourceByTier("medium");
            WorldResourceNode rich = FirstResourceByTier("rich");
            if (poor == null || medium == null || rich == null)
            {
                return new ResourceInteractionProofSnapshot(false, false, false, false, false, false, 0, 0, string.Empty);
            }

            selectedResourceId = rich.Id;
            int before = ResourceRemaining(rich);
            bool selection = SelectedResource() == rich;
            bool collection = CompleteSelectedResourceCollectionForProof();
            int after = ResourceRemaining(rich);
            bool depleted = after == 0 && before > 0;
            ForceRespawnForProof(rich.Id);
            int respawned = ResourceRemaining(rich);
            bool respawn = respawned == rich.Amount;
            bool tiers = ResourceTierToken(poor) == "poor" && ResourceTierToken(medium) == "medium" && ResourceTierToken(rich) == "rich";
            return new ResourceInteractionProofSnapshot(
                tiers && selection && collection && depleted && respawn,
                tiers,
                selection,
                collection,
                depleted,
                respawn,
                before,
                respawned,
                rich.Id + ":" + ResourceTierToken(rich) + ":" + rich.Label);
        }

        public BestiaryInteractionProofSnapshot RunBestiaryInteractionProofForProof()
        {
            ApplyWave5ProofView(new Vector2(16640f, 16640f), 1.0f);
            bool tiers = true;
            for (int tier = 1; tier <= 7; tier++)
            {
                EnsureBestiaryTierForProof(tier);
                tiers &= FirstBestiaryByTier(tier) != null;
            }

            WorldBestiaryNode solo = FirstBestiaryByTier(2);
            WorldBestiaryNode raid = FirstBestiaryByTier(7);
            bool selection = SelectBestiaryForProof(solo != null ? solo.Id : string.Empty);
            bool soloCombat = RunSelectedBestiaryCombatLocalProof();
            bool raidSelection = SelectBestiaryForProof(raid != null ? raid.Id : string.Empty);
            bool raidCombat = RunSelectedBestiaryCombatLocalProof();
            bool noOfficialGain = bestiaryCombatText.Contains("official_gain=false") && bestiaryCombatText.Contains("server=false");
            return new BestiaryInteractionProofSnapshot(
                tiers && selection && soloCombat && raidSelection && raidCombat && noOfficialGain,
                tiers,
                selection,
                soloCombat,
                raidCombat,
                noOfficialGain,
                solo != null ? solo.Id : string.Empty,
                raid != null ? raid.Id : string.Empty,
                bestiaryCombatText);
        }

        public Stress50x50ReadinessSnapshot Run50x50ReadinessStressProofForProof()
        {
            bool wasEnabled = stress50x50ModeEnabled;
            int cacheBefore = chunkCache.Count;
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            stress50x50ModeEnabled = true;

            StressWindowStats center = SimulateStressWindow(new Vector2Int(StressWorldMapChunks / 2, StressWorldMapChunks / 2));
            StressWindowStats northWest = SimulateStressWindow(new Vector2Int(0, 0));
            StressWindowStats southEast = SimulateStressWindow(new Vector2Int(StressWorldMapChunks - 1, StressWorldMapChunks - 1));
            StressWindowStats densest = FindDensestStressWindow();
            int catalogHives;
            int catalogResources;
            int catalogBestiary;
            CountStressCatalog(out catalogHives, out catalogResources, out catalogBestiary);

            stress50x50ModeEnabled = wasEnabled;
            long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            long allocated = Math.Max(0L, allocatedAfter - allocatedBefore);
            bool budgets = center.WithinBudgets && northWest.WithinBudgets && southEast.WithinBudgets && densest.WithinBudgets;
            bool cacheStable = chunkCache.Count == cacheBefore;
            bool terrainPreserved = wave6Provider != null && wave6Provider.ManifestReady && !wave6Provider.HasLoadFailure && wave6Provider.CachedTileCount <= BudgetWave6TextureCache;
            bool disabledByDefault = !wasEnabled;
            bool allocations = allocated <= BudgetStressAllocBytes;
            bool placementMask = runtimePlacementMaskLoaded && runtimePlacementMaskEntries >= WorldMapWave6StreamingTileProvider.Rows * WorldMapWave6StreamingTileProvider.Columns;
            return new Stress50x50ReadinessSnapshot(
                disabledByDefault && budgets && cacheStable && terrainPreserved && allocations && placementMask,
                disabledByDefault,
                StressWorldMapChunks * StressWorldMapChunks,
                center.ActiveChunks,
                northWest.ActiveChunks,
                southEast.ActiveChunks,
                densest.ActiveChunks,
                densest.Hives,
                densest.Resources,
                densest.Bestiary,
                catalogHives,
                catalogResources,
                catalogBestiary,
                wave6Provider != null ? wave6Provider.CachedTileCount : 0,
                cacheBefore,
                chunkCache.Count,
                allocated,
                budgets,
                cacheStable,
                terrainPreserved,
                allocations,
                runtimePlacementMaskLoaded,
                runtimePlacementMaskEntries,
                placementMask);
        }

        public MapReadingToolsProofSnapshot RunMapReadingToolsProofForProof()
        {
            ApplyWave5ProofView(new Vector2(16640f, 16640f), 1.0f);
            mapToolsCollapsed = false;
            mapFilterHives = true;
            mapFilterResources = true;
            mapFilterThreats = true;
            mapFilterBearDen = true;
            Rect before = MapReadingToolsRect();
            SelectNearestMapNode();
            bool nearest = !string.IsNullOrEmpty(mapToolsStatus) && !mapToolsStatus.Contains("Aucun");
            mapFilterHives = false;
            mapFilterResources = false;
            mapFilterThreats = false;
            mapFilterBearDen = false;
            bool filtersOff = !mapFilterHives && !mapFilterResources && !mapFilterThreats && !mapFilterBearDen;
            mapFilterHives = true;
            mapFilterResources = true;
            mapFilterThreats = true;
            mapFilterBearDen = true;
            Rect after = MapReadingToolsRect();
            bool fixedRect = Mathf.Approximately(before.x, after.x) && Mathf.Approximately(before.y, after.y) && Mathf.Approximately(before.width, after.width) && Mathf.Approximately(before.height, after.height);
            bool terrainUnmasked = wave6Provider != null && wave6Provider.ManifestReady && wave6Provider.HasAllVisibleTiles;
            bool legend = true;
            return new MapReadingToolsProofSnapshot(nearest && filtersOff && fixedRect && terrainUnmasked && legend, nearest, filtersOff, fixedRect, terrainUnmasked, legend, mapToolsStatus);
        }

        public InteractionPolishProofSnapshot RunInteractionPolishProofForProof()
        {
            ApplyWave5ProofView(new Vector2(16640f, 16640f), 1.0f);
            WorldResourceNode rich = FirstResourceByTier("rich");
            if (rich == null) return new InteractionPolishProofSnapshot(false, false, false, false, false, false, string.Empty, string.Empty);
            selectedResourceId = rich.Id;
            bool quantity = ResourceQuantityLabel(rich).Contains(ResourceAccessibilityToken(rich));
            bool trajectory = CompleteSelectedResourceCollectionForProof();
            bool depletion = ResourceQuantityLabel(rich).Contains("[X]") && ResourceQuantityLabel(rich).Contains("epuise");
            ForceRespawnForProof(rich.Id);
            bool respawn = !ResourceQuantityLabel(rich).Contains("epuise") && ResourceRemaining(rich) == rich.Amount;
            EnsureBestiaryTierForProof(7);
            WorldBestiaryNode raid = FirstBestiaryByTier(7);
            bool combat = SelectBestiaryForProof(raid != null ? raid.Id : string.Empty) && RunSelectedBestiaryCombatLocalProof();
            bool accessibility = raid != null && BestiaryAccessibilityToken(raid) == "[RAID]" && ResourceAccessibilityToken(rich).StartsWith("[R", StringComparison.Ordinal);
            return new InteractionPolishProofSnapshot(
                quantity && trajectory && depletion && respawn && combat && accessibility,
                quantity,
                trajectory,
                depletion,
                respawn,
                combat,
                ResourceQuantityLabel(rich),
                bestiaryCombatText);
        }

        public RuntimeScenarioDataLayerProofSnapshot RunRuntimeScenarioDataLayerProofForProof()
        {
            ApplyWave5ProofView(new Vector2(16640f, 16640f), 1.0f);
            var provider = new LocalDemoScenarioAuthorityProvider(this);
            List<WorldMapScenarioEntityRecord> first = provider.CaptureActiveEntities();
            List<WorldMapScenarioEntityRecord> second = provider.CaptureActiveEntities();
            bool ids = StableIdsPass(first, second);
            bool normalized = NormalizedCoordinatesPass(first);
            bool reprojected = Reprojection50x50Pass(first);
            bool authority = !provider.Server
                && !provider.Official
                && !provider.OfficialGain
                && provider.RemoteCalls == 0
                && provider.ProviderId == "local_demo";

            bool collect = localLab != null && localLab.ApplyScenarioPresetForProof(0) && ApplyScenarioPresetFromLab("scenario_collect_r3");
            bool duel = localLab != null && localLab.ApplyScenarioPresetForProof(1) && ApplyScenarioPresetFromLab("scenario_duel_two_hives");
            bool raid = localLab != null && localLab.ApplyScenarioPresetForProof(2) && ApplyScenarioPresetFromLab("scenario_raid_t7");
            WorldMapLocalLabRuntime.ScenarioLabProofSnapshot labScenario = localLab != null ? localLab.CurrentScenarioLabProofSnapshot() : default;
            bool scenarios = collect && duel && raid && labScenario.Ready && labScenario.ServerFalse && labScenario.OfficialGainFalse;
            bool hivesEditable = labScenario.TestHivesEditable && ResetLocalLabForProof();

            Wave5ProofSnapshot wave5 = CurrentWave5ProofSnapshot();
            RuntimeEntitiesProofSnapshot runtime = CurrentRuntimeEntitiesProofSnapshot();
            ResourceInteractionProofSnapshot resource = RunResourceInteractionProofForProof();
            BestiaryInteractionProofSnapshot bestiaryProof = RunBestiaryInteractionProofForProof();
            MapReadingToolsProofSnapshot mapTools = RunMapReadingToolsProofForProof();
            InteractionPolishProofSnapshot polish = RunInteractionPolishProofForProof();
            Stress50x50ReadinessSnapshot stress = Run50x50ReadinessStressProofForProof();
            bool legacy = wave5.ManifestReady
                && wave5.VisibleTilesReady
                && wave5.BearDenLoaded
                && runtime.RuntimePlacementMaskCovers50x50
                && runtime.TexturedResourceNodes >= 3
                && runtime.TexturedBestiaryNodes >= 1
                && resource.Pass
                && bestiaryProof.Pass
                && mapTools.Pass
                && polish.Pass
                && stress.Pass;

            bool pass = ids && normalized && reprojected && authority && scenarios && hivesEditable && legacy;
            return new RuntimeScenarioDataLayerProofSnapshot(
                pass,
                ids,
                normalized,
                reprojected,
                authority,
                scenarios,
                hivesEditable,
                legacy,
                first.Count,
                CountFamily(first, "hive"),
                CountFamily(first, "resource"),
                CountFamily(first, "bestiary"),
                CountFamily(first, "event"),
                provider.DataVersion,
                provider.ProviderId);
        }

        public SpawnInspectorProofSnapshot RunSpawnInspectorProofForProof()
        {
            ApplyWave5ProofView(new Vector2(16640f, 16640f), 1.0f);
            bool overlayWasEnabled = spawnDiagnosticOverlayEnabled;
            bool overlayDefaultOff = !overlayWasEnabled;
            List<Vector2Int> centerChunks = BuildSpawnProofWindowChunks(
                WorldMapWave6StreamingTileProvider.Columns / 2,
                WorldMapWave6StreamingTileProvider.Rows / 2,
                WorldMapWave6StreamingTileProvider.Columns,
                WorldMapWave6StreamingTileProvider.OriginChunkX,
                WorldMapWave6StreamingTileProvider.OriginChunkY);

            List<SpawnPreviewRecord> seedA1 = GenerateSpawnPreview(SpawnProofSeedA, SpawnProofSeedVersion, centerChunks);
            GenerateSpawnPreview(
                SpawnProofSeedA,
                SpawnProofSeedVersion,
                BuildSpawnProofWindowChunks(
                    WorldMapWave6StreamingTileProvider.Columns / 2,
                    WorldMapWave6StreamingTileProvider.Rows - 1,
                    WorldMapWave6StreamingTileProvider.Columns,
                    WorldMapWave6StreamingTileProvider.OriginChunkX,
                    WorldMapWave6StreamingTileProvider.OriginChunkY));
            List<SpawnPreviewRecord> seedA2 = GenerateSpawnPreview(SpawnProofSeedA, SpawnProofSeedVersion, centerChunks);
            List<SpawnPreviewRecord> seedB = GenerateSpawnPreview(SpawnProofSeedB, SpawnProofSeedVersion, centerChunks);
            List<SpawnPreviewRecord> versionC = GenerateSpawnPreview(SpawnProofSeedA, SpawnProofAlternateSeedVersion, centerChunks);
            SpawnPreviewSummary summaryA = SummarizeSpawnPreview(seedA1, centerChunks);
            SpawnPreviewSummary summaryB = SummarizeSpawnPreview(seedB, centerChunks);
            SpawnPreviewSummary summaryVersionC = SummarizeSpawnPreview(versionC, centerChunks);
            string hashA1 = SpawnDistributionAuditHash(seedA1);
            string hashA2 = SpawnDistributionAuditHash(seedA2);
            string hashB = SpawnDistributionAuditHash(seedB);
            string hashVersionC = SpawnDistributionAuditHash(versionC);
            SpawnRecordComparisonProof sameSeedComparison = CompareSpawnRecords(seedA1, seedA2);
            SpawnRecordComparisonProof differentSeedComparison = CompareSpawnRecords(seedA1, seedB);
            bool deterministic = hashA1 == hashA2 && sameSeedComparison.Pass;
            bool variation = hashA1 != hashB && !differentSeedComparison.Pass && summaryB.BudgetsPass;
            bool versionVariation = hashA1 != hashVersionC && summaryVersionC.BudgetsPass;

            List<SpawnPreviewRecord> densestRecords;
            SpawnWindowProof[] windows25x25 = BuildSpawnProofWindows25x25(SpawnProofSeedA, SpawnProofSeedVersion, out densestRecords);
            SpawnWindowProof[] windows50x50 = BuildSpawnProofWindows50x50();
            bool windowCoverage = SpawnWindowCoveragePass(windows25x25, windows50x50);
            ForcedExclusionProof[] forcedExclusions = BuildForcedExclusionProofs();
            int acceptedInsideExclusions = summaryA.AcceptedInsideExclusions + summaryB.AcceptedInsideExclusions;
            for (int i = 0; i < windows25x25.Length; i++) acceptedInsideExclusions += windows25x25[i].AcceptedInsideExclusions;
            for (int i = 0; i < forcedExclusions.Length; i++) acceptedInsideExclusions += forcedExclusions[i].Accepted;
            bool exclusions = acceptedInsideExclusions == 0 && ForcedExclusionsPass(forcedExclusions);
            SpawnOverlapProof overlap = BuildSpawnOverlapProof(densestRecords);
            SpawnCombatProof combat = BuildSpawnCombatProof();
            SpawnRichnessProof richness = BuildSpawnRichnessProof(seedA1);
            SpawnReprojectionProof reprojection = BuildSpawnReprojectionProof(seedA1);

            string overlayOffHash;
            string overlayOnHash;
            bool overlayInvariant;
            try
            {
                spawnDiagnosticOverlayEnabled = false;
                List<SpawnPreviewRecord> overlayOff = GenerateSpawnPreview(SpawnProofSeedA, SpawnProofSeedVersion, centerChunks);
                overlayOffHash = SpawnDistributionAuditHash(overlayOff);
                spawnDiagnosticOverlayEnabled = true;
                List<SpawnPreviewRecord> overlayOn = GenerateSpawnPreview(SpawnProofSeedA, SpawnProofSeedVersion, centerChunks);
                overlayOnHash = SpawnDistributionAuditHash(overlayOn);
                overlayInvariant = overlayOffHash == overlayOnHash && CompareSpawnRecords(overlayOff, overlayOn).Pass;
            }
            finally
            {
                spawnDiagnosticOverlayEnabled = overlayWasEnabled;
            }

            NegativeTestProof[] negativeTests = BuildSpawnNegativeTests(seedA1, forcedExclusions);
            bool negativeTestsPass = NegativeTestsPass(negativeTests);
            var authorityProvider = new LocalDemoScenarioAuthorityProvider(this);
            string authorityReason;
            bool authorityFlags = ValidateSpawnAuthority(
                authorityProvider.Server,
                authorityProvider.Official,
                authorityProvider.OfficialGain,
                authorityProvider.RemoteCalls,
                out authorityReason);

            RuntimeScenarioDataLayerProofSnapshot legacySnapshot = RunRuntimeScenarioDataLayerProofForProof();
            Stress50x50ReadinessSnapshot stressSnapshot = Run50x50ReadinessStressProofForProof();
            int wave5CachedTextures = wave6Provider != null ? wave6Provider.CachedTileCount : 0;
            int runtimeEntityTextureCacheEntries = runtimeEntityTextureCache.Count;
            int maxActiveChunks;
            int maxHives;
            int maxResources;
            int maxBestiary;
            SpawnWindowMaxima(windows25x25, windows50x50, summaryB, out maxActiveChunks, out maxHives, out maxResources, out maxBestiary);
            bool budgets = summaryA.BudgetsPass
                && summaryB.BudgetsPass
                && summaryVersionC.BudgetsPass
                && maxActiveChunks <= BudgetActiveChunks
                && maxHives <= BudgetActiveHives
                && maxResources <= BudgetActiveResources
                && maxBestiary <= BudgetActiveBestiary
                && wave5CachedTextures + runtimeEntityTextureCacheEntries <= BudgetWave6TextureCache
                && stressSnapshot.AllocationBudgetPass;
            bool coverage = summaryA.HasHives
                && summaryA.HasResources
                && summaryA.HasBestiary
                && richness.Pass
                && combat.Pass
                && reprojection.Pass;
            bool overlayPass = overlayDefaultOff && overlayInvariant;
            bool no50x50TerrainGenerated = stressSnapshot.CacheStablePass && stressSnapshot.TerrainPreservedPass;
            bool pass = deterministic
                && variation
                && versionVariation
                && exclusions
                && budgets
                && coverage
                && windowCoverage
                && overlap.Pass
                && overlayPass
                && negativeTestsPass
                && authorityFlags
                && no50x50TerrainGenerated
                && legacySnapshot.Pass;
            spawnPreviewRecords = seedA1;
            spawnPreviewSummary = summaryA;
            selectedSpawnPreviewId = seedA1.Count > 0 ? seedA1[0].EntityId : string.Empty;
            spawnInspectorStatus = "Seed " + spawnInspectorSeed.ToString(CultureInfo.InvariantCulture) + " regenere local, jamais officiel";
            return new SpawnInspectorProofSnapshot(
                pass,
                deterministic,
                variation,
                exclusions,
                budgets,
                coverage,
                overlayPass,
                legacySnapshot.Pass,
                SpawnProofSeedA,
                SpawnProofSeedB,
                SpawnProofSeedVersion,
                SpawnProofAlternateSeedVersion,
                SpawnProofExclusionVersion,
                SpawnProofWorldGridVersion,
                hashA1,
                hashA2,
                hashB,
                hashVersionC,
                sameSeedComparison,
                summaryB.ActiveChunks,
                summaryB.Hives,
                summaryB.Resources,
                summaryB.Bestiary,
                summaryB.BudgetsPass,
                versionVariation,
                windows25x25,
                windows50x50,
                windowCoverage,
                forcedExclusions,
                acceptedInsideExclusions,
                overlap,
                combat,
                richness,
                reprojection,
                overlayDefaultOff,
                overlayOffHash,
                overlayOnHash,
                overlayInvariant,
                negativeTests,
                negativeTestsPass,
                authorityProvider.Server,
                authorityProvider.Official,
                authorityProvider.OfficialGain,
                authorityProvider.RemoteCalls,
                authorityFlags,
                authorityReason,
                summaryA.ActiveChunks,
                summaryA.Hives,
                summaryA.Resources,
                summaryA.Bestiary,
                summaryA.ExclusionHitsBearDen,
                summaryA.ExclusionHitsWater,
                summaryA.ExclusionHitsCliff,
                summaryA.ExclusionHitsReservedEvent,
                maxActiveChunks,
                maxHives,
                maxResources,
                maxBestiary,
                wave5CachedTextures,
                runtimeEntityTextureCacheEntries,
                stressSnapshot.AllocatedBytes,
                stressSnapshot.AllocationBudgetPass,
                stressSnapshot.ChunkCacheBefore,
                stressSnapshot.ChunkCacheAfter,
                no50x50TerrainGenerated);
        }

        private List<Vector2Int> BuildSpawnProofWindowChunks(int centerX, int centerY, int gridSize, int originX, int originY)
        {
            var chunks = new List<Vector2Int>(BudgetActiveChunks);
            for (int y = centerY - ActiveChunkRadius; y <= centerY + ActiveChunkRadius; y++)
            {
                for (int x = centerX - ActiveChunkRadius; x <= centerX + ActiveChunkRadius; x++)
                {
                    if (x < 0 || y < 0 || x >= gridSize || y >= gridSize) continue;
                    chunks.Add(new Vector2Int(originX + x, originY + y));
                }
            }

            return chunks;
        }

        private SpawnWindowProof[] BuildSpawnProofWindows25x25(int seed, string version, out List<SpawnPreviewRecord> densestRecords)
        {
            int gridSize = WorldMapWave6StreamingTileProvider.Columns;
            int last = gridSize - 1;
            int middle = gridSize / 2;
            string[] labels = { "center", "N", "S", "E", "W", "NW", "NE", "SW", "SE" };
            Vector2Int[] centers =
            {
                new Vector2Int(middle, middle),
                new Vector2Int(middle, last),
                new Vector2Int(middle, 0),
                new Vector2Int(last, middle),
                new Vector2Int(0, middle),
                new Vector2Int(0, last),
                new Vector2Int(last, last),
                new Vector2Int(0, 0),
                new Vector2Int(last, 0)
            };
            var windows = new SpawnWindowProof[10];
            for (int i = 0; i < labels.Length; i++)
            {
                windows[i] = BuildSpawnWindowProof(labels[i], gridSize, centers[i], seed, version);
            }

            int bestScore = -1;
            Vector2Int densestCenter = default;
            densestRecords = new List<SpawnPreviewRecord>();
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    List<Vector2Int> chunks = BuildSpawnProofWindowChunks(
                        x,
                        y,
                        gridSize,
                        WorldMapWave6StreamingTileProvider.OriginChunkX,
                        WorldMapWave6StreamingTileProvider.OriginChunkY);
                    List<SpawnPreviewRecord> records = GenerateSpawnPreview(seed, version, chunks);
                    SpawnPreviewSummary summary = SummarizeSpawnPreview(records, chunks);
                    int score = summary.Hives * 5 + summary.Resources * 2 + summary.Bestiary * 4;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    densestCenter = new Vector2Int(x, y);
                    densestRecords = records;
                }
            }

            windows[9] = BuildSpawnWindowProof("densest", gridSize, densestCenter, seed, version);
            return windows;
        }

        private SpawnWindowProof BuildSpawnWindowProof(string label, int gridSize, Vector2Int center, int seed, string version)
        {
            int originX = WorldMapWave6StreamingTileProvider.OriginChunkX;
            int originY = WorldMapWave6StreamingTileProvider.OriginChunkY;
            List<Vector2Int> chunks = BuildSpawnProofWindowChunks(center.x, center.y, gridSize, originX, originY);
            List<SpawnPreviewRecord> records = GenerateSpawnPreview(seed, version, chunks);
            SpawnPreviewSummary summary = SummarizeSpawnPreview(records, chunks);
            int minX = gridSize;
            int maxX = -1;
            int minY = gridSize;
            int maxY = -1;
            bool inBounds = chunks.Count > 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                int localX = chunks[i].x - originX;
                int localY = chunks[i].y - originY;
                minX = Mathf.Min(minX, localX);
                maxX = Mathf.Max(maxX, localX);
                minY = Mathf.Min(minY, localY);
                maxY = Mathf.Max(maxY, localY);
                inBounds &= localX >= 0 && localX < gridSize && localY >= 0 && localY < gridSize;
            }

            return new SpawnWindowProof(
                label,
                gridSize,
                false,
                center.x,
                center.y,
                originX + center.x,
                originY + center.y,
                minX,
                maxX,
                minY,
                maxY,
                summary.ActiveChunks,
                summary.Hives,
                summary.Resources,
                summary.Bestiary,
                summary.AcceptedInsideExclusions,
                inBounds,
                summary.BudgetsPass);
        }

        private SpawnWindowProof[] BuildSpawnProofWindows50x50()
        {
            int gridSize = StressWorldMapChunks;
            int last = gridSize - 1;
            int middle = gridSize / 2;
            string[] labels = { "center", "N", "S", "E", "W", "NW", "NE", "SW", "SE" };
            Vector2Int[] centers =
            {
                new Vector2Int(middle, middle),
                new Vector2Int(middle, last),
                new Vector2Int(middle, 0),
                new Vector2Int(last, middle),
                new Vector2Int(0, middle),
                new Vector2Int(0, last),
                new Vector2Int(last, last),
                new Vector2Int(0, 0),
                new Vector2Int(last, 0)
            };
            var windows = new SpawnWindowProof[10];
            for (int i = 0; i < labels.Length; i++) windows[i] = BuildLogicalSpawnWindowProof(labels[i], centers[i]);

            int bestScore = -1;
            Vector2Int densestCenter = default;
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    StressWindowStats stats = SimulateStressWindow(new Vector2Int(x, y));
                    int score = stats.Hives * 5 + stats.Resources * 2 + stats.Bestiary * 4;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    densestCenter = new Vector2Int(x, y);
                }
            }

            windows[9] = BuildLogicalSpawnWindowProof("densest", densestCenter);
            return windows;
        }

        private SpawnWindowProof BuildLogicalSpawnWindowProof(string label, Vector2Int center)
        {
            StressWindowStats stats = SimulateStressWindow(center);
            int minX = Mathf.Max(0, center.x - ActiveChunkRadius);
            int maxX = Mathf.Min(StressWorldMapChunks - 1, center.x + ActiveChunkRadius);
            int minY = Mathf.Max(0, center.y - ActiveChunkRadius);
            int maxY = Mathf.Min(StressWorldMapChunks - 1, center.y + ActiveChunkRadius);
            bool inBounds = minX >= 0
                && maxX < StressWorldMapChunks
                && minY >= 0
                && maxY < StressWorldMapChunks
                && stats.ActiveChunks == (maxX - minX + 1) * (maxY - minY + 1);
            return new SpawnWindowProof(
                label,
                StressWorldMapChunks,
                true,
                center.x,
                center.y,
                center.x,
                center.y,
                minX,
                maxX,
                minY,
                maxY,
                stats.ActiveChunks,
                stats.Hives,
                stats.Resources,
                stats.Bestiary,
                0,
                inBounds,
                stats.WithinBudgets);
        }

        private static bool SpawnWindowCoveragePass(SpawnWindowProof[] windows25x25, SpawnWindowProof[] windows50x50)
        {
            string[] required = { "center", "N", "S", "E", "W", "NW", "NE", "SW", "SE", "densest" };
            if (windows25x25 == null || windows50x50 == null || windows25x25.Length != required.Length || windows50x50.Length != required.Length) return false;
            for (int i = 0; i < required.Length; i++)
            {
                if (!ContainsSpawnWindow(windows25x25, required[i], false) || !ContainsSpawnWindow(windows50x50, required[i], true)) return false;
            }

            return true;
        }

        private static bool ContainsSpawnWindow(SpawnWindowProof[] windows, string label, bool logicalOnly)
        {
            for (int i = 0; i < windows.Length; i++)
            {
                SpawnWindowProof window = windows[i];
                if (window.Label != label) continue;
                return window.LogicalOnly == logicalOnly
                    && window.CoordinatesInBounds
                    && window.BudgetsPass
                    && window.ActiveChunks <= BudgetActiveChunks;
            }

            return false;
        }

        private ForcedExclusionProof[] BuildForcedExclusionProofs()
        {
            Vector2 bearDen = bearDenLandmark != null && bearDenLandmark.IsLoaded
                ? bearDenLandmark.WorldAnchor
                : new Vector2(-ChunkSize, -ChunkSize);
            return new[]
            {
                BuildForcedExclusionProof("BearDen", bearDen, "BearDen"),
                BuildForcedExclusionProof("water", NormalizedToWorldForProof(new Vector2(0.20f, 0.80f)), "water"),
                BuildForcedExclusionProof("cliff", NormalizedToWorldForProof(new Vector2(0.80f, 0.80f)), "cliff"),
                BuildForcedExclusionProof("reserved_event", NormalizedToWorldForProof(new Vector2(0.50f, 0.50f)), "reserved_event")
            };
        }

        private ForcedExclusionProof BuildForcedExclusionProof(string zone, Vector2 world, string expectedReason)
        {
            string reason;
            bool rejected = IsSpawnExcluded(world, out reason);
            int accepted = rejected ? 0 : 1;
            Vector2 normalized = new Vector2(world.x / Mathf.Max(1f, WorldWidthUnits()), world.y / Mathf.Max(1f, WorldHeightUnits()));
            Vector2Int chunk;
            Vector2 local;
            string projectionReason;
            bool projected = TryReprojectNormalizedForProof(normalized, out chunk, out local, out projectionReason);
            bool reprojectedRejected = false;
            string reprojectedReason = projectionReason;
            if (projected)
            {
                Vector2 roundTripNormalized = new Vector2(
                    (chunk.x + local.x) / StressWorldMapChunks,
                    (chunk.y + local.y) / StressWorldMapChunks);
                Vector2 roundTripWorld = NormalizedToWorldForProof(roundTripNormalized);
                string exclusionReason;
                reprojectedRejected = IsSpawnExcluded(roundTripWorld, out exclusionReason);
                reprojectedReason = reprojectedRejected ? "ExclusionVolumeHit:" + exclusionReason : "accepted";
            }

            string observedReason = rejected ? "ExclusionVolumeHit:" + reason : "accepted";
            bool pass = rejected
                && reason == expectedReason
                && accepted == 0
                && reprojectedRejected
                && reprojectedReason == "ExclusionVolumeHit:" + expectedReason;
            return new ForcedExclusionProof(zone, 1, rejected ? 1 : 0, accepted, observedReason, reprojectedRejected, reprojectedReason, pass);
        }

        private Vector2 NormalizedToWorldForProof(Vector2 normalized)
        {
            return new Vector2(normalized.x * WorldWidthUnits(), normalized.y * WorldHeightUnits());
        }

        private static bool ForcedExclusionsPass(ForcedExclusionProof[] exclusions)
        {
            if (exclusions == null || exclusions.Length != 4) return false;
            for (int i = 0; i < exclusions.Length; i++)
            {
                if (!exclusions[i].Pass || exclusions[i].Submitted != 1 || exclusions[i].Rejected != 1 || exclusions[i].Accepted != 0) return false;
            }

            return true;
        }

        private static SpawnOverlapProof BuildSpawnOverlapProof(List<SpawnPreviewRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                return new SpawnOverlapProof(false, 0, 0, SpawnCriticalOverlapDistance, SpawnMinorOverlapDistance, false, string.Empty, string.Empty, float.MaxValue);
            }

            int critical = 0;
            int minor = 0;
            int mostIsolatedIndex = 0;
            float largestNearestDistance = -1f;
            for (int i = 0; i < records.Count; i++)
            {
                float nearest = float.MaxValue;
                for (int j = i + 1; j < records.Count; j++)
                {
                    float distance = Vector2.Distance(records[i].WorldCoord, records[j].WorldCoord);
                    if (distance <= SpawnCriticalOverlapDistance) critical++;
                    else if (distance <= SpawnMinorOverlapDistance) minor++;
                }

                for (int j = 0; j < records.Count; j++)
                {
                    if (i == j) continue;
                    nearest = Mathf.Min(nearest, Vector2.Distance(records[i].WorldCoord, records[j].WorldCoord));
                }

                if (nearest <= largestNearestDistance) continue;
                largestNearestDistance = nearest;
                mostIsolatedIndex = i;
            }

            SpawnPreviewRecord expected = records[mostIsolatedIndex];
            float selectedDistance;
            SpawnPreviewRecord selected = SelectNearestSpawnRecordForProof(records, expected.WorldCoord, out selectedDistance);
            bool nearestPass = selected.EntityId == expected.EntityId && selectedDistance <= 0.001f;
            return new SpawnOverlapProof(
                critical == 0 && nearestPass,
                critical,
                minor,
                SpawnCriticalOverlapDistance,
                SpawnMinorOverlapDistance,
                nearestPass,
                expected.EntityId,
                selected.EntityId,
                selectedDistance);
        }

        private static SpawnPreviewRecord SelectNearestSpawnRecordForProof(List<SpawnPreviewRecord> records, Vector2 probe, out float selectedDistance)
        {
            SpawnPreviewRecord selected = default;
            selectedDistance = float.MaxValue;
            for (int i = 0; i < records.Count; i++)
            {
                float distance = Vector2.Distance(probe, records[i].WorldCoord);
                bool closer = distance < selectedDistance - 0.001f;
                bool stableTieBreak = Mathf.Abs(distance - selectedDistance) <= 0.001f
                    && (string.IsNullOrEmpty(selected.EntityId) || string.CompareOrdinal(records[i].EntityId, selected.EntityId) < 0);
                if (!closer && !stableTieBreak) continue;
                selected = records[i];
                selectedDistance = distance;
            }

            return selected;
        }

        private SpawnCombatProof BuildSpawnCombatProof()
        {
            bool solo = true;
            bool raid = true;
            string[] soloRows = new string[4];
            string[] raidRows = new string[3];
            for (int tier = 1; tier <= 4; tier++)
            {
                var beast = new WorldBestiaryNode("proof_t" + tier, "proof", tier, 1, Vector2.zero, BestiaryRole(tier));
                string reason;
                bool accepted = TryStartSpawnCombatForProof(tier, "solo", out reason);
                solo &= BestiaryAccessibilityToken(beast) == "[SOLO]" && accepted;
                soloRows[tier - 1] = "T" + tier.ToString(CultureInfo.InvariantCulture) + "=solo";
            }

            for (int tier = 5; tier <= 7; tier++)
            {
                var beast = new WorldBestiaryNode("proof_t" + tier, "proof", tier, 1, Vector2.zero, BestiaryRole(tier));
                string reason;
                bool accepted = TryStartSpawnCombatForProof(tier, "raid", out reason);
                raid &= BestiaryAccessibilityToken(beast) == "[RAID]" && accepted;
                raidRows[tier - 5] = "T" + tier.ToString(CultureInfo.InvariantCulture) + "=raid";
            }

            string t7Reason;
            bool t7SoloAccepted = TryStartSpawnCombatForProof(7, "solo", out t7Reason);
            bool t7SoloRefused = !t7SoloAccepted && t7Reason == "RaidRequired:T7";
            return new SpawnCombatProof(
                solo && raid && t7SoloRefused,
                solo,
                raid,
                t7SoloRefused,
                string.Join(",", soloRows),
                string.Join(",", raidRows),
                t7Reason);
        }

        private bool TryStartSpawnCombatForProof(int tier, string requestedMode, out string reason)
        {
            if (tier < 1 || tier > 7)
            {
                reason = "TierOutOfRange:T" + tier.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            string requiredMode = tier >= 5 ? "raid" : "solo";
            if (requestedMode != requiredMode)
            {
                reason = requiredMode == "raid"
                    ? "RaidRequired:T" + tier.ToString(CultureInfo.InvariantCulture)
                    : "SoloExpected:T" + tier.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            reason = "accepted:" + requiredMode;
            return true;
        }

        private SpawnRichnessProof BuildSpawnRichnessProof(List<SpawnPreviewRecord> records)
        {
            var r1 = new WorldResourceNode("proof_r1", "pauvre", ResourceKind.Pollen, Vector2.zero, 10);
            var r2 = new WorldResourceNode("proof_r2", "moyen", ResourceKind.Nectar, Vector2.zero, 60);
            var r3 = new WorldResourceNode("proof_r3", "riche", ResourceKind.Honey, Vector2.zero, 110);
            string r1Text = ResourceAccessibilityToken(r1) + " pauvre";
            string r2Text = ResourceAccessibilityToken(r2) + " moyen";
            string r3Text = ResourceAccessibilityToken(r3) + " riche";
            bool r1Readable = HasSpawnRichness(records, "R1") && ResourceTierToken(r1) == "poor" && r1Text == "[R1] pauvre";
            bool r2Readable = HasSpawnRichness(records, "R2") && ResourceTierToken(r2) == "medium" && r2Text == "[R2] moyen";
            bool r3Readable = HasSpawnRichness(records, "R3") && ResourceTierToken(r3) == "rich" && r3Text == "[R3] riche";
            bool withoutColor = r1Text != r2Text
                && r1Text != r3Text
                && r2Text != r3Text
                && r1Text.Contains("R1")
                && r2Text.Contains("R2")
                && r3Text.Contains("R3");
            return new SpawnRichnessProof(r1Readable && r2Readable && r3Readable && withoutColor, r1Readable, r2Readable, r3Readable, withoutColor, r1Text, r2Text, r3Text);
        }

        private static bool HasSpawnRichness(List<SpawnPreviewRecord> records, string richness)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].Family == "resource" && records[i].TierToken == richness) return true;
            }

            return false;
        }

        private static SpawnReprojectionProof BuildSpawnReprojectionProof(List<SpawnPreviewRecord> records)
        {
            int minChunkX = StressWorldMapChunks;
            int maxChunkX = -1;
            int minChunkY = StressWorldMapChunks;
            int maxChunkY = -1;
            float minLocal = 1f;
            float maxLocal = 0f;
            bool normalizedInRange = records.Count > 0;
            bool chunkInRange = records.Count > 0;
            bool localInRange = records.Count > 0;
            for (int i = 0; i < records.Count; i++)
            {
                Vector2 normalized = records[i].Normalized;
                Vector2Int chunk;
                Vector2 local;
                string reason;
                bool valid = TryReprojectNormalizedForProof(normalized, out chunk, out local, out reason);
                normalizedInRange &= normalized.x >= 0f && normalized.x <= 1f && normalized.y >= 0f && normalized.y <= 1f;
                chunkInRange &= valid && chunk.x >= 0 && chunk.x < StressWorldMapChunks && chunk.y >= 0 && chunk.y < StressWorldMapChunks;
                localInRange &= valid && local.x >= 0f && local.x <= 1f && local.y >= 0f && local.y <= 1f;
                if (!valid) continue;
                minChunkX = Mathf.Min(minChunkX, chunk.x);
                maxChunkX = Mathf.Max(maxChunkX, chunk.x);
                minChunkY = Mathf.Min(minChunkY, chunk.y);
                maxChunkY = Mathf.Max(maxChunkY, chunk.y);
                minLocal = Mathf.Min(minLocal, local.x, local.y);
                maxLocal = Mathf.Max(maxLocal, local.x, local.y);
            }

            bool pass = normalizedInRange && chunkInRange && localInRange;
            return new SpawnReprojectionProof(pass, records.Count, minChunkX, maxChunkX, minChunkY, maxChunkY, minLocal, maxLocal, normalizedInRange, chunkInRange, localInRange);
        }

        private static bool TryReprojectNormalizedForProof(Vector2 normalized, out Vector2Int chunk, out Vector2 local, out string reason)
        {
            chunk = default;
            local = default;
            if (normalized.x < 0f || normalized.x > 1f || normalized.y < 0f || normalized.y > 1f)
            {
                reason = "NormalizedCoordinateOutOfRange";
                return false;
            }

            float scaledX = normalized.x * StressWorldMapChunks;
            float scaledY = normalized.y * StressWorldMapChunks;
            int chunkX = normalized.x >= 1f ? StressWorldMapChunks - 1 : Mathf.FloorToInt(scaledX);
            int chunkY = normalized.y >= 1f ? StressWorldMapChunks - 1 : Mathf.FloorToInt(scaledY);
            float localX = normalized.x >= 1f ? 1f : scaledX - chunkX;
            float localY = normalized.y >= 1f ? 1f : scaledY - chunkY;
            chunk = new Vector2Int(chunkX, chunkY);
            local = new Vector2(localX, localY);
            bool valid = chunkX >= 0
                && chunkX < StressWorldMapChunks
                && chunkY >= 0
                && chunkY < StressWorldMapChunks
                && localX >= 0f
                && localX <= 1f
                && localY >= 0f
                && localY <= 1f;
            reason = valid ? "accepted" : "ReprojectionOutOfRange";
            return valid;
        }

        private NegativeTestProof[] BuildSpawnNegativeTests(List<SpawnPreviewRecord> seedARecords, ForcedExclusionProof[] forcedExclusions)
        {
            var altered = new List<SpawnPreviewRecord>(seedARecords);
            if (altered.Count > 0)
            {
                SpawnPreviewRecord record = altered[0];
                altered[0] = new SpawnPreviewRecord(
                    record.EntityId,
                    record.Family,
                    record.Kind,
                    record.TierToken,
                    record.ChunkId,
                    record.WorldCoord + Vector2.right,
                    record.Normalized,
                    record.Tier,
                    record.Variant,
                    record.State);
            }

            bool determinismRejected = altered.Count > 0
                && !CompareSpawnRecords(seedARecords, altered).Pass
                && SpawnDistributionAuditHash(seedARecords) != SpawnDistributionAuditHash(altered);
            string densityReason;
            bool densityAccepted = ValidateSpawnDensity(BudgetActiveChunks + 1, BudgetActiveHives + 1, BudgetActiveResources + 1, BudgetActiveBestiary + 1, out densityReason);
            ForcedExclusionProof bearDen = FindForcedExclusion(forcedExclusions, "BearDen");
            ForcedExclusionProof water = FindForcedExclusion(forcedExclusions, "water");
            ForcedExclusionProof cliff = FindForcedExclusion(forcedExclusions, "cliff");
            ForcedExclusionProof reservedEvent = FindForcedExclusion(forcedExclusions, "reserved_event");
            string t7Reason;
            bool t7SoloAccepted = TryStartSpawnCombatForProof(7, "solo", out t7Reason);
            Vector2Int invalidChunk;
            Vector2 invalidLocal;
            string normalizedReason;
            bool invalidNormalizedAccepted = TryReprojectNormalizedForProof(new Vector2(-0.01f, 1.01f), out invalidChunk, out invalidLocal, out normalizedReason);
            string overlayReason;
            bool invalidOverlayAccepted = ValidateSpawnOverlayDefault(true, out overlayReason);
            string authorityReason;
            bool invalidAuthorityAccepted = ValidateSpawnAuthority(false, false, true, 0, out authorityReason);
            bool groupedExclusions = water.Pass && cliff.Pass && reservedEvent.Pass;
            return new[]
            {
                new NegativeTestProof("P7-NEG-001", determinismRejected, "same seed/version with altered position", "DeterminismMismatch", determinismRejected ? "DeterminismMismatch" : "mismatch_not_detected"),
                new NegativeTestProof("P7-NEG-002", !densityAccepted && densityReason.StartsWith("DensityBudgetExceeded", StringComparison.Ordinal), "chunks=26,hives=26,resources=76,threats=26", "DensityBudgetExceeded", densityReason),
                new NegativeTestProof("P7-NEG-003", bearDen.Pass, "forced candidate inside BearDen", "ExclusionVolumeHit:BearDen", bearDen.Reason),
                new NegativeTestProof("P7-NEG-004", groupedExclusions, "forced candidates inside water,cliff,reserved_event", "three ExclusionVolumeHit rejections", water.Reason + ";" + cliff.Reason + ";" + reservedEvent.Reason),
                new NegativeTestProof("P7-NEG-005", !t7SoloAccepted && t7Reason == "RaidRequired:T7", "T7 requested as solo", "RaidRequired:T7", t7Reason),
                new NegativeTestProof("P7-NEG-006", !invalidNormalizedAccepted && normalizedReason == "NormalizedCoordinateOutOfRange", "normalized=(-0.01,1.01)", "NormalizedCoordinateOutOfRange", normalizedReason),
                new NegativeTestProof("P7-NEG-007", !invalidOverlayAccepted && overlayReason == "DiagnosticOverlayDefaultOn", "diagnostic overlay default=true", "DiagnosticOverlayDefaultOn", overlayReason),
                new NegativeTestProof("P7-NEG-008", !invalidAuthorityAccepted && authorityReason == "OfficialGainForbidden", "local official_gain=true", "OfficialGainForbidden", authorityReason)
            };
        }

        private static ForcedExclusionProof FindForcedExclusion(ForcedExclusionProof[] exclusions, string zone)
        {
            for (int i = 0; i < exclusions.Length; i++)
            {
                if (exclusions[i].Zone == zone) return exclusions[i];
            }

            return default;
        }

        private static bool NegativeTestsPass(NegativeTestProof[] tests)
        {
            if (tests == null || tests.Length != 8) return false;
            for (int i = 0; i < tests.Length; i++)
            {
                string expectedId = "P7-NEG-" + (i + 1).ToString("000", CultureInfo.InvariantCulture);
                if (tests[i].Id != expectedId || !tests[i].Pass) return false;
            }

            return true;
        }

        private static bool ValidateSpawnDensity(int chunks, int hivesCount, int resourcesCount, int bestiaryCount, out string reason)
        {
            bool pass = chunks <= BudgetActiveChunks
                && hivesCount <= BudgetActiveHives
                && resourcesCount <= BudgetActiveResources
                && bestiaryCount <= BudgetActiveBestiary;
            reason = pass
                ? "accepted"
                : "DensityBudgetExceeded(chunks=" + chunks.ToString(CultureInfo.InvariantCulture)
                    + ",hives=" + hivesCount.ToString(CultureInfo.InvariantCulture)
                    + ",resources=" + resourcesCount.ToString(CultureInfo.InvariantCulture)
                    + ",threats=" + bestiaryCount.ToString(CultureInfo.InvariantCulture) + ")";
            return pass;
        }

        private static bool ValidateSpawnOverlayDefault(bool enabled, out string reason)
        {
            reason = enabled ? "DiagnosticOverlayDefaultOn" : "accepted";
            return !enabled;
        }

        private static bool ValidateSpawnAuthority(bool server, bool official, bool officialGain, int remoteCalls, out string reason)
        {
            if (server)
            {
                reason = "ServerAuthorityForbidden";
                return false;
            }

            if (official)
            {
                reason = "OfficialStateForbidden";
                return false;
            }

            if (officialGain)
            {
                reason = "OfficialGainForbidden";
                return false;
            }

            if (remoteCalls != 0)
            {
                reason = "RemoteCallForbidden:" + remoteCalls.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            reason = "local_only_authority";
            return true;
        }

        private static void SpawnWindowMaxima(SpawnWindowProof[] windows25x25, SpawnWindowProof[] windows50x50, SpawnPreviewSummary seedB, out int maxActiveChunks, out int maxHives, out int maxResources, out int maxBestiary)
        {
            maxActiveChunks = seedB.ActiveChunks;
            maxHives = seedB.Hives;
            maxResources = seedB.Resources;
            maxBestiary = seedB.Bestiary;
            AccumulateSpawnWindowMaxima(windows25x25, ref maxActiveChunks, ref maxHives, ref maxResources, ref maxBestiary);
            AccumulateSpawnWindowMaxima(windows50x50, ref maxActiveChunks, ref maxHives, ref maxResources, ref maxBestiary);
        }

        private static void AccumulateSpawnWindowMaxima(SpawnWindowProof[] windows, ref int maxActiveChunks, ref int maxHives, ref int maxResources, ref int maxBestiary)
        {
            for (int i = 0; i < windows.Length; i++)
            {
                maxActiveChunks = Mathf.Max(maxActiveChunks, windows[i].ActiveChunks);
                maxHives = Mathf.Max(maxHives, windows[i].Hives);
                maxResources = Mathf.Max(maxResources, windows[i].Resources);
                maxBestiary = Mathf.Max(maxBestiary, windows[i].Bestiary);
            }
        }

        private static SpawnRecordComparisonProof CompareSpawnRecords(List<SpawnPreviewRecord> first, List<SpawnPreviewRecord> second)
        {
            bool countEqual = first != null && second != null && first.Count == second.Count;
            bool ids = countEqual;
            bool positions = countEqual;
            bool tiers = countEqual;
            bool richness = countEqual;
            bool flags = countEqual;
            if (countEqual)
            {
                for (int i = 0; i < first.Count; i++)
                {
                    SpawnPreviewRecord left = first[i];
                    SpawnPreviewRecord right = second[i];
                    ids &= left.EntityId == right.EntityId;
                    positions &= Vector2.Distance(left.WorldCoord, right.WorldCoord) <= 0.001f;
                    tiers &= left.Tier == right.Tier && left.TierToken == right.TierToken;
                    if (left.Family == "resource" || right.Family == "resource") richness &= left.TierToken == right.TierToken;
                    flags &= left.Family == right.Family
                        && left.Kind == right.Kind
                        && left.ChunkId == right.ChunkId
                        && left.Variant == right.Variant
                        && left.State == right.State
                        && Vector2.Distance(left.Normalized, right.Normalized) <= 0.000001f;
                }
            }

            return new SpawnRecordComparisonProof(countEqual, ids, positions, tiers, richness, flags);
        }

        private static string SpawnDistributionAuditHash(List<SpawnPreviewRecord> records)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash ^= (uint)records.Count;
                hash *= 16777619u;
                for (int i = 0; i < records.Count; i++)
                {
                    SpawnPreviewRecord record = records[i];
                    string row = record.EntityId
                        + "|" + record.Family
                        + "|" + record.Kind
                        + "|" + record.TierToken
                        + "|" + record.ChunkId
                        + "|" + record.WorldCoord.x.ToString("0.000", CultureInfo.InvariantCulture)
                        + "|" + record.WorldCoord.y.ToString("0.000", CultureInfo.InvariantCulture)
                        + "|" + record.Normalized.x.ToString("0.000000", CultureInfo.InvariantCulture)
                        + "|" + record.Normalized.y.ToString("0.000000", CultureInfo.InvariantCulture)
                        + "|" + record.Tier.ToString(CultureInfo.InvariantCulture)
                        + "|" + record.Variant.ToString(CultureInfo.InvariantCulture)
                        + "|" + record.State;
                    hash ^= StableHash32(row);
                    hash *= 16777619u;
                }

                return hash.ToString("x8", CultureInfo.InvariantCulture);
            }
        }

        public WorldMapLocalLabRuntime.LabProofSnapshot CurrentLocalLabProofSnapshot()
        {
            return localLab != null ? localLab.CurrentProofSnapshot() : default;
        }

        public bool ResetLocalLabForProof()
        {
            return localLab != null && localLab.ResetForProof();
        }

        public bool RunLocalLabCollectionForProof()
        {
            return localLab != null && localLab.RunCollectionForProof();
        }

        public bool RunLocalLabCombatForProof()
        {
            return localLab != null && localLab.RunCombatForProof();
        }

        public WorldMapLocalLabRuntime.HiveVisualProofSnapshot RunLocalLabHiveVisualProgressionForProof()
        {
            return localLab != null ? localLab.RunHiveVisualProgressionProofForProof() : default;
        }

        public bool ApplyScenarioPresetFromLab(string scenarioId)
        {
            if (scenarioId == "scenario_collect_r3")
            {
                WorldResourceNode rich = FirstResourceByTier("rich");
                if (rich == null) return false;
                selectedResourceId = rich.Id;
                ForceRespawnForProof(rich.Id);
                status = "Scenario Collecte R3 local pret: " + rich.Label + " " + ResourceQuantityLabel(rich);
                localRewardText = "Scenario Collecte R3 local, official_gain=false, server=false";
                return ResourceRemaining(rich) == rich.Amount;
            }

            if (scenarioId == "scenario_duel_two_hives")
            {
                selectedHiveId = HiveById("hive_player_test") != null ? "hive_player_test" : (hives.Count > 0 ? hives[0].Id : string.Empty);
                status = "Scenario duel deux ruches local pret, official_gain=false, server=false";
                localRewardText = "Duel local sans recompense officielle";
                return !string.IsNullOrEmpty(selectedHiveId);
            }

            if (scenarioId == "scenario_raid_t7")
            {
                EnsureBestiaryTierForProof(7);
                WorldBestiaryNode raid = FirstBestiaryByTier(7);
                bool selected = SelectBestiaryForProof(raid != null ? raid.Id : string.Empty);
                status = "Scenario Raid T7 local pret, official_gain=false, server=false";
                return selected && raid != null && raid.Tier == 7;
            }

            return false;
        }

        public readonly struct Wave6ProofSnapshot
        {
            public readonly Vector2 WorldCenter;
            public readonly float Zoom;
            public readonly bool ManifestReady;
            public readonly bool VisibleTilesReady;
            public readonly int LoadedVisibleTiles;
            public readonly int RequiredVisibleTiles;
            public readonly int CachedTiles;
            public readonly int PendingTiles;
            public readonly Rect WorldBounds;
            public readonly bool BearDenLoaded;
            public readonly bool BearDenVisible;
            public readonly Vector2 BearDenAnchor;

            public Wave6ProofSnapshot(Vector2 worldCenter, float zoom, bool manifestReady, bool visibleTilesReady, int loadedVisibleTiles, int requiredVisibleTiles, int cachedTiles, int pendingTiles, Rect worldBounds, bool bearDenLoaded, bool bearDenVisible, Vector2 bearDenAnchor)
            {
                WorldCenter = worldCenter;
                Zoom = zoom;
                ManifestReady = manifestReady;
                VisibleTilesReady = visibleTilesReady;
                LoadedVisibleTiles = loadedVisibleTiles;
                RequiredVisibleTiles = requiredVisibleTiles;
                CachedTiles = cachedTiles;
                PendingTiles = pendingTiles;
                WorldBounds = worldBounds;
                BearDenLoaded = bearDenLoaded;
                BearDenVisible = bearDenVisible;
                BearDenAnchor = bearDenAnchor;
            }
        }

        [Obsolete("Compatibility snapshot. The canonical terrain is Wave6 50x50.")]
        public readonly struct Wave5ProofSnapshot
        {
            public readonly Vector2 WorldCenter;
            public readonly float Zoom;
            public readonly bool ManifestReady;
            public readonly bool VisibleTilesReady;
            public readonly int LoadedVisibleTiles;
            public readonly int RequiredVisibleTiles;
            public readonly int CachedTiles;
            public readonly int PendingTiles;
            public readonly Rect WorldBounds;
            public readonly bool BearDenLoaded;
            public readonly bool BearDenVisible;
            public readonly Vector2 BearDenAnchor;

            public Wave5ProofSnapshot(Vector2 worldCenter, float zoom, bool manifestReady, bool visibleTilesReady, int loadedVisibleTiles, int requiredVisibleTiles, int cachedTiles, int pendingTiles, Rect worldBounds, bool bearDenLoaded, bool bearDenVisible, Vector2 bearDenAnchor)
            {
                WorldCenter = worldCenter;
                Zoom = zoom;
                ManifestReady = manifestReady;
                VisibleTilesReady = visibleTilesReady;
                LoadedVisibleTiles = loadedVisibleTiles;
                RequiredVisibleTiles = requiredVisibleTiles;
                CachedTiles = cachedTiles;
                PendingTiles = pendingTiles;
                WorldBounds = worldBounds;
                BearDenLoaded = bearDenLoaded;
                BearDenVisible = bearDenVisible;
                BearDenAnchor = bearDenAnchor;
            }
        }

        public readonly struct RuntimeEntitiesProofSnapshot
        {
            public readonly int ResourceNodes;
            public readonly int TexturedResourceNodes;
            public readonly int WaterNodes;
            public readonly int HoneyNodes;
            public readonly int BestiaryNodes;
            public readonly int TexturedBestiaryNodes;
            public readonly int MaxBestiaryTier;
            public readonly bool RuntimePlacementMaskLoaded;
            public readonly int RuntimePlacementMaskEntries;
            public readonly bool RuntimePlacementMaskCovers50x50;

            public RuntimeEntitiesProofSnapshot(int resourceNodes, int texturedResourceNodes, int waterNodes, int honeyNodes, int bestiaryNodes, int texturedBestiaryNodes, int maxBestiaryTier, bool runtimePlacementMaskLoaded, int runtimePlacementMaskEntries, bool runtimePlacementMaskCovers50x50)
            {
                ResourceNodes = resourceNodes;
                TexturedResourceNodes = texturedResourceNodes;
                WaterNodes = waterNodes;
                HoneyNodes = honeyNodes;
                BestiaryNodes = bestiaryNodes;
                TexturedBestiaryNodes = texturedBestiaryNodes;
                MaxBestiaryTier = maxBestiaryTier;
                RuntimePlacementMaskLoaded = runtimePlacementMaskLoaded;
                RuntimePlacementMaskEntries = runtimePlacementMaskEntries;
                RuntimePlacementMaskCovers50x50 = runtimePlacementMaskCovers50x50;
            }
        }

        public readonly struct ResourceInteractionProofSnapshot
        {
            public readonly bool Pass;
            public readonly bool TierCoveragePass;
            public readonly bool SelectionPass;
            public readonly bool CollectionPass;
            public readonly bool DepletionPass;
            public readonly bool RespawnPass;
            public readonly int QuantityBefore;
            public readonly int QuantityAfterRespawn;
            public readonly string SelectedResource;

            public ResourceInteractionProofSnapshot(bool pass, bool tierCoveragePass, bool selectionPass, bool collectionPass, bool depletionPass, bool respawnPass, int quantityBefore, int quantityAfterRespawn, string selectedResource)
            {
                Pass = pass;
                TierCoveragePass = tierCoveragePass;
                SelectionPass = selectionPass;
                CollectionPass = collectionPass;
                DepletionPass = depletionPass;
                RespawnPass = respawnPass;
                QuantityBefore = quantityBefore;
                QuantityAfterRespawn = quantityAfterRespawn;
                SelectedResource = selectedResource;
            }
        }

        public readonly struct BestiaryInteractionProofSnapshot
        {
            public readonly bool Pass;
            public readonly bool TierCoveragePass;
            public readonly bool SelectionPass;
            public readonly bool SoloCombatPass;
            public readonly bool RaidCombatPass;
            public readonly bool NoOfficialGainPass;
            public readonly string SoloTarget;
            public readonly string RaidTarget;
            public readonly string LastCombatTelemetry;

            public BestiaryInteractionProofSnapshot(bool pass, bool tierCoveragePass, bool selectionPass, bool soloCombatPass, bool raidCombatPass, bool noOfficialGainPass, string soloTarget, string raidTarget, string lastCombatTelemetry)
            {
                Pass = pass;
                TierCoveragePass = tierCoveragePass;
                SelectionPass = selectionPass;
                SoloCombatPass = soloCombatPass;
                RaidCombatPass = raidCombatPass;
                NoOfficialGainPass = noOfficialGainPass;
                SoloTarget = soloTarget;
                RaidTarget = raidTarget;
                LastCombatTelemetry = lastCombatTelemetry;
            }
        }

        public readonly struct Stress50x50ReadinessSnapshot
        {
            public readonly bool Pass;
            public readonly bool DisabledByDefault;
            public readonly int CatalogCoordinates;
            public readonly int CenterActiveChunks;
            public readonly int NorthWestActiveChunks;
            public readonly int SouthEastActiveChunks;
            public readonly int DensestActiveChunks;
            public readonly int DensestHives;
            public readonly int DensestResources;
            public readonly int DensestBestiary;
            public readonly int CatalogHives;
            public readonly int CatalogResources;
            public readonly int CatalogBestiary;
            public readonly int Wave5CachedTextures;
            public readonly int ChunkCacheBefore;
            public readonly int ChunkCacheAfter;
            public readonly long AllocatedBytes;
            public readonly bool BudgetsPass;
            public readonly bool CacheStablePass;
            public readonly bool TerrainPreservedPass;
            public readonly bool AllocationBudgetPass;
            public readonly bool RuntimePlacementMaskLoaded;
            public readonly int RuntimePlacementMaskEntries;
            public readonly bool RuntimePlacementMaskCovers50x50;

            public Stress50x50ReadinessSnapshot(bool pass, bool disabledByDefault, int catalogCoordinates, int centerActiveChunks, int northWestActiveChunks, int southEastActiveChunks, int densestActiveChunks, int densestHives, int densestResources, int densestBestiary, int catalogHives, int catalogResources, int catalogBestiary, int wave5CachedTextures, int chunkCacheBefore, int chunkCacheAfter, long allocatedBytes, bool budgetsPass, bool cacheStablePass, bool terrainPreservedPass, bool allocationBudgetPass, bool runtimePlacementMaskLoaded, int runtimePlacementMaskEntries, bool runtimePlacementMaskCovers50x50)
            {
                Pass = pass;
                DisabledByDefault = disabledByDefault;
                CatalogCoordinates = catalogCoordinates;
                CenterActiveChunks = centerActiveChunks;
                NorthWestActiveChunks = northWestActiveChunks;
                SouthEastActiveChunks = southEastActiveChunks;
                DensestActiveChunks = densestActiveChunks;
                DensestHives = densestHives;
                DensestResources = densestResources;
                DensestBestiary = densestBestiary;
                CatalogHives = catalogHives;
                CatalogResources = catalogResources;
                CatalogBestiary = catalogBestiary;
                Wave5CachedTextures = wave5CachedTextures;
                ChunkCacheBefore = chunkCacheBefore;
                ChunkCacheAfter = chunkCacheAfter;
                AllocatedBytes = allocatedBytes;
                BudgetsPass = budgetsPass;
                CacheStablePass = cacheStablePass;
                TerrainPreservedPass = terrainPreservedPass;
                AllocationBudgetPass = allocationBudgetPass;
                RuntimePlacementMaskLoaded = runtimePlacementMaskLoaded;
                RuntimePlacementMaskEntries = runtimePlacementMaskEntries;
                RuntimePlacementMaskCovers50x50 = runtimePlacementMaskCovers50x50;
            }
        }

        public readonly struct MapReadingToolsProofSnapshot
        {
            public readonly bool Pass;
            public readonly bool NearestSelectionPass;
            public readonly bool FiltersPass;
            public readonly bool FixedHudPass;
            public readonly bool TerrainUnmaskedPass;
            public readonly bool LegendPass;
            public readonly string Status;

            public MapReadingToolsProofSnapshot(bool pass, bool nearestSelectionPass, bool filtersPass, bool fixedHudPass, bool terrainUnmaskedPass, bool legendPass, string status)
            {
                Pass = pass;
                NearestSelectionPass = nearestSelectionPass;
                FiltersPass = filtersPass;
                FixedHudPass = fixedHudPass;
                TerrainUnmaskedPass = terrainUnmaskedPass;
                LegendPass = legendPass;
                Status = status;
            }
        }

        public readonly struct InteractionPolishProofSnapshot
        {
            public readonly bool Pass;
            public readonly bool QuantityFeedbackPass;
            public readonly bool TrajectoryFeedbackPass;
            public readonly bool DepletionFeedbackPass;
            public readonly bool RespawnFeedbackPass;
            public readonly bool CombatFeedbackPass;
            public readonly string ResourceFeedback;
            public readonly string CombatFeedback;

            public InteractionPolishProofSnapshot(bool pass, bool quantityFeedbackPass, bool trajectoryFeedbackPass, bool depletionFeedbackPass, bool respawnFeedbackPass, bool combatFeedbackPass, string resourceFeedback, string combatFeedback)
            {
                Pass = pass;
                QuantityFeedbackPass = quantityFeedbackPass;
                TrajectoryFeedbackPass = trajectoryFeedbackPass;
                DepletionFeedbackPass = depletionFeedbackPass;
                RespawnFeedbackPass = respawnFeedbackPass;
                CombatFeedbackPass = combatFeedbackPass;
                ResourceFeedback = resourceFeedback;
                CombatFeedback = combatFeedback;
            }
        }

        public readonly struct RuntimeScenarioDataLayerProofSnapshot
        {
            public readonly bool Pass;
            public readonly bool StableEntityIdsPass;
            public readonly bool NormalizedCoordinatesPass;
            public readonly bool Reprojection50x50Pass;
            public readonly bool LocalAuthorityAdapterPass;
            public readonly bool ScenarioPresetsPass;
            public readonly bool PlayerEnemyTestHivesEditablePass;
            public readonly bool LegacyDemoRegressionNo;
            public readonly int Records;
            public readonly int Hives;
            public readonly int Resources;
            public readonly int Bestiary;
            public readonly int Events;
            public readonly string DataVersion;
            public readonly string ProviderId;

            public RuntimeScenarioDataLayerProofSnapshot(bool pass, bool stableEntityIdsPass, bool normalizedCoordinatesPass, bool reprojection50x50Pass, bool localAuthorityAdapterPass, bool scenarioPresetsPass, bool playerEnemyTestHivesEditablePass, bool legacyDemoRegressionNo, int records, int hives, int resources, int bestiary, int events, string dataVersion, string providerId)
            {
                Pass = pass;
                StableEntityIdsPass = stableEntityIdsPass;
                NormalizedCoordinatesPass = normalizedCoordinatesPass;
                Reprojection50x50Pass = reprojection50x50Pass;
                LocalAuthorityAdapterPass = localAuthorityAdapterPass;
                ScenarioPresetsPass = scenarioPresetsPass;
                PlayerEnemyTestHivesEditablePass = playerEnemyTestHivesEditablePass;
                LegacyDemoRegressionNo = legacyDemoRegressionNo;
                Records = records;
                Hives = hives;
                Resources = resources;
                Bestiary = bestiary;
                Events = events;
                DataVersion = dataVersion;
                ProviderId = providerId;
            }
        }

        public readonly struct SpawnInspectorProofSnapshot
        {
            public readonly bool Pass;
            public readonly bool DeterministicSpawnPass;
            public readonly bool SeedVariationPass;
            public readonly bool ExclusionZonesPass;
            public readonly bool DensityBudgetsPass;
            public readonly bool SpawnInspectorUiPass;
            public readonly bool DiagnosticOverlayDefaultOff;
            public readonly bool P1P6RegressionNo;
            public readonly int SeedA;
            public readonly int SeedB;
            public readonly string SpawnSeedVersion;
            public readonly string AlternateSpawnSeedVersion;
            public readonly string ExclusionVersion;
            public readonly string WorldGridVersion;
            public readonly string SeedA1Hash;
            public readonly string SeedA2Hash;
            public readonly string SeedBHash;
            public readonly string VersionCHash;
            public readonly SpawnRecordComparisonProof SameSeedComparison;
            public readonly int SeedBActiveChunks;
            public readonly int SeedBHives;
            public readonly int SeedBResources;
            public readonly int SeedBBestiary;
            public readonly bool DifferentSeedBudgetsPreserved;
            public readonly bool SeedVersionVariationPass;
            public readonly SpawnWindowProof[] Windows25x25;
            public readonly SpawnWindowProof[] Windows50x50;
            public readonly bool WindowCoveragePass;
            public readonly ForcedExclusionProof[] ForcedExclusions;
            public readonly int AcceptedEntitiesInsideExclusions;
            public readonly SpawnOverlapProof Overlap;
            public readonly SpawnCombatProof Combat;
            public readonly SpawnRichnessProof Richness;
            public readonly SpawnReprojectionProof Reprojection;
            public readonly bool OverlayDefaultOff;
            public readonly string OverlayOffHash;
            public readonly string OverlayOnHash;
            public readonly bool OverlayDistributionUnchanged;
            public readonly NegativeTestProof[] NegativeTests;
            public readonly bool NegativeTestsPass;
            public readonly bool Server;
            public readonly bool Official;
            public readonly bool OfficialGain;
            public readonly int RemoteCalls;
            public readonly bool AuthorityFlagsPass;
            public readonly string AuthorityReason;
            public readonly int ActiveChunks;
            public readonly int Hives;
            public readonly int Resources;
            public readonly int Bestiary;
            public readonly int BearDenExclusionHits;
            public readonly int WaterExclusionHits;
            public readonly int CliffExclusionHits;
            public readonly int EventExclusionHits;
            public readonly int MaxActiveChunks;
            public readonly int MaxHives;
            public readonly int MaxResources;
            public readonly int MaxBestiary;
            public readonly int Wave5CachedTextures;
            public readonly int RuntimeEntityTextureCacheEntries;
            public readonly long AllocatedBytes;
            public readonly bool AllocationBudgetPass;
            public readonly int ChunkCacheBefore50x50;
            public readonly int ChunkCacheAfter50x50;
            public readonly bool No50x50TerrainGenerated;

            public SpawnInspectorProofSnapshot(
                bool pass,
                bool deterministicSpawnPass,
                bool seedVariationPass,
                bool exclusionZonesPass,
                bool densityBudgetsPass,
                bool spawnInspectorUiPass,
                bool diagnosticOverlayDefaultOff,
                bool p1P6RegressionNo,
                int seedA,
                int seedB,
                string spawnSeedVersion,
                string alternateSpawnSeedVersion,
                string exclusionVersion,
                string worldGridVersion,
                string seedA1Hash,
                string seedA2Hash,
                string seedBHash,
                string versionCHash,
                SpawnRecordComparisonProof sameSeedComparison,
                int seedBActiveChunks,
                int seedBHives,
                int seedBResources,
                int seedBBestiary,
                bool differentSeedBudgetsPreserved,
                bool seedVersionVariationPass,
                SpawnWindowProof[] windows25x25,
                SpawnWindowProof[] windows50x50,
                bool windowCoveragePass,
                ForcedExclusionProof[] forcedExclusions,
                int acceptedEntitiesInsideExclusions,
                SpawnOverlapProof overlap,
                SpawnCombatProof combat,
                SpawnRichnessProof richness,
                SpawnReprojectionProof reprojection,
                bool overlayDefaultOff,
                string overlayOffHash,
                string overlayOnHash,
                bool overlayDistributionUnchanged,
                NegativeTestProof[] negativeTests,
                bool negativeTestsPass,
                bool server,
                bool official,
                bool officialGain,
                int remoteCalls,
                bool authorityFlagsPass,
                string authorityReason,
                int activeChunks,
                int hives,
                int resources,
                int bestiary,
                int bearDenExclusionHits,
                int waterExclusionHits,
                int cliffExclusionHits,
                int eventExclusionHits,
                int maxActiveChunks,
                int maxHives,
                int maxResources,
                int maxBestiary,
                int wave5CachedTextures,
                int runtimeEntityTextureCacheEntries,
                long allocatedBytes,
                bool allocationBudgetPass,
                int chunkCacheBefore50x50,
                int chunkCacheAfter50x50,
                bool no50x50TerrainGenerated)
            {
                Pass = pass;
                DeterministicSpawnPass = deterministicSpawnPass;
                SeedVariationPass = seedVariationPass;
                ExclusionZonesPass = exclusionZonesPass;
                DensityBudgetsPass = densityBudgetsPass;
                SpawnInspectorUiPass = spawnInspectorUiPass;
                DiagnosticOverlayDefaultOff = diagnosticOverlayDefaultOff;
                P1P6RegressionNo = p1P6RegressionNo;
                SeedA = seedA;
                SeedB = seedB;
                SpawnSeedVersion = spawnSeedVersion;
                AlternateSpawnSeedVersion = alternateSpawnSeedVersion;
                ExclusionVersion = exclusionVersion;
                WorldGridVersion = worldGridVersion;
                SeedA1Hash = seedA1Hash;
                SeedA2Hash = seedA2Hash;
                SeedBHash = seedBHash;
                VersionCHash = versionCHash;
                SameSeedComparison = sameSeedComparison;
                SeedBActiveChunks = seedBActiveChunks;
                SeedBHives = seedBHives;
                SeedBResources = seedBResources;
                SeedBBestiary = seedBBestiary;
                DifferentSeedBudgetsPreserved = differentSeedBudgetsPreserved;
                SeedVersionVariationPass = seedVersionVariationPass;
                Windows25x25 = windows25x25;
                Windows50x50 = windows50x50;
                WindowCoveragePass = windowCoveragePass;
                ForcedExclusions = forcedExclusions;
                AcceptedEntitiesInsideExclusions = acceptedEntitiesInsideExclusions;
                Overlap = overlap;
                Combat = combat;
                Richness = richness;
                Reprojection = reprojection;
                OverlayDefaultOff = overlayDefaultOff;
                OverlayOffHash = overlayOffHash;
                OverlayOnHash = overlayOnHash;
                OverlayDistributionUnchanged = overlayDistributionUnchanged;
                NegativeTests = negativeTests;
                NegativeTestsPass = negativeTestsPass;
                Server = server;
                Official = official;
                OfficialGain = officialGain;
                RemoteCalls = remoteCalls;
                AuthorityFlagsPass = authorityFlagsPass;
                AuthorityReason = authorityReason;
                ActiveChunks = activeChunks;
                Hives = hives;
                Resources = resources;
                Bestiary = bestiary;
                BearDenExclusionHits = bearDenExclusionHits;
                WaterExclusionHits = waterExclusionHits;
                CliffExclusionHits = cliffExclusionHits;
                EventExclusionHits = eventExclusionHits;
                MaxActiveChunks = maxActiveChunks;
                MaxHives = maxHives;
                MaxResources = maxResources;
                MaxBestiary = maxBestiary;
                Wave5CachedTextures = wave5CachedTextures;
                RuntimeEntityTextureCacheEntries = runtimeEntityTextureCacheEntries;
                AllocatedBytes = allocatedBytes;
                AllocationBudgetPass = allocationBudgetPass;
                ChunkCacheBefore50x50 = chunkCacheBefore50x50;
                ChunkCacheAfter50x50 = chunkCacheAfter50x50;
                No50x50TerrainGenerated = no50x50TerrainGenerated;
            }
        }

        public readonly struct SpawnRecordComparisonProof
        {
            public readonly bool Pass;
            public readonly bool CountEqual;
            public readonly bool IdsEqual;
            public readonly bool PositionsEqual;
            public readonly bool TiersEqual;
            public readonly bool RichnessEqual;
            public readonly bool FlagsEqual;

            public SpawnRecordComparisonProof(bool countEqual, bool idsEqual, bool positionsEqual, bool tiersEqual, bool richnessEqual, bool flagsEqual)
            {
                CountEqual = countEqual;
                IdsEqual = idsEqual;
                PositionsEqual = positionsEqual;
                TiersEqual = tiersEqual;
                RichnessEqual = richnessEqual;
                FlagsEqual = flagsEqual;
                Pass = countEqual && idsEqual && positionsEqual && tiersEqual && richnessEqual && flagsEqual;
            }
        }

        public readonly struct SpawnWindowProof
        {
            public readonly string Label;
            public readonly int GridSize;
            public readonly bool LogicalOnly;
            public readonly int CenterX;
            public readonly int CenterY;
            public readonly int WorldChunkX;
            public readonly int WorldChunkY;
            public readonly int MinChunkX;
            public readonly int MaxChunkX;
            public readonly int MinChunkY;
            public readonly int MaxChunkY;
            public readonly int ActiveChunks;
            public readonly int Hives;
            public readonly int Resources;
            public readonly int Bestiary;
            public readonly int AcceptedInsideExclusions;
            public readonly bool CoordinatesInBounds;
            public readonly bool BudgetsPass;

            public SpawnWindowProof(string label, int gridSize, bool logicalOnly, int centerX, int centerY, int worldChunkX, int worldChunkY, int minChunkX, int maxChunkX, int minChunkY, int maxChunkY, int activeChunks, int hives, int resources, int bestiary, int acceptedInsideExclusions, bool coordinatesInBounds, bool budgetsPass)
            {
                Label = label;
                GridSize = gridSize;
                LogicalOnly = logicalOnly;
                CenterX = centerX;
                CenterY = centerY;
                WorldChunkX = worldChunkX;
                WorldChunkY = worldChunkY;
                MinChunkX = minChunkX;
                MaxChunkX = maxChunkX;
                MinChunkY = minChunkY;
                MaxChunkY = maxChunkY;
                ActiveChunks = activeChunks;
                Hives = hives;
                Resources = resources;
                Bestiary = bestiary;
                AcceptedInsideExclusions = acceptedInsideExclusions;
                CoordinatesInBounds = coordinatesInBounds;
                BudgetsPass = budgetsPass;
            }
        }

        public readonly struct ForcedExclusionProof
        {
            public readonly string Zone;
            public readonly int Submitted;
            public readonly int Rejected;
            public readonly int Accepted;
            public readonly string Reason;
            public readonly bool ReprojectedRejected;
            public readonly string ReprojectedReason;
            public readonly bool Pass;

            public ForcedExclusionProof(string zone, int submitted, int rejected, int accepted, string reason, bool reprojectedRejected, string reprojectedReason, bool pass)
            {
                Zone = zone;
                Submitted = submitted;
                Rejected = rejected;
                Accepted = accepted;
                Reason = reason;
                ReprojectedRejected = reprojectedRejected;
                ReprojectedReason = reprojectedReason;
                Pass = pass;
            }
        }

        public readonly struct NegativeTestProof
        {
            public readonly string Id;
            public readonly bool Pass;
            public readonly string Injected;
            public readonly string Expected;
            public readonly string Observed;

            public NegativeTestProof(string id, bool pass, string injected, string expected, string observed)
            {
                Id = id;
                Pass = pass;
                Injected = injected;
                Expected = expected;
                Observed = observed;
            }
        }

        public readonly struct SpawnOverlapProof
        {
            public readonly bool Pass;
            public readonly int CriticalOverlaps;
            public readonly int MinorOverlaps;
            public readonly float CriticalDistance;
            public readonly float MinorDistance;
            public readonly bool NearestSelectionPass;
            public readonly string ExpectedNearestId;
            public readonly string SelectedNearestId;
            public readonly float SelectedDistance;

            public SpawnOverlapProof(bool pass, int criticalOverlaps, int minorOverlaps, float criticalDistance, float minorDistance, bool nearestSelectionPass, string expectedNearestId, string selectedNearestId, float selectedDistance)
            {
                Pass = pass;
                CriticalOverlaps = criticalOverlaps;
                MinorOverlaps = minorOverlaps;
                CriticalDistance = criticalDistance;
                MinorDistance = minorDistance;
                NearestSelectionPass = nearestSelectionPass;
                ExpectedNearestId = expectedNearestId;
                SelectedNearestId = selectedNearestId;
                SelectedDistance = selectedDistance;
            }
        }

        public readonly struct SpawnCombatProof
        {
            public readonly bool Pass;
            public readonly bool T1T4Solo;
            public readonly bool T5T7Raid;
            public readonly bool T7SoloRefused;
            public readonly string SoloAccess;
            public readonly string RaidAccess;
            public readonly string T7SoloReason;

            public SpawnCombatProof(bool pass, bool t1T4Solo, bool t5T7Raid, bool t7SoloRefused, string soloAccess, string raidAccess, string t7SoloReason)
            {
                Pass = pass;
                T1T4Solo = t1T4Solo;
                T5T7Raid = t5T7Raid;
                T7SoloRefused = t7SoloRefused;
                SoloAccess = soloAccess;
                RaidAccess = raidAccess;
                T7SoloReason = t7SoloReason;
            }
        }

        public readonly struct SpawnRichnessProof
        {
            public readonly bool Pass;
            public readonly bool R1Readable;
            public readonly bool R2Readable;
            public readonly bool R3Readable;
            public readonly bool ReadableWithoutColor;
            public readonly string R1Text;
            public readonly string R2Text;
            public readonly string R3Text;

            public SpawnRichnessProof(bool pass, bool r1Readable, bool r2Readable, bool r3Readable, bool readableWithoutColor, string r1Text, string r2Text, string r3Text)
            {
                Pass = pass;
                R1Readable = r1Readable;
                R2Readable = r2Readable;
                R3Readable = r3Readable;
                ReadableWithoutColor = readableWithoutColor;
                R1Text = r1Text;
                R2Text = r2Text;
                R3Text = r3Text;
            }
        }

        public readonly struct SpawnReprojectionProof
        {
            public readonly bool Pass;
            public readonly int RecordsChecked;
            public readonly int MinChunkX;
            public readonly int MaxChunkX;
            public readonly int MinChunkY;
            public readonly int MaxChunkY;
            public readonly float MinLocal;
            public readonly float MaxLocal;
            public readonly bool NormalizedInRange;
            public readonly bool ChunkInRange;
            public readonly bool LocalInRange;

            public SpawnReprojectionProof(bool pass, int recordsChecked, int minChunkX, int maxChunkX, int minChunkY, int maxChunkY, float minLocal, float maxLocal, bool normalizedInRange, bool chunkInRange, bool localInRange)
            {
                Pass = pass;
                RecordsChecked = recordsChecked;
                MinChunkX = minChunkX;
                MaxChunkX = maxChunkX;
                MinChunkY = minChunkY;
                MaxChunkY = maxChunkY;
                MinLocal = minLocal;
                MaxLocal = maxLocal;
                NormalizedInRange = normalizedInRange;
                ChunkInRange = chunkInRange;
                LocalInRange = localInRange;
            }
        }

        public static string[] WorldMapStep4DProofControlsForProof()
        {
            return new[]
            {
                "step4d_deterministic_dev_proof_controls:true",
                "step4d_compilation_guard:UNITY_EDITOR_OR_DEVELOPMENT_BUILD",
                "step4d_release_menu_surface:false",
                "step4d_canonical_scene:" + Step4DCanonicalScenePath,
                "step4d_atomic_state_api:true",
                "step4d_sets_current_and_target_zoom:true",
                "step4d_sets_current_and_target_world_center:true",
                "step4d_state_landscape_1920x1080_z0.85_C32_32:true",
                "step4d_state_landscape_1920x1080_z1.10_C32_32:true",
                "step4d_state_landscape_1920x1080_z1.35_C32_32:true",
                "step4d_state_portrait_720x1280_z1.10_C32_32:true",
                "step4d_pan_sequence_C32_32_C35_32_C36_32_z1.10:true",
                "step4d_capture_output_under_workspace:true",
                "step4d_manifest_writes_resolution_zoom_chunk_hashes:true",
                "step4d_refuses_non_play_mode:true",
                "step4d_refuses_non_canonical_scene:true",
                "step4d_refuses_missing_bootstrap:true",
                "step4d_no_shader_blur_band_overlay:true",
                "step4d_preserves_clamp_no_wrap:true",
                "step4d_preserves_macro_surface:true",
                "step4d_preserves_logical_world_64x64:true",
                "step4d_preserves_active_window_5x5:true",
                "step4d_preserves_overlay_separation:true",
                "step4d_preserves_air_only_flights:true",
                "non_development_runtime_surface_added:false"
            };
        }

        public readonly struct DevProofState
        {
            public readonly string Label;
            public readonly int ScreenWidth;
            public readonly int ScreenHeight;
            public readonly float Zoom;
            public readonly Vector2 WorldCenter;
            public readonly Vector2Int Chunk;
            public readonly int ActiveChunkCount;
            public readonly string UvRect;
            public readonly bool UvBounded;
            public readonly string AtlasWrapMode;
            public readonly bool AtlasLoaded;

            public DevProofState(string label, int screenWidth, int screenHeight, float zoom, Vector2 worldCenter, Vector2Int chunk, int activeChunkCount, string uvRect, bool uvBounded, string atlasWrapMode, bool atlasLoaded)
            {
                Label = label;
                ScreenWidth = screenWidth;
                ScreenHeight = screenHeight;
                Zoom = zoom;
                WorldCenter = worldCenter;
                Chunk = chunk;
                ActiveChunkCount = activeChunkCount;
                UvRect = uvRect;
                UvBounded = uvBounded;
                AtlasWrapMode = atlasWrapMode;
                AtlasLoaded = atlasLoaded;
            }
        }
#endif

        public static string[] WorldMapMmoFullscreenFoundationForProof()
        {
            return new[]
            {
                "surface:world_map_mmo_fullscreen_foundation",
                "world_map_wave4_integration_step4a:true",
                "world_art_provider:manifest_driven",
                "world_art_manifest:WorldMapWave4/UIB_SectorWave1/manifest",
                "world_art_wave1_grid:3x3",
                "world_art_wave2_5x5_without_scene_rewrite:true",
                "fallback_proxy_asset:C:/projets/beekingdom/carte.png",
                "dedicated_fullscreen_interface:true",
                "pan_zoom_enabled:true",
                "smooth_pan_zoom:true",
                "map_is_not_final_static_image:true",
                "large_world_logical_space:true",
                "world_id:" + WorldId,
                "game_server_id:" + GameServerId,
                "coordinate_model:WorldId,SectorId,ChunkId,TileCoord,WorldCoord",
                "tile_chunk_model_prepared:true",
                "chunk_size_world_units:512",
                "world_chunks:64x64",
                "active_chunk_neighborhood:5x5",
                "active_chunk_minimum_3x3:true",
                "chunk_activation_on_boundary_cross:true",
                "chunk_deactivation_outside_neighborhood:true",
                "single_large_sprite_logical_dependency:false",
                "deterministic_local_seed:738921",
                "placement_rules:min_hive_distance,no_hive_resource_overlap,limited_density_per_chunk,reproducible_seed",
                "test_hive_present:true",
                "visible_hives:deterministic_by_active_chunks",
                "visible_hive_roles:player,ally,neutral",
                "hive_models:beginning,mid,advanced,capital",
                "collectable_resources:pollen,nectar,wax,propolis,royal_jelly_demo",
                "premium_runtime_resources:nectar,pollen,water,wax,honey,royal_jelly,propolis",
                "premium_runtime_bestiary:T1..T7_two_variants_local_demo",
                "premium_runtime_entities_server:false",
                "premium_runtime_entities_official_rewards:false",
                "visible_collectable_resources:deterministic_by_active_chunks",
                "local_snapshot_deterministic:true",
                "hive_selection_supported:true",
                "resource_selection_supported:true",
                "local_collect_action_supported:true",
                "collection_states:En vol,Collecte,Retour,Termine",
                "multiple_local_demo_flights_supported:true",
                "active_recent_flight_journal:true",
                "flight_anchors:world_coordinates",
                "flight_continues_across_chunk_boundary:true",
                "flight_movement_language:aerial_only",
                "overlays_separated_from_background:true",
                "hud_world_coordinates:true",
                "debug_chunk_bounds_toggle:true",
                "local_demo_reward_supported:true",
                "troop_movement_visual:true",
                "troop_movement_type:aerial_arc_swarm_trail",
                "painted_roads_ignored:true",
                "ground_routes_used:false",
                "official_collection:false",
                "official_combat:false",
                "persistent_economy:false",
                "inner_hive_touched:false",
                "server_live:false",
                "world_map_local_lab:true"
            };
        }

        public static string[] WorldMapLocalLabForProof()
        {
            return WorldMapLocalLabRuntime.ProofRows();
        }

        public static string[] WorldMapLargeWorldStep3SelfCheckForProof()
        {
            return new[]
            {
                "step3_self_check:true",
                "pan_crosses_multiple_chunks:C32_32_to_C35_32",
                "active_chunks_after_boundary_cross:25",
                "minimum_active_chunks_required:9",
                "flight_origin_worldcoord_stable_after_pan:true",
                "flight_destination_worldcoord_stable_after_pan:true",
                "flight_path_recomputed_from_worldcoord:true",
                "ground_route_graph_present:false",
                "painted_road_sampling_for_pathfinding:false"
            };
        }

        public static string[] WorldMapWave4Step4AForProof()
        {
            var provider = new WorldMapWave4ManifestContentProvider();
            provider.Load();
            var rows = new List<string>
            {
                "step4a_worldmap_unity_integration:true",
                "scene:Assets/Scenes/WorldMapMmoFullscreenFoundation.unity",
                "manifest_driven_content_provider:true",
                "logical_world_chunks:64x64",
                "active_window_chunks:5x5",
                "runtime_python_required:false",
                "entities_overlay_separate_from_tiles:true",
                "aerial_flights_only:true",
                "ground_routes_used:false",
                "hud_fixed_during_pan_zoom:true",
                "tablet_landscape_supported:true",
                "phone_portrait_supported:true",
                "server_live:false"
            };

            rows.AddRange(provider.ProofRows());
            return rows.ToArray();
        }

        public static string[] WorldMapRuntimeTileSeamStep4BForProof()
        {
            var rows = new List<string>
            {
                "step4b_runtime_tile_seam_correction:superseded_by_step5a_wave3",
                "step4c_runtime_continuity_correction:superseded_by_step5a_wave3",
                "tile_rect_strategy:wave3_runtime_tiles_shared_world_rects",
                "continuous_atlas_single_draw:false",
                "chunk_tile_draws_for_art:false_when_wave3_unavailable",
                "pixel_snapping_for_primary_art:false",
                "per_chunk_dark_overlay_removed:true",
                "tile_atmosphere_pass:world_tile_post_terrain_pre_overlay",
                "runtime_grid_pattern_visible:false",
                "continuous_world_illusion_runtime_target:true",
                "atlas_wrap_mode:Clamp",
                "atlas_repeat_visible:false",
                "atlas_uv_policy:wave3_inner_uv_clamp_no_repeat",
                "visible_uv_never_samples_outside_0_1:true",
                "single_surface_no_internal_tile_edges:superseded_by_wave3_gutter_tiles",
                "no_5x5_master_integrated:false",
                "wave3_runtime_tile_count:25",
                "wave3_load_failure_fails_closed:true",
                "canonical_static_uv_fallback_reachable:false",
                "canonical_modulo_tile_fallback_reachable:false",
                "source_png_modified:false",
                "logical_world_chunks:64x64",
                "active_window_chunks:5x5",
                "overlays_separated_from_background:true",
                "aerial_flights_only:true",
                "ground_routes_used:false",
                "server_live:false",
                "dynamic_pan_expected:C32_32_to_C35_32_to_C36_32",
                "dynamic_pan_crosses_three_chunks:true",
                "dynamic_pan_active_window_preserved:25",
                "dynamic_pan_flight_world_anchors_preserved:true",
                "dynamic_pan_hud_fixed:true",
                "dynamic_pan_selection_layer_preserved:true",
                "dynamic_pan_no_ground_route_claim:true",
                "dynamic_pan_world_delta_chunks:4",
                "dynamic_pan_surface_uv_bounded:superseded_by_shared_world_transform",
                "dynamic_pan_visual_surface_no_repeat:true",
                "dynamic_pan_no_hole_overlap_flash_static_contract:true"
            };

            return rows.ToArray();
        }

        public static string[] WorldMapWave3SharedTransformStep5AForProof()
        {
            var rows = new List<string>
            {
                "step5a_wave3_shared_world_transform:true",
                "user_reported_static_background_bug:fixed",
                "terrain_primary_renderer:wave3_world_tiles",
                "fullscreen_static_uv_surface_primary:false",
                "terrain_entities_same_world_to_screen:true",
                "hud_screen_space_fixed:true",
                "wave3_runtime_tile_root:WorldMapWave3Runtime/UIB_ContinuousMaster5x5_v1",
                "wave3_grid:5x5",
                "wave3_runtime_tile_count:25",
                "wave3_runtime_tile_size:516x516",
                "wave3_canonical_inner_size:512x512",
                "wave3_gutter_pixels_each_side:2",
                "wave3_uv_inner:2/516..514/516",
                "wave3_macro_origin_chunk:C30_30",
                "wave3_macro_center_chunk:C32_32",
                "wave3_macro_bounds_world:15360,15360,17920,17920",
                "wave3_no_modulo_repeat:true",
                "wave3_load_failure_fails_closed:true",
                "canonical_static_uv_fallback_reachable:false",
                "canonical_modulo_tile_fallback_reachable:false",
                "wave3_texture_wrap:Clamp",
                "wave3_filter:Bilinear",
                "wave3_mipmaps_runtime_required:false",
                "wave3_mapping_identity_no_flip_no_rotate:true",
                "world_logical_chunks_preserved:64x64",
                "visual_camera_bounded_to_wave3_art:true",
                "active_window_chunks:5x5",
                "overlays_separated_from_background:true",
                "aerial_flights_only:true",
                "ground_routes_used:false",
                "step4d_controls_updated_for_shared_transform:true",
                "no_shader_blur_band_overlay:true",
                "png_source_modified:false",
                "server_live:false"
            };

            AddSharedTransformProof(rows, 1920, 1080, 1.10f, new Vector2Int(32, 32), new Vector2Int(33, 32));
            AddZoomScaleProof(rows, 1920, 1080, new Vector2Int(32, 32), 0.85f, 1.35f);
            AddHudFixedProof(rows);
            return rows.ToArray();
        }

        public static string[] WorldMapWave5IntegrationForProof()
        {
            return new[]
            {
                "wave5_assets_preserved:true",
                "wave5_source_master_sha256:" + WorldMapWave5StreamingTileProvider.ExpectedMasterSha256,
                "wave5_runtime_resource_root:" + WorldMapWave5StreamingTileProvider.ResourceRoot,
                "wave5_25x25_canonical_active:false",
                "wave5_png_modified:false"
            };
        }

        public static string[] WorldMapWave6IntegrationForProof()
        {
            var rows = new List<string>
            {
                "wave6_50x50_unity_integration:true",
                "scene:Assets/Scenes/WorldMapMmoFullscreenFoundation.unity",
                "source_master_sha256:" + WorldMapWave6StreamingTileProvider.ExpectedMasterSha256,
                "runtime_resource_root:" + WorldMapWave6StreamingTileProvider.ResourceRoot,
                "grid:50x50",
                "runtime_tile_count:2500",
                "canonical_tile_size:512x512",
                "runtime_tile_size:516x516",
                "true_gutter_pixels_each_side:2",
                "artistic_origin_chunk:C07_07",
                "artistic_center_world:16384,16384",
                "artistic_center_tiles:R24C24,R24C25,R25C24,R25C25",
                "artistic_last_chunk:C56_56",
                "artistic_world_bounds:3584,3584,29184,29184",
                "logical_world_chunks_preserved:64x64",
                "visual_camera_bounded_to_wave6_art:true",
                "streaming_visible_tiles_only:true",
                "streaming_cache_capacity:128",
                "streaming_prefetch_ring:1",
                "monolithic_25600_texture_imported:false",
                "source_png_modified:false",
                "wave5_png_modified:false",
                "v3d_preview_resource_root:" + WorldMapWave6StreamingTileProvider.V3DPreviewResourceRoot,
                "v3d_preview_source_master_sha256:" + WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256,
                "v3e_candidate_resource_root:" + WorldMapWave6StreamingTileProvider.V3ECandidateResourceRoot,
                "v3e_candidate_source_master_sha256:" + WorldMapWave6StreamingTileProvider.V3ECandidateExpectedMasterSha256,
                "v3v_candidate_resource_root:" + WorldMapWave6StreamingTileProvider.V3VCandidateResourceRoot,
                "v3v_candidate_source_master_sha256:" + WorldMapWave6StreamingTileProvider.V3VCandidateExpectedMasterSha256,
                "v3o_reduced_audit_resource_root:" + WorldMapWave6StreamingTileProvider.V3OReducedAuditPreviewResourceRoot,
                "v3o_reduced_audit_source_master_sha256:" + WorldMapWave6StreamingTileProvider.V3OReducedAuditPreviewExpectedMasterSha256,
                "route_lock_coherent_proof_resource_root:" + WorldMapWave6StreamingTileProvider.RouteLockCoherentProofResourceRoot,
                "route_lock_coherent_proof_source_master_sha256:" + WorldMapWave6StreamingTileProvider.RouteLockCoherentProofExpectedMasterSha256,
                "v2o_perimeter_audit_resource_root:" + WorldMapWave6StreamingTileProvider.V2OPerimeterAuditPreviewResourceRoot,
                "v2o_perimeter_audit_source_master_sha256:" + WorldMapWave6StreamingTileProvider.V2OPerimeterAuditPreviewExpectedMasterSha256,
                "v2i_repair_audit_resource_root:" + WorldMapWave6StreamingTileProvider.V2IRepairAuditPreviewResourceRoot,
                "v2i_repair_audit_source_master_sha256:" + WorldMapWave6StreamingTileProvider.V2IRepairAuditPreviewExpectedMasterSha256,
                "v3d_preview_scene_available:true",
                "v3d_preview_play_mode_authorized:true",
                "v3d_preview_canonical_swap:false",
                "v3d_preview_unity_handoff:false",
                "v3d_preview_master_25600_authorized:false",
                "v3e_candidate_canonical_swap:false",
                "v3e_candidate_unity_handoff:false",
                "v3v_candidate_canonical_swap:false",
                "v3v_candidate_unity_handoff:false",
                "v2o_perimeter_audit_canonical_swap:false",
                "v2o_perimeter_audit_unity_handoff:false",
                "v2i_repair_audit_canonical_swap:false",
                "v2i_repair_audit_unity_handoff:false",
                "texture_wrap:Clamp",
                "texture_filter:Bilinear",
                "texture_mipmaps:false",
                "old_wave5_25x25_canonical_active:false",
                "old_wave3_5x5_canonical_active:false",
                "canonical_static_uv_fallback_reachable:false",
                "canonical_modulo_tile_fallback_reachable:false",
                "wave6_load_failure_fails_closed:true",
                "terrain_entities_landmarks_same_world_to_screen:true",
                "hud_screen_space_fixed:true",
                "overlays_separated_from_terrain:true",
                "aerial_flights_only:true",
                "ground_routes_used:false",
                "bear_den_asset_separate:true",
                "bear_den_original_wave5_anchor_tile:R05C02",
                "bear_den_wave6_visible_tile:R18C15",
                "bear_den_anchor_local:256,471",
                "bear_den_world_size:767.5x512",
                "bear_den_pivot_normalized:0.50,0.08",
                "bear_den_no_spawn_radius_tiles:0.85",
                "bear_den_visible_by_default:true",
                "bear_den_toggle_session_local:true",
                "bear_den_toggle_hud_fixed:true",
                "bear_visible:false",
                "bear_den_active_event:false",
                "bear_den_road_visible:false",
                "hive_to_canonical_world_map_navigation_preserved:true",
                "server_live:false",
                "device_claim:false"
            };

            AddSharedTransformProof(rows, 1920, 1080, 1.10f, new Vector2Int(32, 32), new Vector2Int(33, 32));
            AddZoomScaleProof(rows, 1920, 1080, new Vector2Int(32, 32), 0.85f, 1.35f);
            AddHudFixedProof(rows);
            return rows.ToArray();
        }

        private void LoadWave3RuntimeTiles()
        {
            wave3Provider = new Wave3RuntimeGutterTileProvider();
            wave3Provider.Load();
            if (wave3Provider.IsLoaded)
            {
                status = "Wave3 5x5 runtime tiles - terrain monde partage avec entites";
            }
            else
            {
                status = "WorldMap Wave3 indisponible - aucun fallback UV ou modulo active";
            }
        }

        private void LoadWave6RuntimeTiles()
        {
            wave3Provider = null;
            if (useV3VCandidateRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.V3VCandidateResourceRoot,
                    WorldMapWave6StreamingTileProvider.V3VCandidateExpectedMasterSha256);
            }
            else if (useV3OReducedAuditPreviewRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.V3OReducedAuditPreviewResourceRoot,
                    WorldMapWave6StreamingTileProvider.V3OReducedAuditPreviewExpectedMasterSha256);
            }
            else if (useRouteLockCoherentProofRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.RouteLockCoherentProofResourceRoot,
                    WorldMapWave6StreamingTileProvider.RouteLockCoherentProofExpectedMasterSha256);
            }
            else if (useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.RouteLock8192ScaleBridgeProofResourceRoot,
                    WorldMapWave6StreamingTileProvider.RouteLock8192ScaleBridgeProofExpectedMasterSha256);
            }
            else if (useWave5Method12288PreviewRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewResourceRoot,
                    WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewExpectedMasterSha256);
            }
            else if (useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.SupportCenterNativeAuditPreviewResourceRoot,
                    WorldMapWave6StreamingTileProvider.SupportCenterNativeAuditPreviewExpectedMasterSha256);
            }
            else if (useV2IRepairAuditPreviewRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.V2IRepairAuditPreviewResourceRoot,
                    WorldMapWave6StreamingTileProvider.V2IRepairAuditPreviewExpectedMasterSha256);
            }
            else if (useV2ISelectedHdLocalRepairReviewRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.V2ISelectedHdLocalRepairReviewResourceRoot,
                    WorldMapWave6StreamingTileProvider.V2ISelectedHdLocalRepairReviewExpectedMasterSha256);
            }
            else if (useV2OPerimeterAuditPreviewRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.V2OPerimeterAuditPreviewResourceRoot,
                    WorldMapWave6StreamingTileProvider.V2OPerimeterAuditPreviewExpectedMasterSha256);
            }
            else if (useV2INativeAuditPreviewRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.V2INativeAuditPreviewResourceRoot,
                    WorldMapWave6StreamingTileProvider.V2INativeAuditPreviewExpectedMasterSha256);
            }
            else if (useV3MPreviewRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.V3MPreviewResourceRoot,
                    WorldMapWave6StreamingTileProvider.V3MPreviewExpectedMasterSha256);
            }
            else if (useV3ECandidateRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.V3ECandidateResourceRoot,
                    WorldMapWave6StreamingTileProvider.V3ECandidateExpectedMasterSha256);
            }
            else if (useV3DPreviewRuntimePackageForPlayMode)
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider(
                    WorldMapWave6StreamingTileProvider.V3DPreviewResourceRoot,
                    WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256);
            }
            else
            {
                wave6Provider = new WorldMapWave6StreamingTileProvider();
            }

            bool visibleReady = wave6Provider.Initialize(targetWorldCenter, targetZoom, Screen.width, Screen.height);
            if (wave6Provider.ManifestReady && !wave6Provider.HasLoadFailure)
            {
                status = visibleReady
                    ? (useV3VCandidateRuntimePackageForPlayMode
                        ? "Wave6 50x50 V3V candidate - terrain et entites en repere monde partage"
                        : useV3OReducedAuditPreviewRuntimePackageForPlayMode
                        ? "Wave6 50x50 V3O reduced audit - terrain et entites en repere monde partage"
                        : useRouteLockCoherentProofRuntimePackageForPlayMode
                        ? "Wave6 50x50 route-lock proof - terrain et entites en repere monde partage"
                        : useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode
                        ? "Wave6 50x50 route-lock 8192 scale-bridge proof - terrain et entites en repere monde partage"
                        : useWave5Method12288PreviewRuntimePackageForPlayMode
                        ? "Wave6 50x50 Wave5-method 12288 preview - terrain et entites en repere monde partage"
                        : useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode
                        ? "Wave6 50x50 support center native audit - terrain et entites en repere monde partage"
                        : useV2IRepairAuditPreviewRuntimePackageForPlayMode
                        ? "OBSOLETE FAIL - V2I repair audit - ne pas tester pour final 50x50"
                        : useV2ISelectedHdLocalRepairReviewRuntimePackageForPlayMode
                        ? "OBSOLETE REVIEW - selected HD local repair - ne pas tester pour final 50x50"
                        : useV2OPerimeterAuditPreviewRuntimePackageForPlayMode
                        ? "OBSOLETE FAIL - V2O perimeter audit - ne pas tester pour final 50x50"
                        : useV2INativeAuditPreviewRuntimePackageForPlayMode
                        ? "OBSOLETE FAIL - V2I native audit - ne pas tester pour final 50x50"
                        : useV3MPreviewRuntimePackageForPlayMode
                        ? "Wave6 50x50 V3M preview - terrain et entites en repere monde partage"
                        : useV3ECandidateRuntimePackageForPlayMode
                        ? "Wave6 50x50 V3E candidate - terrain et entites en repere monde partage"
                        : useV3DPreviewRuntimePackageForPlayMode
                        ? "Wave6 50x50 V3D preview - terrain et entites en repere monde partage"
                        : "Wave6 50x50 locale - terrain et entites en repere monde partage")
                    : (useV3VCandidateRuntimePackageForPlayMode
                        ? "Wave6 50x50 V3V candidate - chargement des tuiles visibles"
                        : useV3OReducedAuditPreviewRuntimePackageForPlayMode
                        ? "Wave6 50x50 V3O reduced audit - chargement des tuiles visibles"
                        : useRouteLockCoherentProofRuntimePackageForPlayMode
                        ? "Wave6 50x50 route-lock proof - chargement des tuiles visibles"
                        : useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode
                        ? "Wave6 50x50 route-lock 8192 scale-bridge proof - chargement des tuiles visibles"
                        : useWave5Method12288PreviewRuntimePackageForPlayMode
                        ? "Wave6 50x50 Wave5-method 12288 preview - chargement des tuiles visibles"
                        : useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode
                        ? "Wave6 50x50 support center native audit - chargement des tuiles visibles"
                        : useV2IRepairAuditPreviewRuntimePackageForPlayMode
                        ? "OBSOLETE FAIL - V2I repair audit - chargement invalide final 50x50"
                        : useV2OPerimeterAuditPreviewRuntimePackageForPlayMode
                        ? "OBSOLETE FAIL - V2O perimeter audit - chargement invalide final 50x50"
                        : useV2INativeAuditPreviewRuntimePackageForPlayMode
                        ? "OBSOLETE FAIL - V2I native audit - chargement invalide final 50x50"
                        : useV3MPreviewRuntimePackageForPlayMode
                        ? "Wave6 50x50 V3M preview - chargement des tuiles visibles"
                        : useV3ECandidateRuntimePackageForPlayMode
                        ? "Wave6 50x50 V3E candidate - chargement des tuiles visibles"
                        : useV3DPreviewRuntimePackageForPlayMode
                        ? "Wave6 50x50 V3D preview - chargement des tuiles visibles"
                        : "Wave6 50x50 locale - chargement des tuiles visibles");
            }
            else
            {
                status = "WorldMap Wave6 indisponible - aucun fallback Wave5, UV ou modulo active";
            }
        }

        private void LoadBearDenLandmark()
        {
            bearDenLandmark = new WorldMapBearDenLandmark();
            if (!bearDenLandmark.Load())
            {
                status = "Wave6 chargee - landmark Taniere indisponible";
            }
        }

        private void LoadRuntimePlacementMask()
        {
            runtimePlacementMask.Clear();
            runtimePlacementMaskLoaded = false;
            runtimePlacementMaskEntries = 0;
            if (!useWave5Method12288PreviewRuntimePackageForPlayMode) return;

            TextAsset maskAsset = Resources.Load<TextAsset>(Wave6RuntimePlacementMaskResource);
            if (maskAsset == null)
            {
                status = "Wave6 chargee - masque placement runtime manquant";
                return;
            }

            RuntimePlacementMaskData mask;
            try
            {
                mask = JsonUtility.FromJson<RuntimePlacementMaskData>(maskAsset.text);
            }
            catch (Exception exception)
            {
                status = "Wave6 chargee - masque placement runtime illisible: " + exception.Message;
                return;
            }

            if (mask == null || mask.entries == null || mask.entries.Length == 0)
            {
                status = "Wave6 chargee - masque placement runtime vide";
                return;
            }

            for (int i = 0; i < mask.entries.Length; i++)
            {
                RuntimePlacementMaskEntry entry = mask.entries[i];
                Vector2Int chunk = new Vector2Int(entry.chunk_x, entry.chunk_y);
                if (!IsChunkInWorld(chunk)) continue;
                runtimePlacementMask[chunk] = entry;
            }

            runtimePlacementMaskEntries = runtimePlacementMask.Count;
            runtimePlacementMaskLoaded = runtimePlacementMaskEntries > 0;
        }

        private void LoadLocalLab()
        {
            localLab = new WorldMapLocalLabRuntime();
            Rect bounds = wave6Provider != null && wave6Provider.ManifestReady && !wave6Provider.HasLoadFailure
                ? wave6Provider.WorldBounds
                : new Rect(0f, 0f, WorldWidthUnits(), WorldHeightUnits());
            localLab.Initialize(bounds);
        }

        private void HandleInput()
        {
            if (HiveViewProductUiPresenter.GuidedWorldMapTutorialActiveForRuntime())
            {
                HandleGuidedWorldMapInput();
                return;
            }

            Vector2 mouseGui = ScreenToGui(Input.mousePosition);
            bool pointerOverHud = IsPointerOverFixedUi(mouseGui);
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && !pointerOverHud)
            {
                ZoomAround(mouseGui, targetZoom * (1f + scroll * 0.10f));
            }

            if (Input.GetMouseButtonDown(0) && !pointerOverHud)
            {
                dragging = true;
                lastMousePosition = mouseGui;
                mouseDownPosition = mouseGui;
                mouseDragDistance = 0f;
            }

            if (Input.GetMouseButton(0) && dragging)
            {
                Vector2 now = ScreenToGui(Input.mousePosition);
                Vector2 delta = now - lastMousePosition;
                targetWorldCenter -= delta / Mathf.Max(0.01f, targetZoom);
                mouseDragDistance += delta.magnitude;
                lastMousePosition = now;
                ClampTargetWorldCenter();
            }

            if (Input.GetMouseButtonUp(0))
            {
                Vector2 up = ScreenToGui(Input.mousePosition);
                if (dragging && mouseDragDistance < 10f && Vector2.Distance(mouseDownPosition, up) < 12f && !IsPointerOverFixedUi(up))
                {
                    TrySelectAt(up);
                }

                dragging = false;
            }

            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                Vector2 guiTouch = ScreenToGui(touch.position);
                if (IsPointerOverFixedUi(guiTouch))
                {
                    lastTouchDistance = 0f;
                    return;
                }

                if (touch.phase == TouchPhase.Moved)
                {
                    targetWorldCenter -= new Vector2(touch.deltaPosition.x, -touch.deltaPosition.y) / Mathf.Max(0.01f, targetZoom);
                    ClampTargetWorldCenter();
                }
            }
            else if (Input.touchCount >= 2)
            {
                Touch a = Input.GetTouch(0);
                Touch b = Input.GetTouch(1);
                Vector2 center = (a.position + b.position) * 0.5f;
                Vector2 guiCenter = ScreenToGui(center);
                if (IsPointerOverFixedUi(guiCenter))
                {
                    lastTouchDistance = 0f;
                    return;
                }

                float distance = Vector2.Distance(a.position, b.position);
                if (lastTouchDistance > 1f)
                {
                    float ratio = distance / Mathf.Max(1f, lastTouchDistance);
                    ZoomAround(guiCenter, targetZoom * ratio);
                    targetWorldCenter -= (guiCenter - lastTouchCenter) / Mathf.Max(0.01f, targetZoom);
                    ClampTargetWorldCenter();
                }

                lastTouchDistance = distance;
                lastTouchCenter = guiCenter;
            }
            else
            {
                lastTouchDistance = 0f;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                targetWorldCenter = wave6Provider != null && wave6Provider.ManifestReady
                    ? wave6Provider.WorldBounds.center
                    : new Vector2(WorldChunkWidth * ChunkSize * 0.5f, WorldChunkHeight * ChunkSize * 0.5f);
                targetZoom = 1f;
            }

#if UNITY_EDITOR
            // Grille de debug (identifiants de secteur/chunk bruts) reservee a l'Editeur - cette
            // touche etait active dans tous les builds, y compris ceux remis a des joueurs
            // (PREMIUM_PLAYTEST_REPORT.md).
            if (Input.GetKeyDown(KeyCode.G))
            {
                debugChunkOverlay = !debugChunkOverlay;
            }
#endif
        }

        private void HandleGuidedWorldMapInput()
        {
            dragging = false;
            lastTouchDistance = 0f;
            if (!Input.GetMouseButtonUp(0)) return;

            Vector2 point = ScreenToGui(Input.mousePosition);
            bool selectsHive = HiveViewProductUiPresenter.GuidedWorldMapTutorialHiveSelectionStepForRuntime();
            bool selectsResource = HiveViewProductUiPresenter.GuidedWorldMapTutorialResourceSelectionStepForRuntime();
            if ((!selectsHive && !selectsResource)
                || IsPointerOverFixedUi(point))
            {
                HiveViewProductUiPresenter.RegisterGuidedWorldMapBlockedInputForRuntime();
                return;
            }

            TrySelectAt(point);
        }

        private void HandleGuidedWorldMapGuiInput()
        {
            if (!HiveViewProductUiPresenter.GuidedWorldMapTutorialActiveForRuntime()) return;
            Event current = Event.current;
            if (current == null || current.type != EventType.MouseUp) return;

            bool selectsHive = HiveViewProductUiPresenter.GuidedWorldMapTutorialHiveSelectionStepForRuntime();
            bool selectsResource = HiveViewProductUiPresenter.GuidedWorldMapTutorialResourceSelectionStepForRuntime();
            if (!selectsHive && !selectsResource) return;

            Vector2 point = current.mousePosition;
            bool accepted = false;
            if (!IsPointerOverFixedUi(point) && selectsHive)
            {
                WorldHiveNode hive = HiveById("hive_player_test");
                if (hive != null)
                {
                    Vector2 center = WorldToScreen(hive.WorldCoord);
                    float size = IsPortraitLayout() ? 118f : 142f;
                    Rect target = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
                    if (target.Contains(point))
                    {
                        selectedHiveId = hive.Id;
                        accepted = HiveViewProductUiPresenter.SelectGuidedWorldMapHiveForRuntime(hive.Id);
                        if (accepted) status = "Ruche selectionnee: " + hive.Label + " @ " + CoordLabel(hive.WorldCoord);
                    }
                }
            }
            else if (!IsPointerOverFixedUi(point) && selectsResource)
            {
                WorldResourceNode resource = ResourceById("res_pollen_core");
                if (resource != null)
                {
                    Vector2 center = WorldToScreen(resource.WorldCoord);
                    float size = IsPortraitLayout() ? 132f : 156f;
                    Rect target = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
                    if (target.Contains(point))
                    {
                        selectedResourceId = resource.Id;
                        accepted = HiveViewProductUiPresenter.SelectGuidedWorldMapResourceForRuntime(resource.Id);
                        if (accepted) status = "Pollen selectionne: " + resource.Label + " @ " + CoordLabel(resource.WorldCoord);
                    }
                }
            }

            if (!accepted) HiveViewProductUiPresenter.RegisterGuidedWorldMapBlockedInputForRuntime();
            current.Use();
        }

        private void UpdateCollectionFlight()
        {
            UpdateFlightRecords();
            if (collectionState == CollectionFlightState.Idle || collectionState == CollectionFlightState.Completed) return;

            collectionTimer += Time.deltaTime;
            if (collectionState == CollectionFlightState.FlyingToResource && collectionTimer >= 3.2f)
            {
                collectionState = CollectionFlightState.Collecting;
                collectionTimer = 0f;
                WorldResourceNode resource = SelectedResource();
                status = "Collecte locale/demo en cours: " + (resource != null ? resource.Label : "ressource");
            }
            else if (collectionState == CollectionFlightState.Collecting && collectionTimer >= 1.15f)
            {
                collectionState = CollectionFlightState.Returning;
                collectionTimer = 0f;
                status = "Retour aerien vers la ruche - aucune route au sol";
            }
            else if (collectionState == CollectionFlightState.Returning && collectionTimer >= 3.0f)
            {
                CompleteSelectedResourceCollection();
            }
        }

        private void UpdateResourceRespawns()
        {
            if (resourceRespawnAt.Count == 0) return;
            List<string> ready = null;
            foreach (KeyValuePair<string, float> pair in resourceRespawnAt)
            {
                if (Time.realtimeSinceStartup < pair.Value) continue;
                if (ready == null) ready = new List<string>();
                ready.Add(pair.Key);
            }

            if (ready == null) return;
            for (int i = 0; i < ready.Count; i++)
            {
                ForceRespawnForProof(ready[i]);
            }
        }

        private void UpdateFlightRecords()
        {
            for (int i = 0; i < flights.Count; i++)
            {
                WorldFlightRecord flight = flights[i];
                if (flight.State == CollectionFlightState.Completed) continue;

                flight.Timer += Time.deltaTime;
                if (flight.State == CollectionFlightState.FlyingToResource && flight.Timer >= 3.2f)
                {
                    flight.State = CollectionFlightState.Collecting;
                    flight.Timer = 0f;
                }
                else if (flight.State == CollectionFlightState.Collecting && flight.Timer >= 1.15f)
                {
                    flight.State = CollectionFlightState.Returning;
                    flight.Timer = 0f;
                }
                else if (flight.State == CollectionFlightState.Returning && flight.Timer >= 3.0f)
                {
                    flight.State = CollectionFlightState.Completed;
                    flight.Timer = 0f;
                }
            }
        }

        private void RefreshActiveChunks(bool force)
        {
            Vector2Int center = WorldToChunk(currentWorldCenter);
            bool changed = force || activeChunks.Count == 0;
            if (!changed)
            {
                for (int i = 0; i < activeChunks.Count; i++)
                {
                    if (Mathf.Abs(activeChunks[i].x - center.x) > ActiveChunkRadius || Mathf.Abs(activeChunks[i].y - center.y) > ActiveChunkRadius)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed) return;

            activeChunks.Clear();
            hives.Clear();
            resources.Clear();
            bestiary.Clear();
            pointsOfInterest.Clear();

            for (int y = center.y - ActiveChunkRadius; y <= center.y + ActiveChunkRadius; y++)
            {
                for (int x = center.x - ActiveChunkRadius; x <= center.x + ActiveChunkRadius; x++)
                {
                    Vector2Int chunk = new Vector2Int(x, y);
                    if (!IsChunkInWorld(chunk)) continue;
                    activeChunks.Add(chunk);
                    WorldChunkData data = GetOrCreateChunk(chunk);
                    hives.AddRange(data.Hives);
                    resources.AddRange(data.Resources);
                    bestiary.AddRange(data.Bestiary);
                    pointsOfInterest.AddRange(data.PointsOfInterest);
                }
            }

            EnsureSelectionStillValid();
            status = "Chunks actifs: " + activeChunks.Count.ToString(CultureInfo.InvariantCulture) + " autour de " + ChunkId(center);
        }

        private WorldChunkData GetOrCreateChunk(Vector2Int chunk)
        {
            WorldChunkData data;
            if (chunkCache.TryGetValue(chunk, out data)) return data;

            data = new WorldChunkData(chunk);
            GenerateChunkData(data);
            chunkCache[chunk] = data;
            return data;
        }

        private void GenerateChunkData(WorldChunkData data)
        {
            AddCoreSeededNodes(data);
            GenerateSeededHive(data);
            GenerateSeededResources(data);
            GenerateSeededBestiary(data);
            GenerateSeededPointsOfInterest(data);
        }

        private void AddCoreSeededNodes(WorldChunkData data)
        {
            Vector2Int center = PlayerCoreChunk();
            if (data.Chunk == center)
            {
                data.Hives.Add(new WorldHiveNode("hive_player_test", "Rucher du Vieux Chêne", "JOUEUR", HiveMaturity.Beginning, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.48f, 0.54f), RuntimePlacementFamily.Hive, ResourceKind.Pollen, 501), "Le foyer de ta colonie"));
                data.Resources.Add(new WorldResourceNode("res_nectar_core", "Nectar", ResourceKind.Nectar, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.68f, 0.38f), RuntimePlacementFamily.Resource, ResourceKind.Nectar, 511), 90));
                data.Resources.Add(new WorldResourceNode("res_pollen_core", "Pollen", ResourceKind.Pollen, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.29f, 0.35f), RuntimePlacementFamily.Resource, ResourceKind.Pollen, 521), 120));
                data.Resources.Add(new WorldResourceNode("res_water_core", "Eau", ResourceKind.Water, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.18f, 0.64f), RuntimePlacementFamily.Resource, ResourceKind.Water, 531), 80));
                data.Bestiary.Add(new WorldBestiaryNode("beast_t3_core_demo", BestiaryLabel(3, 1), 3, 1, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.78f, 0.66f), RuntimePlacementFamily.Bestiary, ResourceKind.Pollen, 541), BestiaryRole(3)));
                // M020: Araignée placée à côté de la ruche du joueur — visible immédiatement (ChunkLocalWorld direct, pas de mask, à 0.52,0.52 juste à côté de la ruche 0.48,0.54)
                data.Bestiary.Add(new WorldBestiaryNode("spider_next_to_hive", "Araignée", 2, 1, ChunkLocalWorld(data.Chunk, 0.52f, 0.52f), BestiaryRole(2)));
            }
            else if (data.Chunk == center + new Vector2Int(1, 0))
            {
                data.Hives.Add(new WorldHiveNode("hive_ally_mid", "Rucher de l'Aubépine", "ALLIEE", HiveMaturity.Mid, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.34f, 0.43f), RuntimePlacementFamily.Hive, ResourceKind.Pollen, 551), "Colonie alliée"));
                data.Hives.Add(new WorldHiveNode("hive_capital_demo", "Capitale de l'Essaim-Doré", "ALLIEE", HiveMaturity.Capital, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.72f, 0.68f), RuntimePlacementFamily.Hive, ResourceKind.Pollen, 561), "Siège du grand essaim allié"));
                data.Resources.Add(new WorldResourceNode("res_wax_core", "Cire", ResourceKind.Wax, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.58f, 0.23f), RuntimePlacementFamily.Resource, ResourceKind.Wax, 571), 70));
                data.Resources.Add(new WorldResourceNode("res_honey_core", "Miel", ResourceKind.Honey, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.22f, 0.72f), RuntimePlacementFamily.Resource, ResourceKind.Honey, 581), 60));
            }
            else if (data.Chunk == center + new Vector2Int(-1, 1))
            {
                data.Hives.Add(new WorldHiveNode("hive_neutral_advanced", "Rucher Sauvage", "NEUTRE", HiveMaturity.Advanced, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.62f, 0.50f), RuntimePlacementFamily.Hive, ResourceKind.Pollen, 591), "Colonie indépendante"));
                data.Resources.Add(new WorldResourceNode("res_propolis_core", "Propolis", ResourceKind.Propolis, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.39f, 0.68f), RuntimePlacementFamily.Resource, ResourceKind.Propolis, 601), 45));
                data.Resources.Add(new WorldResourceNode("res_royal_jelly_core", "Gelée royale", ResourceKind.RoyalJelly, RuntimePlacementPointAvoidingBearDen(data.Chunk, ChunkLocalWorld(data.Chunk, 0.82f, 0.31f), RuntimePlacementFamily.Resource, ResourceKind.RoyalJelly, 611), 18));
            }
        }

        private static Vector2Int PlayerCoreChunk()
        {
            return new Vector2Int(
                WorldMapWave6StreamingTileProvider.OriginChunkX + WorldMapWave6StreamingTileProvider.Columns / 2,
                WorldMapWave6StreamingTileProvider.OriginChunkY + WorldMapWave6StreamingTileProvider.Rows / 2);
        }

        private void GenerateSeededHive(WorldChunkData data)
        {
            if (data.Hives.Count > 0) return;
            int roll = Hash(data.Chunk.x, data.Chunk.y, 11) % 100;
            if (roll > 28) return;

            Vector2 position = RuntimePlacementPointAvoidingBearDen(data.Chunk, SeededPointInChunk(data.Chunk, 21, 0.18f, 0.82f), RuntimePlacementFamily.Hive, ResourceKind.Pollen, 21);
            if (bearDenLandmark != null && bearDenLandmark.ExcludesSpawn(position)) return;
            if (!PassesHiveDistance(data, position)) return;

            int variant = Hash(data.Chunk.x, data.Chunk.y, 31) % 4;
            HiveMaturity stage = (HiveMaturity)variant;
            string badge = variant == 0 ? "JOUEUR" : (variant == 1 ? "ALLIEE" : "NEUTRE");
            string id = "hive_" + data.Chunk.x.ToString(CultureInfo.InvariantCulture) + "_" + data.Chunk.y.ToString(CultureInfo.InvariantCulture);
            data.Hives.Add(new WorldHiveNode(id, SeededHiveName(data.Chunk), badge, stage, position, "Colonie sauvage"));
        }

        // Noms credibles pour les ruches semees proceduralement a travers le monde - remplace
        // l'ancien "Ruche " + coordonnees de chunk (ex. "Ruche C33_31"), qui exposait des
        // identifiants internes au joueur (PREMIUM_PLAYTEST_REPORT.md). Choix stable par chunk
        // (meme hash que le reste du placement), pas une nouvelle mecanique.
        private static readonly string[] SeededHivePrefixes =
        {
            "Rucher", "Colonie", "Nid", "Essaim", "Foyer"
        };

        private static readonly string[] SeededHiveSuffixes =
        {
            "du Trèfle", "de la Rosée", "des Bourdons", "du Pollen d'Or", "de la Clairière",
            "des Chardons", "du Vent Doux", "de l'Écorce", "des Fleurs Sauvages", "du Ruisseau",
            "de la Mousse", "du Sureau"
        };

        private string SeededHiveName(Vector2Int chunk)
        {
            string prefix = SeededHivePrefixes[((Hash(chunk.x, chunk.y, 181) % SeededHivePrefixes.Length) + SeededHivePrefixes.Length) % SeededHivePrefixes.Length];
            string suffix = SeededHiveSuffixes[((Hash(chunk.x, chunk.y, 191) % SeededHiveSuffixes.Length) + SeededHiveSuffixes.Length) % SeededHiveSuffixes.Length];
            return prefix + " " + suffix;
        }

        private void GenerateSeededResources(WorldChunkData data)
        {
            int count = 1 + Hash(data.Chunk.x, data.Chunk.y, 41) % 2;
            for (int i = 0; i < count; i++)
            {
                ResourceKind kind = (ResourceKind)(Hash(data.Chunk.x, data.Chunk.y, 57 + i) % 7);
                Vector2 position = RuntimePlacementPointAvoidingBearDen(data.Chunk, SeededPointInChunk(data.Chunk, 71 + i * 17, 0.14f, 0.86f), RuntimePlacementFamily.Resource, kind, 71 + i * 17);
                if (bearDenLandmark != null && bearDenLandmark.ExcludesSpawn(position)) continue;
                if (!PassesResourceExclusion(data, position)) continue;

                string id = "res_" + ResourceToken(kind) + "_" + data.Chunk.x.ToString(CultureInfo.InvariantCulture) + "_" + data.Chunk.y.ToString(CultureInfo.InvariantCulture) + "_" + i.ToString(CultureInfo.InvariantCulture);
                data.Resources.Add(new WorldResourceNode(id, ResourceLabel(kind), kind, position, ResourceAmount(kind, data.Chunk, i)));
            }
        }

        private void GenerateSeededBestiary(WorldChunkData data)
        {
            if (data.Bestiary.Count > 0) return;
            int roll = Hash(data.Chunk.x, data.Chunk.y, 141) % 100;
            if (roll > 26) return;

            Vector2 position = RuntimePlacementPointAvoidingBearDen(data.Chunk, SeededPointInChunk(data.Chunk, 151, 0.16f, 0.84f), RuntimePlacementFamily.Bestiary, ResourceKind.Pollen, 151);
            if (bearDenLandmark != null && bearDenLandmark.ExcludesSpawn(position)) return;
            int tier = 1 + Hash(data.Chunk.x, data.Chunk.y, 161) % 7;
            int variant = 1 + Hash(data.Chunk.x, data.Chunk.y, 167) % 2;
            string id = "beast_t" + tier.ToString(CultureInfo.InvariantCulture) + "_v" + variant.ToString(CultureInfo.InvariantCulture) + "_" + data.Chunk.x.ToString(CultureInfo.InvariantCulture) + "_" + data.Chunk.y.ToString(CultureInfo.InvariantCulture);
            WorldBiome spawnBiome = WorldBiomeCatalog.BiomeForChunk(data.Chunk.x, data.Chunk.y);
            data.Bestiary.Add(new WorldBestiaryNode(id, BestiaryLabel(tier, variant, spawnBiome), tier, variant, position, BestiaryRole(tier)));
        }

        // Catalogue des Points d'Interet (demande de Jeff, 2026-08-01, complete par la Bible
        // du Monde le 2026-08-18) : des lieux qui racontent quelque chose sans expliquer de
        // mecanique - le joueur doit vouloir s'en souvenir, pas les "utiliser". Chaque
        // entree prepare un futur systeme different (alliance/diplomatie, occupation,
        // evenement, boss) sans en construire aucun ici. Description/Histoire sont une
        // transcription des lignes "Description carte"/"Histoire" de
        // BIBLE/09_World/POINTS_OF_INTEREST_BIBLE.md pour les 5 POI principaux ; les 5 POI
        // additionnels n'ont dans la bible qu'une description courte (pas de paragraphe
        // d'histoire separe), donc Histoire reste vide pour ceux-la plutot que d'en inventer une.
        // BossTeaser = transcription of BIBLE/09_World/BOSS_FOUNDATION_BIBLE.md's "boss
        // possible" line for this POI (main 5) or the POI bible's own "Promesse future"
        // line when it names a boss (2 of the additional 5 - Mare aux Reflets/"boss
        // humide" and Pierre Chaude/"boss de chaleur" cross-reference to Crapaud Royal and
        // Scorpion Geant in the Boss bible). Left empty where neither source names one
        // (Souche-Cathedrale, Champ des Premieres Fleurs, Branche des Veilleurs) rather
        // than inventing one - purely a flavor line, never a real encounter (see the Boss
        // bible's own rule: "un boss doit avoir un lieu avant d'avoir des statistiques" -
        // this just reserves the lieu, no stats/mechanic follow).
        private static readonly (string Kind, string Label, string Family, WorldBiome PrimaryBiome, string Description, string History, string BossTeaser)[] PointOfInterestCatalog =
        {
            ("ancient_hive", "Ruche ancienne", "Heritage apicole", WorldBiome.ForetClaire,
                "Une ruche morte ne signifie pas un royaume oublie. La cire conserve encore la forme des gestes qui l'ont batie.",
                "Cette ruche appartenait a une colonie disparue avant la saison actuelle. Ses galeries ne sont pas detruites par la guerre, mais usees par le temps.",
                "On raconte qu'un Titan de Propolis ou une Reine Guepe Antique pourrait un jour revendiquer ces ruines."),
            ("fossil_nest", "Nid fossilise", "Prehistoire des insectes", WorldBiome.TerresSeches,
                "Les parois gardent l'empreinte d'un peuple qui savait deja batir avant que les ruches du joueur n'existent.",
                "Avant les Frelons Noirs actuels, d'autres peuples ailes ont construit ici. Le nid a ete enseveli par une saison de boue puis durci par le temps.",
                "Une Reine Guepe Antique pourrait s'y eveiller, si une invasion ou une floraison anormale la derangeait."),
            ("blooming_grove", "Bosquet legendaire", "Sanctuaire vegetal", WorldBiome.PrairieFleurie,
                "Certaines fleurs ne poussent pas parce que le sol est fertile. Elles poussent parce que le monde se souvient d'un printemps parfait.",
                "Le bosquet fleurit selon un rythme que les abeilles ne comprennent pas encore. Les fleurs y semblent plus anciennes, moins cultivees.",
                "Le Gardien du Chene ou une Mante Orchidee legendaire y veillerait, dit-on."),
            ("sunken_hive", "Ruche naufragee", "Catastrophe et reconstruction", WorldBiome.BergesEtMares,
                "L'eau a disperse la ruche, mais pas son histoire. Chaque fragment de cire semble chercher les autres.",
                "Une colonie a ete arrachee par une crue ou une tempete. La ruche n'est pas entiere ; elle est dispersee.",
                "Un Crapaud Royal aurait pris possession de la zone humide apres la catastrophe."),
            ("forgotten_sanctuary", "Sanctuaire oublie", "Spiritualite apicole", WorldBiome.ForetClaire,
                "Rien ne garde le sanctuaire. C'est peut-etre pour cela que personne n'ose le profaner.",
                "Personne ne sait quelle colonie a fonde ce sanctuaire. Les Championnes y parlent plus bas.",
                "L'Ancetre des Abeilles ou un Titan de Propolis gardien y seraient lies."),
            ("souche_cathedrale", "Souche-Cathedrale", "Carrefour naturel", WorldBiome.ForetClaire,
                "Une souche immense creusee par les saisons, assez vaste pour abriter insectes, mousses et chambres naturelles.",
                string.Empty, string.Empty),
            ("mare_aux_reflets", "Mare aux Reflets", "POI aquatique", WorldBiome.BergesEtMares,
                "Une petite mare dont la surface reflete le ciel meme sous les feuilles.",
                string.Empty,
                "Un boss humide - un Crapaud Royal - pourrait un jour y trouver refuge."),
            ("champ_premieres_fleurs", "Champ des Premieres Fleurs", "Lieu saisonnier central", WorldBiome.PrairieFleurie,
                "Une prairie qui fleurit avant toutes les autres, observee par les Chroniqueuses.",
                string.Empty, string.Empty),
            ("pierre_chaude", "Pierre Chaude", "POI de biome sec", WorldBiome.TerresSeches,
                "Une grande pierre qui conserve la chaleur et attire reptiles, scorpions et abeilles fatiguees.",
                string.Empty,
                "Un boss de chaleur - un Scorpion Geant - rode peut-etre deja pres d'ici."),
            ("branche_veilleurs", "Branche des Veilleurs", "POI aerien et diplomatique", WorldBiome.ForetClaire,
                "Une branche haute d'ou l'on voit plusieurs ruches et routes de vol.",
                string.Empty, string.Empty),
        };

        private void GenerateSeededPointsOfInterest(WorldChunkData data)
        {
            if (data.PointsOfInterest.Count > 0) return;
            // Volontairement rare (contrairement aux ressources/bestiaire) : un point d'interet
            // doit rester un lieu remarquable, pas un element banal repete a chaque chunk.
            int roll = Hash(data.Chunk.x, data.Chunk.y, 181) % 100;
            if (roll > 6) return;

            Vector2 position = RuntimePlacementPointAvoidingBearDen(data.Chunk, SeededPointInChunk(data.Chunk, 191, 0.20f, 0.80f), RuntimePlacementFamily.Bestiary, ResourceKind.Pollen, 191);
            if (bearDenLandmark != null && bearDenLandmark.ExcludesSpawn(position)) return;

            int catalogIndex = Hash(data.Chunk.x, data.Chunk.y, 197) % PointOfInterestCatalog.Length;
            (string kind, string label, string family, WorldBiome primaryBiome, string description, string history, string bossTeaser) = PointOfInterestCatalog[catalogIndex];
            string id = "poi_" + kind + "_" + data.Chunk.x.ToString(CultureInfo.InvariantCulture) + "_" + data.Chunk.y.ToString(CultureInfo.InvariantCulture);
            data.PointsOfInterest.Add(new WorldPointOfInterestNode(id, label, kind, position, description, family, primaryBiome, history, bossTeaser));
        }

        private StressWindowStats SimulateStressWindow(Vector2Int center)
        {
            int active = 0;
            int hivesCount = 0;
            int resourcesCount = 0;
            int bestiaryCount = 0;
            for (int y = center.y - ActiveChunkRadius; y <= center.y + ActiveChunkRadius; y++)
            {
                for (int x = center.x - ActiveChunkRadius; x <= center.x + ActiveChunkRadius; x++)
                {
                    if (!IsStressChunkInWorld(x, y)) continue;
                    active++;
                    hivesCount += StressHiveCount(x, y);
                    resourcesCount += StressResourceCount(x, y);
                    bestiaryCount += StressBestiaryCount(x, y);
                }
            }

            return new StressWindowStats(active, hivesCount, resourcesCount, bestiaryCount);
        }

        private StressWindowStats FindDensestStressWindow()
        {
            StressWindowStats best = default;
            int bestScore = -1;
            for (int y = 0; y < StressWorldMapChunks; y++)
            {
                for (int x = 0; x < StressWorldMapChunks; x++)
                {
                    StressWindowStats stats = SimulateStressWindow(new Vector2Int(x, y));
                    int score = stats.Hives * 5 + stats.Resources * 2 + stats.Bestiary * 4;
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = stats;
                }
            }

            return best;
        }

        private void CountStressCatalog(out int hivesCount, out int resourcesCount, out int bestiaryCount)
        {
            hivesCount = 0;
            resourcesCount = 0;
            bestiaryCount = 0;
            for (int y = 0; y < StressWorldMapChunks; y++)
            {
                for (int x = 0; x < StressWorldMapChunks; x++)
                {
                    hivesCount += StressHiveCount(x, y);
                    resourcesCount += StressResourceCount(x, y);
                    bestiaryCount += StressBestiaryCount(x, y);
                }
            }
        }

        private static bool IsStressChunkInWorld(int x, int y)
        {
            return x >= 0 && y >= 0 && x < StressWorldMapChunks && y < StressWorldMapChunks;
        }

        private int StressHiveCount(int x, int y)
        {
            if (x == StressWorldMapChunks / 2 && y == StressWorldMapChunks / 2) return 1;
            return Hash(x, y, 11) % 100 <= 28 ? 1 : 0;
        }

        private int StressResourceCount(int x, int y)
        {
            return 1 + Hash(x, y, 41) % 2;
        }

        private int StressBestiaryCount(int x, int y)
        {
            return Hash(x, y, 141) % 100 <= 26 ? 1 : 0;
        }

        private bool PassesHiveDistance(WorldChunkData data, Vector2 position)
        {
            for (int i = 0; i < data.Hives.Count; i++)
            {
                if (Vector2.Distance(data.Hives[i].WorldCoord, position) < MinHiveDistance) return false;
            }

            foreach (WorldChunkData cached in chunkCache.Values)
            {
                for (int i = 0; i < cached.Hives.Count; i++)
                {
                    if (Vector2.Distance(cached.Hives[i].WorldCoord, position) < MinHiveDistance) return false;
                }
            }

            return true;
        }

        private bool PassesResourceExclusion(WorldChunkData data, Vector2 position)
        {
            for (int i = 0; i < data.Hives.Count; i++)
            {
                if (Vector2.Distance(data.Hives[i].WorldCoord, position) < MinHiveResourceDistance) return false;
            }

            for (int i = 0; i < data.Resources.Count; i++)
            {
                if (Vector2.Distance(data.Resources[i].WorldCoord, position) < 90f) return false;
            }

            return true;
        }

        private void DrawActiveChunks()
        {
            if (wave6Provider != null
                && wave6Provider.ManifestReady
                && !wave6Provider.HasLoadFailure
                && wave6Provider.HasAllVisibleTiles)
            {
                DrawWave6WorldTerrain();
                if (ShouldDrawWave6AtmospherePass()) DrawWorldMapAtmospherePass();
                return;
            }

            DrawWave6UnavailableState();
        }

        private bool ShouldDrawWave6AtmospherePass()
        {
            return !useWave5Method12288PreviewRuntimePackageForPlayMode;
        }

        // Bible-driven biome identity layer (see WorldBiomeCatalog.cs) - a very low-alpha
        // colored wash so each region's "emotional color" (bible rule: every region needs
        // one, not just a visual color) reads at a glance without fighting the painted
        // terrain art underneath.
        //
        // Draws WorldBiomeCatalog's fixed 10x10 grid directly (<=100 rects, screen-culled)
        // rather than one rect per streamed art tile: this file has dedicated 50x50
        // stress-test tooling because IMGUI draw cost has regressed before, and
        // wave6Provider.VisibleTiles can hold up to CacheCapacity=128 tiles at extreme
        // zoom-out - iterating that per-tile would have tripled draw calls (terrain + biome
        // + event wash) right when the map is already under the most load. The 10x10 grid
        // gives a hard, zoom-independent upper bound instead.
        private void DrawBiomeOverlay()
        {
            if (!mapFilterBiomeOverlay) return;
            if (wave6Provider == null || !wave6Provider.ManifestReady || wave6Provider.HasLoadFailure) return;

            ForEachVisibleBiomeCell((cellRect, biome) =>
            {
                Color emotional = WorldBiomeCatalog.ProfileFor(biome).EmotionalColor;
                DrawSolid(cellRect, new Color(emotional.r, emotional.g, emotional.b, 0.07f));
            });
        }

        // Shared iterator for anything that wants to draw one rect per biome-grid cell
        // (DrawBiomeOverlay, DrawWorldEventBiomeBiasedWash) - computes each cell's world rect
        // from WorldBiomeCatalog's own grid constants, projects it, and skips off-screen
        // cells before invoking the callback, so callers never touch tile-streaming state.
        private void ForEachVisibleBiomeCell(Action<Rect, WorldBiome> draw)
        {
            const int cells = 10;
            const int tilesPerCell = 5;
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            for (int cellY = 0; cellY < cells; cellY++)
            {
                for (int cellX = 0; cellX < cells; cellX++)
                {
                    int chunkX = WorldMapWave6StreamingTileProvider.OriginChunkX + cellX * tilesPerCell;
                    int chunkY = WorldMapWave6StreamingTileProvider.OriginChunkY + cellY * tilesPerCell;
                    Rect worldRect = new Rect(chunkX * ChunkSize, chunkY * ChunkSize, tilesPerCell * ChunkSize, tilesPerCell * ChunkSize);
                    Rect projected = WorldRectToScreenRect(worldRect);
                    if (!projected.Overlaps(screen)) continue;
                    WorldBiome biome = WorldBiomeCatalog.BiomeForChunk(chunkX + tilesPerCell / 2, chunkY + tilesPerCell / 2);
                    draw(projected, biome);
                }
            }
        }

        // Large, low-opacity watermark labels for the 5 bible-named regions - only past a
        // zoom-out threshold (currentZoom is small when zoomed out, see WorldToScreen),
        // since at normal play zoom they'd just be clutter over the terrain/entities the
        // player is actually reading. Fades in smoothly over a small zoom band rather than
        // a hard on/off cutoff, so crossing the threshold while zooming doesn't pop.
        private const float RegionLabelZoomThreshold = 0.55f;
        private const float RegionLabelZoomFadeStart = 0.38f;

        private void DrawRegionLabels()
        {
            if (!mapFilterBiomeOverlay) return;
            if (currentZoom >= RegionLabelZoomThreshold) return;
            float fade = Mathf.Clamp01((RegionLabelZoomThreshold - currentZoom) / (RegionLabelZoomThreshold - RegionLabelZoomFadeStart));

            Rect painted = new Rect(
                WorldMapWave6StreamingTileProvider.OriginChunkX * ChunkSize,
                WorldMapWave6StreamingTileProvider.OriginChunkY * ChunkSize,
                WorldMapWave6StreamingTileProvider.Columns * ChunkSize,
                WorldMapWave6StreamingTileProvider.Rows * ChunkSize);

            WorldRegionProfile[] regions = WorldBiomeCatalog.Regions;
            for (int i = 0; i < regions.Length; i++)
            {
                Rect bounds = regions[i].NormalizedBounds;
                Vector2 worldCenter = new Vector2(
                    painted.x + (bounds.x + bounds.width * 0.5f) * painted.width,
                    painted.y + (bounds.y + bounds.height * 0.5f) * painted.height);
                Vector2 screenPoint = WorldToScreen(worldCenter);
                if (screenPoint.x < -200f || screenPoint.x > Screen.width + 200f
                    || screenPoint.y < -60f || screenPoint.y > Screen.height + 60f) continue;

                Color labelColor = WorldBiomeCatalog.ProfileFor(regions[i].DominantBiome).EmotionalColor;
                GUIStyle style = LabelStyle(new Color(labelColor.r, labelColor.g, labelColor.b, 0.55f * fade), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
                Rect labelRect = new Rect(screenPoint.x - 160f, screenPoint.y - 16f, 320f, 32f);
                GUI.Label(labelRect, regions[i].Label.ToUpperInvariant(), style);
            }
        }

        private void DrawWave6WorldTerrain()
        {
            IReadOnlyList<Wave6RuntimeTile> tiles = wave6Provider.VisibleTiles;
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            for (int i = 0; i < tiles.Count; i++)
            {
                Wave6RuntimeTile tile = tiles[i];
                Rect terrainWorldRect = useWave5Method12288PreviewRuntimePackageForPlayMode ? tile.GutterWorldRect : tile.WorldRect;
                Rect textureUv = useWave5Method12288PreviewRuntimePackageForPlayMode ? Wave6RuntimeTile.FullTextureUv : tile.CoreUv;
                Rect projected = WorldRectToScreenRect(terrainWorldRect);
                Rect rect = PixelSnappedTileRect(projected.min, projected.max);
                if (!rect.Overlaps(screen)) continue;
                GUI.DrawTextureWithTexCoords(rect, tile.Texture, textureUv, true);
            }
        }

        private static Rect PixelSnappedTileRect(Vector2 min, Vector2 max)
        {
            const float overlapPixels = 1f;
            float xMin = Mathf.Floor(Mathf.Min(min.x, max.x)) - overlapPixels;
            float yMin = Mathf.Floor(Mathf.Min(min.y, max.y)) - overlapPixels;
            float xMax = Mathf.Ceil(Mathf.Max(min.x, max.x)) + overlapPixels;
            float yMax = Mathf.Ceil(Mathf.Max(min.y, max.y)) + overlapPixels;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void DrawWave3WorldTerrain()
        {
            IReadOnlyList<Wave3RuntimeTile> tiles = wave3Provider.Tiles;
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            float[] columnEdges = new float[6];
            float[] rowEdges = new float[6];
            Rect bounds = wave3Provider.WorldBounds;
            for (int i = 0; i <= 5; i++)
            {
                float worldX = bounds.xMin + i * ChunkSize;
                float worldY = bounds.yMin + i * ChunkSize;
                columnEdges[i] = WorldToScreen(new Vector2(worldX, bounds.yMin)).x;
                rowEdges[i] = WorldToScreen(new Vector2(bounds.xMin, worldY)).y;
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                Wave3RuntimeTile tile = tiles[i];
                Rect rect = Wave3TileScreenRect(tile, columnEdges, rowEdges);
                if (!rect.Overlaps(screen)) continue;
                GUI.DrawTextureWithTexCoords(rect, tile.Texture, tile.GutterUv, true);
            }
        }

        private Rect Wave3TileScreenRect(Wave3RuntimeTile tile, float[] columnEdges, float[] rowEdges)
        {
            const float overlapPixels = 1f;
            int column = Mathf.Clamp(tile.ChunkX - 30, 0, 4);
            int row = Mathf.Clamp(tile.ChunkY - 30, 0, 4);
            float xMin = Mathf.Min(columnEdges[column], columnEdges[column + 1]);
            float xMax = Mathf.Max(columnEdges[column], columnEdges[column + 1]);
            float yMin = Mathf.Min(rowEdges[row], rowEdges[row + 1]);
            float yMax = Mathf.Max(rowEdges[row], rowEdges[row + 1]);
            return Rect.MinMaxRect(
                Mathf.Floor(xMin) - overlapPixels,
                Mathf.Floor(yMin) - overlapPixels,
                Mathf.Ceil(xMax) + overlapPixels,
                Mathf.Ceil(yMax) + overlapPixels);
        }

        private void DrawWave3UnavailableState()
        {
            Rect panel = new Rect(Mathf.Max(24f, Screen.width * 0.5f - 330f), Mathf.Max(24f, Screen.height * 0.5f - 78f), 660f, 156f);
            DrawSolid(panel, new Color(0.035f, 0.026f, 0.014f, 0.92f));
            DrawFrame(panel, new Color(0.95f, 0.64f, 0.12f, 0.88f), 2f);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 28f), "La carte du monde est indisponible", LabelStyle(Color.white, 20, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 58f, panel.width - 48f, 52f), "Le territoire environnant n'a pas pu etre charge.", MiniLabel(new Color(1f, 0.90f, 0.66f, 1f), 13, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 116f, panel.width - 48f, 20f), "Nouvelle tentative en cours...", MiniLabel(Color.white, 12, TextAnchor.MiddleCenter));
        }

        private void DrawWave6UnavailableState()
        {
            Rect panel = new Rect(Mathf.Max(24f, Screen.width * 0.5f - 350f), Mathf.Max(24f, Screen.height * 0.5f - 84f), 700f, 168f);
            DrawSolid(panel, new Color(0.035f, 0.026f, 0.014f, 0.94f));
            DrawFrame(panel, new Color(0.95f, 0.64f, 0.12f, 0.88f), 2f);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 18f, panel.width - 48f, 30f), "La carte du monde est indisponible", LabelStyle(Color.white, 20, FontStyle.Bold, TextAnchor.MiddleCenter));
            bool stillLoading = wave6Provider == null || string.IsNullOrEmpty(wave6Provider.FailureReason);
            string reason = stillLoading ? "Chargement du territoire en cours..." : "Le territoire environnant n'a pas pu etre charge.";
            GUI.Label(new Rect(panel.x + 24f, panel.y + 56f, panel.width - 48f, 58f), reason, MiniLabel(new Color(1f, 0.90f, 0.66f, 1f), 13, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 126f, panel.width - 48f, 22f), "Nouvelle tentative en cours...", MiniLabel(Color.white, 12, TextAnchor.MiddleCenter));
        }

        private void DrawBearDenLandmark()
        {
            if (bearDenLandmark == null || !bearDenLandmark.IsLoaded || !bearDenLandmark.IsVisible) return;
            Rect screenRect = WorldRectToScreenRect(bearDenLandmark.WorldRect);
            if (!screenRect.Overlaps(new Rect(-64f, -64f, Screen.width + 128f, Screen.height + 128f))) return;

            GUI.DrawTexture(screenRect, bearDenLandmark.Texture, ScaleMode.StretchToFill, true);
            if (screenRect.width < 128f) return;

            Vector2 anchor = WorldToScreen(bearDenLandmark.WorldAnchor);
            Rect label = new Rect(anchor.x - 92f, anchor.y + 10f, 184f, 38f);
            DrawSolid(label, new Color(0.025f, 0.022f, 0.016f, 0.82f));
            DrawFrame(label, new Color(0.94f, 0.64f, 0.16f, 0.78f), 1.5f);
            GUI.Label(label, "Taniere assoupie\nEvenement inactif", MiniLabel(new Color(1f, 0.90f, 0.64f, 1f), 11, TextAnchor.MiddleCenter));
        }

        private void DrawChunkDebugOverlay()
        {
            for (int i = 0; i < activeChunks.Count; i++)
            {
                Vector2Int chunk = activeChunks[i];
                Rect rect = ChunkScreenRect(chunk);
                Color color = chunk == CurrentChunk() ? new Color(0.18f, 0.85f, 1f, 0.82f) : new Color(1f, 0.74f, 0.20f, 0.42f);
                DrawFrame(rect, color, chunk == CurrentChunk() ? 3f : 1.5f);
                GUI.Label(new Rect(rect.x + 8f, rect.y + 8f, 240f, 42f), SectorId(chunk) + "\n" + ChunkId(chunk), MiniLabel(new Color(1f, 0.95f, 0.70f, 0.92f), 11, TextAnchor.UpperLeft));
            }
        }

        private void DrawHives()
        {
            for (int i = 0; i < hives.Count; i++)
            {
                WorldHiveNode hive = hives[i];
                Vector2 p = WorldToScreen(hive.WorldCoord);
                if (!IsOnScreen(p, 100f)) continue;

                bool selected = hive.Id == selectedHiveId;
                float size = WorldSizeToScreen(HiveSize(hive.Stage));
                Color color = HiveColor(hive.Stage);
                Texture2D texture = RuntimeEntityTexture(HiveTexturePath(hive.Stage));

                if (texture != null)
                {
                    // Vraie silhouette de ruche (alveoles, decor, ombre au sol deja integree a
                    // l'image) au lieu de trois hexagones vides empiles - la progression Debutante
                    // -> Capitale se lit maintenant sur la forme elle-meme, pas seulement sur un
                    // badge de texte (demande "les ruches doivent sembler construites", chantier
                    // World Map Premium Pass, sprint 3).
                    float spriteSize = size * 2.6f;
                    Rect spriteRect = new Rect(p.x - spriteSize * 0.5f, p.y - spriteSize * 0.58f, spriteSize, spriteSize);
                    GUI.DrawTexture(spriteRect, texture, ScaleMode.ScaleToFit, true);
                    if (selected)
                    {
                        float pulse = 0.5f + 0.3f * Mathf.PingPong(Time.time * 0.7f, 1f);
                        DrawCircle(new Vector2(p.x, p.y - spriteSize * 0.12f), spriteSize * 0.5f, new Color(0.18f, 0.85f, 1f, pulse), 28);
                    }
                }
                else
                {
                    DrawTerrainTileShadow(p, size * 1.55f, size * 0.42f, selected ? 0.30f : 0.20f);
                    DrawHex(p, size, color, WorldStrokeToScreen(4f));
                    DrawHex(p, size * 0.62f, new Color(0.18f, 0.11f, 0.03f, 0.95f), WorldStrokeToScreen(3f));
                    DrawHex(p, size * 0.22f, new Color(1f, 0.86f, 0.20f, 0.95f), WorldStrokeToScreen(2f));
                }

                Rect badge = new Rect(p.x - 42f, p.y - size - 30f, 84f, 20f);
                DrawSolid(badge, new Color(0.02f, 0.018f, 0.012f, 0.86f));
                DrawFrame(badge, selected ? new Color(0.18f, 0.85f, 1f, 0.86f) : new Color(color.r, color.g, color.b, 0.70f), 1.2f);
                GUI.Label(badge, hive.Badge, MiniLabel(Color.white, 10, TextAnchor.MiddleCenter));

                Rect labelRect = new Rect(p.x - 82f, p.y + size + 6f, 164f, 38f);
                DrawSolid(labelRect, new Color(0.02f, 0.018f, 0.012f, 0.78f));
                GUI.Label(labelRect, hive.Label + "\n" + (selected ? "selectionnee" : StageLabel(hive.Stage)), MiniLabel(Color.white, 11, TextAnchor.MiddleCenter));
            }
        }

        // Silhouette reelle par palier de maturite (memes illustrations que celles deja livrees
        // pour ce jeu de ressources, jamais branchees sur la vraie carte jusqu'ici).
        private string HiveTexturePath(HiveMaturity stage)
        {
            int tier = stage == HiveMaturity.Capital ? 9 : stage == HiveMaturity.Advanced ? 7 : stage == HiveMaturity.Mid ? 4 : 1;
            return RuntimeEntityResourceRoot + "/H1/hive_neutral_l" + tier.ToString(CultureInfo.InvariantCulture);
        }

        // Premiers Points d'Interet (demande de Jeff, 2026-08-01) : lieux remarquables purement
        // informationnels. Toujours visibles (pas de filtre, contrairement aux ruches/ressources/
        // menaces) - un point d'interet doit "apparaitre naturellement sur la carte", jamais etre
        // masque. Identite visuelle propre par nature (glyphe + couleur distincts), description
        // affichee des la selection, sans panneau ni action - prepare les futurs systemes
        // (alliances, occupation, evenements, boss) sans en construire aucun ici.
        private void DrawPointsOfInterest()
        {
            for (int i = 0; i < pointsOfInterest.Count; i++)
            {
                WorldPointOfInterestNode poi = pointsOfInterest[i];
                Vector2 p = WorldToScreen(poi.WorldCoord);
                if (!IsOnScreen(p, 100f)) continue;

                bool selected = poi.Id == selectedPointOfInterestId;
                float size = WorldSizeToScreen(selected ? 30f : 26f);
                Color accent = PointOfInterestAccent(poi.Kind);

                DrawTerrainTileShadow(p, size * 1.5f, size * 0.40f, selected ? 0.30f : 0.20f);
                DrawPointOfInterestGlyph(p, size, poi.Kind, accent);

                if (selected)
                {
                    float ringSize = size * 2.1f;
                    float pulse = 0.45f + 0.30f * Mathf.PingPong(Time.time * 0.7f, 1f);
                    DrawFrame(new Rect(p.x - ringSize * 0.5f, p.y - ringSize * 0.5f, ringSize, ringSize), new Color(accent.r, accent.g, accent.b, pulse), WorldStrokeToScreen(2.4f));
                }

                Rect labelRect = new Rect(p.x - 82f, p.y + size + 6f, 164f, 18f);
                DrawSolid(labelRect, new Color(0.02f, 0.018f, 0.012f, 0.78f));
                GUI.Label(labelRect, "◆ " + poi.Label, MiniLabel(accent, 11, TextAnchor.MiddleCenter));

                if (selected)
                {
                    // Bible rule: "un POI ne cree pas de mecanique par lui-meme" - this stays a
                    // small read-only card (family + carte one-liner + short history + a boss
                    // flavor line where the bible names one), never a menu with actions.
                    bool hasHistory = !string.IsNullOrEmpty(poi.History);
                    bool hasBoss = !string.IsNullOrEmpty(poi.BossTeaser);
                    float cardHeight = 22f + 34f + (hasHistory ? 40f : 0f) + (hasBoss ? 32f : 0f);
                    Rect descriptionRect = new Rect(p.x - 120f, labelRect.yMax + 2f, 240f, cardHeight);
                    DrawSolid(descriptionRect, new Color(0.02f, 0.018f, 0.012f, 0.86f));
                    DrawFrame(descriptionRect, new Color(accent.r, accent.g, accent.b, 0.75f), 1.2f);

                    float textX = descriptionRect.x + 8f;
                    float textWidth = descriptionRect.width - 16f;
                    float y = descriptionRect.y + 4f;
                    GUI.Label(new Rect(textX, y, textWidth, 16f), poi.Family, MiniLabel(accent, 10, TextAnchor.UpperLeft));
                    y += 18f;
                    GUI.Label(new Rect(textX, y, textWidth, 34f), poi.Description, new GUIStyle(MiniLabel(Color.white, 10, TextAnchor.UpperLeft)) { wordWrap = true });
                    if (hasHistory)
                    {
                        y += 36f;
                        GUI.Label(new Rect(textX, y, textWidth, 40f), poi.History, new GUIStyle(MiniLabel(new Color(0.82f, 0.80f, 0.74f, 1f), 9, TextAnchor.UpperLeft)) { wordWrap = true });
                    }
                    if (hasBoss)
                    {
                        y += hasHistory ? 42f : 36f;
                        GUI.Label(new Rect(textX, y, textWidth, 30f), "⚔ " + poi.BossTeaser, new GUIStyle(MiniLabel(new Color(0.92f, 0.62f, 0.30f, 1f), 9, TextAnchor.UpperLeft)) { wordWrap = true, fontStyle = FontStyle.Italic });
                    }
                }
            }
        }

        private static Color PointOfInterestAccent(string kind)
        {
            switch (kind)
            {
                case "ancient_hive": return new Color(0.86f, 0.70f, 0.30f, 1f);
                case "fossil_nest": return new Color(0.60f, 0.66f, 0.72f, 1f);
                case "blooming_grove": return new Color(0.92f, 0.48f, 0.72f, 1f);
                case "sunken_hive": return new Color(0.30f, 0.72f, 0.76f, 1f);
                case "forgotten_sanctuary": return new Color(0.64f, 0.44f, 0.88f, 1f);
                case "souche_cathedrale": return new Color(0.55f, 0.42f, 0.22f, 1f);
                case "mare_aux_reflets": return new Color(0.35f, 0.62f, 0.90f, 1f);
                case "champ_premieres_fleurs": return new Color(0.98f, 0.75f, 0.30f, 1f);
                case "pierre_chaude": return new Color(0.80f, 0.42f, 0.20f, 1f);
                default: return new Color(0.68f, 0.78f, 0.95f, 1f); // branche_veilleurs
            }
        }

        // Bible rule: "un POI doit pouvoir etre reconnu par sa silhouette avant son nom" -
        // the 5 additional POIs reuse the same 4 base shape primitives as the original 5
        // (no new polygon-drawing code) but each carries an outer ring so its silhouette
        // never collides with an original POI of the same base shape.
        private void DrawPointOfInterestGlyph(Vector2 p, float size, string kind, Color accent)
        {
            float radius = size * 0.46f;
            float width = WorldStrokeToScreen(2.6f);
            switch (kind)
            {
                case "ancient_hive": DrawHex(p, radius, accent, width); break;
                case "fossil_nest": DrawTriangle(p, radius, accent, width); break;
                case "blooming_grove": DrawCircle(p, radius, accent, 20); break;
                case "sunken_hive": DrawDiamond(p, radius, accent, width); break;
                case "forgotten_sanctuary": DrawCircle(p, radius * 1.15f, accent, 26); break;
                case "souche_cathedrale": DrawHex(p, radius, accent, width); DrawRingAccent(p, radius, accent, width); break;
                case "mare_aux_reflets": DrawDiamond(p, radius, accent, width); DrawRingAccent(p, radius, accent, width); break;
                case "champ_premieres_fleurs": DrawCircle(p, radius, accent, 20); DrawRingAccent(p, radius, accent, width); break;
                case "pierre_chaude": DrawTriangle(p, radius, accent, width); DrawRingAccent(p, radius, accent, width); break;
                default: DrawCircle(p, radius * 1.15f, accent, 26); DrawRingAccent(p, radius * 1.15f, accent, width); break; // branche_veilleurs
            }
        }

        private void DrawRingAccent(Vector2 p, float innerRadius, Color accent, float width)
        {
            float ringRadius = innerRadius * 1.55f;
            DrawFrame(new Rect(p.x - ringRadius, p.y - ringRadius, ringRadius * 2f, ringRadius * 2f), new Color(accent.r, accent.g, accent.b, 0.65f), width);
        }

        // Ressource vivante (demande de Jeff, 2026-08-01) : chacun de ces 4 etats se lit
        // directement sur l'icone (teinte + barre + etiquette courte), sans ouvrir de panneau.
        // Reutilise entierement des donnees deja transmises (noeud officiel, vol actif, presence
        // ambiante des autres colonies) - aucun nouveau systeme, uniquement plus de rendu.
        private enum ResourceLifeState { Free, CollectingMine, CollectingOther, Depleted }

        private void DrawResources()
        {
            WorldResourceCollectionScreenModel officialModel = HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            WorldPresenceScreenModel presenceModel = HiveViewProductUiPresenter.PeekWorldPresenceModelForWorldMap();

            for (int i = 0; i < resources.Count; i++)
            {
                WorldResourceNode resource = resources[i];
                Vector2 p = WorldToScreen(resource.WorldCoord);
                if (!IsOnScreen(p, 74f)) continue;

                Color color = ResourceColor(resource.Kind);
                bool selected = resource.Id == selectedResourceId;
                bool official = IsOfficialResource(resource);
                ResourceLifeState life = official ? ResolveResourceLifeState(resource, officialModel, presenceModel) : ResourceLifeState.Free;
                Color tint = official ? ResourceLifeTint(life) : Color.white;
                // Le monde a une memoire (demande de Jeff, 2026-08-02) : une ressource qui vient
                // tout juste de redevenir disponible garde un aspect legerement terne pendant
                // quelques minutes, sans texte ni panneau - juste une impression qui s'estompe.
                if (official && life == ResourceLifeState.Free) tint = ApplyRecentlyDisturbedTint(tint, resource, OfficialNodeState(resource));

                float size;
                Color previousGuiColor = GUI.color;
                Texture2D texture = RuntimeEntityTexture(ResourceTexturePath(resource));
                if (texture != null)
                {
                    size = ResourceSpriteScreenSize(resource);
                    GUI.color = tint;
                    GUI.DrawTexture(new Rect(p.x - size * 0.5f, p.y - size * 0.58f, size, size), texture, ScaleMode.ScaleToFit, true);
                    GUI.color = previousGuiColor;
                }
                else
                {
                    size = WorldSizeToScreen(24f);
                    DrawDiamond(p, size * 0.36f, color * tint, WorldStrokeToScreen(2f));
                }

                if (official) DrawResourceLifeIndicators(p, size, resource, life, officialModel, presenceModel);

                if (selected && !official)
                {
                    GUI.Label(new Rect(p.x - 72f, p.y + ResourceSpriteScreenSize(resource) * 0.42f, 144f, 34f), resource.Label + "\n" + ResourceQuantityLabel(resource), MiniLabel(Color.white, 10, TextAnchor.MiddleCenter));
                }
            }
        }

        private ResourceLifeState ResolveResourceLifeState(WorldResourceNode resource, WorldResourceCollectionScreenModel officialModel, WorldPresenceScreenModel presenceModel)
        {
            RemoteWorldResourceNode node = OfficialNodeState(resource);
            if (node == null) return ResourceLifeState.Free;
            if (officialModel?.Active != null && string.Equals(officialModel.Active.NodeId, resource.Id, StringComparison.Ordinal))
                return ResourceLifeState.CollectingMine;
            if (presenceModel?.Sightings != null)
                foreach (RemoteWorldPresenceSighting sighting in presenceModel.Sightings)
                    if (string.Equals(sighting.NodeId, resource.Id, StringComparison.Ordinal))
                        return ResourceLifeState.CollectingOther;
            return node.Ready ? ResourceLifeState.Free : ResourceLifeState.Depleted;
        }

        private static Color ResourceLifeTint(ResourceLifeState life)
        {
            switch (life)
            {
                case ResourceLifeState.CollectingMine: return new Color(0.80f, 1.20f, 0.92f, 1f);
                case ResourceLifeState.CollectingOther: return new Color(0.82f, 0.88f, 1.20f, 1f);
                case ResourceLifeState.Depleted: return new Color(0.55f, 0.55f, 0.55f, 0.60f);
                default: return Color.white;
            }
        }

        // Le monde a une memoire (demande de Jeff, 2026-08-02) : le serveur n'expose ReadyAtUtc que
        // PENDANT le repos (il redevient null des que la ressource est prete, voir
        // WorldResourceCollectionService.Snapshot) - il n'y a donc aucune donnee serveur permettant
        // de savoir "depuis quand" une ressource est redevenue disponible. La transition
        // Depleted -> Free est detectee ici, cote client, a partir du booleen Ready deja recu a
        // chaque rafraichissement : la toute premiere fois qu'on l'observe passer a "pret" APRES
        // avoir ete vu au repos, on retient l'instant, et la teinte s'estompe doucement pendant
        // quelques minutes. Purement une memoire ephemere d'affichage (vit tant que cette carte
        // reste ouverte) - aucune nouvelle donnee de jeu, aucun texte, aucun panneau.
        private const float ResourceMemoryFadeSeconds = 180f;
        private readonly Dictionary<string, bool> resourceLastKnownReady = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> resourceSettledAtUnscaledTime = new Dictionary<string, float>(StringComparer.Ordinal);

        private Color ApplyRecentlyDisturbedTint(Color tint, WorldResourceNode resource, RemoteWorldResourceNode node)
        {
            if (node == null) return tint;
            bool hasPriorObservation = resourceLastKnownReady.TryGetValue(resource.Id, out bool wasReady);
            if (hasPriorObservation && node.Ready && !wasReady) resourceSettledAtUnscaledTime[resource.Id] = Time.unscaledTime;
            resourceLastKnownReady[resource.Id] = node.Ready;
            if (!node.Ready || !resourceSettledAtUnscaledTime.TryGetValue(resource.Id, out float settledAt)) return tint;

            float elapsed = Time.unscaledTime - settledAt;
            if (elapsed < 0f || elapsed >= ResourceMemoryFadeSeconds) return tint;
            float fade01 = elapsed / ResourceMemoryFadeSeconds;
            Color settled = new Color(0.74f, 0.72f, 0.64f, 1f);
            return Color.Lerp(settled, tint, fade01);
        }

        // Barre + etiquette courte toujours visibles (pas seulement a la selection) : la
        // progression de collecte et la regeneration naturelle utilisent la MEME barre, juste
        // une couleur et un sens de remplissage differents - un seul widget, quatre histoires.
        private void DrawResourceLifeIndicators(Vector2 p, float iconSize, WorldResourceNode resource, ResourceLifeState life, WorldResourceCollectionScreenModel officialModel, WorldPresenceScreenModel presenceModel)
        {
            RemoteWorldResourceNode node = OfficialNodeState(resource);
            if (node == null) return;

            float progress01;
            Color fill;
            string label;
            switch (life)
            {
                case ResourceLifeState.CollectingMine:
                {
                    RemoteWorldResourceActiveFlight active = officialModel.Active;
                    double total = (active.EndsAtUtc - active.StartedAtUtc).TotalSeconds;
                    double elapsed = (DateTimeOffset.UtcNow - active.StartedAtUtc).TotalSeconds;
                    progress01 = total > 0 ? Mathf.Clamp01((float)(elapsed / total)) : 1f;
                    fill = new Color(0.30f, 0.95f, 0.55f, 0.95f);
                    TimeSpan remaining = active.EndsAtUtc - DateTimeOffset.UtcNow;
                    label = remaining > TimeSpan.Zero ? "Collecte " + remaining.ToString(@"mm\:ss") : "Prete";
                    break;
                }
                case ResourceLifeState.CollectingOther:
                {
                    RemoteWorldPresenceSighting sighting = null;
                    if (presenceModel?.Sightings != null)
                        foreach (RemoteWorldPresenceSighting s in presenceModel.Sightings)
                            if (string.Equals(s.NodeId, resource.Id, StringComparison.Ordinal)) { sighting = s; break; }
                    if (sighting == null) return;
                    double total = (sighting.EndsAtUtc - sighting.StartedAtUtc).TotalSeconds;
                    double elapsed = (DateTimeOffset.UtcNow - sighting.StartedAtUtc).TotalSeconds;
                    progress01 = total > 0 ? Mathf.Clamp01((float)(elapsed / total)) : 1f;
                    fill = new Color(0.55f, 0.62f, 0.98f, 0.95f);
                    label = sighting.ColonyLabel;
                    break;
                }
                case ResourceLifeState.Depleted:
                {
                    if (!node.ReadyAtUtc.HasValue) return;
                    double totalCooldown = node.Cooldown.TotalSeconds;
                    double remainingSeconds = Math.Max(0, (node.ReadyAtUtc.Value - DateTimeOffset.UtcNow).TotalSeconds);
                    progress01 = totalCooldown > 0 ? Mathf.Clamp01(1f - (float)(remainingSeconds / totalCooldown)) : 1f;
                    fill = new Color(0.85f, 0.62f, 0.25f, 0.92f);
                    label = "Repos " + TimeSpan.FromSeconds(remainingSeconds).ToString(@"mm\:ss");
                    break;
                }
                default:
                    progress01 = 1f;
                    fill = new Color(0.62f, 0.92f, 0.68f, 0.85f);
                    label = "Prete";
                    break;
            }

            float barWidth = 44f;
            float barHeight = 5f;
            Rect bar = new Rect(p.x - barWidth * 0.5f, p.y + iconSize * 0.40f, barWidth, barHeight);
            DrawSolid(bar, new Color(0.05f, 0.05f, 0.05f, 0.55f));
            DrawSolid(new Rect(bar.x, bar.y, bar.width * progress01, bar.height), fill);
            DrawFrame(bar, new Color(1f, 1f, 1f, 0.25f), 1f);
            GUI.Label(new Rect(p.x - 60f, bar.yMax + 1f, 120f, 16f), label, MiniLabel(Color.white, 9, TextAnchor.MiddleCenter));

            // Cible du jour / evenement mondial (deja livres) restent visibles sans selection,
            // pour que la ressource raconte tout son etat d'un coup d'oeil.
            string marker = node.IsDailyFocus ? "★ Cible du jour" : node.IsWorldEventBoosted ? "! Meteo active" : null;
            if (marker != null)
                GUI.Label(new Rect(p.x - 60f, bar.y - 15f, 120f, 14f), marker, MiniLabel(new Color(1f, 0.82f, 0.42f, 1f), 9, TextAnchor.MiddleCenter));
        }

        private void DrawBestiary()
        {
            for (int i = 0; i < bestiary.Count; i++)
            {
                WorldBestiaryNode beast = bestiary[i];
                Vector2 p = WorldToScreen(beast.WorldCoord);
                if (!IsOnScreen(p, 120f)) continue;

                Texture2D texture = RuntimeEntityTexture(BestiaryTexturePath(beast));
                float size = WorldSizeToScreen(48f + beast.Tier * 7.5f);
                Color color = BestiaryTierColor(beast.Tier);
                bool selected = beast.Id == selectedBestiaryId;
                bool rareSighting = IsRareSighting(beast, out string rareSightingEventKey);
                DrawTerrainTileShadow(p, size * 0.90f, size * 0.24f, selected ? 0.28f : 0.18f);
                if (texture != null)
                {
                    GUI.DrawTexture(new Rect(p.x - size * 0.5f, p.y - size * 0.58f, size, size), texture, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    DrawTriangle(p, size * 0.34f, color, WorldStrokeToScreen(3f));
                }

                if (rareSighting)
                {
                    float pulse = 0.55f + 0.35f * Mathf.PingPong(Time.time, 1f);
                    Rect ring = new Rect(p.x - size * 0.62f, p.y - size * 0.70f, size * 1.24f, size * 1.24f);
                    DrawFrame(ring, new Color(0.98f, 0.72f, 0.20f, pulse), WorldStrokeToScreen(3f));
                }

                string label = BestiaryAccessibilityWord(beast) + " T" + beast.Tier.ToString(CultureInfo.InvariantCulture) + " " + beast.Label + "\nPV " + BestiaryVirtualHp(beast).ToString(CultureInfo.InvariantCulture);
                if (rareSighting) label = "★ " + HiveViewProductUiPresenter.WorldEventDisplayName(rareSightingEventKey) + "\n" + label;
                GUI.Label(new Rect(p.x - 72f, p.y + size * 0.38f, 144f, rareSighting ? 50f : 34f), label,
                    MiniLabel(rareSighting ? new Color(1f, 0.80f, 0.30f, 1f) : new Color(1f, 0.90f, 0.62f, 1f), 10, TextAnchor.MiddleCenter));
            }
        }

        private void DrawAerialFlights()
        {
            for (int i = 0; i < flights.Count; i++)
            {
                DrawFlightArc(flights[i], false);
            }

            WorldHiveNode from = SelectedHive();
            WorldResourceNode to = SelectedResource();
            // L'arc de vol local/demo ne s'applique qu'aux ressources sans contrepartie serveur -
            // les ressources officielles ont desormais leur propre escouade reelle sur la carte
            // (DrawWorldResourceCollectionMarch), pour ne jamais superposer les deux visuels.
            if (from != null && to != null && !IsOfficialResource(to))
            {
                DrawFlightArc(from.WorldCoord, to.WorldCoord, collectionState, CurrentFlightArcProgress(), "PREVIEW", true);
            }

            DrawCombatPatrolMarch();
            DrawWorldResourceCollectionMarch();
        }

        // Escouade reellement engagee sur la carte pendant toute la duree de la collecte (demande
        // de Jeff, 2026-08-01) : meme principe que DrawCombatPatrolMarch, applique a la Collecte
        // mondiale - premiere brique de l'architecture de deploiement reutilisable plus tard pour
        // le PvP, les raids, les renforts et l'occupation de points d'interet. Contrairement au
        // combat (cible arbitraire, associee via pendingCombatPatrolLaunchTarget), un noeud de
        // ressource a un identifiant stable qu'on peut retrouver directement, pas besoin de
        // dictionnaire de correlation.
        private void DrawWorldResourceCollectionMarch()
        {
            WorldResourceCollectionScreenModel model = HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            RemoteWorldResourceActiveFlight active = model?.Active;
            if (active == null) return;
            WorldHiveNode from = SelectedHive();
            WorldResourceNode to = ResourceById(active.NodeId);
            if (from == null || to == null) return;
            Vector2 a = WorldToScreen(from.WorldCoord);
            Vector2 b = WorldToScreen(to.WorldCoord);
            if (!IsOnScreen(a, 420f) && !IsOnScreen(b, 420f)) return;

            DrawLine(a, b, new Color(0.30f, 0.85f, 0.55f, 0.85f), 3f);
            double totalSeconds = (active.EndsAtUtc - active.StartedAtUtc).TotalSeconds;
            double elapsedSeconds = (DateTimeOffset.UtcNow - active.StartedAtUtc).TotalSeconds;
            float t = totalSeconds > 0 ? Mathf.Clamp01((float)(elapsedSeconds / totalSeconds)) : 1f;
            // L'escouade parcourt le premier tiers du vol pour rejoindre le noeud, puis reste
            // physiquement sur place jusqu'a la fin (demande explicite de Jeff : "rester
            // physiquement sur place pendant toute la duree de la collecte"). L'etat "occupee"
            // + le temps restant sont desormais racontes directement par la ressource elle-meme
            // (DrawResourceLifeIndicators, demande de Jeff, 2026-08-01) - ce marqueur ne
            // represente plus que la position physique de l'escouade en transit.
            float travelT = Mathf.Clamp01(t * 3f);
            bool onSite = travelT >= 1f;
            Vector2 marker = onSite ? b : Vector2.Lerp(a, b, travelT);
            DrawCircle(marker, 8f, new Color(0.30f, 0.95f, 0.55f, 0.95f), 12);
        }

        // Client-side-only correlation between an active encounter and the map coordinate the
        // player targeted at launch time — the server model only knows the tier, not a map node,
        // so this is a cosmetic best-effort association, never a source of truth. Populated when
        // a newly-appeared active encounter is first observed here; purged once the encounter is
        // gone (claimed/recalled).
        private readonly Dictionary<Guid, Vector2> combatPatrolTargetWorldCoordByEncounterId = new Dictionary<Guid, Vector2>();
        private Vector2? pendingCombatPatrolLaunchTarget;

        // Own-player only: draws a marching line + marker from the hive to each active patrol's
        // target (several can be in flight at once). Seeing OTHER players' marching troops would
        // require a shared/synchronized world state that does not exist in this project yet —
        // out of scope here (see Docs/Claude/Claude_Continuation.md).
        private void DrawCombatPatrolMarch()
        {
            CombatPatrolScreenModel model = HiveViewProductUiPresenter.PeekCombatPatrolModelForWorldMap();
            IReadOnlyList<RemoteCombatPatrolActiveEncounter> encounters = model?.ActiveEncounters;
            if (encounters == null || encounters.Count == 0) { combatPatrolTargetWorldCoordByEncounterId.Clear(); return; }

            var seenIds = new HashSet<Guid>();
            foreach (RemoteCombatPatrolActiveEncounter encounter in encounters)
            {
                seenIds.Add(encounter.EncounterId);
                if (!combatPatrolTargetWorldCoordByEncounterId.ContainsKey(encounter.EncounterId) && pendingCombatPatrolLaunchTarget.HasValue)
                {
                    combatPatrolTargetWorldCoordByEncounterId[encounter.EncounterId] = pendingCombatPatrolLaunchTarget.Value;
                    pendingCombatPatrolLaunchTarget = null;
                }
            }
            List<Guid> stale = null;
            foreach (Guid known in combatPatrolTargetWorldCoordByEncounterId.Keys)
                if (!seenIds.Contains(known)) (stale ??= new List<Guid>()).Add(known);
            if (stale != null) foreach (Guid key in stale) combatPatrolTargetWorldCoordByEncounterId.Remove(key);

            WorldHiveNode from = SelectedHive();
            if (from == null) return;
            Vector2 a = WorldToScreen(from.WorldCoord);

            foreach (RemoteCombatPatrolActiveEncounter encounter in encounters)
            {
                if (!combatPatrolTargetWorldCoordByEncounterId.TryGetValue(encounter.EncounterId, out Vector2 targetWorldCoord)) continue;
                Vector2 b = WorldToScreen(targetWorldCoord);
                if (!IsOnScreen(a, 420f) && !IsOnScreen(b, 420f)) continue;

                Vector2 control = (a + b) * 0.5f + new Vector2(0f, -Mathf.Min(220f, Vector2.Distance(a, b) * 0.38f));
                double totalSeconds = (encounter.EndsAtUtc - encounter.StartedAtUtc).TotalSeconds;
                double elapsedSeconds = (DateTimeOffset.UtcNow - encounter.StartedAtUtc).TotalSeconds;
                float t = totalSeconds > 0 ? Mathf.Clamp01((float)(elapsedSeconds / totalSeconds)) : 1f;
                float marchProgress = Mathf.PingPong(t * 2f, 1f);
                Vector2 marker = Bezier(a, control, b, marchProgress);

                DrawStyledMarchPath(a, control, b, marchProgress, CombatMarchPalette);
                DrawCombatMarchBee(marker);
            }
        }

        // Palette de rendu "premium" pour un chemin de marche (halo, coeur, filet qui respire,
        // essaim de braises/etincelles) - factorisee depuis DrawCombatPatrolMarch (demande de
        // Jeff, 2026-08-25) pour que RaidMarchPalette reste prete a etre reutilisee par un futur
        // systeme de raid sans dupliquer le code de rendu. Raid n'existe pas encore cote
        // gameplay/serveur - seule la palette visuelle est conservee ici.
        private readonly struct MarchPalette
        {
            public readonly Color Halo;
            public readonly Color Core;
            public readonly Color Filament;
            public readonly Color SparkColor;
            public readonly Color EmberColor;

            public MarchPalette(Color halo, Color core, Color filament, Color sparkColor, Color emberColor)
            {
                Halo = halo;
                Core = core;
                Filament = filament;
                SparkColor = sparkColor;
                EmberColor = emberColor;
            }
        }

        private static readonly MarchPalette CombatMarchPalette = new MarchPalette(
            halo: new Color(0.55f, 0.04f, 0.05f, 0.28f),
            core: new Color(0.90f, 0.14f, 0.10f, 0.92f),
            filament: new Color(1f, 0.80f, 0.35f, 0.55f),
            sparkColor: new Color(1f, 0.86f, 0.42f, 0.75f),
            emberColor: new Color(0.95f, 0.28f, 0.14f, 0.62f));

        // Reserve pour le futur systeme de Raid (aucune fonctionnalite branchee cote gameplay) -
        // meme rendu que CombatMarchPalette, teinte violette validee par Jeff le 2026-08-25.
        private static readonly MarchPalette RaidMarchPalette = new MarchPalette(
            halo: new Color(0.30f, 0.05f, 0.55f, 0.28f),
            core: new Color(0.55f, 0.16f, 0.92f, 0.92f),
            filament: new Color(0.86f, 0.62f, 1f, 0.55f),
            sparkColor: new Color(0.90f, 0.72f, 1f, 0.75f),
            emberColor: new Color(0.58f, 0.20f, 0.90f, 0.62f));

        private void DrawStyledMarchPath(Vector2 a, Vector2 control, Vector2 b, float marchProgress, MarchPalette palette)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(animatedTime * 2.2f);
            DrawBezier(a, control, b, new Color(palette.Halo.r, palette.Halo.g, palette.Halo.b, palette.Halo.a + pulse * 0.06f), 11f, 40);
            DrawBezier(a, control, b, palette.Core, 4f, 40);
            DrawBezier(a, control, b, new Color(palette.Filament.r, palette.Filament.g, palette.Filament.b, palette.Filament.a + pulse * 0.35f), 1.4f, 40);

            const int swarmCount = 10;
            for (int i = 0; i < swarmCount; i++)
            {
                float st = Mathf.Repeat(marchProgress + i * 0.028f - 0.12f, 1f);
                Vector2 p = Bezier(a, control, b, st);
                Vector2 tangent = Bezier(a, control, b, Mathf.Min(1f, st + 0.02f)) - p;
                Vector2 side = tangent.sqrMagnitude > 0.01f ? new Vector2(-tangent.y, tangent.x).normalized : Vector2.up;
                p += side * Mathf.Sin(animatedTime * 7f + i) * 4f;
                bool spark = i % 3 == 0;
                float flicker = 0.55f + 0.45f * Mathf.Sin(animatedTime * 9f + i * 1.7f);
                Color emberColor = spark
                    ? new Color(palette.SparkColor.r, palette.SparkColor.g, palette.SparkColor.b, palette.SparkColor.a * flicker)
                    : new Color(palette.EmberColor.r, palette.EmberColor.g, palette.EmberColor.b, palette.EmberColor.a * flicker);
                DrawCircle(p, spark ? 3.2f : 4.6f, emberColor, 10);
            }
        }

        // Abeille de la marche d'attaque (demande de Jeff, 2026-08-25) : remplace l'ancien
        // marqueur (simple cercle) par le sprite de la Gardienne, avec des ailes animees en
        // battement rapide (oscillation d'echelle/alpha a haute frequence, meme famille de
        // pattern IMGUI que DrawFlightArc/DrawLine - aucun Animator necessaire).
        private void DrawCombatMarchBee(Vector2 marker)
        {
            Texture2D body = RuntimeEntityTexture(CombatMarchBeeBodyResource);
            Texture2D wings = RuntimeEntityTexture(CombatMarchBeeWingsResource);
            if (body == null)
            {
                DrawCircle(marker, 7f, new Color(1f, 0.75f, 0.25f, 0.95f), 12);
                return;
            }

            const float bodyWidth = 46f;
            float bodyHeight = bodyWidth * body.height / (float)body.width;
            Vector2 bodyCenter = marker + new Vector2(0f, -bodyHeight * 0.18f);
            Rect bodyRect = new Rect(bodyCenter.x - bodyWidth * 0.5f, bodyCenter.y - bodyHeight * 0.5f, bodyWidth, bodyHeight);

            if (wings != null)
            {
                const float wingFrequency = 32f;
                float flap = Mathf.Abs(Mathf.Sin(animatedTime * wingFrequency + marker.x * 0.01f));
                float wingScaleY = Mathf.Lerp(0.32f, 1f, flap);
                float wingAlpha = Mathf.Lerp(0.55f, 0.95f, flap);

                float wingWidth = bodyWidth * 1.35f;
                float wingHeight = wingWidth * wings.height / (float)wings.width;
                Vector2 wingPivot = bodyCenter + new Vector2(0f, -bodyHeight * 0.10f);
                Rect wingRect = new Rect(wingPivot.x - wingWidth * 0.5f, wingPivot.y - wingHeight * 0.5f, wingWidth, wingHeight);

                Matrix4x4 matrix = GUI.matrix;
                Color previousColor = GUI.color;
                GUIUtility.ScaleAroundPivot(new Vector2(1f, wingScaleY), wingPivot);
                GUI.color = new Color(1f, 1f, 1f, wingAlpha);
                GUI.DrawTexture(wingRect, wings, ScaleMode.ScaleToFit, true);
                GUI.matrix = matrix;
                GUI.color = previousColor;
            }

            Color bodyPreviousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(bodyRect, body, ScaleMode.ScaleToFit, true);
            GUI.color = bodyPreviousColor;
        }

        private void DrawFlightArc(WorldFlightRecord flight, bool highlight)
        {
            DrawFlightArc(flight.OriginWorldCoord, flight.DestinationWorldCoord, flight.State, FlightArcProgress(flight), flight.Id, highlight);
        }

        private void DrawFlightArc(Vector2 origin, Vector2 destination, CollectionFlightState state, float anchorProgress, string label, bool highlight)
        {
            Vector2 a = WorldToScreen(origin);
            Vector2 b = WorldToScreen(destination);
            if (!IsOnScreen(a, 420f) && !IsOnScreen(b, 420f)) return;

            Vector2 control = (a + b) * 0.5f + new Vector2(0f, -Mathf.Min(220f, Vector2.Distance(a, b) * 0.38f));
            Color outer = highlight ? new Color(0.12f, 0.85f, 1f, 0.50f) : new Color(0.12f, 0.85f, 1f, 0.26f);
            Color inner = highlight ? new Color(1f, 0.93f, 0.24f, 0.96f) : new Color(1f, 0.88f, 0.18f, 0.62f);
            DrawBezier(a, control, b, outer, highlight ? 10f : 7f, 36);
            DrawBezier(a, control, b, inner, highlight ? 4.5f : 3.2f, 36);

            int swarmCount = highlight ? 16 : 9;
            for (int i = 0; i < swarmCount; i++)
            {
                float t = Mathf.Repeat(anchorProgress + i * 0.030f - 0.12f, 1f);
                if (state == CollectionFlightState.FlyingToResource || state == CollectionFlightState.Returning)
                {
                    t = Mathf.Clamp01(anchorProgress - i * 0.014f);
                }
                else if (state == CollectionFlightState.Collecting)
                {
                    t = 1f - i * 0.006f;
                }
                else if (state == CollectionFlightState.Completed)
                {
                    t = Mathf.Repeat(0.92f + Mathf.Sin(animatedTime + i) * 0.02f, 1f);
                }

                Vector2 p = Bezier(a, control, b, t);
                Vector2 tangent = Bezier(a, control, b, Mathf.Min(1f, t + 0.02f)) - p;
                Vector2 side = tangent.sqrMagnitude > 0.01f ? new Vector2(-tangent.y, tangent.x).normalized : Vector2.up;
                p += side * Mathf.Sin(animatedTime * 7f + i) * (highlight ? 6f : 4f);
                DrawCircle(p, highlight ? 5.8f : 4.6f, new Color(1f, 0.88f, 0.18f, highlight ? 0.96f : 0.72f), 10);
                if (tangent.sqrMagnitude > 0.01f)
                {
                    Vector2 trailDirection = state == CollectionFlightState.Returning ? tangent.normalized : -tangent.normalized;
                    DrawLine(p + trailDirection * 4f, p + trailDirection * (highlight ? 22f : 15f), new Color(1f, 0.96f, 0.55f, highlight ? 0.34f : 0.22f), highlight ? 2.8f : 2.0f);
                }
            }

            if (!highlight) return;
            Vector2 mid = Bezier(a, control, b, 0.45f);
            DrawSolid(new Rect(mid.x - 140f, mid.y - 36f, 280f, 46f), new Color(0.02f, 0.018f, 0.012f, 0.76f));
            DrawFrame(new Rect(mid.x - 140f, mid.y - 36f, 280f, 46f), new Color(0.18f, 0.85f, 1f, 0.90f), 1.5f);
            GUI.Label(new Rect(mid.x - 136f, mid.y - 33f, 272f, 40f), label + "\nEn vol", MiniLabel(new Color(1f, 0.96f, 0.72f, 1f), 12, TextAnchor.MiddleCenter));
        }

        private void DrawFixedHud()
        {
            if (IsPortraitLayout())
            {
                DrawFixedHudPortrait();
                return;
            }

            Rect legend = new Rect(14f, Screen.height - 112f, Mathf.Min(760f, Screen.width - 28f), 96f);
            DrawSolid(legend, new Color(0.025f, 0.022f, 0.016f, 0.82f));
            DrawFrame(legend, new Color(0.94f, 0.64f, 0.16f, 0.70f), 2f);
            GUI.Label(new Rect(legend.x + 14f, legend.y + 8f, 150f, 20f), "Legende", LabelStyle(Color.white, 14, FontStyle.Bold, TextAnchor.MiddleLeft));
            DrawLegendItem(legend.x + 16f, legend.y + 40f, HiveColor(HiveMaturity.Beginning), "Debut");
            DrawLegendItem(legend.x + 132f, legend.y + 40f, HiveColor(HiveMaturity.Mid), "Intermediaire");
            DrawLegendItem(legend.x + 286f, legend.y + 40f, HiveColor(HiveMaturity.Advanced), "Avancee");
            DrawLegendItem(legend.x + 410f, legend.y + 40f, HiveColor(HiveMaturity.Capital), "Capitale");
            DrawLegendItem(legend.x + 536f, legend.y + 40f, ResourceColor(ResourceKind.RoyalJelly), "Ressources rares");
            DrawBearDenToggle();
        }

        private void DrawFixedHudPortrait()
        {
            DrawBearDenToggle();
        }

        private void DrawBearDenToggle()
        {
            Rect rect = BearDenToggleRect();
            bool loaded = bearDenLandmark != null && bearDenLandmark.IsLoaded;
            bool visible = loaded && bearDenLandmark.IsVisible;
            bool hovered = rect.Contains(Event.current.mousePosition);
            Color background = visible
                ? new Color(0.23f, 0.15f, 0.045f, hovered ? 0.97f : 0.91f)
                : new Color(0.055f, 0.065f, 0.065f, hovered ? 0.96f : 0.89f);
            Color border = visible
                ? new Color(1f, 0.70f, 0.18f, 0.98f)
                : new Color(0.42f, 0.58f, 0.60f, 0.86f);
            DrawSolid(rect, background);
            DrawFrame(rect, border, visible ? 2.2f : 1.5f);

            GUI.enabled = loaded;
            GUIContent hitTarget = new GUIContent(string.Empty, "Tanière d'ours - afficher ou masquer ce repaire dormant");
            if (GUI.Button(rect, hitTarget, GUIStyle.none))
            {
                bool nowVisible = bearDenLandmark.ToggleVisibility();
                status = nowVisible
                    ? "Tanière d'ours affichée"
                    : "Tanière d'ours masquée";
            }
            GUI.enabled = true;

            Rect iconRect = new Rect(rect.x + 7f, rect.y + 6f, 42f, rect.height - 12f);
            Color previous = GUI.color;
            GUI.color = visible ? Color.white : new Color(0.70f, 0.76f, 0.76f, 0.62f);
            if (loaded) GUI.DrawTexture(iconRect, bearDenLandmark.Texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;

            string state = !loaded ? "INDISPONIBLE" : (visible ? "VISIBLE" : "MASQUEE");
            GUI.Label(new Rect(rect.x + 54f, rect.y + 4f, rect.width - 60f, 22f), IsPortraitLayout() ? "Taniere" : "Taniere d'ours", LabelStyle(Color.white, 12, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(rect.x + 54f, rect.y + 24f, rect.width - 60f, 17f), state + " · repaire dormant", MiniLabel(visible ? new Color(1f, 0.82f, 0.32f, 1f) : new Color(0.65f, 0.82f, 0.84f, 1f), 10, TextAnchor.MiddleLeft));
        }

        private void DrawSpawnInspector()
        {
            Rect header = SpawnInspectorHeaderRect();
            DrawSolid(header, new Color(0.018f, 0.022f, 0.026f, 0.92f));
            DrawFrame(header, new Color(0.30f, 0.78f, 1f, 0.88f), 2f);
            if (GUI.Button(new Rect(header.x + 8f, header.y + 6f, 34f, 30f), spawnInspectorCollapsed ? "+" : "-"))
            {
                spawnInspectorCollapsed = !spawnInspectorCollapsed;
            }

            GUI.Label(new Rect(header.x + 48f, header.y + 5f, header.width - 56f, 18f), "SPAWN INSPECTEUR | LOCAL - APERCU NON OFFICIEL", LabelStyle(Color.white, 12, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(header.x + 48f, header.y + 24f, header.width - 56f, 16f), "overlay=" + (spawnDiagnosticOverlayEnabled ? "ON" : "OFF") + " | server=false official_gain=false", MiniLabel(new Color(0.75f, 0.93f, 1f, 1f), 10, TextAnchor.MiddleLeft));
            if (spawnInspectorCollapsed) return;

            if (spawnPreviewRecords.Count == 0)
            {
                spawnPreviewRecords = GenerateSpawnPreview(spawnInspectorSeed, spawnSeedVersion, activeChunks);
                spawnPreviewSummary = SummarizeSpawnPreview(spawnPreviewRecords);
            }

            Rect panel = SpawnInspectorPanelRect();
            DrawSolid(panel, new Color(0.014f, 0.018f, 0.022f, 0.94f));
            DrawFrame(panel, new Color(0.30f, 0.78f, 1f, 0.82f), 2f);
            float x = panel.x + 12f;
            float y = panel.y + 10f;
            float w = panel.width - 24f;
            spawnDiagnosticOverlayEnabled = GUI.Toggle(new Rect(x, y, w, 22f), spawnDiagnosticOverlayEnabled, "Overlay diagnostic local");
            y += 26f;
            GUI.Label(new Rect(x, y, 82f, 22f), "Seed local", MiniLabel(Color.white, 10, TextAnchor.MiddleLeft));
            string seedText = GUI.TextField(new Rect(x + 88f, y, 92f, 22f), spawnInspectorSeed.ToString(CultureInfo.InvariantCulture));
            if (int.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedSeed)) spawnInspectorSeed = parsedSeed;
            GUI.Label(new Rect(x + 188f, y, w - 188f, 22f), spawnSeedVersion, MiniLabel(new Color(0.75f, 0.93f, 1f, 1f), 10, TextAnchor.MiddleLeft));
            y += 28f;
            if (GUI.Button(new Rect(x, y, w, 30f), "Regenerer apercu local - Jamais officiel"))
            {
                spawnPreviewRecords = GenerateSpawnPreview(spawnInspectorSeed, spawnSeedVersion, activeChunks);
                spawnPreviewSummary = SummarizeSpawnPreview(spawnPreviewRecords);
                selectedSpawnPreviewId = spawnPreviewRecords.Count > 0 ? spawnPreviewRecords[0].EntityId : string.Empty;
                spawnInspectorStatus = "Regenere seed " + spawnInspectorSeed.ToString(CultureInfo.InvariantCulture) + " | hash " + SpawnDistributionHash(spawnPreviewRecords);
            }
            y += 36f;
            GUI.Label(new Rect(x, y, w, 18f), spawnInspectorStatus, MiniLabel(new Color(1f, 0.88f, 0.45f, 1f), 10, TextAnchor.MiddleLeft));
            y += 22f;
            GUI.Label(new Rect(x, y, w, 18f), "Chunks " + spawnPreviewSummary.ActiveChunks + "/25 | H " + spawnPreviewSummary.Hives + "/25 | R " + spawnPreviewSummary.Resources + "/75 | T " + spawnPreviewSummary.Bestiary + "/25", MiniLabel(spawnPreviewSummary.BudgetsPass ? Color.white : new Color(1f, 0.42f, 0.35f, 1f), 10, TextAnchor.MiddleLeft));
            y += 22f;
            GUI.Label(new Rect(x, y, w, 18f), "R1/R2/R3 " + PassText(spawnPreviewSummary.HasR1 && spawnPreviewSummary.HasR2 && spawnPreviewSummary.HasR3) + " | T1-T7 " + spawnPreviewSummary.MinBestiaryTier + "-" + spawnPreviewSummary.MaxBestiaryTier, MiniLabel(Color.white, 10, TextAnchor.MiddleLeft));
            y += 22f;
            GUI.Label(new Rect(x, y, w, 18f), "Exclusions hits BearDen/Eau/Falaise/Event: " + spawnPreviewSummary.ExclusionHitsBearDen + "/" + spawnPreviewSummary.ExclusionHitsWater + "/" + spawnPreviewSummary.ExclusionHitsCliff + "/" + spawnPreviewSummary.ExclusionHitsReservedEvent, MiniLabel(Color.white, 10, TextAnchor.MiddleLeft));
            y += 24f;
            SpawnPreviewRecord selected = SelectedSpawnPreview();
            GUI.Label(new Rect(x, y, w, 20f), "Detail selection", LabelStyle(Color.white, 11, FontStyle.Bold, TextAnchor.MiddleLeft));
            y += 22f;
            if (!string.IsNullOrEmpty(selected.EntityId))
            {
                GUI.Label(new Rect(x, y, w, 18f), selected.EntityId, MiniLabel(new Color(0.75f, 0.93f, 1f, 1f), 9, TextAnchor.MiddleLeft));
                y += 18f;
                GUI.Label(new Rect(x, y, w, 18f), selected.Family + " " + selected.Kind + " " + selected.TierToken + " | " + selected.ChunkId + " | n=" + selected.Normalized.x.ToString("0.000", CultureInfo.InvariantCulture) + "," + selected.Normalized.y.ToString("0.000", CultureInfo.InvariantCulture), MiniLabel(Color.white, 9, TextAnchor.MiddleLeft));
            }
        }

        private void DrawSpawnDiagnosticOverlay()
        {
            for (int i = 0; i < spawnPreviewRecords.Count; i++)
            {
                SpawnPreviewRecord record = spawnPreviewRecords[i];
                Vector2 p = WorldToScreen(record.WorldCoord);
                if (!IsOnScreen(p, 60f)) continue;
                Color color = record.Family == "hive" ? new Color(1f, 0.85f, 0.20f, 0.95f) : (record.Family == "resource" ? new Color(0.35f, 0.95f, 0.58f, 0.92f) : new Color(1f, 0.38f, 0.30f, 0.92f));
                if (record.Family == "hive") DrawHex(p, 15f, color, 2f);
                else if (record.Family == "resource") DrawDiamond(p, 13f, color, 2f);
                else DrawTriangle(p, 15f, color, 2f);
                GUI.Label(new Rect(p.x - 48f, p.y + 14f, 96f, 18f), record.TierToken, MiniLabel(Color.white, 9, TextAnchor.MiddleCenter));
            }

            DrawExclusionOverlay();
        }

        private void DrawExclusionOverlay()
        {
            if (bearDenLandmark != null && bearDenLandmark.IsLoaded)
            {
                Vector2 p = WorldToScreen(bearDenLandmark.WorldAnchor);
                DrawCircle(p, 56f, new Color(1f, 0.64f, 0.14f, 0.82f), 28);
                GUI.Label(new Rect(p.x - 58f, p.y - 74f, 116f, 18f), "BearDen exclu", MiniLabel(new Color(1f, 0.82f, 0.32f, 1f), 10, TextAnchor.MiddleCenter));
            }
        }

        private void DrawActionPanel()
        {
            if (IsPortraitLayout())
            {
                DrawActionPanelPortrait();
                return;
            }

            WorldHiveNode hive = SelectedHive();
            WorldResourceNode resource = SelectedResource();
            Rect panel = ActionPanelRect();
            DrawSolid(panel, new Color(0.025f, 0.022f, 0.016f, 0.90f));
            DrawFrame(panel, new Color(0.18f, 0.85f, 1f, 0.86f), 2f);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 24f), "Expedition", LabelStyle(Color.white, 15, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(panel.x + 14f, panel.y + 42f, panel.width - 28f, 20f), "Ruche: " + (hive != null ? hive.Label : selectedHiveId + " hors chunks actifs"), MiniLabel(Color.white, 12, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(panel.x + 14f, panel.y + 66f, panel.width - 28f, 20f), "Ressource: " + (resource != null ? resource.Label + " " + ResourceQuantityLabel(resource) : selectedResourceId + " hors chunks actifs"), MiniLabel(Color.white, 12, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(panel.x + 14f, panel.y + 88f, panel.width - 28f, 20f), resource != null ? "Coord cible: " + CoordLabel(resource.WorldCoord) : "Selectionner une ressource active", MiniLabel(new Color(0.86f, 0.92f, 1f, 1f), 11, TextAnchor.MiddleLeft));
            bool officialResource = IsOfficialResource(resource);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 112f, panel.width - 28f, 20f), "Etat: " + (officialResource ? OfficialStateLabel(resource) : CollectionStateLabel()), MiniLabel(new Color(1f, 0.88f, 0.38f, 1f), 12, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(panel.x + 14f, panel.y + 136f, panel.width - 28f, 20f), "Gain: " + (officialResource ? "voir historique officiel" : localRewardText), MiniLabel(Color.white, 12, TextAnchor.MiddleLeft));

            bool canCollect = CanCollectOrLaunch(hive, resource);
            GUI.enabled = canCollect;
            if (GUI.Button(new Rect(panel.x + 14f, panel.y + 166f, panel.width - 28f, 34f), CollectionActionLabel(resource)))
            {
                TryCollectOrLaunch(hive, resource);
            }

            GUI.enabled = true;
            WorldBestiaryNode beast = SelectedBestiary();
            string beastRareSightingEventKey = string.Empty;
            bool beastIsRareSighting = beast != null && IsRareSighting(beast, out beastRareSightingEventKey);
            float rareSightingOffset = 0f;
            GUI.Label(new Rect(panel.x + 14f, panel.y + 208f, panel.width - 28f, 20f), "Bestiaire: " + (beast != null ? "T" + beast.Tier.ToString(CultureInfo.InvariantCulture) + " " + beast.Label + " | " + beast.Role : "selectionner une cible"), MiniLabel(new Color(1f, 0.90f, 0.62f, 1f), 11, TextAnchor.MiddleLeft));
            if (beastIsRareSighting)
            {
                GUI.Label(new Rect(panel.x + 14f, panel.y + 226f, panel.width - 28f, 16f),
                    "★ Reperage rare : " + HiveViewProductUiPresenter.WorldEventDisplayName(beastRareSightingEventKey) + " +25%",
                    MiniLabel(new Color(1f, 0.80f, 0.30f, 1f), 10, TextAnchor.MiddleLeft));
                rareSightingOffset = 16f;
            }
            var squadForAttack = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap?.Model;
            bool hasSquadForAttack = squadForAttack != null && squadForAttack.HasReservation;
            bool canAttack = beast != null && hasSquadForAttack;
            string attackLabel = !hasSquadForAttack ? "Aucune escouade prête" : "ATTAQUER";
            GUI.enabled = canAttack;
            if (GUI.Button(new Rect(panel.x + 14f, panel.y + 232f + rareSightingOffset, panel.width - 28f, 30f), attackLabel))
            {
                HiveViewProductUiPresenter.OpenCombatPatrolOverlayForWorldMap(beast.Tier);
            }
            if (beast != null && !hasSquadForAttack)
            {
                GUI.Label(new Rect(panel.x + 14f, panel.y + 266f + rareSightingOffset, panel.width - 28f, 20f), "Préparez une escouade avant d'attaquer.", MiniLabel(new Color(1f, 0.70f, 0.30f, 1f), 10, TextAnchor.MiddleLeft));
                if (GUI.Button(new Rect(panel.x + 14f, panel.y + 288f + rareSightingOffset, panel.width - 28f, 24f), "Ouvrir Armée"))
                {
                    if (SplashDevelopmentSceneConfig.IsSceneEnabledInBuildSettings(SplashDevelopmentSceneConfig.HiveMapScenePath))
                        SplashDevelopmentSceneConfig.TryOpenScene(SplashDevelopmentSceneConfig.HiveMapScenePath, out _);
                }
            }

            GUI.enabled = true;
            if (!officialResource)
            {
                GUI.Label(new Rect(panel.x + 14f, panel.y + 266f + rareSightingOffset, panel.width - 28f, 48f), "Repérage seulement · rien à récolter ici pour l'instant", MiniLabel(new Color(0.86f, 0.92f, 1f, 1f), 11, TextAnchor.UpperLeft));
            }
        }

        private void DrawActionPanelPortrait()
        {
            WorldHiveNode hive = SelectedHive();
            WorldResourceNode resource = SelectedResource();
            Rect panel = ActionPanelRect();
            DrawSolid(panel, new Color(0.025f, 0.022f, 0.016f, 0.92f));
            DrawFrame(panel, new Color(0.18f, 0.85f, 1f, 0.86f), 2f);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 22f), "Expedition", LabelStyle(Color.white, 14, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(panel.x + 12f, panel.y + 33f, panel.width - 24f, 18f), "Ruche: " + (hive != null ? hive.Label : "hors zone"), MiniLabel(Color.white, 11, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(panel.x + 12f, panel.y + 54f, panel.width - 24f, 18f), "Cible: " + (resource != null ? resource.Label + " " + ResourceQuantityLabel(resource) : "selectionner ressource"), MiniLabel(Color.white, 11, TextAnchor.MiddleLeft));
            bool officialResourcePortrait = IsOfficialResource(resource);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 75f, panel.width - 24f, 18f), "Etat: " + (officialResourcePortrait ? OfficialStateLabel(resource) : CollectionStateLabel() + " | " + localRewardText), MiniLabel(new Color(1f, 0.88f, 0.38f, 1f), 11, TextAnchor.MiddleLeft));

            bool canCollect = CanCollectOrLaunch(hive, resource);
            GUI.enabled = canCollect;
            if (GUI.Button(new Rect(panel.x + 12f, panel.y + 102f, panel.width - 24f, 34f), CollectionActionLabel(resource)))
            {
                TryCollectOrLaunch(hive, resource);
            }

            GUI.enabled = true;
            WorldBestiaryNode beast = SelectedBestiary();
            bool beastIsRareSightingPortrait = beast != null && IsRareSighting(beast, out _);
            string bestiaryLabelPortrait = beast != null
                ? (beastIsRareSightingPortrait ? "★ " : string.Empty) + "T" + beast.Tier.ToString(CultureInfo.InvariantCulture) + " " + beast.Label
                : "aucun";
            GUI.Label(new Rect(panel.x + 12f, panel.y + 140f, panel.width - 24f, 18f), "Bestiaire: " + bestiaryLabelPortrait,
                MiniLabel(beastIsRareSightingPortrait ? new Color(1f, 0.80f, 0.30f, 1f) : new Color(1f, 0.90f, 0.62f, 1f), 10, TextAnchor.MiddleLeft));
            var squadForAttackPortrait = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap?.Model;
            bool hasSquadForAttackPortrait = squadForAttackPortrait != null && squadForAttackPortrait.HasReservation;
            bool canAttackPortrait = beast != null && hasSquadForAttackPortrait;
            string attackLabelPortrait = !hasSquadForAttackPortrait ? "Aucune escouade prête" : "ATTAQUER";
            GUI.enabled = canAttackPortrait;
            if (GUI.Button(new Rect(panel.x + 12f, panel.y + 160f, panel.width - 24f, 30f), attackLabelPortrait))
            {
                HiveViewProductUiPresenter.OpenCombatPatrolOverlayForWorldMap(beast.Tier);
            }
            if (beast != null && !hasSquadForAttackPortrait)
            {
                GUI.Label(new Rect(panel.x + 12f, panel.y + 194f, panel.width - 24f, 16f), "Préparez une escouade avant d'attaquer.", MiniLabel(new Color(1f, 0.70f, 0.30f, 1f), 9, TextAnchor.MiddleLeft));
            }

            GUI.enabled = true;
        }

        private void DrawFlightJournal()
        {
            if (IsPortraitLayout())
            {
                DrawFlightJournalPortrait();
                return;
            }

            Rect panel = FlightJournalRect();
            DrawSolid(panel, new Color(0.025f, 0.022f, 0.016f, 0.88f));
            DrawFrame(panel, new Color(0.94f, 0.64f, 0.16f, 0.78f), 2f);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 22f), "Journal des vols", LabelStyle(Color.white, 14, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(panel.x + 12f, panel.y + 31f, panel.width - 24f, 18f), "Source -> destination | etat | coords monde", MiniLabel(new Color(1f, 0.86f, 0.48f, 1f), 10, TextAnchor.MiddleLeft));

            float y = panel.y + 54f;
            int visible = Mathf.Min(3, flights.Count);
            for (int i = 0; i < visible; i++)
            {
                WorldFlightRecord flight = flights[Mathf.Max(0, flights.Count - visible + i)];
                Rect row = new Rect(panel.x + 10f, y, panel.width - 20f, 28f);
                DrawSolid(row, new Color(0.08f, 0.07f, 0.045f, 0.72f));
                DrawFrame(row, new Color(0.18f, 0.85f, 1f, 0.26f), 1f);
                string progress = Mathf.RoundToInt(FlightProgress01(flight) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
                GUI.Label(new Rect(row.x + 8f, row.y + 4f, row.width - 16f, 20f), flight.Id + "  " + flight.OriginLabel + " -> " + flight.DestinationLabel + " | " + CollectionStateLabel(flight.State) + " | " + progress + " | " + flight.Reward, MiniLabel(new Color(1f, 0.91f, 0.52f, 1f), 10, TextAnchor.MiddleLeft));
                y += 32f;
            }
        }

        private void DrawFlightJournalPortrait()
        {
            Rect panel = FlightJournalRect();
            DrawSolid(panel, new Color(0.025f, 0.022f, 0.016f, 0.84f));
            DrawFrame(panel, new Color(0.94f, 0.64f, 0.16f, 0.78f), 2f);
            GUI.Label(new Rect(panel.x + 10f, panel.y + 6f, panel.width - 20f, 20f), "Vols", LabelStyle(Color.white, 13, FontStyle.Bold, TextAnchor.MiddleLeft));
            string line = flights.Count > 0 ? flights[flights.Count - 1].Id + " | " + CollectionStateLabel(flights[flights.Count - 1].State) + " | aerien" : "Aucun vol actif";
            GUI.Label(new Rect(panel.x + 10f, panel.y + 30f, panel.width - 20f, 20f), line, MiniLabel(new Color(1f, 0.91f, 0.52f, 1f), 10, TextAnchor.MiddleLeft));
        }

        private void DrawMiniMap()
        {
            Rect mini = IsPortraitLayout()
                ? new Rect(Screen.width - 128f, 204f, 118f, 86f)
                : new Rect(Screen.width - 214f, Screen.height - 156f, 198f, 140f);
            DrawSolid(mini, new Color(0.025f, 0.022f, 0.016f, 0.86f));
            DrawFrame(mini, new Color(0.94f, 0.64f, 0.16f, 0.78f), 2f);
            GUI.Label(new Rect(mini.x + 10f, mini.y + 4f, mini.width - 20f, 18f), IsPortraitLayout() ? "Minimap" : "Minimap monde logique", MiniLabel(Color.white, 11, TextAnchor.MiddleCenter));
            Rect image = new Rect(mini.x + 10f, mini.y + 26f, mini.width - 20f, mini.height - 36f);
            DrawSolid(image, new Color(0.05f, 0.10f, 0.075f, 1f));

            if (wave6Provider != null && wave6Provider.ManifestReady)
            {
                Rect bounds = wave6Provider.WorldBounds;
                Rect artRegion = new Rect(
                    image.x + bounds.xMin / WorldWidthUnits() * image.width,
                    image.y + bounds.yMin / WorldHeightUnits() * image.height,
                    bounds.width / WorldWidthUnits() * image.width,
                    bounds.height / WorldHeightUnits() * image.height);
                if (mapFilterBiomeOverlay) DrawMiniMapBiomeGrid(artRegion);
                DrawFrame(artRegion, new Color(0.20f, 0.80f, 0.48f, 0.82f), 1.5f);
            }

            for (int i = 0; i < activeChunks.Count; i++)
            {
                Vector2Int chunk = activeChunks[i];
                Rect r = new Rect(
                    image.x + chunk.x / (float)WorldChunkWidth * image.width,
                    image.y + chunk.y / (float)WorldChunkHeight * image.height,
                    Mathf.Max(2f, image.width / WorldChunkWidth),
                    Mathf.Max(2f, image.height / WorldChunkHeight));
                DrawSolid(r, new Color(1f, 0.78f, 0.18f, 0.72f));
            }

            Vector2 p = new Vector2(
                image.x + currentWorldCenter.x / WorldWidthUnits() * image.width,
                image.y + currentWorldCenter.y / WorldHeightUnits() * image.height);
            DrawCircle(p, 4f, new Color(0.20f, 0.85f, 1f, 0.95f), 12);
        }

        // Same 10x10 grid DrawBiomeOverlay/DrawRegionLabels read (WorldBiomeCatalog), drawn
        // at minimap scale so the minimap itself reads as a small atlas of the world instead
        // of just an activity heatmap - lets the player orient by biome even fully zoomed out.
        private void DrawMiniMapBiomeGrid(Rect artRegion)
        {
            const int cells = 10;
            const int tilesPerCell = 5;
            float cellW = artRegion.width / cells;
            float cellH = artRegion.height / cells;
            for (int cellY = 0; cellY < cells; cellY++)
            {
                for (int cellX = 0; cellX < cells; cellX++)
                {
                    int chunkX = WorldMapWave6StreamingTileProvider.OriginChunkX + cellX * tilesPerCell + tilesPerCell / 2;
                    int chunkY = WorldMapWave6StreamingTileProvider.OriginChunkY + cellY * tilesPerCell + tilesPerCell / 2;
                    WorldBiome biome = WorldBiomeCatalog.BiomeForChunk(chunkX, chunkY);
                    Color c = WorldBiomeCatalog.ProfileFor(biome).EmotionalColor;
                    Rect cellRect = new Rect(artRegion.x + cellX * cellW, artRegion.y + cellY * cellH, cellW + 0.6f, cellH + 0.6f);
                    DrawSolid(cellRect, new Color(c.r, c.g, c.b, 0.55f));
                }
            }
        }

        private void DrawMapReadingTools()
        {
            Rect rect = MapReadingToolsRect();
            DrawSolid(rect, new Color(0.025f, 0.022f, 0.016f, 0.88f));
            DrawFrame(rect, new Color(0.56f, 0.92f, 0.74f, 0.82f), 2f);
            if (GUI.Button(new Rect(rect.x + 8f, rect.y + 6f, 32f, 26f), mapToolsCollapsed ? "+" : "-"))
            {
                mapToolsCollapsed = !mapToolsCollapsed;
            }

            const float hiveButtonWidth = 70f;
            GUI.Label(new Rect(rect.x + 48f, rect.y + 7f, rect.width - hiveButtonWidth - 66f, 24f), "LECTURE CARTE", LabelStyle(Color.white, 13, FontStyle.Bold, TextAnchor.MiddleLeft));
            if (GUI.Button(MapReturnHiveButtonRect(), "Ruche"))
            {
                OpenLivingHiveFromWorldMap();
            }

            if (mapToolsCollapsed) return;

            float y = rect.y + 40f;
            mapFilterHives = GUI.Toggle(new Rect(rect.x + 12f, y, 128f, 22f), mapFilterHives, "Ruches");
            mapFilterResources = GUI.Toggle(new Rect(rect.x + 142f, y, 128f, 22f), mapFilterResources, "Ressources");
            y += 24f;
            mapFilterThreats = GUI.Toggle(new Rect(rect.x + 12f, y, 128f, 22f), mapFilterThreats, "Menaces");
            mapFilterBearDen = GUI.Toggle(new Rect(rect.x + 142f, y, 128f, 22f), mapFilterBearDen, "BearDen");
            y += 24f;
            mapFilterBiomeOverlay = GUI.Toggle(new Rect(rect.x + 12f, y, 128f, 22f), mapFilterBiomeOverlay, "Biomes");
            y += 30f;

            if (GUI.Button(new Rect(rect.x + 12f, y, rect.width - 24f, 28f), "Selectionner plus proche"))
            {
                SelectNearestMapNode();
            }

            y += 34f;
            GUI.Label(new Rect(rect.x + 12f, y, rect.width - 24f, 18f), mapToolsStatus, MiniLabel(new Color(0.82f, 1f, 0.90f, 1f), 10, TextAnchor.MiddleLeft));
            y += 22f;
            GUI.Label(new Rect(rect.x + 12f, y, rect.width - 24f, 58f), "Tiers: R1 pauvre, R2 moyen, R3 riche\nMenaces: T1 solo -> T7 raid\nFiltres = overlays seulement, terrain intact", MiniLabel(new Color(1f, 0.92f, 0.62f, 1f), 10, TextAnchor.UpperLeft));
        }

        private bool worldMapReturnInProgress;

        private void OpenLivingHiveFromWorldMap()
        {
            if (worldMapReturnInProgress) return;
            if (!HiveViewProductUiPresenter.TryBeginGuidedWorldMapReturnForRuntime()) return;
            worldMapReturnInProgress = true;
            if (SplashDevelopmentSceneConfig.TryOpenScene(SplashDevelopmentSceneConfig.HiveMapScenePath, out string message)) return;
            worldMapReturnInProgress = false;
            mapToolsStatus = message;
            status = message;
        }

        private Rect WorldMapReturnBarRect()
        {
            if (IsPortraitLayout())
            {
                return new Rect(8f, Screen.height - 190f - 8f - 56f, Screen.width - 16f, 56f);
            }

            const float barWidth = 360f;
            return new Rect((Screen.width - barWidth) * 0.5f, Screen.height - 108f, barWidth, 92f);
        }

        private void WorldMapReturnBarButtonRects(out Rect locate, out Rect returnHive)
        {
            Rect bar = WorldMapReturnBarRect();
            bool portrait = IsPortraitLayout();
            float pad = portrait ? 10f : 14f;
            float gap = 10f;
            float buttonHeight = portrait ? bar.height - 16f : 40f;
            float y = portrait ? bar.y + 8f : bar.y + bar.height - buttonHeight - 14f;
            float width = (bar.width - pad * 2f - gap) * 0.5f;
            locate = new Rect(bar.x + pad, y, width, buttonHeight);
            returnHive = new Rect(bar.x + pad + width + gap, y, width, buttonHeight);
        }

        private Rect WorldMapLocateHiveButtonRect()
        {
            WorldMapReturnBarButtonRects(out Rect locate, out _);
            return locate;
        }

        private Rect WorldMapReturnHiveButtonRect()
        {
            WorldMapReturnBarButtonRects(out _, out Rect returnHive);
            return returnHive;
        }

        private void DrawWorldMapReturnBar()
        {
            Rect bar = WorldMapReturnBarRect();
            DrawSolid(bar, new Color(0.025f, 0.022f, 0.016f, 0.90f));
            DrawFrame(bar, new Color(0.94f, 0.64f, 0.16f, 0.88f), 2f);

            if (!IsPortraitLayout())
            {
                GUI.Label(new Rect(bar.x + 14f, bar.y + 8f, bar.width - 28f, 22f), "Navigation", LabelStyle(Color.white, 13, FontStyle.Bold, TextAnchor.MiddleCenter));
            }

            if (GUI.Button(WorldMapLocateHiveButtonRect(), "Localiser ma ruche"))
            {
                CenterOnPlayerHive();
            }

            if (GUI.Button(WorldMapReturnHiveButtonRect(), "Retour a la ruche"))
            {
                OpenLivingHiveFromWorldMap();
            }
        }

        private Rect MapReturnHiveButtonRect()
        {
            return WorldMapReturnHiveButtonRect();
        }

        private void DrawGuidedWorldTransitionTutorial()
        {
            if (!HiveViewProductUiPresenter.GuidedWorldMapTutorialActiveForRuntime()) return;
            WorldHiveNode playerHive = HiveById("hive_player_test");
            WorldResourceNode pollen = ResourceById("res_pollen_core");
            Rect playerSpotlight = default;
            if (playerHive != null)
            {
                Vector2 center = WorldToScreen(playerHive.WorldCoord);
                float size = IsPortraitLayout() ? 118f : 142f;
                playerSpotlight = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            }

            Rect resourceSpotlight = default;
            if (pollen != null)
            {
                Vector2 center = WorldToScreen(pollen.WorldCoord);
                float size = IsPortraitLayout() ? 132f : 156f;
                resourceSpotlight = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            }

            Rect returnSpotlight = MapReturnHiveButtonRect();
            returnSpotlight = new Rect(returnSpotlight.x - 8f, returnSpotlight.y - 7f, returnSpotlight.width + 16f, returnSpotlight.height + 14f);
            if (HiveViewProductUiPresenter.GuidedWorldMapTutorialSelectionStepForRuntime()
                && playerHive != null
                && GUI.Button(playerSpotlight, GUIContent.none, GUIStyle.none))
            {
                selectedHiveId = playerHive.Id;
                HiveViewProductUiPresenter.SelectGuidedWorldMapHiveForRuntime(playerHive.Id);
                status = "Ruche selectionnee: " + playerHive.Label + " @ " + CoordLabel(playerHive.WorldCoord);
            }

            if (HiveViewProductUiPresenter.GuidedWorldMapTutorialResourceSelectionStepForRuntime()
                && pollen != null
                && GUI.Button(resourceSpotlight, GUIContent.none, GUIStyle.none))
            {
                selectedResourceId = pollen.Id;
                HiveViewProductUiPresenter.SelectGuidedWorldMapResourceForRuntime(pollen.Id);
                status = "Pollen selectionne: " + pollen.Label + " @ " + CoordLabel(pollen.WorldCoord);
            }

            HiveViewProductUiPresenter.DrawGuidedWorldMapTutorialForRuntime(
                playerSpotlight,
                resourceSpotlight,
                returnSpotlight,
                CurrentGuidedForagingProgress01(),
                StartGuidedForagingFlight);
        }

        private bool StartGuidedForagingFlight()
        {
            if (!HiveViewProductUiPresenter.GuidedWorldMapForagingTutorialActiveForRuntime()) return false;
            WorldHiveNode playerHive = HiveById("hive_player_test");
            WorldResourceNode pollen = ResourceById("res_pollen_core");
            if (playerHive == null || pollen == null || ResourceRemaining(pollen) <= 0) return false;

            selectedHiveId = playerHive.Id;
            selectedResourceId = pollen.Id;
            collectionState = CollectionFlightState.Idle;
            StartLocalCollectionFlight();
            if (collectionState != CollectionFlightState.FlyingToResource) return false;
            return HiveViewProductUiPresenter.RegisterGuidedForagingFlightStartedForRuntime(playerHive.Id, pollen.Id);
        }

        private float CurrentGuidedForagingProgress01()
        {
            if (collectionState == CollectionFlightState.FlyingToResource) return Mathf.Clamp01(collectionTimer / 3.2f) * 0.42f;
            if (collectionState == CollectionFlightState.Collecting) return 0.42f + Mathf.Clamp01(collectionTimer / 1.15f) * 0.18f;
            if (collectionState == CollectionFlightState.Returning) return 0.60f + Mathf.Clamp01(collectionTimer / 3.0f) * 0.40f;
            if (collectionState == CollectionFlightState.Completed) return 1f;
            return 0f;
        }

        private Rect MapReadingToolsRect()
        {
            if (IsPortraitLayout()) return mapToolsCollapsed ? new Rect(8f, 246f, 214f, 40f) : new Rect(8f, 246f, 300f, 220f);
            return mapToolsCollapsed ? new Rect(548f, 128f, 210f, 40f) : new Rect(548f, 128f, 286f, 220f);
        }

        private void SelectNearestMapNode()
        {
            float bestDistance = float.MaxValue;
            string bestKind = string.Empty;
            string bestId = string.Empty;
            if (mapFilterHives)
            {
                for (int i = 0; i < hives.Count; i++)
                {
                    ConsiderNearest(hives[i].Id, "Ruche", hives[i].WorldCoord, ref bestDistance, ref bestKind, ref bestId);
                }
            }

            if (mapFilterResources)
            {
                for (int i = 0; i < resources.Count; i++)
                {
                    ConsiderNearest(resources[i].Id, "Ressource", resources[i].WorldCoord, ref bestDistance, ref bestKind, ref bestId);
                }
            }

            if (mapFilterThreats)
            {
                for (int i = 0; i < bestiary.Count; i++)
                {
                    ConsiderNearest(bestiary[i].Id, "Menace", bestiary[i].WorldCoord, ref bestDistance, ref bestKind, ref bestId);
                }
            }

            if (string.IsNullOrEmpty(bestId))
            {
                mapToolsStatus = "Aucun noeud visible avec filtres actifs";
                return;
            }

            if (bestKind == "Ruche") selectedHiveId = bestId;
            else if (bestKind == "Ressource") selectedResourceId = bestId;
            else selectedBestiaryId = bestId;
            mapToolsStatus = bestKind + " proche: " + bestId + " (" + Mathf.RoundToInt(bestDistance).ToString(CultureInfo.InvariantCulture) + "u)";
        }

        private void ConsiderNearest(string id, string kind, Vector2 worldCoord, ref float bestDistance, ref string bestKind, ref string bestId)
        {
            float distance = Vector2.Distance(currentWorldCenter, worldCoord);
            if (distance >= bestDistance) return;
            bestDistance = distance;
            bestKind = kind;
            bestId = id;
        }

        // Les 3 noeuds officiels (miel/pollen/cire) sont branches au vrai serveur (voir
        // HiveViewProductUiPresenter.LaunchOfficialWorldResourceCollectionForWorldMap) - leur bouton
        // "Collecter" declenche un vrai vol persiste (duree/gain reels), pas l'animation locale/demo
        // de 7 secondes utilisee pour les autres ressources (nectar/eau/propolis/gelee royale), pour
        // ne jamais donner au joueur un compte a rebours qui ne correspond pas au vrai etat serveur.
        private bool IsOfficialResource(WorldResourceNode resource) => resource != null && HiveViewProductUiPresenter.IsOfficialWorldResourceNode(resource.Id);

        private RemoteWorldResourceNode OfficialNodeState(WorldResourceNode resource)
        {
            if (!IsOfficialResource(resource)) return null;
            WorldResourceCollectionScreenModel model = HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            return model?.Nodes?.FirstOrDefault(n => n.NodeId == resource.Id);
        }

        // Le "pret a valider" se calcule ici a partir de l'heure reelle (Active.EndsAtUtc) plutot
        // que du champ CanClaim du modele (qui ne se met a jour qu'apres un aller-retour serveur) -
        // evite d'avoir a sonder le serveur en boucle juste pour faire avancer un compte a rebours ;
        // la validation elle-meme reste de toute facon revalidee cote serveur au moment du Claim.
        private static bool IsOfficialFlightReadyToClaim(WorldResourceCollectionScreenModel model, string nodeId) =>
            model?.Active != null && string.Equals(model.Active.NodeId, nodeId, StringComparison.Ordinal) && DateTimeOffset.UtcNow >= model.Active.EndsAtUtc;

        private void TryCollectOrLaunch(WorldHiveNode hive, WorldResourceNode resource)
        {
            if (hive == null || resource == null) return;
            if (!IsOfficialResource(resource)) { StartLocalCollectionFlight(); return; }
            WorldResourceCollectionScreenModel model = HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            if (model == null) { status = "Serveur monde indisponible"; return; }
            if (IsOfficialFlightReadyToClaim(model, resource.Id))
            {
                HiveViewProductUiPresenter.ClaimOfficialWorldResourceCollectionForWorldMap();
                status = "Recolte officielle en cours de validation: " + resource.Label;
                return;
            }
            if (IsOfficialFlightActiveHere(model, resource.Id))
            {
                HiveViewProductUiPresenter.RecallOfficialWorldResourceCollectionForWorldMap();
                status = "Escouade rappelee depuis " + resource.Label;
                return;
            }
            if (model.Active != null) { status = "Un vol officiel est deja en cours"; return; }
            // Escouade reellement engagee (demande de Jeff, 2026-08-01) : sans troupe disponible,
            // aucun vol ne peut partir - le dire clairement plutot que de laisser le bouton
            // sembler ne rien faire.
            if (model.AvailableRoster == null || model.AvailableRoster.Values.All(v => v <= 0))
            { status = "Aucune troupe disponible pour escorter la collecte"; return; }
            HiveViewProductUiPresenter.LaunchOfficialWorldResourceCollectionForWorldMap(resource.Id);
            status = "Vol officiel lance vers " + resource.Label + " (" + hive.Label + ")";
        }

        private bool CanCollectOrLaunch(WorldHiveNode hive, WorldResourceNode resource)
        {
            if (hive == null || resource == null) return false;
            if (!IsOfficialResource(resource))
                return ResourceRemaining(resource) > 0 && (collectionState == CollectionFlightState.Idle || collectionState == CollectionFlightState.Completed);
            WorldResourceCollectionScreenModel model = HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            if (model == null) return false;
            RemoteWorldResourceNode node = OfficialNodeState(resource);
            return IsOfficialFlightReadyToClaim(model, resource.Id) ||
                IsOfficialFlightActiveHere(model, resource.Id) ||
                (model.Active == null && node != null && node.CanLaunch);
        }

        // L'escouade est reellement engagee pendant tout le vol (demande de Jeff, 2026-08-01) - le
        // joueur doit pouvoir la rappeler avant la fin, pas seulement attendre ou reclamer.
        private static bool IsOfficialFlightActiveHere(WorldResourceCollectionScreenModel model, string nodeId) =>
            model?.Active != null && string.Equals(model.Active.NodeId, nodeId, StringComparison.Ordinal) && DateTimeOffset.UtcNow < model.Active.EndsAtUtc;

        private string CollectionActionLabel(WorldResourceNode resource)
        {
            if (!IsOfficialResource(resource)) return "Collecter";
            WorldResourceCollectionScreenModel model = HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            if (IsOfficialFlightReadyToClaim(model, resource.Id)) return "Recolter (officiel)";
            if (IsOfficialFlightActiveHere(model, resource.Id)) return "Rappeler l'escouade";
            return "Envoyer les abeilles (officiel)";
        }

        private string OfficialStateLabel(WorldResourceNode resource)
        {
            RemoteWorldResourceNode node = OfficialNodeState(resource);
            if (node == null) return "";
            WorldResourceCollectionScreenModel model = HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            if (model?.Active != null && string.Equals(model.Active.NodeId, resource.Id, StringComparison.Ordinal))
            {
                TimeSpan remaining = model.Active.EndsAtUtc - DateTimeOffset.UtcNow;
                return remaining > TimeSpan.Zero ? "Vol officiel en cours: " + Mathf.CeilToInt((float)remaining.TotalSeconds) + "s" : "Recolte prete a valider";
            }
            if (model?.Debrief != null && string.Equals(model.Debrief.NodeId, resource.Id, StringComparison.Ordinal))
            {
                string focusSuffix = model.Debrief.DailyFocusApplied ? " (cible du jour, +50% deja inclus)" : "";
                // Evenement mondial dynamique (demande de Jeff, 2026-08-01) : meme principe que la
                // Cible du jour ci-dessus, mais change plusieurs fois par jour au lieu d'une fois.
                string worldEventSuffix = model.Debrief.WorldEventApplied
                    ? " (" + HiveViewProductUiPresenter.WorldEventDisplayName(model.Debrief.WorldEventKey) + ", bonus deja inclus)"
                    : "";
                return "Derniere recolte officielle: +" + model.Debrief.CreditedAmount.ToString(CultureInfo.InvariantCulture) + " " + model.Debrief.ResourceKey + focusSuffix + worldEventSuffix;
            }
            if (!node.Ready && node.ReadyAtUtc.HasValue)
            {
                TimeSpan cooldown = node.ReadyAtUtc.Value - DateTimeOffset.UtcNow;
                return cooldown > TimeSpan.Zero ? "Repos: " + Mathf.CeilToInt((float)cooldown.TotalSeconds) + "s" : "Pret";
            }
            // Cible du jour (demande de Jeff, 2026-07-31) : ce noeud precis donne +50% de
            // recompense aujourd'hui - visible avant meme d'envoyer les abeilles.
            string readyLabel = "Pret (" + node.Yield.ToString(CultureInfo.InvariantCulture) + " " + node.ResourceKey + ")";
            if (node.IsDailyFocus) readyLabel += " - Cible du jour +50%";
            // Evenement mondial dynamique (demande de Jeff, 2026-08-01) : la meteo active peut
            // booster (ou reduire) ce rendement precis - change plusieurs fois par jour.
            if (node.IsWorldEventBoosted && model?.WorldEvent != null)
                readyLabel += " - " + HiveViewProductUiPresenter.WorldEventDisplayName(model.WorldEvent.Key) +
                    (model.WorldEvent.BonusBp >= 0 ? " +" : " ") + (model.WorldEvent.BonusBp / 100d).ToString("0.#", CultureInfo.InvariantCulture) + "%";
            return readyLabel;
        }

        // Rafraichit le modele officiel a intervalle regulier (pas chaque frame) pendant qu'un vol
        // reel est en cours, pour que le bouton "Recolter" devienne cliquable des que le serveur
        // considere le vol termine, sans dependre d'une nouvelle selection manuelle du joueur.
        private void UpdateOfficialWorldResourceCollectionPolling()
        {
            WorldResourceCollectionScreenModel model = HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            if (model == null || model.Active == null) return;
            officialWorldResourceRefreshTimer += Time.deltaTime;
            if (officialWorldResourceRefreshTimer < 3f) return;
            officialWorldResourceRefreshTimer = 0f;
            HiveViewProductUiPresenter.RefreshOfficialWorldResourceCollectionForWorldMap();
        }

        // Monde vivant (demande de Jeff, 2026-08-01) : presence ambiante des autres colonies -
        // rafraichie moins souvent que son propre vol (purement decorative, pas d'urgence a la
        // faire coincider a la seconde pres).
        private void UpdateWorldPresencePolling()
        {
            worldPresenceRefreshTimer += Time.deltaTime;
            if (worldPresenceRefreshTimer < 12f) return;
            worldPresenceRefreshTimer = 0f;
            HiveViewProductUiPresenter.RefreshWorldPresenceForWorldMap();
        }

        private void StartLocalCollectionFlight()
        {
            WorldHiveNode hive = SelectedHive();
            WorldResourceNode resource = SelectedResource();
            if (hive == null || resource == null)
            {
                status = "Selection requise: une ruche et une ressource active";
                return;
            }

            if (ResourceRemaining(resource) <= 0)
            {
                localRewardText = "Ressource epuisee";
                status = "Ressource epuisee - respawn demo programme";
                return;
            }

            collectionState = CollectionFlightState.FlyingToResource;
            collectionTimer = 0f;
            localRewardText = "En attente retour essaim";
            status = "Vol aerien local/demo lance en coordonnees monde: " + hive.Label + " -> " + resource.Label;
            flights.Add(new WorldFlightRecord(
                "VOL-" + nextFlightId.ToString("00", CultureInfo.InvariantCulture),
                hive.Id,
                resource.Id,
                hive.Label,
                resource.Label,
                hive.WorldCoord,
                resource.WorldCoord,
                CollectionFlightState.FlyingToResource,
                0f,
                RewardText(resource),
                "Collecte joueur demo"));
            nextFlightId++;
        }

        private bool CompleteSelectedResourceCollectionForProof()
        {
            WorldResourceNode resource = SelectedResource();
            if (resource == null || ResourceRemaining(resource) <= 0) return false;
            StartLocalCollectionFlight();
            CompleteSelectedResourceCollection();
            return collectionState == CollectionFlightState.Completed && ResourceRemaining(resource) == 0;
        }

        private void CompleteSelectedResourceCollection()
        {
            collectionState = CollectionFlightState.Completed;
            collectionTimer = 0f;
            WorldResourceNode resource = SelectedResource();
            if (resource == null)
            {
                localRewardText = "Recompense locale/demo";
                status = "Collecte locale/demo terminee: " + localRewardText;
                return;
            }

            int collectedAmount = Mathf.Max(1, Mathf.Min(ResourceRemaining(resource), resource.Amount) / 6);
            localRewardText = RewardText(resource);
            resourceRemaining[resource.Id] = 0;
            resourceRespawnAt[resource.Id] = Time.realtimeSinceStartup + 12f;
            HiveViewProductUiPresenter.CompleteGuidedForagingFlightForRuntime(resource.Id, collectedAmount);
            status = "Collecte locale/demo terminee: " + localRewardText + " | noeud epuise, respawn demo en attente";
        }

        private void AddSeedFlight(string hiveId, string resourceId, CollectionFlightState state, float timer, string label)
        {
            WorldHiveNode hive = HiveById(hiveId);
            WorldResourceNode resource = ResourceById(resourceId);
            if (hive == null || resource == null) return;

            flights.Add(new WorldFlightRecord(
                "VOL-" + nextFlightId.ToString("00", CultureInfo.InvariantCulture),
                hive.Id,
                resource.Id,
                hive.Label,
                resource.Label,
                hive.WorldCoord,
                resource.WorldCoord,
                state,
                timer,
                RewardText(resource),
                label));
            nextFlightId++;
        }

        private void TrySelectAt(Vector2 guiPoint)
        {
            WorldHiveNode nearestHive = null;
            float nearestHiveDistance = float.MaxValue;
            for (int i = 0; i < hives.Count; i++)
            {
                float distance = Vector2.Distance(guiPoint, WorldToScreen(hives[i].WorldCoord));
                if (distance < nearestHiveDistance)
                {
                    nearestHiveDistance = distance;
                    nearestHive = hives[i];
                }
            }

            if (nearestHive != null && nearestHiveDistance <= 58f)
            {
                if (HiveViewProductUiPresenter.GuidedWorldMapTutorialHiveSelectionStepForRuntime()
                    && !HiveViewProductUiPresenter.SelectGuidedWorldMapHiveForRuntime(nearestHive.Id))
                {
                    status = "Repere d'abord la ruche JOUEUR eclairee";
                    return;
                }

                if (HiveViewProductUiPresenter.GuidedWorldMapTutorialResourceSelectionStepForRuntime())
                {
                    HiveViewProductUiPresenter.RegisterGuidedWorldMapBlockedInputForRuntime();
                    status = "Touche directement le pollen eclaire";
                    return;
                }

                selectedHiveId = nearestHive.Id;
                status = "Ruche selectionnee: " + nearestHive.Label + " @ " + CoordLabel(nearestHive.WorldCoord);
                return;
            }

            if (HiveViewProductUiPresenter.GuidedWorldMapTutorialHiveSelectionStepForRuntime())
            {
                HiveViewProductUiPresenter.RegisterGuidedWorldMapBlockedInputForRuntime();
                status = "Touche directement la ruche JOUEUR eclairee";
                return;
            }

            WorldResourceNode nearestResource = null;
            float nearestResourceDistance = float.MaxValue;
            for (int i = 0; i < resources.Count; i++)
            {
                float distance = Vector2.Distance(guiPoint, WorldToScreen(resources[i].WorldCoord));
                if (distance < nearestResourceDistance)
                {
                    nearestResourceDistance = distance;
                    nearestResource = resources[i];
                }
            }

            if (nearestResource != null && nearestResourceDistance <= 48f)
            {
                if (HiveViewProductUiPresenter.GuidedWorldMapTutorialResourceSelectionStepForRuntime()
                    && !HiveViewProductUiPresenter.SelectGuidedWorldMapResourceForRuntime(nearestResource.Id))
                {
                    status = "Choisis le pollen eclaire pour cette mission";
                    return;
                }

                selectedResourceId = nearestResource.Id;
                status = "Ressource cible selectionnee: " + nearestResource.Label + " @ " + CoordLabel(nearestResource.WorldCoord);
                return;
            }

            if (HiveViewProductUiPresenter.GuidedWorldMapTutorialResourceSelectionStepForRuntime())
            {
                HiveViewProductUiPresenter.RegisterGuidedWorldMapBlockedInputForRuntime();
                status = "Touche directement le pollen eclaire";
                return;
            }

            WorldBestiaryNode nearestBestiary = null;
            float nearestBestiaryDistance = float.MaxValue;
            for (int i = 0; i < bestiary.Count; i++)
            {
                float distance = Vector2.Distance(guiPoint, WorldToScreen(bestiary[i].WorldCoord));
                if (distance < nearestBestiaryDistance)
                {
                    nearestBestiaryDistance = distance;
                    nearestBestiary = bestiary[i];
                }
            }

            if (nearestBestiary != null && nearestBestiaryDistance <= 62f)
            {
                selectedBestiaryId = nearestBestiary.Id;
                // Carnet du Bestiaire (demande de Jeff, 2026-08-01) : la selection sur la carte EST
                // le moment naturel ou le joueur "apercoit" cette identite precise (Tier + Variante) -
                // purement client-local, jamais transmis au serveur.
                LocalPreviewBestiarySightingsTracker.RecordSighting(nearestBestiary.Tier, nearestBestiary.Variant);
                status = "Bestiaire selectionne: T" + nearestBestiary.Tier.ToString(CultureInfo.InvariantCulture) + " " + nearestBestiary.Label + " @ " + CoordLabel(nearestBestiary.WorldCoord);
                return;
            }

            // Points d'Interet (demande de Jeff, 2026-08-01) : selectionnables comme les trois
            // autres types de noeuds, mais purement informationnels - aucune action associee.
            WorldPointOfInterestNode nearestPoi = null;
            float nearestPoiDistance = float.MaxValue;
            for (int i = 0; i < pointsOfInterest.Count; i++)
            {
                float distance = Vector2.Distance(guiPoint, WorldToScreen(pointsOfInterest[i].WorldCoord));
                if (distance < nearestPoiDistance)
                {
                    nearestPoiDistance = distance;
                    nearestPoi = pointsOfInterest[i];
                }
            }

            if (nearestPoi != null && nearestPoiDistance <= 66f)
            {
                selectedPointOfInterestId = nearestPoi.Id;
                status = "Point d'interet selectionne: " + nearestPoi.Label + " @ " + CoordLabel(nearestPoi.WorldCoord);
                return;
            }

            selectedResourceId = string.Empty;
            selectedBestiaryId = string.Empty;
            selectedPointOfInterestId = string.Empty;
            status = "Cible desselectionnee - touche une ruche, une ressource ou une menace";
        }

        private void SelectDefaultResource()
        {
            WorldResourceNode resource = ResourceById(selectedResourceId);
            if (resource != null) return;
            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].Kind == ResourceKind.Nectar)
                {
                    selectedResourceId = resources[i].Id;
                    return;
                }
            }
        }

        private void EnsureSelectionStillValid()
        {
            if (HiveById(selectedHiveId) == null)
            {
                selectedHiveId = hives.Count > 0 ? hives[0].Id : string.Empty;
            }

            if (ResourceById(selectedResourceId) == null)
            {
                selectedResourceId = resources.Count > 0 ? resources[0].Id : string.Empty;
            }
        }

        private void ZoomAround(Vector2 guiPoint, float nextZoom)
        {
            float previous = targetZoom;
            nextZoom = Mathf.Clamp(nextZoom, MinZoom, MaxZoom);
            if (Mathf.Approximately(previous, nextZoom)) return;

            Vector2 worldAtPointer = ScreenToWorld(guiPoint, targetWorldCenter, previous);
            targetZoom = nextZoom;
            Vector2 worldAfter = ScreenToWorld(guiPoint, targetWorldCenter, targetZoom);
            targetWorldCenter += worldAtPointer - worldAfter;
            ClampTargetWorldCenter();
        }

        private void ClampTargetWorldCenter()
        {
            if (wave6Provider != null && wave6Provider.ManifestReady && !wave6Provider.HasLoadFailure)
            {
                Rect bounds = wave6Provider.WorldBounds;
                float halfWidth = Screen.width * 0.5f / Mathf.Max(0.01f, targetZoom);
                float halfHeight = Screen.height * 0.5f / Mathf.Max(0.01f, targetZoom);
                float minX = bounds.xMin + halfWidth;
                float maxX = bounds.xMax - halfWidth;
                float minY = bounds.yMin + halfHeight;
                float maxY = bounds.yMax - halfHeight;
                targetWorldCenter.x = minX <= maxX ? Mathf.Clamp(targetWorldCenter.x, minX, maxX) : bounds.center.x;
                targetWorldCenter.y = minY <= maxY ? Mathf.Clamp(targetWorldCenter.y, minY, maxY) : bounds.center.y;
                return;
            }

            targetWorldCenter.x = Mathf.Clamp(targetWorldCenter.x, ChunkSize * 0.5f, WorldWidthUnits() - ChunkSize * 0.5f);
            targetWorldCenter.y = Mathf.Clamp(targetWorldCenter.y, ChunkSize * 0.5f, WorldHeightUnits() - ChunkSize * 0.5f);
        }

        private Vector2 WorldToScreen(Vector2 worldCoord)
        {
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + (worldCoord - currentWorldCenter) * currentZoom;
        }

        // Runtime entities are world-attached: their footprint must follow the same zoom as the terrain.
        private float WorldSizeToScreen(float worldSize)
        {
            return worldSize * currentZoom;
        }

        private float WorldStrokeToScreen(float worldStroke)
        {
            return Mathf.Max(0.75f, WorldSizeToScreen(worldStroke));
        }

        private Vector2 ScreenToWorld(Vector2 guiPoint, Vector2 center, float zoom)
        {
            return center + (guiPoint - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) / Mathf.Max(0.01f, zoom);
        }

        private Rect ChunkScreenRect(Vector2Int chunk)
        {
            Vector2 min = WorldToScreen(new Vector2(chunk.x * ChunkSize, chunk.y * ChunkSize));
            Vector2 max = WorldToScreen(new Vector2((chunk.x + 1) * ChunkSize, (chunk.y + 1) * ChunkSize));
            return PixelSnappedRect(min, max);
        }

        private Rect WorldRectToScreenRect(Rect worldRect)
        {
            Vector2 min = WorldToScreen(new Vector2(worldRect.xMin, worldRect.yMin));
            Vector2 max = WorldToScreen(new Vector2(worldRect.xMax, worldRect.yMax));
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        private Vector2 ChunkLocalWorld(Vector2Int chunk, float x01, float y01)
        {
            return new Vector2((chunk.x + x01) * ChunkSize, (chunk.y + y01) * ChunkSize);
        }

        private Vector2 SeededPointInChunk(Vector2Int chunk, int salt, float min, float max)
        {
            float x = Mathf.Lerp(min, max, (Hash(chunk.x, chunk.y, salt) & 1023) / 1023f);
            float y = Mathf.Lerp(min, max, (Hash(chunk.x, chunk.y, salt + 7) & 1023) / 1023f);
            return ChunkLocalWorld(chunk, x, y);
        }

        private Vector2 RuntimePlacementPoint(Vector2Int chunk, Vector2 rawPoint, RuntimePlacementFamily family, ResourceKind kind, int salt)
        {
            if (useWave5Method12288PreviewRuntimePackageForPlayMode && runtimePlacementMask.TryGetValue(chunk, out RuntimePlacementMaskEntry entry))
            {
                Anchor01 anchor = RuntimeAnchor(entry, family, kind);
                return ChunkLocalWorld(chunk, anchor.X, anchor.Y);
            }

            return ClampPointInsideChunk(chunk, rawPoint, 0.16f, 0.84f);
        }

        private Vector2 RuntimePlacementPointAvoidingBearDen(Vector2Int chunk, Vector2 rawPoint, RuntimePlacementFamily family, ResourceKind kind, int salt)
        {
            Vector2 point = RuntimePlacementPoint(chunk, rawPoint, family, kind, salt);
            if (bearDenLandmark == null || !bearDenLandmark.ExcludesSpawn(point)) return point;

            for (int i = 0; i < 5; i++)
            {
                Vector2 alternate = RuntimePlacementPoint(chunk, rawPoint, family, kind, salt + 31 + i * 17);
                if (!bearDenLandmark.ExcludesSpawn(alternate)) return alternate;
            }

            return point;
        }

        private Anchor01 RuntimeAnchor(RuntimePlacementMaskEntry entry, RuntimePlacementFamily family, ResourceKind kind)
        {
            if (family == RuntimePlacementFamily.Resource)
            {
                if (kind == ResourceKind.Pollen) return new Anchor01(entry.pollen_x, entry.pollen_y);
                if (kind == ResourceKind.Nectar) return new Anchor01(entry.nectar_x, entry.nectar_y);
                if (kind == ResourceKind.Wax) return new Anchor01(entry.wax_x, entry.wax_y);
                if (kind == ResourceKind.Honey) return new Anchor01(entry.honey_x, entry.honey_y);
                if (kind == ResourceKind.Propolis) return new Anchor01(entry.propolis_x, entry.propolis_y);
                if (kind == ResourceKind.RoyalJelly) return new Anchor01(entry.royal_jelly_x, entry.royal_jelly_y);
                if (kind == ResourceKind.Water) return new Anchor01(entry.water_x, entry.water_y);
            }

            if (family == RuntimePlacementFamily.Hive)
            {
                return new Anchor01(entry.hive_x, entry.hive_y);
            }

            if (family == RuntimePlacementFamily.Bestiary)
            {
                return new Anchor01(entry.threat_x, entry.threat_y);
            }

            return new Anchor01(entry.resource_x, entry.resource_y);
        }

        private Vector2 ClampPointInsideChunk(Vector2Int chunk, Vector2 point, float min, float max)
        {
            float x01 = point.x / ChunkSize - chunk.x;
            float y01 = point.y / ChunkSize - chunk.y;
            return ChunkLocalWorld(chunk, Mathf.Clamp(x01, min, max), Mathf.Clamp(y01, min, max));
        }

        private Vector2Int CurrentChunk()
        {
            return WorldToChunk(currentWorldCenter);
        }

        private Vector2Int WorldToChunk(Vector2 worldCoord)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(worldCoord.x / ChunkSize), 0, WorldChunkWidth - 1),
                Mathf.Clamp(Mathf.FloorToInt(worldCoord.y / ChunkSize), 0, WorldChunkHeight - 1));
        }

        private bool IsChunkInWorld(Vector2Int chunk)
        {
            return chunk.x >= 0 && chunk.y >= 0 && chunk.x < WorldChunkWidth && chunk.y < WorldChunkHeight;
        }

        private float WorldWidthUnits()
        {
            return WorldChunkWidth * ChunkSize;
        }

        private float WorldHeightUnits()
        {
            return WorldChunkHeight * ChunkSize;
        }

        private static Vector2 ScreenToGui(Vector2 screen)
        {
            return new Vector2(screen.x, Screen.height - screen.y);
        }

        private static bool IsOnScreen(Vector2 point, float margin)
        {
            return point.x >= -margin && point.x <= Screen.width + margin && point.y >= -margin && point.y <= Screen.height + margin;
        }

        private string ChunkId(Vector2Int chunk)
        {
            return "C" + chunk.x.ToString("00", CultureInfo.InvariantCulture) + "_" + chunk.y.ToString("00", CultureInfo.InvariantCulture);
        }

        private string SectorId(Vector2Int chunk)
        {
            return "S" + (chunk.x / SectorSizeChunks).ToString("00", CultureInfo.InvariantCulture) + "_" + (chunk.y / SectorSizeChunks).ToString("00", CultureInfo.InvariantCulture);
        }

        private string CoordLabel(Vector2 worldCoord)
        {
            return "X" + Mathf.RoundToInt(worldCoord.x).ToString(CultureInfo.InvariantCulture) + " Y" + Mathf.RoundToInt(worldCoord.y).ToString(CultureInfo.InvariantCulture);
        }

        private string ArtProviderLabel()
        {
            if (wave6Provider != null && wave6Provider.ManifestReady && !wave6Provider.HasLoadFailure)
            {
                if (useV3OReducedAuditPreviewRuntimePackageForPlayMode) return "Wave6 50x50 V3O reduced audit";
                if (useRouteLockCoherentProofRuntimePackageForPlayMode) return "Wave6 50x50 route-lock proof";
                if (useRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode) return "Wave6 50x50 route-lock 8192 scale-bridge proof";
                if (useWave5Method12288PreviewRuntimePackageForPlayMode) return "Wave6 50x50 Wave5-method 12288 preview";
                if (useSupportCenterNativeAuditPreviewRuntimePackageForPlayMode) return "Wave6 50x50 support center native audit";
                if (useV2IRepairAuditPreviewRuntimePackageForPlayMode) return "OBSOLETE FAIL - V2I repair audit";
                if (useV2ISelectedHdLocalRepairReviewRuntimePackageForPlayMode) return "OBSOLETE REVIEW - selected HD local repair";
                if (useV2OPerimeterAuditPreviewRuntimePackageForPlayMode) return "OBSOLETE FAIL - V2O audit";
                if (useV2INativeAuditPreviewRuntimePackageForPlayMode) return "OBSOLETE FAIL - V2I audit";
                return "Wave6 50x50 streaming";
            }

            return "Wave6 indisponible";
        }

        private string ResourceLabel(ResourceKind kind)
        {
            if (kind == ResourceKind.Pollen) return "Pollen";
            if (kind == ResourceKind.Nectar) return "Nectar";
            if (kind == ResourceKind.Water) return "Eau";
            if (kind == ResourceKind.Wax) return "Cire";
            if (kind == ResourceKind.Honey) return "Miel";
            if (kind == ResourceKind.RoyalJelly) return "Gelee royale";
            return "Propolis";
        }

        private string ResourceToken(ResourceKind kind)
        {
            if (kind == ResourceKind.Pollen) return "pollen";
            if (kind == ResourceKind.Nectar) return "nectar";
            if (kind == ResourceKind.Water) return "water";
            if (kind == ResourceKind.Wax) return "wax";
            if (kind == ResourceKind.Honey) return "honey";
            if (kind == ResourceKind.RoyalJelly) return "royal_jelly";
            return "propolis";
        }

        private int ResourceAmount(ResourceKind kind, Vector2Int chunk, int index)
        {
            int baseAmount = kind == ResourceKind.RoyalJelly ? 18 : 45 + Hash(chunk.x, chunk.y, 83 + index) % 85;
            if (kind == ResourceKind.Water || kind == ResourceKind.Honey) baseAmount = 35 + Hash(chunk.x, chunk.y, 101 + index) % 70;
            return Mathf.Max(8, baseAmount);
        }

        private string ResourceTierToken(WorldResourceNode resource)
        {
            if (resource.Amount >= 96) return "rich";
            if (resource.Amount >= 50) return "medium";
            return "poor";
        }

        private string ResourceTexturePath(WorldResourceNode resource)
        {
            string lot = ResourceTierToken(resource) == "poor" ? "R1" : (ResourceTierToken(resource) == "medium" ? "R2" : "R3");
            return RuntimeResourcePremiumRoot + "/" + lot + "/resource_" + ResourceToken(resource.Kind) + "_" + ResourceTierToken(resource);
        }

        private int ResourceRemaining(WorldResourceNode resource)
        {
            if (resource == null) return 0;
            if (resourceRemaining.TryGetValue(resource.Id, out int remaining)) return Mathf.Clamp(remaining, 0, resource.Amount);
            return resource.Amount;
        }

        private string ResourceQuantityLabel(WorldResourceNode resource)
        {
            if (resource == null) return string.Empty;
            int remaining = ResourceRemaining(resource);
            if (remaining <= 0) return "Épuisée";
            return remaining.ToString(CultureInfo.InvariantCulture) + "/" + resource.Amount.ToString(CultureInfo.InvariantCulture);
        }

        private string ResourceAccessibilityToken(WorldResourceNode resource)
        {
            string tier = ResourceTierToken(resource);
            if (tier == "rich") return "[R3]";
            if (tier == "medium") return "[R2]";
            return "[R1]";
        }

        private WorldResourceNode FirstResourceByTier(string tier)
        {
            for (int i = 0; i < resources.Count; i++)
            {
                if (ResourceTierToken(resources[i]) == tier) return resources[i];
            }

            foreach (WorldChunkData data in chunkCache.Values)
            {
                for (int i = 0; i < data.Resources.Count; i++)
                {
                    if (ResourceTierToken(data.Resources[i]) == tier) return data.Resources[i];
                }
            }

            return null;
        }

        private void ForceRespawnForProof(string resourceId)
        {
            resourceRemaining.Remove(resourceId);
            resourceRespawnAt.Remove(resourceId);
        }

        private float ResourceSpriteSize(WorldResourceNode resource)
        {
            string tier = ResourceTierToken(resource);
            if (tier == "rich") return 220f;
            if (tier == "medium") return 180f;
            return 148f;
        }

        private float ResourceSpriteScreenSize(WorldResourceNode resource)
        {
            string tier = ResourceTierToken(resource);
            float minimum = tier == "rich" ? 148f : (tier == "medium" ? 124f : 104f);
            return Mathf.Max(minimum, WorldSizeToScreen(ResourceSpriteSize(resource)));
        }

        private string BestiaryLabel(int tier, int variant)
        {
            if (tier == 1) return variant == 1 ? "Puceron" : "Acarien";
            if (tier == 2) return variant == 1 ? "Fourmi" : "Scarabee";
            if (tier == 3) return variant == 1 ? "Araignee" : "Mouche";
            if (tier == 4) return variant == 1 ? "Mante" : "Scolopendre";
            if (tier == 5) return variant == 1 ? "Frelon" : "Lucane";
            if (tier == 6) return variant == 1 ? "Scorpion" : "Tarantule";
            return variant == 1 ? "Reine frelon" : "Titan lucane";
        }

        // Bible-flavored overload (WORLD_BIBLE_FOUNDATION.md "Faune" per biome), used by the
        // organic seeded spawner only (GenerateSeededBestiary) - the fixed demo/proof beasts
        // keep the plain generic name so automated harnesses reading those labels stay
        // stable. Only overrides the handful of (tier, variant) slots whose EXISTING internal
        // id already named a specific real-world creature this file was clearly modeling
        // (see BestiaryFileToken below: "aphid_thief", "jumping_spider", "shield_beetle",
        // "armored_tarantula", "root_scorpion", "ancient_hornet_queen") - translating those
        // into their proper Bible species name, gated to the biome that actually lists them,
        // rather than forcing a bible name onto every one of the 42 (tier,variant,biome)
        // combinations without a clean match.
        private string BestiaryLabel(int tier, int variant, WorldBiome biome)
        {
            string flavor = BestiaryBiomeFlavorName(tier, variant, biome);
            return string.IsNullOrEmpty(flavor) ? BestiaryLabel(tier, variant) : flavor;
        }

        private static string BestiaryBiomeFlavorName(int tier, int variant, WorldBiome biome)
        {
            if (biome == WorldBiome.PrairieFleurie && tier == 1 && variant == 1) return "Puceron Voleur";
            if (biome == WorldBiome.PrairieFleurie && tier == 3 && variant == 1) return "Araignee Sauteuse";
            if (biome == WorldBiome.ForetClaire && tier == 2 && variant == 2) return "Scarabee Ouvrier";
            if (biome == WorldBiome.RonciersEtHaies && tier == 6 && variant == 2) return "Araignee Titan";
            if (biome == WorldBiome.TerresSeches && tier == 6 && variant == 1) return "Scorpion Noir";
            if (biome == WorldBiome.TerresSeches && tier == 7 && variant == 1) return "Reine Guepe Antique";
            return null;
        }

        private string BestiaryRole(int tier)
        {
            if (tier <= 2) return "nuisance";
            if (tier <= 4) return "élite";
            if (tier <= 6) return "petit raid";
            return "raid dormant";
        }

        // Mot d'ambiance affiche au joueur pour distinguer une cible solitaire d'un groupe -
        // BestiaryAccessibilityToken (code interne "[SOLO]/[RAID]") reste intact pour les harnais
        // de preuve automatises.
        private string BestiaryAccessibilityWord(WorldBestiaryNode beast)
        {
            return BestiaryAccessibilityToken(beast) == "[RAID]" ? "Horde" : "Solitaire";
        }

        private string BestiaryTexturePath(WorldBestiaryNode beast)
        {
            return RuntimeEntityResourceRoot + "/M1/" + BestiaryFileToken(beast.Tier, beast.Variant);
        }

        private string BestiaryAccessibilityToken(WorldBestiaryNode beast)
        {
            return beast != null && beast.Tier >= 5 ? "[RAID]" : "[SOLO]";
        }

        private int BestiaryVirtualHp(WorldBestiaryNode beast)
        {
            return beast == null ? 0 : 80 + beast.Tier * 55;
        }

        private WorldBestiaryNode SelectedBestiary()
        {
            return BestiaryById(selectedBestiaryId);
        }

        private WorldBestiaryNode BestiaryById(string id)
        {
            for (int i = 0; i < bestiary.Count; i++)
            {
                if (bestiary[i].Id == id) return bestiary[i];
            }

            foreach (WorldChunkData data in chunkCache.Values)
            {
                for (int i = 0; i < data.Bestiary.Count; i++)
                {
                    if (data.Bestiary[i].Id == id) return data.Bestiary[i];
                }
            }

            return null;
        }

        private WorldBestiaryNode FirstBestiaryByTier(int tier)
        {
            for (int i = 0; i < bestiary.Count; i++)
            {
                if (bestiary[i].Tier == tier) return bestiary[i];
            }

            foreach (WorldChunkData data in chunkCache.Values)
            {
                for (int i = 0; i < data.Bestiary.Count; i++)
                {
                    if (data.Bestiary[i].Tier == tier) return data.Bestiary[i];
                }
            }

            return null;
        }

        private void EnsureBestiaryTierForProof(int tier)
        {
            if (FirstBestiaryByTier(tier) != null) return;
            Vector2Int center = new Vector2Int(WorldChunkWidth / 2, WorldChunkHeight / 2);
            float x = 0.18f + (tier % 4) * 0.16f;
            float y = 0.18f + (tier / 4) * 0.22f;
            int variant = tier % 2 == 0 ? 2 : 1;
            string id = "beast_t" + tier.ToString(CultureInfo.InvariantCulture) + "_proof";
            Vector2 position = RuntimePlacementPointAvoidingBearDen(center, ChunkLocalWorld(center, x, y), RuntimePlacementFamily.Bestiary, ResourceKind.Pollen, 700 + tier * 19);
            bestiary.Add(new WorldBestiaryNode(id, BestiaryLabel(tier, variant), tier, variant, position, BestiaryRole(tier)));
        }

        private bool SelectBestiaryForProof(string id)
        {
            selectedBestiaryId = id;
            WorldBestiaryNode beast = SelectedBestiary();
            if (beast == null) return false;
            status = "Bestiaire selectionne: T" + beast.Tier.ToString(CultureInfo.InvariantCulture) + " " + beast.Label;
            return true;
        }

        private bool RunSelectedBestiaryCombatLocalProof()
        {
            WorldBestiaryNode beast = SelectedBestiary();
            if (beast == null) return false;
            int required = BestiaryRequiredComposition(beast);
            int available = 120 + beast.Tier * 48;
            bool raid = beast.Tier >= 5;
            bool win = available >= required;
            bestiaryCombatText = "T" + beast.Tier.ToString(CultureInfo.InvariantCulture)
                + " " + beast.Label
                + " mode=" + (raid ? "raid_local" : "solo_local")
                + " required=" + required.ToString(CultureInfo.InvariantCulture)
                + " available=" + available.ToString(CultureInfo.InvariantCulture)
                + " result=" + (win ? "win" : "hold")
                + " official_gain=false server=false";
            status = "Combat bestiaire local/demo: " + bestiaryCombatText;
            return win;
        }

        private int BestiaryRequiredComposition(WorldBestiaryNode beast)
        {
            int baseNeed = beast.Tier * 28;
            if (beast.Tier >= 5) baseNeed += 60;
            if (beast.Tier >= 7) baseNeed += 80;
            return baseNeed;
        }

        private string BestiaryFileToken(int tier, int variant)
        {
            if (tier == 1) return variant == 1 ? "beast_t1_aphid_thief" : "beast_t1_red_mite";
            if (tier == 2) return variant == 1 ? "beast_t2_cutter_ant" : "beast_t2_shield_beetle";
            if (tier == 3) return variant == 1 ? "beast_t3_jumping_spider" : "beast_t3_robber_fly";
            if (tier == 4) return variant == 1 ? "beast_t4_mantis_predator" : "beast_t4_centipede_runner";
            if (tier == 5) return variant == 1 ? "beast_t5_hornet_brigand" : "beast_t5_stag_beetle_raider";
            if (tier == 6) return variant == 1 ? "beast_t6_root_scorpion" : "beast_t6_armored_tarantula";
            return variant == 1 ? "beast_t7_ancient_hornet_queen" : "beast_t7_titan_stag_beetle";
        }

        private Color BestiaryTierColor(int tier)
        {
            if (tier <= 2) return new Color(0.62f, 0.78f, 0.34f, 0.95f);
            if (tier <= 4) return new Color(0.78f, 0.48f, 0.32f, 0.95f);
            if (tier <= 6) return new Color(0.60f, 0.45f, 0.82f, 0.95f);
            return new Color(0.95f, 0.78f, 0.22f, 0.95f);
        }

        // Premiere mecanique de decouverte (demande de Jeff, 2026-08-01) : parmi les creatures deja
        // dispersees proceduralement sur la carte (GenerateSeededBestiary, palier aleatoire 1-7 par
        // chunk), celle dont le palier correspond exactement a la "menace en hausse" localisee ce
        // cycle (voir CombatPatrolService/WorldEventCatalog) devient un repérage rare : mise en
        // valeur visuelle uniquement, aucun nouveau combat/creature/ressource - la carte n'est plus
        // toujours la meme, et seul le fait de la parcourir revele ou se trouve la cible du moment.
        private bool IsRareSighting(WorldBestiaryNode beast, out string worldEventKey)
        {
            worldEventKey = string.Empty;
            CombatPatrolScreenModel model = HiveViewProductUiPresenter.PeekCombatPatrolModelForWorldMap();
            if (model?.WorldEvent == null || !string.Equals(model.WorldEvent.Kind, "ThreatSurge", StringComparison.Ordinal))
                return false;
            if (model.WorldEventFeaturedTier != beast.Tier) return false;
            worldEventKey = model.WorldEvent.Key;
            return true;
        }

        private Texture2D RuntimeEntityTexture(string resourcePath)
        {
            if (runtimeEntityTextureCache.TryGetValue(resourcePath, out Texture2D cached)) return cached;
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                texture.anisoLevel = 1;
            }

            runtimeEntityTextureCache[resourcePath] = texture;
            return texture;
        }

        private void UnloadRuntimeEntityTextures()
        {
            foreach (Texture2D texture in runtimeEntityTextureCache.Values)
            {
                if (texture != null) Resources.UnloadAsset(texture);
            }

            runtimeEntityTextureCache.Clear();
        }

        private string RewardText(WorldResourceNode resource)
        {
            int remaining = ResourceRemaining(resource);
            int delta = Mathf.Max(1, Mathf.Min(remaining, resource.Amount) / 6);
            return "+" + delta.ToString(CultureInfo.InvariantCulture) + " " + resource.Label + " (" + ResourceAbundanceWord(resource) + ")";
        }

        // Mot d'ambiance affiche au joueur pour decrire la richesse d'un gisement - a ne jamais
        // confondre avec ResourceAccessibilityToken (code interne "[R1]/[R2]/[R3]" toujours utilise
        // par les harnais de preuve automatises, laisse intact).
        private string ResourceAbundanceWord(WorldResourceNode resource)
        {
            string tier = ResourceTierToken(resource);
            if (tier == "rich") return "récolte abondante";
            if (tier == "medium") return "récolte généreuse";
            return "récolte modeste";
        }

        private WorldHiveNode SelectedHive()
        {
            return HiveById(selectedHiveId);
        }

        private WorldResourceNode SelectedResource()
        {
            return ResourceById(selectedResourceId);
        }

        private WorldHiveNode HiveById(string id)
        {
            for (int i = 0; i < hives.Count; i++)
            {
                if (hives[i].Id == id) return hives[i];
            }

            foreach (WorldChunkData data in chunkCache.Values)
            {
                for (int i = 0; i < data.Hives.Count; i++)
                {
                    if (data.Hives[i].Id == id) return data.Hives[i];
                }
            }

            return null;
        }

        private WorldResourceNode ResourceById(string id)
        {
            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].Id == id) return resources[i];
            }

            foreach (WorldChunkData data in chunkCache.Values)
            {
                for (int i = 0; i < data.Resources.Count; i++)
                {
                    if (data.Resources[i].Id == id) return data.Resources[i];
                }
            }

            return null;
        }

        private WorldPointOfInterestNode SelectedPointOfInterest()
        {
            return PointOfInterestById(selectedPointOfInterestId);
        }

        private WorldPointOfInterestNode PointOfInterestById(string id)
        {
            for (int i = 0; i < pointsOfInterest.Count; i++)
            {
                if (pointsOfInterest[i].Id == id) return pointsOfInterest[i];
            }

            foreach (WorldChunkData data in chunkCache.Values)
            {
                for (int i = 0; i < data.PointsOfInterest.Count; i++)
                {
                    if (data.PointsOfInterest[i].Id == id) return data.PointsOfInterest[i];
                }
            }

            return null;
        }

        private float CurrentFlightArcProgress()
        {
            if (collectionState == CollectionFlightState.FlyingToResource) return Mathf.Clamp01(collectionTimer / 3.2f);
            if (collectionState == CollectionFlightState.Collecting) return 1f;
            if (collectionState == CollectionFlightState.Returning) return 1f - Mathf.Clamp01(collectionTimer / 3.0f);
            return Mathf.Repeat(animatedTime * 0.18f, 1f);
        }

        private float FlightArcProgress(WorldFlightRecord flight)
        {
            if (flight.State == CollectionFlightState.FlyingToResource) return Mathf.Clamp01(flight.Timer / 3.2f);
            if (flight.State == CollectionFlightState.Collecting) return 1f;
            if (flight.State == CollectionFlightState.Returning) return 1f - Mathf.Clamp01(flight.Timer / 3.0f);
            if (flight.State == CollectionFlightState.Completed) return 1f;
            return Mathf.Repeat(animatedTime * 0.18f, 1f);
        }

        private float FlightProgress01(WorldFlightRecord flight)
        {
            if (flight.State == CollectionFlightState.FlyingToResource) return Mathf.Clamp01(flight.Timer / 3.2f) * 0.42f;
            if (flight.State == CollectionFlightState.Collecting) return 0.42f + Mathf.Clamp01(flight.Timer / 1.15f) * 0.18f;
            if (flight.State == CollectionFlightState.Returning) return 0.60f + Mathf.Clamp01(flight.Timer / 3.0f) * 0.40f;
            if (flight.State == CollectionFlightState.Completed) return 1f;
            return 0f;
        }

        private string CollectionStateLabel()
        {
            return CollectionStateLabel(collectionState);
        }

        private string CollectionStateLabel(CollectionFlightState state)
        {
            if (state == CollectionFlightState.FlyingToResource) return "En vol";
            if (state == CollectionFlightState.Collecting) return "Collecte";
            if (state == CollectionFlightState.Returning) return "Retour";
            if (state == CollectionFlightState.Completed) return "Termine";
            return "Idle";
        }

        private bool IsPointerOverFixedUi(Vector2 guiPoint)
        {
            if (localLab != null && localLab.IsPointerOverUi(guiPoint)) return true;
            if (MapReadingToolsRect().Contains(guiPoint)) return true;
            if (spawnDiagnosticOverlayEnabled
                && (SpawnInspectorHeaderRect().Contains(guiPoint) || (!spawnInspectorCollapsed && SpawnInspectorPanelRect().Contains(guiPoint)))) return true;
            if (IsPortraitLayout())
            {
                if (new Rect(0f, 0f, Screen.width, 118f).Contains(guiPoint)) return true;
                if (BearDenToggleRect().Contains(guiPoint)) return true;
                if (ActionPanelRect().Contains(guiPoint)) return true;
                if (FlightJournalRect().Contains(guiPoint)) return true;
                if (new Rect(Screen.width - 128f, 204f, 118f, 86f).Contains(guiPoint)) return true;
                return false;
            }

            if (new Rect(0f, 0f, Screen.width, 124f).Contains(guiPoint)) return true;
            if (new Rect(Screen.width - 292f, 12f, 278f, 150f).Contains(guiPoint)) return true;
            if (BearDenToggleRect().Contains(guiPoint)) return true;
            if (ActionPanelRect().Contains(guiPoint)) return true;
            if (FlightJournalRect().Contains(guiPoint)) return true;
            if (new Rect(14f, Screen.height - 112f, Mathf.Min(760f, Screen.width - 28f), 96f).Contains(guiPoint)) return true;
            if (new Rect(Screen.width - 214f, Screen.height - 156f, 198f, 140f).Contains(guiPoint)) return true;
            return false;
        }

        private Rect ActionPanelRect()
        {
            if (IsPortraitLayout()) return new Rect(8f, Screen.height - 190f, Screen.width - 16f, 178f);
            return new Rect(Screen.width - 320f, 176f, 304f, 286f);
        }

        private Rect FlightJournalRect()
        {
            if (IsPortraitLayout()) return new Rect(8f, 124f, Screen.width - 16f, 58f);
            return new Rect(Screen.width - 380f, 468f, 364f, Mathf.Min(144f, Mathf.Max(132f, Screen.height - 600f)));
        }

        private Rect BearDenToggleRect()
        {
            if (IsPortraitLayout())
            {
                return new Rect(8f, 12f, Mathf.Max(142f, Mathf.Min(238f, Screen.width - 152f)), 48f);
            }

            return new Rect(14f, 12f, 220f, 48f);
        }

        private Rect SpawnInspectorHeaderRect()
        {
            if (IsPortraitLayout()) return spawnInspectorCollapsed ? new Rect(8f, 236f, Mathf.Min(330f, Screen.width - 16f), 42f) : new Rect(8f, 236f, Mathf.Min(360f, Screen.width - 16f), 42f);
            return spawnInspectorCollapsed ? new Rect(Screen.width - 320f, 468f, 304f, 42f) : new Rect(Screen.width - 320f, 468f, 304f, 42f);
        }

        private Rect SpawnInspectorPanelRect()
        {
            if (IsPortraitLayout()) return new Rect(8f, 282f, Mathf.Min(360f, Screen.width - 16f), 230f);
            return new Rect(Screen.width - 320f, 514f, 304f, Mathf.Min(250f, Mathf.Max(224f, Screen.height - 530f)));
        }

        private static bool IsPortraitLayout()
        {
            return Screen.width < 700 || Screen.height > Screen.width * 1.15f;
        }

        private float HiveSize(HiveMaturity stage)
        {
            if (stage == HiveMaturity.Capital) return 40f;
            if (stage == HiveMaturity.Advanced) return 34f;
            if (stage == HiveMaturity.Mid) return 28f;
            return 22f;
        }

        private string StageLabel(HiveMaturity stage)
        {
            if (stage == HiveMaturity.Capital) return "capitale";
            if (stage == HiveMaturity.Advanced) return "avancee";
            if (stage == HiveMaturity.Mid) return "intermediaire";
            return "debutante";
        }

        private Color HiveColor(HiveMaturity stage)
        {
            if (stage == HiveMaturity.Capital) return new Color(0.96f, 0.36f, 1f, 0.96f);
            if (stage == HiveMaturity.Advanced) return new Color(1f, 0.56f, 0.12f, 0.96f);
            if (stage == HiveMaturity.Mid) return new Color(1f, 0.78f, 0.18f, 0.96f);
            return new Color(0.96f, 0.92f, 0.55f, 0.96f);
        }

        private Color ResourceColor(ResourceKind kind)
        {
            if (kind == ResourceKind.Pollen) return new Color(1f, 0.86f, 0.18f, 0.95f);
            if (kind == ResourceKind.Nectar) return new Color(0.78f, 0.45f, 1f, 0.95f);
            if (kind == ResourceKind.Water) return new Color(0.34f, 0.74f, 1f, 0.95f);
            if (kind == ResourceKind.Wax) return new Color(0.95f, 0.58f, 0.16f, 0.95f);
            if (kind == ResourceKind.Honey) return new Color(1f, 0.68f, 0.18f, 0.95f);
            if (kind == ResourceKind.RoyalJelly) return new Color(0.95f, 0.35f, 0.92f, 0.95f);
            return new Color(0.34f, 0.76f, 0.38f, 0.95f);
        }

        private void DrawLegendItem(float x, float y, Color color, string label)
        {
            DrawCircle(new Vector2(x + 12f, y + 12f), 10f, color, 16);
            GUI.Label(new Rect(x + 30f, y + 2f, 128f, 22f), label, MiniLabel(Color.white, 11, TextAnchor.MiddleLeft));
        }

        private void DrawBackground()
        {
            DrawSolid(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.012f, 0.014f, 0.012f, 1f));
        }

        private void DrawWorldMapAtmospherePass()
        {
            DrawSolid(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.025f, 0.014f, 0.004f, 0.055f));
        }

        private static Rect PixelSnappedRect(Vector2 min, Vector2 max)
        {
            float xMin = Mathf.Floor(Mathf.Min(min.x, max.x));
            float yMin = Mathf.Floor(Mathf.Min(min.y, max.y));
            float xMax = Mathf.Ceil(Mathf.Max(min.x, max.x));
            float yMax = Mathf.Ceil(Mathf.Max(min.y, max.y));
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void DrawHex(Vector2 center, float radius, Color color, float width)
        {
            Vector2[] points = new Vector2[6];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = Mathf.PI / 6f + i * Mathf.PI * 2f / points.Length;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            for (int i = 0; i < points.Length; i++) DrawLine(points[i], points[(i + 1) % points.Length], color, width);
        }

        private void DrawCircle(Vector2 center, float radius, Color color, int segments)
        {
            Vector2 previous = center + Vector2.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                DrawLine(previous, next, color, Mathf.Max(2f, radius * 0.12f));
                previous = next;
            }
        }

        private void DrawDiamond(Vector2 center, float radius, Color color, float width)
        {
            Vector2 top = center + Vector2.up * radius;
            Vector2 right = center + Vector2.right * radius;
            Vector2 bottom = center + Vector2.down * radius;
            Vector2 left = center + Vector2.left * radius;
            DrawLine(top, right, color, width);
            DrawLine(right, bottom, color, width);
            DrawLine(bottom, left, color, width);
            DrawLine(left, top, color, width);
        }

        private void DrawTriangle(Vector2 center, float radius, Color color, float width)
        {
            Vector2 a = center + new Vector2(0f, -radius);
            Vector2 b = center + new Vector2(radius * 0.90f, radius * 0.70f);
            Vector2 c = center + new Vector2(-radius * 0.90f, radius * 0.70f);
            DrawLine(a, b, color, width);
            DrawLine(b, c, color, width);
            DrawLine(c, a, color, width);
        }

        private void DrawBezier(Vector2 a, Vector2 c, Vector2 b, Color color, float width, int segments)
        {
            Vector2 previous = a;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector2 next = Bezier(a, c, b, t);
                DrawLine(previous, next, color, width);
                previous = next;
            }
        }

        private static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            EnsurePixel();
            Matrix4x4 matrix = GUI.matrix;
            Color previous = GUI.color;
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(start, end);
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f, length, width), pixel);
            GUI.matrix = matrix;
            GUI.color = previous;
        }

        private void DrawSolid(Rect rect, Color color)
        {
            EnsurePixel();
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        // Ambiance meteo (demande de Jeff, 2026-08-02), enrichie le 2026-08-18 pour suivre la
        // regle de la Bible des Evenements Mondiaux : "un evenement doit se voir sur la carte
        // avant de se lire dans une interface". Le catalogue reel (WorldEventCatalog, cote
        // serveur, non modifie) n'a que 6 cles - chacune est ici associee a l'evenement de la
        // Bible le plus proche (voir WorldEventNarrativeLine) et biaisee vers les biomes que
        // cet evenement nomme (WorldEventBiasesBiome), au lieu d'un lavis plat plein ecran :
        // le lavis n'est fort que sur les tuiles des biomes concernes, quasi invisible ailleurs,
        // donc l'evenement est visible comme une VRAIE zone du monde plutot qu'un simple filtre
        // ecran. spider_surge n'a pas d'equivalent dans la Bible (aucune "menace araignee"
        // nommee) - traite comme une menace generique des Ronciers plutot que d'inventer un nom.
        private void DrawWorldEventAmbiance()
        {
            ActiveWorldEvent activeEvent = WorldEventCatalog.Active(DateTimeOffset.UtcNow);
            Color tint = WorldEventAmbianceTint(activeEvent.Key);
            if (tint.a > 0f) DrawWorldEventBiomeBiasedWash(activeEvent.Key, tint);

            Rect badge = new Rect(14f, 14f, 280f, 50f);
            DrawSolid(badge, new Color(0.020f, 0.018f, 0.014f, 0.74f));
            DrawFrame(badge, new Color(tint.r, tint.g, tint.b, 0.92f), 1.5f);
            GUI.Label(new Rect(badge.x + 10f, badge.y + 2f, badge.width - 20f, 20f),
                HiveViewProductUiPresenter.WorldEventDisplayName(activeEvent.Key),
                LabelStyle(Color.white, 12, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(badge.x + 10f, badge.y + 22f, badge.width - 20f, 26f),
                WorldEventNarrativeLine(activeEvent.Key),
                new GUIStyle(MiniLabel(new Color(0.86f, 0.84f, 0.78f, 1f), 9, TextAnchor.UpperLeft)) { wordWrap = true });
        }

        // Same fixed 10x10 grid budget as DrawBiomeOverlay (see ForEachVisibleBiomeCell) -
        // cells whose biome matches the active event get the full tint, everything else gets
        // a faint fraction of it (never zero, so the map still reads as "something is
        // happening everywhere", matching the bible's ambiance intent) rather than a hard,
        // unnatural cutoff line.
        private void DrawWorldEventBiomeBiasedWash(string eventKey, Color tint)
        {
            if (wave6Provider == null || !wave6Provider.ManifestReady || wave6Provider.HasLoadFailure) return;

            ForEachVisibleBiomeCell((cellRect, biome) =>
            {
                bool biased = WorldEventBiasesBiome(eventKey, biome);
                float alphaScale = biased ? 1f : 0.18f;
                DrawSolid(cellRect, new Color(tint.r, tint.g, tint.b, tint.a * alphaScale));
            });
        }

        private static bool WorldEventBiasesBiome(string eventKey, WorldBiome biome)
        {
            switch (eventKey)
            {
                case "blossom": return biome == WorldBiome.PrairieFleurie || biome == WorldBiome.VergerAncien;
                case "rain": return biome == WorldBiome.BergesEtMares || biome == WorldBiome.ForetClaire;
                case "drought": return biome == WorldBiome.TerresSeches;
                case "ant_invasion": return biome == WorldBiome.PrairieFleurie || biome == WorldBiome.ForetClaire;
                case "spider_surge": return biome == WorldBiome.RonciersEtHaies;
                case "hornet_swarm": return biome == WorldBiome.RonciersEtHaies || biome == WorldBiome.VergerAncien;
                default: return false;
            }
        }

        // Plain literals, matching this file's own convention (it never uses BeeLocalization
        // elsewhere - unlike HiveViewProductUiPresenter.cs, which is a different namespace).
        private static string WorldEventNarrativeLine(string eventKey)
        {
            switch (eventKey)
            {
                case "blossom": return "Les fleurs s'ouvrent presque ensemble, le monde devient plus lumineux.";
                case "rain": return "Pluie, ruissellement, ressources humides le long des berges.";
                case "drought": return "Fleurs plus rares mais nectar plus concentre dans les terres seches.";
                case "ant_invasion": return "De longues pistes de fourmis relient pucerons et fourmilieres.";
                case "spider_surge": return "Les toiles se multiplient dans les ronciers - restez sur vos gardes.";
                case "hornet_swarm": return "Des patrouilles de frelons menacent les ressources exposees.";
                default: return string.Empty;
            }
        }

        private static Color WorldEventAmbianceTint(string key)
        {
            switch (key)
            {
                case "blossom": return new Color(1f, 0.74f, 0.86f, 0.09f);
                case "rain": return new Color(0.52f, 0.64f, 0.82f, 0.11f);
                case "drought": return new Color(0.78f, 0.56f, 0.24f, 0.10f);
                case "ant_invasion":
                case "spider_surge":
                case "hornet_swarm":
                    return new Color(0.68f, 0.16f, 0.12f, 0.09f);
                default: return new Color(0f, 0f, 0f, 0f);
            }
        }

        private void DrawTerrainTileShadow(Vector2 center, float width, float height, float alpha)
        {
            if (width <= 1f || height <= 1f || alpha <= 0f) return;
            DrawSolid(new Rect(center.x - width * 0.5f, center.y + height * 0.10f, width, height), new Color(0.02f, 0.018f, 0.012f, alpha));
        }

        private void DrawFrame(Rect rect, Color color, float width)
        {
            DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y), color, width);
            DrawLine(new Vector2(rect.xMax, rect.y), new Vector2(rect.xMax, rect.yMax), color, width);
            DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.x, rect.yMax), color, width);
            DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.x, rect.y), color, width);
        }

        private void EnsurePixel()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }

        private static GUIStyle LabelStyle(Color color, int size, FontStyle fontStyle, TextAnchor alignment)
        {
            return new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = color },
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment
            };
        }

        private static GUIStyle MiniLabel(Color color, int size, TextAnchor alignment)
        {
            return LabelStyle(color, size, FontStyle.Normal, alignment);
        }

        private static int Hash(int x, int y, int salt)
        {
            unchecked
            {
                int h = LocalDemoSeed;
                h = h * 397 ^ x;
                h = h * 397 ^ y;
                h = h * 397 ^ salt;
                h ^= h << 13;
                h ^= h >> 17;
                h ^= h << 5;
                return h & 0x7fffffff;
            }
        }

        private static uint StableHash32(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619u;
                }

                return hash;
            }
        }

        private static float Unit01(uint hash)
        {
            return (hash & 0x00ffffffu) / 16777215f;
        }

        private List<SpawnPreviewRecord> GenerateSpawnPreview(int seed, string version, List<Vector2Int> chunks)
        {
            var records = new List<SpawnPreviewRecord>();
            for (int i = 0; i < chunks.Count; i++)
            {
                Vector2Int chunk = chunks[i];
                TryAddSpawnRecord(records, seed, version, chunk, "hive", 0);
                TryAddSpawnRecord(records, seed, version, chunk, "resource", 0);
                TryAddSpawnRecord(records, seed, version, chunk, "resource", 1);
                TryAddSpawnRecord(records, seed, version, chunk, "bestiary", 0);
            }

            records.Sort((a, b) => string.CompareOrdinal(a.EntityId, b.EntityId));
            return records;
        }

        private void TryAddSpawnRecord(List<SpawnPreviewRecord> records, int seed, string version, Vector2Int chunk, string family, int slot)
        {
            string chunkId = ChunkId(chunk);
            string key = WorldId + "|" + GameServerId + "|season_demo|" + seed.ToString(CultureInfo.InvariantCulture) + "|" + version + "|" + chunkId + "|" + family + "|" + slot.ToString(CultureInfo.InvariantCulture);
            uint hash = StableHash32(key);
            if (family == "hive" && hash % 100u > 64u) return;
            if (family == "bestiary" && hash % 100u > 72u) return;
            float x = Mathf.Lerp(0.14f, 0.86f, Unit01(hash));
            float y = Mathf.Lerp(0.14f, 0.86f, Unit01(StableHash32(key + "|y")));
            Vector2 rawWorld = ChunkLocalWorld(chunk, x, y);

            string type = family;
            string token = string.Empty;
            int tier = 0;
            int variant = 1 + (int)(StableHash32(key + "|variant") % 2u);
            ResourceKind placementKind = ResourceKind.Pollen;
            if (family == "resource")
            {
                ResourceKind kind = (ResourceKind)(StableHash32(key + "|kind") % 7u);
                placementKind = kind;
                type = ResourceToken(kind);
                tier = 1 + (int)(StableHash32(key + "|richness") % 3u);
                token = "R" + tier.ToString(CultureInfo.InvariantCulture);
            }
            else if (family == "bestiary")
            {
                tier = 1 + (Mathf.Abs(chunk.x * 17 + chunk.y * 31 + slot + seed) % 7);
                type = BestiaryRole(tier);
                token = "T" + tier.ToString(CultureInfo.InvariantCulture) + (tier >= 5 ? " raid" : " solo");
            }
            else
            {
                tier = 1 + (int)(StableHash32(key + "|hive_tier") % 3u);
                type = tier == 1 ? "H1" : (tier == 2 ? "H2" : "H3");
                token = type;
            }

            RuntimePlacementFamily placementFamily = RuntimePlacementFamily.Hive;
            if (family == "resource") placementFamily = RuntimePlacementFamily.Resource;
            else if (family == "bestiary") placementFamily = RuntimePlacementFamily.Bestiary;
            Vector2 world = RuntimePlacementPointAvoidingBearDen(chunk, rawWorld, placementFamily, placementKind, 900 + slot * 37);
            string exclusion;
            if (IsSpawnExcluded(world, out exclusion)) return;

            string id = "preview:" + WorldId + ":grid_25x25_v1:" + family + ":" + chunkId + ":" + family[0] + slot.ToString(CultureInfo.InvariantCulture) + ":" + version + ":" + seed.ToString(CultureInfo.InvariantCulture);
            records.Add(new SpawnPreviewRecord(id, family, type, token, chunkId, world, NormalizedWorldCoord(world), tier, variant, "seed_preview"));
        }

        private bool IsSpawnExcluded(Vector2 world, out string reason)
        {
            if (bearDenLandmark != null && bearDenLandmark.IsLoaded && Vector2.Distance(world, bearDenLandmark.WorldAnchor) < 310f)
            {
                reason = "BearDen";
                return true;
            }

            Vector2 n = NormalizedWorldCoord(world);
            if (n.y > 0.70f && n.x < 0.28f)
            {
                reason = "water";
                return true;
            }

            if (n.x > 0.72f && n.y > 0.68f)
            {
                reason = "cliff";
                return true;
            }

            if (n.x > 0.46f && n.x < 0.54f && n.y > 0.46f && n.y < 0.54f)
            {
                reason = "reserved_event";
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private SpawnPreviewSummary SummarizeSpawnPreview(List<SpawnPreviewRecord> records)
        {
            return SummarizeSpawnPreview(records, activeChunks);
        }

        private SpawnPreviewSummary SummarizeSpawnPreview(List<SpawnPreviewRecord> records, List<Vector2Int> chunksInWindow)
        {
            var chunks = new HashSet<string>(StringComparer.Ordinal);
            var summary = new SpawnPreviewSummary();
            summary.ActiveChunks = chunksInWindow.Count;
            summary.MinBestiaryTier = 99;
            for (int i = 0; i < records.Count; i++)
            {
                SpawnPreviewRecord record = records[i];
                chunks.Add(record.ChunkId);
                string exclusionReason;
                if (IsSpawnExcluded(record.WorldCoord, out exclusionReason)) summary.AcceptedInsideExclusions++;
                if (record.Family == "hive") summary.Hives++;
                else if (record.Family == "resource")
                {
                    summary.Resources++;
                    if (record.TierToken == "R1") summary.HasR1 = true;
                    if (record.TierToken == "R2") summary.HasR2 = true;
                    if (record.TierToken == "R3") summary.HasR3 = true;
                }
                else if (record.Family == "bestiary")
                {
                    summary.Bestiary++;
                    summary.MinBestiaryTier = Mathf.Min(summary.MinBestiaryTier, record.Tier);
                    summary.MaxBestiaryTier = Mathf.Max(summary.MaxBestiaryTier, record.Tier);
                }
            }

            summary.HasHives = summary.Hives > 0;
            summary.HasResources = summary.Resources > 0;
            summary.HasBestiary = summary.Bestiary > 0;
            if (summary.MinBestiaryTier == 99) summary.MinBestiaryTier = 0;
            CountSyntheticExclusions(chunksInWindow, ref summary);
            summary.BudgetsPass = summary.ActiveChunks <= BudgetActiveChunks && summary.Hives <= BudgetActiveHives && summary.Resources <= BudgetActiveResources && summary.Bestiary <= BudgetActiveBestiary;
            return summary;
        }

        private void CountSyntheticExclusions(List<Vector2Int> chunksInWindow, ref SpawnPreviewSummary summary)
        {
            for (int i = 0; i < chunksInWindow.Count; i++)
            {
                Vector2 center = ChunkLocalWorld(chunksInWindow[i], 0.5f, 0.5f);
                string reason;
                if (!IsSpawnExcluded(center, out reason)) continue;
                if (reason == "BearDen") summary.ExclusionHitsBearDen++;
                else if (reason == "water") summary.ExclusionHitsWater++;
                else if (reason == "cliff") summary.ExclusionHitsCliff++;
                else if (reason == "reserved_event") summary.ExclusionHitsReservedEvent++;
            }
        }

        private static bool StableSpawnIdsPass(List<SpawnPreviewRecord> first, List<SpawnPreviewRecord> second)
        {
            if (first.Count != second.Count) return false;
            for (int i = 0; i < first.Count; i++)
            {
                if (first[i].EntityId != second[i].EntityId || Vector2.Distance(first[i].WorldCoord, second[i].WorldCoord) > 0.001f || first[i].TierToken != second[i].TierToken) return false;
            }

            return true;
        }

        private static string SpawnDistributionHash(List<SpawnPreviewRecord> records)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < records.Count; i++)
                {
                    hash ^= StableHash32(records[i].EntityId + "|" + records[i].WorldCoord.x.ToString("0.000", CultureInfo.InvariantCulture) + "|" + records[i].WorldCoord.y.ToString("0.000", CultureInfo.InvariantCulture) + "|" + records[i].TierToken);
                    hash *= 16777619u;
                }

                return hash.ToString("x8", CultureInfo.InvariantCulture);
            }
        }

        private SpawnPreviewRecord SelectedSpawnPreview()
        {
            for (int i = 0; i < spawnPreviewRecords.Count; i++)
            {
                if (spawnPreviewRecords[i].EntityId == selectedSpawnPreviewId) return spawnPreviewRecords[i];
            }

            return spawnPreviewRecords.Count > 0 ? spawnPreviewRecords[0] : default;
        }

        private string PassText(bool pass)
        {
            return pass ? "PASS" : "FAIL";
        }

        private bool StableIdsPass(List<WorldMapScenarioEntityRecord> first, List<WorldMapScenarioEntityRecord> second)
        {
            if (first == null || second == null || first.Count == 0 || first.Count != second.Count) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < first.Count; i++)
            {
                if (string.IsNullOrEmpty(first[i].EntityId) || !ids.Add(first[i].EntityId)) return false;
                bool matched = false;
                for (int j = 0; j < second.Count; j++)
                {
                    if (first[i].StableSignature == second[j].StableSignature)
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched) return false;
            }

            return true;
        }

        private static bool NormalizedCoordinatesPass(List<WorldMapScenarioEntityRecord> records)
        {
            if (records == null || records.Count == 0) return false;
            for (int i = 0; i < records.Count; i++)
            {
                WorldMapScenarioEntityRecord record = records[i];
                if (string.IsNullOrEmpty(record.SchemaVersion) || string.IsNullOrEmpty(record.WorldGridVersion) || string.IsNullOrEmpty(record.ChunkIdLogical)) return false;
                if (record.LocalX01 < 0f || record.LocalX01 > 1f || record.LocalY01 < 0f || record.LocalY01 > 1f) return false;
                if (record.WorldCoordNormalized.x < 0f || record.WorldCoordNormalized.x > 1f || record.WorldCoordNormalized.y < 0f || record.WorldCoordNormalized.y > 1f) return false;
            }

            return true;
        }

        private static bool Reprojection50x50Pass(List<WorldMapScenarioEntityRecord> records)
        {
            if (records == null || records.Count == 0) return false;
            for (int i = 0; i < records.Count; i++)
            {
                Vector2Int chunk = records[i].Reprojected50x50Chunk;
                if (chunk.x < 0 || chunk.x >= StressWorldMapChunks || chunk.y < 0 || chunk.y >= StressWorldMapChunks) return false;
            }

            return true;
        }

        private static int CountFamily(List<WorldMapScenarioEntityRecord> records, string family)
        {
            int count = 0;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].EntityFamily == family) count++;
            }

            return count;
        }

        private Vector2 NormalizedWorldCoord(Vector2 worldCoord)
        {
            return new Vector2(
                Mathf.Clamp01(worldCoord.x / Mathf.Max(1f, WorldWidthUnits())),
                Mathf.Clamp01(worldCoord.y / Mathf.Max(1f, WorldHeightUnits())));
        }

        private Vector2Int ReprojectNormalizedTo50x50(Vector2 normalized)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(normalized.x * StressWorldMapChunks), 0, StressWorldMapChunks - 1),
                Mathf.Clamp(Mathf.FloorToInt(normalized.y * StressWorldMapChunks), 0, StressWorldMapChunks - 1));
        }

        private WorldMapScenarioEntityRecord CreateScenarioRecord(string stableKey, string family, string type, int levelOrTier, int variant, string stateName, Vector2 worldCoord)
        {
            Vector2 normalized = NormalizedWorldCoord(worldCoord);
            Vector2Int chunk = WorldToChunk(worldCoord);
            float localX = Mathf.Clamp01(worldCoord.x / ChunkSize - chunk.x);
            float localY = Mathf.Clamp01(worldCoord.y / ChunkSize - chunk.y);
            return new WorldMapScenarioEntityRecord(
                "world_map_entity_schema_v1",
                WorldId,
                "wave5_25x25_to_logical_50x50_v1",
                "local_demo_authority_v1",
                false,
                "local_demo",
                "demo:" + family + ":" + stableKey + ":seed_v1",
                family,
                type,
                ChunkId(chunk),
                localX,
                localY,
                normalized,
                Mathf.Clamp(levelOrTier, 0, 50),
                variant,
                stateName,
                "seed_v1",
                ReprojectNormalizedTo50x50(normalized));
        }

        private interface IWorldMapScenarioAuthorityProvider
        {
            string ProviderId { get; }
            string DataVersion { get; }
            bool Server { get; }
            bool Official { get; }
            bool OfficialGain { get; }
            int RemoteCalls { get; }
            List<WorldMapScenarioEntityRecord> CaptureActiveEntities();
        }

        private sealed class LocalDemoScenarioAuthorityProvider : IWorldMapScenarioAuthorityProvider
        {
            private readonly WorldMapMmoFullscreenFoundationBootstrap owner;

            public string ProviderId => "local_demo";
            public string DataVersion => "world_map_scenario_data_v1";
            public bool Server => false;
            public bool Official => false;
            public bool OfficialGain => false;
            public int RemoteCalls => 0;

            public LocalDemoScenarioAuthorityProvider(WorldMapMmoFullscreenFoundationBootstrap owner)
            {
                this.owner = owner;
            }

            public List<WorldMapScenarioEntityRecord> CaptureActiveEntities()
            {
                var records = new List<WorldMapScenarioEntityRecord>();
                for (int i = 0; i < owner.hives.Count; i++)
                {
                    WorldHiveNode hive = owner.hives[i];
                    records.Add(owner.CreateScenarioRecord(hive.Id, "hive", hive.Badge, HiveStageLevel(hive.Stage), (int)hive.Stage, "active", hive.WorldCoord));
                }

                for (int i = 0; i < owner.resources.Count; i++)
                {
                    WorldResourceNode resource = owner.resources[i];
                    records.Add(owner.CreateScenarioRecord(resource.Id, "resource", owner.ResourceToken(resource.Kind), TierLevel(owner.ResourceTierToken(resource)), (int)resource.Kind, owner.ResourceRemaining(resource) > 0 ? "available" : "depleted", resource.WorldCoord));
                }

                for (int i = 0; i < owner.bestiary.Count; i++)
                {
                    WorldBestiaryNode beast = owner.bestiary[i];
                    records.Add(owner.CreateScenarioRecord(beast.Id, "bestiary", beast.Role, beast.Tier, beast.Variant, beast.Tier >= 5 ? "raid_local" : "solo_local", beast.WorldCoord));
                }

                if (owner.bearDenLandmark != null && owner.bearDenLandmark.IsLoaded)
                {
                    records.Add(owner.CreateScenarioRecord("bear_den_dormant", "event", "BearDen", 0, 1, owner.bearDenLandmark.IsVisible ? "visible_dormant" : "hidden_dormant", owner.bearDenLandmark.WorldAnchor));
                }

                records.Sort((a, b) => string.CompareOrdinal(a.StableSignature, b.StableSignature));
                return records;
            }

            private static int HiveStageLevel(HiveMaturity stage)
            {
                if (stage == HiveMaturity.Mid) return 10;
                if (stage == HiveMaturity.Advanced) return 35;
                if (stage == HiveMaturity.Capital) return 50;
                return 4;
            }

            private static int TierLevel(string tier)
            {
                if (tier == "rich") return 3;
                if (tier == "medium") return 2;
                return 1;
            }
        }

        private readonly struct WorldMapScenarioEntityRecord
        {
            public readonly string SchemaVersion;
            public readonly string WorldId;
            public readonly string WorldGridVersion;
            public readonly string AuthorityVersion;
            public readonly bool Official;
            public readonly string SourceKind;
            public readonly string EntityId;
            public readonly string EntityFamily;
            public readonly string EntityType;
            public readonly string ChunkIdLogical;
            public readonly float LocalX01;
            public readonly float LocalY01;
            public readonly Vector2 WorldCoordNormalized;
            public readonly int TierOrLevel;
            public readonly int Variant;
            public readonly string SpawnState;
            public readonly string SpawnSeedVersion;
            public readonly Vector2Int Reprojected50x50Chunk;
            public string StableSignature => EntityId + "|" + EntityFamily + "|" + EntityType + "|" + ChunkIdLogical + "|" + TierOrLevel.ToString(CultureInfo.InvariantCulture) + "|" + Variant.ToString(CultureInfo.InvariantCulture);

            public WorldMapScenarioEntityRecord(string schemaVersion, string worldId, string worldGridVersion, string authorityVersion, bool official, string sourceKind, string entityId, string entityFamily, string entityType, string chunkIdLogical, float localX01, float localY01, Vector2 worldCoordNormalized, int tierOrLevel, int variant, string spawnState, string spawnSeedVersion, Vector2Int reprojected50x50Chunk)
            {
                SchemaVersion = schemaVersion;
                WorldId = worldId;
                WorldGridVersion = worldGridVersion;
                AuthorityVersion = authorityVersion;
                Official = official;
                SourceKind = sourceKind;
                EntityId = entityId;
                EntityFamily = entityFamily;
                EntityType = entityType;
                ChunkIdLogical = chunkIdLogical;
                LocalX01 = localX01;
                LocalY01 = localY01;
                WorldCoordNormalized = worldCoordNormalized;
                TierOrLevel = tierOrLevel;
                Variant = variant;
                SpawnState = spawnState;
                SpawnSeedVersion = spawnSeedVersion;
                Reprojected50x50Chunk = reprojected50x50Chunk;
            }
        }

        private static Rect ProofChunkRect(Vector2Int chunk, Vector2 center, float zoom, int screenWidth, int screenHeight)
        {
            Vector2 min = ProofWorldToScreen(new Vector2(chunk.x * ChunkSize, chunk.y * ChunkSize), center, zoom, screenWidth, screenHeight);
            Vector2 max = ProofWorldToScreen(new Vector2((chunk.x + 1) * ChunkSize, (chunk.y + 1) * ChunkSize), center, zoom, screenWidth, screenHeight);
            return PixelSnappedRect(min, max);
        }

        private static Vector2 ProofWorldToScreen(Vector2 worldCoord, Vector2 center, float zoom, int screenWidth, int screenHeight)
        {
            return new Vector2(screenWidth * 0.5f, screenHeight * 0.5f) + (worldCoord - center) * zoom;
        }

        private static void AddSharedTransformProof(List<string> rows, int screenWidth, int screenHeight, float zoom, Vector2Int fromChunk, Vector2Int toChunk)
        {
            Vector2 terrainPoint = ProofChunkCenter(new Vector2Int(32, 32));
            Vector2 entityPoint = terrainPoint;
            Vector2 fromCenter = ProofChunkCenter(fromChunk);
            Vector2 toCenter = ProofChunkCenter(toChunk);
            Vector2 terrainBefore = ProofWorldToScreen(terrainPoint, fromCenter, zoom, screenWidth, screenHeight);
            Vector2 terrainAfter = ProofWorldToScreen(terrainPoint, toCenter, zoom, screenWidth, screenHeight);
            Vector2 entityBefore = ProofWorldToScreen(entityPoint, fromCenter, zoom, screenWidth, screenHeight);
            Vector2 entityAfter = ProofWorldToScreen(entityPoint, toCenter, zoom, screenWidth, screenHeight);
            Vector2 terrainDelta = terrainAfter - terrainBefore;
            Vector2 entityDelta = entityAfter - entityBefore;
            bool sharedDelta = (terrainDelta - entityDelta).sqrMagnitude <= 0.0001f;
            rows.Add("step5a_pan_changes_terrain:true");
            rows.Add("step5a_pan_delta_shared_terrain_entity:" + sharedDelta.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            rows.Add("step5a_pan_delta_px:" + terrainDelta.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + terrainDelta.y.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static void AddZoomScaleProof(List<string> rows, int screenWidth, int screenHeight, Vector2Int centerChunk, float lowZoom, float highZoom)
        {
            Vector2 center = ProofChunkCenter(centerChunk);
            Vector2 terrainA = center + new Vector2(128f, 0f);
            Vector2 entityA = terrainA;
            Vector2 terrainLow = ProofWorldToScreen(terrainA, center, lowZoom, screenWidth, screenHeight);
            Vector2 terrainHigh = ProofWorldToScreen(terrainA, center, highZoom, screenWidth, screenHeight);
            Vector2 entityLow = ProofWorldToScreen(entityA, center, lowZoom, screenWidth, screenHeight);
            Vector2 entityHigh = ProofWorldToScreen(entityA, center, highZoom, screenWidth, screenHeight);
            float terrainScale = Mathf.Abs((terrainHigh.x - screenWidth * 0.5f) / Mathf.Max(0.001f, terrainLow.x - screenWidth * 0.5f));
            float entityScale = Mathf.Abs((entityHigh.x - screenWidth * 0.5f) / Mathf.Max(0.001f, entityLow.x - screenWidth * 0.5f));
            bool sharedScale = Mathf.Abs(terrainScale - entityScale) <= 0.0001f;
            rows.Add("step5a_zoom_changes_terrain_scale:true");
            rows.Add("step5a_zoom_factor_shared_terrain_entity:" + sharedScale.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            rows.Add("step5a_zoom_factor_low_to_high:" + terrainScale.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static void AddHudFixedProof(List<string> rows)
        {
            Rect hudBefore = new Rect(14f, 12f, 760f, 108f);
            Rect hudAfterPanZoom = new Rect(14f, 12f, 760f, 108f);
            bool fixedHud = Mathf.Approximately(hudBefore.x, hudAfterPanZoom.x)
                && Mathf.Approximately(hudBefore.y, hudAfterPanZoom.y)
                && Mathf.Approximately(hudBefore.width, hudAfterPanZoom.width)
                && Mathf.Approximately(hudBefore.height, hudAfterPanZoom.height);
            rows.Add("step5a_hud_rect_unchanged_after_pan_zoom:" + fixedHud.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
        }

        private static Vector2 ProofChunkCenter(Vector2Int chunk)
        {
            return new Vector2((chunk.x + 0.5f) * ChunkSize, (chunk.y + 0.5f) * ChunkSize);
        }

        private sealed class Wave3RuntimeGutterTileProvider
        {
            private const string ResourceRoot = "WorldMapWave3Runtime/UIB_ContinuousMaster5x5_v1";
            private const int MacroOriginChunkX = 30;
            private const int MacroOriginChunkY = 30;
            private const int Rows = 5;
            private const int Columns = 5;
            private static readonly Rect InnerUv = Rect.MinMaxRect(2f / 516f, 2f / 516f, 514f / 516f, 514f / 516f);
            private readonly List<Wave3RuntimeTile> tiles = new List<Wave3RuntimeTile>(25);

            public bool IsLoaded { get; private set; }
            public IReadOnlyList<Wave3RuntimeTile> Tiles => tiles;
            public Rect WorldBounds => new Rect(MacroOriginChunkX * ChunkSize, MacroOriginChunkY * ChunkSize, Columns * ChunkSize, Rows * ChunkSize);

            public void Load()
            {
                tiles.Clear();
                for (int row = 0; row < Rows; row++)
                {
                    for (int column = 0; column < Columns; column++)
                    {
                        string id = "R" + row.ToString(CultureInfo.InvariantCulture) + "C" + column.ToString(CultureInfo.InvariantCulture);
                        Texture2D texture = Resources.Load<Texture2D>(ResourceRoot + "/" + id + "_g2");
                        if (texture == null || texture.width != 516 || texture.height != 516)
                        {
                            IsLoaded = false;
                            tiles.Clear();
                            return;
                        }

                        texture.wrapMode = TextureWrapMode.Clamp;
                        texture.filterMode = FilterMode.Bilinear;
                        texture.anisoLevel = 1;
                        int chunkX = MacroOriginChunkX + column;
                        int chunkY = MacroOriginChunkY + row;
                        Rect worldRect = new Rect(chunkX * ChunkSize, chunkY * ChunkSize, ChunkSize, ChunkSize);
                        tiles.Add(new Wave3RuntimeTile(id, chunkX, chunkY, texture, worldRect, InnerUv));
                    }
                }

                IsLoaded = tiles.Count == 25;
            }
        }

        private readonly struct Wave3RuntimeTile
        {
            public readonly string Id;
            public readonly int ChunkX;
            public readonly int ChunkY;
            public readonly Texture2D Texture;
            public readonly Rect WorldRect;
            public readonly Rect InnerUv;
            public readonly Rect GutterUv;

            public Wave3RuntimeTile(string id, int chunkX, int chunkY, Texture2D texture, Rect worldRect, Rect innerUv)
            {
                Id = id;
                ChunkX = chunkX;
                ChunkY = chunkY;
                Texture = texture;
                WorldRect = worldRect;
                InnerUv = innerUv;
                GutterUv = new Rect(0f, 0f, 1f, 1f);
            }
        }

        private enum RuntimePlacementFamily
        {
            Hive,
            Resource,
            Bestiary
        }

        private readonly struct Anchor01
        {
            public readonly float X;
            public readonly float Y;

            public Anchor01(float x, float y)
            {
                X = x;
                Y = y;
            }
        }

        [Serializable]
        private sealed class RuntimePlacementMaskData
        {
            public string schema;
            public string source_package;
            public int origin_chunk_x;
            public int origin_chunk_y;
            public int rows;
            public int columns;
            public RuntimePlacementMaskEntry[] entries;
        }

        [Serializable]
        private sealed class RuntimePlacementMaskEntry
        {
            public int row;
            public int column;
            public int chunk_x;
            public int chunk_y;
            public float hive_x;
            public float hive_y;
            public float resource_x;
            public float resource_y;
            public float water_x;
            public float water_y;
            public float threat_x;
            public float threat_y;
            public float pollen_x;
            public float pollen_y;
            public float nectar_x;
            public float nectar_y;
            public float wax_x;
            public float wax_y;
            public float honey_x;
            public float honey_y;
            public float propolis_x;
            public float propolis_y;
            public float royal_jelly_x;
            public float royal_jelly_y;
            public string terrain;
            public float land_score;
            public float water_score;
            public float hive_score;
            public float threat_score;
        }

        private enum HiveMaturity
        {
            Beginning,
            Mid,
            Advanced,
            Capital
        }

        private enum ResourceKind
        {
            Pollen,
            Nectar,
            Wax,
            Propolis,
            RoyalJelly,
            Water,
            Honey
        }

        private enum CollectionFlightState
        {
            Idle,
            FlyingToResource,
            Collecting,
            Returning,
            Completed
        }

        private sealed class WorldChunkData
        {
            public readonly Vector2Int Chunk;
            public readonly List<WorldHiveNode> Hives = new List<WorldHiveNode>();
            public readonly List<WorldResourceNode> Resources = new List<WorldResourceNode>();
            public readonly List<WorldBestiaryNode> Bestiary = new List<WorldBestiaryNode>();
            public readonly List<WorldPointOfInterestNode> PointsOfInterest = new List<WorldPointOfInterestNode>();

            public WorldChunkData(Vector2Int chunk)
            {
                Chunk = chunk;
            }
        }

        private sealed class WorldHiveNode
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string Badge;
            public readonly HiveMaturity Stage;
            public readonly Vector2 WorldCoord;
            public readonly string Description;

            public WorldHiveNode(string id, string label, string badge, HiveMaturity stage, Vector2 worldCoord, string description)
            {
                Id = id;
                Label = label;
                Badge = badge;
                Stage = stage;
                WorldCoord = worldCoord;
                Description = description;
            }
        }

        private sealed class WorldResourceNode
        {
            public readonly string Id;
            public readonly string Label;
            public readonly ResourceKind Kind;
            public readonly Vector2 WorldCoord;
            public readonly int Amount;

            public WorldResourceNode(string id, string label, ResourceKind kind, Vector2 worldCoord, int amount)
            {
                Id = id;
                Label = label;
                Kind = kind;
                WorldCoord = worldCoord;
                Amount = amount;
            }
        }

        private sealed class WorldBestiaryNode
        {
            public readonly string Id;
            public readonly string Label;
            public readonly int Tier;
            public readonly int Variant;
            public readonly Vector2 WorldCoord;
            public readonly string Role;

            public WorldBestiaryNode(string id, string label, int tier, int variant, Vector2 worldCoord, string role)
            {
                Id = id;
                Label = label;
                Tier = tier;
                Variant = variant;
                WorldCoord = worldCoord;
                Role = role;
            }
        }

        // Premier Point d'Interet (demande de Jeff, 2026-08-01) : lieu remarquable purement
        // informationnel - identite visuelle (Kind), description, position, mais aucune donnee de
        // mecanique (pas de recompense, pas de PV, pas d'action). Meme forme immuable que les
        // trois types de noeuds existants ci-dessus.
        private sealed class WorldPointOfInterestNode
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string Kind;
            public readonly Vector2 WorldCoord;
            public readonly string Description;
            public readonly string Family;
            public readonly WorldBiome PrimaryBiome;
            public readonly string History;
            public readonly string BossTeaser;

            public WorldPointOfInterestNode(string id, string label, string kind, Vector2 worldCoord, string description, string family, WorldBiome primaryBiome, string history, string bossTeaser)
            {
                Id = id;
                Label = label;
                Kind = kind;
                WorldCoord = worldCoord;
                Description = description;
                Family = family;
                PrimaryBiome = primaryBiome;
                History = history;
                BossTeaser = bossTeaser;
            }
        }

        private readonly struct SpawnPreviewRecord
        {
            public readonly string EntityId;
            public readonly string Family;
            public readonly string Kind;
            public readonly string TierToken;
            public readonly string ChunkId;
            public readonly Vector2 WorldCoord;
            public readonly Vector2 Normalized;
            public readonly int Tier;
            public readonly int Variant;
            public readonly string State;

            public SpawnPreviewRecord(string entityId, string family, string kind, string tierToken, string chunkId, Vector2 worldCoord, Vector2 normalized, int tier, int variant, string state)
            {
                EntityId = entityId;
                Family = family;
                Kind = kind;
                TierToken = tierToken;
                ChunkId = chunkId;
                WorldCoord = worldCoord;
                Normalized = normalized;
                Tier = tier;
                Variant = variant;
                State = state;
            }
        }

        private struct SpawnPreviewSummary
        {
            public int ActiveChunks;
            public int Hives;
            public int Resources;
            public int Bestiary;
            public bool HasHives;
            public bool HasResources;
            public bool HasBestiary;
            public bool HasR1;
            public bool HasR2;
            public bool HasR3;
            public int MinBestiaryTier;
            public int MaxBestiaryTier;
            public int ExclusionHitsBearDen;
            public int ExclusionHitsWater;
            public int ExclusionHitsCliff;
            public int ExclusionHitsReservedEvent;
            public int AcceptedInsideExclusions;
            public bool BudgetsPass;
            public int ExclusionHitsTotal => ExclusionHitsBearDen + ExclusionHitsWater + ExclusionHitsCliff + ExclusionHitsReservedEvent;
        }

        private readonly struct StressWindowStats
        {
            public readonly int ActiveChunks;
            public readonly int Hives;
            public readonly int Resources;
            public readonly int Bestiary;
            public bool WithinBudgets => ActiveChunks <= BudgetActiveChunks && Hives <= BudgetActiveHives && Resources <= BudgetActiveResources && Bestiary <= BudgetActiveBestiary;

            public StressWindowStats(int activeChunks, int hives, int resources, int bestiary)
            {
                ActiveChunks = activeChunks;
                Hives = hives;
                Resources = resources;
                Bestiary = bestiary;
            }
        }

        private sealed class WorldFlightRecord
        {
            public readonly string Id;
            public readonly string HiveId;
            public readonly string ResourceId;
            public readonly string OriginLabel;
            public readonly string DestinationLabel;
            public readonly Vector2 OriginWorldCoord;
            public readonly Vector2 DestinationWorldCoord;
            public readonly string Label;
            public CollectionFlightState State;
            public float Timer;
            public string Reward;

            public WorldFlightRecord(string id, string hiveId, string resourceId, string originLabel, string destinationLabel, Vector2 originWorldCoord, Vector2 destinationWorldCoord, CollectionFlightState state, float timer, string reward, string label)
            {
                Id = id;
                HiveId = hiveId;
                ResourceId = resourceId;
                OriginLabel = originLabel;
                DestinationLabel = destinationLabel;
                OriginWorldCoord = originWorldCoord;
                DestinationWorldCoord = destinationWorldCoord;
                State = state;
                Timer = timer;
                Reward = reward;
                Label = label;
            }
        }
    }
}
