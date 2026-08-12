using System.Collections.Generic;

namespace BeeKingdom.Economy
{
    public sealed class ResourceStorage
    {
        private readonly Dictionary<ResourceType, double> amounts = new Dictionary<ResourceType, double>();
        private readonly Dictionary<ResourceType, double> reserved = new Dictionary<ResourceType, double>();
        private readonly Dictionary<ResourceType, double> capacities = new Dictionary<ResourceType, double>();

        public string StorageId { get; }

        public ResourceStorage(string storageId)
        {
            StorageId = storageId;
        }

        public void SetCapacity(ResourceType type, double capacity)
        {
            capacities[type] = capacity < 0d ? 0d : capacity;
        }

        public double GetAmount(ResourceType type) => amounts.TryGetValue(type, out double value) ? value : 0d;
        public double GetReserved(ResourceType type) => reserved.TryGetValue(type, out double value) ? value : 0d;
        public double GetAvailable(ResourceType type) => GetAmount(type) - GetReserved(type);
        public double GetCapacity(ResourceType type) => capacities.TryGetValue(type, out double value) ? value : double.MaxValue;

        public double Store(ResourceType type, double amount)
        {
            double current = GetAmount(type);
            double capacity = GetCapacity(type);
            double accepted = current + amount > capacity ? capacity - current : amount;
            if (accepted <= 0d) return 0d;
            amounts[type] = current + accepted;
            return accepted;
        }

        public bool Reserve(ResourceType type, double amount)
        {
            if (amount <= 0d || GetAvailable(type) < amount) return false;
            reserved[type] = GetReserved(type) + amount;
            return true;
        }

        public bool Release(ResourceType type, double amount)
        {
            double current = GetReserved(type);
            if (amount <= 0d || current < amount) return false;
            reserved[type] = current - amount;
            return true;
        }

        public bool Consume(ResourceType type, double amount)
        {
            if (amount <= 0d || GetReserved(type) < amount || GetAmount(type) < amount) return false;
            reserved[type] = GetReserved(type) - amount;
            amounts[type] = GetAmount(type) - amount;
            return true;
        }
    }
}
