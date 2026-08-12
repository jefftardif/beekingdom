using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee827PlayableHiveProductTrancheCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-067_BEE821_827_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-067_BEE821_827_Manifest.md";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE821_827_Report.md";
        private const string StateRequested = "BeeKingdom.Playground.BEE827.Requested";
        private const string StateFrames = "BeeKingdom.Playground.BEE827.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.BEE827.Captured";
        private const string StateIndex = "BeeKingdom.Playground.BEE827.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string HotspotId;
            public readonly string LoopState;
            public readonly Vector2 Pan;
            public readonly float Zoom;

            public CaptureSpec(string label, string fileName, int width, int height, string hotspotId, string loopState, Vector2 pan, float zoom)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                HotspotId = hotspotId;
                LoopState = loopState;
                Pan = pan;
                Zoom = zoom;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Phone portrait final polish candidate", "BEE827_01_PhonePortrait_390x844.png", 390, 844, "guard_post", "training_running", new Vector2(-112f, 42f), 1.28f),
            new CaptureSpec("Tablet landscape hive dominant", "BEE827_02_TabletLandscape_1280x720.png", 1280, 720, "honey_storage", "resources_tick", Vector2.zero, 1.10f),
            new CaptureSpec("One finger pan proof telemetry", "BEE827_03_OneFingerPan_1280x720.png", 1280, 720, "administration_core", "gesture_pan_proof", new Vector2(64f, -22f), 1.12f),
            new CaptureSpec("Two finger pinch proof telemetry", "BEE827_04_TwoFingerPinch_1280x720.png", 1280, 720, "administration_core", "gesture_pinch_proof", new Vector2(44f, -12f), 1.22f),
            new CaptureSpec("UI blocks hive gesture", "BEE827_05_UiBlocksHiveGesture_1280x720.png", 1280, 720, "honey_storage", "ui_gesture_blocked", Vector2.zero, 1.10f),
            new CaptureSpec("Rapid tap upgrade guarded", "BEE827_06_RapidTapUpgrade_1280x720.png", 1280, 720, "honey_storage", "double_upgrade_guard", Vector2.zero, 1.10f),
            new CaptureSpec("Rapid tap training guarded", "BEE827_07_RapidTapTraining_1280x720.png", 1280, 720, "guard_post", "double_training_guard", new Vector2(-18f, 8f), 1.12f)
        };

        static SandboxBee827PlayableHiveProductTrancheCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-067 BEE-821-827 Source")]
        public static void CaptureBee821827Source()
        {
            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            DeleteIfExists(ReportPath);
            SessionState.SetBool(StateRequested, true);
            SessionState.SetBool(StateCaptured, false);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetInt(StateIndex, 0);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false) || state != PlayModeStateChange.EnteredPlayMode) return;
            ApplyCurrentState();
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCaptured, false);
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            ApplyCurrentState();
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 70) return;

            try
            {
                string path = CurrentPath();
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    ScreenCapture.CaptureScreenshot(path);
                    SessionState.SetBool(StateCaptured, true);
                    return;
                }

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (frames < 160) return;
                    throw new InvalidOperationException("DEMO-067 screenshot was not written: " + path);
                }

                int index = SessionState.GetInt(StateIndex, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    ApplyCurrentState();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-067 BEE-821-827 source captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-067 BEE-821-827 capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.ServerPreparation);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(capture.HotspotId);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(capture.Zoom);
            HiveViewProductUiPresenter.TriggerProductionFeedbackPulseForProof(capture.HotspotId);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState(capture.LoopState);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-067 BEE-821-827 Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Scope");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit preview`");
            builder.AppendLine("- BEE couvertes: `821-827 uniquement`");
            builder.AppendLine("- BEE-828+: `non implemente`");
            builder.AppendLine("- Carte monde modifiee: `false`");
            builder.AppendLine("- Simulation locale de demonstration: `true`");
            builder.AppendLine("- Progression serveur officielle: `false`");
            builder.AppendLine("- Sauvegarde active: `false`");
            builder.AppendLine("- Economie officielle active: `false`");
            builder.AppendLine("- Armee persistante officielle: `false`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine();
            builder.AppendLine("## Device Touch Protocol");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveDeviceTouchProtocolForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Gesture Telemetry - One Finger Pan");
            builder.AppendLine();
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("gesture_pan_proof");
            foreach (string row in HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Gesture Telemetry - Two Finger Pinch");
            builder.AppendLine();
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("gesture_pinch_proof");
            foreach (string row in HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Gesture Telemetry - UI Blocks Hive Gesture");
            builder.AppendLine();
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("ui_gesture_blocked");
            foreach (string row in HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Deterministic Checks");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_067 = YES");
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A BEE-821 a BEE-827 Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed with recommendations");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("Tranche BEE-821 a BEE-827 implementee cote Unity pour consolider la Ruche jouable produit preview : polish portrait, tablette paysage, protocole tactile, rapid tap upgrade/training et checks deterministes locaux. Aucun travail BEE-828+, carte monde, serveur live, sauvegarde officielle, economie officielle ou armee persistante officielle.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee827PlayableHiveAutomationTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee827PlayableHiveProductTrancheCapture.cs`");
            builder.AppendLine();
            builder.AppendLine("## Preuves source");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("* " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine("* Manifest: `" + ManifestPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Tests attendus");
            builder.AppendLine();
            builder.AppendLine("* `SandboxBee827PlayableHiveAutomationTests.RapidTapUpgradeTrainingAndDeterministicLoopChecksRemainGuarded`");
            builder.AppendLine("* `SandboxBee827PlayableHiveAutomationTests.HiveDeviceTouchProtocolKeepsPreviewNonServerScope`");
            builder.AppendLine("* `SandboxBee827PlayableHiveAutomationTests.GestureTelemetryExposesPanPinchAndFixedUiRules`");
            builder.AppendLine();
            builder.AppendLine("## Limites non-live");
            builder.AppendLine();
            builder.AppendLine("* Simulation locale de demonstration uniquement.");
            builder.AppendLine("* Aucune progression serveur officielle.");
            builder.AppendLine("* Aucune sauvegarde officielle.");
            builder.AppendLine("* Aucune economie officielle.");
            builder.AppendLine("* Aucune armee persistante officielle.");
            builder.AppendLine("* Preuve tactile physique reelle a fournir par Demo/QA sur appareil.");
            builder.AppendLine();
            builder.AppendLine("## READY_FOR_DEMO_067");
            builder.AppendLine();
            builder.AppendLine("YES");
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

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static void TrySetGameViewSize(int width, int height, string label)
        {
            Type gameView = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            EditorWindow window = EditorWindow.GetWindow(gameView);
            if (window != null)
            {
                window.minSize = new Vector2(width, height);
                window.maxSize = new Vector2(width, height);
                window.position = new Rect(20f, 20f, width, height);
                window.Repaint();
            }

            Debug.Log("DEMO-067 capture profile: " + label + " " + width + "x" + height);
        }
    }
}
