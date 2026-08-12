using BeeKingdom.Chat.Models;

namespace BeeKingdom.Chat.Realtime;

public interface IChatRealtimeDispatcher
{
    Task PublishAsync(ChatEventEnvelope envelope, CancellationToken cancellationToken = default);
}
