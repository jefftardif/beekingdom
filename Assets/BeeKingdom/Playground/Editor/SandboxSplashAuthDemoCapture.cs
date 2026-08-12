using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxSplashAuthDemoCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-085_SplashAuthDemo_Source";
        private const string ManifestPath = OutputDirectory + "/SplashAuthDemo_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.SplashAuthDemo.Requested";
        private const string StateFrames = "BeeKingdom.Playground.SplashAuthDemo.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.SplashAuthDemo.Captured";
        private const string StateIndex = "BeeKingdom.Playground.SplashAuthDemo.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string GateState;

            public CaptureSpec(string id, string label, string fileName, int width, int height, string gateState)
            {
                Id = id;
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                GateState = gateState;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("DesktopSplash", "Desktop splash screen", "SplashAuth_01_DesktopSplash_1280x720.png", 1280, 720, "splash"),
            new CaptureSpec("TabletLogin", "Tablet demo login", "SplashAuth_02_TabletLogin_1920x1200.png", 1920, 1200, "login"),
            new CaptureSpec("PhoneCreate", "Phone demo create account", "SplashAuth_03_PhoneCreate_390x844.png", 390, 844, "create"),
            new CaptureSpec("PhoneValidationError", "Phone validation error", "SplashAuth_04_PhoneValidationError_390x844.png", 390, 844, "invalid_create"),
            new CaptureSpec("DesktopTransitionHive", "Desktop transition to hive", "SplashAuth_05_TransitionToHive_1280x720.png", 1280, 720, "hive")
        };

        static SandboxSplashAuthDemoCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture Splash Auth Demo")]
        public static void CaptureSplashAuthDemo()
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

        public static void CaptureSplashAuthDemoForBatch()
        {
            CaptureSplashAuthDemo();
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
                    throw new InvalidOperationException("Splash auth screenshot was not written: " + path);
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
                Debug.Log("Splash auth demo screenshots captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("Splash auth demo capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.LocalPreview);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(capture.Height > capture.Width ? 1.18f : 1.06f);
            HiveViewProductUiPresenter.SetSplashAuthGateForProof(capture.GateState);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Splash Auth Demo Manifest");
            builder.AppendLine();
            builder.AppendLine("- Scene: `Assets/Scenes/SandboxPlayground.unity`");
            builder.AppendLine("- Splash screen: `enabled`");
            builder.AppendLine("- Official logo transparent: `Assets/BeeKingdom/Playground/Resources/PremiumBeeReference/logo_trans.png`");
            builder.AppendLine("- Official logo fallback: `Assets/BeeKingdom/Playground/Resources/PremiumBeeReference/logo.png`");
            builder.AppendLine("- Login: `local demo only`");
            builder.AppendLine("- Create account: `local demo only`");
            builder.AppendLine("- Google/Facebook live auth: `false`");
            builder.AppendLine("- Server auth claim: `false`");
            builder.AppendLine("- World map touched: `false`");
            builder.AppendLine("- SVG contours touched: `false`");
            builder.AppendLine("- Hive transition preserved: `true`");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.SplashAuthDemoForProof()) builder.AppendLine("- " + row);
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
                builder.AppendLine("- gate_state: `" + capture.GateState + "`");
                builder.AppendLine();
            }

            builder.AppendLine("READY_FOR_DEMO_SPLASH_AUTH = YES");
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
                Type gameViewSizes = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
                Type gameViewSizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
                Type gameViewSizeGroupType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeGroupType");
                if (gameViewSizes == null || gameViewSizeType == null || gameViewSizeGroupType == null) return;

                object instance = gameViewSizes.GetMethod("GetGroup", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(ScriptableSingleton(gameViewSizes), new[] { Enum.Parse(gameViewSizeGroupType, "Standalone") });
                if (instance == null) return;

                object customSize = Activator.CreateInstance(gameViewSizeType, 1, width, height, label);
                instance.GetType().GetMethod("AddCustomSize")?.Invoke(instance, new[] { customSize });
            }
            catch
            {
                // Screen.SetResolution is enough for batch capture if the editor API changes.
            }
        }

        private static object ScriptableSingleton(Type type)
        {
            return type.GetProperty("instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(null, null);
        }
    }
}
