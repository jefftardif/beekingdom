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
    public static class LivingHiveChatCaptureHarness
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdomgame-master/Docs/WorldMapCommunication/Evidence/LivingHiveChat";
        private const string ManifestPath = OutputDirectory + "/LivingHiveChat_CaptureManifest.md";
        private const string Requested = "BeeKingdom.Communication.Capture.Requested";
        private const string Frames = "BeeKingdom.Communication.Capture.Frames";
        private const string Captured = "BeeKingdom.Communication.Capture.Captured";
        private const string Index = "BeeKingdom.Communication.Capture.Index";
        private const string ConfiguredIndex = "BeeKingdom.Communication.Capture.ConfiguredIndex";

        private readonly struct CaptureSpec
        {
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public CaptureSpec(string fileName, int width, int height) { FileName = fileName; Width = width; Height = height; }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("ChatButton_MiniChat_Portrait_390x844.png", 390, 844),
            new CaptureSpec("ChatButton_MiniChat_Landscape_1600x900.png", 1600, 900)
        };

        static LivingHiveChatCaptureHarness()
        {
            if (!SessionState.GetBool(Requested, false)) return;
            Subscribe();
        }

        [MenuItem("Bee Kingdom/Communication/Capture LivingHive Mini-Chat Proofs")]
        public static void CaptureAndExit()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            SessionState.SetBool(Requested, true);
            SessionState.SetBool(Captured, false);
            SessionState.SetInt(Frames, 0);
            SessionState.SetInt(Index, 0);
            SessionState.SetInt(ConfiguredIndex, -1);
            Subscribe();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void Subscribe()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Requested, false) || state != PlayModeStateChange.EnteredPlayMode) return;
            ApplyCurrentState();
            SessionState.SetInt(Frames, 0);
            SessionState.SetBool(Captured, false);
        }

        private static void OnUpdate()
        {
            if (!SessionState.GetBool(Requested, false)) { EditorApplication.update -= OnUpdate; return; }
            if (!EditorApplication.isPlaying) return;
            ApplyCurrentState();
            int frames = SessionState.GetInt(Frames, 0) + 1;
            SessionState.SetInt(Frames, frames);
            if (frames < 70) return;
            try
            {
                CaptureSpec spec = Current();
                string path = PathFor(spec);
                if (!SessionState.GetBool(Captured, false))
                {
                    ScreenCapture.CaptureScreenshot(path);
                    SessionState.SetBool(Captured, true);
                    return;
                }
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (frames < 180) return;
                    throw new InvalidOperationException("Communication screenshot was not written: " + path);
                }
                ValidateDimensions(path, spec.Width, spec.Height);
                int index = SessionState.GetInt(Index, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(Index, index + 1);
                    SessionState.SetInt(Frames, 0);
                    SessionState.SetBool(Captured, false);
                    ApplyCurrentState();
                    return;
                }
                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                Finish(0, "LivingHive Mini-Chat proofs captured.");
            }
            catch (Exception exception) { Debug.LogError(exception); Finish(1, "LivingHive Mini-Chat capture failed."); }
        }

        private static void ApplyCurrentState()
        {
            int index = Mathf.Clamp(SessionState.GetInt(Index, 0), 0, Captures.Length - 1);
            CaptureSpec spec = Current();
            if (SessionState.GetInt(ConfiguredIndex, -1) != index)
            {
                TrySetGameViewSize(spec.Width, spec.Height, "Mini-Chat " + spec.Width + "x" + spec.Height);
                Screen.SetResolution(spec.Width, spec.Height, false);
                SessionState.SetInt(ConfiguredIndex, index);
            }
            HiveViewProductUiPresenter.PrepareMiniChatCaptureForProof(spec.Height > spec.Width);
        }

        private static void ValidateDimensions(string path, int width, int height)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            int actualWidth = texture.width;
            int actualHeight = texture.height;
            UnityEngine.Object.DestroyImmediate(texture);
            if (actualWidth != width || actualHeight != height) throw new InvalidOperationException("Unexpected dimensions " + actualWidth + "x" + actualHeight + " for " + path);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# LivingHive Mini-Chat - manifeste de captures");
            builder.AppendLine();
            builder.AppendLine("- Etat fournisseur: `NotConfigured` (honnête, aucun shell auth de production branché)");
            builder.AppendLine("- Faux message, conversation, statut ou badge: `aucun`");
            builder.AppendLine("- Portrait: bouton `Chat` du rail, mini-chat flottant `390x844`");
            builder.AppendLine("- Paysage: bouton `Chat` du rail, mini-chat flottant `1600x900`");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- `" + capture.FileName + "`: `" + capture.Width + "x" + capture.Height + "`");
            return builder.ToString();
        }

        private static CaptureSpec Current() => Captures[Mathf.Clamp(SessionState.GetInt(Index, 0), 0, Captures.Length - 1)];
        private static string PathFor(CaptureSpec capture) => OutputDirectory + "/" + capture.FileName;
        private static void DeleteIfExists(string path) { if (File.Exists(path)) File.Delete(path); }

        private static void Finish(int exitCode, string message)
        {
            SessionState.SetBool(Requested, false);
            EditorApplication.update -= OnUpdate;
            Debug.Log(message);
            EditorApplication.ExitPlaymode();
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
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
                Type scriptableSingletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
                object sizesInstance = scriptableSingletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                object activeGroupType = ResolveActiveGroupType(gameViewSizesType, gameViewSizeGroupType, sizesInstance);
                object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizesInstance, new[] { activeGroupType });
                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                object customSize = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) }).Invoke(new[] { fixedResolution, width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
                int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                gameView.Show();
                gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, selectedIndex);
                gameView.Repaint();
            }
            catch (Exception exception) { Debug.LogWarning("Unable to force " + label + ": " + exception.Message); }
        }

        private static object ResolveActiveGroupType(Type gameViewSizesType, Type gameViewSizeGroupType, object sizesInstance)
        {
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            foreach (MethodInfo method in gameViewSizesType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "GetGroupType" || parameters.Length != 1 || parameters[0].ParameterType != typeof(BuildTarget)) continue;
                try
                {
                    object resolved = method.Invoke(method.IsStatic ? null : sizesInstance, new object[] { activeTarget });
                    if (resolved != null && resolved.GetType() == gameViewSizeGroupType) return resolved;
                }
                catch (TargetInvocationException)
                {
                    break;
                }
            }

            string fallback;
            switch (activeTarget)
            {
                case BuildTarget.Android:
                    fallback = "Android";
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    fallback = "Standalone";
                    break;
                default:
                    throw new NotSupportedException("No safe Game View size group mapping for active build target " + activeTarget + ".");
            }

            return Enum.Parse(gameViewSizeGroupType, fallback);
        }
    }
}
