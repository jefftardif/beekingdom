using System;
using System.Collections.Generic;
using UnityEngine;
namespace BeeKingdom.Playground
{
    [Serializable] public sealed class OccupationDrawEvidence
    {
        public string legacyVisualId,kind,spritePath,hitRule;
        public int frame,screenWidth,screenHeight;
        public float zoom,worldX,worldY,centerX,centerY,hitRadiusScreen;
        public Rect drawScreen,drawWorld;
        public bool cameraMatchesCapture;
    }
    [Serializable] public sealed class OccupationPlacementEvidence
    {
        public string entityId,placementId,kind,worldId,surfaceId,coordinateUnit,terrainRevision;
        public long revision,occupationRevision;
        public double x,y,left,top,right,bottom;
    }
    [Serializable] public sealed class OccupationRuntimeEvidence
    {
        public string schema="bk-occupation-runtime-evidence-v2",utc,legacyMapWorldId;
        public bool notForGameActivation=true,completeWorldInventory=false;
        public OccupationSessionEvidence session;
        public List<OccupationDrawEvidence> drawn=new List<OccupationDrawEvidence>();
        public List<OccupationPlacementEvidence> savedOfficialResources=new List<OccupationPlacementEvidence>();
        public string activeFlightId,activeNodeId;
        public long resourceModelRevision;
        public string limitations="Draw records are actual GUI calls, not alpha bounds; labels/decorations/fallbacks excluded. Legacy IDs are not canonical bindings. No network refresh. Old/off-camera records explicitly marked.";
    }
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        private readonly Dictionary<string,OccupationDrawEvidence> occupationDrawEvidence=new Dictionary<string,OccupationDrawEvidence>();
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void RecordOccupationDraw(string kind,string id,Vector2 world,Rect draw,string sprite,float hitRadius)
        {
            // Diagnostic observation only; bounded to avoid retaining all traversed chunks.
            if(occupationDrawEvidence.Count>=2048)occupationDrawEvidence.Clear();
            Vector2 a=ScreenToWorld(new Vector2(draw.xMin,draw.yMin),currentWorldCenter,currentZoom);
            Vector2 b=ScreenToWorld(new Vector2(draw.xMax,draw.yMax),currentWorldCenter,currentZoom);
            occupationDrawEvidence[kind+":"+id]=new OccupationDrawEvidence{legacyVisualId=id,kind=kind,spritePath=sprite,frame=Time.frameCount,
                screenWidth=Screen.width,screenHeight=Screen.height,zoom=currentZoom,worldX=world.x,worldY=world.y,centerX=currentWorldCenter.x,centerY=currentWorldCenter.y,
                drawScreen=draw,drawWorld=Rect.MinMaxRect(a.x,a.y,b.x,b.y),hitRadiusScreen=Mathf.Max(12f,hitRadius*currentZoom),
                hitRule=kind=="resource"?"distance radius intersect official reserved rectangle when enabled":"distance radius; closest/tie arbitration"};
        }
        partial void AppendOccupationOfficialPlacementEvidence(OccupationRuntimeEvidence result);
        public OccupationRuntimeEvidence CaptureOccupationRuntimeEvidence()
        {
            var result=new OccupationRuntimeEvidence{utc=DateTime.UtcNow.ToString("O"),legacyMapWorldId=WorldId,session=MobileAccountSessionRuntimeBootstrap.CaptureOccupationSessionEvidence()};
            foreach(var record in occupationDrawEvidence.Values)
            {
                // Copy before adding capture-specific validity; leave runtime observations untouched.
                var copy=JsonUtility.FromJson<OccupationDrawEvidence>(JsonUtility.ToJson(record));
                copy.cameraMatchesCapture=record.screenWidth==Screen.width && record.screenHeight==Screen.height && record.zoom==currentZoom
                    && record.centerX==currentWorldCenter.x && record.centerY==currentWorldCenter.y && record.frame>=Time.frameCount-1;
                result.drawn.Add(copy);
            }
            AppendOccupationOfficialPlacementEvidence(result);
            var model=HiveViewProductUiPresenter.OfficialWorldResourceCollectionModelForWorldMap();
            if(model!=null){result.resourceModelRevision=model.Revision;result.activeFlightId=model.Active?.FlightId.ToString("D");result.activeNodeId=model.Active?.NodeId;}
            return result;
        }
    }
}
