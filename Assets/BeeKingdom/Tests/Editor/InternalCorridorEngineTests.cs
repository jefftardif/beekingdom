using BeeKingdom.Chambers;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class InternalCorridorEngineTests
    {
        [Test]
        public void CreateAndDestroyCorridor()
        {
            CorridorManager manager = CreateManager();
            CorridorInstance corridor = manager.CreateCorridor("standard", "a", "b", 5d);

            Assert.That(corridor, Is.Not.Null);
            Assert.That(corridor.State, Is.EqualTo(CorridorState.Operational));
            Assert.That(manager.DestroyCorridor(corridor.EntityId), Is.True);
            Assert.That(manager.CorridorCount, Is.EqualTo(0));
        }

        [Test]
        public void ReservationCanTriggerCongestion()
        {
            CorridorManager manager = CreateManager();
            CorridorInstance corridor = manager.CreateCorridor("standard", "a", "b", 5d);

            Assert.That(manager.ReserveTraversal(corridor.EntityId), Is.True);
            Assert.That(manager.ReserveTraversal(corridor.EntityId), Is.True);
            Assert.That(corridor.State, Is.EqualTo(CorridorState.Congested));
            Assert.That(manager.DetectCongestion().Count, Is.EqualTo(1));

            Assert.That(manager.ReleaseTraversal(corridor.EntityId), Is.True);
            Assert.That(corridor.State, Is.EqualTo(CorridorState.Operational));
        }

        [Test]
        public void BlockedCorridorRejectsTraversal()
        {
            CorridorManager manager = CreateManager();
            CorridorInstance corridor = manager.CreateCorridor("standard", "a", "b", 5d);

            Assert.That(manager.BlockCorridor(corridor.EntityId), Is.True);
            Assert.That(manager.ReserveTraversal(corridor.EntityId), Is.False);
        }

        [Test]
        public void TravelCostIncreasesWithCongestion()
        {
            CorridorManager manager = CreateManager();
            CorridorInstance corridor = manager.CreateCorridor("standard", "a", "b", 5d);
            double baseCost = manager.CalculateTravelCost(corridor.EntityId);

            manager.ReserveTraversal(corridor.EntityId);
            manager.ReserveTraversal(corridor.EntityId);

            Assert.That(manager.CalculateTravelCost(corridor.EntityId), Is.GreaterThan(baseCost));
        }

        private static CorridorManager CreateManager()
        {
            CorridorManager manager = new CorridorManager();
            manager.RegisterDefinition(new CorridorDefinition("standard", CorridorType.Standard, 1d, 1, 3, 1d, 1d));
            return manager;
        }
    }
}
