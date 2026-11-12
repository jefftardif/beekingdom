using System;
using System.Collections.Generic;

namespace BeeKingdom.Networking
{
    // Wire-only, read-only projections of Server/HiveOperations V1.
    // Never calculate yield, pollen balances, XP or ecological rewards in Unity.
    public sealed class RemoteCivilCohort
    {
        public Guid CohortId { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset ReadyAtUtc { get; set; }
        public long ExpectedWorkers { get; set; }
        public long HoneySpent { get; set; }
        public long PollenSpent { get; set; }
    }

    public sealed class RemoteCivilPopulationSnapshot
    {
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public string ContractVersion { get; set; }
        public long HiveRevision { get; set; }
        public long CivilRevision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public long Capacity { get; set; }
        public long AvailableWorkers { get; set; }
        public long AssignedWorkers { get; set; }
        public long RecoveringWorkers { get; set; }
        public RemoteCivilCohort Active { get; set; }
        public bool CanStart { get; set; }
        public string Status { get; set; }
    }

    public sealed class CivilCohortStartWireRequest
    {
        public long ExpectedRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteCivilCohortReceipt
    {
        public string IdempotencyKey { get; set; }
        public Guid CohortId { get; set; }
        public long RevisionBefore { get; set; }
        public long RevisionAfter { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public string Code { get; set; }
    }

    public sealed class RemoteCivilCohortResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public RemoteCivilPopulationSnapshot Snapshot { get; set; }
        public RemoteCivilCohortReceipt Receipt { get; set; }
    }

    public sealed class RemoteFloralSite
    {
        public string ContractVersion { get; set; }
        public string WorldId { get; set; }
        public string RegionId { get; set; }
        public string SiteId { get; set; }
        public string FamilyId { get; set; }
        public string NameFr { get; set; }
        public string NameEn { get; set; }
        public string CatalogVersion { get; set; }
        public long Revision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public long PollenRemaining { get; set; }
        public long PollenCapacity { get; set; }
        public int FertilityBp { get; set; }
        public int BiodiversityBp { get; set; }
        public string Phase { get; set; }
        public bool PublicAccess { get; set; }
        public long TotalPollinationPoints { get; set; }
        public int DistinctContributors { get; set; }
        public string MapSurfaceId { get; set; } = string.Empty;
        public string MapAnchorId { get; set; } = string.Empty;

        public string LocalizedName(string locale) =>
            string.Equals(locale, "en-US", StringComparison.OrdinalIgnoreCase)
                ? NameEn : NameFr;
    }

    public sealed class RemoteCivilExpeditionMission
    {
        public Guid MissionId { get; set; }
        public string WorldId { get; set; }
        public string RegionId { get; set; }
        public string SiteId { get; set; }
        public long Workers { get; set; }
        public string Mode { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset ArrivesAtUtc { get; set; }
        public DateTimeOffset WorkEndsAtUtc { get; set; }
        public DateTimeOffset ReturnsAtUtc { get; set; }
        public long PollenPerWorker { get; set; }
        public int FertilityGainBp { get; set; }
        public int BiodiversityGainBp { get; set; }
        public Guid? AllianceIdAtStart { get; set; }
    }

    public sealed class RemoteCivilExpeditionCompletion
    {
        public Guid MissionId { get; set; }
        public string SiteId { get; set; }
        public long PollenCredited { get; set; }
        public long PollinationPoints { get; set; }
        public DateTimeOffset CompletedAtUtc { get; set; }
        public long SiteRevisionAfter { get; set; }
        public long PollenBalanceAfter { get; set; }
    }

    public sealed class RemoteCivilExpeditionSnapshot
    {
        public string ContractVersion { get; set; }
        public Guid PlayerId { get; set; }
        public Guid HiveId { get; set; }
        public long HiveRevision { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public RemoteCivilExpeditionMission Active { get; set; }
        public string Status { get; set; }
        public RemoteCivilExpeditionCompletion LastCompletion { get; set; }
        public long AvailableWorkers { get; set; }
        public long AssignedWorkers { get; set; }
    }

    public sealed class CivilExpeditionStartWireRequest
    {
        public string SiteId { get; set; }
        public long Workers { get; set; }
        public long ExpectedHiveRevision { get; set; }
        public long ExpectedSiteRevision { get; set; }
        public string IdempotencyKey { get; set; }
        public string Mode { get; set; } = "balanced";
    }

    public sealed class RemoteCivilExpeditionReceipt
    {
        public string Action { get; set; }
        public string IdempotencyKey { get; set; }
        public string PayloadHash { get; set; }
        public Guid MissionId { get; set; }
        public long HiveRevisionBefore { get; set; }
        public long HiveRevisionAfter { get; set; }
        public long SiteRevisionBefore { get; set; }
        public long SiteRevisionAfter { get; set; }
        public DateTimeOffset AcceptedAtUtc { get; set; }
        public string Code { get; set; }
        public long CreditedPollen { get; set; }
    }

    public sealed class RemoteCivilExpeditionResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public RemoteCivilExpeditionSnapshot Snapshot { get; set; }
        public RemoteCivilExpeditionReceipt Receipt { get; set; }
    }

    public sealed class RemoteFloralAllianceProgression
    {
        public Guid AllianceId { get; set; }
        public long Revision { get; set; }
        public int Level { get; set; }
        public long Experience { get; set; }
        public long CompletedProjects { get; set; }
    }

    public sealed class RemoteFloralAllianceProject
    {
        public Guid ProjectId { get; set; }
        public Guid AllianceId { get; set; }
        public string WorldId { get; set; }
        public string SiteId { get; set; }
        public Guid CreatedByPlayerId { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public long Revision { get; set; }
        public long GoalPoints { get; set; }
        public int RequiredDistinctMembers { get; set; }
        public long MinimumPointsPerMember { get; set; }
        public long Points { get; set; }
        public int QualifiedMembers { get; set; }
        public string Status { get; set; }
        public DateTimeOffset? CompletedAtUtc { get; set; }
    }

    public sealed class RemoteFloralAllianceProjectSnapshot
    {
        public string ContractVersion { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public RemoteFloralAllianceProject Project { get; set; }
        public RemoteFloralAllianceProgression Progression { get; set; }
        public bool IsCompleted { get; set; }
    }

    public sealed class CreateFloralProjectWireRequest
    {
        public string SiteId { get; set; }
        public long ExpectedProgressionRevision { get; set; }
        public string IdempotencyKey { get; set; }
    }

    public sealed class RemoteFloralProjectCreateResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public RemoteFloralAllianceProjectSnapshot Snapshot { get; set; }
    }
}
