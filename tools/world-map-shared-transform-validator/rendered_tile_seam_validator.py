#!/usr/bin/env python3
"""Detect rendered WorldMap tile seams without opening or modifying Unity."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import sys
from pathlib import Path
from typing import Any, Iterable

import numpy as np
from PIL import Image


SCHEMA = "bee-kingdom.world-map-rendered-tile-seam-validation.v2"
CHUNK_SIZE = 512.0
RUNTIME_TILE_SIZE = 516
INNER_MIN = 2
INNER_MAX = 514
SIDE_OFFSETS = (3, 4, 5, 6)
SEARCH_RADIUS = 2
MIN_VALID_SAMPLES = 96
MAX_DARK_RATIO = 0.45
MIN_LUMINANCE_DROP = 24.0
MIN_COHERENT_DARK_FRACTION = 0.55
NEAR_BLACK_LUMINANCE = 16.0
BRIGHT_NEIGHBOR_LUMINANCE = 32.0
EDGE_MARGIN_PX = 8
MAX_SHARED_ZOOM_RATIO_ERROR = 0.005
MAX_HUD_TRANSLATION_PX = 1.0
MIN_NONZERO_ANCHOR_DISTANCE_PX = 8.0


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def parse_pair(value: str) -> tuple[float, float]:
    parts = [part.strip() for part in value.split(",")]
    if len(parts) != 2:
        raise ValueError(f"expected pair, got {value!r}")
    return float(parts[0]), float(parts[1])


def parse_size(value: str) -> tuple[int, int]:
    match = re.fullmatch(r"\s*(\d+)\s*[xX]\s*(\d+)\s*", value)
    if not match:
        raise ValueError(f"expected WIDTHxHEIGHT, got {value!r}")
    return int(match.group(1)), int(match.group(2))


def parse_required_size(value: str) -> tuple[tuple[int, int], int]:
    match = re.fullmatch(r"\s*(\d+)\s*[xX]\s*(\d+)\s*:\s*(\d+)\s*", value)
    if not match:
        raise argparse.ArgumentTypeError(f"expected WIDTHxHEIGHT:COUNT, got {value!r}")
    return (int(match.group(1)), int(match.group(2))), int(match.group(3))


def parse_rect(value: str) -> tuple[float, float, float, float]:
    parts = [part.strip() for part in value.split(",")]
    if len(parts) != 4:
        raise ValueError(f"expected rect, got {value!r}")
    return tuple(float(part) for part in parts)  # type: ignore[return-value]


def luminance(image: Image.Image) -> np.ndarray:
    rgb = np.asarray(image.convert("RGB"), dtype=np.float32)
    return rgb @ np.asarray([0.2126, 0.7152, 0.0722], dtype=np.float32)


def add_mask_rect(mask: np.ndarray, rect: tuple[float, float, float, float], padding: int = 4) -> None:
    height, width = mask.shape
    x, y, rect_width, rect_height = rect
    x0 = max(0, int(math.floor(x)) - padding)
    y0 = max(0, int(math.floor(y)) - padding)
    x1 = min(width, int(math.ceil(x + rect_width)) + padding)
    y1 = min(height, int(math.ceil(y + rect_height)) + padding)
    if x1 > x0 and y1 > y0:
        mask[y0:y1, x0:x1] = False


def terrain_mask(width: int, height: int) -> tuple[np.ndarray, list[dict[str, Any]]]:
    """Return terrain-only pixels and the HUD rectangles excluded from analysis."""

    mask = np.ones((height, width), dtype=bool)
    mask[:EDGE_MARGIN_PX, :] = False
    mask[max(0, height - EDGE_MARGIN_PX) :, :] = False
    mask[:, :EDGE_MARGIN_PX] = False
    mask[:, max(0, width - EDGE_MARGIN_PX) :] = False

    portrait = width < 700 or height > width * 1.15
    if portrait:
        rects = [
            ("top_hud", (8.0, 8.0, width - 16.0, 104.0)),
            ("flight_journal", (8.0, 124.0, width - 16.0, 58.0)),
            ("minimap", (width - 128.0, 204.0, 118.0, 86.0)),
            ("action_panel", (8.0, height - 190.0, width - 16.0, 178.0)),
        ]
        profile = "portrait"
    else:
        rects = [
            ("top_hud", (14.0, 12.0, min(820.0, width - 28.0), 108.0)),
            ("stats", (width - 292.0, 12.0, 278.0, 150.0)),
            ("action_panel", (width - 320.0, 176.0, 304.0, 286.0)),
            ("flight_journal", (width - 380.0, 468.0, 364.0, min(144.0, max(132.0, height - 600.0)))),
            ("legend", (14.0, height - 112.0, min(760.0, width - 28.0), 96.0)),
            ("minimap", (width - 214.0, height - 156.0, 198.0, 140.0)),
        ]
        profile = "landscape"

    excluded: list[dict[str, Any]] = []
    for name, rect in rects:
        add_mask_rect(mask, rect)
        excluded.append({"name": name, "rect": [round(value, 3) for value in rect]})
    excluded.append({"name": "screen_edge", "pixels": EDGE_MARGIN_PX})
    excluded.append({"name": "layout_profile", "value": profile})
    return mask, excluded


def metric_is_blocking(metric: dict[str, Any]) -> bool:
    if metric.get("status") == "SKIP":
        return False
    return (
        float(metric["dark_ratio"]) <= MAX_DARK_RATIO
        and float(metric["luminance_drop"]) >= MIN_LUMINANCE_DROP
        and float(metric["coherent_dark_fraction"]) >= MIN_COHERENT_DARK_FRACTION
    )


def measure_line(
    luma: np.ndarray,
    terrain: np.ndarray,
    axis: str,
    coordinate: int,
) -> dict[str, Any]:
    height, width = luma.shape
    limit = width if axis == "vertical" else height
    if coordinate - max(SIDE_OFFSETS) < 0 or coordinate + max(SIDE_OFFSETS) >= limit:
        return {"status": "SKIP", "reason": "SCREEN_EDGE", "measured_screen_px": coordinate}

    if axis == "vertical":
        valid = terrain[:, coordinate].copy()
        for offset in SIDE_OFFSETS:
            valid &= terrain[:, coordinate - offset]
            valid &= terrain[:, coordinate + offset]
        line = luma[:, coordinate][valid]
        sides = np.stack(
            [luma[:, coordinate - offset][valid] for offset in SIDE_OFFSETS]
            + [luma[:, coordinate + offset][valid] for offset in SIDE_OFFSETS],
            axis=0,
        )
    elif axis == "horizontal":
        valid = terrain[coordinate, :].copy()
        for offset in SIDE_OFFSETS:
            valid &= terrain[coordinate - offset, :]
            valid &= terrain[coordinate + offset, :]
        line = luma[coordinate, :][valid]
        sides = np.stack(
            [luma[coordinate - offset, :][valid] for offset in SIDE_OFFSETS]
            + [luma[coordinate + offset, :][valid] for offset in SIDE_OFFSETS],
            axis=0,
        )
    else:
        raise ValueError(f"unsupported axis {axis!r}")

    if line.size < MIN_VALID_SAMPLES:
        return {
            "status": "SKIP",
            "reason": "HUD_OR_INSUFFICIENT_TERRAIN",
            "measured_screen_px": coordinate,
            "valid_samples": int(line.size),
        }

    side_reference = np.median(sides, axis=0)
    line_mean = float(np.mean(line))
    side_mean = float(np.mean(side_reference))
    ratio = line_mean / max(side_mean, 1e-6)
    drop = side_mean - line_mean
    coherent = np.mean(
        (line <= NEAR_BLACK_LUMINANCE)
        & (side_reference >= BRIGHT_NEIGHBOR_LUMINANCE)
        & ((side_reference - line) >= MIN_LUMINANCE_DROP)
    )
    metric = {
        "status": "PASS",
        "measured_screen_px": coordinate,
        "valid_samples": int(line.size),
        "line_luminance_mean": round(line_mean, 4),
        "neighbor_luminance_mean": round(side_mean, 4),
        "dark_ratio": round(ratio, 6),
        "luminance_drop": round(drop, 4),
        "coherent_dark_fraction": round(float(coherent), 6),
    }
    if metric_is_blocking(metric):
        metric["status"] = "FAIL"
    return metric


def measure_expected_boundary(
    luma: np.ndarray,
    terrain: np.ndarray,
    axis: str,
    predicted: float,
) -> dict[str, Any]:
    base = int(round(predicted))
    candidates = [measure_line(luma, terrain, axis, base + delta) for delta in range(-SEARCH_RADIUS, SEARCH_RADIUS + 1)]
    usable = [candidate for candidate in candidates if candidate.get("status") != "SKIP"]
    if not usable:
        return {
            "status": "SKIP",
            "reason": "NO_TERRAIN_CANDIDATE",
            "predicted_screen_px": round(predicted, 4),
        }
    worst = min(usable, key=lambda item: (float(item["dark_ratio"]), -float(item["luminance_drop"])))
    result = dict(worst)
    result["predicted_screen_px"] = round(predicted, 4)
    result["offset_from_prediction_px"] = round(float(result["measured_screen_px"]) - predicted, 4)
    return result


def visible_world_boundaries(
    axis: str,
    center: tuple[float, float],
    zoom: float,
    width: int,
    height: int,
    world_bounds: tuple[float, float, float, float],
) -> list[tuple[float, float]]:
    x, y, bounds_width, bounds_height = world_bounds
    origin = x if axis == "vertical" else y
    extent = bounds_width if axis == "vertical" else bounds_height
    camera = center[0] if axis == "vertical" else center[1]
    viewport_center = width * 0.5 if axis == "vertical" else height * 0.5
    screen_limit = width if axis == "vertical" else height
    results: list[tuple[float, float]] = []
    boundary_count = int(round(extent / CHUNK_SIZE))
    for index in range(1, boundary_count):
        world_coordinate = origin + index * CHUNK_SIZE
        screen_coordinate = viewport_center + (world_coordinate - camera) * zoom
        if EDGE_MARGIN_PX + max(SIDE_OFFSETS) <= screen_coordinate < screen_limit - EDGE_MARGIN_PX - max(SIDE_OFFSETS):
            results.append((world_coordinate, screen_coordinate))
    return results


def collapse_strips(metrics: list[dict[str, Any]]) -> list[dict[str, Any]]:
    if not metrics:
        return []
    metrics.sort(key=lambda item: int(item["measured_screen_px"]))
    groups: list[list[dict[str, Any]]] = [[metrics[0]]]
    for metric in metrics[1:]:
        if int(metric["measured_screen_px"]) <= int(groups[-1][-1]["measured_screen_px"]) + 2:
            groups[-1].append(metric)
        else:
            groups.append([metric])
    return [min(group, key=lambda item: float(item["dark_ratio"])) for group in groups]


def scan_dark_strips(luma: np.ndarray, terrain: np.ndarray, axis: str) -> list[dict[str, Any]]:
    limit = luma.shape[1] if axis == "vertical" else luma.shape[0]
    metrics: list[dict[str, Any]] = []
    for coordinate in range(EDGE_MARGIN_PX + max(SIDE_OFFSETS), limit - EDGE_MARGIN_PX - max(SIDE_OFFSETS)):
        metric = measure_line(luma, terrain, axis, coordinate)
        if metric_is_blocking(metric):
            metrics.append(metric)
    return collapse_strips(metrics)


def compare_arrays(left: np.ndarray, right: np.ndarray) -> int:
    if left.shape != right.shape:
        return int(max(left.size, right.size))
    return int(np.count_nonzero(left != right))


def audit_runtime_tiles(tile_dir: Path) -> dict[str, Any]:
    expected = {f"R{row}C{column}_g2.png" for row in range(5) for column in range(5)}
    actual = {path.name for path in tile_dir.glob("R*C*_g2.png")}
    missing = sorted(expected - actual)
    extra = sorted(actual - expected)
    tiles: dict[tuple[int, int], np.ndarray] = {}
    modes: dict[str, str] = {}
    dimensions_ok = True
    hashes: dict[str, str] = {}
    if not missing and not extra:
        for row in range(5):
            for column in range(5):
                path = tile_dir / f"R{row}C{column}_g2.png"
                with Image.open(path) as image:
                    modes[path.name] = image.mode
                    dimensions_ok &= image.size == (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE)
                    tiles[(row, column)] = np.asarray(image.convert("RGB")).copy()
                hashes[path.name] = sha256_file(path)

    boundaries: list[dict[str, Any]] = []
    gutter_sides_checked = 0
    gutter_mismatch_values = 0
    if len(tiles) == 25 and dimensions_ok:
        for row in range(5):
            for column in range(4):
                west = tiles[(row, column)]
                east = tiles[(row, column + 1)]
                east_gutter_mismatch = compare_arrays(west[2:514, 514:516], east[2:514, 2:4])
                west_gutter_mismatch = compare_arrays(east[2:514, 0:2], west[2:514, 512:514])
                mismatch = east_gutter_mismatch + west_gutter_mismatch
                gutter_sides_checked += 2
                gutter_mismatch_values += mismatch
                boundaries.append(
                    {
                        "id": f"R{row}C{column}_E_R{row}C{column + 1}_W",
                        "axis": "vertical",
                        "gutter_mismatch_values": mismatch,
                        "status": "PASS" if mismatch == 0 else "FAIL",
                    }
                )
        for row in range(4):
            for column in range(5):
                north = tiles[(row, column)]
                south = tiles[(row + 1, column)]
                south_gutter_mismatch = compare_arrays(north[514:516, 2:514], south[2:4, 2:514])
                north_gutter_mismatch = compare_arrays(south[0:2, 2:514], north[512:514, 2:514])
                mismatch = south_gutter_mismatch + north_gutter_mismatch
                gutter_sides_checked += 2
                gutter_mismatch_values += mismatch
                boundaries.append(
                    {
                        "id": f"R{row}C{column}_S_R{row + 1}C{column}_N",
                        "axis": "horizontal",
                        "gutter_mismatch_values": mismatch,
                        "status": "PASS" if mismatch == 0 else "FAIL",
                    }
                )

    outer_clamp_sides_checked = 0
    outer_clamp_mismatch_values = 0
    if len(tiles) == 25 and dimensions_ok:
        for column in range(5):
            top = tiles[(0, column)]
            bottom = tiles[(4, column)]
            outer_clamp_mismatch_values += compare_arrays(top[0:2, 2:514], np.repeat(top[2:3, 2:514], 2, axis=0))
            outer_clamp_mismatch_values += compare_arrays(bottom[514:516, 2:514], np.repeat(bottom[513:514, 2:514], 2, axis=0))
            outer_clamp_sides_checked += 2
        for row in range(5):
            left = tiles[(row, 0)]
            right = tiles[(row, 4)]
            outer_clamp_mismatch_values += compare_arrays(left[2:514, 0:2], np.repeat(left[2:514, 2:3], 2, axis=1))
            outer_clamp_mismatch_values += compare_arrays(right[2:514, 514:516], np.repeat(right[2:514, 513:514], 2, axis=1))
            outer_clamp_sides_checked += 2

    meta_clamp_count = 0
    meta_bilinear_count = 0
    meta_no_mipmap_count = 0
    for name in expected - set(missing):
        meta_path = tile_dir / f"{name}.meta"
        if not meta_path.exists():
            continue
        text = meta_path.read_text(encoding="utf-8")
        if re.search(r"wrapU:\s*1\s+wrapV:\s*1\s+wrapW:\s*1", text):
            meta_clamp_count += 1
        if re.search(r"filterMode:\s*1\b", text):
            meta_bilinear_count += 1
        if re.search(r"enableMipMap:\s*0\b", text):
            meta_no_mipmap_count += 1

    passed = (
        not missing
        and not extra
        and dimensions_ok
        and len(set(hashes.values())) == 25
        and len(boundaries) == 40
        and gutter_sides_checked == 80
        and gutter_mismatch_values == 0
        and outer_clamp_sides_checked == 20
        and outer_clamp_mismatch_values == 0
        and meta_clamp_count == 25
        and meta_bilinear_count == 25
        and meta_no_mipmap_count == 25
    )
    return {
        "status": "PASS" if passed else "FAIL",
        "tile_count": len(actual),
        "missing": missing,
        "extra": extra,
        "dimensions_516x516": dimensions_ok,
        "unique_png_hashes": len(set(hashes.values())),
        "internal_boundaries_checked": len(boundaries),
        "gutter_sides_checked": gutter_sides_checked,
        "gutter_mismatch_values": gutter_mismatch_values,
        "outer_clamp_sides_checked": outer_clamp_sides_checked,
        "outer_clamp_mismatch_values": outer_clamp_mismatch_values,
        "meta_clamp_count": meta_clamp_count,
        "meta_bilinear_count": meta_bilinear_count,
        "meta_no_mipmap_count": meta_no_mipmap_count,
        "boundaries": boundaries,
    }


def extract_method(source: str, method_name: str) -> str:
    match = re.search(rf"\b{re.escape(method_name)}\s*\([^)]*\)\s*\{{", source)
    if not match:
        return ""
    brace = source.find("{", match.start())
    depth = 0
    for index in range(brace, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return source[brace : index + 1]
    return ""


def audit_renderer_source(source_path: Path) -> dict[str, Any]:
    source = source_path.read_text(encoding="utf-8")
    terrain_method = extract_method(source, "DrawWave3WorldTerrain")
    overlay_calls = sorted(set(re.findall(r"\b(DrawLine|DrawFrame|DrawSolid)\s*\(", terrain_method)))
    suspicious_identifiers = sorted(
        set(
            match.group(0)
            for match in re.finditer(
                r"\b(?:seam|gutter|tileBoundary)[A-Za-z0-9_]*(?:mask|cover|hide|strip|overlay)[A-Za-z0-9_]*\b",
                source,
                re.IGNORECASE,
            )
        )
    )
    default_debug_overlay_off = bool(re.search(r"private\s+bool\s+debugChunkOverlay\s*;", source)) and not bool(
        re.search(r"debugChunkOverlay\s*=\s*true", source)
    )
    clamp = "TextureWrapMode.Clamp" in source
    bilinear = "FilterMode.Bilinear" in source
    inner_uv = bool(
        re.search(
            r"Rect\.MinMaxRect\(2f\s*/\s*516f,\s*2f\s*/\s*516f,\s*514f\s*/\s*516f,\s*514f\s*/\s*516f\)",
            source,
        )
    )
    draw_uv_match = re.search(r"GUI\.DrawTextureWithTexCoords\([^;]*tile\.(InnerUv|GutterUv)", terrain_method)
    draw_uv = draw_uv_match.group(1) if draw_uv_match else "UNKNOWN"
    full_gutter_uv = bool(re.search(r"GutterUv\s*=\s*new\s+Rect\(0f,\s*0f,\s*1f,\s*1f\)", source))
    true_gutter_sampling = draw_uv == "GutterUv" and full_gutter_uv
    accepted_uv_contract = (draw_uv == "InnerUv" and inner_uv) or true_gutter_sampling
    terrain_repeat_calls = sorted(
        set(re.findall(r"\b(?:Mathf\.Repeat|TextureWrapMode\.Repeat)\b", terrain_method))
    )
    terrain_modulo_operators = len(re.findall(r"(?<!/)%", terrain_method))
    texture_repeat_absent = "TextureWrapMode.Repeat" not in source
    no_terrain_repeat_or_modulo = not terrain_repeat_calls and terrain_modulo_operators == 0
    no_camouflage = bool(terrain_method) and not overlay_calls and not suspicious_identifiers and default_debug_overlay_off
    passed = (
        clamp
        and bilinear
        and accepted_uv_contract
        and no_camouflage
        and texture_repeat_absent
        and no_terrain_repeat_or_modulo
    )
    return {
        "status": "PASS" if passed else "FAIL",
        "source": str(source_path),
        "sha256": sha256_file(source_path),
        "texture_wrap_clamp": clamp,
        "filter_bilinear": bilinear,
        "inner_uv_2_to_514_over_516": inner_uv,
        "draw_uv": draw_uv,
        "full_true_gutter_uv_declared": full_gutter_uv,
        "true_gutter_sampling": true_gutter_sampling,
        "accepted_uv_contract": accepted_uv_contract,
        "texture_wrap_repeat_absent": texture_repeat_absent,
        "terrain_repeat_calls": terrain_repeat_calls,
        "terrain_modulo_operator_count": terrain_modulo_operators,
        "terrain_repeat_or_modulo_absent": no_terrain_repeat_or_modulo,
        "debug_chunk_overlay_default_off": default_debug_overlay_off,
        "terrain_method_boundary_overlay_calls": overlay_calls,
        "suspicious_camouflage_identifiers": suspicious_identifiers,
        "camouflage_strip_absent": no_camouflage,
    }


def nearest_expected(strip: dict[str, Any], expected: Iterable[dict[str, Any]]) -> tuple[str, float] | None:
    coordinate = float(strip["measured_screen_px"])
    candidates = [
        (str(item["boundary_id"]), abs(coordinate - float(item["predicted_screen_px"])))
        for item in expected
        if item.get("status") != "SKIP"
    ]
    return min(candidates, key=lambda item: item[1]) if candidates else None


def markdown_receipt_value(text: str, label: str) -> str | None:
    match = re.search(rf"{re.escape(label)}\s*:\s*`\s*(.*?)\s*`", text, re.DOTALL | re.IGNORECASE)
    return match.group(1).strip() if match else None


def audit_external_run_receipt(receipt_path: Path | None) -> dict[str, Any]:
    if receipt_path is None:
        return {"status": "NOT_PROVIDED", "path": None}
    if not receipt_path.is_file():
        return {"status": "FAIL", "path": str(receipt_path), "reason": "MISSING_RECEIPT"}
    text = receipt_path.read_text(encoding="utf-8", errors="replace")
    exit_value = markdown_receipt_value(text, "Unity exit code")
    log_value = markdown_receipt_value(text, "Unity log")
    declared_log_hash = (markdown_receipt_value(text, "Unity log SHA-256") or "").upper()
    pid_value = markdown_receipt_value(text, "PID")
    started = markdown_receipt_value(text, "Started UTC")
    completed = markdown_receipt_value(text, "Completed UTC")
    try:
        exit_code = int(exit_value) if exit_value is not None else None
    except ValueError:
        exit_code = None
    log_path = Path(log_value) if log_value else None
    log_exists = bool(log_path and log_path.is_file())
    actual_log_hash = sha256_file(log_path) if log_exists and log_path else None
    hash_matches = bool(declared_log_hash) and declared_log_hash == actual_log_hash
    timeline_present = bool(pid_value and started and completed)
    passed = exit_code == 0 and log_exists and hash_matches and timeline_present
    return {
        "status": "PASS" if passed else "FAIL",
        "path": str(receipt_path),
        "sha256": sha256_file(receipt_path),
        "pid": pid_value,
        "started_utc": started,
        "completed_utc": completed,
        "exit_code": exit_code,
        "log_path": str(log_path) if log_path else None,
        "declared_log_sha256": declared_log_hash or None,
        "actual_log_sha256": actual_log_hash,
        "log_exists": log_exists,
        "log_hash_matches": hash_matches,
        "timeline_present": timeline_present,
    }


def audit_zoom_telemetry(
    telemetry: dict[str, Any],
    external_run_receipt_path: Path | None = None,
) -> dict[str, Any]:
    proof_id = str(telemetry.get("proof_id", ""))
    if "ZOOM" not in proof_id.upper():
        return {"status": "NOT_APPLICABLE", "issue_codes": []}

    issues: list[str] = []
    samples = {str(item.get("label", "")): item for item in telemetry.get("samples", [])}
    expected_sequences = {
        "landscape_zoom_in": ["L13_ZOOM_IN_BEFORE", "L14_ZOOM_IN_MID", "L15_ZOOM_IN_AFTER"],
        "landscape_zoom_out": ["L16_ZOOM_OUT_BEFORE", "L17_ZOOM_OUT_MID", "L18_ZOOM_OUT_AFTER"],
        "portrait_zoom_in": ["P13_ZOOM_IN_BEFORE", "P14_ZOOM_IN_MID", "P15_ZOOM_IN_AFTER"],
        "portrait_zoom_out": ["P16_ZOOM_OUT_BEFORE", "P17_ZOOM_OUT_MID", "P18_ZOOM_OUT_AFTER"],
    }
    expected_labels = {label for labels in expected_sequences.values() for label in labels}
    labels_complete = set(samples) == expected_labels
    if not labels_complete:
        issues.append("ZOOM_TELEMETRY_SEQUENCE_INCOMPLETE")

    sample_metrics: list[dict[str, Any]] = []
    sample_values: dict[str, dict[str, Any]] = {}
    for label in sorted(expected_labels & set(samples)):
        sample = samples[label]
        try:
            zoom = float(sample["zoom"])
            terrain_anchor = parse_pair(str(sample["terrain_anchor"]))
            entity_anchor = parse_pair(str(sample["entity_anchor"]))
            overlay_anchor = parse_pair(str(sample["overlay_anchor"]))
            terrain_distance = float(sample["terrain_distance_to_pivot"])
            entity_distance = float(sample["entity_distance_to_pivot"])
            overlay_distance = float(sample["overlay_distance_to_pivot"])
            hud_rect = parse_rect(str(sample["hud_rect"]))
            hud_ratio = float(sample["hud_ratio"])
            hud_signature = str(sample["hud_anchor_signature"])
        except (KeyError, TypeError, ValueError) as error:
            sample_metrics.append({"label": label, "status": "FAIL", "reason": "MISSING_OR_INVALID_FIELD", "detail": str(error)})
            issues.append("ZOOM_TELEMETRY_SAMPLE_INVALID")
            continue

        terrain_entity_anchor_error = math.dist(terrain_anchor, entity_anchor)
        terrain_entity_distance_error = abs(terrain_distance - entity_distance)
        overlay_nonzero = overlay_distance >= MIN_NONZERO_ANCHOR_DISTANCE_PX
        hud_ratio_ok = 0.995 <= hud_ratio <= 1.005
        values = {
            "zoom": zoom,
            "terrain_distance": terrain_distance,
            "entity_distance": entity_distance,
            "overlay_distance": overlay_distance,
            "hud_rect": hud_rect,
            "hud_ratio": hud_ratio,
            "hud_signature": hud_signature,
        }
        sample_values[label] = values
        status = "PASS"
        if terrain_entity_anchor_error > 0.5 or terrain_entity_distance_error > 0.5:
            status = "FAIL"
            issues.append("TERRAIN_ENTITY_ANCHOR_DIVERGENCE")
        if not overlay_nonzero:
            status = "FAIL"
            issues.append("OVERLAY_ANCHOR_AT_ZOOM_PIVOT")
        if not hud_ratio_ok:
            status = "FAIL"
            issues.append("HUD_SCALE_RATIO_OUT_OF_RANGE")
        sample_metrics.append(
            {
                "label": label,
                "status": status,
                "terrain_entity_anchor_error_px": round(terrain_entity_anchor_error, 6),
                "terrain_entity_distance_error_px": round(terrain_entity_distance_error, 6),
                "overlay_distance_to_pivot_px": round(overlay_distance, 6),
                "overlay_anchor_nonzero": overlay_nonzero,
                "hud_ratio": hud_ratio,
                "hud_ratio_in_0_995_to_1_005": hud_ratio_ok,
                "overlay_anchor": [overlay_anchor[0], overlay_anchor[1]],
            }
        )

    sequence_metrics: list[dict[str, Any]] = []
    max_layer_ratio_error = 0.0
    for sequence_id, labels in expected_sequences.items():
        if any(label not in sample_values for label in labels):
            sequence_metrics.append({"sequence": sequence_id, "status": "FAIL", "reason": "MISSING_SAMPLE"})
            continue
        reference = sample_values[labels[0]]
        observations: list[dict[str, Any]] = []
        sequence_pass = True
        for label in labels:
            values = sample_values[label]
            expected_ratio = values["zoom"] / reference["zoom"]
            layer_ratios: dict[str, float] = {}
            layer_errors: dict[str, float] = {}
            for layer in ("terrain", "entity", "overlay"):
                baseline = float(reference[f"{layer}_distance"])
                distance = float(values[f"{layer}_distance"])
                if baseline < MIN_NONZERO_ANCHOR_DISTANCE_PX:
                    layer_ratios[layer] = 0.0
                    layer_errors[layer] = 1.0
                    sequence_pass = False
                    continue
                actual_ratio = distance / baseline
                relative_error = abs(actual_ratio / expected_ratio - 1.0)
                layer_ratios[layer] = actual_ratio
                layer_errors[layer] = relative_error
                max_layer_ratio_error = max(max_layer_ratio_error, relative_error)
                if relative_error > MAX_SHARED_ZOOM_RATIO_ERROR:
                    sequence_pass = False
            spread = max(layer_ratios.values()) - min(layer_ratios.values())
            if spread > MAX_SHARED_ZOOM_RATIO_ERROR:
                sequence_pass = False
            observations.append(
                {
                    "label": label,
                    "expected_zoom_ratio": round(expected_ratio, 8),
                    "layer_ratios": {key: round(value, 8) for key, value in layer_ratios.items()},
                    "relative_errors": {key: round(value, 8) for key, value in layer_errors.items()},
                    "layer_ratio_spread": round(spread, 8),
                }
            )
        if not sequence_pass:
            issues.append("SHARED_ZOOM_RATIO_MISMATCH")
        sequence_metrics.append(
            {"sequence": sequence_id, "status": "PASS" if sequence_pass else "FAIL", "observations": observations}
        )

    hud_layouts: list[dict[str, Any]] = []
    max_hud_translation = 0.0
    for layout, labels in (("landscape", sorted(label for label in sample_values if label.startswith("L"))), ("portrait", sorted(label for label in sample_values if label.startswith("P")))):
        if len(labels) != 6:
            hud_layouts.append({"layout": layout, "status": "FAIL", "reason": "MISSING_SAMPLE"})
            continue
        reference_rect = sample_values[labels[0]]["hud_rect"]
        signatures = {str(sample_values[label]["hud_signature"]) for label in labels}
        drifts = [max(abs(float(a) - float(b)) for a, b in zip(sample_values[label]["hud_rect"], reference_rect)) for label in labels]
        layout_max_drift = max(drifts)
        max_hud_translation = max(max_hud_translation, layout_max_drift)
        ratios_ok = all(0.995 <= float(sample_values[label]["hud_ratio"]) <= 1.005 for label in labels)
        layout_pass = layout_max_drift <= MAX_HUD_TRANSLATION_PX and len(signatures) == 1 and ratios_ok
        if not layout_pass:
            issues.append("HUD_PIXEL_INVARIANCE_FAIL")
        hud_layouts.append(
            {
                "layout": layout,
                "status": "PASS" if layout_pass else "FAIL",
                "max_rect_coordinate_drift_px": round(layout_max_drift, 6),
                "signature_count": len(signatures),
                "signatures": sorted(signatures),
                "ratios_in_range": ratios_ok,
            }
        )

    negative = telemetry.get("negative_test") or telemetry.get("negative_zoom_fixture")
    negative_unchanged = False
    if isinstance(negative, dict):
        pairs = [
            ("zoom_before", "zoom_after"),
            ("terrain_distance_before", "terrain_distance_after"),
            ("entity_distance_before", "entity_distance_after"),
            ("overlay_distance_before", "overlay_distance_after"),
        ]
        try:
            negative_unchanged = all(abs(float(negative[left]) - float(negative[right])) <= 1e-6 for left, right in pairs)
        except (KeyError, TypeError, ValueError):
            negative_unchanged = False
    negative_pass = (
        isinstance(negative, dict)
        and (bool(negative.get("executed")) or negative_unchanged)
        and negative_unchanged
        and str(negative.get("observed_verdict", "")).upper() == "FAIL"
        and str(negative.get("reason_code", "")).upper() in {"NO_ZOOM_DELTA", "UNCHANGED_ZOOM_STATE"}
    )
    if not negative_pass:
        issues.append("NEGATIVE_UNCHANGED_ZOOM_NOT_EXECUTED_OR_REJECTED")

    external_receipt = audit_external_run_receipt(external_run_receipt_path)
    unity_exit_code = telemetry.get("unity_exit_code")
    unity_exit_pass = (isinstance(unity_exit_code, int) and unity_exit_code == 0) or external_receipt["status"] == "PASS"
    if not unity_exit_pass:
        issues.append("UNITY_EXIT_ZERO_NOT_RECORDED")

    log_value = str(telemetry.get("unity_log", ""))
    log_path = Path(log_value) if log_value else None
    receipt_log_value = str(external_receipt.get("log_path") or "")
    telemetry_log_normalized = str(log_path).replace("\\", "/").lower() if log_path else ""
    receipt_log_normalized = receipt_log_value.replace("\\", "/").lower()
    log_provenance_matches = external_receipt["status"] == "NOT_PROVIDED" or (
        external_receipt["status"] == "PASS"
        and bool(telemetry_log_normalized)
        and telemetry_log_normalized == receipt_log_normalized
    )
    if not log_provenance_matches:
        issues.append("UNITY_LOG_PROVENANCE_MISMATCH")
    log_exists = bool(log_path and log_path.is_file())
    log_hash = sha256_file(log_path) if log_exists and log_path else None
    log_fatal_markers: list[str] = []
    if log_exists and log_path:
        log_text = log_path.read_text(encoding="utf-8", errors="replace")
        for marker in ("NullReferenceException:", "InvalidOperationException:", "WorldMapStep5A_ZoomProofFailure"):
            if marker in log_text:
                log_fatal_markers.append(marker)
    if not log_exists or log_fatal_markers:
        issues.append("UNITY_LOG_MISSING_OR_FATAL")

    producer_flags = {
        "fresh_zoom_source_hash_match": telemetry.get("fresh_zoom_source_hash_match") is True,
        "landscape_zoom_proof": telemetry.get("landscape_zoom_proof") is True,
        "portrait_zoom_proof": telemetry.get("portrait_zoom_proof") is True,
        "terrain_entity_shared_zoom": telemetry.get("terrain_entity_shared_zoom") is True,
        "hud_pixel_invariant": telemetry.get("hud_pixel_invariant") is True,
        "visible_tile_seams_false": telemetry.get("visible_tile_seams") is False,
        "grid_pattern_visible_false": telemetry.get("grid_pattern_visible") is False,
        "ready_for_demo": telemetry.get("ready_for_demo_100_zoom_replacement") is True,
    }
    if not all(producer_flags.values()):
        issues.append("PRODUCER_ZOOM_GATE_NOT_READY")

    unique_issues = sorted(set(issues))
    return {
        "status": "PASS" if not unique_issues else "FAIL",
        "issue_codes": unique_issues,
        "thresholds": {
            "max_shared_zoom_ratio_error": MAX_SHARED_ZOOM_RATIO_ERROR,
            "max_hud_translation_px": MAX_HUD_TRANSLATION_PX,
            "hud_ratio_range": [0.995, 1.005],
            "min_nonzero_anchor_distance_px": MIN_NONZERO_ANCHOR_DISTANCE_PX,
        },
        "labels_complete": labels_complete,
        "samples": sample_metrics,
        "sequences": sequence_metrics,
        "max_layer_zoom_ratio_relative_error": round(max_layer_ratio_error, 8),
        "hud_layouts": hud_layouts,
        "max_hud_rect_coordinate_drift_px": round(max_hud_translation, 6),
        "negative_test_rejected": negative_pass,
        "negative_fixture_unchanged": negative_unchanged,
        "unity_exit_zero_recorded": unity_exit_pass,
        "external_run_receipt": external_receipt,
        "telemetry_log_matches_external_receipt": log_provenance_matches,
        "unity_log": {
            "path": str(log_path) if log_path else None,
            "exists": log_exists,
            "sha256": log_hash,
            "fatal_markers": log_fatal_markers,
        },
        "producer_flags": producer_flags,
    }


def validate(
    telemetry_path: Path,
    capture_dir: Path,
    tile_dir: Path,
    source_path: Path,
    run_id: str,
    verdict_key: str,
    expected_capture_count: int = 3,
    required_sizes: dict[tuple[int, int], int] | None = None,
    external_run_receipt_path: Path | None = None,
) -> dict[str, Any]:
    telemetry = json.loads(telemetry_path.read_text(encoding="utf-8-sig"))
    global_screen_size = telemetry.get("screen_size")
    world_bounds = parse_rect(telemetry["wave3_world_bounds"])
    required_sizes = required_sizes or {}
    image_reports: list[dict[str, Any]] = []
    boundary_groups: dict[str, list[dict[str, Any]]] = {}
    all_expected: list[dict[str, Any]] = []
    all_unexpected: list[dict[str, Any]] = []

    for sample in telemetry.get("samples", []):
        image_name = Path(sample["screenshot"]).name
        image_path = capture_dir / image_name
        if not image_path.exists():
            image_reports.append({"sample": sample.get("label"), "path": str(image_path), "status": "FAIL", "reason": "MISSING_CAPTURE"})
            continue
        try:
            with Image.open(image_path) as image:
                image.verify()
            with Image.open(image_path) as image:
                actual_size = image.size
                luma = luminance(image)
        except (OSError, ValueError) as error:
            image_reports.append(
                {
                    "sample": sample.get("label"),
                    "path": str(image_path),
                    "status": "FAIL",
                    "reason": "UNDECODABLE_CAPTURE",
                    "detail": str(error),
                }
            )
            continue
        terrain, excluded = terrain_mask(*actual_size)
        center = parse_pair(sample["current_center"])
        zoom = float(sample["zoom"])
        declared_screen_size = sample.get("screen_size", global_screen_size)
        declared_dimensions = parse_size(str(declared_screen_size)) if declared_screen_size else None
        actual_png_hash = sha256_file(image_path)
        declared_png_hash = str(sample.get("png_sha256", "")).upper()
        expected: list[dict[str, Any]] = []
        for axis in ("vertical", "horizontal"):
            for world_coordinate, predicted in visible_world_boundaries(
                axis,
                center,
                zoom,
                actual_size[0],
                actual_size[1],
                world_bounds,
            ):
                metric = measure_expected_boundary(luma, terrain, axis, predicted)
                boundary_id = f"{axis[0].upper()}@{world_coordinate:.0f}"
                metric.update(
                    {
                        "axis": axis,
                        "world_coordinate": round(world_coordinate, 4),
                        "boundary_id": boundary_id,
                    }
                )
                expected.append(metric)
                all_expected.append(metric)
                boundary_groups.setdefault(boundary_id, []).append(
                    {
                        "sample": sample["label"],
                        "predicted_screen_px": metric.get("predicted_screen_px"),
                        "measured_screen_px": metric.get("measured_screen_px"),
                        "status": metric["status"],
                        "line_luminance_mean": metric.get("line_luminance_mean"),
                    }
                )

        strips: list[dict[str, Any]] = []
        for axis in ("vertical", "horizontal"):
            axis_expected = [item for item in expected if item["axis"] == axis]
            for strip in scan_dark_strips(luma, terrain, axis):
                nearest = nearest_expected(strip, axis_expected)
                classification = "UNEXPECTED_DARK_STRIP"
                nearest_id = None
                distance = None
                if nearest:
                    nearest_id, distance = nearest
                    if distance <= SEARCH_RADIUS + 1:
                        classification = "EXPECTED_TILE_BOUNDARY"
                strip.update(
                    {
                        "axis": axis,
                        "classification": classification,
                        "nearest_boundary_id": nearest_id,
                        "distance_to_expected_px": round(distance, 4) if distance is not None else None,
                    }
                )
                strips.append(strip)
                if classification == "UNEXPECTED_DARK_STRIP":
                    all_unexpected.append({"sample": sample["label"], **strip})

        blocked = [item for item in expected if item["status"] == "FAIL"]
        image_reports.append(
            {
                "sample": sample["label"],
                "path": str(image_path),
                "sha256": actual_png_hash,
                "declared_png_sha256": declared_png_hash or None,
                "telemetry_png_hash_match": bool(declared_png_hash) and declared_png_hash == actual_png_hash,
                "dimensions": list(actual_size),
                "declared_dimensions": list(declared_dimensions) if declared_dimensions else None,
                "telemetry_dimensions_match": declared_dimensions is not None and actual_size == declared_dimensions,
                "camera_center": [center[0], center[1]],
                "zoom": zoom,
                "excluded_non_terrain_regions": excluded,
                "expected_boundaries": expected,
                "detected_dark_strips": strips,
                "blocking_expected_seams": len(blocked),
                "status": "FAIL" if blocked or any(item["classification"] == "UNEXPECTED_DARK_STRIP" for item in strips) else "PASS",
            }
        )

    tracked_groups: list[dict[str, Any]] = []
    for boundary_id, observations in sorted(boundary_groups.items()):
        if len(observations) < 2:
            continue
        fail_count = sum(item["status"] == "FAIL" for item in observations)
        tracked_groups.append(
            {
                "boundary_id": boundary_id,
                "observations": observations,
                "observed_capture_count": len(observations),
                "blocking_capture_count": fail_count,
                "persistent_blocking": fail_count >= 2,
            }
        )

    tile_audit = audit_runtime_tiles(tile_dir)
    source_audit = audit_renderer_source(source_path)
    zoom_telemetry_audit = audit_zoom_telemetry(telemetry, external_run_receipt_path)
    telemetry_hash_before = str(telemetry.get("bootstrap_hash_before", "")).upper()
    telemetry_hash_after = str(telemetry.get("bootstrap_hash_after", "")).upper()
    source_hash = str(source_audit["sha256"]).upper()
    source_hash_matches_telemetry = bool(telemetry_hash_before) and telemetry_hash_before == telemetry_hash_after == source_hash
    blocking_expected = [item for item in all_expected if item["status"] == "FAIL"]
    background_luminance = 3.5
    background_like = [
        item
        for item in blocking_expected
        if abs(float(item.get("line_luminance_mean", 999.0)) - background_luminance) <= 5.0
    ]
    capture_hashes = [str(item["sha256"]) for item in image_reports if "sha256" in item]
    capture_count_ok = len(image_reports) == expected_capture_count and len(capture_hashes) == expected_capture_count
    capture_hashes_unique = len(set(capture_hashes)) == expected_capture_count
    telemetry_png_hashes_match = capture_count_ok and all(
        bool(item.get("telemetry_png_hash_match")) for item in image_reports
    )
    sample_labels = [str(sample.get("label", "")) for sample in telemetry.get("samples", [])]
    sample_screenshots = [Path(str(sample.get("screenshot", ""))).name for sample in telemetry.get("samples", [])]
    sample_ids_unique = (
        len(sample_labels) == expected_capture_count
        and all(sample_labels)
        and len(set(sample_labels)) == expected_capture_count
        and all(sample_screenshots)
        and len(set(sample_screenshots)) == expected_capture_count
    )
    telemetry_dimensions_match = capture_count_ok and all(
        bool(item.get("telemetry_dimensions_match")) for item in image_reports
    )
    actual_size_counts: dict[tuple[int, int], int] = {}
    for item in image_reports:
        dimensions = item.get("dimensions")
        if dimensions:
            size = (int(dimensions[0]), int(dimensions[1]))
            actual_size_counts[size] = actual_size_counts.get(size, 0) + 1
    required_sizes_match = all(actual_size_counts.get(size, 0) == count for size, count in required_sizes.items())
    source_contract_pass = (
        tile_audit["status"] == "PASS"
        and source_audit["status"] == "PASS"
        and source_hash_matches_telemetry
    )
    passed = (
        capture_count_ok
        and capture_hashes_unique
        and telemetry_png_hashes_match
        and sample_ids_unique
        and telemetry_dimensions_match
        and required_sizes_match
        and not blocking_expected
        and not all_unexpected
        and source_contract_pass
        and zoom_telemetry_audit["status"] != "FAIL"
    )
    issue_codes: list[str] = []
    if not capture_count_ok:
        issue_codes.append("CAPTURE_SET_INCOMPLETE")
    if not capture_hashes_unique:
        issue_codes.append("CAPTURE_HASH_DUPLICATE")
    if not telemetry_png_hashes_match:
        issue_codes.append("CAPTURE_TELEMETRY_PNG_HASH_MISMATCH")
    if not sample_ids_unique:
        issue_codes.append("CAPTURE_SAMPLE_ID_OR_PATH_DUPLICATE")
    if not telemetry_dimensions_match:
        issue_codes.append("CAPTURE_TELEMETRY_DIMENSION_MISMATCH")
    if not required_sizes_match:
        issue_codes.append("REQUIRED_CAPTURE_RESOLUTION_COUNT_MISMATCH")
    if blocking_expected:
        issue_codes.append("RENDERED_TILE_SEAM_DARK")
    if background_like:
        issue_codes.append("RENDERED_BACKGROUND_GAP_EXPOSED")
    if all_unexpected:
        issue_codes.append("UNEXPECTED_DARK_STRIP")
    if tile_audit["status"] != "PASS":
        issue_codes.append("SOURCE_GUTTER_OR_IMPORT_CONTRACT_FAIL")
    if source_audit["status"] != "PASS":
        issue_codes.append("RENDERER_CLAMP_OR_CAMOUFLAGE_CONTRACT_FAIL")
    if not source_hash_matches_telemetry:
        issue_codes.append("SOURCE_HASH_TELEMETRY_MISMATCH")
    issue_codes.extend(str(code) for code in zoom_telemetry_audit.get("issue_codes", []))
    issue_codes = sorted(set(issue_codes))

    return {
        "schema": SCHEMA,
        "run_id": run_id,
        "verdict_key": verdict_key,
        "verdict": "PASS" if passed else "FAIL",
        "status": "PASS" if passed else "FAIL",
        "inputs": {
            "telemetry": str(telemetry_path),
            "telemetry_sha256": sha256_file(telemetry_path),
            "capture_dir": str(capture_dir),
            "tile_dir": str(tile_dir),
            "renderer_source": str(source_path),
            "external_run_receipt": str(external_run_receipt_path) if external_run_receipt_path else None,
        },
        "thresholds": {
            "expected_capture_count": expected_capture_count,
            "required_size_counts": {
                f"{width}x{height}": count for (width, height), count in sorted(required_sizes.items())
            },
            "max_dark_ratio": MAX_DARK_RATIO,
            "min_luminance_drop": MIN_LUMINANCE_DROP,
            "min_coherent_dark_fraction": MIN_COHERENT_DARK_FRACTION,
            "near_black_luminance": NEAR_BLACK_LUMINANCE,
            "search_radius_px": SEARCH_RADIUS,
            "hud_and_screen_edges_excluded": True,
        },
        "checks": {
            "expected_capture_set_present": "PASS" if capture_count_ok else "FAIL",
            "capture_hashes_unique": "PASS" if capture_hashes_unique else "FAIL",
            "telemetry_png_hashes_match_media": "PASS" if telemetry_png_hashes_match else "FAIL",
            "sample_labels_and_paths_unique": "PASS" if sample_ids_unique else "FAIL",
            "telemetry_dimensions_match_media": "PASS" if telemetry_dimensions_match else "FAIL",
            "required_resolution_counts": "PASS" if required_sizes_match else "FAIL",
            "rendered_expected_boundaries_dark_seam_free": "PASS" if not blocking_expected else "FAIL",
            "unexpected_dark_strips_absent": "PASS" if not all_unexpected else "FAIL",
            "cross_frame_world_boundary_tracking": "PASS" if tracked_groups else "FAIL",
            "true_neighbor_gutters_40_of_40": tile_audit["status"],
            "clamp_and_inner_uv_contract": source_audit["status"],
            "camouflage_boundary_strip_absent": "PASS" if source_audit["camouflage_strip_absent"] else "FAIL",
            "renderer_source_hash_matches_capture_telemetry": "PASS" if source_hash_matches_telemetry else "FAIL",
            "terrain_repeat_or_modulo_absent": "PASS" if source_audit["terrain_repeat_or_modulo_absent"] else "FAIL",
            "zoom_transform_hud_negative_and_exit_contract": zoom_telemetry_audit["status"],
        },
        "summary": {
            "captures_checked": len(image_reports),
            "unique_capture_hashes": len(set(capture_hashes)),
            "actual_size_counts": {
                f"{width}x{height}": count for (width, height), count in sorted(actual_size_counts.items())
            },
            "expected_boundaries_checked": len(all_expected),
            "blocking_expected_seams": len(blocking_expected),
            "background_like_blocking_seams": len(background_like),
            "unexpected_dark_strips": len(all_unexpected),
            "cross_frame_boundary_groups": len(tracked_groups),
            "persistent_blocking_groups": sum(bool(group["persistent_blocking"]) for group in tracked_groups),
        },
        "issue_codes": issue_codes,
        "images": image_reports,
        "cross_frame_boundaries": tracked_groups,
        "runtime_tile_contract": tile_audit,
        "renderer_source_contract": source_audit,
        "zoom_telemetry_contract": zoom_telemetry_audit,
        "source_provenance": {
            "telemetry_hash_before": telemetry_hash_before,
            "telemetry_hash_after": telemetry_hash_after,
            "audited_source_hash": source_hash,
            "hash_match": source_hash_matches_telemetry,
        },
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--telemetry", type=Path, required=True)
    parser.add_argument("--capture-dir", type=Path, required=True)
    parser.add_argument("--tile-dir", type=Path, required=True)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--verdict-key", required=True)
    parser.add_argument("--expected-capture-count", type=int, default=3)
    parser.add_argument("--external-run-receipt", type=Path)
    parser.add_argument(
        "--required-size",
        action="append",
        default=[],
        type=parse_required_size,
        metavar="WIDTHxHEIGHT:COUNT",
        help="Require an exact number of captures at this resolution; may be repeated.",
    )
    return parser


def main() -> int:
    args = build_parser().parse_args()
    required_sizes: dict[tuple[int, int], int] = {}
    for size, count in args.required_size:
        if size in required_sizes:
            raise SystemExit(f"duplicate --required-size for {size[0]}x{size[1]}")
        required_sizes[size] = count
    report = validate(
        args.telemetry.resolve(),
        args.capture_dir.resolve(),
        args.tile_dir.resolve(),
        args.source.resolve(),
        args.run_id,
        args.verdict_key,
        args.expected_capture_count,
        required_sizes,
        args.external_run_receipt.resolve() if args.external_run_receipt else None,
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"{args.verdict_key}={report['verdict']}")
    print(f"blocking_expected_seams={report['summary']['blocking_expected_seams']}")
    print(f"output={args.output.resolve()}")
    return 0 if report["verdict"] == "PASS" else 2


if __name__ == "__main__":
    sys.exit(main())
