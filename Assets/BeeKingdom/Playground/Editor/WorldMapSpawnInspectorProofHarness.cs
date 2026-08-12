using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapSpawnInspectorProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        private const string OutputRoot = "Docs/WorldMapAudit/Wave6_50x50_Wave5Method12288/SpawnInspectorProof";
        private const string RunningKey = "BeeKingdom.WorldMapSpawnInspectorProof.Running";
        private const string StartedKey = "BeeKingdom.WorldMapSpawnInspectorProof.Started";
        private const string ExitPendingKey = "BeeKingdom.WorldMapSpawnInspectorProof.ExitPending";
        private const string ExitCodeKey = "BeeKingdom.WorldMapSpawnInspectorProof.ExitCode";
        private const string DeadlineUtcTicksKey = "BeeKingdom.WorldMapSpawnInspectorProof.DeadlineUtcTicks";
        private const int HarnessDeadlineSeconds = 180;

        private static string root;
        private static WorldMapMmoFullscreenFoundationBootstrap bootstrap;
        private static int waitFrames;
        private static bool failed;
        private static WorldMapMmoFullscreenFoundationBootstrap.SpawnInspectorProofSnapshot snapshot;

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

        [MenuItem("Bee Kingdom/World Map/Run Spawn Inspector Proof Harness")]
        public static void RunSpawnInspectorProofHarness()
        {
            root = AbsoluteProjectPath(OutputRoot);
            Directory.CreateDirectory(root);
            DeletePreviousOutputs();
            failed = false;
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetBool(ExitPendingKey, false);
            SessionState.SetString(DeadlineUtcTicksKey, DateTime.UtcNow.AddSeconds(HarnessDeadlineSeconds).Ticks.ToString(CultureInfo.InvariantCulture));
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

            if (HarnessDeadlineExceeded())
            {
                FailAndExit("Spawn inspector proof exceeded the " + HarnessDeadlineSeconds.ToString(CultureInfo.InvariantCulture) + " second deadline before Play Mode became ready.");
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
                if (HarnessDeadlineExceeded()) throw new TimeoutException("Spawn inspector proof exceeded the " + HarnessDeadlineSeconds.ToString(CultureInfo.InvariantCulture) + " second deadline.");
                if (waitFrames > 0)
                {
                    waitFrames--;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                snapshot = bootstrap.RunSpawnInspectorProofForProof();
                if (!snapshot.Pass)
                {
                    WriteReceipt();
                    throw new InvalidOperationException("Spawn inspector proof failed: " + SnapshotFailureSummary());
                }
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
            SessionState.EraseString(DeadlineUtcTicksKey);
            EditorApplication.Exit(0);
        }

        private static void WriteReceipt()
        {
            var receipt = new StringBuilder();
            receipt.AppendLine("# WorldMap Spawn Inspector Proof Receipt");
            receipt.AppendLine();
            receipt.AppendLine("- Scene: `" + ScenePath + "`");
            receipt.AppendLine("- Generated UTC: `" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) + "`");
            receipt.AppendLine("- Unity: `" + Application.unityVersion + "`");
            receipt.AppendLine("- Play Mode harness: " + Pass(snapshot.Pass));
            receipt.AppendLine("- Harness deadline seconds: " + HarnessDeadlineSeconds.ToString(CultureInfo.InvariantCulture));
            receipt.AppendLine();
            receipt.AppendLine("## Seeds and versions");
            receipt.AppendLine();
            receipt.AppendLine("- seed_a: `" + snapshot.SeedA.ToString(CultureInfo.InvariantCulture) + "`");
            receipt.AppendLine("- seed_b: `" + snapshot.SeedB.ToString(CultureInfo.InvariantCulture) + "`");
            receipt.AppendLine("- spawn_seed_version: `" + snapshot.SpawnSeedVersion + "`");
            receipt.AppendLine("- exclusion_version: `" + snapshot.ExclusionVersion + "`");
            receipt.AppendLine("- world_grid_version: `" + snapshot.WorldGridVersion + "`");
            receipt.AppendLine("- A1 hash: `" + snapshot.SeedA1Hash + "`");
            receipt.AppendLine("- A2 hash after center-neighbor-center traversal: `" + snapshot.SeedA2Hash + "`");
            receipt.AppendLine("- B hash: `" + snapshot.SeedBHash + "`");
            receipt.AppendLine("- alternate version/hash: `" + snapshot.AlternateSpawnSeedVersion + "` / `" + snapshot.VersionCHash + "`");
            receipt.AppendLine("- A1/A2 count: " + Pass(snapshot.SameSeedComparison.CountEqual));
            receipt.AppendLine("- A1/A2 IDs: " + Pass(snapshot.SameSeedComparison.IdsEqual));
            receipt.AppendLine("- A1/A2 positions: " + Pass(snapshot.SameSeedComparison.PositionsEqual));
            receipt.AppendLine("- A1/A2 tiers: " + Pass(snapshot.SameSeedComparison.TiersEqual));
            receipt.AppendLine("- A1/A2 richness: " + Pass(snapshot.SameSeedComparison.RichnessEqual));
            receipt.AppendLine("- A1/A2 flags: " + Pass(snapshot.SameSeedComparison.FlagsEqual));
            receipt.AppendLine("- Seed B chunks/hives/resources/threats: " + Counts(snapshot.SeedBActiveChunks, snapshot.SeedBHives, snapshot.SeedBResources, snapshot.SeedBBestiary));
            receipt.AppendLine("- Seed B distribution changed: " + Pass(snapshot.SeedVariationPass));
            receipt.AppendLine("- Seed B budgets preserved: " + Pass(snapshot.DifferentSeedBudgetsPreserved));
            receipt.AppendLine("- Spawn seed version change audited: " + Pass(snapshot.SeedVersionVariationPass));
            receipt.AppendLine();
            receipt.AppendLine("## Window coverage");
            receipt.AppendLine();
            receipt.AppendLine("| Grid | Window | Center | World chunk | Chunk bounds | Active | Hives | Resources | Threats | In bounds | Budgets |");
            receipt.AppendLine("| --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: | --- | --- |");
            AppendWindowRows(receipt, snapshot.Windows25x25);
            AppendWindowRows(receipt, snapshot.Windows50x50);
            receipt.AppendLine();
            receipt.AppendLine("- Logical 50x50 coordinates: 2500");
            receipt.AppendLine("- Logical 50x50 terrain generated: false");
            receipt.AppendLine("- Window coverage: " + Pass(snapshot.WindowCoveragePass));
            receipt.AppendLine();
            receipt.AppendLine("## Forced exclusions");
            receipt.AppendLine();
            receipt.AppendLine("| Zone | Submitted | Rejected | Accepted | Reason | Reprojection rejected | Reprojection reason | Result |");
            receipt.AppendLine("| --- | ---: | ---: | ---: | --- | --- | --- | --- |");
            AppendForcedExclusionRows(receipt, snapshot.ForcedExclusions);
            receipt.AppendLine();
            receipt.AppendLine("- accepted_entities_inside_exclusions: " + snapshot.AcceptedEntitiesInsideExclusions.ToString(CultureInfo.InvariantCulture));
            receipt.AppendLine("- Forced exclusions: " + Pass(snapshot.ExclusionZonesPass));
            receipt.AppendLine();
            receipt.AppendLine("## Negative tests");
            receipt.AppendLine();
            receipt.AppendLine("| ID | Injection | Expected rejection | Observed | Result |");
            receipt.AppendLine("| --- | --- | --- | --- | --- |");
            AppendNegativeRows(receipt, snapshot.NegativeTests);
            receipt.AppendLine();
            receipt.AppendLine("- Negative tests passed: " + CountPassing(snapshot.NegativeTests).ToString(CultureInfo.InvariantCulture) + "/8");
            receipt.AppendLine();
            receipt.AppendLine("## Selection, combat, and readability");
            receipt.AppendLine();
            receipt.AppendLine("- critical_overlaps: " + snapshot.Overlap.CriticalOverlaps.ToString(CultureInfo.InvariantCulture));
            receipt.AppendLine("- minor_overlaps: " + snapshot.Overlap.MinorOverlaps.ToString(CultureInfo.InvariantCulture));
            receipt.AppendLine("- overlap thresholds critical/minor: " + snapshot.Overlap.CriticalDistance.ToString("0.###", CultureInfo.InvariantCulture) + "/" + snapshot.Overlap.MinorDistance.ToString("0.###", CultureInfo.InvariantCulture));
            receipt.AppendLine("- nearest selection expected/selected: `" + snapshot.Overlap.ExpectedNearestId + "` / `" + snapshot.Overlap.SelectedNearestId + "`");
            receipt.AppendLine("- nearest selection: " + Pass(snapshot.Overlap.NearestSelectionPass));
            receipt.AppendLine("- combat_t1_t4_solo: " + Pass(snapshot.Combat.T1T4Solo) + " (`" + snapshot.Combat.SoloAccess + "`)");
            receipt.AppendLine("- combat_t5_t7_raid: " + Pass(snapshot.Combat.T5T7Raid) + " (`" + snapshot.Combat.RaidAccess + "`)");
            receipt.AppendLine("- combat_t7_solo_refused: " + Pass(snapshot.Combat.T7SoloRefused) + " (`" + snapshot.Combat.T7SoloReason + "`)");
            receipt.AppendLine("- richness R1/R2/R3: `" + snapshot.Richness.R1Text + "` / `" + snapshot.Richness.R2Text + "` / `" + snapshot.Richness.R3Text + "`");
            receipt.AppendLine("- richness_r1_r2_r3_readable: " + Pass(snapshot.Richness.Pass));
            receipt.AppendLine("- richness_readable_without_color: " + Pass(snapshot.Richness.ReadableWithoutColor));
            receipt.AppendLine();
            receipt.AppendLine("## Reprojection and overlay");
            receipt.AppendLine();
            receipt.AppendLine("- reprojection records checked: " + snapshot.Reprojection.RecordsChecked.ToString(CultureInfo.InvariantCulture));
            receipt.AppendLine("- reprojected chunk X range: " + snapshot.Reprojection.MinChunkX + ".." + snapshot.Reprojection.MaxChunkX);
            receipt.AppendLine("- reprojected chunk Y range: " + snapshot.Reprojection.MinChunkY + ".." + snapshot.Reprojection.MaxChunkY);
            receipt.AppendLine("- reprojected local range: " + snapshot.Reprojection.MinLocal.ToString("0.######", CultureInfo.InvariantCulture) + ".." + snapshot.Reprojection.MaxLocal.ToString("0.######", CultureInfo.InvariantCulture));
            receipt.AppendLine("- reprojection_50x50_pass: " + Pass(snapshot.Reprojection.Pass));
            receipt.AppendLine("- diagnostic_overlay_default_off: " + Pass(snapshot.OverlayDefaultOff));
            receipt.AppendLine("- overlay OFF hash: `" + snapshot.OverlayOffHash + "`");
            receipt.AppendLine("- overlay ON hash: `" + snapshot.OverlayOnHash + "`");
            receipt.AppendLine("- overlay_distribution_unchanged: " + Pass(snapshot.OverlayDistributionUnchanged));
            receipt.AppendLine();
            receipt.AppendLine("## Authority and budgets");
            receipt.AppendLine();
            receipt.AppendLine("- server=" + Bool(snapshot.Server));
            receipt.AppendLine("- official=" + Bool(snapshot.Official));
            receipt.AppendLine("- official_gain=" + Bool(snapshot.OfficialGain));
            receipt.AppendLine("- remote_calls=" + snapshot.RemoteCalls.ToString(CultureInfo.InvariantCulture));
            receipt.AppendLine("- authority_validation: " + Pass(snapshot.AuthorityFlagsPass) + " (`" + snapshot.AuthorityReason + "`)");
            receipt.AppendLine("- max chunks/hives/resources/threats: " + Counts(snapshot.MaxActiveChunks, snapshot.MaxHives, snapshot.MaxResources, snapshot.MaxBestiary));
            receipt.AppendLine("- wave5_cached_textures: " + snapshot.Wave5CachedTextures.ToString(CultureInfo.InvariantCulture) + "/96");
            receipt.AppendLine("- runtime_entity_texture_cache_entries: " + snapshot.RuntimeEntityTextureCacheEntries.ToString(CultureInfo.InvariantCulture));
            receipt.AppendLine("- total_cached_textures: " + (snapshot.Wave5CachedTextures + snapshot.RuntimeEntityTextureCacheEntries).ToString(CultureInfo.InvariantCulture) + "/96");
            receipt.AppendLine("- allocated_bytes_50x50_stress: " + snapshot.AllocatedBytes.ToString(CultureInfo.InvariantCulture) + "/2000000");
            receipt.AppendLine("- chunk_cache_before_after_50x50: " + snapshot.ChunkCacheBefore50x50 + "/" + snapshot.ChunkCacheAfter50x50);
            receipt.AppendLine("- no_50x50_terrain_generated: " + Bool(snapshot.No50x50TerrainGenerated));
            receipt.AppendLine("- density_budgets: " + Pass(snapshot.DensityBudgetsPass));
            receipt.AppendLine("- P1-P6 regression: " + Pass(snapshot.P1P6RegressionNo));
            receipt.AppendLine();
            receipt.AppendLine("```text");
            receipt.AppendLine("WORLD_MAP_SPAWN_DISTRIBUTION_P7_RECEIPT");
            receipt.AppendLine("same_seed_same_version_hash_a1=" + snapshot.SeedA1Hash);
            receipt.AppendLine("same_seed_same_version_hash_a2=" + snapshot.SeedA2Hash);
            receipt.AppendLine("same_seed_stable=" + Pass(snapshot.DeterministicSpawnPass));
            receipt.AppendLine("different_seed_hash_b=" + snapshot.SeedBHash);
            receipt.AppendLine("different_seed_distribution_changed=" + Pass(snapshot.SeedVariationPass));
            receipt.AppendLine("different_seed_budgets_preserved=" + Pass(snapshot.DifferentSeedBudgetsPreserved));
            receipt.AppendLine("accepted_entities_inside_exclusions=" + snapshot.AcceptedEntitiesInsideExclusions.ToString(CultureInfo.InvariantCulture));
            receipt.AppendLine("critical_overlaps=" + snapshot.Overlap.CriticalOverlaps.ToString(CultureInfo.InvariantCulture));
            receipt.AppendLine("server=" + Bool(snapshot.Server));
            receipt.AppendLine("official=" + Bool(snapshot.Official));
            receipt.AppendLine("official_gain=" + Bool(snapshot.OfficialGain));
            receipt.AppendLine("remote_calls=" + snapshot.RemoteCalls.ToString(CultureInfo.InvariantCulture));
            receipt.AppendLine("DETERMINISTIC_SPAWN=" + Pass(snapshot.DeterministicSpawnPass));
            receipt.AppendLine("SEED_VARIATION=" + Pass(snapshot.SeedVariationPass));
            receipt.AppendLine("EXCLUSION_ZONES=" + Pass(snapshot.ExclusionZonesPass));
            receipt.AppendLine("DENSITY_BUDGETS=" + Pass(snapshot.DensityBudgetsPass));
            receipt.AppendLine("DIAGNOSTIC_OVERLAY_DEFAULT=" + (snapshot.OverlayDefaultOff ? "OFF" : "ON"));
            receipt.AppendLine("P1_P6_REGRESSION=" + (snapshot.P1P6RegressionNo ? "NO" : "YES"));
            receipt.AppendLine("P7_QA_EVIDENCE_CLOSURE=" + Pass(snapshot.Pass));
            receipt.AppendLine("SPAWN_INSPECTOR_EXACT_CROP_RUNTIME=" + Pass(snapshot.Pass));
            receipt.AppendLine("P7_NEGATIVE_TESTS_8_OF_8=" + Pass(snapshot.NegativeTestsPass));
            receipt.AppendLine("FORCED_EXCLUSIONS=" + Pass(snapshot.ExclusionZonesPass));
            receipt.AppendLine("WINDOW_COVERAGE=" + Pass(snapshot.WindowCoveragePass));
            receipt.AppendLine("AUTHORITY_FLAGS=" + Pass(snapshot.AuthorityFlagsPass));
            receipt.AppendLine("READY_FOR_QA_P7_REVIEW=" + (snapshot.Pass ? "YES" : "NO"));
            receipt.AppendLine("```");
            File.WriteAllText(Path.Combine(root, "SpawnInspectorProofReceipt.md"), receipt.ToString(), Encoding.UTF8);
        }

        private static void AppendWindowRows(StringBuilder receipt, WorldMapMmoFullscreenFoundationBootstrap.SpawnWindowProof[] windows)
        {
            for (int i = 0; i < windows.Length; i++)
            {
                WorldMapMmoFullscreenFoundationBootstrap.SpawnWindowProof window = windows[i];
                receipt.AppendLine("| " + window.GridSize + "x" + window.GridSize
                    + " | " + window.Label
                    + " | " + window.CenterX + "," + window.CenterY
                    + " | " + window.WorldChunkX + "," + window.WorldChunkY
                    + " | X" + window.MinChunkX + ".." + window.MaxChunkX + " Y" + window.MinChunkY + ".." + window.MaxChunkY
                    + " | " + window.ActiveChunks
                    + " | " + window.Hives
                    + " | " + window.Resources
                    + " | " + window.Bestiary
                    + " | " + Pass(window.CoordinatesInBounds)
                    + " | " + Pass(window.BudgetsPass) + " |");
            }
        }

        private static void AppendForcedExclusionRows(StringBuilder receipt, WorldMapMmoFullscreenFoundationBootstrap.ForcedExclusionProof[] exclusions)
        {
            for (int i = 0; i < exclusions.Length; i++)
            {
                WorldMapMmoFullscreenFoundationBootstrap.ForcedExclusionProof exclusion = exclusions[i];
                receipt.AppendLine("| " + exclusion.Zone
                    + " | " + exclusion.Submitted
                    + " | " + exclusion.Rejected
                    + " | " + exclusion.Accepted
                    + " | " + EscapeTable(exclusion.Reason)
                    + " | " + Pass(exclusion.ReprojectedRejected)
                    + " | " + EscapeTable(exclusion.ReprojectedReason)
                    + " | " + Pass(exclusion.Pass) + " |");
            }
        }

        private static void AppendNegativeRows(StringBuilder receipt, WorldMapMmoFullscreenFoundationBootstrap.NegativeTestProof[] tests)
        {
            for (int i = 0; i < tests.Length; i++)
            {
                WorldMapMmoFullscreenFoundationBootstrap.NegativeTestProof test = tests[i];
                receipt.AppendLine("| " + test.Id
                    + " | " + EscapeTable(test.Injected)
                    + " | " + EscapeTable(test.Expected)
                    + " | " + EscapeTable(test.Observed)
                    + " | " + Pass(test.Pass) + " |");
            }
        }

        private static int CountPassing(WorldMapMmoFullscreenFoundationBootstrap.NegativeTestProof[] tests)
        {
            int count = 0;
            for (int i = 0; i < tests.Length; i++) if (tests[i].Pass) count++;
            return count;
        }

        private static string Counts(int chunks, int hives, int resources, int threats)
        {
            return chunks.ToString(CultureInfo.InvariantCulture)
                + "/" + hives.ToString(CultureInfo.InvariantCulture)
                + "/" + resources.ToString(CultureInfo.InvariantCulture)
                + "/" + threats.ToString(CultureInfo.InvariantCulture);
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string EscapeTable(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("|", "\\|");
        }

        private static void FailAndExit(string message)
        {
            failed = true;
            EditorApplication.update -= ProcessHarness;
            Directory.CreateDirectory(root);
            string receiptPath = Path.Combine(root, "SpawnInspectorProofReceipt.md");
            if (snapshot.Windows25x25 != null)
            {
                File.AppendAllText(receiptPath, "\n\n## Harness failure\n\n`SPAWN_INSPECTOR_EXACT_CROP_RUNTIME=FAIL`\n\n```text\n" + message + "\n```\n", Encoding.UTF8);
            }
            else
            {
                File.WriteAllText(receiptPath, "SPAWN_INSPECTOR_EXACT_CROP_RUNTIME=FAIL\n\n" + message, Encoding.UTF8);
            }
            SessionState.SetBool(RunningKey, false);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetInt(ExitCodeKey, 1);
            SessionState.SetBool(ExitPendingKey, false);
            SessionState.EraseString(DeadlineUtcTicksKey);
            EditorApplication.Exit(1);
        }

        private static string SnapshotFailureSummary()
        {
            return "determinism=" + Pass(snapshot.DeterministicSpawnPass)
                + ",variation=" + Pass(snapshot.SeedVariationPass)
                + ",exclusions=" + Pass(snapshot.ExclusionZonesPass)
                + ",budgets=" + Pass(snapshot.DensityBudgetsPass)
                + ",coverage=" + Pass(snapshot.SpawnInspectorUiPass)
                + ",overlay=" + Pass(snapshot.DiagnosticOverlayDefaultOff)
                + ",windows=" + Pass(snapshot.WindowCoveragePass)
                + ",overlap=" + Pass(snapshot.Overlap.Pass)
                + ",negative_tests=" + Pass(snapshot.NegativeTestsPass)
                + ",authority=" + Pass(snapshot.AuthorityFlagsPass)
                + ",no_50x50_terrain=" + Pass(snapshot.No50x50TerrainGenerated)
                + ",p1_p6=" + Pass(snapshot.P1P6RegressionNo);
        }

        private static bool HarnessDeadlineExceeded()
        {
            string value = SessionState.GetString(DeadlineUtcTicksKey, string.Empty);
            long ticks;
            return !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks)
                || DateTime.UtcNow.Ticks > ticks;
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
