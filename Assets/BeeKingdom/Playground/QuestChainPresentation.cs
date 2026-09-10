using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // M077-CL: petite chaine d'objectifs Alpha persistante - voir QuestChainService cote serveur.
    // Frere structurel de HiveMilestoneEventPresentation (meme patron simple, pas de file
    // d'attente hors-ligne) mais chaque objectif se reclame individuellement au lieu d'un seul
    // lot agrege, et rien n'expire jamais.
    public enum QuestChainScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        Error = 3,
        Mutating = 4
    }

    public sealed class QuestChainScreenModel
    {
        public QuestChainScreenState State { get; set; } = QuestChainScreenState.NotConfigured;
        public string ErrorCode { get; set; } = string.Empty;
        public long Revision { get; set; }
        public IReadOnlyList<RemoteQuestChainObjective> Objectives { get; set; } = Array.Empty<RemoteQuestChainObjective>();

        public int CompletedCount => Objectives?.Count(o => o.Done) ?? 0;
        public int ClaimedCount => Objectives?.Count(o => o.Claimed) ?? 0;
        public bool AnyClaimable => Objectives?.Any(o => o.CanClaim) ?? false;
    }

    public interface IQuestChainPanelController
    {
        QuestChainScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        void ReportWorldMapVisited();
        void Claim(string objectiveKey);
    }

    public sealed class UnavailableQuestChainPanelController : IQuestChainPanelController
    {
        public QuestChainScreenModel Model { get; } = new QuestChainScreenModel();
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public void ReportWorldMapVisited() { }
        public void Claim(string objectiveKey) { }
    }

    public sealed class QuestChainPanelController : IQuestChainPanelController, IDisposable
    {
        private readonly IQuestChainClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;

        public QuestChainPanelController(IQuestChainClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            Model = new QuestChainScreenModel { State = QuestChainScreenState.Loading };
        }

        public QuestChainScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh() => Forget(RefreshCoreAsync());
        public void ReportWorldMapVisited() => Forget(ReportWorldMapVisitedCoreAsync());
        public void Claim(string objectiveKey) => Forget(ClaimCoreAsync(objectiveKey));

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
                RemoteQuestChainSnapshot snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
            }
            catch (QuestChainClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        // Auto-declaration silencieuse (pas d'etat Mutating, pas d'erreur affichee) : appelee des
        // qu'un bootstrap independant detecte que la World Map vient de s'ouvrir. Idempotent
        // cote serveur, donc sans risque d'etre appelee plusieurs fois par session.
        private async Task ReportWorldMapVisitedCoreAsync()
        {
            if (disposed) return;
            try
            {
                RemoteQuestChainSnapshot snapshot = await client.ReportWorldMapVisitedAsync(hiveId, lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
            }
            catch (QuestChainClientException) { }
            catch (Exception) { }
        }

        private async Task ClaimCoreAsync(string objectiveKey)
        {
            RemoteQuestChainObjective target = Model.Objectives?.FirstOrDefault(o => string.Equals(o.ObjectiveKey, objectiveKey, StringComparison.Ordinal));
            if (busy || disposed || target == null || !target.CanClaim) return;
            busy = true;
            Model.State = QuestChainScreenState.Mutating;
            try
            {
                RemoteQuestChainSnapshot snapshot = await client.ClaimAsync(hiveId, objectiveKey, Model.Revision, NewKey("claim-" + objectiveKey), lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
                HiveViewProductUiPresenter.NotifyStockMightHaveChanged();
            }
            catch (QuestChainClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private void ApplySnapshot(RemoteQuestChainSnapshot snapshot)
        {
            Model.Revision = snapshot.Revision;
            Model.Objectives = (IReadOnlyList<RemoteQuestChainObjective>)snapshot.Objectives ?? Array.Empty<RemoteQuestChainObjective>();
            Model.State = QuestChainScreenState.Ready;
        }

        private void SetError(string code)
        {
            Model.ErrorCode = code;
            Model.State = QuestChainScreenState.Error;
        }

        private static string NewKey(string operation) => "mobile-quest-chain-" + operation + "-" + Guid.NewGuid().ToString("N");

        private static string StableError(QuestChainClientException error)
        {
            switch (error.Message)
            {
                case "game.quest_incomplete": return "quest_incomplete";
                case "game.quest_already_claimed": return "quest_already_claimed";
                case "game.invalid_request": return "invalid_request";
                case "game.revision_conflict": return "revision_conflict";
                case "game.idempotency_conflict": return "idempotency_conflict";
                case "game.unavailable": return "server_unavailable";
            }
            switch (error.Error)
            {
                case QuestChainClientError.NotConfigured: return "not_configured";
                case QuestChainClientError.AuthenticationRequired: return "authentication_required";
                case QuestChainClientError.InvalidRequest: return "invalid_request";
                case QuestChainClientError.TransportFailure: return "network_unavailable";
                default: return "invalid_response";
            }
        }
    }
}
