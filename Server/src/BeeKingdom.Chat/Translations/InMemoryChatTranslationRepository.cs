namespace BeeKingdom.Chat.Translations;

public sealed class InMemoryChatTranslationRepository : IChatTranslationRepository
{
    private readonly Dictionary<string, ChatTranslationCacheEntry> entries = new(StringComparer.Ordinal);
    private readonly object sync = new();
    public ChatTranslationCacheEntry? Get(Guid messageId, string targetLocale, string modelVersion)
    { lock (sync) return entries.GetValueOrDefault(Key(messageId, targetLocale, modelVersion)); }
    public ChatTranslationCacheEntry SaveIfAbsent(ChatTranslationCacheEntry entry)
    { lock (sync) { string key = Key(entry.MessageId, entry.TargetLocale, entry.ModelVersion); if (!entries.TryGetValue(key, out ChatTranslationCacheEntry? found)) entries[key] = found = entry; return found; } }
    private static string Key(Guid id, string locale, string version) => $"{id:N}:{locale}:{version}";
}
