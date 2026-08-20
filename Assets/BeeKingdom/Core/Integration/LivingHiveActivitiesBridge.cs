using System;

namespace BeeKingdom.Core.Integration
{
    // Cross-assembly bridge for the HiveMap Activities fullscreen. The uGUI bottom menu
    // lives in BeeKingdom.LivingHiveMenu, while the real Daily Round / Milestone Event
    // controllers live in the default Playground assembly. This keeps the menu package
    // free of gameplay/controller references and mirrors the existing Chat/Settings bridge
    // pattern.
    public static class LivingHiveActivitiesBridge
    {
        private static Func<bool> isOpenQuery;
        private static Action openHandler;

        public static bool IsOpen => isOpenQuery != null && isOpenQuery();

        public static void SetHandlers(Func<bool> isOpenQuery, Action openHandler)
        {
            LivingHiveActivitiesBridge.isOpenQuery = isOpenQuery;
            LivingHiveActivitiesBridge.openHandler = openHandler;
        }

        public static void OpenOverlay()
        {
            openHandler?.Invoke();
        }
    }
}
