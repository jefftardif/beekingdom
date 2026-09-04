using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    // M041-CL: mirrors HiveResearchClient.cs's structure/session-transport plumbing exactly
    // (MobileAccountSessionGate, IGameAccountSessionSource, IAuthenticatedGameRestTransport,
    // single-refresh-on-401 retry). Response validation here is intentionally lighter than
    // HiveResearchClient's exhaustive per-field bounds checking - Alliance has ~15 distinct
    // response shapes vs research's 1, and the server side (AllianceService, tested) already
    // rejects anything structurally invalid; this client checks identity/shape sanity (never
    // trusts a null where a value is required) but not exhaustive numeric bounds. Endpoints match
    // Server/src/BeeKingdom.Server/Program.cs's /alliance/v1/* family - see
    // Docs/Alliance/ALLIANCE_PLATFORM_ARCHITECTURE.md section 20 for the full route list.
    //
    // Diplomacy/War endpoints deliberately NOT wrapped here yet - the existing Alliance Center
    // window's "diplomacy"/"war" tabs are still coming-soon placeholders (see M041-CL mission
    // report section 2), so there is nothing to wire them to today. Add DiplomacyProposeAsync/
    // DeclareWarAsync etc. here, mirroring the pattern below, when those tabs get real content.
    //
    // M043N-CL: no .ConfigureAwait(false) anywhere in this file, deliberately - Unity's
    // UnityWebRequest (built by UnityAuthenticatedGameRestTransport) can only be constructed on
    // the main thread. ConfigureAwait(false) lets a continuation resume on a thread-pool thread
    // instead of via Unity's SynchronizationContext, so any caller chaining two or more awaited
    // client calls (e.g. AllianceCenterPanelController.RefreshCoreAsync) would crash the second
    // UnityWebRequest with "Create can only be called from the main thread." Proven live: the
    // CEO's first successful Alliance ever exposed this the moment RefreshCoreAsync had a real
    // multi-call chain to walk (GetMyAlliance -> ListMembers -> ...) instead of exiting early on
    // NoAlliance after a single call.

    public enum RemoteAllianceRole { Member = 0, Officer = 1, Leader = 2 }
    public enum RemoteAllianceJoinMode { Open = 0, Application = 1, InviteOnly = 2 }
    public enum RemoteAllianceStatus { Active = 0, Disbanded = 1 }
    public enum RemoteAllianceApplicationStatus { Pending = 0, Accepted = 1, Rejected = 2, Cancelled = 3 }
    public enum RemoteAllianceInvitationStatus { Pending = 0, Accepted = 1, Declined = 2, Revoked = 3 }
    public enum RemoteAllianceActivityVisibility { Public = 0, MembersOnly = 1, OfficersOnly = 2, SystemPrivate = 3 }

    public enum RemoteAllianceActivityType
    {
        AllianceCreated = 0, MemberJoined = 1, MemberLeft = 2, MemberKicked = 3, MemberPromoted = 4,
        MemberDemoted = 5, LeadershipTransferred = 6, ProfileUpdated = 7,
        PlayerBuildingUpgraded = 100, PlayerResearchCompleted = 101, PlayerAttackStarted = 102,
        PlayerAttackWon = 103, PlayerAttackLost = 104, CreatureDefeated = 105, GatheringCompleted = 106,
        AllianceWarDeclared = 200, AllianceWarEnded = 201, AllianceDiplomacyChanged = 202,
        AllianceTerritoryCaptured = 300, AllianceBuildingUpgraded = 301, AllianceTechnologyCompleted = 302
    }

    [Serializable]
    public sealed class RemoteAllianceLeaderSummary
    {
        public Guid PlayerId { get; set; }
        public string DisplayName { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceDiplomacySummary
    {
        public int AllyCount { get; set; }
        public int NonAggressionPactCount { get; set; }
        public int HostileCount { get; set; }
        public int ActiveWarCount { get; set; }
    }

    [Serializable]
    public sealed class RemoteAlliancePublicProfile
    {
        public Guid AllianceId { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string EmblemKey { get; set; }
        public int MemberCount { get; set; }
        public int MaxMembers { get; set; }
        public RemoteAllianceLeaderSummary Leader { get; set; }
        public RemoteAllianceStatus Status { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public RemoteAllianceJoinMode JoinMode { get; set; }
        public string PublicSlug { get; set; }
        public RemoteAllianceDiplomacySummary Diplomacy { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceSummary
    {
        public Guid AllianceId { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public string EmblemKey { get; set; }
        public string Language { get; set; }
        public RemoteAllianceJoinMode JoinMode { get; set; }
        public int MemberCount { get; set; }
        public int MaxMembers { get; set; }
        public string PublicSlug { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceSearchPage
    {
        public List<RemoteAllianceSummary> Items { get; set; }
        public int TotalCount { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceMemberSummary
    {
        public Guid PlayerId { get; set; }
        // M043B-CL: real display name, batch-resolved server-side (never N+1 from Unity) - see
        // AllianceService.ListMembers / PlayerDirectoryService. Empty string, not null, when the
        // server has no account record to resolve (should not happen for a real active member).
        public string DisplayName { get; set; }
        public RemoteAllianceRole Role { get; set; }
        public DateTimeOffset JoinedAtUtc { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceMembership
    {
        public Guid AllianceId { get; set; }
        public Guid PlayerId { get; set; }
        public RemoteAllianceRole Role { get; set; }
        public DateTimeOffset JoinedAtUtc { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceEntity
    {
        public Guid AllianceId { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string EmblemKey { get; set; }
        public RemoteAllianceJoinMode JoinMode { get; set; }
        public RemoteAllianceStatus Status { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public Guid CreatedByPlayerId { get; set; }
        public Guid LeaderPlayerId { get; set; }
        public int MemberCount { get; set; }
        public int MaxMembers { get; set; }
        public string PublicSlug { get; set; }
        // M043-CL: forward reference to the real alliance chat conversation created in M042
        // (AllianceService.CreateOrLinkAllianceChat) - null until the chat link succeeds (best-effort,
        // see AllianceService.cs).
        public Guid? ChatConversationId { get; set; }
        public long Revision { get; set; }
    }

    // M043-CL: wrapper shapes matching the server's *Result records field-for-field
    // (Models/AllianceContracts.cs) - the M041 client wrongly deserialized several endpoints'
    // responses directly into the "inner" DTO (e.g. RemoteAllianceEntity) when the server actually
    // wraps it (e.g. CreateAllianceResult{Alliance,Deduplicated}); System.Text.Json silently produced
    // an all-default/empty object instead of throwing, so this went undetected until traced against
    // real server responses. Fixed by deserializing into these wrappers and unwrapping explicitly.
    [Serializable] public sealed class RemoteCreateAllianceResult { public RemoteAllianceEntity Alliance { get; set; } public bool Deduplicated { get; set; } }
    [Serializable] public sealed class RemoteJoinOpenAllianceResult { public RemoteAllianceEntity Alliance { get; set; } public RemoteAllianceMembership Membership { get; set; } }
    [Serializable] public sealed class RemoteApplicationDecisionResult { public RemoteAllianceApplication Application { get; set; } public RemoteAllianceMembership Membership { get; set; } }
    [Serializable] public sealed class RemoteInvitationDecisionResult { public RemoteAllianceInvitation Invitation { get; set; } public RemoteAllianceMembership Membership { get; set; } }
    [Serializable] public sealed class RemoteLeadershipTransferResult { public RemoteAllianceEntity Alliance { get; set; } public RemoteAllianceMembership PreviousLeader { get; set; } public RemoteAllianceMembership NewLeader { get; set; } }

    // M043-CL: matches the server's MyAllianceOverviewResponse(bool, AllianceEntity?, Membership?) -
    // the ONLY way to discover NO_ALLIANCE vs IN_ALLIANCE without already knowing an AllianceId.
    // Always a 200 OK with HasAlliance=false rather than a bare JSON null body.
    [Serializable] public sealed class RemoteMyAllianceOverview { public bool HasAlliance { get; set; } public RemoteAllianceEntity Alliance { get; set; } public RemoteAllianceMembership Membership { get; set; } }

    [Serializable]
    public sealed class RemoteAllianceApplication
    {
        public Guid ApplicationId { get; set; }
        public Guid AllianceId { get; set; }
        public Guid PlayerId { get; set; }
        public RemoteAllianceApplicationStatus Status { get; set; }
        public DateTimeOffset SubmittedAtUtc { get; set; }
        public string Message { get; set; }
    }

    // M043B-CL: matches the server's AllianceApplicationView - real DisplayName, batch-resolved
    // server-side, for the Leader/Officer application review UI.
    [Serializable]
    public sealed class RemoteAllianceApplicationView
    {
        public Guid ApplicationId { get; set; }
        public Guid AllianceId { get; set; }
        public Guid PlayerId { get; set; }
        public string DisplayName { get; set; }
        public RemoteAllianceApplicationStatus Status { get; set; }
        public DateTimeOffset SubmittedAtUtc { get; set; }
        public string Message { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceInvitation
    {
        public Guid InvitationId { get; set; }
        public Guid AllianceId { get; set; }
        public Guid InvitedPlayerId { get; set; }
        public Guid InvitedByPlayerId { get; set; }
        public RemoteAllianceInvitationStatus Status { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceActivityPayload
    {
        public string EntityKey { get; set; }
        public string EntityName { get; set; }
        public int? Level { get; set; }
        public string Result { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceActivityEvent
    {
        public Guid ActivityId { get; set; }
        public Guid AllianceId { get; set; }
        public RemoteAllianceActivityType Type { get; set; }
        public DateTimeOffset OccurredAtUtc { get; set; }
        public Guid? ActorPlayerId { get; set; }
        public Guid? TargetPlayerId { get; set; }
        public Guid? RelatedAllianceId { get; set; }
        public RemoteAllianceActivityVisibility Visibility { get; set; }
        public RemoteAllianceActivityPayload Payload { get; set; }
        public long Sequence { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceActivityPage
    {
        public List<RemoteAllianceActivityEvent> Items { get; set; }
        public long? NextBeforeSequence { get; set; }
    }

    // M045-CL: Alliance Help category strings match Server/src/BeeKingdom.HiveOperations/
    // SpeedUpContracts.cs's SpeedUpCategories exactly ("construction"/"research"/"training"/
    // "healing") - reused as-is, not a second vocabulary, since Alliance Help drives the same
    // OperationTimerReduction dispatch SpeedUp already uses.
    public static class RemoteAllianceHelpCategories
    {
        public const string Construction = "construction";
        public const string Research = "research";
        public const string Training = "training";
        public const string Healing = "healing";
    }

    public enum RemoteAllianceHelpRequestStatus { Open = 0, Completed = 1, Expired = 2, Cancelled = 3 }

    [Serializable]
    public sealed class RemoteAllianceHelpRequest
    {
        public Guid HelpRequestId { get; set; }
        public Guid AllianceId { get; set; }
        public Guid RequestingPlayerId { get; set; }
        public Guid RequestingHiveId { get; set; }
        public string OperationCategory { get; set; }
        public string OperationTargetId { get; set; }
        public Guid OperationId { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public RemoteAllianceHelpRequestStatus Status { get; set; }
        public long OriginalDurationSeconds { get; set; }
        public int HelpCount { get; set; }
        public int MaxHelpCount { get; set; }
        public long Revision { get; set; }
        public string ClientRequestId { get; set; }
    }

    // Read-model for the "Aides" list - DisplayName and RemainingSeconds are resolved/computed
    // server-side against the real operation at read time, never derived client-side.
    [Serializable]
    public sealed class RemoteAllianceHelpRequestView
    {
        public Guid HelpRequestId { get; set; }
        public Guid RequestingPlayerId { get; set; }
        public string RequestingDisplayName { get; set; }
        public string OperationCategory { get; set; }
        public string OperationTargetId { get; set; }
        public long RemainingSeconds { get; set; }
        public int HelpCount { get; set; }
        public int MaxHelpCount { get; set; }
        public bool AlreadyHelpedByMe { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceHelpCommandResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public RemoteAllianceHelpRequest Request { get; set; }
    }

    [Serializable]
    public sealed class RemoteContributeAllianceHelpResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public RemoteAllianceHelpRequest Request { get; set; }
        public long? DurationReductionSeconds { get; set; }
    }

    [Serializable]
    public sealed class RemoteContributeAllianceHelpAllResult
    {
        public List<RemoteContributeAllianceHelpResult> Results { get; set; }
    }

    // ---- wire request bodies (match the server's Models/AllianceContracts.cs records field-for-field) ----
    [Serializable] public sealed class CreateAllianceWireRequest { public string Name, Tag, Description, Language, EmblemKey, ClientRequestId; public RemoteAllianceJoinMode JoinMode; }
    [Serializable] public sealed class SubmitApplicationWireRequest { public string Message, ClientRequestId; }
    [Serializable] public sealed class CreateInvitationWireRequest { public Guid InvitedPlayerId; public string ClientRequestId; }
    [Serializable] public sealed class UpdateProfileWireRequest { public string Description, Language, EmblemKey; public RemoteAllianceJoinMode? JoinMode; public long ExpectedRevision; }
    [Serializable] public sealed class CreateAllianceHelpRequestWireRequest { public Guid HiveId; public string OperationCategory, OperationTargetId, ClientRequestId; }
    [Serializable] public sealed class AllianceHelpContributeWireRequest { public string ClientRequestId; }
    [Serializable] public sealed class AllianceResearchFundingTargetWireRequest { public string TechnologyId; public string ClientRequestId; }
    [Serializable] public sealed class AllianceResearchDonateWireRequest { public Guid HiveId; public string ResourceKey; public long Amount; public string ClientRequestId; }
    [Serializable] public sealed class AllianceResearchLaunchWireRequest { public string ClientRequestId; }
    [Serializable] public sealed class AllianceResearchSpeedUpWireRequest { public Guid HiveId; public string ItemId; public string ClientRequestId; }

    // M052-CL: Bible-aligned (BIBLE_ALLIANCE_RESEARCH.md V1.0) - field-for-field mirror of the
    // server's AllianceTechnologyReadModel/AllianceResearchReadSnapshot. FundingRequired/
    // FundingContributed are server-declared per-resource data (never a generic fixed bundle), and
    // State is the server's own resolved enum - Unity never re-derives Locked/Eligible/Funding/
    // Ready/Researching/Completed from raw booleans.
    [Serializable]
    public sealed class RemoteAllianceTechnology
    {
        public string TechnologyId { get; set; }
        public string Branch { get; set; }
        public string Category { get; set; } // "minor" | "major"
        public int Tier { get; set; }
        public string DisplayNameKey { get; set; }
        public string DescriptionKey { get; set; }
        public string BonusSummaryKey { get; set; }
        public List<string> PrerequisiteIds { get; set; }
        public string State { get; set; } // matches AllianceTechnologyState enum names
        public Dictionary<string, long> FundingRequired { get; set; }
        public Dictionary<string, long> FundingContributed { get; set; }
        public long ResearchDurationSeconds { get; set; }
        public DateTimeOffset? ResearchStartedAtUtc { get; set; }
        public DateTimeOffset? ResearchCompletesAtUtc { get; set; }
        public DateTimeOffset? CompletedAtUtc { get; set; }
        public long ProductionBp { get; set; }
        public long CapacityBp { get; set; }
        public long CombatPowerBp { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceResearchSnapshot
    {
        public Guid AllianceId { get; set; }
        public string ContractVersion { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public long Revision { get; set; }
        public List<RemoteAllianceTechnology> Technologies { get; set; }
        public string MinorFundingTargetId { get; set; }
        public string MajorFundingTargetId { get; set; }
        public string MinorResearchingTechnologyId { get; set; }
        public string MajorResearchingTechnologyId { get; set; }
        public long MyContributionPoints { get; set; }
        public long MyDonationCount { get; set; }
        public long MyAllianceCurrencyBalance { get; set; }
        // Server-computed authority - Unity reads these rather than re-deriving role permissions.
        public bool CanSelectFundingTarget { get; set; }
        public bool CanLaunch { get; set; }
        public bool CanUseSpeedUp { get; set; }
    }

    [Serializable]
    public sealed class RemoteAllianceResearchCommandResult
    {
        public bool Succeeded { get; set; }
        public string Code { get; set; }
        public RemoteAllianceResearchSnapshot Snapshot { get; set; }
    }

    public interface IAllianceClient
    {
        Task<RemoteMyAllianceOverview> GetMyAllianceAsync(CancellationToken cancellationToken = default);
        Task<RemoteAllianceEntity> CreateAllianceAsync(string name, string tag, string description, string language, string emblemKey, RemoteAllianceJoinMode joinMode, string clientRequestId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceSearchPage> SearchAsync(string nameOrTag, string language, RemoteAllianceJoinMode? joinMode, int offset, int limit, CancellationToken cancellationToken = default);
        Task<RemoteAlliancePublicProfile> GetProfileAsync(Guid allianceId, CancellationToken cancellationToken = default);
        Task<RemoteAlliancePublicProfile> GetProfileBySlugAsync(string slug, CancellationToken cancellationToken = default);
        Task<List<RemoteAllianceMemberSummary>> ListMembersAsync(Guid allianceId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceActivityPage> ListPublicActivityAsync(Guid allianceId, long? beforeSequence, int limit, CancellationToken cancellationToken = default);
        Task<RemoteAllianceActivityPage> ListActivityAsync(Guid allianceId, long? beforeSequence, int limit, CancellationToken cancellationToken = default);
        Task<RemoteAllianceMembership> JoinOpenAsync(Guid allianceId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceApplication> SubmitApplicationAsync(Guid allianceId, string message, string clientRequestId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceApplication> CancelApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceApplication> AcceptApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceApplication> RejectApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
        Task<List<RemoteAllianceApplicationView>> ListPendingApplicationsAsync(CancellationToken cancellationToken = default);
        Task<RemoteAllianceInvitation> CreateInvitationAsync(Guid allianceId, Guid invitedPlayerId, string clientRequestId, CancellationToken cancellationToken = default);
        Task<List<RemoteAllianceInvitation>> ListMyInvitationsAsync(CancellationToken cancellationToken = default);
        Task<RemoteAllianceInvitation> AcceptInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceInvitation> DeclineInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceInvitation> RevokeInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
        Task LeaveAsync(CancellationToken cancellationToken = default);
        Task KickAsync(Guid targetPlayerId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceMembership> PromoteAsync(Guid targetPlayerId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceMembership> DemoteAsync(Guid targetPlayerId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceEntity> TransferLeadershipAsync(Guid targetPlayerId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceEntity> DissolveAsync(CancellationToken cancellationToken = default);
        Task<RemoteAllianceEntity> UpdateProfileAsync(string description, string language, string emblemKey, RemoteAllianceJoinMode? joinMode, long expectedRevision, CancellationToken cancellationToken = default);

        // M045-CL: Alliance Help. Never a parallel timer client-side either - RemainingSeconds on
        // each view row comes straight from the server's live read against the real operation.
        Task<List<RemoteAllianceHelpRequestView>> ListHelpRequestsAsync(CancellationToken cancellationToken = default);
        Task<RemoteAllianceHelpRequest> GetMyOpenHelpRequestAsync(string operationCategory, string operationTargetId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceHelpCommandResult> CreateHelpRequestAsync(Guid hiveId, string operationCategory, string operationTargetId, string clientRequestId, CancellationToken cancellationToken = default);
        Task<RemoteContributeAllianceHelpResult> ContributeHelpAsync(Guid helpRequestId, string clientRequestId, CancellationToken cancellationToken = default);
        Task<RemoteContributeAllianceHelpAllResult> ContributeHelpAllAsync(string clientRequestId, CancellationToken cancellationToken = default);

        // M052-CL: Alliance Research (Bible-aligned lifecycle). Never a parallel/fabricated state
        // model client-side either - every field on RemoteAllianceTechnology/RemoteAllianceResearchSnapshot
        // comes straight from the server's shared, Alliance-owned AllianceResearchState.
        Task<RemoteAllianceResearchSnapshot> GetAllianceResearchAsync(CancellationToken cancellationToken = default);
        Task<RemoteAllianceResearchCommandResult> SelectAllianceResearchFundingTargetAsync(string technologyId, string clientRequestId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceResearchCommandResult> DonateToAllianceResearchAsync(string technologyId, Guid hiveId, string resourceKey, long amount, string clientRequestId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceResearchCommandResult> LaunchAllianceResearchAsync(string technologyId, string clientRequestId, CancellationToken cancellationToken = default);
        Task<RemoteAllianceResearchCommandResult> ApplyAllianceResearchSpeedUpAsync(string technologyId, Guid hiveId, string itemId, string clientRequestId, CancellationToken cancellationToken = default);
    }

    public sealed class AllianceClient : IAllianceClient
    {
        private const string BasePath = "/alliance/v1";
        private readonly MobileAccountSessionGate sessionGate;
        private readonly IGameAccountSessionSource sessionSource;
        private readonly IAuthenticatedGameRestTransport transport;

        public AllianceClient(
            MobileAccountSessionGate sessionGate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport)
        {
            this.sessionGate = sessionGate ?? throw new ArgumentNullException(nameof(sessionGate));
            this.sessionSource = sessionSource ?? throw new ArgumentNullException(nameof(sessionSource));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<RemoteMyAllianceOverview> GetMyAllianceAsync(CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteMyAllianceOverview>("GET", BasePath + "/membership/mine", null, cancellationToken);

        public async Task<RemoteAllianceEntity> CreateAllianceAsync(string name, string tag, string description, string language, string emblemKey, RemoteAllianceJoinMode joinMode, string clientRequestId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireKey(clientRequestId);
            RemoteCreateAllianceResult result = await SendAsync<RemoteCreateAllianceResult>("POST", BasePath + "/alliances",
                new CreateAllianceWireRequest { Name = name, Tag = tag, Description = description, Language = language, EmblemKey = emblemKey, JoinMode = joinMode, ClientRequestId = clientRequestId },
                cancellationToken);
            return result.Alliance;
        }

        public Task<RemoteAllianceSearchPage> SearchAsync(string nameOrTag, string language, RemoteAllianceJoinMode? joinMode, int offset, int limit, CancellationToken cancellationToken = default(CancellationToken))
        {
            string query = "?offset=" + offset + "&limit=" + limit;
            if (!string.IsNullOrWhiteSpace(nameOrTag)) query += "&nameOrTag=" + Uri.EscapeDataString(nameOrTag);
            if (!string.IsNullOrWhiteSpace(language)) query += "&language=" + Uri.EscapeDataString(language);
            if (joinMode.HasValue) query += "&joinMode=" + joinMode.Value;
            return SendAsync<RemoteAllianceSearchPage>("GET", BasePath + "/alliances/search" + query, null, cancellationToken);
        }

        public Task<RemoteAlliancePublicProfile> GetProfileAsync(Guid allianceId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(allianceId);
            return SendAsync<RemoteAlliancePublicProfile>("GET", BasePath + "/alliances/" + allianceId.ToString("D"), null, cancellationToken);
        }

        public Task<RemoteAlliancePublicProfile> GetProfileBySlugAsync(string slug, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(slug)) throw InvalidRequest("A slug is required.");
            return SendAsync<RemoteAlliancePublicProfile>("GET", BasePath + "/alliances/by-slug/" + Uri.EscapeDataString(slug), null, cancellationToken);
        }

        public Task<List<RemoteAllianceMemberSummary>> ListMembersAsync(Guid allianceId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(allianceId);
            return SendAsync<List<RemoteAllianceMemberSummary>>("GET", BasePath + "/alliances/" + allianceId.ToString("D") + "/members", null, cancellationToken);
        }

        public Task<RemoteAllianceActivityPage> ListPublicActivityAsync(Guid allianceId, long? beforeSequence, int limit, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(allianceId);
            string query = "?limit=" + limit + (beforeSequence.HasValue ? "&beforeSequence=" + beforeSequence.Value : "");
            return SendAsync<RemoteAllianceActivityPage>("GET", BasePath + "/alliances/" + allianceId.ToString("D") + "/activity/public" + query, null, cancellationToken);
        }

        public Task<RemoteAllianceActivityPage> ListActivityAsync(Guid allianceId, long? beforeSequence, int limit, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(allianceId);
            string query = "?limit=" + limit + (beforeSequence.HasValue ? "&beforeSequence=" + beforeSequence.Value : "");
            return SendAsync<RemoteAllianceActivityPage>("GET", BasePath + "/alliances/" + allianceId.ToString("D") + "/activity" + query, null, cancellationToken);
        }

        public async Task<RemoteAllianceMembership> JoinOpenAsync(Guid allianceId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(allianceId);
            RemoteJoinOpenAllianceResult result = await SendAsync<RemoteJoinOpenAllianceResult>("POST", BasePath + "/alliances/" + allianceId.ToString("D") + "/join", null, cancellationToken);
            return result.Membership;
        }

        public Task<RemoteAllianceApplication> SubmitApplicationAsync(Guid allianceId, string message, string clientRequestId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(allianceId);
            RequireKey(clientRequestId);
            return SendAsync<RemoteAllianceApplication>("POST", BasePath + "/alliances/" + allianceId.ToString("D") + "/applications",
                new SubmitApplicationWireRequest { Message = message ?? string.Empty, ClientRequestId = clientRequestId }, cancellationToken);
        }

        public Task<RemoteAllianceApplication> CancelApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(applicationId);
            return SendAsync<RemoteAllianceApplication>("POST", BasePath + "/applications/" + applicationId.ToString("D") + "/cancel", null, cancellationToken);
        }

        public async Task<RemoteAllianceApplication> AcceptApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(applicationId);
            RemoteApplicationDecisionResult result = await SendAsync<RemoteApplicationDecisionResult>("POST", BasePath + "/applications/" + applicationId.ToString("D") + "/accept", null, cancellationToken);
            return result.Application;
        }

        public Task<RemoteAllianceApplication> RejectApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(applicationId);
            return SendAsync<RemoteAllianceApplication>("POST", BasePath + "/applications/" + applicationId.ToString("D") + "/reject", null, cancellationToken);
        }

        public Task<RemoteAllianceInvitation> CreateInvitationAsync(Guid allianceId, Guid invitedPlayerId, string clientRequestId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(allianceId);
            RequireId(invitedPlayerId);
            RequireKey(clientRequestId);
            return SendAsync<RemoteAllianceInvitation>("POST", BasePath + "/alliances/" + allianceId.ToString("D") + "/invitations",
                new CreateInvitationWireRequest { InvitedPlayerId = invitedPlayerId, ClientRequestId = clientRequestId }, cancellationToken);
        }

        public Task<List<RemoteAllianceInvitation>> ListMyInvitationsAsync(CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<List<RemoteAllianceInvitation>>("GET", BasePath + "/invitations/mine", null, cancellationToken);

        public Task<List<RemoteAllianceApplicationView>> ListPendingApplicationsAsync(CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<List<RemoteAllianceApplicationView>>("GET", BasePath + "/applications/pending", null, cancellationToken);

        public async Task<RemoteAllianceInvitation> AcceptInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(invitationId);
            RemoteInvitationDecisionResult result = await SendAsync<RemoteInvitationDecisionResult>("POST", BasePath + "/invitations/" + invitationId.ToString("D") + "/accept", null, cancellationToken);
            return result.Invitation;
        }

        public Task<RemoteAllianceInvitation> DeclineInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(invitationId);
            return SendAsync<RemoteAllianceInvitation>("POST", BasePath + "/invitations/" + invitationId.ToString("D") + "/decline", null, cancellationToken);
        }

        public Task<RemoteAllianceInvitation> RevokeInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(invitationId);
            return SendAsync<RemoteAllianceInvitation>("POST", BasePath + "/invitations/" + invitationId.ToString("D") + "/revoke", null, cancellationToken);
        }

        public Task LeaveAsync(CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<object>("POST", BasePath + "/membership/leave", null, cancellationToken);

        public Task KickAsync(Guid targetPlayerId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(targetPlayerId);
            return SendAsync<object>("POST", BasePath + "/membership/" + targetPlayerId.ToString("D") + "/kick", null, cancellationToken);
        }

        public Task<RemoteAllianceMembership> PromoteAsync(Guid targetPlayerId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(targetPlayerId);
            return SendAsync<RemoteAllianceMembership>("POST", BasePath + "/membership/" + targetPlayerId.ToString("D") + "/promote", null, cancellationToken);
        }

        public Task<RemoteAllianceMembership> DemoteAsync(Guid targetPlayerId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(targetPlayerId);
            return SendAsync<RemoteAllianceMembership>("POST", BasePath + "/membership/" + targetPlayerId.ToString("D") + "/demote", null, cancellationToken);
        }

        public async Task<RemoteAllianceEntity> TransferLeadershipAsync(Guid targetPlayerId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(targetPlayerId);
            RemoteLeadershipTransferResult result = await SendAsync<RemoteLeadershipTransferResult>("POST", BasePath + "/membership/" + targetPlayerId.ToString("D") + "/transfer-leadership", null, cancellationToken);
            return result.Alliance;
        }

        public Task<RemoteAllianceEntity> DissolveAsync(CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteAllianceEntity>("POST", BasePath + "/alliances/dissolve", null, cancellationToken);

        public Task<RemoteAllianceEntity> UpdateProfileAsync(string description, string language, string emblemKey, RemoteAllianceJoinMode? joinMode, long expectedRevision, CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteAllianceEntity>("POST", BasePath + "/alliances/profile",
                new UpdateProfileWireRequest { Description = description, Language = language, EmblemKey = emblemKey, JoinMode = joinMode, ExpectedRevision = expectedRevision }, cancellationToken);

        // ---------------- M045-CL: Alliance Help ----------------

        public Task<List<RemoteAllianceHelpRequestView>> ListHelpRequestsAsync(CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<List<RemoteAllianceHelpRequestView>>("GET", BasePath + "/help/requests", null, cancellationToken);

        public Task<RemoteAllianceHelpRequest> GetMyOpenHelpRequestAsync(string operationCategory, string operationTargetId, CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteAllianceHelpRequest>("GET", BasePath + "/help/requests/mine?category=" + Uri.EscapeDataString(operationCategory) + "&targetId=" + Uri.EscapeDataString(operationTargetId), null, cancellationToken);

        public Task<RemoteAllianceHelpCommandResult> CreateHelpRequestAsync(Guid hiveId, string operationCategory, string operationTargetId, string clientRequestId, CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteAllianceHelpCommandResult>("POST", BasePath + "/help/requests",
                new CreateAllianceHelpRequestWireRequest { HiveId = hiveId, OperationCategory = operationCategory, OperationTargetId = operationTargetId, ClientRequestId = clientRequestId }, cancellationToken);

        public Task<RemoteContributeAllianceHelpResult> ContributeHelpAsync(Guid helpRequestId, string clientRequestId, CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(helpRequestId);
            return SendAsync<RemoteContributeAllianceHelpResult>("POST", BasePath + "/help/requests/" + helpRequestId.ToString("D") + "/contribute",
                new AllianceHelpContributeWireRequest { ClientRequestId = clientRequestId }, cancellationToken);
        }

        public Task<RemoteContributeAllianceHelpAllResult> ContributeHelpAllAsync(string clientRequestId, CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteContributeAllianceHelpAllResult>("POST", BasePath + "/help/contribute-all",
                new AllianceHelpContributeWireRequest { ClientRequestId = clientRequestId }, cancellationToken);

        // ---------------- M052-CL: Alliance Research (Bible-aligned lifecycle) ----------------

        public Task<RemoteAllianceResearchSnapshot> GetAllianceResearchAsync(CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteAllianceResearchSnapshot>("GET", BasePath + "/research", null, cancellationToken);

        public Task<RemoteAllianceResearchCommandResult> SelectAllianceResearchFundingTargetAsync(string technologyId, string clientRequestId, CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteAllianceResearchCommandResult>("POST", BasePath + "/research/funding-target",
                new AllianceResearchFundingTargetWireRequest { TechnologyId = technologyId, ClientRequestId = clientRequestId }, cancellationToken);

        public Task<RemoteAllianceResearchCommandResult> DonateToAllianceResearchAsync(string technologyId, Guid hiveId, string resourceKey, long amount, string clientRequestId, CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteAllianceResearchCommandResult>("POST", BasePath + "/research/" + Uri.EscapeDataString(technologyId) + "/donate",
                new AllianceResearchDonateWireRequest { HiveId = hiveId, ResourceKey = resourceKey, Amount = amount, ClientRequestId = clientRequestId }, cancellationToken);

        public Task<RemoteAllianceResearchCommandResult> LaunchAllianceResearchAsync(string technologyId, string clientRequestId, CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteAllianceResearchCommandResult>("POST", BasePath + "/research/" + Uri.EscapeDataString(technologyId) + "/launch",
                new AllianceResearchLaunchWireRequest { ClientRequestId = clientRequestId }, cancellationToken);

        public Task<RemoteAllianceResearchCommandResult> ApplyAllianceResearchSpeedUpAsync(string technologyId, Guid hiveId, string itemId, string clientRequestId, CancellationToken cancellationToken = default(CancellationToken))
            => SendAsync<RemoteAllianceResearchCommandResult>("POST", BasePath + "/research/" + Uri.EscapeDataString(technologyId) + "/speedup",
                new AllianceResearchSpeedUpWireRequest { HiveId = hiveId, ItemId = itemId, ClientRequestId = clientRequestId }, cancellationToken);

        // ---------------- plumbing (mirrors HiveResearchClient) ----------------

        private async Task<T> SendAsync<T>(string method, string path, object body, CancellationToken cancellationToken)
        {
            SessionContext context = await RequireSessionAsync(cancellationToken);
            var request = body == null
                ? new AuthenticatedGameRestRequest(method, path)
                : new AuthenticatedGameRestRequest(method, path, body);
            return await SendWithSingleAuthenticationRefreshAsync<T>(request, context, cancellationToken);
        }

        private async Task<SessionContext> RequireSessionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionGate.CanSubmitLogin)
                throw new HivePerimeterClientException(HivePerimeterClientError.NotConfigured, "Official account session transport is not ready.");

            var refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable != null)
            {
                try { return RequireUsableSession(await refreshable.GetFreshSessionAsync(cancellationToken)); }
                catch (OperationCanceledException) { throw; }
                catch (MobileAccountSessionException exception) { throw MapSessionFailure(exception); }
            }

            GameAccountSession session;
            if (!sessionSource.TryGetSession(out session)) session = null;
            return RequireUsableSession(session);
        }

        private async Task<T> SendWithSingleAuthenticationRefreshAsync<T>(AuthenticatedGameRestRequest request, SessionContext context, CancellationToken cancellationToken)
        {
            try
            {
                return await transport.SendAsync<T>(request, context.AccessToken, cancellationToken);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error != AuthenticatedGameRestError.Unauthorized) throw MapTransportFailure(exception);
            }

            var refreshable = sessionSource as IRefreshableGameAccountSessionSource;
            if (refreshable == null)
                throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "The game session was rejected.");

            GameAccountSession replacement;
            try { replacement = await refreshable.RefreshAfterUnauthorizedAsync(context.AccessToken, cancellationToken); }
            catch (OperationCanceledException) { throw; }
            catch (MobileAccountSessionException exception) { throw MapSessionFailure(exception); }

            if (replacement == null || replacement.PlayerId != context.PlayerId ||
                string.IsNullOrWhiteSpace(replacement.AccessToken) || replacement.AccessToken.Length > 8192)
            {
                await refreshable.InvalidateUnauthorizedSessionAsync(context.AccessToken, cancellationToken);
                throw InvalidResponse("The refreshed game session changed identity.");
            }

            try
            {
                return await transport.SendAsync<T>(request, replacement.AccessToken, cancellationToken);
            }
            catch (AuthenticatedGameRestException exception)
            {
                if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                {
                    await refreshable.InvalidateUnauthorizedSessionAsync(replacement.AccessToken, cancellationToken);
                    throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "The refreshed game session was rejected.");
                }
                throw MapTransportFailure(exception);
            }
        }

        private static SessionContext RequireUsableSession(GameAccountSession session)
        {
            if (session == null || session.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(session.AccessToken) || session.AccessToken.Length > 8192)
                throw new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, "An official account session is required.");
            return new SessionContext(session.PlayerId, session.AccessToken);
        }

        private static HivePerimeterClientException MapSessionFailure(MobileAccountSessionException exception)
        {
            if (exception.Error == MobileAccountSessionError.TransportFailure)
                return new HivePerimeterClientException(HivePerimeterClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == MobileAccountSessionError.NotConfigured)
                return new HivePerimeterClientException(HivePerimeterClientError.NotConfigured, exception.SafeCode);
            return new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, exception.SafeCode);
        }

        private static HivePerimeterClientException MapTransportFailure(AuthenticatedGameRestException exception)
        {
            if (exception.Error == AuthenticatedGameRestError.NetworkFailure)
                return new HivePerimeterClientException(HivePerimeterClientError.TransportFailure, exception.SafeCode);
            if (exception.Error == AuthenticatedGameRestError.Unauthorized)
                return new HivePerimeterClientException(HivePerimeterClientError.AuthenticationRequired, exception.SafeCode);
            return InvalidResponse(exception.SafeCode);
        }

        private static void RequireId(Guid value) { if (value == Guid.Empty) throw InvalidRequest("An identifier is required."); }
        private static void RequireKey(string value) { if (string.IsNullOrWhiteSpace(value) || value.Trim() != value || value.Length > 256) throw InvalidRequest("The idempotency key must contain between one and 256 trimmed characters."); }
        private static HivePerimeterClientException InvalidRequest(string message) => new HivePerimeterClientException(HivePerimeterClientError.InvalidRequest, message);
        private static HivePerimeterClientException InvalidResponse(string message) => new HivePerimeterClientException(HivePerimeterClientError.InvalidResponse, message);

        private sealed class SessionContext
        {
            public SessionContext(Guid playerId, string accessToken) { PlayerId = playerId; AccessToken = accessToken; }
            public Guid PlayerId { get; }
            public string AccessToken { get; }
        }
    }
}
