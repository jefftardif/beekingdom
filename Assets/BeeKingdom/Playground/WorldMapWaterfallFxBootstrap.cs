using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    [Serializable]
    public struct WaterfallWorldRect
    {
        public float x;
        public float y;
        public float width;
        public float height;
        public Vector2 size => new Vector2(width, height);
        public Vector2 center => new Vector2(x + width * 0.5f, y + height * 0.5f);
        public override string ToString() => new Rect(x, y, width, height).ToString();
    }

    [Serializable]
    public sealed class WaterfallRibbon
    {
        // Local map units, Y down; corresponding columns follow the water.
        public Vector2[] top;
        public Vector2[] bottom;
    }

    [Serializable]
    public sealed class WaterfallDefinition
    {
        public string id;
        public WaterfallWorldRect worldRect;
        public Vector2 scale = Vector2.one;
        public float rotationDegrees;
        // Image coordinates: (0,1) flows from lip to foot.
        public Vector2 flowDirection = Vector2.up;
        public float opacity = 0.92f;
        public Vector2 animationSpeed = new Vector2(0.24f, 0.31f);
        public Vector4 feather = new Vector4(0.065f, 0.065f, 0.15f, 0.12f);
        public int textureWidth = 640;
        public int minimumTextureHeight = 64;
        public WaterfallRibbon[] ribbons;
        public Vector2 ribbonOrigin;
    }

    [Serializable]
    public sealed class WaterfallCatalog
    {
        public WaterfallDefinition[] waterfalls;
    }

    public sealed class WorldMapWaterfallFxBootstrap : MonoBehaviour
    {
        public const int WaterfallLayer = 10;
        [SerializeField] private Transform waterfallRoot;
        [SerializeField] private TextAsset catalog;
        [SerializeField] private Vector2 worldOffset;
        private WaterfallDefinition[] definitions = Array.Empty<WaterfallDefinition>();
        private Capture[] captures;
        private Material sourceMaterial;
        private Material foamMaterial;
        private Shader shader;
        public IReadOnlyList<WaterfallDefinition> Definitions => definitions;

        private sealed class Capture
        {
            public GameObject root;
            public Camera camera;
            public Mesh mesh;
            public Material material;
            public RenderTexture texture;
        }

        private void Awake()
        {
            if (catalog == null) catalog = Resources.Load<TextAsset>("WaterfallFX/WaterfallCatalog");
            shader = Resources.Load<Shader>("WaterfallFX/WaterfallMapSurface");
            sourceMaterial = Resources.Load<Material>("WaterfallFX/Materials/fulid_01_urp");
            foamMaterial = Resources.Load<Material>("WaterfallFX/Materials/fulid_alpha_01_urp");
            if (catalog == null || shader == null || !shader.isSupported || sourceMaterial == null || foamMaterial == null)
            {
                Debug.LogError("[WaterfallFX] Missing catalogue, supported shader or source materials.", this);
                enabled = false;
                return;
            }
            definitions = JsonUtility.FromJson<WaterfallCatalog>(catalog.text).waterfalls ?? Array.Empty<WaterfallDefinition>();
            foreach (WaterfallDefinition definition in definitions)
            {
                bool valid = definition.worldRect.width > 0f && definition.worldRect.height > 0f &&
                    definition.scale.x > 0f && definition.scale.y > 0f && definition.ribbons != null;
                if (definition.ribbons != null)
                    foreach (WaterfallRibbon ribbon in definition.ribbons)
                        valid &= ribbon.top != null && ribbon.bottom != null && ribbon.top.Length >= 2 && ribbon.top.Length == ribbon.bottom.Length;
                if (valid) continue;
                Debug.LogError("[WaterfallFX] Invalid rectangle or ribbon: " + definition.id, this);
                enabled = false;
                return;
            }
            captures = new Capture[definitions.Length];
            if (waterfallRoot == null) waterfallRoot = transform;
            foreach (Transform child in waterfallRoot) child.gameObject.SetActive(false);
        }

        // Called in the host's OnGUI, immediately after its terrain tiles.
        public void DrawOverlay(Vector2 worldCenter, float zoom, Rect viewport)
        {
            if (!isActiveAndEnabled || captures == null || Event.current.type != EventType.Repaint) return;
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            try
            {
                GUI.color = Color.white; // The shader applies opacity exactly once.
                for (int i = 0; i < definitions.Length; i++)
                {
                    WaterfallDefinition definition = definitions[i];
                    Vector2 center = viewport.center + (definition.worldRect.center + worldOffset - worldCenter) * zoom;
                    Vector2 size = Vector2.Scale(definition.worldRect.size, definition.scale) * zoom;
                    Rect projected = new Rect(center - size * 0.5f, size);
                    float angle = definition.rotationDegrees * Mathf.Deg2Rad;
                    Vector2 bounds = new Vector2(Mathf.Abs(size.x * Mathf.Cos(angle)) + Mathf.Abs(size.y * Mathf.Sin(angle)),
                        Mathf.Abs(size.x * Mathf.Sin(angle)) + Mathf.Abs(size.y * Mathf.Cos(angle)));
                    if (!new Rect(center - bounds * 0.5f, bounds).Overlaps(viewport))
                    {
                        if (captures[i] != null) captures[i].camera.enabled = false;
                        continue;
                    }
                    if (captures[i] == null) captures[i] = CreateCapture(definition, i);
                    Capture capture = captures[i];
                    capture.camera.enabled = true;
                    GUI.matrix = oldMatrix;
                    GUIUtility.RotateAroundPivot(definition.rotationDegrees, center);
                    GUI.DrawTexture(projected, capture.texture, ScaleMode.StretchToFill, true);
                }
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
        }

        private Capture CreateCapture(WaterfallDefinition definition, int index)
        {
            var capture = new Capture();
            capture.root = new GameObject("WaterfallCapture_" + definition.id);
            capture.root.transform.SetParent(transform, false);
            // Separate depth slabs prevent captures seeing other waterfalls.
            capture.root.transform.position = new Vector3(0f, 0f, 500f + index * 100f);
            capture.material = new Material(shader) { name = definition.id };
            capture.material.SetTexture("_MainTex", sourceMaterial.mainTexture);
            capture.material.SetTexture("_FoamTex", foamMaterial.mainTexture);
            capture.material.SetFloat("_Opacity", definition.opacity);
            capture.material.SetVector("_Flow", new Vector4(-definition.flowDirection.x, definition.flowDirection.y, definition.animationSpeed.x, definition.animationSpeed.y));
            capture.material.SetVector("_Feather", definition.feather);
            if (definition.flowDirection != Vector2.up || definition.animationSpeed != new Vector2(0.24f, 0.31f) ||
                definition.feather != new Vector4(0.065f, 0.065f, 0.15f, 0.12f))
                capture.material.EnableKeyword("WATERFALL_CUSTOM_FLOW");
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            foreach (WaterfallRibbon ribbon in definition.ribbons) AddRibbon(vertices, uv, triangles, ribbon, definition.ribbonOrigin);
            capture.mesh = new Mesh { name = definition.id };
            capture.mesh.SetVertices(vertices);
            capture.mesh.SetUVs(0, uv);
            capture.mesh.SetTriangles(triangles, 0);
            capture.mesh.RecalculateBounds();
            var surface = new GameObject("Surface", typeof(MeshFilter), typeof(MeshRenderer));
            surface.layer = WaterfallLayer;
            surface.transform.SetParent(capture.root.transform, false);
            surface.GetComponent<MeshFilter>().sharedMesh = capture.mesh;
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = capture.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var cameraObject = new GameObject("Camera", typeof(Camera));
            cameraObject.transform.SetParent(capture.root.transform, false);
            cameraObject.transform.localPosition = new Vector3(definition.worldRect.width / 200f, -definition.worldRect.height / 200f, -10f);
            capture.camera = cameraObject.GetComponent<Camera>();
            capture.camera.clearFlags = CameraClearFlags.SolidColor;
            capture.camera.backgroundColor = Color.clear;
            capture.camera.cullingMask = 1 << WaterfallLayer;
            capture.camera.nearClipPlane = 0.1f;
            capture.camera.farClipPlane = 20f;
            capture.camera.orthographic = true;
            capture.camera.orthographicSize = definition.worldRect.height / 200f;
            capture.camera.depth = -10f;
            capture.camera.allowHDR = false;
            capture.camera.allowMSAA = false;
            int width = Mathf.Max(64, definition.textureWidth);
            int height = Mathf.Max(Mathf.Max(64, definition.minimumTextureHeight), Mathf.RoundToInt(width * definition.worldRect.height / definition.worldRect.width));
            capture.texture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            { name = definition.id, antiAliasing = 1, wrapMode = TextureWrapMode.Clamp };
            capture.texture.Create();
            capture.camera.targetTexture = capture.texture;
            capture.camera.aspect = definition.worldRect.width / definition.worldRect.height;
            return capture;
        }

        private static void AddRibbon(List<Vector3> vertices, List<Vector2> uv, List<int> triangles, WaterfallRibbon ribbon, Vector2 origin)
        {
            const int rows = 12;
            int start = vertices.Count;
            for (int column = 0; column < ribbon.top.Length; column++)
                for (int row = 0; row <= rows; row++)
                {
                    float t = row / (float)rows;
                    Vector2 point = Vector2.Lerp(ribbon.top[column], ribbon.bottom[column], t);
                    vertices.Add(new Vector3((point.x - origin.x) / 100f, -(point.y - origin.y) / 100f, 0f));
                    uv.Add(new Vector2(column / (float)(ribbon.top.Length - 1), 1f - t));
                    if (column == 0 || row == 0) continue;
                    int d = start + column * (rows + 1) + row;
                    int a = d - rows - 2;
                    triangles.Add(a); triangles.Add(d - 1); triangles.Add(d);
                    triangles.Add(a); triangles.Add(d); triangles.Add(a + 1);
                }
        }

        private void OnDisable()
        {
            if (captures == null) return;
            foreach (Capture capture in captures)
                if (capture != null) capture.camera.enabled = false;
        }

        private void OnDestroy()
        {
            if (captures == null) return;
            foreach (Capture capture in captures)
            {
                if (capture == null) continue;
                capture.camera.targetTexture = null;
                capture.texture.Release();
                Destroy(capture.texture);
                Destroy(capture.mesh);
                Destroy(capture.material);
                Destroy(capture.root);
            }
        }
    }
}
