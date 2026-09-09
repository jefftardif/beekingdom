using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    [System.Serializable]
    public struct WaterfallDefinition
    {
        public string id;
        public Rect worldRect;
        public float rotationDegrees;
        public Vector2 scale;
        public Vector2 flowDirection;
        [Range(0f, 1f)] public float opacity;
    }

    // Capture separately because IMGUI paints the terrain over the main camera.
    public sealed class WorldMapWaterfallFxBootstrap : MonoBehaviour
    {
        public const int WaterfallLayer = 10;
        // Measured on R04/05 C07/08, excluding their two-pixel gutters.
        public static readonly Rect WorldRect = new Rect(7168f, 5962f, 940f, 630f);

        [SerializeField] private Transform waterfallRoot;
        [SerializeField] private int renderTextureWidth = 640;
        [SerializeField] private int renderTextureHeight = 376;
        [SerializeField, Range(0f, 1f)] private float opacity = 0.82f;
        [SerializeField] private List<WaterfallDefinition> definitions = new List<WaterfallDefinition>();

        private Camera renderCamera;
        private RenderTexture renderTexture;
        private Material surfaceMaterial;
        private Mesh surfaceMesh;
        private GameObject surfaceObject;

        public RenderTexture Texture => renderTexture;
        public IReadOnlyList<WaterfallDefinition> Definitions => definitions;

        private void Awake()
        {
            if (waterfallRoot == null) waterfallRoot = transform;
            EnsureDefinitions();
            if (CreateSurface()) EnsureRenderCamera();
        }

        private void EnsureDefinitions()
        {
            if (definitions.Count > 0) return;
            definitions.Add(new WaterfallDefinition
            {
                id = "reference-r05c07-c08",
                worldRect = new Rect(7168f, 5962f, 940f, 630f),
                rotationDegrees = 0f,
                scale = Vector2.one,
                flowDirection = Vector2.down,
                opacity = opacity
            });
            // Catalog entries for the other visible falls in the canonical map.
            definitions.Add(new WaterfallDefinition { id = "north-west-r01c08", worldRect = new Rect(5128f, 228f, 875f, 690f), rotationDegrees = 0f, scale = Vector2.one, flowDirection = Vector2.down, opacity = opacity });
            definitions.Add(new WaterfallDefinition { id = "west-r03c04", worldRect = new Rect(1930f, 3020f, 1080f, 750f), rotationDegrees = 0f, scale = Vector2.one, flowDirection = Vector2.down, opacity = opacity });
            definitions.Add(new WaterfallDefinition { id = "central-r04c10", worldRect = new Rect(4200f, 3670f, 835f, 710f), rotationDegrees = 0f, scale = Vector2.one, flowDirection = Vector2.down, opacity = opacity });
            definitions.Add(new WaterfallDefinition { id = "south-east-r06c12", worldRect = new Rect(5750f, 4830f, 960f, 790f), rotationDegrees = 0f, scale = Vector2.one, flowDirection = Vector2.down, opacity = opacity });
            definitions.Add(new WaterfallDefinition { id = "east-r05c14", worldRect = new Rect(7630f, 5160f, 420f, 710f), rotationDegrees = 0f, scale = Vector2.one, flowDirection = Vector2.down, opacity = opacity });
            definitions.Add(new WaterfallDefinition { id = "lower-west-r07c09", worldRect = new Rect(5050f, 6490f, 500f, 625f), rotationDegrees = 0f, scale = Vector2.one, flowDirection = Vector2.down, opacity = opacity });
            definitions.Add(new WaterfallDefinition { id = "lower-central-r08c08", worldRect = new Rect(4360f, 7670f, 835f, 770f), rotationDegrees = 0f, scale = Vector2.one, flowDirection = Vector2.down, opacity = opacity });
            definitions.Add(new WaterfallDefinition { id = "south-r09c11", worldRect = new Rect(6180f, 8330f, 460f, 580f), rotationDegrees = 0f, scale = Vector2.one, flowDirection = Vector2.down, opacity = opacity });
            definitions.Add(new WaterfallDefinition { id = "mountain-r01c28", worldRect = new Rect(12360f, 320f, 690f, 600f), rotationDegrees = 0f, scale = Vector2.one, flowDirection = Vector2.down, opacity = opacity });
        }

        private bool CreateSurface()
        {
            Shader shader = Resources.Load<Shader>("WaterfallFX/WaterfallMapSurface");
            Material source = Resources.Load<Material>("WaterfallFX/Materials/fulid_01_urp");
            Material foam = Resources.Load<Material>("WaterfallFX/Materials/fulid_alpha_01_urp");
            if (shader == null || !shader.isSupported || source == null || foam == null)
            {
                Debug.LogError("[WaterfallFX] Missing supported map shader or waterfall materials.", this);
                return false;
            }
            // Preserve authored prefab instances in Edit Mode. Their rectangular
            // curtains and duplicate emitters do not follow this painted cliff.
            foreach (Transform child in waterfallRoot) child.gameObject.SetActive(false);
            surfaceMaterial = new Material(shader) { name = "WaterfallMapSurface_Runtime" };
            surfaceMaterial.SetTexture("_MainTex", source.mainTexture);
            surfaceMaterial.SetTexture("_FoamTex", foam.mainTexture);
            surfaceMaterial.SetFloat("_Opacity", opacity);
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            // Continuous UVs per natural arm, separated only by the real rock.
            AddRibbon(vertices, uv, triangles,
                new[] { new Vector2(20, 526), new Vector2(90, 520), new Vector2(165, 510), new Vector2(245, 470), new Vector2(285, 462), new Vector2(322, 515) },
                new[] { new Vector2(115, 926), new Vector2(195, 945), new Vector2(272, 930), new Vector2(330, 920), new Vector2(365, 891), new Vector2(398, 865) });
            AddRibbon(vertices, uv, triangles,
                new[] { new Vector2(382, 424), new Vector2(460, 430), new Vector2(550, 445), new Vector2(632, 432), new Vector2(719, 414), new Vector2(786, 423), new Vector2(823, 450) },
                new[] { new Vector2(434, 875), new Vector2(510, 871), new Vector2(603, 850), new Vector2(702, 829), new Vector2(794, 800), new Vector2(865, 767), new Vector2(908, 717) });
            surfaceMesh = new Mesh { name = "WaterfallMapSurface_Runtime" };
            surfaceMesh.SetVertices(vertices);
            surfaceMesh.SetUVs(0, uv);
            surfaceMesh.SetTriangles(triangles, 0);
            surfaceMesh.RecalculateBounds();
            surfaceObject = new GameObject("WaterfallMapSurface", typeof(MeshFilter), typeof(MeshRenderer));
            surfaceObject.layer = WaterfallLayer;
            surfaceObject.transform.SetParent(transform, false);
            surfaceObject.GetComponent<MeshFilter>().sharedMesh = surfaceMesh;
            MeshRenderer renderer = surfaceObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = surfaceMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return true;
        }

        private static void AddRibbon(List<Vector3> vertices, List<Vector2> uv, List<int> triangles, Vector2[] top, Vector2[] bottom)
        {
            const int rows = 12;
            int start = vertices.Count;
            for (int column = 0; column < top.Length; column++)
            {
                for (int row = 0; row <= rows; row++)
                {
                    float t = row / (float)rows;
                    Vector2 point = Vector2.Lerp(top[column], bottom[column], t);
                    // World map Y points down; capture camera Y points up.
                    vertices.Add(new Vector3(point.x / 100f, -(point.y - 330f) / 100f, 0f));
                    uv.Add(new Vector2(column / (float)(top.Length - 1), 1f - t));
                    if (column == 0 || row == 0) continue;
                    int d = start + column * (rows + 1) + row;
                    int a = d - rows - 2;
                    triangles.Add(a); triangles.Add(d - 1); triangles.Add(d);
                    triangles.Add(a); triangles.Add(d); triangles.Add(a + 1);
                }
            }
        }

        private void EnsureRenderCamera()
        {
            var camGo = new GameObject("WaterfallRenderCamera", typeof(Camera));
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(WorldRect.width / 200f, -WorldRect.height / 200f, -10f);
            renderCamera = camGo.GetComponent<Camera>();
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.cullingMask = 1 << WaterfallLayer;
            renderCamera.nearClipPlane = 0.1f;
            renderCamera.farClipPlane = 20f;
            renderCamera.orthographic = true;
            renderCamera.orthographicSize = WorldRect.height / 200f;
            renderCamera.depth = -10f;
            renderCamera.allowHDR = false;
            renderCamera.allowMSAA = false;
            // Match the map rect exactly, without perspective or cropped bounds.
            int width = Mathf.Max(64, renderTextureWidth);
            int height = Mathf.Max(Mathf.Max(64, renderTextureHeight), Mathf.RoundToInt(width * WorldRect.height / WorldRect.width));
            renderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            {
                name = "WaterfallFX_RT", antiAliasing = 1, wrapMode = TextureWrapMode.Clamp
            };
            renderTexture.Create();
            renderCamera.targetTexture = renderTexture;
            renderCamera.aspect = WorldRect.width / WorldRect.height;
        }

        private void OnDestroy()
        {
            if (renderCamera != null)
            {
                renderCamera.targetTexture = null;
                Destroy(renderCamera.gameObject);
            }
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            if (surfaceObject != null) Destroy(surfaceObject);
            if (surfaceMesh != null) Destroy(surfaceMesh);
            if (surfaceMaterial != null) Destroy(surfaceMaterial);
        }
    }
}
