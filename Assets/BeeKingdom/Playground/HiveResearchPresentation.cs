using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    public enum HiveResearchScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        OfflineReadOnly = 3,
        Starting = 4,
        Completing = 5,
        Error = 6
    }

    public sealed class HiveResearchOfferModel
    {
        internal HiveResearchOfferModel(RemoteHiveResearchOffer source)
        {
            ResearchId = source.ResearchId ?? string.Empty;
            Duration = source.Duration;
            Costs = new Dictionary<string, long>(source.Costs, StringComparer.Ordinal);
            HoneyProductionBonusBps = source.Effects.HoneyProductionBonusBps;
            WaxCapacityBonusBps = source.Effects.WaxCapacityBonusBps;
            WaxProductionBonusBps = source.Effects.WaxProductionBonusBps;
            PollenProductionBonusBps = source.Effects.PollenProductionBonusBps;
            PollenCapacityBonusBps = source.Effects.PollenCapacityBonusBps;
            GlobalCapacityBonusBps = source.Effects.GlobalCapacityBonusBps;
            Prerequisites = source.Prerequisites == null
                ? Array.Empty<string>()
                : new List<string>(source.Prerequisites);
        }

        public string ResearchId { get; }
        public TimeSpan Duration { get; }
        public IReadOnlyDictionary<string, long> Costs { get; }
        public int HoneyProductionBonusBps { get; }
        public int WaxCapacityBonusBps { get; }
        public int WaxProductionBonusBps { get; }
        public int PollenProductionBonusBps { get; }
        public int PollenCapacityBonusBps { get; }
        public int GlobalCapacityBonusBps { get; }
        public IReadOnlyList<string> Prerequisites { get; }
    }

    public sealed class HiveResearchCompletionModel
    {
        internal HiveResearchCompletionModel(RemoteHiveResearchCompletion source)
        {
            ResearchId = source.ResearchId ?? string.Empty;
            CompletedAtUtc = source.CompletedAtUtc;
            HoneyProductionBonusBps = source.Effects.HoneyProductionBonusBps;
            WaxCapacityBonusBps = source.Effects.WaxCapacityBonusBps;
            WaxProductionBonusBps = source.Effects.WaxProductionBonusBps;
            PollenProductionBonusBps = source.Effects.PollenProductionBonusBps;
            PollenCapacityBonusBps = source.Effects.PollenCapacityBonusBps;
            GlobalCapacityBonusBps = source.Effects.GlobalCapacityBonusBps;
        }

        public string ResearchId { get; }
        public DateTimeOffset CompletedAtUtc { get; }
        public int HoneyProductionBonusBps { get; }
        public int WaxCapacityBonusBps { get; }
        public int WaxProductionBonusBps { get; }
        public int PollenProductionBonusBps { get; }
        public int PollenCapacityBonusBps { get; }
        public int GlobalCapacityBonusBps { get; }
    }

    public sealed class HiveResearchOperationModel
    {
        internal HiveResearchOperationModel(RemoteHiveResearchOperation source)
        {
            OperationId = source.OperationId;
            ResearchId = source.ResearchId ?? string.Empty;
            StartedAtUtc = source.StartedAtUtc;
            CompletesAtUtc = source.CompletesAtUtc;
            Status = source.Status ?? string.Empty;
        }

        public Guid OperationId { get; }
        public string ResearchId { get; }
        public DateTimeOffset StartedAtUtc { get; }
        public DateTimeOffset CompletesAtUtc { get; }
        public string Status { get; }
        public bool IsAwaitingCompletion =>
            string.Equals(Status, HiveResearchClient.AwaitingCompletionStatus, StringComparison.Ordinal);
    }

    public sealed class HiveResearchScreenModel
    {
        internal HiveResearchScreenModel(
            HiveResearchScreenState state,
            string errorCode,
            string retrySignature,
            long revision,
            DateTimeOffset serverTimeUtc,
            TimeSpan projectedAt,
            IReadOnlyDictionary<string, RemoteHiveResearchBalance> balances,
            IReadOnlyList<HiveResearchCompletionModel> completed,
            IReadOnlyList<HiveResearchOfferModel> offers,
            HiveResearchOperationModel activeOperation,
            DateTimeOffset cachedAtUtc,
            string mutatingResearchId = null)
        {
            State = state;
            ErrorCode = errorCode ?? string.Empty;
            RetrySignature = retrySignature ?? string.Empty;
            Revision = Math.Max(0L, revision);
            ServerTimeUtc = serverTimeUtc;
            ProjectedAt = projectedAt < TimeSpan.Zero ? TimeSpan.Zero : projectedAt;
            Balances = balances ?? new Dictionary<string, RemoteHiveResearchBalance>();
            Completed = completed ?? Array.Empty<HiveResearchCompletionModel>();
            Offers = offers ?? Array.Empty<HiveResearchOfferModel>();
            ActiveOperation = activeOperation;
            CachedAtUtc = cachedAtUtc;
            MutatingResearchId = mutatingResearchId ?? string.Empty;
        }

        public HiveResearchScreenState State { get; }
        public string ErrorCode { get; }
        public string RetrySignature { get; }
        public long Revision { get; }
        public DateTimeOffset ServerTimeUtc { get; }
        public TimeSpan ProjectedAt { get; }
        public IReadOnlyDictionary<string, RemoteHiveResearchBalance> Balances { get; }
        public IReadOnlyList<HiveResearchCompletionModel> Completed { get; }
        public IReadOnlyList<HiveResearchOfferModel> Offers { get; }
        public HiveResearchOperationModel ActiveOperation { get; }
        public DateTimeOffset CachedAtUtc { get; }
        // Nom exact de l'etude en cours de demarrage/validation (demande de Jeff,
        // 2026-07-31) : sans ca, la liste entiere d'offres clignotait vers un texte
        // generique "Demarrage..." pendant qu'une seule etude mutait.
        public string MutatingResearchId { get; }
        public bool IsReadOnly => State == HiveResearchScreenState.OfflineReadOnly;

        public bool IsMutating(string researchId)
        {
            return (State == HiveResearchScreenState.Starting || State == HiveResearchScreenState.Completing) &&
                string.Equals(MutatingResearchId, researchId ?? string.Empty, StringComparison.Ordinal);
        }

        public bool IsCompleted(string researchId)
        {
            return Completed.Any(item => string.Equals(item.ResearchId, researchId, StringComparison.Ordinal));
        }

        public HiveResearchOfferModel OfferFor(string researchId)
        {
            return Offers.SingleOrDefault(item =>
                string.Equals(item.ResearchId, researchId, StringComparison.Ordinal));
        }

        public long BalanceFor(string resourceKey)
        {
            RemoteHiveResearchBalance value;
            return Balances.TryGetValue(resourceKey ?? string.Empty, out value) && value != null
                ? value.Amount
                : 0L;
        }

        public string ShortageResource(string researchId)
        {
            HiveResearchOfferModel offer = OfferFor(researchId);
            if (offer == null || ActiveOperation != null || IsCompleted(researchId)) return string.Empty;
            foreach (KeyValuePair<string, long> cost in offer.Costs.OrderBy(item => item.Key, StringComparer.Ordinal))
                if (BalanceFor(cost.Key) < cost.Value) return cost.Key;
            return string.Empty;
        }

        public long MissingAmount(string researchId, string resourceKey)
        {
            HiveResearchOfferModel offer = OfferFor(researchId);
            long cost;
            if (offer == null || !offer.Costs.TryGetValue(resourceKey ?? string.Empty, out cost)) return 0L;
            return Math.Max(0L, cost - BalanceFor(resourceKey));
        }

        public bool PrerequisitesMet(string researchId)
        {
            HiveResearchOfferModel offer = OfferFor(researchId);
            if (offer == null) return true;
            foreach (string prerequisite in offer.Prerequisites)
                if (!IsCompleted(prerequisite)) return false;
            return true;
        }

        public string MissingPrerequisite(string researchId)
        {
            HiveResearchOfferModel offer = OfferFor(researchId);
            if (offer == null) return string.Empty;
            foreach (string prerequisite in offer.Prerequisites)
                if (!IsCompleted(prerequisite)) return prerequisite;
            return string.Empty;
        }

        public bool CanStart(string researchId)
        {
            bool retry = State == HiveResearchScreenState.Error &&
                string.Equals(ErrorCode, "network_unavailable", StringComparison.Ordinal) &&
                RetrySignature.StartsWith("start|" + (researchId ?? string.Empty) + "|", StringComparison.Ordinal);
            if ((State != HiveResearchScreenState.Ready && !retry) || ActiveOperation != null ||
                IsCompleted(researchId) || !PrerequisitesMet(researchId)) return false;
            HiveResearchOfferModel offer = OfferFor(researchId);
            if (offer == null) return false;
            foreach (KeyValuePair<string, long> cost in offer.Costs)
                if (BalanceFor(cost.Key) < cost.Value) return false;
            return true;
        }

        public bool CanComplete(string researchId)
        {
            bool retry = State == HiveResearchScreenState.Error &&
                string.Equals(ErrorCode, "network_unavailable", StringComparison.Ordinal) &&
                ActiveOperation != null && RetrySignature.StartsWith(
                    "complete|" + ActiveOperation.OperationId.ToString("D") + "|", StringComparison.Ordinal);
            return (State == HiveResearchScreenState.Ready || retry) && ActiveOperation != null &&
                ActiveOperation.IsAwaitingCompletion &&
                string.Equals(ActiveOperation.ResearchId, researchId, StringComparison.Ordinal);
        }

        public TimeSpan Remaining(TimeSpan currentElapsed)
        {
            if (ActiveOperation == null || ServerTimeUtc == default(DateTimeOffset)) return TimeSpan.Zero;
            TimeSpan delta = currentElapsed - ProjectedAt;
            if (delta < TimeSpan.Zero) delta = TimeSpan.Zero;
            TimeSpan remaining = ActiveOperation.CompletesAtUtc - (ServerTimeUtc + delta);
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        public double Progress01(TimeSpan currentElapsed)
        {
            if (ActiveOperation == null) return 0d;
            TimeSpan total = ActiveOperation.CompletesAtUtc - ActiveOperation.StartedAtUtc;
            if (total <= TimeSpan.Zero) return 0d;
            return Math.Max(0d, Math.Min(1d, 1d - Remaining(currentElapsed).TotalSeconds / total.TotalSeconds));
        }
    }

    public static class HiveResearchPresentation
    {
        public static HiveResearchScreenModel NotConfigured()
        {
            return Empty(HiveResearchScreenState.NotConfigured, string.Empty);
        }

        public static HiveResearchScreenModel Loading(RemoteHiveResearchSnapshot snapshot, TimeSpan projectedAt)
        {
            return Project(HiveResearchScreenState.Loading, snapshot, projectedAt, string.Empty, string.Empty,
                default(DateTimeOffset));
        }

        public static HiveResearchScreenModel Ready(RemoteHiveResearchSnapshot snapshot, TimeSpan projectedAt)
        {
            return Project(HiveResearchScreenState.Ready, snapshot, projectedAt, string.Empty, string.Empty,
                default(DateTimeOffset));
        }

        public static HiveResearchScreenModel OfflineReadOnly(
            RemoteHiveResearchSnapshot snapshot,
            TimeSpan projectedAt,
            DateTimeOffset cachedAtUtc)
        {
            return Project(HiveResearchScreenState.OfflineReadOnly, snapshot, projectedAt, string.Empty,
                string.Empty, cachedAtUtc);
        }

        public static HiveResearchScreenModel Mutating(
            HiveResearchScreenState state,
            RemoteHiveResearchSnapshot snapshot,
            TimeSpan projectedAt,
            string mutatingResearchId)
        {
            if (state != HiveResearchScreenState.Starting && state != HiveResearchScreenState.Completing)
                throw new ArgumentOutOfRangeException(nameof(state));
            return Project(state, snapshot, projectedAt, string.Empty, string.Empty, default(DateTimeOffset),
                mutatingResearchId);
        }

        public static HiveResearchScreenModel Error(
            RemoteHiveResearchSnapshot snapshot,
            TimeSpan projectedAt,
            string stableCode,
            string retrySignature)
        {
            return Project(HiveResearchScreenState.Error, snapshot, projectedAt, stableCode,
                retrySignature, default(DateTimeOffset));
        }

        private static HiveResearchScreenModel Project(
            HiveResearchScreenState state,
            RemoteHiveResearchSnapshot snapshot,
            TimeSpan projectedAt,
            string errorCode,
            string retrySignature,
            DateTimeOffset cachedAtUtc,
            string mutatingResearchId = null)
        {
            if (snapshot == null) return Empty(state, errorCode);
            var balances = snapshot.Balances.ToDictionary(
                item => item.Key,
                item => new RemoteHiveResearchBalance
                {
                    Amount = item.Value.Amount,
                    Capacity = item.Value.Capacity
                },
                StringComparer.Ordinal);
            HiveResearchCompletionModel[] completed = snapshot.Completed
                .Select(item => new HiveResearchCompletionModel(item)).ToArray();
            HiveResearchOfferModel[] offers = snapshot.Offers
                .Select(item => new HiveResearchOfferModel(item)).ToArray();
            HiveResearchOperationModel operation = snapshot.ActiveOperation == null
                ? null
                : new HiveResearchOperationModel(snapshot.ActiveOperation);
            return new HiveResearchScreenModel(state, errorCode, retrySignature, snapshot.Revision,
                snapshot.ServerTimeUtc, projectedAt, balances, completed, offers, operation, cachedAtUtc,
                mutatingResearchId);
        }

        private static HiveResearchScreenModel Empty(HiveResearchScreenState state, string errorCode)
        {
            return new HiveResearchScreenModel(state, errorCode, string.Empty, 0L,
                default(DateTimeOffset), TimeSpan.Zero,
                new Dictionary<string, RemoteHiveResearchBalance>(), Array.Empty<HiveResearchCompletionModel>(),
                Array.Empty<HiveResearchOfferModel>(), null, default(DateTimeOffset));
        }
    }

    public interface IHiveResearchMonotonicClock
    {
        TimeSpan Elapsed { get; }
    }

    public sealed class StopwatchHiveResearchClock : IHiveResearchMonotonicClock
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        public TimeSpan Elapsed => stopwatch.Elapsed;
    }

    public interface IHiveResearchPanelController
    {
        HiveResearchScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        TimeSpan Elapsed { get; }
        void Refresh();
        void Start(string researchId);
        void Complete();
    }

    public sealed class UnavailableHiveResearchPanelController : IHiveResearchPanelController
    {
        private readonly HiveResearchScreenModel model = HiveResearchPresentation.NotConfigured();
        public HiveResearchScreenModel Model => model;
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public TimeSpan Elapsed => TimeSpan.Zero;
        public void Refresh() { }
        public void Start(string researchId) { }
        public void Complete() { }
    }

    public interface IHiveResearchMutationKeySource
    {
        string Create(string operation);
    }

    public sealed class SessionHiveResearchMutationKeySource : IHiveResearchMutationKeySource
    {
        public string Create(string operation)
        {
            string safe = string.IsNullOrWhiteSpace(operation) ? "mutation" : operation.Trim();
            return "mobile-research-" + safe + "-" + Guid.NewGuid().ToString("N");
        }
    }

    public sealed class HiveResearchPanelController : IHiveResearchPanelController, IDisposable
    {
        private readonly IHiveResearchClient client;
        private readonly Guid hiveId;
        private readonly IHiveResearchMutationKeySource keySource;
        private readonly IHiveResearchMonotonicClock clock;
        private readonly Dictionary<string, string> pendingKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private RemoteHiveResearchSnapshot snapshot;
        private bool disposed;
        private bool busy;

        public HiveResearchPanelController(
            IHiveResearchClient client,
            Guid hiveId,
            IHiveResearchMutationKeySource keySource = null,
            IHiveResearchMonotonicClock clock = null)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            this.keySource = keySource ?? new SessionHiveResearchMutationKeySource();
            this.clock = clock ?? new StopwatchHiveResearchClock();
            Model = HiveResearchPresentation.Loading(null, this.clock.Elapsed);
        }

        public HiveResearchScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;
        public TimeSpan Elapsed => clock.Elapsed;

        public void Refresh() { RefreshInsideLifetime(); }
        public void Start(string researchId) { StartInsideLifetime(researchId); }
        public void Complete() { CompleteInsideLifetime(); }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
            pendingKeys.Clear();
        }

        public Task RefreshForProofAsync() { return RefreshCoreAsync(); }
        public Task StartForProofAsync(string researchId) { return StartCoreAsync(researchId); }
        public Task CompleteForProofAsync() { return CompleteCoreAsync(); }

        private async void RefreshInsideLifetime() { await RefreshCoreAsync(); }
        private async void StartInsideLifetime(string researchId) { await StartCoreAsync(researchId); }
        private async void CompleteInsideLifetime() { await CompleteCoreAsync(); }

        private async Task RefreshCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            Model = HiveResearchPresentation.Loading(snapshot, clock.Elapsed);
            try
            {
                RemoteHiveResearchSnapshot result = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                long previousRevision = snapshot == null ? -1L : snapshot.Revision;
                snapshot = result;
                if (previousRevision >= 0L && previousRevision != snapshot.Revision) pendingKeys.Clear();
                Model = client.LastReadSource == GameReadSource.ProtectedCache
                    ? HiveResearchPresentation.OfflineReadOnly(snapshot, clock.Elapsed, client.LastReadCachedAtUtc)
                    : HiveResearchPresentation.Ready(snapshot, clock.Elapsed);
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Model = HiveResearchPresentation.Error(snapshot, clock.Elapsed, "cancelled", string.Empty);
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed) Model = HiveResearchPresentation.Error(snapshot, clock.Elapsed, StableError(error), string.Empty);
            }
            catch (Exception)
            {
                if (!disposed) Model = HiveResearchPresentation.Error(snapshot, clock.Elapsed, "unexpected", string.Empty);
            }
            finally { busy = false; }
        }

        private async Task StartCoreAsync(string researchId)
        {
            HiveResearchScreenModel current = Model;
            if (busy || disposed || snapshot == null || current == null || !current.CanStart(researchId)) return;
            string signature = "start|" + researchId + "|" + current.Revision.ToString(CultureInfo.InvariantCulture);
            string key = MutationKey(signature, "start-" + researchId);
            busy = true;
            Model = HiveResearchPresentation.Mutating(HiveResearchScreenState.Starting, snapshot, clock.Elapsed, researchId);
            try
            {
                RemoteHiveResearchMutationResponse response = await client.StartAsync(
                    hiveId, researchId, current.Revision, key, lifetime.Token);
                if (disposed) return;
                snapshot = response.Snapshot;
                pendingKeys.Remove(signature);
                Model = HiveResearchPresentation.Ready(snapshot, clock.Elapsed);
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Model = HiveResearchPresentation.Error(snapshot, clock.Elapsed, "cancelled", string.Empty);
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed)
                {
                    string code = StableError(error);
                    Model = HiveResearchPresentation.Error(snapshot, clock.Elapsed, code,
                        code == "network_unavailable" ? signature : string.Empty);
                }
            }
            catch (Exception)
            {
                if (!disposed) Model = HiveResearchPresentation.Error(snapshot, clock.Elapsed, "unexpected", string.Empty);
            }
            finally { busy = false; }
        }

        private async Task CompleteCoreAsync()
        {
            HiveResearchScreenModel current = Model;
            if (busy || disposed || snapshot == null || current == null || current.ActiveOperation == null ||
                !current.CanComplete(current.ActiveOperation.ResearchId)) return;
            Guid operationId = current.ActiveOperation.OperationId;
            string signature = "complete|" + operationId.ToString("D") + "|" +
                current.Revision.ToString(CultureInfo.InvariantCulture);
            string key = MutationKey(signature, "complete-" + operationId.ToString("N"));
            busy = true;
            Model = HiveResearchPresentation.Mutating(HiveResearchScreenState.Completing, snapshot, clock.Elapsed,
                current.ActiveOperation.ResearchId);
            try
            {
                RemoteHiveResearchMutationResponse response = await client.CompleteAsync(
                    hiveId, operationId, current.Revision, key, lifetime.Token);
                if (disposed) return;
                snapshot = response.Snapshot;
                pendingKeys.Remove(signature);
                Model = HiveResearchPresentation.Ready(snapshot, clock.Elapsed);
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Model = HiveResearchPresentation.Error(snapshot, clock.Elapsed, "cancelled", string.Empty);
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed)
                {
                    string code = StableError(error);
                    Model = HiveResearchPresentation.Error(snapshot, clock.Elapsed, code,
                        code == "network_unavailable" ? signature : string.Empty);
                }
            }
            catch (Exception)
            {
                if (!disposed) Model = HiveResearchPresentation.Error(snapshot, clock.Elapsed, "unexpected", string.Empty);
            }
            finally { busy = false; }
        }

        private string MutationKey(string signature, string operation)
        {
            string value;
            if (!pendingKeys.TryGetValue(signature, out value))
            {
                value = keySource.Create(operation);
                pendingKeys[signature] = value;
            }
            return value;
        }

        private static string StableError(HivePerimeterClientException error)
        {
            if (error.Error == HivePerimeterClientError.InvalidResponse)
            {
                switch (error.Message)
                {
                    case "game.revision_conflict": return "revision_conflict";
                    case "game.research_busy": return "research_busy";
                    case "game.research_prerequisite_missing": return "prerequisite_missing";
                    case "game.insufficient_resources": return "insufficient_resources";
                    case "game.research_already_completed": return "already_completed";
                    case "game.research_not_ready": return "not_ready";
                    case "game.research_not_found": return "operation_not_found";
                    case "game.idempotency_conflict": return "idempotency_conflict";
                    case "game.unavailable": return "server_unavailable";
                }
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
