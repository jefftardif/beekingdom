namespace BeeKingdom.Shared.Utilities;

public static class ContractGuard
{
    public static string NotEmpty(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value;
    }
}
