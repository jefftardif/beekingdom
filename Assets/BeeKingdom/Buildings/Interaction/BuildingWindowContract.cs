namespace BeeKingdom.Buildings.Interaction
{
    public sealed class BuildingWindowContext
    {
        public BuildingDefinition Building { get; }
        public string BuildingType { get; }
        public string LegacyKey { get; }
        public string DisplayName { get; }
        public string Role { get; }
        public int ZoneNumber { get; }
        public BuildingState State { get; }
        public BuildingCapabilities Capabilities { get; }
        public BuildingResource ProductionResource { get; }
        public bool IsUpgradable { get; }

        public BuildingWindowContext(BuildingDefinition building)
        {
            Building = building;
            BuildingType = building.BuildingType;
            LegacyKey = building.LegacyKey;
            DisplayName = building.DisplayName;
            Role = building.Role;
            ZoneNumber = building.ZoneNumber;
            State = building.State;
            Capabilities = building.Capabilities;
            ProductionResource = building.ProductionResource;
            IsUpgradable = building.IsUpgradable;
        }
    }

    public interface IBuildingWindowHost
    {
        void Open(BuildingWindowContext context);
        void Close();
        bool IsOpen { get; }
    }

    public static class BuildingWindowRouter
    {
        private static IBuildingWindowHost _host;

        public static IBuildingWindowHost Host
        {
            get { return _host; }
            set { _host = value; }
        }

        public static bool TryOpen(BuildingDefinition building)
        {
            if (building == null || _host == null) return false;
            _host.Open(new BuildingWindowContext(building));
            return true;
        }

        public static bool TryClose()
        {
            if (_host == null) return false;
            _host.Close();
            return true;
        }
    }
}