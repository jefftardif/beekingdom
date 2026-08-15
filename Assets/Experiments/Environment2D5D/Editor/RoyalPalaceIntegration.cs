#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools
{
    // ROYAL_PALACE — first official BeeKingdom building integration (BUILDING_013).
    //
    // Validation step only: this tools builds the REAL artwork (ROYAL_PALACE.png) on the
    // Ground Contact Point architecture, WITHOUT touching the 14 placeholders, the layouts,
    // any PNG/.meta/import settings or LivingHive.unity.
    //
    //   ROOT "ROYAL_PALACE_013"  = Ground Contact Point (1.83, terrainY(1.83), 29.95)
    //     └── Visual             (local 0,0,0 — mirrors PremiumBuildingFactory)
    //          └── VisualQuad    (quad offset so the artwork's CONTACT pixel maps to local
    //                             (0,0,0): the whole building rises above the GCP)
    //
    // terrainY comes from GroundSurfaceResolver (unique source) via BuildingGroundAnchor —
    // never hand-copied, never duplicated. LayoutY=39.13 stays a reference only.
    //
    // Artwork (official): Assets/BeeKingdom/Art/Buildings/ROYAL_PALACE.png
    //   1536x1024, alphaIsTransparency, sprite slice ROYAL_PALACE_0 (rect 0..1024).
    //   Opaque bbox x=[30..1508] y=[2..1021]. Contact pixel = center of the bottommost
    //   opaque row (1019..1021 taper to a plinth): (650, 1021). Bottom margin 2 px.
    //
    // Scale (not invented): CanvasHeightWorld = 18, identical to PremiumBuildingFactory
    //   (BUILDING_001_DAY is also 1536x1024) -> quad 27 x 18 world, same apparent scale
    //   contract as the validated test building.
    public static class RoyalPalaceIntegration
    {
        private const string TargetScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";
        private const string ArtworkPath = "Assets/BeeKingdom/Art/Buildings/ROYAL_PALACE.png";
        private const string ShaderName = "BeeKingdom/Experiments/ArtworkUnlit";
        private const string RootName = "ROYAL_PALACE_013";

        // Layout data (frozen, read-only).
        private const float LayoutX = 1.83f;
        private const float LayoutY = 39.13f;

        // Artwork metrics (baked from the untouched asset — never modified).
        private const int ArtW = 1536;
        private const int ArtH = 1024;
        private const int ContactX = 650;   // center of the bottommost opaque row (plinth)
        private const int ContactY = 1021;  // bottommost opaque row (from top)
        private const float CanvasHeightWorld = 18f; // SAME scale contract as PremiumBuildingFactory

        private static readonly float ContactU = (float)ContactX / ArtW;
        private static readonly float ContactV = 1f - (float)ContactY / ArtH; // from bottom

        private static readonly List<Material> _createdMaterials = new List<Material>();
        private static GameObject _currentRoot;

        [InitializeOnLoadMethod]
        private static void AutoEnsureRoyalPalace()
        {
            if (Application.isPlaying) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != TargetScenePath) return;
            if (RoyalPalaceTestGate.LegacyAutoCreateDisabled) return;
            if (FindRoot() != null) return;
            BuildRoyalPalace(true);
        }

        [MenuItem("BeeKingdom/Experiments/Royal Palace Real (BUILDING_013)")]
        public static void BuildFromMenu()
        {
            BuildRoyalPalace(false);
        }

        [MenuItem("BeeKingdom/Experiments/Royal Palace Real (BUILDING_013)/Toggle Visible (Editor only)")]
        public static void ToggleVisible()
        {
            GameObject root = FindRoot();
            if (!root)
            {
                Debug.LogWarning("[ROYAL_PALACE] Aucun bâtiment présent (créez-le d'abord).");
                return;
            }
            root.SetActive(!root.activeSelf);
            Debug.Log("[ROYAL_PALACE] Visible=" + root.activeSelf + " (masquage Editor uniquement, rien n'est sauvegardé).");
        }

        [MenuItem("BeeKingdom/Experiments/Royal Palace Real (BUILDING_013)/Delete Royal Palace")]
        public static void DeleteRoyalPalace()
        {
            Cleanup();
            Debug.Log("[ROYAL_PALACE] Bâtiment supprimé (objet DontSave, scène intacte).");
        }

        private static void BuildRoyalPalace(bool auto)
        {
            Cleanup();

            Texture2D art = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtworkPath);
            if (!art)
            {
                Debug.LogError("[ROYAL_PALACE] Artwork introuvable : " + ArtworkPath);
                return;
            }

            // GCP via the UNIQUE resolver — never hand-copied.
            BuildingGroundAnchor anchor = BuildingGroundAnchor.Resolve(LayoutX);
            Vector3 gcp = anchor.GroundContactPoint;

            GameObject root = new GameObject(RootName);
            root.hideFlags = HideFlags.DontSave;
            root.transform.position = gcp;
            root.transform.rotation = Quaternion.identity;
            _currentRoot = root;

            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;

            float w = CanvasHeightWorld * ArtW / ArtH; // 27 world units
            float h = CanvasHeightWorld;               // 18 world units

            Mesh mesh = new Mesh { name = "RoyalPalaceQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-ContactU * w, -ContactV * h, 0f),
                new Vector3((1f - ContactU) * w, -ContactV * h, 0f),
                new Vector3((1f - ContactU) * w, (1f - ContactV) * h, 0f),
                new Vector3(-ContactU * w, (1f - ContactV) * h, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject quadGo = new GameObject("VisualQuad");
            quadGo.hideFlags = HideFlags.DontSave;
            quadGo.transform.SetParent(visual, false);
            quadGo.AddComponent<MeshFilter>().sharedMesh = mesh;

            Shader shader = Shader.Find(ShaderName);
            if (!shader)
            {
                Debug.LogError("[ROYAL_PALACE] Shader introuvable : " + ShaderName);
            }
            Material mat = new Material(shader) { name = "RoyalPalaceMat" };
            if (art) mat.SetTexture("_MainTex", art);
            mat.SetColor("_Color", Color.white);
            mat.renderQueue = 3000;
            _createdMaterials.Add(mat);
            quadGo.AddComponent<MeshRenderer>().sharedMaterial = mat;

            Debug.Log("[ROYAL_PALACE] " + (auto ? "auto-créé (domaine reload)" : "créé via menu"));
            Debug.Log("[ROYAL_PALACE] LayoutX=" + F(LayoutX, 2));
            Debug.Log("[ROYAL_PALACE] LayoutY=" + F(LayoutY, 2) + " (référence, jamais utilisée comme pied)");
            Debug.Log("[ROYAL_PALACE] TerrainY=" + F(anchor.TerrainY, 3));
            Debug.Log("[ROYAL_PALACE] GroundZ=" + F(GroundSurfaceResolver.BuildingZ, 2));
            Debug.Log("[ROYAL_PALACE] GroundContactPoint=(" + F(gcp.x, 3) + "," + F(gcp.y, 3) + "," + F(gcp.z, 2) + ")");
            Debug.Log("[ROYAL_PALACE] Resolveur=GroundSurfaceResolver (source terrainY unique)");
            Debug.Log("[ROYAL_PALACE] Artwork=" + ArtworkPath + " (" + ArtW + "x" + ArtH + ") ContactPixel=(" + ContactX +
                      "," + ContactY + ") ContactUV=(" + F(ContactU, 4) + "," + F(ContactV, 4) + ")");
            Debug.Log("[ROYAL_PALACE] Quad=" + F(w, 2) + "x" + F(h, 2) + " u, scale=1, rotation=(0,0,0), Z=" + F(gcp.z, 2));
            Debug.Log("[ROYAL_PALACE] Hauteur finale apparente ~" + F((1f - 2f / ArtH) * h, 2) +
                      " u ; largeur apparente ~" + F(((1508f - 30f) / ArtW) * w, 2) + " u");
        }

        private static GameObject FindRoot()
        {
            GameObject[] all = Object.FindObjectsOfType<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == RootName) return all[i];
            }
            return null;
        }

        private static void Cleanup()
        {
            GameObject existing = FindRoot();
            if (existing) Object.DestroyImmediate(existing);
            if (_currentRoot) Object.DestroyImmediate(_currentRoot);
            _currentRoot = null;

            for (int i = 0; i < _createdMaterials.Count; i++)
            {
                if (_createdMaterials[i]) Object.DestroyImmediate(_createdMaterials[i]);
            }
            _createdMaterials.Clear();
        }

        private static string F(float v, int decimals)
        {
            return v.ToString("F" + decimals, CultureInfo.InvariantCulture);
        }
    }
}
#endif