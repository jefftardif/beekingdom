namespace BeeKingdom.Buildings.Interaction
{
    public static class BuildingCapabilitiesResolver
    {
        public static BuildingCapabilities Resolve(string buildingType, string legacyKey)
        {
            BuildingCapabilities capabilities = BuildingCapabilities.None;

            if (IsUpgradeableLegacy(legacyKey))
                capabilities |= BuildingCapabilities.Upgrade;

            if (ResourceOf(buildingType, legacyKey) != BuildingResource.None)
                capabilities |= BuildingCapabilities.Production;

            if (IsResearchLegacy(legacyKey))
                capabilities |= BuildingCapabilities.Research;

            return capabilities;
        }

        public static BuildingResource ResourceOf(string buildingType, string legacyKey)
        {
            if (legacyKey == BuildingLegacyKeys.HoneyStorage) return BuildingResource.Honey;
            if (legacyKey == BuildingLegacyKeys.WaxWorkshop) return BuildingResource.Wax;
            if (legacyKey == BuildingLegacyKeys.WarehouseCells) return BuildingResource.Pollen;
            return BuildingResource.None;
        }

        public static bool IsUpgradeableLegacy(string legacyKey)
        {
            if (legacyKey == BuildingLegacyKeys.HoneyStorage) return true;
            if (legacyKey == BuildingLegacyKeys.WaxWorkshop) return true;
            if (legacyKey == BuildingLegacyKeys.WarehouseCells) return true;
            if (legacyKey == BuildingLegacyKeys.AdministrationCore) return true;
            return false;
        }

        public static bool IsResearchLegacy(string legacyKey)
        {
            return legacyKey == BuildingLegacyKeys.ResearchNode;
        }
    }
}