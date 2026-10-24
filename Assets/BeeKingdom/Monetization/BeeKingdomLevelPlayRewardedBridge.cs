using System;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Services.LevelPlay;
#endif

namespace BeeKingdom.Monetization
{
    /// <summary>
    /// LevelPlay rewarded ad transport for Android. Never credits game rewards locally.
    /// The game server is the sole authority after signed S2S verification.
    /// This component does not start automatically: authenticated session + privacy
    /// decisions + server quota must be checked by the caller.
    /// </summary>
    public sealed class BeeKingdomLevelPlayRewardedBridge : MonoBehaviour
    {
        public const string LevelPlayAppKey = "28f6cc45d";
        // Copied from the LevelPlay BK_DailyQuest_3Ads dashboard screenshot.
        public const string RewardedAdUnitId = "na3phetjxrlsnnax";
        public const string AdMobAndroidAppId = "ca-app-pub-6355269720386478~5764103111";
        public const int DailyRewardCap = 3;

        private static BeeKingdomLevelPlayRewardedBridge instance;
        private string authenticatedPlayerId;
        private bool privacyResolved;
        private bool initializationStarted;
        private bool initializationSucceeded;
        private bool displayInProgress;

#if UNITY_ANDROID && !UNITY_EDITOR
        private LevelPlayRewardedAd rewardedAd;
#endif

        public static BeeKingdomLevelPlayRewardedBridge GetOrCreate()
        {
            if (instance != null) return instance;
            var gameObject = new GameObject("BeeKingdom.LevelPlayRewarded");
            DontDestroyOnLoad(gameObject);
            return gameObject.AddComponent<BeeKingdomLevelPlayRewardedBridge>();
        }

        public event Action<bool> AvailabilityChanged;
        // This is only a notification. The server must confirm and credit the reward.
        public event Action RewardPendingServerConfirmation;
        public event Action<string> Diagnostic;

        public bool IsAvailable
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return initializationSucceeded && privacyResolved &&
                    !string.IsNullOrEmpty(authenticatedPlayerId) &&
                    !displayInProgress && rewardedAd != null && rewardedAd.IsAdReady();
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Called only after authentication and completion of the applicable
        /// privacy/consent flow. No SDK initialization in the Unity Editor.
        /// </summary>
        public bool InitializeForPlayer(Guid playerId, bool privacyChoicesResolved)
        {
            if (playerId == Guid.Empty || !privacyChoicesResolved)
            {
                Diagnostic?.Invoke("ads_auth_or_privacy_pending");
                return false;
            }

            authenticatedPlayerId = playerId.ToString("D");
            privacyResolved = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (initializationSucceeded)
            {
                EnsureRewardedAd();
                LevelPlay.SetDynamicUserId(authenticatedPlayerId);
                AvailabilityChanged?.Invoke(IsAvailable);
                return true;
            }

            if (initializationStarted) return true;
            initializationStarted = true;
            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(LevelPlayAppKey);
            return true;
#else
            Diagnostic?.Invoke("ads_android_device_only");
            return false;
#endif
        }

        /// <summary>Disable all reward entry points as soon as the player signs out.</summary>
        public void ClearAuthenticatedPlayer()
        {
            authenticatedPlayerId = null;
            privacyResolved = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            CancelInvoke(nameof(RetryLoad));
#endif
            AvailabilityChanged?.Invoke(false);
        }

        /// <summary>
        /// Only call after an authenticated server read confirms the daily quota.
        /// Never award honey, pollen or items in the client callback.
        /// </summary>
        public bool TryShowWhenServerAllows(bool serverQuotaAvailable)
        {
            if (!serverQuotaAvailable || !IsAvailable) return false;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!LevelPlay.SetDynamicUserId(authenticatedPlayerId))
            {
                Diagnostic?.Invoke("ads_s2s_user_id_failed");
                return false;
            }
            displayInProgress = true;
            AvailabilityChanged?.Invoke(false);
            rewardedAd.ShowAd();
            return true;
#else
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void OnInitSuccess(LevelPlayConfiguration config)
        {
            initializationSucceeded = true;
            if (!privacyResolved || string.IsNullOrEmpty(authenticatedPlayerId)) return;
            EnsureRewardedAd();
            LevelPlay.SetDynamicUserId(authenticatedPlayerId);
        }

        private void EnsureRewardedAd()
        {
            if (rewardedAd != null) return;
            rewardedAd = new LevelPlayRewardedAd(RewardedAdUnitId);
            rewardedAd.OnAdLoaded += OnAdLoaded;
            rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
            rewardedAd.OnAdDisplayed += OnAdDisplayed;
            rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
            rewardedAd.OnAdRewarded += OnAdRewarded;
            rewardedAd.OnAdClosed += OnAdClosed;
            rewardedAd.LoadAd();
        }

        private void OnInitFailed(LevelPlayInitError error)
        {
            initializationStarted = false;
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            Diagnostic?.Invoke("ads_sdk_initialization_failed");
        }

        private void OnAdLoaded(LevelPlayAdInfo info)
        {
            AvailabilityChanged?.Invoke(IsAvailable);
        }

        private void OnAdLoadFailed(LevelPlayAdError error)
        {
            AvailabilityChanged?.Invoke(false);
            Diagnostic?.Invoke("ads_inventory_unavailable");
            if (privacyResolved && !string.IsNullOrEmpty(authenticatedPlayerId))
                Invoke(nameof(RetryLoad), 30f);
        }

        private void OnAdDisplayed(LevelPlayAdInfo info)
        {
            AvailabilityChanged?.Invoke(false);
        }

        private void OnAdDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            displayInProgress = false;
            Diagnostic?.Invoke("ads_display_failed");
            RetryLoad();
        }

        private void OnAdRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            // No local reward. An authenticated UI can now refresh the server
            // state; the actual grant comes exclusively from a signed S2S callback.
            RewardPendingServerConfirmation?.Invoke();
        }

        private void OnAdClosed(LevelPlayAdInfo info)
        {
            displayInProgress = false;
            RetryLoad();
        }

        private void RetryLoad()
        {
            CancelInvoke(nameof(RetryLoad));
            if (initializationSucceeded && privacyResolved &&
                !string.IsNullOrEmpty(authenticatedPlayerId) && rewardedAd != null)
                rewardedAd.LoadAd();
        }
#endif

        private void OnDestroy()
        {
            if (instance == this) instance = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            CancelInvoke();
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            if (rewardedAd != null)
            {
                rewardedAd.OnAdLoaded -= OnAdLoaded;
                rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
                rewardedAd.OnAdDisplayed -= OnAdDisplayed;
                rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
                rewardedAd.OnAdRewarded -= OnAdRewarded;
                rewardedAd.OnAdClosed -= OnAdClosed;
            }
#endif
        }
    }
}
