using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapMapReadingToolsProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        private const string OutputRoot = "Docs/WorldMapAudit/Wave6_50x50_Wave5Method12288/MapReadingToolsProof";
        private const string RunningKey = "BeeKingdom.WorldMapMapReadingToolsProof.Running";
        private const string StartedKey = "BeeKingdom.WorldMapMapReadingToolsProof.Started";
        private const string ExitPendingKey = "BeeKingdom.WorldMapMapReadingToolsProof.ExitPending";
        private const string ExitCodeKey = "BeeKingdom.WorldMapMapReadingToolsProof.ExitCode";

        private static string root;
        private static WorldMapMmoFullscreenFoundationBootstrap bootstrap;
        private static int waitFrames;
        private static bool failed;
        private static WorldMapMmoFullscreenFoundationBootstrap.MapReadingToolsProofSnapshot snapshot;

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

        [MenuItem("Bee Kingdom/World Map/Run Map Reading Tools Proof Harness")]
        public static void RunMapReadingToolsProofHarness()
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
            waitFrames = 24;
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

                snapshot = bootstrap.RunMapReadingToolsProofForProof();
                Require(snapshot.Pass, "Map reading tools proof failed.");
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
            receipt.AppendLine("# WorldMap Map Reading Tools Proof Receipt");
            receipt.AppendLine();
            receipt.AppendLine("- Scene: `" + ScenePath + "`");
            receipt.AppendLine("- Play Mode: PASS");
            receipt.AppendLine("- Nearest node selection: " + Pass(snapshot.NearestSelectionPass));
            receipt.AppendLine("- Filters hives/resources/threats/BearDen: " + Pass(snapshot.FiltersPass));
            receipt.AppendLine("- Fixed HUD rectangle: " + Pass(snapshot.FixedHudPass));
            receipt.AppendLine("- Terrain unmasked by default: " + Pass(snapshot.TerrainUnmaskedPass));
            receipt.AppendLine("- Legend tiers/richness: " + Pass(snapshot.LegendPass));
            receipt.AppendLine("- Status: " + snapshot.Status);
            receipt.AppendLine();
            receipt.AppendLine("MAP_READING_TOOLS_EXACT_CROP_RUNTIME=PASS");
            File.WriteAllText(Path.Combine(root, "MapReadingToolsProofReceipt.md"), receipt.ToString(), Encoding.UTF8);
        }

        private static void FailAndExit(string message)
        {
            failed = true;
            EditorApplication.update -= ProcessHarness;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "MapReadingToolsProofReceipt.md"), "MAP_READING_TOOLS_EXACT_CROP_RUNTIME=FAIL\n\n" + message, Encoding.UTF8);
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
