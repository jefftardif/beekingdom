namespace BeeKingdom.Shared.Enums;

public enum ResponseStatus
{
    Success = 0,
    Failure = 1,
    ValidationError = 2,
    ServerError = 3
}

public enum NotificationSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public enum BeeRole
{
    Worker = 0,
    Builder = 1,
    Guard = 2,
    Scout = 3,
    Queen = 4
}

public enum BuildingKind
{
    Storage = 0,
    Nursery = 1,
    Workshop = 2,
    Defense = 3
}

public enum ChamberKind
{
    Brood = 0,
    HoneyStorage = 1,
    PollenStorage = 2,
    Royal = 3,
    Corridor = 4
}

public enum ResourceKind
{
    Nectar = 0,
    Pollen = 1,
    Water = 2,
    Wax = 3,
    Honey = 4,
    RoyalJelly = 5,
    Propolis = 6
}
