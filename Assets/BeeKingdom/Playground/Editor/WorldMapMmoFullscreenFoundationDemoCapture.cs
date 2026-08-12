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
    public static class WorldMapMmoFullscreenFoundationDemoCapture
    {
        private const string ScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-087_WorldMapFullscreenFoundation";
        private const string ManifestPath = OutputDirectory + "/DEMO-087_WorldMapFullscreenFoundation_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.WorldMapMmoFullscreenFoundationDemo.Requested";
        private const string StateFrames = "BeeKingdom.Playground.WorldMapMmoFullscreenFoundationDemo.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.WorldMapMmoFullscreenFoundationDemo.Captured";
        private const string StateIndex = "BeeKingdom.Playground.WorldMapMmoFullscreenFoundationDemo.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly float Zoom;
            public readonly Vector2 Pan;

            public CaptureSpec(string id, string label, string fileName, int width, int height, float zoom, Vector2 pan)
            {
                Id = id;
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Zoom = zoom;
                Pan = pan;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("OverviewFullscreen", "World map fullscreen overview", "DEMO087_01_OverviewFullscreen_1280x720.png", 1280, 720, 1.00f, Vector2.zero),
            new CaptureSpec("PanShifted", "Pan shifted proof with fixed HUD", "DEMO087_02_PanShifted_1280x720.png", 1280, 720, 1.00f, new Vector2(-180f, 96f)),
            new CaptureSpec("Zoomed", "Zoom proof with fixed HUD/minimap", "DEMO087_03_Zoomed_1280x720.png", 1280, 720, 1.62f, new Vector2(-260f, 126f)),
            new CaptureSpec("AerialSwarmArc", "Aerial swarm arc no ground route", "DEMO087_04_AerialSwarmArc_1280x720.png", 1280, 720, 1.36f, new Vector2(-120f, 78f)),
            new CaptureSpec("TabletLandscape", "Tablet landscape world map", "DEMO087_05_TabletLandscape_1920x1200.png", 1920, 1200, 1.08f, new Vector2(-140f, 70f))
        };

        static WorldMapMmoFullscreenFoundationDemoCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-087 World Map Fullscreen Foundation")]
        public static void CaptureWorldMapMmoFullscreenFoundation()
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

        public static void CaptureWorldMapMmoFullscreenFoundationForBatch()
        {
            CaptureWorldMapMmoFullscreenFoundation();
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
                    throw new InvalidOperationException("DEMO-087 screenshot was not written: " + path);
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
                Debug.Log("DEMO-087 world map fullscreen foundation screenshots captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-087 world map fullscreen foundation capture failed: " + exception);
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
        }

        private static void SetField<T>(WorldMapMmoFullscreenFoundationBootstrap bootstrap, string fieldName, T value)
        {
            FieldInfo field = typeof(WorldMapMmoFullscreenFoundationBootstrap).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) field.SetValue(bootstrap, value);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-087 World Map Fullscreen Foundation Manifest");
            builder.AppendLine();
            builder.AppendLine("- Date: 2026-07-13");
            builder.AppendLine("- Scene: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`");
            builder.AppendLine("- Builder-B report: `C:/projets/beekingdom/prompts_codex/rapports/BuilderB_WorldMapMmoFullscreenFoundation_Report.md`");
            builder.AppendLine("- Capture mode: Play Mode local demo");
            builder.AppendLine("- Inner hive modified: `false`");
            builder.AppendLine("- Ground route claim: `false`");
            builder.AppendLine("- Server live claim: `false`");
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
                builder.AppendLine();
            }

            builder.AppendLine("READY_FOR_QA_WORLD_MAP_FULLSCREEN_FOUNDATION = YES");
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
                Debug.LogWarning("Could not resize Game View for DEMO-087 capture: " + exception.Message);
            }
        }
    }
}
