using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    public enum HiveSquadReservationScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        OfflineReadOnly = 3,
        Mutating = 4,
        PendingConfirmation = 5,
        Error = 6
    }

    public sealed class HiveSquadReservationScreenModel
    {
        internal HiveSquadReservationScreenModel(
            HiveSquadReservationScreenState state,
            string errorCode,
            long rosterRevision,
            long reservationRevision,
            int capacity,
            long rosterGuardians,
            long rosterWingrunners,
            long rosterDarters,
            long availableGuardians,
            long availableWingrunners,
            long availableDarters,
            long reservedGuardians,
            long reservedWingrunners,
            long reservedDarters,
            string reservationId,
            bool protectedOutboxAvailable,
            bool readOnlyOffline,
            DateTimeOffset cachedAtUtc,
            string pendingKind,
            int pendingGuardians,
            int pendingWingrunners,
            int pendingDarters,
            string successCode)
        {
            State = state;
            ErrorCode = errorCode ?? string.Empty;
            RosterRevision = Math.Max(0L, rosterRevision);
            ReservationRevision = Math.Max(0L, reservationRevision);
            Capacity = Math.Max(1, capacity);
            RosterGuardians = Math.Max(0L, rosterGuardians);
            RosterWingrunners = Math.Max(0L, rosterWingrunners);
            RosterDarters = Math.Max(0L, rosterDarters);
            AvailableGuardians = Math.Max(0L, availableGuardians);
            AvailableWingrunners = Math.Max(0L, availableWingrunners);
            AvailableDarters = Math.Max(0L, availableDarters);
            ReservedGuardians = Math.Max(0L, reservedGuardians);
            ReservedWingrunners = Math.Max(0L, reservedWingrunners);
            ReservedDarters = Math.Max(0L, reservedDarters);
            ReservationId = reservationId ?? string.Empty;
            ProtectedOutboxAvailable = protectedOutboxAvailable;
            ReadOnlyOffline = readOnlyOffline;
            CachedAtUtc = cachedAtUtc;
            PendingKind = pendingKind ?? string.Empty;
            PendingGuardians = Math.Max(0, pendingGuardians);
            PendingWingrunners = Math.Max(0, pendingWingrunners);
            PendingDarters = Math.Max(0, pendingDarters);
            SuccessCode = successCode ?? string.Empty;
        }

        public HiveSquadReservationScreenState State { get; }
        public string ErrorCode { get; }
        public long RosterRevision { get; }
        public long ReservationRevision { get; }
        public int Capacity { get; }
        public long RosterGuardians { get; }
        public long RosterWingrunners { get; }
        public long RosterDarters { get; }
        public long AvailableGuardians { get; }
        public long AvailableWingrunners { get; }
        public long AvailableDarters { get; }
        public long ReservedGuardians { get; }
        public long ReservedWingrunners { get; }
        public long ReservedDarters { get; }
        public string ReservationId { get; }
        public bool ProtectedOutboxAvailable { get; }
        public bool ReadOnlyOffline { get; }
        public DateTimeOffset CachedAtUtc { get; }
        public string PendingKind { get; }
        public int PendingGuardians { get; }
        public int PendingWingrunners { get; }
        public int PendingDarters { get; }
        public string SuccessCode { get; }

        public long ReservedTotal =>
            SafeTotal(
                ReservedGuardians,
                ReservedWingrunners,
                ReservedDarters);

        public bool HasReservation =>
            !string.IsNullOrWhiteSpace(ReservationId) &&
            ReservedTotal > 0;

        public bool IsPending =>
            State ==
            HiveSquadReservationScreenState.PendingConfirmation;

        public bool CanRetry =>
            IsPending && ProtectedOutboxAvailable;

        public bool CanRelease =>
            State == HiveSquadReservationScreenState.Ready &&
            HasReservation &&
            ProtectedOutboxAvailable &&
            !ReadOnlyOffline;

        public bool CanCommit(
            HiveSquadCompositionSnapshot composition)
        {
            if (composition == null ||
                State != HiveSquadReservationScreenState.Ready ||
                HasReservation ||
                !ProtectedOutboxAvailable ||
                ReadOnlyOffline ||
                composition.IsEmpty ||
                composition.Total > Capacity)
                return false;
            return composition.Guardians <= RosterGuardians &&
                composition.Wingrunners <= RosterWingrunners &&
                composition.Darters <= RosterDarters;
        }

        public HiveSquadCompositionSnapshot ReservedComposition()
        {
            return new HiveSquadCompositionSnapshot(
                Capacity,
                SafeInt(ReservedGuardians),
                SafeInt(ReservedWingrunners),
                SafeInt(ReservedDarters));
        }

        private static int SafeInt(long value)
        {
            return (int)Math.Min(int.MaxValue, Math.Max(0L, value));
        }

        private static long SafeTotal(
            long guardians,
            long wingrunners,
            long darters)
        {
            try
            {
                return checked(guardians + wingrunners + darters);
            }
            catch (OverflowException)
            {
                return long.MaxValue;
            }
        }
    }

    public static class HiveOfficialSquadReservationPresentation
    {
        public static HiveSquadReservationScreenModel NotConfigured(
            bool protectedOutboxAvailable = false)
        {
            return Project(
                HiveSquadReservationScreenState.NotConfigured,
                null,
                protectedOutboxAvailable);
        }

        public static HiveSquadReservationScreenModel Project(
            HiveSquadReservationScreenState state,
            RemoteSquadReservationSnapshot snapshot,
            bool protectedOutboxAvailable,
            string errorCode = "",
            bool readOnlyOffline = false,
            DateTimeOffset cachedAtUtc = default(DateTimeOffset),
            string pendingKind = "",
            int pendingGuardians = 0,
            int pendingWingrunners = 0,
            int pendingDarters = 0,
            string successCode = "")
        {
            return new HiveSquadReservationScreenModel(
                state,
                errorCode,
                snapshot == null ? 0L : snapshot.RosterRevision,
                snapshot == null ? 0L : snapshot.ReservationRevision,
                snapshot == null
                    ? HiveSquadCompositionPlanner.InitialCapacity
                    : snapshot.Capacity,
                Count(snapshot == null ? null : snapshot.Roster, "guardians"),
                Count(snapshot == null ? null : snapshot.Roster, "wingrunners"),
                Count(snapshot == null ? null : snapshot.Roster, "darters"),
                Count(snapshot == null ? null : snapshot.Available, "guardians"),
                Count(snapshot == null ? null : snapshot.Available, "wingrunners"),
                Count(snapshot == null ? null : snapshot.Available, "darters"),
                Count(snapshot == null ? null : snapshot.Reserved, "guardians"),
                Count(snapshot == null ? null : snapshot.Reserved, "wingrunners"),
                Count(snapshot == null ? null : snapshot.Reserved, "darters"),
                snapshot == null ? string.Empty : snapshot.ReservationId,
                protectedOutboxAvailable,
                readOnlyOffline,
                cachedAtUtc,
                pendingKind,
                pendingGuardians,
                pendingWingrunners,
                pendingDarters,
                successCode);
        }

        private static long Count(
            IReadOnlyDictionary<string, long> values,
            string key)
        {
            if (values == null || !values.TryGetValue(key, out long value))
                return 0L;
            return Math.Max(0L, value);
        }
    }

    public interface IHiveSquadReservationPanelController
    {
        HiveSquadReservationScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        void Commit(int guardians, int wingrunners, int darters);
        void Release();
        void Retry();
    }

    public sealed class UnavailableHiveSquadReservationPanelController :
        IHiveSquadReservationPanelController
    {
        private readonly HiveSquadReservationScreenModel model =
            HiveOfficialSquadReservationPresentation.NotConfigured();

        public HiveSquadReservationScreenModel Model => model;
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public void Commit(int guardians, int wingrunners, int darters) { }
        public void Release() { }
        public void Retry() { }
    }

    public interface IHiveSquadReservationKeySource
    {
        string Create(string operation);
    }

    public sealed class SessionHiveSquadReservationKeySource :
        IHiveSquadReservationKeySource
    {
        public string Create(string operation)
        {
            string safe = string.IsNullOrWhiteSpace(operation)
                ? "reservation"
                : operation.Trim();
            return "mobile-" + safe + "-" +
                Guid.NewGuid().ToString("N");
        }
    }

    public sealed class HiveSquadReservationPanelController :
        IHiveSquadReservationPanelController,
        IDisposable
    {
        private readonly IHiveSquadReservationClient client;
        private readonly Guid hiveId;
        private readonly ProtectedGameMutationOutbox outbox;
        private readonly IHiveSquadReservationKeySource keySource;
        private readonly IMobileAccountSessionClock clock;
        private readonly CancellationTokenSource lifetime =
            new CancellationTokenSource();
        private RemoteSquadReservationSnapshot snapshot;
        private bool disposed;
        private bool busy;

        public HiveSquadReservationPanelController(
            IHiveSquadReservationClient client,
            Guid hiveId,
            ProtectedGameMutationOutbox outbox,
            IHiveSquadReservationKeySource keySource = null,
            IMobileAccountSessionClock clock = null)
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
                keySource ?? new SessionHiveSquadReservationKeySource();
            this.clock =
                clock ?? new SystemMobileAccountSessionClock();
            Model = HiveOfficialSquadReservationPresentation.Project(
                HiveSquadReservationScreenState.Loading,
                null,
                outbox.IsProtectionAvailable);
        }

        public HiveSquadReservationScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh()
        {
            RefreshInsideLifetime();
        }

        public void Commit(
            int guardians,
            int wingrunners,
            int darters)
        {
            CommitInsideLifetime(
                guardians,
                wingrunners,
                darters);
        }

        public void Release()
        {
            ReleaseInsideLifetime();
        }

        public void Retry()
        {
            RetryInsideLifetime();
        }

        public Task RefreshForProofAsync()
        {
            return RefreshCoreAsync();
        }

        public Task CommitForProofAsync(
            int guardians,
            int wingrunners,
            int darters)
        {
            return CommitCoreAsync(
                guardians,
                wingrunners,
                darters);
        }

        public Task ReleaseForProofAsync()
        {
            return ReleaseCoreAsync();
        }

        public Task RetryForProofAsync()
        {
            return RetryCoreAsync();
        }

        public IReadOnlyList<string> ProofRows()
        {
            return new[]
            {
                "mobile_squad_reservation_contract:" +
                    HivePerimeterSortieClient.ReservationContractVersion,
                "mobile_squad_reservation_cache:protected_get_only",
                "mobile_squad_reservation_outbox:protected_before_transport",
                "mobile_squad_reservation_auto_submit:false",
                "mobile_squad_reservation_retry:explicit_exact_command",
                "mobile_squad_reservation_device_debit:false",
                "mobile_squad_reservation_device_combat:false"
            };
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

        private async void CommitInsideLifetime(
            int guardians,
            int wingrunners,
            int darters)
        {
            await CommitCoreAsync(
                guardians,
                wingrunners,
                darters);
        }

        private async void ReleaseInsideLifetime()
        {
            await ReleaseCoreAsync();
        }

        private async void RetryInsideLifetime()
        {
            await RetryCoreAsync();
        }

        private async Task RefreshCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            Model = HiveOfficialSquadReservationPresentation.Project(
                HiveSquadReservationScreenState.Loading,
                snapshot,
                outbox.IsProtectionAvailable);
            try
            {
                RemoteSquadReservationSnapshot result =
                    await client.ReadReservationAsync(
                        hiveId,
                        lifetime.Token);
                if (disposed) return;
                snapshot = result;
                PendingDescriptor pending =
                    await LoadPendingAsync(lifetime.Token);
                if (disposed) return;
                if (outbox.LastLoadDetectedCorruption)
                {
                    SetError("mutation_recovery_refresh_required");
                    return;
                }
                bool offline =
                    client.LastReadSource ==
                    GameReadSource.ProtectedCache;
                if (pending != null)
                {
                    Model = PendingModel(pending, offline);
                    return;
                }
                Model = HiveOfficialSquadReservationPresentation.Project(
                    offline
                        ? HiveSquadReservationScreenState.OfflineReadOnly
                        : HiveSquadReservationScreenState.Ready,
                    snapshot,
                    outbox.IsProtectionAvailable,
                    readOnlyOffline: offline,
                    cachedAtUtc: offline
                        ? client.LastReadCachedAtUtc
                        : default(DateTimeOffset));
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

        private async Task CommitCoreAsync(
            int guardians,
            int wingrunners,
            int darters)
        {
            HiveSquadCompositionSnapshot composition =
                new HiveSquadCompositionSnapshot(
                    snapshot == null
                        ? HiveSquadCompositionPlanner.InitialCapacity
                        : snapshot.Capacity,
                    guardians,
                    wingrunners,
                    darters);
            HiveSquadReservationScreenModel current = Model;
            if (busy || disposed || snapshot == null ||
                current == null || !current.CanCommit(composition))
                return;

            busy = true;
            PendingGameMutation pending = null;
            PendingDescriptor descriptor = new PendingDescriptor(
                "commit",
                composition.Guardians,
                composition.Wingrunners,
                composition.Darters,
                string.Empty);
            try
            {
                PendingDescriptor existing =
                    await LoadPendingAsync(lifetime.Token);
                if (outbox.LastLoadDetectedCorruption)
                {
                    SetError("mutation_recovery_refresh_required");
                    return;
                }
                if (existing != null)
                {
                    Model = PendingModel(existing, false);
                    return;
                }
                pending = NewPending(
                    HivePerimeterSortieClient.ReservationCommitPath(
                        hiveId),
                    CommitToken(composition),
                    current.ReservationRevision,
                    keySource.Create("reserve"));
                await outbox.SavePreparedAsync(
                    pending,
                    lifetime.Token);
                Model = HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.Mutating,
                    snapshot,
                    outbox.IsProtectionAvailable,
                    pendingKind: descriptor.Kind,
                    pendingGuardians: descriptor.Guardians,
                    pendingWingrunners: descriptor.Wingrunners,
                    pendingDarters: descriptor.Darters);

                RemoteSquadReservationResponse response =
                    await client.CommitReservationWithReceiptAsync(
                        hiveId,
                        pending.ExpectedRevision,
                        Quantities(descriptor),
                        pending.IdempotencyKey,
                        lifetime.Token);
                if (disposed) return;
                snapshot = response.Snapshot;
                await DeleteContractBestEffortAsync(
                    CancellationToken.None);
                Model = HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.Ready,
                    snapshot,
                    outbox.IsProtectionAvailable,
                    successCode: response.Receipt.Code);
            }
            catch (OperationCanceledException)
            {
                if (!disposed)
                    Model = PendingModel(descriptor, false);
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                if (IsAmbiguousMutationFailure(error))
                    Model = PendingModel(descriptor, false);
                else
                {
                    if (pending != null)
                        await DeleteContractBestEffortAsync(
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

        private async Task ReleaseCoreAsync()
        {
            HiveSquadReservationScreenModel current = Model;
            if (busy || disposed || snapshot == null ||
                current == null || !current.CanRelease)
                return;
            busy = true;
            PendingGameMutation pending = null;
            PendingDescriptor descriptor = new PendingDescriptor(
                "release",
                SafeInt(current.ReservedGuardians),
                SafeInt(current.ReservedWingrunners),
                SafeInt(current.ReservedDarters),
                current.ReservationId);
            try
            {
                PendingDescriptor existing =
                    await LoadPendingAsync(lifetime.Token);
                if (outbox.LastLoadDetectedCorruption)
                {
                    SetError("mutation_recovery_refresh_required");
                    return;
                }
                if (existing != null)
                {
                    Model = PendingModel(existing, false);
                    return;
                }
                pending = NewPending(
                    HivePerimeterSortieClient.ReservationReleasePath(
                        hiveId),
                    ReleaseToken(current.ReservationId),
                    current.ReservationRevision,
                    keySource.Create("release"));
                await outbox.SavePreparedAsync(
                    pending,
                    lifetime.Token);
                Model = HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.Mutating,
                    snapshot,
                    outbox.IsProtectionAvailable,
                    pendingKind: descriptor.Kind,
                    pendingGuardians: descriptor.Guardians,
                    pendingWingrunners: descriptor.Wingrunners,
                    pendingDarters: descriptor.Darters);

                RemoteSquadReservationResponse response =
                    await client.ReleaseReservationWithReceiptAsync(
                        hiveId,
                        pending.ExpectedRevision,
                        pending.IdempotencyKey,
                        lifetime.Token);
                if (disposed) return;
                snapshot = response.Snapshot;
                await DeleteContractBestEffortAsync(
                    CancellationToken.None);
                Model = HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.Ready,
                    snapshot,
                    outbox.IsProtectionAvailable,
                    successCode: response.Receipt.Code);
            }
            catch (OperationCanceledException)
            {
                if (!disposed)
                    Model = PendingModel(descriptor, false);
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                if (IsAmbiguousMutationFailure(error))
                    Model = PendingModel(descriptor, false);
                else
                {
                    if (pending != null)
                        await DeleteContractBestEffortAsync(
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

        private async Task RetryCoreAsync()
        {
            if (busy || disposed || snapshot == null ||
                Model == null || !Model.CanRetry)
                return;
            busy = true;
            PendingDescriptor descriptor = null;
            try
            {
                descriptor = await LoadPendingAsync(lifetime.Token);
                if (outbox.LastLoadDetectedCorruption)
                {
                    SetError("mutation_recovery_refresh_required");
                    return;
                }
                if (descriptor == null || descriptor.Pending == null ||
                    descriptor.Kind == "unknown")
                {
                    SetError("mutation_changed_refresh_required");
                    return;
                }

                PendingGameMutation pending = descriptor.Pending;
                Model = HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.Mutating,
                    snapshot,
                    outbox.IsProtectionAvailable,
                    pendingKind: descriptor.Kind,
                    pendingGuardians: descriptor.Guardians,
                    pendingWingrunners: descriptor.Wingrunners,
                    pendingDarters: descriptor.Darters);
                RemoteSquadReservationResponse response;
                if (descriptor.Kind == "commit")
                {
                    response =
                        await client.CommitReservationWithReceiptAsync(
                            hiveId,
                            pending.ExpectedRevision,
                            Quantities(descriptor),
                            pending.IdempotencyKey,
                            lifetime.Token);
                }
                else if (descriptor.Kind == "release")
                {
                    response =
                        await client.ReleaseReservationWithReceiptAsync(
                            hiveId,
                            pending.ExpectedRevision,
                            pending.IdempotencyKey,
                            lifetime.Token);
                }
                else
                {
                    SetError("mutation_changed_refresh_required");
                    return;
                }
                if (disposed) return;
                snapshot = response.Snapshot;
                await DeleteContractBestEffortAsync(
                    CancellationToken.None);
                Model = HiveOfficialSquadReservationPresentation.Project(
                    HiveSquadReservationScreenState.Ready,
                    snapshot,
                    outbox.IsProtectionAvailable,
                    successCode: response.Receipt.Code);
            }
            catch (OperationCanceledException)
            {
                if (!disposed && descriptor != null)
                    Model = PendingModel(descriptor, false);
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                if (IsAmbiguousMutationFailure(error) &&
                    descriptor != null)
                    Model = PendingModel(descriptor, false);
                else
                {
                    await DeleteContractBestEffortAsync(
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
                    HivePerimeterSortieClient.ReservationContractVersion,
                Path = path,
                Method = "POST",
                PayloadToken = payloadToken,
                ExpectedRevision = expectedRevision,
                IdempotencyKey = idempotencyKey,
                CreatedAtUtc = clock.UtcNow
            };
        }

        private async Task<PendingDescriptor> LoadPendingAsync(
            CancellationToken cancellationToken)
        {
            if (!outbox.IsProtectionAvailable || snapshot == null)
                return null;
            IReadOnlyList<PendingGameMutation> entries =
                await outbox.ListAsync(
                    snapshot.PlayerId,
                    hiveId,
                    HivePerimeterSortieClient.ReservationContractVersion,
                    cancellationToken);
            if (entries.Count == 0) return null;
            if (entries.Count != 1)
                return new PendingDescriptor(
                    "unknown",
                    0,
                    0,
                    0,
                    string.Empty,
                    entries.Last());
            PendingGameMutation pending = entries[0];
            if (string.Equals(
                    pending.Path,
                    HivePerimeterSortieClient.ReservationCommitPath(
                        hiveId),
                    StringComparison.Ordinal) &&
                TryParseCommitToken(
                    pending.PayloadToken,
                    out int guardians,
                    out int wingrunners,
                    out int darters))
                return new PendingDescriptor(
                    "commit",
                    guardians,
                    wingrunners,
                    darters,
                    string.Empty,
                    pending);
            if (string.Equals(
                    pending.Path,
                    HivePerimeterSortieClient.ReservationReleasePath(
                        hiveId),
                    StringComparison.Ordinal) &&
                TryParseReleaseToken(
                    pending.PayloadToken,
                    out string reservationId))
                return new PendingDescriptor(
                    "release",
                    0,
                    0,
                    0,
                    reservationId,
                    pending);
            return new PendingDescriptor(
                "unknown",
                0,
                0,
                0,
                string.Empty,
                pending);
        }

        private HiveSquadReservationScreenModel PendingModel(
            PendingDescriptor descriptor,
            bool readOnlyOffline)
        {
            return HiveOfficialSquadReservationPresentation.Project(
                HiveSquadReservationScreenState.PendingConfirmation,
                snapshot,
                outbox.IsProtectionAvailable,
                readOnlyOffline: readOnlyOffline,
                cachedAtUtc: readOnlyOffline
                    ? client.LastReadCachedAtUtc
                    : default(DateTimeOffset),
                pendingKind: descriptor.Kind,
                pendingGuardians: descriptor.Guardians,
                pendingWingrunners: descriptor.Wingrunners,
                pendingDarters: descriptor.Darters);
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
                    HivePerimeterSortieClient.ReservationContractVersion,
                    cancellationToken);
            }
            catch
            {
            }
        }

        private void SetError(string code)
        {
            Model = HiveOfficialSquadReservationPresentation.Project(
                HiveSquadReservationScreenState.Error,
                snapshot,
                outbox.IsProtectionAvailable,
                errorCode: code);
        }

        private static Dictionary<string, long> Quantities(
            PendingDescriptor descriptor)
        {
            return new Dictionary<string, long>(
                StringComparer.Ordinal)
            {
                ["guardians"] = descriptor.Guardians,
                ["wingrunners"] = descriptor.Wingrunners,
                ["darters"] = descriptor.Darters
            };
        }

        private static string CommitToken(
            HiveSquadCompositionSnapshot composition)
        {
            return "commit.g" +
                composition.Guardians.ToString(
                    CultureInfo.InvariantCulture) +
                ".w" +
                composition.Wingrunners.ToString(
                    CultureInfo.InvariantCulture) +
                ".d" +
                composition.Darters.ToString(
                    CultureInfo.InvariantCulture);
        }

        private static bool TryParseCommitToken(
            string token,
            out int guardians,
            out int wingrunners,
            out int darters)
        {
            guardians = 0;
            wingrunners = 0;
            darters = 0;
            string[] parts =
                string.IsNullOrWhiteSpace(token)
                    ? Array.Empty<string>()
                    : token.Split('.');
            if (parts.Length != 4 ||
                !string.Equals(parts[0], "commit", StringComparison.Ordinal) ||
                !TryParseQuantity(parts[1], 'g', out guardians) ||
                !TryParseQuantity(parts[2], 'w', out wingrunners) ||
                !TryParseQuantity(parts[3], 'd', out darters))
                return false;
            int total = guardians + wingrunners + darters;
            return total > 0 &&
                total <= HiveSquadCompositionPlanner.InitialCapacity;
        }

        private static bool TryParseQuantity(
            string token,
            char prefix,
            out int value)
        {
            value = 0;
            return !string.IsNullOrWhiteSpace(token) &&
                token.Length >= 2 &&
                token[0] == prefix &&
                int.TryParse(
                    token.Substring(1),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out value) &&
                value >= 0 &&
                value <= HiveSquadCompositionPlanner.InitialCapacity;
        }

        private static string ReleaseToken(string reservationId)
        {
            return "release." + (reservationId ?? string.Empty);
        }

        private static bool TryParseReleaseToken(
            string token,
            out string reservationId)
        {
            reservationId = string.Empty;
            const string Prefix = "release.";
            if (string.IsNullOrWhiteSpace(token) ||
                !token.StartsWith(Prefix, StringComparison.Ordinal))
                return false;
            string candidate = token.Substring(Prefix.Length);
            if (candidate.Length != 32 ||
                candidate.Any(character =>
                    !((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f'))))
                return false;
            reservationId = candidate;
            return true;
        }

        private static int SafeInt(long value)
        {
            return (int)Math.Min(int.MaxValue, Math.Max(0L, value));
        }

        private static bool IsProtectedStoreFailure(Exception error)
        {
            return error is InvalidOperationException &&
                error.Message.StartsWith(
                    "game.mutation.",
                    StringComparison.Ordinal);
        }

        private static bool IsAmbiguousMutationFailure(
            HivePerimeterClientException error)
        {
            if (error == null) return true;
            if (error.Error ==
                HivePerimeterClientError.TransportFailure)
                return true;
            if (error.Error !=
                HivePerimeterClientError.InvalidResponse)
                return false;
            string code = error.Message ?? string.Empty;
            return code != "game.squad_over_reserved" &&
                code != "game.squad_in_use" &&
                code != "game.revision_conflict" &&
                code != "game.idempotency_conflict" &&
                code != "game.invalid_request" &&
                code != "game.unavailable";
        }

        private static string StableError(
            HivePerimeterClientException error)
        {
            string remote = error.Message ?? string.Empty;
            switch (remote)
            {
                case "game.squad_over_reserved":
                    return "over_reserved";
                case "game.squad_in_use":
                    return "squad_in_use";
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
                int guardians,
                int wingrunners,
                int darters,
                string reservationId,
                PendingGameMutation pending = null)
            {
                Kind = kind ?? string.Empty;
                Guardians = Math.Max(0, guardians);
                Wingrunners = Math.Max(0, wingrunners);
                Darters = Math.Max(0, darters);
                ReservationId = reservationId ?? string.Empty;
                Pending = pending;
            }

            public string Kind { get; }
            public int Guardians { get; }
            public int Wingrunners { get; }
            public int Darters { get; }
            public string ReservationId { get; }
            public PendingGameMutation Pending { get; }
        }
    }
}
