using BeeKingdom.Economy;

namespace BeeKingdom.World
{
    public sealed class NaturalResourceNode
    {
        public string NodeId { get; }
        public string RegionId { get; }
        public HexCoordinates Coordinates { get; }
        public ResourceType ResourceType { get; }
        public double Capacity { get; }
        public double Amount { get; private set; }
        public ResourceNodeLifecycle Lifecycle { get; }
        public ResourceNodeState State { get; private set; }

        public NaturalResourceNode(string nodeId, string regionId, HexCoordinates coordinates, ResourceType resourceType, double capacity, double initialAmount, ResourceNodeLifecycle lifecycle)
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? System.Guid.NewGuid().ToString("N") : nodeId;
            RegionId = regionId;
            Coordinates = coordinates;
            ResourceType = resourceType;
            Capacity = capacity < 0d ? 0d : capacity;
            Amount = System.Math.Min(Capacity, System.Math.Max(0d, initialAmount));
            Lifecycle = lifecycle ?? new ResourceNodeLifecycle(0.01d, 0.25d);
            State = Lifecycle.Resolve(Amount, Capacity);
        }

        public double Harvest(double amount)
        {
            double harvested = System.Math.Min(Amount, System.Math.Max(0d, amount));
            Amount -= harvested;
            State = Lifecycle.Resolve(Amount, Capacity);
            return harvested;
        }

        public bool Regenerate(double deltaSeconds, EcologicalBalance balance)
        {
            ResourceNodeState previous = State;
            Amount = System.Math.Min(Capacity, Amount + Lifecycle.RegenerationPerSecond * balance.CombinedFactor * System.Math.Max(0d, deltaSeconds));
            State = Lifecycle.Resolve(Amount, Capacity);
            return previous != ResourceNodeState.Available && State == ResourceNodeState.Available;
        }
    }
}
