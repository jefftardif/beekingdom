using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // Panneau officiel SpeedUp : inventaire et timers server-authoritatifs, application
    // idempotente d'un item (consommation serveur). Le serveur valide item/categorie/duree ;
    // le client ne calcule jamais de reduction locale. La demo locale (SpeedUpInventory) reste
    // un separate preview et ne fusionne rien dans cet etat.
    public enum HiveSpeedUpScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        Error = 3,
        Mutating = 4
    }

    public sealed class HiveSpeedUpScreenModel
    {
        public HiveSpeedUpScreenState State { get; set; } = HiveSpeedUpScreenState.NotConfigured;
        public string ErrorCode { get; set; } = string.Empty;
        public long Revision { get; set; }
        public IReadOnlyDictionary<string, int> Inventory { get; set; } = new Dictionary<string, int>();
        public IReadOnlyList<RemoteSpeedUpTimer> Timers { get; set; } = Array.Empty<RemoteSpeedUpTimer>();
        public IReadOnlyList<string> Rewards { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> Events { get; set; } = Array.Empty<string>();

        public int TotalItemCount { get { int count = 0; foreach (KeyValuePair<string, int> item in Inventory) count += item.Value; return count; } }
    }

    public interface IHiveSpeedUpPanelController
    {
        HiveSpeedUpScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        bool TryApply(string itemId, string category, string targetId, long durationSeconds);
    }

    public sealed class UnavailableHiveSpeedUpPanelController : IHiveSpeedUpPanelController
    {
        public HiveSpeedUpScreenModel Model { get; } = new HiveSpeedUpScreenModel();
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public bool TryApply(string itemId, string category, string targetId, long durationSeconds) => false;
    }

    public sealed class HiveSpeedUpPanelController : IHiveSpeedUpPanelController, IDisposable
    {
        private readonly IHiveSpeedUpClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;

        public HiveSpeedUpPanelController(IHiveSpeedUpClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            Model = new HiveSpeedUpScreenModel { State = HiveSpeedUpScreenState.Loading };
        }

        public HiveSpeedUpScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh() => Forget(RefreshCoreAsync());

        public bool TryApply(string itemId, string category, string targetId, long durationSeconds)
        {
            if (busy || disposed || Model.State != HiveSpeedUpScreenState.Ready) return false;
            if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(targetId) || durationSeconds <= 0) return false;
            if (!Model.Inventory.TryGetValue(itemId, out int quantity) || quantity <= 0) return false;
            Forget(ApplyCoreAsync(itemId, category, targetId, durationSeconds));
            return true;
        }

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
                RemoteSpeedUpReadSnapshot snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
            }
            catch (HiveSpeedUpClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task ApplyCoreAsync(string itemId, string category, string targetId, long durationSeconds)
        {
            if (disposed) return;
            busy = true;
            Model.State = HiveSpeedUpScreenState.Mutating;
            try
            {
                var request = new ApplySpeedUpMutationRequest
                {
                    ItemId = itemId,
                    Category = category,
                    TargetId = targetId,
                    DurationSeconds = durationSeconds,
                    ExpectedRevision = Model.Revision,
                    IdempotencyKey = "mobile-speedup-apply-" + Guid.NewGuid().ToString("N")
                };
                RemoteSpeedUpApplyResponse response = await client.ApplyAsync(hiveId, request, lifetime.Token);
                if (disposed) return;
                ApplySnapshot(response.Snapshot);
            }
            catch (HiveSpeedUpClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private void ApplySnapshot(RemoteSpeedUpReadSnapshot snapshot)
        {
            Model.Revision = snapshot.Revision;
            Dictionary<string, int> inventory = new Dictionary<string, int>(StringComparer.Ordinal);
            if (snapshot.Inventory != null && snapshot.Inventory.Items != null)
                foreach (KeyValuePair<string, int> item in snapshot.Inventory.Items)
                    if (item.Value > 0) inventory[item.Key] = item.Value;
            Model.Inventory = inventory;
            Model.Timers = (IReadOnlyList<RemoteSpeedUpTimer>)snapshot.Timers ?? Array.Empty<RemoteSpeedUpTimer>();
            Model.Rewards = (IReadOnlyList<string>)snapshot.Rewards ?? Array.Empty<string>();
            Model.Events = (IReadOnlyList<string>)snapshot.Events ?? Array.Empty<string>();
            Model.State = HiveSpeedUpScreenState.Ready;
        }

        private void SetError(string code)
        {
            Model.ErrorCode = code;
            Model.State = HiveSpeedUpScreenState.Error;
        }

        private static string StableError(HiveSpeedUpClientException error)
        {
            switch (error.Message)
            {
                case "game.invalid_speedup": return "invalid_speedup";
                case "game.inventory_insufficient": return "inventory_insufficient";
                case "game.timer_not_found": return "timer_not_found";
                case "game.revision_conflict": return "revision_conflict";
                case "game.idempotency_conflict": return "idempotency_conflict";
                case "game.category_unsupported": return "category_unsupported";
                case "game.unavailable": return "server_unavailable";
            }
            switch (error.Error)
            {
                case HiveSpeedUpClientError.NotConfigured: return "not_configured";
                case HiveSpeedUpClientError.AuthenticationRequired: return "authentication_required";
                case HiveSpeedUpClientError.InvalidRequest: return "invalid_request";
                case HiveSpeedUpClientError.TransportFailure: return "network_unavailable";
                default: return "invalid_response";
            }
        }
    }
}
