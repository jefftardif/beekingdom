using BeeKingdom.Chat.Configuration;
using BeeKingdom.Authentication;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Security;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Chat.Realtime;

public sealed class ChatRealtimeHub : Hub
{
    private readonly ChatOptions options;
    private readonly AuthenticationManager authentication;
    private readonly ChatManager chat;

    public ChatRealtimeHub(IOptions<ChatOptions> options, AuthenticationManager authentication, ChatManager chat)
    {
        this.options = options.Value;
        this.authentication = authentication;
        this.chat = chat;
    }

    public override Task OnConnectedAsync()
    {
        string? token = ReadAccessToken();
        TokenValidationResult validation = !BearerTokenSyntax.IsValid(token)
            ? TokenValidationResult.Invalid("missing_bearer_token")
            : authentication.ValidateToken(token!);
        if (!validation.IsValid)
        {
            Context.Abort();
            return Task.CompletedTask;
        }

        if (!options.Enabled || !options.RealtimeEnabled)
        {
            Context.Abort();
            return Task.CompletedTask;
        }

        Context.Items[nameof(PlayerId)] = validation.PlayerId;
        return base.OnConnectedAsync();
    }

    public Task JoinConversation(string conversationId)
    {
        EnsureRealtimeEnabled();
        if (!Guid.TryParse(conversationId, out Guid parsedConversationId))
        {
            throw new HubException("conversation_id_invalid");
        }

        chat.EnsureCanRead(GetPlayerId(), parsedConversationId);
        return Groups.AddToGroupAsync(Context.ConnectionId, ChatRealtimeGroups.Conversation(parsedConversationId));
    }

    public Task LeaveConversation(string conversationId)
    {
        EnsureRealtimeEnabled();
        if (!Guid.TryParse(conversationId, out Guid parsedConversationId))
        {
            throw new HubException("conversation_id_invalid");
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatRealtimeGroups.Conversation(parsedConversationId));
    }

    private void EnsureRealtimeEnabled()
    {
        if (!options.Enabled || !options.RealtimeEnabled)
        {
            throw new HubException("chat_realtime_disabled");
        }
    }

    private PlayerId GetPlayerId()
    {
        if (Context.Items.TryGetValue(nameof(PlayerId), out object? value) && value is PlayerId playerId)
        {
            return playerId;
        }

        throw new HubException("chat.session_required");
    }

    private string? ReadAccessToken()
    {
        HttpContext? httpContext = Context.GetHttpContext();
        string authorization = httpContext?.Request.Headers.Authorization.FirstOrDefault() ?? string.Empty;
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization["Bearer ".Length..];
        }

        return httpContext?.Request.Query["access_token"].FirstOrDefault();
    }
}
