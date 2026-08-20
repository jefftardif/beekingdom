using System;

namespace BeeKingdom.Core.Integration
{
    // Cross-assembly bridge for the LivingHive Settings window, same constraint and pattern
    // as LivingHiveChatBridge.cs (see that file for the full explanation): the real
    // implementation (HiveViewProductUiPresenter's DrawMobileComfortSettingsPanel, in
    // Unity's default Assembly-CSharp) can't be referenced directly from
    // BeeKingdom.LivingHiveMenu. The Settings bootstrap (default assembly) is the only
    // writer, installing both handlers once at startup; everyone else only reads/toggles.
    public static class LivingHiveSettingsBridge
    {
        private static Func<bool> isOpenQuery;
        private static Action toggleHandler;

        public static bool IsOpen => isOpenQuery != null && isOpenQuery();

        public static void SetHandlers(Func<bool> isOpenQuery, Action toggleHandler)
        {
            LivingHiveSettingsBridge.isOpenQuery = isOpenQuery;
            LivingHiveSettingsBridge.toggleHandler = toggleHandler;
        }

        public static void ToggleOverlay()
        {
            toggleHandler?.Invoke();
        }
    }
}
