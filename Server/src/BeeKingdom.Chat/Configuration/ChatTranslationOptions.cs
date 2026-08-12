namespace BeeKingdom.Chat.Configuration;

public sealed class ChatTranslationOptions
{
    public const string SectionName = "ChatTranslation";
    public const string DeepLProviderName = "deepl";

    public string Provider { get; init; } = "none";
    public string? ApiKey { get; init; }
    public string? Endpoint { get; init; }
    public int TimeoutSeconds { get; init; } = 10;
}
