using UnityEngine;

namespace BeeKingdom.Buildings.Interaction
{
    public sealed class BuildingInteractionComponent : MonoBehaviour
    {
        [SerializeField] private string _buildingType;

        public string BuildingType
        {
            get { return _buildingType; }
        }

        public BuildingDefinition Definition
        {
            get
            {
                BuildingDefinition definition;
                if (!BuildingCatalog.TryGetByBuildingType(_buildingType, out definition)) return null;
                return definition;
            }
        }

        public void Configure(BuildingDefinition definition)
        {
            _buildingType = definition != null ? definition.BuildingType : string.Empty;
        }

        public void Configure(string buildingType)
        {
            _buildingType = buildingType;
        }
    }
}