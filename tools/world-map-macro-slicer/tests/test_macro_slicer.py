from __future__ import annotations

import hashlib
import io
import json
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

import numpy as np
from PIL import Image

from synthetic_master import MASTER_SIZE, create_synthetic_master, synthetic_feature_masks
from worldmap_macro_slicer.core import (
    CANONICAL_TILE_SIZE,
    GUTTER,
    GRID_SIZE,
    RUNTIME_TILE_SIZE,
    MacroSlicerError,
    _load_master,
    slice_master,
    verify_bundle,
)
from worldmap_macro_slicer.cli import main as cli_main


def _sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _tree_hashes(root: Path) -> dict[str, str]:
    return {
        path.relative_to(root).as_posix(): _sha256(path)
        for path in sorted(root.rglob("*"))
        if path.is_file()
    }


class MacroMasterSlicerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls._temporary = tempfile.TemporaryDirectory()
        cls.root = Path(cls._temporary.name)
        cls.master_path = cls.root / "synthetic_macro_master.png"
        create_synthetic_master(cls.master_path)
        cls.run1 = cls.root / "run1"
        cls.run2 = cls.root / "run2"
        cls.result1 = slice_master(cls.master_path, cls.run1, "synthetic-wave3-proof")
        cls.result2 = slice_master(cls.master_path, cls.run2, "synthetic-wave3-proof")

    @classmethod
    def tearDownClass(cls):
        cls._temporary.cleanup()

    def _issue_codes(self, result):
        return {issue["code"] for issue in result["issues"]}

    def test_01_canonical_reconstruction_is_exact(self):
        self.assertEqual("PASS", self.result1["status"])
        self.assertEqual(0, self.result1["canonical"]["reconstruction_pixel_difference_count"])
        self.assertEqual(0, self.result1["canonical"]["reconstruction_file_pixel_difference_count"])
        self.assertEqual(0, self.result1["canonical"]["pixel_alteration_count"])
        self.assertEqual(25, self.result1["canonical"]["tile_count_actual"])
        self.assertEqual("YES", self.result1["verdicts"]["CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL"])

    def test_02_runtime_gutters_use_true_neighbors_across_all_40_boundaries(self):
        runtime = self.result1["runtime"]
        self.assertEqual(40, runtime["internal_boundaries_checked"])
        self.assertEqual(40, runtime["internal_boundaries_passed"])
        self.assertEqual(0, runtime["boundary_gutter_mismatch_pixel_count"])
        self.assertEqual(0, runtime["gutter_mismatch_pixel_count"])
        self.assertEqual(0, runtime["interior_mismatch_pixel_count"])
        self.assertEqual("YES", self.result1["verdicts"]["RUNTIME_GUTTERS_FROM_TRUE_NEIGHBORS"])

        master, _ = _load_master(self.master_path)
        for row in range(GRID_SIZE):
            for column in range(GRID_SIZE):
                tile_id = f"R{row}C{column}"
                path = self.run1 / "runtime" / "tiles" / f"{tile_id}_g{GUTTER}.png"
                with Image.open(path) as image:
                    pixels = np.asarray(image, dtype=np.uint8)
                x0 = column * CANONICAL_TILE_SIZE
                y0 = row * CANONICAL_TILE_SIZE
                interior = pixels[
                    GUTTER : GUTTER + CANONICAL_TILE_SIZE,
                    GUTTER : GUTTER + CANONICAL_TILE_SIZE,
                    :,
                ]
                self.assertTrue(
                    np.array_equal(
                        interior,
                        master[y0 : y0 + CANONICAL_TILE_SIZE, x0 : x0 + CANONICAL_TILE_SIZE, :],
                    )
                )
                if column > 0:
                    self.assertFalse(np.array_equal(pixels[:, 0, :], pixels[:, 1, :]))
                if column < GRID_SIZE - 1:
                    self.assertFalse(np.array_equal(pixels[:, -1, :], pixels[:, -2, :]))
                if row > 0:
                    self.assertFalse(np.array_equal(pixels[0, :, :], pixels[1, :, :]))
                if row < GRID_SIZE - 1:
                    self.assertFalse(np.array_equal(pixels[-1, :, :], pixels[-2, :, :]))

    def test_03_synthetic_features_cross_every_internal_boundary(self):
        river, relief = synthetic_feature_masks()
        checked = 0
        for boundary in range(1, GRID_SIZE):
            x = boundary * CANONICAL_TILE_SIZE
            self.assertGreater(int(np.count_nonzero(river[:, x - 1 : x + 1])), 0)
            self.assertGreater(np.unique(relief[:, x - 1 : x + 1]).size, 1)
            checked += GRID_SIZE
        for boundary in range(1, GRID_SIZE):
            y = boundary * CANONICAL_TILE_SIZE
            self.assertGreater(int(np.count_nonzero(river[y - 1 : y + 1, :])), 0)
            self.assertGreater(np.unique(relief[y - 1 : y + 1, :]).size, 1)
            checked += GRID_SIZE
        self.assertEqual(40, checked)

    def test_04_run1_run2_are_byte_identical(self):
        hashes1 = _tree_hashes(self.run1)
        hashes2 = _tree_hashes(self.run2)
        self.assertEqual(hashes1, hashes2)
        self.assertEqual(54, len(hashes1))

    def test_05_manifests_have_exact_order_uv_and_outer_clamp_only(self):
        canonical = json.loads((self.run1 / "canonical" / "manifest.canonical.json").read_text(encoding="utf-8"))
        runtime = json.loads((self.run1 / "runtime" / "manifest.runtime.json").read_text(encoding="utf-8"))
        expected_ids = [f"R{row}C{column}" for row in range(5) for column in range(5)]
        self.assertEqual(expected_ids, canonical["tile_order"])
        self.assertEqual(expected_ids, runtime["tile_order"])
        self.assertEqual([entry["id"] for entry in canonical["tiles"]], expected_ids)
        self.assertEqual([entry["id"] for entry in runtime["tiles"]], expected_ids)
        for entry in runtime["tiles"]:
            row = entry["row"]
            column = entry["column"]
            self.assertEqual({"height": 516, "width": 516}, entry["dimensions"])
            self.assertEqual({"height": 512, "width": 512, "x": 2, "y": 2}, entry["inner_rect"])
            clamp = entry["clamp_pixels"]
            self.assertEqual(2 if column == 0 else 0, clamp["left"])
            self.assertEqual(2 if column == 4 else 0, clamp["right"])
            self.assertEqual(2 if row == 0 else 0, clamp["top"])
            self.assertEqual(2 if row == 4 else 0, clamp["bottom"])
            self.assertAlmostEqual(2 / 516, entry["uv_inner_normalized"]["u_min"])
            self.assertAlmostEqual(514 / 516, entry["uv_inner_normalized"]["u_max"])

    def test_06_invalid_dimensions_and_color_mode_are_refused(self):
        wrong_dimensions = self.root / "wrong_dimensions.png"
        Image.new("RGB", (2559, MASTER_SIZE), (1, 2, 3)).save(wrong_dimensions)
        with self.assertRaises(MacroSlicerError) as dimension_error:
            slice_master(wrong_dimensions, self.root / "wrong_dimensions_output")
        self.assertEqual("INVALID_DIMENSIONS", dimension_error.exception.code)

        wrong_mode = self.root / "wrong_mode.png"
        Image.new("L", (MASTER_SIZE, MASTER_SIZE), 127).save(wrong_mode)
        with self.assertRaises(MacroSlicerError) as mode_error:
            slice_master(wrong_mode, self.root / "wrong_mode_output")
        self.assertEqual("INVALID_COLOR_MODE", mode_error.exception.code)

        rgba_path = self.root / "rgba_master.png"
        create_synthetic_master(rgba_path, mode="RGBA")
        rgba_pixels, rgba_mode = _load_master(rgba_path)
        self.assertEqual("RGBA", rgba_mode)
        self.assertEqual((MASTER_SIZE, MASTER_SIZE, 4), rgba_pixels.shape)

    def test_07_missing_duplicate_hash_order_and_alteration_guards(self):
        canonical_tile = self.run1 / "canonical" / "tiles" / "R0C0.png"
        canonical_tile_bytes = canonical_tile.read_bytes()
        canonical_tile.unlink()
        try:
            result = verify_bundle(self.master_path, self.run1)
            self.assertIn("MISSING_CANONICAL_TILE", self._issue_codes(result))
            self.assertEqual("FAIL", result["status"])
        finally:
            canonical_tile.write_bytes(canonical_tile_bytes)

        duplicate_target = self.run1 / "canonical" / "tiles" / "R0C1.png"
        duplicate_target_bytes = duplicate_target.read_bytes()
        duplicate_target.write_bytes(canonical_tile_bytes)
        try:
            result = verify_bundle(self.master_path, self.run1)
            self.assertIn("DUPLICATE_CANONICAL_TILE", self._issue_codes(result))
            self.assertIn("CANONICAL_PIXEL_ALTERATION", self._issue_codes(result))
        finally:
            duplicate_target.write_bytes(duplicate_target_bytes)

        manifest_path = self.run1 / "canonical" / "manifest.canonical.json"
        manifest_bytes = manifest_path.read_bytes()
        manifest = json.loads(manifest_bytes.decode("utf-8"))
        manifest["tiles"][0]["png_sha256"] = "0" * 64
        manifest_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        try:
            result = verify_bundle(self.master_path, self.run1)
            self.assertIn("CANONICAL_HASH_MISMATCH", self._issue_codes(result))
        finally:
            manifest_path.write_bytes(manifest_bytes)

        manifest = json.loads(manifest_bytes.decode("utf-8"))
        manifest["tiles"][0], manifest["tiles"][1] = manifest["tiles"][1], manifest["tiles"][0]
        manifest_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        try:
            result = verify_bundle(self.master_path, self.run1)
            self.assertIn("CANONICAL_ORDER_MISMATCH", self._issue_codes(result))
        finally:
            manifest_path.write_bytes(manifest_bytes)

        runtime_manifest_path = self.run1 / "runtime" / "manifest.runtime.json"
        runtime_manifest_bytes = runtime_manifest_path.read_bytes()
        runtime_manifest = json.loads(runtime_manifest_bytes.decode("utf-8"))
        runtime_manifest["gutter"]["stretching"] = True
        runtime_manifest_path.write_text(
            json.dumps(runtime_manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8"
        )
        try:
            result = verify_bundle(self.master_path, self.run1)
            self.assertIn("RUNTIME_GUTTER_CONTRACT_MISMATCH", self._issue_codes(result))
        finally:
            runtime_manifest_path.write_bytes(runtime_manifest_bytes)

        runtime_target = self.run1 / "runtime" / "tiles" / "R2C2_g2.png"
        runtime_bytes = runtime_target.read_bytes()
        with Image.open(runtime_target) as image:
            altered = np.asarray(image, dtype=np.uint8).copy()
        altered[0, 0, 0] ^= 0xFF
        altered[GUTTER + 5, GUTTER + 7, 1] ^= 0x7F
        altered_image = Image.fromarray(altered)
        altered_image.save(runtime_target, format="PNG", compress_level=9, optimize=False)
        altered_image.close()
        try:
            result = verify_bundle(self.master_path, self.run1)
            codes = self._issue_codes(result)
            self.assertIn("RUNTIME_HASH_MISMATCH", codes)
            self.assertIn("RUNTIME_PIXEL_ALTERATION", codes)
            self.assertIn("INTERNAL_GUTTER_BOUNDARY_FAILURE", codes)
            self.assertEqual("NO", result["verdicts"]["RUNTIME_GUTTERS_FROM_TRUE_NEIGHBORS"])
        finally:
            runtime_target.write_bytes(runtime_bytes)

        restored = verify_bundle(self.master_path, self.run1)
        self.assertEqual("PASS", restored["status"])

    def test_08_cli_verify_reports_the_four_pass_verdicts(self):
        output = io.StringIO()
        with redirect_stdout(output):
            exit_code = cli_main(
                [
                    "verify",
                    "--input",
                    str(self.master_path),
                    "--bundle",
                    str(self.run1),
                ]
            )
        rendered = output.getvalue()
        self.assertEqual(0, exit_code)
        self.assertIn("WORLD_MAP_MACRO_SLICER_WAVE3 = PASS", rendered)
        self.assertIn("CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL = YES", rendered)
        self.assertIn("RUNTIME_GUTTERS_FROM_TRUE_NEIGHBORS = YES", rendered)
        self.assertIn("READY_FOR_UIB_WAVE3_MASTER_INGEST = YES", rendered)


if __name__ == "__main__":
    unittest.main()
