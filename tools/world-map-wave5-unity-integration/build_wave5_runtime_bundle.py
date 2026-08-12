#!/usr/bin/env python3
"""Builds the Unity Wave5 25x25 runtime bundle without altering source art."""

from __future__ import annotations

import argparse
import hashlib
import json
from functools import lru_cache
from pathlib import Path

import numpy as np
from PIL import Image


EXPECTED_MASTER_SHA256 = "50f3ff9640251f365484f31de4aa5ab542587381e5f8eeb9324d67be37125913"
SCHEMA = "bee-kingdom.world-map.wave5-unity-runtime-bundle.v1"
ROWS = 25
COLS = 25
TILE_SIZE = 512
GUTTER = 2
RUNTIME_SIZE = TILE_SIZE + GUTTER * 2
ORIGIN_CHUNK_X = 20
ORIGIN_CHUNK_Y = 20


def project_root() -> Path:
    return Path(__file__).resolve().parents[2]


def source_root() -> Path:
    return project_root() / "artifacts/UIB_ImmenseContinuousMaster25x25_staging"


def output_root() -> Path:
    return (
        project_root()
        / "Assets/BeeKingdom/Playground/Resources/WorldMapWave5Runtime/"
        "UIB_ImmenseContinuousMaster25x25_v1"
    )


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value: dict) -> None:
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def tile_id(row: int, column: int) -> str:
    return f"R{row:02d}C{column:02d}"


class Wave5BundleBuilder:
    def __init__(self) -> None:
        self.source = source_root()
        self.output = output_root()
        self.source_manifest_path = self.source / "manifest.json"
        self.source_manifest = load_json(self.source_manifest_path)
        self.source_records = {tile["id"]: tile for tile in self.source_manifest["tiles"]}

    def validate_source_contract(self) -> None:
        master = self.source_manifest.get("master", {})
        grid = self.source_manifest.get("grid", {})
        if master.get("sha256", "").lower() != EXPECTED_MASTER_SHA256:
            raise ValueError("Unexpected Wave5 master SHA-256 in source manifest.")
        if grid != {"rows": ROWS, "columns": COLS, "tile_size": TILE_SIZE}:
            raise ValueError(f"Unexpected Wave5 grid contract: {grid!r}")
        if self.source_manifest.get("tile_count") != ROWS * COLS:
            raise ValueError("Wave5 source manifest does not contain exactly 625 tiles.")

        expected = [tile_id(row, column) for row in range(ROWS) for column in range(COLS)]
        actual = [tile["id"] for tile in self.source_manifest["tiles"]]
        if actual != expected:
            raise ValueError("Wave5 source tile order is not canonical row-major order.")

    @lru_cache(maxsize=96)
    def load_source_pixels(self, row: int, column: int) -> np.ndarray:
        identifier = tile_id(row, column)
        record = self.source_records[identifier]
        path = self.source / record["file"]
        if sha256_file(path) != record["sha256"].lower():
            raise ValueError(f"Source SHA-256 mismatch for {identifier}.")

        with Image.open(path) as image:
            image.load()
            if image.mode != "RGB" or image.size != (TILE_SIZE, TILE_SIZE):
                raise ValueError(f"Unexpected source image contract for {identifier}: {image.mode} {image.size}")
            pixels = np.asarray(image, dtype=np.uint8).copy()

        if sha256_bytes(pixels.tobytes()) != record["decoded_rgb_sha256"].lower():
            raise ValueError(f"Decoded RGB SHA-256 mismatch for {identifier}.")
        return pixels

    def build_runtime_pixels(self, row: int, column: int) -> np.ndarray:
        global_y = np.clip(
            np.arange(row * TILE_SIZE - GUTTER, row * TILE_SIZE + TILE_SIZE + GUTTER),
            0,
            ROWS * TILE_SIZE - 1,
        )
        global_x = np.clip(
            np.arange(column * TILE_SIZE - GUTTER, column * TILE_SIZE + TILE_SIZE + GUTTER),
            0,
            COLS * TILE_SIZE - 1,
        )
        source_rows = global_y // TILE_SIZE
        source_columns = global_x // TILE_SIZE
        output = np.empty((RUNTIME_SIZE, RUNTIME_SIZE, 3), dtype=np.uint8)

        for source_row in np.unique(source_rows):
            output_rows = np.flatnonzero(source_rows == source_row)
            local_rows = global_y[output_rows] - source_row * TILE_SIZE
            for source_column in np.unique(source_columns):
                output_columns = np.flatnonzero(source_columns == source_column)
                local_columns = global_x[output_columns] - source_column * TILE_SIZE
                source_pixels = self.load_source_pixels(int(source_row), int(source_column))
                output[np.ix_(output_rows, output_columns)] = source_pixels[np.ix_(local_rows, local_columns)]

        return output

    def build(self) -> dict:
        self.validate_source_contract()
        self.output.mkdir(parents=True, exist_ok=True)
        runtime_records: list[dict] = []
        source_manifest_sha = sha256_file(self.source_manifest_path)

        for row in range(ROWS):
            for column in range(COLS):
                identifier = tile_id(row, column)
                pixels = self.build_runtime_pixels(row, column)
                source_pixels = self.load_source_pixels(row, column)
                inner = pixels[GUTTER : GUTTER + TILE_SIZE, GUTTER : GUTTER + TILE_SIZE]
                if not np.array_equal(inner, source_pixels):
                    raise ValueError(f"Runtime inner 512x512 changed source pixels for {identifier}.")

                file_name = identifier + "_g2.png"
                output_path = self.output / file_name
                Image.fromarray(pixels, mode="RGB").save(output_path, format="PNG", compress_level=6)
                runtime_records.append(
                    {
                        "id": identifier,
                        "row": row,
                        "column": column,
                        "chunk_x": ORIGIN_CHUNK_X + column,
                        "chunk_y": ORIGIN_CHUNK_Y + row,
                        "resource_name": identifier + "_g2",
                        "file": file_name,
                        "width": RUNTIME_SIZE,
                        "height": RUNTIME_SIZE,
                        "gutter": GUTTER,
                        "source_file": self.source_records[identifier]["file"],
                        "source_sha256": self.source_records[identifier]["sha256"].lower(),
                        "source_decoded_rgb_sha256": self.source_records[identifier]["decoded_rgb_sha256"].lower(),
                        "runtime_sha256": sha256_file(output_path),
                        "runtime_decoded_rgb_sha256": sha256_bytes(pixels.tobytes()),
                    }
                )
                print(f"[{len(runtime_records):03d}/625] {file_name}")

        manifest = {
            "schema": SCHEMA,
            "source": {
                "manifest": self.source_manifest_path.relative_to(project_root()).as_posix(),
                "manifest_sha256": source_manifest_sha,
                "master_sha256": EXPECTED_MASTER_SHA256,
                "source_png_modified": False,
                "monolithic_master_imported": False,
            },
            "grid": {
                "rows": ROWS,
                "columns": COLS,
                "tile_size": TILE_SIZE,
                "runtime_tile_size": RUNTIME_SIZE,
                "gutter": GUTTER,
                "origin_chunk_x": ORIGIN_CHUNK_X,
                "origin_chunk_y": ORIGIN_CHUNK_Y,
                "world_width": COLS * TILE_SIZE,
                "world_height": ROWS * TILE_SIZE,
            },
            "runtime": {
                "resources_root": "WorldMapWave5Runtime/UIB_ImmenseContinuousMaster25x25_v1",
                "texture_wrap": "Clamp",
                "filter": "Bilinear",
                "mipmaps": False,
                "cache_capacity": 96,
                "visible_prefetch_ring": 1,
                "mapping": "R00C00_to_C20_20;R12C12_to_C32_32;R24C24_to_C44_44",
                "old_wave3_5x5_active": False,
            },
            "tile_count": len(runtime_records),
            "tiles": runtime_records,
        }
        write_json(self.output / "runtime_manifest.json", manifest)
        return manifest

    def validate_output(self, manifest: dict) -> None:
        records = {record["id"]: record for record in manifest["tiles"]}
        if len(records) != ROWS * COLS:
            raise ValueError("Runtime manifest does not contain 625 unique records.")

        @lru_cache(maxsize=96)
        def runtime_pixels(row: int, column: int) -> np.ndarray:
            record = records[tile_id(row, column)]
            path = self.output / record["file"]
            if sha256_file(path) != record["runtime_sha256"]:
                raise ValueError(f"Runtime SHA mismatch for {record['id']}.")
            with Image.open(path) as image:
                image.load()
                if image.mode != "RGB" or image.size != (RUNTIME_SIZE, RUNTIME_SIZE):
                    raise ValueError(f"Runtime image contract mismatch for {record['id']}.")
                return np.asarray(image, dtype=np.uint8).copy()

        for row in range(ROWS):
            for column in range(COLS):
                pixels = runtime_pixels(row, column)
                source_pixels = self.load_source_pixels(row, column)
                if not np.array_equal(
                    pixels[GUTTER : GUTTER + TILE_SIZE, GUTTER : GUTTER + TILE_SIZE],
                    source_pixels,
                ):
                    raise ValueError(f"Inner tile mismatch after PNG decode for {tile_id(row, column)}.")
                if column + 1 < COLS:
                    east = runtime_pixels(row, column + 1)
                    if not np.array_equal(pixels[:, -GUTTER:], east[:, GUTTER : GUTTER * 2]):
                        raise ValueError(f"East gutter mismatch at {tile_id(row, column)}.")
                    if not np.array_equal(east[:, :GUTTER], pixels[:, -GUTTER * 2 : -GUTTER]):
                        raise ValueError(f"West gutter mismatch at {tile_id(row, column + 1)}.")
                if row + 1 < ROWS:
                    south = runtime_pixels(row + 1, column)
                    if not np.array_equal(pixels[-GUTTER:, :], south[GUTTER : GUTTER * 2, :]):
                        raise ValueError(f"South gutter mismatch at {tile_id(row, column)}.")
                    if not np.array_equal(south[:GUTTER, :], pixels[-GUTTER * 2 : -GUTTER, :]):
                        raise ValueError(f"North gutter mismatch at {tile_id(row + 1, column)}.")

        receipt = {
            "schema": "bee-kingdom.world-map.wave5-unity-runtime-bundle-validation.v1",
            "status": "PASS",
            "tile_count": ROWS * COLS,
            "inner_pixel_mismatch_count": 0,
            "neighbor_gutter_mismatch_count": 0,
            "source_master_sha256": EXPECTED_MASTER_SHA256,
            "runtime_manifest_sha256": sha256_file(self.output / "runtime_manifest.json"),
            "source_png_modified": False,
            "monolithic_master_imported": False,
        }
        write_json(self.output / "runtime_validation.json", receipt)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--validate-only", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    builder = Wave5BundleBuilder()
    if args.validate_only:
        manifest = load_json(output_root() / "runtime_manifest.json")
    else:
        manifest = builder.build()
    builder.validate_output(manifest)
    print(f"WAVE5_UNITY_RUNTIME_BUNDLE=PASS output={output_root()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
