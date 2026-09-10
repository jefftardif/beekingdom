using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class WorldMapOneClickWaterPrototype : MonoBehaviour
    {
        private const int WaterLayer = 11;
        private static readonly Zone[] Zones =
        {
            new Zone("River", new Rect(6900f, 5700f, 1280f, 260f), new Vector2(.92f, .16f), .10f, .035f),
            new Zone("Basin", new Rect(7480f, 6070f, 620f, 300f), new Vector2(.18f, .04f), .035f, .012f)
        };
        private Capture[] captures;
        private Material sourceMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DisableIncompatiblePlanarReflections()
        {
            foreach (ESSW.Editorcontroller.PlanarReflections reflection in Object.FindObjectsByType<ESSW.Editorcontroller.PlanarReflections>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                reflection.enabled = false;
        }

        private readonly struct Zone
        {
            public readonly string Name; public readonly Rect WorldRect; public readonly Vector2 Direction;
            public readonly float WaveSpeed; public readonly float SurfaceOpacity;
            public Zone(string name, Rect worldRect, Vector2 direction, float waveSpeed, float surfaceOpacity)
            { Name = name; WorldRect = worldRect; Direction = direction; WaveSpeed = waveSpeed; SurfaceOpacity = surfaceOpacity; }
        }

        private sealed class Capture
        {
            public Camera Camera; public RenderTexture Texture; public Material Material; public GameObject Root;
        }

        private void Awake()
        {
            foreach (ESSW.Editorcontroller.PlanarReflections reflection in FindObjectsByType<ESSW.Editorcontroller.PlanarReflections>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                reflection.enabled = false;
            sourceMaterial = Resources.Load<Material>("water");
            if (sourceMaterial == null) { Debug.LogError("[OneClickWater] Resources/water.mat was not found.", this); enabled = false; return; }
            captures = new Capture[Zones.Length];
            for (int i = 0; i < Zones.Length; i++) captures[i] = CreateCapture(Zones[i], i);
        }

        public void DrawOverlay(Vector2 worldCenter, float zoom, Rect viewport)
        {
            if (!enabled || captures == null || Event.current.type != EventType.Repaint) return;
            Color previous = GUI.color;
            try
            {
                for (int i = 0; i < Zones.Length; i++)
                {
                    Zone zone = Zones[i]; Capture capture = captures[i];
                    Vector2 center = viewport.center + (zone.WorldRect.center - worldCenter) * zoom;
                    Vector2 size = zone.WorldRect.size * zoom; Rect screenRect = new Rect(center - size * .5f, size);
                    capture.Camera.enabled = screenRect.Overlaps(viewport);
                    if (!capture.Camera.enabled) continue;
                    GUI.color = new Color(1f, 1f, 1f, zone.SurfaceOpacity);
                    GUI.DrawTexture(screenRect, capture.Texture, ScaleMode.StretchToFill, true);
                }
            }
            finally { GUI.color = previous; }
        }

        private Capture CreateCapture(Zone zone, int index)
        {
            var capture = new Capture { Material = new Material(sourceMaterial) { name = "OneClickWater_" + zone.Name + "_Runtime" } };
            SetFloat(capture.Material, "_WaveSpeed", zone.WaveSpeed); SetFloat(capture.Material, "_Speed", zone.WaveSpeed * .7f);
            SetVector(capture.Material, "_Wave_Direction", new Vector4(zone.Direction.x, zone.Direction.y, 0f, 0f));
            SetVector(capture.Material, "_Wave_dir", new Vector4(zone.Direction.x, zone.Direction.y, 0f, 0f));
            capture.Root = new GameObject("OneClickWater_" + zone.Name); capture.Root.transform.SetParent(transform, false);
            capture.Root.transform.localPosition = new Vector3(zone.WorldRect.width * .005f, -zone.WorldRect.height * .005f, index * .01f);
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad); surface.layer = WaterLayer; surface.transform.SetParent(capture.Root.transform, false);
            surface.transform.localScale = new Vector3(zone.WorldRect.width * .01f, zone.WorldRect.height * .01f, 1f); surface.GetComponent<Renderer>().sharedMaterial = capture.Material;
            GameObject cameraObject = new GameObject("Camera"); cameraObject.transform.SetParent(capture.Root.transform, false); cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            capture.Camera = cameraObject.AddComponent<Camera>(); capture.Camera.clearFlags = CameraClearFlags.SolidColor; capture.Camera.backgroundColor = Color.clear;
            capture.Camera.cullingMask = 1 << WaterLayer; capture.Camera.orthographic = true; capture.Camera.orthographicSize = zone.WorldRect.height * .005f;
            capture.Camera.aspect = zone.WorldRect.width / zone.WorldRect.height; capture.Camera.nearClipPlane = .1f; capture.Camera.farClipPlane = 20f;
            capture.Camera.allowHDR = false; capture.Camera.allowMSAA = false;
            capture.Texture = new RenderTexture(640, Mathf.Max(96, Mathf.RoundToInt(640f * zone.WorldRect.height / zone.WorldRect.width)), 16, RenderTextureFormat.ARGB32)
            { name = "OneClickWater_" + zone.Name + "_RT", wrapMode = TextureWrapMode.Clamp, antiAliasing = 1 };
            capture.Texture.Create(); capture.Camera.targetTexture = capture.Texture; return capture;
        }

        private static void SetFloat(Material material, string property, float value) { if (material.HasProperty(property)) material.SetFloat(property, value); }
        private static void SetVector(Material material, string property, Vector4 value) { if (material.HasProperty(property)) material.SetVector(property, value); }

        private void OnDestroy()
        {
            if (captures == null) return;
            foreach (Capture capture in captures)
            {
                if (capture == null) continue; if (capture.Camera != null) capture.Camera.targetTexture = null;
                if (capture.Texture != null) { capture.Texture.Release(); Destroy(capture.Texture); }
                if (capture.Material != null) Destroy(capture.Material); if (capture.Root != null) Destroy(capture.Root);
            }
        }
    }
}
