namespace BeeKingdom.Shared.Versioning;

public readonly record struct ContractVersion(int Major, int Minor, int Patch)
{
    public static ContractVersion Current { get; } = new(1, 0, 0);
    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
