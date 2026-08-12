using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Realtime;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

public sealed class SignalRChatRealtimeDispatcherTests
{
    [Test]
    public async Task PublishAsyncSendsEnvelopeToConversationGroupWhenRealtimeEnabled()
    {
        RecordingHubContext hubContext = new();
        SignalRChatRealtimeDispatcher dispatcher = new(hubContext, Options.Create(new ChatOptions
        {
            Enabled = true,
            RealtimeEnabled = true
        }));
        Guid conversationId = Guid.NewGuid();
        ChatEventEnvelope envelope = new(
            "evt_test",
            "message.created",
            DateTimeOffset.UtcNow,
            conversationId,
            1,
            new PlayerId(Guid.NewGuid()),
            new { body = "hello" },
            "server",
            1);

        await dispatcher.PublishAsync(envelope);

        Assert.Multiple(() =>
        {
            Assert.That(hubContext.Clients.LastGroupName, Is.EqualTo(ChatRealtimeGroups.Conversation(conversationId)));
            Assert.That(hubContext.Clients.Proxy.LastMethod, Is.EqualTo(SignalRChatRealtimeDispatcher.EventMethodName));
            Assert.That(hubContext.Clients.Proxy.LastArguments, Has.Length.EqualTo(1));
            Assert.That(hubContext.Clients.Proxy.LastArguments![0], Is.SameAs(envelope));
        });
    }

    [Test]
    public async Task PublishAsyncDoesNothingWhenRealtimeDisabled()
    {
        RecordingHubContext hubContext = new();
        SignalRChatRealtimeDispatcher dispatcher = new(hubContext, Options.Create(new ChatOptions
        {
            Enabled = true,
            RealtimeEnabled = false
        }));

        await dispatcher.PublishAsync(new ChatEventEnvelope(
            "evt_test",
            "message.created",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            1,
            null,
            new { body = "hello" },
            "server",
            1));

        Assert.That(hubContext.Clients.Proxy.LastMethod, Is.Null);
    }

    private sealed class RecordingHubContext : IHubContext<ChatRealtimeHub>
    {
        public RecordingHubContext()
        {
            Clients = new RecordingHubClients();
            Groups = new RecordingGroupManager();
        }

        public RecordingHubClients Clients { get; }
        IHubClients IHubContext<ChatRealtimeHub>.Clients => Clients;
        public IGroupManager Groups { get; }
    }

    private sealed class RecordingHubClients : IHubClients
    {
        public RecordingClientProxy Proxy { get; } = new();
        public string? LastGroupName { get; private set; }

        public IClientProxy All => Proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IClientProxy Client(string connectionId) => Proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;
        public IClientProxy Group(string groupName)
        {
            LastGroupName = groupName;
            return Proxy;
        }

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            LastGroupName = groupName;
            return Proxy;
        }

        public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;
        public IClientProxy User(string userId) => Proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
    }

    private sealed class RecordingClientProxy : IClientProxy
    {
        public string? LastMethod { get; private set; }
        public object?[]? LastArguments { get; private set; }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            LastMethod = method;
            LastArguments = args;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
