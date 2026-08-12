using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class PopulationFrameworkTests
    {
        [Test]
        public void RegisterQueryAndRemoveBee()
        {
            PopulationManager manager = CreateManager();
            BeePopulationRecord bee = new BeePopulationRecord("bee-1", "worker", BeeCaste.Worker, 2d);

            Assert.That(manager.RegisterBee(bee), Is.True);
            Assert.That(manager.QueryByCaste(BeeCaste.Worker).Count, Is.EqualTo(1));
            Assert.That(manager.QueryByState(BeePopulationState.Alive).Count, Is.EqualTo(1));
            Assert.That(manager.UnregisterBee("bee-1"), Is.True);
        }

        [Test]
        public void StatisticsTrackPopulation()
        {
            PopulationManager manager = CreateManager();
            manager.RegisterBee(new BeePopulationRecord("bee-1", "worker", BeeCaste.Worker, 2d));
            manager.RegisterBee(new BeePopulationRecord("bee-2", "builder", BeeCaste.Builder, 4d));

            PopulationStatistics statistics = manager.QueryStatistics();

            Assert.That(statistics.TotalPopulation, Is.EqualTo(2));
            Assert.That(statistics.AverageAge, Is.EqualTo(3d));
            Assert.That(statistics.PopulationByCaste[BeeCaste.Worker], Is.EqualTo(1));
        }

        [Test]
        public void SnapshotRestoresPopulation()
        {
            PopulationManager manager = CreateManager();
            manager.RegisterBee(new BeePopulationRecord("bee-1", "worker", BeeCaste.Worker, 2d));
            PopulationSnapshot snapshot = manager.CreateSnapshot();

            PopulationManager restored = CreateManager();
            restored.RestoreSnapshot(snapshot);

            Assert.That(restored.QueryPopulation().Count, Is.EqualTo(1));
            Assert.That(restored.QueryPopulation()[0].BeeId, Is.EqualTo("bee-1"));
        }

        private static PopulationManager CreateManager()
        {
            PopulationManager manager = new PopulationManager();
            manager.RegisterDefinition(new PopulationDefinition("worker", BeeCaste.Worker, 30d, 1d));
            manager.RegisterDefinition(new PopulationDefinition("builder", BeeCaste.Builder, 25d, 1.2d));
            return manager;
        }
    }
}
