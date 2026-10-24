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
                Vector2 center=WorldToScreen(anchor.WorldCoord);
                if (!IsOnScreen(center,FloralMarkerRadius*2f)) continue;

                bool live=OwnCivilMissionAt(site,out bool atField);
                Texture2D art=ArtworkFor(site);
                float w=Screen.width < 650 ? 91f : 112f;
                float h=w*.74f;
                // Transparent flower sprites preserve the existing terrain
                // texture; the server alone decides whether a habitat exists.
                if (art!=null)
                {
                    GUI.DrawTexture(
                        new Rect(center.x-w*.5f,center.y-h*.52f,w,h),
                        art,ScaleMode.ScaleToFit,true);
                }
                else
                {
                    // Graceful fallback when a client misses optional artwork.
                    DrawCircle(center,FloralMarkerRadius*.53f,FloralGreen,18);
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
                                        Mathf.Sin(a)*h*.29f-7f);
                        float bob=Mathf.Sin(t*4f+n)*2f;
                        GUI.DrawTexture(new Rect(pos.x-14f,pos.y-11f+bob,28f,24f),
                            floralBee,ScaleMode.ScaleToFit,true);
                    }
                }

                Color status=site.PollenRemaining>0 ? FloralGold : FloralRest;
                string title=site.LocalizedName(
                    BeeKingdom.Localization.BeeLocalization.CurrentLocale);
                GUI.Label(new Rect(center.x-80f,center.y-h*.54f-25f,160f,22f),
                    title,MiniLabel(status,12,TextAnchor.MiddleCenter));

                // Shared reserve is an actual server value, not a countdown
                // or client-derived ecological reward. No blinking/refresh.
                float ratio=site.PollenCapacity>0 ?
                    Mathf.Clamp01((float)((double)site.PollenRemaining/site.PollenCapacity))
                    : 0f;
                Rect track=new Rect(center.x-37f,center.y+h*.40f,74f,4f);
                Color previous=GUI.color;
                GUI.color=new Color(.10f,.21f,.14f,.92f);
                GUI.DrawTexture(track,Texture2D.whiteTexture);
                GUI.color=site.PollenRemaining>0 ? FloralGreen : FloralRest;
                GUI.DrawTexture(new Rect(track.x,track.y,track.width*ratio,track.height),
                    Texture2D.whiteTexture);
                GUI.color=previous;
                if (live)
                {
                    GUI.Label(new Rect(center.x-65f,center.y+h*.42f+4f,130f,19f),
                        atField ?
                            (BeeKingdom.Localization.BeeLocalization.CurrentLocale=="en-US"
                                ?"FORAGING":"BUTINAGE") :
                            (BeeKingdom.Localization.BeeLocalization.CurrentLocale=="en-US"
                                ?"ON MISSION":"EN MISSION"),
                        MiniLabel(FloralGold,10,TextAnchor.MiddleCenter));
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
                Vector2 center=WorldToScreen(anchor.WorldCoord);
                if (Vector2.Distance(guiPoint,center)>FloralMarkerRadius+7f)
                    continue;
                if (PollinationWorldMapOverlay.TryOpenForSite(site.SiteId))
                    return true;
            }
            return false;
        }
    }
}
