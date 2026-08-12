#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any

import numpy as np
from PIL import Image


MASTER_SIZE = 2560
TILE_SIZE = 512
GRID_SIZE = 5
GUTTER = 2
RUNTIME_SIZE = TILE_SIZE + 2 * GUTTER
EXPECTED_MASTER_SHA256 = "d3cdc2dde9d56cac58be6833790b6fd8fc38ac157f72a01dcebd8117583a95b4"
EXPECTED_TILE_IDS = [f"R{row}C{column}" for row in range(GRID_SIZE) for column in range(GRID_SIZE)]
UV_MIN = GUTTER / RUNTIME_SIZE
UV_MAX = (GUTTER + TILE_SIZE) / RUNTIME_SIZE


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def sha256_pixels(pixels: np.ndarray) -> str:
    return hashlib.sha256(np.ascontiguousarray(pixels).tobytes()).hexdigest()


def load_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(value, dict):
        raise ValueError(f"JSON root must be an object: {path}")
    return value


def load_rgb(path: Path) -> tuple[np.ndarray, tuple[int, int], str]:
    with Image.open(path) as image:
        size = image.size
        mode = image.mode
        image.verify()
    with Image.open(path) as image:
        pixels = np.asarray(image.convert("RGB"), dtype=np.uint8).copy()
    return pixels, size, mode


def add_issue(issues: list[dict[str, Any]], code: str, **details: Any) -> None:
    issues.append({"code": code, **details})


def validate_claims(document: dict[str, Any], issues: list[dict[str, Any]], file: str) -> None:
    claims = document.get("claims")
    if not isinstance(claims, dict):
        add_issue(issues, "MISSING_NON_LIVE_CLAIMS", file=file)
        return
    for key in ("live_server", "official_world_map", "runtime_integration", "unity_dependency"):
        if claims.get(key) is not False:
            add_issue(issues, "INVALID_LIVE_OR_INTEGRATION_CLAIM", file=file, claim=key, value=claims.get(key))


def expected_runtime(master: np.ndarray, row: int, column: int) -> np.ndarray:
    padded = np.pad(master, ((GUTTER, GUTTER), (GUTTER, GUTTER), (0, 0)), mode="edge")
    y = row * TILE_SIZE
    x = column * TILE_SIZE
    return padded[y:y + RUNTIME_SIZE, x:x + RUNTIME_SIZE]


def audit_run(run_dir: Path, master: np.ndarray, master_sha256: str, ui_tiles_dir: Path) -> dict[str, Any]:
    run_dir = run_dir.resolve()
    issues: list[dict[str, Any]] = []
    canonical_manifest_path = run_dir / "canonical" / "manifest.canonical.json"
    runtime_manifest_path = run_dir / "runtime" / "manifest.runtime.json"
    validation_path = run_dir / "validation.json"
    for path, code in (
        (canonical_manifest_path, "MISSING_CANONICAL_MANIFEST"),
        (runtime_manifest_path, "MISSING_RUNTIME_MANIFEST"),
        (validation_path, "MISSING_PRODUCER_VALIDATION"),
    ):
        if not path.is_file():
            add_issue(issues, code, path=str(path))
    if issues:
        return {"status": "FAIL", "run_dir": str(run_dir), "issues": issues}

    canonical_manifest = load_json(canonical_manifest_path)
    runtime_manifest = load_json(runtime_manifest_path)
    producer_validation = load_json(validation_path)
    validate_claims(canonical_manifest, issues, "canonical/manifest.canonical.json")
    validate_claims(runtime_manifest, issues, "runtime/manifest.runtime.json")
    validate_claims(producer_validation, issues, "validation.json")

    all_files = sorted(path for path in run_dir.rglob("*") if path.is_file())
    relative_files = [path.relative_to(run_dir).as_posix() for path in all_files]
    forbidden_extensions = [
        relative for relative in relative_files if Path(relative).suffix.lower() not in {".png", ".json"}
    ]
    if forbidden_extensions:
        add_issue(issues, "FORBIDDEN_BUNDLE_FILE_TYPE", files=forbidden_extensions)

    canonical_entries = canonical_manifest.get("tiles")
    runtime_entries = runtime_manifest.get("tiles")
    canonical_entries = canonical_entries if isinstance(canonical_entries, list) else []
    runtime_entries = runtime_entries if isinstance(runtime_entries, list) else []
    expected_files = {
        "canonical/manifest.canonical.json",
        "canonical/reconstruction.png",
        "runtime/manifest.runtime.json",
        "validation.json",
    }
    expected_files.update(f"canonical/tiles/{tile_id}.png" for tile_id in EXPECTED_TILE_IDS)
    expected_files.update(f"runtime/tiles/{tile_id}_g2.png" for tile_id in EXPECTED_TILE_IDS)
    missing_files = sorted(expected_files - set(relative_files))
    extra_files = sorted(set(relative_files) - expected_files)
    if missing_files:
        add_issue(issues, "MISSING_BUNDLE_FILES", files=missing_files)
    if extra_files:
        add_issue(issues, "EXTRA_BUNDLE_FILES", files=extra_files)
    if len(relative_files) != 54:
        add_issue(issues, "BUNDLE_FILE_COUNT_MISMATCH", actual=len(relative_files), expected=54)

    canonical_order = [entry.get("id") for entry in canonical_entries if isinstance(entry, dict)]
    runtime_order = [entry.get("id") for entry in runtime_entries if isinstance(entry, dict)]
    if canonical_order != EXPECTED_TILE_IDS:
        add_issue(issues, "CANONICAL_ORDER_MISMATCH", actual=canonical_order)
    if runtime_order != EXPECTED_TILE_IDS:
        add_issue(issues, "RUNTIME_ORDER_MISMATCH", actual=runtime_order)

    canonical_grid = canonical_manifest.get("grid", {})
    if (
        canonical_grid.get("rows") != GRID_SIZE
        or canonical_grid.get("columns") != GRID_SIZE
        or canonical_grid.get("tile_width") != TILE_SIZE
        or canonical_grid.get("tile_height") != TILE_SIZE
    ):
        add_issue(issues, "CANONICAL_GRID_CONTRACT_MISMATCH", grid=canonical_grid)
    source = canonical_manifest.get("source", {})
    if source.get("sha256", "").lower() != master_sha256.lower():
        add_issue(issues, "CANONICAL_SOURCE_HASH_MISMATCH", actual=source.get("sha256"), expected=master_sha256)

    canonical_arrays: dict[str, np.ndarray] = {}
    canonical_pixel_difference = 0
    ui_pixel_difference = 0
    canonical_hash_matches = 0
    canonical_pixel_hash_matches = 0
    for index, tile_id in enumerate(EXPECTED_TILE_IDS):
        row, column = divmod(index, GRID_SIZE)
        entry = canonical_entries[index] if index < len(canonical_entries) and isinstance(canonical_entries[index], dict) else {}
        relative = Path(str(entry.get("file", f"tiles/{tile_id}.png")))
        path = run_dir / "canonical" / relative
        if not path.is_file():
            continue
        pixels, size, mode = load_rgb(path)
        canonical_arrays[tile_id] = pixels
        expected = master[
            row * TILE_SIZE:(row + 1) * TILE_SIZE,
            column * TILE_SIZE:(column + 1) * TILE_SIZE,
        ]
        difference = int(np.count_nonzero(pixels != expected))
        canonical_pixel_difference += difference
        if difference:
            add_issue(issues, "CANONICAL_PIXEL_ALTERATION", tile=tile_id, differing_channels=difference)
        if size != (TILE_SIZE, TILE_SIZE):
            add_issue(issues, "CANONICAL_DIMENSION_MISMATCH", tile=tile_id, actual=list(size))
        if mode not in {"RGB", "RGBA"}:
            add_issue(issues, "CANONICAL_MODE_MISMATCH", tile=tile_id, actual=mode)
        expected_crop = {"x": column * TILE_SIZE, "y": row * TILE_SIZE, "width": TILE_SIZE, "height": TILE_SIZE}
        if entry.get("crop") != expected_crop or entry.get("row") != row or entry.get("column") != column:
            add_issue(issues, "CANONICAL_POSITION_MISMATCH", tile=tile_id, entry=entry)
        file_hash = sha256_file(path)
        pixel_hash = sha256_pixels(pixels)
        if entry.get("png_sha256", "").lower() == file_hash:
            canonical_hash_matches += 1
        else:
            add_issue(issues, "CANONICAL_HASH_MISMATCH", tile=tile_id)
        if entry.get("pixel_sha256", "").lower() == pixel_hash:
            canonical_pixel_hash_matches += 1
        else:
            add_issue(issues, "CANONICAL_PIXEL_HASH_MISMATCH", tile=tile_id)
        ui_path = ui_tiles_dir / f"{tile_id}.png"
        if not ui_path.is_file():
            add_issue(issues, "MISSING_UIB_CANONICAL_TILE", tile=tile_id, path=str(ui_path))
        else:
            ui_pixels, ui_size, _ = load_rgb(ui_path)
            ui_difference = int(np.count_nonzero(pixels != ui_pixels)) if ui_size == size else pixels.size
            ui_pixel_difference += ui_difference
            if ui_difference:
                add_issue(issues, "CANONICAL_DIFFERS_FROM_UIB_TILE", tile=tile_id, differing_channels=ui_difference)

    reconstruction = np.zeros_like(master)
    for index, tile_id in enumerate(EXPECTED_TILE_IDS):
        if tile_id not in canonical_arrays:
            continue
        row, column = divmod(index, GRID_SIZE)
        reconstruction[
            row * TILE_SIZE:(row + 1) * TILE_SIZE,
            column * TILE_SIZE:(column + 1) * TILE_SIZE,
        ] = canonical_arrays[tile_id]
    reconstruction_difference = int(np.count_nonzero(reconstruction != master))
    if reconstruction_difference:
        add_issue(issues, "CANONICAL_RECONSTRUCTION_MISMATCH", differing_channels=reconstruction_difference)
    reconstruction_path = run_dir / "canonical" / "reconstruction.png"
    if reconstruction_path.is_file():
        file_pixels, size, _ = load_rgb(reconstruction_path)
        file_difference = int(np.count_nonzero(file_pixels != master)) if size == (MASTER_SIZE, MASTER_SIZE) else master.size
    else:
        file_difference = master.size
    if file_difference:
        add_issue(issues, "SAVED_RECONSTRUCTION_MISMATCH", differing_channels=file_difference)

    gutter_contract = runtime_manifest.get("gutter", {})
    expected_contract = {
        "pixels_each_side": GUTTER,
        "runtime_width": RUNTIME_SIZE,
        "runtime_height": RUNTIME_SIZE,
        "source_for_internal_sides": "true_adjacent_master_pixels",
        "outer_edge_policy": "clamp_master_edge_only",
        "stretching": False,
    }
    for key, expected_value in expected_contract.items():
        if gutter_contract.get(key) != expected_value:
            add_issue(
                issues,
                "RUNTIME_GUTTER_CONTRACT_MISMATCH",
                field=key,
                actual=gutter_contract.get(key),
                expected=expected_value,
            )

    runtime_arrays: dict[str, np.ndarray] = {}
    runtime_full_difference = 0
    runtime_interior_difference = 0
    runtime_hash_matches = 0
    runtime_pixel_hash_matches = 0
    uv_exact_count = 0
    internal_side_count = 0
    internal_side_pass = 0
    outer_clamp_count = 0
    outer_clamp_pass = 0
    for index, tile_id in enumerate(EXPECTED_TILE_IDS):
        row, column = divmod(index, GRID_SIZE)
        entry = runtime_entries[index] if index < len(runtime_entries) and isinstance(runtime_entries[index], dict) else {}
        relative = Path(str(entry.get("file", f"tiles/{tile_id}_g2.png")))
        path = run_dir / "runtime" / relative
        if not path.is_file():
            continue
        pixels, size, mode = load_rgb(path)
        runtime_arrays[tile_id] = pixels
        expected = expected_runtime(master, row, column)
        full_difference = int(np.count_nonzero(pixels != expected)) if size == (RUNTIME_SIZE, RUNTIME_SIZE) else expected.size
        runtime_full_difference += full_difference
        if full_difference:
            add_issue(issues, "RUNTIME_PIXEL_ALTERATION", tile=tile_id, differing_channels=full_difference)
        if size != (RUNTIME_SIZE, RUNTIME_SIZE):
            add_issue(issues, "RUNTIME_DIMENSION_MISMATCH", tile=tile_id, actual=list(size))
            continue
        if mode not in {"RGB", "RGBA"}:
            add_issue(issues, "RUNTIME_MODE_MISMATCH", tile=tile_id, actual=mode)
        interior = pixels[GUTTER:GUTTER + TILE_SIZE, GUTTER:GUTTER + TILE_SIZE]
        canonical = master[
            row * TILE_SIZE:(row + 1) * TILE_SIZE,
            column * TILE_SIZE:(column + 1) * TILE_SIZE,
        ]
        interior_difference = int(np.count_nonzero(interior != canonical))
        runtime_interior_difference += interior_difference
        if interior_difference:
            add_issue(issues, "RUNTIME_INTERIOR_MISMATCH", tile=tile_id, differing_channels=interior_difference)

        expected_crop = {"x": column * TILE_SIZE, "y": row * TILE_SIZE, "width": TILE_SIZE, "height": TILE_SIZE}
        expected_window = {
            "x": column * TILE_SIZE - GUTTER,
            "y": row * TILE_SIZE - GUTTER,
            "width": RUNTIME_SIZE,
            "height": RUNTIME_SIZE,
        }
        if entry.get("canonical_crop") != expected_crop or entry.get("source_window_unclamped") != expected_window:
            add_issue(issues, "RUNTIME_SOURCE_WINDOW_MISMATCH", tile=tile_id)
        if entry.get("source_master_sha256", "").lower() != master_sha256.lower():
            add_issue(issues, "RUNTIME_SOURCE_HASH_MISMATCH", tile=tile_id)
        inner_rect = entry.get("inner_rect", {})
        if inner_rect != {"x": 2, "y": 2, "width": 512, "height": 512}:
            add_issue(issues, "RUNTIME_INNER_RECT_MISMATCH", tile=tile_id, actual=inner_rect)
        uv = entry.get("uv_inner_normalized", {})
        if all(
            abs(float(uv.get(key, -1.0)) - expected_value) <= 1.0e-15
            for key, expected_value in (("u_min", UV_MIN), ("v_min", UV_MIN), ("u_max", UV_MAX), ("v_max", UV_MAX))
        ):
            uv_exact_count += 1
        else:
            add_issue(issues, "RUNTIME_UV_MISMATCH", tile=tile_id, actual=uv)
        file_hash = sha256_file(path)
        pixel_hash = sha256_pixels(pixels)
        if entry.get("png_sha256", "").lower() == file_hash:
            runtime_hash_matches += 1
        else:
            add_issue(issues, "RUNTIME_HASH_MISMATCH", tile=tile_id)
        if entry.get("pixel_sha256", "").lower() == pixel_hash:
            runtime_pixel_hash_matches += 1
        else:
            add_issue(issues, "RUNTIME_PIXEL_HASH_MISMATCH", tile=tile_id)

        sides = {
            "top": pixels[:GUTTER, GUTTER:-GUTTER],
            "bottom": pixels[-GUTTER:, GUTTER:-GUTTER],
            "left": pixels[GUTTER:-GUTTER, :GUTTER],
            "right": pixels[GUTTER:-GUTTER, -GUTTER:],
        }
        expected_sides = {
            "top": expected[:GUTTER, GUTTER:-GUTTER],
            "bottom": expected[-GUTTER:, GUTTER:-GUTTER],
            "left": expected[GUTTER:-GUTTER, :GUTTER],
            "right": expected[GUTTER:-GUTTER, -GUTTER:],
        }
        is_outer = {"top": row == 0, "bottom": row == 4, "left": column == 0, "right": column == 4}
        provenance = entry.get("gutter_provenance", {})
        for side in ("top", "right", "bottom", "left"):
            side_ok = bool(np.array_equal(sides[side], expected_sides[side]))
            expected_provenance = "outer_edge_clamp" if is_outer[side] else "true_master_neighbor_pixels"
            side_ok = side_ok and provenance.get(side) == expected_provenance
            if is_outer[side]:
                outer_clamp_count += 1
                outer_clamp_pass += int(side_ok)
            else:
                internal_side_count += 1
                internal_side_pass += int(side_ok)
            if not side_ok:
                add_issue(issues, "RUNTIME_SIDE_PROVENANCE_OR_PIXEL_MISMATCH", tile=tile_id, side=side)

    boundary_count = 0
    boundary_pass = 0
    boundary_rows: list[dict[str, Any]] = []
    for row in range(GRID_SIZE):
        for column in range(GRID_SIZE):
            tile_id = f"R{row}C{column}"
            current = runtime_arrays.get(tile_id)
            for direction, next_row, next_column in (("E", row, column + 1), ("S", row + 1, column)):
                if next_row >= GRID_SIZE or next_column >= GRID_SIZE:
                    continue
                neighbor_id = f"R{next_row}C{next_column}"
                neighbor = runtime_arrays.get(neighbor_id)
                boundary_count += 1
                ok = False
                if current is not None and neighbor is not None:
                    if direction == "E":
                        ok = bool(
                            np.array_equal(current[GUTTER:-GUTTER, -GUTTER:], neighbor[GUTTER:-GUTTER, GUTTER:2 * GUTTER])
                            and np.array_equal(neighbor[GUTTER:-GUTTER, :GUTTER], current[GUTTER:-GUTTER, -2 * GUTTER:-GUTTER])
                        )
                    else:
                        ok = bool(
                            np.array_equal(current[-GUTTER:, GUTTER:-GUTTER], neighbor[GUTTER:2 * GUTTER, GUTTER:-GUTTER])
                            and np.array_equal(neighbor[:GUTTER, GUTTER:-GUTTER], current[-2 * GUTTER:-GUTTER, GUTTER:-GUTTER])
                        )
                boundary_pass += int(ok)
                boundary_rows.append({"tile_a": tile_id, "tile_b": neighbor_id, "direction": direction, "status": "PASS" if ok else "FAIL"})
                if not ok:
                    add_issue(issues, "INTERNAL_GUTTER_BOUNDARY_FAILURE", tile_a=tile_id, tile_b=neighbor_id, direction=direction)

    producer_status = producer_validation.get("status")
    if producer_status != "PASS":
        add_issue(issues, "PRODUCER_VALIDATION_NOT_PASS", actual=producer_status)

    return {
        "status": "PASS" if not issues else "FAIL",
        "run_dir": str(run_dir),
        "inventory": {
            "file_count": len(relative_files),
            "missing": missing_files,
            "extra": extra_files,
            "forbidden_extensions": forbidden_extensions,
            "files": relative_files,
        },
        "canonical": {
            "tile_count": len(canonical_arrays),
            "dimensions_expected": [512, 512],
            "png_hash_matches": canonical_hash_matches,
            "pixel_hash_matches": canonical_pixel_hash_matches,
            "pixel_difference_from_master": canonical_pixel_difference,
            "pixel_difference_from_uib_tiles": ui_pixel_difference,
            "reconstruction_pixel_difference_from_master": reconstruction_difference,
            "saved_reconstruction_pixel_difference_from_master": file_difference,
        },
        "runtime": {
            "tile_count": len(runtime_arrays),
            "dimensions_expected": [516, 516],
            "png_hash_matches": runtime_hash_matches,
            "pixel_hash_matches": runtime_pixel_hash_matches,
            "full_tile_difference_from_expected": runtime_full_difference,
            "interior_difference_from_master": runtime_interior_difference,
            "uv_exact_count": uv_exact_count,
            "uv_expected": {"min": UV_MIN, "max": UV_MAX},
            "internal_true_neighbor_sides": {"checked": internal_side_count, "passed": internal_side_pass},
            "outer_clamp_sides": {"checked": outer_clamp_count, "passed": outer_clamp_pass},
            "internal_boundaries": {"checked": boundary_count, "passed": boundary_pass, "rows": boundary_rows},
            "stretching": gutter_contract.get("stretching"),
        },
        "producer_validation_status": producer_status,
        "issues": issues,
    }


def compare_runs(run1: Path, run2: Path) -> dict[str, Any]:
    files1 = {path.relative_to(run1).as_posix(): path for path in run1.rglob("*") if path.is_file()}
    files2 = {path.relative_to(run2).as_posix(): path for path in run2.rglob("*") if path.is_file()}
    missing = sorted(set(files1) - set(files2))
    extra = sorted(set(files2) - set(files1))
    different: list[dict[str, str]] = []
    rows: list[str] = []
    for relative in sorted(set(files1) & set(files2)):
        hash1 = sha256_file(files1[relative])
        hash2 = sha256_file(files2[relative])
        rows.append(f"{hash1}  {relative}\n")
        if hash1 != hash2:
            different.append({"file": relative, "run1_sha256": hash1, "run2_sha256": hash2})
    tree_digest1 = hashlib.sha256("".join(rows).encode("utf-8")).hexdigest()
    rows2 = [f"{sha256_file(files2[relative])}  {relative}\n" for relative in sorted(files2)]
    tree_digest2 = hashlib.sha256("".join(rows2).encode("utf-8")).hexdigest()
    return {
        "status": "PASS" if not missing and not extra and not different else "FAIL",
        "file_count_run1": len(files1),
        "file_count_run2": len(files2),
        "missing_in_run2": missing,
        "extra_in_run2": extra,
        "different_files": different,
        "tree_digest_run1": tree_digest1,
        "tree_digest_run2": tree_digest2,
        "byte_identical": not missing and not extra and not different,
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Independent Builder-C audit of Wave3 runtime gutters.")
    parser.add_argument("--master", required=True, type=Path)
    parser.add_argument("--run1", required=True, type=Path)
    parser.add_argument("--run2", type=Path)
    parser.add_argument("--ui-tiles", type=Path)
    parser.add_argument("--output", type=Path)
    return parser


def main() -> int:
    args = build_parser().parse_args()
    master_path = args.master.resolve()
    master, size, mode = load_rgb(master_path)
    master_hash = sha256_file(master_path)
    master_issues: list[dict[str, Any]] = []
    if size != (MASTER_SIZE, MASTER_SIZE):
        add_issue(master_issues, "MASTER_DIMENSION_MISMATCH", actual=list(size))
    if mode != "RGB":
        add_issue(master_issues, "MASTER_MODE_MISMATCH", actual=mode)
    if master_hash.lower() != EXPECTED_MASTER_SHA256:
        add_issue(master_issues, "MASTER_HASH_MISMATCH", actual=master_hash, expected=EXPECTED_MASTER_SHA256)
    ui_tiles_dir = (args.ui_tiles or (master_path.parent / "tiles")).resolve()
    run1 = audit_run(args.run1.resolve(), master, master_hash, ui_tiles_dir)
    run2 = audit_run(args.run2.resolve(), master, master_hash, ui_tiles_dir) if args.run2 else None
    comparison = compare_runs(args.run1.resolve(), args.run2.resolve()) if args.run2 else None
    statuses = ["PASS" if not master_issues else "FAIL", run1["status"]]
    if run2:
        statuses.append(run2["status"])
    if comparison:
        statuses.append(comparison["status"])
    status = "PASS" if all(value == "PASS" for value in statuses) else "FAIL"
    report = {
        "schema": "bee-kingdom.builder-c-wave3-runtime-gutter-audit.v1",
        "status": status,
        "master": {
            "path": str(master_path),
            "size": list(size),
            "mode": mode,
            "sha256": master_hash,
            "pixel_sha256": sha256_pixels(master),
            "issues": master_issues,
        },
        "ui_tiles_dir": str(ui_tiles_dir),
        "run1": run1,
        "run2": run2,
        "comparison": comparison,
        "claims": {
            "independent_recalculation": True,
            "producer_validation_trusted_without_recalculation": False,
            "unity_integration_done": False,
            "live_server_or_world_claim": False,
        },
        "verdicts": {
            "WORLD_MAP_WAVE3_RUNTIME_BUNDLE_INTEGRITY": status,
            "REAL_MASTER_CANONICAL_PIXEL_IDENTITY": "PASS"
            if run1["canonical"].get("pixel_difference_from_master") == 0
            and (run2 is None or run2["canonical"].get("pixel_difference_from_master") == 0)
            else "FAIL",
            "RUNTIME_TRUE_NEIGHBOR_GUTTERS_40_OF_40": "PASS"
            if run1["runtime"].get("internal_boundaries", {}).get("passed") == 40
            and (run2 is None or run2["runtime"].get("internal_boundaries", {}).get("passed") == 40)
            else "FAIL",
            "RUN1_RUN2_BYTE_IDENTITY": "PASS" if comparison and comparison["byte_identical"] else "FAIL",
        },
    }
    payload = json.dumps(report, indent=2, ensure_ascii=False) + "\n"
    if args.output:
        output = args.output.resolve()
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(payload, encoding="utf-8")
    print(payload, end="")
    return 0 if status == "PASS" else 2


if __name__ == "__main__":
    raise SystemExit(main())
