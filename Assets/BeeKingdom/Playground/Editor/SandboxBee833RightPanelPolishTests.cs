using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee833RightPanelPolishTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee833RightPanelPolishTests();
                tests.RightPanelPolishKeepsEssentialDataAndSinglePrimaryAction();
                tests.DisabledReasonPlacementStaysNearActionAndReadable();
                tests.PreviousPlayableHiveGuardsRemainIntact();
                Debug.Log("BEE-832-833 right panel polish checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-832-833 right panel polish checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void RightPanelPolishKeepsEssentialDataAndSinglePrimaryAction()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            string[] rows = HiveViewProductUiPresenter.PlayableHivePanelPolishForProof();

            AssertRow(rows, "bee_832_833_scope:right_panel_density_and_disabled_reason_only");
            AssertRow(rows, "right_panel_density_polished:true");
            AssertRow(rows, "right_panel_hierarchy:identity_action_reason_cost_duration_progress_queue_preview");
            AssertRow(rows, "right_panel_single_primary_action:true");
            AssertRow(rows, "essential_data_preserved:building_level_status_action_cost_duration_progress_queue_feedback_preview");
            AssertRow(rows, "world_map_expansion_allowed:false");
            AssertRow(rows, "official_server_progression:false");
            AssertRow(rows, "bee_834_plus_implemented:false");
        }

        [Test]
        public void DisabledReasonPlacementStaysNearActionAndReadable()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("upgrade_blocked");
            string[] rows = HiveViewProductUiPresenter.PlayableHivePanelPolishForProof();

            AssertRow(rows, "disabled_reason_in_reading_flow:true");
            AssertRow(rows, "disabled_reason_near_action:true");
            AssertRow(rows, "tablet_landscape_readable:true");
            AssertRow(rows, "phone_portrait_readable:true");
            AssertContains(rows, "upgrade_disabled_reason:");
        }

        [Test]
        public void PreviousPlayableHiveGuardsRemainIntact()
        {
            string[] buttonRows = HiveViewProductUiPresenter.PlayableHiveButtonStateMatrixForProof();
            string[] clarityRows = HiveViewProductUiPresenter.PlayableHiveClarityForProof();
            string[] deterministicRows = HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof();

            AssertRow(buttonRows, "no_mute_important_buttons:true");
            AssertRow(buttonRows, "server_live_claim:false");
            AssertRow(clarityRows, "resource_growth_visible:true");
            AssertRow(clarityRows, "upgrade_double_action_blocked:true");
            AssertRow(clarityRows, "training_double_action_blocked:true");
            AssertRow(deterministicRows, "upgrade_double_tap_guard:True");
            AssertRow(deterministicRows, "training_double_tap_guard:True");
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
