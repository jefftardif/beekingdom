using System;
using BeeKingdom.Networking;
#if UNITY_EDITOR
using System.IO;
using UnityEngine;
#endif

namespace BeeKingdom.Playground
{
    /// <summary>
    /// P5 additive session binding. Opt-in is a serialized setting in the
    /// official runtime configuration, OFF by default. The world map's
    /// pollination panel uses this same authenticated transport and is fully
    /// torn down on logout/account switch.
    /// </summary>
    public static partial class MobileAccountSessionRuntimeBootstrap
    {
        private static PollinationPanelController pollinationPanelController;

        private static readonly IPollinationPanelController
            UnavailablePollinationController = new UnavailablePollinationPanelController();

        public static IPollinationPanelController PollinationControllerForWorldMap =>
            pollinationPanelController != null && pollinationPanelController.IsConfigured
                ? (IPollinationPanelController)pollinationPanelController
                : UnavailablePollinationController;

        public static bool PollinationFeatureEnabledForWorldMap =>
            activeConfiguration != null &&
            activeConfiguration.PollinationFeatureEnabled &&
            pollinationPanelController != null &&
            pollinationPanelController.IsConfigured &&
            gameplayPlayerId != Guid.Empty &&
            gameplayHiveId != Guid.Empty;

        private static void ConfigurePollinationForActiveSession(
            MobileAccountSessionGate gate,
            IGameAccountSessionSource sessionSource,
            IAuthenticatedGameRestTransport transport,
            Guid hiveId,
            MobileAccountSessionRuntimeConfiguration configuration,
            bool authenticatedAuthority)
        {
            ClosePollinationForSignedOutSession();
            if (configuration == null || !configuration.PollinationFeatureEnabled ||
                !configuration.OfficialGameplayEnabled || !authenticatedAuthority ||
                hiveId == Guid.Empty)
                return;
            var api = new PollinationClient(gate, sessionSource, transport, enabled: true);
            pollinationPanelController = new PollinationPanelController(api, hiveId);
        }

#if UNITY_EDITOR
        // Explicit local Editor-only opt-in. Never touches the player-build
        // configuration asset or permits demo identities to call the API.
        private static MobileAccountSessionRuntimeConfiguration
            ResolveEditorPollinationReadOnlyConfig(
                MobileAccountSessionRuntimeConfiguration configuration)
        {
            if (configuration == null) return null;
            string path = Path.Combine(
                Path.GetDirectoryName(Application.dataPath), "Temp",
                "BKPollinationReadOnlyPreview.enabled");
            if (!File.Exists(path)) return configuration;
            Uri uri;
            if (!configuration.OfficialAccountsEnabled ||
                !configuration.OfficialGameplayEnabled ||
                !Uri.TryCreate(configuration.BaseUrl, UriKind.Absolute, out uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(uri.Host, "api-ops.beekingdomgame.com",
                    StringComparison.OrdinalIgnoreCase) ||
                !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo))
            {
                Debug.LogWarning("[BK-Pollination] Editor preview needs the official HTTPS API.");
                return configuration;
            }
            var clone = UnityEngine.Object.Instantiate(configuration);
            clone.hideFlags = HideFlags.DontSave;
            JsonUtility.FromJsonOverwrite(
                "{\"pollinationFeatureEnabled\":true}", clone);
            Debug.Log("[BK-Pollination] Editor-only floral catalogue preview selected.");
            return clone;
        }
#endif

        private static void ClosePollinationForSignedOutSession()
        {
            PollinationPanelController previous = pollinationPanelController;
            pollinationPanelController = null;
            previous?.Dispose();
        }
    }
}
