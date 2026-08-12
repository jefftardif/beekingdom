using BeeKingdom.Hive;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveDomainTests
    {
        [Test]
        public void CreateHiveCreatesValidAggregateWithQueen()
        {
            HiveManager manager = new HiveManager();

            HiveAggregate hive = manager.CreateHive("hive-1", "player-1", "queen-1", new HiveCapacity(10, 5, 2));

            Assert.That(hive.HiveId, Is.EqualTo("hive-1"));
            Assert.That(hive.BeeIds, Does.Contain("queen-1"));
            Assert.That(hive.Validate().IsValid, Is.True);
        }

        [Test]
        public void CannotRemoveQueen()
        {
            HiveManager manager = new HiveManager();
            manager.CreateHive("hive-1", "player-1", "queen-1", new HiveCapacity(10, 5, 2));

            bool removed = manager.RemoveBee("hive-1", "queen-1");

            Assert.That(removed, Is.False);
            Assert.That(manager.GetStatistics("hive-1").Population, Is.EqualTo(1));
        }

        [Test]
        public void BeeCanBelongToOnlyOneHive()
        {
            HiveManager manager = new HiveManager();
            manager.CreateHive("hive-1", "player-1", "queen-1", new HiveCapacity(10, 5, 2));
            manager.CreateHive("hive-2", "player-1", "queen-2", new HiveCapacity(10, 5, 2));

            Assert.That(manager.AddBee("hive-1", "bee-1"), Is.True);
            Assert.That(manager.AddBee("hive-2", "bee-1"), Is.False);
        }

        [Test]
        public void BuildingCanBelongToOnlyOneHive()
        {
            HiveManager manager = new HiveManager();
            manager.CreateHive("hive-1", "player-1", "queen-1", new HiveCapacity(10, 5, 2));
            manager.CreateHive("hive-2", "player-1", "queen-2", new HiveCapacity(10, 5, 2));

            Assert.That(manager.RegisterBuilding("hive-1", "building-1"), Is.True);
            Assert.That(manager.RegisterBuilding("hive-2", "building-1"), Is.False);
        }

        [Test]
        public void HivePublishesDomainEvents()
        {
            EventBus eventBus = new EventBus();
            int created = 0;
            int added = 0;
            eventBus.Subscribe<HiveCreated>(_ => created++);
            eventBus.Subscribe<BeeAdded>(_ => added++);
            HiveManager manager = new HiveManager(eventBus);

            manager.CreateHive("hive-1", "player-1", "queen-1", new HiveCapacity(10, 5, 2));
            manager.AddBee("hive-1", "bee-1");

            Assert.That(created, Is.EqualTo(1));
            Assert.That(added, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotRoundTripsHive()
        {
            HiveManager manager = new HiveManager();
            HiveAggregate hive = manager.CreateHive("hive-1", "player-1", "queen-1", new HiveCapacity(10, 5, 2));
            manager.AddBee("hive-1", "bee-1");
            manager.RegisterBuilding("hive-1", "building-1");

            HiveAggregate loaded = HiveAggregate.FromSnapshot(hive.ToSnapshot());

            Assert.That(loaded.HiveId, Is.EqualTo("hive-1"));
            Assert.That(loaded.BeeIds, Does.Contain("bee-1"));
            Assert.That(loaded.BuildingIds, Does.Contain("building-1"));
            Assert.That(loaded.Validate().IsValid, Is.True);
        }
    }
}
