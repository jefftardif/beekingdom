using BeeKingdom.Hive;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeLifecycleTests
    {
        [Test]
        public void CreateBeePublishesBornEvent()
        {
            EventBus eventBus = new EventBus();
            int born = 0;
            eventBus.Subscribe<BeeBorn>(_ => born++);
            BeeLifecycleManager manager = new BeeLifecycleManager(CreateRules(), eventBus);

            BeeLifecycleBee bee = manager.CreateBee("bee-1", "hive-1", 0d, BeeLifecycleRole.Worker, 100, 100, "gen-a");

            Assert.That(bee.CurrentStage, Is.EqualTo(BeeLifecycleStage.Egg));
            Assert.That(born, Is.EqualTo(1));
        }

        [Test]
        public void AdvanceLifecycleTransitionsByConfiguredAge()
        {
            BeeLifecycleManager manager = new BeeLifecycleManager(CreateRules());
            manager.CreateBee("bee-1", "hive-1", 0d, BeeLifecycleRole.Worker, 100, 100, "gen-a");

            manager.AdvanceLifecycle("bee-1", 10d);

            Assert.That(manager.GetBee("bee-1").CurrentStage, Is.EqualTo(BeeLifecycleStage.Larva));
        }

        [Test]
        public void InvalidTransitionIsRejected()
        {
            BeeLifecycleManager manager = new BeeLifecycleManager(CreateRules());
            manager.CreateBee("bee-1", "hive-1", 0d, BeeLifecycleRole.Worker, 100, 100, "gen-a");

            Assert.That(manager.ChangeStage("bee-1", BeeLifecycleStage.AdultWorker), Is.False);
        }

        [Test]
        public void ChangeRolePublishesEvent()
        {
            EventBus eventBus = new EventBus();
            int roleChanges = 0;
            eventBus.Subscribe<BeeRoleChanged>(_ => roleChanges++);
            BeeLifecycleManager manager = new BeeLifecycleManager(CreateRules(), eventBus);
            manager.CreateBee("bee-1", "hive-1", 0d, BeeLifecycleRole.Worker, 100, 100, "gen-a");

            Assert.That(manager.ChangeRole("bee-1", BeeLifecycleRole.Nurse), Is.True);
            Assert.That(roleChanges, Is.EqualTo(1));
            Assert.That(manager.GetBee("bee-1").CurrentRole, Is.EqualTo(BeeLifecycleRole.Nurse));
        }

        [Test]
        public void OldAgeKillsBee()
        {
            EventBus eventBus = new EventBus();
            int deaths = 0;
            eventBus.Subscribe<BeeDied>(_ => deaths++);
            BeeLifecycleManager manager = new BeeLifecycleManager(CreateRules(), eventBus);
            manager.CreateBee("bee-1", "hive-1", 0d, BeeLifecycleRole.Worker, 100, 100, "gen-a");

            manager.AdvanceLifecycle("bee-1", 100d);

            Assert.That(manager.GetBee("bee-1").Alive, Is.False);
            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotRoundTripsBee()
        {
            BeeLifecycleManager manager = new BeeLifecycleManager(CreateRules());
            BeeLifecycleBee bee = manager.CreateBee("bee-1", "hive-1", 0d, BeeLifecycleRole.Worker, 100, 100, "gen-a");
            manager.AdvanceLifecycle("bee-1", 10d);

            BeeLifecycleBee loaded = BeeLifecycleBee.FromSnapshot(bee.ToSnapshot());

            Assert.That(loaded.BeeId, Is.EqualTo("bee-1"));
            Assert.That(loaded.CurrentStage, Is.EqualTo(BeeLifecycleStage.Larva));
            Assert.That(loaded.Age.AgeSeconds, Is.EqualTo(10d));
        }

        [Test]
        public void LifecycleRemainsStableAfterManyCycles()
        {
            BeeLifecycleManager manager = new BeeLifecycleManager(CreateLongLifeRules());
            manager.CreateBee("bee-1", "hive-1", 0d, BeeLifecycleRole.Worker, 100, 100, "gen-a");

            for (int i = 0; i < 100000; i++)
            {
                manager.AdvanceLifecycle("bee-1", 0.01d);
            }

            Assert.That(manager.Diagnostics.LifecycleAdvances, Is.GreaterThan(100000));
            Assert.That(manager.Validate("bee-1"), Is.True);
        }

        private static BeeLifecycleRules CreateRules()
        {
            return new BeeLifecycleRules(
                new BeeDevelopmentProfile(10d, 20d, 30d, 40d, 50d),
                new BeeMortalityProfile(60d));
        }

        private static BeeLifecycleRules CreateLongLifeRules()
        {
            return new BeeLifecycleRules(
                new BeeDevelopmentProfile(10d, 20d, 30d, 40d, 50d),
                new BeeMortalityProfile(1000000d));
        }
    }
}
