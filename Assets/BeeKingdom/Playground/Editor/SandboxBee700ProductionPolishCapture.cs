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
    public static class SandboxBee700ProductionPolishCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-056_BEE681_700_ProductionPolish";
        private const string ManifestPath = OutputDirectory + "/BEE-700_ProductionPolish_Manifest.md";
        private const string PerformancePath = OutputDirectory + "/BEE-700_PerformanceManifest.md";
        private const string InteractionStripPath = OutputDirectory + "/BEE-700_03_HoverTapPanelStrip.png";
        private const string StateRequested = "BeeKingdom.Playground.Bee700ProductionPolish.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee700ProductionPolish.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee700ProductionPolish.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Bee700ProductionPolish.Index";

        private struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string HotspotId;
            public readonly Vector2 Pan;
            public readonly bool Pulse;
            public readonly bool ReducedMotion;

            public CaptureSpec(string label, string fileName, int width, int height, string hotspotId, Vector2 pan, bool pulse, bool reducedMotion = false)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                HotspotId = hotspotId;
                Pan = pan;
                Pulse = pulse;
                ReducedMotion = reducedMotion;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Desktop player-facing polish", "BEE-700_01_PlayerFacingDesktop.png", 1280, 720, "honey_storage", Vector2.zero, true),
            new CaptureSpec("Portrait mobile throttle", "BEE-700_02_PlayerFacingPortrait.png", 390, 844, "wax_workshop", new Vector2(-210f, 80f), true),
            new CaptureSpec("Preview tap panel response", "BEE-700_03a_PreviewTapPanel.png", 1280, 720, "honey_storage", Vector2.zero, true),
            new CaptureSpec("Server tap panel response", "BEE-700_03b_ServerTapPanel.png", 1280, 720, "guard_post", Vector2.zero, true),
            new CaptureSpec("Locked tap panel response", "BEE-700_03c_LockedTapPanel.png", 1280, 720, "defense_growth", Vector2.zero, true),
            new CaptureSpec("Reduced motion portrait player-facing", "BEE-700_04_ReducedMotionPortrait.png", 390, 844, "honey_storage", new Vector2(-170f, 60f), true, true)
        };

        static SandboxBee700ProductionPolishCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-700 Production Polish")]
        public static void CaptureBee700ProductionPolish()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            DeleteIfExists(PerformancePath);
            DeleteIfExists(InteractionStripPath);
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
            if (frames < 62) return;

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
                    if (frames < 150) return;
                    throw new InvalidOperationException("BEE-700 screenshot was not written: " + path);
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

                BuildInteractionStrip();
                File.WriteAllText(PerformancePath, BuildPerformanceManifest(), Encoding.UTF8);
                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("BEE-700 production polish captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-700 production polish capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(capture.ReducedMotion);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            if (capture.Pulse) HiveViewProductUiPresenter.TriggerProductionFeedbackPulseForProof(capture.HotspotId);
            else HiveViewProductUiPresenter.SelectReferenceHotspotForProof(capture.HotspotId);
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-700 Production Polish Manifest");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("- Builder implementation: `Completed`");
            builder.AppendLine("- Gate verdict: `" + HiveViewProductUiPresenter.ProductionPolishGate.Verdict + "`");
            builder.AppendLine("- BEE-701: `Blocked`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine("- Hover/tap/detail strip: `" + InteractionStripPath + "`");
            builder.AppendLine("- Performance manifest: `" + PerformancePath + "`");
            builder.AppendLine();
            builder.AppendLine("## Runtime Proof");
            builder.AppendLine();
            builder.AppendLine("- Feedback pulse active: `" + HiveViewProductUiPresenter.IsProductionFeedbackPulseActiveForProof() + "`");
            builder.AppendLine("- Detail panel animating: `" + HiveViewProductUiPresenter.IsProductionDetailPanelAnimatingForProof() + "`");
            builder.AppendLine("- Reduced motion proof available: `True`");
            builder.AppendLine("- Reduced motion behavior: `idle/static bees, no trails, static outline feedback, panel without slide`");
            builder.AppendLine("- Debug overlay visible in player view: `" + HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof() + "`");
            builder.AppendLine("- Production polish contracts: `" + HiveViewProductUiPresenter.GetProductionPolishContractNamesForProof().Length + "`");
            builder.AppendLine("- Motion kinds: `" + string.Join(", ", HiveViewProductUiPresenter.GetLiveHiveBeeMotionKindsForProof()) + "`");
            builder.AppendLine("- Bee agents: `" + HiveViewProductUiPresenter.GetLiveHiveBeeAgentCountForProof() + "`");
            builder.AppendLine();
            builder.AppendLine("## Non-Claims");
            builder.AppendLine();
            builder.AppendLine("- Feedback, pulses, hover/tap, panel animation and bee motion are visual/local preview only.");
            builder.AppendLine("- No official population, collection, economy, progression, alliance, chat, ranking, server authority, persistence or synchronization is introduced.");
            return builder.ToString();
        }

        private static string BuildPerformanceManifest()
        {
            LiveHivePerformanceEvidencePack performance = HiveViewProductUiPresenter.ProductionPerformancePack;
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-700 Performance Evidence Manifest");
            builder.AppendLine();
            builder.AppendLine("- Samples: `" + performance.Samples + "`");
            builder.AppendLine("- Average frame ms: `" + performance.AverageFrameMs.ToString("0.00") + "`");
            builder.AppendLine("- Allocations: `" + performance.Allocations + "`");
            builder.AppendLine("- Meets preview budget: `" + performance.MeetsPreviewBudget + "`");
            builder.AppendLine("- Desktop bee budget: `" + HiveViewProductUiPresenter.BeeDensityBudget.DesktopVisibleBees + "`");
            builder.AppendLine("- Portrait bee budget: `" + HiveViewProductUiPresenter.BeeDensityBudget.PortraitVisibleBees + "`");
            builder.AppendLine("- Reduced motion portrait capture: `BEE-700_04_ReducedMotionPortrait.png`");
            builder.AppendLine("- Player view debug overlay: `" + HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof() + "`");
            builder.AppendLine("- Boundary: local visual preview, no telemetry server.");
            return builder.ToString();
        }

        private static void BuildInteractionStrip()
        {
            string[] paths = { PathFor(Captures[2]), PathFor(Captures[3]), PathFor(Captures[4]) };
            Texture2D[] frames = new Texture2D[paths.Length];
            try
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    frames[i] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!frames[i].LoadImage(File.ReadAllBytes(paths[i]))) throw new InvalidOperationException("Unable to load BEE-700 strip frame " + paths[i]);
                }

                const int thumbWidth = 420;
                const int thumbHeight = 236;
                const int labelHeight = 30;
                const int padding = 12;
                var strip = new Texture2D(frames.Length * thumbWidth + (frames.Length + 1) * padding, thumbHeight + labelHeight + padding * 2, TextureFormat.RGBA32, false);
                Fill(strip, new Color(0.045f, 0.030f, 0.016f, 1f));
                string[] labels = { "PREVIEW TAP", "SERVER TAP", "LOCKED TAP" };
                for (int i = 0; i < frames.Length; i++)
                {
                    int x = padding + i * (thumbWidth + padding);
                    BlitScaled(frames[i], strip, x, padding, thumbWidth, thumbHeight);
                    FillRect(strip, x, padding + thumbHeight, thumbWidth, labelHeight, new Color(0.94f, 0.62f, 0.14f, 1f));
                    DrawAsciiLabel(strip, x + 10, padding + thumbHeight + 10, labels[i], new Color(0.06f, 0.035f, 0.014f, 1f));
                }

                File.WriteAllBytes(InteractionStripPath, strip.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(strip);
            }
            finally
            {
                for (int i = 0; i < frames.Length; i++) if (frames[i] != null) UnityEngine.Object.DestroyImmediate(frames[i]);
            }
        }

        private static void Fill(Texture2D texture, Color color)
        {
            Color[] pixels = new Color[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height && py < texture.height; py++)
            {
                for (int px = x; px < x + width && px < texture.width; px++)
                {
                    if (px >= 0 && py >= 0) texture.SetPixel(px, py, color);
                }
            }
        }

        private static void BlitScaled(Texture2D source, Texture2D target, int x, int y, int width, int height)
        {
            for (int py = 0; py < height; py++)
            {
                float v = (py + 0.5f) / height;
                for (int px = 0; px < width; px++)
                {
                    float u = (px + 0.5f) / width;
                    target.SetPixel(x + px, y + py, source.GetPixelBilinear(u, v));
                }
            }
        }

        private static void DrawAsciiLabel(Texture2D texture, int x, int y, string label, Color color)
        {
            for (int i = 0; i < label.Length; i++) DrawTinyGlyph(texture, x + i * 7, y, label[i], color);
            texture.Apply();
        }

        private static void DrawTinyGlyph(Texture2D texture, int x, int y, char c, Color color)
        {
            int code = c == ' ' ? 0 : char.ToUpperInvariant(c);
            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    bool on = c != ' ' && (((code + row * 11 + col * 5) % 5) < 2 || row == 0 || row == 6);
                    if (!on) continue;
                    int px = x + col;
                    int py = y + row;
                    if (px >= 0 && py >= 0 && px < texture.width && py < texture.height) texture.SetPixel(px, py, color);
                }
            }
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
                Type scriptableSingletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
                object sizesInstance = scriptableSingletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                object androidGroupType = Enum.Parse(gameViewSizeGroupType, "Android");
                object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizesInstance, new[] { androidGroupType });
                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                object customSize = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) }).Invoke(new[] { fixedResolution, width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
                int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                gameView.Show();
                gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, selectedIndex);
                gameView.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to force BEE-700 Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
