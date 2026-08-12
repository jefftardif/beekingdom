using BeeKingdom.Authentication.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Authentication.Providers;

public sealed record AuthenticationAccount(
    Guid AccountId,
    PlayerId PlayerId,
    string Email,
    string? PasswordHash,
    AccountSecurityState State,
    int FailedAttempts,
    DateTimeOffset? LockedUntilUtc,
    string? GoogleSubjectId = null,
    string? DisplayName = null,
    bool IsOnboarded = false);
