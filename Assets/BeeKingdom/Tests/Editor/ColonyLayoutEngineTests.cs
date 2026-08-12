using BeeKingdom.Chambers;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ColonyLayoutEngineTests
    {
        [Test]
        public void AnalyzeLayoutProducesScoreAndRecommendations()
        {
            ColonyLayoutManager manager = new ColonyLayoutManager();

            ColonyLayoutSnapshot snapshot = manager.AnalyzeLayout(chamberCount: 2, corridorCount: 0, congestedCorridors: 0);

            Assert.That(snapshot.Version, Is.EqualTo(1));
            Assert.That(snapshot.Score.OverallScore, Is.GreaterThan(0d));
            Assert.That(snapshot.Recommendations.Count, Is.GreaterThan(0));
        }

        [Test]
        public void DetectBottlenecksFindsCongestedCorridors()
        {
            CorridorDefinition definition = new CorridorDefinition("standard", CorridorType.Standard, 1d, 1, 3, 1d, 1d);
            CorridorInstance corridor = new CorridorInstance("standard", "corridor", "a", "b", 1d, definition);
            corridor.ChangeState(CorridorState.Operational);
            corridor.ReserveTraversal();
            corridor.ReserveTraversal();
            ColonyLayoutManager manager = new ColonyLayoutManager();

            Assert.That(manager.DetectBottlenecks(new[] { corridor }), Is.EqualTo(new[] { "corridor" }));
        }

        [Test]
        public void SectorsAreIncludedInSnapshot()
        {
            ColonyLayoutManager manager = new ColonyLayoutManager();
            manager.CreateSector("Royal", new[] { "queen-room" });

            ColonyLayoutSnapshot snapshot = manager.GenerateLayoutSnapshot();

            Assert.That(snapshot.Sectors.Count, Is.EqualTo(1));
            Assert.That(manager.QuerySector("Royal").ChamberIds[0], Is.EqualTo("queen-room"));
        }
    }
}
