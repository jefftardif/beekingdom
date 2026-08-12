#!/usr/bin/env python3
"""Audit post-import Step5A en lecture seule sur les fichiers Unity produits."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image


SCHEMA = "bee-kingdom.world-map-step5a-post-import-audit.v1"
MASTER_SHA256 = "d3cdc2dde9d56cac58be6833790b6fd8fc38ac157f72a01dcebd8117583a95b4"
HANDOFF_MANIFEST_SHA256 = "bde8c07b6430afe964e136256acfcc1f25854331476354bbb9eda9104e391911"
GRID_SIZE = 5
TILE_SIZE = 512
GUTTER = 2
RUNTIME_SIZE = TILE_SIZE + GUTTER * 2


def project_root() -> Path:
    return Path(__file__).resolve().parents[3]


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def sha256_pixels(image: Image.Image) -> str:
    return hashlib.sha256(image.tobytes()).hexdigest()


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def expected_ids() -> list[str]:
    return [f"R{row}C{column}" for row in range(GRID_SIZE) for column in range(GRID_SIZE)]


def expected_names() -> list[str]:
    return [f"{tile_id}_g2.png" for tile_id in expected_ids()]


def mismatch_pixels(left: np.ndarray, right: np.ndarray) -> int:
    if left.shape != right.shape:
        return max(int(np.prod(left.shape)), int(np.prod(right.shape)))
    return int(np.count_nonzero(np.any(left != right, axis=2)))


def expected_runtime_pixels(master: np.ndarray, row: int, column: int) -> np.ndarray:
    y_start = row * TILE_SIZE - GUTTER
    x_start = column * TILE_SIZE - GUTTER
    ys = np.clip(np.arange(y_start, y_start + RUNTIME_SIZE), 0, master.shape[0] - 1)
    xs = np.clip(np.arange(x_start, x_start + RUNTIME_SIZE), 0, master.shape[1] - 1)
    return master[np.ix_(ys, xs)]


def snapshot_tree(root: Path) -> dict:
    files: list[dict] = []
    digest = hashlib.sha256()
    if root.is_dir():
        paths = sorted(path for path in root.rglob("*") if path.is_file())
    elif root.is_file():
        paths = [root]
        root = root.parent
    else:
        paths = []
    for path in paths:
        relative = path.relative_to(root).as_posix()
        file_hash = sha256_file(path)
        files.append({"relative": relative, "bytes": path.stat().st_size, "sha256": file_hash})
        digest.update(relative.encode("utf-8"))
        digest.update(b"\0")
        digest.update(file_hash.encode("ascii"))
        digest.update(b"\n")
    return {"file_count": len(files), "tree_sha256": digest.hexdigest(), "files": files}


def regex_int(text: str, key: str) -> int | None:
    match = re.search(rf"^\s*{re.escape(key)}:\s*(-?\d+)\s*$", text, re.MULTILINE)
    return int(match.group(1)) if match else None


def regex_text(text: str, key: str) -> str | None:
    match = re.search(rf"^\s*{re.escape(key)}:\s*(.*?)\s*$", text, re.MULTILINE)
    return match.group(1) if match else None


def platform_settings(text: str) -> dict[str, dict]:
    profiles: dict[str, dict] = {}
    for block in re.split(r"(?=^\s*- serializedVersion:\s*\d+\s*$)", text, flags=re.MULTILINE):
        target = regex_text(block, "buildTarget")
        if not target:
            continue
        profiles[target] = {
            "max_texture_size": regex_int(block, "maxTextureSize"),
            "resize_algorithm": regex_int(block, "resizeAlgorithm"),
            "texture_format": regex_int(block, "textureFormat"),
            "texture_compression": regex_int(block, "textureCompression"),
            "compression_quality": regex_int(block, "compressionQuality"),
            "crunched_compression": regex_int(block, "crunchedCompression"),
            "allows_alpha_splitting": regex_int(block, "allowsAlphaSplitting"),
            "overridden": regex_int(block, "overridden"),
        }
    return profiles


def parse_meta(path: Path) -> dict:
    text = path.read_text(encoding="utf-8-sig")
    guid = regex_text(text, "guid")
    sprites_empty = bool(re.search(r"^\s*sprites:\s*\[\]\s*$", text, re.MULTILINE))
    settings = {
        "enable_mip_map": regex_int(text, "enableMipMap"),
        "sRGB_texture": regex_int(text, "sRGBTexture"),
        "is_readable": regex_int(text, "isReadable"),
        "streaming_mipmaps": regex_int(text, "streamingMipmaps"),
        "filter_mode": regex_int(text, "filterMode"),
        "aniso": regex_int(text, "aniso"),
        "wrap_u": regex_int(text, "wrapU"),
        "wrap_v": regex_int(text, "wrapV"),
        "wrap_w": regex_int(text, "wrapW"),
        "nPOT_scale": regex_int(text, "nPOTScale"),
        "texture_type": regex_int(text, "textureType"),
        "texture_shape": regex_int(text, "textureShape"),
        "alpha_usage": regex_int(text, "alphaUsage"),
        "alpha_is_transparency": regex_int(text, "alphaIsTransparency"),
        "sprite_mode_serialized": regex_int(text, "spriteMode"),
        "sprites_empty": sprites_empty,
    }
    required = {
        "enable_mip_map": 0,
        "sRGB_texture": 1,
        "is_readable": 0,
        "streaming_mipmaps": 0,
        "filter_mode": 1,
        "aniso": 1,
        "wrap_u": 1,
        "wrap_v": 1,
        "wrap_w": 1,
        "nPOT_scale": 0,
        "texture_type": 0,
        "texture_shape": 1,
        "alpha_usage": 0,
        "alpha_is_transparency": 0,
        "sprites_empty": True,
    }
    mismatches = [
        {"setting": key, "expected": expected, "actual": settings.get(key)}
        for key, expected in required.items()
        if settings.get(key) != expected
    ]
    profiles = platform_settings(text)
    default_profile = profiles.get("DefaultTexturePlatform")
    android_profile = profiles.get("Android")
    observations: list[str] = []
    if settings["texture_type"] == 0 and settings["sprite_mode_serialized"] not in (None, 0):
        observations.append(
            "spriteMode est serialise mais inactif: textureType=Default et aucune slice Sprite n'est declaree."
        )
    if android_profile and android_profile.get("overridden") == 0:
        observations.append(
            "Le profil Android herite des reglages plateforme; ASTC 6x6 n'est pas verrouille par override."
        )
    return {
        "file": path.name,
        "sha256": sha256_file(path),
        "guid": guid,
        "settings": settings,
        "required_mismatches": mismatches,
        "required_settings_pass": not mismatches,
        "platforms": profiles,
        "default_profile": default_profile,
        "android_profile": android_profile,
        "observations": observations,
    }


def tile_side_mismatches(runtime: np.ndarray, expected: np.ndarray, row: int, column: int) -> list[dict]:
    sides = {
        "top": (runtime[:GUTTER, :, :], expected[:GUTTER, :, :], row > 0),
        "bottom": (runtime[-GUTTER:, :, :], expected[-GUTTER:, :, :], row < GRID_SIZE - 1),
        "left": (runtime[:, :GUTTER, :], expected[:, :GUTTER, :], column > 0),
        "right": (runtime[:, -GUTTER:, :], expected[:, -GUTTER:, :], column < GRID_SIZE - 1),
    }
    return [
        {
            "side": side,
            "provenance": "true_master_neighbor_pixels" if internal else "outer_edge_clamp",
            "mismatch_pixels": mismatch_pixels(actual, wanted),
        }
        for side, (actual, wanted, internal) in sides.items()
    ]


def audit(output_path: Path) -> tuple[dict, bool]:
    root = project_root()
    payload_root = root / "artifacts/WorldMapWave3_UnityImportPayload_staging"
    import_root = (
        root
        / "Assets/BeeKingdom/Playground/Resources/WorldMapWave3Runtime/UIB_ContinuousMaster5x5_v1"
    )
    source_root = root / "artifacts/WorldMapWave3_RuntimeBundle_staging/run1"
    payload_lock_path = payload_root / "payload.lock.json"
    payload_manifest_path = payload_root / "source.handoff.unity.json"
    imported_manifest_path = import_root / "manifest.runtime.unity.json"
    reconstruction_path = source_root / "canonical/reconstruction.png"

    started_at = datetime.now(timezone.utc).isoformat()
    protected_before = snapshot_tree(import_root)

    errors: list[str] = []
    warnings: list[str] = []
    for required_path in (
        payload_lock_path,
        payload_manifest_path,
        imported_manifest_path,
        reconstruction_path,
    ):
        if not required_path.is_file():
            errors.append(f"Fichier requis absent: {required_path}")

    if errors:
        protected_after = snapshot_tree(import_root)
        result = {
            "schema": SCHEMA,
            "status": "FAIL",
            "started_at_utc": started_at,
            "completed_at_utc": datetime.now(timezone.utc).isoformat(),
            "errors": errors,
            "warnings": warnings,
            "protected_import_tree_before": protected_before,
            "protected_import_tree_after": protected_after,
            "protected_import_tree_stable": protected_before == protected_after,
            "verdicts": {
                "POST_IMPORT_25_TILE_PAYLOAD": "FAIL",
                "UNITY_IMPORT_SETTINGS": "FAIL",
                "RUNTIME_GUTTERS_AND_HASHES": "FAIL",
                "READY_FOR_DEMO_SUPPORT": "NO",
            },
        }
        write_json(output_path, result)
        return result, False

    payload_lock = load_json(payload_lock_path)
    payload_manifest = load_json(payload_manifest_path)
    imported_manifest = load_json(imported_manifest_path)
    payload_tiles = {tile["id"]: tile for tile in payload_lock.get("tiles", [])}
    imported_manifest_tiles = {tile["id"]: tile for tile in imported_manifest.get("tiles", [])}

    with Image.open(reconstruction_path) as master_image:
        master_image.load()
        master_mode = master_image.mode
        master_size = master_image.size
        master_pixels = np.asarray(master_image.convert("RGB"))

    master_ok = (
        master_mode == "RGB"
        and master_size == (GRID_SIZE * TILE_SIZE, GRID_SIZE * TILE_SIZE)
        and imported_manifest.get("master", {}).get("sha256") == MASTER_SHA256
        and payload_lock.get("source", {}).get("master_sha256") == MASTER_SHA256
    )

    actual_png_names = sorted(path.name for path in import_root.glob("*.png"))
    actual_meta_names = sorted(path.name for path in import_root.glob("*.png.meta"))
    missing_png = sorted(set(expected_names()) - set(actual_png_names))
    extra_png = sorted(set(actual_png_names) - set(expected_names()))
    missing_meta = sorted(f"{name}.meta" for name in expected_names() if f"{name}.meta" not in actual_meta_names)
    extra_meta = sorted(set(actual_meta_names) - {f"{name}.meta" for name in expected_names()})

    tile_results: list[dict] = []
    total_payload_mismatch = 0
    total_master_mismatch = 0
    total_inner_mismatch = 0
    internal_gutter_sides_passed = 0
    outer_clamp_sides_passed = 0
    png_hashes: list[str] = []
    pixel_hashes: list[str] = []

    for order, tile_id in enumerate(expected_ids()):
        row = order // GRID_SIZE
        column = order % GRID_SIZE
        name = f"{tile_id}_g2.png"
        imported_path = import_root / name
        payload_path = payload_root / "tiles" / name
        record: dict = {
            "id": tile_id,
            "order_index": order,
            "row": row,
            "column": column,
            "file": name,
        }
        if not imported_path.is_file() or not payload_path.is_file():
            record["status"] = "FAIL"
            record["error"] = "Tuile importee ou payload absente."
            tile_results.append(record)
            continue

        imported_hash = sha256_file(imported_path)
        payload_hash = sha256_file(payload_path)
        payload_record = payload_tiles.get(tile_id, {})
        manifest_record = imported_manifest_tiles.get(tile_id, {})
        expected_hash = payload_record.get("png_sha256")

        with Image.open(imported_path) as imported_image:
            imported_image.load()
            image_mode = imported_image.mode
            image_size = imported_image.size
            pixel_hash = sha256_pixels(imported_image)
            imported_pixels = np.asarray(imported_image.convert("RGB"))
        with Image.open(payload_path) as payload_image:
            payload_image.load()
            payload_pixels = np.asarray(payload_image.convert("RGB"))

        expected_pixels = expected_runtime_pixels(master_pixels, row, column)
        payload_mismatch = mismatch_pixels(imported_pixels, payload_pixels)
        master_mismatch = mismatch_pixels(imported_pixels, expected_pixels)
        inner = imported_pixels[GUTTER : GUTTER + TILE_SIZE, GUTTER : GUTTER + TILE_SIZE]
        expected_inner = master_pixels[
            row * TILE_SIZE : (row + 1) * TILE_SIZE,
            column * TILE_SIZE : (column + 1) * TILE_SIZE,
        ]
        inner_mismatch = mismatch_pixels(inner, expected_inner)
        side_results = tile_side_mismatches(imported_pixels, expected_pixels, row, column)
        for side in side_results:
            if side["mismatch_pixels"] == 0:
                if side["provenance"] == "true_master_neighbor_pixels":
                    internal_gutter_sides_passed += 1
                else:
                    outer_clamp_sides_passed += 1

        manifest_source = manifest_record.get("source", {})
        manifest_name = Path(manifest_source.get("relative_to_bundle", "")).name
        manifest_hash = manifest_source.get("png_sha256")
        manifest_pixel_hash = manifest_source.get("pixel_sha256")
        record_pass = all(
            (
                image_mode == "RGB",
                image_size == (RUNTIME_SIZE, RUNTIME_SIZE),
                imported_hash == payload_hash == expected_hash == manifest_hash,
                pixel_hash == payload_record.get("pixel_sha256") == manifest_pixel_hash,
                manifest_name == name,
                payload_mismatch == 0,
                master_mismatch == 0,
                inner_mismatch == 0,
                all(side["mismatch_pixels"] == 0 for side in side_results),
            )
        )
        record.update(
            {
                "status": "PASS" if record_pass else "FAIL",
                "dimensions": {"width": image_size[0], "height": image_size[1], "mode": image_mode},
                "imported_png_sha256": imported_hash,
                "payload_png_sha256": payload_hash,
                "expected_png_sha256": expected_hash,
                "pixel_sha256": pixel_hash,
                "payload_mismatch_pixels": payload_mismatch,
                "master_runtime_mismatch_pixels": master_mismatch,
                "inner_512_mismatch_pixels": inner_mismatch,
                "gutter_sides": side_results,
            }
        )
        tile_results.append(record)
        total_payload_mismatch += payload_mismatch
        total_master_mismatch += master_mismatch
        total_inner_mismatch += inner_mismatch
        png_hashes.append(imported_hash)
        pixel_hashes.append(pixel_hash)

    meta_results = [parse_meta(import_root / f"{name}.meta") for name in expected_names() if (import_root / f"{name}.meta").is_file()]
    guid_values = [record["guid"] for record in meta_results if record["guid"]]
    import_settings_pass = (
        len(meta_results) == 25
        and not missing_meta
        and not extra_meta
        and all(record["required_settings_pass"] for record in meta_results)
        and len(guid_values) == len(set(guid_values)) == 25
    )

    android_override_count = sum(
        1
        for record in meta_results
        if record.get("android_profile") and record["android_profile"].get("overridden") == 1
    )
    if android_override_count != 25:
        warnings.append(
            "Les 25 profils Android heritent des reglages plateforme; ASTC 6x6/max 1024 n'est pas explicitement verrouille. "
            "Ce point n'affecte pas les criteres import demandes (Clamp/Bilinear/mipmaps/NPOT/type Default)."
        )
    stale_sprite_mode_count = sum(
        1 for record in meta_results if record["settings"].get("sprite_mode_serialized") not in (None, 0)
    )
    if stale_sprite_mode_count:
        warnings.append(
            f"{stale_sprite_mode_count}/25 metas conservent spriteMode=2 comme champ serialise inactif; "
            "textureType=0 (Default) et sprites=[] confirment un import non-Sprite."
        )

    imported_manifest_hash = sha256_file(imported_manifest_path)
    payload_manifest_hash = sha256_file(payload_manifest_path)
    manifest_pass = all(
        (
            imported_manifest_hash == payload_manifest_hash == HANDOFF_MANIFEST_SHA256,
            imported_manifest == payload_manifest,
            imported_manifest.get("schema") == "bee-kingdom.world-map-wave3-unity-integration-handoff.v1",
            len(imported_manifest.get("tiles", [])) == 25,
            [tile.get("id") for tile in imported_manifest.get("tiles", [])] == expected_ids(),
            imported_manifest.get("mapping", {}).get("source_order") == "row_major_R0C0_to_R4C4",
            imported_manifest.get("mapping", {}).get("source_origin") == "top_left",
            imported_manifest.get("mapping", {}).get("transpose") is False,
            imported_manifest.get("mapping", {}).get("horizontal_flip") is False,
            imported_manifest.get("mapping", {}).get("vertical_flip") is False,
            imported_manifest.get("mapping", {}).get("rotate_degrees") == 0,
        )
    )

    uniqueness_pass = (
        len(png_hashes) == len(set(png_hashes)) == 25
        and len(pixel_hashes) == len(set(pixel_hashes)) == 25
    )
    payload_pass = all(
        (
            len(actual_png_names) == 25,
            not missing_png,
            not extra_png,
            manifest_pass,
            uniqueness_pass,
            len(tile_results) == 25,
            all(record.get("status") == "PASS" for record in tile_results),
        )
    )
    gutters_pass = all(
        (
            master_ok,
            total_payload_mismatch == 0,
            total_master_mismatch == 0,
            total_inner_mismatch == 0,
            internal_gutter_sides_passed == 80,
            outer_clamp_sides_passed == 20,
        )
    )

    protected_after = snapshot_tree(import_root)
    protected_stable = protected_before == protected_after
    if not protected_stable:
        errors.append("Le dossier importe a change pendant la lecture; resultat non stable.")

    ready = payload_pass and import_settings_pass and gutters_pass and protected_stable
    status = "PASS" if ready else "FAIL"
    result = {
        "schema": SCHEMA,
        "status": status,
        "started_at_utc": started_at,
        "completed_at_utc": datetime.now(timezone.utc).isoformat(),
        "scope": {
            "payload_root": str(payload_root),
            "import_root_read_only": str(import_root),
            "source_reconstruction_read_only": str(reconstruction_path),
            "output_written_outside_assets": str(output_path),
            "unity_launched": False,
            "assets_written": False,
            "png_sources_modified": False,
        },
        "payload_inventory": {
            "expected_png_count": 25,
            "actual_png_count": len(actual_png_names),
            "expected_png_names": expected_names(),
            "actual_png_names": actual_png_names,
            "missing_png": missing_png,
            "extra_png": extra_png,
            "exact_duplicate_png_hash_count": len(png_hashes) - len(set(png_hashes)),
            "exact_duplicate_pixel_hash_count": len(pixel_hashes) - len(set(pixel_hashes)),
            "unique_tile_hashes": uniqueness_pass,
        },
        "manifest": {
            "status": "PASS" if manifest_pass else "FAIL",
            "imported_sha256": imported_manifest_hash,
            "payload_sha256": payload_manifest_hash,
            "expected_sha256": HANDOFF_MANIFEST_SHA256,
            "byte_and_json_identical": imported_manifest_hash == payload_manifest_hash and imported_manifest == payload_manifest,
            "tile_count": len(imported_manifest.get("tiles", [])),
            "master_sha256": imported_manifest.get("master", {}).get("sha256"),
            "orientation": {
                "origin": imported_manifest.get("mapping", {}).get("source_origin"),
                "order": imported_manifest.get("mapping", {}).get("source_order"),
                "transpose": imported_manifest.get("mapping", {}).get("transpose"),
                "horizontal_flip": imported_manifest.get("mapping", {}).get("horizontal_flip"),
                "vertical_flip": imported_manifest.get("mapping", {}).get("vertical_flip"),
                "rotate_degrees": imported_manifest.get("mapping", {}).get("rotate_degrees"),
            },
        },
        "pixel_validation": {
            "master_ok": master_ok,
            "master_sha256": MASTER_SHA256,
            "master_dimensions": {"width": master_size[0], "height": master_size[1], "mode": master_mode},
            "total_payload_mismatch_pixels": total_payload_mismatch,
            "total_master_runtime_mismatch_pixels": total_master_mismatch,
            "total_inner_512_mismatch_pixels": total_inner_mismatch,
            "internal_boundaries_undirected": 40,
            "internal_gutter_sides_expected": 80,
            "internal_gutter_sides_passed": internal_gutter_sides_passed,
            "outer_clamp_sides_expected": 20,
            "outer_clamp_sides_passed": outer_clamp_sides_passed,
            "tiles": tile_results,
        },
        "unity_import_settings": {
            "status": "PASS" if import_settings_pass else "FAIL",
            "png_meta_expected": 25,
            "png_meta_actual": len(meta_results),
            "missing_meta": missing_meta,
            "extra_meta": extra_meta,
            "unique_guid_count": len(set(guid_values)),
            "android_override_count": android_override_count,
            "inactive_serialized_sprite_mode_count": stale_sprite_mode_count,
            "metas": meta_results,
        },
        "read_only_stability": {
            "status": "PASS" if protected_stable else "FAIL",
            "protected_import_tree_before_sha256": protected_before["tree_sha256"],
            "protected_import_tree_after_sha256": protected_after["tree_sha256"],
            "protected_import_file_count_before": protected_before["file_count"],
            "protected_import_file_count_after": protected_after["file_count"],
            "stable": protected_stable,
            "audit_write_root": str(output_path.parent),
        },
        "warnings": warnings,
        "errors": errors,
        "verdicts": {
            "POST_IMPORT_25_TILE_PAYLOAD": "PASS" if payload_pass else "FAIL",
            "UNITY_IMPORT_SETTINGS": "PASS" if import_settings_pass else "FAIL",
            "RUNTIME_GUTTERS_AND_HASHES": "PASS" if gutters_pass else "FAIL",
            "READY_FOR_DEMO_SUPPORT": "YES" if ready else "NO",
        },
        "claims": {
            "post_import_static_audit_only": True,
            "unity_play_mode_tested": False,
            "runtime_render_tested": False,
            "android_device_tested": False,
            "live_world_claimed": False,
            "live_server_claimed": False,
        },
    }
    write_json(output_path, result)
    return result, ready


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output",
        type=Path,
        default=Path(__file__).resolve().parent / "post_import_audit.result.json",
        help="Fichier JSON de resultat, obligatoirement hors Assets.",
    )
    args = parser.parse_args()
    root = project_root().resolve()
    output = args.output.resolve()
    assets_root = (root / "Assets").resolve()
    if output == assets_root or assets_root in output.parents:
        print("REFUS: le resultat d'audit ne peut pas etre ecrit sous Assets.", file=sys.stderr)
        return 2
    result, ready = audit(output)
    print(json.dumps({"status": result["status"], "verdicts": result["verdicts"], "output": str(output)}, indent=2))
    return 0 if ready else 1


if __name__ == "__main__":
    raise SystemExit(main())
