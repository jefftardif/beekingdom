using System.Text.Json;
using System.Security.Cryptography;
using BeeKingdom.Alliance.Territories;
using BeeKingdom.HiveOperations;

namespace BeeKingdom.TerritorySqlCandidate;

// No default currency/price. Server-only injected policy; no endpoint registered.
public sealed record TerritoryHiveWorkPolicy(bool Enabled,string Version,string ResourceKey,long Cost,long WorkUnits);
public sealed record TerritoryHiveDebitReceipt(Guid PlayerId,Guid HiveId,Guid BuildingId,Guid AllianceId,
    string WorldId,string SurfaceId,string RequestId,string ReceiptId,string PolicyVersion,
    string ResourceKey,long Cost,long WorkUnits,long HiveRevision);
public sealed record TerritoryHiveDebitResult(bool Applied,bool Replayed,string Code,TerritoryHiveDebitReceipt? Receipt);

public sealed partial class SqlTerritoryStore
{
    public const string HiveWorkSource="hive-resource-work-v1";
    public TerritoryHiveDebitResult ContributeFromHive(Guid player,Guid hiveId,Guid buildingId,string requestId,
        TerritoryHiveWorkPolicy? policy,Action? beforeCommit=null)
    {
        TerritoryHiveDebitResult No(string code)=>new(false,false,code,null);
        if(player==Guid.Empty||hiveId==Guid.Empty||buildingId==Guid.Empty||string.IsNullOrWhiteSpace(requestId)||requestId.Length>128)return No("hive_contribution_request_invalid");
        // Canonical existing hive lock FIRST, then territorial scope. Territory commands never acquire hive afterward.
        using var t=Begin(player,hiveId);
        TerritoryHiveDebitReceipt? prior=null;
        using(var cmd=Command(t.Connection,t.Tx,"SELECT AuditJson,ReceiptId FROM dbo.TerritoryHiveDebitReceipts WITH(HOLDLOCK) WHERE PlayerId=@p AND RequestId=@r",("@p",player),("@r",requestId)))
        using(var r=cmd.ExecuteReader())if(r.Read()) {
            prior=D<TerritoryHiveDebitReceipt>(r.GetString(0));
            if(prior.ReceiptId!=r.GetString(1)||prior.RequestId!=requestId||prior.Cost<=0||prior.WorkUnits<=0)throw new InvalidDataException("hive_debit_audit_invalid");
        }
        if(prior is not null) {
            if(prior.PlayerId!=player||prior.HiveId!=hiveId||prior.BuildingId!=buildingId||prior.AllianceId!=t.Context.AllianceId||prior.WorldId!=world||prior.SurfaceId!=surface)return No("hive_contribution_idempotency_conflict");
            using var cmd=Command(t.Connection,t.Tx,"SELECT PlayerId,AllianceId,BuildingId,Units FROM dbo.TerritoryCommittedWork WITH(HOLDLOCK) WHERE Source=@source AND ReceiptId=@receipt",("@source",HiveWorkSource),("@receipt",prior.ReceiptId));
            using var r=cmd.ExecuteReader();
            if(!r.Read()||r.GetGuid(0)!=player||r.GetGuid(1)!=prior.AllianceId||r.GetGuid(2)!=buildingId||r.GetInt64(3)!=prior.WorkUnits)throw new InvalidDataException("hive_debit_proof_invalid");
            return new(false,true,"hive_contribution_replayed",prior);
        }
        if(policy is null||!policy.Enabled)return No("hive_contribution_disabled");
        if(string.IsNullOrWhiteSpace(policy.Version)||policy.Version.Length>128||policy.ResourceKey is not ("honey" or "pollen" or "wax")||policy.Cost<=0||policy.WorkUnits<=0)return No("hive_contribution_policy_invalid");
        var building=Buildings(t).SingleOrDefault(x=>x.Construction.BuildingId==buildingId);
        if(building.Construction is null||building.Construction.AllianceId!=t.Context.AllianceId)return No("construction_not_owned");
        if(IsCancelled(t,buildingId))return No("construction_cancelled");
        if(building.State is not null)return No("construction_already_completed");
        var context=t.Context;
        if(t.Scope.Exclusions.Any(x=>TerritoryFootprintOverlap.IntersectsValidated(building.Construction.ReservedRing,x)))return No("construction_placement_changed");
        decimal area=TerritoryGeometry.Validate(building.Construction.ReservedRing,context.Bounds).Area??throw new InvalidDataException("construction_geometry_invalid");
        var capacity=AllianceTerritoryCapacity.Evaluate(context.AllianceSchedule,new(context.AllianceId,world,surface,context.AreaUnit,context.AllianceLevel,context.AllianceProgressionRevision,0,area));
        var headquarters=HeadquartersCapacity.EvaluateConstructionTarget(context.AllianceLevel,capacity.MaximumArea??0,1,context.HeadquartersSchedule);
        if(!capacity.CanFit||!headquarters.Available||area>headquarters.MaximumArea)return No("construction_capacity_unavailable");
        long committed;
        using(var cmd=Command(t.Connection,t.Tx,"SELECT COALESCE(SUM(Units),0) FROM dbo.TerritoryCommittedWork WITH(UPDLOCK,HOLDLOCK) WHERE BuildingId=@b",("@b",buildingId)))committed=Convert.ToInt64(cmd.ExecuteScalar());
        if(policy.WorkUnits>building.Construction.RequiredWork-committed)return No("construction_work_already_funded");
        // Include unconsumed proofs: a crash between debit/proof and Credit cannot charge beyond the target.
        var json=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        PlayerHiveState state;
        long rowRevision;
        using(var cmd=Command(t.Connection,t.Tx,"SELECT Revision,StateJson,ModelVersion FROM dbo.HivePlayerStates WITH(UPDLOCK,HOLDLOCK) WHERE PlayerId=@p AND HiveId=@h",("@p",player),("@h",hiveId)))
        using(var r=cmd.ExecuteReader()) {
            if(!r.Read())return No("hive_not_owned");
            rowRevision=r.GetInt64(0);state=JsonSerializer.Deserialize<PlayerHiveState>(r.GetString(1),json)??throw new InvalidDataException("hive_state_invalid");
            if(state.ModelVersion!=r.GetInt32(2))throw new InvalidDataException("hive_model_version_mismatch");
        }
        if(state.PlayerId!=player||state.HiveId!=hiveId||state.Revision!=rowRevision)throw new InvalidDataException("hive_state_identity_invalid");
        if(state.ModelVersion!=HiveStateMigrator.CurrentModelVersion)return No("hive_migration_required");
        HiveStateMigrator.ToCurrent(state); // Validation only; no implicit migration/default persistence here.
        if(state.Resources is null||!state.Resources.TryGetValue(policy.ResourceKey,out var balance)||balance.Amount<policy.Cost)return No("hive_resources_insufficient");
        if(balance.Amount<0||balance.Capacity<0||balance.Amount>balance.Capacity)throw new InvalidDataException("hive_balance_invalid");
        var resources=new Dictionary<string,ResourceBalance>(state.Resources,StringComparer.Ordinal){[policy.ResourceKey]=balance with {Amount=balance.Amount-policy.Cost}};
        var updated=state with {Revision=checked(state.Revision+1),Resources=resources};
        string receiptId=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]{player.ToString("N"),requestId})));
        var audit=new TerritoryHiveDebitReceipt(player,hiveId,buildingId,t.Context.AllianceId,world,surface,requestId,receiptId,policy.Version,policy.ResourceKey,policy.Cost,policy.WorkUnits,updated.Revision);
        using(var cmd=Command(t.Connection,t.Tx,"UPDATE dbo.HivePlayerStates SET Revision=@next,StateJson=@j,UpdatedAtUtc=SYSUTCDATETIME() WHERE PlayerId=@p AND HiveId=@h AND Revision=@old; IF @@ROWCOUNT<>1 THROW 51073,'Hive revision changed.',1; INSERT dbo.TerritoryCommittedWork VALUES(@source,@receipt,@p,@a,@b,@units); INSERT dbo.TerritoryHiveDebitReceipts VALUES(@p,@request,@receipt,@audit);",("@next",updated.Revision),("@j",JsonSerializer.Serialize(updated,json)),("@p",player),("@h",hiveId),("@old",state.Revision),("@source",HiveWorkSource),("@receipt",receiptId),("@a",t.Context.AllianceId),("@b",buildingId),("@units",policy.WorkUnits),("@request",requestId),("@audit",J(audit))))cmd.ExecuteNonQuery();
        beforeCommit?.Invoke();t.Tx.Commit();return new(true,false,"hive_contribution_committed",audit);
    }
}
