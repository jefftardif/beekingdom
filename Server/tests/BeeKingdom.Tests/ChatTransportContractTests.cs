using BeeKingdom.Chat;
using BeeKingdom.Chat.Audience;
using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Realtime;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using BeeKingdom.Shared.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeeKingdom.Tests;

public sealed class ChatTransportContractTests
{
    [Test]
    public async Task Authenticated_sender_receipt_is_derived_from_session_player()
    {
        Fixture f = new();
        CreateChatConversationResult created = f.Service.CreateConversation(f.Player, new(ChatChannelType.Server, Guid.NewGuid(), Guid.NewGuid(), null, "Global", [], "sender-bound"));
        SendChatMessageResult sent = await f.Service.SendMessageAsync(f.Player, created.Conversation.ConversationId, new("sender-bound-request", "body", null, null, null, null, DateTimeOffset.UtcNow));
        Assert.That(sent.Message.SenderPlayerId, Is.EqualTo(f.Player));
        Assert.That(f.Service.GetMessages(f.Player, created.Conversation.ConversationId, 0, 10).Items.Single().SenderPlayerId, Is.EqualTo(f.Player));
    }

    [Test]
    public async Task Idempotency_receipt_cannot_cross_player_boundary()
    {
        Fixture f = new();
        CreateChatConversationResult created = f.Service.CreateConversation(f.Player, new(ChatChannelType.Server, Guid.NewGuid(), Guid.NewGuid(), null, "Global", [], "partition-bound"));
        SendChatMessageResult sent = await f.Service.SendMessageAsync(f.Player, created.Conversation.ConversationId, new("same-request", "A", null, null, null, null, DateTimeOffset.UtcNow));
        PlayerId other = PlayerId.New();
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.SendMessageAsync(other, created.Conversation.ConversationId, new("same-request", "A", null, null, null, null, DateTimeOffset.UtcNow)));
        Assert.That(sent.Message.SenderPlayerId, Is.EqualTo(f.Player));
    }
    [Test]
    public void Conversation_creation_replays_same_request_and_rejects_changed_payload()
    {
        Fixture f=new(); Guid other=Guid.NewGuid();
        CreateChatConversationRequest request=new(ChatChannelType.Private,Guid.NewGuid(),Guid.NewGuid(),null,"A",[other],"create-1");
        CreateChatConversationResult first=f.Service.CreateConversation(f.Player,request); CreateChatConversationResult retry=f.Service.CreateConversation(f.Player,request);
        Assert.That(retry.Conversation.ConversationId,Is.EqualTo(first.Conversation.ConversationId));
        Assert.Throws<InvalidOperationException>(()=>f.Service.CreateConversation(f.Player,request with{Title="B"}));
    }

    [Test]
    public void Conversation_creation_canonicalizes_participant_order_duplicates_and_trim()
    {
        Fixture f=new(); Guid a=Guid.NewGuid(),b=Guid.NewGuid(); Guid server=Guid.NewGuid(),world=Guid.NewGuid();
        CreateChatConversationRequest first=new(ChatChannelType.Private,server,world,null,"  Team  ",[b,a,a],"canonical");
        CreateChatConversationResult created=f.Service.CreateConversation(f.Player,first);
        CreateChatConversationResult replay=f.Service.CreateConversation(f.Player,first with{Title="Team",ParticipantIds=[a,b]});
        Assert.That(replay.Conversation.ConversationId,Is.EqualTo(created.Conversation.ConversationId));
    }

    [Test]
    public async Task Sequences_are_monotone_and_after_sequence_fills_reconnection_gap()
    {
        Fixture f=new(); CreateChatConversationResult created=f.Service.CreateConversation(f.Player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Global",[],"create"));
        await f.Send(created.Conversation.ConversationId,"one","send-1"); await f.Send(created.Conversation.ConversationId,"two","send-2"); await f.Send(created.Conversation.ConversationId,"three","send-3");
        ChatMessagePage gap=f.Service.GetMessages(f.Player,created.Conversation.ConversationId,1,50);
        Assert.Multiple(()=>{Assert.That(gap.Items.Select(x=>x.Sequence),Is.EqualTo(new long[]{2,3}));Assert.That(f.Service.GetLastSequence(created.Conversation.ConversationId),Is.EqualTo(3));});
        Assert.Throws<ArgumentException>(()=>f.Service.GetMessages(f.Player,created.Conversation.ConversationId,-1,50));
    }

    [Test]
    public void Camel_case_wire_shape_preserves_published_message_fields()
    {
        DateTimeOffset now=DateTimeOffset.UtcNow; PlayerId sender=PlayerId.New(); ChatMessage message=new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),ChatChannelType.Server,sender,"sender","original",[],[],[],null,now,now,7,"client",ChatMessageState.Accepted,ChatModerationStatus.Clear,null,null,null,1);
        JsonSerializerOptions wireOptions = BeeJson.CreateDefaultOptions(); wireOptions.Converters.Add(new JsonStringEnumConverter());
        string json=JsonSerializer.Serialize(new ChatWireMessagePage([ChatTransportMapper.Message(message)],7),wireOptions); using JsonDocument doc=JsonDocument.Parse(json); JsonElement item=doc.RootElement.GetProperty("items")[0];
        Assert.Multiple(()=>{Assert.That(item.GetProperty("messageId").GetGuid(),Is.EqualTo(message.MessageId));Assert.That(item.GetProperty("conversationId").GetGuid(),Is.EqualTo(message.ConversationId));Assert.That(item.GetProperty("body").GetString(),Is.EqualTo("original"));Assert.That(item.GetProperty("senderPlayerId").GetString(),Is.EqualTo(sender.Value.ToString()));Assert.That(item.GetProperty("senderDisplayName").GetString(),Is.EqualTo("sender"));Assert.That(item.GetProperty("channelType").GetString(),Is.EqualTo("Server"));Assert.That(item.TryGetProperty("acceptedAtUtc",out _),Is.True);Assert.That(item.GetProperty("sequence").GetInt64(),Is.EqualTo(7));Assert.That(item.GetProperty("clientRequestId").GetString(),Is.EqualTo("client"));Assert.That(doc.RootElement.GetProperty("nextAfterSequence").GetInt64(),Is.EqualTo(7));});
    }

    [Test]
    public void Conversation_wire_shape_contains_channel_and_player_scoped_inbox_fields()
    {
        Fixture f = new();
        CreateChatConversationResult created = f.Service.CreateConversation(f.Player, new(ChatChannelType.Server, Guid.NewGuid(), Guid.NewGuid(), null, "Global", [], "contract-conversation"));
        ChatTransportConversationDto dto = ChatTransportMapper.Conversation(created.Conversation, f.Service.GetLastSequence(created.Conversation.ConversationId), created.Inbox);
        JsonSerializerOptions wireOptions = BeeJson.CreateDefaultOptions(); wireOptions.Converters.Add(new JsonStringEnumConverter());
        using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(new ChatTransportConversationPage([dto], null), wireOptions));
        JsonElement item = json.RootElement.GetProperty("items")[0];
        Assert.Multiple(() =>
        {
            Assert.That(item.GetProperty("conversationId").GetGuid(), Is.EqualTo(created.Conversation.ConversationId));
            Assert.That(item.GetProperty("title").GetString(), Is.EqualTo("Global"));
            Assert.That(item.GetProperty("channelType").GetString(), Is.EqualTo("Server"));
            Assert.That(item.GetProperty("lastSequence").GetInt64(), Is.EqualTo(0));
            Assert.That(item.GetProperty("readCursorSequence").GetInt64(), Is.EqualTo(0));
            Assert.That(item.GetProperty("unreadCount").GetInt32(), Is.EqualTo(0));
            Assert.That(item.GetProperty("mentionCount").GetInt32(), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Repeated_pages_are_idempotent_and_bounded_pages_never_skip()
    {
        Fixture f=new();CreateChatConversationResult c=f.Service.CreateConversation(f.Player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Global",[],"create"));
        for(int i=1;i<=5;i++)await f.Send(c.Conversation.ConversationId,$"m{i}",$"s{i}");
        ChatMessagePage first=f.Service.GetMessages(f.Player,c.Conversation.ConversationId,0,2);ChatMessagePage repeated=f.Service.GetMessages(f.Player,c.Conversation.ConversationId,0,2);ChatMessagePage second=f.Service.GetMessages(f.Player,c.Conversation.ConversationId,first.NextAfterSequence!.Value,2);ChatMessagePage third=f.Service.GetMessages(f.Player,c.Conversation.ConversationId,second.NextAfterSequence!.Value,2);
        Assert.Multiple(()=>{Assert.That(repeated.Items.Select(x=>x.MessageId),Is.EqualTo(first.Items.Select(x=>x.MessageId)));Assert.That(first.Items.Select(x=>x.Sequence),Is.EqualTo(new long[]{1,2}));Assert.That(second.Items.Select(x=>x.Sequence),Is.EqualTo(new long[]{3,4}));Assert.That(third.Items.Select(x=>x.Sequence),Is.EqualTo(new long[]{5}));Assert.That(third.NextAfterSequence,Is.Null);});
    }

    [Test]
    public async Task ValidatedInboundPagesAreUniqueOrderedScopedAndCursorSafe()
    {
        ChatOptions options=new(){Enabled=true,BodyMaxCharacters=32};InMemoryChatRepository repository=new();PlayerId player=PlayerId.New();ChatService service=CreateService(repository,options);
        CreateChatConversationResult first=service.CreateConversation(player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"one",[],"inbound-one"));
        CreateChatConversationResult second=service.CreateConversation(player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"two",[],"inbound-two"));
        await service.SendMessageAsync(player,first.Conversation.ConversationId,new("in-1","a",null,null,null,null,DateTimeOffset.UtcNow));
        await service.SendMessageAsync(player,first.Conversation.ConversationId,new("in-2","b",null,null,null,null,DateTimeOffset.UtcNow));
        ChatMessagePage page=service.GetMessages(player,first.Conversation.ConversationId,0,100);
        ChatConversationPage conversations=service.ListConversations(player,100);
        Assert.Multiple(()=>
        {
            Assert.That(page.Items.Select(x=>x.MessageId).Distinct().Count(),Is.EqualTo(page.Items.Count));
            Assert.That(page.Items.Select(x=>x.ConversationId),Is.All.EqualTo(first.Conversation.ConversationId));
            Assert.That(page.Items.Select(x=>x.Sequence),Is.Ordered.Ascending);
            Assert.That(page.Items.Select(x=>x.Sequence),Is.All.GreaterThan(0));
            Assert.That(page.NextAfterSequence,Is.Null.Or.GreaterThanOrEqualTo(page.Items.Max(x=>x.Sequence)));
            Assert.That(page.Items.Select(x=>x.Body.Length),Is.All.LessThanOrEqualTo(32));
            Assert.That(conversations.Items.Select(x=>x.ConversationId).Distinct().Count(),Is.EqualTo(conversations.Items.Count));
        });
        Assert.Throws<UnauthorizedAccessException>(()=>service.GetMessages(PlayerId.New(),first.Conversation.ConversationId,0,100));
        Assert.That(service.GetMessages(player,second.Conversation.ConversationId,0,100).Items,Is.Empty);
    }

    [Test]
    public async Task Message_rate_limit_is_distinct_and_idempotent_retry_is_free()
    {
        Fixture f=new(messagesPerMinute:1);CreateChatConversationResult c=f.Service.CreateConversation(f.Player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Global",[],"create"));
        SendChatMessageRequest request=new("same","one",null,null,null,null,DateTimeOffset.UtcNow);SendChatMessageResult first=await f.Service.SendMessageAsync(f.Player,c.Conversation.ConversationId,request);SendChatMessageResult replay=await f.Service.SendMessageAsync(f.Player,c.Conversation.ConversationId,request);InvalidOperationException? limited=Assert.ThrowsAsync<InvalidOperationException>(()=>f.Send(c.Conversation.ConversationId,"two","other"));
        Assert.Multiple(()=>{Assert.That(replay.Deduplicated,Is.True);Assert.That(replay.Message.MessageId,Is.EqualTo(first.Message.MessageId));Assert.That(limited!.Message,Is.EqualTo("chat_rate_limited"));});
    }

    [Test]
    public async Task Receipts_survive_service_reconstruction_and_changed_payload_is_final_conflict()
    {
        ChatOptions options=new(){Enabled=true,RealtimeEnabled=false}; InMemoryChatRepository repository=new(); PlayerId player=PlayerId.New();
        ChatService first=CreateService(repository,options); CreateChatConversationRequest create=new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Global",[],"create-durable");
        CreateChatConversationResult conversation=first.CreateConversation(player,create); SendChatMessageRequest send=new("send-durable","body",null,null,null,null,DateTimeOffset.UtcNow); SendChatMessageResult accepted=await first.SendMessageAsync(player,conversation.Conversation.ConversationId,send);
        ChatService reconstructed=CreateService(repository,options); CreateChatConversationResult createReplay=reconstructed.CreateConversation(player,create); SendChatMessageResult sendReplay=await reconstructed.SendMessageAsync(player,conversation.Conversation.ConversationId,send);
        Assert.Multiple(()=>{Assert.That(createReplay.Conversation.ConversationId,Is.EqualTo(conversation.Conversation.ConversationId));Assert.That(sendReplay.Message.MessageId,Is.EqualTo(accepted.Message.MessageId));Assert.That(sendReplay.Deduplicated,Is.True);});
        Assert.Throws<InvalidOperationException>(()=>reconstructed.CreateConversation(player,create with{Title="changed"}));
        Assert.ThrowsAsync<InvalidOperationException>(()=>reconstructed.SendMessageAsync(player,conversation.Conversation.ConversationId,send with{Body="changed"}));
    }

    [Test]
    public async Task Realtime_event_is_published_only_after_identical_message_is_rest_readable()
    {
        ChatOptions options=new(){Enabled=true,RealtimeEnabled=true}; InMemoryChatRepository repository=new(); PlayerId player=PlayerId.New(); RecordingDispatcher dispatcher=new(repository);
        ChatService service=new(repository,new LocalChatAudienceResolver(Options.Create(options)),dispatcher,new Clock(),Options.Create(options));
        CreateChatConversationResult c=service.CreateConversation(player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Global",[],"create"));
        SendChatMessageResult sent=await service.SendMessageAsync(player,c.Conversation.ConversationId,new("send","body",null,null,null,null,DateTimeOffset.UtcNow));
        ChatMessage rest=service.GetMessages(player,c.Conversation.ConversationId,0,50).Items.Single(); ChatWireMessageDto realtime=(ChatWireMessageDto)dispatcher.Envelope!.Payload;
        Assert.Multiple(()=>{Assert.That(dispatcher.WasReadableAtPublish,Is.True);Assert.That(realtime.MessageId,Is.EqualTo(rest.MessageId));Assert.That(realtime.ConversationId,Is.EqualTo(rest.ConversationId));Assert.That(realtime.Sequence,Is.EqualTo(rest.Sequence));Assert.That(realtime.ClientRequestId,Is.EqualTo(rest.ClientRequestId));Assert.That(realtime.Body,Is.EqualTo(rest.Body));Assert.That(realtime.AcceptedAtUtc,Is.EqualTo(rest.AcceptedAtUtc));Assert.That(realtime.SenderPlayerId,Is.EqualTo(rest.SenderPlayerId.Value));Assert.That(sent.Message.MessageId,Is.EqualTo(rest.MessageId));});
    }

    [Test]
    public void Realtime_payload_serializes_as_flat_wire_shape_not_the_domain_record()
    {
        DateTimeOffset now=DateTimeOffset.UtcNow; PlayerId sender=PlayerId.New();
        ChatMessage message=new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),ChatChannelType.Server,sender,"sender","body",[],[],[],null,now,now,3,"client",ChatMessageState.Accepted,ChatModerationStatus.Clear,null,null,null,1);
        ChatEventEnvelope envelope=new("evt_1","message.created",now,message.ConversationId,message.Sequence,sender,ChatTransportMapper.Message(message),"server",1);
        Assert.That(envelope.Payload, Is.InstanceOf<ChatWireMessageDto>());
        ChatWireMessageDto payload=(ChatWireMessageDto)envelope.Payload;
        Assert.Multiple(()=>{Assert.That(payload.SenderPlayerId,Is.EqualTo(sender.Value));Assert.That(payload.MessageId,Is.EqualTo(message.MessageId));});
    }

    [Test]
    public async Task Moderation_report_is_idempotent_after_reconstruction_and_conflicts_on_changed_payload()
    {
        ChatOptions options=new(){Enabled=true}; InMemoryChatRepository repository=new(); PlayerId player=PlayerId.New(); ChatService first=CreateService(repository,options);
        CreateChatConversationResult c=first.CreateConversation(player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Global",[],"create-report"));
        SendChatMessageResult sent=await first.SendMessageAsync(player,c.Conversation.ConversationId,new("send-report","body never copied to receipt",null,null,null,null,DateTimeOffset.UtcNow));
        ReportChatMessageRequest request=new("report-stable"," spam "); ChatModerationReport report=first.ReportMessage(player,sent.Message.MessageId,request);
        ChatService reconstructed=CreateService(repository,options); ChatModerationReport replay=reconstructed.ReportMessage(player,sent.Message.MessageId,request with{Category="spam"});
        Assert.Multiple(()=>{Assert.That(replay.ReportId,Is.EqualTo(report.ReportId));Assert.That(repository.GetModerationReportReceipt(player,"report-stable")!.PayloadHash,Does.Not.Contain("body"));});
        Assert.Throws<InvalidOperationException>(()=>reconstructed.ReportMessage(player,sent.Message.MessageId,request with{Category="abuse"}));
    }

    [Test]
    public void Read_cursor_is_monotone_for_stale_retry()
    {
        Fixture f=new(); CreateChatConversationResult c=f.Service.CreateConversation(f.Player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Global",[],"read-create"));
        ChatInboxEntry ten=f.Service.MarkRead(f.Player,c.Conversation.ConversationId,10); ChatInboxEntry four=f.Service.MarkRead(f.Player,c.Conversation.ConversationId,4);
        Assert.Multiple(()=>{Assert.That(ten.ReadCursorSequence,Is.EqualTo(0));Assert.That(four.ReadCursorSequence,Is.EqualTo(0));Assert.That(four.UnreadCount,Is.EqualTo(0));Assert.That(four.MentionCount,Is.EqualTo(0));});
    }

    [Test]
    public async Task Read_replay_is_ordered_without_skip_or_duplicate_and_cursor_is_server_bounded()
    {
        Fixture f=new(); CreateChatConversationResult c=f.Service.CreateConversation(f.Player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Replay",[] ,"replay-create"));
        for(int i=1;i<=3;i++) await f.Service.SendMessageAsync(f.Player,c.Conversation.ConversationId,new($"replay-{i}",$"body-{i}",null,null,null,null,DateTimeOffset.UtcNow));
        ChatMessagePage old=f.Service.GetMessages(f.Player,c.Conversation.ConversationId,0,2);
        ChatMessagePage repeat=f.Service.GetMessages(f.Player,c.Conversation.ConversationId,0,2);
        ChatMessagePage tail=f.Service.GetMessages(f.Player,c.Conversation.ConversationId,old.NextAfterSequence!.Value,2);
        ChatInboxEntry read=f.Service.MarkRead(f.Player,c.Conversation.ConversationId,9999);
        Assert.Multiple(()=>{
            Assert.That(old.Items.Select(x=>x.Sequence),Is.EqualTo(new long[]{1,2}));
            Assert.That(repeat.Items.Select(x=>x.MessageId),Is.EqualTo(old.Items.Select(x=>x.MessageId)));
            Assert.That(tail.Items.Select(x=>x.Sequence),Is.EqualTo(new long[]{3}));
            Assert.That(read.ReadCursorSequence,Is.EqualTo(3));
            Assert.That(read.UnreadCount,Is.EqualTo(0));
            Assert.That(read.MentionCount,Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Replay_selected_conversation_beyond_first_page_remains_player_scoped()
    {
        Fixture f=new();
        for(int i=0;i<3;i++) f.Service.CreateConversation(f.Player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,$"C{i}",[],$"page-{i}"));
        ChatConversationPage first=f.Service.ListConversations(f.Player,1);
        ChatConversationPage second=f.Service.ListConversations(f.Player,1,first.NextCursor);
        ChatConversation target=second.Items.Single();
        await f.Service.SendMessageAsync(f.Player,target.ConversationId,new("selected-send","selected-body",null,null,null,null,DateTimeOffset.UtcNow));
        ChatMessagePage page=f.Service.GetMessages(f.Player,target.ConversationId,0,100);
        Assert.Multiple(()=>{Assert.That(page.Items,Has.Count.EqualTo(1));Assert.That(page.Items[0].ConversationId,Is.EqualTo(target.ConversationId));Assert.That(page.Items[0].Body,Is.EqualTo("selected-body"));});
        Assert.Throws<UnauthorizedAccessException>(()=>f.Service.GetMessages(PlayerId.New(),target.ConversationId,0,100));
    }

    [Test]
    public void Conversation_cursors_are_opaque_repeatable_progressive_and_player_scoped()
    {
        Fixture f=new();
        for(int i=0;i<3;i++)f.Service.CreateConversation(f.Player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,$"C{i}",[],$"cursor-{i}"));
        ChatConversationPage first=f.Service.ListConversations(f.Player,1); ChatConversationPage repeated=f.Service.ListConversations(f.Player,1); ChatConversationPage second=f.Service.ListConversations(f.Player,1,first.NextCursor);
        Assert.Multiple(()=>{Assert.That(first.Items.Count,Is.EqualTo(1));Assert.That(first.NextCursor,Is.Not.Null.And.Not.Empty);Assert.That(repeated.Items[0].ConversationId,Is.EqualTo(first.Items[0].ConversationId));Assert.That(repeated.NextCursor,Is.EqualTo(first.NextCursor));Assert.That(second.Items[0].ConversationId,Is.Not.EqualTo(first.Items[0].ConversationId));Assert.That(second.NextCursor,Is.Not.EqualTo(first.NextCursor));});
        Assert.Throws<ArgumentException>(()=>f.Service.ListConversations(f.Player,1,first.NextCursor+"x"));
        Assert.Throws<ArgumentException>(()=>f.Service.ListConversations(PlayerId.New(),1,first.NextCursor));
        Assert.Throws<ArgumentException>(()=>f.Service.ListConversations(f.Player,1,new string('a',1025)));
    }

    [Test]
    public async Task Capabilities_and_authoritative_validators_share_the_same_limits()
    {
        ChatOptions options=new(){Enabled=true,BodyMaxCharacters=5,MaxPrivateRecipients=2,MessagesPerMinutePerPlayer=7,MessagesPerTenSecondsPerConversation=3};InMemoryChatRepository repository=new();PlayerId player=PlayerId.New();ChatService service=CreateService(repository,options);ChatCapabilities capabilities=service.GetCapabilities();
        Assert.Multiple(()=>{Assert.That(capabilities.Provider,Is.EqualTo("server"));Assert.That(capabilities.ProtocolVersion,Is.EqualTo("chat-v1"));Assert.That(capabilities.Limits.BodyMaxCharacters,Is.EqualTo(5));Assert.That(capabilities.Limits.MaxPrivateRecipients,Is.EqualTo(2));Assert.That(capabilities.Limits.MessagesPerMinutePerPlayer,Is.EqualTo(7));Assert.That(capabilities.ReadCursors,Is.True);Assert.That(capabilities.ModerationReports,Is.True);Assert.That(capabilities.Channels,Is.EqualTo(new[]{ChatChannelType.Alliance,ChatChannelType.Server,ChatChannelType.Private,ChatChannelType.Leaders}));Assert.That(capabilities.Channels.Distinct().Count(),Is.EqualTo(capabilities.Channels.Count));Assert.That(capabilities.IdempotencyReceiptRetentionDays,Is.EqualTo(30));});
        Assert.Throws<ArgumentException>(()=>service.CreateConversation(player,new(ChatChannelType.Private,Guid.NewGuid(),Guid.NewGuid(),null,null,[Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()],"too-many")));
        CreateChatConversationResult conversation=service.CreateConversation(player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Global",[],"body-limit"));
        Assert.ThrowsAsync<ArgumentException>(()=>service.SendMessageAsync(player,conversation.Conversation.ConversationId,new("oversize","123456",null,null,null,null,DateTimeOffset.UtcNow)));
    }

    [Test]
    public async Task Concurrent_identical_moderation_retries_create_one_report()
    {
        ChatOptions options=new(){Enabled=true};InMemoryChatRepository repository=new();PlayerId player=PlayerId.New();ChatService service=CreateService(repository,options);CreateChatConversationResult c=service.CreateConversation(player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Global",[],"concurrent-report"));SendChatMessageResult sent=await service.SendMessageAsync(player,c.Conversation.ConversationId,new("send","body",null,null,null,null,DateTimeOffset.UtcNow));ReportChatMessageRequest request=new("same-report","spam");
        ChatModerationReport[] reports=await Task.WhenAll(Task.Run(()=>service.ReportMessage(player,sent.Message.MessageId,request)),Task.Run(()=>service.ReportMessage(player,sent.Message.MessageId,request)));
        Assert.That(reports.Select(x=>x.ReportId).Distinct().Count(),Is.EqualTo(1));
    }

    [Test]
    public async Task CancellationBeforeCommitHasNoEffectAndDisconnectAfterCommitReplaysReceipt()
    {
        ChatOptions options=new(){Enabled=true,RealtimeEnabled=true};InMemoryChatRepository repository=new();PlayerId player=PlayerId.New();BlockingDispatcher dispatcher=new();ChatService service=new(repository,new LocalChatAudienceResolver(Options.Create(options)),dispatcher,new Clock(),Options.Create(options));
        CreateChatConversationResult conversation=service.CreateConversation(player,new(ChatChannelType.Server,Guid.NewGuid(),Guid.NewGuid(),null,"Global",[],"timeout-create"));
        SendChatMessageRequest preCommit=new("cancel-before","never committed",null,null,null,null,DateTimeOffset.UtcNow);using CancellationTokenSource alreadyCancelled=new();alreadyCancelled.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(()=>service.SendMessageAsync(player,conversation.Conversation.ConversationId,preCommit,alreadyCancelled.Token));
        Assert.That(service.GetMessages(player,conversation.Conversation.ConversationId,0,50).Items,Is.Empty);

        SendChatMessageRequest request=new("commit-before-disconnect","committed once",null,null,null,null,DateTimeOffset.UtcNow);using CancellationTokenSource disconnected=new();Task<SendChatMessageResult> first=service.SendMessageAsync(player,conversation.Conversation.ConversationId,request,disconnected.Token);
        await dispatcher.PublishEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));disconnected.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async()=>await first);
        SendChatMessageResult replay=await service.SendMessageAsync(player,conversation.Conversation.ConversationId,request);
        Assert.Multiple(()=>{Assert.That(replay.Deduplicated,Is.True);Assert.That(service.GetMessages(player,conversation.Conversation.ConversationId,0,50).Items.Count,Is.EqualTo(1));Assert.That(repository.GetOutboxReceipt(player,conversation.Conversation.ConversationId,request.ClientRequestId)?.MessageId,Is.EqualTo(replay.Message.MessageId));});
    }

    private static ChatService CreateService(InMemoryChatRepository repository,ChatOptions options)=>new(repository,new LocalChatAudienceResolver(Options.Create(options)),new NoopChatRealtimeDispatcher(),new Clock(),Options.Create(options));
    private sealed class RecordingDispatcher(InMemoryChatRepository repository):IChatRealtimeDispatcher
    {
        public ChatEventEnvelope? Envelope; public bool WasReadableAtPublish;
        public Task PublishAsync(ChatEventEnvelope envelope,CancellationToken cancellationToken=default){Envelope=envelope;WasReadableAtPublish=repository.GetMessage(((ChatWireMessageDto)envelope.Payload).MessageId)!=null;return Task.CompletedTask;}
    }
    private sealed class BlockingDispatcher:IChatRealtimeDispatcher
    {
        private int calls;
        public TaskCompletionSource PublishEntered{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task PublishAsync(ChatEventEnvelope envelope,CancellationToken cancellationToken=default)
        {
            if(Interlocked.Increment(ref calls)==1){PublishEntered.TrySetResult();await Task.Delay(Timeout.InfiniteTimeSpan,cancellationToken);}
        }
    }

    private sealed class Fixture
    {
        public PlayerId Player=PlayerId.New(); public ChatService Service{get;}
        public Fixture(int messagesPerMinute=50){ChatOptions o=new(){Enabled=true,RealtimeEnabled=false,MessagesPerMinutePerPlayer=messagesPerMinute};InMemoryChatRepository r=new();Service=new(r,new LocalChatAudienceResolver(Options.Create(o)),new NoopChatRealtimeDispatcher(),new Clock(),Options.Create(o));}
        public Task<SendChatMessageResult> Send(Guid conversation,string body,string id)=>Service.SendMessageAsync(Player,conversation,new(id,body,null,null,null,null,DateTimeOffset.UtcNow));
    }
    private sealed class Clock:IServerClock{public DateTimeOffset UtcNow=>DateTimeOffset.UtcNow;}
}
