using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee831PlayableHiveCoreTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee831PlayableHiveCoreTests();
                tests.ButtonMatrixDocumentsImportantButtonsAsNonMute();
                tests.ResourceUpgradeAndTrainingClarityRowsArePresent();
                tests.DeterministicGuardsStillProtectUpgradeAndTraining();
                Debug.Log("BEE-828-831 playable hive core checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-828-831 playable hive core checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void ButtonMatrixDocumentsImportantButtonsAsNonMute()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveButtonStateMatrixForProof();

            AssertRow(rows, "button_matrix:true");
            AssertRow(rows, "detail_close:ready:closes_panel");
            AssertRow(rows, "detail_open:ready:opens_panel");
            AssertRow(rows, "bottom_world:future:service_future_feedback");
            AssertRow(rows, "side_quests:future:badge_and_feedback");
            AssertRow(rows, "disabled_press_feedback:true");
            AssertRow(rows, "no_mute_important_buttons:true");
            AssertRow(rows, "server_live_claim:false");
        }

        [Test]
        public void ResourceUpgradeAndTrainingClarityRowsArePresent()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("resources_tick");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveClarityForProof();

            AssertRow(rows, "bee_828_831_scope:playable_hive_core_only");
            AssertRow(rows, "resource_growth_visible:true");
            AssertContains(rows, "resource_feedback_text:+42 miel/s");
            AssertContains(rows, "upgrade_cost_visible:");
            AssertContains(rows, "upgrade_duration_visible:18s");
            AssertContains(rows, "upgrade_level_flow:");
            AssertRow(rows, "upgrade_double_action_blocked:true");
            AssertContains(rows, "training_troop_type_visible:");
            AssertContains(rows, "training_cost_visible:");
            AssertContains(rows, "training_duration_visible:14s");
            AssertContains(rows, "training_queue_visible:");
            AssertRow(rows, "training_double_action_blocked:true");
            AssertRow(rows, "world_map_expansion_allowed:false");
            AssertRow(rows, "official_server_progression:false");
            AssertRow(rows, "bee_832_plus_implemented:false");
        }

        [Test]
        public void DeterministicGuardsStillProtectUpgradeAndTraining()
        {
            string[] rows = HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof();

            AssertRow(rows, "upgrade_double_tap_guard:True");
            AssertRow(rows, "training_double_tap_guard:True");
            AssertRow(rows, "level_increment_once:True");
            AssertRow(rows, "training_queue_consistent:True");
            AssertRow(rows, "troop_increment_once:True");
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
