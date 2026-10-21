using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Networking
{
    public enum PollinationClientError
    {
        NotConfigured,
        AuthenticationRequired,
        InvalidRequest,
        InvalidResponse,
        NetworkUnavailable,
        ServerRejected
    }

    public sealed class PollinationClientException : Exception
    {
        public PollinationClientException(PollinationClientError error, string safeCode)
            : base(safeCode ?? string.Empty)
        {
            Error = error;
            SafeCode = safeCode ?? string.Empty;
        }

        public PollinationClientError Error { get; }
        public string SafeCode { get; }
    }

    public interface IPollinationClient
    {
        bool Enabled { get; }
        Task<RemoteCivilPopulationSnapshot> ReadPopulationAsync(
            Guid hiveId, CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteCivilCohortResult> StartCohortAsync(
            Guid hiveId, long expectedRevision, string key,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<IReadOnlyList<RemoteFloralSite>> ReadSitesAsync(
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteFloralSite> ReadSiteAsync(
            string siteId, CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteCivilExpeditionSnapshot> ReadExpeditionAsync(
            Guid hiveId, CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteCivilExpeditionResult> StartExpeditionAsync(
            Guid hiveId, string siteId, long workers,
            long hiveRevision, long siteRevision, string key,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<IReadOnlyList<RemoteFloralAllianceProjectSnapshot>> ReadProjectsAsync(
            Guid allianceId, CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteFloralAllianceProjectSnapshot> ReadProjectAsync(
            Guid allianceId, Guid projectId,
            CancellationToken cancellationToken = default(CancellationToken));
        Task<RemoteFloralProjectCreateResult> CreateProjectAsync(
            Guid allianceId, string siteId, long expectedRevision, string key,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// Opt-in, authenticated, server-authoritative pollination API adapter.
    /// Never exposes demo rewards, harvest RPCs, offline mutation queues,
    /// fake identities, or an automatic retry after an uncertain network POST.
    /// Authentication refresh retries exactly the SAME request at most once,
    /// but only if the player's identity remains unchanged.
    /// </summary>
    public sealed class PollinationClient : IPollinationClient
    {
        public const string PopulationVersion = "bk-civil-population-v1-alpha";
        public const string SiteVersion = "bk-world-floral-site-v1-alpha";
        public const string ExpeditionVersion = "bk-civil-expedition-v1-alpha";
        public const string ProjectVersion = "bk-floral-alliance-project-v1-alpha";
        public const string CohortStartCode = "game.civil_cohort_started";
        public const string ExpeditionStartCode = "game.civil_expedition_started";
        public const string ProjectCreateCode = "game.floral_project_created";

        private readonly MobileAccountSessionGate gate;
        private readonly IGameAccountSessionSource sessions;
        private readonly IAuthenticatedGameRestTransport transport;

        public PollinationClient(
            MobileAccountSessionGate gate,
            IGameAccountSessionSource sessions,
            IAuthenticatedGameRestTransport transport, bool enabled = false)
        {
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Enabled = enabled;
        }

        public bool Enabled { get; }

        public static string PopulationPath(Guid hiveId) =>
            HivePath(hiveId) + "/civil-population";
        public static string CohortStartPath(Guid hiveId) =>
            PopulationPath(hiveId) + "/cohorts/start";
        public const string SitesPath = "/game/v1/world/floral-sites";
        public static string SitePath(string siteId)
        {
            RequireId(siteId);
            return SitesPath + "/" + Uri.EscapeDataString(siteId);
        }
        public static string ExpeditionPath(Guid hiveId) =>
            HivePath(hiveId) + "/civil-expeditions";
        public static string ExpeditionStartPath(Guid hiveId) =>
            ExpeditionPath(hiveId) + "/start";
        public static string ProjectsPath(Guid allianceId)
        {
            if (allianceId == Guid.Empty) throw InvalidRequest("alliance_required");
            return "/alliance/v1/alliances/" + allianceId.ToString("D") + "/floral-projects";
        }
        public static string ProjectPath(Guid allianceId, Guid projectId)
        {
            if (projectId == Guid.Empty) throw InvalidRequest("project_required");
            return ProjectsPath(allianceId) + "/" + projectId.ToString("D");
        }

        public async Task<RemoteCivilPopulationSnapshot> ReadPopulationAsync(
            Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
        {
            string path = PopulationPath(hiveId);
            SessionIdentity identity = await ResolveAsync(cancellationToken).ConfigureAwait(false);
            RemoteCivilPopulationSnapshot result = await SendAsync<RemoteCivilPopulationSnapshot>(
                new AuthenticatedGameRestRequest("GET", path),
                identity, cancellationToken).ConfigureAwait(false);
            ValidatePopulation(result, identity.PlayerId, hiveId);
            return result;
        }

        public async Task<RemoteCivilCohortResult> StartCohortAsync(
            Guid hiveId, long expectedRevision, string key,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireRevision(expectedRevision);
            RequireKey(key);
            string path = CohortStartPath(hiveId);
            SessionIdentity identity = await ResolveAsync(cancellationToken).ConfigureAwait(false);
            RemoteCivilCohortResult result = await SendAsync<RemoteCivilCohortResult>(
                new AuthenticatedGameRestRequest("POST", path,
                    new CivilCohortStartWireRequest
                    {
                        ExpectedRevision = expectedRevision,
                        IdempotencyKey = key
                    }), identity, cancellationToken).ConfigureAwait(false);
            if (result == null || !result.Succeeded || result.Receipt == null ||
                !string.Equals(result.Code, CohortStartCode, StringComparison.Ordinal))
                throw InvalidResponse("civil_cohort_receipt_invalid");
            ValidatePopulation(result.Snapshot, identity.PlayerId, hiveId);
            if (result.Receipt.CohortId == Guid.Empty ||
                result.Receipt.IdempotencyKey != key ||
                result.Receipt.Code != CohortStartCode ||
                result.Receipt.RevisionBefore != expectedRevision ||
                result.Receipt.RevisionAfter != expectedRevision + 1 ||
                result.Snapshot.HiveRevision < result.Receipt.RevisionAfter ||
                !IsUtc(result.Receipt.AcceptedAtUtc))
                throw InvalidResponse("civil_cohort_receipt_invalid");
            return result;
        }

        public async Task<IReadOnlyList<RemoteFloralSite>> ReadSitesAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            SessionIdentity identity = await ResolveAsync(cancellationToken).ConfigureAwait(false);
            List<RemoteFloralSite> sites = await SendAsync<List<RemoteFloralSite>>(
                new AuthenticatedGameRestRequest("GET", SitesPath),
                identity, cancellationToken).ConfigureAwait(false);
            if (sites == null || sites.Count > 128)
                throw InvalidResponse("invalid_floral_catalog");
            var unique = new HashSet<string>(StringComparer.Ordinal);
            var anchors = new HashSet<string>(StringComparer.Ordinal);
            foreach (RemoteFloralSite site in sites)
            {
                ValidateSite(site);
                if (!unique.Add(site.SiteId))
                    throw InvalidResponse("duplicate_floral_site");
                if (!string.IsNullOrEmpty(site.MapSurfaceId) &&
                    !anchors.Add(site.MapSurfaceId + ":" + site.MapAnchorId))
                    throw InvalidResponse("duplicate_floral_map_anchor");
            }
            return sites;
        }

        public async Task<RemoteFloralSite> ReadSiteAsync(
            string siteId, CancellationToken cancellationToken = default(CancellationToken))
        {
            string path = SitePath(siteId);
            SessionIdentity identity = await ResolveAsync(cancellationToken).ConfigureAwait(false);
            RemoteFloralSite site = await SendAsync<RemoteFloralSite>(
                new AuthenticatedGameRestRequest("GET", path),
                identity, cancellationToken).ConfigureAwait(false);
            ValidateSite(site);
            if (site.SiteId != siteId) throw InvalidResponse("wrong_floral_site");
            return site;
        }

        public async Task<RemoteCivilExpeditionSnapshot> ReadExpeditionAsync(
            Guid hiveId, CancellationToken cancellationToken = default(CancellationToken))
        {
            string path = ExpeditionPath(hiveId);
            SessionIdentity identity = await ResolveAsync(cancellationToken).ConfigureAwait(false);
            RemoteCivilExpeditionSnapshot result = await SendAsync<RemoteCivilExpeditionSnapshot>(
                new AuthenticatedGameRestRequest("GET", path),
                identity, cancellationToken).ConfigureAwait(false);
            ValidateExpedition(result, identity.PlayerId, hiveId);
            return result;
        }

        public async Task<RemoteCivilExpeditionResult> StartExpeditionAsync(
            Guid hiveId, string siteId, long workers,
            long hiveRevision, long siteRevision, string key,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            RequireId(siteId);
            if (workers < 1 || workers > 1000000)
                throw InvalidRequest("civil_workers_out_of_range");
            RequireRevision(hiveRevision);
            RequireRevision(siteRevision);
            RequireKey(key);
            string path = ExpeditionStartPath(hiveId);
            SessionIdentity identity = await ResolveAsync(cancellationToken).ConfigureAwait(false);
            var body = new CivilExpeditionStartWireRequest
            {
                SiteId = siteId,
                Workers = workers,
                ExpectedHiveRevision = hiveRevision,
                ExpectedSiteRevision = siteRevision,
                IdempotencyKey = key,
                Mode = "balanced"
            };
            RemoteCivilExpeditionResult result = await SendAsync<RemoteCivilExpeditionResult>(
                new AuthenticatedGameRestRequest("POST", path, body),
                identity, cancellationToken).ConfigureAwait(false);
            if (result == null || !result.Succeeded ||
                result.Code != ExpeditionStartCode || result.Receipt == null)
                throw InvalidResponse("civil_expedition_receipt_invalid");
            ValidateExpedition(result.Snapshot, identity.PlayerId, hiveId);
            RemoteCivilExpeditionReceipt receipt = result.Receipt;
            if (receipt.Action != "start" || receipt.Code != ExpeditionStartCode ||
                receipt.MissionId == Guid.Empty || receipt.IdempotencyKey != key ||
                receipt.HiveRevisionBefore != hiveRevision ||
                receipt.HiveRevisionAfter != hiveRevision + 1 ||
                receipt.SiteRevisionBefore != siteRevision ||
                receipt.SiteRevisionAfter != siteRevision ||
                !IsUtc(receipt.AcceptedAtUtc) ||
                result.Snapshot.HiveRevision < receipt.HiveRevisionAfter)
                throw InvalidResponse("civil_expedition_receipt_invalid");
            if (result.Snapshot.Active != null &&
                (result.Snapshot.Active.MissionId != receipt.MissionId ||
                 result.Snapshot.Active.SiteId != siteId ||
                 result.Snapshot.Active.Workers != workers))
                throw InvalidResponse("civil_expedition_receipt_mismatch");
            return result;
        }

        public async Task<IReadOnlyList<RemoteFloralAllianceProjectSnapshot>> ReadProjectsAsync(
            Guid allianceId, CancellationToken cancellationToken = default(CancellationToken))
        {
            string path = ProjectsPath(allianceId);
            SessionIdentity identity = await ResolveAsync(cancellationToken).ConfigureAwait(false);
            List<RemoteFloralAllianceProjectSnapshot> result =
                await SendAsync<List<RemoteFloralAllianceProjectSnapshot>>(
                    new AuthenticatedGameRestRequest("GET", path),
                    identity, cancellationToken).ConfigureAwait(false);
            if (result == null || result.Count > 100)
                throw InvalidResponse("invalid_floral_project_list");
            var seen = new HashSet<Guid>();
            foreach (RemoteFloralAllianceProjectSnapshot project in result)
            {
                ValidateProject(project, allianceId);
                if (!seen.Add(project.Project.ProjectId))
                    throw InvalidResponse("duplicate_floral_project");
            }
            return result;
        }

        public async Task<RemoteFloralAllianceProjectSnapshot> ReadProjectAsync(
            Guid allianceId, Guid projectId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string path = ProjectPath(allianceId, projectId);
            SessionIdentity identity = await ResolveAsync(cancellationToken).ConfigureAwait(false);
            RemoteFloralAllianceProjectSnapshot result =
                await SendAsync<RemoteFloralAllianceProjectSnapshot>(
                    new AuthenticatedGameRestRequest("GET", path),
                    identity, cancellationToken).ConfigureAwait(false);
            ValidateProject(result, allianceId);
            if (result.Project.ProjectId != projectId)
                throw InvalidResponse("wrong_floral_project");
            return result;
        }

        public async Task<RemoteFloralProjectCreateResult> CreateProjectAsync(
            Guid allianceId, string siteId, long expectedRevision, string key,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string path = ProjectsPath(allianceId);
            RequireId(siteId);
            RequireRevision(expectedRevision);
            RequireKey(key);
            SessionIdentity identity = await ResolveAsync(cancellationToken).ConfigureAwait(false);
            var body = new CreateFloralProjectWireRequest
            {
                SiteId = siteId,
                ExpectedProgressionRevision = expectedRevision,
                IdempotencyKey = key
            };
            RemoteFloralProjectCreateResult result =
                await SendAsync<RemoteFloralProjectCreateResult>(
                    new AuthenticatedGameRestRequest("POST", path, body),
                    identity, cancellationToken).ConfigureAwait(false);
            if (result == null || !result.Succeeded ||
                result.Code != ProjectCreateCode)
                throw InvalidResponse("floral_project_create_invalid");
            ValidateProject(result.Snapshot, allianceId);
            if (result.Snapshot.Project.SiteId != siteId ||
                result.Snapshot.Project.CreatedByPlayerId != identity.PlayerId ||
                result.Snapshot.Progression.Revision < expectedRevision + 1)
                throw InvalidResponse("floral_project_create_invalid");
            return result;
        }

        public static void ValidatePopulation(
            RemoteCivilPopulationSnapshot x, Guid playerId, Guid hiveId)
        {
            if (x == null || x.PlayerId != playerId || x.HiveId != hiveId ||
                x.ContractVersion != PopulationVersion ||
                !IsUtc(x.ServerTimeUtc) || x.HiveRevision < 0 ||
                x.CivilRevision < 0 || x.CivilRevision > x.HiveRevision ||
                x.Capacity < 0 || x.AvailableWorkers < 0 ||
                x.AssignedWorkers < 0 || x.RecoveringWorkers < 0 ||
                x.AvailableWorkers > long.MaxValue - x.AssignedWorkers ||
                x.AvailableWorkers + x.AssignedWorkers > long.MaxValue - x.RecoveringWorkers)
                throw InvalidResponse("invalid_civil_population");
            if (x.Active != null &&
                (x.Active.CohortId == Guid.Empty ||
                 !IsUtc(x.Active.StartedAtUtc) ||
                 !IsUtc(x.Active.ReadyAtUtc) ||
                 x.Active.ReadyAtUtc <= x.Active.StartedAtUtc ||
                 x.Active.ExpectedWorkers <= 0 ||
                 x.Active.HoneySpent < 0 || x.Active.PollenSpent < 0))
                throw InvalidResponse("invalid_civil_cohort");
            if (x.Status != "available" && x.Status != "incubating" &&
                x.Status != "nursery_missing" && x.Status != "capacity_full" &&
                x.Status != "insufficient_resources")
                throw InvalidResponse("unknown_civil_status");
            if (x.CanStart && x.Status != "available")
                throw InvalidResponse("invalid_civil_start_status");
        }

        public static void ValidateSite(RemoteFloralSite x)
        {
            if (x == null || x.ContractVersion != SiteVersion ||
                !ValidId(x.SiteId) || !ValidId(x.WorldId) ||
                !ValidId(x.RegionId) || !ValidId(x.FamilyId) ||
                !ValidId(x.CatalogVersion) ||
                string.IsNullOrWhiteSpace(x.NameFr) ||
                string.IsNullOrWhiteSpace(x.NameEn) ||
                x.NameFr.Length > 128 || x.NameEn.Length > 128 ||
                x.Revision < 0 || !IsUtc(x.ServerTimeUtc) ||
                x.PollenCapacity <= 0 || x.PollenRemaining < 0 ||
                x.PollenRemaining > x.PollenCapacity ||
                x.FertilityBp < 0 || x.FertilityBp > 10000 ||
                x.BiodiversityBp < 0 || x.BiodiversityBp > 10000 ||
                x.TotalPollinationPoints < 0 ||
                x.DistinctContributors < 0 ||
                x.DistinctContributors > x.TotalPollinationPoints ||
                x.Phase != "blooming" ||
                !ValidMapAnchor(x.MapSurfaceId, x.MapAnchorId))
                throw InvalidResponse("invalid_floral_site");
        }

        public static void ValidateExpedition(
            RemoteCivilExpeditionSnapshot x, Guid playerId, Guid hiveId)
        {
            if (x == null || x.ContractVersion != ExpeditionVersion ||
                x.PlayerId != playerId || x.HiveId != hiveId ||
                x.HiveRevision < 0 || !IsUtc(x.ServerTimeUtc) ||
                x.AvailableWorkers < 0 || x.AssignedWorkers < 0)
                throw InvalidResponse("invalid_civil_expedition");
            if (x.Status != "idle" && x.Status != "outbound" &&
                x.Status != "pollinating" && x.Status != "returning" &&
                x.Status != "awaiting_settlement")
                throw InvalidResponse("unknown_civil_phase");
            RemoteCivilExpeditionMission m = x.Active;
            if (m != null)
            {
                if (m.MissionId == Guid.Empty || !ValidId(m.SiteId) ||
                    !ValidId(m.WorldId) || !ValidId(m.RegionId) ||
                    m.Mode != "balanced" || m.Workers <= 0 ||
                    m.Workers > x.AssignedWorkers ||
                    m.PollenPerWorker <= 0 ||
                    m.FertilityGainBp < 0 || m.FertilityGainBp > 1000 ||
                    m.BiodiversityGainBp < 0 || m.BiodiversityGainBp > 1000 ||
                    !IsUtc(m.StartedAtUtc) ||
                    !IsUtc(m.ArrivesAtUtc) ||
                    !IsUtc(m.WorkEndsAtUtc) ||
                    !IsUtc(m.ReturnsAtUtc) ||
                    m.StartedAtUtc >= m.ArrivesAtUtc ||
                    m.ArrivesAtUtc >= m.WorkEndsAtUtc ||
                    m.WorkEndsAtUtc >= m.ReturnsAtUtc ||
                    (m.AllianceIdAtStart.HasValue &&
                     m.AllianceIdAtStart.Value == Guid.Empty))
                    throw InvalidResponse("invalid_civil_mission");
            }
            if ((m == null) != (x.Status == "idle"))
                throw InvalidResponse("civil_phase_mismatch");
            if (x.LastCompletion != null &&
                (x.LastCompletion.MissionId == Guid.Empty ||
                 !ValidId(x.LastCompletion.SiteId) ||
                 x.LastCompletion.PollenCredited < 0 ||
                 x.LastCompletion.PollinationPoints < 0 ||
                 x.LastCompletion.PollenBalanceAfter < 0 ||
                 !IsUtc(x.LastCompletion.CompletedAtUtc)))
                throw InvalidResponse("invalid_civil_debrief");
        }

        public static void ValidateProject(
            RemoteFloralAllianceProjectSnapshot x, Guid allianceId)
        {
            if (x == null || x.ContractVersion != ProjectVersion ||
                !IsUtc(x.ServerTimeUtc) || x.Project == null || x.Progression == null)
                throw InvalidResponse("invalid_floral_project");
            RemoteFloralAllianceProject p = x.Project;
            RemoteFloralAllianceProgression progression = x.Progression;
            if (allianceId == Guid.Empty || p.AllianceId != allianceId ||
                progression.AllianceId != allianceId || p.ProjectId == Guid.Empty ||
                p.CreatedByPlayerId == Guid.Empty ||
                !ValidId(p.SiteId) || !ValidId(p.WorldId) ||
                !IsUtc(p.CreatedAtUtc) || p.Revision < 0 ||
                p.GoalPoints < 2 || p.RequiredDistinctMembers < 2 ||
                p.MinimumPointsPerMember < 1 || p.Points < 0 ||
                p.QualifiedMembers < 0 ||
                p.QualifiedMembers > p.Points / p.MinimumPointsPerMember ||
                progression.Revision < 0 || progression.Level < 1 ||
                progression.Experience < 0 || progression.CompletedProjects < 0 ||
                (p.Status != "Active" && p.Status != "Completed") ||
                x.IsCompleted != (p.Status == "Completed") ||
                (x.IsCompleted && (!p.CompletedAtUtc.HasValue ||
                    !IsUtc(p.CompletedAtUtc.Value) ||
                    p.QualifiedMembers < p.RequiredDistinctMembers ||
                    p.Points < p.GoalPoints)))
                throw InvalidResponse("invalid_floral_project");
        }

        private async Task<SessionIdentity> ResolveAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!Enabled || !gate.CanSubmitLogin)
                throw new PollinationClientException(
                    PollinationClientError.NotConfigured, "pollination_not_configured");
            GameAccountSession current = null;
            IRefreshableGameAccountSessionSource refreshable =
                sessions as IRefreshableGameAccountSessionSource;
            try
            {
                if (refreshable != null)
                    current = await refreshable.GetFreshSessionAsync(ct).ConfigureAwait(false);
                else
                    sessions.TryGetSession(out current);
            }
            catch (OperationCanceledException) { throw; }
            catch (MobileAccountSessionException ex) { throw MapSessionError(ex); }
            if (current == null || current.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(current.AccessToken) ||
                current.AccessToken.Length > 8192)
                throw new PollinationClientException(
                    PollinationClientError.AuthenticationRequired, "game_session_required");
            return new SessionIdentity(current.PlayerId, current.AccessToken);
        }

        private async Task<T> SendAsync<T>(AuthenticatedGameRestRequest request,
            SessionIdentity identity, CancellationToken ct)
        {
            try
            {
                return await transport.SendAsync<T>(
                    request, identity.Token, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (AuthenticatedGameRestException ex)
            {
                if (ex.Error != AuthenticatedGameRestError.Unauthorized)
                    throw MapTransportError(ex);
            }
            IRefreshableGameAccountSessionSource refreshable =
                sessions as IRefreshableGameAccountSessionSource;
            if (refreshable == null)
                throw new PollinationClientException(
                    PollinationClientError.AuthenticationRequired, "game_session_expired");
            GameAccountSession replacement;
            try
            {
                replacement = await refreshable.RefreshAfterUnauthorizedAsync(
                    identity.Token, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (MobileAccountSessionException ex) { throw MapSessionError(ex); }
            if (replacement == null || replacement.PlayerId != identity.PlayerId ||
                string.IsNullOrWhiteSpace(replacement.AccessToken) ||
                replacement.AccessToken.Length > 8192)
            {
                await refreshable.InvalidateUnauthorizedSessionAsync(
                    identity.Token, ct).ConfigureAwait(false);
                throw InvalidResponse("pollination_identity_changed");
            }
            try
            {
                return await transport.SendAsync<T>(
                    request, replacement.AccessToken, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (AuthenticatedGameRestException ex)
            {
                if (ex.Error == AuthenticatedGameRestError.Unauthorized)
                {
                    await refreshable.InvalidateUnauthorizedSessionAsync(
                        replacement.AccessToken, ct).ConfigureAwait(false);
                    throw new PollinationClientException(
                        PollinationClientError.AuthenticationRequired, "game_session_expired");
                }
                throw MapTransportError(ex);
            }
        }

        private static PollinationClientException MapTransportError(
            AuthenticatedGameRestException ex) =>
            new PollinationClientException(
                ex.Error == AuthenticatedGameRestError.NetworkFailure
                    ? PollinationClientError.NetworkUnavailable
                    : ex.Error == AuthenticatedGameRestError.Unauthorized
                        ? PollinationClientError.AuthenticationRequired
                        : ex.Error == AuthenticatedGameRestError.RemoteRejected
                            ? PollinationClientError.ServerRejected
                            : PollinationClientError.InvalidResponse,
                ex.SafeCode);

        private static PollinationClientException MapSessionError(
            MobileAccountSessionException ex) =>
            new PollinationClientException(
                ex.Error == MobileAccountSessionError.TransportFailure
                    ? PollinationClientError.NetworkUnavailable
                    : PollinationClientError.AuthenticationRequired,
                ex.SafeCode);

        public static bool ValidMapAnchor(string surface, string anchor)
        {
            if (string.IsNullOrEmpty(surface) && string.IsNullOrEmpty(anchor))
                return true;
            if (string.IsNullOrWhiteSpace(surface) || surface.Length > 128 ||
                !ValidId(anchor)) return false;
            foreach (char c in surface)
                if (!(c >= 'a' && c <= 'z') &&
                    !(c >= 'A' && c <= 'Z') &&
                    !(c >= '0' && c <= '9') &&
                    c != '-' && c != '_' && c != '.')
                    return false;
            return true;
        }

        private static string HivePath(Guid hive)
        {
            if (hive == Guid.Empty) throw InvalidRequest("hive_required");
            return "/game/v1/hives/" + hive.ToString("D");
        }
        private static void RequireRevision(long revision)
        {
            if (revision < 0 || revision == long.MaxValue)
                throw InvalidRequest("invalid_revision");
        }
        private static void RequireKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length < 8 || key.Length > 128)
                throw InvalidRequest("invalid_idempotency_key");
            foreach (char c in key)
                if (!(c >= 'a' && c <= 'z') &&
                    !(c >= 'A' && c <= 'Z') &&
                    !(c >= '0' && c <= '9') &&
                    c != '-' && c != '_' && c != '.' && c != ':')
                    throw InvalidRequest("invalid_idempotency_key");
        }
        private static void RequireId(string id)
        {
            if (!ValidId(id)) throw InvalidRequest("invalid_floral_site_id");
        }
        private static bool ValidId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length < 2 || id.Length > 64 ||
                id[0] < 'a' || id[0] > 'z') return false;
            foreach (char c in id)
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') &&
                    c != '.' && c != '-' && c != '_') return false;
            return true;
        }
        private static bool IsUtc(DateTimeOffset at) =>
            at != default(DateTimeOffset) && at.Offset == TimeSpan.Zero;
        private static PollinationClientException InvalidRequest(string code) =>
            new PollinationClientException(PollinationClientError.InvalidRequest, code);
        private static PollinationClientException InvalidResponse(string code) =>
            new PollinationClientException(PollinationClientError.InvalidResponse, code);

        private sealed class SessionIdentity
        {
            public SessionIdentity(Guid id, string token)
            {
                PlayerId = id;
                Token = token;
            }
            public Guid PlayerId { get; }
            public string Token { get; }
        }
    }
}
