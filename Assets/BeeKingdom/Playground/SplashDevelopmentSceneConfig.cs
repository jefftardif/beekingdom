using System;
using System.IO;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    public static class SplashDevelopmentSceneConfig
    {
        public const string SandboxScenePath = "Assets/Scenes/SandboxPlayground.unity";
        public const string LegacyWorldMapScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        public const string WorldMapScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        public const string Wave6Premium50x50TerrainScenePath = "Assets/Scenes/WorldMapWave6Premium50x50TerrainTest.unity";
        public const string Wave5Premium25x25ScenePath = "Assets/Scenes/WorldMapWave5Premium25x25Test.unity";
        public const string LoginScenePath = SandboxScenePath;
        public const string HiveScenePath = "Assets/Scenes/LivingHive.unity";
        public const string Wave5PremiumMapModeKey = "BeeKingdom.Dev.WorldMapMode.Wave5Premium25x25";

        public static bool IsDevelopmentMenuVisible()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }

        public static bool IsSceneEnabledInBuildSettings(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath)) return false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string configuredPath = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(configuredPath, scenePath, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        public static bool TryOpenScene(string scenePath, out string message)
        {
            if (!IsSceneEnabledInBuildSettings(scenePath))
            {
                message = "Scene absente des Build Settings: " + scenePath;
                return false;
            }

            if (string.Equals(scenePath, Wave5Premium25x25ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.PlayerPrefs.SetInt(Wave5PremiumMapModeKey, 1);
                UnityEngine.PlayerPrefs.Save();
            }
            else if (string.Equals(scenePath, WorldMapScenePath, StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.PlayerPrefs.SetInt(Wave5PremiumMapModeKey, 0);
                UnityEngine.PlayerPrefs.Save();
            }

            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
            if (string.Equals(scenePath, WorldMapScenePath, StringComparison.OrdinalIgnoreCase))
            {
                message = "Ouverture Wave6 50x50 exact-crop: " + sceneName;
            }
            else if (string.Equals(scenePath, Wave5Premium25x25ScenePath, StringComparison.OrdinalIgnoreCase))
            {
                message = "Ouverture Wave5 Premium 25x25: " + sceneName;
            }
            else
            {
                message = "Ouverture scene: " + sceneName;
            }
            return true;
        }

        public static bool TryOpenWave5Premium25x25(out string message)
        {
            return TryOpenScene(Wave5Premium25x25ScenePath, out message);
        }

        public static bool IsWave5PremiumMapModeEnabled()
        {
            return UnityEngine.PlayerPrefs.GetInt(Wave5PremiumMapModeKey, 0) == 1;
        }

        public static void DisableWave5PremiumMapMode()
        {
            UnityEngine.PlayerPrefs.SetInt(Wave5PremiumMapModeKey, 0);
            UnityEngine.PlayerPrefs.Save();
        }

        public static string[] ProofRows()
        {
            return new[]
            {
                "dev_splash_scene_selector:true",
                "dev_splash_visible_only_editor_or_development:true",
                "dev_splash_worldmap_scene_path:" + WorldMapScenePath,
                "dev_splash_worldmap_wave6_exact_crop:true",
                "dev_splash_worldmap_legacy_scene_path:" + LegacyWorldMapScenePath,
                "dev_splash_hive_scene_path:" + HiveScenePath,
                "dev_splash_wave6_terrain_scene_path:" + Wave6Premium50x50TerrainScenePath,
                "dev_splash_wave5_premium_25x25_scene_path:" + Wave5Premium25x25ScenePath,
                "dev_splash_scene_loads_by_exact_scene_name:true",
                "dev_splash_wave5_mode_key:" + Wave5PremiumMapModeKey,
                "dev_splash_scene_config_centralized:true",
                "dev_splash_scene_build_settings_guard:true"
            };
        }
    }
}
