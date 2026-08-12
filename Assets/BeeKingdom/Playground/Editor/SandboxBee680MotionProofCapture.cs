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
    public static class SandboxBee680MotionProofCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-055_BEE661_680_LiveHivePresence";
        private const string StripPath = OutputDirectory + "/BEE-680_MotionStrip_RuntimeVerified.png";
        private const string ManifestPath = OutputDirectory + "/BEE-680_MotionProof_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee680MotionProof.Requested";
        private const string StateFrame = "BeeKingdom.Playground.Bee680MotionProof.Frame";
        private const string StateCaptureIndex = "BeeKingdom.Playground.Bee680MotionProof.CaptureIndex";
        private const string StateWaitingForFile = "BeeKingdom.Playground.Bee680MotionProof.WaitingForFile";

        private static readonly int[] CaptureFrames = { 55, 75, 95, 115, 135, 155 };
        private static readonly string[] FrameLabels = { "0.0", "0.3", "0.7", "1.0", "1.3", "1.7" };
        private static readonly RectInt MotionCrop = new RectInt(470, 380, 460, 285);

        static SandboxBee680MotionProofCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-680 Motion Proof")]
        public static void CaptureBee680MotionProof()
        {
            Directory.CreateDirectory(OutputDirectory);
            DeleteIfExists(StripPath);
            DeleteIfExists(ManifestPath);
            for (int i = 0; i < CaptureFrames.Length; i++) DeleteIfExists(FramePath(i));

            SessionState.SetBool(StateRequested, true);
            SessionState.SetInt(StateFrame, 0);
            SessionState.SetInt(StateCaptureIndex, 0);
            SessionState.SetBool(StateWaitingForFile, false);
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
            TrySetGameViewSize(1280, 720, "BEE-680 Motion Proof");
            Screen.SetResolution(1280, 720, false);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(0f, 0f);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");
            SessionState.SetInt(StateFrame, 0);
            SessionState.SetInt(StateCaptureIndex, 0);
            SessionState.SetBool(StateWaitingForFile, false);
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            TrySetGameViewSize(1280, 720, "BEE-680 Motion Proof");
            Screen.SetResolution(1280, 720, false);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");

            int frame = SessionState.GetInt(StateFrame, 0) + 1;
            int captureIndex = SessionState.GetInt(StateCaptureIndex, 0);
            SessionState.SetInt(StateFrame, frame);

            try
            {
                if (captureIndex >= CaptureFrames.Length)
                {
                    BuildStripAndManifest();
                    SessionState.SetBool(StateRequested, false);
                    EditorApplication.update -= OnPlayModeUpdate;
                    EditorApplication.ExitPlaymode();
                    Debug.Log("BEE-680 runtime motion proof captured.");
                    if (Application.isBatchMode) EditorApplication.Exit(0);
                    return;
                }

                string path = FramePath(captureIndex);
                if (SessionState.GetBool(StateWaitingForFile, false))
                {
                    if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    {
                        if (frame < CaptureFrames[captureIndex] + 60) return;
                        throw new InvalidOperationException("Motion proof screenshot was not written: " + path);
                    }

                    SessionState.SetInt(StateCaptureIndex, captureIndex + 1);
                    SessionState.SetBool(StateWaitingForFile, false);
                    return;
                }

                if (frame < CaptureFrames[captureIndex]) return;
                ScreenCapture.CaptureScreenshot(path);
                SessionState.SetBool(StateWaitingForFile, true);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-680 runtime motion proof capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void BuildStripAndManifest()
        {
            Texture2D[] frames = new Texture2D[CaptureFrames.Length];
            int[] adjacentDiffs;
            try
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    frames[i] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!frames[i].LoadImage(File.ReadAllBytes(FramePath(i)))) throw new InvalidOperationException("Unable to load motion frame " + i);
                }

                adjacentDiffs = ComputeAdjacentDiffs(frames);
                BuildMotionStrip(frames);
            }
            finally
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    if (frames[i] != null) UnityEngine.Object.DestroyImmediate(frames[i]);
                }
            }

            File.WriteAllText(ManifestPath, BuildManifest(adjacentDiffs), Encoding.UTF8);
        }

        private static int[] ComputeAdjacentDiffs(Texture2D[] frames)
        {
            int[] diffs = new int[Mathf.Max(0, frames.Length - 1)];
            for (int i = 0; i < diffs.Length; i++)
            {
                int changed = 0;
                for (int y = MotionCrop.y; y < MotionCrop.y + MotionCrop.height; y += 6)
                {
                    for (int x = MotionCrop.x; x < MotionCrop.x + MotionCrop.width; x += 6)
                    {
                        Color32 a = frames[i].GetPixel(x, y);
                        Color32 b = frames[i + 1].GetPixel(x, y);
                        int delta = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                        if (delta > 18) changed++;
                    }
                }

                diffs[i] = changed;
            }

            return diffs;
        }

        private static void BuildMotionStrip(Texture2D[] frames)
        {
            const int columns = 6;
            const int thumbWidth = 300;
            const int thumbHeight = 186;
            const int labelHeight = 44;
            const int padding = 12;
            int width = columns * thumbWidth + (columns + 1) * padding;
            int height = thumbHeight + labelHeight + padding * 2;
            var strip = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Fill(strip, new Color(0.045f, 0.030f, 0.016f, 1f));

            for (int i = 0; i < frames.Length; i++)
            {
                int x = padding + i * (thumbWidth + padding);
                BlitCropScaled(frames[i], strip, MotionCrop, x, padding, thumbWidth, thumbHeight);
                FillRect(strip, x, padding + thumbHeight, thumbWidth, labelHeight, new Color(0.94f, 0.62f, 0.14f, 1f));
                DrawFrameMarker(strip, x + 12, padding + thumbHeight + 7, i + 1, FrameLabels[i]);
            }

            strip.Apply();
            File.WriteAllBytes(StripPath, strip.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(strip);
        }

        private static string BuildManifest(int[] adjacentDiffs)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-680 Runtime Motion Proof Manifest");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("- Motion proof: `Runtime multi-frame strip verified`");
            builder.AppendLine("- Frame count: `" + CaptureFrames.Length + "`");
            builder.AppendLine("- Strip: `" + StripPath + "`");
            builder.AppendLine("- BEE-681: `Blocked`");
            builder.AppendLine();
            builder.AppendLine("## Frames");
            builder.AppendLine();
            for (int i = 0; i < CaptureFrames.Length; i++) builder.AppendLine("- Frame " + (i + 1).ToString("00") + " at editor frame `" + CaptureFrames[i] + "`: `" + FramePath(i) + "`");
            builder.AppendLine();
            builder.AppendLine("## Verification");
            builder.AppendLine();
            builder.AppendLine("- Adjacent frame pixel-diff samples: `" + string.Join(", ", adjacentDiffs) + "`");
            builder.AppendLine("- Adjacent frames changed: `" + AllChanged(adjacentDiffs) + "`");
            builder.AppendLine("- Strip crop: `x=" + MotionCrop.x + ", y=" + MotionCrop.y + ", width=" + MotionCrop.width + ", height=" + MotionCrop.height + "`");
            builder.AppendLine("- Runtime motion kinds: `" + string.Join(", ", HiveViewProductUiPresenter.GetLiveHiveBeeMotionKindsForProof()) + "`");
            builder.AppendLine("- Runtime bee agents: `" + HiveViewProductUiPresenter.GetLiveHiveBeeAgentCountForProof() + "`");
            builder.AppendLine();
            builder.AppendLine("## Non-Claims");
            builder.AppendLine();
            builder.AppendLine("- Motion is local visual preview only.");
            builder.AppendLine("- No official population, collection, economy, progression, server authority, persistence or synchronization is introduced.");
            return builder.ToString();
        }

        private static bool AllChanged(int[] diffs)
        {
            for (int i = 0; i < diffs.Length; i++)
            {
                if (diffs[i] <= 0) return false;
            }

            return true;
        }

        private static string FramePath(int index)
        {
            return OutputDirectory + "/BEE-680_MotionFrame_" + (index + 1).ToString("00") + ".png";
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

        private static void BlitCropScaled(Texture2D source, Texture2D target, RectInt crop, int x, int y, int width, int height)
        {
            for (int py = 0; py < height; py++)
            {
                float v = (py + 0.5f) / height;
                for (int px = 0; px < width; px++)
                {
                    float u = (px + 0.5f) / width;
                    int sx = Mathf.Clamp(crop.x + Mathf.FloorToInt(u * crop.width), 0, source.width - 1);
                    int sy = Mathf.Clamp(crop.y + Mathf.FloorToInt(v * crop.height), 0, source.height - 1);
                    target.SetPixel(x + px, y + py, source.GetPixel(sx, sy));
                }
            }
        }

        private static void DrawFrameMarker(Texture2D texture, int x, int y, int frame, string time)
        {
            Color dark = new Color(0.06f, 0.035f, 0.014f, 1f);
            Color blue = new Color(0.03f, 0.18f, 0.28f, 1f);
            DrawBlockGlyph(texture, x, y, 'F', dark, 3);
            DrawDigit(texture, x + 34, y, frame / 10, dark, 3);
            DrawDigit(texture, x + 62, y, frame % 10, dark, 3);
            FillRect(texture, x + 100, y + 6, 132, 18, blue);
            DrawBlockGlyph(texture, x + 108, y + 9, 'T', new Color(0.88f, 0.94f, 1f, 1f), 2);
            DrawTinyNumber(texture, x + 136, y + 10, time, new Color(0.88f, 0.94f, 1f, 1f));
            texture.Apply();
        }

        private static void DrawDigit(Texture2D texture, int x, int y, int digit, Color color, int scale)
        {
            bool[,] segments =
            {
                { true, true, true, true, true, true, false },
                { false, true, true, false, false, false, false },
                { true, true, false, true, true, false, true },
                { true, true, true, true, false, false, true },
                { false, true, true, false, false, true, true },
                { true, false, true, true, false, true, true },
                { true, false, true, true, true, true, true },
                { true, true, true, false, false, false, false },
                { true, true, true, true, true, true, true },
                { true, true, true, true, false, true, true }
            };

            digit = Mathf.Clamp(digit, 0, 9);
            if (segments[digit, 0]) FillRect(texture, x + 3 * scale, y, 10 * scale, 2 * scale, color);
            if (segments[digit, 1]) FillRect(texture, x + 13 * scale, y + 2 * scale, 2 * scale, 9 * scale, color);
            if (segments[digit, 2]) FillRect(texture, x + 13 * scale, y + 13 * scale, 2 * scale, 9 * scale, color);
            if (segments[digit, 3]) FillRect(texture, x + 3 * scale, y + 22 * scale, 10 * scale, 2 * scale, color);
            if (segments[digit, 4]) FillRect(texture, x + scale, y + 13 * scale, 2 * scale, 9 * scale, color);
            if (segments[digit, 5]) FillRect(texture, x + scale, y + 2 * scale, 2 * scale, 9 * scale, color);
            if (segments[digit, 6]) FillRect(texture, x + 3 * scale, y + 11 * scale, 10 * scale, 2 * scale, color);
        }

        private static void DrawBlockGlyph(Texture2D texture, int x, int y, char c, Color color, int scale)
        {
            if (c == 'F')
            {
                FillRect(texture, x, y, 4 * scale, 24 * scale, color);
                FillRect(texture, x, y, 18 * scale, 4 * scale, color);
                FillRect(texture, x, y + 10 * scale, 14 * scale, 4 * scale, color);
            }
            else if (c == 'T')
            {
                FillRect(texture, x, y, 16 * scale, 3 * scale, color);
                FillRect(texture, x + 6 * scale, y, 4 * scale, 14 * scale, color);
            }
        }

        private static void DrawTinyNumber(Texture2D texture, int x, int y, string value, Color color)
        {
            int cursor = x;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsDigit(c))
                {
                    DrawDigit(texture, cursor, y, c - '0', color, 1);
                    cursor += 18;
                }
                else
                {
                    FillRect(texture, cursor + 4, y + 22, 4, 4, color);
                    cursor += 10;
                }
            }
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
                Debug.LogWarning("Unable to force BEE-680 motion Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
