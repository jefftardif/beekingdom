namespace BeeKingdom.Chat.Configuration;

public sealed class ChatOptions
{
    public const string SectionName = "Chat";
    public const string DisabledTranslationModelVersion = "translation-disabled-v1";

    public bool Enabled { get; init; }
    public bool RealtimeEnabled { get; init; }
    public int BodyMaxCharacters { get; init; } = 500;
    public int MessagesPerMinutePerPlayer { get; init; } = 50;
    public int MessagesPerTenSecondsPerConversation { get; init; } = 10;
    public int PrivateConversationCreatesPerHour { get; init; } = 10;
    public int MaxPrivateRecipients { get; init; } = 20;
    public string ProtocolVersion { get; init; } = "chat-v1";
    public int TranslationMaxCharacters { get; init; } = 1000;
    public int TranslationsPerMinutePerPlayer { get; init; } = 10;
    public string TranslationModelVersion { get; init; } = DisabledTranslationModelVersion;
    public string TranslationSourceLocale { get; init; } = "fr-CA";
    public int IdempotencyReceiptRetentionDays { get; init; } = 30;
    public int MaxRequestBytes { get; init; } = 65_536;
    public int MaxRequestTargetBytes { get; init; } = 8_192;
}
