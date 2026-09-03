using System;
using UnityEngine;

namespace BeeKingdom.Core.Integration
{
    public static class LivingHiveArmyBridge
    {
        private static Func<bool> isOpenQuery;
        private static Action openHandler;
        // M038C-CL: same cross-assembly bridge pattern as isOpenQuery/openHandler above -
        // LivingHiveMenuCanvas.cs (BeeKingdom.LivingHiveMenu assembly) publishes the real
        // RectTransform of its "Armée" row here; BeeKingdom.Playground/BeeKingdom.Tutorial
        // (which CAN see each other, unlike LivingHiveMenu) consume it as the FTUE fallback
        // target for whenever the player is looking at the "Plus" submenu instead of the
        // Caserne's own Army button.
        private static Func<RectTransform> armyRowRectQuery;

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

        public static void SetArmyRowRectQuery(Func<RectTransform> query)
        {
            armyRowRectQuery = query;
        }

        public static RectTransform GetArmyRowRect()
        {
            return armyRowRectQuery != null ? armyRowRectQuery() : null;
        }
    }
}
