using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public sealed class HiveExpansionRequest
    {
        public HiveChamberType ChamberType { get; }
        public int Population { get; }
        public double AvailableWax { get; }
        public double TemperatureCelsius { get; }
        public bool PlayerApproved { get; }
        public IReadOnlyCollection<string> CompletedResearchIds { get; }

        public HiveExpansionRequest(HiveChamberType chamberType, int population, double availableWax, double temperatureCelsius, bool playerApproved, IReadOnlyCollection<string> completedResearchIds = null)
        {
            ChamberType = chamberType;
            Population = population;
            AvailableWax = availableWax;
            TemperatureCelsius = temperatureCelsius;
            PlayerApproved = playerApproved;
            CompletedResearchIds = completedResearchIds ?? new string[0];
        }
    }

    public sealed class HiveExpansionPlan
    {
        public bool IsApproved { get; }
        public string Reason { get; }
        public HiveChamberType ChamberType { get; }
        public HivePosition Position { get; }
        public double WaxCost { get; }
        public double RequiredWorkSeconds { get; }
        public int CellCount { get; }

        public HiveExpansionPlan(bool isApproved, string reason, HiveChamberType chamberType, HivePosition position, double waxCost, double requiredWorkSeconds, int cellCount)
        {
            IsApproved = isApproved;
            Reason = reason;
            ChamberType = chamberType;
            Position = position;
            WaxCost = waxCost;
            RequiredWorkSeconds = requiredWorkSeconds;
            CellCount = cellCount;
        }
    }

    public sealed class HiveExpansionPlanner
    {
        public HiveExpansionPlan PlanExpansion(HiveTopology topology, HiveExpansionRequest request)
        {
            double waxCost = GetWaxCost(request.ChamberType);
            if (!request.PlayerApproved)
            {
                return Reject(request, "Player decision is required.", waxCost);
            }

            if (request.Population < 5)
            {
                return Reject(request, "Population is too low.", waxCost);
            }

            if (request.AvailableWax < waxCost)
            {
                return Reject(request, "Insufficient wax reserves.", waxCost);
            }

            if (request.TemperatureCelsius < 18d || request.TemperatureCelsius > 38d)
            {
                return Reject(request, "Temperature is outside construction range.", waxCost);
            }

            HivePosition position = FindNextPosition(topology);
            int cells = request.ChamberType == HiveChamberType.RoyalChamber ? 3 : 6;
            return new HiveExpansionPlan(true, "Approved", request.ChamberType, position, waxCost, waxCost * 10d, cells);
        }

        private static HiveExpansionPlan Reject(HiveExpansionRequest request, string reason, double waxCost)
        {
            return new HiveExpansionPlan(false, reason, request.ChamberType, default, waxCost, waxCost * 10d, 0);
        }

        private static double GetWaxCost(HiveChamberType chamberType)
        {
            switch (chamberType)
            {
                case HiveChamberType.RoyalChamber: return 20d;
                case HiveChamberType.Defense: return 14d;
                case HiveChamberType.WaxWorkshop: return 12d;
                case HiveChamberType.Entrance: return 8d;
                default: return 10d;
            }
        }

        private static HivePosition FindNextPosition(HiveTopology topology)
        {
            int x = topology.Chambers.Count + 1;
            return new HivePosition(x, 0, 0);
        }
    }
}
