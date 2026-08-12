using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class MaintenanceFrameworkTests
    {
        [Test]
        public void ScheduleStartAndCompleteMaintenance()
        {
            MaintenanceManager manager = CreateManager();
            MaintenanceTask task = manager.ScheduleMaintenance("repair", "building", 80d);

            Assert.That(manager.StartMaintenance(task.TaskId), Is.True);
            Assert.That(manager.CompleteMaintenance(task.TaskId), Is.True);
            Assert.That(task.State, Is.EqualTo(MaintenanceTaskState.Completed));
        }

        [Test]
        public void InspectionAndCostAreDeterministic()
        {
            MaintenanceManager manager = CreateManager();

            Assert.That(manager.InspectBuilding(90d), Is.EqualTo(MaintenanceState.Critical));
            Assert.That(manager.CalculateMaintenanceCost("repair", 10d, 5d, 2d), Is.EqualTo(13d));
        }

        [Test]
        public void CancelRemovesScheduledTask()
        {
            MaintenanceManager manager = CreateManager();
            MaintenanceTask task = manager.ScheduleMaintenance("repair", "building", 20d);

            Assert.That(manager.CancelMaintenance(task.TaskId), Is.True);
            Assert.That(manager.QueryMaintenanceTasks().Count, Is.EqualTo(0));
        }

        private static MaintenanceManager CreateManager()
        {
            MaintenanceManager manager = new MaintenanceManager();
            manager.RegisterDefinition(new MaintenanceDefinition("repair", MaintenanceType.Repair, 10d, 50d));
            return manager;
        }
    }
}
