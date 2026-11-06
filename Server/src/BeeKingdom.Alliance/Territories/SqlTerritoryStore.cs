using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using BeeKingdom.Alliance.Territories;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.TerritorySqlCandidate;

// Candidate SQL backend; NOT registered in production. No SQL schema/config mutation on construction.
public sealed record SqlTerritoryScope(string WorldId, string SurfaceId, string AreaUnit, long Revision,
    TerritoryMapBounds Bounds, bool CompletePlacementCoverage, TerritoryPoint[][] Exclusions,
    TerritoryFoundationPolicy Policy, TerritoryCapacitySchedule AllianceSchedule,
    HeadquartersCapacitySchedule HeadquartersSchedule, long RequiredWork);

public sealed partial class SqlTerritoryStore(string connectionString, string world, string surface)
{
    static string J<T>(T value) => JsonSerializer.Serialize(value);
    static T D<T>(string value) => JsonSerializer.Deserialize<T>(value) ?? throw new InvalidDataException("sql_state_invalid");
    public static SqlCommand Command(SqlConnection connection, SqlTransaction? tx, string text, params (string,object)[] values)
    {
        var command = new SqlCommand(text, connection, tx) { CommandTimeout = 20 };
        foreach (var (name,value) in values) command.Parameters.AddWithValue(name,value);
        return command;
    }
    sealed class ScopeTransaction : IDisposable
    {
        public required SqlConnection Connection;
        public required SqlTransaction Tx;
        public required SqlTerritoryScope Scope;
        public required TerritoryFoundationContext Context;
        public long MembershipRevision;
        public void Dispose() { Tx.Dispose(); Connection.Dispose(); }
    }
    ScopeTransaction Begin(Guid player,Guid? hiveId=null)
    {
        var connection = new SqlConnection(connectionString); connection.Open();
        var tx = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            if(hiveId.HasValue) {
                using var hiveLock=Command(connection,tx,"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; SELECT @r;",("@key",$"hive:{player:N}:{hiveId.Value:N}"));
                if(Convert.ToInt32(hiveLock.ExecuteScalar())<0)throw new InvalidOperationException("hive_lock_unavailable");
            }
            // One lock per world/surface across processes. Existing SQL rows also held below;
            // external spatial writers MUST join this protocol before coverage may be asserted.
            string resource = "territory:" + Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]{world,surface})));
            using (var cmd=Command(connection,tx,"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; SELECT @r;",("@key",resource)))
                if(Convert.ToInt32(cmd.ExecuteScalar())<0) throw new InvalidOperationException("territory_lock_unavailable");
            Guid alliance; string role; long membershipRevision;
            using(var cmd=Command(connection,tx,"SELECT m.AllianceId,m.Role,m.Revision FROM dbo.AllianceMemberships m WITH(HOLDLOCK) JOIN dbo.Alliances a WITH(HOLDLOCK) ON a.AllianceId=m.AllianceId WHERE m.PlayerId=@p AND m.RemovedAtUtc IS NULL AND a.Status=N'Active'",("@p",player)))
            using(var r=cmd.ExecuteReader()) {
                if(!r.Read()) throw new InvalidOperationException("membership_required");
                alliance=r.GetGuid(0); role=r.GetString(1); membershipRevision=r.GetInt64(2);
                if(r.Read()) throw new InvalidDataException("ambiguous_membership");
            }
            using(var cmd=Command(connection,tx,"SELECT Revision FROM dbo.TerritoryWorldAssignments WITH(HOLDLOCK) WHERE PlayerId=@p AND WorldId=@w AND SurfaceId=@s",("@p",player),("@w",world),("@s",surface)))
                if(cmd.ExecuteScalar() is null) throw new InvalidOperationException("world_assignment_required");
            SqlTerritoryScope scope;
            using(var cmd=Command(connection,tx,"SELECT Revision,ConfigJson FROM dbo.TerritoryWorldScopes WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",("@w",world),("@s",surface)))
            using(var r=cmd.ExecuteReader()) {
                if(!r.Read()) throw new InvalidOperationException("scope_required");
                scope=D<SqlTerritoryScope>(r.GetString(1));
                if(scope.Revision!=r.GetInt64(0)||scope.WorldId!=world||scope.SurfaceId!=surface) throw new InvalidDataException("scope_mismatch");
            }
            if(!scope.CompletePlacementCoverage) throw new InvalidOperationException("placement_coverage_unavailable");
            if(scope.Exclusions is null || scope.Exclusions.Any(x=>!TerritoryGeometry.Validate(x,scope.Bounds).Valid)) throw new InvalidDataException("placement_invalid");
            int level; long revision; string source;
            using(var cmd=Command(connection,tx,"SELECT Level,Revision,Source FROM dbo.TerritoryProgressions WITH(HOLDLOCK) WHERE AllianceId=@a",("@a",alliance)))
            using(var r=cmd.ExecuteReader()) {
                if(!r.Read()) throw new InvalidOperationException("progression_required");
                level=r.GetInt32(0); revision=r.GetInt64(1); source=r.GetString(2);
            }
            var parsedRole=role switch { "Leader"=>TerritoryFoundationRole.Leader,"Officer"=>TerritoryFoundationRole.Officer,"Member"=>TerritoryFoundationRole.Member,_=>throw new InvalidDataException("role_invalid") };
            return new() {Connection=connection,Tx=tx,Scope=scope,MembershipRevision=membershipRevision,Context=new(player,alliance,parsedRole,true,true,world,surface,scope.AreaUnit,source,level,revision,scope.Revision,scope.Bounds,scope.AllianceSchedule,scope.HeadquartersSchedule)};
        }
        catch { tx.Dispose(); connection.Dispose(); throw; }
    }
    List<(TerritoryConstruction Construction,TerritoryAuthorityState? State)> Buildings(ScopeTransaction t)
    {
        using var cmd=Command(t.Connection,t.Tx,"SELECT ConstructionJson,CompletedJson,b.BuildingId,AllianceId,(SELECT COALESCE(SUM(Units),0) FROM dbo.TerritoryWorkConsumption c WITH(HOLDLOCK) WHERE c.BuildingId=b.BuildingId) FROM dbo.TerritoryBuildings b WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s",("@w",world),("@s",surface));
        using var r=cmd.ExecuteReader(); var result=new List<(TerritoryConstruction,TerritoryAuthorityState?)>();
        while(r.Read()) {
            var c=D<TerritoryConstruction>(r.GetString(0));
            if(c.BuildingId!=r.GetGuid(2)||c.AllianceId!=r.GetGuid(3)||c.PlayerId==Guid.Empty||!c.RequiredWork.HasValue||c.RequiredWork<=0||c.TargetLevel!=1||c.Operational||c.WorldId!=world||c.SurfaceId!=surface||!TerritoryGeometry.Validate(c.ReservedRing,t.Scope.Bounds).Valid||!TerritoryGeometry.ContainsValidated(c.ReservedRing,c.Position)) throw new InvalidDataException("building_geometry_invalid");
            var state=r.IsDBNull(1)?null:D<TerritoryAuthorityState>(r.GetString(1));
            long work=r.GetInt64(4);
            if(work<0||work>c.RequiredWork||(state is not null)!=(work==c.RequiredWork))throw new InvalidDataException("completion_work_inconsistent");
            if(state is not null) {
                TerritoryMapReader.Project(state,0,"validation-only");
                if(state.AllianceId!=c.AllianceId||state.WorldId!=world||state.SurfaceId!=surface||state.Headquarters.BuildingId!=c.BuildingId||state.Headquarters.Position!=c.Position||!state.Ring.SequenceEqual(c.ReservedRing)) throw new InvalidDataException("completed_scope_invalid");
            }
            result.Add((c,state));
        }
        return result;
    }
    static bool IsCancelled(ScopeTransaction t,Guid building) {
        using var cmd=Command(t.Connection,t.Tx,"SELECT 1 FROM dbo.TerritoryConstructionCancellations WITH(HOLDLOCK) WHERE BuildingId=@b",("@b",building));return cmd.ExecuteScalar() is not null;
    }
    public TerritoryFoundationResult Found(Guid player,string request,TerritoryPoint position,Action? beforeCommit=null)
    {
        TerritoryFoundationResult No(string code)=>new(false,false,code,null);
        if(player==Guid.Empty||string.IsNullOrWhiteSpace(request)||request.Length>128) return No("foundation_request_invalid");
        using var t=Begin(player);
        string hash=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[]{t.Context.AllianceId.ToString("N"),position.X.ToString("G29",CultureInfo.InvariantCulture),position.Y.ToString("G29",CultureInfo.InvariantCulture)})));
        var buildings=Buildings(t);
        using(var cmd=Command(t.Connection,t.Tx,"SELECT PayloadHash,BuildingId FROM dbo.TerritoryFoundationReceipts WHERE WorldId=@w AND SurfaceId=@s AND PlayerId=@p AND RequestId=@r",("@w",world),("@s",surface),("@p",player),("@r",request)))
        using(var r=cmd.ExecuteReader()) if(r.Read()) {
            var previous=buildings.Single(x=>x.Construction.BuildingId==r.GetGuid(1)).Construction;
            return r.GetString(0)==hash && previous.AllianceId==t.Context.AllianceId ? new(false,true,"foundation_replayed",previous):No("foundation_idempotency_conflict");
        }
        if(buildings.Any(x=>x.Construction.AllianceId==t.Context.AllianceId)) return No("foundation_already_exists");
        if(t.Scope.RequiredWork<=0) return No("construction_work_policy_unavailable");
        var plan=TerritoryFoundationPlanner.Prepare(t.Scope.Policy,t.Context,position,(ring,rev)=>rev==t.Scope.Revision
            && !t.Scope.Exclusions.Any(x=>TerritoryFootprintOverlap.IntersectsValidated(ring,x))
            && !buildings.Any(x=>TerritoryFootprintOverlap.IntersectsValidated(ring,x.Construction.ReservedRing)));
        if(!plan.Ready) return No(plan.Code);
        var construction=new TerritoryConstruction(Guid.NewGuid(),player,t.Context.AllianceId,world,surface,plan.BuildingBlueprintKey,plan.PolicyVersion,position,plan.ProposedRing!.ToArray(),t.Scope.Revision,1,false,DateTimeOffset.UtcNow,t.Scope.RequiredWork);
        using(var cmd=Command(t.Connection,t.Tx,"INSERT dbo.TerritoryBuildings(BuildingId,AllianceId,WorldId,SurfaceId,ConstructionJson) VALUES(@b,@a,@w,@s,@j); INSERT dbo.TerritoryFoundationReceipts VALUES(@w,@s,@p,@r,@h,@b);",("@b",construction.BuildingId),("@a",construction.AllianceId),("@w",world),("@s",surface),("@j",J(construction)),("@p",player),("@r",request),("@h",hash))) cmd.ExecuteNonQuery();
        beforeCommit?.Invoke(); // fault-injection seam, never supplied by an HTTP request.
        t.Tx.Commit(); return new(true,false,"foundation_construction_reserved",construction);
    }
    public TerritoryWorkResult Credit(Guid player,Guid building,string source,string receipt,Action? beforeCommit=null)
    {
        TerritoryWorkResult No(string code)=>new(false,false,code,0,0,null);
        if(player==Guid.Empty||building==Guid.Empty||string.IsNullOrWhiteSpace(source)||source.Length>128||string.IsNullOrWhiteSpace(receipt)||receipt.Length>128) return No("construction_request_invalid");
        using var t=Begin(player); var row=Buildings(t).SingleOrDefault(x=>x.Construction.BuildingId==building);
        var c=row.Construction;
        if(c is null||c.AllianceId!=t.Context.AllianceId) return No("construction_not_owned");
        if(IsCancelled(t,building))return No("construction_cancelled");
        long units;
        using(var cmd=Command(t.Connection,t.Tx,"SELECT PlayerId,AllianceId,BuildingId,Units FROM dbo.TerritoryCommittedWork WITH(HOLDLOCK) WHERE Source=@s AND ReceiptId=@r",("@s",source),("@r",receipt)))
        using(var r=cmd.ExecuteReader()) {
            if(!r.Read()) return No("work_receipt_missing");
            if(r.GetGuid(0)!=player||r.GetGuid(1)!=c.AllianceId||r.GetGuid(2)!=building) return No("work_receipt_mismatch");
            units=r.GetInt64(3);
        }
        long total;
        using(var cmd=Command(t.Connection,t.Tx,"SELECT COALESCE(SUM(Units),0) FROM dbo.TerritoryWorkConsumption WITH(UPDLOCK,HOLDLOCK) WHERE BuildingId=@b",("@b",building))) total=Convert.ToInt64(cmd.ExecuteScalar());
        long required=c.RequiredWork??0;
        using(var cmd=Command(t.Connection,t.Tx,"SELECT BuildingId,Units FROM dbo.TerritoryWorkConsumption WHERE Source=@s AND ReceiptId=@r",("@s",source),("@r",receipt)))
        using(var r=cmd.ExecuteReader()) if(r.Read()) {
            if(r.GetGuid(0)!=building||r.GetInt64(1)!=units) throw new InvalidDataException("consumption_receipt_mismatch");
            return new(false,true,"construction_work_replayed",total,required,row.State);
        }
        if(row.State is not null) return No("construction_already_completed");
        if(required<=0||units<=0||units>required-total) return No("construction_work_exceeds_remaining");
        if(t.Scope.Exclusions.Any(x=>TerritoryFootprintOverlap.IntersectsValidated(c.ReservedRing,x))) return No("construction_placement_changed");
        TerritoryAuthorityState? state=null;
        if(total+units==required) {
            var ctx=t.Context;
            state=new(Guid.NewGuid(),c.AllianceId,world,surface,ctx.AreaUnit,1,true,ctx.ProgressionSource,ctx.AllianceProgressionRevision,ctx.AllianceLevel,ctx.Bounds,c.ReservedRing,ctx.AllianceSchedule,new(building,1,1,c.Position,true),ctx.HeadquartersSchedule);
            TerritoryMapReader.Project(state,0,"validation-only");
            decimal area=TerritoryGeometry.Validate(state.Ring,state.Bounds).Area ?? throw new InvalidDataException("completed_geometry_invalid");
            var cap=AllianceTerritoryCapacity.Evaluate(ctx.AllianceSchedule,new(ctx.AllianceId,world,surface,ctx.AreaUnit,ctx.AllianceLevel,ctx.AllianceProgressionRevision,0,area));
            var hq=HeadquartersCapacity.Evaluate(ctx.AllianceLevel,cap.MaximumArea??0,state.Headquarters,ctx.HeadquartersSchedule);
            if(!cap.CanFit||!hq.Available||area>hq.MaximumArea) return No("construction_capacity_unavailable");
        }
        using(var cmd=Command(t.Connection,t.Tx,"INSERT dbo.TerritoryWorkConsumption VALUES(@s,@r,@b,@u); UPDATE dbo.TerritoryBuildings SET CompletedJson=@j WHERE BuildingId=@b;",("@s",source),("@r",receipt),("@b",building),("@u",units),("@j",state is null?DBNull.Value:J(state)))) cmd.ExecuteNonQuery();
        beforeCommit?.Invoke(); t.Tx.Commit();
        return new(true,false,state is null?"construction_work_recorded":"construction_completed",total+units,required,state);
    }
    public TerritoryRegistryRead Read(Guid player,string coordinateUnit)
    {
        using var t=Begin(player);
        var state=Buildings(t).SingleOrDefault(x=>x.Construction.AllianceId==t.Context.AllianceId).State;
        return new(t.Context.AllianceId,world,surface,t.MembershipRevision,TerritoryMapReader.Project(state,0,coordinateUnit));
    }
}
