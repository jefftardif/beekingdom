using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public sealed class ColonyTrafficRoute
    {
        public string RouteId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public int Capacity { get; }

        public ColonyTrafficRoute(string routeId, string fromNodeId, string toNodeId, int capacity)
        {
            RouteId = string.IsNullOrWhiteSpace(routeId) ? throw new System.ArgumentException("Route id is required.", nameof(routeId)) : routeId;
            FromNodeId = fromNodeId ?? string.Empty;
            ToNodeId = toNodeId ?? string.Empty;
            Capacity = capacity <= 0 ? 1 : capacity;
        }
    }

    public sealed class ColonyTrafficManager
    {
        private readonly Dictionary<string, ColonyTrafficRoute> routes = new Dictionary<string, ColonyTrafficRoute>();
        private readonly Dictionary<string, int> reservations = new Dictionary<string, int>();

        public int RouteCount => routes.Count;
        public int ReservationCount { get; private set; }

        public bool RegisterRoute(ColonyTrafficRoute route)
        {
            if (route == null || routes.ContainsKey(route.RouteId)) return false;
            routes.Add(route.RouteId, route);
            reservations[route.RouteId] = 0;
            return true;
        }

        public bool Reserve(string routeId)
        {
            if (!routes.TryGetValue(routeId, out ColonyTrafficRoute route)) return false;
            int current = reservations[routeId];
            if (current >= route.Capacity) return false;
            reservations[routeId] = current + 1;
            ReservationCount++;
            return true;
        }

        public bool Release(string routeId)
        {
            if (!reservations.TryGetValue(routeId, out int current) || current <= 0) return false;
            reservations[routeId] = current - 1;
            ReservationCount--;
            return true;
        }

        public bool TryFindRoute(string fromNodeId, string toNodeId, out ColonyTrafficRoute route)
        {
            foreach (ColonyTrafficRoute candidate in routes.Values)
            {
                if (candidate.FromNodeId == fromNodeId && candidate.ToNodeId == toNodeId)
                {
                    route = candidate;
                    return true;
                }
            }
            route = null;
            return false;
        }
    }
}
