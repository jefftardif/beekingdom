from __future__ import annotations

import hashlib
import json
import tempfile
import unittest
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from worldmap_validator.core import ValidationOptions, validate_content


class WorldMapContentValidatorTests(unittest.TestCase):
    def _wave3_base_pixels(self, tile_size: int = 64) -> np.ndarray:
        size = tile_size * 5
        y, x = np.mgrid[0:size, 0:size]
        pixels = np.zeros((size, size, 3), dtype=np.float64)
        pixels[:, :, 0] = 48 + x * 0.42 + 19 * np.sin(y / 11.0) + 12 * np.cos((x + 2 * y) / 23.0)
        pixels[:, :, 1] = 62 + y * 0.36 + 21 * np.cos(x / 17.0) + 10 * np.sin((2 * x - y) / 29.0)
        pixels[:, :, 2] = 112 + 43 * np.sin((x + y) / 31.0) + 17 * np.cos((x - 3 * y) / 37.0)
        return np.clip(pixels, 0, 255).astype(np.uint8)

    def _write_wave3_fixture(
        self,
        root: Path,
        pixels: np.ndarray,
        *,
        route_present: bool = False,
    ) -> dict[str, Path | int]:
        input_dir = root / "wave3"
        gutters_dir = root / "wave3_gutters"
        input_dir.mkdir(parents=True, exist_ok=True)
        gutters_dir.mkdir(parents=True, exist_ok=True)
        tile_size = pixels.shape[0] // 5
        master_path = input_dir / "atlas_master_wave3.png"
        Image.fromarray(pixels).save(master_path)
        rgba = np.asarray(Image.fromarray(pixels).convert("RGBA"), dtype=np.uint8)
        padded = np.pad(rgba, ((2, 2), (2, 2), (0, 0)), mode="edge")

        entries = []
        ids = {(x, y): f"wave3_x{x:04d}_y{y:04d}" for y in range(5) for x in range(5)}
        for tile_y in range(5):
            for tile_x in range(5):
                tile_id = ids[(tile_x, tile_y)]
                filename = f"{tile_id}.png"
                x0 = tile_x * tile_size
                y0 = tile_y * tile_size
                tile_path = input_dir / filename
                Image.fromarray(pixels[y0:y0 + tile_size, x0:x0 + tile_size]).save(tile_path)
                gutter_filename = f"{tile_id}_gutter.png"
                gutter_path = gutters_dir / gutter_filename
                gutter_pixels = padded[y0:y0 + tile_size + 4, x0:x0 + tile_size + 4]
                Image.fromarray(gutter_pixels).save(gutter_path)
                entries.append(
                    {
                        "id": tile_id,
                        "tile_x": tile_x,
                        "tile_y": tile_y,
                        "file": filename,
                        "sha256": hashlib.sha256(tile_path.read_bytes()).hexdigest(),
                        "stored_dimensions": {"width": tile_size, "height": tile_size},
                        "source_rect": {
                            "x": x0,
                            "y": y0,
                            "width": tile_size,
                            "height": tile_size,
                        },
                        "neighbors": {
                            "n": ids.get((tile_x, tile_y - 1)),
                            "e": ids.get((tile_x + 1, tile_y)),
                            "s": ids.get((tile_x, tile_y + 1)),
                            "w": ids.get((tile_x - 1, tile_y)),
                        },
                        "runtime_gutter": {
                            "file": gutter_filename,
                            "sha256": hashlib.sha256(gutter_path.read_bytes()).hexdigest(),
                            "dimensions": {"width": tile_size + 4, "height": tile_size + 4},
                        },
                    }
                )
        manifest = {
            "schema": "bee-kingdom.world-map-continuous-master.v1",
            "grid": {"columns": 5, "rows": 5, "expected_count": 25},
            "master": {
                "file": master_path.name,
                "sha256": hashlib.sha256(master_path.read_bytes()).hexdigest(),
                "dimensions": {"width": tile_size * 5, "height": tile_size * 5},
            },
            "tiles": entries,
        }
        manifest_path = input_dir / "manifest.json"
        manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

        forbidden = {
            "schema": "bee-kingdom.world-map-forbidden-content-review.v1",
            "inspector": "fixture-human",
            "inspected_at_utc": "2026-07-14T00:00:00Z",
            "source_artifact": str(master_path),
            "categories": {
                key: {"status": "PRESENT" if route_present and key == "ground_routes" else "ABSENT"}
                for key in (
                    "ground_routes",
                    "player_hives",
                    "runtime_resources",
                    "troops",
                    "painted_flight_paths",
                    "ui_or_text",
                    "painted_tile_boundaries",
                )
            },
        }
        forbidden_path = root / "wave3_forbidden_review.json"
        forbidden_path.write_text(json.dumps(forbidden, indent=2), encoding="utf-8")

        perceptual = {
            "schema": "bee-kingdom.world-map-perceptual-continuity-review.v2",
            "inspector": "fixture-human",
            "inspected_at_utc": "2026-07-14T00:00:00Z",
            "signature": {
                "reviewer": "fixture-human",
                "role": "Builder-C",
                "signed_at_utc": "2026-07-14T00:00:00Z",
                "decision": "PASS",
            },
            "source_artifacts": ["perceptual_multiscale_sheet.png"],
            "categories": {
                key: {"status": "NO"}
                for key in (
                    "grid_lines_visible",
                    "central_square_visible",
                    "outer_ring_visible",
                    "checkerboard_visible",
                    "blurred_bands_visible",
                    "mirrored_motifs_visible",
                    "repeated_tile_motifs",
                    "river_discontinuity",
                    "relief_discontinuity",
                    "forest_discontinuity",
                    "biome_boundary_rectilinear",
                )
            },
        }
        perceptual_path = root / "wave3_perceptual_review.json"
        perceptual_path.write_text(json.dumps(perceptual, indent=2), encoding="utf-8")
        return {
            "input_dir": input_dir,
            "gutters_dir": gutters_dir,
            "master": master_path,
            "manifest": manifest_path,
            "forbidden": forbidden_path,
            "perceptual": perceptual_path,
            "tile_size": tile_size,
        }

    def _validate_wave3(self, root: Path, fixture: dict[str, Path | int]):
        tile_size = int(fixture["tile_size"])
        return validate_content(
            ValidationOptions(
                input_dir=Path(fixture["input_dir"]),
                output_dir=root / "wave3_output",
                manifest_path=Path(fixture["manifest"]),
                expected_count=25,
                columns=5,
                rows=5,
                label="wave3-continuous-fixture",
                reference_atlas_path=Path(fixture["master"]),
                expected_seam_count=40,
                required_tile_width=tile_size,
                required_tile_height=tile_size,
                forbidden_review_path=Path(fixture["forbidden"]),
                require_forbidden_review=True,
                perceptual_review_path=Path(fixture["perceptual"]),
                require_perceptual_review=True,
                require_signed_perceptual_review=True,
                gutters_dir=Path(fixture["gutters_dir"]),
                gutter_size=2,
                require_gutters=True,
                required_master_width=tile_size * 5,
                required_master_height=tile_size * 5,
            )
        )

    def _make_fixture(self, root: Path) -> Path:
        input_dir = root / "input"
        input_dir.mkdir(parents=True)
        width, height = 128, 96
        y, x = np.mgrid[0:height, 0:width]
        pixels = np.zeros((height, width, 3), dtype=np.uint8)
        pixels[:, :, 0] = (x * 2 + y) % 256
        pixels[:, :, 1] = (y * 3 + x // 2 + 35) % 256
        pixels[:, :, 2] = np.clip(118 + 62 * np.sin(x / 13.0) + 48 * np.cos(y / 11.0), 0, 255)
        atlas = Image.fromarray(pixels)
        tile_width, tile_height = width // 2, height // 2
        for tile_y in range(2):
            for tile_x in range(2):
                tile = atlas.crop(
                    (
                        tile_x * tile_width,
                        tile_y * tile_height,
                        (tile_x + 1) * tile_width,
                        (tile_y + 1) * tile_height,
                    )
                )
                tile.save(input_dir / f"fixture_x{tile_x:04d}_y{tile_y:04d}.png")
        return input_dir

    def _validate(self, root: Path, input_dir: Path):
        return validate_content(
            ValidationOptions(
                input_dir=input_dir,
                output_dir=root / "output",
                expected_count=4,
                columns=2,
                rows=2,
                label="fixture-test",
            )
        )

    def _make_wave2_fixture(self, root: Path):
        input_dir = root / "wave2"
        baseline_dir = root / "wave1"
        input_dir.mkdir(parents=True)
        baseline_dir.mkdir(parents=True)
        tile_size = 32
        size = tile_size * 5
        y, x = np.mgrid[0:size, 0:size]
        pixels = np.zeros((size, size, 3), dtype=np.uint8)
        pixels[:, :, 0] = np.clip(38 + x * 1.15 + 26 * np.sin(y / 9.0), 0, 255)
        pixels[:, :, 1] = np.clip(42 + y * 1.05 + 34 * np.cos(x / 13.0), 0, 255)
        pixels[:, :, 2] = np.clip(116 + 51 * np.sin((x + y) / 17.0), 0, 255)
        atlas = Image.fromarray(pixels.astype(np.uint8))
        reference = input_dir / "atlas_master_5x5.png"
        atlas.save(reference)
        for tile_y in range(5):
            for tile_x in range(5):
                tile = atlas.crop(
                    (
                        tile_x * tile_size,
                        tile_y * tile_size,
                        (tile_x + 1) * tile_size,
                        (tile_y + 1) * tile_size,
                    )
                )
                target = input_dir / f"sector_x{tile_x:04d}_y{tile_y:04d}.png"
                tile.save(target)
                if 1 <= tile_x <= 3 and 1 <= tile_y <= 3:
                    baseline = baseline_dir / f"sector_x{tile_x - 1:04d}_y{tile_y - 1:04d}.png"
                    baseline.write_bytes(target.read_bytes())
        review = {
            "schema": "bee-kingdom.world-map-forbidden-content-review.v1",
            "inspector": "automated-fixture",
            "inspected_at_utc": "2026-07-13T00:00:00Z",
            "source_artifact": str(reference),
            "categories": {
                "ground_routes": {"status": "ABSENT"},
                "player_hives": {"status": "ABSENT"},
                "runtime_resources": {"status": "ABSENT"},
                "troops": {"status": "ABSENT"},
                "painted_flight_paths": {"status": "ABSENT"},
                "ui_or_text": {"status": "ABSENT"},
                "painted_tile_boundaries": {"status": "ABSENT"},
            },
        }
        review_path = root / "forbidden_review.json"
        review_path.write_text(json.dumps(review), encoding="utf-8")
        return input_dir, baseline_dir, reference, review_path, tile_size

    def _validate_wave2(self, root: Path, fixture, perceptual_statuses=None):
        input_dir, baseline_dir, reference, review_path, tile_size = fixture
        statuses = {
            "grid_lines_visible": "NO",
            "central_square_visible": "NO",
            "outer_ring_visible": "NO",
            "checkerboard_visible": "NO",
            "blurred_bands_visible": "NO",
            "mirrored_motifs_visible": "NO",
            "repeated_tile_motifs": "NO",
            "river_discontinuity": "NO",
            "relief_discontinuity": "NO",
            "forest_discontinuity": "NO",
            "biome_boundary_rectilinear": "NO",
        }
        statuses.update(perceptual_statuses or {})
        perceptual_review = {
            "schema": "bee-kingdom.world-map-perceptual-continuity-review.v1",
            "inspector": "automated-fixture",
            "inspected_at_utc": "2026-07-13T00:00:00Z",
            "source_artifacts": [
                "perceptual_mosaic_100.png",
                "perceptual_mosaic_50.png",
                "perceptual_mosaic_25.png",
                "perceptual_contrast_enhanced.png",
            ],
            "categories": {key: {"status": status} for key, status in statuses.items()},
        }
        perceptual_review_path = root / "perceptual_review.json"
        perceptual_review_path.write_text(json.dumps(perceptual_review), encoding="utf-8")
        return validate_content(
            ValidationOptions(
                input_dir=input_dir,
                output_dir=root / "output",
                expected_count=25,
                columns=5,
                rows=5,
                label="wave2-5x5-fixture",
                reference_atlas_path=reference,
                baseline_center_dir=baseline_dir,
                expected_new_ring_count=16,
                expected_seam_count=40,
                required_tile_width=tile_size,
                required_tile_height=tile_size,
                forbidden_review_path=review_path,
                require_forbidden_review=True,
                perceptual_review_path=perceptual_review_path,
                require_perceptual_review=True,
            )
        )

    def test_continuous_grid_passes_and_generates_artifacts(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            report = self._validate(root, self._make_fixture(root))
            self.assertEqual("PASS", report["overall_status"])
            self.assertTrue(all(seam["status"] == "PASS" for seam in report["seams"]))
            for filename in (
                "validation.json",
                "report.md",
                "contact_sheet.png",
                "reconstruction.png",
                "seam_heatmap.png",
            ):
                self.assertTrue((root / "output" / filename).is_file(), filename)

    def test_visible_seam_is_failed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            input_dir = self._make_fixture(root)
            target = input_dir / "fixture_x0001_y0000.png"
            with Image.open(target) as source:
                shifted = np.asarray(source.convert("RGB"), dtype=np.int16).copy()
            shifted[:, :, 0] = 255 - shifted[:, :, 0]
            shifted[:, :, 1] = np.clip(shifted[:, :, 1] + 110, 0, 255)
            Image.fromarray(shifted.astype(np.uint8)).save(target)
            report = self._validate(root, input_dir)
            self.assertEqual("FAIL", report["overall_status"])
            self.assertTrue(any(seam["status"] == "FAIL" for seam in report["seams"]))

    def test_exact_duplicate_is_failed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            input_dir = self._make_fixture(root)
            source = input_dir / "fixture_x0000_y0000.png"
            duplicate = input_dir / "fixture_x0001_y0001.png"
            duplicate.write_bytes(source.read_bytes())
            report = self._validate(root, input_dir)
            self.assertEqual("FAIL", report["overall_status"])
            self.assertGreaterEqual(len(report["duplicates"]["exact"]), 1)

    def test_missing_grid_position_is_failed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            input_dir = self._make_fixture(root)
            (input_dir / "fixture_x0001_y0001.png").unlink()
            report = self._validate(root, input_dir)
            self.assertEqual("FAIL", report["overall_status"])
            count_check = next(check for check in report["checks"] if check["id"] == "expected_count")
            grid_check = next(check for check in report["checks"] if check["id"] == "grid_completeness")
            self.assertEqual("FAIL", count_check["status"])
            self.assertEqual("FAIL", grid_check["status"])

    def test_unreadable_png_is_failed_without_crashing_reports(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            input_dir = self._make_fixture(root)
            (input_dir / "fixture_x0001_y0001.png").write_bytes(b"not-a-png")
            report = self._validate(root, input_dir)
            self.assertEqual("FAIL", report["overall_status"])
            readability = next(check for check in report["checks"] if check["id"] == "readability")
            self.assertEqual("FAIL", readability["status"])
            self.assertTrue((root / "output" / "contact_sheet.png").is_file())

    def test_manifest_uppercase_neighbors_and_nested_hashes_are_supported(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            input_dir = self._make_fixture(root)
            ids = {(0, 0): "NW", (1, 0): "NE", (0, 1): "SW", (1, 1): "SE"}
            entries = []
            for (x, y), tile_id in ids.items():
                filename = f"fixture_x{x:04d}_y{y:04d}.png"
                payload = (input_dir / filename).read_bytes()
                neighbors = {
                    "N": ids.get((x, y - 1)),
                    "E": ids.get((x + 1, y)),
                    "S": ids.get((x, y + 1)),
                    "W": ids.get((x - 1, y)),
                }
                entries.append(
                    {
                        "id": tile_id,
                        "row": y,
                        "column": x,
                        "file": filename,
                        "width": 64,
                        "height": 48,
                        "neighbors": neighbors,
                        "hash": {
                            "algorithm": "SHA-256",
                            "value": hashlib.sha256(payload).hexdigest(),
                        },
                    }
                )
            manifest = {"grid": {"rows": 2, "columns": 2}, "sectors": entries}
            (input_dir / "manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
            report = validate_content(
                ValidationOptions(
                    input_dir=input_dir,
                    output_dir=root / "output",
                    label="manifest-fixture",
                )
            )
            self.assertEqual("PASS", report["overall_status"])
            neighbor_check = next(check for check in report["checks"] if check["id"] == "manifest_neighbors")
            hash_check = next(check for check in report["checks"] if check["id"] == "hash_integrity")
            self.assertEqual("PASS", neighbor_check["status"])
            self.assertEqual("PASS", hash_check["status"])

    def test_wave2_5x5_contract_locks_center_ring_seams_and_reconstruction(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            report = self._validate_wave2(root, self._make_wave2_fixture(root))
            self.assertEqual("PASS", report["overall_status"])
            self.assertEqual(16, report["ring"]["actual_ring_count"])
            self.assertEqual(9, report["center_lock"]["match_count"])
            self.assertEqual(40, report["seam_statistics"]["actual_count"])
            self.assertEqual(0, report["coverage"]["hole_area"])
            self.assertEqual(0, report["coverage"]["overlap_area"])
            self.assertTrue(report["reconstruction"]["source_comparison"]["pixel_identical"])
            self.assertEqual("PASS", report["continuity_gates"]["technical_continuity"]["status"])
            self.assertEqual(
                "PASS",
                report["continuity_gates"]["technical_continuity"]["components"]["center_ring_12_of_12"],
            )
            self.assertEqual("PASS", report["continuity_gates"]["perceptual_continuity"]["status"])
            self.assertEqual("NO", report["continuity_gates"]["grid_pattern_visible"])
            self.assertEqual("PASS", report["low_frequency_grid"]["center_ring_salience"]["status"])
            self.assertEqual(12, report["low_frequency_grid"]["center_ring_salience"]["center_ring_segment_count"])
            self.assertTrue((root / "output" / "qa_grid.png").is_file())
            for filename in (
                "perceptual_mosaic_100.png",
                "perceptual_mosaic_50.png",
                "perceptual_mosaic_25.png",
                "perceptual_contrast_enhanced.png",
                "perceptual_multiscale_sheet.png",
            ):
                self.assertTrue((root / "output" / filename).is_file(), filename)

    def test_wave2_center_hash_drift_is_failed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            fixture = self._make_wave2_fixture(root)
            target = fixture[0] / "sector_x0002_y0002.png"
            with Image.open(target) as source:
                changed = np.asarray(source.convert("RGB"), dtype=np.uint8).copy()
            changed[5, 5, 0] ^= 0xFF
            Image.fromarray(changed).save(target)
            report = self._validate_wave2(root, fixture)
            self.assertEqual("FAIL", report["overall_status"])
            self.assertEqual("FAIL", report["center_lock"]["status"])
            self.assertEqual(1, report["center_lock"]["mismatch_count"])

    def test_wave2_missing_ring_tile_is_failed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            fixture = self._make_wave2_fixture(root)
            (fixture[0] / "sector_x0000_y0000.png").unlink()
            report = self._validate_wave2(root, fixture)
            self.assertEqual("FAIL", report["overall_status"])
            self.assertEqual("FAIL", report["ring"]["status"])
            self.assertEqual(15, report["ring"]["actual_ring_count"])

    def test_wave2_present_forbidden_content_is_failed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            fixture = list(self._make_wave2_fixture(root))
            review = json.loads(fixture[3].read_text(encoding="utf-8"))
            review["categories"]["ground_routes"] = {
                "status": "PRESENT",
                "affected_tiles": ["sector_x0000_y0000"],
            }
            fixture[3].write_text(json.dumps(review), encoding="utf-8")
            report = self._validate_wave2(root, tuple(fixture))
            self.assertEqual("FAIL", report["overall_status"])
            self.assertEqual("FAIL", report["forbidden_content_review"]["status"])

    def test_wave2_visible_grid_in_perceptual_review_is_failed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            fixture = self._make_wave2_fixture(root)
            report = self._validate_wave2(root, fixture, {"grid_lines_visible": "YES"})
            self.assertEqual("FAIL", report["overall_status"])
            self.assertEqual("FAIL", report["perceptual_continuity_review"]["status"])
            self.assertEqual("YES", report["continuity_gates"]["grid_pattern_visible"])

    def test_wave2_dark_outer_ring_is_detected_by_center_ring_salience(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            fixture = self._make_wave2_fixture(root)
            input_dir, _, reference, _, tile_size = fixture
            with Image.open(reference) as source:
                changed = np.asarray(source.convert("RGB"), dtype=np.int16).copy()
            ring_mask = np.zeros(changed.shape[:2], dtype=bool)
            ring_mask[:tile_size, :] = True
            ring_mask[-tile_size:, :] = True
            ring_mask[:, :tile_size] = True
            ring_mask[:, -tile_size:] = True
            changed[ring_mask] = np.clip(changed[ring_mask] - 90, 0, 255)
            changed_u8 = changed.astype(np.uint8)
            Image.fromarray(changed_u8).save(reference)
            for tile_y in range(5):
                for tile_x in range(5):
                    if 1 <= tile_x <= 3 and 1 <= tile_y <= 3:
                        continue
                    tile = changed_u8[
                        tile_y * tile_size:(tile_y + 1) * tile_size,
                        tile_x * tile_size:(tile_x + 1) * tile_size,
                    ]
                    Image.fromarray(tile).save(input_dir / f"sector_x{tile_x:04d}_y{tile_y:04d}.png")

            report = self._validate_wave2(root, fixture)
            self.assertEqual("FAIL", report["overall_status"])
            self.assertEqual("WARN", report["low_frequency_grid"]["center_ring_salience"]["status"])
            self.assertGreaterEqual(report["low_frequency_grid"]["center_ring_salience"]["ratio"], 1.6)

    def test_wave3_continuous_master_passes_all_technical_gates(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            fixture = self._write_wave3_fixture(root, self._wave3_base_pixels())
            report = self._validate_wave3(root, fixture)
            self.assertEqual("PASS", report["overall_status"])
            self.assertEqual("PASS", report["manifest_contract"]["status"])
            self.assertEqual("PASS", report["master_contract"]["status"])
            self.assertIsNone(report["center_lock"])
            self.assertIsNone(report["ring"])
            self.assertTrue(report["reconstruction"]["source_comparison"]["pixel_identical"])
            self.assertEqual(25, report["runtime_gutters"]["validated_count"])
            self.assertEqual(40, report["runtime_gutters"]["boundary_pass_count"])
            self.assertEqual("PASS", report["continuity_gates"]["technical_continuity"]["status"])
            self.assertEqual(
                "PASS",
                report["continuity_gates"]["technical_continuity"]["components"]["wave2_center_hash_lock_removed"],
            )
            for filename in (
                "perceptual_mosaic_100.png",
                "perceptual_mosaic_73.png",
                "perceptual_mosaic_50.png",
                "perceptual_mosaic_25.png",
                "perceptual_pan_horizontal.png",
                "perceptual_pan_vertical.png",
                "runtime_gutters_contact_sheet.png",
                "top_risks.png",
            ):
                self.assertTrue((root / "wave3_output" / filename).is_file(), filename)

    def test_wave3_artificial_checkerboard_fails_despite_good_canonical_edges(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            tile_size = 64
            pixels = self._wave3_base_pixels(tile_size).astype(np.int16)
            margin = 10
            for tile_y in range(5):
                for tile_x in range(5):
                    delta = 42 if (tile_x + tile_y) % 2 == 0 else -42
                    y0 = tile_y * tile_size + margin
                    y1 = (tile_y + 1) * tile_size - margin
                    x0 = tile_x * tile_size + margin
                    x1 = (tile_x + 1) * tile_size - margin
                    pixels[y0:y1, x0:x1] = np.clip(pixels[y0:y1, x0:x1] + delta, 0, 255)
            fixture = self._write_wave3_fixture(root, pixels.astype(np.uint8))
            report = self._validate_wave3(root, fixture)
            self.assertTrue(all(seam["status"] == "PASS" for seam in report["seams"]))
            self.assertEqual("FAIL", report["macro_patterns"]["checkerboard"]["status"])
            self.assertEqual("FAIL", report["overall_status"])

    def test_wave3_mirrored_tile_is_failed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            tile_size = 64
            pixels = self._wave3_base_pixels(tile_size)
            source = pixels[0:tile_size, 0:tile_size].copy()
            pixels[3 * tile_size:4 * tile_size, 4 * tile_size:5 * tile_size] = np.fliplr(source)
            fixture = self._write_wave3_fixture(root, pixels)
            report = self._validate_wave3(root, fixture)
            self.assertEqual("FAIL", report["motif_repetition"]["status"])
            self.assertGreater(report["motif_repetition"]["exact_mirror_count"], 0)
            self.assertEqual("FAIL", report["overall_status"])

    def test_wave3_blurred_boundary_band_is_not_accepted(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            tile_size = 64
            textured = self._wave3_base_pixels(tile_size).astype(np.int16)
            y, x = np.mgrid[0:textured.shape[0], 0:textured.shape[1]]
            texture = 18 * np.sin(x * 1.31) + 15 * np.cos(y * 1.17) + 9 * np.sin((x + y) * 0.83)
            textured = np.clip(textured + texture[:, :, None], 0, 255).astype(np.uint8)
            base = Image.fromarray(textured)
            blurred = base.filter(ImageFilter.GaussianBlur(radius=5.0))
            mask = Image.new("L", base.size, 0)
            draw = ImageDraw.Draw(mask)
            band = 8
            for index in range(1, 5):
                x = index * tile_size
                y = index * tile_size
                draw.rectangle((x - band, 0, x + band, base.height), fill=255)
                draw.rectangle((0, y - band, base.width, y + band), fill=255)
            pixels = np.asarray(Image.composite(blurred, base, mask), dtype=np.uint8)
            fixture = self._write_wave3_fixture(root, pixels)
            report = self._validate_wave3(root, fixture)
            self.assertIn(report["macro_patterns"]["blurred_boundary_bands"]["status"], {"WARN", "FAIL"})
            self.assertNotEqual("PASS", report["overall_status"])

    def test_wave3_repeated_tile_is_failed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            tile_size = 64
            pixels = self._wave3_base_pixels(tile_size)
            pixels[4 * tile_size:5 * tile_size, 4 * tile_size:5 * tile_size] = pixels[0:tile_size, 0:tile_size]
            fixture = self._write_wave3_fixture(root, pixels)
            report = self._validate_wave3(root, fixture)
            self.assertGreater(len(report["duplicates"]["exact"]), 0)
            self.assertEqual("FAIL", report["overall_status"])

    def test_wave3_dominant_route_is_failed_by_human_content_gate(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            image = Image.fromarray(self._wave3_base_pixels())
            draw = ImageDraw.Draw(image)
            draw.line((4, 280, 316, 38), fill=(224, 193, 128), width=12)
            fixture = self._write_wave3_fixture(root, np.asarray(image, dtype=np.uint8), route_present=True)
            report = self._validate_wave3(root, fixture)
            self.assertEqual("FAIL", report["forbidden_content_review"]["status"])
            self.assertIn("ground_routes", report["forbidden_content_review"]["present_categories"])
            self.assertFalse(report["forbidden_content_review"]["semantic_detection_automated"])
            self.assertEqual("FAIL", report["overall_status"])

    def test_wave3_altered_reconstruction_is_failed_against_master(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            fixture = self._write_wave3_fixture(root, self._wave3_base_pixels())
            input_dir = Path(fixture["input_dir"])
            target = input_dir / "wave3_x0002_y0002.png"
            with Image.open(target) as source:
                changed = np.asarray(source.convert("RGB"), dtype=np.uint8).copy()
            changed[20:30, 20:30, 0] ^= 0x7F
            Image.fromarray(changed).save(target)
            manifest = json.loads(Path(fixture["manifest"]).read_text(encoding="utf-8"))
            entry = next(item for item in manifest["tiles"] if item["id"] == "wave3_x0002_y0002")
            entry["sha256"] = hashlib.sha256(target.read_bytes()).hexdigest()
            Path(fixture["manifest"]).write_text(json.dumps(manifest, indent=2), encoding="utf-8")
            report = self._validate_wave3(root, fixture)
            self.assertFalse(report["reconstruction"]["source_comparison"]["pixel_identical"])
            self.assertEqual("FAIL", report["continuity_gates"]["technical_continuity"]["status"])
            self.assertEqual("FAIL", report["overall_status"])


if __name__ == "__main__":
    unittest.main()
