using BeeKingdom.Gameplay.Progression;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class PlayerSkillTreeUiModelTests
    {
        [Test]
        public void LevelNineShowsThreeTabsButClassBranchIsLocked()
        {
            PlayerSkillState state = PlayerSkillState.CreateLocalPreview(
                SkillCatalog.CreateDefault(),
                9,
                PlayerClass.Neutral);

            PlayerSkillTreeView view = PlayerSkillTreeView.Build(state);

            Assert.That(view.Tabs.Count, Is.EqualTo(3));
            SkillTreeTabView classTab = view.Tabs[2];
            Assert.That(classTab.IsLocked, Is.True);
            Assert.That(classTab.LockReason, Does.Contain("level 10"));
        }

        [Test]
        public void SelectedClassOnlyExposesItsClassBranch()
        {
            PlayerSkillState state = PlayerSkillState.CreateLocalPreview(
                SkillCatalog.CreateDefault(),
                10,
                PlayerClass.Striker);

            PlayerSkillTreeView view = PlayerSkillTreeView.Build(state);

            SkillTreeTabView classTab = view.Tabs[2];
            Assert.That(classTab.IsLocked, Is.False);
            Assert.That(classTab.Nodes.Count, Is.EqualTo(5));
            Assert.That(classTab.Nodes, Has.All.Property("Definition").Property("ClassId").EqualTo(PlayerClass.Striker));
        }

        [Test]
        public void PurchasedNodeAndPrerequisiteStateAreReflected()
        {
            PlayerSkillState state = PlayerSkillState.CreateLocalPreview(
                SkillCatalog.CreateDefault(),
                10,
                PlayerClass.RoyalGuard);
            Assert.That(state.TryPurchase("combat_foundation", out string error), Is.True, error);

            PlayerSkillTreeView view = PlayerSkillTreeView.Build(state);
            SkillTreeTabView combat = view.Tabs[0];
            SkillTreeNodeView foundation = Find(combat, "combat_foundation");
            SkillTreeNodeView command = Find(combat, "combat_command");

            Assert.That(foundation.Availability, Is.EqualTo(SkillNodeAvailability.Purchased));
            Assert.That(command.Availability, Is.EqualTo(SkillNodeAvailability.Available));
        }

        private static SkillTreeNodeView Find(SkillTreeTabView tab, string skillId)
        {
            foreach (SkillTreeNodeView node in tab.Nodes)
            {
                if (node.Definition.SkillId == skillId) return node;
            }

            Assert.Fail("Missing skill node: " + skillId);
            return null;
        }
    }
}
