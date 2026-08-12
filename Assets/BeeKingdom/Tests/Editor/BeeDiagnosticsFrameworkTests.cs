using BeeKingdom.Core.Logging;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeDiagnosticsFrameworkTests
    {
        [Test]
        public void RecordsStructuredDiagnosticEvents()
        {
            BeeDiagnosticsManager manager = new BeeDiagnosticsManager(BeeLogLevel.Debug);

            Assert.That(manager.Record(BeeLogLevel.Warning, "Simulation", "slow tick", 12d), Is.True);
            BeeDiagnosticsSnapshot snapshot = manager.CreateSnapshot();

            Assert.That(snapshot.Version, Is.EqualTo(1));
            Assert.That(snapshot.Events.Count, Is.EqualTo(1));
            Assert.That(snapshot.Events[0].Category, Is.EqualTo("Simulation"));
            Assert.That(snapshot.Events[0].SimulationSeconds, Is.EqualTo(12d));
            Assert.That(manager.Counters.WarningCount, Is.EqualTo(1));
        }

        [Test]
        public void MinimumLevelAndMutedCategoriesFilterNoise()
        {
            BeeDiagnosticsManager manager = new BeeDiagnosticsManager(BeeLogLevel.Warning);
            manager.SetCategoryMuted("AI", true);

            Assert.That(manager.Record(BeeLogLevel.Info, "Simulation", "ignored", 0d), Is.False);
            Assert.That(manager.Record(BeeLogLevel.Error, "AI", "ignored", 0d), Is.False);
            Assert.That(manager.Record(BeeLogLevel.Error, "Save", "kept", 0d), Is.True);

            Assert.That(manager.EventCount, Is.EqualTo(1));
            Assert.That(manager.Counters.ErrorCount, Is.EqualTo(1));
        }

        [Test]
        public void KeepsBoundedBufferAndCountsDroppedEvents()
        {
            BeeDiagnosticsManager manager = new BeeDiagnosticsManager(BeeLogLevel.Debug, 2);

            manager.Record(BeeLogLevel.Debug, "A", "one", 0d);
            manager.Record(BeeLogLevel.Debug, "A", "two", 0d);
            manager.Record(BeeLogLevel.Debug, "A", "three", 0d);

            BeeDiagnosticsSnapshot snapshot = manager.CreateSnapshot();
            Assert.That(snapshot.Events.Count, Is.EqualTo(2));
            Assert.That(snapshot.Events[0].Message, Is.EqualTo("two"));
            Assert.That(snapshot.DroppedEvents, Is.EqualTo(1));
        }
    }
}
