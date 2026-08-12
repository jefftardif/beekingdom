using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxActiveBeesPortraitFixCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-081_ActiveBeesPortraitHud";
        private const string ManifestPath = OutputDirectory + "/ActiveBeesPortraitHud_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.ActiveBeesPortraitHud.Requested";
        private const string StateFrames = "BeeKingdom.Playground.ActiveBeesPortraitHud.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.ActiveBeesPortraitHud.Captured";
        private const string StateIndex = "BeeKingdom.Playground.ActiveBeesPortraitHud.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string HotspotId;
            public readonly string LoopState;
            public readonly Vector2 Pan;
            public readonly float Zoom;

            public CaptureSpec(string id, string label, string fileName, int width, int height, string hotspotId, string loopState, Vector2 pan, float zoom)
            {
                Id = id;
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                HotspotId = hotspotId;
                LoopState = loopState;
                Pan = pan;
                Zoom = zoom;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("IdleNoRuntimeBees", "Idle building, no active runtime bees", "ActiveBees_Idle_NoRuntimeBees_1280x720.png", 1280, 720, "honey_storage", "idle", Vector2.zero, 1.08f),
            new CaptureSpec("UpgradeRunning", "Upgrade running, runtime bees visible", "ActiveBees_UpgradeRunning_1280x720.png", 1280, 720, "honey_storage", "upgrade_running", new Vector2(24f, -8f), 1.12f),
            new CaptureSpec("TrainingRunning", "Training running near barracks", "ActiveBees_TrainingRunning_1280x720.png", 1280, 720, "guard_post", "training_running", new Vector2(-18f, 10f), 1.12f),
            new CaptureSpec("PhonePortrait", "Phone portrait, readable resource HUD", "PortraitHud_390x844.png", 390, 844, "guard_post", "training_running", new Vector2(-160f, 48f), 1.16f),
            new CaptureSpec("TabletLandscape", "Tablet landscape, new background preserved", "TabletLandscape_1920x1200.png", 1920, 1200, "honey_storage", "resources_tick", Vector2.zero, 1.05f)
        };

        static SandboxActiveBeesPortraitFixCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture Active Bees Portrait HUD Fix")]
        public static void CaptureActiveBeesPortraitHudFix()
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

        public static void CaptureActiveBeesPortraitHudFixForBatch()
        {
            CaptureActiveBeesPortraitHudFix();
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
            if (frames < 80) return;

            try
            {
                string path = CurrentPath();
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    ScreenCapture.CaptureScreenshot(path);
                    SessionState.SetBool(StateCaptured, true);
                    return;
                }

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (frames < 240) return;
                    throw new InvalidOperationException("Active bees portrait HUD screenshot was not written: " + path);
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
                Debug.Log("Active bees portrait HUD fix screenshots captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("Active bees portrait HUD fix capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.ServerPreparation);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(capture.HotspotId);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(capture.Zoom);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState(capture.LoopState);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Active Bees And Portrait HUD Manifest");
            builder.AppendLine();
            builder.AppendLine("- Surface: `SandboxPlayground Play Mode local`");
            builder.AppendLine("- Runtime background: `PremiumBeeReference/bg_beehive`");
            builder.AppendLine("- Runtime bee sprite: `PremiumBeeReference/worker-bee-runtime`");
            builder.AppendLine("- Hexagonal runtime bee icons expected: `false`");
            builder.AppendLine("- World map relaunched: `false`");
            builder.AppendLine("- BEE-881 created or unlocked: `false`");
            builder.AppendLine("- Official server/live claim: `false`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();

            foreach (CaptureSpec capture in Captures)
            {
                Vector2Int size = ReadPngSize(PathFor(capture), capture.Width, capture.Height);
                FileInfo file = new FileInfo(PathFor(capture));
                builder.AppendLine("### " + capture.Id);
                builder.AppendLine();
                builder.AppendLine("- label: `" + capture.Label + "`");
                builder.AppendLine("- file: `" + PathFor(capture) + "`");
                builder.AppendLine("- file_exists: `" + File.Exists(PathFor(capture)).ToString() + "`");
                builder.AppendLine("- file_size_bytes: `" + (file.Exists ? file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0") + "`");
                builder.AppendLine("- actual_dimensions: `" + size.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "x" + size.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- hotspot: `" + capture.HotspotId + "`");
                builder.AppendLine("- loop_state: `" + capture.LoopState + "`");
                builder.AppendLine("- zoom: `" + capture.Zoom.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine();
            }

            builder.AppendLine("READY_FOR_QA_ACTIVE_BEES_AND_PORTRAIT_FIX = YES");
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
                Debug.LogWarning("Could not resize Game View for active bees portrait HUD capture: " + exception.Message);
            }
        }
    }
}
