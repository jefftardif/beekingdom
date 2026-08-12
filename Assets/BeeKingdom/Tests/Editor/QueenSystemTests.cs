using BeeKingdom.Hive;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class QueenSystemTests
    {
        [Test]
        public void QueenAcceptsValidStateTransitions()
        {
            QueenManager manager = new QueenManager();
            manager.CreateQueen("queen-1", "hive-1", new QueenHealth(100, 100), 100, 1f, 60f);

            Assert.That(manager.UpdateState("queen-1", QueenState.Larva), Is.True);
            Assert.That(manager.UpdateState("queen-1", QueenState.Pupa), Is.True);
            Assert.That(manager.UpdateState("queen-1", QueenState.VirginQueen), Is.True);
            Assert.That(manager.UpdateState("queen-1", QueenState.MatedQueen), Is.True);
            Assert.That(manager.UpdateState("queen-1", QueenState.ActiveQueen), Is.True);
        }

        [Test]
        public void QueenRejectsInvalidStateTransition()
        {
            QueenManager manager = new QueenManager();
            manager.CreateQueen("queen-1", "hive-1", new QueenHealth(100, 100), 100, 1f, 60f);

            Assert.That(manager.UpdateState("queen-1", QueenState.ActiveQueen), Is.False);
        }

        [Test]
        public void ActiveQueenProducesEggsFromConfiguredRate()
        {
            QueenManager manager = CreateActiveQueen(out _);

            int produced = manager.ProduceEggs("queen-1", 60d);

            Assert.That(produced, Is.EqualTo(60));
            Assert.That(manager.GetStatistics("queen-1").EggsProduced, Is.EqualTo(60));
        }

        [Test]
        public void ProductionBonusIncreasesEggOutput()
        {
            QueenManager manager = CreateActiveQueen(out _);
            manager.ApplyBonus("queen-1", QueenBonusType.Production, 1f);

            int produced = manager.ProduceEggs("queen-1", 60d);

            Assert.That(produced, Is.EqualTo(120));
        }

        [Test]
        public void ExperienceCanLevelUpQueen()
        {
            QueenManager manager = CreateActiveQueen(out EventBus eventBus);
            int levelUps = 0;
            eventBus.Subscribe<QueenLevelUp>(_ => levelUps++);

            bool leveled = manager.AddExperience("queen-1", 100);

            Assert.That(leveled, Is.True);
            Assert.That(levelUps, Is.EqualTo(1));
            Assert.That(manager.GetStatistics("queen-1").Level, Is.EqualTo(2));
        }

        [Test]
        public void QueenDeathPublishesEvent()
        {
            EventBus eventBus = new EventBus();
            int deaths = 0;
            eventBus.Subscribe<QueenDied>(_ => deaths++);
            QueenManager manager = new QueenManager(eventBus);
            manager.CreateQueen("queen-1", "hive-1", new QueenHealth(100, 100), 100, 1f, 60f);

            manager.UpdateState("queen-1", QueenState.Dead);

            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotRoundTripsQueen()
        {
            QueenManager manager = CreateActiveQueen(out _);
            manager.AddExperience("queen-1", 50);
            QueenAggregate queen = QueenAggregate.FromSnapshot(manager.GetQueen("queen-1").ToSnapshot());

            Assert.That(queen.QueenId, Is.EqualTo("queen-1"));
            Assert.That(queen.State, Is.EqualTo(QueenState.ActiveQueen));
            Assert.That(queen.Evolution.Experience, Is.EqualTo(50));
        }

        private static QueenManager CreateActiveQueen(out EventBus eventBus)
        {
            eventBus = new EventBus();
            QueenManager manager = new QueenManager(eventBus);
            manager.CreateQueen("queen-1", "hive-1", new QueenHealth(100, 100), 100, 1f, 60f);
            manager.UpdateState("queen-1", QueenState.Larva);
            manager.UpdateState("queen-1", QueenState.Pupa);
            manager.UpdateState("queen-1", QueenState.VirginQueen);
            manager.UpdateState("queen-1", QueenState.MatedQueen);
            manager.UpdateState("queen-1", QueenState.ActiveQueen);
            return manager;
        }
    }

}
