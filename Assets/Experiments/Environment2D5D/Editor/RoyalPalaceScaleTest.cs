#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools
{
    // ROYAL PALACE — VISUAL SCALE TEST.
    //
    // Temporary, purely visual, Editor-only (DontSave, nothing serialized). Compares four
    // scales of the SAME real artwork (ROYAL_PALACE.png) sharing the SAME ground anchor:
    //
    //   terrainY(1.83) = 27.076 (resolved at runtime via GroundSurfaceResolver)
    //   GroundZ = BuildingZ = 29.95 (same depth for every variant)
    //
    //   CURRENT — scale 1.00 -> H~18.0 u  (TOO LARGE reference)
    //   TEST A  — scale 0.55 -> H~9.9 u
    //   TEST B  — scale 0.65 -> H~11.7 u
    //   TEST C  — scale 0.75 -> H~13.5 u
    //
    // Every variant keeps its ROOT base exactly on the shared contact: scale is applied on
    // the Visual child around its local (0,0,0) (== the contact point), so scaling never
    // moves the base. Variants are staggered horizontally ONLY for the side-by-side
    // comparison; each is flagged with reference X=1.83, same TerrainY=27.076, same Z=29.95.
    //
    // The real ROYAL_PALACE_013 stays untouched at its exact GCP (1.83, 27.076, 29.95).
    // Nothing is decided here: the final scale is picked AFTER visual inspection in Unity.
    public static class RoyalPalaceScaleTest
    {
        private const string TargetScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";
        private const string ArtworkPath = "Assets/BeeKingdom/Art/Buildings/ROYAL_PALACE.png";
        private const string ShaderName = "BeeKingdom/Experiments/ArtworkUnlit";
        private const string GroupName = "ROYAL_PALACE_SCALE_TEST";

        private const float RefX = 1.83f;
        private const float RefY = 39.13f;          // layout reference, never a foot
        private const float ExpectedGroundY = 27.076f; // resolver-verified expected value
        private const float GroundZ = 29.95f;
        private const float MarkerFrontZ = 29.89f;  // in front of buildings for visibility

        private const int ArtW = 1536;
        private const int ArtH = 1024;
        private const int ContactX = 650;
        private const int ContactY = 1021;
        private const float CanvasHeightWorld = 18f;

        private static readonly float ContactU = (float)ContactX / ArtW;
        private static readonly float ContactV = 1f - (float)ContactY / ArtH;

        private static readonly List<Material> _createdMaterials = new List<Material>();
        private static GameObject _group;
        private static float _groundY = ExpectedGroundY;

        [InitializeOnLoadMethod]
        private static void AutoEnsureScaleTest()
        {
            if (Application.isPlaying) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != TargetScenePath) return;
            if (RoyalPalaceTestGate.LegacyAutoCreateDisabled) return;
            if (FindGroup() != null) return;
            BuildScaleTest();
        }

        [MenuItem("BeeKingdom/Experiments/Royal Palace Scale Test")]
        public static void BuildFromMenu()
        {
            BuildScaleTest();
        }

        [MenuItem("BeeKingdom/Experiments/Royal Palace Scale Test/Delete Scale Test Variants")]
        public static void DeleteScaleTest()
        {
            Cleanup();
            Debug.Log("[ROYAL_PALACE_SCALE_TEST] Variantes supprimées (objets DontSave, scène intacte).");
        }

        private static void BuildScaleTest()
        {
            Cleanup();

            // Shared ground height via the UNIQUE resolver (27.076 verified in scene).
            _groundY = GroundSurfaceResolver.TerrainYFromX(RefX);
            Debug.Log("[ROYAL_PALACE_SCALE_TEST] TerrainY(1.83)=" + F(_groundY, 3) +
                      " (attendu " + F(ExpectedGroundY, 3) + ", source=GroundSurfaceResolver)");

            Texture2D art = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtworkPath);
            if (!art)
            {
                Debug.LogError("[ROYAL_PALACE_SCALE_TEST] Artwork introuvable : " + ArtworkPath);
                return;
            }

            GameObject group = new GameObject(GroupName);
            group.hideFlags = HideFlags.DontSave;
            _group = group;

            // Shared ground contact line (same TerrainY across the whole lineup).
            AddQuad(group, "SCALETEST_GROUND_LINE", new Vector3(RefX, _groundY - 0.1f, MarkerFrontZ),
                    new Vector2(145f, 0.16f), new Color(0.2f, 1f, 0.35f));

            // Shared reference column at X=1.83 (layout -> ground).
            float layoutLocalY = RefY - _groundY;
            AddQuad(group, "SCALETEST_LAYOUT_LINE", new Vector3(RefX, (RefY + _groundY) * 0.5f, MarkerFrontZ),
                    new Vector2(0.09f, layoutLocalY), new Color(0.9f, 0.9f, 0.9f));
            AddQuad(group, "SCALETEST_LAYOUT_MARKER", new Vector3(RefX, RefY, MarkerFrontZ),
                    new Vector2(1.0f, 0.4f), new Color(1f, 0.85f, 0.15f));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!font) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            AddLabel(group, "SCALETEST_Label_Layout", new Vector3(RefX + 1.4f, RefY + 0.3f, MarkerFrontZ),
                     "LAYOUT Y=39.13 (référence)", font, new Color(1f, 0.85f, 0.15f));
            AddLabel(group, "SCALETEST_Label_Ground", new Vector3(RefX + 1.4f, _groundY + 0.2f, MarkerFrontZ),
                     "TERRAIN Y=27.076 (commun à toutes les variantes)", font, new Color(0.2f, 1f, 0.35f));

            // Variants: [name, displayX offset, scale, label, color].
            VariantRef[] variants =
            {
                new VariantRef("SCALETEST_CURRENT_100", RefX - 62f, 1.00f, "CURRENT — Scale 1.00 — H≈18.0 u (TOO LARGE)", new Color(1f, 0.55f, 0.2f)),
                new VariantRef("SCALETEST_A_055", RefX - 20f, 0.55f, "TEST A — Scale 0.55 — H≈9.9 u", new Color(0.5f, 0.85f, 1f)),
                new VariantRef("SCALETEST_B_065", RefX + 20f, 0.65f, "TEST B — Scale 0.65 — H≈11.7 u", new Color(0.6f, 1f, 0.6f)),
                new VariantRef("SCALETEST_C_075", RefX + 62f, 0.75f, "TEST C — Scale 0.75 — H≈13.5 u", new Color(1f, 0.9f, 0.5f))
            };

            for (int i = 0; i < variants.Length; i++)
            {
                VariantRef v = variants[i];
                CreateVariant(group, art, v.name, v.displayX, v.scale);
                AddLabel(group, v.name + "_Label", new Vector3(v.displayX, _groundY - 2.2f, MarkerFrontZ),
                         v.label + " | GCP ref (1.83, 27.076, 29.95)", font, v.color);
            }

            Debug.Log("[ROYAL_PALACE_SCALE_TEST] 4 variantes créées (CURRENT 1.00 / A 0.55 / B 0.65 / C 0.75)");
            Debug.Log("[ROYAL_PALACE_SCALE_TEST] GCP commun : TerrainY=27.076, Z=29.95 ; X affiché décalé pour comparaison (référence X=1.83).");
            Debug.Log("[ROYAL_PALACE_SCALE_TEST] Aucune décision d'échelle prise — validation visuelle requise.");
        }

        private static void CreateVariant(GameObject group, Texture2D art, string name, float worldX, float scale)
        {
            Vector3 gcp = new Vector3(worldX, _groundY, GroundZ);

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

            Mesh mesh = new Mesh { name = "PalaceScaleQuad_" + scale };
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
            Material mat = new Material(shader) { name = "PalaceScaleMat_" + scale };
            if (art) mat.SetTexture("_MainTex", art);
            mat.SetColor("_Color", Color.white);
            mat.renderQueue = 3000;
            _createdMaterials.Add(mat);
            quadGo.AddComponent<MeshRenderer>().sharedMaterial = mat;

            // GCP tag visible at the base of each variant (same shared ground height).
            AddQuad(group, name + "_GCP_TAG", new Vector3(worldX, _groundY - 0.55f, MarkerFrontZ),
                    new Vector2(1.3f, 0.3f), new Color(1f, 0.15f, 0.15f));
        }

        private static void AddQuad(GameObject parent, string name, Vector3 center, Vector2 size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.hideFlags = HideFlags.DontSave;
            Collider col = go.GetComponent<Collider>();
            if (col) Object.DestroyImmediate(col);
            go.transform.SetParent(parent.transform, true);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.transform.localRotation = Quaternion.identity;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Unlit/Color");
            Material mat = new Material(shader) { name = "ScaleTestMarker" };
            mat.color = color;
            _createdMaterials.Add(mat);
            mr.sharedMaterial = mat;
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
            tm.characterSize = 0.5f;
            tm.fontSize = 38;
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