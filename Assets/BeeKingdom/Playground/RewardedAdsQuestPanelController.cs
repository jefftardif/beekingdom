using System;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Monetization;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// Read-only model + user-initiated display. No client credit endpoint exists.
    /// </summary>
    public sealed class RewardedAdsQuestPanelController : IDisposable
    {
        private readonly RewardedAdsQuestClient client;
        private readonly Guid hiveId;
        private readonly Guid playerId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool busy;
        private bool disposed;
        private bool pendingServerConfirmation;
        private DateTimeOffset pendingSinceUtc;

        public RewardedAdsQuestPanelController(
            RewardedAdsQuestClient client, Guid playerId, Guid hiveId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            if (playerId == Guid.Empty || hiveId == Guid.Empty)
                throw new ArgumentException("An authenticated player and hive are required.");
            this.playerId = playerId;
            this.hiveId = hiveId;
        }

        public RewardedAdsQuestSnapshot Model { get; private set; }
        public bool IsBusy => busy;
        public bool IsConfigured => !disposed;
        public bool PendingServerConfirmation => pendingServerConfirmation;
        public string StatusKey { get; private set; } = string.Empty;
        public bool IsAdReady => !disposed &&
            Model != null && Model.Enabled &&
            Model.CompletedCount < Model.MaximumCount &&
            BeeKingdomLevelPlayRewardedBridge.GetOrCreate().IsAvailable;

        public void Refresh()
        {
            if (disposed || busy) return;
            _ = RefreshCoreAsync();
        }

        private async Task RefreshCoreAsync()
        {
            if (disposed || busy) return;
            busy = true;
            try
            {
                RewardedAdsQuestSnapshot next = await client.ReadAsync(hiveId, lifetime.Token);
                if (disposed) return;
                bool serverConfirmed = !pendingServerConfirmation ||
                    (Model != null && (next.DayUtc != Model.DayUtc ||
                        next.CompletedCount > Model.CompletedCount));
                if (next.PlayerId != playerId) throw new InvalidOperationException("ads.player_mismatch");
                if (Model == null || Model.DayUtc != next.DayUtc ||
                    Model.CompletedCount != next.CompletedCount ||
                    Model.Enabled != next.Enabled ||
                    Model.LifetimeHoneyCredited != next.LifetimeHoneyCredited ||
                    Model.LifetimePollenCredited != next.LifetimePollenCredited)
                    Model = next;
                if (serverConfirmed ||
                    DateTimeOffset.UtcNow - pendingSinceUtc > TimeSpan.FromMinutes(2))
                    pendingServerConfirmation = false;
                StatusKey = !next.Enabled ? "not_enabled" :
                    pendingServerConfirmation ? "pending_server" : "ready";
            }
            catch (OperationCanceledException) { }
            catch
            {
                if (!disposed) StatusKey = "temporarily_unavailable";
            }
            finally { busy = false; }
        }

        // Called only after the app's privacy/CMP process has completed.
        // We intentionally do NOT infer GDPR consent from clicking "Watch".
        public bool BeginOrWatch(bool privacyChoicesResolved)
        {
            if (disposed || Model == null || !Model.Enabled ||
                Model.CompletedCount >= Model.MaximumCount ||
                busy || !privacyChoicesResolved)
                return false;

            BeeKingdomLevelPlayRewardedBridge bridge =
                BeeKingdomLevelPlayRewardedBridge.GetOrCreate();
            bridge.RewardPendingServerConfirmation -= OnRewardPending;
            bridge.RewardPendingServerConfirmation += OnRewardPending;

            if (!bridge.IsAvailable)
            {
                bridge.InitializeForPlayer(playerId, privacyChoicesResolved);
                StatusKey = "loading_ad";
                return false;
            }

            // Model comes from an authenticated server read; a pending callback
            // never directly awards resources on the tablet.
            bool shown = bridge.TryShowWhenServerAllows(
                Model.CompletedCount < Model.MaximumCount);
            if (shown) StatusKey = "watching_ad";
            return shown;
        }

        private void OnRewardPending()
        {
            if (disposed || pendingServerConfirmation) return;
            pendingServerConfirmation = true;
            pendingSinceUtc = DateTimeOffset.UtcNow;
            StatusKey = "pending_server";
            _ = ConfirmAfterServerCallbackAsync();
        }

        // LevelPlay's signed S2S callback may arrive after the Android SDK
        // reward event. Poll server truth without triggering panel redraw flicker.
        private async Task ConfirmAfterServerCallbackAsync()
        {
            try
            {
                for (int attempt = 0; attempt < 20 &&
                    pendingServerConfirmation && !disposed; attempt++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt == 0 ? 1 : 3),
                        lifetime.Token);
                    if (disposed || !pendingServerConfirmation) return;
                    await RefreshCoreAsync();
                }
            }
            catch (OperationCanceledException) { }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
            if (BeeKingdomLevelPlayRewardedBridge.TryGetExisting(out var bridge))
            {
                bridge.RewardPendingServerConfirmation -= OnRewardPending;
                bridge.ClearAuthenticatedPlayer();
            }
        }
    }
}
