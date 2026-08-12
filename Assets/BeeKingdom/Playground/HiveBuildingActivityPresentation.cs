using System;
using System.Collections.Generic;

namespace BeeKingdom.Playground
{
    public enum HiveBuildingActivityKind
    {
        NectarStorage,
        WaxCrafting,
        PollenSorting,
        BroodCare,
        GuardPatrol,
        ChamberMaintenance
    }

    public sealed class HiveBuildingActivityDefinition
    {
        public HiveBuildingActivityDefinition(
            string hotspotId,
            HiveBuildingActivityKind kind,
            string localizationKey,
            string iconId,
            int signalCount,
            double motionSpeed,
            bool guardBee)
        {
            HotspotId = hotspotId ?? string.Empty;
            Kind = kind;
            LocalizationKey = localizationKey ?? string.Empty;
            IconId = iconId ?? string.Empty;
            SignalCount = Math.Max(1, Math.Min(3, signalCount));
            MotionSpeed = Math.Max(0.01d, Math.Min(1d, motionSpeed));
            GuardBee = guardBee;
        }

        public string HotspotId { get; }
        public HiveBuildingActivityKind Kind { get; }
        public string LocalizationKey { get; }
        public string IconId { get; }
        public int SignalCount { get; }
        public double MotionSpeed { get; }
        public bool GuardBee { get; }
    }

    public static class HiveBuildingActivityCatalog
    {
        private static readonly HiveBuildingActivityDefinition[] Entries =
        {
            new HiveBuildingActivityDefinition("honey_storage", HiveBuildingActivityKind.NectarStorage, "building.activity.honey", "honey", 2, 0.16d, false),
            new HiveBuildingActivityDefinition("wax_workshop", HiveBuildingActivityKind.WaxCrafting, "building.activity.wax", "wax", 2, 0.13d, false),
            new HiveBuildingActivityDefinition("warehouse_cells", HiveBuildingActivityKind.PollenSorting, "building.activity.pollen", "pollen", 3, 0.15d, false),
            new HiveBuildingActivityDefinition("nursery_cluster", HiveBuildingActivityKind.BroodCare, "building.activity.nursery", "brood", 3, 0.11d, false),
            new HiveBuildingActivityDefinition("guard_post", HiveBuildingActivityKind.GuardPatrol, "building.activity.guard", "guard-bee", 2, 0.18d, true)
        };

        private static readonly HiveBuildingActivityDefinition Fallback =
            new HiveBuildingActivityDefinition("*", HiveBuildingActivityKind.ChamberMaintenance, "building.activity.chamber", "worker-bee", 1, 0.10d, false);

        public static IReadOnlyList<HiveBuildingActivityDefinition> All => Entries;

        public static HiveBuildingActivityDefinition Resolve(string hotspotId)
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                if (string.Equals(Entries[i].HotspotId, hotspotId, StringComparison.Ordinal)) return Entries[i];
            }
            return Fallback;
        }
    }
}
