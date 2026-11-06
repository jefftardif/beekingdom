using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using BeeKingdom.Alliance.Territories;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.TerritorySqlCandidate;

// Trusted server infrastructure only. No player endpoint or implicit coverage promotion.
// Replaces the authoritative exclusion snapshot in one transaction, refuses reservations.
// Legacy spatial writers must route through this protocol before enabling live foundation.
public sealed class SqlTerritoryPlacementWriter(string connectionString,string world,string surface)
{
    public bool ReplaceExclusions(long expectedRevision,TerritoryPoint[][] exclusions)
    {
        using var c=new SqlConnection(connectionString);c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        string key="territory:"+Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]{world,surface})));
        using(var cmd=SqlTerritoryStore.Command(c,tx,"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; SELECT @r;",("@key",key)))
            if(Convert.ToInt32(cmd.ExecuteScalar())<0)throw new InvalidOperationException("territory_lock_unavailable");
        // Once source-tracked imports own the scope, this older unlabelled writer cannot
        // bypass inventories or claim coverage. All producers must use the importer.
        using(var cmd=SqlTerritoryStore.Command(c,tx,"SELECT 1 FROM dbo.TerritorySpatialImports WITH(HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",("@w",world),("@s",surface)))
            if(cmd.ExecuteScalar() is not null)return false;
        SqlTerritoryScope scope;
        using(var cmd=SqlTerritoryStore.Command(c,tx,"SELECT Revision,ConfigJson FROM dbo.TerritoryWorldScopes WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",("@w",world),("@s",surface)))
        using(var r=cmd.ExecuteReader()) {
            if(!r.Read())throw new InvalidOperationException("scope_required");
            scope=JsonSerializer.Deserialize<SqlTerritoryScope>(r.GetString(1))??throw new InvalidDataException("scope_invalid");
            if(scope.Revision!=r.GetInt64(0)||scope.WorldId!=world||scope.SurfaceId!=surface)throw new InvalidDataException("scope_invalid");
            if(expectedRevision!=scope.Revision)return false;
        }
        if(exclusions is null||exclusions.Any(x=>!TerritoryGeometry.Validate(x,scope.Bounds).Valid))throw new InvalidDataException("exclusions_invalid");
        using(var cmd=SqlTerritoryStore.Command(c,tx,"SELECT BuildingId,AllianceId,ConstructionJson FROM dbo.TerritoryBuildings WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",("@w",world),("@s",surface)))
        using(var r=cmd.ExecuteReader())while(r.Read()) {
            var building=JsonSerializer.Deserialize<TerritoryConstruction>(r.GetString(2))??throw new InvalidDataException("construction_invalid");
            if(building.BuildingId!=r.GetGuid(0)||building.AllianceId!=r.GetGuid(1)||building.WorldId!=world||building.SurfaceId!=surface||!TerritoryGeometry.Validate(building.ReservedRing,scope.Bounds).Valid)throw new InvalidDataException("construction_invalid");
            if(exclusions.Any(x=>TerritoryFootprintOverlap.IntersectsValidated(x,building.ReservedRing)))return false;
        }
        var next=scope with {Revision=checked(scope.Revision+1),Exclusions=exclusions};
        using(var cmd=SqlTerritoryStore.Command(c,tx,"UPDATE dbo.TerritoryWorldScopes SET Revision=@r,ConfigJson=@j WHERE WorldId=@w AND SurfaceId=@s",("@r",next.Revision),("@j",JsonSerializer.Serialize(next)),("@w",world),("@s",surface)))cmd.ExecuteNonQuery();
        tx.Commit();return true;
    }
}
