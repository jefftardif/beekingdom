using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Gameplay.Progression
{
    public enum SpeedUpCategory
    {
        Universal,
        Construction,
        Research,
        Training,
        Healing,
        Manufacturing
    }

    public enum SpeedUpRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public sealed class SpeedUpItem
    {
        public string Id;
        public SpeedUpCategory Category;
        public long DurationSeconds;
        public int StackSize;
        public SpeedUpRarity Rarity;
        public string Icon;
        public string LocalizedNameKey;
        public string DescriptionKey;

        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
        public string FormattedDuration => FormatDuration(Duration);
        public bool IsUniversal => Category == SpeedUpCategory.Universal;

        private static string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}j";
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}min";
            return $"{ts.Seconds}s";
        }

        public bool CanApplyTo(SpeedUpCategory targetCategory)
        {
            return IsUniversal || Category == targetCategory;
        }
    }

    [Serializable]
    public sealed class SpeedUpStack
    {
        public SpeedUpItem Item;
        public int Count;

        public SpeedUpStack(SpeedUpItem item, int count)
        {
            Item = item;
            Count = count;
        }

        public long TotalDurationSeconds => Item.DurationSeconds * Count;
    }

    public static class SpeedUpRegistry
    {
        private static readonly Dictionary<string, SpeedUpItem> items = new Dictionary<string, SpeedUpItem>(64);
        private static readonly Dictionary<SpeedUpCategory, List<SpeedUpItem>> byCategory = new Dictionary<SpeedUpCategory, List<SpeedUpItem>>();

        static SpeedUpRegistry()
        {
            RegisterDefaults();
        }

        private static void RegisterDefaults()
        {
            long[] durations = {
                60,           // 1m
                5 * 60,       // 5m
                10 * 60,      // 10m
                15 * 60,      // 15m
                30 * 60,      // 30m
                60 * 60,      // 1h
                3 * 3600,     // 3h
                8 * 3600,     // 8h
                12 * 3600,    // 12h
                24 * 3600,    // 24h
                3 * 86400,    // 3d
                7 * 86400,    // 7d
                30 * 86400    // 30d
            };

            SpeedUpRarity[] rarities = {
                SpeedUpRarity.Common,
                SpeedUpRarity.Common,
                SpeedUpRarity.Common,
                SpeedUpRarity.Common,
                SpeedUpRarity.Uncommon,
                SpeedUpRarity.Uncommon,
                SpeedUpRarity.Rare,
                SpeedUpRarity.Rare,
                SpeedUpRarity.Epic,
                SpeedUpRarity.Epic,
                SpeedUpRarity.Legendary,
                SpeedUpRarity.Legendary,
                SpeedUpRarity.Legendary
            };

            string[] icons = {
                "speedup_1m",
                "speedup_5m",
                "speedup_10m",
                "speedup_15m",
                "speedup_30m",
                "speedup_1h",
                "speedup_3h",
                "speedup_8h",
                "speedup_12h",
                "speedup_24h",
                "speedup_3d",
                "speedup_7d",
                "speedup_30d"
            };

            foreach (SpeedUpCategory cat in Enum.GetValues(typeof(SpeedUpCategory)))
            {
                if (cat == SpeedUpCategory.Universal) continue;
                RegisterCategoryItems(cat, durations, rarities, icons);
            }

            RegisterCategoryItems(SpeedUpCategory.Universal, durations, rarities, icons);
        }

        private static void RegisterCategoryItems(SpeedUpCategory category, long[] durations, SpeedUpRarity[] rarities, string[] icons)
        {
            string catPrefix = category.ToString().ToLowerInvariant();
            string catNameKey = $"speedup.category.{catPrefix}";
            string catDescKey = $"speedup.category.{catPrefix}.desc";

            if (!byCategory.ContainsKey(category))
                byCategory[category] = new List<SpeedUpItem>();

            for (int i = 0; i < durations.Length; i++)
            {
                string id = $"{catPrefix}_{durations[i]}s";
                var item = new SpeedUpItem
                {
                    Id = id,
                    Category = category,
                    DurationSeconds = durations[i],
                    StackSize = 999,
                    Rarity = rarities[i],
                    Icon = icons[i],
                    LocalizedNameKey = $"speedup.{id}.name",
                    DescriptionKey = $"speedup.{id}.desc"
                };
                items[id] = item;
                byCategory[category].Add(item);
            }
        }

        public static SpeedUpItem Get(string id)
        {
            items.TryGetValue(id, out SpeedUpItem item);
            return item;
        }

        public static IReadOnlyList<SpeedUpItem> GetByCategory(SpeedUpCategory category)
        {
            if (byCategory.TryGetValue(category, out List<SpeedUpItem> list))
                return list;
            return Array.Empty<SpeedUpItem>();
        }

        public static IReadOnlyList<SpeedUpItem> GetAll()
        {
            return new List<SpeedUpItem>(items.Values);
        }
    }
}