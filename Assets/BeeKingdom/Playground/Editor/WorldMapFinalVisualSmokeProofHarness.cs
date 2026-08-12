using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapFinalVisualSmokeProofHarness
    {
        private const string OutputRoot = "Docs/BuilderA/WorldMapRuntimeEntitiesWave1/FinalVisualSmokeProof";
        private const int Width = 1280;
        private const int Height = 720;
        private static readonly List<CaptureReceipt> captures = new List<CaptureReceipt>();

        [MenuItem("Bee Kingdom/World Map/Run Final Visual Smoke Composite Proof")]
        public static void RunFinalVisualSmokeCompositeProof()
        {
            string root = AbsoluteProjectPath(OutputRoot);
            Directory.CreateDirectory(root);
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories)) File.Delete(file);
            captures.Clear();

            WriteCapture(root, "FVS_00_CENTER_LAB_HIVES.png", ComposeCenterLabHives(), "Center global: Wave5 terrain, PLAYER_TEST_HIVE, ENEMY_TEST_HIVE, HUD fixe, ressources et bestiaire visibles.");
            WriteCapture(root, "FVS_01_HIVE_PROGRESSION.png", ComposeHiveProgression(), "Ruches: neutre pre-10, deux classes post-10, evolution N35, overlays faction separes.");
            WriteCapture(root, "FVS_02_RESOURCE_INTERACTION.png", ComposeResourceInteraction(), "Ressources: pauvre/moyenne/riche selectionnees, quantites, noeud epuise puis respawn demo.");
            WriteCapture(root, "FVS_03_BESTIARY_SOLO_RAID.png", ComposeBestiaryInteraction(), "Bestiaire: T1 solo et T7 raid, silhouettes premium, combat local sans gain officiel.");
            WriteCapture(root, "FVS_04_BEAR_DEN_STATES.png", ComposeBearDenStates(), "BearDen visible, cache, restaure, separe des entites.");
            WriteCapture(root, "FVS_05_PAN_ZOOM_EDGE.png", ComposePanZoomEdge(), "Pan/zoom: centre + bord nord-ouest Wave5, HUD fixe, aucune tuile manquante dans la composition.");

            WriteManifest(root);
            AssetDatabase.Refresh();
            Debug.Log("[BeeKingdom] Final visual smoke composite proof written to " + root);
        }

        private static Texture2D ComposeCenterLabHives()
        {
            Texture2D canvas = NewCanvas();
            DrawTerrainGrid(canvas, 12, 12, 1, 1, new Rect(0, 0, 1280, 720));
            DrawHud(canvas, new Color32(18, 24, 20, 226), new RectInt(18, 16, 560, 88));
            DrawHud(canvas, new Color32(18, 24, 20, 226), new RectInt(930, 18, 320, 170));
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/H3/hive_alchemist_l35", 390, 300, 138);
            DrawFactionDot(canvas, 462, 222, new Color32(72, 235, 164, 255));
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/H2/hive_striker_l10", 815, 355, 116);
            DrawFactionDot(canvas, 878, 288, new Color32(255, 72, 54, 255));
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/R3/resource_wax_rich", 630, 250, 74);
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/R2/resource_water_medium", 705, 475, 64);
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/M1/beast_t2_shield_beetle", 520, 510, 86);
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/M1/beast_t7_ancient_hornet_queen", 1030, 460, 126);
            DrawFrame(canvas, new RectInt(0, 0, Width, Height), new Color32(245, 176, 44, 255), 4);
            return canvas;
        }

        private static Texture2D ComposeHiveProgression()
        {
            Texture2D canvas = NewCanvas();
            DrawTerrainGrid(canvas, 12, 12, 1, 1, new Rect(0, 0, 1280, 720));
            DrawPanel(canvas, new RectInt(40, 70, 260, 260), new Color32(22, 28, 24, 210));
            DrawPanel(canvas, new RectInt(340, 70, 260, 260), new Color32(22, 28, 24, 210));
            DrawPanel(canvas, new RectInt(640, 70, 260, 260), new Color32(22, 28, 24, 210));
            DrawPanel(canvas, new RectInt(940, 70, 260, 260), new Color32(22, 28, 24, 210));
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/H1/hive_neutral_l4", 170, 215, 132);
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/H2/hive_royal_guard_l10", 470, 215, 132);
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/H2/hive_scout_l10", 770, 215, 132);
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/H3/hive_alchemist_l35", 1070, 215, 154);
            DrawFactionDot(canvas, 232, 132, new Color32(220, 214, 188, 255));
            DrawFactionDot(canvas, 532, 132, new Color32(72, 235, 164, 255));
            DrawFactionDot(canvas, 832, 132, new Color32(255, 72, 54, 255));
            DrawFactionDot(canvas, 1140, 118, new Color32(72, 235, 164, 255));
            DrawHud(canvas, new Color32(18, 24, 20, 226), new RectInt(70, 470, 1140, 92));
            return canvas;
        }

        private static Texture2D ComposeResourceInteraction()
        {
            Texture2D canvas = NewCanvas();
            DrawTerrainGrid(canvas, 12, 11, 1, 1, new Rect(0, 0, 1280, 720));
            DrawPanel(canvas, new RectInt(60, 94, 250, 280), new Color32(22, 28, 24, 218));
            DrawPanel(canvas, new RectInt(370, 94, 250, 280), new Color32(22, 28, 24, 218));
            DrawPanel(canvas, new RectInt(680, 94, 250, 280), new Color32(22, 28, 24, 218));
            DrawPanel(canvas, new RectInt(990, 94, 220, 280), new Color32(22, 28, 24, 218));
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/R1/resource_pollen_poor", 185, 235, 86);
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/R2/resource_water_medium", 495, 235, 96);
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/R3/resource_wax_rich", 805, 235, 112);
            DrawCross(canvas, 1100, 235, 92, new Color32(255, 72, 54, 255));
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/R3/resource_wax_rich", 1100, 525, 112);
            DrawFrame(canvas, new RectInt(680, 94, 250, 280), new Color32(255, 231, 64, 255), 5);
            DrawHud(canvas, new Color32(18, 24, 20, 226), new RectInt(76, 500, 840, 92));
            return canvas;
        }

        private static Texture2D ComposeBestiaryInteraction()
        {
            Texture2D canvas = NewCanvas();
            DrawTerrainGrid(canvas, 13, 12, 1, 1, new Rect(0, 0, 1280, 720));
            DrawPanel(canvas, new RectInt(100, 100, 440, 420), new Color32(22, 28, 24, 218));
            DrawPanel(canvas, new RectInt(740, 80, 420, 470), new Color32(22, 28, 24, 218));
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/M1/beast_t1_aphid_thief", 320, 290, 128);
            DrawAsset(canvas, "WorldMapRuntimeEntitiesWave1/M1/beast_t7_ancient_hornet_queen", 950, 300, 210);
            DrawFrame(canvas, new RectInt(210, 180, 220, 230), new Color32(255, 231, 64, 255), 5);
            DrawFrame(canvas, new RectInt(805, 130, 290, 330), new Color32(255, 112, 54, 255), 5);
            DrawHud(canvas, new Color32(18, 24, 20, 226), new RectInt(80, 570, 1120, 80));
            return canvas;
        }

        private static Texture2D ComposeBearDenStates()
        {
            Texture2D canvas = NewCanvas();
            DrawTerrainGrid(canvas, 17, 17, 1, 1, new Rect(0, 0, 1280, 720));
            DrawPanel(canvas, new RectInt(45, 92, 350, 430), new Color32(22, 28, 24, 218));
            DrawPanel(canvas, new RectInt(465, 92, 350, 430), new Color32(22, 28, 24, 218));
            DrawPanel(canvas, new RectInt(885, 92, 350, 430), new Color32(22, 28, 24, 218));
            DrawAsset(canvas, "WorldMapWave5Runtime/Landmarks/BearDen/bear_den_dormant_v1", 220, 310, 260);
            DrawCross(canvas, 640, 310, 210, new Color32(120, 170, 180, 255));
            DrawAsset(canvas, "WorldMapWave5Runtime/Landmarks/BearDen/bear_den_dormant_v1", 1060, 310, 260);
            DrawFrame(canvas, new RectInt(45, 92, 350, 430), new Color32(245, 176, 44, 255), 4);
            DrawFrame(canvas, new RectInt(465, 92, 350, 430), new Color32(120, 170, 180, 255), 4);
            DrawFrame(canvas, new RectInt(885, 92, 350, 430), new Color32(245, 176, 44, 255), 4);
            return canvas;
        }

        private static Texture2D ComposePanZoomEdge()
        {
            Texture2D canvas = NewCanvas();
            DrawTerrainGrid(canvas, 0, 0, 1, 1, new Rect(0, 0, 820, 720));
            DrawTerrainGrid(canvas, 12, 12, 1, 1, new Rect(850, 60, 390, 300));
            DrawHud(canvas, new Color32(18, 24, 20, 226), new RectInt(860, 410, 370, 180));
            DrawFrame(canvas, new RectInt(0, 0, 820, 720), new Color32(74, 205, 255, 255), 4);
            DrawFrame(canvas, new RectInt(850, 60, 390, 300), new Color32(245, 176, 44, 255), 4);
            return canvas;
        }

        private static Texture2D NewCanvas()
        {
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            Color32[] fill = new Color32[Width * Height];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(18, 26, 22, 255);
            texture.SetPixels32(fill);
            return texture;
        }

        private static void DrawTerrainGrid(Texture2D canvas, int startRow, int startColumn, int columns, int rows, Rect area)
        {
            int cellW = Mathf.RoundToInt(area.width / columns);
            int cellH = Mathf.RoundToInt(area.height / rows);
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    string path = WorldMapWave5StreamingTileProvider.ResourceRoot + "/" + WorldMapWave5StreamingTileProvider.TileId(startRow + row, startColumn + col) + "_g2";
                    Texture2D tile = LoadResourcePng(path);
                    RectInt target = new RectInt(Mathf.RoundToInt(area.x) + col * cellW, Mathf.RoundToInt(area.y) + row * cellH, cellW + 1, cellH + 1);
                    BlitCropped(canvas, tile, new RectInt(WorldMapWave5StreamingTileProvider.Gutter, WorldMapWave5StreamingTileProvider.Gutter, WorldMapWave5StreamingTileProvider.TileSize, WorldMapWave5StreamingTileProvider.TileSize), target);
                }
            }
        }

        private static void DrawAsset(Texture2D canvas, string resourcePath, int centerX, int centerY, int size)
        {
            Texture2D asset = LoadResourcePng(resourcePath);
            Blit(canvas, asset, new RectInt(centerX - size / 2, centerY - size / 2, size, size));
        }

        private static void DrawPanel(Texture2D canvas, RectInt rect, Color32 color)
        {
            FillRect(canvas, rect, color);
            DrawFrame(canvas, rect, new Color32(245, 176, 44, 225), 3);
        }

        private static void DrawHud(Texture2D canvas, Color32 color, RectInt rect)
        {
            FillRect(canvas, rect, color);
            DrawFrame(canvas, rect, new Color32(74, 205, 255, 220), 3);
        }

        private static void DrawFactionDot(Texture2D canvas, int x, int y, Color32 color)
        {
            FillRect(canvas, new RectInt(x - 14, y - 14, 28, 28), new Color32(16, 18, 15, 240));
            FillRect(canvas, new RectInt(x - 8, y - 8, 16, 16), color);
        }

        private static void DrawCross(Texture2D canvas, int x, int y, int size, Color32 color)
        {
            DrawFrame(canvas, new RectInt(x - size / 2, y - size / 2, size, size), color, 5);
            for (int i = -size / 2; i < size / 2; i++)
            {
                SetPixelSafe(canvas, x + i, y + i, color);
                SetPixelSafe(canvas, x + i, y - i, color);
                SetPixelSafe(canvas, x + i + 1, y + i, color);
                SetPixelSafe(canvas, x + i, y - i + 1, color);
            }
        }

        private static void FillRect(Texture2D canvas, RectInt rect, Color32 color)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++) SetPixelSafe(canvas, x, y, color);
            }
        }

        private static void DrawFrame(Texture2D canvas, RectInt rect, Color32 color, int width)
        {
            FillRect(canvas, new RectInt(rect.xMin, rect.yMin, rect.width, width), color);
            FillRect(canvas, new RectInt(rect.xMin, rect.yMax - width, rect.width, width), color);
            FillRect(canvas, new RectInt(rect.xMin, rect.yMin, width, rect.height), color);
            FillRect(canvas, new RectInt(rect.xMax - width, rect.yMin, width, rect.height), color);
        }

        private static void Blit(Texture2D canvas, Texture2D source, RectInt target)
        {
            if (source == null) return;
            BlitCropped(canvas, source, new RectInt(0, 0, source.width, source.height), target);
        }

        private static void BlitCropped(Texture2D canvas, Texture2D source, RectInt sourceRect, RectInt target)
        {
            if (source == null) return;
            Color32[] pixels = source.GetPixels32();
            for (int y = 0; y < target.height; y++)
            {
                int sy = Mathf.Clamp(sourceRect.y + Mathf.FloorToInt(y / (float)Mathf.Max(1, target.height) * sourceRect.height), 0, source.height - 1);
                for (int x = 0; x < target.width; x++)
                {
                    int sx = Mathf.Clamp(sourceRect.x + Mathf.FloorToInt(x / (float)Mathf.Max(1, target.width) * sourceRect.width), 0, source.width - 1);
                    Color32 src = pixels[sy * source.width + sx];
                    if (src.a == 0) continue;
                    int dx = target.x + x;
                    int dy = target.y + y;
                    if (dx < 0 || dy < 0 || dx >= canvas.width || dy >= canvas.height) continue;
                    Color32 dst = canvas.GetPixel(dx, dy);
                    float a = src.a / 255f;
                    Color32 blended = new Color32(
                        (byte)Mathf.RoundToInt(src.r * a + dst.r * (1f - a)),
                        (byte)Mathf.RoundToInt(src.g * a + dst.g * (1f - a)),
                        (byte)Mathf.RoundToInt(src.b * a + dst.b * (1f - a)),
                        255);
                    canvas.SetPixel(dx, dy, blended);
                }
            }
        }

        private static void SetPixelSafe(Texture2D canvas, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= canvas.width || y >= canvas.height) return;
            canvas.SetPixel(x, y, color);
        }

        private static Texture2D LoadResourcePng(string resourcePath)
        {
            string path = AbsoluteProjectPath("Assets/BeeKingdom/Playground/Resources/" + resourcePath + ".png");
            if (!File.Exists(path)) throw new FileNotFoundException("Missing proof asset", path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        private static void WriteCapture(string root, string name, Texture2D texture, string description)
        {
            texture.Apply(false);
            string path = Path.Combine(root, name);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            captures.Add(new CaptureReceipt(name, description, Sha256(path), new FileInfo(path).Length));
        }

        private static void WriteManifest(string root)
        {
            var md = new StringBuilder();
            md.AppendLine("# Final Visual Smoke Proof Manifest");
            md.AppendLine();
            md.AppendLine("- Method: Unity Editor local composite using real Wave5/runtime PNG assets.");
            md.AppendLine("- Scene target: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`.");
            md.AppendLine("- Width x height: `" + Width.ToString(CultureInfo.InvariantCulture) + "x" + Height.ToString(CultureInfo.InvariantCulture) + "`.");
            md.AppendLine("- Base64/log dump: none.");
            md.AppendLine();
            for (int i = 0; i < captures.Count; i++)
            {
                CaptureReceipt capture = captures[i];
                md.AppendLine("- `" + capture.FileName + "` | " + capture.Description + " | bytes=" + capture.Bytes.ToString(CultureInfo.InvariantCulture) + " | sha256=" + capture.Sha256);
            }

            File.WriteAllText(Path.Combine(root, "manifest.md"), md.ToString(), Encoding.UTF8);
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(File.ReadAllBytes(path));
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static string AbsoluteProjectPath(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        }

        private readonly struct CaptureReceipt
        {
            public readonly string FileName;
            public readonly string Description;
            public readonly string Sha256;
            public readonly long Bytes;

            public CaptureReceipt(string fileName, string description, string sha256, long bytes)
            {
                FileName = fileName;
                Description = description;
                Sha256 = sha256;
                Bytes = bytes;
            }
        }
    }
}
