using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using BeeKingdom.Alliance.Territories;
using BeeKingdom.HiveOperations;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.TerritorySqlCandidate;

public sealed record TerritoryRefundReceipt(string ReceiptId,Guid PlayerId,Guid HiveId,Guid BuildingId,string ResourceKey,long Amount,long HiveRevision,string Reason);
public sealed record TerritoryCompensationResult(bool Applied,bool Replayed,string Code,TerritoryRefundReceipt? Receipt);

public sealed partial class SqlTerritoryStore
{
    // Server maintenance command only, no public route. Original owner is the refund recipient,
    // even after leaving the alliance. Input cannot change destination/currency/amount.
    // Per-payment transactions allow a failed batch to resume without re-crediting prior refunds.
    public TerritoryCompensationResult CompensateInvalidConstruction(Guid player,Guid hiveId,string originalRequest,Action? beforeCommit=null)
    {
        TerritoryCompensationResult No(string code)=>new(false,false,code,null);
        if(player==Guid.Empty||hiveId==Guid.Empty||string.IsNullOrWhiteSpace(originalRequest)||originalRequest.Length>128)return No("refund_request_invalid");
        using var conn=new SqlConnection(connectionString);conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        void Lock(string key) {using var cmd=Command(conn,tx,"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@k,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; SELECT @r;",("@k",key));if(Convert.ToInt32(cmd.ExecuteScalar())<0)throw new InvalidOperationException("refund_lock_unavailable");}
        Lock($"hive:{player:N}:{hiveId:N}");Lock("territory:"+Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]{world,surface}))));
        TerritoryHiveDebitReceipt audit;
        using(var cmd=Command(conn,tx,"SELECT ReceiptId,AuditJson FROM dbo.TerritoryHiveDebitReceipts WITH(HOLDLOCK) WHERE PlayerId=@p AND RequestId=@r",("@p",player),("@r",originalRequest)))
        using(var r=cmd.ExecuteReader()) {
            if(!r.Read())return No("refund_payment_missing");audit=D<TerritoryHiveDebitReceipt>(r.GetString(1));
            if(audit.ReceiptId!=r.GetString(0)||audit.PlayerId!=player||audit.HiveId!=hiveId||audit.RequestId!=originalRequest||audit.WorldId!=world||audit.SurfaceId!=surface||audit.Cost<=0||audit.WorkUnits<=0||audit.ResourceKey is not ("honey" or "pollen" or "wax"))throw new InvalidDataException("refund_audit_invalid");
        }
        using(var cmd=Command(conn,tx,"SELECT PlayerId,AllianceId,BuildingId,Units FROM dbo.TerritoryCommittedWork WITH(HOLDLOCK) WHERE Source=@s AND ReceiptId=@r",("@s",HiveWorkSource),("@r",audit.ReceiptId)))
        using(var r=cmd.ExecuteReader())if(!r.Read()||r.GetGuid(0)!=player||r.GetGuid(1)!=audit.AllianceId||r.GetGuid(2)!=audit.BuildingId||r.GetInt64(3)!=audit.WorkUnits)throw new InvalidDataException("refund_proof_invalid");
        using(var cmd=Command(conn,tx,"SELECT RefundJson FROM dbo.TerritoryHiveRefunds WITH(HOLDLOCK) WHERE ReceiptId=@r",("@r",audit.ReceiptId)))
            if(cmd.ExecuteScalar() is string stored) {
                var prior=D<TerritoryRefundReceipt>(stored);
                if(prior.ReceiptId!=audit.ReceiptId||prior.PlayerId!=player||prior.HiveId!=hiveId||prior.BuildingId!=audit.BuildingId||prior.ResourceKey!=audit.ResourceKey||prior.Amount!=audit.Cost)throw new InvalidDataException("refund_record_invalid");
                return new(false,true,"refund_replayed",prior);
            }
        TerritoryConstruction building;
        using(var cmd=Command(conn,tx,"SELECT AllianceId,WorldId,SurfaceId,ConstructionJson,CompletedJson FROM dbo.TerritoryBuildings WITH(UPDLOCK,HOLDLOCK) WHERE BuildingId=@b",("@b",audit.BuildingId)))
        using(var r=cmd.ExecuteReader()) {
            if(!r.Read())throw new InvalidDataException("refund_building_missing");
            building=D<TerritoryConstruction>(r.GetString(3));
            if(r.GetGuid(0)!=audit.AllianceId||r.GetString(1)!=world||r.GetString(2)!=surface||building.BuildingId!=audit.BuildingId||building.AllianceId!=audit.AllianceId||building.WorldId!=world||building.SurfaceId!=surface)throw new InvalidDataException("refund_building_invalid");
            if(!r.IsDBNull(4))return No("refund_completed_building_requires_review");
        }
        using(var cmd=Command(conn,tx,"SELECT COALESCE(SUM(Units),0) FROM dbo.TerritoryWorkConsumption WITH(HOLDLOCK) WHERE BuildingId=@b",("@b",building.BuildingId))) {
            long consumed=Convert.ToInt64(cmd.ExecuteScalar());
            if(!building.RequiredWork.HasValue||building.RequiredWork<=0||consumed<0||consumed>=building.RequiredWork)throw new InvalidDataException("refund_completion_inconsistent");
        }
        string? reason;
        using(var cmd=Command(conn,tx,"SELECT Reason FROM dbo.TerritoryConstructionCancellations WITH(HOLDLOCK) WHERE BuildingId=@b",("@b",building.BuildingId)))reason=cmd.ExecuteScalar() as string;
        if(reason is null) {
            SqlTerritoryScope scope;
            using(var cmd=Command(conn,tx,"SELECT Revision,ConfigJson FROM dbo.TerritoryWorldScopes WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",("@w",world),("@s",surface)))
            using(var r=cmd.ExecuteReader()) {
                if(!r.Read())return No("refund_authority_unavailable");scope=D<SqlTerritoryScope>(r.GetString(1));
                if(scope.Revision!=r.GetInt64(0)||scope.WorldId!=world||scope.SurfaceId!=surface)throw new InvalidDataException("refund_scope_invalid");
            }
            if(!scope.CompletePlacementCoverage||scope.Exclusions is null)return No("refund_authority_unavailable");
            var geometry=TerritoryGeometry.Validate(building.ReservedRing,scope.Bounds);
            if(!geometry.Valid||scope.Exclusions.Any(x=>!TerritoryGeometry.Validate(x,scope.Bounds).Valid))return No("refund_geometry_review_required");
            if(scope.Exclusions.Any(x=>TerritoryFootprintOverlap.IntersectsValidated(building.ReservedRing,x)))reason="placement_invalidated";
            else {
                int level;long revision;
                using(var cmd=Command(conn,tx,"SELECT Level,Revision FROM dbo.TerritoryProgressions WITH(HOLDLOCK) WHERE AllianceId=@a",("@a",audit.AllianceId)))
                using(var r=cmd.ExecuteReader()){if(!r.Read())return No("refund_authority_unavailable");level=r.GetInt32(0);revision=r.GetInt64(1);}
                var capacity=AllianceTerritoryCapacity.Evaluate(scope.AllianceSchedule,new(audit.AllianceId,world,surface,scope.AreaUnit,level,revision,0,geometry.Area!.Value));
                var hq=HeadquartersCapacity.EvaluateConstructionTarget(level,capacity.MaximumArea??0,1,scope.HeadquartersSchedule);
                if(capacity.MaximumArea.HasValue&&geometry.Area>capacity.MaximumArea||hq.MaximumArea.HasValue&&geometry.Area>hq.MaximumArea)reason="capacity_invalidated";
                else if(hq.Code=="headquarters_alliance_unlock_unmet")reason="alliance_prerequisite_lost";
            }
            if(reason is null)return No("refund_no_confirmed_invalidation");
            using var cancel=Command(conn,tx,"INSERT dbo.TerritoryConstructionCancellations VALUES(@b,@reason,@rev,SYSUTCDATETIME())",("@b",building.BuildingId),("@reason",reason),("@rev",scope.Revision));cancel.ExecuteNonQuery();
        }
        // Missing/deleted hive or insufficient storage leaves cancellation committed and refund pending.
        // Never create a replacement hive, truncate a refund or overflow capacity.
        var json=new JsonSerializerOptions(JsonSerializerDefaults.Web);PlayerHiveState state;
        using(var cmd=Command(conn,tx,"SELECT Revision,ModelVersion,StateJson FROM dbo.HivePlayerStates WITH(UPDLOCK,HOLDLOCK) WHERE PlayerId=@p AND HiveId=@h",("@p",player),("@h",hiveId)))
        using(var r=cmd.ExecuteReader()) {
            if(!r.Read()){r.Close();tx.Commit();return No("refund_pending_hive_unavailable");}
            state=JsonSerializer.Deserialize<PlayerHiveState>(r.GetString(2),json)??throw new InvalidDataException("refund_hive_invalid");
            if(state.PlayerId!=player||state.HiveId!=hiveId||state.Revision!=r.GetInt64(0)||state.ModelVersion!=r.GetInt32(1)||state.ModelVersion!=HiveStateMigrator.CurrentModelVersion)throw new InvalidDataException("refund_hive_invalid");
        }
        HiveStateMigrator.ToCurrent(state);
        if(!state.Resources.TryGetValue(audit.ResourceKey,out var balance)||balance.Amount<0||balance.Capacity<balance.Amount)throw new InvalidDataException("refund_balance_invalid");
        if(audit.Cost>balance.Capacity-balance.Amount){tx.Commit();return No("refund_pending_storage_capacity");}
        var resources=new Dictionary<string,ResourceBalance>(state.Resources,StringComparer.Ordinal){[audit.ResourceKey]=balance with {Amount=checked(balance.Amount+audit.Cost)}};
        var updated=state with {Revision=checked(state.Revision+1),Resources=resources};
        var refund=new TerritoryRefundReceipt(audit.ReceiptId,player,hiveId,building.BuildingId,audit.ResourceKey,audit.Cost,updated.Revision,reason);
        using(var cmd=Command(conn,tx,"UPDATE dbo.HivePlayerStates SET Revision=@next,StateJson=@j,UpdatedAtUtc=SYSUTCDATETIME() WHERE PlayerId=@p AND HiveId=@h AND Revision=@old; IF @@ROWCOUNT<>1 THROW 51073,'Hive revision changed.',1; INSERT dbo.TerritoryHiveRefunds VALUES(@r,@refund)",("@next",updated.Revision),("@j",JsonSerializer.Serialize(updated,json)),("@p",player),("@h",hiveId),("@old",state.Revision),("@r",audit.ReceiptId),("@refund",J(refund))))cmd.ExecuteNonQuery();
        beforeCommit?.Invoke();tx.Commit();return new(true,false,"refund_committed",refund);
    }
}
