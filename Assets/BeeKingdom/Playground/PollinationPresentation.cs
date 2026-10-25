using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    public enum PollinationScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        Mutating = 3,
        Unavailable = 4
    }

    public sealed class PollinationScreenModel
    {
        public PollinationScreenState State { get; internal set; }
            = PollinationScreenState.NotConfigured;
        public IReadOnlyList<RemoteFloralSite> Sites { get; internal set; }
            = Array.Empty<RemoteFloralSite>();
        public RemoteCivilPopulationSnapshot Population { get; internal set; }
        public RemoteCivilExpeditionSnapshot Expedition { get; internal set; }
        public IReadOnlyList<RemoteFloralAllianceProjectSnapshot> Projects { get; internal set; }
            = Array.Empty<RemoteFloralAllianceProjectSnapshot>();
        public string SelectedSiteId { get; internal set; } = string.Empty;
        public long DraftWorkers { get; internal set; } = 1;
        public Guid AllianceId { get; internal set; }
        public bool MayManageAllianceProjects { get; internal set; }
        public bool ProjectServiceAvailable { get; internal set; }
        public bool FloralSitesOnly =>
            Sites != null && Sites.Count > 0 &&
            (Population == null || Expedition == null);
        public string InternalErrorCode { get; internal set; } = string.Empty;

        public RemoteFloralSite SelectedSite =>
            Sites?.FirstOrDefault(x => x.SiteId == SelectedSiteId);

        public bool HasActiveExpedition => Expedition?.Active != null;
        public bool CanStartCohort =>
            State == PollinationScreenState.Ready && Population?.CanStart == true;
        public bool CanLaunch =>
            State == PollinationScreenState.Ready &&
            Population != null && Expedition != null &&
            !HasActiveExpedition && Expedition.Status == "idle" &&
            DraftWorkers > 0 && DraftWorkers <= Population.AvailableWorkers &&
            DraftWorkers <= Expedition.AvailableWorkers &&
            SelectedSite != null && SelectedSite.PublicAccess &&
            SelectedSite.PollenRemaining > 0;
        public bool CanCreateProject =>
            State == PollinationScreenState.Ready &&
            Population != null && Expedition != null &&
            ProjectServiceAvailable &&
            MayManageAllianceProjects && AllianceId != Guid.Empty &&
            SelectedSite != null &&
            Projects != null && !Projects.Any(x =>
                x.Project?.SiteId == SelectedSiteId && !x.IsCompleted);
    }

    public interface IPollinationPanelController
    {
        PollinationScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        void SelectSite(string siteId);
        void AdjustWorkers(long delta);
        void ConfigureAlliance(Guid allianceId, bool mayManage);
        void LaunchSelectedExpedition();
        void StartCivilCohort();
        void CreateSelectedAllianceProject();
    }

    public sealed class UnavailablePollinationPanelController : IPollinationPanelController
    {
        public PollinationScreenModel Model { get; } = new PollinationScreenModel();
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public void SelectSite(string siteId) { }
        public void AdjustWorkers(long delta) { }
        public void ConfigureAlliance(Guid allianceId, bool mayManage) { }
        public void LaunchSelectedExpedition() { }
        public void StartCivilCohort() { }
        public void CreateSelectedAllianceProject() { }
    }

    /// <summary>
    /// Owns a session-scoped read projection only. Every resource, ecological
    /// change and alliance XP is returned by the real server; no optimistic
    /// client credit and no automatic POST retry after uncertain network faults.
    /// During a refresh, previous verified screen values remain stable.
    /// </summary>
    public sealed class PollinationPanelController :
        IPollinationPanelController, IDisposable
    {
        private readonly IPollinationClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;
        // Preserve one original nonce for a pending unconfirmed command. A user
        // can explicitly confirm again; no timer/network error resends a POST.
        private string pendingFingerprint = string.Empty;
        private string pendingNonce = string.Empty;

        public PollinationPanelController(IPollinationClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("Hive required.", nameof(hiveId));
            this.hiveId = hiveId;
            Model = new PollinationScreenModel
            {
                State = client.Enabled
                    ? PollinationScreenState.Loading
                    : PollinationScreenState.NotConfigured
            };
        }

        public PollinationScreenModel Model { get; }
        public bool IsConfigured => !disposed && client.Enabled;
        public bool IsBusy => busy;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
            pendingFingerprint = pendingNonce = string.Empty;
            Model.Sites = Array.Empty<RemoteFloralSite>();
            Model.Projects = Array.Empty<RemoteFloralAllianceProjectSnapshot>();
            Model.Population = null;
            Model.Expedition = null;
            Model.AllianceId = Guid.Empty;
            Model.ProjectServiceAvailable = false;
            Model.State = PollinationScreenState.NotConfigured;
        }

        public void Refresh() => Forget(RefreshCoreAsync());

        public void SelectSite(string siteId)
        {
            if (disposed || busy || Model.Sites == null ||
                !Model.Sites.Any(x => x.SiteId == siteId)) return;
            Model.SelectedSiteId = siteId;
            Model.DraftWorkers = Math.Max(1, Math.Min(
                Model.Population?.AvailableWorkers ?? 1, Model.DraftWorkers));
        }

        public void AdjustWorkers(long delta)
        {
            if (disposed || busy || Model.HasActiveExpedition) return;
            long available = Math.Min(1000000,
                Math.Min(Model.Population?.AvailableWorkers ?? 0,
                    Model.Expedition?.AvailableWorkers ?? 0));
            if (available == 0)
            {
                Model.DraftWorkers = 0;
                return;
            }
            // Avoid long overflow on arbitrary input while preserving UI intent.
            Model.DraftWorkers = delta < 0
                ? Math.Max(1L, Model.DraftWorkers + Math.Max(delta, -Model.DraftWorkers))
                : Math.Min(available,
                    Model.DraftWorkers > long.MaxValue - delta
                        ? available : Model.DraftWorkers + delta);
        }

        public void ConfigureAlliance(Guid allianceId, bool mayManage)
        {
            if (disposed) return;
            if (Model.AllianceId == allianceId &&
                Model.MayManageAllianceProjects == mayManage) return;
            if (Model.AllianceId != allianceId)
            {
                Model.Projects = Array.Empty<RemoteFloralAllianceProjectSnapshot>();
                Model.ProjectServiceAvailable = false;
            }
            Model.AllianceId = allianceId;
            Model.MayManageAllianceProjects = mayManage && allianceId != Guid.Empty;
            // A changed membership must never leave stale project data visible.
            if (!busy && Model.State == PollinationScreenState.Ready)
                Refresh();
        }

        public void LaunchSelectedExpedition()
        {
            if (!disposed && !busy && Model.CanLaunch)
                Forget(LaunchCoreAsync());
        }

        public void StartCivilCohort()
        {
            if (!disposed && !busy && Model.CanStartCohort)
                Forget(StartCohortCoreAsync());
        }

        public void CreateSelectedAllianceProject()
        {
            if (!disposed && !busy && Model.CanCreateProject)
                Forget(CreateProjectCoreAsync());
        }

        private async Task RefreshCoreAsync()
        {
            if (disposed || busy || !client.Enabled) return;
            busy = true;
            bool hadData = Model.State == PollinationScreenState.Ready;
            if (!hadData) Model.State = PollinationScreenState.Loading;
            try
            {
                IReadOnlyList<RemoteFloralSite> sites =
                    await client.ReadSitesAsync(lifetime.Token);
                // During a phased production launch the public floral
                // catalogue can be available while paid civilian operations
                // are still OFF. Treat only the explicit 503 feature gates
                // as a read-only preview; never swallow auth/network faults.
                RemoteCivilPopulationSnapshot population = null;
                RemoteCivilExpeditionSnapshot expedition = null;
                try
                {
                    population = await client.ReadPopulationAsync(
                        hiveId, lifetime.Token);
                }
                catch (PollinationClientException ex) when (
                    ex.Error == PollinationClientError.ServerRejected &&
                    ex.SafeCode == "game.civil_population_disabled")
                {
                    // No civil population is invented or cached.
                }
                try
                {
                    expedition = await client.ReadExpeditionAsync(
                        hiveId, lifetime.Token);
                }
                catch (PollinationClientException ex) when (
                    ex.Error == PollinationClientError.ServerRejected &&
                    ex.SafeCode == "game.civil_expeditions_disabled")
                {
                    // No mission, reward or worker state is invented.
                }
                if (disposed) return;
                IReadOnlyList<RemoteFloralAllianceProjectSnapshot> projects =
                    Array.Empty<RemoteFloralAllianceProjectSnapshot>();
                Guid allianceAtRequest = Model.AllianceId;
                bool projectServiceAvailable = false;
                if (allianceAtRequest != Guid.Empty)
                {
                    try
                    {
                        projects = await client.ReadProjectsAsync(
                            allianceAtRequest, lifetime.Token);
                        projectServiceAvailable = true;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (PollinationClientException)
                    {
                        // Project subsystem may have a separate rollout flag.
                        // Do not block the public civil field view.
                    }
                }
                if (disposed) return;
                Model.Sites = sites;
                Model.Population = population;
                Model.Expedition = expedition;
                Model.Projects = allianceAtRequest == Model.AllianceId
                    ? projects : Array.Empty<RemoteFloralAllianceProjectSnapshot>();
                Model.ProjectServiceAvailable =
                    allianceAtRequest == Model.AllianceId && projectServiceAvailable;
                if (Model.SelectedSite == null)
                    Model.SelectedSiteId = sites.Count > 0 ? sites[0].SiteId : string.Empty;
                if (Model.DraftWorkers < 1) Model.DraftWorkers = 1;
                Model.DraftWorkers = population != null && expedition != null
                    ? Math.Min(Math.Min(population.AvailableWorkers,
                        expedition.AvailableWorkers), Model.DraftWorkers)
                    : 0;
                Model.InternalErrorCode = string.Empty;
                Model.State = PollinationScreenState.Ready;
                if (expedition?.Active != null &&
                    !string.IsNullOrEmpty(pendingFingerprint))
                    ClearPending();
            }
            catch (OperationCanceledException) { }
            catch (PollinationClientException ex)
            {
                if (!disposed)
                {
                    Model.InternalErrorCode = ex.SafeCode;
                    Model.State = hadData
                        ? PollinationScreenState.Ready : PollinationScreenState.Unavailable;
                }
            }
            catch (Exception)
            {
                if (!disposed)
                {
                    Model.InternalErrorCode = "temporarily_unavailable";
                    Model.State = hadData
                        ? PollinationScreenState.Ready : PollinationScreenState.Unavailable;
                }
            }
            finally { busy = false; }
        }

        private async Task LaunchCoreAsync()
        {
            string siteId = Model.SelectedSiteId;
            long count = Model.DraftWorkers;
            long hiveRevision = Math.Max(Model.Expedition.HiveRevision,
                Model.Population.HiveRevision);
            long siteRevision = Model.SelectedSite.Revision;
            string fingerprint = "flight|" + hiveId.ToString("N") + "|" + siteId
                + "|" + count + "|" + hiveRevision + "|" + siteRevision;
            string key = PendingKey(fingerprint);
            await MutateAsync(async () =>
            {
                RemoteCivilExpeditionResult result =
                    await client.StartExpeditionAsync(hiveId, siteId, count,
                        hiveRevision, siteRevision, key, lifetime.Token)
                        ;
                Model.Expedition = result.Snapshot;
                ClearPending();
            });
        }

        private async Task StartCohortCoreAsync()
        {
            long revision = Math.Max(Model.Expedition?.HiveRevision ?? 0,
                Model.Population.HiveRevision);
            string fingerprint = "cohort|" + hiveId.ToString("N") + "|" + revision;
            string key = PendingKey(fingerprint);
            await MutateAsync(async () =>
            {
                RemoteCivilCohortResult result = await client.StartCohortAsync(
                    hiveId, revision, key, lifetime.Token);
                Model.Population = result.Snapshot;
                ClearPending();
            });
        }

        private async Task CreateProjectCoreAsync()
        {
            Guid alliance = Model.AllianceId;
            string site = Model.SelectedSiteId;
            long revision = Model.Projects.Count > 0
                ? Model.Projects.Max(x => x.Progression.Revision) : 0;
            string fingerprint = "project|" + alliance.ToString("N") + "|"
                + site + "|" + revision;
            string key = PendingKey(fingerprint);
            await MutateAsync(async () =>
            {
                RemoteFloralProjectCreateResult result =
                    await client.CreateProjectAsync(alliance, site, revision,
                        key, lifetime.Token);
                if (result.Snapshot != null && Model.AllianceId == alliance)
                {
                    var revised = Model.Projects.Where(
                        x => x.Project.ProjectId != result.Snapshot.Project.ProjectId)
                        .ToList();
                    revised.Add(result.Snapshot);
                    Model.Projects = revised;
                }
                ClearPending();
            });
        }

        private async Task MutateAsync(Func<Task> operation)
        {
            if (disposed || busy) return;
            busy = true;
            Model.State = PollinationScreenState.Mutating;
            try
            {
                await operation();
                if (!disposed)
                {
                    Model.InternalErrorCode = string.Empty;
                    Model.State = PollinationScreenState.Ready;
                }
            }
            catch (OperationCanceledException) { }
            catch (PollinationClientException ex)
            {
                if (!disposed)
                {
                    // No visible technical synchronization error or fake reward.
                    Model.InternalErrorCode = ex.SafeCode;
                    Model.State = PollinationScreenState.Ready;
                }
            }
            catch (Exception)
            {
                if (!disposed)
                {
                    Model.InternalErrorCode = "temporarily_unavailable";
                    Model.State = PollinationScreenState.Ready;
                }
            }
            finally
            {
                busy = false;
                if (!disposed) Refresh();
            }
        }

        private string PendingKey(string fingerprint)
        {
            if (pendingFingerprint == fingerprint && pendingNonce.Length > 0)
                return pendingNonce;
            pendingFingerprint = fingerprint;
            pendingNonce = Guid.NewGuid().ToString("N");
            return pendingNonce;
        }

        private void ClearPending()
        {
            pendingFingerprint = string.Empty;
            pendingNonce = string.Empty;
        }

        private static async void Forget(Task operation)
        {
            try { await operation; }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }
    }
}
