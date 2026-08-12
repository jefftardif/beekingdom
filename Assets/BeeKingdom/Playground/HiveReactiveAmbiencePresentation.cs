using System;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public enum HiveReactiveAmbienceKind
    {
        Neutral,
        UpgradeActive,
        ProductionFull
    }

    public readonly struct HiveReactiveAmbienceState
    {
        public HiveReactiveAmbienceState(HiveReactiveAmbienceKind kind, string zoneId)
        {
            Kind = kind;
            ZoneId = zoneId ?? string.Empty;
        }

        public HiveReactiveAmbienceKind Kind { get; }
        public string ZoneId { get; }
        public bool IsVisible => Kind != HiveReactiveAmbienceKind.Neutral && !string.IsNullOrWhiteSpace(ZoneId);
    }

    public sealed class HiveReactiveAmbienceDefinition
    {
        public HiveReactiveAmbienceDefinition(
            HiveReactiveAmbienceKind kind,
            string iconId,
            int cueCount,
            double motionSpeed,
            Color accent)
        {
            Kind = kind;
            IconId = iconId ?? string.Empty;
            CueCount = Mathf.Clamp(cueCount, 0, 3);
            MotionSpeed = Math.Max(0d, Math.Min(1d, motionSpeed));
            Accent = accent;
        }

        public HiveReactiveAmbienceKind Kind { get; }
        public string IconId { get; }
        public int CueCount { get; }
        public double MotionSpeed { get; }
        public Color Accent { get; }

        public int VisibleCueCount(bool economyMode)
        {
            if (CueCount == 0) return 0;
            return economyMode ? 1 : CueCount;
        }

        public float MotionPhase(float time, int cueIndex, bool reducedMotion)
        {
            if (CueCount == 0) return 0f;
            int boundedIndex = Mathf.Clamp(cueIndex, 0, CueCount - 1);
            if (reducedMotion) return (boundedIndex + 1f) / (CueCount + 1f);
            return Mathf.Repeat(time * (float)MotionSpeed + boundedIndex / (float)CueCount, 1f);
        }
    }

    public static class HiveReactiveAmbienceCatalog
    {
        private static readonly HiveReactiveAmbienceDefinition Neutral =
            new HiveReactiveAmbienceDefinition(
                HiveReactiveAmbienceKind.Neutral,
                string.Empty,
                0,
                0d,
                Color.clear);

        private static readonly HiveReactiveAmbienceDefinition Upgrade =
            new HiveReactiveAmbienceDefinition(
                HiveReactiveAmbienceKind.UpgradeActive,
                "wax",
                3,
                0.14d,
                new Color(1f, 0.72f, 0.12f, 0.82f));

        private static readonly HiveReactiveAmbienceDefinition Full =
            new HiveReactiveAmbienceDefinition(
                HiveReactiveAmbienceKind.ProductionFull,
                "honey",
                2,
                0.10d,
                new Color(0.98f, 0.52f, 0.08f, 0.78f));

        public static HiveReactiveAmbienceState Resolve(string upgradeZoneId, string fullProductionZoneId)
        {
            if (!string.IsNullOrWhiteSpace(upgradeZoneId))
                return new HiveReactiveAmbienceState(HiveReactiveAmbienceKind.UpgradeActive, upgradeZoneId);
            if (IsProductionZone(fullProductionZoneId))
                return new HiveReactiveAmbienceState(HiveReactiveAmbienceKind.ProductionFull, fullProductionZoneId);
            return new HiveReactiveAmbienceState(HiveReactiveAmbienceKind.Neutral, string.Empty);
        }

        public static HiveReactiveAmbienceDefinition DefinitionFor(HiveReactiveAmbienceKind kind)
        {
            switch (kind)
            {
                case HiveReactiveAmbienceKind.UpgradeActive: return Upgrade;
                case HiveReactiveAmbienceKind.ProductionFull: return Full;
                default: return Neutral;
            }
        }

        public static bool IsProductionZone(string zoneId)
        {
            return string.Equals(zoneId, "honey_storage", StringComparison.Ordinal)
                || string.Equals(zoneId, "wax_workshop", StringComparison.Ordinal)
                || string.Equals(zoneId, "warehouse_cells", StringComparison.Ordinal);
        }
    }
}
