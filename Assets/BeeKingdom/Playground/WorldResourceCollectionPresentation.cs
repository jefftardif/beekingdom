using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // Rend reel le vol de collecte deja visible sur la carte du monde (bouton "Collecter" existant
    // dans WorldMapMmoFullscreenFoundationBootstrap, jusqu'ici purement local/demo - voir l'audit du
    // 2026-07-31). Meme patron simple que CombatPatrolPresentation : pas de file d'attente hors-ligne,
    // une action discretionnaire et peu frequente.
    public enum WorldResourceCollectionScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        Ready = 2,
        Active = 3,
        ClaimReady = 4,
        Debrief = 5,
        Error = 6,
        Mutating = 7
    }

    public sealed class WorldResourceCollectionDebrief
    {
        public string NodeId { get; set; }
        public string ResourceKey { get; set; }
        public long CreditedAmount { get; set; }
        public bool DailyFocusApplied { get; set; }
        public bool WorldEventApplied { get; set; }
        public string WorldEventKey { get; set; } = string.Empty;
    }

    public sealed class WorldResourceCollectionScreenModel
    {
        public WorldResourceCollectionScreenState State { get; set; } = WorldResourceCollectionScreenState.NotConfigured;
        public string ErrorCode { get; set; } = string.Empty;
        public long Revision { get; set; }
        public IReadOnlyList<RemoteWorldResourceNode> Nodes { get; set; } = Array.Empty<RemoteWorldResourceNode>();
        public RemoteWorldResourceActiveFlight Active { get; set; }
        public TimeSpan RemainingAtRead { get; set; }
        public WorldResourceCollectionDebrief Debrief { get; set; }
        // Cible du jour (demande de Jeff, 2026-07-31) : quel noeud recoit un bonus de recompense
        // aujourd'hui - pure info d'affichage, la validation reste serveur.
        public string FeaturedNodeId { get; set; }
        // Evenement mondial dynamique (demande de Jeff, 2026-08-01) : meteo/menace active,
        // change plusieurs fois par jour - pure info d'affichage, la validation reste serveur.
        public RemoteActiveWorldEvent WorldEvent { get; set; }

        // Escouade reellement engagee (demande de Jeff, 2026-08-01) : premiere brique de
        // l'architecture de deploiement reutilisable plus tard (PvP, raids, renforts, occupation de
        // points d'interet) - meme patron de brouillon que CombatPatrolScreenModel.
        public int DraftGuardians { get; set; }
        public int DraftWingrunners { get; set; }
        public int DraftDarters { get; set; }
        public IReadOnlyDictionary<string, long> AvailableRoster { get; set; } = new Dictionary<string, long>();

        public RemoteWorldResourceNode ActiveNode => Active == null ? null : Nodes?.FirstOrDefault(n => n.NodeId == Active.NodeId);
        public int DraftTotal => DraftGuardians + DraftWingrunners + DraftDarters;
        public bool CanLaunch(string nodeId) =>
            State != WorldResourceCollectionScreenState.Mutating && Active == null && DraftTotal > 0 &&
            DraftGuardians <= AvailableRoster.GetValueOrDefault("guardians") &&
            DraftWingrunners <= AvailableRoster.GetValueOrDefault("wingrunners") &&
            DraftDarters <= AvailableRoster.GetValueOrDefault("darters") &&
            (Nodes?.FirstOrDefault(n => n.NodeId == nodeId)?.CanLaunch ?? false);
        public bool CanClaim => Active != null && State == WorldResourceCollectionScreenState.ClaimReady;
        public bool CanRecall => Active != null && (State == WorldResourceCollectionScreenState.Active || State == WorldResourceCollectionScreenState.ClaimReady);
    }

    public interface IWorldResourceCollectionPanelController
    {
        WorldResourceCollectionScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        void AdjustDraft(string family, int delta);
        void Launch(string nodeId);
        void Claim();
        void Recall();
        void DismissDebrief();
    }

    public sealed class UnavailableWorldResourceCollectionPanelController : IWorldResourceCollectionPanelController
    {
        public WorldResourceCollectionScreenModel Model { get; } = new WorldResourceCollectionScreenModel();
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public void AdjustDraft(string family, int delta) { }
        public void Launch(string nodeId) { }
        public void Claim() { }
        public void Recall() { }
        public void DismissDebrief() { }
    }

    public sealed class WorldResourceCollectionPanelController : IWorldResourceCollectionPanelController, IDisposable
    {
        private readonly IWorldResourceCollectionClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;

        public WorldResourceCollectionPanelController(IWorldResourceCollectionClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            Model = new WorldResourceCollectionScreenModel { State = WorldResourceCollectionScreenState.Loading };
        }

        public WorldResourceCollectionScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh() => Forget(RefreshCoreAsync());

        public void AdjustDraft(string family, int delta)
        {
            if (busy || disposed) return;
            long available = Model.AvailableRoster.GetValueOrDefault(family);
            int max = (int)Math.Min(int.MaxValue, available);
            int current = family switch
            {
                "guardians" => Model.DraftGuardians,
                "wingrunners" => Model.DraftWingrunners,
                "darters" => Model.DraftDarters,
                _ => 0
            };
            int next = Math.Max(0, Math.Min(max, current + delta));
            switch (family)
            {
                case "guardians": Model.DraftGuardians = next; break;
                case "wingrunners": Model.DraftWingrunners = next; break;
                case "darters": Model.DraftDarters = next; break;
            }
        }

        public void Launch(string nodeId) => Forget(LaunchCoreAsync(nodeId));
        public void Claim() => Forget(ClaimCoreAsync());
        public void Recall() => Forget(RecallCoreAsync());

        public void DismissDebrief()
        {
            if (Model.State != WorldResourceCollectionScreenState.Debrief) return;
            Model.Debrief = null;
            Forget(RefreshCoreAsync());
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
                RemoteWorldResourceCollectionSnapshot snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
            }
            catch (WorldResourceCollectionClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task LaunchCoreAsync(string nodeId)
        {
            if (busy || disposed || !Model.CanLaunch(nodeId)) return;
            int guardians = Model.DraftGuardians, wingrunners = Model.DraftWingrunners, darters = Model.DraftDarters;
            busy = true;
            Model.State = WorldResourceCollectionScreenState.Mutating;
            try
            {
                RemoteWorldResourceCollectionSnapshot snapshot = await client.LaunchAsync(hiveId, nodeId, guardians, wingrunners, darters, Model.Revision, NewKey("launch"), lifetime.Token);
                if (disposed) return;
                Model.DraftGuardians = 0;
                Model.DraftWingrunners = 0;
                Model.DraftDarters = 0;
                ApplySnapshot(snapshot);
            }
            catch (WorldResourceCollectionClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task RecallCoreAsync()
        {
            RemoteWorldResourceActiveFlight target = Model.Active;
            if (busy || disposed || target == null) return;
            busy = true;
            Model.State = WorldResourceCollectionScreenState.Mutating;
            try
            {
                RemoteWorldResourceCollectionSnapshot snapshot = await client.RecallAsync(hiveId, target.FlightId, Model.Revision, NewKey("recall"), lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
            }
            catch (WorldResourceCollectionClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task ClaimCoreAsync()
        {
            RemoteWorldResourceActiveFlight target = Model.Active;
            if (busy || disposed || target == null) return;
            busy = true;
            Model.State = WorldResourceCollectionScreenState.Mutating;
            try
            {
                RemoteWorldResourceCollectionSnapshot snapshot = await client.ClaimAsync(hiveId, target.FlightId, Model.Revision, NewKey("claim"), lifetime.Token);
                if (disposed) return;
                if (snapshot.ClaimReceipt != null)
                {
                    Model.Debrief = new WorldResourceCollectionDebrief
                    {
                        NodeId = snapshot.ClaimReceipt.NodeId,
                        ResourceKey = snapshot.ClaimReceipt.ResourceKey,
                        CreditedAmount = snapshot.ClaimReceipt.CreditedAmount,
                        DailyFocusApplied = snapshot.ClaimReceipt.DailyFocusApplied,
                        WorldEventApplied = snapshot.ClaimReceipt.WorldEventApplied,
                        WorldEventKey = snapshot.ClaimReceipt.WorldEventKey ?? string.Empty
                    };
                }
                ApplySnapshot(snapshot);
                if (Model.Debrief != null) Model.State = WorldResourceCollectionScreenState.Debrief;
            }
            catch (WorldResourceCollectionClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private void ApplySnapshot(RemoteWorldResourceCollectionSnapshot snapshot)
        {
            Model.Revision = snapshot.Revision;
            Model.Nodes = (IReadOnlyList<RemoteWorldResourceNode>)snapshot.Nodes ?? Array.Empty<RemoteWorldResourceNode>();
            Model.Active = snapshot.Active;
            Model.FeaturedNodeId = snapshot.FeaturedNodeId;
            Model.WorldEvent = snapshot.WorldEvent;
            Model.AvailableRoster = (IReadOnlyDictionary<string, long>)snapshot.AvailableRoster ?? new Dictionary<string, long>();

            if (Model.Debrief == null)
            {
                if (Model.Active != null)
                {
                    TimeSpan remaining = Model.Active.EndsAtUtc - snapshot.ServerTimeUtc;
                    Model.RemainingAtRead = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
                    Model.State = remaining <= TimeSpan.Zero ? WorldResourceCollectionScreenState.ClaimReady : WorldResourceCollectionScreenState.Active;
                }
                else
                {
                    Model.State = WorldResourceCollectionScreenState.Ready;
                }
            }
        }

        private void SetError(string code)
        {
            Model.ErrorCode = code;
            Model.State = WorldResourceCollectionScreenState.Error;
        }

        private static string NewKey(string operation) => "mobile-world-resources-" + operation + "-" + Guid.NewGuid().ToString("N");

        private static string StableError(WorldResourceCollectionClientException error)
        {
            switch (error.Message)
            {
                case "game.world_resource_busy": return "world_resource_busy";
                case "game.world_resource_cooling_down": return "world_resource_cooling_down";
                case "game.world_resource_not_ready": return "world_resource_not_ready";
                case "game.world_resource_insufficient_troops": return "world_resource_insufficient_troops";
                case "game.invalid_request": return "invalid_request";
                case "game.revision_conflict": return "revision_conflict";
                case "game.idempotency_conflict": return "idempotency_conflict";
                case "game.unavailable": return "server_unavailable";
            }
            switch (error.Error)
            {
                case WorldResourceCollectionClientError.NotConfigured: return "not_configured";
                case WorldResourceCollectionClientError.AuthenticationRequired: return "authentication_required";
                case WorldResourceCollectionClientError.InvalidRequest: return "invalid_request";
                case WorldResourceCollectionClientError.TransportFailure: return "network_unavailable";
                default: return "invalid_response";
            }
        }
    }
}
