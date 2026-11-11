using System;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.LivingHiveMenu;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// Fallback for producer artwork whose physical collider is smaller than
    /// its visible sprite. It routes to the SAME producer click handler and
    /// never creates an independent collection or information action.
    /// </summary>
    public sealed partial class HiveMapProductionInfoBootstrap
    {
        public static Vector2 ProducerInfoGuiPointerForProof(
            Vector2 screenPointer, float screenHeight)
        {
            return new Vector2(screenPointer.x, screenHeight-screenPointer.y);
        }

        public static Rect ProducerBuildingHitBoundsForProof(
            Rect visual, float screenWidth, float screenHeight, float headerBottom)
        {
            float left=Mathf.Max(0f,visual.xMin);
            float right=Mathf.Min(screenWidth,visual.xMax);
            float top=Mathf.Max(headerBottom+4f,visual.yMin);
            float bottom=Mathf.Min(screenHeight,visual.yMax);
            if(right<=left || bottom<=top) return Rect.zero;
            return Rect.MinMaxRect(left,top,right,bottom);
        }

        private static float HeaderBottom()
        {
            bool portrait=LivingHiveMenuSpec.IsPortrait(Screen.width,Screen.height);
            Rect rect=portrait
                ? LivingHiveMenuHeaderData.PortraitHeaderRect(Screen.width,Screen.height)
                : LivingHiveMenuHeaderData.LandscapeHeaderRect(Screen.width,Screen.height);
            return rect.yMax;
        }

        private void LateUpdate()
        {
            // Selection.Update processes the real building collider first.
            // A single physical tap must never be routed twice.
            if(!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost ||
                subscribedController==null || OverlayOpenForExternalHost ||
                lastProducerClickFrame==Time.frameCount)
                return;
            PollProducerBuildingPointer();
        }

        private void PollProducerBuildingPointer()
        {
            if(HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking() ||
               HiveViewProductUiPresenter.WorldChatInputBlocked)
                return;

            if(Input.touchCount>0)
            {
                for(int i=0;i<Input.touchCount;i++)
                {
                    Touch touch=Input.GetTouch(i);
                    if(touch.phase!=TouchPhase.Began) continue;
                    TryHandleProducerBuildingPointer(touch.position);
                    return;
                }
            }
            if(Input.GetMouseButtonDown(0))
                TryHandleProducerBuildingPointer(Input.mousePosition);
        }

        private void TryHandleProducerBuildingPointer(Vector2 screenPointer)
        {
            Camera camera=Camera.main;
            if(camera==null || lastProducerClickFrame==Time.frameCount) return;
            Vector2 guiPoint=ProducerInfoGuiPointerForProof(
                screenPointer,Screen.height);
            float header=HeaderBottom();

            // Other visible uGUI widgets and the hive header own their taps.
            if(guiPoint.y<header ||
               (EventSystem.current!=null &&
                EventSystem.current.IsPointerOverGameObject()))
                return;

            if(Physics.Raycast(camera.ScreenPointToRay(screenPointer),
                out RaycastHit hit,500f,
                BuildingInteractionController.InteractionLayerMask,
                QueryTriggerInteraction.Ignore))
            {
                BuildingInteractionComponent component=hit.collider!=null
                    ? hit.collider.GetComponentInParent<BuildingInteractionComponent>()
                    : null;
                if(component==null || component.Definition==null ||
                   !IsProducerBuildingType(component.Definition.BuildingType))
                    return; // never click through Bank/Palace/other buildings
                OnBuildingClicked(component.Definition);
                return;
            }

            // Only if the raycast missed everything: use the drawn footprint
            // with HUD clipping. Choose the smallest overlapping producer.
            string best=null;
            float area= float.PositiveInfinity;
            foreach(string type in TrackedBuildingTypes)
            {
                if(!subscribedController.Registry.TryGetGameObjectByBuildingType(
                    type,out GameObject go) || go==null) continue;
                Rect bounds=ProducerBuildingHitBoundsForProof(
                    ScreenRectFor(go,camera),
                    Screen.width,Screen.height,header);
                if(bounds.width<=0f || bounds.height<=0f ||
                   !bounds.Contains(guiPoint)) continue;
                float candidate=bounds.width*bounds.height;
                if(candidate>=area) continue;
                area=candidate;
                best=type;
            }
            if(best==null) return;
            OnBuildingClicked(BuildingCatalog.GetByBuildingType(best));
#if UNITY_EDITOR
            Debug.Log("[BK-ProducerInfo] Artwork selection dispatched: "+best);
#endif
        }
    }
}
