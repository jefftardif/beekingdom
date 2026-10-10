using System;
using System.Linq;
using System.Threading.Tasks;
using BeeKingdom.Chat;
using BeeKingdom.Chat.Audience;
using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Chat.Realtime;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace BeeKingdom.Tests;
public sealed class BKStudioChatRetryRegressionTests
{
    [Test]
    public async Task Repeated_send_with_same_request_id_does_not_duplicate_messages()
    {
        var options = Options.Create(new ChatOptions { Enabled=true, RealtimeEnabled=false });
        var repository = new InMemoryChatRepository();
        var service = new ChatService(repository, new LocalChatAudienceResolver(options), new NoopChatRealtimeDispatcher(), new Clock(), options);
        PlayerId player = PlayerId.New();
        var conversation = service.CreateConversation(player, new(ChatChannelType.Server, Guid.NewGuid(), Guid.NewGuid(), null, "Global", Array.Empty<Guid>(), "unicode-create"));
        string original = "Allô, reine! À bientôt 🍯";
        var retryRequest = new SendChatMessageRequest(
            "retry-request", original, null, null, null, null, DateTimeOffset.UtcNow);
        var first = await service.SendMessageAsync(player, conversation.Conversation.ConversationId, retryRequest);
        var repeat = await service.SendMessageAsync(player, conversation.Conversation.ConversationId, retryRequest);
        var messages = service.GetMessages(player, conversation.Conversation.ConversationId, 0, 10).Items;
        Assert.That(repeat.Deduplicated, Is.True);
        Assert.That(repeat.Message.MessageId, Is.EqualTo(first.Message.MessageId));
        Assert.That(messages, Has.Count.EqualTo(1));

    }
    private sealed class Clock : IServerClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
}
