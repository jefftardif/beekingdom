using System.Collections.Generic;

namespace BeeKingdom.World
{
    public sealed class WorldValidationResult
    {
        public bool IsValid => Issues.Count == 0;
        public List<string> Issues { get; } = new List<string>();
    }

    public sealed class WorldLayoutValidator
    {
        public WorldValidationResult Validate(WorldState world)
        {
            WorldValidationResult result = new WorldValidationResult();
            if (world == null)
            {
                result.Issues.Add("World is null.");
                return result;
            }

            if (world.Regions.Count == 0)
            {
                result.Issues.Add("World has no regions.");
            }

            foreach (WorldRegion region in world.Regions.Values)
            {
                if (string.IsNullOrWhiteSpace(region.RegionId)) result.Issues.Add("Region id is missing.");
                if (region.Richness <= 0d) result.Issues.Add(region.RegionId + " has no richness.");
                if (region.FloralSpecies.Count == 0) result.Issues.Add(region.RegionId + " has no floral species.");
                if (region.Resources.Count == 0) result.Issues.Add(region.RegionId + " has no resources.");
            }

            return result;
        }
    }
}
