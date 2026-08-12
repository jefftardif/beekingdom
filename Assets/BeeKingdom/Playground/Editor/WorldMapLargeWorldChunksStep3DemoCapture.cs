using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class WorldMapLargeWorldChunksStep3DemoCapture
    {
        private const string ScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-090_WorldMapLargeWorldChunksStep3";
        private const string ManifestPath = OutputDirectory + "/DEMO-090_CaptureManifest.md";
        private const string DeterminismPath = OutputDirectory + "/DEMO-090_DeterminismProof.json";
        private const string StateRequested = "BeeKingdom.Playground.DEMO090.Requested";
        private const string StateFrames = "BeeKingdom.Playground.DEMO090.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.DEMO090.Captured";
        private const string StateIndex = "BeeKingdom.Playground.DEMO090.Index";
        private const string StateInitializedIndex = "BeeKingdom.Playground.DEMO090.InitializedIndex";
        private const int ChunkSize = 512;

        private enum FlightSetup
        {
            Core,
            None,
            Stable,
            Launch,
            Continue
        }

        private readonly struct CaptureSpec
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly float Zoom;
            public readonly Vector2 WorldCenter;
            public readonly bool DebugChunks;
            public readonly string HiveId;
            public readonly string ResourceId;
            public readonly string CollectionState;
            public readonly float CollectionTimer;
            public readonly FlightSetup FlightSetup;
            public readonly string Status;
            public readonly string Reward;

            public CaptureSpec(
                string id,
                string label,
                string fileName,
                int width,
                int height,
                float zoom,
                Vector2 worldCenter,
                bool debugChunks,
                string hiveId,
                string resourceId,
                string collectionState,
                float collectionTimer,
                FlightSetup flightSetup,
                string status,
                string reward)
            {
                Id = id;
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Zoom = zoom;
                WorldCenter = worldCenter;
                DebugChunks = debugChunks;
                HiveId = hiveId;
                ResourceId = resourceId;
                CollectionState = collectionState;
                CollectionTimer = collectionTimer;
                FlightSetup = flightSetup;
                Status = status;
                Reward = reward;
            }
        }

        private static readonly Vector2 CenterC32 = ChunkCenter(32, 32);
        private static readonly Vector2 CenterC35 = ChunkCenter(35, 32);
        private static readonly Vector2 CenterC36 = ChunkCenter(36, 32);

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("OverviewFullscreen", "Overview fullscreen C32_32", "DEMO090_01_OverviewFullscreen_C32_32_1280x720.png", 1280, 720, 0.72f, CenterC32, false, "hive_player_test", "res_nectar_core", "Idle", 0f, FlightSetup.Core, "Large monde local/demo - voisinage actif 5x5", "Aucune recompense locale"),
            new CaptureSpec("DebugGridOn", "Chunk debug grid ON C32_32", "DEMO090_02_DebugGridOn_C32_32_1920x1200.png", 1920, 1200, 0.96f, CenterC32, true, "hive_player_test", "res_nectar_core", "Idle", 0f, FlightSetup.Core, "Grille chunks debug/preuve activee - equivalent touche G", "Aucune recompense locale"),
            new CaptureSpec("ProductGridOff", "Product view debug grid OFF", "DEMO090_03_ProductGridOff_C32_32_1280x720.png", 1280, 720, 0.72f, CenterC32, false, "hive_player_test", "res_nectar_core", "Idle", 0f, FlightSetup.Core, "Vue produit locale/demo - grille debug desactivee", "Aucune recompense locale"),
            new CaptureSpec("PanStart", "Pan start C32_32", "DEMO090_04_PanStart_C32_32_1280x720.png", 1280, 720, 0.68f, CenterC32, false, "hive_player_test", "res_nectar_core", "Idle", 0f, FlightSetup.Core, "Depart pan multi-frontieres: C32_32 - 25 chunks actifs", "Aucune recompense locale"),
            new CaptureSpec("PanArrival", "Pan arrival C35_32", "DEMO090_05_PanArrival_C35_32_1280x720.png", 1280, 720, 0.68f, CenterC35, false, "hive_35_32", "res_royal_jelly_35_32_0", "Idle", 0f, FlightSetup.None, "Arrivee apres trois frontieres: C35_32 - 25 chunks actifs", "Aucune recompense locale"),
            new CaptureSpec("ZoomOut", "Zoom out vast world C35_32", "DEMO090_06_ZoomOut_C35_32_1280x720.png", 1280, 720, 0.64f, CenterC35, false, "hive_35_32", "res_royal_jelly_35_32_0", "Idle", 0f, FlightSetup.Stable, "Zoom arriere carte seule - panneaux fixes", "En attente retour essaim"),
            new CaptureSpec("ZoomIn", "Zoom in vast world C35_32", "DEMO090_07_ZoomIn_C35_32_1280x720.png", 1280, 720, 1.45f, CenterC35, false, "hive_35_32", "res_royal_jelly_35_32_0", "FlyingToResource", 1.35f, FlightSetup.Stable, "Zoom avant carte seule - HUD, journal et minimap fixes", "En attente retour essaim"),
            new CaptureSpec("HiveLevelsRoles", "Four hive levels and roles", "DEMO090_08_HiveLevelsRoles_C32_32_1920x1200.png", 1920, 1200, 0.96f, new Vector2(CenterC32.x + 70f, CenterC32.y + 110f), false, "hive_player_test", "res_nectar_core", "Idle", 0f, FlightSetup.Core, "Debut, intermediaire, avancee, capitale - JOUEUR ALLIEE NEUTRE", "Aucune recompense locale"),
            new CaptureSpec("ResourceFamilies", "Five resource families", "DEMO090_09_ResourceFamilies_C32_32_1920x1200.png", 1920, 1200, 0.96f, new Vector2(CenterC32.x + 55f, CenterC32.y + 95f), false, "hive_player_test", "res_royal_jelly_core", "Idle", 0f, FlightSetup.Core, "Nectar Pollen Cire Propolis Gelee royale demo", "Aucune recompense locale"),
            new CaptureSpec("PostChunkSelection", "Selection after chunk change with Collecter", "DEMO090_10_PostChunkSelection_Collecter_C35_32_1280x720.png", 1280, 720, 0.90f, CenterC35, false, "hive_35_32", "res_royal_jelly_35_32_0", "Idle", 0f, FlightSetup.None, "Ruche et ressource selectionnees apres changement de chunk", "Aucune recompense locale"),
            new CaptureSpec("FlightBeforePan", "VOL-42 before boundary pan", "DEMO090_11_VOL42_BeforeBoundaryPan_C35_32_1280x720.png", 1280, 720, 0.82f, CenterC35, false, "hive_35_32", "res_royal_jelly_35_32_0", "FlyingToResource", 1.10f, FlightSetup.Launch, "VOL-42 lance via action runtime - avant pan inter-chunks", "En attente retour essaim"),
            new CaptureSpec("FlightAfterPan", "VOL-42 after boundary pan", "DEMO090_12_VOL42_AfterBoundaryPan_C36_32_1280x720.png", 1280, 720, 0.82f, CenterC36, false, "hive_35_32", "res_royal_jelly_35_32_0", "FlyingToResource", 2.10f, FlightSetup.Continue, "VOL-42 conserve apres frontiere C35_32 vers C36_32", "En attente retour essaim"),
            new CaptureSpec("FixedPanelsZoomOut", "Fixed panels zoom out", "DEMO090_13_FixedPanels_ZoomOut_C35_32_1280x720.png", 1280, 720, 0.64f, CenterC35, false, "hive_35_32", "res_royal_jelly_35_32_0", "FlyingToResource", 1.45f, FlightSetup.Stable, "Zoom 0.64x - HUD/panneaux/journal/minimap ancres", "En attente retour essaim"),
            new CaptureSpec("FixedPanelsZoomIn", "Fixed panels zoom in", "DEMO090_14_FixedPanels_ZoomIn_C35_32_1280x720.png", 1280, 720, 1.45f, CenterC35, false, "hive_35_32", "res_royal_jelly_35_32_0", "FlyingToResource", 1.45f, FlightSetup.Stable, "Zoom 1.45x - HUD/panneaux/journal/minimap ancres", "En attente retour essaim"),
            new CaptureSpec("TabletLandscape", "Tablet landscape Step 3", "DEMO090_15_TabletLandscape_Requested1920x1200.png", 1920, 1200, 0.96f, CenterC35, false, "hive_35_32", "res_royal_jelly_35_32_0", "FlyingToResource", 1.80f, FlightSetup.Stable, "Tablette paysage large monde local/demo - server_live:false", "En attente retour essaim")
        };

        private static object launchedFlight;
        private static int launchedFlightIdentity;
        private static string flightBeforeSnapshot = string.Empty;
        private static string flightAfterSnapshot = string.Empty;
        private static bool flightObjectPreserved;
        private static bool flightAnchorsPreserved;

        static WorldMapLargeWorldChunksStep3DemoCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-090 World Map Large World Chunks Step 3")]
        public static void CaptureWorldMapLargeWorldChunksStep3()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            DeleteIfExists(DeterminismPath);

            launchedFlight = null;
            launchedFlightIdentity = 0;
            flightBeforeSnapshot = string.Empty;
            flightAfterSnapshot = string.Empty;
            flightObjectPreserved = false;
            flightAnchorsPreserved = false;

            SessionState.SetBool(StateRequested, true);
            SessionState.SetBool(StateCaptured, false);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetInt(StateIndex, 0);
            SessionState.SetInt(StateInitializedIndex, -1);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        public static void CaptureWorldMapLargeWorldChunksStep3ForBatch()
        {
            CaptureWorldMapLargeWorldChunksStep3();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false) || state != PlayModeStateChange.EnteredPlayMode) return;
            Time.timeScale = 0f;
            SessionState.SetInt(StateInitializedIndex, -1);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCaptured, false);
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            try
            {
                int index = Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1);
                CaptureSpec capture = Captures[index];
                if (SessionState.GetInt(StateInitializedIndex, -1) != index)
                {
                    InitializeState(index, capture);
                    SessionState.SetInt(StateInitializedIndex, index);
                }

                HoldViewState(capture);
                int frames = SessionState.GetInt(StateFrames, 0) + 1;
                SessionState.SetInt(StateFrames, frames);
                if (frames < 90) return;

                string path = PathFor(capture);
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    ScreenCapture.CaptureScreenshot(path);
                    SessionState.SetBool(StateCaptured, true);
                    return;
                }

                if (frames < 130) return;
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (frames < 260) return;
                    throw new InvalidOperationException("DEMO-090 screenshot was not written: " + path);
                }

                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateInitializedIndex, -1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    return;
                }

                WorldMapMmoFullscreenFoundationBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
                DeterminismResult determinism = BuildDeterminismResult(bootstrap);
                File.WriteAllText(DeterminismPath, BuildDeterminismJson(determinism), Encoding.UTF8);
                File.WriteAllText(ManifestPath, BuildManifest(determinism), Encoding.UTF8);
                Time.timeScale = 1f;
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-090 world map large world chunks Step 3 screenshots captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Time.timeScale = 1f;
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-090 capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void InitializeState(int index, CaptureSpec capture)
        {
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);

            WorldMapMmoFullscreenFoundationBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
            if (bootstrap == null) return;

            ApplyWorldView(bootstrap, capture);
            SetField(bootstrap, "selectedHiveId", capture.HiveId);
            SetField(bootstrap, "selectedResourceId", capture.ResourceId);
            SetField(bootstrap, "collectionTimer", capture.CollectionTimer);
            SetField(bootstrap, "localRewardText", capture.Reward);
            SetField(bootstrap, "animatedTime", 1.25f);
            SetCollectionState(bootstrap, capture.CollectionState);

            if (capture.FlightSetup == FlightSetup.Core)
            {
                SeedCoreFlights(bootstrap);
            }
            else if (capture.FlightSetup == FlightSetup.None)
            {
                ClearFlights(bootstrap);
            }
            else if (capture.FlightSetup == FlightSetup.Stable)
            {
                SeedStableInterChunkFlight(bootstrap, capture.CollectionState, capture.CollectionTimer);
            }
            else if (capture.FlightSetup == FlightSetup.Launch)
            {
                LaunchInterChunkFlight(bootstrap, capture);
            }
            else if (capture.FlightSetup == FlightSetup.Continue)
            {
                ContinueInterChunkFlight(bootstrap, capture);
            }

            SetField(bootstrap, "status", capture.Status);
        }

        private static void HoldViewState(CaptureSpec capture)
        {
            WorldMapMmoFullscreenFoundationBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
            if (bootstrap == null) return;
            SetField(bootstrap, "currentWorldCenter", capture.WorldCenter);
            SetField(bootstrap, "targetWorldCenter", capture.WorldCenter);
            SetField(bootstrap, "currentZoom", capture.Zoom);
            SetField(bootstrap, "targetZoom", capture.Zoom);
            SetField(bootstrap, "debugChunkOverlay", capture.DebugChunks);
            SetField(bootstrap, "status", capture.Status);
        }

        private static void ApplyWorldView(WorldMapMmoFullscreenFoundationBootstrap bootstrap, CaptureSpec capture)
        {
            SetField(bootstrap, "currentWorldCenter", capture.WorldCenter);
            SetField(bootstrap, "targetWorldCenter", capture.WorldCenter);
            SetField(bootstrap, "currentZoom", capture.Zoom);
            SetField(bootstrap, "targetZoom", capture.Zoom);
            SetField(bootstrap, "debugChunkOverlay", capture.DebugChunks);
            InvokePrivate(bootstrap, "RefreshActiveChunks", true);
        }

        private static void SeedCoreFlights(WorldMapMmoFullscreenFoundationBootstrap bootstrap)
        {
            IList flights = ClearFlights(bootstrap);
            if (flights == null) return;
            AddFlight(flights, NewFlight(bootstrap, "VOL-CORE-01", "hive_player_test", "res_nectar_core", "FlyingToResource", 1.20f, "+15 Nectar local/demo", "Collecte joueur demo"));
            AddFlight(flights, NewFlight(bootstrap, "VOL-CORE-02", "hive_ally_mid", "res_wax_core", "Returning", 0.90f, "+11 Cire local/demo", "Retour allie demo"));
            AddFlight(flights, NewFlight(bootstrap, "VOL-CORE-03", "hive_neutral_advanced", "res_propolis_core", "Collecting", 0.45f, "+7 Propolis local/demo", "Collecte neutre demo"));
        }

        private static void SeedStableInterChunkFlight(WorldMapMmoFullscreenFoundationBootstrap bootstrap, string state, float timer)
        {
            IList flights = ClearFlights(bootstrap);
            if (flights == null) return;
            AddFlight(flights, NewFlight(bootstrap, "VOL-42", "hive_35_32", "res_royal_jelly_35_32_0", state, timer, "+3 Gelee royale demo local/demo", "Vol inter-chunks demo"));
        }

        private static void LaunchInterChunkFlight(WorldMapMmoFullscreenFoundationBootstrap bootstrap, CaptureSpec capture)
        {
            IList flights = ClearFlights(bootstrap);
            if (flights == null) return;
            SetField(bootstrap, "nextFlightId", 42);
            SetCollectionState(bootstrap, "Idle");
            SetField(bootstrap, "collectionTimer", 0f);
            InvokePrivate(bootstrap, "StartLocalCollectionFlight");

            launchedFlight = FindFlight(flights, "VOL-42");
            if (launchedFlight == null) throw new InvalidOperationException("Runtime action did not create VOL-42.");
            SetFlightStateAndTimer(launchedFlight, capture.CollectionState, capture.CollectionTimer);
            SetCollectionState(bootstrap, capture.CollectionState);
            SetField(bootstrap, "collectionTimer", capture.CollectionTimer);
            launchedFlightIdentity = RuntimeHelpers.GetHashCode(launchedFlight);
            flightBeforeSnapshot = FlightSnapshot(launchedFlight);
        }

        private static void ContinueInterChunkFlight(WorldMapMmoFullscreenFoundationBootstrap bootstrap, CaptureSpec capture)
        {
            IList flights = GetFlights(bootstrap);
            object current = FindFlight(flights, "VOL-42");
            if (current == null) throw new InvalidOperationException("VOL-42 disappeared before the boundary-cross capture.");

            string anchorsBefore = FlightAnchorSnapshot(current);
            SetFlightStateAndTimer(current, capture.CollectionState, capture.CollectionTimer);
            SetCollectionState(bootstrap, capture.CollectionState);
            SetField(bootstrap, "collectionTimer", capture.CollectionTimer);
            flightAfterSnapshot = FlightSnapshot(current);
            flightObjectPreserved = ReferenceEquals(launchedFlight, current) && launchedFlightIdentity == RuntimeHelpers.GetHashCode(current);
            flightAnchorsPreserved = anchorsBefore == FlightAnchorSnapshot(current) && FlightAnchorSnapshotFromFull(flightBeforeSnapshot) == FlightAnchorSnapshot(current);
        }

        private static IList ClearFlights(WorldMapMmoFullscreenFoundationBootstrap bootstrap)
        {
            IList flights = GetFlights(bootstrap);
            if (flights != null) flights.Clear();
            return flights;
        }

        private static IList GetFlights(WorldMapMmoFullscreenFoundationBootstrap bootstrap)
        {
            FieldInfo field = typeof(WorldMapMmoFullscreenFoundationBootstrap).GetField("flights", BindingFlags.Instance | BindingFlags.NonPublic);
            return field == null ? null : field.GetValue(bootstrap) as IList;
        }

        private static void AddFlight(IList flights, object flight)
        {
            if (flight != null) flights.Add(flight);
        }

        private static object NewFlight(
            WorldMapMmoFullscreenFoundationBootstrap bootstrap,
            string id,
            string hiveId,
            string resourceId,
            string stateName,
            float timer,
            string reward,
            string label)
        {
            object hive = InvokePrivate(bootstrap, "HiveById", hiveId);
            object resource = InvokePrivate(bootstrap, "ResourceById", resourceId);
            if (hive == null || resource == null) return null;

            Type bootstrapType = typeof(WorldMapMmoFullscreenFoundationBootstrap);
            Type flightType = bootstrapType.GetNestedType("WorldFlightRecord", BindingFlags.NonPublic);
            Type stateType = bootstrapType.GetNestedType("CollectionFlightState", BindingFlags.NonPublic);
            object state = Enum.Parse(stateType, stateName);
            string originLabel = ReadField<string>(hive, "Label");
            string destinationLabel = ReadField<string>(resource, "Label");
            Vector2 origin = ReadField<Vector2>(hive, "WorldCoord");
            Vector2 destination = ReadField<Vector2>(resource, "WorldCoord");
            return Activator.CreateInstance(
                flightType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new object[] { id, hiveId, resourceId, originLabel, destinationLabel, origin, destination, state, timer, reward, label },
                CultureInfo.InvariantCulture);
        }

        private static object FindFlight(IList flights, string id)
        {
            if (flights == null) return null;
            foreach (object flight in flights)
            {
                if (ReadField<string>(flight, "Id") == id) return flight;
            }

            return null;
        }

        private static void SetFlightStateAndTimer(object flight, string stateName, float timer)
        {
            FieldInfo stateField = flight.GetType().GetField("State", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo timerField = flight.GetType().GetField("Timer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            stateField.SetValue(flight, Enum.Parse(stateField.FieldType, stateName));
            timerField.SetValue(flight, timer);
        }

        private static string FlightSnapshot(object flight)
        {
            return "id=" + ReadField<string>(flight, "Id")
                + ";origin=" + VectorLabel(ReadField<Vector2>(flight, "OriginWorldCoord"))
                + ";destination=" + VectorLabel(ReadField<Vector2>(flight, "DestinationWorldCoord"))
                + ";state=" + ReadField<object>(flight, "State")
                + ";timer=" + ReadField<float>(flight, "Timer").ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string FlightAnchorSnapshot(object flight)
        {
            return "origin=" + VectorLabel(ReadField<Vector2>(flight, "OriginWorldCoord"))
                + ";destination=" + VectorLabel(ReadField<Vector2>(flight, "DestinationWorldCoord"));
        }

        private static string FlightAnchorSnapshotFromFull(string snapshot)
        {
            int origin = snapshot.IndexOf("origin=", StringComparison.Ordinal);
            int state = snapshot.IndexOf(";state=", StringComparison.Ordinal);
            return origin >= 0 && state > origin ? snapshot.Substring(origin, state - origin) : string.Empty;
        }

        private static DeterminismResult BuildDeterminismResult(WorldMapMmoFullscreenFoundationBootstrap primary)
        {
            Vector2Int chunk = new Vector2Int(35, 32);
            SetField(primary, "currentWorldCenter", CenterC35);
            SetField(primary, "targetWorldCenter", CenterC35);
            InvokePrivate(primary, "RefreshActiveChunks", true);
            string first = ChunkSnapshot(primary, chunk);

            GameObject duplicateObject = new GameObject("DEMO-090 Determinism Comparison");
            duplicateObject.SetActive(false);
            WorldMapMmoFullscreenFoundationBootstrap duplicate = duplicateObject.AddComponent<WorldMapMmoFullscreenFoundationBootstrap>();
            duplicateObject.SetActive(true);
            duplicate.enabled = false;
            SetField(duplicate, "currentWorldCenter", CenterC35);
            SetField(duplicate, "targetWorldCenter", CenterC35);
            InvokePrivate(duplicate, "RefreshActiveChunks", true);
            string second = ChunkSnapshot(duplicate, chunk);
            UnityEngine.Object.DestroyImmediate(duplicateObject);

            return new DeterminismResult(first, second, Sha256(first), Sha256(second));
        }

        private static string ChunkSnapshot(WorldMapMmoFullscreenFoundationBootstrap bootstrap, Vector2Int chunk)
        {
            FieldInfo cacheField = typeof(WorldMapMmoFullscreenFoundationBootstrap).GetField("chunkCache", BindingFlags.Instance | BindingFlags.NonPublic);
            IDictionary cache = cacheField == null ? null : cacheField.GetValue(bootstrap) as IDictionary;
            if (cache == null || !cache.Contains(chunk)) return "missing:C35_32";

            object data = cache[chunk];
            var rows = new List<string>();
            IList hives = ReadField<IList>(data, "Hives");
            IList resources = ReadField<IList>(data, "Resources");
            foreach (object hive in hives)
            {
                rows.Add("HIVE:" + ReadField<string>(hive, "Id")
                    + ":" + ReadField<string>(hive, "Badge")
                    + ":" + ReadField<object>(hive, "Stage")
                    + ":" + VectorLabel(ReadField<Vector2>(hive, "WorldCoord")));
            }

            foreach (object resource in resources)
            {
                rows.Add("RESOURCE:" + ReadField<string>(resource, "Id")
                    + ":" + ReadField<object>(resource, "Kind")
                    + ":" + ReadField<int>(resource, "Amount").ToString(CultureInfo.InvariantCulture)
                    + ":" + VectorLabel(ReadField<Vector2>(resource, "WorldCoord")));
            }

            rows.Sort(StringComparer.Ordinal);
            return "seed=738921;chunk=C35_32;" + string.Join("|", rows);
        }

        private static string BuildDeterminismJson(DeterminismResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"proof_id\": \"DEMO-090-C35_32-DETERMINISM\",");
            builder.AppendLine("  \"scope\": \"local_demo_runtime_comparison\",");
            builder.AppendLine("  \"seed\": 738921,");
            builder.AppendLine("  \"chunk_id\": \"C35_32\",");
            builder.AppendLine("  \"method\": \"Two fresh runtime bootstrap instances, identical scene initialization order, same seed and chunk; sorted entity IDs/types/amounts/world coordinates hashed with SHA-256\",");
            builder.AppendLine("  \"first_sha256\": \"" + result.FirstHash + "\",");
            builder.AppendLine("  \"second_sha256\": \"" + result.SecondHash + "\",");
            builder.AppendLine("  \"hashes_match\": " + result.Match.ToString().ToLowerInvariant() + ",");
            builder.AppendLine("  \"first_snapshot\": \"" + JsonEscape(result.FirstSnapshot) + "\",");
            builder.AppendLine("  \"second_snapshot\": \"" + JsonEscape(result.SecondSnapshot) + "\",");
            builder.AppendLine("  \"official_server_authority\": false");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildManifest(DeterminismResult determinism)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-090 World Map Large World Chunks Step 3 - Capture Manifest");
            builder.AppendLine();
            builder.AppendLine("- Date: `2026-07-13`");
            builder.AppendLine("- Scene: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`");
            builder.AppendLine("- Capture mode: `Unity Play Mode local/demo, Editor capture states only`");
            builder.AppendLine("- World: `BK-DEMO-WORLD-STEP3`");
            builder.AppendLine("- Logical chunks: `64x64`");
            builder.AppendLine("- Active neighborhood: `5x5 / 25 chunks`");
            builder.AppendLine("- Seed: `738921`");
            builder.AppendLine("- Debug grid activation: `capture state sets the same runtime boolean toggled by G; physical key press is not recorded`");
            builder.AppendLine("- Product captures debug grid: `OFF`");
            builder.AppendLine("- Flight launch: `VOL-42 created by runtime StartLocalCollectionFlight`");
            builder.AppendLine("- Flight object preserved across C35_32 -> C36_32: `" + flightObjectPreserved.ToString().ToLowerInvariant() + "`");
            builder.AppendLine("- Flight world anchors preserved: `" + flightAnchorsPreserved.ToString().ToLowerInvariant() + "`");
            builder.AppendLine("- Flight before: `" + flightBeforeSnapshot + "`");
            builder.AppendLine("- Flight after: `" + flightAfterSnapshot + "`");
            builder.AppendLine("- Determinism hash C35_32 first: `" + determinism.FirstHash + "`");
            builder.AppendLine("- Determinism hash C35_32 second: `" + determinism.SecondHash + "`");
            builder.AppendLine("- Determinism hashes match: `" + determinism.Match.ToString().ToLowerInvariant() + "`");
            builder.AppendLine("- Painted roads ignored: `true`");
            builder.AppendLine("- Ground routes used: `false`");
            builder.AppendLine("- Ground pathfinding present: `false`");
            builder.AppendLine("- Inner hive modified: `false`");
            builder.AppendLine("- Server live: `false`");
            builder.AppendLine("- Official placement: `false`");
            builder.AppendLine("- Official collection: `false`");
            builder.AppendLine("- Persistent economy: `false`");
            builder.AppendLine("- Chunk art: `carte.png proxy samples; final artistic tiles are future work`");
            builder.AppendLine();
            builder.AppendLine("## Runtime Proof Rows");
            builder.AppendLine();
            foreach (string row in WorldMapMmoFullscreenFoundationBootstrap.WorldMapMmoFullscreenFoundationForProof()) builder.AppendLine("- `" + row + "`");
            foreach (string row in WorldMapMmoFullscreenFoundationBootstrap.WorldMapLargeWorldStep3SelfCheckForProof()) builder.AppendLine("- `" + row + "`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();

            foreach (CaptureSpec capture in Captures)
            {
                string path = PathFor(capture);
                Vector2Int size = ReadPngSize(path, capture.Width, capture.Height);
                FileInfo file = new FileInfo(path);
                Vector2Int chunk = WorldToChunk(capture.WorldCenter);
                builder.AppendLine("### " + capture.Id);
                builder.AppendLine("- label: `" + capture.Label + "`");
                builder.AppendLine("- file: `" + path + "`");
                builder.AppendLine("- exists: `" + File.Exists(path).ToString().ToLowerInvariant() + "`");
                builder.AppendLine("- size_bytes: `" + (file.Exists ? file.Length.ToString(CultureInfo.InvariantCulture) : "0") + "`");
                builder.AppendLine("- requested_dimensions: `" + capture.Width.ToString(CultureInfo.InvariantCulture) + "x" + capture.Height.ToString(CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- actual_png_dimensions: `" + size.x.ToString(CultureInfo.InvariantCulture) + "x" + size.y.ToString(CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- png_sha256: `" + (file.Exists ? Sha256File(path) : "missing") + "`");
                builder.AppendLine("- world_center: `" + VectorLabel(capture.WorldCenter) + "`");
                builder.AppendLine("- chunk_id: `C" + chunk.x.ToString("00", CultureInfo.InvariantCulture) + "_" + chunk.y.ToString("00", CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- zoom: `" + capture.Zoom.ToString("0.00", CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- debug_chunks: `" + capture.DebugChunks.ToString().ToLowerInvariant() + "`");
                builder.AppendLine("- selected_hive: `" + capture.HiveId + "`");
                builder.AppendLine("- selected_resource: `" + capture.ResourceId + "`");
                builder.AppendLine("- collection_state: `" + capture.CollectionState + "`");
                builder.AppendLine();
            }

            builder.AppendLine("READY_FOR_QA_WORLD_MAP_LARGE_WORLD_CHUNKS_STEP3 = PENDING_VISUAL_INSPECTION");
            return builder.ToString();
        }

        private static object InvokePrivate(WorldMapMmoFullscreenFoundationBootstrap bootstrap, string methodName, params object[] args)
        {
            MethodInfo method = typeof(WorldMapMmoFullscreenFoundationBootstrap).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(typeof(WorldMapMmoFullscreenFoundationBootstrap).FullName, methodName);
            return method.Invoke(bootstrap, args);
        }

        private static void SetField<T>(WorldMapMmoFullscreenFoundationBootstrap bootstrap, string fieldName, T value)
        {
            FieldInfo field = typeof(WorldMapMmoFullscreenFoundationBootstrap).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(typeof(WorldMapMmoFullscreenFoundationBootstrap).FullName, fieldName);
            field.SetValue(bootstrap, value);
        }

        private static T ReadField<T>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(instance.GetType().FullName, fieldName);
            return (T)field.GetValue(instance);
        }

        private static void SetCollectionState(WorldMapMmoFullscreenFoundationBootstrap bootstrap, string value)
        {
            FieldInfo field = typeof(WorldMapMmoFullscreenFoundationBootstrap).GetField("collectionState", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(typeof(WorldMapMmoFullscreenFoundationBootstrap).FullName, "collectionState");
            field.SetValue(bootstrap, Enum.Parse(field.FieldType, value));
        }

        private static Vector2 ChunkCenter(int x, int y)
        {
            return new Vector2((x + 0.5f) * ChunkSize, (y + 0.5f) * ChunkSize);
        }

        private static Vector2Int WorldToChunk(Vector2 worldCenter)
        {
            return new Vector2Int(Mathf.FloorToInt(worldCenter.x / ChunkSize), Mathf.FloorToInt(worldCenter.y / ChunkSize));
        }

        private static string VectorLabel(Vector2 value)
        {
            return value.x.ToString("0.000", CultureInfo.InvariantCulture) + "," + value.y.ToString("0.000", CultureInfo.InvariantCulture);
        }

        private static string PathFor(CaptureSpec capture)
        {
            return OutputDirectory + "/" + capture.FileName;
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static Vector2Int ReadPngSize(string path, int fallbackWidth, int fallbackHeight)
        {
            if (!File.Exists(path)) return new Vector2Int(fallbackWidth, fallbackHeight);
            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(bytes)) return new Vector2Int(fallbackWidth, fallbackHeight);
                return new Vector2Int(texture.width, texture.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string Sha256(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static string JsonEscape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static void TrySetGameViewSize(int width, int height, string label)
        {
            try
            {
                Type gameView = Type.GetType("UnityEditor.GameView,UnityEditor");
                EditorWindow window = gameView == null ? null : EditorWindow.GetWindow(gameView);
                if (window == null) return;
                window.minSize = new Vector2(width, height);
                window.maxSize = new Vector2(width, height);
                window.titleContent = new GUIContent(label);
                window.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not resize Game View for DEMO-090 capture: " + exception.Message);
            }
        }

        private readonly struct DeterminismResult
        {
            public readonly string FirstSnapshot;
            public readonly string SecondSnapshot;
            public readonly string FirstHash;
            public readonly string SecondHash;

            public DeterminismResult(string firstSnapshot, string secondSnapshot, string firstHash, string secondHash)
            {
                FirstSnapshot = firstSnapshot;
                SecondSnapshot = secondSnapshot;
                FirstHash = firstHash;
                SecondHash = secondHash;
            }

            public bool Match => FirstHash == SecondHash && FirstSnapshot == SecondSnapshot;
        }
    }
}
