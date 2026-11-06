using System;
using BeeKingdom.Networking;
namespace BeeKingdom.Playground
{
    [Serializable] public sealed class OccupationSessionEvidence
    {
        public bool authenticatedAuthority;
        public string playerId,gameplayPlayerId,configuredHiveId,configuredWorldId;
        public string colonyId="",colonyStatus="No public authenticated ColonyId accessor in this client binding";
        public string hiveStatus="Configured gameplay HiveId; canonical ownership still requires server binding evidence";
        public string worldStatus="Configured territory world; not an authenticated colony/world mapping";
    }
    public static partial class MobileAccountSessionRuntimeBootstrap
    {
        // Public allowlisted properties only. No session object serialization, token access or I/O.
        public static OccupationSessionEvidence CaptureOccupationSessionEvidence()
        {
            bool authenticated=client!=null && client.State==MobileAccountSessionState.Authenticated && client.ServerGameplayAuthorityGranted;
            return new OccupationSessionEvidence {
                authenticatedAuthority=authenticated,
                playerId=authenticated?client.ActivePlayerId.ToString("D"):"",
                gameplayPlayerId=GameplayPlayerId==Guid.Empty?"":GameplayPlayerId.ToString("D"),
                configuredHiveId=GameplayHiveId==Guid.Empty?"":GameplayHiveId.ToString("D"),
                configuredWorldId=TerritoryConfiguredMapWorld??""
            };
        }
    }
}
