using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ColonyStrategyFrameworkTests
    {
        [Test]
        public void StrategyRespondsToThreat()
        {
            ColonyStrategyManager manager = CreateManager();
            ColonyStrategyDefinition strategy = manager.UpdateStrategy(new StrategyContext(threatPressure: 1d));
            Assert.That(strategy.Type, Is.EqualTo(ColonyStrategyType.Defense));
            Assert.That(manager.QueryGoals().Count, Is.EqualTo(1));
        }

        [Test]
        public void GoalCanBeCompleted()
        {
            ColonyStrategyManager manager = CreateManager();
            manager.UpdateStrategy(new StrategyContext(foodPressure: 1d));
            string goalId = manager.QueryGoals()[0].GoalId;
            Assert.That(manager.CompleteGoal(goalId), Is.True);
        }

        private static ColonyStrategyManager CreateManager()
        {
            ColonyStrategyManager manager = new ColonyStrategyManager();
            manager.RegisterStrategy(new ColonyStrategyDefinition("food", ColonyStrategyType.FoodAccumulation, StrategyMode.Economic, 1d));
            manager.RegisterStrategy(new ColonyStrategyDefinition("defense", ColonyStrategyType.Defense, StrategyMode.Defensive, 1d));
            return manager;
        }
    }
}
