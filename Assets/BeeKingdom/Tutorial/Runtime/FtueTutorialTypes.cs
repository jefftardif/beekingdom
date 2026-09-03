using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Tutorial
{
    public enum FtueInteractionMode
    {
        HighlightOnly = 0,
        RequiredTarget = 1
    }

    public enum FtueStepKind
    {
        Dialogue = 0,
        HighlightBuilding = 1,
        RequireBuildingTap = 2,
        RequireWindowOpened = 3,
        RequireUpgradeStarted = 4,
        HighlightUpgradeButton = 5,
        // M038-CL — PART2 (Research/Training/Army), same interaction shapes as Part1's upgrade step,
        // named per-feature only for readability; behavior mirrors HighlightUpgradeButton/RequireUpgradeStarted.
        HighlightActionButton = 6,
        RequireResearchStarted = 7,
        RequireTrainingStarted = 8,
        RequireArmyInteraction = 9,
        // M038B-CL — real collection step inserted before Training (see mission report §21,
        // FTUE economy blocker: passive production accrues into a claimable pool, never
        // auto-credited, so the FTUE must guide an explicit collect action).
        RequireProductionCollected = 10,
        // M040-CL: requested live by Jeff after Play Mode observed guard_post's upgrade sitting
        // unclaimed ("À valider") once its timer finished - RequireUpgradeStarted only asked the
        // player to START the upgrade, never to actually collect it. Mirrors
        // RequireProductionCollected's shape, gated on the real UpgradeCompleted event.
        RequireUpgradeCompleted = 11
    }

    public enum FtueEventKind
    {
        None = 0,
        DialogueContinue = 1,
        BuildingSelected = 2,
        WindowOpened = 3,
        UpgradeStarted = 4,
        UpgradeCompleted = 5,
        WorldMapOpened = 6,
        // M038-CL — PART2
        ResearchStarted = 7,
        TrainingStarted = 8,
        ArmyInteracted = 9,
        ProductionCollected = 10
    }

    [Serializable]
    public sealed class FtueStepDefinition
    {
        public string StepId;
        public FtueStepKind Kind;
        public FtueInteractionMode InteractionMode;
        public string TargetId; // e.g. "building.administration_core", "ui.button.upgrade"
        public string ChampionId; // e.g. "zephyra", "striga" — placeholder
        public string TextKey; // localization key or raw text for MVP
        public string NextStepId; // null means chapter complete
        public FtueEventKind CompletionEvent;
        public string CompletionEventParam; // buildingKey etc

        public FtueStepDefinition(string stepId, FtueStepKind kind, FtueInteractionMode mode, string targetId, string championId, string textKey, string nextStepId, FtueEventKind completionEvent, string param = null)
        {
            StepId = stepId;
            Kind = kind;
            InteractionMode = mode;
            TargetId = targetId;
            ChampionId = championId;
            TextKey = textKey;
            NextStepId = nextStepId;
            CompletionEvent = completionEvent;
            CompletionEventParam = param;
        }
    }

    [Serializable]
    public sealed class FtueChapterDefinition
    {
        public string ChapterId;
        public string EntryStepId;
        public List<FtueStepDefinition> Steps;

        public FtueChapterDefinition(string chapterId, string entryStepId, List<FtueStepDefinition> steps)
        {
            ChapterId = chapterId;
            EntryStepId = entryStepId;
            Steps = steps ?? new List<FtueStepDefinition>();
        }

        public FtueStepDefinition FindStep(string stepId)
        {
            if (string.IsNullOrEmpty(stepId)) return null;
            for (int i = 0; i < Steps.Count; i++) if (Steps[i].StepId == stepId) return Steps[i];
            return null;
        }
    }

    // Runtime state — persisted via Server TutorialProgress + local PlayerPrefs fallback
    [Serializable]
    public sealed class FtueProgress : ISerializationCallbackReceiver
    {
        public string ChapterId = string.Empty;
        public string CurrentStepId = string.Empty; // safe resume
        public string LastCompletedStepId = string.Empty;
        public List<string> CompletedStepsList = new List<string>();
        public List<string> CompletedChaptersList = new List<string>();
        public long Revision;
        public string UpdatedAtUtcString = string.Empty;

        [NonSerialized] public HashSet<string> CompletedSteps = new HashSet<string>(StringComparer.Ordinal);
        [NonSerialized] public HashSet<string> CompletedChapters = new HashSet<string>(StringComparer.Ordinal);
        [NonSerialized] public DateTimeOffset UpdatedAtUtc;

        public void OnBeforeSerialize()
        {
            CompletedStepsList = new List<string>(CompletedSteps);
            CompletedChaptersList = new List<string>(CompletedChapters);
            UpdatedAtUtcString = UpdatedAtUtc.ToString("O");
        }
        public void OnAfterDeserialize()
        {
            CompletedSteps = new HashSet<string>(CompletedStepsList ?? new List<string>(), StringComparer.Ordinal);
            CompletedChapters = new HashSet<string>(CompletedChaptersList ?? new List<string>(), StringComparer.Ordinal);
            DateTimeOffset.TryParse(UpdatedAtUtcString, out UpdatedAtUtc);
        }

        public bool IsChapterComplete(string chapterId) => CompletedChapters.Contains(chapterId);
        public bool IsStepCompleted(string stepId) => CompletedSteps.Contains(stepId);
    }

    public static class FtueTutorialRegistry
    {
        public const string ChapterFtueHiveIntroPart1 = "FTUE_HIVE_INTRO_PART1";
        // TargetIds — logical, not screen coords
        public const string TargetRoyalPalace = "building.administration_core"; // Palais Royal
        public const string TargetGuardPost = "building.guard_post"; // Caserne
        public const string TargetUpgradeButton = "ui.button.upgrade";
        public const string TargetWindowRoyalPalace = "window.royal_palace";
        public const string TargetWindowGuardPost = "window.guard_post";

        // M038-CL — FTUE_HIVE_CORE_PART2 (Research / Training / Army)
        public const string ChapterFtueHiveCorePart2 = "FTUE_HIVE_CORE_PART2";
        public const string TargetResearchNode = "building.research_node"; // Noeud de Recherche (le vrai batiment server-backed; Academy n'est pas server-backed)
        public const string TargetWindowResearchNode = "window.research_node";
        public const string TargetResearchStartButton = "ui.button.research_start";
        public const string TargetWindowTraining = "window.guard_post"; // reuse: Barrack IS the training window (same overlay as Part1's barrack_open step)
        public const string TargetTrainingStartButton = "ui.button.training_start";
        public const string TargetArmyMenu = "ui.menu.army";
        public const string TargetWindowArmy = "window.army";
        // M038B-CL — real collection target (Honey Reserve building). Legacy/hotspot key
        // confirmed via BuildingMappingTable: BuildingTypes.HoneyReserve -> BuildingLegacyKeys.HoneyStorage
        // ("honey_storage"), matching the HiveOfflineProduction catalog's BuildingKey exactly.
        public const string TargetHoneyReserve = "building.honey_storage";

        // Real, existing, no-prerequisite research chosen for the guided step — see M038 report
        // "First Research chosen" (180 honey + 120 pollen, fits the 1500/500/500 bootstrap).
        public const string FirstResearchId = "tempered_combs_i";
        // Real, existing, always-unlocked troop family with the lowest cost (500 honey + 120 pollen) —
        // see M038 report "First troop/training chosen".
        public const string FirstTrainingFamily = "darters";
    }
}
