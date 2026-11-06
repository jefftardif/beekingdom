#!/usr/bin/env python3
"""Read-only proposals from the ORIGINAL Wave6 tiles, not the broken mask.

The color heuristic is explicitly NOT a reviewed land/water source. It cannot
create an admitted semantic document or authorize placement. A separate human
review of the terrain, provenance and its shoreline is required.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
from itertools import groupby
import json
from pathlib import Path

from PIL import Image, ImageFilter

COORDINATE_CONTRACT = "wave6:origin=7,7;grid=50,50;core=512;gutter=2;y=down"
CLASSIFIER_VERSION = "visual-proposal-only-v1"
UNKNOWN = "Unknown"
LAND = "CandidateLand"
WATER = "CandidateWater"
COLOR = {UNKNOWN: (245, 60, 175), LAND: (80, 184, 95), WATER: (25, 151, 241)}
CORE_SIZE = 512
GUTTER = 2


def digest(path: Path) -> str:
    hasher = hashlib.sha256()
    with path.open("rb") as handle:
        for data in iter(lambda: handle.read(1024 * 1024), b""):
            hasher.update(data)
    return hasher.hexdigest()


def rough_class(rgb: tuple[int, int, int]) -> str:
    r, g, b = rgb
    # High precision, deliberately incomplete. Green hills, grey rock and
    # boundary colors not meeting these thresholds remain UNKNOWN.
    if b >= g - 2 and g >= r + 27 and b >= r + 42 and r < 120 and 58 <= g <= 165:
        return WATER
    if g >= b + 33 and g >= r + 10 and b <= 160 and g >= 95:
        return LAND
    return UNKNOWN


def propose(core: Image.Image, shoreline_pixels: int = 5) -> list[str]:
    if core.size != (CORE_SIZE, CORE_SIZE):
        raise ValueError("Expected exact 512x512 source core, no gutters")
    if shoreline_pixels < 0 or shoreline_pixels > 24:
        raise ValueError("Invalid shoreline margin")
    rgb = core.convert("RGB")
    pixels = rgb.get_flattened_data() if hasattr(rgb, "get_flattened_data") else rgb.getdata()
    values = [rough_class(rgb_px) for rgb_px in pixels]
    if not shoreline_pixels:
        return values
    water = Image.new("L", core.size)
    water.putdata([255 if value == WATER else 0 for value in values])
    land = Image.new("L", core.size)
    land.putdata([255 if value == LAND else 0 for value in values])
    diameter = shoreline_pixels * 2 + 1
    water_core = water.filter(ImageFilter.MinFilter(diameter))
    inside_water = list(water_core.get_flattened_data() if hasattr(water_core, "get_flattened_data") else water_core.getdata())
    land_core = land.filter(ImageFilter.MinFilter(diameter))
    inside_land = list(land_core.get_flattened_data() if hasattr(land_core, "get_flattened_data") else land_core.getdata())
    return [
        WATER if inside_water[i] == 255 else LAND if inside_land[i] == 255
        else UNKNOWN for i in range(len(values))
    ]


def rle(values: list[str]) -> list[dict]:
    if len(values) != CORE_SIZE * CORE_SIZE:
        raise ValueError("Raster must contain all 512^2 pixels")
    return [{"Length": sum(1 for _ in run), "Kind": kind}
            for kind, run in groupby(values)]


def manifest_tiles(manifest: dict) -> dict[tuple[int, int], dict]:
    grid = manifest.get("grid", {})
    if (grid.get("columns"), grid.get("rows"), grid.get("origin_chunk_x"),
        grid.get("origin_chunk_y"), grid.get("tile_size"),
        grid.get("gutter"), grid.get("runtime_tile_size")) != (
            50, 50, 7, 7, CORE_SIZE, GUTTER, CORE_SIZE + 2 * GUTTER):
        raise ValueError("Unsupported Wave6 coordinate/texture contract")
    by_chunk = {}
    for entry in manifest.get("tiles", []):
        key = (entry["chunk_x"], entry["chunk_y"])
        if key in by_chunk or not (7 <= key[0] < 57 and 7 <= key[1] < 57):
            raise ValueError("Duplicate or invalid tile index")
        by_chunk[key] = entry
    if len(by_chunk) != 2500:
        raise ValueError("Incomplete Wave6 tile inventory")
    return by_chunk


def render_preview(values: list[str], path: Path) -> None:
    image = Image.new("RGB", (CORE_SIZE, CORE_SIZE))
    image.putdata([COLOR[value] for value in values])
    image.save(path)


def build(manifest_path: Path, root: Path, out: Path,
          chunk_x: int, chunk_y: int, radius: int,
          shoreline_pixels: int) -> dict:
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    inventory = manifest_tiles(manifest)
    if not 0 <= radius <= 4:
        raise ValueError("Pilot radius must be from 0 to 4")
    out.mkdir(parents=True, exist_ok=True)
    result = {
        "schema": "bk-wave6-semantic-candidate-v1",
        "classificationVersion": CLASSIFIER_VERSION,
        "classificationSource": "unreviewed RGB heuristic + conservative shore erosion",
        "coordinateContract": COORDINATE_CONTRACT,
        "sourceManifestSha256": digest(manifest_path),
        "reviewed": False,
        "safeForServerPlacement": False,
        "safeForClientPlacement": False,
        "notes": ["No source pixel classification grants placement rights.",
                  "All image-derived classes require independent visual review.",
                  "Unknown pixels and missing tiles must remain denied.",
                  "The generated review stub authorizes ZERO placements."],
        "tiles": []
    }
    placeholder = {
        "Schema": "bk-semantic-terrain-v1",
        "Revision": "wave6-unreviewed-stub-v1",
        "ManifestSha256": result["sourceManifestSha256"],
        "CoordinateContract": COORDINATE_CONTRACT,
        "Tiles": []
    }
    totals = Counter()
    for y in range(chunk_y-radius, chunk_y+radius+1):
        for x in range(chunk_x-radius, chunk_x+radius+1):
            item = inventory.get((x, y))
            if item is None:
                continue
            path = root / item["file"]
            if not path.is_file() or digest(path) != item["runtime_sha256"]:
                raise ValueError(f"Wave6 source tile provenance failed: {x},{y}")
            with Image.open(path) as image:
                if image.size != (516, 516):
                    raise ValueError("Wave6 tile must include exactly 2px gutter")
                core = image.convert("RGB").crop((2, 2, 514, 514))
                values = propose(core, shoreline_pixels)
            count = Counter(values)
            totals.update(count)
            preview_file = f"candidate_{x:02d}_{y:02d}.png"
            render_preview(values, out / preview_file)
            result["tiles"].append({
                "chunkX": x, "chunkY": y,
                "runtimePngSha256": item["runtime_sha256"],
                "sourcePngSha256": item["source_sha256"],
                "sourceFile": item["file"],
                "previewFile": preview_file,
                "candidateRle": rle(values),
                "candidateCounts": dict(count),
                "reviewed": False
            })
            placeholder["Tiles"].append({
                "ChunkX": x, "ChunkY": y,
                "PngSha256": item["runtime_sha256"],
                "Reviewed": False,
                "ReviewEvidence": "",
                "Runs": [{"Length": CORE_SIZE * CORE_SIZE, "Kind": UNKNOWN}]
            })
    result["totalPixels"] = sum(totals.values())
    result["totalCandidatePixels"] = dict(totals)
    result["selectedTiles"] = len(result["tiles"])
    (out / "wave6-semantic-candidates.json").write_text(
        json.dumps(result, separators=(",", ":"), ensure_ascii=False),
        encoding="utf-8")
    (out / "review-stub-ALL-UNKNOWN.json").write_text(
        json.dumps(placeholder, separators=(",", ":"), ensure_ascii=False),
        encoding="utf-8")
    return result


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--manifest", type=Path, required=True)
    p.add_argument("--tiles", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    p.add_argument("--chunk-x", type=int, default=32)
    p.add_argument("--chunk-y", type=int, default=32)
    p.add_argument("--radius", type=int, default=1)
    p.add_argument("--shoreline", type=int, default=5)
    args = p.parse_args()
    result = build(args.manifest, args.tiles, args.output,
                   args.chunk_x, args.chunk_y, args.radius, args.shoreline)
    print("WAVE6_PROPOSAL_ONLY=TRUE")
    print("WAVE6_TERRAIN_CERTIFIED=FALSE")
    print("TILES_PROCESSED=" + str(result["selectedTiles"]))
    print("CLASS_COUNTS=" + json.dumps(result["totalCandidatePixels"]))
    print("OUTPUT=" + str(args.output))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())