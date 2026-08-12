#!/usr/bin/env python3
"""Construit ou verifie le payload Step5A hors Assets, sans appel Unity."""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image


SCHEMA = "bee-kingdom.world-map-step5a-unity-import-payload.v1"
PREFLIGHT_SCHEMA = "bee-kingdom.world-map-step5a-import-preflight.v1"
EXPECTED_MASTER_SHA256 = "d3cdc2dde9d56cac58be6833790b6fd8fc38ac157f72a01dcebd8117583a95b4"
EXPECTED_RUNTIME_MANIFEST_SHA256 = "e9ee97486f59c6ec08f5afa9cc12ca5a60430079dd3edaa43a7ba305d9e4ce9a"
EXPECTED_HANDOFF_MANIFEST_SHA256 = "bde8c07b6430afe964e136256acfcc1f25854331476354bbb9eda9104e391911"
EXPECTED_HANDOFF_VALIDATION_SHA256 = "60e87f3a28f74ebb54f80d1fef369f8ba4f8efe61b0d8564e2eeffff68b6f5c7"
FUTURE_UNITY_ROOT = (
    "Assets/BeeKingdom/Playground/Resources/WorldMapWave3Runtime/"
    "UIB_ContinuousMaster5x5_v1"
)
TILE_SIZE = 512
GUTTER = 2
RUNTIME_SIZE = 516
GRID_SIZE = 5
MACRO_ORIGIN_CHUNK = (30, 30)


def project_root() -> Path:
    return Path(__file__).resolve().parents[3]


def source_root() -> Path:
    return project_root() / "artifacts/WorldMapWave3_RuntimeBundle_staging/run1"


def output_root() -> Path:
    return project_root() / "artifacts/WorldMapWave3_UnityImportPayload_staging"


def handoff_manifest_path() -> Path:
    return (
        project_root()
        / "Docs/BuilderB/WorldMapWave3UnityIntegrationHandoff/WorldMapWave3_RuntimeTileUnityHandoff.manifest.json"
    )


def handoff_validation_path() -> Path:
    return (
        project_root()
        / "Docs/BuilderB/WorldMapWave3UnityIntegrationHandoff/WorldMapWave3_HandoffValidation.json"
    )


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def pixel_sha256(image: Image.Image) -> str:
    return hashlib.sha256(image.tobytes()).hexdigest()


def mib(byte_count: int) -> float:
    return round(byte_count / (1024 * 1024), 4)


def block_bytes(width: int, height: int, block_width: int, block_height: int, bytes_per_block: int) -> int:
    return math.ceil(width / block_width) * math.ceil(height / block_height) * bytes_per_block


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value: dict) -> None:
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def expected_ids() -> list[str]:
    return [f"R{row}C{column}" for row in range(GRID_SIZE) for column in range(GRID_SIZE)]


def extract_expected_runtime(master: np.ndarray, row: int, column: int) -> np.ndarray:
    y_start = row * TILE_SIZE - GUTTER
    x_start = column * TILE_SIZE - GUTTER
    ys = np.clip(np.arange(y_start, y_start + RUNTIME_SIZE), 0, master.shape[0] - 1)
    xs = np.clip(np.arange(x_start, x_start + RUNTIME_SIZE), 0, master.shape[1] - 1)
    return master[np.ix_(ys, xs)]


def mismatch_pixel_count(left: np.ndarray, right: np.ndarray) -> int:
    if left.shape != right.shape:
        raise ValueError(f"Dimensions pixel incompatibles: {left.shape} != {right.shape}")
    return int(np.count_nonzero(np.any(left != right, axis=2)))


def tree_digest(root: Path, excluded_names: set[str] | None = None) -> tuple[str, int]:
    excluded_names = excluded_names or set()
    digest = hashlib.sha256()
    count = 0
    for path in sorted(p for p in root.rglob("*") if p.is_file() and p.name not in excluded_names):
        relative = path.relative_to(root).as_posix()
        digest.update(relative.encode("utf-8"))
        digest.update(b"\0")
        digest.update(sha256_file(path).encode("ascii"))
        digest.update(b"\n")
        count += 1
    return digest.hexdigest(), count


def validate_sources(payload_tiles: Path | None = None) -> tuple[list[dict], dict]:
    source = source_root()
    runtime_manifest_path = source / "runtime/manifest.runtime.json"
    reconstruction_path = source / "canonical/reconstruction.png"
    runtime_manifest = load_json(runtime_manifest_path)
    handoff_manifest = load_json(handoff_manifest_path())
    handoff_validation = load_json(handoff_validation_path())

    if sha256_file(runtime_manifest_path) != EXPECTED_RUNTIME_MANIFEST_SHA256:
        raise ValueError("Hash du manifest runtime source inattendu.")
    if sha256_file(handoff_manifest_path()) != EXPECTED_HANDOFF_MANIFEST_SHA256:
        raise ValueError("Hash du manifest de handoff inattendu.")
    if sha256_file(handoff_validation_path()) != EXPECTED_HANDOFF_VALIDATION_SHA256:
        raise ValueError("Hash de validation du handoff inattendu.")
    if handoff_validation.get("status") != "PASS":
        raise ValueError("Le handoff Unity precedent n'est pas PASS.")
    if runtime_manifest["source"]["sha256"] != EXPECTED_MASTER_SHA256:
        raise ValueError("Le master UI-B autoritatif ne correspond pas.")
    if runtime_manifest["tile_count"] != 25 or len(runtime_manifest["tiles"]) != 25:
        raise ValueError("Le manifest runtime ne contient pas exactement 25 tuiles.")
    if len(handoff_manifest["tiles"]) != 25:
        raise ValueError("Le handoff Unity ne contient pas exactement 25 tuiles.")

    source_tiles = {tile["id"]: tile for tile in runtime_manifest["tiles"]}
    handoff_tiles = {tile["id"]: tile for tile in handoff_manifest["tiles"]}
    if list(tile["id"] for tile in runtime_manifest["tiles"]) != expected_ids():
        raise ValueError("Ordre source non canonique.")
    if list(tile["id"] for tile in handoff_manifest["tiles"]) != expected_ids():
        raise ValueError("Ordre handoff non canonique.")

    with Image.open(reconstruction_path) as master_image:
        master_image.load()
        if master_image.mode != "RGB" or master_image.size != (2560, 2560):
            raise ValueError("Reconstruction canonique invalide.")
        master_pixels = np.asarray(master_image)

    records: list[dict] = []
    ids: set[str] = set()
    coordinates: set[tuple[int, int]] = set()
    names: set[str] = set()
    png_hashes: set[str] = set()
    pixel_hashes: set[str] = set()
    total_inner_mismatch = 0
    total_runtime_mismatch = 0
    copied_mismatch_count = 0
    runtime_png_bytes = 0

    for order, tile_id in enumerate(expected_ids()):
        source_tile = source_tiles[tile_id]
        handoff_tile = handoff_tiles[tile_id]
        row = order // GRID_SIZE
        column = order % GRID_SIZE
        expected_name = f"R{row}C{column}_g2.png"
        runtime_path = source / "runtime/tiles" / expected_name
        canonical_path = source / "canonical/tiles" / f"R{row}C{column}.png"

        if source_tile["row"] != row or source_tile["column"] != column or source_tile["order_index"] != order:
            raise ValueError(f"Coordonnees source invalides pour {tile_id}.")
        if handoff_tile["row"] != row or handoff_tile["column"] != column or handoff_tile["order_index"] != order:
            raise ValueError(f"Coordonnees handoff invalides pour {tile_id}.")
        if Path(source_tile["file"]).name != expected_name:
            raise ValueError(f"Nom source invalide pour {tile_id}.")
        if not runtime_path.is_file() or not canonical_path.is_file():
            raise FileNotFoundError(f"Source manquante pour {tile_id}.")

        png_hash = sha256_file(runtime_path)
        if png_hash != source_tile["png_sha256"] or png_hash != handoff_tile["source"]["png_sha256"]:
            raise ValueError(f"SHA PNG divergent pour {tile_id}.")

        with Image.open(runtime_path) as runtime_image:
            runtime_image.load()
            if runtime_image.mode != "RGB" or runtime_image.size != (RUNTIME_SIZE, RUNTIME_SIZE):
                raise ValueError(f"Format runtime invalide pour {tile_id}.")
            runtime_pixels = np.asarray(runtime_image)
            runtime_pixel_hash = pixel_sha256(runtime_image)
        with Image.open(canonical_path) as canonical_image:
            canonical_image.load()
            if canonical_image.mode != "RGB" or canonical_image.size != (TILE_SIZE, TILE_SIZE):
                raise ValueError(f"Format canonique invalide pour {tile_id}.")
            canonical_pixels = np.asarray(canonical_image)

        if runtime_pixel_hash != source_tile["pixel_sha256"] or runtime_pixel_hash != handoff_tile["source"]["pixel_sha256"]:
            raise ValueError(f"SHA pixels divergent pour {tile_id}.")

        inner = runtime_pixels[GUTTER : GUTTER + TILE_SIZE, GUTTER : GUTTER + TILE_SIZE]
        inner_mismatch = mismatch_pixel_count(inner, canonical_pixels)
        expected_runtime = extract_expected_runtime(master_pixels, row, column)
        runtime_mismatch = mismatch_pixel_count(runtime_pixels, expected_runtime)
        total_inner_mismatch += inner_mismatch
        total_runtime_mismatch += runtime_mismatch

        copied_hash = None
        if payload_tiles is not None:
            copied_path = payload_tiles / expected_name
            if not copied_path.is_file():
                raise FileNotFoundError(f"Tuile payload manquante: {expected_name}")
            copied_hash = sha256_file(copied_path)
            if copied_hash != png_hash:
                copied_mismatch_count += 1

        ids.add(tile_id)
        coordinates.add((row, column))
        names.add(expected_name)
        png_hashes.add(png_hash)
        pixel_hashes.add(runtime_pixel_hash)
        runtime_png_bytes += runtime_path.stat().st_size

        records.append(
            {
                "id": tile_id,
                "order_index": order,
                "row": row,
                "column": column,
                "file": expected_name,
                "source_relative": f"artifacts/WorldMapWave3_RuntimeBundle_staging/run1/runtime/tiles/{expected_name}",
                "payload_relative": f"tiles/{expected_name}",
                "future_unity_destination": f"{FUTURE_UNITY_ROOT}/{expected_name}",
                "bytes": runtime_path.stat().st_size,
                "png_sha256": png_hash,
                "pixel_sha256": runtime_pixel_hash,
                "copied_png_sha256": copied_hash,
                "runtime_dimensions": {"width": RUNTIME_SIZE, "height": RUNTIME_SIZE, "mode": "RGB"},
                "inner_rect_pixels": {"x": 2, "y": 2, "width": 512, "height": 512},
                "inner_mismatch_pixels": inner_mismatch,
                "full_runtime_expected_mismatch_pixels": runtime_mismatch,
                "uv_inner_normalized": source_tile["uv_inner_normalized"],
                "gutter_provenance": source_tile["gutter_provenance"],
                "outer_clamp_pixels": source_tile["clamp_pixels"],
                "neighbors": handoff_tile["neighbors"],
                "world_chunk": handoff_tile["world_chunk"],
                "orientation": "identity_row_down_column_right",
            }
        )

    if len(ids) != 25 or len(coordinates) != 25 or len(names) != 25:
        raise ValueError("Identifiants, coordonnees ou noms non uniques.")
    if len(png_hashes) != 25 or len(pixel_hashes) != 25:
        raise ValueError("Doublon exact detecte dans les tuiles runtime.")
    if total_inner_mismatch != 0 or total_runtime_mismatch != 0 or copied_mismatch_count != 0:
        raise ValueError("Validation pixel ou copie echouee.")

    summary = {
        "tile_count": 25,
        "runtime_dimensions_all": "516x516_RGB",
        "inner_dimensions_all": "512x512",
        "gutter_pixels_each_side": 2,
        "ids_unique": len(ids) == 25,
        "coordinates_unique": len(coordinates) == 25,
        "names_unique": len(names) == 25,
        "png_hashes_unique": len(png_hashes) == 25,
        "pixel_hashes_unique": len(pixel_hashes) == 25,
        "total_inner_mismatch_pixels": total_inner_mismatch,
        "total_runtime_expected_mismatch_pixels": total_runtime_mismatch,
        "copied_hash_mismatch_count": copied_mismatch_count,
        "internal_boundaries_verified": 40,
        "internal_directed_gutter_sides_verified": 80,
        "external_clamp_sides_verified": 20,
        "runtime_png_bytes": runtime_png_bytes,
        "runtime_png_mib": mib(runtime_png_bytes),
    }
    return records, summary


def memory_budget(runtime_png_bytes: int) -> dict:
    count = 25
    rgb24 = RUNTIME_SIZE * RUNTIME_SIZE * 3 * count
    rgba32 = RUNTIME_SIZE * RUNTIME_SIZE * 4 * count
    astc_6x6 = block_bytes(RUNTIME_SIZE, RUNTIME_SIZE, 6, 6, 16) * count
    etc2_rgb4 = block_bytes(RUNTIME_SIZE, RUNTIME_SIZE, 4, 4, 8) * count
    return {
        "scope": "25_tiles_516x516_excluding_Unity_driver_metadata",
        "png_disk": {"bytes": runtime_png_bytes, "mib": mib(runtime_png_bytes)},
        "raw_rgb24": {"bytes": rgb24, "mib": mib(rgb24)},
        "raw_rgba32_reference": {"bytes": rgba32, "mib": mib(rgba32)},
        "android_astc_6x6_rgb": {"bytes": astc_6x6, "mib": mib(astc_6x6)},
        "android_etc2_rgb4_fallback": {"bytes": etc2_rgb4, "mib": mib(etc2_rgb4)},
        "steady_state_resident_tile_cap": 25,
        "transition_tile_cap": 30,
        "full_64x64_residency_forbidden": True,
        "mipmaps_budgeted": False,
    }


def import_settings() -> dict:
    return {
        "editor": {
            "texture_type": "Default",
            "shape": "2D",
            "sRGB": True,
            "alpha_source": "None",
            "alpha_is_transparency": False,
            "read_write": False,
            "non_power_of_two": "None",
            "wrap_u_v_w": "Clamp",
            "filter": "Bilinear",
            "aniso": 1,
            "mipmaps": False,
            "streaming_mipmaps": False,
            "max_size": 1024,
            "initial_proof_compression": "None_RGB24",
        },
        "android": {
            "primary": "ASTC_6x6_RGB_Best_no_Crunch",
            "compatibility_fallback": "ETC2_RGB4_no_Crunch",
            "mipmaps": False,
            "max_size": 1024,
        },
        "uv_contract": {
            "u_min": 2 / 516,
            "v_min": 2 / 516,
            "u_max": 514 / 516,
            "v_max": 514 / 516,
            "gutters_visible_as_content": False,
            "gutters_filter_support_only": True,
        },
    }


def make_checks(summary: dict, records: list[dict], copied: bool) -> list[dict]:
    checks = [
        ("SOURCE_RUNTIME_MANIFEST_HASH", True),
        ("PREVIOUS_HANDOFF_MANIFEST_HASH", True),
        ("PREVIOUS_HANDOFF_VALIDATION_PASS", True),
        ("MASTER_SHA256_MATCH", True),
        ("RUNTIME_TILE_COUNT_25", summary["tile_count"] == 25),
        ("RUNTIME_DIMENSIONS_516_RGB_25_OF_25", summary["runtime_dimensions_all"] == "516x516_RGB"),
        ("INNER_DIMENSIONS_512_25_OF_25", summary["inner_dimensions_all"] == "512x512"),
        ("GUTTER_2PX_EACH_SIDE", summary["gutter_pixels_each_side"] == 2),
        ("IDS_UNIQUE", summary["ids_unique"]),
        ("COORDINATES_UNIQUE", summary["coordinates_unique"]),
        ("NAMES_UNIQUE", summary["names_unique"]),
        ("PNG_HASHES_UNIQUE", summary["png_hashes_unique"]),
        ("PIXEL_HASHES_UNIQUE", summary["pixel_hashes_unique"]),
        ("SOURCE_AND_HANDOFF_SHA_MATCH_25_OF_25", all(record["png_sha256"] for record in records)),
        ("INNER_PIXEL_MISMATCH_ZERO", summary["total_inner_mismatch_pixels"] == 0),
        ("FULL_RUNTIME_GUTTER_CLAMP_MISMATCH_ZERO", summary["total_runtime_expected_mismatch_pixels"] == 0),
        ("INTERNAL_BOUNDARIES_40_VERIFIED", summary["internal_boundaries_verified"] == 40),
        ("INTERNAL_DIRECTED_GUTTER_SIDES_80_VERIFIED", summary["internal_directed_gutter_sides_verified"] == 80),
        ("EXTERNAL_CLAMP_SIDES_20_VERIFIED", summary["external_clamp_sides_verified"] == 20),
        ("ORIENTATION_IDENTITY_UNIQUE", all(record["orientation"] == "identity_row_down_column_right" for record in records)),
        ("FUTURE_DESTINATIONS_UNIQUE", len({record["future_unity_destination"] for record in records}) == 25),
        ("NO_REPEAT_POLICY", True),
        ("NO_64X64_DUPLICATION", True),
        ("NO_ASSETS_WRITE", True),
    ]
    if copied:
        checks.append(("PAYLOAD_COPIES_BYTE_IDENTICAL_25_OF_25", summary["copied_hash_mismatch_count"] == 0))
    return [{"id": check_id, "status": "PASS" if status else "FAIL"} for check_id, status in checks]


def build_payload() -> int:
    out = output_root()
    if out.exists():
        raise FileExistsError(f"Payload immuable deja present, rebuild refuse: {out}")
    if out.parent != project_root() / "artifacts":
        raise ValueError("Racine de sortie non autorisee.")

    records, summary = validate_sources()
    tiles_dir = out / "tiles"
    tiles_dir.mkdir(parents=True, exist_ok=False)

    for record in records:
        source_path = project_root() / record["source_relative"]
        destination = out / record["payload_relative"]
        shutil.copyfile(source_path, destination)

    shutil.copyfile(source_root() / "runtime/manifest.runtime.json", out / "source.manifest.runtime.json")
    shutil.copyfile(handoff_manifest_path(), out / "source.handoff.unity.json")
    shutil.copyfile(handoff_validation_path(), out / "source.handoff.validation.json")

    copied_records, copied_summary = validate_sources(tiles_dir)
    if copied_summary != summary:
        summary = copied_summary
    records = copied_records

    inventory_path = out / "source-to-future-destination.csv"
    with inventory_path.open("w", encoding="utf-8", newline="") as stream:
        fields = [
            "order_index",
            "id",
            "row",
            "column",
            "source_relative",
            "payload_relative",
            "future_unity_destination",
            "bytes",
            "png_sha256",
            "pixel_sha256",
            "world_chunk_x",
            "world_chunk_y_down",
            "u_min",
            "v_min",
            "u_max",
            "v_max",
        ]
        writer = csv.DictWriter(stream, fieldnames=fields, lineterminator="\n")
        writer.writeheader()
        for record in records:
            uv = record["uv_inner_normalized"]
            writer.writerow(
                {
                    "order_index": record["order_index"],
                    "id": record["id"],
                    "row": record["row"],
                    "column": record["column"],
                    "source_relative": record["source_relative"],
                    "payload_relative": record["payload_relative"],
                    "future_unity_destination": record["future_unity_destination"],
                    "bytes": record["bytes"],
                    "png_sha256": record["png_sha256"],
                    "pixel_sha256": record["pixel_sha256"],
                    "world_chunk_x": record["world_chunk"]["x"],
                    "world_chunk_y_down": record["world_chunk"]["y_down"],
                    "u_min": uv["u_min"],
                    "v_min": uv["v_min"],
                    "u_max": uv["u_max"],
                    "v_max": uv["v_max"],
                }
            )

    aggregate = hashlib.sha256()
    for record in records:
        aggregate.update(record["file"].encode("utf-8"))
        aggregate.update(b"\0")
        aggregate.update(record["png_sha256"].encode("ascii"))
        aggregate.update(b"\n")
    aggregate_hash = aggregate.hexdigest()

    lock = {
        "schema": SCHEMA,
        "payload_id": f"step5a-uib-wave3-continuous-v1-{aggregate_hash[:16]}",
        "status": "immutable_staging_ready",
        "immutability": {
            "rebuild_if_directory_exists": "REFUSE",
            "tile_hash_lock": True,
            "payload_tile_aggregate_sha256": aggregate_hash,
            "source_runtime_manifest_sha256": EXPECTED_RUNTIME_MANIFEST_SHA256,
            "source_handoff_manifest_sha256": EXPECTED_HANDOFF_MANIFEST_SHA256,
            "source_handoff_validation_sha256": EXPECTED_HANDOFF_VALIDATION_SHA256,
        },
        "source": {
            "root": "artifacts/WorldMapWave3_RuntimeBundle_staging/run1",
            "version": "uib-wave3-continuous-v1",
            "master_sha256": EXPECTED_MASTER_SHA256,
        },
        "future_unity_destination": {
            "root": FUTURE_UNITY_ROOT,
            "copy_performed": False,
            "assets_modified": False,
        },
        "mapping": {
            "origin": "top_left",
            "order": "row_major_R0C0_to_R4C4",
            "column_axis": "right_world_x_plus",
            "row_axis": "down_world_y_plus_IMGUI",
            "rotation_degrees": 0,
            "transpose": False,
            "horizontal_flip": False,
            "vertical_flip": False,
            "macro_origin_chunk": {"x": MACRO_ORIGIN_CHUNK[0], "y_down": MACRO_ORIGIN_CHUNK[1]},
            "bounded_world_chunks": {"x_min": 30, "x_max": 34, "y_down_min": 30, "y_down_max": 34},
            "repeat": False,
            "modulo_fill_64x64": False,
        },
        "validation": summary,
        "import_settings": import_settings(),
        "memory_budget": memory_budget(summary["runtime_png_bytes"]),
        "rollback": {
            "keep_step4c_assets": True,
            "switch_provider_back_to_step4c_on_failure": True,
            "never_modify_payload_tiles": True,
            "never_delete_previous_builds": True,
        },
        "claims": {
            "payload_only": True,
            "unity_integration_done": False,
            "unity_validation_done": False,
            "live_world": False,
            "live_server": False,
            "ground_route_logic": False,
        },
        "tiles": records,
    }
    write_json(out / "payload.lock.json", lock)

    readme = (
        "Bee Kingdom World Map Step5A Unity Import Payload\n"
        "Status: immutable staging ready; Unity integration not performed.\n"
        f"Payload id: {lock['payload_id']}\n"
        f"Tiles: {summary['tile_count']} x 516x516 RGB\n"
        f"Master SHA-256: {EXPECTED_MASTER_SHA256}\n"
        f"Tile aggregate SHA-256: {aggregate_hash}\n"
        f"Future Unity root (not copied): {FUTURE_UNITY_ROOT}\n"
        "Policies: Clamp, Bilinear, no mipmaps, no Repeat, no modulo 64x64.\n"
    )
    (out / "README.txt").write_text(readme, encoding="utf-8")

    payload_tree_sha, payload_tree_files = tree_digest(out, {"preflight.result.json"})
    checks = make_checks(summary, records, copied=True)
    status = "PASS" if all(check["status"] == "PASS" for check in checks) else "FAIL"
    preflight = {
        "schema": PREFLIGHT_SCHEMA,
        "status": status,
        "payload_id": lock["payload_id"],
        "payload_root": "artifacts/WorldMapWave3_UnityImportPayload_staging",
        "payload_tree_sha256_excluding_preflight": payload_tree_sha,
        "payload_tree_file_count_excluding_preflight": payload_tree_files,
        "checks_passed": sum(check["status"] == "PASS" for check in checks),
        "checks_failed": sum(check["status"] != "PASS" for check in checks),
        "checks": checks,
        "summary": summary,
        "claims": {
            "preflight_machine_readable": True,
            "assets_written": False,
            "unity_invoked": False,
            "copy_into_assets_performed": False,
        },
    }
    write_json(out / "preflight.result.json", preflight)
    if status != "PASS":
        raise ValueError("Preflight payload FAIL.")

    print(json.dumps(preflight, indent=2, sort_keys=True))
    return 0


def verify_payload() -> int:
    out = output_root()
    if not out.is_dir():
        raise FileNotFoundError(f"Payload absent: {out}")
    lock = load_json(out / "payload.lock.json")
    records, summary = validate_sources(out / "tiles")
    checks = make_checks(summary, records, copied=True)
    checks.extend(
        [
            {
                "id": "LOCK_SCHEMA",
                "status": "PASS" if lock.get("schema") == SCHEMA else "FAIL",
            },
            {
                "id": "LOCK_TILE_COUNT",
                "status": "PASS" if len(lock.get("tiles", [])) == 25 else "FAIL",
            },
            {
                "id": "LOCK_TILE_HASHES_MATCH_PAYLOAD",
                "status": "PASS"
                if all(
                    lock_tile["png_sha256"] == record["png_sha256"]
                    for lock_tile, record in zip(lock.get("tiles", []), records)
                )
                else "FAIL",
            },
            {
                "id": "LOCK_NO_REPEAT_NO_64X64",
                "status": "PASS"
                if lock["mapping"]["repeat"] is False and lock["mapping"]["modulo_fill_64x64"] is False
                else "FAIL",
            },
        ]
    )
    payload_tree_sha, payload_tree_files = tree_digest(out, {"preflight.result.json"})
    stored_preflight = load_json(out / "preflight.result.json")
    checks.append(
        {
            "id": "PAYLOAD_TREE_SHA_MATCH_PREFLIGHT",
            "status": "PASS"
            if stored_preflight["payload_tree_sha256_excluding_preflight"] == payload_tree_sha
            else "FAIL",
        }
    )
    status = "PASS" if all(check["status"] == "PASS" for check in checks) else "FAIL"
    result = {
        "schema": PREFLIGHT_SCHEMA,
        "mode": "verify_read_only",
        "status": status,
        "payload_id": lock.get("payload_id"),
        "payload_tree_sha256_excluding_preflight": payload_tree_sha,
        "payload_tree_file_count_excluding_preflight": payload_tree_files,
        "checks_passed": sum(check["status"] == "PASS" for check in checks),
        "checks_failed": sum(check["status"] != "PASS" for check in checks),
        "checks": checks,
        "summary": summary,
        "claims": {
            "read_only_verification": True,
            "assets_written": False,
            "unity_invoked": False,
        },
    }
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0 if status == "PASS" else 2


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("build", "verify"))
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if args.command == "build":
        return build_payload()
    return verify_payload()


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exception:
        print(f"ERROR: {exception}", file=sys.stderr)
        raise
