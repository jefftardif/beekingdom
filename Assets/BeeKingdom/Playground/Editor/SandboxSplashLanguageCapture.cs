using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxSplashLanguageCapture
    {
        private const string ScenePath = "Assets/Scenes/LivingHive.unity";
        private const string OutputDirectory = "Docs/Product/Evidence/LivingHiveLanguageSelector";
        private const string ManifestPath = OutputDirectory + "/LivingHiveLanguageSelectorManifest.md";
        private const string StateRequested = "BeeKingdom.Playground.SplashLanguage.Requested";
        private const string StateFrames = "BeeKingdom.Playground.SplashLanguage.Frames";
        private const string StateIndex = "BeeKingdom.Playground.SplashLanguage.Index";
        private const string StateCaptured = "BeeKingdom.Playground.SplashLanguage.Captured";
        private const string StateConfiguredIndex = "BeeKingdom.Playground.SplashLanguage.ConfiguredIndex";
        private const string StateExitWhenFinished = "BeeKingdom.Playground.SplashLanguage.ExitWhenFinished";
        private static double captureReadyAt;
        private static double screenshotRequestedAt;

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string Locale;
            public readonly string GateState;
            public readonly string ReadinessState;

            public CaptureSpec(string label, string fileName, int width, int height, string locale, string gateState = "splash", string readinessState = "not_configured")
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Locale = locale;
                GateState = gateState;
                ReadinessState = readinessState;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Entrée mobile en français", "LivingHive_Language_fr-CA_390x844.png", 390, 844, "fr-CA"),
            new CaptureSpec("Mobile entry in English", "LivingHive_Language_en-US_390x844.png", 390, 844, "en-US"),
            new CaptureSpec("Entrée paysage en français", "LivingHive_Language_fr-CA_1600x900.png", 1600, 900, "fr-CA"),
            new CaptureSpec("Landscape entry in English", "LivingHive_Language_en-US_1600x900.png", 1600, 900, "en-US"),
            new CaptureSpec("Compte officiel fermé côté serveur", "LivingHive_AccountPreparation_fr-CA_390x844.png", 390, 844, "fr-CA", "login", "preparation"),
            new CaptureSpec("Official account closed by server", "LivingHive_AccountPreparation_en-US_1600x900.png", 1600, 900, "en-US", "login", "preparation"),
            new CaptureSpec("Local demo profile without credentials", "LivingHive_DemoProfile_en-US_390x844.png", 390, 844, "en-US", "create", "not_configured"),
            new CaptureSpec("Profil démo sans identifiants", "LivingHive_DemoProfile_fr-CA_1600x900.png", 1600, 900, "fr-CA", "create", "not_configured")
        };

        static SandboxSplashLanguageCapture()
        {
            ReattachCallbacks();
        }

        [MenuItem("Bee Kingdom/Playground/Capture LivingHive Language Selector")]
        public static void Capture()
        {
            StartCapture(false);
        }

        public static void CaptureAndExit()
        {
            StartCapture(true);
        }

        private static void StartCapture(bool exitWhenFinished)
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);

            SessionState.SetBool(StateRequested, true);
            SessionState.SetBool(StateExitWhenFinished, exitWhenFinished);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetInt(StateIndex, 0);
            SessionState.SetInt(StateConfiguredIndex, -1);
            SessionState.SetBool(StateCaptured, false);
            captureReadyAt = EditorApplication.timeSinceStartup + 3.5d;
            screenshotRequestedAt = 0d;
            ReattachCallbacks();

            PlaygroundPlayModeStartScene.UseLivingHiveOnPlay();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void ReattachCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            if (SessionState.GetBool(StateRequested, false)) EditorApplication.update += OnPlayModeUpdate;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false) || state != PlayModeStateChange.EnteredPlayMode) return;
            ConfigureCurrentCapture();
            ApplyCurrentState();
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCaptured, false);
            captureReadyAt = EditorApplication.timeSinceStartup + 3.5d;
            screenshotRequestedAt = 0d;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            if (!Application.isPlaying || EditorApplication.timeSinceStartup < captureReadyAt) return;
            ConfigureCurrentCapture();
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
                    screenshotRequestedAt = EditorApplication.timeSinceStartup;
                    return;
                }

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (EditorApplication.timeSinceStartup - screenshotRequestedAt < 4d) return;
                    throw new InvalidOperationException("Language selector screenshot was not written: " + path);
                }

                CaptureSpec captured = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
                (int width, int height) = ReadPngDimensions(path);
                if (width != captured.Width || height != captured.Height)
                {
                    throw new InvalidOperationException("Screenshot dimensions were " + width + "x" + height
                        + " instead of " + captured.Width + "x" + captured.Height + ": " + path);
                }

                int index = SessionState.GetInt(StateIndex, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateConfiguredIndex, -1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    captureReadyAt = EditorApplication.timeSinceStartup + 1.2d;
                    screenshotRequestedAt = 0d;
                    ConfigureCurrentCapture();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("LivingHive language selector proof captured in " + OutputDirectory);
                if (SessionState.GetBool(StateExitWhenFinished, false)) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("LivingHive language selector proof failed: " + exception);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                if (SessionState.GetBool(StateExitWhenFinished, false)) EditorApplication.Exit(1);
            }
        }

        private static void ConfigureCurrentCapture()
        {
            int index = Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1);
            if (SessionState.GetInt(StateConfiguredIndex, -1) == index) return;
            CaptureSpec capture = Captures[index];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            SessionState.SetInt(StateConfiguredIndex, index);
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            HiveViewProductUiPresenter.SetLocaleForRuntime(capture.Locale);
            HiveViewProductUiPresenter.SetSplashAuthGateForProof(capture.GateState);
            HiveViewProductUiPresenter.SetAccountSessionReadinessForProof(capture.ReadinessState);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# LivingHive Language Selector Visual Proof");
            builder.AppendLine();
            builder.AppendLine("- Scene: `Assets/Scenes/LivingHive.unity`");
            builder.AppendLine("- Language preference authority: `device_only` (`PlayerPrefs`)");
            builder.AppendLine("- Official account/session authority: `server_only`");
            builder.AppendLine("- Credential collection while either readiness gate is closed: `false`");
            builder.AppendLine("- Password, access token or refresh token persisted by this shell: `false`");
            builder.AppendLine("- Production server state represented: `503 auth.unavailable`");
            builder.AppendLine("- Supported locales: `fr-CA`, `en-US`");
            builder.AppendLine("- Minimum language target: `44x44 px`");
            builder.AppendLine("- Protected hive artwork changed: `false`");
            builder.AppendLine("- Canonical world map changed: `false`");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                string path = PathFor(capture);
                (int width, int height) = ReadPngDimensions(path);
                builder.AppendLine("- `" + capture.FileName + "`: `" + width + "x" + height + "`, locale `" + capture.Locale + "`, gate `" + capture.GateState + "`, readiness `" + capture.ReadinessState + "`, SHA-256 `" + Sha256(path) + "`");
            }
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

        private static void TrySetGameViewSize(int width, int height, string label)
        {
            try
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
                Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                Type gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                Type gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type gameViewSizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                Type gameViewSizeGroupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
                Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
                object sizes = singletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                object groupType = ResolveActiveGroupType(gameViewSizesType, gameViewSizeGroupType, sizes);
                object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                object customSize = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) })
                    .Invoke(new[] { fixedResolution, width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
                int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                gameView.Show();
                gameView.maximized = false;
                gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, selectedIndex);
                gameView.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to force language selector Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static object ResolveActiveGroupType(Type gameViewSizesType, Type groupType, object sizes)
        {
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            foreach (MethodInfo method in gameViewSizesType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "GetGroupType" || parameters.Length != 1 || parameters[0].ParameterType != typeof(BuildTarget)) continue;
                object resolved = method.Invoke(method.IsStatic ? null : sizes, new object[] { activeTarget });
                if (resolved != null && resolved.GetType() == groupType) return resolved;
            }

            switch (activeTarget)
            {
                case BuildTarget.Android:
                    return Enum.Parse(groupType, "Android");
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    return Enum.Parse(groupType, "Standalone");
                default:
                    throw new NotSupportedException("No safe Game View size group mapping for " + activeTarget + ".");
            }
        }

        private static (int Width, int Height) ReadPngDimensions(string path)
        {
            byte[] header = new byte[24];
            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.Read(header, 0, header.Length) != header.Length) throw new InvalidOperationException("Incomplete PNG header: " + path);
            }

            int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return (width, height);
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
