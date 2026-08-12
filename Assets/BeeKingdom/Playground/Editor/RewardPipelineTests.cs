using System.Collections.Generic;
using BeeKingdom.Rewards;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class RewardPipelineTests
    {
        private sealed class TestInventory : IRewardInventory
        {
            public readonly List<Reward> Added = new List<Reward>();

            public bool TryAdd(Reward reward)
            {
                Added.Add(reward);
                return true;
            }
        }

        [Test]
        public void PipelineAppliesMixedBundleAndRecordsHistory()
        {
            var inventory = new TestInventory();
            var pipeline = new RewardPipeline(inventory);
            foreach (RewardType type in System.Enum.GetValues(typeof(RewardType)))
                pipeline.RegisterHandler(type, new InventoryRewardHandler());

            var bundle = new RewardBundle("daily_mission")
                .Add(new Reward(RewardType.Resource, "honey", 100))
                .Add(new Reward(RewardType.SpeedUp, "universal_60s", 1))
                .Add(new Reward(RewardType.Future, "battle_pass", 1));

            Assert.That(pipeline.TryDispatch(bundle), Is.True);
            Assert.That(inventory.Added.Count, Is.EqualTo(3));
            Assert.That(pipeline.History.Entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void PipelineRejectsInvalidBundleBeforeApplyingAnything()
        {
            var inventory = new TestInventory();
            var pipeline = new RewardPipeline(inventory);
            pipeline.RegisterHandler(RewardType.Resource, new InventoryRewardHandler());

            var bundle = new RewardBundle("invalid")
                .Add(new Reward(RewardType.Resource, "honey", 0));

            Assert.That(pipeline.TryDispatch(bundle), Is.False);
            Assert.That(inventory.Added.Count, Is.EqualTo(0));
            Assert.That(pipeline.History.Entries.Count, Is.EqualTo(0));
        }
    }
}
