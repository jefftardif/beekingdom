using BeeKingdom.Economy;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ResourceFlowEngineTests
    {
        [Test]
        public void RouteExecutesDeterministicTransfer()
        {
            ResourceFlowManager manager = new ResourceFlowManager();
            manager.Store("source", ResourceType.Nectar, 100d, 0d);
            ResourceFlowEngine engine = new ResourceFlowEngine(manager);
            engine.RegisterRoute(new ResourceFlowRoute("r", "source", "dest", ResourceType.Nectar, 25d));

            Assert.That(engine.Execute(new ResourceFlowRequest("r", 50d, 1d)), Is.True);
            Assert.That(manager.QueryFlow("dest", ResourceType.Nectar), Is.EqualTo(25d));
            Assert.That(engine.ExecutedFlows, Is.EqualTo(1));
        }

        [Test]
        public void MissingRouteFails()
        {
            ResourceFlowEngine engine = new ResourceFlowEngine();

            Assert.That(engine.Execute(new ResourceFlowRequest("missing", 1d, 0d)), Is.False);
        }
    }
}
