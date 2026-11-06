using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using BeeKingdom.Alliance.Territories;
using BeeKingdom.HiveOperations;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.TerritorySqlCandidate;

public sealed record SpatialRequiredSource(string Kind,long Revision,string[] ExpectedIds);
public sealed record SpatialImportManifest(string Version,string WorldId,string SurfaceId,string CoordinateUnit,SpatialRequiredSource[] Sources);
public sealed record SpatialObjectFootprint(string ObjectId,TerritoryPoint[]? Ring);
public sealed record SpatialSourceSnapshot(string Kind,long Revision,bool InventoryComplete,SpatialObjectFootprint[] Objects);
public sealed record SpatialCoverageResult(bool Published,bool Complete,string Code,long ScopeRevision,string[] MissingSources,string[] UnresolvedObjects,int ValidFootprints);

// Trusted server importer. Input snapshots must come from the owners of the source inventories.
// No player endpoint; this verifies completeness against an explicit frozen source manifest,
// not that an arbitrary caller's manifest describes the entire real world.
public sealed class SqlTerritorySpatialImporter(string connectionString,string world,string surface,string coordinateUnit)
{
    public SpatialCoverageResult Import(SpatialImportManifest manifest,SpatialSourceSnapshot[] snapshots,long expectedScopeRevision,Action? beforeCommit=null)
    {
        if(manifest.WorldId!=world||manifest.SurfaceId!=surface||manifest.CoordinateUnit!=coordinateUnit||string.IsNullOrWhiteSpace(manifest.Version)||manifest.Version.Length>128||manifest.Sources is null||manifest.Sources.Length is <1 or >32||snapshots is null)throw new InvalidDataException("spatial_manifest_invalid");
        bool Key(string s)=>!string.IsNullOrWhiteSpace(s)&&s.Length<=128;
        if(manifest.Sources.Any(s=>s is null||!Key(s.Kind)||s.Revision<0||s.ExpectedIds is null||s.ExpectedIds.Length>10000||s.ExpectedIds.Any(x=>!Key(x))||s.ExpectedIds.Distinct(StringComparer.Ordinal).Count()!=s.ExpectedIds.Length)||manifest.Sources.Select(s=>s.Kind).Distinct(StringComparer.Ordinal).Count()!=manifest.Sources.Length)throw new InvalidDataException("spatial_manifest_invalid");
        if(new[]{"hives","public-floral","obstacles"}.Any(kind=>!manifest.Sources.Any(s=>s.Kind==kind)))throw new InvalidDataException("spatial_required_source_missing");
        if(snapshots.Any(s=>s is null||!manifest.Sources.Any(m=>m.Kind==s.Kind)||s.Objects is null||s.Objects.Length>10000)||snapshots.Select(s=>s.Kind).Distinct(StringComparer.Ordinal).Count()!=snapshots.Length)throw new InvalidDataException("spatial_sources_invalid");
        manifest=manifest with {Sources=manifest.Sources.Select(s=>s with {ExpectedIds=s.ExpectedIds.ToArray()}).ToArray()};
        snapshots=snapshots.Select(s=>s with {Objects=s.Objects.Select(o=>o is null?throw new InvalidDataException("spatial_object_null"):o with {Ring=o.Ring?.ToArray()}).ToArray()}).ToArray();
        using var c=new SqlConnection(connectionString);c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        string key="territory:"+Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]{world,surface})));
        using(var cmd=SqlTerritoryStore.Command(c,tx,"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@k,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; SELECT @r;",("@k",key)))if(Convert.ToInt32(cmd.ExecuteScalar())<0)throw new InvalidOperationException("spatial_lock_unavailable");
        SqlTerritoryScope scope;
        using(var cmd=SqlTerritoryStore.Command(c,tx,"SELECT Revision,ConfigJson FROM dbo.TerritoryWorldScopes WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",("@w",world),("@s",surface)))
        using(var r=cmd.ExecuteReader()) {
            if(!r.Read())throw new InvalidOperationException("scope_required");scope=JsonSerializer.Deserialize<SqlTerritoryScope>(r.GetString(1))??throw new InvalidDataException("scope_invalid");
            if(scope.Revision!=r.GetInt64(0)||scope.WorldId!=world||scope.SurfaceId!=surface)throw new InvalidDataException("scope_invalid");
            if(scope.Revision!=expectedScopeRevision)return new(false,false,"spatial_revision_conflict",scope.Revision,[],[],0);
        }
        string manifestJson=JsonSerializer.Serialize(manifest);
        using(var cmd=SqlTerritoryStore.Command(c,tx,"SELECT Kind FROM dbo.TerritorySpatialSourceVersions WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",("@w",world),("@s",surface)))
        using(var r=cmd.ExecuteReader())while(r.Read())if(!manifest.Sources.Any(s=>s.Kind==r.GetString(0)))throw new InvalidDataException("spatial_source_removal_requires_migration");
        using(var cmd=SqlTerritoryStore.Command(c,tx,"SELECT ManifestJson FROM dbo.TerritorySpatialManifests WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s AND Version=@v",("@w",world),("@s",surface),("@v",manifest.Version)))
            if(cmd.ExecuteScalar() is string old&&old!=manifestJson)throw new InvalidDataException("spatial_manifest_version_conflict");
        var missing=new List<string>();var unresolved=new List<string>();var rings=new List<TerritoryPoint[]>();
        foreach(var required in manifest.Sources) {
            var snapshot=snapshots.SingleOrDefault(s=>s.Kind==required.Kind);
            if(snapshot is null){missing.Add(required.Kind);continue;}
            string hash=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(snapshot)));
            using(var cmd=SqlTerritoryStore.Command(c,tx,"SELECT Revision,PayloadHash,Pending FROM dbo.TerritorySpatialSourceVersions WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s AND Kind=@k",("@w",world),("@s",surface),("@k",required.Kind)))
            using(var r=cmd.ExecuteReader())if(r.Read()&&(snapshot.Revision!=r.GetInt64(0)||!r.GetBoolean(2)&&hash!=r.GetString(1)))throw new InvalidDataException("spatial_source_revision_conflict");
            using(var cmd=SqlTerritoryStore.Command(c,tx,"UPDATE dbo.TerritorySpatialSourceVersions SET Revision=@r,PayloadHash=@h,Pending=0 WHERE WorldId=@w AND SurfaceId=@s AND Kind=@k; IF @@ROWCOUNT=0 INSERT dbo.TerritorySpatialSourceVersions VALUES(@w,@s,@k,@r,@h,0)",("@w",world),("@s",surface),("@k",required.Kind),("@r",snapshot.Revision),("@h",hash)))cmd.ExecuteNonQuery();
            if(snapshot.Revision!=required.Revision||!snapshot.InventoryComplete){missing.Add(required.Kind);continue;}
            if(snapshot.Objects.Any(o=>o is null||!Key(o.ObjectId))||snapshot.Objects.Select(o=>o.ObjectId).Distinct(StringComparer.Ordinal).Count()!=snapshot.Objects.Length)throw new InvalidDataException("spatial_duplicate_identity");
            if(snapshot.Objects.Any(o=>!required.ExpectedIds.Contains(o.ObjectId,StringComparer.Ordinal)))throw new InvalidDataException("spatial_unexpected_identity");
            foreach(var id in required.ExpectedIds) {
                var item=snapshot.Objects.SingleOrDefault(o=>o.ObjectId==id);
                if(item?.Ring is null){unresolved.Add(required.Kind+":"+id);continue;}
                if(!TerritoryGeometry.Validate(item.Ring,scope.Bounds).Valid){unresolved.Add(required.Kind+":"+id+":invalid_geometry");continue;}
                rings.Add(item.Ring);
            }
        }
        // Include existing reservations in the decision. Conflict invalidates coverage and keeps
        // old exclusions; never silently certify clear land while the new source overlaps a seat.
        bool overlap=false;
        using(var cmd=SqlTerritoryStore.Command(c,tx,"SELECT BuildingId,AllianceId,ConstructionJson FROM dbo.TerritoryBuildings WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",("@w",world),("@s",surface)))
        using(var r=cmd.ExecuteReader())while(r.Read()) {
            var b=JsonSerializer.Deserialize<TerritoryConstruction>(r.GetString(2))??throw new InvalidDataException("construction_invalid");
            if(b.BuildingId!=r.GetGuid(0)||b.AllianceId!=r.GetGuid(1)||b.WorldId!=world||b.SurfaceId!=surface||!TerritoryGeometry.Validate(b.ReservedRing,scope.Bounds).Valid)throw new InvalidDataException("construction_invalid");
            overlap|=rings.Any(x=>TerritoryFootprintOverlap.IntersectsValidated(x,b.ReservedRing));
        }
        using(var cmd=SqlTerritoryStore.Command(c,tx,"SELECT Kind FROM dbo.TerritorySpatialSourceVersions WITH(HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s AND Pending=1",("@w",world),("@s",surface)))
        using(var r=cmd.ExecuteReader())while(r.Read())if(!missing.Contains(r.GetString(0),StringComparer.Ordinal))missing.Add(r.GetString(0));
        bool complete=missing.Count==0&&unresolved.Count==0&&!overlap;
        var next=scope with {Revision=checked(scope.Revision+1),CompletePlacementCoverage=complete,Exclusions=overlap?scope.Exclusions:rings.ToArray()};
        var result=new SpatialCoverageResult(true,complete,overlap?"spatial_reservation_conflict":complete?"spatial_coverage_complete":"spatial_coverage_incomplete",next.Revision,missing.ToArray(),unresolved.ToArray(),rings.Count);
        using(var cmd=SqlTerritoryStore.Command(c,tx,"IF NOT EXISTS(SELECT 1 FROM dbo.TerritorySpatialManifests WHERE WorldId=@w AND SurfaceId=@s AND Version=@v) INSERT dbo.TerritorySpatialManifests VALUES(@w,@s,@v,@m); INSERT dbo.TerritorySpatialImports VALUES(@w,@s,@r,@v,@snapshot,@result); UPDATE dbo.TerritoryWorldScopes SET Revision=@r,ConfigJson=@j WHERE WorldId=@w AND SurfaceId=@s;",("@w",world),("@s",surface),("@r",next.Revision),("@v",manifest.Version),("@m",manifestJson),("@snapshot",JsonSerializer.Serialize(snapshots)),("@result",JsonSerializer.Serialize(result)),("@j",JsonSerializer.Serialize(next))))cmd.ExecuteNonQuery();
        beforeCommit?.Invoke();tx.Commit();return result;
    }
    // Call IN the producer's own SQL mutation transaction, before it commits its change.
    // No separate best-effort notification: rollback rolls back both source and invalidation.
    public void InvalidateSource(SqlConnection c,SqlTransaction tx,string kind,long expectedSourceRevision)
    {
        if(tx.Connection!=c||tx.IsolationLevel!=IsolationLevel.Serializable||string.IsNullOrWhiteSpace(kind)||kind.Length>128||expectedSourceRevision<0)throw new ArgumentException("spatial_producer_transaction_invalid");
        string key="territory:"+Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]{world,surface})));
        using(var cmd=SqlTerritoryStore.Command(c,tx,"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@k,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; SELECT @r;",("@k",key)))if(Convert.ToInt32(cmd.ExecuteScalar())<0)throw new InvalidOperationException("spatial_lock_unavailable");
        SqlTerritoryScope scope;
        using(var cmd=SqlTerritoryStore.Command(c,tx,"SELECT Revision,ConfigJson FROM dbo.TerritoryWorldScopes WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",("@w",world),("@s",surface)))
        using(var r=cmd.ExecuteReader()){if(!r.Read())throw new InvalidDataException("scope_missing");scope=JsonSerializer.Deserialize<SqlTerritoryScope>(r.GetString(1))??throw new InvalidDataException("scope_invalid");if(scope.Revision!=r.GetInt64(0)||scope.WorldId!=world||scope.SurfaceId!=surface)throw new InvalidDataException("scope_invalid");}
        using(var cmd=SqlTerritoryStore.Command(c,tx,"UPDATE dbo.TerritorySpatialSourceVersions SET Revision=Revision+1,Pending=1 WHERE WorldId=@w AND SurfaceId=@s AND Kind=@k AND Revision=@r; IF @@ROWCOUNT<>1 THROW 51006,'Spatial source revision conflict.',1;",("@w",world),("@s",surface),("@k",kind),("@r",expectedSourceRevision)))cmd.ExecuteNonQuery();
        var next=scope with {Revision=checked(scope.Revision+1),CompletePlacementCoverage=false};
        using var update=SqlTerritoryStore.Command(c,tx,"UPDATE dbo.TerritoryWorldScopes SET Revision=@r,ConfigJson=@j WHERE WorldId=@w AND SurfaceId=@s",("@r",next.Revision),("@j",JsonSerializer.Serialize(next)),("@w",world),("@s",surface));update.ExecuteNonQuery();
    }
}

public static class FloralSpatialSourceAdapter
{
    // Reads actual server catalogue identity/anchor fields. No coordinate/radius inference.
    // Missing geometry remains unresolved. The dictionary is supplied by an authoritative
    // geometry producer; Unity sprite bounds and readiness markers are not accepted sources.
    public static SpatialSourceSnapshot FromCatalogue(FloralSiteOptions catalogue,string world,string surface,long revision,IReadOnlyDictionary<string,TerritoryPoint[]> footprints)
    {
        catalogue.Validate();
        if(!catalogue.Enabled) return new("public-floral",revision,false,[]);
        var sites=catalogue.Sites.Where(s=>s.WorldId==world).ToArray();
        bool mapped=sites.All(s=>s.MapSurfaceId==surface&&!string.IsNullOrWhiteSpace(s.MapAnchorId));
        return new("public-floral",revision,mapped,sites.Select(s=>new SpatialObjectFootprint(s.SiteId,s.MapSurfaceId==surface&&footprints.TryGetValue(s.MapAnchorId,out var ring)?ring:null)).ToArray());
    }
}
