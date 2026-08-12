#!/usr/bin/env python3
"""Genere le manifeste de handoff Unity Wave 3 sans ecrire dans Assets."""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
import struct
from pathlib import Path


SCHEMA = "bee-kingdom.world-map-wave3-unity-integration-handoff.v1"
EXPECTED_MASTER_SHA256 = "d3cdc2dde9d56cac58be6833790b6fd8fc38ac157f72a01dcebd8117583a95b4"
EXPECTED_TREE_DIGEST = "2176c7c5b81108e006014a1310095c9570d414963539bc0766dd4c023456fd2f"
DESTINATION_ROOT = (
    "Assets/BeeKingdom/Playground/Resources/WorldMapWave3Runtime/"
    "UIB_ContinuousMaster5x5_v1"
)
MACRO_ORIGIN_CHUNK = (30, 30)
CENTER_CHUNK = (32, 32)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def png_header(path: Path) -> dict[str, int | str]:
    with path.open("rb") as stream:
        signature = stream.read(8)
        if signature != b"\x89PNG\r\n\x1a\n":
            raise ValueError(f"PNG illisible: {path}")
        length = struct.unpack(">I", stream.read(4))[0]
        chunk_type = stream.read(4)
        if chunk_type != b"IHDR" or length != 13:
            raise ValueError(f"IHDR PNG invalide: {path}")
        width, height, bit_depth, color_type, _, _, _ = struct.unpack(">IIBBBBB", stream.read(13))

    color_names = {
        0: "grayscale",
        2: "RGB",
        3: "indexed",
        4: "grayscale_alpha",
        6: "RGBA",
    }
    return {
        "width": width,
        "height": height,
        "bit_depth": bit_depth,
        "color_type": color_type,
        "color_mode": color_names.get(color_type, "unknown"),
    }


def mib(byte_count: int) -> float:
    return round(byte_count / (1024 * 1024), 4)


def block_bytes(width: int, height: int, block_width: int, block_height: int, bytes_per_block: int) -> int:
    return math.ceil(width / block_width) * math.ceil(height / block_height) * bytes_per_block


def neighbor_id(row: int, column: int, dr: int, dc: int) -> str | None:
    nr, nc = row + dr, column + dc
    if 0 <= nr < 5 and 0 <= nc < 5:
        return f"R{nr}C{nc}"
    return None


def parse_args() -> argparse.Namespace:
    project_root = Path(__file__).resolve().parents[3]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--bundle",
        type=Path,
        default=project_root / "artifacts/WorldMapWave3_RuntimeBundle_staging/run1",
    )
    parser.add_argument(
        "--summary",
        type=Path,
        default=project_root / "artifacts/WorldMapWave3_RuntimeBundle_staging/real_ingest_summary.json",
    )
    parser.add_argument("--output-dir", type=Path, default=Path(__file__).resolve().parent)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    project_root = Path(__file__).resolve().parents[3]
    bundle = args.bundle.resolve()
    runtime_manifest_path = bundle / "runtime/manifest.runtime.json"
    runtime_manifest = json.loads(runtime_manifest_path.read_text(encoding="utf-8"))
    summary = json.loads(args.summary.resolve().read_text(encoding="utf-8"))

    source = runtime_manifest["source"]
    if source["sha256"] != EXPECTED_MASTER_SHA256:
        raise ValueError("Le hash du master ne correspond pas au master UI-B autoritatif.")
    if runtime_manifest["version"] != "uib-wave3-continuous-v1":
        raise ValueError("Version de bundle inattendue.")
    if runtime_manifest["tile_count"] != 25 or len(runtime_manifest["tiles"]) != 25:
        raise ValueError("Le bundle doit contenir exactement 25 tuiles runtime.")
    if summary["run_comparison"]["tree_digest_run1"] != EXPECTED_TREE_DIGEST:
        raise ValueError("Le digest de l'arbre run1 ne correspond pas au lot valide.")

    expected_ids = [f"R{row}C{column}" for row in range(5) for column in range(5)]
    actual_ids = [tile["id"] for tile in runtime_manifest["tiles"]]
    if actual_ids != expected_ids:
        raise ValueError("Ordre des tuiles incorrect: R0C0..R4C4 attendu.")

    unity_paths = [
        "Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs",
        "Assets/BeeKingdom/Playground/WorldMapWave4ManifestContentProvider.cs",
        "Assets/BeeKingdom/Playground/Editor/WorldMapMmoFullscreenFoundationSceneBuilder.cs",
    ]
    unity_baseline = []
    for relative in unity_paths:
        path = project_root / relative
        unity_baseline.append(
            {
                "path": relative,
                "bytes": path.stat().st_size,
                "sha256": sha256_file(path),
                "access": "read_only_for_handoff",
            }
        )

    tiles = []
    runtime_png_bytes = 0
    aggregate = hashlib.sha256()
    for source_tile in runtime_manifest["tiles"]:
        row = int(source_tile["row"])
        column = int(source_tile["column"])
        source_relative = f"runtime/{source_tile['file']}"
        source_path = bundle / source_relative
        if not source_path.is_file():
            raise FileNotFoundError(source_path)

        file_hash = sha256_file(source_path)
        if file_hash != source_tile["png_sha256"]:
            raise ValueError(f"Hash runtime invalide pour {source_tile['id']}")
        header = png_header(source_path)
        if header["width"] != 516 or header["height"] != 516:
            raise ValueError(f"Dimensions runtime invalides pour {source_tile['id']}")
        if header["color_mode"] != "RGB":
            raise ValueError(f"Mode couleur runtime invalide pour {source_tile['id']}")

        file_size = source_path.stat().st_size
        runtime_png_bytes += file_size
        aggregate.update(source_relative.replace("\\", "/").encode("utf-8"))
        aggregate.update(b"\0")
        aggregate.update(file_hash.encode("ascii"))
        aggregate.update(b"\n")

        world_chunk_x = MACRO_ORIGIN_CHUNK[0] + column
        world_chunk_y = MACRO_ORIGIN_CHUNK[1] + row
        world_x = world_chunk_x * 512
        world_y = world_chunk_y * 512
        future_destination = f"{DESTINATION_ROOT}/{source_path.name}"
        clamp = source_tile["clamp_pixels"]
        tiles.append(
            {
                "id": source_tile["id"],
                "order_index": source_tile["order_index"],
                "row": row,
                "column": column,
                "source": {
                    "relative_to_bundle": source_relative.replace("\\", "/"),
                    "bytes": file_size,
                    "png_sha256": file_hash,
                    "pixel_sha256": source_tile["pixel_sha256"],
                    "source_master_sha256": source_tile["source_master_sha256"],
                    "png": header,
                },
                "future_unity_destination": future_destination,
                "canonical_crop": source_tile["canonical_crop"],
                "macro_origin_pixels": source_tile["macro_origin"],
                "runtime_dimensions": source_tile["dimensions"],
                "inner_rect_pixels": source_tile["inner_rect"],
                "uv_inner_normalized": source_tile["uv_inner_normalized"],
                "source_window_unclamped": source_tile["source_window_unclamped"],
                "gutter_provenance": source_tile["gutter_provenance"],
                "outer_clamp_pixels": clamp,
                "outer_clamp_sides": {
                    "north_top": clamp["top"] > 0,
                    "east_right": clamp["right"] > 0,
                    "south_bottom": clamp["bottom"] > 0,
                    "west_left": clamp["left"] > 0,
                },
                "neighbors": {
                    "north_top": neighbor_id(row, column, -1, 0),
                    "east_right": neighbor_id(row, column, 0, 1),
                    "south_bottom": neighbor_id(row, column, 1, 0),
                    "west_left": neighbor_id(row, column, 0, -1),
                },
                "window_offset_from_center": {"x": column - 2, "y_down": row - 2},
                "world_chunk": {"x": world_chunk_x, "y_down": world_chunk_y},
                "world_rect": {"x": world_x, "y_down": world_y, "width": 512, "height": 512},
            }
        )

    tile_width = 516
    tile_height = 516
    tile_count = 25
    rgb24 = tile_width * tile_height * 3 * tile_count
    rgba32 = tile_width * tile_height * 4 * tile_count
    etc2_rgb = block_bytes(tile_width, tile_height, 4, 4, 8) * tile_count
    etc2_rgba = block_bytes(tile_width, tile_height, 4, 4, 16) * tile_count
    astc_6x6 = block_bytes(tile_width, tile_height, 6, 6, 16) * tile_count
    astc_8x8 = block_bytes(tile_width, tile_height, 8, 8, 16) * tile_count

    manifest = {
        "schema": SCHEMA,
        "handoff_status": "prepared_not_integrated",
        "source_bundle": {
            "root": "artifacts/WorldMapWave3_RuntimeBundle_staging/run1",
            "version": runtime_manifest["version"],
            "runtime_manifest": "runtime/manifest.runtime.json",
            "runtime_manifest_sha256": sha256_file(runtime_manifest_path),
            "run1_file_count": summary["run_comparison"]["file_count_run1"],
            "run1_tree_digest_sha256": summary["run_comparison"]["tree_digest_run1"],
            "run1_run2_byte_identical": summary["run_comparison"]["byte_identical"],
            "runtime_tile_aggregate_sha256": aggregate.hexdigest(),
        },
        "master": source,
        "validated_pipeline_evidence": {
            "canonical_reconstruction_different_pixels": summary["canonical"]["reconstruction_pixel_difference_count_run1"],
            "runtime_boundaries_passed": summary["runtime"]["boundaries_passed_run1"],
            "runtime_boundaries_checked": summary["runtime"]["boundaries_checked_run1"],
            "runtime_gutter_mismatch_pixels": summary["runtime"]["gutter_mismatch_pixel_count_run1"],
            "runtime_interior_mismatch_pixels": summary["runtime"]["interior_mismatch_pixel_count_run1"],
            "uv_exact_tile_count": summary["generated_vs_uib"]["run1"]["uv_exact_count"],
        },
        "gates_before_integration": {
            "builder_c_runtime_gutter_validation": "REQUIRED_PASS",
            "qa_unity_integration_authorization": "REQUIRED_PASS",
            "builder_a_action": "DO_NOT_COPY_OR_SWITCH_PROVIDER_BEFORE_BOTH_GATES",
        },
        "future_unity_inventory": {
            "root": DESTINATION_ROOT,
            "runtime_manifest_destination": f"{DESTINATION_ROOT}/manifest.runtime.unity.json",
            "runtime_tile_count": 25,
            "copy_performed_by_builder_b": False,
            "existing_step4c_assets_overwritten": False,
        },
        "renderer_contract": {
            "observed_renderer": "Texture2D_IMGUI_GUI.DrawTextureWithTexCoords",
            "sprite_renderer": False,
            "raw_image": False,
            "pixels_per_unit": "not_applicable_for_current_IMGUI_renderer",
            "world_units_per_inner_tile": 512,
            "inner_source_pixels_per_world_unit": 1,
            "draw_rule": "draw_512_world_unit_rect_with_inner_uv_only",
            "do_not_draw_full_516_as_visible_content": True,
            "do_not_pixel_snap_primary_art": True,
            "keep_hud_overlays_and_aerial_flights_in_separate_fixed_or_world_layers": True,
        },
        "mapping": {
            "source_origin": "top_left",
            "source_order": "row_major_R0C0_to_R4C4",
            "column_axis": "column_increases_right_to_world_chunk_x_plus",
            "row_axis": "row_increases_down_to_world_chunk_y_plus_and_IMGUI_screen_y_plus",
            "transpose": False,
            "rotate_degrees": 0,
            "horizontal_flip": False,
            "vertical_flip": False,
            "runtime_uv_source_note": runtime_manifest["uv_convention"],
            "macro_origin_world_chunk": {"x": MACRO_ORIGIN_CHUNK[0], "y_down": MACRO_ORIGIN_CHUNK[1]},
            "center_world_chunk": {"x": CENTER_CHUNK[0], "y_down": CENTER_CHUNK[1]},
            "macro_world_rect": {
                "x": MACRO_ORIGIN_CHUNK[0] * 512,
                "y_down": MACRO_ORIGIN_CHUNK[1] * 512,
                "width": 2560,
                "height": 2560,
            },
            "active_window": {
                "rows": 5,
                "columns": 5,
                "radius_chunks": 2,
                "steady_state_resident_tiles": 25,
                "transition_hard_cap_tiles": 30,
            },
            "out_of_bounds_policy": "missing_or_step4c_fallback_never_modulo_repeat",
        },
        "uv_contract": {
            "texture_dimensions": {"width": 516, "height": 516},
            "inner_rect_pixels": {"x": 2, "y": 2, "width": 512, "height": 512},
            "u_min": 2 / 516,
            "v_min": 2 / 516,
            "u_max": 514 / 516,
            "v_max": 514 / 516,
            "uv_width": 512 / 516,
            "uv_height": 512 / 516,
            "visible_content_samples_gutters": False,
            "gutters_are_filter_support_only": True,
        },
        "recommended_unity_import": {
            "texture_type": "Default",
            "texture_shape": "2D",
            "sRGB": True,
            "alpha_source": "None_for_RGB_source",
            "alpha_is_transparency": False,
            "read_write": False,
            "non_power_of_two": "None",
            "wrap_u_v_w": "Clamp",
            "filter_mode": "Bilinear",
            "aniso_level": 1,
            "generate_mip_maps": False,
            "streaming_mipmaps": False,
            "max_size": 1024,
            "editor_orientation_validation_format": "RGB24_uncompressed",
            "android_primary": {
                "override": True,
                "format": "ASTC_6x6_RGB",
                "compression_quality": "Best",
                "crunched_compression": False,
            },
            "android_compatibility_fallback": {
                "format": "ETC2_RGB4",
                "use_only_if_device_profile_requires_it": True,
                "crunched_compression": False,
            },
        },
        "mobile_memory_budget": {
            "scope": "25_runtime_tiles_516x516_estimates_excluding_driver_and_Unity_metadata",
            "runtime_png_disk": {"bytes": runtime_png_bytes, "mib": mib(runtime_png_bytes)},
            "raw_rgb24": {"bytes": rgb24, "mib": mib(rgb24)},
            "raw_rgba32": {"bytes": rgba32, "mib": mib(rgba32)},
            "etc2_rgb4": {"bytes": etc2_rgb, "mib": mib(etc2_rgb)},
            "etc2_rgba8": {"bytes": etc2_rgba, "mib": mib(etc2_rgba)},
            "astc_6x6_rgb": {"bytes": astc_6x6, "mib": mib(astc_6x6)},
            "astc_8x8_rgb": {"bytes": astc_8x8, "mib": mib(astc_8x8)},
            "mipmap_multiplier_if_later_enabled": "approximately_1.3333_not_budgeted",
            "policy": {
                "active_visible_window_max": "5x5_25_tiles_steady_state",
                "boundary_transition_hard_cap": "30_tiles_while_one_incoming_stripe_replaces_one_outgoing_stripe",
                "release_outside_window": True,
                "read_write_disabled_avoids_uncompressed_cpu_copy": True,
                "never_resident_full_64x64": True,
            },
        },
        "unity_read_only_baseline": unity_baseline,
        "claims": {
            "handoff_only": True,
            "unity_integration_done": False,
            "unity_validation_done": False,
            "live_world_delivered": False,
            "live_server": False,
            "official_persistent_world": False,
            "ground_route_logic": False,
            "aerial_flights_must_remain_world_space_and_route_independent": True,
        },
        "tiles": tiles,
    }

    output_dir = args.output_dir.resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    manifest_output = output_dir / "WorldMapWave3_RuntimeTileUnityHandoff.manifest.json"
    manifest_output.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    csv_output = output_dir / "WorldMapWave3_SourceDestinationInventory.csv"
    fieldnames = [
        "order_index",
        "id",
        "row",
        "column",
        "source_relative_to_bundle",
        "source_bytes",
        "source_png_sha256",
        "future_unity_destination",
        "world_chunk_x",
        "world_chunk_y_down",
        "north_top",
        "east_right",
        "south_bottom",
        "west_left",
        "outer_clamp_top",
        "outer_clamp_right",
        "outer_clamp_bottom",
        "outer_clamp_left",
    ]
    with csv_output.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=fieldnames, lineterminator="\n")
        writer.writeheader()
        for tile in tiles:
            writer.writerow(
                {
                    "order_index": tile["order_index"],
                    "id": tile["id"],
                    "row": tile["row"],
                    "column": tile["column"],
                    "source_relative_to_bundle": tile["source"]["relative_to_bundle"],
                    "source_bytes": tile["source"]["bytes"],
                    "source_png_sha256": tile["source"]["png_sha256"],
                    "future_unity_destination": tile["future_unity_destination"],
                    "world_chunk_x": tile["world_chunk"]["x"],
                    "world_chunk_y_down": tile["world_chunk"]["y_down"],
                    "north_top": tile["neighbors"]["north_top"] or "",
                    "east_right": tile["neighbors"]["east_right"] or "",
                    "south_bottom": tile["neighbors"]["south_bottom"] or "",
                    "west_left": tile["neighbors"]["west_left"] or "",
                    "outer_clamp_top": tile["outer_clamp_pixels"]["top"],
                    "outer_clamp_right": tile["outer_clamp_pixels"]["right"],
                    "outer_clamp_bottom": tile["outer_clamp_pixels"]["bottom"],
                    "outer_clamp_left": tile["outer_clamp_pixels"]["left"],
                }
            )

    print(f"manifest={manifest_output}")
    print(f"inventory={csv_output}")
    print("tiles=25")
    print(f"master_sha256={source['sha256']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
