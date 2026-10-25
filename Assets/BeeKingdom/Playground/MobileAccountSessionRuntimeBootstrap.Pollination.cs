using System;
using BeeKingdom.Networking;

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

        private static void ClosePollinationForSignedOutSession()
        {
            PollinationPanelController previous = pollinationPanelController;
            pollinationPanelController = null;
            previous?.Dispose();
        }
    }
}
