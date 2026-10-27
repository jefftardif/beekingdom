using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Networking;
using BeeKingdom.MapPlacement;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        [SerializeField] private bool requireOfficialResourcePlacements;
        [SerializeField] private string officialPlacementWorldId;
        [SerializeField] private string officialPlacementSurfaceId;
        [SerializeField] private string officialPlacementCoordinateUnit;
        private readonly WorldResourcePlacementView officialPlacementView=new WorldResourcePlacementView();
        private List<WorldResourceNode> officialPlacedResources=new List<WorldResourceNode>();
        private bool officialPlacementReady;

        private void RefreshOfficialResourcePlacements()
        {
            if(!requireOfficialResourcePlacements)return;
            var model=HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            var nodes=model?.Nodes;
            var staged=new List<WorldResourceNode>();
            if(nodes==null){officialPlacementReady=false;return;}
            foreach(var node in nodes)
            {
                if(node==null){officialPlacementReady=false;return;}
                ResourceKind kind;
                switch(node.ResourceKey){case "honey":kind=ResourceKind.Honey;break;case "pollen":kind=ResourceKind.Pollen;break;case "wax":kind=ResourceKind.Wax;break;default:officialPlacementReady=false;return;}
                if(node.PlacementStatus=="unplaced")continue;
                if(node.Placement==null){officialPlacementReady=false;return;}
                staged.Add(new WorldResourceNode(node.NodeId,node.Label,kind,new Vector2((float)(node.Placement.X??0),(float)(node.Placement.Y??0)),(int)Math.Min(int.MaxValue,Math.Max(0,node.Yield))));
            }
            string reason;
            officialPlacementReady=officialPlacementView.TryInstall(nodes,officialPlacementWorldId,officialPlacementSurfaceId,officialPlacementCoordinateUnit,out reason);
            if(!officialPlacementReady||reason=="unchanged")return;
            officialPlacedResources=staged;
            foreach(var chunk in chunkCache.Values)ApplyOfficialResourceCoordinates(chunk);
            RefreshActiveChunks(true);
        }
        private void ApplyOfficialResourceCoordinates(WorldChunkData data)
        {
            if(!requireOfficialResourcePlacements)return;
            for(int i=0;i<data.Resources.Count;i++)
            {
                var old=data.Resources[i];MapPlacementSnapshot p;
                if(officialPlacementView.TryGet(old.Id,out p))data.Resources[i]=new WorldResourceNode(old.Id,old.Label,old.Kind,new Vector2((float)p.X.Value,(float)p.Y.Value),old.Amount);
            }
        }
        private void AppendOfficialResourcesInView()
        {
            if(!requireOfficialResourcePlacements||!officialPlacementView.HasAcceptedSnapshot)return;
            resources.RemoveAll(r=>IsOfficialResource(r)||officialPlacementView.Contains(r.Id));
            foreach(var resource in officialPlacedResources)
            {
                MapPlacementSnapshot p;if(!officialPlacementView.TryGet(resource.Id,out p))continue;
                var bounds=MapPlacementRules.Bounds(p);
                if(activeChunks.Any(c=>bounds.Conflicts(new PlacementRect(c.x*ChunkSize,c.y*ChunkSize,(c.x+1)*ChunkSize,(c.y+1)*ChunkSize),0)))resources.Add(resource);
            }
        }
        // Save canonical geometry separately from seeded chunk ownership.
        public MapPlacementSnapshot[] CaptureOfficialResourcePlacements()=>officialPlacementView.Save();
        private bool OfficialResourceHitAllowed(WorldResourceNode node,Vector2 screen)
        {
            if(!requireOfficialResourcePlacements||!IsOfficialResource(node))return true;
            MapPlacementSnapshot p;if(!officialPlacementView.TryGet(node.Id,out p))return !officialPlacementView.HasAcceptedSnapshot;
            // Both texture and fallback start from a square; use the same fitted rectangle as DrawResources.
            var rect=OfficialResourceDrawRect(node,new Rect(0,0,1,1));
            return screen.x>=rect.xMin&&screen.x<rect.xMax&&screen.y>=rect.yMin&&screen.y<rect.yMax;
        }
        private WorldMapHitCandidate ResourceHitCandidate(WorldResourceNode node,Vector2 screen)
        {
            if(requireOfficialResourcePlacements && officialPlacementView.Contains(node.Id))
            {
                var rect=OfficialResourceDrawRect(node,new Rect(0,0,1,1));
                var center=new Vector2(rect.x+rect.width/2,rect.y+rect.height/2);
                return new WorldMapHitCandidate(WorldMapHitKind.Resource,node.Id,Vector2.Distance(screen,center),OfficialResourceHitAllowed(node,screen));
            }
            return new WorldMapHitCandidate(WorldMapHitKind.Resource,node.Id,Vector2.Distance(screen,WorldToScreen(node.WorldCoord)));
        }
        private Rect OfficialResourceDrawRect(WorldResourceNode node,Rect original)
        {
            if(!requireOfficialResourcePlacements)return original;
            MapPlacementSnapshot p;if(!officialPlacementView.TryGet(node.Id,out p))return original;
            var r=MapPlacementRules.Project(p,currentWorldCenter.x,currentWorldCenter.y,currentZoom,Screen.width,Screen.height);
            float width=(float)(r.Right-r.Left),height=(float)(r.Bottom-r.Top);
            float scale=Mathf.Min(width/original.width,height/original.height);
            float drawnWidth=original.width*scale,drawnHeight=original.height*scale;
            return new Rect((float)r.Left+(width-drawnWidth)/2,(float)r.Top+(height-drawnHeight)/2,drawnWidth,drawnHeight);
        }
    }
}
