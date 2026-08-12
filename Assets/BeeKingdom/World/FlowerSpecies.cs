namespace BeeKingdom.World
{
    public sealed class FlowerSpecies
    {
        public string SpeciesId { get; }
        public string DisplayName { get; }
        public double NectarCapacity { get; }
        public double PollenCapacity { get; }
        public BloomCycle BloomCycle { get; }
        public PollinationRules PollinationRules { get; }

        public FlowerSpecies(string speciesId, string displayName, double nectarCapacity, double pollenCapacity, BloomCycle bloomCycle, PollinationRules pollinationRules)
        {
            SpeciesId = string.IsNullOrWhiteSpace(speciesId) ? "flower" : speciesId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? SpeciesId : displayName;
            NectarCapacity = nectarCapacity < 0d ? 0d : nectarCapacity;
            PollenCapacity = pollenCapacity < 0d ? 0d : pollenCapacity;
            BloomCycle = bloomCycle ?? BloomCycle.CreateDefault();
            PollinationRules = pollinationRules ?? PollinationRules.CreateDefault();
        }
    }
}
