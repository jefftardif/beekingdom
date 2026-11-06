using System;

namespace BeeKingdom.Networking
{
    // Pure candidate acceptance gate. No transport, polling, Unity lifecycle or account credentials.
    // The owner must Reset on login/logout, same-account reconnect, alliance/config change or disable.
    public sealed class AllianceHeadquartersResponseGate
    {
        private Guid playerId;
        private Guid allianceId;
        private string worldId;
        private string surfaceId;
        private string areaUnit;
        private long generation;
        private long latestRequest;
        private RemoteHeadquartersRevision floor;
        private DateTimeOffset latestServerTime;

        public long Reset(Guid player, Guid alliance, string world, string surface, string unit)
        {
            generation++;
            playerId = player;
            allianceId = alliance;
            worldId = world;
            surfaceId = surface;
            areaUnit = unit;
            floor = null;
            latestServerTime = default;
            return generation;
        }

        public long BeginRequest() => ++latestRequest;

        public bool Accept(long requestGeneration, long requestId, RemoteHeadquartersResponse response, DateTimeOffset now)
        {
            if (requestGeneration != generation || requestId != latestRequest || playerId == Guid.Empty
                || allianceId == Guid.Empty || string.IsNullOrWhiteSpace(worldId) || string.IsNullOrWhiteSpace(surfaceId)
                || string.IsNullOrWhiteSpace(areaUnit) || response == null || response.ContractVersion != RemoteHeadquartersResponse.Version
                || response.PlayerId != playerId || response.ServerTimeUtc < now.AddSeconds(-30)
                || response.ServerTimeUtc > now.AddSeconds(5) || response.ServerTimeUtc < latestServerTime) return false;
            var details = response.Details;
            var revision = details?.Revision;
            if (revision == null || details.WorldId != worldId || details.SurfaceId != surfaceId || details.AreaUnit != areaUnit
                || revision.AllianceId != allianceId || revision.WorldId != worldId || revision.SurfaceId != surfaceId
                || revision.IdentityGeneration < 0 || (revision.BuildingId != Guid.Empty && revision.IdentityGeneration == 0)
                || revision.Membership < 0 || revision.Scope < 0 || revision.Progression < 0 || revision.Placement < 0
                || revision.Territory < 0 || revision.Headquarters < 0 || revision.WorkReceipts < 0 || revision.WorkUnits < 0
                || (floor != null && !revision.AtLeast(floor))) return false;
            if (details.Construction != null && (details.Construction.BuildingId != revision.BuildingId
                || details.Construction.RequiredWork <= 0 || details.Construction.CompletedWork < 0
                || details.Construction.CompletedWork > details.Construction.RequiredWork
                || details.Construction.CompletedWork != revision.WorkUnits)) return false;
            floor = revision.Copy();
            latestServerTime = response.ServerTimeUtc;
            return true;
        }
    }
}
