using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee850PlayableHiveActionLoopTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee850PlayableHiveActionLoopTests();
                tests.ResourceTicksCapAndPersistabilityPrepStayLocalPreview();
                tests.UpgradeCostTimerCompletionAndAntiDoubleGuardAreDocumented();
                tests.TrainingQueueCompletionArmyCountsAndNonPersistenceAreDocumented();
                tests.PreviousPanelAndButtonGuardsRemainIntact();
                Debug.Log("BEE-842-850 playable hive action loop checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-842-850 playable hive action loop checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void ResourceTicksCapAndPersistabilityPrepStayLocalPreview()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("resource_cap");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveActionLoopForProof();
            string[] stateRows = HiveViewProductUiPresenter.PlayableHiveLoopStateForProof();

            AssertRow(rows, "bee_842_850_scope:playable_hive_action_loop_server_first_local_preview");
            AssertRow(rows, "resource_ticks_visible:true");
            AssertRow(rows, "resource_cap_state:capacity_reached");
            AssertRow(rows, "persistability_prep:future_server_snapshot_only_no_save");
            AssertRow(stateRows, "resource_cap_state:capacity_reached");
            AssertRow(rows, "world_map_runtime_allowed:false");
            AssertRow(rows, "official_server_progression:false");
            AssertRow(rows, "official_save_economy_army:false");
        }

        [Test]
        public void UpgradeCostTimerCompletionAndAntiDoubleGuardAreDocumented()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("upgrade_done");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveActionLoopForProof();
            string[] stateRows = HiveViewProductUiPresenter.PlayableHiveLoopStateForProof();
            string[] deterministicRows = HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof();

            AssertRow(rows, "upgrade_cost_timer_completion:true");
            AssertContains(rows, "upgrade_cost:");
            AssertContains(rows, "upgrade_timer_seconds:");
            AssertRow(rows, "upgrade_completion_visible:true");
            AssertRow(rows, "upgrade_cancel_local_active:false_server_first_pending");
            AssertRow(rows, "upgrade_anti_double_server_prep:true");
            AssertRow(stateRows, "upgrade_completed:True");
            AssertRow(deterministicRows, "upgrade_double_tap_guard:True");
            AssertRow(deterministicRows, "level_increment_once:True");
        }

        [Test]
        public void TrainingQueueCompletionArmyCountsAndNonPersistenceAreDocumented()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("training_eclaireuses_done");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveActionLoopForProof();
            string[] stateRows = HiveViewProductUiPresenter.PlayableHiveLoopStateForProof();
            string[] deterministicRows = HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof();

            AssertRow(rows, "training_cost_timer_queue_completion:true");
            AssertContains(rows, "training_cost:");
            AssertContains(rows, "training_queue:");
            AssertRow(rows, "training_completion_visible:true");
            AssertRow(rows, "training_anti_double_queue_guard:true");
            AssertRow(rows, "local_army_visible:true");
            AssertContains(rows, "local_army_counts:");
            AssertRow(rows, "army_non_persistent_guard:true");
            AssertContains(stateRows, "army_feedback:+6 Eclaireuses");
            AssertRow(deterministicRows, "training_double_tap_guard:True");
            AssertRow(deterministicRows, "troop_increment_once:True");
        }

        [Test]
        public void PreviousPanelAndButtonGuardsRemainIntact()
        {
            string[] panelRows = HiveViewProductUiPresenter.PlayableHivePanelPolishForProof();
            string[] buttonRows = HiveViewProductUiPresenter.PlayableHiveButtonStateMatrixForProof();

            AssertRow(panelRows, "right_panel_density_polished:true");
            AssertRow(panelRows, "disabled_reason_near_action:true");
            AssertRow(buttonRows, "no_mute_important_buttons:true");
            AssertRow(buttonRows, "server_live_claim:false");
        }

        private static void AssertRow(string[] rows, string expected)
        {
            if (!rows.Any(row => string.Equals(row, expected, StringComparison.Ordinal)))
            {
                Assert.Fail("Expected proof row not found: " + expected + Environment.NewLine + string.Join(Environment.NewLine, rows));
            }
        }

        private static void AssertContains(string[] rows, string expectedPrefix)
        {
            if (!rows.Any(row => row.StartsWith(expectedPrefix, StringComparison.Ordinal)))
            {
                Assert.Fail("Expected proof row prefix not found: " + expectedPrefix + Environment.NewLine + string.Join(Environment.NewLine, rows));
            }
        }
    }
}
