using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    [Serializable]
    public sealed class LocalPreviewHiveProgress
    {
        public int version = LocalPreviewHiveProgressCodec.CurrentVersion;
        public string profileId = string.Empty;
        public int revision;
        public List<LocalPreviewBuildingProgress> buildings = new List<LocalPreviewBuildingProgress>();
        public int workers = LocalPreviewHiveProgressCodec.DefaultWorkers;
        public int soldiers = LocalPreviewHiveProgressCodec.DefaultSoldiers;
        public int guardians = LocalPreviewHiveProgressCodec.DefaultGuardians;
        public int scouts = LocalPreviewHiveProgressCodec.DefaultScouts;
        public int wingrunners;
        public int darters;
        public List<LocalPreviewChampionBeeProgress> championBees = new List<LocalPreviewChampionBeeProgress>();
        public List<string> assignedChampionBeeIds = new List<string>();
        public List<LocalPreviewTroopTierProgress> troopTiers = new List<LocalPreviewTroopTierProgress>();
    }

    [Serializable]
    public sealed class LocalPreviewBuildingProgress
    {
        public string hotspotId = string.Empty;
        public int level;
    }

    [Serializable]
    public sealed class LocalPreviewChampionBeeProgress
    {
        public string beeId = string.Empty;
        public int level;
    }

    [Serializable]
    public sealed class LocalPreviewTroopTierProgress
    {
        public string populationId = string.Empty;
        public int tier;
    }

    public enum LocalPreviewHiveProgressReadStatus
    {
        Empty,
        Restored,
        Sanitized,
        Corrupt,
        UnsupportedVersion,
        ProfileMismatch
    }

    public sealed class LocalPreviewHiveProgressReadResult
    {
        public LocalPreviewHiveProgressReadResult(LocalPreviewHiveProgress progress, LocalPreviewHiveProgressReadStatus status)
        {
            Progress = progress;
            Status = status;
        }

        public LocalPreviewHiveProgress Progress { get; }
        public LocalPreviewHiveProgressReadStatus Status { get; }
    }

    public interface ILocalPreviewHiveProgressStore
    {
        string Read();
        void Write(string json);
        void Delete();
    }

    public sealed class PlayerPrefsLocalPreviewHiveProgressStore : ILocalPreviewHiveProgressStore
    {
        private const string Key = "BeeKingdom_LivingHive_LocalPreviewHiveProgress_v1";

        public string Read() => PlayerPrefs.GetString(Key, string.Empty);

        public void Write(string json)
        {
            PlayerPrefs.SetString(Key, json ?? string.Empty);
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }

    public static class LocalPreviewHiveProgressCodec
    {
        public const int CurrentVersion = 4;
        public const int DefaultWorkers = 30;
        public const int DefaultSoldiers = 18;
        public const int DefaultGuardians = 8;
        public const int DefaultScouts = 5;
        public const int MaxBuildingEntries = 32;
        public const int MaxBuildingLevel = 9999;
        public const int MaxPopulationCount = 1000000;
        public const int MaxChampionBeeEntries = 32;
        public const int MaxChampionBeeLevel = 10;
        public const int MaxAssignedChampionBees = 5;
        public const int MaxTroopTierEntries = 8;
        public const int MaxTroopTier = 3;

        public static LocalPreviewHiveProgress CreateDefault(string profileId)
        {
            return new LocalPreviewHiveProgress
            {
                profileId = profileId ?? string.Empty
            };
        }

        public static LocalPreviewHiveProgressReadResult Read(ILocalPreviewHiveProgressStore store, string expectedProfileId)
        {
            string expected = expectedProfileId ?? string.Empty;
            if (store == null) return Result(expected, LocalPreviewHiveProgressReadStatus.Empty);

            string json = store.Read();
            if (string.IsNullOrWhiteSpace(json)) return Result(expected, LocalPreviewHiveProgressReadStatus.Empty);

            LocalPreviewHiveProgress progress;
            try
            {
                progress = JsonUtility.FromJson<LocalPreviewHiveProgress>(json);
            }
            catch
            {
                return Result(expected, LocalPreviewHiveProgressReadStatus.Corrupt);
            }

            if (progress == null) return Result(expected, LocalPreviewHiveProgressReadStatus.Corrupt);
            if (progress.version < 1 || progress.version > CurrentVersion) return Result(expected, LocalPreviewHiveProgressReadStatus.UnsupportedVersion);
            if (!string.Equals(progress.profileId ?? string.Empty, expected, StringComparison.Ordinal))
                return Result(expected, LocalPreviewHiveProgressReadStatus.ProfileMismatch);

            bool sanitized = Normalize(progress, expected);
            if (sanitized) Write(store, progress);
            return new LocalPreviewHiveProgressReadResult(
                progress,
                sanitized ? LocalPreviewHiveProgressReadStatus.Sanitized : LocalPreviewHiveProgressReadStatus.Restored);
        }

        public static void Write(ILocalPreviewHiveProgressStore store, LocalPreviewHiveProgress progress)
        {
            if (store == null || progress == null) return;
            Normalize(progress, progress.profileId ?? string.Empty);
            store.Write(JsonUtility.ToJson(progress));
        }

        public static bool TryGetBuildingLevel(LocalPreviewHiveProgress progress, string hotspotId, out int level)
        {
            level = 0;
            if (progress?.buildings == null || string.IsNullOrWhiteSpace(hotspotId)) return false;
            for (int i = 0; i < progress.buildings.Count; i++)
            {
                LocalPreviewBuildingProgress building = progress.buildings[i];
                if (building != null && string.Equals(building.hotspotId, hotspotId, StringComparison.Ordinal))
                {
                    level = building.level;
                    return true;
                }
            }

            return false;
        }

        public static bool MergeBuildingLevel(LocalPreviewHiveProgress progress, string hotspotId, int level)
        {
            if (progress == null || string.IsNullOrWhiteSpace(hotspotId)) return false;
            progress.buildings ??= new List<LocalPreviewBuildingProgress>();
            int safeLevel = Mathf.Clamp(level, 1, MaxBuildingLevel);
            for (int i = 0; i < progress.buildings.Count; i++)
            {
                LocalPreviewBuildingProgress building = progress.buildings[i];
                if (building == null || !string.Equals(building.hotspotId, hotspotId, StringComparison.Ordinal)) continue;
                if (safeLevel <= building.level) return false;
                building.level = safeLevel;
                progress.revision++;
                return true;
            }

            if (progress.buildings.Count >= MaxBuildingEntries) return false;
            progress.buildings.Add(new LocalPreviewBuildingProgress { hotspotId = hotspotId, level = safeLevel });
            progress.buildings.Sort((left, right) => string.CompareOrdinal(left.hotspotId, right.hotspotId));
            progress.revision++;
            return true;
        }

        public static bool MergePopulationCount(LocalPreviewHiveProgress progress, string populationId, int count)
        {
            if (progress == null || string.IsNullOrWhiteSpace(populationId)) return false;
            int safeCount = Mathf.Clamp(count, 0, MaxPopulationCount);
            switch (populationId)
            {
                case "workers": return MergeCount(progress, ref progress.workers, safeCount);
                case "soldiers": return MergeCount(progress, ref progress.soldiers, safeCount);
                case "guardians": return MergeCount(progress, ref progress.guardians, safeCount);
                case "scouts": return MergeCount(progress, ref progress.scouts, safeCount);
                case "wingrunners": return MergeCount(progress, ref progress.wingrunners, safeCount);
                case "darters": return MergeCount(progress, ref progress.darters, safeCount);
                default: return false;
            }
        }

        private static bool MergeCount(LocalPreviewHiveProgress progress, ref int current, int candidate)
        {
            if (candidate <= current) return false;
            current = candidate;
            progress.revision++;
            return true;
        }

        public static bool TryGetChampionBeeLevel(LocalPreviewHiveProgress progress, string beeId, out int level)
        {
            level = 0;
            if (progress?.championBees == null || string.IsNullOrWhiteSpace(beeId)) return false;
            for (int i = 0; i < progress.championBees.Count; i++)
            {
                LocalPreviewChampionBeeProgress bee = progress.championBees[i];
                if (bee != null && string.Equals(bee.beeId, beeId, StringComparison.Ordinal))
                {
                    level = bee.level;
                    return true;
                }
            }

            return false;
        }

        public static bool MergeChampionBeeLevel(LocalPreviewHiveProgress progress, string beeId, int level)
        {
            if (progress == null || string.IsNullOrWhiteSpace(beeId)) return false;
            progress.championBees ??= new List<LocalPreviewChampionBeeProgress>();
            int safeLevel = Mathf.Clamp(level, 1, MaxChampionBeeLevel);
            for (int i = 0; i < progress.championBees.Count; i++)
            {
                LocalPreviewChampionBeeProgress bee = progress.championBees[i];
                if (bee == null || !string.Equals(bee.beeId, beeId, StringComparison.Ordinal)) continue;
                if (safeLevel <= bee.level) return false;
                bee.level = safeLevel;
                progress.revision++;
                return true;
            }

            if (progress.championBees.Count >= MaxChampionBeeEntries) return false;
            progress.championBees.Add(new LocalPreviewChampionBeeProgress { beeId = beeId, level = safeLevel });
            progress.championBees.Sort((left, right) => string.CompareOrdinal(left.beeId, right.beeId));
            progress.revision++;
            return true;
        }

        public static bool SetAssignedChampionBees(LocalPreviewHiveProgress progress, IReadOnlyList<string> beeIds)
        {
            if (progress == null) return false;
            progress.championBees ??= new List<LocalPreviewChampionBeeProgress>();
            var ownedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < progress.championBees.Count; i++)
            {
                LocalPreviewChampionBeeProgress bee = progress.championBees[i];
                if (bee != null && !string.IsNullOrWhiteSpace(bee.beeId)) ownedIds.Add(bee.beeId);
            }

            var sanitized = new List<string>(MaxAssignedChampionBees);
            if (beeIds != null)
            {
                for (int i = 0; i < beeIds.Count && sanitized.Count < MaxAssignedChampionBees; i++)
                {
                    string beeId = beeIds[i];
                    if (string.IsNullOrWhiteSpace(beeId) || !ownedIds.Contains(beeId) || sanitized.Contains(beeId)) continue;
                    sanitized.Add(beeId);
                }
            }

            progress.assignedChampionBeeIds ??= new List<string>();
            if (sanitized.Count == progress.assignedChampionBeeIds.Count)
            {
                bool same = true;
                for (int i = 0; i < sanitized.Count; i++)
                {
                    if (!string.Equals(sanitized[i], progress.assignedChampionBeeIds[i], StringComparison.Ordinal)) { same = false; break; }
                }
                if (same) return false;
            }

            progress.assignedChampionBeeIds = sanitized;
            progress.revision++;
            return true;
        }

        public static bool TryGetTroopTier(LocalPreviewHiveProgress progress, string populationId, out int tier)
        {
            tier = 0;
            if (progress?.troopTiers == null || string.IsNullOrWhiteSpace(populationId)) return false;
            for (int i = 0; i < progress.troopTiers.Count; i++)
            {
                LocalPreviewTroopTierProgress entry = progress.troopTiers[i];
                if (entry != null && string.Equals(entry.populationId, populationId, StringComparison.Ordinal))
                {
                    tier = entry.tier;
                    return true;
                }
            }

            return false;
        }

        public static bool MergeTroopTier(LocalPreviewHiveProgress progress, string populationId, int tier)
        {
            if (progress == null || string.IsNullOrWhiteSpace(populationId)) return false;
            progress.troopTiers ??= new List<LocalPreviewTroopTierProgress>();
            int safeTier = Mathf.Clamp(tier, 1, MaxTroopTier);
            for (int i = 0; i < progress.troopTiers.Count; i++)
            {
                LocalPreviewTroopTierProgress entry = progress.troopTiers[i];
                if (entry == null || !string.Equals(entry.populationId, populationId, StringComparison.Ordinal)) continue;
                if (safeTier <= entry.tier) return false;
                entry.tier = safeTier;
                progress.revision++;
                return true;
            }

            if (progress.troopTiers.Count >= MaxTroopTierEntries) return false;
            progress.troopTiers.Add(new LocalPreviewTroopTierProgress { populationId = populationId, tier = safeTier });
            progress.troopTiers.Sort((left, right) => string.CompareOrdinal(left.populationId, right.populationId));
            progress.revision++;
            return true;
        }

        private static LocalPreviewHiveProgressReadResult Result(string profileId, LocalPreviewHiveProgressReadStatus status)
        {
            return new LocalPreviewHiveProgressReadResult(CreateDefault(profileId), status);
        }

        private static bool Normalize(LocalPreviewHiveProgress progress, string profileId)
        {
            bool changed = false;
            if (progress.version != CurrentVersion)
            {
                progress.version = CurrentVersion;
                changed = true;
            }
            if (!string.Equals(progress.profileId, profileId, StringComparison.Ordinal))
            {
                progress.profileId = profileId;
                changed = true;
            }
            if (progress.revision < 0)
            {
                progress.revision = 0;
                changed = true;
            }

            changed |= Clamp(ref progress.workers, 0, MaxPopulationCount);
            changed |= Clamp(ref progress.soldiers, 0, MaxPopulationCount);
            changed |= Clamp(ref progress.guardians, 0, MaxPopulationCount);
            changed |= Clamp(ref progress.scouts, 0, MaxPopulationCount);
            changed |= Clamp(ref progress.wingrunners, 0, MaxPopulationCount);
            changed |= Clamp(ref progress.darters, 0, MaxPopulationCount);

            var normalized = new List<LocalPreviewBuildingProgress>(MaxBuildingEntries);
            var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
            if (progress.buildings != null)
            {
                for (int i = 0; i < progress.buildings.Count; i++)
                {
                    LocalPreviewBuildingProgress source = progress.buildings[i];
                    if (source == null || string.IsNullOrWhiteSpace(source.hotspotId))
                    {
                        changed = true;
                        continue;
                    }

                    int safeLevel = Mathf.Clamp(source.level, 1, MaxBuildingLevel);
                    if (safeLevel != source.level) changed = true;
                    if (indexes.TryGetValue(source.hotspotId, out int existingIndex))
                    {
                        if (safeLevel > normalized[existingIndex].level) normalized[existingIndex].level = safeLevel;
                        changed = true;
                        continue;
                    }
                    if (normalized.Count >= MaxBuildingEntries)
                    {
                        changed = true;
                        continue;
                    }

                    indexes[source.hotspotId] = normalized.Count;
                    normalized.Add(new LocalPreviewBuildingProgress { hotspotId = source.hotspotId, level = safeLevel });
                }
            }
            else changed = true;

            normalized.Sort((left, right) => string.CompareOrdinal(left.hotspotId, right.hotspotId));
            if (!changed && !SameBuildings(progress.buildings, normalized)) changed = true;
            progress.buildings = normalized;

            var normalizedBees = new List<LocalPreviewChampionBeeProgress>(MaxChampionBeeEntries);
            var beeIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            if (progress.championBees != null)
            {
                for (int i = 0; i < progress.championBees.Count; i++)
                {
                    LocalPreviewChampionBeeProgress source = progress.championBees[i];
                    if (source == null || string.IsNullOrWhiteSpace(source.beeId))
                    {
                        changed = true;
                        continue;
                    }

                    int safeLevel = Mathf.Clamp(source.level, 1, MaxChampionBeeLevel);
                    if (safeLevel != source.level) changed = true;
                    if (beeIndexes.TryGetValue(source.beeId, out int existingBeeIndex))
                    {
                        if (safeLevel > normalizedBees[existingBeeIndex].level) normalizedBees[existingBeeIndex].level = safeLevel;
                        changed = true;
                        continue;
                    }
                    if (normalizedBees.Count >= MaxChampionBeeEntries)
                    {
                        changed = true;
                        continue;
                    }

                    beeIndexes[source.beeId] = normalizedBees.Count;
                    normalizedBees.Add(new LocalPreviewChampionBeeProgress { beeId = source.beeId, level = safeLevel });
                }
            }
            else changed = true;

            normalizedBees.Sort((left, right) => string.CompareOrdinal(left.beeId, right.beeId));
            progress.championBees = normalizedBees;

            var normalizedAssigned = new List<string>(MaxAssignedChampionBees);
            if (progress.assignedChampionBeeIds != null)
            {
                for (int i = 0; i < progress.assignedChampionBeeIds.Count; i++)
                {
                    string beeId = progress.assignedChampionBeeIds[i];
                    if (string.IsNullOrWhiteSpace(beeId) || !beeIndexes.ContainsKey(beeId) || normalizedAssigned.Contains(beeId) || normalizedAssigned.Count >= MaxAssignedChampionBees)
                    {
                        changed = true;
                        continue;
                    }
                    normalizedAssigned.Add(beeId);
                }
            }
            else changed = true;

            progress.assignedChampionBeeIds = normalizedAssigned;

            var normalizedTiers = new List<LocalPreviewTroopTierProgress>(MaxTroopTierEntries);
            var tierIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            if (progress.troopTiers != null)
            {
                for (int i = 0; i < progress.troopTiers.Count; i++)
                {
                    LocalPreviewTroopTierProgress source = progress.troopTiers[i];
                    if (source == null || string.IsNullOrWhiteSpace(source.populationId))
                    {
                        changed = true;
                        continue;
                    }

                    int safeTier = Mathf.Clamp(source.tier, 1, MaxTroopTier);
                    if (safeTier != source.tier) changed = true;
                    if (tierIndexes.TryGetValue(source.populationId, out int existingTierIndex))
                    {
                        if (safeTier > normalizedTiers[existingTierIndex].tier) normalizedTiers[existingTierIndex].tier = safeTier;
                        changed = true;
                        continue;
                    }
                    if (normalizedTiers.Count >= MaxTroopTierEntries)
                    {
                        changed = true;
                        continue;
                    }

                    tierIndexes[source.populationId] = normalizedTiers.Count;
                    normalizedTiers.Add(new LocalPreviewTroopTierProgress { populationId = source.populationId, tier = safeTier });
                }
            }
            else changed = true;

            normalizedTiers.Sort((left, right) => string.CompareOrdinal(left.populationId, right.populationId));
            progress.troopTiers = normalizedTiers;
            return changed;
        }

        private static bool SameBuildings(IReadOnlyList<LocalPreviewBuildingProgress> left, IReadOnlyList<LocalPreviewBuildingProgress> right)
        {
            if (left == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] == null
                    || !string.Equals(left[i].hotspotId, right[i].hotspotId, StringComparison.Ordinal)
                    || left[i].level != right[i].level) return false;
            }
            return true;
        }

        private static bool Clamp(ref int value, int minimum, int maximum)
        {
            int safe = Mathf.Clamp(value, minimum, maximum);
            if (safe == value) return false;
            value = safe;
            return true;
        }
    }
}
