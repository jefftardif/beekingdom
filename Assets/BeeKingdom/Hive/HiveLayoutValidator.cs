using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public sealed class HiveLayoutValidationResult
    {
        public bool IsValid => IsolatedChamberIds.Count == 0 && InaccessibleChamberIds.Count == 0 && CapacityExceededChamberIds.Count == 0;
        public List<string> IsolatedChamberIds { get; } = new List<string>();
        public List<string> InaccessibleChamberIds { get; } = new List<string>();
        public List<string> CapacityExceededChamberIds { get; } = new List<string>();
    }

    public sealed class HiveLayoutValidator
    {
        public HiveLayoutValidationResult Validate(HiveTopology topology)
        {
            HiveLayoutValidationResult result = new HiveLayoutValidationResult();
            if (topology.Chambers.Count == 0)
            {
                return result;
            }

            string root = FindEntrance(topology) ?? FirstChamber(topology);
            HashSet<string> reachable = Traverse(topology, root);

            foreach (HiveChamber chamber in topology.Chambers.Values)
            {
                if (chamber.Connections.Count == 0 && topology.Chambers.Count > 1)
                {
                    result.IsolatedChamberIds.Add(chamber.ChamberId);
                }

                if (!reachable.Contains(chamber.ChamberId))
                {
                    result.InaccessibleChamberIds.Add(chamber.ChamberId);
                }

                if (chamber.CellIds.Count > chamber.Capacity)
                {
                    result.CapacityExceededChamberIds.Add(chamber.ChamberId);
                }
            }

            return result;
        }

        private static string FindEntrance(HiveTopology topology)
        {
            foreach (HiveChamber chamber in topology.Chambers.Values)
            {
                if (chamber.ChamberType == HiveChamberType.Entrance)
                {
                    return chamber.ChamberId;
                }
            }

            return null;
        }

        private static string FirstChamber(HiveTopology topology)
        {
            foreach (string chamberId in topology.Chambers.Keys)
            {
                return chamberId;
            }

            return null;
        }

        private static HashSet<string> Traverse(HiveTopology topology, string root)
        {
            HashSet<string> visited = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(root))
            {
                return visited;
            }

            Queue<string> queue = new Queue<string>();
            queue.Enqueue(root);
            visited.Add(root);

            while (queue.Count > 0)
            {
                HiveChamber chamber = topology.GetChamber(queue.Dequeue());
                foreach (string next in chamber.Connections)
                {
                    if (topology.Chambers.ContainsKey(next) && visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return visited;
        }
    }
}
