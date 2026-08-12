using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeNeedsFrameworkTests
    {
        [Test]
        public void UpdateNeedsIncreasesValues()
        {
            BeeNeedsManager manager = CreateManager();

            manager.UpdateNeeds(new BeeNeedsContext("bee-1"), 2d);

            Assert.That(manager.QueryNeeds("bee-1")[0].CurrentValue, Is.EqualTo(4d));
        }

        [Test]
        public void SatisfyNeedReducesValue()
        {
            BeeNeedsManager manager = CreateManager();
            manager.UpdateNeeds(new BeeNeedsContext("bee-1"), 2d);

            Assert.That(manager.SatisfyNeed("bee-1", "hunger", 3d), Is.True);

            Assert.That(manager.QueryNeeds("bee-1")[0].CurrentValue, Is.EqualTo(1d));
        }

        [Test]
        public void HighestPriorityNeedUsesWeights()
        {
            BeeNeedsManager manager = CreateManager();
            manager.UpdateNeeds(new BeeNeedsContext("bee-1"), 3d);

            NeedInstance need = manager.GetHighestPriorityNeed("bee-1");

            Assert.That(need.NeedId, Is.EqualTo("hunger"));
        }

        [Test]
        public void CriticalThresholdIsDetected()
        {
            BeeNeedsManager manager = CreateManager();

            manager.UpdateNeeds(new BeeNeedsContext("bee-1"), 5d);

            Assert.That(manager.QueryNeeds("bee-1")[0].IsCritical, Is.True);
        }

        private static BeeNeedsManager CreateManager()
        {
            BeeNeedsManager manager = new BeeNeedsManager();
            manager.RegisterNeedDefinition(new NeedDefinition("hunger", NeedKind.Hunger, 10d, 2d, 8d, 2d, 2d));
            manager.RegisterNeedDefinition(new NeedDefinition("rest", NeedKind.Rest, 10d, 1d, 9d, 2d, 0.5d));
            return manager;
        }
    }
}
