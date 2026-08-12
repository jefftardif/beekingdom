using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // Premier "evenement" du jeu : relie plusieurs systemes deja construits (voir
    // HiveMilestoneEventService cote serveur). Meme patron simple que
    // WorldResourceCollectionPresentation - une consultation/reclamation peu frequente.
    public enum HiveMilestoneEventScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        Error = 3,
        Mutating = 4
    }

    public sealed class HiveMilestoneEventScreenModel
    {
        public HiveMilestoneEventScreenState State { get; set; } = HiveMilestoneEventScreenState.NotConfigured;
        public string ErrorCode { get; set; } = string.Empty;
        public long Revision { get; set; }
        public IReadOnlyList<RemoteHiveMilestoneObjective> Objectives { get; set; } = Array.Empty<RemoteHiveMilestoneObjective>();
        public int RequiredObjectiveCount { get; set; }
        public bool Claimed { get; set; }
        public bool CanClaim { get; set; }
        public bool WindowExpired { get; set; }
        public DateTimeOffset WindowEndsAtUtc { get; set; }
        public IReadOnlyDictionary<string, long> Reward { get; set; } = new Dictionary<string, long>();

        public int CompletedCount => Objectives?.Count(o => o.Done) ?? 0;
    }

    public interface IHiveMilestoneEventPanelController
    {
        HiveMilestoneEventScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        void Claim();
    }

    public sealed class UnavailableHiveMilestoneEventPanelController : IHiveMilestoneEventPanelController
    {
        public HiveMilestoneEventScreenModel Model { get; } = new HiveMilestoneEventScreenModel();
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public void Claim() { }
    }

    public sealed class HiveMilestoneEventPanelController : IHiveMilestoneEventPanelController, IDisposable
    {
        private readonly IHiveMilestoneEventClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;

        public HiveMilestoneEventPanelController(IHiveMilestoneEventClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            Model = new HiveMilestoneEventScreenModel { State = HiveMilestoneEventScreenState.Loading };
        }

        public HiveMilestoneEventScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh() => Forget(RefreshCoreAsync());
        public void Claim() => Forget(ClaimCoreAsync());

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
                RemoteHiveMilestoneEventSnapshot snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
            }
            catch (HiveMilestoneEventClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task ClaimCoreAsync()
        {
            if (busy || disposed || !Model.CanClaim) return;
            busy = true;
            Model.State = HiveMilestoneEventScreenState.Mutating;
            try
            {
                RemoteHiveMilestoneEventSnapshot snapshot = await client.ClaimAsync(hiveId, Model.Revision, NewKey("claim"), lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
            }
            catch (HiveMilestoneEventClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private void ApplySnapshot(RemoteHiveMilestoneEventSnapshot snapshot)
        {
            Model.Revision = snapshot.Revision;
            Model.Objectives = (IReadOnlyList<RemoteHiveMilestoneObjective>)snapshot.Objectives ?? Array.Empty<RemoteHiveMilestoneObjective>();
            Model.RequiredObjectiveCount = snapshot.RequiredObjectiveCount;
            Model.Claimed = snapshot.Claimed;
            Model.CanClaim = snapshot.CanClaim;
            Model.WindowExpired = snapshot.WindowExpired;
            Model.WindowEndsAtUtc = snapshot.WindowEndsAtUtc;
            Model.Reward = snapshot.Reward ?? new Dictionary<string, long>();
            Model.State = HiveMilestoneEventScreenState.Ready;
        }

        private void SetError(string code)
        {
            Model.ErrorCode = code;
            Model.State = HiveMilestoneEventScreenState.Error;
        }

        private static string NewKey(string operation) => "mobile-milestone-event-" + operation + "-" + Guid.NewGuid().ToString("N");

        private static string StableError(HiveMilestoneEventClientException error)
        {
            switch (error.Message)
            {
                case "game.milestone_incomplete": return "milestone_incomplete";
                case "game.milestone_already_claimed": return "milestone_already_claimed";
                case "game.milestone_window_expired": return "milestone_window_expired";
                case "game.invalid_request": return "invalid_request";
                case "game.revision_conflict": return "revision_conflict";
                case "game.idempotency_conflict": return "idempotency_conflict";
                case "game.unavailable": return "server_unavailable";
            }
            switch (error.Error)
            {
                case HiveMilestoneEventClientError.NotConfigured: return "not_configured";
                case HiveMilestoneEventClientError.AuthenticationRequired: return "authentication_required";
                case HiveMilestoneEventClientError.InvalidRequest: return "invalid_request";
                case HiveMilestoneEventClientError.TransportFailure: return "network_unavailable";
                default: return "invalid_response";
            }
        }
    }
}
