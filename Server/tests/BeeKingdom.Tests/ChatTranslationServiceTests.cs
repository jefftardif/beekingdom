using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Chat.Translations;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using BeeKingdom.Shared.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace BeeKingdom.Tests;

public sealed class ChatTranslationServiceTests
{
    [Test]
    public async Task Translation_is_cached_by_message_locale_and_model()
    {
        Fixture f = new();
        ChatTranslationResult first = await f.Service.TranslateAsync(f.Reader, f.MessageId, f.Request(), default);
        ChatTranslationResult second = await f.Service.TranslateAsync(f.Reader, f.MessageId, f.Request(), default);
        Assert.Multiple(() => { Assert.That(first.TranslatedText, Is.EqualTo("translated:en-US")); Assert.That(second, Is.EqualTo(first)); Assert.That(f.ProviderCalls, Is.EqualTo(1)); });
    }

    [Test]
    public void Reader_authorization_is_required()
    {
        Fixture f = new();
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.TranslateAsync(PlayerId.New(), f.MessageId, f.Request(), default));
    }

    [Test]
    public async Task Translation_cache_is_authorized_before_read_for_other_player()
    {
        Fixture f = new();
        ChatTranslationResult cached = await f.Service.TranslateAsync(f.Reader, f.MessageId, f.Request(), default);
        Assert.That(cached.TranslatedText, Is.Not.Empty);
        // Le second appel est refusé avant lecture du cache, même si la traduction est déjà matérialisée.
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.TranslateAsync(PlayerId.New(), f.MessageId, f.Request(), default));
    }

    [Test]
    public void Original_body_above_limit_is_rejected_before_provider()
    {
        Fixture f = new(body: new string('x', 33), maxCharacters: 32);
        Assert.ThrowsAsync<ArgumentException>(() => f.Service.TranslateAsync(f.Reader, f.MessageId, f.Request(), default));
        Assert.That(f.ProviderCalls, Is.Zero);
    }

    [TestCase("e", false)]
    [TestCase("en-US", true)]
    [TestCase("zh-Hant-TW", true)]
    [TestCase("en--US", false)]
    [TestCase("en US", false)]
    public void Translation_locale_contract_is_bounded(string locale, bool valid)
    {
        Assert.That(ChatTranslationService.IsSimpleBcp47(locale), Is.EqualTo(valid));
    }

    [Test]
    public async Task Invalid_provider_text_is_not_cached_and_retry_can_succeed()
    {
        Fixture f = new(); int calls = 0;
        ChatOptions options = new() { Enabled=true, TranslationMaxCharacters=1000, TranslationsPerMinutePerPlayer=10, TranslationModelVersion="test-v1", TranslationSourceLocale="fr-CA" };
        InMemoryChatRepository chat = new(); DateTimeOffset now = new(2026,7,21,12,0,0,TimeSpan.Zero); Guid conversationId=Guid.NewGuid();
        chat.SaveConversation(new(conversationId,Guid.NewGuid(),Guid.NewGuid(),ChatChannelType.Private,"private",null,f.Reader,now,null,null,"standard",1),[new(conversationId,f.Reader,ChatPermissionRole.Member,now,null,true,true)]);
        chat.SaveMessage(new(f.MessageId,conversationId,Guid.NewGuid(),Guid.NewGuid(),ChatChannelType.Private,f.Reader,"reader","body",[],[],[],null,now,now,1,"request",ChatMessageState.Accepted,ChatModerationStatus.Clear,null,null,null,1));
        InMemoryChatTranslationRepository cache = new(); DelegateChatTranslationProvider provider = new((_,_) => Task.FromResult(++calls == 1 ? new string('x',16001) : "ok"));
        ChatTranslationService service = new(chat,cache,provider,new ChatTranslationRateLimiter(Options.Create(options)),new FixedClock(now),Options.Create(options),new ChatTranslationDiagnostics(NullLogger<ChatTranslationDiagnostics>.Instance));
        Assert.ThrowsAsync<InvalidOperationException>(()=>service.TranslateAsync(f.Reader,f.MessageId,f.Request(),default));
        Assert.That(cache.Get(f.MessageId,"en-US","test-v1"),Is.Null);
        Assert.That((await service.TranslateAsync(f.Reader,f.MessageId,f.Request(),default)).TranslatedText,Is.EqualTo("ok"));
    }

    [Test]
    public async Task Rate_limit_rejects_uncached_second_translation()
    {
        Fixture f = new(perMinute: 1);
        await f.Service.TranslateAsync(f.Reader, f.MessageId, f.Request("en-US"), default);
        InvalidOperationException? error = Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.TranslateAsync(f.Reader, f.MessageId, f.Request("es-ES"), default));
        Assert.That(error!.Message, Is.EqualTo("translation_rate_limited"));
    }

    [Test]
    public void Translation_wire_fields_are_stable_camel_case()
    {
        ChatTranslationResult value=new(Guid.NewGuid(),"fr-CA","en-US","test-v1","Hello","completed"); string json=JsonSerializer.Serialize(value,BeeJson.CreateDefaultOptions()); using JsonDocument doc=JsonDocument.Parse(json);
        Assert.Multiple(()=>{Assert.That(doc.RootElement.TryGetProperty("messageId",out _),Is.True);Assert.That(doc.RootElement.GetProperty("sourceLocale").GetString(),Is.EqualTo("fr-CA"));Assert.That(doc.RootElement.GetProperty("targetLocale").GetString(),Is.EqualTo("en-US"));Assert.That(doc.RootElement.GetProperty("modelVersion").GetString(),Is.EqualTo("test-v1"));Assert.That(doc.RootElement.GetProperty("translatedText").GetString(),Is.EqualTo("Hello"));Assert.That(doc.RootElement.GetProperty("status").GetString(),Is.EqualTo("completed"));});
    }

    [Test]
    public async Task Provider_failure_writes_no_incomplete_cache_and_retry_can_succeed()
    {
        ChatOptions options = new() { Enabled=true, TranslationMaxCharacters=1000, TranslationsPerMinutePerPlayer=10, TranslationModelVersion="test-v1", TranslationSourceLocale="fr-CA" };
        PlayerId reader=PlayerId.New(); Guid messageId=Guid.NewGuid(), conversationId=Guid.NewGuid(); DateTimeOffset now=new(2026,7,21,12,0,0,TimeSpan.Zero);
        InMemoryChatRepository chat=new(); chat.SaveConversation(new(conversationId,Guid.NewGuid(),Guid.NewGuid(),ChatChannelType.Private,"private",null,reader,now,null,null,"standard",1),[new(conversationId,reader,ChatPermissionRole.Member,now,null,true,true)]);
        chat.SaveMessage(new(messageId,conversationId,Guid.NewGuid(),Guid.NewGuid(),ChatChannelType.Private,reader,"reader","secret body",[],[],[],null,now,now,1,"request",ChatMessageState.Accepted,ChatModerationStatus.Clear,null,null,null,1));
        InMemoryChatTranslationRepository cache=new(); int calls=0;
        DelegateChatTranslationProvider provider=new((_,_)=>++calls==1?throw new InvalidOperationException("translation_provider_unavailable"):Task.FromResult("success"));
        ChatTranslationService service=new(chat,cache,provider,new ChatTranslationRateLimiter(Options.Create(options)),new FixedClock(now),Options.Create(options),new ChatTranslationDiagnostics(NullLogger<ChatTranslationDiagnostics>.Instance));
        Assert.ThrowsAsync<InvalidOperationException>(()=>service.TranslateAsync(reader,messageId,new(messageId,"en-US","test-v1"),default));
        Assert.That(cache.Get(messageId,"en-US","test-v1"),Is.Null);
        ChatTranslationResult retry=await service.TranslateAsync(reader,messageId,new(messageId,"en-US","test-v1"),default);
        Assert.Multiple(()=>{Assert.That(retry.TranslatedText,Is.EqualTo("success"));Assert.That(calls,Is.EqualTo(2));Assert.That(chat.GetMessage(messageId)!.Body,Is.EqualTo("secret body"));Assert.That(chat.GetLastSequence(conversationId),Is.EqualTo(1));});
    }

    [Test]
    public async Task Structured_translation_logs_exclude_message_translation_and_identifiers()
    {
        ChatOptions options=new(){Enabled=true,TranslationMaxCharacters=1000,TranslationsPerMinutePerPlayer=10,TranslationModelVersion="test-v1",TranslationSourceLocale="fr-CA"};PlayerId reader=PlayerId.New();Guid messageId=Guid.NewGuid(),conversationId=Guid.NewGuid();DateTimeOffset now=new(2026,7,21,12,0,0,TimeSpan.Zero);InMemoryChatRepository chat=new();chat.SaveConversation(new(conversationId,Guid.NewGuid(),Guid.NewGuid(),ChatChannelType.Private,"private",null,reader,now,null,null,"standard",1),[new(conversationId,reader,ChatPermissionRole.Member,now,null,true,true)]);chat.SaveMessage(new(messageId,conversationId,Guid.NewGuid(),Guid.NewGuid(),ChatChannelType.Private,reader,"reader","ORIGINAL_SECRET",[],[],[],null,now,now,1,"request-secret",ChatMessageState.Accepted,ChatModerationStatus.Clear,null,null,null,1));CollectingLogger logger=new();ChatTranslationService service=new(chat,new InMemoryChatTranslationRepository(),new DelegateChatTranslationProvider((_,_)=>Task.FromResult("TRANSLATED_SECRET")),new ChatTranslationRateLimiter(Options.Create(options)),new FixedClock(now),Options.Create(options),new ChatTranslationDiagnostics(logger));
        await service.TranslateAsync(reader,messageId,new(messageId,"en-US","test-v1"),default);await service.TranslateAsync(reader,messageId,new(messageId,"en-US","test-v1"),default);string combined=string.Join("\n",logger.Messages);
        Assert.Multiple(()=>{Assert.That(combined,Does.Contain("success"));Assert.That(combined,Does.Contain("cache"));Assert.That(combined,Does.Not.Contain("ORIGINAL_SECRET"));Assert.That(combined,Does.Not.Contain("TRANSLATED_SECRET"));Assert.That(combined,Does.Not.Contain(messageId.ToString()));Assert.That(combined,Does.Not.Contain(reader.Value.ToString()));});
    }

    private sealed class CollectingLogger:ILogger<ChatTranslationDiagnostics>
    {public List<string> Messages{get;}=[];public IDisposable? BeginScope<TState>(TState state)where TState:notnull=>null;public bool IsEnabled(LogLevel logLevel)=>true;public void Log<TState>(LogLevel logLevel,EventId eventId,TState state,Exception? exception,Func<TState,Exception?,string> formatter)=>Messages.Add(formatter(state,exception));}

    private sealed class Fixture
    {
        private readonly ChatOptions options;
        public readonly PlayerId Reader = PlayerId.New();
        public readonly Guid MessageId = Guid.NewGuid();
        public int ProviderCalls;
        public ChatTranslationService Service { get; }

        public Fixture(string body = "Bonjour", int maxCharacters = 1000, int perMinute = 10)
        {
            options = new ChatOptions { Enabled = true, TranslationMaxCharacters = maxCharacters, TranslationsPerMinutePerPlayer = perMinute, TranslationModelVersion = "test-v1", TranslationSourceLocale = "fr-CA" };
            InMemoryChatRepository chat = new(); Guid conversationId = Guid.NewGuid(); DateTimeOffset now = new(2026,7,21,12,0,0,TimeSpan.Zero);
            chat.SaveConversation(new(conversationId,Guid.NewGuid(),Guid.NewGuid(),ChatChannelType.Private,"private",null,Reader,now,null,null,"standard",1), [new(conversationId,Reader,ChatPermissionRole.Member,now,null,true,true)]);
            chat.SaveMessage(new(MessageId,conversationId,Guid.NewGuid(),Guid.NewGuid(),ChatChannelType.Private,Reader,"reader",body,[],[],[],null,now,now,1,"request",ChatMessageState.Accepted,ChatModerationStatus.Clear,null,null,null,1));
            DelegateChatTranslationProvider provider = new((input,_) => { ProviderCalls++; return Task.FromResult($"translated:{input.TargetLocale}"); });
            Service = new(chat,new InMemoryChatTranslationRepository(),provider,new ChatTranslationRateLimiter(Options.Create(options)),new FixedClock(now),Options.Create(options),new ChatTranslationDiagnostics(NullLogger<ChatTranslationDiagnostics>.Instance));
        }
        public ChatTranslationRequest Request(string locale="en-US") => new(MessageId,locale,options.TranslationModelVersion);
    }
    private sealed class FixedClock(DateTimeOffset now) : IServerClock { public DateTimeOffset UtcNow => now; }
}
