namespace BeeKingdom.Buildings.Interaction
{
    public sealed class BuildingDefinition
    {
        public string BuildingType { get; }
        public string LegacyKey { get; }
        public string DisplayName { get; }
        public string Role { get; }
        public int ZoneNumber { get; }
        public string CellId { get; }
        public string IconId { get; }
        public BuildingState State { get; }
        public string ActionLabel { get; }
        public string Disclosure { get; }

        public BuildingCapabilities Capabilities { get; }

        public BuildingResource ProductionResource { get; }

        public int Level { get; }
        public long ProductionCapacity { get; }
        public bool IsUpgradable { get; }
        public bool StateIsFuture { get; }
        public bool StateIsActive { get; }
        public bool StateIsPreview { get; }

        public BuildingDefinition(
            string buildingType,
            string legacyKey,
            string displayName,
            string role,
            int zoneNumber,
            string cellId,
            string iconId,
            BuildingState state,
            string actionLabel,
            string disclosure,
            BuildingCapabilities capabilities,
            BuildingResource productionResource,
            int level = 0,
            long productionCapacity = 0L,
            bool isUpgradable = false)
        {
            BuildingType = buildingType;
            LegacyKey = legacyKey;
            DisplayName = displayName;
            Role = role;
            ZoneNumber = zoneNumber;
            CellId = cellId;
            IconId = iconId;
            State = state;
            ActionLabel = actionLabel;
            Disclosure = disclosure;
            Capabilities = capabilities;
            ProductionResource = productionResource;
            Level = level;
            ProductionCapacity = productionCapacity;
            IsUpgradable = isUpgradable;
            StateIsFuture = state == BuildingState.Future;
            StateIsActive = state == BuildingState.Active;
            StateIsPreview = state == BuildingState.Preview;
        }
    }
}