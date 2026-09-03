using System;
using System.Collections.Generic;
using System.IO;
using BeeKingdom.Buildings.Placement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Petites abeilles ambiantes qui voyagent entre le Palais Royal et les batiments
    // satellites sur le fond hivebg.png (demande de Jeff, 2026-08-26 : "des petites
    // abeilles qui s'activent dans ces chemins"). Purement decoratif - aucune donnee de
    // gameplay, juste de la vie ambiante sur les vrais chemins visibles dans l'illustration
    // de fond, en reutilisant les positions reelles des batiments (sidecar JSON, meme
    // source que BuildingRuntimeViewBootstrap) et le sprite/battement d'ailes de la
    // Gardienne deja construit pour la marche d'attaque (Assets/BeeKingdom/Playground/
    // Resources/WorldMapWave6Runtime/CombatMarch).
    public sealed class HiveMapAmbientBeesBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Ambient Bees Runtime";
        private const string DefaultRelativeSidecarPath = "Assets/Experiments/Environment2D5D/Config/BuildingPlacementEditor_Saves.json";
        private const string ShaderName = "BeeKingdom/Experiments/ArtworkUnlit";
        private const string BodyResourcePath = "WorldMapWave6Runtime/CombatMarch/CombatMarchBeeBody";
        private const string WingsResourcePath = "WorldMapWave6Runtime/CombatMarch/CombatMarchBeeWings";
        private const float BeeWorldSize = 2.6f;
        private const int FlyerCount = 3;
        // Ouvrieres qui marchent au sol le long des chemins (demande de Jeff, 2026-08-26 :
        // "je veux de petites ouvrieres qui marchent sur les chemins" - distinctes des
        // abeilles volantes deja en place, qu'il a demande de garder telles quelles).
        private const int WalkerCount = 3;
        private const float WalkerWorldSize = 1.9f;

        [Serializable] private sealed class SidecarViewFile { public SidecarViewEntry[] placements; }
        [Serializable]
        private sealed class SidecarViewEntry
        {
            public string buildingId;
            public string buildingType;
            public float X;
            public float TerrainY;
            public float Z;
        }

        private sealed class AmbientBee
        {
            public Transform Root;
            public Transform Wings;
            public Vector3 From;
            public Vector3 To;
            public float Speed;
            public float Progress;
            public bool Forward = true;
            public float FlapPhase;
            public bool Flying;
        }

        private readonly List<AmbientBee> bees = new List<AmbientBee>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            InitializeForScene(active);
        }

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying) return;
            if (!IsEnvironmentScene(scene)) return;
            if (UnityEngine.Object.FindFirstObjectByType<HiveMapAmbientBeesBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapAmbientBeesBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        private void Start()
        {
            List<Vector3> waypoints = LoadWaypoints();
            if (waypoints.Count < 2) return; // no sidecar data yet - nothing to animate between

            Texture2D body = LoadTexture(BodyResourcePath);
            Texture2D wings = LoadTexture(WingsResourcePath);
            if (body == null || wings == null) return;

            Material material = CreateMaterial(body, "AmbientBeeBodyMat");
            Material wingMaterial = CreateMaterial(wings, "AmbientBeeWingsMat");
            if (material == null || wingMaterial == null) return;

            Vector3 hub = waypoints[0];
            for (int i = 0; i < FlyerCount; i++)
            {
                Vector3 destination = waypoints[1 + i % (waypoints.Count - 1)];
                bees.Add(CreateBee(hub, destination, material, wingMaterial, "AmbientBeeFlyer_" + i, flying: true));
            }

            // Les ouvrieres marchent au sol : les positions de batiments (donnees de jeu)
            // ne correspondent pas aux chemins peints dans hivebg.png (illustration
            // independante), donc un trajet batiment-a-batiment coupe a travers l'herbe
            // entre les chemins - corrige le 2026-08-26 suite au retour de Jeff ("tu les
            // fais marcher entre les chemins, pas sur eux"). A la place, on les fait
            // marcher sur la grande allee centrale verticale qui traverse le palais du
            // haut en bas - le seul chemin non ambigu de l'illustration. Calibration
            // mesuree en jeu sur le GameObject FrontalBackdrop : le fond couvre le monde
            // X in [-50,50], Y in [0,100] a Z=30 (constant, meme plan que les batiments).
            const float PathTopY = 97f;
            const float PathBottomY = 3f;
            for (int i = 0; i < WalkerCount; i++)
            {
                float lane = (i - (WalkerCount - 1) * 0.5f) * 1.5f; // voies paralleles pour eviter la superposition
                Vector3 laneTop = new Vector3(hub.x + lane, PathTopY, hub.z);
                Vector3 laneBottom = new Vector3(hub.x + lane, PathBottomY, hub.z);
                bees.Add(CreateBee(laneTop, laneBottom, material, wingMaterial, "AmbientBeeWalker_" + i, flying: false));
            }
        }

        private List<Vector3> LoadWaypoints()
        {
            var waypoints = new List<Vector3>();
            var context = UnityEngine.Object.FindFirstObjectByType<HiveMapPlacementContext>();
            string relative = context != null && !string.IsNullOrEmpty(context.sidecarPath) ? context.sidecarPath : DefaultRelativeSidecarPath;
            if (relative.StartsWith("Assets/")) relative = relative.Substring("Assets/".Length);
            string fullPath = Path.Combine(Application.dataPath, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath)) return waypoints;

            SidecarViewFile save;
            try { save = JsonUtility.FromJson<SidecarViewFile>(File.ReadAllText(fullPath)); }
            catch (Exception) { return waypoints; }
            if (save?.placements == null) return waypoints;

            Vector3? palace = null;
            var satellites = new List<Vector3>();
            foreach (SidecarViewEntry entry in save.placements)
            {
                if (entry == null || string.IsNullOrEmpty(entry.buildingType)) continue;
                Vector3 position = new Vector3(entry.X, entry.TerrainY, entry.Z);
                if (string.Equals(entry.buildingType.Trim(), "ROYAL_PALACE", StringComparison.OrdinalIgnoreCase)) palace = position;
                else satellites.Add(position);
            }
            if (palace == null || satellites.Count == 0) return waypoints;

            waypoints.Add(palace.Value);
            waypoints.AddRange(satellites);
            return waypoints;
        }

        private static Texture2D LoadTexture(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private static Material CreateMaterial(Texture2D texture, string name)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null) return null;
            Material material = new Material(shader) { name = name };
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", Color.white);
            material.renderQueue = 3100;
            return material;
        }

        private AmbientBee CreateBee(Vector3 hub, Vector3 destination, Material bodyMaterial, Material wingMaterial, string name, bool flying)
        {
            GameObject root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, gameObject.scene);
            root.transform.SetParent(transform, false);
            root.transform.position = hub;

            float size = flying ? BeeWorldSize : WalkerWorldSize;
            Transform wingsTransform = null;
            // Les marcheuses gardent des ailes visibles mais quasi immobiles (repliees) -
            // seules les volantes ont le vrai battement rapide de la marche d'attaque.
            wingsTransform = BuildQuad(root.transform, "Wings", wingMaterial, size * 1.2f, new Vector3(0f, 0.05f, -0.01f));
            BuildQuad(root.transform, "Body", bodyMaterial, size, Vector3.zero);

            return new AmbientBee
            {
                Root = root.transform,
                Wings = wingsTransform,
                From = hub,
                To = destination,
                Speed = flying ? UnityEngine.Random.Range(0.10f, 0.16f) : UnityEngine.Random.Range(0.045f, 0.07f),
                Progress = UnityEngine.Random.Range(0f, 1f),
                FlapPhase = UnityEngine.Random.Range(0f, 10f),
                Flying = flying
            };
        }

        private static Transform BuildQuad(Transform parent, string name, Material material, float worldSize, Vector3 localOffset)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localOffset;

            float half = worldSize * 0.5f;
            Mesh mesh = new Mesh { name = name + "Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-half, -half, 0f), new Vector3(half, -half, 0f),
                new Vector3(half, half, 0f), new Vector3(-half, half, 0f)
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }

        private void Update()
        {
            if (bees.Count == 0) return;
            float t = Time.time;
            foreach (AmbientBee bee in bees)
            {
                if (bee.Root == null) continue;

                bee.Progress += Time.deltaTime * bee.Speed * (bee.Forward ? 1f : -1f);
                if (bee.Progress >= 1f) { bee.Progress = 1f; bee.Forward = false; }
                else if (bee.Progress <= 0f) { bee.Progress = 0f; bee.Forward = true; }

                Vector3 position;
                Vector3 tangent;
                if (bee.Flying)
                {
                    // Vol : arc au-dessus des chemins, comme la marche d'attaque.
                    Vector3 control = Vector3.Lerp(bee.From, bee.To, 0.5f) + new Vector3(0f, 0f, -2.5f);
                    position = Bezier(bee.From, control, bee.To, bee.Progress);
                    tangent = Bezier(bee.From, control, bee.To, Mathf.Min(1f, bee.Progress + 0.01f)) - position;
                }
                else
                {
                    // Marche : reste au sol sur le chemin, avec un leger dandinement de pas.
                    position = Vector3.Lerp(bee.From, bee.To, bee.Progress);
                    position.y += Mathf.Abs(Mathf.Sin(t * 8f + bee.FlapPhase)) * 0.08f;
                    tangent = bee.To - bee.From;
                }
                bee.Root.position = position;
                if (tangent.sqrMagnitude > 0.0001f)
                {
                    float facing = tangent.x >= 0f ? 1f : -1f;
                    bee.Root.localScale = new Vector3(facing, 1f, 1f);
                }

                if (bee.Wings != null)
                {
                    float flap = bee.Flying
                        ? Mathf.Abs(Mathf.Sin(t * 30f + bee.FlapPhase))
                        : 0.85f + 0.05f * Mathf.Sin(t * 3f + bee.FlapPhase); // ailes repliees, quasi immobiles
                    float scaleY = Mathf.Lerp(0.32f, 1f, flap);
                    bee.Wings.localScale = new Vector3(1f, scaleY, 1f);
                }
            }
        }

        private static Vector3 Bezier(Vector3 a, Vector3 c, Vector3 b, float progress)
        {
            float u = 1f - progress;
            return u * u * a + 2f * u * progress * c + progress * progress * b;
        }
    }
}
