using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // M076I-CL: Courrier (rapports de combat, invitations d'Alliance, recompenses) persiste
    // cote SERVEUR - demande CEO explicite : "il faut que ce soit sauvegarde sur le serveur,
    // nous ne conservons rien localement" (un premier correctif PlayerPrefs local a ete juge
    // insuffisant). Meme patron simple (pas de file d'attente hors-ligne, mutation optimiste
    // immediate puis reconciliation avec la reponse serveur) que QuestChainPanelController,
    // dont ce controleur est un frere structurel. Le client reste seul responsable de DERIVER
    // le contenu de chaque message (combat/alliance restent la source de verite pour leur
    // propre contenu) - ce controleur ne fait que persister la boite deja construite.
    public enum CourierMailboxScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        Error = 3
    }

    public sealed class CourierMailboxScreenModel
    {
        public CourierMailboxScreenState State { get; set; } = CourierMailboxScreenState.NotConfigured;
        public string ErrorCode { get; set; } = string.Empty;
        public IReadOnlyList<RemoteCourierMessage> Messages { get; set; } = Array.Empty<RemoteCourierMessage>();
    }

    public interface ICourierMailboxPanelController
    {
        CourierMailboxScreenModel Model { get; }
        bool IsConfigured { get; }
        void Refresh();
        void Append(AppendCourierMessageMutationRequest message);
        void SetRead(string messageId, bool read);
        void SetFavorite(string messageId, bool favorite);
        void CollectRewards(string messageId);
        void MarkAllRead();
        void DeleteRead();
    }

    public sealed class UnavailableCourierMailboxPanelController : ICourierMailboxPanelController
    {
        public CourierMailboxScreenModel Model { get; } = new CourierMailboxScreenModel();
        public bool IsConfigured => false;
        public void Refresh() { }
        public void Append(AppendCourierMessageMutationRequest message) { }
        public void SetRead(string messageId, bool read) { }
        public void SetFavorite(string messageId, bool favorite) { }
        public void CollectRewards(string messageId) { }
        public void MarkAllRead() { }
        public void DeleteRead() { }
    }

    public sealed class CourierMailboxPanelController : ICourierMailboxPanelController, IDisposable
    {
        private readonly ICourierMailboxClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool refreshing;

        public CourierMailboxPanelController(ICourierMailboxClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            Model = new CourierMailboxScreenModel { State = CourierMailboxScreenState.Loading };
        }

        public CourierMailboxScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;

        public void Refresh() => Forget(RefreshCoreAsync());
        public void Append(AppendCourierMessageMutationRequest message) => Forget(AppendCoreAsync(message));
        public void SetRead(string messageId, bool read) => Forget(SetReadCoreAsync(messageId, read));
        public void SetFavorite(string messageId, bool favorite) => Forget(SetFavoriteCoreAsync(messageId, favorite));
        public void CollectRewards(string messageId) => Forget(CollectRewardsCoreAsync(messageId));
        public void MarkAllRead() => Forget(MarkAllReadCoreAsync());
        public void DeleteRead() => Forget(DeleteReadCoreAsync());

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
            if (refreshing || disposed) return;
            refreshing = true;
            try
            {
                RemoteCourierMailboxSnapshot snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
            }
            catch (CourierMailboxClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { refreshing = false; }
        }

        // Append est deliberement silencieux (pas d'etat Error affiche a l'echec) : appele en
        // continu depuis CheckForNewCombatReportAlerts/CheckForNewAllianceInvitationAlerts a
        // chaque frame ou un nouveau receipt/invitation apparait - une panne reseau passagere ne
        // doit jamais faire disparaitre le reste du courrier deja affiche. Optimiste : le message
        // est visible immediatement, avant meme la reponse serveur.
        private async Task AppendCoreAsync(AppendCourierMessageMutationRequest message)
        {
            if (disposed || message == null) return;
            List<RemoteCourierMessage> optimistic = new List<RemoteCourierMessage>(Model.Messages.Count + 1)
            {
                new RemoteCourierMessage
                {
                    Id = message.Id, Category = message.Category, Title = message.Title,
                    Preview = message.Preview ?? string.Empty, Body = message.Body ?? string.Empty,
                    CreatedAtUtc = DateTimeOffset.UtcNow, Read = false, Favorite = false,
                    Rewards = message.Rewards ?? new List<RemoteCourierReward>()
                }
            };
            optimistic.AddRange(Model.Messages);
            Model.Messages = optimistic;
            try
            {
                RemoteCourierMailboxSnapshot snapshot = await client.AppendAsync(hiveId, message, lifetime.Token);
                if (!disposed) ApplySnapshot(snapshot);
            }
            catch (CourierMailboxClientException) { }
            catch (Exception) { }
        }

        private async Task SetReadCoreAsync(string messageId, bool read)
        {
            if (disposed) return;
            MutateLocal(messageId, m => m.Read = read);
            try
            {
                RemoteCourierMailboxSnapshot snapshot = await client.SetReadAsync(hiveId, messageId, read, lifetime.Token);
                if (!disposed) ApplySnapshot(snapshot);
            }
            catch (CourierMailboxClientException) { }
            catch (Exception) { }
        }

        private async Task SetFavoriteCoreAsync(string messageId, bool favorite)
        {
            if (disposed) return;
            MutateLocal(messageId, m => m.Favorite = favorite);
            try
            {
                RemoteCourierMailboxSnapshot snapshot = await client.SetFavoriteAsync(hiveId, messageId, favorite, lifetime.Token);
                if (!disposed) ApplySnapshot(snapshot);
            }
            catch (CourierMailboxClientException) { }
            catch (Exception) { }
        }

        private async Task CollectRewardsCoreAsync(string messageId)
        {
            if (disposed) return;
            MutateLocal(messageId, m => { foreach (RemoteCourierReward r in m.Rewards ?? new List<RemoteCourierReward>()) r.Collected = true; });
            try
            {
                RemoteCourierMailboxSnapshot snapshot = await client.CollectRewardsAsync(hiveId, messageId, lifetime.Token);
                if (!disposed) ApplySnapshot(snapshot);
            }
            catch (CourierMailboxClientException) { }
            catch (Exception) { }
        }

        private async Task MarkAllReadCoreAsync()
        {
            if (disposed) return;
            foreach (RemoteCourierMessage m in Model.Messages) m.Read = true;
            try
            {
                RemoteCourierMailboxSnapshot snapshot = await client.MarkAllReadAsync(hiveId, lifetime.Token);
                if (!disposed) ApplySnapshot(snapshot);
            }
            catch (CourierMailboxClientException) { }
            catch (Exception) { }
        }

        private async Task DeleteReadCoreAsync()
        {
            if (disposed) return;
            Model.Messages = Model.Messages.Where(m => !(m.Read && !m.Favorite)).ToList();
            try
            {
                RemoteCourierMailboxSnapshot snapshot = await client.DeleteReadAsync(hiveId, lifetime.Token);
                if (!disposed) ApplySnapshot(snapshot);
            }
            catch (CourierMailboxClientException) { }
            catch (Exception) { }
        }

        private void MutateLocal(string messageId, Action<RemoteCourierMessage> mutate)
        {
            RemoteCourierMessage target = Model.Messages.FirstOrDefault(m => string.Equals(m.Id, messageId, StringComparison.Ordinal));
            if (target != null) mutate(target);
        }

        private void ApplySnapshot(RemoteCourierMailboxSnapshot snapshot)
        {
            Model.Messages = (IReadOnlyList<RemoteCourierMessage>)snapshot.Messages ?? Array.Empty<RemoteCourierMessage>();
            Model.State = CourierMailboxScreenState.Ready;
        }

        private void SetError(string code)
        {
            Model.ErrorCode = code;
            Model.State = CourierMailboxScreenState.Error;
        }

        private static string StableError(CourierMailboxClientException error)
        {
            switch (error.Error)
            {
                case CourierMailboxClientError.NotConfigured: return "not_configured";
                case CourierMailboxClientError.AuthenticationRequired: return "authentication_required";
                case CourierMailboxClientError.InvalidRequest: return "invalid_request";
                case CourierMailboxClientError.TransportFailure: return "network_unavailable";
                default: return "invalid_response";
            }
        }
    }
}
