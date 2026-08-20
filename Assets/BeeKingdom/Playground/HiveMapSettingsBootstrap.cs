using System;
using BeeKingdom.Core.Integration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Wires HiveViewProductUiPresenter's real Settings panel (reduced motion, economy mode,
    // sound, music, language - PlayerPrefs-backed, not a placeholder) into
    // BeeKingdom.Core.Integration.LivingHiveSettingsBridge, so BeeKingdom.LivingHiveMenu's
    // "Parametres" row (Assets/Experiments/Environment2D5D/LivingHiveMenu) can open the real
    // window without needing a direct reference to the monolith assembly (see
    // LivingHiveChatBridge.cs for why that reference isn't possible). Replaces the previous
    // local uGUI placeholder (4 toggles wired to nothing).
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps: a
    // RuntimeInitializeOnLoadMethod creates this only when the active scene starts with
    // "Environment2D5D", no scene wiring required.
    public sealed class HiveMapSettingsBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Settings Runtime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapSettingsBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapSettingsBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        private void Start()
        {
            LivingHiveSettingsBridge.SetHandlers(
                () => HiveViewProductUiPresenter.SettingsOverlayOpenForExternalHost,
                HiveViewProductUiPresenter.ToggleSettingsOverlayForExternalHost);
        }

        private void OnGUI()
        {
            HiveViewProductUiPresenter.DrawSettingsOverlayForExternalHost(Screen.width < 900);
        }
    }
}
