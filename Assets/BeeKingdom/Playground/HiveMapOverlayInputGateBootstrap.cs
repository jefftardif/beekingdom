using System;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.LivingHiveMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Blocks clicks from reaching whatever's underneath (3D buildings, the uGUI rail/
    // header) while any full-screen/floating IMGUI overlay reused from LivingHive is open
    // (Alliance, Communication/mini-chat/Mail, Barrack, Construction, Settings). IMGUI
    // (OnGUI) draws are purely visual - Unity never lets them intercept uGUI's EventSystem
    // raycasts or BuildingInteractionController's own 3D physics raycast, both of which run
    // independently every frame regardless of what OnGUI draws on top. Without this, a
    // click meant for an overlay's own close button (top-left) can simultaneously open
    // whatever building or rail button happens to sit at that same screen position - e.g.
    // closing the Barrack panel also opening the Queen (ROYAL_PALACE) detail, or the
    // "Plus" rail panel, neither anywhere near the actual click.
    //
    // The reference monolith has its own equivalent (PremiumUiBlocksWorldInput(), used by
    // its own Draw() wrapper) - HiveMap's bootstraps call individual Draw*ForExternalHost
    // methods directly instead of that wrapper, so that protection never applied here.
    //
    // Research is a uGUI fullscreen modal, so its own card clicks are already protected by
    // EventSystem raycasts. It is still listed here because HiveMap also has independent
    // IMGUI/world bootstraps and a bottom/header canvas that must be suppressed while
    // Research owns the screen.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapOverlayInputGateBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Overlay Input Gate Runtime";

        private BuildingInteractionController controller;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapOverlayInputGateBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapOverlayInputGateBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;

            bool blocked = HiveViewProductUiPresenter.AllianceOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.CommunicationOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.BarrackOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.ConstructionOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.SettingsOverlayOpenForExternalHost
                || LivingHiveResearchRuntime.IsModalOpen
                // M006-CL wave 1: new HiveMap-native windows, not part of the monolith's
                // own overlay bookkeeping, so they need their own flags here.
                || HiveMapNurseryBootstrap.OverlayOpenForExternalHost
                || HiveMapProductionInfoBootstrap.OverlayOpenForExternalHost
                // M009-CX wave 3: Champion Hall uses its own IMGUI catalog/status window.
                || HiveMapChampionHallBootstrap.OverlayOpenForExternalHost
                // M008-CX wave 2: same IMGUI input protection for the Genetics/Infirmary
                // capability/status window; M009 extends it to Academy/Defense status.
                || HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost;

            if (controller == null) controller = FindFirstObjectByType<BuildingInteractionController>();
            if (controller != null) controller.IsEnabled = !blocked;

            if (LivingHiveMenuRuntime.CanvasComponent != null) LivingHiveMenuRuntime.CanvasComponent.SetInputBlocked(blocked);
        }
    }
}
