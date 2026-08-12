namespace BeeKingdom.Shared.Versioning;

public static class ContractCompatibility
{
    public static bool IsCompatible(ContractVersion supported, ContractVersion requested)
    {
        if (supported.Major != requested.Major)
        {
            return false;
        }

        return requested.Minor <= supported.Minor;
    }
}
