using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeeKingdom.MapPlacement;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.HiveOperations;

/// <summary>
/// Only a server-controlled, independently reviewed icon + permanent caption
/// footprint catalog may implement this interface. No client rectangle is trusted.
/// A missing/unknown footprint always rejects the placement.
/// </summary>
public interface IWorldMapReviewedFootprintVerifier
{
    bool IsVerified(MapPlacementSnapshot footprint);
}

public sealed record WorldMapSpatialBatchCommand(
    Guid GameServerId, Guid AuthenticatedActorPlayerId,
    string IdempotencyKey, long ExpectedOccupationRevision,
    SemanticTerrain Terrain, TerrainPlacementCoverage Coverage,
    IReadOnlyList<PlacementRequest> Requests);

public sealed record WorldMapSpatialBatchResult(
    bool Committed, string Code, long OccupationRevision);

/// <summary>
/// Not registered in the production API. Both the explicit constructor flag
/// and dbo.WorldMapSpatialScopes.WritesEnabled must be turned on separately
/// after reviewed terrain, server footprint measurements and SQL QA.
/// Only this gateway may call the stored procedure in a future gameplay route.
/// </summary>
public sealed class SqlWorldMapSpatialCommitGateway
{
    private readonly string connectionString;
    private readonly IWorldMapReviewedFootprintVerifier footprints;
    private readonly bool enabledByServer;

    public SqlWorldMapSpatialCommitGateway(string connectionString,
        IWorldMapReviewedFootprintVerifier footprints,
        bool enabledByServer = false)
    {
        this.connectionString = connectionString ??
            throw new ArgumentNullException(nameof(connectionString));
        this.footprints = footprints ??
            throw new ArgumentNullException(nameof(footprints));
        this.enabledByServer = enabledByServer;
    }

    public static string Validate(WorldMapSpatialBatchCommand? command,
        IWorldMapReviewedFootprintVerifier? policy)
    {
        if(command is null || policy is null ||
           command.GameServerId == Guid.Empty ||
           command.AuthenticatedActorPlayerId == Guid.Empty ||
           command.Terrain is null || command.Coverage is null ||
           (command.Terrain.ContentSha256?.Length != 64 ||
             !command.Terrain.ContentSha256.All(Uri.IsHexDigit)) ||
           string.IsNullOrWhiteSpace(command.Coverage.WorldId) ||
           command.Coverage.WorldId.Length > 128 ||
           string.IsNullOrWhiteSpace(command.Coverage.SurfaceId) ||
           command.Coverage.SurfaceId.Length > 128 ||
           string.IsNullOrWhiteSpace(command.Coverage.CoordinateUnit) ||
           string.IsNullOrWhiteSpace(command.Terrain.Revision) ||
           command.Terrain.Revision.Length > 64 ||
           command.Coverage.Revision != command.Terrain.Revision ||
           !command.Coverage.VerifiedGeometry ||
           command.ExpectedOccupationRevision < 1 ||
           string.IsNullOrWhiteSpace(command.IdempotencyKey) ||
           command.IdempotencyKey.Length > 128 ||
           command.Requests is null ||
           command.Requests.Count is < 1 or > 100)
            return "invalid_spatial_request";

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach(var request in command.Requests)
        {
            if(request is null ||
               string.IsNullOrWhiteSpace(request.Kind) ||
               request.Kind.Length > 32 ||
               string.IsNullOrWhiteSpace(request.EntityId) ||
               request.EntityId.Length > 128 ||
               request.Candidates is null ||
               request.Candidates.Count is < 1 or > 256 ||
               !seen.Add(request.Kind + ":" + request.EntityId))
                return "invalid_spatial_request";

            foreach(var candidate in request.Candidates)
            {
                if(candidate is null || string.IsNullOrWhiteSpace(candidate.PlacementId) ||
                   candidate.PlacementId.Length > 128 ||
                   MapPlacementRules.Validate(candidate,request.Kind,request.EntityId,
                   command.Coverage.WorldId,command.Coverage.SurfaceId,
                   command.Coverage.CoordinateUnit)!="verified" ||
                   !policy.IsVerified(candidate))
                    return "unverified_server_footprint";
            }
        }
        return "verified";
    }

    /// <summary>
    /// Performs pure verified-terrain admission first, SQL final overlap
    /// admission second. SQL serializable+world mutex+CAS+receipt is the
    /// authoritative atomic commit, never the earlier in-memory plan.
    /// </summary>
    public async Task<WorldMapSpatialBatchResult> CommitAsync(
        WorldMapSpatialBatchCommand command, CancellationToken ct=default)
    {
        if(!enabledByServer)
            return new(false,"server_spatial_feature_disabled",0);
        string validation=Validate(command,footprints);
        if(validation!="verified")
            return new(false,validation,0);

        // No guessed proposals; input candidate size is checked against
        // a server-only footprint catalog, never trusted from a player.
        var plan=ReviewedSemanticPlacementPlanner.Build(
            command.Terrain,command.Coverage,
            Array.Empty<MapPlacementSnapshot>(),command.Requests,
            gap:14,occupationRevision:command.ExpectedOccupationRevision);
        if(plan.Code!="planned")
            return new(false,plan.Code,command.ExpectedOccupationRevision);

        var chosen=plan.Placements;
        if(chosen.Count!=command.Requests.Count)
            return new(false,"incomplete_spatial_plan",command.ExpectedOccupationRevision);

        // Store outward-rounded bounds so 6-decimal SQL conversion cannot
        // reduce clearance. Check that the server footprint still covers the
        // revised rectangle. Terrain admission is repeated with the rounded
        // box before passing the TVP to SQL.
        var data=new DataTable();
        data.Columns.Add("Kind",typeof(string));
        data.Columns.Add("EntityId",typeof(string));
        data.Columns.Add("PlacementId",typeof(string));
        data.Columns.Add("X",typeof(decimal));
        data.Columns.Add("Y",typeof(decimal));
        data.Columns.Add("BoxLeft",typeof(decimal));
        data.Columns.Add("BoxTop",typeof(decimal));
        data.Columns.Add("BoxRight",typeof(decimal));
        data.Columns.Add("BoxBottom",typeof(decimal));
        var sorted=new List<MapPlacementSnapshot>();
        foreach(var p in chosen.OrderBy(x=>x.Kind,StringComparer.Ordinal)
                               .ThenBy(x=>x.EntityId,StringComparer.Ordinal))
        {
            var box=Outward(p);
            string identity=p.Kind+":"+p.EntityId;
            string family=command.Coverage.TerrainFamily(p.Kind,p.EntityId);
            if((family!="Water" && family!="Land") ||
                !command.Terrain.AllowsNewFootprint(
                    MapPlacementRules.Bounds(box),14,family=="Water") ||
                !footprints.IsVerified(box))
                return new(false,"rounded_footprint_not_reviewed",
                    command.ExpectedOccupationRevision);
            sorted.Add(box);
            data.Rows.Add(box.Kind,box.EntityId,box.PlacementId,
                (decimal)box.X!.Value,(decimal)box.Y!.Value,
                (decimal)box.Left!.Value,(decimal)box.Top!.Value,
                (decimal)box.Right!.Value,(decimal)box.Bottom!.Value);
        }
        string hash=PayloadHash(command,sorted);
        await using var connection=new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var sql=new SqlCommand(
            "dbo.WorldMapCommitSpatialBatch",connection) {
                CommandType=CommandType.StoredProcedure,CommandTimeout=15
            };
        sql.Parameters.Add("@GameServerId",SqlDbType.UniqueIdentifier)
            .Value=command.GameServerId;
        sql.Parameters.Add("@ActorPlayerId",SqlDbType.UniqueIdentifier)
            .Value=command.AuthenticatedActorPlayerId;
        Text(sql,"@WorldId",command.Coverage.WorldId,128);
        Text(sql,"@SurfaceId",command.Coverage.SurfaceId,128);
        Text(sql,"@RequestId",command.IdempotencyKey,128);
        sql.Parameters.Add("@PayloadHash",SqlDbType.Char,64).Value=hash;
        sql.Parameters.Add("@ExpectedRevision",SqlDbType.BigInt)
            .Value=command.ExpectedOccupationRevision;
        Text(sql,"@TerrainRevision",command.Terrain.Revision,64);
        sql.Parameters.Add("@TerrainContentSha256",SqlDbType.Char,64)
            .Value=command.Terrain.ContentSha256;
        var batch=sql.Parameters.Add("@Items",SqlDbType.Structured);
        batch.TypeName="dbo.WorldMapSpatialPlacementBatch";
        batch.Value=data;
        try
        {
            await using var reader=await sql.ExecuteReaderAsync(ct);
            if(!await reader.ReadAsync(ct))
                return new(false,"spatial_database_missing_result",0);
            string status=reader.GetString(0);
            long revision=reader.GetInt64(1);
            return new(status is "committed" or "idempotent_replay",
                status,revision);
        }
        catch(SqlException error) when (error.Number>=51100 &&
                                        error.Number<=51113)
        {
            return new(false,"spatial_sql_rejected_"+error.Number,0);
        }
    }

    private static void Text(SqlCommand sql,string name,string value,int length)
    {
        sql.Parameters.Add(name,SqlDbType.NVarChar,length).Value=value;
    }
    private static MapPlacementSnapshot Outward(MapPlacementSnapshot input)
    {
        var p=MapPlacementRules.Copy(input);
        static double Lo(double x)=>Math.Floor(x*1e6)/1e6;
        static double Hi(double x)=>Math.Ceiling(x*1e6)/1e6;
        p.Left=Lo(p.Left!.Value);p.Top=Lo(p.Top!.Value);
        p.Right=Hi(p.Right!.Value);p.Bottom=Hi(p.Bottom!.Value);
        p.X=Math.Round(p.X!.Value,6,MidpointRounding.AwayFromZero);
        p.Y=Math.Round(p.Y!.Value,6,MidpointRounding.AwayFromZero);
        return p;
    }
    public static string PayloadHash(WorldMapSpatialBatchCommand request,
                                      IReadOnlyList<MapPlacementSnapshot> accepted)
    {
        string json=JsonSerializer.Serialize(new {
            request.GameServerId,request.AuthenticatedActorPlayerId,
            request.ExpectedOccupationRevision,
            request.Coverage.WorldId,request.Coverage.SurfaceId,
            request.Terrain.Revision,request.Terrain.ContentSha256,
            Candidates=accepted.Select(p=>new {
                p.Kind,p.EntityId,p.PlacementId,p.X,p.Y,
                p.Left,p.Top,p.Right,p.Bottom
            }).ToArray()
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
