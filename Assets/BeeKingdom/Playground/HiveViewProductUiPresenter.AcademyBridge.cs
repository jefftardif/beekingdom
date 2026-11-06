using System;

namespace BeeKingdom.Playground
{
    public static partial class HiveViewProductUiPresenter
    {
        // Academy displays the authenticated Construction / Royal Palace state.
        // Advanced courses, certificates and training modifiers have no
        // Academy-specific server authority yet; never mutate them locally.
        private const string AcademyBuildingKey = "academy_canopy";

        public static HiveBuildingUpgradeScreenModel OfficialAcademyUpgradeModelForExternalHost()
        {
            return OfficialBuildingUpgradeConfigured()
                ? OfficialBuildingUpgradeModel() : null;
        }

        public static void RefreshOfficialAcademyBuildingForExternalHost()
        {
            // Construction sidebar is not visible when Academy is fullscreen.
            // Use the existing account-scoped controller, never a new request/client.
            if (!OfficialBuildingUpgradeConfigured() ||
                buildingUpgradeController == null ||
                buildingUpgradeController.IsBusy) return;
            buildingUpgradeController.Refresh();
        }

        public static int? OfficialAcademyBuildingLevelForExternalHost()
        {
            HiveBuildingUpgradeScreenModel model =
                OfficialAcademyUpgradeModelForExternalHost();
            if (model == null ||
                model.State == HiveBuildingUpgradeScreenState.NotConfigured)
                return null;
            int level = model.LevelFor(AcademyBuildingKey);
            return level > 0 ? level : (int?)null;
        }

        public static HiveBuildingUpgradeOfferModel OfficialAcademyUpgradeOfferForExternalHost()
        {
            HiveBuildingUpgradeScreenModel model =
                OfficialAcademyUpgradeModelForExternalHost();
            return model == null ||
                model.State == HiveBuildingUpgradeScreenState.NotConfigured
                ? null : model.OfferFor(AcademyBuildingKey);
        }

        public static HiveBuildingUpgradeOperationModel OfficialAcademyUpgradeOperationForExternalHost()
        {
            HiveBuildingUpgradeOperationModel operation =
                OfficialAcademyUpgradeModelForExternalHost()?.ActiveOperation;
            return operation != null &&
                string.Equals(operation.BuildingKey, AcademyBuildingKey,
                    StringComparison.Ordinal)
                ? operation : null;
        }

        public static TimeSpan? OfficialAcademyUpgradeRemainingForExternalHost()
        {
            HiveBuildingUpgradeScreenModel model = OfficialAcademyUpgradeModelForExternalHost();
            if (model == null || buildingUpgradeController == null ||
                OfficialAcademyUpgradeOperationForExternalHost() == null) return null;
            return model.Remaining(buildingUpgradeController.Elapsed);
        }

        public static HiveRoyalPalaceProgressionModel OfficialAcademyRoyalPalaceForExternalHost()
        {
            return OfficialAcademyUpgradeModelForExternalHost()?.RoyalPalace;
        }
    }
}
