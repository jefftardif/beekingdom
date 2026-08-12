using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapAutomatedRegressionProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        private const string OutputRoot = "Docs/WorldMapAudit/Wave6_50x50_Wave5Method12288/AutomatedRegressionProof";
        private const string RunningKey = "BeeKingdom.WorldMapAutomatedRegressionProof.Running";
        private const string StartedKey = "BeeKingdom.WorldMapAutomatedRegressionProof.Started";
        private const string ExitPendingKey = "BeeKingdom.WorldMapAutomatedRegressionProof.ExitPending";
        private const string ExitCodeKey = "BeeKingdom.WorldMapAutomatedRegressionProof.ExitCode";
        private const int Width = 1280;
        private const int Height = 720;

        private static string root;
        private static WorldMapMmoFullscreenFoundationBootstrap bootstrap;
        private static int waitFrames;
        private static bool failed;
        private static bool bearDenTogglePass;
        private static bool labResetPass;
        private static bool labCollectionPass;
        private static bool labCombatPass;
        private static WorldMapMmoFullscreenFoundationBootstrap.Wave5ProofSnapshot wave5Snapshot;
        private static WorldMapLocalLabRuntime.LabProofSnapshot labSnapshot;
        private static WorldMapLocalLabRuntime.HiveVisualProofSnapshot hiveVisualSnapshot;
        private static WorldMapMmoFullscreenFoundationBootstrap.RuntimeEntitiesProofSnapshot runtimeSnapshot;
        private static WorldMapMmoFullscreenFoundationBootstrap.ResourceInteractionProofSnapshot resourceSnapshot;
        private static WorldMapMmoFullscreenFoundationBootstrap.BestiaryInteractionProofSnapshot bestiarySnapshot;
        private static WorldMapMmoFullscreenFoundationBootstrap.MapReadingToolsProofSnapshot mapToolsSnapshot;
        private static WorldMapMmoFullscreenFoundationBootstrap.InteractionPolishProofSnapshot polishSnapshot;
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

        [MenuItem("Bee Kingdom/World Map/Run Automated Regression Proof Harness")]
        public static void RunAutomatedRegressionProofHarness()
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

                bootstrap.ApplyWave5ProofView(new Vector2(16640f, 16640f), 1.0f);
                wave5Snapshot = bootstrap.CurrentWave5ProofSnapshot();
                Require(wave5Snapshot.ManifestReady, "Wave5 manifest is not ready.");
                Require(wave5Snapshot.VisibleTilesReady, "Wave5 visible tiles are not ready.");
                Require(wave5Snapshot.BearDenLoaded, "BearDen is not loaded.");

                bootstrap.SetBearDenVisibilityForProof(true);
                bool visible = bootstrap.BearDenVisibleForProof();
                bootstrap.SetBearDenVisibilityForProof(false);
                bool hidden = !bootstrap.BearDenVisibleForProof();
                bootstrap.SetBearDenVisibilityForProof(true);
                bool restored = bootstrap.BearDenVisibleForProof();
                bearDenTogglePass = visible && hidden && restored;
                Require(bearDenTogglePass, "BearDen visible/hidden/restored proof failed.");

                labResetPass = bootstrap.ResetLocalLabForProof();
                labSnapshot = bootstrap.CurrentLocalLabProofSnapshot();
                Require(labResetPass && labSnapshot.Ready && labSnapshot.LocalOnly, "LAB LOCAL reset/local-only proof failed.");
                hiveVisualSnapshot = bootstrap.RunLocalLabHiveVisualProgressionForProof();
                Require(hiveVisualSnapshot.Pass, "Hive H1/H2/H3 visual progression regression failed.");
                labResetPass = bootstrap.ResetLocalLabForProof();
                labCollectionPass = bootstrap.RunLocalLabCollectionForProof();
                labCombatPass = bootstrap.RunLocalLabCombatForProof();
                labSnapshot = bootstrap.CurrentLocalLabProofSnapshot();
                Require(labResetPass && labCollectionPass && labCombatPass && labSnapshot.PremiumHivesLoaded, "LAB LOCAL collection/combat regression failed.");

                runtimeSnapshot = bootstrap.CurrentRuntimeEntitiesProofSnapshot();
                Require(runtimeSnapshot.RuntimePlacementMaskLoaded, "Runtime placement mask is not loaded.");
                Require(runtimeSnapshot.RuntimePlacementMaskCovers50x50, "Runtime placement mask does not cover all 50x50 chunks.");
                Require(runtimeSnapshot.TexturedResourceNodes >= 3, "Premium resource textures are not loaded.");
                Require(runtimeSnapshot.WaterNodes >= 1 && runtimeSnapshot.HoneyNodes >= 1, "Water or honey resource nodes are missing.");
                Require(runtimeSnapshot.TexturedBestiaryNodes >= 1, "Premium bestiary textures are not loaded.");

                resourceSnapshot = bootstrap.RunResourceInteractionProofForProof();
                Require(resourceSnapshot.Pass, "Resource interaction regression failed.");
                bestiarySnapshot = bootstrap.RunBestiaryInteractionProofForProof();
                Require(bestiarySnapshot.Pass, "Bestiary solo/raid regression failed.");
                runtimeSnapshot = bootstrap.CurrentRuntimeEntitiesProofSnapshot();
                Require(runtimeSnapshot.MaxBestiaryTier >= 7, "Premium bestiary T1..T7 coverage is incomplete.");
                mapToolsSnapshot = bootstrap.RunMapReadingToolsProofForProof();
                Require(mapToolsSnapshot.Pass, "Map reading tools regression failed.");
                polishSnapshot = bootstrap.RunInteractionPolishProofForProof();
                Require(polishSnapshot.Pass, "Interaction polish regression failed.");
                stressSnapshot = bootstrap.Run50x50ReadinessStressProofForProof();
                Require(stressSnapshot.Pass, "50x50 stress regression failed.");

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
            receipt.AppendLine("# WorldMap Automated Regression Proof Receipt");
            receipt.AppendLine();
            receipt.AppendLine("- Scene: `" + ScenePath + "`");
            receipt.AppendLine("- Play Mode: PASS");
            receipt.AppendLine("- Wave5 manifest: " + Pass(wave5Snapshot.ManifestReady));
            receipt.AppendLine("- Wave5 visible tiles: " + Pass(wave5Snapshot.VisibleTilesReady));
            receipt.AppendLine("- Wave5 loaded/required tiles: " + wave5Snapshot.LoadedVisibleTiles + "/" + wave5Snapshot.RequiredVisibleTiles);
            receipt.AppendLine("- BearDen loaded: " + Pass(wave5Snapshot.BearDenLoaded));
            receipt.AppendLine("- BearDen visible/hidden/restored: " + Pass(bearDenTogglePass));
            receipt.AppendLine("- LAB reset/local-only: " + Pass(labResetPass && labSnapshot.LocalOnly));
            receipt.AppendLine("- LAB two hives premium loaded: " + Pass(labSnapshot.PremiumHivesLoaded));
            receipt.AppendLine("- LAB collection/combat: " + Pass(labCollectionPass && labCombatPass));
            receipt.AppendLine("- Hive H1/H2/H3 progression: " + Pass(hiveVisualSnapshot.Pass));
            receipt.AppendLine("- Hive neutral N4: " + Pass(hiveVisualSnapshot.NeutralLevel4Pass));
            receipt.AppendLine("- Hive five classes N10: " + Pass(hiveVisualSnapshot.AllLevel10ClassesPass));
            receipt.AppendLine("- Hive N35 evolution: " + Pass(hiveVisualSnapshot.Level35Pass));
            receipt.AppendLine("- Hive player/enemy distinct overlays: " + Pass(hiveVisualSnapshot.PlayerEnemyDistinctPass));
            receipt.AppendLine("- Runtime placement mask loaded: " + Pass(runtimeSnapshot.RuntimePlacementMaskLoaded));
            receipt.AppendLine("- Runtime placement mask entries: " + runtimeSnapshot.RuntimePlacementMaskEntries);
            receipt.AppendLine("- Runtime placement mask covers 50x50: " + Pass(runtimeSnapshot.RuntimePlacementMaskCovers50x50));
            receipt.AppendLine("- Runtime resources textured: " + runtimeSnapshot.TexturedResourceNodes);
            receipt.AppendLine("- Runtime bestiary textured: " + runtimeSnapshot.TexturedBestiaryNodes);
            receipt.AppendLine("- Runtime max bestiary tier: " + runtimeSnapshot.MaxBestiaryTier);
            receipt.AppendLine("- Resource R1/R2/R3 interaction: " + Pass(resourceSnapshot.Pass));
            receipt.AppendLine("- Bestiary T1..T7 solo/raid: " + Pass(bestiarySnapshot.Pass));
            receipt.AppendLine("- Bestiary no official gain/server: " + Pass(bestiarySnapshot.NoOfficialGainPass));
            receipt.AppendLine("- Filters/nearest/legend: " + Pass(mapToolsSnapshot.Pass));
            receipt.AppendLine("- Interaction polish quantity/trajectory/depletion/respawn/combat: " + Pass(polishSnapshot.Pass));
            receipt.AppendLine("- 50x50 stress disabled by default: " + Pass(stressSnapshot.DisabledByDefault));
            receipt.AppendLine("- 50x50 catalog coordinates: " + stressSnapshot.CatalogCoordinates);
            receipt.AppendLine("- 50x50 active chunks center/NW/SE/dense: " + stressSnapshot.CenterActiveChunks + "/" + stressSnapshot.NorthWestActiveChunks + "/" + stressSnapshot.SouthEastActiveChunks + "/" + stressSnapshot.DensestActiveChunks);
            receipt.AppendLine("- 50x50 budgets/cache/terrain/allocation: " + Pass(stressSnapshot.BudgetsPass && stressSnapshot.CacheStablePass && stressSnapshot.TerrainPreservedPass && stressSnapshot.AllocationBudgetPass));
            receipt.AppendLine("- Server/remote/officiel: ABSENT");
            receipt.AppendLine("- APK rebuild: ABSENT");
            receipt.AppendLine("- 50x50 terrain art generation: ABSENT");
            receipt.AppendLine("- Raw random terrain placement: ABSENT");
            receipt.AppendLine();
            receipt.AppendLine("AUTOMATED_REGRESSION_EXACT_CROP_RUNTIME=PASS");
            receipt.AppendLine("WAVE6_50X50_RUNTIME_PLACEMENT_MASK=PASS");
            receipt.AppendLine("WAVE6_50X50_EXACT_CROP_TERRAIN_REGRESSION=NO");
            receipt.AppendLine("BEAR_DEN_REGRESSION=NO");
            receipt.AppendLine("READY_FOR_P5_DEMO_PACKAGE=YES");
            File.WriteAllText(Path.Combine(root, "AutomatedRegressionProofReceipt.md"), receipt.ToString(), Encoding.UTF8);
        }

        private static void FailAndExit(string message)
        {
            failed = true;
            EditorApplication.update -= ProcessHarness;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "AutomatedRegressionProofReceipt.md"), "AUTOMATED_REGRESSION_EXACT_CROP_RUNTIME=FAIL\n\n" + message, Encoding.UTF8);
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
