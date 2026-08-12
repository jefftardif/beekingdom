using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxSplashAuthOfficialCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-085_SplashAuth";
        private const string ManifestPath = OutputDirectory + "/DEMO-085_SplashAuth_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.SplashAuthOfficial.Requested";
        private const string StateFrames = "BeeKingdom.Playground.SplashAuthOfficial.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.SplashAuthOfficial.Captured";
        private const string StateIndex = "BeeKingdom.Playground.SplashAuthOfficial.Index";

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
            new CaptureSpec("SplashScreen", "Splash screen", "SplashAuth_01_SplashScreen_1280x720.png", 1280, 720, "splash"),
            new CaptureSpec("LoginEmpty", "Empty login screen", "SplashAuth_02_LoginEmpty_1280x720.png", 1280, 720, "login_empty"),
            new CaptureSpec("RequiredFieldError", "Required field error", "SplashAuth_03_RequiredFieldError_1280x720.png", 1280, 720, "invalid_login"),
            new CaptureSpec("CreateAccount", "Create account", "SplashAuth_04_CreateAccount_1280x720.png", 1280, 720, "create"),
            new CaptureSpec("AccountCreatedLocal", "Account created local demo", "SplashAuth_05_AccountCreatedLocal_1280x720.png", 1280, 720, "created_local"),
            new CaptureSpec("TransitionHive", "Transition to hive", "SplashAuth_06_TransitionHive_1280x720.png", 1280, 720, "hive"),
            new CaptureSpec("PhonePortrait", "Phone portrait", "SplashAuth_07_PhonePortrait_390x844.png", 390, 844, "create"),
            new CaptureSpec("TabletLandscape", "Tablet landscape", "SplashAuth_08_TabletLandscape_1920x1200.png", 1920, 1200, "login_empty")
        };

        static SandboxSplashAuthOfficialCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-085 Splash Auth")]
        public static void CaptureSplashAuthOfficial()
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

        public static void CaptureSplashAuthOfficialForBatch()
        {
            CaptureSplashAuthOfficial();
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
            // The runtime splash lasts 2.4 seconds; in batchmode 80 editor updates can
            // complete before that real-time interval has elapsed.
            if (frames < 240) return;

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
                    throw new InvalidOperationException("DEMO-085 screenshot was not written: " + path);
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
                Debug.Log("DEMO-085 splash auth screenshots captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-085 splash auth capture failed: " + exception);
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
            builder.AppendLine("# DEMO-085 Splash Auth Manifest");
            builder.AppendLine();
            builder.AppendLine("- Date: 2026-07-13");
            builder.AppendLine("- Scene: `Assets/Scenes/SandboxPlayground.unity`");
            builder.AppendLine("- Splash screen: `enabled`");
            builder.AppendLine("- Login: `local demo only`");
            builder.AppendLine("- Create account: `local demo only`");
            builder.AppendLine("- Server auth live: `false`");
            builder.AppendLine("- Official account claim: `false`");
            builder.AppendLine("- Official save claim: `false`");
            builder.AppendLine("- Economy live claim: `false`");
            builder.AppendLine("- World map relaunched: `false`");
            builder.AppendLine("- BEE-881 created or unlocked: `false`");
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

            builder.AppendLine("READY_FOR_QA_SPLASH_AUTH = YES");
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
                Debug.LogWarning("Could not resize Game View for DEMO-085 capture: " + exception.Message);
            }
        }
    }
}
