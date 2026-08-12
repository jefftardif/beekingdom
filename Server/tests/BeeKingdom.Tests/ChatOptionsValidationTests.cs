using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.DependencyInjection;
using BeeKingdom.Chat.Translations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Tests;

public sealed class ChatOptionsValidationTests
{
    private static readonly (string Key, int Minimum, int Maximum)[] BoundedValues =
    [
        (nameof(ChatOptions.BodyMaxCharacters), 1, 4000),
        (nameof(ChatOptions.MessagesPerMinutePerPlayer), 1, 600),
        (nameof(ChatOptions.MessagesPerTenSecondsPerConversation), 1, 100),
        (nameof(ChatOptions.PrivateConversationCreatesPerHour), 1, 1000),
        (nameof(ChatOptions.MaxPrivateRecipients), 1, 100),
        (nameof(ChatOptions.IdempotencyReceiptRetentionDays), 2, 3650),
        (nameof(ChatOptions.MaxRequestBytes), 1024, 1048576)
        ,(nameof(ChatOptions.MaxRequestTargetBytes), 1024, 16384)
    ];

    [Test]
    public void CapabilityBoundsAcceptBothEdges()
    {
        foreach ((string key, int minimum, int maximum) in BoundedValues)
        {
            Assert.DoesNotThrow(() => ReadOptions(key, minimum), $"minimum {key}");
            Assert.DoesNotThrow(() => ReadOptions(key, maximum), $"maximum {key}");
        }
    }

    [Test]
    public void CapabilityBoundsRejectValuesJustOutsideEdges()
    {
        foreach ((string key, int minimum, int maximum) in BoundedValues)
        {
            Assert.Throws<OptionsValidationException>(() => ReadOptions(key, minimum - 1), $"below {key}");
            Assert.Throws<OptionsValidationException>(() => ReadOptions(key, maximum + 1), $"above {key}");
        }
    }

    [Test]
    public void ProtocolVersionCannotChangeSilently()
    {
        Assert.Throws<OptionsValidationException>(() => ReadOptions(nameof(ChatOptions.ProtocolVersion), "chat-v2"));
    }

    [Test]
    public void TranslationProviderDefaultsToUnavailableWithoutConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        using ServiceProvider provider = new ServiceCollection().AddBeeKingdomChat(configuration).BuildServiceProvider();
        Assert.That(provider.GetRequiredService<IChatTranslationProvider>(), Is.InstanceOf<UnavailableChatTranslationProvider>());
    }

    [Test]
    public void TranslationProviderSwitchesToDeepLWhenConfigured()
    {
        Dictionary<string, string?> values = new()
        {
            ["ChatTranslation:Provider"] = "deepl",
            ["ChatTranslation:ApiKey"] = "test-key:fx"
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        using ServiceProvider provider = new ServiceCollection().AddBeeKingdomChat(configuration).BuildServiceProvider();
        Assert.That(provider.GetRequiredService<IChatTranslationProvider>(), Is.InstanceOf<DeepLChatTranslationProvider>());
    }

    [Test]
    public void TranslationProviderStaysUnavailableWhenDeepLSelectedButKeyMissing()
    {
        Dictionary<string, string?> values = new() { ["ChatTranslation:Provider"] = "deepl" };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        using ServiceProvider provider = new ServiceCollection().AddBeeKingdomChat(configuration).BuildServiceProvider();
        Assert.That(provider.GetRequiredService<IChatTranslationProvider>(), Is.InstanceOf<UnavailableChatTranslationProvider>());
    }

    private static ChatOptions ReadOptions(string key, object value)
    {
        Dictionary<string, string?> values = new()
        {
            [$"Chat:{key}"] = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        using ServiceProvider provider = new ServiceCollection().AddBeeKingdomChat(configuration).BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ChatOptions>>().Value;
    }
}
