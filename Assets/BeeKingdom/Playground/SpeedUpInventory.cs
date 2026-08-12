using System;
using System.Collections.Generic;
using BeeKingdom.UI;

namespace BeeKingdom.Gameplay.Progression
{
    public static class SpeedUpInventory
    {
        private static readonly Dictionary<string, SpeedUpStack> stacks = new Dictionary<string, SpeedUpStack>(64);
        private static readonly Dictionary<SpeedUpCategory, List<SpeedUpStack>> byCategory = new Dictionary<SpeedUpCategory, List<SpeedUpStack>>();

        static SpeedUpInventory()
        {
            foreach (SpeedUpCategory cat in Enum.GetValues(typeof(SpeedUpCategory)))
            {
                byCategory[cat] = new List<SpeedUpStack>();
            }
        }

        public static void Add(string itemId, int count = 1)
        {
            if (count <= 0) return;
            var item = SpeedUpRegistry.Get(itemId);
            if (item == null) return;

            if (stacks.TryGetValue(itemId, out SpeedUpStack stack))
            {
                stack.Count = Math.Min(stack.Count + count, item.StackSize);
            }
            else
            {
                var newStack = new SpeedUpStack(item, count);
                stacks[itemId] = newStack;
                byCategory[item.Category].Add(newStack);
            }
        }

        public static bool Remove(string itemId, int count = 1)
        {
            if (count <= 0) return false;
            if (!stacks.TryGetValue(itemId, out SpeedUpStack stack)) return false;
            if (stack.Count < count) return false;

            stack.Count -= count;
            if (stack.Count <= 0)
            {
                stacks.Remove(itemId);
                byCategory[stack.Item.Category].Remove(stack);
            }
            return true;
        }

        public static int GetCount(string itemId)
        {
            return stacks.TryGetValue(itemId, out SpeedUpStack stack) ? stack.Count : 0;
        }

        public static IReadOnlyList<SpeedUpStack> GetStacks(SpeedUpCategory category)
        {
            if (byCategory.TryGetValue(category, out List<SpeedUpStack> list))
            {
                list.Sort((left, right) => left.Item.DurationSeconds.CompareTo(right.Item.DurationSeconds));
                return list;
            }
            return Array.Empty<SpeedUpStack>();
        }

        public static IReadOnlyList<SpeedUpStack> GetAllStacks()
        {
            return new List<SpeedUpStack>(stacks.Values);
        }

        public static SpeedUpStack GetStack(string itemId)
        {
            stacks.TryGetValue(itemId, out SpeedUpStack stack);
            return stack;
        }
    }

    public static class SpeedUpAutoUse
    {
        public sealed class AutoUsePlan
        {
            public readonly List<SpeedUpStack> Stacks = new List<SpeedUpStack>();
            public readonly long TotalSeconds;
            public readonly long WasteSeconds;

            public AutoUsePlan(List<SpeedUpStack> stacks, long targetSeconds)
            {
                Stacks = stacks;
                TotalSeconds = SumDuration(stacks);
                WasteSeconds = Math.Max(0, TotalSeconds - targetSeconds);
            }

            private static int StackCount(SpeedUpStack s) => s.Count;

            private static long SumDuration(IReadOnlyList<SpeedUpStack> values)
            {
                long total = 0L;
                for (int i = 0; i < values.Count; i++)
                    total += values[i].Item.DurationSeconds * (long)StackCount(values[i]);
                return total;
            }
        }

        public static AutoUsePlan ComputeBestPlan(SpeedUpCategory category, long remainingSeconds, long maxWasteSeconds = long.MaxValue)
        {
            SmartSpeedUpPlan smartPlan = SmartSpeedUpCalculator.ComputePlan(
                new SpeedUpDialogContext(category, string.Empty, remainingSeconds));
            if (smartPlan == null || smartPlan.WasteSeconds > maxWasteSeconds) return null;

            var plan = new List<SpeedUpStack>(smartPlan.Entries.Count);
            for (int i = 0; i < smartPlan.Entries.Count; i++)
            {
                SmartSpeedUpPlan.Entry entry = smartPlan.Entries[i];
                plan.Add(new SpeedUpStack(entry.Item, entry.Count));
            }
            return new AutoUsePlan(plan, remainingSeconds);
        }

        public static bool ApplyPlan(AutoUsePlan plan, SpeedUpCategory category)
        {
            if (plan == null) return false;

            foreach (var stack in plan.Stacks)
            {
                if (!SpeedUpInventory.Remove(stack.Item.Id, StackCount(stack)))
                    return false;
            }
            return true;
        }

        private static int StackCount(SpeedUpStack s) => s.Count;

        public static long GetAvailableTotalSeconds(SpeedUpCategory category)
        {
            long total = 0;
            foreach (var stack in SpeedUpInventory.GetStacks(category))
                total += stack.TotalDurationSeconds;
            if (category != SpeedUpCategory.Universal)
            {
                foreach (var stack in SpeedUpInventory.GetStacks(SpeedUpCategory.Universal))
                    total += stack.TotalDurationSeconds;
            }
            return total;
        }
    }
}
