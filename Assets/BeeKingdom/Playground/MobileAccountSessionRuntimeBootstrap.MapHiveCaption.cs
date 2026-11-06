using System;
using BeeKingdom.Networking;
namespace BeeKingdom.Playground
{
    public static partial class MobileAccountSessionRuntimeBootstrap
    {
        public static string MapPlayerDisplayName(string fallback)
        {
            GameAccountSession session;
            if (client == null || !client.ServerGameplayAuthorityGranted || !client.TryGetSession(out session)) return fallback;
            return PlayerHiveCaption.Name(true, gameplayPlayerId, session.PlayerId, session.DisplayName, fallback);
        }
        public static string MapPlayerAllianceName(out bool membershipKnown)
        {
            membershipKnown = false;
            GameAccountSession session;
            if (client == null || !client.ServerGameplayAuthorityGranted || !client.TryGetSession(out session)
                || session.PlayerId != gameplayPlayerId || gameplayPlayerId == Guid.Empty) return string.Empty;
            var model = allianceCenterController?.Model;
            if (model == null) return string.Empty;
            // Loading/Error are unknown, not a confirmed absence. Never reuse stale Overview.
            membershipKnown = model.State == AllianceCenterScreenState.NoAlliance || model.State == AllianceCenterScreenState.Ready;
            if (model.State != AllianceCenterScreenState.Ready || model.Overview == null) return string.Empty;
            var overview = model.Overview;
            if (overview.MyPlayerId != session.PlayerId) { membershipKnown = false; return string.Empty; }
            return PlayerHiveCaption.Alliance(true, session.PlayerId, overview.MyPlayerId, true, overview.AllianceId, overview.Name);
        }
    }
}
