using BeeKingdom.Gameplay.Progression;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SmartSpeedUpTests
    {
        [Test]
        public void SuperiorSpeedUpCompletesTargetWithoutNegativeRemaining()
        {
            const string id = "universal_900s";
            int before = SpeedUpInventory.GetCount(id);
            SpeedUpInventory.Add(id, 1);
            try
            {
                SmartSpeedUpPlan plan = SmartSpeedUpCalculator.ComputePlan(new SpeedUpDialogContext(SpeedUpCategory.Research, "research", 60L));
                Assert.That(plan, Is.Not.Null);
                Assert.That(plan.CompletesTarget, Is.True);
                Assert.That(plan.RemainingAfterSeconds, Is.EqualTo(0L));
                Assert.That(plan.WasteSeconds, Is.GreaterThanOrEqualTo(0L));
            }
            finally
            {
                SpeedUpInventory.Remove(id, SpeedUpInventory.GetCount(id) - before);
            }
        }

        [Test]
        public void SpecializedContextExcludesOtherSpecializations()
        {
            const string constructionId = "construction_300s";
            const string researchId = "research_900s";
            int constructionBefore = SpeedUpInventory.GetCount(constructionId);
            int researchBefore = SpeedUpInventory.GetCount(researchId);
            SpeedUpInventory.Add(constructionId, 1);
            SpeedUpInventory.Add(researchId, 1);
            try
            {
                SmartSpeedUpPlan plan = SmartSpeedUpCalculator.ComputePlan(new SpeedUpDialogContext(SpeedUpCategory.Research, "research", 900L));
                Assert.That(plan, Is.Not.Null);
                for (int i = 0; i < plan.Entries.Count; i++)
                    Assert.That(plan.Entries[i].Item.Category, Is.EqualTo(SpeedUpCategory.Research).Or.EqualTo(SpeedUpCategory.Universal));
            }
            finally
            {
                SpeedUpInventory.Remove(constructionId, SpeedUpInventory.GetCount(constructionId) - constructionBefore);
                SpeedUpInventory.Remove(researchId, SpeedUpInventory.GetCount(researchId) - researchBefore);
            }
        }

        [Test]
        public void ApplyingPlanConsumesExactlyThePlannedStacks()
        {
            const string id = "universal_60s";
            int before = SpeedUpInventory.GetCount(id);
            SpeedUpInventory.Add(id, 2);
            try
            {
                SmartSpeedUpPlan plan = SmartSpeedUpCalculator.ComputePlan(new SpeedUpDialogContext(SpeedUpCategory.Research, "research", 120L));
                Assert.That(SmartSpeedUpCalculator.ApplyPlan(plan, out long remaining), Is.True);
                Assert.That(remaining, Is.EqualTo(0L));
                Assert.That(SpeedUpInventory.GetCount(id), Is.EqualTo(before));
            }
            finally
            {
                int current = SpeedUpInventory.GetCount(id);
                if (current < before) SpeedUpInventory.Add(id, before - current);
                if (current > before) SpeedUpInventory.Remove(id, current - before);
            }
        }
    }
}
