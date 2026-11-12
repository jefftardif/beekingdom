using System;

namespace BeeKingdom.Playground
{
    public static partial class HiveViewProductUiPresenter
    {
        public static string PremiumRoyalPalaceAuthorityForExternalHost()
        {
            return BeeKingdom.Localization.BeeLocalization.Text(OfficialBuildingUpgradeConfigured()
                ? "building_upgrade.disclosure" : "splash.message.local_preview");
        }

        public static string PremiumRoyalPalaceStatusForExternalHost()
        {
            if (OfficialBuildingUpgradeConfigured() || !RoyalPalaceUpgradeActionEnabledForExternalHost())
                return RoyalPalaceUpgradeStatusForExternalHost();
            UnityEngine.Vector2 cost = UpgradeCost(FindReferenceHotspot("administration_core"));
            return BeeKingdom.Localization.BeeLocalization.Text("ui.building.upgrade_action") + " ("
                + UnityEngine.Mathf.RoundToInt(cost.x).ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " " + BeeKingdom.Localization.BeeLocalization.Text("resource.honey") + ", "
                + UnityEngine.Mathf.RoundToInt(cost.y).ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " " + BeeKingdom.Localization.BeeLocalization.Text("resource.wax") + ")";
        }

        // Palace-only presentation access. Existing commands and server authority stay intact.
        public static int? PremiumRoyalPalaceLevelForExternalHost()
        {
            if (!OfficialBuildingUpgradeConfigured()) return RoyalPalaceLevelForExternalHost();
            int level = OfficialBuildingUpgradeModel()?.LevelFor("administration_core") ?? 0;
            return level > 0 ? level : (int?)null;
        }

        public static float PremiumRoyalPalaceProgressForExternalHost()
        {
            if (!OfficialBuildingUpgradeConfigured()) return RoyalPalaceUpgradeProgressForExternalHost();
            HiveBuildingUpgradeScreenModel model = OfficialBuildingUpgradeModel();
            if (model?.ActiveOperation == null || buildingUpgradeController == null
                || !string.Equals(model.ActiveOperation.BuildingKey, "administration_core", StringComparison.Ordinal))
                return 0f;
            return (float)model.Progress01(buildingUpgradeController.Elapsed);
        }
    }
}
