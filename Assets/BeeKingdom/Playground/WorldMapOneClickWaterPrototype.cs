using UnityEngine;

namespace BeeKingdom.Playground
{
    /// <summary>Single-basin evaluation overlay for the imported One Click Add Water asset.</summary>
    public sealed class WorldMapOneClickWaterPrototype : MonoBehaviour
    {
        private const int WaterLayer = 11;
        private static readonly Rect PrototypeWorldRect = new Rect(7300f, 5800f, 760f, 360f);
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
            SetFloat("_WaveSpeed", 0.12f);
            SetFloat("_Speed", 0.08f);
            SetVector("_Wave_Direction", new Vector4(0.85f, 0.18f, 0f, 0f));
            SetVector("_Wave_dir", new Vector4(0.85f, 0.18f, 0f, 0f));

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
