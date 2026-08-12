using BeeKingdom.Gateway.Configuration;
using BeeKingdom.Gateway.Models;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Protocol.Messages;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Gateway.RateLimiting;

public sealed class GatewayRateLimiter
{
    private readonly Dictionary<string, Counter> counters = new(StringComparer.Ordinal);
    private readonly IServerClock clock;
    private readonly GatewayOptions options;
    private readonly object sync = new();

    public GatewayRateLimiter(IServerClock clock, IOptions<GatewayOptions> options)
    {
        this.clock = clock;
        this.options = options.Value;
    }

    public bool IsAllowed(GatewayConnection connection, ProtocolMessageType messageType, out string scope)
    {
        lock (sync)
        {
            if (!Increment($"player:{connection.PlayerId}", options.PlayerMessagesPerMinute))
            {
                scope = "player";
                return false;
            }

            if (!Increment($"session:{connection.SessionId}", options.SessionMessagesPerMinute))
            {
                scope = "session";
                return false;
            }

            if (!Increment($"ip:{connection.IpAddress}", options.IpMessagesPerMinute))
            {
                scope = "ip";
                return false;
            }

            if (!Increment($"type:{messageType}", options.MessageTypePerMinute))
            {
                scope = "message_type";
                return false;
            }

            scope = string.Empty;
            return true;
        }
    }

    private bool Increment(string key, int limit)
    {
        DateTimeOffset now = clock.UtcNow;
        if (!counters.TryGetValue(key, out Counter? counter) || now >= counter.WindowEndsUtc)
        {
            counters[key] = new Counter(1, now.AddMinutes(1));
            return true;
        }

        if (counter.Count >= limit)
        {
            return false;
        }

        counters[key] = counter with { Count = counter.Count + 1 };
        return true;
    }

    private sealed record Counter(int Count, DateTimeOffset WindowEndsUtc);
}
