using System;

namespace BeeKingdom.Core.Integration
{
    // Cross-assembly bridge for authenticated/official Research routing. BeeKingdom.LivingHiveMenu
    // owns the existing fullscreen Research window (LivingHiveResearchWindow/LivingHiveResearchHost,
    // local-preview data, M011 modal-safe) while the official server-backed Research overlay
    // (HiveViewProductUiPresenter.*ResearchOverlayForExternalHost, backed by
    // MobileAccountSessionRuntimeBootstrap's HiveResearchPanelController) lives in the default
    // Playground assembly. LivingHiveMenu cannot reference Assembly-CSharp/Playground directly
    // (Unity does not allow an .asmdef assembly to depend on the implicit default assembly), so
    // this bridge - mirroring the existing LivingHiveActivitiesBridge/LivingHiveSettingsBridge
    // pattern - is the only legal path between the two.
    public static class LivingHiveResearchBridge
    {
        private static Func<bool> isOfficialOpenQuery;
        private static Func<bool> isOfficialAvailableQuery;
        private static Action openOfficialHandler;

        // True while the official (server-backed) Research overlay is open.
        public static bool IsOfficialOpen => isOfficialOpenQuery != null && isOfficialOpenQuery();

        // True when an authenticated session has a configured, server-backed Research controller
        // - i.e. official data is actually available, not just that the player is logged in.
        public static bool IsOfficialAvailable => isOfficialAvailableQuery != null && isOfficialAvailableQuery();

        public static void SetHandlers(Func<bool> isOfficialOpenQuery, Func<bool> isOfficialAvailableQuery, Action openOfficialHandler)
        {
            LivingHiveResearchBridge.isOfficialOpenQuery = isOfficialOpenQuery;
            LivingHiveResearchBridge.isOfficialAvailableQuery = isOfficialAvailableQuery;
            LivingHiveResearchBridge.openOfficialHandler = openOfficialHandler;
        }

        public static void OpenOfficialOverlay()
        {
            openOfficialHandler?.Invoke();
        }
    }
}
