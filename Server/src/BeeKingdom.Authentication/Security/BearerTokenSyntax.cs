namespace BeeKingdom.Authentication.Security;

public static class BearerTokenSyntax
{
    public const int MaximumLength = 8192;

    public static bool IsValid(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > MaximumLength)
        {
            return false;
        }

        bool paddingStarted = false;
        bool payloadSeen = false;
        foreach (char value in token)
        {
            if (value == '=')
            {
                paddingStarted = true;
                continue;
            }

            if (paddingStarted || !IsB64TokenCharacter(value))
            {
                return false;
            }
            payloadSeen = true;
        }

        return payloadSeen;
    }

    private static bool IsB64TokenCharacter(char value) =>
        value is >= 'A' and <= 'Z'
        or >= 'a' and <= 'z'
        or >= '0' and <= '9'
        or '-' or '.' or '_' or '~' or '+' or '/';
}
