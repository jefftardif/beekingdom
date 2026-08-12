using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee930DailyHiveLoopTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee930DailyHiveLoopTests();
                tests.CollectResourcesIsVisibleAndNonOfficial();
                tests.UpgradeLoopShowsPendingCompletionAndSingleSpend();
                tests.TrainingAndArmyInspectionStayCoherent();
                tests.RefusalRecoveryDoesNotSpendOrStartOfficialProgress();
                tests.Qa074CorrectionsRemainPreserved();
                Debug.Log("BEE-925-930 daily hive loop checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-925-930 daily hive loop checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void CollectResourcesIsVisibleAndNonOfficial()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("daily_collect_done");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof();

            AssertRow(rows, "daily_step:collecte");
            AssertRow(rows, "collect_resources_visible:true");
            AssertContains(rows, "collect_feedback:+840 miel");
            AssertRow(rows, "official_economy:false");
            AssertRow(rows, "official_save:false");
        }

        [Test]
        public void UpgradeLoopShowsPendingCompletionAndSingleSpend()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("daily_upgrade_pending");
            string[] pendingRows = HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof();
            AssertRow(pendingRows, "upgrade_pending:true");
            AssertContains(pendingRows, "upgrade_progress:");
            AssertContains(pendingRows, "upgrade_cost_reserved:");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("daily_upgrade_complete");
            string[] completeRows = HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof();
            AssertRow(completeRows, "upgrade_completed:true");
            AssertRow(completeRows, "upgrade_cost_spent_once:true");
            AssertRow(completeRows, "single_spend_guard:true");
        }

        [Test]
        public void TrainingAndArmyInspectionStayCoherent()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("daily_training_pending");
            string[] pendingRows = HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof();
            AssertRow(pendingRows, "training_pending:true");
            AssertContains(pendingRows, "training_queue:File:");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("daily_training_complete");
            string[] completeRows = HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof();
            AssertRow(completeRows, "training_completed:true");
            AssertRow(completeRows, "training_delta:+6 Eclaireuses");
            AssertContains(completeRows, "local_army_counts:Soldats 18 / Gardiennes 8 / Eclaireuses 11");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("daily_army_inspect");
            string[] inspectRows = HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof();
            AssertRow(inspectRows, "inspect_local_army_visible:true");
            AssertRow(inspectRows, "local_army_non_persistent:false");
            AssertRow(inspectRows, "local_army_device_persistent:true");
            AssertRow(inspectRows, "local_army_official_authority:server");
        }

        [Test]
        public void RefusalRecoveryDoesNotSpendOrStartOfficialProgress()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("daily_refusal_recovery");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof();

            AssertRow(rows, "refusal_active:true");
            AssertRow(rows, "refusal_cause:ressources insuffisantes");
            AssertContains(rows, "refusal_next_step:Produis plus.");
            AssertRow(rows, "refusal_cost_debited:false_when_rejected");
            AssertRow(rows, "official_server_live:false");
            AssertRow(rows, "official_army_persistence:false");
        }

        [Test]
        public void Qa074CorrectionsRemainPreserved()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("daily_loop_complete");
            string[] dailyRows = HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof();
            AssertRow(dailyRows, "qa074_bee905_manifest_coherence_preserved:true");
            AssertRow(dailyRows, "qa074_bee910_ui_gesture_blocking_preserved:true");
            AssertRow(dailyRows, "world_map_runtime_allowed:false");
            AssertRow(dailyRows, "bee_881_implemented:false");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("training_eclaireuses_done");
            string[] bee905Rows = HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof();
            AssertRow(bee905Rows, "training_arrival_visible:true");
            AssertRow(bee905Rows, "training_delta:+6 Eclaireuses");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("ui_gesture_blocked");
            string[] bee910Rows = HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof();
            AssertRow(bee910Rows, "fixed_ui_blocks_hive_gesture:True");
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
