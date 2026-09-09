using UnityEditor;
using UnityEngine;

namespace ESSW.Editorcontroller
{
    public static class WaterToolMenu
    {
        private const string DefaultMaterialName = "water";
        private const string LowPolyPrefabPath = "LowPolyObject";
        private const string HighPolyPrefabPath = "HighPolyObject";
        private const string GeneratedMaterialFolder = "Assets/ESSW Generated/Materials";
        private const string OPEN_WELCOME_PREF = "ESSW_OpenWelcomeAfterCreate";

        [MenuItem("GameObject/3D Object/Add Water/Low Poly", false, 0)]
        public static void AddWaterWithLowPoly()
        {
            AddWaterSurface(LowPolyPrefabPath);
        }

        [MenuItem("GameObject/3D Object/Add Water/High Poly", false, 0)]
        public static void AddWaterWithHighPoly()
        {
            AddWaterSurface(HighPolyPrefabPath);
        }

        // Shared entry point (Menu + Welcome Window)
        public static void AddWaterSurface(string prefabPath)
        {
            // Load base material
            Material baseMaterial = Resources.Load<Material>(DefaultMaterialName);
            if (baseMaterial == null)
            {
                Debug.LogError($"ESSW: Default water material '{DefaultMaterialName}' not found in Resources.");
                return;
            }

            // Load prefab
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"ESSW: Water prefab '{prefabPath}' not found in Resources.");
                return;
            }

            // Scene view position
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                Debug.LogError("ESSW: No active Scene View found.");
                return;
            }

            Vector3 position =
                sceneView.camera.transform.position +
                sceneView.camera.transform.forward * 10f;

            // Instantiate prefab
            GameObject waterObject =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            Undo.RegisterCreatedObjectUndo(waterObject, "Create Water Surface");

            waterObject.name = "Water Surface";
            waterObject.transform.position = position;
            waterObject.transform.rotation = Quaternion.identity;
            waterObject.transform.localScale = new Vector3(200f, 1f, 200f);

            // Ensure folders exist
            if (!AssetDatabase.IsValidFolder("Assets/ESSW Generated"))
                AssetDatabase.CreateFolder("Assets", "ESSW Generated");

            if (!AssetDatabase.IsValidFolder(GeneratedMaterialFolder))
                AssetDatabase.CreateFolder("Assets/ESSW Generated", "Materials");

            // Create material
            Material waterMaterial = new Material(baseMaterial)
            {
                name = "ESSW_Water_Material"
            };

            string materialPath =
                AssetDatabase.GenerateUniqueAssetPath(
                    $"{GeneratedMaterialFolder}/{waterMaterial.name}.mat");

            AssetDatabase.CreateAsset(waterMaterial, materialPath);

            // Assign material
            if (waterObject.TryGetComponent(out Renderer renderer))
                renderer.sharedMaterial = waterMaterial;

            // Add controller
            var controller = waterObject.AddComponent<WaterShaderController>();
            controller.waterMaterial = waterMaterial;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = waterObject;

            // Auto-open Welcome window (once)
            if (!EditorPrefs.GetBool(OPEN_WELCOME_PREF, false))
            {
                ESSW.Welcome.WaterShaderWelcome.ShowWindow();
                EditorPrefs.SetBool(OPEN_WELCOME_PREF, true);
            }

            Debug.Log("ESSW: Water surface created successfully.");
        }
    }
}
