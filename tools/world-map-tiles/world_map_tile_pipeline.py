#!/usr/bin/env python
"""Offline world map tile pipeline for Bee Kingdom.

This tool is intentionally independent from Unity. It reads a master map image,
cuts it into stable visual tiles, writes a JSON manifest, validates coverage,
reconstructs the source pixels, and produces a small contact sheet.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import shutil
from pathlib import Path
from typing import Dict, Iterable, List, Tuple

from PIL import Image, ImageChops, UnidentifiedImageError


SUPPORTED_FORMATS = {"PNG"}


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def stable_json_dump(data: object, path: Path) -> None:
    path.write_text(json.dumps(data, ensure_ascii=True, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def tile_id(atlas_id: str, version: str, x: int, y: int) -> str:
    return f"{atlas_id}_v{version}_x{x:04d}_y{y:04d}"


def ensure_empty_dir(path: Path) -> None:
    if path.exists():
        shutil.rmtree(path)
    path.mkdir(parents=True, exist_ok=True)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Cut a world map master image into deterministic tiles.")
    parser.add_argument("--source", required=True, help="Master image path, e.g. C:/projets/beekingdom/carte.png")
    parser.add_argument("--output", required=True, help="Output directory for manifest, tiles and validation artifacts")
    parser.add_argument("--tile-size", type=int, default=512, help="Square tile size in pixels")
    parser.add_argument("--world-id", required=True, help="Stable world id")
    parser.add_argument("--atlas-id", required=True, help="Stable atlas id")
    parser.add_argument("--version", required=True, help="Atlas version string")
    parser.add_argument("--origin-x", type=int, default=0, help="Logical world X origin for source pixel 0")
    parser.add_argument("--origin-y", type=int, default=0, help="Logical world Y origin for source pixel 0")
    parser.add_argument(
        "--edge-mode",
        choices=("actual", "transparent-padding"),
        default="actual",
        help="How to handle right/bottom non-multiple edges",
    )
    parser.add_argument("--contact-sheet-max", type=int, default=128, help="Max tile thumbnail size in contact sheet")
    return parser.parse_args()


def open_source(source: Path) -> Image.Image:
    try:
        image = Image.open(source)
        image.load()
    except (OSError, UnidentifiedImageError) as exc:
        raise SystemExit(f"Invalid source image: {source} ({exc})")

    if image.format not in SUPPORTED_FORMATS:
        raise SystemExit(f"Unsupported source format: {image.format}. Supported: {', '.join(sorted(SUPPORTED_FORMATS))}")
    if image.width <= 0 or image.height <= 0:
        raise SystemExit("Invalid source dimensions.")
    return image.convert("RGBA")


def build_tiles(args: argparse.Namespace) -> Dict[str, object]:
    source = Path(args.source).resolve()
    output = Path(args.output).resolve()
    if not source.exists():
        raise SystemExit(f"Source does not exist: {source}")
    if args.tile_size <= 0:
        raise SystemExit("--tile-size must be positive")

    image = open_source(source)
    source_hash = sha256_file(source)
    cols = math.ceil(image.width / args.tile_size)
    rows = math.ceil(image.height / args.tile_size)
    expected_count = cols * rows

    ensure_empty_dir(output)
    tiles_dir = output / "tiles"
    validation_dir = output / "validation"
    tiles_dir.mkdir(parents=True, exist_ok=True)
    validation_dir.mkdir(parents=True, exist_ok=True)

    tiles: List[Dict[str, object]] = []
    tile_hashes: List[str] = []

    for y in range(rows):
        for x in range(cols):
            left = x * args.tile_size
            top = y * args.tile_size
            width = min(args.tile_size, image.width - left)
            height = min(args.tile_size, image.height - top)
            crop = image.crop((left, top, left + width, top + height))
            if args.edge_mode == "transparent-padding" and (width != args.tile_size or height != args.tile_size):
                padded = Image.new("RGBA", (args.tile_size, args.tile_size), (0, 0, 0, 0))
                padded.paste(crop, (0, 0))
                tile_image = padded
                stored_width = args.tile_size
                stored_height = args.tile_size
            else:
                tile_image = crop
                stored_width = width
                stored_height = height

            tid = tile_id(args.atlas_id, args.version, x, y)
            rel_file = Path("tiles") / f"{tid}.png"
            tile_path = output / rel_file
            tile_image.save(tile_path, format="PNG", optimize=False, compress_level=6)
            tile_hash = sha256_file(tile_path)
            tile_hashes.append(tile_hash)

            neighbors = {
                "n": tile_id(args.atlas_id, args.version, x, y - 1) if y > 0 else None,
                "e": tile_id(args.atlas_id, args.version, x + 1, y) if x < cols - 1 else None,
                "s": tile_id(args.atlas_id, args.version, x, y + 1) if y < rows - 1 else None,
                "w": tile_id(args.atlas_id, args.version, x - 1, y) if x > 0 else None,
            }

            tiles.append(
                {
                    "id": tid,
                    "tile_x": x,
                    "tile_y": y,
                    "source_rect": {"x": left, "y": top, "width": width, "height": height},
                    "stored_dimensions": {"width": stored_width, "height": stored_height},
                    "edge": {
                        "mode": args.edge_mode,
                        "is_edge_tile": width != args.tile_size or height != args.tile_size,
                        "transparent_padding": args.edge_mode == "transparent-padding",
                    },
                    "file": str(rel_file).replace("\\", "/"),
                    "sha256": tile_hash,
                    "neighbors": neighbors,
                    "world_rect": {
                        "x": args.origin_x + left,
                        "y": args.origin_y + top,
                        "width": width,
                        "height": height,
                    },
                }
            )

    manifest: Dict[str, object] = {
        "schema": "bee-kingdom.world-map-tile-atlas.v1",
        "world_id": args.world_id,
        "atlas_id": args.atlas_id,
        "version": args.version,
        "source": {
            "path": str(source).replace("\\", "/"),
            "format": "PNG",
            "width": image.width,
            "height": image.height,
            "sha256": source_hash,
        },
        "tile_settings": {
            "tile_size": args.tile_size,
            "columns": cols,
            "rows": rows,
            "expected_tile_count": expected_count,
            "edge_mode": args.edge_mode,
            "origin": {"x": args.origin_x, "y": args.origin_y},
            "resize": False,
            "pathfinding_or_routes": False,
            "visual_tiles_only": True,
        },
        "tiles": tiles,
    }

    stable_json_dump(manifest, output / "manifest.json")
    validation = validate_output(output, manifest, image)
    stable_json_dump(validation, output / "validation" / "validation.json")
    write_contact_sheet(output, manifest, args.contact_sheet_max)
    return manifest


def validate_output(output: Path, manifest: Dict[str, object], source_image: Image.Image) -> Dict[str, object]:
    settings = manifest["tile_settings"]
    cols = int(settings["columns"])
    rows = int(settings["rows"])
    expected_count = int(settings["expected_tile_count"])
    tiles = manifest["tiles"]

    coverage = [[0 for _ in range(source_image.width)] for _ in range(source_image.height)]
    reconstructed = Image.new("RGBA", source_image.size, (0, 0, 0, 0))
    tile_by_coord: Dict[Tuple[int, int], Dict[str, object]] = {}
    neighbor_errors: List[str] = []

    for tile in tiles:
        tx = int(tile["tile_x"])
        ty = int(tile["tile_y"])
        tile_by_coord[(tx, ty)] = tile
        rect = tile["source_rect"]
        left = int(rect["x"])
        top = int(rect["y"])
        width = int(rect["width"])
        height = int(rect["height"])
        tile_path = output / str(tile["file"])
        with Image.open(tile_path) as tile_image:
            tile_rgba = tile_image.convert("RGBA")
            reconstructed.paste(tile_rgba.crop((0, 0, width, height)), (left, top))
        for py in range(top, top + height):
            row = coverage[py]
            for px in range(left, left + width):
                row[px] += 1

    holes = 0
    overlaps = 0
    for row in coverage:
        for value in row:
            if value == 0:
                holes += 1
            elif value > 1:
                overlaps += 1

    diff = ImageChops.difference(source_image, reconstructed)
    pixel_identical = diff.getbbox() is None
    reconstruction_path = output / "validation" / "reconstruction.png"
    reconstructed.save(reconstruction_path, format="PNG", optimize=False, compress_level=6)

    for tile in tiles:
        tx = int(tile["tile_x"])
        ty = int(tile["tile_y"])
        neighbors = tile["neighbors"]
        expected = {
            "n": tile_by_coord.get((tx, ty - 1), {}).get("id") if ty > 0 else None,
            "e": tile_by_coord.get((tx + 1, ty), {}).get("id") if tx < cols - 1 else None,
            "s": tile_by_coord.get((tx, ty + 1), {}).get("id") if ty < rows - 1 else None,
            "w": tile_by_coord.get((tx - 1, ty), {}).get("id") if tx > 0 else None,
        }
        for direction, expected_id in expected.items():
            if neighbors[direction] != expected_id:
                neighbor_errors.append(f"{tile['id']} {direction}: expected {expected_id}, got {neighbors[direction]}")

    result = {
        "tile_count_actual": len(tiles),
        "tile_count_expected": expected_count,
        "tile_count_ok": len(tiles) == expected_count,
        "coverage": {
            "complete_without_holes": holes == 0,
            "no_overlap": overlaps == 0,
            "hole_pixels": holes,
            "overlap_pixels": overlaps,
        },
        "reconstruction": {
            "pixel_identical_to_source": pixel_identical,
            "file": "validation/reconstruction.png",
            "sha256": sha256_file(reconstruction_path),
        },
        "neighbors": {
            "ok": len(neighbor_errors) == 0,
            "errors": neighbor_errors,
        },
        "determinism_note": "Compare manifest.json, validation/validation.json and tile hashes from two independent runs.",
    }
    if not result["tile_count_ok"] or holes != 0 or overlaps != 0 or not pixel_identical or neighbor_errors:
        raise SystemExit("Validation failed. See validation/validation.json")
    return result


def write_contact_sheet(output: Path, manifest: Dict[str, object], thumb_max: int) -> None:
    settings = manifest["tile_settings"]
    cols = int(settings["columns"])
    rows = int(settings["rows"])
    tile_size = int(settings["tile_size"])
    thumb = max(16, min(thumb_max, tile_size))
    sheet = Image.new("RGBA", (cols * thumb, rows * thumb), (12, 12, 12, 255))

    for tile in manifest["tiles"]:
        tx = int(tile["tile_x"])
        ty = int(tile["tile_y"])
        with Image.open(output / str(tile["file"])) as tile_image:
            preview = tile_image.convert("RGBA")
            preview.thumbnail((thumb, thumb), Image.Resampling.LANCZOS)
            cell = Image.new("RGBA", (thumb, thumb), (0, 0, 0, 0))
            cell.paste(preview, ((thumb - preview.width) // 2, (thumb - preview.height) // 2))
            sheet.paste(cell, (tx * thumb, ty * thumb))

    sheet.save(output / "validation" / "contact_sheet.png", format="PNG", optimize=False, compress_level=6)


def main() -> None:
    args = parse_args()
    build_tiles(args)


if __name__ == "__main__":
    main()
