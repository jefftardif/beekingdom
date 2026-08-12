from __future__ import annotations

import hashlib
import json
import re
from collections import defaultdict
from pathlib import Path
from typing import Any

import numpy as np
from PIL import Image


TOOL_VERSION = "1.0.0"
MASTER_SIZE = 2560
GRID_SIZE = 5
CANONICAL_TILE_SIZE = 512
GUTTER = 2
RUNTIME_TILE_SIZE = CANONICAL_TILE_SIZE + GUTTER * 2
CANONICAL_SCHEMA = "bee-kingdom.world-map-macro-canonical.v1"
RUNTIME_SCHEMA = "bee-kingdom.world-map-macro-runtime-gutter.v1"
VALIDATION_SCHEMA = "bee-kingdom.world-map-macro-validation.v1"
ALLOWED_MODES = {"RGB", "RGBA"}


class MacroSlicerError(RuntimeError):
    def __init__(self, code: str, message: str):
        super().__init__(message)
        self.code = code


def _tile_id(row: int, column: int) -> str:
    return f"R{row}C{column}"


def _expected_tile_ids() -> list[str]:
    return [_tile_id(row, column) for row in range(GRID_SIZE) for column in range(GRID_SIZE)]


def _sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _sha256_pixels(array: np.ndarray) -> str:
    return hashlib.sha256(np.ascontiguousarray(array).tobytes()).hexdigest()


def _write_json(path: Path, data: dict[str, Any]) -> None:
    path.write_text(
        json.dumps(data, indent=2, sort_keys=True, ensure_ascii=False) + "\n",
        encoding="utf-8",
        newline="\n",
    )


def _read_json(path: Path) -> dict[str, Any]:
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
    except FileNotFoundError as exc:
        raise MacroSlicerError("MISSING_MANIFEST", f"Manifest absent: {path}") from exc
    except json.JSONDecodeError as exc:
        raise MacroSlicerError("INVALID_MANIFEST_JSON", f"JSON invalide: {path}: {exc}") from exc
    if not isinstance(data, dict):
        raise MacroSlicerError("INVALID_MANIFEST_ROOT", f"Le manifest doit contenir un objet: {path}")
    return data


def _clean_image_from_array(array: np.ndarray) -> Image.Image:
    contiguous = np.ascontiguousarray(array, dtype=np.uint8)
    return Image.fromarray(contiguous)


def _save_png(array: np.ndarray, path: Path) -> None:
    image = _clean_image_from_array(array)
    image.save(path, format="PNG", compress_level=9, optimize=False)
    image.close()


def _load_master(path: Path) -> tuple[np.ndarray, str]:
    path = path.resolve()
    if not path.is_file():
        raise MacroSlicerError("MASTER_NOT_FOUND", f"Master PNG introuvable: {path}")
    try:
        with Image.open(path) as source:
            source.load()
            mode = source.mode
            size = source.size
            file_format = source.format
            if file_format != "PNG":
                raise MacroSlicerError("INVALID_FORMAT", f"Format attendu PNG, reçu {file_format!r}.")
            if size != (MASTER_SIZE, MASTER_SIZE):
                raise MacroSlicerError(
                    "INVALID_DIMENSIONS",
                    f"Dimensions attendues {MASTER_SIZE}x{MASTER_SIZE}, reçues {size[0]}x{size[1]}.",
                )
            if mode not in ALLOWED_MODES:
                raise MacroSlicerError(
                    "INVALID_COLOR_MODE",
                    f"Mode attendu RGB ou RGBA, reçu {mode}.",
                )
            array = np.asarray(source, dtype=np.uint8).copy()
    except MacroSlicerError:
        raise
    except Exception as exc:
        raise MacroSlicerError("MASTER_UNREADABLE", f"Master illisible: {type(exc).__name__}: {exc}") from exc
    return array, mode


def _source_metadata(master_path: Path, master: np.ndarray, mode: str) -> dict[str, Any]:
    return {
        "file": master_path.name,
        "format": "PNG",
        "height": MASTER_SIZE,
        "mode": mode,
        "pixel_sha256": _sha256_pixels(master),
        "sha256": _sha256_file(master_path),
        "width": MASTER_SIZE,
    }


def _claims() -> dict[str, Any]:
    return {
        "art_pipeline_only": True,
        "live_server": False,
        "official_world_map": False,
        "runtime_integration": False,
        "scope": "local_demo_pipeline",
        "unity_dependency": False,
    }


def _canonical_crop(row: int, column: int) -> dict[str, int]:
    return {
        "height": CANONICAL_TILE_SIZE,
        "width": CANONICAL_TILE_SIZE,
        "x": column * CANONICAL_TILE_SIZE,
        "y": row * CANONICAL_TILE_SIZE,
    }


def _runtime_expected(master: np.ndarray, row: int, column: int) -> np.ndarray:
    x0 = column * CANONICAL_TILE_SIZE
    y0 = row * CANONICAL_TILE_SIZE
    xs = np.clip(
        np.arange(x0 - GUTTER, x0 + CANONICAL_TILE_SIZE + GUTTER, dtype=np.int32),
        0,
        MASTER_SIZE - 1,
    )
    ys = np.clip(
        np.arange(y0 - GUTTER, y0 + CANONICAL_TILE_SIZE + GUTTER, dtype=np.int32),
        0,
        MASTER_SIZE - 1,
    )
    return np.ascontiguousarray(master[ys[:, None], xs[None, :], :])


def _runtime_tile_metadata(
    row: int,
    column: int,
    order_index: int,
    path: Path,
    pixels: np.ndarray,
    source_sha256: str,
) -> dict[str, Any]:
    x0 = column * CANONICAL_TILE_SIZE
    y0 = row * CANONICAL_TILE_SIZE
    clamp = {
        "bottom": GUTTER if row == GRID_SIZE - 1 else 0,
        "left": GUTTER if column == 0 else 0,
        "right": GUTTER if column == GRID_SIZE - 1 else 0,
        "top": GUTTER if row == 0 else 0,
    }
    provenance = {
        "bottom": "outer_edge_clamp" if clamp["bottom"] else "true_master_neighbor_pixels",
        "left": "outer_edge_clamp" if clamp["left"] else "true_master_neighbor_pixels",
        "right": "outer_edge_clamp" if clamp["right"] else "true_master_neighbor_pixels",
        "top": "outer_edge_clamp" if clamp["top"] else "true_master_neighbor_pixels",
    }
    uv_min = GUTTER / RUNTIME_TILE_SIZE
    uv_max = (GUTTER + CANONICAL_TILE_SIZE) / RUNTIME_TILE_SIZE
    return {
        "canonical_crop": _canonical_crop(row, column),
        "clamp_pixels": clamp,
        "column": column,
        "dimensions": {"height": RUNTIME_TILE_SIZE, "width": RUNTIME_TILE_SIZE},
        "file": f"tiles/{path.name}",
        "gutter_provenance": provenance,
        "id": _tile_id(row, column),
        "inner_rect": {
            "height": CANONICAL_TILE_SIZE,
            "width": CANONICAL_TILE_SIZE,
            "x": GUTTER,
            "y": GUTTER,
        },
        "macro_origin": {"x": x0, "y": y0},
        "order_index": order_index,
        "pixel_sha256": _sha256_pixels(pixels),
        "png_sha256": _sha256_file(path),
        "row": row,
        "source_master_sha256": source_sha256,
        "source_window_unclamped": {
            "height": RUNTIME_TILE_SIZE,
            "width": RUNTIME_TILE_SIZE,
            "x": x0 - GUTTER,
            "y": y0 - GUTTER,
        },
        "uv_inner_normalized": {
            "u_max": uv_max,
            "u_min": uv_min,
            "v_max": uv_max,
            "v_min": uv_min,
        },
    }


def _assert_new_output(output_dir: Path) -> None:
    if output_dir.exists() and any(output_dir.iterdir()):
        raise MacroSlicerError(
            "OUTPUT_NOT_EMPTY",
            f"Le dossier de sortie doit être absent ou vide: {output_dir}",
        )


def slice_master(master_path: Path, output_dir: Path, version: str = "wave3") -> dict[str, Any]:
    master_path = master_path.resolve()
    output_dir = output_dir.resolve()
    if not re.fullmatch(r"[A-Za-z0-9._-]+", version):
        raise MacroSlicerError("INVALID_VERSION", "Version limitée à A-Z, a-z, 0-9, point, tiret et underscore.")
    master, mode = _load_master(master_path)
    _assert_new_output(output_dir)

    canonical_dir = output_dir / "canonical"
    canonical_tiles_dir = canonical_dir / "tiles"
    runtime_dir = output_dir / "runtime"
    runtime_tiles_dir = runtime_dir / "tiles"
    canonical_tiles_dir.mkdir(parents=True, exist_ok=True)
    runtime_tiles_dir.mkdir(parents=True, exist_ok=True)

    source = _source_metadata(master_path, master, mode)
    canonical_entries: list[dict[str, Any]] = []
    runtime_entries: list[dict[str, Any]] = []

    for order_index, tile_id in enumerate(_expected_tile_ids()):
        row, column = divmod(order_index, GRID_SIZE)
        y0 = row * CANONICAL_TILE_SIZE
        x0 = column * CANONICAL_TILE_SIZE
        canonical_pixels = np.ascontiguousarray(
            master[y0 : y0 + CANONICAL_TILE_SIZE, x0 : x0 + CANONICAL_TILE_SIZE, :]
        )
        canonical_path = canonical_tiles_dir / f"{tile_id}.png"
        _save_png(canonical_pixels, canonical_path)
        canonical_entries.append(
            {
                "column": column,
                "crop": _canonical_crop(row, column),
                "dimensions": {"height": CANONICAL_TILE_SIZE, "width": CANONICAL_TILE_SIZE},
                "file": f"tiles/{canonical_path.name}",
                "id": tile_id,
                "order_index": order_index,
                "pixel_sha256": _sha256_pixels(canonical_pixels),
                "png_sha256": _sha256_file(canonical_path),
                "row": row,
            }
        )

        runtime_pixels = _runtime_expected(master, row, column)
        runtime_path = runtime_tiles_dir / f"{tile_id}_g{GUTTER}.png"
        _save_png(runtime_pixels, runtime_path)
        runtime_entries.append(
            _runtime_tile_metadata(
                row,
                column,
                order_index,
                runtime_path,
                runtime_pixels,
                source["sha256"],
            )
        )

    reconstruction = np.empty_like(master)
    for entry in canonical_entries:
        tile_path = canonical_dir / entry["file"]
        with Image.open(tile_path) as tile_image:
            tile_image.load()
            tile_pixels = np.asarray(tile_image, dtype=np.uint8)
        crop = entry["crop"]
        reconstruction[
            crop["y"] : crop["y"] + crop["height"],
            crop["x"] : crop["x"] + crop["width"],
            :,
        ] = tile_pixels
    reconstruction_difference = np.any(reconstruction != master, axis=2)
    reconstruction_diff_count = int(np.count_nonzero(reconstruction_difference))
    reconstruction_path = canonical_dir / "reconstruction.png"
    _save_png(reconstruction, reconstruction_path)

    canonical_manifest = {
        "claims": _claims(),
        "encoding": {"format": "PNG", "lossless": True, "pillow_optimize": False, "png_compress_level": 9},
        "grid": {
            "columns": GRID_SIZE,
            "order": "row_major_R0C0_to_R4C4",
            "rows": GRID_SIZE,
            "tile_height": CANONICAL_TILE_SIZE,
            "tile_width": CANONICAL_TILE_SIZE,
        },
        "reconstruction": {
            "file": "reconstruction.png",
            "height": MASTER_SIZE,
            "pixel_difference_count": reconstruction_diff_count,
            "pixel_identical_to_source": reconstruction_diff_count == 0,
            "pixel_sha256": _sha256_pixels(reconstruction),
            "png_sha256": _sha256_file(reconstruction_path),
            "width": MASTER_SIZE,
        },
        "schema": CANONICAL_SCHEMA,
        "source": source,
        "tile_count": len(canonical_entries),
        "tile_order": _expected_tile_ids(),
        "tiles": canonical_entries,
        "tool_version": TOOL_VERSION,
        "version": version,
    }
    canonical_manifest_path = canonical_dir / "manifest.canonical.json"
    _write_json(canonical_manifest_path, canonical_manifest)

    runtime_manifest = {
        "claims": _claims(),
        "grid": {
            "columns": GRID_SIZE,
            "order": "row_major_R0C0_to_R4C4",
            "rows": GRID_SIZE,
        },
        "gutter": {
            "outer_edge_policy": "clamp_master_edge_only",
            "pixels_each_side": GUTTER,
            "runtime_height": RUNTIME_TILE_SIZE,
            "runtime_width": RUNTIME_TILE_SIZE,
            "source_for_internal_sides": "true_adjacent_master_pixels",
            "stretching": False,
        },
        "schema": RUNTIME_SCHEMA,
        "source": source,
        "tile_count": len(runtime_entries),
        "tile_order": _expected_tile_ids(),
        "tiles": runtime_entries,
        "tool_version": TOOL_VERSION,
        "uv_convention": {
            "rect_uses_pixel_boundaries": True,
            "texture_local": True,
            "v_flip_is_runtime_adapter_responsibility": True,
        },
        "version": version,
    }
    runtime_manifest_path = runtime_dir / "manifest.runtime.json"
    _write_json(runtime_manifest_path, runtime_manifest)

    verification = verify_bundle(master_path, output_dir)
    verification["manifest_sha256"] = {
        "canonical": _sha256_file(canonical_manifest_path),
        "runtime": _sha256_file(runtime_manifest_path),
    }
    _write_json(output_dir / "validation.json", verification)
    if verification["status"] != "PASS":
        raise MacroSlicerError("SELF_VERIFICATION_FAILED", "Le bundle généré a échoué à sa propre vérification.")
    return verification


def _issue(issues: list[dict[str, Any]], code: str, scope: str, message: str, **details: Any) -> None:
    row: dict[str, Any] = {"code": code, "message": message, "scope": scope}
    if details:
        row["details"] = details
    issues.append(row)


def _safe_bundle_file(base: Path, relative: Any) -> Path | None:
    if not isinstance(relative, str) or not relative.strip():
        return None
    candidate = Path(relative)
    if candidate.is_absolute():
        return None
    resolved = (base / candidate).resolve()
    if not resolved.is_relative_to(base.resolve()):
        return None
    return resolved


def _load_tile(path: Path) -> tuple[np.ndarray, str]:
    with Image.open(path) as image:
        image.load()
        return np.asarray(image, dtype=np.uint8).copy(), image.mode


def _pixel_difference_count(left: np.ndarray, right: np.ndarray) -> int:
    if left.shape != right.shape:
        return max(left.shape[0] * left.shape[1], right.shape[0] * right.shape[1])
    if left.ndim == 2:
        return int(np.count_nonzero(left != right))
    return int(np.count_nonzero(np.any(left != right, axis=2)))


def _expected_runtime_manifest_metadata(row: int, column: int) -> dict[str, Any]:
    x0 = column * CANONICAL_TILE_SIZE
    y0 = row * CANONICAL_TILE_SIZE
    clamp = {
        "bottom": GUTTER if row == GRID_SIZE - 1 else 0,
        "left": GUTTER if column == 0 else 0,
        "right": GUTTER if column == GRID_SIZE - 1 else 0,
        "top": GUTTER if row == 0 else 0,
    }
    provenance = {
        side: "outer_edge_clamp" if pixels else "true_master_neighbor_pixels"
        for side, pixels in clamp.items()
    }
    uv_min = GUTTER / RUNTIME_TILE_SIZE
    uv_max = (GUTTER + CANONICAL_TILE_SIZE) / RUNTIME_TILE_SIZE
    return {
        "canonical_crop": _canonical_crop(row, column),
        "clamp_pixels": clamp,
        "column": column,
        "dimensions": {"height": RUNTIME_TILE_SIZE, "width": RUNTIME_TILE_SIZE},
        "gutter_provenance": provenance,
        "inner_rect": {
            "height": CANONICAL_TILE_SIZE,
            "width": CANONICAL_TILE_SIZE,
            "x": GUTTER,
            "y": GUTTER,
        },
        "macro_origin": {"x": x0, "y": y0},
        "row": row,
        "source_window_unclamped": {
            "height": RUNTIME_TILE_SIZE,
            "width": RUNTIME_TILE_SIZE,
            "x": x0 - GUTTER,
            "y": y0 - GUTTER,
        },
        "uv_inner_normalized": {
            "u_max": uv_max,
            "u_min": uv_min,
            "v_max": uv_max,
            "v_min": uv_min,
        },
    }


def _empty_verification() -> dict[str, Any]:
    return {
        "canonical": {
            "pixel_alteration_count": None,
            "reconstruction_pixel_difference_count": None,
            "tile_count_actual": 0,
            "tile_count_expected": GRID_SIZE * GRID_SIZE,
        },
        "claims": _claims(),
        "issues": [],
        "runtime": {
            "full_tile_mismatch_pixel_count": None,
            "gutter_mismatch_pixel_count": None,
            "internal_boundaries_checked": 0,
            "internal_boundaries_expected": 40,
            "internal_boundaries_passed": 0,
            "interior_mismatch_pixel_count": None,
            "tile_count_actual": 0,
            "tile_count_expected": GRID_SIZE * GRID_SIZE,
        },
        "schema": VALIDATION_SCHEMA,
        "status": "FAIL",
        "tool_version": TOOL_VERSION,
        "verdicts": {
            "CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL": "NO",
            "READY_FOR_UIB_WAVE3_MASTER_INGEST": "NO",
            "RUNTIME_GUTTERS_FROM_TRUE_NEIGHBORS": "NO",
            "WORLD_MAP_MACRO_SLICER_WAVE3": "FAIL",
        },
    }


def verify_bundle(master_path: Path, bundle_dir: Path) -> dict[str, Any]:
    master_path = master_path.resolve()
    bundle_dir = bundle_dir.resolve()
    result = _empty_verification()
    issues: list[dict[str, Any]] = result["issues"]

    try:
        master, mode = _load_master(master_path)
    except MacroSlicerError as exc:
        _issue(issues, exc.code, "source", str(exc))
        return result
    source = _source_metadata(master_path, master, mode)
    result["source"] = source

    canonical_dir = bundle_dir / "canonical"
    runtime_dir = bundle_dir / "runtime"
    try:
        canonical_manifest = _read_json(canonical_dir / "manifest.canonical.json")
    except MacroSlicerError as exc:
        _issue(issues, exc.code, "canonical", str(exc))
        canonical_manifest = {}
    try:
        runtime_manifest = _read_json(runtime_dir / "manifest.runtime.json")
    except MacroSlicerError as exc:
        _issue(issues, exc.code, "runtime", str(exc))
        runtime_manifest = {}

    for scope, manifest, expected_schema in (
        ("canonical", canonical_manifest, CANONICAL_SCHEMA),
        ("runtime", runtime_manifest, RUNTIME_SCHEMA),
    ):
        if manifest and manifest.get("schema") != expected_schema:
            _issue(
                issues,
                "SCHEMA_MISMATCH",
                scope,
                "Schéma de manifest inattendu.",
                expected=expected_schema,
                actual=manifest.get("schema"),
            )
        manifest_source = manifest.get("source") if isinstance(manifest.get("source"), dict) else {}
        source_contract_mismatches = {
            key: {"expected": source[key], "actual": manifest_source.get(key)}
            for key in ("width", "height", "mode", "format")
            if manifest and manifest_source.get(key) != source[key]
        }
        if source_contract_mismatches:
            _issue(
                issues,
                "SOURCE_CONTRACT_MISMATCH",
                scope,
                "Dimensions, format ou mode source du manifest incohérents.",
                mismatches=source_contract_mismatches,
            )
        if manifest and manifest_source.get("sha256") != source["sha256"]:
            _issue(
                issues,
                "SOURCE_HASH_MISMATCH",
                scope,
                "Le hash source du manifest ne correspond pas au master fourni.",
            )
        if manifest and manifest_source.get("pixel_sha256") != source["pixel_sha256"]:
            _issue(
                issues,
                "SOURCE_PIXEL_HASH_MISMATCH",
                scope,
                "Le hash pixel source du manifest ne correspond pas au master fourni.",
            )

    expected_canonical_grid = {
        "columns": GRID_SIZE,
        "order": "row_major_R0C0_to_R4C4",
        "rows": GRID_SIZE,
        "tile_height": CANONICAL_TILE_SIZE,
        "tile_width": CANONICAL_TILE_SIZE,
    }
    if canonical_manifest and canonical_manifest.get("grid") != expected_canonical_grid:
        _issue(issues, "CANONICAL_GRID_CONTRACT_MISMATCH", "canonical", "Contrat global de grille canonique incohérent.")
    if canonical_manifest and canonical_manifest.get("tile_count") != GRID_SIZE * GRID_SIZE:
        _issue(issues, "CANONICAL_DECLARED_TILE_COUNT_MISMATCH", "canonical", "tile_count canonique doit valoir 25.")

    expected_runtime_grid = {
        "columns": GRID_SIZE,
        "order": "row_major_R0C0_to_R4C4",
        "rows": GRID_SIZE,
    }
    expected_gutter_contract = {
        "outer_edge_policy": "clamp_master_edge_only",
        "pixels_each_side": GUTTER,
        "runtime_height": RUNTIME_TILE_SIZE,
        "runtime_width": RUNTIME_TILE_SIZE,
        "source_for_internal_sides": "true_adjacent_master_pixels",
        "stretching": False,
    }
    if runtime_manifest and runtime_manifest.get("grid") != expected_runtime_grid:
        _issue(issues, "RUNTIME_GRID_CONTRACT_MISMATCH", "runtime", "Contrat global de grille runtime incohérent.")
    if runtime_manifest and runtime_manifest.get("gutter") != expected_gutter_contract:
        _issue(issues, "RUNTIME_GUTTER_CONTRACT_MISMATCH", "runtime", "Contrat global de gouttière incohérent.")
    if runtime_manifest and runtime_manifest.get("tile_count") != GRID_SIZE * GRID_SIZE:
        _issue(issues, "RUNTIME_DECLARED_TILE_COUNT_MISMATCH", "runtime", "tile_count runtime doit valoir 25.")

    expected_ids = _expected_tile_ids()
    canonical_entries = canonical_manifest.get("tiles") if isinstance(canonical_manifest.get("tiles"), list) else []
    result["canonical"]["tile_count_actual"] = len(canonical_entries)
    if len(canonical_entries) != len(expected_ids):
        _issue(
            issues,
            "CANONICAL_TILE_COUNT_MISMATCH",
            "canonical",
            "Le manifest canonique ne contient pas 25 tuiles.",
            actual=len(canonical_entries),
            expected=len(expected_ids),
        )
    canonical_order = [entry.get("id") for entry in canonical_entries if isinstance(entry, dict)]
    if canonical_order != expected_ids:
        _issue(
            issues,
            "CANONICAL_ORDER_MISMATCH",
            "canonical",
            "L'ordre doit être strictement R0C0..R4C4 en row-major.",
            actual=canonical_order,
            expected=expected_ids,
        )
    if canonical_manifest and canonical_manifest.get("tile_order") != expected_ids:
        _issue(issues, "CANONICAL_DECLARED_ORDER_MISMATCH", "canonical", "tile_order canonique incorrect.")

    canonical_by_id: dict[str, dict[str, Any]] = {}
    for entry in canonical_entries:
        if not isinstance(entry, dict) or not isinstance(entry.get("id"), str):
            _issue(issues, "INVALID_CANONICAL_ENTRY", "canonical", "Entrée canonique invalide.")
            continue
        tile_id = entry["id"]
        if tile_id in canonical_by_id:
            _issue(issues, "DUPLICATE_CANONICAL_ID", "canonical", "Identifiant canonique dupliqué.", tile_id=tile_id)
        canonical_by_id[tile_id] = entry

    canonical_hash_groups: dict[str, list[str]] = defaultdict(list)
    canonical_pixel_hash_groups: dict[str, list[str]] = defaultdict(list)
    canonical_arrays: dict[str, np.ndarray] = {}
    canonical_pixel_alteration_count = 0
    canonical_reconstruction = np.zeros_like(master)
    canonical_reconstruction_complete = True

    for order_index, tile_id in enumerate(expected_ids):
        row, column = divmod(order_index, GRID_SIZE)
        entry = canonical_by_id.get(tile_id)
        if entry is None:
            _issue(issues, "MISSING_CANONICAL_ENTRY", "canonical", "Tuile absente du manifest.", tile_id=tile_id)
            canonical_reconstruction_complete = False
            continue
        expected_metadata = {
            "column": column,
            "crop": _canonical_crop(row, column),
            "dimensions": {"height": CANONICAL_TILE_SIZE, "width": CANONICAL_TILE_SIZE},
            "order_index": order_index,
            "row": row,
        }
        metadata_mismatches = {
            key: {"expected": value, "actual": entry.get(key)}
            for key, value in expected_metadata.items()
            if entry.get(key) != value
        }
        if metadata_mismatches:
            _issue(
                issues,
                "CANONICAL_METADATA_MISMATCH",
                "canonical",
                "Métadonnées canoniques incohérentes.",
                tile_id=tile_id,
                mismatches=metadata_mismatches,
            )
        tile_path = _safe_bundle_file(canonical_dir, entry.get("file"))
        if tile_path is None:
            _issue(issues, "INVALID_CANONICAL_PATH", "canonical", "Chemin de tuile invalide.", tile_id=tile_id)
            canonical_reconstruction_complete = False
            continue
        if not tile_path.is_file():
            _issue(issues, "MISSING_CANONICAL_TILE", "canonical", "Fichier de tuile absent.", tile_id=tile_id)
            canonical_reconstruction_complete = False
            continue
        actual_hash = _sha256_file(tile_path)
        canonical_hash_groups[actual_hash].append(tile_id)
        if actual_hash != entry.get("png_sha256"):
            _issue(issues, "CANONICAL_HASH_MISMATCH", "canonical", "Hash PNG canonique incohérent.", tile_id=tile_id)
        try:
            tile_pixels, tile_mode = _load_tile(tile_path)
        except Exception as exc:
            _issue(issues, "UNREADABLE_CANONICAL_TILE", "canonical", str(exc), tile_id=tile_id)
            canonical_reconstruction_complete = False
            continue
        canonical_arrays[tile_id] = tile_pixels
        canonical_pixel_hash_groups[_sha256_pixels(tile_pixels)].append(tile_id)
        if tile_mode != mode or tile_pixels.shape[:2] != (CANONICAL_TILE_SIZE, CANONICAL_TILE_SIZE):
            _issue(
                issues,
                "CANONICAL_IMAGE_CONTRACT_MISMATCH",
                "canonical",
                "Mode ou dimensions de tuile canonique invalides.",
                tile_id=tile_id,
                mode=tile_mode,
                shape=list(tile_pixels.shape),
            )
            canonical_reconstruction_complete = False
            continue
        if _sha256_pixels(tile_pixels) != entry.get("pixel_sha256"):
            _issue(issues, "CANONICAL_PIXEL_HASH_MISMATCH", "canonical", "Hash pixel canonique incohérent.", tile_id=tile_id)
        crop = _canonical_crop(row, column)
        expected_pixels = master[
            crop["y"] : crop["y"] + crop["height"],
            crop["x"] : crop["x"] + crop["width"],
            :,
        ]
        tile_difference = _pixel_difference_count(tile_pixels, expected_pixels)
        canonical_pixel_alteration_count += tile_difference
        if tile_difference:
            _issue(
                issues,
                "CANONICAL_PIXEL_ALTERATION",
                "canonical",
                "La tuile ne correspond plus au crop source.",
                tile_id=tile_id,
                different_pixels=tile_difference,
            )
        canonical_reconstruction[
            crop["y"] : crop["y"] + crop["height"],
            crop["x"] : crop["x"] + crop["width"],
            :,
        ] = tile_pixels

    for duplicate_hash, tile_ids in canonical_hash_groups.items():
        if len(tile_ids) > 1:
            _issue(
                issues,
                "DUPLICATE_CANONICAL_TILE",
                "canonical",
                "Plusieurs tuiles canoniques ont un contenu PNG identique.",
                sha256=duplicate_hash,
                tile_ids=tile_ids,
            )
    for duplicate_hash, tile_ids in canonical_pixel_hash_groups.items():
        if len(tile_ids) > 1 and not any(
            issue["code"] == "DUPLICATE_CANONICAL_TILE"
            and set(issue.get("details", {}).get("tile_ids", [])) == set(tile_ids)
            for issue in issues
        ):
            _issue(
                issues,
                "DUPLICATE_CANONICAL_PIXELS",
                "canonical",
                "Plusieurs tuiles canoniques ont des pixels identiques.",
                pixel_sha256=duplicate_hash,
                tile_ids=tile_ids,
            )
    expected_canonical_files = {f"{tile_id}.png" for tile_id in expected_ids}
    actual_canonical_files = {path.name for path in (canonical_dir / "tiles").glob("*.png")} if (canonical_dir / "tiles").is_dir() else set()
    for extra in sorted(actual_canonical_files - expected_canonical_files):
        _issue(issues, "EXTRA_CANONICAL_TILE", "canonical", "PNG canonique non déclaré.", file=extra)

    reconstruction_diff = (
        _pixel_difference_count(canonical_reconstruction, master)
        if canonical_reconstruction_complete
        else None
    )
    result["canonical"]["pixel_alteration_count"] = canonical_pixel_alteration_count
    result["canonical"]["reconstruction_pixel_difference_count"] = reconstruction_diff
    reconstruction_entry = canonical_manifest.get("reconstruction")
    reconstruction_entry = reconstruction_entry if isinstance(reconstruction_entry, dict) else {}
    reconstruction_path = _safe_bundle_file(canonical_dir, reconstruction_entry.get("file"))
    if reconstruction_path is None or not reconstruction_path.is_file():
        _issue(issues, "MISSING_RECONSTRUCTION", "canonical", "Reconstruction canonique absente.")
    else:
        if _sha256_file(reconstruction_path) != reconstruction_entry.get("png_sha256"):
            _issue(issues, "RECONSTRUCTION_HASH_MISMATCH", "canonical", "Hash PNG de reconstruction incohérent.")
        try:
            reconstruction_pixels, reconstruction_mode = _load_tile(reconstruction_path)
            reconstruction_file_diff = _pixel_difference_count(reconstruction_pixels, master)
            result["canonical"]["reconstruction_file_pixel_difference_count"] = reconstruction_file_diff
            if _sha256_pixels(reconstruction_pixels) != reconstruction_entry.get("pixel_sha256"):
                _issue(issues, "RECONSTRUCTION_PIXEL_HASH_MISMATCH", "canonical", "Hash pixel de reconstruction incohérent.")
            if reconstruction_entry.get("pixel_difference_count") != 0 or reconstruction_entry.get("pixel_identical_to_source") is not True:
                _issue(issues, "RECONSTRUCTION_DECLARATION_MISMATCH", "canonical", "Déclaration de reconstruction incohérente.")
            if reconstruction_mode != mode or reconstruction_file_diff:
                _issue(
                    issues,
                    "RECONSTRUCTION_PIXEL_MISMATCH",
                    "canonical",
                    "La reconstruction enregistrée n'est pas pixel-identique au master.",
                    different_pixels=reconstruction_file_diff,
                )
        except Exception as exc:
            _issue(issues, "UNREADABLE_RECONSTRUCTION", "canonical", str(exc))

    runtime_entries = runtime_manifest.get("tiles") if isinstance(runtime_manifest.get("tiles"), list) else []
    result["runtime"]["tile_count_actual"] = len(runtime_entries)
    if len(runtime_entries) != len(expected_ids):
        _issue(
            issues,
            "RUNTIME_TILE_COUNT_MISMATCH",
            "runtime",
            "Le manifest runtime ne contient pas 25 tuiles.",
            actual=len(runtime_entries),
            expected=len(expected_ids),
        )
    runtime_order = [entry.get("id") for entry in runtime_entries if isinstance(entry, dict)]
    if runtime_order != expected_ids:
        _issue(
            issues,
            "RUNTIME_ORDER_MISMATCH",
            "runtime",
            "L'ordre runtime doit être strictement R0C0..R4C4.",
            actual=runtime_order,
            expected=expected_ids,
        )
    if runtime_manifest and runtime_manifest.get("tile_order") != expected_ids:
        _issue(issues, "RUNTIME_DECLARED_ORDER_MISMATCH", "runtime", "tile_order runtime incorrect.")

    runtime_by_id: dict[str, dict[str, Any]] = {}
    for entry in runtime_entries:
        if not isinstance(entry, dict) or not isinstance(entry.get("id"), str):
            _issue(issues, "INVALID_RUNTIME_ENTRY", "runtime", "Entrée runtime invalide.")
            continue
        tile_id = entry["id"]
        if tile_id in runtime_by_id:
            _issue(issues, "DUPLICATE_RUNTIME_ID", "runtime", "Identifiant runtime dupliqué.", tile_id=tile_id)
        runtime_by_id[tile_id] = entry

    runtime_hash_groups: dict[str, list[str]] = defaultdict(list)
    runtime_pixel_hash_groups: dict[str, list[str]] = defaultdict(list)
    runtime_arrays: dict[str, np.ndarray] = {}
    runtime_full_mismatch = 0
    runtime_interior_mismatch = 0
    runtime_gutter_mismatch = 0
    gutter_mask = np.ones((RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE), dtype=bool)
    gutter_mask[GUTTER : GUTTER + CANONICAL_TILE_SIZE, GUTTER : GUTTER + CANONICAL_TILE_SIZE] = False

    for order_index, tile_id in enumerate(expected_ids):
        row, column = divmod(order_index, GRID_SIZE)
        entry = runtime_by_id.get(tile_id)
        if entry is None:
            _issue(issues, "MISSING_RUNTIME_ENTRY", "runtime", "Tuile absente du manifest runtime.", tile_id=tile_id)
            continue
        expected_metadata = _expected_runtime_manifest_metadata(row, column)
        expected_metadata["order_index"] = order_index
        metadata_mismatches = {
            key: {"expected": value, "actual": entry.get(key)}
            for key, value in expected_metadata.items()
            if entry.get(key) != value
        }
        if entry.get("source_master_sha256") != source["sha256"]:
            metadata_mismatches["source_master_sha256"] = {
                "expected": source["sha256"],
                "actual": entry.get("source_master_sha256"),
            }
        if metadata_mismatches:
            _issue(
                issues,
                "RUNTIME_METADATA_MISMATCH",
                "runtime",
                "Métadonnées runtime/gutter incohérentes.",
                tile_id=tile_id,
                mismatches=metadata_mismatches,
            )
        tile_path = _safe_bundle_file(runtime_dir, entry.get("file"))
        if tile_path is None:
            _issue(issues, "INVALID_RUNTIME_PATH", "runtime", "Chemin de tuile runtime invalide.", tile_id=tile_id)
            continue
        if not tile_path.is_file():
            _issue(issues, "MISSING_RUNTIME_TILE", "runtime", "Fichier runtime absent.", tile_id=tile_id)
            continue
        actual_hash = _sha256_file(tile_path)
        runtime_hash_groups[actual_hash].append(tile_id)
        if actual_hash != entry.get("png_sha256"):
            _issue(issues, "RUNTIME_HASH_MISMATCH", "runtime", "Hash PNG runtime incohérent.", tile_id=tile_id)
        try:
            tile_pixels, tile_mode = _load_tile(tile_path)
        except Exception as exc:
            _issue(issues, "UNREADABLE_RUNTIME_TILE", "runtime", str(exc), tile_id=tile_id)
            continue
        runtime_arrays[tile_id] = tile_pixels
        runtime_pixel_hash_groups[_sha256_pixels(tile_pixels)].append(tile_id)
        if tile_mode != mode or tile_pixels.shape[:2] != (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE):
            _issue(
                issues,
                "RUNTIME_IMAGE_CONTRACT_MISMATCH",
                "runtime",
                "Mode ou dimensions de tuile runtime invalides.",
                tile_id=tile_id,
                mode=tile_mode,
                shape=list(tile_pixels.shape),
            )
            continue
        if _sha256_pixels(tile_pixels) != entry.get("pixel_sha256"):
            _issue(issues, "RUNTIME_PIXEL_HASH_MISMATCH", "runtime", "Hash pixel runtime incohérent.", tile_id=tile_id)
        expected_pixels = _runtime_expected(master, row, column)
        mismatch_map = np.any(tile_pixels != expected_pixels, axis=2)
        full_diff = int(np.count_nonzero(mismatch_map))
        gutter_diff = int(np.count_nonzero(mismatch_map & gutter_mask))
        interior_diff = int(np.count_nonzero(mismatch_map & ~gutter_mask))
        runtime_full_mismatch += full_diff
        runtime_gutter_mismatch += gutter_diff
        runtime_interior_mismatch += interior_diff
        if full_diff:
            _issue(
                issues,
                "RUNTIME_PIXEL_ALTERATION",
                "runtime",
                "La tuile runtime ne correspond pas à l'échantillonnage attendu du master.",
                tile_id=tile_id,
                different_pixels=full_diff,
                gutter_different_pixels=gutter_diff,
                interior_different_pixels=interior_diff,
            )

    for duplicate_hash, tile_ids in runtime_hash_groups.items():
        if len(tile_ids) > 1:
            _issue(
                issues,
                "DUPLICATE_RUNTIME_TILE",
                "runtime",
                "Plusieurs tuiles runtime ont un contenu PNG identique.",
                sha256=duplicate_hash,
                tile_ids=tile_ids,
            )
    for duplicate_hash, tile_ids in runtime_pixel_hash_groups.items():
        if len(tile_ids) > 1 and not any(
            issue["code"] == "DUPLICATE_RUNTIME_TILE"
            and set(issue.get("details", {}).get("tile_ids", [])) == set(tile_ids)
            for issue in issues
        ):
            _issue(
                issues,
                "DUPLICATE_RUNTIME_PIXELS",
                "runtime",
                "Plusieurs tuiles runtime ont des pixels identiques.",
                pixel_sha256=duplicate_hash,
                tile_ids=tile_ids,
            )
    expected_runtime_files = {f"{tile_id}_g{GUTTER}.png" for tile_id in expected_ids}
    actual_runtime_files = {path.name for path in (runtime_dir / "tiles").glob("*.png")} if (runtime_dir / "tiles").is_dir() else set()
    for extra in sorted(actual_runtime_files - expected_runtime_files):
        _issue(issues, "EXTRA_RUNTIME_TILE", "runtime", "PNG runtime non déclaré.", file=extra)

    boundaries_checked = 0
    boundaries_passed = 0
    boundary_gutter_mismatch = 0
    for row in range(GRID_SIZE):
        for column in range(GRID_SIZE - 1):
            left_id = _tile_id(row, column)
            right_id = _tile_id(row, column + 1)
            left = runtime_arrays.get(left_id)
            right = runtime_arrays.get(right_id)
            if left is None or right is None or left.shape[:2] != (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE) or right.shape[:2] != (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE):
                continue
            boundaries_checked += 1
            expected_left = _runtime_expected(master, row, column)
            expected_right = _runtime_expected(master, row, column + 1)
            mismatch = _pixel_difference_count(left[:, -GUTTER:, :], expected_left[:, -GUTTER:, :])
            mismatch += _pixel_difference_count(right[:, :GUTTER, :], expected_right[:, :GUTTER, :])
            boundary_gutter_mismatch += mismatch
            if mismatch == 0:
                boundaries_passed += 1
    for row in range(GRID_SIZE - 1):
        for column in range(GRID_SIZE):
            top_id = _tile_id(row, column)
            bottom_id = _tile_id(row + 1, column)
            top = runtime_arrays.get(top_id)
            bottom = runtime_arrays.get(bottom_id)
            if top is None or bottom is None or top.shape[:2] != (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE) or bottom.shape[:2] != (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE):
                continue
            boundaries_checked += 1
            expected_top = _runtime_expected(master, row, column)
            expected_bottom = _runtime_expected(master, row + 1, column)
            mismatch = _pixel_difference_count(top[-GUTTER:, :, :], expected_top[-GUTTER:, :, :])
            mismatch += _pixel_difference_count(bottom[:GUTTER, :, :], expected_bottom[:GUTTER, :, :])
            boundary_gutter_mismatch += mismatch
            if mismatch == 0:
                boundaries_passed += 1

    if boundaries_checked != 40 or boundaries_passed != 40 or boundary_gutter_mismatch:
        _issue(
            issues,
            "INTERNAL_GUTTER_BOUNDARY_FAILURE",
            "runtime",
            "Les 40 frontières internes ne sont pas toutes prouvées par pixels voisins réels.",
            checked=boundaries_checked,
            passed=boundaries_passed,
            different_pixels=boundary_gutter_mismatch,
        )
    result["runtime"].update(
        {
            "boundary_gutter_mismatch_pixel_count": boundary_gutter_mismatch,
            "full_tile_mismatch_pixel_count": runtime_full_mismatch,
            "gutter_mismatch_pixel_count": runtime_gutter_mismatch,
            "internal_boundaries_checked": boundaries_checked,
            "internal_boundaries_passed": boundaries_passed,
            "interior_mismatch_pixel_count": runtime_interior_mismatch,
        }
    )

    canonical_issue = any(issue["scope"] in {"source", "canonical"} for issue in issues)
    runtime_issue = any(issue["scope"] in {"source", "runtime"} for issue in issues)
    canonical_ok = (
        not canonical_issue
        and reconstruction_diff == 0
        and canonical_pixel_alteration_count == 0
        and len(canonical_entries) == 25
    )
    gutters_ok = (
        not runtime_issue
        and runtime_gutter_mismatch == 0
        and runtime_interior_mismatch == 0
        and boundaries_checked == 40
        and boundaries_passed == 40
    )
    ready = canonical_ok and gutters_ok
    result["status"] = "PASS" if ready else "FAIL"
    result["verdicts"] = {
        "CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL": "YES" if canonical_ok else "NO",
        "READY_FOR_UIB_WAVE3_MASTER_INGEST": "YES" if ready else "NO",
        "RUNTIME_GUTTERS_FROM_TRUE_NEIGHBORS": "YES" if gutters_ok else "NO",
        "WORLD_MAP_MACRO_SLICER_WAVE3": "PASS" if ready else "FAIL",
    }
    return result
