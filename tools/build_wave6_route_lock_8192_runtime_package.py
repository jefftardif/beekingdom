#!/usr/bin/env python3
import argparse
import hashlib
import json
import shutil
from datetime import datetime, timezone
from functools import lru_cache
from pathlib import Path

from PIL import Image


ROWS = 50
COLUMNS = 50
TILE_SIZE = 512
GUTTER = 2
RUNTIME_TILE_SIZE = TILE_SIZE + GUTTER * 2
ORIGIN_CHUNK_X = 7
ORIGIN_CHUNK_Y = 7
EXPECTED_SOURCE_SIZE = 8192
EXPECTED_SOURCE_SHA256 = "307FF4B6EC6D08FCEF196AEF5298AA79F5D5FD7AFC634BE4834CB999BE8ACD0F"


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--reports", required=True, type=Path)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    source_path = args.source.resolve()
    output_dir = args.output.resolve()
    reports_dir = args.reports.resolve()
    reports_dir.mkdir(parents=True, exist_ok=True)

    if not source_path.exists():
        raise FileNotFoundError(source_path)

    source_sha = sha256_file(source_path)
    if source_sha != EXPECTED_SOURCE_SHA256:
        raise RuntimeError(f"Unexpected 8192 proof SHA-256: {source_sha}")

    if output_dir.exists():
        backup = output_dir.with_name(output_dir.name + "_backup_" + datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"))
        shutil.move(str(output_dir), str(backup))
    output_dir.mkdir(parents=True, exist_ok=True)

    source = Image.open(source_path).convert("RGB")
    if source.size != (EXPECTED_SOURCE_SIZE, EXPECTED_SOURCE_SIZE):
        raise RuntimeError(f"Unexpected source size: {source.size}")

    source_tile_width = source.width / COLUMNS
    source_tile_height = source.height / ROWS

    @lru_cache(maxsize=384)
    def core(row: int, column: int) -> Image.Image:
        x0 = column * source_tile_width
        y0 = row * source_tile_height
        return source.transform(
            (TILE_SIZE, TILE_SIZE),
            Image.Transform.AFFINE,
            (source_tile_width / TILE_SIZE, 0.0, x0, 0.0, source_tile_height / TILE_SIZE, y0),
            resample=Image.Resampling.BICUBIC,
        )

    tiles = []
    for row in range(ROWS):
        for column in range(COLUMNS):
            canvas = Image.new("RGB", (RUNTIME_TILE_SIZE, RUNTIME_TILE_SIZE))
            center = core(row, column)
            canvas.paste(center, (GUTTER, GUTTER))

            if row > 0:
                canvas.paste(core(row - 1, column).crop((0, TILE_SIZE - GUTTER, TILE_SIZE, TILE_SIZE)), (GUTTER, 0))
            else:
                canvas.paste(center.crop((0, 0, TILE_SIZE, GUTTER)), (GUTTER, 0))

            if row < ROWS - 1:
                canvas.paste(core(row + 1, column).crop((0, 0, TILE_SIZE, GUTTER)), (GUTTER, GUTTER + TILE_SIZE))
            else:
                canvas.paste(center.crop((0, TILE_SIZE - GUTTER, TILE_SIZE, TILE_SIZE)), (GUTTER, GUTTER + TILE_SIZE))

            if column > 0:
                canvas.paste(core(row, column - 1).crop((TILE_SIZE - GUTTER, 0, TILE_SIZE, TILE_SIZE)), (0, GUTTER))
            else:
                canvas.paste(center.crop((0, 0, GUTTER, TILE_SIZE)), (0, GUTTER))

            if column < COLUMNS - 1:
                canvas.paste(core(row, column + 1).crop((0, 0, GUTTER, TILE_SIZE)), (GUTTER + TILE_SIZE, GUTTER))
            else:
                canvas.paste(center.crop((TILE_SIZE - GUTTER, 0, TILE_SIZE, TILE_SIZE)), (GUTTER + TILE_SIZE, GUTTER))

            for dy, src_row in ((0, row - 1), (GUTTER + TILE_SIZE, row + 1)):
                for dx, src_col in ((0, column - 1), (GUTTER + TILE_SIZE, column + 1)):
                    safe_row = min(max(src_row, 0), ROWS - 1)
                    safe_col = min(max(src_col, 0), COLUMNS - 1)
                    x = 0 if src_col >= column else TILE_SIZE - GUTTER
                    y = 0 if src_row >= row else TILE_SIZE - GUTTER
                    canvas.paste(core(safe_row, safe_col).crop((x, y, x + GUTTER, y + GUTTER)), (dx, dy))

            tile_id = f"R{row:02d}C{column:02d}_g2"
            canvas.save(output_dir / f"{tile_id}.png", optimize=False)
            tiles.append(
                {
                    "row": row,
                    "column": column,
                    "chunk_x": ORIGIN_CHUNK_X + column,
                    "chunk_y": ORIGIN_CHUNK_Y + row,
                    "resource_name": tile_id,
                    "width": RUNTIME_TILE_SIZE,
                    "height": RUNTIME_TILE_SIZE,
                    "gutter": GUTTER,
                }
            )

    manifest = {
        "schema": "bee-kingdom.world-map.wave6-unity-runtime-bundle.v1",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "package_kind": "route_lock_8192_scale_bridge_proof_unity_runtime_audit_only",
        "source": {
            "master_sha256": EXPECTED_SOURCE_SHA256,
            "source_proof_path": str(source_path),
            "source_proof_resolution": [source.width, source.height],
            "production_note": "Unity 50x50 audit package from one coherent 8192 scale-bridge proof. Not native final HD, not canonical swap.",
        },
        "grid": {
            "rows": ROWS,
            "columns": COLUMNS,
            "tile_size": TILE_SIZE,
            "runtime_tile_size": RUNTIME_TILE_SIZE,
            "gutter": GUTTER,
            "origin_chunk_x": ORIGIN_CHUNK_X,
            "origin_chunk_y": ORIGIN_CHUNK_Y,
        },
        "tile_count": len(tiles),
        "tiles": tiles,
    }
    (output_dir / "runtime_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    receipt = {
        "schema": "beekingdom.wave6.route_lock_8192_scale_bridge.runtime_package.receipt.v1",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": "PASS_PACKAGE_CREATED_AUDIT_ONLY",
        "source": str(source_path),
        "source_sha256": source_sha,
        "output": str(output_dir),
        "tile_count": len(tiles),
        "runtime_tile_size": RUNTIME_TILE_SIZE,
        "gutter": GUTTER,
        "gates": {
            "NO_BLIND_2500_TILE_REGEN": "YES",
            "ONE_COHERENT_SOURCE_USED": "YES",
            "NATIVE_FINAL_HD": "NO",
            "READY_FOR_QA_BUILDERC": "NO",
            "READY_FOR_UNITY_HANDOFF": "NO",
            "READY_FOR_CANONICAL_SWAP": "NO",
        },
    }
    (reports_dir / "WorldMapWave6_RouteLock8192ScaleBridge_RuntimePackageReceipt.json").write_text(
        json.dumps(receipt, indent=2),
        encoding="utf-8",
    )
    print(json.dumps({"status": receipt["status"], "tile_count": len(tiles), "output": str(output_dir)}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
