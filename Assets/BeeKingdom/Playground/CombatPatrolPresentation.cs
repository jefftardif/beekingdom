using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    // PvE only (player squad vs. world map bestiary). Deliberately simpler than
    // HivePerimeterSortiePresentation: no offline mutation outbox/pending-retry queue —
    // that resilience layer is a documented follow-up, not required to prove the core loop.
    // Supports several concurrent patrols (1 free slot, up to 5 total via purchased slots) —
    // each active encounter carries its own committed troops, independent of the shared squad
    // reservation used by HivePerimeterSortie.
    public enum CombatPatrolScreenState
    {
        NotConfigured = 0,
        Loading = 1,
        ReadyToLaunch = 2,
        Blocked = 3,
        Active = 4,
        ClaimReady = 5,
        Debrief = 6,
        Error = 7,
        Mutating = 8
    }

    public sealed class CombatPatrolDebrief
    {
        public Guid EncounterId { get; set; }
        public int Tier { get; set; }
        public string Band { get; set; }
        public IReadOnlyDictionary<string, long> PermanentLosses { get; set; }
        public IReadOnlyDictionary<string, long> WoundedLosses { get; set; }
        public IReadOnlyDictionary<string, long> CreditedByResource { get; set; }
        public IReadOnlyList<string> ContributingChampionBeeIds { get; set; } = Array.Empty<string>();
        public IReadOnlyDictionary<string, long> ChampionPowerBonusBpByFamily { get; set; } = new Dictionary<string, long>();
        public IReadOnlyDictionary<string, int> TroopTierByFamily { get; set; } = new Dictionary<string, int>();
        public IReadOnlyDictionary<string, long> TroopPowerBonusBpByFamily { get; set; } = new Dictionary<string, long>();
        public long AvailablePower { get; set; }
        public long RequiredPower { get; set; }
        public long ReadinessBp { get; set; }
        public string StrategicPathId { get; set; }
        public IReadOnlyDictionary<string, long> StrategicPathPowerBonusBpByFamily { get; set; } = new Dictionary<string, long>();
        public bool DailyFocusApplied { get; set; }
        public bool WorldEventApplied { get; set; }
        public string WorldEventKey { get; set; } = string.Empty;
    }

    public sealed class CombatPatrolScreenModel
    {
        public CombatPatrolScreenState State { get; set; } = CombatPatrolScreenState.NotConfigured;
        public string ErrorCode { get; set; } = string.Empty;
        public int SelectedTier { get; set; } = 1;
        public int DraftGuardians { get; set; }
        public int DraftWingrunners { get; set; }
        public int DraftDarters { get; set; }
        public RemoteCombatPatrolPreview Preview { get; set; }
        public long Revision { get; set; }
        public IReadOnlyList<RemoteCombatPatrolActiveEncounter> ActiveEncounters { get; set; } = Array.Empty<RemoteCombatPatrolActiveEncounter>();
        public Guid? SelectedEncounterId { get; set; }
        public TimeSpan RemainingAtRead { get; set; }
        public IReadOnlyDictionary<string, long> AvailableRoster { get; set; } = DefaultRoster();
        public int Capacity { get; set; } = 1;
        public int TotalSlots { get; set; } = 1;
        public int ResourcePurchasedSlots { get; set; }
        public int PremiumPurchasedSlots { get; set; }
        public RemoteCombatPatrolSlotCost NextResourceSlotCost { get; set; }
        public IReadOnlyList<RemoteCombatPatrolRecoveringBatch> Recovering { get; set; } = Array.Empty<RemoteCombatPatrolRecoveringBatch>();
        public CombatPatrolDebrief Debrief { get; set; }
        // Cible du jour (demande de Jeff, 2026-07-31) : quel palier recoit un bonus de
        // recompense aujourd'hui - pure info d'affichage, la validation reste serveur.
        public int FeaturedTier { get; set; }
        // Evenement mondial dynamique (demande de Jeff, 2026-08-01) : meteo/menace active,
        // change plusieurs fois par jour - pure info d'affichage, la validation reste serveur.
        public RemoteActiveWorldEvent WorldEvent { get; set; }
        // Localisation (demande de Jeff, 2026-08-01) : parmi les paliers de la famille visee,
        // lequel est la region precise ciblee ce cycle - null si l'evenement actif n'est pas
        // une menace de combat.
        public int? WorldEventFeaturedTier { get; set; }

        public int UsedSlots => ActiveEncounters?.Count ?? 0;
        public bool HasFreeSlot => UsedSlots < TotalSlots;
        public int DraftTotal => DraftGuardians + DraftWingrunners + DraftDarters;

        public bool CanLaunch =>
            State != CombatPatrolScreenState.Mutating &&
            HasFreeSlot &&
            DraftTotal > 0 &&
            DraftTotal <= Capacity &&
            DraftGuardians <= AvailableRoster.GetValueOrDefault("guardians") &&
            DraftWingrunners <= AvailableRoster.GetValueOrDefault("wingrunners") &&
            DraftDarters <= AvailableRoster.GetValueOrDefault("darters") &&
            (Preview == null || Preview.CanLaunch);

        public RemoteCombatPatrolActiveEncounter SelectedEncounter =>
            SelectedEncounterId.HasValue ? ActiveEncounters?.FirstOrDefault(e => e.EncounterId == SelectedEncounterId.Value) : null;

        public bool CanClaim => SelectedEncounter != null && State == CombatPatrolScreenState.ClaimReady;
        public bool CanRecall => SelectedEncounter != null && (State == CombatPatrolScreenState.Active || State == CombatPatrolScreenState.ClaimReady);
        public bool CanPurchaseResourceSlot => ResourcePurchasedSlots < 2 && NextResourceSlotCost != null;
        public bool CanGrantPremiumSlot => PremiumPurchasedSlots < 2;

        private static IReadOnlyDictionary<string, long> DefaultRoster() => new Dictionary<string, long> { ["guardians"] = 0, ["wingrunners"] = 0, ["darters"] = 0 };
    }

    public interface ICombatPatrolPanelController
    {
        CombatPatrolScreenModel Model { get; }
        bool IsConfigured { get; }
        bool IsBusy { get; }
        void Refresh();
        void SelectTier(int tier);
        void AdjustDraft(string family, int delta);
        void SelectEncounter(Guid encounterId);
        void ClearSelection();
        void Launch();
        void Claim();
        void Recall();
        void PurchaseResourceSlot();
        void GrantPremiumSlot();
        void DismissDebrief();
    }

    public sealed class UnavailableCombatPatrolPanelController : ICombatPatrolPanelController
    {
        public CombatPatrolScreenModel Model { get; } = new CombatPatrolScreenModel();
        public bool IsConfigured => false;
        public bool IsBusy => false;
        public void Refresh() { }
        public void SelectTier(int tier) { }
        public void AdjustDraft(string family, int delta) { }
        public void SelectEncounter(Guid encounterId) { }
        public void ClearSelection() { }
        public void Launch() { }
        public void Claim() { }
        public void Recall() { }
        public void PurchaseResourceSlot() { }
        public void GrantPremiumSlot() { }
        public void DismissDebrief() { }
    }

    public sealed class CombatPatrolPanelController : ICombatPatrolPanelController, IDisposable
    {
        private readonly ICombatPatrolClient client;
        private readonly Guid hiveId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        private bool busy;

        public CombatPatrolPanelController(ICombatPatrolClient client, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (hiveId == Guid.Empty) throw new ArgumentException("A hive identifier is required.", nameof(hiveId));
            this.hiveId = hiveId;
            Model = new CombatPatrolScreenModel { State = CombatPatrolScreenState.Loading };
        }

        public CombatPatrolScreenModel Model { get; private set; }
        public bool IsConfigured => !disposed;
        public bool IsBusy => busy;

        public void Refresh() => Forget(RefreshCoreAsync());

        public void SelectTier(int tier)
        {
            if (busy || disposed || tier < 1 || tier > 7) return;
            Model.SelectedTier = tier;
            Forget(RefreshPreviewAsync());
        }

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
                default: return;
            }
            Forget(RefreshPreviewAsync());
        }

        public void SelectEncounter(Guid encounterId)
        {
            if (disposed) return;
            Model.SelectedEncounterId = encounterId;
            Model.Debrief = null;
            RemoteCombatPatrolActiveEncounter selected = Model.SelectedEncounter;
            if (selected != null) Model.State = Model.RemainingAtRead <= TimeSpan.Zero ? CombatPatrolScreenState.ClaimReady : CombatPatrolScreenState.Active;
        }

        public void ClearSelection()
        {
            if (disposed) return;
            Model.SelectedEncounterId = null;
            Model.State = Model.HasFreeSlot ? CombatPatrolScreenState.ReadyToLaunch : CombatPatrolScreenState.Blocked;
        }

        public void Launch() => Forget(LaunchCoreAsync());
        public void Claim() => Forget(ClaimCoreAsync());
        public void Recall() => Forget(RecallCoreAsync());
        public void PurchaseResourceSlot() => Forget(PurchaseResourceSlotCoreAsync());
        public void GrantPremiumSlot() => Forget(GrantPremiumSlotCoreAsync());

        public void DismissDebrief()
        {
            if (Model.State != CombatPatrolScreenState.Debrief) return;
            Model.Debrief = null;
            Model.SelectedEncounterId = null;
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
                RemoteCombatPatrolSnapshot snapshot = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                ApplySnapshot(snapshot);
                if (Model.Debrief == null && Model.SelectedEncounterId == null)
                    await RefreshPreviewCoreAsync();
            }
            catch (CombatPatrolClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task RefreshPreviewAsync()
        {
            if (busy || disposed) return;
            busy = true;
            try { await RefreshPreviewCoreAsync(); }
            catch (CombatPatrolClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task RefreshPreviewCoreAsync()
        {
            RemoteCombatPatrolPreview preview = await client.PreviewAsync(hiveId, Model.SelectedTier, Model.DraftGuardians, Model.DraftWingrunners, Model.DraftDarters, lifetime.Token);
            if (disposed) return;
            Model.Preview = preview;
            if (Model.Debrief == null && Model.SelectedEncounterId == null)
                Model.State = Model.HasFreeSlot ? CombatPatrolScreenState.ReadyToLaunch : CombatPatrolScreenState.Blocked;
        }

        private async Task LaunchCoreAsync()
        {
            if (busy || disposed || !Model.CanLaunch) return;
            busy = true;
            Model.State = CombatPatrolScreenState.Mutating;
            try
            {
                RemoteCombatPatrolMutationResponse response = await client.LaunchAsync(hiveId, Model.SelectedTier, Model.DraftGuardians, Model.DraftWingrunners, Model.DraftDarters, Model.Revision, NewKey("launch"), lifetime.Token);
                if (disposed) return;
                ApplySnapshot(response.Snapshot);
                Model.DraftGuardians = 0;
                Model.DraftWingrunners = 0;
                Model.DraftDarters = 0;
                if (Model.SelectedEncounterId == null) await RefreshPreviewCoreAsync();
            }
            catch (CombatPatrolClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task ClaimCoreAsync()
        {
            RemoteCombatPatrolActiveEncounter target = Model.SelectedEncounter;
            if (busy || disposed || target == null) return;
            busy = true;
            Model.State = CombatPatrolScreenState.Mutating;
            try
            {
                RemoteCombatPatrolMutationResponse response = await client.ClaimAsync(hiveId, target.EncounterId, Model.Revision, NewKey("claim"), lifetime.Token);
                if (disposed) return;
                if (response.ClaimReceipt != null)
                {
                    Model.Debrief = new CombatPatrolDebrief
                    {
                        EncounterId = response.ClaimReceipt.EncounterId,
                        Tier = response.ClaimReceipt.Tier,
                        Band = response.ClaimReceipt.Band,
                        PermanentLosses = response.ClaimReceipt.PermanentLosses,
                        WoundedLosses = response.ClaimReceipt.WoundedLosses,
                        CreditedByResource = response.ClaimReceipt.CreditedByResource,
                        ContributingChampionBeeIds = response.ClaimReceipt.ContributingChampionBeeIds ?? (IReadOnlyList<string>)Array.Empty<string>(),
                        ChampionPowerBonusBpByFamily = response.ClaimReceipt.ChampionPowerBonusBpByFamily ?? new Dictionary<string, long>(),
                        TroopTierByFamily = response.ClaimReceipt.TroopTierByFamily ?? new Dictionary<string, int>(),
                        TroopPowerBonusBpByFamily = response.ClaimReceipt.TroopPowerBonusBpByFamily ?? new Dictionary<string, long>(),
                        AvailablePower = response.ClaimReceipt.AvailablePower,
                        RequiredPower = response.ClaimReceipt.RequiredPower,
                        ReadinessBp = response.ClaimReceipt.ReadinessBp,
                        StrategicPathId = response.ClaimReceipt.StrategicPathId,
                        StrategicPathPowerBonusBpByFamily = response.ClaimReceipt.StrategicPathPowerBonusBpByFamily ?? new Dictionary<string, long>(),
                        DailyFocusApplied = response.ClaimReceipt.DailyFocusApplied,
                        WorldEventApplied = response.ClaimReceipt.WorldEventApplied,
                        WorldEventKey = response.ClaimReceipt.WorldEventKey ?? string.Empty
                    };
                }
                ApplySnapshot(response.Snapshot);
                if (Model.Debrief != null) Model.State = CombatPatrolScreenState.Debrief;
            }
            catch (CombatPatrolClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task RecallCoreAsync()
        {
            RemoteCombatPatrolActiveEncounter target = Model.SelectedEncounter;
            if (busy || disposed || target == null) return;
            busy = true;
            Model.State = CombatPatrolScreenState.Mutating;
            try
            {
                RemoteCombatPatrolMutationResponse response = await client.RecallAsync(hiveId, target.EncounterId, Model.Revision, NewKey("recall"), lifetime.Token);
                if (disposed) return;
                Model.SelectedEncounterId = null;
                ApplySnapshot(response.Snapshot);
                await RefreshPreviewCoreAsync();
            }
            catch (CombatPatrolClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task PurchaseResourceSlotCoreAsync()
        {
            if (busy || disposed || !Model.CanPurchaseResourceSlot) return;
            busy = true;
            Model.State = CombatPatrolScreenState.Mutating;
            try
            {
                RemoteCombatPatrolMutationResponse response = await client.PurchaseResourceSlotAsync(hiveId, Model.Revision, NewKey("purchase-slot"), lifetime.Token);
                if (disposed) return;
                ApplySnapshot(response.Snapshot);
                if (Model.SelectedEncounterId == null) await RefreshPreviewCoreAsync();
            }
            catch (CombatPatrolClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private async Task GrantPremiumSlotCoreAsync()
        {
            if (busy || disposed || !Model.CanGrantPremiumSlot) return;
            busy = true;
            Model.State = CombatPatrolScreenState.Mutating;
            try
            {
                RemoteCombatPatrolMutationResponse response = await client.GrantPremiumSlotAsync(hiveId, Model.Revision, NewKey("grant-slot"), lifetime.Token);
                if (disposed) return;
                ApplySnapshot(response.Snapshot);
                if (Model.SelectedEncounterId == null) await RefreshPreviewCoreAsync();
            }
            catch (CombatPatrolClientException error) { if (!disposed) SetError(StableError(error)); }
            catch (Exception) { if (!disposed) SetError("unexpected"); }
            finally { busy = false; }
        }

        private void ApplySnapshot(RemoteCombatPatrolSnapshot snapshot)
        {
            Model.Revision = snapshot.Revision;
            Model.ActiveEncounters = (IReadOnlyList<RemoteCombatPatrolActiveEncounter>)snapshot.ActiveEncounters ?? Array.Empty<RemoteCombatPatrolActiveEncounter>();
            Model.Recovering = (IReadOnlyList<RemoteCombatPatrolRecoveringBatch>)snapshot.Recovering ?? Array.Empty<RemoteCombatPatrolRecoveringBatch>();
            Model.AvailableRoster = snapshot.AvailableRoster ?? Model.AvailableRoster;
            if (snapshot.Capacity > 0) Model.Capacity = snapshot.Capacity;
            if (snapshot.TotalSlots > 0) Model.TotalSlots = snapshot.TotalSlots;
            Model.ResourcePurchasedSlots = snapshot.ResourcePurchasedSlots;
            Model.PremiumPurchasedSlots = snapshot.PremiumPurchasedSlots;
            Model.FeaturedTier = snapshot.FeaturedTier;
            Model.WorldEvent = snapshot.WorldEvent;
            Model.WorldEventFeaturedTier = snapshot.WorldEventFeaturedTier;
            Model.NextResourceSlotCost = snapshot.NextResourceSlotCost;

            if (Model.SelectedEncounterId.HasValue && Model.Debrief == null && Model.ActiveEncounters.All(e => e.EncounterId != Model.SelectedEncounterId.Value))
                Model.SelectedEncounterId = null;

            if (Model.Debrief == null)
            {
                RemoteCombatPatrolActiveEncounter selected = Model.SelectedEncounter;
                if (selected != null)
                {
                    TimeSpan remaining = selected.EndsAtUtc - snapshot.ServerTimeUtc;
                    Model.RemainingAtRead = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
                    Model.State = remaining <= TimeSpan.Zero ? CombatPatrolScreenState.ClaimReady : CombatPatrolScreenState.Active;
                }
                else
                {
                    Model.State = Model.HasFreeSlot ? CombatPatrolScreenState.ReadyToLaunch : CombatPatrolScreenState.Blocked;
                }
            }
        }

        private void SetError(string code)
        {
            Model.ErrorCode = code;
            Model.State = CombatPatrolScreenState.Error;
        }

        private static string NewKey(string operation) => "mobile-patrol-" + operation + "-" + Guid.NewGuid().ToString("N");

        private static string StableError(CombatPatrolClientException error)
        {
            switch (error.Message)
            {
                case "game.patrol_underpowered": return "patrol_underpowered";
                case "game.patrol_cooldown_active": return "patrol_cooldown_active";
                case "game.patrol_not_complete": return "patrol_not_complete";
                case "game.patrol_conflict": return "patrol_conflict";
                case "game.patrol_no_slot_available": return "patrol_no_slot_available";
                case "game.patrol_invalid_composition": return "patrol_invalid_composition";
                case "game.patrol_insufficient_troops": return "patrol_insufficient_troops";
                case "game.patrol_slot_limit_reached": return "patrol_slot_limit_reached";
                case "game.insufficient_resources": return "insufficient_resources";
                case "game.revision_conflict": return "revision_conflict";
                case "game.idempotency_conflict": return "idempotency_conflict";
                case "game.unavailable": return "server_unavailable";
            }
            switch (error.Error)
            {
                case CombatPatrolClientError.NotConfigured: return "not_configured";
                case CombatPatrolClientError.AuthenticationRequired: return "authentication_required";
                case CombatPatrolClientError.InvalidRequest: return "invalid_request";
                case CombatPatrolClientError.TransportFailure: return "network_unavailable";
                default: return "invalid_response";
            }
        }
    }
}
