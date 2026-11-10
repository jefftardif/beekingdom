using System;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.LivingHiveMenu;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeeKingdom.Playground
{
    // Second input path for the premium information button and producer
    // artwork. Unity's IMGUI Button sometimes never receives MouseUp under
    // a full-screen uGUI Canvas. Normal BuildingClicked handling is preserved.
    public sealed partial class HiveMapProductionInfoBootstrap
    {
        public static Rect ProducerInfoBadgeRectForProof(
            Rect buildingBounds, float screenWidth, float screenHeight, float headerBottom)
        {
            const float size = 44f;
            return new Rect(
                Mathf.Clamp(buildingBounds.xMax - size - 3f, 4f,
                    Mathf.Max(4f, screenWidth - size - 4f)),
                Mathf.Clamp(Mathf.Max(buildingBounds.yMin + 8f, headerBottom + 8f),
                    4f, Mathf.Max(4f, screenHeight - size - 4f)),
                size, size);
        }

        public static Vector2 ProducerInfoGuiPointerForProof(
            Vector2 screenPointer, float screenHeight)
        {
            return new Vector2(screenPointer.x, screenHeight - screenPointer.y);
        }

        private static float HeaderBottom()
        {
            bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
            Rect rect = portrait
                ? LivingHiveMenuHeaderData.PortraitHeaderRect(Screen.width, Screen.height)
                : LivingHiveMenuHeaderData.LandscapeHeaderRect(Screen.width, Screen.height);
            return rect.yMax;
        }

        public static bool ProducerInfoBadgeOwnsCurrentPointer()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost ||
                (!Input.GetMouseButtonDown(0) && Input.touchCount == 0))
                return false;
            HiveMapProductionInfoBootstrap info =
                FindFirstObjectByType<HiveMapProductionInfoBootstrap>();
            if (info == null || info.subscribedController == null ||
                OverlayOpenForExternalHost) return false;
            Camera camera=Camera.main;
            if (camera == null) return false;
            float header=HeaderBottom();

            if (Input.GetMouseButtonDown(0) &&
                info.AnyVisibleInfoBadgeContains(
                    ProducerInfoGuiPointerForProof(Input.mousePosition,Screen.height),
                    camera,header))
                return true;
            for(int i=0;i<Input.touchCount;i++)
            {
                Touch touch=Input.GetTouch(i);
                if(touch.phase != TouchPhase.Began) continue;
                if(info.AnyVisibleInfoBadgeContains(
                    ProducerInfoGuiPointerForProof(touch.position,Screen.height),
                    camera,header)) return true;
            }
            return false;
        }

        private bool AnyVisibleInfoBadgeContains(
            Vector2 guiPoint,Camera camera,float headerBottom)
        {
            foreach(string type in TrackedBuildingTypes)
            {
                if(!subscribedController.Registry.TryGetGameObjectByBuildingType(
                    type,out GameObject building) || building == null) continue;
                Rect bounds=ScreenRectFor(building,camera);
                if(bounds.width<=0f || bounds.height<=0f) continue;
                if(ProducerInfoBadgeRectForProof(bounds,
                    Screen.width,Screen.height,headerBottom).Contains(guiPoint))
                    return true;
            }
            return false;
        }
        private void LateUpdate()
        {
            // Run after BuildingInteractionController.Update: preserve the
            // original manual-collection and selection callbacks if they
            // fired. The fallback acts only when no modal opened normally.
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost ||
                subscribedController == null || OverlayOpenForExternalHost)
                return;
            PollProducerInfoPointer();
        }
        private void PollProducerInfoPointer()
        {
            if (OverlayOpenForExternalHost || subscribedController == null ||
                HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking() ||
                HiveViewProductUiPresenter.WorldChatInputBlocked)
                return;

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase != TouchPhase.Began) continue;
                    TryHandleProducerInfoPointer(touch.position);
                    return;
                }
            }
            if (Input.GetMouseButtonDown(0))
                TryHandleProducerInfoPointer(Input.mousePosition);
        }

        private void TryHandleProducerInfoPointer(Vector2 screenPointer)
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector2 guiPoint = ProducerInfoGuiPointerForProof(screenPointer, Screen.height);
            float header = HeaderBottom();

            // Tapping the visible gold "i" is always read directly in Update,
            // independently of the legacy OnGUI GUI.Button control.
            foreach (string type in TrackedBuildingTypes)
            {
                if (!subscribedController.Registry.TryGetGameObjectByBuildingType(
                    type, out GameObject building) || building == null) continue;
                Rect bounds = ScreenRectFor(building, camera);
                if (bounds.width <= 0f || bounds.height <= 0f) continue;
                Rect badge = ProducerInfoBadgeRectForProof(bounds,
                    Screen.width, Screen.height, header);
                if (!badge.Contains(guiPoint)) continue;
                OpenPremiumInfo(type);
#if UNITY_EDITOR
                Debug.Log("[BK-ProducerInfo] Gold info badge: " + type +
                    " shown=" + OverlayOpenForExternalHost);
#endif
                return;
            }

            // The shared UI/header and any other open building must keep
            // exclusive ownership of their own buttons and gestures.
            if (guiPoint.y < header ||
                EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = camera.ScreenPointToRay(screenPointer);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f,
                BuildingInteractionController.InteractionLayerMask,
                QueryTriggerInteraction.Ignore))
            {
                BuildingInteractionComponent interactive = hit.collider != null
                    ? hit.collider.GetComponent<BuildingInteractionComponent>() : null;
                if (interactive == null || interactive.Definition == null ||
                    !IsProducerBuildingType(interactive.Definition.BuildingType))
                    return; // e.g. Bank is not a producer
                OpenPremiumInfo(interactive.Definition.BuildingType);
#if UNITY_EDITOR
                Debug.Log("[BK-ProducerInfo] Collider producer click: " +
                    interactive.Definition.BuildingType + " shown=" + OverlayOpenForExternalHost);
#endif
                return;
            }

            // A decorated sprite can exceed its click collider. Restrict
            // fallback to producer art footprints, choosing the smallest
            // match so a larger background building does not win.
            string selectedType = null;
            float smallestArea = float.PositiveInfinity;
            foreach (string type in TrackedBuildingTypes)
            {
                if (!subscribedController.Registry.TryGetGameObjectByBuildingType(
                    type, out GameObject building) || building == null) continue;
                Rect bounds = ScreenRectFor(building, camera);
                if (bounds.width <= 0f || bounds.height <= 0f ||
                    !bounds.Contains(guiPoint)) continue;
                float area = bounds.width * bounds.height;
                if (area >= smallestArea) continue;
                smallestArea = area;
                selectedType = type;
            }
            if (selectedType == null) return;
            OpenPremiumInfo(selectedType);
#if UNITY_EDITOR
            Debug.Log("[BK-ProducerInfo] Building artwork clicked: " + selectedType +
                " shown=" + OverlayOpenForExternalHost);
#endif
        }
    }
}
