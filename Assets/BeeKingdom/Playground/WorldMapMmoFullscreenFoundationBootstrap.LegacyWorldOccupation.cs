using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        private WorldMapLocalWorldOccupancy localWorldReservations;
        private readonly HashSet<string> unresolvedLegacyWorldPositions =
            new HashSet<string>(StringComparer.Ordinal);

        public int LocalWorldPositionConflictsForProof => unresolvedLegacyWorldPositions.Count;

        private WorldMapLocalWorldOccupancy.Claim Claim(string family,string id,
            Vector2 location,Vector2 size,Vector2 offset)
        {
            return new WorldMapLocalWorldOccupancy.Claim {
                Id=family+":"+id,WorldPoint=location,Size=size,Offset=offset
            };
        }

        // Geometry in world units. The caption reserve is designed for the
        // common mid-zoom UI; fixed-pixel labels need separate zoom-space layout.
        private static Vector2 LegacyEnvelope(string family,int tier=0)
        {
            if(family=="hive")return new Vector2(146,132);
            if(family=="resource")return new Vector2(164,105);
            if(family=="bestiary")return new Vector2(135,120);
            return new Vector2(106,80);
        }
        private static Vector2 LegacyOffset(string family)
        {
            if(family=="hive")return new Vector2(0,31);
            if(family=="resource")return new Vector2(0,12);
            if(family=="bestiary")return new Vector2(0,28);
            return new Vector2(0,10);
        }

        private void EnsureLocalWorldReservations()
        {
            if(localWorldReservations!=null)return;
            localWorldReservations=new WorldMapLocalWorldOccupancy(14f);
            // Permanent core-hive reservation prevents earlier-loaded neighbouring
            // chunks from taking the player's place before the core chunk streams in.
            Vector2Int home=PlayerCoreChunk();
            Vector2 point=RuntimePlacementPointAvoidingBearDen(home,
                ChunkLocalWorld(home,.48f,.54f),RuntimePlacementFamily.Hive,
                ResourceKind.Pollen,501);
            localWorldReservations.TryPin(Claim("hive","hive_player_test",
                point,LegacyEnvelope("hive"),LegacyOffset("hive")));
        }

        private IEnumerable<Vector2> ReviewedLocalLandAnchors(
            Vector2Int origin,Vector2 preferred,string family,ResourceKind kind)
        {
            // The mask is a visual approval tied to the Wave5-method preview,
            // NOT a production semantic terrain registry. Never move an official
            // hive, resource, land claim, or water node using this fallback.
            if(!useWave5Method12288PreviewRuntimePackageForPlayMode ||
                !runtimePlacementMaskLoaded)yield break;
            var positions=new List<Vector2>();
            positions.Add(preferred);
            for(int radius=0;radius<=2;radius++)
                for(int dy=-radius;dy<=radius;dy++)
                    for(int dx=-radius;dx<=radius;dx++)
                    {
                        if(Mathf.Max(Mathf.Abs(dx),Mathf.Abs(dy))!=radius)continue;
                        var chunk=new Vector2Int(origin.x+dx,origin.y+dy);
                        if(!runtimePlacementMask.TryGetValue(chunk,out RuntimePlacementMaskEntry mask))continue;
                        if(mask.hive_score<=0f)continue;
                        void Add(float x,float y)
                        {
                            Vector2 p=ChunkLocalWorld(chunk,x,y);
                            if((p-preferred).sqrMagnitude>1024f*1024f)return;
                            if(p.x<WorldMapWave6StreamingTileProvider.OriginChunkX*ChunkSize+32 ||
                               p.y<WorldMapWave6StreamingTileProvider.OriginChunkY*ChunkSize+32 ||
                               p.x>(WorldMapWave6StreamingTileProvider.OriginChunkX+
                                    WorldMapWave6StreamingTileProvider.Columns)*ChunkSize-32 ||
                               p.y>(WorldMapWave6StreamingTileProvider.OriginChunkY+
                                    WorldMapWave6StreamingTileProvider.Rows)*ChunkSize-32)return;
                            if(bearDenLandmark!=null && bearDenLandmark.ExcludesSpawn(p))return;
                            positions.Add(p);
                        }
                        if(family=="hive") {Add(mask.hive_x,mask.hive_y);}
                        else if(family=="bestiary"||family=="poi")
                        {
                            Add(mask.threat_x,mask.threat_y);
                            Add(mask.hive_x,mask.hive_y);
                            Add(mask.nectar_x,mask.nectar_y);
                            Add(mask.pollen_x,mask.pollen_y);
                        }
                        else {
                            Anchor01 a=RuntimeAnchor(mask,RuntimePlacementFamily.Resource,kind);
                            Add(a.X,a.Y);
                            Add(mask.resource_x,mask.resource_y);
                            Add(mask.nectar_x,mask.nectar_y);
                            Add(mask.pollen_x,mask.pollen_y);
                        }
                    }
            // Stable enumeration independent of the camera zoom and floating UI.
            positions.Sort((a,b)=>{
                int cmp=(a-preferred).sqrMagnitude.CompareTo((b-preferred).sqrMagnitude);
                if(cmp!=0)return cmp;
                cmp=a.y.CompareTo(b.y);
                return cmp!=0?cmp:a.x.CompareTo(b.x);
            });
            foreach(Vector2 p in positions)yield return p;
        }

        private Vector2 ReserveLegacyWorldPosition(
            Vector2Int home,string family,string id,Vector2 previous,
            ResourceKind kind,bool immutable)
        {
            EnsureLocalWorldReservations();
            var envelope=Claim(family,id,previous,
                LegacyEnvelope(family),LegacyOffset(family));
            if(localWorldReservations.TryGet(envelope.Id,out _))return previous;
            if(immutable)
            {
                if(!localWorldReservations.TryPin(envelope))
                {
                    localWorldReservations.ObserveUnresolved(envelope);
                    unresolvedLegacyWorldPositions.Add(envelope.Id);
                }
                return previous;
            }
'            // Unverified or unavailable mask: retain clean historical anchors,
            // but never manufacture a replacement from random terrain pixels.
            if(!useWave5Method12288PreviewRuntimePackageForPlayMode ||
                !runtimePlacementMaskLoaded)
            {
                if(localWorldReservations.TryPin(envelope))return previous;
                localWorldReservations.ObserveUnresolved(envelope);
                unresolvedLegacyWorldPositions.Add(envelope.Id);
                return previous;
            }
'            var candidates=ReviewedLocalLandAnchors(home,previous,family,kind);
            var answer=localWorldReservations.Reserve(envelope,candidates,
                point=>bearDenLandmark==null||!bearDenLandmark.ExcludesSpawn(point));
            if(answer.Accepted) return answer.Position;
            // A refusal must preserve the previous position, not suppress its
            // icon, mutate it anyway, or save any invented location.
            localWorldReservations.ObserveUnresolved(envelope);
            unresolvedLegacyWorldPositions.Add(envelope.Id);
            return previous;
        }

        private void ReserveLegacyChunkInWorld(WorldChunkData data)
        {
            EnsureLocalWorldReservations();
            for(int i=0;i<data.Hives.Count;i++)
            {
                WorldHiveNode h=data.Hives[i];
                Vector2 location=ReserveLegacyWorldPosition(data.Chunk,
                    "hive",h.Id,h.WorldCoord,ResourceKind.Pollen,h.Id=="hive_player_test");
                if(location!=h.WorldCoord)
                    data.Hives[i]=new WorldHiveNode(h.Id,h.Label,h.Badge,h.Stage,
                        location,h.Description);
            }
            for(int i=0;i<data.Resources.Count;i++)
            {
                WorldResourceNode r=data.Resources[i];
                bool official=IsOfficialResource(r);
                // The current prototype has no reviewed hydrology polygon for
                // relocating water. Preserve its original anchor until that
                // spatial registry can certify a different location.
                bool immutable=official||r.Kind==ResourceKind.Water;
                Vector2 location=ReserveLegacyWorldPosition(data.Chunk,
                    "resource",r.Id,r.WorldCoord,r.Kind,immutable);
                if(location!=r.WorldCoord)
                    data.Resources[i]=new WorldResourceNode(r.Id,r.Label,r.Kind,location,r.Amount);
            }
            for(int i=0;i<data.Bestiary.Count;i++)
            {
                WorldBestiaryNode b=data.Bestiary[i];
                Vector2 location=ReserveLegacyWorldPosition(data.Chunk,
                    "bestiary",b.Id,b.WorldCoord,ResourceKind.Pollen,false);
                if(location!=b.WorldCoord)
                    data.Bestiary[i]=new WorldBestiaryNode(b.Id,b.Label,b.Tier,b.Variant,
                        location,b.Role);
            }
            for(int i=0;i<data.PointsOfInterest.Count;i++)
            {
                WorldPointOfInterestNode poi=data.PointsOfInterest[i];
                Vector2 location=ReserveLegacyWorldPosition(data.Chunk,
                    "poi",poi.Id,poi.WorldCoord,ResourceKind.Pollen,false);
                if(location!=poi.WorldCoord)
                    data.PointsOfInterest[i]=new WorldPointOfInterestNode(
                        poi.Id,poi.Label,poi.Kind,location,poi.Description,
                        poi.Family,poi.PrimaryBiome,poi.History,poi.BossTeaser);
            }
        }
    }
}
