using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeePersonalityFrameworkTests
    {
        [Test]
        public void GeneratePersonalityIsDeterministic()
        {
            BeePersonalityManager first = CreateManager();
            BeePersonalityManager second = CreateManager();

            PersonalityProfile left = first.GeneratePersonality("worker-personality", new PersonalityContext("bee-1"), 42);
            PersonalityProfile right = second.GeneratePersonality("worker-personality", new PersonalityContext("bee-1"), 42);

            Assert.That(left.Values["curiosity"], Is.EqualTo(right.Values["curiosity"]));
        }

        [Test]
        public void UpdatePersonalityRecordsEvolution()
        {
            BeePersonalityManager manager = CreateManager();
            manager.GeneratePersonality("worker-personality", new PersonalityContext("bee-1"), 1);

            Assert.That(manager.UpdatePersonality(new PersonalityContext("bee-1", experienceFactor: 1d, environmentFactor: 1d)), Is.True);

            Assert.That(manager.QueryPersonality("bee-1").EvolutionHistory.Count, Is.GreaterThan(0));
        }

        [Test]
        public void BehaviorModifiersAreCalculated()
        {
            BeePersonalityManager manager = CreateManager();
            manager.GeneratePersonality("worker-personality", new PersonalityContext("bee-1"), 5);

            Assert.That(manager.CalculateBehaviorModifiers("bee-1").ContainsKey("curiosity"), Is.True);
        }

        [Test]
        public void ResetPersonalityRemovesProfile()
        {
            BeePersonalityManager manager = CreateManager();
            manager.GeneratePersonality("worker-personality", new PersonalityContext("bee-1"), 5);

            Assert.That(manager.ResetPersonality("bee-1"), Is.True);
            Assert.That(manager.QueryPersonality("bee-1"), Is.Null);
        }

        private static BeePersonalityManager CreateManager()
        {
            BeePersonalityManager manager = new BeePersonalityManager();
            manager.RegisterTraitDefinition(new PersonalityDefinition(
                "worker-personality",
                new[]
                {
                    new PersonalityTrait("curiosity", PersonalityTraitKind.Curiosity, 0d, 1d, 0.4d, 0.2d, 0.3d, 0.05d),
                    new PersonalityTrait("discipline", PersonalityTraitKind.Discipline, 0d, 1d, 0.4d, 0.3d, 0.2d, 0.05d),
                    new PersonalityTrait("risk", PersonalityTraitKind.RiskTolerance, 0d, 1d, 0.5d, 0.2d, 0.2d, 0.05d)
                }));
            return manager;
        }
    }
}
