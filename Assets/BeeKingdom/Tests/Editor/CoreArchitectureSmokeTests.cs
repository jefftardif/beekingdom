using BeeKingdom.Core.Services;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class CoreArchitectureSmokeTests
    {
        [Test]
        public void ServiceContainerRegistersAndResolvesService()
        {
            ServiceContainer container = new ServiceContainer();
            IEventBus eventBus = new EventBus();

            container.Register(eventBus);

            Assert.That(container.TryGet(out IEventBus resolved), Is.True);
            Assert.That(resolved, Is.SameAs(eventBus));
        }

        [Test]
        public void GameServiceLifecycleIsIdempotent()
        {
            EventBus eventBus = new EventBus();
            ServiceContainer container = new ServiceContainer();

            eventBus.Initialize(container);
            Assert.That(eventBus.IsInitialized, Is.True);

            eventBus.Start();
            Assert.That(eventBus.State, Is.EqualTo(Core.Services.ServiceState.Running));

            eventBus.Shutdown();
            Assert.That(eventBus.IsInitialized, Is.False);
            Assert.That(eventBus.State, Is.EqualTo(Core.Services.ServiceState.Disposed));
        }
    }
}
