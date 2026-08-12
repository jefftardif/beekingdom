namespace BeeKingdom.Gameplay.Domain.Enums
{
    public enum BeeRole { Worker, Nurse, Builder, Defender, Scout, Queen }
    public enum BeeState { Idle, Working, Resting, Traveling, Injured }
    public enum BuildingType { QueensChamber, HoneyStorage, PollenStorage, WaxWorkshop, FlowerGarden, Barracks, ResearchLab, Market }
    public enum ResourceType { Honey, Pollen, Wax, RoyalJelly, Nectar }
    public enum TaskType { None, Gather, Build, Research, Train, Defend, Scout }
    public enum RegionType { Meadow, Forest, Garden, Wetland, Mountain, EnemyHive }
    public enum WeatherType { Clear, Cloudy, Rain, Wind, Storm }
    public enum Season { Spring, Summer, Autumn, Winter }
}
