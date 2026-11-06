using System.Data;
using System.Text.Json;
using BeeKingdom.HiveOperations;
using Microsoft.Data.SqlClient;
namespace BeeKingdom.TerritorySqlCandidate;

public sealed record CanonicalHiveIdentityRequest(Guid PlayerId, Guid HiveId, Guid ColonyId,
    long ExpectedHiveRevision, long ExpectedColonyRevision, string EvidenceReference);
public sealed record CanonicalHiveMapPosition(Guid HiveId, Guid PlayerId, string WorldId,
    string SurfaceId, string CoordinateUnit, float X, float Y, long Revision);

public sealed partial class SqlHivePositionStore
{
    private static long LockCanonicalHive(SqlConnection c, SqlTransaction tx, Guid player, Guid hive)
    {
        if(player==Guid.Empty||hive==Guid.Empty) throw new InvalidOperationException("canonical_hive_not_owned");
        using(var cmd=SqlTerritoryStore.Command(c,tx,
            "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@k,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @r<0 THROW 51070,'Hive state lock unavailable.',1;",
            ("@k",$"hive:{player:N}:{hive:N}"))) cmd.ExecuteNonQuery();
        using var read=SqlTerritoryStore.Command(c,tx,
            "SELECT Revision,ModelVersion,StateJson FROM dbo.HivePlayerStates WITH(HOLDLOCK) WHERE PlayerId=@p AND HiveId=@h",("@p",player),("@h",hive));
        using var r=read.ExecuteReader();
        if(!r.Read()) throw new InvalidOperationException("canonical_hive_not_owned");
        var state=JsonSerializer.Deserialize<PlayerHiveState>(r.GetString(2),new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if(state is null||state.PlayerId!=player||state.HiveId!=hive||state.Revision!=r.GetInt64(0)||state.ModelVersion!=r.GetInt32(1))
            throw new InvalidDataException("canonical_hive_state_inconsistent");
        if(state.ModelVersion!=HiveStateMigrator.CurrentModelVersion) throw new InvalidOperationException("canonical_hive_migration_required");
        return state.Revision;
    }

    private void RequireCanonicalAssignment(SqlConnection c, SqlTransaction tx, Guid player)
    {
        using var cmd=SqlTerritoryStore.Command(c,tx,
            "SELECT COUNT(*) FROM dbo.TerritoryWorldAssignments WITH(HOLDLOCK) WHERE PlayerId=@p AND WorldId=@w AND SurfaceId=@s",
            ("@p",player),("@w",world),("@s",surface));
        if(Convert.ToInt32(cmd.ExecuteScalar())!=1) throw new InvalidOperationException("canonical_hive_world_assignment_required");
    }

    private void RequireCanonicalNotRevoked(SqlConnection c, SqlTransaction tx, Guid player, Guid hive)
    {
        using var cmd=SqlTerritoryStore.Command(c,tx,
            "SELECT COUNT(*) FROM dbo.HiveCanonicalIdentityRevocations WITH(HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s AND PlayerId=@p AND HiveId=@h",
            ("@w",world),("@s",surface),("@p",player),("@h",hive));
        if(Convert.ToInt32(cmd.ExecuteScalar())!=0) throw new InvalidOperationException("canonical_hive_binding_revoked");
    }

    // Trusted operator reconciliation only: evidence identifies a reviewed mapping source.
    // Ownership does not imply which colony to choose. No public mutation endpoint.
    public bool BindCanonicalHive(CanonicalHiveIdentityRequest request, Action? beforeCommit=null)
    {
        RequireEnabled();
        if(request is null||request.ColonyId==Guid.Empty||request.ExpectedHiveRevision<0||request.ExpectedColonyRevision<0||!Key(request.EvidenceReference,256))
            throw new InvalidDataException("canonical_hive_binding_invalid");
        using var c=new SqlConnection(connectionString);c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        long hiveRevision=LockCanonicalHive(c,tx,request.PlayerId,request.HiveId);
        var binding=ReadBinding(c,tx);RequireCanonicalAssignment(c,tx,request.PlayerId);
        RequireCanonicalNotRevoked(c,tx,request.PlayerId,request.HiveId);
        long colonyRevision;
        using(var cmd=SqlTerritoryStore.Command(c,tx,
            "SELECT Revision FROM dbo.Colonies WITH(HOLDLOCK) WHERE ColonyId=@c AND PlayerId=@p AND WorldId=@cw AND Status=1",
            ("@c",request.ColonyId),("@p",request.PlayerId),("@cw",binding.ColonyWorld)))
        {
            object? revision=cmd.ExecuteScalar();
            if(revision is null) throw new InvalidOperationException("canonical_colony_scope_denied");
            colonyRevision=Convert.ToInt64(revision);
        }
        using(var cmd=SqlTerritoryStore.Command(c,tx,
            "SELECT HiveId,ColonyId,EvidenceReference,PlayerId FROM dbo.HiveCanonicalIdentityBindings WITH(UPDLOCK,HOLDLOCK) WHERE WorldId=@w AND SurfaceId=@s AND (ColonyId=@c OR (PlayerId=@p AND HiveId=@h))",
            ("@w",world),("@s",surface),("@c",request.ColonyId),("@p",request.PlayerId),("@h",request.HiveId)))
        using(var r=cmd.ExecuteReader()) if(r.Read())
        {
            if(r.GetGuid(0)!=request.HiveId||r.GetGuid(1)!=request.ColonyId||r.GetString(2)!=request.EvidenceReference||r.GetGuid(3)!=request.PlayerId)
                throw new InvalidDataException("canonical_hive_binding_conflict");
            return false;
        }
        if(hiveRevision!=request.ExpectedHiveRevision||colonyRevision!=request.ExpectedColonyRevision)
            throw new InvalidDataException("canonical_hive_binding_revision_conflict");
        using(var cmd=SqlTerritoryStore.Command(c,tx,
            "INSERT dbo.HiveCanonicalIdentityBindings VALUES(@w,@s,@p,@h,@c,@hr,@cr,@e,SYSUTCDATETIME())",
            ("@w",world),("@s",surface),("@p",request.PlayerId),("@h",request.HiveId),("@c",request.ColonyId),
            ("@hr",hiveRevision),("@cr",colonyRevision),("@e",request.EvidenceReference)))cmd.ExecuteNonQuery();
        beforeCommit?.Invoke();tx.Commit();return true;
    }

    public CanonicalHiveMapPosition? ReadCanonicalOwn(Guid player,Guid hive)
    {
        RequireEnabled();
        using var c=new SqlConnection(connectionString);c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        LockCanonicalHive(c,tx,player,hive);
        var binding=ReadBinding(c,tx);RequireCanonicalAssignment(c,tx,player);
        RequireCanonicalNotRevoked(c,tx,player,hive);
        using var cmd=SqlTerritoryStore.Command(c,tx,
            """
            SELECT p.X,p.Y,p.Revision,c.PlayerId,c.WorldId,c.Status,p.PlayerId
            FROM dbo.HiveCanonicalIdentityBindings b WITH(HOLDLOCK)
            JOIN dbo.Colonies c WITH(HOLDLOCK) ON c.ColonyId=b.ColonyId
            LEFT JOIN dbo.HiveMapPositions p WITH(HOLDLOCK) ON p.ColonyId=b.ColonyId AND p.WorldId=b.WorldId AND p.SurfaceId=b.SurfaceId
            WHERE b.WorldId=@w AND b.SurfaceId=@s AND b.PlayerId=@p AND b.HiveId=@h
            """,("@w",world),("@s",surface),("@p",player),("@h",hive));
        using var r=cmd.ExecuteReader();if(!r.Read())return null;
        if(r.GetGuid(3)!=player||r.GetGuid(4)!=binding.ColonyWorld||r.GetInt32(5)!=1)throw new InvalidOperationException("canonical_colony_scope_denied");
        if(r.IsDBNull(0))return null;
        if(r.GetGuid(6)!=player)throw new InvalidOperationException("canonical_colony_scope_denied");
        return new(hive,player,world,surface,binding.Unit,r.GetFloat(0),r.GetFloat(1),r.GetInt64(2));
    }
}
