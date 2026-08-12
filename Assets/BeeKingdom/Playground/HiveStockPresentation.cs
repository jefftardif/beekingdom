using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    public enum HiveStockScreenState
    {
        NotConfigured,
        Loading,
        Ready,
        OfflineReadOnly,
        Error
    }

    public sealed class HiveStockResourceModel
    {
        internal HiveStockResourceModel(string key, long amount, long capacity)
        {
            Key = key;
            Amount = Math.Max(0L, amount);
            Capacity = Math.Max(Amount, capacity);
        }

        public string Key { get; }
        public long Amount { get; }
        public long Capacity { get; }
        public bool IsFull => Amount >= Capacity;
        public double Fill => Capacity <= 0L ? 0d : Math.Max(0d, Math.Min(1d, (double)Amount / Capacity));
    }

    public sealed class HiveStockEngagementModel
    {
        internal HiveStockEngagementModel(RemoteHiveStockEngagement value)
        {
            OperationId = value.OperationId;
            Kind = value.Kind;
            Key = value.Key;
            StartedAtUtc = value.StartedAtUtc;
            EndsAtUtc = value.EndsAtUtc;
        }

        public Guid OperationId { get; }
        public string Kind { get; }
        public string Key { get; }
        public DateTimeOffset StartedAtUtc { get; }
        public DateTimeOffset EndsAtUtc { get; }
    }

    public sealed class HiveStockScreenModel
    {
        internal HiveStockScreenModel(
            HiveStockScreenState state,
            string errorCode,
            long revision,
            DateTimeOffset serverTimeUtc,
            DateTimeOffset cachedAtUtc,
            IReadOnlyList<HiveStockResourceModel> resources,
            long? population,
            long? populationCapacity,
            IReadOnlyList<string> completedResearchIds,
            IReadOnlyList<HiveStockEngagementModel> activeEngagements)
        {
            State = state;
            ErrorCode = errorCode ?? string.Empty;
            Revision = revision;
            ServerTimeUtc = serverTimeUtc;
            CachedAtUtc = cachedAtUtc;
            Resources = resources ?? Array.Empty<HiveStockResourceModel>();
            Population = population;
            PopulationCapacity = populationCapacity;
            CompletedResearchIds = completedResearchIds ?? Array.Empty<string>();
            ActiveEngagements = activeEngagements ?? Array.Empty<HiveStockEngagementModel>();
        }

        public HiveStockScreenState State { get; }
        public string ErrorCode { get; }
        public long Revision { get; }
        public DateTimeOffset ServerTimeUtc { get; }
        public DateTimeOffset CachedAtUtc { get; }
        public IReadOnlyList<HiveStockResourceModel> Resources { get; }
        public long? Population { get; }
        public long? PopulationCapacity { get; }
        public IReadOnlyList<string> CompletedResearchIds { get; }
        public IReadOnlyList<HiveStockEngagementModel> ActiveEngagements { get; }
        public bool IsReadOnly => State == HiveStockScreenState.OfflineReadOnly;
        public bool HasSnapshot => Resources.Count == 3;

        public HiveStockResourceModel FindResource(string key)
        {
            return Resources.FirstOrDefault(item =>
                string.Equals(item.Key, key, StringComparison.Ordinal));
        }

        public int ActiveEngagementCountFor(string key)
        {
            return ActiveEngagements.Count(item =>
                string.Equals(item.Key, key, StringComparison.Ordinal));
        }
    }

    public static class HiveStockPresentation
    {
        public static HiveStockScreenModel NotConfigured()
        {
            return Empty(HiveStockScreenState.NotConfigured, string.Empty);
        }

        public static HiveStockScreenModel Loading(RemoteHiveStockSnapshot snapshot = null)
        {
            return Project(
                HiveStockScreenState.Loading,
                snapshot,
                string.Empty,
                default(DateTimeOffset));
        }

        public static HiveStockScreenModel Ready(RemoteHiveStockSnapshot snapshot)
        {
            return Project(
                HiveStockScreenState.Ready,
                snapshot,
                string.Empty,
                default(DateTimeOffset));
        }

        public static HiveStockScreenModel OfflineReadOnly(
            RemoteHiveStockSnapshot snapshot,
            DateTimeOffset cachedAtUtc)
        {
            return Project(
                HiveStockScreenState.OfflineReadOnly,
                snapshot,
                string.Empty,
                cachedAtUtc);
        }

        public static HiveStockScreenModel Error(
            RemoteHiveStockSnapshot snapshot,
            string stableCode)
        {
            return Project(
                HiveStockScreenState.Error,
                snapshot,
                stableCode,
                default(DateTimeOffset));
        }

        private static HiveStockScreenModel Project(
            HiveStockScreenState state,
            RemoteHiveStockSnapshot snapshot,
            string errorCode,
            DateTimeOffset cachedAtUtc)
        {
            if (snapshot == null) return Empty(state, errorCode);
            HiveStockResourceModel[] resources =
            {
                new HiveStockResourceModel("honey", snapshot.Honey.Amount, snapshot.Honey.Capacity),
                new HiveStockResourceModel("wax", snapshot.Wax.Amount, snapshot.Wax.Capacity),
                new HiveStockResourceModel("pollen", snapshot.Pollen.Amount, snapshot.Pollen.Capacity)
            };
            string[] completed = snapshot.CompletedResearchIds.ToArray();
            HiveStockEngagementModel[] engagements = snapshot.ActiveEngagements
                .Select(value => new HiveStockEngagementModel(value))
                .ToArray();
            return new HiveStockScreenModel(
                state,
                errorCode,
                snapshot.Revision,
                snapshot.ServerTimeUtc,
                cachedAtUtc,
                resources,
                snapshot.Population,
                snapshot.PopulationCapacity,
                completed,
                engagements);
        }

        private static HiveStockScreenModel Empty(
            HiveStockScreenState state,
            string errorCode)
        {
            return new HiveStockScreenModel(
                state,
                errorCode,
                0L,
                default(DateTimeOffset),
                default(DateTimeOffset),
                Array.Empty<HiveStockResourceModel>(),
                null,
                null,
                Array.Empty<string>(),
                Array.Empty<HiveStockEngagementModel>());
        }
    }

    public interface IHiveStockPanelController
    {
        HiveStockScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
    }

    public sealed class UnavailableHiveStockPanelController : IHiveStockPanelController
    {
        private readonly HiveStockScreenModel model = HiveStockPresentation.NotConfigured();
        public HiveStockScreenModel Model => model;
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
    }

    public sealed class HiveStockPanelController : IHiveStockPanelController, IDisposable
    {
        private readonly IHiveStockSnapshotClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private RemoteHiveStockSnapshot snapshot;
        private bool disposed;
        private bool busy;

        public HiveStockPanelController(IHiveStockSnapshotClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty)
                throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            Model = HiveStockPresentation.Loading();
        }

        public HiveStockScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh()
        {
            RefreshInsideLifetime();
        }

        public Task RefreshForProofAsync()
        {
            return RefreshCoreAsync();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private async void RefreshInsideLifetime()
        {
            await RefreshCoreAsync();
        }

        private async Task RefreshCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            Model = HiveStockPresentation.Loading(snapshot);
            try
            {
                RemoteHiveStockSnapshot result =
                    await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                snapshot = result;
                Model = client.LastReadSource == GameReadSource.ProtectedCache
                    ? HiveStockPresentation.OfflineReadOnly(
                        snapshot, client.LastReadCachedAtUtc)
                    : HiveStockPresentation.Ready(snapshot);
            }
            catch (OperationCanceledException)
            {
                if (!disposed)
                    Model = HiveStockPresentation.Error(snapshot, "cancelled");
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed)
                    Model = HiveStockPresentation.Error(snapshot, StableError(error));
            }
            catch (Exception)
            {
                if (!disposed)
                    Model = HiveStockPresentation.Error(snapshot, "unexpected");
            }
            finally
            {
                busy = false;
            }
        }

        private static string StableError(HivePerimeterClientException error)
        {
            if (error.Error == HivePerimeterClientError.InvalidResponse &&
                string.Equals(error.Message, "game.unavailable", StringComparison.Ordinal))
                return "server_unavailable";
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
