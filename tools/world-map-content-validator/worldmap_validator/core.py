from __future__ import annotations

import hashlib
import json
import math
import re
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageOps


SCHEMA = "bee-kingdom.world-map-content-validation.v2"
TOOL_VERSION = "3.0.0"
STATUS_RANK = {"PASS": 0, "WARN": 1, "FAIL": 2}
CARDINALS = {
    "n": (0, -1),
    "e": (1, 0),
    "s": (0, 1),
    "w": (-1, 0),
}
FORBIDDEN_CONTENT_CATEGORIES = {
    "ground_routes": "route ou piste dominante peinte",
    "player_hives": "ruches ou structures joueur peintes",
    "runtime_resources": "ressources collectables ou marqueurs runtime peints",
    "troops": "troupes, essaims ou unites peintes",
    "painted_flight_paths": "trajectoires, arcs ou lignes de vol peints",
    "ui_or_text": "UI, texte, badges ou marqueurs peints",
    "painted_tile_boundaries": "frontieres, grille ou coutures artificielles peintes",
}
PERCEPTUAL_CONTINUITY_CATEGORIES = {
    "grid_lines_visible": "lignes horizontales ou verticales de grille visibles",
    "central_square_visible": "carre central reconnaissable",
    "outer_ring_visible": "anneau peripherique reconnaissable",
    "checkerboard_visible": "damier ou alternance de cellules reconnaissable",
    "blurred_bands_visible": "bandes floues ou lissees aux futures limites",
    "mirrored_motifs_visible": "motifs miroirs reconnaissables",
    "repeated_tile_motifs": "motifs repetes ou copies entre tuiles",
    "river_discontinuity": "ruptures de rivieres ou d'eau aux limites",
    "relief_discontinuity": "ruptures de relief ou de cretes aux limites",
    "forest_discontinuity": "ruptures de forets ou de vegetation aux limites",
    "biome_boundary_rectilinear": "frontieres de biome rectilignes liees aux fichiers",
}


@dataclass(frozen=True)
class ValidationOptions:
    input_dir: Path
    output_dir: Path
    manifest_path: Path | None = None
    thresholds_path: Path | None = None
    expected_count: int | None = None
    columns: int | None = None
    rows: int | None = None
    label: str = "lot-sans-label"
    profile: str | None = None
    reference_atlas_path: Path | None = None
    baseline_center_dir: Path | None = None
    baseline_manifest_path: Path | None = None
    expected_new_ring_count: int | None = None
    expected_seam_count: int | None = None
    required_tile_width: int | None = None
    required_tile_height: int | None = None
    forbidden_review_path: Path | None = None
    require_forbidden_review: bool = False
    perceptual_review_path: Path | None = None
    require_perceptual_review: bool = False
    require_signed_perceptual_review: bool = False
    gutters_dir: Path | None = None
    gutter_size: int = 2
    require_gutters: bool = False
    required_master_width: int | None = None
    required_master_height: int | None = None
    readiness_report_path: Path | None = None
    require_wave3_ready_marker: bool = False


@dataclass
class TileSpec:
    tile_id: str
    file_path: Path
    relative_file: str
    x: int | None = None
    y: int | None = None
    expected_sha256: str | None = None
    expected_width: int | None = None
    expected_height: int | None = None
    declared_neighbors: dict[str, str | None] | None = None
    pixel_x: int | None = None
    pixel_y: int | None = None
    manifest_entry: dict[str, Any] = field(default_factory=dict)


@dataclass
class TileRuntime:
    spec: TileSpec
    image: Image.Image | None
    analysis: dict[str, Any]


@dataclass(frozen=True)
class GridInfo:
    columns: int
    rows: int
    expected_count: int
    coordinate_origin_x: int
    coordinate_origin_y: int


def _read_json(path: Path) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(data, dict):
        raise ValueError(f"Le JSON doit contenir un objet racine: {path}")
    return data


def _default_thresholds_path() -> Path:
    return Path(__file__).resolve().parents[1] / "thresholds.default.json"


def _load_thresholds(override_path: Path | None) -> dict[str, Any]:
    thresholds = _read_json(_default_thresholds_path())
    if override_path:
        override = _read_json(override_path.resolve())
        override.pop("schema", None)
        thresholds.update(override)
    return thresholds


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _max_status(statuses: Iterable[str]) -> str:
    return max(statuses, key=lambda value: STATUS_RANK[value], default="PASS")


def _check(check_id: str, status: str, message: str, **details: Any) -> dict[str, Any]:
    row: dict[str, Any] = {"id": check_id, "status": status, "message": message}
    if details:
        row["details"] = details
    return row


def _find_manifest(input_dir: Path, explicit: Path | None) -> Path | None:
    if explicit:
        candidate = explicit.resolve()
        if not candidate.is_file():
            raise FileNotFoundError(f"Manifest introuvable: {candidate}")
        return candidate
    candidate = input_dir / "manifest.json"
    return candidate if candidate.is_file() else None


def _manifest_tiles(manifest: dict[str, Any]) -> list[dict[str, Any]]:
    for key in ("tiles", "sectors", "images"):
        value = manifest.get(key)
        if isinstance(value, list):
            return [entry for entry in value if isinstance(entry, dict)]
    tiling = manifest.get("tiling")
    if isinstance(tiling, dict) and isinstance(tiling.get("tiles"), list):
        return [entry for entry in tiling["tiles"] if isinstance(entry, dict)]
    return []


def _nested_int(entry: dict[str, Any], *paths: tuple[str, ...]) -> int | None:
    for path in paths:
        value: Any = entry
        for part in path:
            if not isinstance(value, dict) or part not in value:
                value = None
                break
            value = value[part]
        if isinstance(value, int) and not isinstance(value, bool):
            return value
    return None


def _first_text(entry: dict[str, Any], keys: Iterable[str]) -> str | None:
    for key in keys:
        value = entry.get(key)
        if isinstance(value, str) and value.strip():
            return value.strip()
    return None


def _declared_hash(entry: dict[str, Any]) -> str | None:
    direct = _first_text(entry, ("sha256", "hash"))
    if direct:
        return direct
    nested = entry.get("hash")
    if isinstance(nested, dict):
        algorithm = str(nested.get("algorithm", "SHA-256")).upper().replace("_", "-")
        value = nested.get("value")
        if algorithm in {"SHA-256", "SHA256"} and isinstance(value, str) and value.strip():
            return value.strip()
    return None


_COORD_PATTERNS = (
    re.compile(r"(?:^|[_-])x(?P<x>-?\d+)[_-]?y(?P<y>-?\d+)(?:$|[_-])", re.IGNORECASE),
    re.compile(r"(?:tile|sector)[_-]?x?(?P<x>-?\d+)[_-]y?(?P<y>-?\d+)", re.IGNORECASE),
)

_COMPASS_COORDINATES = {
    "NW": (0, 0),
    "N": (1, 0),
    "NE": (2, 0),
    "W": (0, 1),
    "C": (1, 1),
    "E": (2, 1),
    "SW": (0, 2),
    "S": (1, 2),
    "SE": (2, 2),
}


def _infer_coordinates(stem: str) -> tuple[int, int] | None:
    compass_match = re.fullmatch(
        r"sector[_-](NW|N|NE|W|C|E|SW|S|SE)", stem, re.IGNORECASE
    )
    if compass_match:
        return _COMPASS_COORDINATES[compass_match.group(1).upper()]
    for pattern in _COORD_PATTERNS:
        match = pattern.search(stem)
        if match:
            return int(match.group("x")), int(match.group("y"))
    return None


def _resolve_input_file(input_dir: Path, declared: str) -> Path:
    candidate = Path(declared)
    if not candidate.is_absolute():
        candidate = input_dir / candidate
    return candidate.resolve()


def _build_tile_specs(
    input_dir: Path,
    manifest: dict[str, Any] | None,
    columns_hint: int | None,
) -> tuple[list[TileSpec], list[str]]:
    notes: list[str] = []
    specs: list[TileSpec] = []
    entries = _manifest_tiles(manifest or {})

    if entries:
        for index, entry in enumerate(entries):
            declared_file = _first_text(entry, ("file", "path", "filename", "image"))
            if declared_file is None:
                declared_file = f"__manifest_entry_without_file_{index}.png"
            file_path = _resolve_input_file(input_dir, declared_file)
            tile_id = _first_text(entry, ("id", "tile_id", "sector_id", "name")) or file_path.stem
            x = _nested_int(
                entry,
                ("tile_x",),
                ("grid_x",),
                ("column",),
                ("coord", "x"),
                ("tile_coord", "x"),
            )
            y = _nested_int(
                entry,
                ("tile_y",),
                ("grid_y",),
                ("row",),
                ("coord", "y"),
                ("tile_coord", "y"),
            )
            inferred = _infer_coordinates(file_path.stem)
            if inferred:
                x = inferred[0] if x is None else x
                y = inferred[1] if y is None else y
            expected_width = _nested_int(entry, ("stored_dimensions", "width"), ("width",))
            expected_height = _nested_int(entry, ("stored_dimensions", "height"), ("height",))
            rect = entry.get("source_rect") or entry.get("pixel_rect") or entry.get("world_rect")
            pixel_x = rect.get("x") if isinstance(rect, dict) and isinstance(rect.get("x"), int) else None
            pixel_y = rect.get("y") if isinstance(rect, dict) and isinstance(rect.get("y"), int) else None
            neighbors = entry.get("neighbors")
            normalized_neighbors = (
                {str(key).lower(): value for key, value in neighbors.items()}
                if isinstance(neighbors, dict)
                else None
            )
            specs.append(
                TileSpec(
                    tile_id=tile_id,
                    file_path=file_path,
                    relative_file=declared_file.replace("\\", "/"),
                    x=x,
                    y=y,
                    expected_sha256=_declared_hash(entry),
                    expected_width=expected_width,
                    expected_height=expected_height,
                    declared_neighbors=normalized_neighbors,
                    pixel_x=pixel_x,
                    pixel_y=pixel_y,
                    manifest_entry=entry,
                )
            )
    else:
        all_pngs = sorted(
            path
            for path in input_dir.rglob("*.png")
            if not any(part.lower() in {"validation", "validator-output"} for part in path.parts)
        )
        sector_pngs = [path for path in all_pngs if re.match(r"^sector[_-]", path.stem, re.IGNORECASE)]
        pngs = sector_pngs or all_pngs
        if sector_pngs:
            notes.append(
                f"Convention sector_* detectee: {len(sector_pngs)} secteurs retenus, "
                f"{len(all_pngs) - len(sector_pngs)} PNG de support inventories separement."
            )
        for path in pngs:
            coords = _infer_coordinates(path.stem)
            specs.append(
                TileSpec(
                    tile_id=path.stem,
                    file_path=path.resolve(),
                    relative_file=path.relative_to(input_dir).as_posix(),
                    x=coords[0] if coords else None,
                    y=coords[1] if coords else None,
                )
            )
        notes.append("Aucun manifest.json: inventaire PNG et deduction des coordonnees.")

    unknown = [spec for spec in specs if spec.x is None or spec.y is None]
    if unknown:
        known = {(spec.x, spec.y) for spec in specs if spec.x is not None and spec.y is not None}
        count = max(len(specs), 1)
        columns = columns_hint or (math.isqrt(count) if math.isqrt(count) ** 2 == count else count)
        cursor = 0
        for spec in sorted(unknown, key=lambda item: item.relative_file.lower()):
            while (cursor % columns, cursor // columns) in known:
                cursor += 1
            spec.x = cursor % columns
            spec.y = cursor // columns
            known.add((spec.x, spec.y))
            cursor += 1
        notes.append("Coordonnees absentes completees en ordre lexical ligne par ligne.")
    return specs, notes


def _grid_info(
    specs: list[TileSpec],
    manifest: dict[str, Any] | None,
    options: ValidationOptions,
    thresholds: dict[str, Any],
) -> GridInfo:
    tile_settings = (manifest or {}).get("tile_settings")
    tile_settings = tile_settings if isinstance(tile_settings, dict) else {}
    grid = (manifest or {}).get("grid")
    grid = grid if isinstance(grid, dict) else {}
    tiling = (manifest or {}).get("tiling")
    tiling = tiling if isinstance(tiling, dict) else {}

    xs = [int(spec.x) for spec in specs if spec.x is not None]
    ys = [int(spec.y) for spec in specs if spec.y is not None]
    origin_x = min(xs, default=0)
    origin_y = min(ys, default=0)
    inferred_columns = max(xs, default=-1) - origin_x + 1
    inferred_rows = max(ys, default=-1) - origin_y + 1

    columns = options.columns or tile_settings.get("columns") or grid.get("columns") or tiling.get("columns") or inferred_columns or 1
    rows = options.rows or tile_settings.get("rows") or grid.get("rows") or tiling.get("rows") or inferred_rows or 1
    manifest_expected = (
        tile_settings.get("expected_tile_count")
        or (manifest or {}).get("expected_count")
        or grid.get("expected_count")
        or (int(tiling.get("rows", 0)) * int(tiling.get("columns", 0)) if tiling else None)
    )
    expected = options.expected_count or thresholds.get("expected_count") or manifest_expected or columns * rows
    return GridInfo(
        columns=int(columns),
        rows=int(rows),
        expected_count=int(expected),
        coordinate_origin_x=origin_x,
        coordinate_origin_y=origin_y,
    )


def _dhash(image: Image.Image) -> int:
    gray = image.convert("L").resize((9, 8), Image.Resampling.BILINEAR)
    pixels = np.asarray(gray, dtype=np.int16)
    bits = pixels[:, 1:] > pixels[:, :-1]
    value = 0
    for bit in bits.flatten():
        value = (value << 1) | int(bit)
    return value


def _hamming(left: int, right: int) -> int:
    return (left ^ right).bit_count()


def _analyze_image(spec: TileSpec, thresholds: dict[str, Any]) -> TileRuntime:
    base: dict[str, Any] = {
        "id": spec.tile_id,
        "file": spec.relative_file,
        "grid": {"x": spec.x, "y": spec.y},
        "exists": spec.file_path.is_file(),
    }
    if not spec.file_path.is_file():
        base.update({"status": "FAIL", "error": "Fichier absent du disque."})
        return TileRuntime(spec=spec, image=None, analysis=base)

    base["disk_bytes"] = spec.file_path.stat().st_size
    base["sha256"] = _sha256(spec.file_path)
    if spec.expected_sha256:
        base["expected_sha256"] = spec.expected_sha256.lower()
        base["hash_match"] = base["sha256"].lower() == spec.expected_sha256.lower()

    try:
        with Image.open(spec.file_path) as source:
            source.load()
            original_mode = source.mode
            original_bands = list(source.getbands())
            image = source.convert("RGBA").copy()
    except Exception as exc:
        base.update({"status": "FAIL", "error": f"Image illisible: {type(exc).__name__}: {exc}"})
        return TileRuntime(spec=spec, image=None, analysis=base)

    sample = image.copy()
    sample.thumbnail((512, 512), Image.Resampling.BILINEAR)
    rgba = np.asarray(sample, dtype=np.float32)
    alpha = rgba[:, :, 3]
    opaque = alpha > float(thresholds["transparent_alpha_threshold"])
    transparent_ratio = float(np.mean(~opaque))
    rgb = rgba[:, :, :3]
    luma = (0.2126 * rgb[:, :, 0] + 0.7152 * rgb[:, :, 1] + 0.0722 * rgb[:, :, 2]) / 255.0
    if np.any(opaque):
        opaque_luma = luma[opaque]
        black_ratio = float(np.mean(opaque_luma <= float(thresholds["black_luma_threshold"])))
        luma_stddev = float(np.std(opaque_luma))
        mean_rgb = np.mean(rgb[opaque], axis=0) / 255.0
    else:
        black_ratio = 0.0
        luma_stddev = 0.0
        mean_rgb = np.zeros(3, dtype=np.float32)

    status = "PASS"
    warnings: list[str] = []
    failures: list[str] = []
    if spec.expected_sha256 and not base.get("hash_match", False):
        failures.append("SHA-256 different du manifest.")
    if original_mode not in {"RGB", "RGBA"}:
        warnings.append(f"Mode image {original_mode}; RGB ou RGBA recommande.")
    if transparent_ratio >= float(thresholds["transparent_ratio_fail"]):
        failures.append("Image presque entierement transparente.")
    elif transparent_ratio >= float(thresholds["transparent_ratio_warn"]):
        warnings.append("Grande zone transparente detectee.")
    if black_ratio >= float(thresholds["black_ratio_fail"]):
        failures.append("Image presque entierement noire.")
    elif black_ratio >= float(thresholds["black_ratio_warn"]):
        warnings.append("Grande zone noire detectee.")
    if luma_stddev <= float(thresholds["low_variance_stddev_warn"]):
        warnings.append("Variance visuelle anormalement faible.")
    if failures:
        status = "FAIL"
    elif warnings:
        status = "WARN"

    base.update(
        {
            "status": status,
            "width": image.width,
            "height": image.height,
            "mode": original_mode,
            "bands": original_bands,
            "channel_count": len(original_bands),
            "has_alpha": "A" in original_bands,
            "transparent_ratio": round(transparent_ratio, 6),
            "black_ratio": round(black_ratio, 6),
            "luma_stddev": round(luma_stddev, 6),
            "mean_rgb": [round(float(value), 6) for value in mean_rgb],
            "dhash64": f"{_dhash(image):016x}",
            "warnings": warnings,
            "failures": failures,
        }
    )
    return TileRuntime(spec=spec, image=image, analysis=base)


def _border_arrays(
    left: Image.Image,
    right: Image.Image,
    direction: str,
    strip_width: int,
) -> tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray, float]:
    left_rgb = np.asarray(left.convert("RGB"), dtype=np.float32)
    right_rgb = np.asarray(right.convert("RGB"), dtype=np.float32)

    if direction == "E":
        length = min(left_rgb.shape[0], right_rgb.shape[0])
        width = min(strip_width, left_rgb.shape[1], right_rgb.shape[1])
        a = left_rgb[:length, -width:, :][:, ::-1, :]
        b = right_rgb[:length, :width, :]
        edge_a = left_rgb[:length, -1, :]
        edge_b = right_rgb[:length, 0, :]
        if left_rgb.shape[1] > 1 and right_rgb.shape[1] > 1:
            internal_a = np.abs(left_rgb[:length, -1, :] - left_rgb[:length, -2, :])
            internal_b = np.abs(right_rgb[:length, 1, :] - right_rgb[:length, 0, :])
        else:
            internal_a = np.zeros_like(edge_a)
            internal_b = np.zeros_like(edge_b)
    elif direction == "S":
        length = min(left_rgb.shape[1], right_rgb.shape[1])
        width = min(strip_width, left_rgb.shape[0], right_rgb.shape[0])
        a = left_rgb[-width:, :length, :][::-1, :, :]
        b = right_rgb[:width, :length, :]
        edge_a = left_rgb[-1, :length, :]
        edge_b = right_rgb[0, :length, :]
        if left_rgb.shape[0] > 1 and right_rgb.shape[0] > 1:
            internal_a = np.abs(left_rgb[-1, :length, :] - left_rgb[-2, :length, :])
            internal_b = np.abs(right_rgb[1, :length, :] - right_rgb[0, :length, :])
        else:
            internal_a = np.zeros_like(edge_a)
            internal_b = np.zeros_like(edge_b)
    else:
        raise ValueError(f"Direction de couture non supportee: {direction}")

    baseline = float((np.mean(internal_a) + np.mean(internal_b)) / 2.0 / 255.0)
    return a, b, edge_a, edge_b, baseline


def _seam_metrics(
    tile_a: TileRuntime,
    tile_b: TileRuntime,
    direction: str,
    thresholds: dict[str, Any],
) -> dict[str, Any]:
    assert tile_a.image is not None and tile_b.image is not None
    strip_a, strip_b, edge_a, edge_b, internal_baseline = _border_arrays(
        tile_a.image,
        tile_b.image,
        direction,
        int(thresholds["seam_strip_width"]),
    )
    pixel_mae = float(np.mean(np.abs(strip_a - strip_b)) / 255.0)
    boundary_mae = float(np.mean(np.abs(edge_a - edge_b)) / 255.0)
    mean_a = np.mean(edge_a, axis=0)
    mean_b = np.mean(edge_b, axis=0)
    color_delta = float(np.linalg.norm(mean_a - mean_b) / (math.sqrt(3.0) * 255.0))

    gray_a = 0.2126 * edge_a[:, 0] + 0.7152 * edge_a[:, 1] + 0.0722 * edge_a[:, 2]
    gray_b = 0.2126 * edge_b[:, 0] + 0.7152 * edge_b[:, 1] + 0.0722 * edge_b[:, 2]
    std_a = float(np.std(gray_a))
    std_b = float(np.std(gray_b))
    if std_a > 1.0 and std_b > 1.0:
        correlation = float(np.corrcoef(gray_a, gray_b)[0, 1])
        if not math.isfinite(correlation):
            correlation = 0.0
        structure_difference = (1.0 - max(-1.0, min(1.0, correlation))) / 2.0
    else:
        correlation = None
        structure_difference = min(1.0, abs(float(np.mean(gray_a) - np.mean(gray_b))) / 255.0)

    discontinuity_ratio = boundary_mae / max(internal_baseline, 1.0 / 255.0)
    discontinuity_normalized = min(
        1.0,
        discontinuity_ratio / float(thresholds["seam_discontinuity_ratio_cap"]),
    )
    score = (
        pixel_mae * float(thresholds["seam_pixel_mae_weight"])
        + color_delta * float(thresholds["seam_color_delta_weight"])
        + structure_difference * float(thresholds["seam_structure_weight"])
        + discontinuity_normalized * float(thresholds["seam_discontinuity_weight"])
    )
    if score <= float(thresholds["seam_pass_score"]):
        status = "PASS"
    elif score <= float(thresholds["seam_warn_score"]):
        status = "WARN"
    else:
        status = "FAIL"

    return {
        "id": f"{tile_a.spec.tile_id}--{direction}--{tile_b.spec.tile_id}",
        "status": status,
        "tile_a": tile_a.spec.tile_id,
        "tile_b": tile_b.spec.tile_id,
        "direction": direction,
        "grid_a": {"x": tile_a.spec.x, "y": tile_a.spec.y},
        "grid_b": {"x": tile_b.spec.x, "y": tile_b.spec.y},
        "score": round(score, 6),
        "pixel_mae": round(pixel_mae, 6),
        "boundary_mae": round(boundary_mae, 6),
        "mean_color_delta": round(color_delta, 6),
        "structure_difference": round(structure_difference, 6),
        "edge_profile_correlation": None if correlation is None else round(correlation, 6),
        "internal_gradient_baseline": round(internal_baseline, 6),
        "discontinuity_ratio": round(discontinuity_ratio, 6),
    }


def _coordinate_map(runtimes: list[TileRuntime]) -> tuple[dict[tuple[int, int], TileRuntime], list[dict[str, Any]]]:
    mapping: dict[tuple[int, int], TileRuntime] = {}
    duplicates: list[dict[str, Any]] = []
    for runtime in runtimes:
        spec = runtime.spec
        if spec.x is None or spec.y is None:
            continue
        key = (spec.x, spec.y)
        if key in mapping:
            duplicates.append(
                {
                    "x": spec.x,
                    "y": spec.y,
                    "first": mapping[key].spec.tile_id,
                    "second": spec.tile_id,
                }
            )
        else:
            mapping[key] = runtime
    return mapping, duplicates


def _analyze_seams(
    coordinate_map: dict[tuple[int, int], TileRuntime],
    thresholds: dict[str, Any],
) -> list[dict[str, Any]]:
    seams: list[dict[str, Any]] = []
    for (x, y), tile in sorted(coordinate_map.items()):
        if tile.image is None:
            continue
        for direction, neighbor_coord in (("E", (x + 1, y)), ("S", (x, y + 1))):
            neighbor = coordinate_map.get(neighbor_coord)
            if neighbor is None or neighbor.image is None:
                continue
            seams.append(_seam_metrics(tile, neighbor, direction, thresholds))
    return seams


def _duplicate_analysis(runtimes: list[TileRuntime], thresholds: dict[str, Any]) -> dict[str, Any]:
    readable = [runtime for runtime in runtimes if runtime.image is not None]
    exact: list[dict[str, str]] = []
    quasi: list[dict[str, Any]] = []
    for index, left in enumerate(readable):
        for right in readable[index + 1 :]:
            left_analysis = left.analysis
            right_analysis = right.analysis
            if left_analysis["sha256"] == right_analysis["sha256"]:
                exact.append({"tile_a": left.spec.tile_id, "tile_b": right.spec.tile_id})
                continue
            distance = _hamming(int(left_analysis["dhash64"], 16), int(right_analysis["dhash64"], 16))
            mean_left = np.asarray(left_analysis["mean_rgb"], dtype=np.float32)
            mean_right = np.asarray(right_analysis["mean_rgb"], dtype=np.float32)
            mean_delta = float(np.linalg.norm(mean_left - mean_right) / math.sqrt(3.0))
            if (
                distance <= int(thresholds["quasi_duplicate_hamming_distance"])
                and mean_delta <= float(thresholds["quasi_duplicate_mean_color_delta"])
            ):
                quasi.append(
                    {
                        "tile_a": left.spec.tile_id,
                        "tile_b": right.spec.tile_id,
                        "dhash_hamming_distance": distance,
                        "mean_color_delta": round(mean_delta, 6),
                    }
                )
    return {"exact": exact, "quasi": quasi}


def _neighbor_errors(
    coordinate_map: dict[tuple[int, int], TileRuntime],
) -> list[dict[str, Any]]:
    errors: list[dict[str, Any]] = []
    for (x, y), runtime in sorted(coordinate_map.items()):
        declared = runtime.spec.declared_neighbors
        if declared is None:
            continue
        for direction, (dx, dy) in CARDINALS.items():
            actual_neighbor = coordinate_map.get((x + dx, y + dy))
            expected_id = actual_neighbor.spec.tile_id if actual_neighbor else None
            declared_id = declared.get(direction)
            if declared_id != expected_id:
                errors.append(
                    {
                        "tile": runtime.spec.tile_id,
                        "direction": direction,
                        "declared": declared_id,
                        "computed": expected_id,
                    }
                )
    return errors


def _gpu_bytes(width: int, height: int, block_width: int, block_height: int, block_bytes: int) -> int:
    return math.ceil(width / block_width) * math.ceil(height / block_height) * block_bytes


def _memory_budget(runtimes: list[TileRuntime], thresholds: dict[str, Any]) -> dict[str, Any]:
    readable = [runtime for runtime in runtimes if runtime.image is not None]
    disk = sum(int(runtime.analysis["disk_bytes"]) for runtime in readable)
    pixels = sum(runtime.image.width * runtime.image.height for runtime in readable if runtime.image)
    rgba32 = pixels * 4
    rgb24 = pixels * 3
    etc2_rgb = sum(
        _gpu_bytes(runtime.image.width, runtime.image.height, 4, 4, 8)
        for runtime in readable
        if runtime.image
    )
    etc2_rgba = sum(
        _gpu_bytes(runtime.image.width, runtime.image.height, 4, 4, 16)
        for runtime in readable
        if runtime.image
    )
    astc_6x6 = sum(
        _gpu_bytes(runtime.image.width, runtime.image.height, 6, 6, 16)
        for runtime in readable
        if runtime.image
    )
    astc_8x8 = sum(
        _gpu_bytes(runtime.image.width, runtime.image.height, 8, 8, 16)
        for runtime in readable
        if runtime.image
    )
    mib = 1024.0 * 1024.0

    values = {
        "disk_mb": disk / mib,
        "gpu_rgba32_mb": rgba32 / mib,
        "gpu_rgb24_mb": rgb24 / mib,
        "gpu_etc2_rgb4bpp_mb": etc2_rgb / mib,
        "gpu_etc2_rgba8bpp_mb": etc2_rgba / mib,
        "gpu_astc_6x6_mb": astc_6x6 / mib,
        "gpu_astc_8x8_mb": astc_8x8 / mib,
        "gpu_rgba32_with_mips_mb": rgba32 * 4.0 / 3.0 / mib,
        "gpu_astc_6x6_with_mips_mb": astc_6x6 * 4.0 / 3.0 / mib,
    }
    status = "PASS"
    reasons: list[str] = []
    comparisons = (
        ("disk_mb", "disk_warn_mb", "disk_fail_mb", "budget disque"),
        ("gpu_rgba32_mb", "gpu_rgba32_warn_mb", "gpu_rgba32_fail_mb", "RGBA32 decode"),
        ("gpu_astc_6x6_mb", "gpu_astc_6x6_warn_mb", "gpu_astc_6x6_fail_mb", "ASTC 6x6"),
    )
    for value_key, warn_key, fail_key, label in comparisons:
        if values[value_key] >= float(thresholds[fail_key]):
            status = "FAIL"
            reasons.append(f"{label} depasse le seuil FAIL")
        elif values[value_key] >= float(thresholds[warn_key]) and status != "FAIL":
            status = "WARN"
            reasons.append(f"{label} depasse le seuil WARN")
    return {
        "status": status,
        "tile_pixels": pixels,
        "values_mb": {key: round(value, 3) for key, value in values.items()},
        "reasons": reasons,
        "note": "Estimations texture seules; mipmaps approximes par facteur 4/3, hors overhead moteur.",
    }


def _required_tile_size_check(
    runtimes: list[TileRuntime],
    required_width: int | None,
    required_height: int | None,
) -> dict[str, Any] | None:
    if required_width is None and required_height is None:
        return None
    width = int(required_width or required_height or 0)
    height = int(required_height or required_width or 0)
    mismatches = []
    for runtime in runtimes:
        actual = runtime.image.size if runtime.image is not None else None
        if actual != (width, height):
            mismatches.append(
                {
                    "tile": runtime.spec.tile_id,
                    "actual": None if actual is None else {"width": actual[0], "height": actual[1]},
                    "expected": {"width": width, "height": height},
                }
            )
    status = "PASS" if not mismatches and runtimes else "FAIL"
    return _check(
        "required_tile_size",
        status,
        f"Toutes les tuiles mesurent exactement {width} x {height}."
        if status == "PASS"
        else f"Une ou plusieurs tuiles ne mesurent pas {width} x {height}.",
        expected={"width": width, "height": height},
        mismatches=mismatches,
    )


def _manifest_contract(
    specs: list[TileSpec],
    grid: GridInfo,
    manifest: dict[str, Any] | None,
    required_tile_width: int | None,
    required_tile_height: int | None,
    required: bool,
) -> dict[str, Any]:
    """Validate the canonical Wave3 slice declarations, independently from pixels."""
    if not required:
        return {"status": "PASS", "required": False, "issues": []}

    width = int(required_tile_width or 0)
    height = int(required_tile_height or 0)
    issues: list[dict[str, Any]] = []
    derived_source_rects: list[dict[str, Any]] = []
    tile_ids: dict[str, list[str]] = {}
    files: dict[str, list[str]] = {}
    hashes: dict[str, list[str]] = {}
    for spec in specs:
        tile_ids.setdefault(spec.tile_id, []).append(spec.relative_file)
        files.setdefault(spec.relative_file.lower(), []).append(spec.tile_id)
        if spec.expected_sha256:
            hashes.setdefault(spec.expected_sha256.lower(), []).append(spec.tile_id)
        else:
            issues.append({"tile": spec.tile_id, "reason": "missing_sha256"})

        if spec.x is None or spec.y is None:
            issues.append({"tile": spec.tile_id, "reason": "missing_grid_position"})
            continue
        expected_pixel_x = (int(spec.x) - grid.coordinate_origin_x) * width
        expected_pixel_y = (int(spec.y) - grid.coordinate_origin_y) * height
        entry = spec.manifest_entry
        rect = entry.get("source_rect") or entry.get("pixel_rect")
        if rect is None:
            derived_source_rects.append(
                {
                    "tile": spec.tile_id,
                    "x": expected_pixel_x,
                    "y": expected_pixel_y,
                    "width": width,
                    "height": height,
                    "basis": "grid_position_and_declared_uniform_tile_size",
                }
            )
        elif spec.pixel_x != expected_pixel_x or spec.pixel_y != expected_pixel_y:
            issues.append(
                {
                    "tile": spec.tile_id,
                    "reason": "source_rect_position_mismatch",
                    "declared": {"x": spec.pixel_x, "y": spec.pixel_y},
                    "expected": {"x": expected_pixel_x, "y": expected_pixel_y},
                }
            )
        rect_width = rect.get("width") if isinstance(rect, dict) else None
        rect_height = rect.get("height") if isinstance(rect, dict) else None
        if rect is not None and (rect_width != width or rect_height != height):
            issues.append(
                {
                    "tile": spec.tile_id,
                    "reason": "source_rect_size_mismatch",
                    "declared": {"width": rect_width, "height": rect_height},
                    "expected": {"width": width, "height": height},
                }
            )
        if spec.expected_width != width or spec.expected_height != height:
            issues.append(
                {
                    "tile": spec.tile_id,
                    "reason": "stored_dimensions_missing_or_mismatch",
                    "declared": {"width": spec.expected_width, "height": spec.expected_height},
                    "expected": {"width": width, "height": height},
                }
            )
        if spec.declared_neighbors is None:
            issues.append({"tile": spec.tile_id, "reason": "missing_neighbors"})

    for tile_id, members in tile_ids.items():
        if len(members) > 1:
            issues.append({"reason": "duplicate_tile_id", "tile_id": tile_id, "files": members})
    for filename, members in files.items():
        if len(members) > 1:
            issues.append({"reason": "duplicate_tile_file", "file": filename, "tiles": members})
    for sha256, members in hashes.items():
        if len(members) > 1:
            issues.append({"reason": "duplicate_tile_hash", "sha256": sha256, "tiles": members})

    return {
        "status": "PASS" if not issues and len(specs) == grid.expected_count else "FAIL",
        "required": True,
        "declared_tile_count": len(specs),
        "expected_tile_count": grid.expected_count,
        "unique_tile_id_count": len(tile_ids),
        "unique_file_count": len(files),
        "unique_hash_count": len(hashes),
        "source_rect_policy": "explicit_or_deterministically_derived_from_grid",
        "derived_source_rect_count": len(derived_source_rects),
        "derived_source_rects": derived_source_rects,
        "manifest_schema": (manifest or {}).get("schema"),
        "issues": issues,
    }


def _manifest_master_block(manifest: dict[str, Any] | None) -> dict[str, Any]:
    if not manifest:
        return {}
    for key in ("master", "atlas", "source"):
        candidate = manifest.get(key)
        if isinstance(candidate, dict):
            return candidate
    return {}


def _master_contract(
    input_dir: Path,
    manifest: dict[str, Any] | None,
    reference_atlas_path: Path | None,
    required_width: int | None,
    required_height: int | None,
    required: bool,
) -> dict[str, Any]:
    block = _manifest_master_block(manifest)
    declared_file = _first_text(block, ("file", "path", "filename", "image"))
    declared_sha256 = _declared_hash(block)
    declared_width = _nested_int(block, ("dimensions", "width"), ("stored_dimensions", "width"), ("width",))
    declared_height = _nested_int(block, ("dimensions", "height"), ("stored_dimensions", "height"), ("height",))
    issues: list[dict[str, Any]] = []

    reference = reference_atlas_path.resolve() if reference_atlas_path else None
    declared_path = _resolve_input_file(input_dir, declared_file) if declared_file else None
    master_path = reference or declared_path
    if required and manifest is None:
        issues.append({"reason": "manifest_required"})
    if required and not declared_file:
        issues.append({"reason": "master_file_missing_in_manifest"})
    if required and not declared_sha256:
        issues.append({"reason": "master_sha256_missing_in_manifest"})
    if required and (declared_width is None or declared_height is None):
        issues.append({"reason": "master_dimensions_missing_in_manifest"})
    if required and reference is None:
        issues.append({"reason": "reference_master_required"})
    if reference and declared_path and reference != declared_path:
        issues.append(
            {
                "reason": "reference_master_path_differs_from_manifest",
                "reference": str(reference),
                "manifest_path": str(declared_path),
            }
        )

    actual_sha256: str | None = None
    actual_width: int | None = None
    actual_height: int | None = None
    if master_path is None or not master_path.is_file():
        if required:
            issues.append({"reason": "master_file_missing", "path": str(master_path) if master_path else None})
    else:
        actual_sha256 = _sha256(master_path)
        try:
            with Image.open(master_path) as image:
                actual_width, actual_height = image.size
                image.verify()
        except Exception as exc:
            issues.append({"reason": "master_unreadable", "error": f"{type(exc).__name__}: {exc}"})
        if declared_sha256 and actual_sha256.lower() != declared_sha256.lower():
            issues.append(
                {
                    "reason": "master_sha256_mismatch",
                    "declared": declared_sha256,
                    "actual": actual_sha256,
                }
            )
        if declared_width is not None and actual_width != declared_width:
            issues.append({"reason": "master_declared_width_mismatch", "declared": declared_width, "actual": actual_width})
        if declared_height is not None and actual_height != declared_height:
            issues.append({"reason": "master_declared_height_mismatch", "declared": declared_height, "actual": actual_height})
        if required_width is not None and actual_width != required_width:
            issues.append({"reason": "master_required_width_mismatch", "required": required_width, "actual": actual_width})
        if required_height is not None and actual_height != required_height:
            issues.append({"reason": "master_required_height_mismatch", "required": required_height, "actual": actual_height})

    return {
        "status": "PASS" if not issues else "FAIL",
        "required": required,
        "path": str(master_path) if master_path else None,
        "declared_file": declared_file,
        "declared_sha256": declared_sha256,
        "actual_sha256": actual_sha256,
        "declared_dimensions": {"width": declared_width, "height": declared_height},
        "actual_dimensions": {"width": actual_width, "height": actual_height},
        "required_dimensions": {"width": required_width, "height": required_height},
        "issues": issues,
    }


def _declared_gutter(entry: dict[str, Any]) -> tuple[str | None, str | None, int | None, int | None]:
    nested = entry.get("runtime_gutter") or entry.get("gutter")
    nested = nested if isinstance(nested, dict) else {}
    filename = _first_text(entry, ("gutter_file", "runtime_gutter_file")) or _first_text(
        nested, ("file", "path", "filename", "image")
    )
    sha256 = _declared_hash(nested) or _first_text(entry, ("gutter_sha256", "runtime_gutter_sha256"))
    width = _nested_int(nested, ("dimensions", "width"), ("width",))
    height = _nested_int(nested, ("dimensions", "height"), ("height",))
    return filename, sha256, width, height


def _resolve_gutter_file(gutters_dir: Path, declared: str) -> Path:
    path = Path(declared)
    if path.is_absolute():
        return path.resolve()
    direct = (gutters_dir / path).resolve()
    if direct.is_file():
        return direct
    return (gutters_dir / path.name).resolve()


def _validate_runtime_gutters(
    reconstruction: Image.Image,
    runtimes: list[TileRuntime],
    coordinate_map: dict[tuple[int, int], TileRuntime],
    placements: dict[str, tuple[int, int]],
    gutters_dir: Path | None,
    gutter_size: int,
    required: bool,
    output_dir: Path,
) -> dict[str, Any]:
    if not required and gutters_dir is None:
        return {
            "status": "NOT_DELIVERED",
            "required": False,
            "blocking_canonical_art_validation": False,
            "validated_count": 0,
            "boundary_count": 0,
            "boundaries": [],
            "reason": "runtime_gutters_not_present_in_canonical_art_package",
        }
    if gutter_size <= 0:
        return {"status": "FAIL", "required": required, "reason": "gutter_size_must_be_positive"}
    if gutters_dir is None or not gutters_dir.resolve().is_dir():
        return {
            "status": "FAIL",
            "required": required,
            "reason": "gutters_directory_missing",
            "path": str(gutters_dir.resolve()) if gutters_dir else None,
            "validated_count": 0,
            "boundaries": [],
        }

    resolved_dir = gutters_dir.resolve()
    master = np.asarray(reconstruction.convert("RGBA"), dtype=np.uint8)
    padded = np.pad(master, ((gutter_size, gutter_size), (gutter_size, gutter_size), (0, 0)), mode="edge")
    rows: list[dict[str, Any]] = []
    arrays: dict[str, np.ndarray] = {}
    file_paths: set[Path] = set()
    expected_size: tuple[int, int] | None = None
    for runtime in runtimes:
        spec = runtime.spec
        filename, declared_sha256, declared_width, declared_height = _declared_gutter(spec.manifest_entry)
        issues: list[str] = []
        if not filename:
            issues.append("missing_gutter_file_declaration")
            path = resolved_dir / f"{spec.tile_id}_gutter.png"
        else:
            path = _resolve_gutter_file(resolved_dir, filename)
        if path in file_paths:
            issues.append("duplicate_gutter_file_reference")
        file_paths.add(path)
        canonical = runtime.image.convert("RGBA") if runtime.image is not None else None
        if canonical is not None:
            expected_size = (canonical.width + 2 * gutter_size, canonical.height + 2 * gutter_size)
        actual_sha256: str | None = None
        actual_size: tuple[int, int] | None = None
        full_pixel_identical = False
        center_pixel_identical = False
        if canonical is None:
            issues.append("canonical_tile_unreadable")
        elif spec.tile_id not in placements:
            issues.append("canonical_placement_missing")
        elif not path.is_file():
            issues.append("gutter_file_missing")
        else:
            try:
                with Image.open(path) as source:
                    gutter_image = source.convert("RGBA")
                actual = np.asarray(gutter_image, dtype=np.uint8)
                actual_size = gutter_image.size
                actual_sha256 = _sha256(path)
                expected = expected_size
                if actual_size != expected:
                    issues.append("gutter_dimensions_mismatch")
                else:
                    x, y = placements[spec.tile_id]
                    expected_pixels = padded[y:y + canonical.height + 2 * gutter_size, x:x + canonical.width + 2 * gutter_size]
                    full_pixel_identical = bool(np.array_equal(actual, expected_pixels))
                    center = actual[
                        gutter_size:gutter_size + canonical.height,
                        gutter_size:gutter_size + canonical.width,
                    ]
                    center_pixel_identical = bool(np.array_equal(center, np.asarray(canonical, dtype=np.uint8)))
                    if not full_pixel_identical:
                        issues.append("gutter_not_derived_from_true_neighbors")
                    if not center_pixel_identical:
                        issues.append("gutter_center_differs_from_canonical_tile")
                    arrays[spec.tile_id] = actual
                if declared_sha256 is None:
                    issues.append("missing_gutter_sha256")
                elif actual_sha256.lower() != declared_sha256.lower():
                    issues.append("gutter_sha256_mismatch")
                if declared_width != expected[0] or declared_height != expected[1]:
                    issues.append("gutter_manifest_dimensions_missing_or_mismatch")
                gutter_image.close()
            except Exception as exc:
                issues.append(f"gutter_unreadable:{type(exc).__name__}:{exc}")
        rows.append(
            {
                "tile": spec.tile_id,
                "file": str(path),
                "status": "PASS" if not issues else "FAIL",
                "expected_size": None if expected_size is None else {"width": expected_size[0], "height": expected_size[1]},
                "actual_size": None if actual_size is None else {"width": actual_size[0], "height": actual_size[1]},
                "declared_sha256": declared_sha256,
                "actual_sha256": actual_sha256,
                "center_pixel_identical": center_pixel_identical,
                "full_pixel_identical": full_pixel_identical,
                "issues": issues,
            }
        )

    boundaries: list[dict[str, Any]] = []
    for (x, y), left in sorted(coordinate_map.items()):
        left_array = arrays.get(left.spec.tile_id)
        for direction, neighbor_coord in (("E", (x + 1, y)), ("S", (x, y + 1))):
            right = coordinate_map.get(neighbor_coord)
            if right is None:
                continue
            right_array = arrays.get(right.spec.tile_id)
            ok = False
            if left_array is not None and right_array is not None and left.image is not None and right.image is not None:
                left_canonical = np.asarray(left.image.convert("RGBA"), dtype=np.uint8)
                right_canonical = np.asarray(right.image.convert("RGBA"), dtype=np.uint8)
                g = gutter_size
                if direction == "E":
                    ok = bool(
                        np.array_equal(left_array[g:-g, -g:], right_canonical[:, :g])
                        and np.array_equal(right_array[g:-g, :g], left_canonical[:, -g:])
                    )
                else:
                    ok = bool(
                        np.array_equal(left_array[-g:, g:-g], right_canonical[:g, :])
                        and np.array_equal(right_array[:g, g:-g], left_canonical[-g:, :])
                    )
            boundaries.append(
                {
                    "id": f"{left.spec.tile_id}:{direction}:{right.spec.tile_id}",
                    "tile_a": left.spec.tile_id,
                    "tile_b": right.spec.tile_id,
                    "direction": direction,
                    "status": "PASS" if ok else "FAIL",
                }
            )

    readable_arrays = [(row["tile"], arrays[row["tile"]]) for row in rows if row["tile"] in arrays]
    artifact: dict[str, Any] | None = None
    if readable_arrays:
        thumb_size = 180
        sheet = Image.new("RGB", (5 * thumb_size, math.ceil(len(readable_arrays) / 5) * thumb_size), (20, 23, 28))
        draw = ImageDraw.Draw(sheet)
        for index, (tile_id, pixels) in enumerate(readable_arrays):
            preview = Image.fromarray(pixels).convert("RGB")
            preview.thumbnail((thumb_size - 12, thumb_size - 28), Image.Resampling.NEAREST)
            x0 = (index % 5) * thumb_size
            y0 = (index // 5) * thumb_size
            sheet.paste(preview, (x0 + 6, y0 + 20))
            draw.text((x0 + 6, y0 + 5), tile_id[:24], fill=(235, 238, 243), font=ImageFont.load_default())
            preview.close()
        artifact_path = output_dir / "runtime_gutters_contact_sheet.png"
        sheet.save(artifact_path, optimize=True)
        artifact = {
            "file": artifact_path.name,
            "width": sheet.width,
            "height": sheet.height,
            "sha256": _sha256(artifact_path),
            "debug_grid": True,
        }
        sheet.close()

    status = "PASS" if rows and all(row["status"] == "PASS" for row in rows) and boundaries and all(
        boundary["status"] == "PASS" for boundary in boundaries
    ) else "FAIL"
    return {
        "status": status,
        "required": required,
        "directory": str(resolved_dir),
        "gutter_size_each_side": gutter_size,
        "expected_runtime_dimensions": None if expected_size is None else {
            "width": expected_size[0],
            "height": expected_size[1],
        },
        "edge_policy": "clamp_master_edge; internal edges and corners come from real adjacent master pixels",
        "validated_count": len(rows),
        "pass_count": sum(row["status"] == "PASS" for row in rows),
        "fail_count": sum(row["status"] == "FAIL" for row in rows),
        "boundary_count": len(boundaries),
        "boundary_pass_count": sum(row["status"] == "PASS" for row in boundaries),
        "boundary_fail_count": sum(row["status"] == "FAIL" for row in boundaries),
        "tiles": rows,
        "boundaries": boundaries,
        "artifact": artifact,
    }


def _baseline_center_lock(
    runtimes: list[TileRuntime],
    grid: GridInfo,
    baseline_dir: Path | None,
    baseline_manifest_path: Path | None,
) -> dict[str, Any] | None:
    if baseline_dir is None:
        return None
    resolved_dir = baseline_dir.resolve()
    if not resolved_dir.is_dir():
        return {
            "status": "FAIL",
            "baseline_dir": str(resolved_dir),
            "error": "Dossier de reference centrale introuvable.",
            "matches": [],
            "mismatches": [],
        }

    manifest_path = _find_manifest(resolved_dir, baseline_manifest_path)
    manifest = _read_json(manifest_path) if manifest_path else None
    specs, notes = _build_tile_specs(resolved_dir, manifest, 3)
    baseline_map: dict[tuple[int, int], TileSpec] = {}
    duplicate_positions: list[dict[str, Any]] = []
    for spec in specs:
        if spec.x is None or spec.y is None:
            continue
        key = (int(spec.x), int(spec.y))
        if key in baseline_map:
            duplicate_positions.append(
                {"x": key[0], "y": key[1], "first": baseline_map[key].tile_id, "second": spec.tile_id}
            )
        else:
            baseline_map[key] = spec

    xs = sorted({coord[0] for coord in baseline_map})
    ys = sorted({coord[1] for coord in baseline_map})
    baseline_columns = max(xs) - min(xs) + 1 if xs else 0
    baseline_rows = max(ys) - min(ys) + 1 if ys else 0
    target_origin_x = grid.coordinate_origin_x + (grid.columns - baseline_columns) // 2
    target_origin_y = grid.coordinate_origin_y + (grid.rows - baseline_rows) // 2
    baseline_origin_x = min(xs, default=0)
    baseline_origin_y = min(ys, default=0)
    target_map, target_duplicate_positions = _coordinate_map(runtimes)

    matches: list[dict[str, Any]] = []
    mismatches: list[dict[str, Any]] = []
    baseline_hashes: dict[str, str] = {}
    for (baseline_x, baseline_y), spec in sorted(baseline_map.items()):
        target_coord = (
            target_origin_x + baseline_x - baseline_origin_x,
            target_origin_y + baseline_y - baseline_origin_y,
        )
        row: dict[str, Any] = {
            "baseline_tile": spec.tile_id,
            "baseline_grid": {"x": baseline_x, "y": baseline_y},
            "target_grid": {"x": target_coord[0], "y": target_coord[1]},
            "baseline_file": spec.relative_file,
        }
        if not spec.file_path.is_file():
            row["reason"] = "baseline_file_missing"
            mismatches.append(row)
            continue
        baseline_sha = _sha256(spec.file_path)
        baseline_hashes[spec.tile_id] = baseline_sha
        row["baseline_sha256"] = baseline_sha
        if spec.expected_sha256 and spec.expected_sha256.lower() != baseline_sha.lower():
            row["reason"] = "baseline_manifest_hash_mismatch"
            row["baseline_manifest_sha256"] = spec.expected_sha256.lower()
            mismatches.append(row)
            continue
        target = target_map.get(target_coord)
        if target is None or target.image is None:
            row["reason"] = "target_center_tile_missing_or_unreadable"
            mismatches.append(row)
            continue
        target_sha = str(target.analysis.get("sha256", ""))
        row.update(
            {
                "target_tile": target.spec.tile_id,
                "target_file": target.spec.relative_file,
                "target_sha256": target_sha,
            }
        )
        if target_sha.lower() != baseline_sha.lower():
            row["reason"] = "sha256_drift"
            mismatches.append(row)
        else:
            matches.append(row)

    structural_errors = []
    if len(specs) != 9 or baseline_columns != 3 or baseline_rows != 3:
        structural_errors.append(
            {
                "reason": "baseline_must_be_3x3_with_9_tiles",
                "actual_count": len(specs),
                "columns": baseline_columns,
                "rows": baseline_rows,
            }
        )
    structural_errors.extend(duplicate_positions)
    structural_errors.extend(target_duplicate_positions)
    status = "PASS" if len(matches) == 9 and not mismatches and not structural_errors else "FAIL"
    return {
        "status": status,
        "baseline_dir": str(resolved_dir),
        "baseline_manifest": str(manifest_path) if manifest_path else None,
        "baseline_count": len(specs),
        "baseline_grid": {"columns": baseline_columns, "rows": baseline_rows},
        "target_center_origin": {"x": target_origin_x, "y": target_origin_y},
        "match_count": len(matches),
        "mismatch_count": len(mismatches),
        "matches": matches,
        "mismatches": mismatches,
        "structural_errors": structural_errors,
        "baseline_hashes": baseline_hashes,
        "discovery_notes": notes,
    }


def _ring_analysis(
    runtimes: list[TileRuntime],
    grid: GridInfo,
    expected_new_ring_count: int | None,
    center_lock: dict[str, Any] | None,
) -> dict[str, Any] | None:
    if expected_new_ring_count is None:
        return None
    coordinate_map, _ = _coordinate_map(runtimes)
    min_x = grid.coordinate_origin_x
    min_y = grid.coordinate_origin_y
    max_x = min_x + grid.columns - 1
    max_y = min_y + grid.rows - 1
    ring_positions = {
        (x, y)
        for y in range(min_y, max_y + 1)
        for x in range(min_x, max_x + 1)
        if x in {min_x, max_x} or y in {min_y, max_y}
    }
    center_positions = {
        (x, y)
        for y in range(min_y + 1, max_y)
        for x in range(min_x + 1, max_x)
    }
    present_ring = sorted(position for position in ring_positions if position in coordinate_map)
    present_center = sorted(position for position in center_positions if position in coordinate_map)
    baseline_hashes = set((center_lock or {}).get("baseline_hashes", {}).values())
    reused_baseline_hashes = []
    for position in present_ring:
        runtime = coordinate_map[position]
        sha = runtime.analysis.get("sha256")
        if sha and sha in baseline_hashes:
            reused_baseline_hashes.append(
                {"tile": runtime.spec.tile_id, "grid": {"x": position[0], "y": position[1]}, "sha256": sha}
            )
    expected_center_count = max(0, (grid.columns - 2) * (grid.rows - 2))
    status = "PASS"
    reasons: list[str] = []
    if len(present_ring) != int(expected_new_ring_count):
        status = "FAIL"
        reasons.append("ring_count_mismatch")
    if len(present_center) != expected_center_count:
        status = "FAIL"
        reasons.append("center_count_mismatch")
    if reused_baseline_hashes:
        status = "FAIL"
        reasons.append("ring_reuses_baseline_center_hash")
    return {
        "status": status,
        "expected_ring_count": int(expected_new_ring_count),
        "actual_ring_count": len(present_ring),
        "expected_center_count": expected_center_count,
        "actual_center_count": len(present_center),
        "missing_ring_positions": [
            {"x": x, "y": y} for x, y in sorted(ring_positions - set(present_ring))
        ],
        "reused_baseline_hashes": reused_baseline_hashes,
        "reasons": reasons,
        "ring_tiles": [coordinate_map[position].spec.tile_id for position in present_ring],
    }


def _rectangle_union_area(rectangles: list[tuple[int, int, int, int]]) -> int:
    x_edges = sorted({value for rectangle in rectangles for value in (rectangle[0], rectangle[2])})
    area = 0
    for left, right in zip(x_edges, x_edges[1:]):
        if right <= left:
            continue
        intervals = sorted(
            (top, bottom)
            for x0, top, x1, bottom in rectangles
            if x0 < right and x1 > left and bottom > top
        )
        covered_y = 0
        if intervals:
            current_top, current_bottom = intervals[0]
            for top, bottom in intervals[1:]:
                if top <= current_bottom:
                    current_bottom = max(current_bottom, bottom)
                else:
                    covered_y += current_bottom - current_top
                    current_top, current_bottom = top, bottom
            covered_y += current_bottom - current_top
        area += (right - left) * covered_y
    return area


def _coverage_analysis(
    runtimes: list[TileRuntime],
    grid: GridInfo,
    required_width: int | None,
    required_height: int | None,
) -> dict[str, Any]:
    placements, logical_width, logical_height = _placements(runtimes, grid)
    rectangles: list[tuple[int, int, int, int]] = []
    rows: list[dict[str, Any]] = []
    for runtime in runtimes:
        if runtime.image is None or runtime.spec.tile_id not in placements:
            continue
        x, y = placements[runtime.spec.tile_id]
        rectangle = (x, y, x + runtime.image.width, y + runtime.image.height)
        rectangles.append(rectangle)
        rows.append({"tile": runtime.spec.tile_id, "rectangle": list(rectangle)})
    total_area = sum((right - left) * (bottom - top) for left, top, right, bottom in rectangles)
    union_area = _rectangle_union_area(rectangles) if rectangles else 0
    overlap_area = max(0, total_area - union_area)
    expected_width = int(required_width or 0) * grid.columns if required_width else logical_width
    expected_height = int(required_height or 0) * grid.rows if required_height else logical_height
    expected_area = expected_width * expected_height
    hole_area = max(0, expected_area - union_area)
    status = "PASS" if rectangles and overlap_area == 0 and hole_area == 0 and union_area == expected_area else "FAIL"
    return {
        "status": status,
        "logical_dimensions": {"width": logical_width, "height": logical_height},
        "expected_dimensions": {"width": expected_width, "height": expected_height},
        "tile_area_sum": total_area,
        "union_area": union_area,
        "expected_area": expected_area,
        "hole_area": hole_area,
        "overlap_area": overlap_area,
        "rectangles": rows,
    }


def _seam_statistics(
    seams: list[dict[str, Any]],
    grid: GridInfo,
    expected_seam_count: int | None,
) -> dict[str, Any]:
    scores = np.asarray([float(seam["score"]) for seam in seams], dtype=np.float64)
    expected_formula = grid.rows * max(grid.columns - 1, 0) + grid.columns * max(grid.rows - 1, 0)
    expected = int(expected_seam_count) if expected_seam_count is not None else expected_formula
    if scores.size:
        score_stats = {
            "minimum": round(float(np.min(scores)), 6),
            "mean": round(float(np.mean(scores)), 6),
            "median": round(float(np.median(scores)), 6),
            "p95": round(float(np.percentile(scores, 95)), 6),
            "maximum": round(float(np.max(scores)), 6),
        }
    else:
        score_stats = {"minimum": None, "mean": None, "median": None, "p95": None, "maximum": None}
    min_x = grid.coordinate_origin_x
    min_y = grid.coordinate_origin_y
    max_x = min_x + grid.columns - 1
    max_y = min_y + grid.rows - 1

    def is_ring(coordinate: dict[str, Any]) -> bool:
        x = coordinate.get("x")
        y = coordinate.get("y")
        return x in {min_x, max_x} or y in {min_y, max_y}

    groups: dict[str, list[dict[str, Any]]] = {
        "center_center": [],
        "center_ring": [],
        "ring_ring": [],
    }
    for seam in seams:
        a_ring = is_ring(seam["grid_a"])
        b_ring = is_ring(seam["grid_b"])
        group = "ring_ring" if a_ring and b_ring else "center_center" if not a_ring and not b_ring else "center_ring"
        groups[group].append(seam)

    grouped_statistics: dict[str, Any] = {}
    for name, rows in groups.items():
        group_scores = np.asarray([float(row["score"]) for row in rows], dtype=np.float64)
        grouped_statistics[name] = {
            "count": len(rows),
            "pass_count": sum(row["status"] == "PASS" for row in rows),
            "warn_count": sum(row["status"] == "WARN" for row in rows),
            "fail_count": sum(row["status"] == "FAIL" for row in rows),
            "minimum_score": round(float(np.min(group_scores)), 6) if group_scores.size else None,
            "mean_score": round(float(np.mean(group_scores)), 6) if group_scores.size else None,
            "maximum_score": round(float(np.max(group_scores)), 6) if group_scores.size else None,
        }

    return {
        "status": "PASS" if len(seams) == expected else "FAIL",
        "actual_count": len(seams),
        "expected_count": expected,
        "grid_formula_count": expected_formula,
        "pass_count": sum(seam["status"] == "PASS" for seam in seams),
        "warn_count": sum(seam["status"] == "WARN" for seam in seams),
        "fail_count": sum(seam["status"] == "FAIL" for seam in seams),
        "scores": score_stats,
        "by_boundary_class": grouped_statistics,
    }


def _metric_status(value: float, warn: float, fail: float) -> str:
    if value >= fail:
        return "FAIL"
    if value >= warn:
        return "WARN"
    return "PASS"


def _low_frequency_grid_analysis(
    reconstruction: Image.Image,
    seams: list[dict[str, Any]],
    grid: GridInfo,
    thresholds: dict[str, Any],
) -> dict[str, Any]:
    blur_radius = float(thresholds["perceptual_blur_radius"])
    blurred = reconstruction.convert("RGB").filter(ImageFilter.GaussianBlur(radius=blur_radius))
    rgb = np.asarray(blurred, dtype=np.float32) / 255.0
    luma = 0.2126 * rgb[:, :, 0] + 0.7152 * rgb[:, :, 1] + 0.0722 * rgb[:, :, 2]
    cell_width = reconstruction.width / float(max(grid.columns, 1))
    cell_height = reconstruction.height / float(max(grid.rows, 1))
    band = max(4, int(round(min(cell_width, cell_height) * 0.03125)))
    reference_offset_x = max(band * 2, int(round(cell_width * 0.25)))
    reference_offset_y = max(band * 2, int(round(cell_height * 0.25)))
    row_delta_threshold = float(thresholds["perceptual_grid_line_row_delta"])
    min_x = grid.coordinate_origin_x
    min_y = grid.coordinate_origin_y
    max_x = min_x + grid.columns - 1
    max_y = min_y + grid.rows - 1

    def is_ring(coordinate: dict[str, Any]) -> bool:
        x = coordinate.get("x")
        y = coordinate.get("y")
        return x in {min_x, max_x} or y in {min_y, max_y}

    rows: list[dict[str, Any]] = []
    for seam in seams:
        grid_a = seam["grid_a"]
        x_index = int(grid_a["x"]) - grid.coordinate_origin_x
        y_index = int(grid_a["y"]) - grid.coordinate_origin_y
        if seam["direction"] == "E":
            x = int(round((x_index + 1) * cell_width))
            y0 = int(round(y_index * cell_height))
            y1 = int(round((y_index + 1) * cell_height))
            x = max(1, min(rgb.shape[1] - 1, x))
            y0 = max(0, min(rgb.shape[0] - 1, y0))
            y1 = max(y0 + 1, min(rgb.shape[0], y1))
            boundary_gradient = float(np.mean(np.abs(rgb[y0:y1, x, :] - rgb[y0:y1, x - 1, :])))
            reference_values = []
            for candidate in (x - reference_offset_x, x + reference_offset_x):
                if 1 <= candidate < rgb.shape[1]:
                    reference_values.append(
                        float(np.mean(np.abs(rgb[y0:y1, candidate, :] - rgb[y0:y1, candidate - 1, :])))
                    )
            reference_gradient = float(np.median(reference_values)) if reference_values else 1.0 / 255.0
            left_band = rgb[y0:y1, max(0, x - band):x, :]
            right_band = rgb[y0:y1, x:min(rgb.shape[1], x + band), :]
            color_delta = float(np.mean(np.abs(np.mean(left_band, axis=(0, 1)) - np.mean(right_band, axis=(0, 1)))))
            boundary_luma = (luma[y0:y1, x - 1] + luma[y0:y1, x]) * 0.5
            left_luma = np.mean(luma[y0:y1, max(0, x - band):max(1, x - band // 2)], axis=1)
            right_luma = np.mean(
                luma[y0:y1, min(rgb.shape[1] - 1, x + band // 2):min(rgb.shape[1], x + band)],
                axis=1,
            )
            side_luma = (left_luma + right_luma) * 0.5
            line_coverage = float(np.mean(np.abs(boundary_luma - side_luma) >= row_delta_threshold))
        else:
            y = int(round((y_index + 1) * cell_height))
            x0 = int(round(x_index * cell_width))
            x1 = int(round((x_index + 1) * cell_width))
            y = max(1, min(rgb.shape[0] - 1, y))
            x0 = max(0, min(rgb.shape[1] - 1, x0))
            x1 = max(x0 + 1, min(rgb.shape[1], x1))
            boundary_gradient = float(np.mean(np.abs(rgb[y, x0:x1, :] - rgb[y - 1, x0:x1, :])))
            reference_values = []
            for candidate in (y - reference_offset_y, y + reference_offset_y):
                if 1 <= candidate < rgb.shape[0]:
                    reference_values.append(
                        float(np.mean(np.abs(rgb[candidate, x0:x1, :] - rgb[candidate - 1, x0:x1, :])))
                    )
            reference_gradient = float(np.median(reference_values)) if reference_values else 1.0 / 255.0
            top_band = rgb[max(0, y - band):y, x0:x1, :]
            bottom_band = rgb[y:min(rgb.shape[0], y + band), x0:x1, :]
            color_delta = float(np.mean(np.abs(np.mean(top_band, axis=(0, 1)) - np.mean(bottom_band, axis=(0, 1)))))
            boundary_luma = (luma[y - 1, x0:x1] + luma[y, x0:x1]) * 0.5
            top_luma = np.mean(luma[max(0, y - band):max(1, y - band // 2), x0:x1], axis=0)
            bottom_luma = np.mean(
                luma[min(rgb.shape[0] - 1, y + band // 2):min(rgb.shape[0], y + band), x0:x1],
                axis=0,
            )
            side_luma = (top_luma + bottom_luma) * 0.5
            line_coverage = float(np.mean(np.abs(boundary_luma - side_luma) >= row_delta_threshold))

        gradient_ratio = boundary_gradient / max(reference_gradient, 1.0 / 65535.0)
        statuses = [
            _metric_status(
                gradient_ratio,
                float(thresholds["perceptual_grid_gradient_ratio_warn"]),
                float(thresholds["perceptual_grid_gradient_ratio_fail"]),
            ),
            _metric_status(
                color_delta,
                float(thresholds["perceptual_boundary_color_delta_warn"]),
                float(thresholds["perceptual_boundary_color_delta_fail"]),
            ),
            _metric_status(
                line_coverage,
                float(thresholds["perceptual_grid_line_coverage_warn"]),
                float(thresholds["perceptual_grid_line_coverage_fail"]),
            ),
        ]
        a_ring = is_ring(seam["grid_a"])
        b_ring = is_ring(seam["grid_b"])
        boundary_class = (
            "ring_ring" if a_ring and b_ring else "center_center" if not a_ring and not b_ring else "center_ring"
        )
        rows.append(
            {
                "seam": seam["id"],
                "direction": seam["direction"],
                "boundary_class": boundary_class,
                "status": _max_status(statuses),
                "blur_radius": blur_radius,
                "boundary_gradient": round(boundary_gradient, 8),
                "reference_gradient": round(reference_gradient, 8),
                "gradient_ratio": round(gradient_ratio, 6),
                "low_frequency_color_delta": round(color_delta, 6),
                "grid_line_coverage": round(line_coverage, 6),
            }
        )
    center_ring_rows = [row for row in rows if row["boundary_class"] == "center_ring"]
    comparison_rows = [row for row in rows if row["boundary_class"] != "center_ring"]

    def broad_salience(row: dict[str, Any]) -> float:
        return float(row["low_frequency_color_delta"]) + row_delta_threshold * float(row["grid_line_coverage"])

    if center_ring_rows and comparison_rows:
        center_ring_median = float(np.median([broad_salience(row) for row in center_ring_rows]))
        comparison_median = float(np.median([broad_salience(row) for row in comparison_rows]))
        center_ring_ratio = center_ring_median / max(comparison_median, 1.0 / 65535.0)
        center_ring_status = _metric_status(
            center_ring_ratio,
            float(thresholds["perceptual_center_ring_ratio_warn"]),
            float(thresholds["perceptual_center_ring_ratio_fail"]),
        )
    else:
        center_ring_median = None
        comparison_median = None
        center_ring_ratio = None
        center_ring_status = "PASS"

    row_status = _max_status(row["status"] for row in rows)
    return {
        "status": _max_status((row_status, center_ring_status)),
        "blur_radius": blur_radius,
        "band_width": band,
        "segment_count": len(rows),
        "pass_count": sum(row["status"] == "PASS" for row in rows),
        "warn_count": sum(row["status"] == "WARN" for row in rows),
        "fail_count": sum(row["status"] == "FAIL" for row in rows),
        "maximum_gradient_ratio": max((row["gradient_ratio"] for row in rows), default=0.0),
        "maximum_color_delta": max((row["low_frequency_color_delta"] for row in rows), default=0.0),
        "maximum_grid_line_coverage": max((row["grid_line_coverage"] for row in rows), default=0.0),
        "center_ring_salience": {
            "status": center_ring_status,
            "center_ring_segment_count": len(center_ring_rows),
            "comparison_segment_count": len(comparison_rows),
            "center_ring_median": round(center_ring_median, 8) if center_ring_median is not None else None,
            "comparison_median": round(comparison_median, 8) if comparison_median is not None else None,
            "ratio": round(center_ring_ratio, 6) if center_ring_ratio is not None else None,
            "warn_threshold": float(thresholds["perceptual_center_ring_ratio_warn"]),
            "fail_threshold": float(thresholds["perceptual_center_ring_ratio_fail"]),
        },
        "segments": rows,
        "note": "Mesures sur reconstruction floutee; elles signalent les ruptures basses frequences sans remplacer la revue humaine.",
    }


def _motif_repetition_analysis(
    runtimes: list[TileRuntime],
    thresholds: dict[str, Any],
) -> dict[str, Any]:
    patches: list[dict[str, Any]] = []
    for runtime in runtimes:
        if runtime.image is None:
            continue
        image = runtime.image.convert("RGB")
        patch_width = max(1, image.width // 4)
        patch_height = max(1, image.height // 4)
        for patch_y in range(4):
            for patch_x in range(4):
                crop = image.crop(
                    (
                        patch_x * patch_width,
                        patch_y * patch_height,
                        image.width if patch_x == 3 else (patch_x + 1) * patch_width,
                        image.height if patch_y == 3 else (patch_y + 1) * patch_height,
                    )
                )
                sample = np.asarray(crop.resize((24, 24), Image.Resampling.LANCZOS), dtype=np.float32) / 255.0
                gray = 0.2126 * sample[:, :, 0] + 0.7152 * sample[:, :, 1] + 0.0722 * sample[:, :, 2]
                centered = gray - float(np.mean(gray))
                norm = float(np.linalg.norm(centered))
                vector = centered.flatten() / max(norm, 1.0e-8)
                horizontal = np.fliplr(centered)
                vertical = np.flipud(centered)
                patches.append(
                    {
                        "tile": runtime.spec.tile_id,
                        "patch_x": patch_x,
                        "patch_y": patch_y,
                        "exact_sha256": hashlib.sha256(crop.tobytes()).hexdigest(),
                        "horizontal_mirror_sha256": hashlib.sha256(ImageOps.mirror(crop).tobytes()).hexdigest(),
                        "vertical_mirror_sha256": hashlib.sha256(ImageOps.flip(crop).tobytes()).hexdigest(),
                        "mean_rgb": np.mean(sample, axis=(0, 1)),
                        "vector": vector,
                        "horizontal_mirror_vector": horizontal.flatten() / max(norm, 1.0e-8),
                        "vertical_mirror_vector": vertical.flatten() / max(norm, 1.0e-8),
                    }
                )
    exact: list[dict[str, Any]] = []
    suspicious: list[dict[str, Any]] = []
    exact_mirrors: list[dict[str, Any]] = []
    suspicious_mirrors: list[dict[str, Any]] = []
    correlation_threshold = float(thresholds["perceptual_motif_correlation_warn"])
    color_threshold = float(thresholds["perceptual_motif_color_delta_warn"])
    for index, left in enumerate(patches):
        for right in patches[index + 1 :]:
            if left["tile"] == right["tile"]:
                continue
            row = {
                "tile_a": left["tile"],
                "patch_a": {"x": left["patch_x"], "y": left["patch_y"]},
                "tile_b": right["tile"],
                "patch_b": {"x": right["patch_x"], "y": right["patch_y"]},
            }
            if left["exact_sha256"] == right["exact_sha256"]:
                exact.append(row)
                continue
            exact_mirror_axes = []
            if left["horizontal_mirror_sha256"] == right["exact_sha256"]:
                exact_mirror_axes.append("horizontal")
            if left["vertical_mirror_sha256"] == right["exact_sha256"]:
                exact_mirror_axes.append("vertical")
            if exact_mirror_axes:
                exact_mirrors.append({**row, "axes": exact_mirror_axes})
                continue
            correlation = float(np.dot(left["vector"], right["vector"]))
            color_delta = float(np.linalg.norm(left["mean_rgb"] - right["mean_rgb"]) / math.sqrt(3.0))
            if correlation >= correlation_threshold and color_delta <= color_threshold:
                row.update({"correlation": round(correlation, 6), "mean_color_delta": round(color_delta, 6)})
                suspicious.append(row)
            horizontal_correlation = float(np.dot(left["horizontal_mirror_vector"], right["vector"]))
            vertical_correlation = float(np.dot(left["vertical_mirror_vector"], right["vector"]))
            mirror_correlation = max(horizontal_correlation, vertical_correlation)
            if mirror_correlation >= correlation_threshold and color_delta <= color_threshold:
                suspicious_mirrors.append(
                    {
                        **row,
                        "axis": "horizontal" if horizontal_correlation >= vertical_correlation else "vertical",
                        "correlation": round(mirror_correlation, 6),
                        "mean_color_delta": round(color_delta, 6),
                    }
                )
    suspicious.sort(key=lambda row: (-row["correlation"], row["mean_color_delta"]))
    suspicious_mirrors.sort(key=lambda row: (-row["correlation"], row["mean_color_delta"]))
    status = "FAIL" if exact or exact_mirrors else "WARN" if suspicious or suspicious_mirrors else "PASS"
    return {
        "status": status,
        "patch_count": len(patches),
        "patch_grid_per_tile": "4x4",
        "exact_copy_count": len(exact),
        "suspicious_similarity_count": len(suspicious),
        "exact_mirror_count": len(exact_mirrors),
        "suspicious_mirror_count": len(suspicious_mirrors),
        "exact_copies": exact[:100],
        "suspicious_similarities": suspicious[:100],
        "exact_mirrors": exact_mirrors[:100],
        "suspicious_mirrors": suspicious_mirrors[:100],
        "truncated": any(len(rows) > 100 for rows in (exact, suspicious, exact_mirrors, suspicious_mirrors)),
        "note": "Recherche perceptuelle de copies et miroirs; les candidats WARN exigent une inspection humaine.",
    }


def _inverse_metric_status(value: float, warn: float, fail: float) -> str:
    if value <= fail:
        return "FAIL"
    if value <= warn:
        return "WARN"
    return "PASS"


def _macro_pattern_analysis(
    reconstruction: Image.Image,
    grid: GridInfo,
    thresholds: dict[str, Any],
) -> dict[str, Any]:
    rgb = np.asarray(reconstruction.convert("RGB"), dtype=np.float32) / 255.0
    luma = 0.2126 * rgb[:, :, 0] + 0.7152 * rgb[:, :, 1] + 0.0722 * rgb[:, :, 2]
    cell_width = reconstruction.width / float(max(grid.columns, 1))
    cell_height = reconstruction.height / float(max(grid.rows, 1))
    cell_means = np.zeros((grid.rows, grid.columns), dtype=np.float32)
    for row in range(grid.rows):
        for column in range(grid.columns):
            x0 = int(round(column * cell_width))
            x1 = int(round((column + 1) * cell_width))
            y0 = int(round(row * cell_height))
            y1 = int(round((row + 1) * cell_height))
            cell_means[row, column] = float(np.mean(luma[y0:y1, x0:x1]))

    checker = np.fromfunction(lambda y, x: ((x + y) % 2) * 2 - 1, cell_means.shape, dtype=int).astype(np.float32)
    cell_y, cell_x = np.mgrid[0:grid.rows, 0:grid.columns]
    design = np.column_stack((np.ones(cell_means.size), cell_x.ravel(), cell_y.ravel()))
    coefficients, _, _, _ = np.linalg.lstsq(design, cell_means.ravel(), rcond=None)
    trend = (design @ coefficients).reshape(cell_means.shape)
    centered = cell_means - trend
    checker_denominator = float(np.linalg.norm(centered) * np.linalg.norm(checker))
    checker_correlation = abs(float(np.sum(centered * checker)) / max(checker_denominator, 1.0e-8))
    even = cell_means[np.fromfunction(lambda y, x: (x + y) % 2 == 0, cell_means.shape, dtype=int)]
    odd = cell_means[np.fromfunction(lambda y, x: (x + y) % 2 == 1, cell_means.shape, dtype=int)]
    checker_contrast = abs(float(np.mean(even) - np.mean(odd)))
    if checker_contrast < float(thresholds["perceptual_checker_contrast_min"]):
        checker_status = "PASS"
    else:
        checker_status = _metric_status(
            checker_correlation,
            float(thresholds["perceptual_checker_correlation_warn"]),
            float(thresholds["perceptual_checker_correlation_fail"]),
        )

    dx = np.abs(np.diff(luma, axis=1, prepend=luma[:, :1]))
    dy = np.abs(np.diff(luma, axis=0, prepend=luma[:1, :]))
    detail = dx + dy
    band = max(2, int(round(min(cell_width, cell_height) * 0.08)))
    offset_x = max(band * 2, int(round(cell_width * 0.28)))
    offset_y = max(band * 2, int(round(cell_height * 0.28)))
    minimum_reference = float(thresholds["perceptual_blur_reference_detail_min"])
    blur_rows: list[dict[str, Any]] = []
    for column in range(1, grid.columns):
        x = int(round(column * cell_width))
        boundary = float(np.mean(detail[:, max(0, x - band):min(detail.shape[1], x + band)]))
        references = [
            float(np.mean(detail[:, max(0, candidate - band):min(detail.shape[1], candidate + band)]))
            for candidate in (x - offset_x, x + offset_x)
            if band <= candidate < detail.shape[1] - band
        ]
        reference = float(np.median(references)) if references else boundary
        ratio = boundary / max(reference, 1.0e-8)
        status = "PASS" if reference < minimum_reference else _inverse_metric_status(
            ratio,
            float(thresholds["perceptual_blur_detail_ratio_warn"]),
            float(thresholds["perceptual_blur_detail_ratio_fail"]),
        )
        blur_rows.append(
            {
                "id": f"vertical_x{column}",
                "orientation": "vertical",
                "status": status,
                "boundary_detail": round(boundary, 8),
                "reference_detail": round(reference, 8),
                "detail_ratio": round(ratio, 6),
            }
        )
    for row in range(1, grid.rows):
        y = int(round(row * cell_height))
        boundary = float(np.mean(detail[max(0, y - band):min(detail.shape[0], y + band), :]))
        references = [
            float(np.mean(detail[max(0, candidate - band):min(detail.shape[0], candidate + band), :]))
            for candidate in (y - offset_y, y + offset_y)
            if band <= candidate < detail.shape[0] - band
        ]
        reference = float(np.median(references)) if references else boundary
        ratio = boundary / max(reference, 1.0e-8)
        status = "PASS" if reference < minimum_reference else _inverse_metric_status(
            ratio,
            float(thresholds["perceptual_blur_detail_ratio_warn"]),
            float(thresholds["perceptual_blur_detail_ratio_fail"]),
        )
        blur_rows.append(
            {
                "id": f"horizontal_y{row}",
                "orientation": "horizontal",
                "status": status,
                "boundary_detail": round(boundary, 8),
                "reference_detail": round(reference, 8),
                "detail_ratio": round(ratio, 6),
            }
        )
    blur_status = _max_status(row["status"] for row in blur_rows)
    return {
        "status": _max_status((checker_status, blur_status)),
        "checkerboard": {
            "status": checker_status,
            "absolute_correlation": round(checker_correlation, 6),
            "parity_contrast": round(checker_contrast, 6),
            "detrended": True,
            "cell_luma_means": [[round(float(value), 6) for value in row] for row in cell_means],
        },
        "blurred_boundary_bands": {
            "status": blur_status,
            "band_width": band,
            "pass_count": sum(row["status"] == "PASS" for row in blur_rows),
            "warn_count": sum(row["status"] == "WARN" for row in blur_rows),
            "fail_count": sum(row["status"] == "FAIL" for row in blur_rows),
            "boundaries": blur_rows,
        },
        "note": (
            "Support automatique pour damier et bandes floues. Un score PASS ne remplace jamais "
            "la revue humaine des carres, anneaux, lignes globales et continuites naturelles."
        ),
    }


def _perceptual_artifacts(reconstruction: Image.Image, output_dir: Path) -> dict[str, Any]:
    source = reconstruction.convert("RGB")
    artifacts: dict[str, Any] = {}
    scales = (("100", 1.0), ("73", 0.73), ("50", 0.5), ("25", 0.25))
    previews: list[tuple[str, Image.Image]] = []
    for label, scale in scales:
        image = source.copy() if scale == 1.0 else source.resize(
            (max(1, int(round(source.width * scale))), max(1, int(round(source.height * scale)))),
            Image.Resampling.LANCZOS,
        )
        path = output_dir / f"perceptual_mosaic_{label}.png"
        image.save(path, optimize=True)
        artifacts[f"mosaic_{label}"] = {
            "file": path.name,
            "scale": scale,
            "width": image.width,
            "height": image.height,
            "sha256": _sha256(path),
            "debug_grid": False,
        }
        preview = image.copy()
        preview.thumbnail((720, 720), Image.Resampling.LANCZOS)
        previews.append((label, preview))
        image.close()

    contrasted = ImageEnhance.Contrast(ImageOps.autocontrast(source)).enhance(1.75)
    contrast_path = output_dir / "perceptual_contrast_enhanced.png"
    contrasted.save(contrast_path, optimize=True)
    artifacts["contrast_enhanced"] = {
        "file": contrast_path.name,
        "contrast_factor": 1.75,
        "width": contrasted.width,
        "height": contrasted.height,
        "sha256": _sha256(contrast_path),
        "debug_grid": False,
    }
    contrast_preview = contrasted.copy()
    contrast_preview.thumbnail((720, 720), Image.Resampling.LANCZOS)
    previews.append(("CONTRAST", contrast_preview))
    contrasted.close()

    horizontal_height = max(1, min(source.height, int(round(source.height * 0.28))))
    horizontal_top = max(0, (source.height - horizontal_height) // 2)
    horizontal = source.crop((0, horizontal_top, source.width, horizontal_top + horizontal_height))
    horizontal_path = output_dir / "perceptual_pan_horizontal.png"
    horizontal.save(horizontal_path, optimize=True)
    artifacts["pan_horizontal"] = {
        "file": horizontal_path.name,
        "width": horizontal.width,
        "height": horizontal.height,
        "sha256": _sha256(horizontal_path),
        "debug_grid": False,
    }
    horizontal.close()

    vertical_width = max(1, min(source.width, int(round(source.width * 0.28))))
    vertical_left = max(0, (source.width - vertical_width) // 2)
    vertical = source.crop((vertical_left, 0, vertical_left + vertical_width, source.height))
    vertical_path = output_dir / "perceptual_pan_vertical.png"
    vertical.save(vertical_path, optimize=True)
    artifacts["pan_vertical"] = {
        "file": vertical_path.name,
        "width": vertical.width,
        "height": vertical.height,
        "sha256": _sha256(vertical_path),
        "debug_grid": False,
    }
    vertical.close()

    margin = 24
    label_height = 28
    cell_width = max(preview.width for _, preview in previews) + margin * 2
    cell_height = max(preview.height for _, preview in previews) + margin * 2 + label_height
    sheet_rows = math.ceil(len(previews) / 2)
    sheet = Image.new("RGB", (cell_width * 2, cell_height * sheet_rows), (18, 21, 26))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (label, preview) in enumerate(previews):
        column = index % 2
        row = index // 2
        x0 = column * cell_width
        y0 = row * cell_height
        x = x0 + (cell_width - preview.width) // 2
        y = y0 + margin + label_height
        sheet.paste(preview, (x, y))
        draw.text((x0 + margin, y0 + margin), f"MOSAIC {label}% - NO DEBUG GRID", font=font, fill=(240, 243, 247))
        preview.close()
    sheet_path = output_dir / "perceptual_multiscale_sheet.png"
    sheet.save(sheet_path, optimize=True)
    artifacts["multiscale_sheet"] = {
        "file": sheet_path.name,
        "width": sheet.width,
        "height": sheet.height,
        "sha256": _sha256(sheet_path),
        "debug_grid": False,
    }
    sheet.close()
    source.close()
    return artifacts


def _perceptual_continuity_review(
    review_path: Path | None,
    output_dir: Path,
    required: bool,
    require_signature: bool,
    low_frequency: dict[str, Any],
    motifs: dict[str, Any],
    macro_patterns: dict[str, Any],
    seam_statistics: dict[str, Any],
) -> dict[str, Any]:
    template = {
        "schema": "bee-kingdom.world-map-perceptual-continuity-review.v2",
        "inspector": "",
        "inspected_at_utc": "",
        "signature": {
            "reviewer": "",
            "role": "Builder-C",
            "signed_at_utc": "",
            "decision": "NOT_REVIEWED",
        },
        "source_artifacts": [
            "perceptual_mosaic_100.png",
            "perceptual_mosaic_73.png",
            "perceptual_mosaic_50.png",
            "perceptual_mosaic_25.png",
            "perceptual_contrast_enhanced.png",
            "perceptual_pan_horizontal.png",
            "perceptual_pan_vertical.png",
        ],
        "categories": {
            key: {"status": "NOT_REVIEWED", "affected_boundaries": [], "note": label}
            for key, label in PERCEPTUAL_CONTINUITY_CATEGORIES.items()
        },
        "allowed_statuses": ["NO", "YES", "UNCERTAIN", "NOT_REVIEWED"],
        "decision_rule": "Toute limite perceptible, categorie YES ou incertitude bloque le PASS perceptuel.",
    }
    template_path = output_dir / "perceptual_continuity_review.template.json"
    template_path.write_text(json.dumps(template, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    result: dict[str, Any] = {
        "status": "FAIL" if require_signature else "WARN" if required else "PASS",
        "required": required,
        "signature_required": require_signature,
        "review_path": str(review_path.resolve()) if review_path else None,
        "template_file": template_path.name,
        "categories": {},
        "semantic_detection_automated": False,
        "automation_support": {
            "low_frequency_status": low_frequency["status"],
            "motif_repetition_status": motifs["status"],
            "macro_pattern_status": macro_patterns["status"],
            "technical_center_ring": seam_statistics.get("by_boundary_class", {}).get("center_ring"),
        },
    }
    if review_path is None:
        for key, label in PERCEPTUAL_CONTINUITY_CATEGORIES.items():
            result["categories"][key] = {"status": "NOT_REVIEWED", "label": label}
        if required:
            result["reason"] = "perceptual_continuity_review_missing"
        if require_signature:
            result.update(
                {
                    "signature_status": "FAIL",
                    "signature_issues": ["signed_review_missing"],
                    "human_gate_signed": False,
                    "grid_pattern_visible": "UNRESOLVED",
                }
            )
        return result

    resolved = review_path.resolve()
    if not resolved.is_file():
        result.update({"status": "FAIL", "reason": "perceptual_review_file_missing"})
        return result
    try:
        review = _read_json(resolved)
    except Exception as exc:
        result.update({"status": "FAIL", "reason": f"invalid_perceptual_review_json: {type(exc).__name__}: {exc}"})
        return result
    categories = review.get("categories")
    categories = categories if isinstance(categories, dict) else {}
    visible: list[str] = []
    unresolved: list[str] = []
    invalid: list[str] = []
    for key, label in PERCEPTUAL_CONTINUITY_CATEGORIES.items():
        entry = categories.get(key)
        if isinstance(entry, str):
            status = entry.upper()
            normalized = {"status": status, "label": label}
        elif isinstance(entry, dict):
            status = str(entry.get("status", "NOT_REVIEWED")).upper()
            normalized = dict(entry)
            normalized.update({"status": status, "label": label})
        else:
            status = "NOT_REVIEWED"
            normalized = {"status": status, "label": label}
        if status not in {"NO", "YES", "UNCERTAIN", "NOT_REVIEWED"}:
            invalid.append(key)
        elif status == "YES":
            visible.append(key)
        elif status != "NO":
            unresolved.append(key)
        result["categories"][key] = normalized

    signature = review.get("signature")
    signature = signature if isinstance(signature, dict) else {}
    reviewer = str(signature.get("reviewer", "")).strip()
    role = str(signature.get("role", "")).strip()
    signed_at_utc = str(signature.get("signed_at_utc", "")).strip()
    signed_decision = str(signature.get("decision", "")).strip().upper()
    signature_issues: list[str] = []
    if require_signature:
        if not reviewer:
            signature_issues.append("missing_reviewer")
        if role.upper() != "BUILDER-C":
            signature_issues.append("role_must_be_builder_c")
        if not signed_at_utc:
            signature_issues.append("missing_signed_at_utc")
        else:
            try:
                datetime.fromisoformat(signed_at_utc.replace("Z", "+00:00"))
            except ValueError:
                signature_issues.append("invalid_signed_at_utc")
        if signed_decision != "PASS":
            signature_issues.append("signed_decision_must_be_pass")

    automation_status = _max_status((low_frequency["status"], motifs["status"], macro_patterns["status"]))
    human_status = "FAIL" if visible or invalid else "WARN" if unresolved else "PASS"
    signature_status = "FAIL" if signature_issues else "PASS"
    result["status"] = _max_status((automation_status, human_status, signature_status))
    grid_category_keys = (
        "grid_lines_visible",
        "central_square_visible",
        "outer_ring_visible",
        "checkerboard_visible",
        "blurred_bands_visible",
    )
    grid_reviews = [result["categories"].get(key, {}).get("status") for key in grid_category_keys]
    grid_automation_status = _max_status((low_frequency["status"], macro_patterns["status"]))
    if "YES" in grid_reviews or grid_automation_status == "FAIL":
        grid_pattern_visible = "YES"
    elif all(status == "NO" for status in grid_reviews) and grid_automation_status == "PASS":
        grid_pattern_visible = "NO"
    else:
        grid_pattern_visible = "UNRESOLVED"
    result.update(
        {
            "human_review_status": human_status,
            "automation_status": automation_status,
            "signature_status": signature_status,
            "signature_issues": signature_issues,
            "signature": {
                "reviewer": reviewer or None,
                "role": role or None,
                "signed_at_utc": signed_at_utc or None,
                "decision": signed_decision or None,
            },
            "human_gate_signed": signature_status == "PASS" if require_signature else None,
            "grid_pattern_visible": grid_pattern_visible,
            "visible_categories": visible,
            "unresolved_categories": unresolved,
            "invalid_categories": invalid,
            "inspector": review.get("inspector"),
            "inspected_at_utc": review.get("inspected_at_utc"),
            "source_artifacts": review.get("source_artifacts"),
        }
    )
    return result


def _forbidden_content_review(
    review_path: Path | None,
    output_dir: Path,
    required: bool,
    manifest: dict[str, Any] | None,
    seam_statistics: dict[str, Any],
) -> dict[str, Any]:
    method = (manifest or {}).get("method")
    method = method if isinstance(method, dict) else {}
    template = {
        "schema": "bee-kingdom.world-map-forbidden-content-review.v1",
        "inspector": "",
        "inspected_at_utc": "",
        "source_artifact": "",
        "categories": {
            key: {"status": "NOT_REVIEWED", "affected_tiles": [], "note": label}
            for key, label in FORBIDDEN_CONTENT_CATEGORIES.items()
        },
        "allowed_statuses": ["ABSENT", "PRESENT", "UNCERTAIN", "NOT_REVIEWED"],
        "note": (
            "Revue visuelle humaine obligatoire. ABSENT signifie non observe dans le fond; "
            "PRESENT bloque le lot. Les fleurs, forets et reliefs naturels ne sont pas des marqueurs runtime."
        ),
    }
    template_path = output_dir / "forbidden_content_review.template.json"
    template_path.write_text(json.dumps(template, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

    result: dict[str, Any] = {
        "status": "WARN" if required else "PASS",
        "required": required,
        "review_path": str(review_path.resolve()) if review_path else None,
        "template_file": template_path.name,
        "semantic_detection_automated": False,
        "categories": {},
        "automation_support": {
            "painted_boundary_seam_failures": seam_statistics["fail_count"],
            "painted_boundary_seam_warnings": seam_statistics["warn_count"],
            "manifest_no_road_directive": (manifest or {}).get("noRoadDirective", method.get("noRoadDirective")),
        },
        "note": (
            "Les coutures et frontieres artificielles disposent de metriques pixel. "
            "Routes, ruches, ressources runtime, troupes et UI exigent une revue visuelle humaine."
        ),
    }
    if review_path is None:
        for key, label in FORBIDDEN_CONTENT_CATEGORIES.items():
            result["categories"][key] = {"status": "NOT_REVIEWED", "label": label}
        if required:
            result["reason"] = "forbidden_content_review_missing"
        return result

    resolved = review_path.resolve()
    if not resolved.is_file():
        result.update({"status": "FAIL", "reason": "forbidden_content_review_file_missing"})
        return result
    try:
        review = _read_json(resolved)
    except Exception as exc:
        result.update({"status": "FAIL", "reason": f"invalid_review_json: {type(exc).__name__}: {exc}"})
        return result

    categories = review.get("categories")
    categories = categories if isinstance(categories, dict) else {}
    present: list[str] = []
    unresolved: list[str] = []
    invalid: list[str] = []
    for key, label in FORBIDDEN_CONTENT_CATEGORIES.items():
        entry = categories.get(key)
        if isinstance(entry, str):
            status = entry.upper()
            normalized = {"status": status, "label": label}
        elif isinstance(entry, dict):
            status = str(entry.get("status", "NOT_REVIEWED")).upper()
            normalized = dict(entry)
            normalized["status"] = status
            normalized["label"] = label
        else:
            status = "NOT_REVIEWED"
            normalized = {"status": status, "label": label}
        if status not in {"ABSENT", "PRESENT", "UNCERTAIN", "NOT_REVIEWED"}:
            invalid.append(key)
        elif status == "PRESENT":
            present.append(key)
        elif status != "ABSENT":
            unresolved.append(key)
        result["categories"][key] = normalized

    if invalid or present:
        result["status"] = "FAIL"
    elif unresolved:
        result["status"] = "WARN" if required else "PASS"
    else:
        result["status"] = "PASS"
    result.update(
        {
            "inspector": review.get("inspector"),
            "inspected_at_utc": review.get("inspected_at_utc"),
            "source_artifact": review.get("source_artifact"),
            "present_categories": present,
            "unresolved_categories": unresolved,
            "invalid_categories": invalid,
        }
    )
    return result


def _placements(runtimes: list[TileRuntime], grid: GridInfo) -> tuple[dict[str, tuple[int, int]], int, int]:
    readable = [runtime for runtime in runtimes if runtime.image is not None]
    if not readable:
        return {}, 1, 1

    use_manifest_positions = all(
        runtime.spec.pixel_x is not None and runtime.spec.pixel_y is not None for runtime in readable
    )
    placements: dict[str, tuple[int, int]] = {}
    if use_manifest_positions:
        min_x = min(int(runtime.spec.pixel_x) for runtime in readable)
        min_y = min(int(runtime.spec.pixel_y) for runtime in readable)
        for runtime in readable:
            placements[runtime.spec.tile_id] = (
                int(runtime.spec.pixel_x) - min_x,
                int(runtime.spec.pixel_y) - min_y,
            )
    else:
        cell_width = max(runtime.image.width for runtime in readable if runtime.image)
        cell_height = max(runtime.image.height for runtime in readable if runtime.image)
        for runtime in readable:
            x = (int(runtime.spec.x) - grid.coordinate_origin_x) * cell_width
            y = (int(runtime.spec.y) - grid.coordinate_origin_y) * cell_height
            placements[runtime.spec.tile_id] = (x, y)

    width = max(
        placements[runtime.spec.tile_id][0] + runtime.image.width
        for runtime in readable
        if runtime.image
    )
    height = max(
        placements[runtime.spec.tile_id][1] + runtime.image.height
        for runtime in readable
        if runtime.image
    )
    return placements, width, height


def _reconstruct(
    runtimes: list[TileRuntime],
    grid: GridInfo,
    output_dir: Path,
    input_dir: Path,
    manifest: dict[str, Any] | None,
    thresholds: dict[str, Any],
    reference_atlas_path: Path | None = None,
) -> tuple[Image.Image, dict[str, Any], dict[str, tuple[int, int]]]:
    placements, width, height = _placements(runtimes, grid)
    pixel_count = width * height
    max_pixels = int(thresholds.get("max_reconstruction_pixels", 120_000_000))
    scale = 1.0
    if pixel_count > max_pixels:
        scale = math.sqrt(max_pixels / float(pixel_count))
    canvas_width = max(1, int(round(width * scale)))
    canvas_height = max(1, int(round(height * scale)))
    reconstruction = Image.new("RGBA", (canvas_width, canvas_height), (0, 0, 0, 0))

    for runtime in runtimes:
        if runtime.image is None:
            continue
        x, y = placements[runtime.spec.tile_id]
        tile = runtime.image
        if scale != 1.0:
            tile = tile.resize(
                (max(1, int(round(tile.width * scale))), max(1, int(round(tile.height * scale)))),
                Image.Resampling.LANCZOS,
            )
        reconstruction.alpha_composite(tile, (int(round(x * scale)), int(round(y * scale))))

    reconstruction_path = output_dir / "reconstruction.png"
    reconstruction.save(reconstruction_path, optimize=True)
    result: dict[str, Any] = {
        "status": "PASS" if scale == 1.0 else "WARN",
        "file": reconstruction_path.name,
        "logical_width": width,
        "logical_height": height,
        "output_width": canvas_width,
        "output_height": canvas_height,
        "scale": round(scale, 8),
        "full_resolution": scale == 1.0,
        "sha256": _sha256(reconstruction_path),
    }
    if scale != 1.0:
        result["note"] = "Reconstruction reduite pour respecter max_reconstruction_pixels."

    source_info = (manifest or {}).get("source")
    source_path_text = source_info.get("path") if isinstance(source_info, dict) else None
    auto_reference = False
    explicit_reference = reference_atlas_path is not None
    if explicit_reference:
        source_path_text = str(reference_atlas_path.resolve())
    elif not isinstance(source_path_text, str) or not source_path_text.strip():
        atlas_candidates = sorted(input_dir.glob("atlas_master*.png"))
        if len(atlas_candidates) == 1:
            source_path_text = str(atlas_candidates[0])
            auto_reference = True
    if isinstance(source_path_text, str) and source_path_text.strip():
        source_path = Path(source_path_text)
        if not source_path.is_absolute():
            source_path = (input_dir / source_path).resolve()
        result["source_comparison"] = {
            "path": str(source_path),
            "available": source_path.is_file(),
            "auto_detected_atlas_master": auto_reference,
            "explicit_reference": explicit_reference,
        }
        if explicit_reference and not source_path.is_file():
            result["status"] = "FAIL"
            result["source_comparison"]["error"] = "Atlas de reference explicite introuvable."
        if source_path.is_file() and scale == 1.0:
            try:
                with Image.open(source_path) as source_file:
                    source = source_file.convert("RGBA")
                same_dimensions = source.size == reconstruction.size
                identical = (
                    same_dimensions
                    and ImageChops.difference(source, reconstruction).convert("RGB").getbbox() is None
                    and np.array_equal(
                        np.asarray(source.getchannel("A"), dtype=np.uint8),
                        np.asarray(reconstruction.getchannel("A"), dtype=np.uint8),
                    )
                )
                source_comparison = result["source_comparison"]
                source_comparison.update(
                    {
                        "same_dimensions": same_dimensions,
                        "pixel_identical": identical,
                        "source_sha256": _sha256(source_path),
                    }
                )
                if same_dimensions and not identical:
                    source_sample = source.copy()
                    reconstruction_sample = reconstruction.copy()
                    source_sample.thumbnail((1024, 1024), Image.Resampling.BILINEAR)
                    reconstruction_sample = reconstruction_sample.resize(source_sample.size, Image.Resampling.BILINEAR)
                    source_arr = np.asarray(source_sample.convert("RGB"), dtype=np.float32)
                    reconstruction_arr = np.asarray(reconstruction_sample.convert("RGB"), dtype=np.float32)
                    source_comparison["pixel_mae"] = round(
                        float(np.mean(np.abs(source_arr - reconstruction_arr)) / 255.0), 8
                    )
                    result["status"] = "FAIL"
                elif not same_dimensions:
                    result["status"] = "WARN"
            except Exception as exc:
                result["status"] = "WARN"
                result["source_comparison"]["error"] = f"{type(exc).__name__}: {exc}"
    return reconstruction, result, placements


def _contact_sheet(
    runtimes: list[TileRuntime],
    grid: GridInfo,
    output_dir: Path,
    thresholds: dict[str, Any],
) -> dict[str, Any]:
    thumb_max = int(thresholds["contact_thumb_max"])
    cell_width = thumb_max + 24
    cell_height = thumb_max + 58
    columns = max(1, grid.columns)
    rows = max(grid.rows, math.ceil(max(len(runtimes), 1) / columns))
    sheet = Image.new("RGB", (columns * cell_width, rows * cell_height), (24, 27, 31))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    colors = {"PASS": (56, 188, 114), "WARN": (242, 184, 69), "FAIL": (230, 76, 76)}

    ordered = sorted(runtimes, key=lambda runtime: (runtime.spec.y or 0, runtime.spec.x or 0, runtime.spec.tile_id))
    for index, runtime in enumerate(ordered):
        x_index = int(runtime.spec.x) - grid.coordinate_origin_x if runtime.spec.x is not None else index % columns
        y_index = int(runtime.spec.y) - grid.coordinate_origin_y if runtime.spec.y is not None else index // columns
        if x_index < 0 or x_index >= columns or y_index < 0:
            x_index = index % columns
            y_index = index // columns
        x0 = x_index * cell_width
        y0 = y_index * cell_height
        status = runtime.analysis.get("status", "FAIL")
        draw.rectangle((x0 + 4, y0 + 4, x0 + cell_width - 5, y0 + cell_height - 5), outline=colors[status], width=3)
        if runtime.image is not None:
            thumb = runtime.image.convert("RGB").copy()
            thumb.thumbnail((thumb_max, thumb_max), Image.Resampling.LANCZOS)
            px = x0 + (cell_width - thumb.width) // 2
            py = y0 + 12 + (thumb_max - thumb.height) // 2
            sheet.paste(thumb, (px, py))
        else:
            draw.rectangle((x0 + 12, y0 + 12, x0 + cell_width - 13, y0 + thumb_max), fill=(58, 38, 38))
            draw.text((x0 + 22, y0 + 30), "IMAGE MANQUANTE", font=font, fill=(255, 210, 210))
        label = f"({runtime.spec.x},{runtime.spec.y}) {runtime.spec.tile_id}"
        if len(label) > 42:
            label = label[:39] + "..."
        draw.text((x0 + 12, y0 + thumb_max + 18), label, font=font, fill=(235, 235, 235))
        draw.text((x0 + 12, y0 + thumb_max + 34), status, font=font, fill=colors[status])

    path = output_dir / "contact_sheet.png"
    sheet.save(path, optimize=True)
    return {"file": path.name, "width": sheet.width, "height": sheet.height, "sha256": _sha256(path)}


def _seam_heatmap(
    reconstruction: Image.Image,
    placements: dict[str, tuple[int, int]],
    runtimes: list[TileRuntime],
    seams: list[dict[str, Any]],
    output_dir: Path,
) -> dict[str, Any]:
    preview = reconstruction.convert("RGB").copy()
    preview.thumbnail((1800, 1800), Image.Resampling.LANCZOS)
    scale_x = preview.width / max(reconstruction.width, 1)
    scale_y = preview.height / max(reconstruction.height, 1)
    shade = Image.new("RGBA", preview.size, (0, 0, 0, 72))
    preview = Image.alpha_composite(preview.convert("RGBA"), shade)
    draw = ImageDraw.Draw(preview)
    font = ImageFont.load_default()
    runtime_by_id = {runtime.spec.tile_id: runtime for runtime in runtimes}
    colors = {"PASS": (50, 210, 115, 220), "WARN": (255, 190, 55, 235), "FAIL": (255, 65, 65, 245)}

    for seam in seams:
        tile_a = runtime_by_id[seam["tile_a"]]
        if tile_a.image is None or seam["tile_a"] not in placements:
            continue
        x, y = placements[seam["tile_a"]]
        color = colors[seam["status"]]
        line_width = 3 if seam["status"] == "PASS" else 7 if seam["status"] == "WARN" else 11
        if seam["direction"] == "E":
            px = int(round((x + tile_a.image.width) * scale_x))
            y1 = int(round(y * scale_y))
            y2 = int(round((y + tile_a.image.height) * scale_y))
            draw.line((px, y1, px, y2), fill=color, width=line_width)
            draw.text((px + 4, (y1 + y2) // 2), f"{seam['score']:.3f}", font=font, fill=color)
        else:
            py = int(round((y + tile_a.image.height) * scale_y))
            x1 = int(round(x * scale_x))
            x2 = int(round((x + tile_a.image.width) * scale_x))
            draw.line((x1, py, x2, py), fill=color, width=line_width)
            draw.text(((x1 + x2) // 2, py + 4), f"{seam['score']:.3f}", font=font, fill=color)

    legend = (("PASS", colors["PASS"]), ("WARN", colors["WARN"]), ("FAIL", colors["FAIL"]))
    legend_x = 14
    for index, (label, color) in enumerate(legend):
        y = 14 + index * 22
        draw.rectangle((legend_x, y, legend_x + 14, y + 14), fill=color)
        draw.text((legend_x + 21, y + 2), label, font=font, fill=(255, 255, 255, 255))

    path = output_dir / "seam_heatmap.png"
    preview.save(path, optimize=True)
    return {"file": path.name, "width": preview.width, "height": preview.height, "sha256": _sha256(path)}


def _top_risk_artifact(
    reconstruction: Image.Image,
    placements: dict[str, tuple[int, int]],
    runtimes: list[TileRuntime],
    seams: list[dict[str, Any]],
    low_frequency: dict[str, Any],
    macro_patterns: dict[str, Any],
    motifs: dict[str, Any],
    output_dir: Path,
) -> dict[str, Any]:
    risks: list[dict[str, Any]] = []
    for seam in seams:
        risks.append(
            {
                "kind": "canonical_boundary",
                "id": seam["id"],
                "status": seam["status"],
                "risk_value": float(seam["score"]),
                "tile_a": seam["tile_a"],
                "tile_b": seam["tile_b"],
                "direction": seam["direction"],
            }
        )
    for segment in low_frequency.get("segments", []):
        if segment.get("status") != "PASS":
            risks.append(
                {
                    "kind": "low_frequency_boundary",
                    "id": segment["seam"],
                    "status": segment["status"],
                    "risk_value": max(
                        float(segment.get("gradient_ratio", 0.0)) / 3.0,
                        float(segment.get("low_frequency_color_delta", 0.0)) / 0.16,
                        float(segment.get("grid_line_coverage", 0.0)) / 0.5,
                    ),
                }
            )
    checker = macro_patterns.get("checkerboard", {})
    if checker.get("status") != "PASS":
        risks.append(
            {
                "kind": "checkerboard",
                "id": "macro_checkerboard",
                "status": checker.get("status", "WARN"),
                "risk_value": float(checker.get("absolute_correlation", 0.0)),
            }
        )
    for boundary in macro_patterns.get("blurred_boundary_bands", {}).get("boundaries", []):
        if boundary.get("status") != "PASS":
            ratio = float(boundary.get("detail_ratio", 1.0))
            risks.append(
                {
                    "kind": "blurred_boundary_band",
                    "id": boundary["id"],
                    "status": boundary["status"],
                    "risk_value": max(0.0, 1.0 - ratio),
                }
            )
    for row in motifs.get("exact_mirrors", [])[:20]:
        risks.append({"kind": "exact_mirror", "id": f"{row['tile_a']}~{row['tile_b']}", "status": "FAIL", "risk_value": 1.0})
    for row in motifs.get("exact_copies", [])[:20]:
        risks.append({"kind": "exact_copy", "id": f"{row['tile_a']}={row['tile_b']}", "status": "FAIL", "risk_value": 1.0})
    risks.sort(key=lambda row: (-STATUS_RANK.get(row["status"], 1), -float(row["risk_value"]), row["id"]))

    runtime_by_id = {runtime.spec.tile_id: runtime for runtime in runtimes}
    seam_by_id = {seam["id"]: seam for seam in seams}
    crop_entries: list[tuple[dict[str, Any], Image.Image]] = []
    for risk in risks:
        if risk["kind"] != "canonical_boundary" or risk["id"] not in seam_by_id:
            continue
        seam = seam_by_id[risk["id"]]
        runtime = runtime_by_id.get(seam["tile_a"])
        if runtime is None or runtime.image is None or runtime.spec.tile_id not in placements:
            continue
        x, y = placements[runtime.spec.tile_id]
        half = max(8, min(runtime.image.width, runtime.image.height) // 6)
        if seam["direction"] == "E":
            center_x = x + runtime.image.width
            box = (center_x - half, y, center_x + half, y + runtime.image.height)
        else:
            center_y = y + runtime.image.height
            box = (x, center_y - half, x + runtime.image.width, center_y + half)
        box = (
            max(0, box[0]),
            max(0, box[1]),
            min(reconstruction.width, box[2]),
            min(reconstruction.height, box[3]),
        )
        crop_entries.append((risk, reconstruction.convert("RGB").crop(box)))
        if len(crop_entries) == 8:
            break

    cell_width = 360
    cell_height = 250
    rows = max(1, math.ceil(max(len(crop_entries), 1) / 2))
    sheet = Image.new("RGB", (cell_width * 2, cell_height * rows), (18, 21, 26))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    if not crop_entries:
        draw.text((24, 24), "NO CANONICAL BOUNDARY RISK CROP", fill=(230, 234, 240), font=font)
    for index, (risk, crop) in enumerate(crop_entries):
        crop.thumbnail((cell_width - 20, cell_height - 56), Image.Resampling.LANCZOS)
        x0 = (index % 2) * cell_width
        y0 = (index // 2) * cell_height
        sheet.paste(crop, (x0 + (cell_width - crop.width) // 2, y0 + 42))
        draw.text((x0 + 10, y0 + 8), f"{risk['status']} {risk['id']}", fill=(240, 243, 247), font=font)
        draw.text((x0 + 10, y0 + 24), f"risk={risk['risk_value']:.4f}", fill=(190, 198, 210), font=font)
        crop.close()
    path = output_dir / "top_risks.png"
    sheet.save(path, optimize=True)
    artifact = {"file": path.name, "width": sheet.width, "height": sheet.height, "sha256": _sha256(path), "debug_grid": True}
    sheet.close()
    return {"risks": risks[:50], "risk_count": len(risks), "artifact": artifact}


def _qa_grid(
    reconstruction: Image.Image,
    placements: dict[str, tuple[int, int]],
    runtimes: list[TileRuntime],
    grid: GridInfo,
    forbidden_review: dict[str, Any],
    output_dir: Path,
    profile: str | None = None,
) -> dict[str, Any]:
    preview = reconstruction.convert("RGB").copy()
    preview.thumbnail((1600, 1600), Image.Resampling.LANCZOS)
    scale_x = preview.width / max(reconstruction.width, 1)
    scale_y = preview.height / max(reconstruction.height, 1)
    panel_width = 360
    canvas = Image.new("RGB", (preview.width + panel_width, preview.height), (20, 23, 28))
    canvas.paste(preview, (0, 0))
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    tile_status_colors = {"PASS": (65, 220, 125), "WARN": (255, 190, 55), "FAIL": (255, 72, 72)}
    center_color = (80, 225, 235)
    ring_color = (104, 156, 255)
    min_x = grid.coordinate_origin_x
    min_y = grid.coordinate_origin_y
    max_x = min_x + grid.columns - 1
    max_y = min_y + grid.rows - 1

    for runtime in runtimes:
        if runtime.image is None or runtime.spec.tile_id not in placements:
            continue
        x, y = placements[runtime.spec.tile_id]
        x0 = int(round(x * scale_x))
        y0 = int(round(y * scale_y))
        x1 = int(round((x + runtime.image.width) * scale_x)) - 1
        y1 = int(round((y + runtime.image.height) * scale_y)) - 1
        is_ring = runtime.spec.x in {min_x, max_x} or runtime.spec.y in {min_y, max_y}
        role_color = center_color if profile == "wave3-continuous-5x5" else ring_color if is_ring else center_color
        status = runtime.analysis.get("status", "FAIL")
        color = role_color if status == "PASS" else tile_status_colors.get(status, tile_status_colors["FAIL"])
        draw.rectangle((x0, y0, x1, y1), outline=(0, 0, 0), width=5)
        draw.rectangle((x0 + 2, y0 + 2, x1 - 2, y1 - 2), outline=color, width=3)
        role = "CANONICAL SLICE" if profile == "wave3-continuous-5x5" else "RING" if is_ring else "CENTER LOCK"
        label = f"{runtime.spec.x},{runtime.spec.y} {role} {status}"
        label_box = draw.textbbox((0, 0), label, font=font)
        label_width = label_box[2] - label_box[0]
        draw.rectangle((x0 + 6, y0 + 6, x0 + 12 + label_width, y0 + 24), fill=(0, 0, 0))
        draw.text((x0 + 9, y0 + 9), label, font=font, fill=color)

    panel_x = preview.width + 18
    draw.text((panel_x, 16), "QA GRID - WORLD MAP 5x5", font=font, fill=(255, 255, 255))
    if profile == "wave3-continuous-5x5":
        draw.text((panel_x, 42), "Cyan: slice du master continu Wave3", font=font, fill=center_color)
        draw.text((panel_x, 62), "Aucun verrou de hash Wave1", font=font, fill=(210, 214, 222))
    else:
        draw.text((panel_x, 42), "Cyan: centre Wave1 verrouille", font=font, fill=center_color)
        draw.text((panel_x, 62), "Bleu: nouvel anneau Wave2", font=font, fill=ring_color)
    draw.text((panel_x, 92), "CONTENU INTERDIT", font=font, fill=(255, 255, 255))
    y_cursor = 116
    review_colors = {
        "ABSENT": (65, 220, 125),
        "PRESENT": (255, 72, 72),
        "UNCERTAIN": (255, 190, 55),
        "NOT_REVIEWED": (170, 176, 187),
    }
    categories = forbidden_review.get("categories", {})
    for key, label in FORBIDDEN_CONTENT_CATEGORIES.items():
        entry = categories.get(key, {})
        status = str(entry.get("status", "NOT_REVIEWED")) if isinstance(entry, dict) else str(entry)
        status = status.upper()
        draw.text((panel_x, y_cursor), f"{status}: {label}", font=font, fill=review_colors.get(status, (255, 190, 55)))
        y_cursor += 38
    draw.text(
        (panel_x, y_cursor + 12),
        "Semantique: revue humaine,\npas de claim de detection IA.",
        font=font,
        fill=(210, 214, 222),
        spacing=5,
    )

    path = output_dir / "qa_grid.png"
    canvas.save(path, optimize=True)
    return {"file": path.name, "width": canvas.width, "height": canvas.height, "sha256": _sha256(path)}


def _dimension_check(
    runtimes: list[TileRuntime],
    manifest: dict[str, Any] | None,
    thresholds: dict[str, Any],
) -> dict[str, Any]:
    readable = [runtime for runtime in runtimes if runtime.image is not None]
    sizes = sorted({(runtime.image.width, runtime.image.height) for runtime in readable if runtime.image})
    expected_matches = all(
        (
            runtime.spec.expected_width is not None
            and runtime.spec.expected_height is not None
            and runtime.image is not None
            and runtime.image.size == (runtime.spec.expected_width, runtime.spec.expected_height)
        )
        for runtime in readable
    ) if readable else False
    tile_settings = (manifest or {}).get("tile_settings")
    edge_mode = tile_settings.get("edge_mode") if isinstance(tile_settings, dict) else None

    if not readable:
        status = "FAIL"
        message = "Aucune image lisible pour verifier les dimensions."
    elif len(sizes) == 1:
        status = "PASS"
        message = "Dimensions uniformes."
    elif (
        bool(thresholds.get("allow_manifest_edge_dimensions"))
        and edge_mode == "actual"
        and expected_matches
    ):
        status = "PASS"
        message = "Dimensions variables conformes aux tuiles de bord declarees en mode actual."
    elif bool(thresholds.get("uniform_dimensions_required")):
        status = "FAIL"
        message = "Dimensions non uniformes sans exception de bord valide."
    else:
        status = "WARN"
        message = "Dimensions non uniformes."
    return _check(
        "dimensions",
        status,
        message,
        sizes=[{"width": width, "height": height} for width, height in sizes],
        manifest_edge_mode=edge_mode,
        all_manifest_dimensions_match=expected_matches,
    )


def _write_markdown(report: dict[str, Any], path: Path) -> None:
    lines: list[str] = []
    lines.append("# Validation de contenu carte mondiale")
    lines.append("")
    lines.append(f"- **Lot:** `{report['run']['label']}`")
    lines.append(f"- **Verdict:** **{report['overall_status']}**")
    lines.append(f"- **Entree:** `{report['run']['input_dir']}`")
    lines.append(f"- **Manifest:** `{report['run']['manifest_path'] or 'absent'}`")
    lines.append(f"- **Execution UTC:** `{report['run']['generated_at_utc']}`")
    lines.append("")
    lines.append(
        "> Ce validateur inspecte uniquement les images et leurs metadonnees. "
        "Il ne contient aucun routage, pathfinding ou algorithme de deplacement."
    )
    lines.append("")
    lines.append("## Synthese")
    lines.append("")
    summary = report["summary"]
    lines.append(f"- Images: **{summary['actual_count']} / {summary['expected_count']}**")
    lines.append(f"- Images lisibles: **{summary['readable_count']}**")
    lines.append(f"- Raccords controles: **{summary['seam_count']}**")
    lines.append(
        f"- Raccords PASS / WARN / FAIL: **{summary['seams_pass']} / "
        f"{summary['seams_warn']} / {summary['seams_fail']}**"
    )
    lines.append(f"- Doublons exacts: **{summary['exact_duplicate_count']}**")
    lines.append(f"- Quasi-doublons: **{summary['quasi_duplicate_count']}**")
    seam_statistics = report.get("seam_statistics") or {}
    if seam_statistics:
        lines.append(
            f"- Coutures attendues / controlees: **{seam_statistics['expected_count']} / "
            f"{seam_statistics['actual_count']}**"
        )
    ring = report.get("ring")
    if ring:
        lines.append(
            f"- Anneau Wave2: **{ring['actual_ring_count']} / {ring['expected_ring_count']}** nouvelles tuiles"
        )
    center_lock = report.get("center_lock")
    if center_lock:
        lines.append(f"- Centre Wave1 verrouille: **{center_lock['match_count']} / 9 hashes**")
    master_contract = report.get("master_contract") or {}
    if master_contract.get("required"):
        dimensions = master_contract.get("actual_dimensions") or {}
        lines.append(
            f"- Master continu: **{master_contract.get('status')}** "
            f"({dimensions.get('width')}x{dimensions.get('height')})"
        )
    runtime_gutters = report.get("runtime_gutters") or {}
    if runtime_gutters.get("required"):
        lines.append(
            f"- Gutters runtime: **{runtime_gutters.get('pass_count', 0)} / "
            f"{runtime_gutters.get('validated_count', 0)}**, frontieres "
            f"**{runtime_gutters.get('boundary_pass_count', 0)} / {runtime_gutters.get('boundary_count', 0)}**"
        )
    continuity_gates = report.get("continuity_gates") or {}
    if continuity_gates:
        lines.append(
            f"- Continuite technique: **{continuity_gates['technical_continuity']['status']}**"
        )
        lines.append(
            f"- Continuite perceptuelle: **{continuity_gates['perceptual_continuity']['status']}**"
        )
        lines.append(f"- Motif de grille visible: **{continuity_gates['grid_pattern_visible']}**")
    lines.append("")
    lines.append("## Controles")
    lines.append("")
    lines.append("| Controle | Statut | Detail |")
    lines.append("|---|---:|---|")
    for check in report["checks"]:
        message = str(check["message"]).replace("|", "\\|")
        lines.append(f"| `{check['id']}` | **{check['status']}** | {message} |")

    if center_lock:
        lines.append("")
        lines.append("## Verrouillage centre Wave1")
        lines.append("")
        lines.append(f"Statut: **{center_lock['status']}**")
        lines.append("")
        lines.append("| Baseline | Cible 5x5 | Concordance |")
        lines.append("|---|---|---:|")
        for row in center_lock.get("matches", []):
            lines.append(
                f"| `{row['baseline_tile']}` | `{row['target_tile']}` | **SHA-256 identique** |"
            )
        for row in center_lock.get("mismatches", []):
            target = row.get("target_tile", "absente")
            lines.append(f"| `{row['baseline_tile']}` | `{target}` | **FAIL: {row.get('reason')}** |")

    forbidden_review = report.get("forbidden_content_review")
    if forbidden_review:
        lines.append("")
        lines.append("## Contenu interdit")
        lines.append("")
        lines.append(
            "> Les objets semantiques sont issus d'une revue visuelle humaine; le validateur ne pretend pas "
            "reconnaitre automatiquement routes, ruches, ressources, troupes ou UI."
        )
        lines.append("")
        lines.append("| Categorie | Statut | Note |")
        lines.append("|---|---:|---|")
        for key, label in FORBIDDEN_CONTENT_CATEGORIES.items():
            entry = forbidden_review.get("categories", {}).get(key, {})
            status = entry.get("status", "NOT_REVIEWED") if isinstance(entry, dict) else entry
            note = entry.get("note", "") if isinstance(entry, dict) else ""
            lines.append(f"| {label} | **{status}** | {str(note).replace('|', chr(92) + '|')} |")

    perceptual_review = report.get("perceptual_continuity_review")
    if perceptual_review:
        lines.append("")
        lines.append("## Continuite perceptuelle multi-echelle")
        lines.append("")
        lines.append(
            "> Revue sur mosaique propre a 100 %, 73 %, 50 %, 25 %, bandes de pan et contraste renforce. "
            "Toute limite perceptible bloque le PASS, meme si les coutures techniques passent."
        )
        lines.append("")
        low_frequency = report.get("low_frequency_grid") or {}
        motifs = report.get("motif_repetition") or {}
        macro_patterns = report.get("macro_patterns") or {}
        lines.append(f"- Mesures basse frequence: **{low_frequency.get('status', 'n/a')}**")
        center_ring_salience = low_frequency.get("center_ring_salience") or {}
        lines.append(
            "- Saillance centre/anneau: "
            f"**{center_ring_salience.get('status', 'n/a')}** "
            f"(ratio {center_ring_salience.get('ratio', 'n/a')})"
        )
        lines.append(f"- Recherche de motifs: **{motifs.get('status', 'n/a')}**")
        lines.append(f"- Damier / bandes floues: **{macro_patterns.get('status', 'n/a')}**")
        lines.append(f"- Revue humaine: **{perceptual_review.get('human_review_status', 'n/a')}**")
        if perceptual_review.get("signature_required"):
            lines.append(f"- Signature Builder-C: **{perceptual_review.get('signature_status', 'n/a')}**")
        lines.append(f"- Grille perceptible: **{perceptual_review.get('grid_pattern_visible', 'UNRESOLVED')}**")
        lines.append("")
        lines.append("| Categorie | Statut | Note |")
        lines.append("|---|---:|---|")
        for key, label in PERCEPTUAL_CONTINUITY_CATEGORIES.items():
            entry = perceptual_review.get("categories", {}).get(key, {})
            status = entry.get("status", "NOT_REVIEWED") if isinstance(entry, dict) else entry
            note = entry.get("note", "") if isinstance(entry, dict) else ""
            lines.append(f"| {label} | **{status}** | {str(note).replace('|', chr(92) + '|')} |")

    lines.append("")
    lines.append("## Inventaire")
    lines.append("")
    lines.append("| Coord. | Image | Dimensions | Mode | Statut | Noir | Transparent | SHA-256 |")
    lines.append("|---:|---|---:|---:|---:|---:|---:|---|")
    for tile in report["tiles"]:
        grid = tile.get("grid") or {}
        coord = f"{grid.get('x')},{grid.get('y')}"
        dimensions = (
            f"{tile.get('width')}x{tile.get('height')}" if tile.get("width") is not None else "n/a"
        )
        sha = tile.get("sha256", "n/a")
        if sha != "n/a":
            sha = sha[:12] + "..."
        lines.append(
            f"| `{coord}` | `{tile['file']}` | {dimensions} | {tile.get('mode', 'n/a')} | "
            f"**{tile['status']}** | {tile.get('black_ratio', 0):.3f} | "
            f"{tile.get('transparent_ratio', 0):.3f} | `{sha}` |"
        )

    lines.append("")
    lines.append("## Raccords adjacents")
    lines.append("")
    if report["seams"]:
        lines.append("| A | Direction | B | Statut | Score | Pixel MAE | Couleur | Structure | Ratio discontinuite |")
        lines.append("|---|---:|---|---:|---:|---:|---:|---:|---:|")
        for seam in report["seams"]:
            lines.append(
                f"| `{seam['tile_a']}` | {seam['direction']} | `{seam['tile_b']}` | "
                f"**{seam['status']}** | {seam['score']:.4f} | {seam['pixel_mae']:.4f} | "
                f"{seam['mean_color_delta']:.4f} | {seam['structure_difference']:.4f} | "
                f"{seam['discontinuity_ratio']:.3f} |"
            )
    else:
        lines.append("Aucun raccord adjacent exploitable.")

    if seam_statistics.get("by_boundary_class"):
        lines.append("")
        lines.append("### Statistiques par classe de raccord")
        lines.append("")
        lines.append("| Classe | Total | PASS | WARN | FAIL | Score min. | Score moyen | Score max. |")
        lines.append("|---|---:|---:|---:|---:|---:|---:|---:|")
        labels = {
            "center_center": "centre-centre",
            "center_ring": "centre-anneau",
            "ring_ring": "anneau-anneau",
        }
        for key, values in seam_statistics["by_boundary_class"].items():
            lines.append(
                f"| {labels.get(key, key)} | {values['count']} | {values['pass_count']} | "
                f"{values['warn_count']} | {values['fail_count']} | {values['minimum_score'] or 0:.4f} | "
                f"{values['mean_score'] or 0:.4f} | {values['maximum_score'] or 0:.4f} |"
            )

    lines.append("")
    lines.append("## Repetitions")
    lines.append("")
    duplicates = report["duplicates"]
    if not duplicates["exact"] and not duplicates["quasi"]:
        lines.append("Aucun doublon exact ou quasi-doublon detecte avec les seuils actifs.")
    for row in duplicates["exact"]:
        lines.append(f"- FAIL exact: `{row['tile_a']}` = `{row['tile_b']}`")
    for row in duplicates["quasi"]:
        lines.append(
            f"- WARN quasi: `{row['tile_a']}` ~ `{row['tile_b']}` "
            f"(Hamming {row['dhash_hamming_distance']}, couleur {row['mean_color_delta']:.4f})"
        )
    motifs = report.get("motif_repetition") or {}
    lines.append(f"- Copies exactes de patches: **{motifs.get('exact_copy_count', 0)}**")
    lines.append(f"- Miroirs exacts de patches: **{motifs.get('exact_mirror_count', 0)}**")

    top_risks = report.get("top_risks") or []
    if top_risks:
        lines.append("")
        lines.append("## Risques prioritaires")
        lines.append("")
        lines.append("| Type | ID | Statut | Valeur |")
        lines.append("|---|---|---:|---:|")
        for risk in top_risks[:12]:
            lines.append(
                f"| `{risk.get('kind')}` | `{risk.get('id')}` | **{risk.get('status')}** | "
                f"{float(risk.get('risk_value', 0.0)):.4f} |"
            )

    lines.append("")
    lines.append("## Budget")
    lines.append("")
    lines.append(f"Statut budget: **{report['memory_budget']['status']}**")
    lines.append("")
    lines.append("| Estimation | MiB |")
    lines.append("|---|---:|")
    for key, value in report["memory_budget"]["values_mb"].items():
        lines.append(f"| `{key}` | {value:.3f} |")
    lines.append("")
    lines.append(report["memory_budget"]["note"])

    lines.append("")
    lines.append("## Artefacts")
    lines.append("")
    for key, artifact in report["artifacts"].items():
        if isinstance(artifact, dict) and artifact.get("file"):
            lines.append(f"- `{key}`: [{artifact['file']}]({artifact['file']})")
    lines.append("- `validation_json`: [validation.json](validation.json)")
    lines.append("- `rapport_markdown`: [report.md](report.md)")
    lines.append("")
    lines.append("## Seuils actifs")
    lines.append("")
    lines.append("```json")
    lines.append(json.dumps(report["thresholds"], indent=2, ensure_ascii=False))
    lines.append("```")
    lines.append("")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def validate_content(options: ValidationOptions) -> dict[str, Any]:
    input_dir = options.input_dir.resolve()
    output_dir = options.output_dir.resolve()
    if not input_dir.is_dir():
        raise NotADirectoryError(f"Dossier d'entree introuvable: {input_dir}")
    if options.profile == "wave2-5x5":
        contract = {
            "expected_count": (options.expected_count, 25),
            "columns": (options.columns, 5),
            "rows": (options.rows, 5),
            "expected_new_ring_count": (options.expected_new_ring_count, 16),
            "expected_seam_count": (options.expected_seam_count, 40),
            "required_tile_width": (options.required_tile_width, 512),
            "required_tile_height": (options.required_tile_height, 512),
        }
        invalid = {key: {"actual": actual, "required": required} for key, (actual, required) in contract.items() if actual != required}
        if invalid:
            raise ValueError(f"Profil wave2-5x5 mal configure: {invalid}")
        if options.baseline_center_dir is None:
            raise ValueError("Profil wave2-5x5: --baseline-center est obligatoire.")
        if options.reference_atlas_path is None:
            raise ValueError("Profil wave2-5x5: --reference-atlas est obligatoire.")
    elif options.profile == "wave3-continuous-5x5":
        contract = {
            "expected_count": (options.expected_count, 25),
            "columns": (options.columns, 5),
            "rows": (options.rows, 5),
            "expected_seam_count": (options.expected_seam_count, 40),
            "required_tile_width": (options.required_tile_width, 512),
            "required_tile_height": (options.required_tile_height, 512),
            "required_master_width": (options.required_master_width, 2560),
            "required_master_height": (options.required_master_height, 2560),
            "gutter_size": (options.gutter_size, 2),
        }
        invalid = {
            key: {"actual": actual, "required": required}
            for key, (actual, required) in contract.items()
            if actual != required
        }
        if invalid:
            raise ValueError(f"Profil wave3-continuous-5x5 mal configure: {invalid}")
        if options.baseline_center_dir is not None or options.baseline_manifest_path is not None:
            raise ValueError("Profil Wave3: le verrou de hash du centre Wave1 est interdit.")
        if options.expected_new_ring_count is not None:
            raise ValueError("Profil Wave3: aucun anneau Wave2 ne doit etre configure.")
        if options.reference_atlas_path is None:
            raise ValueError("Profil Wave3: --reference-atlas (master continu) est obligatoire.")
        if options.gutters_dir is not None and not options.require_gutters:
            raise ValueError("Profil Wave3: un dossier gutters fourni doit activer son gate strict.")
        if not options.require_forbidden_review or not options.require_perceptual_review:
            raise ValueError("Profil Wave3: les revues contenu interdit et perceptuelle sont obligatoires.")
        if not options.require_signed_perceptual_review:
            raise ValueError("Profil Wave3: la revue perceptuelle signee Builder-C est obligatoire.")
        if not options.require_wave3_ready_marker or options.readiness_report_path is None:
            raise ValueError("Profil Wave3: --readiness-report est obligatoire pour le rerun officiel.")
        readiness_path = options.readiness_report_path.resolve()
        if not readiness_path.is_file():
            raise ValueError(f"Profil Wave3: rapport de readiness introuvable: {readiness_path}")
        readiness_text = readiness_path.read_text(encoding="utf-8-sig")
        if not re.search(
            r"(?m)^\s*READY_FOR_WORLD_MAP_ART_WAVE3_VALIDATION\s*=\s*YES\s*$",
            readiness_text,
        ):
            raise ValueError(
                "Profil Wave3: marker READY_FOR_WORLD_MAP_ART_WAVE3_VALIDATION=YES absent."
            )
    output_dir.mkdir(parents=True, exist_ok=True)

    thresholds = _load_thresholds(options.thresholds_path)
    manifest_path = _find_manifest(input_dir, options.manifest_path)
    manifest = _read_json(manifest_path) if manifest_path else None
    if options.profile == "wave3-continuous-5x5" and manifest_path is None:
        raise ValueError("Profil Wave3: manifest.json est obligatoire.")
    specs, discovery_notes = _build_tile_specs(input_dir, manifest, options.columns)
    grid = _grid_info(specs, manifest, options, thresholds)
    runtimes = [_analyze_image(spec, thresholds) for spec in specs]
    coordinate_map, duplicate_coordinates = _coordinate_map(runtimes)
    checks: list[dict[str, Any]] = []

    wave3_contract_required = options.profile == "wave3-continuous-5x5" or (
        options.require_gutters and options.required_master_width is not None
    )
    manifest_contract = _manifest_contract(
        specs,
        grid,
        manifest,
        options.required_tile_width,
        options.required_tile_height,
        wave3_contract_required,
    )
    checks.append(
        _check(
            "wave3_manifest_contract",
            manifest_contract["status"],
            "Manifest Wave3 complet: IDs, positions, rectangles, hashes et voisins uniques."
            if manifest_contract["status"] == "PASS"
            else "Manifest Wave3 incomplet, ambigu ou incoherent.",
            issues=manifest_contract.get("issues", []),
        )
    )
    master_contract = _master_contract(
        input_dir,
        manifest,
        options.reference_atlas_path,
        options.required_master_width,
        options.required_master_height,
        wave3_contract_required,
    )
    checks.append(
        _check(
            "continuous_master_contract",
            master_contract["status"],
            "Master continu lisible, dimensionne et verrouille par SHA-256."
            if master_contract["status"] == "PASS"
            else "Master continu absent ou different du contrat/manifest.",
            issues=master_contract.get("issues", []),
        )
    )

    actual_count = len(specs)
    count_status = "PASS" if actual_count == grid.expected_count else "FAIL"
    checks.append(
        _check(
            "expected_count",
            count_status,
            "Nombre d'images conforme." if count_status == "PASS" else "Nombre d'images different de l'attendu.",
            actual=actual_count,
            expected=grid.expected_count,
        )
    )

    readable_count = sum(runtime.image is not None for runtime in runtimes)
    checks.append(
        _check(
            "readability",
            "PASS" if readable_count == actual_count and actual_count > 0 else "FAIL",
            "Tous les fichiers sont lisibles."
            if readable_count == actual_count and actual_count > 0
            else "Un ou plusieurs fichiers sont absents ou illisibles.",
            readable=readable_count,
            declared=actual_count,
        )
    )

    hash_mismatches = [
        runtime.spec.tile_id
        for runtime in runtimes
        if runtime.spec.expected_sha256 and not runtime.analysis.get("hash_match", False)
    ]
    checks.append(
        _check(
            "hash_integrity",
            "FAIL" if hash_mismatches else "PASS",
            "Hashes conformes au manifest." if not hash_mismatches else "Hashes differents du manifest.",
            mismatches=hash_mismatches,
        )
    )
    checks.append(_dimension_check(runtimes, manifest, thresholds))
    required_size_check = _required_tile_size_check(
        runtimes, options.required_tile_width, options.required_tile_height
    )
    if required_size_check:
        checks.append(required_size_check)

    modes = sorted({runtime.analysis.get("mode") for runtime in runtimes if runtime.image is not None})
    unsupported_modes = [mode for mode in modes if mode not in {"RGB", "RGBA"}]
    channel_status = "WARN" if unsupported_modes or len(modes) > 1 else "PASS"
    checks.append(
        _check(
            "channels_alpha",
            channel_status,
            "Canaux RGB/RGBA coherents." if channel_status == "PASS" else "Modes ou canaux heterogenes a revoir.",
            modes=modes,
            unsupported_modes=unsupported_modes,
        )
    )

    coverage = _coverage_analysis(
        runtimes,
        grid,
        options.required_tile_width,
        options.required_tile_height,
    )
    checks.append(
        _check(
            "coverage_geometry",
            coverage["status"],
            "Couverture complete sans trou ni recouvrement."
            if coverage["status"] == "PASS"
            else "Trou, recouvrement ou derive de dimensions dans la reconstruction.",
            union_area=coverage["union_area"],
            expected_area=coverage["expected_area"],
            hole_area=coverage["hole_area"],
            overlap_area=coverage["overlap_area"],
        )
    )

    center_lock = _baseline_center_lock(
        runtimes,
        grid,
        options.baseline_center_dir,
        options.baseline_manifest_path,
    )
    if center_lock:
        checks.append(
            _check(
                "baseline_center_sha256_lock",
                center_lock["status"],
                "Les 9 tuiles centrales sont bit-a-bit identiques a Wave1."
                if center_lock["status"] == "PASS"
                else "Derive ou absence detectee dans le centre Wave1 verrouille.",
                matches=center_lock.get("match_count", 0),
                mismatches=center_lock.get("mismatch_count", 0),
                structural_errors=center_lock.get("structural_errors", []),
            )
        )

    ring = _ring_analysis(runtimes, grid, options.expected_new_ring_count, center_lock)
    if ring:
        checks.append(
            _check(
                "wave2_new_ring",
                ring["status"],
                "L'anneau contient exactement le nombre attendu de nouvelles tuiles."
                if ring["status"] == "PASS"
                else "L'anneau est incomplet, surnumeraire ou reutilise un hash central.",
                actual=ring["actual_ring_count"],
                expected=ring["expected_ring_count"],
                reasons=ring["reasons"],
                reused_baseline_hashes=ring["reused_baseline_hashes"],
            )
        )

    content_status = _max_status(runtime.analysis["status"] for runtime in runtimes)
    checks.append(
        _check(
            "empty_black_low_variance",
            content_status,
            "Aucune zone anormalement vide/noire."
            if content_status == "PASS"
            else "Au moins une image declenche un seuil de contenu.",
            affected=[
                runtime.spec.tile_id for runtime in runtimes if runtime.analysis["status"] != "PASS"
            ],
        )
    )

    expected_positions = {
        (grid.coordinate_origin_x + x, grid.coordinate_origin_y + y)
        for y in range(grid.rows)
        for x in range(grid.columns)
    }
    actual_positions = set(coordinate_map)
    missing_positions = sorted(expected_positions - actual_positions)
    extra_positions = sorted(actual_positions - expected_positions)
    grid_status = "FAIL" if missing_positions or extra_positions or duplicate_coordinates else "PASS"
    checks.append(
        _check(
            "grid_completeness",
            grid_status,
            "Grille complete sans trou ni coordonnee dupliquee."
            if grid_status == "PASS"
            else "Grille incomplete, hors limites ou avec coordonnees dupliquees.",
            columns=grid.columns,
            rows=grid.rows,
            origin={"x": grid.coordinate_origin_x, "y": grid.coordinate_origin_y},
            missing=[{"x": x, "y": y} for x, y in missing_positions],
            extra=[{"x": x, "y": y} for x, y in extra_positions],
            duplicate_coordinates=duplicate_coordinates,
        )
    )

    neighbor_errors = _neighbor_errors(coordinate_map)
    checks.append(
        _check(
            "manifest_neighbors",
            "FAIL" if neighbor_errors else "PASS",
            "Voisins declares coherents avec la grille."
            if not neighbor_errors
            else "Incoherences entre voisins declares et grille calculee.",
            errors=neighbor_errors,
        )
    )

    duplicates = _duplicate_analysis(runtimes, thresholds)
    duplicate_status = "FAIL" if duplicates["exact"] else "WARN" if duplicates["quasi"] else "PASS"
    checks.append(
        _check(
            "duplicates",
            duplicate_status,
            "Aucune repetition suspecte."
            if duplicate_status == "PASS"
            else "Doublons exacts ou quasi-doublons detectes.",
            exact_count=len(duplicates["exact"]),
            quasi_count=len(duplicates["quasi"]),
        )
    )
    motif_repetition = _motif_repetition_analysis(runtimes, thresholds)

    seams = _analyze_seams(coordinate_map, thresholds)
    seam_status = _max_status(seam["status"] for seam in seams)
    checks.append(
        _check(
            "adjacent_seams",
            seam_status,
            "Tous les raccords restent sous le seuil PASS."
            if seam_status == "PASS"
            else "Un ou plusieurs raccords sont visuellement suspects.",
            pass_count=sum(seam["status"] == "PASS" for seam in seams),
            warn_count=sum(seam["status"] == "WARN" for seam in seams),
            fail_count=sum(seam["status"] == "FAIL" for seam in seams),
        )
    )
    seam_statistics = _seam_statistics(seams, grid, options.expected_seam_count)
    checks.append(
        _check(
            "seam_count",
            seam_statistics["status"],
            "Nombre de coutures internes conforme."
            if seam_statistics["status"] == "PASS"
            else "Nombre de coutures internes different du contrat.",
            actual=seam_statistics["actual_count"],
            expected=seam_statistics["expected_count"],
            score_statistics=seam_statistics["scores"],
        )
    )

    forbidden_review = _forbidden_content_review(
        options.forbidden_review_path,
        output_dir,
        options.require_forbidden_review,
        manifest,
        seam_statistics,
    )
    if options.require_forbidden_review or options.forbidden_review_path is not None:
        checks.append(
            _check(
                "forbidden_painted_content",
                forbidden_review["status"],
                "Revue visuelle: aucun contenu runtime/interdit peint dans le fond."
                if forbidden_review["status"] == "PASS"
                else "Contenu interdit present, incertain ou non revu.",
                present=forbidden_review.get("present_categories", []),
                unresolved=forbidden_review.get("unresolved_categories", []),
                reason=forbidden_review.get("reason"),
            )
        )

    memory_budget = _memory_budget(runtimes, thresholds)
    checks.append(
        _check(
            "memory_budget",
            memory_budget["status"],
            "Budgets sous les seuils configures."
            if memory_budget["status"] == "PASS"
            else "Un ou plusieurs budgets depassent les seuils configures.",
            reasons=memory_budget["reasons"],
        )
    )

    reconstruction, reconstruction_info, placements = _reconstruct(
        runtimes,
        grid,
        output_dir,
        input_dir,
        manifest,
        thresholds,
        options.reference_atlas_path,
    )
    checks.append(
        _check(
            "reconstruction",
            reconstruction_info["status"],
            "Reconstruction generee."
            if reconstruction_info["status"] == "PASS"
            else "Reconstruction generee avec reserve ou ecart source.",
            full_resolution=reconstruction_info["full_resolution"],
            source_comparison=reconstruction_info.get("source_comparison"),
        )
    )
    runtime_gutters = _validate_runtime_gutters(
        reconstruction,
        runtimes,
        coordinate_map,
        placements,
        options.gutters_dir,
        options.gutter_size,
        options.require_gutters,
        output_dir,
    )
    if options.require_gutters or options.gutters_dir is not None:
        checks.append(
            _check(
                "runtime_neighbor_gutters",
                runtime_gutters["status"],
                "Les 25 gutters runtime sont pixel-identiques aux vrais voisins."
                if runtime_gutters["status"] == "PASS"
                else "Gutters absents, alteres ou non derives des vrais voisins.",
                validated_count=runtime_gutters.get("validated_count", 0),
                boundary_count=runtime_gutters.get("boundary_count", 0),
                boundary_pass_count=runtime_gutters.get("boundary_pass_count", 0),
                boundary_fail_count=runtime_gutters.get("boundary_fail_count", 0),
                reason=runtime_gutters.get("reason"),
            )
        )
    low_frequency_grid = _low_frequency_grid_analysis(
        reconstruction,
        seams,
        grid,
        thresholds,
    )
    macro_patterns = _macro_pattern_analysis(reconstruction, grid, thresholds)
    perceptual_artifacts = _perceptual_artifacts(reconstruction, output_dir)
    perceptual_review = _perceptual_continuity_review(
        options.perceptual_review_path,
        output_dir,
        options.require_perceptual_review,
        options.require_signed_perceptual_review,
        low_frequency_grid,
        motif_repetition,
        macro_patterns,
        seam_statistics,
    )
    if options.require_perceptual_review or options.perceptual_review_path is not None:
        checks.append(
            _check(
                "low_frequency_grid_continuity",
                low_frequency_grid["status"],
                "Aucune ligne de grille basse frequence ni rupture colorimetrique bloquante detectee."
                if low_frequency_grid["status"] == "PASS"
                else "Rupture basse frequence ou profil de grille detecte aux limites.",
                pass_count=low_frequency_grid["pass_count"],
                warn_count=low_frequency_grid["warn_count"],
                fail_count=low_frequency_grid["fail_count"],
                maximum_gradient_ratio=low_frequency_grid["maximum_gradient_ratio"],
                maximum_color_delta=low_frequency_grid["maximum_color_delta"],
                maximum_grid_line_coverage=low_frequency_grid["maximum_grid_line_coverage"],
            )
        )
        checks.append(
            _check(
                "macro_pattern_continuity",
                macro_patterns["status"],
                "Aucun damier artificiel ni bande floue bloquante detecte."
                if macro_patterns["status"] == "PASS"
                else "Damier artificiel ou bande floue suspecte detecte.",
                checkerboard=macro_patterns["checkerboard"],
                blurred_boundary_bands={
                    key: value
                    for key, value in macro_patterns["blurred_boundary_bands"].items()
                    if key != "boundaries"
                },
            )
        )
        checks.append(
            _check(
                "motif_repetition",
                motif_repetition["status"],
                "Aucune copie ou repetition perceptuelle suspecte."
                if motif_repetition["status"] == "PASS"
                else "Copies exactes ou motifs perceptuellement proches a revoir.",
                exact_copies=motif_repetition["exact_copy_count"],
                suspicious=motif_repetition["suspicious_similarity_count"],
                exact_mirrors=motif_repetition["exact_mirror_count"],
                suspicious_mirrors=motif_repetition["suspicious_mirror_count"],
            )
        )
        checks.append(
            _check(
                "perceptual_continuity_review",
                perceptual_review["status"],
                "Continuite perceptuelle multi-echelle approuvee sans grille visible."
                if perceptual_review["status"] == "PASS"
                else "Limite perceptible, incertitude ou revue multi-echelle incomplete.",
                grid_pattern_visible=perceptual_review.get("grid_pattern_visible", "UNRESOLVED"),
                visible=perceptual_review.get("visible_categories", []),
                unresolved=perceptual_review.get("unresolved_categories", []),
                reason=perceptual_review.get("reason"),
            )
        )
    contact_info = _contact_sheet(runtimes, grid, output_dir, thresholds)
    heatmap_info = _seam_heatmap(reconstruction, placements, runtimes, seams, output_dir)
    top_risks = _top_risk_artifact(
        reconstruction,
        placements,
        runtimes,
        seams,
        low_frequency_grid,
        macro_patterns,
        motif_repetition,
        output_dir,
    )
    qa_grid_info = _qa_grid(
        reconstruction,
        placements,
        runtimes,
        grid,
        forbidden_review,
        output_dir,
        options.profile,
    )

    all_pngs = sorted(input_dir.rglob("*.png"))
    referenced = {spec.file_path.resolve() for spec in specs}
    unreferenced_all = [
        path.relative_to(input_dir).as_posix()
        for path in all_pngs
        if path.resolve() not in referenced
        and not any(part.lower() in {"validation", "validator-output"} for part in path.parts)
    ]
    support_name_pattern = re.compile(
        r"^(atlas_|master_|reconstruction_|contact_sheet|qa_|seam_|source_|pan_|outpaint_|wave1_|strict_)",
        re.IGNORECASE,
    )
    support_pngs = [path for path in unreferenced_all if support_name_pattern.match(Path(path).stem)]
    unreferenced = [path for path in unreferenced_all if path not in support_pngs]
    checks.append(
        _check(
            "unreferenced_png",
            "WARN" if unreferenced else "PASS",
            "Aucun PNG source non reference."
            if not unreferenced
            else "PNG presents mais absents du manifest.",
            files=unreferenced,
            recognized_support_files=support_pngs,
        )
    )

    center_ring_statistics = seam_statistics["by_boundary_class"]["center_ring"]
    center_ring_all_pass = (
        center_ring_statistics["count"] == 12
        and center_ring_statistics["pass_count"] == 12
        and center_ring_statistics["warn_count"] == 0
        and center_ring_statistics["fail_count"] == 0
    )
    all_seams_pass = (
        "PASS" if seam_statistics["warn_count"] == 0 and seam_statistics["fail_count"] == 0 else "FAIL"
    )
    source_comparison = reconstruction_info.get("source_comparison") or {}
    reconstruction_pixel_identical = source_comparison.get("pixel_identical") is True
    if wave3_contract_required:
        technical_required = {
            "manifest_contract": manifest_contract["status"],
            "master_contract": master_contract["status"],
            "coverage": coverage["status"],
            "seam_count_40": seam_statistics["status"],
            "all_40_canonical_boundaries_pass": all_seams_pass,
            "reconstruction_pixel_identical_to_master": "PASS" if reconstruction_pixel_identical else "FAIL",
            "wave2_center_hash_lock_removed": "PASS" if center_lock is None and ring is None else "FAIL",
        }
        if options.gutters_dir is not None:
            technical_required["runtime_gutters_516_from_true_neighbors"] = runtime_gutters["status"]
            technical_required["gutter_boundaries_40_of_40"] = (
                "PASS"
                if runtime_gutters.get("boundary_count") == 40
                and runtime_gutters.get("boundary_pass_count") == 40
                and runtime_gutters.get("boundary_fail_count") == 0
                else "FAIL"
            )
    elif options.profile == "wave2-5x5" or center_lock is not None or ring is not None:
        technical_required = {
            "coverage": coverage["status"],
            "center_lock": center_lock["status"] if center_lock else "PASS",
            "ring": ring["status"] if ring else "PASS",
            "seam_count": seam_statistics["status"],
            "center_ring_12_of_12": "PASS" if center_ring_all_pass else "FAIL",
            "all_seams_pass": all_seams_pass,
            "reconstruction": reconstruction_info["status"],
        }
    else:
        technical_required = {
            "coverage": coverage["status"],
            "seam_count": seam_statistics["status"],
            "all_seams_pass": all_seams_pass,
            "reconstruction": reconstruction_info["status"],
        }
    technical_continuity_status = _max_status(technical_required.values())
    perceptual_continuity_status = perceptual_review["status"]
    grid_pattern_visible = perceptual_review.get("grid_pattern_visible", "UNRESOLVED")
    continuity_gates = {
        "technical_continuity": {
            "status": technical_continuity_status,
            "components": technical_required,
        },
        "perceptual_continuity": {
            "status": perceptual_continuity_status,
            "automation_status": perceptual_review.get("automation_status"),
            "human_review_status": perceptual_review.get("human_review_status"),
        },
        "grid_pattern_visible": grid_pattern_visible,
        "runtime_gutters": {
            "status": runtime_gutters["status"],
            "blocking_canonical_art_validation": runtime_gutters.get(
                "blocking_canonical_art_validation", options.gutters_dir is not None
            ),
        },
        "content_status": "PASS"
        if technical_continuity_status == "PASS"
        and perceptual_continuity_status == "PASS"
        and grid_pattern_visible == "NO"
        and forbidden_review["status"] == "PASS"
        else "FAIL",
    }

    overall_status = _max_status(check["status"] for check in checks)
    report: dict[str, Any] = {
        "schema": SCHEMA,
        "tool_version": TOOL_VERSION,
        "overall_status": overall_status,
        "run": {
            "label": options.label,
            "profile": options.profile,
            "input_dir": str(input_dir),
            "output_dir": str(output_dir),
            "manifest_path": str(manifest_path) if manifest_path else None,
            "manifest_detected": manifest_path is not None,
            "generated_at_utc": datetime.now(timezone.utc).isoformat(),
            "discovery_notes": discovery_notes,
            "art_inspection_only": True,
            "route_or_pathfinding_logic": False,
            "readiness_report_path": str(options.readiness_report_path.resolve())
            if options.readiness_report_path
            else None,
            "wave3_ready_marker_required": options.require_wave3_ready_marker,
            "wave3_ready_marker_verified": options.profile == "wave3-continuous-5x5",
        },
        "grid": {
            "columns": grid.columns,
            "rows": grid.rows,
            "expected_count": grid.expected_count,
            "coordinate_origin": {"x": grid.coordinate_origin_x, "y": grid.coordinate_origin_y},
        },
        "summary": {
            "actual_count": actual_count,
            "expected_count": grid.expected_count,
            "readable_count": readable_count,
            "seam_count": len(seams),
            "seams_pass": sum(seam["status"] == "PASS" for seam in seams),
            "seams_warn": sum(seam["status"] == "WARN" for seam in seams),
            "seams_fail": sum(seam["status"] == "FAIL" for seam in seams),
            "exact_duplicate_count": len(duplicates["exact"]),
            "quasi_duplicate_count": len(duplicates["quasi"]),
        },
        "checks": checks,
        "tiles": [runtime.analysis for runtime in runtimes],
        "seams": seams,
        "duplicates": duplicates,
        "coverage": coverage,
        "manifest_contract": manifest_contract,
        "master_contract": master_contract,
        "center_lock": center_lock,
        "ring": ring,
        "seam_statistics": seam_statistics,
        "forbidden_content_review": forbidden_review,
        "low_frequency_grid": low_frequency_grid,
        "macro_patterns": macro_patterns,
        "motif_repetition": motif_repetition,
        "perceptual_continuity_review": perceptual_review,
        "continuity_gates": continuity_gates,
        "memory_budget": memory_budget,
        "reconstruction": reconstruction_info,
        "runtime_gutters": runtime_gutters,
        "top_risks": top_risks["risks"],
        "artifacts": {
            "reconstruction": reconstruction_info,
            "contact_sheet": contact_info,
            "seam_heatmap": heatmap_info,
            "top_risks": top_risks["artifact"],
            "qa_grid": qa_grid_info,
            "forbidden_content_review_template": {
                "file": forbidden_review["template_file"],
            },
            "perceptual_continuity_review_template": {
                "file": perceptual_review["template_file"],
            },
            **perceptual_artifacts,
        },
        "thresholds": thresholds,
    }
    if runtime_gutters.get("artifact"):
        report["artifacts"]["runtime_gutters_contact_sheet"] = runtime_gutters["artifact"]
    validation_path = output_dir / "validation.json"
    validation_path.write_text(
        json.dumps(report, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    _write_markdown(report, output_dir / "report.md")
    for runtime in runtimes:
        if runtime.image is not None:
            runtime.image.close()
    reconstruction.close()
    return report
