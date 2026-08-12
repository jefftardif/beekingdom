using System.Collections.Generic;
using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeExperienceFrameworkTests
    {
        [Test]
        public void AddExperienceProgressesLevel()
        {
            BeeExperienceManager manager = CreateManager();

            manager.AddExperience("bee-1", ExperienceSource.Construction, 125d);

            Assert.That(manager.QueryLevel("bee-1"), Is.EqualTo(ExperienceLevel.Skilled));
        }

        [Test]
        public void ExperienceBySourceIsTracked()
        {
            BeeExperienceManager manager = CreateManager();

            manager.AddExperience("bee-1", ExperienceSource.Transport, 12d);

            Assert.That(manager.QueryExperience("bee-1").ExperienceBySource[ExperienceSource.Transport], Is.EqualTo(12d));
        }

        [Test]
        public void BonusUsesCurrentLevel()
        {
            BeeExperienceManager manager = CreateManager();
            manager.AddExperience("bee-1", ExperienceSource.Care, 250d);

            Assert.That(manager.CalculateBonus("bee-1"), Is.GreaterThan(0d));
        }

        [Test]
        public void ResetExperienceRestoresNovice()
        {
            BeeExperienceManager manager = CreateManager();
            manager.AddExperience("bee-1", ExperienceSource.Cleaning, 250d);

            Assert.That(manager.ResetExperience("bee-1"), Is.True);

            Assert.That(manager.QueryLevel("bee-1"), Is.EqualTo(ExperienceLevel.Novice));
            Assert.That(manager.QueryExperience("bee-1").TotalExperience, Is.EqualTo(0d));
        }

        private static BeeExperienceManager CreateManager()
        {
            BeeExperienceManager manager = new BeeExperienceManager();
            manager.RegisterExperienceDefinition(new ExperienceDefinition(
                "worker-xp",
                new Dictionary<ExperienceLevel, double>
                {
                    { ExperienceLevel.Novice, 0d },
                    { ExperienceLevel.Apprentice, 50d },
                    { ExperienceLevel.Skilled, 100d },
                    { ExperienceLevel.Experienced, 200d },
                    { ExperienceLevel.Veteran, 400d },
                    { ExperienceLevel.Elite, 800d },
                    { ExperienceLevel.Legendary, 1600d }
                },
                0.03d));
            manager.CreateProfile("bee-1", "worker-xp");
            return manager;
        }
    }
}
