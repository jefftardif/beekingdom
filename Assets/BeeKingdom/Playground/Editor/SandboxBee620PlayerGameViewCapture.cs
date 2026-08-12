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
    public static class SandboxBee620PlayerGameViewCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-052_BEE620_PlayerGameView";
        private const string DesktopScreenshotPath = OutputDirectory + "/DEMO-052_BEE-620_PlayerGameView_PlayMode_Desktop.png";
        private const string PortraitScreenshotPath = OutputDirectory + "/DEMO-052_BEE-620_PlayerGameView_PlayMode_MobilePortrait.png";
        private const string ManifestPath = OutputDirectory + "/DEMO-052_BEE-620_PlayerGameView_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee620PlayerGameView.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee620PlayerGameView.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee620PlayerGameView.Captured";
        private const string StateCaptureIndex = "BeeKingdom.Playground.Bee620PlayerGameView.CaptureIndex";

        static SandboxBee620PlayerGameViewCapture()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-620 Player Game View")]
        public static void CaptureBee620PlayerGameView()
        {
            Directory.CreateDirectory(OutputDirectory);
            DeleteIfExists(DesktopScreenshotPath);
            DeleteIfExists(PortraitScreenshotPath);
            DeleteIfExists(ManifestPath);
            SessionState.SetBool(StateRequested, true);
            SessionState.SetBool(StateCaptured, false);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetInt(StateCaptureIndex, 0);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ApplyResolution(0);
                SessionState.SetInt(StateFrames, 0);
                SessionState.SetBool(StateCaptured, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.update += OnPlayModeUpdate;
            }
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 45)
            {
                return;
            }

            try
            {
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    ScreenCapture.CaptureScreenshot(CurrentScreenshotPath());
                    SessionState.SetBool(StateCaptured, true);
                    return;
                }

                string screenshotPath = CurrentScreenshotPath();
                if (!File.Exists(screenshotPath) || new FileInfo(screenshotPath).Length == 0)
                {
                    if (frames < 120)
                    {
                        return;
                    }

                    throw new InvalidOperationException("BEE-620 Game View screenshot was not written.");
                }

                FrameAnalysis analysis = Analyze(screenshotPath);
                if (!analysis.IsNonBlank)
                {
                    throw new InvalidOperationException("BEE-620 Game View screenshot is blank.");
                }

                int index = SessionState.GetInt(StateCaptureIndex, 0);
                if (index == 0)
                {
                    SessionState.SetInt(StateCaptureIndex, 1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    ApplyResolution(1);
                    return;
                }

                FrameAnalysis desktopAnalysis = Analyze(DesktopScreenshotPath);
                FrameAnalysis portraitAnalysis = Analyze(PortraitScreenshotPath);
                File.WriteAllText(ManifestPath, BuildManifest(desktopAnalysis, portraitAnalysis), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-052 BEE-620 player Game View captured: " + DesktopScreenshotPath + " and " + PortraitScreenshotPath);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-052 BEE-620 player Game View capture failed: " + exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static FrameAnalysis Analyze(string path)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = texture.GetPixels32();
            Color32 first = pixels.Length > 0 ? pixels[0] : default;
            int different = 0;
            int bright = 0;
            int sampled = 0;
            int step = Math.Max(1, pixels.Length / 8000);
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 pixel = pixels[i];
                int delta = Math.Abs(pixel.r - first.r) + Math.Abs(pixel.g - first.g) + Math.Abs(pixel.b - first.b);
                if (delta > 12) different++;
                if (pixel.r + pixel.g + pixel.b > 60) bright++;
                sampled++;
            }

            var analysis = new FrameAnalysis(texture.width, texture.height, sampled, sampled > 0 ? (double)different / sampled : 0d, sampled > 0 ? (double)bright / sampled : 0d);
            UnityEngine.Object.DestroyImmediate(texture);
            return analysis;
        }

        private static string BuildManifest(FrameAnalysis desktopAnalysis, FrameAnalysis portraitAnalysis)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-052 - BEE-620 Player Game View");
            builder.AppendLine();
            builder.AppendLine("Captures Play Mode normales de SandboxPlayground via ScreenCapture, incluant la couche OnGUI joueur.");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            builder.AppendLine("- Desktop PNG : `" + DesktopScreenshotPath + "`");
            builder.AppendLine("- Desktop resolution : `" + desktopAnalysis.Width + "x" + desktopAnalysis.Height + "`");
            builder.AppendLine("- Desktop nonBlank : `" + desktopAnalysis.IsNonBlank + "`");
            builder.AppendLine("- Desktop variation : `" + desktopAnalysis.VariationRatio.ToString("0.0000") + "`");
            builder.AppendLine("- Portrait PNG : `" + PortraitScreenshotPath + "`");
            builder.AppendLine("- Portrait resolution : `" + portraitAnalysis.Width + "x" + portraitAnalysis.Height + "`");
            builder.AppendLine("- Portrait nonBlank : `" + portraitAnalysis.IsNonBlank + "`");
            builder.AppendLine("- Portrait variation : `" + portraitAnalysis.VariationRatio.ToString("0.0000") + "`");
            builder.AppendLine();
            builder.AppendLine("## Couverture visible attendue");
            builder.AppendLine();
            builder.AppendLine("- Ruche runtime, HUD ressources OnGUI, navigation, panneau detail, cellule selectionnee, etats visuels et non-claims preview/local.");
            builder.AppendLine("- Aucun overlay QA, diagnostic, scorecard ou preuve ajoute par le captureur.");
            builder.AppendLine("- Aucune action officielle, economie live, progression officielle ou synchronisation serveur.");
            return builder.ToString();
        }

        private static void ApplyResolution(int index)
        {
            if (index == 0)
            {
                TrySetGameViewSize(1280, 720, "BEE-620 Desktop 1280x720");
                Screen.SetResolution(1280, 720, false);
                return;
            }

            TrySetGameViewSize(390, 844, "BEE-620 Mobile Portrait 390x844");
            Screen.SetResolution(390, 844, false);
        }

        private static string CurrentScreenshotPath()
        {
            return SessionState.GetInt(StateCaptureIndex, 0) == 0 ? DesktopScreenshotPath : PortraitScreenshotPath;
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
                PropertyInfo instanceProperty = scriptableSingletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                object sizesInstance = instanceProperty.GetValue(null);
                object androidGroupType = Enum.Parse(gameViewSizeGroupType, "Android");
                object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizesInstance, new[] { androidGroupType });
                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                ConstructorInfo constructor = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
                object customSize = constructor.Invoke(new[] { fixedResolution, width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
                int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                gameView.Show();
                PropertyInfo selectedSizeIndex = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                selectedSizeIndex?.SetValue(gameView, selectedIndex);
                gameView.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to force BEE-620 Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private readonly struct FrameAnalysis
        {
            public FrameAnalysis(int width, int height, int sampledPixels, double variationRatio, double visibleRatio)
            {
                Width = width;
                Height = height;
                SampledPixels = sampledPixels;
                VariationRatio = variationRatio;
                VisibleRatio = visibleRatio;
            }

            public int Width { get; }
            public int Height { get; }
            public int SampledPixels { get; }
            public double VariationRatio { get; }
            public double VisibleRatio { get; }
            public bool IsNonBlank => VariationRatio > 0.01d && VisibleRatio > 0.05d;
        }
    }
}
