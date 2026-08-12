using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    public enum HiveDailyRoundScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        OfflineReadOnly = 3,
        PreparingClaim = 4,
        Claiming = 5,
        ClaimPendingConfirmation = 6,
        Error = 7
    }

    public sealed class HiveDailyRoundScreenModel
    {
        internal HiveDailyRoundScreenModel(
            HiveDailyRoundScreenState state,
            string errorCode,
            Guid playerId,
            Guid hiveId,
            DateTimeOffset dayUtc,
            DateTimeOffset nextResetUtc,
            DateTimeOffset serverTimeUtc,
            long revision,
            bool collectionReceived,
            bool operationLaunched,
            bool snapshotRead,
            int completedCount,
            long honeyReward,
            long pollenReward,
            bool claimAvailable,
            DateTimeOffset? claimedAtUtc,
            DateTimeOffset cachedAtUtc,
            bool protectedOutboxAvailable,
            bool pendingClaim,
            string receiptCode)
        {
            State = state;
            ErrorCode = errorCode ?? string.Empty;
            PlayerId = playerId;
            HiveId = hiveId;
            DayUtc = dayUtc;
            NextResetUtc = nextResetUtc;
            ServerTimeUtc = serverTimeUtc;
            Revision = Math.Max(0L, revision);
            CollectionReceived = collectionReceived;
            OperationLaunched = operationLaunched;
            SnapshotRead = snapshotRead;
            CompletedCount = Math.Max(0, Math.Min(3, completedCount));
            HoneyReward = Math.Max(0L, honeyReward);
            PollenReward = Math.Max(0L, pollenReward);
            ClaimAvailable = claimAvailable;
            ClaimedAtUtc = claimedAtUtc;
            CachedAtUtc = cachedAtUtc;
            ProtectedOutboxAvailable = protectedOutboxAvailable;
            PendingClaim = pendingClaim;
            ReceiptCode = receiptCode ?? string.Empty;
        }

        public HiveDailyRoundScreenState State { get; }
        public string ErrorCode { get; }
        public Guid PlayerId { get; }
        public Guid HiveId { get; }
        public DateTimeOffset DayUtc { get; }
        public DateTimeOffset NextResetUtc { get; }
        public DateTimeOffset ServerTimeUtc { get; }
        public long Revision { get; }
        public bool CollectionReceived { get; }
        public bool OperationLaunched { get; }
        public bool SnapshotRead { get; }
        public int CompletedCount { get; }
        public long HoneyReward { get; }
        public long PollenReward { get; }
        public bool ClaimAvailable { get; }
        public DateTimeOffset? ClaimedAtUtc { get; }
        public DateTimeOffset CachedAtUtc { get; }
        public bool ProtectedOutboxAvailable { get; }
        public bool PendingClaim { get; }
        public string ReceiptCode { get; }
        public bool HasSnapshot =>
            PlayerId != Guid.Empty &&
            HiveId != Guid.Empty &&
            DayUtc != default(DateTimeOffset);
        public bool IsReadOnly => State == HiveDailyRoundScreenState.OfflineReadOnly;
        public bool IsClaimed => ClaimedAtUtc.HasValue;
        public bool CanClaim =>
            State == HiveDailyRoundScreenState.Ready &&
            ClaimAvailable &&
            ProtectedOutboxAvailable &&
            !PendingClaim;
        public bool CanRetryClaim =>
            State == HiveDailyRoundScreenState.ClaimPendingConfirmation &&
            PendingClaim &&
            ProtectedOutboxAvailable;

        public bool IsFactComplete(string key)
        {
            if (string.Equals(
                key,
                HiveDailyRoundClient.CollectionFact,
                StringComparison.Ordinal))
                return CollectionReceived;
            if (string.Equals(
                key,
                HiveDailyRoundClient.OperationFact,
                StringComparison.Ordinal))
                return OperationLaunched;
            if (string.Equals(
                key,
                HiveDailyRoundClient.SnapshotFact,
                StringComparison.Ordinal))
                return SnapshotRead;
            return false;
        }
    }

    public static class HiveDailyRoundPresentation
    {
        public static HiveDailyRoundScreenModel NotConfigured()
        {
            return Empty(HiveDailyRoundScreenState.NotConfigured, string.Empty, false);
        }

        public static HiveDailyRoundScreenModel Loading(
            RemoteHiveDailyRoundSnapshot snapshot,
            bool protectedOutboxAvailable)
        {
            return Project(
                HiveDailyRoundScreenState.Loading,
                snapshot,
                string.Empty,
                default(DateTimeOffset),
                protectedOutboxAvailable,
                false,
                string.Empty);
        }

        public static HiveDailyRoundScreenModel Ready(
            RemoteHiveDailyRoundSnapshot snapshot,
            bool protectedOutboxAvailable,
            string receiptCode = "")
        {
            return Project(
                HiveDailyRoundScreenState.Ready,
                snapshot,
                string.Empty,
                default(DateTimeOffset),
                protectedOutboxAvailable,
                false,
                receiptCode);
        }

        public static HiveDailyRoundScreenModel OfflineReadOnly(
            RemoteHiveDailyRoundSnapshot snapshot,
            DateTimeOffset cachedAtUtc,
            bool protectedOutboxAvailable)
        {
            return Project(
                HiveDailyRoundScreenState.OfflineReadOnly,
                snapshot,
                string.Empty,
                cachedAtUtc,
                protectedOutboxAvailable,
                false,
                string.Empty);
        }

        public static HiveDailyRoundScreenModel Mutating(
            HiveDailyRoundScreenState state,
            RemoteHiveDailyRoundSnapshot snapshot,
            bool protectedOutboxAvailable,
            bool pendingClaim)
        {
            if (state != HiveDailyRoundScreenState.PreparingClaim &&
                state != HiveDailyRoundScreenState.Claiming &&
                state != HiveDailyRoundScreenState.ClaimPendingConfirmation)
                throw new ArgumentOutOfRangeException(nameof(state));
            return Project(
                state,
                snapshot,
                string.Empty,
                default(DateTimeOffset),
                protectedOutboxAvailable,
                pendingClaim,
                string.Empty);
        }

        public static HiveDailyRoundScreenModel Error(
            RemoteHiveDailyRoundSnapshot snapshot,
            string stableCode,
            bool protectedOutboxAvailable)
        {
            return Project(
                HiveDailyRoundScreenState.Error,
                snapshot,
                stableCode,
                default(DateTimeOffset),
                protectedOutboxAvailable,
                false,
                string.Empty);
        }

        private static HiveDailyRoundScreenModel Project(
            HiveDailyRoundScreenState state,
            RemoteHiveDailyRoundSnapshot snapshot,
            string errorCode,
            DateTimeOffset cachedAtUtc,
            bool protectedOutboxAvailable,
            bool pendingClaim,
            string receiptCode)
        {
            if (snapshot == null)
                return Empty(state, errorCode, protectedOutboxAvailable);
            return new HiveDailyRoundScreenModel(
                state,
                errorCode,
                snapshot.PlayerId,
                snapshot.HiveId,
                snapshot.DayUtc,
                snapshot.NextResetUtc,
                snapshot.ServerTimeUtc,
                snapshot.Revision,
                snapshot.Facts[HiveDailyRoundClient.CollectionFact],
                snapshot.Facts[HiveDailyRoundClient.OperationFact],
                snapshot.Facts[HiveDailyRoundClient.SnapshotFact],
                snapshot.CompletedCount,
                snapshot.HoneyReward,
                snapshot.PollenReward,
                snapshot.ClaimAvailable,
                snapshot.ClaimedAtUtc,
                cachedAtUtc,
                protectedOutboxAvailable,
                pendingClaim,
                receiptCode);
        }

        private static HiveDailyRoundScreenModel Empty(
            HiveDailyRoundScreenState state,
            string errorCode,
            bool protectedOutboxAvailable)
        {
            return new HiveDailyRoundScreenModel(
                state,
                errorCode,
                Guid.Empty,
                Guid.Empty,
                default(DateTimeOffset),
                default(DateTimeOffset),
                default(DateTimeOffset),
                0L,
                false,
                false,
                false,
                0,
                0L,
                0L,
                false,
                null,
                default(DateTimeOffset),
                protectedOutboxAvailable,
                false,
                string.Empty);
        }
    }

    public interface IHiveDailyRoundPanelController
    {
        HiveDailyRoundScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        void Claim();
        void RetryClaim();
    }

    public sealed class UnavailableHiveDailyRoundPanelController :
        IHiveDailyRoundPanelController
    {
        private readonly HiveDailyRoundScreenModel model =
            HiveDailyRoundPresentation.NotConfigured();
        public HiveDailyRoundScreenModel Model => model;
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public void Claim() { }
        public void RetryClaim() { }
    }

    public interface IHiveDailyRoundMutationKeySource
    {
        string Create();
    }

    public sealed class SessionHiveDailyRoundMutationKeySource :
        IHiveDailyRoundMutationKeySource
    {
        public string Create()
        {
            return "mobile-daily-round-claim-" + Guid.NewGuid().ToString("N");
        }
    }

    public sealed class HiveDailyRoundPanelController :
        IHiveDailyRoundPanelController,
        IDisposable
    {
        private readonly IHiveDailyRoundClient client;
        private readonly Guid hiveId;
        private readonly ProtectedGameMutationOutbox outbox;
        private readonly IHiveDailyRoundMutationKeySource keySource;
        private readonly CancellationTokenSource lifetime =
            new CancellationTokenSource();
        private RemoteHiveDailyRoundSnapshot snapshot;
        private bool disposed;
        private bool busy;

        public HiveDailyRoundPanelController(
            IHiveDailyRoundClient client,
            Guid hiveId,
            ProtectedGameMutationOutbox outbox,
            IHiveDailyRoundMutationKeySource keySource = null)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty)
                throw new ArgumentException(
                    "A hive identifier is required.",
                    nameof(hiveId));
            this.hiveId = hiveId;
            this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            this.keySource = keySource ?? new SessionHiveDailyRoundMutationKeySource();
            Model = HiveDailyRoundPresentation.Loading(
                null,
                outbox.IsProtectionAvailable);
        }

        public HiveDailyRoundScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh()
        {
            RefreshInsideLifetime();
        }

        public void Claim()
        {
            ClaimInsideLifetime(false);
        }

        public void RetryClaim()
        {
            ClaimInsideLifetime(true);
        }

        public Task RefreshForProofAsync()
        {
            return RefreshCoreAsync();
        }

        public Task ClaimForProofAsync(bool retry = false)
        {
            return ClaimCoreAsync(retry);
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

        private async void ClaimInsideLifetime(bool retry)
        {
            await ClaimCoreAsync(retry);
        }

        private async Task RefreshCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            Model = HiveDailyRoundPresentation.Loading(
                snapshot,
                outbox.IsProtectionAvailable);
            try
            {
                RemoteHiveDailyRoundSnapshot result =
                    await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                snapshot = result;
                PendingGameMutation pending =
                    await LoadPendingBestEffortAsync(snapshot, lifetime.Token);
                if (disposed) return;
                if (outbox.LastLoadDetectedCorruption)
                {
                    Model = HiveDailyRoundPresentation.Error(
                        snapshot,
                        "claim_recovery_refresh_required",
                        outbox.IsProtectionAvailable);
                    return;
                }
                if (pending != null &&
                    (!SameDay(pending, snapshot.DayUtc) ||
                     snapshot.ClaimedAtUtc.HasValue))
                {
                    await DeletePendingBestEffortAsync(
                        snapshot.PlayerId,
                        CancellationToken.None);
                    pending = null;
                }

                if (client.LastReadSource == GameReadSource.ProtectedCache)
                {
                    Model = HiveDailyRoundPresentation.OfflineReadOnly(
                        snapshot,
                        client.LastReadCachedAtUtc,
                        outbox.IsProtectionAvailable);
                }
                else if (pending != null)
                {
                    Model = HiveDailyRoundPresentation.Mutating(
                        HiveDailyRoundScreenState.ClaimPendingConfirmation,
                        snapshot,
                        outbox.IsProtectionAvailable,
                        true);
                }
                else
                {
                    Model = HiveDailyRoundPresentation.Ready(
                        snapshot,
                        outbox.IsProtectionAvailable);
                }
            }
            catch (OperationCanceledException)
            {
                if (!disposed)
                    Model = HiveDailyRoundPresentation.Error(
                        snapshot,
                        "cancelled",
                        outbox.IsProtectionAvailable);
            }
            catch (HivePerimeterClientException error)
            {
                if (!disposed)
                    Model = HiveDailyRoundPresentation.Error(
                        snapshot,
                        StableError(error),
                        outbox.IsProtectionAvailable);
            }
            catch (Exception error)
            {
                if (!disposed)
                    Model = HiveDailyRoundPresentation.Error(
                        snapshot,
                        IsProtectedStoreFailure(error)
                            ? "protected_storage_unavailable"
                            : "unexpected",
                        outbox.IsProtectionAvailable);
            }
            finally
            {
                busy = false;
            }
        }

        private async Task ClaimCoreAsync(bool retry)
        {
            if (busy || disposed || snapshot == null) return;
            if (retry)
            {
                if (!Model.CanRetryClaim) return;
            }
            else if (!Model.CanClaim)
            {
                return;
            }

            busy = true;
            PendingGameMutation pending = null;
            try
            {
                Model = HiveDailyRoundPresentation.Mutating(
                    HiveDailyRoundScreenState.PreparingClaim,
                    snapshot,
                    outbox.IsProtectionAvailable,
                    retry);
                pending = await outbox.TryLoadAsync(
                    snapshot.PlayerId,
                    hiveId,
                    HiveDailyRoundClient.ContractVersion,
                    HiveDailyRoundClient.ClaimPath(hiveId),
                    lifetime.Token);
                if (outbox.LastLoadDetectedCorruption)
                {
                    Model = HiveDailyRoundPresentation.Error(
                        snapshot,
                        "claim_recovery_refresh_required",
                        outbox.IsProtectionAvailable);
                    return;
                }

                if (pending != null &&
                    (!SameDay(pending, snapshot.DayUtc) ||
                     pending.ExpectedRevision != snapshot.Revision))
                {
                    await outbox.DeletePartitionAsync(
                        snapshot.PlayerId,
                        hiveId,
                        HiveDailyRoundClient.ContractVersion,
                        HiveDailyRoundClient.ClaimPath(hiveId),
                        lifetime.Token);
                    pending = null;
                    if (retry)
                    {
                        Model = HiveDailyRoundPresentation.Error(
                            snapshot,
                            "claim_changed_refresh_required",
                            outbox.IsProtectionAvailable);
                        return;
                    }
                }

                if (pending == null)
                {
                    pending = new PendingGameMutation
                    {
                        PlayerId = snapshot.PlayerId,
                        HiveId = hiveId,
                        Contract = HiveDailyRoundClient.ContractVersion,
                        Path = HiveDailyRoundClient.ClaimPath(hiveId),
                        Method = "POST",
                        ExpectedDayUtc = snapshot.DayUtc.ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture),
                        ExpectedRevision = snapshot.Revision,
                        IdempotencyKey = keySource.Create(),
                        CreatedAtUtc = snapshot.ServerTimeUtc
                    };
                    await outbox.SavePreparedAsync(pending, lifetime.Token);
                }

                Model = HiveDailyRoundPresentation.Mutating(
                    HiveDailyRoundScreenState.Claiming,
                    snapshot,
                    outbox.IsProtectionAvailable,
                    true);
                RemoteHiveDailyRoundClaimResponse response =
                    await client.ClaimAsync(
                        hiveId,
                        ParseDay(pending.ExpectedDayUtc),
                        pending.ExpectedRevision,
                        pending.IdempotencyKey,
                        lifetime.Token);
                if (disposed) return;
                snapshot = response.Snapshot;
                await DeletePendingBestEffortAsync(
                    snapshot.PlayerId,
                    CancellationToken.None);
                Model = HiveDailyRoundPresentation.Ready(
                    snapshot,
                    outbox.IsProtectionAvailable,
                    response.Receipt.Code);
            }
            catch (OperationCanceledException)
            {
                if (!disposed)
                    Model = HiveDailyRoundPresentation.Mutating(
                        HiveDailyRoundScreenState.ClaimPendingConfirmation,
                        snapshot,
                        outbox.IsProtectionAvailable,
                        pending != null);
            }
            catch (HivePerimeterClientException error)
            {
                if (disposed) return;
                string code = StableError(error);
                if (error.Error == HivePerimeterClientError.TransportFailure)
                {
                    Model = HiveDailyRoundPresentation.Mutating(
                        HiveDailyRoundScreenState.ClaimPendingConfirmation,
                        snapshot,
                        outbox.IsProtectionAvailable,
                        pending != null);
                }
                else
                {
                    await DeletePendingBestEffortAsync(
                        snapshot.PlayerId,
                        CancellationToken.None);
                    Model = HiveDailyRoundPresentation.Error(
                        snapshot,
                        code,
                        outbox.IsProtectionAvailable);
                }
            }
            catch (Exception error)
            {
                if (!disposed)
                    Model = HiveDailyRoundPresentation.Error(
                        snapshot,
                        IsProtectedStoreFailure(error)
                            ? "protected_storage_unavailable"
                            : "unexpected",
                        outbox.IsProtectionAvailable);
            }
            finally
            {
                busy = false;
            }
        }

        private async Task<PendingGameMutation> LoadPendingBestEffortAsync(
            RemoteHiveDailyRoundSnapshot current,
            CancellationToken cancellationToken)
        {
            if (!outbox.IsProtectionAvailable || current == null) return null;
            return await outbox.TryLoadAsync(
                current.PlayerId,
                hiveId,
                HiveDailyRoundClient.ContractVersion,
                HiveDailyRoundClient.ClaimPath(hiveId),
                cancellationToken);
        }

        private async Task DeletePendingBestEffortAsync(
            Guid playerId,
            CancellationToken cancellationToken)
        {
            if (!outbox.IsProtectionAvailable || playerId == Guid.Empty) return;
            try
            {
                await outbox.DeletePartitionAsync(
                    playerId,
                    hiveId,
                    HiveDailyRoundClient.ContractVersion,
                    HiveDailyRoundClient.ClaimPath(hiveId),
                    cancellationToken);
            }
            catch
            {
            }
        }

        private static bool SameDay(
            PendingGameMutation pending,
            DateTimeOffset dayUtc)
        {
            return pending != null &&
                string.Equals(
                    pending.ExpectedDayUtc,
                    dayUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    StringComparison.Ordinal);
        }

        private static DateTimeOffset ParseDay(string value)
        {
            DateTime parsed = DateTime.ParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            return new DateTimeOffset(parsed, TimeSpan.Zero);
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
            if (string.Equals(
                remote,
                "game.daily_round_day_changed",
                StringComparison.Ordinal))
                return "day_changed";
            if (string.Equals(
                remote,
                "game.revision_conflict",
                StringComparison.Ordinal))
                return "revision_conflict";
            if (string.Equals(
                remote,
                "game.daily_round_incomplete",
                StringComparison.Ordinal))
                return "incomplete";
            if (string.Equals(
                remote,
                "game.daily_round_already_claimed",
                StringComparison.Ordinal))
                return "already_claimed";
            if (string.Equals(
                remote,
                "game.storage_capacity_insufficient",
                StringComparison.Ordinal))
                return "storage_capacity";
            if (string.Equals(
                remote,
                "game.idempotency_conflict",
                StringComparison.Ordinal))
                return "idempotency_conflict";
            if (string.Equals(remote, "game.unavailable", StringComparison.Ordinal))
                return "server_unavailable";

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
