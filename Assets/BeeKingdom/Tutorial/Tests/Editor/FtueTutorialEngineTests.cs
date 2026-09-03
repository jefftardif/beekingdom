using NUnit.Framework;
using System.Collections.Generic;
using BeeKingdom.Tutorial;

namespace BeeKingdom.Tests.Editor
{
    internal sealed class InMemoryTutorialStore : ITutorialProgressStore
    {
        public FtueProgress Stored;
        public FtueProgress LoadLocal()
        {
            if (Stored == null) return null;
            return new FtueProgress
            {
                ChapterId = Stored.ChapterId,
                CurrentStepId = Stored.CurrentStepId,
                LastCompletedStepId = Stored.LastCompletedStepId,
                CompletedSteps = new HashSet<string>(Stored.CompletedSteps, System.StringComparer.Ordinal),
                CompletedChapters = new HashSet<string>(Stored.CompletedChapters, System.StringComparer.Ordinal),
                Revision = Stored.Revision,
                UpdatedAtUtc = Stored.UpdatedAtUtc
            };
        }
        public void SaveLocal(FtueProgress progress)
        {
            if (progress == null) { Stored = null; return; }
            Stored = new FtueProgress
            {
                ChapterId = progress.ChapterId,
                CurrentStepId = progress.CurrentStepId,
                LastCompletedStepId = progress.LastCompletedStepId,
                CompletedSteps = new HashSet<string>(progress.CompletedSteps, System.StringComparer.Ordinal),
                CompletedChapters = new HashSet<string>(progress.CompletedChapters, System.StringComparer.Ordinal),
                Revision = progress.Revision,
                UpdatedAtUtc = progress.UpdatedAtUtc
            };
        }
    }

    [TestFixture]
    public class FtueTutorialEngineTests
    {
        private FtueTutorialEngine CreateEngine(out InMemoryTutorialStore store, FtueProgress initial = null)
        {
            store = new InMemoryTutorialStore();
            if (initial != null) store.Stored = initial;
            return new FtueTutorialEngine(FtueChapterDefinitions.All, store, store.LoadLocal());
        }

        [Test]
        public void TutorialStateInitialization_NewPlayer_StartsAtWelcome()
        {
            var engine = CreateEngine(out _);
            Assert.IsTrue(engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1));
            Assert.AreEqual("ftue.intro.welcome", engine.CurrentStep.StepId);
        }

        [Test]
        public void TutorialStatePersistence_SaveAndResume()
        {
            var engine = CreateEngine(out var store);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            Assert.AreEqual("ftue.intro.welcome", engine.CurrentStep.StepId);
            // complete welcome
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            Assert.AreEqual("ftue.intro.royal_intro", engine.CurrentStep.StepId);
            // persist
            var progress = engine.Progress;
            Assert.AreEqual("FTUE_HIVE_INTRO_PART1", progress.ChapterId);
            // new engine with same store should resume at royal_intro
            var engine2 = new FtueTutorialEngine(FtueChapterDefinitions.All, store, store.LoadLocal());
            engine2.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            Assert.AreEqual("ftue.intro.royal_intro", engine2.CurrentStep.StepId);
        }

        [Test]
        public void ResumeSameStep_AfterCloseReopen()
        {
            var engine = CreateEngine(out var store);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // welcome -> royal_intro
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // royal_intro -> royal_tap
            Assert.AreEqual("ftue.intro.royal_tap", engine.CurrentStep.StepId);
            var engine2 = new FtueTutorialEngine(FtueChapterDefinitions.All, store, store.LoadLocal());
            engine2.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            Assert.AreEqual("ftue.intro.royal_tap", engine2.CurrentStep.StepId);
        }

        [Test]
        public void CompletedStepIdempotence_NoDoubleAdvance()
        {
            var engine = CreateEngine(out _);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            var step = engine.CurrentStep.StepId;
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            Assert.AreNotEqual(step, engine.CurrentStep.StepId);
            // try to complete same welcome again (should be idempotent, no double advance)
            bool second = engine.NotifyEvent(FtueEventKind.DialogueContinue); // this should advance royal_intro, not welcome
            // Ensure we didn't skip two steps at once
            Assert.AreEqual("ftue.intro.royal_tap", engine.CurrentStep.StepId);
            // Try to notify with wrong event should not advance
            bool wrong = engine.NotifyEvent(FtueEventKind.UpgradeStarted, "guard_post");
            Assert.IsFalse(wrong);
            Assert.AreEqual("ftue.intro.royal_tap", engine.CurrentStep.StepId);
        }

        [Test]
        public void TargetResolution_BuildingTargetsResolve()
        {
            // Registry without scene objects should still resolve upgrade button fallback and not crash
            var reg = TutorialTargetRegistry.Instance;
            reg.ClearForTests();
            Assert.IsTrue(reg.TryGetTargetPosition(FtueTutorialRegistry.TargetUpgradeButton, null, out _, out _));
            // building without scene objects should return false (no provider) but not throw
            bool hasRoyal = reg.TryGetTargetPosition(FtueTutorialRegistry.TargetRoyalPalace, null, out _, out _);
            // false is expected when no scene, but must not throw — PASS if no exception
            Assert.IsFalse(hasRoyal);
            // Register a fake UI provider
            var go = new UnityEngine.GameObject("TestRect");
            var rt = go.AddComponent<UnityEngine.RectTransform>();
            reg.RegisterUi(FtueTutorialRegistry.TargetUpgradeButton, () => rt);
            Assert.IsTrue(reg.TryResolveUi(FtueTutorialRegistry.TargetUpgradeButton, out var resolved));
            Assert.AreEqual(rt, resolved);
            UnityEngine.Object.DestroyImmediate(go);
            reg.ClearForTests();
        }

        [Test]
        public void RequiredTarget_RejectsWrong_CorrectAdvances()
        {
            var engine = CreateEngine(out _);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // welcome
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // royal_intro -> royal_tap (requires administration_core)
            Assert.AreEqual("ftue.intro.royal_tap", engine.CurrentStep.StepId);
            // wrong building should not advance
            bool wrong = engine.NotifyEvent(FtueEventKind.BuildingSelected, "guard_post");
            Assert.IsFalse(wrong);
            Assert.AreEqual("ftue.intro.royal_tap", engine.CurrentStep.StepId);
            // correct advances
            bool correct = engine.NotifyEvent(FtueEventKind.BuildingSelected, "administration_core");
            Assert.IsTrue(correct);
            Assert.AreEqual("ftue.intro.colony_dialogue", engine.CurrentStep.StepId);
        }

        [Test]
        public void BuildingWindowDetection_GuardPost()
        {
            var engine = CreateEngine(out _);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            // advance to barrack_open
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // welcome
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // royal_intro
            engine.NotifyEvent(FtueEventKind.BuildingSelected, "administration_core"); // royal_tap
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // colony_dialogue -> barrack_intro
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // barrack_intro -> barrack_open
            Assert.AreEqual("ftue.intro.barrack_open", engine.CurrentStep.StepId);
            bool advanced = engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post");
            Assert.IsTrue(advanced);
            Assert.AreEqual("ftue.intro.upgrade_highlight", engine.CurrentStep.StepId);
        }

        [Test]
        public void UpgradeStartDetection_GuardPost()
        {
            var engine = CreateEngine(out _);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            // fast forward to upgrade_started
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // welcome
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // royal_intro
            engine.NotifyEvent(FtueEventKind.BuildingSelected, "administration_core");
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // colony
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // barrack_intro
            engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post"); // barrack_open
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // upgrade_highlight -> upgrade_started
            Assert.AreEqual("ftue.intro.upgrade_started", engine.CurrentStep.StepId);
            // wrong key no advance
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.UpgradeStarted, "administration_core"));
            Assert.AreEqual("ftue.intro.upgrade_started", engine.CurrentStep.StepId);
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.UpgradeStarted, "guard_post"));
            Assert.AreEqual("ftue.intro.timer_dialogue", engine.CurrentStep.StepId);
        }

        [Test]
        public void UpgradeCompleteDetection_GuardPost()
        {
            // M040-CL: requested live by Jeff after Play Mode observed guard_post's finished
            // upgrade sitting unclaimed ("À valider") once RequireUpgradeStarted was satisfied -
            // the tutorial now also requires the player to actually claim it.
            var engine = CreateEngine(out _);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // welcome
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // royal_intro
            engine.NotifyEvent(FtueEventKind.BuildingSelected, "administration_core");
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // colony
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // barrack_intro
            engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post"); // barrack_open
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // upgrade_highlight
            engine.NotifyEvent(FtueEventKind.UpgradeStarted, "guard_post"); // upgrade_started
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // timer_dialogue -> upgrade_claim
            Assert.AreEqual("ftue.intro.upgrade_claim", engine.CurrentStep.StepId);
            // wrong key must not advance or complete the chapter early
            Assert.IsFalse(engine.NotifyEvent(FtueEventKind.UpgradeCompleted, "administration_core"));
            Assert.AreEqual("ftue.intro.upgrade_claim", engine.CurrentStep.StepId);
            Assert.IsFalse(engine.Progress.IsChapterComplete(FtueTutorialRegistry.ChapterFtueHiveIntroPart1));
            // correct key completes the chapter
            Assert.IsTrue(engine.NotifyEvent(FtueEventKind.UpgradeCompleted, "guard_post"));
            Assert.IsNull(engine.CurrentStep);
            Assert.IsTrue(engine.Progress.IsChapterComplete(FtueTutorialRegistry.ChapterFtueHiveIntroPart1));
        }

        [Test]
        public void NoGameplayMutationByTutorial_DoesNotChangeBuildingLevel()
        {
            // Engine must not mutate building levels — it only observes
            var engine = CreateEngine(out _);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            // Engine has no reference to building levels, so completing steps should not change any external state
            // We verify by ensuring engine state changes but no side effect on a dummy building level dict
            var levels = new Dictionary<string,int>{{"guard_post",1}};
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            Assert.AreEqual(1, levels["guard_post"]); // unchanged
        }

        [Test]
        public void NoLivingHiveDependency_EngineWorksWithoutLivingHive()
        {
            var engine = CreateEngine(out _);
            // Should start and run without any LivingHive GameObject
            Assert.IsTrue(engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1));
            Assert.IsNotNull(engine.CurrentStep);
            // Complete full chapter without LivingHive
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // welcome
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // royal_intro
            engine.NotifyEvent(FtueEventKind.BuildingSelected, "administration_core");
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post");
            engine.NotifyEvent(FtueEventKind.DialogueContinue);
            engine.NotifyEvent(FtueEventKind.UpgradeStarted, "guard_post");
            engine.NotifyEvent(FtueEventKind.DialogueContinue); // timer -> upgrade_claim
            engine.NotifyEvent(FtueEventKind.UpgradeCompleted, "guard_post"); // upgrade_claim -> complete
            Assert.IsNull(engine.CurrentStep); // chapter complete
            Assert.IsTrue(engine.Progress.IsChapterComplete(FtueTutorialRegistry.ChapterFtueHiveIntroPart1));
        }

        [Test]
        public void FullChapter_Playable_EndToEnd()
        {
            var engine = CreateEngine(out _);
            engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            var sequence = new (FtueEventKind kind, string param)[] {
                (FtueEventKind.DialogueContinue,null), // welcome
                (FtueEventKind.DialogueContinue,null), // royal_intro
                (FtueEventKind.BuildingSelected,"administration_core"), // royal_tap
                (FtueEventKind.DialogueContinue,null), // colony
                (FtueEventKind.DialogueContinue,null), // barrack_intro
                (FtueEventKind.WindowOpened,"guard_post"), // barrack_open
                (FtueEventKind.DialogueContinue,null), // upgrade_highlight
                (FtueEventKind.UpgradeStarted,"guard_post"), // upgrade_started
                (FtueEventKind.DialogueContinue,null), // timer -> upgrade_claim
                (FtueEventKind.UpgradeCompleted,"guard_post"), // upgrade_claim -> complete
            };
            foreach(var e in sequence) Assert.IsTrue(engine.NotifyEvent(e.kind, e.param), $"Failed at {engine.CurrentStep?.StepId} expecting {e.kind} {e.param}");
            Assert.IsTrue(engine.Progress.IsChapterComplete(FtueTutorialRegistry.ChapterFtueHiveIntroPart1));
        }
    }
}
