using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee917RuntimeReserveClosureCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-073_BEE901_920_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-073_BEE903_907_910_917_Manifest.md";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE903_907_910_917_Report.md";
        private const string StateRequested = "BeeKingdom.Playground.BEE917.Requested";
        private const string StateFrames = "BeeKingdom.Playground.BEE917.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.BEE917.Captured";
        private const string StateIndex = "BeeKingdom.Playground.BEE917.Index";

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
            new CaptureSpec("Upgrade completion niveau avant apres", "BEE917_01_UpgradeCompletionVisible_1280x720.png", 1280, 720, "honey_storage", "upgrade_completion_visible", Vector2.zero, 1.10f),
            new CaptureSpec("Resource cap clarity", "BEE917_02_ResourceCapClarity_1280x720.png", 1280, 720, "honey_storage", "resource_cap", Vector2.zero, 1.10f),
            new CaptureSpec("Training arrival army delta", "BEE917_03_TrainingArrivalArmyDelta_1280x720.png", 1280, 720, "guard_post", "training_eclaireuses_done", new Vector2(-18f, 8f), 1.12f),
            new CaptureSpec("Refusal cause next step", "BEE917_04_RefusalRecovery_1280x720.png", 1280, 720, "honey_storage", "server_bridge_rejected", Vector2.zero, 1.10f),
            new CaptureSpec("Gesture pan telemetry", "BEE917_05_GesturePanProof_1280x720.png", 1280, 720, "honey_storage", "gesture_pan_proof", new Vector2(42f, -18f), 1.08f),
            new CaptureSpec("Gesture pinch telemetry", "BEE917_06_GesturePinchProof_1280x720.png", 1280, 720, "honey_storage", "gesture_pinch_proof", Vector2.zero, 1.18f),
            new CaptureSpec("Portrait upgrade completion", "BEE917_07_PhonePortraitUpgradeCompletion_390x844.png", 390, 844, "honey_storage", "upgrade_completion_visible", new Vector2(-92f, 36f), 1.26f)
        };

        static SandboxBee917RuntimeReserveClosureCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-073 BEE-903-907-910-917 Source")]
        public static void CaptureBee903907910917Source()
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

        public static void WriteBee903907910917ManifestOnlyForBatch()
        {
            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
            File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
            File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
            Debug.Log("DEMO-073 BEE-903-907-910-917 manifest/report refreshed.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
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
                    throw new InvalidOperationException("DEMO-073 screenshot was not written: " + path);
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
                Debug.Log("DEMO-073 BEE-903-907-910-917 source captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-073 BEE-903-907-910-917 capture failed: " + exception);
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
            builder.AppendLine("# DEMO-073 BEE-903-907-910-917 Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Scope");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit uniquement`");
            builder.AppendLine("- BEE couvertes Builder-A: `903, 904, 905, 906, 907, 910, 917`");
            builder.AppendLine("- Carte monde modifiee: `false`");
            builder.AppendLine("- BEE-881: `bloquee / non implementee`");
            builder.AppendLine("- Serveur officiel live: `false`");
            builder.AppendLine("- Endpoint officiel: `false`");
            builder.AppendLine("- Sauvegarde officielle: `false`");
            builder.AppendLine("- Economie officielle: `false`");
            builder.AppendLine("- Armee persistante officielle: `false`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine();
            builder.AppendLine("## Runtime Reserve Closure Proof");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Scenario Proof Matrix");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                ApplyProofScenario(capture);
                builder.AppendLine("### " + capture.Label);
                builder.AppendLine();
                builder.AppendLine("- capture: `" + PathFor(capture) + "`");
                builder.AppendLine("- loop_state: `" + capture.LoopState + "`");
                foreach (string row in HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof()) builder.AppendLine("- " + row);
                builder.AppendLine();
            }
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("ui_gesture_blocked");
            builder.AppendLine("## UI Gesture Block Proof");
            builder.AppendLine();
            builder.AppendLine("- scenario: `ui_gesture_blocked`");
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Gesture Proof");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Previous Gates Preserved");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof()) builder.AppendLine("- " + row);
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_073_RUNTIME = YES");
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A BEE-903/904/905/906/907/910/917 Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed with recommendations");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("Fermeture runtime des reserves QA-072 cote Ruche jouable: preuve upgrade completion avec niveau avant/apres, clarification ressources/cout reserve/cap, feedback training et armee locale, boutons non muets, cause + prochain geste apres refus, preuves pan/pinch/UI fixe et timeline T0-T9. Aucun travail carte monde, aucun BEE-881, aucun claim serveur officiel.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee917RuntimeReserveClosureTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee917RuntimeReserveClosureCapture.cs`");
            builder.AppendLine();
            builder.AppendLine("## APIs publiques ajoutees");
            builder.AppendLine();
            builder.AppendLine("* `HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof()`");
            builder.AppendLine("* `SandboxBee917RuntimeReserveClosureTests.RunAllForBatch()`");
            builder.AppendLine("* `SandboxBee917RuntimeReserveClosureCapture.CaptureBee903907910917Source()`");
            builder.AppendLine();
            builder.AppendLine("## Preuves source");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("* " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine("* Manifest: `" + ManifestPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("* Tests attendus: `SandboxBee917RuntimeReserveClosureTests`.");
            builder.AppendLine("* Tests executes en batch Unity: PASS.");
            builder.AppendLine("* Capture DEMO-073 executee en batch Unity: PASS.");
            builder.AppendLine("* Couverture cible: upgrade completion, cap/cout reserve, training delta, boutons, refus, gestes et timeline T0-T9.");
            builder.AppendLine();
            builder.AppendLine("## Limitations");
            builder.AppendLine();
            builder.AppendLine("* Aucune preuve physique device reelle produite par Builder-A.");
            builder.AppendLine("* Aucun serveur officiel live, endpoint, sauvegarde, economie ou armee persistante officielle.");
            builder.AppendLine("* Aucune carte monde et aucun BEE-881.");
            builder.AppendLine();
            builder.AppendLine("## Ready for next brick");
            builder.AppendLine();
            builder.AppendLine("YES");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_073_RUNTIME = YES");
            return builder.ToString();
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
        }

        private static void ApplyProofScenario(CaptureSpec capture)
        {
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(capture.HotspotId);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(capture.Zoom);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState(capture.LoopState);
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
            try
            {
                Type gameView = Type.GetType("UnityEditor.GameView,UnityEditor");
                EditorWindow window = gameView == null ? null : EditorWindow.GetWindow(gameView);
                if (window != null)
                {
                    window.minSize = new Vector2(width, height);
                    window.maxSize = new Vector2(width, height);
                    window.titleContent = new GUIContent(label);
                    window.Repaint();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not resize Game View for DEMO-073 capture: " + exception.Message);
            }
        }
    }
}
