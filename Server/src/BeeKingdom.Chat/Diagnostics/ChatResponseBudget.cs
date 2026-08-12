namespace BeeKingdom.Chat.Diagnostics;

public static class ChatResponseBudget
{
    public const int DefaultBytes = 1_048_576;
    public const int MinimumBytes = 1_024;
    public const int MaximumBytes = 4_194_304;

    public static bool IsValidConfiguration(int bytes) => bytes is >= MinimumBytes and <= MaximumBytes;

    public static bool IsWithinLimit(ReadOnlySpan<byte> payload, int limitBytes = DefaultBytes) =>
        IsValidConfiguration(limitBytes) && payload.Length <= limitBytes;

    public static bool IsJsonContentType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType)
        && contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
}
