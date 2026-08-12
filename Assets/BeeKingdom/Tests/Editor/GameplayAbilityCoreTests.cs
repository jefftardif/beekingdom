using BeeKingdom.Core.Abilities;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class GameplayAbilityCoreTests
    {
        [Test]
        public void RegisterAbilityMakesItQueryableByHierarchicalTag()
        {
            GameplayAbilityManager manager = new GameplayAbilityManager();

            Assert.That(manager.RegisterAbility(Definition("build", "Ability.Bee.Build")), Is.True);

            Assert.That(manager.QueryAbilities(new GameplayAbilityTag("Ability.Bee")).Count, Is.EqualTo(1));
        }

        [Test]
        public void ActivationMovesThroughLifecycle()
        {
            GameplayAbilityManager manager = new GameplayAbilityManager();
            manager.RegisterAbility(Definition("lay-egg", "Ability.Queen.LayEgg"));

            manager.RequestActivation("lay-egg", Context(), out GameplayAbilityHandle handle);
            manager.Validate(handle);
            manager.Activate(handle);
            GameplayAbilityResult result = manager.Complete(handle);

            Assert.That(result.Success, Is.True);
            Assert.That(manager.GetInstance(handle).State, Is.EqualTo(GameplayAbilityState.Completed));
        }

        [Test]
        public void CancelStopsInstance()
        {
            GameplayAbilityManager manager = new GameplayAbilityManager();
            manager.RegisterAbility(Definition("harvest", "Ability.Bee.Harvest"));
            manager.RequestActivation("harvest", Context(), out GameplayAbilityHandle handle);

            GameplayAbilityResult result = manager.Cancel(handle);

            Assert.That(result.Success, Is.True);
            Assert.That(manager.GetInstance(handle).State, Is.EqualTo(GameplayAbilityState.Cancelled));
        }

        [Test]
        public void InterruptStopsInstance()
        {
            GameplayAbilityManager manager = new GameplayAbilityManager();
            manager.RegisterAbility(Definition("weather", "Ability.World.Weather"));
            manager.RequestActivation("weather", Context(), out GameplayAbilityHandle handle);

            GameplayAbilityResult result = manager.Interrupt(handle);

            Assert.That(result.Success, Is.True);
            Assert.That(manager.GetInstance(handle).State, Is.EqualTo(GameplayAbilityState.Interrupted));
        }

        [Test]
        public void HandlesAreDeterministicForSameActivationOrder()
        {
            GameplayAbilityHandle first = ActivateFirstHandle();
            GameplayAbilityHandle second = ActivateFirstHandle();

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void SnapshotCapturesStableInstanceData()
        {
            GameplayAbilityManager manager = new GameplayAbilityManager();
            manager.RegisterAbility(Definition("research", "Ability.Alliance.Research"));
            manager.RequestActivation("research", Context(), out GameplayAbilityHandle handle);

            GameplayAbilitySnapshot snapshot = manager.CreateSnapshot(handle);

            Assert.That(snapshot.Handle, Is.EqualTo(handle.Value));
            Assert.That(snapshot.AbilityId, Is.EqualTo("research"));
            Assert.That(snapshot.State, Is.EqualTo(GameplayAbilityState.Requested));
        }

        [Test]
        public void HandlesLargeActivationSet()
        {
            GameplayAbilityManager manager = new GameplayAbilityManager();
            manager.RegisterAbility(Definition("build", "Ability.Bee.Build"));

            for (int i = 0; i < 100000; i++)
            {
                manager.RequestActivation("build", Context(), out GameplayAbilityHandle handle);
                Assert.That(handle.IsValid, Is.True);
            }

            Assert.That(manager.Diagnostics.RequestedAbilities, Is.EqualTo(100000));
        }

        private static GameplayAbilityHandle ActivateFirstHandle()
        {
            GameplayAbilityManager manager = new GameplayAbilityManager();
            manager.RegisterAbility(Definition("build", "Ability.Bee.Build"));
            manager.RequestActivation("build", Context(), out GameplayAbilityHandle handle);
            return handle;
        }

        private static GameplayAbilityDefinition Definition(string id, string tag)
        {
            return new GameplayAbilityDefinition(id, id, "test", 10, new[] { new GameplayAbilityTag(tag) });
        }

        private static GameplayAbilityContext Context()
        {
            return new GameplayAbilityContext("source", new[] { "target" }, "world", 10d, 42, "zone", "alliance", "player", GameplayAbilityActivationSource.Local);
        }
    }
}
