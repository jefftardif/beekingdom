using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // Panneau officiel du ledger Rewards : consultation server-authoritative des recompenses
    // claimables et des evenements (queue_completed, reward_granted, reward_claimed). Lecture
    // seule - les octrois restent au serveur et aux routes admin.
    public enum HiveRewardLedgerScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        Error = 3
    }

    public sealed class HiveRewardLedgerScreenModel
    {
        public HiveRewardLedgerScreenState State { get; set; } = HiveRewardLedgerScreenState.NotConfigured;
        public string ErrorCode { get; set; } = string.Empty;
        public long Revision { get; set; }
        public IReadOnlyList<RemoteRewardLedgerEntry> Rewards { get; set; } = Array.Empty<RemoteRewardLedgerEntry>();
        public IReadOnlyList<RemoteRewardLedgerEvent> Events { get; set; } = Array.Empty<RemoteRewardLedgerEvent>();

        public int PendingRewardCount { get { int count = 0; foreach (RemoteRewardLedgerEntry reward in Rewards) if (!reward.Claimed) count++; return count; } }
    }

    public interface IHiveRewardLedgerPanelController
    {
        HiveRewardLedgerScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
    }

    public sealed class UnavailableHiveRewardLedgerPanelController : IHiveRewardLedgerPanelController
    {
        public HiveRewardLedgerScreenModel Model { get; } = new HiveRewardLedgerScreenModel();
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
    }

    public sealed class HiveRewardLedgerPanelController : IHiveRewardLedgerPanelController, IDisposable
    {
        private readonly IHiveRewardLedgerClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;

        public HiveRewardLedgerPanelController(IHiveRewardLedgerClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            Model = new HiveRewardLedgerScreenModel { State = HiveRewardLedgerScreenState.Loading };
        }

        public HiveRewardLedgerScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh() => Forget(RefreshCoreAsync());

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private static async void Forget(Task task)
        {
            try { await task; } catch (OperationCanceledException) { } catch { }
        }

        private async Task RefreshCoreAsync()
        {
            if (busy || disposed) return;
            busy = true;
            try
            {
                RemoteRewardLedgerSnapshot snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                Model.Revision = snapshot.Revision;
                Model.Rewards = (IReadOnlyList<RemoteRewardLedgerEntry>)snapshot.Rewards ?? Array.Empty<RemoteRewardLedgerEntry>();
                Model.Events = (IReadOnlyList<RemoteRewardLedgerEvent>)snapshot.Events ?? Array.Empty<RemoteRewardLedgerEvent>();
                Model.State = HiveRewardLedgerScreenState.Ready;
            }
            catch (HiveRewardLedgerClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private void SetError(string code)
        {
            Model.ErrorCode = code;
            Model.State = HiveRewardLedgerScreenState.Error;
        }

        private static string StableError(HiveRewardLedgerClientException error)
        {
            switch (error.Message)
            {
                case "game.unavailable": return "server_unavailable";
            }
            switch (error.Error)
            {
                case HiveRewardLedgerClientError.NotConfigured: return "not_configured";
                case HiveRewardLedgerClientError.AuthenticationRequired: return "authentication_required";
                case HiveRewardLedgerClientError.InvalidRequest: return "invalid_request";
                case HiveRewardLedgerClientError.TransportFailure: return "network_unavailable";
                default: return "invalid_response";
            }
        }
    }
}
