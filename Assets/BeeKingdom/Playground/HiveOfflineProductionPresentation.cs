using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    public enum HiveOfflineProductionScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        OfflineReadOnly = 3,
        Collecting = 4,
        Error = 5
    }

    public sealed class HiveOfflineProductionLineModel
    {
        internal HiveOfflineProductionLineModel(
            string buildingKey,
            string resourceKey,
            decimal pendingAmount,
            decimal hourlyRate,
            long productionCapacity,
            long collectableWholeUnits,
            long balanceAmount,
            long balanceCapacity)
        {
            BuildingKey = buildingKey ?? string.Empty;
            ResourceKey = resourceKey ?? string.Empty;
            PendingAmount = Math.Max(0m, pendingAmount);
            HourlyRate = Math.Max(0m, hourlyRate);
            ProductionCapacity = Math.Max(0L, productionCapacity);
            CollectableWholeUnits = Math.Max(0L, collectableWholeUnits);
            BalanceAmount = Math.Max(0L, balanceAmount);
            BalanceCapacity = Math.Max(BalanceAmount, balanceCapacity);
        }

        public string BuildingKey { get; }
        public string ResourceKey { get; }
        public decimal PendingAmount { get; }
        public decimal HourlyRate { get; }
        public long ProductionCapacity { get; }
        public long CollectableWholeUnits { get; }
        public long BalanceAmount { get; }
        public long BalanceCapacity { get; }
        public bool IsResourceCapacityFull => BalanceAmount >= BalanceCapacity;
    }

    public sealed class HiveOfflineProductionScreenModel
    {
        internal HiveOfflineProductionScreenModel(
            HiveOfflineProductionScreenState state,
            string errorCode,
            string retryBuildingKey,
            string collectingBuildingKey,
            long productionRevision,
            DateTimeOffset serverTimeUtc,
            DateTimeOffset productionAsOfUtc,
            TimeSpan maxRecognizedDuration,
            IReadOnlyList<HiveOfflineProductionLineModel> lines,
            DateTimeOffset cachedAtUtc)
        {
            State = state;
            ErrorCode = errorCode ?? string.Empty;
            RetryBuildingKey = retryBuildingKey ?? string.Empty;
            CollectingBuildingKey = collectingBuildingKey ?? string.Empty;
            ProductionRevision = Math.Max(0L, productionRevision);
            ServerTimeUtc = serverTimeUtc;
            ProductionAsOfUtc = productionAsOfUtc;
            MaxRecognizedDuration = maxRecognizedDuration < TimeSpan.Zero ? TimeSpan.Zero : maxRecognizedDuration;
            Lines = lines ?? Array.Empty<HiveOfflineProductionLineModel>();
            CachedAtUtc = cachedAtUtc;
        }

        public HiveOfflineProductionScreenState State { get; }
        public string ErrorCode { get; }
        public string RetryBuildingKey { get; }
        // Seul CE batiment doit afficher un statut "collecte en cours" - une collecte est une
        // mutation par batiment, meme si l'etat State (partage par les 3 lignes) passe a
        // Collecting pour tout le modele. Sans cette distinction, l'UI affichait un message de
        // synchronisation sur les DEUX AUTRES batiments pendant qu'un seul etait reellement en
        // cours de collecte - la ruche semblait "attendre le serveur" en permanence (rapporte par
        // Jeff, voir Claude_Continuation.md).
        public string CollectingBuildingKey { get; }
        public long ProductionRevision { get; }
        public DateTimeOffset ServerTimeUtc { get; }
        public DateTimeOffset ProductionAsOfUtc { get; }
        public TimeSpan MaxRecognizedDuration { get; }
        public IReadOnlyList<HiveOfflineProductionLineModel> Lines { get; }
        public DateTimeOffset CachedAtUtc { get; }
        public bool IsReadOnly => State == HiveOfflineProductionScreenState.OfflineReadOnly;

        public HiveOfflineProductionLineModel FindLine(string buildingKey)
        {
            return Lines.SingleOrDefault(line => string.Equals(line.BuildingKey, buildingKey, StringComparison.Ordinal));
        }

        public bool CanCollect(string buildingKey)
        {
            HiveOfflineProductionLineModel line = FindLine(buildingKey);
            if (line == null || line.CollectableWholeUnits <= 0 || line.IsResourceCapacityFull) return false;
            if (State == HiveOfflineProductionScreenState.Ready) return true;
            return State == HiveOfflineProductionScreenState.Error &&
                string.Equals(ErrorCode, "network_unavailable", StringComparison.Ordinal) &&
                string.Equals(RetryBuildingKey, buildingKey, StringComparison.Ordinal);
        }
    }

    public static class HiveOfflineProductionPresentation
    {
        public static HiveOfflineProductionScreenModel NotConfigured()
        {
            return Empty(HiveOfflineProductionScreenState.NotConfigured, string.Empty);
        }

        public static HiveOfflineProductionScreenModel Loading(RemoteOfflineProductionSnapshot snapshot = null)
        {
            return Project(HiveOfflineProductionScreenState.Loading, snapshot, string.Empty, string.Empty, string.Empty, default(DateTimeOffset));
        }

        public static HiveOfflineProductionScreenModel Collecting(RemoteOfflineProductionSnapshot snapshot, string collectingBuildingKey)
        {
            return Project(HiveOfflineProductionScreenState.Collecting, snapshot, string.Empty, string.Empty, collectingBuildingKey, default(DateTimeOffset));
        }

        public static HiveOfflineProductionScreenModel Ready(RemoteOfflineProductionSnapshot snapshot)
        {
            return Project(HiveOfflineProductionScreenState.Ready, snapshot, string.Empty, string.Empty, string.Empty, default(DateTimeOffset));
        }

        public static HiveOfflineProductionScreenModel OfflineReadOnly(
            RemoteOfflineProductionSnapshot snapshot,
            DateTimeOffset cachedAtUtc)
        {
            return Project(
                HiveOfflineProductionScreenState.OfflineReadOnly,
                snapshot,
                string.Empty,
                string.Empty,
                string.Empty,
                cachedAtUtc);
        }

        public static HiveOfflineProductionScreenModel Error(
            RemoteOfflineProductionSnapshot snapshot,
            string stableCode,
            string retryBuildingKey = null)
        {
            return Project(
                HiveOfflineProductionScreenState.Error,
                snapshot,
                stableCode,
                retryBuildingKey,
                string.Empty,
                default(DateTimeOffset));
        }

        private static HiveOfflineProductionScreenModel Project(
            HiveOfflineProductionScreenState state,
            RemoteOfflineProductionSnapshot snapshot,
            string errorCode,
            string retryBuildingKey,
            string collectingBuildingKey,
            DateTimeOffset cachedAtUtc)
        {
            if (snapshot == null) return Empty(state, errorCode);
            HiveOfflineProductionLineModel[] lines = snapshot.Lines.Select(line =>
            {
                RemoteOfflineProductionBalance balance = snapshot.Balances[line.ResourceKey];
                return new HiveOfflineProductionLineModel(
                    line.BuildingKey,
                    line.ResourceKey,
                    line.PendingAmount,
                    line.HourlyRate,
                    line.Capacity,
                    line.CollectableWholeUnits,
                    balance.Amount,
                    balance.Capacity);
            }).ToArray();
            return new HiveOfflineProductionScreenModel(
                state,
                errorCode,
                retryBuildingKey,
                collectingBuildingKey,
                snapshot.ProductionRevision,
                snapshot.ServerTimeUtc,
                snapshot.ProductionAsOfUtc,
                snapshot.MaxRecognizedDuration,
                lines,
                cachedAtUtc);
        }

        private static HiveOfflineProductionScreenModel Empty(
            HiveOfflineProductionScreenState state,
            string errorCode)
        {
            return new HiveOfflineProductionScreenModel(
                state,
                errorCode,
                string.Empty,
                string.Empty,
                0L,
                default(DateTimeOffset),
                default(DateTimeOffset),
                TimeSpan.Zero,
                Array.Empty<HiveOfflineProductionLineModel>(),
                default(DateTimeOffset));
        }
    }

    public interface IHiveOfflineProductionPanelController
    {
        HiveOfflineProductionScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        void Collect(string buildingKey);
    }

    public sealed class UnavailableHiveOfflineProductionPanelController : IHiveOfflineProductionPanelController
    {
        private readonly HiveOfflineProductionScreenModel model = HiveOfflineProductionPresentation.NotConfigured();
        public HiveOfflineProductionScreenModel Model => model;
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public void Collect(string buildingKey) { }
    }

    public interface IHiveOfflineProductionMutationKeySource
    {
        string Create(string operation);
    }

    public sealed class SessionHiveOfflineProductionMutationKeySource : IHiveOfflineProductionMutationKeySource
    {
        public string Create(string operation)
        {
            string safe = string.IsNullOrWhiteSpace(operation) ? "collect" : operation.Trim();
            return "mobile-production-" + safe + "-" + Guid.NewGuid().ToString("N");
        }
    }

    public sealed class HiveOfflineProductionPanelController : IHiveOfflineProductionPanelController, IDisposable
    {
        private readonly IHiveOfflineProductionClient client;
        private readonly Guid hiveId;
        private readonly IHiveOfflineProductionMutationKeySource keySource;
        private readonly Dictionary<string, string> pendingKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private RemoteOfflineProductionSnapshot snapshot;
        private bool disposed;
        private bool busy;

        public HiveOfflineProductionPanelController(
            IHiveOfflineProductionClient client,
            Guid hiveId,
            IHiveOfflineProductionMutationKeySource keySource = null)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            this.keySource = keySource ?? new SessionHiveOfflineProductionMutationKeySource();
            Model = HiveOfflineProductionPresentation.Loading();
        }

        public HiveOfflineProductionScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh()
        {
            RefreshInsideLifetime();
        }

        public void Collect(string buildingKey)
        {
            CollectInsideLifetime(buildingKey);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
            pendingKeys.Clear();
        }

        internal async Task RefreshForProofAsync()
        {
            await RefreshCoreAsync();
        }

        internal async Task CollectForProofAsync(string buildingKey)
        {
            await CollectCoreAsync(buildingKey);
        }

        private async void RefreshInsideLifetime()
        {
            await RefreshCoreAsync();
        }

        private async void CollectInsideLifetime(string buildingKey)
        {
            await CollectCoreAsync(buildingKey);
        }

        private async Task RefreshCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            // Un sondage periodique en arriere-plan (panneau deja ouvert avec des donnees
            // valides) ne doit pas faire clignoter l'affichage sur "Synchronisation" - on ne
            // bascule sur l'etat Loading que pour la toute premiere lecture, sans instantane.
            if (snapshot == null) Model = HiveOfflineProductionPresentation.Loading(snapshot);
            try
            {
                RemoteOfflineProductionSnapshot result = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                long previousRevision = snapshot == null ? -1L : snapshot.ProductionRevision;
                snapshot = result;
                if (previousRevision >= 0L && previousRevision != result.ProductionRevision) pendingKeys.Clear();
                Model = client.LastReadSource == GameReadSource.ProtectedCache
                    ? HiveOfflineProductionPresentation.OfflineReadOnly(result, client.LastReadCachedAtUtc)
                    : HiveOfflineProductionPresentation.Ready(result);
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Model = HiveOfflineProductionPresentation.Error(snapshot, "cancelled");
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed) Model = HiveOfflineProductionPresentation.Error(snapshot, StableError(error));
            }
            catch (Exception)
            {
                if (!disposed) Model = HiveOfflineProductionPresentation.Error(snapshot, "unexpected");
            }
            finally
            {
                busy = false;
            }
        }

        private async Task CollectCoreAsync(string buildingKey)
        {
            HiveOfflineProductionScreenModel current = Model;
            if (busy || disposed || snapshot == null || current == null || !current.CanCollect(buildingKey)) return;
            string signature = "collect|" + buildingKey + "|" +
                current.ProductionRevision.ToString(CultureInfo.InvariantCulture);
            string key = MutationKey(signature, buildingKey);
            busy = true;
            Model = HiveOfflineProductionPresentation.Collecting(snapshot, buildingKey);
            try
            {
                RemoteOfflineProductionCollectResponse response = await client.CollectAsync(
                    hiveId,
                    buildingKey,
                    current.ProductionRevision,
                    key,
                    lifetime.Token);
                if (disposed) return;
                snapshot = response.Snapshot;
                pendingKeys.Remove(signature);
                try { BeeKingdom.Tutorial.TutorialGameplayNotifier.NotifyProductionCollected(buildingKey); } catch {}
                Model = HiveOfflineProductionPresentation.Ready(snapshot);
            }
            catch (OperationCanceledException)
            {
                if (!disposed) Model = HiveOfflineProductionPresentation.Error(snapshot, "cancelled");
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed)
                {
                    string code = StableError(error);
                    string retry = string.Equals(code, "network_unavailable", StringComparison.Ordinal)
                        ? buildingKey
                        : string.Empty;
                    Model = HiveOfflineProductionPresentation.Error(snapshot, code, retry);
                }
            }
            catch (Exception)
            {
                if (!disposed) Model = HiveOfflineProductionPresentation.Error(snapshot, "unexpected");
            }
            finally
            {
                busy = false;
            }
        }

        private string MutationKey(string signature, string buildingKey)
        {
            string value;
            if (!pendingKeys.TryGetValue(signature, out value))
            {
                value = keySource.Create("collect-" + buildingKey);
                pendingKeys[signature] = value;
            }
            return value;
        }

        private static string StableError(HivePerimeterClientException error)
        {
            if (error.Error == HivePerimeterClientError.NotConfigured) return "not_configured";
            if (error.Error == HivePerimeterClientError.AuthenticationRequired) return "authentication_required";
            if (error.Error == HivePerimeterClientError.InvalidRequest) return "invalid_request";
            if (error.Error == HivePerimeterClientError.TransportFailure) return "network_unavailable";
            if (!string.IsNullOrEmpty(error.Message) && error.Message.StartsWith("game.", StringComparison.Ordinal))
                return error.Message.Substring("game.".Length);
            return "invalid_response";
        }
    }
}
