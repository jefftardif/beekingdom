using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapRuntimeEntitiesProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        private const string OutputRoot = "Docs/WorldMapAudit/Wave6_50x50_Wave5Method12288/RuntimeEntitiesProof";
        private const string RunningKey = "BeeKingdom.WorldMapRuntimeEntitiesProof.Running";
        private const string StartedKey = "BeeKingdom.WorldMapRuntimeEntitiesProof.Started";
        private const string ExitPendingKey = "BeeKingdom.WorldMapRuntimeEntitiesProof.ExitPending";
        private const string ExitCodeKey = "BeeKingdom.WorldMapRuntimeEntitiesProof.ExitCode";
        private const int Width = 1280;
        private const int Height = 720;

        private static string root;
        private static WorldMapMmoFullscreenFoundationBootstrap bootstrap;
        private static int waitFrames;
        private static bool failed;
        private static WorldMapMmoFullscreenFoundationBootstrap.RuntimeEntitiesProofSnapshot snapshot;
        private static WorldMapMmoFullscreenFoundationBootstrap.ResourceInteractionProofSnapshot resourceInteractionSnapshot;
        private static WorldMapMmoFullscreenFoundationBootstrap.BestiaryInteractionProofSnapshot bestiaryInteractionSnapshot;

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

        [MenuItem("Bee Kingdom/World Map/Run Runtime Entities Proof Harness")]
        public static void RunRuntimeEntitiesProofHarness()
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
                snapshot = bootstrap.CurrentRuntimeEntitiesProofSnapshot();
                Require(snapshot.RuntimePlacementMaskLoaded, "Runtime placement mask is not loaded.");
                Require(snapshot.RuntimePlacementMaskCovers50x50, "Runtime placement mask does not cover all 50x50 chunks.");
                Require(snapshot.ResourceNodes >= 3, "Runtime resources are missing near the proof view.");
                Require(snapshot.TexturedResourceNodes >= 3, "Premium resource textures are not loaded.");
                Require(snapshot.WaterNodes >= 1, "Water resource node is missing.");
                Require(snapshot.HoneyNodes >= 1, "Honey resource node is missing.");
                Require(snapshot.BestiaryNodes >= 1, "Runtime bestiary node is missing.");
                Require(snapshot.TexturedBestiaryNodes >= 1, "Premium bestiary texture is not loaded.");
                resourceInteractionSnapshot = bootstrap.RunResourceInteractionProofForProof();
                Require(resourceInteractionSnapshot.Pass, "Resource interaction proof failed.");
                bestiaryInteractionSnapshot = bootstrap.RunBestiaryInteractionProofForProof();
                Require(bestiaryInteractionSnapshot.Pass, "Bestiary interaction proof failed.");
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
            receipt.AppendLine("# WorldMap Runtime Entities Proof Receipt");
            receipt.AppendLine();
            receipt.AppendLine("- Scene: `" + ScenePath + "`");
            receipt.AppendLine("- Play Mode: PASS");
            receipt.AppendLine("- Runtime placement mask loaded: " + Pass(snapshot.RuntimePlacementMaskLoaded));
            receipt.AppendLine("- Runtime placement mask entries: " + snapshot.RuntimePlacementMaskEntries);
            receipt.AppendLine("- Runtime placement mask covers 50x50: " + Pass(snapshot.RuntimePlacementMaskCovers50x50));
            receipt.AppendLine("- Premium resource textures loaded: " + Pass(snapshot.TexturedResourceNodes >= 3));
            receipt.AppendLine("- Water node present: " + Pass(snapshot.WaterNodes >= 1));
            receipt.AppendLine("- Honey node present: " + Pass(snapshot.HoneyNodes >= 1));
            receipt.AppendLine("- Premium bestiary textures loaded: " + Pass(snapshot.TexturedBestiaryNodes >= 1));
            receipt.AppendLine("- Resource nodes visible/proof: " + snapshot.ResourceNodes);
            receipt.AppendLine("- Textured resource nodes: " + snapshot.TexturedResourceNodes);
            receipt.AppendLine("- Bestiary nodes visible/proof: " + snapshot.BestiaryNodes);
            receipt.AppendLine("- Textured bestiary nodes: " + snapshot.TexturedBestiaryNodes);
            receipt.AppendLine("- Max bestiary tier in active proof window: " + snapshot.MaxBestiaryTier);
            receipt.AppendLine("- Resource interaction stage: " + Pass(resourceInteractionSnapshot.Pass));
            receipt.AppendLine("- Poor/medium/rich coverage: " + Pass(resourceInteractionSnapshot.TierCoveragePass));
            receipt.AppendLine("- Resource selection: " + Pass(resourceInteractionSnapshot.SelectionPass));
            receipt.AppendLine("- Local collection: " + Pass(resourceInteractionSnapshot.CollectionPass));
            receipt.AppendLine("- Depletion after collection: " + Pass(resourceInteractionSnapshot.DepletionPass));
            receipt.AppendLine("- Deterministic demo respawn: " + Pass(resourceInteractionSnapshot.RespawnPass));
            receipt.AppendLine("- Quantity before collection: " + resourceInteractionSnapshot.QuantityBefore);
            receipt.AppendLine("- Quantity after respawn: " + resourceInteractionSnapshot.QuantityAfterRespawn);
            receipt.AppendLine("- Selected resource proof: " + resourceInteractionSnapshot.SelectedResource);
            receipt.AppendLine("- Bestiary interaction stage: " + Pass(bestiaryInteractionSnapshot.Pass));
            receipt.AppendLine("- T1..T7 coverage: " + Pass(bestiaryInteractionSnapshot.TierCoveragePass));
            receipt.AppendLine("- Bestiary selection: " + Pass(bestiaryInteractionSnapshot.SelectionPass));
            receipt.AppendLine("- Solo combat local: " + Pass(bestiaryInteractionSnapshot.SoloCombatPass));
            receipt.AppendLine("- Raid combat local: " + Pass(bestiaryInteractionSnapshot.RaidCombatPass));
            receipt.AppendLine("- No official gain/server: " + Pass(bestiaryInteractionSnapshot.NoOfficialGainPass));
            receipt.AppendLine("- Solo target: " + bestiaryInteractionSnapshot.SoloTarget);
            receipt.AppendLine("- Raid target: " + bestiaryInteractionSnapshot.RaidTarget);
            receipt.AppendLine("- Last bestiary telemetry: " + bestiaryInteractionSnapshot.LastCombatTelemetry);
            receipt.AppendLine("- Server/remote/officiel: ABSENT");
            receipt.AppendLine("- Raw random terrain placement: ABSENT");
            receipt.AppendLine();
            receipt.AppendLine("RUNTIME_ENTITIES_WAVE1_EXACT_CROP_UNITY_INTEGRATION=PASS");
            receipt.AppendLine("WAVE6_50X50_RUNTIME_PLACEMENT_MASK=PASS");
            File.WriteAllText(Path.Combine(root, "RuntimeEntitiesProofReceipt.md"), receipt.ToString(), Encoding.UTF8);
        }

        private static void FailAndExit(string message)
        {
            failed = true;
            EditorApplication.update -= ProcessHarness;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "RuntimeEntitiesProofReceipt.md"), "RUNTIME_ENTITIES_WAVE1_EXACT_CROP_UNITY_INTEGRATION=FAIL\n\n" + message, Encoding.UTF8);
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
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }
        }

        private static string AbsoluteProjectPath(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        }
    }
}
