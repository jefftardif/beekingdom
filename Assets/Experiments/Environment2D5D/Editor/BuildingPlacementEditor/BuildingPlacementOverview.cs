#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools.BuildingPlacement
{
    // BUILDING PLACEMENT OVERVIEW - read-only composition view.
    //
    // Shows all buildings saved in the editor sidecar
    // (BuildingPlacementEditor_Saves.json) simultaneously, reusing the same
    // artwork quad convention as the placement preview. This is NOT a second
    // editor: no move, no resize, no save, no record mutation. The sidecar and
    // the official layout are strictly read-only here.
    //
    // All scene objects created are HideFlags.DontSave and are rebuilt from the
    // sidecar each time the view is shown. Labels are editor-only GUI overlays
    // and are never serialized anywhere.
    [InitializeOnLoad]
    public static class BuildingPlacementOverview
    {
        private const string RootName = "BUILDING_PLACEMENT_OVERVIEW";
        private const string ShaderName = "BeeKingdom/Experiments/ArtworkUnlit";
        private const float CanvasHeightWorld = 18f;
        private const float FitMargin = 1.3f;
        private const float FitZThickness = 3f;

        private const string ShowAllPath = "BeeKingdom/Building Placement Editor/Overview/Show All Buildings";
        private const string FitAllPath = "BeeKingdom/Building Placement Editor/Overview/Fit All Buildings";
        private const string ShowLabelsPath = "BeeKingdom/Building Placement Editor/Overview/Show Building Labels";

        private sealed class OverviewItem
        {
            public string buildingType;
            public Vector3 topCenter;
            public Renderer renderer;
        }

        private static readonly List<OverviewItem> _items = new List<OverviewItem>();
        private static GameObject _root;
        private static GUIStyle _labelStyle;
        private static bool _built;
        private static bool _showLabels = true;

        static BuildingPlacementOverview()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        [MenuItem(ShowAllPath, false, 2001)]
        public static void ShowAllBuildings()
        {
            BuildOverview();
        }

        [MenuItem(FitAllPath, false, 2002)]
        public static void FitAllBuildings()
        {
            FrameAllBuildings();
        }

        [MenuItem(ShowLabelsPath, false, 2003)]
        public static void ToggleShowLabels()
        {
            _showLabels = !_showLabels;
            SceneView.RepaintAll();
        }

        [MenuItem(ShowLabelsPath, true)]
        public static bool ToggleShowLabelsValidate()
        {
            Menu.SetChecked(ShowLabelsPath, _showLabels);
            return true;
        }

        public static void BuildOverview()
        {
            if (BuildingPlacementSession.Active)
            {
                BuildingPlacementSession.Deactivate();
            }

            DestroyOverview();

            PlacementSaveFile file = ReadSidecar();
            if (file == null || file.placements == null)
            {
                Debug.LogWarning("[BUILDING_PLACEMENT_OVERVIEW] Sidecar introuvable ou illisible : " +
                                 BuildingPlacementLayoutIO.SidecarPath);
                return;
            }

            _root = new GameObject(RootName);
            _root.hideFlags = HideFlags.DontSave;

            int shown = 0;
            for (int i = 0; i < file.placements.Count; i++)
            {
                PlacementSaveEntry entry = file.placements[i];
                if (entry == null) continue;

                BuildingCatalogEntry catalog = BuildingCatalog.Find(entry.buildingType);
                if (catalog == null || Duplicate(entry.buildingType)) continue;

                ArtworkScan scan = BuildingArtworkScanner.Scan(catalog.artworkPath);
                if (!scan.Valid) continue;

                CreateItem(catalog, entry, scan);
                shown++;
            }

            _built = true;
            SceneView.RepaintAll();
            Debug.Log("[BUILDING_PLACEMENT_OVERVIEW] SHOW_ALL_SHOWN=" + shown +
                      " of " + (file.placements != null ? file.placements.Count : 0));
        }

        public static void FrameAllBuildings()
        {
            if (!_built)
            {
                BuildOverview();
            }
            if (_items.Count == 0) return;

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool any = false;

            for (int i = 0; i < _items.Count; i++)
            {
                OverviewItem item = _items[i];
                if (item == null || !item.renderer) continue;

                Bounds b = item.renderer.bounds;
                min = Vector3.Min(min, b.min);
                max = Vector3.Max(max, b.max);
                min = Vector3.Min(min, item.topCenter);
                max = Vector3.Max(max, item.topCenter);

                if (_showLabels)
                {
                    max = Vector3.Max(max, item.topCenter + new Vector3(0f, 0.8f, 0f));
                }
                any = true;
            }

            if (!any) return;

            min.z = GroundSurfaceResolver.BuildingZ - FitZThickness;
            max.z = GroundSurfaceResolver.BuildingZ + FitZThickness;

            Bounds frame = new Bounds((min + max) * 0.5f, max - min);
            frame.size *= FitMargin;

            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) return;
            view.Frame(frame, true);
            view.Repaint();
            SceneView.RepaintAll();
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (!_built || !_showLabels || _items.Count == 0) return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.alignment = TextAnchor.MiddleCenter;
                _labelStyle.fontSize = 10;
                _labelStyle.normal.textColor = Color.white;
            }

            Handles.BeginGUI();
            for (int i = 0; i < _items.Count; i++)
            {
                OverviewItem item = _items[i];
                if (item == null) continue;
                Vector2 gui = HandleUtility.WorldToGUIPoint(item.topCenter);
                GUI.Label(
                    new Rect(gui.x - 60f, gui.y - 8f, 120f, 16f),
                    item.buildingType.Replace('_', ' '),
                    _labelStyle);
            }
            Handles.EndGUI();
        }

        private static void CreateItem(BuildingCatalogEntry catalog, PlacementSaveEntry entry, ArtworkScan scan)
        {
            OverviewItem item = new OverviewItem { buildingType = entry.buildingType };

            GameObject itemGo = new GameObject("Overview_" + entry.buildingType);
            itemGo.hideFlags = HideFlags.DontSave;
            itemGo.transform.SetParent(_root.transform, false);
            itemGo.transform.position = new Vector3(entry.X, entry.TerrainY, entry.Z);
            itemGo.transform.rotation = Quaternion.Euler(0f, entry.Rotation, 0f);

            GameObject visualGo = new GameObject("Visual");
            visualGo.hideFlags = HideFlags.DontSave;
            visualGo.transform.SetParent(itemGo.transform, false);
            visualGo.transform.localPosition = Vector3.zero;
            visualGo.transform.localRotation = Quaternion.identity;
            float scale = entry.Scale > 0f ? entry.Scale : 1f;
            visualGo.transform.localScale = new Vector3(scale, scale, 1f);

            Mesh mesh = BuildQuadMesh(scan);
            mesh.hideFlags = HideFlags.DontSave;
            visualGo.AddComponent<MeshFilter>().sharedMesh = mesh;

            Shader shader = Shader.Find(ShaderName);
            if (!shader)
            {
                Debug.LogError("[BUILDING_PLACEMENT_OVERVIEW] Shader introuvable : " + ShaderName);
                return;
            }

            Texture2D artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(catalog.artworkPath);
            Material material = new Material(shader);
            material.name = "OverviewMat_" + entry.buildingType;
            material.hideFlags = HideFlags.DontSave;
            if (artwork != null)
            {
                material.SetTexture("_MainTex", artwork);
            }
            material.SetColor("_Color", Color.white);
            material.renderQueue = 3000;

            MeshRenderer renderer = visualGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            item.renderer = renderer;
            item.topCenter = new Vector3(
                entry.X,
                entry.TerrainY + (1f - scan.contactV) * CanvasHeightWorld * scale,
                entry.Z);

            _items.Add(item);
        }

        private static PlacementSaveFile ReadSidecar()
        {
            if (!File.Exists(BuildingPlacementLayoutIO.SidecarPath)) return null;
            try
            {
                string json = File.ReadAllText(BuildingPlacementLayoutIO.SidecarPath);
                return JsonUtility.FromJson<PlacementSaveFile>(json);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static bool Duplicate(string buildingType)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null && _items[i].buildingType == buildingType) return true;
            }
            return false;
        }

        private static Mesh BuildQuadMesh(ArtworkScan scan)
        {
            float w = CanvasHeightWorld * scan.Aspect;
            float h = CanvasHeightWorld;

            Mesh mesh = new Mesh { name = "BuildingPlacementOverviewQuad" };
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

        public static void DestroyOverview()
        {
            if (_root) Object.DestroyImmediate(_root);
            _root = null;
            _items.Clear();
            _built = false;
        }
    }
}
#endif