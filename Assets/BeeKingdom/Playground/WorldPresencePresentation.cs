using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // Monde vivant (demande de Jeff, 2026-08-01) : presence ambiante uniquement - lecture seule,
    // aucune interaction, aucun combat. Reutilise l'architecture d'escouades persistantes deja
    // construite pour Combat Patrol / Collecte mondiale : une occupation de ressource par un autre
    // joueur EST deja un deploiement reel, ce controleur se contente de l'afficher.
    public sealed class WorldPresenceScreenModel
    {
        public bool Loaded { get; set; }
        public IReadOnlyList<RemoteWorldPresenceSighting> Sightings { get; set; } = Array.Empty<RemoteWorldPresenceSighting>();
    }

    public interface IWorldPresencePanelController
    {
        WorldPresenceScreenModel Model { get; }
        bool IsConfigured { get; }
        void Refresh();
    }

    public sealed class UnavailableWorldPresencePanelController : IWorldPresencePanelController
    {
        public WorldPresenceScreenModel Model { get; } = new WorldPresenceScreenModel();
        public bool IsConfigured => false;
        public void Refresh() { }
    }

    public sealed class WorldPresencePanelController : IWorldPresencePanelController, IDisposable
    {
        private readonly IWorldPresenceClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;

        public WorldPresencePanelController(IWorldPresenceClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
        }

        public WorldPresenceScreenModel Model { get; } = new WorldPresenceScreenModel();
        public bool IsConfigured => !disposed;

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
                RemoteWorldPresenceSnapshot snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                Model.Sightings = (IReadOnlyList<RemoteWorldPresenceSighting>)snapshot.Sightings ?? Array.Empty<RemoteWorldPresenceSighting>();
                Model.Loaded = true;
            }
            catch (WorldPresenceClientException) { /* ambiance seulement - un echec silencieux ne prive le joueur d'aucune fonctionnalite reelle */ }
            catch (Exception) { }
            finally { busy = false; }
        }
    }
}
