using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Gameplay.Progression
{
    public enum SkillNodeAvailability
    {
        LockedByLevel,
        LockedByClass,
        LockedByPrerequisite,
        Available,
        Purchased,
        Maxed
    }

    public sealed class SkillTreeNodeView
    {
        public SkillDefinition Definition { get; }
        public int CurrentRank { get; }
        public SkillNodeAvailability Availability { get; }
        public bool CanPurchase => Availability == SkillNodeAvailability.Available;
        public string LockReason { get; }

        internal SkillTreeNodeView(
            SkillDefinition definition,
            int currentRank,
            SkillNodeAvailability availability,
            string lockReason)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentRank = currentRank;
            Availability = availability;
            LockReason = lockReason ?? string.Empty;
        }
    }

    public sealed class SkillTreeTabView
    {
        public SkillTreeId TreeId { get; }
        public string Title { get; }
        public bool IsLocked { get; }
        public string LockReason { get; }
        public IReadOnlyList<SkillTreeNodeView> Nodes { get; }

        internal SkillTreeTabView(
            SkillTreeId treeId,
            string title,
            bool isLocked,
            string lockReason,
            IReadOnlyList<SkillTreeNodeView> nodes)
        {
            TreeId = treeId;
            Title = title ?? string.Empty;
            IsLocked = isLocked;
            LockReason = lockReason ?? string.Empty;
            Nodes = nodes ?? Array.Empty<SkillTreeNodeView>();
        }
    }

    public sealed class PlayerSkillTreeView
    {
        public const int ClassUnlockLevel = PlayerSkillState.ClassUnlockLevel;
        public PlayerClass PlayerClass { get; }
        public int PlayerLevel { get; }
        public int SkillPointsAvailable { get; }
        public IReadOnlyList<SkillTreeTabView> Tabs { get; }

        private PlayerSkillTreeView(
            PlayerClass playerClass,
            int playerLevel,
            int skillPointsAvailable,
            IReadOnlyList<SkillTreeTabView> tabs)
        {
            PlayerClass = playerClass;
            PlayerLevel = playerLevel;
            SkillPointsAvailable = skillPointsAvailable;
            Tabs = tabs;
        }

        public static PlayerSkillTreeView Build(PlayerSkillState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var tabs = new List<SkillTreeTabView>
            {
                BuildTab(state, SkillTreeId.Combat, "Combat"),
                BuildTab(state, SkillTreeId.Resources, "Resources / Evolution"),
                BuildTab(state, SkillTreeId.Class, "Class")
            };

            return new PlayerSkillTreeView(
                state.ClassId,
                state.Level,
                state.SkillPointsUnspent,
                tabs);
        }

        private static SkillTreeTabView BuildTab(PlayerSkillState state, SkillTreeId treeId, string title)
        {
            bool classTab = treeId == SkillTreeId.Class;
            bool locked = classTab && state.Level < ClassUnlockLevel;
            string tabLockReason = locked
                ? "Choose a class at level 10 to unlock this branch."
                : string.Empty;

            var nodes = state.Catalog.Definitions
                .Where(definition => definition.TreeId == treeId)
                .Where(definition => !classTab || definition.IsAvailableFor(state.ClassId))
                .OrderBy(definition => definition.RequiredLevel)
                .ThenBy(definition => definition.SkillId, StringComparer.Ordinal)
                .Select(definition => BuildNode(state, definition, locked))
                .ToList();

            return new SkillTreeTabView(treeId, title, locked, tabLockReason, nodes);
        }

        private static SkillTreeNodeView BuildNode(
            PlayerSkillState state,
            SkillDefinition definition,
            bool classTabLocked)
        {
            int currentRank = state.GetRank(definition.SkillId);
            if (currentRank >= definition.MaxRank)
            {
                return new SkillTreeNodeView(definition, currentRank, SkillNodeAvailability.Maxed, string.Empty);
            }

            if (classTabLocked)
            {
                return new SkillTreeNodeView(
                    definition,
                    currentRank,
                    SkillNodeAvailability.LockedByClass,
                    "Choose a class at level 10.");
            }

            if (state.Level < definition.RequiredLevel)
            {
                return new SkillTreeNodeView(
                    definition,
                    currentRank,
                    SkillNodeAvailability.LockedByLevel,
                    "Requires level " + definition.RequiredLevel + ".");
            }

            foreach (string prerequisiteId in definition.PrerequisiteSkillIds)
            {
                if (state.GetRank(prerequisiteId) < 1)
                {
                    return new SkillTreeNodeView(
                        definition,
                        currentRank,
                        SkillNodeAvailability.LockedByPrerequisite,
                        "Requires " + prerequisiteId + ".");
                }
            }

            if (currentRank > 0)
            {
                return new SkillTreeNodeView(definition, currentRank, SkillNodeAvailability.Purchased, string.Empty);
            }

            if (!definition.IsAvailableFor(state.ClassId))
            {
                return new SkillTreeNodeView(
                    definition,
                    currentRank,
                    SkillNodeAvailability.LockedByClass,
                    "This skill belongs to another class.");
            }

            if (state.SkillPointsUnspent < definition.CostPerRank)
            {
                return new SkillTreeNodeView(
                    definition,
                    currentRank,
                    SkillNodeAvailability.LockedByPrerequisite,
                    "Not enough skill points.");
            }

            return new SkillTreeNodeView(definition, currentRank, SkillNodeAvailability.Available, string.Empty);
        }
    }
}
