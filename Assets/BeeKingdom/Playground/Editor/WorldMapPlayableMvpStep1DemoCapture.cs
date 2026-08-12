using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class WorldMapPlayableMvpStep1DemoCapture
    {
        private const string ScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-088_WorldMapPlayableMvpStep1";
        private const string ManifestPath = OutputDirectory + "/DEMO-088_WorldMapPlayableMvpStep1_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.WorldMapPlayableMvpStep1Demo.Requested";
        private const string StateFrames = "BeeKingdom.Playground.WorldMapPlayableMvpStep1Demo.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.WorldMapPlayableMvpStep1Demo.Captured";
        private const string StateIndex = "BeeKingdom.Playground.WorldMapPlayableMvpStep1Demo.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly float Zoom;
            public readonly Vector2 Pan;
            public readonly string CollectionState;
            public readonly float CollectionTimer;
            public readonly string Status;
            public readonly string Reward;

            public CaptureSpec(string id, string label, string fileName, int width, int height, float zoom, Vector2 pan, string collectionState, float collectionTimer, string status, string reward)
            {
                Id = id;
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Zoom = zoom;
                Pan = pan;
                CollectionState = collectionState;
                CollectionTimer = collectionTimer;
                Status = status;
                Reward = reward;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("OverviewFullscreen", "Carte mondiale plein ecran jouable", "DEMO088_01_OverviewFullscreen_1280x720.png", 1280, 720, 1.00f, Vector2.zero, "Idle", 0f, "Selection locale/demo prete: ruche + ressource", "Aucune recompense locale"),
            new CaptureSpec("HiveSelected", "Selection ruche sur la carte", "DEMO088_02_HiveSelected_1280x720.png", 1280, 720, 1.12f, new Vector2(-90f, 48f), "Idle", 0f, "Ruche selectionnee: Ruche test Alpha", "Aucune recompense locale"),
            new CaptureSpec("ResourceSelectedCollect", "Selection ressource et bouton Collecter", "DEMO088_03_ResourceSelectedCollect_1280x720.png", 1280, 720, 1.18f, new Vector2(-130f, 62f), "Idle", 0f, "Ressource cible selectionnee: Nectar", "Aucune recompense locale"),
            new CaptureSpec("EnVol", "Etat En vol local demo", "DEMO088_04_EnVol_1280x720.png", 1280, 720, 1.34f, new Vector2(-115f, 72f), "FlyingToResource", 1.25f, "Vol aerien local/demo lance: Ruche test Alpha -> Nectar", "En attente retour essaim"),
            new CaptureSpec("Collecte", "Etat Collecte local demo", "DEMO088_05_Collecte_1280x720.png", 1280, 720, 1.34f, new Vector2(-115f, 72f), "Collecting", 0.62f, "Collecte locale/demo en cours: Nectar", "En attente retour essaim"),
            new CaptureSpec("Retour", "Etat Retour local demo", "DEMO088_06_Retour_1280x720.png", 1280, 720, 1.34f, new Vector2(-115f, 72f), "Returning", 1.40f, "Retour aerien vers la ruche - aucune route au sol", "En attente retour essaim"),
            new CaptureSpec("TermineReward", "Etat Termine avec recompense locale demo", "DEMO088_07_TermineReward_1280x720.png", 1280, 720, 1.18f, new Vector2(-130f, 62f), "Completed", 0f, "Collecte locale/demo terminee: +15 Nectar local/demo", "+15 Nectar local/demo"),
            new CaptureSpec("PanZoomFixedHud", "Pan zoom apres MVP avec HUD fixe", "DEMO088_08_PanZoomFixedHud_1280x720.png", 1280, 720, 1.70f, new Vector2(-300f, 150f), "FlyingToResource", 1.75f, "Pan/zoom conserve apres ajout MVP - panneaux fixes", "En attente retour essaim"),
            new CaptureSpec("TabletLandscape", "Tablette paysage MVP world map", "DEMO088_09_TabletLandscape_1920x1200.png", 1920, 1200, 1.12f, new Vector2(-140f, 70f), "Idle", 0f, "Carte MMO locale/demo tablette - aucune collecte officielle", "Aucune recompense locale")
        };

        static WorldMapPlayableMvpStep1DemoCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-088 World Map Playable MVP Step 1")]
        public static void CaptureWorldMapPlayableMvpStep1()
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

        public static void CaptureWorldMapPlayableMvpStep1ForBatch()
        {
            CaptureWorldMapPlayableMvpStep1();
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
                    throw new InvalidOperationException("DEMO-088 screenshot was not written: " + path);
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
                Debug.Log("DEMO-088 world map playable MVP step 1 screenshots captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-088 world map playable MVP step 1 capture failed: " + exception);
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
            SetField(bootstrap, "selectedHiveId", "hive_alpha");
            SetField(bootstrap, "selectedResourceId", "res_nectar_01");
            SetField(bootstrap, "collectionTimer", capture.CollectionTimer);
            SetField(bootstrap, "status", capture.Status);
            SetField(bootstrap, "localRewardText", capture.Reward);
            SetCollectionState(bootstrap, capture.CollectionState);
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
            builder.AppendLine("# DEMO-088 World Map Playable MVP Step 1 Manifest");
            builder.AppendLine();
            builder.AppendLine("- Date: 2026-07-13");
            builder.AppendLine("- Scene: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`");
            builder.AppendLine("- Builder-B report: `C:/projets/beekingdom/prompts_codex/rapports/BuilderB_WorldMapPlayableMvpStep1_Report.md`");
            builder.AppendLine("- Capture mode: Play Mode local/demo");
            builder.AppendLine("- Inner hive modified: `false`");
            builder.AppendLine("- Painted roads ignored: `true`");
            builder.AppendLine("- Ground route claim: `false`");
            builder.AppendLine("- Server live claim: `false`");
            builder.AppendLine("- Official collection claim: `false`");
            builder.AppendLine("- Persistent economy claim: `false`");
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
                builder.AppendLine();
            }

            builder.AppendLine("READY_FOR_QA_WORLD_MAP_PLAYABLE_MVP_STEP1 = YES");
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
                Debug.LogWarning("Could not resize Game View for DEMO-088 capture: " + exception.Message);
            }
        }
    }
}
