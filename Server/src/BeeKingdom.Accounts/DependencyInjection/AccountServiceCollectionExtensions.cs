using BeeKingdom.Accounts.Configuration;
using BeeKingdom.Accounts.Events;
using BeeKingdom.Accounts.Repositories;
using BeeKingdom.Persistence.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Accounts.DependencyInjection;

public static class AccountServiceCollectionExtensions
{
    public static IServiceCollection AddBeeKingdomAccounts(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AccountOptions>()
            .Bind(configuration.GetSection(AccountOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultLanguage), "DefaultLanguage is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultTimeZone), "DefaultTimeZone is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultCurrency), "DefaultCurrency is required.");

        if (PersistenceOptions.UsesSqlServer(configuration))
        {
            services.AddSingleton<IAccountRepository, SqlAccountRepository>();
        }
        else
        {
            services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
        }

        services.AddSingleton<IAccountEventSink, InMemoryAccountEventSink>();
        services.AddSingleton<IAccountService, AccountService>();
        services.AddSingleton<AccountManager>();
        // M043B-CL: generic player search/lookup, reusable by any domain (Alliance invites today,
        // Communication/Friends/mail recipient selection later) - see PlayerDirectoryService.cs.
        services.AddSingleton<IPlayerDirectoryService, PlayerDirectoryService>();

        // M043Q-CL: real IChatSenderDisplayNameResolver, registered AFTER AddBeeKingdomChat's own
        // NullChatSenderDisplayNameResolver default (see Program.cs ordering) so this wins for
        // single-instance resolution - same pattern as IAllianceMembershipResolver.
        services.AddSingleton<BeeKingdom.Chat.Audience.IChatSenderDisplayNameResolver, PlayerDirectoryChatSenderDisplayNameResolver>();

        return services;
    }
}
