using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// Stable, world-space allocation of LEGACY/DEMO map visuals only.
    /// This never creates an official lease and must not authorize a resource,
    /// world-map move, reward, colony or territory on the server.
    /// </summary>
    public sealed class WorldMapLocalWorldOccupancy
    {
        public struct Claim
        {
            public string Id;
            public Vector2 WorldPoint;
            public Vector2 Size;
            public Vector2 Offset;
            public bool Fixed;
        }
        public readonly struct Decision
        {
            public readonly string Code;
            public readonly Vector2 Position;
            public Decision(string code, Vector2 position) { Code=code; Position=position; }
            public bool Accepted => Code=="reserved" || Code=="unchanged";
        }
        private readonly List<Claim> claims=new List<Claim>();
        private readonly Dictionary<string, Claim> byId=new Dictionary<string, Claim>(StringComparer.Ordinal);
        private readonly float margin;
        public WorldMapLocalWorldOccupancy(float gap=14f) { if(gap<0)throw new ArgumentException("gap");margin=gap; }
        public int Count=>claims.Count;
        public bool TryGet(string id,out Claim result)=>byId.TryGetValue(id,out result);
        public static Rect Box(Claim item) => new Rect(
            item.WorldPoint.x+item.Offset.x-item.Size.x*.5f,
            item.WorldPoint.y+item.Offset.y-item.Size.y*.5f,
            item.Size.x,item.Size.y);
        private static bool Intersects(Claim a,Claim b,float gap)
        {
            Rect rect=Box(a);
            rect.xMin-=gap*.5f;rect.xMax+=gap*.5f;
            rect.yMin-=gap*.5f;rect.yMax+=gap*.5f;
            return rect.Overlaps(Box(b));
        }
        public bool IsClear(Claim candidate,string exceptId=null)
        {
            foreach(var existing in claims)
                if(existing.Id!=exceptId && Intersects(existing,candidate,margin))return false;
            return true;
        }
        public bool TryPin(Claim claim)
        {
            if(string.IsNullOrEmpty(claim.Id)||claim.Size.x<=0||claim.Size.y<=0||byId.ContainsKey(claim.Id))return false;
            if(!IsClear(claim))return false;
            claim.Fixed=true;
            claims.Add(claim);byId.Add(claim.Id,claim);
            return true;
        }
        // A rejected historic position still blocks new placements.
        public bool ObserveUnresolved(Claim claim)
        {
            if(string.IsNullOrEmpty(claim.Id)||claim.Size.x<=0||claim.Size.y<=0||
                byId.ContainsKey(claim.Id))return false;
            claims.Add(claim);byId.Add(claim.Id,claim);
            return true;
        }
        public Decision Reserve(Claim claim,IEnumerable<Vector2> vettedCandidates,
            Func<Vector2,bool> terrainValid)
        {
            if(string.IsNullOrEmpty(claim.Id)||claim.Size.x<=0||claim.Size.y<=0||
                byId.ContainsKey(claim.Id)||vettedCandidates==null||terrainValid==null)
                return new Decision("invalid_or_duplicate",claim.WorldPoint);
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(Vector2 proposed in vettedCandidates)
            {
                if(float.IsNaN(proposed.x)||float.IsNaN(proposed.y)||
                    float.IsInfinity(proposed.x)||float.IsInfinity(proposed.y))continue;
                string key=proposed.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)
                    +":"+proposed.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
                if(!seen.Add(key)||!terrainValid(proposed))continue;
                Claim candidate=claim;candidate.WorldPoint=proposed;
                if(!IsClear(candidate))continue;
                claims.Add(candidate);byId.Add(candidate.Id,candidate);
                return new Decision(proposed==claim.WorldPoint?"unchanged":"reserved",proposed);
            }
            // Do NOT invent a new location, erase a node or persist a rejected move.
            return new Decision("no_verified_free_world_location",claim.WorldPoint);
        }
    }
}
