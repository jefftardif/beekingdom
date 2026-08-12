using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Accounts.Models;

public enum AccountStatus
{
    PendingVerification = 0,
    Active = 1,
    Suspended = 2,
    Banned = 3,
    Deleted = 4
}

public sealed record AccountProfile(
    Guid AccountId,
    PlayerId PlayerId,
    string DisplayName,
    string Email,
    string Language,
    string TimeZone,
    string Country,
    DateTimeOffset CreationDate,
    DateTimeOffset? LastLogin,
    AccountStatus Status);

public sealed record AccountSettings(string Currency, bool AnalyticsEnabled, bool CrossPlayEnabled);

public sealed record AccountPreferences(
    string Language,
    bool NotificationsEnabled,
    bool PrivateProfile,
    string GraphicsQuality,
    double MasterVolume,
    bool AllowFriendRequests,
    IReadOnlyDictionary<string, string> Extensions);

public sealed record AccountProgression(
    IReadOnlySet<string> GlobalAchievements,
    IReadOnlyDictionary<string, double> GlobalStatistics,
    IReadOnlySet<string> PermanentRewards,
    IReadOnlyList<string> SeasonHistory,
    IReadOnlyList<string> PurchaseHistory);

public sealed record AccountRecord(AccountProfile Profile, AccountSettings Settings, AccountPreferences Preferences, AccountProgression Progression);

public sealed record CreateAccountRequest(string DisplayName, string Email, string? Language = null, string? TimeZone = null, string? Country = null);

public sealed record AccountQuery(string? Email = null, AccountStatus? Status = null, string? DisplayNameContains = null);
