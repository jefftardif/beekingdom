using UnityEngine;

namespace BeeKingdom.Playground
{
    // M073B-CL: hosts the Tazo_fx "Realistic Waterfall Prefab" instance(s) and
    // their dedicated render camera. The World Map's terrain
    // (WorldMapMmoFullscreenFoundationBootstrap) is drawn every frame via
    // OnGUI, which always paints on top of anything the Main Camera renders in
    // 3D - a 3D prefab placed directly in the scene would otherwise be fully
    // hidden behind the terrain blit every frame. Instead, this waterfall lives
    // on its own culling-mask layer and is captured to an offscreen
    // RenderTexture by a dedicated camera; WorldMapMmoFullscreenFoundationBootstrap
    // composites that texture into its own OnGUI draw sequence at the screen
    // rect matching WorldRect below, using the same WorldToScreen conversion as
    // the terrain tiles so the waterfall pans/zooms in perfect sync with the map.
    public sealed class WorldMapWaterfallFxBootstrap : MonoBehaviour
    {
        public const int WaterfallLayer = 10;

        // World-space rect of the painted waterfall on the wave5method_12288
        // preview terrain package, in the same world-unit coordinate system as
        // WorldMapWave6StreamingTileProvider (TileSize=512, OriginChunk=(7,7)).
        // Located by scanning all 2500 tiles for a local-brightness-turbulence +
        // foam-area-fraction signature (see Docs/AI/Missions M073B-CL report):
        // tiles R02C20/R02C21 stood out clearly (58%/51% foam-area fraction vs.
        // under 20% for the next-highest non-adjacent tile). chunkX 27-28,
        // chunkY 9 -> worldRect (13824, 4608, 1024, 512).
        public static readonly Rect WorldRect = new Rect(13824f, 4608f, 1024f, 512f);

        [SerializeField] private Transform waterfallRoot;
        [SerializeField] private int renderTextureWidth = 640;
        [SerializeField] private int renderTextureHeight = 320;
        [SerializeField] private float cameraDistance = 6.5f;
        [SerializeField] private float cameraFieldOfView = 60f;

        private Camera renderCamera;
        private RenderTexture renderTexture;

        public RenderTexture Texture => renderTexture;

        private void Awake()
        {
            if (waterfallRoot == null) waterfallRoot = transform;
            SetLayerRecursive(waterfallRoot.gameObject, WaterfallLayer);
            EnsureRenderCamera();
        }

        private void EnsureRenderCamera()
        {
            Transform existing = transform.Find("WaterfallRenderCamera");
            GameObject camGo = existing != null ? existing.gameObject : new GameObject("WaterfallRenderCamera");
            camGo.transform.SetParent(transform, false);
            // Vendor demo camera sits 3 world units from a single ~2-unit-wide
            // waterfall at FOV 60. We frame three side-by-side instances
            // (~5.4 units combined width) from further back so all three fit.
            camGo.transform.localPosition = new Vector3(0f, 0.05f, -cameraDistance);
            camGo.transform.localRotation = Quaternion.identity;

            renderCamera = camGo.GetComponent<Camera>();
            if (renderCamera == null) renderCamera = camGo.AddComponent<Camera>();
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            renderCamera.cullingMask = 1 << WaterfallLayer;
            renderCamera.fieldOfView = cameraFieldOfView;
            renderCamera.nearClipPlane = 0.05f;
            renderCamera.farClipPlane = cameraDistance + 10f;
            renderCamera.orthographic = false;
            renderCamera.depth = -10f;
            renderCamera.allowHDR = false;
            renderCamera.allowMSAA = false;

            if (renderTexture != null) renderTexture.Release();
            renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "WaterfallFX_RT",
                antiAliasing = 1,
            };
            renderTexture.Create();
            renderCamera.targetTexture = renderTexture;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        private void OnDestroy()
        {
            if (renderCamera != null) renderCamera.targetTexture = null;
            if (renderTexture != null)
            {
                renderTexture.Release();
                renderTexture = null;
            }
        }
    }
}
