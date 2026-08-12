using BeeKingdom.Economy;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HiveInventoryTests
    {
        [Test]
        public void CreateCellPublishesEvent()
        {
            EventBus eventBus = new EventBus();
            int created = 0;
            eventBus.Subscribe<StorageCellCreated>(_ => created++);
            HiveInventoryManager manager = new HiveInventoryManager(new ResourceFlowManager(), eventBus);

            manager.CreateCell("cell-1", new StoragePosition(0, 0), ResourceType.Nectar, 10d);

            Assert.That(created, Is.EqualTo(1));
            Assert.That(manager.QueryInventory().CellCount, Is.EqualTo(1));
        }

        [Test]
        public void ReserveAndDepositFillsCell()
        {
            HiveInventoryManager manager = new HiveInventoryManager(new ResourceFlowManager());
            manager.CreateCell("cell-1", new StoragePosition(0, 0), ResourceType.Nectar, 10d);

            StorageReservation reservation = manager.ReserveSpace(ResourceType.Nectar, 5d, new StoragePosition(0, 0), StoragePolicy.Nearest);

            Assert.That(reservation.IsValid, Is.True);
            Assert.That(manager.Deposit(reservation, 0d), Is.True);
            Assert.That(manager.QueryInventory().TotalAmount, Is.EqualTo(5d));
        }

        [Test]
        public void WithdrawRemovesAmount()
        {
            HiveInventoryManager manager = new HiveInventoryManager(new ResourceFlowManager());
            manager.CreateCell("cell-1", new StoragePosition(0, 0), ResourceType.Pollen, 10d);
            StorageReservation reservation = manager.ReserveSpace(ResourceType.Pollen, 8d, new StoragePosition(0, 0), StoragePolicy.Nearest);
            manager.Deposit(reservation, 0d);

            Assert.That(manager.Withdraw("cell-1", 3d), Is.True);
            Assert.That(manager.QueryInventory().TotalAmount, Is.EqualTo(5d));
        }

        [Test]
        public void LocatorFindsNearestCompatibleCell()
        {
            HiveInventoryManager manager = new HiveInventoryManager(new ResourceFlowManager());
            manager.CreateCell("far", new StoragePosition(10, 0), ResourceType.Honey, 10d);
            manager.CreateCell("near", new StoragePosition(1, 0), ResourceType.Honey, 10d);

            Assert.That(manager.FindStorage(ResourceType.Honey, 1d, new StoragePosition(0, 0), StoragePolicy.Nearest, out StorageCell cell), Is.True);
            Assert.That(cell.CellId, Is.EqualTo("near"));
        }

        [Test]
        public void FullClusterPublishesDiagnostic()
        {
            HiveInventoryManager manager = new HiveInventoryManager(new ResourceFlowManager());
            manager.CreateCell("cell-1", new StoragePosition(0, 0), ResourceType.Wax, 5d, "wax-cluster");
            StorageReservation reservation = manager.ReserveSpace(ResourceType.Wax, 5d, new StoragePosition(0, 0), StoragePolicy.Nearest);

            manager.Deposit(reservation, 0d);

            Assert.That(manager.Diagnostics.SaturationCount, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void LongStabilityCreatesManyCells()
        {
            HiveInventoryManager manager = new HiveInventoryManager(new ResourceFlowManager());
            for (int i = 0; i < 5000; i++)
            {
                manager.CreateCell("cell-" + i, new StoragePosition(i, 0), ResourceType.Nectar, 10d);
            }

            Assert.That(manager.QueryInventory().CellCount, Is.EqualTo(5000));
        }
    }
}
