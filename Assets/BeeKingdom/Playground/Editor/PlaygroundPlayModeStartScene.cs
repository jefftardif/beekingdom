using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class PlaygroundPlayModeStartScene
    {
        private const string MainDemoScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string LivingHiveScenePath = "Assets/Scenes/LivingHive.unity";
        private const string Environment2D5DScenesFolder = "Assets/Experiments/Environment2D5D/";
        private const string HiveMapScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_HiveMap_Test.unity";
        private const string Wave5Premium25x25ScenePath = "Assets/Scenes/WorldMapWave5Premium25x25Test.unity";
        private const string Wave6V3ECandidateScenePath = "Assets/Scenes/WorldMapWave6V3ECandidate.unity";
        private const string Wave6V2INativeAuditPreviewScenePath = "Assets/Scenes/WorldMapWave6V2INativeAuditPreview.unity";
        private const string Wave6V2OPerimeterAuditPreviewScenePath = "Assets/Scenes/WorldMapWave6V2OPerimeterAuditPreview.unity";
        private const string Wave6V2IRepairAuditPreviewScenePath = "Assets/Scenes/WorldMapWave6V2IRepairAuditPreview.unity";
        private const string Wave6V3OReducedAuditPreviewScenePath = "Assets/Scenes/WorldMapWave6V3OReducedAuditPreview.unity";
        private const string Wave6SupportCenterNativeAuditPreviewScenePath = "Assets/Scenes/WorldMapWave6SupportCenterNativeAuditPreview.unity";
        private const string Wave6RouteLock8192ScaleBridgeProofScenePath = "Assets/Scenes/WorldMapWave6RouteLock8192ScaleBridgeProofPreview.unity";
        private const string Wave6ExactCropMmoPreviewScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        private const string Wave6ExactCropTerrainTestScenePath = "Assets/Scenes/WorldMapWave6Premium50x50TerrainTest.unity";
        private const string Wave6SharpExistingCandidateReviewScenePath = Wave6V2IRepairAuditPreviewScenePath;

        static PlaygroundPlayModeStartScene()
        {
            EditorApplication.delayCall += ConfigurePlayModeStartScene;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChangedInEditMode;
        }

        [MenuItem("Bee Kingdom/Playground/Open Main Demo Scene")]
        public static void OpenMainDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            EditorSceneManager.OpenScene(MainDemoScenePath, OpenSceneMode.Single);
            UseSandboxPlaygroundOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Living Hive Scene")]
        public static void OpenLivingHiveScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            EditorSceneManager.OpenScene(LivingHiveScenePath, OpenSceneMode.Single);
            UseLivingHiveOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open HiveMap Scene")]
        public static void OpenHiveMapScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            EditorSceneManager.OpenScene(HiveMapScenePath, OpenSceneMode.Single);
            UseHiveMapOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave5 Premium 25x25 Test Scene")]
        public static void OpenWave5Premium25x25TestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            PlayerPrefs.SetInt(SplashDevelopmentSceneConfig.Wave5PremiumMapModeKey, 1);
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene(Wave5Premium25x25ScenePath, OpenSceneMode.Single);
            UseWave5Premium25x25OnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave6 V3E Candidate Scene")]
        public static void OpenWave6V3ECandidateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Debug.LogWarning("Wave6 V3E Candidate is deprecated for final map validation. Opening the route-lock 8192 scale-bridge proof scene instead.");
            EditorSceneManager.OpenScene(Wave6RouteLock8192ScaleBridgeProofScenePath, OpenSceneMode.Single);
            UseWave6RouteLock8192ScaleBridgeProofOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave6 V2I Native Audit Preview Scene")]
        public static void OpenWave6V2INativeAuditPreviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Debug.LogWarning("Wave6 V2I Native Audit is deprecated for final map validation. Opening the route-lock 8192 scale-bridge proof scene instead.");
            EditorSceneManager.OpenScene(Wave6RouteLock8192ScaleBridgeProofScenePath, OpenSceneMode.Single);
            UseWave6RouteLock8192ScaleBridgeProofOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave6 V2O Perimeter Audit Preview Scene")]
        public static void OpenWave6V2OPerimeterAuditPreviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Debug.LogWarning("Wave6 V2O Perimeter Audit is deprecated for final map validation. Opening the route-lock 8192 scale-bridge proof scene instead.");
            EditorSceneManager.OpenScene(Wave6RouteLock8192ScaleBridgeProofScenePath, OpenSceneMode.Single);
            UseWave6RouteLock8192ScaleBridgeProofOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave6 V2I Repair Audit Preview Scene")]
        public static void OpenWave6V2IRepairAuditPreviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Debug.LogWarning("Wave6 V2I Repair Audit is open for sharp existing-candidate review only. It is not a final Unity handoff scene.");
            EditorSceneManager.OpenScene(Wave6SharpExistingCandidateReviewScenePath, OpenSceneMode.Single);
            UseWave6SharpExistingCandidateReviewOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave6 Sharp Existing Candidate Review Scene")]
        public static void OpenWave6SharpExistingCandidateReviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Debug.LogWarning("Wave6 Sharp Existing Candidate Review is for choosing an existing detailed 50x50 source before targeted seam repair. It is not a final Unity handoff scene.");
            EditorSceneManager.OpenScene(Wave6SharpExistingCandidateReviewScenePath, OpenSceneMode.Single);
            UseWave6SharpExistingCandidateReviewOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave6 V3O Reduced Audit Preview Scene")]
        public static void OpenWave6V3OReducedAuditPreviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Debug.LogWarning("Wave6 V3O Reduced Audit is deprecated for final map validation. Opening the route-lock 8192 scale-bridge proof scene instead.");
            EditorSceneManager.OpenScene(Wave6RouteLock8192ScaleBridgeProofScenePath, OpenSceneMode.Single);
            UseWave6RouteLock8192ScaleBridgeProofOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave6 Support Center Native Audit Preview Scene")]
        public static void OpenWave6SupportCenterNativeAuditPreviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(Wave6RouteLock8192ScaleBridgeProofScenePath, OpenSceneMode.Single);
            UseWave6RouteLock8192ScaleBridgeProofOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave6 Route-Lock 8192 Scale-Bridge Proof Scene")]
        public static void OpenWave6RouteLock8192ScaleBridgeProofScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(Wave6RouteLock8192ScaleBridgeProofScenePath, OpenSceneMode.Single);
            UseWave6RouteLock8192ScaleBridgeProofOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave6 50x50 Exact-Crop MMO Preview Scene")]
        public static void OpenWave6ExactCropMmoPreviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(Wave6ExactCropMmoPreviewScenePath, OpenSceneMode.Single);
            UseWave6ExactCropMmoPreviewOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Open Wave6 50x50 Exact-Crop Terrain Test Scene")]
        public static void OpenWave6ExactCropTerrainTestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(Wave6ExactCropTerrainTestScenePath, OpenSceneMode.Single);
            UseWave6ExactCropTerrainTestOnPlay();
        }

        [MenuItem("Bee Kingdom/Playground/Use Sandbox Playground On Play")]
        public static void UseSandboxPlaygroundOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            ConfigurePlayModeStartScene(MainDemoScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Living Hive On Play")]
        public static void UseLivingHiveOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            ConfigurePlayModeStartScene(LivingHiveScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use HiveMap On Play")]
        public static void UseHiveMapOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            ConfigurePlayModeStartScene(HiveMapScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave5 Premium 25x25 On Play")]
        public static void UseWave5Premium25x25OnPlay()
        {
            PlayerPrefs.SetInt(SplashDevelopmentSceneConfig.Wave5PremiumMapModeKey, 1);
            PlayerPrefs.Save();
            ConfigurePlayModeStartScene(Wave5Premium25x25ScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave6 V3E Candidate On Play")]
        public static void UseWave6V3ECandidateOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            Debug.LogWarning("Wave6 V3E Candidate is deprecated for final map validation. Play Mode now redirects to the route-lock 8192 scale-bridge proof scene.");
            ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave6 V2I Native Audit Preview On Play")]
        public static void UseWave6V2INativeAuditPreviewOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            Debug.LogWarning("Wave6 V2I Native Audit is deprecated for final map validation. Play Mode now redirects to the route-lock 8192 scale-bridge proof scene.");
            ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave6 V2O Perimeter Audit Preview On Play")]
        public static void UseWave6V2OPerimeterAuditPreviewOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            Debug.LogWarning("Wave6 V2O Perimeter Audit is deprecated for final map validation. Play Mode now redirects to the route-lock 8192 scale-bridge proof scene.");
            ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave6 V2I Repair Audit Preview On Play")]
        public static void UseWave6V2IRepairAuditPreviewOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            Debug.LogWarning("Wave6 V2I Repair Audit is open for sharp existing-candidate review only. It is not a final Unity handoff scene.");
            ConfigurePlayModeStartScene(Wave6SharpExistingCandidateReviewScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave6 Sharp Existing Candidate Review On Play")]
        public static void UseWave6SharpExistingCandidateReviewOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            ConfigurePlayModeStartScene(Wave6SharpExistingCandidateReviewScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave6 V3O Reduced Audit Preview On Play")]
        public static void UseWave6V3OReducedAuditPreviewOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            Debug.LogWarning("Wave6 V3O Reduced Audit is deprecated for final map validation. Play Mode now redirects to the route-lock 8192 scale-bridge proof scene.");
            ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave6 Support Center Native Audit Preview On Play")]
        public static void UseWave6SupportCenterNativeAuditPreviewOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave6 Route-Lock 8192 Scale-Bridge Proof On Play")]
        public static void UseWave6RouteLock8192ScaleBridgeProofOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave6 50x50 Exact-Crop MMO Preview On Play")]
        public static void UseWave6ExactCropMmoPreviewOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            ConfigurePlayModeStartScene(Wave6ExactCropMmoPreviewScenePath);
        }

        [MenuItem("Bee Kingdom/Playground/Use Wave6 50x50 Exact-Crop Terrain Test On Play")]
        public static void UseWave6ExactCropTerrainTestOnPlay()
        {
            SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
            ConfigurePlayModeStartScene(Wave6ExactCropTerrainTestScenePath);
        }

        public static void ConfigurePlayModeStartScene()
        {
            string activeScenePath = SceneManager.GetActiveScene().path;
            if (IsEnvironment2D5DScene(activeScenePath))
            {
                ClearPlayModeStartScene();
                return;
            }

            string currentStartScenePath = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
            bool activeSceneIsLivingHive = activeScenePath == LivingHiveScenePath;
            bool currentStartSceneIsLivingHive = currentStartScenePath == LivingHiveScenePath;
            if (activeSceneIsLivingHive || currentStartSceneIsLivingHive)
            {
                SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
                ConfigurePlayModeStartScene(LivingHiveScenePath);
                return;
            }

            bool activeSceneIsExactCrop = IsExactCropWave6Scene(activeScenePath);
            bool currentStartSceneIsExactCrop = IsExactCropWave6Scene(currentStartScenePath);
            if (activeSceneIsExactCrop || currentStartSceneIsExactCrop)
            {
                SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
                ConfigurePlayModeStartScene(activeSceneIsExactCrop ? activeScenePath : currentStartScenePath);
                return;
            }

            bool activeSceneIsRouteLock8192 = activeScenePath == Wave6RouteLock8192ScaleBridgeProofScenePath;
            bool currentStartSceneIsRouteLock8192 = currentStartScenePath == Wave6RouteLock8192ScaleBridgeProofScenePath;
            if (activeSceneIsRouteLock8192 || currentStartSceneIsRouteLock8192)
            {
                SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
                ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
                return;
            }

            if (IsLegacyWave6Scene(activeScenePath) || IsLegacyWave6Scene(currentStartScenePath))
            {
                SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
                Debug.LogWarning("Legacy Wave6 audit scenes are deprecated for final map validation. Play Mode now redirects to the route-lock 8192 scale-bridge proof scene.");
                ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
                return;
            }

            bool activeSceneIsSupportCenter = activeScenePath == Wave6SupportCenterNativeAuditPreviewScenePath;
            bool currentStartSceneIsSupportCenter = currentStartScenePath == Wave6SupportCenterNativeAuditPreviewScenePath;
            if (activeSceneIsSupportCenter || currentStartSceneIsSupportCenter)
            {
                SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
                ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
                return;
            }

            bool activeSceneIsWave6V2IRepair = activeScenePath == Wave6V2IRepairAuditPreviewScenePath;
            bool currentStartSceneIsWave6V2IRepair = currentStartScenePath == Wave6V2IRepairAuditPreviewScenePath;
            if (activeSceneIsWave6V2IRepair || currentStartSceneIsWave6V2IRepair)
            {
                SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
                Debug.LogWarning("Wave6 V2I Repair Audit is open for sharp existing-candidate review only. It is not a final Unity handoff scene.");
                ConfigurePlayModeStartScene(Wave6SharpExistingCandidateReviewScenePath);
                return;
            }

            bool activeSceneIsWave6V3O = activeScenePath == Wave6V3OReducedAuditPreviewScenePath;
            bool currentStartSceneIsWave6V3O = currentStartScenePath == Wave6V3OReducedAuditPreviewScenePath;
            if (activeSceneIsWave6V3O || currentStartSceneIsWave6V3O)
            {
                SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
                Debug.LogWarning("Wave6 V3O Reduced Audit is deprecated for final map validation. Play Mode now redirects to the route-lock 8192 scale-bridge proof scene.");
                ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
                return;
            }

            bool activeSceneIsWave6V2O = activeScenePath == Wave6V2OPerimeterAuditPreviewScenePath;
            bool currentStartSceneIsWave6V2O = currentStartScenePath == Wave6V2OPerimeterAuditPreviewScenePath;
            if (activeSceneIsWave6V2O || currentStartSceneIsWave6V2O)
            {
                SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
                Debug.LogWarning("Wave6 V2O Perimeter Audit is deprecated for final map validation. Play Mode now redirects to the route-lock 8192 scale-bridge proof scene.");
                ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
                return;
            }

            bool activeSceneIsWave6V2I = activeScenePath == Wave6V2INativeAuditPreviewScenePath;
            bool currentStartSceneIsWave6V2I = currentStartScenePath == Wave6V2INativeAuditPreviewScenePath;
            if (activeSceneIsWave6V2I || currentStartSceneIsWave6V2I)
            {
                SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
                Debug.LogWarning("Wave6 V2I Native Audit is deprecated for final map validation. Play Mode now redirects to the route-lock 8192 scale-bridge proof scene.");
                ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
                return;
            }

            bool activeSceneIsWave6V3E = activeScenePath == Wave6V3ECandidateScenePath;
            bool currentStartSceneIsWave6V3E = currentStartScenePath == Wave6V3ECandidateScenePath;
            if (activeSceneIsWave6V3E || currentStartSceneIsWave6V3E)
            {
                SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode();
                Debug.LogWarning("Wave6 V3E Candidate is deprecated for final map validation. Play Mode now redirects to the route-lock 8192 scale-bridge proof scene.");
                ConfigurePlayModeStartScene(Wave6RouteLock8192ScaleBridgeProofScenePath);
                return;
            }

            bool activeSceneIsWave5 = activeScenePath == Wave5Premium25x25ScenePath;
            bool currentStartSceneIsWave5 = currentStartScenePath == Wave5Premium25x25ScenePath;
            bool wave5Mode = SplashDevelopmentSceneConfig.IsWave5PremiumMapModeEnabled();
            ConfigurePlayModeStartScene(activeSceneIsWave5 || currentStartSceneIsWave5 || wave5Mode ? Wave5Premium25x25ScenePath : MainDemoScenePath);
        }

        private static void OnActiveSceneChangedInEditMode(Scene previousScene, Scene nextScene)
        {
            if (IsEnvironment2D5DScene(nextScene.path))
            {
                ClearPlayModeStartScene();
            }
        }

        private static bool IsEnvironment2D5DScene(string scenePath)
        {
            return !string.IsNullOrEmpty(scenePath)
                && scenePath.StartsWith(Environment2D5DScenesFolder, System.StringComparison.Ordinal);
        }

        private static void ClearPlayModeStartScene()
        {
            if (EditorSceneManager.playModeStartScene != null)
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }

        private static bool IsLegacyWave6Scene(string scenePath)
        {
            return !string.IsNullOrEmpty(scenePath)
                && scenePath.StartsWith("Assets/Scenes/WorldMapWave6", System.StringComparison.Ordinal)
                && scenePath != Wave6RouteLock8192ScaleBridgeProofScenePath
                && scenePath != Wave6SharpExistingCandidateReviewScenePath
                && !IsExactCropWave6Scene(scenePath);
        }

        private static bool IsExactCropWave6Scene(string scenePath)
        {
            return scenePath == Wave6ExactCropMmoPreviewScenePath
                || scenePath == Wave6ExactCropTerrainTestScenePath;
        }

        private static void ConfigurePlayModeStartScene(string scenePath)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null && EditorSceneManager.playModeStartScene != sceneAsset)
            {
                EditorSceneManager.playModeStartScene = sceneAsset;
            }

            EnsureSceneEnabled(scenePath);
            EnsureSceneEnabled(MainDemoScenePath);
        }

        private static void EnsureSceneEnabled(string scenePath)
        {
            if (EditorBuildSettings.scenes.Any(scene => scene.path == scenePath && scene.enabled)) return;

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            }.Concat(EditorBuildSettings.scenes).ToArray();
        }
    }
}
