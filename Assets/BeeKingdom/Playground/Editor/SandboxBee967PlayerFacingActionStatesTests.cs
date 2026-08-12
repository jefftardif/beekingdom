using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee967PlayerFacingActionStatesTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee967PlayerFacingActionStatesTests();
                tests.ActionConfirmationsArePlayerFacing();
                tests.DisabledStatesAndRefusalRecoveryAreVisible();
                tests.UpgradeCompletionShowsLevelAndReward();
                tests.TrainingCompletionShowsDeltaCounterAndNextAction();
                tests.NonClaimsAndScopeArePreserved();
                Debug.Log("BEE-963-967 player-facing action state checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-963-967 player-facing action state checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void ActionConfirmationsArePlayerFacing()
        {
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_action_confirm_collect");
            string[] collectRows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            AssertRow(collectRows, "action_confirmation_visible:true");
            AssertContains(collectRows, "player_facing_primary:Collecte confirmee");
            AssertContains(collectRows, "player_facing_secondary:+840 miel");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_action_confirm_upgrade");
            string[] upgradeRows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            AssertRow(upgradeRows, "action_confirmation_visible:true");
            AssertContains(upgradeRows, "player_facing_primary:Amelioration confirmee");
            AssertContains(upgradeRows, "player_facing_secondary:");
            AssertContains(upgradeRows, "action_decision:pending");
        }

        [Test]
        public void DisabledStatesAndRefusalRecoveryAreVisible()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_disabled_insufficient_resources");
            string[] disabledRows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            AssertRow(disabledRows, "disabled_state_visible:true");
            AssertRow(disabledRows, "disabled_reason_visible:true");
            AssertContains(disabledRows, "disabled_reason:Miel insuffisant");
            AssertRow(disabledRows, "disabled_no_new_cost_debited:true");
            AssertRow(disabledRows, "disabled_no_timer_started:true");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_disabled_queue_busy");
            string[] queueRows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            AssertRow(queueRows, "disabled_state_visible:true");
            AssertContains(queueRows, "disabled_reason:File entrainement occupee");
            AssertRow(queueRows, "disabled_blocks_new_action:true");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_refusal_recovery");
            string[] refusalRows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            AssertRow(refusalRows, "refusal_recovery_visible:true");
            AssertContains(refusalRows, "refusal_cause:ressources insuffisantes");
            AssertContains(refusalRows, "refusal_next_step:Collecte du miel");
            AssertRow(refusalRows, "refusal_no_cost_debited:true");
        }

        [Test]
        public void UpgradeCompletionShowsLevelAndReward()
        {
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_upgrade_completion");
            string[] rows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            AssertRow(rows, "upgrade_completion_player_visible:true");
            AssertContains(rows, "upgrade_completed_hotspot:honey_storage");
            AssertContains(rows, "upgrade_level_before_inferred:");
            AssertContains(rows, "upgrade_level_after:");
            AssertContains(rows, "upgrade_reward:+1 niveau");
            AssertRow(rows, "upgrade_cost_spent_once:true");
        }

        [Test]
        public void TrainingCompletionShowsDeltaCounterAndNextAction()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_training_completion");
            string[] rows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            AssertRow(rows, "training_completion_player_visible:true");
            AssertRow(rows, "training_completed_type:Eclaireuses");
            AssertRow(rows, "training_delta:+6 Eclaireuses");
            AssertRow(rows, "training_eclaireuses_count:11");
            AssertRow(rows, "training_next_action:Inspecter armee locale ou former un nouveau groupe.");
            AssertRow(rows, "training_cost_spent_once:true");
        }

        [Test]
        public void NonClaimsAndScopeArePreserved()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_upgrade_completion");
            string[] rows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            AssertRow(rows, "surface:playable_hive_only");
            AssertRow(rows, "physical_device_proof:pending");
            AssertRow(rows, "local_demo_only:true");
            AssertRow(rows, "official_server_live:false");
            AssertRow(rows, "official_endpoint:false");
            AssertRow(rows, "official_save:false");
            AssertRow(rows, "official_economy:false");
            AssertRow(rows, "official_army_persistence:false");
            AssertRow(rows, "world_map_runtime_allowed:false");
            AssertRow(rows, "bee_881_implemented:false");
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
