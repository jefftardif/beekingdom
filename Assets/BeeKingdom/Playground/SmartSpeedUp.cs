using System;
using System.Collections.Generic;

namespace BeeKingdom.Gameplay.Progression
{
    public readonly struct SpeedUpDialogContext
    {
        public SpeedUpCategory Category { get; }
        public string TargetId { get; }
        public long RemainingSeconds { get; }

        public SpeedUpDialogContext(SpeedUpCategory category, string targetId, long remainingSeconds)
        {
            Category = category;
            TargetId = targetId ?? string.Empty;
            RemainingSeconds = Math.Max(0L, remainingSeconds);
        }
    }

    public sealed class SmartSpeedUpPlan
    {
        public sealed class Entry
        {
            public SpeedUpItem Item { get; }
            public int Count { get; }
            public long TotalSeconds => Item.DurationSeconds * Count;

            public Entry(SpeedUpItem item, int count)
            {
                Item = item;
                Count = count;
            }
        }

        private readonly List<Entry> entries;
        public IReadOnlyList<Entry> Entries => entries;
        public long RequestedSeconds { get; }
        public long AppliedSeconds { get; }
        public long WasteSeconds => Math.Max(0L, AppliedSeconds - RequestedSeconds);
        public long RemainingAfterSeconds => Math.Max(0L, RequestedSeconds - AppliedSeconds);
        public bool CompletesTarget => RequestedSeconds <= 0L || AppliedSeconds >= RequestedSeconds;

        internal SmartSpeedUpPlan(List<Entry> entries, long requestedSeconds, long appliedSeconds)
        {
            this.entries = entries;
            RequestedSeconds = Math.Max(0L, requestedSeconds);
            AppliedSeconds = Math.Max(0L, appliedSeconds);
        }
    }

    public static class SmartSpeedUpCalculator
    {
        public static SmartSpeedUpPlan ComputePlan(SpeedUpDialogContext context)
        {
            long requestedSeconds = context.RemainingSeconds;
            if (requestedSeconds <= 0L)
                return new SmartSpeedUpPlan(new List<SmartSpeedUpPlan.Entry>(), 0L, 0L);

            var available = new List<SpeedUpStack>(SpeedUpInventory.GetStacks(context.Category));
            if (context.Category != SpeedUpCategory.Universal)
                available.AddRange(SpeedUpInventory.GetStacks(SpeedUpCategory.Universal));
            if (available.Count == 0) return null;

            available.Sort((left, right) => right.Item.DurationSeconds.CompareTo(left.Item.DurationSeconds));
            int targetMinutes = (int)Math.Min(int.MaxValue, (requestedSeconds + 59L) / 60L);
            int maxItemMinutes = Math.Max(1, (int)Math.Min(int.MaxValue, (available[0].Item.DurationSeconds + 59L) / 60L));
            int maxMinutes = targetMinutes + maxItemMinutes;
            var reachable = new bool[maxMinutes + 1];
            var previousAmount = new int[maxMinutes + 1];
            var previousGroup = new int[maxMinutes + 1];
            for (int i = 0; i < previousAmount.Length; i++)
            {
                previousAmount[i] = -1;
                previousGroup[i] = -1;
            }
            reachable[0] = true;

            var groups = new List<Group>(available.Count * 2);
            for (int itemIndex = 0; itemIndex < available.Count; itemIndex++)
            {
                SpeedUpStack stack = available[itemIndex];
                int remaining = stack.Count;
                int chunk = 1;
                while (remaining > 0)
                {
                    int count = Math.Min(chunk, remaining);
                    int minutes = (int)Math.Max(1L, (stack.Item.DurationSeconds * count + 59L) / 60L);
                    groups.Add(new Group(stack.Item, count, minutes));
                    remaining -= count;
                    chunk <<= 1;
                }
            }

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                Group group = groups[groupIndex];
                for (int amount = maxMinutes; amount >= group.Minutes; amount--)
                {
                    int previous = amount - group.Minutes;
                    if (!reachable[previous] || reachable[amount]) continue;
                    reachable[amount] = true;
                    previousAmount[amount] = previous;
                    previousGroup[amount] = groupIndex;
                }
            }

            int selectedAmount = -1;
            for (int amount = targetMinutes; amount <= maxMinutes; amount++)
            {
                if (reachable[amount])
                {
                    selectedAmount = amount;
                    break;
                }
            }
            if (selectedAmount < 0) return null;

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var items = new Dictionary<string, SpeedUpItem>(StringComparer.Ordinal);
            int cursor = selectedAmount;
            while (cursor > 0 && previousGroup[cursor] >= 0)
            {
                Group group = groups[previousGroup[cursor]];
                int count = counts.TryGetValue(group.Item.Id, out int current) ? current : 0;
                counts[group.Item.Id] = count + group.Count;
                items[group.Item.Id] = group.Item;
                cursor = previousAmount[cursor];
            }

            var entries = new List<SmartSpeedUpPlan.Entry>(counts.Count);
            long appliedSeconds = 0L;
            foreach (KeyValuePair<string, int> pair in counts)
            {
                SpeedUpItem item = items[pair.Key];
                entries.Add(new SmartSpeedUpPlan.Entry(item, pair.Value));
                appliedSeconds += item.DurationSeconds * pair.Value;
            }
            entries.Sort((left, right) => right.Item.DurationSeconds.CompareTo(left.Item.DurationSeconds));
            return new SmartSpeedUpPlan(entries, requestedSeconds, appliedSeconds);
        }

        public static bool ApplyPlan(SmartSpeedUpPlan plan, out long remainingAfterSeconds)
        {
            remainingAfterSeconds = plan == null ? 0L : plan.RemainingAfterSeconds;
            if (plan == null || !plan.CompletesTarget) return false;

            for (int i = 0; i < plan.Entries.Count; i++)
            {
                SmartSpeedUpPlan.Entry entry = plan.Entries[i];
                if (SpeedUpInventory.GetCount(entry.Item.Id) < entry.Count) return false;
            }
            for (int i = 0; i < plan.Entries.Count; i++)
            {
                SmartSpeedUpPlan.Entry entry = plan.Entries[i];
                if (!SpeedUpInventory.Remove(entry.Item.Id, entry.Count)) return false;
            }
            remainingAfterSeconds = 0L;
            return true;
        }

        private readonly struct Group
        {
            public readonly SpeedUpItem Item;
            public readonly int Count;
            public readonly int Minutes;

            public Group(SpeedUpItem item, int count, int minutes)
            {
                Item = item;
                Count = count;
                Minutes = minutes;
            }
        }
    }

    public static class SpeedUpDialog
    {
        private static SpeedUpDialogContext context;
        private static bool open;

        public static bool IsOpen => open;
        public static SpeedUpDialogContext Context => context;

        public static void Open(SpeedUpDialogContext value)
        {
            context = value;
            open = true;
        }

        public static void Close()
        {
            open = false;
        }
    }
}
