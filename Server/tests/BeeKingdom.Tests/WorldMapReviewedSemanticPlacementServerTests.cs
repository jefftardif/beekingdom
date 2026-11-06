using BeeKingdom.MapPlacement;

namespace BeeKingdom.Tests;

public sealed class WorldMapReviewedSemanticPlacementServerTests
{
    private const string World = "BK-DEMO-WORLD-WAVE6-LOCAL";
    private const string Surface = "Wave6";
    private const string Revision = "reviewed-20261106-v1";
    private const string Contract =
        "wave6:origin=7,7;grid=50,50;core=512;gutter=2;y=down";

    private static SemanticTerrainDocument Document(string surfaceClass = "Land")
    {
        return new SemanticTerrainDocument
        {
            Schema = "bk-semantic-terrain-v1",
            Revision = Revision,
            ManifestSha256 = new string('a', 64),
            CoordinateContract = Contract,
            Tiles =
            [
                new SemanticTerrainTile
                {
                    ChunkX = 32,
                    ChunkY = 32,
                    PngSha256 = new string('b', 64),
                    Reviewed = surfaceClass != "Unknown",
                    ReviewEvidence = surfaceClass != "Unknown"
                        ? "test-fixture:synthetic" : "",
                    Runs = [new TerrainRun
                    {
                        Length = 512 * 512, Kind = surfaceClass
                    }]
                }
            ]
        };
    }

    private static SemanticTerrain Terrain(string terrain = "Land")
    {
        SemanticTerrainDocument document = Document(terrain);
        return SemanticTerrain.Load(
            document, new string('a', 64), Revision,
            new Dictionary<string, string>
            {
                ["32,32"] = new string('b', 64)
            }, SemanticTerrain.ComputeContentSha256(document));
    }

    private static TerrainPlacementCoverage Scope(params (string Identity, string Family)[] families)
    {
        var byId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in families) byId.Add(item.Identity, item.Family);
        return new TerrainPlacementCoverage
        {
            WorldId = World,
            SurfaceId = Surface,
            CoordinateUnit = "world",
            Revision = Revision,
            VerifiedGeometry = true,
            WorldBounds = new PlacementRect(16384, 16384, 16896, 16896),
            EntityTerrainFamilies = byId
        };
    }

    private static MapPlacementSnapshot Candidate(string kind, string id,
        double x, double y, string suffix = "original")
    {
        return new MapPlacementSnapshot
        {
            PlacementId = id + ":" + suffix,
            EntityId = id,
            Kind = kind,
            WorldId = World,
            SurfaceId = Surface,
            CoordinateUnit = "world",
            Revision = 1,
            TerrainRevision = Revision,
            OccupationRevision = 1,
            X = x,
            Y = y,
            Left = x - 10,
            Top = y - 10,
            Right = x + 10,
            Bottom = y + 10
        };
    }

    private static PlacementRequest Request(MapPlacementSnapshot first,
        params MapPlacementSnapshot[] more) => new()
    {
        EntityId = first.EntityId,
        Kind = first.Kind,
        Candidates = (new[] { first }).Concat(more).ToArray()
    };

    private static PlacementPlan Plan(SemanticTerrain terrain,
        TerrainPlacementCoverage coverage,
        IReadOnlyList<MapPlacementSnapshot> fixedPlacements,
        IReadOnlyList<PlacementRequest> requests,
        IReadOnlyCollection<string>? historical = null) =>
            ReviewedSemanticPlacementPlanner.Build(
                terrain, coverage, fixedPlacements, requests,
                gap: 8, occupationRevision: 1, historicalPinned: historical ?? Array.Empty<string>());

    [Test]
    public void SharedServerAssemblyUsesSameSemanticTerrainContentDigestAsUnityAndPython()
    {
        Assert.That(SemanticTerrain.ComputeContentSha256(Document()),
            Is.EqualTo("0F868878BC946BF681765AE4885DC2E2AFB7406898D1B54453D3085875DCD43D"));
    }

    [Test]
    public void ServerPlannerAdmitsHiveOnlyInsideReviewedLand()
    {
        MapPlacementSnapshot hive = Candidate("hive", "player", 16500, 16500);
        var coverage = Scope(("hive:player", "Land"));
        PlacementPlan plan = Plan(Terrain(), coverage,
            [], [Request(hive)]);
        Assert.Multiple(() =>
        {
            Assert.That(plan.Code, Is.EqualTo("planned"));
            Assert.That(plan.Placements.Count, Is.EqualTo(1));
            Assert.That(plan.Placements[0].X, Is.EqualTo(hive.X));
        });
    }

    [Test]
    public void ServerPlannerDeniesWaterOnLandWithoutMovingResource()
    {
        MapPlacementSnapshot water = Candidate("resource", "water-1", 16500, 16500);
        var coverage = Scope(("resource:water-1", "Water"));
        PlacementPlan plan = Plan(Terrain(), coverage,
            [], [Request(water)]);
        Assert.Multiple(() =>
        {
            Assert.That(plan.Code, Is.EqualTo("no_admissible_distinct_placement"));
            Assert.That(plan.Placements, Is.Empty);
            Assert.That(water.X, Is.EqualTo(16500));
        });
    }

    [Test]
    public void ServerPlannerDeniesHiveOnRiverAndUnknownTiles()
    {
        var hive = Candidate("hive", "player", 16500, 16500);
        var coverage = Scope(("hive:player", "Land"));
        foreach (string kind in new[] { "River", "Lake", "Unknown" })
        {
            PlacementPlan plan = Plan(Terrain(kind), coverage,
                [], [Request(hive)]);
            Assert.That(plan.Placements, Is.Empty, kind);
        }
    }

    [Test]
    public void ServerPlannerPreservesPinnedExistingHiveAndSelectsSafeSpiderCandidate()
    {
        MapPlacementSnapshot hive = Candidate("hive", "player", 16500, 16500);
        MapPlacementSnapshot spiderNear = Candidate("bestiary", "spider",
            16502, 16503, "overlap");
        MapPlacementSnapshot spiderFar = Candidate("bestiary", "spider",
            16625, 16625, "candidate-approved");
        var coverage = Scope(("hive:player", "Land"),
            ("bestiary:spider", "Land"));
        PlacementPlan plan = Plan(Terrain(), coverage,
            [hive], [Request(spiderNear, spiderFar)]);
        Assert.Multiple(() =>
        {
            Assert.That(plan.Code, Is.EqualTo("planned"));
            Assert.That(plan.Placements.Count, Is.EqualTo(2));
            Assert.That(plan.Placements.Single(x => x.EntityId == "player").X,
                Is.EqualTo(16500));
            Assert.That(plan.Placements.Single(x => x.EntityId == "spider").X,
                Is.EqualTo(16625));
        });
    }

    [Test]
    public void ConflictHasNoPartialPlanOrGhostClaim()
    {
        MapPlacementSnapshot hive = Candidate("hive", "player", 16500, 16500);
        MapPlacementSnapshot spider = Candidate("bestiary", "spider", 16502, 16503);
        var coverage = Scope(("hive:player", "Land"),
            ("bestiary:spider", "Land"));
        PlacementPlan plan = Plan(Terrain(), coverage,
            [hive], [Request(spider)]);
        Assert.Multiple(() =>
        {
            Assert.That(plan.Code, Is.EqualTo("no_admissible_distinct_placement"));
            Assert.That(plan.Placements, Is.Empty);
            Assert.That(spider.X, Is.EqualTo(16502));
        });
    }

    [Test]
    public void OldWaterOnLandMustBeExplicitlyHistoricalAndDoesNotMove()
    {
        MapPlacementSnapshot water = Candidate("resource", "water-1", 16500, 16500);
        var coverage = Scope(("resource:water-1", "Water"));
        var denied = Plan(Terrain(), coverage, [water], []);
        var preserved = Plan(Terrain(), coverage, [water], [],
            ["resource:water-1"]);
        Assert.Multiple(() =>
        {
            Assert.That(denied.Code, Is.EqualTo("fixed_semantic_terrain_unverified"));
            Assert.That(denied.Placements, Is.Empty);
            Assert.That(preserved.Code, Is.EqualTo("planned"));
            Assert.That(preserved.Placements.Single().X, Is.EqualTo(16500));
            Assert.That(preserved.Placements.Single().Y, Is.EqualTo(16500));
        });
    }

    [Test]
    public void ServerRefusesUnreviewedOrWrongRevisionRaster()
    {
        MapPlacementSnapshot hive = Candidate("hive", "player", 16500, 16500);
        var coverage = Scope(("hive:player", "Land"));
        coverage.Revision = "stale";
        var denied = Plan(Terrain(), coverage, [], [Request(hive)]);
        Assert.That(denied.Code,
            Is.EqualTo("reviewed_semantic_terrain_missing_or_revision_mismatch"));

        SemanticTerrainDocument forged = Document("Land");
        forged.Tiles[0].Reviewed = false;
        forged.Tiles[0].ReviewEvidence = "";
        Assert.Throws<ArgumentException>(() => SemanticTerrain.Load(
            forged, new string('a',64), Revision,
            new Dictionary<string,string>{{"32,32",new string('b',64)}},
            SemanticTerrain.ComputeContentSha256(forged)));
    }
}