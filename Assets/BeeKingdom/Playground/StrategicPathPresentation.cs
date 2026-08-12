using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // A single permanent hive-identity choice (royal_guard/striker/nurturer/scout/alchemist),
    // locked forever server-side once made. Deliberately as small as CombatPatrolPresentation:
    // no active state, no timers, no offline mutation outbox - a one-shot read + one-shot write.
    public enum StrategicPathScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Available = 2,
        Selected = 3,
        Mutating = 4,
        Error = 5
    }

    public sealed class StrategicPathScreenModel
    {
        public StrategicPathScreenState State { get; set; } = StrategicPathScreenState.NotConfigured;
        public string ErrorCode { get; set; } = string.Empty;
        public IReadOnlyList<string> CanonicalPaths { get; set; } = Array.Empty<string>();
        public string SelectedPath { get; set; }
        public long Revision { get; set; }

        public bool CanChoose(string pathId) =>
            State == StrategicPathScreenState.Available && SelectedPath == null &&
            CanonicalPaths != null && System.Linq.Enumerable.Contains(CanonicalPaths, pathId);
    }

    public interface IStrategicPathPanelController
    {
        StrategicPathScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        void Choose(string pathId);
    }

    public sealed class UnavailableStrategicPathPanelController : IStrategicPathPanelController
    {
        public StrategicPathScreenModel Model { get; } = new StrategicPathScreenModel();
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public void Choose(string pathId) { }
    }

    public sealed class StrategicPathPanelController : IStrategicPathPanelController, IDisposable
    {
        private readonly IStrategicPathClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;

        public StrategicPathPanelController(IStrategicPathClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            Model = new StrategicPathScreenModel { State = StrategicPathScreenState.Loading };
        }

        public StrategicPathScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh() => Forget(RefreshCoreAsync());
        public void Choose(string pathId) => Forget(ChooseCoreAsync(pathId));

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
                RemoteStrategicPathSnapshot snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
            }
            catch (StrategicPathClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task ChooseCoreAsync(string pathId)
        {
            if (busy || disposed || !Model.CanChoose(pathId)) return;
            busy = true;
            Model.State = StrategicPathScreenState.Mutating;
            try
            {
                RemoteStrategicPathSnapshot snapshot = await client.ChooseAsync(hiveId, pathId, Model.Revision, NewKey("choose"), lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
            }
            catch (StrategicPathClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private void ApplySnapshot(RemoteStrategicPathSnapshot snapshot)
        {
            Model.Revision = snapshot.Revision;
            Model.CanonicalPaths = (IReadOnlyList<string>)snapshot.CanonicalPaths ?? Array.Empty<string>();
            Model.SelectedPath = snapshot.SelectedPath;
            Model.State = snapshot.SelectedPath != null ? StrategicPathScreenState.Selected : StrategicPathScreenState.Available;
        }

        private void SetError(string code)
        {
            Model.ErrorCode = code;
            Model.State = StrategicPathScreenState.Error;
        }

        private static string NewKey(string operation) => "mobile-strategic-path-" + operation + "-" + Guid.NewGuid().ToString("N");

        private static string StableError(StrategicPathClientException error)
        {
            switch (error.Message)
            {
                case "game.strategic_path_ineligible": return "strategic_path_ineligible";
                case "game.strategic_path_locked": return "strategic_path_locked";
                case "game.revision_conflict": return "revision_conflict";
                case "game.idempotency_conflict": return "idempotency_conflict";
                case "game.invalid_request": return "invalid_request";
                case "game.unavailable": return "server_unavailable";
            }
            switch (error.Error)
            {
                case StrategicPathClientError.NotConfigured: return "not_configured";
                case StrategicPathClientError.AuthenticationRequired: return "authentication_required";
                case StrategicPathClientError.InvalidRequest: return "invalid_request";
                case StrategicPathClientError.TransportFailure: return "network_unavailable";
                default: return "invalid_response";
            }
        }
    }
}
