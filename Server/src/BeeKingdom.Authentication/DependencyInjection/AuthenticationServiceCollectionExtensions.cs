using BeeKingdom.Authentication.Configuration;
using BeeKingdom.Authentication.Events;
using BeeKingdom.Authentication.Providers;
using BeeKingdom.Authentication.Security;
using BeeKingdom.Authentication.Sessions;
using BeeKingdom.Authentication.Tokens;
using BeeKingdom.Persistence.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeeKingdom.Authentication.DependencyInjection;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddBeeKingdomAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthenticationOptions>()
            .Bind(configuration.GetSection(AuthenticationOptions.SectionName))
            .Validate(options => options.AccessTokenLifetime > TimeSpan.Zero, "AccessTokenLifetime must be positive.")
            .Validate(options => options.RefreshTokenLifetime > options.AccessTokenLifetime, "RefreshTokenLifetime must be greater than AccessTokenLifetime.")
            .Validate(options => options.MaxSessionsPerAccount > 0, "MaxSessionsPerAccount must be positive.")
            .Validate(options => options.MaxFailedAttempts > 0, "MaxFailedAttempts must be positive.");

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenGenerator, SecureTokenGenerator>();
        if (PersistenceOptions.UsesSqlServer(configuration))
        {
            services.AddSingleton<IAccountCredentialStore, SqlAccountCredentialStore>();
            services.AddSingleton<IAuthenticationSessionStore, SqlAuthenticationSessionStore>();
        }
        else
        {
            services.AddSingleton<IAccountCredentialStore, InMemoryAccountCredentialStore>();
            services.AddSingleton<IAuthenticationSessionStore, AuthenticationSessionStore>();
        }

        services.AddSingleton<IAuthenticationProvider, EmailPasswordAuthenticationProvider>();
        services.AddOptions<GoogleOAuthOptions>()
            .Bind(configuration.GetSection(GoogleOAuthOptions.SectionName));
        services.AddHttpClient<IGoogleIdentityExchanger, GoogleOAuthIdentityExchanger>();
        services.AddSingleton<AuthenticationTokenManager>();
        services.AddSingleton<AuthenticationSessionValidator>();
        services.AddSingleton<IAuthenticationEventSink, InMemoryAuthenticationEventSink>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<AuthenticationManager>();

        return services;
    }
}
