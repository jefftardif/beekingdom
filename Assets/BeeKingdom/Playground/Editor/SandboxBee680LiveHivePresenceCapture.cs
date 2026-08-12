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
    public static class SandboxBee680LiveHivePresenceCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-055_BEE661_680_LiveHivePresence";
        private const string ManifestPath = OutputDirectory + "/BEE-680_LiveHivePresence_Manifest.md";
        private const string ContactSheetPath = OutputDirectory + "/BEE-679_BeeFamilyMotion_ContactSheet.png";
        private const string StateRequested = "BeeKingdom.Playground.Bee680LiveHivePresence.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee680LiveHivePresence.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee680LiveHivePresence.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Bee680LiveHivePresence.Index";

        private struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string HotspotId;
            public readonly Vector2 Pan;

            public CaptureSpec(string label, string fileName, int width, int height, string hotspotId, Vector2 pan)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                HotspotId = hotspotId;
                Pan = pan;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Desktop ruche habitee player-facing", "BEE-680_01_Desktop_InhabitedHive.png", 1280, 720, "honey_storage", Vector2.zero),
            new CaptureSpec("Portrait mobile habite navigable", "BEE-680_02_MobilePortrait_InhabitedHive.png", 390, 844, "wax_workshop", new Vector2(-210f, 80f)),
            new CaptureSpec("Defense occlusion guard", "BEE-670_03_Defense_OcclusionGuard.png", 1280, 720, "defense_growth", Vector2.zero),
            new CaptureSpec("Centre alliance non-claim", "BEE-677_04_Alliance_NonLiveAudit.png", 1280, 720, "alliance_future_hall", Vector2.zero),
            new CaptureSpec("Nurserie personnages visibles", "BEE-662_05_BeeFamily_Nursery.png", 1280, 720, "nursery_cluster", Vector2.zero)
        };

        static SandboxBee680LiveHivePresenceCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-680 Live Hive Presence")]
        public static void CaptureBee680LiveHivePresence()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            DeleteIfExists(ContactSheetPath);
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
            if (frames < 55) return;

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
                    if (frames < 140) return;
                    throw new InvalidOperationException("BEE-680 screenshot was not written: " + path);
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

                BuildContactSheet();
                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("BEE-680 live hive presence captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-680 live hive presence capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(capture.HotspotId);
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-680 Live Hive Presence Manifest");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("- Builder implementation: `Completed`");
            builder.AppendLine("- Gate verdict: `" + HiveViewProductUiPresenter.LivePresenceGate.Verdict + "`");
            builder.AppendLine("- BEE-681: `Blocked`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine("- Contact sheet: `" + ContactSheetPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Runtime Proof");
            builder.AppendLine();
            builder.AppendLine("- Bee agents: `" + HiveViewProductUiPresenter.GetLiveHiveBeeAgentCountForProof() + "`");
            builder.AppendLine("- Bee families visible: `" + HiveViewProductUiPresenter.VisibleBeeCatalog.VisibleFamilyCount + "`");
            builder.AppendLine("- Motion kinds: `" + string.Join(", ", HiveViewProductUiPresenter.GetLiveHiveBeeMotionKindsForProof()) + "`");
            builder.AppendLine("- Desktop density budget: `" + HiveViewProductUiPresenter.BeeDensityBudget.DesktopVisibleBees + "`");
            builder.AppendLine("- Mobile portrait density budget: `" + HiveViewProductUiPresenter.BeeDensityBudget.PortraitVisibleBees + "`");
            builder.AppendLine("- Occlusion click-through: `" + HiveViewProductUiPresenter.BeeOcclusionGuard.ClickThroughToHotspots + "`");
            builder.AppendLine("- Hotspot count preserved: `" + HiveViewProductUiPresenter.ReferenceHotspotCount + "`");
            builder.AppendLine("- Asset manifest: `C:/projets/beekingdomgame-master/Docs/Demos/BEE-673_BeeAssetManifest.md`");
            builder.AppendLine("- Animation handoff: `C:/projets/beekingdomgame-master/Docs/Demos/BEE-674_BeeAnimationHandoff.md`");
            builder.AppendLine();
            builder.AppendLine("## Non-Claims");
            builder.AppendLine();
            builder.AppendLine("- Presence is visual/local preview only.");
            builder.AppendLine("- No official population, collection, economy, progression, alliance, chat, ranking, server authority, persistence or synchronization is introduced.");
            return builder.ToString();
        }

        private static void BuildContactSheet()
        {
            Texture2D[] sources = new Texture2D[Captures.Length];
            try
            {
                for (int i = 0; i < Captures.Length; i++)
                {
                    sources[i] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!sources[i].LoadImage(File.ReadAllBytes(PathFor(Captures[i])))) throw new InvalidOperationException("Unable to load capture: " + PathFor(Captures[i]));
                }

                const int columns = 1;
                const int thumbWidth = 720;
                const int thumbHeight = 405;
                const int labelHeight = 34;
                const int padding = 14;
                int width = columns * thumbWidth + (columns + 1) * padding;
                int height = sources.Length * (thumbHeight + labelHeight + padding) + padding;
                var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Fill(sheet, new Color(0.045f, 0.030f, 0.016f, 1f));

                for (int i = 0; i < sources.Length; i++)
                {
                    int y = padding + i * (thumbHeight + labelHeight + padding);
                    BlitScaled(sources[i], sheet, padding, y, thumbWidth, thumbHeight);
                    FillRect(sheet, padding, y + thumbHeight, thumbWidth, labelHeight, new Color(0.94f, 0.62f, 0.14f, 1f));
                    DrawAsciiLabel(sheet, padding + 10, y + thumbHeight + 10, (i + 1).ToString("00") + " " + Captures[i].Label, new Color(0.06f, 0.035f, 0.014f, 1f));
                }

                File.WriteAllBytes(ContactSheetPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            finally
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != null) UnityEngine.Object.DestroyImmediate(sources[i]);
                }
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
                float v = 1f - (py + 0.5f) / height;
                for (int px = 0; px < width; px++)
                {
                    float u = (px + 0.5f) / width;
                    target.SetPixel(x + px, y + py, source.GetPixelBilinear(u, v));
                }
            }
        }

        private static void DrawAsciiLabel(Texture2D texture, int x, int y, string label, Color color)
        {
            for (int i = 0; i < label.Length; i++) DrawTinyGlyph(texture, x + i * 7, y, char.ToUpperInvariant(label[i]), color);
            texture.Apply();
        }

        private static void DrawTinyGlyph(Texture2D texture, int x, int y, char c, Color color)
        {
            int code = c == ' ' ? 0 : c;
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
                Debug.LogWarning("Unable to force BEE-680 Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
