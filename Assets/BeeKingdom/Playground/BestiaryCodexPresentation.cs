using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // Carnet du Bestiaire (demande de Jeff, 2026-08-01) : lecture seule, sous-produit du flux
    // Combat Patrol existant. L'etat "Apercue" par variante (purement cosmetique, jamais connu du
    // serveur) vit a part, dans LocalPreviewBestiarySightings - ce controleur ne gere que ce que le
    // serveur connait reellement : l'historique par TIER (Rencontree/Maitrisee/Legendaire).
    public sealed class BestiaryCodexScreenModel
    {
        public bool Loaded { get; set; }
        public IReadOnlyList<RemoteBestiaryCodexEntry> Tiers { get; set; } = Array.Empty<RemoteBestiaryCodexEntry>();
        public int MasteredTierCount { get; set; }
        public int TotalTierCount { get; set; }
        public long MasteryEncounterThreshold { get; set; }
    }

    public interface IBestiaryCodexPanelController
    {
        BestiaryCodexScreenModel Model { get; }
        bool IsConfigured { get; }
        void Refresh();
    }

    public sealed class UnavailableBestiaryCodexPanelController : IBestiaryCodexPanelController
    {
        public BestiaryCodexScreenModel Model { get; } = new BestiaryCodexScreenModel();
        public bool IsConfigured => false;
        public void Refresh() { }
    }

    public sealed class BestiaryCodexPanelController : IBestiaryCodexPanelController, IDisposable
    {
        private readonly IBestiaryCodexClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;

        public BestiaryCodexPanelController(IBestiaryCodexClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
        }

        public BestiaryCodexScreenModel Model { get; } = new BestiaryCodexScreenModel();
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
                RemoteBestiaryCodexSnapshot snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                Model.Tiers = (IReadOnlyList<RemoteBestiaryCodexEntry>)snapshot.Tiers ?? Array.Empty<RemoteBestiaryCodexEntry>();
                Model.MasteredTierCount = snapshot.MasteredTierCount;
                Model.TotalTierCount = snapshot.TotalTierCount;
                Model.MasteryEncounterThreshold = snapshot.MasteryEncounterThreshold;
                Model.Loaded = true;
            }
            catch (BestiaryCodexClientException) { /* le carnet n'ouvre pas de fenetre de jeu - un echec silencieux n'empeche aucune action reelle */ }
            catch (Exception) { }
            finally { busy = false; }
        }
    }
}
