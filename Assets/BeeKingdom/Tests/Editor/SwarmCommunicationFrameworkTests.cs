using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SwarmCommunicationFrameworkTests
    {
        [Test]
        public void BroadcastReceiveAndExpireSignal()
        {
            SwarmCommunicationManager manager = CreateManager();
            CommunicationSignal signal = manager.BroadcastSignal("pheromone", CommunicationSignalType.FoodFound, "bee-1", 1d, 1d, 0.5d, 1d, 1d);

            Assert.That(manager.ReceiveSignal(signal.SignalId, 2d, 1d), Is.True);
            manager.PropagateSignal(2d);
            manager.ExpireSignal();
            Assert.That(manager.QuerySignals().Count, Is.EqualTo(0));
        }

        [Test]
        public void SaturationBlocksExtraSignals()
        {
            SwarmCommunicationManager manager = CreateManager();
            manager.BroadcastSignal("pheromone", CommunicationSignalType.FoodFound, "bee-1", 1d, 1d, 0d, 10d, 1d);

            Assert.That(manager.BroadcastSignal("pheromone", CommunicationSignalType.DangerDetected, "bee-2", 1d, 1d, 0d, 10d, 1d), Is.Null);
        }

        private static SwarmCommunicationManager CreateManager()
        {
            SwarmCommunicationManager manager = new SwarmCommunicationManager();
            manager.RegisterCommunicationChannel(new CommunicationChannel("pheromone", CommunicationKind.Pheromone, 1d));
            return manager;
        }
    }
}
