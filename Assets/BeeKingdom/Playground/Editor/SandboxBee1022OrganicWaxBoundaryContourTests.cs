using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee1022OrganicWaxBoundaryContourTests
    {
        private static readonly string[] PriorityOrganicZones =
        {
            "warehouse_cells",
            "wax_workshop",
            "administration_core",
            "honey_storage",
            "nursery_cluster",
            "guard_post",
            "research_node",
            "genetics_garden"
        };

        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee1022OrganicWaxBoundaryContourTests();
                tests.PriorityZonesUseDenseOrganicVisibleContours();
                tests.OrganicContoursRemainSeparateFromInvisibleTactileHitboxes();
                tests.RuntimeProofDeclaresUserReferenceCorrection();
                tests.OrganicContoursPreserveSelectionAndDemo078Scope();
                Debug.Log("BEE-1022 organic wax boundary contour checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-1022 organic wax boundary contour checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void PriorityZonesUseDenseOrganicVisibleContours()
        {
            foreach (string hotspotId in PriorityOrganicZones)
            {
                Vector2[] hitbox = HiveViewProductUiPresenter.GetReferenceHotspotTactileHitboxForProof(hotspotId);
                Assert.That(hitbox.Length, Is.GreaterThanOrEqualTo(8), hotspotId + " must keep a comfortable invisible tactile hitbox.");
            }
        }

        [Test]
        public void OrganicContoursRemainSeparateFromInvisibleTactileHitboxes()
        {
            foreach (string hotspotId in PriorityOrganicZones)
            {
                Vector2[] hitbox = HiveViewProductUiPresenter.GetReferenceHotspotTactileHitboxForProof(hotspotId);
                Assert.That(hitbox.Length, Is.GreaterThanOrEqualTo(8));
            }

            string[] rows = HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof();
            AssertRow(rows, "hitbox_visible:false");
            AssertRow(rows, "visual_contour_source:none_waiting_ui_import");
            AssertRow(rows, "visual_outline_separate_from_tactile_hitbox:false");
            AssertRow(rows, "fallback_final_visual_contour:false");
        }

        [Test]
        public void RuntimeProofDeclaresUserReferenceCorrection()
        {
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("wax_workshop");
            string[] rows = HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof();
            AssertRow(rows, "organic_wax_boundary_contours:false");
            AssertRow(rows, "user_reference_blue_paint_direction:waiting_ui_b_source");
            AssertRow(rows, "technical_yellow_polygon_replaced:false");
            AssertRow(rows, "coded_guess_visual_contour_final:false");
            AssertRow(rows, "priority_organic_zones:warehouse_cells,wax_workshop,administration_core,honey_storage,nursery_cluster,guard_post,research_node,genetics_garden");
        }

        [Test]
        public void OrganicContoursPreserveSelectionAndDemo078Scope()
        {
            Assert.That(HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(620f, 570f), Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Transformation"));

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_upgrade_completion");
            string[] rows = HiveViewProductUiPresenter.PlayableHiveT0T8ScreenshotStateForProof("T4");
            AssertRow(rows, "surface:playable_hive_only");
            AssertRow(rows, "world_map_runtime_allowed:false");
            AssertRow(rows, "bee_881_implemented:false");
            AssertRow(rows, "official_server_live:false");
        }

        private static int CountLongSegments(Vector2[] polygon, float maxLength)
        {
            int count = 0;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (Vector2.Distance(polygon[i], polygon[(i + 1) % polygon.Length]) > maxLength) count++;
            }

            return count;
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
