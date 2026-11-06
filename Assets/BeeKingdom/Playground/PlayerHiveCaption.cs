using System;
namespace BeeKingdom.Playground
{
    // Presentation only: never establishes an authoritative world-node identity.
    public static class PlayerHiveCaption
    {
        public static string Name(bool authenticated, Guid boundPlayer, Guid sessionPlayer, string displayName, string fallback)
        {
            return authenticated && boundPlayer != Guid.Empty && boundPlayer == sessionPlayer
                && !string.IsNullOrWhiteSpace(displayName) ? Plain(displayName) : fallback;
        }
        public static string Alliance(bool authenticated, Guid player, Guid membershipPlayer, bool ready, Guid allianceId, string name)
        {
            return authenticated && player != Guid.Empty && player == membershipPlayer && ready
                && allianceId != Guid.Empty && !string.IsNullOrWhiteSpace(name) ? Plain(name) : string.Empty;
        }
        private static string Plain(string value)
        {
            return value.Trim().Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        }
    }
}
