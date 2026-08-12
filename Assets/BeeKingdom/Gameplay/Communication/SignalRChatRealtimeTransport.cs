using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace BeeKingdom.Gameplay.Communication
{
    // Real production transport for /chat/v1/realtime, replacing the pure REST+polling path.
    // The hub groups events by conversation (ChatRealtimeHub.JoinConversation/LeaveConversation),
    // so callers must join every conversation they want live "chat.event" updates for — see
    // ServerChatProvider.EnsureRealtimeSubscriptionsAsync, which drives Join/LeaveConversationAsync.
    public sealed class SignalRChatRealtimeTransport : IChatRealtimeTransport
    {
        private readonly string hubUrl;
        private HubConnection connection;
        private IDisposable eventSubscription;
        private Func<RemoteChatEvent, Task> onEvent;

        public SignalRChatRealtimeTransport(string chatBaseUrl)
        {
            // Compose (not NormalizeBaseUrl) so this works whether chatBaseUrl is a bare origin
            // or already includes the /chat/v1 suffix, matching how REST calls resolve their paths.
            hubUrl = ChatEndpointUrl.Compose(chatBaseUrl, "/chat/v1/realtime");
        }

        public bool IsAvailable => true;

        public async Task ConnectAsync(ChatSession session, Func<RemoteChatEvent, Task> onEvent, CancellationToken cancellationToken)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (onEvent == null) throw new ArgumentNullException(nameof(onEvent));
            await DisposeConnectionAsync();
            this.onEvent = onEvent;
            string accessToken = session.AccessToken;
            connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => { options.AccessTokenProvider = () => Task.FromResult(accessToken); })
                .WithAutomaticReconnect()
                .Build();
            eventSubscription = connection.On<WireChatEventEnvelope>("chat.event", HandleEnvelope);
            try
            {
                await connection.StartAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                await DisposeConnectionAsync();
                throw new RemoteChatTransportException(RemoteChatError.Transport, "Unable to establish the chat realtime connection.", 0, "realtime_connect_failed", innerException: exception);
            }
        }

        public async Task JoinConversationAsync(string conversationId, CancellationToken cancellationToken)
        {
            if (connection == null || connection.State != HubConnectionState.Connected) return;
            try { await connection.InvokeAsync("JoinConversation", conversationId, cancellationToken); }
            catch (Exception exception)
            {
                throw new RemoteChatTransportException(RemoteChatError.Transport, "Unable to join the chat conversation channel.", 0, "realtime_join_failed", innerException: exception);
            }
        }

        public async Task LeaveConversationAsync(string conversationId, CancellationToken cancellationToken)
        {
            if (connection == null || connection.State != HubConnectionState.Connected) return;
            try { await connection.InvokeAsync("LeaveConversation", conversationId, cancellationToken); }
            catch
            {
                // Best-effort: the connection may already be gone (reconnect, sign-out) — nothing to clean up.
            }
        }

        public Task DisconnectAsync(CancellationToken cancellationToken) => DisposeConnectionAsync();

        private async Task DisposeConnectionAsync()
        {
            IDisposable subscription = eventSubscription;
            HubConnection current = connection;
            eventSubscription = null;
            connection = null;
            subscription?.Dispose();
            if (current != null)
            {
                try { await current.DisposeAsync(); } catch { }
            }
        }

        private Task HandleEnvelope(WireChatEventEnvelope envelope)
        {
            Func<RemoteChatEvent, Task> handler = onEvent;
            if (handler == null || envelope?.Payload == null) return Task.CompletedTask;
            var message = new RemoteChatMessage
            {
                MessageId = envelope.Payload.MessageId.ToString(),
                ConversationId = envelope.Payload.ConversationId.ToString(),
                Sequence = envelope.Payload.Sequence,
                ClientRequestId = envelope.Payload.ClientRequestId,
                SenderId = envelope.Payload.SenderPlayerId.ToString(),
                SenderDisplayName = envelope.Payload.SenderDisplayName,
                ChannelType = envelope.Payload.ChannelType,
                OriginalBody = envelope.Payload.Body,
                CreatedAt = envelope.Payload.AcceptedAtUtc
            };
            var evt = new RemoteChatEvent
            {
                EventId = envelope.EventId,
                ConversationId = envelope.ConversationId.ToString(),
                Sequence = envelope.Sequence,
                Message = message
            };
            return handler(evt);
        }

        // Mirrors the server wire shapes exactly (BeeKingdom.Chat.Models.ChatEventEnvelope /
        // ChatTransportMapper.Message output) so SignalR's JsonHubProtocol can bind it directly —
        // no manual JSON parsing needed.
        private sealed class WireChatEventEnvelope
        {
            [JsonPropertyName("eventId")] public string EventId { get; set; }
            [JsonPropertyName("eventType")] public string EventType { get; set; }
            [JsonPropertyName("conversationId")] public Guid ConversationId { get; set; }
            [JsonPropertyName("sequence")] public long? Sequence { get; set; }
            [JsonPropertyName("payload")] public WireChatMessage Payload { get; set; }
        }

        private sealed class WireChatMessage
        {
            [JsonPropertyName("messageId")] public Guid MessageId { get; set; }
            [JsonPropertyName("conversationId")] public Guid ConversationId { get; set; }
            [JsonPropertyName("channelType")] public string ChannelType { get; set; }
            [JsonPropertyName("senderPlayerId")] public Guid SenderPlayerId { get; set; }
            [JsonPropertyName("senderDisplayName")] public string SenderDisplayName { get; set; }
            [JsonPropertyName("body")] public string Body { get; set; }
            [JsonPropertyName("acceptedAtUtc")] public DateTimeOffset AcceptedAtUtc { get; set; }
            [JsonPropertyName("sequence")] public long Sequence { get; set; }
            [JsonPropertyName("clientRequestId")] public string ClientRequestId { get; set; }
        }
    }
}
