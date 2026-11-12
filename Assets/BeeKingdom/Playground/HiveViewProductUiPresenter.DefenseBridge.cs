using System;
using BeeKingdom.Localization;

namespace BeeKingdom.Playground
{
    public static partial class HiveViewProductUiPresenter
    {
        // Read-only access to the already configured, official building
        // upgrade snapshot. Never infer defense gameplay from building level.
        public static int? OfficialDefenseBuildingLevelForExternalHost()
        {
            HiveBuildingUpgradeScreenModel model = OfficialBuildingUpgradeModel();
            if (model == null ||
                model.State == HiveBuildingUpgradeScreenState.NotConfigured ||
                model.State == HiveBuildingUpgradeScreenState.Loading ||
                model.State == HiveBuildingUpgradeScreenState.Error)
                return null;
            int level = model.LevelFor("defense_growth");
            return level > 0 ? level : (int?)null;
        }

        public static bool DefenseUpgradeInProgressForExternalHost()
        {
            HiveBuildingUpgradeScreenModel model = OfficialBuildingUpgradeModel();
            return model?.ActiveOperation != null &&
                string.Equals(model.ActiveOperation.BuildingKey,
                    "defense_growth", StringComparison.Ordinal);
        }
    }
}