namespace BeeKingdom.World
{
    public sealed class ResourceNodeLifecycle
    {
        public double RegenerationPerSecond { get; }
        public double AvailableThreshold { get; }

        public ResourceNodeLifecycle(double regenerationPerSecond, double availableThreshold)
        {
            RegenerationPerSecond = regenerationPerSecond < 0d ? 0d : regenerationPerSecond;
            AvailableThreshold = availableThreshold < 0d ? 0d : availableThreshold;
        }

        public ResourceNodeState Resolve(double amount, double capacity)
        {
            if (capacity <= 0d || amount <= 0d) return ResourceNodeState.Depleted;
            if (amount >= capacity * AvailableThreshold) return ResourceNodeState.Available;
            return ResourceNodeState.Growing;
        }
    }
}
