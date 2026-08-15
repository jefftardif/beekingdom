#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools
{
    // GROUND CONTACT POINT / GROUND ANCHOR — PROTOTYPE PILOT (ROYAL_PALACE only).
    //
    // Demonstrates the formalized architecture without touching the 14 placeholders,
    // the frozen layouts, the PNGs, LivingHive.unity or any production system:
    //
    //   Building Transform  =  Ground Contact Point (GCP)
    //       X = Layout X            = 1.83
    //       Y = TerrainY(Layout X)  = 27.076   (from the validated AnchorMarker trace)
    //       Z = BackdropZ - 0.05    = 29.95
    //
    //   Layout Y = 39.13 stays UNCHANGED in the layout and is only drawn as a reference.
    //
    // The prototype artwork is built from plain opaque Quads (MeshRenderer + Unlit/Color,
    // no texture created) so that its local GCP is (0,0,0) and the whole silhouette rises
    // ABOVE that point — the exact opposite of the placeholder's centered (0.5,0.5) pivot.
    // It mirrors the existing PremiumBuildingFactory pipeline (root = contact point,
    // artwork extends above it).
    //
    // The terrain Y is NOT recomputed here: it uses GroundSurfaceResolver (unique source of
    // truth for terrainY(X), ex GroundAnchorDiagnostic) -> single validated engine.
    //
    // Everything is created with HideFlags.DontSave: nothing is serialized into the scene
    // (the scene file is NEVER saved or dirtied by this tool).
    public static class GroundAnchorPrototype
    {
        private const string TargetScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";
        private const string RootName = "GROUND_ANCHOR_PROTOTYPE_ROYAL_PALACE";

        // Layout data (frozen, read-only — never written anywhere).
        private const float LayoutX = 1.83f;
        private const float LayoutY = 39.13f;
        private const float LayoutZ = 29.95f;

        // Resolved Ground Anchor (from the validated engine).
        private const float ExpectedTerrainY = 27.076f;

        // Rendering depth: slightly in front of the building plane so markers/labels stay
        // visible over both the artwork and the painted backdrop in Scene and Game view.
        private const float MarkerZ = LayoutZ - 0.06f;

        private static readonly List<Material> _createdMaterials = new List<Material>();
        private static GameObject _currentRoot;

        [InitializeOnLoadMethod]
        private static void AutoEnsurePrototype()
        {
            if (Application.isPlaying) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != TargetScenePath) return;
            if (RoyalPalaceTestGate.LegacyAutoCreateDisabled) return;
            if (FindRoot() != null) return;
            BuildPrototype(true);
        }

        [MenuItem("BeeKingdom/Experiments/Ground Anchor Prototype (ROYAL_PALACE)")]
        public static void BuildFromMenu()
        {
            BuildPrototype(false);
        }

        [MenuItem("BeeKingdom/Experiments/Ground Anchor Prototype (ROYAL_PALACE)/Delete Prototype")]
        public static void DeletePrototype()
        {
            Cleanup();
            Debug.Log("[GROUND_ANCHOR_PROTOTYPE] Prototype supprimé (aucune donnée de scène touchée).");
        }

        private static void BuildPrototype(bool auto)
        {
            Cleanup();

            // 1) Terrain Y via the SAME validated engine (GroundSurfaceResolver is now the
            //    unique source; this refactor check must keep terrainY(1.83)=27.076).
            float terrainY = GroundSurfaceResolver.TerrainYFromX(LayoutX);
            float selfCheck = GroundSurfaceResolver.TerrainYFromX(35f);

            Vector3 gcp = new Vector3(LayoutX, terrainY, LayoutZ);

            // 2) Root transform = Ground Contact Point.
            GameObject root = new GameObject(RootName);
            root.hideFlags = HideFlags.DontSave;
            root.transform.position = gcp;
            root.transform.rotation = Quaternion.identity;
            _currentRoot = root;

            // 3) Prototype artwork: local GCP = (0,0,0), silhouette strictly above.
            CreateArtwork(root);

            // 4) Visualization: layout reference, ground anchor, GCP, guide line, labels.
            CreateReferenceVisuals(root, terrainY);

            Debug.Log("[GROUND_ANCHOR_PROTOTYPE] " + (auto ? "auto-créé (domaine reload)" : "créé via menu") +
                      " | GCP=( " + F(LayoutX) + " , " + F(terrainY) + " , " + F(LayoutZ) + " )");
            Debug.Log("[GROUND_ANCHOR_PROTOTYPE] terrainY via GroundSurfaceResolver (moteur unique validé) -> " + F(terrainY) +
                      " (attendu " + F(ExpectedTerrainY) + ") | self-check x=35 -> " + F(selfCheck) + " (ancre BUILDING=18.003)");
            Debug.Log("[GROUND_ANCHOR_PROTOTYPE] LayoutY=" + F(LayoutY) + " conservé (référence jaune), jamais utilisé pour le transform.");
        }

        private static void CreateArtwork(GameObject root)
        {
            AddPart(root, "RP_Door",    new Vector3(0f, 0.95f, 0.002f),  new Vector2(1.3f, 1.9f), new Color(0.25f, 0.16f, 0.10f));
            AddPart(root, "RP_Body",    new Vector3(0f, 2.60f, 0f),      new Vector2(5.0f, 5.2f), new Color(0.96f, 0.90f, 0.76f));
            AddPart(root, "RP_TurretL", new Vector3(-3.1f, 1.70f, 0f),   new Vector2(1.5f, 3.4f), new Color(0.96f, 0.90f, 0.76f));
            AddPart(root, "RP_TurretR", new Vector3(3.1f, 1.70f, 0f),    new Vector2(1.5f, 3.4f), new Color(0.96f, 0.90f, 0.76f));
            AddPart(root, "RP_Tower",   new Vector3(0f, 9.00f, 0f),      new Vector2(3.0f, 7.6f), new Color(0.92f, 0.83f, 0.64f));
            AddPart(root, "RP_Roof",    new Vector3(0f, 13.30f, 0f),     new Vector2(3.4f, 1.0f), new Color(0.78f, 0.35f, 0.25f));

            // GCP dot: right at the base center, marks local (0,0,0) on the artwork.
            AddPart(root, "RP_GCP_DOT", new Vector3(0f, 0.02f, -0.002f), new Vector2(0.55f, 0.55f), new Color(1f, 0.15f, 0.15f));
        }

        private static void CreateReferenceVisuals(GameObject root, float terrainY)
        {
            float layoutLocalY = LayoutY - terrainY;   // local: layout is ABOVE the GCP
            float guideX = 2.2f;
            Vector3 lineCenter = new Vector3(guideX, layoutLocalY * 0.5f, MarkerZ - LayoutZ);
            Vector2 lineSize = new Vector2(0.09f, layoutLocalY);
            AddQuad(root, "RP_LINE_LAYOUT_TO_GROUND", lineCenter, lineSize, new Color(0.9f, 0.9f, 0.9f));

            AddQuad(root, "RP_LAYOUT_MARKER", new Vector3(guideX, layoutLocalY, MarkerZ - LayoutZ),
                    new Vector2(0.9f, 0.35f), new Color(1f, 0.85f, 0.15f));
            AddQuad(root, "RP_GROUND_MARKER", new Vector3(guideX, 0f, MarkerZ - LayoutZ),
                    new Vector2(1.1f, 0.28f), new Color(0.2f, 1f, 0.35f));

            // Terrain contact line across the building footprint at Y = terrainY.
            AddQuad(root, "RP_TERRAIN_CONTACT_LINE", new Vector3(0f, -0.12f, MarkerZ - LayoutZ),
                    new Vector2(7.4f, 0.16f), new Color(0.2f, 1f, 0.35f));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!font) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (!font) return;

            float labelX = guideX + 1.3f;
            AddLabel(root, "RP_Label_Layout",  new Vector3(labelX, layoutLocalY, MarkerZ - LayoutZ),
                     "LAYOUT Y = 39.13 (référence, inchangé)", font, new Color(1f, 0.85f, 0.15f));
            AddLabel(root, "RP_Label_Ground",  new Vector3(labelX, -0.2f, MarkerZ - LayoutZ),
                     "GROUND ANCHOR Y = " + F(terrainY), font, new Color(0.2f, 1f, 0.35f));
            AddLabel(root, "RP_Label_GCP",     new Vector3(labelX, -0.9f, MarkerZ - LayoutZ),
                     "GCP = (1.83, " + F(terrainY) + ", 29.95) — pivot artwork local (0,0,0)", font, new Color(1f, 0.4f, 0.35f));
            AddLabel(root, "RP_Label_Title",   new Vector3(-6f, layoutLocalY + 0.8f, MarkerZ - LayoutZ),
                     "PROTOTYPE ROYAL_PALACE — GROUND CONTACT POINT (GCP)", font, Color.white);
        }

        private static void AddPart(GameObject parent, string name, Vector3 localCenter, Vector2 size, Color color)
        {
            AddQuad(parent, name, localCenter, size, color);
        }

        private static void AddQuad(GameObject parent, string name, Vector3 localCenter, Vector2 size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.hideFlags = HideFlags.DontSave;
            Collider col = go.GetComponent<Collider>();
            if (col) Object.DestroyImmediate(col);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localCenter;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.transform.localRotation = Quaternion.identity;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = GetMaterial(color);
        }

        private static void AddLabel(GameObject parent, string name, Vector3 localPos, string text, Font font, Color color)
        {
            GameObject go = new GameObject(name);
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font;
            tm.characterSize = 0.5f;
            tm.fontSize = 40;
            tm.anchor = TextAnchor.MiddleLeft;
            tm.color = color;
        }

        private static Material GetMaterial(Color color)
        {
            Shader shader = Shader.Find("Unlit/Color");
            Material mat = new Material(shader);
            mat.name = "GCP_Proto_" + color.r.ToString("F2") + color.g.ToString("F2") + color.b.ToString("F2");
            mat.color = color;
            _createdMaterials.Add(mat);
            return mat;
        }

        private static GameObject FindRoot()
        {
            GameObject[] all = UnityEngine.Object.FindObjectsOfType<GameObject>();
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

        private static string F(float v)
        {
            return v.ToString("F3", CultureInfo.InvariantCulture);
        }
    }
}
#endif
