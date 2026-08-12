using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Chat.Translations;

public sealed class ChatTranslationService(IChatRepository chat, IChatTranslationRepository cache, IChatTranslationProvider provider, IChatTranslationRateLimiter rateLimiter, IServerClock clock, IOptions<ChatOptions> options, ChatTranslationDiagnostics diagnostics)
{
    private const int MaxLocaleLength = 35;
    private const int MaxModelVersionLength = 128;

    public async Task<ChatTranslationResult> TranslateAsync(PlayerId playerId, Guid routeMessageId, ChatTranslationRequest request, CancellationToken cancellationToken)
    {
        var timer = diagnostics.Start();
        ChatOptions settings = options.Value;
        if (!settings.Enabled) throw new InvalidOperationException("chat_disabled");
        if (routeMessageId != request.MessageId) throw new ArgumentException("message_id_mismatch");
        string target = request.TargetLocale ?? string.Empty;
        string version = request.ModelVersion ?? string.Empty;
        if (!IsSimpleBcp47(target)) throw new ArgumentException("target_locale_invalid");
        if (!IsModelVersion(version)) throw new ArgumentException("model_version_invalid");
        if (!string.Equals(version, settings.TranslationModelVersion, StringComparison.Ordinal)) throw new ArgumentException("model_version_not_supported");
        ChatMessage message = chat.GetMessage(routeMessageId) ?? throw new KeyNotFoundException("message_not_found");
        ChatConversationParticipant participant = chat.GetParticipant(message.ConversationId, playerId) ?? throw new UnauthorizedAccessException("chat_read_forbidden");
        if (!participant.CanRead || participant.RemovedAtUtc != null) throw new UnauthorizedAccessException("chat_read_forbidden");
        if (message.Body.Length > settings.TranslationMaxCharacters) throw new ArgumentException("translation_text_too_large");
        ChatTranslationCacheEntry? cached = cache.Get(routeMessageId, target, version);
        if (cached != null) { diagnostics.Complete("cache", timer); return Result(cached); }
        if (!rateLimiter.TryAcquire(playerId, clock.UtcNow)) { diagnostics.Complete("rate_limited", timer); throw new InvalidOperationException("translation_rate_limited"); }
        string translated;
        try { translated = await provider.TranslateAsync(new(routeMessageId, message.Body, settings.TranslationSourceLocale, target, version), cancellationToken); }
        catch (InvalidOperationException exception) when (exception.Message == "translation_provider_unavailable") { diagnostics.Complete("provider_unavailable", timer); throw; }
        if (string.IsNullOrWhiteSpace(translated) || translated.Length > 16000 || !IsSimpleBcp47(settings.TranslationSourceLocale)) throw new InvalidOperationException("translation_response_mismatch");
        ChatTranslationCacheEntry saved = cache.SaveIfAbsent(new(routeMessageId,target,version,settings.TranslationSourceLocale,translated,clock.UtcNow));
        diagnostics.Complete("success", timer);
        return Result(saved);
    }
    public static bool IsSimpleBcp47(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length is < 2 or > MaxLocaleLength || value[0] == '-' || value[^1] == '-' || value.Contains("--", StringComparison.Ordinal)) return false;
        foreach (char c in value) if (!((c is >= 'A' and <= 'Z') || (c is >= 'a' and <= 'z') || (c is >= '0' and <= '9') || c == '-')) return false;
        return true;
    }
    public static bool IsModelVersion(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxModelVersionLength) return false;
        foreach (char c in value) if (!((c is >= 'A' and <= 'Z') || (c is >= 'a' and <= 'z') || (c is >= '0' and <= '9') || c is '.' or '_' or '-')) return false;
        return true;
    }
    private static ChatTranslationResult Result(ChatTranslationCacheEntry value) => new(value.MessageId,value.SourceLocale,value.TargetLocale,value.ModelVersion,value.TranslatedText,"completed");
}
