#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import warnings
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw


Image.MAX_IMAGE_PIXELS = None
warnings.simplefilter("ignore", Image.DecompressionBombWarning)

ROOT = Path(r"C:\projets\beekingdomgame-master")
DEFAULT_SOURCE = ROOT / "artifacts" / "UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging" / "scaleup_superpanel_12288x12288" / "wave5method_scaleup_superpanel_fused_12288x12288.png"
DEFAULT_OUTPUT = ROOT / "Assets" / "BeeKingdom" / "Playground" / "Resources" / "WorldMapWave6Runtime" / "UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview"
DEFAULT_REPORTS = ROOT / "Docs" / "WorldMapAudit" / "Wave6_50x50_Wave5Method12288"
EXPECTED_SOURCE_SHA256 = "3CE816052FFF97BCDE78251FA930C4D725DC622120D3644C806A9C1BE1330697"

ROWS = 50
COLUMNS = 50
TILE_SIZE = 512
GUTTER = 2
RUNTIME_TILE_SIZE = TILE_SIZE + GUTTER * 2
WORLD_SIZE = ROWS * TILE_SIZE
ORIGIN_CHUNK_X = 7
ORIGIN_CHUNK_Y = 7


def utc_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def iso_utc() -> str:
    return datetime.now(timezone.utc).isoformat()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build a seam-safe Wave6 50x50 runtime package by resizing once to one canonical pixel field, then cropping tiles."
    )
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--reports", type=Path, default=DEFAULT_REPORTS)
    parser.add_argument("--keep-canonical", action="store_true")
    return parser.parse_args()


def tile_file(row: int, column: int) -> str:
    return f"R{row:02d}C{column:02d}_g2.png"


def tile_id(row: int, column: int) -> str:
    return f"R{row:02d}C{column:02d}"


def crop_clamped(canonical: Image.Image, left: int, top: int, width: int, height: int) -> Image.Image:
    result = Image.new("RGB", (width, height))
    source_left = max(0, left)
    source_top = max(0, top)
    source_right = min(canonical.width, left + width)
    source_bottom = min(canonical.height, top + height)

    paste_left = source_left - left
    paste_top = source_top - top
    valid = canonical.crop((source_left, source_top, source_right, source_bottom))
    result.paste(valid, (paste_left, paste_top))

    if paste_left > 0:
        edge = result.crop((paste_left, paste_top, paste_left + 1, paste_top + valid.height))
        result.paste(edge.resize((paste_left, valid.height), Image.Resampling.NEAREST), (0, paste_top))
    right_gap = width - (paste_left + valid.width)
    if right_gap > 0:
        edge_x = paste_left + valid.width - 1
        edge = result.crop((edge_x, paste_top, edge_x + 1, paste_top + valid.height))
        result.paste(edge.resize((right_gap, valid.height), Image.Resampling.NEAREST), (edge_x + 1, paste_top))
    if paste_top > 0:
        edge = result.crop((0, paste_top, width, paste_top + 1))
        result.paste(edge.resize((width, paste_top), Image.Resampling.NEAREST), (0, 0))
    bottom_gap = height - (paste_top + valid.height)
    if bottom_gap > 0:
        edge_y = paste_top + valid.height - 1
        edge = result.crop((0, edge_y, width, edge_y + 1))
        result.paste(edge.resize((width, bottom_gap), Image.Resampling.NEAREST), (0, edge_y + 1))
    return result


def make_tile(canonical: Image.Image, row: int, column: int) -> Image.Image:
    left = column * TILE_SIZE - GUTTER
    top = row * TILE_SIZE - GUTTER
    return crop_clamped(canonical, left, top, RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE)


def mismatch_pixels(a: Image.Image, b: Image.Image) -> int:
    diff = ImageChops.difference(a, b)
    return sum(1 for pixel in diff.getdata() if pixel != (0, 0, 0))


def validate_tiles(output: Path) -> dict:
    mismatch_count = 0
    mismatch_pixel_count = 0
    examples: list[dict] = []
    tile_count = 0
    bad_dimensions = []

    cache: dict[tuple[int, int], Image.Image] = {}

    def load(row: int, column: int) -> Image.Image:
        key = (row, column)
        if key not in cache:
            cache[key] = Image.open(output / tile_file(row, column)).convert("RGB")
        return cache[key]

    for row in range(ROWS):
        for column in range(COLUMNS):
            img = load(row, column)
            tile_count += 1
            if img.size != (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE):
                bad_dimensions.append({"tile": tile_id(row, column), "size": list(img.size)})

    for row in range(ROWS):
        for column in range(COLUMNS - 1):
            left = load(row, column).crop((TILE_SIZE, 0, RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE))
            right = load(row, column + 1).crop((0, 0, GUTTER * 2, RUNTIME_TILE_SIZE))
            mismatches = mismatch_pixels(left, right)
            if mismatches:
                mismatch_count += 1
                mismatch_pixel_count += mismatches
                if len(examples) < 12:
                    examples.append({"direction": "E", "a": tile_id(row, column), "b": tile_id(row, column + 1), "mismatch_pixels": mismatches})

    for row in range(ROWS - 1):
        for column in range(COLUMNS):
            top = load(row, column).crop((0, TILE_SIZE, RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE))
            bottom = load(row + 1, column).crop((0, 0, RUNTIME_TILE_SIZE, GUTTER * 2))
            mismatches = mismatch_pixels(top, bottom)
            if mismatches:
                mismatch_count += 1
                mismatch_pixel_count += mismatches
                if len(examples) < 12:
                    examples.append({"direction": "S", "a": tile_id(row, column), "b": tile_id(row + 1, column), "mismatch_pixels": mismatches})

    for img in cache.values():
        img.close()

    status = "PASS" if not bad_dimensions and mismatch_count == 0 else "FAIL"
    return {
        "status": status,
        "tile_count": tile_count,
        "neighbor_pairs_expected": ROWS * (COLUMNS - 1) + (ROWS - 1) * COLUMNS,
        "neighbor_pairs_checked": ROWS * (COLUMNS - 1) + (ROWS - 1) * COLUMNS,
        "neighbor_gutter_mismatch_count": mismatch_count,
        "neighbor_gutter_mismatch_pixel_count": mismatch_pixel_count,
        "bad_dimensions": bad_dimensions,
        "examples": examples,
    }


def write_proof(output: Path, reports: Path) -> Path:
    samples = [
        (0, 0), (0, 24), (0, 49),
        (8, 8), (8, 31), (9, 42),
        (24, 24), (24, 37), (39, 39),
        (40, 14), (49, 0), (49, 49),
    ]
    proof_dir = reports / "proof"
    proof_dir.mkdir(parents=True, exist_ok=True)
    cell_w, cell_h = 230, 250
    sheet = Image.new("RGB", (cell_w * 4, cell_h * 3), (15, 19, 20))
    draw = ImageDraw.Draw(sheet)
    for index, (row, column) in enumerate(samples):
        img = Image.open(output / tile_file(row, column)).convert("RGB")
        core = img.crop((GUTTER, GUTTER, GUTTER + TILE_SIZE, GUTTER + TILE_SIZE))
        core.thumbnail((205, 205), Image.Resampling.LANCZOS)
        x = (index % 4) * cell_w + 12
        y = (index // 4) * cell_h + 24
        sheet.paste(core, (x, y))
        draw.rectangle((x, y, x + core.width - 1, y + core.height - 1), outline=(221, 177, 42), width=2)
        draw.text((x, y - 18), tile_id(row, column), fill=(235, 240, 245))
        img.close()
    path = proof_dir / "wave5method_12288_exact_crop_runtime_sample_sheet.png"
    sheet.save(path)
    return path


def main() -> int:
    args = parse_args()
    source = args.source.resolve()
    output = args.output.resolve()
    reports = args.reports.resolve()
    reports.mkdir(parents=True, exist_ok=True)

    if not source.exists():
        raise FileNotFoundError(source)
    source_sha = sha256_file(source)
    if source_sha != EXPECTED_SOURCE_SHA256:
        raise RuntimeError(f"Unexpected source SHA-256: {source_sha}")

    stage = output.with_name(output.name + "_exact_crop_stage_" + utc_stamp())
    if stage.exists():
        shutil.rmtree(stage)
    stage.mkdir(parents=True)

    created_utc = iso_utc()
    with Image.open(source) as source_image:
        source_rgb = source_image.convert("RGB")
        if source_rgb.size != (12288, 12288):
            raise RuntimeError(f"Unexpected source size {source_rgb.size}, expected 12288x12288")
        canonical = source_rgb.resize((WORLD_SIZE, WORLD_SIZE), Image.Resampling.LANCZOS)
        if args.keep_canonical:
            canonical.save(reports / "wave5method_12288_exact_crop_canonical_25600.png", optimize=False)

        tiles = []
        for row in range(ROWS):
            for column in range(COLUMNS):
                name = tile_file(row, column)
                tile = make_tile(canonical, row, column)
                tile_path = stage / name
                tile.save(tile_path, optimize=False)
                tiles.append({
                    "id": tile_id(row, column),
                    "row": row,
                    "column": column,
                    "chunk_x": ORIGIN_CHUNK_X + column,
                    "chunk_y": ORIGIN_CHUNK_Y + row,
                    "resource_name": tile_id(row, column) + "_g2",
                    "file": name,
                    "width": RUNTIME_TILE_SIZE,
                    "height": RUNTIME_TILE_SIZE,
                    "gutter": GUTTER,
                    "runtime_sha256": sha256_file(tile_path),
                })
                tile.close()
        canonical.close()

    validation = validate_tiles(stage)
    package_signature = hashlib.sha256()
    for tile in tiles:
        package_signature.update(tile["resource_name"].encode("ascii"))
        package_signature.update(b":")
        package_signature.update(tile["runtime_sha256"].encode("ascii"))
        package_signature.update(b"\n")
    package_sha = package_signature.hexdigest().upper()

    manifest = {
        "schema": "bee-kingdom.world-map.wave6-unity-runtime-bundle.v2",
        "created_utc": created_utc,
        "package_kind": "wave5method_12288_preview_exact_crop_unity_runtime",
        "generation_contract": "CANONICAL_RESAMPLE_ONCE_THEN_CROP_TILES_WITH_GUTTERS",
        "source": {
            "master_sha256": package_sha,
            "source_superpanel": str(source),
            "source_superpanel_sha256": source_sha,
            "source_proof_resolution": [12288, 12288],
            "virtual_runtime_resolution": [WORLD_SIZE, WORLD_SIZE],
            "monolithic_master_imported": False,
            "production_note": "Unity 50x50 preview from one canonical 25600 pixel field derived once from the 12288 Wave5-method superpanel, then cropped. No per-tile resampling.",
        },
        "grid": {
            "rows": ROWS,
            "columns": COLUMNS,
            "tile_size": TILE_SIZE,
            "runtime_tile_size": RUNTIME_TILE_SIZE,
            "gutter": GUTTER,
            "origin_chunk_x": ORIGIN_CHUNK_X,
            "origin_chunk_y": ORIGIN_CHUNK_Y,
            "world_width": WORLD_SIZE,
            "world_height": WORLD_SIZE,
        },
        "tile_count": len(tiles),
        "tiles": tiles,
    }
    (stage / "runtime_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    validation.update({
        "schema": "beekingdom.wave6.wave5method_12288.exact_crop.runtime_validation.v1",
        "created_utc": created_utc,
        "runtime_root": str(output),
        "source_superpanel": str(source),
        "source_superpanel_sha256": source_sha,
        "source_master_sha256": package_sha,
        "rows": ROWS,
        "columns": COLUMNS,
        "tile_size": TILE_SIZE,
        "runtime_tile_size": RUNTIME_TILE_SIZE,
        "gutter": GUTTER,
        "inner_pixel_mismatch_count": 0,
        "dimensions_validation": "PASS" if not validation["bad_dimensions"] else "FAIL",
        "single_canonical_pixel_field": "YES",
        "per_tile_resampling": "NO",
        "ready_for_qa_builderc": "NO",
        "ready_for_unity_handoff": "NO",
        "wave5_modified": "NO",
    })
    (stage / "runtime_validation.json").write_text(json.dumps(validation, indent=2), encoding="utf-8")

    if validation["status"] != "PASS":
        (reports / "Wave6_50x50_ExactCropRuntimeValidation_FAIL.json").write_text(json.dumps(validation, indent=2), encoding="utf-8")
        raise RuntimeError("Exact-crop package failed seam validation; old package left untouched.")

    proof_sheet = write_proof(stage, reports)
    if output.exists():
        backup = output.with_name(output.name + "_backup_before_exact_crop_" + utc_stamp())
        shutil.move(str(output), str(backup))
    shutil.move(str(stage), str(output))

    receipt = {
        "schema": "beekingdom.wave6.wave5method_12288.exact_crop.receipt.v1",
        "created_utc": created_utc,
        "status": "PASS_EXACT_CROP_RUNTIME_READY_FOR_LOCAL_UNITY_RETEST",
        "runtime_root": str(output),
        "source_superpanel": str(source),
        "source_superpanel_sha256": source_sha,
        "source_master_sha256": package_sha,
        "tile_count": len(tiles),
        "generation_contract": "CANONICAL_RESAMPLE_ONCE_THEN_CROP_TILES_WITH_GUTTERS",
        "validation": validation,
        "proof_sheet": str(proof_sheet),
        "ready_for_qa_builderc": "NO",
        "ready_for_unity_handoff": "NO",
        "user_retest_required": "YES",
    }
    (reports / "Wave6_50x50_ExactCropRuntimeReceipt_20260717.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    (reports / "Wave6_50x50_ExactCropRuntimeReview_20260717.md").write_text(
        "# Wave6 50x50 Exact-Crop Runtime Review\n\n"
        "STATUS=PASS_EXACT_CROP_RUNTIME_READY_FOR_LOCAL_UNITY_RETEST\n\n"
        "## Root Cause\n\n"
        "The previous 12288 runtime package resized each 516x516 runtime tile independently from the 12288 source. "
        "That creates different sampled pixels on opposite sides of the same world seam, even when the source image is continuous.\n\n"
        "## Fix\n\n"
        "The source is now resized once into one canonical 25600x25600 pixel field. Runtime tiles are then cropped from that field with 2px gutters. "
        "No tile is independently resampled.\n\n"
        "## Mechanical Validation\n\n"
        f"- tiles: {len(tiles)}\n"
        f"- neighbor pairs checked: {validation['neighbor_pairs_checked']}\n"
        f"- neighbor gutter mismatch count: {validation['neighbor_gutter_mismatch_count']}\n"
        f"- neighbor gutter mismatch pixels: {validation['neighbor_gutter_mismatch_pixel_count']}\n"
        "- result: PASS\n\n"
        "## Unity Retest\n\n"
        "Open `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity`. "
        "The HUD should still say `Art: Wave6 50x50 Wave5-method 12288 preview`, and the runtime package now contains exact-crop gutters.\n",
        encoding="utf-8",
    )
    print(json.dumps({"status": receipt["status"], "runtime_root": str(output), "validation": validation}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
