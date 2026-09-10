using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace ESSW.Editorcontroller
{
    [ExecuteAlways, AddComponentMenu("Rendering/Planar Reflections")]
    public class PlanarReflections : MonoBehaviour
    {
        // ---------------- Enums ----------------
      [HideInInspector]  public enum TextureSlot { ID1 = 1, ID2 = 2, ID3 = 3, ID4 = 4 }
        public enum QualityLevel { VeryLow, Low, Medium, Max }

        // ---------------- Inspector ----------------
        [Header("Target")]
        [Tooltip("Global texture slot (maps to _PlanarReflectionsTex1..4 in shaders).")]
        [HideInInspector]public TextureSlot textureSlot = TextureSlot.ID1;

        [Header("Quality")]
        [Tooltip("Resolution scale of the reflection render texture.")]
        public QualityLevel quality = QualityLevel.Max;
    public bool HDR;
    public bool MSAA;

        [Tooltip("Maximum rendering distance for the reflection camera.")]
        public float farClipPlane = 1500f;

        [Header("Rendering")]
        [Tooltip("If enabled, the reflection includes the background/skybox.")]
        public bool renderBackground = true;

        [Tooltip("If enabled, probe renders for Scene View cameras in Edit Mode.")]
        public bool renderInEditor = true;
        
         [Header("Normal")]
        [Tooltip("Use a custom plane normal instead of the object's forward.")]
        public bool useCustomNormal = true;
        [Tooltip("Custom normal direction. If (0,0,0), defaults to Vector3.up.")]
        public Vector3 customNormal = Vector3.zero;

        // ---------------- Internals ----------------
        private GameObject _probeRef;
        private Camera _probe0;
        private Skybox _probeSkybx;
        private readonly Dictionary<Camera, RenderTexture> _camTextureMap = new Dictionary<Camera, RenderTexture>();
        private readonly List<Camera> _ignoredCameras = new List<Camera>();

        // Legacy migration
        [FormerlySerializedAs("targetTextureID")] [SerializeField, HideInInspector] private int _legacyTargetId = 0;
        [FormerlySerializedAs("reflectionsQuality")] [SerializeField, HideInInspector] private float _legacyQuality = -1f;

        // ---------------- Unity Events ----------------
        private void OnValidate()
        {
            // Migrate old texture ID -> enum
            if (_legacyTargetId >= 1 && _legacyTargetId <= 4)
            {
                textureSlot = (TextureSlot)_legacyTargetId;
                _legacyTargetId = 0;
            }

            // Migrate old float quality -> enum
            if (_legacyQuality >= 0f)
            {
                float v = Mathf.Clamp01(_legacyQuality);
                if (v < 0.375f) quality = QualityLevel.VeryLow;
                else if (v < 0.625f) quality = QualityLevel.Low;
                else if (v < 0.875f) quality = QualityLevel.Medium;
                else quality = QualityLevel.Max;

                _legacyQuality = -1f;
            }
        }

        private void OnEnable() 
        {RenderPipelineManager.beginCameraRendering += PreRender;
            ShowURPDepthNoteOnce();
           // Debug.Log("[ESSW] Note : When planar reflections are enabled, URP may log a harmless" + "depth-only warning. This is a known URP limitation and does not affect " + "visuals or performance.");
        } 
        static bool s_InfoShown=false;
        void ShowURPDepthNoteOnce()
        {   

            if (s_InfoShown) return;
            Debug.Log("[ESSW] Note : When planar reflections are enabled, URP may log a harmless" + "depth-only warning. This is a known URP limitation and does not affect" + "visuals or performance.");
            s_InfoShown= true;
        }

        private void OnDisable() => Cleanup();
        private void OnDestroy() => Cleanup();

        // ---------------- Setup & Cleanup ----------------
        private void Cleanup()
        {
            FinalizeProbe();
            RenderPipelineManager.beginCameraRendering -= PreRender;
            CleanupRenderTextures();
        }

        private void InitializeProbe()
        {
            _probeRef = new GameObject($"PlanarReflectionProbe_{GetEntityId()}", typeof(Camera), typeof(Skybox))
            { hideFlags = HideFlags.HideAndDontSave };

            _probe0 = _probeRef.GetComponent<Camera>();
            _probeSkybx = _probeRef.GetComponent<Skybox>();

            _probeSkybx.enabled = false;
            _probeSkybx.material = null;
        }

        private void FinalizeProbe()
        {
            if (_probeRef == null) return;

            if (Application.isEditor) DestroyImmediate(_probeRef);
            else Destroy(_probeRef);

            _probeRef = null;
            _probe0 = null;
            _probeSkybx = null;
        }

        private void CleanupRenderTextures()
        {
            foreach (var texture in _camTextureMap.Values)
            {
                if (texture != null)
                {
                    texture.Release();
                    if (Application.isEditor) DestroyImmediate(texture);
                    else Destroy(texture);
                }
            }
            _camTextureMap.Clear();
        }

        // ---------------- Rendering ----------------
        private bool ShouldSkipCamera(Camera cam)
        {
            return cam == null ||
                   cam.cameraType == CameraType.Reflection ||
                   (!renderInEditor && cam.cameraType == CameraType.SceneView) ||
                   _ignoredCameras.Contains(cam);
        }

        private void PreRender(ScriptableRenderContext context, Camera cam)
        {
            if (Application.isPlaying && SceneManager.GetActiveScene().name.Contains("WorldMap")) return;
            if (ShouldSkipCamera(cam)) return;
            if (_probe0 == null) InitializeProbe();

            Vector3 normal = GetNormal();
            UpdateProbeSettings(cam);
            CreateRenderTexture(cam);
            UpdateProbeTransform(cam, normal);
            CalculateObliqueProjection(normal);

            if (_probe0.targetTexture == null || !_probe0.targetTexture.IsCreated())
            {
                Debug.LogWarning("PlanarReflections: reflection render texture not ready");
                return;
            }

#pragma warning disable CS0618
            UniversalRenderPipeline.RenderSingleCamera(context, _probe0);
#pragma warning restore CS0618

            Shader.SetGlobalTexture($"_PlanarReflectionsTex{(int)textureSlot}", _probe0.targetTexture);
        }

        private void UpdateProbeSettings(Camera cam)
        {
            _probe0.CopyFrom(cam);

            _probe0.enabled = false;
            _probe0.cameraType = CameraType.Reflection;
            _probe0.usePhysicalProperties = false;
            _probe0.farClipPlane = farClipPlane;

            _probe0.forceIntoRenderTexture = true;
            _probe0.allowMSAA = MSAA;
            _probe0.allowHDR = HDR;

            if (renderBackground)
            {
                _probe0.clearFlags = CameraClearFlags.Skybox;

                // Try camera skybox, else RenderSettings
                Material skyMat = null;
                if (cam.TryGetComponent(out Skybox camSkybox) && camSkybox.material != null)
                    skyMat = camSkybox.material;
                else if (RenderSettings.skybox != null)
                    skyMat = RenderSettings.skybox;

                if (!_probe0.TryGetComponent(out Skybox probeSkybox))
                    probeSkybox = _probe0.gameObject.AddComponent<Skybox>();

                probeSkybox.material = skyMat;
                probeSkybox.enabled = skyMat != null;
            }
            else
            {
                _probe0.clearFlags = CameraClearFlags.SolidColor;
                _probe0.backgroundColor = Color.clear;

                if (_probe0.TryGetComponent(out Skybox probeSkybox))
                    probeSkybox.enabled = false;
            }
        }

        private float QualityToScale(QualityLevel q)
        {
            switch (q)
            {
                case QualityLevel.VeryLow: return 0.25f;
                case QualityLevel.Low: return 0.5f;
                case QualityLevel.Medium: return 0.75f;
                default: return 1f;
            }
        }

        private void CreateRenderTexture(Camera cam)
        {
            float scale = QualityToScale(quality);
            int width = Mathf.Max(8, Mathf.RoundToInt(cam.pixelWidth * scale));
            int height = Mathf.Max(8, Mathf.RoundToInt(cam.pixelHeight * scale));

            if (!_camTextureMap.TryGetValue(cam, out var texture) ||
                texture == null ||
                texture.width != width ||
                texture.height != height)
            {
                if (texture != null)
                {
                    texture.Release();
                    _camTextureMap.Remove(cam);
                }

                texture = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR)
                {
                    name = $"PlanarReflection_{cam.name}_{GetEntityId()}",
                    useMipMap = false,
                    autoGenerateMips = false,
                    depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat_S8_UInt
                };

                texture.Create();
                _camTextureMap[cam] = texture;
            }

            _probe0.targetTexture = texture;
        }

        private Vector3 GetNormal()
        {
            if (!useCustomNormal) return transform.forward;
            return customNormal == Vector3.zero ? Vector3.up : customNormal.normalized;
        }

        private void UpdateProbeTransform(Camera cam, Vector3 normal)
        {
            Vector3 positionOffset = cam.transform.position - transform.position;
            Vector3 projectedOffset = normal * Vector3.Dot(normal, positionOffset);
            _probe0.transform.position = cam.transform.position - 2f * projectedOffset;

            Vector3 reflectedForward = Vector3.Reflect(cam.transform.forward, normal);
            Vector3 reflectedUp = Vector3.Reflect(cam.transform.up, normal);
            _probe0.transform.rotation = Quaternion.LookRotation(reflectedForward, reflectedUp);
        }

        private void CalculateObliqueProjection(Vector3 normal)
        {
            Matrix4x4 viewMatrix = _probe0.worldToCameraMatrix;
            Vector3 viewPosition = viewMatrix.MultiplyPoint(transform.position);
            Vector3 viewNormal = viewMatrix.MultiplyVector(normal).normalized;

            Vector4 clipPlane = new Vector4(
                viewNormal.x,
                viewNormal.y,
                viewNormal.z,
                -Vector3.Dot(viewPosition, viewNormal));

            _probe0.projectionMatrix = _probe0.CalculateObliqueMatrix(clipPlane);
        }

        // ---------------- API ----------------
        public void IgnoreCamera(Camera cam)
        {
            if (cam != null && !_ignoredCameras.Contains(cam))
                _ignoredCameras.Add(cam);
        }

        public void UnignoreCamera(Camera cam)
        {
            if (cam != null)
                _ignoredCameras.Remove(cam);
        }

        public void ClearIgnoredList() => _ignoredCameras.Clear();
        public bool IsIgnoring(Camera cam) => cam != null && _ignoredCameras.Contains(cam);

        // ---------------- Helpers ----------------
        public static PlanarReflections[] FindProbesRenderingTo(int id)
        {
            var allProbes = FindObjectsByType<PlanarReflections>(FindObjectsSortMode.None);
            var list = new List<PlanarReflections>();
            foreach (var probe in allProbes)
            {
                if ((int)probe.textureSlot == id) list.Add(probe);
            }
            return list.ToArray();
        }

        public static PlanarReflections FindProbeRenderingTo(int id)
        {
            var allProbes = FindObjectsByType<PlanarReflections>(FindObjectsSortMode.None);
            foreach (var probe in allProbes)
            {
                if ((int)probe.textureSlot == id) return probe;
            }
            return null;
        }
    }
}
