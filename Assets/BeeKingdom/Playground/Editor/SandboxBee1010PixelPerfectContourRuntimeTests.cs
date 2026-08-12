using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee1010PixelPerfectContourRuntimeTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee1010PixelPerfectContourRuntimeTests();
                tests.InventoryCoversAllPlayableHiveZones();
                tests.VisualContourAndTactileHitboxAreSeparated();
                tests.RuntimeSelectionUsesCalibratedTactileHitbox();
                tests.MultiZonePriorityIsDeterministic();
                tests.ZoomPanAlignmentAndDemo078ProofsArePreserved();
                Debug.Log("BEE-1001/1007/1010 pixel-perfect contour runtime checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-1001/1007/1010 pixel-perfect contour runtime checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void InventoryCoversAllPlayableHiveZones()
        {
            string[] hotspotIds = HiveViewProductUiPresenter.GetReferenceHotspotIdsForProof();
            string[] inventory = HiveViewProductUiPresenter.PixelPerfectContourInventoryForProof();

            Assert.That(inventory.Length, Is.EqualTo(hotspotIds.Length));
            foreach (string id in hotspotIds)
            {
                Assert.That(inventory.Any(row => row.StartsWith(id + "|", StringComparison.Ordinal)), "Missing contour inventory row for " + id);
                Vector2[] hitbox = HiveViewProductUiPresenter.GetReferenceHotspotTactileHitboxForProof(id);
                Assert.That(hitbox.Length, Is.GreaterThanOrEqualTo(8), "Tactile hitbox too coarse for " + id);
            }
        }

        [Test]
        public void VisualContourAndTactileHitboxAreSeparated()
        {
            string[] rows = HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof();
            AssertRow(rows, "visual_contour_source:none_waiting_ui_import");
            AssertRow(rows, "external_visual_contour_loaded:false");
            AssertRow(rows, "fallback_final_visual_contour:false");
            AssertRow(rows, "coded_guess_visual_contour_final:false");
            AssertRow(rows, "technical_calibration_used_for_hitbox:true");
            AssertRow(rows, "visual_outline_separate_from_tactile_hitbox:false");
            AssertRow(rows, "hitbox_visible:false");
            AssertRow(rows, "generic_circle_halo_final:false");
        }

        [Test]
        public void RuntimeSelectionUsesCalibratedTactileHitbox()
        {
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            bool selected = HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(784f, 178f);

            Assert.That(selected, Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Reserve miel"));
            AssertRow(HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof(), "selected_hotspot:honey_storage");
        }

        [Test]
        public void MultiZonePriorityIsDeterministic()
        {
            string priority = HiveViewProductUiPresenter.PixelPerfectContourPriorityForProof(772f, 430f);
            Assert.That(priority, Does.StartWith("administration_core|P0|"));

            bool selected = HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(772f, 430f);
            Assert.That(selected, Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Administration"));
            AssertRow(HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof(), "multi_zone_priority_enabled:true");
        }

        [Test]
        public void ZoomPanAlignmentAndDemo078ProofsArePreserved()
        {
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("research_node");
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(1.32f);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(-42f, 18f);

            string[] rows = HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof();
            AssertRow(rows, "zoom_pan_alignment_source:same_reference_art_transform");
            AssertRow(rows, "menus_fixed_after_zoom_pan:true");
            AssertRow(rows, "demo078_t0_t8_preserved:true");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_upgrade_completion");
            string[] t4Rows = HiveViewProductUiPresenter.PlayableHiveT0T8ScreenshotStateForProof("T4");
            AssertRow(t4Rows, "frame_id:T4");
            AssertRow(t4Rows, "surface:playable_hive_only");
            AssertRow(t4Rows, "upgrade_completion_player_visible:true");
            AssertRow(t4Rows, "world_map_runtime_allowed:false");
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
