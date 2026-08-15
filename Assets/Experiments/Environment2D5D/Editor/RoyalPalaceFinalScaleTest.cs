#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools
{
    // ROYAL PALACE — FINAL SCALE TEST ON THE REFERENCE SITE (site B).
    //
    // The four-site selection method (A/B/C/D) is abandoned. The design decision now
    // returns to the HISTORICAL ROYAL_PALACE location, slightly low and to the right of
    // the great central tree:
    //
    //     X = 1.83
    //     TerrainY = 27.076
    //     Z = 29.95
    //
    // This tool is temporary, purely visual, Editor-only (DontSave, nothing serialized).
    // It compares simultaneously THREE scales of the SAME real artwork (ROYAL_PALACE.png)
    // using EXACTLY the same GCP for all three variants:
    //
    //     GroundSurfaceResolver.TerrainYFromX(1.83)  -> (1.83, 27.076, 29.95)
    //
    //   SCALE 0.35 -> apparent height ~6.3 u
    //   SCALE 0.40 -> apparent height ~7.2 u
    //   SCALE 0.45 -> apparent height ~8.1 u
    //
    // SAME artwork & SAME anchor contract as the current integration (RoyalPalaceIntegration):
    //   Root = Ground Contact Point
    //   Artwork = offset child whose visual base maps to local (0,0,0)
    // The scale is applied on the Visual child AROUND its local (0,0,0) (== the root GCP),
    // so the visual base of the Palace stays EXACTLY at the same spot for the three variants.
    //
    // The three variants are only staggered in X (display offsets, east of the site) to allow
    // a side-by-side comparison; their LOGICAL GCP is never modified (always the reference).
    // Discreet labels show ONLY the scale number (0.35 / 0.40 / 0.45) — no GCP tags, no ground
    // line, no other diagnostic: exactly three buildings and their three labels are visible.
    //
    // Nothing is frozen here: the final scale is picked AFTER visual inspection in Unity.
    public static class RoyalPalaceFinalScaleTest
    {
        private const string TargetScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";
        private const string ArtworkPath = "Assets/BeeKingdom/Art/Buildings/ROYAL_PALACE.png";
        private const string ShaderName = "BeeKingdom/Experiments/ArtworkUnlit";
        private const string GroupName = "ROYAL_PALACE_FINAL_SCALE_TEST";

        // Reference site B (frozen design decision, never invented).
        private const float RefX = 1.83f;
        private const float RefY = 39.13f;             // layout reference, never a foot
        private const float ExpectedGroundY = 27.076f; // resolver-verified expected value
        private const float GroundZ = 29.95f;
        private const float MarkerFrontZ = 29.89f;     // in front of buildings for visibility

        private const int ArtW = 1536;
        private const int ArtH = 1024;
        private const int ContactX = 650;
        private const int ContactY = 1021;
        private const float CanvasHeightWorld = 18f;   // SAME scale contract as PremiumBuildingFactory

        private static readonly float ContactU = (float)ContactX / ArtW;
        private static readonly float ContactV = 1f - (float)ContactY / ArtH;

        private static readonly List<Material> _createdMaterials = new List<Material>();
        private static GameObject _group;
        private static float _groundY = ExpectedGroundY;

        // [name, display X offset (east of the site, X-only spread for comparison), scale, label, color]
        private static readonly VariantRef[] Variants =
        {
            new VariantRef("FINALSCALETEST_035", RefX + 6f, 0.35f, "0.35", new Color(0.5f, 0.85f, 1f)),
            new VariantRef("FINALSCALETEST_040", RefX + 24f, 0.40f, "0.40", new Color(0.6f, 1f, 0.6f)),
            new VariantRef("FINALSCALETEST_045", RefX + 42f, 0.45f, "0.45", new Color(1f, 0.9f, 0.5f))
        };

        [InitializeOnLoadMethod]
        private static void AutoEnsureFinalScaleTest()
        {
            if (Application.isPlaying) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != TargetScenePath) return;
            if (RoyalPalaceTestGate.LegacyAutoCreateDisabled) return;
            if (FindGroup() != null) return;
            BuildFinalScaleTest();
        }

        // Public entry point used by the session cleanup tool: force a fresh rebuild.
        public static void Rebuild()
        {
            BuildFinalScaleTest();
        }

        [MenuItem("BeeKingdom/Experiments/Royal Palace Final Scale Test")]
        public static void BuildFromMenu()
        {
            BuildFinalScaleTest();
        }

        [MenuItem("BeeKingdom/Experiments/Royal Palace Final Scale Test/Delete Final Scale Test Variants")]
        public static void DeleteFinalScaleTest()
        {
            Cleanup();
            Debug.Log("[ROYAL_PALACE_FINAL_SCALE_TEST] Variantes 0.35/0.40/0.45 supprimées (objets DontSave, scène intacte).");
        }

        private static void BuildFinalScaleTest()
        {
            Cleanup();

            // Shared ground height via the UNIQUE resolver (27.076 verified in scene).
            _groundY = GroundSurfaceResolver.TerrainYFromX(RefX);
            Debug.Log("[ROYAL_PALACE_FINAL_SCALE_TEST] TerrainY(1.83)=" + F(_groundY, 3) +
                      " (attendu " + F(ExpectedGroundY, 3) + ", source=GroundSurfaceResolver)");

            Texture2D art = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtworkPath);
            if (!art)
            {
                Debug.LogError("[ROYAL_PALACE_FINAL_SCALE_TEST] Artwork introuvable : " + ArtworkPath);
                return;
            }

            GameObject group = new GameObject(GroupName);
            group.hideFlags = HideFlags.DontSave;
            _group = group;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!font) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            for (int i = 0; i < Variants.Length; i++)
            {
                VariantRef v = Variants[i];
                CreateVariant(group, art, v.name, v.displayX, v.scale);

                // Discreet label: only the scale number, small, below the base.
                AddLabel(group, v.name + "_Label",
                         new Vector3(v.displayX, _groundY - 2.0f, MarkerFrontZ),
                         v.label, font, v.color);
            }

            Debug.Log("[ROYAL_PALACE_FINAL_SCALE_TEST] 3 variantes créées : 0.35 / 0.40 / 0.45");
            Debug.Log("[ROYAL_PALACE_FINAL_SCALE_TEST] GCP commun (logique, jamais modifié) = (1.83, " + F(_groundY, 3) + ", 29.95)");
            Debug.Log("[ROYAL_PALACE_FINAL_SCALE_TEST] Variantes décalées en X UNIQUEMENT pour comparaison : " +
                      string.Join(" | ", System.Array.ConvertAll(Variants, v => v.label + "@X=" + F(v.displayX, 2))));
            Debug.Log("[ROYAL_PALACE_FINAL_SCALE_TEST] Scale appliquée autour du GCP : base visuelle identique pour les 3 variantes.");
            Debug.Log("[ROYAL_PALACE_FINAL_SCALE_TEST] Hauteurs apparentes : 0.35~6.3 u / 0.40~7.2 u / 0.45~8.1 u.");
            Debug.Log("[ROYAL_PALACE_FINAL_SCALE_TEST] Aucune décision d'échelle prise — validation visuelle requise.");
        }

        private static void CreateVariant(GameObject group, Texture2D art, string name, float displayX, float scale)
        {
            // Logical GCP stays the reference site; only the DISPLAY X is staggered.
            Vector3 gcp = new Vector3(displayX, _groundY, GroundZ);

            GameObject root = new GameObject(name);
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(group.transform, true);
            root.transform.position = gcp;
            root.transform.rotation = Quaternion.identity;

            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            // Scale AROUND the contact point: local origin of Visual == (0,0,0) of the root.
            visual.localScale = Vector3.one * scale;

            float w = CanvasHeightWorld * ArtW / ArtH;
            float h = CanvasHeightWorld;

            Mesh mesh = new Mesh { name = "RoyalPalaceFinalQuad_" + scale };
            mesh.vertices = new[]
            {
                new Vector3(-ContactU * w, -ContactV * h, 0f),
                new Vector3((1f - ContactU) * w, -ContactV * h, 0f),
                new Vector3((1f - ContactU) * w, (1f - ContactV) * h, 0f),
                new Vector3(-ContactU * w, (1f - ContactV) * h, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject quadGo = new GameObject("VisualQuad");
            quadGo.hideFlags = HideFlags.DontSave;
            quadGo.transform.SetParent(visual, false);
            quadGo.AddComponent<MeshFilter>().sharedMesh = mesh;

            Shader shader = Shader.Find(ShaderName);
            Material mat = new Material(shader) { name = "RoyalPalaceFinalMat_" + scale };
            if (art) mat.SetTexture("_MainTex", art);
            mat.SetColor("_Color", Color.white);
            mat.renderQueue = 3000;
            _createdMaterials.Add(mat);
            quadGo.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void AddLabel(GameObject parent, string name, Vector3 position, string text, Font font, Color color)
        {
            GameObject go = new GameObject(name);
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent.transform, true);
            go.transform.position = position;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font;
            tm.characterSize = 0.35f;
            tm.fontSize = 24;
            tm.anchor = TextAnchor.MiddleLeft;
            tm.color = color;
        }

        private static GameObject FindGroup()
        {
            GameObject[] all = Object.FindObjectsOfType<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == GroupName) return all[i];
            }
            return null;
        }

        private static void Cleanup()
        {
            GameObject existing = FindGroup();
            if (existing) Object.DestroyImmediate(existing);
            if (_group) Object.DestroyImmediate(_group);
            _group = null;

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

        private struct VariantRef
        {
            public string name;
            public float displayX;
            public float scale;
            public string label;
            public Color color;

            public VariantRef(string name, float displayX, float scale, string label, Color color)
            {
                this.name = name;
                this.displayX = displayX;
                this.scale = scale;
                this.label = label;
                this.color = color;
            }
        }
    }
}
#endif
