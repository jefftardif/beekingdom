using System.Collections.Generic;

namespace BeeKingdom.Economy
{
    public sealed class ResourceFlowRoute
    {
        public string RouteId { get; }
        public string SourceStorageId { get; }
        public string DestinationStorageId { get; }
        public ResourceType ResourceType { get; }
        public double MaxAmountPerExecution { get; }

        public ResourceFlowRoute(string routeId, string sourceStorageId, string destinationStorageId, ResourceType resourceType, double maxAmountPerExecution)
        {
            RouteId = string.IsNullOrWhiteSpace(routeId) ? throw new System.ArgumentException("Route id is required.", nameof(routeId)) : routeId;
            SourceStorageId = sourceStorageId ?? string.Empty;
            DestinationStorageId = destinationStorageId ?? string.Empty;
            ResourceType = resourceType;
            MaxAmountPerExecution = maxAmountPerExecution < 0d ? 0d : maxAmountPerExecution;
        }
    }

    public readonly struct ResourceFlowRequest
    {
        public string RouteId { get; }
        public double Amount { get; }
        public double TimeSeconds { get; }

        public ResourceFlowRequest(string routeId, double amount, double timeSeconds)
        {
            RouteId = routeId;
            Amount = amount < 0d ? 0d : amount;
            TimeSeconds = timeSeconds < 0d ? 0d : timeSeconds;
        }
    }

    public sealed class ResourceFlowEngine
    {
        private readonly Dictionary<string, ResourceFlowRoute> routes = new Dictionary<string, ResourceFlowRoute>();
        private readonly ResourceFlowManager flowManager;

        public int RouteCount => routes.Count;
        public int ExecutedFlows { get; private set; }

        public ResourceFlowEngine(ResourceFlowManager flowManager = null)
        {
            this.flowManager = flowManager ?? new ResourceFlowManager();
        }

        public bool RegisterRoute(ResourceFlowRoute route)
        {
            if (route == null || routes.ContainsKey(route.RouteId)) return false;
            routes.Add(route.RouteId, route);
            return true;
        }

        public bool Execute(ResourceFlowRequest request)
        {
            if (!routes.TryGetValue(request.RouteId, out ResourceFlowRoute route)) return false;
            double amount = System.Math.Min(request.Amount, route.MaxAmountPerExecution);
            bool transferred = flowManager.Transfer(route.SourceStorageId, route.DestinationStorageId, route.ResourceType, amount, request.TimeSeconds);
            if (transferred) ExecutedFlows++;
            return transferred;
        }

        public ResourceFlowManager GetFlowManager() => flowManager;
    }
}
