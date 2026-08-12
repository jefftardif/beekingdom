using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    public interface IHivePerimeterSortiePanelController
    {
        HivePerimeterSortieScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        void ReserveSquad(int guardians, int wingrunners, int darters);
        void Launch(string signalKey);
        void Claim();
        void Recall();
        void Retry();
        void DismissDebrief();
    }

    public sealed class UnavailableHivePerimeterSortiePanelController : IHivePerimeterSortiePanelController
    {
        private readonly HivePerimeterSortieScreenModel model = HivePerimeterSortiePresentation.NotConfigured();

        public HivePerimeterSortieScreenModel Model => model;
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public void ReserveSquad(int guardians, int wingrunners, int darters) { }
        public void Launch(string signalKey) { }
        public void Claim() { }
        public void Recall() { }
        public void Retry() { }
        public void DismissDebrief() { }
    }

    public interface IHivePerimeterMutationKeySource
    {
        string Create(string operation);
    }

    public sealed class SessionHivePerimeterMutationKeySource : IHivePerimeterMutationKeySource
    {
        public string Create(string operation)
        {
            string safeOperation = string.IsNullOrWhiteSpace(operation) ? "mutation" : operation.Trim();
            return "mobile-" + safeOperation + "-" + Guid.NewGuid().ToString("N");
        }
    }

    public sealed class HivePerimeterSortiePanelController : IHivePerimeterSortiePanelController, IDisposable
    {
        private readonly IHivePerimeterSortieClient client;
        private readonly Guid hiveId;
        private readonly ProtectedGameMutationOutbox outbox;
        private readonly IHivePerimeterMutationKeySource keySource;
        private readonly IMobileAccountSessionClock clock;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private RemoteHivePerimeterSnapshot snapshot;
        private bool disposed;
        private bool busy;

        public HivePerimeterSortiePanelController(
            IHivePerimeterSortieClient client,
            Guid hiveId,
            ProtectedGameMutationOutbox outbox,
            IHivePerimeterMutationKeySource keySource = null,
            IMobileAccountSessionClock clock = null)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            this.keySource = keySource ?? new SessionHivePerimeterMutationKeySource();
            this.clock = clock ?? new SystemMobileAccountSessionClock();
            Model = HivePerimeterSortiePresentation.Loading(
                null,
                outbox.IsProtectionAvailable);
        }

        public HivePerimeterSortieScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh()
        {
            RefreshInsideLifetime();
        }

        public void ReserveSquad(int guardians, int wingrunners, int darters)
        {
            // The official reservation controller owns this server mutation.
            // Keeping this legacy method inert prevents a second unprotected
            // reservation path from being reintroduced by an old caller.
        }

        public void Launch(string signalKey)
        {
            LaunchInsideLifetime(signalKey);
        }

        public void Claim()
        {
            ClaimInsideLifetime();
        }

        public void Recall()
        {
            RecallInsideLifetime();
        }

        public void Retry()
        {
            RetryInsideLifetime();
        }

        public void DismissDebrief()
        {
            if (busy || disposed || snapshot == null || Model == null ||
                Model.State != HivePerimeterSortieScreenState.ReturnDebrief) return;
            Model = HivePerimeterSortiePresentation.FromSnapshot(snapshot, false);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
        }

        public Task RefreshForProofAsync()
        {
            return RefreshCoreAsync();
        }

        public Task LaunchForProofAsync(string signalKey)
        {
            return LaunchCoreAsync(signalKey);
        }

        public Task ClaimForProofAsync()
        {
            return ClaimCoreAsync();
        }

        public Task RecallForProofAsync()
        {
            return RecallCoreAsync();
        }

        public Task RetryForProofAsync()
        {
            return RetryCoreAsync();
        }

        public IReadOnlyList<string> ProofRows()
        {
            return new[]
            {
                "mobile_perimeter_sortie_contract:" +
                    HivePerimeterSortieClient.SortieContractVersion,
                "mobile_perimeter_sortie_cache:protected_get_only",
                "mobile_perimeter_sortie_outbox:protected_before_transport",
                "mobile_perimeter_sortie_auto_submit:false",
                "mobile_perimeter_sortie_retry:explicit_exact_command",
                "mobile_perimeter_sortie_reward_authority:server_receipt_only",
                "mobile_perimeter_sortie_reservation_path:official_controller_only"
            };
        }

        private async void RefreshInsideLifetime()
        {
            await RefreshCoreAsync();
        }

        private async void LaunchInsideLifetime(string signalKey)
        {
            await LaunchCoreAsync(signalKey);
        }

        private async void ClaimInsideLifetime()
        {
            await ClaimCoreAsync();
        }

        private async void RecallInsideLifetime()
        {
            await RecallCoreAsync();
        }

        private async void RetryInsideLifetime()
        {
            await RetryCoreAsync();
        }

        private async Task RefreshCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            Model = HivePerimeterSortiePresentation.Loading(
                snapshot,
                outbox.IsProtectionAvailable);
            try
            {
                RemoteHivePerimeterSnapshot result =
                    await client.ReadSortieBoardAsync(
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
                bool protectedOfflineRead = client.LastReadSource == GameReadSource.ProtectedCache;
                if (pending != null)
                {
                    Model = PendingModel(
                        pending,
                        protectedOfflineRead);
                    return;
                }
                Model = HivePerimeterSortiePresentation.FromSnapshot(
                    result,
                    includeClaimReceipt: !protectedOfflineRead,
                    readOnlyOffline: protectedOfflineRead,
                    cachedAtUtc: protectedOfflineRead
                        ? client.LastReadCachedAtUtc
                        : default(DateTimeOffset),
                    protectedOutboxAvailable:
                        outbox.IsProtectionAvailable);
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

        private async Task LaunchCoreAsync(string signalKey)
        {
            HivePerimeterSortieScreenModel current = Model;
            HivePerimeterSignalCard signal = current == null
                ? null
                : current.Signals.SingleOrDefault(item =>
                    string.Equals(
                        item.SignalKey,
                        signalKey,
                        StringComparison.Ordinal));
            if (busy || disposed || snapshot == null ||
                signal == null || !current.CanLaunch(signalKey))
                return;
            PendingDescriptor descriptor = new PendingDescriptor(
                "launch",
                signal.SignalKey,
                signal.SignalInstanceId,
                current.ReservationId,
                Guid.Empty);
            await PrepareAndSendAsync(
                descriptor,
                HivePerimeterSortieClient.SortieLaunchPath(hiveId),
                LaunchToken(descriptor),
                current.Revision,
                "launch");
        }

        private async Task ClaimCoreAsync()
        {
            HivePerimeterSortieScreenModel current = Model;
            if (busy || disposed || snapshot == null ||
                current == null ||
                current.State !=
                    HivePerimeterSortieScreenState.ClaimReady ||
                current.ActiveSortieId == Guid.Empty)
                return;
            PendingDescriptor descriptor = new PendingDescriptor(
                "claim",
                current.ActiveSignalKey,
                string.Empty,
                current.ReservationId,
                current.ActiveSortieId);
            await PrepareAndSendAsync(
                descriptor,
                HivePerimeterSortieClient.SortieClaimPath(
                    hiveId,
                    current.ActiveSortieId),
                FinishToken(
                    "claim",
                    current.ActiveSortieId),
                current.Revision,
                "claim");
        }

        private async Task RecallCoreAsync()
        {
            HivePerimeterSortieScreenModel current = Model;
            if (busy || disposed || snapshot == null ||
                current == null ||
                current.SecondaryAction !=
                    HivePerimeterSortieAction.Recall ||
                current.ActiveSortieId == Guid.Empty)
                return;
            PendingDescriptor descriptor = new PendingDescriptor(
                "recall",
                current.ActiveSignalKey,
                string.Empty,
                current.ReservationId,
                current.ActiveSortieId);
            await PrepareAndSendAsync(
                descriptor,
                HivePerimeterSortieClient.SortieRecallPath(
                    hiveId,
                    current.ActiveSortieId),
                FinishToken(
                    "recall",
                    current.ActiveSortieId),
                current.Revision,
                "recall");
        }

        private async Task PrepareAndSendAsync(
            PendingDescriptor descriptor,
            string path,
            string payloadToken,
            long expectedRevision,
            string keyOperation)
        {
            busy = true;
            PendingGameMutation prepared = null;
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
                prepared = NewPending(
                    path,
                    payloadToken,
                    expectedRevision,
                    keySource.Create(keyOperation));
                await outbox.SavePreparedAsync(
                    prepared,
                    lifetime.Token);
                descriptor = descriptor.WithPending(prepared);
                Model = MutationModel(
                    HivePerimeterSortieScreenState.Mutating,
                    descriptor);
                await SendPreparedAsync(descriptor);
            }
            catch (OperationCanceledException)
            {
                if (!disposed && prepared != null)
                    Model = PendingModel(
                        descriptor.WithPending(prepared),
                        false);
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                if (prepared != null &&
                    IsAmbiguousMutationFailure(error))
                    Model = PendingModel(
                        descriptor.WithPending(prepared),
                        false);
                else
                {
                    if (prepared != null)
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
                descriptor =
                    await LoadPendingAsync(lifetime.Token);
                if (outbox.LastLoadDetectedCorruption)
                {
                    SetError("mutation_recovery_refresh_required");
                    return;
                }
                if (descriptor == null ||
                    descriptor.Pending == null ||
                    descriptor.Kind == "unknown")
                {
                    SetError("mutation_changed_refresh_required");
                    return;
                }
                Model = MutationModel(
                    HivePerimeterSortieScreenState.Mutating,
                    descriptor);
                await SendPreparedAsync(descriptor);
            }
            catch (OperationCanceledException)
            {
                if (!disposed && descriptor != null)
                    Model = PendingModel(descriptor, false);
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                if (descriptor != null &&
                    IsAmbiguousMutationFailure(error))
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

        private async Task SendPreparedAsync(
            PendingDescriptor descriptor)
        {
            PendingGameMutation pending = descriptor.Pending;
            if (pending == null)
                throw new InvalidOperationException(
                    "game.mutation.pending_missing");
            RemoteHivePerimeterMutationResponse response;
            if (descriptor.Kind == "launch")
            {
                response =
                    await client.LaunchWithReceiptAsync(
                        hiveId,
                        descriptor.SignalKey,
                        descriptor.SignalInstanceId,
                        descriptor.ReservationId,
                        pending.ExpectedRevision,
                        pending.IdempotencyKey,
                        lifetime.Token);
            }
            else if (descriptor.Kind == "claim")
            {
                response =
                    await client.ClaimWithReceiptAsync(
                        hiveId,
                        descriptor.SortieId,
                        pending.ExpectedRevision,
                        pending.IdempotencyKey,
                        lifetime.Token);
            }
            else if (descriptor.Kind == "recall")
            {
                response =
                    await client.RecallWithReceiptAsync(
                        hiveId,
                        descriptor.SortieId,
                        pending.ExpectedRevision,
                        pending.IdempotencyKey,
                        lifetime.Token);
            }
            else
            {
                throw new HivePerimeterClientException(
                    HivePerimeterClientError.InvalidRequest,
                    "game.invalid_request");
            }
            if (disposed) return;
            snapshot = response.Snapshot;
            if (descriptor.Kind == "claim")
                snapshot.ClaimReceipt =
                    ClaimReceipt(response.Receipt);
            await DeleteContractBestEffortAsync(
                CancellationToken.None);
            Model = HivePerimeterSortiePresentation.FromSnapshot(
                snapshot,
                includeClaimReceipt:
                    descriptor.Kind == "claim",
                protectedOutboxAvailable:
                    outbox.IsProtectionAvailable,
                successCode: response.Receipt.Code);
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
                    HivePerimeterSortieClient.SortieContractVersion,
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
            if (!outbox.IsProtectionAvailable ||
                snapshot == null)
                return null;
            IReadOnlyList<PendingGameMutation> entries =
                await outbox.ListAsync(
                    snapshot.PlayerId,
                    hiveId,
                    HivePerimeterSortieClient.SortieContractVersion,
                    cancellationToken);
            if (entries.Count == 0) return null;
            if (entries.Count != 1)
                return PendingDescriptor.Unknown(
                    entries.Last());

            PendingGameMutation pending = entries[0];
            if (!string.Equals(
                    pending.Method,
                    "POST",
                    StringComparison.Ordinal) ||
                pending.ExpectedRevision < 0 ||
                pending.ExpectedRevision == long.MaxValue)
                return PendingDescriptor.Unknown(pending);
            if (string.Equals(
                    pending.Path,
                    HivePerimeterSortieClient.SortieLaunchPath(
                        hiveId),
                    StringComparison.Ordinal) &&
                TryParseLaunchToken(
                    pending.PayloadToken,
                    out string signalKey,
                    out string signalInstanceId,
                    out string reservationId))
                return new PendingDescriptor(
                    "launch",
                    signalKey,
                    signalInstanceId,
                    reservationId,
                    Guid.Empty,
                    pending);
            if (TryParseFinishToken(
                    pending.PayloadToken,
                    "claim",
                    out Guid claimSortieId) &&
                string.Equals(
                    pending.Path,
                    HivePerimeterSortieClient.SortieClaimPath(
                        hiveId,
                        claimSortieId),
                    StringComparison.Ordinal))
                return new PendingDescriptor(
                    "claim",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    claimSortieId,
                    pending);
            if (TryParseFinishToken(
                    pending.PayloadToken,
                    "recall",
                    out Guid recallSortieId) &&
                string.Equals(
                    pending.Path,
                    HivePerimeterSortieClient.SortieRecallPath(
                        hiveId,
                        recallSortieId),
                    StringComparison.Ordinal))
                return new PendingDescriptor(
                    "recall",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    recallSortieId,
                    pending);
            return PendingDescriptor.Unknown(pending);
        }

        private HivePerimeterSortieScreenModel PendingModel(
            PendingDescriptor descriptor,
            bool readOnlyOffline)
        {
            return HivePerimeterSortiePresentation.FromSnapshot(
                snapshot,
                includeClaimReceipt: false,
                readOnlyOffline: readOnlyOffline,
                cachedAtUtc: readOnlyOffline
                    ? client.LastReadCachedAtUtc
                    : default(DateTimeOffset),
                protectedOutboxAvailable:
                    outbox.IsProtectionAvailable,
                stateOverride:
                    HivePerimeterSortieScreenState.PendingConfirmation,
                pendingKind: descriptor.Kind);
        }

        private HivePerimeterSortieScreenModel MutationModel(
            HivePerimeterSortieScreenState state,
            PendingDescriptor descriptor)
        {
            return HivePerimeterSortiePresentation.FromSnapshot(
                snapshot,
                includeClaimReceipt: false,
                protectedOutboxAvailable:
                    outbox.IsProtectionAvailable,
                stateOverride: state,
                pendingKind: descriptor.Kind);
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
                    HivePerimeterSortieClient.SortieContractVersion,
                    cancellationToken);
            }
            catch
            {
            }
        }

        private void SetError(string code)
        {
            Model = HivePerimeterSortiePresentation.Error(
                code,
                snapshot,
                outbox.IsProtectionAvailable);
        }

        private static RemoteHivePerimeterClaimReceipt ClaimReceipt(
            RemoteHivePerimeterMutationReceipt receipt)
        {
            return new RemoteHivePerimeterClaimReceipt
            {
                PlayerId = receipt.PlayerId,
                HiveId = receipt.HiveId,
                SortieId = receipt.SortieId,
                SignalKey = receipt.SignalKey,
                SignalInstanceId = receipt.SignalInstanceId,
                CycleStartedAtUtc = receipt.CycleStartedAtUtc,
                CycleEndsAtUtc = receipt.CycleEndsAtUtc,
                Revision = receipt.RevisionAfter,
                ServerTimeUtc = receipt.AcceptedAtUtc,
                CreditedByResource =
                    receipt.CreditedByResource,
                ResultingBalances =
                    receipt.ResultingBalances
            };
        }

        private static string LaunchToken(
            PendingDescriptor descriptor)
        {
            return "launch." + descriptor.SignalKey + "." +
                descriptor.SignalInstanceId + "." +
                descriptor.ReservationId;
        }

        private static bool TryParseLaunchToken(
            string token,
            out string signalKey,
            out string signalInstanceId,
            out string reservationId)
        {
            signalKey = string.Empty;
            signalInstanceId = string.Empty;
            reservationId = string.Empty;
            string[] parts =
                string.IsNullOrWhiteSpace(token)
                    ? Array.Empty<string>()
                    : token.Split('.');
            if (parts.Length != 4 ||
                !string.Equals(
                    parts[0],
                    "launch",
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(parts[1]) ||
                parts[1].Length > 64 ||
                !IsLowerHex(parts[2], 32) ||
                !IsLowerHex(parts[3], 32))
                return false;
            signalKey = parts[1];
            signalInstanceId = parts[2];
            reservationId = parts[3];
            return true;
        }

        private static string FinishToken(
            string action,
            Guid sortieId)
        {
            return action + "." + sortieId.ToString("N");
        }

        private static bool TryParseFinishToken(
            string token,
            string action,
            out Guid sortieId)
        {
            sortieId = Guid.Empty;
            string prefix = action + ".";
            if (string.IsNullOrWhiteSpace(token) ||
                !token.StartsWith(
                    prefix,
                    StringComparison.Ordinal))
                return false;
            string value = token.Substring(prefix.Length);
            return value.Length == 32 &&
                Guid.TryParseExact(value, "N", out sortieId) &&
                sortieId != Guid.Empty;
        }

        private static bool IsLowerHex(
            string value,
            int length)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length != length)
                return false;
            return value.All(character =>
                (character >= '0' && character <= '9') ||
                (character >= 'a' && character <= 'f'));
        }

        private static bool IsProtectedStoreFailure(
            Exception error)
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
                    HivePerimeterClientError.TransportFailure ||
                error.Error ==
                    HivePerimeterClientError.AuthenticationRequired ||
                error.Error ==
                    HivePerimeterClientError.NotConfigured)
                return true;
            if (error.Error !=
                HivePerimeterClientError.InvalidResponse)
                return false;
            string code = error.Message ?? string.Empty;
            return code != "game.revision_conflict" &&
                code != "game.perimeter_precondition_failed" &&
                code != "game.perimeter_signal_completed" &&
                code != "game.perimeter_not_complete" &&
                code != "game.perimeter_conflict" &&
                code != "game.idempotency_conflict" &&
                code != "game.invalid_request" &&
                code != "game.unavailable" &&
                code != "game.receipts_full";
        }

        private static string StableError(
            HivePerimeterClientException error)
        {
            string remote = error.Message ?? string.Empty;
            switch (remote)
            {
                case "game.revision_conflict":
                    return "revision_conflict";
                case "game.perimeter_precondition_failed":
                    return "precondition_failed";
                case "game.perimeter_signal_completed":
                    return "signal_completed";
                case "game.perimeter_not_complete":
                    return "not_complete";
                case "game.perimeter_conflict":
                    return "perimeter_conflict";
                case "game.idempotency_conflict":
                    return "idempotency_conflict";
                case "game.unavailable":
                    return "server_unavailable";
                case "game.receipts_full":
                    return "server_receipts_full";
            }
            switch (error.Error)
            {
                case HivePerimeterClientError.NotConfigured:
                    return "not_configured";
                case HivePerimeterClientError.AuthenticationRequired:
                    return "authentication_required";
                case HivePerimeterClientError.InvalidRequest:
                    return "invalid_request";
                case HivePerimeterClientError.TransportFailure:
                    return "network_unavailable";
                default:
                    return "invalid_response";
            }
        }

        private sealed class PendingDescriptor
        {
            public PendingDescriptor(
                string kind,
                string signalKey,
                string signalInstanceId,
                string reservationId,
                Guid sortieId,
                PendingGameMutation pending = null)
            {
                Kind = kind ?? string.Empty;
                SignalKey = signalKey ?? string.Empty;
                SignalInstanceId =
                    signalInstanceId ?? string.Empty;
                ReservationId = reservationId ?? string.Empty;
                SortieId = sortieId;
                Pending = pending;
            }

            public string Kind { get; }
            public string SignalKey { get; }
            public string SignalInstanceId { get; }
            public string ReservationId { get; }
            public Guid SortieId { get; }
            public PendingGameMutation Pending { get; }

            public PendingDescriptor WithPending(
                PendingGameMutation pending)
            {
                return new PendingDescriptor(
                    Kind,
                    SignalKey,
                    SignalInstanceId,
                    ReservationId,
                    SortieId,
                    pending);
            }

            public static PendingDescriptor Unknown(
                PendingGameMutation pending)
            {
                return new PendingDescriptor(
                    "unknown",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    Guid.Empty,
                    pending);
            }
        }
    }

    public enum HivePerimeterSortieScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        NeedsReservation = 2,
        ReadyToLaunch = 3,
        Active = 4,
        ClaimReady = 5,
        CycleComplete = 6,
        ReturnDebrief = 7,
        Error = 8,
        Mutating = 9,
        PendingConfirmation = 10
    }

    public enum HivePerimeterSortieAction
    {
        None = 0,
        ReserveSquad = 1,
        LaunchSignal = 2,
        Claim = 3,
        Recall = 4,
        DismissDebrief = 5,
        Retry = 6
    }

    public sealed class HivePerimeterReturnDebrief
    {
        internal HivePerimeterReturnDebrief(
            Guid sortieId,
            string signalKey,
            long revision,
            DateTimeOffset serverTimeUtc,
            long honeyCredited,
            long pollenCredited,
            long honeyExpected,
            long pollenExpected,
            long honeyBalance,
            long honeyCapacity,
            long pollenBalance,
            long pollenCapacity)
        {
            SortieId = sortieId;
            SignalKey = signalKey ?? string.Empty;
            Revision = Math.Max(0L, revision);
            ServerTimeUtc = serverTimeUtc;
            HoneyCredited = Math.Max(0L, honeyCredited);
            PollenCredited = Math.Max(0L, pollenCredited);
            HoneyExpected = Math.Max(0L, honeyExpected);
            PollenExpected = Math.Max(0L, pollenExpected);
            HoneyBalance = Math.Max(0L, honeyBalance);
            HoneyCapacity = Math.Max(HoneyBalance, honeyCapacity);
            PollenBalance = Math.Max(0L, pollenBalance);
            PollenCapacity = Math.Max(PollenBalance, pollenCapacity);
        }

        public Guid SortieId { get; }
        public string SignalKey { get; }
        public long Revision { get; }
        public DateTimeOffset ServerTimeUtc { get; }
        public long HoneyCredited { get; }
        public long PollenCredited { get; }
        public long HoneyExpected { get; }
        public long PollenExpected { get; }
        public long HoneyBalance { get; }
        public long HoneyCapacity { get; }
        public long PollenBalance { get; }
        public long PollenCapacity { get; }
        public bool HoneyCapacityLimited => HoneyCredited < HoneyExpected;
        public bool PollenCapacityLimited => PollenCredited < PollenExpected;
        public bool AnyCapacityLimited => HoneyCapacityLimited || PollenCapacityLimited;
    }

    public sealed class HivePerimeterSignalCard
    {
        public HivePerimeterSignalCard(
            string signalKey,
            string signalInstanceId,
            string hazardDoctrine,
            TimeSpan duration,
            int minimumSquad,
            long honeyReward,
            long pollenReward,
            bool completed,
            bool canLaunch)
        {
            SignalKey = signalKey ?? string.Empty;
            SignalInstanceId = signalInstanceId ?? string.Empty;
            HazardDoctrine = hazardDoctrine ?? string.Empty;
            Duration = duration;
            MinimumSquad = Math.Max(0, minimumSquad);
            HoneyReward = Math.Max(0L, honeyReward);
            PollenReward = Math.Max(0L, pollenReward);
            Completed = completed;
            CanLaunch = canLaunch;
        }

        public string SignalKey { get; }
        public string SignalInstanceId { get; }
        public string HazardDoctrine { get; }
        public TimeSpan Duration { get; }
        public int MinimumSquad { get; }
        public long HoneyReward { get; }
        public long PollenReward { get; }
        public bool Completed { get; }
        public bool CanLaunch { get; }
    }

    public sealed class HivePerimeterSortieScreenModel
    {
        internal HivePerimeterSortieScreenModel(
            HivePerimeterSortieScreenState state,
            HivePerimeterSortieAction primaryAction,
            HivePerimeterSortieAction secondaryAction,
            string errorCode,
            long revision,
            DateTimeOffset serverTimeUtc,
            DateTimeOffset cycleEndsAtUtc,
            string reservationId,
            long reservedTotal,
            Guid activeSortieId,
            string activeSignalKey,
            TimeSpan remainingAtReceipt,
            IReadOnlyList<HivePerimeterSignalCard> signals,
            long reservationRevision,
            HivePerimeterReturnDebrief returnDebrief,
            bool readOnlyOffline,
            DateTimeOffset cachedAtUtc,
            bool protectedOutboxAvailable,
            string pendingKind,
            string successCode)
        {
            State = state;
            PrimaryAction = primaryAction;
            SecondaryAction = secondaryAction;
            ErrorCode = errorCode ?? string.Empty;
            Revision = Math.Max(0L, revision);
            ServerTimeUtc = serverTimeUtc;
            CycleEndsAtUtc = cycleEndsAtUtc;
            ReservationId = reservationId ?? string.Empty;
            ReservedTotal = Math.Max(0L, reservedTotal);
            ActiveSortieId = activeSortieId;
            ActiveSignalKey = activeSignalKey ?? string.Empty;
            RemainingAtReceipt = remainingAtReceipt < TimeSpan.Zero ? TimeSpan.Zero : remainingAtReceipt;
            Signals = signals ?? Array.Empty<HivePerimeterSignalCard>();
            ReservationRevision = Math.Max(0L, reservationRevision);
            ReturnDebrief = returnDebrief;
            ReadOnlyOffline = readOnlyOffline;
            CachedAtUtc = cachedAtUtc;
            ProtectedOutboxAvailable = protectedOutboxAvailable;
            PendingKind = pendingKind ?? string.Empty;
            SuccessCode = successCode ?? string.Empty;
        }

        public HivePerimeterSortieScreenState State { get; }
        public HivePerimeterSortieAction PrimaryAction { get; }
        public HivePerimeterSortieAction SecondaryAction { get; }
        public string ErrorCode { get; }
        public long Revision { get; }
        public DateTimeOffset ServerTimeUtc { get; }
        public DateTimeOffset CycleEndsAtUtc { get; }
        public string ReservationId { get; }
        public long ReservedTotal { get; }
        public Guid ActiveSortieId { get; }
        public string ActiveSignalKey { get; }
        public TimeSpan RemainingAtReceipt { get; }
        public IReadOnlyList<HivePerimeterSignalCard> Signals { get; }
        public long ReservationRevision { get; }
        public HivePerimeterReturnDebrief ReturnDebrief { get; }
        public bool ReadOnlyOffline { get; }
        public DateTimeOffset CachedAtUtc { get; }
        public bool ProtectedOutboxAvailable { get; }
        public string PendingKind { get; }
        public string SuccessCode { get; }
        public bool IsPending =>
            State ==
            HivePerimeterSortieScreenState.PendingConfirmation;
        public bool CanRetry =>
            IsPending && ProtectedOutboxAvailable;

        public TimeSpan EstimateRemaining(TimeSpan monotonicElapsedSinceReceipt)
        {
            TimeSpan safeElapsed = monotonicElapsedSinceReceipt < TimeSpan.Zero
                ? TimeSpan.Zero
                : monotonicElapsedSinceReceipt;
            TimeSpan remaining = RemainingAtReceipt - safeElapsed;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        public bool CanLaunch(string signalKey)
        {
            return !ReadOnlyOffline && State == HivePerimeterSortieScreenState.ReadyToLaunch &&
                ProtectedOutboxAvailable &&
                Signals.Any(signal => string.Equals(signal.SignalKey, signalKey, StringComparison.Ordinal) && signal.CanLaunch);
        }
    }

    public static class HivePerimeterSortiePresentation
    {
        public static HivePerimeterSortieScreenModel NotConfigured(
            bool protectedOutboxAvailable = false)
        {
            return Empty(
                HivePerimeterSortieScreenState.NotConfigured,
                string.Empty,
                null,
                protectedOutboxAvailable);
        }

        public static HivePerimeterSortieScreenModel Loading(
            RemoteHivePerimeterSnapshot snapshot = null,
            bool protectedOutboxAvailable = false)
        {
            return snapshot == null
                ? Empty(
                    HivePerimeterSortieScreenState.Loading,
                    string.Empty,
                    null,
                    protectedOutboxAvailable)
                : FromSnapshot(
                    snapshot,
                    includeClaimReceipt: false,
                    protectedOutboxAvailable:
                        protectedOutboxAvailable,
                    stateOverride:
                        HivePerimeterSortieScreenState.Loading);
        }

        public static HivePerimeterSortieScreenModel Error(
            string stableCode,
            RemoteHivePerimeterSnapshot snapshot = null,
            bool protectedOutboxAvailable = false)
        {
            return snapshot == null
                ? Empty(
                    HivePerimeterSortieScreenState.Error,
                    stableCode,
                    null,
                    protectedOutboxAvailable)
                : FromSnapshot(
                    snapshot,
                    includeClaimReceipt: false,
                    protectedOutboxAvailable:
                        protectedOutboxAvailable,
                    stateOverride:
                        HivePerimeterSortieScreenState.Error,
                    errorCode: stableCode);
        }

        public static HivePerimeterSortieScreenModel FromSnapshot(
            RemoteHivePerimeterSnapshot snapshot,
            bool includeClaimReceipt = true,
            bool readOnlyOffline = false,
            DateTimeOffset cachedAtUtc = default(DateTimeOffset),
            bool protectedOutboxAvailable = true,
            HivePerimeterSortieScreenState? stateOverride = null,
            string pendingKind = "",
            string successCode = "",
            string errorCode = "")
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var signals = (snapshot.Signals ?? new List<RemoteHivePerimeterSignal>())
                .Select(signal => new HivePerimeterSignalCard(
                    signal.SignalKey,
                    signal.SignalInstanceId,
                    signal.HazardDoctrine,
                    signal.Duration,
                    signal.MinimumSquad,
                    signal.HoneyReward,
                    signal.PollenReward,
                    signal.Completed,
                    signal.CanLaunch))
                .ToArray();
            long reservedTotal = SafeTotal(snapshot.Reservation == null ? null : snapshot.Reservation.Reserved);
            string reservationId = snapshot.Reservation == null ? string.Empty : snapshot.Reservation.ReservationId;

            HivePerimeterSortieScreenState state;
            HivePerimeterSortieAction primary;
            HivePerimeterSortieAction secondary;
            Guid activeId = Guid.Empty;
            string activeSignal = string.Empty;
            TimeSpan remaining = TimeSpan.Zero;
            HivePerimeterReturnDebrief returnDebrief = includeClaimReceipt
                ? ProjectDebrief(snapshot, signals)
                : null;
            if (returnDebrief != null)
            {
                state = HivePerimeterSortieScreenState.ReturnDebrief;
                primary = HivePerimeterSortieAction.DismissDebrief;
                secondary = HivePerimeterSortieAction.None;
                activeId = returnDebrief.SortieId;
                activeSignal = returnDebrief.SignalKey;
            }
            else if (snapshot.Active != null)
            {
                activeId = snapshot.Active.SortieId;
                activeSignal = snapshot.Active.SignalKey ?? string.Empty;
                remaining = snapshot.Active.EndsAtUtc - snapshot.ServerTimeUtc;
                bool claimReady = remaining <= TimeSpan.Zero;
                state = claimReady ? HivePerimeterSortieScreenState.ClaimReady : HivePerimeterSortieScreenState.Active;
                primary = claimReady ? HivePerimeterSortieAction.Claim : HivePerimeterSortieAction.None;
                secondary = HivePerimeterSortieAction.Recall;
            }
            else if (signals.Length > 0 && signals.All(signal => signal.Completed))
            {
                state = HivePerimeterSortieScreenState.CycleComplete;
                primary = HivePerimeterSortieAction.None;
                secondary = HivePerimeterSortieAction.None;
            }
            else if (reservedTotal <= 0 || string.IsNullOrEmpty(reservationId))
            {
                state = HivePerimeterSortieScreenState.NeedsReservation;
                primary = HivePerimeterSortieAction.ReserveSquad;
                secondary = HivePerimeterSortieAction.None;
            }
            else
            {
                state = HivePerimeterSortieScreenState.ReadyToLaunch;
                primary = HivePerimeterSortieAction.LaunchSignal;
                secondary = HivePerimeterSortieAction.None;
            }

            if (readOnlyOffline)
            {
                if (primary != HivePerimeterSortieAction.DismissDebrief)
                    primary = HivePerimeterSortieAction.None;
                secondary = HivePerimeterSortieAction.None;
            }
            if (!protectedOutboxAvailable &&
                primary !=
                    HivePerimeterSortieAction.DismissDebrief)
            {
                primary = HivePerimeterSortieAction.None;
                secondary = HivePerimeterSortieAction.None;
            }
            if (stateOverride.HasValue)
            {
                state = stateOverride.Value;
                primary =
                    state ==
                    HivePerimeterSortieScreenState.PendingConfirmation
                        ? HivePerimeterSortieAction.Retry
                        : HivePerimeterSortieAction.None;
                secondary = HivePerimeterSortieAction.None;
                returnDebrief = null;
            }

            return new HivePerimeterSortieScreenModel(
                state,
                primary,
                secondary,
                errorCode,
                snapshot.Revision,
                snapshot.ServerTimeUtc,
                snapshot.CycleEndsAtUtc,
                reservationId,
                reservedTotal,
                activeId,
                activeSignal,
                remaining,
                signals,
                snapshot.Reservation == null ? 0L : snapshot.Reservation.ReservationRevision,
                returnDebrief,
                readOnlyOffline,
                cachedAtUtc,
                protectedOutboxAvailable,
                pendingKind,
                successCode);
        }

        private static HivePerimeterSortieScreenModel Empty(
            HivePerimeterSortieScreenState state,
            string errorCode,
            RemoteHivePerimeterSnapshot snapshot,
            bool protectedOutboxAvailable)
        {
            return new HivePerimeterSortieScreenModel(
                state,
                HivePerimeterSortieAction.None,
                HivePerimeterSortieAction.None,
                errorCode,
                0,
                default(DateTimeOffset),
                default(DateTimeOffset),
                string.Empty,
                0,
                Guid.Empty,
                string.Empty,
                TimeSpan.Zero,
                Array.Empty<HivePerimeterSignalCard>(),
                0,
                null,
                false,
                default(DateTimeOffset),
                protectedOutboxAvailable,
                string.Empty,
                string.Empty);
        }

        private static HivePerimeterReturnDebrief ProjectDebrief(
            RemoteHivePerimeterSnapshot snapshot,
            IReadOnlyList<HivePerimeterSignalCard> signals)
        {
            RemoteHivePerimeterClaimReceipt receipt = snapshot.ClaimReceipt;
            if (receipt == null) return null;
            HivePerimeterSignalCard signal = signals.Single(item =>
                string.Equals(item.SignalKey, receipt.SignalKey, StringComparison.Ordinal));
            RemoteHiveResourceBalance honey = receipt.ResultingBalances["honey"];
            RemoteHiveResourceBalance pollen = receipt.ResultingBalances["pollen"];
            return new HivePerimeterReturnDebrief(
                receipt.SortieId,
                receipt.SignalKey,
                receipt.Revision,
                receipt.ServerTimeUtc,
                receipt.CreditedByResource["honey"],
                receipt.CreditedByResource["pollen"],
                signal.HoneyReward,
                signal.PollenReward,
                honey.Amount,
                honey.Capacity,
                pollen.Amount,
                pollen.Capacity);
        }

        private static long SafeTotal(Dictionary<string, long> quantities)
        {
            if (quantities == null) return 0;
            long total = 0;
            try
            {
                foreach (long quantity in quantities.Values) total = checked(total + Math.Max(0L, quantity));
            }
            catch (OverflowException)
            {
                return long.MaxValue;
            }
            return total;
        }
    }
}
