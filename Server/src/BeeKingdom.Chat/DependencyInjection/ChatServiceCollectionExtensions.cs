using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Audience;
using BeeKingdom.Chat.Realtime;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Chat.Translations;
using BeeKingdom.Persistence.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Chat.DependencyInjection;

public static class ChatServiceCollectionExtensions
{
    public static IServiceCollection AddBeeKingdomChat(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ChatOptions>()
            .Bind(configuration.GetSection(ChatOptions.SectionName))
            .Validate(options => options.BodyMaxCharacters is >= 1 and <= 4000, "BodyMaxCharacters must be between 1 and 4000.")
            .Validate(options => options.MessagesPerMinutePerPlayer is >= 1 and <= 600, "MessagesPerMinutePerPlayer must be between 1 and 600.")
            .Validate(options => options.MessagesPerTenSecondsPerConversation is >= 1 and <= 100, "MessagesPerTenSecondsPerConversation must be between 1 and 100.")
            .Validate(options => options.PrivateConversationCreatesPerHour is >= 1 and <= 1000, "PrivateConversationCreatesPerHour must be between 1 and 1000.")
            .Validate(options => options.MaxPrivateRecipients is >= 1 and <= 100, "MaxPrivateRecipients must be between 1 and 100.")
            .Validate(options => options.TranslationMaxCharacters > 0, "TranslationMaxCharacters must be positive.")
            .Validate(options => options.TranslationsPerMinutePerPlayer > 0, "TranslationsPerMinutePerPlayer must be positive.")
            .Validate(options => options.IdempotencyReceiptRetentionDays is >= 2 and <= 3650, "IdempotencyReceiptRetentionDays must be between 2 and 3650 days.")
            .Validate(options => options.MaxRequestBytes is >= 1_024 and <= 1_048_576, "MaxRequestBytes must be between 1024 and 1048576 bytes.")
            .Validate(options => options.MaxRequestTargetBytes is >= 1_024 and <= 16_384, "MaxRequestTargetBytes must be between 1024 and 16384 bytes.")
            .Validate(options => options.ProtocolVersion == "chat-v1", "ProtocolVersion must remain chat-v1 until a compatible protocol is introduced.")
            .ValidateOnStart();

        if (PersistenceOptions.UsesSqlServer(configuration))
        {
            services.AddSingleton<IChatRepository, SqlChatRepository>();
        }
        else
        {
            services.AddSingleton<IChatRepository, InMemoryChatRepository>();
        }

        services.AddSingleton<IChatAudienceResolver, LocalChatAudienceResolver>();
        services.AddSingleton<IChatTranslationRepository>(provider =>
            PersistenceOptions.UsesSqlServer(configuration)
                ? ActivatorUtilities.CreateInstance<SqlChatTranslationRepository>(provider)
                : ActivatorUtilities.CreateInstance<InMemoryChatTranslationRepository>(provider));

        IConfigurationSection translationSection = configuration.GetSection(ChatTranslationOptions.SectionName);
        services.AddOptions<ChatTranslationOptions>().Bind(translationSection).ValidateOnStart();
        bool deepLConfigured = string.Equals(translationSection["Provider"], ChatTranslationOptions.DeepLProviderName, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(translationSection["ApiKey"]);
        if (deepLConfigured)
        {
            services.AddHttpClient();
            services.AddSingleton<IChatTranslationProvider>(provider =>
            {
                IOptions<ChatTranslationOptions> translationOptions = provider.GetRequiredService<IOptions<ChatTranslationOptions>>();
                HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(DeepLChatTranslationProvider));
                httpClient.Timeout = TimeSpan.FromSeconds(translationOptions.Value.TimeoutSeconds);
                return new DeepLChatTranslationProvider(httpClient, translationOptions);
            });
        }
        else
        {
            services.AddSingleton<IChatTranslationProvider, UnavailableChatTranslationProvider>();
        }
        services.AddSingleton<IChatTranslationRateLimiter, ChatTranslationRateLimiter>();
        services.AddSingleton<ChatTranslationDiagnostics>();
        services.AddSingleton<ChatTranslationService>();
        services.AddSingleton<IChatRealtimeDispatcher, SignalRChatRealtimeDispatcher>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<ChatManager>();
        // Match the REST endpoints' JSON conventions (camelCase enum strings) so realtime "chat.event"
        // payloads deserialize the same way on the client regardless of transport.
        services.AddSignalR().AddJsonProtocol(options =>
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));

        return services;
    }
}
