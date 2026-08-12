using BeeKingdom.Authentication.Configuration;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Authentication.Security;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Authentication.Tokens;

public sealed class AuthenticationTokenManager
{
    private readonly Dictionary<string, TokenRecord> tokensByHash = new(StringComparer.Ordinal);
    private readonly ITokenGenerator tokenGenerator;
    private readonly IServerClock clock;
    private readonly AuthenticationOptions options;
    private readonly object sync = new();

    public AuthenticationTokenManager(ITokenGenerator tokenGenerator, IServerClock clock, IOptions<AuthenticationOptions> options)
    {
        this.tokenGenerator = tokenGenerator;
        this.clock = clock;
        this.options = options.Value;
    }

    public AuthenticationTokenPair CreateTokenPair(PlayerId playerId, Guid accountId, string sessionId, DateTimeOffset? now = null)
    {
        string accessToken = tokenGenerator.CreateToken();
        string refreshToken = tokenGenerator.CreateToken();
        DateTimeOffset issuedAt = now ?? clock.UtcNow;
        DateTimeOffset accessExpires = issuedAt.Add(options.AccessTokenLifetime);
        DateTimeOffset refreshExpires = issuedAt.Add(options.RefreshTokenLifetime);

        lock (sync)
        {
            tokensByHash[tokenGenerator.HashToken(accessToken)] = new TokenRecord(playerId, accountId, sessionId, TokenKind.Access, accessExpires, false);
            tokensByHash[tokenGenerator.HashToken(refreshToken)] = new TokenRecord(playerId, accountId, sessionId, TokenKind.Refresh, refreshExpires, false);
        }

        return new AuthenticationTokenPair(accessToken, refreshToken, accessExpires, refreshExpires, playerId, sessionId);
    }

    public TokenValidationResult ValidateAccessToken(string token)
    {
        return Validate(token, TokenKind.Access);
    }

    public AuthenticationTokenPair? RotateRefreshToken(string refreshToken)
    {
        string hash = tokenGenerator.HashToken(refreshToken);
        lock (sync)
        {
            if (!tokensByHash.TryGetValue(hash, out TokenRecord? record) || record.Kind != TokenKind.Refresh || record.IsRevoked || record.ExpiresUtc <= clock.UtcNow)
            {
                return null;
            }

            tokensByHash[hash] = record with { IsRevoked = true };
            return CreateTokenPair(record.PlayerId, record.AccountId, record.SessionId);
        }
    }

    public bool RevokeToken(string token)
    {
        string hash = tokenGenerator.HashToken(token);
        lock (sync)
        {
            if (!tokensByHash.TryGetValue(hash, out TokenRecord? record))
            {
                return false;
            }

            tokensByHash[hash] = record with { IsRevoked = true };
            return true;
        }
    }

    private TokenValidationResult Validate(string token, TokenKind expectedKind)
    {
        if (!BearerTokenSyntax.IsValid(token))
        {
            return TokenValidationResult.Invalid("token_invalid");
        }

        string hash = tokenGenerator.HashToken(token);
        lock (sync)
        {
            if (!tokensByHash.TryGetValue(hash, out TokenRecord? record) || record.Kind != expectedKind)
            {
                return TokenValidationResult.Invalid("token_not_found");
            }

            if (record.IsRevoked)
            {
                return TokenValidationResult.Invalid("token_revoked");
            }

            if (record.ExpiresUtc <= clock.UtcNow)
            {
                return TokenValidationResult.Invalid("token_expired");
            }

            return TokenValidationResult.Valid(record.PlayerId, record.AccountId, record.SessionId);
        }
    }

    private enum TokenKind { Access, Refresh }

    private sealed record TokenRecord(PlayerId PlayerId, Guid AccountId, string SessionId, TokenKind Kind, DateTimeOffset ExpiresUtc, bool IsRevoked);
}
