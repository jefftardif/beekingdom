using BeeKingdom.HiveOperations;
using BeeKingdom.MapPlacement;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BeeKingdom.Tests;

/// <summary>
/// True end-to-end SQL Server concurrency / replay test. Opt-in only, against
/// user-owned LocalDB + disposable BeeKingdomSpatialSandbox_* database.
/// NEVER run against server, Azure, SQLEXPRESS or the production game database.
/// </summary>
public sealed class WorldMapSpatialLocalDbIntegrationTests
{
    private const string Revision = "reviewed-localdb-qa-20261106";
    private const string World = "BK-DEMO-WORLD-WAVE6-LOCAL";
    private const string Surface = "Wave6";
    private const string Unit = "world";
    private static readonly Guid ActorA = Guid.Parse("11111111-2222-3333-4444-555555555551");
    private static readonly Guid ActorB = Guid.Parse("11111111-2222-3333-4444-555555555552");

    private sealed class ServerOnlyDimensions : IWorldMapReviewedFootprintVerifier
    {
        public bool IsVerified(MapPlacementSnapshot p) =>
            p.Kind == "hive" && p.EntityId is "H1" or "H2" or "H3" &&
            p.Left.HasValue && p.Right.HasValue && p.Top.HasValue && p.Bottom.HasValue &&
            Math.Abs(p.Right.Value - p.Left.Value - 24) < .000001 &&
            Math.Abs(p.Bottom.Value - p.Top.Value - 24) < .000001;
    }
    private static string Connection()
    {
        if(Environment.GetEnvironmentVariable("BK_ENABLE_DISPOSABLE_SPATIAL_QA") != "1")
            Assert.Ignore("Disposable SQL integration test requires explicit opt-in");
        var raw=Environment.GetEnvironmentVariable("BK_SPATIAL_SANDBOX_CONNECTION");
        if(string.IsNullOrWhiteSpace(raw)) Assert.Ignore("No disposable sandbox connection supplied");
        var builder=new SqlConnectionStringBuilder(raw!);
        if(!builder.DataSource.StartsWith("(localdb)\\BKSpatialQA",StringComparison.OrdinalIgnoreCase) ||
           !builder.InitialCatalog.StartsWith("BeeKingdomSpatialSandbox_",StringComparison.OrdinalIgnoreCase) ||
           builder.IntegratedSecurity != true)
            throw new InvalidOperationException("Refusing non-LocalDB/non-disposable SQL target");
        return builder.ConnectionString;
    }
    private static SemanticTerrain Terrain()
    {
        var document=new SemanticTerrainDocument
        {
            Schema="bk-semantic-terrain-v1",
            Revision=Revision,
            ManifestSha256=new string('a',64),
            CoordinateContract="wave6:origin=7,7;grid=50,50;core=512;gutter=2;y=down",
            Tiles=new[] {new SemanticTerrainTile
            {
                ChunkX=32,ChunkY=32,PngSha256=new string('b',64),
                Reviewed=true,ReviewEvidence="synthetic-fixture-only",
                Runs=new[] {new TerrainRun {Length=512*512,Kind="Land"}}
            }}
        };
        return SemanticTerrain.Load(document,document.ManifestSha256,Revision,
            new Dictionary<string,string>{{"32,32",new string('b',64)}},
            SemanticTerrain.ComputeContentSha256(document));
    }
    private static TerrainPlacementCoverage Scope(string id)=>new()
    {
        WorldId=World,SurfaceId=id,CoordinateUnit=Unit,Revision=Revision,
        VerifiedGeometry=true,
        WorldBounds=new PlacementRect(16384,16384,16896,16896),
        EntityTerrainFamilies=new Dictionary<string,string>
        {
            {"hive:H1","Land"},{"hive:H2","Land"},{"hive:H3","Land"}
        }
    };
    private static MapPlacementSnapshot Placement(string id,double x,double y,string surface)=>new()
    {
        Kind="hive",EntityId=id,PlacementId=id+"-qa",WorldId=World,
        SurfaceId=surface,CoordinateUnit=Unit,Revision=1,OccupationRevision=1,
        TerrainRevision=Revision,
        X=x,Y=y,Left=x-12,Top=y-12,Right=x+12,Bottom=y+12
    };
    private static WorldMapSpatialBatchCommand Request(
        Guid server,Guid actor,string surface,string identity,double x,double y,string key,
        SemanticTerrain terrain,long revision=1)=>new(
            server,actor,key,revision,terrain,Scope(surface),
            new[]{new PlacementRequest {
                Kind="hive",EntityId=identity,
                Candidates=new[]{Placement(identity,x,y,surface)}
            }});

    private static async Task SeedAsync(string connection,Guid server,string surface,string hash)
    {
        await using var c=new SqlConnection(connection);
        await c.OpenAsync();
        await using var sql=c.CreateCommand();
        sql.CommandText=@"IF DB_NAME() NOT LIKE N'BeeKingdomSpatialSandbox[_]%'
                             THROW 51290,'Not a disposable sandbox.',1;
                          INSERT dbo.WorldMapSpatialScopes
                          (GameServerId,WorldId,SurfaceId,TerrainRevision,
                           TerrainContentSha256,OccupationRevision,WritesEnabled)
                          VALUES(@gs,@world,@surface,@terrain,@sha,1,1)";
        sql.Parameters.Add("@gs",SqlDbType.UniqueIdentifier).Value=server;
        sql.Parameters.Add("@world",SqlDbType.NVarChar,128).Value=World;
        sql.Parameters.Add("@surface",SqlDbType.NVarChar,128).Value=surface;
        sql.Parameters.Add("@terrain",SqlDbType.NVarChar,64).Value=Revision;
        sql.Parameters.Add("@sha",SqlDbType.Char,64).Value=hash;
        await sql.ExecuteNonQueryAsync();
    }
    private static async Task<(long Revision,int Entities,int Receipts)> StateAsync(
        string connection,Guid server,string surface)
    {
        await using var c=new SqlConnection(connection);
        await c.OpenAsync();
        await using var q=c.CreateCommand();
        q.CommandText=@"SELECT s.OccupationRevision,
                       (SELECT COUNT(*) FROM dbo.WorldMapSpatialReservations p
                        WHERE p.GameServerId=s.GameServerId AND p.WorldId=s.WorldId
                          AND p.SurfaceId=s.SurfaceId),
                       (SELECT COUNT(*) FROM dbo.WorldMapSpatialReceipts r
                        WHERE r.GameServerId=s.GameServerId AND r.WorldId=s.WorldId
                          AND r.SurfaceId=s.SurfaceId)
                        FROM dbo.WorldMapSpatialScopes s WHERE
                        s.GameServerId=@gs AND s.WorldId=@world AND s.SurfaceId=@surface";
        q.Parameters.Add("@gs",SqlDbType.UniqueIdentifier).Value=server;
        q.Parameters.Add("@world",SqlDbType.NVarChar,128).Value=World;
        q.Parameters.Add("@surface",SqlDbType.NVarChar,128).Value=surface;
        await using var reader=await q.ExecuteReaderAsync();
        Assert.That(await reader.ReadAsync(),Is.True);
        return (reader.GetInt64(0),reader.GetInt32(1),reader.GetInt32(2));
    }

    [Test]
    public async Task TwoConcurrentPlayersCannotBothCommitOnStaleWorldRevision()
    {
        string connection=Connection();
        var terrain=Terrain();
        Guid server=Guid.NewGuid();
        string surface="QA-"+Guid.NewGuid().ToString("N");
        await SeedAsync(connection,server,surface,terrain.ContentSha256);
        var gateway=new SqlWorldMapSpatialCommitGateway(
            connection,new ServerOnlyDimensions(),enabledByServer:true);
        var first=Request(server,ActorA,surface,"H1",16500,16500,"first",terrain);
        var second=Request(server,ActorB,surface,"H2",16600,16600,"second",terrain);
        WorldMapSpatialBatchResult[] results=await Task.WhenAll(
            gateway.CommitAsync(first),gateway.CommitAsync(second));
        Assert.That(results.Count(x=>x.Committed),Is.EqualTo(1));
        Assert.That(results.Count(x=>!x.Committed),Is.EqualTo(1));
        var current=await StateAsync(connection,server,surface);
        Assert.Multiple(()=>{
            Assert.That(current.Revision,Is.EqualTo(2));
            Assert.That(current.Entities,Is.EqualTo(1));
            Assert.That(current.Receipts,Is.EqualTo(1));
        });
        // Exact idempotent replay must not increment world revision.
        var winner=results[0].Committed ? first : second;
        var replay=await gateway.CommitAsync(winner);
        Assert.That(replay.Committed,Is.True);
        Assert.That(replay.Code,Is.EqualTo("idempotent_replay"));
        Assert.That(await StateAsync(connection,server,surface),
            Is.EqualTo(current));
        // Reject overlap and wrong-owner move, without removing the winner.
        var loser=results[0].Committed ? second : first;
        var bad=loser with {
            ExpectedOccupationRevision=2,
            Requests=new [] {new PlacementRequest {
                Kind="hive",EntityId=loser.Requests[0].EntityId,
                Candidates=new[]{Placement(loser.Requests[0].EntityId,16500,16500,surface)}
            }}
        };
        // The winner's location may be 16500 or 16600; always overlap exactly.
        var winnerPos=winner.Requests[0].Candidates[0];
        bad=bad with {Requests=new [] {new PlacementRequest {
            Kind="hive",EntityId=loser.Requests[0].EntityId,
            Candidates=new[]{Placement(loser.Requests[0].EntityId,
                                          winnerPos.X!.Value,winnerPos.Y!.Value,surface)}
        }}};
        var overlap=await gateway.CommitAsync(bad);
        Assert.That(overlap.Committed,Is.False);
        Assert.That(await StateAsync(connection,server,surface),Is.EqualTo(current));
    }
}
