using BeeKingdom.Core.Abilities;
using BeeKingdom.Core.Effects;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class GameplayEffectsCoreTests
    {
        [Test]
        public void RegisterEffectMakesItQueryable()
        {
            GameplayEffectManager manager = new GameplayEffectManager();

            Assert.That(manager.RegisterEffect(Definition("queen-bonus", GameplayEffectType.Duration)), Is.True);

            Assert.That(manager.QueryEffects(new GameplayAbilityTag("Effect.Queen")).Count, Is.EqualTo(1));
        }

        [Test]
        public void ApplyEffectCreatesActiveInstance()
        {
            GameplayEffectManager manager = new GameplayEffectManager();
            manager.RegisterEffect(Definition("rain", GameplayEffectType.Duration));

            GameplayEffectResult result = manager.ApplyEffect("rain", Context(), out GameplayEffectHandle handle);

            Assert.That(result.Success, Is.True);
            Assert.That(handle.IsValid, Is.True);
            Assert.That(manager.GetInstance(handle).State, Is.EqualTo(GameplayEffectState.Active));
        }

        [Test]
        public void RemoveEffectMovesToRemoved()
        {
            GameplayEffectManager manager = new GameplayEffectManager();
            manager.RegisterEffect(Definition("disease", GameplayEffectType.Infinite));
            manager.ApplyEffect("disease", Context(), out GameplayEffectHandle handle);

            GameplayEffectResult result = manager.RemoveEffect(handle);

            Assert.That(result.Success, Is.True);
            Assert.That(manager.GetInstance(handle).State, Is.EqualTo(GameplayEffectState.Removed));
        }

        [Test]
        public void DurationEffectExpiresWhenTicked()
        {
            GameplayEffectManager manager = new GameplayEffectManager();
            manager.RegisterEffect(Definition("season", GameplayEffectType.Duration, 10d));
            manager.ApplyEffect("season", Context(), out GameplayEffectHandle handle);

            manager.Tick(10d);

            Assert.That(manager.GetInstance(handle).State, Is.EqualTo(GameplayEffectState.Expired));
            Assert.That(manager.Diagnostics.ExpiredEffects, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotCapturesActiveEffect()
        {
            GameplayEffectManager manager = new GameplayEffectManager();
            manager.RegisterEffect(Definition("liveops", GameplayEffectType.Global));
            manager.ApplyEffect("liveops", Context(), out GameplayEffectHandle handle);

            GameplayEffectSnapshot snapshot = manager.CreateSnapshot(handle);

            Assert.That(snapshot.Handle, Is.EqualTo(handle.Value));
            Assert.That(snapshot.EffectId, Is.EqualTo("liveops"));
            Assert.That(snapshot.State, Is.EqualTo(GameplayEffectState.Active));
        }

        [Test]
        public void HandlesAreDeterministicForSameApplyOrder()
        {
            GameplayEffectHandle first = FirstHandle();
            GameplayEffectHandle second = FirstHandle();

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void HandlesLargeEffectSet()
        {
            GameplayEffectManager manager = new GameplayEffectManager();
            manager.RegisterEffect(Definition("aura", GameplayEffectType.Aura));
            for (int i = 0; i < 100000; i++)
            {
                manager.ApplyEffect("aura", Context(), out GameplayEffectHandle handle);
                Assert.That(handle.IsValid, Is.True);
            }

            Assert.That(manager.Diagnostics.AppliedEffects, Is.EqualTo(100000));
        }

        private static GameplayEffectHandle FirstHandle()
        {
            GameplayEffectManager manager = new GameplayEffectManager();
            manager.RegisterEffect(Definition("bonus", GameplayEffectType.Duration));
            manager.ApplyEffect("bonus", Context(), out GameplayEffectHandle handle);
            return handle;
        }

        private static GameplayEffectDefinition Definition(string id, GameplayEffectType type, double duration = 60d)
        {
            return new GameplayEffectDefinition(id, id, "test", type, duration, 0d, new[] { new GameplayAbilityTag("Effect.Queen.Bonus") });
        }

        private static GameplayEffectContext Context()
        {
            return new GameplayEffectContext("source", "target", "world", "region", "player", "alliance", 10d, 42);
        }
    }
}
