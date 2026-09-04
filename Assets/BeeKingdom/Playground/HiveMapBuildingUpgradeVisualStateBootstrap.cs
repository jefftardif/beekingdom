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
        private const float PulseAlphaMin = 0.88f;
        private const float PulseAlphaMax = 1f;
        private const float PulseOutlineWidthMin = 4.5f;
        private const float PulseOutlineWidthMax = 8f;
        private const float PulseIntensityMin = 1f;
        private const float PulseIntensityMax = 1.48f;
        private const float PulsePeriodSeconds = 1.35f;

        private static readonly Color UpgradingTintColor = new Color(0.35f, 0.75f, 1f, 1f);

        private BuildingInteractionController controller;
        private BuildingSelectionHighlight highlight;
        private string highlightedHotspotId;
        private bool preemptionHookInstalled;

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

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying) return;
            if (!IsEnvironmentScene(scene)) return;
            if (FindFirstObjectByType<HiveMapBuildingUpgradeVisualStateBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapBuildingUpgradeVisualStateBootstrap>();
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (controller == null)
            {
                controller = FindFirstObjectByType<BuildingInteractionController>();
                if (controller == null) return;
            }
            // M045F-CL: was a Selection.BuildingClicked subscriber (M045E) - fired alongside
            // whichever building-specific handler (e.g. HiveMapBarrackBootstrap) was ALSO
            // subscribed to the same click, so completing the upgrade AND opening the building's
            // own window both happened on the same click (that event has no consumption
            // mechanism, and subscription order across independently-lazy bootstraps is not
            // controllable). Installed once as the controller's single preemption hook instead -
            // runs before any Selection.BuildingClicked subscriber even fires, guaranteeing
            // exactly one action per click regardless of registration order.
            if (!preemptionHookInstalled)
            {
                BuildingInteractionController.InteractionPreemptionHook = TryCompleteReadyUpgradeOnClick;
                preemptionHookInstalled = true;
            }

            string activeHotspotId = HiveViewProductUiPresenter.ActiveOfficialUpgradeHotspotIdForExternalHost();
            if (string.Equals(activeHotspotId, highlightedHotspotId, StringComparison.Ordinal))
            {
                ApplyUpgradePulse(Time.unscaledTime);
                return;
            }

            if (highlight != null) highlight.Hide();
            highlightedHotspotId = activeHotspotId;
            if (string.IsNullOrEmpty(activeHotspotId)) return;

            if (!BuildingCatalog.TryGetByLegacyKey(activeHotspotId, out BuildingDefinition definition)) return;
            GameObject target = controller.Registry.GetGameObjectByBuildingType(definition.BuildingType);
            if (target == null) return;

            if (highlight == null) highlight = target.AddComponent<BuildingSelectionHighlight>();
            highlight.TintColor = UpgradingTintColor;
            highlight.OutlineIntensity = PulseIntensityMin;
            highlight.OutlineWidthTexels = PulseOutlineWidthMin;
            highlight.Show(definition, target);
            ApplyUpgradePulse(Time.unscaledTime);
        }

        private void OnDestroy()
        {
            if (preemptionHookInstalled && BuildingInteractionController.InteractionPreemptionHook == (Func<BuildingDefinition, bool>)TryCompleteReadyUpgradeOnClick)
                BuildingInteractionController.InteractionPreemptionHook = null;
        }

        // M045F-CL: restores the "tap the building directly to validate" UX the legacy
        // reference-hotspot renderer had (TryCompleteReadyBuildingUpgradeOnTap /
        // DrawBuildingUpgradeReadyMarkers), never ported to HiveMap - same real
        // server-authoritative Complete() call, not a shortcut. Installed as
        // BuildingInteractionController's single preemption hook (see the Update() comment
        // above) so it always runs first, ahead of any building-specific "open my window"
        // handler - ANY click on a completion-ready building validates it, not just a click on
        // the small ready badge, and the click never also opens that building's window. Only
        // the building whose OWN operation is ready intercepts - every other building's click
        // falls through (returns false) and behaves exactly as before. Consumes the click
        // (returns true) even if the real server completion call itself fails, per the required
        // failure semantics: a failed validation must never silently fall through to opening the
        // building window - RunOfficialBuildingUpgradeAction already logs/surfaces the error and
        // leaves the operation in AwaitingCompletion so the next click retries the same real path.
        private static bool TryCompleteReadyUpgradeOnClick(BuildingDefinition building)
        {
            if (building == null) return false;
            string hotspotId = BuildingMappingTable.GetByBuildingType(building.BuildingType).LegacyKey;
            string readyHotspotId = HiveViewProductUiPresenter.ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost();
            if (string.IsNullOrEmpty(readyHotspotId) || !string.Equals(hotspotId, readyHotspotId, StringComparison.Ordinal)) return false;
            HiveViewProductUiPresenter.TryCompleteReadyBuildingUpgradeOnTapForExternalHost(hotspotId);
            return true;
        }

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost || controller == null) return;
            if (HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()) return;
            string readyHotspotId = HiveViewProductUiPresenter.ReadyToCompleteOfficialUpgradeHotspotIdForExternalHost();
            if (string.IsNullOrEmpty(readyHotspotId)) return;
            if (!BuildingCatalog.TryGetByLegacyKey(readyHotspotId, out BuildingDefinition definition)) return;
            GameObject target = controller.Registry.GetGameObjectByBuildingType(definition.BuildingType);
            if (target == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            Rect rect = ScreenRectFor(target, camera);
            if (rect.width <= 0f) return;
            float pixelsPerWorldUnit = camera.orthographic && camera.orthographicSize > 0.001f
                ? Screen.height / (2f * camera.orthographicSize)
                : 11.76f;
            float glowSize = Mathf.Clamp(10.8f * pixelsPerWorldUnit, 40f, 220f);
            HiveViewProductUiPresenter.DrawBuildingUpgradeReadyBadgeForExternalHost(rect, Time.unscaledTime, glowSize);
        }

        // Same collider-bounds screen projection as HiveMapProductionBootstrap.ScreenRectFor.
        private static Rect ScreenRectFor(GameObject go, Camera camera)
        {
            Collider collider = go.GetComponent<Collider>();
            Bounds bounds = collider != null ? collider.bounds : default;
            if (collider == null)
            {
                Renderer renderer = go.GetComponentInChildren<Renderer>();
                if (renderer == null) return default;
                bounds = renderer.bounds;
            }

            Vector3 centerScreen = camera.WorldToScreenPoint(bounds.center);
            if (centerScreen.z <= 0f) return default;

            Vector3 topScreen = camera.WorldToScreenPoint(bounds.center + new Vector3(0f, bounds.extents.y, 0f));
            Vector3 rightScreen = camera.WorldToScreenPoint(bounds.center + new Vector3(bounds.extents.x, 0f, 0f));
            float halfHeight = Mathf.Abs(topScreen.y - centerScreen.y);
            float halfWidth = Mathf.Abs(rightScreen.x - centerScreen.x);
            if (halfWidth <= 0f || halfHeight <= 0f) return default;

            float guiY = Screen.height - centerScreen.y;
            return new Rect(centerScreen.x - halfWidth, guiY - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }

        public static float UpgradeOutlinePulseAlpha(float time)
        {
            return Mathf.Lerp(PulseAlphaMin, PulseAlphaMax, UpgradeOutlinePulse01(time));
        }

        public static float UpgradeOutlinePulseWidth(float time)
        {
            return Mathf.Lerp(PulseOutlineWidthMin, PulseOutlineWidthMax, UpgradeOutlinePulse01(time));
        }

        public static float UpgradeOutlinePulseIntensity(float time)
        {
            return Mathf.Lerp(PulseIntensityMin, PulseIntensityMax, UpgradeOutlinePulse01(time));
        }

        public static Color UpgradeOutlinePulseColorForProof(float time)
        {
            Color color = UpgradingTintColor;
            color.a = UpgradeOutlinePulseAlpha(time);
            return color;
        }

        private static float UpgradeOutlinePulse01(float time)
        {
            float phase = Mathf.Sin(time / PulsePeriodSeconds * Mathf.PI * 2f) * 0.5f + 0.5f;
            float shaped = Mathf.SmoothStep(0f, 1f, phase);
            return shaped * shaped * (3f - 2f * shaped);
        }

        private void ApplyUpgradePulse(float time)
        {
            if (highlight == null) return;
            highlight.SetVisualState(
                UpgradeOutlinePulseAlpha(time),
                UpgradeOutlinePulseWidth(time),
                UpgradeOutlinePulseIntensity(time));
        }
    }
}
