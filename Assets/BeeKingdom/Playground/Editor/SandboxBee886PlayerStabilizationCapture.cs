using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee886PlayerStabilizationCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-072_BEE882_900_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-072_BEE882_886_Manifest.md";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE882_886_Report.md";
        private const string StateRequested = "BeeKingdom.Playground.BEE886.Requested";
        private const string StateFrames = "BeeKingdom.Playground.BEE886.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.BEE886.Captured";
        private const string StateIndex = "BeeKingdom.Playground.BEE886.Index";

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
            new CaptureSpec("Production tick visible", "BEE886_01_ProduceTick_1280x720.png", 1280, 720, "honey_storage", "resources_tick", Vector2.zero, 1.10f),
            new CaptureSpec("Upgrade cout reserve pending", "BEE886_02_UpgradeReservedPending_1280x720.png", 1280, 720, "honey_storage", "server_bridge_pending", Vector2.zero, 1.10f),
            new CaptureSpec("Upgrade refus recuperation", "BEE886_03_UpgradeRejectedRecovery_1280x720.png", 1280, 720, "honey_storage", "server_bridge_rejected", Vector2.zero, 1.10f),
            new CaptureSpec("Training queue visible", "BEE886_04_TrainingQueue_1280x720.png", 1280, 720, "guard_post", "training_gardiennes_running", new Vector2(-18f, 8f), 1.12f),
            new CaptureSpec("Training arrivee troupe", "BEE886_05_TrainingArrivalArmy_1280x720.png", 1280, 720, "guard_post", "training_eclaireuses_done", new Vector2(-18f, 8f), 1.12f),
            new CaptureSpec("Source serveur requis", "BEE886_06_ActionSourceServerRequired_1280x720.png", 1280, 720, "research", "server_required", new Vector2(-10f, 0f), 1.10f),
            new CaptureSpec("Portrait refus recuperation", "BEE886_07_PhonePortraitRejectedRecovery_390x844.png", 390, 844, "honey_storage", "server_bridge_rejected", new Vector2(-92f, 36f), 1.26f)
        };

        static SandboxBee886PlayerStabilizationCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-072 BEE-882-886 Source")]
        public static void CaptureBee882886Source()
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
                    throw new InvalidOperationException("DEMO-072 screenshot was not written: " + path);
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
                Debug.Log("DEMO-072 BEE-882-886 source captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-072 BEE-882-886 capture failed: " + exception);
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
            builder.AppendLine("# DEMO-072 BEE-882-886 Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Scope");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit uniquement`");
            builder.AppendLine("- BEE couvertes: `882-886`");
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
            builder.AppendLine("## Player Stabilization Proof");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Runtime State");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveLoopStateForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Preserved Previous Gates");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveActionLoopForProof()) builder.AppendLine("- " + row);
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveDevOnlyBridgeForProof()) builder.AppendLine("- " + row);
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_072_RUNTIME = YES");
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A BEE-882 a BEE-886 Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed with recommendations");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("BEE-882 a BEE-886 stabilisent la boucle joueur de la Ruche: production visible, cout reserve, cout unique, decision upgrade/training, queue lisible, arrivee de troupes locales, source d'action et guidance apres refus. Le tout reste simulation locale/dev-only sans serveur officiel, endpoint, sauvegarde, economie ou armee persistante.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee886PlayerStabilizationTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee886PlayerStabilizationCapture.cs`");
            builder.AppendLine();
            builder.AppendLine("## APIs publiques ajoutees");
            builder.AppendLine();
            builder.AppendLine("* `HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof()`");
            builder.AppendLine("* `SandboxBee886PlayerStabilizationTests.RunAllForBatch()`");
            builder.AppendLine("* `SandboxBee886PlayerStabilizationCapture.CaptureBee882886Source()`");
            builder.AppendLine();
            builder.AppendLine("## Preuves source");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("* " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine("* Manifest: `" + ManifestPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("* Tests attendus: `SandboxBee886PlayerStabilizationTests`.");
            builder.AppendLine("* Couverture cible: produce/spend, cout reserve, rapid tap, upgrade decision, training queue/arrival, source action, recovery apres refus et non-claims.");
            builder.AppendLine();
            builder.AppendLine("## Limitations");
            builder.AppendLine();
            builder.AppendLine("* Aucun serveur officiel live.");
            builder.AppendLine("* Aucun endpoint officiel.");
            builder.AppendLine("* Aucune sauvegarde officielle.");
            builder.AppendLine("* Aucune economie officielle.");
            builder.AppendLine("* Aucune armee persistante officielle.");
            builder.AppendLine("* Aucune carte monde et aucun BEE-881.");
            builder.AppendLine();
            builder.AppendLine("## Ready for next brick");
            builder.AppendLine();
            builder.AppendLine("YES");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_072_RUNTIME = YES");
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
                Debug.LogWarning("Could not resize Game View for DEMO-072 capture: " + exception.Message);
            }
        }
    }
}
