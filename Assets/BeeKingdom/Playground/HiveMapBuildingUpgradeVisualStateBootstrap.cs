using System;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Shows an "upgrading in progress" outline on whichever building currently has a
    // running server-authoritative upgrade operation (HiveViewProductUiPresenter's
    // ActiveOfficialUpgradeHotspotIdForExternalHost) - reuses the same silhouette-outline
    // technique as building selection (BuildingSelectionHighlight, see its new TintColor
    // property) rather than inventing a separate visual system, but as its own overlay
    // instance parented directly to the building itself so it stays lit independently of
    // whatever the player currently has selected.
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps.
    public sealed class HiveMapBuildingUpgradeVisualStateBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Building Upgrade Visual State Runtime";
        private static readonly Color UpgradingTintColor = new Color(0.35f, 0.75f, 1f, 1f);

        private BuildingInteractionController controller;
        private BuildingSelectionHighlight highlight;
        private string highlightedHotspotId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapBuildingUpgradeVisualStateBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapBuildingUpgradeVisualStateBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (controller == null)
            {
                controller = FindFirstObjectByType<BuildingInteractionController>();
                if (controller == null) return;
            }

            string activeHotspotId = HiveViewProductUiPresenter.ActiveOfficialUpgradeHotspotIdForExternalHost();
            if (string.Equals(activeHotspotId, highlightedHotspotId, StringComparison.Ordinal)) return;

            if (highlight != null) highlight.Hide();
            highlightedHotspotId = activeHotspotId;
            if (string.IsNullOrEmpty(activeHotspotId)) return;

            if (!BuildingCatalog.TryGetByLegacyKey(activeHotspotId, out BuildingDefinition definition)) return;
            GameObject target = controller.Registry.GetGameObjectByBuildingType(definition.BuildingType);
            if (target == null) return;

            if (highlight == null) highlight = target.AddComponent<BuildingSelectionHighlight>();
            highlight.TintColor = UpgradingTintColor;
            highlight.Show(definition, target);
        }
    }
}
