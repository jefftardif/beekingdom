using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Hive;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class OrganicHiveGrowthTests
    {
        [Test]
        public void PlanExpansionRejectsInsufficientWax()
        {
            HiveGrowthManager manager = new HiveGrowthManager();

            HiveExpansionPlan plan = manager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.HoneyStorage, 20, 1d, 28d, true));

            Assert.That(plan.IsApproved, Is.False);
            Assert.That(plan.Reason, Does.Contain("wax"));
        }

        [Test]
        public void CreateChamberCreatesConstructionSiteAndCells()
        {
            HiveGrowthManager manager = new HiveGrowthManager();
            HiveExpansionPlan plan = manager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.Nursery, 20, 50d, 28d, true));

            ConstructionSite site = manager.CreateChamber(plan);
            HiveTopologySnapshot layout = manager.GetLayout();

            Assert.That(site.State, Is.EqualTo(ConstructionSiteState.UnderConstruction));
            Assert.That(layout.Chambers.Count, Is.EqualTo(1));
            Assert.That(layout.Cells.Count, Is.EqualTo(plan.CellCount));
            Assert.That(layout.ConstructionSites.Count, Is.EqualTo(1));
        }

        [Test]
        public void ExecuteCompletesConstructionOverTime()
        {
            HiveGrowthManager manager = new HiveGrowthManager();
            HiveExpansionPlan plan = manager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.WaxWorkshop, 20, 50d, 28d, true));
            ConstructionSite site = manager.CreateChamber(plan);

            manager.Execute(Context(plan.RequiredWorkSeconds));

            Assert.That(site.State, Is.EqualTo(ConstructionSiteState.Upgradeable));
            Assert.That(manager.Diagnostics.CompletedChambers, Is.EqualTo(1));
            Assert.That(manager.GetLayout().Cells[0].State, Is.EqualTo(HoneycombCellState.Complete));
        }

        [Test]
        public void ConnectChambersCreatesReachableTopology()
        {
            HiveGrowthManager manager = new HiveGrowthManager();
            ConstructionSite entrance = manager.CreateChamber(manager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.Entrance, 20, 50d, 28d, true)));
            ConstructionSite nursery = manager.CreateChamber(manager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.Nursery, 20, 50d, 28d, true)), entrance.ChamberId);

            HiveLayoutValidationResult result = manager.ValidateTopology();

            Assert.That(manager.GetLayout().Chambers.Count, Is.EqualTo(2));
            Assert.That(result.IsValid, Is.True);
            Assert.That(nursery.ChamberId, Is.Not.EqualTo(entrance.ChamberId));
        }

        [Test]
        public void ValidationDetectsIsolatedChambers()
        {
            HiveGrowthManager manager = new HiveGrowthManager();
            manager.CreateChamber(manager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.Entrance, 20, 50d, 28d, true)));
            manager.CreateChamber(manager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.Defense, 20, 50d, 28d, true)));

            HiveLayoutValidationResult result = manager.ValidateTopology();

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsolatedChamberIds.Count, Is.EqualTo(2));
            Assert.That(result.InaccessibleChamberIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotExposesSerializableLayoutData()
        {
            HiveGrowthManager manager = new HiveGrowthManager();
            manager.CreateChamber(manager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.PollenStorage, 20, 50d, 28d, true)));

            HiveTopologySnapshot snapshot = manager.GetLayout();

            Assert.That(snapshot.Revision, Is.GreaterThan(0));
            Assert.That(snapshot.Chambers[0].ChamberId, Is.Not.Empty);
            Assert.That(snapshot.Cells[0].ChamberId, Is.EqualTo(snapshot.Chambers[0].ChamberId));
            Assert.That(snapshot.ConstructionSites[0].ChamberId, Is.EqualTo(snapshot.Chambers[0].ChamberId));
        }

        private static SimulationExecutionContext Context(double deltaSeconds)
        {
            return new SimulationExecutionContext(
                new SimulationTimestamp(1, deltaSeconds),
                new SimulationCalendar(1, 12, 0, SimulationSeason.Summer),
                SimulationTickFrequency.EveryFrame,
                deltaSeconds,
                new ServiceContainer());
        }
    }
}
