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
public sealed class BKStudioUnicodeChatRegressionTests
{
    [Test]
    public async Task French_message_roundtrips()
    {
        var options = Options.Create(new ChatOptions { Enabled=true, RealtimeEnabled=false });
        var repository = new InMemoryChatRepository();
        var service = new ChatService(repository, new LocalChatAudienceResolver(options), new NoopChatRealtimeDispatcher(), new Clock(), options);
        PlayerId player = PlayerId.New();
        var conversation = service.CreateConversation(player, new(ChatChannelType.Server, Guid.NewGuid(), Guid.NewGuid(), null, "Global", Array.Empty<Guid>(), "unicode-create"));
        string original = "Allô, reine! À bientôt 🍯";
        var sent = await service.SendMessageAsync(player, conversation.Conversation.ConversationId, new("unicode-send", original, null, null, null, null, DateTimeOffset.UtcNow));
        var received = service.GetMessages(player, conversation.Conversation.ConversationId, 0, 10).Items.Single();
        Assert.That(received.Body, Is.EqualTo(original));
        Assert.That(received.MessageId, Is.EqualTo(sent.Message.MessageId));

    }
    private sealed class Clock : IServerClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
}
