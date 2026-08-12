using BeeKingdom.Hive;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ColonyTrafficManagerTests
    {
        [Test]
        public void RouteCanBeFoundAndReserved()
        {
            ColonyTrafficManager manager = new ColonyTrafficManager();
            manager.RegisterRoute(new ColonyTrafficRoute("r", "entrance", "nursery", 1));

            Assert.That(manager.TryFindRoute("entrance", "nursery", out ColonyTrafficRoute route), Is.True);
            Assert.That(route.RouteId, Is.EqualTo("r"));
            Assert.That(manager.Reserve("r"), Is.True);
            Assert.That(manager.Reserve("r"), Is.False);
        }

        [Test]
        public void ReleaseFreesCapacity()
        {
            ColonyTrafficManager manager = new ColonyTrafficManager();
            manager.RegisterRoute(new ColonyTrafficRoute("r", "a", "b", 1));
            manager.Reserve("r");

            Assert.That(manager.Release("r"), Is.True);
            Assert.That(manager.Reserve("r"), Is.True);
        }
    }
}
