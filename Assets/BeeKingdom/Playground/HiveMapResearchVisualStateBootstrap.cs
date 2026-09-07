using System;
using BeeKingdom.Buildings.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // M049-CL: emerald "researching in progress" outline on the single real Research building,
    // driven purely by the server-authoritative Research operation's own Running state
    // (HiveViewProductUiPresenter.IsOfficialResearchRunningForExternalHost - never
    // AwaitingCompletion, never "the Research window happens to be open"). Same technique and
    // same shared BuildingActivityPulse curve as Construction's existing blue pulse
    // (HiveMapBuildingUpgradeVisualStateBootstrap) - deliberately its own small bootstrap rather
    // than folded into that one, since Construction's file also owns click-preemption/
    // ready-badge concerns this mission must not touch (M045F).
    //
    // Same auto-bootstrap strategy as the other Environment2D5D runtime bootstraps.
    public sealed class HiveMapResearchVisualStateBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Research Visual State Runtime";

        private BuildingInteractionController controller;
        private BuildingSelectionHighlight highlight;
        private bool wasRunning;
        private bool preemptionHookInstalled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapResearchVisualStateBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapResearchVisualStateBootstrap>();
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
            if (FindFirstObjectByType<HiveMapResearchVisualStateBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapResearchVisualStateBootstrap>();
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (controller == null)
            {
                controller = FindFirstObjectByType<BuildingInteractionController>();
                if (controller == null) return;
            }

            // M049B-CL: generalizes the same M045F single-hook mechanism Construction already
            // installs (BuildingInteractionController.InteractionPreemptionHook) instead of
            // creating a second, competing click router - Research registers its own small
            // "is the Research building ready, and if so complete it" handler alongside
            // Construction's. Whichever building was actually clicked is the only one either
            // handler ever matches, so registration order between them is irrelevant.
            if (!preemptionHookInstalled)
            {
                BuildingInteractionController.RegisterCompletionPreemption(TryCompleteReadyResearchOnClick);
                preemptionHookInstalled = true;
            }

            bool running = HiveViewProductUiPresenter.IsOfficialResearchRunningForExternalHost();
            if (running == wasRunning)
            {
                if (running) BuildingActivityPulse.Research.Apply(highlight, Time.unscaledTime);
                return;
            }

            wasRunning = running;
            if (!running)
            {
                if (highlight != null) highlight.Hide();
                return;
            }

            if (!BuildingCatalog.TryGetByBuildingType(BuildingTypes.Research, out BuildingDefinition definition)) return;
            GameObject target = controller.Registry.GetGameObjectByBuildingType(BuildingTypes.Research);
            if (target == null) return;

            if (highlight == null) highlight = target.AddComponent<BuildingSelectionHighlight>();
            BuildingActivityPulse.Research.Configure(highlight);
            highlight.Show(definition, target);
            BuildingActivityPulse.Research.Apply(highlight, Time.unscaledTime);
        }

        private void OnDestroy()
        {
            if (preemptionHookInstalled)
                BuildingInteractionController.UnregisterCompletionPreemption(TryCompleteReadyResearchOnClick);
        }

        // M049B-CL: mirrors HiveMapBuildingUpgradeVisualStateBootstrap.TryCompleteReadyUpgradeOnClick
        // exactly - real server-authoritative completion
        // (HiveViewProductUiPresenter.TryCompleteReadyResearchOnTapForExternalHost ->
        // RunOfficialResearchAction -> researchController.Complete(), the same path the Research
        // screen's own "Terminer" button calls), never a local unlock/shortcut. Only the single
        // Research building intercepts (every other building's click falls through unchanged).
        // Consumes the click (returns true) even if the real completion call fails - the failure
        // is surfaced/logged by RunOfficialResearchAction itself, the operation stays
        // AwaitingCompletion, and the next click retries the same real path.
        private static bool TryCompleteReadyResearchOnClick(BuildingDefinition building)
        {
            if (building == null || !string.Equals(building.BuildingType, BuildingTypes.Research, StringComparison.Ordinal)) return false;
            if (string.IsNullOrEmpty(HiveViewProductUiPresenter.ReadyToCompleteOfficialResearchForExternalHost())) return false;
            HiveViewProductUiPresenter.TryCompleteReadyResearchOnTapForExternalHost();
            return true;
        }

        private void OnGUI()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost || controller == null) return;
            if (HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()) return;
            if (string.IsNullOrEmpty(HiveViewProductUiPresenter.ReadyToCompleteOfficialResearchForExternalHost())) return;
            GameObject target = controller.Registry.GetGameObjectByBuildingType(BuildingTypes.Research);
            if (target == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            Rect rect = ScreenRectFor(target, camera);
            if (rect.width <= 0f) return;
            float pixelsPerWorldUnit = camera.orthographic && camera.orthographicSize > 0.001f
                ? Screen.height / (2f * camera.orthographicSize)
                : 11.76f;
            float glowSize = Mathf.Clamp(10.8f * pixelsPerWorldUnit, 40f, 220f);
            HiveViewProductUiPresenter.DrawResearchReadyBadgeForExternalHost(rect, Time.unscaledTime, glowSize);
        }

        // Same collider-bounds screen projection as HiveMapBuildingUpgradeVisualStateBootstrap's
        // own copy (each bootstrap owns this small utility independently - not part of the
        // reused completion architecture, which is the badge renderer + click router).
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
    }
}
