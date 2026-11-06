using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        private WorldMapVisualSeparation.Layout visualOccupation;
        private int visualOccupationFrame = -1;
        private Vector2 visualOccupationCenter;
        private float visualOccupationZoom;
        private int visualOccupationPopulation;
        private int visualOccupationScreenWidth, visualOccupationScreenHeight;
        private string visualOccupationError = "";

        private static string VisualKey(string kind, string id) => kind + ":" + id;

        // Visual repositioning at each zoom is prohibited. Only committed world
        // coordinates can locate icons; collision diagnostics remain observational.
        private Vector2 VisualPoint(string kind, string id, Vector2 original) => original;
        private Rect? ReservedVisualCaption(string kind, string id) => null;

        private void DrawVisualAttachment(string kind, string id, Vector2 worldAnchor, Vector2 displayed)
        {
            // UI separation is never a new persistent/server world position.
            if (Vector2.Distance(worldAnchor, displayed) > 2f)
                DrawLine(worldAnchor, displayed,
                    new Color(.81f, .69f, .40f, .65f), 1.5f);
        }

        // Measure actual opaque sprite ink, not the transparent 512x512 source
        // canvas. DrawTexture(ScaleToFit) and the caption use exactly this fit.
        private Vector2 MeasureVisibleMapArtwork(Texture2D texture,
            float fullWidth, float fullHeight, float topFraction, out Vector2 offset)
        {
            float fittedWidth=fullWidth,fittedHeight=fullHeight;
            if(texture!=null && texture.width>0 && texture.height>0)
            {
                float fit=Mathf.Min(fullWidth/texture.width,fullHeight/texture.height);
                fittedWidth=texture.width*fit;fittedHeight=texture.height*fit;
            }
            Rect ink=ResourceLabelInkBounds(texture);
            float visibleW=Mathf.Max(1f,ink.width*fittedWidth);
            float visibleH=Mathf.Max(1f,ink.height*fittedHeight);
            float renderedLeft=-fittedWidth*.5f;
            float renderedTop=-fullHeight*topFraction+(fullHeight-fittedHeight)*.5f;
            float inkLeft=renderedLeft+ink.x*fittedWidth;
            float inkTop=renderedTop+ink.y*fittedHeight;
            offset=new Vector2(inkLeft+visibleW*.5f,
                inkTop+visibleH*topFraction);
            return new Vector2(visibleW,visibleH);
        }

        private void PrepareSharedVisualOccupation()
        {
            var sites = OfficialFloralSites();
            int count = hives.Count + resources.Count + bestiary.Count +
                pointsOfInterest.Count + (sites?.Count ?? 0);
            if (visualOccupation != null &&
                visualOccupationFrame == Time.frameCount &&
                visualOccupationScreenWidth == Screen.width &&
                visualOccupationScreenHeight == Screen.height &&
                visualOccupationPopulation == count &&
                visualOccupationCenter == currentWorldCenter &&
                Mathf.Abs(visualOccupationZoom - currentZoom) < .00001f) return;

            visualOccupationFrame = Time.frameCount;
            visualOccupationScreenWidth = Screen.width;
            visualOccupationScreenHeight = Screen.height;
            visualOccupationPopulation = count;
            visualOccupationCenter = currentWorldCenter;
            visualOccupationZoom = currentZoom;

            var claims = new List<WorldMapVisualSeparation.Claim>(count);
            if (mapFilterHives)
                foreach (var hive in hives)
                {
                    Vector2 anchor = WorldToScreen(hive.WorldCoord);
                    if (!IsOnScreen(anchor, 170f)) continue;
                    float nominal = WorldSizeToScreen(HiveSize(hive.Stage));
                    Texture2D texture = RuntimeEntityTexture(HiveTexturePath(hive.Stage));
                    float artSize = texture != null ? nominal * 2.6f : nominal * 2f;
                    Vector2 artInk=MeasureVisibleMapArtwork(texture,
                        artSize,artSize,.58f,out Vector2 artOffset);
                    bool memberKnown;
                    string alliance = hive.Id == "hive_player_test"
                        ? MobileAccountSessionRuntimeBootstrap.MapPlayerAllianceName(out memberKnown)
                        : string.Empty;
                    string status = LocalizedHiveBadge(hive.Badge) + " · " + StageLabel(hive.Stage)
                        + (hive.Id == selectedHiveId ? " · " + MapText("hive.selected","selectionnee") : "");
                    string description = string.IsNullOrEmpty(alliance) ? status : alliance + "\n" + status;
                    claims.Add(new WorldMapVisualSeparation.Claim {
                        Key=VisualKey("hive",hive.Id), Anchor=anchor,
                        ArtWidth=artInk.x,ArtHeight=artInk.y,ArtOffset=artOffset,
                        CaptionSize=MeasurePremiumMapLabel(LocalizedHiveName(hive),description,true),
                        Priority=hive.Id=="hive_player_test" ? 0 : 2
                    });
                }
            if (mapFilterResources)
                foreach(var node in resources)
                {
                    Vector2 anchor=WorldToScreen(node.WorldCoord);
                    if(!IsOnScreen(anchor,180f))continue;
                    Texture2D texture=RuntimeEntityTexture(ResourceTexturePath(node));
                    float size=texture != null ? ResourceSpriteScreenSize(node) : WorldSizeToScreen(24f);
                    bool officialPinned=requireOfficialResourcePlacements && officialPlacementView.Contains(node.Id);
                    float w=size,h=size;
                    if(officialPinned)
                    {
                        Rect officialRect=OfficialResourceDrawRect(node,
                            new Rect(anchor.x-size*.5f,anchor.y-size*.58f,size,size));
                        anchor=officialRect.center + new Vector2(0f,officialRect.height*.08f);
                        w=officialRect.width;h=officialRect.height;
                    }
                    bool label=node.Id==selectedResourceId;
                    Vector2 artInk=MeasureVisibleMapArtwork(texture,
                        w,h,.58f,out Vector2 artOffset);
                    claims.Add(new WorldMapVisualSeparation.Claim {
                        Key=VisualKey("resource",node.Id),Anchor=anchor,
                        ArtWidth=artInk.x,ArtHeight=artInk.y,ArtOffset=artOffset,
                        CaptionSize=label ? MeasurePremiumMapLabel(ResourceLabel(node.Kind),
                            ResourceQuantityLabel(node),false) : Vector2.zero,
                        ExtraBottom=IsOfficialResource(node) ? 27f : 0f,
                        // Official SQL resource placements are immutable in this
                        // presentation solver. Local water is also kept at its
                        // source anchor until reviewed hydrology is available.
                        Priority=officialPinned?1:4,
                        Pinned=officialPinned || node.Kind==ResourceKind.Water
                    });
                }
            if(sites!=null)
                foreach(var site in sites)
                {
                    if(!CanMapFloralSite(site)) continue;
                    var anchorNode=ResourceById(site.MapAnchorId);
                    if(anchorNode==null)continue;
                    Vector2 anchor=WorldToScreen(anchorNode.WorldCoord);
                    if(!IsOnScreen(anchor,190f))continue;
                    float width=Screen.width<650?91f:112f;
                    float height=width*.74f;
                    Texture2D art=ArtworkFor(site);
                    Vector2 artInk=MeasureVisibleMapArtwork(art,width,height,
                        .52f,out Vector2 artOffset);
                    claims.Add(new WorldMapVisualSeparation.Claim {
                        Key=VisualKey("floral",site.SiteId),Anchor=anchor,
                        ArtWidth=artInk.x,ArtHeight=artInk.y,
                        ArtTopFraction=.52f,ArtOffset=artOffset,
                        CaptionSize=new Vector2(160f,22f),
                        FixedCaptionOffset=true,
                        CaptionOffset=new Vector2(-80f,-height*.54f-25f),
                        ExtraBottom=17f,Priority=3
                    });
                }
            if(mapFilterThreats)
                foreach(var beast in bestiary)
                {
                    Vector2 anchor=WorldToScreen(beast.WorldCoord);
                    if(!IsOnScreen(anchor,180f))continue;
                    float size=WorldSizeToScreen(48f+beast.Tier*7.5f);
                    if(beast.Tier==2&&beast.Variant==2)size*=1.15f;
                    Texture2D texture=RuntimeEntityTexture(BestiaryTexturePath(beast));
                    Vector2 artInk=MeasureVisibleMapArtwork(texture,
                        size,size,.58f,out Vector2 artOffset);
                    string title=BestiaryAccessibilityWord(beast)+" T"+beast.Tier+" "+LocalizedBestiaryName(beast.Label);
                    if(IsRareSighting(beast,out string eventName))
                        title="? "+HiveViewProductUiPresenter.WorldEventDisplayName(eventName)+"\n"+title;
                    claims.Add(new WorldMapVisualSeparation.Claim {
                        Key=VisualKey("bestiary",beast.Id),Anchor=anchor,
                        ArtWidth=artInk.x,ArtHeight=artInk.y,
                        ArtOffset=artOffset,
                        CaptionSize=MeasurePremiumMapLabel(title,"PV "+BestiaryVirtualHp(beast),false),
                        ExtraTop=beast.Id==selectedBestiaryId?9f:0f,
                        Priority=5
                    });
                }
            foreach(var point in pointsOfInterest)
            {
                Vector2 anchor=WorldToScreen(point.WorldCoord);
                if(!IsOnScreen(anchor,170f))continue;
                float nominal=WorldSizeToScreen(point.Id==selectedPointOfInterestId?30f:26f);
                claims.Add(new WorldMapVisualSeparation.Claim {
                    Key=VisualKey("poi",point.Id),Anchor=anchor,
                    ArtWidth=nominal*2f,ArtHeight=nominal*2f,ArtTopFraction=.5f,
                    CaptionSize=new Vector2(164f,18f),FixedCaptionOffset=true,
                    CaptionOffset=new Vector2(-82f,nominal+6f),Priority=6
                });
            }
            var boundary=new Rect(8f, 32f, Mathf.Max(1,Screen.width-16f),
                Mathf.Max(1,Screen.height-62f));
            visualOccupation=WorldMapVisualSeparation.Solve(claims,boundary,10f);
            if(!visualOccupation.IsClear)
            {
                // Missing visual capacity is an auditable failure, NOT permission
                // to silently hide a resource, enemy, hive or floral site.
                var signature=string.Join(",",visualOccupation.Unresolved);
                if(signature!=visualOccupationError) {
                    visualOccupationError=signature;
                    Debug.LogWarning("WorldMap occupation requires more space: "+signature);
                }
            }
            else visualOccupationError="";
        }

        // Selection tests the identical drawn artwork and caption rectangles,
        // without relocating any entity or clicking an invisible legacy radius.
        private bool TryMapVisualHit(Vector2 point,out WorldMapHitKind kind,out string id)
            => TrySelectDrawnMapHit(point,out kind,out id);

        public int UnresolvedVisualOccupationCountForProof =>
            visualOccupation == null ? -1 : visualOccupation.Unresolved.Count;
        public int VisualOccupationEntityCountForProof =>
            visualOccupation == null ? -1 : visualOccupation.Slots.Count;
    }
}
