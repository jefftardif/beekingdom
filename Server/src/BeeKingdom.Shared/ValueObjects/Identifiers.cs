namespace BeeKingdom.Shared.ValueObjects;

public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct ColonyId(Guid Value)
{
    public static ColonyId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct WorldId(Guid Value)
{
    public static WorldId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct GameServerId(Guid Value)
{
    public static GameServerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct BeeId(Guid Value)
{
    public static BeeId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct BuildingId(Guid Value)
{
    public static BuildingId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct ChamberId(Guid Value)
{
    public static ChamberId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct AllianceId(Guid Value)
{
    public static AllianceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
