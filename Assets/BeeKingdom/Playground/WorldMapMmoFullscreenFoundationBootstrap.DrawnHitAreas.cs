using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        private readonly List<DrawnMapHit> drawnMapHits = new List<DrawnMapHit>(256);
        private struct DrawnMapHit
        {
            public WorldMapHitKind Kind;
            public string Id;
            public Rect Rectangle;
            public bool Label;
        }
        private void ClearDrawnMapHitAreas()
        {
            if(Event.current != null && Event.current.type == EventType.Repaint)
                drawnMapHits.Clear();
        }
        // Predict later-drawn artwork before placing labels. Icon positions
        // stay WORLD-anchored; only captions may move within screen-space.
        private void ReserveFutureMapArtwork()
        {
            if(Event.current==null || Event.current.type!=EventType.Repaint)return;
            if(mapFilterHives)
                foreach(var hive in hives)
                {
                    Vector2 p=WorldToScreen(hive.WorldCoord);
                    if(!IsOnScreen(p,155f))continue;
                    float nominal=WorldSizeToScreen(HiveSize(hive.Stage));
                    Texture2D tex=RuntimeEntityTexture(HiveTexturePath(hive.Stage));
                    float s=tex!=null?nominal*2.6f:nominal;
                    var art=new Rect(p.x-s*.5f,p.y-s*.58f,s,s);
                    RegisterDrawnMapHit(WorldMapHitKind.Hive,hive.Id,OpaqueArtHitRect(art,tex));
                }
            if(mapFilterResources)
                foreach(var res in resources)
                {
                    Vector2 p=WorldToScreen(res.WorldCoord);
                    if(!IsOnScreen(p,170f))continue;
                    Texture2D tex=RuntimeEntityTexture(ResourceTexturePath(res));
                    float s=tex!=null?ResourceSpriteScreenSize(res):WorldSizeToScreen(24f);
                    var rect=OfficialResourceDrawRect(res,new Rect(p.x-s*.5f,p.y-s*.58f,s,s));
                    RegisterDrawnMapHit(WorldMapHitKind.Resource,res.Id,OpaqueArtHitRect(rect,tex));
                }
            if(mapFilterThreats)
                foreach(var beast in bestiary)
                {
                    Vector2 p=WorldToScreen(beast.WorldCoord);
                    if(!IsOnScreen(p,155f))continue;
                    float s=WorldSizeToScreen(48f+beast.Tier*7.5f);
                    if(beast.Tier==2 && beast.Variant==2)s*=1.15f;
                    Texture2D tex=RuntimeEntityTexture(BestiaryTexturePath(beast));
                    RegisterDrawnMapHit(WorldMapHitKind.Bestiary,beast.Id,
                        OpaqueArtHitRect(new Rect(p.x-s*.5f,p.y-s*.58f,s,s),tex));
                }
            var sites=OfficialFloralSites();
            if(sites!=null)
                foreach(var site in sites)
                {
                    if(!CanMapFloralSite(site))continue;
                    var node=ResourceById(site.MapAnchorId);
                    if(node==null)continue;
                    Vector2 p=WorldToScreen(node.WorldCoord);
                    if(!IsOnScreen(p,155f))continue;
                    float w=Screen.width<650?91f:112f,h=w*.74f;
                    Texture2D tex=ArtworkFor(site);
                    RegisterDrawnMapHit(WorldMapHitKind.PointOfInterest,"floral:"+site.SiteId,
                        OpaqueArtHitRect(new Rect(p.x-w*.5f,p.y-h*.52f,w,h),tex));
                }
        }

        private Rect OpaqueArtHitRect(Rect drawRect,Texture2D texture)
        {
            Rect alpha=ResourceLabelInkBounds(texture);
            return new Rect(drawRect.x+drawRect.width*alpha.x,
                drawRect.y+drawRect.height*alpha.y,
                drawRect.width*alpha.width,drawRect.height*alpha.height);
        }
        private void RegisterDrawnMapHit(WorldMapHitKind kind,string id,Rect rectangle,bool label=false)
        {
            if(Event.current == null || Event.current.type != EventType.Repaint ||
                string.IsNullOrEmpty(id) || rectangle.width<=0 || rectangle.height<=0)
                return;
            drawnMapHits.Add(new DrawnMapHit {
                Kind=kind,Id=id,Rectangle=rectangle,Label=label
            });
        }
        private bool TrySelectDrawnMapHit(Vector2 point,out WorldMapHitKind kind,out string id)
        {
            kind=WorldMapHitKind.None;id=null;
            float smallest=float.MaxValue;
            foreach(var hit in drawnMapHits)
            {
                if(!hit.Rectangle.Contains(point))continue;
                float area=hit.Rectangle.width*hit.Rectangle.height;
                if(area>=smallest)continue;
                smallest=area;kind=hit.Kind;id=hit.Id;
            }
            return kind!=WorldMapHitKind.None;
        }
        private bool IsFloralHit(string siteId,Vector2 point)
        {
            foreach(var hit in drawnMapHits)
                if(hit.Kind==WorldMapHitKind.PointOfInterest &&
                    hit.Id=="floral:"+siteId && hit.Rectangle.Contains(point))
                    return true;
            return false;
        }
        private bool HasDrawnMapHitAreas => drawnMapHits.Count>0;
        public int DrawnHitAreaCountForProof => drawnMapHits.Count;
    }
}
