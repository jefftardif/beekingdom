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
        //
        // CORRECTED (M073B-CL live Play Mode debugging, 2026-09-09): the first
        // pass located tiles R02C20/R02C21 via an unconstrained full-map scan -
        // a real waterfall, but NOT the one near the CEO's own hive/play area.
        // Cross-checked against the CEO's actual live worldCenter while
        // standing on the waterfall (~7741, 6326) against a rescan restricted
        // to the observed play region: tiles R05C07/R05C08 (chunkX 14-15,
        // chunkY 12) match almost exactly. worldRect (7168, 6144, 1024, 512).
        public static readonly Rect WorldRect = new Rect(7168f, 6144f, 1024f, 512f);

        [SerializeField] private Transform waterfallRoot;
        [SerializeField] private int renderTextureWidth = 640;
        [SerializeField] private int renderTextureHeight = 320;
        [SerializeField] private float cameraDistance = 3.2f;
        [SerializeField] private float cameraFieldOfView = 60f;

        private Camera renderCamera;
        private RenderTexture renderTexture;

        public RenderTexture Texture => renderTexture;

        private void Awake()
        {
            Debug.Log("[WaterfallFX] Awake() running on " + gameObject.name);
            if (waterfallRoot == null) waterfallRoot = transform;
            SetLayerRecursive(waterfallRoot.gameObject, WaterfallLayer);
            EnsureRenderCamera();
            Debug.Log("[WaterfallFX] Awake() done. camera=" + (renderCamera != null) + " texture=" + (renderTexture != null) + " camWorldPos=" + (renderCamera != null ? renderCamera.transform.position.ToString() : "n/a"));
        }

        private void EnsureRenderCamera()
        {
            Transform existing = transform.Find("WaterfallRenderCamera");
            GameObject camGo = existing != null ? existing.gameObject : new GameObject("WaterfallRenderCamera");
            camGo.transform.SetParent(transform, false);

            // CORRECTED (M073B-CL live Play Mode debugging, 2026-09-09): a fixed
            // local Y of 0.05 assumed the waterfall mesh's pivot sat at its
            // vertical center, but sold3_waterfall_high's pivot is at its base -
            // the mesh actually spans roughly Y=[0.02, 2.84]. With the camera
            // aimed at Y=0.05 the mesh's upper ~95% fell outside the frustum,
            // leaving only a tiny sliver in the render texture (measured via
            // RT pixel inspection: ~5% non-transparent coverage in a small
            // off-center bbox) that then got stretched across the full screen
            // rect - the "flat grey blob, no visible detail" the CEO reported.
            // Framing is now computed from the actual combined renderer bounds
            // under waterfallRoot instead of a hardcoded offset, so it self-
            // adjusts if the hosted prefab/instance changes.
            // Only MeshRenderers (the stable "waterfall_meash*" curtain geometry)
            // feed the framing bounds. ParticleSystemRenderers (fog/splash) can
            // report degenerate/zero bounds at their emitter origin before their
            // first simulation tick, which skewed the computed center wildly
            // off from the actual waterfall mesh on the very first Awake() frame.
            Vector3 centerLocal = Vector3.zero;
            MeshRenderer[] renderers = waterfallRoot.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length > 0)
            {
                Bounds combined = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) combined.Encapsulate(renderers[i].bounds);
                centerLocal = transform.InverseTransformPoint(combined.center);
            }

            camGo.transform.localPosition = new Vector3(centerLocal.x, centerLocal.y, centerLocal.z - cameraDistance);
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
