using NUnit.Framework;
using BeeKingdom.Tutorial;

namespace BeeKingdom.Tests.Editor
{
    // M038-CL — FTUE_HIVE_CORE_PART2 (Research/Training/Army), same engine as M037's
    // FtueTutorialEngineTests.cs (InMemoryTutorialStore reused from that file, same namespace/assembly).
    [TestFixture]
    public class FtueHiveCorePart2Tests
    {
        private FtueTutorialEngine CreateEngine(out InMemoryTutorialStore store, FtueProgress initial = null)
        {
            store = new InMemoryTutorialStore();
            if (initial != null) store.Stored = initial;
            return new FtueTutorialEngine(FtueChapterDefinitions.All, store, store.LoadLocal());
        }

        private static void CompletePart1(FtueTutorialEngine engine)
        {
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // welcome
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // royal_intro
            engine.NotifyEvent(FtueEventKind.BuildingSelected, "administration_core"); // royal_tap
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // colony
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // barrack_intro
            engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post"); // barrack_open
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // upgrade_highlight
            engine.NotifyEvent(FtueEventKind.UpgradeStarted, "guard_post"); // upgrade_started
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // timer -> upgrade_claim
            engine.NotifyEvent(FtueEventKind.UpgradeCompleted, "guard_post"); // upgrade_claim -> complete
        }

        [Test]
        public void Part2Initialization_StartsAtWelcome()
        {
            var engine = CreateEngine(out _);
            Assert.IsTrue(engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2));
            Assert.AreEqual("ftue.core2.welcome", engine.CurrentStep.StepId);
        }

        [Test]
        public void Part1ToPart2Transition_StartsPart2AfterPart1Completes()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            Assert.IsTrue(engine.Progress.IsChapterComplete(FtueTutorialRegistry.ChapterFtueHiveIntroPart1));
            Assert.IsTrue(engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2));
            Assert.AreEqual("ftue.core2.welcome", engine.CurrentStep.StepId);
        }

        [Test]
        public void ResearchTargetResolution_FallbackButtonsResolveWithoutScene()
        {
            var reg = TutorialTargetRegistry.Instance;
            reg.ClearForTests();
            Assert.IsTrue(reg.TryGetTargetPosition(FtueTutorialRegistry.TargetResearchStartButton, null, out _, out _));
            bool hasResearchBuilding = reg.TryGetTargetPosition(FtueTutorialRegistry.TargetResearchNode, null, out _, out _);
            Assert.IsFalse(hasResearchBuilding); // no scene objects registered — must not throw
            reg.ClearForTests();
        }

        [Test]
        public void WrongResearchTargetRejected_CorrectAdvances()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // welcome -> research_intro
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // research_intro -> research_open
            Assert.AreEqual("ftue.core2.research_open", engine.CurrentStep.StepId);
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post")); // wrong window
            Assert.AreEqual("ftue.core2.research_open", engine.CurrentStep.StepId);
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.WindowOpened, "research_node")); // correct
            Assert.AreEqual("ftue.core2.research_select_highlight", engine.CurrentStep.StepId);
        }

        [Test]
        public void ResearchWindowDetection_AdvancesToSelectHighlight()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2);
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.WindowOpened, "research_node"));
            Assert.AreEqual("ftue.core2.research_select_highlight", engine.CurrentStep.StepId);
        }

        [Test]
        public void RealResearchStartedAdvances_WrongIdRejected()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2);
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            engine.NotifyEvent(FtueEventKind.WindowOpened, "research_node");
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // research_select_highlight -> research_started
            Assert.AreEqual("ftue.core2.research_started", engine.CurrentStep.StepId);
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.ResearchStarted, "foraging_routes_i")); // wrong research
            Assert.AreEqual("ftue.core2.research_started", engine.CurrentStep.StepId);
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.ResearchStarted, FtueTutorialRegistry.FirstResearchId));
            Assert.AreEqual("ftue.core2.research_timer_dialogue", engine.CurrentStep.StepId);
        }

        [Test]
        public void TutorialDoesNotStartResearchItself_OnlyObservesRealEvent()
        {
            // The engine has no gameplay/network reference at all — it can only ever advance in
            // response to an explicit NotifyEvent call from a REAL call site (see
            // HiveResearchPresentation.cs's StartCoreAsync, which fires TutorialGameplayNotifier
            // only after `snapshot = response.Snapshot;`, i.e. after the server accepted the mutation).
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2);
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            engine.NotifyEvent(FtueEventKind.WindowOpened, "research_node");
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            Assert.AreEqual("ftue.core2.research_started", engine.CurrentStep.StepId);
            // Nothing except an explicit ResearchStarted event can move this step forward.
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.DialogueContinue));
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.WindowOpened, "research_node"));
            Assert.AreEqual("ftue.core2.research_started", engine.CurrentStep.StepId);
        }

        private static void CompleteResearch(FtueTutorialEngine engine)
        {
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // welcome
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // research_intro
            engine.NotifyEvent(FtueEventKind.WindowOpened, "research_node"); // research_open
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // research_select_highlight
            engine.NotifyEvent(FtueEventKind.ResearchStarted, FtueTutorialRegistry.FirstResearchId); // research_started
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // research_timer_dialogue -> collect_intro
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // collect_intro -> collect_started
            engine.NotifyEvent(FtueEventKind.ProductionCollected, "honey_storage"); // collect_started -> training_intro
        }

        [Test]
        public void RealProductionCollectedAdvances_WrongBuildingRejected()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // welcome
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // research_intro
            engine.NotifyEvent(FtueEventKind.WindowOpened, "research_node"); // research_open
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // research_select_highlight
            engine.NotifyEvent(FtueEventKind.ResearchStarted, FtueTutorialRegistry.FirstResearchId); // research_started
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // research_timer_dialogue -> collect_intro
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // collect_intro -> collect_started
            Assert.AreEqual("ftue.core2.collect_started", engine.CurrentStep.StepId);
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.ProductionCollected, "wax_workshop")); // wrong building
            Assert.AreEqual("ftue.core2.collect_started", engine.CurrentStep.StepId);
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.ProductionCollected, "honey_storage"));
            Assert.AreEqual("ftue.core2.training_intro", engine.CurrentStep.StepId);
        }

        [Test]
        public void TrainingTargetResolution_FallbackButtonResolvesWithoutScene()
        {
            var reg = TutorialTargetRegistry.Instance;
            reg.ClearForTests();
            Assert.IsTrue(reg.TryGetTargetPosition(FtueTutorialRegistry.TargetTrainingStartButton, null, out _, out _));
            reg.ClearForTests();
        }

        [Test]
        public void WrongTrainingTargetRejected_RealTrainingStartedAdvances()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            CompleteResearch(engine);
            Assert.AreEqual("ftue.core2.training_intro", engine.CurrentStep.StepId);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // training_intro -> training_open
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.WindowOpened, "research_node")); // wrong window
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post"));
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // training_select_highlight -> training_started
            Assert.AreEqual("ftue.core2.training_started", engine.CurrentStep.StepId);
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.TrainingStarted, "guardians")); // wrong family
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.TrainingStarted, FtueTutorialRegistry.FirstTrainingFamily));
            Assert.AreEqual("ftue.core2.training_timer_dialogue", engine.CurrentStep.StepId);
        }

        [Test]
        public void TutorialDoesNotStartTrainingItself_OnlyObservesRealEvent()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            CompleteResearch(engine);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // training_intro -> training_open
            engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post"); // training_select_highlight
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // training_started
            Assert.AreEqual("ftue.core2.training_started", engine.CurrentStep.StepId);
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.DialogueContinue));
            Assert.AreEqual("ftue.core2.training_started", engine.CurrentStep.StepId);
        }

        private static void CompleteTraining(FtueTutorialEngine engine)
        {
            CompleteResearch(engine);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // training_intro -> training_open
            engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post"); // training_select_highlight
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // training_started
            engine.NotifyEvent(FtueEventKind.TrainingStarted, FtueTutorialRegistry.FirstTrainingFamily); // training_timer_dialogue
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // -> army_intro
        }

        [Test]
        public void ArmyTargetResolution_FallbackMenuResolvesWithoutScene()
        {
            var reg = TutorialTargetRegistry.Instance;
            reg.ClearForTests();
            Assert.IsTrue(reg.TryGetTargetPosition(FtueTutorialRegistry.TargetArmyMenu, null, out _, out _));
            reg.ClearForTests();
        }

        [Test]
        public void ArmyWindowDetection_AdvancesToInteract()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            CompleteTraining(engine);
            Assert.AreEqual("ftue.core2.army_intro", engine.CurrentStep.StepId);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // army_intro -> army_open
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post"));
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.WindowOpened, "army"));
            Assert.AreEqual("ftue.core2.army_interact", engine.CurrentStep.StepId);
        }

        [Test]
        public void ArmyInteractionAdvances_AnyFamilyAccepted()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            CompleteTraining(engine);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // army_open
            engine.NotifyEvent(FtueEventKind.WindowOpened, "army"); // army_interact
            Assert.AreEqual("ftue.core2.army_interact", engine.CurrentStep.StepId);
            // No CompletionEventParam set on this step — any real family adjustment must count.
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.ArmyInteracted, "wingrunners"));
            Assert.AreEqual("ftue.core2.farewell", engine.CurrentStep.StepId);
        }

        [Test]
        public void Part2Persistence_ResumeCorrectStep()
        {
            var engine = CreateEngine(out var store);
            CompletePart1(engine);
            CompleteTraining(engine);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // army_intro -> army_open
            Assert.AreEqual("ftue.core2.army_open", engine.CurrentStep.StepId);

            var engine2 = new FtueTutorialEngine(FtueChapterDefinitions.All, store, store.LoadLocal());
            engine2.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2);
            Assert.AreEqual("ftue.core2.army_open", engine2.CurrentStep.StepId);
            // No re-completion of research/training on resume
            Assert.IsFalse(engine2.NotifyEvent(FtueEventKind.ResearchStarted, FtueTutorialRegistry.FirstResearchId));
            Assert.AreEqual("ftue.core2.army_open", engine2.CurrentStep.StepId);
        }

        [Test]
        public void Part2CompletedStepsIdempotent_NoDoubleAdvanceNoDoubleReward()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            CompleteResearch(engine);
            string step = engine.CurrentStep.StepId; // training_intro
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            Assert.AreNotEqual(step, engine.CurrentStep.StepId);
            // Re-notify the ALREADY-completed research event: must not re-advance or re-fire anything.
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.ResearchStarted, FtueTutorialRegistry.FirstResearchId));
        }

        [Test]
        public void NoLivingHiveDependency_Part2WorksWithoutLivingHive()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            CompleteTraining(engine);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // army_open
            engine.NotifyEvent(FtueEventKind.WindowOpened, "army"); // army_interact
            engine.NotifyEvent(FtueEventKind.ArmyInteracted, "darters"); // farewell
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.DialogueContinue)); // complete
            Assert.IsNull(engine.CurrentStep);
            Assert.IsTrue(engine.Progress.IsChapterComplete(FtueTutorialRegistry.ChapterFtueHiveCorePart2));
        }

        [Test]
        public void FullPart2Chapter_Playable_EndToEnd()
        {
            var engine = CreateEngine(out _);
            CompletePart1(engine);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2);
            var sequence = new (FtueEventKind kind, string param)[]
            {
                (FtueEventKind.DialogueContinue, null),                                       // welcome
                (FtueEventKind.DialogueContinue, null),                                       // research_intro
                (FtueEventKind.WindowOpened, "research_node"),                                // research_open
                (FtueEventKind.DialogueContinue, null),                                       // research_select_highlight
                (FtueEventKind.ResearchStarted, FtueTutorialRegistry.FirstResearchId),        // research_started
                (FtueEventKind.DialogueContinue, null),                                       // research_timer_dialogue
                (FtueEventKind.DialogueContinue, null),                                       // collect_intro
                (FtueEventKind.ProductionCollected, "honey_storage"),                         // collect_started
                (FtueEventKind.DialogueContinue, null),                                       // training_intro
                (FtueEventKind.WindowOpened, "guard_post"),                                   // training_open
                (FtueEventKind.DialogueContinue, null),                                       // training_select_highlight
                (FtueEventKind.TrainingStarted, FtueTutorialRegistry.FirstTrainingFamily),    // training_started
                (FtueEventKind.DialogueContinue, null),                                       // training_timer_dialogue
                (FtueEventKind.DialogueContinue, null),                                       // army_intro
                (FtueEventKind.WindowOpened, "army"),                                         // army_open
                (FtueEventKind.ArmyInteracted, "guardians"),                                  // army_interact
                (FtueEventKind.DialogueContinue, null),                                       // farewell -> complete
            };
            foreach (var e in sequence)
                Assert.IsTrue(engine.NotifyEvent(e.kind, e.param), $"Failed at {engine.CurrentStep?.StepId} expecting {e.kind} {e.param}");
            Assert.IsTrue(engine.Progress.IsChapterComplete(FtueTutorialRegistry.ChapterFtueHiveCorePart2));
        }
    }
}
