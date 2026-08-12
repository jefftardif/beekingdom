using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee833RightPanelPolishCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-069_BEE832_840_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-069_BEE832_833_Manifest.md";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE832_833_Report.md";
        private const string StateRequested = "BeeKingdom.Playground.BEE833.Requested";
        private const string StateFrames = "BeeKingdom.Playground.BEE833.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.BEE833.Captured";
        private const string StateIndex = "BeeKingdom.Playground.BEE833.Index";

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
            new CaptureSpec("Tablet landscape less dense right panel", "BEE833_01_TabletLandscape_RightPanelPolish_1280x720.png", 1280, 720, "honey_storage", "idle", Vector2.zero, 1.10f),
            new CaptureSpec("Disabled reason placed under action", "BEE833_02_DisabledReasonNearAction_1280x720.png", 1280, 720, "honey_storage", "upgrade_blocked", Vector2.zero, 1.10f),
            new CaptureSpec("Training compact queue product panel", "BEE833_03_TrainingCompactQueue_1280x720.png", 1280, 720, "guard_post", "training_running", new Vector2(-18f, 8f), 1.12f),
            new CaptureSpec("Upgrade running progress product panel", "BEE833_04_UpgradeRunningProgress_1280x720.png", 1280, 720, "honey_storage", "upgrade_running", Vector2.zero, 1.10f),
            new CaptureSpec("Phone portrait readable disabled reason", "BEE833_05_PhonePortraitPanelReason_390x844.png", 390, 844, "guard_post", "training_running", new Vector2(-112f, 42f), 1.28f)
        };

        static SandboxBee833RightPanelPolishCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-069 BEE-832-833 Source")]
        public static void CaptureBee832833Source()
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
                    throw new InvalidOperationException("DEMO-069 screenshot was not written: " + path);
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
                Debug.Log("DEMO-069 BEE-832-833 source captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-069 BEE-832-833 capture failed: " + exception);
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
            builder.AppendLine("# DEMO-069 BEE-832-833 Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Scope");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit preview`");
            builder.AppendLine("- BEE couvertes: `832-833 uniquement`");
            builder.AppendLine("- BEE-834+: `non implemente par Builder-A`");
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
            builder.AppendLine("## Right Panel Polish");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHivePanelPolishForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Previous Guards Preserved");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveButtonStateMatrixForProof()) builder.AppendLine("- " + row);
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveClarityForProof()) builder.AppendLine("- " + row);
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_069 = YES");
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A BEE-832 et BEE-833 Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed with recommendations");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("BEE-832 et BEE-833 implementees cote Unity pour alleger le panneau droit produit et replacer les raisons disabled/locked/blocked dans le flux de lecture normal, immediatement sous l'action principale. Les acquis BEE-828 a BEE-831 sont preserves : boutons non muets, feedback ressource, amelioration claire, entrainement clair et garde anti double action. Aucun travail BEE-834+, carte monde, serveur live, sauvegarde officielle, economie officielle ou armee persistante officielle.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee833RightPanelPolishTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee833RightPanelPolishCapture.cs`");
            builder.AppendLine();
            builder.AppendLine("## Preuves source");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("* " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine("* Manifest: `" + ManifestPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Tests attendus");
            builder.AppendLine();
            builder.AppendLine("* `SandboxBee833RightPanelPolishTests.RightPanelPolishKeepsEssentialDataAndSinglePrimaryAction`");
            builder.AppendLine("* `SandboxBee833RightPanelPolishTests.DisabledReasonPlacementStaysNearActionAndReadable`");
            builder.AppendLine("* `SandboxBee833RightPanelPolishTests.PreviousPlayableHiveGuardsRemainIntact`");
            builder.AppendLine();
            builder.AppendLine("## Limites non-live");
            builder.AppendLine();
            builder.AppendLine("* Simulation locale de demonstration uniquement.");
            builder.AppendLine("* Aucune progression serveur officielle.");
            builder.AppendLine("* Aucune sauvegarde officielle.");
            builder.AppendLine("* Aucune economie officielle.");
            builder.AppendLine("* Aucune armee persistante officielle.");
            builder.AppendLine("* BEE-834+ non implementee par Builder-A dans cette tranche.");
            builder.AppendLine();
            builder.AppendLine("## READY_FOR_DEMO_069");
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

            Debug.Log("DEMO-069 capture profile: " + label + " " + width + "x" + height);
        }
    }
}
