#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools
{
    // ROYAL PALACE — SITE TEST (4 CANDIDATES AROUND THE CENTRAL GREAT TREE).
    //
    // Temporary, purely visual, Editor-only (DontSave, nothing serialized). Builds the
    // SAME real ROYAL_PALACE (artwork ROYAL_PALACE.png, offset quad, Visual scale FIXED
    // at 0.55 by the CEO) at FOUR plausible sites around the great central tree of
    // PlayerHive.png. Every candidate shares the same depth (Z = BuildingZ = 29.95) and
    // the same scale contract; only the SITE (X + its own terrainY) changes.
    //
    //   Site      X      terrainY (GroundSurfaceResolver)          relation to the tree
    //   --------------------------------------------------------------------------------
    //   TEST A  -9.0    ~37.807   high grassy terrace WEST of the trunk (distance ~8.8 u)
    //   TEST B  +1.83   ~27.076   AT the tree base, east side (the ROYAL_PALACE reference)
    //   TEST C  +9.0    ~15.601   mid descending terrace EAST of the tree (~9.2 u)
    //   TEST D  +15.0   ~14.801   lower terrace EAST, near the valley floor (~15.2 u)
    //
    // These X were chosen from a pixel analysis of PlayerHive.png (2500x1500 -> plane
    // 100x60 world, 25 px/unit): the great tree trunk sits around world X = -0.24
    // (texture column ~1244), the ROYAL_PALACE layout reference X = 1.83 lands on its
    // east flank (column 1296). The four sites follow the painted terraces that step
    // down eastward (terrainY 43 -> 30 -> 14 -> 18 between the live anchors
    // A(-15) B(0) C(10) BUILDING(35)). All four are INSIDE the reliable interpolation
    // span [-15, 35], so every terrainY comes from GroundSurfaceResolver (the unique
    // authority) and none is a "visual-only" candidate.
    //
    // Scale is FROZEN at 0.55 (CEO decision, previous mission) -> apparent height
    // ~9.9 u. LayoutY=39.13 stays a reference only, never a foot.
    //
    // Nothing is chosen here: this tool only SHOWS the four sites for a visual
    // comparison. The final site is picked after inspection in Unity (report first,
    // then stop).
    public static class RoyalPalaceSiteTest
    {
        private const string TargetScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";
        private const string ArtworkPath = "Assets/BeeKingdom/Art/Buildings/ROYAL_PALACE.png";
        private const string ShaderName = "BeeKingdom/Experiments/ArtworkUnlit";
        private const string GroupName = "ROYAL_PALACE_SITE_TEST";

        private const float FrozenScale = 0.55f;      // CEO-fixed scale (previous mission)
        private const float LayoutY = 39.13f;         // layout reference, never a foot
        private const float MarkerFrontZ = 29.89f;    // in front of buildings for visibility

        // Approximate world X of the great central tree trunk (pixel analysis:
        // PlayerHive column 1244 -> uv 0.4976 -> world -0.24). Used only for labels.
        private const float TreeWorldX = -0.24f;

        private const int ArtW = 1536;
        private const int ArtH = 1024;
        private const int ContactX = 650;
        private const int ContactY = 1021;
        private const float CanvasHeightWorld = 18f;  // SAME scale contract as PremiumBuildingFactory

        private static readonly float ContactU = (float)ContactX / ArtW;
        private static readonly float ContactV = 1f - (float)ContactY / ArtH;

        private static readonly List<Material> _createdMaterials = new List<Material>();
        private static GameObject _group;

        // [name, siteX, expected terrainY (documented; the resolver is the authority), color]
        private static readonly SiteRef[] Sites =
        {
            new SiteRef("SITETEST_A_WEST_TERRACE", -9.0f, 37.807f, "A — Terrasse haute OUEST (~gauche de l'arbre)", new Color(0.5f, 0.85f, 1f)),
            new SiteRef("SITETEST_B_AT_TREE_BASE", 1.83f, 27.076f, "B — Pied EST de l'arbre (référence ROYAL_PALACE)", new Color(0.2f, 1f, 0.35f)),
            new SiteRef("SITETEST_C_MID_EAST", 9.0f, 15.601f, "C — Terrasse descendante EST", new Color(1f, 0.9f, 0.5f)),
            new SiteRef("SITETEST_D_LOWER_EAST", 15.0f, 14.801f, "D — Terrasse basse EST (fond de vallée)", new Color(1f, 0.55f, 0.2f))
        };

        [InitializeOnLoadMethod]
        private static void AutoEnsureSiteTest()
        {
            if (Application.isPlaying) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != TargetScenePath) return;
            if (RoyalPalaceTestGate.LegacyAutoCreateDisabled) return;
            if (FindGroup() != null) return;
            BuildSiteTest();
        }

        [MenuItem("BeeKingdom/Experiments/Royal Palace Site Test")]
        public static void BuildFromMenu()
        {
            BuildSiteTest();
        }

        [MenuItem("BeeKingdom/Experiments/Royal Palace Site Test/Delete Site Test Candidates")]
        public static void DeleteSiteTest()
        {
            Cleanup();
            Debug.Log("[ROYAL_PALACE_SITE_TEST] 4 candidats supprimés (objets DontSave, scène intacte).");
        }

        private static void BuildSiteTest()
        {
            Cleanup();

            Texture2D art = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtworkPath);
            if (!art)
            {
                Debug.LogError("[ROYAL_PALACE_SITE_TEST] Artwork introuvable : " + ArtworkPath);
                return;
            }

            GameObject group = new GameObject(GroupName);
            group.hideFlags = HideFlags.DontSave;
            _group = group;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!font) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // One candidate per site, TerrainY ALWAYS via the unique resolver.
            for (int i = 0; i < Sites.Length; i++)
            {
                SiteRef s = Sites[i];

                float terrainY = GroundSurfaceResolver.TerrainYFromX(s.x);
                CreateCandidate(group, art, s.name, s.x, terrainY);

                // Discreet labels: small, below the base, in front of the building.
                AddLabel(group, s.name + "_Label",
                         new Vector3(s.x, terrainY - 2.6f, MarkerFrontZ),
                         s.label + " | GCP " + F(s.x, 2) + " / " + F(terrainY, 3) + " / 29.95 | Scale 0.55 (figé) | ~" +
                         F(Mathf.Abs(s.x - TreeWorldX), 1) + " u de l'arbre", font, s.color);
            }

            Debug.Log("[ROYAL_PALACE_SITE_TEST] 4 candidats créés (même artwork, scale " + F(FrozenScale, 2) + " figé, Z=29.95)");
            Debug.Log("[ROYAL_PALACE_SITE_TEST] GCP par candidat via GroundSurfaceResolver : " +
                      string.Join(" | ", System.Array.ConvertAll(Sites, s => F(s.x, 2) + "->" + F(GroundSurfaceResolver.TerrainYFromX(s.x), 3))));
            Debug.Log("[ROYAL_PALACE_SITE_TEST] Les 4 X sont dans [-15, 35] : interpolation fiable, aucun candidat visuel seul.");
            Debug.Log("[ROYAL_PALACE_SITE_TEST] Rapporter les 4 sites puis STOP — aucune décision d'implantation prise.");
        }

        private static void CreateCandidate(GameObject group, Texture2D art, string name, float worldX, float terrainY)
        {
            Vector3 gcp = new Vector3(worldX, terrainY, GroundSurfaceResolver.BuildingZ);

            GameObject root = new GameObject(name);
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(group.transform, true);
            root.transform.position = gcp;
            root.transform.rotation = Quaternion.identity;

            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            // Scale AROUND the contact point: local origin of Visual == root (0,0,0).
            visual.localScale = Vector3.one * FrozenScale;

            float w = CanvasHeightWorld * ArtW / ArtH;
            float h = CanvasHeightWorld;

            Mesh mesh = new Mesh { name = "PalaceSiteQuad_" + FrozenScale };
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
            Material mat = new Material(shader) { name = "PalaceSiteMat_" + FrozenScale };
            if (art) mat.SetTexture("_MainTex", art);
            mat.SetColor("_Color", Color.white);
            mat.renderQueue = 3000;
            _createdMaterials.Add(mat);
            quadGo.AddComponent<MeshRenderer>().sharedMaterial = mat;

            // GCP tag visible at each candidate's base (terrain contact).
            AddQuad(group, name + "_GCP_TAG", new Vector3(worldX, terrainY - 0.55f, MarkerFrontZ),
                    new Vector2(1.3f, 0.3f), new Color(1f, 0.15f, 0.15f));

            // Small ground contact line right at this candidate's own terrain height.
            AddQuad(group, name + "_GROUND_LINE", new Vector3(worldX, terrainY - 0.1f, MarkerFrontZ),
                    new Vector2(16f, 0.12f), new Color(0.9f, 0.9f, 0.9f));
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
            Material mat = new Material(shader) { name = "SiteTestMarker" };
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

        private struct SiteRef
        {
            public string name;
            public float x;
            public float expectedTerrainY;
            public string label;
            public Color color;

            public SiteRef(string name, float x, float expectedTerrainY, string label, Color color)
            {
                this.name = name;
                this.x = x;
                this.expectedTerrainY = expectedTerrainY;
                this.label = label;
                this.color = color;
            }
        }
    }
}
#endif