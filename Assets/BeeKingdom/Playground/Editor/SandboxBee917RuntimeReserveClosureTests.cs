using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee917RuntimeReserveClosureTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee917RuntimeReserveClosureTests();
                tests.UpgradeCompletionIsExplicitAndLevelChanges();
                tests.ResourceCapReservedCostAndSingleSpendRemainClear();
                tests.TrainingArrivalArmyFeedbackIsStrong();
                tests.ButtonsRefusalsGesturesAndTimelineAreDocumented();
                Debug.Log("BEE-903-907-910-917 runtime reserve closure checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-903-907-910-917 runtime reserve closure checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void UpgradeCompletionIsExplicitAndLevelChanges()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("upgrade_completion_visible");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof();
            string[] stateRows = HiveViewProductUiPresenter.PlayableHiveLoopStateForProof();

            AssertRow(rows, "upgrade_completion_visible:true");
            AssertRow(rows, "upgrade_timer_completed:true");
            AssertContains(rows, "upgrade_level_before_inferred:");
            AssertContains(rows, "upgrade_level_after:");
            AssertContains(stateRows, "last_action_status:Upgrade termine");
            AssertRow(rows, "single_spend_guard:true");
        }

        [Test]
        public void ResourceCapReservedCostAndSingleSpendRemainClear()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("resource_cap");
            string[] capRows = HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof();
            AssertRow(capRows, "cap_state:capacity_reached");
            AssertRow(capRows, "resource_growth_clarity:true");
            AssertRow(capRows, "single_spend_guard:true");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("double_upgrade_guard");
            string[] spendRows = HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof();
            string[] deterministicRows = HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof();
            AssertContains(spendRows, "reserved_cost:");
            AssertRow(deterministicRows, "upgrade_double_tap_guard:True");
            AssertRow(deterministicRows, "training_double_tap_guard:True");
        }

        [Test]
        public void TrainingArrivalArmyFeedbackIsStrong()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("training_eclaireuses_done");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof();

            AssertRow(rows, "training_arrival_visible:true");
            AssertContains(rows, "training_delta:+6 Eclaireuses");
            AssertContains(rows, "local_army_counts:");
            AssertRow(rows, "official_army_persistence:false");
        }

        [Test]
        public void ButtonsRefusalsGesturesAndTimelineAreDocumented()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("gesture_pan_proof");
            string[] gestureRows = HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof();
            AssertRow(gestureRows, "one_finger_rule:pan_only");
            AssertRow(gestureRows, "pan_never_sets_zoom:true");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("gesture_pinch_proof");
            string[] pinchRows = HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof();
            AssertRow(pinchRows, "two_finger_rule:pinch_zoom_only");
            AssertRow(pinchRows, "pinch_suppresses_selection:true");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("server_bridge_rejected");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof();
            AssertRow(rows, "buttons_non_mute:true");
            AssertRow(rows, "button_states:ready,pending,disabled,refused,completed");
            AssertContains(rows, "refusal_next_step:Produis plus.");
            AssertContains(rows, "timeline_t0_t9:T0 ressources");
            AssertRow(rows, "world_map_runtime_allowed:false");
            AssertRow(rows, "bee_881_implemented:false");
            AssertRow(rows, "official_server_live:false");
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
