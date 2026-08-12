#!/usr/bin/env python3
"""Validate one world-to-screen transform for terrain, entities, and HUD."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import sys
from pathlib import Path
from typing import Any


SCHEMA = "bee-kingdom.world-map-shared-transform-evidence.v1"
REPORT_SCHEMA = "bee-kingdom.world-map-shared-transform-validation.v1"
DEFAULT_TOLERANCE_PX = 0.25
WORLD_CHUNKS = 64
CHUNK_SIZE = 512
WORLD_UNITS = WORLD_CHUNKS * CHUNK_SIZE


def clamp(value: float, minimum: float, maximum: float) -> float:
    return max(minimum, min(maximum, value))


def vec_add(a: list[float], b: list[float]) -> list[float]:
    return [a[0] + b[0], a[1] + b[1]]


def vec_sub(a: list[float], b: list[float]) -> list[float]:
    return [a[0] - b[0], a[1] - b[1]]


def vec_mul(a: list[float], scalar: float) -> list[float]:
    return [a[0] * scalar, a[1] * scalar]


def magnitude(a: list[float]) -> float:
    return math.hypot(a[0], a[1])


def distance(a: list[float], b: list[float]) -> float:
    return magnitude(vec_sub(a, b))


def viewport_center(screen_size: list[float]) -> list[float]:
    return [screen_size[0] * 0.5, screen_size[1] * 0.5]


def world_to_screen(
    world: list[float], camera: dict[str, Any], screen_size: list[float]
) -> list[float]:
    """The sole accepted mathematical oracle.

    Screen coordinates use the same positive-Y convention as the audited GUI
    code. A renderer with inverted Y may apply one explicit axis sign, shared by
    every world layer.
    """

    center = [float(value) for value in camera["center"]]
    zoom = float(camera["zoom"])
    if zoom <= 0:
        raise ValueError("camera zoom must be positive")
    return vec_add(viewport_center(screen_size), vec_mul(vec_sub(world, center), zoom))


def screen_to_world(
    screen: list[float], camera: dict[str, Any], screen_size: list[float]
) -> list[float]:
    center = [float(value) for value in camera["center"]]
    zoom = float(camera["zoom"])
    if zoom <= 0:
        raise ValueError("camera zoom must be positive")
    return vec_add(center, vec_mul(vec_sub(screen, viewport_center(screen_size)), 1.0 / zoom))


def camera_after_zoom_around_pivot(
    before: dict[str, Any], next_zoom: float, pivot: list[float], screen_size: list[float]
) -> dict[str, Any]:
    pivot_world = screen_to_world(pivot, before, screen_size)
    offset = vec_mul(vec_sub(pivot, viewport_center(screen_size)), 1.0 / next_zoom)
    return {"center": vec_sub(pivot_world, offset), "zoom": next_zoom}


def legacy_uv_rect(
    screen_size: list[float], zoom: float, world_center: list[float]
) -> list[float]:
    """Exact off-Unity reproduction of ContinuousAtlasUvRect."""

    width, height = screen_size
    aspect = max(0.01, width / max(1.0, height))
    if aspect >= 1.0:
        base_width = 1.0
        base_height = clamp(1.0 / aspect, 0.52, 1.0)
    else:
        base_height = 1.0
        base_width = clamp(aspect, 0.42, 1.0)

    bounded_zoom = clamp(zoom, 0.85, 1.35)
    zoom_01 = clamp((bounded_zoom - 0.85) / (1.35 - 0.85), 0.0, 1.0)
    zoom_scale = 1.0 + (0.74 - 1.0) * zoom_01
    uv_width = clamp(base_width * zoom_scale, 0.38, 1.0)
    uv_height = clamp(base_height * zoom_scale, 0.38, 1.0)

    normalized_x = world_center[0] / WORLD_UNITS - 0.5
    normalized_y = world_center[1] / WORLD_UNITS - 0.5
    uv_center_x = 0.5 + normalized_x * 0.36
    uv_center_y = 0.5 + normalized_y * 0.36
    uv_center_x = clamp(uv_center_x, uv_width * 0.5, 1.0 - uv_width * 0.5)
    uv_center_y = clamp(uv_center_y, uv_height * 0.5, 1.0 - uv_height * 0.5)
    return [
        uv_center_x - uv_width * 0.5,
        uv_center_y - uv_height * 0.5,
        uv_width,
        uv_height,
    ]


def uv_to_screen(uv: list[float], uv_rect: list[float], screen_size: list[float]) -> list[float]:
    return [
        (uv[0] - uv_rect[0]) / uv_rect[2] * screen_size[0],
        (uv[1] - uv_rect[1]) / uv_rect[3] * screen_size[1],
    ]


def uv_aligned_to_screen(screen: list[float], uv_rect: list[float], screen_size: list[float]) -> list[float]:
    return [
        uv_rect[0] + screen[0] / screen_size[0] * uv_rect[2],
        uv_rect[1] + screen[1] / screen_size[1] * uv_rect[3],
    ]


def legacy_metrics() -> dict[str, Any]:
    screen = [1920.0, 1080.0]
    start_center = [(32.5) * CHUNK_SIZE, (32.5) * CHUNK_SIZE]
    pan_end_center = [(35.5) * CHUNK_SIZE, start_center[1]]
    zoom = 1.10
    anchor_world = [start_center[0] + 300.0, start_center[1]]
    start_camera = {"center": start_center, "zoom": zoom}
    pan_camera = {"center": pan_end_center, "zoom": zoom}
    entity_start = world_to_screen(anchor_world, start_camera, screen)
    entity_end = world_to_screen(anchor_world, pan_camera, screen)
    start_rect = legacy_uv_rect(screen, zoom, start_center)
    end_rect = legacy_uv_rect(screen, zoom, pan_end_center)
    anchor_uv = uv_aligned_to_screen(entity_start, start_rect, screen)
    terrain_start = uv_to_screen(anchor_uv, start_rect, screen)
    terrain_end = uv_to_screen(anchor_uv, end_rect, screen)
    entity_pan_delta = vec_sub(entity_end, entity_start)
    terrain_pan_delta = vec_sub(terrain_end, terrain_start)
    expected_pan_magnitude = magnitude(entity_pan_delta)
    pan_ratio = magnitude(terrain_pan_delta) / expected_pan_magnitude

    pivot = [1320.0, 540.0]
    zoom_after = camera_after_zoom_around_pivot(start_camera, 1.35, pivot, screen)
    pivot_world = screen_to_world(pivot, start_camera, screen)
    feature_world = [pivot_world[0] + 300.0, pivot_world[1]]
    zoom_start_rect = legacy_uv_rect(screen, zoom, start_center)
    zoom_end_rect = legacy_uv_rect(screen, 1.35, zoom_after["center"])
    pivot_uv = uv_aligned_to_screen(pivot, zoom_start_rect, screen)
    feature_before = world_to_screen(feature_world, start_camera, screen)
    feature_uv = uv_aligned_to_screen(feature_before, zoom_start_rect, screen)
    terrain_pivot_after = uv_to_screen(pivot_uv, zoom_end_rect, screen)
    terrain_feature_after = uv_to_screen(feature_uv, zoom_end_rect, screen)
    terrain_zoom_factor = distance(terrain_feature_after, terrain_pivot_after) / distance(
        feature_before, pivot
    )
    entity_feature_after = world_to_screen(feature_world, zoom_after, screen)
    entity_zoom_factor = distance(entity_feature_after, pivot) / distance(feature_before, pivot)

    return {
        "scenario": "1920x1080_z1.10_C32_32_to_C35_32_and_zoom_to_1.35",
        "pan_world_delta": [pan_end_center[0] - start_center[0], 0.0],
        "entity_pan_delta_px": entity_pan_delta,
        "terrain_pan_delta_px": terrain_pan_delta,
        "terrain_to_entity_pan_ratio": pan_ratio,
        "terrain_pan_percent_of_expected": pan_ratio * 100.0,
        "zoom_pivot_px": pivot,
        "expected_zoom_factor": 1.35 / 1.10,
        "entity_zoom_factor": entity_zoom_factor,
        "terrain_zoom_factor": terrain_zoom_factor,
        "terrain_pivot_after_px": terrain_pivot_after,
        "terrain_pivot_drift_px": distance(terrain_pivot_after, pivot),
        "legacy_uv_start": start_rect,
        "legacy_uv_pan_end": end_rect,
        "legacy_uv_zoom_end": zoom_end_rect,
    }


def _screen_observation(
    world: list[float],
    before: dict[str, Any],
    after: dict[str, Any],
    screen_size: list[float],
    legacy_terrain: bool,
) -> dict[str, Any]:
    entity_before = world_to_screen(world, before, screen_size)
    entity_after = world_to_screen(world, after, screen_size)
    if legacy_terrain:
        before_rect = legacy_uv_rect(screen_size, float(before["zoom"]), before["center"])
        after_rect = legacy_uv_rect(screen_size, float(after["zoom"]), after["center"])
        anchor_uv = uv_aligned_to_screen(entity_before, before_rect, screen_size)
        terrain_before = uv_to_screen(anchor_uv, before_rect, screen_size)
        terrain_after = uv_to_screen(anchor_uv, after_rect, screen_size)
    else:
        terrain_before = list(entity_before)
        terrain_after = list(entity_after)
    return {
        "world": world,
        "terrain_before": terrain_before,
        "terrain_after": terrain_after,
        "entity_before": entity_before,
        "entity_after": entity_after,
    }


def build_fixture(kind: str) -> dict[str, Any]:
    if kind not in {"current-defect", "positive-shared"}:
        raise ValueError(f"unknown fixture kind: {kind}")
    legacy = kind == "current-defect"
    screen = [1920.0, 1080.0]
    start_center = [(32.5) * CHUNK_SIZE, (32.5) * CHUNK_SIZE]
    before = {"center": start_center, "zoom": 1.10}
    pan_after = {"center": [(35.5) * CHUNK_SIZE, start_center[1]], "zoom": 1.10}
    pivot = [1320.0, 540.0]
    zoom_after = camera_after_zoom_around_pivot(before, 1.35, pivot, screen)
    pivot_world = screen_to_world(pivot, before, screen)
    hud = {
        "top": [14.0, 12.0, 820.0, 108.0],
        "stats": [1628.0, 12.0, 278.0, 150.0],
        "action": [1600.0, 176.0, 304.0, 286.0],
    }

    def hud_snapshot() -> dict[str, list[float]]:
        return {key: list(value) for key, value in hud.items()}

    pan_anchor = _screen_observation(
        [start_center[0] + 300.0, start_center[1]],
        before,
        pan_after,
        screen,
        legacy,
    )
    pan_anchor["id"] = "terrain_feature_pan"
    pivot_anchor = _screen_observation(
        pivot_world, before, zoom_after, screen, legacy
    )
    pivot_anchor["id"] = "zoom_pivot"
    scale_anchor = _screen_observation(
        [pivot_world[0] + 300.0, pivot_world[1]],
        before,
        zoom_after,
        screen,
        legacy,
    )
    scale_anchor["id"] = "zoom_scale_feature"

    return {
        "schema": SCHEMA,
        "id": kind,
        "description": (
            "Exact legacy fullscreen UV defect reproduction"
            if legacy
            else "Expected shared world camera transform"
        ),
        "oracle": "screen = viewport_center + (world - camera_center) * zoom",
        "tolerance_px": DEFAULT_TOLERANCE_PX,
        "policies": {
            "fullscreen_uv_decoupled": legacy,
            "wrap_mode": "Clamp",
            "pilot_repeat": False,
            "pilot_population_mode": (
                "fullscreen_proxy_without_declared_world_bounds"
                if legacy
                else "bounded_declared_world_region"
            ),
            "pilot_world_bounds_declared": not legacy,
            "server_live": False,
        },
        "transitions": [
            {
                "id": "pan_three_chunks",
                "kind": "pan",
                "screen_size": screen,
                "pivot": viewport_center(screen),
                "before_camera": before,
                "after_camera": pan_after,
                "anchors": [pan_anchor],
                "hud_before": hud_snapshot(),
                "hud_after": hud_snapshot(),
            },
            {
                "id": "zoom_around_off_center_pivot",
                "kind": "zoom",
                "screen_size": screen,
                "pivot": pivot,
                "pivot_anchor_id": "zoom_pivot",
                "before_camera": before,
                "after_camera": zoom_after,
                "anchors": [pivot_anchor, scale_anchor],
                "hud_before": hud_snapshot(),
                "hud_after": hud_snapshot(),
            },
        ],
        "claims": {
            "unity_executed": False,
            "physical_device_proof": False,
            "live_world": False,
        },
    }


def add_issue(issues: list[dict[str, Any]], code: str, **details: Any) -> None:
    issues.append({"code": code, **details})


def _rects_equal(a: dict[str, Any], b: dict[str, Any], tolerance: float) -> bool:
    if set(a) != set(b):
        return False
    for key in a:
        av = [float(value) for value in a[key]]
        bv = [float(value) for value in b[key]]
        if len(av) != len(bv) or any(abs(x - y) > tolerance for x, y in zip(av, bv)):
            return False
    return True


def validate_evidence(payload: dict[str, Any]) -> dict[str, Any]:
    issues: list[dict[str, Any]] = []
    metrics: list[dict[str, Any]] = []
    tolerance = float(payload.get("tolerance_px", DEFAULT_TOLERANCE_PX))
    if payload.get("schema") != SCHEMA:
        add_issue(issues, "SCHEMA_MISMATCH", actual=payload.get("schema"), expected=SCHEMA)

    policies = payload.get("policies", {})
    if bool(policies.get("fullscreen_uv_decoupled")):
        add_issue(issues, "FULLSCREEN_UV_DECOUPLED")
    wrap_mode = str(policies.get("wrap_mode", "")).lower()
    if wrap_mode not in {"clamp", "clamptoedge", "clamp_to_edge"}:
        add_issue(issues, "WRAP_MODE_NOT_CLAMP", actual=policies.get("wrap_mode"))
    if bool(policies.get("pilot_repeat")):
        add_issue(issues, "PILOT_REPEATED_AS_LOGICAL_WORLD")
    population_mode = str(policies.get("pilot_population_mode", ""))
    if population_mode in {"repeat", "modulo_repeat", "stretch_to_logical_world"}:
        add_issue(issues, "PILOT_WORLD_POPULATION_POLICY_INVALID", actual=population_mode)
    if not bool(policies.get("pilot_world_bounds_declared")):
        add_issue(issues, "PILOT_WORLD_BOUNDS_UNDECLARED")
    if bool(policies.get("server_live")):
        add_issue(issues, "SERVER_LIVE_CLAIM_FORBIDDEN")

    pan_pass = True
    zoom_pass = True
    hud_pass = True
    for transition in payload.get("transitions", []):
        transition_id = str(transition.get("id", "unnamed"))
        kind = str(transition.get("kind", ""))
        screen_size = [float(value) for value in transition["screen_size"]]
        pivot = [float(value) for value in transition["pivot"]]
        before = transition["before_camera"]
        after = transition["after_camera"]
        if not _rects_equal(
            transition.get("hud_before", {}), transition.get("hud_after", {}), tolerance
        ):
            hud_pass = False
            add_issue(issues, "HUD_TRANSFORM_CHANGED", transition=transition_id)

        pivot_anchor_id = transition.get("pivot_anchor_id")
        for anchor in transition.get("anchors", []):
            anchor_id = str(anchor.get("id", "unnamed"))
            world = [float(value) for value in anchor["world"]]
            expected_before = world_to_screen(world, before, screen_size)
            expected_after = world_to_screen(world, after, screen_size)
            terrain_before = [float(value) for value in anchor["terrain_before"]]
            terrain_after = [float(value) for value in anchor["terrain_after"]]
            entity_before = [float(value) for value in anchor["entity_before"]]
            entity_after = [float(value) for value in anchor["entity_after"]]

            terrain_error_before = distance(terrain_before, expected_before)
            terrain_error_after = distance(terrain_after, expected_after)
            entity_error_before = distance(entity_before, expected_before)
            entity_error_after = distance(entity_after, expected_after)
            terrain_delta = vec_sub(terrain_after, terrain_before)
            entity_delta = vec_sub(entity_after, entity_before)
            expected_delta = vec_sub(expected_after, expected_before)
            shared_delta_error = distance(terrain_delta, entity_delta)

            metrics.append(
                {
                    "transition": transition_id,
                    "anchor": anchor_id,
                    "kind": kind,
                    "terrain_error_before_px": terrain_error_before,
                    "terrain_error_after_px": terrain_error_after,
                    "entity_error_before_px": entity_error_before,
                    "entity_error_after_px": entity_error_after,
                    "terrain_delta_px": terrain_delta,
                    "entity_delta_px": entity_delta,
                    "expected_delta_px": expected_delta,
                    "shared_delta_error_px": shared_delta_error,
                }
            )

            if max(terrain_error_before, terrain_error_after) > tolerance:
                add_issue(
                    issues,
                    "TERRAIN_ORACLE_MISMATCH",
                    transition=transition_id,
                    anchor=anchor_id,
                    before_error_px=terrain_error_before,
                    after_error_px=terrain_error_after,
                )
                pan_pass = pan_pass and kind != "pan"
                zoom_pass = zoom_pass and kind != "zoom"
            if max(entity_error_before, entity_error_after) > tolerance:
                add_issue(
                    issues,
                    "ENTITY_ORACLE_MISMATCH",
                    transition=transition_id,
                    anchor=anchor_id,
                )
                pan_pass = pan_pass and kind != "pan"
                zoom_pass = zoom_pass and kind != "zoom"
            if shared_delta_error > tolerance:
                code = "SHARED_PAN_DELTA_MISMATCH" if kind == "pan" else "SHARED_ZOOM_DELTA_MISMATCH"
                add_issue(
                    issues,
                    code,
                    transition=transition_id,
                    anchor=anchor_id,
                    error_px=shared_delta_error,
                )
                if kind == "pan":
                    pan_pass = False
                if kind == "zoom":
                    zoom_pass = False

            if kind == "pan":
                expected_magnitude = magnitude(expected_delta)
                response_ratio = (
                    magnitude(terrain_delta) / expected_magnitude if expected_magnitude > tolerance else 1.0
                )
                metrics[-1]["terrain_pan_response_ratio"] = response_ratio
                if expected_magnitude > tolerance and response_ratio < 0.95:
                    pan_pass = False
                    add_issue(
                        issues,
                        "TERRAIN_PAN_QUASI_STATIC",
                        transition=transition_id,
                        anchor=anchor_id,
                        response_ratio=response_ratio,
                    )

            if kind == "zoom" and anchor_id == pivot_anchor_id:
                terrain_pivot_drift = distance(terrain_after, pivot)
                entity_pivot_drift = distance(entity_after, pivot)
                metrics[-1]["terrain_pivot_drift_px"] = terrain_pivot_drift
                metrics[-1]["entity_pivot_drift_px"] = entity_pivot_drift
                if max(terrain_pivot_drift, entity_pivot_drift) > tolerance:
                    zoom_pass = False
                    add_issue(
                        issues,
                        "ZOOM_PIVOT_NOT_SHARED",
                        transition=transition_id,
                        terrain_drift_px=terrain_pivot_drift,
                        entity_drift_px=entity_pivot_drift,
                    )

        if kind == "zoom":
            anchors = {str(item.get("id")): item for item in transition.get("anchors", [])}
            pivot_anchor = anchors.get(str(pivot_anchor_id))
            scale_anchor = next(
                (item for key, item in anchors.items() if key != str(pivot_anchor_id)), None
            )
            if pivot_anchor and scale_anchor:
                terrain_before_distance = distance(
                    scale_anchor["terrain_before"], pivot_anchor["terrain_before"]
                )
                terrain_after_distance = distance(
                    scale_anchor["terrain_after"], pivot_anchor["terrain_after"]
                )
                entity_before_distance = distance(
                    scale_anchor["entity_before"], pivot_anchor["entity_before"]
                )
                entity_after_distance = distance(
                    scale_anchor["entity_after"], pivot_anchor["entity_after"]
                )
                expected_factor = float(after["zoom"]) / float(before["zoom"])
                terrain_factor = terrain_after_distance / terrain_before_distance
                entity_factor = entity_after_distance / entity_before_distance
                factor_error = abs(terrain_factor - entity_factor)
                metrics.append(
                    {
                        "transition": transition_id,
                        "kind": "zoom_factor",
                        "expected_factor": expected_factor,
                        "terrain_factor": terrain_factor,
                        "entity_factor": entity_factor,
                        "terrain_entity_factor_error": factor_error,
                    }
                )
                if (
                    abs(terrain_factor - expected_factor) > 0.002
                    or abs(entity_factor - expected_factor) > 0.002
                    or factor_error > 0.002
                ):
                    zoom_pass = False
                    add_issue(
                        issues,
                        "ZOOM_FACTOR_NOT_SHARED",
                        transition=transition_id,
                        expected=expected_factor,
                        terrain=terrain_factor,
                        entity=entity_factor,
                    )

    status = "PASS" if not issues else "FAIL"
    issue_codes = sorted({issue["code"] for issue in issues})
    return {
        "schema": REPORT_SCHEMA,
        "status": status,
        "evidence_id": payload.get("id"),
        "oracle": "screen = viewport_center + (world - camera_center) * zoom",
        "tolerance_px": tolerance,
        "checks": {
            "shared_pan_delta": "PASS" if pan_pass else "FAIL",
            "shared_zoom_factor_and_pivot": "PASS" if zoom_pass else "FAIL",
            "hud_screen_space_invariant": "PASS" if hud_pass else "FAIL",
            "fullscreen_uv_coupled": "PASS" if not policies.get("fullscreen_uv_decoupled") else "FAIL",
            "wrap_mode_clamp": "PASS" if wrap_mode in {"clamp", "clamptoedge", "clamp_to_edge"} else "FAIL",
            "pilot_not_repeated": "PASS" if not policies.get("pilot_repeat") else "FAIL",
            "pilot_world_bounds_declared": "PASS" if policies.get("pilot_world_bounds_declared") else "FAIL",
            "server_live_claim_absent": "PASS" if not policies.get("server_live") else "FAIL",
        },
        "metrics": metrics,
        "issue_codes": issue_codes,
        "issues": issues,
        "verdicts": {
            "SHARED_WORLD_TRANSFORM_EVIDENCE": status,
            "PAN_TERRAIN_ENTITY_DELTA_MATCH": "PASS" if pan_pass else "FAIL",
            "ZOOM_TERRAIN_ENTITY_FACTOR_PIVOT_MATCH": "PASS" if zoom_pass else "FAIL",
            "HUD_TRANSFORM_INVARIANT": "PASS" if hud_pass else "FAIL",
        },
    }


def _method_span(source: str, method_name: str) -> tuple[str, int, int] | None:
    declaration = re.compile(
        r"(?m)^[ \t]*"
        r"(?:(?:public|private|protected|internal|static|virtual|override|sealed|async|extern|new|partial)\s+)*"
        r"[A-Za-z_][\w<>\[\],.?]*\s+"
        + re.escape(method_name)
        + r"\s*\("
    )
    match = declaration.search(source)
    if not match:
        return None
    brace = source.find("{", match.end())
    if brace < 0:
        return None
    depth = 0
    for index in range(brace, len(source)):
        char = source[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                start_line = source.count("\n", 0, match.start()) + 1
                end_line = source.count("\n", 0, index) + 1
                return source[match.start() : index + 1], start_line, end_line
    return None


def _line_of(source: str, token: str) -> int | None:
    index = source.find(token)
    return source.count("\n", 0, index) + 1 if index >= 0 else None


def _declared_method_names(source: str) -> set[str]:
    declaration = re.compile(
        r"(?m)^[ \t]*"
        r"(?:(?:public|private|protected|internal|static|virtual|override|sealed|async|extern|new|partial)\s+)*"
        r"[A-Za-z_][\w<>\[\],.?]*\s+"
        r"(?P<name>[A-Za-z_]\w*)\s*\("
    )
    excluded = {"if", "for", "foreach", "while", "switch", "catch", "using", "lock"}
    return {
        match.group("name")
        for match in declaration.finditer(source)
        if match.group("name") not in excluded
    }


def _reachable_methods(source: str, roots: set[str]) -> set[str]:
    """Build a conservative intra-file call graph from the requested roots."""

    method_names = _declared_method_names(source)
    spans = {name: _method_span(source, name) for name in method_names}
    reachable: set[str] = set()
    pending = [name for name in roots if name in method_names]
    while pending:
        name = pending.pop()
        if name in reachable:
            continue
        reachable.add(name)
        span = spans.get(name)
        if not span:
            continue
        body = span[0]
        for candidate in method_names:
            if candidate in reachable:
                continue
            if re.search(r"\b" + re.escape(candidate) + r"\s*\(", body):
                pending.append(candidate)
    return reachable


def _png_dimensions(path: Path) -> tuple[int, int] | None:
    header = path.read_bytes()[:24]
    if len(header) < 24 or header[:8] != b"\x89PNG\r\n\x1a\n" or header[12:16] != b"IHDR":
        return None
    return int.from_bytes(header[16:20], "big"), int.from_bytes(header[20:24], "big")


def audit_tile_directory(tile_dir: Path) -> dict[str, Any]:
    expected_names = {
        f"R{row}C{column}_g2.png"
        for row in range(5)
        for column in range(5)
    }
    png_paths = sorted(tile_dir.glob("*.png"), key=lambda item: item.name)
    actual_names = {path.name for path in png_paths}
    rows: list[dict[str, Any]] = []
    hashes: list[str] = []
    invalid_dimensions: list[str] = []
    invalid_png: list[str] = []
    for path in png_paths:
        raw = path.read_bytes()
        digest = hashlib.sha256(raw).hexdigest()
        dimensions = _png_dimensions(path)
        if dimensions is None:
            invalid_png.append(path.name)
        elif dimensions != (516, 516):
            invalid_dimensions.append(path.name)
        hashes.append(digest)
        rows.append(
            {
                "file": path.name,
                "sha256": digest,
                "dimensions": list(dimensions) if dimensions else None,
                "bytes": len(raw),
            }
        )

    duplicate_hashes = sorted(
        digest for digest in set(hashes) if hashes.count(digest) > 1
    )
    missing = sorted(expected_names - actual_names)
    extra = sorted(actual_names - expected_names)
    manifest_path = tile_dir / "manifest.runtime.unity.json"
    manifest_tile_count: int | None = None
    manifest_error: str | None = None
    if manifest_path.is_file():
        try:
            manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
            tiles = manifest.get("tiles")
            manifest_tile_count = len(tiles) if isinstance(tiles, list) else None
        except (OSError, json.JSONDecodeError) as error:
            manifest_error = str(error)

    passed = (
        tile_dir.is_dir()
        and len(png_paths) == 25
        and not missing
        and not extra
        and not invalid_png
        and not invalid_dimensions
        and len(set(hashes)) == 25
        and manifest_tile_count == 25
        and manifest_error is None
    )
    return {
        "status": "PASS" if passed else "FAIL",
        "path": str(tile_dir.resolve()),
        "expected_count": 25,
        "actual_count": len(png_paths),
        "unique_id_count": len(actual_names),
        "unique_sha256_count": len(set(hashes)),
        "missing": missing,
        "extra": extra,
        "invalid_png": invalid_png,
        "invalid_dimensions": invalid_dimensions,
        "duplicate_sha256": duplicate_hashes,
        "manifest_path": str(manifest_path.resolve()),
        "manifest_tile_count": manifest_tile_count,
        "manifest_error": manifest_error,
        "tiles": rows,
        "modified": False,
    }


def audit_source(source_path: Path, tile_dir: Path | None = None) -> dict[str, Any]:
    raw = source_path.read_bytes()
    source = raw.decode("utf-8-sig")
    issues: list[dict[str, Any]] = []
    evidence: list[dict[str, Any]] = []

    active_chunks = _method_span(source, "DrawActiveChunks")
    wave3_terrain = _method_span(source, "DrawWave3WorldTerrain")
    world_rect_to_screen = _method_span(source, "WorldRectToScreenRect")
    terrain = _method_span(source, "DrawContinuousAtlasSurface")
    uv_method = _method_span(source, "ContinuousAtlasUvRect")
    world_method = _method_span(source, "WorldToScreen")
    tile_method = _method_span(source, "TileTexCoords")
    hud_method = _method_span(source, "DrawFixedHud")
    action_method = _method_span(source, "DrawActionPanel")
    journal_method = _method_span(source, "DrawFlightJournal")
    reachable_from_terrain_dispatch = _reachable_methods(source, {"DrawActiveChunks"})

    active_chunks_body = active_chunks[0] if active_chunks else ""
    wave3_terrain_body = wave3_terrain[0] if wave3_terrain else ""
    world_rect_body = world_rect_to_screen[0] if world_rect_to_screen else ""
    terrain_body = terrain[0] if terrain else ""
    uv_body = uv_method[0] if uv_method else ""
    tile_body = tile_method[0] if tile_method else ""
    direct_terrain_shared = any(
        token in terrain_body for token in ("WorldToScreen(", "ScreenToWorld(")
    )
    uv_shared = any(token in uv_body for token in ("WorldToScreen(", "ScreenToWorld("))
    primary_wave3_selected = (
        "wave3Provider" in active_chunks_body
        and "DrawWave3WorldTerrain()" in active_chunks_body
    )
    primary_wave3_shared = (
        primary_wave3_selected
        and "WorldRectToScreenRect(" in wave3_terrain_body
        and "tile.WorldRect" in wave3_terrain_body
        and "WorldToScreen(" in world_rect_body
    )
    terrain_shared = direct_terrain_shared or uv_shared or primary_wave3_shared
    entity_shared = all(
        token in source
        for token in (
            "WorldToScreen(hive.WorldCoord)",
            "WorldToScreen(resource.WorldCoord)",
            "WorldToScreen(origin)",
            "WorldToScreen(destination)",
        )
    )
    legacy_fallback_reachable = "DrawContinuousAtlasSurface" in reachable_from_terrain_dispatch
    proxy_fallback_reachable = "DrawFallbackProxyTile" in reachable_from_terrain_dispatch
    modulo_fallback_reachable = "TileTexCoords" in reachable_from_terrain_dispatch
    sector_fallback_reachable = "TextureForChunk(" in active_chunks_body
    solid_fallback_reachable = "DrawSolid(" in active_chunks_body
    fake_map_fallback_reachable = any(
        (
            legacy_fallback_reachable,
            proxy_fallback_reachable,
            modulo_fallback_reachable,
            sector_fallback_reachable,
            solid_fallback_reachable,
        )
    )
    fullscreen_uv = (
        legacy_fallback_reachable
        and
        "DrawTextureWithTexCoords" in terrain_body
        and "new Rect(0f, 0f, Screen.width, Screen.height)" in terrain_body
        and not (direct_terrain_shared or uv_shared)
    )
    projection_split = (
        entity_shared
        and legacy_fallback_reachable
        and "ContinuousAtlasUvRect" in terrain_body
        and not (direct_terrain_shared or uv_shared)
    )
    legacy_formula_active = (
        projection_split
        and "normalizedWorldX" in uv_body
        and "* 0.36f" in uv_body
        and "Mathf.Lerp(1f, 0.74f" in uv_body
    )
    texture_repeat_present = "TextureWrapMode.Repeat" in source
    texture_repeat_reachable = any(
        "TextureWrapMode.Repeat" in (_method_span(source, name) or ("", 0, 0))[0]
        for name in reachable_from_terrain_dispatch
    )
    pilot_modulo_present = "PositiveModulo" in tile_body or re.search(r"%\s*[345]", tile_body) is not None
    pilot_modulo_reachable = modulo_fallback_reachable and pilot_modulo_present
    direct_world_bounds = any(
        token in terrain_body + uv_body
        for token in ("atlasWorldBounds", "AtlasWorldBounds", "worldBounds", "WorldBounds")
    )
    primary_world_bounds = (
        primary_wave3_shared
        and "tile.WorldRect" in wave3_terrain_body
        and "wave3Provider.WorldBounds" in source
    )
    explicit_world_bounds = direct_world_bounds or primary_world_bounds
    hud_bodies = "\n".join(
        item[0] for item in (hud_method, action_method, journal_method) if item is not None
    )
    hud_static_support = "WorldToScreen(" not in hud_bodies and "GUI.matrix" not in hud_bodies
    negative_load_guard = re.search(
        r"if\s*\([^)]*(?:wave3Provider\s*==\s*null|!\s*wave3Provider\.IsLoaded)[^)]*\)\s*\{(?:(?!\n\s*\}).)*?return\s*;",
        active_chunks_body,
        re.DOTALL,
    )
    positive_load_guard = re.search(
        r"if\s*\([^)]*wave3Provider\s*!=\s*null[^)]*wave3Provider\.IsLoaded[^)]*\)\s*\{(?:(?!\n\s*\}).)*?DrawWave3WorldTerrain\s*\(\)(?:(?!\n\s*\}).)*?\n\s*\}",
        active_chunks_body,
        re.DOTALL,
    )
    wave3_failure_fail_closed = bool(
        active_chunks
        and primary_wave3_selected
        and not fake_map_fallback_reachable
        and (
            negative_load_guard
            or (
                positive_load_guard
                and active_chunks_body.count("DrawWave3WorldTerrain()") == 1
            )
        )
    )
    provider_span = _method_span(source, "Load")
    provider_body = provider_span[0] if provider_span and "Wave3RuntimeGutterTileProvider" in source else ""
    provider_25_contract = all(
        token in source
        for token in (
            "private const int Rows = 5",
            "private const int Columns = 5",
            "new List<Wave3RuntimeTile>(25)",
            "IsLoaded = tiles.Count == 25",
        )
    ) and all(
        token in provider_body
        for token in (
            "for (int row = 0; row < Rows; row++)",
            "for (int column = 0; column < Columns; column++)",
            "Resources.Load<Texture2D>",
            "tiles.Clear()",
            "return;",
        )
    )
    tile_inventory = audit_tile_directory(tile_dir) if tile_dir is not None else None

    symbol_rows = [
        ("DrawActiveChunks", active_chunks),
        ("DrawWave3WorldTerrain", wave3_terrain),
        ("WorldRectToScreenRect", world_rect_to_screen),
        ("DrawContinuousAtlasSurface", terrain),
        ("ContinuousAtlasUvRect", uv_method),
        ("WorldToScreen", world_method),
        ("TileTexCoords", tile_method),
        ("DrawFixedHud", hud_method),
    ]
    for symbol, span in symbol_rows:
        if span:
            evidence.append(
                {"symbol": symbol, "start_line": span[1], "end_line": span[2]}
            )

    call_tokens = [
        "GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, Screen.width, Screen.height)",
        "Vector2 p = WorldToScreen(hive.WorldCoord)",
        "Vector2 p = WorldToScreen(resource.WorldCoord)",
        "Vector2 a = WorldToScreen(origin)",
        "float normalizedWorldX = worldCenter.x / (WorldChunkWidth * ChunkSize) - 0.5f",
        "new Vector2(0.5f + normalizedWorldX * 0.36f",
    ]
    for token in call_tokens:
        line = _line_of(source, token)
        if line is not None:
            evidence.append({"token": token, "line": line})

    metrics = legacy_metrics()
    if projection_split:
        add_issue(issues, "TERRAIN_ENTITY_PROJECTION_SPLIT")
    if fullscreen_uv:
        add_issue(issues, "FULLSCREEN_UV_DECOUPLED")
        if primary_wave3_selected:
            add_issue(issues, "DECOUPLED_TERRAIN_FALLBACK_REACHABLE")
    if legacy_formula_active and metrics["terrain_to_entity_pan_ratio"] < 0.95:
        add_issue(
            issues,
            "TERRAIN_PAN_QUASI_STATIC",
            response_ratio=metrics["terrain_to_entity_pan_ratio"],
        )
    if legacy_formula_active and abs(metrics["terrain_pan_delta_px"][0] - metrics["entity_pan_delta_px"][0]) > 0.25:
        add_issue(issues, "TERRAIN_PAN_SCALE_MISMATCH")
    if legacy_formula_active and abs(metrics["terrain_zoom_factor"] - metrics["entity_zoom_factor"]) > 0.002:
        add_issue(issues, "TERRAIN_ZOOM_FACTOR_MISMATCH")
    if legacy_formula_active and metrics["terrain_pivot_drift_px"] > 0.25:
        add_issue(issues, "TERRAIN_ZOOM_PIVOT_DRIFT")
    if not primary_wave3_shared:
        add_issue(issues, "CANONICAL_WAVE3_SHARED_PATH_MISSING")
    if fake_map_fallback_reachable:
        add_issue(issues, "WAVE3_LOAD_FAILURE_FAKE_MAP_FALLBACK_REACHABLE")
    if not wave3_failure_fail_closed:
        add_issue(issues, "WAVE3_LOAD_FAILURE_NOT_FAIL_CLOSED")
    if texture_repeat_reachable:
        add_issue(issues, "TEXTURE_REPEAT_ENABLED")
    if pilot_modulo_reachable:
        add_issue(issues, "PILOT_MODULO_REPEAT_PATH")
    if fullscreen_uv and not direct_world_bounds:
        add_issue(issues, "LEGACY_ATLAS_WORLD_BOUNDS_NOT_EXPLICIT")
    elif not explicit_world_bounds:
        add_issue(issues, "ATLAS_WORLD_BOUNDS_NOT_EXPLICIT")
    if not hud_static_support:
        add_issue(issues, "HUD_STATIC_SOURCE_SUPPORT_MISSING")
    if not provider_25_contract:
        add_issue(issues, "WAVE3_PROVIDER_25_TILE_CONTRACT_MISSING")
    if tile_inventory is not None and tile_inventory["status"] != "PASS":
        add_issue(issues, "WAVE3_TILE_INVENTORY_INVALID")

    issue_codes = sorted({item["code"] for item in issues})
    defect_reproduced = all(
        code in issue_codes
        for code in (
            "TERRAIN_ENTITY_PROJECTION_SPLIT",
            "FULLSCREEN_UV_DECOUPLED",
            "TERRAIN_PAN_QUASI_STATIC",
            "TERRAIN_ZOOM_FACTOR_MISMATCH",
        )
    )
    source_gate = "PASS" if not issues else "FAIL"
    return {
        "schema": "bee-kingdom.world-map-shared-transform-source-audit.v1",
        "status": source_gate,
        "source": {
            "path": str(source_path.resolve()),
            "sha256": hashlib.sha256(raw).hexdigest(),
            "line_count": len(source.splitlines()),
            "modified": False,
        },
        "oracle": "screen = viewport_center + (world - camera_center) * zoom",
        "source_checks": {
            "terrain_calls_shared_projection": terrain_shared,
            "primary_wave3_path_selected_first": primary_wave3_selected,
            "primary_wave3_shared_projection": primary_wave3_shared,
            "primary_wave3_world_bounds_explicit": primary_world_bounds,
            "legacy_fullscreen_uv_fallback_reachable": fullscreen_uv,
            "proxy_tile_fallback_reachable": proxy_fallback_reachable,
            "sector_tile_fallback_reachable": sector_fallback_reachable,
            "solid_checker_fallback_reachable": solid_fallback_reachable,
            "fake_map_fallback_reachable": fake_map_fallback_reachable,
            "wave3_load_failure_fail_closed": wave3_failure_fail_closed,
            "entities_call_world_to_screen": entity_shared,
            "fullscreen_uv_decoupled": fullscreen_uv,
            "separate_terrain_entity_projection": projection_split,
            "texture_repeat_present_anywhere": texture_repeat_present,
            "texture_repeat_reachable": texture_repeat_reachable,
            "pilot_modulo_repeat_path_present_anywhere": bool(pilot_modulo_present),
            "pilot_modulo_repeat_path_reachable": bool(pilot_modulo_reachable),
            "atlas_world_bounds_explicit": explicit_world_bounds,
            "hud_screen_space_static_support": hud_static_support,
            "provider_25_tile_contract": provider_25_contract,
        },
        "reachable_from_DrawActiveChunks": sorted(reachable_from_terrain_dispatch),
        "tile_inventory": tile_inventory,
        "legacy_metrics": metrics,
        "evidence": evidence,
        "issue_codes": issue_codes,
        "issues": issues,
        "verdicts": {
            "CURRENT_STATIC_BACKGROUND_DEFECT_REPRODUCED": "YES" if defect_reproduced else "NO",
            "SOURCE_SHARED_TRANSFORM_GATE": source_gate,
            "PRIMARY_WAVE3_SHARED_PATH_STATIC_SUPPORT": "PASS" if primary_wave3_shared else "NOT_PRESENT",
            "HUD_STATIC_SOURCE_SUPPORT": "PASS" if hud_static_support else "FAIL",
            "FULLSCREEN_UV_FALLBACK_REACHABILITY": "FAIL" if fullscreen_uv else "PASS",
            "MODULO_REPEAT_FALLBACK_REACHABILITY": "FAIL" if pilot_modulo_reachable else "PASS",
            "WAVE3_LOAD_FAILURE_FAIL_CLOSED": "PASS" if wave3_failure_fail_closed else "FAIL",
            "WAVE3_25_UNIQUE_TILES": (
                tile_inventory["status"] if tile_inventory is not None else "NOT_AUDITED"
            ),
            "REPEAT_OR_PILOT_REPLICATION_PATH": "PRESENT" if texture_repeat_reachable or pilot_modulo_reachable else "UNREACHABLE_OR_ABSENT",
            "FINAL_STEP5A_SOURCE_GATE": source_gate,
        },
        "claims": {
            "unity_executed": False,
            "source_modified": False,
            "server_live": False,
        },
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    fixture = subparsers.add_parser("generate-fixture", help="Generate a deterministic evidence fixture")
    fixture.add_argument("--kind", choices=("current-defect", "positive-shared"), required=True)
    fixture.add_argument("--output", type=Path, required=True)

    validate = subparsers.add_parser("validate-evidence", help="Validate exported transform evidence")
    validate.add_argument("--input", type=Path, required=True)
    validate.add_argument("--output", type=Path, required=True)

    source = subparsers.add_parser("audit-source", help="Audit the current C# transform paths read-only")
    source.add_argument("--source", type=Path, required=True)
    source.add_argument(
        "--tile-dir",
        type=Path,
        help="Optional read-only Wave3 runtime tile directory (25 R0C0..R4C4 PNG files)",
    )
    source.add_argument("--output", type=Path, required=True)
    return parser


def main() -> int:
    args = build_parser().parse_args()
    if args.command == "generate-fixture":
        payload = build_fixture(args.kind)
        write_json(args.output, payload)
        print(json.dumps(payload, indent=2))
        return 0
    if args.command == "validate-evidence":
        payload = json.loads(args.input.read_text(encoding="utf-8"))
        report = validate_evidence(payload)
        write_json(args.output, report)
        print(json.dumps(report, indent=2))
        return 0 if report["status"] == "PASS" else 2
    if args.command == "audit-source":
        report = audit_source(args.source, args.tile_dir)
        write_json(args.output, report)
        print(json.dumps(report, indent=2))
        return 0 if report["status"] == "PASS" else 2
    raise AssertionError(args.command)


if __name__ == "__main__":
    raise SystemExit(main())
