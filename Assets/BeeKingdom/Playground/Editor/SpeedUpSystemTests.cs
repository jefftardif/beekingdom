using BeeKingdom.Gameplay.Progression;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SpeedUpSystemTests
    {
        [Test]
        public void RegistryProvidesAllSixCategories()
        {
            Assert.That(SpeedUpRegistry.GetByCategory(SpeedUpCategory.Universal).Count, Is.GreaterThan(0));
            Assert.That(SpeedUpRegistry.GetByCategory(SpeedUpCategory.Construction).Count, Is.GreaterThan(0));
            Assert.That(SpeedUpRegistry.GetByCategory(SpeedUpCategory.Research).Count, Is.GreaterThan(0));
            Assert.That(SpeedUpRegistry.GetByCategory(SpeedUpCategory.Training).Count, Is.GreaterThan(0));
            Assert.That(SpeedUpRegistry.GetByCategory(SpeedUpCategory.Healing).Count, Is.GreaterThan(0));
            Assert.That(SpeedUpRegistry.GetByCategory(SpeedUpCategory.Manufacturing).Count, Is.GreaterThan(0));
        }

        [Test]
        public void InventoryRejectsNonPositiveMutationsAndNeverRemovesMoreThanAvailable()
        {
            const string id = "universal_60s";
            int before = SpeedUpInventory.GetCount(id);
            SpeedUpInventory.Add(id, 0);
            SpeedUpInventory.Add(id, -1);
            Assert.That(SpeedUpInventory.GetCount(id), Is.EqualTo(before));
            Assert.That(SpeedUpInventory.Remove(id, 1), Is.EqualTo(before > 0));
            if (before > 0) SpeedUpInventory.Add(id, 1);
            Assert.That(SpeedUpInventory.Remove(id, 0), Is.False);
        }

        [Test]
        public void AutoUseDoesNotDuplicateUniversalStacksForUniversalTarget()
        {
            const string id = "universal_60s";
            SpeedUpInventory.Add(id, 1);
            SpeedUpAutoUse.AutoUsePlan plan = SpeedUpAutoUse.ComputeBestPlan(SpeedUpCategory.Universal, 60L);
            Assert.That(plan, Is.Not.Null);
            Assert.That(plan.Stacks.Count, Is.EqualTo(1));
            Assert.That(plan.TotalSeconds, Is.EqualTo(60L));
            Assert.That(SpeedUpAutoUse.ApplyPlan(plan, SpeedUpCategory.Universal), Is.True);
            Assert.That(SpeedUpInventory.GetCount(id), Is.EqualTo(0));
        }
    }
}
