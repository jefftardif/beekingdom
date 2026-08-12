using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public enum HiveSpecializedBeeCueKind
    {
        NectarCarry,
        WaxShaping,
        PollenSorting,
        BroodNursing,
        GuardPatrol
    }

    public sealed class HiveSpecializedBeeActivityDefinition
    {
        public HiveSpecializedBeeActivityDefinition(
            string zoneId,
            HiveSpecializedBeeCueKind kind,
            string resourceIconId,
            int cueCount,
            double motionSpeed,
            Color accent)
        {
            ZoneId = zoneId ?? string.Empty;
            Kind = kind;
            ResourceIconId = resourceIconId ?? string.Empty;
            CueCount = Mathf.Clamp(cueCount, 1, 3);
            MotionSpeed = Math.Max(0.01d, Math.Min(1d, motionSpeed));
            Accent = accent;
        }

        public string ZoneId { get; }
        public HiveSpecializedBeeCueKind Kind { get; }
        public string ResourceIconId { get; }
        public int CueCount { get; }
        public double MotionSpeed { get; }
        public Color Accent { get; }

        public int VisibleCueCount(bool economyMode)
        {
            return economyMode ? 1 : CueCount;
        }

        public float MotionPhase(float time, int cueIndex, bool reducedMotion)
        {
            int boundedIndex = Mathf.Clamp(cueIndex, 0, CueCount - 1);
            if (reducedMotion) return (boundedIndex + 1f) / (CueCount + 1f);
            return Mathf.Repeat(time * (float)MotionSpeed + boundedIndex / (float)CueCount, 1f);
        }
    }

    public static class HiveSpecializedBeeActivityCatalog
    {
        private static readonly HiveSpecializedBeeActivityDefinition[] Entries =
        {
            new HiveSpecializedBeeActivityDefinition(
                "honey_storage",
                HiveSpecializedBeeCueKind.NectarCarry,
                "honey",
                2,
                0.22d,
                new Color(1f, 0.66f, 0.08f, 0.88f)),
            new HiveSpecializedBeeActivityDefinition(
                "wax_workshop",
                HiveSpecializedBeeCueKind.WaxShaping,
                "wax",
                2,
                0.16d,
                new Color(1f, 0.82f, 0.28f, 0.88f)),
            new HiveSpecializedBeeActivityDefinition(
                "warehouse_cells",
                HiveSpecializedBeeCueKind.PollenSorting,
                "pollen",
                3,
                0.19d,
                new Color(0.72f, 0.92f, 0.20f, 0.88f)),
            new HiveSpecializedBeeActivityDefinition(
                "nursery_cluster",
                HiveSpecializedBeeCueKind.BroodNursing,
                "brood",
                3,
                0.13d,
                new Color(0.34f, 0.92f, 0.58f, 0.88f)),
            new HiveSpecializedBeeActivityDefinition(
                "guard_post",
                HiveSpecializedBeeCueKind.GuardPatrol,
                "guard-bee",
                2,
                0.24d,
                new Color(0.98f, 0.34f, 0.16f, 0.90f))
        };

        public static IReadOnlyList<HiveSpecializedBeeActivityDefinition> All => Entries;

        public static bool TryResolve(string zoneId, out HiveSpecializedBeeActivityDefinition definition)
        {
            for (int index = 0; index < Entries.Length; index++)
            {
                if (!string.Equals(Entries[index].ZoneId, zoneId, StringComparison.Ordinal)) continue;
                definition = Entries[index];
                return true;
            }

            definition = null;
            return false;
        }
    }
}
