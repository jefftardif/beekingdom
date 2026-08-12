using BeeKingdom.Core.Save;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class FirstPlayableHiveTests
    {
        [Test]
        public void NewGameCreatesPlayableColony()
        {
            PlayableHiveState state = CreateState();

            Assert.That(state.HiveId, Is.EqualTo("test-hive"));
            Assert.That(state.BeeIds.Count, Is.GreaterThan(1));
            Assert.That(state.GrowthManager.GetLayout().Chambers.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(state.InventoryManager.QueryInventory().TotalAmount, Is.GreaterThan(0d));
        }

        [Test]
        public void SimulatesTwentyFourHours()
        {
            PlayableHiveState state = CreateState();

            Simulate(state, 24, 3600d);

            Assert.That(state.Diagnostics.SimulatedSeconds, Is.EqualTo(86400d));
            Assert.That(state.BeeIds.Count, Is.GreaterThan(StarterPopulationProfile.CreateDefault().TotalBees));
            Assert.That(state.Diagnostics.ErrorCount, Is.EqualTo(0));
        }

        [Test]
        public void AcceleratedSimulationAdvancesFaster()
        {
            StarterHiveProfile fastProfile = CreateHiveProfile(4d);
            PlayableHiveState state = new PlayableHiveBootstrap().StartNewGame(fastProfile, StarterPopulationProfile.CreateDefault(), StarterResourceProfile.CreateDefault());

            state.Controller.Execute(SimulationContextFactory.Create(60d));

            Assert.That(state.Diagnostics.SimulatedSeconds, Is.EqualTo(240d));
        }

        [Test]
        public void SaveAndLoadRoundTripsPlayablePayload()
        {
            PlayableHiveState state = CreateState();
            SaveEngine save = new SaveEngine(new InMemorySaveRepository());

            new PlayableHiveBootstrap().Save(save, state, "playable");

            Assert.That(save.TryLoad("playable", out SaveSnapshot snapshot), Is.True);
            Assert.That(snapshot.Payload, Does.Contain("hive=test-hive"));
            Assert.That(snapshot.Payload, Does.Contain("population="));
        }

        [Test]
        public void LongSessionRemainsStable()
        {
            PlayableHiveState state = CreateState();

            Simulate(state, 240, 60d);

            Assert.That(state.Diagnostics.ErrorCount, Is.EqualTo(0));
            Assert.That(state.BeeIds.Count, Is.LessThan(256));
            Assert.That(state.Diagnostics.TotalResources, Is.GreaterThan(0d));
        }

        private static PlayableHiveState CreateState()
        {
            return new PlayableHiveBootstrap().StartNewGame(CreateHiveProfile(1d), StarterPopulationProfile.CreateDefault(), StarterResourceProfile.CreateDefault());
        }

        private static StarterHiveProfile CreateHiveProfile(double speed)
        {
            return new StarterHiveProfile(
                "test-hive",
                "test-player",
                "queen-test",
                new HiveCapacity(256, 64, 64),
                1,
                0.01f,
                speed,
                new[] { HiveChamberType.Entrance, HiveChamberType.RoyalChamber, HiveChamberType.Nursery },
                new[] { "starter-beekeeping" });
        }

        private static void Simulate(PlayableHiveState state, int steps, double stepSeconds)
        {
            for (int i = 0; i < steps; i++)
            {
                state.Controller.Execute(SimulationContextFactory.Create(stepSeconds));
            }
        }
    }
}
