using BeeKingdom.Chat.Models;

namespace BeeKingdom.Chat.Realtime;

public sealed class NoopChatRealtimeDispatcher : IChatRealtimeDispatcher
{
    public Task PublishAsync(ChatEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
