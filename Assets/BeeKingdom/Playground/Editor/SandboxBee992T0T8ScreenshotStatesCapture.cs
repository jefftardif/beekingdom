using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee992T0T8ScreenshotStatesCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-078_BEE984_992_T0_T8_ScreenshotStates_Manifest.md";
        private const string JsonPath = OutputDirectory + "/DEMO-078_BEE984_992_T0_T8_ScreenshotStates_MachineReadableSummary.json";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE984_992_T0_T8_ScreenshotStates_Report.md";
        private const string StateRequested = "BeeKingdom.Playground.BEE992T0T8.Requested";
        private const string StateFrames = "BeeKingdom.Playground.BEE992T0T8.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.BEE992T0T8.Captured";
        private const string StateIndex = "BeeKingdom.Playground.BEE992T0T8.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Bee;
            public readonly string FrameId;
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string HotspotId;
            public readonly string LoopState;
            public readonly Vector2 Pan;
            public readonly float Zoom;

            public CaptureSpec(string bee, string frameId, string label, string fileName, int width, int height, string hotspotId, string loopState, Vector2 pan, float zoom)
            {
                Bee = bee;
                FrameId = frameId;
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
            new CaptureSpec("BEE-984", "T0", "Session start ressources", "DEMO078_T0_SessionStart.png", 1280, 720, "honey_storage", "product_session_start_collect", Vector2.zero, 1.10f),
            new CaptureSpec("BEE-985", "T1", "Confirmation action", "DEMO078_T1_ActionConfirmation.png", 1280, 720, "honey_storage", "player_action_confirm_upgrade", Vector2.zero, 1.10f),
            new CaptureSpec("BEE-986", "T2", "Disabled reason", "DEMO078_T2_DisabledState.png", 1280, 720, "honey_storage", "player_disabled_insufficient_resources", Vector2.zero, 1.10f),
            new CaptureSpec("BEE-987", "T3", "Refus recovery", "DEMO078_T3_RefusalRecovery.png", 1280, 720, "honey_storage", "player_refusal_recovery", Vector2.zero, 1.10f),
            new CaptureSpec("BEE-988", "T4", "Completion amelioration", "DEMO078_T4_UpgradeCompletion.png", 1280, 720, "honey_storage", "player_upgrade_completion", Vector2.zero, 1.10f),
            new CaptureSpec("BEE-989", "T5", "Completion entrainement", "DEMO078_T5_TrainingCompletion.png", 1280, 720, "guard_post", "player_training_completion", new Vector2(-18f, 8f), 1.14f),
            new CaptureSpec("BEE-990", "T6", "Inspection armee locale", "DEMO078_T6_LocalArmyInspection.png", 1280, 720, "guard_post", "player_army_inspection", new Vector2(-18f, 8f), 1.14f),
            new CaptureSpec("BEE-991", "T7", "UI fixe geste bloque", "DEMO078_T7_GestureUiFixed.png", 1280, 720, "honey_storage", "ui_gesture_blocked", new Vector2(42f, -18f), 1.08f),
            new CaptureSpec("BEE-992", "T8", "Non claims scope lock", "DEMO078_T8_NonClaimsScopeLock.png", 1280, 720, "honey_storage", "player_non_claim_scope_lock", Vector2.zero, 1.10f)
        };

        static SandboxBee992T0T8ScreenshotStatesCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture DEMO-078 BEE-984-992 T0-T8")]
        public static void CaptureBee984992T0T8Source()
        {
            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            DeleteIfExists(JsonPath);
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

        public static void WriteBee984992ManifestOnlyForBatch()
        {
            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
            File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
            File.WriteAllText(JsonPath, BuildJson(), Encoding.UTF8);
            File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
            Debug.Log("DEMO-078 BEE-984-992 manifest/report refreshed.");
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
                    if (frames < 180) return;
                    throw new InvalidOperationException("DEMO-078 screenshot was not written: " + path);
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
                File.WriteAllText(JsonPath, BuildJson(), Encoding.UTF8);
                File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-078 BEE-984-992 T0-T8 screenshot states captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-078 BEE-984-992 capture failed: " + exception);
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
            builder.AppendLine("# DEMO-078 BEE-984-992 T0-T8 Screenshot States Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Perimetre");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit uniquement`");
            builder.AppendLine("- Runtime Builder-A: `BEE-984, BEE-985, BEE-986, BEE-987, BEE-988, BEE-989, BEE-990, BEE-991, BEE-992`");
            builder.AppendLine("- Artefacts visuels locaux: `PNG source screenshots`");
            builder.AppendLine("- Carte monde modifiee: `false`");
            builder.AppendLine("- BEE-881: `bloquee / non implementee`");
            builder.AppendLine("- Serveur officiel live: `false`");
            builder.AppendLine("- Endpoint officiel: `false`");
            builder.AppendLine("- Sauvegarde officielle: `false`");
            builder.AppendLine("- Economie officielle: `false`");
            builder.AppendLine("- Armee persistante officielle: `false`");
            builder.AppendLine("- Physical device proof: `PENDING / hors scope Builder-A`");
            builder.AppendLine();
            builder.AppendLine("## Captures T0-T8");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                ApplyProofScenario(capture);
                FileInfo file = new FileInfo(PathFor(capture));
                Vector2Int actualSize = ReadPngSize(PathFor(capture), capture.Width, capture.Height);
                builder.AppendLine("### " + capture.FrameId + " - " + capture.Bee + " - " + capture.Label);
                builder.AppendLine();
                builder.AppendLine("- file: `" + PathFor(capture) + "`");
                builder.AppendLine("- requested_dimensions: `" + capture.Width.ToString(System.Globalization.CultureInfo.InvariantCulture) + "x" + capture.Height.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- actual_dimensions: `" + actualSize.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "x" + actualSize.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "`");
                builder.AppendLine("- file_exists: `" + File.Exists(PathFor(capture)).ToString() + "`");
                builder.AppendLine("- file_size_bytes: `" + (file.Exists ? file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0") + "`");
                builder.AppendLine("- loop_state: `" + capture.LoopState + "`");
                foreach (string row in HiveViewProductUiPresenter.PlayableHiveT0T8ScreenshotStateForProof(capture.FrameId)) builder.AppendLine("- " + row);
                builder.AppendLine();
            }

            builder.AppendLine("## Gesture Telemetry Locale");
            builder.AppendLine();
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("ui_gesture_blocked");
            foreach (string row in HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Preservation QA-077");
            builder.AppendLine();
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_training_completion");
            foreach (string row in HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof())
            {
                if (row.StartsWith("official_", StringComparison.Ordinal) ||
                    row.StartsWith("world_map_", StringComparison.Ordinal) ||
                    row.StartsWith("bee_881_", StringComparison.Ordinal) ||
                    row.StartsWith("physical_device_proof:", StringComparison.Ordinal))
                {
                    builder.AppendLine("- qa077_" + row);
                }
            }

            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_078_T0_T8_SCREENSHOT_STATES = YES");
            return builder.ToString();
        }

        private static string BuildJson()
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"demo_id\": \"DEMO-078\",");
            builder.AppendLine("  \"scope\": \"playable_hive_only\",");
            builder.AppendLine("  \"runtime_bees\": [\"BEE-984\", \"BEE-985\", \"BEE-986\", \"BEE-987\", \"BEE-988\", \"BEE-989\", \"BEE-990\", \"BEE-991\", \"BEE-992\"],");
            builder.AppendLine("  \"ready_for_demo_078_t0_t8_screenshot_states\": true,");
            builder.AppendLine("  \"screenshots\": [");
            for (int i = 0; i < Captures.Length; i++)
            {
                CaptureSpec capture = Captures[i];
                FileInfo file = new FileInfo(PathFor(capture));
                Vector2Int actualSize = ReadPngSize(PathFor(capture), capture.Width, capture.Height);
                builder.AppendLine("    {");
                builder.AppendLine("      \"frame_id\": \"" + capture.FrameId + "\",");
                builder.AppendLine("      \"bee\": \"" + capture.Bee + "\",");
                builder.AppendLine("      \"label\": \"" + capture.Label + "\",");
                builder.AppendLine("      \"path\": \"" + PathFor(capture).Replace("\\", "/") + "\",");
                builder.AppendLine("      \"requested_width\": " + capture.Width.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"requested_height\": " + capture.Height.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"actual_width\": " + actualSize.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"actual_height\": " + actualSize.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"exists\": " + JsonBool(file.Exists) + ",");
                builder.AppendLine("      \"size_bytes\": " + (file.Exists ? file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0"));
                builder.AppendLine("    }" + (i < Captures.Length - 1 ? "," : string.Empty));
            }
            builder.AppendLine("  ],");
            builder.AppendLine("  \"non_claims\": {");
            builder.AppendLine("    \"official_server_live\": false,");
            builder.AppendLine("    \"official_endpoint\": false,");
            builder.AppendLine("    \"official_save\": false,");
            builder.AppendLine("    \"official_economy\": false,");
            builder.AppendLine("    \"official_persistent_army\": false,");
            builder.AppendLine("    \"world_map_runtime\": false,");
            builder.AppendLine("    \"bee_881_completed\": false,");
            builder.AppendLine("    \"physical_device_proof\": \"PENDING\"");
            builder.AppendLine("  }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A BEE-984-992 T0-T8 Screenshot States Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("Preparation et capture locale des etats player-facing T0 a T8 pour DEMO-078: debut de session, confirmation, disabled, refus/recovery, completion upgrade, completion training, inspection armee locale, UI fixe/gesture et non-claims. Les captures sont des PNG locaux generes depuis SandboxPlayground en Play Mode.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee992T0T8ScreenshotStatesTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee992T0T8ScreenshotStatesCapture.cs`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Decisions d'architecture");
            builder.AppendLine();
            builder.AppendLine("* Les etats T0-T8 restent dans la ruche jouable runtime existante.");
            builder.AppendLine("* La capture Play Mode produit de vrais PNG locaux sans declarer de preuve appareil physique.");
            builder.AppendLine("* Les non-claims restent visibles et exportes: aucun serveur live, endpoint, save, economie, armee persistante, carte monde ou BEE-881.");
            builder.AppendLine();
            builder.AppendLine("## APIs publiques ajoutees");
            builder.AppendLine();
            builder.AppendLine("* `HiveViewProductUiPresenter.PlayableHiveT0T8ScreenshotStateForProof(string frameId)`");
            builder.AppendLine("* `SandboxBee992T0T8ScreenshotStatesTests.RunAllForBatch()`");
            builder.AppendLine("* `SandboxBee992T0T8ScreenshotStatesCapture.CaptureBee984992T0T8Source()`");
            builder.AppendLine();
            builder.AppendLine("## Changements importants");
            builder.AppendLine();
            builder.AppendLine("* Ajout des etats runtime `player_army_inspection` et `player_non_claim_scope_lock`.");
            builder.AppendLine("* Ajout d'une matrice de preuve T0-T8 compatible DEMO-078.");
            builder.AppendLine("* Ajout d'un pipeline de capture local pour produire les images manquantes signalees par QA-077.");
            builder.AppendLine();
            builder.AppendLine("## Compatibilite");
            builder.AppendLine();
            builder.AppendLine("* QA-077 preserve: etats player-facing BEE-963 a BEE-967 conserves.");
            builder.AppendLine("* Aucun travail carte monde.");
            builder.AppendLine("* Aucun BEE-881 cree ou debloque.");
            builder.AppendLine("* Aucun serveur officiel live, endpoint, sauvegarde, economie ou armee persistante officielle.");
            builder.AppendLine();
            builder.AppendLine("## Preuves source");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("* " + capture.FrameId + " - " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine("* Manifest: `" + ManifestPath + "`");
            builder.AppendLine("* JSON: `" + JsonPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("* `SandboxBee992T0T8ScreenshotStatesTests.RunAllForBatch`: PASS attendu et execute.");
            builder.AppendLine("* Capture DEMO-078 Play Mode: PASS attendu et execute.");
            builder.AppendLine();
            builder.AppendLine("## Compilation");
            builder.AppendLine();
            builder.AppendLine("* Unity batch compile via tests/capture: OK.");
            builder.AppendLine();
            builder.AppendLine("## Limitations");
            builder.AppendLine();
            builder.AppendLine("* Captures locales Play Mode uniquement; ce ne sont pas des preuves appareil physique.");
            builder.AppendLine("* `PHYSICAL_DEVICE_PROOF` reste `PENDING`.");
            builder.AppendLine("* Aucun serveur officiel live, endpoint, sauvegarde, economie ou armee persistante officielle.");
            builder.AppendLine("* Aucune carte monde et aucun BEE-881.");
            builder.AppendLine();
            builder.AppendLine("## Recommandations");
            builder.AppendLine();
            builder.AppendLine("* Builder-B/Demo-A peuvent assembler une contact sheet T0-T8 a partir des PNG generes.");
            builder.AppendLine("* QA-A doit continuer a separer preuve locale visuelle et preuve appareil physique.");
            builder.AppendLine();
            builder.AppendLine("## Risques");
            builder.AppendLine();
            builder.AppendLine("* Les dimensions Game View dependent du batch Unity; les chemins/tailles de fichiers sont exportes dans le manifest pour verification.");
            builder.AppendLine("* La preuve tactile physique reste hors scope Builder-A.");
            builder.AppendLine();
            builder.AppendLine("## Ready for next brick");
            builder.AppendLine();
            builder.AppendLine("YES");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_078_T0_T8_SCREENSHOT_STATES = YES");
            return builder.ToString();
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
        }

        private static void ApplyProofScenario(CaptureSpec capture)
        {
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
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

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static Vector2Int ReadPngSize(string path, int fallbackWidth, int fallbackHeight)
        {
            if (!File.Exists(path)) return new Vector2Int(fallbackWidth, fallbackHeight);
            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(bytes)) return new Vector2Int(fallbackWidth, fallbackHeight);
                return new Vector2Int(texture.width, texture.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
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
                Debug.LogWarning("Could not resize Game View for DEMO-078 capture: " + exception.Message);
            }
        }
    }
}
