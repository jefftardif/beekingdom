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
public sealed class BKStudioIdempotencyChatRegressionTests
{
    [Test]
    public async Task Retry_with_same_client_request_id_does_not_duplicate_message()
    {
        var options = Options.Create(new ChatOptions { Enabled=true, RealtimeEnabled=false });
        var repository = new InMemoryChatRepository();
        var service = new ChatService(repository, new LocalChatAudienceResolver(options),
            new NoopChatRealtimeDispatcher(), new Clock(), options);
        PlayerId player = PlayerId.New();
        var conversation = service.CreateConversation(player,
            new CreateChatConversationRequest(ChatChannelType.Server, Guid.NewGuid(), Guid.NewGuid(), null,
                "Global", Array.Empty<Guid>(), "bkstudio-idem-create"));
        var request = new SendChatMessageRequest("bkstudio-idem-send",
            "Nectar \u00E9quitable \uD83D\uDC1D", null, null, null, null, DateTimeOffset.UtcNow);
        var first = await service.SendMessageAsync(player, conversation.Conversation.ConversationId, request);
        var second = await service.SendMessageAsync(player, conversation.Conversation.ConversationId, request);
        var messages = service.GetMessages(player, conversation.Conversation.ConversationId, 0, 10).Items.ToArray();
        Assert.That(second.Deduplicated, Is.True);
        Assert.That(second.Message.MessageId, Is.EqualTo(first.Message.MessageId));
        Assert.That(messages.Length, Is.EqualTo(1));
    }
    private sealed class Clock : IServerClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
