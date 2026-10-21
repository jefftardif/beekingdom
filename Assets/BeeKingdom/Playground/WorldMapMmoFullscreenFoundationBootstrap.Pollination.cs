using System;
using System.Collections.Generic;
using BeeKingdom.Networking;
using UnityEngine;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// Adds read-only floral overlays to the EXISTING world map. A marker
    /// can only appear when the real server supplies both the exact surface
    /// identifier and the ID of an existing stable world-map resource anchor.
    /// No scene coordinates are generated or stored by this adapter.
    /// </summary>
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        private const float FloralMarkerRadius = 22f;
        private static readonly Color FloralActive = new Color(0.97f,0.78f,0.30f,0.95f);
        private static readonly Color FloralExhausted = new Color(0.59f,0.57f,0.46f,0.86f);
        private static readonly Color FloralFill = new Color(0.10f,0.23f,0.14f,0.80f);

        // Presentation only: site identities are global server-generated IDs;
        // this value is the *existing* Wave6 terrain surface (not a new world).
        private static bool CanMapFloralSite(RemoteFloralSite site) =>
            site != null &&
            site.MapSurfaceId == WorldId &&
            !string.IsNullOrEmpty(site.MapAnchorId) &&
            MobileAccountSessionRuntimeBootstrap.PollinationFeatureEnabledForWorldMap;

        private static IReadOnlyList<RemoteFloralSite> OfficialFloralSites() =>
            MobileAccountSessionRuntimeBootstrap.PollinationControllerForWorldMap
                ?.Model?.Sites ?? Array.Empty<RemoteFloralSite>();

        private void DrawOfficialFloralMarkers()
        {
            if (PollinationWorldMapOverlay.IsModalOpen) return;
            IReadOnlyList<RemoteFloralSite> sites = OfficialFloralSites();
            if (sites == null || sites.Count == 0) return;

            for (int i = 0; i < sites.Count; i++)
            {
                RemoteFloralSite site = sites[i];
                if (!CanMapFloralSite(site)) continue;
                WorldResourceNode anchor = ResourceById(site.MapAnchorId);
                if (anchor == null) continue; // never synthesize a position

                Vector2 center = WorldToScreen(anchor.WorldCoord);
                if (!IsOnScreen(center, FloralMarkerRadius * 3f)) continue;
                float radius = FloralMarkerRadius;
                Color edge = site.PollenRemaining > 0
                    ? FloralActive : FloralExhausted;
                DrawCircle(center, radius + 5f,
                    new Color(edge.r, edge.g, edge.b, 0.24f), 20);
                DrawCircle(center, radius, edge, 18);
                DrawCircle(center, radius - 5f, FloralFill, 14);

                var labelRect = new Rect(center.x - 65f, center.y - radius - 27f, 130f, 23f);
                GUI.Label(labelRect, site.LocalizedName(
                        BeeKingdom.Localization.BeeLocalization.CurrentLocale),
                    MiniLabel(edge, 12, TextAnchor.MiddleCenter));
            }
        }

        private bool TrySelectOfficialFloralMarker(Vector2 guiPoint)
        {
            if (PollinationWorldMapOverlay.IsModalOpen ||
                !MobileAccountSessionRuntimeBootstrap.PollinationFeatureEnabledForWorldMap)
                return false;

            IReadOnlyList<RemoteFloralSite> sites = OfficialFloralSites();
            foreach (RemoteFloralSite site in sites)
            {
                if (!CanMapFloralSite(site)) continue;
                WorldResourceNode anchor = ResourceById(site.MapAnchorId);
                if (anchor == null) continue;
                Vector2 center = WorldToScreen(anchor.WorldCoord);
                if (Vector2.Distance(guiPoint, center) > FloralMarkerRadius + 7f)
                    continue;
                if (PollinationWorldMapOverlay.TryOpenForSite(site.SiteId))
                    return true;
            }
            return false;
        }
    }
}
