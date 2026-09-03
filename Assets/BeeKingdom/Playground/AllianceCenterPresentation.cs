using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using UnityEngine;

namespace BeeKingdom.Playground
{
    // M043-CL: mirrors HiveResearchPresentation.cs's structure exactly (screen state enum, an
    // immutable screen model projected from the raw wire DTOs, a controller that owns the async
    // client calls and mutates Model synchronously between OnGUI frames). See HiveResearchPresentation
    // for the reference this was copied from - kept as close to it as the two domains allow so a
    // future reader who already knows one understands the other immediately.
    public enum AllianceCenterScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        NoAlliance = 2,
        Ready = 3,
        Mutating = 4,
        Error = 5
    }

    public sealed class AllianceOverviewModel
    {
        internal AllianceOverviewModel(RemoteAllianceEntity source, Guid myPlayerId, RemoteAllianceRole myRole, DateTimeOffset myJoinedAtUtc)
        {
            MyPlayerId = myPlayerId;
            AllianceId = source.AllianceId;
            Name = source.Name ?? string.Empty;
            Tag = source.Tag ?? string.Empty;
            Description = source.Description ?? string.Empty;
            Language = source.Language ?? string.Empty;
            EmblemKey = source.EmblemKey ?? string.Empty;
            JoinMode = source.JoinMode;
            MemberCount = source.MemberCount;
            MaxMembers = source.MaxMembers;
            LeaderPlayerId = source.LeaderPlayerId;
            PublicSlug = source.PublicSlug ?? string.Empty;
            ChatConversationId = source.ChatConversationId;
            Revision = source.Revision;
            MyRole = myRole;
            MyJoinedAtUtc = myJoinedAtUtc;
        }

        public Guid MyPlayerId { get; }
        public Guid AllianceId { get; }
        public string Name { get; }
        public string Tag { get; }
        public string Description { get; }
        public string Language { get; }
        public string EmblemKey { get; }
        public RemoteAllianceJoinMode JoinMode { get; }
        public int MemberCount { get; }
        public int MaxMembers { get; }
        public Guid LeaderPlayerId { get; }
        public string PublicSlug { get; }
        public Guid? ChatConversationId { get; }
        public long Revision { get; }
        public RemoteAllianceRole MyRole { get; }
        public DateTimeOffset MyJoinedAtUtc { get; }

        public bool IsLeader => MyRole == RemoteAllianceRole.Leader;
        public bool IsOfficerOrLeader => MyRole == RemoteAllianceRole.Officer || MyRole == RemoteAllianceRole.Leader;
    }

    public sealed class AllianceMemberModel
    {
        internal AllianceMemberModel(RemoteAllianceMemberSummary source)
        {
            PlayerId = source.PlayerId;
            DisplayName = source.DisplayName ?? string.Empty;
            Role = source.Role;
            JoinedAtUtc = source.JoinedAtUtc;
        }

        public Guid PlayerId { get; }
        public string DisplayName { get; }
        public RemoteAllianceRole Role { get; }
        public DateTimeOffset JoinedAtUtc { get; }
        // M043B-CL: real DisplayName now exists server-side (AllianceService.ListMembers, batch-
        // resolved via PlayerDirectoryService) - the shortened-PlayerId fallback is used only when
        // the server genuinely has no account record to resolve (should not happen for a real
        // active member), never as the normal path.
        public string ResolvedDisplayName => !string.IsNullOrEmpty(DisplayName) ? DisplayName : PlayerId.ToString("D").Substring(0, 8);
    }

    public sealed class AllianceApplicationModel
    {
        internal AllianceApplicationModel(RemoteAllianceApplicationView source)
        {
            ApplicationId = source.ApplicationId;
            PlayerId = source.PlayerId;
            DisplayName = source.DisplayName ?? string.Empty;
            Status = source.Status;
            SubmittedAtUtc = source.SubmittedAtUtc;
            Message = source.Message ?? string.Empty;
        }

        public Guid ApplicationId { get; }
        public Guid PlayerId { get; }
        public string DisplayName { get; }
        public RemoteAllianceApplicationStatus Status { get; }
        public DateTimeOffset SubmittedAtUtc { get; }
        public string Message { get; }
        public string ResolvedDisplayName => !string.IsNullOrEmpty(DisplayName) ? DisplayName : PlayerId.ToString("D").Substring(0, 8);
    }

    public sealed class AllianceInvitationModel
    {
        internal AllianceInvitationModel(RemoteAllianceInvitation source)
        {
            InvitationId = source.InvitationId;
            AllianceId = source.AllianceId;
            InvitedPlayerId = source.InvitedPlayerId;
            InvitedByPlayerId = source.InvitedByPlayerId;
            Status = source.Status;
            CreatedAtUtc = source.CreatedAtUtc;
        }

        public Guid InvitationId { get; }
        public Guid AllianceId { get; }
        public Guid InvitedPlayerId { get; }
        public Guid InvitedByPlayerId { get; }
        public RemoteAllianceInvitationStatus Status { get; }
        public DateTimeOffset CreatedAtUtc { get; }
    }

    public sealed class AllianceSearchResultModel
    {
        internal AllianceSearchResultModel(RemoteAllianceSummary source)
        {
            AllianceId = source.AllianceId;
            Name = source.Name ?? string.Empty;
            Tag = source.Tag ?? string.Empty;
            EmblemKey = source.EmblemKey ?? string.Empty;
            Language = source.Language ?? string.Empty;
            JoinMode = source.JoinMode;
            MemberCount = source.MemberCount;
            MaxMembers = source.MaxMembers;
        }

        public Guid AllianceId { get; }
        public string Name { get; }
        public string Tag { get; }
        public string EmblemKey { get; }
        public string Language { get; }
        public RemoteAllianceJoinMode JoinMode { get; }
        public int MemberCount { get; }
        public int MaxMembers { get; }
    }

    public sealed class AllianceActivityModel
    {
        internal AllianceActivityModel(RemoteAllianceActivityEvent source)
        {
            ActivityId = source.ActivityId;
            Type = source.Type;
            OccurredAtUtc = source.OccurredAtUtc;
            ActorPlayerId = source.ActorPlayerId;
            TargetPlayerId = source.TargetPlayerId;
            Sequence = source.Sequence;
            EntityName = source.Payload?.EntityName ?? string.Empty;
            Level = source.Payload?.Level;
        }

        public Guid ActivityId { get; }
        public RemoteAllianceActivityType Type { get; }
        public DateTimeOffset OccurredAtUtc { get; }
        public Guid? ActorPlayerId { get; }
        public Guid? TargetPlayerId { get; }
        public long Sequence { get; }
        public string EntityName { get; }
        public int? Level { get; }
    }

    // M043B-CL: real player search results for the Alliance "Invite Player" flow. Deliberately kept
    // OUTSIDE AllianceCenterScreenModel (not alliance domain data) and owned directly by the
    // controller as a small separate mutable list - PlayerDirectoryClient is a generic, reusable
    // service (see PlayerDirectoryClient.cs), not an Alliance concept, so its results don't belong
    // in the Alliance-specific immutable model.
    // M043S-CL: the CEO's single "Inviter" click on Stara produced zero visible change - traced to
    // InvitePlayerCoreAsync discarding the CreateInvitationAsync result entirely and only ever
    // refreshing the alliance overview model, never this row list. A row now carries its own real
    // outcome instead of silently reverting to looking untouched regardless of what the server did.
    public enum InvitationRowStatus { Eligible, Sending, Sent, AlreadyPending, Error }

    public sealed class PlayerSearchResultModel
    {
        internal PlayerSearchResultModel(RemotePlayerPublicIdentity source)
        {
            PlayerId = source.PlayerId;
            DisplayName = source.DisplayName ?? string.Empty;
            Status = InvitationRowStatus.Eligible;
        }

        public Guid PlayerId { get; }
        public string DisplayName { get; }
        public InvitationRowStatus Status { get; internal set; }
    }

    public sealed class AllianceCenterScreenModel
    {
        internal AllianceCenterScreenModel(
            AllianceCenterScreenState state,
            string errorCode,
            AllianceOverviewModel overview,
            IReadOnlyList<AllianceMemberModel> members,
            IReadOnlyList<AllianceApplicationModel> applications,
            IReadOnlyList<AllianceInvitationModel> myInvitations,
            IReadOnlyList<AllianceSearchResultModel> searchResults,
            IReadOnlyList<AllianceActivityModel> activity,
            string mutatingOperation = null)
        {
            State = state;
            ErrorCode = errorCode ?? string.Empty;
            Overview = overview;
            Members = members ?? Array.Empty<AllianceMemberModel>();
            Applications = applications ?? Array.Empty<AllianceApplicationModel>();
            MyInvitations = myInvitations ?? Array.Empty<AllianceInvitationModel>();
            SearchResults = searchResults ?? Array.Empty<AllianceSearchResultModel>();
            Activity = activity ?? Array.Empty<AllianceActivityModel>();
            MutatingOperation = mutatingOperation ?? string.Empty;
        }

        public AllianceCenterScreenState State { get; }
        public string ErrorCode { get; }
        public AllianceOverviewModel Overview { get; }
        public IReadOnlyList<AllianceMemberModel> Members { get; }
        public IReadOnlyList<AllianceApplicationModel> Applications { get; }
        public IReadOnlyList<AllianceInvitationModel> MyInvitations { get; }
        public IReadOnlyList<AllianceSearchResultModel> SearchResults { get; }
        public IReadOnlyList<AllianceActivityModel> Activity { get; }
        public string MutatingOperation { get; }

        public bool HasAlliance => Overview != null;
        public bool IsMutating(string operation) => State == AllianceCenterScreenState.Mutating &&
            string.Equals(MutatingOperation, operation ?? string.Empty, StringComparison.Ordinal);
    }

    public static class AllianceCenterPresentation
    {
        public static AllianceCenterScreenModel NotConfigured() =>
            new AllianceCenterScreenModel(AllianceCenterScreenState.NotConfigured, string.Empty, null, null, null, null, null, null);

        public static AllianceCenterScreenModel Loading(AllianceCenterScreenModel previous) =>
            new AllianceCenterScreenModel(AllianceCenterScreenState.Loading, string.Empty,
                previous?.Overview, previous?.Members, previous?.Applications, previous?.MyInvitations,
                previous?.SearchResults, previous?.Activity);

        public static AllianceCenterScreenModel NoAlliance(IReadOnlyList<AllianceInvitationModel> myInvitations, IReadOnlyList<AllianceSearchResultModel> searchResults) =>
            new AllianceCenterScreenModel(AllianceCenterScreenState.NoAlliance, string.Empty, null, null, null,
                myInvitations, searchResults, null);

        public static AllianceCenterScreenModel Ready(
            RemoteAllianceEntity alliance,
            RemoteAllianceMembership myMembership,
            IReadOnlyList<RemoteAllianceMemberSummary> members,
            IReadOnlyList<RemoteAllianceApplicationView> applications,
            IReadOnlyList<RemoteAllianceActivityEvent> activity)
        {
            var overview = new AllianceOverviewModel(alliance, myMembership.PlayerId, myMembership.Role, myMembership.JoinedAtUtc);
            IReadOnlyList<AllianceMemberModel> memberModels = (members ?? Array.Empty<RemoteAllianceMemberSummary>())
                .Select(m => new AllianceMemberModel(m))
                .OrderBy(m => RoleSortKey(m.Role))
                .ThenBy(m => m.PlayerId)
                .ToArray();
            IReadOnlyList<AllianceApplicationModel> applicationModels = (applications ?? Array.Empty<RemoteAllianceApplicationView>())
                .Select(a => new AllianceApplicationModel(a)).ToArray();
            IReadOnlyList<AllianceActivityModel> activityModels = (activity ?? Array.Empty<RemoteAllianceActivityEvent>())
                .Select(e => new AllianceActivityModel(e)).ToArray();
            return new AllianceCenterScreenModel(AllianceCenterScreenState.Ready, string.Empty, overview,
                memberModels, applicationModels, Array.Empty<AllianceInvitationModel>(), Array.Empty<AllianceSearchResultModel>(), activityModels);
        }

        public static AllianceCenterScreenModel Mutating(AllianceCenterScreenModel previous, string operation) =>
            new AllianceCenterScreenModel(AllianceCenterScreenState.Mutating, string.Empty,
                previous?.Overview, previous?.Members, previous?.Applications, previous?.MyInvitations,
                previous?.SearchResults, previous?.Activity, operation);

        public static AllianceCenterScreenModel Error(AllianceCenterScreenModel previous, string errorCode) =>
            new AllianceCenterScreenModel(AllianceCenterScreenState.Error, errorCode,
                previous?.Overview, previous?.Members, previous?.Applications, previous?.MyInvitations,
                previous?.SearchResults, previous?.Activity);

        private static int RoleSortKey(RemoteAllianceRole role)
        {
            switch (role)
            {
                case RemoteAllianceRole.Leader: return 0;
                case RemoteAllianceRole.Officer: return 1;
                default: return 2;
            }
        }
    }

    // M043R-CL: distinguishes "haven't typed enough yet" from "searched and found nothing" from
    // "search failed" - the invite modal used to collapse all three into the same "type at least 2
    // characters" helper text, which read as a lie once a real 2+ character query legitimately
    // returned zero results or errored.
    public enum InvitePlayerSearchStatus { Idle, Searching, Empty, Results, Error }

    public interface IAllianceCenterPanelController
    {
        AllianceCenterScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        IReadOnlyList<PlayerSearchResultModel> InvitePlayerSearchResults { get; }
        InvitePlayerSearchStatus InviteSearchStatus { get; }
        void Refresh();
        void RefreshQuietly();
        void Search(string nameOrTag, string language, RemoteAllianceJoinMode? joinMode);
        void Create(string name, string tag, string description, string language, RemoteAllianceJoinMode joinMode);
        void JoinOpen(Guid allianceId);
        void SubmitApplication(Guid allianceId, string message);
        void AcceptApplication(Guid applicationId);
        void RejectApplication(Guid applicationId);
        void AcceptInvitation(Guid invitationId);
        void DeclineInvitation(Guid invitationId);
        void SearchPlayersForInvite(string query);
        void InvitePlayer(Guid playerId);
        void Leave();
        void Kick(Guid targetPlayerId);
        void Promote(Guid targetPlayerId);
        void Demote(Guid targetPlayerId);
        void TransferLeadership(Guid targetPlayerId);
        void Dissolve();
    }

    public sealed class UnavailableAllianceCenterPanelController : IAllianceCenterPanelController
    {
        private readonly AllianceCenterScreenModel model = AllianceCenterPresentation.NotConfigured();
        public AllianceCenterScreenModel Model => model;
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public IReadOnlyList<PlayerSearchResultModel> InvitePlayerSearchResults => Array.Empty<PlayerSearchResultModel>();
        public InvitePlayerSearchStatus InviteSearchStatus => InvitePlayerSearchStatus.Idle;
        public void Refresh() { }
        public void RefreshQuietly() { }
        public void Search(string nameOrTag, string language, RemoteAllianceJoinMode? joinMode) { }
        public void Create(string name, string tag, string description, string language, RemoteAllianceJoinMode joinMode) { }
        public void JoinOpen(Guid allianceId) { }
        public void SubmitApplication(Guid allianceId, string message) { }
        public void AcceptApplication(Guid applicationId) { }
        public void RejectApplication(Guid applicationId) { }
        public void AcceptInvitation(Guid invitationId) { }
        public void DeclineInvitation(Guid invitationId) { }
        public void SearchPlayersForInvite(string query) { }
        public void InvitePlayer(Guid playerId) { }
        public void Leave() { }
        public void Kick(Guid targetPlayerId) { }
        public void Promote(Guid targetPlayerId) { }
        public void Demote(Guid targetPlayerId) { }
        public void TransferLeadership(Guid targetPlayerId) { }
        public void Dissolve() { }
    }

    public interface IAllianceCenterMutationKeySource
    {
        string Create(string operation);
    }

    public sealed class SessionAllianceCenterMutationKeySource : IAllianceCenterMutationKeySource
    {
        public string Create(string operation)
        {
            string safe = string.IsNullOrWhiteSpace(operation) ? "mutation" : operation.Trim();
            return "mobile-alliance-" + safe + "-" + Guid.NewGuid().ToString("N");
        }
    }

    // The controller that owns every async AllianceClient call. Never awaited by the presenter -
    // the presenter only ever reads Model synchronously each OnGUI frame, exactly like
    // HiveResearchPanelController/researchController.
    public sealed class AllianceCenterPanelController : IAllianceCenterPanelController, IDisposable
    {
        private readonly IAllianceClient client;
        private readonly IPlayerDirectoryClient playerDirectory;
        private readonly IAllianceCenterMutationKeySource keySource;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private List<PlayerSearchResultModel> invitePlayerSearchResults = new List<PlayerSearchResultModel>();
        private InvitePlayerSearchStatus invitePlayerSearchStatus = InvitePlayerSearchStatus.Idle;
        private bool disposed;
        private bool busy;

        public AllianceCenterPanelController(IAllianceClient client, IPlayerDirectoryClient playerDirectory = null, IAllianceCenterMutationKeySource keySource = null)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            // M043B-CL: optional/nullable on purpose, same pattern as ChatManager on AllianceService -
            // the generic PlayerDirectoryClient (Assets/BeeKingdom/Networking/PlayerDirectoryClient.cs)
            // is a separate, reusable service; SearchPlayersForInvite silently no-ops without it
            // rather than throwing, so this controller keeps working standalone if it's ever absent.
            this.playerDirectory = playerDirectory;
            this.keySource = keySource ?? new SessionAllianceCenterMutationKeySource();
            Model = AllianceCenterPresentation.Loading(null);
        }

        public AllianceCenterScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;
        public IReadOnlyList<PlayerSearchResultModel> InvitePlayerSearchResults => invitePlayerSearchResults;
        public InvitePlayerSearchStatus InviteSearchStatus => invitePlayerSearchStatus;

        public void Refresh() { RunFireAndForget(() => RefreshCoreAsync(false)); }
        public void RefreshQuietly() { RunFireAndForget(() => RefreshCoreAsync(true)); }

        public void Search(string nameOrTag, string language, RemoteAllianceJoinMode? joinMode)
            => RunFireAndForget(() => SearchCoreAsync(nameOrTag, language, joinMode));

        public void Create(string name, string tag, string description, string language, RemoteAllianceJoinMode joinMode)
            => RunFireAndForget(() => CreateCoreAsync(name, tag, description, language, joinMode));

        public void JoinOpen(Guid allianceId) => RunFireAndForget(() => JoinOpenCoreAsync(allianceId));
        public void SubmitApplication(Guid allianceId, string message) => RunFireAndForget(() => SubmitApplicationCoreAsync(allianceId, message));
        public void AcceptApplication(Guid applicationId) => RunFireAndForget(() => AcceptApplicationCoreAsync(applicationId));
        public void RejectApplication(Guid applicationId) => RunFireAndForget(() => RejectApplicationCoreAsync(applicationId));
        public void AcceptInvitation(Guid invitationId) => RunFireAndForget(() => AcceptInvitationCoreAsync(invitationId));
        public void DeclineInvitation(Guid invitationId) => RunFireAndForget(() => DeclineInvitationCoreAsync(invitationId));
        public void SearchPlayersForInvite(string query) => RunFireAndForget(() => SearchPlayersForInviteCoreAsync(query));
        public void InvitePlayer(Guid playerId) => RunFireAndForget(() => InvitePlayerCoreAsync(playerId));
        public void Leave() => RunFireAndForget(() => LeaveCoreAsync());
        public void Kick(Guid targetPlayerId) => RunFireAndForget(() => KickCoreAsync(targetPlayerId));
        public void Promote(Guid targetPlayerId) => RunFireAndForget(() => PromoteCoreAsync(targetPlayerId));
        public void Demote(Guid targetPlayerId) => RunFireAndForget(() => DemoteCoreAsync(targetPlayerId));
        public void TransferLeadership(Guid targetPlayerId) => RunFireAndForget(() => TransferLeadershipCoreAsync(targetPlayerId));
        public void Dissolve() => RunFireAndForget(() => DissolveCoreAsync());

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
        }

        // ---- proof/test hooks (awaitable), mirrors HiveResearchPanelController's *ForProofAsync ----
        public Task RefreshForProofAsync() => RefreshCoreAsync(false);
        public Task SearchForProofAsync(string nameOrTag, string language, RemoteAllianceJoinMode? joinMode) => SearchCoreAsync(nameOrTag, language, joinMode);
        public Task CreateForProofAsync(string name, string tag, string description, string language, RemoteAllianceJoinMode joinMode) => CreateCoreAsync(name, tag, description, language, joinMode);
        public Task JoinOpenForProofAsync(Guid allianceId) => JoinOpenCoreAsync(allianceId);
        public Task LeaveForProofAsync() => LeaveCoreAsync();
        public Task DissolveForProofAsync() => DissolveCoreAsync();
        public Task SearchPlayersForInviteForProofAsync(string query) => SearchPlayersForInviteCoreAsync(query);
        public Task InvitePlayerForProofAsync(Guid playerId) => InvitePlayerCoreAsync(playerId);

        private async void RunFireAndForget(Func<Task> action) { try { await action(); } catch { /* Model already carries the Error state */ } }

        private async Task RefreshCoreAsync(bool quiet)
        {
            if (busy || disposed) return;
            busy = true;
            if (!quiet) Model = AllianceCenterPresentation.Loading(Model);
            try
            {
                RemoteMyAllianceOverview overview = await client.GetMyAllianceAsync(lifetime.Token);
                if (disposed) return;
                if (overview == null || !overview.HasAlliance || overview.Alliance == null || overview.Membership == null)
                {
                    List<RemoteAllianceInvitation> invitations = await SafeListMyInvitationsAsync();
                    if (disposed) return;
                    Model = AllianceCenterPresentation.NoAlliance(
                        invitations.Select(i => new AllianceInvitationModel(i)).ToArray(),
                        Array.Empty<AllianceSearchResultModel>());
                    return;
                }

                Guid allianceId = overview.Alliance.AllianceId;
                List<RemoteAllianceMemberSummary> members = await client.ListMembersAsync(allianceId, lifetime.Token);
                if (disposed) return;
                List<RemoteAllianceApplicationView> applications = overview.Membership.Role == RemoteAllianceRole.Leader || overview.Membership.Role == RemoteAllianceRole.Officer
                    ? await SafeListApplicationsAsync()
                    : new List<RemoteAllianceApplicationView>();
                if (disposed) return;
                RemoteAllianceActivityPage activityPage = await SafeListActivityAsync(allianceId);
                if (disposed) return;

                Model = AllianceCenterPresentation.Ready(overview.Alliance, overview.Membership, members, applications, activityPage?.Items);
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Model = AllianceCenterPresentation.Error(Model, "cancelled");
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error));
            }
            catch (Exception)
            {
                if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected");
            }
            finally { busy = false; }
        }

        // M043B-CL: the gap M043 documented (no "applications for my alliance" endpoint existed) is
        // closed - GET /alliance/v1/applications/pending, server-derived AllianceId, real DisplayName.
        private async Task<List<RemoteAllianceApplicationView>> SafeListApplicationsAsync()
        {
            try { return await client.ListPendingApplicationsAsync(lifetime.Token); }
            catch { return new List<RemoteAllianceApplicationView>(); }
        }

        private async Task<List<RemoteAllianceInvitation>> SafeListMyInvitationsAsync()
        {
            try { return await client.ListMyInvitationsAsync(lifetime.Token); }
            catch { return new List<RemoteAllianceInvitation>(); }
        }

        private async Task<RemoteAllianceActivityPage> SafeListActivityAsync(Guid allianceId)
        {
            try { return await client.ListActivityAsync(allianceId, null, 30, lifetime.Token); }
            catch { return null; }
        }

        private async Task SearchCoreAsync(string nameOrTag, string language, RemoteAllianceJoinMode? joinMode)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Loading(Model);
            try
            {
                RemoteAllianceSearchPage page = await client.SearchAsync(nameOrTag, language, joinMode, 0, 20, lifetime.Token);
                if (disposed) return;
                IReadOnlyList<AllianceSearchResultModel> results = (page?.Items ?? new List<RemoteAllianceSummary>())
                    .Select(s => new AllianceSearchResultModel(s)).ToArray();
                Model = AllianceCenterPresentation.NoAlliance(Model?.MyInvitations ?? Array.Empty<AllianceInvitationModel>(), results);
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            finally { busy = false; }
        }

        private async Task CreateCoreAsync(string name, string tag, string description, string language, RemoteAllianceJoinMode joinMode)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "create");
            try
            {
                string key = keySource.Create("create");
                await client.CreateAllianceAsync(name, tag, description, language, string.Empty, joinMode, key, lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task JoinOpenCoreAsync(Guid allianceId)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "join");
            try
            {
                await client.JoinOpenAsync(allianceId, lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task SubmitApplicationCoreAsync(Guid allianceId, string message)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "apply");
            try
            {
                string key = keySource.Create("apply");
                await client.SubmitApplicationAsync(allianceId, message, key, lifetime.Token);
                if (disposed) return;
                Model = AllianceCenterPresentation.NoAlliance(Model?.MyInvitations ?? Array.Empty<AllianceInvitationModel>(), Model?.SearchResults ?? Array.Empty<AllianceSearchResultModel>());
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            finally { busy = false; }
        }

        private async Task AcceptApplicationCoreAsync(Guid applicationId)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "accept-application");
            try
            {
                await client.AcceptApplicationAsync(applicationId, lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task RejectApplicationCoreAsync(Guid applicationId)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "reject-application");
            try
            {
                await client.RejectApplicationAsync(applicationId, lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task AcceptInvitationCoreAsync(Guid invitationId)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "accept-invitation");
            try
            {
                await client.AcceptInvitationAsync(invitationId, lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task DeclineInvitationCoreAsync(Guid invitationId)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "decline-invitation");
            try
            {
                await client.DeclineInvitationAsync(invitationId, lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task SearchPlayersForInviteCoreAsync(string query)
        {
            if (disposed) return;
            if (playerDirectory == null || string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                invitePlayerSearchResults = new List<PlayerSearchResultModel>();
                invitePlayerSearchStatus = InvitePlayerSearchStatus.Idle;
                return;
            }
            invitePlayerSearchStatus = InvitePlayerSearchStatus.Searching;
            try
            {
                List<RemotePlayerPublicIdentity> results = await playerDirectory.SearchAsync(query, 0, 20, lifetime.Token);
                if (disposed) return;
                invitePlayerSearchResults = (results ?? new List<RemotePlayerPublicIdentity>())
                    .Select(r => new PlayerSearchResultModel(r)).ToList();
                invitePlayerSearchStatus = invitePlayerSearchResults.Count == 0 ? InvitePlayerSearchStatus.Empty : InvitePlayerSearchStatus.Results;
            }
            catch (Exception)
            {
                if (!disposed)
                {
                    invitePlayerSearchResults = new List<PlayerSearchResultModel>();
                    invitePlayerSearchStatus = InvitePlayerSearchStatus.Error;
                }
            }
        }

        private async Task InvitePlayerCoreAsync(Guid playerId)
        {
            if (busy || disposed || Model?.Overview == null) return;
            PlayerSearchResultModel row = invitePlayerSearchResults.FirstOrDefault(r => r.PlayerId == playerId);
            if (row == null || row.Status == InvitationRowStatus.Sending || row.Status == InvitationRowStatus.Sent || row.Status == InvitationRowStatus.AlreadyPending) return;
            busy = true;
            row.Status = InvitationRowStatus.Sending;
            Model = AllianceCenterPresentation.Mutating(Model, "invite");
            try
            {
                string key = keySource.Create("invite");
                await client.CreateInvitationAsync(Model.Overview.AllianceId, playerId, key, lifetime.Token);
                row.Status = InvitationRowStatus.Sent;
                busy = false;
                if (disposed) return;
                await RefreshCoreAsync(true);
                return;
            }
            catch (HivePerimeterClientException error)
            {
                string code = StableError(error);
                row.Status = code == "already_invited" || code == "target_already_in_alliance" ? InvitationRowStatus.AlreadyPending : InvitationRowStatus.Error;
                // M043S-CL: was completely silent on failure (Model flipped to an Error state the
                // invite modal never reads, and nothing else logged anything) - a real rejection
                // and "the click never registered" were indistinguishable from the CEO's side. Now
                // always leaves a trace, whatever the underlying cause turns out to be. Logs the raw
                // error.Message too, not just the sanitized StableError code - a genuine
                // InvalidResponse (deserialization/transport-level failure, not a clean server
                // rejection) loses its real detail once collapsed to "invalid_response".
                Debug.LogWarning("[AllianceInvite] CreateInvitation rejected for player " + playerId + ": code=" + code + " rawError=" + error.Error + " rawMessage=" + error.Message);
                Model = AllianceCenterPresentation.Error(Model, code);
            }
            catch (Exception exception)
            {
                if (!disposed)
                {
                    row.Status = InvitationRowStatus.Error;
                    Debug.LogWarning("[AllianceInvite] CreateInvitation failed for player " + playerId + ": " + exception.GetType().Name + " - " + exception.Message);
                    Model = AllianceCenterPresentation.Error(Model, "unexpected");
                }
            }
            busy = false;
        }

        private async Task LeaveCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "leave");
            try
            {
                await client.LeaveAsync(lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task KickCoreAsync(Guid targetPlayerId)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "kick");
            try
            {
                await client.KickAsync(targetPlayerId, lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task PromoteCoreAsync(Guid targetPlayerId)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "promote");
            try
            {
                await client.PromoteAsync(targetPlayerId, lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task DemoteCoreAsync(Guid targetPlayerId)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "demote");
            try
            {
                await client.DemoteAsync(targetPlayerId, lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task TransferLeadershipCoreAsync(Guid targetPlayerId)
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "transfer-leadership");
            try
            {
                await client.TransferLeadershipAsync(targetPlayerId, lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private async Task DissolveCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            Model = AllianceCenterPresentation.Mutating(Model, "dissolve");
            try
            {
                await client.DissolveAsync(lifetime.Token);
                if (disposed) return;
                busy = false;
                await RefreshCoreAsync(false);
                return;
            }
            catch (HivePerimeterClientException error) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, StableError(error)); }
            catch (Exception) { if (!disposed) Model = AllianceCenterPresentation.Error(Model, "unexpected"); }
            busy = false;
        }

        private static string StableError(HivePerimeterClientException error)
        {
            // M043G-CL: was an exhaustive string whitelist that had already drifted out of sync
            // with the real server codes (it checked for the literal "alliance.alliance_disabled",
            // but Program.cs's ExecuteAlliance has always sent "alliance.unavailable" for a
            // disabled Alliance service; "alliance.not_found"/"alliance.forbidden"/
            // "alliance.invalid_request" - the 403/404/400 cases - were never covered at all, and
            // any new 409 message ExecuteAlliance's dynamic "alliance." + exception.Message can
            // produce would silently fall through too). AllianceClient.MapTransportFailure
            // deliberately preserves the server's real SafeCode on every non-2xx/non-401 rejection
            // (see AllianceClient.cs) - every one of them is "alliance.<realCode>", so stripping
            // that fixed prefix and using the remainder directly is correct and cannot drift again,
            // unlike an enumerated switch. A genuinely malformed/unparseable response (the server
            // sent something that isn't even valid JSON, or isn't an alliance.* error envelope at
            // all) still falls through to the generic "invalid_response" below - that case is real
            // and distinct from "the server validly rejected this with a specific reason".
            if (error.Error == HivePerimeterClientError.InvalidResponse &&
                !string.IsNullOrEmpty(error.Message) && error.Message.StartsWith("alliance.", StringComparison.Ordinal))
            {
                return error.Message.Substring("alliance.".Length);
            }
            switch (error.Error)
            {
                case HivePerimeterClientError.NotConfigured: return "not_configured";
                case HivePerimeterClientError.AuthenticationRequired: return "session_required";
                case HivePerimeterClientError.InvalidRequest: return "invalid_request";
                case HivePerimeterClientError.InvalidResponse: return "invalid_response";
                case HivePerimeterClientError.TransportFailure: return "network_unavailable";
                default: return "unexpected";
            }
        }
    }
}
