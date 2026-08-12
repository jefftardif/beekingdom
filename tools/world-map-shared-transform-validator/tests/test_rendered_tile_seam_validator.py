from __future__ import annotations

import unittest

import numpy as np

from rendered_tile_seam_validator import (
    audit_renderer_source,
    audit_zoom_telemetry,
    measure_expected_boundary,
    measure_line,
    metric_is_blocking,
    parse_required_size,
    parse_size,
)


class RenderedTileSeamValidatorTests(unittest.TestCase):
    def setUp(self) -> None:
        self.luma = np.full((160, 200), 110.0, dtype=np.float32)
        self.terrain = np.ones_like(self.luma, dtype=bool)

    def test_uniform_terrain_passes(self) -> None:
        metric = measure_expected_boundary(self.luma, self.terrain, "horizontal", 80.0)
        self.assertEqual(metric["status"], "PASS")
        self.assertFalse(metric_is_blocking(metric))

    def test_black_horizontal_seam_is_rejected(self) -> None:
        self.luma[80, :] = 3.5
        metric = measure_expected_boundary(self.luma, self.terrain, "horizontal", 80.0)
        self.assertEqual(metric["status"], "FAIL")
        self.assertLess(metric["dark_ratio"], 0.05)
        self.assertGreater(metric["coherent_dark_fraction"], 0.99)

    def test_black_vertical_seam_is_rejected(self) -> None:
        self.luma[:, 120] = 4.0
        metric = measure_expected_boundary(self.luma, self.terrain, "vertical", 120.0)
        self.assertEqual(metric["status"], "FAIL")
        self.assertGreater(metric["luminance_drop"], 100.0)

    def test_hud_mask_prevents_false_positive(self) -> None:
        self.luma[80, :] = 3.5
        self.terrain[70:90, :] = False
        metric = measure_line(self.luma, self.terrain, "horizontal", 80)
        self.assertEqual(metric["status"], "SKIP")
        self.assertEqual(metric["reason"], "HUD_OR_INSUFFICIENT_TERRAIN")

    def test_screen_edge_is_skipped(self) -> None:
        metric = measure_line(self.luma, self.terrain, "vertical", 2)
        self.assertEqual(metric["status"], "SKIP")
        self.assertEqual(metric["reason"], "SCREEN_EDGE")

    def test_true_gutter_uv_is_not_a_camouflage_strip(self) -> None:
        import tempfile
        from pathlib import Path

        source = """
        private bool debugChunkOverlay;
        private void DrawWave3WorldTerrain() {
            GUI.DrawTextureWithTexCoords(rect, tile.Texture, tile.GutterUv, true);
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        private static readonly Rect InnerUv = Rect.MinMaxRect(2f / 516f, 2f / 516f, 514f / 516f, 514f / 516f);
        GutterUv = new Rect(0f, 0f, 1f, 1f);
        """
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "candidate.cs"
            path.write_text(source, encoding="utf-8")
            audit = audit_renderer_source(path)
        self.assertEqual(audit["status"], "PASS")
        self.assertTrue(audit["true_gutter_sampling"])
        self.assertTrue(audit["camouflage_strip_absent"])

    def test_mixed_resolution_parser(self) -> None:
        self.assertEqual(parse_size("1920x1080"), (1920, 1080))
        self.assertEqual(parse_size("720X1280"), (720, 1280))
        self.assertEqual(parse_required_size("1920x1080:6"), ((1920, 1080), 6))

    def test_terrain_repeat_is_rejected(self) -> None:
        import tempfile
        from pathlib import Path

        source = """
        private bool debugChunkOverlay;
        private void DrawWave3WorldTerrain() {
            float repeated = Mathf.Repeat(cameraCenter.x, 512f);
            GUI.DrawTextureWithTexCoords(rect, tile.Texture, tile.GutterUv, true);
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        GutterUv = new Rect(0f, 0f, 1f, 1f);
        """
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "candidate.cs"
            path.write_text(source, encoding="utf-8")
            audit = audit_renderer_source(path)
        self.assertEqual(audit["status"], "FAIL")
        self.assertFalse(audit["terrain_repeat_or_modulo_absent"])

    def test_zoom_telemetry_contract_passes_complete_evidence(self) -> None:
        import tempfile
        from pathlib import Path

        labels_and_zooms = [
            ("L13_ZOOM_IN_BEFORE", 1.0), ("L14_ZOOM_IN_MID", 1.1), ("L15_ZOOM_IN_AFTER", 1.21),
            ("L16_ZOOM_OUT_BEFORE", 1.09), ("L17_ZOOM_OUT_MID", 0.98), ("L18_ZOOM_OUT_AFTER", 0.81),
            ("P13_ZOOM_IN_BEFORE", 1.0), ("P14_ZOOM_IN_MID", 1.1), ("P15_ZOOM_IN_AFTER", 1.21),
            ("P16_ZOOM_OUT_BEFORE", 1.09), ("P17_ZOOM_OUT_MID", 0.98), ("P18_ZOOM_OUT_AFTER", 0.81),
        ]
        samples = []
        for label, zoom in labels_and_zooms:
            landscape = label.startswith("L")
            samples.append(
                {
                    "label": label,
                    "zoom": zoom,
                    "terrain_anchor": "560,400",
                    "entity_anchor": "560,400",
                    "overlay_anchor": "480,460",
                    "terrain_distance_to_pivot": 200.0 * zoom,
                    "entity_distance_to_pivot": 200.0 * zoom,
                    "overlay_distance_to_pivot": 120.0 * zoom,
                    "hud_rect": "14,12,760,108" if landscape else "8,8,704,104",
                    "hud_anchor_signature": "DDDDD",
                    "hud_ratio": 1.0,
                }
            )
        with tempfile.TemporaryDirectory() as directory:
            log_path = Path(directory) / "unity.log"
            log_path.write_text("Zoom proof completed cleanly.\n", encoding="utf-8")
            telemetry = {
                "proof_id": "WORLD_MAP_STEP5A_ZOOM_PROOF_HARNESS",
                "unity_log": str(log_path),
                "unity_exit_code": 0,
                "negative_test": {
                    "executed": True,
                    "zoom_before": 1.1,
                    "zoom_after": 1.1,
                    "terrain_distance_before": 220.0,
                    "terrain_distance_after": 220.0,
                    "entity_distance_before": 220.0,
                    "entity_distance_after": 220.0,
                    "overlay_distance_before": 132.0,
                    "overlay_distance_after": 132.0,
                    "observed_verdict": "FAIL",
                    "reason_code": "NO_ZOOM_DELTA",
                },
                "fresh_zoom_source_hash_match": True,
                "landscape_zoom_proof": True,
                "portrait_zoom_proof": True,
                "terrain_entity_shared_zoom": True,
                "hud_pixel_invariant": True,
                "visible_tile_seams": False,
                "grid_pattern_visible": False,
                "ready_for_demo_100_zoom_replacement": True,
                "samples": samples,
            }
            audit = audit_zoom_telemetry(telemetry)
        self.assertEqual(audit["status"], "PASS")
        self.assertEqual(audit["issue_codes"], [])
        self.assertLessEqual(audit["max_layer_zoom_ratio_relative_error"], 1e-8)

    def test_zoom_telemetry_rejects_pivot_overlay_and_unexecuted_negative(self) -> None:
        audit = audit_zoom_telemetry(
            {
                "proof_id": "WORLD_MAP_STEP5A_ZOOM_PROOF_HARNESS",
                "samples": [],
                "negative_unchanged_zoom_state_would_fail": True,
            }
        )
        self.assertEqual(audit["status"], "FAIL")
        self.assertIn("NEGATIVE_UNCHANGED_ZOOM_NOT_EXECUTED_OR_REJECTED", audit["issue_codes"])
        self.assertIn("ZOOM_TELEMETRY_SEQUENCE_INCOMPLETE", audit["issue_codes"])


if __name__ == "__main__":
    unittest.main()
