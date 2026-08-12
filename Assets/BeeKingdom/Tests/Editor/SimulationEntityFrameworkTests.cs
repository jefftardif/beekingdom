using BeeKingdom.Core.Entities;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SimulationEntityFrameworkTests
    {
        [Test]
        public void FactoryCreatesDeterministicIds()
        {
            EntityFactory first = new EntityFactory();
            EntityFactory second = new EntityFactory();

            Assert.That(first.Create("Bee").Id, Is.EqualTo(second.Create("Bee").Id));
        }

        [Test]
        public void RegistryStoresEntitiesById()
        {
            EntityRegistry registry = new EntityRegistry();
            SimulationEntity entity = new EntityFactory().Create("Flower", "World");

            Assert.That(registry.Register(entity), Is.True);
            Assert.That(registry.TryGet(entity.Id, out SimulationEntity loaded), Is.True);
            Assert.That(loaded, Is.SameAs(entity));
        }

        [Test]
        public void LifecycleTransitionsWork()
        {
            SimulationEntity entity = new EntityFactory().Create("Resource");
            EntityLifecycle lifecycle = new EntityLifecycle();

            Assert.That(lifecycle.Activate(entity), Is.True);
            Assert.That(lifecycle.Suspend(entity), Is.True);
            Assert.That(lifecycle.Activate(entity), Is.True);
            Assert.That(lifecycle.Destroy(entity), Is.True);
            Assert.That(entity.State, Is.EqualTo(EntityLifecycleState.Destroyed));
        }
    }
}
