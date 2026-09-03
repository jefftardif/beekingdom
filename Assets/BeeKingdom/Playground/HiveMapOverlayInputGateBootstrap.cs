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

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying) return;
            if (!IsEnvironmentScene(scene)) return;
            if (FindFirstObjectByType<HiveMapOverlayInputGateBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapOverlayInputGateBootstrap>();
        }

        // M043V-CL: shared by any bootstrap that draws or reacts to input outside the
        // overlay's own draw call (e.g. HiveMapProductionBootstrap's honey/warehouse/
        // transformation ready-glow + bee-swirl badges, found bleeding through the
        // Communication/Chat Royal overlay because it only checked Research/Activities/
        // RoyalPalace/Army, never Alliance/Communication/Barrack/Construction/Settings).
        // Single source of truth so a new overlay only needs to be added once here.
        public static bool IsAnyOverlayBlocking()
        {
            return HiveViewProductUiPresenter.AllianceOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.CommunicationOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.BarrackOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.ConstructionOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.SettingsOverlayOpenForExternalHost
                || LivingHiveResearchRuntime.IsModalOpen || HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost
                || HiveMapActivitiesBootstrap.ModalOpenForExternalHost
                || HiveMapRoyalPalaceBootstrap.ModalOpenForExternalHost
                // M006-CL wave 1: new HiveMap-native windows, not part of the monolith's
                // own overlay bookkeeping, so they need their own flags here.
                || HiveMapNurseryBootstrap.OverlayOpenForExternalHost
                || HiveMapProductionInfoBootstrap.OverlayOpenForExternalHost
                // M009-CX wave 3: Champion Hall uses its own IMGUI catalog/status window.
                || HiveMapChampionHallBootstrap.OverlayOpenForExternalHost
                // M008-CX wave 2: same IMGUI input protection for the Genetics/Infirmary
                // capability/status window; M009 extends it to Academy/Defense status.
                // M013 extends that status window to Bank.
                || HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost
                || HiveMapArmyBootstrap.ModalOpenForExternalHost;
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;

            bool blocked = IsAnyOverlayBlocking();

            // M043Z-CL: the non-modal mini-chat draws via IMGUI, not uGUI, so
            // BuildingInteractionController's own EventSystem.IsPointerOverGameObject() guard
            // never sees it - a click on its "Ouvrir" button (or anywhere else on its panel)
            // fell through to whatever 3D building happened to sit behind it on screen (Jeff,
            // 2026-09-03: clicking "Ouvrir" over the Royal Palace opened the Royal Palace
            // instead). `blocked` above must stay false while only the mini-chat is open (it
            // must not hide the rest of the HUD - see CommunicationOverlayOpenForExternalHost's
            // comment), so this is a separate, pointer-position-scoped check: building clicks
            // are only suppressed while the mouse is actually over the mini-chat's own rect,
            // not the whole screen.
            bool blockedByMiniChatPointer = false;
            if (!blocked && HiveViewProductUiPresenter.MiniChatOnlyOpenForExternalHost)
            {
                Vector2 guiPoint = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                blockedByMiniChatPointer = HiveViewProductUiPresenter.MiniChatFloatingRectForExternalHost.Contains(guiPoint);
            }

            if (controller == null) controller = FindFirstObjectByType<BuildingInteractionController>();
            if (controller != null) controller.IsEnabled = !blocked && !blockedByMiniChatPointer;

            if (LivingHiveMenuRuntime.CanvasComponent != null) LivingHiveMenuRuntime.CanvasComponent.SetInputBlocked(blocked);
        }
    }
}
