using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D
{
    public enum BuildingType
    {
        NURSERY = 0,
        HONEY_RESERVE = 1,
        BARRACK = 2,
        DEFENSE = 3,
        GENETICS = 4,
        RESEARCH = 5,
        WAREHOUSE = 6,
        TRANSFORMATION = 7,
        INFIRMARY = 8,
        ACADEMY = 9,
        BANK = 10,
        ROYAL_PALACE = 11,
        CHAMPION_HALL = 12,
        ALLIANCE_CENTER = 13,
    }

    public sealed class TestBuildingPlaceholder : MonoBehaviour
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private BuildingType buildingType = BuildingType.NURSERY;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public BuildingType BuildingType
        {
            get => buildingType;
            set => buildingType = value;
        }
    }
}
