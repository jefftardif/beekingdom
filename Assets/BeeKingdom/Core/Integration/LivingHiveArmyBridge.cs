using System;

namespace BeeKingdom.Core.Integration
{
    public static class LivingHiveArmyBridge
    {
        private static Func<bool> isOpenQuery;
        private static Action openHandler;

        public static bool IsOpen => isOpenQuery != null && isOpenQuery();

        public static void SetHandlers(Func<bool> isOpenQuery, Action openHandler)
        {
            LivingHiveArmyBridge.isOpenQuery = isOpenQuery;
            LivingHiveArmyBridge.openHandler = openHandler;
        }

        public static void OpenOverlay()
        {
            openHandler?.Invoke();
        }
    }
}
