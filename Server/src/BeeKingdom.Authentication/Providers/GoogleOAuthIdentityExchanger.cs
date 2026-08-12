using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeeKingdom.Authentication.Configuration;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Authentication.Providers;

public sealed class GoogleOAuthIdentityExchanger : IGoogleIdentityExchanger
{
    private readonly HttpClient httpClient;
    private readonly GoogleOAuthOptions options;

    public GoogleOAuthIdentityExchanger(HttpClient httpClient, IOptions<GoogleOAuthOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
    }

    public async Task<GoogleIdentity> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string codeVerifier,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        using FormUrlEncodedContent body = new(new[]
        {
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("client_id", options.ClientId),
            new KeyValuePair<string, string>("client_secret", options.ClientSecret),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("code_verifier", codeVerifier),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });

        using HttpResponseMessage response = await httpClient.PostAsync(options.TokenEndpoint, body, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Google token exchange failed: " + responseBody);

        TokenResponseWire? wire = JsonSerializer.Deserialize<TokenResponseWire>(responseBody);
        if (wire == null || string.IsNullOrWhiteSpace(wire.IdToken))
            throw new InvalidOperationException("Google token exchange response did not contain an id_token.");

        GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(
            wire.IdToken,
            new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { options.ClientId } });

        return new GoogleIdentity(payload.Subject, payload.Email, payload.EmailVerified);
    }

    private sealed class TokenResponseWire
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }
    }
}
