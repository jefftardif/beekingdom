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
    public static class SandboxGuidedBroodIncubationCapture
    {
        private const string ScenePath = "Assets/Scenes/LivingHive.unity";
        private const string OutputDirectory = "Artifacts/GuidedBroodIncubation";
        private const string ManifestPath = OutputDirectory + "/GuidedBroodIncubationManifest.md";
        private const string TriggerPath = "Temp/GuidedBroodIncubationCapture.request";
        private const string StateRequested = "BeeKingdom.Playground.GuidedBroodIncubation.Requested";
        private const string StateStartAfterExit = "BeeKingdom.Playground.GuidedBroodIncubation.StartAfterExit";
        private const string StateFrames = "BeeKingdom.Playground.GuidedBroodIncubation.Frames";
        private const string StateIndex = "BeeKingdom.Playground.GuidedBroodIncubation.Index";
        private const string StateCaptured = "BeeKingdom.Playground.GuidedBroodIncubation.Captured";
        private const string StateConfiguredIndex = "BeeKingdom.Playground.GuidedBroodIncubation.ConfiguredIndex";
        private const string StateAppliedIndex = "BeeKingdom.Playground.GuidedBroodIncubation.AppliedIndex";
        private const string StateExitWhenFinished = "BeeKingdom.Playground.GuidedBroodIncubation.ExitWhenFinished";
        private static double captureReadyAt;
        private static double screenshotRequestedAt;

        private enum PreviewState
        {
            OpeningReserveFeedback,
            OpeningNurseryFeedback,
            ChapterOneHandoff,
            InspectionChoice,
            VitalityCareRunning,
            VitalityAssessment,
            DoctrineChoice,
            ReadinessCheck,
            QueueResume,
            TutorialResume,
            StrategicProfile
        }

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly PreviewState State;

            public CaptureSpec(string label, string fileName, int width, int height, PreviewState state)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                State = state;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Retour route reserve sur telephone", "Chapter1_RouteReserveFeedback_390x844.png", 390, 844, PreviewState.OpeningReserveFeedback),
            new CaptureSpec("Retour relais couvain sur telephone", "Chapter1_RouteNurseryFeedback_390x844.png", 390, 844, PreviewState.OpeningNurseryFeedback),
            new CaptureSpec("Passage du circuit sur telephone", "Chapter2_Chapter1Handoff_390x844.png", 390, 844, PreviewState.ChapterOneHandoff),
            new CaptureSpec("Observation d'incubation sur téléphone", "Chapter2_IncubationInspection_390x844.png", 390, 844, PreviewState.InspectionChoice),
            new CaptureSpec("Vitalité du couvain en soin sur téléphone", "Chapter2_NurseryVitalityCare_390x844.png", 390, 844, PreviewState.VitalityCareRunning),
            new CaptureSpec("Lecture active de vitalité sur téléphone", "Chapter2_VitalityAssessment_390x844.png", 390, 844, PreviewState.VitalityAssessment),
            new CaptureSpec("Doctrine du couvain sur téléphone", "Chapter2_IncubationDoctrine_390x844.png", 390, 844, PreviewState.DoctrineChoice),
            new CaptureSpec("Observation d'incubation en paysage", "Chapter2_IncubationInspection_1600x900.png", 1600, 900, PreviewState.InspectionChoice),
            new CaptureSpec("Vitalité du couvain en soin en paysage", "Chapter2_NurseryVitalityCare_1600x900.png", 1600, 900, PreviewState.VitalityCareRunning),
            new CaptureSpec("Lecture active de vitalité en paysage", "Chapter2_VitalityAssessment_1600x900.png", 1600, 900, PreviewState.VitalityAssessment),
            new CaptureSpec("Doctrine du couvain en paysage", "Chapter2_IncubationDoctrine_1600x900.png", 1600, 900, PreviewState.DoctrineChoice),
            new CaptureSpec("Retour route reserve en paysage", "Chapter1_RouteReserveFeedback_1600x900.png", 1600, 900, PreviewState.OpeningReserveFeedback),
            new CaptureSpec("Retour relais couvain en paysage", "Chapter1_RouteNurseryFeedback_1600x900.png", 1600, 900, PreviewState.OpeningNurseryFeedback),
            new CaptureSpec("Passage du circuit en paysage", "Chapter2_Chapter1Handoff_1600x900.png", 1600, 900, PreviewState.ChapterOneHandoff),
            new CaptureSpec("Verification tactique sur téléphone", "Chapter5_ReadinessChecks_390x844.png", 390, 844, PreviewState.ReadinessCheck),
            new CaptureSpec("Verification tactique en paysage", "Chapter5_ReadinessChecks_1600x900.png", 1600, 900, PreviewState.ReadinessCheck),
            new CaptureSpec("Reprise des files sur téléphone", "QueueResume_390x844.png", 390, 844, PreviewState.QueueResume),
            new CaptureSpec("Reprise des files en paysage", "QueueResume_1600x900.png", 1600, 900, PreviewState.QueueResume),
            new CaptureSpec("Reprise du tutoriel sur téléphone", "TutorialResume_390x844.png", 390, 844, PreviewState.TutorialResume),
            new CaptureSpec("Reprise du tutoriel en paysage", "TutorialResume_1600x900.png", 1600, 900, PreviewState.TutorialResume),
            new CaptureSpec("Profil strategique sur telephone", "StrategicProfile_390x844.png", 390, 844, PreviewState.StrategicProfile),
            new CaptureSpec("Profil strategique en paysage", "StrategicProfile_1600x900.png", 1600, 900, PreviewState.StrategicProfile)
        };

        static SandboxGuidedBroodIncubationCapture()
        {
            ReattachCallbacks();
            if (!File.Exists(TriggerPath)) return;

            File.Delete(TriggerPath);
            EditorApplication.delayCall += RequestCaptureAfterReload;
        }

        [MenuItem("Bee Kingdom/Playground/Capture Guided Brood Incubation")]
        public static void CaptureGuidedBroodIncubation()
        {
            RequestCaptureAfterReload();
        }

        public static void CaptureGuidedBroodIncubationAndExit()
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
            SessionState.SetInt(StateAppliedIndex, -1);
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
            int currentIndex = SessionState.GetInt(StateIndex, 0);
            if (SessionState.GetInt(StateAppliedIndex, -1) != currentIndex)
            {
                ApplyCurrentState();
                SessionState.SetInt(StateAppliedIndex, currentIndex);
            }
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
                    SessionState.SetInt(StateAppliedIndex, -1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    captureReadyAt = EditorApplication.timeSinceStartup + 1.2d;
                    screenshotRequestedAt = 0d;
                    ConfigureCurrentCapture();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), new UTF8Encoding(false));
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("Guided brood incubation proof captured in " + OutputDirectory);
                EditorApplication.delayCall += ExitEditorIfRequested;
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("Guided brood incubation proof failed: " + exception);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                if (SessionState.GetBool(StateExitWhenFinished, false)) EditorApplication.Exit(1);
            }
        }

        private static void ExitEditorIfRequested()
        {
            if (!SessionState.GetBool(StateExitWhenFinished, false)) return;
            SessionState.SetBool(StateExitWhenFinished, false);
            EditorApplication.Exit(0);
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
            PreparePreview(capture.State);
        }

        private static void PreparePreview(PreviewState state)
        {
            if (state == PreviewState.StrategicProfile)
            {
                var profile = new LocalPreviewStrategicProfile
                {
                    profileId = "visual-proof",
                    revision = 5,
                    openingCharter = "brood_bridge",
                    broodDevelopment = "growth",
                    broodDoctrine = "resilience",
                    workerAssignment = "honey",
                    workshopSpecialization = "production",
                    workshopDoctrine = "precision",
                    operationalHoneyProductionBonus = 0.02f
                };
                HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(new CaptureStrategicProfileStore(JsonUtility.ToJson(profile)));
                HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
                HiveViewProductUiPresenter.OpenLocalPreviewStrategicProfileForProof();
                return;
            }

            if (state == PreviewState.QueueResume)
            {
                HiveViewProductUiPresenter.SelectReferenceHotspotForProof("wax_workshop");
                HiveViewProductUiPresenter.StartFocusedUpgradeForProof();
                HiveViewProductUiPresenter.StartSoldierTrainingForProof();
                HiveViewProductUiPresenter.SimulateLocalPreviewQueueRestartForProof();
                return;
            }

            if (state == PreviewState.TutorialResume)
            {
                HiveViewProductUiPresenter.BeginGuidedBroodTutorialForProof();
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
                HiveViewProductUiPresenter.SimulateLocalPreviewTutorialRestartForProof();
                return;
            }

            if (state == PreviewState.OpeningReserveFeedback || state == PreviewState.OpeningNurseryFeedback)
            {
                HiveViewProductUiPresenter.PreviewGuidedOpeningRouteFeedbackForProof(state == PreviewState.OpeningNurseryFeedback);
                return;
            }

            if (state == PreviewState.ChapterOneHandoff)
            {
                HiveViewProductUiPresenter.BeginGuidedBroodHandoffForProof();
                return;
            }

            if (state == PreviewState.ReadinessCheck)
            {
                HiveViewProductUiPresenter.BeginGuidedReadinessLoopForProof();
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
                HiveViewProductUiPresenter.ChooseGuidedReadinessProductionForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedReadinessProductionForProof();
                HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
                HiveViewProductUiPresenter.CollectManualProductionForProof("warehouse_cells");
                HiveViewProductUiPresenter.ChooseGuidedReadinessPopulationForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedReadinessPopulationForProof();
                HiveViewProductUiPresenter.ChooseGuidedReadinessDefenseForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedReadinessDefenseForProof();
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedBroodIncubationForProof();
            HiveViewProductUiPresenter.SetBroodVitalityForProof(73f, 79f);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            if (state == PreviewState.InspectionChoice) return;
            if (state == PreviewState.VitalityCareRunning)
            {
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationInspectionForProof(true);
                HiveViewProductUiPresenter.FreezeGuidedBroodIncubationInspectionForProof(0.42f);
                return;
            }
            if (state == PreviewState.VitalityAssessment)
            {
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationInspectionForProof(true);
                HiveViewProductUiPresenter.CompleteGuidedBroodIncubationInspectionForProof();
                return;
            }

            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationInspectionForProof(round == 0);
                HiveViewProductUiPresenter.CompleteGuidedBroodIncubationInspectionForProof();
                HiveViewProductUiPresenter.ChooseExpectedGuidedBroodIncubationVitalityPriorityForProof();
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(0);
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(1);
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(2);
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationTreatmentForProof(round == 0);
                HiveViewProductUiPresenter.CompleteGuidedBroodIncubationTreatmentForProof();
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            }
        }

        private sealed class CaptureStrategicProfileStore : ILocalPreviewStrategicProfileStore
        {
            private string json;
            public CaptureStrategicProfileStore(string json) { this.json = json; }
            public string Read() => json;
            public void Write(string value) { json = value; }
            public void Delete() { json = string.Empty; }
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Guided Brood Incubation Visual Proof");
            builder.AppendLine();
            builder.AppendLine("- Scene: `LivingHive`");
            builder.AppendLine("- Protected hive artwork changed: `false`");
            builder.AppendLine("- Tutorial scrim opacity: `0.12`");
            builder.AppendLine("- Chapter objective target: `15`");
            builder.AppendLine("- Nursery vitality authority: `local_preview_non_official`; production values remain server-owned.");
            builder.AppendLine("- Deterministic incubation proof seed: `nutrition=73`, `stability=79`; this value is not production authority.");
            builder.AppendLine("- Device cache policy: `last_acknowledged_snapshot_only`; offline mutations are disabled.");
            builder.AppendLine("- Purpose: verify Chapter 1 route feedback, the Chapter 1-to-2 consequence handoff, nursery vitality and care progress, active vitality interpretation, incubation choices, and Chapter 5 tactical readiness checks in phone portrait and landscape layouts.");
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
                Debug.LogWarning("Unable to force guided brood Game View size " + width + "x" + height + ": " + exception.Message);
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
