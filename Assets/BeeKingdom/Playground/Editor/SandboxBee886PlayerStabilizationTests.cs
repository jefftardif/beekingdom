using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee886PlayerStabilizationTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee886PlayerStabilizationTests();
                tests.ProduceSpendLoopShowsResourceFeedbackAndNonClaims();
                tests.UpgradeReservationDecisionAndRapidTapRemainStable();
                tests.TrainingQueueArrivalAndLocalArmyRemainClear();
                tests.ActionSourceAndRejectionRecoveryAreExposed();
                Debug.Log("BEE-882-886 playable hive player stabilization checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-882-886 playable hive player stabilization checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void ProduceSpendLoopShowsResourceFeedbackAndNonClaims()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("resources_tick");
            string[] rows = HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof();

            AssertRow(rows, "bee_882_886_scope:playable_hive_action_loop_player_stabilization");
            AssertRow(rows, "produce_spend_sequence_stable:true");
            AssertRow(rows, "resource_ticks_visible:true");
            AssertContains(rows, "resource_delta_feedback:");
            AssertRow(rows, "server_live_claim:false");
            AssertRow(rows, "official_save:false");
            AssertRow(rows, "official_economy:false");
            AssertRow(rows, "official_army_persistence:false");
            AssertRow(rows, "world_map_runtime_allowed:false");
            AssertRow(rows, "bee_881_implemented:false");
        }

        [Test]
        public void UpgradeReservationDecisionAndRapidTapRemainStable()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("double_upgrade_guard");
            string[] rows = HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof();
            string[] deterministicRows = HiveViewProductUiPresenter.PlayableHiveDeterministicChecksForProof();

            AssertRow(rows, "reserved_cost_visible:true");
            AssertContains(rows, "reserved_cost:");
            AssertRow(rows, "cost_applied_once:true");
            AssertRow(rows, "upgrade_decision_feedback:true");
            AssertRow(rows, "action_decision:rejected");
            AssertRow(rows, "rejection_code:already_running");
            AssertContains(rows, "rejection_guidance:Attends");
            AssertRow(deterministicRows, "upgrade_double_tap_guard:True");
            AssertRow(deterministicRows, "level_increment_once:True");
        }

        [Test]
        public void TrainingQueueArrivalAndLocalArmyRemainClear()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("training_eclaireuses_done");
            string[] rows = HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof();
            string[] stateRows = HiveViewProductUiPresenter.PlayableHiveLoopStateForProof();

            AssertRow(rows, "training_queue_clarity:true");
            AssertRow(rows, "training_arrival_visible:true");
            AssertContains(rows, "training_queue:");
            AssertContains(rows, "local_army_snapshot:");
            AssertContains(stateRows, "army_feedback:+6 Eclaireuses");
            AssertRow(rows, "action_source_banner_visible:true");
        }

        [Test]
        public void ActionSourceAndRejectionRecoveryAreExposed()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("server_required");
            string[] serverRows = HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof();
            AssertRow(serverRows, "action_source:Serveur futur requis");
            AssertRow(serverRows, "rejection_code:server_required");
            AssertContains(serverRows, "rejection_guidance:Serveur futur");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("stale_snapshot_conflict");
            string[] conflictRows = HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof();
            AssertRow(conflictRows, "action_decision:rejected");
            AssertRow(conflictRows, "rejection_code:stale_snapshot_conflict");
            AssertContains(conflictRows, "rejection_guidance:Relance plus tard");
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
