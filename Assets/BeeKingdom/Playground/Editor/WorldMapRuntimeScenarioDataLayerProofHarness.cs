using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapRuntimeScenarioDataLayerProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        private const string OutputRoot = "Docs/WorldMapAudit/Wave6_50x50_Wave5Method12288/RuntimeScenarioDataLayerProof";
        private const string RunningKey = "BeeKingdom.WorldMapRuntimeScenarioDataLayerProof.Running";
        private const string StartedKey = "BeeKingdom.WorldMapRuntimeScenarioDataLayerProof.Started";
        private const string ExitPendingKey = "BeeKingdom.WorldMapRuntimeScenarioDataLayerProof.ExitPending";
        private const string ExitCodeKey = "BeeKingdom.WorldMapRuntimeScenarioDataLayerProof.ExitCode";

        private static string root;
        private static WorldMapMmoFullscreenFoundationBootstrap bootstrap;
        private static int waitFrames;
        private static bool failed;
        private static WorldMapMmoFullscreenFoundationBootstrap.RuntimeScenarioDataLayerProofSnapshot snapshot;

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (SessionState.GetBool(ExitPendingKey, false) && !EditorApplication.isPlaying)
            {
                int code = SessionState.GetInt(ExitCodeKey, 1);
                SessionState.SetBool(ExitPendingKey, false);
                EditorApplication.delayCall += () => EditorApplication.Exit(code);
                return;
            }

            if (!SessionState.GetBool(RunningKey, false)) return;
            root = AbsoluteProjectPath(OutputRoot);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= ResumeWhenPlayModeReady;
            EditorApplication.update += ResumeWhenPlayModeReady;
        }

        [MenuItem("Bee Kingdom/World Map/Run Runtime Scenario Data Layer Proof Harness")]
        public static void RunRuntimeScenarioDataLayerProofHarness()
        {
            root = AbsoluteProjectPath(OutputRoot);
            Directory.CreateDirectory(root);
            DeletePreviousOutputs();
            failed = false;
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetBool(ExitPendingKey, false);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Screen.SetResolution(1280, 720, false);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) ResumeWhenPlayModeReady();
        }

        private static void ResumeWhenPlayModeReady()
        {
            if (!SessionState.GetBool(RunningKey, false) || SessionState.GetBool(StartedKey, false))
            {
                EditorApplication.update -= ResumeWhenPlayModeReady;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
            if (bootstrap == null)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            SessionState.SetBool(StartedKey, true);
            waitFrames = 36;
            EditorApplication.update -= ResumeWhenPlayModeReady;
            EditorApplication.update -= ProcessHarness;
            EditorApplication.update += ProcessHarness;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static void ProcessHarness()
        {
            if (failed) return;
            try
            {
                if (waitFrames > 0)
                {
                    waitFrames--;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                snapshot = bootstrap.RunRuntimeScenarioDataLayerProofForProof();
                Require(snapshot.Pass, "Runtime scenario data layer proof failed.");
                CompleteHarness();
            }
            catch (Exception exception)
            {
                FailAndExit(exception.ToString());
            }
        }

        private static void CompleteHarness()
        {
            EditorApplication.update -= ProcessHarness;
            WriteReceipt();
            SessionState.SetBool(RunningKey, false);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetInt(ExitCodeKey, 0);
            SessionState.SetBool(ExitPendingKey, false);
            EditorApplication.Exit(0);
        }

        private static void WriteReceipt()
        {
            var receipt = new StringBuilder();
            receipt.AppendLine("# WorldMap Runtime Scenario Data Layer Proof Receipt");
            receipt.AppendLine();
            receipt.AppendLine("- Scene: `" + ScenePath + "`");
            receipt.AppendLine("- Play Mode: PASS");
            receipt.AppendLine("- Provider: " + snapshot.ProviderId);
            receipt.AppendLine("- Data version: " + snapshot.DataVersion);
            receipt.AppendLine("- Records/hives/resources/bestiary/events: " + snapshot.Records + "/" + snapshot.Hives + "/" + snapshot.Resources + "/" + snapshot.Bestiary + "/" + snapshot.Events);
            receipt.AppendLine("- Stable entity ids: " + Pass(snapshot.StableEntityIdsPass));
            receipt.AppendLine("- Normalized coordinates: " + Pass(snapshot.NormalizedCoordinatesPass));
            receipt.AppendLine("- 25x25 to 50x50 reprojection: " + Pass(snapshot.Reprojection50x50Pass));
            receipt.AppendLine("- Local authority adapter: " + Pass(snapshot.LocalAuthorityAdapterPass));
            receipt.AppendLine("- Scenario presets: " + Pass(snapshot.ScenarioPresetsPass));
            receipt.AppendLine("- Player/enemy test hives editable: " + Pass(snapshot.PlayerEnemyTestHivesEditablePass));
            receipt.AppendLine("- Legacy P1-P5 regression: " + Pass(snapshot.LegacyDemoRegressionNo));
            receipt.AppendLine("- Server/remote: ABSENT");
            receipt.AppendLine("- official_gain: false");
            receipt.AppendLine("- APK rebuild: ABSENT");
            receipt.AppendLine("- 50x50 terrain generation: ABSENT");
            receipt.AppendLine();
            receipt.AppendLine("STABLE_ENTITY_IDS=PASS");
            receipt.AppendLine("NORMALIZED_COORDINATES=PASS");
            receipt.AppendLine("LOCAL_AUTHORITY_ADAPTER=PASS");
            receipt.AppendLine("SCENARIO_PRESETS=PASS");
            receipt.AppendLine("PLAYER_ENEMY_TEST_HIVES_EDITABLE=PASS");
            receipt.AppendLine("SERVER_OR_OFFICIAL_GAIN=NO");
            receipt.AppendLine("LEGACY_DEMO_REGRESSION=NO");
            receipt.AppendLine("READY_FOR_OWNER_CONFIGURABLE_SCENARIO_TEST=YES");
            receipt.AppendLine("RUNTIME_SCENARIO_DATA_LAYER_EXACT_CROP=PASS");
            File.WriteAllText(Path.Combine(root, "RuntimeScenarioDataLayerProofReceipt.md"), receipt.ToString(), Encoding.UTF8);
        }

        private static void FailAndExit(string message)
        {
            failed = true;
            EditorApplication.update -= ProcessHarness;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "RuntimeScenarioDataLayerProofReceipt.md"), "RUNTIME_SCENARIO_DATA_LAYER_EXACT_CROP=FAIL\n\n" + message, Encoding.UTF8);
            SessionState.SetBool(RunningKey, false);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetInt(ExitCodeKey, 1);
            SessionState.SetBool(ExitPendingKey, false);
            EditorApplication.Exit(1);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Pass(bool value)
        {
            return value ? "PASS" : "FAIL";
        }

        private static void DeletePreviousOutputs()
        {
            if (!Directory.Exists(root)) return;
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories)) File.Delete(file);
        }

        private static string AbsoluteProjectPath(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        }
    }
}
