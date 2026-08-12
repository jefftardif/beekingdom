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
    public static class SandboxBee640ReferenceProductionizationCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-053_BEE621_640_ReferenceProductionization";
        private const string ContactSheetPath = OutputDirectory + "/BEE-638_ContactSheet.png";
        private const string ManifestPath = OutputDirectory + "/BEE-638_BEE-640_ReferenceProductionization_Manifest.md";
        private const string GateBoardPath = OutputDirectory + "/BEE-640_GateBoard.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee640ReferenceProductionization.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee640ReferenceProductionization.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee640ReferenceProductionization.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Bee640ReferenceProductionization.Index";

        private struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string HotspotId;
            public readonly Vector2 Pan;
            public readonly bool UseArtPoint;
            public readonly Vector2 ArtPoint;

            public CaptureSpec(string label, string fileName, int width, int height, string hotspotId, Vector2 pan)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                HotspotId = hotspotId;
                Pan = pan;
                UseArtPoint = false;
                ArtPoint = Vector2.zero;
            }

            public CaptureSpec(string label, string fileName, int width, int height, Vector2 artPoint)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                HotspotId = string.Empty;
                Pan = Vector2.zero;
                UseArtPoint = true;
                ArtPoint = artPoint;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Overview desktop", "BEE-638_01_OverviewDesktop.png", 1280, 720, "honey_storage", Vector2.zero),
            new CaptureSpec("Reserve miel", "BEE-638_02_ReserveMiel.png", 1280, 720, "honey_storage", Vector2.zero),
            new CaptureSpec("Poste garde / server required", "BEE-638_03_PosteGarde_ServerRequired.png", 1280, 720, "guard_post", Vector2.zero),
            new CaptureSpec("Atelier cire", "BEE-638_04_AtelierCire.png", 1280, 720, "wax_workshop", Vector2.zero),
            new CaptureSpec("Tap drag / bord polygonal", "BEE-638_05_TapDrag_PolygonBorder.png", 1280, 720, new Vector2(700f, 91f)),
            new CaptureSpec("Portrait mobile panned", "BEE-638_06_MobilePortrait_Panned.png", 390, 844, "wax_workshop", new Vector2(-210f, 80f)),
            new CaptureSpec("Entree MMO preview", "BEE-638_07_MmoEntryPreview.png", 1280, 720, "alliance_future_hall", Vector2.zero),
            new CaptureSpec("Profil local", "BEE-638_08_LocalProfile.png", 1280, 720, "administration_core", Vector2.zero),
            new CaptureSpec("Cinquieme hotspot lisible", "BEE-638_09_FifthHotspot_Nurserie.png", 1280, 720, "nursery_cluster", Vector2.zero),
            new CaptureSpec("Gate BEE-640 player surface", "BEE-640_10_GatePlayerSurface.png", 1280, 720, "alliance_future_hall", Vector2.zero)
        };

        static SandboxBee640ReferenceProductionizationCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-640 Reference Productionization Proof")]
        public static void CaptureBee640ReferenceProductionizationProof()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures)
            {
                DeleteIfExists(PathFor(capture));
            }

            DeleteIfExists(ContactSheetPath);
            DeleteIfExists(ManifestPath);
            DeleteIfExists(GateBoardPath);
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
            if (!SessionState.GetBool(StateRequested, false)) return;
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            ApplyCurrentProofState();
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCaptured, false);
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

            ApplyCurrentProofState();
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 45) return;

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
                    if (frames < 120) return;
                    throw new InvalidOperationException("BEE-640 productionization screenshot was not written: " + path);
                }

                int index = SessionState.GetInt(StateIndex, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    ApplyCurrentProofState();
                    return;
                }

                BuildContactSheet();
                File.WriteAllText(GateBoardPath, BuildGateBoard(), Encoding.UTF8);
                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("BEE-640 reference productionization proof captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-640 reference productionization proof failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentProofState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            if (capture.UseArtPoint)
            {
                HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(capture.ArtPoint.x, capture.ArtPoint.y);
                return;
            }

            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(capture.HotspotId);
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-640 Reference Productionization Manifest");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("Completed for Demo evidence. Gate verdict remains a BEE-640 gate, not a BEE-641 opening.");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            }

            builder.AppendLine("- Contact sheet: `" + ContactSheetPath + "`");
            builder.AppendLine("- Gate board: `" + GateBoardPath + "`");
            builder.AppendLine();
            builder.AppendLine("## DEMO-052 Baseline Comparison");
            builder.AppendLine();
            builder.AppendLine("- Baseline: `C:/projets/beekingdom/prompt_demo/rapports/DEMO-052_BEE620_PlayerGameView`");
            builder.AppendLine("- Difference: DEMO-053 adds 14 polygon zones, five selected hotspots, MMO entry preview, local profile proof, tap/border proof and gate board.");
            builder.AppendLine("- Continuity: same SandboxPlayground Play Mode surface, player-facing Game View, no QA/debug overlay.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Evidence");
            builder.AppendLine();
            builder.AppendLine("- Official polygon zones: `" + HiveViewProductUiPresenter.ReferenceHotspotCount + "`");
            builder.AppendLine("- Zone reference: `C:/projets/beekingdom/zones.png`");
            builder.AppendLine("- Hotspot ids: `" + string.Join(", ", HiveViewProductUiPresenter.GetReferenceHotspotIdsForProof()) + "`");
            builder.AppendLine("- Gate verdict: `" + HiveViewProductUiPresenter.ReferenceMmoEntryGate.Verdict + "`");
            builder.AppendLine("- BEE-641 remains blocked.");
            builder.AppendLine();
            builder.AppendLine("## Non-Claims");
            builder.AppendLine();
            builder.AppendLine("- LOCAL PREVIEW, Apercu local, Serveur futur and Non synchronise remain player-facing.");
            builder.AppendLine("- No server authority, account, live economy, persistent alliance, chat, ranking, war or synchronization is introduced.");
            return builder.ToString();
        }

        private static string BuildGateBoard()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-640 Gate Board");
            builder.AppendLine();
            builder.AppendLine("## Verdict");
            builder.AppendLine();
            builder.AppendLine("- Framework verdict: `" + HiveViewProductUiPresenter.ReferenceMmoEntryGate.Verdict + "`");
            builder.AppendLine("- Demo gate conclusion: `pass for the local player-facing evidence pack`.");
            builder.AppendLine("- BEE-641: `blocked`.");
            builder.AppendLine();
            builder.AppendLine("## Covered Gate Items");
            builder.AppendLine();
            foreach (string verdict in HiveViewProductUiPresenter.ReferenceMmoEntryGate.PassedVerdicts)
            {
                builder.AppendLine("- " + verdict);
            }

            builder.AppendLine();
            builder.AppendLine("## Explicit Non-Claims");
            builder.AppendLine();
            builder.AppendLine("- No live MMO entry.");
            builder.AppendLine("- No account creation.");
            builder.AppendLine("- No official alliance, chat, ranking, war, economy, resource persistence or synchronization.");
            builder.AppendLine("- Any future authoritative behavior remains Bee Server scope.");
            return builder.ToString();
        }

        private static void BuildContactSheet()
        {
            Texture2D[] sources = new Texture2D[Captures.Length];
            try
            {
                for (int i = 0; i < Captures.Length; i++)
                {
                    byte[] bytes = File.ReadAllBytes(PathFor(Captures[i]));
                    sources[i] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!sources[i].LoadImage(bytes)) throw new InvalidOperationException("Unable to load capture for contact sheet: " + PathFor(Captures[i]));
                }

                const int columns = 2;
                const int thumbWidth = 640;
                const int thumbHeight = 360;
                const int labelHeight = 44;
                const int padding = 18;
                int rows = Mathf.CeilToInt(Captures.Length / (float)columns);
                int width = columns * thumbWidth + (columns + 1) * padding;
                int height = rows * (thumbHeight + labelHeight) + (rows + 1) * padding;
                var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color background = new Color(0.05f, 0.035f, 0.02f, 1f);
                Color label = new Color(1f, 0.86f, 0.45f, 1f);
                Color text = new Color(0.08f, 0.045f, 0.018f, 1f);
                Fill(sheet, background);

                for (int i = 0; i < sources.Length; i++)
                {
                    int column = i % columns;
                    int row = i / columns;
                    int x = padding + column * (thumbWidth + padding);
                    int y = padding + row * (thumbHeight + labelHeight + padding);
                    BlitScaled(sources[i], sheet, x, y, thumbWidth, thumbHeight);
                    FillRect(sheet, x, y + thumbHeight, thumbWidth, labelHeight, label);
                    DrawAsciiLabel(sheet, x + 12, y + thumbHeight + 12, (i + 1).ToString("00") + " " + Captures[i].Label, text);
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
            for (int i = 0; i < label.Length; i++)
            {
                DrawTinyGlyph(texture, x + i * 8, y, char.ToUpperInvariant(label[i]), color);
            }

            texture.Apply();
        }

        private static void DrawTinyGlyph(Texture2D texture, int x, int y, char c, Color color)
        {
            int code = c == ' ' ? 0 : c;
            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    bool on = c == ' ' ? false : ((code + row * 13 + col * 7) % 5) < 2 || row == 0 || row == 6;
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
                Debug.LogWarning("Unable to force BEE-640 Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
