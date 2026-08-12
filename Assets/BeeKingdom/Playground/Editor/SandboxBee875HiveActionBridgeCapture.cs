using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee875HiveActionBridgeCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-071_BEE861_880_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-071_BEE861_875_Manifest.md";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE861_875_Report.md";
        private const string StateRequested = "BeeKingdom.Playground.BEE875.Requested";
        private const string StateFrames = "BeeKingdom.Playground.BEE875.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.BEE875.Captured";
        private const string StateIndex = "BeeKingdom.Playground.BEE875.Index";

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
            new CaptureSpec("Etat accepte upgrade dev-only", "BEE875_01_AcceptedUpgrade_1280x720.png", 1280, 720, "honey_storage", "server_bridge_accepted", Vector2.zero, 1.10f),
            new CaptureSpec("Etat refuse ressources insuffisantes", "BEE875_02_RejectedInsufficientResources_1280x720.png", 1280, 720, "honey_storage", "server_bridge_rejected", Vector2.zero, 1.10f),
            new CaptureSpec("Etat pending timer upgrade", "BEE875_03_PendingUpgrade_1280x720.png", 1280, 720, "honey_storage", "server_bridge_pending", Vector2.zero, 1.10f),
            new CaptureSpec("Etat serveur requis local preview", "BEE875_04_ServerRequiredPreview_1280x720.png", 1280, 720, "research", "server_required", new Vector2(-10f, 0f), 1.10f),
            new CaptureSpec("Conflit snapshot futur dev-only", "BEE875_05_StaleSnapshotConflict_1280x720.png", 1280, 720, "guard_post", "stale_snapshot_conflict", new Vector2(-18f, 8f), 1.12f),
            new CaptureSpec("Training queue pending dev-only", "BEE875_06_TrainingPending_1280x720.png", 1280, 720, "guard_post", "training_gardiennes_running", new Vector2(-18f, 8f), 1.12f),
            new CaptureSpec("Portrait action states readable", "BEE875_07_PhonePortraitActionStates_390x844.png", 390, 844, "guard_post", "server_required", new Vector2(-112f, 42f), 1.28f)
        };

        static SandboxBee875HiveActionBridgeCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-071 BEE-861-875 Source")]
        public static void CaptureBee861875Source()
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
                    throw new InvalidOperationException("DEMO-071 screenshot was not written: " + path);
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
                Debug.Log("DEMO-071 BEE-861-875 source captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-071 BEE-861-875 capture failed: " + exception);
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
            builder.AppendLine("# DEMO-071 BEE-861-875 Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Scope");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit uniquement`");
            builder.AppendLine("- BEE couvertes: `861-875`");
            builder.AppendLine("- SERVER-043: `contrats dev-only refletes cote Unity`");
            builder.AppendLine("- Carte monde modifiee: `false`");
            builder.AppendLine("- BEE-881: `bloquee / non implementee`");
            builder.AppendLine("- Serveur officiel live: `false`");
            builder.AppendLine("- Sauvegarde officielle: `false`");
            builder.AppendLine("- Economie officielle: `false`");
            builder.AppendLine("- Armee persistante officielle: `false`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine();
            builder.AppendLine("## Dev Only Bridge Proof");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveDevOnlyBridgeForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Runtime State");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveLoopStateForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Previous Guards Preserved");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveActionLoopForProof()) builder.AppendLine("- " + row);
            foreach (string row in HiveViewProductUiPresenter.PlayableHivePanelPolishForProof()) builder.AppendLine("- " + row);
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_071 = YES");
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A BEE-861 a BEE-875 Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed with recommendations");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("BEE-861 a BEE-875 integrees cote Unity dans la Ruche jouable uniquement. Le runtime reflete les contrats SERVER-043 en mode dev-only, expose les etats action acceptee/refusee/en attente/serveur requis, ajoute une timeline de feedback, un catalogue de refus visible, une preparation snapshot/revision/reconciliation locale et conserve les non-claims serveur.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee875HiveActionBridgeTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee875HiveActionBridgeCapture.cs`");
            builder.AppendLine();
            builder.AppendLine("## Decisions d'architecture");
            builder.AppendLine();
            builder.AppendLine("* Le pont serveur reste un reflet dev-only local, sans endpoint ni autorite officielle.");
            builder.AppendLine("* Les decisions action sont rattachees aux actions locales existantes pour preserver BEE-842 a BEE-850.");
            builder.AppendLine("* Snapshot/revision/reconciliation sont exposes comme preparation de contrat, sans restore ni sauvegarde officielle.");
            builder.AppendLine();
            builder.AppendLine("## APIs publiques ajoutees");
            builder.AppendLine();
            builder.AppendLine("* `HiveViewProductUiPresenter.PlayableHiveDevOnlyBridgeForProof()`");
            builder.AppendLine("* `SandboxBee875HiveActionBridgeTests.RunAllForBatch()`");
            builder.AppendLine("* `SandboxBee875HiveActionBridgeCapture.CaptureBee861875Source()`");
            builder.AppendLine();
            builder.AppendLine("## Preuves source");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("* " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine("* Manifest: `" + ManifestPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("* Tests attendus: `SandboxBee875HiveActionBridgeTests`.");
            builder.AppendLine("* Couverture cible: contrats dev-only, etats action, refus, snapshot/revision, non-claims et garde-fous BEE-842 a BEE-850.");
            builder.AppendLine();
            builder.AppendLine("## Limites");
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
            builder.AppendLine("READY_FOR_DEMO_071 = YES");
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
                Debug.LogWarning("Could not resize Game View for DEMO-071 capture: " + exception.Message);
            }
        }
    }
}
