using BeeKingdom.Gameplay.Progression;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class PlayerSkillTreeTests
    {
        private static SkillCatalog Catalog => SkillCatalog.CreateDefault();

        [Test]
        public void ExperienceLevelsAwardOnePointPerLevel()
        {
            var state = new PlayerSkillState(Catalog);
            int levels = state.AddExperience((int)PlayerXpCurve.CumulativeXpForLevel(10));

            Assert.That(levels, Is.EqualTo(9));
            Assert.That(state.Level, Is.EqualTo(10));
            Assert.That(state.SkillPointsAwarded, Is.EqualTo(10));
            Assert.That(state.SkillPointsUnspent, Is.EqualTo(10));
        }

        [Test]
        public void TreesRemainLockedBeforeLevelTen()
        {
            var state = new PlayerSkillState(Catalog);

            Assert.That(state.TryChooseClass(PlayerClass.Scout, out _), Is.False);
            Assert.That(state.TryPurchase("combat_foundation", out _), Is.False);
        }

        [Test]
        public void ClassSelectionAndClassTreeAreIsolated()
        {
            var state = PlayerSkillState.CreateLocalPreview(Catalog, 10, PlayerClass.Scout);

            Assert.That(state.ClassId, Is.EqualTo(PlayerClass.Scout));
            Assert.That(state.TryPurchase("scout_foundation", out _), Is.True);
            Assert.That(state.TryPurchase("striker_foundation", out _), Is.False);
        }

        [Test]
        public void PrerequisitesAndPointBudgetAreEnforced()
        {
            var state = PlayerSkillState.CreateLocalPreview(Catalog, 10, PlayerClass.RoyalGuard);

            Assert.That(state.TryPurchase("combat_command", out _), Is.False);
            Assert.That(state.TryPurchase("combat_foundation", out _), Is.True);
            Assert.That(state.TryPurchase("combat_command", out _), Is.True);
            Assert.That(state.SkillPointsSpent, Is.EqualTo(2));
            Assert.That(state.GetRank("combat_command"), Is.EqualTo(1));
        }

        [Test]
        public void ResetRestoresTheFullBudgetAndProfile()
        {
            var state = PlayerSkillState.CreateLocalPreview(Catalog, 10, PlayerClass.Alchemist);
            Assert.That(state.TryPurchase("combat_foundation", out _), Is.True);
            Assert.That(state.TryPurchase("alchemist_foundation", out _), Is.True);
            Assert.That(state.BuildProfile().GetBonus("combat.damage_percent"), Is.GreaterThan(0f));

            Assert.That(state.TryResetSkills(out _), Is.True);
            Assert.That(state.SkillPointsSpent, Is.EqualTo(0));
            Assert.That(state.SkillPointsUnspent, Is.EqualTo(10));
            Assert.That(state.BuildProfile().Bonuses, Is.Empty);
        }

        [Test]
        public void LocalPreviewCanChangeLevelAndClassWithoutOfficialAuthority()
        {
            var state = PlayerSkillState.CreateLocalPreview(Catalog, 12, PlayerClass.Scout);

            Assert.That(state.IsLocalPreview, Is.True);
            Assert.That(state.TrySetLocalClass(PlayerClass.Nurturer, out _), Is.True);
            Assert.That(state.TrySetLocalLevel(8, out _), Is.True);

            Assert.That(state.Level, Is.EqualTo(8));
            Assert.That(state.ClassId, Is.EqualTo(PlayerClass.Neutral));
            Assert.That(state.SkillPointsUnspent, Is.EqualTo(8));
        }
    }
}
