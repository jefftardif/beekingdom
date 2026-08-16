using System;

namespace BeeKingdom.Buildings.Interaction
{
    public sealed class SelectionChangedEventArgs : EventArgs
    {
        public BuildingDefinition Building { get; }
        public BuildingDefinition Previous { get; }
        public bool IsSelected { get; }

        public SelectionChangedEventArgs(BuildingDefinition building, BuildingDefinition previous, bool isSelected)
        {
            Building = building;
            Previous = previous;
            IsSelected = isSelected;
        }
    }

    public interface ISelectionService
    {
        event Action<BuildingDefinition> BuildingClicked;
        event Action<SelectionChangedEventArgs> SelectionChanged;

        BuildingDefinition CurrentSelection { get; }
        bool HasSelection { get; }

        void Select(BuildingDefinition building);
        void SelectByBuildingType(string buildingType);
        void SelectByLegacyKey(string legacyKey);
        void Deselect();
        bool IsSelected(BuildingDefinition building);
    }

    public sealed class BuildingSelectionService : ISelectionService
    {
        public event Action<BuildingDefinition> BuildingClicked;
        public event Action<SelectionChangedEventArgs> SelectionChanged;

        private BuildingDefinition _current;

        public BuildingDefinition CurrentSelection
        {
            get { return _current; }
        }

        public bool HasSelection
        {
            get { return _current != null; }
        }

        public void NotifyClicked(BuildingDefinition building)
        {
            if (building == null) return;
            Action<BuildingDefinition> handler = BuildingClicked;
            if (handler != null) handler(building);
        }

        public void Select(BuildingDefinition building)
        {
            if (building == null)
            {
                Deselect();
                return;
            }

            BuildingDefinition previous = _current;
            _current = building;

            Action<SelectionChangedEventArgs> handler = SelectionChanged;
            if (handler != null) handler(new SelectionChangedEventArgs(building, previous, true));
        }

        public void SelectByBuildingType(string buildingType)
        {
            BuildingDefinition definition = BuildingCatalog.GetByBuildingType(buildingType);
            Select(definition);
        }

        public void SelectByLegacyKey(string legacyKey)
        {
            BuildingDefinition definition = BuildingCatalog.GetByLegacyKey(legacyKey);
            Select(definition);
        }

        public void Deselect()
        {
            BuildingDefinition previous = _current;
            if (previous == null) return;
            _current = null;

            Action<SelectionChangedEventArgs> handler = SelectionChanged;
            if (handler != null) handler(new SelectionChangedEventArgs(null, previous, false));
        }

        public bool IsSelected(BuildingDefinition building)
        {
            return building != null && _current != null && _current.BuildingType == building.BuildingType;
        }
    }
}