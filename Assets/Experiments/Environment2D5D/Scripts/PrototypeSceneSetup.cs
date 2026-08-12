#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace BeeKingdom.Experiments.Environment2D5D
{
    public static class PrototypeSceneSetup
    {
        private const string ExperimentRoot = "Assets/Experiments/Environment2D5D";
        private const string ScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";

        private const float ImageWorldWidth = 100f;
        private const float BackdropZ = 30f;
        private const float LookTargetY = 30f;
        private const float DefaultCameraY = 30f;
        private const float DefaultCameraZ = -24f;

        [MenuItem("BeeKingdom/Experiments/Create 2.5D Building Perspective Lab")]
        public static void CreatePrototypeScene()
        {
            SceneSetup();
        }

        public static void SceneSetup()
        {
            // Create scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Environment2D5D_SpatialV3";

            // Create root object (NO EditorOnly tag - must ship in builds)
            GameObject root = new GameObject("Environment2D5D_SpatialV3");

            // Load reference image (kept exactly as is, no modification)
            Texture2D refImage = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ExperimentRoot}/Textures/ReferenceImage.png");
            if (!refImage)
            {
                Debug.LogError("[Environment2D5D] Reference image not found!");
                return;
            }

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");

            // ---------- Backdrop: single flat vertical plane with the untouched image ----------
            // This 3D quad stays ON the depth surface (z = BackdropZ) as the invisible
            // SURFACE REFERENCE of the anchor system (UV <-> world mapping). It is never
            // rendered: the VISUAL backdrop is the frontal 2D image (child quad of the
            // camera, see FrontalBackdrop below). B (debug) can re-enable it.
            float planeH = ImageWorldWidth / ((float)refImage.width / refImage.height);

            GameObject backdropObj = new GameObject("Backdrop");
            backdropObj.transform.SetParent(root.transform);
            backdropObj.transform.position = new Vector3(0f, planeH * 0.5f, BackdropZ);

            Mesh backdropMesh = new Mesh { name = "BackdropQuad" };
            float hw = ImageWorldWidth * 0.5f;
            float hh = planeH * 0.5f;
            backdropMesh.vertices = new[]
            {
                new Vector3(-hw, -hh, 0f),
                new Vector3(hw, -hh, 0f),
                new Vector3(hw, hh, 0f),
                new Vector3(-hw, hh, 0f)
            };
            backdropMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            backdropMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            backdropMesh.RecalculateNormals();
            backdropMesh.RecalculateBounds();

            backdropObj.AddComponent<MeshFilter>().sharedMesh = backdropMesh;
            var backdropRenderer = backdropObj.AddComponent<MeshRenderer>();
            Material backdropMat = new Material(unlitShader) { name = "BackdropMat" };
            backdropMat.mainTexture = refImage;
            backdropMat.SetTexture("_BaseMap", refImage);
            backdropMat.SetTexture("_MainTex", refImage);
            backdropRenderer.sharedMaterial = backdropMat;
            // Invisible by default: the visual backdrop is the frontal 2D image (below).
            // Only the ANCHOR SURFACE reference (UV <-> world, z = BackdropZ) stays active.
            backdropObj.SetActive(false);

            // ---------- Camera (real 3D perspective, movable vertically/horizontally) ----------
            GameObject cameraObj = new GameObject("PrototypeCamera");
            cameraObj.transform.SetParent(root.transform);
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(0f, DefaultCameraY, DefaultCameraZ);

            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.1f);
            camera.cullingMask = ~0;
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            // Far clip extended: the frontal image quad sits at the anchor-plane view
            // distance (up to ~220 u) and the anchors are drawn on top of it.
            camera.farClipPlane = 600f;
            camera.depth = 0;

            var camController = cameraObj.AddComponent<BuildingPerspectiveCamera>();
            camController.cameraComponent = camera;
            camController.fov = 55f;
            camController.lowPitch = 10f;
            camController.lowAnchorY = 22f;
            camController.mediumPitch = 25f;
            camController.mediumAnchorY = 30f;
            camController.highPitch = 40f;
            camController.highAnchorY = 38f;
            camController.defaultPitch = 12f;
            camController.defaultAnchorY = 30f;
            camController.defaultDistance = 54f;
            camController.pitch = camController.defaultPitch;
            camController.distance = camController.defaultDistance;
            camController.anchor = new Vector2(0f, camController.defaultAnchorY);

            // ---------- Anchor markers (thin ground anchor points for future buildings) ----------
            // CRITICAL: anchors are placed ON the backdrop surface, never floating in front of it.
            // The painted landscape has no depth: the surface is the flat plane (z = BackdropZ).
            // World position is computed from a precise image UV through the EXACT plane mapping
            // (x = (u-0.5)*100, y = v*planeH, z = 30) using the real plane height (planeH is
            // derived from the imported texture size, which may differ slightly from 60 when the
            // import pipeline resizes the texture). UVs below are starting positions targeting
            // identifiable zones (A mountain / B center / C foreground); refine visually in
            // MARKER mode (WASD = X/Y along the surface) and read the final UV in the HUD.
            List<AnchorMarker> markers = new List<AnchorMarker>();
            markers.Add(CreateAnchorMarker(root, "A", "FAR (mountain)", new Color(1f, 0.3f, 0.3f),
                SurfacePointOnPlane(new Vector2(0.35f, 0.7167f), planeH), planeH, litShader, unlitShader, cameraObj.transform));
            markers.Add(CreateAnchorMarker(root, "B", "MID (center)", new Color(1f, 0.9f, 0.2f),
                SurfacePointOnPlane(new Vector2(0.50f, 0.50f), planeH), planeH, litShader, unlitShader, cameraObj.transform));
            markers.Add(CreateAnchorMarker(root, "C", "NEAR (foreground)", new Color(0.3f, 1f, 1f),
                SurfacePointOnPlane(new Vector2(0.60f, 0.2333f), planeH), planeH, litShader, unlitShader, cameraObj.transform));
            // BUILDING TEST: dedicated anchor on a clear meadow zone (right field,
            // away from the village / forest / stream) supporting the premium 3D
            // building test. Selected with B; moved like any anchor (MARKER mode).
            markers.Add(CreateAnchorMarker(root, "BUILDING", "BUILDING TEST", new Color(0.85f, 0.5f, 1f),
                SurfacePointOnPlane(new Vector2(0.85f, 0.30f), planeH), planeH, litShader, unlitShader, cameraObj.transform));

            // ---------- Test UI ----------
            GameObject uiObj = new GameObject("TestUI");
            uiObj.transform.SetParent(root.transform);
            var testUI = uiObj.AddComponent<AnchorMarkerUI>();
            testUI.cameraController = camController;
            testUI.markers = markers.ToArray();
            testUI.backdrop = backdropObj;
            testUI.buildingAnchor = markers[markers.Count - 1];

            // ---------- Premium 3D building (BUILDING TEST) ----------
            // Procedural organic BeeKingdom building (wax rotunda + dome + roots + wood
            // door + honeycomb windows) standing at the BUILDING_TEST_ANCHOR world point.
            // Rendered by the same 3D perspective camera, IN FRONT of the frontal 2D
            // backdrop (world position slightly in front of the plane surface).
            AnchorMarker buildingAnchor = markers[markers.Count - 1];
            Vector3 buildingBase = buildingAnchor.transform.position;
            Shader premiumShader = Shader.Find("BeeKingdom/Experiments/PremiumBuilding");
            Shader shadowShader = Shader.Find("BeeKingdom/Experiments/SoftShadow");
            GameObject buildingObj = PremiumBuildingFactory.Build(root.transform, buildingBase, premiumShader, shadowShader);
            var buildingCtrl = buildingObj.AddComponent<BuildingPremiumController>();
            buildingCtrl.buildingAnchor = buildingAnchor;
            buildingCtrl.visualRoot = buildingObj.transform;

            // ---------- Anchor validation harness (automated world-lock + screen-return test) ----------
            GameObject validationObj = new GameObject("AnchorValidation");
            validationObj.transform.SetParent(root.transform);
            validationObj.AddComponent<AnchorValidation>();

            // ---------- Frontal 2D backdrop: the image is a child quad of the camera --------
            // The image is displayed as a perfectly flat, full 2D layer glued to the camera:
            // no perspective, no trapezoid, no tilt, at any camera pose (zoom/pan/pitch).
            // The quad is placed each frame at the anchor plane's view distance and
            // aspect-fitted (whole image always visible). The anchors (invisible world-space
            // surface system, z = BackdropZ) are rendered ON TOP of it by this same camera.
            GameObject frontalObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frontalObj.name = "FrontalBackdrop";
            frontalObj.transform.SetParent(cameraObj.transform, false);
            // Identity rotation: Unity's built-in quad faces the camera from its -Z side
            // (same convention as the surface repère crosses that render correctly).
            // Material is double-sided (_Cull 0) so no winding/rotation issue can ever
            // make the image invisible.
            Object.DestroyImmediate(frontalObj.GetComponent<Collider>());
            Material frontalMat = new Material(unlitShader) { name = "FrontalBackdropMat" };
            frontalMat.mainTexture = refImage;
            frontalMat.SetTexture("_BaseMap", refImage);
            frontalMat.SetTexture("_MainTex", refImage);
            frontalMat.SetInt("_Cull", 0);
            frontalObj.GetComponent<Renderer>().sharedMaterial = frontalMat;
            var frontal = frontalObj.AddComponent<FrontalBackdrop>();
            frontal.image = refImage;
            frontal.targetCamera = camera;

            // ---------- Event system (Input System UI module) ----------
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.transform.SetParent(root.transform);
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // ---------- Directional light ----------
            GameObject lightObj = new GameObject("DirectionalLight");
            lightObj.transform.SetParent(root.transform);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.4f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.55f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.32f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.22f);
            RenderSettings.ambientIntensity = 1f;

            // Save scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Environment2D5D] Anchor Point Lab created at {ScenePath}");
            Debug.Log("[Environment2D5D] FRONTAL backdrop (2D image glued to camera, zero perspective) + anchors on depth surface (z=30, UV->world) | M=switch mode | 1/2/3 select | WASD X/Y on surface | Shift fine | X=grid+HUD debug | 0=reset camera");
        }

        private static Vector3 SurfacePointOnPlane(Vector2 uv, float planeH)
        {
            return new Vector3(uv.x * ImageWorldWidth - ImageWorldWidth * 0.5f, uv.y * planeH, BackdropZ);
        }

        private static AnchorMarker CreateAnchorMarker(GameObject root, string id, string name, Color color,
            Vector3 pos, float planeH, Shader litShader, Shader unlitShader, Transform cameraRef)
        {
            GameObject marker = new GameObject("AnchorMarker_" + id);
            marker.transform.SetParent(root.transform);
            marker.transform.position = pos;

            Material mat = new Material(litShader) { name = "AnchorMat_" + id };
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.2f);
            mat.SetFloat("_Smoothness", 0.3f);
            mat.SetFloat("_Metallic", 0f);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(marker.transform, false);
            pole.transform.localPosition = new Vector3(0f, 3f, 0f);
            pole.transform.localScale = new Vector3(0.25f, 3f, 0.25f);
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseDisc.name = "BaseDisc";
            baseDisc.transform.SetParent(marker.transform, false);
            baseDisc.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            baseDisc.transform.localScale = new Vector3(1.2f, 0.04f, 1.2f);
            Object.DestroyImmediate(baseDisc.GetComponent<Collider>());
            baseDisc.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Tip";
            tip.transform.SetParent(marker.transform, false);
            tip.transform.localPosition = new Vector3(0f, 6.2f, 0f);
            tip.transform.localScale = Vector3.one * 0.6f;
            Object.DestroyImmediate(tip.GetComponent<Collider>());
            tip.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(marker.transform, false);
            labelObj.transform.localPosition = new Vector3(0.4f, 7.2f, 0f);
            TextMesh label = labelObj.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.characterSize = 0.12f;
            label.anchor = TextAnchor.MiddleLeft;
            label.alignment = TextAlignment.Left;
            label.color = Color.white;

            var anchor = marker.AddComponent<AnchorMarker>();
            anchor.labelTransform = labelObj.transform;
            anchor.cameraRef = cameraRef;
            anchor.planeHeight = planeH;
            anchor.Initialize(id, name, color, mat, label, cameraRef);
            anchor.RefreshLabel();

            // Surface repère: flat crosshair glued to the painting at the anchor's base point.
            // AnchorMarker.LateUpdate keeps it on the surface (x, y, BackdropZ + small offset).
            GameObject repObj = new GameObject("SurfaceRepère_" + id);
            repObj.transform.SetParent(root.transform);
            repObj.transform.position = new Vector3(pos.x, pos.y, AnchorMarker.BackdropZ + 0.03f);
            Material repMat = new Material(unlitShader) { name = "RepèreMat_" + id };
            repMat.color = color;
            repMat.renderQueue = 4000;
            CreateQuad(repObj.transform, new Vector3(0f, 0f, 0f), new Vector3(1.1f, 0.07f, 1f), repMat);
            CreateQuad(repObj.transform, new Vector3(0f, 0f, 0f), new Vector3(0.07f, 1.1f, 1f), repMat);
            anchor.surfaceMarker = repObj.transform;

            return anchor;
        }

        private static GameObject CreateQuad(Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Quad";
            q.transform.SetParent(parent, false);
            q.transform.localPosition = localPos;
            q.transform.localScale = localScale;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.GetComponent<Renderer>().sharedMaterial = mat;
            return q;
        }
    }
}
#endif
