using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee827PlayableHiveAutomationTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee827PlayableHiveAutomationTests();
                tests.RapidTapUpgradeTrainingAndDeterministicLoopChecksRemainGuarded();
                tests.HiveDeviceTouchProtocolKeepsPreviewNonServerScope();
                tests.GestureTelemetryExposesPanPinchAndFixedUiRules();
                Debug.Log("BEE-827 playable hive automation checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-827 playable hive automation checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void RapidTapUpgradeTrainingAndDeterministicLoopChecksRemainGuarded()
        {
            string[] rows = HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof();

            AssertRow(rows, "deterministic_checks:true");
            AssertRow(rows, "upgrade_commit_count_after_double_input:1");
            AssertRow(rows, "upgrade_repeat_blocked_count:1");
            AssertRow(rows, "upgrade_double_tap_guard:True");
            AssertRow(rows, "level_increment_once:True");
            AssertRow(rows, "training_commit_count_after_double_input:1");
            AssertRow(rows, "training_repeat_blocked_count:1");
            AssertRow(rows, "training_double_tap_guard:True");
            AssertRow(rows, "training_queue_delta:1");
            AssertRow(rows, "training_queue_consistent:True");
            AssertRow(rows, "troop_increment_once:True");
            AssertRow(rows, "resource_values_non_negative:True");
            AssertRow(rows, "official_server_progression:false");
        }

        [Test]
        public void HiveDeviceTouchProtocolKeepsPreviewNonServerScope()
        {
            string[] rows = HiveViewProductUiPresenter.PlayableHiveDeviceTouchProtocolForProof();

            AssertRow(rows, "bee_821_827_scope:playable_hive_only");
            AssertRow(rows, "world_map_expansion_allowed:false");
            AssertRow(rows, "device_touch_proof_required:true");
            AssertRow(rows, "one_finger_expected:pan_only");
            AssertRow(rows, "two_finger_expected:pinch_zoom_only");
            AssertRow(rows, "ui_touch_expected:ui_action_no_hive_pan_zoom");
            AssertRow(rows, "server_live_claim:false");
            AssertRow(rows, "official_save_economy_army_claim:false");
            AssertRow(rows, "bee_828_plus_implemented:false");
        }

        [Test]
        public void GestureTelemetryExposesPanPinchAndFixedUiRules()
        {
            HiveViewProductUiPresenter.SetReferenceHiveGestureTelemetryForProof("one-finger-pan", 1, 32f, -12f, 0f, 1.12f, 1.10f);
            string[] rows = HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof();

            AssertRow(rows, "gesture_mode:one-finger-pan");
            AssertRow(rows, "touch_count:1");
            AssertRow(rows, "one_finger_rule:pan_only");
            AssertRow(rows, "two_finger_rule:pinch_zoom_only");
            AssertRow(rows, "pan_never_sets_zoom:true");
            AssertRow(rows, "pinch_suppresses_selection:true");
            AssertRow(rows, "hud_fixed:true");
            AssertRow(rows, "panels_fixed:true");
            AssertRow(rows, "navigation_fixed:true");
            AssertRow(rows, "selection_suppressed_during_pan_or_pinch:True");
        }

        private static void AssertRow(string[] rows, string expected)
        {
            if (!rows.Any(row => string.Equals(row, expected, StringComparison.Ordinal)))
            {
                Assert.Fail("Expected proof row not found: " + expected + Environment.NewLine + string.Join(Environment.NewLine, rows));
            }
        }
    }
}
