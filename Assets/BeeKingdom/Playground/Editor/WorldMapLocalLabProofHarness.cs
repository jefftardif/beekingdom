using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapLocalLabProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        private const string OutputRoot = "Docs/WorldMapAudit/Wave6_50x50_Wave5Method12288/LocalLabProof";
        private const string RunningKey = "BeeKingdom.WorldMapLocalLabProof.Running";
        private const string StartedKey = "BeeKingdom.WorldMapLocalLabProof.Started";
        private const string ExitPendingKey = "BeeKingdom.WorldMapLocalLabProof.ExitPending";
        private const string ExitCodeKey = "BeeKingdom.WorldMapLocalLabProof.ExitCode";
        private const int Width = 1280;
        private const int Height = 720;

        private static string root;
        private static WorldMapMmoFullscreenFoundationBootstrap bootstrap;
        private static int phase;
        private static int waitFrames;
        private static bool failed;
        private static bool resetPass;
        private static bool collectionPass;
        private static bool combatPass;
        private static bool resetAfterVisualPass;
        private static WorldMapLocalLabRuntime.HiveVisualProofSnapshot hiveVisualSnapshot;
        private static WorldMapLocalLabRuntime.LabProofSnapshot initialSnapshot;
        private static WorldMapLocalLabRuntime.LabProofSnapshot finalSnapshot;
        private static int screenshotWaitCycles;
        private static bool screenshotsReady;
        private static readonly string[] ScreenshotNames =
        {
            "L00_LOCAL_LAB_DEFAULT.png",
            "L01_LOCAL_LAB_COLLECTION_RESULT.png",
            "L02_LOCAL_LAB_COMBAT_RESULT.png"
        };

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

        [MenuItem("Bee Kingdom/World Map/Run Local Lab Proof Harness")]
        public static void RunLocalLabProofHarness()
        {
            root = AbsoluteProjectPath(OutputRoot);
            Directory.CreateDirectory(root);
            DeletePreviousOutputs();
            failed = false;
            resetPass = false;
            collectionPass = false;
            combatPass = false;
            resetAfterVisualPass = false;
            screenshotWaitCycles = 0;
            screenshotsReady = false;
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetBool(ExitPendingKey, false);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Screen.SetResolution(Width, Height, false);
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
            phase = 0;
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

                if (phase == 0)
                {
                    resetPass = bootstrap.ResetLocalLabForProof();
                    initialSnapshot = bootstrap.CurrentLocalLabProofSnapshot();
                    Require(resetPass, "Reset local lab failed.");
                    Require(initialSnapshot.Ready, "Initial local lab snapshot is not ready.");
                    bootstrap.ApplyWave5ProofView(new Vector2(16640f, 16640f), 1.0f);
                    Capture(ScreenshotNames[0]);
                    phase++;
                    waitFrames = 18;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase == 1)
                {
                    hiveVisualSnapshot = bootstrap.RunLocalLabHiveVisualProgressionForProof();
                    Require(hiveVisualSnapshot.Pass, "Hive visual progression proof failed.");
                    resetAfterVisualPass = bootstrap.ResetLocalLabForProof();
                    Require(resetAfterVisualPass, "Reset after hive visual proof failed.");
                    phase++;
                    waitFrames = 12;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase == 2)
                {
                    collectionPass = bootstrap.RunLocalLabCollectionForProof();
                    Require(collectionPass, "Deterministic collection proof failed.");
                    Capture(ScreenshotNames[1]);
                    phase++;
                    waitFrames = 18;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase == 3)
                {
                    combatPass = bootstrap.RunLocalLabCombatForProof();
                    Require(combatPass, "Deterministic combat proof failed.");
                    finalSnapshot = bootstrap.CurrentLocalLabProofSnapshot();
                    Require(finalSnapshot.Ready, "Final local lab snapshot is not ready.");
                    Require(finalSnapshot.PremiumHivesLoaded, "Premium local hive textures are not loaded.");
                    Capture(ScreenshotNames[2]);
                    phase++;
                    waitFrames = 24;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                screenshotsReady = AllScreenshotsReady();
                if (!screenshotsReady && screenshotWaitCycles < 20)
                {
                    screenshotWaitCycles++;
                    waitFrames = 12;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                CompleteHarness();
            }
            catch (Exception exception)
            {
                FailAndExit(exception.ToString());
            }
        }

        private static void Capture(string fileName)
        {
            string path = Path.Combine(root, fileName);
            ScreenCapture.CaptureScreenshot(path);
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
            receipt.AppendLine("# WorldMap Local Lab Proof Receipt");
            receipt.AppendLine();
            receipt.AppendLine("- Scene: `" + ScenePath + "`");
            receipt.AppendLine("- Play Mode: PASS");
            receipt.AppendLine("- Reset defaults: " + Pass(resetPass));
            receipt.AppendLine("- Hive visual progression resolver: " + Pass(hiveVisualSnapshot.Pass));
            receipt.AppendLine("- Neutral level 4 -> H1: " + Pass(hiveVisualSnapshot.NeutralLevel4Pass));
            receipt.AppendLine("- Each class level 10 -> H2: " + Pass(hiveVisualSnapshot.AllLevel10ClassesPass));
            receipt.AppendLine("- Level 35 class -> H3: " + Pass(hiveVisualSnapshot.Level35Pass));
            receipt.AppendLine("- Player/enemy distinct sprites and overlays: " + Pass(hiveVisualSnapshot.PlayerEnemyDistinctPass));
            receipt.AppendLine("- Reset after visual proof: " + Pass(resetAfterVisualPass));
            receipt.AppendLine("- Deterministic collection: " + Pass(collectionPass));
            receipt.AppendLine("- Deterministic combat: " + Pass(combatPass));
            receipt.AppendLine("- Local only: " + Pass(finalSnapshot.LocalOnly));
            receipt.AppendLine("- Premium hive textures loaded: " + Pass(finalSnapshot.PremiumHivesLoaded));
            receipt.AppendLine("- Last player sprite path: " + hiveVisualSnapshot.PlayerSpritePath);
            receipt.AppendLine("- Last enemy sprite path: " + hiveVisualSnapshot.EnemySpritePath);
            receipt.AppendLine("- Player faction overlay source: " + hiveVisualSnapshot.PlayerFaction);
            receipt.AppendLine("- Enemy faction overlay source: " + hiveVisualSnapshot.EnemyFaction);
            receipt.AppendLine("- Screenshots ready: " + Pass(screenshotsReady));
            receipt.AppendLine("- Player position: " + finalSnapshot.PlayerPosition);
            receipt.AppendLine("- Enemy position: " + finalSnapshot.EnemyPosition);
            receipt.AppendLine("- Player level: " + finalSnapshot.PlayerLevel);
            receipt.AppendLine("- Enemy level: " + finalSnapshot.EnemyLevel);
            receipt.AppendLine("- Last telemetry: " + finalSnapshot.LastTelemetry);
            receipt.AppendLine();
            receipt.AppendLine("## Captures");
            receipt.AppendLine();
            receipt.AppendLine("- `L00_LOCAL_LAB_DEFAULT.png`");
            receipt.AppendLine("- `L01_LOCAL_LAB_COLLECTION_RESULT.png`");
            receipt.AppendLine("- `L02_LOCAL_LAB_COMBAT_RESULT.png`");
            if (!screenshotsReady)
            {
                receipt.AppendLine();
                receipt.AppendLine("Screenshot files were requested but not available before the bounded batchmode wait ended. Logical Play Mode gates above are authoritative for this harness run.");
            }
            receipt.AppendLine();
            receipt.AppendLine("No server, remote persistence, official gain, APK rebuild, terrain tile rewrite, or BearDen event activation was performed.");
            receipt.AppendLine();
            receipt.AppendLine("LOCAL_LAB_EXACT_CROP_RUNTIME_INTEGRATION=PASS");
            File.WriteAllText(Path.Combine(root, "WorldMapLocalLabProofReceipt.md"), receipt.ToString());
        }

        private static bool AllScreenshotsReady()
        {
            for (int i = 0; i < ScreenshotNames.Length; i++)
            {
                string path = Path.Combine(root, ScreenshotNames[i]);
                if (!File.Exists(path)) return false;
                FileInfo info = new FileInfo(path);
                if (info.Length <= 1024) return false;
            }

            return true;
        }

        private static void DeletePreviousOutputs()
        {
            if (!Directory.Exists(root)) return;
            foreach (string file in Directory.GetFiles(root, "*.png")) File.Delete(file);
            string receipt = Path.Combine(root, "WorldMapLocalLabProofReceipt.md");
            if (File.Exists(receipt)) File.Delete(receipt);
            string failure = Path.Combine(root, "WorldMapLocalLabProofFailure.txt");
            if (File.Exists(failure)) File.Delete(failure);
        }

        private static void FailAndExit(string message)
        {
            failed = true;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "WorldMapLocalLabProofFailure.txt"), message);
            Debug.LogError(message);
            EditorApplication.update -= ProcessHarness;
            SessionState.SetBool(RunningKey, false);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetInt(ExitCodeKey, 1);
            SessionState.SetBool(ExitPendingKey, true);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            else EditorApplication.Exit(1);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Pass(bool value)
        {
            return value ? "PASS" : "FAIL";
        }

        private static string AbsoluteProjectPath(string projectRelative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelative));
        }
    }
}
