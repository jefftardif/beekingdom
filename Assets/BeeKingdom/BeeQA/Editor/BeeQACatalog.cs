using System;
using System.Collections.Generic;

namespace BeeKingdom.BeeQA
{
    public enum BeeQACategory
    {
        Gameplay,
        Hive,
        World,
        Alliance,
        Economy,
        SpeedUps,
        Inventory,
        Rewards,
        Research,
        Buildings,
        Notifications,
        Performance,
        Networking,
        Save,
        UI,
        Graphics,
        Audio,
        Automation
    }

    public readonly struct BeeQACategoryDefinition
    {
        public BeeQACategory Id { get; }
        public string DisplayName { get; }

        public BeeQACategoryDefinition(BeeQACategory id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }
    }

    public static class BeeQACatalog
    {
        private static readonly BeeQACategoryDefinition[] definitions =
        {
            new BeeQACategoryDefinition(BeeQACategory.Gameplay, "Gameplay"),
            new BeeQACategoryDefinition(BeeQACategory.Hive, "Hive"),
            new BeeQACategoryDefinition(BeeQACategory.World, "World"),
            new BeeQACategoryDefinition(BeeQACategory.Alliance, "Alliance"),
            new BeeQACategoryDefinition(BeeQACategory.Economy, "Economy"),
            new BeeQACategoryDefinition(BeeQACategory.SpeedUps, "SpeedUps"),
            new BeeQACategoryDefinition(BeeQACategory.Inventory, "Inventory"),
            new BeeQACategoryDefinition(BeeQACategory.Rewards, "Rewards"),
            new BeeQACategoryDefinition(BeeQACategory.Research, "Research"),
            new BeeQACategoryDefinition(BeeQACategory.Buildings, "Buildings"),
            new BeeQACategoryDefinition(BeeQACategory.Notifications, "Notifications"),
            new BeeQACategoryDefinition(BeeQACategory.Performance, "Performance"),
            new BeeQACategoryDefinition(BeeQACategory.Networking, "Networking"),
            new BeeQACategoryDefinition(BeeQACategory.Save, "Save"),
            new BeeQACategoryDefinition(BeeQACategory.UI, "UI"),
            new BeeQACategoryDefinition(BeeQACategory.Graphics, "Graphics"),
            new BeeQACategoryDefinition(BeeQACategory.Audio, "Audio"),
            new BeeQACategoryDefinition(BeeQACategory.Automation, "Automation")
        };

        public static IReadOnlyList<BeeQACategoryDefinition> Categories => definitions;
    }
}
