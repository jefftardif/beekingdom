using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ColonyOptimizationFrameworkTests
    {
        [Test]
        public void AnalyzeColonyGeneratesRecommendations()
        {
            ColonyOptimizationManager manager = CreateManager();
            OptimizationReport report = manager.AnalyzeColony(0.8d, 0.8d, 0.2d, 0.1d, 0.7d, 0.1d);
            Assert.That(report.Recommendations.Count, Is.GreaterThan(0));
        }

        [Test]
        public void RegressionIsDetected()
        {
            ColonyOptimizationManager manager = CreateManager();
            manager.AnalyzeColony(0.1d, 0.1d, 0.1d, 0.1d, 0.1d, 0.1d);
            OptimizationReport report = manager.AnalyzeColony(0.9d, 0.9d, 0.9d, 0.9d, 0.9d, 0.9d);
            Assert.That(report.RegressionDetected, Is.True);
        }

        private static ColonyOptimizationManager CreateManager()
        {
            ColonyOptimizationManager manager = new ColonyOptimizationManager();
            manager.RegisterOptimizationRule(new OptimizationRule("logistics", OptimizationDomain.Logistics, 0.5d, OptimizationRecommendationType.OpenCorridor));
            manager.RegisterOptimizationRule(new OptimizationRule("population", OptimizationDomain.Population, 0.5d, OptimizationRecommendationType.ReassignCastes));
            return manager;
        }
    }
}
