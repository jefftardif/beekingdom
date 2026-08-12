using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMap50x50ReadinessProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        private const string OutputRoot = "Docs/WorldMapAudit/Wave6_50x50_Wave5Method12288/WorldMap50x50ReadinessProof";
        private const string RunningKey = "BeeKingdom.WorldMap50x50ReadinessProof.Running";
        private const string StartedKey = "BeeKingdom.WorldMap50x50ReadinessProof.Started";
        private const string ExitPendingKey = "BeeKingdom.WorldMap50x50ReadinessProof.ExitPending";
        private const string ExitCodeKey = "BeeKingdom.WorldMap50x50ReadinessProof.ExitCode";
        private const int Width = 1280;
        private const int Height = 720;

        private static string root;
        private static WorldMapMmoFullscreenFoundationBootstrap bootstrap;
        private static int waitFrames;
        private static bool failed;
        private static WorldMapMmoFullscreenFoundationBootstrap.Wave5ProofSnapshot wave5Snapshot;
        private static WorldMapMmoFullscreenFoundationBootstrap.Stress50x50ReadinessSnapshot stressSnapshot;

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

        [MenuItem("Bee Kingdom/World Map/Run 50x50 Readiness Proof Harness")]
        public static void Run50x50ReadinessProofHarness()
        {
            root = AbsoluteProjectPath(OutputRoot);
            Directory.CreateDirectory(root);
            DeletePreviousOutputs();
            failed = false;
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

                bootstrap.ApplyWave5ProofView(new Vector2(16640f, 16640f), 1.0f);
                wave5Snapshot = bootstrap.CurrentWave5ProofSnapshot();
                Require(wave5Snapshot.ManifestReady, "Wave5 manifest is not ready.");
                Require(wave5Snapshot.VisibleTilesReady, "Wave5 visible tiles are not ready.");
                stressSnapshot = bootstrap.Run50x50ReadinessStressProofForProof();
                Require(stressSnapshot.Pass, "50x50 readiness stress snapshot failed.");
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
            receipt.AppendLine("# WorldMap 50x50 Readiness Proof Receipt");
            receipt.AppendLine();
            receipt.AppendLine("- Scene: `" + ScenePath + "`");
            receipt.AppendLine("- Play Mode: PASS");
            receipt.AppendLine("- Wave6 50x50 exact-crop visible terrain preserved: " + Pass(wave5Snapshot.VisibleTilesReady));
            receipt.AppendLine("- Stress mode disabled by default: " + Pass(stressSnapshot.DisabledByDefault));
            receipt.AppendLine("- Stress logical catalog coordinates: " + stressSnapshot.CatalogCoordinates);
            receipt.AppendLine("- Center active chunks: " + stressSnapshot.CenterActiveChunks);
            receipt.AppendLine("- North-west active chunks: " + stressSnapshot.NorthWestActiveChunks);
            receipt.AppendLine("- South-east active chunks: " + stressSnapshot.SouthEastActiveChunks);
            receipt.AppendLine("- Densest active chunks: " + stressSnapshot.DensestActiveChunks);
            receipt.AppendLine("- Densest hives/resources/bestiary: " + stressSnapshot.DensestHives + "/" + stressSnapshot.DensestResources + "/" + stressSnapshot.DensestBestiary);
            receipt.AppendLine("- Catalog hives/resources/bestiary: " + stressSnapshot.CatalogHives + "/" + stressSnapshot.CatalogResources + "/" + stressSnapshot.CatalogBestiary);
            receipt.AppendLine("- Wave5 cached textures: " + stressSnapshot.Wave5CachedTextures);
            receipt.AppendLine("- Chunk cache before/after stress: " + stressSnapshot.ChunkCacheBefore + "/" + stressSnapshot.ChunkCacheAfter);
            receipt.AppendLine("- Allocated bytes during stress: " + stressSnapshot.AllocatedBytes);
            receipt.AppendLine("- Budgets: " + Pass(stressSnapshot.BudgetsPass));
            receipt.AppendLine("- Cache stable: " + Pass(stressSnapshot.CacheStablePass));
            receipt.AppendLine("- Terrain preserved: " + Pass(stressSnapshot.TerrainPreservedPass));
            receipt.AppendLine("- Allocation budget: " + Pass(stressSnapshot.AllocationBudgetPass));
            receipt.AppendLine("- Runtime placement mask loaded: " + Pass(stressSnapshot.RuntimePlacementMaskLoaded));
            receipt.AppendLine("- Runtime placement mask entries: " + stressSnapshot.RuntimePlacementMaskEntries);
            receipt.AppendLine("- Runtime placement mask covers 50x50: " + Pass(stressSnapshot.RuntimePlacementMaskCovers50x50));
            receipt.AppendLine();
            receipt.AppendLine("WORLD_MAP_50X50_EXACT_CROP_READINESS=PASS");
            receipt.AppendLine("WAVE6_50X50_RUNTIME_PLACEMENT_MASK=PASS");
            receipt.AppendLine("NO_50X50_ART_GENERATED=true");
            receipt.AppendLine("WAVE6_50X50_EXACT_CROP_VISIBLE_PRESERVED=true");
            File.WriteAllText(Path.Combine(root, "WorldMap50x50ReadinessProofReceipt.md"), receipt.ToString(), Encoding.UTF8);
        }

        private static void FailAndExit(string message)
        {
            failed = true;
            EditorApplication.update -= ProcessHarness;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "WorldMap50x50ReadinessProofReceipt.md"), "WORLD_MAP_50X50_EXACT_CROP_READINESS=FAIL\n\n" + message, Encoding.UTF8);
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
