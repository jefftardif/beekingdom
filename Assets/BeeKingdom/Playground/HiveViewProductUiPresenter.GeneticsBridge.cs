using System;

namespace BeeKingdom.Playground
{
    public static partial class HiveViewProductUiPresenter
    {
        // The Genetics building uses the same authenticated Construction snapshot.
        // No hereditary trait, mutation, experiment or resource mutation is simulated.
        private const string GeneticsBuildingKey = "genetics_garden";

        public static HiveBuildingUpgradeScreenModel OfficialGeneticsUpgradeModelForExternalHost()
        {
            return OfficialBuildingUpgradeConfigured()
                ? OfficialBuildingUpgradeModel() : null;
        }

        public static void RefreshOfficialGeneticsBuildingForExternalHost()
        {
            // Make Genetics independently refreshable even when the Construction
            // sidebar is hidden behind the fullscreen Activities-style window.
            if (!OfficialBuildingUpgradeConfigured() ||
                buildingUpgradeController == null ||
                buildingUpgradeController.IsBusy) return;
            buildingUpgradeController.Refresh();
        }

        // Loading/Error preserve the last signed-in server snapshot when present.
        // Do not wipe the display on every 20-second refresh.
        public static int? OfficialGeneticsBuildingLevelForExternalHost()
        {
            HiveBuildingUpgradeScreenModel model = OfficialGeneticsUpgradeModelForExternalHost();
            if (model == null ||
                model.State == HiveBuildingUpgradeScreenState.NotConfigured)
                return null;
            int level = model.LevelFor(GeneticsBuildingKey);
            return level > 0 ? level : (int?)null;
        }

        public static HiveBuildingUpgradeOfferModel OfficialGeneticsUpgradeOfferForExternalHost()
        {
            HiveBuildingUpgradeScreenModel model = OfficialGeneticsUpgradeModelForExternalHost();
            if (model == null ||
                model.State == HiveBuildingUpgradeScreenState.NotConfigured) return null;
            return model.OfferFor(GeneticsBuildingKey);
        }

        public static HiveBuildingUpgradeOperationModel OfficialGeneticsUpgradeOperationForExternalHost()
        {
            HiveBuildingUpgradeOperationModel operation =
                OfficialGeneticsUpgradeModelForExternalHost()?.ActiveOperation;
            return operation != null &&
                string.Equals(operation.BuildingKey, GeneticsBuildingKey,
                    StringComparison.Ordinal)
                ? operation : null;
        }

        public static TimeSpan? OfficialGeneticsUpgradeRemainingForExternalHost()
        {
            HiveBuildingUpgradeScreenModel model = OfficialGeneticsUpgradeModelForExternalHost();
            if (model == null || buildingUpgradeController == null ||
                OfficialGeneticsUpgradeOperationForExternalHost() == null) return null;
            return model.Remaining(buildingUpgradeController.Elapsed);
        }

        public static bool GeneticsUpgradeInProgressForExternalHost()
        {
            return OfficialGeneticsUpgradeOperationForExternalHost() != null;
        }
    }
}
