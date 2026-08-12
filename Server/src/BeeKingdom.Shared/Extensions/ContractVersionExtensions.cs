using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.Extensions;

public static class ContractVersionExtensions
{
    public static bool IsCompatibleWithCurrent(this ContractVersion version)
    {
        return ContractCompatibility.IsCompatible(ContractVersion.Current, version);
    }
}
