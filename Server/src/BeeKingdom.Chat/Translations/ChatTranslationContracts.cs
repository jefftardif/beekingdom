using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Chat.Translations;

public sealed record ChatTranslationRequest(Guid MessageId, string TargetLocale, string ModelVersion);
public sealed record ChatTranslationResult(Guid MessageId, string SourceLocale, string TargetLocale, string ModelVersion, string TranslatedText, string Status);
public sealed record ChatTranslationError(string Code, string? Message, int? RetryAfterSeconds = null);
public sealed record ChatTranslationCacheEntry(Guid MessageId, string TargetLocale, string ModelVersion, string SourceLocale, string TranslatedText, DateTimeOffset CreatedAtUtc);
public sealed record ChatTranslationInput(Guid MessageId, string OriginalText, string SourceLocale, string TargetLocale, string ModelVersion);

public interface IChatTranslationProvider
{
    Task<string> TranslateAsync(ChatTranslationInput input, CancellationToken cancellationToken);
}

public interface IChatTranslationRepository
{
    ChatTranslationCacheEntry? Get(Guid messageId, string targetLocale, string modelVersion);
    ChatTranslationCacheEntry SaveIfAbsent(ChatTranslationCacheEntry entry);
}

public interface IChatTranslationRateLimiter
{
    bool TryAcquire(PlayerId playerId, DateTimeOffset nowUtc);
}
