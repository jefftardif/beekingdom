using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee951ProductCoreTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee951ProductCoreTests();
                tests.SessionStartCollectAndCapacityAreClear();
                tests.UpgradeChoiceAndRewardAreClear();
                tests.TrainingChoiceCompletionAndArmyPanelAreClear();
                tests.Qa075Qa074AndNonClaimsArePreserved();
                Debug.Log("BEE-945-951 playable hive product core checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-945-951 playable hive product core checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void SessionStartCollectAndCapacityAreClear()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("product_session_start_collect");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            AssertRow(rows, "session_start_visible:true");
            AssertContains(rows, "session_prompt:Collecte du jour prete.");
            AssertContains(rows, "collect_feedback:+620 miel");
            AssertRow(rows, "official_economy:false");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("product_capacity_overflow");
            string[] overflowRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            AssertRow(overflowRows, "capacity_state:capacity_reached");
            AssertRow(overflowRows, "overflow_blocks_collect:true");
            AssertContains(overflowRows, "overflow_feedback:Overflow bloque");
            AssertContains(overflowRows, "recovery_guidance:Monte capacite.");
        }

        [Test]
        public void UpgradeChoiceAndRewardAreClear()
        {
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("product_upgrade_choice");
            string[] choiceRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            AssertRow(choiceRows, "upgrade_choice_visible:true");
            AssertContains(choiceRows, "upgrade_choice_summary:");
            AssertContains(choiceRows, "upgrade_cost_time:");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("product_upgrade_reward");
            string[] rewardRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            AssertRow(rewardRows, "upgrade_completion_reward_visible:true");
            AssertContains(rewardRows, "upgrade_reward:+1 niveau");
            AssertRow(rewardRows, "upgrade_cost_spent_once:true");
        }

        [Test]
        public void TrainingChoiceCompletionAndArmyPanelAreClear()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("product_training_choice");
            string[] choiceRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            AssertRow(choiceRows, "training_choice_visible:true");
            AssertContains(choiceRows, "training_availability:Soldats pret");
            AssertContains(choiceRows, "training_cost_time:");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("product_training_next_action");
            string[] completeRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            AssertRow(completeRows, "training_completion_visible:true");
            AssertRow(completeRows, "training_next_action:Inspecter armee locale ou former un nouveau groupe.");
            AssertContains(completeRows, "local_army_counts:Soldats 18 / Gardiennes 8 / Eclaireuses 11");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("product_army_panel");
            string[] panelRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            AssertRow(panelRows, "local_army_panel_visible:true");
            AssertRow(panelRows, "local_army_panel_mode:detail");
            AssertRow(panelRows, "local_army_non_persistent:false");
            AssertRow(panelRows, "local_army_device_persistent:true");
            AssertRow(panelRows, "local_army_official_authority:server");
            AssertContains(panelRows, "local_army_counts:Soldats 26 / Gardiennes 12 / Eclaireuses 11");
        }

        [Test]
        public void Qa075Qa074AndNonClaimsArePreserved()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("product_army_panel");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            AssertRow(rows, "qa075_daily_loop_preserved:true");
            AssertRow(rows, "qa074_bee905_manifest_coherence_preserved:true");
            AssertRow(rows, "qa074_bee910_ui_gesture_blocking_preserved:true");
            AssertRow(rows, "physical_device_proof:pending");
            AssertRow(rows, "official_server_live:false");
            AssertRow(rows, "official_save:false");
            AssertRow(rows, "world_map_runtime_allowed:false");
            AssertRow(rows, "bee_881_implemented:false");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("training_eclaireuses_done");
            AssertRow(HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof(), "training_delta:+6 Eclaireuses");
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("ui_gesture_blocked");
            AssertRow(HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof(), "fixed_ui_blocks_hive_gesture:True");
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
