using BeeKingdom.Buildings;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BuildingUpgradeFrameworkTests
    {
        [Test]
        public void AvailableUpgradeCanStartAndComplete()
        {
            BuildingUpgradeManager manager = CreateManager();

            Assert.That(manager.GetAvailableUpgrades("building", "nursery").Count, Is.EqualTo(1));
            BuildingUpgradeInstance instance = manager.StartUpgrade("building", "nursery-l1");
            Assert.That(instance.State, Is.EqualTo(UpgradeState.Upgrading));
            Assert.That(manager.CompleteUpgrade(instance.InstanceId), Is.True);
            Assert.That(manager.QueryUpgradeHistory().Count, Is.EqualTo(1));
        }

        [Test]
        public void RequirementsBlockUpgrade()
        {
            BuildingUpgradeManager manager = CreateManager();
            manager.RegisterUpgrade(new BuildingUpgradeDefinition("nursery-l2", "nursery", 2, new UpgradeRequirement(requiredLevel: 1)));

            Assert.That(manager.ValidateUpgrade("building", "nursery-l2"), Is.False);
        }

        [Test]
        public void CancelUpgradeRecordsHistory()
        {
            BuildingUpgradeManager manager = CreateManager();
            BuildingUpgradeInstance instance = manager.StartUpgrade("building", "nursery-l1");

            Assert.That(manager.CancelUpgrade(instance.InstanceId), Is.True);
            Assert.That(instance.State, Is.EqualTo(UpgradeState.Cancelled));
            Assert.That(manager.Diagnostics.Cancelled, Is.EqualTo(1));
        }

        private static BuildingUpgradeManager CreateManager()
        {
            BuildingUpgradeManager manager = new BuildingUpgradeManager();
            manager.RegisterUpgrade(new BuildingUpgradeDefinition("nursery-l1", "nursery", 1));
            return manager;
        }
    }
}
