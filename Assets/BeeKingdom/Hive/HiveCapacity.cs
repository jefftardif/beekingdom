namespace BeeKingdom.Hive
{
    public readonly struct HiveCapacity
    {
        public int MaxPopulation { get; }
        public int MaxBuildings { get; }
        public int MaxInventories { get; }

        public HiveCapacity(int maxPopulation, int maxBuildings, int maxInventories)
        {
            MaxPopulation = maxPopulation < 0 ? 0 : maxPopulation;
            MaxBuildings = maxBuildings < 0 ? 0 : maxBuildings;
            MaxInventories = maxInventories < 0 ? 0 : maxInventories;
        }
    }
}
