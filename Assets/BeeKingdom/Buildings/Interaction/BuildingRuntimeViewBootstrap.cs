using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeeKingdom.Buildings.Placement;

namespace BeeKingdom.Buildings.Interaction
{
    // RUNTIME GAME-VIEW BOOTSTRAP (non-editor, BeeKingdom.Buildings assembly only).
    //
    // Mission : rendre les 14 bâtiments VISIBLES à l'ouverture de la scène de jeu
    // Environment2D5D_SpatialV3 (sans « Show All Buildings »), en créant au runtime les
    // quads d'artwork positionnés au Ground Contact Point du sidecar, chacun portant une
    // zone de clic (BoxCollider + BuildingInteractionComponent) pour M-INTERACTION, et en
    // masquant au runtime les marqueurs de dev (croix cyan « SurfaceRepère_* »).
    //
    // Contraintes respectées :
    //   - Aucune référence UnityEditor : tout passe par UnityEngine + System.IO.
    //   - Aucun fichier protégé modifié (sidecar/scènes/éditeur/artworks).
    //   - Compilé dans BeeKingdom.Buildings (référence BeeKingdom.Core + BeeKingdom.Buildings.Placement).
    public static class BuildingRuntimeViewBootstrap
    {
        private const string VisualRootNamePrefix = "RuntimeVisual_";
        private const string ShaderName = "BeeKingdom/Experiments/ArtworkUnlit";
        private const float CanvasHeightWorld = 18f;
        private const float AlphaThreshold = 8f / 255f;
        private const string DefaultRelativeSidecarPath = "Assets/Experiments/Environment2D5D/Config/BuildingPlacementEditor_Saves.json";
        private const string RelativeArtRoot = "Assets/BeeKingdom/Art/Buildings";

        // Type -> fichier d'artwork (miroir EXACT du catalogue éditeur BuildingCatalog).
        private static readonly Dictionary<string, string> ArtworkByType = new Dictionary<string, string>
        {
            { "NURSERY", "NURSERY_001.png" },
            { "BARRACK", "BARRACK_001.png" },
            { "HONEY_RESERVE", "HONEY_RESERVE_001.png" },
            { "DEFENSE", "DEFENSE_001.png" },
            { "GENETICS", "GENETICS_001.png" },
            { "RESEARCH", "RESEARCH_001.png" },
            { "WAREHOUSE", "WAREHOUSE_001.png" },
            { "TRANSFORMATION", "TRANSFORMATION_001.png" },
            { "INFIRMARY", "INFIRMARY_001.png" },
            { "ALLIANCE_CENTER", "ALLIANCE_CENTER_001.png" },
            { "ACADEMY", "ACADEMY_001.png" },
            { "BANK", "BANK_001.png" },
            { "ROYAL_PALACE", "ROYAL_PALACE.png" },
            { "CHAMPION_HALL", "CHAMPION_HALL_001.png" }
        };

        // Override validé ROYAL_PALACE (miroir du BuildingArtworkScanner éditeur).
        private static readonly ArtworkScan RoyalPalaceOverride = new ArtworkScan
        {
            Width = 1536,
            Height = 1024,
            ContactX = 650,
            ContactYFromTop = 1021,
            ContactU = 0.4231770833333333f,
            ContactV = 0.0029296875f
        };

        [Serializable]
        private sealed class SidecarViewFile
        {
            public SidecarViewEntry[] placements;
        }

        [Serializable]
        private sealed class SidecarViewEntry
        {
            public string buildingId;
            public string buildingType;
            public float X;
            public float TerrainY;
            public float Z;
            public float Rotation;
            public float Scale;
        }

        private struct ArtworkScan
        {
            public int Width;
            public int Height;
            public int ContactX;
            public int ContactYFromTop;
            public float ContactU;
            public float ContactV;
            public float Aspect;
            public bool Valid;
        }

        // --- Point d'entrée autonome : s'exécute au play-mode sans câblage scène. ---
        private const string RuntimeRootName = "BeeKingdom BuildingInteraction Runtime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;

            BuildingInteractionController controller = FindOrCreateController(active);
            if (controller == null) return;

            MaterializeInto(controller); // visuels + zones de clic + markers masqués
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            if (scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal)) return true;
            // Scène connue sous un autre nom mais contenant la racine 2.5D.
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("Environment2D5D", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public static void AutoStartForScene(Scene scene)
        {
            if (!Application.isPlaying) return;
            if (!IsEnvironmentScene(scene)) return;

            BuildingInteractionController controller = FindOrCreateController(scene);
            if (controller == null) return;

            MaterializeInto(controller); // visuels + zones de clic + markers masqués
        }

        private static string GetSidecarPathForScene(Scene scene)
        {
            // Cherche un contexte HiveMap dans la scène
            var context = UnityEngine.Object.FindFirstObjectByType<HiveMapPlacementContext>();
            if (context != null && !string.IsNullOrEmpty(context.sidecarPath))
            {
                return context.sidecarPath;
            }
            return DefaultRelativeSidecarPath;
        }

        private static BuildingInteractionController FindOrCreateController(Scene scene)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                foreach (GameObject root in sc.GetRootGameObjects())
                {
                    BuildingInteractionController existing = root.GetComponentInChildren<BuildingInteractionController>(true);
                    if (existing != null) return existing;
                }
            }

            GameObject rootGo = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(rootGo, scene);
            return rootGo.AddComponent<BuildingInteractionController>();
        }

        private static void MaterializeInto(BuildingInteractionController controller)
        {
            if (VisualsAlreadyPresent(controller.Registry)) return;
            Scene active = SceneManager.GetActiveScene();
            MaterializeRuntimeVisualBuildings(controller.Registry, active);
            HideDevMarkers();
            EnsureSelectionFeedback(controller);
        }

        // Câble le retour de sélection visuel (CLICK -> SELECTION -> HIGHLIGHT) sur le
        // contrôleur d'interaction, sans créer de nouveau système de sélection : il réutilise
        // BuildingSelectionService (événements) + BuildingSelectionHighlight (affichage).
        private static void EnsureSelectionFeedback(BuildingInteractionController controller)
        {
            if (controller == null) return;
            BuildingSelectionFeedback existing = controller.GetComponent<BuildingSelectionFeedback>();
            if (existing != null) return;
            controller.gameObject.AddComponent<BuildingSelectionFeedback>().Initialize(controller);
        }

        private static bool VisualsAlreadyPresent(BuildingInteractionRegistry registry)
        {
            for (int sc = 0; sc < SceneManager.sceneCount; sc++)
            {
                Scene scene = SceneManager.GetSceneAt(sc);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root == null) continue;
                    if (root.name.StartsWith(VisualRootNamePrefix, StringComparison.Ordinal)) return true;
                }
            }
            return registry != null && registry.Count >= BuildingTypes.All.Length;
        }

        // --- Matérialisation des 14 bâtiments visibles + cliquables (M-INTERACTION). ---
        public static int MaterializeRuntimeVisualBuildings(BuildingInteractionRegistry registry)
        {
            // Compat overload: utilise la scène active comme fallback
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid()) activeScene = SceneManager.GetSceneAt(0);
            return MaterializeRuntimeVisualBuildings(registry, activeScene);
        }

        public static int MaterializeRuntimeVisualBuildings(BuildingInteractionRegistry registry, Scene scene)
        {
            if (registry == null) throw new ArgumentNullException("registry");

            string relative = GetSidecarPathForScene(scene);
            string dataPath = Application.dataPath;
            if (relative.StartsWith("Assets/")) relative = relative.Substring("Assets/".Length);
            string fullPath = Path.Combine(dataPath, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath)) return 0;

            SidecarViewFile save;
            try
            {
                save = JsonUtility.FromJson<SidecarViewFile>(File.ReadAllText(fullPath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BuildingRuntimeViewBootstrap] Lecture sidecar impossible : " + e.Message);
                return 0;
            }
            if (save == null || save.placements == null) return 0;

            int count = 0;
            for (int i = 0; i < save.placements.Length; i++)
            {
                SidecarViewEntry entry = save.placements[i];
                if (entry == null || string.IsNullOrEmpty(entry.buildingType)) continue;
                string buildingType = EntryToType(entry.buildingType);
                if (buildingType == null) continue;

                GameObject go = CreateVisualBuilding(buildingType, entry);
                if (go == null) continue;

                registry.Register(go, buildingType);
                count++;
            }
            return count;
        }

        private static GameObject CreateVisualBuilding(string buildingType, SidecarViewEntry entry)
        {
            string fileName;
            if (!ArtworkByType.TryGetValue(buildingType, out fileName)) return null;

            string artRelative = (RelativeArtRoot + "/" + fileName);
            if (artRelative.StartsWith("Assets/")) artRelative = artRelative.Substring("Assets/".Length);
            string artPath = Path.Combine(Application.dataPath, artRelative.Replace('/', Path.DirectorySeparatorChar));

            float scale = entry.Scale > 0f ? entry.Scale : 1f;

            GameObject root = new GameObject(VisualRootNamePrefix + buildingType);
            root.layer = BuildingInteractionController.InteractionLayer;
            root.transform.position = new Vector3(entry.X, entry.TerrainY, entry.Z);
            root.transform.rotation = Quaternion.Euler(0f, entry.Rotation, 0f);

            ArtworkScan scan = ScanArtwork(artPath);
            if (!scan.Valid)
            {
                Debug.LogWarning("[BuildingRuntimeViewBootstrap] Artwork illisible : " + artPath);
                DestroyQuiet(root);
                return null;
            }

            float meshW = CanvasHeightWorld * scan.Aspect;
            float meshH = CanvasHeightWorld;

            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = new Vector3(scale, scale, 1f);

            GameObject quadGo = new GameObject("VisualQuad");
            quadGo.transform.SetParent(visual, false);
            quadGo.AddComponent<MeshFilter>().sharedMesh = BuildQuadMesh(meshW, meshH, scan);

            Shader shader = Shader.Find(ShaderName);
            Material material = shader != null
                ? new Material(shader) { name = "BuildingPlacementMat_" + buildingType }
                : null;
            Texture2D artwork = LoadTexture(artPath);
            if (material != null && artwork != null)
            {
                material.SetTexture("_MainTex", artwork);
                material.SetColor("_Color", Color.white);
                material.renderQueue = 3000;
                quadGo.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            else
            {
                if (material == null) Debug.LogWarning("[BuildingRuntimeViewBootstrap] Shader introuvable : " + ShaderName);
                if (artwork == null) Debug.LogWarning("[BuildingRuntimeViewBootstrap] Texture introuvable : " + artPath);
            }

            // Zone de clic : boîte couvrant le visuel, ancrée au point de contact au sol.
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(meshW * scale, meshH * scale, 0.5f);
            collider.center = new Vector3(
                0f,
                (1f - scan.ContactV - 0.5f) * meshH * scale,
                0f);

            BuildingInteractionComponent interaction = root.AddComponent<BuildingInteractionComponent>();
            interaction.Configure(buildingType);
            return root;
        }

        // --- Masquage runtime des marqueurs de dev (croix cyan / pôles / labels). ---
        // Par nom, sans dépendance de type : les objets de scène sont intouchés.
        public static int HideDevMarkers()
        {
            int hidden = 0;
            for (int sc = 0; sc < SceneManager.sceneCount; sc++)
            {
                Scene scene = SceneManager.GetSceneAt(sc);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    hidden += HideInChildren(root);
                }
            }
            return hidden;
        }

        private static int HideInChildren(GameObject go)
        {
            int hidden = 0;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (!IsDevMarker(r.gameObject)) continue;
                if (r.enabled)
                {
                    r.enabled = false;
                    hidden++;
                }
            }
            return hidden;
        }

        private static bool IsDevMarker(GameObject go)
        {
            if (go == null) return false;
            string n = go.name;
            if (n.StartsWith("SurfaceRep", StringComparison.Ordinal)) return true;
            if (n.Equals("Pole", StringComparison.Ordinal)) return true;
            if (n.Equals("Label", StringComparison.Ordinal)) return true;
            // BaseDisc (cylindre de base) et Tip (sphère de tête) des marqueurs d'ancres :
            // eux aussi doivent disparaître au runtime (TEST A exige des marqueurs invisibles).
            if (n.Equals("BaseDisc", StringComparison.Ordinal)) return true;
            if (n.Equals("Tip", StringComparison.Ordinal)) return true;
            // Les quads/croix enfants d'un "SurfaceRepère" héritent du parent dev.
            Transform t = go.transform.parent;
            while (t != null)
            {
                if (t.name.StartsWith("SurfaceRep", StringComparison.Ordinal)) return true;
                t = t.parent;
            }
            return false;
        }

        // --- Scan runtime de l'artwork (miroir non-éditeur de BuildingArtworkScanner). ---
        private static void DestroyQuiet(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        private static ArtworkScan ScanArtwork(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) return default(ArtworkScan);

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(fullPath);
            }
            catch (Exception)
            {
                return default(ArtworkScan);
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, bytes) || tex.width <= 0 || tex.height <= 0)
            {
                DestroyQuiet(tex);
                return default(ArtworkScan);
            }

            int width = tex.width;
            int height = tex.height;
            Color32[] pixels = tex.GetPixels32();

            bool isRoyal = fullPath.EndsWith("ROYAL_PALACE.png", StringComparison.OrdinalIgnoreCase);
            ArtworkScan scan;
            if (isRoyal)
            {
                scan = RoyalPalaceOverride;
                scan.Width = 1536;
                scan.Height = 1024;
                scan.Aspect = ScanAspect(1536, 1024);
                scan.Valid = true;
            }
            else
            {
                scan = ResolveOpaqueBoundsScan(width, height, pixels);
                if (!scan.Valid)
                {
                    DestroyQuiet(tex);
                    return default(ArtworkScan);
                }
            }

            DestroyQuiet(tex);
            return scan;
        }

        private static ArtworkScan ResolveOpaqueBoundsScan(int width, int height, Color32[] pixels)
        {
            ArtworkScan scan = default(ArtworkScan);
            scan.Width = width;
            scan.Height = height;

            int opaqueMinX = width;
            int opaqueMaxX = -1;
            int opaqueMinYFromTop = height;
            int opaqueMaxYFromTop = -1;
            bool any = false;

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a < AlphaThreshold) continue;
                    if (x < opaqueMinX) opaqueMinX = x;
                    if (x > opaqueMaxX) opaqueMaxX = x;
                    if (y < opaqueMinYFromTop) opaqueMinYFromTop = y;
                    if (y > opaqueMaxYFromTop) opaqueMaxYFromTop = y;
                    any = true;
                }
            }
            if (!any) return default(ArtworkScan);

            scan.ContactYFromTop = opaqueMaxYFromTop;
            scan.ContactX = (opaqueMinX + opaqueMaxX) / 2;
            if (scan.ContactX <= 0) scan.ContactX = width / 2;

            scan.ContactU = (float)scan.ContactX / width;
            scan.ContactV = 1f - (float)scan.ContactYFromTop / height;
            scan.Aspect = (float)width / height;
            scan.Valid = true;
            return scan;
        }

        private static float ScanAspect(int width, int height)
        {
            return width > 0 && height > 0 ? (float)width / height : 1f;
        }

        private static Texture2D LoadTexture(string fullPath)
        {
            if (!File.Exists(fullPath)) return null;
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(fullPath);
            }
            catch (Exception)
            {
                return null;
            }
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, bytes) || tex.width <= 0 || tex.height <= 0)
            {
                DestroyQuiet(tex);
                return null;
            }
            return tex;
        }

        private static Mesh BuildQuadMesh(float w, float h, ArtworkScan scan)
        {
            Mesh mesh = new Mesh { name = "BuildingPlacementQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-scan.ContactU * w, -scan.ContactV * h, 0f),
                new Vector3((1f - scan.ContactU) * w, -scan.ContactV * h, 0f),
                new Vector3((1f - scan.ContactU) * w, (1f - scan.ContactV) * h, 0f),
                new Vector3(-scan.ContactU * w, (1f - scan.ContactV) * h, 0f)
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

        private static string EntryToType(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            string upper = raw.Trim().ToUpperInvariant();
            for (int i = 0; i < BuildingTypes.All.Length; i++)
            {
                if (string.Equals(BuildingTypes.All[i], upper, StringComparison.Ordinal))
                    return BuildingTypes.All[i];
            }
            return null;
        }
    }
}