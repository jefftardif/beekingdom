using System;

namespace BeeKingdom.Hive
{
    [Serializable]
    public sealed class HiveSnapshot
    {
        public string HiveId;
        public string OwnerId;
        public string QueenBeeId;
        public string[] BeeIds;
        public string[] BuildingIds;
        public string[] InventoryIds;
        public int MaxPopulation;
        public int MaxBuildings;
        public int MaxInventories;
    }
}
