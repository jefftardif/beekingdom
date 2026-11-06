using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BeeKingdom.EditorOccupationAudit
{
    [Serializable] public sealed class OccupationNode
    {
        public string source,kind,id,subtype,chunk;
        public float x,y;
        public int tier,variant;
        public string footprintStatus="not_measured";
    }
    [Serializable] public sealed class FloralAnchorRead
    {
        public string siteId,worldId,surfaceId,resourceAnchorId;
        public string positionStatus="legacy_resource_anchor_only";
    }
    [Serializable] public sealed class OccupationRead
    {
        public string schema="bk-occupation-readonly-v1",utc,worldId;
        public bool completeWorldInventory=false;
        public bool serverAuthorityCertified=false;
        public float centerX,centerY,zoom;
        public int chunkSize;
        public List<string> cachedChunks=new List<string>();
        public List<OccupationNode> nodes=new List<OccupationNode>();
        public List<FloralAnchorRead> floralSites=new List<FloralAnchorRead>();
        public List<string> limitations=new List<string>();
    }
    // Reads only an explicit allowlist of fields/backing fields. No property getters,
    // method invocation, setters, network, generation, texture loads or source mutation.
    public static class MapOccupationReadOnlySnapshot
    {
        private const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        public static object Field(object value,string name)
        {
            if(value==null)return null;
            var type=value.GetType();
            // Vector2Int exposes x/y as properties backed by m_X/m_Y. Read backing fields only.
            string backing=type.FullName=="UnityEngine.Vector2Int" && (name=="x"||name=="y")
                ? (name=="x"?"m_X":"m_Y") : "<"+name+">k__BackingField";
            return (type.GetField(name,Instance)??type.GetField(backing,Instance))?.GetValue(value);
        }
        private static string Text(object value)=>value==null?string.Empty:Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);
        private static float Number(object value)=>value==null?0f:Convert.ToSingle(value,System.Globalization.CultureInfo.InvariantCulture);
        private static void Nodes(OccupationRead result,object list,string kind,string source,string chunk)
        {
            if(!(list is IList values)){result.limitations.Add("missing_collection:"+source);return;}
            foreach(object node in values)
            {
                object position=Field(node,"WorldCoord");
                if(position==null){result.limitations.Add("missing_coordinate:"+Text(Field(node,"Id")));continue;}
                result.nodes.Add(new OccupationNode{source=source,chunk=chunk,kind=kind,id=Text(Field(node,"Id")),
                    x=Number(Field(position,"x")),y=Number(Field(position,"y")),
                    subtype=Text(Field(node,kind=="hive"?"Stage":"Kind")),
                    tier=(int)Number(Field(node,"Tier")),variant=(int)Number(Field(node,"Variant"))});
            }
        }
        public static OccupationRead Read(object bootstrap,object floralController)
        {
            if(bootstrap==null)throw new ArgumentNullException(nameof(bootstrap));
            var result=new OccupationRead{utc=DateTime.UtcNow.ToString("O")};
            var type=bootstrap.GetType();
            var world=type.GetField("WorldId",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            var size=type.GetField("ChunkSize",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            result.worldId=world!=null&&world.IsLiteral?Text(world.GetRawConstantValue()):"unknown";
            result.chunkSize=size!=null&&size.IsLiteral?(int)size.GetRawConstantValue():0;
            object center=Field(bootstrap,"currentWorldCenter");
            result.centerX=Number(Field(center,"x"));result.centerY=Number(Field(center,"y"));result.zoom=Number(Field(bootstrap,"currentZoom"));
            foreach(var pair in new[]{new[]{"hives","hive"},new[]{"resources","resource"},new[]{"bestiary","bestiary"},new[]{"pointsOfInterest","point-of-interest"}})
                Nodes(result,Field(bootstrap,pair[0]),pair[1],"visible:"+pair[0],"");
            if(Field(bootstrap,"chunkCache") is IDictionary cache)
                foreach(DictionaryEntry entry in cache)
                {
                    string chunk=Text(Field(entry.Key,"x"))+","+Text(Field(entry.Key,"y"));result.cachedChunks.Add(chunk);
                    foreach(var pair in new[]{new[]{"Hives","hive"},new[]{"Resources","resource"},new[]{"Bestiary","bestiary"},new[]{"PointsOfInterest","point-of-interest"}})
                        Nodes(result,Field(entry.Value,pair[0]),pair[1],"cache:"+pair[0],chunk);
                }
            else result.limitations.Add("chunk_cache_unavailable");
            object model=Field(floralController,"Model");
            if(Field(model,"Sites") is IEnumerable sites)
                foreach(var site in sites)result.floralSites.Add(new FloralAnchorRead{siteId=Text(Field(site,"SiteId")),worldId=Text(Field(site,"WorldId")),surfaceId=Text(Field(site,"MapSurfaceId")),resourceAnchorId=Text(Field(site,"MapAnchorId"))});
            else result.limitations.Add("loaded_floral_model_unavailable");
            result.limitations.Add("Only already-loaded chunks; no generation or traversal. Missing neighbours are not empty.");
            result.limitations.Add("Duplicate visible/cache observations retained by source. No canonical account binding inferred.");
            result.limitations.Add("Sprite/decorative bounds not measured here; combine with reviewed rendering rules and authorised capture.");
            return result;
        }
    }
}
