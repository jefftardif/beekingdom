using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Chat.Realtime;

public sealed class SignalRChatRealtimeDispatcher : IChatRealtimeDispatcher
{
    public const string EventMethodName = "chat.event";

    private readonly IHubContext<ChatRealtimeHub> hubContext;
    private readonly ChatOptions options;

    public SignalRChatRealtimeDispatcher(IHubContext<ChatRealtimeHub> hubContext, IOptions<ChatOptions> options)
    {
        this.hubContext = hubContext;
        this.options = options.Value;
    }

    public Task PublishAsync(ChatEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled || !options.RealtimeEnabled)
        {
            return Task.CompletedTask;
        }

        return hubContext.Clients
            .Group(ChatRealtimeGroups.Conversation(envelope.ConversationId))
            .SendAsync(EventMethodName, envelope, cancellationToken);
    }
}
