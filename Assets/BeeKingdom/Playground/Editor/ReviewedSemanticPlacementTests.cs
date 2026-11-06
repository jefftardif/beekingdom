using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.MapPlacement;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class ReviewedSemanticPlacementTests
    {
        private const string Contract = "wave6:origin=7,7;grid=50,50;core=512;gutter=2;y=down";
        private const string Revision = "reviewed-20261106-v1";
        private static string Manifest => new string('a',64);
        private static string PngHash => new string('b',64);

        private static SemanticTerrainDocument Document(string kind="Land",
            bool reviewed=true)
        {
            return new SemanticTerrainDocument
            {
                Schema="bk-semantic-terrain-v1",
                Revision=Revision,
                ManifestSha256=Manifest,
                CoordinateContract=Contract,
                Tiles=new[] {
                    new SemanticTerrainTile {
                        ChunkX=32,ChunkY=32,
                        PngSha256=PngHash,
                        Reviewed=reviewed,
                        ReviewEvidence=reviewed ? "test-fixture:synthetic" : null,
                        Runs=new[] {new TerrainRun {Length=512*512,Kind=kind}}
                    }
                }
            };
        }

        private static SemanticTerrain Reviewed(string kind="Land")
        {
            var doc=Document(kind);
            return SemanticTerrain.Load(doc,Manifest,Revision,
                new Dictionary<string,string> {{"32,32",PngHash}},
                SemanticTerrain.ComputeContentSha256(doc));
        }

        private static TerrainPlacementCoverage Scope(params string[] families)
        {
            var map=new Dictionary<string,string>(StringComparer.Ordinal);
            for(int i=0;i<families.Length;i+=2)
                map.Add(families[i],families[i+1]);
            return new TerrainPlacementCoverage
            {
                WorldId="BK-DEMO-WORLD-WAVE6-LOCAL",
                SurfaceId="Wave6",
                CoordinateUnit="world",
                Revision=Revision,
                VerifiedGeometry=true,
                WorldBounds=new PlacementRect(16384,16384,16896,16896),
                EntityTerrainFamilies=map
            };
        }
        private static MapPlacementSnapshot Candidate(string kind,string id,
            double x,double y,string placementId=null)
        {
            return new MapPlacementSnapshot
            {
                PlacementId=placementId??id+"-1",
                Kind=kind, EntityId=id,
                WorldId="BK-DEMO-WORLD-WAVE6-LOCAL",
                SurfaceId="Wave6",
                CoordinateUnit="world",
                Revision=1,OccupationRevision=1,
                TerrainRevision=Revision,
                X=x,Y=y,Left=x-10,Top=y-10,Right=x+10,Bottom=y+10
            };
        }
        private static PlacementRequest Request(MapPlacementSnapshot first,
            params MapPlacementSnapshot[] other)
        {
            return new PlacementRequest
            {
                EntityId=first.EntityId,Kind=first.Kind,
                Candidates=new[] {first}.Concat(other).ToArray()
            };
        }
        private static PlacementPlan Build(SemanticTerrain terrain,
            TerrainPlacementCoverage scope,params PlacementRequest[] requests)
        {
            return ReviewedSemanticPlacementPlanner.Build(
                terrain,scope,Array.Empty<MapPlacementSnapshot>(),requests,8,1);
        }

        [Test]
        public void ReviewedTerrainDigestMatchesIndependentPythonExporter()
        {
            // Independent Python promoter must use the exact BinaryWriter
            // serialization used by the Unity semantic loader.
            Assert.That(SemanticTerrain.ComputeContentSha256(Document()),
                Is.EqualTo("0F868878BC946BF681765AE4885DC2E2AFB7406898D1B54453D3085875DCD43D"));
        }
        [Test]
        public void ReviewedLandAllowsLandHive()
        {
            var p=Candidate("hive","H1",16500,16500);
            var plan=Build(Reviewed(),Scope("hive:H1","Land"),Request(p));
            Assert.That(plan.Code,Is.EqualTo("planned"));
            Assert.That(plan.Placements.Count,Is.EqualTo(1));
            Assert.That(plan.Placements[0].X,Is.EqualTo(16500));
        }

        [Test]
        public void WaterResourceNeverSpawnsOnReviewedLand()
        {
            var p=Candidate("resource","water-1",16500,16500);
            var plan=Build(Reviewed(),Scope("resource:water-1","Water"),Request(p));
            Assert.That(plan.Code,Is.EqualTo("no_admissible_distinct_placement"));
            Assert.That(plan.Placements,Is.Empty);
        }

        [Test]
        public void LandHiveNeverSpawnsOnReviewedWater()
        {
            var p=Candidate("hive","H1",16500,16500);
            var plan=Build(Reviewed("River"),Scope("hive:H1","Land"),Request(p));
            Assert.That(plan.Code,Is.EqualTo("no_admissible_distinct_placement"));
        }

        [Test]
        public void RiverAndLakeAllowWaterResources()
        {
            foreach(string kind in new[]{"River","Lake"})
            {
                var p=Candidate("resource","W1",16500,16500);
                var plan=Build(Reviewed(kind),Scope("resource:W1","Water"),Request(p));
                Assert.That(plan.Code,Is.EqualTo("planned"),kind);
            }
        }

        [Test]
        public void UnknownCannotAdmitNewEntityOfEitherFamily()
        {
            var terrain=Reviewed("Unknown");
            Assert.That(Build(terrain,Scope("hive:H1","Land"),
                Request(Candidate("hive","H1",16500,16500))).Placements,Is.Empty);
            Assert.That(Build(terrain,Scope("resource:W1","Water"),
                Request(Candidate("resource","W1",16500,16500))).Placements,Is.Empty);
        }

        [Test]
        public void UnreviewedNonUnknownAnnotationCannotLoad()
        {
            var doc=Document("Land",reviewed:false);
            Assert.Throws<ArgumentException>(()=>SemanticTerrain.Load(
                doc,Manifest,Revision,
                new Dictionary<string,string>{{"32,32",PngHash}},
                SemanticTerrain.ComputeContentSha256(doc)));
        }

        [Test]
        public void WrongPngHashCannotLoadEvenWithReviewLabel()
        {
            var doc=Document();
            Assert.Throws<ArgumentException>(()=>SemanticTerrain.Load(
                doc,Manifest,Revision,
                new Dictionary<string,string>{{"32,32",new string('f',64)}},
                SemanticTerrain.ComputeContentSha256(doc)));
        }

        [Test]
        public void ContentRevisionOrHashMismatchFailsClosed()
        {
            var terrain=Reviewed();
            var scope=Scope("hive:H1","Land");
            scope.Revision="stale-revision";
            var rejected=Build(terrain,scope,
                Request(Candidate("hive","H1",16500,16500)));
            Assert.That(rejected.Code,
                Is.EqualTo("reviewed_semantic_terrain_missing_or_revision_mismatch"));
            var doc=Document();
            Assert.Throws<ArgumentException>(()=>SemanticTerrain.Load(
                doc,Manifest,Revision,
                new Dictionary<string,string>{{"32,32",PngHash}},
                new string('0',64)));
        }

        [Test]
        public void AbsentTerrainOrUnreviewedScopeIsRejected()
        {
            var request=Request(Candidate("hive","H1",16500,16500));
            Assert.That(Build(null,Scope("hive:H1","Land"),request).Placements,Is.Empty);
            var scope=Scope("hive:H1","Land");
            scope.VerifiedGeometry=false;
            Assert.That(Build(Reviewed(),scope,request).Placements,Is.Empty);
        }

        [Test]
        public void EveryPlacementNeedsExplicitWaterOrLandClassification()
        {
            var p=Candidate("resource","unknown",16500,16500);
            var plan=Build(Reviewed(),Scope(),Request(p));
            Assert.That(plan.Code,Is.EqualTo("entity_terrain_family_not_certified"));
        }

        [Test]
        public void RejectingOverlappingBatchIsAtomicAndDoesNotMutateInputs()
        {
            var a=Candidate("hive","H1",16500,16500);
            var b=Candidate("bestiary","spider",16503,16504);
            var scope=Scope("hive:H1","Land","bestiary:spider","Land");
            var plan=Build(Reviewed(),scope,Request(a),Request(b));
            Assert.That(plan.Code,Is.EqualTo("no_admissible_distinct_placement"));
            Assert.That(plan.Placements,Is.Empty);
            Assert.That(b.X,Is.EqualTo(16503));
            Assert.That(a.X,Is.EqualTo(16500));
        }

        [Test]
        public void ApprovedAlternativeCandidateRespectsIconAndCaptionGap()
        {
            var a=Candidate("hive","H1",16500,16500);
            var first=Candidate("bestiary","spider",16503,16504,"spider-near");
            var alternate=Candidate("bestiary","spider",16600,16600,"spider-reviewed");
            var scope=Scope("hive:H1","Land","bestiary:spider","Land");
            // The existing player's hive must be a pinned reservation, not
            // a new contender competing with a freshly spawned enemy.
            var plan=ReviewedSemanticPlacementPlanner.Build(Reviewed(),scope,
                new[] {a},new[] {Request(first,alternate)},8,1);
            Assert.That(plan.Code,Is.EqualTo("planned"));
            Assert.That(plan.Placements.Count,Is.EqualTo(2));
            Assert.That(plan.Placements.Single(x=>x.EntityId=="spider").X,Is.EqualTo(16600));
        }

        [Test]
        public void MissingNeighbourTileIsNotTreatedAsLand()
        {
            var a=Candidate("hive","edge",16890,16500);
            var plan=Build(Reviewed(),Scope("hive:edge","Land"),Request(a));
            Assert.That(plan.Placements,Is.Empty);
        }

        [Test]
        public void ExistingUnverifiedFixedPlacementCannotGetTerrainApproval()
        {
            // A legacy water resource on certified land must never gain a
            // "verified" placement simply because a saved rectangle exists.
            var oldWater=Candidate("resource","waterLegacy",16500,16500);
            var scope=Scope("resource:waterLegacy","Water");
            var plan=ReviewedSemanticPlacementPlanner.Build(Reviewed(),scope,
                new[]{oldWater},Array.Empty<PlacementRequest>(),8,1);
            Assert.That(plan.Code,Is.EqualTo("fixed_semantic_terrain_unverified"));
            Assert.That(plan.Placements,Is.Empty);
            Assert.That(oldWater.X,Is.EqualTo(16500));
        }

        [Test]
        public void ExplicitlyPinnedHistoricalPositionIsRetainedButNeverRelocated()
        {
            var oldWater=Candidate("resource","waterLegacy",16500,16500);
            var scope=Scope("resource:waterLegacy","Water");
            var plan=ReviewedSemanticPlacementPlanner.Build(Reviewed(),scope,
                new[]{oldWater},Array.Empty<PlacementRequest>(),8,1,
                new[]{"resource:waterLegacy"});
            Assert.That(plan.Code,Is.EqualTo("planned"));
            Assert.That(plan.Placements.Count,Is.EqualTo(1));
            Assert.That(plan.Placements[0].X,Is.EqualTo(16500));
            Assert.That(plan.Placements[0].Y,Is.EqualTo(16500));
        }

        [Test]
        public void ExistingOnUnknownTerrainMustBeExplicitlyGrandfathered()
        {
            var oldHive=Candidate("hive","legacy",16500,16500);
            var scope=Scope("hive:legacy","Land");
            var plan=ReviewedSemanticPlacementPlanner.Build(Reviewed("Unknown"),scope,
                new[]{oldHive},Array.Empty<PlacementRequest>(),8,1);
            Assert.That(plan.Code,Is.EqualTo("fixed_semantic_terrain_unverified"));
            Assert.That(plan.Placements,Is.Empty);
        }
        [Test]
        public void ExistingFixedOverlapCannotBeSilentlyRemoved()
        {
            var fixed1=Candidate("hive","H1",16500,16500);
            var fixed2=Candidate("bestiary","spider",16500,16500);
            var scope=Scope("hive:H1","Land","bestiary:spider","Land");
            var plan=ReviewedSemanticPlacementPlanner.Build(Reviewed(),scope,
                new[]{fixed1,fixed2},Array.Empty<PlacementRequest>(),8,1);
            Assert.That(plan.Code,Is.EqualTo("fixed_occupation_conflict_or_unverified"));
            Assert.That(plan.Placements,Is.Empty);
        }
    }
}