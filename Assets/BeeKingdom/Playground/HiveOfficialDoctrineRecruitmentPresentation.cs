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
    public enum HiveDoctrineRecruitmentScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        OfflineReadOnly = 3,
        Mutating = 4,
        PendingConfirmation = 5,
        Error = 6
    }

    public sealed class HiveDoctrineRecruitmentOfferModel
    {
        internal HiveDoctrineRecruitmentOfferModel(
            RemoteDoctrineRecruitmentOffer source)
        {
            Family = source.Family ?? string.Empty;
            BatchSize = source.BatchSize;
            HoneyCost = source.HoneyCost;
            PollenCost = source.PollenCost;
            Duration = source.Duration;
        }

        public string Family { get; }
        public int BatchSize { get; }
        public long HoneyCost { get; }
        public long PollenCost { get; }
        public TimeSpan Duration { get; }
    }

    public sealed class HiveDoctrineRecruitmentOperationModel
    {
        internal HiveDoctrineRecruitmentOperationModel(
            RemoteDoctrineRecruitmentOperation source)
        {
            OperationId = source.OperationId;
            Family = source.Family ?? string.Empty;
            BatchSize = source.BatchSize;
            StartedAtUtc = source.StartedAtUtc;
            EndsAtUtc = source.EndsAtUtc;
            Status = source.Status ?? string.Empty;
        }

        public Guid OperationId { get; }
        public string Family { get; }
        public int BatchSize { get; }
        public DateTimeOffset StartedAtUtc { get; }
        public DateTimeOffset EndsAtUtc { get; }
        public string Status { get; }
        public bool AwaitingCompletion =>
            string.Equals(
                Status,
                HiveDoctrineRecruitmentClient.AwaitingCompletionStatus,
                StringComparison.Ordinal);
    }

    public sealed class HiveDoctrineRecruitmentScreenModel
    {
        internal HiveDoctrineRecruitmentScreenModel(
            HiveDoctrineRecruitmentScreenState state,
            string errorCode,
            string successCode,
            bool protectedOutboxAvailable,
            string pendingKind,
            string pendingFamily,
            Guid pendingOperationId,
            long revision,
            DateTimeOffset serverTimeUtc,
            TimeSpan projectedAt,
            DateTimeOffset cachedAtUtc,
            IReadOnlyList<HiveDoctrineRecruitmentOfferModel> offers,
            IReadOnlyDictionary<string, long> counts,
            IReadOnlyDictionary<string, long> balances,
            IReadOnlyList<string> legacyRoles,
            HiveDoctrineRecruitmentOperationModel activeOperation,
            int populationCapacity,
            long populationUsed)
        {
            State = state;
            ErrorCode = errorCode ?? string.Empty;
            SuccessCode = successCode ?? string.Empty;
            ProtectedOutboxAvailable = protectedOutboxAvailable;
            PendingKind = pendingKind ?? string.Empty;
            PendingFamily = pendingFamily ?? string.Empty;
            PendingOperationId = pendingOperationId;
            Revision = Math.Max(0L, revision);
            ServerTimeUtc = serverTimeUtc;
            ProjectedAt = projectedAt < TimeSpan.Zero
                ? TimeSpan.Zero
                : projectedAt;
            CachedAtUtc = cachedAtUtc;
            Offers = offers ?? Array.Empty
                <HiveDoctrineRecruitmentOfferModel>();
            Counts = counts ?? new Dictionary<string, long>();
            Balances = balances ?? new Dictionary<string, long>();
            LegacyRoles = legacyRoles ?? Array.Empty<string>();
            ActiveOperation = activeOperation;
            PopulationCapacity = populationCapacity;
            PopulationUsed = populationUsed;
            FormationRoster =
                HiveFormationReadinessProjection.ProjectOfficial(
                    Counts,
                    LegacyRoles);
        }

        public HiveDoctrineRecruitmentScreenState State { get; }
        public string ErrorCode { get; }
        public string SuccessCode { get; }
        public bool ProtectedOutboxAvailable { get; }
        public string PendingKind { get; }
        public string PendingFamily { get; }
        public Guid PendingOperationId { get; }
        public long Revision { get; }
        public DateTimeOffset ServerTimeUtc { get; }
        public TimeSpan ProjectedAt { get; }
        public DateTimeOffset CachedAtUtc { get; }
        public IReadOnlyList<HiveDoctrineRecruitmentOfferModel> Offers
        {
            get;
        }
        public IReadOnlyDictionary<string, long> Counts { get; }
        public IReadOnlyDictionary<string, long> Balances { get; }
        public IReadOnlyList<string> LegacyRoles { get; }
        public HiveDoctrineRecruitmentOperationModel ActiveOperation
        {
            get;
        }
        public int PopulationCapacity { get; }
        public long PopulationUsed { get; }
        public HiveFormationReadinessSnapshot FormationRoster { get; }
        public bool IsReadOnly =>
            State == HiveDoctrineRecruitmentScreenState.OfflineReadOnly;
        public bool IsPending =>
            State ==
            HiveDoctrineRecruitmentScreenState.PendingConfirmation;

        public HiveDoctrineRecruitmentOfferModel FindOffer(string family)
        {
            return Offers.FirstOrDefault(
                offer => string.Equals(
                    offer.Family,
                    family,
                    StringComparison.Ordinal));
        }

        public bool CanStart(string family)
        {
            HiveDoctrineRecruitmentOfferModel offer = FindOffer(family);
            if (offer == null ||
                !ProtectedOutboxAvailable ||
                ActiveOperation != null ||
                IsReadOnly)
                return false;
            bool retry = IsPending &&
                string.Equals(PendingKind, "start", StringComparison.Ordinal) &&
                string.Equals(
                    PendingFamily,
                    family,
                    StringComparison.Ordinal);
            if (State != HiveDoctrineRecruitmentScreenState.Ready &&
                !retry)
                return false;
            return Balance("honey") >= offer.HoneyCost &&
                Balance("pollen") >= offer.PollenCost;
        }

        public bool CanClaim()
        {
            if (!ProtectedOutboxAvailable ||
                ActiveOperation == null ||
                !ActiveOperation.AwaitingCompletion ||
                IsReadOnly)
                return false;
            bool retry = IsPending &&
                string.Equals(PendingKind, "claim", StringComparison.Ordinal) &&
                PendingOperationId == ActiveOperation.OperationId &&
                string.Equals(
                    PendingFamily,
                    ActiveOperation.Family,
                    StringComparison.Ordinal);
            return State == HiveDoctrineRecruitmentScreenState.Ready ||
                retry;
        }

        public bool CanRetry(string family)
        {
            return IsPending &&
                string.Equals(
                    PendingFamily,
                    family,
                    StringComparison.Ordinal) &&
                (string.Equals(
                     PendingKind,
                     "start",
                     StringComparison.Ordinal) ||
                 string.Equals(
                     PendingKind,
                     "claim",
                     StringComparison.Ordinal));
        }

        public long Count(string family)
        {
            return Counts.TryGetValue(family, out long value)
                ? value
                : 0L;
        }

        public long Balance(string resource)
        {
            return Balances.TryGetValue(resource, out long value)
                ? value
                : 0L;
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
            return remaining < TimeSpan.Zero
                ? TimeSpan.Zero
                : remaining;
        }

        public double Progress01(TimeSpan currentElapsed)
        {
            if (ActiveOperation == null) return 0d;
            TimeSpan total =
                ActiveOperation.EndsAtUtc -
                ActiveOperation.StartedAtUtc;
            if (total <= TimeSpan.Zero) return 0d;
            return Math.Max(
                0d,
                Math.Min(
                    1d,
                    1d -
                    Remaining(currentElapsed).TotalSeconds /
                    total.TotalSeconds));
        }
    }

    public static class HiveOfficialDoctrineRecruitmentPresentation
    {
        public static HiveDoctrineRecruitmentScreenModel NotConfigured()
        {
            return Empty(
                HiveDoctrineRecruitmentScreenState.NotConfigured,
                string.Empty,
                false);
        }

        public static HiveDoctrineRecruitmentScreenModel Project(
            HiveDoctrineRecruitmentScreenState state,
            RemoteDoctrineRecruitmentSnapshot snapshot,
            TimeSpan projectedAt,
            bool protectedOutboxAvailable,
            string errorCode = "",
            string successCode = "",
            string pendingKind = "",
            string pendingFamily = "",
            Guid pendingOperationId = default(Guid),
            DateTimeOffset cachedAtUtc = default(DateTimeOffset))
        {
            if (snapshot == null)
                return Empty(
                    state,
                    errorCode,
                    protectedOutboxAvailable);
            return new HiveDoctrineRecruitmentScreenModel(
                state,
                errorCode,
                successCode,
                protectedOutboxAvailable,
                pendingKind,
                pendingFamily,
                pendingOperationId,
                snapshot.Revision,
                snapshot.ServerTimeUtc,
                projectedAt,
                cachedAtUtc,
                snapshot.Offers.Select(
                        offer =>
                            new HiveDoctrineRecruitmentOfferModel(offer))
                    .ToArray(),
                new Dictionary<string, long>(
                    snapshot.Counts,
                    StringComparer.Ordinal),
                snapshot.Balances.ToDictionary(
                    item => item.Key,
                    item => item.Value.Amount,
                    StringComparer.Ordinal),
                snapshot.LegacyRoles.ToArray(),
                snapshot.ActiveOperation == null
                    ? null
                    : new HiveDoctrineRecruitmentOperationModel(
                        snapshot.ActiveOperation),
                snapshot.PopulationCapacity,
                snapshot.PopulationUsed);
        }

        private static HiveDoctrineRecruitmentScreenModel Empty(
            HiveDoctrineRecruitmentScreenState state,
            string errorCode,
            bool protectedOutboxAvailable)
        {
            return new HiveDoctrineRecruitmentScreenModel(
                state,
                errorCode,
                string.Empty,
                protectedOutboxAvailable,
                string.Empty,
                string.Empty,
                Guid.Empty,
                0L,
                default(DateTimeOffset),
                TimeSpan.Zero,
                default(DateTimeOffset),
                Array.Empty<HiveDoctrineRecruitmentOfferModel>(),
                new Dictionary<string, long>(),
                new Dictionary<string, long>(),
                Array.Empty<string>(),
                null,
                0,
                0L);
        }
    }

    public interface IHiveDoctrineRecruitmentClock
    {
        TimeSpan Elapsed { get; }
        DateTimeOffset UtcNow { get; }
    }

    public sealed class StopwatchHiveDoctrineRecruitmentClock :
        IHiveDoctrineRecruitmentClock
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        public TimeSpan Elapsed => stopwatch.Elapsed;
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    public interface IHiveDoctrineRecruitmentKeySource
    {
        string Create(string operation);
    }

    public sealed class SessionHiveDoctrineRecruitmentKeySource :
        IHiveDoctrineRecruitmentKeySource
    {
        public string Create(string operation)
        {
            string safe = string.IsNullOrWhiteSpace(operation)
                ? "mutation"
                : operation.Trim();
            return "mobile-recruit-" + safe + "-" +
                Guid.NewGuid().ToString("N");
        }
    }

    public interface IHiveDoctrineRecruitmentPanelController
    {
        HiveDoctrineRecruitmentScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        TimeSpan Elapsed { get; }
        void Refresh();
        void Start(string family);
        void Claim();
        void Retry();
    }

    public sealed class UnavailableHiveDoctrineRecruitmentPanelController :
        IHiveDoctrineRecruitmentPanelController
    {
        private readonly HiveDoctrineRecruitmentScreenModel model =
            HiveOfficialDoctrineRecruitmentPresentation.NotConfigured();
        public HiveDoctrineRecruitmentScreenModel Model => model;
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public TimeSpan Elapsed => TimeSpan.Zero;
        public void Refresh() { }
        public void Start(string family) { }
        public void Claim() { }
        public void Retry() { }
    }

    public sealed class HiveDoctrineRecruitmentPanelController :
        IHiveDoctrineRecruitmentPanelController,
        IDisposable
    {
        private readonly IHiveDoctrineRecruitmentClient client;
        private readonly Guid hiveId;
        private readonly ProtectedGameMutationOutbox outbox;
        private readonly IHiveDoctrineRecruitmentKeySource keySource;
        private readonly IHiveDoctrineRecruitmentClock clock;
        private readonly CancellationTokenSource lifetime =
            new CancellationTokenSource();
        private RemoteDoctrineRecruitmentSnapshot snapshot;
        private bool disposed;
        private bool busy;

        public HiveDoctrineRecruitmentPanelController(
            IHiveDoctrineRecruitmentClient client,
            Guid hiveId,
            ProtectedGameMutationOutbox outbox,
            IHiveDoctrineRecruitmentKeySource keySource = null,
            IHiveDoctrineRecruitmentClock clock = null)
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
                keySource ?? new SessionHiveDoctrineRecruitmentKeySource();
            this.clock =
                clock ?? new StopwatchHiveDoctrineRecruitmentClock();
            Model = HiveOfficialDoctrineRecruitmentPresentation.Project(
                HiveDoctrineRecruitmentScreenState.Loading,
                null,
                this.clock.Elapsed,
                outbox.IsProtectionAvailable);
        }

        public HiveDoctrineRecruitmentScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;
        public TimeSpan Elapsed => clock.Elapsed;

        public void Refresh()
        {
            RefreshInsideLifetime();
        }

        public void Start(string family)
        {
            StartInsideLifetime(family, false);
        }

        public void Claim()
        {
            ClaimInsideLifetime(false);
        }

        public void Retry()
        {
            RetryInsideLifetime();
        }

        public Task RefreshForProofAsync()
        {
            return RefreshCoreAsync();
        }

        public Task StartForProofAsync(
            string family,
            bool retry = false)
        {
            return retry
                ? RetryPendingCoreAsync()
                : StartCoreAsync(family, false);
        }

        public Task ClaimForProofAsync(bool retry = false)
        {
            return retry
                ? RetryPendingCoreAsync()
                : ClaimCoreAsync(false);
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

        private async void StartInsideLifetime(
            string family,
            bool retry)
        {
            await StartCoreAsync(family, retry);
        }

        private async void ClaimInsideLifetime(bool retry)
        {
            await ClaimCoreAsync(retry);
        }

        private async void RetryInsideLifetime()
        {
            HiveDoctrineRecruitmentScreenModel current = Model;
            if (current == null || !current.IsPending) return;
            await RetryPendingCoreAsync();
        }

        private async Task RefreshCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            Model = HiveOfficialDoctrineRecruitmentPresentation.Project(
                HiveDoctrineRecruitmentScreenState.Loading,
                snapshot,
                clock.Elapsed,
                outbox.IsProtectionAvailable);
            try
            {
                RemoteDoctrineRecruitmentSnapshot result =
                    await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                snapshot = result;
                if (client.LastReadSource ==
                    GameReadSource.ProtectedCache)
                {
                    Model =
                        HiveOfficialDoctrineRecruitmentPresentation.Project(
                            HiveDoctrineRecruitmentScreenState
                                .OfflineReadOnly,
                            snapshot,
                            clock.Elapsed,
                            outbox.IsProtectionAvailable,
                            cachedAtUtc: client.LastReadCachedAtUtc);
                    return;
                }

                PendingDescriptor pending =
                    await LoadPendingAsync(lifetime.Token);
                if (disposed) return;
                if (outbox.LastLoadDetectedCorruption)
                {
                    SetError("mutation_recovery_refresh_required");
                    return;
                }
                Model = pending == null
                    ? HiveOfficialDoctrineRecruitmentPresentation.Project(
                        HiveDoctrineRecruitmentScreenState.Ready,
                        snapshot,
                        clock.Elapsed,
                        outbox.IsProtectionAvailable)
                    : PendingModel(pending);
            }
            catch (OperationCanceledException)
            {
                if (!disposed) SetError("cancelled");
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed) SetError(StableError(error));
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

        private async Task StartCoreAsync(
            string family,
            bool retry)
        {
            HiveDoctrineRecruitmentScreenModel current = Model;
            if (busy ||
                disposed ||
                snapshot == null ||
                current == null ||
                !current.CanStart(family))
                return;
            busy = true;
            PendingGameMutation pending = null;
            try
            {
                pending = retry
                    ? await outbox.TryLoadAsync(
                        snapshot.PlayerId,
                        hiveId,
                        HiveDoctrineRecruitmentClient.ContractVersion,
                        HiveDoctrineRecruitmentClient.StartPath(hiveId),
                        lifetime.Token)
                    : null;
                if (outbox.LastLoadDetectedCorruption)
                {
                    SetError("mutation_recovery_refresh_required");
                    return;
                }
                if (pending != null &&
                    (!string.Equals(
                         pending.PayloadToken,
                         family,
                         StringComparison.Ordinal) ||
                     pending.ExpectedRevision != current.Revision))
                {
                    SetError("mutation_changed_refresh_required");
                    return;
                }
                if (pending == null)
                {
                    pending = NewPending(
                        HiveDoctrineRecruitmentClient.StartPath(hiveId),
                        family,
                        current.Revision,
                        keySource.Create("start-" + family));
                    await outbox.SavePreparedAsync(
                        pending,
                        lifetime.Token);
                }

                Model = HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.Mutating,
                    snapshot,
                    clock.Elapsed,
                    outbox.IsProtectionAvailable,
                    pendingKind: "start",
                    pendingFamily: family);
                RemoteDoctrineRecruitmentResponse response =
                    await client.StartAsync(
                        hiveId,
                        family,
                        pending.ExpectedRevision,
                        pending.IdempotencyKey,
                        lifetime.Token);
                if (disposed) return;
                snapshot = response.Snapshot;
                await DeletePartitionBestEffortAsync(
                    pending.Path,
                    CancellationToken.None);
                Model = HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.Ready,
                    snapshot,
                    clock.Elapsed,
                    outbox.IsProtectionAvailable,
                    successCode: response.Receipt.Code);
            }
            catch (OperationCanceledException)
            {
                if (!disposed)
                    Model = PendingModel(
                        new PendingDescriptor(
                            "start",
                            family,
                            Guid.Empty));
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                if (error.Error ==
                    HivePerimeterClientError.TransportFailure)
                    Model = PendingModel(
                        new PendingDescriptor(
                            "start",
                            family,
                            Guid.Empty));
                else
                {
                    if (pending != null)
                        await DeletePartitionBestEffortAsync(
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

        private async Task ClaimCoreAsync(bool retry)
        {
            HiveDoctrineRecruitmentScreenModel current = Model;
            if (busy ||
                disposed ||
                snapshot == null ||
                current == null ||
                current.ActiveOperation == null ||
                !current.CanClaim())
                return;
            busy = true;
            HiveDoctrineRecruitmentOperationModel operation =
                current.ActiveOperation;
            string path = HiveDoctrineRecruitmentClient.ClaimPath(
                hiveId,
                operation.OperationId);
            string token = ClaimToken(
                operation.Family,
                operation.OperationId);
            PendingGameMutation pending = null;
            try
            {
                pending = retry
                    ? await outbox.TryLoadAsync(
                        snapshot.PlayerId,
                        hiveId,
                        HiveDoctrineRecruitmentClient.ContractVersion,
                        path,
                        lifetime.Token)
                    : null;
                if (outbox.LastLoadDetectedCorruption)
                {
                    SetError("mutation_recovery_refresh_required");
                    return;
                }
                if (pending != null &&
                    (!string.Equals(
                         pending.PayloadToken,
                         token,
                         StringComparison.Ordinal) ||
                     pending.ExpectedRevision != current.Revision))
                {
                    SetError("mutation_changed_refresh_required");
                    return;
                }
                if (pending == null)
                {
                    pending = NewPending(
                        path,
                        token,
                        current.Revision,
                        keySource.Create(
                            "claim-" + operation.Family));
                    await outbox.SavePreparedAsync(
                        pending,
                        lifetime.Token);
                }

                Model = HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.Mutating,
                    snapshot,
                    clock.Elapsed,
                    outbox.IsProtectionAvailable,
                    pendingKind: "claim",
                    pendingFamily: operation.Family,
                    pendingOperationId: operation.OperationId);
                RemoteDoctrineRecruitmentResponse response =
                    await client.ClaimAsync(
                        hiveId,
                        operation.OperationId,
                        operation.Family,
                        pending.ExpectedRevision,
                        pending.IdempotencyKey,
                        lifetime.Token);
                if (disposed) return;
                snapshot = response.Snapshot;
                await DeleteContractBestEffortAsync(
                    CancellationToken.None);
                Model = HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.Ready,
                    snapshot,
                    clock.Elapsed,
                    outbox.IsProtectionAvailable,
                    successCode: response.Receipt.Code);
            }
            catch (OperationCanceledException)
            {
                if (!disposed)
                    Model = PendingModel(
                        new PendingDescriptor(
                            "claim",
                            operation.Family,
                            operation.OperationId));
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                if (error.Error ==
                    HivePerimeterClientError.TransportFailure)
                    Model = PendingModel(
                        new PendingDescriptor(
                            "claim",
                            operation.Family,
                            operation.OperationId));
                else
                {
                    if (pending != null)
                        await DeletePartitionBestEffortAsync(
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

        private async Task RetryPendingCoreAsync()
        {
            if (busy || disposed || snapshot == null) return;
            busy = true;
            PendingGameMutation pending = null;
            PendingDescriptor descriptor = null;
            try
            {
                IReadOnlyList<PendingGameMutation> entries =
                    await outbox.ListAsync(
                        snapshot.PlayerId,
                        hiveId,
                        HiveDoctrineRecruitmentClient.ContractVersion,
                        lifetime.Token);
                pending = entries.LastOrDefault();
                if (outbox.LastLoadDetectedCorruption)
                {
                    SetError("mutation_recovery_refresh_required");
                    return;
                }
                if (pending == null)
                {
                    SetError("mutation_changed_refresh_required");
                    return;
                }

                if (string.Equals(
                        pending.Path,
                        HiveDoctrineRecruitmentClient.StartPath(hiveId),
                        StringComparison.Ordinal))
                {
                    descriptor = new PendingDescriptor(
                        "start",
                        pending.PayloadToken,
                        Guid.Empty);
                    Model =
                        HiveOfficialDoctrineRecruitmentPresentation.Project(
                            HiveDoctrineRecruitmentScreenState.Mutating,
                            snapshot,
                            clock.Elapsed,
                            outbox.IsProtectionAvailable,
                            pendingKind: descriptor.Kind,
                            pendingFamily: descriptor.Family);
                    RemoteDoctrineRecruitmentResponse response =
                        await client.StartAsync(
                            hiveId,
                            descriptor.Family,
                            pending.ExpectedRevision,
                            pending.IdempotencyKey,
                            lifetime.Token);
                    if (disposed) return;
                    snapshot = response.Snapshot;
                    await DeletePartitionBestEffortAsync(
                        pending.Path,
                        CancellationToken.None);
                    Model =
                        HiveOfficialDoctrineRecruitmentPresentation.Project(
                            HiveDoctrineRecruitmentScreenState.Ready,
                            snapshot,
                            clock.Elapsed,
                            outbox.IsProtectionAvailable,
                            successCode: response.Receipt.Code);
                    return;
                }

                if (!TryParseClaimToken(
                        pending.PayloadToken,
                        out string family,
                        out Guid operationId) ||
                    !string.Equals(
                        pending.Path,
                        HiveDoctrineRecruitmentClient.ClaimPath(
                            hiveId,
                            operationId),
                        StringComparison.Ordinal))
                {
                    SetError("mutation_changed_refresh_required");
                    return;
                }

                descriptor = new PendingDescriptor(
                    "claim",
                    family,
                    operationId);
                Model = HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.Mutating,
                    snapshot,
                    clock.Elapsed,
                    outbox.IsProtectionAvailable,
                    pendingKind: descriptor.Kind,
                    pendingFamily: descriptor.Family,
                    pendingOperationId: descriptor.OperationId);
                RemoteDoctrineRecruitmentResponse claim =
                    await client.ClaimAsync(
                        hiveId,
                        descriptor.OperationId,
                        descriptor.Family,
                        pending.ExpectedRevision,
                        pending.IdempotencyKey,
                        lifetime.Token);
                if (disposed) return;
                snapshot = claim.Snapshot;
                await DeleteContractBestEffortAsync(
                    CancellationToken.None);
                Model = HiveOfficialDoctrineRecruitmentPresentation.Project(
                    HiveDoctrineRecruitmentScreenState.Ready,
                    snapshot,
                    clock.Elapsed,
                    outbox.IsProtectionAvailable,
                    successCode: claim.Receipt.Code);
            }
            catch (OperationCanceledException)
            {
                if (!disposed && descriptor != null)
                    Model = PendingModel(descriptor);
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                if (error.Error ==
                        HivePerimeterClientError.TransportFailure &&
                    descriptor != null)
                {
                    Model = PendingModel(descriptor);
                }
                else
                {
                    if (pending != null)
                        await DeletePartitionBestEffortAsync(
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
            string payloadToken,
            long expectedRevision,
            string idempotencyKey)
        {
            return new PendingGameMutation
            {
                PlayerId = snapshot.PlayerId,
                HiveId = hiveId,
                Contract =
                    HiveDoctrineRecruitmentClient.ContractVersion,
                Path = path,
                Method = "POST",
                PayloadToken = payloadToken,
                ExpectedRevision = expectedRevision,
                IdempotencyKey = idempotencyKey,
                CreatedAtUtc = snapshot.ServerTimeUtc ==
                    default(DateTimeOffset)
                    ? clock.UtcNow
                    : snapshot.ServerTimeUtc
            };
        }

        private async Task<PendingDescriptor> LoadPendingAsync(
            CancellationToken cancellationToken)
        {
            if (!outbox.IsProtectionAvailable ||
                snapshot == null)
                return null;
            IReadOnlyList<PendingGameMutation> entries =
                await outbox.ListAsync(
                    snapshot.PlayerId,
                    hiveId,
                    HiveDoctrineRecruitmentClient.ContractVersion,
                    cancellationToken);
            PendingGameMutation pending = entries.LastOrDefault();
            if (pending == null) return null;
            if (string.Equals(
                    pending.Path,
                    HiveDoctrineRecruitmentClient.StartPath(hiveId),
                    StringComparison.Ordinal))
                return new PendingDescriptor(
                    "start",
                    pending.PayloadToken,
                    Guid.Empty);
            if (TryParseClaimToken(
                    pending.PayloadToken,
                    out string family,
                    out Guid operationId))
                return new PendingDescriptor(
                    "claim",
                    family,
                    operationId);
            return new PendingDescriptor(
                "unknown",
                string.Empty,
                Guid.Empty);
        }

        private HiveDoctrineRecruitmentScreenModel PendingModel(
            PendingDescriptor pending)
        {
            return HiveOfficialDoctrineRecruitmentPresentation.Project(
                HiveDoctrineRecruitmentScreenState.PendingConfirmation,
                snapshot,
                clock.Elapsed,
                outbox.IsProtectionAvailable,
                pendingKind: pending.Kind,
                pendingFamily: pending.Family,
                pendingOperationId: pending.OperationId);
        }

        private async Task DeletePartitionBestEffortAsync(
            string path,
            CancellationToken cancellationToken)
        {
            if (!outbox.IsProtectionAvailable ||
                snapshot == null ||
                snapshot.PlayerId == Guid.Empty)
                return;
            try
            {
                await outbox.DeletePartitionAsync(
                    snapshot.PlayerId,
                    hiveId,
                    HiveDoctrineRecruitmentClient.ContractVersion,
                    path,
                    cancellationToken);
            }
            catch
            {
            }
        }

        private async Task DeleteContractBestEffortAsync(
            CancellationToken cancellationToken)
        {
            if (!outbox.IsProtectionAvailable ||
                snapshot == null ||
                snapshot.PlayerId == Guid.Empty)
                return;
            try
            {
                await outbox.DeleteContractAsync(
                    snapshot.PlayerId,
                    hiveId,
                    HiveDoctrineRecruitmentClient.ContractVersion,
                    cancellationToken);
            }
            catch
            {
            }
        }

        private void SetError(string code)
        {
            Model = HiveOfficialDoctrineRecruitmentPresentation.Project(
                HiveDoctrineRecruitmentScreenState.Error,
                snapshot,
                clock.Elapsed,
                outbox.IsProtectionAvailable,
                code);
        }

        private static string ClaimToken(
            string family,
            Guid operationId)
        {
            return family + "." + operationId.ToString("N");
        }

        private static bool TryParseClaimToken(
            string token,
            out string family,
            out Guid operationId)
        {
            family = string.Empty;
            operationId = Guid.Empty;
            if (string.IsNullOrWhiteSpace(token)) return false;
            string[] parts = token.Split('.');
            if (parts.Length != 2 ||
                !Guid.TryParseExact(
                    parts[1],
                    "N",
                    out operationId) ||
                operationId == Guid.Empty)
                return false;
            family = parts[0];
            return family == "guardians" ||
                family == "wingrunners" ||
                family == "darters";
        }

        private static bool IsProtectedStoreFailure(Exception error)
        {
            return error is InvalidOperationException &&
                error.Message.StartsWith(
                    "game.mutation.",
                    StringComparison.Ordinal);
        }

        private static string StableError(
            HivePerimeterClientException error)
        {
            string remote = error.Message ?? string.Empty;
            switch (remote)
            {
                case "game.recruitment_precondition_failed":
                    return "precondition_failed";
                case "game.recruitment_not_complete":
                    return "not_complete";
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

        private sealed class PendingDescriptor
        {
            public PendingDescriptor(
                string kind,
                string family,
                Guid operationId)
            {
                Kind = kind ?? string.Empty;
                Family = family ?? string.Empty;
                OperationId = operationId;
            }

            public string Kind { get; }
            public string Family { get; }
            public Guid OperationId { get; }
        }
    }
}
