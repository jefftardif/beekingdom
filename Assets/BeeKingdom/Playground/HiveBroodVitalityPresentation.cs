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
    public enum HiveBroodVitalityScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        OfflineReadOnly = 3,
        PreparingMutation = 4,
        Mutating = 5,
        PendingConfirmation = 6,
        Error = 7
    }

    public sealed class HiveBroodVitalityOperationModel
    {
        internal HiveBroodVitalityOperationModel(
            RemoteBroodVitalityOperation source)
        {
            OperationId = source.OperationId;
            Type = source.Type ?? string.Empty;
            StartedAtUtc = source.StartedAtUtc;
            EndsAtUtc = source.EndsAtUtc;
        }

        public Guid OperationId { get; }
        public string Type { get; }
        public DateTimeOffset StartedAtUtc { get; }
        public DateTimeOffset EndsAtUtc { get; }
    }

    public sealed class HiveBroodVitalityScreenModel
    {
        internal HiveBroodVitalityScreenModel(
            HiveBroodVitalityScreenState state,
            string errorCode,
            string pendingKind,
            string pendingPayload,
            bool protectedOutboxAvailable,
            bool initialized,
            int nutrition,
            int stability,
            long globalRevision,
            long vitalityRevision,
            DateTimeOffset serverTimeUtc,
            TimeSpan projectedAt,
            DateTimeOffset updatedAtUtc,
            HiveBroodVitalityOperationModel activeOperation,
            DateTimeOffset cachedAtUtc)
        {
            State = state;
            ErrorCode = errorCode ?? string.Empty;
            PendingKind = pendingKind ?? string.Empty;
            PendingPayload = pendingPayload ?? string.Empty;
            ProtectedOutboxAvailable = protectedOutboxAvailable;
            Initialized = initialized;
            Nutrition = Math.Max(0, Math.Min(100, nutrition));
            Stability = Math.Max(0, Math.Min(100, stability));
            GlobalRevision = Math.Max(0L, globalRevision);
            VitalityRevision = Math.Max(0L, vitalityRevision);
            ServerTimeUtc = serverTimeUtc;
            ProjectedAt = projectedAt < TimeSpan.Zero
                ? TimeSpan.Zero
                : projectedAt;
            UpdatedAtUtc = updatedAtUtc;
            ActiveOperation = activeOperation;
            CachedAtUtc = cachedAtUtc;
        }

        public HiveBroodVitalityScreenState State { get; }
        public string ErrorCode { get; }
        public string PendingKind { get; }
        public string PendingPayload { get; }
        public bool ProtectedOutboxAvailable { get; }
        public bool Initialized { get; }
        public int Nutrition { get; }
        public int Stability { get; }
        public long GlobalRevision { get; }
        public long VitalityRevision { get; }
        public DateTimeOffset ServerTimeUtc { get; }
        public TimeSpan ProjectedAt { get; }
        public DateTimeOffset UpdatedAtUtc { get; }
        public HiveBroodVitalityOperationModel ActiveOperation { get; }
        public DateTimeOffset CachedAtUtc { get; }
        public bool IsReadOnly =>
            State == HiveBroodVitalityScreenState.OfflineReadOnly;
        public bool IsPending =>
            State == HiveBroodVitalityScreenState.PendingConfirmation;

        public int Quality => Math.Min(Nutrition, Stability);

        public string Tier
        {
            get
            {
                if (!Initialized) return "uninitialized";
                if (Quality < 40) return "care_required";
                if (Quality < 65) return "watch";
                if (Quality < 90) return "stable";
                return "thriving";
            }
        }

        public bool CanStart(string type)
        {
            if (!Initialized ||
                !ProtectedOutboxAvailable ||
                ActiveOperation != null ||
                IsReadOnly)
                return false;
            bool retry = IsPending &&
                string.Equals(PendingKind, "start", StringComparison.Ordinal) &&
                string.Equals(PendingPayload, type, StringComparison.Ordinal);
            if (State != HiveBroodVitalityScreenState.Ready && !retry)
                return false;
            if (string.Equals(
                    type,
                    HiveBroodVitalityClient.FeedingType,
                    StringComparison.Ordinal))
                return Nutrition < 100;
            if (string.Equals(
                    type,
                    HiveBroodVitalityClient.StabilizationType,
                    StringComparison.Ordinal))
                return Stability < 100;
            return false;
        }

        public bool CanComplete(TimeSpan currentElapsed)
        {
            if (!Initialized ||
                !ProtectedOutboxAvailable ||
                ActiveOperation == null ||
                IsReadOnly ||
                Remaining(currentElapsed) > TimeSpan.Zero)
                return false;
            bool retry = IsPending &&
                string.Equals(
                    PendingKind,
                    "complete",
                    StringComparison.Ordinal) &&
                string.Equals(
                    PendingPayload,
                    ActiveOperation.OperationId.ToString("N"),
                    StringComparison.Ordinal);
            return State == HiveBroodVitalityScreenState.Ready || retry;
        }

        public TimeSpan Remaining(TimeSpan currentElapsed)
        {
            if (ActiveOperation == null ||
                ServerTimeUtc == default(DateTimeOffset))
                return TimeSpan.Zero;
            TimeSpan delta = currentElapsed - ProjectedAt;
            if (delta < TimeSpan.Zero) delta = TimeSpan.Zero;
            TimeSpan remaining =
                ActiveOperation.EndsAtUtc - (ServerTimeUtc + delta);
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        public double Progress01(TimeSpan currentElapsed)
        {
            if (ActiveOperation == null) return 0d;
            TimeSpan total =
                ActiveOperation.EndsAtUtc - ActiveOperation.StartedAtUtc;
            if (total <= TimeSpan.Zero) return 0d;
            return Math.Max(
                0d,
                Math.Min(
                    1d,
                    1d - Remaining(currentElapsed).TotalSeconds /
                        total.TotalSeconds));
        }
    }

    public static class HiveBroodVitalityPresentation
    {
        public static HiveBroodVitalityScreenModel NotConfigured()
        {
            return Empty(
                HiveBroodVitalityScreenState.NotConfigured,
                string.Empty,
                false);
        }

        public static HiveBroodVitalityScreenModel Project(
            HiveBroodVitalityScreenState state,
            RemoteBroodVitalitySnapshot snapshot,
            TimeSpan projectedAt,
            bool protectedOutboxAvailable,
            string errorCode = "",
            string pendingKind = "",
            string pendingPayload = "",
            DateTimeOffset cachedAtUtc = default(DateTimeOffset))
        {
            if (snapshot == null)
                return Empty(state, errorCode, protectedOutboxAvailable);
            RemoteBroodVitalityState vitality = snapshot.Vitality;
            return new HiveBroodVitalityScreenModel(
                state,
                errorCode,
                pendingKind,
                pendingPayload,
                protectedOutboxAvailable,
                vitality != null,
                vitality == null ? 0 : vitality.Nutrition,
                vitality == null ? 0 : vitality.Stability,
                snapshot.GlobalRevision,
                vitality == null ? 0 : vitality.Revision,
                snapshot.ServerTimeUtc,
                projectedAt,
                vitality == null
                    ? default(DateTimeOffset)
                    : vitality.UpdatedAtUtc,
                vitality == null || vitality.ActiveOperation == null
                    ? null
                    : new HiveBroodVitalityOperationModel(
                        vitality.ActiveOperation),
                cachedAtUtc);
        }

        private static HiveBroodVitalityScreenModel Empty(
            HiveBroodVitalityScreenState state,
            string errorCode,
            bool protectedOutboxAvailable)
        {
            return new HiveBroodVitalityScreenModel(
                state,
                errorCode,
                string.Empty,
                string.Empty,
                protectedOutboxAvailable,
                false,
                0,
                0,
                0,
                0,
                default(DateTimeOffset),
                TimeSpan.Zero,
                default(DateTimeOffset),
                null,
                default(DateTimeOffset));
        }
    }

    public interface IHiveBroodVitalityMonotonicClock
    {
        TimeSpan Elapsed { get; }
        DateTimeOffset UtcNow { get; }
    }

    public sealed class StopwatchHiveBroodVitalityClock :
        IHiveBroodVitalityMonotonicClock
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        public TimeSpan Elapsed => stopwatch.Elapsed;
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    public interface IHiveBroodVitalityMutationKeySource
    {
        string Create(string operation);
    }

    public sealed class SessionHiveBroodVitalityMutationKeySource :
        IHiveBroodVitalityMutationKeySource
    {
        public string Create(string operation)
        {
            string safe = string.IsNullOrWhiteSpace(operation)
                ? "mutation"
                : operation.Trim();
            return "mobile-brood-" + safe + "-" +
                Guid.NewGuid().ToString("N");
        }
    }

    public interface IHiveBroodVitalityPanelController
    {
        HiveBroodVitalityScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        TimeSpan Elapsed { get; }
        void Refresh();
        void Start(string type);
        void Complete();
        void Retry();
    }

    public sealed class UnavailableHiveBroodVitalityPanelController :
        IHiveBroodVitalityPanelController
    {
        private readonly HiveBroodVitalityScreenModel model =
            HiveBroodVitalityPresentation.NotConfigured();
        public HiveBroodVitalityScreenModel Model => model;
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public TimeSpan Elapsed => TimeSpan.Zero;
        public void Refresh() { }
        public void Start(string type) { }
        public void Complete() { }
        public void Retry() { }
    }

    public sealed class HiveBroodVitalityPanelController :
        IHiveBroodVitalityPanelController,
        IDisposable
    {
        private readonly IHiveBroodVitalityClient client;
        private readonly Guid hiveId;
        private readonly ProtectedGameMutationOutbox outbox;
        private readonly IHiveBroodVitalityMutationKeySource keySource;
        private readonly IHiveBroodVitalityMonotonicClock clock;
        private readonly CancellationTokenSource lifetime =
            new CancellationTokenSource();
        private RemoteBroodVitalitySnapshot snapshot;
        private bool disposed;
        private bool busy;

        public HiveBroodVitalityPanelController(
            IHiveBroodVitalityClient client,
            Guid hiveId,
            ProtectedGameMutationOutbox outbox,
            IHiveBroodVitalityMutationKeySource keySource = null,
            IHiveBroodVitalityMonotonicClock clock = null)
        {
            this.client =
                client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty)
                throw new ArgumentException(
                    "A hive identifier is required.",
                    nameof(hiveId));
            this.hiveId = hiveId;
            this.outbox =
                outbox ?? throw new ArgumentNullException(nameof(outbox));
            this.keySource =
                keySource ?? new SessionHiveBroodVitalityMutationKeySource();
            this.clock = clock ?? new StopwatchHiveBroodVitalityClock();
            Model = HiveBroodVitalityPresentation.Project(
                HiveBroodVitalityScreenState.Loading,
                null,
                this.clock.Elapsed,
                outbox.IsProtectionAvailable);
        }

        public HiveBroodVitalityScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;
        public TimeSpan Elapsed => clock.Elapsed;

        public void Refresh() { RefreshInsideLifetime(); }
        public void Start(string type) { StartInsideLifetime(type, false); }
        public void Complete() { CompleteInsideLifetime(false); }
        public void Retry() { RetryInsideLifetime(); }

        public Task RefreshForProofAsync() { return RefreshCoreAsync(); }
        public Task StartForProofAsync(string type, bool retry = false)
        {
            return StartCoreAsync(type, retry);
        }
        public Task CompleteForProofAsync(bool retry = false)
        {
            return CompleteCoreAsync(retry);
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

        private async void StartInsideLifetime(string type, bool retry)
        {
            await StartCoreAsync(type, retry);
        }

        private async void CompleteInsideLifetime(bool retry)
        {
            await CompleteCoreAsync(retry);
        }

        private async void RetryInsideLifetime()
        {
            HiveBroodVitalityScreenModel current = Model;
            if (current == null || !current.IsPending) return;
            if (string.Equals(
                    current.PendingKind,
                    "start",
                    StringComparison.Ordinal))
                await StartCoreAsync(current.PendingPayload, true);
            else if (string.Equals(
                current.PendingKind,
                "complete",
                StringComparison.Ordinal))
                await CompleteCoreAsync(true);
        }

        private async Task RefreshCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            Model = HiveBroodVitalityPresentation.Project(
                HiveBroodVitalityScreenState.Loading,
                snapshot,
                clock.Elapsed,
                outbox.IsProtectionAvailable);
            try
            {
                RemoteBroodVitalitySnapshot result =
                    await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                snapshot = result;
                if (client.LastReadSource == GameReadSource.ProtectedCache)
                {
                    Model = HiveBroodVitalityPresentation.Project(
                        HiveBroodVitalityScreenState.OfflineReadOnly,
                        snapshot,
                        clock.Elapsed,
                        outbox.IsProtectionAvailable,
                        cachedAtUtc: client.LastReadCachedAtUtc);
                    return;
                }

                PendingGameMutation pending =
                    await ReconcilePendingAsync(lifetime.Token);
                if (disposed) return;
                if (outbox.LastLoadDetectedCorruption)
                {
                    Model = HiveBroodVitalityPresentation.Project(
                        HiveBroodVitalityScreenState.Error,
                        snapshot,
                        clock.Elapsed,
                        outbox.IsProtectionAvailable,
                        "mutation_recovery_refresh_required");
                    return;
                }
                Model = pending == null
                    ? HiveBroodVitalityPresentation.Project(
                        HiveBroodVitalityScreenState.Ready,
                        snapshot,
                        clock.Elapsed,
                        outbox.IsProtectionAvailable)
                    : PendingModel(pending);
            }
            catch (OperationCanceledException)
            {
                if (!disposed)
                    SetError("cancelled");
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed)
                    SetError(StableError(error));
            }
            catch (Exception error)
            {
                if (!disposed)
                    SetError(
                        IsProtectedStoreFailure(error)
                            ? "protected_storage_unavailable"
                            : "unexpected");
            }
            finally
            {
                busy = false;
            }
        }

        private async Task StartCoreAsync(string type, bool retry)
        {
            HiveBroodVitalityScreenModel current = Model;
            if (busy ||
                disposed ||
                snapshot == null ||
                current == null ||
                !current.CanStart(type))
                return;
            busy = true;
            PendingGameMutation pending = null;
            try
            {
                pending = retry
                    ? await FindPendingAsync(
                        "start",
                        type,
                        lifetime.Token)
                    : null;
                if (pending == null)
                {
                    pending = NewPending(
                        HiveBroodVitalityClient.StartPath(hiveId, type),
                        type,
                        snapshot.GlobalRevision,
                        keySource.Create("start-" + type));
                    await outbox.SavePreparedAsync(
                        pending,
                        lifetime.Token);
                }
                Model = HiveBroodVitalityPresentation.Project(
                    HiveBroodVitalityScreenState.Mutating,
                    snapshot,
                    clock.Elapsed,
                    outbox.IsProtectionAvailable,
                    pendingKind: "start",
                    pendingPayload: type);
                RemoteBroodVitalityCareResponse response =
                    await client.StartCareAsync(
                        hiveId,
                        type,
                        pending.ExpectedRevision,
                        pending.IdempotencyKey,
                        lifetime.Token);
                if (disposed) return;
                snapshot = response.Snapshot;
                await DeletePendingBestEffortAsync(
                    pending.Path,
                    CancellationToken.None);
                Model = HiveBroodVitalityPresentation.Project(
                    HiveBroodVitalityScreenState.Ready,
                    snapshot,
                    clock.Elapsed,
                    outbox.IsProtectionAvailable);
            }
            catch (OperationCanceledException)
            {
                if (!disposed)
                    SetPending("start", type);
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                if (error.Error == HivePerimeterClientError.TransportFailure)
                    SetPending("start", type);
                else
                {
                    if (pending != null)
                        await DeletePendingBestEffortAsync(
                            pending.Path,
                            CancellationToken.None);
                    SetError(StableError(error));
                }
            }
            catch (Exception error)
            {
                if (!disposed)
                    SetError(
                        IsProtectedStoreFailure(error)
                            ? "protected_storage_unavailable"
                            : "unexpected");
            }
            finally
            {
                busy = false;
            }
        }

        private async Task CompleteCoreAsync(bool retry)
        {
            HiveBroodVitalityScreenModel current = Model;
            if (busy ||
                disposed ||
                snapshot == null ||
                current == null ||
                current.ActiveOperation == null ||
                !current.CanComplete(clock.Elapsed))
                return;
            Guid operationId = current.ActiveOperation.OperationId;
            string payload = operationId.ToString("N");
            busy = true;
            PendingGameMutation pending = null;
            try
            {
                pending = retry
                    ? await FindPendingAsync(
                        "complete",
                        payload,
                        lifetime.Token)
                    : null;
                if (pending == null)
                {
                    pending = NewPending(
                        HiveBroodVitalityClient.CompletePath(
                            hiveId,
                            operationId),
                        payload,
                        snapshot.GlobalRevision,
                        keySource.Create("complete-" + payload));
                    await outbox.SavePreparedAsync(
                        pending,
                        lifetime.Token);
                }
                Model = HiveBroodVitalityPresentation.Project(
                    HiveBroodVitalityScreenState.Mutating,
                    snapshot,
                    clock.Elapsed,
                    outbox.IsProtectionAvailable,
                    pendingKind: "complete",
                    pendingPayload: payload);
                RemoteBroodVitalityCareResponse response =
                    await client.CompleteCareAsync(
                        hiveId,
                        operationId,
                        pending.ExpectedRevision,
                        pending.IdempotencyKey,
                        lifetime.Token);
                if (disposed) return;
                snapshot = response.Snapshot;
                await DeletePendingBestEffortAsync(
                    pending.Path,
                    CancellationToken.None);
                Model = HiveBroodVitalityPresentation.Project(
                    HiveBroodVitalityScreenState.Ready,
                    snapshot,
                    clock.Elapsed,
                    outbox.IsProtectionAvailable);
            }
            catch (OperationCanceledException)
            {
                if (!disposed)
                    SetPending("complete", payload);
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                if (error.Error == HivePerimeterClientError.TransportFailure)
                    SetPending("complete", payload);
                else
                {
                    if (pending != null)
                        await DeletePendingBestEffortAsync(
                            pending.Path,
                            CancellationToken.None);
                    SetError(StableError(error));
                }
            }
            catch (Exception error)
            {
                if (!disposed)
                    SetError(
                        IsProtectedStoreFailure(error)
                            ? "protected_storage_unavailable"
                            : "unexpected");
            }
            finally
            {
                busy = false;
            }
        }

        private PendingGameMutation NewPending(
            string path,
            string payload,
            long expectedRevision,
            string idempotencyKey)
        {
            return new PendingGameMutation
            {
                PlayerId = snapshot.PlayerId,
                HiveId = hiveId,
                Contract = HiveBroodVitalityClient.ContractVersion,
                Path = path,
                Method = "POST",
                PayloadToken = payload,
                ExpectedRevision = expectedRevision,
                IdempotencyKey = idempotencyKey,
                CreatedAtUtc = clock.UtcNow
            };
        }

        private async Task<PendingGameMutation> ReconcilePendingAsync(
            CancellationToken cancellationToken)
        {
            if (!outbox.IsProtectionAvailable ||
                snapshot == null ||
                snapshot.PlayerId == Guid.Empty)
                return null;
            IReadOnlyList<PendingGameMutation> entries =
                await outbox.ListAsync(
                    snapshot.PlayerId,
                    hiveId,
                    HiveBroodVitalityClient.ContractVersion,
                    cancellationToken);
            if (entries.Count == 0) return null;

            RemoteBroodVitalityOperation operation =
                snapshot.Vitality == null
                    ? null
                    : snapshot.Vitality.ActiveOperation;
            PendingGameMutation keep = null;
            foreach (PendingGameMutation entry in entries)
            {
                bool start = entry.Path.IndexOf(
                    "/care/start?type=",
                    StringComparison.Ordinal) >= 0;
                bool complete = entry.Path.EndsWith(
                    "/complete",
                    StringComparison.Ordinal);
                bool validPendingStart = start &&
                    operation == null &&
                    entry.ExpectedRevision == snapshot.GlobalRevision;
                bool validPendingComplete = complete &&
                    operation != null &&
                    string.Equals(
                        entry.PayloadToken,
                        operation.OperationId.ToString("N"),
                        StringComparison.Ordinal);
                if (keep == null &&
                    (validPendingStart || validPendingComplete))
                {
                    keep = entry;
                    continue;
                }
                await DeletePendingBestEffortAsync(
                    entry.Path,
                    cancellationToken);
            }
            return keep;
        }

        private async Task<PendingGameMutation> FindPendingAsync(
            string kind,
            string payload,
            CancellationToken cancellationToken)
        {
            if (!outbox.IsProtectionAvailable ||
                snapshot == null ||
                snapshot.PlayerId == Guid.Empty)
                return null;
            IReadOnlyList<PendingGameMutation> entries =
                await outbox.ListAsync(
                    snapshot.PlayerId,
                    hiveId,
                    HiveBroodVitalityClient.ContractVersion,
                    cancellationToken);
            return entries.FirstOrDefault(entry =>
                string.Equals(
                    entry.PayloadToken,
                    payload,
                    StringComparison.Ordinal) &&
                (string.Equals(kind, "start", StringComparison.Ordinal)
                    ? entry.Path.IndexOf(
                        "/care/start?type=",
                        StringComparison.Ordinal) >= 0
                    : entry.Path.EndsWith(
                        "/complete",
                        StringComparison.Ordinal)));
        }

        private async Task DeletePendingBestEffortAsync(
            string path,
            CancellationToken cancellationToken)
        {
            if (!outbox.IsProtectionAvailable ||
                snapshot == null ||
                snapshot.PlayerId == Guid.Empty ||
                string.IsNullOrWhiteSpace(path))
                return;
            try
            {
                await outbox.DeletePartitionAsync(
                    snapshot.PlayerId,
                    hiveId,
                    HiveBroodVitalityClient.ContractVersion,
                    path,
                    cancellationToken);
            }
            catch
            {
            }
        }

        private HiveBroodVitalityScreenModel PendingModel(
            PendingGameMutation pending)
        {
            string kind = pending.Path.IndexOf(
                "/care/start?type=",
                StringComparison.Ordinal) >= 0
                ? "start"
                : "complete";
            return HiveBroodVitalityPresentation.Project(
                HiveBroodVitalityScreenState.PendingConfirmation,
                snapshot,
                clock.Elapsed,
                outbox.IsProtectionAvailable,
                pendingKind: kind,
                pendingPayload: pending.PayloadToken);
        }

        private void SetPending(string kind, string payload)
        {
            Model = HiveBroodVitalityPresentation.Project(
                HiveBroodVitalityScreenState.PendingConfirmation,
                snapshot,
                clock.Elapsed,
                outbox.IsProtectionAvailable,
                "network_unavailable",
                kind,
                payload);
        }

        private void SetError(string code)
        {
            Model = HiveBroodVitalityPresentation.Project(
                HiveBroodVitalityScreenState.Error,
                snapshot,
                clock.Elapsed,
                outbox.IsProtectionAvailable,
                code);
        }

        private static bool IsProtectedStoreFailure(Exception error)
        {
            return error is InvalidOperationException &&
                error.Message.StartsWith(
                    "game.mutation.",
                    StringComparison.Ordinal);
        }

        private static string StableError(HivePerimeterClientException error)
        {
            string remote = error.Message ?? string.Empty;
            switch (remote)
            {
                case "game.vitality_not_initialized":
                    return "not_initialized";
                case "game.vitality_busy":
                    return "busy";
                case "game.vitality_not_ready":
                    return "not_ready";
                case "game.vitality_not_found":
                    return "operation_not_found";
                case "game.insufficient_resources":
                    return "insufficient_resources";
                case "game.revision_conflict":
                    return "revision_conflict";
                case "game.idempotency_conflict":
                    return "idempotency_conflict";
                case "game.unavailable":
                    return "server_unavailable";
            }

            switch (error.Error)
            {
                case HivePerimeterClientError.NotConfigured:
                    return "not_configured";
                case HivePerimeterClientError.AuthenticationRequired:
                    return "session_required";
                case HivePerimeterClientError.InvalidRequest:
                    return "invalid_request";
                case HivePerimeterClientError.InvalidResponse:
                    return "invalid_response";
                case HivePerimeterClientError.TransportFailure:
                    return "network_unavailable";
                default:
                    return "unexpected";
            }
        }
    }
}
