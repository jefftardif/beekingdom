using BeeKingdom.Chambers;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ChamberConnectionSystemTests
    {
        [Test]
        public void ConnectCreatesNeighbours()
        {
            ChamberConnectionManager manager = CreateManager();

            ChamberConnection connection = manager.ConnectChambers("corridor", "entrance", "nursery", 3d);

            Assert.That(connection, Is.Not.Null);
            Assert.That(manager.ConnectionCount, Is.EqualTo(1));
            Assert.That(manager.QueryNeighbours("entrance"), Does.Contain("nursery"));
            Assert.That(manager.QueryNeighbours("nursery"), Does.Contain("entrance"));
        }

        [Test]
        public void ValidationRejectsInvalidConnections()
        {
            ChamberConnectionManager manager = CreateManager();

            Assert.That(manager.ValidateConnection("corridor", "a", "a", 1d, 1).Status, Is.EqualTo(ConnectionValidationStatus.ForbiddenLoop));
            Assert.That(manager.ValidateConnection("corridor", "a", "b", 99d, 1).Status, Is.EqualTo(ConnectionValidationStatus.TooFar));
            Assert.That(manager.ValidateConnection("corridor", "a", "b", 1d, 0).Status, Is.EqualTo(ConnectionValidationStatus.InvalidCapacity));
        }

        [Test]
        public void ShortestPathUsesTraversalCost()
        {
            ChamberConnectionManager manager = CreateManager();
            manager.ConnectChambers("corridor", "a", "b", 1d, traversalCost: 5d);
            manager.ConnectChambers("corridor", "a", "c", 1d, traversalCost: 1d);
            manager.ConnectChambers("corridor", "c", "b", 1d, traversalCost: 1d);

            var path = manager.FindShortestPath("a", "b");

            Assert.That(path, Is.EqualTo(new[] { "a", "c", "b" }));
        }

        [Test]
        public void BlockRestoreDisconnectAndRebuildGraph()
        {
            ChamberConnectionManager manager = CreateManager();
            ChamberConnection first = manager.ConnectChambers("corridor", "a", "b", 1d);
            ChamberConnection second = new ChamberConnection("manual", "x", "y", ChamberConnectionType.Direct, 1d, 1, 1d, ChamberConnectionState.Connected);

            Assert.That(manager.BlockConnection(first.ConnectionId), Is.True);
            Assert.That(manager.QueryNeighbours("a"), Is.Empty);
            Assert.That(manager.RestoreConnection(first.ConnectionId), Is.True);
            Assert.That(manager.DisconnectChambers(first.ConnectionId), Is.True);

            manager.RebuildGraph(new[] { second });
            Assert.That(manager.ConnectionCount, Is.EqualTo(1));
            Assert.That(manager.QueryNeighbours("x"), Does.Contain("y"));
        }

        private static ChamberConnectionManager CreateManager()
        {
            ChamberConnectionManager manager = new ChamberConnectionManager();
            manager.RegisterDefinition(new ChamberConnectionDefinition("corridor", ChamberConnectionType.Corridor, 10d, 2, 1d));
            return manager;
        }
    }
}
