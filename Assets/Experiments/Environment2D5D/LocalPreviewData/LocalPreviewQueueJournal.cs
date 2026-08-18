using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    [Serializable]
    public sealed class LocalPreviewQueueJournal
    {
        public int version = LocalPreviewQueueJournalCodec.CurrentVersion;
        public LocalPreviewQueueOperation upgrade = new LocalPreviewQueueOperation();
        public LocalPreviewQueueOperation training = new LocalPreviewQueueOperation();
        public LocalPreviewQueueOperation research = new LocalPreviewQueueOperation();
        public List<string> completedResearchIds = new List<string>();
    }

    [Serializable]
    public sealed class LocalPreviewQueueOperation
    {
        public string operationId = string.Empty;
        public string targetId = string.Empty;
        public long startedUtcTicks;
        public long endsUtcTicks;
        public float honeyCost;
        public float waxCost;
        public float pollenCost;
        public bool completionClaimed;
        public int resultValue;

        public bool Exists => !string.IsNullOrWhiteSpace(operationId) && !string.IsNullOrWhiteSpace(targetId);
    }

    public enum LocalPreviewQueueReturnStatus
    {
        Active,
        CompletedWhileAway
    }

    public sealed class LocalPreviewQueueReturnItem
    {
        public LocalPreviewQueueReturnItem(string kind, string targetId, long endsUtcTicks, LocalPreviewQueueReturnStatus status)
        {
            Kind = kind ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            EndsUtcTicks = endsUtcTicks;
            Status = status;
        }

        public string Kind { get; }
        public string TargetId { get; }
        public long EndsUtcTicks { get; }
        public LocalPreviewQueueReturnStatus Status { get; }
    }

    public static class LocalPreviewQueueReturnSummary
    {
        public static IReadOnlyList<LocalPreviewQueueReturnItem> Build(LocalPreviewQueueJournal journal, long utcNowTicks)
        {
            var items = new List<LocalPreviewQueueReturnItem>(3);
            if (journal == null) return items;

            Add(items, "upgrade", journal.upgrade, utcNowTicks);
            Add(items, "training", journal.training, utcNowTicks);
            Add(items, "research", journal.research, utcNowTicks);
            return items;
        }

        private static void Add(List<LocalPreviewQueueReturnItem> items, string kind, LocalPreviewQueueOperation operation, long utcNowTicks)
        {
            if (operation == null || !operation.Exists || operation.completionClaimed) return;
            LocalPreviewQueueReturnStatus status = operation.endsUtcTicks <= utcNowTicks
                ? LocalPreviewQueueReturnStatus.CompletedWhileAway
                : LocalPreviewQueueReturnStatus.Active;
            items.Add(new LocalPreviewQueueReturnItem(kind, operation.targetId, operation.endsUtcTicks, status));
        }
    }

    public interface ILocalPreviewQueueJournalStore
    {
        string Read();
        void Write(string json);
        void Delete();
    }

    public sealed class PlayerPrefsLocalPreviewQueueJournalStore : ILocalPreviewQueueJournalStore
    {
        private const string Key = "BeeKingdom_LivingHive_LocalPreviewQueues_v1";

        public string Read()
        {
            return PlayerPrefs.GetString(Key, string.Empty);
        }

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

    public static class LocalPreviewQueueJournalCodec
    {
        public const int CurrentVersion = 2;

        public static LocalPreviewQueueJournal Read(ILocalPreviewQueueJournalStore store)
        {
            if (store == null) return new LocalPreviewQueueJournal();
            string json = store.Read();
            if (string.IsNullOrWhiteSpace(json)) return new LocalPreviewQueueJournal();

            try
            {
                LocalPreviewQueueJournal journal = JsonUtility.FromJson<LocalPreviewQueueJournal>(json);
                if (journal == null || (journal.version != 1 && journal.version != CurrentVersion)) return new LocalPreviewQueueJournal();
                bool migrated = journal.version != CurrentVersion;
                journal.upgrade ??= new LocalPreviewQueueOperation();
                journal.training ??= new LocalPreviewQueueOperation();
                journal.research ??= new LocalPreviewQueueOperation();
                journal.completedResearchIds ??= new List<string>();
                journal.version = CurrentVersion;
                if (migrated) Write(store, journal);
                return journal;
            }
            catch
            {
                return new LocalPreviewQueueJournal();
            }
        }

        public static void Write(ILocalPreviewQueueJournalStore store, LocalPreviewQueueJournal journal)
        {
            if (store == null || journal == null) return;
            store.Write(JsonUtility.ToJson(journal));
        }
    }
}
