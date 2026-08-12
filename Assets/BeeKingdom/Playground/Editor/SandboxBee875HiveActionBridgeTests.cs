using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee875HiveActionBridgeTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee875HiveActionBridgeTests();
                tests.DevOnlyContractsSnapshotAndNonClaimsAreReflected();
                tests.AcceptedRejectedPendingAndServerRequiredStatesAreVisible();
                tests.RejectionCatalogAndConflictPrepAreDocumented();
                tests.PreviousActionLoopGuardsRemainIntact();
                Debug.Log("BEE-861-875 hive action bridge checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-861-875 hive action bridge checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void DevOnlyContractsSnapshotAndNonClaimsAreReflected()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("server_bridge_pending");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveDevOnlyBridgeForProof();

            AssertRow(rows, "bee_861_875_scope:hive_action_loop_dev_only_bridge_action_states");
            AssertRow(rows, "server_043_contracts_reflected:true");
            AssertRow(rows, "resource_command_dev_only:true");
            AssertRow(rows, "upgrade_command_dev_only:true");
            AssertRow(rows, "training_command_dev_only:true");
            AssertRow(rows, "snapshot_strategy_prep:future_server_snapshot_only_no_save");
            AssertContains(rows, "snapshot_version:hive-preview-v1");
            AssertContains(rows, "snapshot_revision:");
            AssertRow(rows, "official_endpoint_active:false");
            AssertRow(rows, "official_server_live:false");
            AssertRow(rows, "official_save:false");
            AssertRow(rows, "official_economy:false");
            AssertRow(rows, "official_army_persistence:false");
            AssertRow(rows, "world_map_runtime_allowed:false");
            AssertRow(rows, "bee_881_implemented:false");
        }

        [Test]
        public void AcceptedRejectedPendingAndServerRequiredStatesAreVisible()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("server_bridge_accepted");
            AssertRow(HiveViewProductUiPresenter.PlayableHiveDevOnlyBridgeForProof(), "action_decision:accepted");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("server_bridge_rejected");
            string[] rejectedRows = HiveViewProductUiPresenter.PlayableHiveDevOnlyBridgeForProof();
            AssertRow(rejectedRows, "action_decision:rejected");
            AssertRow(rejectedRows, "action_rejection_code:insufficient_resources");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("server_bridge_pending");
            AssertRow(HiveViewProductUiPresenter.PlayableHiveDevOnlyBridgeForProof(), "action_decision:pending");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("server_required");
            string[] serverRequiredRows = HiveViewProductUiPresenter.PlayableHiveDevOnlyBridgeForProof();
            AssertRow(serverRequiredRows, "action_decision:server_required");
            AssertRow(serverRequiredRows, "action_rejection_code:server_required");
        }

        [Test]
        public void RejectionCatalogAndConflictPrepAreDocumented()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("stale_snapshot_conflict");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveDevOnlyBridgeForProof();

            AssertRow(rows, "action_decision:rejected");
            AssertRow(rows, "action_rejection_code:stale_snapshot_conflict");
            AssertContains(rows, "reconciliation_boundary:conflict_detected_dev_only_no_restore");
            AssertContains(rows, "rejection_catalog:insufficient_resources,already_running,queue_busy,cap_reached,stale_snapshot,conflict,server_required");
            AssertContains(rows, "action_timeline_full:T0 avant > T1 tap > T2 decision > T3 timer/file > T4 resultat > T5 non-claim");
        }

        [Test]
        public void PreviousActionLoopGuardsRemainIntact()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("double_upgrade_guard");
            string[] deterministicRows = HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof();
            string[] actionRows = HiveViewProductUiPresenter.PlayableHiveActionLoopForProof();

            AssertRow(deterministicRows, "upgrade_double_tap_guard:True");
            AssertRow(deterministicRows, "training_double_tap_guard:True");
            AssertRow(actionRows, "resource_ticks_visible:true");
            AssertRow(actionRows, "upgrade_anti_double_server_prep:true");
            AssertRow(actionRows, "training_anti_double_queue_guard:true");
            AssertRow(actionRows, "official_save_economy_army:false");
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
