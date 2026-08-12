using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee1023VisualContourImportReadyTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee1023VisualContourImportReadyTests();
                tests.RuntimeDeclaresExternalVisualImportContract();
                tests.FallbackDoesNotPretendCodedContourIsFinal();
                tests.RequiredUiZoneIdsAreStable();
                tests.InvisibleHitboxesStillDriveSelection();
                Debug.Log("BEE-1023 visual contour import readiness checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-1023 visual contour import readiness checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void RuntimeDeclaresExternalVisualImportContract()
        {
            string[] rows = HiveViewProductUiPresenter.VisualContourImportStatusForProof();
            AssertRow(rows, "visual_contour_import_schema:bee-hive-visual-contours-v1");
            AssertRow(rows, "visual_contour_resource_path:BeeKingdom/HiveVisualContours");
            AssertRow(rows, "visual_contour_expected_asset_path:Assets/BeeKingdom/Playground/Resources/BeeKingdom/HiveVisualContours.json");
            AssertRow(rows, "visual_contour_coordinate_space:normalized_0_1_reference_hive_art");
            AssertRow(rows, "fallback_final_visual_contour:false");
        }

        [Test]
        public void FallbackDoesNotPretendCodedContourIsFinal()
        {
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("wax_workshop");
            string[] rows = HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof();
            AssertRow(rows, "visual_contour_source:none_waiting_ui_import");
            AssertRow(rows, "external_visual_contour_loaded:false");
            AssertRow(rows, "fallback_final_visual_contour:false");
            AssertRow(rows, "coded_guess_visual_contour_final:false");
            AssertRow(rows, "technical_calibration_used_for_hitbox:true");
            AssertRow(rows, "organic_wax_boundary_contours:false");
            AssertRow(rows, "technical_yellow_polygon_replaced:false");
        }

        [Test]
        public void RequiredUiZoneIdsAreStable()
        {
            string[] required = HiveVisualContourImportRuntime.RequiredZoneIds();
            Assert.That(required, Does.Contain("warehouse_cells"));
            Assert.That(required, Does.Contain("wax_workshop"));
            Assert.That(required, Does.Contain("administration_core"));
            Assert.That(required, Does.Contain("honey_storage"));
            Assert.That(required, Does.Contain("nursery_cluster"));
            Assert.That(required, Does.Contain("guard_post"));
            Assert.That(required, Does.Contain("research_node"));
            Assert.That(required, Does.Contain("genetics_garden"));
        }

        [Test]
        public void InvisibleHitboxesStillDriveSelection()
        {
            Assert.That(HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(620f, 570f), Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Transformation"));
            Assert.That(HiveViewProductUiPresenter.GetReferenceHotspotTactileHitboxForProof("wax_workshop").Length, Is.GreaterThanOrEqualTo(8));
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
