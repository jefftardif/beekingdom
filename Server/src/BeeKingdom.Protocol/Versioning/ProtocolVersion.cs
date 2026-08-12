namespace BeeKingdom.Protocol.Versioning;

public readonly record struct ProtocolVersion(int Major, int Minor)
{
    public static ProtocolVersion Current { get; } = new(1, 0);
    public override string ToString() => $"{Major}.{Minor}";
}
