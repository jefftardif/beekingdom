#!/usr/bin/env python3
"""Independent read-only validator for the Wave5 25x25 art package."""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import math
import sys
from datetime import datetime, timezone
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageFont


Image.MAX_IMAGE_PIXELS = None

GRID_SIZE = 25
TILE_SIZE = 512
MASTER_SIZE = GRID_SIZE * TILE_SIZE
EXPECTED_MASTER_SHA256 = "50f3ff9640251f365484f31de4aa5ab542587381e5f8eeb9324d67be37125913"


def load_wave4_helpers():
    helper_path = Path(__file__).with_name("validate_wave4_15x15.py")
    spec = importlib.util.spec_from_file_location("wave4_validation_helpers", helper_path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load helper module: {helper_path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.GRID_SIZE = GRID_SIZE
    module.TILE_SIZE = TILE_SIZE
    module.MASTER_SIZE = MASTER_SIZE
    return module


W4 = load_wave4_helpers()


def log(message: str) -> None:
    print(message, flush=True)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(4 * 1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8-sig") as handle:
        value = json.load(handle)
    if not isinstance(value, dict):
        raise ValueError(f"Expected a JSON object: {path}")
    return value


def tile_id(row: int, column: int) -> str:
    return f"R{row:02d}C{column:02d}"


def expected_neighbors(row: int, column: int) -> dict[str, str | None]:
    return {
        "N": tile_id(row - 1, column) if row > 0 else None,
        "E": tile_id(row, column + 1) if column < GRID_SIZE - 1 else None,
        "S": tile_id(row + 1, column) if row < GRID_SIZE - 1 else None,
        "W": tile_id(row, column - 1) if column > 0 else None,
    }


def decoded_difference(left: Image.Image, right: Image.Image, strip_height: int = 128) -> dict:
    if left.size != right.size:
        return {
            "size_match": False,
            "different_pixel_count": None,
            "different_channel_count": None,
            "max_channel_delta": None,
            "pixel_identical": False,
        }
    different_pixels = 0
    different_channels = 0
    max_delta = 0
    width, height = left.size
    for y0 in range(0, height, strip_height):
        y1 = min(height, y0 + strip_height)
        a = np.asarray(left.crop((0, y0, width, y1)).convert("RGB"), dtype=np.int16)
        b = np.asarray(right.crop((0, y0, width, y1)).convert("RGB"), dtype=np.int16)
        delta = np.abs(a - b)
        different_pixels += int(np.count_nonzero(np.any(delta != 0, axis=2)))
        different_channels += int(np.count_nonzero(delta))
        max_delta = max(max_delta, int(delta.max(initial=0)))
    return {
        "size_match": True,
        "different_pixel_count": different_pixels,
        "different_channel_count": different_channels,
        "max_channel_delta": max_delta,
        "pixel_identical": different_pixels == 0,
    }


def image_record(path: Path, root: Path) -> dict:
    with Image.open(path) as image:
        size = image.size
        mode = image.mode
    return {
        "file": path.relative_to(root).as_posix(),
        "width": size[0],
        "height": size[1],
        "mode": mode,
        "bytes": path.stat().st_size,
        "sha256": sha256(path),
    }


def save_png(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, format="PNG", optimize=False, compress_level=5)


def validate_lock(lock: dict, lock_path: Path, expected_hash: str) -> dict:
    issues: list[dict] = []
    if lock.get("schema") != "bee-kingdom.world-map.wave5-25x25-checkpoint-g-lock.v1":
        issues.append({"field": "schema", "actual": lock.get("schema")})
    candidate = lock.get("candidate") if isinstance(lock.get("candidate"), dict) else {}
    master = lock.get("locked_master") if isinstance(lock.get("locked_master"), dict) else {}
    expected_candidate = {
        "file": "checkpoint_G_master_candidate_12800.png",
        "sha256": expected_hash,
    }
    expected_master = {
        "file": "master_25x25_12800.png",
        "width": MASTER_SIZE,
        "height": MASTER_SIZE,
        "sha256": expected_hash,
        "read_only": True,
        "source_of_truth_for_future_tile_cut": True,
    }
    for scope, actual_block, expected_block in (
        ("candidate", candidate, expected_candidate),
        ("locked_master", master, expected_master),
    ):
        for field, expected in expected_block.items():
            actual = actual_block.get(field)
            if field == "sha256" and isinstance(actual, str):
                actual = actual.lower()
            if actual != expected:
                issues.append({"scope": scope, "field": field, "expected": expected, "actual": actual})
    return {
        "status": "PASS" if not issues else "FAIL",
        "file": str(lock_path),
        "sha256": sha256(lock_path),
        "issues": issues,
    }


def validate_manifest(manifest: dict, expected_hash: str) -> dict:
    issues: list[dict] = []
    if manifest.get("schema") != "bee-kingdom.world-map.wave5-25x25-tile-manifest.v1":
        issues.append({"field": "schema", "actual": manifest.get("schema")})
    master = manifest.get("master") if isinstance(manifest.get("master"), dict) else {}
    grid = manifest.get("grid") if isinstance(manifest.get("grid"), dict) else {}
    entries = manifest.get("tiles") if isinstance(manifest.get("tiles"), list) else []
    for field, expected in {
        "file": "master_25x25_12800.png",
        "width": MASTER_SIZE,
        "height": MASTER_SIZE,
        "sha256": expected_hash,
        "read_only_source": True,
    }.items():
        actual = master.get(field)
        if field == "sha256" and isinstance(actual, str):
            actual = actual.lower()
        if actual != expected:
            issues.append({"scope": "master", "field": field, "expected": expected, "actual": actual})
    for field, expected in {"rows": GRID_SIZE, "columns": GRID_SIZE, "tile_size": TILE_SIZE}.items():
        if grid.get(field) != expected:
            issues.append({"scope": "grid", "field": field, "expected": expected, "actual": grid.get(field)})
    if manifest.get("tile_count") != GRID_SIZE**2:
        issues.append({"field": "tile_count", "expected": GRID_SIZE**2, "actual": manifest.get("tile_count")})

    seen_ids: set[str] = set()
    seen_coords: set[tuple[int, int]] = set()
    seen_files: set[str] = set()
    directed_neighbors = 0
    for entry in entries:
        if not isinstance(entry, dict):
            issues.append({"reason": "non_object_tile_entry"})
            continue
        row = entry.get("row")
        column = entry.get("column")
        if not isinstance(row, int) or not isinstance(column, int):
            issues.append({"reason": "invalid_coordinates", "entry": entry})
            continue
        identifier = tile_id(row, column)
        expected_file = f"tiles/{identifier}.png"
        expected_box = {
            "x": column * TILE_SIZE,
            "y": row * TILE_SIZE,
            "width": TILE_SIZE,
            "height": TILE_SIZE,
        }
        for field, expected in {"id": identifier, "file": expected_file, **expected_box}.items():
            if entry.get(field) != expected:
                issues.append({"scope": identifier, "field": field, "expected": expected, "actual": entry.get(field)})
        neighbors = entry.get("neighbors")
        expected_neighbor_set = expected_neighbors(row, column)
        if neighbors != expected_neighbor_set:
            issues.append({"scope": identifier, "field": "neighbors", "expected": expected_neighbor_set, "actual": neighbors})
        if isinstance(neighbors, dict):
            directed_neighbors += sum(value is not None for value in neighbors.values())
        if identifier in seen_ids:
            issues.append({"scope": identifier, "reason": "duplicate_id"})
        if (row, column) in seen_coords:
            issues.append({"scope": identifier, "reason": "duplicate_coordinate"})
        if expected_file in seen_files:
            issues.append({"scope": identifier, "reason": "duplicate_file"})
        seen_ids.add(identifier)
        seen_coords.add((row, column))
        seen_files.add(expected_file)

    expected_coords = {(row, column) for row in range(GRID_SIZE) for column in range(GRID_SIZE)}
    expected_files = {f"tiles/{tile_id(row, column)}.png" for row, column in expected_coords}
    if seen_coords != expected_coords:
        issues.append({"reason": "coordinate_set_mismatch", "missing_count": len(expected_coords - seen_coords)})
    if seen_files != expected_files:
        issues.append({"reason": "file_set_mismatch", "missing_count": len(expected_files - seen_files), "extra_count": len(seen_files - expected_files)})
    if directed_neighbors != 2400:
        issues.append({"reason": "directed_neighbor_count", "expected": 2400, "actual": directed_neighbors})
    return {
        "status": "PASS" if not issues else "FAIL",
        "entry_count": len(entries),
        "unique_id_count": len(seen_ids),
        "unique_coordinate_count": len(seen_coords),
        "directed_neighbor_count": directed_neighbors,
        "undirected_neighbor_count": directed_neighbors // 2,
        "issues": issues,
    }


def native_metrics(image: Image.Image) -> dict:
    gray = np.asarray(image.convert("L"), dtype=np.float32)
    laplacian = (
        4.0 * gray[1:-1, 1:-1]
        - gray[:-2, 1:-1]
        - gray[2:, 1:-1]
        - gray[1:-1, :-2]
        - gray[1:-1, 2:]
    )
    gx = np.abs(np.diff(gray, axis=1)).mean()
    gy = np.abs(np.diff(gray, axis=0)).mean()
    blurred = np.asarray(image.convert("L").filter(ImageFilter.GaussianBlur(radius=1.0)), dtype=np.float32)
    high_frequency_rms = float(np.sqrt(np.mean(np.square(gray - blurred))))
    counts = np.bincount(gray.astype(np.uint8).ravel(), minlength=256).astype(np.float64)
    probabilities = counts[counts > 0] / counts.sum()
    entropy = float(-np.sum(probabilities * np.log2(probabilities)))
    return {
        "laplacian_variance": round(float(laplacian.var()), 6),
        "gradient_mean": round(float(math.hypot(float(gx), float(gy))), 6),
        "high_frequency_rms": round(high_frequency_rms, 6),
        "entropy_bits": round(entropy, 6),
    }


def label_panel(image: Image.Image, label: str, width: int, height: int) -> Image.Image:
    fitted = image.convert("RGB").copy()
    fitted.thumbnail((width, height - 28), Image.Resampling.LANCZOS)
    panel = Image.new("RGB", (width, height), (15, 16, 18))
    panel.paste(fitted, ((width - fitted.width) // 2, 28 + (height - 28 - fitted.height) // 2))
    ImageDraw.Draw(panel).text((8, 8), label, fill=(244, 235, 210), font=ImageFont.load_default())
    return panel


def collect_declared_hashes(value: object, result: dict[str, str]) -> None:
    if isinstance(value, dict):
        file_name = value.get("file")
        digest = value.get("sha256")
        if isinstance(file_name, str) and isinstance(digest, str):
            result[file_name.replace("\\", "/")] = digest.lower()
        for child in value.values():
            collect_declared_hashes(child, result)
    elif isinstance(value, list):
        for child in value:
            collect_declared_hashes(child, result)


def independent_duplicate_analysis(records: list[dict], thumbs: dict[str, np.ndarray]) -> dict:
    identifiers = [record["id"] for record in records]
    rgb = np.stack([thumbs[identifier] for identifier in identifiers]).astype(np.float32)
    small = np.stack([
        np.asarray(Image.fromarray(thumbs[identifier].astype(np.uint8)).resize((16, 16), Image.Resampling.BILINEAR), dtype=np.float32)
        for identifier in identifiers
    ])
    gray = small.mean(axis=3).reshape(len(identifiers), -1)

    def normalize(features: np.ndarray) -> np.ndarray:
        centered = features - features.mean(axis=1, keepdims=True)
        norms = np.linalg.norm(centered, axis=1, keepdims=True)
        return centered / np.maximum(norms, 1e-9)

    normalized = normalize(gray)
    gray_images = small.mean(axis=3)
    horizontal = normalize(np.flip(gray_images, axis=2).reshape(len(identifiers), -1))
    vertical = normalize(np.flip(gray_images, axis=1).reshape(len(identifiers), -1))
    correlation = normalized @ normalized.T
    horizontal_correlation = normalized @ horizontal.T
    vertical_correlation = normalized @ vertical.T
    upper = np.triu_indices(len(identifiers), 1)

    normal_order = np.argsort(correlation[upper])[::-1][:200]
    mirror_score = np.maximum(horizontal_correlation[upper], vertical_correlation[upper])
    mirror_order = np.argsort(mirror_score)[::-1][:200]

    normal_candidates: list[dict] = []
    for offset in normal_order:
        i = int(upper[0][offset])
        j = int(upper[1][offset])
        mad = float(np.abs(rgb[i] - rgb[j]).mean())
        color_distance = float(np.linalg.norm(rgb[i].mean(axis=(0, 1)) - rgb[j].mean(axis=(0, 1))))
        normal_candidates.append({
            "a": identifiers[i],
            "b": identifiers[j],
            "correlation": round(float(correlation[i, j]), 6),
            "thumbnail_mad": round(mad, 6),
            "mean_color_distance": round(color_distance, 6),
        })

    mirror_candidates: list[dict] = []
    for offset in mirror_order:
        i = int(upper[0][offset])
        j = int(upper[1][offset])
        horizontal_mad = float(np.abs(rgb[i] - np.fliplr(rgb[j])).mean())
        vertical_mad = float(np.abs(rgb[i] - np.flipud(rgb[j])).mean())
        mirror_candidates.append({
            "a": identifiers[i],
            "b": identifiers[j],
            "horizontal_correlation": round(float(horizontal_correlation[i, j]), 6),
            "vertical_correlation": round(float(vertical_correlation[i, j]), 6),
            "horizontal_mad": round(horizontal_mad, 6),
            "vertical_mad": round(vertical_mad, 6),
            "best_mad": round(min(horizontal_mad, vertical_mad), 6),
        })

    near_suspects = [
        item for item in normal_candidates
        if item["correlation"] >= 0.995 and item["thumbnail_mad"] < 8.0
    ]
    mirror_suspects = [
        item for item in mirror_candidates
        if max(item["horizontal_correlation"], item["vertical_correlation"]) >= 0.995
        and item["best_mad"] < 8.0
    ]
    return {
        "method": "Independent all-pairs normalized 16x16 luma correlation followed by exact 32x32 RGB MAD on the 200 closest normal and mirrored candidates.",
        "pair_count": len(identifiers) * (len(identifiers) - 1) // 2,
        "near_duplicate_suspect_count": len(near_suspects),
        "mirror_suspect_count": len(mirror_suspects),
        "near_duplicate_suspects": near_suspects,
        "mirror_suspects": mirror_suspects,
        "top_40_closest_pairs": normal_candidates[:40],
        "top_40_closest_mirrors": mirror_candidates[:40],
        "human_review_required": True,
    }


def verify_proofs(master: Image.Image, package: Path, output: Path, i_manifest: dict, g_manifest: dict) -> dict:
    proof_output = output / "independent_proofs"
    proof_output.mkdir(parents=True, exist_ok=True)
    declared_hashes: dict[str, str] = {}
    collect_declared_hashes(i_manifest, declared_hashes)
    collect_declared_hashes(g_manifest, declared_hashes)

    native_records: list[dict] = []
    native_panels: list[Image.Image] = []
    for entry in i_manifest.get("native_512_samples", []):
        identifier = entry["id"]
        row = int(identifier[1:3])
        column = int(identifier[4:6])
        path = package / entry["file"]
        box = (column * TILE_SIZE, row * TILE_SIZE, (column + 1) * TILE_SIZE, (row + 1) * TILE_SIZE)
        crop = master.crop(box)
        with Image.open(path) as file_image:
            file_image.load()
            pixel_identical = ImageChops.difference(crop, file_image.convert("RGB")).getbbox() is None
            metrics = native_metrics(file_image)
            native_panels.append(label_panel(file_image, identifier, 512, 540))
            actual_size = file_image.size
            actual_mode = file_image.mode
        digest = sha256(path)
        native_records.append({
            "id": identifier,
            "file": entry["file"],
            "sha256": digest,
            "declared_hash_match": digest == str(entry.get("sha256", "")).lower(),
            "source_pixel_identical": pixel_identical,
            "dimensions": list(actual_size),
            "mode": actual_mode,
            **metrics,
        })
        crop.close()

    native_contact = Image.new("RGB", (4 * 512, 4 * 540), (0, 0, 0))
    for index, panel in enumerate(native_panels):
        native_contact.paste(panel, ((index % 4) * 512, (index // 4) * 540))
        panel.close()
    native_contact_path = proof_output / "Wave5_16_Native512_IndependentContact.png"
    save_png(native_contact, native_contact_path)
    native_contact.close()

    join_records: list[dict] = []
    join_panels: list[Image.Image] = []
    join_dir = proof_output / "joins"
    for entry in i_manifest.get("adjacent_pairs", []):
        path = package / entry["file"]
        box = (entry["x"], entry["y"], entry["x"] + entry["width"], entry["y"] + entry["height"])
        source = master.crop(box)
        with Image.open(path) as proof:
            proof.load()
            difference = decoded_difference(source, proof, 128)
            dimensions = list(proof.size)
            mode = proof.mode
        independent_path = join_dir / f"{entry['label']}_source_exact.png"
        save_png(source, independent_path)
        digest = sha256(path)
        join_records.append({
            "label": entry["label"],
            "file": entry["file"],
            "source_box": list(box),
            "sha256": digest,
            "declared_hash_match": digest == str(entry.get("sha256", "")).lower(),
            "dimensions": dimensions,
            "mode": mode,
            "source_comparison": difference,
            "independent_source_extract": image_record(independent_path, output),
        })
        join_panels.append(label_panel(source, entry["label"], 640, 348))
        source.close()

    join_contact = Image.new("RGB", (2 * 640, 4 * 348), (0, 0, 0))
    for index, panel in enumerate(join_panels):
        join_contact.paste(panel, ((index % 2) * 640, (index // 2) * 348))
        panel.close()
    join_contact_path = proof_output / "Wave5_8_Joins_IndependentContact.png"
    save_png(join_contact, join_contact_path)
    join_contact.close()

    pan_records: list[dict] = []
    pan_panels: list[Image.Image] = []
    diagonal_entries: list[dict] = []
    for entry in i_manifest.get("pan_strips", []):
        path = package / entry["file"]
        digest = sha256(path)
        with Image.open(path) as proof:
            proof.load()
            dimensions = list(proof.size)
            mode = proof.mode
            pan_panels.append(label_panel(proof, entry["label"], 1200, 108))
            if entry["orientation"] in ("horizontal", "vertical"):
                box = (entry["x"], entry["y"], entry["x"] + entry["width"], entry["y"] + entry["height"])
                source = master.crop(box)
                comparison = decoded_difference(source, proof, 128)
                source.close()
            else:
                comparison = None
                diagonal_entries.append(entry)
        pan_records.append({
            "label": entry["label"],
            "orientation": entry["orientation"],
            "file": entry["file"],
            "sha256": digest,
            "declared_hash_match": digest == str(entry.get("sha256", "")).lower(),
            "dimensions": dimensions,
            "mode": mode,
            "source_comparison": comparison,
        })

    master_array = np.asarray(master, dtype=np.uint8)
    u = np.arange(MASTER_SIZE, dtype=np.float32)[None, :]
    v = (np.arange(TILE_SIZE, dtype=np.float32)[:, None] - (TILE_SIZE - 1) / 2.0) / math.sqrt(2.0)
    diagonal_maps = {
        "D_NW_SE": (
            np.broadcast_to(u + v, (TILE_SIZE, MASTER_SIZE)),
            np.broadcast_to(u - v, (TILE_SIZE, MASTER_SIZE)),
        ),
        "D_NE_SW": (
            np.broadcast_to((MASTER_SIZE - 1.0 - u) + v, (TILE_SIZE, MASTER_SIZE)),
            np.broadcast_to(u + v, (TILE_SIZE, MASTER_SIZE)),
        ),
    }
    for entry in diagonal_entries:
        map_x, map_y = diagonal_maps[entry["label"]]
        regenerated = cv2.remap(
            master_array,
            np.clip(map_x, 0, MASTER_SIZE - 1).astype(np.float32),
            np.clip(map_y, 0, MASTER_SIZE - 1).astype(np.float32),
            interpolation=cv2.INTER_LANCZOS4,
            borderMode=cv2.BORDER_REFLECT_101,
        )
        with Image.open(package / entry["file"]) as proof:
            proof_rgb = np.asarray(proof.convert("RGB"), dtype=np.uint8)
        mismatch = np.any(regenerated != proof_rgb, axis=2)
        delta = np.abs(regenerated.astype(np.int16) - proof_rgb.astype(np.int16))
        comparison = {
            "size_match": True,
            "different_pixel_count": int(np.count_nonzero(mismatch)),
            "different_channel_count": int(np.count_nonzero(delta)),
            "max_channel_delta": int(delta.max(initial=0)),
            "pixel_identical": not bool(np.any(mismatch)),
            "regeneration": "Independent cv2 INTER_LANCZOS4 remap using producer-declared mathematical path.",
        }
        next(item for item in pan_records if item["label"] == entry["label"])["source_comparison"] = comparison
        del regenerated, proof_rgb, mismatch, delta
    del master_array, diagonal_maps, u, v

    pan_contact = Image.new("RGB", (1200, 12 * 108), (0, 0, 0))
    for index, panel in enumerate(pan_panels):
        pan_contact.paste(panel, (0, index * 108))
        panel.close()
    pan_contact_path = proof_output / "Wave5_12_Pans_IndependentContact.png"
    save_png(pan_contact, pan_contact_path)
    pan_contact.close()

    scale_specs = [
        ("63_7pct", "checkpoint_G_proofs/master_view_63_7pct_8154.png", (8154, 8154), "RGB"),
        ("50pct", "checkpoint_G_proofs/master_view_50pct_6400.png", (6400, 6400), "RGB"),
        ("25pct", "checkpoint_G_proofs/master_view_25pct_3200.png", (3200, 3200), "RGB"),
        ("12_5pct", "checkpoint_G_proofs/master_view_12_5pct_1600.png", (1600, 1600), "RGB"),
    ]
    scale_records: list[dict] = []
    scale_panels: list[Image.Image] = []
    for label, relative, dimensions, expected_mode in scale_specs:
        path = package / relative
        regenerated = master.resize(dimensions, Image.Resampling.LANCZOS)
        with Image.open(path) as proof:
            proof.load()
            comparison = decoded_difference(regenerated, proof, 128)
            mode = proof.mode
            actual_dimensions = list(proof.size)
            scale_panels.append(label_panel(proof, label, 800, 800))
        digest = sha256(path)
        scale_records.append({
            "label": label,
            "file": relative,
            "sha256": digest,
            "declared_hash_match": declared_hashes.get(relative) == digest,
            "expected_dimensions": list(dimensions),
            "actual_dimensions": actual_dimensions,
            "expected_mode": expected_mode,
            "actual_mode": mode,
            "independent_resize_comparison": comparison,
        })
        regenerated.close()

    scale_contact = Image.new("RGB", (1600, 1600), (0, 0, 0))
    for index, panel in enumerate(scale_panels):
        scale_contact.paste(panel, ((index % 2) * 800, (index // 2) * 800))
        panel.close()
    scale_contact_path = proof_output / "Wave5_Multiscale_IndependentContact.png"
    save_png(scale_contact, scale_contact_path)
    scale_contact.close()

    preview = master.resize((2048, 2048), Image.Resampling.LANCZOS)
    desaturated = ImageEnhance.Contrast(ImageEnhance.Color(preview).enhance(0.0)).enhance(1.32)
    contrasted = ImageEnhance.Sharpness(ImageEnhance.Contrast(preview).enhance(1.55)).enhance(1.15)
    reveal_specs = [
        ("desaturated", "checkpoint_G_proofs/master_grid_reveal_desaturated_2048.png", desaturated),
        ("high_contrast", "checkpoint_G_proofs/master_grid_reveal_high_contrast_2048.png", contrasted),
    ]
    reveal_records: list[dict] = []
    for label, relative, regenerated in reveal_specs:
        path = package / relative
        with Image.open(path) as proof:
            proof.load()
            comparison = decoded_difference(regenerated, proof, 128)
        digest = sha256(path)
        reveal_records.append({
            "label": label,
            "file": relative,
            "sha256": digest,
            "declared_hash_match": declared_hashes.get(relative) == digest,
            "independent_regeneration_comparison": comparison,
        })
        independent_path = proof_output / f"Wave5_{label}_Independent.png"
        save_png(regenerated, independent_path)
    preview.close()
    desaturated.close()
    contrasted.close()

    center_box = (4864, 4864, 7936, 7936)
    center = master.crop(center_box)
    center_path = proof_output / "Wave5_CentralPrairie_3072_Native.png"
    save_png(center, center_path)
    center_desaturated = ImageEnhance.Contrast(ImageEnhance.Color(center).enhance(0.0)).enhance(1.45)
    center_desaturated_path = proof_output / "Wave5_CentralPrairie_3072_DesaturatedContrast.png"
    save_png(center_desaturated, center_desaturated_path)
    center_overview = center.resize((1536, 1536), Image.Resampling.LANCZOS)
    center_overview_path = proof_output / "Wave5_CentralPrairie_50pct_Overview.png"
    save_png(center_overview, center_overview_path)
    center.close()
    center_desaturated.close()
    center_overview.close()

    native_gate = (
        len(native_records) == 16
        and all(item["declared_hash_match"] and item["source_pixel_identical"] for item in native_records)
        and all(item["dimensions"] == [512, 512] and item["mode"] == "RGB" for item in native_records)
        and all(item["laplacian_variance"] >= 50.0 and item["high_frequency_rms"] >= 3.0 and item["entropy_bits"] >= 5.5 for item in native_records)
    )
    joins_gate = len(join_records) == 8 and all(
        item["declared_hash_match"] and item["source_comparison"].get("pixel_identical") is True
        for item in join_records
    )
    pans_gate = len(pan_records) == 12 and all(
        item["declared_hash_match"] and item["source_comparison"] and item["source_comparison"].get("pixel_identical") is True
        for item in pan_records
    )
    scales_gate = len(scale_records) == 4 and all(
        item["declared_hash_match"] and item["independent_resize_comparison"].get("pixel_identical") is True
        for item in scale_records
    )
    reveals_gate = len(reveal_records) == 2 and all(
        item["declared_hash_match"] and item["independent_regeneration_comparison"].get("pixel_identical") is True
        for item in reveal_records
    )
    return {
        "native_512": {
            "status": "PASS" if native_gate else "FAIL",
            "count": len(native_records),
            "records": native_records,
            "contact": image_record(native_contact_path, output),
        },
        "adjacent_pairs": {
            "status": "PASS" if joins_gate else "FAIL",
            "count": len(join_records),
            "records": join_records,
            "contact": image_record(join_contact_path, output),
        },
        "pan_strips": {
            "status": "PASS" if pans_gate else "FAIL",
            "count": len(pan_records),
            "records": pan_records,
            "contact": image_record(pan_contact_path, output),
        },
        "multi_scale": {
            "status": "PASS" if scales_gate else "FAIL",
            "records": scale_records,
            "contact": image_record(scale_contact_path, output),
        },
        "grid_reveals": {
            "status": "PASS" if reveals_gate else "FAIL",
            "records": reveal_records,
        },
        "central_prairie_review_set": {
            "native": image_record(center_path, output),
            "desaturated_contrast": image_record(center_desaturated_path, output),
            "overview_50pct": image_record(center_overview_path, output),
            "human_review_required": True,
            "watch_item": "Potential swirling or synthetic texture in the central prairie, as raised by UI-A.",
        },
        "status": "PASS" if native_gate and joins_gate and pans_gate and scales_gate and reveals_gate else "FAIL",
    }


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
        raise ValueError("Output must remain outside the read-only producer package.")
    output.mkdir(parents=True, exist_ok=True)

    expected_hash = args.expected_master_sha256.lower()
    master_path = package / "master_25x25_12800.png"
    candidate_path = package / "checkpoint_G_master_candidate_12800.png"
    lock_path = package / "checkpoint_G_master_lock.json"
    manifest_path = package / "manifest.json"
    reconstruction_path = package / "reconstruction_25x25.png"
    i_manifest_path = package / "checkpoint_I_proofs" / "checkpoint_I_proof_manifest.json"
    g_manifest_path = package / "checkpoint_G_proofs" / "checkpoint_G_multiscale_manifest.json"
    tile_dir = package / "tiles"
    required = (master_path, candidate_path, lock_path, manifest_path, reconstruction_path, i_manifest_path, g_manifest_path, tile_dir)
    for path in required:
        if not path.exists():
            raise FileNotFoundError(path)

    started = datetime.now(timezone.utc)
    source_stats_before = {str(path): (path.stat().st_size, path.stat().st_mtime_ns) for path in required if path.is_file()}
    log("[1/9] Hashing master, lock G, manifests and reconstruction")
    master_hash = sha256(master_path)
    candidate_hash = sha256(candidate_path)
    lock = read_json(lock_path)
    manifest = read_json(manifest_path)
    i_manifest = read_json(i_manifest_path)
    g_manifest = read_json(g_manifest_path)
    lock_contract = validate_lock(lock, lock_path, expected_hash)
    manifest_contract = validate_manifest(manifest, expected_hash)

    master = Image.open(master_path)
    master.load()
    master_contract = {
        "file": str(master_path),
        "expected_sha256": expected_hash,
        "actual_sha256": master_hash,
        "hash_match": master_hash == expected_hash,
        "expected_dimensions": [MASTER_SIZE, MASTER_SIZE],
        "actual_dimensions": list(master.size),
        "dimensions_match": master.size == (MASTER_SIZE, MASTER_SIZE),
        "expected_mode": "RGB",
        "actual_mode": master.mode,
        "mode_match": master.mode == "RGB",
        "checkpoint_G_candidate_sha256": candidate_hash,
        "checkpoint_G_candidate_byte_identical": candidate_hash == master_hash,
    }

    log("[2/9] Verifying 625 tiles against manifest and exact master pixels")
    entries = {
        entry["id"]: entry
        for entry in manifest.get("tiles", [])
        if isinstance(entry, dict) and isinstance(entry.get("id"), str)
    }
    tile_paths = sorted(tile_dir.glob("*.png"))
    expected_relative = {f"tiles/{tile_id(row, column)}.png" for row in range(GRID_SIZE) for column in range(GRID_SIZE)}
    actual_relative = {path.relative_to(package).as_posix() for path in tile_paths}
    tile_records: list[dict] = []
    tile_hashes: set[str] = set()
    decoded_hashes: set[str] = set()
    mismatch_records: list[dict] = []
    thumbs: dict[str, np.ndarray] = {}
    for row in range(GRID_SIZE):
        for column in range(GRID_SIZE):
            identifier = tile_id(row, column)
            path = tile_dir / f"{identifier}.png"
            entry = entries.get(identifier, {})
            digest = sha256(path)
            with Image.open(path) as tile:
                tile.load()
                original_size = tile.size
                original_mode = tile.mode
                decoded_hash = hashlib.sha256(tile.convert("RGB").tobytes()).hexdigest()
                box = (column * TILE_SIZE, row * TILE_SIZE, (column + 1) * TILE_SIZE, (row + 1) * TILE_SIZE)
                source = master.crop(box)
                difference_bbox = ImageChops.difference(source, tile.convert("RGB")).getbbox()
                pixel_identical = difference_bbox is None
                if not pixel_identical:
                    delta = np.abs(np.asarray(source, dtype=np.int16) - np.asarray(tile.convert("RGB"), dtype=np.int16))
                    mismatch_records.append({
                        "id": identifier,
                        "different_pixel_count": int(np.count_nonzero(np.any(delta != 0, axis=2))),
                        "max_channel_delta": int(delta.max(initial=0)),
                        "difference_bbox": list(difference_bbox) if difference_bbox else None,
                    })
                thumbs[identifier] = np.asarray(tile.convert("RGB").resize((32, 32), Image.Resampling.BILINEAR), dtype=np.uint8)
                source.close()
            tile_hashes.add(digest)
            decoded_hashes.add(decoded_hash)
            tile_records.append({
                "id": identifier,
                "row": row,
                "column": column,
                "file": path.relative_to(package).as_posix(),
                "sha256": digest,
                "manifest_sha256": entry.get("sha256"),
                "manifest_file_hash_match": digest == str(entry.get("sha256", "")).lower(),
                "decoded_rgb_sha256": decoded_hash,
                "manifest_decoded_rgb_sha256": entry.get("decoded_rgb_sha256"),
                "manifest_decoded_hash_match": decoded_hash == str(entry.get("decoded_rgb_sha256", "")).lower(),
                "dimensions": list(original_size),
                "mode": original_mode,
                "source_pixel_identical": pixel_identical,
            })
        log(f"  tile row {row + 1:02d}/{GRID_SIZE} complete")

    log("[3/9] Comparing packaged reconstruction with decoded master")
    with Image.open(reconstruction_path) as reconstruction:
        reconstruction.load()
        reconstruction_mode = reconstruction.mode
        reconstruction_size = reconstruction.size
        reconstruction_comparison = decoded_difference(master, reconstruction, 128)
    reconstruction_comparison.update({
        "file": str(reconstruction_path),
        "sha256": sha256(reconstruction_path),
        "dimensions": list(reconstruction_size),
        "mode": reconstruction_mode,
        "byte_identical_to_master": sha256(reconstruction_path) == master_hash,
    })

    log("[4/9] Recalculating 1200 boundaries and global 512-pixel phase")
    gray_image = master.convert("L")
    gray = np.asarray(gray_image, dtype=np.uint8)
    boundaries: list[dict] = []
    for row in range(GRID_SIZE):
        for column in range(GRID_SIZE - 1):
            boundaries.append(W4.boundary_metric(gray, row, column, "vertical"))
    for row in range(GRID_SIZE - 1):
        for column in range(GRID_SIZE):
            boundaries.append(W4.boundary_metric(gray, row, column, "horizontal"))
    boundary_failures = [item for item in boundaries if item["status"] != "PASS"]
    ratios = np.asarray([item["gradient_ratio"] for item in boundaries], dtype=np.float64)
    boundary_summary = {
        "expected_count": 1200,
        "actual_count": len(boundaries),
        "pass_count": len(boundaries) - len(boundary_failures),
        "fail_count": len(boundary_failures),
        "mean_gradient_ratio": round(float(ratios.mean()), 6),
        "p95_gradient_ratio": round(float(np.percentile(ratios, 95)), 6),
        "max_gradient_ratio": round(float(ratios.max()), 6),
        "status": "PASS" if len(boundaries) == 1200 and not boundary_failures else "FAIL",
        "failures": boundary_failures,
    }
    grid_phase = W4.global_grid_phase(gray)
    del gray
    gray_image.close()

    log("[5/9] Running independent all-pairs duplicate and mirror analysis")
    duplicate_analysis = independent_duplicate_analysis(tile_records, thumbs)

    log("[6/9] Verifying Checkpoint I native samples, joins, pans and multiscale proofs")
    proof_validation = verify_proofs(master, package, output, i_manifest, g_manifest)

    log("[7/9] Closing mechanical gates and checking source immutability")
    source_stats_after = {str(path): (path.stat().st_size, path.stat().st_mtime_ns) for path in required if path.is_file()}
    source_unchanged = source_stats_before == source_stats_after and sha256(master_path) == master_hash
    master.close()

    package_gate = (
        master_contract["hash_match"]
        and master_contract["dimensions_match"]
        and master_contract["mode_match"]
        and master_contract["checkpoint_G_candidate_byte_identical"]
        and lock_contract["status"] == "PASS"
        and manifest_contract["status"] == "PASS"
        and len(tile_paths) == GRID_SIZE**2
        and actual_relative == expected_relative
        and len(tile_hashes) == GRID_SIZE**2
        and len(decoded_hashes) == GRID_SIZE**2
        and all(item["dimensions"] == [TILE_SIZE, TILE_SIZE] and item["mode"] == "RGB" for item in tile_records)
        and all(item["manifest_file_hash_match"] and item["manifest_decoded_hash_match"] for item in tile_records)
        and source_unchanged
    )
    reconstruction_gate = (
        not mismatch_records
        and reconstruction_comparison.get("pixel_identical") is True
        and reconstruction_comparison.get("different_pixel_count") == 0
        and reconstruction_comparison.get("max_channel_delta") == 0
    )
    boundary_gate = (
        boundary_summary["status"] == "PASS"
        and manifest_contract["undirected_neighbor_count"] == 1200
        and not grid_phase["automation_grid_suspect"]
    )
    duplicate_gate = (
        len(tile_hashes) == GRID_SIZE**2
        and len(decoded_hashes) == GRID_SIZE**2
        and duplicate_analysis["near_duplicate_suspect_count"] == 0
        and duplicate_analysis["mirror_suspect_count"] == 0
    )

    tile_mtimes = [path.stat().st_mtime for path in tile_paths]
    result = {
        "schema": "bee-kingdom.builder-c.world-map-wave5-25x25-independent-prevalidation.v1",
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "validator": "Builder-C Wave5 25x25 read-only independent oracle",
        "package": str(package),
        "output": str(output),
        "source_mutation_performed": False,
        "unity_opened_or_modified": False,
        "server_opened_or_modified": False,
        "master": master_contract,
        "lock_G": lock_contract,
        "manifest_contract": manifest_contract,
        "tile_inventory": {
            "expected_count": 625,
            "actual_count": len(tile_paths),
            "expected_files_match": actual_relative == expected_relative,
            "unique_file_hash_count": len(tile_hashes),
            "unique_decoded_rgb_hash_count": len(decoded_hashes),
            "all_manifest_hashes_match": all(item["manifest_file_hash_match"] and item["manifest_decoded_hash_match"] for item in tile_records),
            "all_512x512_rgb": all(item["dimensions"] == [512, 512] and item["mode"] == "RGB" for item in tile_records),
            "records": tile_records,
        },
        "reconstruction": {
            "tile_to_master_mismatch_count": len(mismatch_records),
            "tile_to_master_mismatches": mismatch_records,
            "logical_independent_reconstruction_pixel_identical": not mismatch_records and actual_relative == expected_relative,
            "packaged_reconstruction_vs_master": reconstruction_comparison,
        },
        "boundaries": {
            "summary": boundary_summary,
            "global_grid_phase": grid_phase,
            "records": boundaries,
        },
        "duplicates_and_mirrors": duplicate_analysis,
        "checkpoint_I_and_multiscale_proofs": proof_validation,
        "provenance": {
            "master_mtime_utc": datetime.fromtimestamp(master_path.stat().st_mtime, timezone.utc).isoformat(),
            "checkpoint_G_candidate_mtime_utc": datetime.fromtimestamp(candidate_path.stat().st_mtime, timezone.utc).isoformat(),
            "earliest_tile_mtime_utc": datetime.fromtimestamp(min(tile_mtimes), timezone.utc).isoformat(),
            "latest_tile_mtime_utc": datetime.fromtimestamp(max(tile_mtimes), timezone.utc).isoformat(),
            "master_precedes_all_tiles": master_path.stat().st_mtime < min(tile_mtimes),
            "source_stats_and_hash_unchanged_during_run": source_unchanged,
        },
        "mechanical_gates": {
            "PACKAGE_GATE": "PASS" if package_gate else "FAIL",
            "RECONSTRUCTION_GATE": "PASS" if reconstruction_gate else "FAIL",
            "BOUNDARY_GATE": "PASS" if boundary_gate else "FAIL",
            "ANTI_DUPLICATE_GATE": "PASS" if duplicate_gate else "REVIEW",
            "PROOF_I_INTEGRITY_GATE": proof_validation["status"],
            "NATIVE_DETAIL_AUTOMATION": proof_validation["native_512"]["status"],
            "GRID_PATTERN_AUTOMATION_SUSPECT": "YES" if grid_phase["automation_grid_suspect"] else "NO",
            "NATIVE_DETAIL_GATE": "PENDING_HUMAN_REVIEW",
            "PERCEPTUAL_GATE": "PENDING_HUMAN_AND_UI_A_REVIEW",
            "P0_RESERVE_COUNT": "PENDING_UI_A",
            "READY_FOR_UNITY_INTEGRATION": "NO_PENDING_UI_A",
        },
        "watch_items": [
            "Inspect the central prairie for swirling, synthetic, flow-field or repeated texture at native and reduced scales.",
            "Numerical scores cannot grant perceptual or native-detail PASS.",
            "Final release remains conditioned on UI-A PASS or PASS_WITH_NOTES with zero P0 reserves.",
        ],
        "honesty": [
            "This is local art prevalidation only.",
            "No Unity integration, runtime rendering, device behavior, server or live-world claim is made.",
        ],
    }

    log("[8/9] Writing independent machine-readable receipts")
    validation_path = output / "Wave5_25x25_IndependentPrevalidation.json"
    validation_path.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    hash_path = output / "Wave5_25x25_TileHashes.sha256"
    hash_path.write_text(
        "".join(f"{item['sha256']}  {item['file']}\n" for item in tile_records),
        encoding="ascii",
    )
    summary = {
        "validation_json": str(validation_path),
        "validation_sha256": sha256(validation_path),
        "duration_seconds": round((datetime.now(timezone.utc) - started).total_seconds(), 3),
        **result["mechanical_gates"],
    }
    summary_path = output / "Wave5_25x25_IndependentPrevalidation_Summary.json"
    summary_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    log("[9/9] Mechanical prevalidation complete")
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0 if package_gate and reconstruction_gate and boundary_gate and duplicate_gate and proof_validation["status"] == "PASS" else 2


if __name__ == "__main__":
    sys.exit(main())
