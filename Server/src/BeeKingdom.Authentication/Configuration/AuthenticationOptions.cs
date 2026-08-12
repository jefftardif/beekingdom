namespace BeeKingdom.Authentication.Configuration;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);
    public int MaxSessionsPerAccount { get; set; } = 5;
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(10);
    public string MinimumClientVersion { get; set; } = "1.0.0";
}
