namespace BeeKingdom.Server;

// Internal player-support admin surface (view/adjust a player's hive state for bug fixes).
// Distinct from OpsSecurityOptions (infra/migrations) so the two keys can be rotated
// independently and given to different people if needed later.
public sealed class AdminSupportOptions
{
    public const string SectionName = "AdminSupport";

    public bool Enabled { get; set; }
    public string Key { get; set; } = string.Empty;
    public string KeySha256 { get; set; } = string.Empty;
}
