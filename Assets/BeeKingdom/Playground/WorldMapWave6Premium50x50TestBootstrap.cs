using System;
using System.Globalization;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class WorldMapWave6Premium50x50TestBootstrap : MonoBehaviour
    {
        public const string ScenePath = "Assets/Scenes/WorldMapWave6Premium50x50TerrainTest.unity";
        public const string SceneName = "WorldMapWave6Premium50x50TerrainTest";
        private const float MinZoom = 0.35f;
        private const float MaxZoom = 2.0f;
        private const float InitialZoom = 0.58f;
        private const float PanDamping = 15f;
        private const float ZoomDamping = 14f;
        private static readonly Rect FullTextureUv = new Rect(0f, 0f, 1f, 1f);

        private static readonly Hotspot[] Hotspots =
        {
            new Hotspot("C54_09", 54, 9),
            new Hotspot("C53_26", 53, 26),
            new Hotspot("C52_52", 52, 52),
            new Hotspot("C48_46", 48, 46),
            new Hotspot("Centre", 32, 32)
        };

        private WorldMapWave6StreamingTileProvider provider;
        private Texture2D pixel;
        private Vector2 currentWorldCenter;
        private Vector2 targetWorldCenter;
        private float currentZoom = InitialZoom;
        private float targetZoom = InitialZoom;
        private bool showTileGuides;
        private bool useGutterRendering = true;
        private bool mouseDragging;
        private Vector2 previousMousePosition;
        private string status = "Initialisation Wave6 50x50";

        public Rect WorldBounds => provider != null ? provider.WorldBounds : default;
        public Vector2 CurrentWorldCenter => currentWorldCenter;
        public float CurrentZoom => currentZoom;
        public bool ManifestReady => provider != null && provider.ManifestReady && !provider.HasLoadFailure;
        public bool VisibleTilesReady => provider != null && provider.HasAllVisibleTiles;
        public int LoadedVisibleTiles => provider != null ? provider.LoadedVisibleTileCount : 0;
        public int RequiredVisibleTiles => provider != null ? provider.RequiredVisibleTileCount : 0;
        public int CachedTiles => provider != null ? provider.CachedTileCount : 0;
        public bool UseGutterRendering => useGutterRendering;
        public bool ShowTileGuides => showTileGuides;

        public void SetProofView(int chunkX, int chunkY, float zoom, bool guides, bool gutterRendering)
        {
            showTileGuides = guides;
            useGutterRendering = gutterRendering;
            targetWorldCenter = ChunkCenter(chunkX, chunkY);
            currentWorldCenter = targetWorldCenter;
            targetZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            currentZoom = targetZoom;
            if (provider != null) provider.UpdateStreaming(targetWorldCenter, targetZoom, Screen.width, Screen.height, true);
        }

        private void Awake()
        {
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();

            provider = new WorldMapWave6StreamingTileProvider(
                WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewResourceRoot,
                WorldMapWave6StreamingTileProvider.Wave5Method12288PreviewExpectedMasterSha256);

            targetWorldCenter = ChunkCenter(54, 9);
            currentWorldCenter = targetWorldCenter;
            bool ready = provider.Initialize(currentWorldCenter, currentZoom, Screen.width, Screen.height);
            status = ready ? "Wave6 50x50 Wave5-method 12288 prete" : provider.FailureReason;
        }

        private void Update()
        {
            HandleInput();
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, 1f - Mathf.Exp(-ZoomDamping * Time.deltaTime));
            currentWorldCenter = Vector2.Lerp(currentWorldCenter, targetWorldCenter, 1f - Mathf.Exp(-PanDamping * Time.deltaTime));
            if (provider != null) provider.UpdateStreaming(targetWorldCenter, targetZoom, Screen.width, Screen.height);
        }

        private void OnGUI()
        {
            DrawSolid(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.015f, 0.016f, 0.012f, 1f));
            if (ManifestReady && VisibleTilesReady)
            {
                DrawTerrain();
            }
            else
            {
                DrawUnavailableState();
            }

            DrawFixedHud();
        }

        private void OnDestroy()
        {
            if (provider != null) provider.Dispose();
            if (pixel != null) Destroy(pixel);
        }

        private void DrawTerrain()
        {
            var tiles = provider.VisibleTiles;
            Rect viewport = new Rect(0f, 0f, Screen.width, Screen.height);
            for (int i = 0; i < tiles.Count; i++)
            {
                Wave6RuntimeTile tile = tiles[i];
                Rect terrainWorldRect = useGutterRendering ? tile.GutterWorldRect : tile.WorldRect;
                Rect textureUv = useGutterRendering ? FullTextureUv : tile.CoreUv;
                Rect projected = WorldRectToScreen(terrainWorldRect);
                Rect snapped = PixelSnappedRect(projected.min, projected.max);
                if (!snapped.Overlaps(viewport)) continue;

                GUI.DrawTextureWithTexCoords(snapped, tile.Texture, textureUv, true);
                if (showTileGuides)
                {
                    DrawFrame(snapped, new Color(0.1f, 0.95f, 1f, 0.62f), 1f);
                    GUI.Label(new Rect(snapped.x + 6f, snapped.y + 4f, 90f, 18f), tile.Id, LabelStyle(Color.white, 11, FontStyle.Bold, TextAnchor.MiddleLeft));
                }
            }
        }

        private void DrawFixedHud()
        {
            Rect title = new Rect(12f, 12f, Mathf.Min(920f, Screen.width - 320f), 104f);
            DrawPanel(title, new Color(0.025f, 0.027f, 0.020f, 0.90f), new Color(0.97f, 0.67f, 0.16f, 0.92f));
            GUI.Label(new Rect(title.x + 16f, title.y + 10f, title.width - 32f, 28f), "Wave6 Premium 50x50 - terrain seul", LabelStyle(Color.white, 22, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(title.x + 16f, title.y + 40f, title.width - 32f, 22f), "TEST LOCAL - 2500 tuiles - package Wave5-method 12288 - aucune couche MMO", LabelStyle(new Color(1f, 0.84f, 0.42f, 1f), 12, FontStyle.Bold, TextAnchor.MiddleLeft));
            string renderMode = useGutterRendering
                ? "Rendu gouttiere: rectangle 516 + UV complete, comme la carte premium 25x25"
                : "Rendu core-only: rectangle 512 + UV coeur, mode diagnostic des coutures";
            GUI.Label(new Rect(title.x + 16f, title.y + 66f, title.width - 32f, 22f), renderMode, LabelStyle(new Color(0.82f, 0.90f, 0.78f, 1f), 12, FontStyle.Normal, TextAnchor.MiddleLeft));

            Rect stats = new Rect(Screen.width - 306f, 12f, 292f, 150f);
            DrawPanel(stats, new Color(0.025f, 0.027f, 0.020f, 0.90f), new Color(0.97f, 0.67f, 0.16f, 0.92f));
            Vector2Int chunk = CurrentChunk();
            GUI.Label(new Rect(stats.x + 12f, stats.y + 8f, stats.width - 24f, 22f), "Zoom " + currentZoom.ToString("0.00", CultureInfo.InvariantCulture) + " | C" + chunk.x.ToString("00", CultureInfo.InvariantCulture) + "_" + chunk.y.ToString("00", CultureInfo.InvariantCulture), LabelStyle(Color.white, 13, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(stats.x + 12f, stats.y + 32f, stats.width - 24f, 20f), "Tuiles " + LoadedVisibleTiles + "/" + RequiredVisibleTiles + " | cache " + CachedTiles + "/128", LabelStyle(new Color(0.86f, 1f, 0.88f, 1f), 12, FontStyle.Normal, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(stats.x + 12f, stats.y + 54f, stats.width - 24f, 36f), status, LabelStyle(new Color(1f, 0.84f, 0.42f, 1f), 11, FontStyle.Normal, TextAnchor.UpperLeft));
            GUI.Label(new Rect(stats.x + 12f, stats.y + 94f, stats.width - 24f, 42f), "SHA 3CE816052FFF...330697\nScene de diagnostic, pas un handoff Unity", LabelStyle(new Color(0.72f, 0.80f, 0.70f, 1f), 10, FontStyle.Normal, TextAnchor.UpperLeft));

            Rect controls = new Rect(12f, 126f, Mathf.Min(920f, Screen.width - 320f), 52f);
            DrawPanel(controls, new Color(0.025f, 0.027f, 0.020f, 0.90f), new Color(0.36f, 0.92f, 0.68f, 0.82f));
            if (GUI.Button(new Rect(controls.x + 8f, controls.y + 8f, 110f, 36f), "Recentrer")) ResetView();
            if (GUI.Button(new Rect(controls.x + 126f, controls.y + 8f, 128f, 36f), showTileGuides ? "Guides ON" : "Guides OFF")) showTileGuides = !showTileGuides;
            if (GUI.Button(new Rect(controls.x + 262f, controls.y + 8f, 132f, 36f), useGutterRendering ? "Gouttiere ON" : "Core 512")) useGutterRendering = !useGutterRendering;

            float x = controls.x + 404f;
            for (int i = 0; i < Hotspots.Length; i++)
            {
                if (GUI.Button(new Rect(x, controls.y + 8f, 82f, 36f), Hotspots[i].Label))
                {
                    FocusChunk(Hotspots[i].ChunkX, Hotspots[i].ChunkY);
                }
                x += 88f;
            }

            DrawMiniMap();
        }

        private void DrawMiniMap()
        {
            Rect panel = new Rect(Screen.width - 228f, Screen.height - 154f, 212f, 138f);
            DrawPanel(panel, new Color(0.025f, 0.027f, 0.020f, 0.90f), new Color(0.97f, 0.67f, 0.16f, 0.82f));
            GUI.Label(new Rect(panel.x + 10f, panel.y + 6f, panel.width - 20f, 20f), "Carte 50x50 terrain", LabelStyle(Color.white, 11, FontStyle.Bold, TextAnchor.MiddleCenter));
            Rect map = new Rect(panel.x + 12f, panel.y + 30f, panel.width - 24f, panel.height - 42f);
            DrawSolid(map, new Color(0.06f, 0.10f, 0.055f, 1f));
            DrawFrame(map, new Color(0.2f, 0.9f, 0.62f, 1f), 2f);
            Rect bounds = provider.WorldBounds;
            float x = map.x + (currentWorldCenter.x - bounds.xMin) / bounds.width * map.width;
            float y = map.y + (currentWorldCenter.y - bounds.yMin) / bounds.height * map.height;
            DrawSolid(new Rect(x - 4f, y - 4f, 8f, 8f), new Color(1f, 0.78f, 0.18f, 1f));
        }

        private void DrawUnavailableState()
        {
            Rect panel = new Rect(Mathf.Max(20f, Screen.width * 0.5f - 360f), Mathf.Max(20f, Screen.height * 0.5f - 80f), 720f, 160f);
            DrawPanel(panel, new Color(0.025f, 0.020f, 0.014f, 0.96f), new Color(1f, 0.44f, 0.24f, 0.94f));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 32f), "Wave6 50x50 indisponible", LabelStyle(Color.white, 22, FontStyle.Bold, TextAnchor.MiddleCenter));
            string reason = provider != null && !string.IsNullOrEmpty(provider.FailureReason) ? provider.FailureReason : "Chargement des tuiles visibles";
            GUI.Label(new Rect(panel.x + 24f, panel.y + 62f, panel.width - 48f, 60f), reason + "\nAucun fallback ni scene MMO n'est utilise ici.", LabelStyle(new Color(1f, 0.86f, 0.58f, 1f), 13, FontStyle.Normal, TextAnchor.MiddleCenter));
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.R)) ResetView();
            if (Input.GetKeyDown(KeyCode.G)) showTileGuides = !showTileGuides;
            if (Input.GetKeyDown(KeyCode.M)) useGutterRendering = !useGutterRendering;

            Vector2 mouseGui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (Input.GetMouseButtonDown(0) && !IsPointerOverFixedUi(mouseGui))
            {
                mouseDragging = true;
                previousMousePosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0)) mouseDragging = false;
            if (mouseDragging && Input.GetMouseButton(0))
            {
                Vector2 current = Input.mousePosition;
                Vector2 delta = current - previousMousePosition;
                previousMousePosition = current;
                targetWorldCenter += new Vector2(-delta.x, delta.y) / Mathf.Max(0.01f, targetZoom);
                ClampTarget();
            }

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.001f && !IsPointerOverFixedUi(mouseGui)) ApplyZoomDelta(wheel * 0.12f);
        }

        private void ApplyZoomDelta(float delta)
        {
            targetZoom = Mathf.Clamp(targetZoom * Mathf.Exp(delta), MinZoom, MaxZoom);
            ClampTarget();
        }

        private void ResetView()
        {
            FocusChunk(54, 9);
        }

        private void FocusChunk(int chunkX, int chunkY)
        {
            targetWorldCenter = ChunkCenter(chunkX, chunkY);
            currentWorldCenter = targetWorldCenter;
            targetZoom = InitialZoom;
            currentZoom = targetZoom;
            if (provider != null) provider.UpdateStreaming(targetWorldCenter, targetZoom, Screen.width, Screen.height, true);
        }

        private Vector2 ChunkCenter(int chunkX, int chunkY)
        {
            int column = Mathf.Clamp(chunkX - WorldMapWave6StreamingTileProvider.OriginChunkX, 0, WorldMapWave6StreamingTileProvider.Columns - 1);
            int row = Mathf.Clamp(chunkY - WorldMapWave6StreamingTileProvider.OriginChunkY, 0, WorldMapWave6StreamingTileProvider.Rows - 1);
            return WorldMapWave6StreamingTileProvider.TileAnchorWorld(row, column, WorldMapWave6StreamingTileProvider.TileSize * 0.5f, WorldMapWave6StreamingTileProvider.TileSize * 0.5f);
        }

        private void ClampTarget()
        {
            if (provider == null) return;
            Rect bounds = provider.WorldBounds;
            float halfWidth = Screen.width * 0.5f / Mathf.Max(0.01f, targetZoom);
            float halfHeight = Screen.height * 0.5f / Mathf.Max(0.01f, targetZoom);
            float minX = bounds.xMin + halfWidth;
            float maxX = bounds.xMax - halfWidth;
            float minY = bounds.yMin + halfHeight;
            float maxY = bounds.yMax - halfHeight;
            targetWorldCenter.x = minX <= maxX ? Mathf.Clamp(targetWorldCenter.x, minX, maxX) : bounds.center.x;
            targetWorldCenter.y = minY <= maxY ? Mathf.Clamp(targetWorldCenter.y, minY, maxY) : bounds.center.y;
        }

        private Vector2Int CurrentChunk()
        {
            return new Vector2Int(
                Mathf.FloorToInt(currentWorldCenter.x / WorldMapWave6StreamingTileProvider.TileSize),
                Mathf.FloorToInt(currentWorldCenter.y / WorldMapWave6StreamingTileProvider.TileSize));
        }

        private bool IsPointerOverFixedUi(Vector2 guiPoint)
        {
            return new Rect(0f, 0f, Mathf.Min(960f, Screen.width), 190f).Contains(guiPoint)
                || new Rect(Screen.width - 320f, 0f, 320f, 176f).Contains(guiPoint)
                || new Rect(Screen.width - 240f, Screen.height - 170f, 240f, 170f).Contains(guiPoint);
        }

        private Vector2 WorldToScreen(Vector2 world)
        {
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + (world - currentWorldCenter) * currentZoom;
        }

        private Rect WorldRectToScreen(Rect worldRect)
        {
            Vector2 min = WorldToScreen(worldRect.min);
            Vector2 max = WorldToScreen(worldRect.max);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        private static Rect PixelSnappedRect(Vector2 min, Vector2 max)
        {
            return Rect.MinMaxRect(
                Mathf.Floor(Mathf.Min(min.x, max.x)),
                Mathf.Floor(Mathf.Min(min.y, max.y)),
                Mathf.Ceil(Mathf.Max(min.x, max.x)),
                Mathf.Ceil(Mathf.Max(min.y, max.y)));
        }

        private void DrawPanel(Rect rect, Color fill, Color border)
        {
            DrawSolid(rect, fill);
            DrawFrame(rect, border, 2f);
        }

        private void DrawSolid(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        private void DrawFrame(Rect rect, Color color, float width)
        {
            DrawSolid(new Rect(rect.x, rect.y, rect.width, width), color);
            DrawSolid(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            DrawSolid(new Rect(rect.x, rect.y, width, rect.height), color);
            DrawSolid(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }

        private static GUIStyle LabelStyle(Color color, int size, FontStyle fontStyle, TextAnchor alignment)
        {
            return new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = color },
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = true,
                clipping = TextClipping.Clip
            };
        }

        private readonly struct Hotspot
        {
            public readonly string Label;
            public readonly int ChunkX;
            public readonly int ChunkY;

            public Hotspot(string label, int chunkX, int chunkY)
            {
                Label = label;
                ChunkX = chunkX;
                ChunkY = chunkY;
            }
        }
    }
}
