using BeeKingdom.Economy;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ResourceFlowTests
    {
        [Test]
        public void ProduceStoresResource()
        {
            ResourceFlowManager manager = new ResourceFlowManager();
            manager.GetStorage("hive").SetCapacity(ResourceType.Nectar, 100d);

            double stored = manager.Produce("flower", "hive", ResourceType.Nectar, 25d, 0d);

            Assert.That(stored, Is.EqualTo(25d));
            Assert.That(manager.QueryFlow("hive", ResourceType.Nectar), Is.EqualTo(25d));
        }

        [Test]
        public void ReservePreventsDoubleConsumption()
        {
            ResourceFlowManager manager = new ResourceFlowManager();
            manager.Produce("flower", "hive", ResourceType.Nectar, 25d, 0d);

            ResourceReservation first = manager.Reserve("hive", ResourceType.Nectar, 20d);
            ResourceReservation second = manager.Reserve("hive", ResourceType.Nectar, 20d);

            Assert.That(first.IsValid, Is.True);
            Assert.That(second.IsValid, Is.False);
        }

        [Test]
        public void ConsumeReservedResource()
        {
            ResourceFlowManager manager = new ResourceFlowManager();
            manager.Produce("flower", "hive", ResourceType.Pollen, 25d, 0d);
            ResourceReservation reservation = manager.Reserve("hive", ResourceType.Pollen, 10d);

            Assert.That(manager.Consume(reservation, 1d), Is.True);
            Assert.That(manager.QueryFlow("hive", ResourceType.Pollen), Is.EqualTo(15d));
        }

        [Test]
        public void TransferMovesResourceBetweenStorages()
        {
            ResourceFlowManager manager = new ResourceFlowManager();
            manager.Produce("flower", "field", ResourceType.Water, 20d, 0d);

            Assert.That(manager.Transfer("field", "hive", ResourceType.Water, 12d, 1d), Is.True);
            Assert.That(manager.QueryFlow("field", ResourceType.Water), Is.EqualTo(8d));
            Assert.That(manager.QueryFlow("hive", ResourceType.Water), Is.EqualTo(12d));
        }

        [Test]
        public void StorageFullPublishesDiagnostic()
        {
            ResourceFlowManager manager = new ResourceFlowManager();
            manager.GetStorage("hive").SetCapacity(ResourceType.Honey, 10d);

            manager.Store("hive", ResourceType.Honey, 25d, 0d);

            Assert.That(manager.QueryFlow("hive", ResourceType.Honey), Is.EqualTo(10d));
            Assert.That(manager.Diagnostics.StorageFullCount, Is.EqualTo(1));
        }

        [Test]
        public void ShortagePublishesEvent()
        {
            EventBus eventBus = new EventBus();
            int shortages = 0;
            eventBus.Subscribe<ResourceShortage>(_ => shortages++);
            ResourceFlowManager manager = new ResourceFlowManager(eventBus);

            ResourceReservation reservation = manager.Reserve("hive", ResourceType.RoyalJelly, 1d);

            Assert.That(reservation.IsValid, Is.False);
            Assert.That(shortages, Is.EqualTo(1));
        }

        [Test]
        public void HistoryIsLimited()
        {
            ResourceFlowManager manager = new ResourceFlowManager(null, 3);
            for (int i = 0; i < 10; i++)
            {
                manager.Produce("flower", "hive", ResourceType.Nectar, 1d, i);
            }

            Assert.That(manager.GetHistory().Count, Is.EqualTo(3));
        }
    }
}
