using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee905910TargetedFixTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee905910TargetedFixTests();
                tests.TrainingArrivalExportMatchesRuntimeState();
                tests.UiButtonGestureBlockingIsExplicitlyProven();
                tests.TargetedManifestDoesNotReintroduceQa073Contradictions();
                Debug.Log("BEE-905/910 targeted reserve fix checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-905/910 targeted reserve fix checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void TrainingArrivalExportMatchesRuntimeState()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("training_eclaireuses_done");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof();
            string[] stabilizationRows = HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof();

            AssertRow(rows, "training_arrival_visible:true");
            AssertRow(rows, "training_delta:+6 Eclaireuses");
            AssertContains(rows, "local_army_counts:Soldats 18 / Gardiennes 8 / Eclaireuses 11");
            AssertRow(stabilizationRows, "training_arrival_visible:true");
            AssertContains(stabilizationRows, "local_army_snapshot:Soldats 18 / Gardiennes 8 / Eclaireuses 11");
            AssertNoRow(rows, "training_arrival_visible:false");
            AssertNoRow(rows, "training_delta:none");
            AssertNoRow(stabilizationRows, "local_army_snapshot:Soldats 18 / Gardiennes 8 / Eclaireuses 5");
        }

        [Test]
        public void UiButtonGestureBlockingIsExplicitlyProven()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("ui_gesture_blocked");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof();
            string[] gestureRows = HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof();

            AssertRow(rows, "gesture_ui_blocks_hive:True");
            AssertRow(gestureRows, "fixed_ui_blocks_hive_gesture:True");
            AssertRow(gestureRows, "gesture_mode:ui-blocked-touch");
            AssertRow(gestureRows, "pan_delta:0,0");
            AssertRow(gestureRows, "pinch_delta:0");
            AssertRow(gestureRows, "hud_fixed:true");
            AssertRow(gestureRows, "panels_fixed:true");
            AssertRow(gestureRows, "navigation_fixed:true");
            AssertNoRow(rows, "gesture_ui_blocks_hive:False");
            AssertNoRow(gestureRows, "fixed_ui_blocks_hive_gesture:False");
        }

        [Test]
        public void TargetedManifestDoesNotReintroduceQa073Contradictions()
        {
            string manifest = SandboxBee905910TargetedFixExport.BuildManifestForProof();

            StringAssert.Contains("training_arrival_visible:true", manifest);
            StringAssert.Contains("training_delta:+6 Eclaireuses", manifest);
            StringAssert.Contains("Eclaireuses 11", manifest);
            StringAssert.Contains("gesture_ui_blocks_hive:True", manifest);
            StringAssert.Contains("fixed_ui_blocks_hive_gesture:True", manifest);
            Assert.That(manifest, Does.Not.Contain("training_arrival_visible:false"));
            Assert.That(manifest, Does.Not.Contain("training_delta:none"));
            Assert.That(manifest, Does.Not.Contain("Eclaireuses 5"));
            Assert.That(manifest, Does.Not.Contain("gesture_ui_blocks_hive:False"));
            Assert.That(manifest, Does.Not.Contain("fixed_ui_blocks_hive_gesture:False"));
        }

        private static void AssertRow(string[] rows, string expected)
        {
            if (!rows.Any(row => string.Equals(row, expected, StringComparison.Ordinal)))
            {
                Assert.Fail("Expected proof row not found: " + expected + Environment.NewLine + string.Join(Environment.NewLine, rows));
            }
        }

        private static void AssertNoRow(string[] rows, string forbidden)
        {
            if (rows.Any(row => string.Equals(row, forbidden, StringComparison.Ordinal)))
            {
                Assert.Fail("Forbidden proof row found: " + forbidden + Environment.NewLine + string.Join(Environment.NewLine, rows));
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
