using System;

namespace BeeKingdom.Tutorial
{
    public static class TutorialGameplayNotifier
    {
        public static event Action<string> BuildingSelected;
        public static event Action<string> WindowOpened;
        public static event Action<string> UpgradeStarted;
        public static event Action<string> UpgradeCompleted;
        public static event Action<string> ResearchStarted;
        public static event Action<string> TrainingStarted;
        public static event Action<string> ArmyInteracted;
        public static event Action<string> ProductionCollected;

        public static void NotifyBuildingSelected(string buildingKey) => BuildingSelected?.Invoke(buildingKey);
        public static void NotifyWindowOpened(string buildingKey) => WindowOpened?.Invoke(buildingKey);
        public static void NotifyUpgradeStarted(string buildingKey) => UpgradeStarted?.Invoke(buildingKey);
        // M040-CL: fired only after the real server Complete/claim call succeeds (see
        // RunOfficialBuildingUpgradeAction's Complete branch in HiveViewProductUiPresenter.cs) -
        // requested live by Jeff: starting an upgrade isn't the whole task, the FTUE should guide
        // the player through actually claiming it too.
        public static void NotifyUpgradeCompleted(string buildingKey) => UpgradeCompleted?.Invoke(buildingKey);
        // M038-CL: fired only after the REAL server call for research/training succeeds
        // (see call sites in HiveResearchPresentation.cs / HiveOfficialDoctrineRecruitmentPresentation.cs) -
        // the tutorial never triggers these itself, it only observes them.
        public static void NotifyResearchStarted(string researchId) => ResearchStarted?.Invoke(researchId);
        public static void NotifyTrainingStarted(string family) => TrainingStarted?.Invoke(family);
        // Army's squad-confirm mutation is disabled server-side today (CombatSquadReservation:Enabled=false
        // in Production) - this fires on a real but purely local UI action (adjusting a squad stepper) so the
        // tutorial step doesn't depend on a feature flag it doesn't control. See M038 report section "Army".
        public static void NotifyArmyInteracted(string family) => ArmyInteracted?.Invoke(family);
        // M038B-CL: real, server-confirmed production collection (see HiveOfflineProductionPresentation.cs's
        // CollectCoreAsync, fired after `snapshot = response.Snapshot;` — never the optimistic tap itself).
        public static void NotifyProductionCollected(string buildingKey) => ProductionCollected?.Invoke(buildingKey);

        public static void ClearForTests()
        {
            BuildingSelected = null;
            WindowOpened = null;
            UpgradeStarted = null;
            UpgradeCompleted = null;
            ResearchStarted = null;
            TrainingStarted = null;
            ArmyInteracted = null;
            ProductionCollected = null;
        }
    }
}
