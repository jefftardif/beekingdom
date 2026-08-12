using System;
using UnityEngine;

namespace BeeKingdom.WorldMap
{
    // Overlay de debug de la carte : coordonnees monde, chunk courant, chunks
    // charges, nombre d'objets, zoom et FPS. Toggle via F3 ; l'etat est sauvegarde
    // dans les preferences de la carte (WorldSave).
    public sealed class WorldDebugOverlay : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F3;

        [SerializeField] private bool visibleByDefault = true;

        private WorldManager manager;
        private WorldCameraController cameraController;
        private GUIStyle labelStyle;
        private float smoothedFps = 60f;
        private bool visible;

        protected void Awake()
        {
            manager = FindObjectOfType<WorldManager>();
            cameraController = FindObjectOfType<WorldCameraController>();
            visible = visibleByDefault;
            if (manager != null)
            {
                WorldMapSaveData data = new WorldMapSaveData { DebugOverlayVisible = visibleByDefault };
                if (manager.Save != null && manager.Save.TryLoad(out WorldMapSaveData saved))
                {
                    visible = saved.DebugOverlayVisible;
                }
            }
        }

        protected void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                visible = !visible;
                if (manager != null && manager.Save != null)
                {
                    WorldMapSaveData data = new WorldMapSaveData { DebugOverlayVisible = visible };
                    if (manager.Save.TryLoad(out WorldMapSaveData existing))
                    {
                        data = existing;
                        data.DebugOverlayVisible = visible;
                    }

                    manager.Save.Save(data);
                }
            }

            float instantFps = Time.deltaTime > 0f ? 1f / Time.deltaTime : 0f;
            smoothedFps = Mathf.Lerp(smoothedFps, instantFps, 0.06f);
        }

        protected void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            EnsureStyles();
            GUILayout.BeginArea(new Rect(8f, 8f, 300f, 220f));
            GUILayout.BeginVertical(new GUIStyle(GUI.skin.box) { normal = { background = SolidTexture(new Color(0f, 0f, 0f, 0.62f)) } });

            WorldVector2 cameraPosition = cameraController != null ? cameraController.Position : new WorldVector2(0d, 0d);
            float zoom = cameraController != null ? cameraController.Zoom : 0f;
            WorldPosition focus = new WorldPosition((long)Math.Round(cameraPosition.X), (long)Math.Round(cameraPosition.Y));
            ChunkCoordinate focusChunk = manager != null && manager.Grid != null
                ? manager.Grid.ChunkOf(focus)
                : WorldCoordinateSystem.ChunkOf(focus, 64L);

            GUILayout.Label("Carte du Monde", labelStyle);
            GUILayout.Label("FPS          " + smoothedFps.ToString("0.0"), labelStyle);
            GUILayout.Label("Position     " + focus.X + ", " + focus.Y, labelStyle);
            GUILayout.Label("Chunk        " + focusChunk.X + ", " + focusChunk.Y, labelStyle);
            GUILayout.Label("Zoom         " + zoom.ToString("0.0"), labelStyle);
            GUILayout.Label("Chunks       " + (manager != null && manager.Streamer != null ? manager.Streamer.LoadedChunkCount : 0)
                + " (+" + (manager != null && manager.Streamer != null ? manager.Streamer.PendingLoadCount : 0) + " charg.)", labelStyle);
            GUILayout.Label("Objets       " + (manager != null && manager.Grid != null ? manager.Grid.ObjectCount : 0), labelStyle);
            GUILayout.Label("Selection    " + (manager != null && manager.Selection != null ? manager.Selection.Count : 0), labelStyle);
            GUILayout.Label("F3 pour masquer", labelStyle);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
            };
            labelStyle.padding = new RectOffset(6, 6, 1, 1);
        }

        private static Texture2D solidTextureCache;

        private static Texture2D SolidTexture(Color color)
        {
            if (solidTextureCache == null)
            {
                solidTextureCache = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            }

            solidTextureCache.SetPixel(0, 0, color);
            solidTextureCache.Apply();
            return solidTextureCache;
        }
    }
}
