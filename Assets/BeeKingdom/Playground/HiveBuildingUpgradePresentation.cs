using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Gameplay.Events;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    public enum HiveBuildingUpgradeScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        OfflineReadOnly = 3,
        Starting = 4,
        Completing = 5,
        Error = 6
    }

    public sealed class HiveBuildingUpgradeOfferModel
    {
        internal HiveBuildingUpgradeOfferModel(RemoteBuildingUpgradeOffer source)
        {
            BuildingKey = source.BuildingKey ?? string.Empty;
            FromLevel = source.FromLevel;
            ToLevel = source.ToLevel;
            Duration = source.Duration;
            Costs = new Dictionary<string, long>(source.Costs, StringComparer.Ordinal);
        }

        public string BuildingKey { get; }
        public int FromLevel { get; }
        public int ToLevel { get; }
        public TimeSpan Duration { get; }
        public IReadOnlyDictionary<string, long> Costs { get; }
    }

    public sealed class HiveBuildingUpgradeOperationModel
    {
        internal HiveBuildingUpgradeOperationModel(RemoteBuildingUpgradeOperation source)
        {
            OperationId = source.OperationId;
            BuildingKey = source.BuildingKey ?? string.Empty;
            FromLevel = source.FromLevel;
            ToLevel = source.ToLevel;
            StartedAtUtc = source.StartedAtUtc;
            CompletesAtUtc = source.CompletesAtUtc;
            Status = source.Status ?? string.Empty;
        }

        public Guid OperationId { get; }
        public string BuildingKey { get; }
        public int FromLevel { get; }
        public int ToLevel { get; }
        public DateTimeOffset StartedAtUtc { get; }
        public DateTimeOffset CompletesAtUtc { get; }
        public string Status { get; }
        public bool IsAwaitingCompletion =>
            string.Equals(Status, HiveBuildingUpgradeClient.AwaitingCompletionStatus, StringComparison.Ordinal);
    }

    public sealed class HiveBuildingUpgradeScreenModel
    {
        internal HiveBuildingUpgradeScreenModel(
            HiveBuildingUpgradeScreenState state,
            string errorCode,
            string retrySignature,
            string mutatingBuildingKey,
            long revision,
            DateTimeOffset serverTimeUtc,
            TimeSpan projectedAt,
            IReadOnlyDictionary<string, RemoteBuildingUpgradeBalance> balances,
            IReadOnlyDictionary<string, int> buildingLevels,
            IReadOnlyList<HiveBuildingUpgradeOfferModel> offers,
            HiveBuildingUpgradeOperationModel activeOperation,
            DateTimeOffset cachedAtUtc)
        {
            State = state;
            ErrorCode = errorCode ?? string.Empty;
            RetrySignature = retrySignature ?? string.Empty;
            MutatingBuildingKey = mutatingBuildingKey ?? string.Empty;
            Revision = Math.Max(0L, revision);
            ServerTimeUtc = serverTimeUtc;
            ProjectedAt = projectedAt < TimeSpan.Zero ? TimeSpan.Zero : projectedAt;
            Balances = balances ?? new Dictionary<string, RemoteBuildingUpgradeBalance>();
            BuildingLevels = buildingLevels ?? new Dictionary<string, int>();
            Offers = offers ?? Array.Empty<HiveBuildingUpgradeOfferModel>();
            ActiveOperation = activeOperation;
            CachedAtUtc = cachedAtUtc;
        }

        public HiveBuildingUpgradeScreenState State { get; }
        public string ErrorCode { get; }
        public string RetrySignature { get; }
        // Seul CE batiment doit afficher "le serveur reserve/valide..." pendant le bref instant du
        // clic Demarrer/Terminer - meme raison que CollectingBuildingKey cote production : State
        // est partage par les 3 batiments, mais la mutation ne concerne qu'un seul d'entre eux.
        public string MutatingBuildingKey { get; }
        public long Revision { get; }
        public DateTimeOffset ServerTimeUtc { get; }
        public TimeSpan ProjectedAt { get; }
        public IReadOnlyDictionary<string, RemoteBuildingUpgradeBalance> Balances { get; }
        public IReadOnlyDictionary<string, int> BuildingLevels { get; }
        public IReadOnlyList<HiveBuildingUpgradeOfferModel> Offers { get; }
        public HiveBuildingUpgradeOperationModel ActiveOperation { get; }
        public DateTimeOffset CachedAtUtc { get; }
        public bool IsReadOnly => State == HiveBuildingUpgradeScreenState.OfflineReadOnly;

        public int LevelFor(string buildingKey)
        {
            int value;
            return BuildingLevels.TryGetValue(buildingKey ?? string.Empty, out value) ? value : 0;
        }

        public HiveBuildingUpgradeOfferModel OfferFor(string buildingKey)
        {
            return Offers.SingleOrDefault(offer =>
                string.Equals(offer.BuildingKey, buildingKey, StringComparison.Ordinal));
        }

        public bool CanStart(string buildingKey)
        {
            bool retry = State == HiveBuildingUpgradeScreenState.Error &&
                string.Equals(ErrorCode, "network_unavailable", StringComparison.Ordinal) &&
                RetrySignature.StartsWith("start|" + (buildingKey ?? string.Empty) + "|", StringComparison.Ordinal);
            if ((State != HiveBuildingUpgradeScreenState.Ready && !retry) || ActiveOperation != null) return false;
            HiveBuildingUpgradeOfferModel offer = OfferFor(buildingKey);
            if (offer == null) return false;
            foreach (KeyValuePair<string, long> cost in offer.Costs)
            {
                RemoteBuildingUpgradeBalance balance;
                if (!Balances.TryGetValue(cost.Key, out balance) || balance == null || balance.Amount < cost.Value)
                    return false;
            }
            return true;
        }

        public bool CanComplete(string buildingKey)
        {
            bool retry = State == HiveBuildingUpgradeScreenState.Error &&
                string.Equals(ErrorCode, "network_unavailable", StringComparison.Ordinal) &&
                ActiveOperation != null && RetrySignature.StartsWith(
                    "complete|" + ActiveOperation.OperationId.ToString("D") + "|", StringComparison.Ordinal);
            return (State == HiveBuildingUpgradeScreenState.Ready || retry) && ActiveOperation != null &&
                ActiveOperation.IsAwaitingCompletion &&
                string.Equals(ActiveOperation.BuildingKey, buildingKey, StringComparison.Ordinal);
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

    public static class HiveBuildingUpgradePresentation
    {
        public static HiveBuildingUpgradeScreenModel NotConfigured()
        {
            return Empty(HiveBuildingUpgradeScreenState.NotConfigured, string.Empty);
        }

        public static HiveBuildingUpgradeScreenModel Loading(
            RemoteBuildingUpgradeSnapshot snapshot,
            TimeSpan projectedAt)
        {
            return Project(HiveBuildingUpgradeScreenState.Loading, snapshot, projectedAt, string.Empty, string.Empty,
                string.Empty, default(DateTimeOffset));
        }

        public static HiveBuildingUpgradeScreenModel Ready(
            RemoteBuildingUpgradeSnapshot snapshot,
            TimeSpan projectedAt)
        {
            return Project(HiveBuildingUpgradeScreenState.Ready, snapshot, projectedAt, string.Empty, string.Empty,
                string.Empty, default(DateTimeOffset));
        }

        public static HiveBuildingUpgradeScreenModel OfflineReadOnly(
            RemoteBuildingUpgradeSnapshot snapshot,
            TimeSpan projectedAt,
            DateTimeOffset cachedAtUtc)
        {
            return Project(HiveBuildingUpgradeScreenState.OfflineReadOnly, snapshot, projectedAt, string.Empty,
                string.Empty, string.Empty, cachedAtUtc);
        }

        public static HiveBuildingUpgradeScreenModel Mutating(
            HiveBuildingUpgradeScreenState state,
            RemoteBuildingUpgradeSnapshot snapshot,
            TimeSpan projectedAt,
            string mutatingBuildingKey)
        {
            if (state != HiveBuildingUpgradeScreenState.Starting &&
                state != HiveBuildingUpgradeScreenState.Completing)
                throw new ArgumentOutOfRangeException(nameof(state));
            return Project(state, snapshot, projectedAt, string.Empty, string.Empty, mutatingBuildingKey, default(DateTimeOffset));
        }

        public static HiveBuildingUpgradeScreenModel Error(
            RemoteBuildingUpgradeSnapshot snapshot,
            TimeSpan projectedAt,
            string stableCode,
            string retrySignature)
        {
            return Project(HiveBuildingUpgradeScreenState.Error, snapshot, projectedAt, stableCode,
                retrySignature, string.Empty, default(DateTimeOffset));
        }

        private static HiveBuildingUpgradeScreenModel Project(
            HiveBuildingUpgradeScreenState state,
            RemoteBuildingUpgradeSnapshot snapshot,
            TimeSpan projectedAt,
            string errorCode,
            string retrySignature,
            string mutatingBuildingKey,
            DateTimeOffset cachedAtUtc)
        {
            if (snapshot == null) return Empty(state, errorCode);
            var balances = snapshot.Balances.ToDictionary(
                entry => entry.Key,
                entry => new RemoteBuildingUpgradeBalance
                {
                    Amount = entry.Value.Amount,
                    Capacity = entry.Value.Capacity
                },
                StringComparer.Ordinal);
            var levels = new Dictionary<string, int>(snapshot.BuildingLevels, StringComparer.Ordinal);
            HiveBuildingUpgradeOfferModel[] offers = snapshot.Offers
                .Select(offer => new HiveBuildingUpgradeOfferModel(offer)).ToArray();
            HiveBuildingUpgradeOperationModel operation = snapshot.ActiveOperation == null
                ? null
                : new HiveBuildingUpgradeOperationModel(snapshot.ActiveOperation);
            return new HiveBuildingUpgradeScreenModel(state, errorCode, retrySignature, mutatingBuildingKey, snapshot.Revision,
                snapshot.ServerTimeUtc, projectedAt, balances, levels, offers, operation, cachedAtUtc);
        }

        private static HiveBuildingUpgradeScreenModel Empty(HiveBuildingUpgradeScreenState state, string errorCode)
        {
            return new HiveBuildingUpgradeScreenModel(state, errorCode, string.Empty, string.Empty, 0L,
                default(DateTimeOffset), TimeSpan.Zero,
                new Dictionary<string, RemoteBuildingUpgradeBalance>(), new Dictionary<string, int>(),
                Array.Empty<HiveBuildingUpgradeOfferModel>(), null, default(DateTimeOffset));
        }
    }

    public interface IHiveBuildingUpgradeMonotonicClock
    {
        TimeSpan Elapsed { get; }
    }

    public sealed class StopwatchHiveBuildingUpgradeClock : IHiveBuildingUpgradeMonotonicClock
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        public TimeSpan Elapsed => stopwatch.Elapsed;
    }

    public interface IHiveBuildingUpgradePanelController
    {
        HiveBuildingUpgradeScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        TimeSpan Elapsed { get; }
        void Refresh();
        void Start(string buildingKey);
        void Complete();
    }

    public sealed class UnavailableHiveBuildingUpgradePanelController : IHiveBuildingUpgradePanelController
    {
        private readonly HiveBuildingUpgradeScreenModel model = HiveBuildingUpgradePresentation.NotConfigured();
        public HiveBuildingUpgradeScreenModel Model => model;
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public TimeSpan Elapsed => TimeSpan.Zero;
        public void Refresh() { }
        public void Start(string buildingKey) { }
        public void Complete() { }
    }

    public interface IHiveBuildingUpgradeMutationKeySource
    {
        string Create(string operation);
    }

    public sealed class SessionHiveBuildingUpgradeMutationKeySource : IHiveBuildingUpgradeMutationKeySource
    {
        public string Create(string operation)
        {
            string safe = string.IsNullOrWhiteSpace(operation) ? "mutation" : operation.Trim();
            return "mobile-building-" + safe + "-" + Guid.NewGuid().ToString("N");
        }
    }

    public sealed class HiveBuildingUpgradePanelController : IHiveBuildingUpgradePanelController, IDisposable
    {
        private readonly IHiveBuildingUpgradeClient client;
        private readonly Guid hiveId;
        private readonly IHiveBuildingUpgradeMutationKeySource keySource;
        private readonly IHiveBuildingUpgradeMonotonicClock clock;
        private readonly Dictionary<string, string> pendingKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private RemoteBuildingUpgradeSnapshot snapshot;
        private bool disposed;
        private bool busy;

        public HiveBuildingUpgradePanelController(
            IHiveBuildingUpgradeClient client,
            Guid hiveId,
            IHiveBuildingUpgradeMutationKeySource keySource = null,
            IHiveBuildingUpgradeMonotonicClock clock = null)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            this.keySource = keySource ?? new SessionHiveBuildingUpgradeMutationKeySource();
            this.clock = clock ?? new StopwatchHiveBuildingUpgradeClock();
            Model = HiveBuildingUpgradePresentation.Loading(null, this.clock.Elapsed);
        }

        public HiveBuildingUpgradeScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;
        public TimeSpan Elapsed => clock.Elapsed;

        public void Refresh() { RefreshInsideLifetime(); }
        public void Start(string buildingKey) { StartInsideLifetime(buildingKey); }
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
        public Task StartForProofAsync(string buildingKey) { return StartCoreAsync(buildingKey); }
        public Task CompleteForProofAsync() { return CompleteCoreAsync(); }

        private async void RefreshInsideLifetime() { await RefreshCoreAsync(); }
        private async void StartInsideLifetime(string buildingKey) { await StartCoreAsync(buildingKey); }
        private async void CompleteInsideLifetime() { await CompleteCoreAsync(); }

        private async Task RefreshCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            Model = HiveBuildingUpgradePresentation.Loading(snapshot, clock.Elapsed);
            try
            {
                RemoteBuildingUpgradeSnapshot result = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                long previousRevision = snapshot == null ? -1L : snapshot.Revision;
                snapshot = result;
                if (previousRevision >= 0L && previousRevision != snapshot.Revision) pendingKeys.Clear();
                Model = client.LastReadSource == GameReadSource.ProtectedCache
                    ? HiveBuildingUpgradePresentation.OfflineReadOnly(snapshot, clock.Elapsed, client.LastReadCachedAtUtc)
                    : HiveBuildingUpgradePresentation.Ready(snapshot, clock.Elapsed);
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Model = HiveBuildingUpgradePresentation.Error(snapshot, clock.Elapsed, "cancelled", string.Empty);
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed) Model = HiveBuildingUpgradePresentation.Error(snapshot, clock.Elapsed, StableError(error), string.Empty);
            }
            catch (Exception)
            {
                if (!disposed) Model = HiveBuildingUpgradePresentation.Error(snapshot, clock.Elapsed, "unexpected", string.Empty);
            }
            finally { busy = false; }
        }

        private async Task StartCoreAsync(string buildingKey)
        {
            HiveBuildingUpgradeScreenModel current = Model;
            if (busy || disposed || snapshot == null || current == null || !current.CanStart(buildingKey)) return;
            busy = true;
            Model = HiveBuildingUpgradePresentation.Mutating(HiveBuildingUpgradeScreenState.Starting, snapshot, clock.Elapsed, buildingKey);
            try
            {
                // La revision partagee avance en continu (le panneau se resynchronise tout
                // seul toutes les 5 secondes) - une revision perimee de quelques secondes ne
                // doit pas obliger le joueur a cliquer "Actualiser" lui-meme ; on rattrape une
                // seule fois avec la revision fraiche avant d'abandonner.
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    string signature = "start|" + buildingKey + "|" + current.Revision.ToString(CultureInfo.InvariantCulture);
                    string key = MutationKey(signature, "start-" + buildingKey);
                    try
                    {
                        RemoteBuildingUpgradeMutationResponse response = await client.StartAsync(
                            hiveId, buildingKey, current.Revision, key, lifetime.Token);
                        if (disposed) return;
                        snapshot = response.Snapshot;
                        pendingKeys.Remove(signature);
                        Model = HiveBuildingUpgradePresentation.Ready(snapshot, clock.Elapsed);
                        return;
                    }
                    catch (HivePerimeterClientException error) when (attempt == 0 && StableError(error) == "revision_conflict")
                    {
                        snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                        if (disposed) return;
                        current = HiveBuildingUpgradePresentation.Ready(snapshot, clock.Elapsed);
                        if (!current.CanStart(buildingKey)) { Model = current; return; }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Model = HiveBuildingUpgradePresentation.Error(snapshot, clock.Elapsed, "cancelled", string.Empty);
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed)
                {
                    string code = StableError(error);
                    string signature = "start|" + buildingKey + "|" + current.Revision.ToString(CultureInfo.InvariantCulture);
                    Model = HiveBuildingUpgradePresentation.Error(snapshot, clock.Elapsed, code,
                        code == "network_unavailable" ? signature : string.Empty);
                }
            }
            catch (Exception)
            {
                if (!disposed) Model = HiveBuildingUpgradePresentation.Error(snapshot, clock.Elapsed, "unexpected", string.Empty);
            }
            finally { busy = false; }
        }

        private async Task CompleteCoreAsync()
        {
            HiveBuildingUpgradeScreenModel current = Model;
            if (busy || disposed || snapshot == null || current == null || current.ActiveOperation == null ||
                !current.CanComplete(current.ActiveOperation.BuildingKey)) return;
            busy = true;
            Model = HiveBuildingUpgradePresentation.Mutating(HiveBuildingUpgradeScreenState.Completing, snapshot, clock.Elapsed, current.ActiveOperation.BuildingKey);
            try
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    Guid operationId = current.ActiveOperation.OperationId;
                    string signature = "complete|" + operationId.ToString("D") + "|" +
                        current.Revision.ToString(CultureInfo.InvariantCulture);
                    string key = MutationKey(signature, "complete-" + operationId.ToString("N"));
                    try
                    {
                        RemoteBuildingUpgradeMutationResponse response = await client.CompleteAsync(
                            hiveId, operationId, current.Revision, key, lifetime.Token);
                        if (disposed) return;
                        snapshot = response.Snapshot;
                        pendingKeys.Remove(signature);
                        Model = HiveBuildingUpgradePresentation.Ready(snapshot, clock.Elapsed);
                        GameEventBus.Shared.Publish(
                            new BuildingCompleted(current.ActiveOperation.BuildingKey, operationId),
                            "construction");
                        return;
                    }
                    catch (HivePerimeterClientException error) when (attempt == 0 && StableError(error) == "revision_conflict")
                    {
                        snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                        if (disposed) return;
                        current = HiveBuildingUpgradePresentation.Ready(snapshot, clock.Elapsed);
                        if (current.ActiveOperation == null || !current.CanComplete(current.ActiveOperation.BuildingKey)) { Model = current; return; }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Model = HiveBuildingUpgradePresentation.Error(snapshot, clock.Elapsed, "cancelled", string.Empty);
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed)
                {
                    string code = StableError(error);
                    string signature = current.ActiveOperation == null ? string.Empty : "complete|" + current.ActiveOperation.OperationId.ToString("D") + "|" + current.Revision.ToString(CultureInfo.InvariantCulture);
                    Model = HiveBuildingUpgradePresentation.Error(snapshot, clock.Elapsed, code,
                        code == "network_unavailable" ? signature : string.Empty);
                }
            }
            catch (Exception)
            {
                if (!disposed) Model = HiveBuildingUpgradePresentation.Error(snapshot, clock.Elapsed, "unexpected", string.Empty);
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
                    case "game.construction_busy": return "construction_busy";
                    case "game.insufficient_resources": return "insufficient_resources";
                    case "game.level_conflict": return "level_conflict";
                    case "game.not_ready": return "not_ready";
                    case "game.operation_not_found": return "operation_not_found";
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
