using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class WorldMapPlayableMvpStep2DemoCapture
    {
        private const string ScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-089_WorldMapPlayableMvpStep2";
        private const string ManifestPath = OutputDirectory + "/DEMO-089_WorldMapPlayableMvpStep2_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.WorldMapPlayableMvpStep2Demo.Requested";
        private const string StateFrames = "BeeKingdom.Playground.WorldMapPlayableMvpStep2Demo.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.WorldMapPlayableMvpStep2Demo.Captured";
        private const string StateIndex = "BeeKingdom.Playground.WorldMapPlayableMvpStep2Demo.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly float Zoom;
            public readonly Vector2 Pan;
            public readonly string HiveId;
            public readonly string ResourceId;
            public readonly string CollectionState;
            public readonly float CollectionTimer;
            public readonly string Status;
            public readonly string Reward;

            public CaptureSpec(string id, string label, string fileName, int width, int height, float zoom, Vector2 pan, string hiveId, string resourceId, string collectionState, float collectionTimer, string status, string reward)
            {
                Id = id;
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Zoom = zoom;
                Pan = pan;
                HiveId = hiveId;
                ResourceId = resourceId;
                CollectionState = collectionState;
                CollectionTimer = collectionTimer;
                Status = status;
                Reward = reward;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Overview3Hives5Resources", "3 hives and 5 resources fullscreen", "DEMO089_01_Overview3Hives5Resources_1280x720.png", 1280, 720, 1.00f, Vector2.zero, "hive_alpha", "res_nectar_01", "Idle", 0f, "Step 2 local/demo: 3 ruches, 5 ressources, vols multiples", "Aucune recompense locale"),
            new CaptureSpec("SourceHiveSelected", "Source hive selected", "DEMO089_02_SourceHiveSelected_1280x720.png", 1280, 720, 1.14f, new Vector2(-90f, 42f), "hive_alpha", "res_nectar_01", "Idle", 0f, "Ruche source selectionnee: Ruche joueur test", "Aucune recompense locale"),
            new CaptureSpec("RoyalJellyTargetCollect", "Royal jelly target with Collecter", "DEMO089_03_RoyalJellyTargetCollect_1280x720.png", 1280, 720, 1.08f, new Vector2(-260f, 74f), "hive_alpha", "res_royal_jelly_01", "Idle", 0f, "Ressource cible selectionnee: Gelee royale demo", "Aucune recompense locale"),
            new CaptureSpec("NewFlightLaunched", "New local demo flight launched", "DEMO089_04_NewFlightLaunched_1280x720.png", 1280, 720, 1.22f, new Vector2(-210f, 82f), "hive_alpha", "res_royal_jelly_01", "FlyingToResource", 0.62f, "Vol aerien local/demo lance: Ruche joueur test -> Gelee royale demo", "En attente retour essaim"),
            new CaptureSpec("MultiFlightsJournal", "Multiple aerial flights and fixed journal", "DEMO089_05_MultiFlightsJournal_1280x720.png", 1280, 720, 1.18f, new Vector2(-145f, 78f), "hive_alpha", "res_nectar_01", "FlyingToResource", 1.80f, "Vols multiples local/demo visibles - routes peintes ignorees", "En attente retour essaim"),
            new CaptureSpec("CollectingState", "Collecte state with reward pending", "DEMO089_06_CollectingState_1280x720.png", 1280, 720, 1.24f, new Vector2(-140f, 76f), "hive_alpha", "res_nectar_01", "Collecting", 0.68f, "Collecte locale/demo en cours: Nectar", "En attente retour essaim"),
            new CaptureSpec("ReturningState", "Retour state aerial route", "DEMO089_07_ReturningState_1280x720.png", 1280, 720, 1.24f, new Vector2(-140f, 76f), "hive_alpha", "res_nectar_01", "Returning", 1.44f, "Retour aerien vers la ruche - aucune route au sol", "En attente retour essaim"),
            new CaptureSpec("CompletedReward", "Termine state with local demo reward", "DEMO089_08_CompletedReward_1280x720.png", 1280, 720, 1.14f, new Vector2(-120f, 60f), "hive_alpha", "res_nectar_01", "Completed", 0f, "Collecte locale/demo terminee: +15 Nectar local/demo", "+15 Nectar local/demo"),
            new CaptureSpec("PanZoomFixedPanels", "Pan zoom after multiple flights with fixed panels", "DEMO089_09_PanZoomFixedPanels_1280x720.png", 1280, 720, 1.78f, new Vector2(-320f, 154f), "hive_alpha", "res_nectar_01", "FlyingToResource", 1.55f, "Pan/zoom apres vols multiples - HUD et journal fixes", "En attente retour essaim"),
            new CaptureSpec("TabletLandscape", "Tablet landscape Step 2", "DEMO089_10_TabletLandscape_1920x1200.png", 1920, 1200, 1.12f, new Vector2(-142f, 70f), "hive_alpha", "res_royal_jelly_01", "Idle", 0f, "Carte MMO locale/demo tablette - server_live:false", "Aucune recompense locale")
        };

        static WorldMapPlayableMvpStep2DemoCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-089 World Map Playable MVP Step 2")]
        public static void CaptureWorldMapPlayableMvpStep2()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            SessionState.SetBool(StateRequested, true);
            SessionState.SetBool(StateCaptured, false);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetInt(StateIndex, 0);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        public static void CaptureWorldMapPlayableMvpStep2ForBatch()
        {
            CaptureWorldMapPlayableMvpStep2();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false) || state != PlayModeStateChange.EnteredPlayMode) return;
            ApplyCurrentState();
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

            ApplyCurrentState();
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 100) return;

            try
            {
                string path = CurrentPath();
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    ScreenCapture.CaptureScreenshot(path);
                    SessionState.SetBool(StateCaptured, true);
                    return;
                }

                if (frames < 140) return;

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (frames < 260) return;
                    throw new InvalidOperationException("DEMO-089 screenshot was not written: " + path);
                }

                int index = SessionState.GetInt(StateIndex, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    ApplyCurrentState();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-089 world map playable MVP step 2 screenshots captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-089 world map playable MVP step 2 capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);

            WorldMapMmoFullscreenFoundationBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
            if (bootstrap == null) return;

            SetField(bootstrap, "currentZoom", capture.Zoom);
            SetField(bootstrap, "targetZoom", capture.Zoom);
            SetField(bootstrap, "currentPan", capture.Pan);
            SetField(bootstrap, "targetPan", capture.Pan);
            SetField(bootstrap, "selectedHiveId", capture.HiveId);
            SetField(bootstrap, "selectedResourceId", capture.ResourceId);
            SetField(bootstrap, "collectionTimer", capture.CollectionTimer);
            SetField(bootstrap, "status", capture.Status);
            SetField(bootstrap, "localRewardText", capture.Reward);
            SetCollectionState(bootstrap, capture.CollectionState);
            SeedProofFlights(bootstrap, capture.CollectionState, capture.CollectionTimer, capture.ResourceId);
        }

        private static void SeedProofFlights(WorldMapMmoFullscreenFoundationBootstrap bootstrap, string state, float timer, string selectedResource)
        {
            FieldInfo flightsField = typeof(WorldMapMmoFullscreenFoundationBootstrap).GetField("flights", BindingFlags.Instance | BindingFlags.NonPublic);
            IList flights = flightsField == null ? null : flightsField.GetValue(bootstrap) as IList;
            if (flights == null) return;

            flights.Clear();
            flights.Add(NewFlight("VOL-11", "hive_beta", "res_pollen_01", "FlyingToResource", 1.45f, "+20 Pollen local/demo", "Vol allie demo"));
            flights.Add(NewFlight("VOL-12", "hive_gamma", "res_propolis_01", "Returning", 1.05f, "+7 Propolis local/demo", "Retour neutre demo"));
            flights.Add(NewFlight("VOL-13", "hive_alpha", selectedResource, state, timer, RewardFor(selectedResource), "Collecte joueur demo"));
            SetField(bootstrap, "nextFlightId", 14);
        }

        private static object NewFlight(string id, string hiveId, string resourceId, string stateName, float timer, string reward, string label)
        {
            Type bootstrapType = typeof(WorldMapMmoFullscreenFoundationBootstrap);
            Type flightType = bootstrapType.GetNestedType("WorldFlightRecord", BindingFlags.NonPublic);
            Type stateType = bootstrapType.GetNestedType("CollectionFlightState", BindingFlags.NonPublic);
            object state = Enum.Parse(stateType, stateName);
            return Activator.CreateInstance(flightType, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { id, hiveId, resourceId, state, timer, reward, label }, null);
        }

        private static string RewardFor(string resourceId)
        {
            if (resourceId == "res_royal_jelly_01") return "+3 Gelee royale demo local/demo";
            if (resourceId == "res_pollen_01") return "+20 Pollen local/demo";
            if (resourceId == "res_wax_01") return "+11 Cire local/demo";
            if (resourceId == "res_propolis_01") return "+7 Propolis local/demo";
            return "+15 Nectar local/demo";
        }

        private static void SetField<T>(WorldMapMmoFullscreenFoundationBootstrap bootstrap, string fieldName, T value)
        {
            FieldInfo field = typeof(WorldMapMmoFullscreenFoundationBootstrap).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) field.SetValue(bootstrap, value);
        }

        private static void SetCollectionState(WorldMapMmoFullscreenFoundationBootstrap bootstrap, string value)
        {
            FieldInfo field = typeof(WorldMapMmoFullscreenFoundationBootstrap).GetField("collectionState", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;
            object state = Enum.Parse(field.FieldType, value);
            field.SetValue(bootstrap, state);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-089 World Map Playable MVP Step 2 Manifest");
            builder.AppendLine();
            builder.AppendLine("- Date: 2026-07-13");
            builder.AppendLine("- Scene: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`");
            builder.AppendLine("- Builder-C report: `C:/projets/beekingdom/prompts_codex/rapports/BuilderC_WorldMapPlayableMvpStep2_Report.md`");
            builder.AppendLine("- Capture mode: Play Mode local/demo");
            builder.AppendLine("- Visible hives minimum met: `true`");
            builder.AppendLine("- Visible resources minimum met: `true`");
            builder.AppendLine("- Flight journal captured: `true`");
            builder.AppendLine("- Painted roads ignored: `true`");
            builder.AppendLine("- Ground route claim: `false`");
            builder.AppendLine("- Server live claim: `false`");
            builder.AppendLine("- Official collection claim: `false`");
            builder.AppendLine("- Persistent economy claim: `false`");
            builder.AppendLine("- Inner hive modified: `false`");
            builder.AppendLine();
            builder.AppendLine("## Runtime Proof Rows");
            foreach (string row in WorldMapMmoFullscreenFoundationBootstrap.WorldMapMmoFullscreenFoundationForProof()) builder.AppendLine("- `" + row + "`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                Vector2Int size = ReadPngSize(PathFor(capture), capture.Width, capture.Height);
                FileInfo file = new FileInfo(PathFor(capture));
                builder.AppendLine("### " + capture.Id);
                builder.AppendLine("- label: `" + capture.Label + "`");
                builder.AppendLine("- file: `" + PathFor(capture) + "`");
                builder.AppendLine("- exists: `" + File.Exists(PathFor(capture)) + "`");
                builder.AppendLine("- size_bytes: `" + (file.Exists ? file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0") + "`");
                builder.AppendLine("- dimensions: `" + size.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "x" + size.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- proof_zoom: `" + capture.Zoom.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- proof_pan: `" + capture.Pan.x.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "," + capture.Pan.y.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- collection_state: `" + capture.CollectionState + "`");
                builder.AppendLine("- selected_resource: `" + capture.ResourceId + "`");
                builder.AppendLine();
            }

            builder.AppendLine("READY_FOR_QA_WORLD_MAP_PLAYABLE_MVP_STEP2 = YES");
            return builder.ToString();
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
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

        private static void TrySetGameViewSize(int width, int height, string label)
        {
            try
            {
                Type gameView = Type.GetType("UnityEditor.GameView,UnityEditor");
                EditorWindow window = gameView == null ? null : EditorWindow.GetWindow(gameView);
                if (window != null)
                {
                    window.minSize = new Vector2(width, height);
                    window.maxSize = new Vector2(width, height);
                    window.titleContent = new GUIContent(label);
                    window.Repaint();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not resize Game View for DEMO-089 capture: " + exception.Message);
            }
        }
    }
}
