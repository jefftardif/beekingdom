using UnityEngine;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// M074-CX single-basin evaluation overlay for the imported
    /// "One Click Add Water - Stylized Water Shader" asset
    /// (Assets/Houidisoft technology/One Click Add Water .../Shader/water
    /// shader ocean.shadergraph). Deliberately separate from
    /// WorldMapWaterfallFxBootstrap - the waterfall system is finished per
    /// this mission's brief and must not be touched.
    ///
    /// LIMITATION 1 - no directional flow (documented per mission brief
    /// rather than worked around): the shader exposes wave height/length/
    /// speed and a single scalar "_water_movement_speed" for the normal-map
    /// pan, but NO direction vector property at all (confirmed via
    /// assets-shader-get-data - 32 properties total, none of them a flow
    /// direction). The normal-map pan axis is baked into the Shader Graph
    /// itself. A real river current that follows the painted channel's bend
    /// is not achievable with this asset as-is.
    ///
    /// LIMITATION 2 - the bigger blocker, found during Play Mode
    /// verification: captured through this World Map's required offscreen-
    /// camera + RenderTexture pipeline, the water surface renders as a
    /// perfectly FLAT, UNIFORM white (or grey, depending on Metallic) across
    /// its entire area - no foam pattern, no ripple, no light variation at
    /// all, confirmed by sampling dozens of RT pixels at every corner and
    /// center. Ruled out one at a time: it isn't a missing-texture fallback
    /// (assigned a real foam.png and the output didn't change), it isn't the
    /// "_ENABLE_WAVES" keyword being off (enabled it explicitly, no change),
    /// it isn't Metallic/Smoothness (changing them shifted the flat color
    /// from grey to white, proving the material IS being evaluated, just
    /// never with any spatial variation). A real texture-sampled result
    /// could not be perfectly identical at every one of ~35 sampled points
    /// spanning the whole surface; the most likely explanation is a
    /// Divide/gradient node in the graph keyed on Scene Depth producing a
    /// degenerate value (this isolated single-quad offscreen camera has no
    /// Opaque/Depth Texture support - disabled project-wide - and no other
    /// 3D geometry in its layer to generate real depth variation even if it
    /// did), which most GPUs saturate to white. Per the mission brief's own
    /// instruction ("ne construis pas encore un gros système maison"), this
    /// was not chased further into shader-graph surgery - it's a CEO
    /// decision whether this asset is worth adapting further or dropping.
    /// </summary>
    public sealed class WorldMapOneClickWaterPrototype : MonoBehaviour
    {
        private const int WaterLayer = 11;
        // M074-CX: calm river bend just above the waterfall crest (tile
        // R04C07 of the wave5method_12288 preview package), shows both a
        // wider pool-like bend and a narrower current-rippled channel in one
        // shot. Height capped at Y=6140 to stay clear of
        // WorldMapWaterfallFxBootstrap.WorldRect (starts at Y=6144) - the
        // waterfall system is finished and must not be touched or visually
        // collided with.
        private static readonly Rect PrototypeWorldRect = new Rect(7300f, 5800f, 760f, 340f);
        private Camera captureCamera;
        private RenderTexture captureTexture;
        private Material waterMaterial;
        private GameObject surface;

        private void Awake()
        {
            waterMaterial = Resources.Load<Material>("water");
            if (waterMaterial == null)
            {
                Debug.LogError("[OneClickWater] Resources/water.mat was not found.", this);
                enabled = false;
                return;
            }

            waterMaterial = new Material(waterMaterial) { name = "OneClickWater_Prototype_Runtime" };
            // CORRECTED (M074-CX): "_WaveSpeed"/"_Speed"/"_Wave_Direction"/
            // "_Wave_dir" are not real properties of "Shader Graphs/water
            // shader ocean" (confirmed via assets-shader-get-data - the
            // shader exposes 32 properties, none of them a direction vector).
            // SetFloat/SetVector below were silent no-ops guarded by
            // HasProperty, giving a false impression that a flow direction
            // had been wired up. The real property names are used now; see
            // the class-level remark for why directional flow itself isn't
            // available at all without editing the shader graph.
            SetFloat("_Wave_Speed", 0.12f);
            SetFloat("_water_movement_speed", 0.08f);
            SetFloat("_Normal_strength", 0.6f);
            SetFloat("_Wave_Height", 0.05f);
            SetFloat("_Foam_Amount", 0.25f);
            SetFloat("_Foam_Intensity", 0.08f);
            SetFloat("_Metallic", 0f); // full Metallic depends on environment reflections this isolated layer can't provide
            // Refraction/reflection sample Scene Color from this isolated
            // offscreen camera, which renders nothing behind the quad (no
            // Opaque Texture support enabled project-wide, and nothing to
            // reflect in this empty layer anyway) - zeroed out rather than
            // left at their demo-scene defaults tuned for a real 3D scene.
            SetFloat("_Refraction_power", 0f);
            SetFloat("_Reflect_power", 0f);
            // Base "water.mat" ships with no Foam Texture assigned at all
            // (null) - see LIMITATION 2 above for why this alone doesn't fix
            // the flat-white result, but a missing texture is still worth
            // fixing on principle.
            var foamTexture = Resources.Load<Texture2D>("foam3");
            if (foamTexture != null) waterMaterial.SetTexture("_Foam_Texture", foamTexture);

            surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "OneClickWater_Surface_Prototype";
            surface.layer = WaterLayer;
            surface.transform.SetParent(transform, false);
            surface.transform.localPosition = new Vector3(PrototypeWorldRect.width * 0.005f, -PrototypeWorldRect.height * 0.005f, 0f);
            surface.transform.localScale = new Vector3(PrototypeWorldRect.width * 0.01f, PrototypeWorldRect.height * 0.01f, 1f);
            surface.GetComponent<Renderer>().sharedMaterial = waterMaterial;

            GameObject cameraObject = new GameObject("OneClickWater_CaptureCamera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = new Vector3(PrototypeWorldRect.width * 0.005f, -PrototypeWorldRect.height * 0.005f, -10f);
            captureCamera = cameraObject.AddComponent<Camera>();
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = Color.clear;
            captureCamera.cullingMask = 1 << WaterLayer;
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = PrototypeWorldRect.height * 0.005f;
            captureCamera.aspect = PrototypeWorldRect.width / PrototypeWorldRect.height;
            captureCamera.nearClipPlane = 0.1f;
            captureCamera.farClipPlane = 20f;
            captureCamera.allowHDR = false;
            captureCamera.allowMSAA = false;
            captureTexture = new RenderTexture(640, 304, 16, RenderTextureFormat.ARGB32)
            {
                name = "OneClickWater_Prototype_RT",
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            captureTexture.Create();
            captureCamera.targetTexture = captureTexture;

            // The shader's normal-mapped specular highlight needs a light to
            // react to; this isolated layer has none of the World Map's own
            // lighting. A single directional light scoped to WaterLayer only
            // (cullingMask) gives the ripples something to catch without
            // touching any other lighting in the scene.
            var lightObject = new GameObject("OneClickWater_Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.cullingMask = 1 << WaterLayer;
            light.intensity = 1.2f;
            light.shadows = LightShadows.None;
        }

        public void DrawOverlay(Vector2 worldCenter, float zoom, Rect viewport)
        {
            if (!enabled || captureTexture == null || Event.current.type != EventType.Repaint) return;
            Vector2 center = viewport.center + (PrototypeWorldRect.center - worldCenter) * zoom;
            Vector2 size = PrototypeWorldRect.size * zoom;
            Rect screenRect = new Rect(center - size * 0.5f, size);
            if (!screenRect.Overlaps(viewport))
            {
                captureCamera.enabled = false;
                return;
            }
            captureCamera.enabled = true;
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.16f);
            GUI.DrawTexture(screenRect, captureTexture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        private void SetFloat(string property, float value)
        {
            if (waterMaterial.HasProperty(property)) waterMaterial.SetFloat(property, value);
        }

        private void SetVector(string property, Vector4 value)
        {
            if (waterMaterial.HasProperty(property)) waterMaterial.SetVector(property, value);
        }

        private void OnDestroy()
        {
            if (captureCamera != null) captureCamera.targetTexture = null;
            if (captureTexture != null)
            {
                captureTexture.Release();
                Destroy(captureTexture);
            }
            if (waterMaterial != null) Destroy(waterMaterial);
            if (surface != null) Destroy(surface);
        }
    }
}
