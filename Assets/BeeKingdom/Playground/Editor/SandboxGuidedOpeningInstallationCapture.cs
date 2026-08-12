using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxGuidedOpeningInstallationCapture
    {
        private const string ScenePath = "Assets/Scenes/LivingHive.unity";
        private const string OutputDirectory = "Artifacts/GuidedOpeningInstallation";
        private const string ManifestPath = OutputDirectory + "/GuidedOpeningInstallationManifest.md";
        private const string TriggerPath = "Temp/GuidedOpeningInstallationCapture.request";
        private const string StateRequested = "BeeKingdom.Playground.GuidedOpeningInstallation.Requested";
        private const string StateStartAfterExit = "BeeKingdom.Playground.GuidedOpeningInstallation.StartAfterExit";
        private const string StateFrames = "BeeKingdom.Playground.GuidedOpeningInstallation.Frames";
        private const string StateIndex = "BeeKingdom.Playground.GuidedOpeningInstallation.Index";
        private const string StateCaptured = "BeeKingdom.Playground.GuidedOpeningInstallation.Captured";
        private const string StateConfiguredIndex = "BeeKingdom.Playground.GuidedOpeningInstallation.ConfiguredIndex";
        private const string StateExitWhenFinished = "BeeKingdom.Playground.GuidedOpeningInstallation.ExitWhenFinished";
        private static double captureReadyAt;
        private static double screenshotRequestedAt;

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly int State;
            public readonly string Locale;

            public CaptureSpec(string label, string fileName, int width, int height, int state, string locale = "fr-CA")
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                State = state;
                Locale = locale;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Chapter 1 production choice on phone (en-US)", "Chapter1_ProductionChoice_en-US_390x844.png", 390, 844, 26, "en-US"),
            new CaptureSpec("Chapter 1 production choice in landscape (en-US)", "Chapter1_ProductionChoice_en-US_1600x900.png", 1600, 900, 26, "en-US"),
            new CaptureSpec("Qualification du lot témoin sur téléphone", "Chapter4_BatchQualification_390x844.png", 390, 844, 25),
            new CaptureSpec("Qualification du lot témoin en paysage", "Chapter4_BatchQualification_1600x900.png", 1600, 900, 25),
            new CaptureSpec("Dotation fondatrice sur téléphone", "Chapter1_FoundationReward_390x844.png", 390, 844, 24),
            new CaptureSpec("Dotation fondatrice en paysage", "Chapter1_FoundationReward_1600x900.png", 1600, 900, 24),
            new CaptureSpec("Orientation de l'ouvrière sur téléphone", "Chapter3_WorkerOrientation_390x844.png", 390, 844, 23),
            new CaptureSpec("Orientation de l'ouvrière en paysage", "Chapter3_WorkerOrientation_1600x900.png", 1600, 900, 23),
            new CaptureSpec("Débrief tactique sur téléphone", "Chapter5_TacticalDebrief_390x844.png", 390, 844, 22),
            new CaptureSpec("Débrief tactique en paysage", "Chapter5_TacticalDebrief_1600x900.png", 1600, 900, 22),
            new CaptureSpec("Ratification de charte sur téléphone", "Chapter1_CharterRatification_390x844.png", 390, 844, 21),
            new CaptureSpec("Ratification de charte en paysage", "Chapter1_CharterRatification_1600x900.png", 1600, 900, 21),
            new CaptureSpec("Validation de doctrine sur téléphone", "Chapter4_OperationsDoctrineChecks_390x844.png", 390, 844, 20),
            new CaptureSpec("Validation de doctrine en paysage", "Chapter4_OperationsDoctrineChecks_1600x900.png", 1600, 900, 20),
            new CaptureSpec("Briefing de sortie sur telephone", "Chapter5_WorldBriefingChoice_390x844.png", 390, 844, 18),
            new CaptureSpec("Simulation de sortie sur telephone", "Chapter5_WorldBriefingSimulation_390x844.png", 390, 844, 19),
            new CaptureSpec("Briefing de sortie en paysage", "Chapter5_WorldBriefingChoice_1600x900.png", 1600, 900, 18),
            new CaptureSpec("Simulation de sortie en paysage", "Chapter5_WorldBriefingSimulation_1600x900.png", 1600, 900, 19),
            new CaptureSpec("Mise en service sur téléphone", "Chapter1_Commissioning_390x844.png", 390, 844, 0),
            new CaptureSpec("Charte sur téléphone", "Chapter1_Charter_390x844.png", 390, 844, 1),
            new CaptureSpec("Contrôle sous charge sur téléphone", "Chapter1_LoadValidation_390x844.png", 390, 844, 2),
            new CaptureSpec("Sceau durable sur téléphone", "Chapter1_CommissioningSeal_390x844.png", 390, 844, 3),
            new CaptureSpec("Certification atelier sur téléphone", "Chapter4_CertificationChoice_390x844.png", 390, 844, 4),
            new CaptureSpec("Contrôles atelier sur téléphone", "Chapter4_CertificationChecks_390x844.png", 390, 844, 5),
            new CaptureSpec("Relais ouvrière sur téléphone", "Chapter3_WorkshopHandoffChoice_390x844.png", 390, 844, 6),
            new CaptureSpec("Contrôles de liaison sur téléphone", "Chapter3_WorkshopHandoffChecks_390x844.png", 390, 844, 7),
            new CaptureSpec("Mandat d'expédition sur téléphone", "Chapter5_ExpeditionMandateChoice_390x844.png", 390, 844, 8),
            new CaptureSpec("Contrôles du mandat sur téléphone", "Chapter5_ExpeditionMandateChecks_390x844.png", 390, 844, 9),
            new CaptureSpec("Livraison défensive sur téléphone", "Chapter4_DefenseHandoffChoice_390x844.png", 390, 844, 10),
            new CaptureSpec("Contrôles de livraison sur téléphone", "Chapter4_DefenseHandoffChecks_390x844.png", 390, 844, 11),
            new CaptureSpec("Ravitaillement du couvain sur téléphone", "Chapter1_BroodSupplyChoice_390x844.png", 390, 844, 12),
            new CaptureSpec("Contrôles nourriciers sur téléphone", "Chapter1_BroodSupplyChecks_390x844.png", 390, 844, 13),
            new CaptureSpec("Préparation d'émergence sur téléphone", "Chapter2_WorkerHandoffChoice_390x844.png", 390, 844, 14),
            new CaptureSpec("Contrôles d'émergence sur téléphone", "Chapter2_WorkerHandoffChecks_390x844.png", 390, 844, 15),
            new CaptureSpec("Passation technique sur téléphone", "Chapter3_WorkshopCommissionChoice_390x844.png", 390, 844, 16),
            new CaptureSpec("Contrôles de passation sur téléphone", "Chapter3_WorkshopCommissionChecks_390x844.png", 390, 844, 17),
            new CaptureSpec("Mise en service en paysage", "Chapter1_Commissioning_1600x900.png", 1600, 900, 0),
            new CaptureSpec("Charte en paysage", "Chapter1_Charter_1600x900.png", 1600, 900, 1),
            new CaptureSpec("Contrôle sous charge en paysage", "Chapter1_LoadValidation_1600x900.png", 1600, 900, 2),
            new CaptureSpec("Sceau durable en paysage", "Chapter1_CommissioningSeal_1600x900.png", 1600, 900, 3),
            new CaptureSpec("Certification atelier en paysage", "Chapter4_CertificationChoice_1600x900.png", 1600, 900, 4),
            new CaptureSpec("Contrôles atelier en paysage", "Chapter4_CertificationChecks_1600x900.png", 1600, 900, 5),
            new CaptureSpec("Relais ouvrière en paysage", "Chapter3_WorkshopHandoffChoice_1600x900.png", 1600, 900, 6),
            new CaptureSpec("Contrôles de liaison en paysage", "Chapter3_WorkshopHandoffChecks_1600x900.png", 1600, 900, 7),
            new CaptureSpec("Mandat d'expédition en paysage", "Chapter5_ExpeditionMandateChoice_1600x900.png", 1600, 900, 8),
            new CaptureSpec("Contrôles du mandat en paysage", "Chapter5_ExpeditionMandateChecks_1600x900.png", 1600, 900, 9),
            new CaptureSpec("Livraison défensive en paysage", "Chapter4_DefenseHandoffChoice_1600x900.png", 1600, 900, 10),
            new CaptureSpec("Contrôles de livraison en paysage", "Chapter4_DefenseHandoffChecks_1600x900.png", 1600, 900, 11),
            new CaptureSpec("Ravitaillement du couvain en paysage", "Chapter1_BroodSupplyChoice_1600x900.png", 1600, 900, 12),
            new CaptureSpec("Contrôles nourriciers en paysage", "Chapter1_BroodSupplyChecks_1600x900.png", 1600, 900, 13),
            new CaptureSpec("Préparation d'émergence en paysage", "Chapter2_WorkerHandoffChoice_1600x900.png", 1600, 900, 14),
            new CaptureSpec("Contrôles d'émergence en paysage", "Chapter2_WorkerHandoffChecks_1600x900.png", 1600, 900, 15),
            new CaptureSpec("Passation technique en paysage", "Chapter3_WorkshopCommissionChoice_1600x900.png", 1600, 900, 16),
            new CaptureSpec("Contrôles de passation en paysage", "Chapter3_WorkshopCommissionChecks_1600x900.png", 1600, 900, 17)
        };

        static SandboxGuidedOpeningInstallationCapture()
        {
            ReattachCallbacks();
            if (!File.Exists(TriggerPath)) return;

            File.Delete(TriggerPath);
            EditorApplication.delayCall += RequestCaptureAfterReload;
        }

        [MenuItem("Bee Kingdom/Playground/Capture Guided Opening Installation")]
        public static void CaptureGuidedOpeningInstallation()
        {
            RequestCaptureAfterReload();
        }

        public static void CaptureGuidedOpeningInstallationAndExit()
        {
            SessionState.SetBool(StateExitWhenFinished, true);
            RequestCaptureAfterReload();
        }

        private static void RequestCaptureAfterReload()
        {
            if (SessionState.GetBool(StateRequested, false)) return;
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(StateStartAfterExit, true);
                ReattachCallbacks();
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                return;
            }

            StartCapture();
        }

        private static void StartCapture()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);

            SessionState.SetBool(StateRequested, true);
            SessionState.SetBool(StateStartAfterExit, false);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetInt(StateIndex, 0);
            SessionState.SetInt(StateConfiguredIndex, -1);
            SessionState.SetBool(StateCaptured, false);
            captureReadyAt = EditorApplication.timeSinceStartup + 3.5d;
            screenshotRequestedAt = 0d;
            ReattachCallbacks();

            PlaygroundPlayModeStartScene.UseLivingHiveOnPlay();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void ReattachCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            if (SessionState.GetBool(StateRequested, false)) EditorApplication.update += OnPlayModeUpdate;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode
                && SessionState.GetBool(StateStartAfterExit, false))
            {
                SessionState.SetBool(StateStartAfterExit, false);
                EditorApplication.delayCall += StartCapture;
                return;
            }

            if (!SessionState.GetBool(StateRequested, false)
                || state != PlayModeStateChange.EnteredPlayMode) return;

            ConfigureCurrentCapture();
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCaptured, false);
            captureReadyAt = EditorApplication.timeSinceStartup + 3.5d;
            screenshotRequestedAt = 0d;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            if (!Application.isPlaying || EditorApplication.timeSinceStartup < captureReadyAt) return;

            ConfigureCurrentCapture();
            ApplyCurrentState();
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 80) return;

            try
            {
                string path = CurrentPath();
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    ScreenCapture.CaptureScreenshot(path);
                    SessionState.SetBool(StateCaptured, true);
                    screenshotRequestedAt = EditorApplication.timeSinceStartup;
                    return;
                }

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (EditorApplication.timeSinceStartup - screenshotRequestedAt < 4d) return;
                    throw new InvalidOperationException("Screenshot was not written: " + path);
                }

                CaptureSpec capturedSpec = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
                (int width, int height) = ReadPngDimensions(path);
                if (width != capturedSpec.Width || height != capturedSpec.Height)
                {
                    throw new InvalidOperationException(
                        "Screenshot dimensions were " + width + "x" + height
                        + " instead of " + capturedSpec.Width + "x" + capturedSpec.Height + ": " + path);
                }

                int index = SessionState.GetInt(StateIndex, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateConfiguredIndex, -1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    captureReadyAt = EditorApplication.timeSinceStartup + 1.2d;
                    screenshotRequestedAt = 0d;
                    ConfigureCurrentCapture();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("Guided opening installation proof captured in " + OutputDirectory);
                if (SessionState.GetBool(StateExitWhenFinished, false)) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("Guided opening installation proof failed: " + exception);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                if (SessionState.GetBool(StateExitWhenFinished, false)) EditorApplication.Exit(1);
            }
        }

        private static void ConfigureCurrentCapture()
        {
            int index = Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1);
            if (SessionState.GetInt(StateConfiguredIndex, -1) == index) return;

            CaptureSpec capture = Captures[index];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            SessionState.SetInt(StateConfiguredIndex, index);
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.ServerPreparation);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(1f);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(0f, 0f);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.ResetAntLegionHudForProof();
            HiveViewProductUiPresenter.SetLocaleForRuntime(capture.Locale);
            PreparePreview(capture.State);
        }

        private static void PreparePreview(int state)
        {
            if (state >= 26)
            {
                PrepareOpeningProductionChoice();
                return;
            }
            if (state >= 25)
            {
                PrepareUpgradeBatchQualification();
                return;
            }
            if (state >= 24)
            {
                PrepareOpeningFoundationReward();
                return;
            }
            if (state >= 23)
            {
                PrepareWorkerOrientation();
                return;
            }
            if (state >= 22)
            {
                PrepareDefenseTacticalDebrief();
                return;
            }
            if (state >= 21)
            {
                PrepareOpeningCharterRatification();
                return;
            }
            if (state >= 20)
            {
                PrepareUpgradeOperationsDoctrineCheck();
                return;
            }
            if (state >= 18)
            {
                PrepareDefenseWorldBriefing(state == 19);
                return;
            }
            if (state >= 16)
            {
                PrepareWorkerWorkshopCommission(state == 17);
                return;
            }
            if (state >= 14)
            {
                PrepareBroodWorkerHandoff(state == 15);
                return;
            }
            if (state >= 12)
            {
                PrepareOpeningBroodSupply(state == 13);
                return;
            }
            if (state >= 10)
            {
                PrepareWorkshopDefenseHandoff(state == 11);
                return;
            }
            if (state >= 8)
            {
                PrepareDefenseExpeditionMandate(state == 9);
                return;
            }
            if (state >= 6)
            {
                PrepareWorkerWorkshopHandoff(state == 7);
                return;
            }
            if (state >= 4)
            {
                PrepareWorkshopCertification(state == 5);
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedOpeningInstallationForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitRouteForProof(round == 0);
                HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitRouteForProof();
                HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
                HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitMaintenanceForProof(round == 1);
                HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitMaintenanceForProof();
                if (round == 0) HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            }

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ClearGuidedOpeningRouteFeedbackForProof();
            if (state == 0) return;

            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(2);
            if (state == 1) return;

            HiveViewProductUiPresenter.ChooseGuidedOpeningCharterForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCharterForProof();
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedOpeningCommissioningLoadForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCommissioningLoadForProof();
            HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
            if (state == 2) return;

            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(2);
        }

        private static void PrepareOpeningProductionChoice()
        {
            HiveViewProductUiPresenter.BeginGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.DismissGuidedChapterIntroForProof(1);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
        }

        private static void PrepareOpeningCharterRatification()
        {
            PreparePreview(1);
            HiveViewProductUiPresenter.ChooseGuidedOpeningCharterForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCharterForProof();
        }

        private static void PrepareDefenseTacticalDebrief()
        {
            HiveViewProductUiPresenter.BeginGuidedDefenseTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("guard_post");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedDefenseScoutingForProof();
            HiveViewProductUiPresenter.ChooseGuidedDefensePlanForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedDefenseForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
        }

        private static void PrepareWorkerOrientation()
        {
            HiveViewProductUiPresenter.BeginGuidedWorkerOrientationForProof(true);
        }

        private static void PrepareOpeningFoundationReward()
        {
            HiveViewProductUiPresenter.BeginGuidedOpeningRewardChoiceForProof();
        }

        private static void PrepareUpgradeBatchQualification()
        {
            HiveViewProductUiPresenter.BeginGuidedUpgradeTutorialForProof();
            HiveViewProductUiPresenter.DismissGuidedChapterFourResourcePrimerForProof();
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
        }

        private static void PrepareWorkshopCertification(bool showChecks)
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
            if (!showChecks) return;
            HiveViewProductUiPresenter.ChooseGuidedUpgradeCertificationForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedUpgradeCertificationForProof();
        }

        private static void PrepareWorkerWorkshopHandoff(bool showChecks)
        {
            HiveViewProductUiPresenter.BeginGuidedWorkerCertificationForProof();
            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
                HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationTaskForProof(true);
                HiveViewProductUiPresenter.CompleteGuidedWorkerCertificationTaskForProof();
                HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(0);
                HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(1);
                HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(2);
                HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationMentorshipForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedWorkerCertificationMentorshipForProof();
            }
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            if (!showChecks) return;
            HiveViewProductUiPresenter.ChooseGuidedWorkerWorkshopHandoffForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedWorkerWorkshopHandoffForProof();
            HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop");
        }

        private static void PrepareWorkerWorkshopCommission(bool showChecks)
        {
            HiveViewProductUiPresenter.BeginGuidedWorkerWorkshopCommissionForProof();
            if (!showChecks) return;
            HiveViewProductUiPresenter.ChooseGuidedWorkerWorkshopCommissionForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedWorkerWorkshopCommissionForProof();
        }

        private static void PrepareDefenseExpeditionMandate(bool showChecks)
        {
            HiveViewProductUiPresenter.BeginGuidedReadinessLoopForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.ChooseGuidedReadinessProductionForProof(true);
                HiveViewProductUiPresenter.CompleteGuidedReadinessProductionForProof();
                HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
                HiveViewProductUiPresenter.CollectManualProductionForProof("warehouse_cells");
                HiveViewProductUiPresenter.ChooseGuidedReadinessPopulationForProof(true);
                HiveViewProductUiPresenter.CompleteGuidedReadinessPopulationForProof();
                HiveViewProductUiPresenter.ChooseGuidedReadinessDefenseForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedReadinessDefenseForProof();
                HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(0);
                HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(1);
                HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(2);
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            }
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            if (!showChecks) return;
            HiveViewProductUiPresenter.ChooseGuidedDefenseExpeditionMandateForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedDefenseExpeditionMandateForProof();
        }

        private static void PrepareWorkshopDefenseHandoff(bool showChecks)
        {
            PrepareUpgradeOperationsDoctrineCheck();
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsDoctrineCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsDoctrineCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsDoctrineCheckForProof(2);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            if (!showChecks) return;
            HiveViewProductUiPresenter.ChooseGuidedWorkshopDefenseHandoffForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedWorkshopDefenseHandoffForProof();
            HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop");
        }

        private static void PrepareUpgradeOperationsDoctrineCheck()
        {
            HiveViewProductUiPresenter.BeginGuidedUpgradeOperationsForProof();
            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
                HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsSupplyForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsSupplyForProof();
                HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop");
                HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsDeploymentForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsDeploymentForProof();
                HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(0);
                HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(1);
                HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(2);
            }
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedUpgradeCertificationForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedUpgradeCertificationForProof();
            HiveViewProductUiPresenter.RegisterGuidedUpgradeCertificationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeCertificationCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeCertificationCheckForProof(2);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsDoctrineForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsDoctrineForProof();
        }

        private static void PrepareDefenseWorldBriefing(bool showSimulation)
        {
            HiveViewProductUiPresenter.BeginGuidedDefenseWorldBriefingForProof();
            if (!showSimulation) return;
            HiveViewProductUiPresenter.ChooseGuidedDefenseWorldBriefingForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedDefenseWorldBriefingForProof();
        }

        private static void PrepareOpeningBroodSupply(bool showChecks)
        {
            PreparePreview(3);
            HiveViewProductUiPresenter.ChooseGuidedOpeningCommissioningSealForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCommissioningSealForProof();
            if (!showChecks) return;
            HiveViewProductUiPresenter.ChooseGuidedOpeningBroodSupplyForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedOpeningBroodSupplyForProof();
            HiveViewProductUiPresenter.CollectManualProductionForProof("warehouse_cells");
        }

        private static void PrepareBroodWorkerHandoff(bool showChecks)
        {
            HiveViewProductUiPresenter.BeginGuidedBroodIncubationForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationInspectionForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedBroodIncubationInspectionForProof();
                HiveViewProductUiPresenter.ChooseExpectedGuidedBroodIncubationVitalityPriorityForProof();
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(0);
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(1);
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(2);
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationTreatmentForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedBroodIncubationTreatmentForProof();
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            }
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationDoctrineForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedBroodIncubationDoctrineForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            if (!showChecks) return;
            HiveViewProductUiPresenter.ChooseGuidedBroodWorkerHandoffForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedBroodWorkerHandoffForProof();
            HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Guided Opening Installation Visual Proof");
            builder.AppendLine();
            builder.AppendLine("- Scene: `LivingHive`");
            builder.AppendLine("- Protected hive artwork changed: `false`");
            builder.AppendLine("- Tutorial scrim opacity: `0.12`");
            builder.AppendLine("- Purpose: verify Chapter 1 bilingual first-session production controls, commissioning, charter ratification and the founding grant, worker orientation and handoff, workshop batch qualification, certification and doctrine validation, tactical defense debrief, expedition mandate, and world briefing controls in phone portrait and landscape layouts.");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            }
            return builder.ToString();
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
        }

        private static string PathFor(CaptureSpec capture)
        {
            return OutputDirectory + "/" + capture.FileName;
        }

        private static void TrySetGameViewSize(int width, int height, string label)
        {
            try
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
                Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                Type gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                Type gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type gameViewSizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                Type gameViewSizeGroupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
                Type scriptableSingletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
                object sizesInstance = scriptableSingletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                object activeGroupType = Enum.Parse(gameViewSizeGroupType, "Standalone");
                object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizesInstance, new[] { activeGroupType });
                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                object customSize = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) }).Invoke(new[] { fixedResolution, width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
                int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                gameView.Show();
                gameView.maximized = false;
                gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, selectedIndex);
                gameView.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to force guided opening Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static (int Width, int Height) ReadPngDimensions(string path)
        {
            byte[] header = new byte[24];
            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.Read(header, 0, header.Length) != header.Length)
                {
                    throw new InvalidOperationException("PNG header is incomplete: " + path);
                }
            }

            int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return (width, height);
        }
    }
}
