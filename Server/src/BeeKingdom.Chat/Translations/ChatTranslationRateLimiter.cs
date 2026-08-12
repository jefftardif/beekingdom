using System.Collections.Concurrent;
using BeeKingdom.Chat.Configuration;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Chat.Translations;

public sealed class ChatTranslationRateLimiter(IOptions<ChatOptions> options) : IChatTranslationRateLimiter
{
    private readonly ConcurrentDictionary<Guid, Queue<DateTimeOffset>> attempts = new();
    private readonly int limit = options.Value.TranslationsPerMinutePerPlayer;

    public bool TryAcquire(PlayerId playerId, DateTimeOffset nowUtc)
    {
        Queue<DateTimeOffset> queue = attempts.GetOrAdd(playerId.Value, _ => new());
        lock (queue)
        {
            while (queue.Count > 0 && queue.Peek() <= nowUtc.AddMinutes(-1)) queue.Dequeue();
            if (queue.Count >= limit) return false;
            queue.Enqueue(nowUtc);
            return true;
        }
    }
}
