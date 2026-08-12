using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D
{
    // BUILDING ARTWORK V1 — the official artist asset (BUILDING_001_DAY.png,
    // 1536x1024 RGBA transparent) replaces ALL procedural 3D geometry.
    //
    // The building is a single flat quad, texture-mapped with the untouched PNG,
    // shaded by ArtworkUnlit (straight alpha blend, double-sided, transparent
    // queue, LightMode SRPDefaultUnlit -> rendered by the URP 2D Renderer).
    //
    // ANCHORING (critical): the artwork is anchored at the CONTACT POINT of the
    // building base with the terrain — the bottommost opaque pixel of the PNG
    // (x=856, y=1009 from top), NOT the image center. The quad's vertices are
    // offset so that pixel maps to local (0,0,0): the root object placed on the
    // BUILDING anchor world point stands with its base exactly on the anchor,
    // and the camera/anchor/zoom/depth systems stay untouched. Transparent
    // canvas padding (below the base, sides, above the spire) is invisible.
    public static class PremiumBuildingFactory
    {
        private const float BackZ = AnchorMarker.BackdropZ;
        private const string ArtworkPath = "Assets/Experiments/Environment2D5D/Artwork/BUILDING_001_DAY.png";

        // Artwork metrics (baked from the asset, never modified).
        private const int ArtW = 1536;
        private const int ArtH = 1024;
        private const int ContactX = 856;   // bottommost opaque pixel column
        private const int ContactY = 1009;  // bottommost opaque pixel row (from top)
        private const float CanvasHeightWorld = 18f; // world height of the full canvas

        private static readonly float ContactU = (float)ContactX / ArtW;
        private static readonly float ContactV = 1f - (float)ContactY / ArtH; // from bottom

        public static GameObject Build(Transform parent, Vector3 basePos, Shader premiumShader, Shader shadowShader)
        {
#if UNITY_EDITOR
            ConfigureImporter();
#endif
            Texture2D art = LoadArtwork();
            if (!art)
            {
                Debug.LogError("[BuildingArtwork] artwork texture NOT FOUND at " + ArtworkPath);
                return null;
            }

            GameObject root = new GameObject("BuildingPremium");
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(basePos.x, basePos.y, basePos.z - 0.05f);

            Transform v = new GameObject("Visual").transform;
            v.SetParent(root.transform);
            v.localPosition = Vector3.zero;

            float w = CanvasHeightWorld * ArtW / ArtH; // keep exact 1536:1024 aspect
            float h = CanvasHeightWorld;

            Mesh mesh = new Mesh { name = "ArtworkQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-ContactU * w, -ContactV * h, 0f), // bottom-left  (image pixel 0,1023)
                new Vector3((1f - ContactU) * w, -ContactV * h, 0f), // bottom-right
                new Vector3((1f - ContactU) * w, (1f - ContactV) * h, 0f), // top-right
                new Vector3(-ContactU * w, (1f - ContactV) * h, 0f) // top-left
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

            GameObject go = new GameObject("VisualQuad");
            go.transform.SetParent(v, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            Shader artShader = Shader.Find("BeeKingdom/Experiments/ArtworkUnlit");
            Material mat = new Material(artShader) { name = "ArtworkMat_001_DAY" };
            mat.SetTexture("_MainTex", art);
            mat.SetColor("_Color", Color.white);
            mat.renderQueue = 3000;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;

            return root;
        }

        private static Texture2D LoadArtwork()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(ArtworkPath);
#else
            return Resources.Load<Texture2D>("BUILDING_001_DAY");
#endif
        }

#if UNITY_EDITOR
        private static void ConfigureImporter()
        {
            var importer = UnityEditor.AssetImporter.GetAtPath(ArtworkPath) as UnityEditor.TextureImporter;
            if (!importer) return;
            bool changed = false;
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.textureCompression != UnityEditor.TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log("[BuildingArtwork] importer configured (alphaIsTransparency, uncompressed, no mips)");
            }
        }
#endif
    }
}
