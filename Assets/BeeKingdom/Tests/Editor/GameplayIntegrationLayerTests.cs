using BeeKingdom.Core.Integration;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class GameplayIntegrationLayerTests
    {
        [Test]
        public void RegisterBridgeMakesCapabilityRoutable()
        {
            GameplayIntegrationManager manager = new GameplayIntegrationManager();
            manager.RegisterBridge(new GameplayBridge("bee-ai", "BeeAI", new[] { "Ability.Execute", "Task.Assign" }));

            Assert.That(manager.TryRoute("Ability.Execute", out GameplayBridge bridge), Is.True);
            Assert.That(bridge.BridgeId, Is.EqualTo("bee-ai"));
        }

        [Test]
        public void MissingCapabilityRecordsDiagnostic()
        {
            GameplayIntegrationManager manager = new GameplayIntegrationManager();

            Assert.That(manager.TryRoute("World.Stream", out _), Is.False);
            Assert.That(manager.Diagnostics.MissingRoutes, Is.EqualTo(1));
        }

        [Test]
        public void QueryReturnsAllCompatibleBridges()
        {
            GameplayIntegrationManager manager = new GameplayIntegrationManager();
            manager.RegisterBridge(new GameplayBridge("backend", "Backend", new[] { "Workflow.Create" }));
            manager.RegisterBridge(new GameplayBridge("liveops", "LiveOps", new[] { "Workflow.Create" }));

            Assert.That(manager.QueryBridges("Workflow.Create").Count, Is.EqualTo(2));
        }
    }
}
