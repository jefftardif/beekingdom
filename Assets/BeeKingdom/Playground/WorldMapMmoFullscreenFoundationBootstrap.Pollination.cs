using System;
using System.Collections.Generic;
using BeeKingdom.Networking;
using UnityEngine;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// Read-only, zoom-legible floral habitats on the existing Wave6 nodes.
    /// Only the server catalogue can authorize a site and its real anchor.
    /// Artwork and bee flight are presentation; no local resource mutation.
    /// </summary>
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        private const float FloralMarkerRadius = 52f;
        private static readonly Color FloralGold = new Color(.98f,.81f,.43f,1f);
        private static readonly Color FloralRest = new Color(.60f,.55f,.39f,1f);
        private static readonly Color FloralGreen = new Color(.39f,.75f,.48f,1f);
        private static Texture2D floralBloom;
        private static Texture2D floralRecovering;
        private static Texture2D floralResting;
        private static Texture2D floralBee;
        private static bool floralArtLoaded;

        private static void LoadFloralArtwork()
        {
            if (floralArtLoaded) return;
            floralArtLoaded = true;
            floralBloom = Resources.Load<Texture2D>("Pollination/field_bloom");
            floralRecovering = Resources.Load<Texture2D>("Pollination/field_recovering");
            floralResting = Resources.Load<Texture2D>("Pollination/field_resting");
            floralBee = Resources.Load<Texture2D>("Pollination/forager");
        }

        private static bool CanMapFloralSite(RemoteFloralSite site) =>
            site != null &&
            site.MapSurfaceId == WorldId &&
            !string.IsNullOrEmpty(site.MapAnchorId) &&
            MobileAccountSessionRuntimeBootstrap.PollinationFeatureEnabledForWorldMap;

        private static IReadOnlyList<RemoteFloralSite> OfficialFloralSites() =>
            MobileAccountSessionRuntimeBootstrap.PollinationControllerForWorldMap
                ?.Model?.Sites ?? Array.Empty<RemoteFloralSite>();

        private static bool Recovering(RemoteFloralSite site)
        {
            string phase=site.Phase ?? string.Empty;
            return phase.IndexOf("recover",StringComparison.OrdinalIgnoreCase)>=0
                || phase.IndexOf("rest",StringComparison.OrdinalIgnoreCase)>=0
                || phase.IndexOf("regener",StringComparison.OrdinalIgnoreCase)>=0;
        }

        private static Texture2D ArtworkFor(RemoteFloralSite site)
        {
            if (site.PollenRemaining<=0 || site.PollenCapacity<=0)
                return floralResting;
            if (Recovering(site) ||
                site.PollenRemaining <= site.PollenCapacity / 4L)
                return floralRecovering;
            return floralBloom;
        }

        private static bool OwnCivilMissionAt(RemoteFloralSite site, out bool atField)
        {
            atField=false;
            RemoteCivilExpeditionMission mission =
                MobileAccountSessionRuntimeBootstrap.PollinationControllerForWorldMap
                    ?.Model?.Expedition?.Active;
            if (mission == null || mission.SiteId != site.SiteId)
                return false;
            DateTimeOffset now=DateTimeOffset.UtcNow;
            atField=now>=mission.ArrivesAtUtc && now<mission.WorkEndsAtUtc;
            return true;
        }

        // A floral site refers to the resource's server anchor. Display its
        // annotation above that resource instead of painting a second icon on it.
        // This changes neither the anchor nor a placement/reward request.
        private const float FloralWorldWidth = 112f;
        private Vector2 FloralDisplayPoint(WorldResourceNode anchor)
        {
            // A fixed WORLD annotation offset. Reserve the resource's largest
            // world envelope across the supported zoom range, including its
            // existing minimum-pixel artwork. Never shift the canonical node.
            string tier = ResourceTierToken(anchor);
            float minimum = tier == "rich" ? 148f : (tier == "medium" ? 124f : 104f);
            float envelope = Mathf.Max(ResourceSpriteSize(anchor), minimum / MinZoom);
            float top = anchor.WorldCoord.y - envelope * .58f;
            if (requireOfficialResourcePlacements &&
                officialPlacementView.TryGet(anchor.Id, out var placement))
                top = (float)placement.Top.Value;
            return WorldToScreen(new Vector2(anchor.WorldCoord.x,
                top - 12f - (FloralWorldWidth * .74f * .42f + 23f)));
        }

        private void DrawOfficialFloralMarkers()
        {
            if (PollinationWorldMapOverlay.IsModalOpen) return;
            IReadOnlyList<RemoteFloralSite> sites=OfficialFloralSites();
            if (sites == null || sites.Count==0) return;
            LoadFloralArtwork();
            foreach(RemoteFloralSite site in sites)
            {
                if (!CanMapFloralSite(site)) continue;
                WorldResourceNode anchor=ResourceById(site.MapAnchorId);
                if (anchor == null) continue;
                Vector2 center=FloralDisplayPoint(anchor);
                if (!IsOnScreen(center,FloralMarkerRadius*2f)) continue;
                // No decorative anchor connector; the server reserve gauge below remains.

                bool live=OwnCivilMissionAt(site,out bool atField);
                Texture2D art=ArtworkFor(site);
                float scale = currentZoom;
                float w=WorldSizeToScreen(FloralWorldWidth);
                float h=w*.74f;
                // Transparent flower sprites preserve the existing terrain
                // texture; the server alone decides whether a habitat exists.
                if (art!=null)
                {
                    Rect floralArt=new Rect(center.x-w*.5f,center.y-h*.52f,w,h);
                    GUI.DrawTexture(floralArt,art,ScaleMode.ScaleToFit,true);
                    RegisterDrawnMapHit(WorldMapHitKind.PointOfInterest,
                        "floral:"+site.SiteId,OpaqueArtHitRect(floralArt,art));
                }
                else
                {
                    // Graceful fallback when a client misses optional artwork.
                    DrawCircle(center,FloralMarkerRadius*.53f*scale,FloralGreen,18);
                    RegisterDrawnMapHit(WorldMapHitKind.PointOfInterest,
                        "floral:"+site.SiteId,new Rect(center.x-27f*scale,center.y-27f*scale,54f*scale,54f*scale));
                }

                if (live && floralBee!=null)
                {
                    // These animated workers represent this authenticated
                    // player's persisted mission, never invented enemy activity.
                    float t=Time.unscaledTime;
                    int count=atField ? 3 : 1;
                    for(int n=0;n<count;n++)
                    {
                        float a=t*(atField?1.33f:.72f)+n*2.0944f;
                        Vector2 pos=center+
                            new Vector2(Mathf.Cos(a)*w*.37f,
                                        Mathf.Sin(a)*h*.29f-7f*scale);
                        float bob=Mathf.Sin(t*4f+n)*2f*scale;
                        GUI.DrawTexture(new Rect(pos.x-14f*scale,pos.y-11f*scale+bob,28f*scale,24f*scale),
                            floralBee,ScaleMode.ScaleToFit,true);
                    }
                }

                Color status=site.PollenRemaining>0 ? FloralGold : FloralRest;
                string title=site.LocalizedName(
                    BeeKingdom.Localization.BeeLocalization.CurrentLocale);
                Rect floralCaption=new Rect(center.x-80f*scale,center.y-h*.54f-25f*scale,160f*scale,22f*scale);
                GUI.Label(floralCaption,title,MiniLabel(status,Mathf.Max(1,Mathf.RoundToInt(12f*scale)),TextAnchor.MiddleCenter));
                RegisterDrawnMapHit(WorldMapHitKind.PointOfInterest,
                    "floral:"+site.SiteId,floralCaption,true);

                // Shared reserve is an actual server value, not a countdown
                // or client-derived ecological reward. No blinking/refresh.
                float ratio=site.PollenCapacity>0 ?
                    Mathf.Clamp01((float)((double)site.PollenRemaining/site.PollenCapacity))
                    : 0f;
                Rect track=new Rect(center.x-37f*scale,center.y+h*.40f,74f*scale,4f*scale);
                Color previous=GUI.color;
                GUI.color=new Color(.10f,.21f,.14f,.92f);
                GUI.DrawTexture(track,Texture2D.whiteTexture);
                GUI.color=site.PollenRemaining>0 ? FloralGreen : FloralRest;
                GUI.DrawTexture(new Rect(track.x,track.y,track.width*ratio,track.height),
                    Texture2D.whiteTexture);
                GUI.color=previous;
                if (live)
                {
                    GUI.Label(new Rect(center.x-65f*scale,center.y+h*.42f+4f*scale,130f*scale,19f*scale),
                        atField ?
                            (BeeKingdom.Localization.BeeLocalization.CurrentLocale=="en-US"
                                ?"FORAGING":"BUTINAGE") :
                            (BeeKingdom.Localization.BeeLocalization.CurrentLocale=="en-US"
                                ?"ON MISSION":"EN MISSION"),
                        MiniLabel(FloralGold,Mathf.Max(1,Mathf.RoundToInt(10f*scale)),TextAnchor.MiddleCenter));
                }
            }
        }

        private bool TrySelectOfficialFloralMarker(Vector2 guiPoint)
        {
            if (PollinationWorldMapOverlay.IsModalOpen ||
                !MobileAccountSessionRuntimeBootstrap.PollinationFeatureEnabledForWorldMap)
                return false;
            foreach(RemoteFloralSite site in OfficialFloralSites())
            {
                if (!CanMapFloralSite(site)) continue;
                WorldResourceNode anchor=ResourceById(site.MapAnchorId);
                if (anchor==null) continue;
                Vector2 center=FloralDisplayPoint(anchor);
                if(HasDrawnMapHitAreas)
                {
                    if(!IsFloralHit(site.SiteId,guiPoint))continue;
                }
                else if(Vector2.Distance(guiPoint,center)>WorldSizeToScreen(FloralMarkerRadius+7f))
                    continue;
                if (PollinationWorldMapOverlay.TryOpenForSite(site.SiteId))
                    return true;
            }
            return false;
        }
    }
}
