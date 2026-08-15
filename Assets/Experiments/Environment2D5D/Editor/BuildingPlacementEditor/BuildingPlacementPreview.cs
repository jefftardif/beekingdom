#if UNITY_EDITOR
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools.BuildingPlacement
{
    public static class BuildingPlacementPreview
    {
        public const string RootName = "BUILDING_PLACEMENT_EDITOR_PREVIEW";
        private const string ShaderName = "BeeKingdom/Experiments/ArtworkUnlit";
        private const float CanvasHeightWorld = 18f;

        private static GameObject _root;
        private static Transform _visual;
        private static ArtworkScan _scan;
        private static float _meshW;
        private static float _meshH;
        private static Material _material;
        private static Texture2D _artwork;
        private static BuildingCatalogEntry _recordEntry;

        public static GameObject Root
        {
            get { return _root; }
        }

        public static ArtworkScan Scan
        {
            get { return _scan; }
        }

        public static void Build(BuildingCatalogEntry entry)
        {
            if (entry == null) return;
            Destroy();

            _recordEntry = entry;
            _artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.artworkPath);
            _scan = BuildingArtworkScanner.Scan(entry.artworkPath);
            if (!_scan.Valid || !_artwork)
            {
                Debug.LogWarning("[BUILDING_PLACEMENT] Artwork introuvable ou illisible : " + entry.artworkPath);
                return;
            }

            _meshW = CanvasHeightWorld * _scan.Aspect;
            _meshH = CanvasHeightWorld;

            _root = new GameObject(RootName);
            _root.hideFlags = HideFlags.DontSave;
            _root.transform.rotation = Quaternion.identity;

            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(_root.transform);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            _visual = visual;

            Mesh mesh = BuildQuadMesh(_meshW, _meshH, _scan);
            GameObject quadGo = new GameObject("VisualQuad");
            quadGo.hideFlags = HideFlags.DontSave;
            quadGo.transform.SetParent(visual, false);
            quadGo.AddComponent<MeshFilter>().sharedMesh = mesh;

            Shader shader = Shader.Find(ShaderName);
            if (!shader)
            {
                Debug.LogError("[BUILDING_PLACEMENT] Shader introuvable : " + ShaderName);
                return;
            }
            _material = new Material(shader) { name = "BuildingPlacementMat_" + entry.buildingType };
            _material.SetTexture("_MainTex", _artwork);
            _material.SetColor("_Color", Color.white);
            _material.renderQueue = 3000;
            quadGo.AddComponent<MeshRenderer>().sharedMaterial = _material;
        }

        public static void RebuildIfNeeded()
        {
            if (_root) return;
            if (_recordEntry == null) return;
            Build(_recordEntry);
        }

        public static void UpdateTransform(BuildingPlacementRecord record)
        {
            if (!_root || !_visual) return;
            _root.transform.position = new Vector3(record.x, record.terrainY, record.z);
            _root.transform.rotation = Quaternion.Euler(0f, record.rotation, 0f);
            _visual.localScale = new Vector3(record.scaleX, record.scaleY, 1f);

            if (_root.transform.position != new Vector3(record.x, record.terrainY, record.z))
            {
                _root.transform.position = new Vector3(record.x, record.terrainY, record.z);
            }
        }

        public static Vector3[] GetCornerOffsetsAtScaleOne()
        {
            if (!_scan.Valid) return null;

            float lx = (_scan.opaqueUMin - _scan.contactU) * _meshW;
            float rx = (_scan.opaqueUMax - _scan.contactU) * _meshW;
            float by = (_scan.opaqueVMin - _scan.contactV) * _meshH;
            float ty = (_scan.opaqueVMax - _scan.contactV) * _meshH;

            return new[]
            {
                new Vector3(lx, by, 0f),
                new Vector3(rx, by, 0f),
                new Vector3(rx, ty, 0f),
                new Vector3(lx, ty, 0f)
            };
        }

        public static Vector3[] GetCornerWorldPositions(BuildingPlacementRecord record)
        {
            if (!_scan.Valid || !_root) return null;

            Vector3[] o1 = GetCornerOffsetsAtScaleOne();
            Vector3 gcp = new Vector3(record.x, record.terrainY, record.z);
            return new[]
            {
                gcp + new Vector3(o1[0].x * record.scaleX, o1[0].y * record.scaleY, 0f),
                gcp + new Vector3(o1[1].x * record.scaleX, o1[1].y * record.scaleY, 0f),
                gcp + new Vector3(o1[2].x * record.scaleX, o1[2].y * record.scaleY, 0f),
                gcp + new Vector3(o1[3].x * record.scaleX, o1[3].y * record.scaleY, 0f)
            };
        }

        public static Vector3 GetGroundLineLeft(BuildingPlacementRecord record)
        {
            return new Vector3(record.x - 0.6f, record.terrainY, record.z);
        }

        public static Vector3 GetGroundLineRight(BuildingPlacementRecord record)
        {
            return new Vector3(record.x + 0.6f, record.terrainY, record.z);
        }

        public static void SetVisible(bool visible)
        {
            if (!_root) return;
            if (_root.activeSelf != visible) _root.SetActive(visible);
        }

        public static void Destroy()
        {
            if (_root) Object.DestroyImmediate(_root);
            _root = null;
            _visual = null;
            if (_material) Object.DestroyImmediate(_material);
            _material = null;
            _artwork = null;
            _scan = default(ArtworkScan);
        }

        private static Mesh BuildQuadMesh(float w, float h, ArtworkScan scan)
        {
            Mesh mesh = new Mesh { name = "BuildingPlacementQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-scan.contactU * w, -scan.contactV * h, 0f),
                new Vector3((1f - scan.contactU) * w, -scan.contactV * h, 0f),
                new Vector3((1f - scan.contactU) * w, (1f - scan.contactV) * h, 0f),
                new Vector3(-scan.contactU * w, (1f - scan.contactV) * h, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
#endif