using BeeKingdom.Buildings;
using BeeKingdom.Chambers;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ChamberFrameworkTests
    {
        [Test]
        public void CreateAndQueryChamber()
        {
            ChamberManager manager = CreateManager();
            ChamberInstance chamber = manager.CreateChamber("nursery", "building-1", new BuildingPosition(1, 2), 0);

            Assert.That(chamber, Is.Not.Null);
            Assert.That(manager.QueryByCategory("Nursery").Count, Is.EqualTo(1));
            Assert.That(manager.QueryByActivity("brood-care").Count, Is.EqualTo(1));
        }

        [Test]
        public void CapacityCanOverloadChamber()
        {
            ChamberManager manager = CreateManager();
            ChamberInstance chamber = manager.CreateChamber("nursery", "building-1", new BuildingPosition(0, 0), 0);

            manager.SetOccupancy(chamber.EntityId, 6);

            Assert.That(chamber.Capacity.IsOverloaded, Is.True);
            Assert.That(chamber.CurrentState, Is.EqualTo(ChamberState.Overloaded));
            Assert.That(manager.Diagnostics.Overloaded, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotRestoresChambers()
        {
            ChamberManager manager = CreateManager();
            ChamberInstance chamber = manager.CreateChamber("nursery", "building-1", new BuildingPosition(2, 3), 90);
            manager.SetOccupancy(chamber.EntityId, 2);
            ChamberSnapshot snapshot = manager.Snapshot();

            ChamberManager restored = CreateManager();
            restored.RestoreSnapshot(snapshot);

            Assert.That(restored.GetChamber(chamber.EntityId, out ChamberInstance loaded), Is.True);
            Assert.That(loaded.Capacity.Occupancy, Is.EqualTo(2));
            Assert.That(loaded.Position, Is.EqualTo(new BuildingPosition(2, 3)));
        }

        [Test]
        public void RegistryScales()
        {
            ChamberManager manager = new ChamberManager();
            for (int i = 0; i < 10000; i++)
            {
                Assert.That(manager.RegisterDefinition(new ChamberDefinition("c" + i, "c" + i, "Utility", 1, new BuildingSize(1, 1))), Is.True);
            }
            Assert.That(manager.Diagnostics.RegisteredDefinitions, Is.EqualTo(10000));
        }

        private static ChamberManager CreateManager()
        {
            ChamberManager manager = new ChamberManager();
            manager.RegisterDefinition(new ChamberDefinition("nursery", "Nursery", "Nursery", 5, new BuildingSize(2, 2), supportedActivities: new[] { "brood-care" }));
            return manager;
        }
    }
}
