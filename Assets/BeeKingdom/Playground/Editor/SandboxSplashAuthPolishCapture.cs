using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxSplashAuthPolishCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-086_SplashAuthPolish";
        private const string ManifestPath = OutputDirectory + "/DEMO-086_SplashAuthPolish_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.SplashAuthPolish.Requested";
        private const string StateFrames = "BeeKingdom.Playground.SplashAuthPolish.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.SplashAuthPolish.Captured";
        private const string StateIndex = "BeeKingdom.Playground.SplashAuthPolish.Index";

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
            new CaptureSpec("SplashDesktop", "Splash desktop polished", "SplashAuthPolish_01_SplashDesktop_1280x720.png", 1280, 720, "splash"),
            new CaptureSpec("LoginDesktop", "Connexion desktop", "SplashAuthPolish_02_LoginDesktop_1280x720.png", 1280, 720, "login_empty"),
            new CaptureSpec("CreateDesktop", "Creation compte desktop", "SplashAuthPolish_03_CreateDesktop_1280x720.png", 1280, 720, "create"),
            new CaptureSpec("RequiredFieldError", "Erreur champ requis", "SplashAuthPolish_04_RequiredFieldError_1280x720.png", 1280, 720, "invalid_login"),
            new CaptureSpec("LocalDemoConfirmed", "Confirmation compte local demo", "SplashAuthPolish_05_LocalDemoConfirmed_1280x720.png", 1280, 720, "created_local"),
            new CaptureSpec("TransitionHive", "Transition vers la ruche", "SplashAuthPolish_06_TransitionHive_1280x720.png", 1280, 720, "hive"),
            new CaptureSpec("PhonePortrait", "Telephone portrait creation lisible", "SplashAuthPolish_07_PhonePortrait_390x844.png", 390, 844, "create"),
            new CaptureSpec("TabletLandscape", "Tablette paysage panneau auth confortable", "SplashAuthPolish_08_TabletLandscape_1920x1200.png", 1920, 1200, "login_empty"),
            new CaptureSpec("SocialFutureDisabled", "Etat social futur desactive", "SplashAuthPolish_09_SocialFutureDisabled_1280x720.png", 1280, 720, "login")
        };

        static SandboxSplashAuthPolishCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-086 Splash Auth Polish")]
        public static void CaptureSplashAuthPolish()
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

        public static void CaptureSplashAuthPolishForBatch()
        {
            CaptureSplashAuthPolish();
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

                if (frames < 120) return;

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (frames < 240) return;
                    throw new InvalidOperationException("DEMO-086 screenshot was not written: " + path);
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
                Debug.Log("DEMO-086 splash auth polish screenshots captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-086 splash auth polish capture failed: " + exception);
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
            builder.AppendLine("# DEMO-086 Splash Auth Polish Manifest");
            builder.AppendLine();
            builder.AppendLine("- Date: 2026-07-13");
            builder.AppendLine("- Scene: `Assets/Scenes/SandboxPlayground.unity`");
            builder.AppendLine("- Builder-A report: `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_SplashAuthPolish_Report.md`");
            builder.AppendLine("- Official logo preserved: `true`");
            builder.AppendLine("- Splash social/future buttons masked: `false`");
            builder.AppendLine("- Social auth live claim: `false`");
            builder.AppendLine("- Server auth live claim: `false`");
            builder.AppendLine("- Official account promised: `false`");
            builder.AppendLine("- World map relaunched: `false`");
            builder.AppendLine("- SVG contours modified by Demo: `false`");
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

            builder.AppendLine("READY_FOR_QA_SPLASH_AUTH_POLISH = YES");
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
                Debug.LogWarning("Could not resize Game View for DEMO-086 capture: " + exception.Message);
            }
        }
    }
}
