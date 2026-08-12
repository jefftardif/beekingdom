using System;
using System.Collections.Generic;

namespace BeeKingdom.Rewards
{
    public enum RewardType
    {
        Resource,
        SpeedUp,
        Item,
        Champion,
        Experience,
        Vip,
        Gems,
        Equipment,
        Crest,
        Title,
        Collection,
        Future
    }

    public readonly struct Reward
    {
        public RewardType Type { get; }
        public string Id { get; }
        public long Amount { get; }

        public Reward(RewardType type, string id, long amount)
        {
            Type = type;
            Id = id ?? string.Empty;
            Amount = amount;
        }
    }

    public sealed class RewardBundle
    {
        private readonly List<Reward> rewards = new List<Reward>(4);

        public string Source { get; }
        public IReadOnlyList<Reward> Rewards => rewards;

        public RewardBundle(string source)
        {
            Source = string.IsNullOrWhiteSpace(source) ? "unknown" : source;
        }

        public RewardBundle Add(Reward reward)
        {
            rewards.Add(reward);
            return this;
        }
    }

    public interface IRewardInventory
    {
        bool TryAdd(Reward reward);
    }

    public interface IRewardHandler
    {
        bool TryApply(Reward reward, IRewardInventory inventory);
    }

    public interface IRewardAnalytics
    {
        void Record(RewardBundle bundle);
    }

    public interface IRewardPresentation
    {
        void Present(RewardBundle bundle);
    }

    public sealed class RewardHistory
    {
        private readonly List<RewardBundle> entries;
        private readonly int capacity;

        public IReadOnlyList<RewardBundle> Entries => entries;

        public RewardHistory(int capacity = 64)
        {
            this.capacity = Math.Max(1, capacity);
            entries = new List<RewardBundle>(this.capacity);
        }

        public void Add(RewardBundle bundle)
        {
            if (bundle == null) return;
            if (entries.Count == capacity) entries.RemoveAt(0);
            entries.Add(bundle);
        }
    }

    public sealed class RewardPipeline
    {
        private readonly Dictionary<RewardType, IRewardHandler> handlers = new Dictionary<RewardType, IRewardHandler>();
        private readonly IRewardInventory inventory;
        private readonly IRewardAnalytics analytics;
        private readonly IRewardPresentation presentation;

        public RewardHistory History { get; }

        public RewardPipeline(
            IRewardInventory inventory,
            IRewardAnalytics analytics = null,
            IRewardPresentation presentation = null,
            RewardHistory history = null)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.analytics = analytics;
            this.presentation = presentation;
            History = history ?? new RewardHistory();
        }

        public void RegisterHandler(RewardType type, IRewardHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            handlers[type] = handler;
        }

        public bool TryDispatch(RewardBundle bundle)
        {
            if (bundle == null || bundle.Rewards.Count == 0) return false;

            for (int i = 0; i < bundle.Rewards.Count; i++)
            {
                Reward reward = bundle.Rewards[i];
                if (reward.Amount <= 0 || string.IsNullOrWhiteSpace(reward.Id) || !handlers.ContainsKey(reward.Type))
                    return false;
            }

            for (int i = 0; i < bundle.Rewards.Count; i++)
            {
                Reward reward = bundle.Rewards[i];
                if (!handlers[reward.Type].TryApply(reward, inventory)) return false;
            }

            History.Add(bundle);
            analytics?.Record(bundle);
            presentation?.Present(bundle);
            return true;
        }
    }

    public sealed class InventoryRewardHandler : IRewardHandler
    {
        public bool TryApply(Reward reward, IRewardInventory inventory)
        {
            return inventory != null && inventory.TryAdd(reward);
        }
    }
}
