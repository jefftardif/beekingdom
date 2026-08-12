using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    [Serializable]
    public sealed class LocalPreviewManualProductionJournal
    {
        public int version = LocalPreviewManualProductionJournalCodec.CurrentVersion;
        public string profileId = string.Empty;
        public int revision;
        public long lastAccrualUtcTicks;
        public List<LocalPreviewManualProductionEntry> entries = new List<LocalPreviewManualProductionEntry>();
    }

    [Serializable]
    public sealed class LocalPreviewManualProductionEntry
    {
        public string hotspotId = string.Empty;
        public float pending;
    }

    public sealed class LocalPreviewManualProductionRule
    {
        public LocalPreviewManualProductionRule(string hotspotId, float perHour, float capacity)
        {
            HotspotId = hotspotId ?? string.Empty;
            PerHour = SafeNonNegative(perHour);
            Capacity = SafeNonNegative(capacity);
        }

        public string HotspotId { get; }
        public float PerHour { get; }
        public float Capacity { get; }

        private static float SafeNonNegative(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        }
    }

    public enum LocalPreviewManualProductionReadStatus
    {
        Empty,
        Restored,
        Sanitized,
        Corrupt,
        UnsupportedVersion,
        ProfileMismatch
    }

    public enum LocalPreviewManualProductionAccrualStatus
    {
        Initialized,
        NoChange,
        Accrued,
        FutureLeapCapped,
        ClockRollback
    }

    public sealed class LocalPreviewManualProductionReadResult
    {
        public LocalPreviewManualProductionReadResult(
            LocalPreviewManualProductionJournal journal,
            LocalPreviewManualProductionReadStatus status)
        {
            Journal = journal;
            Status = status;
        }

        public LocalPreviewManualProductionJournal Journal { get; }
        public LocalPreviewManualProductionReadStatus Status { get; }
    }

    public sealed class LocalPreviewManualProductionAccrualResult
    {
        private readonly Dictionary<string, float> accruedByHotspot;

        public LocalPreviewManualProductionAccrualResult(
            LocalPreviewManualProductionAccrualStatus status,
            double observedSeconds,
            double recognizedSeconds,
            Dictionary<string, float> accruedByHotspot)
        {
            Status = status;
            ObservedSeconds = observedSeconds;
            RecognizedSeconds = recognizedSeconds;
            this.accruedByHotspot = accruedByHotspot ?? new Dictionary<string, float>(StringComparer.Ordinal);
        }

        public LocalPreviewManualProductionAccrualStatus Status { get; }
        public double ObservedSeconds { get; }
        public double RecognizedSeconds { get; }
        public IReadOnlyDictionary<string, float> AccruedByHotspot => accruedByHotspot;

        public float AccruedFor(string hotspotId)
        {
            return accruedByHotspot.TryGetValue(hotspotId ?? string.Empty, out float value) ? value : 0f;
        }

        public float TotalAccrued
        {
            get
            {
                float total = 0f;
                foreach (float value in accruedByHotspot.Values) total += value;
                return total;
            }
        }
    }

    public interface ILocalPreviewManualProductionJournalStore
    {
        string Read();
        void Write(string json);
        void Delete();
    }

    public sealed class PlayerPrefsLocalPreviewManualProductionJournalStore : ILocalPreviewManualProductionJournalStore
    {
        private const string Key = "BeeKingdom_LivingHive_LocalPreviewManualProduction_v1";

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

    public static class LocalPreviewManualProductionJournalCodec
    {
        public const int CurrentVersion = 1;
        public const int MaxEntries = 16;
        public const double DefaultMaxOfflineSeconds = 12d * 60d * 60d;

        public static LocalPreviewManualProductionJournal CreateDefault(
            string profileId,
            long utcNowTicks,
            IReadOnlyList<LocalPreviewManualProductionRule> rules)
        {
            var journal = new LocalPreviewManualProductionJournal
            {
                profileId = profileId ?? string.Empty,
                lastAccrualUtcTicks = Math.Max(0L, utcNowTicks)
            };
            AddMissingEntries(journal, rules);
            return journal;
        }

        public static LocalPreviewManualProductionReadResult Read(
            ILocalPreviewManualProductionJournalStore store,
            string expectedProfileId,
            long utcNowTicks,
            IReadOnlyList<LocalPreviewManualProductionRule> rules)
        {
            string expected = expectedProfileId ?? string.Empty;
            if (store == null)
                return Result(expected, utcNowTicks, rules, LocalPreviewManualProductionReadStatus.Empty);

            string json = store.Read();
            if (string.IsNullOrWhiteSpace(json))
                return Result(expected, utcNowTicks, rules, LocalPreviewManualProductionReadStatus.Empty);

            LocalPreviewManualProductionJournal journal;
            try
            {
                journal = JsonUtility.FromJson<LocalPreviewManualProductionJournal>(json);
            }
            catch
            {
                return Result(expected, utcNowTicks, rules, LocalPreviewManualProductionReadStatus.Corrupt);
            }

            if (journal == null)
                return Result(expected, utcNowTicks, rules, LocalPreviewManualProductionReadStatus.Corrupt);
            if (journal.version != CurrentVersion)
                return Result(expected, utcNowTicks, rules, LocalPreviewManualProductionReadStatus.UnsupportedVersion);
            if (!string.Equals(journal.profileId ?? string.Empty, expected, StringComparison.Ordinal))
                return Result(expected, utcNowTicks, rules, LocalPreviewManualProductionReadStatus.ProfileMismatch);

            bool sanitized = Normalize(journal, expected, rules);
            if (sanitized) Write(store, journal, rules);
            return new LocalPreviewManualProductionReadResult(
                journal,
                sanitized ? LocalPreviewManualProductionReadStatus.Sanitized : LocalPreviewManualProductionReadStatus.Restored);
        }

        public static LocalPreviewManualProductionAccrualResult Accrue(
            LocalPreviewManualProductionJournal journal,
            long utcNowTicks,
            IReadOnlyList<LocalPreviewManualProductionRule> rules,
            double maxOfflineSeconds = DefaultMaxOfflineSeconds)
        {
            var accrued = new Dictionary<string, float>(StringComparer.Ordinal);
            if (journal == null)
                return new LocalPreviewManualProductionAccrualResult(
                    LocalPreviewManualProductionAccrualStatus.NoChange, 0d, 0d, accrued);

            long now = Math.Max(0L, utcNowTicks);
            if (journal.lastAccrualUtcTicks <= 0L)
            {
                journal.lastAccrualUtcTicks = now;
                journal.revision++;
                return new LocalPreviewManualProductionAccrualResult(
                    LocalPreviewManualProductionAccrualStatus.Initialized, 0d, 0d, accrued);
            }

            if (now < journal.lastAccrualUtcTicks)
            {
                double rollback = new TimeSpan(journal.lastAccrualUtcTicks - now).TotalSeconds;
                return new LocalPreviewManualProductionAccrualResult(
                    LocalPreviewManualProductionAccrualStatus.ClockRollback, -rollback, 0d, accrued);
            }

            double observed = new TimeSpan(now - journal.lastAccrualUtcTicks).TotalSeconds;
            double safeMaximum = Math.Max(0d, maxOfflineSeconds);
            double recognized = Math.Min(observed, safeMaximum);
            LocalPreviewManualProductionAccrualStatus status = observed > safeMaximum
                ? LocalPreviewManualProductionAccrualStatus.FutureLeapCapped
                : LocalPreviewManualProductionAccrualStatus.NoChange;

            if (recognized > 0d && rules != null)
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    LocalPreviewManualProductionRule rule = rules[i];
                    if (rule == null || string.IsNullOrWhiteSpace(rule.HotspotId)) continue;
                    LocalPreviewManualProductionEntry entry = FindOrAddEntry(journal, rule.HotspotId);
                    if (entry == null) continue;
                    float before = Mathf.Clamp(Safe(entry.pending), 0f, rule.Capacity);
                    float produced = (float)(rule.PerHour / 3600d * recognized);
                    entry.pending = Mathf.Min(rule.Capacity, before + produced);
                    float delta = Mathf.Max(0f, entry.pending - before);
                    if (delta > 0f) accrued[rule.HotspotId] = delta;
                }
            }

            journal.lastAccrualUtcTicks = now;
            journal.revision++;
            if (status != LocalPreviewManualProductionAccrualStatus.FutureLeapCapped && accrued.Count > 0)
                status = LocalPreviewManualProductionAccrualStatus.Accrued;
            return new LocalPreviewManualProductionAccrualResult(status, observed, recognized, accrued);
        }

        public static void SetPending(
            LocalPreviewManualProductionJournal journal,
            string hotspotId,
            float pending,
            IReadOnlyList<LocalPreviewManualProductionRule> rules)
        {
            if (journal == null || string.IsNullOrWhiteSpace(hotspotId)) return;
            LocalPreviewManualProductionRule rule = FindRule(rules, hotspotId);
            if (rule == null) return;
            LocalPreviewManualProductionEntry entry = FindOrAddEntry(journal, hotspotId);
            if (entry == null) return;
            entry.pending = Mathf.Clamp(Safe(pending), 0f, rule.Capacity);
        }

        public static float PendingFor(LocalPreviewManualProductionJournal journal, string hotspotId)
        {
            if (journal?.entries == null || string.IsNullOrWhiteSpace(hotspotId)) return 0f;
            for (int i = 0; i < journal.entries.Count; i++)
            {
                LocalPreviewManualProductionEntry entry = journal.entries[i];
                if (entry != null && string.Equals(entry.hotspotId, hotspotId, StringComparison.Ordinal))
                    return Safe(entry.pending);
            }
            return 0f;
        }

        public static void Write(
            ILocalPreviewManualProductionJournalStore store,
            LocalPreviewManualProductionJournal journal,
            IReadOnlyList<LocalPreviewManualProductionRule> rules)
        {
            if (store == null || journal == null) return;
            Normalize(journal, journal.profileId ?? string.Empty, rules);
            store.Write(JsonUtility.ToJson(journal));
        }

        private static LocalPreviewManualProductionReadResult Result(
            string profileId,
            long utcNowTicks,
            IReadOnlyList<LocalPreviewManualProductionRule> rules,
            LocalPreviewManualProductionReadStatus status)
        {
            return new LocalPreviewManualProductionReadResult(CreateDefault(profileId, utcNowTicks, rules), status);
        }

        private static bool Normalize(
            LocalPreviewManualProductionJournal journal,
            string profileId,
            IReadOnlyList<LocalPreviewManualProductionRule> rules)
        {
            bool changed = false;
            if (journal.version != CurrentVersion)
            {
                journal.version = CurrentVersion;
                changed = true;
            }
            if (!string.Equals(journal.profileId, profileId, StringComparison.Ordinal))
            {
                journal.profileId = profileId;
                changed = true;
            }
            if (journal.revision < 0)
            {
                journal.revision = 0;
                changed = true;
            }
            if (journal.lastAccrualUtcTicks < 0L)
            {
                journal.lastAccrualUtcTicks = 0L;
                changed = true;
            }

            var normalized = new List<LocalPreviewManualProductionEntry>(Math.Min(MaxEntries, rules?.Count ?? 0));
            var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
            if (journal.entries != null)
            {
                for (int i = 0; i < journal.entries.Count; i++)
                {
                    LocalPreviewManualProductionEntry source = journal.entries[i];
                    LocalPreviewManualProductionRule rule = FindRule(rules, source?.hotspotId);
                    if (source == null || rule == null || string.IsNullOrWhiteSpace(source.hotspotId))
                    {
                        changed = true;
                        continue;
                    }
                    float pending = Mathf.Clamp(Safe(source.pending), 0f, rule.Capacity);
                    if (!Mathf.Approximately(pending, source.pending)) changed = true;
                    if (indexes.TryGetValue(source.hotspotId, out int existing))
                    {
                        normalized[existing].pending = Mathf.Max(normalized[existing].pending, pending);
                        changed = true;
                        continue;
                    }
                    if (normalized.Count >= MaxEntries)
                    {
                        changed = true;
                        continue;
                    }
                    indexes[source.hotspotId] = normalized.Count;
                    normalized.Add(new LocalPreviewManualProductionEntry { hotspotId = source.hotspotId, pending = pending });
                }
            }
            else changed = true;

            if (rules != null)
            {
                for (int i = 0; i < rules.Count && normalized.Count < MaxEntries; i++)
                {
                    LocalPreviewManualProductionRule rule = rules[i];
                    if (rule == null || string.IsNullOrWhiteSpace(rule.HotspotId) || indexes.ContainsKey(rule.HotspotId)) continue;
                    indexes[rule.HotspotId] = normalized.Count;
                    normalized.Add(new LocalPreviewManualProductionEntry { hotspotId = rule.HotspotId, pending = 0f });
                    changed = true;
                }
            }

            normalized.Sort((left, right) => string.CompareOrdinal(left.hotspotId, right.hotspotId));
            if (!changed && !SameEntries(journal.entries, normalized)) changed = true;
            journal.entries = normalized;
            return changed;
        }

        private static void AddMissingEntries(
            LocalPreviewManualProductionJournal journal,
            IReadOnlyList<LocalPreviewManualProductionRule> rules)
        {
            if (journal == null || rules == null) return;
            for (int i = 0; i < rules.Count && journal.entries.Count < MaxEntries; i++)
            {
                LocalPreviewManualProductionRule rule = rules[i];
                if (rule == null || string.IsNullOrWhiteSpace(rule.HotspotId)) continue;
                if (FindEntry(journal, rule.HotspotId) == null)
                    journal.entries.Add(new LocalPreviewManualProductionEntry { hotspotId = rule.HotspotId });
            }
            journal.entries.Sort((left, right) => string.CompareOrdinal(left.hotspotId, right.hotspotId));
        }

        private static LocalPreviewManualProductionEntry FindOrAddEntry(
            LocalPreviewManualProductionJournal journal,
            string hotspotId)
        {
            LocalPreviewManualProductionEntry entry = FindEntry(journal, hotspotId);
            if (entry != null) return entry;
            journal.entries ??= new List<LocalPreviewManualProductionEntry>();
            if (journal.entries.Count >= MaxEntries) return null;
            entry = new LocalPreviewManualProductionEntry { hotspotId = hotspotId };
            journal.entries.Add(entry);
            journal.entries.Sort((left, right) => string.CompareOrdinal(left.hotspotId, right.hotspotId));
            return entry;
        }

        private static LocalPreviewManualProductionEntry FindEntry(
            LocalPreviewManualProductionJournal journal,
            string hotspotId)
        {
            if (journal?.entries == null || string.IsNullOrWhiteSpace(hotspotId)) return null;
            for (int i = 0; i < journal.entries.Count; i++)
            {
                LocalPreviewManualProductionEntry entry = journal.entries[i];
                if (entry != null && string.Equals(entry.hotspotId, hotspotId, StringComparison.Ordinal)) return entry;
            }
            return null;
        }

        private static LocalPreviewManualProductionRule FindRule(
            IReadOnlyList<LocalPreviewManualProductionRule> rules,
            string hotspotId)
        {
            if (rules == null || string.IsNullOrWhiteSpace(hotspotId)) return null;
            for (int i = 0; i < rules.Count; i++)
            {
                LocalPreviewManualProductionRule rule = rules[i];
                if (rule != null && string.Equals(rule.HotspotId, hotspotId, StringComparison.Ordinal)) return rule;
            }
            return null;
        }

        private static bool SameEntries(
            IReadOnlyList<LocalPreviewManualProductionEntry> left,
            IReadOnlyList<LocalPreviewManualProductionEntry> right)
        {
            if (left == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] == null
                    || !string.Equals(left[i].hotspotId, right[i].hotspotId, StringComparison.Ordinal)
                    || !Mathf.Approximately(left[i].pending, right[i].pending)) return false;
            }
            return true;
        }

        private static float Safe(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }
    }
}
