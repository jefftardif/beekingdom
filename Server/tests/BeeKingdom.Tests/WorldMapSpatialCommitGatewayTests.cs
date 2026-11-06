using BeeKingdom.HiveOperations;
using BeeKingdom.MapPlacement;

namespace BeeKingdom.Tests;

/// <summary>No SQL connection or production writes: validate the gateway gates.</summary>
public sealed class WorldMapSpatialCommitGatewayTests
{
    private const string Revision="reviewed-test-20261106";
    private static readonly Guid ServerId=Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ActorId=Guid.Parse("00000000-0000-0000-0000-000000000111");

    private sealed class VerifiedCatalog : IWorldMapReviewedFootprintVerifier
    {
        public bool IsVerified(MapPlacementSnapshot p) =>
            p.Left.HasValue && p.Right.HasValue &&
            p.Top.HasValue && p.Bottom.HasValue &&
            p.Right.Value-p.Left.Value>=20 &&
            p.Bottom.Value-p.Top.Value>=20;
    }
    private sealed class NoCatalog : IWorldMapReviewedFootprintVerifier
    {
        public bool IsVerified(MapPlacementSnapshot p)=>false;
    }
    private static SemanticTerrain ApprovedTerrain()
    {
        var doc=new SemanticTerrainDocument {
            Schema="bk-semantic-terrain-v1",Revision=Revision,
            ManifestSha256=new string('a',64),
            CoordinateContract="wave6:origin=7,7;grid=50,50;core=512;gutter=2;y=down",
            Tiles=new[]{new SemanticTerrainTile {
                ChunkX=32,ChunkY=32,PngSha256=new string('b',64),
                Reviewed=true,ReviewEvidence="synthetic-test-only",
                Runs=new[]{new TerrainRun {Length=512*512,Kind="Land"}}
            }}
        };
        return SemanticTerrain.Load(doc,doc.ManifestSha256,Revision,
            new Dictionary<string,string>{{"32,32",new string('b',64)}},
            SemanticTerrain.ComputeContentSha256(doc));
    }
    private static TerrainPlacementCoverage Coverage()=>new(){
        WorldId="BK-DEMO-WORLD-WAVE6-LOCAL",SurfaceId="Wave6",
        CoordinateUnit="world",Revision=Revision,
        VerifiedGeometry=true,WorldBounds=new PlacementRect(16384,16384,16896,16896),
        EntityTerrainFamilies=new Dictionary<string,string>{{"hive:JEFF","Land"}}
    };
    private static MapPlacementSnapshot Placement(double x=16500, double y=16500,
                                                   double halfWidth=20) => new(){
        Kind="hive",EntityId="JEFF",PlacementId="player-hive-v1",
        WorldId="BK-DEMO-WORLD-WAVE6-LOCAL",SurfaceId="Wave6",
        CoordinateUnit="world",TerrainRevision=Revision,
        Revision=1,OccupationRevision=1,X=x,Y=y,
        Left=x-halfWidth,Top=y-halfWidth,
        Right=x+halfWidth,Bottom=y+halfWidth
    };
    private static WorldMapSpatialBatchCommand Request(MapPlacementSnapshot? p=null,
                                                       string key="request1") =>
        new(ServerId,ActorId,key,1,ApprovedTerrain(),Coverage(),
            new[]{new PlacementRequest {
                Kind="hive",EntityId="JEFF",
                Candidates=new[]{p??Placement()}
            }});

    [Test]
    public async Task DisabledGatewayMakesNoDatabaseConnection()
    {
        // If the gateway touched SQL this would fail to connect; it must
        // return before opening the non-existent database/host.
        var gateway=new SqlWorldMapSpatialCommitGateway(
            "Server=127.0.0.1,1;Database=unreachable;Connect Timeout=1",
            new VerifiedCatalog());
        var result=await gateway.CommitAsync(Request());
        Assert.Multiple(()=>{
            Assert.That(result.Committed,Is.False);
            Assert.That(result.Code,Is.EqualTo("server_spatial_feature_disabled"));
            Assert.That(result.OccupationRevision,Is.Zero);
        });
    }

    [Test]
    public void VerifiedFootprintAndScopedCoordinatesAcceptedByPureGate()
    {
        Assert.That(SqlWorldMapSpatialCommitGateway.Validate(Request(),
            new VerifiedCatalog()),Is.EqualTo("verified"));
    }

    [Test]
    public void UnverifiedFootprintIsRejectedEvenIfGeometryIsOtherwiseValid()
    {
        Assert.That(SqlWorldMapSpatialCommitGateway.Validate(Request(),
            new NoCatalog()),Is.EqualTo("unverified_server_footprint"));
        Assert.That(SqlWorldMapSpatialCommitGateway.Validate(
            Request(Placement(16500,16500,5)),new VerifiedCatalog()),
            Is.EqualTo("unverified_server_footprint"));
    }

    [Test]
    public void MissingCatalogNeverGainsImplicitApproval()
    {
        Assert.That(SqlWorldMapSpatialCommitGateway.Validate(Request(),null),
            Is.EqualTo("invalid_spatial_request"));
    }

    [Test]
    public void MismatchedTerrainVersionOrIdentityIsRejected()
    {
        var original=Request();
        var scope=Coverage();scope.Revision="stale";
        var mismatched=original with { Coverage=scope };
        Assert.That(SqlWorldMapSpatialCommitGateway.Validate(
            mismatched,new VerifiedCatalog()),Is.EqualTo("invalid_spatial_request"));
        var wrong=Placement(); wrong.WorldId="other-world";
        Assert.That(SqlWorldMapSpatialCommitGateway.Validate(
            Request(wrong),new VerifiedCatalog()),Is.EqualTo("unverified_server_footprint"));
    }

    [Test]
    public void ActorAndIdempotencyAreMandatory()
    {
        var r=Request();
        Assert.That(SqlWorldMapSpatialCommitGateway.Validate(
            r with { AuthenticatedActorPlayerId=Guid.Empty },new VerifiedCatalog()),
            Is.EqualTo("invalid_spatial_request"));
        Assert.That(SqlWorldMapSpatialCommitGateway.Validate(
            r with { IdempotencyKey="" },new VerifiedCatalog()),
            Is.EqualTo("invalid_spatial_request"));
    }

    [Test]
    public void DuplicateEntitiesInOneBatchAreRejectedBeforeSql()
    {
        var r=Request();
        var clone=r.Requests[0];
        var duplicate=r with { Requests=new[]{clone,clone} };
        Assert.That(SqlWorldMapSpatialCommitGateway.Validate(
            duplicate,new VerifiedCatalog()),Is.EqualTo("invalid_spatial_request"));
    }

    [Test]
    public void StablePayloadHashChangesWithCoordinatesAndActor()
    {
        var r=Request();
        var a=SqlWorldMapSpatialCommitGateway.PayloadHash(r,new[]{Placement()});
        var b=SqlWorldMapSpatialCommitGateway.PayloadHash(r,new[]{Placement()});
        var moved=SqlWorldMapSpatialCommitGateway.PayloadHash(
            r,new[]{Placement(16580,16500)});
        var otherActor=SqlWorldMapSpatialCommitGateway.PayloadHash(
            r with { AuthenticatedActorPlayerId=Guid.NewGuid() },new[]{Placement()});
        Assert.Multiple(()=>{
            Assert.That(a,Is.EqualTo(b));
            Assert.That(a.Length,Is.EqualTo(64));
            Assert.That(a,Is.Not.EqualTo(moved));
            Assert.That(a,Is.Not.EqualTo(otherActor));
        });
    }
}
