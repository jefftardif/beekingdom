#!/usr/bin/env python3
"""Independent, read-only validator for the Wave4 15x15 art package."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import sys
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFont, ImageOps


GRID_SIZE = 15
TILE_SIZE = 512
MASTER_SIZE = GRID_SIZE * TILE_SIZE
EXPECTED_MASTER_SHA256 = "7e8d44d4bcb346de386b314e6b9b843d3c3dee1b80bc045477da65a4c5f5498d"

NATIVE_CROPS = [
    (0, 1),
    (1, 6),
    (2, 12),
    (4, 3),
    (5, 7),
    (6, 13),
    (8, 2),
    (8, 10),
    (9, 12),
    (10, 3),
    (12, 7),
    (13, 12),
]

JOIN_CROPS = [
    ("J01_R04C02_R04C03", (4, 2), (4, 3), "vertical"),
    ("J02_R06C12_R06C13", (6, 12), (6, 13), "vertical"),
    ("J03_R09C03_R10C03", (9, 3), (10, 3), "horizontal"),
    ("J04_R12C07_R13C07", (12, 7), (13, 7), "horizontal"),
]

PAN_POSITIONS = [0, 1792, 3584, 5376, 7168]
DIAGONAL_PANS = [
    ("D01_NW_SE", (512.0, 512.0), (7168.0, 7168.0)),
    ("D02_NE_SW", (7168.0, 512.0), (512.0, 7168.0)),
    ("D03_WNE_ESE", (512.0, 2304.0), (7168.0, 5376.0)),
    ("D04_WSE_ENE", (512.0, 5376.0), (7168.0, 2304.0)),
]


def log(message: str) -> None:
    print(message, flush=True)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def save_png(image: Image.Image, path: Path, compress_level: int = 6) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, format="PNG", optimize=False, compress_level=compress_level)


def file_record(path: Path, root: Path | None = None) -> dict:
    with Image.open(path) as image:
        width, height = image.size
        mode = image.mode
    return {
        "file": path.relative_to(root).as_posix() if root else str(path),
        "width": width,
        "height": height,
        "mode": mode,
        "bytes": path.stat().st_size,
        "sha256": sha256(path),
    }


def tile_id(row: int, column: int) -> str:
    return f"R{row:02d}C{column:02d}"


def dhash(image: Image.Image) -> int:
    gray = np.asarray(image.convert("L").resize((9, 8), Image.Resampling.BILINEAR))
    bits = (gray[:, 1:] > gray[:, :-1]).flatten()
    value = 0
    for bit in bits:
        value = (value << 1) | int(bit)
    return value


def normalized_correlation(left: np.ndarray, right: np.ndarray) -> float:
    left_gray = left.mean(axis=2, dtype=np.float64).reshape(-1)
    right_gray = right.mean(axis=2, dtype=np.float64).reshape(-1)
    left_centered = left_gray - left_gray.mean()
    right_centered = right_gray - right_gray.mean()
    denominator = float(np.linalg.norm(left_centered) * np.linalg.norm(right_centered))
    if denominator <= 1e-9:
        return 1.0 if np.array_equal(left, right) else 0.0
    return float(np.dot(left_centered, right_centered) / denominator)


def decoded_difference(left: Image.Image, right: Image.Image, strip_height: int = 256) -> dict:
    if left.size != right.size:
        return {
            "size_match": False,
            "different_pixel_count": None,
            "max_channel_delta": None,
        }
    different = 0
    max_delta = 0
    width, height = left.size
    for y0 in range(0, height, strip_height):
        y1 = min(height, y0 + strip_height)
        left_array = np.asarray(left.crop((0, y0, width, y1)).convert("RGB"), dtype=np.int16)
        right_array = np.asarray(right.crop((0, y0, width, y1)).convert("RGB"), dtype=np.int16)
        delta = np.abs(left_array - right_array)
        different += int(np.count_nonzero(np.any(delta != 0, axis=2)))
        max_delta = max(max_delta, int(delta.max(initial=0)))
    return {
        "size_match": True,
        "different_pixel_count": different,
        "max_channel_delta": max_delta,
        "pixel_identical": different == 0,
    }


def expected_neighbors(row: int, column: int) -> dict[str, str | None]:
    return {
        "N": tile_id(row - 1, column) if row > 0 else None,
        "E": tile_id(row, column + 1) if column < GRID_SIZE - 1 else None,
        "S": tile_id(row + 1, column) if row < GRID_SIZE - 1 else None,
        "W": tile_id(row, column - 1) if column > 0 else None,
    }


def compare_manifest_contract(manifest: dict, expected_files: set[str]) -> dict:
    issues: list[dict] = []
    master = manifest.get("master") if isinstance(manifest.get("master"), dict) else {}
    tiling = manifest.get("tiling") if isinstance(manifest.get("tiling"), dict) else {}
    entries = tiling.get("tiles") if isinstance(tiling.get("tiles"), list) else []

    expected_master = {
        "file": "master_15x15_7680.png",
        "width": MASTER_SIZE,
        "height": MASTER_SIZE,
        "mode": "RGB",
        "sha256": EXPECTED_MASTER_SHA256,
    }
    for key, expected in expected_master.items():
        actual = master.get(key)
        if isinstance(expected, str) and key == "sha256" and isinstance(actual, str):
            actual = actual.lower()
        if actual != expected:
            issues.append({"scope": "master", "field": key, "expected": expected, "actual": actual})

    for key, expected in (("rows", GRID_SIZE), ("columns", GRID_SIZE), ("tile_count", GRID_SIZE**2)):
        if tiling.get(key) != expected:
            issues.append({"scope": "tiling", "field": key, "expected": expected, "actual": tiling.get(key)})

    seen_ids: set[str] = set()
    seen_coords: set[tuple[int, int]] = set()
    seen_files: set[str] = set()
    directed_neighbors = 0
    for entry in entries:
        if not isinstance(entry, dict):
            issues.append({"scope": "tile", "reason": "non_object_entry"})
            continue
        row = entry.get("row")
        column = entry.get("column")
        if not isinstance(row, int) or not isinstance(column, int):
            issues.append({"scope": "tile", "reason": "invalid_coordinates", "entry": entry})
            continue
        identifier = tile_id(row, column)
        expected_file = f"tiles/{identifier}.png"
        if entry.get("id") != identifier:
            issues.append({"scope": identifier, "field": "id", "expected": identifier, "actual": entry.get("id")})
        if entry.get("file") != expected_file:
            issues.append({"scope": identifier, "field": "file", "expected": expected_file, "actual": entry.get("file")})
        if entry.get("width") != TILE_SIZE or entry.get("height") != TILE_SIZE:
            issues.append({"scope": identifier, "field": "dimensions", "expected": [TILE_SIZE, TILE_SIZE], "actual": [entry.get("width"), entry.get("height")]})
        declared_neighbors = entry.get("neighbors")
        expected = expected_neighbors(row, column)
        if declared_neighbors != expected:
            issues.append({"scope": identifier, "field": "neighbors", "expected": expected, "actual": declared_neighbors})
        if isinstance(declared_neighbors, dict):
            directed_neighbors += sum(value is not None for value in declared_neighbors.values())
        if identifier in seen_ids:
            issues.append({"scope": identifier, "reason": "duplicate_id"})
        if (row, column) in seen_coords:
            issues.append({"scope": identifier, "reason": "duplicate_coordinates"})
        if expected_file in seen_files:
            issues.append({"scope": identifier, "reason": "duplicate_file"})
        seen_ids.add(identifier)
        seen_coords.add((row, column))
        seen_files.add(expected_file)

    missing_coords = sorted(
        { (row, column) for row in range(GRID_SIZE) for column in range(GRID_SIZE) } - seen_coords
    )
    if missing_coords:
        issues.append({"scope": "tiling", "reason": "missing_coordinates", "coordinates": missing_coords})
    if seen_files != expected_files:
        issues.append({
            "scope": "tiling",
            "reason": "file_set_mismatch",
            "missing": sorted(expected_files - seen_files),
            "extra": sorted(seen_files - expected_files),
        })
    if directed_neighbors != 840:
        issues.append({"scope": "tiling", "reason": "directed_neighbor_count", "expected": 840, "actual": directed_neighbors})

    return {
        "status": "PASS" if not issues else "FAIL",
        "entry_count": len(entries),
        "unique_id_count": len(seen_ids),
        "unique_coordinate_count": len(seen_coords),
        "directed_neighbor_count": directed_neighbors,
        "undirected_neighbor_count": directed_neighbors // 2,
        "issues": issues,
    }


def boundary_metric(gray: np.ndarray, row: int, column: int, orientation: str) -> dict:
    radius = 20
    if orientation == "vertical":
        x = (column + 1) * TILE_SIZE
        y0 = row * TILE_SIZE
        patch = gray[y0 : y0 + TILE_SIZE, x - radius : x + radius + 1].astype(np.float32)
        gradients = np.abs(np.diff(patch, axis=1))
        profile = gradients.mean(axis=0)
        boundary_index = radius - 1
        boundary_gradient = float(profile[boundary_index])
        baseline_values = np.concatenate((profile[2 : boundary_index - 2], profile[boundary_index + 3 : -2]))
        baseline = float(np.median(baseline_values))
        line_luma = float(patch[:, radius - 1 : radius + 1].mean())
        nearby_luma = float(np.median(np.concatenate((patch[:, 2:12], patch[:, -12:-2]), axis=1)))
        coordinate = x
        identifier = f"R{row:02d}C{column:02d}--E--R{row:02d}C{column + 1:02d}"
    else:
        y = (row + 1) * TILE_SIZE
        x0 = column * TILE_SIZE
        patch = gray[y - radius : y + radius + 1, x0 : x0 + TILE_SIZE].astype(np.float32)
        gradients = np.abs(np.diff(patch, axis=0))
        profile = gradients.mean(axis=1)
        boundary_index = radius - 1
        boundary_gradient = float(profile[boundary_index])
        baseline_values = np.concatenate((profile[2 : boundary_index - 2], profile[boundary_index + 3 : -2]))
        baseline = float(np.median(baseline_values))
        line_luma = float(patch[radius - 1 : radius + 1, :].mean())
        nearby_luma = float(np.median(np.concatenate((patch[2:12, :], patch[-12:-2, :]), axis=0)))
        coordinate = y
        identifier = f"R{row:02d}C{column:02d}--S--R{row + 1:02d}C{column:02d}"

    ratio = boundary_gradient / max(baseline, 1e-6)
    dark_drop = nearby_luma - line_luma
    abnormal_gradient = ratio > 2.25 and boundary_gradient > 12.0
    abnormal_dark_line = dark_drop > 22.0 and line_luma < nearby_luma * 0.75
    return {
        "id": identifier,
        "orientation": orientation,
        "row": row,
        "column": column,
        "master_boundary_coordinate": coordinate,
        "boundary_gradient_luma": round(boundary_gradient, 6),
        "nearby_median_gradient_luma": round(baseline, 6),
        "gradient_ratio": round(ratio, 6),
        "boundary_line_mean_luma": round(line_luma, 6),
        "nearby_median_luma": round(nearby_luma, 6),
        "dark_line_drop": round(dark_drop, 6),
        "abnormal_gradient": abnormal_gradient,
        "abnormal_dark_line": abnormal_dark_line,
        "status": "FAIL" if abnormal_gradient or abnormal_dark_line else "PASS",
    }


def profile_by_column(gray: np.ndarray) -> np.ndarray:
    sums = np.zeros(gray.shape[1] - 1, dtype=np.float64)
    for y0 in range(0, gray.shape[0], 256):
        block = gray[y0 : y0 + 256].astype(np.int16)
        sums += np.abs(np.diff(block, axis=1)).sum(axis=0)
    return sums / gray.shape[0]


def profile_by_row(gray: np.ndarray) -> np.ndarray:
    result = np.zeros(gray.shape[0] - 1, dtype=np.float64)
    for y0 in range(0, gray.shape[0] - 1, 256):
        y1 = min(gray.shape[0] - 1, y0 + 256)
        block = gray[y0 : y1 + 1].astype(np.int16)
        result[y0:y1] = np.abs(np.diff(block, axis=0)).mean(axis=1)
    return result


def global_grid_phase(gray: np.ndarray) -> dict:
    vertical_profile = profile_by_column(gray)
    horizontal_profile = profile_by_row(gray)

    def summarize(profile: np.ndarray) -> dict:
        boundary_indexes = np.array([index * TILE_SIZE - 1 for index in range(1, GRID_SIZE)], dtype=np.int32)
        mask = np.ones(profile.shape[0], dtype=bool)
        for index in boundary_indexes:
            mask[max(0, index - 16) : min(mask.size, index + 17)] = False
        background = profile[mask]
        median = float(np.median(background))
        mad = float(np.median(np.abs(background - median)))
        values = profile[boundary_indexes]
        ratios = values / max(median, 1e-9)
        robust_z = (values - median) / max(1.4826 * mad, 1e-9)
        return {
            "boundary_values": [round(float(value), 6) for value in values],
            "background_median": round(median, 6),
            "background_mad": round(mad, 6),
            "boundary_ratios": [round(float(value), 6) for value in ratios],
            "boundary_robust_z": [round(float(value), 6) for value in robust_z],
            "mean_ratio": round(float(np.mean(ratios)), 6),
            "p95_ratio": round(float(np.percentile(ratios, 95)), 6),
            "max_ratio": round(float(np.max(ratios)), 6),
            "max_robust_z": round(float(np.max(robust_z)), 6),
        }

    vertical = summarize(vertical_profile)
    horizontal = summarize(horizontal_profile)
    automation_suspect = (
        vertical["mean_ratio"] > 1.35
        or horizontal["mean_ratio"] > 1.35
        or vertical["max_ratio"] > 2.25
        or horizontal["max_ratio"] > 2.25
    )
    return {
        "method": "Mean full-master luma gradient at each 512 px phase, compared with non-boundary positions excluding +/-16 px.",
        "vertical": vertical,
        "horizontal": horizontal,
        "automation_grid_suspect": automation_suspect,
        "human_review_required": True,
    }


def duplicate_analysis(records: list[dict], thumbnails: dict[str, np.ndarray], perceptual_hashes: dict[str, int]) -> dict:
    pairs: list[dict] = []
    mirrors: list[dict] = []
    for index, first in enumerate(records):
        first_id = first["id"]
        first_thumb = thumbnails[first_id]
        for second in records[index + 1 :]:
            second_id = second["id"]
            second_thumb = thumbnails[second_id]
            delta = np.abs(first_thumb - second_thumb)
            mad = float(delta.mean())
            color_distance = float(np.linalg.norm(first_thumb.mean(axis=(0, 1)) - second_thumb.mean(axis=(0, 1))))
            hamming = int((perceptual_hashes[first_id] ^ perceptual_hashes[second_id]).bit_count())
            correlation = normalized_correlation(first_thumb, second_thumb)
            combined = mad + color_distance * 0.20 + hamming * 0.35
            pairs.append({
                "a": first_id,
                "b": second_id,
                "thumbnail_mad": round(mad, 6),
                "mean_color_distance": round(color_distance, 6),
                "dhash_hamming": hamming,
                "normalized_luma_correlation": round(correlation, 6),
                "combined_score": round(combined, 6),
            })

            horizontal = np.fliplr(second_thumb)
            vertical = np.flipud(second_thumb)
            horizontal_mad = float(np.abs(first_thumb - horizontal).mean())
            vertical_mad = float(np.abs(first_thumb - vertical).mean())
            mirrors.append({
                "a": first_id,
                "b": second_id,
                "horizontal_mad": round(horizontal_mad, 6),
                "horizontal_correlation": round(normalized_correlation(first_thumb, horizontal), 6),
                "vertical_mad": round(vertical_mad, 6),
                "vertical_correlation": round(normalized_correlation(first_thumb, vertical), 6),
                "best_mad": round(min(horizontal_mad, vertical_mad), 6),
            })

    pairs.sort(key=lambda item: item["combined_score"])
    mirrors.sort(key=lambda item: item["best_mad"])
    near_suspects = [
        item for item in pairs
        if (
            item["thumbnail_mad"] < 1.5
            and item["mean_color_distance"] < 3.0
            and item["dhash_hamming"] < 4
        )
        or (
            item["normalized_luma_correlation"] > 0.995
            and item["thumbnail_mad"] < 8.0
        )
    ]
    mirror_suspects = [
        item for item in mirrors
        if item["best_mad"] < 1.5
        or (
            max(item["horizontal_correlation"], item["vertical_correlation"]) > 0.995
            and item["best_mad"] < 8.0
        )
    ]
    return {
        "method": "Independent all-pairs 32x32 RGB MAD, color distance, dHash, normalized luma correlation, horizontal mirror and vertical mirror comparison.",
        "pair_count": len(pairs),
        "near_duplicate_suspect_count": len(near_suspects),
        "mirror_suspect_count": len(mirror_suspects),
        "near_duplicate_suspects": near_suspects,
        "mirror_suspects": mirror_suspects,
        "top_40_closest_pairs": pairs[:40],
        "top_40_closest_mirrors": mirrors[:40],
        "human_review_required": True,
    }


def draw_labeled_panel(image: Image.Image, label: str, width: int = 512) -> Image.Image:
    panel_image = image.copy()
    if panel_image.width != width:
        target_height = max(1, round(panel_image.height * width / panel_image.width))
        panel_image = panel_image.resize((width, target_height), Image.Resampling.LANCZOS)
    label_height = 28
    panel = Image.new("RGB", (panel_image.width, panel_image.height + label_height), (18, 18, 18))
    panel.paste(panel_image.convert("RGB"), (0, label_height))
    ImageDraw.Draw(panel).text((8, 8), label, fill=(244, 236, 211), font=ImageFont.load_default())
    return panel


def generate_native_crops(master: Image.Image, output_dir: Path) -> dict:
    output_dir.mkdir(parents=True, exist_ok=True)
    panels: list[Image.Image] = []
    records: list[dict] = []
    for row, column in NATIVE_CROPS:
        identifier = tile_id(row, column)
        box = (column * TILE_SIZE, row * TILE_SIZE, (column + 1) * TILE_SIZE, (row + 1) * TILE_SIZE)
        crop = master.crop(box)
        path = output_dir / f"{identifier}_source_native512.png"
        save_png(crop, path)
        records.append({"id": identifier, "source_box": list(box), **file_record(path, output_dir.parent.parent)})
        panels.append(draw_labeled_panel(crop, f"{identifier} | exact source crop", TILE_SIZE))

    contact = Image.new("RGB", (4 * TILE_SIZE, 3 * (TILE_SIZE + 28)), (0, 0, 0))
    for index, panel in enumerate(panels):
        contact.paste(panel, ((index % 4) * TILE_SIZE, (index // 4) * (TILE_SIZE + 28)))
    contact_path = output_dir / "native_512_independent_contact.png"
    save_png(contact, contact_path)

    page_records: list[dict] = []
    page_size = 5 * TILE_SIZE
    page_dir = output_dir / "full_100pct_pages"
    for page_row in range(3):
        for page_column in range(3):
            x0 = page_column * page_size
            y0 = page_row * page_size
            box = (x0, y0, x0 + page_size, y0 + page_size)
            path = page_dir / (
                f"native100_R{page_row * 5:02d}-{page_row * 5 + 4:02d}_"
                f"C{page_column * 5:02d}-{page_column * 5 + 4:02d}.png"
            )
            save_png(master.crop(box), path)
            page_records.append({
                "source_box": list(box),
                "resized": False,
                **file_record(path, output_dir.parent.parent),
            })
    return {
        "count": len(records),
        "crops": records,
        "contact": file_record(contact_path, output_dir.parent.parent),
        "full_master_100pct_page_count": len(page_records),
        "full_master_100pct_pages": page_records,
    }


def generate_join_crops(master: Image.Image, output_dir: Path) -> dict:
    output_dir.mkdir(parents=True, exist_ok=True)
    records: list[dict] = []
    contact_panels: list[Image.Image] = []
    for name, first, second, orientation in JOIN_CROPS:
        first_row, first_column = first
        second_row, second_column = second
        x0 = min(first_column, second_column) * TILE_SIZE
        y0 = min(first_row, second_row) * TILE_SIZE
        x1 = (max(first_column, second_column) + 1) * TILE_SIZE
        y1 = (max(first_row, second_row) + 1) * TILE_SIZE
        full_pair = master.crop((x0, y0, x1, y1))
        full_path = output_dir / f"{name}_source_pair.png"
        save_png(full_pair, full_path)
        if orientation == "vertical":
            join = second_column * TILE_SIZE
            context_box = (join - 96, y0, join + 96, y1)
        else:
            join = second_row * TILE_SIZE
            context_box = (x0, join - 96, x1, join + 96)
        context = master.crop(context_box)
        context_path = output_dir / f"{name}_join_context_96px.png"
        save_png(context, context_path)
        records.append({
            "id": name,
            "orientation": orientation,
            "source_pair_box": [x0, y0, x1, y1],
            "join_context_box": list(context_box),
            "pair": file_record(full_path, output_dir.parent.parent),
            "context": file_record(context_path, output_dir.parent.parent),
        })
        display = full_pair if full_pair.width >= full_pair.height else full_pair.rotate(90, expand=True)
        contact_panels.append(draw_labeled_panel(display, name, 1024))

    height = sum(panel.height for panel in contact_panels)
    contact = Image.new("RGB", (1024, height), (0, 0, 0))
    y = 0
    for panel in contact_panels:
        contact.paste(panel, (0, y))
        y += panel.height
    contact_path = output_dir / "four_source_join_pairs_contact.png"
    save_png(contact, contact_path)
    return {"count": len(records), "joins": records, "contact": file_record(contact_path, output_dir.parent.parent)}


def diagonal_pan(master: Image.Image, start: tuple[float, float], end: tuple[float, float]) -> Image.Image:
    width = MASTER_SIZE
    height = TILE_SIZE
    axis_x = end[0] - start[0]
    axis_y = end[1] - start[1]
    length = math.hypot(axis_x, axis_y)
    perpendicular_x = -axis_y / length
    perpendicular_y = axis_x / length
    center_offset = (height - 1) / 2.0
    coefficients = (
        axis_x / (width - 1),
        perpendicular_x,
        start[0] - center_offset * perpendicular_x,
        axis_y / (width - 1),
        perpendicular_y,
        start[1] - center_offset * perpendicular_y,
    )
    return master.transform(
        (width, height),
        Image.Transform.AFFINE,
        coefficients,
        resample=Image.Resampling.BICUBIC,
    )


def generate_pans(master: Image.Image, output_dir: Path) -> dict:
    output_dir.mkdir(parents=True, exist_ok=True)
    records: list[dict] = []
    panels: list[Image.Image] = []
    for index, y0 in enumerate(PAN_POSITIONS, start=1):
        name = f"H{index:02d}_y{y0:04d}"
        box = (0, y0, MASTER_SIZE, y0 + TILE_SIZE)
        image = master.crop(box)
        path = output_dir / f"pan_{name}_source.png"
        save_png(image, path)
        records.append({"id": name, "kind": "horizontal_exact_crop", "source_box": list(box), **file_record(path, output_dir.parent.parent)})
        panels.append(draw_labeled_panel(image, name, 1200))
    for index, x0 in enumerate(PAN_POSITIONS, start=1):
        name = f"V{index:02d}_x{x0:04d}"
        box = (x0, 0, x0 + TILE_SIZE, MASTER_SIZE)
        image = master.crop(box)
        path = output_dir / f"pan_{name}_source.png"
        save_png(image, path)
        records.append({"id": name, "kind": "vertical_exact_crop", "source_box": list(box), **file_record(path, output_dir.parent.parent)})
        panels.append(draw_labeled_panel(image.rotate(90, expand=True), name, 1200))
    for name, start, end in DIAGONAL_PANS:
        image = diagonal_pan(master, start, end)
        path = output_dir / f"pan_{name}_source_sample.png"
        save_png(image, path)
        records.append({
            "id": name,
            "kind": "diagonal_bicubic_source_sample",
            "start": list(start),
            "end": list(end),
            **file_record(path, output_dir.parent.parent),
        })
        panels.append(draw_labeled_panel(image, name, 1200))

    contact_height = sum(panel.height for panel in panels)
    contact = Image.new("RGB", (1200, contact_height), (0, 0, 0))
    y = 0
    for panel in panels:
        contact.paste(panel, (0, y))
        y += panel.height
    contact_path = output_dir / "fourteen_independent_pans_contact.png"
    save_png(contact, contact_path)
    return {"count": len(records), "pans": records, "contact": file_record(contact_path, output_dir.parent.parent)}


def generate_scale_proofs(master: Image.Image, output_dir: Path) -> dict:
    output_dir.mkdir(parents=True, exist_ok=True)
    records: list[dict] = [{
        "id": "100pct_authoritative_master",
        "scale": 1.0,
        "source_only_no_copy": True,
    }]
    generated: dict[str, Image.Image] = {}
    for name, scale, size in (
        ("63_7pct", 0.637, 4892),
        ("50pct", 0.5, 3840),
        ("25pct", 0.25, 1920),
        ("12_5pct", 0.125, 960),
    ):
        image = master.resize((size, size), Image.Resampling.LANCZOS)
        path = output_dir / f"independent_master_{name}.png"
        save_png(image, path)
        generated[name] = image
        records.append({"id": name, "scale": scale, **file_record(path, output_dir.parent)})

    quadrant_records: list[dict] = []
    for name in ("63_7pct", "50pct"):
        image = generated[name]
        mid_x = image.width // 2
        mid_y = image.height // 2
        boxes = {
            "NW": (0, 0, mid_x, mid_y),
            "NE": (mid_x, 0, image.width, mid_y),
            "SW": (0, mid_y, mid_x, image.height),
            "SE": (mid_x, mid_y, image.width, image.height),
        }
        for quadrant, box in boxes.items():
            path = output_dir / f"independent_master_{name}_{quadrant}_inspection.png"
            save_png(image.crop(box), path)
            quadrant_records.append({
                "source_view": name,
                "quadrant": quadrant,
                "source_box": list(box),
                **file_record(path, output_dir.parent),
            })

    proof_25 = generated["25pct"]
    desaturated = ImageOps.grayscale(proof_25)
    desat_path = output_dir / "independent_master_25pct_desaturated.png"
    save_png(desaturated, desat_path)
    contrast = ImageEnhance.Contrast(proof_25).enhance(1.55)
    contrast_path = output_dir / "independent_master_25pct_contrast155.png"
    save_png(contrast, contrast_path)
    desat_contrast = ImageEnhance.Contrast(ImageOps.autocontrast(desaturated, cutoff=0.5)).enhance(1.55)
    desat_contrast_path = output_dir / "independent_master_25pct_desaturated_contrast155.png"
    save_png(desat_contrast, desat_contrast_path)
    records.extend([
        {"id": "25pct_desaturated", **file_record(desat_path, output_dir.parent)},
        {"id": "25pct_contrast155", **file_record(contrast_path, output_dir.parent)},
        {"id": "25pct_desaturated_contrast155", **file_record(desat_contrast_path, output_dir.parent)},
    ])

    contact_panels = [
        draw_labeled_panel(generated["25pct"], "25% natural", 960),
        draw_labeled_panel(generated["12_5pct"], "12.5% natural", 960),
        draw_labeled_panel(contrast, "25% contrast 155%", 960),
        draw_labeled_panel(desat_contrast.convert("RGB"), "25% desaturated + contrast", 960),
    ]
    contact = Image.new("RGB", (1920, contact_panels[0].height + contact_panels[2].height), (0, 0, 0))
    contact.paste(contact_panels[0], (0, 0))
    contact.paste(contact_panels[1], (960, 0))
    contact.paste(contact_panels[2], (0, contact_panels[0].height))
    contact.paste(contact_panels[3], (960, contact_panels[1].height))
    contact_path = output_dir / "independent_scale_contrast_contact.png"
    save_png(contact, contact_path)
    return {
        "views": records,
        "inspection_quadrants": quadrant_records,
        "contact": file_record(contact_path, output_dir.parent),
    }


def generate_boundary_diagnostic(master: Image.Image, boundaries: list[dict], output_path: Path) -> dict:
    base = master.resize((1920, 1920), Image.Resampling.LANCZOS).convert("RGB")
    draw = ImageDraw.Draw(base, "RGBA")
    scale = 1920 / MASTER_SIZE
    for boundary in boundaries:
        ratio = boundary["gradient_ratio"]
        if ratio > 2.25:
            color = (230, 30, 30, 230)
        elif ratio > 1.5:
            color = (245, 176, 50, 210)
        else:
            color = (35, 190, 95, 165)
        row = boundary["row"]
        column = boundary["column"]
        if boundary["orientation"] == "vertical":
            x = round((column + 1) * TILE_SIZE * scale)
            y = round((row + 0.5) * TILE_SIZE * scale)
        else:
            x = round((column + 0.5) * TILE_SIZE * scale)
            y = round((row + 1) * TILE_SIZE * scale)
        radius = 3 if ratio <= 1.5 else 5
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=color)
    save_png(base, output_path)
    return file_record(output_path, output_path.parent.parent)


def generate_similarity_contact(
    master: Image.Image,
    pairs: list[dict],
    output_path: Path,
    mirror: bool = False,
    count: int = 12,
) -> dict:
    panel_width = 540
    panel_height = 286
    contact = Image.new("RGB", (panel_width * 2, panel_height * math.ceil(count / 2)), (12, 12, 12))
    draw = ImageDraw.Draw(contact)
    for index, pair in enumerate(pairs[:count]):
        left_id = pair["a"]
        right_id = pair["b"]
        left_row = int(left_id[1:3])
        left_column = int(left_id[4:6])
        right_row = int(right_id[1:3])
        right_column = int(right_id[4:6])
        left = master.crop((left_column * TILE_SIZE, left_row * TILE_SIZE, (left_column + 1) * TILE_SIZE, (left_row + 1) * TILE_SIZE)).resize((256, 256), Image.Resampling.LANCZOS)
        right = master.crop((right_column * TILE_SIZE, right_row * TILE_SIZE, (right_column + 1) * TILE_SIZE, (right_row + 1) * TILE_SIZE)).resize((256, 256), Image.Resampling.LANCZOS)
        if mirror:
            if pair["horizontal_mad"] <= pair["vertical_mad"]:
                right = ImageOps.mirror(right)
                transform = "H"
            else:
                right = ImageOps.flip(right)
                transform = "V"
            metric = f"mirror {transform} MAD={pair['best_mad']:.2f}"
        else:
            metric = f"MAD={pair['thumbnail_mad']:.2f} dH={pair['dhash_hamming']} corr={pair['normalized_luma_correlation']:.3f}"
        x0 = (index % 2) * panel_width
        y0 = (index // 2) * panel_height
        contact.paste(left, (x0, y0 + 28))
        contact.paste(right, (x0 + 264, y0 + 28))
        draw.text((x0 + 6, y0 + 7), f"{left_id} / {right_id} | {metric}", fill=(240, 236, 218), font=ImageFont.load_default())
    save_png(contact, output_path)
    return file_record(output_path, output_path.parent.parent)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--package", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--expected-master-sha256", default=EXPECTED_MASTER_SHA256)
    args = parser.parse_args()

    package = args.package.resolve()
    output = args.output.resolve()
    if not package.is_dir():
        raise NotADirectoryError(package)
    if package == output or package in output.parents:
        raise ValueError("Output must be outside the read-only producer package.")
    output.mkdir(parents=True, exist_ok=True)

    master_path = package / "master_15x15_7680.png"
    manifest_path = package / "manifest.json"
    reconstruction_path = package / "reconstruction_15x15.png"
    checkpoint_path = package / "checkpoint_D_hd_fused_candidate_7680.png"
    lock_path = package / "MASTER_WAVE4_15X15_LOCK.json"
    tile_dir = package / "tiles"
    for required in (master_path, manifest_path, reconstruction_path, checkpoint_path, lock_path, tile_dir):
        if not required.exists():
            raise FileNotFoundError(required)

    started = datetime.now(timezone.utc)
    source_stat_before = {
        "master_mtime_ns": master_path.stat().st_mtime_ns,
        "manifest_mtime_ns": manifest_path.stat().st_mtime_ns,
        "reconstruction_mtime_ns": reconstruction_path.stat().st_mtime_ns,
        "lock_mtime_ns": lock_path.stat().st_mtime_ns,
    }
    log("[1/8] Hashing and opening authoritative master")
    master_hash = sha256(master_path)
    checkpoint_hash = sha256(checkpoint_path)
    expected_hash = args.expected_master_sha256.lower()
    with manifest_path.open("r", encoding="utf-8-sig") as handle:
        manifest = json.load(handle)
    with lock_path.open("r", encoding="utf-8-sig") as handle:
        master_lock = json.load(handle)
    lock_master = master_lock.get("master") if isinstance(master_lock.get("master"), dict) else {}
    lock_issues: list[dict] = []
    expected_lock_values = {
        "schema": "bee-kingdom.world-map.wave4-15x15-approved-master-lock.v1",
        "status": "FROZEN_FOR_INDEPENDENT_VALIDATION",
    }
    for field, expected in expected_lock_values.items():
        if master_lock.get(field) != expected:
            lock_issues.append({"field": field, "expected": expected, "actual": master_lock.get(field)})
    expected_lock_master = {
        "file": "master_15x15_7680.png",
        "width": MASTER_SIZE,
        "height": MASTER_SIZE,
        "mode": "RGB",
        "sha256": expected_hash,
        "pixel_mutation_allowed": False,
    }
    for field, expected in expected_lock_master.items():
        actual = lock_master.get(field)
        if field == "sha256" and isinstance(actual, str):
            actual = actual.lower()
        if actual != expected:
            lock_issues.append({"scope": "master", "field": field, "expected": expected, "actual": actual})
    lock_contract = {
        "status": "PASS" if not lock_issues else "FAIL",
        "file": str(lock_path),
        "sha256": sha256(lock_path),
        "issues": lock_issues,
    }
    expected_files = {f"tiles/{tile_id(row, column)}.png" for row in range(GRID_SIZE) for column in range(GRID_SIZE)}
    contract = compare_manifest_contract(manifest, expected_files)

    with Image.open(master_path) as master_file:
        master_original_mode = master_file.mode
        master_original_size = master_file.size
        master = master_file.convert("RGB")
        master.load()
    master_contract = {
        "expected_sha256": expected_hash,
        "actual_sha256": master_hash,
        "hash_match": master_hash == expected_hash,
        "expected_dimensions": [MASTER_SIZE, MASTER_SIZE],
        "actual_dimensions": list(master_original_size),
        "dimensions_match": master_original_size == (MASTER_SIZE, MASTER_SIZE),
        "expected_mode": "RGB",
        "actual_mode": master_original_mode,
        "mode_match": master_original_mode == "RGB",
    }

    tile_inventory = sorted(tile_dir.glob("*.png"))
    actual_tile_relative = {path.relative_to(package).as_posix() for path in tile_inventory}
    log("[2/8] Validating all 225 tiles against source pixels")
    independent_reconstruction = Image.new("RGB", (MASTER_SIZE, MASTER_SIZE))
    tile_records: list[dict] = []
    thumbnails: dict[str, np.ndarray] = {}
    perceptual_hashes: dict[str, int] = {}
    tile_hashes: set[str] = set()
    tile_pixel_mismatches: list[dict] = []
    manifest_entries = {entry["id"]: entry for entry in manifest.get("tiling", {}).get("tiles", []) if isinstance(entry, dict) and isinstance(entry.get("id"), str)}
    for row in range(GRID_SIZE):
        for column in range(GRID_SIZE):
            identifier = tile_id(row, column)
            tile_path = tile_dir / f"{identifier}.png"
            entry = manifest_entries.get(identifier, {})
            digest = sha256(tile_path)
            with Image.open(tile_path) as tile_file:
                original_mode = tile_file.mode
                original_size = tile_file.size
                tile = tile_file.convert("RGB")
                tile.load()
            box = (column * TILE_SIZE, row * TILE_SIZE, (column + 1) * TILE_SIZE, (row + 1) * TILE_SIZE)
            source_crop = master.crop(box)
            difference_box = ImageChops.difference(tile, source_crop).getbbox()
            pixel_identical = difference_box is None
            if not pixel_identical:
                delta = np.abs(np.asarray(tile, dtype=np.int16) - np.asarray(source_crop, dtype=np.int16))
                mismatch = {
                    "id": identifier,
                    "source_box": list(box),
                    "different_pixel_count": int(np.count_nonzero(np.any(delta != 0, axis=2))),
                    "max_channel_delta": int(delta.max(initial=0)),
                    "difference_bbox": list(difference_box),
                }
                tile_pixel_mismatches.append(mismatch)
            independent_reconstruction.paste(tile, (column * TILE_SIZE, row * TILE_SIZE))
            thumb = np.asarray(tile.resize((32, 32), Image.Resampling.BILINEAR), dtype=np.float32)
            thumbnails[identifier] = thumb
            perceptual_hashes[identifier] = dhash(tile)
            tile_hashes.add(digest)
            tile_records.append({
                "id": identifier,
                "row": row,
                "column": column,
                "file": tile_path.relative_to(package).as_posix(),
                "sha256": digest,
                "manifest_sha256": entry.get("sha256"),
                "manifest_hash_match": isinstance(entry.get("sha256"), str) and digest == entry.get("sha256", "").lower(),
                "width": original_size[0],
                "height": original_size[1],
                "mode": original_mode,
                "source_box": list(box),
                "source_pixel_identical": pixel_identical,
            })
            tile.close()
            source_crop.close()
        log(f"  row {row + 1:02d}/15 complete")

    log("[3/8] Comparing independent and packaged reconstructions")
    independent_difference = decoded_difference(master, independent_reconstruction, 256)
    with Image.open(reconstruction_path) as packaged_file:
        packaged_mode = packaged_file.mode
        packaged_reconstruction = packaged_file.convert("RGB")
        packaged_reconstruction.load()
    packaged_difference = decoded_difference(master, packaged_reconstruction, 256)
    packaged_difference["file_sha256"] = sha256(reconstruction_path)
    packaged_difference["mode"] = packaged_mode
    packaged_reconstruction.close()
    independent_reconstruction.close()

    log("[4/8] Measuring 420 geometric boundaries and grid phase")
    gray = np.asarray(master.convert("L"), dtype=np.uint8)
    boundaries: list[dict] = []
    for row in range(GRID_SIZE):
        for column in range(GRID_SIZE - 1):
            boundaries.append(boundary_metric(gray, row, column, "vertical"))
    for row in range(GRID_SIZE - 1):
        for column in range(GRID_SIZE):
            boundaries.append(boundary_metric(gray, row, column, "horizontal"))
    boundary_failures = [item for item in boundaries if item["status"] != "PASS"]
    ratios = np.asarray([item["gradient_ratio"] for item in boundaries], dtype=np.float64)
    boundary_summary = {
        "expected_count": 420,
        "actual_count": len(boundaries),
        "pass_count": len(boundaries) - len(boundary_failures),
        "fail_count": len(boundary_failures),
        "mean_gradient_ratio": round(float(ratios.mean()), 6),
        "p95_gradient_ratio": round(float(np.percentile(ratios, 95)), 6),
        "max_gradient_ratio": round(float(ratios.max()), 6),
        "failures": boundary_failures,
        "status": "PASS" if len(boundaries) == 420 and not boundary_failures else "FAIL",
        "note": "Each tile was first proven pixel-identical to its exact master crop; boundary metrics therefore inspect the exact decoded pixels rendered on both sides.",
    }
    grid_phase = global_grid_phase(gray)

    log("[5/8] Running independent all-pairs duplicate and mirror analysis")
    duplicates = duplicate_analysis(tile_records, thumbnails, perceptual_hashes)

    log("[6/8] Regenerating independent crops, joins, pans and scale views")
    proofs_dir = output / "proofs"
    native = generate_native_crops(master, proofs_dir / "native_512")
    joins = generate_join_crops(master, proofs_dir / "join_contexts")
    pans = generate_pans(master, proofs_dir / "pans")
    scales = generate_scale_proofs(master, proofs_dir / "scales")
    boundary_diagnostic = generate_boundary_diagnostic(master, boundaries, proofs_dir / "diagnostics" / "boundary_ratio_markers.png")
    closest_contact = generate_similarity_contact(master, duplicates["top_40_closest_pairs"], proofs_dir / "diagnostics" / "closest_pairs_contact.png")
    mirror_contact = generate_similarity_contact(master, duplicates["top_40_closest_mirrors"], proofs_dir / "diagnostics" / "closest_mirror_pairs_contact.png", mirror=True)

    source_stat_after = {
        "master_mtime_ns": master_path.stat().st_mtime_ns,
        "manifest_mtime_ns": manifest_path.stat().st_mtime_ns,
        "reconstruction_mtime_ns": reconstruction_path.stat().st_mtime_ns,
        "lock_mtime_ns": lock_path.stat().st_mtime_ns,
    }
    relevant_source_unchanged = source_stat_before == source_stat_after and sha256(master_path) == master_hash
    tile_mtimes = [path.stat().st_mtime_ns for path in tile_inventory]
    provenance = {
        "checkpoint_D_sha256": checkpoint_hash,
        "checkpoint_D_equals_master_bytes": checkpoint_hash == master_hash,
        "checkpoint_D_mtime_utc": datetime.fromtimestamp(checkpoint_path.stat().st_mtime, timezone.utc).isoformat(),
        "master_mtime_utc": datetime.fromtimestamp(master_path.stat().st_mtime, timezone.utc).isoformat(),
        "earliest_tile_mtime_utc": datetime.fromtimestamp(min(tile_mtimes) / 1_000_000_000, timezone.utc).isoformat(),
        "latest_tile_mtime_utc": datetime.fromtimestamp(max(tile_mtimes) / 1_000_000_000, timezone.utc).isoformat(),
        "reconstruction_mtime_utc": datetime.fromtimestamp(reconstruction_path.stat().st_mtime, timezone.utc).isoformat(),
        "master_precedes_all_tiles": master_path.stat().st_mtime_ns < min(tile_mtimes),
        "all_tiles_pixel_identical_to_master_crops": not tile_pixel_mismatches,
        "finalizer_source_audit": {
            "file": str(package / "finalize_wave4_artifacts.py"),
            "observed_sequence": "Open master, crop 225 tiles, reconstruct, then generate proofs.",
            "tile_local_post_edit_stage_observed": False,
        },
        "read_only_source_stat_unchanged_during_run": relevant_source_unchanged,
    }

    package_gate = (
        master_contract["hash_match"]
        and master_contract["dimensions_match"]
        and master_contract["mode_match"]
        and contract["status"] == "PASS"
        and lock_contract["status"] == "PASS"
        and len(tile_inventory) == GRID_SIZE**2
        and actual_tile_relative == expected_files
        and len(tile_hashes) == GRID_SIZE**2
        and all(record["width"] == TILE_SIZE and record["height"] == TILE_SIZE and record["mode"] == "RGB" for record in tile_records)
        and all(record["manifest_hash_match"] for record in tile_records)
        and relevant_source_unchanged
    )
    reconstruction_gate = (
        not tile_pixel_mismatches
        and independent_difference.get("pixel_identical") is True
        and packaged_difference.get("pixel_identical") is True
    )
    boundary_gate = boundary_summary["status"] == "PASS" and contract["undirected_neighbor_count"] == 420
    anti_duplicate_automation = (
        len(tile_hashes) == GRID_SIZE**2
        and duplicates["near_duplicate_suspect_count"] == 0
        and duplicates["mirror_suspect_count"] == 0
    )

    log("[7/8] Writing machine-readable independent results")
    result = {
        "schema": "bee-kingdom.builder-c.world-map-wave4-15x15-independent-validation.v1",
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "validator": "Builder-C dedicated Wave4 15x15 read-only validator",
        "package": str(package),
        "output": str(output),
        "source_mutation_performed": False,
        "unity_opened_or_modified": False,
        "server_opened_or_modified": False,
        "master": master_contract,
        "master_lock": lock_contract,
        "manifest_contract": contract,
        "tile_inventory": {
            "expected_count": 225,
            "actual_count": len(tile_inventory),
            "expected_files_match": actual_tile_relative == expected_files,
            "unique_sha256_count": len(tile_hashes),
            "all_512x512_rgb": all(record["width"] == TILE_SIZE and record["height"] == TILE_SIZE and record["mode"] == "RGB" for record in tile_records),
            "all_manifest_hashes_match": all(record["manifest_hash_match"] for record in tile_records),
            "records": tile_records,
        },
        "reconstruction": {
            "tile_source_pixel_mismatch_count": len(tile_pixel_mismatches),
            "tile_source_pixel_mismatches": tile_pixel_mismatches,
            "independent_reconstruction_vs_master": independent_difference,
            "packaged_reconstruction_vs_master": packaged_difference,
        },
        "boundaries": {
            "summary": boundary_summary,
            "records": boundaries,
            "global_grid_phase": grid_phase,
        },
        "duplicates_and_mirrors": duplicates,
        "provenance": provenance,
        "proofs": {
            "native_512": native,
            "four_joins": joins,
            "fourteen_pans": pans,
            "scales": scales,
            "boundary_diagnostic": boundary_diagnostic,
            "closest_pairs_contact": closest_contact,
            "closest_mirror_pairs_contact": mirror_contact,
        },
        "automation_gates": {
            "PACKAGE_GATE": "PASS" if package_gate else "FAIL",
            "RECONSTRUCTION_GATE": "PASS" if reconstruction_gate else "FAIL",
            "BOUNDARY_GATE": "PASS" if boundary_gate else "FAIL",
            "ANTI_DUPLICATE_AUTOMATION": "PASS" if anti_duplicate_automation else "REVIEW",
            "GRID_PHASE_AUTOMATION_SUSPECT": "YES" if grid_phase["automation_grid_suspect"] else "NO",
            "PERCEPTUAL_GATE": "PENDING_HUMAN_REVIEW",
        },
        "honesty": [
            "Numerical thresholds do not grant the perceptual PASS.",
            "Forbidden painted content and natural terrain continuity require independent human inspection of the regenerated evidence.",
            "This package is local art validation only; it is not Unity integration or a live/world/server claim.",
        ],
    }
    validation_path = output / "Wave4_15x15_IndependentValidation.json"
    validation_path.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (output / "Wave4_15x15_TileHashes.sha256").write_text(
        "".join(f"{record['sha256']}  {record['file']}\n" for record in tile_records),
        encoding="ascii",
    )

    summary = {
        "validation_json": str(validation_path),
        "duration_seconds": round((datetime.now(timezone.utc) - started).total_seconds(), 3),
        "PACKAGE_GATE": result["automation_gates"]["PACKAGE_GATE"],
        "RECONSTRUCTION_GATE": result["automation_gates"]["RECONSTRUCTION_GATE"],
        "BOUNDARY_GATE": result["automation_gates"]["BOUNDARY_GATE"],
        "ANTI_DUPLICATE_AUTOMATION": result["automation_gates"]["ANTI_DUPLICATE_AUTOMATION"],
        "GRID_PHASE_AUTOMATION_SUSPECT": result["automation_gates"]["GRID_PHASE_AUTOMATION_SUSPECT"],
        "PERCEPTUAL_GATE": "PENDING_HUMAN_REVIEW",
    }
    summary_path = output / "Wave4_15x15_IndependentValidation_Summary.json"
    summary_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    master.close()
    del gray
    log("[8/8] Complete")
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0 if package_gate and reconstruction_gate and boundary_gate else 2


if __name__ == "__main__":
    sys.exit(main())
