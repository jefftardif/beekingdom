using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.LivingHiveMenu;

namespace BeeKingdom.Playground
{
    public static class HiveMapRuntimeBootstrapInitializer
    {
        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedCallback()
        {
            if (subscribed) return;
            subscribed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsEnvironmentScene(scene)) return;
            InitializeAllBootstraps(scene);
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            if (scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal)) return true;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != null && root.name != null && root.name.StartsWith("Environment2D5D", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void InitializeAllBootstraps(Scene scene)
        {
            // Building controller must exist before Research host attaches to its selection service
            BuildingRuntimeViewBootstrap.AutoStartForScene(scene);
            LivingHiveMenuRuntime.EnsureRuntime(scene);
            LivingHiveResearchRuntime.EnsureRuntime(scene);
            HiveMapSplashBootstrap.InitializeForScene(scene);
            HiveMapOverlayInputGateBootstrap.InitializeForScene(scene);
            HiveMapActivitiesBootstrap.InitializeForScene(scene);
            HiveMapAllianceBootstrap.InitializeForScene(scene);
            HiveMapBarrackBootstrap.InitializeForScene(scene);
            HiveMapBuildingUpgradeClickBootstrap.InitializeForScene(scene);
            HiveMapBuildingUpgradeVisualStateBootstrap.InitializeForScene(scene);
            HiveMapChampionHallBootstrap.InitializeForScene(scene);
            HiveMapConstructionBootstrap.InitializeForScene(scene);
            HiveMapNurseryBootstrap.InitializeForScene(scene);
            HiveMapProductionBootstrap.InitializeForScene(scene);
            HiveMapProductionInfoBootstrap.InitializeForScene(scene);
            HiveMapQueueSidebarBootstrap.InitializeForScene(scene);
            HiveMapResourceHudBootstrap.InitializeForScene(scene);
            HiveMapRoyalPalaceBootstrap.InitializeForScene(scene);
            HiveMapSettingsBootstrap.InitializeForScene(scene);
            HiveMapUnsupportedBuildingBootstrap.InitializeForScene(scene);
            HiveMapArmyBootstrap.InitializeForScene(scene);
            HiveMapAmbientBeesBootstrap.InitializeForScene(scene);
            LivingHiveChatBridgeBootstrap.InitializeForScene(scene);
        }
    }
}