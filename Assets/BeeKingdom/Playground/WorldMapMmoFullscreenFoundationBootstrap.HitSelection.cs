using UnityEngine;
using System.Collections.Generic;

namespace BeeKingdom.Playground
{
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        private Vector2 lastHitPoint;
        private Vector2 lastHitCenter;
        private float lastHitZoom;
        private WorldMapHitKind lastHitKind;
        private string lastHitId;

        private Vector2 BestiaryDisplayPoint(WorldBestiaryNode beast)
        {
            return WorldToScreen(beast.WorldCoord);
        }

        private WorldMapHitKind ChooseMapHit(Vector2 point)
        {
            if (TrySelectDrawnMapHit(point,out WorldMapHitKind drawnKind,out string drawnId))
            {
                lastHitPoint=point;lastHitCenter=currentWorldCenter;
                lastHitZoom=currentZoom;lastHitKind=drawnKind;lastHitId=drawnId;
                return drawnKind;
            }
            if(HasDrawnMapHitAreas)
            {
                lastHitKind=WorldMapHitKind.None;lastHitId=null;
                return WorldMapHitKind.None;
            }
            var candidates = new List<WorldMapHitCandidate>(hives.Count + resources.Count + bestiary.Count + pointsOfInterest.Count);
            if (mapFilterHives)
                foreach (WorldHiveNode node in hives)
                    candidates.Add(new WorldMapHitCandidate(WorldMapHitKind.Hive, node.Id, Vector2.Distance(point, WorldToScreen(node.WorldCoord))));
            if (mapFilterResources)
                foreach (WorldResourceNode node in resources)
                    if(OfficialResourceHitAllowed(node,point))candidates.Add(ResourceHitCandidate(node,point));
            if (mapFilterThreats)
                foreach (WorldBestiaryNode node in bestiary)
                    candidates.Add(new WorldMapHitCandidate(WorldMapHitKind.Bestiary, node.Id, Vector2.Distance(point, BestiaryDisplayPoint(node))));
            foreach (WorldPointOfInterestNode node in pointsOfInterest)
                candidates.Add(new WorldMapHitCandidate(WorldMapHitKind.PointOfInterest, node.Id, Vector2.Distance(point, WorldToScreen(node.WorldCoord))));
            bool repeat = Vector2.Distance(point, lastHitPoint) <= 4f
                && Vector2.Distance(currentWorldCenter, lastHitCenter) <= 0.01f
                && Mathf.Abs(currentZoom - lastHitZoom) <= 0.001f;
            int index = WorldMapHitSelection.ChooseIndex(candidates, currentZoom,
                repeat ? lastHitKind : WorldMapHitKind.None, repeat ? lastHitId : null);
            WorldMapHitKind hit = index < 0 ? WorldMapHitKind.None : candidates[index].Kind;
            lastHitPoint = point;
            lastHitCenter = currentWorldCenter;
            lastHitZoom = currentZoom;
            lastHitKind = hit;
            lastHitId = index < 0 ? null : candidates[index].Id;
            return hit;
        }
    }
}
