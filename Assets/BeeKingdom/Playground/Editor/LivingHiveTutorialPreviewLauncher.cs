using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class LivingHiveTutorialPreviewLauncher
    {
        public const string ChapterFour = "chapter_4";
        public const string ChapterFourTestBatchChoice = "chapter_4_test_batch_choice";
        public const string ChapterFourStructuralChecks = "chapter_4_structural_checks";
        public const string ChapterFourSupplyChoice = "chapter_4_supply_choice";
        public const string ChapterFourDoctrineChoice = "chapter_4_doctrine_choice";

        private const string LivingHiveScenePath = "Assets/Scenes/LivingHive.unity";
        private const string PendingPreviewKey = "BeeKingdom.LivingHive.PendingTutorialPreview";
        private const string EarliestFrameKey = "BeeKingdom.LivingHive.PendingTutorialPreviewEarliestFrame";

        static LivingHiveTutorialPreviewLauncher()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.update -= ApplyPendingPreviewWhenReady;
            EditorApplication.update += ApplyPendingPreviewWhenReady;
        }

        public static void Request(string previewId)
        {
            if (!IsSupported(previewId))
            {
                throw new ArgumentOutOfRangeException(nameof(previewId), previewId, "Unknown LivingHive tutorial preview.");
            }

            SessionState.SetString(PendingPreviewKey, previewId);
            SessionState.SetInt(EarliestFrameKey, -1);

            if (EditorApplication.isPlaying)
            {
                ScheduleAfterBootstrap();
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!string.Equals(SceneManager.GetActiveScene().path, LivingHiveScenePath, StringComparison.Ordinal))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    ClearPendingPreview();
                    return;
                }

                EditorSceneManager.OpenScene(LivingHiveScenePath, OpenSceneMode.Single);
            }

            PlaygroundPlayModeStartScene.UseLivingHiveOnPlay();
            EditorApplication.EnterPlaymode();
        }

        public static string PendingPreviewForProof()
        {
            return SessionState.GetString(PendingPreviewKey, string.Empty);
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ScheduleAfterBootstrap();
            }
            else if (state == PlayModeStateChange.EnteredEditMode
                && !string.IsNullOrWhiteSpace(SessionState.GetString(PendingPreviewKey, string.Empty)))
            {
                ClearPendingPreview();
            }
        }

        private static void ScheduleAfterBootstrap()
        {
            if (string.IsNullOrWhiteSpace(SessionState.GetString(PendingPreviewKey, string.Empty))) return;
            SessionState.SetInt(EarliestFrameKey, Time.frameCount + 3);
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static void ApplyPendingPreviewWhenReady()
        {
            string previewId = SessionState.GetString(PendingPreviewKey, string.Empty);
            if (string.IsNullOrWhiteSpace(previewId) || !EditorApplication.isPlaying) return;
            if (!string.Equals(SceneManager.GetActiveScene().path, LivingHiveScenePath, StringComparison.Ordinal)) return;

            int earliestFrame = SessionState.GetInt(EarliestFrameKey, -1);
            if (earliestFrame < 0)
            {
                ScheduleAfterBootstrap();
                return;
            }

            if (Time.frameCount < earliestFrame)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            ClearPendingPreview();
            ApplyPreview(previewId);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            Debug.Log("LivingHive tutorial preview active: " + previewId + ".");
        }

        private static void ApplyPreview(string previewId)
        {
            HiveViewProductUiPresenter.EnsureSceneObjects();
            switch (previewId)
            {
                case ChapterFour:
                    HiveViewProductUiPresenter.BeginGuidedUpgradeTutorialForProof();
                    return;
                case ChapterFourTestBatchChoice:
                    BeginChapterFourTestBatchChoice();
                    return;
                case ChapterFourStructuralChecks:
                    BeginChapterFourStructuralChecks();
                    return;
                case ChapterFourSupplyChoice:
                    HiveViewProductUiPresenter.BeginGuidedUpgradeOperationsForProof();
                    HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
                    return;
                case ChapterFourDoctrineChoice:
                    BeginChapterFourDoctrineChoice();
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(previewId), previewId, "Unknown LivingHive tutorial preview.");
            }
        }

        private static void BeginChapterFourTestBatchChoice()
        {
            HiveViewProductUiPresenter.BeginGuidedUpgradeTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("wax_workshop");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedUpgradeAuditForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedUpgradePlanForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedUpgradeForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedUpgradeCalibrationForProof();
            HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
        }

        private static void BeginChapterFourStructuralChecks()
        {
            HiveViewProductUiPresenter.BeginGuidedUpgradeOperationsForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsSupplyForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsSupplyForProof();
            HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop");
            HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsDeploymentForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsDeploymentForProof();
        }

        private static void BeginChapterFourDoctrineChoice()
        {
            HiveViewProductUiPresenter.BeginGuidedUpgradeOperationsForProof();
            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
                HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsSupplyForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsSupplyForProof();
                HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop");
                HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsDeploymentForProof(round == 1);
                HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsDeploymentForProof();
                HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(0);
                HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(1);
                HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(2);
            }

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
        }

        private static bool IsSupported(string previewId)
        {
            return previewId == ChapterFour
                || previewId == ChapterFourTestBatchChoice
                || previewId == ChapterFourStructuralChecks
                || previewId == ChapterFourSupplyChoice
                || previewId == ChapterFourDoctrineChoice;
        }

        private static void ClearPendingPreview()
        {
            SessionState.EraseString(PendingPreviewKey);
            SessionState.EraseInt(EarliestFrameKey);
        }
    }
}
