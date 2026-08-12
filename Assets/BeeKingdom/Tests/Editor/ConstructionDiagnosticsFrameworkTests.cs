using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ConstructionDiagnosticsFrameworkTests
    {
        [Test]
        public void GeneratesReportWithHealthAndBottlenecks()
        {
            ConstructionDiagnosticsManager manager = new ConstructionDiagnosticsManager();
            ConstructionStatistics statistics = new ConstructionStatistics(2, 10d, 0.4d, 1, 0, 2, 5d, 1, 0, 0.2d);

            ConstructionDiagnosticReport report = manager.GenerateDiagnostics(statistics);

            Assert.That(report.Health, Is.EqualTo(ConstructionHealthState.Critical));
            Assert.That(report.Bottlenecks, Does.Contain(ConstructionBottleneckType.MissingResources));
            Assert.That(report.Bottlenecks, Does.Contain(ConstructionBottleneckType.MissingBuilders));
        }

        [Test]
        public void SnapshotCapturesLastReport()
        {
            ConstructionDiagnosticsManager manager = new ConstructionDiagnosticsManager();
            manager.GenerateDiagnostics(new ConstructionStatistics(1, 1d, 1d, 0, 1, 0, 0d, 0, 0, 1d));

            ConstructionSnapshot snapshot = manager.GenerateSnapshot();

            Assert.That(snapshot.Version, Is.EqualTo(1));
            Assert.That(snapshot.Report.Health, Is.EqualTo(ConstructionHealthState.Excellent));
        }

        [Test]
        public void QueryStatisticsReturnsLastStatistics()
        {
            ConstructionDiagnosticsManager manager = new ConstructionDiagnosticsManager();
            ConstructionStatistics statistics = new ConstructionStatistics(3, 4d, 0.5d, 0, 2, 1, 0d, 0, 1, 0.6d);
            manager.AnalyzeConstruction(statistics);

            Assert.That(manager.QueryStatistics().ConstructionCount, Is.EqualTo(3));
            Assert.That(manager.DetectBottlenecks(), Does.Contain(ConstructionBottleneckType.ReservationConflict));
        }
    }
}
