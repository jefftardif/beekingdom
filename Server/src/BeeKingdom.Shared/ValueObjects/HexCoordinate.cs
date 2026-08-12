namespace BeeKingdom.Shared.ValueObjects;

public readonly record struct HexCoordinate(int Q, int R)
{
    public int S => -Q - R;
}
