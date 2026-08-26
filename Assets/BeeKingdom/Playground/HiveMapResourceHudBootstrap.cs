using System;
using BeeKingdom.LivingHiveMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Pushes real resource totals (Miel/Cire/Pollen/Abeilles/Capacité) from
    // HiveViewProductUiPresenter into LivingHiveMenuHeaderData, which HiveMap's own top
    // uGUI header (LivingHiveMenuCanvas) reads from. Before this, LivingHiveMenuHeaderData
    // only held hardcoded preview constants (125800, ...) - the header never moved, even
    // after collecting from Honey Reserve/Warehouse/Transformation via
    // HiveMapProductionBootstrap.
    //
    // Default assembly -> BeeKingdom.LivingHiveMenu package is the allowed reference
    // direction (see HiveMapSplashBootstrap/HiveMapAllianceBootstrap for the same pattern);
    // no cross-assembly bridge needed here, unlike the package -> default-assembly case
    // (LivingHiveChatBridge).
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapResourceHudBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Resource Hud Runtime";
        private const float PushIntervalSeconds = 1f;

        private float lastPushAt = -100f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapResourceHudBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapResourceHudBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying) return;
            if (!IsEnvironmentScene(scene)) return;
            if (FindFirstObjectByType<HiveMapResourceHudBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapResourceHudBootstrap>();
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (Time.unscaledTime - lastPushAt < PushIntervalSeconds) return;
            lastPushAt = Time.unscaledTime;

            HiveViewProductUiPresenter.GetResourceTotalsForExternalHost(
                out float honey, out float wax, out float pollen,
                out float bees, out float capacityUsed, out float capacityMax, out float royalJelly);

            LivingHiveMenuHeaderData.SetLiveValues(
                Mathf.RoundToInt(honey), Mathf.RoundToInt(wax), Mathf.RoundToInt(pollen),
                Mathf.RoundToInt(bees), Mathf.RoundToInt(capacityUsed), Mathf.RoundToInt(capacityMax),
                Mathf.RoundToInt(royalJelly));
        }
    }
}
