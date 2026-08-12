#!/usr/bin/env python3
"""Build the Unity Wave6 50x50 runtime bundle without altering source art."""

from __future__ import annotations

import argparse
import hashlib
import json
from functools import lru_cache
from pathlib import Path

import numpy as np
from PIL import Image


EXPECTED_MASTER_SHA256 = "03793053993cf71af0ed1997fbb8a00c695ca32f31ddd512b28625002c033203"
EXPECTED_SOURCE_TILE_SET_SHA256 = "5b8ecfb91fb89108468a082df9018ddd2895c7d337ceea62384e177b61caaa87"
SCHEMA = "bee-kingdom.world-map.wave6-unity-runtime-bundle.v1"
ROWS = 50
COLS = 50
TILE_SIZE = 512
GUTTER = 2
RUNTIME_SIZE = TILE_SIZE + GUTTER * 2
ORIGIN_CHUNK_X = 7
ORIGIN_CHUNK_Y = 7


def project_root() -> Path:
    return Path(__file__).resolve().parents[2]


def source_root() -> Path:
    return project_root() / "artifacts/UIB_ImmenseContinuousMaster50x50_staging"


def output_root() -> Path:
    return (
        project_root()
        / "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/"
        "UIB_ImmenseContinuousMaster50x50_v1"
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
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, value: dict) -> None:
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def tile_id(row: int, column: int) -> str:
    return f"R{row:02d}C{column:02d}"


def source_name(row: int, column: int) -> str:
    return f"wave6_50x50_C{column:02d}_{row:02d}.png"


def checkpoint_name(row: int, column: int) -> str:
    if row < 25:
        return "checkpoint_C_hd_25" if column < 25 else "checkpoint_D_hd_50"
    return "checkpoint_E_hd_75" if column < 25 else "checkpoint_F_hd_100"


def hash_manifest_name(checkpoint: str) -> str:
    suffix = {
        "checkpoint_C_hd_25": "checkpoint_C_hd25_hashes.json",
        "checkpoint_D_hd_50": "checkpoint_D_hd50_hashes.json",
        "checkpoint_E_hd_75": "checkpoint_E_hd75_hashes.json",
        "checkpoint_F_hd_100": "checkpoint_F_hd100_hashes.json",
    }
    return suffix[checkpoint]


class Wave6BundleBuilder:
    def __init__(self) -> None:
        self.source = source_root()
        self.output = output_root()
        self.master_manifest_path = (
            self.source
            / "checkpoint_G_native_master_25600/master_wave6_50x50_25600_manifest.json"
        )
        self.master_path = (
            self.source
            / "checkpoint_G_native_master_25600/master_wave6_50x50_25600.png"
        )
        self.master_manifest = load_json(self.master_manifest_path)
        self.source_hashes: dict[str, str] = {}
        for checkpoint in (
            "checkpoint_C_hd_25",
            "checkpoint_D_hd_50",
            "checkpoint_E_hd_75",
            "checkpoint_F_hd_100",
        ):
            records = load_json(self.source / checkpoint / hash_manifest_name(checkpoint))
            for file_name, digest in records.items():
                if file_name in self.source_hashes:
                    raise ValueError(f"Duplicate Wave6 source record: {file_name}")
                self.source_hashes[file_name] = str(digest).lower()

    def source_path(self, row: int, column: int) -> Path:
        return self.source / checkpoint_name(row, column) / "tiles_512" / source_name(row, column)

    def validate_source_contract(self) -> None:
        manifest = self.master_manifest
        if str(manifest.get("master_sha256", "")).lower() != EXPECTED_MASTER_SHA256:
            raise ValueError("Unexpected Wave6 master SHA-256 in source manifest.")
        if manifest.get("master_size_px") != [ROWS * TILE_SIZE, COLS * TILE_SIZE]:
            raise ValueError("Wave6 master dimensions are not 25600x25600.")
        if manifest.get("grid_tiles") != [ROWS, COLS] or manifest.get("tile_size_px") != TILE_SIZE:
            raise ValueError("Unexpected Wave6 grid contract.")
        if manifest.get("source_tile_count") != ROWS * COLS:
            raise ValueError("Wave6 manifest does not declare exactly 2500 source tiles.")
        if str(manifest.get("source_tile_set_hash_sha256", "")).lower() != EXPECTED_SOURCE_TILE_SET_SHA256:
            raise ValueError("Wave6 source tile-set hash contract changed.")
        if len(self.source_hashes) != ROWS * COLS or len(set(self.source_hashes.values())) != ROWS * COLS:
            raise ValueError("Wave6 source hash manifests do not contain 2500 unique tiles.")
        if sha256_file(self.master_path) != EXPECTED_MASTER_SHA256:
            raise ValueError("Frozen Wave6 native master SHA-256 mismatch.")

        for row in range(ROWS):
            for column in range(COLS):
                file_name = source_name(row, column)
                path = self.source_path(row, column)
                if not path.is_file() or file_name not in self.source_hashes:
                    raise ValueError(f"Missing Wave6 source tile: {file_name}")

    @lru_cache(maxsize=128)
    def load_source_pixels(self, row: int, column: int) -> np.ndarray:
        path = self.source_path(row, column)
        file_name = source_name(row, column)
        if sha256_file(path) != self.source_hashes[file_name]:
            raise ValueError(f"Source SHA-256 mismatch for {file_name}.")

        with Image.open(path) as image:
            image.load()
            if image.mode != "RGB" or image.size != (TILE_SIZE, TILE_SIZE):
                raise ValueError(f"Unexpected source image contract for {file_name}: {image.mode} {image.size}")
            return np.asarray(image, dtype=np.uint8).copy()

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
        master_manifest_sha = sha256_file(self.master_manifest_path)

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
                source_file_name = source_name(row, column)
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
                        "source_checkpoint": checkpoint_name(row, column),
                        "source_file": source_file_name,
                        "source_sha256": self.source_hashes[source_file_name],
                        "source_decoded_rgb_sha256": sha256_bytes(source_pixels.tobytes()),
                        "runtime_sha256": sha256_file(output_path),
                        "runtime_decoded_rgb_sha256": sha256_bytes(pixels.tobytes()),
                    }
                )
                if len(runtime_records) % 50 == 0:
                    print(f"[{len(runtime_records):04d}/2500] {file_name}", flush=True)

        manifest = {
            "schema": SCHEMA,
            "source": {
                "manifest": self.master_manifest_path.relative_to(project_root()).as_posix(),
                "manifest_sha256": master_manifest_sha,
                "master_sha256": EXPECTED_MASTER_SHA256,
                "source_tile_set_sha256": EXPECTED_SOURCE_TILE_SET_SHA256,
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
                "resources_root": "WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v1",
                "texture_wrap": "Clamp",
                "filter": "Bilinear",
                "mipmaps": False,
                "cache_capacity": 128,
                "visible_prefetch_ring": 1,
                "mapping": "R00C00_to_C07_07;R24C24_near_world_center;R49C49_to_C56_56",
                "old_wave5_25x25_active": False,
            },
            "tile_count": len(runtime_records),
            "tiles": runtime_records,
        }
        write_json(self.output / "runtime_manifest.json", manifest)
        return manifest

    def validate_output(self, manifest: dict) -> None:
        records = {record["id"]: record for record in manifest["tiles"]}
        if len(records) != ROWS * COLS:
            raise ValueError("Runtime manifest does not contain 2500 unique records.")

        @lru_cache(maxsize=128)
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
            "schema": "bee-kingdom.world-map.wave6-unity-runtime-bundle-validation.v1",
            "status": "PASS",
            "tile_count": ROWS * COLS,
            "inner_pixel_mismatch_count": 0,
            "neighbor_gutter_mismatch_count": 0,
            "source_master_sha256": EXPECTED_MASTER_SHA256,
            "runtime_manifest_sha256": sha256_file(self.output / "runtime_manifest.json"),
            "source_png_modified": False,
            "wave5_modified": False,
            "monolithic_master_imported": False,
        }
        write_json(self.output / "runtime_validation.json", receipt)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--validate-only", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    builder = Wave6BundleBuilder()
    if args.validate_only:
        manifest = load_json(output_root() / "runtime_manifest.json")
    else:
        manifest = builder.build()
    builder.validate_output(manifest)
    print(f"WAVE6_UNITY_RUNTIME_BUNDLE=PASS output={output_root()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
