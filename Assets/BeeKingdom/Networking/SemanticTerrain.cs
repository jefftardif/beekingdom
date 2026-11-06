using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using BeeKingdom.MapPlacement;

namespace BeeKingdom.MapPlacement
{
    public interface IPlacementTerrainAdmission
    {
        string Revision { get; }
        bool AllowsNewFootprint(PlacementRect footprint,double margin,bool water);
    }
    public sealed class TerrainRun { public int Length { get; set; } public string Kind { get; set; } }
    public sealed class SemanticTerrainTile
    {
        public int ChunkX { get; set; } public int ChunkY { get; set; }
        public string PngSha256 { get; set; } public bool Reviewed { get; set; }
        public string ReviewEvidence { get; set; } public TerrainRun[] Runs { get; set; }
    }
    public sealed class SemanticTerrainDocument
    {
        public string Schema { get; set; } public string Revision { get; set; }
        public string ManifestSha256 { get; set; }
        public string CoordinateContract { get; set; }
        public SemanticTerrainTile[] Tiles { get; set; }
    }
    public sealed class SemanticTerrain : IPlacementTerrainAdmission
    {
        private sealed class Run { public int End;public string Kind; }
        private readonly Dictionary<string,Run[]> tiles=new Dictionary<string,Run[]>(StringComparer.Ordinal);
        public string Revision { get; }
        public string ContentSha256 { get; private set; }
        private SemanticTerrain(string revision){Revision=revision;}
        // Expected hashes come from the independently checked frozen terrain inventory.
        // No automatic colour classification is accepted as a reviewed surface.
        public static SemanticTerrain Load(SemanticTerrainDocument doc,string manifestSha,string expectedRevision,IReadOnlyDictionary<string,string> pngHashes,string expectedContentSha256)
        {
            if(doc==null||doc.Schema!="bk-semantic-terrain-v1"||doc.Revision!=expectedRevision||string.IsNullOrWhiteSpace(expectedRevision)
                ||doc.ManifestSha256!=manifestSha||manifestSha==null||manifestSha.Length!=64
                ||doc.CoordinateContract!="wave6:origin=7,7;grid=50,50;core=512;gutter=2;y=down"
                ||doc.Tiles==null||doc.Tiles.Length>2500||pngHashes==null)throw new ArgumentException("semantic_terrain_contract_invalid");
            var result=new SemanticTerrain(doc.Revision);
            foreach(var tile in doc.Tiles)
            {
                if(tile==null||tile.ChunkX<7||tile.ChunkX>=57||tile.ChunkY<7||tile.ChunkY>=57)throw new ArgumentException("semantic_tile_invalid");
                var key=tile.ChunkX+","+tile.ChunkY;
                if(result.tiles.ContainsKey(key)||!pngHashes.TryGetValue(key,out var hash)||tile.PngSha256!=hash||hash.Length!=64
                    ||tile.Runs==null||tile.Runs.Length>262144)throw new ArgumentException("semantic_tile_provenance_invalid");
                var runs=new List<Run>();int end=0;
                foreach(var run in tile.Runs)
                {
                    if(run==null||run.Length<=0||run.Length>262144-end||!(run.Kind=="Unknown"||run.Kind=="Land"||run.Kind=="River"||run.Kind=="Lake")
                        ||run.Kind!="Unknown"&&(!tile.Reviewed||string.IsNullOrWhiteSpace(tile.ReviewEvidence)))throw new ArgumentException("semantic_annotation_unreviewed_or_invalid");
                    end+=run.Length;runs.Add(new Run{End=end,Kind=run.Kind});
                }
                if(end!=262144)throw new ArgumentException("semantic_raster_incomplete");
                result.tiles.Add(key,runs.ToArray());
            }
            result.ContentSha256=ComputeContentSha256(doc);
            if(!string.Equals(result.ContentSha256,expectedContentSha256,StringComparison.Ordinal))throw new ArgumentException("semantic_content_version_mismatch");
            return result;
        }
        public static string ComputeContentSha256(SemanticTerrainDocument doc)
        {
            using(var stream=new MemoryStream())
            {
                using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
                {
                    writer.Write(doc.Schema??"");writer.Write(doc.Revision??"");writer.Write(doc.ManifestSha256??"");writer.Write(doc.CoordinateContract??"");
                    foreach(var tile in doc.Tiles.OrderBy(t=>t.ChunkY).ThenBy(t=>t.ChunkX))
                    {
                        writer.Write(tile.ChunkX);writer.Write(tile.ChunkY);writer.Write(tile.PngSha256??"");writer.Write(tile.Reviewed);writer.Write(tile.ReviewEvidence??"");writer.Write(tile.Runs.Length);
                        foreach(var run in tile.Runs){writer.Write(run.Length);writer.Write(run.Kind??"");}
                    }
                }
                using(var algorithm=SHA256.Create())return BitConverter.ToString(algorithm.ComputeHash(stream.ToArray())).Replace("-","");
            }
        }
        public bool AllowsNewFootprint(PlacementRect footprint,double margin,bool water)
        {
            if(!footprint.Valid||!MapPlacementRules.Finite(margin)||margin<0)return false;
            var r=footprint.Expand(margin);
            if(r.Left<3584||r.Top<3584||r.Right>=29184||r.Bottom>=29184)return false;
            if((Math.Floor(r.Right)-Math.Floor(r.Left)+1)*(Math.Floor(r.Bottom)-Math.Floor(r.Top)+1)>1048576)return false;
            // Closed edges are conservatively included. Missing neighbouring tile = Unknown.
            for(int y=(int)Math.Floor(r.Top);y<=(int)Math.Floor(r.Bottom);y++)
            for(int x=(int)Math.Floor(r.Left);x<=(int)Math.Floor(r.Right);x++)
            {
                if(!tiles.TryGetValue((x/512)+","+(y/512),out var runs))return false;
                int pixel=(y%512)*512+x%512;int low=0,high=runs.Length-1;
                while(low<high){int mid=(low+high)/2;if(pixel<runs[mid].End)high=mid;else low=mid+1;}
                string kind=runs[low].Kind;
                if(water?kind!="River"&&kind!="Lake":kind!="Land")return false;
            }
            return true;
        }
    }
}
