using System;
using System.Globalization;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class WorldMapWave5Premium25x25TestBootstrap : MonoBehaviour
    {
        public const string ScenePath = "Assets/Scenes/WorldMapWave5Premium25x25Test.unity";
        public const string SceneName = "WorldMapWave5Premium25x25Test";
        private const float MinZoom = 0.65f;
        private const float MaxZoom = 2.0f;
        private const float PanDamping = 15f;
        private const float ZoomDamping = 14f;

        private WorldMapWave5StreamingTileProvider provider;
        private Texture2D pixel;
        private Texture2D playerHive;
        private Texture2D enemyHive;
        private Texture2D nectar;
        private Vector2 currentWorldCenter;
        private Vector2 targetWorldCenter;
        private float currentZoom = 1f;
        private float targetZoom = 1f;
        private bool showMarkers = true;
        private bool mouseDragging;
        private Vector2 previousMousePosition;
        private string status = "Initialisation Wave5 premium";

        public Rect WorldBounds => provider != null ? provider.WorldBounds : default;
        public Vector2 CurrentWorldCenter => currentWorldCenter;
        public float CurrentZoom => currentZoom;
        public bool ManifestReady => provider != null && provider.ManifestReady && !provider.HasLoadFailure;
        public bool VisibleTilesReady => provider != null && provider.HasAllVisibleTiles;
        public int LoadedVisibleTiles => provider != null ? provider.LoadedVisibleTileCount : 0;
        public int RequiredVisibleTiles => provider != null ? provider.RequiredVisibleTileCount : 0;
        public int CachedTiles => provider != null ? provider.CachedTileCount : 0;

        private void Awake()
        {
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();

            provider = new WorldMapWave5StreamingTileProvider();
            currentWorldCenter = provider.WorldBounds.center;
            targetWorldCenter = currentWorldCenter;
            bool ready = provider.Initialize(currentWorldCenter, currentZoom, Screen.width, Screen.height);
            status = ready ? "Wave5 premium prête" : provider.FailureReason;

            playerHive = Resources.Load<Texture2D>("WorldMapRuntimeEntitiesWave1/H2/hive_nurturer_l10");
            enemyHive = Resources.Load<Texture2D>("WorldMapRuntimeEntitiesWave1/H2/hive_striker_l10");
            nectar = Resources.Load<Texture2D>("WorldMapRuntimeEntitiesWave1/R3/resource_nectar_rich");
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
                if (showMarkers) DrawMarkers();
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
            if (playerHive != null) Resources.UnloadAsset(playerHive);
            if (enemyHive != null) Resources.UnloadAsset(enemyHive);
            if (nectar != null) Resources.UnloadAsset(nectar);
            if (pixel != null) Destroy(pixel);
        }

        private void DrawTerrain()
        {
            var tiles = provider.VisibleTiles;
            Rect viewport = new Rect(0f, 0f, Screen.width, Screen.height);
            for (int i = 0; i < tiles.Count; i++)
            {
                Wave5RuntimeTile tile = tiles[i];
                Rect projected = WorldRectToScreen(tile.GutterWorldRect);
                Rect snapped = PixelSnappedRect(projected.min, projected.max);
                if (!snapped.Overlaps(viewport)) continue;
                GUI.DrawTextureWithTexCoords(snapped, tile.Texture, new Rect(0f, 0f, 1f, 1f), true);
            }
        }

        private void DrawMarkers()
        {
            Rect bounds = provider.WorldBounds;
            DrawWorldMarker(playerHive, bounds.center + new Vector2(-260f, 120f), 138f, "Ruche test locale", new Color(0.36f, 0.94f, 0.72f, 1f));
            DrawWorldMarker(enemyHive, bounds.center + new Vector2(760f, -430f), 126f, "Ruche hostile démo", new Color(1f, 0.42f, 0.28f, 1f));
            DrawWorldMarker(nectar, bounds.center + new Vector2(-780f, -360f), 92f, "Nectar local", new Color(0.98f, 0.78f, 0.22f, 1f));
        }

        private void DrawWorldMarker(Texture2D texture, Vector2 worldPosition, float worldSize, string label, Color accent)
        {
            if (texture == null) return;
            Vector2 center = WorldToScreen(worldPosition);
            float size = Mathf.Clamp(worldSize * currentZoom, 48f, 210f);
            Rect icon = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            if (!icon.Overlaps(new Rect(-size, -size, Screen.width + size * 2f, Screen.height + size * 2f))) return;

            DrawFrame(new Rect(icon.x - 4f, icon.y - 4f, icon.width + 8f, icon.height + 8f), accent, 2f);
            GUI.DrawTexture(icon, texture, ScaleMode.ScaleToFit, true);
            Rect caption = new Rect(center.x - 88f, icon.yMax + 4f, 176f, 28f);
            DrawSolid(caption, new Color(0.02f, 0.024f, 0.018f, 0.88f));
            DrawFrame(caption, new Color(accent.r, accent.g, accent.b, 0.72f), 1f);
            GUI.Label(caption, label, LabelStyle(Color.white, 12, FontStyle.Bold, TextAnchor.MiddleCenter));
        }

        private void DrawFixedHud()
        {
            bool portrait = IsPortrait();
            Rect title = portrait
                ? new Rect(8f, 8f, Screen.width - 16f, 108f)
                : new Rect(14f, 12f, Mathf.Min(780f, Screen.width - 320f), 108f);
            DrawPanel(title, new Color(0.025f, 0.027f, 0.020f, 0.92f), new Color(0.97f, 0.67f, 0.16f, 0.92f));
            GUI.Label(new Rect(title.x + 16f, title.y + 10f, title.width - 32f, 30f), "Wave5 Premium 25x25", LabelStyle(Color.white, portrait ? 22 : 24, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(title.x + 16f, title.y + 42f, title.width - 32f, 22f), "TEST LOCAL - 625 tuiles premium - aucune donnée live", LabelStyle(new Color(1f, 0.84f, 0.42f, 1f), portrait ? 12 : 13, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(title.x + 16f, title.y + 70f, title.width - 32f, 22f), "Pan/zoom terrain uniquement | HUD fixe | source non modifiée", LabelStyle(new Color(0.82f, 0.90f, 0.78f, 1f), portrait ? 11 : 12, FontStyle.Normal, TextAnchor.MiddleLeft));

            Rect stats = portrait
                ? new Rect(8f, 124f, Screen.width - 16f, 76f)
                : new Rect(Screen.width - 292f, 12f, 278f, 150f);
            DrawPanel(stats, new Color(0.025f, 0.027f, 0.020f, 0.92f), new Color(0.97f, 0.67f, 0.16f, 0.92f));
            Vector2Int chunk = new Vector2Int(Mathf.FloorToInt(currentWorldCenter.x / 512f), Mathf.FloorToInt(currentWorldCenter.y / 512f));
            string firstLine = "Zoom " + currentZoom.ToString("0.00", CultureInfo.InvariantCulture)
                + " | C" + chunk.x.ToString("00", CultureInfo.InvariantCulture) + "_" + chunk.y.ToString("00", CultureInfo.InvariantCulture);
            GUI.Label(new Rect(stats.x + 12f, stats.y + 8f, stats.width - 24f, 22f), firstLine, LabelStyle(Color.white, 13, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(stats.x + 12f, stats.y + 32f, stats.width - 24f, 20f), "Tuiles " + LoadedVisibleTiles + "/" + RequiredVisibleTiles + " | cache " + CachedTiles + "/96", LabelStyle(new Color(0.86f, 1f, 0.88f, 1f), 12, FontStyle.Normal, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(stats.x + 12f, stats.y + 54f, stats.width - 24f, portrait ? 18f : 40f), status, LabelStyle(new Color(1f, 0.84f, 0.42f, 1f), 11, FontStyle.Normal, TextAnchor.UpperLeft));
            if (!portrait)
            {
                GUI.Label(new Rect(stats.x + 12f, stats.y + 94f, stats.width - 24f, 42f), "SHA master\n50F3FF964025...9125913", LabelStyle(new Color(0.72f, 0.80f, 0.70f, 1f), 10, FontStyle.Normal, TextAnchor.UpperLeft));
            }

            Rect controls = portrait
                ? new Rect(8f, 208f, Mathf.Min(Screen.width - 16f, 354f), 54f)
                : new Rect(14f, 128f, 354f, 54f);
            DrawPanel(controls, new Color(0.025f, 0.027f, 0.020f, 0.92f), new Color(0.36f, 0.92f, 0.68f, 0.82f));
            if (GUI.Button(new Rect(controls.x + 8f, controls.y + 8f, 158f, 38f), "Recentrer")) ResetView();
            if (GUI.Button(new Rect(controls.x + 176f, controls.y + 8f, 168f, 38f), showMarkers ? "Repères : visibles" : "Repères : masqués")) showMarkers = !showMarkers;

            if (!portrait) DrawMiniMap();
        }

        private void DrawMiniMap()
        {
            Rect panel = new Rect(Screen.width - 214f, Screen.height - 154f, 198f, 138f);
            DrawPanel(panel, new Color(0.025f, 0.027f, 0.020f, 0.92f), new Color(0.97f, 0.67f, 0.16f, 0.82f));
            GUI.Label(new Rect(panel.x + 10f, panel.y + 6f, panel.width - 20f, 20f), "Carte premium 25x25", LabelStyle(Color.white, 11, FontStyle.Bold, TextAnchor.MiddleCenter));
            Rect map = new Rect(panel.x + 12f, panel.y + 30f, panel.width - 24f, panel.height - 42f);
            DrawSolid(map, new Color(0.06f, 0.10f, 0.055f, 1f));
            Rect bounds = provider.WorldBounds;
            float x = map.x + (currentWorldCenter.x - bounds.xMin) / bounds.width * map.width;
            float y = map.y + (currentWorldCenter.y - bounds.yMin) / bounds.height * map.height;
            DrawSolid(new Rect(x - 4f, y - 4f, 8f, 8f), new Color(1f, 0.78f, 0.18f, 1f));
        }

        private void DrawUnavailableState()
        {
            Rect panel = new Rect(Mathf.Max(20f, Screen.width * 0.5f - 330f), Mathf.Max(20f, Screen.height * 0.5f - 80f), 660f, 160f);
            DrawPanel(panel, new Color(0.025f, 0.020f, 0.014f, 0.96f), new Color(1f, 0.44f, 0.24f, 0.94f));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 32f), "Wave5 Premium indisponible", LabelStyle(Color.white, 22, FontStyle.Bold, TextAnchor.MiddleCenter));
            string reason = provider != null && !string.IsNullOrEmpty(provider.FailureReason) ? provider.FailureReason : "Chargement des tuiles visibles";
            GUI.Label(new Rect(panel.x + 24f, panel.y + 62f, panel.width - 48f, 60f), reason + "\nAucun fallback Wave6 ou aplat vert n'est autorisé dans cette scène.", LabelStyle(new Color(1f, 0.86f, 0.58f, 1f), 13, FontStyle.Normal, TextAnchor.MiddleCenter));
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.R)) ResetView();

            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                Vector2 guiPoint = new Vector2(touch.position.x, Screen.height - touch.position.y);
                if (touch.phase == TouchPhase.Moved && !IsPointerOverFixedUi(guiPoint))
                {
                    targetWorldCenter += new Vector2(-touch.deltaPosition.x, touch.deltaPosition.y) / Mathf.Max(0.01f, targetZoom);
                    ClampTarget();
                }
                return;
            }

            if (Input.touchCount >= 2)
            {
                Touch first = Input.GetTouch(0);
                Touch second = Input.GetTouch(1);
                Vector2 firstPrevious = first.position - first.deltaPosition;
                Vector2 secondPrevious = second.position - second.deltaPosition;
                float previousDistance = Vector2.Distance(firstPrevious, secondPrevious);
                float currentDistance = Vector2.Distance(first.position, second.position);
                ApplyZoomDelta((currentDistance - previousDistance) * 0.0032f);
                return;
            }

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
            targetWorldCenter = provider != null ? provider.WorldBounds.center : new Vector2(16640f, 16640f);
            currentWorldCenter = targetWorldCenter;
            targetZoom = 1f;
            currentZoom = 1f;
            if (provider != null) provider.UpdateStreaming(targetWorldCenter, targetZoom, Screen.width, Screen.height, true);
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

        private bool IsPointerOverFixedUi(Vector2 guiPoint)
        {
            if (IsPortrait())
            {
                return new Rect(0f, 0f, Screen.width, 270f).Contains(guiPoint);
            }
            return new Rect(0f, 0f, Mathf.Min(800f, Screen.width), 190f).Contains(guiPoint)
                || new Rect(Screen.width - 306f, 0f, 306f, 176f).Contains(guiPoint)
                || new Rect(Screen.width - 230f, Screen.height - 170f, 230f, 170f).Contains(guiPoint);
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

        private static bool IsPortrait()
        {
            return Screen.width < 700 || Screen.height > Screen.width * 1.15f;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void ApplyProofView(Vector2 worldCenter, float zoom)
        {
            currentZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            targetZoom = currentZoom;
            currentWorldCenter = worldCenter;
            targetWorldCenter = worldCenter;
            ClampTarget();
            currentWorldCenter = targetWorldCenter;
            if (provider != null) provider.UpdateStreaming(currentWorldCenter, currentZoom, Screen.width, Screen.height, true);
        }

        public ProofSnapshot CurrentProofSnapshot()
        {
            return new ProofSnapshot(
                currentWorldCenter,
                currentZoom,
                ManifestReady,
                VisibleTilesReady,
                LoadedVisibleTiles,
                RequiredVisibleTiles,
                CachedTiles,
                WorldBounds,
                IsPortrait() ? new Rect(8f, 8f, Screen.width - 16f, 108f) : new Rect(14f, 12f, Mathf.Min(780f, Screen.width - 320f), 108f));
        }

        public readonly struct ProofSnapshot
        {
            public readonly Vector2 WorldCenter;
            public readonly float Zoom;
            public readonly bool ManifestReady;
            public readonly bool VisibleTilesReady;
            public readonly int LoadedVisibleTiles;
            public readonly int RequiredVisibleTiles;
            public readonly int CachedTiles;
            public readonly Rect WorldBounds;
            public readonly Rect HudRect;

            public ProofSnapshot(Vector2 worldCenter, float zoom, bool manifestReady, bool visibleTilesReady, int loadedVisibleTiles, int requiredVisibleTiles, int cachedTiles, Rect worldBounds, Rect hudRect)
            {
                WorldCenter = worldCenter;
                Zoom = zoom;
                ManifestReady = manifestReady;
                VisibleTilesReady = visibleTilesReady;
                LoadedVisibleTiles = loadedVisibleTiles;
                RequiredVisibleTiles = requiredVisibleTiles;
                CachedTiles = cachedTiles;
                WorldBounds = worldBounds;
                HudRect = hudRect;
            }
        }
#endif
    }
}
