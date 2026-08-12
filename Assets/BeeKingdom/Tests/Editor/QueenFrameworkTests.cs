using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class QueenFrameworkTests
    {
        [Test]
        public void RegisterQueenAndQueryStatus()
        {
            QueenManager manager = CreateManager();

            QueenInstance queen = manager.RegisterQueen("queen", "default", 0d, "starter");

            Assert.That(queen, Is.Not.Null);
            Assert.That(manager.QueryQueenStatus("queen"), Is.EqualTo(QueenState.Egg));
            Assert.That(manager.ChangeQueenState("queen", QueenState.MatureQueen), Is.True);
            Assert.That(manager.QueryQueenStatus("queen"), Is.EqualTo(QueenState.MatureQueen));
        }

        [Test]
        public void QueenBonusesAndPheromonesAreDeterministic()
        {
            QueenManager manager = CreateManager();
            QueenInstance queen = manager.RegisterQueen("queen", "default");

            Assert.That(manager.ApplyQueenEffect("queen", QueenPheromoneType.RoyalPresence), Is.True);
            Assert.That(queen.ActivePheromones, Does.Contain(QueenPheromoneType.RoyalPresence));
            Assert.That(queen.CalculateGrowthBonus(), Is.GreaterThan(0d));
            Assert.That(queen.CalculateReproductionBonus(), Is.GreaterThan(0d));
        }

        [Test]
        public void SnapshotRestoresQueen()
        {
            QueenManager manager = CreateManager();
            manager.RegisterQueen("queen", "default", 2d, "wild");
            manager.ChangeQueenState("queen", QueenState.MatureQueen);
            QueenSnapshot snapshot = manager.CreateSnapshot();

            QueenManager restored = CreateManager();
            restored.RestoreSnapshot(snapshot);

            Assert.That(restored.GetQueen("queen"), Is.Not.Null);
            Assert.That(restored.QueryQueenStatus("queen"), Is.EqualTo(QueenState.MatureQueen));
        }

        private static QueenManager CreateManager()
        {
            QueenManager manager = new QueenManager();
            manager.RegisterDefinition(new QueenDefinition("default", 1d, 0.8d, 0.9d, 12d, 100d, 0.7d, 0.6d, 0.1d, 1d));
            return manager;
        }
    }
}
